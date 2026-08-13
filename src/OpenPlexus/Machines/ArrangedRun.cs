using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Machines;

/// <summary>
/// A trial on the arranged world, plus the two things only that world can say.
/// </summary>
/// <remarks>
/// <b>Soundness is back, and on photons.</b> <see cref="Learned"/> carries the same
/// idea for the multiplexer, where a scope pins bits and the check is arithmetic. Here
/// a scope pins nothing — it names winners of a projection — so the question is asked
/// the only way it can be: code every scene the world admits, and ask whether the rule
/// ever disagrees with one. That is exact rather than sampled, which is what
/// <see cref="Cifar"/> could not have at any price.
/// </remarks>
public sealed record Grounded
{
    /// <summary>What the trial did, in terms every world shares.</summary>
    public required Tally Tally { get; init; }

    /// <summary>What the world says about the rules the trial left behind.</summary>
    /// <remarks>
    /// <b>Its own record, because a tally is what a run did and this is not.</b> They
    /// are gathered at different times by different code — one counts as the rounds go
    /// by, the other enumerates a world afterwards — and flattening them into one shape
    /// meant the grader had to hand back a half-built report with a <c>required</c>
    /// field holding a lie. `DuplicationTests` refused the copy, which was the right
    /// answer to the wrong shape.
    /// </remarks>
    public required Judged Rules { get; init; }

    /// <summary>
    /// How many distinct things the front end said, over how many readings.
    /// </summary>
    /// <remarks>
    /// <b>The collapse instrument, and this world is small enough for it to have a
    /// ceiling.</b> On CLEVR a projection over three numbers emitted one tag for four
    /// thousand objects, and nothing said so. Here what the front end could possibly
    /// distinguish is known and small — every scene for <see cref="Looking.Whole"/>,
    /// every distinct patch for <see cref="Looking.Tiled"/> — so a count far below it is
    /// a fact about the front end and not about the learner.
    /// </remarks>
    public required int Tags { get; init; }

    /// <summary>How many readings the front end was handed.</summary>
    public required long Readings { get; init; }
}

/// <summary>What the world says about the rules a run left behind.</summary>
public sealed record Judged
{
    /// <summary>Experienced commitments that no scene in the world contradicts.</summary>
    public required int Sound { get; init; }

    /// <summary>Experienced commitments some scene contradicts.</summary>
    public required int Unsound { get; init; }

    /// <summary>
    /// Experienced commitments that fire on no scene the world admits.
    /// </summary>
    /// <remarks>
    /// <b>Reported rather than counted as sound, which is the whole difference.</b> A
    /// rule contradicted by nothing because it applies to nothing is vacuously true, and
    /// folding those into <see cref="Sound"/> would let a population score by minting
    /// scopes that never fire. It is the same fault as counting a contradiction as
    /// sound, arriving from the other side.
    /// </remarks>
    public required int Inert { get; init; }

    /// <summary>How many scenes the check enumerated.</summary>
    public required int Layouts { get; init; }

    /// <summary>
    /// Unsound residents that structurally narrow a resident SOUND one.
    /// </summary>
    /// <remarks>
    /// <b>Subsumption's own test, minus the accuracy clause that gates it.</b> These say
    /// nothing their general parent does not, cover fewer moments, and are false of the
    /// world where the parent is true — so the design already wants them gone, and they
    /// are here. What is holding them is the <c>general.Accuracy >= specific.Accuracy</c>
    /// clause: a memorised child is PERFECTLY accurate on what it was shown, so the
    /// parent never quite beats it, and the one mechanism that prefers generality
    /// declines to fire exactly where generality is what is missing.
    /// </remarks>
    public required int Narrowed { get; init; }

