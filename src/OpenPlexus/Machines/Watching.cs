using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Machines;

/// <summary>
/// What the front end said, in terms every front end shares.
/// </summary>
/// <remarks>
/// <para>
/// <b>The seam's own account rather than each quantiser's.</b> Thirteen front ends would be
/// thirteen places to instrument and thirteen places to forget; what every one of them has in
/// common is the four channels of <see cref="Codes.IQuantizer{TObservation}"/> and the two
/// derivations the join makes out of them. So this counts what crossed the seam, and a front
/// end wanting to report its own internals reports them itself.
/// </para>
/// <para>
/// <b>Pushed moments only.</b> A withheld observation is read through the same front end and
/// is not a moment the run had, so counting it here would make an examination look like
/// perception. A chooser's reading of the state it is about to act on is left out for the
/// same reason: it is the same moment, asked about twice.
/// </para>
/// </remarks>
internal sealed record Fronted
{
    /// <summary>Moments read.</summary>
    /// <remarks>
    /// <b>The denominator the four below want</b>, and it is not <see cref="Tally.Rounds"/>.
    /// That one is what the bench asked for; this is what the front end was given, and the
    /// two come apart the moment a source is allowed to be quiet.
    /// </remarks>
    public required long Moments { get; init; }

    /// <summary>Codes the quantiser said were present, added up.</summary>
    /// <remarks>
    /// <b>Before the join derives anything</b>, which is what separates it from
    /// <see cref="Tally.Codes"/>. That one is the moment the brain was handed and this is the
    /// part of it the signal accounts for, so the difference between them is what rung three
    /// and the intervention codes cost.
    /// </remarks>
    public required long Said { get; init; }

    /// <summary>Precedence codes derived from the order the front end reported.</summary>
    public required long Ordered { get; init; }

    /// <summary>Intervention codes derived from what the front end said was forced.</summary>
    public required long Doings { get; init; }

    /// <summary>Departure codes derived from what was live in the moment before.</summary>
    /// <remarks>
    /// <b>Read against <see cref="Said"/> as the others are</b>, and it is the one that says
    /// what an absence COSTS. A world whose moment is a sentence has every word of the last
    /// one depart, so this landing near <see cref="Said"/> is the moment doubling — which is
    /// the price the dial is measured against rather than a fault.
    /// </remarks>
    public required long Leaving { get; init; }

    /// <summary>Codes the front end said name this occasion and cannot recur.</summary>
    public required long Fleeting { get; init; }

    /// <summary>Codes the front end put in one of the moment's things.</summary>
    /// <remarks>
    /// <para>
    /// <b>Codes rather than things</b>, so it reads against <see cref="Said"/> and says what
    /// share of a moment the front end could place. How many things there were is a fact
    /// about the scene; how much of the scene is IN one is a fact about the front end, and
    /// that is the one a reader needs to tell a segmenting world from a segmented moment.
    /// </para>
    /// <para>
    /// <b>And DISTINCT codes</b>, now that a code can be in two things at once. Counting a
    /// code once a thing would make this a count of memberships against a count of codes,
    /// which is the share whose halves count different events and announces itself by
    /// exceeding one. The multiplicity is in the parts and this is not where to read it.
    /// </para>
    /// </remarks>
    public required long Grouped { get; init; }
}

/// <summary>
/// What the chooser did, or nothing where the world cannot be acted in.
/// </summary>
/// <remarks>
/// <b>Nothing rather than zero</b>, on <see cref="Examined"/>'s rule. A world nobody may act
/// in reporting nought doings reads exactly like a chooser that never found anything to say,
/// and those are opposite readings.
/// </remarks>
internal sealed record Chosen
{
    /// <summary>Doings said.</summary>
    public required long Doings { get; init; }

    /// <summary>Moments the chooser had nothing to say, so the world was told so.</summary>
    public required long Quiet { get; init; }

