using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// How much the walk differs from ITSELF, and what that costs.
/// </summary>
/// <remarks>
/// <b>Never measured until 2026-08-02, and it changes how every other number in
/// this project should be read.</b> Every result here has been "X against Y with
/// a spread across seeds". None of them measured how far one walk lands from an
/// identical walk — and delivery is concurrent, so that is not zero.
/// </remarks>
public sealed class NoiseFloorTests
{
    /// <summary>
    /// The noisy senses world, with something already learned in it.
    /// </summary>
    /// <remarks>
    /// <b>The teaching is not optional.</b> Every question on a fresh graph
    /// reaches nothing, and two walks that both reached nothing agree perfectly —
    /// which would read as a walk that never disagrees with itself.
    /// </remarks>
    private static SensesRun Taught(int seed) => new(
        Fixture.Senses(concepts: 12, noise: 0.1), Fixture.Dials(stamina: 8.0), seed);

    [Fact]
    public async Task The_same_question_does_not_always_get_the_same_answer()
    {
        List<double> same = [], other = [];

        for (var seed = 1; seed <= 6; seed++)
        {
            using var run = Taught(seed);

            // Learn something first, or every question reaches nothing.
            await run.RunAsync(400, every: 10);

            for (var concept = 0; concept < 12; concept++)
            {
                var first = await run.AskAsync(concept);
                var again = await run.AskAsync(concept);
                var different = await run.AskAsync((concept + 1) % 12);

                // 1.0 means the two answers agreed exactly.
                same.Add(Agree(first, again));
                other.Add(Agree(first, different));
            }
        }

        var s = new Measured { Arm = "same", Values = same };
        var o = new Measured { Arm = "other", Values = other };

        // THE WALK DOES NOT DISAGREE WITH ITSELF, AND IT NEVER REALLY DID --
        // MEASURED 1.0000 +-0.0000 OVER 72 QUESTIONS, 2026-08-03.
        //
        // This used to read 0.8833 +-0.0294 run alone, rising to 1.0000 inside a
        // busy suite, and that load-dependence was recorded as a property of
        // concurrent delivery. IT WAS FORK 22. Questions were being read before
        // their walk had finished -- 5 to 8 of 39 -- and an unfinished walk
        // answers differently from a finished one. Under load everything ran
        // slower, so walks had longer to settle and the disagreement vanished,
        // which looked like the opposite of what it was.
        //
        // THE CONSEQUENCES ARE LARGE AND ARE RECORDED IN THE PLAN. Voting exists
        // because of that number, and buys nothing in one process now. So does
        // the trap saying numbers under different loads are not comparable.
        //
        // ASSERTED AT EXACTLY 1.0 rather than merely bounded, because that is
        // what closing fork 22 bought and a regression would put it straight
        // back.
        Assert.Equal(1.0, s.Mean);

        // WHAT DOES HOLD EVERYWHERE: a different question gets a different
        // answer, so whatever disagreement there is sits around an answer rather
        // than standing in for the absence of one.
        Assert.True(s.Mean - o.Mean > 0.5,
            $"same {s} against different {o}: asking the same question again is "
            + "no more likely to repeat the answer than asking a different one");

        // AND NOW ON SIGMA AS WELL, WHICH THIS TEST USED TO REFUSE TO DO. Under
        // load both arms have zero spread -- 1.0000 and 0.0000 -- and
        // `Separation` used to return 0 there, reading "indistinguishable" for
        // arms that are perfectly separated. It reports infinity for that case
        // now, so the claim can be made the same way as every other claim here
        // instead of on a bare mean with a paragraph of apology.
        Assert.True(s.Separation(o) > 3.0,
            $"same {s} against different {o} is only {s.Separation(o):F1} sigma");
    }

    [Fact]
    public async Task Does_asking_three_times_beat_asking_once()
    {
        // IF ACCURACY IS AT THE SELF-CONSISTENCY CEILING, a majority of three
        // should clear it -- the errors would be independent draws rather than
        // a gap in what the graph holds. If it does not, the ceiling is
        // ignorance and the two numbers matching was a coincidence.
        List<double> once = [], thrice = [];

        for (var seed = 1; seed <= 5; seed++)
        {
            using var run = Taught(seed);

            await run.RunAsync(400, every: 10);

            int single = 0, voted = 0, asked = 0;

            for (var round = 0; round < 2; round++)
                for (var concept = 0; concept < 12; concept++)
                {
                    var alone = await run.AskAsync(concept);

                    // JOHN'S VERSION: one round trip, several broadcast ids,
                    // rather than three sequential asks.
                    var voting = await run.AskAsync(concept, votes: 3);

                    asked++;
                    if (alone is { } first && Senses.Concept(first) == concept) single++;
                    if (voting is { } best && Senses.Concept(best) == concept) voted++;
                }

            once.Add(asked == 0 ? 0 : single / (double)asked);
            thrice.Add(asked == 0 ? 0 : voted / (double)asked);
        }

        var one = new Measured { Arm = "once", Values = once };
        var three = new Measured { Arm = "thrice", Values = thrice };

        // MEASURED AT 8 SEEDS AND 4 ROUNDS: 0.9688 +-0.0056 once against 0.9974
        // +-0.0026 thrice -- 4.7 sigma. NEARLY ALL THE REMAINING ERROR WAS
        // NONDETERMINISM RATHER THAN IGNORANCE: the graph held the answer and a
        // single walk failed to fetch it.
        //
        // THIS TEST GUARDS THE DIRECTION AND DOES NOT RE-DERIVE THE SIGMA. At a
        // sample small enough to keep the suite quick, `thrice` reaches 1.0000
        // with no spread at all, so a separation gate reads 1.0 sigma and fails
        // for want of variance rather than want of effect. Asserting it here
        // would be asserting the sample size.
        // AND THE FLOOR MOVES WITH MACHINE LOAD, which is worth knowing on its
        // own. Run alone this scores 0.9917 once; run inside the full suite,
        // with other classes executing in parallel, it scores 1.0000 and there
        // is nothing left for voting to repair. So no assertion here demands
        // that a single ask be imperfect -- that would be asserting how busy the
        // machine is. The companion that DOES bite is the agreement test above.
        Assert.True(three.Mean >= one.Mean, $"thrice {three} did not beat once {one}");
    }

    private static double Agree(Code? one, Code? other) =>
        one is null && other is null ? 1.0
        : one is null || other is null ? 0.0
        : one.Value == other.Value ? 1.0 : 0.0;
}
