using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The world for step 4, built before step 4.
/// </summary>
/// <remarks>
/// <b>It exists because survival was gameable.</b> Snake scored by staying alive
/// and circling wins that — it lives longest and eats least. Keeping variables in
/// bounds cannot be gamed the same way, because everything falls whether or not
/// anything is done, so standing still is the fastest way to fail rather than the
/// safe option.
/// </remarks>
public sealed class HomeostatTests(ITestOutputHelper output)
{
    private static HomeostatSettings World() => new();

    private const int Steps = 400;

    private const long Rounds = 20_000;

    // ---- what the world is, asserted rather than described -----------------

    [Fact]
    public void The_world_is_arithmetically_capable_and_not_trivially_so()
    {
        // Both bounds, or the world measures nothing. Restoring less than
        // everything falls means nothing could hold it and the ceiling is
        // unreachable; restoring more than the fastest drain times the number of
        // needs means attending at random suffices and the ceiling is free.
        var world = new Homeostat(World());

        Assert.True(world.Restore > world.Falling,
            $"nothing could hold this body: restore {world.Restore} against "
            + $"fall {world.Falling}");

        Assert.True(world.Restore < world.Needs * world.Falls(world.Needs - 1),
            $"attending at random would hold this body, so it discriminates "
            + $"nothing: restore {world.Restore}");
    }

    [Fact]
    public void Everything_falls_whether_or_not_anything_is_done()
    {
        // The property that makes idling cost. Under survival, doing nothing was
        // the strategy; here it is the failure.
        var world = new Homeostat(World());
        var before = world.At.ToList();

        world.Step(null);

        Assert.All(Enumerable.Range(0, world.Needs),
            which => Assert.True(world.At[which] < before[which]));

        // And the fastest-falling one falls fastest, which is what makes spreading
        // attention evenly the wrong thing to do.
        Assert.True(
            before[world.Needs - 1] - world.At[world.Needs - 1] > before[0] - world.At[0]);
    }

    [Fact]
    public void A_drive_is_felt_as_a_band_and_not_read_as_a_number()
    {
        var world = new Homeostat(World());

        var felt = world.Feels();

        Assert.Equal(world.Needs, felt.Length);

        // ONE MODALITY PER VARIABLE, so the graph can tell hunger from thirst
        // without anything downstream knowing which is which.
        Assert.Equal(world.Needs, felt.Select(code => code.Modality).Distinct().Count());
    }

    // ---- what the graph does with it ---------------------------------------

    // ---- step 4's blocker: the front end ------------------------------------

    /// <summary>What a felt state says about magnitude, as a comparable string.</summary>
    private static string Bands(IEnumerable<Code> felt) =>
        string.Join(",", felt.Where(code => code.Modality < Homeostat.Rank)
            .OrderBy(code => code.Modality).Select(code => code.Value));

    /// <summary>What it says about order. See <see cref="Homeostat.Standing"/>.</summary>
    private static string Positions(Homeostat body) =>
        string.Join(",", Enumerable.Range(0, body.Needs).Select(body.Standing));

    [Fact]
    public void A_band_cannot_say_which_is_lowest_and_a_rank_can()
    {
        // The ceiling, asserted rather than argued, and it is fork 25's SHAPE IN A
        // SECOND WORLD. Two states of one body whose variables sit in the SAME
        // bands and in a DIFFERENT order. A front end that emits bands alone emits
        // the identical code set for both, so no amount of counting can separate
        // them -- and which is lowest is the only fact this world turns on.
        var ranked = new Homeostat(World() with { Ranked = true });

        var before = ranked.Feels();
        var wasStanding = Positions(ranked);

        // Everything falls, and attending to the first one puts it back on top.
        // One step is enough because the drains are uneven.
        ranked.Step(attend: 0);

        var after = ranked.Feels();
        var nowStanding = Positions(ranked);

        output.WriteLine($"bands {Bands(before)} -> {Bands(after)}");
        output.WriteLine($"ranks {wasStanding} -> {nowStanding}");

        // SAME BANDS. This is the state a banded front end cannot tell from the
        // one before it.
        Assert.Equal(Bands(before), Bands(after));

        // DIFFERENT ORDER, and it is the ordering that carries the answer.
        Assert.NotEqual(wasStanding, nowStanding);

        // ADDITIVE: the ranks are extra codes and the band codes are untouched, so
        // switching the arm off reproduces every earlier measurement exactly.
        var plain = new Homeostat(World()).Feels();
        Assert.Equal(plain.Length * 2, before.Length);
        Assert.All(plain, code => Assert.Contains(code, before));

        // And the ordering is a permutation, never a near-miss: every position is
        // held by exactly one variable, so the front end cannot emit a rank no
        // variable holds -- a state the graph would learn about and the body can
        // never be in again.
        var standing = Enumerable.Range(0, ranked.Needs).Select(ranked.Standing).ToList();
        Assert.Equal(Enumerable.Range(0, ranked.Needs), standing.Order());
    }

