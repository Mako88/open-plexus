namespace Plexus.Distributed;

/// <summary>
/// One delivery, which is not the identity of what it carries.
/// </summary>
/// <remarks>
/// The same settlement may arrive under three message identities. Deduplicating on the
/// payload would drop a genuine second settlement; deduplicating on the message is what makes
/// a retry safe.
/// </remarks>
public readonly record struct MessageId(Guid Value);