    /// <summary>
    /// Moments the chooser spoke more than once — <b>the conversation, as a number.</b>
    /// </summary>
    /// <remarks>
    /// <b>One doing a moment was a real ceiling</b>, and the loop that lifted it is reachable
    /// only where a world keeps listening. A machine that speaks once cannot ask, hear
    /// <i>no</i>, and ask again, so every reading of whether asking pays was taken under that
    /// ceiling — and a run reporting nought here is a run that was still under it.
    /// </remarks>
    public required long Again { get; init; }
}

/// <summary>An input that can say what its own front end and chooser did.</summary>
/// <remarks>
/// <b>Asked of the input rather than computed by the bench</b>, because the bench sees a
/// moment and never the channels it was made of. <see cref="IExamines"/> is the same
/// arrangement one seam along: a question only some inputs can answer arrives as an interface
/// some of them implement.
/// </remarks>
internal interface IReports
{
    /// <summary>What its front end said.</summary>
    Fronted Fronted { get; }

    /// <summary>What its chooser did, or nothing where there is no chooser.</summary>
    Chosen? Chosen { get; }
}

/// <summary>A pulled world read through a front end, as one sense.</summary>
/// <typeparam name="TSeen">Whatever the world natively produces.</typeparam>
/// <remarks>
/// <para>
/// <b>The join, and it is a third thing rather than either side.</b> A world says what
/// happened in its own terms and a brain only ever sees codes, so the translation belongs
/// here — which is also why this is where rung three's precedences and the intervention
/// codes are derived. Neither is a fact about the signal and neither is a setting on the
/// brain.
/// </para>
/// <para>
/// <b>And it is what lets a pulled world push.</b> <see cref="IWorld{TSeen}.Next"/> has to
/// be called, so something has to call it; doing that here means every world on the branch
/// reaches the new seam without being rewritten, and the ones that keep their own schedule
/// arrive as a second implementation of <see cref="IInput"/> rather than as a second bench.
/// </para>
/// </remarks>
internal sealed class Watching<TSeen> : IInput, IExamines, IReports
{
    private readonly IWorld<TSeen> _world;
    private readonly IQuantizer<TSeen> _sensing;
    private readonly IChooses? _acting;
    private readonly Departing _departing;

    /// <summary>What the last moment held, before any departure was derived into it.</summary>
    /// <remarks>
    /// <b>What a SENSE said and not what the join built</b>, so a departure is never derived
    /// from a departure. Keeping the finished moment would let the alphabet grow a level every
    /// round, which is the same rule that stops a precedence taking a precedence.
    /// </remarks>
    private IReadOnlySet<Code>? _last;

    private long _sequence;

    private long _said, _ordered, _doings, _fleeting, _grouped, _leaving;
    private long _chosen, _quiet, _again;

    /// <param name="world">The problem.</param>
    /// <param name="sensing">The translation between it and the brain.</param>
    /// <param name="source">Which stream this is, where a body holds more than one.</param>
    /// <param name="acting">
    /// What to do about the state the world is in, given the codes it reads as —
    /// <b>required of a world that can be acted in</b>, and refused of one that cannot.
    /// </param>
    /// <param name="departing">
    /// Whether a moment carries what has just stopped being live. <b>Here rather than on the
    /// brain</b>, because it decides what a moment IS and the join is where every other
    /// derivation of one lives — a brain dial would be the world reaching in one level out.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>An interface that names no world</b>, because a sense may not know which world it
    /// is reading. A chooser that named <c>Homeostat</c> would put one world's vocabulary in
    /// front of every other one, and <see cref="Chooses.From"/> is there for the arms that
    /// really are one expression.
    /// </para>
    /// <para>
    /// <b>An acted world with no chooser is refused</b> rather than left doing nothing, which
    /// is this repo's own trap about a fallback being a control arm nobody meant to run.
    /// Doing nothing is an arm on a body whose variables fall unattended — it is the fastest
    /// way to fail, and a run that took it by omission would report a dead body as a
    /// learner's score. Asking for it is a chooser that returns nothing.
    /// </para>
    /// <para>
    /// <b>The chooser reads codes and never the world's own terms</b>, so it sits on the same
    /// seam the brain sits on. An oracle is then a chooser that was handed the answer, a
    /// control is one that draws uniformly, and a learner is one that reads a population —
    /// three arms over one interface rather than three kinds of bench.
    /// </para>
    /// </remarks>
    public Watching(
        IWorld<TSeen> world,
        IQuantizer<TSeen> sensing,
        byte source = Stamp.First,
        IChooses? acting = null,
        Departing departing = Departing.Left)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(sensing);

