using System.Collections.Immutable;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace OpenPlexus.Codes;

/// <summary>
/// A frozen published encoder, and the preprocessing it was trained behind.
/// </summary>
/// <remarks>
/// <para>
/// <b>FROZEN IS WHAT MAKES IT LEGAL HERE.</b> The red-ball property says two machines
/// must agree about what they are looking at without speaking, and a published file of
/// constants satisfies it exactly as <see cref="Winnow"/>'s arithmetic-derived wiring
/// does: same file, same numbers, every machine, forever. An encoder that adapted
/// during a run would be a codebook fitted to the data, which the refutation table
/// forbids outright.
/// </para>
/// <para>
/// <b>AND IT MAY SAY WHAT IT IS LOOKING AT AND NEVER WHAT TO CONCLUDE.</b> A vector of
/// class scores is a conclusion. CLIP's vision tower emits an embedding by
/// construction; MobileNet's published export ends at a 1000-way classifier and
/// <c>fetch.sh</c> cuts the graph one <c>Gemm</c> early for exactly this reason.
/// </para>
/// </remarks>
public sealed record Encoder
{
    /// <summary>The ONNX file.</summary>
    public required string Model { get; init; }

    /// <summary>What the graph calls its input.</summary>
    public required string Input { get; init; }

    /// <summary>
    /// What the graph calls the output to read, or nothing where it has only one.
    /// </summary>
    /// <remarks>
    /// <b>NAMED RATHER THAN INDEXED, BECAUSE TAKING OUTPUT ZERO IS A SILENT WRONG
    /// ANSWER.</b> A vision tower that emits both a pooled embedding and a per-token
    /// hidden state offers a 512-wide reading and a 38,400-wide one, and the second is
    /// a perfectly plausible-looking tensor that would be coded, learnt from, and
    /// scored. Where a graph has exactly one output there is nothing to get wrong and
    /// this may be left unset; where it has several, <see cref="Encoded"/> refuses
    /// rather than choosing.
    /// </remarks>
    public string? Output { get; init; }

    /// <summary>How many pixels across the graph expects.</summary>
    public int Side { get; init; } = 224;

    /// <summary>Per-channel means subtracted after scaling to 0..1.</summary>
    public required ImmutableArray<double> Mean { get; init; }

    /// <summary>Per-channel standard deviations divided out.</summary>
    public required ImmutableArray<double> Deviation { get; init; }

    /// <summary>
    /// CLIP ViT-B/32's vision tower — the strong arm, and the expensive one.
    /// </summary>
    /// <param name="encoders">The <c>corpora/encoders</c> directory.</param>
    /// <remarks>
    /// <b>512 floats out, ~88M constants, ~4.4 GFLOPs an image</b>, and about 46 ms an
    /// image on four cores of a 2014 i7. Radford et al. 2021; weights MIT, © 2021
    /// OpenAI, ONNX export by Qdrant.
    /// </remarks>
    public static Encoder Clip(string encoders) => new()
    {
        Model = Path.Combine(encoders, "clip-vit-b32-vision", "model.onnx"),
        Input = "pixel_values",
        Output = "image_embeds",
        Mean = [0.48145466, 0.4578275, 0.40821073],
        Deviation = [0.26862954, 0.26130258, 0.27577711],
    };

    /// <summary>
    /// MobileNetV3-Small with its classifier cut off — the arm that fits the budget.
    /// </summary>
    /// <param name="encoders">The <c>corpora/encoders</c> directory.</param>
    /// <remarks>
    /// <b>1024 floats out, and about 1.7 ms an image on the same machine</b> — thirty
    /// times cheaper on one core than CLIP, and 6 MB against 352. Howard et al. 2019,
    /// Apache 2.0, <c>timm</c> lamb_in1k weights. The headless file is produced by
    /// <c>fetch.sh</c> and is not in the published repository.
    /// </remarks>
    public static Encoder MobileNet(string encoders) => new()
    {
        Model = Path.Combine(encoders, "mobilenetv3-small", "model_headless.onnx"),
        Input = "pixel_values",
        Mean = [0.485, 0.456, 0.406],
        Deviation = [0.229, 0.224, 0.225],
    };
}

/// <summary>
/// A reading run through a frozen encoder before anything codes it.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE ENCODER IS A TRANSLATION AND NOT A WORLD DIAL, WHICH IS THE PLAN'S OWN
/// RULE.</b> <i>The translation is a third thing and belongs at the join</i> — whether
/// a reading is banded, winnowed, or pushed through somebody's frozen weights first is
/// neither a fact about the problem nor a setting on the brain. So the world goes on
/// shipping photons, the brain goes on taking codes, and this sits between them as a
/// third arm rather than a rewrite of either.
/// </para>
/// <para>
/// <b>IT WRAPS A QUANTISER RATHER THAN REPLACING ONE.</b> An embedding is still a
/// reading — 512 or 1024 numbers instead of 3,072 — and something still has to turn
/// numbers into codes. Composing means the encoder arm and the raw arm differ in
/// exactly one place, which is the only way the comparison says anything.
/// </para>
/// <para>
/// <b>AND ON ITS OWN IT CANNOT ANSWER THE QUESTION IT IS FOR.</b> The open defect asks
/// whether the ceiling is the front end or the learner behind it. A score here is two
/// unknowns multiplied; it needs <see cref="Machines.Probe"/> over the SAME embeddings
/// as the other column, or the number is unanchored — a published 95% was measured
/// through a different pipeline at a different resolution and is not a bar this can be
/// held against.
/// </para>
/// </remarks>
public sealed class Encoded : IQuantizer<IReadOnlyList<double>>, IDisposable
{
    /// <summary>How many colour planes an encoder reads.</summary>
    private const int Planes = 3;

