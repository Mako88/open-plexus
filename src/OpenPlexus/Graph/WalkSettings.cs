using OpenPlexus.Thinking;

namespace OpenPlexus.Graph;

/// <summary>
/// Which end of an edge weighs it — <b>and therefore what a hop costs.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>BOTH ARE C1-LEGAL AND ONLY ONE HAS EVER BEEN BUILT.</b> The message
/// carries <see cref="Thinking.Message.Together"/> and now
/// <see cref="Thinking.Message.Seen"/>, so the receiver can divide by either
/// marginal without reading anything it does not own.
/// </para>
/// <para>
/// <b>The tag experiment is what asked for this.</b> An ephemeral index has
/// <c>seen = 1</c>, so under <see cref="Receiver"/> every attribute accumulates
/// one maximally-cheap partner per occurrence and the fan-out explodes —
/// measured at 6.6× the messages, and timing out entirely on longer runs.
/// Weighing from the sender inverts exactly that hop: reaching a fresh index
/// from a common attribute becomes expensive, and leaving the index for what it
/// points at stays cheap.
/// </para>
/// <para>
/// <b>It changes the ranking as well as the price</b>, because one weight does
/// both jobs — so a result under this arm cannot be attributed to cost alone.
/// Said out loud rather than discovered later.
/// </para>
/// </remarks>
public enum Pricing
{
    /// <summary>
    /// <c>together / seen(receiver)</c> — <i>how characteristic is where you came
    /// from, of me</i>. <b>The default, and everything measured so far.</b>
    /// Arriving somewhere popular is expensive, which is the anti-hub property.
    /// </summary>
    Receiver,

    /// <summary>
    /// <c>together / seen(sender)</c> — <i>how characteristic am I, of where you
    /// came from</i>. Leaving somewhere popular is expensive instead.
    /// </summary>
    Sender,

}

/// <summary>
/// What a hop is CHARGED — <b>the outstanding half of this design's recurring
/// fault, and the one number still doing both jobs.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>IT HAS BITTEN THREE TIMES AND <see cref="WalkSettings.Doubt"/> ONLY SPLIT
/// HALF OF IT.</b> A row entry ranks a partner AND prices the hop to it. Doubt
/// separated what an edge is BELIEVED from what it COSTS — but both are still
/// functions of the same statistic, <c>together / seen</c>, so improving the
/// evidence still moves the budget and a result under either can never be
/// attributed to one alone. <b>The split this dial makes is of the STATISTIC and
/// not of the arithmetic over it.</b>
/// </para>
/// <para>
/// <b>THE BUDGET IS SPENT ON MESSAGES, SO PRICE IT IN MESSAGES.</b>
/// <see cref="Node.Fire"/> emits one message per surviving row ENTRY, so what a
/// hop actually costs the system is the width of the row it lands in — which
/// <see cref="Node.Entries"/> already calls the cost, and which the scaling
/// section already names as the thing to bound. How well-evidenced the edge was
/// has nothing to do with it.
/// </para>
/// <para>
/// <b>NOTHING NEW TRAVELS AND NOTHING IS READ THAT IS NOT OWNED.</b> The
/// receiver charges from its OWN snapshot, taken before it weighs anything, so
/// this is a strictly smaller claim on other nodes' data than either
/// <see cref="Pricing"/> arm — the message does not have to carry a thing.
/// </para>
/// </remarks>
public enum Toll
{
    /// <summary>
    /// <c>1 / weight</c> — <b>the default, the control, and every measurement
    /// taken up to now.</b> A weak edge is dear to cross.
    /// </summary>
    Evidence,