    /// <summary>Unsound residents that narrow no resident sound one at all.</summary>
    /// <remarks>
    /// <b>And it is every one of them, which is not what was expected.</b> The guess was
    /// that these would be memorised children of sound parents that subsumption declined
    /// to absorb. <see cref="Narrowed"/> reads NOUGHT on both arms under both gates: the
    /// unsound residents are almost all ONE CODE, so there is nothing narrower about them
    /// and nothing for subsumption to fire on. They are general claims that happen to be
    /// false somewhere in the world.
    /// </remarks>
    /// <remarks>
    /// <b>So nothing in the mechanism set removes them, and nothing is supposed to.</b>
    /// Genesis is promiscuous by design and the vote is what is meant to handle a rule
    /// that is often wrong — <see cref="Commitments.Population.Cull"/> returns early
    /// below capacity, and this world never reaches it. Which puts the remaining gap
    /// squarely on the vote rather than on the population, and the plan named that
    /// failure before it happened: an expectation being worth its best advocate and no
    /// more is what stops a crowd of mediocre rules outvoting one that is always right.
    /// </remarks>
    public required int Rootless { get; init; }

    /// <summary>
    /// What the population BELIEVES about the rules that are true, against the rest.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The number that says whether the vote could possibly reach the gap, and it
    /// costs one pass.</b> An expectation is worth its best advocate, so a crowd of
    /// mediocre rules cannot outvote one that is always right at any scale — which only
    /// helps if the crowd LOOKS worse from inside.
    /// </para>
    /// <para>
    /// <b>And there is every reason to fear it does not.</b> A rule is unsound when the
    /// world contradicts it SOMEWHERE, and a fifth of this world is never drawn — so a
    /// rule wrong only about arrangements the learner has not been shown has a PERFECT
    /// observed record. Where these two numbers meet, no weighting of what the
    /// population knows can separate what is true from what merely has not been caught,
    /// and the answer is not a dial.
    /// </para>
    /// </remarks>
    public required double Trusted { get; init; }

    /// <summary>The same, for the residents the world contradicts.</summary>
    public required double Doubted { get; init; }

    /// <summary>How many codes a resident scope names, on average.</summary>
    /// <remarks>
    /// <b>The memorisation tell, and it is a distribution rather than a score.</b> This
    /// repo has already learnt once that the spread says what the mean cannot; a
    /// population drifting toward one rule per instance grows its scopes, and nothing
    /// else in <see cref="Tally"/> would show it.
    /// </remarks>
    public required double Scope { get; init; }
}

/// <summary>
/// What the dullest learner there is gets on the same exam.
/// </summary>
/// <remarks>
/// <para>
/// <b>The refutation table asks for this by name.</b> <i>A score on a trained front end
/// with no probe beside it — never; same features, same held-out set, or the bar is
/// decoration.</i> A population reaching some number on withheld arrangements answers
/// nothing on its own: it could be a fine front end under a weak learner or the reverse,
/// and one number cannot tell those apart.
/// </para>
/// <para>
/// <b>Two bars, because they answer different questions.</b>
/// <see cref="OnPixels"/> asks how much of this problem is there before any symbol is
/// manufactured — it is the same for every arm, so it is the world's difficulty rather
/// than a front end's. <see cref="OnCodes"/> asks what a linear model gets from the
/// EXACT codes the population reads, which is the only comparison that isolates the
/// learner.
/// </para>
/// <para>
/// <b>And the gap between them is what a front end is worth.</b> That is the grid step
/// four ran on CIFAR, arriving on a world where the answer depends on an arrangement —
/// which is the thing that grid could not ask about.
/// </para>
/// </remarks>
public sealed record Yardstick
{
    /// <summary>A linear probe on the raw scene, fitted on drawn and scored on withheld.</summary>
    public required Probed OnPixels { get; init; }

    /// <summary>The same probe on the codes this run's front end emits.</summary>
    public required Probed OnCodes { get; init; }

    /// <summary>How many features the coded probe was given.</summary>
    /// <remarks>
    /// <b>Beside the score, because a probe handed forty times as many features is a
    /// different probe.</b> A front end allowed to say more has more to be fitted on,
    /// and comparing two arms without this rewards whichever one talks most.
    /// </remarks>
    public required int Features { get; init; }
}

