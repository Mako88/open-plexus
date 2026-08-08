using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The world's one hard constraint, checked by enumeration rather than hoped for.
/// </summary>
/// <remarks>
/// <para>
/// <b>A WORLD SCOREABLE BY A BAG OF PARTS IS CIFAR WITH BETTER PICTURES.</b> The whole
/// reason this world exists is that a ten-way label has no arrangement, so no score on
/// one can tell a front end that manufactures reusable symbols from one that emits a
/// holistic blob. If the parts alone carry the answer here, the exercise repeats and
/// nobody finds out until a number has already been written up.
/// </para>
/// <para>
/// <b>SO IT IS CHECKED THE ONLY WAY THAT SETTLES IT: OVER THE WHOLE SPACE.</b> A
/// sampled version of this test would pass on a world that leaked a little, and a
/// little is all it takes — the leak becomes the score, and the score becomes the
/// finding.
/// </para>
/// </remarks>
public sealed class ArrangedTests(ITestOutputHelper output)
{
    private static ArrangedSettings Small => new() { Side = 3, Cell = 3, Clutter = 1, Hold = 4 };

    /// <summary>What is present, with no word about where any of it is.</summary>
    private static string Parts(Layout layout) =>
        string.Join(",", layout.Places.Select(one => one.Shape).Order());

    [Fact]
    public void Knowing_every_part_that_is_present_says_nothing_about_the_answer()
    {
        // THE CONSTRAINT, AS A COUNT RATHER THAN AS AN ARGUMENT. Group the entire
        // space by what it CONTAINS and throw away where everything is: if any group
        // leans, a front end that names the parts and forgets their places can beat
        // chance, and this world is not the instrument it claims to be.
        var world = new Arranged(Small, seed: 1);

        var groups = world.Layouts()
            .GroupBy(Parts)
            .Select(group => (group.Key, Left: group.Count(one => one.Outcome == 0), group.Count()))
            .ToList();

        Assert.NotEmpty(groups);

        foreach (var (parts, left, total) in groups)
            Assert.True(left * 2 == total,
                $"the bag {{{parts}}} answers left {left} times in {total}. A front end "
                + "that forgets where things are can score above chance on this world, "
                + "which is the one thing it was built not to allow.");

        output.WriteLine($"{groups.Count} distinct bags of parts, every one of them even");
    }

    [Fact]
    public void And_the_swap_is_why_that_is_true_rather_than_a_coincidence()
    {
        // THE MAP THAT MAKES THE COUNT INEVITABLE. Every layout has a partner holding
        // the identical parts and the opposite answer, and the partner is in the
        // space -- so the pairing above cannot be broken by a change to the clutter,
        // the grid or the resolution. A count that happens to come out even is a
        // property of one configuration; an involution is a property of the world.
        var world = new Arranged(Small, seed: 1);

        var space = world.Layouts().ToHashSet();

        Assert.NotEmpty(space);

        foreach (var layout in space)
        {
            var swapped = Arranged.Swapped(layout);

            Assert.Contains(swapped, space);
            Assert.Equal(1 - layout.Outcome, swapped.Outcome);
            Assert.Equal(Parts(layout), Parts(swapped));

            // AND IT STAYS ON ITS OWN SIDE OF THE HELD-OUT LINE, which is what makes
            // the exam balanced without anything balancing it. Withholding one half of
            // a swapped pair would hand the withheld set a majority class, and a
            // constant answer would then beat chance on it.
            Assert.Equal(layout.Shown, swapped.Shown);

            // AND THE LIGHT IS THE SAME, which closes the other way a bag of parts
            // could leak. `Winnow` centres a reading before it projects it, so a
            // scene brighter on one answer than the other would be separable by
            // total intensity alone -- parts, again, wearing a different hat.
            Assert.Equal(world.Render(layout).Sum(), world.Render(swapped).Sum(), 9);
        }
    }

