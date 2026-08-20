using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The crossing world, and the properties the experiment rests on.
/// </summary>
/// <param name="output">Where the rows go.</param>
/// <remarks>
/// <para>
/// <b>A shape beside a fact would make the crossing a lookup</b>, and nothing downstream
/// could tell the difference. So the absence is asserted here rather than trusted, the way
/// <see cref="SensesTests"/> asserts its own.
/// </para>
/// <para>
/// <b>And the two exams have to be the same drawing</b>, or the pair stops isolating
/// anything. They differ in which sense is asked and in nothing else, which is what lets a
/// gap between them be read as binding rather than as position.
/// </para>
/// </remarks>
public sealed class CrossingTests(ITestOutputHelper output)
{
    /// <summary>How many rounds the measurement runs for.</summary>
    /// <remarks>
    /// <b>Sized to a runner and not to a target.</b> Two hundred rounds took three minutes
    /// on this front end, so twenty thousand is five hours — and a runner reports that as
    /// cancelled rather than as overrun, which is the trap about a timeout wearing the
    /// concurrency group's clothes. Two thousand is half an hour and it is what the cost
    /// allows; raise it when the moment gets narrower rather than when patience does.
    /// </remarks>
    private const int Rounds = 2_000;

    private static CrossingSettings Clean(
        int words = 16, int facts = 4, int stride = 4, int asked = 64, bool scrambled = false) =>
        new()
        {
            Words = words,
            Facts = facts,
            Stride = stride,
            Asked = asked,
            Scrambled = scrambled,
        };

    [Fact]
    public void A_shape_and_a_fact_are_never_shown_together()
    {
        var world = new Crossing(Clean(), seed: 1);

        for (var round = 0; round < 5_000; round++)
        {
            var turn = world.Next();
            var said = turn.Seen.Said.Select(code => code.Modality).ToHashSet();

            Assert.False(turn.Seen.Shape is not null && said.Contains(Crossing.Fact),
                "a moment showed a drawing beside a fact, which makes the crossing a lookup");
        }
    }

    [Fact]
    public void Both_occasions_do_occur()
    {
        // The companion. Without it the test above passes for a world that only ever tells
        // facts, or one that draws nothing at all.
        var world = new Crossing(Clean(), seed: 1);
        var shapes = 0;
        var facts = 0;

        for (var round = 0; round < 1_000; round++)
        {
            var turn = world.Next();

            if (turn.Seen.Shape is not null) shapes++;
            if (turn.Seen.Said.Any(code => code.Modality == Crossing.Fact)) facts++;
        }

        Assert.True(shapes > 300, $"only {shapes} of a thousand moments drew anything");
        Assert.True(facts > 300, $"only {facts} of a thousand moments told a fact");
    }

    [Fact]
    public void The_examinations_are_drawn_where_the_stream_never_draws()
    {
        // The property both exams rest on. A held-out POSITION is what makes them a question
        // about generalising rather than a round the stream has already run, and a world that
        // wandered into it would measure the trailing accuracy more slowly.
        var world = new Crossing(Clean(), seed: 2);

        var kept = world.Withheld
            .Select(turn => string.Join(",", turn.Seen.Shape!))
            .ToHashSet();

        Assert.NotEmpty(kept);

        for (var round = 0; round < 20_000; round++)
        {
            var turn = world.Next();

            if (turn.Seen.Shape is null) continue;

            Assert.DoesNotContain(string.Join(",", turn.Seen.Shape), kept);
        }
    }

    [Fact]
    public void The_two_examinations_differ_only_in_which_sense_is_asked()
    {
        // Without this the pair stops isolating anything: two exams on different drawings
        // would differ in the drawing as well as in the question, and a gap between them
        // could be either.
        var world = new Crossing(Clean(), seed: 3);

        Assert.Equal(world.Withheld.Count, world.Moved.Count);

        for (var at = 0; at < world.Withheld.Count; at++)
        {
            Assert.Equal(world.Withheld[at].Seen.Shape, world.Moved[at].Seen.Shape);

            Assert.Equal(
                [new Code(Crossing.Asks, Crossing.Fact)],
                world.Withheld[at].Seen.Said);

            Assert.Equal(
                [new Code(Crossing.Asks, Crossing.Symbol)],
                world.Moved[at].Seen.Said);
        }
    }

    [Fact]
    public void A_body_with_two_senses_puts_both_in_one_moment()
    {
        // The mechanism the whole world exists to present. A drawing and a word arriving in
        // separate moments would never co-fire, and co-firing is what rung five names.
        var world = new Crossing(Clean(), seed: 4);

        var body = new Compound<Crossed>(
        [
            new Tiling(Crossing.Shape, Lettering.Side, CrossingRun.Patch),
            new Passthrough(),
        ]);

        var together = 0;

        for (var round = 0; round < 200; round++)
        {
            var codes = body.Codify(world.Next().Seen);
            var senses = codes.Select(code => code.Modality).ToHashSet();

            if (senses.Contains(Crossing.Shape) && senses.Contains(Crossing.Symbol)) together++;
        }

        Assert.True(together > 50,
            $"only {together} of two hundred moments carried a drawing and a word at once, "
            + "so the two senses are not reaching one moment and nothing can bind them");
    }

