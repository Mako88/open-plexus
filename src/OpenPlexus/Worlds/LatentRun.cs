using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Machines;
using OpenPlexus.Thinking;

namespace OpenPlexus.Worlds;

/// <summary>What the latent world measured. <b>Counts, not claims.</b></summary>
public sealed record LatentResult : Questioned
{
    /// <inheritdoc cref="LatentSettings.Channels"/>
    public required int Channels { get; init; }

    /// <inheritdoc cref="LatentSettings.Causes"/>
    public required int Causes { get; init; }

    /// <summary>
    /// How many questions found a group of origins all reaching each other.
    /// </summary>
    /// <remarks>
    /// <b>THE NUMBER THIS WORLD EXISTS FOR.</b> See
    /// <see cref="Thought.Grouped"/>: the channels are driven by one hidden thing,
    /// so they co-occur constantly and a walk from several of them should find them
    /// mutually reachable. Nought here means the candidate a posited hub would be
    /// minted over is never found, and every cheaper-by-arithmetic argument about
    /// hubs is moot.
    /// </remarks>
    public required int Found { get; init; }

    /// <summary>The share of questions that found one.</summary>
    public double Grouping => Asked == 0 ? 0.0 : Found / (double)Asked;

    /// <summary>
    /// How many row entries the minting dropped because a hub now stands for them.
    /// </summary>
    /// <remarks>
    /// <b>NOUGHT WITH THE ARM OFF, AND NOUGHT WITH IT ON MEANS IT MINTED
    /// NOTHING.</b> Counts only ever rise, so a hub added beside the clique it
    /// replaces makes every row WIDER -- this is the number that says the
    /// description-length argument was actually carried out rather than half of it.
    /// </remarks>
    public required int Subsumed { get; init; }

    /// <summary>Messages the bus carried per question asked.</summary>
    /// <remarks>
    /// <b>THE COST A HUB WOULD ATTACK, and the reason accuracy is not the headline
    /// here.</b> Every moment writes <c>k(k-1)</c> row entries where a hub would
    /// write <c>k</c>, and what a thought costs is set by the widest row — see
    /// <see cref="Motif.Compressed"/> for the same argument over sets.
    /// </remarks>
    public double Traffic => Asked == 0 ? 0.0 : Messages / (double)Asked;

    /// <inheritdoc/>
    protected override string Shown => "moments";

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Two, because the channels co-occur.</b> One shown channel reaches the
    /// hidden one in a single hop, which is exactly why this world's claim is about
    /// COST rather than about whether the answer is reachable.
    /// </remarks>
    protected override int Composes => 2;

    /// <inheritdoc/>
    protected override string Stalled => "no route left its origin";

    /// <inheritdoc/>
    protected override void Beyond(List<string> wrong)
    {
        ArgumentNullException.ThrowIfNull(wrong);

        // THE WORLD HAS TO BE ANSWERABLE AT ALL, or its cost numbers describe a
        // walk that was never doing the task.
        if (Asked > 0 && Right == 0)
            wrong.Add("no question was ever answered, so the cost is of nothing");

        // AND THE GROUP HAS TO BE FINDABLE, which is the whole point of this world.
        // Every channel reports the same hidden state, so a walk from several of
        // them that never finds them mutually reachable has not run the task.
        if (Asked > 0 && Found == 0)
            wrong.Add("no question ever found a group, so a hub has no candidate here");
    }

    public override string ToString() =>
        $"channels={Channels} causes={Causes} moments={Moments} asked={Asked} " +
        $"right={Right} silent={Silent} | accuracy={Accuracy:F4} chance={Chance:F4} " +
        $"grouping={Grouping:F4} subsumed={Subsumed} divides={Divides} | " +
        $"nodes={Nodes} edges={Edges} widest={Widest} | " +
        $"msgs={Messages} traffic={Traffic:F0} halted={Halted} " +
        $"unbalanced={Unbalanced} unsettled={Unsettled}{Wrong}";
}

/// <summary>
/// The latent world, wired to the graph.
/// </summary>
/// <remarks>
/// <b>Scored prequentially</b>, like every other world here: a moment is shown, a
/// question is asked against whatever has been learnt so far, and learning never
/// stops. C4 forbids a training phase.
/// </remarks>
public sealed class LatentRun : IDisposable
{
    private readonly Fabric _fabric;
    private readonly InputMachine<Coded> _eyes;
    private readonly Latent _world;
    private readonly WalkSettings _dials;