    // ---- and what it is when it is acted in --------------------------------

    /// <summary>Whichever variable is furthest from where it should be.</summary>
    /// <remarks>
    /// <b>The oracle, and it reads the world rather than the codes</b>, which is the
    /// licence a ceiling has and a mechanism does not. It says how well the body could be
    /// held with these actions, so a learner's number is read against something rather
    /// than against nothing.
    /// </remarks>
    private static IChooses Aimed(Homeostat body) =>
        Chooses.From(_ => body.Lowest);

    /// <summary>A variable drawn uniformly, knowing nothing.</summary>
    private static IChooses Blindly(Homeostat body, Random draw) =>
        Chooses.From(_ => draw.Next(body.Doings));

    /// <summary>How much of a run a body stayed inside its bounds.</summary>
    /// <param name="body">The world.</param>
    /// <param name="choosing">What to do about each state.</param>
    /// <param name="steps">How many turns.</param>
    private static double Held(
        Homeostat body, IChooses choosing, int steps)
    {
        var front = new Bodied(Feeling.Acted);
        var viable = 0;

        for (var step = 0; step < steps; step++)
        {
            body.Do(choosing.Choose(front.Codify(body.Now)));
            body.Next();

            if (body.Viable) viable++;
        }

        return viable / (double)steps;
    }

    /// <summary>
    /// The verb exists and the world answers it, which is what nothing could do before.
    /// </summary>
    /// <remarks>
    /// <b>The property rather than a comparison</b>, so it outlives whatever arms are run
    /// over it. A turn reports the state an action was taken in and the consequence that
    /// followed, so the state must be the one BEFORE the step and the outcome the one after
    /// -- reading both after would hand a commitment what it did and what it did it to at
    /// once, with nothing left to be wrong about.
    /// </remarks>
    [Fact]
    public void A_turn_reports_the_state_it_was_acted_in_and_what_followed()
    {
        var body = new Homeostat(World());

        var before = body.Feels();

        body.Do(0);

        var turn = body.Next();

        // The state acted in, not the one that resulted. Compared AS A SEQUENCE, because a
        // `readonly record struct` holding an `ImmutableArray` compares by the array's
        // identity -- this repo's own trap, and it fires here on two arrays that print the
        // same and are not the same object.
        Assert.Equal<IEnumerable<Code>>(before, turn.Seen.Felt);
        Assert.Equal(0, turn.Seen.Did);

        // And the consequence, read after the step. `Lowest` is a fact about where the body
        // stands NOW, so it must have moved on from what `before` describes.
        Assert.Equal(body.Lowest, turn.Outcome);

        // An action is spent once. A second turn with nothing chosen does nothing, which is
        // the arm rather than the absence of one -- and a pending action surviving its own
        // step would make every later round act on a choice nobody made.
        var next = body.Next();

        Assert.Null(next.Seen.Did);
    }

