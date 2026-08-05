using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using OpenPlexus.Thinking;

namespace OpenPlexus.Worlds;

/// <summary>
/// What CLEVR measured. <b>Counts, not claims.</b>
/// </summary>
public sealed record ClevrResult : Questioned
{
    /// <inheritdoc cref="Refer"/>
    public required Refer Referring { get; init; }

    /// <inheritdoc cref="ClevrSettings.Segmented"/>
    public required bool Segmented { get; init; }

    /// <inheritdoc cref="ClevrSettings.Tagged"/>
    public required bool Tagged { get; init; }

    /// <inheritdoc cref="ClevrSettings.Fleeting"/>
    public required bool Carried { get; init; }

    /// <summary>
    /// How often the walk ranked the RIGHT object's index first among the indexes
    /// it reached.
    /// </summary>
    /// <remarks>
    /// <b>The same split `Composed` had to make, and for the same reason.</b>
    /// Reference and answer are two different failures: a walk can find exactly
    /// the object meant and still rank the wrong attribute of it first, and the
    /// two are indistinguishable in an accuracy alone.
    /// </remarks>
    public required int Found { get; init; }

    /// <inheritdoc cref="Found"/>
    public double Reference => Asked == 0 ? 0.0 : Found / (double)Asked;

    /// <inheritdoc/>
    protected override string Shown => "scenes";

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Three, except on the arm that is handed the index.</b> A question names
    /// attributes and wants an attribute, so the route runs attribute to index to
    /// attribute; hand it the index and the same task is one hop shorter.
    /// </remarks>
    protected override int Composes => Referring == Refer.Index ? 2 : 3;

    /// <inheritdoc/>
    protected override string Stalled => "no route composed anything";

    /// <inheritdoc/>
    protected override void Beyond(List<string> wrong)
    {
        ArgumentNullException.ThrowIfNull(wrong);

        // AN ARM THAT CANNOT REACH AN INDEX HAS NOT RUN THE TASK. Every arm but
        // the supplied-index one has to get to an object before it can say
        // anything about it, and a run where that never once happened produces a
        // chance score that reads exactly like a hard problem.
        if (Asked > 0 && Tagged && Found == 0 && Referring != Refer.Index)
            wrong.Add("no walk ever reached the object it was asking about");

        if (!Tagged && Referring == Refer.Index)
            wrong.Add("the index arm was asked for on a world that mints no indexes");
    }

    public override string ToString() =>
        $"refer={Referring} segmented={Segmented} tagged={Tagged} fleeting={Carried} | " +
        $"scenes={Moments} asked={Asked} right={Right} silent={Silent} | " +
        $"accuracy={Accuracy:F4} chance={Chance:F4} reference={Reference:F4} | " +
        $"reflect={(Reflecting ? "on" : "off")} wrote={Reflected} | " +
        $"nodes={Nodes} edges={Edges} widest={Widest} spread=[{string.Join(",", Spread)}] | " +
        $"chains={{{Plumbing.Lengths}}} deepest={Deepest} | " +
        $"msgs={Messages} halted={Halted} unbalanced={Unbalanced} unsettled={Unsettled}{Wrong}";
}

/// <summary>
/// CLEVR, wired to the graph.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE BINDING WORLD'S ARMS, ON SOMEBODY ELSE'S SCENES.</b> The same
/// <see cref="Refer"/> the composition world uses is reused unchanged rather than
/// copied, so the two are directly comparable: the conjunction is the arm, one
/// attribute alone is the control, the object's own index is the ceiling, and
/// reading the index back is the two-broadcast version.
/// </para>
/// <para>
/// <b>Scored prequentially.</b> A scene is shown, its questions are asked, and
/// the run moves on — there is no training phase, because C4 forbids one.
/// </para>
/// </remarks>
public sealed class ClevrRun : IDisposable
{
    private readonly Fabric _fabric;
    private readonly InputMachine<Sighting> _eyes;
    private readonly Clevr _world;
    private readonly WalkSettings _dials;

    /// <summary>How this world's question wants its candidates ranked.</summary>

    /// <param name="world">How much to read, and which arms are on.</param>
    /// <param name="dials">The walk.</param>
    /// <param name="seed">The ring's seed.</param>
    /// <param name="clusters">How many clusters the codes are spread over.</param>
    /// <param name="replicas">Ring replicas per cluster.</param>
    public ClevrRun(
        ClevrSettings world,
        WalkSettings dials,
        int seed,
        int clusters = 8,
        int replicas = 256)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        _world = new Clevr(world);
        _dials = dials;
        _fabric = new Fabric(dials, seed, clusters, replicas);