    /// <summary>
    /// <b>What the crossing scores, and the position exam beside it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The first reading of fork 107 with a learner in it</b>, and the pair is what makes
    /// it readable. A drawn word at an unseen offset either still names its word or does not;
    /// only where it does is a nought on the fact a statement about binding.
    /// </para>
    /// <para>
    /// <b>What would drop this arm</b>: the drawn stream itself sitting at chance. Then
    /// nothing was learnt at all, both exams are about that, and the run says nothing about
    /// either question.
    /// </para>
    /// <para>
    /// <b>A sweep, because the moment is the widest this repo has run.</b> A tiled canvas
    /// says thirty-six patches twice over, and genesis mints across a moment — two hundred
    /// rounds took three minutes and left eighteen thousand commitments, and twenty thousand
    /// rounds crashed the test host outright. That cost is fork 31 and fork 49 arriving
    /// together on a real world, and it belongs on a runner rather than in the suite.
    /// </para>
    /// </remarks>
    [Trait(Sweeps.Kind, Sweeps.Name)]
    [Fact]
    public void What_the_crossing_and_the_position_exams_come_to()
    {
        var world = Clean();
        var brain = new Brain(new CommittingSettings { Capacity = 4000 }, seed: 1);
        var chance = 1.0 / (world.Words + world.Facts);

        var read = new CrossingRun(world, brain, seed: 1)
            .Run(rounds: Rounds, sweep: 500, target: 0.9, window: 2000);

        var crossing = Assert.IsType<Examined>(read.Learnt.Unseen);
        var placed = Assert.IsType<Examined>(read.Placed);

        output.WriteLine(
            $"{world.Words} words, {world.Facts} facts, chance {chance:F3}, "
            + $"patch {CrossingRun.Patch}");

        output.WriteLine($"{"exam",-12}{"asked",8}{"answered",10}{"accuracy",10}");
        output.WriteLine(
            $"{"drawn",-12}{"",8}{"",10}{read.Learnt.Recent,10:F3}");
        output.WriteLine(
            $"{"position",-12}{placed.Asked,8}{placed.Answered,10}{placed.Accuracy,10:F3}");
        output.WriteLine(
            $"{"crossing",-12}{crossing.Asked,8}{crossing.Answered,10}{crossing.Accuracy,10:F3}");

        // The instrument first. A stream the learner never got anywhere on makes both exams
        // readings about that, and every number under it would be about the fault.
        Assert.True(read.Learnt.Recent > 2.0 * chance,
            $"the drawn stream scored {read.Learnt.Recent:F3} against a blind draw of "
            + $"{chance:F3}, so the world is not reaching the learner and the exams below "
            + "say nothing");

        // WHAT THE POPULATION ACTUALLY BUILT, because a nought on the crossing is a
        // restatement until something says which link is missing. Three counts answer it:
        // rules keyed on the drawing that expect a word, rules keyed on a word that expect a
        // fact, and rules whose scope spans both senses at once. The third is what binding
        // looks like from inside a population, and rung five renaming over it is what would
        // carry a fact back to a drawing.
        var held = brain.Held.All.ToList();

        var reading = held.Count(one =>
            one.Scope.Any(code => code.Modality == Crossing.Shape)
            && one.Expects.Modality != Crossing.Shape);

        var telling = held.Count(one =>
            one.Scope.Any(code => code.Modality == Crossing.Symbol)
            && one.Expects.Modality != Crossing.Shape);

        var spanning = held.Count(one =>
            one.Scope.Any(code => code.Modality == Crossing.Shape)
            && one.Scope.Any(code => code.Modality == Crossing.Symbol));

        var renamed = brain.Held.Births.Values.Count(birth => birth == Birth.Renamed);

        output.WriteLine(
            $"{held.Count} held: {reading} keyed on the drawing, {telling} on the word, "
            + $"{spanning} spanning both, {renamed} rewritten over a minted name");

        // A population holding nothing keyed on the drawing means the shape sense reached
        // no rule at all, and then both exams are readings about the front end. The two
        // numbers above are unreadable without this one.
        Assert.True(reading > 0,
            "no commitment is keyed on a drawn word, so the shape sense reached no rule and "
            + "neither exam is about binding");

        // And why rung five does or does not fire, which is the whole of what a nought on
        // the crossing turns into once the population is known to hold scopes spanning both
        // senses. The gate charges every ask to the first bar that stopped it, so this says
        // whether the rung is never ASKED, asked and finding nothing to repay, or finding a
        // pair and refusing it on the correction. Those are three different repairs.
        var naming = brain.Held;

        output.WriteLine(
            $"rung five: {naming.Asked} asked, {naming.Spoke} spoke, "
            + $"{naming.AtScarce} scarce, {naming.AtUnpaired} unpaired, {naming.AtRare} rare, "
            + $"{naming.AtIndependent} independent, {naming.AtUncertain} uncertain");

        if (naming.Lately is { } lately)
            output.WriteLine(
                $"  last ask: {lately.Scopes} scopes, {lately.Candidates} candidates, "
                + $"strongest {lately.Strongest}");

        // A rung nothing ever asks and a rung asked and refused are different faults with
        // different repairs, and a count of names cannot tell them apart. This is what makes
        // the difference readable, so it is asserted rather than printed.
        Assert.True(naming.Asked > 0,
            "rung five was never asked on a world built to make it fire, so the crossing's "
            + "nought is about when the rung is offered a population rather than about what "
            + "it does with one");
    }
}
