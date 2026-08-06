using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Commitments;

/// <summary>What a settlement said about one commitment that fired.</summary>
public enum Outcome
{
    /// <summary>What it expected was there.</summary>
    Hit,

    /// <summary>The settlement closed and what it expected was not in it.</summary>
    Miss,

    /// <summary>
    /// The settlement could not say. <b>Neither counter moves.</b>
    /// </summary>
    /// <remarks>
    /// <b>C3 REQUIRES THIS AND A TWO-OUTCOME PRIMITIVE CANNOT HAVE IT.</b> A cluster
    /// vanishing mid-thought is normal rather than an error, so a commitment whose
    /// consequent lived on it would take a miss for a death — and with counts that
    /// only ever rise there is nothing that could take the miss back. Abstaining
    /// costs nothing precisely because a G-Counter never has to be undone.
    /// </remarks>
    Abstain,
}

/// <summary>
/// A scope that entails an expectation, and is therefore right or wrong about
/// something specific.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE ENTIRE DIFFERENCE FROM A COUNT.</b> A co-occurrence cell that mispredicts
/// becomes a slightly different number; this is WRONG ABOUT SOMETHING, and which
/// something is the whole of what can be learnt from the failure.
/// </para>
/// <para>
/// <b>THE COUNTING DID NOT GO AWAY, IT MOVED UNDER THE PREDICTION.</b> Repair has to
/// ask which code separates the misses from the hits, and answering that needs a
/// tally per code seen while firing — which is `csharp`'s `together / seen` indexed
/// by commitment rather than by node. <see cref="Separations"/> is that tally, and
/// it is the object that grows: commitments times distinct codes, where both are
/// large under population coding. The commitment itself is four fields.
/// </para>
/// <para>
/// <b>MERGE MONOTONE, DECIDE LOCAL.</b> <see cref="Hits"/>, <see cref="Misses"/> and
/// <see cref="Abstains"/> only rise, so they are G-Counters: they converge with no
/// coordinator and they are the only thing another node is ever told.
/// <see cref="Accuracy"/> is a recency-weighted estimate over what THIS node saw, it
/// never merges and never travels, and it is what decides — because a lifetime
/// average cannot track a world with no episode boundary, which is why XCS does not
/// use one. Fork 27 is whether the second one earns its keep.
/// </para>
/// </remarks>
public sealed class Commitment
{
    /// <summary>The modality a commitment's own identity lives in.</summary>
    /// <remarks>
    /// <b>A commitment is identified by a <see cref="Code"/>, which is the cheapest
    /// decision in the design.</b> It is the same type a front end emits, so a
    /// commitment can appear inside another commitment's scope — and metacognition,
    /// chaining and abstraction then need no new machinery at all. A handle or an
    /// index would special-case all three later and want a second routing scheme for
    /// the distributed half.
    /// </remarks>
    public const byte Committed = 210;

    private readonly Dictionary<Code, Separation> _separations = [];

    private double _accuracy;
    private long _seen;

    /// <param name="scope">Codes that must all be present, in any order.</param>
    /// <param name="expects">The code that should follow.</param>
    public Commitment(ImmutableArray<Code> scope, Code expects)
    {
        if (scope.IsDefaultOrEmpty)
            throw new ArgumentException("a commitment with no scope fires always", nameof(scope));

        // SORTED, BECAUSE A SCOPE IS A SET. Two scopes naming the same codes in
        // different orders are the same commitment, and canonicalising here is what
        // lets the identity below be computed from the scope rather than from the
        // path that built it.
        Scope = [.. scope.Distinct().Order()];
        Expects = expects;
        Identity = Name(Scope, expects);
    }

    /// <summary>Codes that must all be present, in <see cref="Code"/> order.</summary>
    public ImmutableArray<Code> Scope { get; }

    /// <summary>The code that should follow when <see cref="Scope"/> is satisfied.</summary>
    public Code Expects { get; }

    /// <summary>What this commitment is called, on every machine, forever.</summary>
    public Code Identity { get; }

    /// <summary>Settlements where what it expected was there.</summary>
    public long Hits { get; private set; }

    /// <summary>Settlements that closed without what it expected.</summary>
    public long Misses { get; private set; }

    /// <summary>Settlements that could not say.</summary>
    public long Abstains { get; private set; }

    /// <summary>How often it has fired and been answered.</summary>
    public long Fired => Hits + Misses;

    /// <summary>
    /// The share of answered firings that were hits, over the whole lifetime.
    /// </summary>
    /// <remarks>
    /// <b>What CONVINCES, and never what decides.</b> Both terms are G-Counters, so
    /// this converges everywhere without a coordinator — and it is also a lifetime
    /// average, so it cannot track. <see cref="Accuracy"/> decides.
    /// </remarks>
    public double Reliability => Fired == 0 ? 0.0 : Hits / (double)Fired;

    /// <summary>
    /// The recency-weighted share of firings that were hits, as this node saw them.
    /// </summary>
    /// <remarks>
    /// <b>Widrow-Hoff, which is XCS's answer and not a new one.</b> It never merges
    /// and never travels, so C1 is untouched: nothing here is another node's data.
    /// </remarks>
    public double Accuracy => _accuracy;

    /// <summary>How many firings the local estimate has seen.</summary>
    /// <remarks>
    /// <b>Separate from <see cref="Fired"/> on purpose.</b> That one counts what
    /// every node between them has seen; this counts what THIS one has, and the gap
    /// between the two is how much of a commitment's record arrived rather than
    /// happened.
    /// </remarks>
    public long Seen => _seen;

    /// <summary>The tally repair reads: per code, how often it was there in each.</summary>
    public IReadOnlyDictionary<Code, Separation> Separations => _separations;

