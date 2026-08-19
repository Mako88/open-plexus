using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>
/// How the composition world is set up. Every number named and none defaulted.
/// </summary>
public sealed record ComposedSettings
{
    /// <summary>
    /// How many values each attribute can take.
    /// </summary>
    /// <remarks>
    /// <b>The number that decides whether the conjunction is a conjunction.</b>
    /// The question names one value of <see cref="Composed.First"/> and one of
    /// <see cref="Composed.Second"/>, and it works because only the asked-about
    /// object had both. Some other scene will have had both by coincidence, and
    /// how often is <c>scenes / values²</c> — so a small alphabet makes the
    /// conjunction ambiguous and the ambiguity grows with the run.
    /// </remarks>
    public required int Values { get; init; }

    /// <summary>
    /// How many codes one value of one attribute produces.
    /// </summary>
    /// <remarks>
    /// <b>More than one, or identity is a lookup.</b> The same reason
    /// <see cref="BindingSettings.CodesPerAttribute"/> exists. The question
    /// broadcasts every code of the value it names, exactly one of which is the
    /// code the scene happened to show.
    /// </remarks>
    public required int CodesPerValue { get; init; }

    /// <summary>
    /// Whether the front end can say which codes belong to which object.
    /// </summary>
    /// <remarks>
    /// <b>A control, and it must fail.</b> Ungrouped, the first attribute of one
    /// object pairs with the index of the other as readily as with its own, so
    /// the conjunction selects nothing.
    /// </remarks>
    public bool Segmented { get; init; }

    /// <summary>
    /// Whether each object gets a contentless index, fresh every scene.
    /// </summary>
    /// <remarks>
    /// <b>A control, and it must fail.</b> The three moments share no code but
    /// the index, so without one there is nothing whatsoever linking what was
    /// shown first to what was shown last, and no walk can compose them.
    /// </remarks>
    public bool Tagged { get; init; }
}

/// <summary>
/// One scene: three moments, two objects, and an index each.
/// </summary>
/// <remarks>
/// <b><see cref="Values"/> is indexed by attribute and then by object.</b> And that
/// pairing is what the walk has to recover. No moment contains it, because no
/// moment contains two attributes.
/// </remarks>
public sealed record Episode
{
    /// <summary>
    /// What the world shows, one collection per moment.
    /// </summary>
    /// <remarks>
    /// <b>The indexes appear in EVERY moment and the attributes in exactly
    /// one.</b> So an index persists across the scene and is live rather than
    /// starting again, while each attribute onsets and then stops — which is what
    /// makes <c>A</c>, <c>B</c> and <c>C</c> pairwise non-co-occurring while all
    /// three reach the index.
    /// </remarks>
    public required IReadOnlyList<IReadOnlyCollection<Code>> Moments { get; init; }

    /// <summary>What value each object has, by attribute and then by object.</summary>
    public required IReadOnlyList<IReadOnlyList<int>> Values { get; init; }

    /// <summary>
    /// The contentless index for each object, or empty when the world hands none
    /// out.
    /// </summary>
    public IReadOnlyList<Code> Tags { get; init; } = [];

    /// <summary>
    /// Which code belongs to which object, or null when the front end cannot say.
    /// </summary>
    /// <remarks>
    /// <b>One map for the whole scene rather than one per moment.</b> A code is
    /// looked up only when it is actually in the occasion, so entries for the
    /// moments that have not happened yet cost nothing and say nothing.
    /// </remarks>
    public IReadOnlyDictionary<Code, int>? Groups { get; init; }
}

/// <summary>
/// Two objects, three attributes apiece that are never shown together, and a
/// question that refers to an object by a CONJUNCTION rather than by an index.
/// </summary>
/// <remarks>
/// <para>
/// <b>The honest test of the binding result, and it exists because that world is
/// memorisable.</b> There, the index is grouped with the shape being asked
/// about, so the occasion under question wrote the answer down directly and a
/// lookup table scores perfectly. What it showed is that the binding can be
/// <i>represented</i>. This asks whether it can be <i>composed</i>.
/// </para>
/// <para>
/// <b>The trap it is designed around:</b> to ask about a particular object you
/// must refer to it, and any reference that touches the answer makes the answer
/// directly observed. <b>So the reference is a conjunction</b> — <i>the one that
/// was A and also B</i> — and the referring attributes never co-occur with the
/// answer.
/// </para>
/// <para>
/// <b>Three moments, two objects each, grouped by index:</b>
/// <c>{tag₀+A₀, tag₁+A₁}</c>, then <c>{tag₀+B₀, tag₁+B₁}</c>, then
/// <c>{tag₀+C₀, tag₁+C₁}</c>. <b>A, B and C never co-occur with each other</b>;
/// only an index links them, which is the senses world's trick applied to bound
/// objects.
/// </para>
/// <para>
/// <b>A memoriser scores exactly zero</b>, because <c>A₀ → C₀</c> was never
/// observed — and the values are drawn fresh every scene, so there is no lasting
/// kind for a memoriser to fall back on either.
/// </para>
/// <para>
/// <b>The answer is open rather than a forced choice, and that is not the
/// binding world's shape.</b> There the question was which of the two shapes
/// present belonged to the colour, because a forced choice isolates binding from
/// recognition. Here a forced choice would break the sharpest control: grouping
/// means <c>A₀</c> pairs only with <c>tag₀</c>, so between <c>C₀</c> and
/// <c>C₁</c> even a single attribute would lean the right way and the
/// one-attribute arm would not sit near chance.
/// Ranked over the whole alphabet, one attribute reaches every index it has ever
/// met and picks between them at random, which is the control the design needs.
/// </para>
/// </remarks>
public sealed class Composed
{
    /// <summary>The first of the two attributes a question refers with. <b>A.</b></summary>
    public const byte First = 30;

