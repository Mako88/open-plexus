using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using OpenPlexus.Thinking;

namespace OpenPlexus.Worlds;

/// <summary>How the body decides what to attend to.</summary>
/// <remarks>
/// <b>Controls that change ONE thing.</b> The body, the drains and the score are
/// identical under every arm; only the choice moves.
/// </remarks>
public enum Attending
{
    /// <summary>Nothing at all. <b>The control that survival could not refute.</b></summary>
    Idle,

    /// <summary>A variable at random, without looking at the body.</summary>
    Blind,

    /// <summary>Whatever the graph ranks first. <b>The arm.</b></summary>
    Chain,

    /// <summary>Whichever variable is lowest. <b>The ceiling.</b></summary>
    Lowest,

    /// <summary>
    /// The credit in ITS OWN CELL, and the walk asks what is WORTH doing rather
    /// than what was done — <b>step 4's second attempt, and the first that is
    /// contrastive.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>WHY THE FIRST THREE FAILED, IN ONE SENTENCE:</b> they all wrote a HEAVIER
    /// number into the same cell, and that cell already means <i>this was done
    /// here</i> — so reinforcing it deepens exactly the groove it was meant to fix.
    /// </para>
    /// <para>
    /// <b>This writes a SECOND cell instead.</b> The occasion is joined as usual,
    /// and one step later — when the outcome is known — the same occasion is joined
    /// again under <see cref="Graph.Kind.Helped"/>, but only if the most-at-risk
    /// variable improved. The choice then walks that cell alone, so it ranks by the
    /// share of times an action helped rather than by how often it was taken.
    /// Nothing is punished; an act that did not help simply gets no second write.
    /// </para>
    /// <para>
    /// <b>THE BOOTSTRAP MATTERS MORE HERE THAN ANYWHERE.</b> Until something has
    /// helped, the credit cell is empty and the walk has nothing to say — so this
    /// arm is nearly all coin toss early on, and its silence is the thing to read
    /// beside its score.
    /// </para>
    /// <para>
    /// <b>AND THE CONTROL THAT ATTRIBUTED THIS TO THE CONTRAST IS GONE, so the
    /// attribution is now a RECORD rather than a re-runnable comparison.</b>
    /// <c>Marked</c> wrote the same second cell unconditionally — same relation,
    /// same one-step staleness, differing by the condition alone — and peaked at
    /// 0.3167 against a blind bar of 0.3668, below plain association's own peak
    /// region and less than half of this arm's 0.7347. It was collapsed under the
    /// delete-the-loser rule; the revival condition is in the plan's table. What
    /// is asserted below is only the surviving half: plain association peaks below
    /// the bar too.
    /// </para>
    /// </remarks>
    Credited,

    /// <summary>
    /// <see cref="Credited"/> with the NEGATIVE half as well — <b>a contingency
    /// rather than a one-sided count, and the first inhibition in this design.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Credited"/> can only ever say what helped.</b> An act that was
    /// taken and made things worse gets no second write, so it is merely
    /// un-recommended — it still sits in the ordinary cell, and nothing anywhere
    /// says <i>not that one</i>. That is one-sided evidence, and
    /// <c>helped / seen</c> is a hit rate rather than a contingency.
    /// </para>
    /// <para>
    /// <b>This writes <see cref="Graph.Kind.Hindered"/> when the most-at-risk
    /// variable got worse</b>, and the second join raises the ACT's own marginal —
    /// so an act that often hurts sinks through the denominator every credit weight
    /// already divides by. Both counts still only rise, so convergence is untouched.
    /// <b>Nothing reads the negative cell; writing it is the entire mechanism</b>,
    /// which was measured rather than intended — see <see cref="Graph.Kind.Hindered"/>.
    /// </para>
    /// </remarks>
    Contested,

}

/// <summary>
/// What the homeostat measured. <b>Counts, not claims.</b>
/// </summary>
public sealed record HomeostatResult : Measurement
{
    /// <inheritdoc cref="Attending"/>
    public required Attending Choosing { get; init; }

    /// <summary>Steps taken.</summary>
    public required int Steps { get; init; }

    /// <summary>Of those, how many left every variable inside its bounds.</summary>
    public required int Held { get; init; }

    /// <summary>How long doing nothing would have lasted.</summary>
    public required int Idling { get; init; }