    /// <summary>Whether every code in the scope is present.</summary>
    /// <param name="moment">What is live.</param>
    public bool Fires(IReadOnlySet<Code> moment)
    {
        ArgumentNullException.ThrowIfNull(moment);
        return Scope.All(moment.Contains);
    }

    /// <summary>Whether this commitment says everything another one says, and more.</summary>
    /// <param name="other">The commitment to compare against.</param>
    /// <remarks>
    /// <b>The test subsumption rests on.</b> A child that is more specific than its
    /// parent and at least as accurate REPLACES it — which is the one exception to
    /// never editing an existing commitment, and the only reason cheap genesis plus
    /// a child per repair is not unbounded.
    /// </remarks>
    public bool Narrows(Commitment other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Expects == other.Expects
            && Scope.Length > other.Scope.Length
            && other.Scope.All(Scope.Contains);
    }

    /// <summary>Records what a settlement said, and what was present when it fired.</summary>
    /// <param name="outcome">What the settlement said.</param>
    /// <param name="moment">What was live when it fired.</param>
    /// <param name="recency">How fast the local estimate forgets, in 0..1.</param>
    public void Settle(Outcome outcome, IReadOnlySet<Code> moment, double recency)
    {
        ArgumentNullException.ThrowIfNull(moment);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(recency);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(recency, 1.0);

        if (outcome == Outcome.Abstain)
        {
            // NOTHING ELSE MOVES, INCLUDING THE TABLE. A settlement that could not
            // say is not evidence about which code separates anything, and letting
            // it into the tally would make a run's separation depend on how often
            // the network was unwell.
            Abstains++;
            return;
        }

        var hit = outcome == Outcome.Hit;

        if (hit) Hits++; else Misses++;

        _seen++;

        // THE RUNNING AVERAGE UNTIL THERE IS ENOUGH TO FORGET, WHICH IS XCS'S OWN
        // PRACTICE AND NOT A KNOB. Widrow-Hoff from zero says a commitment that has
        // been right once is a tenth right, so a fresh commitment is indistinguishable
        // from a refuted one and loses every vote it should win. Averaging for the
        // first 1/recency firings starts it where the evidence actually is, and it
        // introduces no number that `recency` did not already fix.
        var rate = _seen <= (long)(1.0 / recency) ? 1.0 / _seen : recency;

        _accuracy += rate * ((hit ? 1.0 : 0.0) - _accuracy);

        // THE TALLY IS OVER WHAT WAS PRESENT AND NOT OVER THE SCOPE. Every scope
        // code is present in every firing by definition, so a tally over the scope
        // separates nothing; what repair needs is the codes that came ALONG.
        foreach (var code in moment)
        {
            if (Scope.Contains(code)) continue;

            _separations.TryGetValue(code, out var seen);
            _separations[code] = hit
                ? seen with { InHits = seen.InHits + 1 }
                : seen with { InMisses = seen.InMisses + 1 };
        }
    }

    /// <summary>Drops the tally, keeping everything that decides whether it fires.</summary>
    /// <remarks>
    /// <b>THE TABLE IS WHAT BLOWS UP, NOT THE COMMITMENT.</b> Four fields have to
    /// stay resident or the commitment cannot match; the per-code tally is large,
    /// and it is only needed while this is a live repair candidate. Fork 31 is
    /// whether it can go and come back without changing what fires.
    /// </remarks>
    public void Forget() => _separations.Clear();

    /// <summary>What a commitment over this scope, expecting this, is called.</summary>
    /// <param name="scope">The codes that must be present, in any order.</param>
    /// <param name="expects">The code that should follow.</param>
    /// <remarks>
    /// <para>
    /// <b>DERIVED FROM THE SCOPE ITSELF, AND NOT FROM THE PARENT PLUS THE CONDITION
    /// ADDED.</b> Both satisfy the rule that two machines must agree without
    /// speaking, but parent-plus-condition gives the SAME scope two names when two
    /// nodes reach it by adding the same pair of codes in a different order — and
    /// the plan's own sibling problem is bad enough without a commitment being able
    /// to be its own sibling. From the scope, every path that arrives converges.
    /// </para>
    /// <para>
    /// <b><see cref="Agreed"/> and never <see cref="object.GetHashCode"/></b>, which
    /// is randomised per process — a codebook resting on a library's internals would
    /// mean two machines quietly disagreeing about what a commitment IS.
    /// </para>
    /// </remarks>
    public static Code Name(ImmutableArray<Code> scope, Code expects)
    {
        if (scope.IsDefaultOrEmpty)
            throw new ArgumentException("a commitment with no scope fires always", nameof(scope));

        // THE LENGTH IS FOLDED IN FIRST so that no scope can be the prefix of
        // another and reach the same name.
        var hash = Agreed.Fold(Agreed.Basis, (ulong)scope.Length);

        foreach (var code in scope.Distinct().Order())
        {
            hash = Agreed.Fold(hash, code.Modality);
            hash = Agreed.Fold(hash, code.Value);
        }

        hash = Agreed.Fold(hash, expects.Modality);
        hash = Agreed.Fold(hash, expects.Value);

        return new Code(Committed, Agreed.Mix(hash));
    }
}

/// <summary>How often one code was there when a commitment hit, and when it missed.</summary>
/// <remarks>
/// <b>The two-by-two repair reads, one code at a time.</b> The other row — how often
/// the code was ABSENT in each — is the commitment's own hit and miss counts minus
/// these, so it is never stored.
/// </remarks>
public readonly record struct Separation
{
    /// <summary>Firings that hit with this code present.</summary>
    public long InHits { get; init; }

    /// <summary>Firings that missed with this code present.</summary>
    public long InMisses { get; init; }
}
