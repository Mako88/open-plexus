using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Machines;

/// <summary>
/// Photographs, learnt through a front end that has to make the symbols.
/// </summary>
/// <remarks>
/// <para>
/// <b>THIS IS THE ONE MEASUREMENT THE PROJECT'S CLAIM RESTS ON.</b> Every other world
/// hands the learner symbols somebody else separated — words, a scene graph, a
/// relation column, or bits with noise poured over them. This hands it photons, and
/// which front end turns them into symbols is the arm.
/// </para>
/// <para>
/// <b>AND THE TWO ARMS ARE NOT INTERCHANGEABLE HERE, WHICH IS ITSELF THE FINDING.</b>
/// <see cref="Banded{TFrame}"/> gives every dimension its own block of modalities, and
/// a modality is one byte — so a reading wider than about fifty dimensions cannot be
/// addressed at all, once what the other worlds already claim is set aside. An
/// eight-by-eight thumbnail in grey is 64 numbers and does not fit.
/// <see cref="Winnowing"/> rides every code on ONE modality and has no such ceiling.
/// </para>
/// <para>
/// <b>SO "WINNOW BEATS BANDS ON IMAGES" IS THE WRONG SHAPE OF CLAIM TO GO LOOKING
/// FOR.</b> At the widths where both fit, the score is a fair comparison. Above them
/// there is no contest to run, because one of the two front ends cannot be pointed at
/// the problem. That is a structural difference and worth more than a number.
/// </para>
/// </remarks>
public sealed class CifarRun
{
    /// <summary>How finely the banded arm cuts a dimension.</summary>
    /// <remarks>
    /// <b>THE SAME EIGHT <see cref="GradedRun"/> USES</b>, because a front end setting
    /// that moved between worlds would put this run's score and that one's on
    /// different footings — which is the fault the whole seam exists to stop.
    /// </remarks>
    public const int Bands = 8;

    /// <summary>How many coarser retellings the banded arm adds.</summary>
    public const int Grains = 2;

    /// <summary>
    /// The modality the banded arm's first pixel rides on.
    /// </summary>
    /// <remarks>
    /// <b>148 BECAUSE THAT IS WHERE THE FREE RUN IS, AND THE RUN IS SHORT.</b> The
    /// worlds below it have claimed 20-22, 40-41, 50-55, 70, 100-101 and a block from
    /// 140 for moisture; the learner has 200-203 and 210-211, and relations sit at
    /// 255. What is left contiguous is 148 to 199 — fifty-two modalities, which at two
    /// spans is twenty-six dimensions. <b>A five-by-five thumbnail is the largest
    /// picture this front end can be shown</b>, and saying so is the point.
    /// </remarks>
    public const byte Pixel = 148;

    private readonly Trial<IReadOnlyList<double>> _trial;

    /// <summary>The world, for asking what it holds.</summary>
    public Cifar World { get; }

    /// <summary>What a blind guess scores.</summary>
    public double Chance => _trial.Chance;

    /// <param name="world">How much of the corpus to read, and how coarsely.</param>
    /// <param name="brain">The one brain, already configured.</param>
    /// <param name="fronting">Which translation makes the symbols.</param>
    /// <param name="seed">The world's own generator.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The banded arm was asked for a reading wider than a byte of modality affords.
    /// </exception>
    public CifarRun(CifarSettings world, Brain brain, Fronting fronting, int seed)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(brain);

        World = new Cifar(world, seed);

        IQuantizer<IReadOnlyList<double>> sensing = fronting == Fronting.Winnowed
            ? new Winnowing(Pixel, World.Width)
            : new Banded<IReadOnlyList<double>>(
                reading => reading, Pixel, World.Width, Bands, Grains);

        _trial = new Trial<IReadOnlyList<double>>(World, sensing, brain);
    }

    /// <summary>Runs the world and learns from it.</summary>
    /// <param name="rounds">How many rounds.</param>
    /// <param name="sweep">How often to subsume, abstract and cull.</param>
    /// <param name="target">The trailing accuracy to wait for.</param>
    /// <param name="window">How many answered predictions that accuracy is over.</param>
    public Tally Run(long rounds, int sweep = 1000, double target = 0.5, int window = 2000) =>
        _trial.Run(rounds, sweep, target, window);
}