    /// <summary>How many times each need was attended to. <b>The diagnostic.</b></summary>
    public required IReadOnlyList<int> Attended { get; init; }

    /// <summary>
    /// How many DISTINCT states the body was ever in, as the graph sees them.
    /// <b>A policy cannot be conditional on a state the graph cannot tell
    /// apart.</b>
    /// </summary>
    public required int States { get; init; }

    /// <summary>
    /// Steps where the walk proposed no action, <b>so the bootstrap acted at
    /// random instead.</b>
    /// </summary>
    /// <remarks>
    /// <b>NOT "the body did nothing" — it acts, and the act is a coin toss.</b>
    /// So this is the share of an arm that is measuring its own fallback rather
    /// than the graph, and an arm whose silence is high is a random policy with
    /// extra steps. See the note on the bootstrap in <see cref="HomeostatRun"/>.
    /// </remarks>
    public required int Silent { get; init; }

    /// <summary>
    /// Steps where the walk offered MORE THAN ONE action — <b>the only steps on
    /// which a ranking can possibly matter.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>SILENCE SAYS THE WALK PROPOSED NOTHING; THIS SAYS IT PROPOSED NO
    /// CHOICE</b>, and the two are entirely different failures. An arm can be
    /// eloquent on nearly every step, score well, and still have had exactly one
    /// candidate every time — in which case however it ranks them is arithmetic
    /// performed on a list of length one.
    /// </para>
    /// <para>
    /// <b>IT IS A CHECK THAT HAS TO BE ARMED, WHICH IS WHY IT IS REPORTED RATHER
    /// THAN ASSERTED HERE.</b> A change to how partners are RANKED cannot be
    /// measured on a run where this is near nought: the arm will reproduce its
    /// control exactly, and an exact reproduction reads as "no effect" when what
    /// happened is "no opportunity". That is the trap this project names, and it
    /// caught a real arm.
    /// </para>
    /// </remarks>
    public required int Choices { get; init; }

    /// <summary>
    /// The share of transitions that improved the most-at-risk variable —
    /// <b>a signal the MACHINE computes about itself, and the first one exposed
    /// where it can be audited.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>IT WAS COMPUTED EVERY STEP AND READ BY NOTHING</b>, which the dead-code
    /// budget caught — and it had been described as one of the three internal
    /// signals this project has, making the honest count of signals the SYSTEM can
    /// act on zero.
    /// </para>
    /// <para>
    /// <b>EXPOSED RATHER THAN WIRED TO A CONTROLLER, AND THAT ORDER IS THE WHOLE
    /// LESSON OF FORK 23.</b> Three controllers have been built here and all three
    /// failed for want of a signal that DISCRIMINATES: `Hunger` was inverted,
    /// `Thwarted` had the right shape and swung too little. <b>So a candidate
    /// signal gets audited before anything is driven by it</b> — see
    /// <see cref="Worlds.Homeostat"/>'s tests.
    /// </para>
    /// </remarks>
    public required double Improving { get; init; }

    /// <summary>The share of the run spent viable. <b>The score.</b></summary>
    /// <remarks>
    /// <b>Time viable, not time until failure.</b> Homeostasis has no episode
    /// boundary, so a body that falls out of bounds carries on and may come back —
    /// which is the whole difference from scoring by survival.
    /// </remarks>
    public double Viable => Steps == 0 ? 0.0 : Held / (double)Steps;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Nothing at all for the arms that never consult the graph.</b> Idling,
    /// attending at random and attending to whatever is lowest are all decided
    /// without a walk, so a depth requirement would complain about arms that are
    /// working exactly as intended.
    /// </remarks>
    protected override int Composes => Choosing == Attending.Chain ? 2 : 0;

    /// <inheritdoc/>
    protected override string Stalled => "no route walked at all";

    /// <inheritdoc/>
    protected override void Peculiar(List<string> wrong)
    {
        ArgumentNullException.ThrowIfNull(wrong);

        if (Steps == 0) wrong.Add("the run took no steps");

        // THE WORLD'S OWN INTEGRITY CHECK, AND IT IS THE ONE THAT MATTERS. This
        // world exists because survival was gameable by circling. If doing nothing
        // scored well here, it would be gameable the same way and every other
        // number would be worthless.
        if (Choosing == Attending.Idle && Held > Idling + 1)
            wrong.Add($"idling held for {Held} steps where the arithmetic says "
                + $"{Idling} — standing still is paying, so this world is gameable");
    }

