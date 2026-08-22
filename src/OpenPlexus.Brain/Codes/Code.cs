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
public readonly record struct Code(byte Modality, ulong Value) : IComparable<Code>
{
    /// <summary>
    /// Modality first, then value.
    /// </summary>
    /// <remarks>
    /// Ordering exists so that anything iterating codes can do so
    /// deterministically. A dictionary's order is not stable across runs, and a
    /// result that moves with it would be a difference nobody chose.
    /// </remarks>
    public int CompareTo(Code other)
    {
        var modality = Modality.CompareTo(other.Modality);
        return modality != 0 ? modality : Value.CompareTo(other.Value);
    }
}
