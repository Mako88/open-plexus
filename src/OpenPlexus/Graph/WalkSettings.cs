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

    /// <inheritdoc cref="Graph.Pricing"/>
    /// <remarks><b><see cref="Graph.Pricing.Receiver"/> is the default and the
    /// control</b>, so every measurement taken before this existed still
    /// stands.</remarks>
    public Pricing Pricing { get; init; } = Pricing.Receiver;

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

    /// <inheritdoc cref="Accumulate"/>
    public required Accumulate Accumulate { get; init; }

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
