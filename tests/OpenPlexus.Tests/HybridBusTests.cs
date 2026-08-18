using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;

namespace OpenPlexus.Tests;

/// <summary>
/// The transport. What matters is that a sender never waits on a receiver, that an ask
/// which never left is written off, and that a failure surfaces.
/// </summary>
/// <remarks>
/// <b>What the walk's deletion took out of this file was the addressed send.</b> An
/// envelope named one cluster, so half of these tests were about the bus picking the right
/// entry out of a dictionary. An ask is a BROADCAST — every holder gets it — so the routing
/// question is gone and what is left is the concurrency, which was always the part worth
/// asserting.
/// </remarks>
public sealed class HybridBusTests
{
    private static Ask Asking() => new()
    {
        Broadcast = BroadcastId.New(),
        ReturnTo = new MachineAddress("asker"),
        Wants = Wanted.Vote,
        Moment = [new Code(1, 2)],
    };

    private static Answer Answering() => new()
    {
        Broadcast = BroadcastId.New(),
        From = new MachineAddress("holder"),
        Said = new Weights { Each = [] },
    };

    /// <summary>Records what it was handed, and can be made to wait.</summary>
    private sealed class Held(string name) : IReceiveAsks
    {
        private readonly List<Ask> _got = [];

        public MachineAddress Address { get; } = new(name);

        public TaskCompletionSource Entered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Waits { get; set; } = Task.CompletedTask;

        public Exception? Throws { get; set; }

        public IReadOnlyList<Ask> Got
        {
            get { lock (_got) return [.. _got]; }
        }

        public async Task DeliverAsync(Ask ask, CancellationToken ct = default)
        {
            Entered.TrySetResult();
            await Waits.WaitAsync(Fixture.Patience, ct);
            if (Throws is { } failure) throw failure;
            lock (_got) _got.Add(ask);
        }
    }

    private sealed class Asking_(string name) : IReceiveAnswers
    {
        private readonly List<Answer> _got = [];

        public MachineAddress Address { get; } = new(name);

        public IReadOnlyList<Answer> Got
        {
            get { lock (_got) return [.. _got]; }
        }

        public Task DeliverAsync(Answer answer, CancellationToken ct = default)
        {
            lock (_got) _got.Add(answer);
            return Task.CompletedTask;
        }
    }

    // ---- delivery ---------------------------------------------------------

    [Fact]
    public async Task An_ask_reaches_every_holder_and_the_asker_is_told_who()
    {
        var bus = new HybridBus();
        var alpha = new Held("alpha");
        var beta = new Held("beta");
        using var _ = bus.Subscribe(alpha);
        using var __ = bus.Subscribe(beta);

        var asked = await bus.AskAsync(Asking());
        await bus.WhenIdle().WaitAsync(Fixture.Patience);

        // The denominator is returned and the answers are not, which is the whole shape of
        // this bus: an asker learns who it asked now and what they said later.
        Assert.Equal([alpha.Address, beta.Address], [.. asked]);

        Assert.Single(alpha.Got);
        Assert.Single(beta.Got);
    }

    [Fact]
    public async Task An_answer_reaches_the_asker_that_asked_and_no_other()
    {
        var bus = new HybridBus();
        var mine = new Asking_("mine");
        var theirs = new Asking_("theirs");
        using var _ = bus.Subscribe(mine);
        using var __ = bus.Subscribe(theirs);

        // Addressed to the second subscriber on purpose. Sending to the first would let a
        // mutation that ignores the address entirely and always takes the first entry
        // survive every assertion here.
        await bus.SendAsync(theirs.Address, Answering());
        await bus.WhenIdle().WaitAsync(Fixture.Patience);

        Assert.Single(theirs.Got);

        // The companion. Without it this passes for a bus that hands every answer to
        // everybody.
        Assert.Empty(mine.Got);
    }

    [Fact]
    public async Task An_answer_to_an_asker_that_has_gone_is_dropped_rather_than_thrown()
    {
        var bus = new HybridBus();

        // C3: An asker that died between asking and being answered is ordinary. Throwing
        // would make one machine's departure another machine's error, which is the whole
        // thing this constraint refuses.
        await bus.SendAsync(new MachineAddress("gone"), Answering());

        await bus.WhenIdle().WaitAsync(Fixture.Patience);
    }

    // ---- a sender never waits on a receiver -------------------------------

    [Fact]
    public async Task Asking_returns_before_the_holder_has_finished()
    {
        var bus = new HybridBus();
        var slow = new Held("slow") { Waits = new TaskCompletionSource().Task };
        using var _ = bus.Subscribe(slow);

        await bus.AskAsync(Asking());

        // The holder is still inside DeliverAsync and the ask has already returned. That is
        // the whole of "unawaited": a fan-out to many holders is parallel rather than a
        // queue, and a queue would put the slowest machine's latency into every round.
        await slow.Entered.Task.WaitAsync(Fixture.Patience);
        Assert.Empty(slow.Got);
        Assert.Equal(1, bus.InFlight);
    }

