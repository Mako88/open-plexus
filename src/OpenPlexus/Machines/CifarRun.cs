using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Machines;

/// <summary>
/// Photographs, learnt through a front end that has to make the symbols.
/// </summary>
/// <remarks>
/// <para>
/// <b>THIS MEASURES THE FRONT END AND IT DOES NOT MEASURE THE BET.</b> Every other
/// world hands the learner symbols somebody else separated — words, a scene graph, a
/// relation column, or bits with noise poured over them. This hands it photons, and
/// which front end turns them into symbols is the arm. That is worth having, because
/// the field's own post-mortem puts the failure at the interface with perception; it
/// is also only half of what this design claims.
/// </para>
/// <para>
/// <b>AND THE HALF IT LEAVES OUT IS THE HALF THE GOAL IS WRITTEN IN.</b> <i>Understand
/// rather than perform — what would the world look like if I did X.</i> A draw from
/// CIFAR is single-shot and independent: no action, no intervention, no sequence, no
/// counterfactual. Settlement is trivial at K=1, <c>Abstain</c> cannot fire,
/// entailment depth is always one, and rungs three and four have nothing to bite on.
/// </para>
/// <para>
/// <b>SO A MEDIOCRE SCORE HERE IS A FACT ABOUT A FIXED PROJECTION ON THUMBNAILS AND
/// NOT A VERDICT ON THE ARCHITECTURE</b> — and saying so before the number arrives is
/// the only time saying it is worth anything. A ten-way label per image has no parts
/// and no arrangement, so it cannot tell a front end that manufactures REUSABLE
/// symbols from one that emits a holistic blob per picture; both separate ten classes
/// equally well, and only the first leads anywhere. What would tell them apart is a
/// world where the same parts recur in different arrangements and the answer depends
/// on the arrangement.
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
public sealed class CifarRun : IDisposable
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
    private readonly Encoded? _encoded;

    /// <summary>The world, for asking what it holds.</summary>
    public Cifar World { get; }

    /// <summary>What a blind guess scores.</summary>
    public double Chance => _trial.Chance;

    /// <summary>How many numbers reach the front end, after any encoder.</summary>
    public int Reading { get; }

    /// <param name="world">How much of the corpus to read, and how coarsely.</param>
    /// <param name="brain">The one brain, already configured.</param>
    /// <param name="fronting">Which translation makes the symbols.</param>
    /// <param name="seed">The world's own generator.</param>
    /// <param name="through">
    /// A frozen encoder to run the reading through first, or nothing for raw.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The banded arm was asked for a reading wider than a byte of modality affords,
    /// or an encoder was handed a world with no colour to read.
    /// </exception>
    /// <remarks>
    /// <b>THE ENCODER COMPOSES WITH THE FRONT END RATHER THAN REPLACING IT</b>, so the
    /// raw arm and the encoded arm differ in exactly one place. An embedding is still a
    /// reading and something still has to turn numbers into codes — which also means
    /// the banded ceiling applies to it unchanged, and 512 numbers is ten times past
    /// what a byte of modality can address.
    /// </remarks>
    public CifarRun(
        CifarSettings world,
        Brain brain,
        Fronting fronting,
        int seed,
        Encoder? through = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(brain);

        World = new Cifar(world, seed);

        if (through is not null && world.Grey)
            throw new ArgumentOutOfRangeException(
                nameof(world),
                "an encoder reads a picture and a grey world has none to give it. Set "
                + "Grey = false, and Side = 32 unless you mean to show it less than the "
                + "corpus holds.");

        IQuantizer<IReadOnlyList<double>> Front(int width) =>
            fronting == Fronting.Winnowed
                ? new Winnowing(Pixel, width)
                : new Banded<IReadOnlyList<double>>(
                    reading => reading, Pixel, width, Bands, Grains);

        IQuantizer<IReadOnlyList<double>> sensing;

        if (through is null)
        {
            Reading = World.Width;
            sensing = Front(Reading);
        }
        else
        {
            sensing = _encoded = new Encoded(through, Front, World.Width);
            Reading = _encoded.Embedding;
        }

        _trial = new Trial<IReadOnlyList<double>>(World, sensing, brain);
    }

    /// <summary>Releases the encoder's graph, where there is one.</summary>
    public void Dispose() => _encoded?.Dispose();

    /// <summary>Runs the world and learns from it.</summary>
    /// <param name="rounds">How many rounds.</param>
    /// <param name="sweep">How often to subsume, abstract and cull.</param>
    /// <param name="target">The trailing accuracy to wait for.</param>
    /// <param name="window">How many answered predictions that accuracy is over.</param>
    public Tally Run(long rounds, int sweep = 1000, double target = 0.5, int window = 2000) =>
        _trial.Run(rounds, sweep, target, window);
}
