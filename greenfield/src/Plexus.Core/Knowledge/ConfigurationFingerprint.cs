using Plexus.Core.Representation;

namespace Plexus.Core.Knowledge;

/// <summary>
/// The settings, seed and build a run was produced under.
/// </summary>
/// <remarks>
/// Deviation from the skeleton document, and a forced one. Section 9 puts a
/// <c>ConfigurationFingerprint</c> on <see cref="Derivation"/>, which is in
/// <c>Plexus.Core</c>, and section 15 declares the type in <c>Plexus.Distributed</c>, which
/// references <c>Core</c>. That is a cycle and does not compile, so the type lives here and
/// the distributed envelope carries it rather than defining it.
/// </remarks>
public sealed record ConfigurationFingerprint(SemanticId Value);