        _eyes = new InputMachine<Sighting>(
            new MachineAddress("eyes"), new Looking(), new LocalRendezvous(_fabric.Local),
            _fabric.Bus, _fabric.Ring, dials);

        _fabric.Subscribe(_eyes);
    }

    /// <summary>The world this run is reading.</summary>
    public Clevr World => _world;

    /// <summary>
    /// The front end, which does no vision at all.
    /// </summary>
    /// <remarks>
    /// <b>The scene graph IS the quantised signal.</b> CLEVR ships every object's
    /// colour, size, shape and material already separated, which is exactly the
    /// segmented input <see cref="Occasion.Groups"/> says a retina hands over —
    /// so what is being measured here is what the graph does with segmentation,
    /// not whether segmentation is possible.
    /// </remarks>
    private sealed class Looking : IQuantizer<Sighting>
    {
        public byte Modality => Clevr.Colour;

        public IReadOnlyCollection<Code> Codify(Sighting observation)
        {
            ArgumentNullException.ThrowIfNull(observation);
            return observation.Codes;
        }

        public IReadOnlyDictionary<Code, int>? Bind(Sighting observation)
        {
            ArgumentNullException.ThrowIfNull(observation);
            return observation.Groups;
        }

        public IReadOnlySet<Code>? Fleeting(Sighting observation)
        {
            ArgumentNullException.ThrowIfNull(observation);
            return observation.Fleeting;
        }
    }

    /// <summary>Shows every scene and asks what is asked about it.</summary>
    /// <param name="refer">How a question refers to the object it is about.</param>
    /// <param name="votes">Concurrent walks per question; see <see cref="SensesRun"/>.</param>
    /// <param name="ct">Cancellation.</param>
    public async Task<ClevrResult> RunAsync(
        Refer refer = Refer.Conjunction, int votes = 1, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(votes);

        int asked = 0, right = 0, silent = 0, found = 0, unbalanced = 0, unsettled = 0;
        long halted = 0;

        var reflected = 0;
        var chains = new Chains();
        var at = 0L;

        foreach (var scene in _world.Scenes)
        {
            var observed = await _eyes.ObserveAsync(scene, at++, ct: ct).ConfigureAwait(false);
            await _fabric.QuietAsync(ct).ConfigureAwait(false);

            // REFLECTION SEES WHAT WAS OBSERVED AND NEVER WHAT WAS ASKED, exactly
            // as in every other world here.
            if (observed is not null)
                reflected += await _eyes.ReflectAsync(observed, at, ct).ConfigureAwait(false);

            foreach (var question in _world.About(scene.Scene))
            {
                asked++;

                var answered = await AskingAsync(question, refer, votes, ct).ConfigureAwait(false);

                halted += answered.Halted;
                if (!answered.Balanced) unbalanced++;
                if (!answered.Settled) unsettled++;
                if (answered.Pointed) found++;

                chains.Fold(answered.Reached);

                if (answered.Answer is null) silent++;
                else if (answered.Answer.Value == question.Answer) right++;
            }
        }

        _fabric.Failures();

        return new ClevrResult
        {
            Referring = refer,
            Segmented = _world.Segmented,
            Tagged = _world.Tagged,
            Carried = _world.Fleeting,
            Moments = _world.Scenes.Count,
            Asked = asked,
            Right = right,
            Silent = silent,
            Found = found,
            Chance = _world.Chance,
            Reflections = Reflections.Of(_dials, reflected),
            Plumbing = _fabric.Facts(chains, unbalanced),
            Halted = halted,
            Unsettled = unsettled,
        };
    }

    /// <summary>One question, with the plumbing left attached.</summary>
    private readonly record struct Answering(
        Code? Answer,
        bool Pointed,
        int Halted,
        bool Balanced,
        bool Settled,
        IReadOnlyList<Arrival> Reached);

    /// <summary>Asks several times at once and takes the majority.</summary>
    private async Task<Answering> AskingAsync(
        Referred question, Refer refer, int votes, CancellationToken ct)
    {
        var asking = new Task<Answering>[votes];
        for (var i = 0; i < votes; i++) asking[i] = OnceAsync(question, refer, ct);

        var answers = await Task.WhenAll(asking).ConfigureAwait(false);

        return answers[0] with
        {
            Answer = Majority.Of(answers.Select(one => one.Answer)).Chosen,
            Pointed = answers.Count(one => one.Pointed) * 2 > votes,
            Halted = answers.Sum(one => one.Halted),
            Balanced = answers.All(one => one.Balanced),
            Settled = answers.All(one => one.Settled),
            Reached = [.. answers.SelectMany(one => one.Reached)],
        };
    }

    /// <summary>One walk, or two under <see cref="Refer.Narrowed"/>.</summary>
    private async Task<Answering> OnceAsync(Referred question, Refer refer, CancellationToken ct)
    {
        var origins = Origins(question, refer);

        var thought = await _eyes
            .ThinkAsync(origins, _dials.Stamina, Asking(origins), ct).ConfigureAwait(false);

        var settled = await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);

        // AN ORIGIN PRODUCES NO ARRIVAL, so an index the question was handed
        // counts as pointed at -- otherwise the ceiling arm reads as never having
        // found the object it was given.
        var pointing = thought.BestOf(Clevr.Object, 1);

        var pointed = origins.Contains(question.Tag)
            || (pointing.Count > 0 && pointing[0].Endpoint == question.Tag);

        if (refer == Refer.Narrowed)
            return await AgainAsync(thought, question, pointing, pointed, settled, ct)
                .ConfigureAwait(false);

        var reached = thought.BestOf(question.Asking, 1);

        var answered = new Answering(
            reached.Count == 0 ? null : reached[0].Endpoint,
            pointed,
            thought.Halted,
            thought.Balanced(),
            settled,
            thought.Best(int.MaxValue));

        _eyes.Forget(thought.Id);
        return answered;
    }

    /// <summary>
    /// Asks again, from whichever index the first walk ranked first.
    /// </summary>
    /// <remarks>
    /// <b>The index is READ, never supplied</b> — see <see cref="Refer.Narrowed"/>.
    /// A first walk that reached no index at all has nothing to ask and says so,
    /// rather than falling back on the conjunction.
    /// </remarks>
    private async Task<Answering> AgainAsync(
        Thought first,
        Referred question,
        IReadOnlyList<Arrival> pointing,
        bool pointed,
        bool settled,
        CancellationToken ct)
    {
        _eyes.Forget(first.Id);

        if (pointing.Count == 0)
            return new Answering(null, pointed, first.Halted, first.Balanced(), settled, []);

        var second = await _eyes
            .ThinkAsync([pointing[0].Endpoint], _dials.Stamina, null, ct).ConfigureAwait(false);

        var closed = await _fabric.SettleAsync(second, ct).ConfigureAwait(false);
        var reached = second.BestOf(question.Asking, 1);

        var answered = new Answering(
            reached.Count == 0 ? null : reached[0].Endpoint,
            pointed,
            first.Halted + second.Halted,
            first.Balanced() && second.Balanced(),
            settled && closed,
            second.Best(int.MaxValue));

        _eyes.Forget(second.Id);
        return answered;
    }

    /// <summary>
    /// What the question broadcasts. <b>The only thing an arm changes.</b>
    /// </summary>
    /// <remarks>
    /// <b>The scene is in every arm but the ceiling</b>, because somebody asking
    /// about <i>the big metal thing</i> is looking at a picture while they ask —
    /// see <see cref="Clevr.Where"/>. The ceiling arm is handed the object itself
    /// and has no use for it.
    /// </remarks>
    private static ImmutableArray<Code> Origins(Referred question, Refer refer)
    {
        var here = Clevr.Seen(question.Scene);

        return refer switch
        {
            Refer.Conjunction or Refer.Narrowed => [here, .. question.Origins],

            Refer.Single => [here, question.Origins[0]],

            // The index the question is not supposed to have. If even this fails,
            // the second hop is broken and the reference was never the problem.
            Refer.Index => [question.Tag],

            _ => [here, .. question.Origins],
        };
    }

    /// <summary>
    /// Which origins are one thing said several ways.
    /// </summary>
    /// <remarks>
    /// <b>Modality is the key, exactly as in the composition world.</b> Each
    /// filter names a different attribute, so under
    /// <see cref="Accumulate.Agreement"/> they are the independent witnesses a
    /// conjunction is counting — and two filters on one attribute, if the corpus
    /// ever produced them, would rightly count once.
    /// </remarks>
    private Question Asking(ImmutableArray<Code> origins) => new()
    {
        Ranking = _dials.Ranking,
        Asking = origins.ToDictionary(code => code, code => (int)code.Modality),
    };

    public void Dispose() => _fabric.Dispose();
}
