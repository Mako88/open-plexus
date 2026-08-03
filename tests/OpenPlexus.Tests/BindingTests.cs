using OpenPlexus.Graph;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The world this architecture provably cannot do, and the prediction registered
/// before it was ever run.
/// </summary>
/// <remarks>
/// <para>
/// <b>PRE-REGISTERED, 2026-08-03, BEFORE THE FIRST EXECUTION: the unbound arm
/// scores EXACTLY AT CHANCE.</b> Not poorly — at chance, because the two
/// situations it is asked to tell apart are literally the same input. If it
/// scored meaningfully above chance, the model of this architecture written in
/// the handoff would be wrong and the four borrowings planned on top of it would
/// need revisiting before any of them was built.
/// </para>
/// <para>
/// <b>Measured, 16 seeds, stamina 12: 0.5240 ± 0.0268 against a chance of
/// 0.5000</b> — nine tenths of a standard error — while the control arm, which
/// differs only in a fact the counts can hold, scores 0.9167 ± 0.0095. Nearly
/// fourteen standard errors apart, on identical input.
/// </para>
/// <para>
/// <b>Re-baselined 2026-08-03 once <see cref="Seeds.Apart"/> reached
/// <see cref="Sweep"/>.</b> The first reading was 0.5064 ± 0.0213 against
/// 0.9247 ± 0.0072; the claim is unchanged and every error bar is wider, which
/// is what removing correlated seeds is supposed to do.
/// </para>
/// <para>
/// <b>The proof is the world tests, not the accuracy.</b> Two scenes with
/// opposite bindings emit the identical code sequence, which is asserted rather
/// than argued; the accuracy is what that identity looks like from the far end of
/// the system.
/// </para>
/// </remarks>
public sealed class BindingTests
{
    private static BindingSettings World(bool bound, int concepts = 8, int codes = 3) => new()
    {
        Concepts = concepts, CodesPerAttribute = codes, Bound = bound,
    };

    // ---- what the world is, asserted rather than described ------------------

    [Fact]
    public void The_two_arms_see_the_identical_input()
    {
        // THE PROOF, AND EVERYTHING ELSE HERE IS A DEMONSTRATION OF IT. The bound
        // world and the unbound world at the same seed emit the same codes in the
        // same order, scene after scene. Only which shape is answerable for which
        // colour differs -- and that lives nowhere in what the machine receives.
        var bound = new Binding(World(bound: true), seed: 1);
        var unbound = new Binding(World(bound: false), seed: 1);

        for (var i = 0; i < 2_000; i++)
            Assert.Equal(bound.Next().Codes, unbound.Next().Codes);
    }

    [Fact]
    public void And_the_answer_differs_about_half_the_time()
    {
        // THE COMPANION, and without it the test above passes for two worlds that
        // are simply the same world. Same input, different truth, on about half
        // the scenes -- which is what makes the task a coin flip from inside.
        var bound = new Binding(World(bound: true), seed: 1);
        var unbound = new Binding(World(bound: false), seed: 1);

        var differed = 0;
        for (var i = 0; i < 1_000; i++)
            if (!bound.Next().Shapes.SequenceEqual(unbound.Next().Shapes)) differed++;

        Assert.InRange(differed, 400, 600);
    }

    [Fact]
    public void The_binding_coin_is_fair_and_its_spread_is_honest()
    {
        // THE TRAP THAT COST A FALSE FIVE-SIGMA RESULT, 2026-08-03, kept as a
        // check so it cannot come back. A seeded Random in .NET normalises by
        // magnitude, so consecutive seeds produce nearly the same stream: over
        // seeds 1..8 the swap count landed in 19..23 of 39, a spread of about 1.3
        // where a fair coin gives 3.1. A standard error taken ACROSS those seeds
        // is then far too small, and this world's headline read as five sigma
        // below chance when it was sitting on it.
        //
        // So both halves are asserted. Fair on its own is not enough -- the
        // broken seeding was fair too, and still wrong.
        var counts = Enumerable.Range(1, 32).Select(seed =>
        {
            var world = new Binding(World(bound: false), seed);
            var swaps = 0;

            for (var moment = 0; moment < 400; moment++)
            {
                var scene = world.Next();

                // The questions are asked on every tenth scene, so that is the
                // subsample whose fairness actually decides a score.
                if (moment % 10 == 0 && moment != 0 && scene.Shapes[0] != scene.Colours[0])
                    swaps++;
            }

            return (double)swaps;
        }).ToList();

        var measured = new Measured { Arm = "swap rate", Values = counts };

        // Fair: 39 draws a seed, so half is 19.5 and the pooled error is small.
        Assert.True(Math.Abs(measured.Mean - 19.5) < 3 * measured.StdErr + 0.5,
            $"{measured} against 19.5 of 39");

        // AND HONEST: a fair coin over 39 draws has a standard deviation of 3.12,
        // so anything much under that means the seeds are agreeing with each
        // other rather than sampling.
        var spread = measured.StdErr * Math.Sqrt(counts.Count);
        Assert.True(spread > 2.0,
            $"the per-seed spread is {spread:F2} where a fair coin gives 3.12, " +
            "so these seeds are not independent");
    }

