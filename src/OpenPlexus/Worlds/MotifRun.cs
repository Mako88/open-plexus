using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using OpenPlexus.Thinking;

namespace OpenPlexus.Worlds;

/// <summary>
/// What the motif world measured. <b>Counts, not claims.</b>
/// </summary>
public sealed record MotifResult : Questioned
{
    /// <inheritdoc cref="Motif.Compressed"/>
    public required int Compressed { get; init; }

    /// <inheritdoc cref="Motif.Uncompressed"/>
    public required int Uncompressed { get; init; }

    /// <summary>How many recurring sets the world held. <b>Zero is the control.</b></summary>
    public required int Motifs { get; init; }

    /// <summary>Messages the bus carried per question asked.</summary>
    /// <remarks>
    /// <b>THE NUMBER STEP 3 IS ABOUT.</b> Completing a familiar set by walking
    /// costs traffic every single time; a minted node standing for the set would
    /// make it one hop. Accuracy here is only present to show that the compression
    /// would not have cost anything.
    /// </remarks>
    public double Traffic => Asked == 0 ? 0.0 : Messages / (double)Asked;

    /// <inheritdoc/>
    protected override string Shown => "moments";

    /// <inheritdoc/>
    /// <remarks><b>Two — a set's members co-occur, so completing one is a hop.</b></remarks>
    protected override int Composes => 2;

    /// <inheritdoc/>
    protected override string Stalled => "no route walked at all";

    /// <inheritdoc/>
    protected override void Beyond(List<string> wrong)
    {
        ArgumentNullException.ThrowIfNull(wrong);

        // THE ALPHABET GROWS ONLY WHERE SOMETHING IS MEANT TO BE GROWING IT, and
        // this is the check. Chunking is unconditional now, so minting nothing at
        // all is the mechanism failing silently and there is no off arm left to
        // check the other direction against.
        if (Coined == 0)
            wrong.Add("chunking minted nothing at all");

        // AND A DETECTOR THAT MINTS NEARLY EVERYTHING HAS FOUND NO STRUCTURE.
        //
        // THE BOUND WAS `Coined > Motifs` AND THAT WAS A WHOLE-MOMENT ARTEFACT.
        // While only an entire moment could be a candidate, the one legitimate
        // name in this world was the motif itself, so one name per motif was the
        // ceiling. Candidates are sub-moment now -- pair-merging, which composes
        // -- and every recurring PART of a motif is a legitimate name too. A set
        // of size S contains 2^S - S - 1 subsets of size two or more, and that
        // times the motif count is what a perfectly selective detector could mint
        // here. Derived from the world, like the description-length threshold it
        // is checking, and not chosen.
        if (Motifs > 0)
        {
            // `Compressed` IS the sets written as one node each, so it is the
            // motif count times the size and the size falls straight out of it.
            var size = Compressed / Motifs;
            var parts = Motifs * ((1 << size) - size - 1);

            if (Coined > parts)
                wrong.Add($"minted {Coined} names where {Motifs} sets of {size} "
                    + $"hold {parts} recurring parts, so the detector is naming "
                    + "the noise");
        }
    }

    /// <summary>How many distinct codes the world can emit.</summary>
    public required int Symbols { get; init; }

    /// <inheritdoc cref="Learning.Chunk.Coined"/>
    public required int Coined { get; init; }

    /// <inheritdoc cref="Learning.Chunk.Noticed"/>
    /// <remarks>
    /// <b>THE DENOMINATOR, AND IT IS REPORTED BECAUSE CHUNKING IS AN OPEN
    /// QUESTION AS OF 2026-08-05.</b> Minting is unconditional now and costs this
    /// world half its accuracy — 0.8276 with the naming suppressed against 0.4483
    /// with it. Whether that is the detector naming NOISE or naming the right sets
    /// and the walk paying for the hop is exactly the ratio of these two numbers,
    /// and it was not visible from outside.
    /// </remarks>
    public required int Noticed { get; init; }

    public override string ToString() =>
        $"motifs={Motifs} moments={Moments} asked={Asked} right={Right} silent={Silent} | " +
        $"accuracy={Accuracy:F4} chance={Chance:F4} | " +
        $"edges={Edges} compressed={Compressed} uncompressed={Uncompressed} | " +
        $"coined={Coined} noticed={Noticed} | " +
        $"reflect={(Reflecting ? "on" : "off")} wrote={Reflected} | " +
        $"nodes={Nodes} widest={Widest} spread=[{string.Join(",", Spread)}] | " +
        $"chains={{{Plumbing.Lengths}}} deepest={Deepest} | " +
        $"msgs={Messages} traffic={Traffic:F0} halted={Halted} " +
        $"unbalanced={Unbalanced} unsettled={Unsettled}{Wrong}";
}