    /// <param name="world">How many causes, and how many channels.</param>
    /// <param name="dials">The walk.</param>
    /// <param name="seed">This run's own generator.</param>
    /// <param name="clusters">How many clusters to stand up.</param>
    /// <param name="replicas">Ring points per cluster.</param>
    public LatentRun(
        LatentSettings world,
        WalkSettings dials,
        int seed,
        int clusters = 8,
        int replicas = 256)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        _world = new Latent(world, seed);
        _dials = dials;
        _fabric = new Fabric(dials, seed, clusters, replicas);

        _eyes = _fabric.Watching("channels", dials);
    }

    /// <summary>
    /// Shows moments, and every <paramref name="every"/> of them hides one channel
    /// and asks what it showed.
    /// </summary>
    /// <param name="moments">How many moments to show.</param>
    /// <param name="every">Ask on every nth moment.</param>
    /// <param name="posit">
    /// Whether a group found by a question is minted as a hub, and the edges it
    /// stands for dropped. <b>Off is the control and every measurement taken before
    /// this existed.</b> See <see cref="InputMachine{TFrame}.PositAsync"/>.
    /// </param>
    /// <param name="ct">Cancellation.</param>
    public async Task<LatentResult> RunAsync(
        int moments, int every = 10, bool posit = false, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(moments);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(every);

        int asked = 0, right = 0, silent = 0, found = 0, unbalanced = 0, unsettled = 0;
        long halted = 0;

        // HOW MANY SUBSUMED ENTRIES THE MINTING DROPPED. See LatentResult.Subsumed.
        var subsumed = 0;

        var chains = new Chains();

        for (var moment = 0; moment < moments; moment++)
        {
            var (cause, shown) = _world.Moment();

            await _eyes.ObserveAsync(Coded.Of(shown), moment, ct: ct).ConfigureAwait(false);
            await _fabric.QuietAsync(ct).ConfigureAwait(false);

            if (moment % every != 0 || moment == 0) continue;

            // HIDE ONE CHANNEL AND ASK WHAT IT SHOWED. The others are the origins,
            // and they are the group a hub would be minted over.
            var hidden = moment % _world.Channels;

            var origins = shown
                .Where((_, channel) => channel != hidden)
                .ToImmutableArray();

            // EVERY CODE THAT CHANNEL COULD HAVE SHOWN. The answer is narrowed to
            // one channel's alphabet, exactly as `Babi` narrows to the answers it
            // has seen -- a walk that returned an origin would otherwise "answer"
            // with what it was just told.
            var candidates = Enumerable
                .Range(0, _world.Causes)
                .Select(one => Latent.Shows(hidden, one))
                .ToHashSet();

            var thought = await _eyes
                .ThinkAsync(origins, _dials.Stamina, null, ct).ConfigureAwait(false);

            var settled = await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);

            asked++;
            halted += thought.Halted;
            if (!thought.Balanced()) unbalanced++;
            if (!settled) unsettled++;

            chains.Fold(thought.Best(int.MaxValue));
            chains.Divided(thought.Divides);

            // THE CANDIDATE FOR A POSITED HUB, COUNTED. See LatentResult.Found.
            if (!thought.Grouped().IsEmpty) found++;

            // AND MINTED, WHEN THE ARM IS ON. The decision is the machine's -- this
            // world only says when a thought is finished being read.
            if (posit)
                subsumed += await _eyes
                    .PositAsync(thought, moment, ct).ConfigureAwait(false);

            var reached = thought.BestAmong(candidates, 1);

            if (reached.Count == 0) silent++;
            else if (reached[0].Endpoint == Latent.Shows(hidden, cause)) right++;

            _eyes.Forget(thought.Id);
        }

        _fabric.Failures();

        return new LatentResult
        {
            Channels = _world.Channels,
            Causes = _world.Causes,
            Moments = moments,
            Asked = asked,
            Right = right,
            Silent = silent,
            Found = found,
            Subsumed = subsumed,
            Chance = _world.Chance,
            Divides = chains.Divides,
            Reflections = Reflections.Of(_dials, 0),
            Plumbing = _fabric.Facts(chains, unbalanced),
            Halted = halted,
            Unsettled = unsettled,
        };
    }

    public void Dispose() => _fabric.Dispose();
}
