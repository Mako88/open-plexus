using OpenPlexus.Bus;
using OpenPlexus.Thinking;

namespace OpenPlexus.Tests;

/// <summary>
/// The transport. What matters is that a sender never waits on a receiver, that
/// leaving is not silent, and that a failure surfaces.
/// </summary>
public sealed class HybridBusTests
{

    private static Envelope To(string cluster) => new()
    {
        To = new ClusterAddress(cluster),
        Messages = [],
    };

    private static Report Reporting() => new()
    {
        From = new ClusterAddress("somewhere"),
        Handled = 1,
        SentInto = [],
        Arrivals = [],
        Accounting = new Accounting(new BroadcastId(Guid.Empty), 0, 1),
    };

    /// <summary>Records what it was handed, and can be made to wait.</summary>
    private sealed class Cluster(string name) : IReceiveEnvelopes
    {
        private readonly List<Envelope> _got = [];

        public ClusterAddress Address { get; } = new(name);

        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Held { get; set; } = Task.CompletedTask;

        public Exception? Throws { get; set; }

        public IReadOnlyList<Envelope> Got
        {
            get { lock (_got) return [.. _got]; }
        }

        public async Task DeliverAsync(Envelope envelope, CancellationToken ct = default)
        {
            Entered.TrySetResult();
            await Held.WaitAsync(Fixture.Patience, ct);
            if (Throws is { } failure) throw failure;
            lock (_got) _got.Add(envelope);
        }
    }

    private sealed class Machine(string name) : IReceiveReports
    {
        private readonly List<Report> _got = [];

        public MachineAddress Address { get; } = new(name);

        public IReadOnlyList<Report> Got
        {
            get { lock (_got) return [.. _got]; }
        }

        public Task DeliverAsync(Report report, CancellationToken ct = default)
        {
            lock (_got) _got.Add(report);
            return Task.CompletedTask;
        }
    }

    // ---- delivery ---------------------------------------------------------

    [Fact]
    public async Task An_envelope_reaches_the_cluster_it_is_addressed_to()
    {
        var bus = new HybridBus();
        var alpha = new Cluster("alpha");
        var beta = new Cluster("beta");
        using var _ = bus.Subscribe(alpha);
        using var __ = bus.Subscribe(beta);

        // ADDRESSED TO THE SECOND SUBSCRIBER ON PURPOSE. Sending to the first
        // let a mutation that ignores the address entirely and always picks
        // `_clusters.Values.First()` survive every assertion here.
        await bus.SendAsync(beta.Address, To("beta"));
        await bus.WhenIdle().WaitAsync(Fixture.Patience);

        Assert.Single(beta.Got);

        // The companion. Without it this passes for a bus that hands every
        // envelope to everybody.
        Assert.Empty(alpha.Got);

        await bus.SendAsync(alpha.Address, To("alpha"));
        await bus.WhenIdle().WaitAsync(Fixture.Patience);

        Assert.Single(alpha.Got);
        Assert.Single(beta.Got);
    }

    [Fact]
    public async Task A_report_reaches_the_machine_that_started_the_thought()
    {
        var bus = new HybridBus();
        var machine = new Machine("origin");
        using var _ = bus.Subscribe(machine);

        await bus.SendAsync(machine.Address, Reporting());
        await bus.WhenIdle().WaitAsync(Fixture.Patience);

        Assert.Single(machine.Got);
    }

    // ---- a sender never waits on a receiver -------------------------------

    [Fact]
    public async Task Sending_returns_before_the_receiver_has_finished()
    {
        var bus = new HybridBus();
        var slow = new Cluster("slow") { Held = new TaskCompletionSource().Task };
        using var _ = bus.Subscribe(slow);

        await bus.SendAsync(slow.Address, To("slow"));

        // The receiver is still inside DeliverAsync and the send has already
        // returned. That is the whole of "unawaited": a fan-out to many
        // clusters is parallel rather than a queue.
        await slow.Entered.Task.WaitAsync(Fixture.Patience);
        Assert.Empty(slow.Got);
        Assert.Equal(1, bus.InFlight);
    }

    [Fact]
    public async Task Two_clusters_are_delivered_to_at_the_same_time()
    {
        var bus = new HybridBus();
        var alpha = new Cluster("alpha");
        var beta = new Cluster("beta");

        // Each waits for the other to have started. Serial delivery cannot
        // satisfy both, so this finishes only if they really overlap.
        alpha.Held = beta.Entered.Task;
        beta.Held = alpha.Entered.Task;

        using var _ = bus.Subscribe(alpha);
        using var __ = bus.Subscribe(beta);

        await bus.SendAsync(alpha.Address, To("alpha"));
        await bus.SendAsync(beta.Address, To("beta"));

        await bus.WhenIdle().WaitAsync(Fixture.Patience);

        Assert.Single(alpha.Got);
        Assert.Single(beta.Got);
    }