    /// <summary>
    /// <c>1 + log₂(entries)</c> — <b>what arriving here will cost in traffic.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE LOG IS THE SAME ARGUMENT <see cref="Learning.Chunk"/> MINTS BY, and
    /// it is description length rather than a curve somebody liked.</b> Saying
    /// which of <c>n</c> partners a route took costs <c>log₂ n</c> bits, so a hop
    /// into a node of a thousand partners is ten times the hop into a node of two
    /// — not five hundred times, which is what the raw fan-out would charge and
    /// which would refuse a hub harder than the anti-hub weighting already does.
    /// </para>
    /// <para>
    /// <b>THE ONE ADDED IS WHAT KEEPS THE WALK BOUNDED, and it is exactly the
    /// condition the refuted <c>StepCost</c> row asks for.</b> Those arms died of
    /// factorial message growth because a perfect edge could cost nothing; here
    /// the cheapest possible hop — into a node with a single partner — still costs
    /// one, so a budget of <c>B</c> buys at most <c>B</c> steps and the bound does
    /// not rest on any weight being below 1.0. <b>A bound not relying on positive
    /// cost at weight 1.0 is what that row named as its revival condition.</b>
    /// </para>
    /// <para>
    /// <b>IT IS ALSO THE ARM FOR STEP 3'S OPEN QUESTION.</b> A minted node is a
    /// hub by construction and <see cref="Pricing.Receiver"/> refuses hubs, which
    /// is the unverified reading of why chunking costs a little accuracy. Under
    /// this a chunk becomes <i>expensive to enter and still believed</i>, which is
    /// the reading that should be true — so the two dials together say whether
    /// that explanation holds.
    /// </para>
    /// <para>
    /// <b>WHAT IT COSTS: STAMINA STOPS CAPPING DEPTH UNIFORMLY, AND
    /// <see cref="WalkSettings.Horizon"/> BECOMES LOAD-BEARING AGAIN.</b> Under
    /// <see cref="Evidence"/> the worst case is one hop per unit of budget
    /// everywhere. Under this the price is local: a route crossing narrow rows
    /// pays two a hop and one crossing wide ones pays six, so the SAME budget
    /// walks three times as deep through a sparse region. <b>That is the dial
    /// working — spend where it is cheap — and it is also how a run that landed in
    /// seconds on one world fails to land at all on another.</b> The horizon has
    /// not fired since the cost became inverse; under this it is the only uniform
    /// bound left, and a ladder swept here must be read against the message count
    /// rather than against the dial.
    /// </para>
    /// </remarks>
    Traffic,
}

/// <summary>How a candidate accumulates evidence from the routes reaching it.</summary>
public enum Accumulate
{
    /// <summary>
    /// Every route. <b>Many weak agreeing routes outrank one strong route.</b>
    /// </summary>
    /// <remarks>
    /// <b><c>Max</c> — the single strongest route — is gone.</b> It was swept and
    /// inert on the typed walk, and re-run on the composition world where its
    /// revival condition pointed it lost there too.
    /// </remarks>
    Sum,

    /// <summary>
    /// <b>How many DISTINCT ORIGINS reached it</b>, and strength only to break a
    /// tie.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THIS IS WHAT A CONJUNCTION IS, AND <see cref="Sum"/> CANNOT EXPRESS
    /// IT.</b> Ask with two things at once and the thing you meant is the one
    /// BOTH of them reach, where everything else is reached by one. That is a
    /// count of origins agreeing — and summing path strengths does not measure
    /// it, because strength varies far more between routes than the count does,
    /// so one strong single-origin route outranks two weak agreeing ones.
    /// </para>
    /// <para>
    /// <b>It also fixes what <see cref="Sum"/> over-counts.</b> Many routes from
    /// ONE origin are one piece of evidence arriving by several paths, not
    /// several pieces; <see cref="Sum"/> adds them all and this does not.
    /// </para>
    /// <para>
    /// <b>Nothing new travels for it.</b> A chain begins at its origin and is
    /// already carried for the cycle check, so the origin is in every arrival
    /// that comes back — the count is taken at the machine that asked, which
    /// reads nobody else's data.
    /// </para>
    /// </remarks>
    Agreement,
}

/// <summary>
/// FORK 21 — when a conclusion is worth writing down as an observation.
/// </summary>
/// <remarks>
/// <para>
/// <b>Borrowed from slime mold, thresholded like a crystal.</b> <i>Physarum</i>
/// solves a maze with no brain and no global view: tubes carrying high flux
/// thicken and low-flux tubes atrophy, entirely locally — which is the only kind
/// of mechanism C1 permits. So a route walked often enough is minted as a direct
/// edge, and the composition stops being re-derived from scratch every time.
/// </para>
/// <para>
/// <b>Left alone that collapses the graph into a hairball</b>, so the threshold
/// comes from nucleation: a new phase forms only above a critical size, because
/// below it the surface cost exceeds the volume gain. An abstraction is minted
/// only when the pattern is frequent enough to pay for its own storage.
/// </para>
/// <para>
/// <b>THE RISK IS THAT THE SYSTEM LEARNS ITS OWN HALLUCINATIONS</b> — confirmation
/// bias, literally, and it is the reason both dials exist rather than one. Null
/// on <see cref="WalkSettings.Reflect"/> is the control, and it is off.
/// </para>
/// </remarks>
public sealed record Reflection
{
    /// <summary>
    /// The score an arrival must reach before it is worth minting. <b>The
    /// nucleation threshold</b>, and the only thing standing between this and a
    /// complete graph.
    /// </summary>
    public required double Threshold { get; init; }

