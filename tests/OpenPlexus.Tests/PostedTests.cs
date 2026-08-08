using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Thinking;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Two machines that share nothing but a socket.
/// </summary>
/// <remarks>
/// <para>
/// <b>EVERY DISTRIBUTED CLAIM THIS PROJECT HAS MADE HAS BEEN MEASURED IN ONE PROCESS.</b>
/// <see cref="HybridBus"/> is a dictionary and a <see cref="Task.Delay(int)"/>, so C1, C2
/// and C3 have been honoured against a simulation of a network. These tests use real
/// sockets on real ports, so a message that does not serialise, does not route, or does
/// not arrive fails here rather than on twenty phones.
/// </para>
/// <para>
/// <b>AND THEY ARE NOT A TEST OF C2, WHICH IS THE THING THAT WILL BE ASSUMED.</b> TCP does
/// not reorder within a connection, so this exercises LESS adversity than the simulator
/// does. Green here means the bytes and the routing are right; the ordering constraint is
/// measured where it is injected on purpose.
/// </para>
/// </remarks>
public sealed class PostedTests(ITestOutputHelper output)
{
    /// <inheritdoc cref="Wired.Free"/>
    private static string Free() => Wired.Free();

    /// <summary>A cluster that keeps what it was handed.</summary>
    private sealed class Catches(ClusterAddress address) : IReceiveEnvelopes
    {
        public ClusterAddress Address => address;

        public List<Envelope> Caught { get; } = [];

        public Task DeliverAsync(Envelope envelope, CancellationToken ct = default)
        {
            lock (Caught) Caught.Add(envelope);
            return Task.CompletedTask;
        }
    }

    /// <summary>A machine that keeps the finished thoughts it was told about.</summary>
    private sealed class Hears(MachineAddress address) : IReceiveArrivals
    {
        public MachineAddress Address => address;

        public List<Settled> Heard { get; } = [];

        public Task DeliverAsync(Settled settled, CancellationToken ct = default)
        {
            lock (Heard) Heard.Add(settled);
            return Task.CompletedTask;
        }
    }

    private static Envelope Carrying(ClusterAddress to, Code what) => new()
    {
        To = to,
        Messages =
        [
            new Message
            {
                Broadcast = BroadcastId.New(),
                ReturnTo = new MachineAddress("asker"),
                To = what,
                Held = Math.BitDecrement(4.0),
                Chain = [what],
                Carried = 1.0 / 3.0,
            },
        ],
    };

    /// <inheritdoc cref="Wired.UntilAsync"/>
    /// <param name="done">What is being waited for.</param>
    private static Task<bool> UntilAsync(Func<bool> done) => Wired.UntilAsync(done);

    /// <summary>
    /// <b>AN ENVELOPE CROSSES A SOCKET AND ARRIVES AS ITSELF.</b>
    /// </summary>
    /// <remarks>
    /// The first thing in this repo that is distributed rather than described as
    /// distributable. Two buses, two ports, no shared object between them — the receiving
    /// machine has never seen the sending machine's envelope instance and reconstructs it
    /// from bytes.
    /// </remarks>
    [Fact]
    public async Task An_envelope_reaches_a_cluster_on_another_machine()
    {
        var here = Free();
        var there = Free();

        await using var one = new Posted(here, [new Peer(there)]);
        await using var two = new Posted(there, [new Peer(here)]);

        var mine = new Catches(new ClusterAddress("cluster-far"));

        using var held = two.Subscribe(mine);

        await one.OpenAsync();
        await two.OpenAsync();

        // THE ROSTER HAS TO HAVE CROSSED BEFORE THE ENVELOPE CAN BE ROUTED, and the
        // subscription happened before either door opened -- so `two` announces it on open
        // and `one` learns where the cluster lives.
        Assert.True(
            await UntilAsync(() => one.Known.Contains(mine.Address)),
            "the far cluster never announced itself");

        var what = new Code(11, 4242424242424242);

        await one.SendAsync(mine.Address, Carrying(mine.Address, what));

        Assert.True(await UntilAsync(() => mine.Caught.Count > 0), "the envelope never arrived");

        var got = mine.Caught[0];

        output.WriteLine($"{here} -> {there}: {Wire.Write(got)}");

        Assert.Equal(mine.Address, got.To);
        Assert.Equal(what, got.Messages[0].To);
        Assert.Equal(new MachineAddress("asker"), got.Messages[0].ReturnTo);

        // AND THE DOUBLE BY ITS BITS, because a reading is a quantised number and a value
        // that drifts in its last bit codes differently at a band boundary. Fork 12 with
        // the two halves on different machines is the fault this whole file is about.
        Assert.Equal(
            BitConverter.DoubleToInt64Bits(Math.BitDecrement(4.0)),
            BitConverter.DoubleToInt64Bits(got.Messages[0].Held));
    }