/// <summary>
/// What the CURRENT scope language could hold, if the learner were perfect.
/// </summary>
/// <remarks>
/// <para>
/// <b>The plan's own rule for extending the language is decidable and nobody had
/// decided it.</b> <i>The language extends when, and only when, no expression in the
/// current language separates the failures from the hits.</i> Until something computes
/// that, choosing the next rung is a guess about which construct sounds useful — which
/// is the hand-specified language bias the refutation table calls ILP's cause of death,
/// arriving through the one door the plan left open.
/// </para>
/// <para>
/// <b>So this enumerates the language rather than the learner.</b> Every scope up to
/// <see cref="Depth"/> codes is built, asked whether the world ever contradicts it, and
/// the sound ones are asked how much of the world they cover between them. A scene a
/// sound scope fires on is a scene a population of sound rules answers correctly,
/// because a sound rule is right wherever it fires.
/// </para>
/// <para>
/// <b>Which is a target and not a ceiling, and the difference cost a wrong sentence
/// here.</b> See <see cref="CoversUnseen"/>: a population of rules the world contradicts
/// can score ABOVE this by being right on average, and one of them does. What the number
/// bounds is what UNDERSTANDING the world would get you, which is the thing worth
/// wanting and is not the same as the thing worth scoring.
/// </para>
/// <para>
/// <b>And it separates the two excuses a disappointing score has.</b> Below the bound,
/// the learner is leaving something on the table that the language already affords. At
/// the bound, the language is the constraint and the failures have named the rung.
/// Without it those two are indistinguishable, and the repo has spent sessions on that
/// exact confusion in the other direction.
/// </para>
/// </remarks>
public sealed record Reached
{
    /// <summary>The widest scope this enumerated.</summary>
    public required int Depth { get; init; }

    /// <summary>How many scopes it built and tested.</summary>
    public required long Considered { get; init; }

    /// <summary>How many of those the world never contradicts.</summary>
    public required int Sound { get; init; }

    /// <summary>The share of every scene some sound scope fires on.</summary>
    public required double Covers { get; init; }

    /// <summary>
    /// The share of WITHHELD scenes some sound scope fires on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What a sound-only population would score, which is not a bound on every
    /// population.</b> A sound rule is right wherever it fires, so a population holding
    /// every sound rule and nothing else answers every covered scene correctly and
    /// abstains on the rest. That is exact, and it is the number worth aiming at.
    /// </para>
    /// <para>
    /// <b>And it is not a ceiling, which this said and was wrong about.</b> A rule the
    /// world contradicts SOMEWHERE can still be right on most of the held-out set, and a
    /// vote full of them can beat a sound-only population — measured, not argued: the
    /// whole-image arm scores 0.956 on the unseen against 0.877 here, over five seeds.
    /// A population above this line is not doing something impossible; it is being right
    /// on average with rules that are false of the world, which is worth knowing and is
    /// the opposite of the reassurance the word CEILING gives.
    /// </para>
    /// <para>
    /// <b>So it separates the two excuses and convicts neither on its own.</b> Far below
    /// it, the learner is leaving something the language already affords. At or above it
    /// with a population of mostly unsound rules, the score is not evidence that anything
    /// was understood.
    /// </para>
    /// </remarks>
    public required double CoversUnseen { get; init; }

    /// <summary>How many sound scopes a greedy cover needs.</summary>
    /// <remarks>
    /// <b>The size of the rule set the world actually wants, to put beside
    /// <see cref="Tally.Resident"/>.</b> The plan asks for a resident count near the
    /// true rule set and has never had the second number on a perceptual world. Greedy
    /// is an over-estimate of the minimum cover and is said to be one — the exact
    /// answer is set cover, and a bound nobody can compute is worse than a loose one
    /// everybody can.
    /// </remarks>
    public required int Least { get; init; }

    /// <summary>
    /// The codes that are sound ON THEIR OWN, which is what genesis mints.
    /// </summary>
    /// <remarks>
    /// <b>The list that turns a gap into a diagnosis.</b> Genesis mints one-code
    /// commitments and nothing else, so these are not merely reachable in the language —
    /// they are reachable by the very first thing the machine does. Whether they are
    /// RESIDENT at the end separates a learner that never found them from one that found
    /// them and was outvoted, and those two have different repairs.
    /// </remarks>
    public required ImmutableArray<Code> Alone { get; init; }

