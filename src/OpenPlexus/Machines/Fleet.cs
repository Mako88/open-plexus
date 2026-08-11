using System.Collections.Immutable;
using System.Diagnostics;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;

namespace OpenPlexus.Machines;

/// <summary>
/// The learning loop with the commitments on other machines — <b>fork 52's transport
/// carrying a run rather than a demonstration.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>TWO ROUND TRIPS A ROUND, AND WHICH TWO IS THE DESIGN.</b> The vote is a scatter and a
/// gather because the round cannot be scored until it comes back; the settlement is a
/// scatter and a gather because a holder has to be told what happened and the asker has to
/// know how many were told. Everything between those — settling, sweeping, covering,
/// repairing — happens on the machine that holds the commitment, which is why there is no
/// third exchange for any of it.
/// </para>
/// <para>
/// <b>AND A THIRD ONE ON A SWEEP ROUND, WHICH IS ONE ROUND IN A THOUSAND.</b> Abstraction
/// is the only operator in <see cref="Population"/> whose statistic is the whole
/// population's, so it is the only thing a holder cannot decide by itself — measured in
/// <c>SplitNamingTests</c>, where three shards holding thirty-six eligible scopes each name
/// nothing at all.
/// </para>
/// <para>
/// <b>IT WAITS FOR EVERYONE STILL OWED, AND WHAT IS OWED COMES DOWN — fork 53, and the
/// signal is not a clock.</b> A holder the question could not be handed to is written off by
/// <see cref="Gathering.WriteOff"/>, so a fleet that has lost a machine finishes its rounds
/// on the arrivals it can still expect rather than stopping here forever. What a deadline
/// would have guessed at is instead observed: the sender watched the ask fail to leave, and
/// a machine that was never given a question cannot answer it.
/// </para>
/// <para>
/// <b>AND THE ROUND A HOLDER DIES INSIDE IS REACHED BY SLOTS AND BY NOTHING ELSE — fork
/// 62.</b> A holder that took the question and went before answering is indistinguishable
/// from a slow one under C2, and nothing but a deadline separates them — <i>a miss decided
/// by a deadline</i> carries a revival row saying never. So no observation ends that wait,
/// and what ends it instead is not needing the machine: a slot holding R identical
/// populations completes when any ONE of them speaks, which is a condition on the evidence
/// rather than on the clock. Handed in through <see cref="Asker"/>, and without it every
/// holder is a slot of one and this paragraph is the old one.
/// </para>
/// <para>
/// <b>NOTHING HERE HOLDS A COMMITMENT, WHICH IS C1 BEING STRUCTURAL.</b> The asker cannot
/// report what the fleet holds because it cannot know — residents, tables and names are
/// facts about machines it may only ask questions of. An experimenter outside the machine
/// assembles that, and always did.
/// </para>
/// </remarks>
public sealed class Fleet : ICouncil
{
    private readonly Asker _asker;
    private readonly CommittingSettings _dials;

    private IReadOnlySet<Code> _moment = new HashSet<Code>();

    private long _asking;
    private long _telling;
    private long _counting;

    /// <param name="asker">The machine that puts the questions.</param>
    /// <param name="dials">
    /// The brain's numbers. <b>Only the weighing is read here</b>, because merging what
    /// holders said is the one piece of arithmetic that happens off the holders.
    /// </param>
    public Fleet(Asker asker, CommittingSettings dials)
    {
        ArgumentNullException.ThrowIfNull(asker);
        ArgumentNullException.ThrowIfNull(dials);

        _asker = asker;
        _dials = dials;
    }

    /// <summary>How many holders the last exchange was put to.</summary>
    /// <remarks>
    /// <b>THE DENOMINATOR, WHICH IS THE WHOLE C3 INSTRUMENT.</b> A vote over eight
    /// survivors of twelve reads exactly like a vote over twelve without it, and
    /// <see cref="Population.Decide"/> cannot tell a silent holder from a dead one by
    /// design.
    /// </remarks>
    public int Asked { get; private set; }