    /// <summary>
    /// What a concluded occasion counts against an observed one.
    /// </summary>
    /// <remarks>
    /// <b>Below 1.0, or a belief reinforces itself as fast as evidence does.</b>
    /// </remarks>
    public required double Weight { get; init; }

    /// <summary>How many arrivals at most are written back.</summary>
    public required int Names { get; init; }

}

/// <summary>
/// The swept dials.
/// </summary>
/// <remarks>
/// <para>
/// <b>Identical on every node</b>, or the same route gets priced differently
/// depending on where it happens to be standing. Handed out once and frozen,
/// which C1 permits.
/// </para>
/// <para>
/// <b>WHAT USED TO BE HERE AND IS GONE — John's call, 2026-08-02.</b> A decided
/// option kept around as a control sneaks back in and causes havoc later, so
/// the refuted arms are deleted rather than parked.
/// </para>
/// <list type="bullet">
/// <item><b>`StepCost`</b> — `Best`, `Local` and `Constant`. A step costs
/// <c>1/weight</c> of the edge it walks and nothing else is reachable. `Best`
/// was measured factorial where this is polynomial: on a 12-clique with equal
/// weights it passed five million messages where this took 1,111.</item>
/// <item><b>`Refuel`</b> — nothing is paid back, so there was nothing for it
/// to do.</item>
/// <item><b>`Charge`</b> — the price for `StepCost.Constant`, which is gone.</item>
/// <item><b>`Weighing`</b> — the sender arm read the partner's marginal, which
/// is the C1 violation the receiver arm exists to remove. `IMarginals` and
/// `LocalMarginals` went with it.</item>
/// <item><b>`ArrivalValue` and `Value`</b> — `Lift` divided an arrival by the
/// endpoint's own prevalence. Swept, inert, and both explanations for why were
/// refuted too, so the enum had one member left and the dial chose nothing.
/// </item>
/// <item><b>`Accumulate.Max`</b> — the single strongest route. Inert on the
/// typed walk and worse on the composition world, which is where its revival
/// condition sent it.</item>
/// <item><b>`Accumulate` itself</b> — <b>not refuted, MOVED.</b> Agreement is
/// right on a conjunction and harmful on an indexed question, so it was never a
/// level to find: it is a property of what is being asked. It now travels on
/// <see cref="Thinking.Question"/>, beside the grouping that was already going
/// there.</item>
/// <item><b>`Accumulate.Fused`</b> — rank fusion over the two orders, built to
/// dissolve that dial rather than move it. Two candidates whose orders invert
/// score identically under it for EVERY damping constant, so it ties exactly
/// where it was needed and the tiebreak answers.</item>
/// </list>
/// </remarks>
public sealed record WalkSettings
{
    /// <summary>
    /// What each route starts with, in perfect hops.
    /// </summary>
    /// <remarks>
    /// A step costs <c>1/weight</c> and a weight cannot exceed 1.0, so a hop
    /// costs at least 1 and a budget of <c>B</c> buys at most <c>B</c> steps
    /// however strong the path. <b>That is what bounds the walk</b>, and it
    /// makes the number mean something rather than being a constant nobody set.
    /// </remarks>
    public required double Stamina { get; init; }

    /// <summary>
    /// The budget for a prediction, when it should differ from the budget for
    /// acting. Null means they are the same.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>FORK 20, AND IT IS FORCED BY MEASUREMENT RATHER THAN CHOSEN.</b> One
    /// budget was serving two questions that want opposite depths.
    /// </para>
    /// <para>
    /// <b>Prediction wants the shallowest walk that reaches anything.</b>
    /// Direct association <i>is</i> the signal: at stamina 2 the novelty gap is
    /// 0.0605 ± 0.0039 and at stamina 4 it is 0.0042 ± 0.0025 — fourteen times
    /// worse, because without edge kinds a deeper walk reaches more and ranks
    /// worse.
    /// </para>
    /// <para>
    /// <b>Choosing an action needs more.</b> At stamina 2 <i>every one</i> of
    /// 139 chain-chosen moves repeated the last action: the action just taken is
    /// in the current occasion, so one hop reaches it and nothing else, and the
    /// chain becomes a pure mirror. Depth is what lets another action be
    /// reached at all.
    /// </para>
    /// </remarks>
    public double? Foresight { get; init; }

