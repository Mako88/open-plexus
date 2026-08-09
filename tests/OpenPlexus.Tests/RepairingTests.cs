using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What breaking the vote's hold over repair costs — <b>the other half of the lineage
/// reading, and the half that decides whether it changes anything.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b><c>LineageTests</c> SAYS THE DEFECT IS A COUPLING AND <see cref="Repairing"/> IS
/// THE DIAL THAT REMOVES IT.</b> Under <see cref="Repairing.AfterFailure"/> repair runs
/// only on a round the vote got wrong, so under skew almost all blame lands on the
/// majority lineages and the rules that would carry the hard rounds are never offered.
/// <see cref="Repairing.EveryRound"/> touches the vote in no way and takes hard-round
/// coverage from 4% to 97% at six bits.
/// </para>
/// <para>
/// <b>THE KILL CONDITION IS WRITTEN DOWN BEFORE THE RUN, BECAUSE THE PLAN ALREADY HOLDS
/// EVIDENCE AGAINST IT.</b> Fork 58 says the gate's sign flips with the timing and that
/// on <see cref="Worlds.Arranged"/> repairing without waiting for the vote minted 1,349
/// children where the waiting arm minted nine, taking a perfect withheld score to 0.752.
/// So this is a default candidate only if it costs nothing on the worlds where the vote
/// is already right; if it buys the skewed world by ruining the even ones, it is a
/// diagnostic that confirms the mechanism and not a change to what ships.
/// </para>
/// </remarks>
public sealed class RepairingTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;
    private const int Runs = 8;

    /// <summary>Seeds a budget CURVE gets, against the grid's eight.</summary>
    private const int Curve = 6;

    /// <summary>Fixed forever — see <c>Sweep</c>, whose purpose this deliberately is not.</summary>
    private const uint Purpose = 0x5EED_0065;

    private static readonly (string Name, CommittingSettings Dials)[] Arms =
    [
        ("afterfailure", new CommittingSettings()),
        ("everyround", new CommittingSettings { Repairing = Repairing.EveryRound }),
    ];

    /// <summary>
    /// <b>WHAT THE COUPLING COSTS AND WHAT REMOVING IT COSTS, ON EVERY WIDTH AND SKEW.</b>
    /// </summary>
    [Fact]
    public void Repairing_without_waiting_for_the_vote_across_the_multiplexer_grid()
    {
        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (2, 0.8), (3, 0.8) })
        {
            var settings = new MultiplexerSettings { Address = address, Skew = skew };

            output.WriteLine($"=== {address + (1 << address)} bits, skew {skew:F1}, "
                + $"{Runs} seeds");

            var taken = new Dictionary<string, Dictionary<string, Measured>>();

            foreach (var (name, dials) in Arms) taken[name] = Take(settings, dials, Runs);

            output.WriteLine(
                $"reading    {"afterfailure",-22} {"everyround",-22} sigma");

            foreach (var reading in taken["afterfailure"].Keys)
            {
                var was = taken["afterfailure"][reading];
                var now = taken["everyround"][reading];

                output.WriteLine(
                    $"{reading,-10} {Show(was),-22} {Show(now),-22} "
                    + $"{now.Separation(was),5:F1}");
            }

            output.WriteLine("");
        }
    }

    /// <summary>
    /// <b>THE WORLD THE PLAN SAYS THIS RUINS, ASKED DIRECTLY.</b>
    /// </summary>
    /// <remarks>
    /// <b>SEPARATE FROM THE GRID BECAUSE IT IS THE EXPENSIVE HALF</b>, and because a
    /// refutation arriving here says something different from a null on the multiplexer:
    /// the multiplexer says whether the arm PAYS and this says whether it may ship.
    /// </remarks>
    [Fact]
    public void Repairing_without_waiting_for_the_vote_on_the_world_it_is_predicted_to_ruin()
    {
        var small = new ArrangedSettings { Side = 3, Cell = 3, Clutter = 1, Hold = 4 };

        output.WriteLine($"=== arranged, tiled, {Runs} seeds");
        output.WriteLine($"arm          {"unseen",-22} {"sound",-22} "
            + $"{"unsound",-22} {"residents",-22} repaired");

        Measured? against = null;

        foreach (var (name, dials) in Arms)
        {
            var unseen = new List<double>();
            var sound = new List<double>();
            var unsound = new List<double>();
            var residents = new List<double>();
            var repaired = new List<double>();

            for (var index = 1; index <= Runs; index++)
            {
                var seed = Seeds.Apart(index, Purpose);
                var run = new ArrangedRun(small, new Brain(dials, seed), Looking.Tiled, seed);

                var got = run.Run(Rounds);

                unseen.Add(got.Tally.Unseen!.Accuracy);
                sound.Add(got.Rules.Sound);
                unsound.Add(got.Rules.Unsound);
                residents.Add(got.Tally.Resident);
                repaired.Add(got.Tally.Repaired);
            }

            var mine = new Measured { Arm = name, Values = unseen };

            output.WriteLine(
                $"{name,-12} {Show(mine),-22} "
                + $"{Show(new Measured { Arm = name, Values = sound }),-22} "
                + $"{Show(new Measured { Arm = name, Values = unsound }),-22} "
                + $"{Show(new Measured { Arm = name, Values = residents }),-22} "
                + $"{repaired.Average():F0}"
                + (against is null ? string.Empty : $"  ({mine.Separation(against):F1} sigma)"));

            against ??= mine;
        }
    }

    /// <summary>
    /// <b>PEAK TO PEAK, WHICH THE GRID ABOVE DOES NOT DO AND A TRAP NAMES BY NAME.</b>
    /// </summary>
    /// <remarks>
    /// <b>TWO ARMS CAN PEAK AT DIFFERENT BUDGETS, AND THESE TWO SPEND ONE AT DIFFERENT
    /// RATES BY CONSTRUCTION.</b> <see cref="Repairing.EveryRound"/> walks the culprits on
    /// every round and <see cref="Repairing.AfterFailure"/> on the wrong seventh of them,
    /// so at one fixed <see cref="CommittingSettings.Budget"/> the arms are not offered the
    /// same search — which makes the grid above a comparison at ONE point of a curve whose
    /// shape is unmeasured. Six seeds rather than eight, because this is a curve and the
    /// grid above is the reading.
    /// </remarks>
    [Fact]
    public void Whether_the_timing_still_leads_when_each_arm_is_given_its_own_budget()
    {
        const int Unlimited = int.MaxValue;

        var cells = new (string Name, int Budget)[]
        {
            ("16", 16), ("64", 64), ("free", Unlimited),
        };

        foreach (var (address, skew) in new[] { (3, 0.0), (3, 0.8) })
        {
            var settings = new MultiplexerSettings { Address = address, Skew = skew };

            output.WriteLine($"=== {address + (1 << address)} bits, skew {skew:F1}, "
                + $"{Curve} seeds");
            output.WriteLine(
                $"timing        budget  {"paying",-20} {"recent",-20} {"sound",-20} repaired");

            foreach (var (timing, repairing) in new[]
            {
                ("afterfailure", Repairing.AfterFailure),
                ("everyround", Repairing.EveryRound),
            })
            {
                foreach (var (name, budget) in cells)
                {
                    var taken = Take(
                        settings,
                        new CommittingSettings { Repairing = repairing, Budget = budget },
                        Curve);

                    output.WriteLine(
                        $"{timing,-13} {name,-6}  {Show(taken["paying"]),-20} "
                        + $"{Show(taken["recent"]),-20} {Show(taken["sound"]),-20} "
                        + $"{taken["repaired"].Mean:F0}");
                }
            }

            output.WriteLine("");
        }
    }

    /// <summary>
    /// <b>WHETHER THE TWO THINGS THAT REDIRECT BLAME ARE ONE THING, WHICH DECIDES WHAT
    /// SHIPS.</b>
    /// </summary>
    /// <remarks>
    /// <b>IF BOTH ACT ON THE SAME COUPLING THEY DO NOT ADD, AND THAT IS THE PREDICTION.</b>
    /// <see cref="Weighing.Lifting"/> reaches the blame by making the vote say the rare
    /// answer and <see cref="Repairing.EveryRound"/> by not consulting the vote at all, so
    /// under the second the first has nothing left to redirect. A full two-by-two says
    /// whether that is right — and it matters for what ships, because
    /// <see cref="Weighing.Lifting"/> carries a cost the plan wrote down before it was ever
    /// run: a rare expectation divides by a small number, so it prefers an unusual answer
    /// on thin evidence. A dial kept for a job another dial already does is a cost with no
    /// benefit.
    /// </remarks>
    [Fact]
    public void Whether_lifting_still_buys_anything_once_repair_stops_waiting_for_the_vote()
    {
        foreach (var (address, skew) in new[] { (2, 0.8), (3, 0.8) })
        {
            var settings = new MultiplexerSettings { Address = address, Skew = skew };

            output.WriteLine($"=== {address + (1 << address)} bits, skew {skew:F1}, "
                + $"{Curve} seeds");
            output.WriteLine(
                $"timing        weighing  {"paying",-20} {"recent",-20} "
                + $"{"found",-20} sound");

            foreach (var (timing, repairing) in new[]
            {
                ("afterfailure", Repairing.AfterFailure),
                ("everyround", Repairing.EveryRound),
            })
            {
                foreach (var weighing in new[] { Weighing.Summing, Weighing.Lifting })
                {
                    var taken = Take(
                        settings,
                        new CommittingSettings { Repairing = repairing, Weighing = weighing },
                        Curve);

                    output.WriteLine(
                        $"{timing,-13} {weighing,-8}  {Show(taken["paying"]),-20} "
                        + $"{Show(taken["recent"]),-20} {Show(taken["found"]),-20} "
                        + $"{taken["sound"].Mean:F1}");
                }
            }

            output.WriteLine("");
        }
    }

    /// <summary>
    /// <b>WHETHER THE NEW TIMING STARVES RUNG FIVE, WHICH A PRECONDITION FOUND BY
    /// FAILING.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>`AskedTests` ASSERTS THAT A TRAINED POPULATION HAS SOMETHING WORTH NAMING BEFORE
    /// IT TESTS THE WIRE, AND UNDER <see cref="Repairing.EveryRound"/> IT HAS NOT.</b>
    /// <see cref="Abstracting.Shared"/> came back empty at eleven bits, which is a fact
    /// about the population rather than about the socket — and it is the one cost of this
    /// timing that no grid here would have shown, because every reading taken so far is
    /// about rules and none is about NAMES.
    /// </para>
    /// <para>
    /// <b>AND THE PLAN PUTS ABSTRACTION SECOND IN THE WHOLE ORDER OF WORK</b> — <i>if
    /// abstraction fails nothing downstream matters</i> — so a default that buys coverage
    /// and costs naming is not obviously a win, whatever the score does. This measures it
    /// rather than reasoning from the one seed the precondition happened to use.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_the_new_timing_costs_the_rung_that_mints_names()
    {
        foreach (var address in new[] { 2, 3 })
        {
            output.WriteLine($"=== {address + (1 << address)} bits even, {Curve} seeds");
            output.WriteLine(
                $"timing        {"named",-20} {"stacked",-20} {"residents",-20} "
                + "seeds naming something");

            foreach (var (timing, repairing) in new[]
            {
                ("afterfailure", Repairing.AfterFailure),
                ("everyround", Repairing.EveryRound),
            })
            {
                var taken = Take(
                    new MultiplexerSettings { Address = address },
                    new CommittingSettings { Repairing = repairing },
                    Curve);

                // SEEDS RATHER THAN A MEAN, BECAUSE A MEAN OF NINE NAMES IS COMPATIBLE
                // WITH FIVE SEEDS NAMING NOTHING. The precondition that found this had one
                // seed and read its silence as the rung being dead; what says otherwise is
                // how many seeds name at all, and it is a different question from how many
                // names they mint.
                var any = taken["named"].Values.Count(one => one > 0);

                output.WriteLine(
                    $"{timing,-13} {Show(taken["named"]),-20} {Show(taken["stacked"]),-20} "
                    + $"{Show(taken["residents"]),-20} {any} of {Curve}");
            }

            output.WriteLine("");
        }
    }

    /// <summary>Every reading one arm produces on one world, across the seeds.</summary>
    /// <param name="settings">The world.</param>
    /// <param name="dials">The arm.</param>
    /// <remarks>
    /// <b>THE SEEDS ARE MIXED AND THE ARMS SHARE THEM</b>, which is what makes the second
    /// column a control rather than a second experiment — see <c>Sweep.ArmAsync</c>, whose
    /// discipline this borrows and whose purpose word it deliberately does not reuse.
    /// </remarks>
    /// <summary>
    /// <b>WHETHER THE NAMES COME BACK WITH THE RULES ONCE THE BUDGET STOPS BINDING.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE NAMING COST AND THE SMALLER POPULATION ARE THE SAME OBSERVATION, AND ONE OF
    /// THEM IS ALREADY KNOWN TO BE THE BUDGET'S.</b> <see cref="Repairing.EveryRound"/>
    /// walks the culprits every round rather than on the wrong seventh, so a parent spends
    /// its ATTEMPTS about seven times faster — and `BudgetingTests` shows that with the
    /// budget counting distinct children instead, the same timing holds 126.6 sound rules
    /// against 99.6 rather than fewer. Rung five names a sub-scope that enough commitments
    /// share, so if the missing names were missing residents they come back here; if they
    /// do not, the naming cost is the timing's own and belongs beside it as a price.
    /// </remarks>
    [Fact]
    public void Whether_the_names_return_when_the_budget_counts_children()
    {
        output.WriteLine($"=== 11 bits even, {Curve} seeds");
        output.WriteLine(
            $"timing        budget    {"named",-20} {"stacked",-20} "
            + $"{"residents",-20} {"sound",-20} seeds naming");

        foreach (var (timing, repairing) in new[]
        {
            ("afterfailure", Repairing.AfterFailure),
            ("everyround", Repairing.EveryRound),
        })
        {
            foreach (var (name, budgeting) in new[]
            {
                ("attempts", Budgeting.Attempts),
                ("children", Budgeting.Children),
            })
            {
                var taken = Take(
                    new MultiplexerSettings { Address = 3 },
                    new CommittingSettings { Repairing = repairing, Budgeting = budgeting },
                    Curve);

                var any = taken["named"].Values.Count(one => one > 0);

                output.WriteLine(
                    $"{timing,-13} {name,-8}  {Show(taken["named"]),-20} "
                    + $"{Show(taken["stacked"]),-20} {Show(taken["residents"]),-20} "
                    + $"{Show(taken["sound"]),-20} {any} of {Curve}");
            }
        }
    }

    /// <param name="runs">How many seeds, since a curve gets fewer than a grid.</param>
    private static Dictionary<string, Measured> Take(
        MultiplexerSettings settings, CommittingSettings dials, int runs)
    {
        var readings = new Dictionary<string, List<double>>
        {
            ["paying"] = [], ["recent"] = [], ["found"] = [],
            ["sound"] = [], ["unsound"] = [], ["residents"] = [], ["repaired"] = [],
            ["named"] = [], ["stacked"] = [],
        };

        for (var index = 1; index <= runs; index++)
        {
            var seed = Seeds.Apart(index, Purpose);
            var run = new MultiplexerRun(settings, new Brain(dials, seed), seed, census: true);

            var learnt = run.Run(Rounds);

            readings["paying"].Add(learnt.Census!.Paying);
            readings["recent"].Add(learnt.Recent);
            readings["found"].Add(learnt.Found);
            readings["sound"].Add(learnt.Sound);
            readings["unsound"].Add(learnt.Unsound);
            readings["residents"].Add(learnt.Resident);
            readings["repaired"].Add(learnt.Repaired);
            readings["named"].Add(learnt.Named);
            readings["stacked"].Add(learnt.Stacked);
        }

        return readings.ToDictionary(
            one => one.Key,
            one => new Measured { Arm = one.Key, Values = one.Value });
    }

    /// <summary>A mean and its standard error, in one column.</summary>
    /// <param name="measured">What was taken.</param>
    private static string Show(Measured measured) =>
        $"{measured.Mean,9:F3} +/- {measured.StdErr:F3}";

}
