using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using OpenPlexus.Thinking;

namespace OpenPlexus.Worlds;

/// <summary>
/// What the rhythm world measured. <b>Counts, not claims.</b>
/// </summary>
public sealed record RhythmResult : Questioned
{
    /// <inheritdoc cref="Learning.Window"/>
    public required int Span { get; init; }

    /// <inheritdoc cref="Rhythm.Ceiling"/>
    public required double Ceiling { get; init; }

    /// <inheritdoc cref="Rhythm.Marginal"/>
    public required double Marginal { get; init; }

    /// <summary>Moments the cycle called for and got.</summary>
    public required int Kept { get; init; }

    /// <summary>Of those, how many were predicted right.</summary>
    public required int Foreseen { get; init; }

    /// <summary>Moments that broke the rule.</summary>
    public required int Broke { get; init; }

    /// <summary>
    /// Of the violations, how many were nonetheless predicted right.
    /// </summary>
    /// <remarks>
    /// <b>SHOULD BE AT CHANCE AND IS REPORTED SO IT CAN BE CHECKED.</b> A
    /// violation is unpredictable by construction, so anything above a blind draw
    /// here means the world is leaking — the "random" symbol is not independent of
    /// what came before, and every other number would be built on that.
    /// </remarks>
    public required int Caught { get; init; }

    /// <summary>The score on the moments that were predictable at all.</summary>
    public double Expected => Kept == 0 ? 0.0 : Foreseen / (double)Kept;

    /// <inheritdoc cref="Caught"/>
    public double Surprised => Broke == 0 ? 0.0 : Caught / (double)Broke;

    /// <summary>
    /// The share of onsets the system already expected, or zero when step 2 is
    /// off. <b>The internal error signal.</b>
    /// </summary>
    public required double Expecting { get; init; }

    /// <summary>Moments where nothing was broadcast because nothing was surprising.</summary>
    public required int Unspoken { get; init; }

    /// <summary>
    /// The share of what was expected that did not happen, or zero when step 2 is
    /// off. <b>ABSENCE — the negative half, and the guard on the half above.</b>
    /// </summary>
    /// <remarks>
    /// <b>THIS WORLD BETS EXACTLY ONE SYMBOL A MOMENT, so here it is one minus the
    /// precision of the bet</b> and it should track <see cref="Expected"/>
    /// downward. It earns its keep on any front end that can expect more than one
    /// thing at once, where a predictor naming the whole alphabet would otherwise
    /// read as a perfectly modelled world — see <see cref="Learning.Surprise"/>.
    /// </remarks>
    public required double Overreached { get; init; }

    /// <summary>Bets that were settled against the moment AFTER the one they were for.</summary>
    public required int Late { get; init; }

    /// <summary>
    /// Of those, how many named the symbol two moments ahead instead of one.
    /// </summary>
    /// <remarks>
    /// <b>THE DIAGNOSTIC, AND IT IS THE WHOLE FINDING OF THIS WORLD.</b> A graph
    /// that predicts the NEXT symbol at chance while predicting the one after it
    /// far above chance has learnt real temporal structure and learnt it at the
    /// wrong offset — which is a different fault from having learnt nothing, and
    /// the two are indistinguishable in an accuracy alone.
    /// </remarks>
    public required int Skipped { get; init; }

    /// <inheritdoc cref="Skipped"/>
    public double TwoAhead => Late == 0 ? 0.0 : Skipped / (double)Late;

    /// <summary>How many steps ahead the rollout reached. <b>One is no rollout.</b></summary>
    public required int Depth { get; init; }

    /// <summary>
    /// What share of the rolled predictions were right, <b>settled against the
    /// moment they were actually about.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE CEILING DOES NOT MOVE WITH DEPTH HERE, WHICH IS WHY THIS WORLD IS THE
    /// RIGHT ONE FOR IT.</b> A cycle is exactly as predictable four steps out as one
    /// — the symbol is determined either way — so <see cref="Ceiling"/> is the same
    /// number at every depth and ANY decay is compounding error and nothing else.
    /// On a world whose far future were genuinely harder, the two would be
    /// impossible to tell apart.
    /// </para>
    /// <para>
    /// <b>At depth one this is the ordinary score arrived at by another road</b>,
    /// which is the check that the rollout has not disturbed the reflex it extends.
    /// </para>
    /// </remarks>
    public required double Rolled { get; init; }