    public override string ToString() =>
        $"choosing={Choosing} steps={Steps} held={Held} silent={Silent} " +
        $"choices={Choices} | " +
        $"viable={Viable:F4} idling={Idling} | " +
        $"nodes={Nodes} edges={Edges} widest={Widest} spread=[{string.Join(",", Spread)}] | " +
        $"chains={{{Plumbing.Lengths}}} deepest={Deepest} | " +
        $"msgs={Messages} unbalanced={Unbalanced} unsettled={Unsettled}{Wrong}";
}

/// <summary>
/// The homeostat, wired to the graph.
/// </summary>
/// <remarks>
/// <b>Acts rather than answers.</b> At every step the body feels itself, that
/// feeling is learnt as an occasion together with whatever was done about it, and
/// the next choice is made by walking from what is currently felt.
/// </remarks>
public sealed class HomeostatRun : IDisposable
{
    private readonly Fabric _fabric;
    private readonly InputMachine<Coded> _body;
    private readonly HomeostatSettings _settings;

    /// <summary>The join, held so a write can happen without thinking.</summary>
    private readonly LocalRendezvous _joining;
    private readonly WalkSettings _dials;

    /// <param name="world">The shape of the body.</param>
    /// <param name="dials">The walk.</param>
    /// <param name="seed">The world's generator and the ring's, so a run reproduces.</param>
    /// <remarks>
    /// <para>
    /// <b>THIS WORLD HAD NO TEMPORAL CELLS AT ALL, AND NOBODY HAD NOTICED.</b>
    /// Every occasion here is a flat set written under <see cref="Kind.With"/>, so
    /// <c>after</c> and <c>before</c> are empty and every question about what
    /// FOLLOWS reaches nothing. That is why step 4's world could be asked what
    /// helped HERE and never what helps where it is HEADING.
    /// </para>
    /// <para>
    /// <b>THE SPAN IS A REFUTED ROW, AND ITS REVIVAL CONDITION IS WHAT THIS IS.</b>
    /// Carrying was null on snake, worse on `Babi` and ruinous on `Rhythm`, and the
    /// row asks for <i>something that makes a carried edge worth its row</i>. A
    /// carried edge is the only thing that can answer <c>[After, Helped]</c>, and
    /// that question cannot be asked any other way — so the cost now buys something
    /// rather than merely existing.
    /// </para>
    /// <para>
    /// <b>Zero is the control and stays the default</b>, so every number this world
    /// has produced still stands and the span is an arm rather than a change.
    /// </para>
    /// </remarks>
    /// <param name="clusters">How many clusters the fabric holds.</param>
    /// <param name="replicas">Ring replicas per cluster.</param>
    public HomeostatRun(
        HomeostatSettings world,
        WalkSettings dials,
        int seed,
        int clusters = 8,
        int replicas = 256)
    {
        _fabric = Fabric.Standing(world, dials, seed, clusters, replicas);
        _settings = world;
        _dials = dials;
        Seed = seed;

        _joining = new LocalRendezvous(_fabric.Local);

        _body = new InputMachine<Coded>(
            new MachineAddress("body"), new Passthrough(), _joining,
            _fabric.Bus, _fabric.Ring, dials);

        _fabric.Subscribe(_body);
    }

    /// <summary>The seed this run was built with.</summary>
    public int Seed { get; }

