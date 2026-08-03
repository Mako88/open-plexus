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
public sealed record SensesResult
{
    /// <summary>Moments shown.</summary>
    public required int Moments { get; init; }

    /// <summary>Questions asked — a sight, answered with a touch.</summary>
    public required int Asked { get; init; }

    /// <summary>Of those, how many named the right concept's touch first.</summary>
    public required int Right { get; init; }

    /// <summary>Of those, how many the graph had nothing at all to say about.</summary>
    public required int Silent { get; init; }

    /// <summary>What a blind guess would score.</summary>
    public required double Chance { get; init; }

    /// <summary>
    /// Conclusions written back as observations. <b>Zero when fork 21 is off</b>,
    /// which is how a run says out loud whether the mechanism was even running.
    /// </summary>
    public required int Reflected { get; init; }

    /// <summary>Nodes across every cluster.</summary>
    public required int Nodes { get; init; }

    /// <summary>Partner entries across every node.</summary>
    public required int Edges { get; init; }

    /// <summary>How many nodes each cluster holds.</summary>
    public required IReadOnlyList<int> Spread { get; init; }

    /// <summary>
    /// How long the chains that came back were, by length.
    /// </summary>
    /// <remarks>
    /// <b>The single most revealing line in this world.</b> Sight reaches touch
    /// only through sound, so a correct answer is a chain of length three. If
    /// nothing ever walked that far, any accuracy here came from somewhere other
    /// than composition and the headline number means nothing.
    /// </remarks>
    public required IReadOnlyDictionary<int, int> ChainLengths { get; init; }

    /// <summary>What the bus carried.</summary>
    public required long Messages { get; init; }

    /// <summary>Routes killed by the horizon rather than by economics.</summary>
    public required long Halted { get; init; }

    /// <summary>Thoughts whose own accounting did not close.</summary>
    public required int Unbalanced { get; init; }

    /// <summary>
    /// Questions read before their walk had finished.
    /// </summary>
    /// <remarks>
    /// <b>Counted rather than absorbed.</b> A walk that had not finished
    /// produces "nothing reached", which is indistinguishable in a score from a
    /// graph that genuinely had nothing to say -- so a run where this is not
    /// zero has a silent-count that cannot be trusted.
    /// </remarks>
    public required int Unsettled { get; init; }

    /// <summary>Whether fork 21 was switched on for this run.</summary>
    public required bool Reflecting { get; init; }

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

    /// <summary>The share of questions answered correctly.</summary>
    public double Accuracy => Asked == 0 ? 0.0 : Right / (double)Asked;

    /// <summary>How far a route actually walked, at most.</summary>
    public int Deepest => ChainLengths.Count == 0 ? 0 : ChainLengths.Keys.Max();

    /// <summary>
    /// Everything out of the range it would be in if this world were wired the
    /// way it is supposed to be.
    /// </summary>
    /// <remarks>
    /// <b>John's ask, and it is the same one <see cref="RunReport"/> answers for
    /// snake:</b> a number gets swept, barely moves, and much later it turns out
    /// something was never connected. Every entry here is a quantity that could
    /// only be out of range if something had come unwired.
    /// </remarks>
    public IReadOnlyList<string> Complaints
    {
        get
        {
            var wrong = new List<string>();

            if (Moments == 0) wrong.Add("the run showed no moments");
            if (Asked == 0) wrong.Add("nothing was ever asked");
            if (Nodes == 0) wrong.Add("no node ever came into existence");
            if (Edges == 0) wrong.Add("no connection was ever formed");
            if (Messages == 0) wrong.Add("the bus carried nothing");
            if (Unbalanced > 0) wrong.Add($"{Unbalanced} thoughts did not balance");
            if (Unsettled > 0) wrong.Add($"{Unsettled} questions were read before their walk finished");

            // THE ONE THAT IS SPECIFIC TO THIS WORLD. Sight and touch never
            // co-occur, so reaching one from the other is a two-hop chain of
            // length three. Anything shallower is not the task.
            if (Deepest < 3)
                wrong.Add($"no route composed anything — deepest chain {Deepest}");

            if (Asked > 0 && Silent >= Asked) wrong.Add("every question went unanswered");
            if (Spread.Count(count => count > 0) < 2) wrong.Add("every node landed on one cluster");

            // FORK 21'S OWN WIRING CHECK. A dial that is on and does nothing is
            // a sweep arm that looks distinct and is not, which is exactly how
            // this project has fooled itself before.
            if (Reflecting && Reflected == 0)
                wrong.Add("reflection was switched on and wrote nothing");
            if (!Reflecting && Reflected > 0)
                wrong.Add($"reflection was off and still wrote {Reflected}");

            return wrong;
        }
    }

