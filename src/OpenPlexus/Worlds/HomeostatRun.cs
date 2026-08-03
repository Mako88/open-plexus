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

    /// <summary>Steps where the walk had nothing to say and the body did nothing.</summary>
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
        $"msgs={Messages} unbalanced={Unbalanced}{Wrong}";
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

        _body = new InputMachine<ImmutableArray<Code>>(
            new MachineAddress("body"), new Feeling(), new LocalRendezvous(_fabric.Local),
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

        int held = 0, silent = 0, unbalanced = 0;
        var chains = new Chains();

        for (var step = 0; step < steps; step++)
        {
            var felt = world.Feels();

            // WHAT IS FELT AND WHAT IS DONE ARE ONE OCCASION. The action joins the
            // state it was taken in, which is the only way a later walk from a
            // state can reach an action at all.
            var chosen = choosing switch
            {
                Attending.Idle => (int?)null,
                Attending.Blind => rng.Next(world.Needs),
                Attending.Lowest => world.Lowest,
                _ => await ChosenAsync(felt, chains, ct).ConfigureAwait(false),
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
            if (choosing == Attending.Chain && chosen is null)
            {
                silent++;
                chosen = rng.Next(world.Needs);
            }

            ImmutableArray<Code> occasion = chosen is { } which
                ? [.. felt, Homeostat.Attending(which)]
                : felt;

            await _body.ObserveAsync(occasion, step, ct).ConfigureAwait(false);
            await _fabric.QuietAsync(ct).ConfigureAwait(false);

            if (world.Step(chosen)) held++;
        }

        _fabric.Failures();

        return new HomeostatResult
        {
            Choosing = choosing,
            Steps = steps,
            Held = held,
            Silent = silent,
            Idling = world.Idling,
            Plumbing = _fabric.Facts(chains, unbalanced),
        };
    }

    /// <summary>
    /// Walks from what is felt and takes whatever action it ranks first.
    /// </summary>
    /// <remarks>
    /// <b>The same narrowing every other world uses</b> — see
    /// <see cref="Thought.BestOf"/>. Nothing tells the walk what an action does;
    /// it has only ever seen actions beside the states they were taken in.
    /// </remarks>
    private async Task<int?> ChosenAsync(
        ImmutableArray<Code> felt, Chains chains, CancellationToken ct)
    {
        var thought = await _body
            .ThinkAsync(felt, _dials.Stamina, null, ct).ConfigureAwait(false);

        await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);

        var reached = thought.BestOf(Homeostat.Act, 1);
        chains.Fold(thought.Best(int.MaxValue));

        var chosen = reached.Count == 0 ? (int?)null : Homeostat.Attended(reached[0].Endpoint);

        _body.Forget(thought.Id);
        return chosen;
    }

    public void Dispose() => _fabric.Dispose();
}
