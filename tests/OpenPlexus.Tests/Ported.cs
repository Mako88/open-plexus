using OpenPlexus.Bus;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;

namespace OpenPlexus.Tests;

/// <summary>
/// One asker and several holders, each in its own <see cref="Posted"/> on its own port.
/// </summary>
/// <remarks>
/// <para>
/// <b>Separate buses and separate ports is what makes a test distributed at all.</b> Two
/// holders on one bus would share a dictionary, which is the arrangement every measurement
/// in this project has been taken under and the one <see cref="Posted"/> exists to stop
/// being the only one.
/// </para>
/// <para>
/// <b>The asker opens first and the announce-back is why that is safe.</b> A machine
/// announces when it opens and a peer that is not up yet drops it, so whoever opens first
/// would otherwise tell nobody where it is — and every answer to it would have nowhere to
/// go. <see cref="Posted"/> answers an announcement with one, so the roster converges
/// whatever order the fleet came up in.
/// </para>
/// <para>
/// <b>Shared because two files now bring a fleet up and the copies would drift.</b>
/// <c>AskedTests</c> puts one exchange on a socket and <c>FleetTests</c> runs a whole
/// learner over one; a difference in how the fleet is composed would show up as a
/// difference the wire appeared to cause, which is exactly what <c>Fixture.Sharded</c>
/// already exists to prevent one layer down.
/// </para>
/// </remarks>
public sealed class Ported : IAsyncDisposable
{
    private readonly List<IDisposable> _handles = [];
    private readonly List<IDisposable> _subscriptions = [];
    private readonly List<Posted> _machines = [];
    private Posted _asking = null!;

    /// <summary>The machine that puts the questions.</summary>
    public Asker Asker { get; private set; } = null!;

    /// <summary>What each holder holds, in holder order.</summary>
    public List<Population> Held { get; } = [];

    /// <summary>The holders themselves, in holder order.</summary>
    public List<Holder> Holders { get; } = [];

    /// <summary>
    /// Messages the fleet could not hand over or could not act on, across every machine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The one number that separates</b> a run whose answer is wrong from a run whose
    /// evidence never arrived. A gathering waits for a denominator and nothing here
    /// decides a missing holder by a clock, so a lost message can stop a run forever on a
    /// fleet where every machine is alive and idle — and the only reading that says so is
    /// this one.
    /// </para>
    /// <para>
    /// <b>And it is the lost answer that does it now</b>, rather than any lost message. Fork
    /// 53 writes off an ask that failed to leave, so the outbound half no longer strands a
    /// round; what is left is an answer that was sent and did not arrive, which is
    /// indistinguishable from a slow one and is the case nothing may decide. The two are one
    /// count here on purpose — a fleet losing either is a fleet whose wire is unwell.
    /// </para>
    /// </remarks>
    public long Lost =>
        _machines.Concat([_asking]).Sum(one => one.Dropped + one.Refused);

    /// <summary>What has been lost since the fleet finished coming up.</summary>
    /// <remarks>
    /// <b>The coming-up losses are real and are not a fault.</b> Which is why they are
    /// subtracted rather than fixed. A machine announces itself to every peer as it
    /// opens, and a peer that is not listening yet cannot take it — so a fleet of N always
    /// loses exactly N(N-1)/2 announcements, deterministically, and the announce-back is
    /// what makes the roster converge anyway. What matters is whether anything is lost
    /// AFTER that, because a gathering waits on a denominator and one lost answer stops a
    /// run forever.
    /// </remarks>
    public long Since => Lost - _opened;

    private long _opened;

    /// <summary>
    /// A bound on a whole fleet run — <b>the experimenter's patience and never the
    /// machine's.</b>
    /// </summary>
    /// <remarks>
    /// <b>A deadlock detector in exactly the sense <see cref="Wired.ArrivedAsync"/> IS.</b>
    /// Nothing in the library may decide a missing holder by a clock — <i>a miss decided by
    /// a deadline</i> carries a revival row saying never — so a fleet that loses an ANSWER
    /// waits forever, correctly, and a suite that inherited that would hang rather than
    /// fail. This is generous enough to be uninteresting and is asserted on by nothing.
    /// </remarks>
    public static readonly TimeSpan Patience = TimeSpan.FromMinutes(10);

