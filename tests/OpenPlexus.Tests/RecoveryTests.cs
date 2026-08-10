using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What it costs to recover when the world moves under the learner — <b>fork 27's direct
/// test, and the one step-one requirement whose world was built and never measured.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE PLAN NAMES THIS INSTRUMENT AND THE REPO HAS ONLY THE WORLD.</b>
/// <c>MultiplexerSettings.Switch</c> moves the target mid-run and <c>MultiplexerTests</c>
/// asserts that it moves it correctly — the key travels with it, the first mapping is the
/// identity so a switching run and a standard one are one world until the first flip. What
/// nobody built is the reading: <i>flip the target mid-run, report steps to recover</i>.
/// </para>
/// <para>
/// <b>AND IT IS THE ONLY THING THAT CAN SETTLE FORK 27.</b> Hits, misses and abstains are
/// G-Counters and give a LIFETIME average for free; beside them each node keeps a
/// recency-weighted estimate of what IT saw, which never merges and is what decides. The
/// second estimate is justified by C4 — no episode boundary, so a lifetime average cannot
/// track — and on a stationary world it is predicted to buy nothing. A world that moves is
/// where that prediction is falsifiable, and the dial that turns the local estimate back
/// into the lifetime one is <see cref="CommittingSettings.Recency"/> at near zero.
/// </para>
/// <para>
/// <b>SO THE GRID IS TWO WORLDS BY TWO ARMS, AND THE STATIONARY HALF IS NOT DECORATION.</b>
/// A difference on the switching world alone is the finding; the same difference on both is
/// the dial doing something else entirely, and this repo has paid for reading one cell of a
/// two-by-two as though it were the whole of it. Measure one mechanism ON from a known
/// baseline, never one OFF from all-on.
/// </para>
/// </remarks>
public sealed class RecoveryTests(ITestOutputHelper output)
{
    /// <summary>How long the world holds still before it moves.</summary>
    /// <remarks>
    /// <b>LONG ENOUGH THAT THE TARGET IS HELD BEFORE THE FLIP, or this measures learning
    /// rather than recovery.</b> Six bits reaches the target well inside this on every seed
    /// the scaling grid reads, so what happens after the flip is a fall from a height the
    /// run had actually reached.
    /// </remarks>
    private const int Settled = 20_000;

    /// <summary>Matched to the other multiplexer grids, so the rows are comparable.</summary>
    private const int Seeds = 6;

    /// <summary>How often the target moves where it keeps moving.</summary>
    /// <remarks>
    /// <b>LONGER THAN THE TRAILING WINDOW AND SHORTER THAN A RECOVERY.</b> Two thousand
    /// rounds is what accuracy is read over, so a shorter interval would report a window
    /// straddling two targets and nothing else; five thousand is where the one-flip curves
    /// are furthest apart — a free budget has turned the corner and started climbing while a
    /// capped one is still falling — so each interval is long enough for the arms to have
    /// separated inside it.
    /// </remarks>
    private const int Moving = 5_000;

    /// <summary>The local estimate turned off, as near as a rate can be turned off.</summary>
    /// <remarks>
    /// <b>NOT ZERO, BECAUSE ZERO IS A DIFFERENT MECHANISM AND NOT A SLOWER ONE.</b> At
    /// exactly nought the estimate never moves off whatever it was initialised to, which is
    /// an arm about initialisation. Near zero it is the lifetime average the G-Counters
    /// already carry, which is the arm fork 27 actually names.
    /// </remarks>
    private const double Lifetime = 0.001;

