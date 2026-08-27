using Plexus.Core.Representation;

namespace Plexus.Core.Knowledge;

public readonly record struct CommitmentId(SemanticId Value);

public readonly record struct PredictionId(SemanticId Value);

/// <summary>
/// A rule that can be wrong about something in particular.
/// </summary>
/// <remarks>
/// The identity is derived from the scope and the expectation, never from a parent identity
/// and a repair. Two holders that reach the same rule by different paths must reach the same
/// <see cref="CommitmentId"/>, or evidence for one rule is split across two.
/// </remarks>
public sealed record Commitment
{
    public required CommitmentId Id { get; init; }

    public required ImmutableArray<FactPattern> Scope { get; init; }

    public required ExpectationTemplate Expectation { get; init; }

    public required TimeRelation Timing { get; init; }

    public required Provenance Provenance { get; init; }

    public bool Equals(Commitment? other) => other is not null && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>
/// One commitment advocating one grounded expectation at one opportunity.
/// </summary>
/// <remarks>
/// The identity is the opportunity rather than the rule, which is what lets settlement be
/// idempotent without an observation becoming part of the rule's identity.
/// </remarks>
public sealed record Prediction
{
    public required PredictionId Id { get; init; }

    public required CommitmentId Commitment { get; init; }

    public required Bindings Bindings { get; init; }

    public required GroundFact ExpectedFact { get; init; }

    public required bool ExpectedToHold { get; init; }

    public required ObservationId BasedOn { get; init; }

    public bool Equals(Prediction? other) => other is not null && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}
