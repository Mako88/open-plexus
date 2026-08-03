namespace OpenPlexus.Graph;

/// <summary>How an arrival is valued, which is a different question from how a route is funded.</summary>
public enum ArrivalValue
{
    /// <summary>Accumulated path strength. The best answer is the
    /// best-supported one, and can only be the expected one.</summary>
    Strength,

    /// <summary>Path strength divided by how prevalent the endpoint is alone.
    /// Confident <i>and</i> landing somewhere rare scores high. C1-legal where
    /// PPMI is not: the global occasion total is identical for every candidate
    /// so it cancels in a ranking, leaving one node's own marginal.</summary>
    Lift,
}

/// <summary>How a candidate accumulates evidence from the routes reaching it.</summary>
public enum Accumulate
{
    /// <summary>The single strongest route.</summary>
    Max,

    /// <summary>Every route. Many weak agreeing routes outrank one strong
    /// route — 0.1234 against max's 0.0834 on the typed walk.</summary>
    Sum,
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

    /// <inheritdoc cref="ArrivalValue"/>
    public required ArrivalValue Value { get; init; }

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
}
