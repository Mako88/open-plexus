using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using OpenPlexus.Thinking;

namespace OpenPlexus.Worlds;

/// <summary>
/// How a question refers to the object it is about.
/// </summary>
/// <remarks>
/// <b>Controls that change ONE thing.</b> The world is identical under all
/// three; only what the question broadcasts moves, so the graph learns the same
/// thing and the arms differ in nothing but the reference.
/// </remarks>
public enum Refer
{
    /// <summary>
    /// The conjunction — <i>the one that was A and also B</i>. <b>The arm.</b>
    /// </summary>
    Conjunction,

    /// <summary>
    /// One attribute alone. <b>THE SHARPEST CONTROL OF THE SET.</b>
    /// </summary>
    /// <remarks>
    /// One attribute reaches every index it has ever appeared beside, each on a
    /// single count, so it cannot say <i>which occasion</i> — and it should
    /// therefore pick between their answers at something close to chance, falling
    /// further as a longer run gives it more indexes to confuse.
    /// </remarks>
    Single,

    /// <summary>
    /// The index itself, which the world knows and the question is not supposed
    /// to have. <b>THE CEILING</b>, and the arm that says the walk could compose
    /// this at all if only it could refer.
    /// </summary>
    Index,

    /// <summary>
    /// Broadcast the conjunction, read back <b>whichever index the graph itself
    /// ranked first</b>, and ask that one. Two broadcasts, no index supplied.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>MEASURED, AND IT IS WHAT THE ONE-BROADCAST ARM CANNOT DO.</b> The
    /// conjunction picks the right index almost every time, and an index handed
    /// straight to the walk answers perfectly — yet asking with the conjunction
    /// scores far below both. The evidence that selects the index lives in the
    /// origin's tally FOR THAT INDEX and never travels through it: two routes
    /// arriving at one node fire it twice and fan out independently, so the
    /// support that made it the winner is not carried onward.
    /// </para>
    /// <para>
    /// <b>The machine supplies no knowledge here, only a second question.</b> It
    /// does not know which index is right; it reads the one the graph ranked
    /// first, exactly as <see cref="Thought.BestOf"/> already reads an action or
    /// a prediction. That is "arrival narrows" applied twice — and two-stage
    /// reference is what a visual index is FOR, in Pylyshyn's account: point
    /// first, interrogate second.
    /// </para>
    /// <para>
    /// <b>The honest objection, said out loud:</b> the composition is then split
    /// across two broadcasts with the machine holding the referent in between,
    /// so this is the SYSTEM composing rather than a single walk composing. What
    /// it costs is a round trip; what it is not is the harness knowing the
    /// answer.
    /// </para>
    /// </remarks>
    Narrowed,
}

/// <summary>
/// What the composition world measured. <b>Counts, not claims.</b>
/// </summary>
public sealed record ComposedResult : Questioned
{
    /// <summary>How the question referred to its object.</summary>
    public required Refer Referring { get; init; }

    /// <summary>
    /// How often the walk ranked the RIGHT index first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE MECHANISM, RATHER THAN THE SCORE, AND IT SEPARATES TWO FAILURES
    /// THAT LOOK IDENTICAL.</b> A wrong answer can mean the reference failed to
    /// single the object out, or that it singled it out and the second hop went
    /// astray. Only this tells them apart — and measured, it is the reference
    /// that fails: under <see cref="Refer.Narrowed"/> the score lands almost
    /// exactly on this number, so the second stage loses next to nothing.
    /// </para>
    /// <para>
    /// <b>Ranked first, never merely reached.</b> See
    /// <see cref="ComposedRun"/> — a conjunction reaches every index either
    /// attribute has ever met, so "was it in there" is near-perfect and says
    /// nothing at all.
    /// </para>
    /// </remarks>
    public required int Found { get; init; }

    /// <inheritdoc cref="Composed.Chance"/>
    public required int Values { get; init; }

    /// <summary>The share of questions whose walk reached the right index.</summary>
    public double Reference => Asked == 0 ? 0.0 : Found / (double)Asked;

    /// <inheritdoc/>
    protected override string Shown => "scenes";

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Three, and it is the whole task.</b> An attribute reaches the answer
    /// only through the index, so a correct answer is a two-hop chain. Anything
    /// shallower did not compose anything, whatever it scored.
    /// </remarks>
    protected override int Composes => 3;

    /// <inheritdoc/>
    protected override string Stalled => "no route composed anything";

