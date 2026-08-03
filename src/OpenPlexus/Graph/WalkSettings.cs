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
    /// <b>Bounds the walk only where weights DIFFER</b>: the best edge pays
    /// exactly zero net, so in a near-deterministic world where almost every
    /// weight is near 1.0 nothing decays at all. See <see cref="Inverse"/>.
    /// </summary>
    Best,

    /// <summary>
    /// <c>1 / weight</c> of the edge actually taken, and nothing is paid back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>John's design, 2026-08-02.</b> The stronger the connection the
    /// cheaper the step, so a route runs further down strong edges than weak
    /// ones — and the cost is a property of <i>that connection alone</i>, where
    /// <see cref="Best"/> makes the cost of an edge depend on what other edges
    /// the node happens to have.
    /// </para>
    /// <para>
    /// <b>THIS IS WHAT BOUNDS THE WALK.</b> Every hop costs at least 1, because
    /// a weight cannot exceed 1.0, so a route with budget B takes at most B
    /// steps however perfect its path. `Best` has no such floor and that is the
    /// measured factorial.
    /// </para>
    /// <para>
    /// <b>The near misses matter and are recorded so they are not re-proposed.</b>
    /// <c>1 - weight</c> and <c>-log(weight)</c> both cost <b>zero</b> at a
    /// weight of 1.0 and reproduce the exact failure they would be meant to
    /// fix. Only a form strictly positive at perfect strength terminates.
    /// </para>
    /// <para>
    /// <see cref="WalkSettings.Refuel"/> does nothing here — there is no
    /// payment — so pairing this with <see cref="Refuel.Surprise"/> is refused
    /// rather than silently ignored.
    /// </para>
    /// </remarks>
    Inverse,
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

/// <summary>Which end of an edge works out how strong it is.</summary>
public enum Weighing
{
    /// <summary>
    /// The sender weighs, which needs the PARTNER's marginal and is therefore
    /// a C1 violation — see <see cref="IMarginals"/>.
    /// </summary>
    Sender,

    /// <summary>
    /// The receiver weighs. <b>John's call on fork 2, and C1-legal by
    /// construction.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The sender owns <c>together(me, you)</c> — its own row — and puts that
    /// number in the message. The receiver divides by <c>seen(me)</c>, its own
    /// marginal. <b>Neither node ever reads the other's data</b>, so nothing has
    /// to be fetched, gossiped or cached.
    /// </para>
    /// <para>
    /// <b>Fork 14 is what made this possible.</b> While a step was priced at
    /// the node's strongest edge, the sender had to know every partner's weight
    /// before it could send anything. Under <see cref="StepCost.Inverse"/> the
    /// cost belongs to the edge, so the receiver can charge for the hop it just
    /// took and the sender needs no weights at all.
    /// </para>
    /// <para>
    /// <b>The cost is that the sender cannot prune.</b> It fans out to every
    /// partner and each receiver works out that it should die — except that a
    /// weight cannot exceed 1.0, so a hop cannot cost less than 1, and a sender
    /// holding a budget of 1 or less can refuse the whole fan-out exactly. That
    /// prune is C1-legal and needs nothing from anyone.
    /// </para>
    /// </remarks>
    Receiver,
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

    /// <inheritdoc cref="Weighing"/>
    /// <remarks>
    /// <b>Defaults to <see cref="Weighing.Receiver"/> — John's call on fork 2,
    /// and measured.</b> 100 seeds: behaviour is indistinguishable, 88.87 mean
    /// steps against 95.12 either side of a standard error of about 5.5, and it
    /// costs 26.7 messages a step against 17.0 — half again as many, not the
    /// blow-up it might have been. That is the price of removing the C1
    /// violation, paid in the currency C2 already says is cheap.
    /// </remarks>
    public Weighing Weighing { get; init; } = Weighing.Receiver;

    /// <summary>
    /// The longest chain a route may carry. A route that reaches it dies.
    /// </summary>
    /// <remarks>
    /// <b>Under <see cref="StepCost.Inverse"/> this should not be the thing
    /// that stops a walk</b> — the budget bounds it, and a horizon that fires
    /// first would hide whether the economics work. Set it well above the
    /// stamina and watch <see cref="Thinking.Accounting.Halted"/>.
    /// </remarks>
    /// <remarks>
    /// <para>
    /// <b>A SAFETY, NOT PART OF THE DESIGN — and it had to exist, because the
    /// design's own bound was measured to fail.</b> The claim was that stamina
    /// is the whole of the schedule and no depth limit is needed, with
    /// <see cref="StepCost.Best"/> the pricing that bounds the walk.
    /// </para>
    /// <para>
    /// <b>That holds only where edge weights DIFFER.</b> Under `Best` the price
    /// is the strongest partner's fuel, so a route down the best edge keeps its
    /// budget exactly. In a near-deterministic world almost every weight is
    /// near 1.0, every partner is the best partner, and NOTHING decays — the
    /// cycle check becomes the only bound and the flood enumerates every simple
    /// path. Measured on a clique with equal weights, messages from one origin:
    /// 4 nodes → 15, 5 → 64, 6 → 325, 7 → 1,956, 8 → 13,699. Factorial.
    /// </para>
    /// <para>
    /// <b>Required rather than defaulted</b>, and every route it kills is
    /// counted as <see cref="Thinking.Accounting.Halted"/>, because a walk that
    /// hit the horizon looks exactly like one that finished unless that is
    /// reported. See open fork 8.
    /// </para>
    /// </remarks>
    public required int Horizon { get; init; }
}