    /// <summary>
    /// Acting is worth something here, which is what makes the world an instrument.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The control a verb needs, and it costs no learning at all.</b> A world that could be
    /// held as well by drawing uniformly measures nothing about choosing, and this repo has
    /// already been caught by an arm reading identically to its control. The three arms are
    /// every choice that needs no population: the oracle, a uniform draw, and doing nothing.
    /// </para>
    /// <para>
    /// <b>Doing nothing is the arm rather than the absence of one</b>, which is why it is run
    /// here. Everything falls whether or not anything is done, so idling is the fastest way
    /// to fail -- and a learner scoring above it has cleared a bar that a world rewarding
    /// caution would not have set.
    /// </para>
    /// </remarks>
    [Fact]
    public void Aiming_holds_the_body_where_guessing_and_idling_do_not()
    {
        var aimed = new Homeostat(World());
        var blind = new Homeostat(World());
        var idle = new Homeostat(World());

        var byAim = Held(aimed, Aimed(aimed), Steps);
        var byLuck = Held(blind, Blindly(blind, new Random(1)), Steps);
        var byNothing = Held(idle, Chooses.From(_ => null), Steps);

        output.WriteLine(
            $"viable | aimed {byAim:F3} | uniform {byLuck:F3} | idle {byNothing:F3} "
            + $"| idling lasts {idle.Idling} steps of {Steps}");

        // The ceiling is reachable, or the world is unholdable and every arm over it is a
        // comparison between two failures.
        Assert.True(byAim > 0.99, $"the oracle held the body only {byAim:F3} of the time");

        // And it is not reachable by luck, or attending to the lowest and attending at
        // random differ in variance alone -- which is the world measuring nothing. The
        // margin is left unstated on purpose: what is asserted is that the arms differ, and
        // a prediction written into a wiring check fails two ways and reads the same.
        Assert.True(byLuck < byAim, $"a uniform draw held the body {byLuck:F3} against {byAim:F3}");

        // And idling is the floor rather than a safe option, which is the whole reason this
        // world replaced one scored on survival.
        Assert.True(byNothing < byLuck, $"doing nothing held the body {byNothing:F3}");
    }

