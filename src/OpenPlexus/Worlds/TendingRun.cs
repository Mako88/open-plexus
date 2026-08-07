using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using OpenPlexus.Thinking;

namespace OpenPlexus.Worlds;

/// <summary>How the body decides what to do.</summary>
/// <remarks>
/// <b>Controls that change ONE thing.</b> The garden, the drying and the score are
/// identical under all of them; only the choice moves.
/// </remarks>
public enum Gardening
{
    /// <summary>Nothing at all. <b>The control that says drying is not optional.</b></summary>
    Idle,

    /// <summary>An action at random, without looking at the garden.</summary>
    Blind,

    /// <summary>Whatever the graph ranks first, walking what was DONE here.</summary>
    Chain,

    /// <summary>Go to the driest plant and water it. <b>The ceiling.</b></summary>
    Best,

    /// <summary>
    /// The graph, walking the credit cell — <b>step 4's arm, in its second
    /// world.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE WHOLE REASON THIS WORLD EXISTS IS THAT THIS ARM SHOULD STRUGGLE
    /// HERE.</b> <see cref="Graph.Kind.Helped"/> is written when the most-at-risk
    /// plant improved between one step and the next, and a MOVE never improves
    /// anything — it only makes the next act possible. So a one-step credit signal
    /// must rate every move as worthless, and a body that never moves waters one
    /// plant forever. <b>Whatever this scores, the gap it leaves is step 7.</b>
    /// </remarks>
    Credited,

    /// <summary>
    /// The credit spread back over everything still in the trace — <b>step 7, and
    /// the first thing here that can say an act led somewhere good LATER.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THREE-FACTOR HEBBIAN LEARNING, AND IT NEEDS NEITHER A REWARD FUNCTION
    /// NOR BACKPROPAGATION</b> (Izhikevich 2007, the distal reward problem): a
    /// fading record of what recently fired, and a third signal that consolidates
    /// whatever is still in it, most credit to the most recent.
    /// <see cref="Credited"/> is the same mechanism with a trace of length one.
    /// </para>
    /// <para>
    /// <b>IT IS AIMED AT COVERAGE, WHICH IS THE DIAGNOSED BOTTLENECK.</b> One
    /// credit event writes one cell under <see cref="Credited"/>, so the credit
    /// covers a vanishing share of a state count that keeps growing — measured here
    /// as silence on nearly every step. Under this, one event writes a cell for
    /// every state still in the trace. <b>Step 9 established that the coverage
    /// cannot be fixed by asking a WIDER question; this widens what is WRITTEN
    /// instead.</b>
    /// </para>
    /// <para>
    /// <b>AND IT IS THE ONLY THING THAT CAN CREDIT A MOVE.</b> A move improves
    /// nothing, so a one-step signal must rate it worthless — while the watering it
    /// enabled is two steps away. A trace reaching back that far is what makes
    /// means-ends expressible at all.
    /// </para>
    /// <para>
    /// <b>SAFE FOR THE CRDT PROPERTY.</b> The trace is transient state deciding how
    /// MUCH to add; every count still only rises, so convergence is untouched. That
    /// is the same argument that makes <see cref="Graph.Kind.Helped"/> legal.
    /// </para>
    /// </remarks>
    Traced,

    /// <summary>
    /// <see cref="Traced"/>'s trace WITHOUT its decay — <b>the control that says
    /// whether recency did the work, or merely writing more did.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE ARM CHANGES TWO THINGS, so on its own it can attribute neither.</b> A
    /// trace writes more cells AND weights them by how recent they are. This writes
    /// the same cells over the same span at full weight, so the gap between this
    /// and <see cref="Traced"/> is the recency and nothing else — and if the two
    /// match, <i>most credit to the most recent</i> is decoration and the mechanism
    /// is simply coverage.
    /// </remarks>
    Smeared,