    /// <summary>Brings up an asker and one holder per population, and waits for the roster.</summary>
    /// <param name="holding">What each holder holds, already built.</param>
    /// <param name="slotOf">
    /// Which slot the holder at an index is in, or nothing where the fleet is not
    /// partitioned — <b>fork 62, handed in from here because this is what composes the
    /// fleet.</b>
    /// </param>
    public static async Task<Ported> OpenAsync(
        IReadOnlyList<Population> holding, Func<int, string>? slotOf = null)
    {
        ArgumentNullException.ThrowIfNull(holding);

        var fleet = new Ported();

        var hosts = Enumerable.Range(0, holding.Count + 1).Select(_ => Wired.Free()).ToList();
        var peers = hosts.Select(one => new Peer(one)).ToList();

        // The map is built before the asker because the asker is handed it, and the
        // addresses are known before anything opens a port — which is the whole reason a
        // partition can be a deployment fact rather than something announced.
        var slots = new Dictionary<MachineAddress, string>();

        for (var at = 0; at < holding.Count; at++)
            if (slotOf is not null) slots[new MachineAddress($"holder-{at}")] = slotOf(at);

        fleet._asking = new Posted(hosts[0], peers);

        fleet.Asker = new Asker(
            new MachineAddress("asker"),
            fleet._asking,
            slotOf is null ? null : one => slots[one]);

        fleet._handles.Add(fleet._asking.Subscribe(fleet.Asker));

        for (var at = 0; at < holding.Count; at++)
        {
            var bus = new Posted(hosts[at + 1], peers);
            var address = new MachineAddress($"holder-{at}");
            var holder = new Holder(address, holding[at], bus, slotOf?.Invoke(at));

            var handle = bus.Subscribe(holder);

            fleet._handles.Add(handle);
            fleet._subscriptions.Add(handle);
            fleet._machines.Add(bus);
            fleet.Held.Add(holding[at]);
            fleet.Holders.Add(holder);
        }

        await fleet._asking.OpenAsync().ConfigureAwait(false);

        foreach (var bus in fleet._machines) await bus.OpenAsync().ConfigureAwait(false);

        if (!await Wired.UntilAsync(() => fleet._asking.Holding.Count == holding.Count)
            .ConfigureAwait(false))
            throw new InvalidOperationException(
                $"only {fleet._asking.Holding.Count} of {holding.Count} holders announced");

        // And one ask thrown away, which is the barrier the roster alone cannot be. Knowing
        // where the holders are says the outbound half converged; nothing observable says
        // the holders know where to send an answer, and that is the direction the
        // announce-back exists for. A round trip completing is the only honest check of it,
        // and doing it here keeps every measurement below from paying for the first one.
        using var warming = await fleet.Asker.AskAsync(Wanted.Counts).ConfigureAwait(false);

        if (!await Wired.ArrivedAsync(warming.Everyone).ConfigureAwait(false))
            throw new InvalidOperationException(
                $"{warming.Heard} of {warming.Asked} answers came back, so the return "
                + "path never converged");

        fleet._opened = fleet.Lost;

        return fleet;
    }

    /// <summary>Brings up a fleet already holding a population, split the way the ring would.</summary>
    /// <param name="shards">A population, already placed on holders.</param>
    /// <param name="dials">The brain's numbers.</param>
    public static Task<Ported> OpenAsync(
        IReadOnlyList<List<Commitment>> shards, CommittingSettings dials)
    {
        ArgumentNullException.ThrowIfNull(shards);

        return OpenAsync(shards
            .Select(shard =>
            {
                var held = new Population(dials, seed: 1);
                foreach (var commitment in shard) held.Add(commitment);
                return held;
            })
            .ToList());
    }

    /// <summary>Brings up a fleet holding NOTHING, ready to learn.</summary>
    /// <param name="holders">How many machines.</param>
    /// <param name="dials">The brain's numbers.</param>
    /// <param name="seed">The control arm's generator, the same on every machine.</param>
    /// <remarks>
    /// <para>
    /// <b>The placement is the only thing that makes these machines different.</b> Every
    /// holder sees every observation and runs the identical round, so without
    /// <see cref="Population.Places"/> a fleet of twelve would hold twelve copies of one
    /// population — the same rules minted by every machine that was surprised, which is
    /// not a shard and is not a distribution.
    /// </para>
    /// <para>
    /// <b>AND IT IS <c>Fixture.Sharded</c>'S rule rather than a new one</b>, so a fleet
    /// that learnt its population and a fleet handed one already trained are split the
    /// same way — otherwise a difference between the two arrangements would be a
    /// difference in how the ring was drawn.
    /// </para>
    /// <para>
    /// <b>AND <see cref="Population.Placing"/> is left null, which is not an
    /// oversight.</b> That predicate exists to SIMULATE sharding inside one process, by
    /// telling the repair gate which of the commitments it can see are notionally
    /// elsewhere. Here what a holder can see is genuinely only what it holds, so the
    /// simulation would be a second, disagreeing account of the same fact.
    /// </para>
    /// </remarks>
    public static Task<Ported> OpenAsync(int holders, CommittingSettings dials, int seed)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(holders);

