using Plexus.Core.Agency;
using Plexus.Core.Cognition;
using Plexus.Core.Knowledge;
using Plexus.Core.Representation;

namespace Plexus.Core.Memory;

/// <summary>
/// What identifies a single-valued piece of current state.
/// </summary>
/// <remarks>
/// <para>
/// Deviation from the skeleton document, which proposes
/// <c>StateKey(RelationId Relation, EntityId Subject)</c> and asks in question 2 whether that
/// restriction distorts the first world. It does. Section 16 requires a container relation
/// with distinct roles and a step in which the same relation arrives with permuted roles, so
/// the argument that identifies the state is not always the first one, and two facts that
/// differ in a later argument are not alternatives to each other. A single-argument key makes
/// a container that holds one thing.
/// </para>
/// <para>
/// The key is therefore the argument positions a relation is keyed on, chosen by
/// <see cref="IStateKeyPolicy"/>. That keeps the restriction visible and controlled rather
/// than hiding it in what happens to overwrite what.
/// </para>
/// </remarks>
public sealed record StateKey
{
    public required RelationId Relation { get; init; }

    public required ImmutableArray<EntityId> KeyArguments { get; init; }

    public bool Equals(StateKey? other) =>
        other is not null
        && Relation == other.Relation
        && KeyArguments.AsSpan().SequenceEqual(other.KeyArguments.AsSpan());

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Relation);
        foreach (var argument in KeyArguments) hash.Add(argument);
        return hash.ToHashCode();
    }
}

/// <summary>
/// Which argument positions of a relation identify the state.
/// </summary>
/// <remarks>
/// One relation may be single-valued in its first argument, another in its first two, and a
/// symmetric one in the sorted set of both. The policy starts fixed and becomes learned; what
/// it may not become is silence.
/// </remarks>
public interface IStateKeyPolicy
{
    ImmutableArray<int> KeyPositions(RelationId relation);
}

public enum ClaimStatus
{
    Current,
    Superseded,
    Conflicting,
}

public sealed record StateClaim(
    StateKey Key,
    GroundFact Fact,
    ObservationId Observation,
    SourceId Source,
    SequenceNumber Sequence,
    ClaimStatus Status);

/// <summary>What applying one observation changed.</summary>
public sealed record StateUpdateResult(
    ImmutableArray<StateClaim> Current,
    ImmutableArray<StateClaim> Superseded,
    ImmutableArray<StateClaim> Conflicting);

/// <summary>
/// What is the case now, keyed and single-valued.
/// </summary>
/// <remarks>
/// Two sources disagreeing leaves the key explicitly conflicting rather than letting arrival
/// order pick a winner.
/// </remarks>
public interface ICurrentState
{
    IReadOnlyCollection<StateClaim> Read(StateKey key);

    IReadOnlyCollection<GroundFact> Snapshot();

    StateUpdateResult Apply(Observation observation);
}

public sealed record EpisodeQuery(
    ImmutableArray<SourceId> Sources,
    ImmutableArray<RelationId> Relations,
    SequenceNumber? After,
    int Limit);

/// <summary>
/// What happened, in the order one source reported it.
/// </summary>
/// <remarks>
/// Append-only. A correction is a later observation linked to the earlier one; the earlier one
/// is never edited, or the record of what the machine believed at the time is gone.
/// </remarks>
public interface IEpisodeStore
{
    ValueTask AppendAsync(Observation observation, CancellationToken ct);

    IAsyncEnumerable<Observation> QueryAsync(EpisodeQuery query, CancellationToken ct);
}

/// <summary>
/// The commitments, indexed by what they are about.
/// </summary>
/// <remarks>
/// Indexed by relation, arity and the constants in the scope. Scanning the population is what
/// the index exists to stop, so a scanning implementation is a scaffold rather than a target.
/// </remarks>
public interface ISemanticStore
{
    ValueTask AddAsync(Commitment commitment, CancellationToken ct);

    ValueTask<Commitment?> FindAsync(CommitmentId id, CancellationToken ct);

    IAsyncEnumerable<Commitment> MatchScopeAsync(
        IReadOnlyCollection<GroundFact> facts,
        CancellationToken ct);

    ValueTask ApplyAsync(Settlement settlement, CancellationToken ct);
}

/// <summary>A sequence of operators that has been worth running before.</summary>
public sealed record Procedure(
    ProcedureId Id,
    ImmutableArray<FactPattern> Applicability,
    ImmutableArray<OperatorId> Steps,
    EvidenceRecord Evidence,
    Provenance Provenance);

public readonly record struct ProcedureId(SemanticId Value);

/// <summary>
/// The procedures, which stay empty in the first slice.
/// </summary>
/// <remarks>
/// The seam is kept so that consolidation has somewhere to go. Single operators are what runs
/// until a measurement asks for more.
/// </remarks>
public interface IProcedureStore
{
    IAsyncEnumerable<Procedure> FindApplicableAsync(
        WorkingCoalition coalition,
        RetrievalBudget budget,
        CancellationToken ct);
}

public readonly record struct RetrievalBudget(int MaximumArtifacts, int MaximumExpansions);

public sealed record RetrievalQuery(
    ImmutableArray<GroundFact> CurrentFacts,
    ImmutableArray<Goal> Goals,
    ImmutableArray<RelationId> RequestedRelations);

/// <summary>Why one artifact was brought back.</summary>
public enum RetrievalReason
{
    SharesRelation,
    SharesEntity,
    Neighbour,
    GoalRelevant,
}

public sealed record RetrievedArtifact(
    ArtifactId Artifact,
    double Score,
    RetrievalReason Reason);

/// <summary>
/// Bringing back a bounded set of what might bear on the moment.
/// </summary>
/// <remarks>
/// The controls stay: no retrieval, random artifacts at the same budget, structural
/// retrieval, and a learned policy once one exists. Recall and downstream decision quality are
/// measured separately, because a retriever can improve one while costing the other.
/// </remarks>
public interface IRetriever
{
    ValueTask<IReadOnlyList<RetrievedArtifact>> RetrieveAsync(
        RetrievalQuery query,
        RetrievalBudget budget,
        CancellationToken ct);
}
