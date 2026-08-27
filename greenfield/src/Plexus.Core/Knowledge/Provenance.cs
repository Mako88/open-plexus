namespace Plexus.Core.Knowledge;

using Plexus.Core.Representation;

public readonly record struct ArtifactId(SemanticId Value);

/// <summary>
/// How an artifact came to exist.
/// </summary>
/// <remarks>
/// A graph rather than a sentence. An imported claim keeps the original speaker or instrument
/// as its source; the relay, the cache and the local store are delivery and storage metadata
/// and never the author.
/// </remarks>
public sealed record Provenance(
    ImmutableHashSet<SourceId> Sources,
    ImmutableHashSet<ArtifactId> Inputs,
    DerivationId? Derivation);