    private readonly Encoder _encoder;
    private readonly IQuantizer<IReadOnlyList<double>> _inner;
    private readonly InferenceSession _session;
    private readonly string _output;
    private readonly int _width;

    private readonly Dictionary<ulong, (IReadOnlyList<double> Reading, ImmutableArray<double> Embedding)>
        _memo = [];

    /// <param name="encoder">Which frozen graph, and what it was trained behind.</param>
    /// <param name="inner">
    /// What turns the embedding into codes, given how wide the embedding turned out.
    /// </param>
    /// <param name="width">How many numbers a reading has, three planes together.</param>
    /// <exception cref="FileNotFoundException">The encoder was not fetched.</exception>
    /// <remarks>
    /// <b>THE INNER FRONT END IS BUILT FROM A WIDTH THE GRAPH IS ASKED FOR, never one
    /// written down here.</b> CLIP emits 512 and the headless MobileNet 1024, and a
    /// constant would be a number that silently disagrees with whatever
    /// <c>fetch.sh</c> last pulled — which is the aliasing failure again, one layer up.
    /// </remarks>
    public Encoded(
        Encoder encoder, Func<int, IQuantizer<IReadOnlyList<double>>> inner, int width)
    {
        ArgumentNullException.ThrowIfNull(encoder);
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);

        if (width % Planes != 0)
            throw new ArgumentOutOfRangeException(
                nameof(width),
                $"{width} numbers is not three whole colour planes. An encoder reads a "
                + "picture and a grey world has none to give it.");

        if (!File.Exists(encoder.Model))
            throw new FileNotFoundException(
                $"the encoder is not at '{encoder.Model}'. Run corpora/fetch.sh.",
                encoder.Model);

        _encoder = encoder;
        _width = width;

        // ONE THREAD, BECAUSE A RUN HAS TO REPRODUCE. Intra-op parallelism reorders
        // floating-point reductions, so the same image encodes to slightly different
        // numbers run to run -- and a code is a QUANTISED number, so a reading either
        // side of a band boundary would emit different codes. Fork 12 has cost this
        // project twice and neither time was worth the wall clock.
        using var options = new SessionOptions
        {
            IntraOpNumThreads = 1,
            InterOpNumThreads = 1,
            ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
        };

        _session = new InferenceSession(encoder.Model, options);

        var outputs = _session.OutputMetadata.Keys.Order(StringComparer.Ordinal).ToList();

        _output = encoder.Output ?? (outputs.Count == 1 ? outputs[0] : null)
            ?? throw new ArgumentOutOfRangeException(
                nameof(encoder),
                $"'{Path.GetFileName(encoder.Model)}' has {outputs.Count} outputs "
                + $"({string.Join(", ", outputs)}) and none was named. Taking the first "
                + "would silently read a per-token hidden state as if it were an "
                + "embedding, so name one on the Encoder.");

        if (!_session.OutputMetadata.ContainsKey(_output))
            throw new ArgumentOutOfRangeException(
                nameof(encoder),
                $"'{Path.GetFileName(encoder.Model)}' has no output called "
                + $"'{_output}'. It has: {string.Join(", ", outputs)}.");