    /// <inheritdoc/>
    protected override void Beyond(List<string> wrong)
    {
        ArgumentNullException.ThrowIfNull(wrong);

        // THE ONE THAT IS SPECIFIC TO THIS WORLD, and it is the reason the index
        // arm exists. If no walk ever reached an index, nothing was composed and
        // a chance score says only that the harness never ran the task.
        if (Asked > 0 && Found == 0)
            wrong.Add("no walk ever reached the index it was asking about");
    }

    public override string ToString() =>
        $"refer={Referring} scenes={Moments} asked={Asked} right={Right} silent={Silent} " +
        $"accuracy={Accuracy:F4} chance={Chance:F4} reference={Reference:F4} | " +
        $"reflect={(Reflecting ? "on" : "off")} wrote={Reflected} | " +
        $"nodes={Nodes} edges={Edges} widest={Widest} spread=[{string.Join(",", Spread)}] | " +
        $"chains={{{Plumbing.Lengths}}} deepest={Deepest} | " +
        $"msgs={Messages} halted={Halted} unbalanced={Unbalanced} unsettled={Unsettled}{Wrong}";
}

/// <summary>
/// The composition world, wired to the graph.
/// </summary>
/// <remarks>
/// <para>
/// <b>Scored prequentially</b>, like every other world here: three moments are
/// shown, a question is asked about them, and then learning carries on.
/// </para>
/// <para>
/// <b>THE TEMPORAL WINDOW MUST STAY SHUT, AND THAT IS LOAD-BEARING RATHER THAN A
/// DEFAULT.</b> The window carries a departed code forward so it can record what
/// followed it — which here would write <c>A → B</c> and <c>B → C</c> directly,
/// because those moments are consecutive. The task would become a two-hop lookup
/// along observed edges and every number would be measuring that instead. So the
/// machine is built with no span, and a test asserts A and C are never joined.
/// </para>
/// </remarks>
public sealed class ComposedRun : IDisposable
{
    private readonly Fabric _fabric;
    private readonly InputMachine<Moment> _eyes;
    private readonly Composed _world;
    private readonly WalkSettings _dials;

    /// <summary>How this world's question wants its candidates ranked.</summary>
    private readonly Accumulate _ranking;

    /// <param name="world">The world's shape.</param>
    /// <param name="dials">The walk.</param>
    /// <param name="seed">The world's generator and the ring's.</param>
    /// <param name="ranking">
    /// How this world's question wants its candidates ranked. <b>A conjunction by
    /// default, because that is what this world asks</b> — the parameter exists so
    /// a test can show that asking otherwise costs the result.
    /// </param>
    /// <param name="clusters">How many clusters the codes are spread over.</param>
    /// <param name="replicas">Ring replicas per cluster.</param>
    public ComposedRun(
        ComposedSettings world,
        WalkSettings dials,
        int seed,
        Accumulate ranking = Accumulate.Agreement,
        int clusters = 8,
        int replicas = 256)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        _world = new Composed(world, seed);
        _dials = dials;
        _ranking = ranking;
        _fabric = new Fabric(dials, seed, clusters, replicas);

        _eyes = new InputMachine<Moment>(
            new MachineAddress("scene"), new Passthrough(), new LocalRendezvous(_fabric.Local),
            _fabric.Bus, _fabric.Ring, dials);