    [Fact]
    public void A_scene_is_two_objects_of_two_different_kinds()
    {
        var world = new Binding(World(bound: false), seed: 2);

        for (var i = 0; i < 1_000; i++)
        {
            var scene = world.Next();

            Assert.Equal(2, scene.Objects);
            Assert.Equal(4, scene.Codes.Count);
            Assert.NotEqual(scene.Colours[0], scene.Colours[1]);

            // Both objects' shapes are present, and they are the two kinds in the
            // scene. The binding permutes them; it never introduces a third.
            Assert.Equal([.. scene.Colours.Order()], [.. scene.Shapes.Order()]);
        }
    }

    [Fact]
    public void A_scene_shows_two_colours_and_two_shapes()
    {
        var world = new Binding(World(bound: false), seed: 2);

        for (var i = 0; i < 500; i++)
        {
            var attributes = world.Next().Codes
                .GroupBy(code => code.Modality)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.Equal(2, attributes[Binding.Colour]);
            Assert.Equal(2, attributes[Binding.Shape]);
        }
    }

    [Fact]
    public void Two_concepts_never_share_a_code()
    {
        var world = new Binding(World(bound: false, concepts: 6, codes: 4), seed: 3);

        var all = (from attribute in (byte[])[Binding.Colour, Binding.Shape]
                   from concept in Enumerable.Range(0, 6)
                   from code in world.Of(attribute, concept)
                   select code).ToArray();

        Assert.Equal(all.Length, all.Distinct().Count());
        Assert.All(all, code => Assert.Equal(
            (int)(code.Value / 1000), Binding.Concept(code)));
    }

    [Fact]
    public void A_blind_guess_is_worth_one_in_two()
    {
        // Not one in however many kinds there are: the question is a forced
        // choice between the two shapes actually in the scene.
        Assert.Equal(0.5, Binding.Chance, precision: 10);
    }

    // ---- what it measures ---------------------------------------------------

    /// <summary>
    /// Stamina 12, and the number is not arbitrary.
    /// </summary>
    /// <remarks>
    /// <b>It is what makes the choice actually forced.</b> A colour reaches its
    /// own concept's shape down a strong edge and the other object's shape down a
    /// weak one, and a weak edge is expensive under inverse cost — so at stamina 4
    /// only 16% of questions reach both candidates and a coin-flip score would be
    /// measuring what the walk could afford rather than what it preferred. Both
    /// candidates are in reach on 98.7% of questions here.
    /// </remarks>
    private const double Deep = 12.0;

    private static WalkSettings Dials(double stamina) => new()
    {
        Stamina = stamina, Value = ArrivalValue.Strength,
        Accumulate = Accumulate.Sum, Horizon = 50,
    };

    private static Task<Measured> Accuracy(bool bound, int seeds = 8) =>
        Sweep.ArmAsync(
            bound ? "stable" : "per-scene",
            seeds,
            async seed =>
            {
                using var run = new BindingRun(World(bound), Dials(Deep), seed);
                return (await run.RunAsync(400, every: 10)).Accuracy;
            });

