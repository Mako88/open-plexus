using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whole against tiled, on the world where the difference can show.
/// </summary>
/// <remarks>
/// <b>THE MEASUREMENT THE PLAN HAS BEEN OWED SINCE STEP FOUR.</b> A pooled embedding
/// has no parts and cannot carry an arrangement, and a whole-picture projection is the
/// same thing by another road. Patch tokens are named as the fix; here is the first
/// world on which the claim can be wrong.
/// </remarks>
public sealed class ArrangingTests(ITestOutputHelper output)
{
    private static readonly ArrangedSettings Small =
        new() { Side = 3, Cell = 3, Clutter = 1, Hold = 4 };

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_an_unbounded_repair_budget_costs_on_the_world_that_reaches_the_capacity()
    {
        // THE GAP THE BUDGET CURVE LEFT, AND IT IS NAMED IN THAT COMMIT RATHER THAN FOUND
        // AFTERWARDS. `BudgetCurveTests` says every column that is a fact about what was
        // learnt rises monotonically to a FREE budget -- and every cell of it is a
        // multiplexer, where the population never comes near `Capacity`. A dial wired to one
        // world in ten and cashed in as though it were general is this doc's own trap.
        //
        // `Arranged` IS WHERE IT WOULD SHOW. Five hundred residents against a capacity of two
        // thousand is closer than a multiplexer ever gets, culling actually runs, and the
        // world's true rules are ONE CODE -- so this doc already carries the row that on a
        // world whose rules are one code, any repair is damage. If unbounded repair costs
        // anything anywhere, it costs it here.
        //
        // AND THE READING IS THE WITHHELD SET RATHER THAN A TRAILING WINDOW, which is what
        // this world has that the multiplexer does not: scenes the run was never taught on.
        // A population that over-specialises scores on what it has seen and not on those.
        output.WriteLine("budget | unseen accuracy | spread | sound | unsound | residents");

        foreach (var (arm, dials) in new (string Arm, CommittingSettings Dials)[]
        {
            ("256", new CommittingSettings { Surprising = Surprising.AnyFailure }),
            ("free", new CommittingSettings
            {
                Surprising = Surprising.AnyFailure,
                Budget = int.MaxValue,
            }),

            // AND THE ARM WHOSE DISTINGUISHING PROPERTY ONLY APPEARS HERE. `Earned` pays one
            // attempt per `Floor` misses, so it binds where misses are SCARCE -- and on the
            // multiplexer they are not, which is why it came back indistinguishable from free
            // on every cell of two worlds. This world's true rules are one code and its
            // accuracy is high, so its parents are wrong far less often. If the rule binds
            // anywhere on this bench it binds here, and if it does not it is a third off
            // switch and goes the way of `Children`.
            ("earned", new CommittingSettings
            {
                Surprising = Surprising.AnyFailure,
                Budgeting = Budgeting.Earned,
            }),
        })
        {
            var (unseen, last) = Sweep(Small, dials, Looking.Tiled);

            output.WriteLine(
                $"{arm,6} | {unseen.Average(),15:F3} | {Spread(unseen),6:F3} "
                + $"| {last.Rules.Sound,5} | {last.Rules.Unsound,7} "
                + $"| {last.Tally.Resident,9}");
        }

        // NO BAR. What unbounded repair costs on a world that reaches its capacity has never
        // been measured, and a threshold written before the first reading would be the answer
        // rather than the finding.
    }

    /// <summary>Five seeds of one configuration, and what the last one left behind.</summary>
    /// <param name="world">The scene the seeds are drawn from.</param>
    /// <param name="dials">The brain, built once and handed to every seed.</param>
    /// <param name="looking">How the picture is cut up.</param>
    /// <remarks>
    /// <b>ONE BRAIN PER CONFIGURATION AND FIVE SEEDS OF IT, because one seed is not a
    /// comparison and this repo has watched an ordering invert.</b> Written out twice
    /// before `DuplicationTests` refused the second, which is that budget doing its job
    /// on a measurement file rather than on the library.
    /// </remarks>
    private static (List<double> Unseen, Grounded Last) Sweep(
        ArrangedSettings world, CommittingSettings dials, Looking looking)
    {
        var unseen = new List<double>();
        var last = default(Grounded);

        foreach (var seed in new[] { 1, 2, 3, 4, 5 })
        {
            last = new ArrangedRun(world, new Brain(dials, seed), looking, seed).Run(20_000);
            unseen.Add(last.Tally.Unseen!.Accuracy);
        }

        return (unseen, last!);
    }

