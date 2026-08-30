namespace Unseen;

/// <summary>Whether a thing's vector carries what English knows about it.</summary>
/// <remarks>
/// A dial on an instrument rather than a switch on a target, which is what makes it legal.
/// The two runs are the same world, the same difficulty and the same number of things; the
/// only difference is whether the vectors mean anything.
/// </remarks>
public enum Labelling
{
    /// <summary>Real nouns, read by the frozen encoder.</summary>
    Real,

    /// <summary>The same nouns, given fixed vectors drawn from nothing.</summary>
    Opaque,
}

/// <summary>One thing in the world: a code, a vector, and whether it pours.</summary>
public sealed record Thing(int Code, string Word, float[] Vector, bool Pours);

/// <summary>
/// One step of the world, as the set of codes that were true.
/// </summary>
/// <remarks>
/// <see cref="Other"/> is the second argument where the world has one, and it is carried
/// beside the moment rather than inside it on purpose. The moment is a set of codes and a set
/// cannot say which of two things was the subject; a mechanism that needs to know has to be
/// handed it, and needing to be handed it is the finding rather than a convenience.
/// </remarks>
public sealed record Step(int[] Now, int Next, Thing Subject, Thing? Other = null);

/// <summary>
/// A world where what happens depends on an unstated property of the thing.
/// </summary>
/// <remarks>
/// <para>
/// Something is put in a container. If it pours it spreads, and if it does not it ends up
/// inside. Nothing ever says which things pour, and no fact of the world names the property.
/// </para>
/// <para>
/// The whole experiment is the held-out set. A machine that remembers which particular things
/// spilled cannot answer for a thing it has never met, and a machine that worked out what the
/// spilling ones have in common can.
/// </para>
/// </remarks>
public sealed class World
{
    public const int Put = 1;
    public const int Spread = 2;
    public const int Inside = 3;

    private readonly IReadOnlyList<Thing> _things;
    private readonly IReadOnlyList<int> _containers;

    public World(IReadOnlyList<Thing> things, IReadOnlyList<int> containers)
    {
        _things = things;
        _containers = containers;
    }

    /// <summary>A run of the world, of a given length, from a given seed.</summary>
    public IEnumerable<Step> Steps(int count, int seed)
    {
        var rng = new Random(seed);

        for (var at = 0; at < count; at++)
        {
            var thing = _things[rng.Next(_things.Count)];
            var container = _containers[rng.Next(_containers.Count)];

            yield return new Step(
                Now: [Put, thing.Code, container],
                Next: thing.Pours ? Spread : Inside,
                Subject: thing);
        }
    }

    /// <summary>Every thing crossed with every container, which is the whole held-out exam.</summary>
    public IEnumerable<Step> Exam()
    {
        foreach (var thing in _things)
            foreach (var container in _containers)
                yield return new Step(
                    Now: [Put, thing.Code, container],
                    Next: thing.Pours ? Spread : Inside,
                    Subject: thing);
    }
}
