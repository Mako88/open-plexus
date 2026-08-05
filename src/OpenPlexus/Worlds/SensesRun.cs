using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using OpenPlexus.Thinking;

namespace OpenPlexus.Worlds;

/// <summary>
/// What the senses world measured. <b>Counts, not claims.</b>
/// </summary>
public sealed record SensesResult : Questioned
{
    /// <summary>
    /// Where fork 24's hunt for stamina ended up, and how often it moved.
    /// <b>Zero moves with hunting on means it never adjusted anything.</b>
    /// </summary>
    public required double Settled { get; init; }

    /// <inheritdoc cref="Settled"/>
    public required int Moves { get; init; }

    /// <summary>
    /// The mean share of a question's deaths that were routes unable to pay for
    /// the hop they were on.
    /// </summary>
    /// <remarks>
    /// <b>REPORTED BECAUSE IT FAILED, AND THE FAILURE IS THE USEFUL PART.</b>
    /// Fork 23 tried to make compression self-regulating by scaling how much it
    /// writes by this. It does not discriminate: inverse cost exists to exhaust
    /// the budget, so starvation is the normal way a route ends at every scale.
    /// Keeping it visible is how a future attempt can tell at a glance whether
    /// that is still true rather than re-deriving it.
    /// </remarks>
    public required double Hunger { get; init; }

    /// <summary>
    /// The mean share of DIED STRENGTH that the budget killed. <b>Fork 23's
    /// second candidate, and John's correction.</b>
    /// </summary>
    public required double Thwarted { get; init; }

    /// <inheritdoc/>
    protected override string Shown => "moments";

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Three, and it is the whole task.</b> Sight and touch never co-occur, so
    /// reaching one from the other is a two-hop chain of length three. If nothing
    /// ever walked that far, any accuracy here came from somewhere other than
    /// composition and the headline number means nothing.
    /// </remarks>
    protected override int Composes => 3;

    /// <inheritdoc/>
    protected override string Stalled => "no route composed anything";

    /// <inheritdoc/>
    protected override void Beyond(List<string> wrong)
    {
        // Nothing beyond the asking. Fork 24's numbers are reported rather than
        // ranged, because a controller that never moved is a finding and not a
        // fault -- see Settled.
    }

    public override string ToString() =>
        $"moments={Moments} asked={Asked} right={Right} silent={Silent} " +
        $"accuracy={Accuracy:F4} chance={Chance:F4} | " +
        $"reflect={(Reflecting ? "on" : "off")} wrote={Reflected} | " +
        $"nodes={Nodes} edges={Edges} spread=[{string.Join(",", Spread)}] | " +
        $"chains={{{Plumbing.Lengths}}} deepest={Deepest} | " +
        $"msgs={Messages} halted={Halted} unbalanced={Unbalanced} unsettled={Unsettled} " +
        $"hunger={Hunger:F2} thwarted={Thwarted:F2} | " +
        $"stamina={Settled} moves={Moves}{Wrong}";
}

/// <summary>
/// The senses world, wired to the graph.
/// </summary>
/// <remarks>
/// <b>Scored prequentially and never trained-then-tested.</b> A question is
/// asked, settled, and then learning carries on — C4 forbids a run that stops,
/// so there is no "after training" to test in.
/// </remarks>
public sealed class SensesRun : IDisposable
{
    private readonly Fabric _fabric;
    private readonly InputMachine<Coded> _senses;
    private readonly Senses _world;
    private readonly WalkSettings _dials;

    /// <summary>
    /// Fork 24. <b>Always hunting — there is no hand-set arm here any more.</b>
    /// <see cref="WalkSettings.Stamina"/> is where the hunt STARTS, and the
    /// convergence test asserts the answer does not depend on it.
    /// </summary>
    /// <remarks>
    /// <b>THIS IS THE ONLY WORLD THE CONTROLLER IS WIRED INTO, and that is a gap
    /// rather than a setting.</b> The dial that used to switch it off is gone;
    /// what is left is six worlds that never call it, which the plan names as
    /// work and not as an arm.
    /// </remarks>
    private readonly Budget _budget;

