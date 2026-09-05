using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace OpenPlexus.Codes;

/// <summary>
/// A frozen published sentence encoder, asked for one word at a time.
/// </summary>
/// <remarks>
/// <para>
/// The text half of <see cref="Encoded"/>, and it is here for the same reason. A published
/// file of constants gives the same numbers on every machine forever, so two holders agree
/// about what they are looking at without speaking. Nothing fits it and nothing trains it.
/// </para>
/// <para>
/// It says what it is looking at and never what to conclude. A vector is a reading of a word
/// the way an embedding is a reading of a picture; what any direction through it means is
/// not written down anywhere and has to be learnt.
/// </para>
/// <para>
/// Single tokens only. Word-piece splitting is a real thing this does not do, so a word the
/// published vocabulary does not hold whole is refused rather than approximated, and
/// <see cref="Knows"/> is how a caller asks first.
/// </para>
/// </remarks>
public sealed class Worded : IDisposable
{
    /// <summary>The published vocabulary's sentence-start token.</summary>
    private const int Cls = 101;

    /// <summary>The published vocabulary's sentence-end token.</summary>
    private const int Sep = 102;

    /// <summary>How many positions one word occupies, the two markers included.</summary>
    private const int Positions = 3;

    private readonly InferenceSession _session;
    private readonly Dictionary<string, int> _vocabulary;
    private readonly string _output;
    private readonly Dictionary<string, float[]> _memo = [];

    /// <param name="encoders">The <c>corpora/encoders</c> directory.</param>
    /// <exception cref="FileNotFoundException">The encoder was not fetched.</exception>
    public Worded(string encoders)
    {
        ArgumentNullException.ThrowIfNull(encoders);

        var directory = Path.Combine(encoders, "all-minilm-l6-v2");
        var model = Path.Combine(directory, "model.onnx");

        if (!File.Exists(model))
            throw new FileNotFoundException(
                $"the encoder is not at '{model}'. Run corpora/fetch.sh.", model);

        // One thread and sequential, for the reason `Encoded` is: intra-op parallelism
        // reorders floating-point reductions, and a code is a quantised number, so a reading
        // either side of a band boundary would emit different codes run to run.
        using var options = new SessionOptions
        {
            IntraOpNumThreads = 1,
            InterOpNumThreads = 1,
            ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
        };

        _session = new InferenceSession(model, options);
        _output = _session.OutputMetadata.Keys.Order(StringComparer.Ordinal).First();

        _vocabulary = [];

        var line = 0;

        foreach (var token in File.ReadLines(Path.Combine(directory, "vocab.txt")))
        {
            _vocabulary.TryAdd(token, line);
            line++;
        }
    }

    /// <summary>How many numbers a reading has.</summary>
    public int Width { get; private set; } = 384;

    /// <summary>Whether the published vocabulary holds this word as one token.</summary>
    /// <param name="word">The word, lowercase.</param>
    public bool Knows(string word) => _vocabulary.ContainsKey(word);

    /// <summary>
    /// One word as a unit vector.
    /// </summary>
    /// <param name="word">The word, lowercase and one token.</param>
    /// <remarks>
    /// Mean-pooled over the positions and then normalised, which is what the published
    /// model's own pooling does. The length is thrown away so a direction is a direction
    /// rather than a direction times how common the word is.
    /// </remarks>
    /// <exception cref="ArgumentException">The vocabulary does not hold the word whole.</exception>
    public float[] Of(string word)
    {
        ArgumentNullException.ThrowIfNull(word);

        lock (_memo)
            if (_memo.TryGetValue(word, out var held)) return held;

        if (!_vocabulary.TryGetValue(word, out var id))
            throw new ArgumentException(
                $"`{word}` is not one token of the published vocabulary", nameof(word));

        var ids = new DenseTensor<long>([1, Positions]);
        ids[0, 0] = Cls;
        ids[0, 1] = id;
        ids[0, 2] = Sep;

        var mask = new DenseTensor<long>([1, Positions]);
        var types = new DenseTensor<long>([1, Positions]);

        for (var at = 0; at < Positions; at++)
        {
            mask[0, at] = 1;
            types[0, at] = 0;
        }

        List<NamedOnnxValue> inputs =
        [
            NamedOnnxValue.CreateFromTensor("input_ids", ids),
            NamedOnnxValue.CreateFromTensor("attention_mask", mask),
        ];

        if (_session.InputMetadata.ContainsKey("token_type_ids"))
            inputs.Add(NamedOnnxValue.CreateFromTensor("token_type_ids", types));

        using var said = _session.Run(inputs);
        var hidden = said.First(one => one.Name == _output).AsTensor<float>();

        Width = hidden.Dimensions[^1];

        var pooled = new float[Width];

        for (var at = 0; at < Positions; at++)
            for (var d = 0; d < Width; d++)
                pooled[d] += hidden[0, at, d];

        var unit = Unit(pooled);

        lock (_memo) _memo[word] = unit;

        return unit;
    }

    /// <summary>The same vector scaled to length one, or left alone where it has none.</summary>
    /// <param name="vector">The numbers.</param>
    public static float[] Unit(float[] vector)
    {
        ArgumentNullException.ThrowIfNull(vector);

        var length = MathF.Sqrt(vector.Sum(one => one * one));

        if (length == 0) return vector;

        var unit = new float[vector.Length];

        for (var d = 0; d < vector.Length; d++) unit[d] = vector[d] / length;

        return unit;
    }

    /// <summary>Releases the graph.</summary>
    public void Dispose() => _session.Dispose();
}
