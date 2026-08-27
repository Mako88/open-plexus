namespace Plexus.Core.Knowledge;

/// <summary>
/// The one place a decision rule about evidence lives.
/// </summary>
/// <remarks>
/// Creation, repair, generalisation and pruning all take indefinitely many optional looks at
/// the same population, so the bar has to account for the whole family rather than for each
/// candidate's own looks. Scattering fixed thresholds through the learning classes is how
/// that accounting gets lost.
/// </remarks>
public interface IEvidencePolicy
{
    EvidenceVerdict Evaluate(
        Commitment commitment,
        EvidenceRecord durableEvidence,
        LocalEstimate currentRegime);
}

/// <summary>Comparing one prediction against what happened.</summary>
/// <remarks>
/// Pure. Writing the settlement down and incrementing a shard is a separate operation, and it
/// is the one that has to be idempotent.
/// </remarks>
public interface ISettler
{
    Settlement Settle(Prediction prediction, Observation outcome);
}

public sealed record Settlement(
    PredictionId Prediction,
    CommitmentId Commitment,
    SettlementKind Kind,
    ObservationId Outcome);

/// <summary>
/// How one prediction came out.
/// </summary>
/// <remarks>
/// <see cref="Abstention"/> is reached when the outcome could not have shown the prediction
/// wrong, which is a property of the observation domain rather than of nothing having
/// happened.
/// </remarks>
public enum SettlementKind
{
    Support,
    Contradiction,
    Abstention,
}