    /// <summary>How many of them answered.</summary>
    public int Heard { get; private set; }

    /// <summary>Votes where more than one holder had something to advocate.</summary>
    /// <remarks>
    /// <b>WHETHER THE SPLIT WAS EVER LOAD-BEARING, WHICH NO SCORE CAN SAY.</b> A moment
    /// only one holder speaks on is merged identically by every rule there is, so a run
    /// full of those would agree perfectly with one process while combining nothing — and
    /// would read as the merge working. This is the count that separates a distributed run
    /// from a run that happened to be distributed.
    /// </remarks>
    public long Contested { get; private set; }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>THE ASKER'S CLOCK, WHICH IS THE NETWORK AND NOT THE MECHANISM.</b>
    /// <c>Firing</c> is how long the votes took to come back and <c>Settling</c> how long
    /// the tellings did; the phases a holder actually spends its time in are its own and
    /// stay there. Reading this beside a one-process <see cref="Spent"/> and calling the
    /// difference an overhead would be comparing a wait with a computation.
    /// </remarks>
    public Spent Spent => new()
    {
        Firing = Milliseconds(_asking),
        Settling = Milliseconds(_telling),
        Sweeping = Milliseconds(_counting),
        Covering = 0.0,
        Mending = 0.0,
    };

    /// <inheritdoc/>
    public async ValueTask<Vote> AskAsync(
        IReadOnlySet<Code> raw, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(raw);

        // REMEMBERED HERE AND SENT AGAIN WITH THE SETTLEMENT, so no holder has to keep
        // state keyed by a vote that C2 permits never to be followed up. The asker is the
        // one machine that certainly knows both halves of a round happened.
        _moment = raw;

        var at = Stopwatch.GetTimestamp();

        using var gathering = await _asker
            .AskAsync(Wanted.Vote, raw, ct).ConfigureAwait(false);

        await gathering.Everyone.ConfigureAwait(false);

        Asked = gathering.Asked;
        Heard = gathering.Heard;

        if (gathering.Spoke > 1) Contested++;

        var vote = gathering.Decide();

        Mark(ref _asking, at);

        return vote;
    }

    /// <inheritdoc/>
    public async ValueTask<Learnt> TellAsync(
        Code? arrived, bool wrong, bool sweeping, CancellationToken ct = default)
    {
        ImmutableArray<Tabled> counted = [];

        // THE TABLES ARE TAKEN BEFORE THE SETTLEMENT AND READ AFTER THE SUBSUMPTION, AND
        // THAT IS A DIFFERENCE FROM ONE PROCESS RATHER THAN A DEFECT. Alone, `Abstract`
        // counts its own scopes at the moment it runs, which is after `Subsume` has removed
        // whatever it removed; here every holder's table is a snapshot from one exchange
        // earlier. Everybody speaks from the same moment, which is the property that
        // matters -- a holder abstracting against tables taken at different instants would
        // be certifying a redundancy against a population that never existed.
        if (sweeping)
        {
            var counting = Stopwatch.GetTimestamp();

            using var tables = await _asker
                .AskAsync(Wanted.Counts, ct: ct).ConfigureAwait(false);

            await tables.Everyone.ConfigureAwait(false);

            counted = tables.Tables();

            Mark(ref _counting, counting);
        }

        var at = Stopwatch.GetTimestamp();

        using var told = await _asker
            .TellAsync(_moment, arrived, wrong, sweeping, counted, ct)
            .ConfigureAwait(false);

        await told.Everyone.ConfigureAwait(false);

        var learnt = told.Added();

        Mark(ref _telling, at);

        return learnt;
    }

    private static double Milliseconds(long ticks) =>
        ticks * 1000.0 / Stopwatch.Frequency;

    private static void Mark(ref long phase, long since) =>
        phase += Stopwatch.GetTimestamp() - since;
}