    [Fact]
    public async Task Quiet_waits_for_a_delivery_that_is_still_running()
    {
        var bus = new HybridBus();
        var gate = new TaskCompletionSource();
        var slow = new Cluster("slow") { Held = gate.Task };
        using var _ = bus.Subscribe(slow);

        await bus.SendAsync(slow.Address, To("slow"));
        await slow.Entered.Task.WaitAsync(Fixture.Patience);

        var quiet = bus.WhenIdle();
        Assert.False(quiet.IsCompleted);

        gate.SetResult();
        await quiet.WaitAsync(Fixture.Patience);

        Assert.Equal(0, bus.InFlight);
    }

    // ---- leaving is not silent --------------------------------------------

    [Fact]
    public async Task Leaving_the_bus_fires_a_death_carrying_the_address()
    {
        var bus = new HybridBus();
        var departures = new List<ClusterAddress>();
        bus.Deaths += departures.Add;

        var alpha = new Cluster("alpha");
        var handle = bus.Subscribe(alpha);

        // The companion, first: while it is subscribed, nothing has died and a
        // send lands. Without this the assertions below pass for a bus that
        // never delivered anything at all.
        await bus.SendAsync(alpha.Address, To("alpha"));
        await bus.WhenIdle().WaitAsync(Fixture.Patience);
        Assert.Single(alpha.Got);
        Assert.Empty(departures);

        handle.Dispose();

        Assert.Equal([alpha.Address], departures);
    }

    [Fact]
    public void Disposing_twice_reports_one_death()
    {
        var bus = new HybridBus();
        var departures = new List<ClusterAddress>();
        bus.Deaths += departures.Add;

        var handle = bus.Subscribe(new Cluster("alpha"));
        handle.Dispose();
        handle.Dispose();

        Assert.Single(departures);
    }

    [Fact]
    public async Task A_stale_handle_does_not_evict_the_cluster_that_replaced_it()
    {
        // C3 says a cluster vanishing is normal, so one coming back under the
        // same address is normal too. A handle from the previous life must not
        // be able to unsubscribe its successor — which is what makes the
        // guard inside the handle load-bearing rather than a second copy of
        // the check `Leave` already does.
        var bus = new HybridBus();
        var gone = bus.Subscribe(new Cluster("alpha"));
        gone.Dispose();

        var returned = new Cluster("alpha");
        using var _ = bus.Subscribe(returned);

        gone.Dispose();

        await bus.SendAsync(returned.Address, To("alpha"));
        await bus.WhenIdle().WaitAsync(Fixture.Patience);

        Assert.Single(returned.Got);
    }

    [Fact]
    public void An_address_with_nothing_local_and_no_wire_is_a_bug_not_a_drop()
    {
        var bus = new HybridBus();

        // With no remote half, an unknown address can only be a routing error.
        // Dropping it would be indistinguishable from ordinary C2 message loss,
        // which is exactly the confusion that hides a wiring failure.
        Assert.Throws<InvalidOperationException>(
            () => bus.SendAsync(new ClusterAddress("nowhere"), To("nowhere")));
    }

    // ---- failures surface -------------------------------------------------

    [Fact]
    public async Task A_receiver_that_throws_surfaces_the_failure()
    {
        var bus = new HybridBus();
        var faults = new List<Exception>();
        bus.Faults += faults.Add;

        var broken = new Cluster("broken") { Throws = new InvalidOperationException("no") };
        using var _ = bus.Subscribe(broken);

        await bus.SendAsync(broken.Address, To("broken"));
        await bus.WhenIdle().WaitAsync(Fixture.Patience);

        // A send that returns before delivery has no other way to report
        // failure, and swallowing is how a thing turns out never to have been
        // wired up.
        Assert.Single(faults);
        Assert.Equal(0, bus.InFlight);
    }

    [Fact]
    public async Task A_failed_delivery_still_lets_the_bus_go_quiet()
    {
        var bus = new HybridBus();
        bus.Faults += _ => { };

        var broken = new Cluster("broken") { Throws = new InvalidOperationException("no") };
        using var _ = bus.Subscribe(broken);

        await bus.SendAsync(broken.Address, To("broken"));

        await bus.WhenIdle().WaitAsync(Fixture.Patience);
    }

    [Fact]
    public async Task Many_sends_all_arrive()
    {
        var bus = new HybridBus();
        var alpha = new Cluster("alpha");
        using var _ = bus.Subscribe(alpha);

        for (var i = 0; i < 200; i++) await bus.SendAsync(alpha.Address, To("alpha"));
        await bus.WhenIdle().WaitAsync(Fixture.Patience);

        Assert.Equal(200, alpha.Got.Count);
        Assert.Equal(0, bus.InFlight);
    }
}