    [Fact]
    public void The_withheld_arrangements_are_never_drawn_and_the_exam_is_balanced()
    {
        var world = new Arranged(Small, seed: 7);

        // A HELD-OUT SET WITH A MAJORITY CLASS IS AN EXAM A CONSTANT ANSWER PASSES,
        // and this world holds arrangements back in swapped PAIRS precisely so that
        // cannot happen. Asserted rather than argued.
        Assert.NotEmpty(world.Withheld);
        Assert.Equal(
            world.Withheld.Count(one => one.Outcome == 0),
            world.Withheld.Count(one => one.Outcome == 1));

        // AND THE WORLD MUST NEVER WANDER INTO IT. A withheld set the generator can
        // reach measures the same thing a trailing accuracy does, more slowly.
        var kept = world.Withheld
            .Select(one => string.Join(",", one.Seen.Select(pixel => pixel > 0.5 ? '1' : '0')))
            .ToHashSet(StringComparer.Ordinal);

        for (var draw = 0; draw < 20_000; draw++)
        {
            var shown = world.Next();
            var seen = string.Join(",", shown.Seen.Select(pixel => pixel > 0.5 ? '1' : '0'));

            Assert.DoesNotContain(seen, kept);
        }

        output.WriteLine(
            $"{world.Drawn} arrangements drawn, {world.Withheld.Count} scenes withheld");
    }

    [Fact]
    public void The_drawn_stream_is_even_and_reproducible()
    {
        // TWO WORLDS ON ONE SEED SEE THE SAME THING. Fork 12 has been reopened three
        // times, and every time it was something outside this file -- which is why
        // the check belongs in every world rather than in one.
        var first = new Arranged(Small, seed: 3);
        var second = new Arranged(Small, seed: 3);

        var left = 0;

        for (var draw = 0; draw < 5_000; draw++)
        {
            var one = first.Next();
            var two = second.Next();

            Assert.Equal(one.Outcome, two.Outcome);
            Assert.Equal(one.Seen, two.Seen);

            if (one.Outcome == 0) left++;
        }

        // EVEN, BECAUSE THE DRAW IS UNIFORM OVER A SPACE CLOSED UNDER THE SWAP. A
        // world whose chance bar was not really a half would make every score above
        // it a comparison against the wrong number.
        Assert.InRange(left / 5_000.0, 0.47, 0.53);
    }

    [Fact]
    public void A_shape_looks_the_same_wherever_it_goes_and_different_from_the_others()
    {
        // THE RECURRENCE THE WORLD IS FOR. A part must be the same part in every cell,
        // or there is nothing for a reusable symbol to be reused ON -- and the shapes
        // must differ from each other, or the clutter is indistinguishable from a
        // marker and the answer stops being about arrangement.
        var world = new Arranged(Small, seed: 1);

        var patches = new Dictionary<int, string>();

        foreach (var cell in new[] { 0, 4, 8 })
        foreach (var shape in Enumerable.Range(0, Arranged.Shapes))
        {
            var only = new Layout
            {
                Places = [new Placed { Shape = shape, Cell = cell }],
                Outcome = 0,
                Shown = true,
            };

            var reading = world.Render(only);

            // READ BACK OUT OF THE CELL IT WAS DRAWN IN, so what is compared is the
            // GLYPH and not where it sat.
            var top = cell / 3 * 3;
            var mleft = cell % 3 * 3;

            var patch = string.Concat(
                from down in Enumerable.Range(0, 3)
                from across in Enumerable.Range(0, 3)
                select reading[((top + down) * world.Pixels) + mleft + across] > 0.5 ? '1' : '0');

            if (patches.TryGetValue(shape, out var already))
                Assert.Equal(already, patch);
            else
                patches[shape] = patch;
        }

        Assert.Equal(Arranged.Shapes, patches.Values.Distinct(StringComparer.Ordinal).Count());

        foreach (var (shape, patch) in patches.OrderBy(one => one.Key))
            output.WriteLine($"shape {shape}: {patch}");
    }

    [Fact]
    public void No_shape_is_a_solid_block_at_any_resolution()
    {
        // THE BUDGET FOR A FAILURE CLASS, AND THE CLASS IS "A PART NO SENSE CAN SEE".
        // `Winnow` centres a reading before it projects it, so a uniformly filled patch
        // and an empty one are the SAME reading -- and the first shape here was a solid
        // block. The whole-image arm never noticed, because a whole picture is never
        // uniform; a front end reading one part at a time would have coded a filled
        // cell as an empty one, silently, and the score would have been read as a
        // learner that could not learn.
        //
        // SO IT IS CHECKED AT EVERY RESOLUTION AND NOT JUST THE DEFAULT ONE, because
        // the shapes are predicates over the patch and a predicate can go uniform at a
        // size nobody tried.
        foreach (var cell in new[] { 3, 4, 5, 6, 8 })
        {
            var world = new Arranged(new ArrangedSettings { Cell = cell, Clutter = 0 }, seed: 1);

            foreach (var shape in Enumerable.Range(0, Arranged.Shapes))
            {
                var reading = world.Render(new Layout
                {
                    Places = [new Placed { Shape = shape, Cell = 0 }],
                    Outcome = 0,
                    Shown = true,
                });

                var lit = 0;

                for (var down = 0; down < cell; down++)
                for (var across = 0; across < cell; across++)
                    if (reading[(down * world.Pixels) + across] > 0.5) lit++;

                Assert.InRange(lit, 1, (cell * cell) - 1);
            }
        }
    }

