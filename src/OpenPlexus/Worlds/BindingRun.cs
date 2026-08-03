using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using OpenPlexus.Thinking;

namespace OpenPlexus.Worlds;

/// <summary>
/// What the binding world measured. <b>Counts, not claims.</b>
/// </summary>
public sealed record BindingResult
{
    /// <summary>Scenes shown.</summary>
    public required int Moments { get; init; }

    /// <summary>Questions asked — a colour, answered with one of the two shapes present.</summary>
    public required int Asked { get; init; }

    /// <summary>Of those, how many named the shape that colour was actually bound to.</summary>
    public required int Right { get; init; }

    /// <summary>Of those, how many reached neither shape in the scene.</summary>
    public required int Silent { get; init; }

    /// <summary>What a blind guess would score.</summary>
    public required double Chance { get; init; }

    /// <summary>Whether a colour kept its shape for the life of the world.</summary>
    public required bool Bound { get; init; }

    /// <summary>Conclusions written back as observations. Zero when fork 21 is off.</summary>
    public required int Reflected { get; init; }

    /// <summary>Whether fork 21 was switched on for this run.</summary>
    public required bool Reflecting { get; init; }

    /// <summary>Nodes across every cluster.</summary>
    public required int Nodes { get; init; }

    /// <summary>Partner entries across every node.</summary>
    public required int Edges { get; init; }

    /// <summary>How many nodes each cluster holds.</summary>
    public required IReadOnlyList<int> Spread { get; init; }

    /// <summary>How long the chains that came back were, by length.</summary>
    /// <remarks>
    /// <b>A colour and a shape occur in the same scene</b>, so the answer here is
    /// reachable in one hop and a chain of length two is enough. That is the
    /// opposite of the senses world, where nothing shallower than three could be
    /// the task — and it is why "the walk was too shallow" is not available as an
    /// explanation for anything measured here.
    /// </remarks>
    public required IReadOnlyDictionary<int, int> ChainLengths { get; init; }

    /// <summary>
    /// How often a question reached BOTH shapes in the scene.
    /// </summary>
    /// <remarks>
    /// <b>THE CHECK THAT STOPS THIS WORLD SCORING CHANCE FOR THE WRONG REASON.</b>
    /// A forced choice between two candidates is only a forced choice if both
    /// candidates were actually reached. If the walk routinely finds one and not
    /// the other, the score is measuring what the walk happened to touch, not what
    /// it preferred — and a coin-flip result would then say nothing about binding.
    /// </remarks>
    public required int SawBoth { get; init; }

    /// <summary>
    /// How many answers named the queried colour's OWN concept.
    /// </summary>
    /// <remarks>
    /// <b>The mechanism, rather than the score.</b> A colour co-occurs with its
    /// own concept's shape in every scene it appears in, and with any other shape
    /// only when that concept happens to be the other object — so the counts point
    /// at the colour's own kind whatever the scene did. This says how completely
    /// the system is doing that one thing, which turns "at chance" from a null
    /// result into a description of what it is doing instead.
    /// </remarks>
    public required int Echoed { get; init; }

    /// <summary>
    /// How many of the asked scenes actually swapped the queried object's shape.
    /// </summary>
    /// <remarks>
    /// <b>The fairness of the coin, in the sample that was scored.</b> The world
    /// swaps half the time in the long run; the questions are a subsample of it,
    /// and a subsample that drifted would move the score without anything in the
    /// graph changing.
    /// </remarks>
    public required int Swapped { get; init; }

    /// <summary>What the bus carried.</summary>
    public required long Messages { get; init; }

    /// <summary>Routes killed by the horizon rather than by economics.</summary>
    public required long Halted { get; init; }

    /// <summary>Thoughts whose own accounting did not close.</summary>
    public required int Unbalanced { get; init; }

    /// <summary>Questions read before their walk had finished — fork 22.</summary>
    public required int Unsettled { get; init; }

    /// <summary>The share of questions answered correctly.</summary>
    public double Accuracy => Asked == 0 ? 0.0 : Right / (double)Asked;

    /// <summary>The share of questions where both candidates were in reach.</summary>
    public double Forced => Asked == 0 ? 0.0 : SawBoth / (double)Asked;

    /// <summary>The share of answers that named the queried colour's own concept.</summary>
    public double Echo => Asked == 0 ? 0.0 : Echoed / (double)Asked;

    /// <summary>How far a route actually walked, at most.</summary>
    public int Deepest => ChainLengths.Count == 0 ? 0 : ChainLengths.Keys.Max();

