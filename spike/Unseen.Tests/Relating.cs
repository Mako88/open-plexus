namespace Unseen;

/// <summary>
/// A world where the answer belongs to the pair and not to either thing.
/// </summary>
/// <remarks>
/// <para>
/// Two things are compared and the bigger one wins. No fact about one of them settles it, so a
/// region over the subject alone cannot answer however good the encoder is -- which is the
/// point. This is the world that decides whether the representation needs roles.
/// </para>
/// <para>
/// The exam is balanced for every held-out subject: each is asked against as many smaller
/// training things as larger ones. A subject-only rule therefore scores exactly one half by
/// construction rather than by luck, and anything above that came from the pair.
/// </para>
/// </remarks>
public sealed class Compared(IReadOnlyList<Thing> things)
{
    public const int Weigh = 1;
    public const int Over = 2;
    public const int Under = 3;

    private readonly IReadOnlyList<Thing> _things = things;

    /// <summary>A run of random ordered pairs.</summary>
    public IEnumerable<Step> Steps(int count, int seed)
    {
        var rng = new Random(seed);

        for (var at = 0; at < count; at++)
        {
            var left = rng.Next(_things.Count);
            var right = rng.Next(_things.Count);
            if (left == right) continue;

            yield return Ask(_things[left], _things[right]);
        }
    }

    /// <summary>
    /// Every held-out thing against equal numbers of smaller and larger training things.
    /// </summary>
    /// <remarks>
    /// The balance is what makes the reading interpretable. Asked about unbalanced pairs, a
    /// rule that knows only how big the subject is scores well above chance without having
    /// represented a relation at all.
    /// </remarks>
    public static IEnumerable<Step> Exam(
        IReadOnlyList<Thing> held,
        IReadOnlyList<Thing> train,
        int each,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(train);

        var rng = new Random(seed);

        foreach (var subject in held)
        {
            var smaller = train.Where(one => one.Code < subject.Code)
                .OrderBy(one => rng.Next()).ToList();
            var larger = train.Where(one => one.Code > subject.Code)
                .OrderBy(one => rng.Next()).ToList();

            var take = Math.Min(each, Math.Min(smaller.Count, larger.Count));

            foreach (var other in smaller.Take(take).Concat(larger.Take(take)))
                yield return Ask(subject, other);
        }
    }

    /// <summary>One comparison, as the moment it produces and the answer it has.</summary>
    private static Step Ask(Thing subject, Thing other) => new(
        Now: [Weigh, subject.Code, other.Code],
        Next: subject.Code > other.Code ? Over : Under,
        Subject: subject,
        Other: other);
}