    /// <summary>How much of what was achievable was achieved, in 0..1.</summary>
    /// <remarks>
    /// <b>Against the ceiling rather than against one.</b> A perfect model is
    /// wrong on every violation, so scoring against a perfect run makes the best
    /// possible system look broken in proportion to how noisy the world is.
    /// </remarks>
    public double OfCeiling => Ceiling <= 0.0 ? 0.0 : Accuracy / Ceiling;

    /// <inheritdoc/>
    protected override string Shown => "moments";

    /// <inheritdoc/>
    /// <remarks><b>Two — this world's task is one hop, from now to next.</b></remarks>
    protected override int Composes => 2;

    /// <inheritdoc/>
    protected override string Stalled => "no route walked at all";

    /// <inheritdoc/>
    protected override void Beyond(List<string> wrong)
    {
        ArgumentNullException.ThrowIfNull(wrong);

        if (Kept == 0) wrong.Add("the cycle never once played through");

        // THE WORLD'S OWN INTEGRITY CHECK. A violation is a draw from everything
        // the cycle did not call for, so predicting them above chance means the
        // stream is not what it says it is.
        // ABSENCE, WIRED. `Overreach` existed and nothing read it, which is the
        // named trap about a dial connected to nothing. This is the failure it
        // was built to catch: a predictor that names everything foresees every
        // onset, reads a rate near one, and silences the machine on a lie --
        // and the positive half cannot tell that from a solved world.
        if (Moments > 50 && Expecting > 0.9 && Overreached > 0.5)
            wrong.Add($"the predictor foresaw {Expecting:F2} of onsets while "
                + $"{Overreached:F2} of what it named never happened — it is "
                + "naming everything, and the silence it buys is not earned");

        if (Broke > 50 && Surprised > Chance * 3)
            wrong.Add($"violations were predicted at {Surprised:F2}, far above chance "
                + $"{Chance:F2} — the world is leaking what it is about to do");
    }

    public override string ToString() =>
        $"span={Span} moments={Moments} asked={Asked} right={Right} silent={Silent} | " +
        $"accuracy={Accuracy:F4} ofCeiling={OfCeiling:F4} " +
        $"expected={Expected:F4} surprised={Surprised:F4} twoAhead={TwoAhead:F4} | " +
        $"expecting={Expecting:F4} overreached={Overreached:F4} unspoken={Unspoken} | " +
        $"ceiling={Ceiling:F4} marginal={Marginal:F4} chance={Chance:F4} | " +
        $"reflect={(Reflecting ? "on" : "off")} wrote={Reflected} | " +
        $"nodes={Nodes} edges={Edges} widest={Widest} spread=[{string.Join(",", Spread)}] | " +
        $"chains={{{Plumbing.Lengths}}} deepest={Deepest} | " +
        $"msgs={Messages} halted={Halted} unbalanced={Unbalanced} unsettled={Unsettled}{Wrong}";
}

/// <summary>
/// The rhythm world, wired to the graph.
/// </summary>
/// <remarks>
/// <b>Predicts rather than answers, and is scored prequentially.</b> At every
/// moment the graph is asked what comes next, the answer is held, the next moment
/// arrives and settles the bet, and learning carries on throughout. Nothing stops
/// and nothing is tested afterwards, which is what C4 requires.
/// </remarks>
public sealed class RhythmRun : IDisposable
{
    private readonly Fabric _fabric;
    private readonly InputMachine<Code> _ear;
    private readonly Rhythm _world;
    private readonly WalkSettings _dials;
    private readonly int _span;

    /// <inheritdoc cref="Learning.Surprise"/>
    private readonly Surprise? _surprise;

    /// <summary>How many steps ahead the rollout reaches. <b>One is no rollout.</b></summary>
    private readonly int _depth;

    /// <summary>
    /// What the prediction asks for. <b>Null is every measurement taken before
    /// recency existed</b>, and is what a stationary stream wants — nothing here
    /// goes stale unless the world turns.
    /// </summary>
    private readonly Question? _asking;

