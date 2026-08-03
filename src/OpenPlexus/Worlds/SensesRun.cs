using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;

namespace OpenPlexus.Worlds;

/// <summary>
/// What the senses world measured. <b>Counts, not claims.</b>
/// </summary>
public sealed record SensesResult
{
    /// <summary>Moments shown.</summary>
    public required int Moments { get; init; }

    /// <summary>Questions asked — a sight, answered with a touch.</summary>
    public required int Asked { get; init; }

    /// <summary>Of those, how many named the right concept's touch first.</summary>
    public required int Right { get; init; }

    /// <summary>Of those, how many the graph had nothing at all to say about.</summary>
    public required int Silent { get; init; }

    /// <summary>What a blind guess would score.</summary>
    public required double Chance { get; init; }

    /// <summary>Nodes across every cluster.</summary>
    public required int Nodes { get; init; }

    /// <summary>Partner entries across every node.</summary>
    public required int Edges { get; init; }

    /// <summary>The share of questions answered correctly.</summary>
    public double Accuracy => Asked == 0 ? 0.0 : Right / (double)Asked;

    public override string ToString() =>
        $"moments={Moments} asked={Asked} right={Right} silent={Silent} " +
        $"accuracy={Accuracy:F4} chance={Chance:F4} nodes={Nodes} edges={Edges}";
}

/// <summary>
/// The senses world, wired to the graph.
/// </summary>
/// <remarks>
/// <b>Scored prequentially and never trained-then-tested.</b> A question is
/// asked, settled, and then learning carries on — C4 forbids a run that stops,
/// so there is no "after training" to test in.
/// </remarks>
public sealed class SensesRun : IDisposable
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    private readonly HybridBus _bus = new();
    private readonly Ring _ring;
    private readonly LocalClusters _local;
    private readonly List<Cluster> _clusters = [];
    private readonly List<IDisposable> _handles = [];
    private readonly InputMachine<IReadOnlyCollection<Code>> _senses;
    private readonly Senses _world;
    private readonly WalkSettings _dials;
    private readonly List<Exception> _faults = [];

    public SensesRun(
        SensesSettings world,
        WalkSettings dials,
        int seed,
        int clusters = 8,
        int replicas = 256)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        _world = new Senses(world, seed);
        _dials = dials;
        _ring = new Ring(seed, replicas);
        _local = new LocalClusters(_ring);
        _bus.Faults += failure => { lock (_faults) _faults.Add(failure); };

        for (var i = 0; i < clusters; i++)
        {
            var address = new ClusterAddress($"c{i}");
            _ring.Join(address);
            var cluster = new Cluster(address, _bus, _ring, dials);
            _local.Include(cluster);
            _clusters.Add(cluster);
            _handles.Add(_bus.Subscribe(cluster));
        }

        _senses = new InputMachine<IReadOnlyCollection<Code>>(
            new MachineAddress("senses"), new Passthrough(), new LocalRendezvous(_local),
            _bus, _ring, dials);

        _handles.Add(_bus.Subscribe(_senses));
    }

    /// <summary>The codes are already codes; there is nothing to quantise.</summary>
    private sealed class Passthrough : IQuantizer<IReadOnlyCollection<Code>>
    {
        public byte Modality => Senses.Sight;

        public IReadOnlyCollection<Code> Codify(IReadOnlyCollection<Code> observation) => observation;
    }

    /// <summary>
    /// Shows moments, and every <paramref name="every"/> of them stops to ask
    /// what a sight feels like.
    /// </summary>
    public async Task<SensesResult> RunAsync(
        int moments, int every = 10, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(moments);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(every);

        int asked = 0, right = 0, silent = 0;

        for (var moment = 0; moment < moments; moment++)
        {
            await _senses.ObserveAsync(_world.Moment(), moment, ct).ConfigureAwait(false);
            await _bus.WhenQuiet().WaitAsync(Patience, ct).ConfigureAwait(false);

            if (moment % every != 0 || moment == 0) continue;

            var concept = moment % _world.Concepts;
            var answer = await AskAsync(concept, ct).ConfigureAwait(false);

            asked++;
            if (answer is null) silent++;
            else if (Senses.Concept(answer.Value) == concept) right++;
        }

        Failures();

        return new SensesResult
        {
            Moments = moments,
            Asked = asked,
            Right = right,
            Silent = silent,
            Chance = _world.Chance,
            Nodes = _clusters.Sum(cluster => cluster.Count),
            Edges = _clusters.Sum(cluster => cluster.Edges),
        };
    }

    /// <summary>
    /// Shows a concept's sight codes and asks which touch they lead to.
    /// </summary>
    /// <remarks>
    /// <b>Sight and touch have never occurred together</b>, so nothing here can
    /// be a lookup: the only route from one to the other runs through sound.
    /// </remarks>
    public async Task<Code?> AskAsync(int concept, CancellationToken ct = default)
    {
        var thought = await _senses
            .ThinkAsync(_world.Of(Senses.Sight, concept), _dials.Stamina, ct)
            .ConfigureAwait(false);

        await _bus.WhenQuiet().WaitAsync(Patience, ct).ConfigureAwait(false);

        var reached = thought.BestOf(Senses.Touch, 1);
        _senses.Forget(thought.Id);

        return reached.Count == 0 ? null : reached[0].Endpoint;
    }

    private void Failures()
    {
        lock (_faults)
        {
            if (_faults.Count > 0) throw new AggregateException(_faults);
        }
    }

    public void Dispose()
    {
        foreach (var handle in _handles) handle.Dispose();
    }
}
