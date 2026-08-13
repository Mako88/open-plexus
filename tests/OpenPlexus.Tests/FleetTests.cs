using System.Diagnostics;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// A learner whose commitments are on other machines — <b>fork 52 mounted, and fork 1's
/// answer for the commitment half.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>What crossed before this was a demonstration and not a run.</b> <c>AskedTests</c>
/// puts one vote and one counts merge on real sockets between machines that share no
/// object, and both are asked of a population somebody else had already trained. Nothing
/// had ever LEARNT across a wire: fork 1 is open because an occasion writes its edges into
/// locally-held clusters, and the commitment learner was in the same position for a
/// different reason — <c>Cycle</c> called <c>Predict</c> on its own population and
/// <c>Abstract</c> with no argument.
/// </para>
/// <para>
/// <b>And it is not a test of C2, which is what will be assumed.</b> TCP does not reorder
/// within a connection, so this exercises less adversity than <see cref="Bus.HybridBus"/>
/// does. Green here says the bytes, the routing and the arithmetic are right, and says
/// nothing whatever about lateness.
/// </para>
/// <para>
/// <b>NOR OF C3.</b> Nothing dies in any of this, and every number below is a fleet whose
/// machines are all present — which is what makes them comparable with the one-process
/// curve. A run that LOSES a holder goes on, since fork 53 writes off an ask that could not
/// be handed over; what that costs a curve is measured in <c>UnreachedTests</c> and is a
/// different question from what distance costs.
/// </para>
/// </remarks>
public sealed class FleetTests(ITestOutputHelper output)
{
    /// <summary>Six bits, which is the world step one is judged on.</summary>
    private const int Narrow = 2;

