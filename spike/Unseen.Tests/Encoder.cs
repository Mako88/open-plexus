using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Unseen;

/// <summary>
/// A frozen published sentence encoder, asked for one word at a time.
/// </summary>
/// <remarks>
/// <para>
/// Frozen is what makes it usable here: a published file of constants gives the same numbers
/// on every machine forever, so two holders agree about what they are looking at without
/// speaking. Nothing fits it, nothing trains it, and it is never asked what to conclude.
/// </para>
/// <para>
/// Single words only, and words that the vocabulary holds whole. Word-piece splitting is a
/// real thing this does not do, so a word that is not one token is dropped from the study and
/// said so rather than being approximated.
/// </para>
/// </remarks>
public sealed class Encoder : IDisposable
{
    private const int Cls = 101;
    private const int Sep = 102;

    private readonly InferenceSession _session;
    private readonly Dictionary<string, int> _vocab;
    private readonly string _output;

    public Encoder()
    {
        var here = Directory.GetCurrentDirectory();
        var root = Root(here);
        var dir = Path.Combine(root, "corpora", "encoders", "all-minilm-l6-v2");
        var model = Path.Combine(dir, "model.onnx");

        if (!File.Exists(model))
        {
            throw new FileNotFoundException(
                $"the encoder is not at {model}. Fetch it with:\n"
                + "    bash corpora/fetch.sh", model);
        }

        _session = new InferenceSession(model);
        _output = _session.OutputMetadata.Keys.First();

        _vocab = [];
        var line = 0;
        foreach (var token in File.ReadLines(Path.Combine(dir, "vocab.txt")))
        {
            _vocab.TryAdd(token, line);
            line++;
        }
    }

    /// <summary>How wide a reading is.</summary>
    public int Width { get; private set; } = 384;

    /// <summary>Whether the vocabulary holds this word as one token.</summary>
    public bool Knows(string word) => _vocab.ContainsKey(word);

    /// <summary>
    /// One word as a unit vector.
    /// </summary>
    /// <remarks>
    /// Mean-pooled over the three positions and then normalised, which is what the published
    /// model's own pooling does. The length is thrown away so that a direction is a direction
    /// and not a direction times how common the word is.
    /// </remarks>
    public float[] Of(string word)
    {
        if (!_vocab.TryGetValue(word, out var id))
            throw new ArgumentException($"`{word}` is not one token", nameof(word));

        var ids = new DenseTensor<long>([1, 3]);
        ids[0, 0] = Cls;
        ids[0, 1] = id;
        ids[0, 2] = Sep;

        var mask = new DenseTensor<long>([1, 3]);
        var types = new DenseTensor<long>([1, 3]);
        for (var at = 0; at < 3; at++)
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

        using var ran = _session.Run(inputs);
        var hidden = ran.First(one => one.Name == _output).AsTensor<float>();

        Width = hidden.Dimensions[^1];
        var pooled = new float[Width];

        for (var at = 0; at < 3; at++)
            for (var d = 0; d < Width; d++)
                pooled[d] += hidden[0, at, d];

        return Unit(pooled);
    }

    /// <summary>The same vector scaled to length one, or left alone where it has no length.</summary>
    public static float[] Unit(float[] vector)
    {
        ArgumentNullException.ThrowIfNull(vector);

        var length = MathF.Sqrt(vector.Sum(one => one * one));
        if (length == 0) return vector;

        var unit = new float[vector.Length];
        for (var d = 0; d < vector.Length; d++) unit[d] = vector[d] / length;
        return unit;
    }

    public void Dispose() => _session.Dispose();

    /// <summary>The repository root, found by walking up for the corpora directory.</summary>
    private static string Root(string from)
    {
        var here = new DirectoryInfo(from);

        while (here is not null)
        {
            if (Directory.Exists(Path.Combine(here.FullName, "corpora"))) return here.FullName;
            here = here.Parent;
        }

        throw new DirectoryNotFoundException($"no corpora/ directory above {from}");
    }
}