    public override string ToString()
    {
        var lengths = string.Join(
            " ", ChainLengths.OrderBy(e => e.Key).Select(e => $"{e.Key}:{e.Value}"));

        var line =
            $"moments={Moments} asked={Asked} right={Right} silent={Silent} " +
            $"accuracy={Accuracy:F4} chance={Chance:F4} | " +
            $"reflect={(Reflecting ? "on" : "off")} wrote={Reflected} | " +
            $"nodes={Nodes} edges={Edges} spread=[{string.Join(",", Spread)}] | " +
            $"chains={{{lengths}}} deepest={Deepest} | " +
            $"msgs={Messages} halted={Halted} unbalanced={Unbalanced} unsettled={Unsettled} " +
            $"hunger={Hunger:F2} thwarted={Thwarted:F2} | " +
            $"stamina={Settled} moves={Moves}";

        return Complaints.Count == 0
            ? line
            : $"{line} || WRONG: {string.Join("; ", Complaints)}";
    }
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
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long a question waits for its own walk to finish.
    /// </summary>
    /// <remarks>
    /// <b>Short, and every expiry is counted rather than absorbed.</b> A wait
    /// that quietly gives up produces an answer of "nothing reached" that is
    /// really "not finished yet", and the two are indistinguishable in a score.
    /// See <see cref="SensesResult.Unsettled"/>.
    /// </remarks>
    private static readonly TimeSpan Waiting = TimeSpan.FromMilliseconds(250);

    private readonly HybridBus _bus = new();
    private readonly Ring _ring;
    private readonly LocalClusters _local;
    private readonly List<Cluster> _clusters = [];
    private readonly List<IDisposable> _handles = [];
    private readonly InputMachine<IReadOnlyCollection<Code>> _senses;
    private readonly Senses _world;
    private readonly WalkSettings _dials;

    /// <summary>
    /// Fork 24. <b>Null when the stamina dial is hand-set</b>, which is the
    /// control the sweep is measured against.
    /// </summary>
    private readonly Budget? _budget;
    private readonly List<Exception> _faults = [];

    public SensesRun(
        SensesSettings world,
        WalkSettings dials,
        int seed,
        int clusters = 8,
        int replicas = 256)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        _world = new Senses(world, seed);
        _dials = dials;
        _budget = dials.Budget is null ? null : new Budget(dials.Stamina, dials.Budget);
        _ring = new Ring(seed, replicas);
        _local = new LocalClusters(_ring);
        _bus.Faults += failure => { lock (_faults) _faults.Add(failure); };

        for (var i = 0; i < clusters; i++)
        {
            var address = new ClusterAddress($"c{i}");
            _ring.Join(address);
            var cluster = new Cluster(address, _bus, _ring, dials);
            _local.Include(cluster);
            _clusters.Add(cluster);
            _handles.Add(_bus.Subscribe(cluster));
        }

        _senses = new InputMachine<IReadOnlyCollection<Code>>(
            new MachineAddress("senses"), new Passthrough(), new LocalRendezvous(_local),
            _bus, _ring, dials);

