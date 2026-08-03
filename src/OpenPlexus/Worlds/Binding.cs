using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>
/// How the binding world is set up. Every number named and none defaulted.
/// </summary>
public sealed record BindingSettings
{
    /// <summary>
    /// How many kinds of thing there are. Each one has a colour and a shape,
    /// and the two carry the same number.
    /// </summary>
    public required int Concepts { get; init; }

    /// <summary>
    /// How many codes one attribute of one concept produces.
    /// </summary>
    /// <remarks>
    /// <b>More than one, or identity is a lookup.</b> The same reason
    /// <see cref="SensesSettings.CodesPerSense"/> exists: an attribute that
    /// showed one code every time would make "which shape is this" a table read
    /// rather than a walk.
    /// </remarks>
    public required int CodesPerAttribute { get; init; }

    /// <summary>
    /// Whether a colour belongs to its own shape for the life of the world,
    /// rather than being decided scene by scene.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE CONTROL, AND IT RUNS THE OTHER WAY ROUND FROM
    /// <see cref="SensesSettings.Scrambled"/>.</b> There the control destroys
    /// structure and is expected to fail; here the control ADDS structure the
    /// counts can see, and is expected to succeed. It exists so that "scored at
    /// chance" cannot be explained by a harness that could never have scored
    /// anything.
    /// </para>
    /// <para>
    /// <b>It changes no code the world emits.</b> The scenes are drawn from the
    /// same generator in the same order either way — only which shape is
    /// *answerable for* which colour moves. That is asserted rather than
    /// described.
    /// </para>
    /// </remarks>
    public bool Bound { get; init; }
}

/// <summary>
/// One scene: two objects, each with a colour and a shape.
/// </summary>
/// <remarks>
/// <b><see cref="Colours"/> and <see cref="Shapes"/> are indexed BY OBJECT, and
/// that shared index IS the binding.</b> It is the one thing in this record that
/// <see cref="Codes"/> does not contain, which is the whole experiment.
/// </remarks>
public sealed record Scene
{
    /// <summary>
    /// What the world shows — a SET of codes, and nothing else.
    /// </summary>
    /// <remarks>
    /// <b>Ordered by concept, never by object.</b> If the objects decided the
    /// order, the sequence would smuggle the binding past the front door and
    /// every number measured here would be measuring the leak.
    /// </remarks>
    public required IReadOnlyCollection<Code> Codes { get; init; }

    /// <summary>The colour each object has.</summary>
    public required IReadOnlyList<int> Colours { get; init; }

    /// <summary>The shape each object has, at the same index as its colour.</summary>
    public required IReadOnlyList<int> Shapes { get; init; }

    /// <summary>How many objects the scene holds.</summary>
    public int Objects => Colours.Count;
}

/// <summary>
/// Two objects with attributes that swap, and a question about which belongs to
/// which.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE WORLD BUILT TO FAIL, WITH THE FAILURE PREDICTED IN ADVANCE.</b> An
/// occasion in this architecture is a SET of co-occurring codes. So a scene of a
/// red ball beside a blue box and a scene of a blue ball beside a red box produce
/// the <i>identical</i> set — <c>{red, blue, ball, box}</c> — and no amount of
/// counting, walking, compressing or scaling can separate them, because the
/// information was destroyed before anything the graph does got a chance to run.
/// </para>
/// <para>
/// <b>That is the binding problem, and this world is its smallest honest
/// statement.</b> The prediction registered before the first run is not "poorly"
/// but <b>exactly at chance</b>: the two situations are literally the same input,
/// so the system cannot do better than a coin, and a result meaningfully above
/// chance would mean the model of this architecture is wrong.
/// </para>
/// <para>
/// <b>The two arms see the same data.</b> Under <see cref="BindingSettings.Bound"/>
/// a colour keeps its shape for the life of the world, so the counts alone carry
/// the answer and the system should do well. Unbound, the pairing is drawn per
/// scene from its own generator, so the emitted stream is <i>identical</i> and
/// only the truth moves. Same input, different answer — which is the sharpest
/// form this comparison can take.
/// </para>
/// </remarks>
public sealed class Binding
{
    /// <summary>What an object looks like.</summary>
    public const byte Colour = 20;

