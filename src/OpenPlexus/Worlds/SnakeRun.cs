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

    /// <summary>
    /// Walks read before they had finished — <b>fork 22, and snake was the last
    /// world without the check.</b>
    /// </summary>
    /// <remarks>
    /// <b>THREE READS PER STEP, AND EVERY ONE OF THEM MATTERS DIFFERENTLY.</b> An
    /// unfinished choice becomes a move made at random and counted as the chain's;
    /// an unfinished prediction names fewer codes than the walk reached and reads
    /// as a worse predictor. Both are silent, and both look like results.
    /// </remarks>
    public required int Unsettled { get; init; }

    /// <summary>Every message the bus carried over the run.</summary>
    public required long Messages { get; init; }


    /// <summary>
    /// How well the graph foresaw what it was about to see, given what it was
    /// about to do.
    /// </summary>
    public required Foresight Foresight { get; init; }

    /// <summary>
    /// The same predictions, scored only against what CHANGED.
    /// </summary>
    /// <remarks>
    /// <b>Predicting the whole next observation is mostly predicting
    /// persistence</b>, and persistence is free — most codes are still there
    /// next frame whatever anyone does. Scoring against the onsets alone asks
    /// the only part of the question that is not already answered by saying
    /// "the same again".
    /// </remarks>
    public required Foresight Novelty { get; init; }

    /// <summary>
    /// <b>FORK 18'S METRIC — what this project scores, by John's call.</b>
    /// </summary>
    /// <inheritdoc cref="Thinking.Consequence"/>
    public required Consequence Consequence { get; init; }

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

    /// <summary>
    /// Steps where the concurrent votes did not all name the same action.
    /// </summary>
    /// <remarks>
    /// <b>THE WIRING CHECK ON VOTING, AND IT IS THE POINT OF MEASURING IT AT
    /// ALL.</b> "Voting changed nothing" is also exactly what a disconnected dial
    /// looks like, so the outcome cannot be the check. Zero at one vote by
    /// construction.
    /// <para>
    /// <b>Measured at 0.0018 ± 0.0018 of steps, and the senses walk now disagrees
    /// on none at all.</b> Voting was built because the walk disagreed with itself
    /// — 0.8833 on senses — and that turned out to be fork 22: questions were
    /// being read before their walk had finished. **So voting buys nothing in one
    /// process.** It is kept because a real network loses reports and redundancy
    /// is the honest answer to C2, but it is no longer evidence of anything.
    /// </para>
    /// </remarks>
    public required int Disagreed { get; init; }
}