    /// <summary>
    /// Trailing accuracy at three distances past the flip, as three formatted cells.
    /// </summary>
    /// <param name="flip">How often the target moves, or nought to leave it still.</param>
    /// <param name="dials">The brain, per seed.</param>
    /// <param name="address">Address bits, so a finding can be asked at a second width.</param>
    /// <remarks>
    /// <b>ONE COPY BECAUSE `DuplicationTests` REFUSED THE THIRD.</b> Every grid in this file
    /// is the same curve read under a different setting, and three copies of the loop is
    /// three chances for one grid's distances or seed count to drift from another's — which
    /// would make rows that look comparable and are not. The reading is the row; the walk is
    /// not.
    /// </remarks>
    private static string Curve(int flip, Func<int, CommittingSettings> dials, int address = 2)
    {
        var read = new List<string>();

        foreach (var past in new[] { 250, 1_000, 5_000 })
        {
            var recent = new List<double>();

            for (var seed = 1; seed <= Seeds; seed++)
                recent.Add(new MultiplexerRun(
                    new MultiplexerSettings { Address = address, Switch = flip },
                    new Brain(dials(seed), seed),
                    seed).Run(Settled + past).Recent);

            read.Add($"{Sweep.Spread(recent),18}");
        }

        return string.Join(" | ", read);
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_moving_target_costs_and_whether_the_local_estimate_pays_for_it()
    {
        // READ AS A CURVE PAST THE FLIP RATHER THAN AS A CROSSING, because there is no
        // machinery for a second crossing and inventing one would be a mechanism built to
        // serve a measurement. `Tally.Reached` reports the FIRST time the trailing window
        // held the target and nothing re-arms it, so a run that flips at twenty thousand
        // reports the same crossing it would have reported without flipping at all.
        //
        // SO THE READING IS THE TRAILING ACCURACY AT A KNOWN DISTANCE PAST THE FLIP, taken
        // over separate runs of the same seed. Same world, same brain, same draw order up to
        // the flip -- only how long the run continues afterwards differs, which is what makes
        // the row a recovery curve rather than four unrelated numbers.
        // AND WITH A SPREAD ON EVERY CELL, WHICH THE FIRST TAKE OF THIS GRID DID NOT HAVE.
        // It printed four rows of bare means and they ordered cleanly, which is exactly the
        // shape this repo's traps list warns about -- one seed is not a comparison and six
        // means are not one either unless something says how far apart they are. The
        // difference this grid exists to find is a few points, and a few points is inside a
        // seed spread on plenty of worlds.
        output.WriteLine($"{Seeds} seeds, target moves once at {Settled} rounds");
        output.WriteLine("world       | recency | rounds past the flip: 250 | 1000 | 5000");

        foreach (var (world, flip) in new (string World, int Flip)[]
        {
            // THE CONTROL FIRST. A stationary world's row is the same three runs at three
            // lengths, so anything that moves along it is the run getting longer and not the
            // world moving -- and if the two arms differ HERE, they differ for a reason this
            // grid is not about.
            ("stationary", 0),
            ("switching", Settled),
        })
        {
            foreach (var (arm, recency) in new (string Arm, double Recency)[]
            {
                ("0.1", new CommittingSettings().Recency),
                ("~0", Lifetime),
            })
            {
                output.WriteLine(
                    $"{world,-11} | {arm,7} | "
                    + Curve(flip, _ => new CommittingSettings { Recency = recency }));
            }
        }

        // NO BAR. Whether the local estimate earns its keep is what this reports, and a
        // threshold written before the first reading would be the answer rather than the
        // finding. What a bar would also do is hide the case the plan predicts: no difference
        // on either world, which would mean the second estimate is unearned everywhere and
        // the G-Counters are enough.
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_what_recovery_waits_for_is_genesis()
    {
        // THE CANDIDATE LEFT STANDING AFTER THE WRECKAGE STORY DIED, and it follows from what
        // repair CANNOT do. A flip changes which answer a scope entails, and repair only ever
        // adds a condition -- it cannot change what a commitment EXPECTS. So every rule the
        // new target needs has to be minted, and minting is the one operator with a gate in
        // front of it.
        //
        // AND THE GATE IS EXACTLY WRONG-SHAPED FOR THIS. `Surprising.Unaccounted` mints only
        // where nothing that fired proposed what arrived -- and after a flip the population is
        // dense, so with two outcomes something proposes the right answer about half the time
        // by chance alone. Genesis goes quiet precisely when the population most needs new
        // claims, and that is a self-limiting rule doing what fork 40 already caught it doing
        // on `Arranged`.
        //
        // `AnyFailure` IS A CONTROL AND NOT A PROPOSAL. It walks the whole `code -> outcome`
        // space and this repo's revival table says so; what it is for here is isolating
        // whether the gate is what recovery waits on. If the two arms recover alike, genesis
        // is not the bottleneck and the question is open again.
        output.WriteLine($"{Seeds} seeds, target moves once at {Settled} rounds");
        output.WriteLine("genesis      | rounds past the flip: 250 | 1000 | 5000 | minted");

        foreach (var gate in new[] { Surprising.Unaccounted, Surprising.AnyFailure })
        {
            var curve = Curve(Settled, _ => new CommittingSettings { Surprising = gate });

            var minted = Enumerable.Range(1, Seeds)
                .Select(seed => (double)new MultiplexerRun(
                    new MultiplexerSettings { Address = 2, Switch = Settled },
                    new Brain(new CommittingSettings { Surprising = gate }, seed),
                    seed).Run(Settled + 5_000).Tally.Minted)
                .ToList();

            output.WriteLine(
                $"{gate,-12} | " + curve + $" | {Sweep.Spread(minted, "F0")}");
        }

        // NO BAR. Whether the gate is what recovery waits on has never been measured, and a
        // threshold written before the first reading would be the answer rather than the
        // finding.
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_world_that_keeps_moving_costs_a_budget_that_never_returns()
    {
        // ONE FLIP IS THE EASY CASE AND C4 DESCRIBES THE HARD ONE. *No episode boundary* does
        // not mean the target moves once and settles; it means there is no round at which the
        // world can be assumed to have stopped. Every recovery reading in this file so far
        // moves the target a single time and then lets the run finish in peace, which is a
        // world with an episode boundary in all but name.
        //
        // AND A LIFETIME BUDGET SHOULD DEGRADE WITH THE NUMBER OF MOVES rather than with any
        // one of them. Each flip asks the parents that now expect the right answer to spend
        // from a total that was never topped up, so the second move has less to spend than
        // the first and the fourth may have nothing. That is a prediction about the SHAPE of
        // the curve and not about a gap: `Attempts` should fall away move by move while a
        // bound on what a parent HOLDS should not.
        //
        // READ AT THE END RATHER THAN PAST A FLIP, because with the target moving every five
        // thousand rounds there is no *past the flip* -- the run is always between two of
        // them. The trailing window is two thousand rounds, so a reading taken at the end is
        // taken well inside the last interval and is comparable across arms.
        output.WriteLine($"{Seeds} seeds, six bits, the target moves every {Moving} rounds");
        output.WriteLine("budget         | moves: 0 | 1 | 2 | 4");

        foreach (var (arm, dials) in new (string Arm, CommittingSettings Dials)[]
        {
            ("attempts 64", new CommittingSettings { Budget = 64 }),
            ("attempts 256", new CommittingSettings()),
            ("attempts free", new CommittingSettings { Budget = int.MaxValue }),
            ("children 64", new CommittingSettings
            {
                Budget = 64,
                Budgeting = Budgeting.Children,
            }),
        })
        {
            var read = new List<string>();

            // NOUGHT MOVES IS THE CONTROL AND IT IS THE SAME LENGTH OF RUN, so the row reads
            // across rather than down: what changes between the cells is how many times the
            // target moved and nothing else whatever.
            foreach (var moves in new[] { 0, 1, 2, 4 })
            {
                var recent = new List<double>();

                for (var seed = 1; seed <= Seeds; seed++)
                    recent.Add(new MultiplexerRun(
                        new MultiplexerSettings
                        {
                            Address = 2,
                            Switch = moves == 0 ? 0 : Moving,
                        },
                        new Brain(dials, seed),
                        seed).Run(Settled + (moves * Moving)).Recent);

                read.Add($"{Sweep.Spread(recent),18}");
            }

            output.WriteLine($"{arm,-14} | " + string.Join(" | ", read));
        }

        // NO BAR. What a world that keeps moving costs has never been measured here, and a
        // threshold written before the first reading would be the answer rather than the
        // finding.
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_a_budget_on_children_rather_than_attempts_is_what_C4_wanted()
    {
        // THE ANSWER TO FORK 72 NAMES A MECHANISM THAT IS ALREADY BUILT, and this is the
        // reading that says whether it is the one. A per-parent budget in ATTEMPTS is a
        // lifetime cap on EFFORT: spend it being wrong for twenty thousand rounds and there is
        // none left when the target moves. `Budgeting.Children` charges only for a child the
        // parent has not reached before, so it caps what a parent may HOLD rather than what it
        // may try -- a memory bound instead of a time bound, and a memory bound assumes
        // nothing about how long a parent lives.
        //
        // WHICH IS EXACTLY WHAT C4 ASKS FOR. *No episode boundary, so nothing may depend on
        // train-then-test* -- and a total spent over a life is a bet that the life is one
        // episode. This is the one arm in the census that is not such a bet.
        //
        // AND IT HAS READ AS INERT FOR THE LIFE OF THE BRANCH, WHICH IS THE POINT. Distinct
        // children are capped by the vocabulary far below the budget -- twelve at six bits,
        // twenty-two at eleven, against sixty-four -- so `Children` has never bound on any
        // world here and is indistinguishable from a free budget. On a world that holds still
        // that made it look like an arm with nothing to say. Free is what recovers, so a
        // moving world is the cell that separates them, and `DialTests` has been carrying
        // this row waiting for a world whose vocabulary reaches the budget when the separator
        // was non-stationarity all along.
        //
        // THE PREDICTION: `Children` recovers like free on the switching world and sits with
        // `Attempts` on the stationary one. If it recovers no better than `Attempts`, the
        // budget's shape is not what matters and only its SIZE is -- which would make the
        // finding about a number rather than about C4.
        output.WriteLine($"{Seeds} seeds, target moves once at {Settled} rounds, budget 64");
        output.WriteLine("bits | world       | budgeting | rounds past the flip: 250 | 1000 | 5000");

        foreach (var address in new[] { 2, 3 })
        {
            foreach (var (world, flip) in new (string World, int Flip)[]
            {
                ("stationary", 0),
                ("switching", Settled),
            })
            {
                foreach (var counting in new[] { Budgeting.Attempts, Budgeting.Children })
                {
                    output.WriteLine(
                        $"{address + (1 << address),4} | {world,-11} | {counting,-9} | "
                        + Curve(
                            flip,
                            // SIXTY-FOUR RATHER THAN THE SHIPPED LEVEL, because the question is
                            // what the budget COUNTS and not how big it is. At the shipped 256
                            // the attempts arm is already close to free at six bits, which
                            // would leave the two arms with nothing to differ about.
                            _ => new CommittingSettings { Budget = 64, Budgeting = counting },
                            address));
                }
            }
        }

        // NO BAR. Whether the budget's shape or its size is what recovery is short of has
        // never been measured, and a threshold written before the first reading would be the
        // answer rather than the finding.
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_the_parents_that_must_relearn_have_spent_their_budget()
    {
        // THE FOURTH ACCOUNT, AND IT IS A STRUCTURAL ASYMMETRY RATHER THAN AN OPERATOR MISBE-
        // HAVING. Repair adds a CONDITION and never changes what a commitment expects, so
        // every rule a moved target needs has to descend from a parent that ALREADY expects
        // the new answer -- and those are precisely the parents that spent twenty thousand
        // rounds being wrong. `Budget` is per parent and counts attempts, and this doc's own
        // row says most of it goes on re-derivation, so the lineages that must now do the
        // work arrive at the flip with theirs largely gone.
        //
        // WHICH IS THE ONE THING THAT DIFFERS BETWEEN RELEARNING AND LEARNING FROM NOTHING.
        // From scratch every parent has a full budget; after a flip the ones that matter do
        // not. If that is the bottleneck, a free budget recovers faster and a smaller one is
        // worse; if the three arms come back level, the budget is not what the window is
        // short of and the asymmetry is somewhere else.
        //
        // AND THE STATIONARY CONTROL IS NOT OPTIONAL HERE, because `BudgetCurveTests` already
        // says the budget has an interior optimum on a world that holds still. A grid that
        // moved on both worlds would be re-reading that curve rather than measuring recovery.
        output.WriteLine($"{Seeds} seeds, target moves once at {Settled} rounds");
        output.WriteLine("bits | world       | budget | rounds past the flip: 250 | 1000 | 5000");

        // BOTH WIDTHS, BECAUSE A FINDING ON ONE IS A FINDING ABOUT ONE. Six bits was where
        // this was first read; eleven is where the repair budget is known to have an interior
        // optimum on a world that holds still, so it is also where a budget effect could most
        // easily be that curve arriving in disguise. The stationary rows are what separate
        // them.
        foreach (var address in new[] { 2, 3 })
        {
            foreach (var (world, flip) in new (string World, int Flip)[]
            {
                ("stationary", 0),
                ("switching", Settled),
            })
            {
                foreach (var budget in new[] { 64, 256, int.MaxValue })
                {
                    output.WriteLine(
                        $"{address + (1 << address),4} | {world,-11} | "
                        + $"{(budget == int.MaxValue ? "free" : budget.ToString()),6} | "
                        + Curve(flip, _ => new CommittingSettings { Budget = budget }, address));
                }
            }
        }

        // NO BAR. Which of the two the window is short of has never been measured, and a
        // threshold written before the first reading would be the answer rather than the
        // finding.
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_a_subsumption_that_demands_significance_recovers_faster()
    {
        // THE THIRD ACCOUNT, AND ITS PREDICTION IS IN THE COMMIT THAT SHIPPED THE READING
        // BELOW RATHER THAN IN THIS COMMENT. Repair makes about nineteen children in the five
        // thousand rounds after a flip and subsumption removes about twenty-five, so the
        // population shrinks across the window it is meant to be rebuilding in.
        //
        // AND THE MECHANISM IS ALREADY NAMED. A fresh child inherits no table, so it must
        // re-earn its statistics -- and under `Subsuming.Weaker` a rule that is merely NOT
        // BETTER than its parent loses, which a child with almost no firings usually is.
        // `Insignificant` demands the narrower rule be significantly better before the
        // general one may take its place, by the two-proportion test repair already owns, and
        // holds roughly twice the residents where it has been measured.
        //
        // WHAT WOULD KILL IT: no separation on the switching world, or the same movement on
        // the stationary one. The second would mean the arm is doing something this question
        // is not about, which is why the control is here rather than assumed away.
        output.WriteLine($"{Seeds} seeds, target moves once at {Settled} rounds");
        output.WriteLine("world       | subsuming      | 250 past the flip | 1000 | 5000");

        foreach (var (world, flip) in new (string World, int Flip)[]
        {
            ("stationary", 0),
            ("switching", Settled),
        })
        {
            foreach (var rule in new[] { Subsuming.Weaker, Subsuming.Insignificant })
            {
                output.WriteLine(
                    $"{world,-11} | {rule,-14} | "
                    + Curve(flip, _ => new CommittingSettings { Subsuming = rule }));
            }
        }

        // NO BAR, AND THE PREDICTION IS ELSEWHERE ON PURPOSE. A threshold here would be the
        // answer rather than the reading; a prediction in a commit can be wrong in public.
    }

    [Fact]
    public void What_the_operators_do_in_the_five_thousand_rounds_after_a_flip()
    {
        // TWO ACCOUNTS OF SLOW RECOVERY ARE DEAD AND THIS PARTITIONS WHAT IS LEFT. The old
        // rules are not squatting and genesis is not gated shut; what remains is that the
        // rules the new target needs are CONJUNCTIONS, and the only thing that builds a
        // conjunction is repair -- which wants a floor of misses on a parent, a condition
        // past a separation bar, and a budget.
        //
        // SO THE READING IS WHAT EACH OPERATOR DID IN THE WINDOW, and it is a subtraction
        // rather than a new counter. The same seed run to twenty thousand and to twenty-five
        // thousand differ only in the five thousand rounds after the flip, so the difference
        // between their tallies is what those rounds cost -- and the same subtraction on a
        // world that never flips is the control that says which of it is the flip and which
        // is just five thousand more rounds.
        output.WriteLine("world       | minted | repaired | subsumed | residents | recent");

        foreach (var (world, flip) in new (string World, int Flip)[]
        {
            ("stationary", 0),
            ("switching", Settled),
        })
        {
            var minted = new List<double>();
            var repaired = new List<double>();
            var subsumed = new List<double>();
            var residents = new List<double>();
            var recent = new List<double>();

            for (var seed = 1; seed <= Seeds; seed++)
            {
                var before = Run(flip, seed, Settled);
                var after = Run(flip, seed, Settled + 5_000);

                minted.Add(after.Tally.Minted - before.Tally.Minted);
                repaired.Add(after.Tally.Repaired - before.Tally.Repaired);
                subsumed.Add(after.Tally.Subsumed - before.Tally.Subsumed);
                residents.Add(after.Resident - before.Resident);
                recent.Add(after.Recent);
            }

            output.WriteLine(
                $"{world,-11} | {Sweep.Spread(minted, "F1")} | {Sweep.Spread(repaired, "F1")} "
                + $"| {Sweep.Spread(subsumed, "F1")} | {Sweep.Spread(residents, "F1")} "
                + $"| {Sweep.Spread(recent)}");
        }

        // NO BAR. Which operator the recovery is waiting on has never been measured, and a
        // threshold written before the first reading would be the answer rather than the
        // finding.
        return;

        static Learned Run(int flip, int seed, long rounds) => new MultiplexerRun(
            new MultiplexerSettings { Address = 2, Switch = flip },
            new Brain(new CommittingSettings(), seed),
            seed).Run(rounds);
    }

    [Fact]
    public void The_wreckage_of_a_flip_is_not_what_is_resident_afterwards()
    {
        // THE OBVIOUS EXPLANATION FOR SLOW RECOVERY, AND THIS IS THE CONTROL THAT REFUSES
        // IT. Relearning after the target moves is slower than learning the same world from
        // nothing, and the reading that suggests itself is that the population is full of
        // confidently wrong rules holding seats -- `Cull` returns early below `Capacity` and
        // this world holds a few dozen commitments against two thousand, so nothing is ever
        // culled at all.
        //
        // AND THE MACHINE ALREADY KEEPS BOTH STATISTICS NEEDED TO LOOK. `Reliability` is the
        // G-Counter ratio over a commitment's whole life; `Accuracy` is the local decaying
        // estimate. A rule that was right and is now wrong is one where the first is high and
        // the second is not, and the gap between two numbers the design already carries needs
        // no threshold anyone had to choose.
        //
        // WHAT IT SAYS IS THAT THE WRECKAGE IS NOT THERE. A handful of residents past a flip,
        // almost none of them believed-over-a-life and refuted-lately, and subsumption
        // removing a hundred and more over the run -- so the population turns over even where
        // culling never runs, and slow recovery is NOT the old rules squatting. Whatever
        // makes it slow is open, and it is not this.
        var settings = new MultiplexerSettings { Address = 2 };

        var brain = new Brain(new CommittingSettings(), 1);

        new MultiplexerRun(settings with { Switch = Settled }, brain, seed: 1)
            .Run(Settled + 5_000);

        var experienced = brain.Held.All
            .Where(one => one.Seen >= brain.Dials.Floor)
            .ToList();

        // BELIEVED OVER A LIFETIME AND REFUTED LATELY. Both bars are the same number and it
        // is one the design already uses for a coin toss, so neither is tuned.
        var stale = experienced.Count(one => one.Reliability > 0.5 && one.Accuracy < 0.5);

        var culled = brain.Held.Lineages.Values.Sum(one => one.Culled);
        var subsumed = brain.Held.Lineages.Values.Sum(one => one.Subsumed);

        output.WriteLine(
            $"{experienced.Count} experienced residents 5000 rounds past one flip, "
            + $"{stale} believed over a lifetime and refuted lately");

        output.WriteLine(
            $"culled {culled}, subsumed {subsumed}, residents {brain.Held.Count} "
            + $"against a capacity of {brain.Dials.Capacity}");

        output.WriteLine(
            $"mean reliability {experienced.Average(one => one.Reliability):F3}, "
            + $"mean accuracy {experienced.Average(one => one.Accuracy):F3}");

        // THE POPULATION IS NOWHERE NEAR THE CAPACITY, SO CULLING NEVER RUNS. Asserted rather
        // than remarked, because it is the half of the story that IS true: nothing here
        // deletes on a rule going bad, and the population turns over anyway.
        Assert.True(
            brain.Held.Count < brain.Dials.Capacity,
            "this world now overshoots the capacity, so culling runs and the reading below "
            + "is about a different machine");

        Assert.Equal(0, culled);

        // AND SUBSUMPTION IS WHAT MOVES INSTEAD, which is what makes the stale count small
        // rather than an accident of one seed. If this ever reads nought the population is
        // static and the paragraph above stops being the explanation.
        Assert.True(subsumed > 0,
            "nothing was subsumed either, so nothing whatever removes a commitment on this "
            + "world and the small stale count needs another account");

        // NO BAR ON `stale`. It is the reading, and a threshold on it would be a prediction
        // dressed as a requirement -- in either direction.
    }

    [Fact]
    public void The_flip_is_reached_by_the_learner_and_not_only_by_the_world()
    {
        // A WORLD CAN MOVE AND THE LEARNER NEVER NOTICE, WHICH READS EXACTLY LIKE A LEARNER
        // THAT RECOVERED INSTANTLY. `MultiplexerTests` asserts the target moves and the key
        // moves with it; that is a fact about the world and says nothing about whether any
        // run was disturbed. So the grid above is unreadable until something shows the flip
        // costs accuracy at all.
        //
        // AND IT IS ASSERTED AS A DIFFERENCE RATHER THAN AS A DEPTH. How far a run falls is
        // the grid's question; that it falls is this one's, and a bar on the size of the fall
        // would be a number chosen to sit just under what was measured.
        var settings = new MultiplexerSettings { Address = 2 };

        var still = new MultiplexerRun(
            settings, new Brain(new CommittingSettings(), 1), seed: 1).Run(Settled + 250);

        var moved = new MultiplexerRun(
            settings with { Switch = Settled },
            new Brain(new CommittingSettings(), 1),
            seed: 1).Run(Settled + 250);

        output.WriteLine(
            $"250 rounds past the flip: still {still.Recent:F3}, moved {moved.Recent:F3}");

        Assert.True(moved.Recent < still.Recent,
            $"the flipped run scored {moved.Recent:F3} against {still.Recent:F3} standing "
            + "still, so moving the target cost it nothing and the recovery grid beside "
            + "this is measuring two identical worlds");

        // AND THE TWO ARE ONE WORLD UNTIL THE FLIP, so the difference above is the flip and
        // not two runs that diverged from the first round. `Switch` leaves the first mapping
        // as the identity for exactly this reason.
        var before = new MultiplexerRun(
            settings with { Switch = Settled },
            new Brain(new CommittingSettings(), 1),
            seed: 1).Run(Settled - 1_000);

        var alone = new MultiplexerRun(
            settings, new Brain(new CommittingSettings(), 1), seed: 1).Run(Settled - 1_000);

        Assert.Equal(alone.Recent, before.Recent, precision: 10);
        Assert.Equal(alone.Sound, before.Sound);
    }
}
