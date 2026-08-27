using Plexus.Core.Knowledge;
using Plexus.Core.Memory;
using Plexus.Core.Representation;

namespace Plexus.Engine.Memory;

/// <summary>
/// Commitments in memory, indexed by relation, arity and scope constants.
/// </summary>
/// <remarks>
/// Settlement arrives here as a shard increment and must be idempotent: the same settlement
/// delivered twice leaves the same counts.
/// </remarks>
public sealed class SemanticStore : ISemanticStore
{
    public ValueTask AddAsync(Commitment commitment, CancellationToken ct) =>
        throw new NotImplementedException();

    public ValueTask<Commitment?> FindAsync(CommitmentId id, CancellationToken ct) =>
        throw new NotImplementedException();

    public IAsyncEnumerable<Commitment> MatchScopeAsync(
        IReadOnlyCollection<GroundFact> facts,
        CancellationToken ct) =>
        throw new NotImplementedException();

    public ValueTask ApplyAsync(Settlement settlement, CancellationToken ct) =>
        throw new NotImplementedException();
}
