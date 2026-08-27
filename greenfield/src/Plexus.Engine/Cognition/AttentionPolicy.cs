using Plexus.Core.Cognition;
using Plexus.Core.Memory;

namespace Plexus.Engine.Cognition;

/// <summary>
/// What of the retrieved set actually enters the round.
/// </summary>
/// <remarks>
/// Retrieval is bounded and so is the coalition, and they are bounded for different reasons:
/// one is what the store was asked for, the other is what the round can afford to think with.
/// Collapsing them hides which bound a loss came from.
/// </remarks>
public interface IAttentionPolicy
{
    ImmutableArray<RetrievedArtifact> Admit(
        IReadOnlyList<RetrievedArtifact> retrieved,
        ResourceBudget budget);
}

/// <summary>Admits the best scoring artifacts the budget allows, ties broken by identity.</summary>
public sealed class ScoreOrderedAttention : IAttentionPolicy
{
    public ImmutableArray<RetrievedArtifact> Admit(
        IReadOnlyList<RetrievedArtifact> retrieved,
        ResourceBudget budget) =>
        throw new NotImplementedException();
}
