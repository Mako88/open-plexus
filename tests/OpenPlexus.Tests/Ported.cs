using OpenPlexus.Bus;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;

namespace OpenPlexus.Tests;

/// <summary>
/// One asker and several holders, each in its own <see cref="Posted"/> on its own port.
/// </summary>
/// <remarks>
/// <para>
/// <b>SEPARATE BUSES AND SEPARATE PORTS IS WHAT MAKES A TEST DISTRIBUTED AT ALL.</b> Two
/// holders on one bus would share a dictionary, which is the arrangement every measurement
/// in this project has been taken under and the one <see cref="Posted"/> exists to stop
/// being the only one.
/// </para>
/// <para>
/// <b>THE ASKER OPENS FIRST AND THE ANNOUNCE-BACK IS WHY THAT IS SAFE.</b> A machine
/// announces when it opens and a peer that is not up yet drops it, so whoever opens first
/// would otherwise tell nobody where it is — and every answer to it would have nowhere to
/// go. <see cref="Posted"/> answers an announcement with one, so the roster converges
/// whatever order the fleet came up in.
/// </para>
/// <para>
/// <b>SHARED BECAUSE TWO FILES NOW BRING A FLEET UP AND THE COPIES WOULD DRIFT.</b>
/// <c>AskedTests</c> puts one exchange on a socket and <c>FleetTests</c> runs a whole
/// learner over one; a difference in how the fleet is composed would show up as a
/// difference the wire appeared to cause, which is exactly what <c>Fixture.Sharded</c>
/// already exists to prevent one layer down.
/// </para>
/// </remarks>
public sealed class Ported : IAsyncDisposable
{
    private readonly List<IDisposable> _handles = [];
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
    /// <b>THE ONE NUMBER THAT SEPARATES A RUN WHOSE ANSWER IS WRONG FROM A RUN WHOSE
    /// EVIDENCE NEVER ARRIVED.</b> A gathering waits for a denominator and nothing here
    /// decides a missing holder by a clock, so a lost message can stop a run forever on a
    /// fleet where every machine is alive and idle — and the only reading that says so is
    /// this one.
    /// </para>
    /// <para>
    /// <b>AND IT IS THE LOST ANSWER THAT DOES IT NOW, RATHER THAN ANY LOST MESSAGE.</b> Fork
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
    /// <b>THE COMING-UP LOSSES ARE REAL AND ARE NOT A FAULT, WHICH IS WHY THEY ARE
    /// SUBTRACTED RATHER THAN FIXED.</b> A machine announces itself to every peer as it
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
    /// <b>A DEADLOCK DETECTOR IN EXACTLY THE SENSE <see cref="Wired.ArrivedAsync"/> IS.</b>
    /// Nothing in the library may decide a missing holder by a clock — <i>a miss decided by
    /// a deadline</i> carries a revival row saying never — so a fleet that loses an ANSWER
    /// waits forever, correctly, and a suite that inherited that would hang rather than
    /// fail. This is generous enough to be uninteresting and is asserted on by nothing.
    /// </remarks>
    public static readonly TimeSpan Patience = TimeSpan.FromMinutes(10);

    /// <summary>Brings up an asker and one holder per population, and waits for the roster.</summary>
    /// <param name="holding">What each holder holds, already built.</param>
    public static async Task<Ported> OpenAsync(IReadOnlyList<Population> holding)
    {
        ArgumentNullException.ThrowIfNull(holding);

        var fleet = new Ported();

        var hosts = Enumerable.Range(0, holding.Count + 1).Select(_ => Wired.Free()).ToList();
        var peers = hosts.Select(one => new Peer(one)).ToList();

        fleet._asking = new Posted(hosts[0], peers);
        fleet.Asker = new Asker(new MachineAddress("asker"), fleet._asking);
        fleet._handles.Add(fleet._asking.Subscribe(fleet.Asker));

        for (var at = 0; at < holding.Count; at++)
        {
            var bus = new Posted(hosts[at + 1], peers);
            var holder = new Holder(new MachineAddress($"holder-{at}"), holding[at], bus);

            fleet._handles.Add(bus.Subscribe(holder));
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

        // AND ONE ASK THROWN AWAY, WHICH IS THE BARRIER THE ROSTER ALONE CANNOT BE. Knowing
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
    /// <b>THE PLACEMENT IS THE ONLY THING THAT MAKES THESE MACHINES DIFFERENT.</b> Every
    /// holder sees every observation and runs the identical round, so without
    /// <see cref="Population.Places"/> a fleet of twelve would hold twelve copies of one
    /// population — the same rules minted by every machine that was surprised, which is
    /// not a shard and is not a distribution.
    /// </para>
    /// <para>
    /// <b>AND IT IS <c>Fixture.Sharded</c>'S RULE RATHER THAN A NEW ONE</b>, so a fleet
    /// that learnt its population and a fleet handed one already trained are split the
    /// same way — otherwise a difference between the two arrangements would be a
    /// difference in how the ring was drawn.
    /// </para>
    /// <para>
    /// <b>AND <see cref="Population.Placing"/> IS LEFT NULL, WHICH IS NOT AN
    /// OVERSIGHT.</b> That predicate exists to SIMULATE sharding inside one process, by
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
    /// One holder's machine closes its door — <b>C3, and not a polite departure.</b>
    /// </summary>
    /// <param name="which">Which holder dies.</param>
    /// <remarks>
    /// <b>THE WHOLE MACHINE RATHER THAN THE SUBSCRIPTION, BECAUSE THOSE ARE DIFFERENT
    /// DEATHS.</b> Dropping a subscription leaves a listener that accepts the ask and
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