    [Fact]
    public void The_front_end_can_tell_the_two_answers_apart_at_all()
    {
        // THE CEILING, COMPUTED RATHER THAN DISCOVERED AFTERWARDS. A learner cannot
        // beat the front end it is fed, so before any score is read it is worth
        // knowing how often two scenes with OPPOSITE answers arrive as the same set
        // of codes. That is an exact upper bound on this world, and it is the number
        // `Cifar` could never have.
        //
        // AND IT IS A COLLAPSE DETECTOR TOO. On CLEVR a projection over three numbers
        // emitted one tag for four thousand objects and nothing said so.
        var world = new Arranged(Small, seed: 1);
        var sensing = new Winnowing(ArrangedRun.Patch, world.Width);

        var said = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);

        var layouts = 0;

        foreach (var layout in world.Layouts())
        {
            layouts++;

            var tag = string.Join(
                ",",
                sensing.Codify(world.Render(layout)).Select(code => code.Value).Order());

            if (!said.TryGetValue(tag, out var answers)) said[tag] = answers = [];
            answers.Add(layout.Outcome);
        }

        var confused = said.Count(one => one.Value.Count > 1);

        output.WriteLine(
            $"{layouts} scenes -> {said.Count} distinct tags, {sensing.Distinct} watched "
            + $"over {sensing.Emitted} readings; {confused} tags carry both answers");

