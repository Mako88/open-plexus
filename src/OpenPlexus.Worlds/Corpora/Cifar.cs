using System.Collections.Immutable;

namespace OpenPlexus.Worlds;

/// <summary>
/// How much of CIFAR-10 to read, and how coarsely.
/// </summary>
/// <remarks>
/// <b>Resolution is a world dial and not a brain one.</b> How finely a world shows
/// itself is a fact about what is being looked at; how that reading becomes symbols is
/// the translation's business, and neither is the brain's. The plan says it in one
/// line — the demand for resolution may change how finely the front end cuts, and
/// never what it cuts along.
/// </remarks>
public sealed record CifarSettings
{
    /// <summary>The extracted <c>cifar-10-batches-bin</c> directory.</summary>
    public required string Corpus { get; init; }

    /// <summary>How many images to hold, from the start of the test batch.</summary>
    /// <remarks>
    /// <b>The split means nothing to a system with no training phase</b>, exactly as
    /// it means nothing on CLEVR and CLUTRR. The test batch is taken first because it
    /// is one file of ten thousand with every class equally represented; the five
    /// training batches follow it in order when more than that is asked for.
    /// </remarks>
    public int Images { get; init; } = 10_000;

    /// <summary>How many further images to load and never draw.</summary>
    /// <remarks>
    /// <para>
    /// <b>The only thing on this world that can tell learning from memorising.</b> The
    /// bag is finite and the draw is with replacement, so at forty thousand rounds over
    /// ten thousand images each has recurred four times — and a score over the drawn bag
    /// cannot distinguish a learner that generalises from one holding a lookup table
    /// keyed on a winner set. These are loaded, coded and scored exactly as the drawn
    /// ones are, and <see cref="Cifar.Next"/> never reaches them.
    /// </para>
    /// <para>
    /// <b>Taken after the drawn ones in the fixed file order.</b> So the split is a
    /// position and not a sample. A withheld set chosen by the world's own generator
    /// would move with the seed, and two seeds would then be scored against two
    /// different questions — which is the shape of thing <see cref="Seeds"/> exists
    /// about.
    /// </para>
    /// </remarks>
    public int Withheld { get; init; } = 2_000;

    /// <summary>How many pixels across, after box-averaging. Must divide 32.</summary>
    /// <remarks>
    /// <para>
    /// <b>Eight by default, and the reason is arithmetic rather than taste.</b>
    /// <see cref="Codes.Winnowing"/> derives its sheet from the reading's width and
    /// takes one winner per twenty cells, so the number of codes a moment contains is
    /// LINEAR in how wide the reading is. Full 32x32 colour is 3,072 numbers, which is
    /// 6,144 live codes per moment — and genesis mints a candidate per live code on
    /// every surprise.
    /// </para>
    /// <para>
    /// <b>So the full-resolution arm is not expensive, it is unrunnable</b>, and that
    /// is a finding about the front end rather than a limit of the machine. At eight
    /// by eight in grey the reading is 64 numbers and the sheet is 2,560 cells with
    /// 128 winners — which is the scale the fly actually operates at, fifty receptors
    /// into two thousand cells. See <see cref="Codes.Winnowing.Sheet"/>.
    /// </para>
    /// </remarks>
    public int Side { get; init; } = 8;

    /// <summary>Whether the three channels are averaged into one.</summary>
    /// <remarks>
    /// <b>On by default</b>, because colour triples the width for this world's hardest
    /// classes. It is a real part of the problem and it is the first thing to turn
    /// off when the code count can afford it — which is exactly what makes it a dial
    /// on the world rather than a decision baked into the reader.
    /// </remarks>
    public bool Grey { get; init; } = true;
}

