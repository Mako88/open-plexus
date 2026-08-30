using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Unseen;

/// <summary>Which sense a thing is seen with.</summary>
/// <remarks>
/// A machine is bounded by what its senses carry, and the point of having more than one here
/// is to show that the bound moves when a sense is added and not when what one sense already
/// gave is recombined. Information absent from a vector cannot be recovered from that vector,
/// however cleverly it is sliced.
/// </remarks>
public enum Sense
{
    /// <summary>The frozen sentence encoder: what English knows about the word.</summary>
    Meaning,

    /// <summary>The letters: what the word looks like, and nothing about what it means.</summary>
    Spelling,

    /// <summary>Both, each normalised first so neither drowns the other.</summary>
    Both,
}

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

    /// <summary>One word through the chosen sense.</summary>
    public float[] Of(string word, Sense sense) => sense switch
    {
        Sense.Meaning => Of(word),
        Sense.Spelling => Spelling(word),
        Sense.Both => Unit([.. Of(word), .. Spelling(word)]),
        _ => throw new ArgumentOutOfRangeException(nameof(sense)),
    };

    /// <summary>
    /// A word as its letters: how many of each, and which one it starts with.
    /// </summary>
    /// <remarks>
    /// A sense rather than a label. A machine that can see the shape of a word can see its
    /// first letter, the same way one that can see a ball can see that it is round. It says
    /// nothing whatever about what the word means, which is what makes it a clean second
    /// channel rather than a hint.
    /// </remarks>
    public static float[] Spelling(string word)
    {
        ArgumentNullException.ThrowIfNull(word);

        var vector = new float[52];

        foreach (var letter in word.Where(char.IsAsciiLetterLower))
            vector[letter - 'a']++;

        if (word.Length > 0 && char.IsAsciiLetterLower(word[0])) vector[26 + word[0] - 'a'] = 1;

        return Unit(vector);
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