    /// <summary>
    /// <b>Whether a chooser reading a population holds the body</b> better than one drawing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>Drives</c>, and it is the last idea owed off <c>csharp</c>.</b> A third factor
    /// from the body's own variables rather than a reward handed in: the population says what
    /// follows each action, and the preference over those outcomes is computed from the bands
    /// the learner feels. Nothing here is told that regulating is the task.
    /// </para>
    /// <para>
    /// <b>Three arms over one seam, which <c>IActed</c> named before anything filled it.</b>
    /// The oracle reads <c>Lowest</c> off the world and is the ceiling; the uniform draw is the
    /// floor; this is the learner between them. A number against neither says nothing, which is
    /// why both controls run in this same test rather than being cited from another.
    /// </para>
    /// <para>
    /// <b>What would drop it: landing on the uniform draw.</b> That says the population's
    /// expectations carry nothing about consequence and the preference is decorating a random
    /// walk. Beating the oracle is not the bar and would be the reading to distrust — the
    /// oracle sees the state exactly and this sees a quantised report of it.
    /// </para>
    /// <para>
    /// <b>It did not land on the floor, it landed through it.</b> Regulated 0.001 against the
    /// uniform draw's 0.238 and the oracle's 1.000, on 19,993 rounds of 20,000 where a
    /// commitment named the action — so the fallback is not what ran and the chooser is worse
    /// than choosing at random by two orders.
    /// </para>
    /// <para>
    /// <b>And it predicted perfectly while doing it</b>, 1.000 against the uniform arm's 0.956,
    /// off fourteen commitments against ninety-four. The two halves came apart: a chooser that
    /// ranks by what it expects can win the prediction by making the world constant, and a
    /// collapsed body is perfectly predictable. Nothing scored the regulating, so nothing
    /// stopped it.
    /// </para>
    /// <para>
    /// <b>Which is the never-varied genesis rule arriving from the other end.</b> This file
    /// already records that a body held perfectly steady emits the same bands every round and
    /// mints nothing; a body pinned at the floor is equally constant, and the fourteen
    /// commitments are what a run learns from a world that stopped moving. The rule is
    /// symmetric and only one end of it had been seen.
    /// </para>
    /// <para>
    /// <b>So the preference is what lost rather than the idea</b>, and this repo allows a
    /// losing arm one more shape on exactly that reading. Wanting a high band on the expected
    /// lowest variable is satisfiable by making every band equal at the bottom, so the
    /// preference has no term that a dead body fails. What is forbidden is leaving it off
    /// while nobody decides, so it stays wired and measured until the next shape is run
    /// against this row.
    /// </para>
    /// <para>
    /// <b>And the silence is reported beside the score</b>, because a fallback is a control arm
    /// nobody meant to run. A chooser told nothing on most rounds IS its fallback, and this
    /// repo has already watched an arm drift to its control for free. <c>Told</c> against
    /// <c>Untold</c> is what says which mechanism was running.
    /// </para>
    /// <para>
    /// <b>The fallback is the uniform draw rather than idling</b>, which is a decision and not
    /// a default. Idling is measured as the floor here, so falling back to it would make an
    /// early run score below its own control for a reason that is not about choosing.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_a_chooser_reading_a_population_holds_the_body_better_than_drawing()
    {
        Regulated Ran(string arm, Regulating regulating)
        {
            var brain = new Brain(new CommittingSettings { Capacity = 2000 }, 1);
            var run = new HomeostatRun(World(), brain, regulating, Feeling.Acted, seed: 1);

            var came = run.Run(rounds: Rounds, sweep: 500, target: 0.9, window: 1000);

            output.WriteLine(
                $"{arm,-8} | regulated {came.Viable:F3} | predicted {came.Tally.Recent:F3} "
                + $"| chance {run.Chance:F3} | held {brain.Held.Count,4}");

            return came;
        }

        var byDrives = Ran("drives", Regulating.Driven);
        var byAim = Ran("aimed", Regulating.Aimed);
        var byLuck = Ran("uniform", Regulating.Uniform);

        output.WriteLine(
            $"drives told {byDrives.Told} of {byDrives.Told + byDrives.Untold}, "
            + $"untold {byDrives.Untold} | aimed {byAim.Viable:F3} "
            + $"| uniform {byLuck.Viable:F3} | drives {byDrives.Viable:F3}");

        // No bar on the score, because what a population-reading chooser is worth on this body
        // has never been measured and a threshold written before the first reading would be the
        // answer rather than the finding. What IS asserted is that the mechanism ran: a chooser
        // told nothing every round is the fallback wearing its name, and that reading would be
        // indistinguishable from the uniform control no matter what the score said.
        Assert.True(byDrives.Told > 0,
            $"the population named an action on none of {Rounds} rounds, so every choice was "
            + "the fallback and this arm is its own control");
    }

    /// <summary>
    /// A trial refuses to run an acted world with nothing to act with.
    /// </summary>
    /// <remarks>
    /// <b>A fallback is a control arm nobody meant to run</b>, and this is that trap caught at
    /// the constructor. A run that quietly did nothing every round would report a body that
    /// had failed within <see cref="Homeostat.Idling"/> steps as though it were a learner's
    /// score, and nothing in the tally would say which had happened.
    /// </remarks>
    [Fact]
    public void An_acted_world_with_no_chooser_is_refused_rather_than_left_idle()
    {
        var body = new Homeostat(World());
        var brain = new Brain(new CommittingSettings(), 1);

        Assert.Throws<ArgumentNullException>(() =>
            new Bench(new Watching<Bodily>(body, new Bodied(Feeling.Acted)), brain));

        // And the other way round, because a chooser nobody asks is an arm that reads as
        // having run. The multiplexer is watched, so a chooser handed to it would sit
        // unused while its cell reported a policy.
        Assert.Throws<ArgumentException>(() =>
            new Bench(
                new Watching<IReadOnlyList<int>>(
                    new Multiplexer(new MultiplexerSettings { Address = 2 }, 1),
                    new Bits(2 + 4),
                    acting: Chooses.From(_ => 0)),
                brain));
    }

