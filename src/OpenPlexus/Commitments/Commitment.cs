using System.Collections.Immutable;
using System.Runtime.InteropServices;
using OpenPlexus.Codes;

namespace OpenPlexus.Commitments;

/// <summary>What a settlement said about one commitment that fired.</summary>
public enum Verdict
{
    /// <summary>What it expected was there.</summary>
    Hit,

    /// <summary>The settlement closed and what it expected was not in it.</summary>
    Miss,

    /// <summary>
    /// The settlement could not say. <b>Neither counter moves.</b>
    /// </summary>
    /// <remarks>
    /// <b>C3 requires this and a two-outcome primitive cannot have it.</b> A cluster
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
/// <b>The entire difference from a count.</b> A co-occurrence cell that mispredicts
/// becomes a slightly different number; this is WRONG ABOUT SOMETHING, and which
/// something is the whole of what can be learnt from the failure.
/// </para>
/// <para>
/// <b>The counting did not go away, it moved under the prediction.</b> Repair has to
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

    /// <summary>
    /// How many buckets the distinct-occasion register has.
    /// </summary>
    /// <remarks>
    /// <b>One word per commitment, and the width is the word rather than a choice.</b>
    /// An exact set of the occasions a commitment has fired in is commitments times
    /// distinct moments — the same product that already makes
    /// <see cref="Separations"/> the thing that blows up, arriving a second time for a
    /// statistic that only has to tell A FEW from MANY. Sixty-four is how many bits are
    /// in the register, so there is no size here anybody chose and none to hunt.
    /// </remarks>
    private const int Buckets = 64;

    private readonly Dictionary<Code, Separation> _separations = [];

    private double _accuracy;
    private long _seen;
    private ulong _witness;

