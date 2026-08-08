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
    /// <summary>
    /// How long a settling wave is given before the harness calls it a hang —
    /// <b>a deadlock detector, and never a claim about speed.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>IT WAS FIVE SECONDS IN FOUR FILES AND 5, 10 AND 30 INLINE IN EIGHT MORE,
    /// AND CI HAD STARTED FLAPPING ON IT.</b> Two <c>MachineTests</c> failed a run
    /// with <see cref="TimeoutException"/> and passed the one before it, having
    /// changed nothing — so the RED SET WAS NOT STABLE, and a suite whose failures
    /// come and go cannot be the baseline anything is measured against.
    /// </para>
    /// <para>
    /// <b>THE CONSTANT WAS SIZED BEFORE EVERY MECHANISM WAS SWITCHED ON.</b> One
    /// <c>Motif</c> run now moves 360,000 messages where the number that chose
    /// five seconds was measured against a fraction of that, so what the bound
    /// actually tested by the end was how loaded the runner happened to be.
    /// <b>A wall-clock bound on a workload that grew is a measurement of the
    /// machine.</b>
    /// </para>
    /// <para>
    /// <b>SO IT IS GENEROUS ON PURPOSE.</b> The only question this is entitled to
    /// answer is whether the bus ever goes quiet at all; a run that is merely slow
    /// is not a run that is wrong. Anything wanting to assert a COST should assert
    /// the message count, which is deterministic — <see cref="Worlds.RunReport"/>
    /// already carries it — rather than the clock, which is not.
    /// </para>
    /// </remarks>
    public static readonly TimeSpan Patience = TimeSpan.FromSeconds(120);

    /// <summary>
    /// A row cap so large no world here reaches it — <b>the top of the sweep, and
    /// no longer a way to switch the cap off.</b>
    /// </summary>
    /// <remarks>
    /// <b>IT REPLACED A NULL, AND THE DIFFERENCE IS NOT COSMETIC.</b> A nullable
    /// cap made <i>unbounded</i> a state the brain could be configured into, which
    /// is an off switch by another name — <see cref="WalkSettings.Row"/> is 32 now
    /// and there is no null. What is left is a QUANTITY, and a quantity is swept:
    /// a measurement that reads the cap at one end of its range and the other is
    /// how anybody knows 32 is the right number, which is the opposite of an arm
    /// nobody can turn off.
    /// </remarks>
    public const int Unbounded = 1_000_000;

    /// <summary>The walk, with only what a test actually varies exposed.</summary>
    /// <param name="stamina">What each route starts with, in perfect hops.</param>
    /// <param name="foresight">
    /// The shallower budget for a prediction — fork 20. <b>There is no
    /// shared-budget arm any more</b>, so this defaults to what the brain does.
    /// </param>
    /// <param name="horizon">
    /// The chain-length backstop. <b>Lower it to make it actually fire</b>; at 50
    /// it never does under inverse cost.
    /// </param>
    /// <remarks>
    /// <b>IT NAMES ONLY WHAT A TEST VARIES, AND AFTER 2026-08-04 THAT IS THREE
    /// NUMBERS.</b> Everything else — the doubt, the row cap, the span, the
    /// naming, the kinds, the surprise, the chunking — is on and has no off, so
    /// there is nothing for a fixture to switch.
    /// </remarks>
    public static WalkSettings Dials(
        double stamina = 4.0, double foresight = 2.0, int horizon = 50) => new()
    {
        Stamina = stamina,
        Foresight = foresight,

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
        int pool = 0,
        double skew = 0.0) => new()
    {
        Concepts = concepts,
        CodesPerSense = codes,
        Noise = noise,
        Clutter = clutter,
        Pool = pool,
        Skew = skew,
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

    /// <summary>
    /// Independent arms of one measurement, run at the same time, answered in order.
    /// </summary>
    /// <typeparam name="T">Whatever an arm reports.</typeparam>
    /// <param name="arms">The arms, each a whole run and each ignorant of the others.</param>
    /// <returns>What each arm returned, in the order the arms were given.</returns>
    /// <remarks>
    /// <para>
    /// <b>THE SUITE IS SERIAL AND STAYS SERIAL; THIS IS THE EXEMPTION `Parallelism.cs`
    /// ALREADY NAMES.</b> A test that genuinely needs concurrency must create it INSIDE
    /// itself — and the reason the assembly is serialised is that numbers move with how
    /// busy the machine is, measured: the walk's agreement with itself reads 0.8833
    /// alone and 1.0000 under load. That is a fact about DELIVERY, and delivery is the
    /// bus.
    /// </para>
    /// <para>
    /// <b>SO THIS IS FOR THE LEARNER AND NEVER FOR THE WALK, AND THE SIGNATURE IS THE
    /// GUARD.</b> <see cref="Machines.ArrangedRun"/>, <see cref="Machines.MultiplexerRun"/>,
    /// <see cref="Machines.CifarRun"/> and <see cref="Machines.GradedRun"/> hold no bus
    /// and are synchronous end to end: a fixed seed determines every number they report
    /// whatever else the machine is doing. Every bus world answers with a
    /// <see cref="Task{TResult}"/> instead, so an arm that would be unsafe here does not
    /// fit the parameter — the rule is enforced by the type rather than written in a
    /// comment nobody reads at the moment it matters.
    /// </para>
    /// <para>
    /// <b>ANSWERED IN ORDER, BECAUSE THE ORDER IS WHAT THE OUTPUT AND THE ASSERTIONS
    /// READ.</b> A grid printed in completion order is a grid whose rows move between
    /// runs, and this project reads its grids.
    /// </para>
    /// </remarks>
    public static T[] Abreast<T>(params Func<T>[] arms)
    {
        ArgumentNullException.ThrowIfNull(arms);

        var answers = new T[arms.Length];

        Parallel.For(0, arms.Length, at => answers[at] = arms[at]());

        return answers;
    }

    /// <summary>A code in the plain test modality.</summary>
    public static Code C(ulong value) => new(Modality: 1, value);

    /// <summary>
    /// The ONE-CODE scopes a population holds, with any minted name spelled back out.
    /// </summary>
    /// <param name="held">What the brain holds.</param>
    /// <remarks>
    /// <b>WHAT GENESIS CAN MINT, WHICH IS THE QUESTION A SOUNDNESS COUNT CANNOT
    /// ANSWER.</b> Covering mints one-code commitments and nothing else, so a code that
    /// is sound ON ITS OWN is reachable by the very first thing the machine does.
    /// Whether it is resident afterwards separates a learner that never found it from
    /// one that found it and was outvoted — and those want different work.
    /// <para>
    /// <b>Unfolded, so a minted name cannot hide a scope that is really one code wearing
    /// a hat.</b> Written out twice in two measurement files before `DuplicationTests`
    /// refused the second, which is that budget doing exactly its job.
    /// </para>
    /// </remarks>
    public static HashSet<Code> Alone(Commitments.Population held)
    {
        ArgumentNullException.ThrowIfNull(held);

        return held.All
            .Select(one => held.Names.Unfold(one.Scope))
            .Where(scope => scope.Length == 1)
            .Select(scope => scope[0])
            .ToHashSet();
    }

    /// <summary>
    /// The four arrangements <c>Mending</c> shipped as one list, as the two settings they
    /// turned out to be.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>SHARED SO THE GRIDS STAY COMPARABLE ACROSS FILES, WHICH IS THE WHOLE RISK OF
    /// SPLITTING AN ENUM.</b> Four files sweep these cells and every finding in the
    /// commits is labelled by them; four private copies of the mapping is four chances for
    /// one row to mean something different from the row it is being read against.
    /// </para>
    /// <para>
    /// <b>AND THE SILENT HALF IS WHY THIS EXISTS RATHER THAN A FIND-AND-REPLACE.</b> Two of
    /// the old cells kept their names as GATES, so <c>Mending = Mending.Uncovered</c> still
    /// compiles and now means <i>uncovered, after a failure</i> where it used to mean
    /// <i>uncovered, every round</i>. The compiler catches the two renamed cells and says
    /// nothing at all about the two that quietly changed arm.
    /// </para>
    /// <para>
    /// <b>THE TWO CELLS THE SPLIT MAKES REACHABLE ARE NOT HERE.</b> An ungated repair every
    /// round and the improving signal after a failure have never been measured, and adding
    /// them to a grid in the same edit that rearranged it would mean no row could be
    /// compared with the reading it replaced.
    /// </para>
    /// </remarks>
    public static readonly (string Arm, Commitments.Mending Gate, Commitments.Repairing When)[]
        Repairs =
        [
            ("after failure, no gate", Commitments.Mending.Ungated, Commitments.Repairing.AfterFailure),
            ("after failure, gate", Commitments.Mending.Uncovered, Commitments.Repairing.AfterFailure),
            ("every round, gate", Commitments.Mending.Uncovered, Commitments.Repairing.EveryRound),
            ("every round, gate, paid", Commitments.Mending.Improving, Commitments.Repairing.EveryRound),
        ];

    /// <summary>
    /// Places every commitment on a holder, the way the ring would.
    /// </summary>
    /// <param name="all">The whole population.</param>
    /// <param name="holders">How many machines to spread it over.</param>
    /// <remarks>
    /// <b>SHARED BECAUSE A SECOND COPY WOULD MAKE TWO GRIDS THAT LOOK COMPARABLE AND ARE
    /// NOT.</b> <c>SplitNamingTests</c> measured what sharding costs rung five and
    /// <c>AskedTests</c> puts the same exchange on a socket, so a difference in HOW the
    /// population is split would show up as a difference the wire appeared to cause.
    /// </remarks>
    public static List<List<Commitments.Commitment>> Sharded(
        IEnumerable<Commitments.Commitment> all, int holders)
    {
        ArgumentNullException.ThrowIfNull(all);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(holders);

        var shards = new List<List<Commitments.Commitment>>();

        for (var holder = 0; holder < holders; holder++) shards.Add([]);

        foreach (var commitment in all)
            shards[(int)(commitment.Identity.Value % (ulong)holders)].Add(commitment);

        return shards;
    }

    /// <summary>
    /// A broadcast standing on its first node, with budget to spend.
    /// </summary>
    /// <remarks>
    /// <b>Written out twice before `DuplicationTests` refused it</b>, which is the
    /// check doing its job: two copies of the message a walk STARTS from are two
    /// places for a field's default to drift, and a walk that begins differently in
    /// two files is two different experiments wearing one name.
    /// </remarks>
    public static Thinking.Message Origin(Code code) => new()
    {
        Broadcast = Thinking.BroadcastId.New(),
        ReturnTo = new MachineAddress("test"),
        To = code,
        Held = 10.0,
        Chain = [code],
        Carried = 1.0,
    };
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