    /// <summary>
    /// Whether telling the learner what it DID buys anything over telling it only what it felt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The reading the verb exists for</b>, and the control that could kill it. An action in
    /// the moment is only worth having if the moment could not already say what follows. Under
    /// a uniform chooser the action is independent of the state, so it is information the bands
    /// do not contain — which makes this the cell where the two arms can differ at all.
    /// </para>
    /// <para>
    /// <b>What was said before the run is wrong.</b> It was that the arms would sit together
    /// under the oracle, and the reasoning was that attending to the lowest makes the action a
    /// deterministic function of the state, so saying it adds a code the learner could have
    /// derived. What happens is 0.000 against 0.500, and the reason is the shipped genesis
    /// rule: a body held perfectly steady emits the same bands every round, nothing has ever
    /// varied, and genesis roots on nothing — 20,000 rounds, no commitment minted, silent
    /// throughout. The action is the ONLY thing that varies there, because attending to the
    /// lowest makes which-variable rotate while the magnitudes barely move.
    /// </para>
    /// <para>
    /// <b>So a perfectly regulated body teaches nothing about itself</b>, which is a fact about
    /// this design meeting Ashby's rather than a fault in either. The never-varied rule is in
    /// the refutation table for good reasons measured elsewhere, and this is the first world
    /// where it takes a whole arm to silence. What is left to learn from is what was DONE.
    /// </para>
    /// <para>
    /// <b>And the uniform cell went the other way</b>, 0.975 blind against 0.956 acted, so
    /// saying what was done costs a little there. Which is arithmetic once it is looked at:
    /// one restore rarely moves which variable is lowest, so the bands almost determine the
    /// answer and the action code is mostly noise beside them. The kill line did not fire —
    /// it was the arms sitting TOGETHER under the uniform chooser, and they do not — but the
    /// cell that carries the design is the oracle one and that was not the prediction.
    /// </para>
    /// <para>
    /// <b>And what is asserted is that they differ somewhere</b>, never which way or by how
    /// much. A prediction written into a wiring check fails two ways and reads the same, and
    /// this repo has already paid for that once — which is why the numbers above could be
    /// recorded as a correction instead of quietly matching a bar.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_saying_what_was_done_buys_anything_over_saying_what_was_felt()
    {
        var scored = new Dictionary<string, double>(StringComparer.Ordinal);

        foreach (var uniform in new[] { true, false })
        foreach (var feeling in new[] { Feeling.Blind, Feeling.Acted })
        {
            var brain = new Brain(new CommittingSettings { Capacity = 2000 }, 1);

            var run = new HomeostatRun(
                World(),
                brain,
                uniform ? Regulating.Uniform : Regulating.Aimed,
                feeling,
                seed: 1);

            var came = run.Run(rounds: Rounds, sweep: 500, target: 0.9, window: 1000);

            scored[$"{(uniform ? "uniform" : "aimed  ")} {feeling}"] = came.Tally.Recent;

            output.WriteLine(
                $"{(uniform ? "uniform" : "aimed  ")} {feeling,-6} "
                + $"| drawn {came.Tally.Recent:F3} | chance {run.Chance:F3} "
                + $"| viable {(came.Standing ? "yes" : "no ")} | held {brain.Held.Count,4} "
                + $"| silent {came.Tally.Silent} | repaired {came.Tally.Repaired}");
        }

        // Under a uniform chooser the action is the one thing the bands cannot supply, so the
        // arms have to come apart there or the moment is not carrying it.
        Assert.NotEqual(scored["uniform Blind"], scored["uniform Acted"], precision: 3);

        // And under the oracle the blind arm holds NOTHING, which is the reading worth failing
        // the build over. A body held steady never varies a band, genesis roots on nothing,
        // and the run is silent for its whole length -- so the acted arm is not merely ahead
        // there, it is the only arm with a population at all. This asserts the silence rather
        // than the gap, because the gap is a score and the silence is the mechanism.
        var blind = new HomeostatRun(
            World(),
            new Brain(new CommittingSettings { Capacity = 2000 }, 1),
            Regulating.Aimed,
            Feeling.Blind,
            seed: 1);

        Assert.Equal(
            Rounds,
            blind.Run(rounds: Rounds, sweep: 500, target: 0.9, window: 1000).Tally.Silent);
    }
}