    /// <param name="world">The senses to show.</param>
    /// <param name="dials">The walk.</param>
    /// <param name="seed">This run's own generator.</param>
    /// <param name="clusters">How many clusters to stand up.</param>
    /// <param name="replicas">Ring points per cluster.</param>
    /// <param name="late">
    /// <inheritdoc cref="Bus.Lateness" path="/summary"/> <b>This is the world that
    /// can see it.</b> Sight and touch never co-occur here, so the answer can only
    /// be COMPOSED across hops — which is precisely what lateness disturbs, where a
    /// world whose answer is one hop away would survive anything.
    /// </param>
    public SensesRun(
        SensesSettings world,
        WalkSettings dials,
        int seed,
        int clusters = 8,
        int replicas = 256,
        Bus.Lateness? late = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        _world = new Senses(world, seed);
        _dials = dials;
        _budget = new Budget(dials.Stamina, Budgeting.Standard);
        _fabric = new Fabric(dials, seed, clusters, replicas, late);

        _senses = _fabric.Watching("senses", dials);
    }

    /// <inheritdoc cref="Bus.HybridBus.Delayed"/>
    public long Delayed => _fabric.Bus.Delayed;

    /// <summary>
    /// Shows moments, and every <paramref name="every"/> of them stops to ask
    /// what a sight feels like.
    /// </summary>
    public async Task<SensesResult> RunAsync(
        int moments, int every = 10, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(moments);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(every);

        int asked = 0, right = 0, silent = 0, unbalanced = 0, unsettled = 0;
        double hunger = 0.0, thwarted = 0.0;
        long halted = 0;

        var reflected = 0;
        var chains = new Chains();

        for (var moment = 0; moment < moments; moment++)
        {
            var thought = await _senses
                .ObserveAsync(Coded.Of(_world.Moment()), moment, ct: ct).ConfigureAwait(false);
            await _fabric.QuietAsync(ct).ConfigureAwait(false);

            // FORK 21 REFLECTS ON WHAT WAS OBSERVED AND NEVER ON WHAT WAS ASKED.
            // Writing a question's own answer back would teach the graph the
            // test — the score would climb because the measurement had leaked
            // into the training, which is the one way this number could rise
            // while meaning nothing.
            if (thought is not null)
                reflected += await _senses
                    .ReflectAsync(thought, moment, ct).ConfigureAwait(false);

            if (moment % every != 0 || moment == 0) continue;

            var concept = moment % _world.Concepts;
            var (answer, stopped, balanced, settled, hungry, cutOff, everything) =
                await AskingAsync(concept, ct).ConfigureAwait(false);

            asked++;
            halted += stopped;
            hunger += hungry;
            thwarted += cutOff;
            if (!balanced) unbalanced++;
            if (!settled) unsettled++;

            chains.Fold(everything);

            if (answer is null) silent++;
            else if (Senses.Concept(answer.Value) == concept) right++;
        }

        _fabric.Failures();

        return new SensesResult
        {
            Moments = moments,
            Asked = asked,
            Right = right,
            Silent = silent,
            Chance = _world.Chance,
            Reflections = Reflections.Of(_dials, reflected),
            Plumbing = _fabric.Facts(chains, unbalanced),
            Halted = halted,
            Unsettled = unsettled,
            Hunger = asked == 0 ? 0.0 : hunger / asked,
            Thwarted = asked == 0 ? 0.0 : thwarted / asked,
            Settled = _budget.Stamina,
            Moves = _budget.Moves,
        };
    }

    /// <summary>
    /// Shows a concept's sight codes and asks which touch they lead to.
    /// </summary>
    /// <remarks>
    /// <b>Sight and touch have never occurred together</b>, so nothing here can
    /// be a lookup: the only route from one to the other runs through sound.
    /// </remarks>
    public async Task<Code?> AskAsync(int concept, CancellationToken ct = default) =>
        (await AskingAsync(concept, ct).ConfigureAwait(false)).Answer;

    /// <summary>
    /// Asks the same question several times at once and takes the majority.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>JOHN'S DESIGN, 2026-08-02, AND IT IS THE SHAPE THE ARCHITECTURE ALREADY
    /// IMPLIES.</b> A thought is identified by its broadcast id, so several
    /// concurrent thoughts about one question are not a special case — they are
    /// what the accounting was built for. Same redundancy as asking repeatedly,
    /// one round trip instead of <paramref name="votes"/> of them.
    /// </para>
    /// <para>
    /// <b>It exists because the walk disagrees with itself.</b> Delivery is
    /// concurrent, so an identical question does not always get an identical
    /// answer — measured at 0.8833 agreement. Voting recovers what one walk
    /// drops: 0.9688 to 0.9974 over 8 seeds, about 4.7 standard errors.
    /// </para>
    /// <para>
    /// <b>This is C2 being paid for rather than complained about.</b> The
    /// constraint says messages are late, jittered and out of order; redundancy
    /// is the ordinary answer to that, and it costs queries rather than
    /// coordination.
    /// </para>
    /// </remarks>
    public async Task<Code?> AskAsync(int concept, int votes, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(votes);

        var asking = new Task<Asking>[votes];
        for (var i = 0; i < votes; i++) asking[i] = AskingAsync(concept, ct);

        var answers = await Task.WhenAll(asking).ConfigureAwait(false);

        return Majority.Of(answers.Select(answer => answer.Answer)).Chosen;
    }

