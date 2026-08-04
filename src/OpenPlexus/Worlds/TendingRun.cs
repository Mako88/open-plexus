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
    private readonly InputMachine<ImmutableArray<Code>> _body;
    private readonly WalkSettings _dials;
    private readonly int _span;

    /// <param name="world">The shape of the garden.</param>
    /// <param name="dials">The walk.</param>
    /// <param name="seed">The world's generator and the ring's, so a run reproduces.</param>
    /// <param name="span">
    /// How many moments a departed code is carried for. <b>One by default, and
    /// that is a departure from every other world here.</b>
    /// </param>
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
        int span = 1,
        int clusters = 8,
        int replicas = 256)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        _settings = world;
        _dials = dials;
        _span = span;
        Seed = seed;
        _fabric = new Fabric(dials, seed, clusters, replicas);

        _body = new InputMachine<ImmutableArray<Code>>(
            new MachineAddress("gardener"), new Feeling(), new LocalRendezvous(_fabric.Local),
            _fabric.Bus, _fabric.Ring, dials, span);

        _fabric.Subscribe(_body);
    }

    /// <summary>The seed this run was built with.</summary>
    public int Seed { get; }

    /// <summary>The codes are already codes; there is nothing to quantise.</summary>
    private sealed class Feeling : IQuantizer<ImmutableArray<Code>>
    {
        public byte Modality => Tending.Damp;

        public IReadOnlyCollection<Code> Codify(ImmutableArray<Code> observation) => observation;
    }

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

        // WHAT IS OWED A SECOND CELL, held as the machine actually wrote it.
        Occasion? crediting = null;

        for (var step = 0; step < steps; step++)
        {
            var felt = world.Feels();

            // FEEL FIRST, so the credit standing here belongs to the transition
            // that has just happened -- which is the one the held occasion caused.
            sensing.Note(world.At, felt);

            var walked = !Walks(choosing)
                ? (Chosen: (int?)null, Settled: true, Balanced: true)
                : await ChosenAsync(
                    felt,
                    chains,
                    choosing == Gardening.Credited ? Question.Worthwhile() : null,
                    ct).ConfigureAwait(false);

            if (!walked.Balanced) unbalanced++;
            if (!walked.Settled) unsettled++;

            var chose = choosing switch
            {
                Gardening.Idle => (int?)null,
                Gardening.Blind => rng.Next(Tending.Actions),
                Gardening.Best => world.Best(),
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

            ImmutableArray<Code> occasion = chose is { } did
                ? [.. felt, Tending.Doing(did)]
                : felt;

            await _body.ObserveAsync(occasion, step, ct: ct).ConfigureAwait(false);
            await _fabric.QuietAsync(ct).ConfigureAwait(false);

            // AND THE PREVIOUS MOMENT COLLECTS A SECOND CELL IF IT EARNED ONE.
            // Identical to `Homeostat`'s `Credited`, on purpose: the arm is the
            // one already measured and only the world is new.
            if (choosing == Gardening.Credited
                && crediting is { } earned && sensing.Credit > 1.0)
            {
                await _body
                    .ReinforceAsync(earned with { As = Kind.Helped }, 1.0, ct)
                    .ConfigureAwait(false);

                await _fabric.QuietAsync(ct).ConfigureAwait(false);
            }

            crediting = _body.Joined;

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
