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
}
