namespace Unseen;

/// <summary>
/// One world's worth of vocabulary: the things it holds and the property nothing states.
/// </summary>
/// <remarks>
/// The machine is never told which list a word came from, never sees the word, and never sees
/// anything derived from the word except the frozen encoder's vector for it.
/// </remarks>
/// <param name="Name">What the property is, for the report and for nothing else.</param>
/// <param name="Positive">Things the first outcome happens to.</param>
/// <param name="Negative">Things the second outcome happens to.</param>
/// <param name="HeldOutEach">How many of each side never appear in the stream.</param>
public sealed record Study(string Name, string[] Positive, string[] Negative, int HeldOutEach);

/// <summary>
/// The ladder of properties, ordered by how much of one English has in a word.
/// </summary>
/// <remarks>
/// <para>
/// The point of the ladder is to find where the borrowed prior runs out. An encoder trained on
/// language carries the distinctions language makes, and a world whose causal structure turns
/// on something language never mentions should defeat it. Whether real worlds are more like
/// the top of this ladder or the bottom is the question the whole approach turns on.
/// </para>
/// <para>
/// The bottom two rungs share a pool on purpose, so the only difference between them is which
/// partition of the same forty words the world uses.
/// </para>
/// </remarks>
public static class Nouns
{
    public static readonly string[] Containers = ["cup", "bowl", "jar", "bucket", "pot"];

    /// <summary>Forty ordinary things, spread across kinds so no one meaning dominates.</summary>
    private static readonly string[] Mixed =
    [
        "apple", "book", "chair", "door", "egg", "fish", "glass", "hammer", "ice", "jacket",
        "key", "lamp", "mouse", "box", "cup", "drum", "engine", "farm", "gate", "house",
        "nail", "ocean", "pen", "queen", "rock", "spoon", "table", "van", "wall", "yard",
        "needle", "onion", "pot", "river", "ship", "tree", "wheel", "window", "zebra", "umbrella",
    ];

    public static readonly Study Pours = new(
        "pours",
        [
            "water", "milk", "oil", "juice", "honey", "wine", "beer", "soup",
            "coffee", "tea", "blood", "ink", "paint", "vinegar", "syrup", "cream",
            "alcohol", "acid", "fuel", "sauce",
        ],
        [
            "rock", "book", "coin", "stone", "brick", "hammer", "phone", "key",
            "chair", "shoe", "ball", "knife", "spoon", "clock", "ring", "pen",
            "card", "hat", "brush", "nail",
        ],
        HeldOutEach: 6);

    public static readonly Study Alive = new(
        "alive",
        [
            "dog", "cat", "horse", "bird", "fish", "cow", "sheep", "mouse",
            "snake", "bear", "wolf", "frog", "duck", "goat", "deer", "rabbit",
            "lion", "tiger", "monkey", "spider",
        ],
        [
            "rock", "book", "coin", "stone", "brick", "hammer", "phone", "key",
            "chair", "shoe", "ball", "knife", "spoon", "clock", "ring", "pen",
            "card", "hat", "brush", "nail",
        ],
        HeldOutEach: 6);

    /// <summary>
    /// Two properties at once: it spreads only if it pours and is thin.
    /// </summary>
    /// <remarks>
    /// The negatives are two unlike groups — thick liquids and solids — so no single fact about
    /// a thing puts it on the right side, and the thing that separates them is a conjunction.
    /// Whether that needs two regions or whether the encoder's space happens to hold the
    /// conjunction as one direction is the reading.
    /// </remarks>
    public static readonly Study Thin = new(
        "thin",
        [
            "water", "juice", "wine", "beer", "tea", "coffee", "milk", "vinegar",
            "alcohol", "blood", "ink", "acid",
        ],
        [
            "honey", "syrup", "cream", "paint", "oil", "sauce",
            "rock", "book", "coin", "stone", "brick", "hammer",
        ],
        HeldOutEach: 3);

