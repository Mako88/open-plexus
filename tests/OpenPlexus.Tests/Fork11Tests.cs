using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Machines;
using OpenPlexus.Thinking;

namespace OpenPlexus.Tests;

/// <summary>
/// Fork 11 — <b>the output machine is addressed, so a second one can exist.</b>
/// </summary>
/// <remarks>
/// <b>WHAT WAS WRONG WAS NOT THAT NOBODY HAD WRITTEN IT.</b> Acting required
/// holding the asker's <see cref="Thought"/>, and only the asker ever holds one,
/// so several output machines acting on one broadcast was not a missing feature —
/// it was inexpressible. These check the shape that makes it expressible, and the
/// two traps it had to be routed around.
/// </remarks>
public sealed class Fork11Tests
{
    private static Code C(ulong value) => Fixture.C(value);

    /// <summary>An actuator that records what it was told to do.</summary>
    private sealed class Actuator(string name, params Code[] codes) : IReceiveArrivals
    {
        private readonly OutputMachine _machine =
            new(new MachineAddress(name), codes);

        public MachineAddress Address => _machine.Address;

        public IReadOnlyCollection<Code> Codes => _machine.Codes;

        public List<Code> Did { get; } = [];

        public int Waiting => _machine.Waiting;

        public async Task DeliverAsync(Settled settled, CancellationToken ct = default)
        {
            await _machine.DeliverAsync(settled, ct).ConfigureAwait(false);

            if (_machine.Take(settled.Broadcast) is { } chosen) Did.Add(chosen);
        }
    }

    private static Settled Finished(params Code[] reached) => new()
    {
        Broadcast = BroadcastId.New(),
        From = new MachineAddress("asker"),
        Arrivals = [.. reached.Select((code, at) => new Arrival
        {
            Endpoint = code,
            Score = 1.0 / (at + 1),
            Chain = [code],
            Best = 1.0 / (at + 1),
            Routes = 1,
        })],
    };

    // ---- the thing that was not expressible --------------------------------

    [Fact]
    public async Task Two_output_machines_both_act_on_one_broadcast()
    {
        // THE WHOLE POINT OF THE FORK. Neither holds the thought, neither knows
        // the other exists, and the asker knows about neither.
        var bus = new HybridBus();
        bus.Faults += failure => throw failure;

        var hands = new Actuator("hands", C(10), C(11));
        var voice = new Actuator("voice", C(20), C(21));

        using var one = bus.Listen(hands, hands.Codes);
        using var two = bus.Listen(voice, voice.Codes);

        await bus.PublishAsync(Finished(C(10), C(21)));
        await bus.WhenIdle().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal([C(10)], hands.Did);
        Assert.Equal([C(21)], voice.Did);
    }

    [Fact]
    public async Task A_machine_hears_nothing_about_a_thought_that_reached_none_of_its_codes()
    {
        // Routing by code is what keeps the publisher ignorant of who listens.
        // If everyone heard everything, the filtering would be each actuator's
        // problem and the bus would be a megaphone.
        var bus = new HybridBus();
        bus.Faults += failure => throw failure;

        var voice = new Actuator("voice", C(20));
        using var handle = bus.Listen(voice, voice.Codes);

        await bus.PublishAsync(Finished(C(10), C(11)));
        await bus.WhenIdle().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Empty(voice.Did);
        Assert.Equal(0, voice.Waiting);
    }

    [Fact]
    public async Task Publishing_with_nobody_listening_is_not_an_error()
    {
        // EVERY MEASUREMENT TAKEN BEFORE THIS EXISTED. A harness can publish
        // unconditionally without knowing whether anything is attached, which is
        // what keeps this additive.
        var bus = new HybridBus();
        bus.Faults += failure => throw failure;

        await bus.PublishAsync(Finished(C(10)));
        await bus.WhenIdle().WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task A_machine_that_has_stopped_listening_is_not_told()
    {
        var bus = new HybridBus();
        bus.Faults += failure => throw failure;

        var hands = new Actuator("hands", C(10));
        var handle = bus.Listen(hands, hands.Codes);
        handle.Dispose();

        await bus.PublishAsync(Finished(C(10)));
        await bus.WhenIdle().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Empty(hands.Did);
    }

    // ---- the traps it had to be routed around ------------------------------

    [Fact]
    public async Task A_published_thought_is_taken_once()
    {
        // A finished result is not a standing fact. Leaving it in would let one
        // broadcast drive an action twice, and would grow the map forever.
        var machine = new OutputMachine(new MachineAddress("hands"), [C(10)]);
        var settled = Finished(C(10));

        await machine.DeliverAsync(settled);

        Assert.Equal(1, machine.Waiting);
        Assert.Equal(C(10), machine.Take(settled.Broadcast));

        Assert.Equal(0, machine.Waiting);
        Assert.Null(machine.Take(settled.Broadcast));
    }

    [Fact]
    public void Taking_a_broadcast_nobody_published_is_null_rather_than_a_throw()
    {
        // NULL IS A REAL ANSWER, and it is the same answer as "nothing reached
        // me" -- a machine asked about a thought it never heard of has nothing
        // to say, which is not an error condition.
        var machine = new OutputMachine(new MachineAddress("hands"), [C(10)]);

        Assert.Null(machine.Take(BroadcastId.New()));
    }

    [Fact]
    public async Task The_best_scoring_of_this_machines_codes_wins_and_others_are_ignored()
    {
        // Arrival narrows, then rank -- the same rule as the direct call, over
        // arrivals that came by address.
        var machine = new OutputMachine(new MachineAddress("hands"), [C(11)]);

        // C(10) scores higher and is not this machine's to choose.
        var settled = Finished(C(10), C(11));

        await machine.DeliverAsync(settled);

        Assert.Equal(C(11), machine.Take(settled.Broadcast));
    }

    [Fact]
    public async Task Several_thoughts_are_in_flight_at_once_and_do_not_mix()
    {
        // WHAT BroadcastId IS FOR, and why concurrent output is nearly free
        // rather than a new mechanism.
        var machine = new OutputMachine(new MachineAddress("hands"), [C(10), C(11)]);

        var first = Finished(C(10));
        var second = Finished(C(11));

        await machine.DeliverAsync(first);
        await machine.DeliverAsync(second);

        Assert.Equal(2, machine.Waiting);
        Assert.Equal(C(11), machine.Take(second.Broadcast));
        Assert.Equal(C(10), machine.Take(first.Broadcast));
    }

    [Fact]
    public void An_empty_interest_is_refused_rather_than_registered()
    {
        // A listener for nothing is a listener that never fires, which looks
        // exactly like one that was never wired up.
        var bus = new HybridBus();
        var hands = new Actuator("hands", C(10));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => bus.Listen(hands, ImmutableArray<Code>.Empty));
    }
}