    /// <summary>Whether the enumeration ran out of budget before it ran out of scopes.</summary>
    /// <remarks>
    /// <b>Reported, because a silent cap reads as coverage.</b> A ceiling computed over
    /// half the language is not a ceiling, and the trap list already carries this in
    /// general terms: if a measurement bounds its own coverage, say what it dropped.
    /// </remarks>
    public required bool Capped { get; init; }
}

/// <summary>How a picture is cut up before it is winnowed.</summary>
/// <remarks>
/// <b>The arm this world was built to run, and the one <see cref="Cifar"/> COULD NOT.</b>
/// A ten-way label has no parts, so a front end that reads the whole picture at once and
/// one that reads it patch by patch score the same on it — and only the second can carry
/// an arrangement. Here they need not.
/// </remarks>
public enum Looking
{
    /// <summary>One projection over every pixel at once.</summary>
    Whole,

    /// <summary>One projection per patch, each part said bare and again with its place.</summary>
    Tiled,
}

/// <summary>
/// The arranged world, learnt through a front end that has to make the symbols.
/// </summary>
/// <remarks>
/// <para>
/// <b>What this measures that <see cref="CifarRun"/> could not.</b> That world ships
/// photons and asks for a ten-way label, and a label has no parts — so a front end
/// emitting a holistic blob per picture and one manufacturing reusable symbols score
/// the same, and only the second leads anywhere. Here the same parts recur in different
/// arrangements with opposite answers, so the two come apart.
/// </para>
/// <para>
/// <b>And the withheld arrangements are where memorising shows.</b> The scene space is
/// small enough to store outright, and a population that has done so is arbitrarily
/// accurate on what it was shown. <see cref="ArrangedSettings.Hold"/> keeps whole
/// arrangements back, so a lookup table scores chance on them and a rule about columns
/// does not.
/// </para>
/// <para>
/// <b>So the arm is whole against tiled, which is the one the plan calls the fix.</b>
/// Both are the same projection with the same geometry; what differs is whether it reads
/// the picture at once or one patch at a time. See <see cref="Looking"/>.
/// </para>
/// <para>
/// <b>And neither is banded, which is arithmetic rather than a preference.</b>
/// <see cref="Banded{TFrame}"/> spends a modality block per dimension and a modality is
/// one byte; a nine-by-nine scene is 81 numbers and does not fit, exactly as an
/// eight-by-eight thumbnail did not. The structural ceiling is the same finding
/// <see cref="CifarRun"/> already records.
/// </para>
/// </remarks>
public sealed class ArrangedRun
{
    /// <summary>
    /// The modality this world's pixels ride on.
    /// </summary>
    /// <remarks>
    /// <b>110, which is free and contiguous with nothing.</b> The worlds below claim
    /// 1-2, 10-13, 20-22, 30-33, 40-41, 50-55, 60, 70-71, 79-80, 90, 100-101 and 120;
    /// pixels take a block from 138 and 148, the learner has 200-203 and 210-211, and
    /// relations sit at 255. One byte is all a winnowed front end ever needs, because
    /// every code it emits is a winner among peers.
    /// </remarks>
    public const byte Patch = 110;

    private readonly Brain _brain;
    private readonly IQuantizer<IReadOnlyList<double>> _sensing;
    private readonly Func<(int Tags, long Readings)> _watching;
    private readonly Trial<IReadOnlyList<double>> _trial;

    /// <summary>The world, for asking what it holds.</summary>
    public Arranged World { get; }

    /// <summary>What the brain holds, for asking whether a nameable rule was found.</summary>
    public Commitments.Population Held => _brain.Held;

    /// <summary>What a blind guess scores.</summary>
    public double Chance => _trial.Chance;