    /// <summary>What form an object has.</summary>
    public const byte Shape = 21;

    /// <summary>
    /// How many objects a scene holds. <b>Two, and not a dial</b> — the question
    /// is whether the architecture can bind at all, and two is where that becomes
    /// answerable. More objects make the same point more expensively.
    /// </summary>
    public const int PerScene = 2;

    private readonly BindingSettings _settings;

    /// <summary>Draws the scene: which two concepts, and which codes for them.</summary>
    private readonly Random _scenes;

    /// <summary>
    /// Draws the binding, and <b>nothing else</b>.
    /// </summary>
    /// <remarks>
    /// <b>Its own generator, so the two arms see identical input.</b> Sharing one
    /// would mean the bound arm and the unbound arm diverged in what they showed
    /// as well as in what was true, and then "one succeeds and one does not"
    /// would have two explanations instead of one.
    /// </remarks>
    private readonly Random _binding;

    public Binding(BindingSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Concepts, PerScene);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.CodesPerAttribute);

        _settings = settings;

        // TWO PURPOSES, SO THE TWO STREAMS SHARE NOTHING. See Seeds.Apart for
        // what happens when they are merely offset from each other.
        _scenes = new Random(Seeds.Apart(seed, 0x9E37_79B9));
        _binding = new Random(Seeds.Apart(seed, 0x85EB_CA6B));
    }

    /// <summary>How many kinds of thing there are.</summary>
    public int Concepts => _settings.Concepts;

    /// <inheritdoc cref="BindingSettings.Bound"/>
    public bool Bound => _settings.Bound;

    /// <summary>
    /// What a blind guess is worth.
    /// </summary>
    /// <remarks>
    /// <b>One in <see cref="PerScene"/>, not one in <see cref="Concepts"/>.</b>
    /// The question is a forced choice between the shapes actually present, which
    /// is what isolates binding from recognition: a system that has no idea which
    /// shapes are in the scene would be failing at something else entirely, and
    /// that failure would be indistinguishable from this one in a wider score.
    /// </remarks>
    public static double Chance => 1.0 / PerScene;

    /// <summary>Every code one attribute of one concept produces.</summary>
    public IReadOnlyList<Code> Of(byte attribute, int concept)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(concept);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(concept, _settings.Concepts);

        return [.. Enumerable.Range(0, _settings.CodesPerAttribute)
            .Select(slot => new Code(attribute, (ulong)((concept * 1000) + slot)))];
    }

    /// <summary>Which concept a code belongs to, whichever attribute it came from.</summary>
    public static int Concept(Code code) => (int)(code.Value / 1000);

    /// <summary>
    /// One scene: two distinct concepts, their four codes, and a binding that is
    /// nowhere in those codes.
    /// </summary>
    public Scene Next()
    {
        var first = _scenes.Next(_settings.Concepts);

        // Distinct and still uniform: draw from one fewer and step over the one
        // already taken. Re-drawing until it differs would be uniform too and
        // would consume an unpredictable number of draws, which would break the
        // guarantee that both arms see the same stream.
        var second = _scenes.Next(_settings.Concepts - 1);
        if (second >= first) second++;

        var (low, high) = first < second ? (first, second) : (second, first);

        // BY CONCEPT, NEVER BY OBJECT. See Scene.Codes -- ordering by object
        // would leak the binding into the sequence.
        IReadOnlyCollection<Code> codes =
        [
            Pick(Colour, low), Pick(Colour, high),
            Pick(Shape, low), Pick(Shape, high),
        ];

        // DRAWN EITHER WAY AND USED ONLY WHEN UNBOUND, so the binding generator
        // advances identically in both arms and a future arm added between them
        // cannot silently shift one.
        var swapped = _binding.Next(2) == 1 && !_settings.Bound;

        return new Scene
        {
            Codes = codes,
            Colours = [first, second],
            Shapes = swapped ? [second, first] : [first, second],
        };
    }

    private Code Pick(byte attribute, int concept) =>
        new(attribute, (ulong)((concept * 1000) + _scenes.Next(_settings.CodesPerAttribute)));
}
