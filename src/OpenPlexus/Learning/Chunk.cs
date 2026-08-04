using OpenPlexus.Codes;

namespace OpenPlexus.Learning;

/// <summary>
/// A code minted for a set that keeps arriving whole — <b>step 3, and the thing
/// that lets the alphabet GROW.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>FORK 21 MINTS EDGES; THIS MINTS NODES.</b> Reflection writes a conclusion
/// back as an observation, so a route walked often enough becomes a direct edge —
/// but the alphabet it walks over is whatever the quantiser fixed at the start and
/// can never change. A recurring set has no name, so it is re-described from its
/// parts every single time it arrives.
/// </para>
/// <para>
/// <b>THE COST IS THE CLAIM, NOT THE ACCURACY.</b> A graph with no chunking
/// already completes a familiar set perfectly: the codes co-occur, so the counts
/// are exactly what they should be. What it cannot do is stop PAYING for it. A set
/// of size S written pairwise is S(S-1) directed entries and a fan-out of S-1 from
/// every member; one node standing for the set is S entries and a fan-out of one.
/// <see cref="Worlds.Motif.Compressed"/> is that target computed rather than
/// measured.
/// </para>
/// <para>
/// <b>THE CODE IS DERIVED FROM THE MEMBERS AND NEVER ASSIGNED.</b> A counter would
/// give two machines different codes for the same set, and the whole design rests
/// on a code meaning the same thing on every machine forever — the same property
/// that forbids a fitted codebook in step 8, and the same trick
/// <see cref="Bus.Ring"/> uses to agree on placement with nobody to ask. Hashing
/// the sorted members means two machines that independently notice the same set
/// mint the same code without exchanging a word, which is the only kind of minting
/// C1 permits.
/// </para>
/// <para>
/// <b>THE THRESHOLD IS DERIVED TOO — MINIMUM DESCRIPTION LENGTH, NOT A
/// CONSTANT.</b> Describing <c>n</c> occurrences of an S-code set costs
/// <c>n·S·log₂A</c> bits; naming it costs <c>S·log₂A</c> once to define plus
/// <c>n·log₂A</c> to use. Naming wins when <c>n(S-1) &gt; S</c>, so a set of four
/// pays for itself on its SECOND arrival and a pair never quite does. <b>Nothing
/// here was chosen</b>, which is the point: a constant nobody set doing the
/// cutting is a refuted row already.
/// </para>
/// <para>
/// <b>WHAT IT DOES NOT DO, SAID PLAINLY.</b> Only a WHOLE moment is a candidate.
/// A set embedded inside a larger moment is invisible to this, because enumerating
/// subsets is exponential and no threshold rescues that. So this finds *things that
/// keep happening identically* and not *parts that keep happening together* —
/// which is the smaller half of chunking and must not be written up as the whole
/// of it. The utility problem (Minton, SOAR) is the same boundary from the other
/// side: utility belongs per chunk, and a chunk that never recurs again is a row
/// entry earning its keep on nothing.
/// </para>
/// </remarks>
public sealed class Chunk
{
    /// <summary>
    /// The modality every minted code carries.
    /// </summary>
    /// <remarks>
    /// <b>Its own modality, so a chunk is never mistaken for a thing that was
    /// sensed.</b> A walk can be narrowed to what a front end produced, which is
    /// what <see cref="Thinking.Thought.BestOf"/> is for — and a minted code
    /// answering a question about what was SEEN would be a completion nobody could
    /// act on.
    /// </remarks>
    public const byte Minted = 200;

    /// <summary>How many times each whole-moment set has arrived.</summary>
    private readonly Dictionary<ulong, int> _seen = [];

    /// <summary>How big each of those sets was, kept for the threshold.</summary>
    private readonly Dictionary<ulong, int> _size = [];

    /// <summary>Sets that have paid for their own name.</summary>
    private readonly HashSet<ulong> _minted = [];

    private readonly Lock _gate = new();

    /// <summary>How many sets have been minted.</summary>
    public int Coined
    {
        get { lock (_gate) return _minted.Count; }
    }

    /// <summary>How many distinct whole moments have been noticed at all.</summary>
    /// <remarks>
    /// <b>THE DENOMINATOR THAT SAYS WHETHER MINTING WAS SELECTIVE.</b> A detector
    /// that mints nearly everything it sees has found no structure — it has just
    /// renamed the stream, and the compression is arithmetic rather than real.
    /// </remarks>
    public int Noticed
    {
        get { lock (_gate) return _seen.Count; }
    }

    /// <summary>
    /// Takes one moment's onsets and says whether they now have a name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Counting happens on every call and minting only crosses the threshold
    /// once</b>, so a set that keeps arriving keeps returning the same code — the
    /// code is a function of the members and nothing about the count reaches it.
    /// </para>
    /// <para>
    /// <b>A set of one is never a chunk.</b> The description-length inequality is
    /// <c>n(S-1) &gt; S</c>, which no <c>n</c> satisfies at <c>S = 1</c>; the guard
    /// is here as well so the arithmetic never has to be trusted at the boundary.
    /// </para>
    /// </remarks>
    /// <param name="onsets">What started this moment.</param>
    /// <returns>The code standing for this set, or null if it has not earned one.</returns>
    public Code? Notice(IReadOnlyCollection<Code> onsets)
    {
        ArgumentNullException.ThrowIfNull(onsets);

        if (onsets.Count < 2) return null;

        var key = Name(onsets);

        lock (_gate)
        {
            var count = _seen.GetValueOrDefault(key) + 1;
            _seen[key] = count;
            _size[key] = onsets.Count;

            if (_minted.Contains(key)) return new Code(Minted, key);

            // MINIMUM DESCRIPTION LENGTH, and every term of it is the world's own
            // arithmetic. See the note on this class.
            if ((long)count * (onsets.Count - 1) <= onsets.Count) return null;

            _minted.Add(key);
            return new Code(Minted, key);
        }
    }

    /// <summary>
    /// Whether this set already has a name, <b>without counting the look as an
    /// arrival.</b>
    /// </summary>
    /// <remarks>
    /// <b>For asking a question about a set rather than observing one.</b> A
    /// question is not evidence that the set occurred, and letting it count would
    /// mean the act of asking could mint the very chunk being asked about.
    /// </remarks>
    public Code? Named(IReadOnlyCollection<Code> codes)
    {
        ArgumentNullException.ThrowIfNull(codes);

        if (codes.Count < 2) return null;

        var key = Name(codes);

        lock (_gate) return _minted.Contains(key) ? new Code(Minted, key) : null;
    }

    /// <summary>
    /// The name of a set: <b>a pure function of its members, order-free.</b>
    /// </summary>
    /// <remarks>
    /// <b>SORTED FIRST, BECAUSE AN OCCASION IS A SET.</b> The same codes arriving
    /// in a different order are the same moment, and a hash that disagreed would
    /// mint one code per permutation — which is the alphabet growing without
    /// bound rather than growing usefully.
    /// <para>
    /// <b><see cref="Agreed"/> is the same arithmetic <see cref="Bus.Ring"/>
    /// places codes with</b>, and shared rather than copied for the reason named
    /// there: two machines must mint the same name with nothing to ask.
    /// </para>
    /// </remarks>
    private static ulong Name(IReadOnlyCollection<Code> codes)
    {
        var hash = Agreed.Basis;

        foreach (var code in codes.Order())
        {
            hash = Agreed.Fold(hash, code.Modality);
            hash = Agreed.Fold(hash, code.Value);
        }

        return Agreed.Mix(hash);
    }

    public override string ToString() => $"coined={Coined} noticed={Noticed}";
}
