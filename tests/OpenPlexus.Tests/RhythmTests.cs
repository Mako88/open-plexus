using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The world built so that prediction has somewhere to be measured.
/// </summary>
/// <remarks>
/// <b>Snake dies at seventy-seven steps</b>, and it is the only predictive world
/// here — which is why fork 24's budget hunt was measured never moving and nobody
/// could say whether that was the controller or the sample. This stream is
/// stationary and never ends, so a dial can be swept against it at two run
/// lengths, which the traps section asks for and nothing here could offer.
/// </remarks>
public sealed class RhythmTests(ITestOutputHelper output)
{
    private static RhythmSettings World(
        int symbols = 12, int period = 5, double violations = 0.1) => new()
    {
        Symbols = symbols, Period = period, Violations = violations,
    };

    // ---- what the world is, asserted rather than described -----------------

    [Fact]
    public void The_cycle_repeats_and_the_statistics_never_move()
    {
        // Stationary is the whole point. A dial swept over a long run against a
        // world that drifts is measuring the drift.
        var world = new Rhythm(World(violations: 0.0), seed: 1);

        var first = Enumerable.Range(0, 20).Select(_ => world.Strike().Shown).ToList();

        Assert.Equal(first.Take(5), first.Skip(5).Take(5));
        Assert.Equal(first.Take(5), first.Skip(15).Take(5));
    }

    [Fact]
    public void A_violation_is_never_the_symbol_the_cycle_called_for()
    {
        // A "violation" that drew the expected symbol is not one, and counting it
        // as one would put predictable moments into the unpredictable column and
        // quietly raise the ceiling.
        var world = new Rhythm(World(violations: 1.0), seed: 3);

        for (var moment = 0; moment < 200; moment++)
        {
            var wanted = world.Wanted(moment);
            var (shown, violated) = world.Strike();

            Assert.True(violated);
            Assert.NotEqual(Rhythm.Of(wanted), shown);
        }
    }

    [Fact]
    public void The_ceiling_is_what_a_perfect_model_would_score()
    {
        var world = new Rhythm(World(violations: 0.2), seed: 1);

        Assert.Equal(0.8, world.Ceiling, 6);

        // And the marginal baseline is below it, which is what makes it a control:
        // a system that learnt only which symbols are frequent, and nothing about
        // order, lands there.
        Assert.True(world.Marginal < world.Ceiling);
        Assert.True(world.Marginal > world.Chance);
    }

    /// <summary>
    /// A moment is one code on this world's own modality, which is what the rest rests on.
    /// </summary>
    /// <remarks>
    /// <b>Asserted rather than described, because two claims depend on it.</b> Repair being
    /// inert here is a fact about the moment and not about the gate, and a code riding
    /// another world's modality would put this stream's symbols and that one's under one
    /// name — the collision every front end here is separated to avoid.
    /// </remarks>
    [Fact]
    public void A_moment_is_one_symbol_on_this_world_s_own_modality()
    {
        var world = new Rhythm(World(), seed: 1);

        for (var round = 0; round < 200; round++)
        {
            var turn = world.Next();

            Assert.Equal(Rhythm.Beat, Assert.Single(turn.Seen.Codes).Modality);
            Assert.NotNull(turn.Outcome);
        }
    }

    /// <summary>
    /// The bar one symbol of memory allows is checked against the stream that has to obey it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Rhythm.Carried"/> is arithmetic over the settings and the stream is a
    /// draw, so either could be wrong without the other noticing — and every learner
    /// reading here is taken against it. This runs the perfect one-symbol predictor over the
    /// world itself: for the symbol just shown, the successor the cycle would call for.
    /// </para>
    /// <para>
    /// <b>A long run rather than a bar with slack in it.</b> Two hundred thousand moments
    /// put the sampling error under a thousandth, so the comparison is between two numbers
    /// rather than between a number and a tolerance somebody chose.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_bar_one_symbol_allows_is_what_the_stream_allows()
    {
        var world = new Rhythm(World(), seed: 3);

        // The oracle, built from what the cycle calls for rather than from anything the
        // stream happened to show. A symbol the cycle never calls for gets the successor of
        // position nought, which is as good as any other and is what `Carried` assumes.
        var after = new Dictionary<int, int>();

        for (var at = 0; at < world.Period; at++)
            after[world.Wanted(at)] = world.Wanted(at + 1);

        var rounds = 200_000;
        var right = 0;
        var last = (int)world.Strike().Shown.Value;

        for (var round = 0; round < rounds; round++)
        {
            var shown = (int)world.Strike().Shown.Value;

            if (after.GetValueOrDefault(last, world.Wanted(1)) == shown) right++;

            last = shown;
        }

        var reached = right / (double)rounds;

        output.WriteLine($"oracle {reached:F4} against a computed {world.Carried:F4}");

        Assert.Equal(world.Carried, reached, 2);
    }