    /// <param name="world">The shape of the scene.</param>
    /// <param name="brain">The one brain, already configured.</param>
    /// <param name="looking">How the picture is cut up before it is winnowed.</param>
    /// <param name="seed">The world's own generator.</param>
    /// <remarks>
    /// <b>The patch is the world's cell, and that is the one thing here worth arguing
    /// about.</b> A front end told where the parts are has been handed half the problem,
    /// which is the hand-specified bias this project exists to avoid. What saves it is
    /// that RESOLUTION is a world dial by the plan's own rule — how finely a scene shows
    /// itself is a fact about what is being looked at — and a patch size is a resolution.
    /// It is still the weakest joint in this measurement, and the honest version is a
    /// patch grid that does not divide the world's; fork 44 holds it.
    /// </remarks>
    public ArrangedRun(
        ArrangedSettings world,
        Brain brain,
        Looking looking,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(brain);

        World = new Arranged(world, seed);

        _brain = brain;

        if (looking == Looking.Tiled)
        {
            var tiling = new Tiling(Patch, World.Pixels, world.Cell);

            _sensing = tiling;
            _watching = () => (tiling.Distinct, tiling.Emitted);
        }
        else
        {
            var winnowing = new Winnowing(Patch, World.Width);

            _sensing = winnowing;
            _watching = () => (winnowing.Distinct, winnowing.Emitted);
        }

        _trial = new Trial<IReadOnlyList<double>>(World, _sensing, brain);
    }

    /// <summary>Runs the world, learns from it, and asks whether what it holds is true.</summary>
    /// <param name="rounds">How many rounds.</param>
    /// <param name="sweep">How often to subsume, abstract and cull.</param>
    /// <param name="target">The trailing accuracy to wait for.</param>
    /// <param name="window">How many answered predictions that accuracy is over.</param>
    public Grounded Run(long rounds, int sweep = 1000, double target = 0.8, int window = 2000)
    {
        var tally = _trial.Run(rounds, sweep, target, window);

        var graded = Grade();
        var (tags, readings) = _watching();

        return new Grounded
        {
            Tally = tally,
            Rules = graded,
            Tags = tags,
            Readings = readings,
        };
    }

    /// <summary>
    /// What a linear probe gets on this world, on pixels and on this run's codes.
    /// </summary>
    /// <param name="seed">The probe's shuffle, which is not the world's.</param>
    /// <remarks>
    /// <para>
    /// <b>Fitted on every scene the world draws and scored on every one it does not,
    /// which is exactly the exam the population sits.</b> Not a sample of the drawn
    /// scenes — all of them — because a probe shown less than the population was would
    /// be a bar set low by accident, and a bar set low by accident is worse than none.
    /// </para>
    /// <para>
    /// <b>It is allowed to train and the population is not, and that is the point.</b>
    /// C4 forbids the MACHINE depending on a train-then-test boundary; a yardstick is
    /// not the machine. Holding it to the architecture's constraints would stop it being
    /// a yardstick and make it a second unmeasured learner.
    /// </para>
    /// <para>
    /// <b>The coded probe reads an indicator per code, which is what the population
    /// reads.</b> A commitment's scope is a subset test over a set of codes; a linear
    /// model over the same set as ones and zeroes is the dullest thing that could use
    /// the identical information. Any other encoding would hand one side something the
    /// other never had.
    /// </para>
    /// </remarks>
    public Yardstick Measure(int seed = 1)
    {
        var drawn = new List<(IReadOnlyList<double> Reading, int Outcome)>();
        var unseen = new List<(IReadOnlyList<double> Reading, int Outcome)>();

        var coded = new List<(IReadOnlyCollection<Code> Codes, int Outcome, bool Shown)>();
        var features = new Dictionary<Code, int>();

        foreach (var layout in World.Layouts())
        {
            var reading = World.Render(layout);
            var codes = _sensing.Codify(reading);

            foreach (var code in codes)
                if (!features.ContainsKey(code)) features[code] = features.Count;

            (layout.Shown ? drawn : unseen).Add((reading, layout.Outcome));
            coded.Add((codes, layout.Outcome, layout.Shown));
        }

        var onDrawn = new List<(IReadOnlyList<double> Reading, int Outcome)>();
        var onUnseen = new List<(IReadOnlyList<double> Reading, int Outcome)>();

        foreach (var (codes, outcome, shown) in coded)
        {
            var indicator = new double[features.Count];
            foreach (var code in codes) indicator[features[code]] = 1.0;

            (shown ? onDrawn : onUnseen).Add((indicator, outcome));
        }

        return new Yardstick
        {
            OnPixels = Probe.Fit(drawn, unseen, World.Outcomes, seed: seed),
            OnCodes = Probe.Fit(onDrawn, onUnseen, World.Outcomes, seed: seed),
            Features = features.Count,
        };
    }

