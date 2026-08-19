namespace OpenPlexus.Codes;

/// <summary>
/// One observation said at SEVERAL GRAINS at once — <b>step 8's middle, and the
/// only likeness a walk can use that the graph did not compute.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The problem, measured three ways.</b> A credit cell keyed on the state that
/// earned it cannot cover a state count that keeps growing — and on `Tending`,
/// quadrupling a run grows the distinct states more than three times over while
/// the credit cells grow less than half again. <b>Coverage does not stall, it
/// falls.</b> Experience never accumulates while every state is its own island.
/// </para>
/// <para>
/// <b>And step 9 ruled out fixing it inside the graph.</b> Every likeness the
/// graph can derive is made of co-occurrence, and in a body co-occurrence records
/// the policy that produced it — so a walk wide enough to stop being silent
/// converges on whatever was done most. <b>A likeness has to come from OUTSIDE the
/// counting, which means the front end.</b>
/// </para>
/// <para>
/// <b>The move: say the same observation coarsely as well as finely</b>, and put both
/// in the occasion. Two states that differ finely still share their coarse
/// code, so they meet in the graph without anything having to decide they are
/// similar. <b>The hierarchy IS the similarity</b>, and no metric, no distance and
/// no comparison is needed anywhere.
/// </para>
/// <para>
/// <b>IT IS THE <c>Groups</c> TRICK A FOURTH TIME.</b> Segmentation, order and now
/// grain: the front end says something structural INSIDE the occasion, where C2
/// cannot touch it, and the graph is left to find out what it means. <b>And it is
/// the one of the four that asks least</b> — a coarser reading of a signal is not
/// a fact about the world at all, it is the same fact with bits dropped, which any
/// front end can do to its own output without knowing anything.
/// </para>
/// <para>
/// <b>Why this is not a fitted codebook.</b> Nothing is learnt, nothing is
/// sampled, and two machines handed the same observation drop the same bits — so
/// the red-ball property holds exactly as it does for a hash. <b>What a fitted
/// quantiser buys is spending codes where the data is; what this buys is spending
/// codes at more than one SCALE</b>, which is the half that generalisation needs
/// and the half that does not require seeing the data first.
/// </para>
/// <para>
/// <b>A DELETED <c>Code.Prefix</c> ALREADY ANTICIPATED IT.</b> Its note said LSH
/// codes near in Hamming distance share prefixes — which is exactly this, for a
/// front end whose codes come from hyperplanes rather than from bands. <b>Nothing
/// ever called it</b>: it was written for fork 3, which is still open, and the
/// idea arrived here by another road instead. Three lines to write again if fork 3
/// ever wants them.
/// </para>
/// </remarks>
public static class Grains
{
    /// <summary>
    /// The same value read at <paramref name="grains"/> coarsenesses, each on its
    /// own modality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A modality per grain, or a coarse code and a fine one collide.</b> Band
    /// 1 of 2 and band 1 of 8 are different claims about the world, and one code
    /// meaning two things is this design's recurring fault at its most literal.
    /// </para>
    /// <para>
    /// <b>HALVING, so each grain is the one above with a bit dropped.</b> That
    /// makes the coarse reading a strict summary of the fine one rather than a
    /// second opinion about it — two observations sharing a fine code always share
    /// every coarse code, which is what makes the shared node mean <i>alike</i> and
    /// not merely <i>adjacent</i>.
    /// </para>
    /// </remarks>
    /// <param name="modality">
    /// The finest grain's modality. Grain <c>g</c> rides on <c>modality + g</c>,
    /// <b>so the caller owns that block</b> exactly as it owns one modality today.
    /// </param>
    /// <param name="value">Which band the observation fell in, at the finest grain.</param>
    /// <param name="bands">How many bands the finest grain has.</param>
    /// <param name="grains">
    /// How many readings to emit. <b>One is the finest alone</b>, which is every
    /// measurement taken before this existed — so a caller that does not ask
    /// gets exactly the old behaviour.
    /// </param>
    public static IEnumerable<Code> Of(byte modality, int value, int bands, int grains)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bands);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(grains);

        for (var grain = 0; grain < grains; grain++)
        {
            // Each grain is the one before it with a bit dropped. Integer division
            // rather than a shift so the arithmetic reads as what it is: the same
            // reading, coarser.
            var coarse = value >> grain;

            yield return new Code((byte)(modality + grain), (ulong)coarse);

            // NOTHING FINER TO SAY. Once the whole range collapses to one band the
            // reading carries no information, and emitting it would put a code in
            // every occasion this front end ever produces -- a background the
            // weighting refuses anyway, bought at the price of a row entry
            // everywhere.
            if (bands >> grain <= 1) yield break;
        }
    }

    /// <summary>
    /// How many modalities <see cref="Of"/> will use, so a caller can lay out its
    /// blocks.
    /// </summary>
    public static int Spans(int bands, int grains)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bands);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(grains);

        var used = 0;

        for (var grain = 0; grain < grains; grain++)
        {
            used++;
            if (bands >> grain <= 1) break;
        }

        return used;
    }
}
