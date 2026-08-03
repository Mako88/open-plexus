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
public sealed record BindingResult : Questioned
{
    /// <summary>Whether a colour kept its shape for the life of the world.</summary>
    public required bool Bound { get; init; }

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

    /// <summary>The share of questions where both candidates were in reach.</summary>
    public double Forced => Asked == 0 ? 0.0 : SawBoth / (double)Asked;

    /// <summary>The share of answers that named the queried colour's own concept.</summary>
    public double Echo => Asked == 0 ? 0.0 : Echoed / (double)Asked;

    /// <inheritdoc/>
    protected override string Shown => "scenes";

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Two, because a colour and a shape occur in the same scene</b>, so the
    /// answer is reachable in one hop. That is the opposite of the senses world —
    /// and it is why "the walk was too shallow" is not available as an explanation
    /// for anything measured here.
    /// </remarks>
    protected override int Composes => 2;

    /// <inheritdoc/>
    protected override string Stalled => "no route ever left its origin";

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Load-bearing here more than anywhere else in the project.</b> Every
    /// other world's headline is a number going UP, where a disconnected dial
    /// shows up as a disappointment. This world's prediction is that a number
    /// stays flat — and a broken harness produces exactly that, indistinguishably.
    /// So the complaints are what stands between "measured at chance" and "never
    /// measured anything".
    /// </remarks>
    protected override void Beyond(List<string> wrong)
    {
        ArgumentNullException.ThrowIfNull(wrong);

        // See SawBoth: a chance score means nothing if the choice was never
        // actually forced.
        if (Asked > 0 && SawBoth * 2 < Asked)
            wrong.Add($"only {SawBoth} of {Asked} questions reached both candidates");
    }

    public override string ToString() =>
        $"binding={(Bound ? "stable" : "per-scene")} " +
        $"moments={Moments} asked={Asked} right={Right} silent={Silent} " +
        $"accuracy={Accuracy:F4} chance={Chance:F4} forced={Forced:F4} " +
        $"echo={Echo:F4} swapped={Swapped} | " +
        $"reflect={(Reflecting ? "on" : "off")} wrote={Reflected} | " +
        $"nodes={Nodes} edges={Edges} spread=[{string.Join(",", Spread)}] | " +
        $"chains={{{Plumbing.Lengths}}} deepest={Deepest} | " +
        $"msgs={Messages} halted={Halted} unbalanced={Unbalanced} unsettled={Unsettled}{Wrong}";
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
    private readonly Fabric _fabric;
    private readonly InputMachine<Scene> _eyes;
    private readonly Binding _world;
    private readonly WalkSettings _dials;

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
        _fabric = new Fabric(dials, seed, clusters, replicas);

        _eyes = new InputMachine<Scene>(
            new MachineAddress("scene"), new Passthrough(), new LocalRendezvous(_fabric.Local),
            _fabric.Bus, _fabric.Ring, dials);

        _fabric.Subscribe(_eyes);
    }

    /// <summary>
    /// The codes are already codes; there is nothing to quantise. <b>What it does
    /// do is pass the segmentation through</b> — see
    /// <see cref="Learning.Occasion.Groups"/>.
    /// </summary>
    private sealed class Passthrough : IQuantizer<Scene>
    {
        public byte Modality => Binding.Colour;

        public IReadOnlyCollection<Code> Codify(Scene observation) => observation.Codes;

        public IReadOnlyDictionary<Code, int>? Bind(Scene observation) => observation.Groups;

        public IReadOnlySet<Code>? Fleeting(Scene observation) => observation.Passing;
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
        var chains = new Chains();

        for (var moment = 0; moment < moments; moment++)
        {
            var scene = _world.Next();

            var thought = await _eyes
                .ObserveAsync(scene, moment, ct).ConfigureAwait(false);
            await _fabric.QuietAsync(ct).ConfigureAwait(false);

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

            // `Landed` here means BOTH candidate shapes were in reach, so the
            // forced choice really was forced -- see Answered.Landed.
            if (both) sawBoth++;

            if (!balanced) unbalanced++;
            if (!settled) unsettled++;
            if (scene.Shapes[which] != scene.Colours[which]) swapped++;

            chains.Fold(reached);

            if (answer is null) silent++;
            else if (answer == scene.Shapes[which]) right++;

            if (answer == scene.Colours[which]) echoed++;
        }

        _fabric.Failures();

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
            Reflections = Reflections.Of(_dials, reflected),
            Plumbing = _fabric.Facts(chains, unbalanced),
            Halted = halted,
            Unsettled = unsettled,
        };
    }

    /// <summary>
    /// Asks which shape one object had, several times at once, and takes the
    /// majority.
    /// </summary>
    private async Task<Answered> AskingAsync(
        Scene scene, int which, int votes, CancellationToken ct)
    {
        var asking = new Task<Answered>[votes];
        for (var i = 0; i < votes; i++) asking[i] = OnceAsync(scene, which, ct);

        return Answered.Voted(await Task.WhenAll(asking).ConfigureAwait(false));
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
    private async Task<Answered> OnceAsync(Scene scene, int which, CancellationToken ct)
    {
        // THE QUESTION CARRIES THE INDEX WHEN THE WORLD HANDS ONE OUT. Without
        // it there is no way to say *this one*: an object can only be named by
        // its attributes, and its attributes are what is being asked about. With
        // it, the question is "the object I am pointing at, which is this colour
        // -- what shape is it".
        var colour = _world.Of(Binding.Colour, scene.Colours[which]);

        IReadOnlyCollection<Code> origins = scene.Tags.Count > which
            ? [.. colour, scene.Tags[which]]
            : colour;

        // WHICH ORIGINS ARE ONE THING SAID SEVERAL WAYS. Every code of the
        // colour is the same colour, and the index is something else entirely --
        // so under `Agreement` they are two witnesses rather than four. Without
        // this the colour outvotes the index three to one and the answer is the
        // echo, which is exactly what the index was added to beat.
        var asking = new Dictionary<Code, int>();
        foreach (var code in colour) asking[code] = 0;
        if (scene.Tags.Count > which) asking[scene.Tags[which]] = 1;

        var thought = await _eyes
            // THE GROUPING TRAVELS AND THE RANKING DOES NOT ASK FOR AGREEMENT.
            // This world points with an index and supplies a colour for context,
            // so its origins are not equals — counting how many of them agree is
            // the wrong question here, and measured harmful.
            .ThinkAsync(origins, _dials.Stamina, new Question { Asking = asking }, ct)
            .ConfigureAwait(false);

        var settled = await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);

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

        var report = Answered.From(
            thought, answer, seen.Count == candidates.Count, settled);

        _eyes.Forget(thought.Id);
        return report;
    }

    public void Dispose() => _fabric.Dispose();
}
