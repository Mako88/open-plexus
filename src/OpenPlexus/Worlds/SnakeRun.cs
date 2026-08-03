using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using OpenPlexus.Thinking;

namespace OpenPlexus.Worlds;

/// <summary>
/// What a run did. <b>Counts, not claims.</b>
/// </summary>
/// <remarks>
/// <see cref="ChosenByChain"/> is the number this whole project has never been
/// able to report: how many times a chain of reasoning caused something to
/// happen in a world. Everything else is context for reading it.
/// </remarks>
public sealed record RunResult
{
    /// <summary>Steps taken before the run ended.</summary>
    public required int Steps { get; init; }

    /// <summary>
    /// Steps where a chain reached an action code and that action was taken.
    /// </summary>
    public required int ChosenByChain { get; init; }

    /// <summary>Steps where a thought ran and reached no action code.</summary>
    public required int ReachedNothing { get; init; }

    /// <summary>
    /// Steps where nothing changed, so no thought started at all. A stable
    /// scene is silent — this is the design working, not failing.
    /// </summary>
    public required int Silent { get; init; }

    /// <summary>Fruit taken. <b>Nothing declares food good</b>; this only counts it.</summary>
    public required int Ate { get; init; }

    public required int FinalLength { get; init; }

    public required double FinalEnergy { get; init; }

    public required bool Alive { get; init; }

    /// <summary>How many nodes came into existence across every cluster.</summary>
    public required int Nodes { get; init; }

    /// <summary>Cluster departures seen. Zero unless something left.</summary>
    public required int Deaths { get; init; }

    /// <summary>
    /// Thoughts whose accounting did not add up when the run read them.
    /// </summary>
    /// <remarks>
    /// <b>An integrity check on the whole distributed accounting, run for real
    /// rather than in a fixture.</b> `origins + splits - deaths == live` holds
    /// by construction, but the per-cluster in-flight counts are built from an
    /// entirely separate quantity — the routing named in each report — so the
    /// two agreeing says the routes really were where the origin thought they
    /// were.
    /// </remarks>
    public required int Unbalanced { get; init; }

    /// <summary>Every message the bus carried over the run.</summary>
    public required long Messages { get; init; }

    /// <summary>
    /// How well the graph foresaw what it was about to see, given what it was
    /// about to do.
    /// </summary>
    public required Foresight Foresight { get; init; }

    /// <summary>
    /// Of the steps a chain chose, how many chose the action just taken.
    /// </summary>
    /// <remarks>
    /// <b>The check that decides whether <see cref="ChosenByChain"/> means
    /// anything.</b> The action joins the occasion it was taken in, so the last
    /// action is tightly bound to the current view — a walk that only ever
    /// returns what the body just did would make "a chain caused a move" true
    /// and empty. If this equals <see cref="ChosenByChain"/>, the chain is a
    /// mirror.
    /// </remarks>
    public required int EchoedLast { get; init; }

    /// <summary>
    /// Routes killed by the horizon rather than by economics, summed over the
    /// run. <b>A walk that hit the horizon looks exactly like one that
    /// finished unless this is reported.</b>
    /// </summary>
    public required long Halted { get; init; }
}

/// <summary>
/// What decides the move.
/// </summary>
/// <remarks>
/// <b>Controls that change ONE thing.</b> <see cref="SnakeRun.PlayAsync"/>'s
/// <c>blind</c> flag changes two — it stops the action joining the occasion,
/// which alters the graph <i>and</i> forces every move to be random — so it
/// cannot say whether the chain helps. These can: the graph learns identically
/// under all three and only the choice differs.
/// </remarks>
public enum Policy
{
    /// <summary>Take the action a chain reached, and fall back to random.</summary>
    Chain,

    /// <summary>Ignore the chain entirely. <b>The floor.</b></summary>
    Random,

    /// <summary>
    /// Ignore the chain and repeat the last action.
    /// </summary>
    /// <remarks>
    /// <b>The control for momentum.</b> Reversing into the neck is instantly
    /// fatal, so anything that repeats the last action more often than chance
    /// survives longer for a reason that has nothing to do with reasoning. If
    /// this matches <see cref="Chain"/>, that is the whole explanation.
    /// </remarks>
    Repeat,
}

