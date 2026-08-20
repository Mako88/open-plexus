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

    /// <summary>How often the run subsumes, abstracts and culls.</summary>
    /// <remarks>
    /// <b>Named here because rung five is asked once of it</b>, so the count of names a run
    /// can mint is bounded by <see cref="Rounds"/> over this and by nothing about the
    /// population. Lowering it is not an isolated arm: one interval drives subsumption,
    /// culling and abstraction together, which is the trap about a comparison that moves two
    /// things at once.
    /// </remarks>
    private const int Sweep = 500;

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
    /// <b>What the crossing scores, and the position exam beside it</b>, at two sweep
    /// cadences.
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
    /// says thirty-six patches, and genesis mints across a moment — two hundred rounds took
    /// three minutes with the placed half in, and twenty thousand crashed the test host.
    /// That cost is fork 31 and fork 49 arriving together on a real world.
    /// </para>
    /// <para>
    /// <b>And two cadences, a SCREEN rather than an arm.</b> `Bench` already says an
    /// absolute name count is bounded by the sweep calendar rather than by the gate, and on
    /// this world that bound is what decides the run — four asks over two thousand rounds,
    /// with tens of thousands of candidate pairs never looked at. One interval drives
    /// subsumption, culling and abstraction together, so a difference between these two
    /// rows has three possible causes and is not a finding about any of them.
    /// </para>
    /// <para>
    /// <b>What it is FOR</b> is deciding whether a dial is worth building. If the crossing
    /// stays at nought while the names rise, the ask rate is not what holds it and rung
    /// five's own interval need not be built. If the crossing moves, the confound has to be
    /// split and that is what the dial would be for.
    /// </para>
    /// </remarks>
    [Trait(Sweeps.Kind, Sweeps.Name)]
    [Fact]
    public void What_the_crossing_and_the_position_exams_come_to()
    {
        var world = Clean();
        var chance = 1.0 / (world.Words + world.Facts);

        output.WriteLine(
            $"{world.Words} words, {world.Facts} facts, chance {chance:F3}, "
            + $"patch {CrossingRun.Patch}, {Rounds} rounds");

        output.WriteLine(
            $"{"sweep",-7}{"drawn",8}{"position",10}{"crossing",10}{"held",8}{"span",7}"
            + $"{"named",7}{"asked",7}{"spoke",7}");

        foreach (var sweep in new[] { Sweep, Sweep / 5 })
        {
            var brain = new Brain(new CommittingSettings { Capacity = 4000 }, seed: 1);

            var read = new CrossingRun(world, brain, seed: 1)
                .Run(rounds: Rounds, sweep: sweep, target: 0.9, window: 2000);

            var crossing = Assert.IsType<Examined>(read.Learnt.Unseen);
            var placed = Assert.IsType<Examined>(read.Placed);

            var held = brain.Held.All.ToList();

            var reading = held.Count(one =>
                one.Scope.Any(code => code.Modality == Crossing.Shape)
                && one.Expects.Modality != Crossing.Shape);

            var spanning = held.Count(one =>
                one.Scope.Any(code => code.Modality == Crossing.Shape)
                && one.Scope.Any(code => code.Modality == Crossing.Symbol));

            var renamed = brain.Held.Births.Values.Count(birth => birth == Birth.Renamed);

            output.WriteLine(
                $"{sweep,-7}{read.Learnt.Recent,8:F3}{placed.Accuracy,10:F3}"
                + $"{crossing.Accuracy,10:F3}{held.Count,8}{spanning,7}{renamed,7}"
                + $"{brain.Held.Asked,7}{brain.Held.Spoke,7}");

            output.WriteLine(
                $"  refused: {brain.Held.AtScarce} scarce, {brain.Held.AtUnpaired} unpaired, "
                + $"{brain.Held.AtRare} rare, {brain.Held.AtIndependent} independent, "
                + $"{brain.Held.AtUncertain} uncertain");

            if (brain.Held.Lately is { } lately)
                output.WriteLine(
                    $"  last ask: {lately.Scopes} scopes, {lately.Candidates} candidates");

            // The instrument, taken on every row. A stream the learner never got anywhere on
            // makes both exams readings about that, and every number beside them would be
            // about the fault.
            Assert.True(read.Learnt.Recent > 2.0 * chance,
                $"at a sweep of {sweep} the drawn stream scored {read.Learnt.Recent:F3} "
                + $"against a blind draw of {chance:F3}, so the world is not reaching the "
                + "learner and the exams beside it say nothing");

            // A population holding nothing keyed on a drawn word means the shape sense
            // reached no rule at all, and then neither exam is about binding.
            Assert.True(reading > 0,
                $"at a sweep of {sweep} no commitment is keyed on a drawn word, so the shape "
                + "sense reached no rule and neither exam is about binding");

            // And the bound this screen exists for. The rung is asked once a sweep, so a run
            // can ask it this many times whatever the population holds -- which is `Bench`'s
            // own line about an absolute name count, arriving where it decides a result
            // rather than where it qualifies one.
            Assert.True(brain.Held.Asked <= (Rounds / sweep) + 1,
                $"rung five was asked {brain.Held.Asked} times over {Rounds / sweep} sweeps "
                + "at that cadence, so it is no longer asked once of each. Something has "
                + "changed about when the rung runs and this screen is measuring that");
        }
    }
}
