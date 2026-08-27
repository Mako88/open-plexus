using Plexus.Core.Agency;
using Plexus.Core.Representation;

namespace Plexus.Core.Knowledge;

public readonly record struct SourceId(SemanticId Value);

public readonly record struct ObservationId(SemanticId Value);

public readonly record struct SequenceNumber(ulong Value);

/// <summary>How a fact was come by.</summary>
/// <remarks>
/// Observing and intervening are different evidence sources even where the resulting fact is
/// the same fact. Losing that distinction is what makes a correlation look like a cause.
/// </remarks>
public enum AcquisitionKind
{
    Observed,
    Intervened,
    Told,
    Derived,
}

/// <summary>
/// What one source reported at one of its moments.
/// </summary>
/// <remarks>
/// Ordering is per source and monotone. Wall-clock stamps may ride along on a distributed
/// envelope for diagnosis, and they never establish a global order.
/// </remarks>
public sealed record Observation
{
    public required ObservationId Id { get; init; }

    public required SourceId Source { get; init; }

    public required SequenceNumber Sequence { get; init; }

    public required AcquisitionKind Acquisition { get; init; }

    public required ImmutableArray<GroundFact> Facts { get; init; }

    /// <summary>Which relations this observation reports exhaustively.</summary>
    /// <remarks>
    /// Deviation from the skeleton document. Section 7 says a world must report a closed
    /// observation domain before a failure to observe can settle <c>DoesNotHold</c>, and then
    /// nothing on the observation carries one, which leaves negation and abstention with no
    /// way to be settled or refused. The field is the smallest thing that makes those two
    /// falsifiable rather than a promise.
    /// </remarks>
    public required ObservationDomain Domain { get; init; }

    public ActionId? Intervention { get; init; }

    public bool Equals(Observation? other) => other is not null && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>
/// The relations an observation is complete about.
/// </summary>
/// <remarks>
/// A relation named here was looked at and every fact of it that held was reported, so a fact
/// of that relation not present is absent rather than unknown. A relation not named is
/// unknown, and an expectation of absence over it settles as an abstention.
/// </remarks>
public sealed record ObservationDomain
{
    /// <summary>Nothing is reported exhaustively, which is the safe default.</summary>
    public static readonly ObservationDomain Open = new() { ClosedRelations = [] };

    public required ImmutableArray<RelationId> ClosedRelations { get; init; }

    public bool Covers(RelationId relation) => ClosedRelations.Contains(relation);

    public bool Equals(ObservationDomain? other) =>
        other is not null
        && ClosedRelations.AsSpan().SequenceEqual(other.ClosedRelations.AsSpan());

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var relation in ClosedRelations) hash.Add(relation);
        return hash.ToHashCode();
    }
}

/// <summary>
/// When an expectation comes due, relative to what advocated it.
/// </summary>
/// <remarks>
/// Three values is machinery rather than an ontology of time. It grows when a world shows
/// something it cannot express, and not before.
/// </remarks>
public enum TimeRelation
{
    SameMoment,
    NextMoment,
    Eventually,
}