    /// <summary>One question, with the plumbing left attached.</summary>
    private readonly record struct Asking(
        Code? Answer,
        int Halted,
        bool Balanced,
        bool Settled,
        double Hunger,
        double Thwarted,
        IReadOnlyList<Arrival> Reached);

    /// <summary>The same question, with the plumbing left attached.</summary>
    private async Task<Asking> AskingAsync(int concept, CancellationToken ct)
    {
        // THE ANSWER IS ALWAYS ASKED AT THE SETTLED BUDGET. Fork 24's probe used
        // to BE this question, so a third of the run was answered at half the
        // depth the world needs and marked wrong when it failed.
        var origin = _world.Of(Senses.Sight, concept);

        var thought = await _senses
            .ThinkAsync(origin, _budget.Stamina, null, ct)
            .ConfigureAwait(false);

        var settled = await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);

        var reached = thought.BestOf(Senses.Touch, 2);
        var report = new Asking(
            reached.Count == 0 ? null : reached[0].Endpoint,
            thought.Halted,
            thought.Balanced(),
            settled,
            thought.Hunger,
            thought.Thwarted,
            thought.Best(int.MaxValue));

        // FORK 24 IS TOLD HOW CLEARLY IT ARRIVED, NOT MERELY WHETHER IT DID.
        //
        // REACHING IS NOT ANSWERING, and `Reached` could not tell them apart: a
        // walk at half the budget still reaches SOME touch code, so the probe
        // scored as a success while answering wrong -- and the probe is billed,
        // because every question here is the measurement. Measured on `Senses`:
        // 0.6051 with the controller against 0.8154 with it bypassed.
        //
        // SO THE COST IS THE SEPARATION, which `Note` was always the general form
        // for -- its own note says a richer cost "is a quantity the harness
        // already computes and has never been fed to anything". A top answer that
        // barely beats the runner-up did not discriminate, however surely it
        // arrived; one that beats it clearly did. NOTHING HERE READS WHETHER THE
        // ANSWER WAS RIGHT: the margin is the walk's own, so the controller still
        // cannot see the score, which is what keeps C4 intact.
        // AND THE PROBE IS ASKED SEPARATELY AND NEVER SCORED, which is the whole
        // of the fix. C4 leaves no free question -- every question here IS the
        // measurement -- so the hunt cannot borrow one. It can ask its OWN, and
        // pay in traffic rather than in accuracy. Measured on `Senses`: 0.6051
        // billed against 0.8154 unbilled, and the second is what a run that never
        // probes reads too, so the probe was the entire cost.
        var trying = _budget.Next();

        if (Math.Abs(trying - _budget.Stamina) < double.Epsilon)
        {
            _budget.Note(Separation(reached));
        }
        else
        {
            var probe = await _senses.ThinkAsync(origin, trying, null, ct)
                .ConfigureAwait(false);

            await _fabric.SettleAsync(probe, ct).ConfigureAwait(false);

            _budget.Note(Separation(probe.BestOf(Senses.Touch, 2)));

            _senses.Forget(probe.Id);
        }

        _senses.Forget(thought.Id);
        return report;
    }

    /// <summary>
    /// How clearly the best answer beat the next — <b>a cost, so lower is
    /// better.</b>
    /// </summary>
    /// <remarks>
    /// <b>NOTHING REACHED IS THE WORST AND COSTS ONE</b>, which is exactly what
    /// <see cref="Budget.Reached"/> reported and keeps the two scales comparable.
    /// A lone arrival separated everything it found, so it costs nothing. Two
    /// arrivals cost the share of the winner the runner-up took — a tie costs
    /// one, and a rout costs nothing.
    /// </remarks>
    private static double Separation(IReadOnlyList<Arrival> reached)
    {
        if (reached.Count == 0) return 1.0;
        if (reached.Count == 1) return 0.0;

        var best = reached[0].Score;

        // A NON-POSITIVE WINNER SEPARATES NOTHING. Scores are accumulated route
        // strengths and cannot be negative, so this is the degenerate case where
        // the walk arrived with no weight at all.
        return best <= 0.0 ? 1.0 : Math.Clamp(reached[1].Score / best, 0.0, 1.0);
    }

    public void Dispose() => _fabric.Dispose();
}