    /// <summary>
    /// The graph proposes and THE MACHINE'S OWN HISTORY disposes — <b>exploration
    /// where it can actually live, which is not in the walk.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>NO WALK CAN RECOMMEND AN ACT THE BODY HAS NEVER TAKEN.</b> Every cell is
    /// keyed on <i>(state, act)</i> and written only for acts taken, so three
    /// different questions over three different statistics produce one identical
    /// run. That is structural, not a tuning failure, and no fourth statistic
    /// fixes it.
    /// </para>
    /// <para>
    /// <b>BUT A MACHINE KNOWS WHAT IT HAS DONE, and its own history is its own
    /// data.</b> So the tally lives on the actuator, C1-legal by exactly the
    /// argument that makes <see cref="Thinking.Message.Seen"/> legal — a node
    /// sending its own count about itself is not reading anybody else's.
    /// </para>
    /// <para>
    /// <b>OPTIMISM UNDER UNCERTAINTY, and the bonus form is the standard one:</b>
    /// <c>√(ln t / n)</c> falls as an act is tried and rises as the run goes on, so
    /// an act neglected long enough is eventually revisited and one tried often
    /// stops being urged. <b>The coin toss is the degenerate version of this</b> —
    /// a bonus that never falls.
    /// </para>
    /// <para>
    /// <b>THE ONE REAL JUDGEMENT IS SCALE, AND IT IS SAID OUT LOUD.</b> Path
    /// strengths and a count bonus are not in the same units, so either could
    /// swamp the other by accident of magnitude. Both are normalised to their own
    /// maximum before being added, which chooses no constant and makes the trade
    /// one-for-one — <b>and that IS a choice, just a visible one.</b>
    /// </para>
    /// </remarks>
    Venturing,
}

/// <summary>What the garden measured. <b>Counts, not claims.</b></summary>
public sealed record TendingResult : Measurement
{
    /// <inheritdoc cref="Gardening"/>
    public required Gardening Choosing { get; init; }

    /// <summary>Steps taken.</summary>
    public required int Steps { get; init; }

    /// <summary>Of those, how many left every plant inside its bounds.</summary>
    public required int Held { get; init; }

    /// <summary>How long doing nothing would have lasted.</summary>
    public required int Idling { get; init; }

    /// <summary>How many times each action was taken. <b>The diagnostic.</b></summary>
    public required IReadOnlyList<int> Doing { get; init; }

    /// <summary>How many times each plant was watered.</summary>
    public required IReadOnlyList<int> Watered { get; init; }

    /// <summary>How many DISTINCT states the body was ever in, as the graph sees them.</summary>
    public required int States { get; init; }

    /// <summary>
    /// Steps where the walk proposed nothing, <b>so the bootstrap acted at random
    /// instead.</b>
    /// </summary>
    /// <remarks>
    /// <b>Read beside the score, always.</b> An arm acting at random where the
    /// graph is silent drifts toward the random bar for free, and that reads as
    /// the change working.
    /// </remarks>
    public required int Silent { get; init; }

    /// <inheritdoc cref="Worlds.Reflections"/>
    public required Reflections Reflections { get; init; }

    /// <summary>The share of the run spent viable. <b>The score.</b></summary>
    /// <remarks>
    /// <b>Time viable, not time until failure.</b> Nothing resets, so a garden
    /// that goes out of bounds carries on and may come back — which is what keeps
    /// C4 and what disqualified survival as a score.
    /// </remarks>
    public double Viable => Steps == 0 ? 0.0 : Held / (double)Steps;

    /// <summary>How much of the run was spent moving rather than watering.</summary>
    /// <remarks>
    /// <b>THE NUMBER THIS WORLD ADDS.</b> A body that never moves waters one plant
    /// forever and the rest die; a body that only moves waters nothing. Neither
    /// shows up in the score as anything but failure, and this says which.
    /// </remarks>
    public double Travelling =>
        Steps == 0 ? 0.0 : (Doing[0] + Doing[1]) / (double)Steps;

    /// <inheritdoc/>
    protected override int Composes => Choosing == Gardening.Chain ? 2 : 0;

    /// <inheritdoc/>
    protected override string Stalled => "no route walked at all";

    /// <inheritdoc/>
    protected override void Peculiar(List<string> wrong)
    {
        ArgumentNullException.ThrowIfNull(wrong);

        if (Held > Steps) wrong.Add($"held {Held} of {Steps} steps");
        if (Silent > Steps) wrong.Add($"silent on {Silent} of {Steps} steps");

        // A CHECK THAT CAN ACTUALLY FIRE. The ceiling arm reads the levels as
        // numbers, so if IT cannot hold the garden the world is not winnable and
        // every other number on it is meaningless.
        if (Choosing == Gardening.Best && Viable < 0.5)
            wrong.Add($"the oracle held the garden for only {Viable:F4} of the run");
    }