    [Fact]
    public async Task The_system_cannot_say_which_attribute_belongs_to_which_object()
    {
        // THE PRE-REGISTERED PREDICTION, AND IT LANDED. Two objects, attributes
        // that swap, an input that is identical either way -- so the answer is a
        // coin, and nothing in the graph can make it otherwise. Measured at 16
        // seeds: 0.5240 +- 0.0268 against a chance of 0.5000, which is 0.9 sigma.
        var unbound = await Accuracy(bound: false);

        // TWO-SIDED, because "at chance" is the claim and a win would refute it
        // exactly as hard as a loss. The floor stops an unusually tight spread
        // from failing a result that is sitting on the prediction.
        var tolerance = Math.Max(3 * unbound.StdErr, 0.05);

        Assert.True(Math.Abs(unbound.Mean - Binding.Chance) < tolerance,
            $"{unbound} against chance {Binding.Chance:F4}, tolerance {tolerance:F4}");
    }

    [Fact]
    public async Task And_the_same_machinery_succeeds_the_moment_the_counts_can_see_it()
    {
        // THE CONTROL, AND IT RUNS THE OPPOSITE WAY FROM THE SENSES WORLD'S. A
        // null result is worthless without an arm that could have produced a
        // positive one: "at chance" and "the harness never measured anything"
        // look identical from outside. Here a colour keeps its shape, so plain
        // co-occurrence carries the answer -- same code, same dials, same
        // question, same input, and the only difference is a fact the counts can
        // hold. Measured at 16 seeds: 0.9167 +- 0.0095, which is 13.8 sigma clear.
        var bound = await Accuracy(bound: true);
        var unbound = await Accuracy(bound: false);

        Assert.True(bound.Mean > 0.75, $"the control only scored {bound}");
        Assert.True(bound.Separation(unbound) > 3.0,
            $"{bound} against {unbound} is only {bound.Separation(unbound):F1} sigma");
    }

    [Fact]
    public async Task Both_arms_build_the_identical_graph()
    {
        // THE FINDING STATED AS A COUNT. Learning sees only the codes, the codes
        // are identical, so the two worlds produce the same graph down to the last
        // edge -- 48 nodes and 1,730 edges either way. One of those graphs answers
        // the question and one of them cannot, and whatever separates them is
        // therefore not in there.
        using var bound = new BindingRun(World(bound: true), Dials(4.0), seed: 1);
        using var unbound = new BindingRun(World(bound: false), Dials(4.0), seed: 1);

        var withStructure = await bound.RunAsync(200, every: 10);
        var without = await unbound.RunAsync(200, every: 10);

        Assert.Equal(withStructure.Nodes, without.Nodes);
        Assert.Equal(withStructure.Edges, without.Edges);
    }

    [Fact]
    public async Task It_answers_with_the_colour_s_own_kind_whatever_the_scene_did()
    {
        // THE MECHANISM, NOT THE SCORE, and it is what turns a null result into a
        // description. A colour co-occurs with its own kind's shape in every scene
        // it appears in, and with the other object's shape only when that kind
        // happens to be the partner -- so the counts point at the colour's own
        // kind whichever object it belonged to. Measured at 0.9167 in BOTH arms,
        // which is the control's accuracy to four decimal places -- because in
        // the stable world echoing IS the right answer.
        //
        // That is the whole of it: the system is not failing to choose. It is
        // answering a question about co-occurrence correctly, because that is the
        // only question its representation can hold.
        using var run = new BindingRun(World(bound: false), Dials(Deep), seed: 1);

        var result = await run.RunAsync(400, every: 10);

        Assert.True(result.Echo > 0.8, $"echoed on only {result.Echo:F4} of questions");

        // AND THE SCORE IS THE COIN, NOT THE SYSTEM. Right answers are the ones
        // where the world happened not to swap, which is exactly what an echo
        // predicts and is asserted rather than inferred from the mean.
        Assert.InRange(result.Right, result.Asked - result.Swapped - 4, result.Asked - result.Swapped + 4);
    }

    // ---- step 1a: grouping fixes learning and does not fix reference --------

    private static Task<Measured> Segmented(bool bound, int seeds = 8) =>
        Sweep.ArmAsync(
            bound ? "stable segmented" : "per-scene segmented",
            seeds,
            async seed =>
            {
                using var run = new BindingRun(
                    World(bound) with { Segmented = true }, Dials(Deep), seed);

                return (await run.RunAsync(400, every: 10)).Accuracy;
            });