    // ---- what the graph does with it ---------------------------------------

    /// <summary>One run of the stream through the population, and what it held afterwards.</summary>
    /// <param name="settings">The shape of the stream.</param>
    /// <param name="seed">What draws the cycle, the violations and the brain.</param>
    /// <param name="rounds">How many moments.</param>
    /// <remarks>
    /// <see cref="Passthrough"/>, because a moment here is already a code and rendering it
    /// as a signal first would put a second thing between the mechanism and the reading.
    /// </remarks>
    private static (Rhythm World, Tally Tally, int Held) Learnt(
        RhythmSettings settings, int seed, long rounds = 20_000)
    {
        var world = new Rhythm(settings, seed);
        var brain = new Brain(new CommittingSettings { Capacity = 4000 }, seed);

        var tally = new Bench(new Watching<Coded>(world, new Passthrough()), brain)
            .Run(rounds, sweep: 1000, target: 0.9, window: 2000);

        return (world, tally, brain.Held.Count);
    }

    /// <summary>
    /// The stream reaches the commitment learner, read against the bar one symbol of memory
    /// allows rather than against the one a perfect model would score.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first run of this world against a population of commitments; what it had before
    /// was the walk, and the walk's runner went. It is here because a world nothing runs
    /// asserts what the WORLD is and nothing about what is learnt.
    /// </para>
    /// <para>
    /// <b>Two bars, and neither of them is <see cref="Rhythm.Ceiling"/>.</b> A perfect model
    /// of the stream knows where in the cycle it is and a moment here carries one symbol, so
    /// <see cref="Rhythm.Carried"/> is what perfect looks like through this world's own
    /// moment and <see cref="Rhythm.Marginal"/> is what a model with no sense of when
    /// scores. Scoring against the unreachable one would read a correct population as most
    /// of the way wrong.
    /// </para>
    /// <para>
    /// <b>The midpoint is the bar because the two bars are far apart</b>, and a run landing
    /// between them would be ambiguous under any tighter reading. What is being asserted is
    /// that the moment is doing work: nearer the reachable ceiling than the memoryless
    /// control, rather than an exact distance nobody has a prediction for.
    /// </para>
    /// <para>
    /// <b>And a score ABOVE the reachable ceiling would mean the bar is wrong</b>, which is
    /// the assertion underneath. A bar computed with no learning is only worth having while
    /// something would notice it being exceeded.
    /// </para>
    /// </remarks>
    [Fact]
    public void Rhythm_reaches_the_commitment_learner()
    {
        var (world, tally, held) = Learnt(World(), seed: 1);

        var between = (world.Marginal + world.Carried) / 2.0;

        output.WriteLine($"drawn   : {tally.Recent:F3} over the last tenth");
        output.WriteLine(
            $"bars    : carried {world.Carried:F3}, marginal {world.Marginal:F3}, "
            + $"blind draw {world.Chance:F3}, perfect {world.Ceiling:F3}");
        output.WriteLine(
            $"held    : {held} commitments, {tally.Repaired} repairs, {tally.Named} names "
            + $"over {tally.Eligible} eligible scopes");
        output.WriteLine($"wanting : {tally.Wanting:F3} of blamed rounds nothing separated");

        Assert.True(tally.Recent > between,
            $"{tally.Recent:F3} is under {between:F3}, halfway between a model with no "
            + $"sense of when ({world.Marginal:F3}) and the best one symbol of memory "
            + $"allows ({world.Carried:F3}), so the moment is not doing work");

        // And the slack is three standard errors of a run rather than of a share, because
        // the moment and the outcome are CONSECUTIVE draws from one stream. A quiet window
        // is clean at both ends, so a trailing tenth moves about half as much again as a
        // count of two thousand hits would.
        Assert.True(tally.Recent < world.Carried + 0.05,
            $"{tally.Recent:F3} is over the {world.Carried:F3} one symbol of memory allows, "
            + "so the bar is wrong rather than the learner good");
    }

    /// <summary>
    /// Repair has nothing to add here, and that is what the world is worth to the suite.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A moment of one code cannot be narrowed</b>, so a scope is already everything
    /// there is to say and no rung of the ladder can extend it. Every other world here moves
    /// genesis, repair, the vote and naming at once; this one holds all but the first two
    /// still, which is the control every arm on the vote has been missing.
    /// </para>
    /// <para>
    /// <b>And it is the ladder's admission signal read where the answer is known.</b> Fork 86
    /// asks what separates <i>nothing separates</i> from <i>nothing GENERAL separates</i>;
    /// here nothing separates because there is nothing to separate WITH, so a trigger that
    /// does not saturate is a trigger reading something other than what it claims.
    /// </para>
    /// </remarks>
    [Fact]
    public void Nothing_can_be_narrowed_where_the_moment_holds_one_code()
    {
        var (_, tally, _) = Learnt(World(), seed: 1);

        output.WriteLine(
            $"blamed {tally.Blamed}, unseparated {tally.Unseparated}, "
            + $"wanting {tally.Wanting:F3}, repaired {tally.Repaired}");

        Assert.True(tally.Blamed > 0,
            "no round ever reached repair, so this says nothing about what repair could do");

        Assert.Equal(0, tally.Repaired);

        Assert.True(tally.Wanting > 0.99,
            $"only {tally.Wanting:F3} of blamed rounds went unseparated on a world where "
            + "nothing CAN separate, so the ladder's trigger is reading something else");
    }