    public override string ToString() =>
        $"choosing={Choosing} steps={Steps} held={Held} silent={Silent} | "
        + $"viable={Viable:F4} idling={Idling} travelling={Travelling:F4} | "
        + $"doing=[{string.Join(",", Doing)}] watered=[{string.Join(",", Watered)}] "
        + $"states={States} | {Plumbing}";
}

/// <summary>
/// The garden, run against the graph — <b>the second body, and the first world
/// here where an act is enabled by an act that helped nothing.</b>
/// </summary>
/// <remarks>
/// <b>See <see cref="Tending"/> for why this world exists.</b> In one line: every
/// act on <see cref="Homeostat"/> pays off in the step it is taken, so a world
/// where one-step credit is sufficient cannot measure the mechanisms built because
/// one-step credit is insufficient.
/// </remarks>
public sealed class TendingRun : IDisposable
{
    private readonly TendingSettings _settings;
    private readonly Fabric _fabric;
    private readonly InputMachine<Tended> _body;

    /// <summary>
    /// The body's front ends — <b>moisture is a real vector and position is a
    /// name, so they are quantised differently and land in one occasion.</b>
    /// </summary>
    private readonly Compound<Tended> _senses;
    private readonly WalkSettings _dials;
    /// <param name="world">The shape of the garden.</param>
    /// <param name="dials">The walk.</param>
    /// <param name="seed">The world's generator and the ring's, so a run reproduces.</param>
    /// <remarks>
    /// <b>THE CARRIED WINDOW IS ON BY DEFAULT BECAUSE THIS WORLD IS ABOUT TIME.</b>
    /// A span of nought writes no <c>after</c> cells at all, and a world built so
    /// that acts have delayed consequences would then hold no record that anything
    /// followed anything — which is how <see cref="Homeostat"/> came to have no
    /// temporal cells without anybody noticing.
    /// </remarks>
    /// <param name="clusters">How many clusters the fabric holds.</param>
    /// <param name="replicas">Ring replicas per cluster.</param>
    public TendingRun(
        TendingSettings world,
        WalkSettings dials,
        int seed,
        int clusters = 8,
        int replicas = 256)
    {
        _fabric = Fabric.Standing(world, dials, seed, clusters, replicas);
        _settings = world;
        _dials = dials;
        Seed = seed;

        // THE BANDS AND THE GRAIN ARE ADAPTER SETTINGS AND LIVE ON THIS SIDE.
        // The world holds moisture as numbers; how finely to say them is a
        // decision about how the brain thinks -- John's line, 2026-08-05.
        _senses = new Compound<Tended>(
        [
            new Banded<Tended>(
                one => one.Damp, Tending.Damp, world.Plants, world.Bands, world.Grains),
            new Marked<Tended>(one => one.At, Tending.Where),
            new Marked<Tended>(one => one.Did, Tending.Did),
        ]);

        _body = _fabric.Watching("gardener", dials, _senses);
    }

    /// <summary>The seed this run was built with.</summary>
    public int Seed { get; }