        _handles.Add(_bus.Subscribe(_senses));
    }

    /// <summary>The codes are already codes; there is nothing to quantise.</summary>
    private sealed class Passthrough : IQuantizer<IReadOnlyCollection<Code>>
    {
        public byte Modality => Senses.Sight;

        public IReadOnlyCollection<Code> Codify(IReadOnlyCollection<Code> observation) => observation;
    }

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
        var chains = new Dictionary<int, int>();

        for (var moment = 0; moment < moments; moment++)
        {
            var thought = await _senses
                .ObserveAsync(_world.Moment(), moment, ct).ConfigureAwait(false);
            await _bus.WhenQuiet().WaitAsync(Patience, ct).ConfigureAwait(false);

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

            // How far routes actually walked. Sight reaches touch only through
            // sound, so a correct answer is a chain of length three -- and a run
            // that never produced one cannot have composed anything.
            foreach (var arrival in everything)
                chains[arrival.Chain.Length] = chains.GetValueOrDefault(arrival.Chain.Length) + 1;

            if (answer is null) silent++;
            else if (Senses.Concept(answer.Value) == concept) right++;
        }

        Failures();

        return new SensesResult
        {
            Moments = moments,
            Asked = asked,
            Right = right,
            Silent = silent,
            Chance = _world.Chance,
            Reflected = reflected,
            Reflecting = _dials.Reflect is not null,
            Nodes = _clusters.Sum(cluster => cluster.Count),
            Edges = _clusters.Sum(cluster => cluster.Edges),
            Spread = [.. _clusters.Select(cluster => cluster.Count)],
            ChainLengths = chains,
            Messages = _bus.Messages,
            Halted = halted,
            Unbalanced = unbalanced,
            Unsettled = unsettled,
            Hunger = asked == 0 ? 0.0 : hunger / asked,
            Thwarted = asked == 0 ? 0.0 : thwarted / asked,
            Settled = _budget?.Stamina ?? _dials.Stamina,
            Moves = _budget?.Moves ?? 0,
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
        var thought = await _senses
            .ThinkAsync(_world.Of(Senses.Sight, concept), _budget?.Next() ?? _dials.Stamina, ct)
            .ConfigureAwait(false);

        var settled = await SettleAsync(thought, ct).ConfigureAwait(false);

        var reached = thought.BestOf(Senses.Touch, 1);
        var report = new Asking(
            reached.Count == 0 ? null : reached[0].Endpoint,
            thought.Halted,
            thought.Balanced(),
            settled,
            thought.Hunger,
            thought.Thwarted,
            thought.Best(int.MaxValue));

        // FORK 24 IS TOLD WHETHER IT REACHED WHAT IT WAS NARROWING TO, not
        // merely whether it reached somewhere. The caller knows what it was
        // looking for and the controller deliberately does not.
        _budget?.Reached(reached.Count > 0);

        _senses.Forget(thought.Id);
        return report;
    }

    /// <summary>
    /// Waits on the THOUGHT'S OWN ACCOUNTING, not on the bus.
    /// </summary>
    /// <remarks>
    /// <b>The bus going quiet does not mean the walk finished.</b> In-flight
    /// reaches zero in the gap between a cluster handling a message and
    /// dispatching what that message produced — fork 12. Reading a thought
    /// there gives an answer of "nothing reached" that is really "not
    /// finished", and the two are indistinguishable downstream.
    /// </remarks>
    /// <returns>Whether it settled rather than running out of patience.</returns>
    private async Task<bool> SettleAsync(Thought thought, CancellationToken ct)
    {
        await _bus.WhenQuiet().WaitAsync(Patience, ct).ConfigureAwait(false);

        // REAL ELAPSED TIME, NOT AN ITERATION COUNT. `Task.Delay(1)` on Windows
        // sleeps about fifteen milliseconds, so counting one per pass made a
        // two-second budget wait for thirty -- which looked exactly like a hang
        // and is the same class of mistake as trusting an unmeasured constant.
        var until = Environment.TickCount64 + (long)Waiting.TotalMilliseconds;

        while (!thought.Settled && Environment.TickCount64 < until)
        {
            await Task.Delay(1, ct).ConfigureAwait(false);
            await _bus.WhenQuiet().WaitAsync(Patience, ct).ConfigureAwait(false);
        }

        return thought.Settled;
    }

    private void Failures()
    {
        lock (_faults)
        {
            if (_faults.Count > 0) throw new AggregateException(_faults);
        }
    }

    public void Dispose()
    {
        foreach (var handle in _handles) handle.Dispose();
    }
}