    /// <summary>
    /// Everything out of the range it would be in if this world were wired the
    /// way it is supposed to be.
    /// </summary>
    /// <remarks>
    /// <b>Load-bearing here more than anywhere else in the project.</b> Every
    /// other world's headline is a number going UP, where a disconnected dial
    /// shows up as a disappointment. This world's prediction is that a number
    /// stays flat — and a broken harness produces exactly that, indistinguishably.
    /// So the complaints are what stands between "measured at chance" and "never
    /// measured anything".
    /// </remarks>
    public IReadOnlyList<string> Complaints
    {
        get
        {
            var wrong = new List<string>();

            if (Moments == 0) wrong.Add("the run showed no scenes");
            if (Asked == 0) wrong.Add("nothing was ever asked");
            if (Nodes == 0) wrong.Add("no node ever came into existence");
            if (Edges == 0) wrong.Add("no connection was ever formed");
            if (Messages == 0) wrong.Add("the bus carried nothing");
            if (Unbalanced > 0) wrong.Add($"{Unbalanced} thoughts did not balance");
            if (Unsettled > 0) wrong.Add($"{Unsettled} questions were read before their walk finished");

            // A colour reaches a shape in one hop, so a chain of two is the task.
            // Anything shallower means nothing ever left the code it started on.
            if (Deepest < 2)
                wrong.Add($"no route ever left its origin — deepest chain {Deepest}");

            if (Asked > 0 && Silent >= Asked) wrong.Add("every question went unanswered");

            // THE ONE THAT IS SPECIFIC TO THIS WORLD. See SawBoth: a chance score
            // means nothing if the choice was never actually forced.
            if (Asked > 0 && SawBoth * 2 < Asked)
                wrong.Add($"only {SawBoth} of {Asked} questions reached both candidates");

            if (Spread.Count(count => count > 0) < 2) wrong.Add("every node landed on one cluster");

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
            $"binding={(Bound ? "stable" : "per-scene")} " +
            $"moments={Moments} asked={Asked} right={Right} silent={Silent} " +
            $"accuracy={Accuracy:F4} chance={Chance:F4} forced={Forced:F4} " +
            $"echo={Echo:F4} swapped={Swapped} | " +
            $"reflect={(Reflecting ? "on" : "off")} wrote={Reflected} | " +
            $"nodes={Nodes} edges={Edges} spread=[{string.Join(",", Spread)}] | " +
            $"chains={{{lengths}}} deepest={Deepest} | " +
            $"msgs={Messages} halted={Halted} unbalanced={Unbalanced} unsettled={Unsettled}";

        return Complaints.Count == 0
            ? line
            : $"{line} || WRONG: {string.Join("; ", Complaints)}";
    }
}

/// <summary>
/// The binding world, wired to the graph.
/// </summary>
/// <remarks>
/// <para>
/// <b>Scored prequentially</b>, like every other world here: a question is asked
/// about the scene that was just shown, settled, and then learning carries on.
/// </para>
/// <para>
/// <b>The question is asked with the queried object's colour and nothing else,
/// and that is not a shortcut — it is the finding arriving early.</b> There is no
/// way in this architecture to say <i>this one</i>. An object can only be named
/// by its attributes, and its attributes are what is being asked about, so
/// broadcasting the whole scene to supply context would broadcast the answer's
/// competitor on exactly equal terms. The failure shows up before the answer
/// does: the question cannot be posed.
/// </para>
/// </remarks>
public sealed class BindingRun : IDisposable
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    /// <summary>How long a question waits for its own walk. Every expiry is counted.</summary>
    private static readonly TimeSpan Waiting = TimeSpan.FromMilliseconds(250);

    private readonly HybridBus _bus = new();
    private readonly Ring _ring;
    private readonly LocalClusters _local;
    private readonly List<Cluster> _clusters = [];
    private readonly List<IDisposable> _handles = [];
    private readonly InputMachine<IReadOnlyCollection<Code>> _eyes;
    private readonly Binding _world;
    private readonly WalkSettings _dials;
    private readonly List<Exception> _faults = [];

    public BindingRun(
        BindingSettings world,
        WalkSettings dials,
        int seed,
        int clusters = 8,
        int replicas = 256)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        _world = new Binding(world, seed);
        _dials = dials;
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

        _eyes = new InputMachine<IReadOnlyCollection<Code>>(
            new MachineAddress("scene"), new Passthrough(), new LocalRendezvous(_local),
            _bus, _ring, dials);