    [Fact]
    public async Task Grouping_removes_the_edges_that_were_never_real()
    {
        // THE MECHANISM CHECK, AND IT IS UNAMBIGUOUS. Pairing gated by object
        // means a colour never joins the other object's shape, so the graph
        // holds only bindings that actually happened. Measured at 16 seeds:
        // 1,751 edges flat against 144 segmented, and the stable control rises
        // from 0.9167 to a perfect 1.0000 on the smaller graph.
        using var flat = new BindingRun(World(bound: true), Dials(Deep), seed: 1);
        using var grouped = new BindingRun(
            World(bound: true) with { Segmented = true }, Dials(Deep), seed: 1);

        var loose = await flat.RunAsync(400, every: 10);
        var tight = await grouped.RunAsync(400, every: 10);

        // Same codes, same order, same count of nodes -- only which pairs were
        // written differs, which is the whole of what grouping does.
        Assert.Equal(loose.Nodes, tight.Nodes);
        Assert.True(tight.Edges * 4 < loose.Edges,
            $"{tight.Edges} edges against {loose.Edges}, which is not the collapse expected");

        Assert.True(tight.Accuracy > loose.Accuracy,
            $"segmented {tight.Accuracy:F4} against flat {loose.Accuracy:F4}");
    }

    [Fact]
    public async Task And_it_still_does_not_lift_the_binding_task()
    {
        // PRE-REGISTERED BEFORE THE FIRST RUN OF THIS ARM, and it held: grouping
        // does NOT move the per-scene score. Measured at 16 seeds, 0.5465 +-
        // 0.0236 against flat's 0.5240 +- 0.0268 -- six tenths of a standard
        // error.
        //
        // AND THE REASON IS THE USEFUL PART. Grouping fixes LEARNING: the graph
        // now holds only bindings that happened. It cannot fix REFERENCE, because
        // the question is still asked with a colour and nothing else, and a
        // colour's aggregate still points at its own kind whichever object it
        // belonged to in the scene being asked about.
        //
        // **So an object file needs its INDEX in the question, not only in the
        // occasion.** That is the next arm, and this is what says so.
        var segmented = await Segmented(bound: false);

        var tolerance = Math.Max(3 * segmented.StdErr, 0.05);

        Assert.True(Math.Abs(segmented.Mean - Binding.Chance) < tolerance,
            $"{segmented} against chance {Binding.Chance:F4}, tolerance {tolerance:F4}");
    }

    // ---- the run says what it did -------------------------------------------

    [Fact]
    public async Task Every_run_reports_its_own_plumbing_and_has_nothing_to_complain_about()
    {
        // MORE LOAD-BEARING HERE THAN ANYWHERE ELSE IN THE PROJECT. This world's
        // headline is a number that does NOT move, and a harness wired to nothing
        // produces exactly that. The complaints are what stands between the two.
        using var run = new BindingRun(World(bound: false), Dials(Deep), seed: 1);

        var result = await run.RunAsync(400, every: 10);

        // EVERY COMPLAINT, WITH NOTHING EXEMPTED -- fork 22 is closed, so the
        // exemption this used to carry is gone with it.
        Assert.Empty(result.Complaints);
        Assert.Equal(0, result.Unsettled);

        // The choice was actually forced: both candidates were in reach, so a
        // coin-flip score is a preference and not an accident of what was found.
        Assert.True(result.Forced > 0.5, $"only {result.Forced:F4} of questions were forced");
        Assert.True(result.Deepest >= 2, $"deepest chain {result.Deepest}");
    }

    [Fact]
    public async Task A_run_whose_walk_never_left_home_says_so()
    {
        // The companion to the companion: at a stamina that cannot afford one hop
        // the complaint MUST fire, or the check above is passing for a report that
        // can never fail.
        using var run = new BindingRun(World(bound: false), Dials(0.5), seed: 1);

        var result = await run.RunAsync(200, every: 10);

        Assert.Contains(result.Complaints, one => one.Contains("left its origin"));
    }

    [Fact]
    public async Task A_run_that_never_forced_the_choice_says_so()
    {
        // THE COMPLAINT THIS WORLD NEEDED AND THE OTHERS DO NOT. A forced choice
        // between two candidates means nothing if only one was ever reachable, and
        // a shallow walk reaches exactly one. Measured at stamina 4: 5 of 39.
        using var run = new BindingRun(World(bound: false), Dials(4.0), seed: 1);

        var result = await run.RunAsync(400, every: 10);

        Assert.Contains(result.Complaints, one => one.Contains("both candidates"));
    }
}
