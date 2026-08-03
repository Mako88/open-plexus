using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>
/// The code scheme the senses world and the binding world share: a concept
/// number, times a stride, plus which of that concept's codes this one is.
/// </summary>
/// <remarks>
/// <para>
/// <b>SEVERAL CODES PER CONCEPT, OR THE TASK IS A LOOKUP TABLE.</b> A concept
/// that showed the same single code every time would make identity trivial;
/// several is what forces <i>a concept is what you reach by walking</i> to do any
/// work. That is the whole reason the scheme is not simply the concept number.
/// </para>
/// <para>
/// <b>Both worlds wrote it out separately, down to the literal 1000</b>, along
/// with the division that reads it back. One of them changing its stride while
/// the other did not would break nothing visibly — the two worlds share no code
/// paths — and would quietly make their numbers incomparable.
/// </para>
/// </remarks>
public static class Kinds
{
    /// <summary>
    /// How far apart two concepts' code ranges sit. <b>Fixed forever</b> —
    /// changing it renumbers every code either world has ever emitted.
    /// </summary>
    private const int Stride = 1000;

    /// <summary>Every code one modality produces for one concept.</summary>
    /// <param name="modality">Which sense or attribute the codes belong to.</param>
    /// <param name="concept">Which thing they are about.</param>
    /// <param name="codes">How many codes that modality gives one concept.</param>
    public static IReadOnlyList<Code> All(byte modality, int concept, int codes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(concept);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(codes);

        return [.. Enumerable.Range(0, codes).Select(slot => At(modality, concept, slot))];
    }

    /// <summary>One of a concept's codes, drawn at random.</summary>
    /// <param name="modality">Which sense or attribute the code belongs to.</param>
    /// <param name="concept">Which thing it is about.</param>
    /// <param name="codes">How many codes that modality gives one concept.</param>
    /// <param name="rng">The world's own generator.</param>
    public static Code Pick(byte modality, int concept, int codes, Random rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentOutOfRangeException.ThrowIfNegative(concept);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(codes);

        return At(modality, concept, rng.Next(codes));
    }

    /// <summary>Which concept a code belongs to, whatever modality it came from.</summary>
    public static int Of(Code code) => (int)(code.Value / Stride);

    /// <summary>
    /// The code for a thing that arrives already named — <b>a word, a colour, a
    /// shape</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The worlds built here number their concepts and the worlds read off
    /// disk do not.</b> An external corpus hands over <i>cylinder</i> and
    /// <i>bathroom</i>, so there is no concept number to stride by and something
    /// has to turn a name into a code.
    /// </para>
    /// <para>
    /// <b>A hash and not an interning table, which is the red-ball property.</b>
    /// The same input must give the same code on every machine forever, and a
    /// table numbered by order of first appearance gives two machines reading two
    /// samples different codes for the same word. FNV-1a because
    /// <see cref="string.GetHashCode()"/> is randomised per process, so a run
    /// built on it would not reproduce itself — the property fork 12 protects.
    /// </para>
    /// </remarks>
    /// <param name="modality">Which front end the name came from.</param>
    /// <param name="name">The name, taken exactly as given.</param>
    public static Code Named(byte modality, string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var hash = 14695981039346656037UL;

        foreach (var letter in name)
        {
            hash ^= letter;
            hash *= 1099511628211UL;
        }

        return new Code(modality, hash);
    }

    private static Code At(byte modality, int concept, int slot) =>
        new(modality, (ulong)((concept * Stride) + slot));
}
