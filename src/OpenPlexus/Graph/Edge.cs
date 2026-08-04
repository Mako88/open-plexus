using OpenPlexus.Codes;

namespace OpenPlexus.Graph;

/// <summary>
/// What a row entry MEANS — <b>step 6, and the thing a bare count could never
/// say.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THREE ROADS END HERE.</b> A deeper walk for prediction was monotonically
/// worse, because without kinds a deeper reach ranks a thing that merely
/// accompanies against a thing that follows. The temporal window helps where
/// everything is sequential and hurts where things overlap, for the same reason:
/// a carried edge is written into the same cell as a simultaneous one, so the
/// walk cannot tell <i>follows</i> from <i>accompanies</i>. And ordered output
/// needs an order to output.
/// </para>
/// <para>
/// <b>IT IS THE <c>Groups</c> TRICK AGAIN — JOHN'S INSIGHT.</b> A phase cannot
/// survive C2: messages are late, jittered and out of order, which is exactly
/// what destroys a continuous oscillator relationship. So the front end SAYS the
/// order, inside the occasion, where lateness cannot touch it — precisely as
/// grouping said which attribute belonged to which object.
/// </para>
/// <para>
/// <b>The alphabet does not grow.</b> A kind is a property of the PAIR, not a new
/// code — <c>after</c> is not a node, any more than absence is. What grows is the
/// row, by at most one entry per kind per partner, and that is the price named in
/// full on <see cref="Tie"/>.
/// </para>
/// <para>
/// <b>AND IT IS NOT AN ENUM, BECAUSE AN ENUM CANNOT GROW WITHOUT A RECOMPILE —
/// JOHN'S ASK.</b> A relation is named by <see cref="Of"/>, which derives a
/// <see cref="ulong"/> from the name the way <see cref="Learning.Chunk"/> derives
/// a node's, so a front end can mint <i>north-of</i> and two machines agree with
/// nothing to ask. <b>The five below are ordinary calls to it</b> and hold no
/// privilege the arithmetic does not give them.
/// </para>
/// <para>
/// <b>IT COSTS NOTHING, AND THAT WAS MEASURED BEFORE IT WAS BUILT.</b> The enum's
/// four bytes were followed by four of padding, so the name lands in space the row
/// was already wasting — <c>RowWidthTests</c> holds the number.
/// </para>
/// </remarks>
public readonly record struct Kind
{
    private readonly ulong _name;

    private Kind(ulong name) => _name = name;

    /// <summary>
    /// The relation of this name, <b>agreed by every machine without asking
    /// one.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Agreed"/> AND NOT <see cref="object.GetHashCode"/>, for the
    /// red-ball reason.</b> String hashing is randomised per process, so a relation
    /// named that way would be a different cell in every run and on every machine —
    /// the same defect that makes an interning table wrong for
    /// <see cref="Worlds.Kinds.Named"/>.
    /// </para>
    /// <para>
    /// <b>THE NAME IS THE IDENTITY AND NOTHING ELSE IS.</b> Two machines that call
    /// this with the same string hold the same cell, and one that has never heard
    /// of a relation simply has no entry for it — which is what a row does with
    /// every partner it has not met.
    /// </para>
    /// </remarks>
    /// <param name="relation">The relation's name, taken exactly as given.</param>
    public static Kind Of(string relation)
    {
        ArgumentException.ThrowIfNullOrEmpty(relation);

        var hash = Agreed.Basis;

        foreach (var letter in relation) hash = Agreed.Fold(hash, letter);

        return new Kind(Agreed.Mix(hash));
    }

    /// <summary>
    /// They were in the same moment. <b>Symmetric, and everything measured before
    /// kinds existed.</b>
    /// </summary>
    /// <remarks>
    /// Nothing came first, so both sides write each other and the pair means the
    /// same read from either end.
    /// </remarks>
    public static Kind With { get; } = Of("with");

    /// <summary>
    /// The partner came AFTER this node. <b>One-way, and that asymmetry is the
    /// whole of what makes an edge temporal.</b>
    /// </summary>
    /// <remarks>
    /// <b>The past records the future</b>, so a broadcast of what has just
    /// happened can walk forward to what usually follows. That rule already
    /// governed <see cref="Learning.Occasion.Recent"/>; what is new is that the
    /// count lands in its own cell instead of being added to the simultaneous one.
    /// </remarks>
    public static Kind After { get; } = Of("after");

    /// <summary>
    /// The partner came BEFORE this node. <b>The same fact as <see cref="After"/>
    /// read from the other end, and it is a SEPARATE CELL rather than a symmetric
    /// one.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE ONE-WAY RULE WAS A WORKAROUND FOR THE MISSING KIND, AND STEP 6
    /// RETIRED WHAT IT WAS WORKING AROUND.</b> Writing a temporal pair both ways
    /// used to destroy it: with one cell per pair, a reverse write made the edge
    /// mean <i>with</i>, and the asymmetry was the only thing keeping <i>then</i>
    /// distinguishable. A row that can say which relation it holds does not need
    /// that trick — <c>before</c> and <c>after</c> are different cells and neither
    /// collapses into <c>with</c>.
    /// </para>
    /// <para>
    /// <b>MEASURED, AND IT WAS A SEVERED PATH RATHER THAN A PREFERENCE.</b> On
    /// snake the action is said to come before the view it produced. With only the
    /// forward cell written, choosing an action — which broadcasts the CURRENT
    /// VIEW and has to reach an action code — had no edge to walk at all, and the
    /// chain reached an action zero times on every seed. The body moved at random
    /// while the prediction improved.
    /// </para>
    /// <para>
    /// <b>ONLY WHERE BOTH CODES WERE IN THE OCCASION</b>, which is
    /// <see cref="Learning.Occasion.Sequence"/> and never
    /// <see cref="Learning.Occasion.Recent"/>. A carried code had already stopped,
    /// so it does not note the occasion; a reverse edge INTO it would be weighed
    /// against a marginal that never counted this moment, and
    /// <c>together</c> could exceed <c>seen</c> — which breaks the bound that
    /// makes every hop cost at least one and the walk terminate.
    /// </para>
    /// </remarks>
    public static Kind Before { get; } = Of("before");

    /// <summary>
    /// They met, <b>and things got better afterwards</b> — the contrastive half of
    /// step 4's third factor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>IT IS A DIFFERENT CATEGORY OF RELATION FROM THE OTHER THREE, AND SAYING
    /// SO IS THE HONEST PART.</b> <see cref="With"/>, <see cref="After"/> and
    /// <see cref="Before"/> are facts about WHEN two codes met; this is a fact
    /// about what happened next to a body. It rides in the same slot because the
    /// slot is *which statistic about this pair*, and that is exactly what it is —
    /// but nobody should read the four as one taxonomy.
    /// </para>
    /// <para>
    /// <b>MEASURED: A COUNT OF WHAT WAS DONE CONVERGES ON THE POLICY THAT DID
    /// IT.</b> A state and the act taken in it join one occasion, so
    /// <c>together(state, act)</c> estimates <i>P(act | state)</i> — which is the
    /// behaviour policy and nothing else. Worse, the states a body finds itself in
    /// are the ones its own actions produced: attend one variable and the others
    /// drain, so "the others are at the floor" becomes strongly associated with the
    /// action that drained them. On <c>Homeostat</c> the arm settled on ONE action,
    /// never touched the two fastest-draining needs, and scored exactly what doing
    /// nothing scores.
    /// </para>
    /// <para>
    /// <b>WEIGHTING THE OCCASION CANNOT FIX THAT, WHICH IS WHY THREE CREDIT ARMS
    /// FAILED.</b> A heavier write lands on the SAME cell, so it deepens the groove
    /// it was meant to correct. What is needed is a contrast — this pair against
    /// the alternatives — and a contrast needs a second statistic to divide by,
    /// not a bigger number in the first one.
    /// </para>
    /// <para>
    /// <b>SO THE CREDIT GETS ITS OWN CELL, AND THE CONTRAST FALLS OUT OF THE
    /// EXISTING WEIGHTING.</b> A hop is priced <c>together / seen(receiver)</c>,
    /// and <c>seen(act)</c> counts every occasion the action was in — so this cell
    /// over that marginal is the share of the times an action was taken that it
    /// helped. An action taken constantly and rarely useful is refused by the
    /// anti-hub property that already exists. <b>Nothing is punished and nothing
    /// decays</b>: an act that did not help simply gets no second write, and both
    /// counts stay monotonic, so the G-Counter property is untouched.
    /// </para>
    /// <para>
    /// <b>It is the project's own remedy applied once more</b> — one number was
    /// saying both <i>this was done here</i> and <i>this is worth doing here</i>,
    /// and those are two statistics.
    /// </para>
    /// </remarks>
    public static Kind Helped { get; } = Of("helped");

    /// <summary>
    /// They met, <b>and things got WORSE afterwards</b> — the negative half, and
    /// the first thing in this design that can say <i>not that one</i>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE PLAN SAID PUNISHMENT WAS UNAVAILABLE, AND IT WAS THE WRONG CRDT
    /// RATHER THAN A LAW.</b> Counts must only increase, so a factor that could go
    /// negative would break convergence — true of ONE counter. A <b>PN-Counter</b>
    /// (Shapiro et al., 2011) is TWO G-Counters, and its value is read as the
    /// difference: each side only ever rises, each merges by taking the larger,
    /// and the derived quantity can still fall. Convergence is untouched.
    /// <see cref="Helped"/> is P and this is N.
    /// </para>
    /// <para>
    /// <b>IT COSTS A KIND AND NOT A FIELD, WHICH IS WHY IT IS AFFORDABLE.</b> The
    /// row is keyed by <c>(partner, kind)</c>, so this is one more entry per
    /// partner that earns one — the cost model <see cref="Tie"/> already names.
    /// Widening the row a second time is what that note forbids, and this does not.
    /// </para>
    /// <para>
    /// <b>THE SUBTRACTION HAPPENS WHERE THE SENDER FANS OUT</b>, reading its own
    /// row and nobody else's, so C1 is untouched. <b>Clamped at nought</b>: a
    /// negative weight would break <c>together &lt;= seen</c>, which is the bound
    /// that makes every hop cost at least one and the walk terminate. A partner
    /// whose net is nought or less is not walked at all — <b>and that is
    /// inhibition</b>, an edge saying <i>this rules that out</i> rather than merely
    /// failing to recommend it.
    /// </para>
    /// <para>
    /// <b>Together the two cells are a contingency rather than a frequency.</b>
    /// <c>(helped − hindered) / seen</c> is the shape of Rescorla and Wagner's
    /// ΔP: what a count of co-occurrence structurally cannot say is whether the
    /// thing helped MORE than it hurt, and two monotonic counters can.
    /// </para>
    /// </remarks>
    public static Kind Hindered { get; } = Of("hindered");

    /// <summary>
    /// The relation's name where one is known, and its number where it is not.
    /// </summary>
    /// <remarks>
    /// <b>PRESENTATION, AND NEVER IDENTITY.</b> A derived name is an opaque number
    /// and a failing assertion that prints one teaches nobody anything, so the five
    /// built here are spelled back out. <b>A relation minted by a front end prints
    /// as its number</b>, which is honest: nothing in this process knows the string
    /// it came from, and nothing needs to.
    /// </remarks>
    public override string ToString() =>
        Spelled.GetValueOrDefault(_name) ?? $"kind:{_name:x16}";

    private static readonly Dictionary<ulong, string> Spelled = new()
    {
        [With._name] = "with",
        [After._name] = "after",
        [Before._name] = "before",
        [Helped._name] = "helped",
        [Hindered._name] = "hindered",
    };
}

