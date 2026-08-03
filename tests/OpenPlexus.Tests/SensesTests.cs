using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The second world, and the property the whole experiment rests on.
/// </summary>
/// <remarks>
/// <b>If sight and touch ever appeared together the result would be
/// meaningless</b> — the question would be a lookup rather than a composition,
/// and nothing downstream could tell the difference. So it is asserted here
/// rather than trusted.
/// </remarks>
public sealed class SensesTests
{
    private static SensesSettings Clean(int concepts = 8, int codes = 3) => new()
    {
        Concepts = concepts, CodesPerSense = codes, Noise = 0.0,
    };

    [Fact]
    public void Sight_and_touch_are_never_shown_together()
    {
        var world = new Senses(Clean(), seed: 1);

        for (var i = 0; i < 5_000; i++)
        {
            var moment = world.Moment();
            var senses = moment.Select(code => code.Modality).ToHashSet();

            Assert.False(senses.Contains(Senses.Sight) && senses.Contains(Senses.Touch),
                "a moment showed sight and touch at once, which makes the task a lookup");
        }
    }

    [Fact]
    public void Both_pairings_do_occur()
    {
        // The companion. Without it the test above passes for a world that only
        // ever shows one sense, or nothing at all.
        var world = new Senses(Clean(), seed: 1);
        var pairings = new HashSet<string>();

        for (var i = 0; i < 500; i++)
            pairings.Add(string.Join(
                ",", world.Moment().Select(c => c.Modality).Distinct().Order()));

        Assert.Contains($"{Senses.Sight},{Senses.Sound}", pairings);
        Assert.Contains($"{Senses.Sound},{Senses.Touch}", pairings);
        Assert.Equal(2, pairings.Count);
    }

    [Fact]
    public void A_clean_moment_is_two_senses_of_one_concept()
    {
        var world = new Senses(Clean(), seed: 2);

        for (var i = 0; i < 500; i++)
        {
            var moment = world.Moment();
            Assert.Equal(2, moment.Count);
            Assert.Single(moment.Select(Senses.Concept).Distinct());
        }
    }

    [Fact]
    public void Noise_puts_another_concept_in_the_moment()
    {
        // The companion to the test above, and the reason it specifies clean:
        // real co-occurrence is noisy, and a world without any rewards a
        // mechanism that cannot tolerate it.
        var noisy = new Senses(new SensesSettings
        {
            Concepts = 8, CodesPerSense = 3, Noise = 1.0,
        }, seed: 2);

        var strayed = 0;
        for (var i = 0; i < 200; i++)
            if (noisy.Moment().Select(Senses.Concept).Distinct().Count() > 1) strayed++;

        Assert.True(strayed > 100, $"only {strayed} of 200 moments carried a stray code");
    }

    [Fact]
    public void Every_sense_of_a_concept_says_which_concept_it_is()
    {
        var world = new Senses(Clean(concepts: 5, codes: 4), seed: 3);

        foreach (var sense in (byte[])[Senses.Sight, Senses.Sound, Senses.Touch])
            for (var concept = 0; concept < 5; concept++)
            {
                var codes = world.Of(sense, concept);

                Assert.Equal(4, codes.Count);
                Assert.All(codes, code => Assert.Equal(concept, Senses.Concept(code)));
                Assert.All(codes, code => Assert.Equal(sense, code.Modality));
            }
    }

    [Fact]
    public void Two_concepts_never_share_a_code()
    {
        // A collision would make two things one thing, which is the opposite of
        // what a front end is for.
        var world = new Senses(Clean(concepts: 6, codes: 4), seed: 3);

        var all = (from sense in (byte[])[Senses.Sight, Senses.Sound, Senses.Touch]
                   from concept in Enumerable.Range(0, 6)
                   from code in world.Of(sense, concept)
                   select code).ToArray();

        Assert.Equal(all.Length, all.Distinct().Count());
    }

    [Fact]
    public void A_blind_guess_is_worth_one_in_however_many_things_there_are()
    {
        Assert.Equal(1.0 / 12, new Senses(Clean(concepts: 12), seed: 1).Chance, precision: 10);
    }

    // ---- what the world is for ---------------------------------------------

    private static WalkSettings Dials(double stamina) => new()
    {
        Stamina = stamina, Value = ArrivalValue.Strength,
        Accumulate = Accumulate.Sum, Horizon = 50,
    };

    private static async Task<double> Accuracy(double stamina, bool scrambled, int seeds = 5)
    {
        var got = new List<double>();
        for (var seed = 1; seed <= seeds; seed++)
        {
            using var run = new SensesRun(new SensesSettings
            {
                Concepts = 12, CodesPerSense = 3, Noise = 0.1, Scrambled = scrambled,
            }, Dials(stamina), seed);

            got.Add((await run.RunAsync(400, every: 10)).Accuracy);
        }

        return got.Average();
    }

    [Fact]
    public async Task It_answers_a_question_it_was_never_told()
    {
        // THE RESULT THE PROJECT EXISTS FOR. Sight and touch never occur
        // together, so the pair being asked about has never been seen and a
        // memoriser scores exactly zero. Measured at 12 seeds: 0.8898 +- 0.0068
        // against a chance of 0.0833.
        var real = await Accuracy(stamina: 8.0, scrambled: false);

        Assert.True(real > 0.5, $"accuracy {real:F4} against a chance of {1.0 / 12:F4}");
    }

    [Fact]
    public async Task Scrambling_the_world_destroys_it()
    {
        // A CONTROL TESTS THE DATA, NOT THE CODE. Every mechanism runs
        // identically; only the structure the world contains is destroyed. If
        // accuracy survived this it was never composition. Measured at 12
        // seeds: 0.0311 +- 0.0086, which is BELOW chance, and two thirds of
        // questions get no answer at all.
        var scrambled = await Accuracy(stamina: 8.0, scrambled: true);

        Assert.True(scrambled < 0.1, $"the control still scored {scrambled:F4}");
    }

    [Fact]
    public async Task Composition_needs_the_depth_that_snake_said_to_avoid()
    {
        // AND THIS IS WHY A SECOND WORLD WAS WORTH BUILDING. In snake a deeper
        // walk dilutes prediction and shallow wins. Here depth IS the
        // mechanism: sight reaches touch only through sound, so a budget that
        // cannot afford two hops answers nothing at all.
        //
        // Measured at 12 seeds: stamina 2 answers 0 of 708 questions, stamina 4
        // scores 0.1384, stamina 8 scores 0.8884.
        var shallow = await Accuracy(stamina: 2.0, scrambled: false);
        var deep = await Accuracy(stamina: 8.0, scrambled: false);

        Assert.Equal(0.0, shallow);
        Assert.True(deep > shallow + 0.5, $"deep {deep:F4} against shallow {shallow:F4}");
    }
}