/// <summary>
/// Ten classes of small photograph, said as numbers.
/// </summary>
/// <remarks>
/// <para>
/// <b>The first world here that was not already symbols</b>, and that is the whole
/// point. bAbI ships words, CLEVR ships a scene graph with every object's colour
/// and shape already separated, CLUTRR ships the relation as a column. Each of those
/// hands over the front end this architecture claims to replace, so nothing measured
/// on them has said anything about the interface. This ships photons.
/// </para>
/// <para>
/// <b>AND <see cref="Graded"/> COULD NOT STRESS IT.</b> That world is the multiplexer
/// with its bits turned back into readings, so a dimension still means one thing and
/// the answer still depends on a handful of them exactly. Here nothing is a bit, no
/// dimension means anything on its own, and which pixels matter is not known to
/// anybody — including whoever wrote the world.
/// </para>
/// <para>
/// <b>It draws with replacement rather than walking the file.</b> A stream with no
/// episode boundary is what the rest of the design assumes — a lifetime accuracy over
/// a fixed pass would be measuring the end of the corpus — so the world is an infinite
/// draw from a finite bag, as <see cref="Graded"/> and <see cref="Multiplexer"/> are.
/// </para>
/// <para>
/// Krizhevsky 2009, <i>Learning Multiple Layers of Features from Tiny Images</i>. The
/// binary distribution is fixed-width records and needs no interpreter to open, which
/// is why <c>fetch.sh</c> takes it over the pickled one.
/// </para>
/// </remarks>
public sealed class Cifar : IWorld<IReadOnlyList<double>>, IWithholds<IReadOnlyList<double>>
{
    /// <summary>How many classes there are.</summary>
    public const int Classes = 10;

    /// <summary>How many pixels across a stored image is.</summary>
    private const int Stored = 32;

    /// <summary>One label byte then three planes of <see cref="Stored"/> squared.</summary>
    private const int Record = 1 + (3 * Stored * Stored);

    /// <summary>How many records one batch file holds.</summary>
    private const int Batch = 10_000;

    /// <summary>
    /// The batch files, test first.
    /// </summary>
    /// <remarks>
    /// <b>Named rather than globbed, so the order cannot change with the
    /// filesystem.</b> A world whose stream depends on directory enumeration order is
    /// not reproducible, and <see cref="Seeds"/> already records what that costs.
    /// </remarks>
    private static readonly string[] Files =
    [
        "test_batch.bin",
        "data_batch_1.bin", "data_batch_2.bin", "data_batch_3.bin",
        "data_batch_4.bin", "data_batch_5.bin",
    ];

    private readonly ImmutableArray<ImmutableArray<double>> _readings;
    private readonly ImmutableArray<int> _labels;
    private readonly Random _rng;

    /// <inheritdoc/>
    public int Outcomes => Classes;

    /// <inheritdoc/>
    public IReadOnlyList<Turn<IReadOnlyList<double>>> Withheld { get; }

    /// <summary>How many numbers one reading has.</summary>
    public int Width { get; }

    /// <summary>How many images were loaded.</summary>
    public int Held => _labels.Length;

    /// <summary>What a blind guess scores.</summary>
    public static double Chance => 1.0 / Classes;

