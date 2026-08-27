namespace Plexus.Core.Representation;

/// <summary>One relation over terms, some of which may be variables.</summary>
/// <remarks>
/// Equality is written out for the reason given on <see cref="GroundFact"/>.
/// </remarks>
public sealed record FactPattern
{
    public required RelationId Relation { get; init; }

    public required ImmutableArray<Term> Arguments { get; init; }

    public bool Equals(FactPattern? other) =>
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