        // NOT A BAR ON QUALITY, A BAR ON THE FRONT END BEING THERE AT ALL. A
        // projection that mushed most of the world into tags carrying both answers
        // would make every downstream number a measurement of nothing, and it would
        // read exactly like a learner that could not learn.
        Assert.True(confused * 2 < said.Count,
            $"{confused} of {said.Count} tags carry both answers, so the front end has "
            + "collapsed this world and no score taken through it means anything.");
    }

    [Fact]
    public void A_tiled_front_end_says_the_part_and_says_where_it_is()
    {
        // THE TWO CLAIMS THE ARM RESTS ON, ASSERTED SEPARATELY BECAUSE EITHER COULD
        // HOLD WITHOUT THE OTHER. A front end saying only what is present makes the
        // world unscoreable; one saying only where things are makes nothing
        // transferable. It has to do both, and the bare code has to be literally the
        // shared part of the placed ones or rung five has nothing to reach for.
        var world = new Arranged(Small, seed: 1);
        var sensing = new Tiling(ArrangedRun.Patch, world.Pixels, 3);

        Assert.Equal(9, sensing.Patches);

        ImmutableHashSet<Code> Said(int shape, int cell) => [.. sensing.Codify(world.Render(
            new Layout { Places = [new Placed { Shape = shape, Cell = cell }], Outcome = 0, Shown = true }))];

        // THE SAME PART IN TWO PLACES SHARES CODES, which is the recurrence. A whole
        // picture projection shares none, because every winner reads pixels from
        // everywhere at once.
        var here = Said(shape: 0, cell: 0);
        var there = Said(shape: 0, cell: 8);

        Assert.NotEmpty(here.Intersect(there));

        // AND IT IS STILL NOT THE SAME MOMENT, which is the arrangement. A front end
        // whose two readings were identical would have thrown the position away and
        // the world would be unscoreable through it.
        Assert.NotEqual(here, there);

        // AND TWO DIFFERENT PARTS IN ONE PLACE DIFFER, or the clutter is
        // indistinguishable from a marker and nothing about arrangement survives.
        var wedge = Said(shape: 0, cell: 4);

        foreach (var other in Enumerable.Range(1, Arranged.Shapes - 1))
            Assert.NotEqual(wedge, Said(other, cell: 4));

        // AND AN EMPTY CELL IS NOT A FILLED ONE. `Winnow` centres before it projects,
        // so a uniformly filled patch and an empty one are the same reading -- which
        // is why no shape here is a solid block, and this is the check that keeps it
        // that way from the front end's side rather than the world's.
        var nothing = ImmutableHashSet.CreateRange(
            sensing.Codify(world.Render(new Layout { Places = [], Outcome = 0, Shown = true })));

        foreach (var shape in Enumerable.Range(0, Arranged.Shapes))
            Assert.NotEqual(nothing, Said(shape, cell: 4));

        output.WriteLine(
            $"{sensing.Distinct} distinct patches over {sensing.Emitted} readings, "
            + $"{wedge.Count} codes for a scene holding one part");
    }

    [Fact]
    public void Both_front_ends_are_measured_against_the_dullest_learner_there_is()
    {
        // THE BAR, AND IT COSTS NOTHING BECAUSE IT NEEDS NO LEARNING. A probe reads the
        // world and the front end and never the population, so this is a fact about how
        // much of the problem each arm CARRIES -- available before any run, and the only
        // thing that makes a run's number readable afterwards.
        //
        // AND THE PIXEL BAR IS THE SAME FOR BOTH ARMS BY CONSTRUCTION, which is what
        // makes it the world's difficulty rather than a front end's.
        var pixels = new List<double>();

        foreach (var looking in new[] { Looking.Whole, Looking.Tiled })
        {
            var run = new ArrangedRun(
                Small, new Brain(new CommittingSettings(), seed: 1), looking, seed: 1);

            var bar = run.Measure();

            pixels.Add(bar.OnPixels.Accuracy);

            Assert.Equal(882, bar.OnCodes.Trained);
            Assert.Equal(252, bar.OnCodes.Tested);

            output.WriteLine(
                $"{looking,-6} | pixels {bar.OnPixels.Accuracy:F3} "
                + $"codes {bar.OnCodes.Accuracy:F3} over {bar.Features} features");
        }

        Assert.Equal(pixels[0], pixels[1], 12);
    }

    [Fact]
    public void What_the_scope_language_could_hold_if_the_learner_were_perfect()
    {
        // THE PLAN'S RULE FOR EXTENDING THE LANGUAGE IS DECIDABLE AND NOTHING HAD
        // DECIDED IT. Until this number exists, "the learner needs rung four" and "the
        // learner is leaving something on the table" are the same observation, and
        // picking a rung between them is the hand-specified bias the refutation table
        // calls ILP's cause of death.
        foreach (var looking in new[] { Looking.Whole, Looking.Tiled })
        {
            var run = new ArrangedRun(
                Small, new Brain(new CommittingSettings(), seed: 1), looking, seed: 1);

            foreach (var depth in new[] { 1, 2 })
            {
                var could = run.Reachable(depth);

                Assert.Equal(depth, could.Depth);

                output.WriteLine(
                    $"{looking,-6} depth {could.Depth} | covers {could.Covers:F3} "
                    + $"unseen {could.CoversUnseen:F3} | {could.Sound} sound scopes of "
                    + $"{could.Considered} considered, {could.Least} of them cover it"
                    + (could.Capped ? " | CAPPED" : string.Empty));

                Assert.InRange(could.CoversUnseen, 0.0, 1.0);
            }
        }
    }

    [Fact]
    public void Whether_the_learner_ever_holds_the_rules_its_own_genesis_can_mint()
    {
        // THE DIAGNOSIS, AND IT SPLITS THE GAP IN TWO. Genesis mints one-code
        // commitments and nothing else, so a code that is SOUND ON ITS OWN is reachable
        // by the very first thing the machine does. Whether the twelve are resident at
        // the end separates a learner that never found them from one that found them and
        // was outvoted -- and those two want completely different repairs.
        // AND THE ARM IS THE GATE, BECAUSE IT DECIDES WHETHER THEY ARE EVER MINTED.
        // `Unaccounted` is self-limiting by construction -- once anything proposes the
        // outcome there is no surprise and genesis stops -- which is fork 40 exactly,
        // and it is also how a mechanism quietly stops. On CIFAR the gate was
        // load-bearing and beat the ungated arm on every seed. Here the world is small
        // and the sound rules are one code each, so the gate may be starving it.
        var grid =
            (from looking in new[] { Looking.Whole, Looking.Tiled }
             from gate in new[] { Surprising.Unaccounted, Surprising.AnyFailure }
             select (looking, gate)).ToArray();

        // FOUR WHOLE RUNS THAT SHARE NOTHING, AND THIS WAS THE SLOWEST TEST IN THE SUITE
        // AT 184 SECONDS OF THEM WAITING FOR EACH OTHER. `ArrangedRun` holds no bus and
        // is synchronous end to end, so a fixed seed fixes every number it reports
        // whatever else the machine is doing -- see `Fixture.Abreast` for why that is the
        // condition and why no bus world may go through it.
        var arms = Fixture.Abreast(
            [.. grid.Select<(Looking Looking, Surprising Gate), Func<(Reached Could, Grounded Got, HashSet<Code> Alone)>>(
                one => () =>
                {
                    var run = new ArrangedRun(
                        Small,
                        new Brain(new CommittingSettings { Surprising = one.Gate }, seed: 1),
                        one.Looking,
                        seed: 1);

                    var could = run.Reachable(depth: 1);
                    var got = run.Run(20_000);

                    // WHAT IT HOLDS, SPELLED BACK OUT, so a minted name cannot hide a
                    // scope that is really one code wearing a hat.
                    return (could, got, Fixture.Alone(run.Held));
                })]);

        for (var at = 0; at < grid.Length; at++)
        {
            var (looking, gate) = grid[at];
            var (could, got, alone) = arms[at];

            var found = could.Alone.Count(alone.Contains);

            output.WriteLine(
                $"{looking,-6} {gate,-11} | {found} of {could.Alone.Length} sound single "
                + $"codes resident, of {got.Tally.Resident} commitments "
                + $"({got.Tally.Minted} minted, {got.Tally.Repaired} repaired) · "
                + $"unseen {got.Tally.Unseen!.Accuracy:F3} against a ceiling of "
                + $"{could.CoversUnseen:F3} · sound {got.Rules.Sound} unsound {got.Rules.Unsound} "
                + $"(narrowed {got.Rules.Narrowed}, rootless {got.Rules.Rootless}) · "
                + $"believed {got.Rules.Trusted:F3} sound vs {got.Rules.Doubted:F3} "
                + $"unsound · mean scope {got.Rules.Scope:F2}");

            // THE TWO WAYS AN UNSOUND RULE SURVIVES, AND THEY PARTITION. Either
            // subsumption had a general parent to absorb it into and declined, or there
            // was no parent and nothing in the mechanism set could have removed it --
            // `Cull` returns early below capacity, and this world never reaches it.
            Assert.Equal(got.Rules.Unsound, got.Rules.Narrowed + got.Rules.Rootless);

            Assert.NotEmpty(could.Alone);
        }
    }

    [Fact]
    public void A_run_finishes_and_every_commitment_it_holds_is_graded()
    {
        // END TO END, AND SHORT ON PURPOSE. What this asserts is that the world, the
        // front end, the learner and the soundness check compose -- the numbers are a
        // separate question and belong in a commit message, not in a bar here.
        var run = new ArrangedRun(Small, new Brain(new CommittingSettings(), seed: 1), Looking.Whole, seed: 1);

        var got = run.Run(4_000);

        Assert.Equal(4_000, got.Tally.Rounds);
        Assert.Equal(0.5, run.Chance);

        // THE THREE BUCKETS ARE A PARTITION. A commitment is contradicted, or it is
        // not and fires somewhere, or it fires nowhere -- and `Inert` existing at all
        // is what stops a vacuous rule being counted as a true one.
        Assert.Equal(
            run.World.Layouts().Count(),
            got.Rules.Layouts);

        Assert.True(got.Rules.Sound + got.Rules.Unsound + got.Rules.Inert > 0, "nothing was graded at all");

        Assert.NotNull(got.Tally.Unseen);

        output.WriteLine(
            $"recent {got.Tally.Recent:F3} · resident {got.Tally.Resident} · "
            + $"codes/round {got.Tally.Codes:F1} · sound {got.Rules.Sound} · "
            + $"unsound {got.Rules.Unsound} · inert {got.Rules.Inert} over {got.Rules.Layouts} scenes");

        output.WriteLine(
            $"withheld: {got.Tally.Unseen!.Accuracy:F3} over "
            + $"{got.Tally.Unseen.Answered}/{got.Tally.Unseen.Asked}, "
            + $"silence {got.Tally.Unseen.Silence:F3}");

        output.WriteLine($"front end: {got.Tags} tags over {got.Readings} readings");
    }
}
