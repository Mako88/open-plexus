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

    public HomeostatRun(
        HomeostatSettings world,
        WalkSettings dials,
        int seed,
        int clusters = 8,
        int replicas = 256)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        _settings = world;
        _dials = dials;
        Seed = seed;
        _fabric = new Fabric(dials, seed, clusters, replicas);

        _joining = new LocalRendezvous(_fabric.Local);

        _body = new InputMachine<ImmutableArray<Code>>(
            new MachineAddress("body"), new Feeling(), _joining,
            _fabric.Bus, _fabric.Ring, dials);

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

        // WHAT IS OWED A WEIGHT. The occasion for a step cannot be written until
        // the step after it, because until then nothing has happened that could
        // say what it was worth.
        (ImmutableArray<Code> Codes, long At)? owed = null;

        // WHAT IS OWED A TOP-UP, held as the machine actually wrote it.
        Occasion? topping = null;

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
            var walked = choosing is Attending.Idle or Attending.Blind or Attending.Lowest
                ? Walked.None
                : await ChosenAsync(felt, chains, ct).ConfigureAwait(false);

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
            if (choosing is Attending.Chain or Attending.Driven or Attending.Delayed
                    or Attending.Topped
                && chosen is null)
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
        ImmutableArray<Code> felt, Chains chains, CancellationToken ct)
    {
        var thought = await _body
            .ThinkAsync(felt, _dials.Stamina, null, ct).ConfigureAwait(false);

        var settled = await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);

        var reached = thought.BestOf(Homeostat.Act, 1);
        chains.Fold(thought.Best(int.MaxValue));

        var chosen = reached.Count == 0 ? (int?)null : Homeostat.Attended(reached[0].Endpoint);
        var balanced = thought.Balanced();

        _body.Forget(thought.Id);
        return new Walked(chosen, settled, balanced);
    }

    public void Dispose() => _fabric.Dispose();
}
