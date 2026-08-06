using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Learning;

/// <summary>
/// A name for a SEQUENCE that keeps recurring — <b><see cref="Chunk"/>'s sibling,
/// and the temporal abstraction the plan asks for.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A CHUNK NAMES A SET AND A MACRO NAMES AN ORDER, AND THE DIFFERENCE IS NOT
/// THE HASH.</b> The plan carried this as "a sibling whose name is derived from
/// members IN ORDER rather than sorted — everything else carries over", and that
/// understated it: naming in order is one line. What does not carry over is where
/// the CANDIDATES come from. <see cref="Chunk"/> reads the codes of ONE moment and
/// merges pairs within it; a sequence spans several moments, so the pairs here are
/// consecutive rather than co-occurring — the same <c>past → onset</c> pairs the
/// rendezvous writes as <see cref="Graph.Kind.After"/>.
/// </para>
/// <para>
/// <b>WHAT DOES CARRY OVER IS THE GATE, AND IT IS SHARED RATHER THAN COPIED.</b>
/// See <see cref="Paying"/>: description length AND beating chance, because MDL
/// alone minted 715 names on pure noise. A detector that mints on order needs the
/// second bar at least as much — consecutive pairs of two common codes are the
/// commonest accident a stream produces.
/// </para>
/// <para>
/// <b>A TWO-SYMBOL WINDOW, WHICH COMPOSES WITHOUT A CAP NOBODY SET.</b> Greedy
/// left-to-right merging holds one symbol in hand: the next code either merges with
/// it into a name, or replaces it. So <c>a→b</c> mints <c>M₁</c>, and <c>M₁→c</c>
/// then mints <c>M₂</c> covering all three — arbitrarily long sequences are reached
/// by repeated pairing, exactly as byte-pair encoding reaches long tokens, and
/// nothing has to choose how long a macro may be.
/// </para>
/// <para>
/// <b>IT IS THE SAME MOVE SEQUITUR MAKES</b>, and the reason to prefer pair-merging
/// over counting whole n-grams is the one already recorded for
/// <see cref="Chunk"/>: pair-merging COMPOSES, so the parts of a long sequence are
/// themselves nameable and a name is reached the same way from either end.
/// </para>
/// </remarks>
public sealed class Macro
{
    /// <summary>
    /// The modality every minted macro carries.
    /// </summary>
    /// <remarks>
    /// <b>Its own, and distinct from <see cref="Chunk.Minted"/>.</b> A set and an
    /// order are different claims about a world, and a walk narrowed to one must
    /// not reach the other — the same argument that gives a chunk a modality apart
    /// from anything sensed.
    /// </remarks>
    public const byte Made = 202;

    /// <summary>How many times each ordered candidate has been seen.</summary>
    private readonly Dictionary<ulong, int> _seen = [];

    /// <summary>What each candidate covers, <b>in order</b>.</summary>
    private readonly Dictionary<ulong, ImmutableArray<Code>> _members = [];

    /// <summary>Sequences that have paid for their own name.</summary>
    private readonly HashSet<ulong> _minted = [];

    /// <summary>How often each name has actually stood in for something.</summary>
    /// <remarks>
    /// <b>A minted name never folded has never entered the graph</b>, exactly as in
    /// <see cref="Chunk"/> — it passed a threshold and then never recurred.
    /// </remarks>
    private readonly Dictionary<ulong, int> _used = [];

    /// <inheritdoc cref="Paying"/>
    private readonly Paying _paying = new();

    /// <summary>The one symbol in hand, and what it covers in order.</summary>
    private Code? _held;
    private ImmutableArray<Code> _covers = [];

    private readonly Lock _gate = new();

    /// <summary>How many sequences have been minted.</summary>
    public int Coined
    {
        get { lock (_gate) return _minted.Count; }
    }

    /// <summary>How many distinct candidates have been noticed at all.</summary>
    /// <remarks>
    /// <b>The denominator that says whether minting was SELECTIVE.</b> A detector
    /// naming most of what it sees has found no structure — it has renamed the
    /// stream.
    /// </remarks>
    public int Noticed
    {
        get { lock (_gate) return _seen.Count; }
    }

    /// <summary>
    /// How many minted names repaid their own definition in USES.
    /// </summary>
    /// <remarks>
    /// <b>The utility problem, and it needs no new constant</b> — Minton and SOAR.
    /// Minting asks whether a sequence recurs often enough to be worth a symbol;
    /// KEEPING it asks whether the symbol was then used often enough, which is the
    /// same inequality counted on uses. See <see cref="Chunk.Applied"/>.
    /// </remarks>
    public int Applied
    {
        get
        {
            lock (_gate)
                return _used.Count(one => Paying.Repays(one.Value, _members[one.Key].Length));
        }
    }