        _handles.Add(_bus.Subscribe(_eyes));
    }

    /// <summary>The codes are already codes; there is nothing to quantise.</summary>
    private sealed class Passthrough : IQuantizer<IReadOnlyCollection<Code>>
    {
        public byte Modality => Binding.Colour;

        public IReadOnlyCollection<Code> Codify(IReadOnlyCollection<Code> observation) => observation;
    }

    /// <summary>
    /// Shows scenes, and every <paramref name="every"/> of them asks which shape
    /// one of the objects just shown had.
    /// </summary>
    /// <param name="moments">How many scenes to show.</param>
    /// <param name="every">Ask on every nth scene.</param>
    /// <param name="votes">
    /// How many concurrent thoughts settle one question. <b>The walk disagrees
    /// with itself</b> — delivery is concurrent — so a single ask carries noise
    /// that a chance-level result could hide behind either way.
    /// </param>
    /// <param name="ct">Cancellation.</param>
    public async Task<BindingResult> RunAsync(
        int moments, int every = 10, int votes = 3, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(moments);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(every);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(votes);

        int asked = 0, right = 0, silent = 0, sawBoth = 0, unbalanced = 0, unsettled = 0;
        int echoed = 0, swapped = 0;
        long halted = 0;

        var reflected = 0;
        var chains = new Dictionary<int, int>();

        for (var moment = 0; moment < moments; moment++)
        {
            var scene = _world.Next();

            var thought = await _eyes
                .ObserveAsync(scene.Codes, moment, ct).ConfigureAwait(false);
            await _bus.WhenQuiet().WaitAsync(Patience, ct).ConfigureAwait(false);

            // Reflection sees what was OBSERVED and never what was asked, for the
            // same reason it does in the senses world: writing a question's own
            // answer back would teach the graph the test.
            if (thought is not null)
                reflected += await _eyes
                    .ReflectAsync(thought, moment, ct).ConfigureAwait(false);

            if (moment % every != 0 || moment == 0) continue;

            // ALTERNATING, so nothing rests on which object happened to be drawn
            // first. Both objects are asked about equally often.
            var which = asked % Binding.PerScene;

            var (answer, both, stopped, balanced, settled, reached) =
                await AskingAsync(scene, which, votes, ct).ConfigureAwait(false);

            asked++;
            halted += stopped;
            if (both) sawBoth++;
            if (!balanced) unbalanced++;
            if (!settled) unsettled++;
            if (scene.Shapes[which] != scene.Colours[which]) swapped++;

            foreach (var arrival in reached)
                chains[arrival.Chain.Length] = chains.GetValueOrDefault(arrival.Chain.Length) + 1;

            if (answer is null) silent++;
            else if (answer == scene.Shapes[which]) right++;

            if (answer == scene.Colours[which]) echoed++;
        }

        Failures();

        return new BindingResult
        {
            Moments = moments,
            Asked = asked,
            Right = right,
            Silent = silent,
            SawBoth = sawBoth,
            Echoed = echoed,
            Swapped = swapped,
            Chance = Binding.Chance,
            Bound = _world.Bound,
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
        };
    }

    /// <summary>One question, with the plumbing left attached.</summary>
    private readonly record struct Asking(
        int? Answer,
        bool Both,
        int Halted,
        bool Balanced,
        bool Settled,
        IReadOnlyList<Arrival> Reached);

    /// <summary>
    /// Asks which shape one object had, several times at once, and takes the
    /// majority.
    /// </summary>
    private async Task<Asking> AskingAsync(
        Scene scene, int which, int votes, CancellationToken ct)
    {
        var asking = new Task<Asking>[votes];
        for (var i = 0; i < votes; i++) asking[i] = OnceAsync(scene, which, ct);

        var answers = await Task.WhenAll(asking).ConfigureAwait(false);

        // MOST VOTES WINS, AND SILENCE DOES NOT GET ONE.
        var tally = new Dictionary<int, int>();
        foreach (var answer in answers)
            if (answer.Answer is { } shape) tally[shape] = tally.GetValueOrDefault(shape) + 1;

        var chosen = tally.Count == 0
            ? (int?)null
            : tally.OrderByDescending(e => e.Value).ThenBy(e => e.Key).First().Key;

        return new Asking(
            chosen,
            answers.Any(one => one.Both),
            answers.Sum(one => one.Halted),
            answers.All(one => one.Balanced),
            answers.All(one => one.Settled),
            [.. answers.SelectMany(one => one.Reached)]);
    }

    /// <summary>
    /// One walk: broadcast a colour, and see which of the scene's two shapes it
    /// ranks first.
    /// </summary>
    /// <remarks>
    /// <b>Ranked by concept, not by code.</b> An attribute has several codes and
    /// the walk may reach a different one from the one the scene happened to show,
    /// which is identity doing work rather than a lookup — so the comparison is
    /// on what the code stands for.
    /// </remarks>
    private async Task<Asking> OnceAsync(Scene scene, int which, CancellationToken ct)
    {
        var thought = await _eyes
            .ThinkAsync(_world.Of(Binding.Colour, scene.Colours[which]), _dials.Stamina, ct)
            .ConfigureAwait(false);

        var settled = await SettleAsync(thought, ct).ConfigureAwait(false);

        var candidates = scene.Shapes.ToHashSet();
        var ranked = thought.BestOf(Binding.Shape, int.MaxValue);

        // FIRST CANDIDATE IN THE RANKING, not the top shape overall. A shape from
        // some other scene outranking both is not a wrong answer to this
        // question -- it is an answer to a question nobody asked.
        int? answer = null;
        var seen = new HashSet<int>();

        foreach (var arrival in ranked)
        {
            var concept = Binding.Concept(arrival.Endpoint);
            if (!candidates.Contains(concept)) continue;

            answer ??= concept;
            seen.Add(concept);
        }

        var report = new Asking(
            answer,
            seen.Count == candidates.Count,
            thought.Halted,
            thought.Balanced(),
            settled,
            thought.Best(int.MaxValue));

        _eyes.Forget(thought.Id);
        return report;
    }

    /// <summary>
    /// Waits on the THOUGHT'S OWN ACCOUNTING, not on the bus — fork 12.
    /// </summary>
    private async Task<bool> SettleAsync(Thought thought, CancellationToken ct)
    {
        await _bus.WhenQuiet().WaitAsync(Patience, ct).ConfigureAwait(false);

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
