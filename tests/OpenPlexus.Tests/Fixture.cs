using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The settings records every measurement in this suite is built from.
/// </summary>
/// <remarks>
/// <para>
/// <b>These were written out fifteen times, and the copies had already
/// drifted</b> — same four dials, three different orderings, two different
/// indentations, and no way to tell at a glance whether two tests were measuring
/// the same configuration. A number that differs between two files should differ
/// because somebody chose it.
/// </para>
/// <para>
/// <b>What varies is a parameter; what has never varied is a constant here.</b>
/// <see cref="Accumulate.Sum"/> is what every measurement in the project was
/// taken under, so an arm that wants otherwise says
/// <c>with { Accumulate = ... }</c> at the point of use and is visibly the
/// exception.
/// </para>
/// </remarks>
public static class Fixture
{
    /// <summary>The walk, with only what a test actually varies exposed.</summary>
    /// <param name="stamina">What each route starts with, in perfect hops.</param>
    /// <param name="foresight">
    /// The shallower budget for a prediction — fork 20. Null means the two
    /// questions share one budget.
    /// </param>
    /// <param name="horizon">
    /// The chain-length backstop. <b>Lower it to make it actually fire</b>; at 50
    /// it never does under inverse cost.
    /// </param>
    public static WalkSettings Dials(
        double stamina = 4.0, double? foresight = null, int horizon = 50) => new()
    {
        Stamina = stamina,
        Foresight = foresight,
        Accumulate = Accumulate.Sum,
        Horizon = horizon,
    };

    /// <summary>The board nearly every snake measurement is taken on.</summary>
    public static SnakeSettings Snake(
        int? sight = 1,
        double energy = 60.0,
        double perFood = 30.0,
        int size = 15) => new()
    {
        Width = size,
        Height = size,
        Sight = sight,
        StartingEnergy = energy,
        EnergyPerStep = 1.0,
        EnergyPerFood = perFood,
    };

    /// <summary>The senses world, clean unless a test asks for noise.</summary>
    public static SensesSettings Senses(
        int concepts = 8,
        int codes = 3,
        double noise = 0.0,
        int clutter = 0,
        int pool = 0) => new()
    {
        Concepts = concepts,
        CodesPerSense = codes,
        Noise = noise,
        Clutter = clutter,
        Pool = pool,
    };

    /// <summary>The binding world.</summary>
    public static BindingSettings Binding(
        bool bound = false,
        int concepts = 8,
        int codes = 3,
        bool segmented = false,
        bool tagged = false,
        bool fleeting = false) => new()
    {
        Concepts = concepts,
        CodesPerAttribute = codes,
        Bound = bound,
        Segmented = segmented,
        Tagged = tagged,
        Fleeting = fleeting,
    };

    /// <summary>A code in the plain test modality.</summary>
    public static Code C(ulong value) => new(Modality: 1, value);
}

/// <summary>
/// A bus, a ring, some clusters and a rendezvous — what a test needs when it is
/// about the graph itself rather than about a world.
/// </summary>
/// <remarks>
/// <para>
/// <b>Not <see cref="Fabric"/>, deliberately.</b> A fabric subscribes every
/// cluster to the bus, because a world needs messages to actually travel. Half
/// the tests here are about what the rendezvous WRITES and never dispatch
/// anything, and putting them on a live bus would add traffic to runs whose
/// message counts are the thing being asserted.
/// </para>
/// <para>
/// <b>Faults are thrown rather than collected.</b> A world counts them so a run
/// can report them; a test wants the stack trace at the point it happened.
/// </para>
/// </remarks>
public sealed class Bench : IDisposable
{
    private readonly List<IDisposable> _handles = [];
    private readonly List<IDisposable> _clusters = [];

    /// <param name="dials">The walk settings every cluster is built with.</param>
    /// <param name="listening">
    /// Whether the clusters are subscribed to the bus. <b>Off unless a test
    /// actually dispatches</b>; see the note on this class.
    /// </param>
    /// <param name="names">
    /// What to call the clusters. Several, so codes really are spread and a join
    /// has to reach across them rather than into one dictionary.
    /// </param>
    public Bench(WalkSettings dials, bool listening = false, params string[] names)
    {
        ArgumentNullException.ThrowIfNull(dials);

        Bus.Faults += failure => throw failure;
        Local = new LocalClusters(Ring);
        Rendezvous = new LocalRendezvous(Local);

        foreach (var name in names.Length == 0 ? ["a", "b", "c", "d"] : names)
        {
            var address = new ClusterAddress(name);
            Ring.Join(address);

            var cluster = new Cluster(address, Bus, Ring, dials);
            Local.Include(cluster);

            if (listening) _clusters.Add(Bus.Subscribe(cluster));
        }
    }

    public HybridBus Bus { get; } = new();

    public Ring Ring { get; } = new(seed: 42, replicas: 64);

    public LocalClusters Local { get; }

    public LocalRendezvous Rendezvous { get; }

    /// <summary>Puts a machine on the bus and keeps the handle.</summary>
    public IDisposable Subscribe(IReceiveReports machine)
    {
        var handle = Bus.Subscribe(machine);
        _handles.Add(handle);
        return handle;
    }

    /// <summary>The node for a code, created if this is its first mention.</summary>
    public Node Node(Code code) => Local.For(code);

    /// <summary>
    /// One cluster leaves the bus, which is what raises a death event.
    /// </summary>
    /// <remarks>
    /// <b>Named rather than done by index.</b> Disposing a handle out of a list
    /// is the same thing and says nothing about why, and the why is the whole
    /// content of fork 5.
    /// </remarks>
    public void Depart()
    {
        Assert.NotEmpty(_clusters);

        _clusters[0].Dispose();
        _clusters.RemoveAt(0);
    }

    public void Dispose()
    {
        foreach (var handle in _handles.Concat(_clusters)) handle.Dispose();
    }
}
