namespace OpenPlexus.Codes;

/// <summary>
/// The hash every machine has to compute the same answer from.
/// </summary>
/// <remarks>
/// <para>
/// <b>NOT <see cref="object.GetHashCode"/>, AND THAT IS THE WHOLE REASON THIS
/// EXISTS.</b> String hashing is randomised per process in .NET, so anything built
/// on it gives a different answer in every run — and this design turns on two
/// names agreeing without anyone being asked.
/// <see cref="Commitments.Commitment.Name"/> derives a commitment's identity from
/// its SCOPE, so every repair path that reaches a scope converges on one name;
/// <see cref="Commitments.Naming"/> mints the same name for the same set on every
/// machine. Both are coordination-free only while the arithmetic is fixed.
/// </para>
/// <para>
/// <b>It was written twice and `DuplicationTests` refused it</b>, which is the
/// check working as intended: two copies of a hash that must agree is the one
/// duplication that could silently stop agreeing.
/// </para>
/// <para>
/// FNV-1a over eight bytes at a time, then the splitmix64 finaliser. <b>The
/// finaliser is not decoration</b> — FNV alone leaves neighbouring values close
/// together, and codes for adjacent cells of one view are neighbouring values.
/// </para>
/// </remarks>
public static class Agreed
{
    /// <summary>The FNV-1a offset basis. Where a fold starts.</summary>
    public const ulong Basis = 0xcbf29ce484222325;

    private const ulong Prime = 0x100000001b3;

    /// <summary>Folds one value into a running hash, a byte at a time.</summary>
    public static ulong Fold(ulong hash, ulong value)
    {
        for (var shift = 0; shift < 64; shift += 8)
        {
            hash ^= (byte)(value >> shift);
            hash *= Prime;
        }

        return hash;
    }

    /// <summary>The splitmix64 finaliser, which spreads neighbours apart.</summary>
    public static ulong Mix(ulong value)
    {
        value ^= value >> 30;
        value *= 0xbf58476d1ce4e5b9;
        value ^= value >> 27;
        value *= 0x94d049bb133111eb;
        value ^= value >> 31;
        return value;
    }
}
