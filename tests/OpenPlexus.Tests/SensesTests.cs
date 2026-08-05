using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

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
public sealed class SensesTests(ITestOutputHelper output)
{
    private static SensesSettings Clean(int concepts = 8, int codes = 3) =>
        Fixture.Senses(concepts, codes);

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

    private static WalkSettings Dials(double stamina) => Fixture.Dials(stamina);

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

        // AND IT IS REPORTED WHETHER OR NOT IT PASSES. The bar below is chance,
        // and the project's headline claim is 0.80 -- so this test went on passing
        // while chunking took the score to 0.4138, because 0.41 clears chance by
        // three standard errors just as comfortably as 0.81 does. A number nobody
        // can read is a number that can halve without failing anything.
        output.WriteLine($"{real} against chance {1.0 / 12:F4}");

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

    // ---- C2, checked rather than assumed ------------------------------------

    [Fact]
    public async Task Lateness_is_survived_rather_than_assumed_to_be_survivable()
    {
        // C2 IS STILL NOT TESTED, AND THIS IS WHY -- which is worth more than the
        // green tick it produces.
        //
        // Lateness is injected and absorbed COMPLETELY: accuracy is identical to
        // four places on every seed, on a world scoring 0.6552 against a chance of
        // 0.0833, with 304 of 11,925 deliveries held back fifty milliseconds each.
        //
        // THE REASON IS THE HARNESS, NOT THE DESIGN. A held-back delivery is
        // delayed INSIDE the in-flight count, so `WhenIdle` does not fire while it
        // is waiting -- and every reader here waits for quiet before it reads. A
        // late message therefore turns into the harness waiting longer, never into
        // a message that arrives after somebody has acted. No amount of delay
        // under `Fabric`'s thirty-second patience can escape that.
        //
        // SO WHAT THIS ESTABLISHES IS NARROW: the bus, the accounting and the walk
        // are unharmed by deliveries arriving out of order and far apart. What it
        // does NOT establish is the thing C2 actually claims -- that the design
        // survives acting on what has arrived so far while something is still in
        // flight. That needs a reader on a DEADLINE rather than one waiting for
        // quiet, and every reader in this project waits for quiet.
        //
        // The original note stands: every measurement here runs in one process
        // with in-memory delivery.
        //
        // Thread-pool dispatch already reorders. What it never produces is a
        // message arriving LONG after its siblings, which is what a real network
        // adds and the case that can outlive a thought's patience. So a few
        // percent of deliveries are held back by far longer than a walk takes.
        //
        // THIS WORLD, AND CHOOSING IT WAS THE WHOLE DIFFICULTY. The binding world
        // is the usual scoreboard and CANNOT SEE THIS from either end: its unbound
        // arm scores the world's coin rather than the system, so both arms return
        // an identical number whatever the bus does -- measured, 0.5577 against
        // 0.5577 to four places -- and its bound-and-segmented arm sits at exactly
        // 1.0000 with every answer an echo. A world that is at chance or at its
        // ceiling can absorb any amount of damage without moving.
        //
        // Sight and touch NEVER CO-OCCUR here, so the answer cannot be looked up
        // and can only be composed across hops. That is precisely what lateness
        // disturbs, and it scores well clear of chance without saturating.
        var world = new SensesSettings { Concepts = 12, CodesPerSense = 3, Noise = 0.1 };
        var late = new Bus.Lateness(Share: 0.02, Delay: TimeSpan.FromMilliseconds(50), Seed: 7);

        var arms = await Sweep.AcrossAsync(
            4,
            ("on time", async seed =>
            {
                using var run = new SensesRun(world, Dials(6.0), seed);
                return (await run.RunAsync(300, every: 10)).Accuracy;
            }),
            ("late", async seed =>
            {
                using var run = new SensesRun(world, Dials(6.0), seed, late: late);
                return (await run.RunAsync(300, every: 10)).Accuracy;
            }));

        using var jittered = new SensesRun(world, Dials(6.0), seed: 1, late: late);
        var result = await jittered.RunAsync(300, every: 10);

        output.WriteLine(Sweep.Table(arms));
        output.WriteLine(result.ToString());
        output.WriteLine(
            $"held back {jittered.Delayed} of {result.Messages} deliveries, "
            + $"{arms[1].Separation(arms[0]):F1} sigma apart");

        // THE ARM ACTUALLY DID SOMETHING. A jitter arm that delayed nothing is a
        // control wearing the arm's name, which is the failure this project keeps
        // having -- so this is asserted before anything is read from the table.
        Assert.True(jittered.Delayed > 0,
            "no delivery was ever held back, so this measured the control twice");

        // AND THE WORLD CAN STILL SEE A DIFFERENCE, or "unchanged" would be a
        // claim about a measurement that cannot move. Both guards are here because
        // the binding world failed each of them in turn.
        Assert.True(
            arms[0].Mean > 2.0 * SensesChance && arms[0].Mean < 0.95,
            $"the on-time arm is at chance or at its ceiling ({arms[0].Mean:F4}), "
            + "so it could not detect a degradation either");

        // THE ACCOUNTING CLOSES UNDER LATENESS. `Balanced` is built from a
        // quantity entirely separate from the live count -- the routing named in
        // each report -- so the two agreeing says the distributed bookkeeping is
        // not disturbed by deliveries arriving far out of order.
        Assert.Equal(0, result.Unbalanced);
        Assert.Equal(0, result.Unsettled);

        // AND THE ABSORPTION IS ASSERTED AS ABSORPTION. If this ever starts
        // failing, lateness has begun to REACH the score -- which would mean a
        // reader stopped waiting for quiet, and the note at the top of this test
        // needs rewriting rather than repeating.
        Assert.True(arms[1].Separation(arms[0]) < 1.0,
            $"lateness now moves the score ({arms[1].Mean:F4} against "
            + $"{arms[0].Mean:F4}, {arms[1].Separation(arms[0]):F1} sigma), so it "
            + "is no longer being absorbed by the harness waiting for it");
    }

    /// <summary>What a blind guess is worth on the configuration above.</summary>
    private const double SensesChance = 1.0 / 12.0;
}