    /// <summary>
    /// Which half of the alphabet the word starts with.
    /// </summary>
    /// <remarks>
    /// Real, learnable, and not a thing English has a word for. A sub-word model sees the
    /// letters, so a faint signal is possible and would itself be worth knowing about.
    /// </remarks>
    public static readonly Study Letter = new(
        "letter",
        [.. Mixed.Where(one => one[0] <= 'm')],
        [.. Mixed.Where(one => one[0] > 'm')],
        HeldOutEach: 5);

    /// <summary>
    /// The same forty words, split by nothing at all.
    /// </summary>
    /// <remarks>
    /// The floor of the ladder. Any partition of forty points in three hundred and eighty-four
    /// dimensions is almost certainly separable on the training half, so this measures the one
    /// thing that matters: whether a direction that fits what was seen says anything about what
    /// was not.
    /// </remarks>
    public static readonly Study Arbitrary = Split(Mixed, "arbitrary", 90210);

    public static IReadOnlyList<Study> Ladder() => [Pours, Alive, Thin, Letter, Arbitrary];

    /// <summary>
    /// Every thing in one study, with the vector the chosen labelling gives it.
    /// </summary>
    /// <remarks>
    /// The codes are identical under both labellings, so a learner that works by remembering
    /// which code did what is unaffected by the dial. Only the geometry changes, which is what
    /// makes the comparison about the geometry. Words the vocabulary does not hold whole are
    /// dropped and both sides are then trimmed to the same length, so the exam stays balanced
    /// and chance stays one half.
    /// </remarks>
    public static IReadOnlyList<Thing> All(Encoder encoder, Study study, Labelling labelling)
    {
        ArgumentNullException.ThrowIfNull(encoder);
        ArgumentNullException.ThrowIfNull(study);

        var positive = study.Positive.Where(encoder.Knows).ToList();
        var negative = study.Negative.Where(encoder.Knows).ToList();
        var each = Math.Min(positive.Count, negative.Count);

        var things = new List<Thing>();
        var code = 100;

        foreach (var (word, side) in positive.Take(each).Select(one => (one, true))
            .Concat(negative.Take(each).Select(one => (one, false))))
        {
            things.Add(new Thing(
                code++,
                word,
                labelling == Labelling.Real ? encoder.Of(word) : Arbitrarily(word, encoder.Width),
                side));
        }

        return things;
    }

    /// <summary>The container codes, which carry no vector and are never proposed over.</summary>
    public static IReadOnlyList<int> ContainerCodes() =>
        [.. Enumerable.Range(10, Containers.Length)];

    /// <summary>Split so that no thing in the exam was ever in the stream.</summary>
    public static (IReadOnlyList<Thing> Train, IReadOnlyList<Thing> Test) Held(
        IReadOnlyList<Thing> things,
        int heldOutEach,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(things);

        var rng = new Random(seed);

        var positive = things.Where(one => one.Pours).OrderBy(one => rng.Next()).ToList();
        var negative = things.Where(one => !one.Pours).OrderBy(one => rng.Next()).ToList();

        return (
            Train: [.. positive.Skip(heldOutEach), .. negative.Skip(heldOutEach)],
            Test: [.. positive.Take(heldOutEach), .. negative.Take(heldOutEach)]);
    }

    /// <summary>A partition of one pool that carries no meaning, fixed by its seed.</summary>
    private static Study Split(string[] pool, string name, int seed)
    {
        var rng = new Random(seed);
        var shuffled = pool.OrderBy(one => rng.Next()).ToArray();

        return new Study(
            name,
            [.. shuffled.Take(pool.Length / 2)],
            [.. shuffled.Skip(pool.Length / 2)],
            HeldOutEach: 5);
    }

    /// <summary>
    /// A fixed vector that means nothing, derived from the word so it is the same every run.
    /// </summary>
    /// <remarks>
    /// Stable, so a learner that remembers things still works; meaningless, so a learner that
    /// generalises over the geometry has nothing to generalise over.
    /// </remarks>
    private static float[] Arbitrarily(string word, int width)
    {
        var rng = new Random(word.Aggregate(17, (hash, letter) => (hash * 31) + letter));
        var vector = new float[width];

        for (var d = 0; d < width; d++)
            vector[d] = (float)((rng.NextDouble() * 2.0) - 1.0);

        return Encoder.Unit(vector);
    }
}