    /// <param name="scope">Codes that must all be present, in any order.</param>
    /// <param name="expects">The code that should follow.</param>
    public Commitment(ImmutableArray<Code> scope, Code expects)
    {
        if (scope.IsDefaultOrEmpty)
            throw new ArgumentException("a commitment with no scope fires always", nameof(scope));

        // Sorted, because a scope is a set. Two scopes naming the same codes in
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

    /// <summary>
    /// How many DISTINCT moments it has fired in, estimated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A firing is not an observation when the world draws the same thing twice.</b>
    /// <see cref="Fired"/> counts how often a commitment was answered;
    /// this counts how much of what it was answered about was DIFFERENT. Where a world
    /// redraws from a bag the two come apart by exactly the recurrence — and every
    /// proportion this design tests is a claim about independent evidence, which the
    /// first number is not.
    /// </para>
    /// <para>
    /// <b>The only measure of generality here that is not built from accuracy, and what
    /// it was built for is refuted.</b> Every repair gate tried — the vote being wrong, a
    /// failure nothing covers, forking having paid — is computed from observed accuracy
    /// or observed failure, and the story was that on a world drawing with replacement
    /// all of them are fooled by children standing on one repeated moment. Subsuming on
    /// THIS instead of on firings said otherwise; see the revival row in the plan.
    /// </para>
    /// <para>
    /// <b>It is kept as an instrument because it settled something no other number
    /// could.</b> On <see cref="Worlds.Arranged"/> a subsumption rule weighing advantage
    /// against THIS deleted an ordinary 83% of children — the same share as the rules
    /// that weigh it against firings — and landed on the identical withheld score, seed
    /// for seed. The children that sink that world are not memorised. That is a negative
    /// nothing in <see cref="Separations"/> or the counters could have produced.
    /// </para>
    /// <para>
    /// <b>Linear counting over a single word, which is exact where it matters and
    /// saturates where it does not.</b> Each occasion sets one bit; the count is
    /// recovered from how many bits are still clear. At one, two and three occasions it
    /// reads 1.0, 2.0 and 3.1 — the regime the whole question lives in — and above about
    /// two hundred it stops rising, which costs nothing because a proportion tested on
    /// two hundred independent readings already has all the power it will ever need.
    /// </para>
    /// <para>
    /// <b>Local, like <see cref="Accuracy"/> and unlike the counters.</b> It never
    /// merges and never travels, so C1 is untouched: what another node saw is not
    /// evidence this one may count as its own. It is deterministic under a fixed seed,
    /// which is all it has to be — two machines are not required to agree on it, because
    /// neither is ever told it.
    /// </para>
    /// </remarks>
    public double Occasions
    {
        get
        {
            var set = System.Numerics.BitOperations.PopCount(_witness);

            // The last bucket is held back rather than divided by zero. A full
            // register is an estimate of infinity, and the clamp is what turns it into
            // the largest number the word can honestly stand for.
            var lit = Math.Min(set, Buckets - 1);

            return lit == 0 ? 0.0 : -Buckets * Math.Log(1.0 - (lit / (double)Buckets));
        }
    }

    /// <summary>The tally repair reads: per code, how often it was there in each.</summary>
    public IReadOnlyDictionary<Code, Separation> Separations => _separations;

    /// <summary>Whether every code in the scope is present.</summary>
    /// <param name="moment">What is live.</param>
    public bool Fires(IReadOnlySet<Code> moment)
    {
        ArgumentNullException.ThrowIfNull(moment);

        // WRITTEN OUT RATHER THAN `Scope.All(moment.Contains)`, and the reason is that
        // this is the hottest line in the project. The first cost profile put matching
        // at a third of the wall clock, and the elegant version pays for it three ways
        // on every call: a delegate allocated fresh because the lambda captures
        // `moment`, the struct enumerator boxed onto the heap to reach `IEnumerable`,
        // and an indirect call per code where a direct one would do. Tens of millions of
        // calls a run, every one of them allocating twice.
        //
        // IT SHORT-CIRCUITS WHERE `All` DID AND ANSWERS WHAT `All` ANSWERED, so nothing
        // a run reports may move. `DeterminismTests` is what holds that down.
        foreach (var code in Scope)
            if (!moment.Contains(code)) return false;

        return true;
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

        if (Expects != other.Expects || Scope.Length <= other.Scope.Length) return false;

        // THE SAME REWRITE AS `Fires`, ON THE SWEEP'S SIDE. Subsumption is quadratic in
        // the experienced population, so this runs on every pair — and it was allocating
        // a closure for each one.
        foreach (var code in other.Scope)
            if (!Scope.Contains(code)) return false;

        return true;
    }

    /// <summary>Records what a settlement said, and what was present when it fired.</summary>
    /// <param name="outcome">What the settlement said.</param>
    /// <param name="moment">What was live when it fired.</param>
    /// <param name="recency">How fast the local estimate forgets, in 0..1.</param>
    /// <param name="fleeting">Which of the moment's codes the source says will not come back.</param>
    public void Settle(
        Verdict outcome,
        IReadOnlySet<Code> moment,
        double recency,
        IReadOnlySet<Code>? fleeting = null)
    {
        ArgumentNullException.ThrowIfNull(moment);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(recency);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(recency, 1.0);

        if (outcome == Verdict.Abstain)
        {
            // Nothing else moves, including the table. A settlement that could not
            // say is not evidence about which code separates anything, and letting
            // it into the tally would make a run's separation depend on how often
            // the network was unwell.
            Abstains++;
            return;
        }

        var hit = outcome == Verdict.Hit;

        if (hit) Hits++; else Misses++;

        _seen++;

        // The running average until there is enough to forget, which is XCS's own
        // practice and not a knob. Widrow-Hoff from zero says a commitment that has
        // been right once is a tenth right, so a fresh commitment is indistinguishable
        // from a refuted one and loses every vote it should win. Averaging for the
        // first 1/recency firings starts it where the evidence actually is, and it
        // introduces no number that `recency` did not already fix.
        var rate = _seen <= (long)(1.0 / recency) ? 1.0 / _seen : recency;

        _accuracy += rate * ((hit ? 1.0 : 0.0) - _accuracy);

        var occasion = 0UL;

        // The tally is over what was present and not over the scope. Every scope
        // code is present in every firing by definition, so a tally over the scope
        // separates nothing; what repair needs is the codes that came along.
        foreach (var code in moment)
        {
            // and the occasion is folded in the same pass, so counting what is
            // DIFFERENT about a firing costs no walk of its own -- see
            // <see cref="Occasions"/>. XOR because a moment is a SET: two machines
            // walking it in different orders must reach the same word, and this is
            // the one place where that comes free rather than from sorting.
            occasion ^= Hashing.Mix(code.Value ^ Hashing.Mix(code.Modality));

            if (Scope.Contains(code)) continue;

            // A code the world says will not come back gets no row. It is still folded into
            // the occasion above, because a firing beside a fresh index genuinely IS a
            // distinct firing and dropping it there would make two scenes read as one -- what
            // it may not do is claim a row nothing will ever read again. See fork 31: the
            // table is what blows up, and on `Clevr` 94% of it is codes of exactly this kind.
            if (fleeting is not null && fleeting.Contains(code)) continue;

            // One hash lookup and not two, which is the whole of this line's point.
            // `TryGetValue` followed by an indexer set hashes the code twice and walks
            // the bucket chain twice to write back a struct sixteen bytes wide. The
            // first cost profile put settling at a third of the wall clock and this loop
            // IS settling: it runs once per firing commitment per code in the moment,
            // which on a wide world is the largest number in the run.
            //
            // The table ends up identical, entry for entry and in insertion order. A
            // code not yet present is added as a default <see cref="Separation"/> and
            // then incremented, which is exactly what the pair of calls did — so
            // `Tally.Separations`, the one half of the cost that CAN be barred, does not
            // move, and `DeterminismTests` is what says so.
            ref var seen = ref CollectionsMarshal.GetValueRefOrAddDefault(
                _separations, code, out _);

            seen = hit
                ? seen with { InHits = seen.InHits + 1 }
                : seen with { InMisses = seen.InMisses + 1 };
        }

        _witness |= 1UL << (int)(Hashing.Mix(occasion) & (Buckets - 1));
    }

    /// <summary>Takes over another commitment's record, when it is the same claim said shorter.</summary>
    /// <param name="from">The commitment being rewritten.</param>
    /// <remarks>
    /// <para>
    /// <b>The one place evidence moves, and it is not an exception to never editing
    /// a commitment.</b> Rung five does not change what is claimed — it changes how
    /// the claim is written, by putting a name where its members were. The two entail
    /// exactly the same moments, which <c>Unfold</c> is what checks.
    /// </para>
    /// <para>
    /// <b>Making it re-earn its record would punish being compressed</b>, and a
    /// mechanism whose reward is losing its evidence is one nobody would run.
    /// </para>
    /// </remarks>
    public void Carry(Commitment from)
    {
        ArgumentNullException.ThrowIfNull(from);

        Hits = from.Hits;
        Misses = from.Misses;
        Abstains = from.Abstains;

        _accuracy = from._accuracy;
        _seen = from._seen;

        // The register comes too, for the reason the tally does. A rewrite entails
        // exactly the moments it did before, so it has fired in exactly the same
        // occasions -- and a mechanism whose reward is looking less general than it is
        // would be one nobody would run.
        _witness = from._witness;

        // THE TALLY COMES TOO, and the codes now hidden inside the name are harmless
        // in it: they are present in every firing this can have, so they separate
        // nothing and repair will never choose one.
        foreach (var (code, seen) in from._separations) _separations[code] = seen;
    }

    /// <summary>Drops the tally, keeping everything that decides whether it fires.</summary>
    /// <remarks>
    /// <b>The table is what blows up, not the commitment.</b> Four fields have to
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
    /// <b>Derived from the scope itself, and not from the parent plus the condition
    /// added.</b> Both satisfy the rule that two machines must agree without
    /// speaking, but parent-plus-condition gives the SAME scope two names when two
    /// nodes reach it by adding the same pair of codes in a different order — and
    /// the plan's own sibling problem is bad enough without a commitment being able
    /// to be its own sibling. From the scope, every path that arrives converges.
    /// </para>
    /// <para>
    /// <b><see cref="Hashing"/> and never <see cref="object.GetHashCode"/></b>, which
    /// is randomised per process — a codebook resting on a library's internals would
    /// mean two machines quietly disagreeing about what a commitment IS.
    /// </para>
    /// </remarks>
    public static Code Name(ImmutableArray<Code> scope, Code expects)
    {
        if (scope.IsDefaultOrEmpty)
            throw new ArgumentException("a commitment with no scope fires always", nameof(scope));

        // The length is folded in first so that no scope can be the prefix of
        // another and reach the same name.
        var hash = Hashing.Fold(Hashing.Basis, (ulong)scope.Length);

        foreach (var code in scope.Distinct().Order())
        {
            hash = Hashing.Fold(hash, code.Modality);
            hash = Hashing.Fold(hash, code.Value);
        }

        hash = Hashing.Fold(hash, expects.Modality);
        hash = Hashing.Fold(hash, expects.Value);

        return new Code(Committed, Hashing.Mix(hash));
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