    /// <summary>The second. <b>B</b>, and the conjunction is A with B.</summary>
    public const byte Second = 31;

    /// <summary>What is asked for. <b>C</b>, and it is never shown beside A or B.</summary>
    public const byte Third = 32;

    /// <summary>
    /// Which object this is, and nothing else about it. <b>A contentless index,
    /// fresh every scene</b> — see <see cref="ComposedSettings.Tagged"/>.
    /// </summary>
    public const byte Tag = 33;

    /// <summary>The three attributes, in the order their moments happen.</summary>
    public static IReadOnlyList<byte> Attributes { get; } = [First, Second, Third];

    /// <summary>
    /// How many objects a scene holds. <b>Two, and not a dial</b> — the question
    /// is whether a conjunction can single one out, and two is where that becomes
    /// answerable.
    /// </summary>
    public const int PerScene = 2;

    private readonly ComposedSettings _settings;

    /// <summary>Draws the values, and which codes stand for them.</summary>
    private readonly Random _scenes;

    /// <summary>Scenes shown, so an index can be fresh every time.</summary>
    private long _moment;

    public Composed(ComposedSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Values, PerScene);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.CodesPerValue);

        // An ungrouped index is allowed here, and the binding world refuses it.
        // There it could only be a mistake: an index that pairs with the whole
        // scene indexes nothing, so the pair was rejected rather than measured.
        // Here that IS one of the controls the design asks for -- an index
        // reachable from both objects should fail, and a control that cannot be
        // constructed is a control nobody ran.
        _settings = settings;
        _scenes = new Random(Seeds.Apart(seed, 0xC0FF_EE01));
    }

    /// <inheritdoc cref="ComposedSettings.Values"/>
    public int Values => _settings.Values;

    /// <summary>
    /// What a blind guess is worth. <b>One in <see cref="Values"/></b>, because
    /// the answer is ranked over the whole alphabet rather than forced between
    /// the two present — see the note on this class.
    /// </summary>
    public double Chance => 1.0 / _settings.Values;

    /// <summary>Every code one value of one attribute produces.</summary>
    public IReadOnlyList<Code> Of(byte attribute, int value)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, _settings.Values);

        return Kinds.All(attribute, value, _settings.CodesPerValue);
    }

    /// <summary>Which value a code stands for, whichever attribute it came from.</summary>
    public static int Value(Code code) => Kinds.Of(code);

    /// <summary>
    /// One scene: three moments, and a pairing that no moment contains.
    /// </summary>
    public Episode Next()
    {
        // DRAWN FRESH EVERY SCENE, so there is no lasting kind. A world where
        // `A` and `C` belonged to one another across scenes would be answerable
        // by recognising the kind, and this world would be measuring that.
        var values = new List<IReadOnlyList<int>>(Attributes.Count);

        foreach (var _ in Attributes) values.Add(Distinct());

        var at = _moment++;
        IReadOnlyList<Code> tags = _settings.Tagged
            ? [.. Enumerable.Range(0, PerScene)
                .Select(obj => new Code(Tag, (ulong)((at * PerScene) + obj)))]
            : [];

        var picked = new Code[Attributes.Count][];
        for (var i = 0; i < Attributes.Count; i++)
        {
            picked[i] = [.. Enumerable.Range(0, PerScene)
                .Select(obj => Kinds.Pick(
                    Attributes[i], values[i][obj], _settings.CodesPerValue, _scenes))];
        }

        // The indexes are in every moment and each attribute in one. So an index
        // persists and the attributes flicker past it, which is what leaves A, B
        // and C pairwise unobserved while all three meet the index.
        var moments = new List<IReadOnlyCollection<Code>>(Attributes.Count);
        for (var i = 0; i < Attributes.Count; i++)
            moments.Add([.. picked[i], .. tags]);

        return new Episode
        {
            Moments = moments,
            Values = values,
            Tags = tags,
            Groups = _settings.Segmented ? Segment(picked, tags) : null,
        };
    }

    /// <summary>Two different values, drawn uniformly.</summary>
    /// <remarks>
    /// <b>Distinct, or the attribute cannot tell the objects apart</b> — and a
    /// question naming a value both objects had refers to neither.
    /// <para>
    /// Drawn from one fewer and stepped over the one already taken, rather than
    /// re-drawn until it differs: re-drawing consumes an unpredictable number of
    /// draws, which would stop the arms seeing the same stream.
    /// </para>
    /// </remarks>
    private IReadOnlyList<int> Distinct()
    {
        var first = _scenes.Next(_settings.Values);
        var second = _scenes.Next(_settings.Values - 1);
        if (second >= first) second++;

        return [first, second];
    }

    /// <summary>Which code belongs to which object, across every moment.</summary>
    private static Dictionary<Code, int> Segment(Code[][] picked, IReadOnlyList<Code> tags)
    {
        var groups = new Dictionary<Code, int>();

        for (var obj = 0; obj < tags.Count; obj++) groups[tags[obj]] = obj;

        foreach (var moment in picked)
            for (var obj = 0; obj < moment.Length; obj++)
                groups[moment[obj]] = obj;

        return groups;
    }
}