    /// <summary>
    /// <b>A FINISHED THOUGHT REACHES A LISTENER ON ANOTHER MACHINE, ROUTED BY CODE.</b>
    /// </summary>
    /// <remarks>
    /// Fork 11 across a wire. The publisher does not know who listens and cannot, so it
    /// tells every peer and each decides for itself — which is what lets an actuator be
    /// attached on a machine the thinker has never heard of.
    /// </remarks>
    [Fact]
    public async Task A_finished_thought_reaches_a_listener_that_wanted_one_of_its_codes()
    {
        var here = Free();
        var there = Free();

        await using var thinker = new Posted(here, [new Peer(there)]);
        await using var actuator = new Posted(there, [new Peer(here)]);

        var wanted = new Code(3, 99);
        var ignored = new Code(3, 100);

        var acts = new Hears(new MachineAddress("hand"));
        var deaf = new Hears(new MachineAddress("ear"));

        using var listening = actuator.Listen(acts, [wanted]);
        using var notListening = actuator.Listen(deaf, [ignored]);

        await thinker.OpenAsync();
        await actuator.OpenAsync();

        await thinker.PublishAsync(new Settled
        {
            Broadcast = BroadcastId.New(),
            From = new MachineAddress("asker"),
            Arrivals =
            [
                new Arrival
                {
                    Endpoint = wanted,
                    Score = 1.0 / 3.0,
                    Chain = [wanted],
                    Best = 0.5,
                    Routes = 1,
                },
            ],
        });

        Assert.True(await UntilAsync(() => acts.Heard.Count > 0), "the actuator never heard it");

        Assert.Equal(wanted, acts.Heard[0].Arrivals[0].Endpoint);

        // AND THE ONE THAT ASKED FOR A DIFFERENT CODE HEARS NOTHING, which is the half
        // that says the routing is by code rather than by everybody getting everything.
        Assert.Empty(deaf.Heard);
    }

    /// <summary>
    /// <b>A CLUSTER LEAVING IS TOLD TO THE OTHER MACHINE, WHICH IS WHY THIS IS A BUS.</b>
    /// </summary>
    /// <remarks>
    /// C3 says a cluster vanishing mid-thought is normal rather than an error, and a route
    /// heading into one that has gone is stranded — so the origin can only write it off if
    /// somebody says the cluster went. In one process that is an event; across machines it
    /// has to be a message, and this is that message.
    /// </remarks>
    [Fact]
    public async Task A_cluster_leaving_is_heard_on_the_other_machine()
    {
        var here = Free();
        var there = Free();

        await using var watcher = new Posted(here, [new Peer(there)]);
        await using var leaver = new Posted(there, [new Peer(here)]);

        var going = new Catches(new ClusterAddress("cluster-going"));
        var held = leaver.Subscribe(going);

        var deaths = new List<ClusterAddress>();
        watcher.Deaths += gone => { lock (deaths) deaths.Add(gone); };

        await watcher.OpenAsync();
        await leaver.OpenAsync();

        Assert.True(
            await UntilAsync(() => watcher.Known.Contains(going.Address)),
            "the cluster never announced itself");

        held.Dispose();

        Assert.True(await UntilAsync(() => deaths.Count > 0), "the death never crossed");

        Assert.Equal(going.Address, deaths[0]);

        // AND IT IS FORGOTTEN RATHER THAN REMEMBERED AS DEAD, so a machine that comes back
        // and announces itself again is simply reachable once more.
        Assert.DoesNotContain(going.Address, watcher.Known);
    }
}