    /// <summary>
    /// A run in one process, which is every number this project has ever taken.
    /// </summary>
    /// <param name="dials">The brain's numbers.</param>
    /// <param name="address">Address bits.</param>
    /// <param name="rounds">How many rounds.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    private static (Tally Tally, Population Held) Alone_(
        CommittingSettings dials, int address, long rounds, int seed)
    {
        var brain = new Brain(dials, seed);

        var world = new Multiplexer(new MultiplexerSettings { Address = address }, seed);
        var trial = new Trial<IReadOnlyList<int>>(world, new Bits(Multiplexer.Bit), brain);

        return (trial.Run(rounds), brain.Held);
    }

    /// <summary>
    /// The same run with the commitments spread over machines that share no object.
    /// </summary>
    /// <param name="dials">The brain's numbers.</param>
    /// <param name="address">Address bits.</param>
    /// <param name="rounds">How many rounds.</param>
    /// <param name="seed">The world's generator and every holder's.</param>
    /// <param name="holders">How many machines.</param>
    /// <remarks>
    /// <b>Same world, same seed, same front end, same dials.</b> The holders are the only
    /// difference, which is what makes the two curves comparable at all — this repo's own
    /// rule is to measure one mechanism on from a known baseline rather than a sharded
    /// world against a whole one, where four things move and the score cannot say which.
    /// </remarks>
    private static async Task<(Tally Tally, Ported Fleet, Fleet Council)> Spread(
        CommittingSettings dials, int address, long rounds, int seed, int holders)
    {
        var brain = new Brain(dials, seed);

        var world = new Multiplexer(new MultiplexerSettings { Address = address }, seed);
        var trial = new Trial<IReadOnlyList<int>>(world, new Bits(Multiplexer.Bit), brain);

        var fleet = await Ported.OpenAsync(holders, dials, seed);

        var council = new Fleet(fleet.Asker, dials);

        var running = trial.RunAsync(council, fleet.Held, rounds);

        // The experimenter's patience and never the machine's. A fleet waits on its
        // gathering forever by design, so a single lost message is a run that never ends --
        // correct behaviour, and a suite that inherited it would hang instead of failing.
        // See `Ported.Patience`.
        if (await Task.WhenAny(running, Task.Delay(Ported.Patience)) != running)
        {
            await fleet.DisposeAsync();

            Assert.Fail(
                $"a fleet of {holders} never finished {rounds} rounds — it asked "
                + $"{council.Asked} and heard {council.Heard}, and {fleet.Since} messages "
                + "have been lost since it came up");
        }

        var tally = await running;

        // The denominator, asserted where the run ends rather than reported. Nothing here
        // decides a missing holder by a clock, so a fleet that quietly stopped asking one
        // machine would learn from the rest and score perfectly well -- and the only thing
        // that could say so is how many it asked against how many answered.
        Assert.Equal(holders, council.Asked);
        Assert.Equal(holders, council.Heard);

        // And nothing was lost while it ran, which is a claim about the WIRE rather than
        // about the learner and could not be made before the bus counted. A dropped ask is
        // indistinguishable from a slow one to everything else here, and the difference is
        // whether the run has an answer or is merely still waiting for one.
        Assert.Equal(0, fleet.Since);

        return (tally, fleet, council);
    }

    /// <summary>
    /// <b>A fleet learns the multiplexer, and no one machine holds the rules.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The wiring check rather than the measurement. What it asserts is that the loop
    /// reaches every holder, that the population is genuinely SPLIT rather than copied,
    /// and that the fleet's vote is good enough to be a learner at all.
    /// </para>
    /// <para>
    /// <b>The disjointness is the half that could silently not happen.</b> Every holder
    /// sees every observation and runs the identical round, so without
    /// <see cref="Population.Places"/> each would mint the same rules and the fleet would
    /// be N copies of one population — which learns perfectly well and is not a
    /// distribution. A score alone could never tell those two apart.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task A_fleet_learns_across_sockets_and_no_machine_holds_the_whole_population()
    {
        const int Holders = 3;
        const long Rounds = 4000;

        var dials = new CommittingSettings();

        var (tally, fleet, council) = await Spread(dials, Narrow, Rounds, seed: 1, Holders);

        await using var _ = fleet;

        // And the merge was asked to combine something, which the score cannot say. A run
        // where one holder speaks and the rest are silent is merged identically by every
        // rule there is, so it would agree with one process while combining nothing.
        Assert.True(council.Contested > Rounds / 10,
            $"only {council.Contested} of {Rounds} votes put advocates on more than one "
            + "machine, so the merge was hardly ever asked to combine anything");

        // IT LEARNT. Six bits is a four-way problem, so chance is a quarter and anything
        // near it says the exchange carried nothing the learner could use.
        Assert.True(tally.Recent > 0.6,
            $"a fleet of {Holders} scored {tally.Recent:F3} over the last tenth, which is "
            + "not far enough above four-way chance to be learning");

        // And it is split rather than copied, which no score could say. Every holder holds
        // something, and every ROOT sits on the machine its identity hashes to.
        foreach (var held in fleet.Held)
            Assert.True(held.Count > 0, "a holder learnt nothing, so the fleet is not spread");

        // Genesis placed them, which is the mechanism and not the outcome. Without
        // `Population.Places` every holder would mint the same rules and the fleet would be
        // N copies of one population -- which learns perfectly well and is not a
        // distribution. Only the roots are checked, because a child belongs with its parent.
        for (var at = 0; at < Holders; at++)
            foreach (var one in fleet.Held[at].All.Where(one => one.Scope.Length == 1))
                Assert.Equal((ulong)at, one.Identity.Value % Holders);

        // And the roots are the only thing that is disjoint, which is fork 29 arriving
        // sharper than it was written. Two nodes repairing one parent were predicted to
        // mint SIBLINGS; what actually happens is that two DIFFERENT parents on different
        // machines reach the identical child -- `{x}` repaired with `z` and `{z}` repaired
        // with `x` are one scope and one name. Nothing here can prevent it: placing a child
        // by hash would drop it, because repair is the only thing that proposes a scope
        // longer than one code and nobody else would mint it; minting it on the machine it
        // hashes to would put a commitment on the wire, which C1 refuses outright.
        var everywhere = fleet.Held.SelectMany(one => one.All).ToList();

        var twice = everywhere
            .GroupBy(one => one.Identity)
            .Where(one => one.Count() > 1)
            .ToList();

        Assert.Equal(tally.Resident, everywhere.Count);

        // And every one of them is a child, which is the claim above rather than a
        // summary of it. A duplicated ROOT would mean the placement is not working at all
        // -- and would fail the loop above too -- so this says the only thing crossing
        // machines is a scope repair reached from two directions.
        Assert.All(twice, one => Assert.True(one.First().Scope.Length > 1,
            "a one-code commitment is held twice, so genesis is not placed"));

        // No bar on how many, because this is the first reading of it. What it costs is a
        // fact about the weighing rather than about the count, and the weighing that would
        // have charged for it is deleted: an expectation is worth its best advocate, so a
        // rule held twice is worth exactly what it was. A summed vote counted its evidence
        // once per machine -- the weigh-one-machine's-scopes-double fault arriving from
        // inside the population rather than from the merge -- and that is now unreachable.
        output.WriteLine(
            $"{Holders} holders on {Holders + 1} ports | {tally.Rounds} rounds | "
            + $"recent {tally.Recent:F3} | {tally.Resident} resident, "
            + $"{string.Join("/", fleet.Held.Select(one => one.Count))} | "
            + $"minted {tally.Minted}, repaired {tally.Repaired}, subsumed {tally.Subsumed}");

        output.WriteLine(
            $"{twice.Count} of {tally.Resident} commitments are held by more than one "
            + $"machine, every one of them a child two parents reached — fork 29, and "
            + $"a maximum is what keeps its evidence from counting twice");
    }

    /// <summary>
    /// <b>What distribution costs a learning curve — a grid, and no bar.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE ONE NUMBER NOBODY HAS.</b> The merge is integer addition and the vote
    /// composes exactly, a maximum of maxima being a maximum — both proved, neither
    /// re-proved here. What is unknown is what a whole RUN does when the population is
    /// spread: the vote composes per round and the population does not, because genesis is
    /// placed, repair is local, subsumption sees only what one machine holds, and the
    /// repair gate can only refuse a repair another commitment on THIS machine covers.
    /// </para>
    /// <para>
    /// <b>So a gap is not a defect and neither is a lead, and that is why there is no
    /// bar.</b> A threshold written before the first reading is a prediction dressed as a
    /// requirement. The grid is the finding, and the number to read is the last tenth's
    /// accuracy against the same brain in one process.
    /// </para>
    /// <para>
    /// <b>The clock is printed and asserted on by nothing.</b> Two socket round trips a
    /// round is what the shape costs, and on loopback that is a floor rather than an
    /// answer — a LAN adds its own delay and the internet a great deal more. See fork 56.
    /// </para>
    /// </remarks>
    [Theory]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    [InlineData(2, 3)]
    [InlineData(2, 6)]
    [InlineData(3, 3)]
    [InlineData(3, 6)]
    public async Task What_spreading_the_population_costs_a_learning_curve(
        int address, int holders)
    {
        const long Rounds = 8000;

        var dials = new CommittingSettings();

        // One case per arrangement rather than one grid, and the reason is that a fleet
        // costs a fleet. Bringing one up and taking it down is most of this test's clock,
        // and a single method holding every cell reports nothing at all until the last one
        // finishes -- so one slow arrangement hides every reading before it.
        output.WriteLine(
            $"{address + (1 << address)} bits, {holders} holders | seed | "
            + "alone | spread | resident alone/spread | minted | repaired | contested");

        foreach (var seed in new[] { 1, 2, 3 })
        {
            var (here, held) = Alone_(dials, address, Rounds, seed);

            var (there, fleet, council) = await Spread(dials, address, Rounds, seed, holders);

            await using var _ = fleet;

            output.WriteLine(
                $"{seed,4} | {here.Recent,5:F3} | {there.Recent,6:F3} | "
                + $"{held.Count,8}/{there.Resident,-8} | "
                + $"{here.Minted,3}/{there.Minted,-3} | "
                + $"{here.Repaired,4}/{there.Repaired,-4} | {council.Contested,9}");
        }
    }

    /// <summary>
    /// <b>Whether the fleet's advantage is the repair gate turned down, and not the wire
    /// at all.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The control was already in the codebase and was built for exactly this.</b>
    /// <see cref="Population.Placing"/> tells the repair gate which of the commitments it
    /// can see are notionally elsewhere, so a run with it set has a fleet's GATE and one
    /// process's POPULATION. That splits the two things distribution does at once, and
    /// nothing else can: a sharded run moves both and a whole run moves neither.
    /// </para>
    /// <para>
    /// <b>So the three arms answer the question between them.</b> If placed-alone lands on
    /// the fleet, the advantage is the gate and the wire contributes nothing; if it lands
    /// on alone, the advantage is the population being split and the gate is innocent. Both
    /// readings are useful and only one of them is the story the eleven-bit row was written
    /// up as, which is why this exists rather than a paragraph asserting it.
    /// </para>
    /// <para>
    /// <b>Eleven bits, because six has nothing to separate.</b> The fleet is level with one
    /// process at the narrow width and ahead at the wide one, so the wide one is where an
    /// explanation has something to explain.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task Whether_the_fleets_advantage_is_the_repair_gate_rather_than_the_wire()
    {
        const long Rounds = 8000;
        const int Holders = 3;
        const int Address = 3;

        var dials = new CommittingSettings();

        output.WriteLine(
            "eleven bits | seed | alone | placed alone | fleet | residents | repaired");

        foreach (var seed in new[] { 1, 2, 3 })
        {
            var (here, held) = Alone_(dials, Address, Rounds, seed);

            // The gate of a fleet and the population of one machine. `Placing` is read by
            // `Mend` alone -- firing, voting and settling never see it -- so this run
            // differs from the one above in the repair gate and in nothing else.
            var placed = new Brain(dials, seed);

            placed.Held.Placing = one => one.Identity.Value % Holders;

            var world = new Multiplexer(new MultiplexerSettings { Address = Address }, seed);

            var apart = new Trial<IReadOnlyList<int>>(
                world, new Bits(Multiplexer.Bit), placed).Run(Rounds);

            var (there, fleet, council) = await Spread(dials, Address, Rounds, seed, Holders);

            await using var running = fleet;

            Assert.Equal(Holders, council.Asked);

            output.WriteLine(
                $"{seed,12} | {here.Recent,5:F3} | {apart.Recent,12:F3} | {there.Recent,5:F3} "
                + $"| {held.Count}/{placed.Held.Count}/{there.Resident} "
                + $"| {here.Repaired}/{apart.Repaired}/{there.Repaired}");
        }

        // No bar, because the point is which two of three arms land together and that has
        // never been read. A threshold written before the first reading would be a
        // prediction dressed as a requirement, and this file already has one prediction in
        // it that the grid refuted.
    }

    /// <summary>
    /// <b>And a fleet run reproduces itself, which a wire could easily have cost.</b>
    /// </summary>
    /// <remarks>
    /// <b>Fork 12, with the two halves on different machines.</b> Every merge here is
    /// ordered before it is combined and every placement is a fact about a commitment
    /// rather than about who asked, so nothing should move with delivery order — but that
    /// is an argument, and this project has reopened that fork twice on arguments.
    /// </remarks>
    [Fact]
    public async Task The_same_seed_produces_the_same_fleet_run()
    {
        // Short, because what is being asserted is an equality rather than a score. Three
        // fleets on four ports each is most of this test's clock; a longer run would buy
        // nothing but a slower suite, and the backstop below is what says the report has
        // enough in it to differ at all.
        const long Rounds = 1000;

        var dials = new CommittingSettings();

        var (first, one, _) = await Spread(dials, Narrow, Rounds, seed: 7, holders: 3);
        await using (one) { }

        var (second, two, _) = await Spread(dials, Narrow, Rounds, seed: 7, holders: 3);
        await using (two) { }

        Assert.Equal(first, second);

        // AND THE BACKSTOP: a different seed is a different run, or the equality above is
        // being satisfied by a report that carries nothing.
        var (other, three, _) = await Spread(dials, Narrow, Rounds, seed: 8, holders: 3);
        await using (three) { }

        Assert.NotEqual(first, other);
    }
}
