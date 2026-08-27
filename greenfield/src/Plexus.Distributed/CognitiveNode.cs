using Plexus.Core.Knowledge;

namespace Plexus.Distributed;

/// <summary>
/// One holder, addressed by request rather than by mailbox.
/// </summary>
/// <remarks>
/// A concrete holder may own a channel and a loop behind this. The contract stays
/// request-scoped so that an in-process holder and a holder across a socket implement the
/// same thing, which is what makes the one-holder equivalence reading meaningful.
/// </remarks>
public interface ICognitiveNode
{
    NodeId Id { get; }

    ValueTask<NodeVote> MatchAsync(Envelope<MatchRequest> request, CancellationToken ct);

    ValueTask ApplyAsync(Envelope<SettlementDelta> delta, CancellationToken ct);
}