    /// <summary>Runs the body for a while.</summary>
    /// <param name="steps">How long to run for.</param>
    /// <param name="choosing">How the body decides what to attend to.</param>
    /// <param name="ct">Cancellation.</param>
    public async Task<HomeostatResult> RunAsync(
        int steps, Attending choosing = Attending.Chain, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(steps);

        var world = new Homeostat(_settings);
        var rng = new Random(Seed);

        int held = 0, silent = 0, unbalanced = 0, unsettled = 0, choices = 0;
        var attended = new int[_settings.Needs];
        var chains = new Chains();

        // THE UNIT IS THE WORLD'S OWN, NOT A CONSTANT. One step's fall of the
        // fastest-draining variable is what breaking even looks like here, so it
        // is what earns the full band -- see Drives.
        var sensing = new Sensing(_settings.Drain * _settings.Needs);

        // AND WHAT IS OWED A CREDIT CELL, held the same way and for the same
        // reason: a rebuilt occasion pairs differently. See InputMachine.Joined.
        Occasion? crediting = null;

        for (var step = 0; step < steps; step++)
        {
            var felt = world.Feels();

            // WHAT IS FELT AND WHAT IS DONE ARE ONE OCCASION. The action joins the
            // state it was taken in, which is the only way a later walk from a
            // state can reach an action at all.
            // FEEL BEFORE ANYTHING ELSE, so the credit standing here is for the
            // transition that just happened -- which is the one the occasion
            // being held was responsible for.
            sensing.Note(world.At, felt);

            // THE ARMS THAT NEVER CONSULT THE GRAPH DECIDE WITHOUT A WALK, so
            // there is nothing to fold for them and `None` is silent.
            //
            // AND `Credited` ASKS A DIFFERENT QUESTION OF THE SAME GRAPH: what is
            // worth doing here, which is a walk over the credit cell alone. See
            // Kind.Helped.
            var walked = !Walks(choosing)
                ? Walked.None
                : await ChosenAsync(
                    felt,
                    chains,
                    choosing switch
                    {
                        Attending.Credited or Attending.Contested
                            => Question.Worthwhile(),
                        _ => null,
                    },
                    ct).ConfigureAwait(false);

            // THESE TWO WERE DECLARED HERE AND NEVER MOVED, so this world alone
            // reported `unbalanced=0` unconditionally and had no unsettled count
            // at all -- while every other world folded both. Two range checks
            // that could not fire, in the world step 4's conclusion rests on.
            if (!walked.Balanced) unbalanced++;
            if (!walked.Settled) unsettled++;
            if (walked.Candidates > 1) choices++;

            var chosen = choosing switch
            {
                Attending.Idle => (int?)null,
                Attending.Blind => rng.Next(world.Needs),
                Attending.Lowest => world.Lowest,
                _ => walked.Chosen,
            };

            // THE BOOTSTRAP, AND WITHOUT IT THIS ARM IS DEGENERATE. An action code
            // only ever enters the graph by joining the occasion it was taken in,
            // so a walk that has never seen one cannot propose one -- and a body
            // that never acts never sees one. Measured: silent on every step of a
            // four-hundred-step run, scoring identically to idling.
            //
            // Falling back to a random act when the walk has nothing to say breaks
            // the deadlock and makes the arm say something: given that it HAS seen
            // actions, does walking choose better than drawing at random. That is
            // the question step 4 is about, and the fallback is counted so the
            // share of the run the graph actually decided is visible.
            if (Walks(choosing) && chosen is null)
            {
                silent++;
                chosen = rng.Next(world.Needs);
            }

            if (chosen is { } picked && picked >= 0 && picked < attended.Length)
                attended[picked]++;

            ImmutableArray<Code> occasion = chosen is { } which
                ? [.. felt, Homeostat.Attending(which)]
                : felt;

            if (choosing is Attending.Credited or Attending.Contested)
            {
                // WRITTEN AS IT HAPPENED, exactly as `Chain` writes it, so the
                // ordinary cell is untouched and this arm changes one thing.
                await _body.ObserveAsync(Coded.Of(occasion), step, ct: ct).ConfigureAwait(false);
                await _fabric.QuietAsync(ct).ConfigureAwait(false);

                // AND THE PREVIOUS MOMENT COLLECTS A SECOND CELL IF IT EARNED ONE.
                // The credit standing here belongs to the transition that just
                // happened, which is the one the held occasion caused -- so an act
                // is priced by what followed it. `Credit` above one is the band
                // `Drives` gives an improvement; at or below one nothing is
                // written, and that absence is the whole of the contrast.
                //
                // AND `Contested` ALSO WRITES THE NEGATIVE CELL when the
                // most-at-risk variable got worse, so the walk reads a difference
                // rather than a hit rate. Both counts still only rise; see
                // Kind.Hindered.
                if (crediting is { } earned)
                {
                    var helped = sensing.Credit > 1.0;
                    var hurt = choosing == Attending.Contested && sensing.Credit < 1.0;

                    if (helped || hurt)
                    {
                        await _body
                            .ReinforceAsync(
                                earned with { As = hurt ? Kind.Hindered : Kind.Helped },
                                1.0,
                                ct)
                            .ConfigureAwait(false);

                        await _fabric.QuietAsync(ct).ConfigureAwait(false);
                    }

                }

                crediting = _body.Joined;
            }
            else
            {
                await _body.ObserveAsync(Coded.Of(occasion), step, ct: ct).ConfigureAwait(false);
                await _fabric.QuietAsync(ct).ConfigureAwait(false);
            }

            if (world.Step(chosen)) held++;
        }

        _fabric.Failures();

        return new HomeostatResult
        {
            Choosing = choosing,
            Steps = steps,
            Held = held,
            Silent = silent,
            Choices = choices,
            Improving = sensing.Improving,
            Attended = attended,
            States = sensing.States,
            Idling = world.Idling,
            Unsettled = unsettled,
            Plumbing = _fabric.Facts(chains, unbalanced),
        };
    }