    /// <summary>
    /// Every experienced commitment, asked whether the world ever contradicts it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An inverted index rather than the obvious nested loop, and the reason is that
    /// the obvious one does not finish.</b> A few thousand commitments against ten
    /// thousand scenes is tens of millions of subset tests per run, which is enough to
    /// make the instrument the expensive part of the experiment — and an instrument
    /// somebody switches off to save time is an instrument that is not there.
    /// </para>
    /// <para>
    /// <b>Each code carries the set of scenes it fires in, as bits.</b> A scope fires
    /// exactly where all of its codes do, which is an AND; it is contradicted exactly
    /// where it fires and the outcome is not what it expects, which is an AND with the
    /// complement of the agreeing set. Three word-wise passes over a few hundred words,
    /// per commitment.
    /// </para>
    /// <para>
    /// <b>The names are spelled back out first.</b> A world knows nothing about minted
    /// codes, so a rule written in one can only be checked once it is unfolded — and a
    /// rewrite that changed what a commitment CLAIMS would show up right here as a rule
    /// that had quietly stopped being true.
    /// </para>
    /// </remarks>
    private Judged Grade()
    {
        var (scenes, words, firing, agreeing, _) = Index();

        var sound = 0;
        var unsound = 0;
        var inert = 0;

        // Kept as scopes so the two can be compared afterwards. Whether an unsound rule
        // has a sound rule sitting above it is the difference between a mechanism that
        // declined to fire and one that does not exist, and those want different work.
        var trues = new List<(ImmutableArray<Code> Scope, Code Expects)>();
        var falses = new List<(ImmutableArray<Code> Scope, Code Expects)>();

        // What the population believes about each, kept beside what the world says. The
        // gap between them is the only thing that decides whether a sharper vote could
        // help, and it costs one running total apiece.
        var trusted = 0.0;
        var doubted = 0.0;

        var codes = 0L;
        var held = 0;

        var fires = new ulong[words];

        foreach (var one in _brain.Held.All.Where(one => one.Seen >= _brain.Dials.Floor))
        {
            var scope = _brain.Held.Names.Unfold(one.Scope);

            codes += scope.Length;
            held++;

            // A code this world never emits makes the scope fire nowhere, which is
            // `Inert` and not `Unsound`. It happens whenever a population outlives the
            // front end that fed it, and reading it as a contradiction would blame the
            // learner for a scene that does not exist.
            Array.Fill(fires, ulong.MaxValue);

            foreach (var code in scope)
            {
                if (!firing.TryGetValue(code, out var where))
                {
                    Array.Clear(fires);
                    break;
                }

                for (var word = 0; word < words; word++) fires[word] &= where[word];
            }

            // The tail of the last word is not a scene. Leaving those bits set would
            // make an empty scope look as though it fired, which is the difference
            // between `Inert` and a silently perfect score.
            if (scenes.Count % 64 != 0)
                fires[words - 1] &= (1UL << (scenes.Count % 64)) - 1;

            // An expectation this world cannot produce is contradicted by every scene
            // it fires on, and saying so beats indexing off the end of the table. It
            // cannot happen while genesis only ever expects an outcome; it is here
            // because "cannot happen" is how a check stops being able to fail.
            var expects = one.Expects.Modality == Brain.Followed
                && one.Expects.Value < (ulong)World.Outcomes
                    ? (int)one.Expects.Value
                    : -1;

            var fired = false;
            var wrong = false;

            for (var word = 0; word < words; word++)
            {
                if (fires[word] == 0) continue;

                fired = true;

                if (expects < 0 || (fires[word] & ~agreeing[expects][word]) != 0)
                {
                    wrong = true;
                    break;
                }
            }

            if (!fired)
            {
                inert++;
            }
            else if (wrong)
            {
                unsound++;
                doubted += one.Accuracy;
                falses.Add((scope, one.Expects));
            }
            else
            {
                sound++;
                trusted += one.Accuracy;
                trues.Add((scope, one.Expects));
            }
        }

        var narrowed = falses.Count(bad => trues.Any(good =>
            good.Expects == bad.Expects
            && good.Scope.Length < bad.Scope.Length
            && good.Scope.All(bad.Scope.Contains)));

        return new Judged
        {
            Sound = sound,
            Unsound = unsound,
            Inert = inert,
            Layouts = scenes.Count,
            Narrowed = narrowed,
            Rootless = falses.Count - narrowed,
            Trusted = sound == 0 ? 0.0 : trusted / sound,
            Doubted = unsound == 0 ? 0.0 : doubted / unsound,
            Scope = held == 0 ? 0.0 : codes / (double)held,
        };
    }