    /// <param name="settings">How much to read, and how coarsely.</param>
    /// <param name="seed">The world's own generator.</param>
    /// <exception cref="DirectoryNotFoundException">The corpus is not there.</exception>
    public Cifar(CifarSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Images, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Withheld);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            settings.Images + settings.Withheld, Files.Length * Batch);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Side, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Side, Stored);

        if (Stored % settings.Side != 0)
            throw new ArgumentOutOfRangeException(
                nameof(settings),
                $"Side must divide {Stored}; {settings.Side} does not. Box-averaging a "
                + "32-pixel row into an unequal number of buckets would weight some "
                + "pixels more than others, which is a bias nobody asked for.");

        if (!Directory.Exists(settings.Corpus))
            throw new DirectoryNotFoundException(
                $"CIFAR-10 is not at '{settings.Corpus}'. Run corpora/fetch.sh.");

        Width = settings.Side * settings.Side * (settings.Grey ? 1 : 3);

        // The withheld are loaded by the same reader and coded the same way. A
        // held-out set that arrived down a second path could differ from the drawn one
        // by a bug rather than by learning, and the gap between the two scores is the
        // whole product here.
        var wanted = settings.Images + settings.Withheld;

        var readings = ImmutableArray.CreateBuilder<ImmutableArray<double>>();
        var labels = ImmutableArray.CreateBuilder<int>();

        var buffer = new byte[Record];

        foreach (var name in Files)
        {
            if (labels.Count >= wanted) break;

            var path = Path.Combine(settings.Corpus, name);

            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"CIFAR-10 batch '{name}' is missing from '{settings.Corpus}'. "
                    + "Run corpora/fetch.sh.", path);

            using var file = File.OpenRead(path);

            while (labels.Count < wanted && file.ReadExactlyOrEnd(buffer))
            {
                labels.Add(buffer[0]);
                readings.Add(Read(buffer, settings));
            }
        }

        if (labels.Count < wanted)
            throw new InvalidDataException(
                $"asked for {settings.Images} images and {settings.Withheld} held back, "
                + $"and the corpus held {labels.Count}.");

        // The split is a position in a fixed file order and not a draw, so every seed
        // is scored against the same held-out question. Splitting with the world's own
        // generator would give each seed a different exam.
        _readings = [.. readings.Take(settings.Images)];
        _labels = [.. labels.Take(settings.Images)];

        Withheld =
        [
            .. Enumerable.Range(settings.Images, settings.Withheld)
                .Select(at => new Turn<IReadOnlyList<double>>
                {
                    Seen = readings[at],
                    Outcome = labels[at],
                }),
        ];

        _rng = new Random(seed);
    }

    /// <summary>One draw from the bag.</summary>
    public Turn<IReadOnlyList<double>> Next()
    {
        var which = _rng.Next(_labels.Length);

        return new Turn<IReadOnlyList<double>>
        {
            Seen = _readings[which],
            Outcome = _labels[which],
        };
    }

    /// <summary>What class a label stands for, for reading a run by eye.</summary>
    /// <param name="label">The label, 0..9.</param>
    public static string Named(int label) => label switch
    {
        0 => "airplane", 1 => "automobile", 2 => "bird", 3 => "cat", 4 => "deer",
        5 => "dog", 6 => "frog", 7 => "horse", 8 => "ship", 9 => "truck",
        _ => throw new ArgumentOutOfRangeException(nameof(label)),
    };

    /// <summary>
    /// One record turned into a reading in 0..1.
    /// </summary>
    /// <remarks>
    /// <b>BOX-AVERAGED RATHER THAN SUBSAMPLED.</b> Taking every fourth pixel throws
    /// away three quarters of the photons and makes the reading depend on the phase of
    /// the grid; averaging keeps them and is what a coarser sensor would actually
    /// report. It also means the coarse arm is a genuine LOSS OF RESOLUTION rather
    /// than a differently-aliased image, which is what the dial claims to be.
    /// </remarks>
    private static ImmutableArray<double> Read(byte[] record, CifarSettings settings)
    {
        var box = Stored / settings.Side;
        var per = box * box;
        var channels = settings.Grey ? 1 : 3;

        var reading = new double[settings.Side * settings.Side * channels];

        for (var down = 0; down < settings.Side; down++)
        for (var across = 0; across < settings.Side; across++)
        {
            // The three planes are read together so grey costs one pass. A separate
            // averaging step per channel would read the record three times to produce
            // one number.
            Span<double> sum = [0.0, 0.0, 0.0];

            for (var y = down * box; y < (down + 1) * box; y++)
            for (var x = across * box; x < (across + 1) * box; x++)
            {
                var at = (y * Stored) + x;

                for (var plane = 0; plane < 3; plane++)
                    sum[plane] += record[1 + (plane * Stored * Stored) + at];
            }

            var cell = (down * settings.Side) + across;

            if (settings.Grey)
            {
                reading[cell] = (sum[0] + sum[1] + sum[2]) / (per * 3 * 255.0);
            }
            else
            {
                for (var plane = 0; plane < 3; plane++)
                    reading[(plane * settings.Side * settings.Side) + cell] =
                        sum[plane] / (per * 255.0);
            }
        }

        return [.. reading];
    }
}

/// <summary>Reading a whole record, or saying the file ended.</summary>
internal static class Records
{
    /// <summary>
    /// Fills the buffer, or reports a clean end of file.
    /// </summary>
    /// <param name="file">The open batch.</param>
    /// <param name="buffer">Where the record goes.</param>
    /// <returns>Whether a whole record was read.</returns>
    /// <exception cref="InvalidDataException">The file ended mid-record.</exception>
    /// <remarks>
    /// <b><see cref="Stream.Read(byte[], int, int)"/> Is allowed to return short and
    /// usually does not</b>, which is how a reader passes every test on a local disk
    /// and truncates one image in ten thousand somewhere else. A partial record is a
    /// corrupt corpus and says so rather than being padded with zeroes into a
    /// plausible grey square.
    /// </remarks>
    public static bool ReadExactlyOrEnd(this Stream file, byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(buffer);

        var filled = 0;

        while (filled < buffer.Length)
        {
            var got = file.Read(buffer, filled, buffer.Length - filled);

            if (got == 0)
                return filled == 0
                    ? false
                    : throw new InvalidDataException(
                        $"the batch ended {buffer.Length - filled} bytes into a record.");

            filled += got;
        }

        return true;
    }
}