        var holding = new List<Population>(holders);

        for (var at = 0; at < holders; at++)
        {
            var mine = (ulong)at;

            holding.Add(new Population(dials, seed)
            {
                Places = one => one.Identity.Value % (ulong)holders == mine,
            });
        }

        return OpenAsync(holding);
    }

    /// <summary>
    /// Brings up a fleet holding nothing, partitioned into slots with R machines a slot —
    /// <b>fork 62.</b>
    /// </summary>
    /// <param name="slots">How many partitions of the population.</param>
    /// <param name="replicas">How many machines hold each one.</param>
    /// <param name="dials">The brain's numbers.</param>
    /// <param name="seed">The control arm's generator, the same on every machine.</param>
    /// <remarks>
    /// <para>
    /// <b>The replicas cost nothing to keep in sync and that is why this is only a
    /// placement.</b> Every machine is told the same moment and the same settlement, and
    /// <see cref="Population.Places"/> is a fact about a commitment rather than about who
    /// asked — so two machines given one <c>slot</c> mint the same children from the same
    /// stream and stay identical with no message between them. There is nothing here that
    /// copies anything.
    /// </para>
    /// <para>
    /// <b>AND <c>replicas: 1</c> is the fleet this file already built</b>, one slot a holder
    /// and the slot named after nothing else — so a difference between the two arrangements
    /// is the replication and never the composition.
    /// </para>
    /// </remarks>
    public static Task<Ported> OpenAsync(
        int slots, int replicas, CommittingSettings dials, int seed)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(slots);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(replicas);

        var holding = new List<Population>(slots * replicas);

        for (var slot = 0; slot < slots; slot++)
            for (var copy = 0; copy < replicas; copy++)
            {
                var mine = (ulong)slot;

                holding.Add(new Population(dials, seed)
                {
                    Places = one => one.Identity.Value % (ulong)slots == mine,
                });
            }

        return OpenAsync(holding, at => $"slot-{at / replicas}");
    }

    /// <summary>
    /// One holder stops answering while its machine stays up — <b>the death C3 leaves
    /// nothing to observe, and fork 62's whole subject.</b>
    /// </summary>
    /// <param name="which">Which holder goes quiet.</param>
    /// <remarks>
    /// <para>
    /// <b>The other kind of death</b>, and it is the one no write-off can reach.
    /// <see cref="KillAsync"/> closes the door, so a post is refused and the sender WATCHES
    /// the question fail to leave — which is fork 53 and is exact. This drops the
    /// subscription and leaves the listener up: the ask is accepted, acknowledged, routed
    /// nowhere, and no answer ever comes. From the asker that is indistinguishable from a
    /// machine that took the question and died holding it, which is precisely the case only
    /// a deadline could separate and only a slot can survive.
    /// </para>
    /// <para>
    /// <b>And it is deterministic</b>, which is why it is this and not a raced kill. Killing
    /// a machine mid-round means winning a race against a socket to make the test say
    /// anything at all — so the round it lands in would vary run to run and a green suite
    /// would be evidence about scheduling. A holder that never answers is that same
    /// condition with the timing taken out and made permanent.
    /// </para>
    /// </remarks>
    public void Mute(int which)
    {
        _subscriptions[which].Dispose();
    }

    /// <summary>
    /// One holder's machine closes its door — <b>C3, and not a polite departure.</b>
    /// </summary>
    /// <param name="which">Which holder dies.</param>
    /// <remarks>
    /// <b>The whole machine rather than the subscription, because those are different
    /// deaths.</b> Dropping a subscription leaves a listener that accepts the ask and
    /// routes it nowhere; closing the door refuses the connection, which is what a phone
    /// going into a tunnel does. The second is the one the design claims to survive, and it
    /// is the harsher of the two.
    /// </remarks>
    public async Task KillAsync(int which)
    {
        await _machines[which].DisposeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        foreach (var handle in _handles) handle.Dispose();

        foreach (var bus in _machines) await bus.DisposeAsync().ConfigureAwait(false);

        await _asking.DisposeAsync().ConfigureAwait(false);
    }
}
