using OpenPlexus.Codes;

namespace OpenPlexus.Learning;

/// <summary>
/// Whether a candidate has earned a name — <b>the two questions
/// <see cref="Chunk"/> learned to ask, held where a second detector can ask
/// them too.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>BOTH ARE NEEDED AND THE SECOND COST A WHOLE MECHANISM TO FIND.</b>
/// Description length weighs naming against NOT naming, and is satisfied by any
/// candidate frequent enough to be worth a symbol — including one that is frequent
/// only because both its halves are. Measured on <see cref="Worlds.Motif"/>: the
/// structured world minted 245 names for six recurring sets and <b>the pure-noise
/// control minted 715</b>. A detector finding three times more structure in noise
/// than in signal has found none.
/// </para>
/// <para>
/// <b>IT IS A NULL MODEL AND NOT A THRESHOLD.</b> The expectation is the product of
/// the marginals, which is independence and nothing more; the spread is its square
/// root, because occurrences of an independent pair are Poisson about that
/// expectation and <c>√λ</c> is the distribution's own arithmetic rather than
/// anybody's choice.
/// </para>
/// <para>
/// <b>THE THREE IS THE ONE CHOSEN NUMBER, AND IT IS BORROWED RATHER THAN
/// DERIVED.</b> Three standard errors is already this project's bar for believing a
/// difference, so a name is held to the same standard as a result. Said plainly: a
/// constant nobody derived is a refuted row's shape, and the honest defence is only
/// that it is the bar already in use. A sweep is what would settle it.
/// </para>
/// <para>
/// <b>Extracted rather than copied</b> — <c>DuplicationTests</c>'s rule, and this is
/// the one duplication that could silently stop agreeing: two detectors minting by
/// different arithmetic would grow two alphabets nobody could compare.
/// </para>
/// </remarks>
public sealed class Paying
{
    /// <summary>How often each thing has been in hand when candidates were counted.</summary>
    private readonly Dictionary<Code, int> _occurs = [];

    /// <summary>How many times candidates have been counted at all.</summary>
    private long _rounds;

    /// <summary>
    /// One round of counting, and what was in hand for it.
    /// </summary>
    /// <remarks>
    /// <b>THE MARGINALS MUST BE TAKEN OVER THE SAME POPULATION AS THE JOINT
    /// COUNTS</b>, or the null model is comparing two different worlds and the bar
    /// means nothing.
    /// </remarks>
    public void Counted(IReadOnlyCollection<Code> inHand)
    {
        ArgumentNullException.ThrowIfNull(inHand);

        _rounds++;
        foreach (var one in inHand) _occurs[one] = _occurs.GetValueOrDefault(one) + 1;
    }

    /// <summary>
    /// Whether a candidate of <paramref name="members"/> parts, seen
    /// <paramref name="count"/> times, has paid for its own storage.
    /// </summary>
    /// <remarks>
    /// <b>MINIMUM DESCRIPTION LENGTH, and every term is the world's own
    /// arithmetic.</b> Naming saves <c>members - 1</c> symbols on each of
    /// <c>count</c> occasions and costs <c>members</c> once to define.
    /// </remarks>
    public static bool Repays(long count, int members) =>
        count * (members - 1) > members;

    /// <summary>
    /// Whether a pair met more often than two independent things would.
    /// </summary>
    /// <remarks>
    /// <b>Nought rounds is not evidence of anything</b>, so nothing clears this
    /// before any counting has happened.
    /// </remarks>
    public bool Beats(long count, Code left, Code right)
    {
        if (_rounds == 0) return false;

        var expected =
            (double)_occurs.GetValueOrDefault(left) * _occurs.GetValueOrDefault(right) / _rounds;

        return count > expected + (3 * Math.Sqrt(expected));
    }
}
