using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

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
/// <b>What varies is a parameter</b>; what has never varied is a constant here. An
/// arm that wants otherwise says <c>with { Whatever = ... }</c> at the point of use
/// and is visibly the exception, rather than adding a parameter nobody else passes.
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
    /// <b>It was five seconds in four files</b> and 5, 10 and 30 inline in eight more,
    /// and CI had started flapping on it. Two <c>MachineTests</c> failed a run
    /// with <see cref="TimeoutException"/> and passed the one before it, having
    /// changed nothing — so the RED SET WAS NOT STABLE, and a suite whose failures
    /// come and go cannot be the baseline anything is measured against.
    /// </para>
    /// <para>
    /// <b>The constant was sized before every mechanism was switched on.</b> One
    /// <c>Motif</c> run now moves 360,000 messages where the number that chose
    /// five seconds was measured against a fraction of that, so what the bound
    /// actually tested by the end was how loaded the runner happened to be.
    /// <b>A wall-clock bound on a workload that grew</b> is a measurement of the
    /// machine.
    /// </para>
    /// <para>
    /// <b>So it is generous on purpose.</b> The only question this is entitled to
    /// answer is whether the bus ever goes quiet at all; a run that is merely slow
    /// is not a run that is wrong. Anything wanting to assert a COST should assert
    /// a count, which is deterministic, rather than the clock, which is not.
    /// </para>
    /// </remarks>
    public static readonly TimeSpan Patience = TimeSpan.FromSeconds(120);

    /// <summary>
    /// The spine world's house, at the cell its own grids read — <b>one house rather than a
    /// literal per instrument.</b>
    /// </summary>
    /// <param name="examining">Which question the house is asked.</param>
    /// <remarks>
    /// <b>Named at every call and defaulted nowhere else</b>, which is the rule this repo
    /// learnt the hard way: a fixture inherits every dial it does not pin, so a default
    /// moving rewrites an experiment nobody edited. What is shared here is the whole house
    /// and the one axis that differs, so two instruments cannot drift apart while reading
    /// against each other. Four people, because one leaves the middle hop free.
    /// </remarks>
    public static Worlds.RoamingSettings House(Worlds.Examining examining) =>
        new()
        {
            Rooms = 6,
            Props = 4,
            People = 4,
            Steps = 120,
            Withheld = 600,
            Examining = examining,
        };

    /// <summary>The senses world, clean unless a test asks for noise.</summary>
    public static SensesSettings Senses(
        int concepts = 8,
        int codes = 3,
        double noise = 0.0,
        int clutter = 0,
        int pool = 0,
        double skew = 0.0,
        bool scrambled = false,
        int withheld = 0) => new()
    {
        Concepts = concepts,
        CodesPerSense = codes,
        Noise = noise,
        Clutter = clutter,
        Pool = pool,
        Skew = skew,
        Scrambled = scrambled,
        Withheld = withheld,
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
    /// <b>The suite is serial and stays serial</b>; this is the exemption `Parallelism.cs`
    /// ALREADY NAMES. A test that genuinely needs concurrency must create it INSIDE
    /// itself — and the reason the assembly is serialised is that numbers move with how
    /// busy the machine is, measured: the walk's agreement with itself reads 0.8833
    /// alone and 1.0000 under load. That is a fact about DELIVERY, and delivery is the
    /// bus.
    /// </para>
    /// <para>
    /// <b>So this is for the learner and never for the walk</b>, and the signature is the
    /// guard. <see cref="Machines.ArrangedRun"/>, <see cref="Machines.MultiplexerRun"/>,
    /// <see cref="Machines.CifarRun"/> and <see cref="Machines.GradedRun"/> hold no bus
    /// and are synchronous end to end: a fixed seed determines every number they report
    /// whatever else the machine is doing. Every bus world answers with a
    /// <see cref="Task{TResult}"/> instead, so an arm that would be unsafe here does not
    /// fit the parameter — the rule is enforced by the type rather than written in a
    /// comment nobody reads at the moment it matters.
    /// </para>
    /// <para>
    /// <b>Answered in order</b>, because the order is what the output and the assertions
    /// read. A grid printed in completion order is a grid whose rows move between
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

    /// <summary>A bench over the narrow multiplexer, driving a fleet already open.</summary>
    /// <param name="fleet">The machines that hold the commitments.</param>
    /// <param name="dials">The brain's numbers, shared with every holder.</param>
    /// <param name="address">Address bits.</param>
    /// <remarks>
    /// <para>
    /// <b>Extracted because the clone budget refused the third copy</b>, which is what that
    /// budget is for. Three fleet files want the identical arrangement and a difference
    /// between them would read as a difference the deployment caused.
    /// </para>
    /// <para>
    /// <b>The fleet is opened first and the brain built over it</b>, which is the order the
    /// substrate now imposes. A brain whose council is handed to the run rather than to the
    /// constructor could be given a different one per call, and then a run and its baseline
    /// would not have to be the same machine.
    /// </para>
    /// </remarks>
    public static (Machines.Bench Bench, Machines.Fleet Council) Multiplexed(
        Ported fleet, Commitments.CommittingSettings dials, int address)
    {
        ArgumentNullException.ThrowIfNull(fleet);

        var council = new Machines.Fleet(fleet.Asker, dials);

        var brain = new Machines.Brain(dials, seed: 1, _ => council);

        var world = new Worlds.Multiplexer(
            new Worlds.MultiplexerSettings { Address = address }, seed: 1);

        return (
            new Machines.Bench(
                new Machines.Watching<IReadOnlyList<int>>(
                    world, new Codes.Bits(Worlds.Multiplexer.Bit)),
                brain),
            council);
    }

    /// <summary>
    /// The ONE-CODE scopes a population holds, with any minted name spelled back out.
    /// </summary>
    /// <param name="held">What the brain holds.</param>
    /// <remarks>
    /// <b>What genesis can mint</b>, which is the question a soundness count cannot
    /// answer. Genesis mints one-code commitments and nothing else, so a code that
    /// is sound ON ITS OWN is reachable by the very first thing the machine does.
    /// Whether it is resident afterwards separates a learner that never found it from
    /// one that found it and was outvoted — and those want different work.
    /// <para>
    /// <b>Unfolded</b>, so a minted name cannot hide a scope that is really one code wearing
    /// a hat. Written out twice in two measurement files before `DuplicationTests`
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
    /// <b>Shared so the grids stay comparable across files</b>, which is the whole risk of
    /// splitting an enum. Four files sweep these cells and every finding in the
    /// commits is labelled by them; four private copies of the mapping is four chances for
    /// one row to mean something different from the row it is being read against.
    /// </para>
    /// <para>
    /// <b>And the silent half is why this exists rather than a find-and-replace.</b> Two of
    /// the old cells kept their names as GATES, so <c>Mending = Mending.Uncovered</c> still
    /// compiles and now means <i>uncovered, after a failure</i> where it used to mean
    /// <i>uncovered, every round</i>. The compiler catches the two renamed cells and says
    /// nothing at all about the two that quietly changed arm.
    /// </para>
    /// <para>
    /// <b>The two cells the split makes reachable are not here.</b> An ungated repair every
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
    /// The two cells the split made reachable, <b>which have never been run.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A separate row rather than two more entries above</b>, and the reason is that they
    /// are not the same kind of thing. <see cref="Repairs"/> is a historical record —
    /// four arrangements that shipped, that every commit's numbers are labelled by, and
    /// that four files sweep. These two were never REFUSED; they were unreachable, because
    /// one enum decided both axes and no value of it landed here.
    /// </para>
    /// <para>
    /// <b>So a grid that wants all six concatenates</b>, and one that wants the old four is
    /// untouched. Adding these above would silently widen every existing sweep, and the
    /// four rows those sweeps print would stop being the four rows their commits recorded.
    /// </para>
    /// </remarks>
    public static readonly (string Arm, Commitments.Mending Gate, Commitments.Repairing When)[]
        Reachable =
        [
            ("every round, no gate", Commitments.Mending.Ungated, Commitments.Repairing.EveryRound),
            ("after failure, gate, paid", Commitments.Mending.Improving, Commitments.Repairing.AfterFailure),
        ];

    /// <summary>
    /// The repair budgets a curve is read at, from well below the level to no limit at all.
    /// </summary>
    /// <remarks>
    /// <b>Shared for the reason <see cref="ReadAsync"/> is shared</b>, and the clone budget
    /// said so a second time. Two grids sweeping the budget are only comparable if they sweep
    /// the same budgets, and a list written out per file is how one of them comes to bracket
    /// a default the other does not. <c>BudgetCurveTests</c> asserts the shipped default is
    /// in here, so every reader of this list inherits that guard rather than restating it.
    /// </remarks>
    public static IReadOnlyList<int> Budgets { get; } = [8, 16, 32, 64, 128, 256, int.MaxValue];

    /// <summary>
    /// The four multiplexers a budget curve is read on, as address bits and skew.
    /// </summary>
    /// <remarks>
    /// <b>Both widths and both tilts</b>, because the two halves of every trade measured here
    /// live on different ones. Coverage and sound rules are bought where the base rate
    /// pays nothing; trailing accuracy is sold where it pays. A curve on one of them reads as
    /// a clean win in whichever direction it was taken.
    /// </remarks>
    public static IReadOnlyList<(int Address, double Skew)> Curve { get; } =
        [(3, 0.8), (3, 0.0), (2, 0.8), (2, 0.0)];

    /// <summary>
    /// Places every commitment on a holder, the way the ring would.
    /// </summary>
    /// <param name="all">The whole population.</param>
    /// <param name="holders">How many machines to spread it over.</param>
    /// <remarks>
    /// <b>Shared because a second copy would make two grids</b> that look comparable and are
    /// not. <c>SplitNamingTests</c> measured what sharding costs rung five and
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
    /// <param name="output">Where the rows go.</param>
    /// <param name="cell">What this row of the grid is, in the grid's own words.</param>
    /// <param name="seeds">How many seeds each reading is taken over.</param>
    /// <param name="cached">
    /// One run per seed. <b>Cached by the caller and not by this</b> — six readings asked
    /// independently would run the identical configuration six times and report one
    /// measurement as though it were six.
    /// </param>
    /// <param name="readings">What to pull out of each run.</param>
    /// <remarks>
    /// <b>The same six statements were in two grids</b> and the clone budget said so. They
    /// are not incidentally alike: a cell of a sweep IS a configuration, a seed count and a
    /// list of readings, and writing that out per file is how two grids come to print
    /// columns that look comparable while one of them quietly sweeps a different number of
    /// seeds. The one number every reader compares across files is the standard error, and
    /// it is a function of exactly what this takes.
    /// </remarks>
    public static async Task ReadAsync(
        ITestOutputHelper output,
        string cell,
        int seeds,
        Func<int, Machines.Learned> cached,
        params (string What, Func<Machines.Learned, double> Of)[] readings)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(cached);
        ArgumentNullException.ThrowIfNull(readings);

        foreach (var reading in readings)
        {
            var arm = await Sweep.ArmAsync(
                reading.What,
                seeds,
                seed => Task.FromResult(reading.Of(cached(seed)))).ConfigureAwait(false);

            output.WriteLine(
                $"  {cell,-15} {reading.What,-10} | {arm.Mean,10:F3} "
                + $"+/-{arm.StdErr,8:F3} | n={arm.Seeds}");
        }
    }
}

