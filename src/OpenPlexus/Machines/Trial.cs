using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Worlds;

namespace OpenPlexus.Machines;

/// <summary>What a trial did, in terms every world shares.</summary>
/// <remarks>
/// <b>NOTHING WORLD-SPECIFIC LIVES HERE.</b> An answer key, a soundness check, a count
/// of true rules — those are facts about one problem, and a world is asked for them
/// separately. A shared report that grew a field per world would be the mixing this
/// arrangement exists to prevent.
/// </remarks>
public sealed record Tally
{
    /// <summary>Rounds run.</summary>
    public required long Rounds { get; init; }

    /// <summary>Predictions that matched what followed.</summary>
    public required long Right { get; init; }

    /// <summary>Predictions that did not.</summary>
    public required long Wrong { get; init; }

    /// <summary>Rounds where nothing fired, so there was no prediction to be wrong.</summary>
    public required long Silent { get; init; }

    /// <summary>The share of answered predictions right over the last tenth.</summary>
    public required double Recent { get; init; }

    /// <summary>The round a trailing window first held the target, or zero if never.</summary>
    public required long Reached { get; init; }

    /// <summary>Children minted by repair.</summary>
    public required long Repaired { get; init; }

    /// <summary>Commitments resident at the end.</summary>
    public required int Resident { get; init; }

    /// <summary>Codes minted to stand for sub-scopes that kept recurring.</summary>
    public required int Named { get; init; }

    /// <summary>Names that stand for a set containing another name.</summary>
    public required int Stacked { get; init; }

    /// <summary>Commitments that have spent their whole repair budget.</summary>
    public required int Exhausted { get; init; }

    /// <summary>How many codes one round produced, on average.</summary>
    /// <remarks>
    /// <b>The cost side of a front end.</b> One allowed to say four times as much has
    /// four times as much to search, so a score without this rewards whoever talks more.
    /// </remarks>
    public required double Codes { get; init; }
}

/// <summary>
/// A world, a translation, and the brain — joined here and nowhere else.
/// </summary>
/// <typeparam name="TSeen">Whatever the world natively produces.</typeparam>
/// <remarks>
/// <para>
/// <b>THE SEAM IS ONE CALL WIDE IN EACH DIRECTION.</b> A world says what happened in
/// its own terms; a quantiser turns that into codes; the brain learns from codes. No
/// world knows a brain exists, and the brain knows nothing about where its codes came
/// from — which is what lets the SAME brain, configured once, run every world.
/// </para>
/// <para>
/// <b>THE TRANSLATION IS CHOSEN HERE, WHICH IS NEITHER SIDE'S BUSINESS TO DECIDE.</b>
/// Whether a reading is banded or winnowed is a fact about the pipe. Putting that
/// choice inside a world is how a world starts deciding what the brain perceives, and
/// putting it inside the brain is how the brain starts knowing about worlds.
/// </para>
/// </remarks>
public sealed class Trial<TSeen>
{
    private readonly IWorld<TSeen> _world;
    private readonly IQuantizer<TSeen> _sensing;
    private readonly Brain _brain;

    /// <param name="world">The problem.</param>
    /// <param name="sensing">The translation between it and the brain.</param>
    /// <param name="brain">The one brain, already configured.</param>
    public Trial(IWorld<TSeen> world, IQuantizer<TSeen> sensing, Brain brain)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(sensing);
        ArgumentNullException.ThrowIfNull(brain);

        _world = world;
        _sensing = sensing;
        _brain = brain;
    }

    /// <summary>What a blind guess scores on this world.</summary>
    public double Chance => 1.0 / _world.Outcomes;

    /// <summary>Runs the world through the translation into the brain.</summary>
    /// <param name="rounds">How many rounds.</param>
    /// <param name="sweep">How often to subsume, abstract and cull.</param>
    /// <param name="target">The trailing accuracy <see cref="Tally.Reached"/> waits for.</param>
    /// <param name="window">How many answered predictions that accuracy is over.</param>
    public Tally Run(long rounds, int sweep = 1000, double target = 0.9, int window = 2000)
    {
        var held = _brain.Held;
        var cycle = new Cycle(held, rounds, sweep, target, window);

        long codes = 0;

        for (long round = 0; round < rounds; round++)
        {
            var turn = _world.Next();

            var said = _sensing.Codify(turn.Seen);
            codes += said.Count;

            cycle.Step(held.Moment(new HashSet<Code>(said)), Brain.Says(turn.Outcome));
        }

        return new Tally
        {
            Rounds = rounds,
            Right = cycle.Right,
            Wrong = cycle.Wrong,
            Silent = cycle.Silent,
            Recent = cycle.Recent,
            Reached = cycle.Reached,
            Repaired = cycle.Repaired,
            Resident = held.Count,
            Named = held.Names.Count,
            Stacked = held.Names.Means.Count(one => one.Value.Any(held.Names.Knows)),
            Exhausted = held.Exhausted(_brain.Dials.Budget),
            Codes = codes / (double)rounds,
        };
    }
}