/// <summary>
/// One row entry's identity: <b>who, and in what relation.</b>
/// </summary>
/// <remarks>
/// <b>THE KIND IS IN THE KEY AND NOT IN THE VALUE</b>, because <c>with(a, b)</c>
/// and <c>after(a, b)</c> are different statistics about the same pair and
/// merging them is the defect this exists to fix. A node that saw B alongside A
/// forty times and following A twice knows two things, and one number could only
/// ever hold their sum.
/// </remarks>
public readonly record struct Edge(Code Partner, Kind Kind);

/// <summary>
/// One row entry's value: <b>the count, and when it was last touched.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>TWO CHANNELS THAT MUST NEVER MERGE.</b> <see cref="Count"/> only ever
/// increases, which is the G-Counter property that makes it converge under
/// reordering and loss with no coordinator. <see cref="When"/> is an
/// LWW-Register, which is also a CRDT and converges by taking the later write —
/// and LWW DISCARDS CONCURRENT WRITES, which is right for a piece of state and
/// ruinous for learning. Joining them would throw away counts.
/// </para>
/// <para>
/// <b>THE ROW IS WIDENED ONCE, NOT THREE TIMES — and this is that once.</b> Edge
/// kinds, supersession and eviction metadata all want a record where there was a
/// number, and they share a single price: memory per edge, which is the scaling
/// wall. Paying it three times over would be three migrations of the one
/// structure that cannot afford them.
/// </para>
/// <para>
/// <b>Nothing ranks by <see cref="When"/> yet.</b> It is written and read back and
/// no walk consults it. Its two consumers are named and unbuilt: ranking by
/// recency, which belongs on <see cref="Thinking.Question"/> and not on a dial,
/// and eviction on "not touched since", which is how a row gets bounded without
/// eroding a count. <b>Said plainly rather than left to look wired.</b>
/// </para>
/// </remarks>
/// <param name="Count">
/// How much this pair has co-occurred in this relation. <b>Monotonic. Never
/// decays</b> — decay breaks convergence, which is the property the whole
/// coordination-free design rests on.
/// </param>
/// <param name="When">
/// The observing machine's own clock at the last write, from
/// <see cref="Learning.Occasion.At"/>. <b>Zero means never written by anything
/// that had a clock to offer</b>, which is every edge minted by a test or by a
/// caller that did not say.
/// </param>
public readonly record struct Tie(double Count, long When)
{
    /// <summary>
    /// This entry, having just been observed again.
    /// </summary>
    /// <remarks>
    /// <b>The two channels move by their own rules in one place</b>, so nothing
    /// can add to the count while forgetting the clock, and nothing can move the
    /// clock backwards on a late-arriving observation — C2 says messages are out
    /// of order, and an LWW-Register that took the last WRITE rather than the
    /// latest STAMP would converge on whichever message happened to lose the race.
    /// </remarks>
    public Tie Plus(double by, long when) => new(Count + by, Math.Max(When, when));
}