/// <summary>
/// What decides the move.
/// </summary>
/// <remarks>
/// <b>Controls that change ONE thing.</b> <see cref="SnakeRun.PlayAsync"/>'s
/// <c>cut</c> flag changes two — it stops the action joining the occasion,
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
    private readonly Fabric _fabric;
    private readonly InputMachine<SnakeFrame> _eye;
    private readonly OutputMachine _hand;
    private readonly Snake _snake;
    private readonly Random _fallback;
    private readonly SnakeSense _sense;
    private readonly WalkSettings _dials;

    /// <inheritdoc cref="Graph.Cluster.Temporal"/>
    public int TemporalCells => _fabric.Temporal;
    private readonly Foresight _foresight = new();
    private readonly Foresight _novelty = new();

    /// <summary>Fork 18's metric. <inheritdoc cref="Consequence"/></summary>
    private readonly Consequence _consequence = new();

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

    /// <summary>What was present last step, so this step's onsets can be found.</summary>
    private HashSet<Code> _before = [];

    /// <inheritdoc cref="Chains"/>
    private readonly Chains _chains = new();

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

        _dials = dials;
        _sense = new SnakeSense();
        _snake = new Snake(world, seed);
        _fallback = new Random(seed);
        _guessing = new Random(~seed);
        _fabric = new Fabric(dials, seed, clusters, replicas);


        _eye = new InputMachine<SnakeFrame>(
            new MachineAddress("eye"),
            _sense,
            new LocalRendezvous(_fabric.Local),
            _fabric.Bus,
            _fabric.Ring,
            dials);

        _fabric.Subscribe(_eye);
        _hand = new OutputMachine(new MachineAddress("hand"), SnakeSense.Turns);
    }

    /// <summary>
    /// Plays, and reports what the system actually did while playing.
    /// </summary>
    /// <remarks>
    /// <b>Prefer this to <see cref="PlayAsync"/> for anything being measured.</b>
    /// A <see cref="RunReport"/> carries the plumbing alongside the result, and
    /// says outright when a quantity is out of the range it would be in if
    /// everything were wired.
    /// </remarks>
    public async Task<RunReport> ReportAsync(
        int steps,
        bool cut = false,
        Policy policy = Policy.Chain,
        int votes = 1,
        CancellationToken ct = default)
    {
        var result = await PlayAsync(steps, cut, policy, votes, ct).ConfigureAwait(false);

        return new RunReport
        {
            Result = result,
            Unsettled = result.Unsettled,
            Plumbing = _fabric.Facts(_chains, result.Unbalanced),
        };
    }

    /// <summary>Plays until the run ends or the step budget runs out.</summary>
    /// <param name="steps">The budget.</param>
    /// <param name="cut">
    /// <b>The control arm: cuts the one wire that makes an action reachable.</b>
    /// With this set, what the snake did never joins the occasion, so an action
    /// code gains no edges and no walk can arrive at one.
    /// </param>
    /// <param name="policy">What decides the move. <see cref="Policy"/>.</param>
    /// <param name="votes">
    /// How many concurrent thoughts decide one move.
    /// </param>
    /// <param name="ct">Cancellation.</param>
    public async Task<RunResult> PlayAsync(
        int steps,
        bool cut = false,
        Policy policy = Policy.Chain,
        int votes = 1,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(steps);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(votes);

        Code? did = null;
        int taken = 0, byChain = 0, reachedNothing = 0, silent = 0, ate = 0, echoed = 0;
        var disagreed = 0;
        long halted = 0;
        var unbalanced = 0;
        var unsettled = 0;

        IReadOnlyList<Code> foreseen = [];
        IReadOnlyList<Code> blind = [];

        // The same prediction with a DIFFERENT action in it. Fork 18's control.
        IReadOnlyList<Code> otherwise = [];
        IReadOnlyList<Code> again = [];

        for (; taken < steps && _snake.Alive; taken++)
        {
            var before = _snake.Energy;
            var frame = new SnakeFrame { View = _snake.View(), Did = did };
            var present = _sense.Codify(frame);

            // PREQUENTIAL: last step's guess is settled against this step's
            // world BEFORE anything is counted, so the graph never sees the
            // answer before being asked.
            _foresight.Settle(foreseen, present, blind);

            // Scored against what STARTED, which is the part persistence does
            // not answer for free.
            _novelty.Settle(foreseen, [.. present.Where(c => !_before.Contains(c))], blind);


            // FORK 18, AND IT IS SCORED AGAINST EVERYTHING PRESENT ON PURPOSE.
            // Whatever is predictable without knowing the action is equally
            // predictable in both arms, so it cancels in the gap -- there is no
            // need to strip persistence out by hand the way `_novelty` does.
            _consequence.Settle(foreseen, otherwise, again, blind, present);

            _before = [.. present];
            Remember(present);

            // CHOOSING AN ACTION WALKS BACKWARDS FROM A CONSEQUENCE TO ITS CAUSE.
            // The occasion says the action came first, so the forward cell runs
            // action -> view; broadcasting the view and hoping to arrive at an
            // action has no forward edge to use. Measured: with only the forward
            // cell written, the chain reached an action ZERO times on every seed
            // and the body moved entirely at random. See Kind.Before.
            var thought = await _eye
                .ObserveAsync(frame, taken, Question.Preceding(), ct: ct)
                .ConfigureAwait(false);

            // WAIT ON THE THOUGHT'S OWN ACCOUNTING, NOT ON THE BUS. This was
            // `QuietAsync` alone, which is fork 22 exactly: in-flight reaches zero
            // in the gap between a cluster handling a message and dispatching what
            // that message produced, so a thought read there says "reached
            // nothing" where the truth is "not finished" -- and here that becomes
            // a move made at random and counted as the chain's. Every other world
            // settles; snake is the oldest and never got the fix.
            if (thought is null) await _fabric.QuietAsync(ct).ConfigureAwait(false);
            else if (!await _fabric.SettleAsync(thought, ct).ConfigureAwait(false)) unsettled++;

            Code? chosen = null;

            if (thought is not null)
            {
                var voted = await VoteAsync(thought, votes, ct).ConfigureAwait(false);
                chosen = voted.Chosen;
                halted += voted.Halted;
                unbalanced += voted.Unbalanced;
                unsettled += voted.Unsettled;
                if (voted.Disagreed) disagreed++;
            }

            if (thought is null) silent++;
            else
            {
                halted += thought.Halted;
                if (!thought.Balanced()) unbalanced++;
                if (chosen is null) reachedNothing++;

                _chains.Fold(thought.Best(int.MaxValue));
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
            bool closed;
            (foreseen, blind, closed) = await ForeseeAsync(present, doing, ct).ConfigureAwait(false);
            if (!closed) unsettled++;

            // THE CONTROL, AND IT CHANGES EXACTLY ONE THING. Same graph, same
            // budget, same walk, same narrowing, same number of codes named --
            // only the action inside the question differs. If this scores as
            // well as the real one, the graph is predicting the next frame
            // regardless of what the body does, which is the thing that looks
            // like understanding and is not.
            (otherwise, _, closed) =
                await ForeseeAsync(present, Instead(doing), ct).ConfigureAwait(false);
            if (!closed) unsettled++;

            // THE THIRD ARM, AND IT IS WHAT MAKES THE OTHER TWO READABLE. The same
            // question a second time, action and all. Delivery is concurrent, so
            // two identical broadcasts already land in different places -- and
            // without measuring that floor there is no way to tell a difference
            // caused by the action from a difference caused by the jitter. Three
            // earlier attempts to kill the surviving mutation failed for exactly
            // this reason. See Consequence.Echoed.
            (again, _, closed) = await ForeseeAsync(present, doing, ct).ConfigureAwait(false);
            if (!closed) unsettled++;

            Perform(doing);
            did = cut ? null : doing;

            if (_snake.Alive && _snake.Energy > before) ate++;
        }

        _fabric.Failures();

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
            Nodes = _fabric.Nodes,
            Deaths = _eye.DeathsSeen,
            Halted = halted,
            EchoedLast = echoed,
            Disagreed = disagreed,
            Unbalanced = unbalanced,
            Unsettled = unsettled,
            Messages = _fabric.Bus.Messages,
            Foresight = _foresight,
            Novelty = _novelty,
            Consequence = _consequence,
        };
    }

    /// <summary>
    /// Decides the move, optionally by asking the same question several times at
    /// once and taking the majority.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The mechanism the other two worlds already had, and snake did not.</b>
    /// Delivery is concurrent, so an identical question does not always get an
    /// identical answer — measured at 0.8833 agreement on the senses world, where
    /// voting recovered 0.9688 to 0.9974. <b>Every snake number taken at one vote
    /// is therefore a lower bound taken at the noisy end.</b>
    /// </para>
    /// <para>
    /// <b>The extra thoughts re-ask from the SAME origins and never re-learn.</b>
    /// <see cref="Thought.Started"/> is the origin set, so the graph is written
    /// once by <c>ObserveAsync</c> and read <paramref name="votes"/> times —
    /// otherwise voting would be teaching, and the arm would beat its control for
    /// a reason that has nothing to do with agreement.
    /// </para>
    /// <para>
    /// <b>One vote is the default</b>, so it is off unless a caller asks and the
    /// existing measurements are untouched by its arrival.
    /// </para>
    /// </remarks>
    private async Task<(Code? Chosen, long Halted, int Unbalanced, int Unsettled, bool Disagreed)>
        VoteAsync(Thought first, int votes, CancellationToken ct)
    {
        if (votes <= 1) return (_hand.Choose(first), 0, 0, 0, false);

        var asking = new Task<Thought>[votes - 1];
        for (var i = 0; i < asking.Length; i++)
            asking[i] = _eye.ThinkAsync(first.Started, _dials.Stamina, null, ct);

        var others = await Task.WhenAll(asking).ConfigureAwait(false);

        var opinions = new List<Code?>(votes);
        long halted = 0;
        var unbalanced = 0;
        var unsettled = 0;

        foreach (var one in others)
        {
            // EACH VOTE IS A WALK AND EACH IS READ, so each is settled. A vote
            // read early is a random opinion dressed as agreement, which would
            // move `Disagreed` -- the one number this arm exists to produce.
            if (!await _fabric.SettleAsync(one, ct).ConfigureAwait(false)) unsettled++;

            opinions.Add(_hand.Choose(one));
            halted += one.Halted;
            if (!one.Balanced()) unbalanced++;
            _eye.Forget(one.Id);
        }

        opinions.Add(_hand.Choose(first));

        var vote = Majority.Of(opinions);
        return (vote.Chosen, halted, unbalanced, unsettled, vote.Disagreed);
    }

    /// <summary>
    /// A different action from the one about to be taken.
    /// </summary>
    /// <remarks>
    /// <b>Deterministic, and it draws on no generator.</b> A counterfactual
    /// chosen at random would either need its own stream or perturb the blind
    /// control's — and a control sharing a generator with the arm it controls
    /// has already caused a wrong result here once.
    /// <para>
    /// <c>IndexOf</c> returns -1 for something absent, which has also bitten
    /// this project, so the miss is handled rather than wrapped.
    /// </para>
    /// </remarks>
    private static Code Instead(Code doing)
    {
        var turns = SnakeSense.Turns;
        var at = -1;

        for (var i = 0; i < turns.Count; i++)
            if (turns[i] == doing) { at = i; break; }

        return at < 0 ? turns[0] : turns[(at + 1) % turns.Count];
    }

    /// <summary>
    /// Asks the graph what it expects to see next, given what is about to be
    /// done, and draws a blind guess of the same size to score it against.
    /// </summary>
    private async Task<(IReadOnlyList<Code> Foreseen, IReadOnlyList<Code> Blind, bool Settled)>
        ForeseeAsync(IReadOnlyCollection<Code> present, Code doing, CancellationToken ct)
    {
        // NOTHING WAS ASKED, so nothing failed to settle. A step with no codes
        // must not read as an unfinished walk.
        if (present.Count == 0) return ([], [], true);

        // A SHALLOWER BUDGET FOR THE PREDICTION, because the two questions
        // want opposite depths -- fork 20.
        //
        // AND FORK 18: WHAT FOLLOWS, NOT WHAT ACCOMPANIES. `What will the world
        // look like if I do X` is a question about consequence, and until the row
        // could hold a temporal cell there was no way to ask it -- the action and
        // the view that followed it were one flat occasion, so a walk from the
        // action reached whatever merely co-occurred with it. With kinds on, the
        // front end says the action came first and this asks only for what came
        // after.
        var thought = await _eye.ThinkAsync(
            [.. present, doing],
            _dials.Foresight,
            Question.Following(),
            ct).ConfigureAwait(false);

        // SETTLED, NOT MERELY QUIET. A prediction read mid-flight names fewer
        // codes than the walk actually reached, which lowers precision on a walk
        // that was working -- and fork 18's whole result is a GAP between two
        // predictions, so an unfinished one on either side moves it either way.
        var settled = await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);

        var wanted = _dials.Names;
        var foreseen = thought.BestOf(SnakeQuantizer.Vision, wanted)
            .Select(a => a.Endpoint).ToArray();

        _eye.Forget(thought.Id);

        var blind = _alphabet.Count == 0
            ? []
            : Enumerable.Range(0, foreseen.Length)
                .Select(_ => _alphabet[_guessing.Next(_alphabet.Count)])
                .ToArray();

        return (foreseen, blind, settled);
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

    public void Dispose() => _fabric.Dispose();
}
