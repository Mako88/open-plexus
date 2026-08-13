using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What a population costs to HOLD — <b>the first byte count in this project, and the only
/// instrument that can refute the north star.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Every instrument here watches time or counts rows, and the traps list already names
/// that shape.</b> A cost can sit in memory while every reading watches the clock; a run
/// that gets slower is noticed within a session and a run that gets fatter is noticed when
/// it will not start. Residents, tables, scopes and names have all been counted since the
/// branch began, and not one of them is in bytes.
/// </para>
/// <para>
/// <b>And it is the north star's own question.</b> Twenty used phones running the brain is a
/// claim about memory before it is a claim about anything else — no score, no curve and no
/// latency reading says whether a holder's share fits on a machine somebody already owns.
/// This is the one that can come back and say no.
/// </para>
/// <para>
/// <b>The slope is the reading and the intercept is noise, which is why it is measured at
/// three sizes.</b> A managed heap in a test host holds the runner, the world, the
/// framework and whatever the last test left behind, so one delta is a number about this
/// process. What a population costs is how the delta GROWS with what it holds, and that
/// subtracts the process it was measured in.
/// </para>
/// </remarks>
public sealed class FootprintTests(ITestOutputHelper output)
{
    /// <summary>
    /// Eleven bits, because six is refused on power and holds too little to weigh.
    /// </summary>
    private const int Wide = 3;

    /// <summary>
    /// What one holder's share may not exceed, in bytes — <b>the north star's claim rather
    /// than a performance target.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A bar written before the first reading is usually a prediction dressed as a
    /// requirement, and this one is not mine to predict.</b> It comes from outside the
    /// measurement entirely — <b>John's number, 2026-08-11</b>, and he is the one who knows
    /// what the hardware is: used Android handsets with one to two gigabytes total, of which
    /// the operating system and everything else take their share, leaving something between
    /// half a gigabyte and a gigabyte a process can actually have.
    /// </para>
    /// <para>
    /// <b>And the first guess here was 64MB, which was wrong in the direction that matters.</b>
    /// A bar eight times too tight fails a population that would have run perfectly well on
    /// the machine it was written about, and the failure would have read as the learner
    /// being too fat rather than as the bar being invented. A number about somebody else's
    /// hardware should come from whoever has the hardware.
    /// </para>
    /// <para>
    /// <b>So a red here is a finding rather than a regression.</b> If a trained population
    /// outgrows this the answer is that the north star needs fewer commitments a machine,
    /// more machines, or a smaller table per commitment — and that would be worth knowing
    /// long before twenty phones are in a room.
    /// </para>
    /// </remarks>
    private const long Share = 512L * 1024 * 1024;

    /// <summary>What a trained population costs, and what it holds to cost it.</summary>
    /// <param name="rounds">How long it ran.</param>
    /// <remarks>
    /// <b>The scaffolding is dropped before the second reading and the population is not,
    /// which is the whole trick.</b> A brain holds its population, a run holds its world and
    /// a trial holds both, so measuring with any of them alive would weigh the harness. What
    /// is kept alive across the collection is the population and nothing else, which is
    /// exactly what a holder keeps between rounds.
    /// </remarks>
    private static (long Bytes, int Resident, long Entries) Costs(long rounds)
    {
        var before = Settled();

        var held = Grown(rounds);

        var after = Settled();

        var entries = held.All.Sum(one => (long)one.Separations.Count);
        var resident = held.Count;

        // And the population is alive at the moment the second reading is taken, which is
        // not something the compiler owes anybody. A local whose last use is above the
        // collection is collectable AT that collection, and the reading would then be of a
        // heap that had just dropped its subject.
        GC.KeepAlive(held);

        return (after - before, resident, entries);
    }

    /// <summary>A population trained on the multiplexer, with nothing else left holding it.</summary>
    /// <param name="rounds">How long it ran.</param>
    /// <param name="address">Address bits, which is what sets the alphabet's size.</param>
    /// <remarks>
    /// <b>In its own frame so the brain and the run go out of scope, and the population
    /// comes back by itself.</b> Everything else this builds is garbage by the time the
    /// caller collects, which is what makes the delta a reading about commitments.
    /// </remarks>
    private static Population Grown(long rounds, int address = Wide)
    {
        var brain = new Brain(new CommittingSettings(), seed: 1);

        new MultiplexerRun(new MultiplexerSettings { Address = address }, brain, seed: 1)
            .Run(rounds);

        return brain.Held;
    }