        _fabric.Subscribe(_eyes);
    }

    /// <summary>One moment of a scene, carrying the scene's segmentation with it.</summary>
    public readonly record struct Moment(
        IReadOnlyCollection<Code> Codes,
        IReadOnlyDictionary<Code, int>? Groups);

    /// <summary>
    /// The codes are already codes; there is nothing to quantise. <b>What it does
    /// do is pass the segmentation through.</b>
    /// </summary>
    /// <remarks>
    /// <b>It does NOT declare the indexes fleeting, and that is the difference
    /// from the binding world.</b> There the question carried the index, so the
    /// walk began at one and the edge back into the attribute's row was dead
    /// weight. Here the question deliberately withholds the index, so
    /// <c>attribute → index</c> is the hop the whole task runs through. The row
    /// growth that <see cref="Occasion.Fleeting"/> removes there is unavoidable
    /// here, and bounding it needs eviction rather than omission.
    /// </remarks>
    private sealed class Passthrough : IQuantizer<Moment>
    {
        public byte Modality => Composed.First;

        public IReadOnlyCollection<Code> Codify(Moment observation) => observation.Codes;

        public IReadOnlyDictionary<Code, int>? Bind(Moment observation) => observation.Groups;
    }

    /// <summary>
    /// Shows scenes, and every <paramref name="every"/> of them asks what the
    /// third attribute of one object was.
    /// </summary>
    /// <param name="scenes">How many scenes to show.</param>
    /// <param name="refer">How the question refers to its object.</param>
    /// <param name="every">Ask on every nth scene.</param>
    /// <param name="votes">How many concurrent thoughts settle one question.</param>
    /// <param name="ct">Cancellation.</param>
    public async Task<ComposedResult> RunAsync(
        int scenes,
        Refer refer = Refer.Conjunction,
        int every = 10,
        int votes = 3,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(scenes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(every);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(votes);

        int asked = 0, right = 0, silent = 0, found = 0, unbalanced = 0, unsettled = 0;
        long halted = 0;

        var reflected = 0;
        var chains = new Chains();
        var at = 0L;

        for (var scene = 0; scene < scenes; scene++)
        {
            var episode = _world.Next();

            foreach (var codes in episode.Moments)
            {
                var thought = await _eyes
                    .ObserveAsync(new Moment(codes, episode.Groups), at++, ct: ct)
                    .ConfigureAwait(false);

                await _fabric.QuietAsync(ct).ConfigureAwait(false);

                // Reflection sees what was OBSERVED and never what was asked, or
                // it would teach the graph the test.
                if (thought is not null)
                    reflected += await _eyes
                        .ReflectAsync(thought, at, ct).ConfigureAwait(false);
            }

            if (scene % every != 0 || scene == 0) continue;

            // ALTERNATING, so nothing rests on which object happened to be drawn
            // first. Both are asked about equally often.
            var which = asked % Composed.PerScene;

            var answer = await AskingAsync(episode, which, refer, votes, ct)
                .ConfigureAwait(false);

            asked++;
            halted += answer.Halted;
            if (!answer.Balanced) unbalanced++;
            if (!answer.Settled) unsettled++;
            if (answer.Landed) found++;

            chains.Fold(answer.Reached);

            if (answer.Named is null) silent++;
            else if (answer.Named == episode.Values[^1][which]) right++;
        }

        _fabric.Failures();

        return new ComposedResult
        {
            Moments = scenes,
            Asked = asked,
            Right = right,
            Silent = silent,
            Found = found,
            Chance = _world.Chance,
            Values = _world.Values,
            Referring = refer,
            Reflections = Reflections.Of(_dials, reflected),
            Plumbing = _fabric.Facts(chains, unbalanced),
            Halted = halted,
            Unsettled = unsettled,
        };
    }

    /// <summary>Asks several times at once and takes the majority.</summary>
    private async Task<Answered> AskingAsync(
        Episode episode, int which, Refer refer, int votes, CancellationToken ct)
    {
        var asking = new Task<Answered>[votes];
        for (var i = 0; i < votes; i++) asking[i] = OnceAsync(episode, which, refer, ct);

        return Answered.Voted(await Task.WhenAll(asking).ConfigureAwait(false));
    }

    /// <summary>
    /// One walk: broadcast the reference, and see which third-attribute value it
    /// ranks first.
    /// </summary>
    /// <remarks>
    /// <b>Ranked over the whole alphabet, not forced between the two present.</b>
    /// See the note on <see cref="Composed"/> — a forced choice would hand the
    /// one-attribute control a lean it has not earned.
    /// </remarks>
    private async Task<Answered> OnceAsync(
        Episode episode, int which, Refer refer, CancellationToken ct)
    {
        var origins = Origins(episode, which, refer);

        var thought = await _eyes
            .ThinkAsync(origins, _dials.Stamina, Asking(origins, _ranking), ct)
            .ConfigureAwait(false);

        var settled = await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);

        // THE SECOND BROADCAST, AND THE FIRST ONE IS DISCARDED EXCEPT FOR THE
        // INDEX IT NAMED. See Refer.Narrowed: the graph chose the referent, and
        // this asks it the question the conjunction could not carry through.
        if (refer == Refer.Narrowed)
            return await AgainAsync(thought, episode, which, settled, ct)
                .ConfigureAwait(false);

        var reached = thought.BestOf(Composed.Third, 1);

        var report = Answered.From(
            thought,
            reached.Count == 0 ? null : Composed.Value(reached[0].Endpoint),
            Points(thought, episode, which),
            settled);

        _eyes.Forget(thought.Id);
        return report;
    }

    /// <summary>
    /// Whether the walk ranked the RIGHT index first among the indexes it
    /// reached.
    /// </summary>
    /// <remarks>
    /// <b>RANKED FIRST, NOT MERELY REACHED, AND THE DIFFERENCE IS THE WHOLE
    /// FINDING.</b> Counting "the right index turned up somewhere in the
    /// arrivals" reads near-perfect and means nothing — a conjunction reaches
    /// every index either attribute ever met, so of course the right one is in
    /// there. What decides the answer is which one comes out on top, and that is
    /// far rarer. Measuring the first and reporting it as the second made the
    /// reference look solved when it is the entire deficit.
    /// <para>
    /// <b>An origin produces no arrival</b>, so an index the question was handed
    /// counts as chosen — otherwise the ceiling arm reads as never finding the
    /// index it was given.
    /// </para>
    /// </remarks>
    private static bool Points(Thought thought, Episode episode, int which)
    {
        if (episode.Tags.Count <= which) return false;

        var wanted = episode.Tags[which];
        if (thought.Started.Contains(wanted)) return true;

        var pointing = thought.BestOf(Composed.Tag, 1);
        return pointing.Count > 0 && pointing[0].Endpoint == wanted;
    }

    /// <summary>
    /// Asks again, from whichever index the first walk ranked first.
    /// </summary>
    /// <remarks>
    /// <b>The index is READ, never supplied.</b> Nothing here knows which one is
    /// right — <see cref="Thought.BestOf"/> is the same narrowing the other
    /// worlds use to pick an action or a prediction, pointed at the index
    /// modality. A first walk that reached no index at all has nothing to ask, and
    /// says so rather than falling back on the conjunction.
    /// </remarks>
    private async Task<Answered> AgainAsync(
        Thought first, Episode episode, int which, bool settled, CancellationToken ct)
    {
        var pointing = first.BestOf(Composed.Tag, 1);
        var chosen = pointing.Count == 0 ? (Code?)null : pointing[0].Endpoint;
        var right = Points(first, episode, which);

        _eyes.Forget(first.Id);

        if (chosen is not { } index) return new Answered(null, false, 0, true, settled, []);

        var second = await _eyes
            .ThinkAsync([index], _dials.Stamina, null, ct).ConfigureAwait(false);

        var closed = await _fabric.SettleAsync(second, ct).ConfigureAwait(false);
        var reached = second.BestOf(Composed.Third, 1);

        var report = Answered.From(
            second,
            reached.Count == 0 ? null : Composed.Value(reached[0].Endpoint),
            right,
            settled && closed);

        _eyes.Forget(second.Id);
        return report;
    }

    /// <summary>
    /// Which origins are one thing said several ways.
    /// </summary>
    /// <remarks>
    /// <b>An attribute is one witness however many codes name it.</b> The
    /// question broadcasts every code of the value it names, because the walk has
    /// to reach whichever one the scene happened to show — and under
    /// <see cref="Accumulate.Agreement"/> those must count once, or a conjunction
    /// of two attributes reads as six independent witnesses and the arithmetic
    /// that makes it a conjunction stops working. Modality is exactly the right
    /// key: it is what distinguishes A from B from the index.
    /// </remarks>
    /// <remarks>
    /// <b>THE RANKING IS PART OF THE QUESTION HERE, NOT OF THE MACHINE.</b> This
    /// world asks a conjunction, so it says so; the arm exists only because a test
    /// has to be able to show that saying so is what makes the difference.
    /// </remarks>
    private static Question Asking(IReadOnlyCollection<Code> origins, Accumulate ranking) =>
        new()
        {
            Ranking = ranking,
            Asking = origins.ToDictionary(code => code, code => (int)code.Modality),
        };

    /// <summary>What the question broadcasts. <b>The only thing an arm changes.</b></summary>
    private IReadOnlyCollection<Code> Origins(Episode episode, int which, Refer refer)
    {
        var first = _world.Of(Composed.First, episode.Values[0][which]);

        return refer switch
        {
            // THE CONJUNCTION. Both reach the asked-about object's index and only
            // that one reaches it twice, so under `Sum` it outscores every index
            // either attribute met on its own.
            Refer.Conjunction or Refer.Narrowed =>
                [.. first, .. _world.Of(Composed.Second, episode.Values[1][which])],

            Refer.Single => first,

            // The index the question is not supposed to have. If even this fails,
            // the second hop is broken and the conjunction was never the problem.
            Refer.Index => episode.Tags.Count > which ? [episode.Tags[which]] : first,

            _ => first,
        };
    }

    public void Dispose() => _fabric.Dispose();
}
