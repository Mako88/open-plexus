using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Machines;

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
public sealed class Watching<TSeen> : IInput, IExamines
{
    private readonly IWorld<TSeen> _world;
    private readonly IQuantizer<TSeen> _sensing;
    private readonly Func<IReadOnlyCollection<Code>, int?>? _acting;

    private long _sequence;

    /// <param name="world">The problem.</param>
    /// <param name="sensing">The translation between it and the brain.</param>
    /// <param name="source">Which stream this is, where a body holds more than one.</param>
    /// <param name="acting">
    /// What to do about the state the world is in, given the codes it reads as —
    /// <b>required of a world that can be acted in</b>, and refused of one that cannot.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>A delegate rather than a policy type</b>, because a sense may not know which world
    /// it is reading. A chooser that named <c>Homeostat</c> would put one world's vocabulary
    /// in front of every other one.
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
        Func<IReadOnlyCollection<Code>, int?>? acting = null)
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
    public IReadOnlyList<Question> Exam =>
        _world is not IWithholds<TSeen> withholding
            ? []
            :
            [
                .. withholding.Withheld
                    .Where(one => one.Outcome is not null)
                    .Select(one => new Question
                    {
                        Codes = new HashSet<Code>(Sensed(one.Seen)),
                        Followed = Brain.Says(one.Outcome!.Value),
                    }),
            ];

    /// <inheritdoc/>
    /// <remarks>
    /// <b>A pulled world always has something</b>, so this never returns nothing. What a
    /// quiet sense looks like is answered by whatever implements this over a real sensor.
    /// </remarks>
    public Pushed? Push()
    {
        // Chosen in the state the world is in, and spent by the step that follows. The
        // chooser reads `Now` through the same front end the learner reads its moments
        // through, so what it is allowed to see is exactly what the learner is allowed to
        // see -- an oracle that read the world's own terms would be a fourth channel
        // nobody declared.
        if (_world is IActed<TSeen> acted) acted.Do(_acting!(Sensed(acted.Now)));

        var turn = _world.Next();

        return new Pushed
        {
            From = new Stamp { Source = Source, Sequence = _sequence++ },
            Codes = new HashSet<Code>(Sensed(turn.Seen)),

            // Read here and forwarded whole, because it is the world's claim about its own
            // codes rather than a translation of them. `Sensed` derives precedences and
            // intervention codes and neither is fleeting -- a derived code is as durable as
            // what it was derived from.
            Fleeting = _sensing.Fleeting(turn.Seen),
            Followed = turn.Outcome is { } outcome ? Brain.Says(outcome) : null,
        };
    }

    /// <summary>
    /// One observation as the codes a machine broadcasts for it — <b>the front end's
    /// reading</b>, plus whatever rung three derives from the order it reported.
    /// </summary>
    /// <param name="seen">What the world showed.</param>
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
    private IReadOnlyCollection<Code> Sensed(TSeen seen)
    {
        var said = _sensing.Codify(seen);

        var order = _sensing.Order(seen) is { Count: > 1 } reported ? reported : null;
        var forced = _sensing.Forced(seen) is { Count: > 0 } assigned ? assigned : null;

        if (order is null && forced is null) return said;

        var carried = new HashSet<Code>(said);

        if (order is not null)
            foreach (var precedence in Sequenced.From(order)) carried.Add(precedence);

        // And what was DONE rather than seen, on the same seam and for the same reason. The
        // channel has reported it since the day it was written and nothing read it, so a
        // scope naming a code the learner chose and one naming the code the world drew were
        // the same scope with their evidence added together -- which is `P(y | x)` standing
        // in for `P(y | do(x))`, and no amount of counting the first yields the second.
        if (forced is not null)
            foreach (var doing in Intervened.From(forced)) carried.Add(doing);

        return carried;
    }
}
