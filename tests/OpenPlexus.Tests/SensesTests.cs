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

    private static Task<Measured> Accuracy(double stamina, bool scrambled, int seeds = 5) =>
        Sweep.ArmAsync(
            scrambled ? $"scrambled@{stamina}" : $"world@{stamina}",
            seeds,
            async seed =>
            {
                using var run = new SensesRun(new SensesSettings
                {
                    Concepts = 12, CodesPerSense = 3, Noise = 0.1, Scrambled = scrambled,
                }, Dials(stamina), seed);

                return (await run.RunAsync(400, every: 10)).Accuracy;
            });

    [Fact]
    public async Task It_answers_a_question_it_was_never_told()
    {
        // THE RESULT THE PROJECT EXISTS FOR. Sight and touch never occur
        // together, so the pair being asked about has never been seen and a
        // memoriser scores exactly zero. Measured at 12 seeds: 0.8077 +- 0.0215
        // against a chance of 0.0833.
        //
        // IT ROSE FROM 0.7906 WHEN FORK 22 WAS CLOSED, and that is the fix
        // showing up rather than noise: questions were being read before their
        // walk had finished, and "nothing reached yet" scores exactly like
        // "nothing to say".
        //
        // RE-BASELINED 2026-08-03, and the correction is worth reading. This was
        // published as 0.8898 +- 0.0068 on consecutive integer seeds. Under
        // `Seeds.Apart` the spread across seeds TRIPLES -- 0.081 against 0.024 --
        // and the mean falls about one true standard deviation. So the old error
        // bar was understating by more than three times, and the old mean was a
        // favourable draw sitting inside a spread nobody could see. The claim is
        // untouched; the confidence in it was inflated.
        var real = await Accuracy(stamina: 8.0, scrambled: false);

        // AGAINST THE SPREAD, NOT THE BARE MEAN. Three standard errors clear of
        // chance, so a lucky run of seeds cannot carry the project's headline
        // claim on its own.
        Assert.True(real.Mean - (3 * real.StdErr) > 1.0 / 12, $"{real} against chance {1.0 / 12:F4}");
    }

    [Fact]
    public async Task Scrambling_the_world_destroys_it()
    {
        // A CONTROL TESTS THE DATA, NOT THE CODE. Every mechanism runs
        // identically; only the structure the world contains is destroyed. If
        // accuracy survived this it was never composition. Measured at 12
        // seeds: 0.0534 +- 0.0116, which is BELOW chance, and most questions get
        // no answer at all. The two arms are 28.2 sigma apart.
        var scrambled = await Accuracy(stamina: 8.0, scrambled: true);
        var real = await Accuracy(stamina: 8.0, scrambled: false);

        Assert.True(scrambled.Mean < 0.1, $"the control still scored {scrambled}");

        // AND THE TWO ARMS ARE GENUINELY APART, which a pair of bare means
        // cannot say. Anything under about three sigma is a difference this
        // project has retracted before.
        Assert.True(real.Separation(scrambled) > 3.0,
            $"{real} against {scrambled} is only {real.Separation(scrambled):F1} sigma");
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

        Assert.Equal(0.0, shallow.Mean);
        Assert.True(deep.Mean > shallow.Mean + 0.5, $"deep {deep} against shallow {shallow}");
    }

    // ---- the run says what it did ------------------------------------------

    [Fact]
    public async Task Every_run_reports_its_own_plumbing_and_has_nothing_to_complain_about()
    {
        // JOHN'S ASK, AND IT IS THE SAME CHECK SNAKE ALREADY HAD. A number gets
        // swept, barely moves, and much later it turns out something was never
        // connected. This reads the complaints on a real run so that cannot sit
        // undetected behind a plausible-looking accuracy.
        using var run = new SensesRun(new SensesSettings
        {
            Concepts = 12, CodesPerSense = 3, Noise = 0.1,
        }, Dials(8.0), seed: 1);

        var result = await run.RunAsync(400, every: 10);

        // EVERY COMPLAINT, WITH NOTHING EXEMPTED. This used to allow fork 22 by
        // name -- 7 of 39 questions never settled -- and fork 22 is closed: a
        // transiently-zero live count was untracking thoughts while reports were
        // still in flight, and every report after that was dropped. See
        // `InputMachine.Retire`.
        Assert.Empty(result.Complaints);

        // ASSERTED AT ZERO RATHER THAN BOUNDED, which is what closing it means.
        // While this was merely bounded, every silent count in the project was an
        // upper bound, because "nothing reached" and "not finished yet" are
        // indistinguishable in a score.
        Assert.Equal(0, result.Unsettled);

        // The companion: the report is not empty of everything. A complaints
        // list that is empty because nothing was ever measured would pass the
        // assertion above and mean the opposite of what it looks like.
        Assert.True(result.Deepest >= 3, $"deepest chain {result.Deepest}");
        Assert.True(result.Messages > 0);
        Assert.True(result.Nodes > 0);
    }

    [Fact]
    public async Task A_run_that_never_composed_anything_says_so()
    {
        // The companion to the companion. At a stamina that cannot afford two
        // hops the complaint MUST fire -- otherwise the check above is passing
        // for a report that can never fail.
        using var run = new SensesRun(new SensesSettings
        {
            Concepts = 12, CodesPerSense = 3, Noise = 0.1,
        }, Dials(2.0), seed: 1);

        var result = await run.RunAsync(200, every: 10);

        Assert.Contains(result.Complaints, one => one.Contains("composed"));
    }

    [Fact]
    public async Task A_dial_that_is_on_and_does_nothing_is_reported()
    {
        // FORK 21'S WIRING CHECK, and it is the exact failure mode this project
        // has hit before: a parameter declared, documented, passed at every call
        // site, and connected to nothing. Reflection on with a threshold nothing
        // can reach writes nothing, and the run has to say so out loud.
        using var run = new SensesRun(new SensesSettings
        {
            Concepts = 12, CodesPerSense = 3, Noise = 0.1,
        }, Dials(8.0) with
        {
            Reflect = new Reflection { Threshold = 1e9, Weight = 0.5, Names = 3 },
        }, seed: 1);

        var result = await run.RunAsync(200, every: 10);

        Assert.Equal(0, result.Reflected);
        Assert.Contains(result.Complaints, one => one.Contains("wrote nothing"));
    }
}