        Embedding = Embed(new double[width]).Length;
        _inner = inner(Embedding);
    }

    /// <inheritdoc/>
    public byte Modality => _inner.Modality;

    /// <summary>How many numbers the encoder emits.</summary>
    public int Embedding { get; }

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(IReadOnlyList<double> observation) =>
        _inner.Codify(Of(observation));

    /// <summary>
    /// The embedding itself, before anything codes it.
    /// </summary>
    /// <param name="observation">One reading, three colour planes together.</param>
    /// <remarks>
    /// <b>WHAT THE CONTROL ARM NEEDS, AND IT HAS TO BE THE SAME NUMBERS.</b>
    /// <see cref="Machines.Probe"/> is only a yardstick if it reads exactly what the
    /// commitment population reads; an embedding computed down a second path could
    /// differ by a bug rather than by the learner, and the whole comparison would be
    /// measuring that instead.
    /// </remarks>
    public ImmutableArray<double> Of(IReadOnlyList<double> observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        if (observation.Count != _width)
            throw new ArgumentOutOfRangeException(
                nameof(observation),
                $"this encoder was built for {_width} numbers and was handed "
                + $"{observation.Count}.");

        return Remembered(observation);
    }

    /// <summary>
    /// The embedding, computed once per distinct reading.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A MEMO OVER A PURE FUNCTION CANNOT CHANGE WHAT FIRES, which is the one thing
    /// it has to be able to say.</b> The graph is frozen and single-threaded, so the
    /// same reading yields the same numbers whether they were computed now or an hour
    /// ago. Fork 31 asks the same question of the separation table's spill, and the
    /// answer there is harder precisely because that one is not a pure function.
    /// </para>
    /// <para>
    /// <b>AND IT IS THE DIFFERENCE BETWEEN A RUN AND AN AFTERNOON.</b> A world draws
    /// with replacement from a finite bag, so twenty thousand rounds over twelve
    /// thousand images re-encodes each one nearly twice; at CLIP's 46 ms that is
    /// fifteen minutes of arithmetic already done.
    /// </para>
    /// <para>
    /// <b>The hash is checked rather than trusted.</b> A collision would hand one
    /// picture another's embedding and every downstream number would stay plausible,
    /// which is the exact shape of the aliasing fault <see cref="Banded{TFrame}"/>
    /// carried for the life of the repo.
    /// </para>
    /// </remarks>
    private ImmutableArray<double> Remembered(IReadOnlyList<double> reading)
    {
        var hash = Agreed.Basis;

        foreach (var number in reading)
            hash = Agreed.Fold(hash, (ulong)BitConverter.DoubleToInt64Bits(number));

        var key = Agreed.Mix(hash);

        if (_memo.TryGetValue(key, out var held) && held.Reading.SequenceEqual(reading))
            return held.Embedding;

        var embedding = Embed(reading);

        _memo[key] = (reading, embedding);

        return embedding;
    }

    /// <summary>
    /// One reading, resized and normalised the way the encoder was trained, and run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>RESIZED STRAIGHT TO THE GRAPH'S SIDE RATHER THAN THROUGH THE PUBLISHED
    /// SHORTEST-EDGE-THEN-CROP, and that is a deviation said out loud.</b> The
    /// published pipelines resize the short edge to 224 or 256 and centre-crop, which
    /// on a square 32-pixel source throws away an eighth of the picture at the border
    /// for no gain — the source is already square and already tiny. Every CIFAR linear
    /// probe worth comparing against does the same thing.
    /// </para>
    /// <para>
    /// <b>Bilinear rather than the bicubic the configs name</b>, which is the second
    /// deviation and the smaller one: upsampling 32 to 224 is a sevenfold stretch where
    /// the two filters differ by very little, and bicubic can overshoot outside 0..1
    /// and would need clamping that is itself a choice.
    /// </para>
    /// </remarks>
    private ImmutableArray<double> Embed(IReadOnlyList<double> reading)
    {
        var side = _encoder.Side;
        var from = (int)Math.Round(Math.Sqrt(_width / (double)Planes));

        var pixels = new DenseTensor<float>([1, Planes, side, side]);

        for (var plane = 0; plane < Planes; plane++)
        {
            var mean = _encoder.Mean[plane];
            var deviation = _encoder.Deviation[plane];

            for (var down = 0; down < side; down++)
            for (var across = 0; across < side; across++)
            {
                var value = Bilinear(reading, plane, from, side, down, across);

                pixels[0, plane, down, across] = (float)((value - mean) / deviation);
            }
        }

        using var said = _session.Run(
            [NamedOnnxValue.CreateFromTensor(_encoder.Input, pixels)], [_output]);

        return [.. said.Single().AsEnumerable<float>().Select(one => (double)one)];
    }

    /// <summary>One resampled pixel, read out of a planar reading in 0..1.</summary>
    private static double Bilinear(
        IReadOnlyList<double> reading, int plane, int from, int to, int down, int across)
    {
        // THE HALF-PIXEL CENTRES ARE NOT DECORATION. Mapping corner to corner instead
        // shifts the whole picture by half a source pixel, which on a 32-pixel source
        // stretched sevenfold is a visible translation -- and a front end that sees a
        // shifted picture is measuring something nobody asked about.
        var scale = from / (double)to;

        var y = Math.Clamp(((down + 0.5) * scale) - 0.5, 0.0, from - 1.0);
        var x = Math.Clamp(((across + 0.5) * scale) - 0.5, 0.0, from - 1.0);

        var top = (int)Math.Floor(y);
        var left = (int)Math.Floor(x);

        var bottom = Math.Min(top + 1, from - 1);
        var right = Math.Min(left + 1, from - 1);

        var downWeight = y - top;
        var acrossWeight = x - left;

        var at = plane * from * from;

        var upper = (reading[at + (top * from) + left] * (1.0 - acrossWeight))
            + (reading[at + (top * from) + right] * acrossWeight);

        var lower = (reading[at + (bottom * from) + left] * (1.0 - acrossWeight))
            + (reading[at + (bottom * from) + right] * acrossWeight);

        return (upper * (1.0 - downWeight)) + (lower * downWeight);
    }

    /// <summary>Releases the graph.</summary>
    public void Dispose() => _session.Dispose();
}
