using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Worlds;

namespace OpenPlexus.Machines;

/// <summary>
/// What the population said about observations the world never drew.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE ONLY NUMBER ON A PERCEPTUAL WORLD THAT SEPARATES LEARNING FROM
/// MEMORISING.</b> Where a world's true rule set can be enumerated, soundness answers
/// that question exactly and this is redundant. Where it cannot — which is every world
/// made of photographs, forever — a trailing accuracy over a bag drawn with
/// replacement is indistinguishable from a lookup table, and this is the whole of the
/// difference.
/// </para>
/// <para>
/// <b>AND THE SILENCE IS REPORTED BESIDE THE SCORE RATHER THAN FOLDED INTO IT.</b> A
/// population that fires on nothing it has not seen scores an excellent
/// <see cref="Accuracy"/> over the handful it does answer, which is a fallback arm
/// nobody meant to run. Both halves or neither.
/// </para>
/// </remarks>
public sealed record Examined
{
    /// <summary>Withheld observations put to the population.</summary>
    public required int Asked { get; init; }

    /// <summary>How many of them anything fired on.</summary>
    public required int Answered { get; init; }

    /// <summary>How many of those were right.</summary>
    public required int Right { get; init; }

    /// <summary>The share of answered predictions that were right.</summary>
    public double Accuracy => Answered == 0 ? 0.0 : Right / (double)Answered;

    /// <summary>The share of withheld observations nothing fired on.</summary>
    public double Silence => Asked == 0 ? 0.0 : (Asked - Answered) / (double)Asked;
}

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

    /// <summary>
    /// How much of the winner's weight its lead over the runner-up accounted for.
    /// </summary>
    /// <remarks>
    /// <b>ARMED HERE FOR THE FIRST TIME, AND THE PLAN SAID IT ALREADY WAS.</b> The
    /// margin has been computed every round for the life of the branch and read by
    /// nothing — see <see cref="Commitments.Cycle.Confidence"/>. Near nought it says the
    /// answer is being settled by how many advocates each side had rather than by how
    /// accurate any of them is, which is the one failure the vote's whole shape exists
    /// to prevent and the one thing no score reports.
    /// </remarks>
    public required double Confidence { get; init; }

    /// <summary>The round a trailing window first held the target, or zero if never.</summary>
    public required long Reached { get; init; }

    /// <summary>Children minted by repair.</summary>
    public required long Repaired { get; init; }

    /// <summary>Commitments minted by genesis, before anything culled them.</summary>
    /// <remarks>
    /// <b>The rate genesis ran at, which <see cref="Resident"/> cannot show.</b> A
    /// population held at capacity looks identical whether covering minted two hundred
    /// commitments or two hundred thousand, and the difference between those is the
    /// difference between learning and enumerating.
    /// </remarks>
    public required long Minted { get; init; }

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

    /// <summary>
    /// What it said about what it was never shown, or nothing if the world showed
    /// everything.
    /// </summary>
    /// <remarks>
    /// <b>NOTHING RATHER THAN ZERO WHERE A WORLD WITHHOLDS NOTHING.</b> A generated
    /// world cannot contain its own answer and has nothing to hold back, so a zero here
    /// would read as a learner that generalises to nothing at all — which is a check
    /// that cannot fire reading as a failure instead of as absent.
    /// </remarks>
    public required Examined? Unseen { get; init; }
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
    /// <remarks>
    /// <b>The held-out examination is taken at the END and is also callable at any
    /// point</b> — see <see cref="Examine"/>. A curve over it says something a single
    /// endpoint cannot: whether the gap to the drawn bag OPENS as the population fills,
    /// which is what memorising looks like from outside.
    /// </remarks>
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
            Confidence = cycle.Confidence,
            Reached = cycle.Reached,
            Repaired = cycle.Repaired,
            Minted = cycle.Minted,
            Resident = held.Count,
            Named = held.Names.Count,
            Stacked = held.Names.Means.Count(one => one.Value.Any(held.Names.Knows)),
            Exhausted = held.Exhausted(_brain.Dials.Budget),
            Codes = codes / (double)rounds,
            Unseen = Examine(),
        };
    }

    /// <summary>
    /// Asks the population about observations the world never drew, and teaches it
    /// nothing.
    /// </summary>
    /// <returns>What it said, or nothing where the world withholds nothing.</returns>
    /// <remarks>
    /// <para>
    /// <b>EVERY CALL HERE IS READ-ONLY AND THAT IS LOAD-BEARING RATHER THAN
    /// INCIDENTAL.</b> <see cref="Population.Moment"/> folds names without minting one,
    /// <see cref="Population.Firing"/> gathers candidates without touching them, and
    /// <see cref="Population.Predict"/> reads accuracies it does not write.
    /// <c>Settle</c>, <c>Cover</c> and <c>Mend</c> are the three that teach and none of
    /// them is called — an examination that moved a single counter would be a second
    /// training run wearing the word <i>held-out</i>, and the number would be worth
    /// less than no number.
    /// </para>
    /// <para>
    /// <b>AND C4 IS UNTOUCHED, WHICH IS THE POINT THAT WAS BEING MISSED.</b> The
    /// constraint is that the MACHINE may not depend on an episode boundary. It does
    /// not: nothing here is fed back, the run does not pause, and the population cannot
    /// tell this happened. The person reading the number is outside the machine, and
    /// always was.
    /// </para>
    /// </remarks>
    public Examined? Examine()
    {
        if (_world is not IWithholds<TSeen> withholding) return null;

        var held = _brain.Held;

        var answered = 0;
        var right = 0;

        foreach (var turn in withholding.Withheld)
        {
            var moment = held.Moment(new HashSet<Code>(_sensing.Codify(turn.Seen)));

            if (held.Predict(held.Firing(moment)).Expects is not { } said) continue;

            answered++;
            if (said == Brain.Says(turn.Outcome)) right++;
        }

        return new Examined
        {
            Asked = withholding.Withheld.Count,
            Answered = answered,
            Right = right,
        };
    }
}
