namespace OpenPlexus.Codes;

/// <summary>
/// A quantised fragment of one observation from one modality. Several codes
/// fire for the same thing.
/// </summary>
/// <remarks>
/// <para>
/// <b>Never a concept.</b> A concept is what you reach by walking, and nobody
/// holds one — decision 3. Naming this <c>ConceptCode</c> would quietly grant
/// permission for the thing the design refuses.
/// </para>
/// <para>
/// The same input produces the same code on every machine, forever. That is
/// the property the whole distributed design rests on, and it is why a
/// quantiser is built from the shared seed and never fitted to data.
/// </para>
/// </remarks>
public readonly record struct Code(byte Modality, ulong Value)
{
    /// <summary>
    /// The top <paramref name="bits"/> bits of <see cref="Value"/>.
    /// </summary>
    /// <remarks>
    /// Used only by the cluster-placement locality arm — open fork 3. LSH
    /// codes near in Hamming distance share prefixes, so hashing a prefix puts
    /// similar codes on the same machine, which is a column falling out of the
    /// addressing rather than being built. Limit: within-modality only, since
    /// two front ends never share a prefix.
    /// </remarks>
    public ulong Prefix(int bits) => throw new NotImplementedException();
}
