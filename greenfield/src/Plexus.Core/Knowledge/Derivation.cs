using Plexus.Core.Representation;

namespace Plexus.Core.Knowledge;

public readonly record struct DerivationId(SemanticId Value);

/// <summary>One application of one operation, and what it consumed and produced.</summary>
/// <remarks>
/// <c>Operation</c> is a diagnostic name. Nothing may parse it to decide anything, and it
/// becomes a stable identifier the moment something wants to.
/// </remarks>
public sealed record Derivation(
    DerivationId Id,
    string Operation,
    ImmutableArray<ArtifactId> Inputs,
    ImmutableArray<ArtifactId> Outputs,
    ConfigurationFingerprint Configuration);
