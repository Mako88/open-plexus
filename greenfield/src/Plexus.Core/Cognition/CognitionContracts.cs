using Plexus.Core.Agency;
using Plexus.Core.Knowledge;
using Plexus.Core.Memory;
using Plexus.Core.Representation;

namespace Plexus.Core.Cognition;

/// <summary>What one round is allowed to spend.</summary>
public readonly record struct ResourceBudget(
    int MaximumArtifacts,
    int MaximumExpansions,
    int MaximumPredictions);

/// <summary>
/// Everything one round is thinking with.
/// </summary>
/// <remarks>
/// Immutable and scoped to the round. It is never fleet-wide mutable state, because a holder
/// reading a current-moment field belonging to somebody else's round is how two rounds
/// exchange moments.
/// </remarks>
public sealed record WorkingCoalition(
    Observation Trigger,
    ImmutableArray<GroundFact> CurrentFacts,
    ImmutableArray<Commitment> Commitments,
    ImmutableArray<Goal> Goals,
    ImmutableArray<Procedure> Procedures,
    ResourceBudget Budget);

/// <summary>Turning matched commitments into grounded predictions.</summary>
public interface IPredictor
{
    ImmutableArray<Prediction> Predict(WorkingCoalition coalition);
}