    /// <param name="world">The shape of the stream.</param>
    /// <param name="dials">The walk.</param>
    /// <param name="seed">The world's generator and the ring's, so a run reproduces.</param>
    /// <param name="span">
    /// How many moments a departed symbol is carried for. <b>Zero leaves this
    /// world with no edges at all</b>, because nothing here is ever simultaneous
    /// with anything — see <see cref="Rhythm"/>.
    /// </param>
    /// <param name="surprising">
    /// Whether step 2 is on. <b>Off is every measurement taken before
    /// <see cref="Learning.Surprise"/> existed</b>: every onset is broadcast,
    /// nothing is suppressed, and both internal signals read zero.
    /// </param>
    /// <param name="carried">
    /// What a carried pair counts for against a simultaneous one — <b>the window's
    /// standing revival condition.</b> One is every measurement taken before it,
    /// and this world is where the arm has most to lose: nothing here is ever
    /// simultaneous, so EVERY edge it holds is a carried one.
    /// </param>
    /// <param name="recent">
    /// Whether the prediction PREFERS WHAT IS STILL TRUE — see
    /// <see cref="Question.Recent"/>. <b>Off is every measurement taken before it
    /// existed</b>, and it has nothing to say on a stream that never turns.
    /// </param>
    /// <param name="gated">
    /// Whether the WRITE path is gated by surprise too — <b>step 2's second half.</b>
    /// Off is every measurement taken before it existed.
    /// </param>
    /// <param name="depth">
    /// How many steps ahead to roll — <b>step 11, and one is the reflex this
    /// project already had.</b> Each extra step feeds the last prediction back in
    /// as a synthetic moment and asks again.
    /// </param>
    /// <param name="clusters">How many clusters the codes are spread over.</param>
    /// <param name="replicas">Ring replicas per cluster.</param>
    public RhythmRun(
        RhythmSettings world,
        WalkSettings dials,
        int seed,
        int span = 1,
        bool surprising = false,
        double carried = 1.0,
        bool recent = false,
        bool gated = false,
        int depth = 1,
        int clusters = 8,
        int replicas = 256)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        _world = new Rhythm(world, seed);
        _dials = dials;
        _span = span;
        _surprise = surprising ? new Surprise() : null;
        _depth = depth < 1 ? 1 : depth;
        _asking = recent ? new Question { Recent = true } : null;
        _fabric = new Fabric(dials, seed, clusters, replicas);

        _ear = new InputMachine<Code>(
            new MachineAddress("ear"), new Hearing(),
            new LocalRendezvous(_fabric.Local, carried: carried),
            _fabric.Bus, _fabric.Ring, dials, span, _surprise, gated: gated);