    /// <summary>
    /// How much of the world a perfect learner could hold, in the language it has.
    /// </summary>
    /// <param name="depth">The widest scope to enumerate. Two is a conjunction of two.</param>
    /// <param name="budget">How many scopes to build before giving up and saying so.</param>
    /// <remarks>
    /// <para>
    /// <b>A sound scope makes every narrower one redundant, which is subsumption and is
    /// why this finishes.</b> Where a scope and a narrower version of it are equally
    /// accurate the general one stays, so a code already sound on its own is never
    /// extended — the plan's own rule, doing double duty as the pruning that makes an
    /// exhaustive search affordable.
    /// </para>
    /// <para>
    /// <b>And the pair is skipped where the two codes never co-occur.</b> A scope that
    /// fires nowhere is vacuously uncontradicted, and counting it as sound is exactly
    /// the <see cref="Judged.Inert"/> fault arriving in the ceiling instead of in the
    /// score.
    /// </para>
    /// </remarks>
    public Reached Reachable(int depth = 2, long budget = 20_000_000)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(depth, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(depth, 2);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(budget);

        var (scenes, words, firing, agreeing, unseen) = Index();

        var tail = scenes.Count % 64 == 0 ? ulong.MaxValue : (1UL << (scenes.Count % 64)) - 1;

        var codes = firing.Keys.Order().ToList();

        bool Sound(ulong[] fires)
        {
            for (var outcome = 0; outcome < World.Outcomes; outcome++)
            {
                var fits = true;

                for (var word = 0; word < words && fits; word++)
                    fits = (fires[word] & ~agreeing[outcome][word]) == 0;

                if (fits) return true;
            }

            return false;
        }

        bool Fires(ulong[] bits)
        {
            for (var word = 0; word < words; word++) if (bits[word] != 0) return true;
            return false;
        }

        var found = new List<ulong[]>();
        var singly = new bool[codes.Count];

        // Two scopes covering the same scenes are one fact about the world, and pairs
        // produce enormous numbers of duplicates -- a wedge in a patch wins four cells,
        // so every pair among them fires identically. Keeping all of them would spend
        // gigabytes to make the greedy cover slower and change nothing it returns.
        var already = new HashSet<long>();

        // And the retained set is bounded, because a few thousand codes make millions
        // of sound pairs and each is a bitset. Reported through `Capped` rather than
        // silently, since a ceiling computed over part of the language is not one.
        const int Keep = 200_000;

        long considered = 0;
        var capped = false;

        bool Fresh(ulong[] bits)
        {
            var hash = 17L;
            foreach (var word in bits) hash = (hash * 31) + (long)word;

            return already.Add(hash);
        }

        for (var which = 0; which < codes.Count; which++)
        {
            considered++;

            var fires = firing[codes[which]];

            if (!Fires(fires) || !Sound(fires)) continue;

            singly[which] = true;
            if (Fresh(fires)) found.Add(fires);
        }

        if (depth >= 2)
        {
            var fires = new ulong[words];

            for (var left = 0; left < codes.Count && !capped; left++)
            {
                // A code already sound alone is never narrowed. Anything it would reach
                // in a pair, it already reaches on its own and on more scenes besides.
                if (singly[left]) continue;

                var one = firing[codes[left]];

                for (var right = left + 1; right < codes.Count; right++)
                {
                    if (singly[right]) continue;

                    if (++considered > budget)
                    {
                        capped = true;
                        break;
                    }

                    var other = firing[codes[right]];

                    for (var word = 0; word < words; word++)
                        fires[word] = one[word] & other[word] & (word == words - 1 ? tail : ulong.MaxValue);

                    if (!Fires(fires) || !Sound(fires)) continue;
                    if (!Fresh(fires)) continue;

                    if (found.Count >= Keep)
                    {
                        capped = true;
                        break;
                    }

                    found.Add([.. fires]);
                }
            }
        }

        // The cover is greedy and says so. Set cover is the exact question and it is
        // NP-hard; what this number is for is standing beside `Resident`, and an
        // over-estimate of the true rule set is the safe direction for that comparison.
        var covered = new ulong[words];
        var least = 0;

        while (true)
        {
            ulong[]? best = null;
            var gain = 0;

            foreach (var candidate in found)
            {
                var adds = 0;

                for (var word = 0; word < words; word++)
                    adds += System.Numerics.BitOperations.PopCount(candidate[word] & ~covered[word]);

                if (adds > gain) (gain, best) = (adds, candidate);
            }

            if (best is null) break;

            for (var word = 0; word < words; word++) covered[word] |= best[word];
            least++;
        }

        var all = 0;
        var hidden = 0;
        var hiddenOf = 0;

        for (var word = 0; word < words; word++)
        {
            all += System.Numerics.BitOperations.PopCount(covered[word]);
            hidden += System.Numerics.BitOperations.PopCount(covered[word] & unseen[word]);
            hiddenOf += System.Numerics.BitOperations.PopCount(unseen[word]);
        }

        return new Reached
        {
            Depth = depth,
            Considered = considered,
            Sound = found.Count,
            Covers = all / (double)scenes.Count,
            CoversUnseen = hiddenOf == 0 ? 0.0 : hidden / (double)hiddenOf,
            Least = least,
            Alone = [.. Enumerable.Range(0, codes.Count).Where(one => singly[one]).Select(one => codes[one])],
            Capped = capped,
        };
    }