    [Fact]
    public async Task Two_holders_are_delivered_to_at_the_same_time()
    {
        var bus = new HybridBus();
        var alpha = new Held("alpha");
        var beta = new Held("beta");

        // Each waits for the other to have started. Serial delivery cannot satisfy both, so
        // this finishes only if they really overlap.
        alpha.Waits = beta.Entered.Task;
        beta.Waits = alpha.Entered.Task;

        using var _ = bus.Subscribe(alpha);
        using var __ = bus.Subscribe(beta);

        await bus.AskAsync(Asking());

        await bus.WhenIdle().WaitAsync(Fixture.Patience);

        Assert.Single(alpha.Got);
        Assert.Single(beta.Got);
    }

    [Fact]
    public async Task Quiet_waits_for_a_delivery_that_is_still_running()
    {
        var bus = new HybridBus();
        var gate = new TaskCompletionSource();
        var slow = new Held("slow") { Waits = gate.Task };
        using var _ = bus.Subscribe(slow);

        await bus.AskAsync(Asking());
        await slow.Entered.Task.WaitAsync(Fixture.Patience);

        var quiet = bus.WhenIdle();
        Assert.False(quiet.IsCompleted);

        gate.SetResult();
        await quiet.WaitAsync(Fixture.Patience);

        Assert.Equal(0, bus.InFlight);
    }

    // ---- who is about to be asked, before anyone is asked -----------------

    [Fact]
    public async Task The_asker_is_told_the_denominator_before_any_holder_is_reached()
    {
        var bus = new HybridBus();
        var one = new Held("one");
        using var _ = bus.Subscribe(one);

        var ready = new List<MachineAddress>();

        // An answer to an ask nobody remembers is dropped, so the asker has to record its
        // gathering inside this window. Dispatch is `Task.Run`, so a holder can answer
        // before `AskAsync` returns -- asserting the callback ran before delivery is
        // asserting that the window is real rather than documented.
        await bus.AskAsync(Asking(), ready: everyone => ready.AddRange(everyone));

        Assert.Equal([one.Address], ready);
    }

    // ---- a holder that never took the question ----------------------------

    [Fact]
    public async Task A_holder_that_threw_taking_the_question_is_written_off_by_name()
    {
        var bus = new HybridBus();
        bus.Faults += _ => { };

        var written = new List<(BroadcastId Broadcast, MachineAddress Who)>();
        bus.Unreached += (broadcast, who) => written.Add((broadcast, who));

        var broken = new Held("broken") { Throws = new InvalidOperationException("no") };
        using var _ = bus.Subscribe(broken);

        var ask = Asking();

        await bus.AskAsync(ask);
        await bus.WhenIdle().WaitAsync(Fixture.Patience);

        // Fork 53, in the one form one process can show it. A holder that took the ask and
        // threw is the local spelling of a refused connection: no answer to THAT ask is
        // owed from it, which is a smaller and exacter claim than saying it is dead.
        Assert.Equal([(ask.Broadcast, broken.Address)], written);
    }

    // ---- failures surface -------------------------------------------------

    [Fact]
    public async Task A_receiver_that_throws_surfaces_the_failure()
    {
        var bus = new HybridBus();
        var faults = new List<Exception>();
        bus.Faults += faults.Add;

        var broken = new Held("broken") { Throws = new InvalidOperationException("no") };
        using var _ = bus.Subscribe(broken);

        await bus.AskAsync(Asking());
        await bus.WhenIdle().WaitAsync(Fixture.Patience);

        // A send that returns before delivery has no other way to report failure, and
        // swallowing is how a thing turns out never to have been wired up.
        Assert.Single(faults);
        Assert.Equal(0, bus.InFlight);
    }

    [Fact]
    public async Task A_failed_delivery_still_lets_the_bus_go_quiet()
    {
        var bus = new HybridBus();
        bus.Faults += _ => { };

        var broken = new Held("broken") { Throws = new InvalidOperationException("no") };
        using var _ = bus.Subscribe(broken);

        await bus.AskAsync(Asking());

        await bus.WhenIdle().WaitAsync(Fixture.Patience);
    }

    [Fact]
    public async Task Many_asks_all_arrive_and_all_of_them_are_counted()
    {
        var bus = new HybridBus();
        var alpha = new Held("alpha");
        using var _ = bus.Subscribe(alpha);

        for (var i = 0; i < 200; i++) await bus.AskAsync(Asking());
        await bus.WhenIdle().WaitAsync(Fixture.Patience);

        Assert.Equal(200, alpha.Got.Count);
        Assert.Equal(0, bus.InFlight);

        // The count is the instrument and the arrivals are the behaviour, and a bus that
        // delivered everything while counting nothing would pass the line above alone.
        Assert.Equal(200, bus.Messages);
    }

    [Fact]
    public async Task A_holder_that_has_left_is_not_asked()
    {
        var bus = new HybridBus();
        var going = new Held("going");
        var handle = bus.Subscribe(going);

        // Leaving is silent here, which is the opposite of the walk's cluster. A holder
        // that has unsubscribed is not in the roster, so it is never in the denominator and
        // never owed anything -- there is nothing to announce and nobody to announce it.
        handle.Dispose();

        var asked = await bus.AskAsync(Asking());
        await bus.WhenIdle().WaitAsync(Fixture.Patience);

        Assert.Empty(asked);
        Assert.Empty(going.Got);
    }
}
