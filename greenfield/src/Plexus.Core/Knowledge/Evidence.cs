using Plexus.Core.Representation;

namespace Plexus.Core.Knowledge;

public readonly record struct NodeId(SemanticId Value);

/// <summary>
/// What one holder has seen of one commitment.
/// </summary>
/// <remarks>
/// Three grow-only counters. A holder increments its own shard and nobody else's, so merging
/// is a component-wise maximum and arrives at the same value under any delivery order,
/// duplication or replay.
/// </remarks>
public readonly record struct EvidenceCounts(
    ulong Supports,
    ulong Contradictions,
    ulong Abstentions)
{
    public EvidenceCounts Merge(EvidenceCounts other) => new(
        Math.Max(Supports, other.Supports),
        Math.Max(Contradictions, other.Contradictions),
        Math.Max(Abstentions, other.Abstentions));
}

/// <summary>Durable evidence for one commitment, one shard a holder.</summary>
public sealed record EvidenceRecord
{
    public required CommitmentId Commitment { get; init; }

    public required ImmutableDictionary<NodeId, EvidenceCounts> Shards { get; init; }

    public bool Equals(EvidenceRecord? other) =>
        other is not null
        && Commitment == other.Commitment
        && Shards.Count == other.Shards.Count
        && Shards.All(shard =>
            other.Shards.TryGetValue(shard.Key, out var theirs) && theirs == shard.Value);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Commitment);
        foreach (var shard in Shards.OrderBy(one => one.Key.Value.High).ThenBy(one => one.Key.Value.Low))
        {
            hash.Add(shard.Key);
            hash.Add(shard.Value);
        }

        return hash.ToHashCode();
    }
}

/// <summary>
/// What the recent past says, which is not the same claim as the durable evidence.
/// </summary>
/// <remarks>
/// A decayed or windowed reading names the observation domain it was taken under and does not
/// merge as though it were timeless. Keeping it out of <see cref="EvidenceRecord"/> is what
/// stops a regime change being averaged away.
/// </remarks>
public sealed record LocalEstimate
{
    public required CommitmentId Commitment { get; init; }

    public required ObservationDomain Domain { get; init; }

    public required double SupportRate { get; init; }

    public required int WindowSize { get; init; }
}

/// <summary>What the evidence currently licenses.</summary>
/// <remarks>
/// <see cref="Insufficient"/> is not enough looks yet.
/// <see cref="Indistinguishable"/> is enough looks and no separation from a rival, which is a
/// different reason to abstain and calls for a different move.
/// </remarks>
public enum EvidenceVerdict
{
    Insufficient,
    Supported,
    Refuted,
    Indistinguishable,
}
