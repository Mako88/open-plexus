using Plexus.Core.Knowledge;
using Plexus.Core.Representation;

namespace Plexus.Distributed;

/// <summary>
/// Everything a holder needs to answer, carried in the question.
/// </summary>
/// <remarks>
/// A request that leaves a transient input to be read from somewhere else is a request that
/// cannot be replayed and cannot run beside another round.
/// </remarks>
public sealed record MatchRequest(
    ImmutableArray<GroundFact> Facts,
    ImmutableArray<ArtifactId> Retrieved);

/// <summary>
/// One holder's answer, and what it could not answer with.
/// </summary>
/// <remarks>
/// <see cref="MissingDependencies"/> is the difference between a holder that disagrees and a
/// holder that was not holding the artifact the question turned on.
/// </remarks>
public sealed record NodeVote(
    ImmutableArray<Prediction> Predictions,
    ImmutableArray<ArtifactId> MissingDependencies,
    EvidenceVerdict Verdict);

/// <summary>
/// A settlement and the sender's resulting counts.
/// </summary>
/// <remarks>
/// The counts are the sender's new monotone state rather than an increment to apply, so a
/// duplicate or a reordered delivery merges to the same value instead of double counting.
/// </remarks>
public sealed record SettlementDelta(
    Settlement Settlement,
    EvidenceCounts NewLocalCounts);

/// <summary>Why a holder would not answer.</summary>
/// <remarks>
/// A configuration mismatch is an explicit refusal rather than a silent difference of
/// opinion, and it is counted separately from a message that was lost.
/// </remarks>
public sealed record Refusal(
    MessageId Message,
    RefusalKind Kind,
    ConfigurationFingerprint Expected,
    ConfigurationFingerprint Received);

public enum RefusalKind
{
    ConfigurationMismatch,
    UnknownRound,
    RoundClosed,
}
