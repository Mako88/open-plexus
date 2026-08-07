using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Machines;

/// <summary>
/// A trial on the arranged world, plus the two things only that world can say.
/// </summary>
/// <remarks>
/// <b>SOUNDNESS IS BACK, AND ON PHOTONS.</b> <see cref="Learned"/> carries the same
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

    /// <summary>Experienced commitments that no scene in the world contradicts.</summary>
    public required int Sound { get; init; }

    /// <summary>Experienced commitments some scene contradicts.</summary>
    public required int Unsound { get; init; }

    /// <summary>
    /// Experienced commitments that fire on no scene the world admits.
    /// </summary>
    /// <remarks>
    /// <b>REPORTED RATHER THAN COUNTED AS SOUND, WHICH IS THE WHOLE DIFFERENCE.</b> A
    /// rule contradicted by nothing because it applies to nothing is vacuously true, and
    /// folding those into <see cref="Sound"/> would let a population score by minting
    /// scopes that never fire. It is the same fault as counting a contradiction as
    /// sound, arriving from the other side.
    /// </remarks>
    public required int Inert { get; init; }

    /// <summary>How many scenes the check enumerated.</summary>
    public required int Layouts { get; init; }

    /// <summary>
    /// How many distinct things the front end said, over how many readings.
    /// </summary>
    /// <remarks>
    /// <b>THE COLLAPSE INSTRUMENT, AND THIS WORLD IS SMALL ENOUGH FOR IT TO HAVE A
    /// CEILING.</b> On CLEVR a projection over three numbers emitted one tag for four
    /// thousand objects, and nothing said so. Here what the front end could possibly
    /// distinguish is known and small — every scene for <see cref="Looking.Whole"/>,
    /// every distinct patch for <see cref="Looking.Tiled"/> — so a count far below it is
    /// a fact about the front end and not about the learner.
    /// </remarks>
    public required int Tags { get; init; }

    /// <summary>How many readings the front end was handed.</summary>
    public required long Readings { get; init; }
}

/// <summary>How a picture is cut up before it is winnowed.</summary>
/// <remarks>
/// <b>THE ARM THIS WORLD WAS BUILT TO RUN, AND THE ONE <see cref="Cifar"/> COULD NOT.</b>
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
/// <b>WHAT THIS MEASURES THAT <see cref="CifarRun"/> COULD NOT.</b> That world ships
/// photons and asks for a ten-way label, and a label has no parts — so a front end
/// emitting a holistic blob per picture and one manufacturing reusable symbols score
/// the same, and only the second leads anywhere. Here the same parts recur in different
/// arrangements with opposite answers, so the two come apart.
/// </para>
/// <para>
/// <b>AND THE WITHHELD ARRANGEMENTS ARE WHERE MEMORISING SHOWS.</b> The scene space is
/// small enough to store outright, and a population that has done so is arbitrarily
/// accurate on what it was shown. <see cref="ArrangedSettings.Hold"/> keeps whole
/// arrangements back, so a lookup table scores chance on them and a rule about columns
/// does not.
/// </para>
/// <para>
/// <b>SO THE ARM IS WHOLE AGAINST TILED, WHICH IS THE ONE THE PLAN CALLS THE FIX.</b>
/// Both are the same projection with the same geometry; what differs is whether it reads
/// the picture at once or one patch at a time. See <see cref="Looking"/>.
/// </para>
/// <para>
/// <b>AND NEITHER IS BANDED, WHICH IS ARITHMETIC RATHER THAN A PREFERENCE.</b>
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
    /// <b>110, WHICH IS FREE AND CONTIGUOUS WITH NOTHING.</b> The worlds below claim
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

    /// <summary>What a blind guess scores.</summary>
    public double Chance => _trial.Chance;

    /// <param name="world">The shape of the scene.</param>
    /// <param name="brain">The one brain, already configured.</param>
    /// <param name="looking">How the picture is cut up before it is winnowed.</param>
    /// <param name="seed">The world's own generator.</param>
    /// <remarks>
    /// <b>THE PATCH IS THE WORLD'S CELL, AND THAT IS THE ONE THING HERE WORTH ARGUING
    /// ABOUT.</b> A front end told where the parts are has been handed half the problem,
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

        var (sound, unsound, inert, scenes) = Grade();
        var (tags, readings) = _watching();

        return new Grounded
        {
            Tally = tally,
            Sound = sound,
            Unsound = unsound,
            Inert = inert,
            Layouts = scenes,
            Tags = tags,
            Readings = readings,
        };
    }

    /// <summary>
    /// Every experienced commitment, asked whether the world ever contradicts it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>AN INVERTED INDEX RATHER THAN THE OBVIOUS NESTED LOOP, AND THE REASON IS THAT
    /// THE OBVIOUS ONE DOES NOT FINISH.</b> A few thousand commitments against ten
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
    /// <b>THE NAMES ARE SPELLED BACK OUT FIRST.</b> A world knows nothing about minted
    /// codes, so a rule written in one can only be checked once it is unfolded — and a
    /// rewrite that changed what a commitment CLAIMS would show up right here as a rule
    /// that had quietly stopped being true.
    /// </para>
    /// </remarks>
    private (int Sound, int Unsound, int Inert, int Layouts) Grade()
    {
        var scenes = World.Layouts().ToList();
        var words = (scenes.Count + 63) / 64;

        var firing = new Dictionary<Code, ulong[]>();
        var agreeing = new ulong[World.Outcomes][];

        for (var outcome = 0; outcome < World.Outcomes; outcome++)
            agreeing[outcome] = new ulong[words];

        for (var at = 0; at < scenes.Count; at++)
        {
            agreeing[scenes[at].Outcome][at / 64] |= 1UL << (at % 64);

            foreach (var code in _sensing.Codify(World.Render(scenes[at])))
            {
                if (!firing.TryGetValue(code, out var where))
                    firing[code] = where = new ulong[words];

                where[at / 64] |= 1UL << (at % 64);
            }
        }

        var sound = 0;
        var unsound = 0;
        var inert = 0;

        var fires = new ulong[words];

        foreach (var one in _brain.Held.All.Where(one => one.Seen >= _brain.Dials.Floor))
        {
            var scope = _brain.Held.Names.Unfold(one.Scope);

            // A CODE THIS WORLD NEVER EMITS MAKES THE SCOPE FIRE NOWHERE, which is
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

            // THE TAIL OF THE LAST WORD IS NOT A SCENE. Leaving those bits set would
            // make an empty scope look as though it fired, which is the difference
            // between `Inert` and a silently perfect score.
            if (scenes.Count % 64 != 0)
                fires[words - 1] &= (1UL << (scenes.Count % 64)) - 1;

            // AN EXPECTATION THIS WORLD CANNOT PRODUCE IS CONTRADICTED BY EVERY SCENE
            // IT FIRES ON, and saying so beats indexing off the end of the table. It
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

            if (!fired) inert++;
            else if (wrong) unsound++;
            else sound++;
        }

        return (sound, unsound, inert, scenes.Count);
    }
}