    /// <summary>
    /// A world that changes its answer costs the learner, and the learner tracks it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The only non-stationary world here, and the one place <i>malleability is the
    /// record</i> can be right or wrong.</b> Nothing in this design decays, so a count that
    /// stopped rising still stands at whatever it reached — which only costs anything where
    /// the answer MOVES. Every other world keeps its rules for the whole run.
    /// </para>
    /// <para>
    /// <b>The statistics do not move and only the answer does</b>, so the two arms are read
    /// against the same bars. A new cycle is drawn the same way over the same alphabet at
    /// the same period and violation rate.
    /// </para>
    /// <para>
    /// <b>How often it turns is the axis, and one period would have read as noise.</b> At two
    /// thousand rounds a turn the arms are apart by less than a seed spread, which is a
    /// finding — the population is back before the next turn arrives — and it is not a
    /// comparison. Shortening the period until the recovery no longer fits inside it is what
    /// puts a number on how fast it tracks.
    /// </para>
    /// <para>
    /// <b>What would drop the world, written before the run.</b> If no period however short
    /// puts the score below the stationary arm by more than the seed spread, the turn is not
    /// reaching the learner and this is a probe rather than a bench — there would be no cost
    /// for a tracking mechanism to be measured against.
    /// </para>
    /// <para>
    /// The trailing tenth is two thousand rounds, so every arm but the longest period
    /// averages over many turns and reports the recovery rather than the schedule.
    /// </para>
    /// </remarks>
    [Fact]
    public void How_fast_a_turning_world_is_tracked_is_the_period_it_costs_at()
    {
        var scored = new Dictionary<int, List<double>>();
        var stationary = new List<double>();
        var saturated = 0;

        foreach (var seed in new[] { 1, 2, 3 })
        {
            var (still, held, stillHeld) = Learnt(World(), seed);

            stationary.Add(held.Recent);

            Assert.True(stillHeld < World().Symbols * World().Symbols,
                $"the stationary arm already holds all {stillHeld} one-code commitments "
                + "there are, so it has enumerated the space and the arms below say nothing "
                + "about what turning costs");

            output.WriteLine(
                $"seed {seed} | never    | {held.Recent:F3} | held {stillHeld,3} | "
                + $"carried {still.Carried:F3} marginal {still.Marginal:F3}");

            foreach (var every in new[] { 1000, 200, 50 })
            {
                var (turning, turned, turnedHeld) =
                    Learnt(World() with { Turns = every }, seed);

                Assert.True(turning.Turned > 0,
                    $"a period of {every} never turned, so that arm is the stationary world");

                (scored.TryGetValue(every, out var at)
                    ? at
                    : scored[every] = []).Add(turned.Recent);

                if (turnedHeld == World().Symbols * World().Symbols) saturated++;

                output.WriteLine(
                    $"seed {seed} | every {every,4} | {turned.Recent:F3} | "
                    + $"held {turnedHeld,3} | {turning.Turned} turns");
            }
        }

        // What a score cannot say, and it is the reading rather than a caveat. Every turning
        // arm ends holding the WHOLE one-code space -- every symbol against every outcome --
        // while the stationary one stops short of it. So nothing is being minted to track a
        // turn: the rules are all there already and what moves between regimes is which of
        // them the vote reads. That is `malleability is the record` with the record doing
        // all the work and the population doing none, on the only world where the answer
        // moves at all.
        Assert.Equal(9, saturated);

        // The seed spread of the arm that is meant to be untouched, which is the yardstick
        // the cost has to clear. A kill line resting on a spread that is nought would admit
        // any gain at all, so it is read out rather than assumed.
        var spread = stationary.Max() - stationary.Min();
        var quiet = stationary.Average();
        var fastest = scored[50].Average();

        output.WriteLine(
            $"still {quiet:F3} over a spread of {spread:F3}, fastest turn {fastest:F3}");

        Assert.True(quiet - fastest > spread,
            $"turning every fifty rounds cost {quiet - fastest:F3} against a stationary "
            + $"spread of {spread:F3}, so the turn is not reaching the learner and this "
            + "world prices nothing");
    }
}