    /// <summary>
    /// Whether this arm decides by consulting the graph.
    /// </summary>
    /// <remarks>
    /// <b>ONE LIST, BECAUSE IT WAS TWO AND THEY DRIFTED IMMEDIATELY.</b> The arms
    /// that walk were enumerated once for "fold the walk's accounting" and again
    /// for "fall back to a random act when the walk says nothing", and a new arm
    /// was added to the first and not the second. It walked, said nothing, and the
    /// body then simply did not act — scoring EXACTLY what idling scores with
    /// <c>attended</c> all zero and <c>silent</c> at nought, which reads as a
    /// finding rather than as a missing line. Named by what is true of them rather
    /// than by listing them, so a new arm is included by default.
    /// </remarks>
    private static bool Walks(Attending how) =>
        how is not (Attending.Idle or Attending.Blind or Attending.Lowest);

    /// <summary>
    /// What one walk produced, and <b>whether it was in any state to be read.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE TWO TRAVEL WITH THE ANSWER BECAUSE DISCARDING THEM IS HOW THIS WENT
    /// WRONG.</b> An unsettled walk answers "no action", which the bootstrap turns
    /// into a coin toss — so a run could be measuring randomness and reporting it
    /// as the graph's choice, with nothing saying so.
    /// </remarks>
    /// <param name="Chosen">Which need to attend to, or null if nothing was reached.</param>
    /// <param name="Settled">Whether the walk had finished when it was read.</param>
    /// <param name="Balanced">Whether the thought's own accounting closed.</param>
    /// <param name="Candidates">
    /// How many actions the walk actually offered. <b>A ranking arm can only be
    /// measured where this exceeds one</b> — see <see cref="HomeostatResult.Choices"/>.
    /// </param>
    private readonly record struct Walked(
        int? Chosen, bool Settled, bool Balanced, int Candidates)
    {
        /// <summary>An arm that decided without walking. There is nothing to fold.</summary>
        public static Walked None =>
            new(null, Settled: true, Balanced: true, Candidates: 0);
    }

    /// <summary>
    /// Walks from what is felt and takes whatever action it ranks first.
    /// </summary>
    /// <remarks>
    /// <b>The same narrowing every other world uses</b> — see
    /// <see cref="Thought.BestOf"/>. Nothing tells the walk what an action does;
    /// it has only ever seen actions beside the states they were taken in.
    /// </remarks>
    private async Task<Walked> ChosenAsync(
        ImmutableArray<Code> felt, Chains chains, Question? asking, CancellationToken ct)
    {
        var thought = await _body
            .ThinkAsync(felt, _dials.Stamina, asking, ct).ConfigureAwait(false);

        var settled = await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);

        var reached = thought.BestOf(Homeostat.Act, 1);

        // HOW MANY ACTIONS THE WALK ACTUALLY OFFERED, which is a different question
        // from whether it offered one. A ranking arm can only be measured where
        // there is something to rank. See HomeostatResult.Choices.
        var candidates = thought.BestOf(Homeostat.Act, int.MaxValue).Count;

        chains.Fold(thought.Best(int.MaxValue));

        var chosen = reached.Count == 0 ? (int?)null : Homeostat.Attended(reached[0].Endpoint);
        var balanced = thought.Balanced();

        _body.Forget(thought.Id);
        return new Walked(chosen, settled, balanced, candidates);
    }

    public void Dispose() => _fabric.Dispose();
}