    /// <summary>
    /// The most entries one node's row may hold. <b>Null is unbounded, which is
    /// every measurement taken before this existed.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE SCALING WALL, AND THE ONLY THING THAT TURNS <i>cost grows with data
    /// forever</i> INTO <i>cost is constant</i>.</b> Cost per thought is set by the
    /// widest row rather than by the node count, and <see cref="Node.Fire"/> emits
    /// one message per ENTRY — so a cap here is a cap on the fan-out, which is the
    /// trick approximate-nearest-neighbour indexes run at billions on.
    /// </para>
    /// <para>
    /// <b>IT EVICTS ON "NOT TOUCHED SINCE" AND NEVER BY ERODING A COUNT</b>, which
    /// is the distinction the whole coordination-free design rests on. A count that
    /// decreased would break convergence; an entry that stops being RESIDENT does
    /// not — the number was not revised, it was paged out. <see cref="Tie.When"/>
    /// is what makes that possible and this is its first consumer.
    /// </para>
    /// <para>
    /// <b>IT IS ALSO THE ONLY FORGETTING THIS DESIGN HAS.</b> The bet is that
    /// nothing can be unlearned, only outvoted — and the plan names eviction as the
    /// expensive thing to walk back if forgetting turns out to be necessary rather
    /// than optional. <b>A cap makes that testable instead of assumed.</b>
    /// </para>
    /// </remarks>
    public int? Row { get; init; }

    /// <inheritdoc cref="Graph.Pricing"/>
    /// <remarks><b><see cref="Graph.Pricing.Receiver"/> is the default and the
    /// control</b>, so every measurement taken before this existed still
    /// stands.</remarks>
    public Pricing Pricing { get; init; } = Pricing.Receiver;

    /// <inheritdoc cref="Graph.Toll"/>
    /// <remarks>
    /// <b><see cref="Graph.Toll.Evidence"/> is the default and the control</b>, so
    /// every measurement taken before this existed still stands. <b>It is
    /// independent of <see cref="Pricing"/> on purpose</b>: that dial chooses
    /// which marginal weighs an edge and therefore moves both jobs at once, which
    /// is the fault rather than the fix.
    /// </remarks>
    public Toll Toll { get; init; } = Toll.Evidence;

    /// <summary>
    /// How much evidence a partner must show before its edge is believed —
    /// <c>together / (seen + doubt)</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>MEASURED: THE WEIGHTING REFUSES A BACKGROUND BEAUTIFULLY AND IS FOOLED
    /// BY A COINCIDENCE.</b> Clutter that is present at every moment costs
    /// messages and does not move the score at all, which is the anti-hub
    /// property working exactly as designed. Clutter drawn from a large pool —
    /// each code rare — takes the senses world down hard, and gets worse the
    /// larger the pool is.
    /// </para>
    /// <para>
    /// <b>Because <c>together / seen</c> is a maximum-likelihood estimate with no
    /// confidence in it.</b> A code seen ONCE, that happened to co-occur that
    /// once, scores a weight of 1.0 — the strongest edge the system can hold, on
    /// a single accident. And rare accidental co-occurrences are precisely what
    /// a bigger world produces more of, so the failure grows with scale.
    /// </para>
    /// <para>
    /// <b>The fix is shrinkage, and it is not new anywhere but here.</b> Adding a
    /// constant to the denominator pulls a thinly-evidenced ratio toward zero and
    /// leaves a well-evidenced one alone — Laplace's rule, the Dirichlet prior,
    /// the smoothing in IDF and the saturation in BM25 are all this. A partner
    /// seen once cannot then outscore one seen a hundred times.
    /// </para>
    /// <para>
    /// <b>Zero is off, and off is every measurement taken before this existed.</b>
    /// </para>
    /// </remarks>
    public double Doubt { get; init; }

    /// <summary>
    /// The longest chain a route may carry. A route that reaches it dies.
    /// </summary>
    /// <remarks>
    /// <b>A backstop that has never fired since the cost became inverse</b> —
    /// measured at zero halts over 200 seeds with this set to 50 and a stamina
    /// of 4. Kept because an unbounded walk is the one failure that takes the
    /// process with it, and every route it kills is counted as
    /// <see cref="Thinking.Accounting.Halted"/> so a run that hit it cannot
    /// look like one that finished.
    /// </remarks>
    public required int Horizon { get; init; }

    /// <inheritdoc cref="Reflection"/>
    /// <remarks><b>Null is off, and off is the control.</b></remarks>
    public Reflection? Reflect { get; init; }

    /// <summary>
    /// Let a machine hunt for its own <see cref="Stamina"/> — <b>fork 24</b>.
    /// </summary>
    /// <remarks>
    /// <b>Null keeps the hand-set number, and that is the control.</b> When it is
    /// set, <see cref="Stamina"/> is only where the hunt begins, and the
    /// convergence test asserts the answer does not depend on it.
    /// </remarks>
    public Budgeting? Budget { get; init; }
}
