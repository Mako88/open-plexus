using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whole against tiled, on the world where the difference can show.
/// </summary>
/// <remarks>
/// <b>The measurement the plan has been owed since step four.</b> A pooled embedding
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
        // The gap the budget curve left, and it is named in that commit rather than found
        // afterwards. `BudgetCurveTests` says every column that is a fact about what was
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
        // And the reading is the withheld set rather than a trailing window, which is what
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

            // And the arm whose distinguishing property only appears here. `Earned` pays one
            // attempt per `Floor` misses, so it binds where misses are SCARCE -- and on the
            // multiplexer they are not, which is why it came back indistinguishable from free
            // on every cell of two worlds. This world's true rules are one code and its
            // accuracy is high, so its parents are wrong far less often. If the rule binds
            // anywhere on this bench it binds here, and if it does not it is a third off
            // switch and goes the way of `Children`.
            //
            // It does not bind. The shipped total reads 0.725 unseen at 709 sound and 525
            // unsound; free reads 0.663 at 94 and 1195; earned reads 0.651 at 95 and 1281,
            // holding 1868 residents against a capacity of 2000. So it sits with FREE on
            // every column and not with the total -- the sound rules collapse sevenfold, the
            // unsound ones nearly treble, and the withheld score falls seven points.
            //
            // And those three rows were taken at five seeds and recorded as ten, which is
            // corrected here rather than carried. `f26b6ed`'s message and the note that stood
            // in this place both said ten; this grid has called `Sweep` at its default since
            // the row was written. The direction survives the correction and the WIDTH may
            // not: this file's own two-code reading says five seeds on this world carry a
            // spread of 0.053, and the gap being read is 0.074. That is 1.4 spreads, which is
            // the width at which this file already refused to report a direction.
            //
            // So the grid asks for ten and every row is retaken together, which is the only
            // honest way to add a fourth. A number in a commit message is a claim rather than
            // a record, and this one was a claim about how the record was taken.
            //
            // And the reason offered for it not binding is the rule's own arithmetic rather
            // than this world. One attempt per `Floor` misses funds a parent in proportion to
            // how WRONG it is, and on a world whose truths are one code every repair is
            // damage -- so the parents doing the most damage are exactly the ones earning
            // fastest. That is the shape John's curve names, and it is the row below.
            ("earned", new CommittingSettings
            {
                Surprising = Surprising.AnyFailure,
                Budgeting = Budgeting.Earned,
            }),

            // Fork 110, and it is the term the row above is missing rather than a fourth
            // opinion. `Curved` divides the same floors and takes the SCARCER counter, so its
            // allowance is `Earned`'s multiplied by `min(hits/misses, 1)` -- inert on a parent
            // right half the time or better, and a ninth of the grant for one right a round in
            // ten. The 1281 unsound rules the row above holds are exactly the population it
            // bites on, so this is aimed at the damage the reading blamed.
            //
            // What would drop it: landing on `earned`'s numbers rather than the total's. That
            // would say the cap never binds where it matters and the arm is a free budget
            // wearing a limit's name, which is `Children`'s refutation and needs no second
            // one. It already differs from `Earned` on the six-bit multiplexer, so it is
            // wired -- `BudgetingTests` holds that check, and being wired is not being worth
            // anything.
            //
            // And the cell that would say the curve is the wrong SHAPE rather than the wrong
            // rule is this one landing between the two: a cap that recovers some of the total's
            // score and not all of it says the term belongs and `min` is too blunt a form of
            // it. That is a different next step from the arm dying, so the three outcomes are
            // separated before the run rather than after.
            ("curved", new CommittingSettings
            {
                Surprising = Surprising.AnyFailure,
                Budgeting = Budgeting.Curved,
            }),
        })
        {
            var (unseen, last) = Sweep(Small, dials, Looking.Tiled, seeds: 10);

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
    /// <b>One brain per configuration and five seeds of it, because one seed is not a
    /// comparison and this repo has watched an ordering invert.</b> Written out twice
    /// before `DuplicationTests` refused the second, which is that budget doing its job
    /// on a measurement file rather than on the library.
    /// <para>
    /// <b>And the count is a parameter because five was not enough once.</b> A two-code
    /// repair step read 0.702 here with a standard error of 0.053, which is a grid unable to
    /// say anything about a difference of that size — so a comparison that expects a small
    /// effect asks for more seeds rather than reporting a spread it cannot use. Five stays the
    /// default so every number taken before this is still the number it was.
    /// </para>
    /// </remarks>
    /// <param name="seeds">How many seeds to run, defaulting to what every earlier grid used.</param>
    private static (List<double> Unseen, Grounded Last) Sweep(
        ArrangedSettings world, CommittingSettings dials, Looking looking, int seeds = 5)
    {
        var unseen = new List<double>();
        var last = default(Grounded);

        foreach (var seed in Enumerable.Range(1, seeds))
        {
            last = new ArrangedRun(world, new Brain(dials, seed), looking, seed).Run(20_000);
            unseen.Add(last.Tally.Unseen!.Accuracy);
        }

        return (unseen, last!);
    }

    /// <summary>
    /// <b>What distinct children cost where the world's rules are one code — fork 76's
    /// falsifier.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The case for refusing a parent its spent codes is entirely about quantity.</b>
    /// Covering what a parent is right about takes many children, and a budget spent
    /// re-deriving one child buys none of them. This world's truths are ONE CODE, so there is
    /// nothing for a child to cover and this doc already carries the row that here any repair
    /// is damage — eight times the children should be eight times that damage.
    /// </para>
    /// <para>
    /// <b>Which makes it the falsifier rather than a second opinion.</b> If distinct children
    /// are level or better HERE, then whatever they do on the multiplexer is not about
    /// covering a parent's territory, and a grid winning on both worlds is evidence against
    /// the account rather than for the mechanism.
    /// </para>
    /// <para>
    /// <b>And it asks for ten seeds because the last falsifier run here could not speak.</b>
    /// A two-code step read 0.755 against 0.702 with a spread of 0.053, which carries nothing
    /// either way — and reporting a direction off a grid that wide is how a noisy reading
    /// becomes a finding.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_distinct_children_are_damage_where_one_code_is_already_the_truth()
    {
        output.WriteLine("arm        | unseen accuracy | spread | sound | unsound | residents");

        // And the budget comes with it, because the two cannot be chosen apart. Distinct
        // forking at the shipped budget floods this world to its capacity and is five
        // standard errors down; on the multiplexer the same pair has an interior optimum
        // around four to eight, where it beats the shipped rule on coverage AND accuracy. So
        // the arm being refuted here is the FLOODED one, and asking whether a capped version
        // is also damage is a different question that this grid can answer in one more row.
        foreach (var (arm, forking, budget) in new (string Arm, Forking Forking, int? Budget)[]
        {
            // Pinned at what these two were taken under, because the comparison below moved
            // the default and an unpinned fixture would re-take them at the new one under the
            // old rows' names -- this repo's own trap, made live by this very grid.
            ("repeated", Forking.Repeated, 256),
            ("distinct", Forking.Distinct, 256),
            ("distinct 8", Forking.Distinct, 8),
            ("distinct 4", Forking.Distinct, 4),
        })
        {
            // Shipped dials and one thing moved, which is this repo's rule about measuring a
            // mechanism ON from a known baseline rather than OFF from all-on.
            var dials = new CommittingSettings { Forking = forking };

            var (unseen, last) = Sweep(
                Small,
                budget is null ? dials : dials with { Budget = budget.Value },
                Looking.Tiled,
                seeds: 10);

            output.WriteLine(
                $"{arm,-10} | {unseen.Average(),15:F3} | {Spread(unseen),6:F3} "
                + $"| {last.Rules.Sound,5} | {last.Rules.Unsound,7} "
                + $"| {last.Tally.Resident,9} | repairs {last.Tally.Repaired}");
        }

        // NO BAR. The prediction is on the method, where it is read against the grid it was
        // written for rather than enforced by an assertion somebody would have to edit.
    }

    /// <summary>
    /// <b>Whether shortening a scope is damage where the truths are already one code — the
    /// ship gate, and it is the mirror of the grid above.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This world is where an operator that undoes repair should do best, and also where
    /// it could do the most harm.</b> Its truths are one code, this doc already carries the
    /// row that any repair here is damage, and a handful of one-code rules hold the whole
    /// withheld set. So generalisation is either the correction that world has been asking
    /// for, or it deletes the only rules that were working — and both are large effects
    /// rather than a wash.
    /// </para>
    /// <para>
    /// <b>And it is the unseen set that decides, which is why the gate is here and not on
    /// the multiplexer.</b> A generated world has no withheld half, so nothing there can
    /// distinguish a rule that reaches further from a rule that has memorised more. This is
    /// the only bench with the instrument, and shortening a scope is exactly the change
    /// where those two come apart.
    /// </para>
    /// <para>
    /// <b>Ten seeds, for the same reason the grid above asks for them.</b> The last
    /// falsifier run on this world read 0.755 against 0.702 with a spread of 0.053 and
    /// carried nothing either way; a ship gate that cannot speak is not a gate.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_shortening_a_scope_is_damage_where_one_code_is_already_the_truth()
    {
        output.WriteLine("arm        | unseen accuracy | spread | sound | unsound | residents");

        // Ten seeds, and the gate fires: Never 0.805 at 863 sound and 296 unsound over 1164
        // residents; Unmissed 0.721 at 448 and 309; Shared 0.762 at 1277 and 52. So the arm
        // costs four points of the withheld score on the world it was predicted to be
        // dangerous on, which is what this grid was written to ask.
        //
        // And the two halves disagree, which is the reading rather than the verdict. `Shared`
        // holds the MOST sound rules of the three by a half and the FEWEST unsound by five --
        // its population is far cleaner than the arm that beats it. A cleaner population that
        // answers worse is not a mechanism failing; it is a mechanism whose output is not
        // reaching the vote.
        //
        // And the comparison moves two things, which is this repo's oldest trap. Both widening
        // arms sit at 2001 residents against a capacity of 2000 and `Never` sits at 1164, so
        // this is not the operator measured against no operator -- it is a capacity-bound
        // population against one with headroom, and culling decides the withheld set in one
        // cell and not in the other. The raised-capacity row below is what separates them, and
        // until it is read the flip is not licensed either way.
        foreach (var (widening, capacity) in new[]
        {
            (Widening.Never, 2_000),
            (Widening.Unmissed, 2_000),
            (Widening.Shared, 2_000),

            // The same arm with room, so the pin is not doing the work. If `Shared` still
            // trails here then the operator costs on this world and the gate stands; if it
            // catches up, what was measured above was the cap.
            (Widening.Never, 8_000),
            (Widening.Shared, 8_000),
        })
        {
            // The search pair pinned rather than inherited, and the grid above is why. Both
            // of these moved while fixtures that named neither were re-taken silently under
            // their own rows' names, so a grid about a THIRD operator states them.
            var dials = new CommittingSettings
            {
                Widening = widening,
                Forking = Forking.Distinct,
                Budget = 8,
                Capacity = capacity,
            };

            var (unseen, last) = Sweep(Small, dials, Looking.Tiled, seeds: 10);

            output.WriteLine(
                $"{widening,-10} {capacity,5} | {unseen.Average(),15:F3} | {Spread(unseen),6:F3} "
                + $"| {last.Rules.Sound,5} | {last.Rules.Unsound,7} "
                + $"| {last.Tally.Resident,9} | widened {last.Tally.Widened}");
        }

        // NO BAR, for the same reason as the grid above: the prediction is on the method and
        // read against the rows it was written for.
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
                + $"named {got.Tally.Named} of {got.Tally.Eligible} eligible, "
                + $"spoke {got.Tally.Speaking:F2} | "
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
        // The one arm with a clean target, swept on the one dial the plan named. Tiled
        // under `AnyFailure` holds every sound single-code rule on every seed, the
        // front end loses nothing a linear probe can find, and the language covers the
        // whole world at depth one. Everything is in place and it scores 0.800.
        //
        // And the population is not confused about which rules are true -- it believes
        // the sound ones 1.000 and the unsound ones 0.522. So a crowd of rules that
        // LOOK worse is outvoting a handful that look perfect, which is what raising
        // accuracy to a power exists to stop, and five is evidently not enough of it:
        // 0.522^5 is 0.039, and a few dozen of those agreeing beat one weight of 1.
        //
        // So this is a prediction with a number on it. If the vote is the gap, the
        // score climbs toward 1.000 as the power rises. If it does not, the gap is
        // somewhere nobody has looked yet and this rules out the obvious place.
        var could = new ArrangedRun(
            Small, new Brain(new CommittingSettings(), seed: 1), Looking.Tiled, seed: 1)
            .Reachable(depth: 1);

        output.WriteLine(
            $"target {could.CoversUnseen:F3} on the unseen, from {could.Alone.Length} "
            + $"codes sound alone, {could.Least} of them enough");

        // And the question is settled rather than swept now. This crossed a summed vote at
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
        // The side of the prediction that must not move. Repair on this world is not
        // the constraint -- the rules that solve it are one code each and genesis mints
        // them directly -- so letting repair run on more rounds should change nothing
        // and the score should stay at the target. If it FALLS, the extra gate was
        // holding back damage rather than search, and the whole argument inverts.
        // And the occasion count is reported beside the score because of what it
        // settled here. A third subsumption rule weighing a child's advantage against
        // the DISTINCT moments it stands on -- built to delete children that had
        // memorised a corner of the drawn bag -- deleted an ordinary share of them and
        // reached the identical withheld score on all five seeds. What sinks this cell
        // is not children standing on one repeated scene.
        // The three cells this grid was taken over, as the pairs they turned out to be.
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
        // Three instruments have now said the same thing and none of them said whose
        // fault it is. The children that cost this world a quarter of its score are not
        // memorised, the population is not what is read, and handing every seat back to
        // a general rule changes not one answer -- because those deciders ARE
        // significantly better on what they were shown. A rule true of the drawn
        // arrangements and false of the withheld ones has a perfect observed record, and
        // no statistic over drawn data can see it.
        //
        // Which makes the next question about the exam rather than the machine. `Hold`
        // withholds every nth arrangement, so a LARGER value draws more of the world. If
        // the gap is what the drawn set cannot distinguish, it closes as this rises. If
        // it does not, the deciders are wrong for a reason coverage does not touch, and
        // that rules out the last explanation this session has left.
        //
        // Not comparable cell to cell, and saying so is the point. Each value withholds a
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
        // One seed is not a comparison and will happily invert, which this repo has
        // already paid for once -- winnowing beat bands on seed one and lost over five.
        // The single-seed reading says `Unaccounted` starves genesis on this world,
        // which is the OPPOSITE of what five seeds said on CIFAR, so it gets error bars
        // before it gets written down as anything.
        foreach (var looking in new[] { Looking.Whole, Looking.Tiled })
        {
            // Hoisted, because the ceiling is a fact about the world and the front end
            // and neither moves with the seed. Recomputing it per run would spend most
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