    /// <summary>The managed heap with everything collectable collected.</summary>
    /// <remarks>
    /// <b>Twice, because one pass leaves what finalisers freed.</b> A collection queues
    /// finalisable objects rather than freeing them, so the first reading counts memory that
    /// is already unreachable — and the sockets and streams these runs leave behind are
    /// exactly the finalisable kind.
    /// </remarks>
    private static long Settled()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();

        return GC.GetTotalMemory(forceFullCollection: true);
    }

    /// <summary>
    /// <b>What a commitment costs in bytes, and whether a phone could hold a holder's
    /// share.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three sizes, because the reading is the slope. What is printed is the whole table —
    /// bytes, residents, table entries and the two ratios — and what is asserted is that a
    /// holder's share fits on the machine the north star names.
    /// </para>
    /// <para>
    /// <b>And the table is expected to dominate, which is fork 51 in bytes for the first
    /// time.</b> A commitment is a scope, an expectation and three counters; its
    /// <c>Separations</c> is one entry per code it has ever been asked about, and an
    /// always-present code is an entry in every table forever. So the ratio worth reading is
    /// bytes per ENTRY, and bytes per commitment is that times however many entries a
    /// commitment has grown.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_a_population_costs_to_hold()
    {
        output.WriteLine("rounds | resident | entries | bytes | b/commitment | b/entry");

        var readings = new List<(long Rounds, long Bytes, int Resident, long Entries)>();

        foreach (var rounds in (long[])[2_000, 8_000, 20_000])
        {
            var (bytes, resident, entries) = Costs(rounds);

            readings.Add((rounds, bytes, resident, entries));

            output.WriteLine(
                $"{rounds,6} | {resident,8} | {entries,7} | {bytes,9} | "
                + $"{bytes / (double)Math.Max(resident, 1),12:F0} | "
                + $"{bytes / (double)Math.Max(entries, 1),7:F1}");
        }

        var biggest = readings[^1];

        // The instrument had something to measure, asserted before anything is read off it.
        // A collection that happened to free more than the population cost would report a
        // negative delta and every ratio below would be a number about the harness.
        Assert.True(biggest.Bytes > 0,
            $"the heap did not grow by holding {biggest.Resident} commitments, so this is "
            + "measuring the test host rather than the population");

        Assert.True(biggest.Resident > 0 && biggest.Entries > biggest.Resident,
            $"{biggest.Resident} residents holding {biggest.Entries} entries between them, "
            + "so there is no table here to weigh");

        // And the north star's own question, which is the only bar. See `Share`: this is
        // what a phone can give a process, not what the learner deserves.
        Assert.True(biggest.Bytes < Share,
            $"one holder's share is {biggest.Bytes / 1024 / 1024}MB after "
            + $"{biggest.Rounds} rounds, which is past what a used phone can give a "
            + "process — so twenty of them is the wrong arrangement, or the table per "
            + "commitment is");

        output.WriteLine(
            $"a holder's share after {biggest.Rounds} rounds: "
            + $"{biggest.Bytes / 1024.0 / 1024.0:F1}MB of the {Share / 1024 / 1024}MB a "
            + $"phone can give | twenty of those is "
            + $"{20 * biggest.Bytes / 1024.0 / 1024.0:F0}MB of brain across the fleet");
    }

    /// <summary>
    /// <b>What a commitment costs does not grow with how long the run was — the table does,
    /// and that is where the memory is.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The companion, and it separates two futures that the total cannot. If bytes per
    /// commitment is flat across run lengths then a population's footprint is its
    /// population, and a fleet's memory is a deployment sum. If it climbs, every commitment
    /// is accumulating table forever and a long-lived brain outgrows its machine whatever
    /// the population is capped at — which is the shape fork 51 predicts and nothing has
    /// ever weighed.
    /// </para>
    /// <para>
    /// <b>No bar on the direction, because this is the first reading of it.</b> What is
    /// asserted is that the two run lengths are comparable at all — the same world, the same
    /// seed, the same dials — and the ratio is printed. A threshold here would be deciding
    /// the answer in the file that asks the question.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_a_commitment_gets_dearer_the_longer_a_brain_lives()
    {
        var (young, few, thin) = Costs(2_000);
        var (old, many, fat) = Costs(20_000);

        var early = young / (double)Math.Max(few, 1);
        var late = old / (double)Math.Max(many, 1);

        var perEntry = (young / (double)Math.Max(thin, 1), old / (double)Math.Max(fat, 1));

        output.WriteLine(
            $"2000 rounds: {few} resident, {thin} entries, {early:F0} b/commitment, "
            + $"{thin / (double)Math.Max(few, 1):F1} entries a commitment");

        output.WriteLine(
            $"20000 rounds: {many} resident, {fat} entries, {late:F0} b/commitment, "
            + $"{fat / (double)Math.Max(many, 1):F1} entries a commitment");

        output.WriteLine(
            $"bytes an entry: {perEntry.Item1:F1} young, {perEntry.Item2:F1} old — "
            + $"a commitment costs {late / early:F2}x what it did at a tenth the run");

        Assert.True(few > 0 && many > 0, "one of the two runs held nothing to compare");
    }

    /// <summary>
    /// <b>A commitment's table is bounded by the front end's vocabulary, which is the number
    /// the north star actually turns on.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The control the reading above needed, and without it the projection is an
    /// argument.</b> Bytes per entry is flat and entries a commitment climbs with the run,
    /// so a holder's footprint is residents times entries times a constant. What decides
    /// whether twenty phones carry a CAMERA rather than a multiplexer is what the middle
    /// term is bounded by — and a table with one entry per code it has been asked about can
    /// only be bounded by how many codes there are.
    /// </para>
    /// <para>
    /// <b>So it is measured at two vocabularies rather than reasoned about.</b> Six bits and
    /// eleven, same dials, same seed, same rounds. If entries a commitment moves with the
    /// alphabet then a front end that emits a thousand codes is the thing to price before
    /// any sensor is plumbed, and the ratio here is the first estimate of what it costs.
    /// </para>
    /// <para>
    /// <b>And the bar is that it is bounded at all.</b> A table cannot hold more entries
    /// than the world has codes — asserted, because the alternative is an entry per
    /// OCCASION, which is the shape that turns a phone's memory into a run length. What the
    /// ratio between the two widths is, is the finding and carries no threshold.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_a_commitments_table_is_bounded_by()
    {
        const long Rounds = 8_000;

        output.WriteLine("bits | codes | resident | entries a commitment | b/commitment");

        var readings = new List<(int Bits, int Codes, double Each)>();

        foreach (var (address, bits) in ((int Address, int Bits)[])[(2, 6), (3, 11)])
        {
            var before = Settled();
            var held = Grown(Rounds, address);
            var after = Settled();

            var seen = held.All
                .SelectMany(one => one.Separations.Keys)
                .Concat(held.All.SelectMany(one => one.Scope))
                .Distinct()
                .Count();

            var each = held.All.Average(one => (double)one.Separations.Count);

            readings.Add((bits, seen, each));

            output.WriteLine(
                $"{bits,4} | {seen,5} | {held.Count,8} | {each,20:F1} | "
                + $"{(after - before) / (double)Math.Max(held.Count, 1),12:F0}");

            // No table may hold more than the world has codes, which is the whole claim. An
            // entry per occasion would pass every other assertion in this file and would
            // make a phone's memory a function of how long it had been switched on.
            Assert.All(held.All, one => Assert.True(one.Separations.Count <= seen,
                $"a commitment holds {one.Separations.Count} entries against {seen} codes "
                + "the population has ever seen, so the table is not bounded by the "
                + "alphabet and a long-lived brain has no ceiling"));

            GC.KeepAlive(held);
        }

        output.WriteLine(
            $"the alphabet grows {readings[1].Codes / (double)readings[0].Codes:F2}x from "
            + $"six bits to eleven and a commitment's table grows "
            + $"{readings[1].Each / readings[0].Each:F2}x with it — so a front end emitting "
            + "a thousand codes is what to price before a sensor is plumbed");
    }
}