    /// <summary>Tends the garden for a while.</summary>
    /// <param name="steps">How long to run for.</param>
    /// <param name="choosing">How the body decides what to do.</param>
    /// <param name="ct">Cancellation.</param>
    public async Task<TendingResult> RunAsync(
        int steps,
        Gardening choosing = Gardening.Chain,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(steps);

        var world = new Tending(_settings);
        var rng = new Random(Seed);

        int held = 0, silent = 0, unbalanced = 0, unsettled = 0;
        var doing = new int[Tending.Actions];
        var watered = new int[_settings.Plants];
        var chains = new Chains();

        // THE UNIT IS THE WORLD'S OWN. One step's drying of everything is what
        // breaking even looks like here, so it is what earns the full band.
        var sensing = new Sensing(_settings.Drain * _settings.Plants);

        // THE TRACE, NEWEST LAST, HELD AS THE MACHINE ACTUALLY WROTE EACH ONE.
        // Rebuilding an occasion from the codes gets a NEIGHBOURING occasion --
        // onsets separated from what was already live, the window's carried codes
        // folded in -- and measured elsewhere, that loses to not writing at all.
        //
        // ONE DEEP IS `Credited` EXACTLY, which is what makes the arms comparable:
        // the only thing that moves is how far back the credit reaches.
        var trace = new List<Occasion>();
        var reach = Reaches(choosing, _settings);

        for (var step = 0; step < steps; step++)
        {
            var frame = world.Sensed();
            ImmutableArray<Code> felt = [.. _senses.Codify(frame)];

            // FEEL FIRST, so the credit standing here belongs to the transition
            // that has just happened -- which is the one the held occasion caused.
            sensing.Note(world.At, felt);

            var walked = !Walks(choosing)
                ? (Chosen: (int?)null, Settled: true, Balanced: true)
                : await ChosenAsync(
                    felt,
                    chains,
                    choosing switch
                    {
                        Gardening.Venturing => Question.Worthwhile(),
                        _ when Credits(choosing) => Question.Worthwhile(),
                        _ => null,
                    },
                    ct).ConfigureAwait(false);

            if (!walked.Balanced) unbalanced++;
            if (!walked.Settled) unsettled++;

            // THE MACHINE OVERRULES THE WALK FROM ITS OWN HISTORY -- see
            // Gardening.Venturing. Asked separately because it needs the graph's
            // opinion of EVERY act rather than only its favourite.
            int? ventured = null;

            if (choosing == Gardening.Venturing)
            {
                var weighed = await WeighedAsync(felt, chains, Question.Worthwhile(), ct)
                    .ConfigureAwait(false);

                ventured = Venture(weighed.Scores, doing, step);
                walked = (ventured, weighed.Settled, weighed.Balanced);
            }

            var chose = choosing switch
            {
                Gardening.Idle => (int?)null,
                Gardening.Blind => rng.Next(Tending.Actions),
                Gardening.Best => world.Best(),
                Gardening.Venturing => ventured,
                _ => walked.Chosen,
            };

            // THE BOOTSTRAP, AND IT IS COUNTED. An action enters the graph only by
            // being taken, so a walk that has never seen one cannot propose one.
            // Acting at random where the graph is silent breaks that deadlock and
            // makes the arm say something -- and the count is what stops a mostly
            // random policy reading as the graph's choice.
            if (Walks(choosing) && chose is null)
            {
                silent++;
                chose = rng.Next(Tending.Actions);
            }

            if (chose is { } what && what >= 0 && what < doing.Length) doing[what]++;
            if (chose == 2) watered[world.Standing]++;

            // THE ACT RIDES IN THE FRAME NOW RATHER THAN BEING APPENDED AS A
            // CODE. It is the same code on the same modality -- `Marked` mints
            // what `Tending.Doing` minted -- but the world no longer decides it.
            await _body
                .ObserveAsync(frame with { Did = chose }, step, ct: ct)
                .ConfigureAwait(false);
            await _fabric.QuietAsync(ct).ConfigureAwait(false);

            // AND WHATEVER IS STILL IN THE TRACE COLLECTS A CELL IF THE BODY
            // IMPROVED. The credit standing here belongs to the transition that
            // just happened, and under a trace that transition is credited to
            // everything that led to it rather than to the last step alone.
            //
            // MOST TO THE MOST RECENT, and the weight is `1 / (age + 1)` -- a
            // reciprocal rather than a constant somebody picked, so there is no
            // sixth knob and the newest step still earns exactly the 1.0 that
            // `Credited` writes.
            if (Credits(choosing) && sensing.Credit > 1.0)
            {
                for (var back = 0; back < trace.Count; back++)
                {
                    var age = trace.Count - 1 - back;

                    var worth = choosing == Gardening.Smeared ? 1.0 : 1.0 / (age + 1);

                    await _body
                        .ReinforceAsync(trace[back] with { As = Kind.Helped }, worth, ct)
                        .ConfigureAwait(false);
                }

                await _fabric.QuietAsync(ct).ConfigureAwait(false);
            }

            // THE TRACE FADES BY DROPPING ITS OLDEST, which is what makes it a
            // WINDOW rather than a memory. It is NOT cleared on consolidation:
            // a trace that emptied itself every time credit arrived could never
            // credit two overlapping sequences, and counts only rise anyway.
            if (_body.Joined is { } wrote)
            {
                trace.Add(wrote);
                if (trace.Count > reach) trace.RemoveAt(0);
            }

            if (world.Step(chose)) held++;
        }

        _fabric.Failures();

        return new TendingResult
        {
            Choosing = choosing,
            Steps = steps,
            Held = held,
            Idling = world.Idling,
            Doing = doing,
            Watered = watered,
            States = sensing.States,
            Silent = silent,
            Reflections = Reflections.Of(_dials, 0),
            Plumbing = _fabric.Facts(chains, unbalanced),
            Unsettled = unsettled,
        };
    }