    /// <summary>The standard error of a handful of readings.</summary>
    private static double Spread(List<double> readings)
    {
        var mean = readings.Average();

        return Math.Sqrt(
            readings.Sum(one => (one - mean) * (one - mean)) / (readings.Count - 1))
            / Math.Sqrt(readings.Count);
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void The_gap_between_what_it_was_shown_and_what_it_was_not()
    {
        foreach (var rounds in new[] { 10_000, 40_000 })
        foreach (var looking in new[] { Looking.Whole, Looking.Tiled })
        foreach (var seed in new[] { 1, 2, 3 })
        {
            var run = new ArrangedRun(
                Small, new Brain(new CommittingSettings(), seed), looking, seed);

            var got = run.Run(rounds);
            var bar = run.Measure();

            output.WriteLine(
                $"rounds {rounds} {looking,-6} seed {seed} | drawn {got.Tally.Recent:F3} "
                + $"unseen {got.Tally.Unseen!.Accuracy:F3} "
                + $"silence {got.Tally.Unseen.Silence:F3} | "
                + $"codes {got.Tally.Codes:F0} resident {got.Tally.Resident} "
                + $"minted {got.Tally.Minted} repaired {got.Tally.Repaired} "
                + $"named {got.Tally.Named} | "
                + $"sound {got.Rules.Sound} unsound {got.Rules.Unsound} inert {got.Rules.Inert} | "
                + $"tags {got.Tags}/{got.Readings} | "
                + $"probe pixels {bar.OnPixels.Accuracy:F3} codes {bar.OnCodes.Accuracy:F3} "
                + $"over {bar.Features} features, {bar.OnPixels.Trained} fitted "
                + $"{bar.OnPixels.Tested} scored");
        }

        // NO BAR, DELIBERATELY. Nobody knows what either arm scores here yet, and a
        // threshold written before the first run is a prediction dressed as a check --
        // which is how a measurement quietly becomes a thing that must not change.
        Assert.True(true);
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_the_vote_costs_when_the_population_already_knows_which_rules_are_true()
    {
        // THE ONE ARM WITH A CLEAN TARGET, SWEPT ON THE ONE DIAL THE PLAN NAMED. Tiled
        // under `AnyFailure` holds every sound single-code rule on every seed, the
        // front end loses nothing a linear probe can find, and the language covers the
        // whole world at depth one. Everything is in place and it scores 0.800.
        //
        // AND THE POPULATION IS NOT CONFUSED ABOUT WHICH RULES ARE TRUE -- it believes
        // the sound ones 1.000 and the unsound ones 0.522. So a crowd of rules that
        // LOOK worse is outvoting a handful that look perfect, which is what raising
        // accuracy to a power exists to stop, and five is evidently not enough of it:
        // 0.522^5 is 0.039, and a few dozen of those agreeing beat one weight of 1.
        //
        // SO THIS IS A PREDICTION WITH A NUMBER ON IT. If the vote is the gap, the
        // score climbs toward 1.000 as the power rises. If it does not, the gap is
        // somewhere nobody has looked yet and this rules out the obvious place.
        var could = new ArrangedRun(
            Small, new Brain(new CommittingSettings(), seed: 1), Looking.Tiled, seed: 1)
            .Reachable(depth: 1);

        output.WriteLine(
            $"target {could.CoversUnseen:F3} on the unseen, from {could.Alone.Length} "
            + $"codes sound alone, {could.Least} of them enough");

        // AND THE QUESTION IS SETTLED RATHER THAN SWEPT NOW. This crossed a summed vote at
        // five powers against the scale-free one, to ask whether the peak moves with the
        // world. Both the sum and the power are deleted -- the sum led on no world of ten --
        // so what is left is one cell, and it is the cell that used to be the answer.
        {
            var (unseen, last) = Sweep(
                Small,
                new CommittingSettings { Surprising = Surprising.AnyFailure },
                Looking.Tiled);

            output.WriteLine(
                $"  unseen {unseen.Average():F3} +/- "
                + $"{Spread(unseen):F3} | "
                + $"[{string.Join(" ", unseen.Select(one => one.ToString("F3")))}] | "
                + $"last run: {last.Rules.Sound} sound {last.Rules.Unsound} unsound, "
                + $"believed {last.Rules.Trusted:F3} vs {last.Rules.Doubted:F3}, "
                + $"lead {last.Tally.Confidence:F3}");
        }

        Assert.True(true);
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void And_the_other_half_of_the_decoupling_where_the_target_is_known()
    {
        // THE SIDE OF THE PREDICTION THAT MUST NOT MOVE. Repair on this world is not
        // the constraint -- the rules that solve it are one code each and genesis mints
        // them directly -- so letting repair run on more rounds should change nothing
        // and the score should stay at the target. If it FALLS, the extra gate was
        // holding back damage rather than search, and the whole argument inverts.
        // AND THE OCCASION COUNT IS REPORTED BESIDE THE SCORE BECAUSE OF WHAT IT
        // SETTLED HERE. A third subsumption rule weighing a child's advantage against
        // the DISTINCT moments it stands on -- built to delete children that had
        // memorised a corner of the drawn bag -- deleted an ordinary share of them and
        // reached the identical withheld score on all five seeds. What sinks this cell
        // is not children standing on one repeated scene.
        // THE THREE CELLS THIS GRID WAS TAKEN OVER, AS THE PAIRS THEY TURNED OUT TO BE.
        // `Mending` was one setting deciding a gate and a timing at once; the arms here are
        // unchanged, and `Fixture.Repairs` is what keeps that true across the four files
        // that sweep them.
        foreach (var (arm, gate, when) in Fixture.Repairs.Where(one => one.Arm != "after failure, gate"))
        foreach (var subsuming in new[] { Subsuming.Weaker, Subsuming.Insignificant })
        {
            var (unseen, last) = Sweep(
                Small,
                new CommittingSettings
                {
                    Surprising = Surprising.AnyFailure,
                    Mending = gate,
                    Repairing = when,
                    Subsuming = subsuming,
                },
                Looking.Tiled);

            output.WriteLine(
                $"{arm,-23} {subsuming,-13} | unseen {unseen.Average():F3} "
                + $"[{string.Join(" ", unseen.Select(one => one.ToString("F3")))}] | "
                + $"{last.Rules.Sound} sound {last.Rules.Unsound} unsound, "
                + $"repaired {last.Tally.Repaired} subsumed {last.Tally.Subsumed}, "
                + $"resident {last.Tally.Resident} scope {last.Rules.Scope:F2} "
                + $"occasions {last.Tally.Occasions:F1}, "
                + $"deciders {last.Tally.Unseen!.Deciders}/{last.Tally.Unseen.Answered} "
                + $"lead {last.Tally.Confidence:F3}");
        }

        Assert.True(true);
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_the_gap_is_a_fact_about_the_learner_or_about_how_much_it_was_shown()
    {
        // THREE INSTRUMENTS HAVE NOW SAID THE SAME THING AND NONE OF THEM SAID WHOSE
        // FAULT IT IS. The children that cost this world a quarter of its score are not
        // memorised, the population is not what is read, and handing every seat back to
        // a general rule changes not one answer -- because those deciders ARE
        // significantly better on what they were shown. A rule true of the drawn
        // arrangements and false of the withheld ones has a perfect observed record, and
        // no statistic over drawn data can see it.
        //
        // WHICH MAKES THE NEXT QUESTION ABOUT THE EXAM RATHER THAN THE MACHINE. `Hold`
        // withholds every nth arrangement, so a LARGER value draws more of the world. If
        // the gap is what the drawn set cannot distinguish, it closes as this rises. If
        // it does not, the deciders are wrong for a reason coverage does not touch, and
        // that rules out the last explanation this session has left.
        //
        // NOT COMPARABLE CELL TO CELL, AND SAYING SO IS THE POINT. Each value withholds a
        // different set, so these are different exams -- what is readable is the
        // DIRECTION and whether the drawn score moves with it.
        foreach (var hold in new[] { 2, 4, 8, 16 })
        foreach (var (arm, gate, when) in Fixture.Repairs
            .Where(one => one.Arm is "after failure, no gate" or "every round, gate"))
        {
            var (unseen, last) = Sweep(
                Small with { Hold = hold },
                new CommittingSettings
                {
                    Surprising = Surprising.AnyFailure,
                    Mending = gate,
                    Repairing = when,
                    Subsuming = Subsuming.Insignificant,
                },
                Looking.Tiled);

            output.WriteLine(
                $"hold {hold,2} {arm,-23} | unseen {unseen.Average():F3} "
                + $"[{string.Join(" ", unseen.Select(one => one.ToString("F3")))}] | "
                + $"drawn {last.Tally.Recent:F3} | "
                + $"{last.Rules.Sound} sound {last.Rules.Unsound} unsound, "
                + $"resident {last.Tally.Resident} scope {last.Rules.Scope:F2}, "
                + $"deciders {last.Tally.Unseen!.Deciders}/{last.Tally.Unseen.Answered}");
        }

        Assert.True(true);
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_the_gate_that_was_load_bearing_on_photographs_is_one_here()
    {
        // ONE SEED IS NOT A COMPARISON AND WILL HAPPILY INVERT, which this repo has
        // already paid for once -- winnowing beat bands on seed one and lost over five.
        // The single-seed reading says `Unaccounted` starves genesis on this world,
        // which is the OPPOSITE of what five seeds said on CIFAR, so it gets error bars
        // before it gets written down as anything.
        foreach (var looking in new[] { Looking.Whole, Looking.Tiled })
        {
            // HOISTED, BECAUSE THE CEILING IS A FACT ABOUT THE WORLD AND THE FRONT END
            // AND NEITHER MOVES WITH THE SEED. Recomputing it per run would spend most
            // of the grid's time confirming the same number twenty times.
            var could = new ArrangedRun(
                Small, new Brain(new CommittingSettings(), seed: 1), looking, seed: 1)
                .Reachable(depth: 1);

            output.WriteLine(
                $"{looking}: ceiling {could.CoversUnseen:F3} on the unseen, from "
                + $"{could.Alone.Length} codes sound alone, {could.Least} of them enough");

            foreach (var gate in new[] { Surprising.Unaccounted, Surprising.AnyFailure })
            {
                var unseen = new List<double>();

                foreach (var seed in new[] { 1, 2, 3, 4, 5 })
                {
                    var run = new ArrangedRun(
                        Small,
                        new Brain(new CommittingSettings { Surprising = gate }, seed),
                        looking,
                        seed);

                    var got = run.Run(20_000);

                    var alone = Fixture.Alone(run.Held);

                    unseen.Add(got.Tally.Unseen!.Accuracy);

                    output.WriteLine(
                        $"  {gate,-11} seed {seed} | unseen {got.Tally.Unseen.Accuracy:F3} "
                        + $"drawn {got.Tally.Recent:F3} | "
                        + $"{could.Alone.Count(alone.Contains)}/{could.Alone.Length} sound "
                        + $"singles held, {got.Tally.Resident} resident "
                        + $"({got.Tally.Minted} minted) | "
                        + $"sound {got.Rules.Sound} unsound {got.Rules.Unsound}");
                }

                var mean = unseen.Average();
                var spread = Math.Sqrt(
                    unseen.Sum(one => (one - mean) * (one - mean)) / (unseen.Count - 1));

                output.WriteLine(
                    $"  {gate,-11} MEAN {mean:F3} +/- {spread / Math.Sqrt(unseen.Count):F3}");
            }
        }

        Assert.True(true);
    }
}