/// <summary>
/// Snake, wired to the graph, with the loop closed.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the first place in this project where a chain can cause
/// anything.</b> On <c>master</c> the walk hands back what it reached and
/// stops; every action in every experiment there is a hand-written policy or a
/// random draw.
/// </para>
/// <para>
/// <b>Falling back to a random move is deliberate and is counted.</b> When no
/// chain reaches an action, the snake still has to do something, and inventing
/// a preference there would be exactly the invented internal score this design
/// refuses. So it moves at random and <see cref="RunResult.ReachedNothing"/>
/// says how often — which also makes random play the arm this is measured
/// against rather than a hidden default.
/// </para>
/// <para>
/// <b>The run waits for each thought to settle before acting, and that is the
/// HARNESS, not the architecture.</b> Snake is turn-based, so there is a
/// natural moment to decide. A continuous world would act on the best chain
/// arrived so far and let later arrivals refine it, which is what
/// <see cref="Thinking.Thought.Best"/> is readable at any time for.
/// </para>
/// </remarks>
public sealed class SnakeRun : IDisposable
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    private readonly HybridBus _bus = new();
    private readonly Ring _ring;
    private readonly LocalClusters _local;
    private readonly List<Cluster> _clusters = [];
    private readonly List<IDisposable> _handles = [];
    private readonly InputMachine<SnakeFrame> _eye;
    private readonly OutputMachine _hand;
    private readonly Snake _snake;
    private readonly Random _fallback;
    private readonly List<Exception> _faults = [];
    private readonly SnakeSense _sense = new();
    private readonly Foresight _foresight = new();

    /// <summary>Every vision code seen, so a blind guess can draw from the same alphabet.</summary>
    private readonly List<Code> _alphabet = [];

    /// <summary>
    /// The blind guess draws from here and NOT from the policy's own stream.
    /// </summary>
    /// <remarks>
    /// <b>Sharing one generator would make the control change the thing it is
    /// controlling for</b> — adding prediction moved the random policy's
    /// trajectory and broke a test that had nothing to do with prediction,
    /// which is how a measurement quietly stops measuring what it says.
    /// </remarks>
    private readonly Random _guessing;

    public SnakeRun(
        SnakeSettings world,
        WalkSettings dials,
        int seed,
        int clusters = 8,
        int replicas = 256)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clusters);

        _snake = new Snake(world, seed);
        _fallback = new Random(seed);
        _guessing = new Random(~seed);
        _ring = new Ring(seed, replicas);
        _local = new LocalClusters(_ring);

        // A delivery that throws would otherwise vanish, and a run reporting
        // numbers built on swallowed failures is worse than one that stops.
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

        _eye = new InputMachine<SnakeFrame>(
            new MachineAddress("eye"),
            _sense,
            new LocalRendezvous(_local),
            _bus,
            _ring,
            dials);

        _handles.Add(_bus.Subscribe(_eye));
        _hand = new OutputMachine(new MachineAddress("hand"), SnakeSense.Turns);
    }

    /// <summary>Plays until the run ends or the step budget runs out.</summary>
    /// <param name="steps">The budget.</param>
    /// <param name="blind">
    /// <b>The control arm: cuts the one wire that makes an action reachable.</b>
    /// With this set, what the snake did never joins the occasion, so an action
    /// code gains no edges and no walk can arrive at one.
    /// </param>
    /// <param name="ct">Cancellation.</param>
    public async Task<RunResult> PlayAsync(
        int steps,
        bool cut = false,
        Policy policy = Policy.Chain,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(steps);

        Code? did = null;
        int taken = 0, byChain = 0, reachedNothing = 0, silent = 0, ate = 0, echoed = 0;
        long halted = 0;
        var unbalanced = 0;

        IReadOnlyList<Code> foreseen = [];
        IReadOnlyList<Code> blind = [];

        for (; taken < steps && _snake.Alive; taken++)
        {
            var before = _snake.Energy;
            var frame = new SnakeFrame { View = _snake.View(), Did = did };
            var present = _sense.Codify(frame);

            // PREQUENTIAL: last step's guess is settled against this step's
            // world BEFORE anything is counted, so the graph never sees the
            // answer before being asked.
            _foresight.Settle(foreseen, present, blind);
            Remember(present);

            var thought = await _eye.ObserveAsync(frame, taken, ct).ConfigureAwait(false);

            // Wait for the dust to settle before deciding. Turn-based world,
            // harness affordance -- see the note on this class.
            await _bus.WhenQuiet().WaitAsync(Patience, ct).ConfigureAwait(false);

            Code? chosen = thought is null ? null : _hand.Choose(thought);

            if (thought is null) silent++;
            else
            {
                halted += thought.Halted;
                if (!thought.Balanced()) unbalanced++;
                if (chosen is null) reachedNothing++;
            }

            if (policy != Policy.Chain) chosen = null;

            Code? meant = chosen is { } code && Understood(code) ? code : null;

            Code doing;
            if (meant is { } picked)
            {
                byChain++;
                if (did == picked) echoed++;
                doing = picked;
            }
            else
            {
                doing = policy == Policy.Repeat && did is { } last ? last : Anything();
            }

            // WHAT WOULD THE WORLD LOOK LIKE IF I DID THIS? A second broadcast
            // carrying the chosen action, narrowed to sensory codes rather than
            // to the output machine's. Same flood, different question -- and it
            // is conditional on the action, so a different action would ask a
            // different one.
            (foreseen, blind) = await ForeseeAsync(present, doing, ct).ConfigureAwait(false);

            Perform(doing);
            did = cut ? null : doing;

            if (_snake.Alive && _snake.Energy > before) ate++;
        }

        Failures();

        return new RunResult
        {
            Steps = taken,
            ChosenByChain = byChain,
            ReachedNothing = reachedNothing,
            Silent = silent,
            Ate = ate,
            FinalLength = _snake.Length,
            FinalEnergy = _snake.Energy,
            Alive = _snake.Alive,
            Nodes = _clusters.Sum(cluster => cluster.Count),
            Deaths = _eye.DeathsSeen,
            Halted = halted,
            EchoedLast = echoed,
            Unbalanced = unbalanced,
            Messages = _bus.Messages,
            Foresight = _foresight,
        };
    }

    /// <summary>
    /// Asks the graph what it expects to see next, given what is about to be
    /// done, and draws a blind guess of the same size to score it against.
    /// </summary>
    private async Task<(IReadOnlyList<Code> Foreseen, IReadOnlyList<Code> Blind)> ForeseeAsync(
        IReadOnlyCollection<Code> present, Code doing, CancellationToken ct)
    {
        if (present.Count == 0) return ([], []);

        var thought = await _eye.ThinkAsync([.. present, doing], ct).ConfigureAwait(false);
        await _bus.WhenQuiet().WaitAsync(Patience, ct).ConfigureAwait(false);

        // As many codes as an observation holds: predicting an
        // observation-sized set rather than a number nobody chose.
        var wanted = present.Count;
        var foreseen = thought.BestOf(SnakeQuantizer.Vision, wanted)
            .Select(a => a.Endpoint).ToArray();

        _eye.Forget(thought.Id);

        var blind = _alphabet.Count == 0
            ? []
            : Enumerable.Range(0, foreseen.Length)
                .Select(_ => _alphabet[_guessing.Next(_alphabet.Count)])
                .ToArray();

        return (foreseen, blind);
    }

    private void Remember(IReadOnlyCollection<Code> present)
    {
        foreach (var code in present)
            if (code.Modality == SnakeQuantizer.Vision && !_alphabet.Contains(code))
                _alphabet.Add(code);
    }

    /// <summary>Whether a code names something this world can actually do.</summary>
    private static bool Understood(Code code) => SnakeSense.Turned(code) is not null;

    /// <summary>A move nobody reasoned about. The floor everything is measured against.</summary>
    private Code Anything() => SnakeSense.Encode((Turn)_fallback.Next(3));

    private void Perform(Code code) => _snake.Steer(SnakeSense.Turned(code)!.Value);

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
