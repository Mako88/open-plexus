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
/// identical under all four; only the choice moves.
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
    /// The graph again, with what it learns WEIGHTED BY WHETHER THINGS GOT
    /// BETTER — <b>step 4's third factor.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A SEPARATE ARM BECAUSE IT CHANGES TWO THINGS, and saying so is the
    /// point.</b> It weights the occasion by <see cref="Drives.Credit"/>, and it
    /// writes that occasion ONE STEP LATE — an act can only be priced by what
    /// followed it, so the credit for a transition belongs to the occasion before
    /// it. <see cref="Chain"/> is left exactly as it was measured.
    /// </para>
    /// <para>
    /// <b>The bar is <see cref="Blind"/> and not <see cref="Idle"/>.</b> Choosing
    /// by association already scores below drawing at random, which is what step 4
    /// exists to fix; beating idling would only mean the arithmetic still works.
    /// </para>
    /// </remarks>
    Driven,

    /// <summary>
    /// <see cref="Driven"/>'s delay WITHOUT its credit — <b>the control that says
    /// which of the two changes did the work.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE ARM CHANGES TWO THINGS, so on its own it can attribute neither.</b>
    /// Writing an occasion one step late means every walk sees a graph one
    /// occasion staler, which is a difference all by itself. This writes just as
    /// late and always at weight one, so the gap between this and
    /// <see cref="Driven"/> is the credit and nothing else.
    /// </remarks>
    Delayed,

    /// <summary>
    /// The credit as a TOP-UP rather than a delay — <b>write at once, add the
    /// rest when the outcome lands.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>MEASURED: THE DELAY COSTS NINE TIMES WHAT THE CREDIT BUYS.</b> So the
    /// credit is kept and the delay is not. The occasion is written immediately at
    /// weight one, exactly as <see cref="Chain"/> writes it, and a step later the
    /// SAME occasion is written again carrying whatever the transition earned
    /// beyond one.
    /// </para>
    /// <para>
    /// <b>ADDING TWICE IS WHAT A G-COUNTER IS FOR</b>, which is the whole reason
    /// this is legal where subtracting would not be. A transition that went badly
    /// simply gets no second write; there is no punishment available, only degrees
    /// of reinforcement, and that is a property of the CRDT rather than a choice.
    /// </para>
    /// <para>
    /// <b>The top-up is LEARNING WITHOUT THINKING</b>, so it goes to the
    /// rendezvous directly. Sending it back through the input machine would find
    /// no onsets — the codes are already live — and would join nothing at all.
    /// </para>
    /// </remarks>
    Topped,

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
    /// </remarks>
    Credited,

    /// <summary>
    /// <see cref="Credited"/>'s SECOND CELL WITHOUT ITS CONDITION — <b>the control
    /// that says whether the contrast did the work.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE ARM CHANGES TWO THINGS, so on its own it can attribute neither.</b>
    /// It writes a pair into a cell nothing else writes, and it walks that cell
    /// instead of the ordinary one — which on its own is a walk over a ONE-STEP
    /// STALE association, and staleness is a difference all by itself. This writes
    /// the same second cell on EVERY step regardless of whether anything improved,
    /// so the gap between this and <see cref="Credited"/> is the condition and
    /// nothing else.
    /// </remarks>
    Marked,

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
    /// variable got worse</b>, and the walk reads the difference. Both counts still
    /// only ever rise — the PN-Counter property — so nothing about convergence
    /// changes. See <see cref="Graph.Kind.Hindered"/> for why the plan's "counts
    /// only increment, so punishment is unavailable" was the wrong CRDT rather
    /// than a law.
    /// </para>
    /// </remarks>
    Contested,

    /// <summary>
    /// <see cref="Credited"/> writing exactly the same graph, ASKED A WIDER
    /// QUESTION — <b>step 9, and the attack on the silence rather than the
    /// score.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>ONE THING CHANGES, AND IT IS THE QUESTION.</b> Every write here is
    /// byte-for-byte what <see cref="Credited"/> writes: the occasion as it
    /// happened, and the second cell a step later only if the most-at-risk
    /// variable improved. What moves is that the choice walks
    /// <see cref="Thinking.Question.Alike"/> instead of
    /// <see cref="Thinking.Question.Worthwhile"/>.
    /// </para>
    /// <para>
    /// <b>BECAUSE THE SCORE WAS NEVER THE PROBLEM — THE SILENCE WAS.</b>
    /// <c>Worthwhile</c> asks the credit cell of the codes felt right now, and
    /// most of the time every one of them is empty, so the arm falls back on its
    /// coin toss for the great majority of a run. That is not inexperience:
    /// quadrupling the run moves neither the silence nor the score, because the
    /// state count grows as fast as the coverage does.
    /// </para>
    /// <para>
    /// <b>So the walk takes one hop OUT FIRST.</b> Anything that has shared a
    /// moment with what is felt now, and then what helped THERE — credit earned in
    /// a state this one merely resembles, spent in a state that earned none.
    /// </para>
    /// <para>
    /// <b>MEASURED, AND IT IS THE SECOND FAILURE THE COMMENT PREDICTED: LOUDER AND
    /// WORSE.</b> The silence really does collapse — the coverage problem is
    /// genuinely gone — and the score falls below drawing at random. <b>A shared
    /// moment is too cheap a notion of alike.</b> <see cref="Graph.Kind.With"/> is
    /// symmetric and dense, so one hop reaches nearly everything, "states like this
    /// one" becomes "almost every state", and the credit averages back into the
    /// behaviour policy step 4 exists to escape.
    /// </para>
    /// <para>
    /// <b>KEPT AS THE MEASURED CONTROL FOR A SHARPER NOTION OF ALIKE.</b>
    /// <see cref="Thinking.Question.Downstream"/> is that notion — states whose
    /// FUTURES agree rather than whose moments do — and it cannot be asked here
    /// yet: the reverse temporal edge it needs is exactly the one a carried code
    /// does not write. See <see cref="Graph.Kind.Before"/>.
    /// </para>
    /// </remarks>
    Kindred,

    /// <summary>
    /// <see cref="Credited"/> asking what helps where the body is HEADING —
    /// <b>lookahead rather than likeness, and step 11 at depth one.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>WHY THIS AFTER <see cref="Kindred"/> FAILED.</b> Likeness was the wrong
    /// idea rather than a badly tuned one: any cheap notion of <i>alike</i> reaches
    /// most of the world, and credit averaged over most of the world is the
    /// behaviour policy again. <b>A CONSEQUENCE IS NOT A RESEMBLANCE</b> — what
    /// usually follows this state is a far smaller set than what merely co-occurs
    /// with it, and it is the set that actually bears on what to do now.
    /// </para>
    /// <para>
    /// <b>IT NEEDS THIS WORLD TO HAVE TEMPORAL CELLS AT ALL, WHICH IT DID NOT.</b>
    /// Every occasion here was a flat set, so <c>after</c> was empty everywhere.
    /// The span that fills it is a refuted row being revived by its own condition —
    /// see the constructor.
    /// </para>
    /// <para>
    /// <b>THE SPAN CHANGES TWO THINGS AT ONCE, so the arm cannot attribute
    /// either.</b> Carrying a window adds edges to the graph AND lets a new
    /// question be asked. <see cref="Credited"/> run at the same span is the
    /// control that separates them: same graph, old question.
    /// </para>
    /// </remarks>
    Foreseeing,

    /// <summary>
    /// The precise question first, and the general one ONLY where it was silent —
    /// <b>backoff, and it is the synthesis of two failures rather than a third
    /// idea.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A FIXED PATH TRADES COVERAGE AGAINST PRECISION WITH NO MIDDLE, WHICH IS
    /// WHAT STEP 9 ACTUALLY FOUND.</b> <see cref="Kindred"/> asks through
    /// <c>With</c>, which is dense: it reaches nearly every state, so the credit
    /// averages into the behaviour policy and the arm scores below a coin toss.
    /// <see cref="Foreseeing"/> asks through <c>After</c>, which is sparse: the
    /// composition is empty, the walk is silent on every step of a run at every
    /// window width, and the arm IS a coin toss. <b>Same failure, opposite ends.</b>
    /// </para>
    /// <para>
    /// <b>SO ASK THE NARROW QUESTION FIRST AND WIDEN ONLY ON SILENCE.</b> That is
    /// Katz backoff — the estimator language modelling has used for forty years for
    /// exactly this shape of problem, where the specific statistic is right when it
    /// exists and absent most of the time. <see cref="Question.Worthwhile"/> where
    /// the credit cell has something to say; <see cref="Question.Alike"/> only
    /// where it does not.
    /// </para>
    /// <para>
    /// <b>AND IT IS A REAL TEST RATHER THAN A HOPEFUL ONE.</b> <c>Kindred</c> scored
    /// BELOW drawing at random, so the general question's answers are worse than a
    /// coin toss on average — and this arm replaces a coin toss with exactly those
    /// answers. <b>If it loses, the general question is simply bad. If it wins, the
    /// general question was only bad because it was overriding the specific one</b>,
    /// and those are different diagnoses wanting different things next.
    /// </para>
    /// <para>
    /// <b>It costs a second walk on the steps where the first said nothing</b>, and
    /// the message count reports that.
    /// </para>
    /// </remarks>
    Backing,

    /// <summary>
    /// <see cref="Credited"/> with a REASON TO SEEK where it had a coin toss —
    /// <b>step 10, and it aims at the bootstrap rather than at the score.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>WHAT THIS DESIGN HAS NEVER HAD.</b> <see cref="Learning.Drives"/> makes
    /// the body want to stay alive; nothing makes it want to find out.
    /// <see cref="Learning.Surprise"/> is a quantity the machine reads about
    /// itself and no action has ever been chosen to move it.
    /// </para>
    /// <para>
    /// <b>THE SAME SHAPE AS THE CREDIT CELL, WITH A DIFFERENT THIRD FACTOR.</b>
    /// Each step the graph is asked what usually FOLLOWS, and the answer is held.
    /// The step after, what actually arrived is compared against it, and the
    /// occasion that caused the transition earns
    /// <see cref="Graph.Kind.Informed"/> if the machine was WRONG — exactly as it
    /// earns <see cref="Graph.Kind.Helped"/> if the body improved.
    /// </para>
    /// <para>
    /// <b>AND IT IS ASKED SECOND, NEVER INSTEAD.</b> The credit cell answers where
    /// it has anything to say; curiosity replaces the fallback and nothing else. So
    /// this is <see cref="Backing"/>'s structure with a different second question —
    /// and where backoff's wider walk lost, this one does not widen at all, which
    /// is what keeps it clear of step 9's refutation.
    /// </para>
    /// <para>
    /// <b>IT NEEDS TEMPORAL CELLS, so it runs at a span.</b> A prediction of what
    /// follows cannot be made in a world whose occasions are all flat sets, which
    /// is what this world was until the span arrived.
    /// </para>
    /// <para>
    /// <b>MEASURED, AND IT FAILS — AND NOT BY WIDENING, WHICH IS WHY IT MATTERS.</b>
    /// The walk is one hop, exactly as narrow as <see cref="Credited"/>'s, and the
    /// arm still collapses: silence falls and the score falls with it. <b>THE CELL
    /// IS WRITTEN TOO OFTEN.</b> Early on the machine predicts nothing, so nearly
    /// every transition is surprising, nearly every occasion earns
    /// <see cref="Graph.Kind.Informed"/>, and <i>what was informative here</i>
    /// becomes <i>what was done here</i> — the behaviour policy, reached by a third
    /// route.
    /// </para>
    /// <para>
    /// <b>THAT IS THE NOISY-TELEVISION PROBLEM, AND SCHMIDHUBER'S CLAIM WAS NEVER
    /// ABOUT ERROR.</b> Prediction error alone is maximised by noise; what is meant
    /// to be sought is COMPRESSION PROGRESS — the error coming DOWN. Rewarding the
    /// error itself rewards whatever is least learnable. <see cref="Probing"/> is
    /// that correction.
    /// </para>
    /// </remarks>
    Curious,

    /// <summary>
    /// <see cref="Curious"/> written SELECTIVELY — <b>the control that says whether
    /// curiosity failed, or only the signal did.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>ONE THING CHANGES: WHEN THE CELL IS WRITTEN.</b> Everything else — the
    /// prediction, the question, the fallback structure — is
    /// <see cref="Curious"/> exactly. This writes
    /// <see cref="Graph.Kind.Informed"/> only where the machine did WORSE than its
    /// own running average, so the cell stays rare by construction and rarity is
    /// the thing being tested.
    /// </para>
    /// <para>
    /// <b>NO CONSTANT ANYWHERE.</b> The threshold is the machine's own
    /// <see cref="Learning.Surprise.Rate"/>, which is a quantity it already keeps
    /// about itself — so this is a dial that sets itself rather than a sixth knob,
    /// which is the standing ask.
    /// </para>
    /// <para>
    /// <b>WHAT IT SEPARATES.</b> <see cref="Credited"/> works and is written
    /// rarely; three arms that write densely all collapse to the behaviour policy.
    /// If this recovers, <b>selectivity is the mechanism</b> and the semantics of
    /// the second cell barely matter. If it does not, the contrast in
    /// <see cref="Graph.Kind.Helped"/> is doing something rarity alone cannot
    /// explain.
    /// </para>
    /// </remarks>
    Probing,
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
        $"choosing={Choosing} steps={Steps} held={Held} silent={Silent} | " +
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
    private readonly InputMachine<ImmutableArray<Code>> _body;
    private readonly HomeostatSettings _settings;

    /// <summary>
    /// The join, held so a top-up can write without thinking — see
    /// <see cref="Attending.Topped"/>.
    /// </summary>
    private readonly LocalRendezvous _joining;
    private readonly WalkSettings _dials;

    /// <summary>How many moments a departed code is carried for.</summary>
    private readonly int _span;

    /// <param name="world">The shape of the body.</param>
    /// <param name="dials">The walk.</param>
    /// <param name="seed">The world's generator and the ring's, so a run reproduces.</param>
    /// <param name="span">
    /// How many moments a departed code is carried for — <b>and zero is every
    /// measurement this world has ever produced.</b>
    /// </param>
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
        int span = 0,
        int clusters = 8,
        int replicas = 256)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        _settings = world;
        _dials = dials;
        Seed = seed;
        _span = span;
        _fabric = new Fabric(dials, seed, clusters, replicas);

        _joining = new LocalRendezvous(_fabric.Local);

        _body = new InputMachine<ImmutableArray<Code>>(
            new MachineAddress("body"), new Feeling(), _joining,
            _fabric.Bus, _fabric.Ring, dials, span);

        _fabric.Subscribe(_body);
    }

    /// <summary>The seed this run was built with.</summary>
    public int Seed { get; }

    /// <summary>The codes are already codes; there is nothing to quantise.</summary>
    private sealed class Feeling : IQuantizer<ImmutableArray<Code>>
    {
        public byte Modality => Homeostat.Need;

        public IReadOnlyCollection<Code> Codify(ImmutableArray<Code> observation) => observation;
    }

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

        int held = 0, silent = 0, unbalanced = 0, unsettled = 0;
        var attended = new int[_settings.Needs];
        var states = new HashSet<string>(StringComparer.Ordinal);
        var chains = new Chains();

        // THE UNIT IS THE WORLD'S OWN, NOT A CONSTANT. One step's fall of the
        // fastest-draining variable is what breaking even looks like here, so it
        // is what earns the full band -- see Drives.
        var drives = new Drives(_settings.Drain * _settings.Needs);

        // STEP 10'S THIRD FACTOR, and null for every arm that does not seek. Held
        // by the run rather than handed to the machine on purpose: passing it to
        // `InputMachine` would also switch on step 2's broadcast gating, and an arm
        // that changed the walk AND the learning could attribute neither.
        var surprise = choosing is Attending.Curious or Attending.Probing
            ? new Surprise()
            : null;

        // WHAT IS OWED A WEIGHT. The occasion for a step cannot be written until
        // the step after it, because until then nothing has happened that could
        // say what it was worth.
        (ImmutableArray<Code> Codes, long At)? owed = null;

        // WHAT IS OWED A TOP-UP, held as the machine actually wrote it.
        Occasion? topping = null;

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
            drives.Feel(world.At);
            states.Add(string.Join(",", felt.Select(code => $"{code.Modality}:{code.Value}")));

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
                        // ONE HOP OUT BEFORE THE CREDIT CELL, so a state that
                        // earned nothing can still be advised -- step 9.
                        Attending.Kindred => Question.Alike(),

                        // AND THE HOP FORWARD RATHER THAN SIDEWAYS: what usually
                        // follows here, and what was worth doing there.
                        Attending.Foreseeing => Question.Ahead(),

                        Attending.Credited or Attending.Marked
                            or Attending.Contested
                            or Attending.Backing
                            or Attending.Curious or Attending.Probing
                                => Question.Worthwhile(),

                        _ => null,
                    },
                    ct).ConfigureAwait(false);

            // A SECOND QUESTION ONLY WHERE THE FIRST WAS SILENT, AND NEVER INSTEAD.
            // A state whose own credit cell has something to say is answered by it,
            // so the second walk cannot override a specific answer -- which is
            // exactly what `Kindred` did wrong.
            //
            // THE TWO SECOND QUESTIONS ARE NOT THE SAME KIND OF THING, AND STEP 9
            // IS WHY. `Backing` asks a WIDER walk, which is refuted: anything wide
            // enough to stop being silent converges on what was done most. `Curious`
            // asks a walk of the SAME width over a DIFFERENT statistic, so it does
            // not inherit that.
            var second = choosing switch
            {
                Attending.Backing => Question.Alike(),
                Attending.Curious or Attending.Probing => Question.Curious(),
                _ => null,
            };

            if (second is not null && walked.Chosen is null)
            {
                var again = await ChosenAsync(felt, chains, second, ct)
                    .ConfigureAwait(false);

                // THE PLUMBING OF BOTH WALKS COUNTS, not just the one that
                // answered. A second walk that failed to settle is still a second
                // walk, and hiding it would understate what this arm costs.
                walked = new Walked(
                    again.Chosen,
                    walked.Settled && again.Settled,
                    walked.Balanced && again.Balanced);
            }

            // THESE TWO WERE DECLARED HERE AND NEVER MOVED, so this world alone
            // reported `unbalanced=0` unconditionally and had no unsettled count
            // at all -- while every other world folded both. Two range checks
            // that could not fire, in the world step 4's conclusion rests on.
            if (!walked.Balanced) unbalanced++;
            if (!walked.Settled) unsettled++;

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

            if (choosing == Attending.Topped)
            {
                // AT ONCE, AT WEIGHT ONE -- the walk and the graph see exactly
                // what `Chain` sees, so nothing is delayed.
                await _body.ObserveAsync(occasion, step, ct: ct).ConfigureAwait(false);
                await _fabric.QuietAsync(ct).ConfigureAwait(false);

                // AND THE PREVIOUS OCCASION COLLECTS WHAT IT EARNED. Only the
                // surplus above one, and only when there is one: a G-Counter can
                // be added to twice and can never be added to less.
                //
                // THE OCCASION THE MACHINE WROTE, NOT ONE REBUILT FROM THE CODES.
                // Rebuilding gets a neighbouring occasion -- onsets separated from
                // what was already live, the window's carried codes folded in --
                // and measured, that lost to not topping up at all.
                if (topping is { } due && drives.Credit > 1.0)
                {
                    await _body
                        .ReinforceAsync(due, drives.Credit - 1.0, ct)
                        .ConfigureAwait(false);

                    await _fabric.QuietAsync(ct).ConfigureAwait(false);
                }

                topping = _body.Joined;
            }
            else if (choosing is Attending.Driven or Attending.Delayed)
            {
                // ONE STEP LATE, AND WEIGHTED BY WHAT FOLLOWED. The occasion held
                // from last step is written now, priced by the transition it
                // produced; this step's occasion is held in its place. The walk
                // above already ran against the graph as it stood, so what is
                // delayed is the LEARNING and never the thinking.
                if (owed is { } last)
                {
                    var worth = choosing == Attending.Driven ? drives.Credit : 1.0;

                    await _body
                        .ObserveAsync(last.Codes, last.At, worth: worth, ct: ct)
                        .ConfigureAwait(false);

                    await _fabric.QuietAsync(ct).ConfigureAwait(false);
                }

                owed = (occasion, step);
            }
            else if (choosing is Attending.Credited or Attending.Marked
                     or Attending.Contested or Attending.Kindred
                     or Attending.Foreseeing or Attending.Backing
                     or Attending.Curious or Attending.Probing)
            {
                // WRITTEN AS IT HAPPENED, exactly as `Chain` writes it, so the
                // ordinary cell is untouched and this arm changes one thing.
                await _body.ObserveAsync(occasion, step, ct: ct).ConfigureAwait(false);
                await _fabric.QuietAsync(ct).ConfigureAwait(false);

                // AND THE PREVIOUS MOMENT COLLECTS A SECOND CELL IF IT EARNED ONE.
                // The credit standing here belongs to the transition that just
                // happened, which is the one the held occasion caused -- so an act
                // is priced by what followed it. `Credit` above one is the band
                // `Drives` gives an improvement; at or below one nothing is
                // written, and that absence is the whole of the contrast.
                // `Marked` writes it unconditionally, which is the control: same
                // cell, same staleness, no contrast.
                //
                // AND `Contested` ALSO WRITES THE NEGATIVE CELL when the
                // most-at-risk variable got worse, so the walk reads a difference
                // rather than a hit rate. Both counts still only rise; see
                // Kind.Hindered.
                if (crediting is { } earned)
                {
                    var helped = choosing == Attending.Marked || drives.Credit > 1.0;
                    var hurt = choosing == Attending.Contested && drives.Credit < 1.0;

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

                    // STEP 10 -- THE OTHER THIRD FACTOR, AND IT IS SCORED AGAINST
                    // THE MACHINE RATHER THAN THE BODY. The expectation held from
                    // last step is settled against what is felt now; a transition
                    // that was NOT foreseen taught the model something, and the
                    // occasion that caused it earns its own cell. See
                    // Kind.Informed.
                    if (surprise is not null)
                    {
                        // THE RUNNING AVERAGE, READ BEFORE THIS MOMENT JOINS IT.
                        // `Probing` writes only where the machine did worse than
                        // it usually does, so the threshold is a quantity the
                        // machine already keeps about itself rather than a knob.
                        var usually = surprise.Rate;

                        var residual = surprise.Residual(felt);

                        var foreseen = felt.Length == 0
                            ? 1.0
                            : 1.0 - (residual.Surprising.Count / (double)felt.Length);

                        // AND THE ONE THING THAT DIFFERS BETWEEN THE TWO ARMS.
                        // `Curious` takes any surprise at all -- which early on is
                        // every transition, so the cell stops discriminating. See
                        // Attending.Probing.
                        var worthKnowing = residual.Surprising.Count > 0
                            && (choosing == Attending.Curious || foreseen < usually);

                        if (worthKnowing)
                        {
                            await _body
                                .ReinforceAsync(
                                    earned with { As = Kind.Informed }, 1.0, ct)
                                .ConfigureAwait(false);

                            await _fabric.QuietAsync(ct).ConfigureAwait(false);
                        }
                    }
                }

                crediting = _body.Joined;

                // AND WHAT THE GRAPH EXPECTS OF THE NEXT MOMENT, ASKED AFTER THE
                // OCCASION IS WRITTEN so the prediction sees the graph the body
                // has actually just moved through. Held, and settled one step from
                // now against what arrives.
                if (surprise is not null)
                    surprise.Expect(
                        await ExpectingAsync(occasion, chains, ct).ConfigureAwait(false));
            }
            else
            {
                await _body.ObserveAsync(occasion, step, ct: ct).ConfigureAwait(false);
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
            Attended = attended,
            States = states.Count,
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
    private readonly record struct Walked(int? Chosen, bool Settled, bool Balanced)
    {
        /// <summary>An arm that decided without walking. There is nothing to fold.</summary>
        public static Walked None => new(null, Settled: true, Balanced: true);
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
        chains.Fold(thought.Best(int.MaxValue));

        var chosen = reached.Count == 0 ? (int?)null : Homeostat.Attended(reached[0].Endpoint);
        var balanced = thought.Balanced();

        _body.Forget(thought.Id);
        return new Walked(chosen, settled, balanced);
    }

    /// <summary>
    /// What the graph expects to be true next — <b>step 10's third factor, and the
    /// only thing here the machine can be WRONG about.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>NARROWED TO WHAT THE BODY FEELS AND NOT TO ACTIONS.</b> A prediction that
    /// named the next act would be scored against a choice this run is about to
    /// make, so it would be right or wrong for reasons that have nothing to do with
    /// the model.
    /// </para>
    /// <para>
    /// <b>SCORED AGAINST WHAT IS FELT RATHER THAN AGAINST ONSETS</b>, which is a
    /// departure from <see cref="Surprise"/>'s own reading and deliberate here: a
    /// band that was predicted and stayed put is a prediction that came TRUE, and
    /// counting it absent would make a still body read as maximally surprising.
    /// </para>
    /// </remarks>
    private async Task<IReadOnlyList<Code>> ExpectingAsync(
        ImmutableArray<Code> felt, Chains chains, CancellationToken ct)
    {
        var thought = await _body
            .ThinkAsync(felt, _dials.Stamina, Question.Following(), ct)
            .ConfigureAwait(false);

        await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);
        chains.Fold(thought.Best(int.MaxValue));

        // ONE PER NEED, which is as many codes as the next moment can hold. Naming
        // more would make the machine right by exhaustion, which is the failure
        // `Surprise.Overreach` exists to catch.
        //
        // EVERY NEED IS ITS OWN MODALITY -- need `i` is `Need + i` -- so the
        // narrowing is a band of modalities rather than one, and `BestOf` takes
        // exactly one. Ranked order is preserved by taking from `Best`.
        var named = thought.Best(int.MaxValue)
            .Where(arrival => arrival.Endpoint.Modality >= Homeostat.Need
                && arrival.Endpoint.Modality < Homeostat.Need + _settings.Needs)
            .Take(_settings.Needs)
            .Select(arrival => arrival.Endpoint)
            .ToList();

        _body.Forget(thought.Id);
        return named;
    }

    public void Dispose() => _fabric.Dispose();
}
