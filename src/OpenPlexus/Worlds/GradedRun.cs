using OpenPlexus.Codes;
using OpenPlexus.Commitments;

namespace OpenPlexus.Worlds;

/// <summary>Which front end makes the symbols.</summary>
/// <remarks>
/// <b>Two ways of saying a number, and both do something.</b> One code per band per
/// dimension against a sparse set of winners read across dimensions together — the
/// comparison the plan has called its defence and never run.
/// </remarks>
public enum Fronting
{
    /// <summary>One code per band, per dimension, per grain.</summary>
    Banded,

    /// <summary>A sparse set of winners over a random projection of every dimension.</summary>
    Winnowed,
}

/// <summary>What one run of the graded world learnt.</summary>
/// <remarks>
/// <b>NO SOUNDNESS HERE, AND THAT IS THE PRICE OF THE INTERFACE.</b> A scope over
/// banded or winnowed codes does not pin bits, so the exact enumeration that made the
/// multiplexer's score basis-independent has nothing to enumerate over. Said before
/// the numbers rather than after them.
/// </remarks>
public sealed record Sensed
{
    /// <summary>Rounds run.</summary>
    public required long Rounds { get; init; }

    /// <summary>The share of answered predictions right over the last tenth.</summary>
    public required double Recent { get; init; }

    /// <summary>The round a trailing window first held the target, or zero if never.</summary>
    public required long Reached { get; init; }

    /// <summary>Rounds where nothing fired.</summary>
    public required long Silent { get; init; }

    /// <summary>Commitments resident at the end.</summary>
    public required int Resident { get; init; }

    /// <summary>How many codes one round produced, on average.</summary>
    /// <remarks>
    /// <b>The cost side of the arm, and what makes the comparison fair.</b> A front
    /// end that emits four times as many codes has four times as much to search, so a
    /// score that ignored this would be rewarding whoever was allowed to say more.
    /// </remarks>
    public required double Codes { get; init; }

    /// <summary>Codes minted to stand for sub-scopes that kept recurring.</summary>
    public required int Named { get; init; }
}

/// <summary>
/// The graded world, learnt through a front end that has to make the symbols.
/// </summary>
/// <remarks>
/// <b>THIS IS THE ONLY MEASUREMENT IN THE REPO THAT TOUCHES THE PROJECT'S CLAIM.</b>
/// Everything else takes symbols as given.
/// </remarks>
public sealed class GradedRun
{
    /// <summary>How many cells the projection expands into, per dimension read.</summary>
    /// <remarks>
    /// <b>A constant of the design and not of a run, exactly as the projection
    /// itself is.</b> The fly expands fifty receptors into two thousand cells; the
    /// ratio is what preserves similarity on little data, and making it a dial would
    /// invite tuning the front end to the world — which is the fitted codebook the
    /// red-ball property forbids, arriving by the back door.
    /// </remarks>
    private const int Expansion = 40;

    /// <summary>One winner per this many cells.</summary>
    private const int Sparsity = 20;

    /// <summary>How finely the banded arm cuts a dimension, and how many times over.</summary>
    public const int Bands = 8;

    /// <summary>How many coarser retellings the banded arm adds.</summary>
    public const int Grains = 2;

    private readonly Graded _world;
    private readonly Population _held;
    private readonly Fronting _sensing;
    private readonly Winnow? _winnow;
    private readonly Banded<IReadOnlyList<double>>? _banded;

    /// <param name="world">The shape of the world.</param>
    /// <param name="dials">Every number the brain is allowed to have.</param>
    /// <param name="sensing">Which front end makes the symbols.</param>
    /// <param name="seed">The run's own generator.</param>
    public GradedRun(GradedSettings world, CommittingSettings dials, Fronting sensing, int seed)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        _world = new Graded(world, seed);
        _held = new Population(dials, seed);
        _sensing = sensing;

        if (sensing == Fronting.Winnowed)
        {
            var (cells, reach, winners) = Geometry(_world.Width);
            _winnow = new Winnow(Multiplexer.Bit, _world.Width, cells, reach, winners);
        }
        else
            _banded = new Banded<IReadOnlyList<double>>(
                reading => reading, Multiplexer.Bit, Bands, Grains);
    }

    /// <summary>What the machine holds.</summary>
    public Population Held => _held;

    /// <summary>
    /// How wide a sheet a reading of this many dimensions can actually support.
    /// </summary>
    /// <param name="width">How many dimensions are read.</param>
    /// <remarks>
    /// <para>
    /// <b>DERIVED, BECAUSE A FIXED GEOMETRY IS DEGENERATE ON A NARROW READING.</b> The
    /// fly projects fifty receptors into two thousand cells; six numbers sampled six
    /// at a time have exactly ONE distinct wiring, so every cell would fire
    /// identically on every reading and the tag would separate nothing. `Winnow`
    /// refuses that outright rather than quietly producing it.
    /// </para>
    /// <para>
    /// <b>So the sheet is capped by how many distinct wirings exist</b>, and a narrow
    /// world simply gets a small one. That is the honest answer: population coding
    /// needs dimensions to project FROM, and a reading below about ten of them is
    /// under where the trick works at all.
    /// </para>
    /// </remarks>
    public static (int Cells, int Reach, int Winners) Geometry(int width)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 3);

        var reach = Math.Clamp(width / 3, 2, width - 1);
        var wanted = width * Expansion;
        var cells = Math.Min(wanted, Wirings(width, reach, wanted));

        return (cells, reach, Math.Max(2, cells / Sparsity));
    }

    /// <summary>How many distinct wirings there are, giving up once past a ceiling.</summary>
    private static int Wirings(int inputs, int samples, int ceiling)
    {
        long total = 1;

        for (var step = 0; step < samples; step++)
        {
            total = total * (inputs - step) / (step + 1);
            if (total >= ceiling) return ceiling;
        }

        return (int)total;
    }

    /// <summary>Runs the world and learns from it.</summary>
    /// <param name="rounds">How many rounds.</param>
    /// <param name="sweep">How often to subsume, abstract and cull.</param>
    /// <param name="target">The trailing accuracy to wait for.</param>
    /// <param name="window">How many answered predictions that accuracy is over.</param>
    public Sensed Run(long rounds, int sweep = 1000, double target = 0.9, int window = 2000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rounds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sweep);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(window);

        var cycle = new Cycle(_held, rounds, sweep, target, window);

        long codes = 0;

        for (long round = 0; round < rounds; round++)
        {
            var shown = _world.Next();

            // THE ONLY LINE THAT DIFFERS FROM THE SYMBOLIC WORLD, which is the whole
            // point of the pairing: same function, same learner, and the interface is
            // the arm.
            var said = _sensing == Fronting.Winnowed
                ? _winnow!.Of(shown.Reading)
                : (IReadOnlyCollection<Code>)_banded!.Codify(shown.Reading);

            codes += said.Count;

            cycle.Step(_held.Moment(new HashSet<Code>(said)), shown.Outcome);
        }

        return new Sensed
        {
            Rounds = rounds,
            Recent = cycle.Recent,
            Reached = cycle.Reached,
            Silent = cycle.Silent,
            Resident = _held.Count,
            Codes = codes / (double)rounds,
            Named = _held.Names.Count,
        };
    }
}