        if (world is IActed<TSeen> && acting is null)
            throw new ArgumentNullException(nameof(acting),
                "this world is acted in and nothing was given to act with, so every round "
                + "would do nothing -- which is an arm rather than an absence. Pass a chooser, "
                + "and pass one returning null if doing nothing is the arm wanted");

        if (world is not IActed<TSeen> && acting is not null)
            throw new ArgumentException(
                "this world cannot be acted in, so a chooser would never be asked and its "
                + "arm would read as having run", nameof(acting));

        _world = world;
        _sensing = sensing;
        _acting = acting;
        _departing = departing;

        Source = source;
    }

    /// <inheritdoc/>
    public byte Source { get; }

    /// <inheritdoc/>
    public int Outcomes => _world.Outcomes;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Nothing where the world withholds nothing</b>, and a world that can withhold and
    /// is not withholding reports empty rather than absent. An examination of nothing
    /// answers nothing, so every count in it is nought and the accuracy with them — which
    /// reads as a population that generalises to nothing rather than as a question nobody
    /// asked.
    /// </remarks>
    /// <remarks>
    /// <b>And the withheld set is a STREAM rather than a bag</b>, which is what lets a
    /// question carry what has just left. A world draws its withheld turns consecutively, so
    /// each one has the one before it as a predecessor and a departure is derivable exactly as
    /// it is on the live stream. Reading them as unrelated would put the examination in a
    /// different alphabet from the run -- a scope naming a departure could never fire on it,
    /// and the arm would read at its control's score for a reason that has nothing to do with
    /// generalising.
    /// </remarks>
    public IReadOnlyList<Question> Exam
    {
        get
        {
            if (_world is not IWithholds<TSeen> withholding) return [];

            var asked = new List<Question>();

            // What a SENSE said of the previous withheld turn, so the derivation is fed the
            // same thing it is fed on the live stream. Every turn advances it, including one
            // with no outcome -- a turn nobody may be asked about still happened, and skipping
            // it would make the question after it follow a moment that never preceded it.
            IReadOnlySet<Code>? before = null;

            foreach (var one in withholding.Withheld)
            {
                var codes = new HashSet<Code>(Sensed(one.Seen, out var raw, before: before));

                if (one.Outcome is { } outcome)
                    asked.Add(new Question
                    {
                        Codes = codes,
                        Followed = Brain.Says(outcome),
                        Grouping = _sensing.Bind(one.Seen),
                    });

                before = raw;
            }

            return asked;
        }
    }

    /// <inheritdoc/>
    public Fronted Fronted =>
        new()
        {
            Moments = _sequence,
            Said = _said,
            Ordered = _ordered,
            Doings = _doings,
            Fleeting = _fleeting,
            Grouped = _grouped,
            Leaving = _leaving,
        };

    /// <inheritdoc/>
    public Chosen? Chosen =>
        _acting is null
            ? null
            : new Chosen { Doings = _chosen, Quiet = _quiet, Again = _again };

    /// <inheritdoc/>
    /// <remarks>
    /// <b>A pulled world always has something</b>, so this never returns nothing. What a
    /// quiet sense looks like is answered by whatever implements this over a real sensor.
    /// </remarks>
    public Pushed? Push()
    {
        if (_world is IActed<TSeen> acted) Acting(acted);

        var turn = _world.Next();

        // Read here and forwarded whole, because it is the world's claim about its own codes
        // rather than a translation of them. `Sensed` derives precedences and intervention
        // codes and neither is fleeting -- a derived code is as durable as what it was
        // derived from.
        var passing = _sensing.Fleeting(turn.Seen);

        _fleeting += passing?.Count ?? 0;

        // Forwarded whole for the same reason and it is the channel that cannot be derived.
        // A precedence and an intervention become codes here because a code is what a fleet
        // broadcasts; a grouping cannot, and the derivation that tried it is refuted. So this
        // one travels beside the moment.
        var grouping = _sensing.Bind(turn.Seen);

        _grouped += grouping is null
            ? 0
            : grouping.SelectMany(part => part.Codes).Distinct().Count();

        var moment = new Pushed
        {
            From = new Stamp { Source = Source, Sequence = _sequence++ },
            Codes = new HashSet<Code>(
                Sensed(turn.Seen, out var raw, telling: true, before: _last)),
            Fleeting = passing,
            Grouping = grouping,
            Followed = turn.Outcome is { } outcome ? Brain.Says(outcome) : null,
        };

        // What a sense said, and never the moment that was built out of it. A departure
        // derived from a departure would grow the alphabet a level a round. Taken from the
        // read above rather than asked for again, because a front end may learn from what it
        // emits and a second ask is an observation the world never sent.
        _last = raw;

        return moment;
    }

    /// <summary>Everything the chooser has to say about the moment the world is in.</summary>
    /// <param name="acted">The world, in the state its next turn is the consequence of.</param>
    /// <remarks>
    /// <para>
    /// <b>For as long as both sides have something in it</b>, and one call was a real ceiling
    /// rather than a simplification. A machine that speaks once about a moment cannot ask,
    /// hear <i>no</i>, and ask again — so a refusal is worth nothing to it, and every reading
    /// of whether asking pays was taken on a machine that got one go.
    /// </para>
    /// <para>
    /// <b>Two conditions rather than a count, because the halves are different questions.</b>
    /// The chooser says whether it has anything more to say and the world says whether hearing
    /// it would change anything, and a loop that read only the first would collect answers a
    /// settled round has nowhere to put. The first doing is never asked about: every world
    /// takes one.
    /// </para>
    /// <para>
    /// <b>The state is re-read every time round</b>, because a doing is allowed to change it.
    /// Hoisting the reading out of the loop would show a chooser the world as it was before
    /// its own last action, which is the state <see cref="IActed{TSeen}.Now"/> exists to
    /// avoid handing anybody.
    /// </para>
    /// <para>
    /// <b>And a quiet moment is still told to the world</b>, exactly once. A world counts the
    /// rounds nothing was said about it, and a loop that simply broke would stop counting
    /// them — so the one call that used to happen still happens where nothing was said, and
    /// where something was the world has already heard it.
    /// </para>
    /// </remarks>
    private void Acting(IActed<TSeen> acted)
    {
        var said = 0;

        while (said == 0 || acted.Listening)
        {
            // The chooser reads `Now` through the same front end the learner reads its
            // moments through, so what it is allowed to see is exactly what the learner is
            // allowed to see -- an oracle that read the world's own terms would be a fourth
            // channel nobody declared.
            if (_acting!.Choose(Sensed(acted.Now)) is not { } doing) break;

            acted.Do(doing);

            said++;
        }

        if (said == 0) acted.Do(null);

        _chosen += said;
        _quiet += said == 0 ? 1 : 0;
        _again += said > 1 ? 1 : 0;

        _acting!.Cleared();
    }

    /// <summary>
    /// One observation as the codes a machine broadcasts for it — <b>the front end's
    /// reading</b>, plus whatever rung three derives from the order it reported.
    /// </summary>
    /// <param name="seen">What the world showed.</param>
    /// <param name="telling">
    /// Whether this reading is the moment being pushed, so the front-end census counts it.
    /// <b>An examination and a chooser’s look</b> are the same moment asked about again,
    /// and counting either would make one moment read as several.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>Where the moment is formed and not where it is matched</b>, which is a decision
    /// about the wire. A fleet broadcasts a moment as a set of codes, and a precedence is
    /// one — so deriving it here means it travels with everything else and no holder needs
    /// the order report beside the moment it already has. Doing it in
    /// <c>Population.Moment</c> would have put the front end's order on the wire.
    /// </para>
    /// <para>
    /// <b>And it is the machine that derives it, not the front end</b>, which is the seam
    /// that matters. <see cref="IQuantizer{TObservation}.Order"/> reports word order, which
    /// is a fact about the signal; turning it into <i>these two stood this way round</i> is a
    /// derivation, and a front end doing it would be deciding which relations exist. There
    /// is no dial: a front end reporting no order gets exactly the codes it always did.
    /// </para>
    /// </remarks>
    /// <param name="before">
    /// What a sense said of the moment before, or nothing where there is none.
    /// <b>Threaded rather than read off a field</b>, because the examination is a second
    /// stream: a withheld question's predecessor is the withheld question before it, and
    /// taking the live stream's last moment there would derive departures against a moment
    /// the question never followed.
    /// </param>
    private IReadOnlyCollection<Code> Sensed(
        TSeen seen, bool telling = false, IReadOnlySet<Code>? before = null) =>
        Sensed(seen, out _, telling, before);

    /// <summary>The same, handing back what the SENSE said before anything was derived.</summary>
    /// <param name="seen">One observation.</param>
    /// <param name="said">
    /// What the quantiser emitted, which is what a departure is derived against next time.
    /// <b>Handed back rather than asked for again</b>, and that is a correctness rule rather
    /// than a saving. A front end may LEARN from what it emits — <c>Deriving</c> counts the
    /// company its own codes keep — so calling it twice about one observation feeds the
    /// derivation an observation the world never sent, and the vocabulary a run ends with is
    /// then a fact about how often the seam asked.
    /// </param>
    /// <param name="telling">Whether this moment is one the run had.</param>
    /// <param name="before">What a sense said of the moment before.</param>
    private IReadOnlyCollection<Code> Sensed(
        TSeen seen,
        out IReadOnlySet<Code> said,
        bool telling = false,
        IReadOnlySet<Code>? before = null)
    {
        said = new HashSet<Code>(_sensing.Codify(seen));

        var order = _sensing.Order(seen) is { Count: > 1 } reported ? reported : null;
        var forced = _sensing.Forced(seen) is { Count: > 0 } assigned ? assigned : null;

        // What LEFT, and it needs the moment before rather than anything in this one. A
        // departure is the only derivation here that is about a change, which is why it is the
        // only one that could not have been written when the others were: nothing at this seam
        // remembered a moment until it had to.
        var leaving = _departing is Departing.Left && before is not null
            ? Departed.From(before, said).ToList()
            : null;

        if (telling) _said += said.Count;

        if (order is null && forced is null && leaving is not { Count: > 0 }) return said;

        var carried = new HashSet<Code>(said);

        if (leaving is not null)
            foreach (var gone in leaving)
            {
                carried.Add(gone);

                if (telling) _leaving++;
            }

        if (order is not null)
            foreach (var precedence in Sequenced.From(order))
            {
                carried.Add(precedence);

                if (telling) _ordered++;
            }

        // And what was DONE rather than seen, on the same seam and for the same reason. The
        // channel has reported it since the day it was written and nothing read it, so a
        // scope naming a code the learner chose and one naming the code the world drew were
        // the same scope with their evidence added together -- which is `P(y | x)` standing
        // in for `P(y | do(x))`, and no amount of counting the first yields the second.
        if (forced is not null)
            foreach (var doing in Intervened.From(forced))
            {
                carried.Add(doing);

                if (telling) _doings++;
            }

        return carried;
    }
}
