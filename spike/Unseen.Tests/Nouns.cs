namespace Unseen;

/// <summary>
/// The vocabulary the world is built out of, and the property nothing states.
/// </summary>
/// <remarks>
/// Two lists of ordinary English nouns. The machine is never told which list a word came
/// from, never sees the word, and never sees anything derived from the word except the frozen
/// encoder's vector for it.
/// </remarks>
public static class Nouns
{
    public static readonly string[] Pours =
    [
        "water", "milk", "oil", "juice", "honey", "wine", "beer", "soup",
        "coffee", "tea", "blood", "ink", "paint", "vinegar", "syrup", "cream",
        "alcohol", "acid", "fuel", "sauce",
    ];

    public static readonly string[] Holds =
    [
        "rock", "book", "coin", "stone", "brick", "hammer", "phone", "key",
        "chair", "shoe", "ball", "knife", "spoon", "clock", "ring", "pen",
        "card", "hat", "brush", "nail",
    ];

    public static readonly string[] Containers =
    [
        "cup", "bowl", "jar", "bucket", "pot",
    ];

    /// <summary>
    /// Every thing, with the vector the chosen labelling gives it.
    /// </summary>
    /// <remarks>
    /// The codes are identical under both labellings, so a learner that works by remembering
    /// which code did what is unaffected by the dial. Only the geometry changes, which is what
    /// makes the comparison about the geometry.
    /// </remarks>
    public static IReadOnlyList<Thing> All(Encoder encoder, Labelling labelling, out string[] dropped)
    {
        ArgumentNullException.ThrowIfNull(encoder);

        var missing = new List<string>();
        var things = new List<Thing>();
        var code = 100;

        foreach (var (word, pours) in Pours.Select(one => (one, true))
            .Concat(Holds.Select(one => (one, false))))
        {
            if (!encoder.Knows(word))
            {
                missing.Add(word);
                continue;
            }

            var vector = labelling == Labelling.Real
                ? encoder.Of(word)
                : Arbitrary(word, encoder.Width);

            things.Add(new Thing(code, word, vector, pours));
            code++;
        }

        dropped = [.. missing];
        return things;
    }

    /// <summary>The container codes, which carry no vector and are never proposed over.</summary>
    public static IReadOnlyList<int> ContainerCodes() =>
        [.. Enumerable.Range(10, Containers.Length)];

    /// <summary>
    /// Split so that no thing in the exam was ever in the stream.
    /// </summary>
    /// <remarks>
    /// Balanced on both sides, so chance on the exam is one half and a machine that always
    /// says the same thing scores exactly that.
    /// </remarks>
    public static (IReadOnlyList<Thing> Train, IReadOnlyList<Thing> Test) Split(
        IReadOnlyList<Thing> things,
        int heldOutEach,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(things);

        var rng = new Random(seed);

        var pours = things.Where(one => one.Pours).OrderBy(one => rng.Next()).ToList();
        var holds = things.Where(one => !one.Pours).OrderBy(one => rng.Next()).ToList();

        return (
            Train: [.. pours.Skip(heldOutEach), .. holds.Skip(heldOutEach)],
            Test: [.. pours.Take(heldOutEach), .. holds.Take(heldOutEach)]);
    }

    /// <summary>
    /// A fixed vector that means nothing, derived from the word so it is the same every run.
    /// </summary>
    /// <remarks>
    /// The control's whole job is to be stable and meaningless. Stable, so a learner that
    /// remembers things still works; meaningless, so a learner that generalises over the
    /// geometry has nothing to generalise over.
    /// </remarks>
    private static float[] Arbitrary(string word, int width)
    {
        var rng = new Random(word.Aggregate(17, (hash, letter) => (hash * 31) + letter));
        var vector = new float[width];

        for (var d = 0; d < width; d++)
            vector[d] = (float)((rng.NextDouble() * 2.0) - 1.0);

        return Encoder.Unit(vector);
    }
}