        _fabric.Subscribe(_ear);
    }

    /// <summary>The world this run is listening to.</summary>
    public Rhythm World => _world;

    /// <summary>One symbol is already a code; there is nothing to quantise.</summary>
    private sealed class Hearing : IQuantizer<Code>
    {
        public byte Modality => Rhythm.Beat;

        public IReadOnlyCollection<Code> Codify(Code observation) => [observation];
    }

    /// <summary>
    /// Listens, and at every moment bets on the next one.
    /// </summary>
    /// <param name="moments">How long to listen for.</param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <b>The bet is placed BEFORE the next symbol is drawn</b>, which is the
    /// whole discipline of a prequential score. Drawing first and asking
    /// afterwards would let the world's own generator advance inside the question,
    /// and the number would be meaningless in a way nothing downstream could see.
    /// </remarks>
    public async Task<RhythmResult> RunAsync(int moments, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(moments);

        int asked = 0, right = 0, silent = 0, unbalanced = 0, unsettled = 0;
        int kept = 0, foreseen = 0, broke = 0, caught = 0;
        long halted = 0;

        var reflected = 0;
        var chains = new Chains();

        int late = 0, skipped = 0;
        Code? bet = null, older = null;

        // STEP 11'S ROLLOUT. A prediction made now is settled `_depth` moments from
        // now, so the bets in flight are held in order and the oldest falls due
        // first. At depth one this holds exactly one bet and settles it against the
        // very next moment, which is what the score above already does -- and the
        // two agreeing is the check that the rollout has not quietly changed the
        // one-step case.
        var rolling = new Queue<Code?>();
        int far = 0, farRight = 0;

        for (var moment = 0; moment < moments; moment++)
        {
            var (shown, violated) = _world.Next();

            // SETTLE THE PREVIOUS BET FIRST, against what has just arrived.
            if (bet is not null)
            {
                asked++;
                if (violated) broke++; else kept++;

                if (bet.Value == shown)
                {
                    right++;
                    if (violated) caught++; else foreseen++;
                }
            }

            // AND SETTLE THE ONE BEFORE IT AGAINST THE SAME MOMENT, which is the
            // diagnostic rather than the score -- see Skipped. Only the moments
            // the cycle called for, or a violation would count against an offset
            // that was never predictable at either distance.
            if (older is not null && !violated)
            {
                late++;
                if (older.Value == shown) skipped++;
            }

            older = bet;

            // SETTLE THE ROLLED BET THAT FALLS DUE NOW, before this moment adds
            // another to the queue.
            if (rolling.Count >= _depth)
            {
                var due = rolling.Dequeue();

                if (due is not null)
                {
                    far++;
                    if (due.Value == shown) farRight++;
                }
            }

            var observed = await _ear.ObserveAsync(shown, moment, ct: ct).ConfigureAwait(false);
            await _fabric.QuietAsync(ct).ConfigureAwait(false);

            if (observed is not null)
                reflected += await _ear.ReflectAsync(observed, moment, ct).ConfigureAwait(false);

            // AND NOW BET ON THE NEXT ONE, from what is being heard right now.
            var (guess, stopped, balanced, settled, reached) =
                await GuessAsync(shown, ct).ConfigureAwait(false);

            halted += stopped;
            if (!balanced) unbalanced++;
            if (!settled) unsettled++;
            if (guess is null && bet is not null) silent++;

            chains.Fold(reached);
            bet = guess;

            // AND ROLL IT FORWARD, feeding each prediction back in as though it had
            // been heard. NOTHING IS OBSERVED HERE: a synthetic moment must never
            // reach the rendezvous, or the graph learns from its own guesses and
            // every count downstream is measuring the model's imagination. Thinking
            // without observing is exactly what `ThinkAsync` already is.
            var reach = guess;

            for (var step = 1; step < _depth && reach is not null; step++)
                reach = (await GuessAsync(reach.Value, ct).ConfigureAwait(false)).Next;

            rolling.Enqueue(reach);

            // WHAT THE SYSTEM NOW EXPECTS, handed to the input path so that an
            // onset matching it is never broadcast. This is the same bet the
            // score is settled against, so the traffic saved and the prediction
            // measured are the same prediction -- and a run cannot quietly
            // silence itself on an expectation nobody scored.
            _surprise?.Expect(bet is { } one ? [one] : []);
        }

        _fabric.Failures();

        return new RhythmResult
        {
            Span = _span,
            Depth = _depth,
            Rolled = far == 0 ? 0.0 : farRight / (double)far,
            Moments = moments,
            Asked = asked,
            Right = right,
            Silent = silent,
            Kept = kept,
            Foreseen = foreseen,
            Broke = broke,
            Caught = caught,
            Late = late,
            Skipped = skipped,
            Expecting = _surprise?.Rate ?? 0.0,
            Unspoken = _surprise?.Silent ?? 0,
            Overreached = _surprise?.Overreach ?? 0.0,
            Ceiling = _world.Ceiling,
            Marginal = _world.Marginal,
            Chance = _world.Chance,
            Reflections = Reflections.Of(_dials, reflected),
            Plumbing = _fabric.Facts(chains, unbalanced),
            Halted = halted,
            Unsettled = unsettled,
        };
    }

    /// <summary>One bet, with the plumbing left attached.</summary>
    private readonly record struct Guess(
        Code? Next,
        int Halted,
        bool Balanced,
        bool Settled,
        IReadOnlyList<Arrival> Reached);

    /// <summary>
    /// Broadcasts what is being heard and reads back what usually follows it.
    /// </summary>
    /// <remarks>
    /// <b>The symbol itself is struck out of the candidates.</b> A route that
    /// walks nowhere ends where it began, so without this the graph would answer
    /// every question with the thing it was just told — and this stream never
    /// repeats a symbol twice running, so that answer is always wrong and would
    /// read as the walk having no model at all.
    /// </remarks>
    private async Task<Guess> GuessAsync(Code heard, CancellationToken ct)
    {
        var thought = await _ear
            .ThinkAsync([heard], _dials.Foresight ?? _dials.Stamina, _asking, ct)
            .ConfigureAwait(false);

        var settled = await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);

        var reached = thought
            .BestOf(Rhythm.Beat, 2)
            .Where(arrival => arrival.Endpoint != heard)
            .ToList();

        var guess = new Guess(
            reached.Count == 0 ? null : reached[0].Endpoint,
            thought.Halted,
            thought.Balanced(),
            settled,
            thought.Best(int.MaxValue));

        _ear.Forget(thought.Id);
        return guess;
    }

    public void Dispose() => _fabric.Dispose();
}
