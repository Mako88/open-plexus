using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Step 11 — <b>feeding a prediction back in and asking again, which is the
/// difference between a world model and an expensive reflex.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>`Consequence` ASKS <i>what would the world look like if I did X</i>, ONE
/// STEP, AND STOPS.</b> One step is a reflex however good it is; planning needs the
/// answer fed back as though it had happened. Craik, Tolman, MuZero's search half —
/// and the plan was right that it needs nothing built, only wiring.
/// </para>
/// <para>
/// <b>NOTHING IS OBSERVED ON THE WAY.</b> A synthetic moment must never reach the
/// rendezvous or the graph learns from its own guesses, and every count downstream
/// is then measuring the model's imagination. Thinking without observing is what
/// <c>ThinkAsync</c> already is, which is why this cost no new mechanism.
/// </para>
/// <para>
/// <b>`Rhythm` IS THE RIGHT WORLD BECAUSE ITS CEILING DOES NOT MOVE WITH DEPTH.</b>
/// A cycle is exactly as predictable four steps out as one — the symbol is
/// determined either way — so any decay is COMPOUNDING ERROR and nothing else. On a
/// world whose far future were genuinely harder the two would be inseparable.
/// </para>
/// </remarks>
public sealed class RolloutTests(ITestOutputHelper output)
{
    private static RhythmSettings World() =>
        new() { Symbols = 12, Period = 5, Violations = 0.1 };

    private static readonly int[] Seeds = [1, 2, 3, 5, 8, 13];

    private static async Task<(double Rolled, double Direct, double Messages)> AtAsync(int depth)
    {
        double rolled = 0.0, direct = 0.0, messages = 0.0;

        foreach (var seed in Seeds)
        {
            using var run = new RhythmRun(
                World(), Fixture.Dials(stamina: 3.0), seed, span: 1, depth: depth);

            var result = await run.RunAsync(600);

            rolled += result.Rolled;
            direct += result.Accuracy;
            messages += result.Messages;
        }

        return (rolled / Seeds.Length, direct / Seeds.Length, messages / Seeds.Length);
    }

    [Fact]
    public async Task At_one_step_the_rollout_is_the_reflex_it_extends()
    {
        // THE CHECK THAT THE WIRING DID NOT DISTURB WHAT IT EXTENDS. At depth one
        // the queue holds a single bet and settles it against the very next moment,
        // which is precisely what the ordinary score already does -- by a different
        // route, through different code. The two must agree.
        var one = await AtAsync(depth: 1);

        output.WriteLine($"depth 1  rolled={one.Rolled:F4} direct={one.Direct:F4}");

        Assert.Equal(one.Direct, one.Rolled, precision: 3);
    }

    [Fact]
    public async Task It_rolls_forward_and_the_error_barely_compounds()
    {
        // THE RISK THE PLAN NAMED IS REAL AND SMALL HERE, and the reason is worth
        // more than the number. Four steps out costs about three points of accuracy
        // where a compounding error should have been ruinous -- and depth two comes
        // out marginally ABOVE depth one, which is noise but is certainly not decay.
        //
        // A CYCLE IS AN ATTRACTOR, WHICH IS WHY. Every symbol in it predicts its own
        // successor, so a rollout that guesses wrong often lands on another cycle
        // member and is back on the rails at the next step. There is nowhere else to
        // go. A WORLD WHOSE DYNAMICS BRANCH WOULD PUNISH THIS FAR HARDER, and the
        // mild decay here should not be read as a general property of the rollout.
        //
        // The ceiling is flat across depth on this world, so whatever fall there is
        // IS compounding error and not the far future being harder.
        var depths = new List<(int Depth, double Rolled, double Messages)>();

        foreach (var depth in (int[])[1, 2, 3, 4])
        {
            var (rolled, _, messages) = await AtAsync(depth);
            depths.Add((depth, rolled, messages));

            output.WriteLine($"depth {depth}  rolled={rolled:F4} msgs={messages:F0}");
        }

        var chance = 1.0 / 12.0;

        // IT DECAYS, which is the finding rather than a disappointment -- a rollout
        // that held its accuracy exactly would more likely mean the extra steps were
        // not being taken at all.
        Assert.True(depths[^1].Rolled < depths[0].Rolled,
            $"rolling four steps out is as accurate as one ({depths[^1].Rolled:F4} "
            + $"against {depths[0].Rolled:F4}), so either the rollout is not "
            + "reaching or this world's far future is free");

        // AND IT IS STILL A MODEL RATHER THAN NOISE AT TWO STEPS, which is the
        // claim worth having: the thing fed back in was good enough to ask a
        // second question of.
        Assert.True(depths[1].Rolled > chance * 2.0,
            $"two steps out is at chance ({depths[1].Rolled:F4} against "
            + $"{chance:F4}), so nothing survives being fed back even once");

        // AND EVERY STEP COSTS A WHOLE WALK -- traffic rises by about half again
        // from one step to two and doubles by four. THAT is what makes depth want
        // its own control rather than a bigger number, and on this world it is the
        // cost rather than the compounding that argues against going deep.
        Assert.True(depths[^1].Messages > depths[0].Messages,
            "rolling further stopped costing anything, so the extra walks are not "
            + "happening");
    }
}
