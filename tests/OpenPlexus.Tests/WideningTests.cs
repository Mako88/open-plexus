using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The ladder's missing direction — <b>whether anything is gained by making a scope
/// SHORTER.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE PLAN HAS SAID FOR THE LIFE OF THE BRANCH THAT A SPECIALISE-ONLY MACHINE IS
/// CONCEPTLESS, AND THE COST IS MEASURED NOW.</b> On the skewed multiplexer the machine
/// holds sound rules at perfect accuracy that fire on not one of nearly four thousand
/// rounds the base rate gets wrong. True, never mistaken, and too narrow to pay — which
/// is precisely what a population with no way back up the ladder looks like from outside.
/// </para>
/// <para>
/// <b>AND `Paying` IS WHAT THIS IS JUDGED ON, NOT `Found` OR ACCURACY.</b> Generalisation
/// makes rules fire MORE, so it will move a resident count and a repair count whatever it
/// is worth; and it could raise accuracy by covering the easy rounds more thoroughly
/// while buying nothing where guessing already fails. The only reading that cannot be had
/// that way is whether a true rule turns up on a round the base rate misses.
/// </para>
/// <para>
/// <b>AND THE COST IS SAID BEFORE THE RUN.</b> A scope of length k proposes k shortened
/// rules, so a population of narrow commitments can propose a great many at once — and a
/// generalisation that is WRONG fires on more moments than the rule it came from, which
/// is a bad rule with a wider reach. If the unsound count climbs faster than
/// <see cref="Census.Paying"/> does, that is the mechanism failing rather than a dial
/// wanting a turn.
/// </para>
/// </remarks>
public sealed class WideningTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    private const int Seeds = 8;

    /// <param name="address">Address bits.</param>
    /// <param name="skew">How often a data bit is one, or zero to leave them even.</param>
    /// <param name="widening">Whether anything shortens a scope.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    /// <param name="capacity">How many commitments the machine may hold.</param>
    private static Learned Run(
        int address, double skew, Widening widening, int seed, int? capacity = null) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address, Skew = skew },
            new Brain(
                new CommittingSettings
                {
                    Widening = widening,

                    // PINNED AT WHAT SHIPS RATHER THAN LEFT TO INHERIT IT, because this grid
                    // is being re-taken precisely BECAUSE those two moved. A fixture that
                    // inherits the dial whose change prompted the re-take cannot say which
                    // machine its rows are about the next time one of them moves again.
                    Forking = Forking.Distinct,
                    Budget = 8,

                    // AND THE DEFAULT IS SPELT OUT RATHER THAN INHERITED FOR THE SAME REASON
                    // ONE LINE UP, since the grid below is entirely about this number.
                    Capacity = capacity ?? new CommittingSettings().Capacity,
                },
                seed),
            seed,
            census: true).Run(Rounds);

    /// <summary>
    /// <b>WHETHER GENERALISATION REACHES THE ROUNDS GUESSING MISSES.</b>
    /// </summary>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task Whether_shortening_a_scope_reaches_what_specialising_cannot()
    {
        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (2, 0.8), (3, 0.8) })
        {
            output.WriteLine($"--- {address + (1 << address)} bits, skew {skew:F1} ---");

            foreach (var widening in new[] { Widening.Never, Widening.Unmissed })
                await Report($"{widening}", seed => Run(address, skew, widening, seed));
        }
    }

    /// <summary>Every column of one cell, over the seeds, from one population a seed.</summary>
    /// <param name="cell">What to print the rows under.</param>
    /// <param name="run">How to build the population for a seed.</param>
    /// <remarks>
    /// <b>THE CACHE IS THE POINT AND NOT A TIDINESS.</b> A grid asking `Sweep` for its runs
    /// per COLUMN builds every population once a column, so one measurement is paid for
    /// seven times and the column that would rank the arms looks too expensive to add. That
    /// is a line on this repo's trap list, and it is written once here so a second grid in
    /// this file cannot reintroduce it by copying the wrong half.
    /// </remarks>
    private async Task Report(string cell, Func<int, Learned> run)
    {
        var once = new Dictionary<int, Learned>();

        Learned Cached(int seed)
        {
            if (!once.TryGetValue(seed, out var learned))
                once[seed] = learned = run(seed);

            return learned;
        }

        foreach (var reading in new (string What, Func<Learned, double> Of)[]
        {
            ("paying", one => one.Census!.Paying),
            ("recent", one => one.Recent),
            ("sound", one => one.Sound),

            // WHAT THIS OPERATOR EXISTS TO REMOVE, AND THE FIRST GRID THAT COULD SAY SO. A
            // sound rule containing a shorter sound rule fires on fewer moments than a truth
            // it already holds, which is the reach problem stated as a property of the
            // population rather than as a score. If shortening scopes is worth anything at
            // all, this is the column it is worth it ON.
            ("overshot", one => one.Overshot),
            ("unsound", one => one.Unsound),
            ("residents", one => one.Resident),
            ("widened", one => one.Tally.Widened),
            ("subsumed", one => one.Tally.Subsumed),
        })
        {
            var arm = await Sweep.ArmAsync(
                reading.What,
                Seeds,
                seed => Task.FromResult(reading.Of(Cached(seed))));

            output.WriteLine(
                $"  {cell,-20} {reading.What,-10} | {arm.Mean,10:F3} "
                + $"+/-{arm.StdErr,8:F3} | n={arm.Seeds}");
        }

        output.WriteLine("");
    }

    /// <summary>
    /// <b>THE ARM IS CONNECTED, asserted as a difference and never as a direction.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE TRAP THIS GUARDS COST THIS SESSION A FOURTEEN-MINUTE GRID AND A WRONG
    /// WRITE-UP.</b> A mechanism that is off and one that is on but inert report the same
    /// everything, so a new operator is asserted to FIRE before any grid it appears in is
    /// worth reading. Whether firing is an improvement is the test above's question.
    /// </remarks>
    [Fact]
    public void Generalisation_proposes_something_when_it_is_switched_on()
    {
        var off = Run(2, skew: 0.0, Widening.Never, seed: 1);
        var on = Run(2, skew: 0.0, Widening.Unmissed, seed: 1);

        output.WriteLine($"never: {off.Tally.Widened} proposed, {off.Resident} residents");
        output.WriteLine($"unmissed: {on.Tally.Widened} proposed, {on.Resident} residents");

        Assert.Equal(0, off.Tally.Widened);

        Assert.True(on.Tally.Widened > 0,
            "generalisation is switched on and proposed nothing, so either no commitment "
            + "ever reaches a scope of two with no misses, or the operator is not wired");
    }

    /// <summary>
    /// <b>WHETHER WIDENING'S ACCURACY COST IS THE SHORTENED RULES OR THE CEILING THEY
    /// PUSH THE POPULATION INTO.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE GRID ABOVE HOLDS 2001 RESIDENTS OF A CAPACITY OF 2000 AT BOTH ELEVEN-BIT
    /// WIDTHS, AND 302 AT SIX.</b> So the arm is pinned at the ceiling exactly where it
    /// costs a tenth of the trailing accuracy and free of it where it costs least — which
    /// is a confound rather than a finding, because this repo has already measured that
    /// what is damage is REACHING a capacity rather than holding more.
    /// </para>
    /// <para>
    /// <b>AND THE TWO STORIES PREDICT OPPOSITE THINGS OF ONE CONTROL.</b> If the cost is
    /// the shortened rules being wrong, raising the ceiling changes nothing about accuracy
    /// and merely lets more wrong rules sit resident. If the cost is eviction, the
    /// shortened rules were never the problem: a flood of blind proposals sorts at the
    /// median of the judged and pushes earned rules out from below, and room removes it.
    /// </para>
    /// <para>
    /// <b>CROSSED RATHER THAN MEASURED OFF THE ARM, because raising a ceiling moves the
    /// baseline too.</b> A bigger population without widening is a different machine, and
    /// comparing a widened arm at one capacity against an unwidened one at another would
    /// move both axes — the trap this repo has now paid for four times.
    /// </para>
    /// <para>
    /// <b>AND <c>residents</c> AGAINST THE CAPACITY IS WHAT SAYS THE MECHANISM RAN, WHICH
    /// NO SCORE CAN.</b> A ceiling that is never reached and one that evicts constantly
    /// read the same in every other column, and this grid is unreadable unless the raised
    /// arm comes back UNDER its own ceiling — otherwise 8000 is another pinned machine and
    /// the control never happened.
    /// </para>
    /// </remarks>
    /// <param name="address">Address bits.</param>
    /// <param name="skew">How often a data bit is one, or zero to leave them even.</param>
    [Theory]
    [InlineData(3, 0.0)]
    [InlineData(3, 0.8)]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task Whether_the_cost_of_widening_is_the_ceiling_it_reaches(
        int address, double skew)
    {
        output.WriteLine($"--- {address + (1 << address)} bits, skew {skew:F1} ---");

        foreach (var capacity in new[] { 2_000, 8_000 })
            foreach (var widening in new[] { Widening.Never, Widening.Unmissed })
                await Report(
                    $"cap={capacity} {widening}",
                    seed => Run(address, skew, widening, seed, capacity));
    }

    /// <summary>
    /// <b>THE OVERSHOOT READING FIRES, AND IT IS NOT THE SAME NUMBER AT BOTH WIDTHS.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A COLUMN THAT IS ALWAYS NOUGHT IS A CHECK THAT CANNOT FIRE</b>, which this repo
    /// has now found twice by arming something that had always read zero. So the instrument
    /// is asserted to have a subject before any row above is read as a comparison.
    /// </para>
    /// <para>
    /// <b>AND IT IS ONE SEED A WIDTH ON PURPOSE, because this asks whether the number
    /// EXISTS and not how big it is.</b> How much a population overshoots, and whether
    /// shortening scopes reduces it, is the grid's question and carries error bars there.
    /// </para>
    /// <para>
    /// <b>WHAT IT WOULD MEAN FOR THE TWO TO BE EQUAL IS WORTH SAYING FIRST.</b> Repair
    /// refuses a parent under <c>Floor</c> misses and a sound rule on a clean world never
    /// misses, so a chain that only ever narrows what is still wrong cannot pass a sound
    /// depth by that route — it can only arrive at one along a lineage that was never sound.
    /// A width where this reads nought is a width where every route was short enough for
    /// that to be the whole story.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_overshoot_reading_has_a_subject_before_any_row_is_read()
    {
        var narrow = Run(2, skew: 0.0, Widening.Never, seed: 1);
        var wide = Run(3, skew: 0.0, Widening.Never, seed: 1);

        output.WriteLine(
            $"6 bits: {narrow.Overshot} of {narrow.Sound} sound over-specialised");
        output.WriteLine(
            $"11 bits: {wide.Overshot} of {wide.Sound} sound over-specialised");

        Assert.True(wide.Overshot > 0,
            "no sound commitment at eleven bits contains a shorter sound one, so either the "
            + "chain always stops at a minimum or the reading is not wired");

        Assert.NotEqual(narrow.Overshot, wide.Overshot);
    }

    /// <summary>
    /// <b>THE GATE THIS OPERATOR'S OWN ROW HAS ALWAYS NAMED IS PAID BY
    /// <see cref="CommittingSettings.Floor"/> BEFORE IT IS ASKED.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE PROPOSAL WAS TO REFUSE A CLEAN RECORD THAT IS NOT SIGNIFICANT AGAINST THE
    /// BASE RATE</b>, by the pooled two-proportion z repair already owns and at the same
    /// <see cref="CommittingSettings.Alpha"/> — <i>zero misses over twenty firings is not
    /// the same claim as zero over four hundred</i>. It was built, and it is bit-identical
    /// to the ungated arm on all four cells of the grid above: 861 and 6058 and 12813
    /// proposals, the same residents, the same <c>Paying</c>, the same trailing accuracy.
    /// </para>
    /// <para>
    /// <b>AND THE REASON IS ARITHMETIC RATHER THAN A WORLD, WHICH IS WHY THIS IS A CHECK
    /// AND NOT A ROW.</b> <see cref="Population.Widen"/> already refuses anything under
    /// <see cref="CommittingSettings.Floor"/> settlements, and a perfect record over n
    /// firings clears a one-sided bar at <c>alpha</c> for every base rate below roughly
    /// <c>n / (n + 2.71)</c>. At the shipped floor of twenty that is 0.88, and the most
    /// skewed world on this bench draws four in five.
    /// </para>
    /// <para>
    /// <b>SO THE REVIVAL CONDITION IS A NUMBER RATHER THAN A HOPE, AND THIS IS WHAT WOULD
    /// SPOT IT.</b> The day a world's commonest outcome passes the boundary below, or the
    /// day the floor drops far enough to move the boundary under a world already here, the
    /// gate stops being inert and is worth building again. Until then it is a second name
    /// for <see cref="Widening.Unmissed"/>, and a grid of identical rows is this repo's own
    /// trap about a bench with no question.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_gate_the_row_names_is_paid_by_the_miss_floor_before_it_is_asked()
    {
        var dials = new CommittingSettings();

        // A CLEAN RECORD AT EXACTLY THE FLOOR, WHICH IS THE THINNEST ONE THE OPERATOR CAN
        // EVER SEE. Anything it would refuse it would refuse here first.
        var thinnest = dials.Floor;

        double Refused(double rate)
        {
            const long Trials = 1_000_000L;

            return Normal.Tail(
                Repair.Ahead(thinnest, thinnest, (long)(rate * Trials), Trials));
        }

        foreach (var rate in new[] { 0.5, 0.8, 0.85, 0.9, 0.95 })
            output.WriteLine($"  base rate {rate:F2} -> p={Refused(rate):F4}");

        // THE WORLDS THIS BENCH HAS, NAMED RATHER THAN ASSUMED. `Multiplexer`'s answer is a
        // data bit, so its commonest outcome IS the skew, and 0.8 is the steepest tilt any
        // grid here runs.
        Assert.True(Refused(0.5) <= dials.Alpha);
        Assert.True(Refused(0.8) <= dials.Alpha,
            "the steepest world on this bench now refuses a clean record at the floor, so "
            + "the gate is no longer inert and `Widening.Significant` is worth building "
            + "again -- see the revival row");

        // AND WHERE IT WOULD BITE, so the check fails from BOTH sides. A bar that refuses
        // nothing anywhere is not a bar, and asserting only the inert half would pass for
        // free if `Ahead` ever returned a constant.
        Assert.True(Refused(0.95) > dials.Alpha);

        // THE BOUNDARY ITSELF, WHICH IS THE NUMBER THE REVIVAL ROW CITES. Below it the
        // floor has already paid for the significance; above it the gate has something to
        // say. It moves with the floor and with nothing else.
        var boundary = thinnest / (thinnest + 2.71);

        output.WriteLine($"boundary at floor {thinnest}: {boundary:F4}");

        Assert.True(Refused(boundary - 0.02) <= dials.Alpha);
        Assert.True(Refused(boundary + 0.02) > dials.Alpha);
    }
}
