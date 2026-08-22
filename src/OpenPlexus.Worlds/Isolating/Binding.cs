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
    /// <b>The control, and it runs the other way round from
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

    /// <summary>
    /// Whether the front end can say which codes belong to which object —
    /// <b>step 1a, and the arm this world was built to make falsifiable.</b>
    /// </summary>
    /// <remarks>
    /// <b>It changes nothing the world emits.</b> The same four codes arrive in
    /// the same order; only the grouping alongside them appears, so the arm is
    /// still measured against bit-identical input. It rides
    /// <see cref="Codes.IQuantizer{TObservation}.Bind"/>, <b>which has had no reader
    /// since the walk went</b> — so this dial is currently a world stating a fact
    /// nothing can act on.
    /// </remarks>
    public bool Segmented { get; init; }

    /// <summary>
    /// Whether each object also gets a contentless code of its own, fresh every
    /// scene — <b>the index, and it can go in the question.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Requires <see cref="Segmented"/></b>, because an ungrouped tag pairs
    /// with everything in the scene and carries nothing at all.
    /// </para>
    /// <para>
    /// <b>This is what grouping alone could not do.</b> Grouping fixed learning
    /// and left reference unsolved: a question asked with a colour cannot say
    /// *which* object it means, and the colour's aggregate points at its own kind
    /// regardless. A tag is how you point.
    /// </para>
    /// </remarks>
    public bool Tagged { get; init; }

    /// <summary>
    /// Whether the front end declares the index for what it is — <b>a code that
    /// names this occasion and will never be seen again.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Requires <see cref="Tagged"/></b>, since there is nothing fleeting in
    /// a world that hands out no indexes.
    /// </para>
    /// <para>
    /// <b>It changes nothing the world emits</b>, and it answered the one quantity that
    /// grew without bound. The same codes arrive in the same order; the walk's
    /// rendezvous stopped writing the reverse edge, so an attribute's row no longer
    /// gained a permanent entry per scene. It rides
    /// <see cref="Codes.IQuantizer{TObservation}.Fleeting"/>, <b>which has had no
    /// reader since that rendezvous went.</b>
    /// </para>
    /// </remarks>
    public bool Fleeting { get; init; }
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

    /// <summary>
    /// Which code belongs to which object, or null when the front end cannot
    /// say. <b>This is the only place the binding is expressed as data.</b>
    /// </summary>
    public IReadOnlyDictionary<Code, int>? Groups { get; init; }

    /// <summary>
    /// The contentless index for each object, or empty when the world does not
    /// hand them out. <b>Fresh every scene,</b> so it names an occasion of an object
    /// rather than a kind of object.
    /// </summary>
    public IReadOnlyList<Code> Tags { get; init; } = [];

    /// <summary>
    /// The codes that name this occasion rather than a kind of thing, or null
    /// when the front end does not say. <b>The tags, when the world declares
    /// them</b> — see <see cref="BindingSettings.Fleeting"/>.
    /// </summary>
    public IReadOnlySet<Code>? Passing { get; init; }

    /// <summary>How many objects the scene holds.</summary>
    public int Objects => Colours.Count;
}

