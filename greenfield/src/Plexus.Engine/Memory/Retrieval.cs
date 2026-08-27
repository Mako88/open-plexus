using Plexus.Core.Memory;

namespace Plexus.Engine.Memory;

/// <summary>
/// Deterministic structural retrieval.
/// </summary>
/// <remarks>
/// Seed with the entities and relations in the present state and the active goals, fetch the
/// commitments those index, expand at most the budgeted number of neighbours, rank by
/// structural overlap, evidence verdict, goal relevance and estimated cost, and return at most
/// the budgeted number of artifacts.
/// </remarks>
public sealed class StructuralRetriever : IRetriever
{
    public ValueTask<IReadOnlyList<RetrievedArtifact>> RetrieveAsync(
        RetrievalQuery query,
        RetrievalBudget budget,
        CancellationToken ct) =>
        throw new NotImplementedException();
}

/// <summary>The floor: the coalition is whatever was already in the moment.</summary>
public sealed class NoRetriever : IRetriever
{
    public ValueTask<IReadOnlyList<RetrievedArtifact>> RetrieveAsync(
        RetrievalQuery query,
        RetrievalBudget budget,
        CancellationToken ct) =>
        throw new NotImplementedException();
}

/// <summary>
/// The control that costs the same as the real one.
/// </summary>
/// <remarks>
/// Beating no retrieval only shows that having more artifacts helps. Beating the same number
/// of arbitrary artifacts is what shows the structure was doing the work.
/// </remarks>
public sealed class RandomRetriever(int seed) : IRetriever
{
    private readonly int _seed = seed;

    public ValueTask<IReadOnlyList<RetrievedArtifact>> RetrieveAsync(
        RetrievalQuery query,
        RetrievalBudget budget,
        CancellationToken ct) =>
        throw new NotImplementedException();
}
