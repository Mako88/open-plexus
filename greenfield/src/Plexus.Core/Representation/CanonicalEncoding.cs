namespace Plexus.Core.Representation;

/// <summary>
/// The bytes an artifact is identified by.
/// </summary>
/// <remarks>
/// <para>
/// The encoder owns ordering and normalisation. Set-like inputs are written in the order of
/// their own canonical bytes; sequence-like inputs keep the order they were given.
/// </para>
/// <para>
/// Deviation from the skeleton document. The document requires versioned encodings and then
/// gives the interface no version, so a reader has no way to tell which encoder produced an
/// identity it disagrees with. The version is declared here, which turns question 8 into a
/// choice about migration rather than a missing field.
/// </para>
/// </remarks>
public interface ICanonicalEncoding<in T>
{
    /// <summary>Which encoder this is, carried into every identity it produces.</summary>
    uint Version { get; }

    void Write(T value, IBufferWriter<byte> destination);
}

/// <summary>Derives a <see cref="SemanticId"/> from canonical bytes.</summary>
/// <remarks>
/// Collision handling starts as a guard: debug and test builds keep the canonical bytes
/// beside each identity seen and fail when one identity arrives with different bytes.
/// </remarks>
public interface IContentIdentity
{
    SemanticId Of<T>(T value, ICanonicalEncoding<T> encoding);
}
