using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Machines;

/// <summary>What both examinations of the crossing world came to.</summary>
/// <remarks>
/// <b>Two numbers, and neither is readable alone.</b> A shape that does not survive an unseen
/// offset fails both, so a crossing score with no position score beside it cannot tell a
/// learner that failed to bind from a front end that never carried the word.
/// </remarks>
public sealed record Crossings
{
    /// <summary>The whole run, whose <see cref="Tally.Unseen"/> is the crossing exam.</summary>
    public required Tally Learnt { get; init; }

    /// <summary>The position exam: which WORD is this, drawn where it never was.</summary>
    public required Examined? Placed { get; init; }
}

/// <summary>
/// The crossing world, learnt through a body with two senses — <b>fork 107's runner.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A <see cref="Compound{TFrame}"/> and not two machines</b>, because a moment is what
/// puts codes together. A camera on one machine and a reader on another would never co-fire,
/// and co-firing is the whole of what this world presents.
/// </para>
/// <para>
/// <b>Four pixels a patch, and `LetteringTests` is why.</b> Three, six and eight separate no
/// drawn word at any conjunction depth, so a run at one of those would be measuring a front
/// end that cannot carry the question. The size is the join's to choose and it is chosen
/// here rather than in the world.
/// </para>
/// </remarks>
public sealed class CrossingRun
{
    /// <summary>How many pixels across one patch is.</summary>
    public const int Patch = 4;

    private readonly Bench _trial;
    private readonly Bench _placing;

    /// <param name="world">The shape of the world.</param>
    /// <param name="brain">The one brain, already configured.</param>
    /// <param name="seed">The world's own generator.</param>
    public CrossingRun(CrossingSettings world, Brain brain, int seed)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(brain);

        var made = new Crossing(world, seed);

        var body = new Compound<Crossed>(
        [
            new Tiling(Crossing.Shape, Lettering.Side, Patch),
            new Passthrough(),
        ]);

        _trial = new Bench(new Watching<Crossed>(made, body), brain);

        // The same brain and the same body, over a view whose withheld set is the other
        // exam. It never runs -- `Examine` asks the population and does not advance a world
        // -- so this costs one wrapper rather than a second run of anything.
        _placing = new Bench(new Watching<Crossed>(new Placing(made), body), brain);
    }

    /// <summary>Runs the world and learns from it, then takes both examinations.</summary>
    /// <param name="rounds">How many rounds.</param>
    /// <param name="sweep">How often to subsume, abstract and cull.</param>
    /// <param name="target">The trailing accuracy to wait for.</param>
    /// <param name="window">How many answered predictions that accuracy is over.</param>
    public Crossings Run(long rounds, int sweep = 1000, double target = 0.9, int window = 2000)
    {
        var learnt = _trial.Run(rounds, sweep, target, window);

        return new Crossings { Learnt = learnt, Placed = _placing.Examine() };
    }

    /// <summary>One world seen through its other examination.</summary>
    /// <param name="world">The world, which this neither advances nor owns.</param>
    /// <remarks>
    /// <b>A view rather than a second world</b>, so both exams are drawn at the identical
    /// offset from the identical vocabulary. Two worlds built from one seed would agree
    /// today and diverge the first time either gained a draw.
    /// </remarks>
    private sealed class Placing(Crossing world) : IWorld<Crossed>, IWithholds<Crossed>
    {
        /// <inheritdoc/>
        public Turn<Crossed> Next() => world.Next();

        /// <inheritdoc/>
        public int Outcomes => world.Outcomes;

        /// <inheritdoc/>
        public IReadOnlyList<Turn<Crossed>> Withheld => world.Moved;
    }
}
