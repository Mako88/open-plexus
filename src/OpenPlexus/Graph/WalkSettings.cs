namespace OpenPlexus.Graph;

/// <summary>How a route pays for a step.</summary>
public enum StepCost
{
    /// <summary>The same charge everywhere. One tuned number.</summary>
    Constant,

    /// <summary>
    /// The mean weight of the edges leaving the node. <b>Refuted.</b> About
    /// half a node's edges are above its own mean, so a route taking
    /// above-mean steps gains budget forever: unbounded at every budget from
    /// 0.002 to 0.25, everything reached, agreement at chance. A walk that
    /// reaches everything has answered nothing. Kept as an arm because a
    /// refutation that cannot be re-run is a claim.
    /// </summary>
    Local,

    /// <summary>
    /// The strongest edge available here — an opportunity cost. Budget can
    /// then never rise, so only a route taking near-best edges keeps it.
    /// <b>The one measured to bound the walk.</b>
    /// </summary>
    Best,
}

/// <summary>What a route is paid for taking an edge. The budget, not the score.</summary>
public enum Refuel
{
    /// <summary>The edge weight. A route survives by walking strong edges, so
    /// it can only ever surface what is already expected.</summary>
    Strength,

    /// <summary>How unlikely the edge was. Preference as economics rather than
    /// as selection: a broadcast cannot rank a frontier, but it can make the
    /// unexpected cheaper to keep walking. <b>Named risk</b>: the rarest edges
    /// are the noisiest, so this may fund routes through coincidence.</summary>
    Surprise,
}

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
/// <b>Identical on every node</b>, or the same route gets priced differently
/// depending on where it happens to be standing. Handed out once and frozen,
/// which C1 permits.
/// </remarks>
public sealed record WalkSettings
{
    /// <summary>
    /// What each route starts with. <b>A swept budget, not a freed knob</b> —
    /// the claim that stamina removes a tuned constant was measured false. What
    /// it genuinely buys is history: a route that walked strong edges can afford
    /// a weak one, where a floor cuts on the single step in front of it.
    /// </summary>
    public required double Stamina { get; init; }

    /// <inheritdoc cref="StepCost"/>
    public required StepCost Cost { get; init; }

    /// <summary>The price per step under <see cref="StepCost.Constant"/>.
    /// Refused if set otherwise, because an argument that silently does nothing
    /// is a sweep arm that looks distinct and is not.</summary>
    public double Charge { get; init; }

    /// <inheritdoc cref="Refuel"/>
    public required Refuel Refuel { get; init; }

    /// <inheritdoc cref="ArrivalValue"/>
    public required ArrivalValue Value { get; init; }

    /// <inheritdoc cref="Accumulate"/>
    public required Accumulate Accumulate { get; init; }
}