    /// <summary>
    /// One more code arrived after the last.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Returns the macro that JUST COMPLETED, or null.</b> A sequence is only
    /// recognisable at its end — nothing about seeing <c>a</c> says whether
    /// <c>a→b</c> is happening — so this can say nothing until the last member
    /// lands, which is what makes a macro different from a chunk in use and not
    /// only in derivation.
    /// </para>
    /// <para>
    /// <b>The pair is counted whether or not it is minted</b>, because a candidate
    /// that has not yet paid still has to accumulate the evidence that it might.
    /// </para>
    /// <para>
    /// <b>THE MARGINALS ARE OVER THE STREAM AND NOT OVER THE PAIR, WHICH IS THE
    /// DIFFERENCE BETWEEN A NULL MODEL AND A DEAD ONE.</b> Counting only the two
    /// symbols being paired makes every marginal equal the round count, so the
    /// expectation saturates and a two-symbol stream can never beat chance however
    /// perfectly it alternates. One round per arrival is what makes
    /// <c>occurs(a)·occurs(b)/rounds</c> the expected number of ACCIDENTAL
    /// adjacencies, which is the quantity the bar is about.
    /// </para>
    /// </remarks>
    /// <param name="next">The code that has just started.</param>
    public Code? Notice(Code next)
    {
        lock (_gate)
        {
            if (_held is not { } held)
            {
                _held = next;
                _covers = [next];
                _paying.Counted([next]);
                return null;
            }

            var members = _covers.Add(next);
            var key = Name(members);

            var count = _seen.GetValueOrDefault(key) + 1;
            _seen[key] = count;
            _members[key] = members;

            if (!_minted.Contains(key)
                && Paying.Repays(count, members.Length)
                && _paying.Beats(count, held, next))
                _minted.Add(key);

            if (!_minted.Contains(key))
            {
                // NO NAME, SO THE WINDOW MOVES ON. What was held is finished with;
                // it cannot join anything further back, because nothing further
                // back is still in hand.
                _held = next;
                _covers = [next];
                _paying.Counted([next]);
                return null;
            }

            // THE NAME STANDS IN FOR BOTH, AND STAYS IN HAND. That is what lets the
            // next code extend it rather than start again, and it is the whole of
            // how a two-symbol window reaches a sequence of any length.
            var made = new Code(Made, key);

            _used[key] = _used.GetValueOrDefault(key) + 1;
            _held = made;
            _covers = members;

            // ONE ROUND, AND THE MACRO IS IN HAND FOR IT AS MUCH AS THE CODE THAT
            // COMPLETED IT. Without the macro's own marginal the next pairing
            // divides by nought, `Beats` waves it through on the description-length
            // bar alone, and the 715-names failure comes back one level up.
            _paying.Counted([next, made]);

            return made;
        }
    }

    /// <summary>
    /// Forgets what is in hand. <b>A break in the stream, not a reset.</b>
    /// </summary>
    /// <remarks>
    /// <b>What has been LEARNT is untouched</b> — counts only ever rise, and a gap
    /// in the stream is not evidence against a sequence. What it drops is the
    /// half-built sequence, so a macro is never minted across a discontinuity
    /// nobody claimed was continuous.
    /// </remarks>
    public void Broke()
    {
        lock (_gate)
        {
            _held = null;
            _covers = [];
        }
    }

    /// <summary>What a minted macro stands for, <b>in order</b>.</summary>
    /// <remarks>
    /// <b>Empty for a code this detector never minted</b>, which is every code
    /// that is not a macro.
    /// </remarks>
    public ImmutableArray<Code> Members(Code made)
    {
        lock (_gate)
            return made.Modality == Made && _members.TryGetValue(made.Value, out var members)
                ? members
                : [];
    }

    /// <summary>
    /// The name of one ordered sequence.
    /// </summary>
    /// <remarks>
    /// <b>IN ORDER AND NEVER SORTED, WHICH IS THE ONE LINE THAT SEPARATES THIS FROM
    /// <see cref="Chunk"/>.</b> <c>a then b</c> and <c>b then a</c> are different
    /// facts about a world, where <c>{a, b}</c> is one fact however it is written —
    /// so <see cref="Chunk"/> sorts before folding and this must not.
    /// <para>
    /// <b>Derived by the same arithmetic every code here agrees by</b>, so two
    /// machines that independently notice the same sequence mint the same name with
    /// nothing to ask. See <see cref="Stated.Instance"/>, which folds in order for
    /// the same reason.
    /// </para>
    /// </remarks>
    private static ulong Name(IReadOnlyList<Code> members)
    {
        var hash = Agreed.Basis;

        for (var at = 0; at < members.Count; at++)
        {
            hash = Agreed.Fold(hash, members[at].Modality);
            hash = Agreed.Fold(hash, members[at].Value);
        }

        return Agreed.Mix(hash);
    }

    public override string ToString() => $"coined={Coined} noticed={Noticed}";
}