    /// <summary>Whether this arm consults the graph at all.</summary>
    private static bool Walks(Gardening how) =>
        how is not (Gardening.Idle or Gardening.Blind or Gardening.Best);

    /// <summary>Whether this arm writes the credit cell at all.</summary>
    private static bool Credits(Gardening how) =>
        how is Gardening.Credited or Gardening.Traced or Gardening.Smeared
            or Gardening.Venturing;

    /// <summary>
    /// What the graph made of each action, ranked, rather than just its favourite.
    /// </summary>
    /// <remarks>
    /// <b>THE TOP ONE IS NOT ENOUGH FOR A MACHINE THAT MEANS TO OVERRULE IT.</b>
    /// Weighing the walk against a count needs the walk's opinion of EVERY act,
    /// including the ones it thinks little of, and an act it never reached scores
    /// nought rather than being absent.
    /// </remarks>
    private async Task<(IReadOnlyDictionary<int, double> Scores, bool Settled, bool Balanced)>
        WeighedAsync(
            ImmutableArray<Code> felt, Chains chains, Question? asking, CancellationToken ct)
    {
        var thought = await _body
            .ThinkAsync(felt, _dials.Stamina, asking, ct).ConfigureAwait(false);

        var settled = await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);
        chains.Fold(thought.Best(int.MaxValue));

        var scores = thought.BestOf(Tending.Did, int.MaxValue)
            .GroupBy(arrival => Tending.Done(arrival.Endpoint))
            .ToDictionary(one => one.Key, one => one.Max(arrival => arrival.Score));

        var balanced = thought.Balanced();
        _body.Forget(thought.Id);

        return (scores, settled, balanced);
    }

    /// <summary>
    /// The graph's opinion and the machine's own history, added on one scale.
    /// </summary>
    /// <remarks>
    /// <b>BOTH TERMS NORMALISED TO THEIR OWN MAXIMUM</b>, so neither swamps the
    /// other by accident of units and no constant is chosen. See
    /// <see cref="Gardening.Venturing"/>.
    /// </remarks>
    private static int Venture(IReadOnlyDictionary<int, double> scores, int[] tried, int step)
    {
        var bonus = new double[tried.Length];

        for (var act = 0; act < tried.Length; act++)
            bonus[act] = Math.Sqrt(Math.Log(step + 2.0) / (tried[act] + 1.0));

        var believed = scores.Count == 0 ? 1.0 : Math.Max(scores.Values.Max(), double.Epsilon);
        var curious = bonus.Max();

        var best = 0;
        var most = double.MinValue;

        for (var act = 0; act < tried.Length; act++)
        {
            var worth = (scores.GetValueOrDefault(act) / believed) + (bonus[act] / curious);

            if (worth > most) { most = worth; best = act; }
        }

        return best;
    }

    /// <summary>
    /// How far back the credit reaches, in steps.
    /// </summary>
    /// <remarks>
    /// <b>DERIVED FROM THE WORLD AND NEVER SET.</b> The longest thing worth
    /// crediting here is a whole instrumental sequence: cross the garden, water
    /// what is underfoot, and wait a step for it to land. That is
    /// <c>Plants - 1</c> moves plus the pour plus the landing, so the arithmetic
    /// names the number and nobody has to. <b>One is <see cref="Gardening.Credited"/>
    /// exactly</b>, which is what makes the arms comparable.
    /// </remarks>
    private static int Reaches(Gardening how, TendingSettings world) =>
        how is Gardening.Traced or Gardening.Smeared ? world.Plants + 1 : 1;

    /// <summary>One walk, and whether it was in any state to be read.</summary>
    private async Task<(int? Chosen, bool Settled, bool Balanced)> ChosenAsync(
        ImmutableArray<Code> felt, Chains chains, Question? asking, CancellationToken ct)
    {
        var thought = await _body
            .ThinkAsync(felt, _dials.Stamina, asking, ct).ConfigureAwait(false);

        var settled = await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);

        var reached = thought.BestOf(Tending.Did, 1);
        chains.Fold(thought.Best(int.MaxValue));

        var chosen = reached.Count == 0 ? (int?)null : Tending.Done(reached[0].Endpoint);
        var balanced = thought.Balanced();

        _body.Forget(thought.Id);
        return (chosen, settled, balanced);
    }

    public void Dispose() => _fabric.Dispose();
}
