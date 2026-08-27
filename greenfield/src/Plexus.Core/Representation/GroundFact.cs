namespace Plexus.Core.Representation;

/// <summary>
/// One relation holding of an ordered list of entities.
/// </summary>
/// <remarks>
/// <para>
/// Roles are positional in this implementation. <c>gives</c> has three distinguishable
/// argument positions and no English name is attached to any of them. If positional roles
/// turn out to block transfer across representations, the array is replaced by learned role
/// identifiers rather than by role names.
/// </para>
/// <para>
/// Equality is written out rather than generated. A generated record equals compares
/// <see cref="ImmutableArray{T}"/> by the underlying array's object identity, so two facts
/// built from the same entities in the same order would be unequal, and the existing repo's
/// <c>DeterminismTests</c> names that exact trap.
/// </para>
/// </remarks>
public sealed record GroundFact
{
    public required RelationId Relation { get; init; }

    public required ImmutableArray<EntityId> Arguments { get; init; }

    public bool Equals(GroundFact? other) =>
        other is not null
        && Relation == other.Relation
        && Arguments.AsSpan().SequenceEqual(other.Arguments.AsSpan());

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Relation);
        foreach (var argument in Arguments) hash.Add(argument);
        return hash.ToHashCode();
    }
}