/// <summary>
/// The motif world, wired to the graph.
/// </summary>
/// <remarks>
/// <b>Shows moments and periodically asks a set to complete itself.</b> Scored
/// prequentially, like every other world here — the question is asked against
/// whatever has been learnt so far and learning never stops.
/// </remarks>
public sealed class MotifRun : IDisposable
{
    private readonly Fabric _fabric;
    private readonly InputMachine<Coded> _eyes;
    private readonly Motif _world;
    private readonly MotifSettings _settings;
    private readonly WalkSettings _dials;

    /// <inheritdoc cref="Learning.Chunk"/>


    /// <param name="world">The stream to watch.</param>
    /// <param name="dials">The walk.</param>
    /// <param name="seed">This run's own generator.</param>
    /// <param name="clusters">How many clusters to stand up.</param>
    /// <param name="replicas">Ring points per cluster.</param>
    public MotifRun(
        MotifSettings world,
        WalkSettings dials,
        int seed,
        int clusters = 8,
        int replicas = 256)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        _world = new Motif(world, seed);
        _settings = world;
        _dials = dials;
        _fabric = new Fabric(dials, seed, clusters, replicas);

        _eyes = _fabric.Watching("eyes", dials);
    }

    /// <inheritdoc cref="Learning.Chunk"/>
    public Chunk Chunks => _eyes.Chunks;

    /// <summary>The world this run is watching.</summary>
    public Motif World => _world;

    /// <summary>
    /// Shows moments, and every <paramref name="every"/> of them asks a set to
    /// complete itself.
    /// </summary>
    public async Task<MotifResult> RunAsync(
        int moments, int every = 10, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(moments);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(every);

        int asked = 0, right = 0, silent = 0, unbalanced = 0, unsettled = 0;
        long halted = 0;

        var reflected = 0;
        var chains = new Chains();

        for (var moment = 0; moment < moments; moment++)
        {
            var (shown, _) = _world.Next();

            var observed = await _eyes.ObserveAsync(Coded.Of(shown), moment, ct: ct).ConfigureAwait(false);
            await _fabric.QuietAsync(ct).ConfigureAwait(false);

            if (observed is not null)
                reflected += await _eyes.ReflectAsync(observed, moment, ct).ConfigureAwait(false);

            if (moment % every != 0 || moment == 0 || _world.Motifs.Count == 0) continue;

            var (cue, wanted) = _world.Ask(moment % _world.Motifs.Count);

            var thought = await _eyes
                .ThinkAsync(cue, _dials.Stamina, null, ct).ConfigureAwait(false);

            var settled = await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);

            // THE CUE IS STRUCK OUT, or a route that walked nowhere answers with
            // what it was just told and every question is trivially wrong.
            var asking = cue.ToHashSet();

            var reached = thought
                .BestOf(Motif.Token, _world.Size)
                .Where(arrival => !asking.Contains(arrival.Endpoint))
                .ToList();

            asked++;
            halted += thought.Halted;
            if (!thought.Balanced()) unbalanced++;
            if (!settled) unsettled++;

            chains.Fold(thought.Best(int.MaxValue));

            if (reached.Count == 0) silent++;
            else if (wanted.Contains(reached[0].Endpoint)) right++;

            _eyes.Forget(thought.Id);
        }

        _fabric.Failures();

        return new MotifResult
        {
            Motifs = _world.Motifs.Count,
            Symbols = _settings.Symbols,
            Moments = moments,
            Asked = asked,
            Right = right,
            Silent = silent,
            Chance = _world.Chance,
            Compressed = _world.Compressed,
            Uncompressed = _world.Uncompressed,
            Reflections = Reflections.Of(_dials, reflected),
            Plumbing = _fabric.Facts(chains, unbalanced),
            Halted = halted,
            Unsettled = unsettled,
            Coined = _eyes.Chunks.Coined,
            Noticed = _eyes.Chunks.Noticed,
        };
    }

    public void Dispose() => _fabric.Dispose();
}
