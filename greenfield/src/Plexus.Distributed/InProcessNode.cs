using Plexus.Core.Knowledge;
using Plexus.Engine;

namespace Plexus.Distributed;

/// <summary>
/// A holder with a mailbox, in this process.
/// </summary>
/// <remarks>
/// <para>
/// It holds no round state between calls. Everything one round needs arrives in the envelope,
/// so two rounds may be in flight at once and neither can read the other's moment.
/// </para>
/// <para>
/// A deadline closes decision collection and never evidence collection: a vote that arrives
/// after the decision was returned still moves the durable evidence, and cannot move the
/// decision.
/// </para>
/// </remarks>
public sealed class InProcessNode(
    NodeId id,
    ConfigurationFingerprint configuration,
    IIdempotencyLedger<NodeVote> ledger) : ICognitiveNode
{
    private readonly ConfigurationFingerprint _configuration = configuration;
    private readonly IIdempotencyLedger<NodeVote> _ledger = ledger;

    public NodeId Id { get; } = id;

    public ValueTask<NodeVote> MatchAsync(Envelope<MatchRequest> request, CancellationToken ct) =>
        throw new NotImplementedException();

    public ValueTask ApplyAsync(Envelope<SettlementDelta> delta, CancellationToken ct) =>
        throw new NotImplementedException();
}

/// <summary>
/// The composition that has to agree with a bare engine.
/// </summary>
/// <remarks>
/// One holder here and one local engine must produce the same ordered semantic output for
/// every supported dial and seeded world, once diagnostic stamps and transport identifiers
/// are normalised away. Semantic ordering, prediction identity, evidence and abstention are
/// not normalised away, because those are the output.
/// </remarks>
public sealed class DistributedEngine(
    IReadOnlyList<ICognitiveNode> nodes,
    EngineSettings settings)
{
    private readonly IReadOnlyList<ICognitiveNode> _nodes = nodes;
    private readonly EngineSettings _settings = settings;
}
