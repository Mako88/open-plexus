namespace OpenPlexus.Worlds;

/// <summary>
/// Turns a seed number into a generator seed that is nowhere near its
/// neighbours.
/// </summary>
/// <remarks>
/// <para>
/// <b>Measured 2026-08-03, and it had already produced one false result.</b> A
/// seeded <see cref="Random"/> in .NET normalises its seed by magnitude, so
/// <c>new Random(~s)</c> <i>is</i> <c>new Random(s + 1)</c> — a "separate"
/// generator built that way is the next seed's. Worse and more general,
/// consecutive integer seeds produce streams whose per-seed statistics come out
/// far tighter than chance allows: over seeds 1..8 a fair coin's count landed in
/// 19..23 of 39, a spread of about 1.3 where the binomial gives 3.1.
/// </para>
/// <para>
/// <b>That understates every standard error taken ACROSS those seeds</b>, and
/// the binding world's first measurement read as five sigma below chance while
/// sitting exactly on it. Every sweep in this project runs seeds 1..n, so the
/// mixing belongs in one place that both the worlds and the sweep harness use.
/// </para>
/// <para>
/// <b>Deliberately not <see cref="HashCode.Combine{T1, T2}"/></b>, which is
/// randomised per process and would make a run irreproducible — the opposite of
/// what a seed is for.
/// </para>
/// </remarks>
public static class Seeds
{
    /// <summary>
    /// Mixes a seed with a purpose, so two generators built from one seed number
    /// share nothing and neighbouring seed numbers share nothing either.
    /// </summary>
    /// <param name="seed">The seed number, typically a small counter.</param>
    /// <param name="purpose">
    /// What this generator is for. <b>Two streams in one object must pass
    /// different values</b>, or they are the same stream.
    /// </param>
    public static int Apart(int seed, uint purpose)
    {
        unchecked
        {
            var mixed = (uint)seed ^ purpose;
            mixed = (mixed ^ (mixed >> 16)) * 0x7FEB_352D;
            mixed = (mixed ^ (mixed >> 15)) * 0x846C_A68B;
            mixed ^= mixed >> 16;

            // Positive, because the magnitude is all the generator reads anyway.
            return (int)(mixed & 0x7FFF_FFFF);
        }
    }
}