/// <summary>
/// Two objects with attributes that swap, and a question about which belongs to
/// which.
/// </summary>
/// <remarks>
/// <para>
/// <b>The world built to fail, with the failure predicted in advance.</b> An
/// occasion in this architecture is a SET of co-occurring codes. So a scene of a
/// red ball beside a blue box and a scene of a blue ball beside a red box produce
/// the <i>identical</i> set — <c>{red, blue, ball, box}</c> — and no amount of
/// counting, walking, compressing or scaling can separate them, because the
/// information was destroyed before anything the graph does got a chance to run.
/// </para>
/// <para>
/// <b>That is the binding problem</b>, and this world is its smallest honest
/// statement. The prediction registered before the first run is not "poorly"
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
    /// Which object this is, and nothing else about it. <b>A contentless index —
    /// see <see cref="BindingSettings.Tagged"/>.</b>
    /// </summary>
    public const byte Tag = 22;

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

    /// <summary>Scenes shown, so a tag can be fresh every time.</summary>
    private long _moment;

    public Binding(BindingSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Concepts, PerScene);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.CodesPerAttribute);

        // An ungrouped tag pairs with everything in the scene, so it would carry
        // nothing at all -- and an arm that looks distinct and is not is exactly
        // how this project has fooled itself before.
        if (settings.Tagged && !settings.Segmented)
            throw new ArgumentException(
                "a tag needs its object's group, or it pairs with the whole scene "
                + "and indexes nothing", nameof(settings));

        if (settings.Fleeting && !settings.Tagged)
            throw new ArgumentException(
                "nothing is fleeting in a world that hands out no indexes",
                nameof(settings));

        _settings = settings;

        // Two purposes, so the two streams share nothing. See Seeds.Apart for
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
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(concept, _settings.Concepts);

        return Kinds.All(attribute, concept, _settings.CodesPerAttribute);
    }

    /// <summary>Which concept a code belongs to, whichever attribute it came from.</summary>
    public static int Concept(Code code) => Kinds.Of(code);

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
        Code colourLow = Pick(Colour, low), colourHigh = Pick(Colour, high);
        Code shapeLow = Pick(Shape, low), shapeHigh = Pick(Shape, high);

        // FRESH EVERY SCENE, so a tag names this occasion of this object and
        // never a kind of object. That is the whole difference between an index
        // and a category, and it is why `seen` for a tag is always one.
        var at = _moment++;
        IReadOnlyList<Code> tags = _settings.Tagged
            ? [.. Enumerable.Range(0, PerScene).Select(obj => new Code(Tag, (ulong)((at * PerScene) + obj)))]
            : [];

        IReadOnlyCollection<Code> codes =
            [colourLow, colourHigh, shapeLow, shapeHigh, .. tags];

        // Drawn either way and used only when unbound, so the binding generator
        // advances identically in both arms and a future arm added between them
        // cannot silently shift one.
        var swapped = _binding.Next(2) == 1 && !_settings.Bound;

        IReadOnlyList<int> colours = [first, second];
        IReadOnlyList<int> shapes = swapped ? [second, first] : [first, second];

        return new Scene
        {
            Codes = codes,
            Colours = colours,
            Shapes = shapes,
            Tags = tags,
            Groups = _settings.Segmented ? Segment(colours, shapes, codes, tags) : null,
            Passing = _settings.Fleeting ? tags.ToHashSet() : null,
        };
    }

    /// <summary>
    /// Which of the four codes belongs to which object.
    /// </summary>
    /// <remarks>
    /// <b>Built from the OBJECTS and not from the codes</b>, which is the whole
    /// content of it: the code set is identical whichever way the scene is bound,
    /// and this is the one thing that differs. It is the front end saying *these
    /// two belong to the same thing* — which is what a visual index is, and it
    /// says nothing about what either of them is.
    /// </remarks>
    private Dictionary<Code, int> Segment(
        IReadOnlyList<int> colours,
        IReadOnlyList<int> shapes,
        IReadOnlyCollection<Code> codes,
        IReadOnlyList<Code> tags)
    {
        var groups = new Dictionary<Code, int>();

        for (var obj = 0; obj < tags.Count; obj++) groups[tags[obj]] = obj;

        foreach (var code in codes)
        {
            if (code.Modality == Tag) continue;

            var concept = Concept(code);
            var which = code.Modality == Colour ? colours : shapes;

            for (var obj = 0; obj < which.Count; obj++)
                if (which[obj] == concept) { groups[code] = obj; break; }
        }

        return groups;
    }

    private Code Pick(byte attribute, int concept) =>
        Kinds.Pick(attribute, concept, _settings.CodesPerAttribute, _scenes);
}