    /// <summary>
    /// Every scene coded once, with a bitset per code saying where it fires.
    /// </summary>
    /// <remarks>
    /// <b>Shared, because both things that ask the world a question need it and they
    /// must ask the same world.</b> Soundness reads what the population holds against
    /// this; the ceiling reads what the LANGUAGE could hold against it. Two builds of
    /// the same index could differ by a bug rather than by a finding, and the whole
    /// value of the second number is that it stands beside the first.
    /// </remarks>
    private (
        List<Layout> Scenes,
        int Words,
        Dictionary<Code, ulong[]> Firing,
        ulong[][] Agreeing,
        ulong[] Unseen) Index()
    {
        var scenes = World.Layouts().ToList();
        var words = (scenes.Count + 63) / 64;

        var firing = new Dictionary<Code, ulong[]>();
        var agreeing = new ulong[World.Outcomes][];
        var unseen = new ulong[words];

        for (var outcome = 0; outcome < World.Outcomes; outcome++)
            agreeing[outcome] = new ulong[words];

        for (var at = 0; at < scenes.Count; at++)
        {
            agreeing[scenes[at].Outcome][at / 64] |= 1UL << (at % 64);

            if (!scenes[at].Shown) unseen[at / 64] |= 1UL << (at % 64);

            foreach (var code in _sensing.Codify(World.Render(scenes[at])))
            {
                if (!firing.TryGetValue(code, out var where))
                    firing[code] = where = new ulong[words];

                where[at / 64] |= 1UL << (at % 64);
            }
        }

        return (scenes, words, firing, agreeing, unseen);
    }
}
