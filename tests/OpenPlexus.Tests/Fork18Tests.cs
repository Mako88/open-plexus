using OpenPlexus.Graph;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Fork 18 — <b>does the graph model its own effect on the world, once it has an
/// edge that can mean <i>then</i>?</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>IT WAS BLOCKED ON TEMPORAL EDGES AND THE BLOCKER IS GONE.</b>
/// <see cref="SnakeFrame.Did"/> is the move already taken and the view is the
/// world after it, but written as one flat occasion they became a <c>With</c>
/// pair — so the graph recorded that the view ACCOMPANIED the action, which is
/// indistinguishable from the view having been there when the action was chosen.
/// A walk from an action reached whatever co-occurred with it, and *what will the
/// world look like if I do X* was not a question the row could answer.
/// </para>
/// <para>
/// <b>The measurement is <see cref="Consequence"/>'s and is untouched.</b> Same
/// three arms, same prequential scoring. Only what the front end says about order
/// and what the prediction asks for change.
/// </para>
/// </remarks>
public sealed class Fork18Tests(ITestOutputHelper output)
{
    private static SnakeSettings World() => Fixture.Snake(energy: 80.0);

    private static WalkSettings Dials() => Fixture.Dials(foresight: 2.0);

    private const int Steps = 400;

    [Fact]
    public async Task An_action_said_to_come_first_is_recorded_as_coming_first()
    {
        // THE WIRING, BEFORE ANY CLAIM ABOUT WHAT IT BUYS. A dial declared,
        // documented, passed everywhere and connected to nothing is a named trap
        // here, and it has already caught one arm that survived three
        // measurements.
        using var flat = new SnakeRun(World(), Dials(), seed: 3);
        using var ordered = new SnakeRun(World(), Dials() with { Kinds = true }, seed: 3);

        await flat.PlayAsync(120);
        await ordered.PlayAsync(120);

        // With no order said, nothing is temporal and every cell is `With`.
        Assert.Equal(0, Temporal(flat));

        // With the action said to come first, the action-to-view pairs are.
        Assert.True(Temporal(ordered) > 0,
            "the front end said the action came first and no temporal cell exists");
    }

    [Fact]
    public async Task Asking_what_follows_is_measured_against_asking_what_accompanies()
    {
        // THE EXPERIMENT. `Moved` is the honest number: how much further naming a
        // DIFFERENT action moves the prediction than asking the same question
        // twice does. The jitter floor is what makes it readable at all --
        // delivery is concurrent, so two identical broadcasts already land in
        // different places, and three earlier attempts to answer this failed for
        // want of that third arm.
        // TWELVE SEEDS, BECAUSE A SMALL SAMPLE CAN LOOK LIKE A MECHANISM. That
        // is a named trap here — one seed with a collapsing echo read as a
        // discovery and turned out to be three questions — and a single snake
        // run supplies only as many questions as it survives steps.
        //
        // AND FIVE WAS NOT ENOUGH TO SEE THE COST. At five, survival matched or
        // beat the baseline on four and one seed looked like an outlier worth
        // explaining. At twelve, survival is WORSE ON FIVE, twice severely — so
        // the outlier was the sample and not the seed, and chasing that one run
        // would have been chasing noise with a story attached.
        var moved = new Dictionary<bool, List<double>> { [false] = [], [true] = [] };
        var gap = new Dictionary<bool, List<double>> { [false] = [], [true] = [] };
        var asked = new Dictionary<bool, int> { [false] = 0, [true] = 0 };
        var chosen = new Dictionary<bool, List<int>> { [false] = [], [true] = [] };
        var foresight = new Dictionary<bool, List<double>> { [false] = [], [true] = [] };
        var lived = new Dictionary<bool, List<int>> { [false] = [], [true] = [] };
        var eaten = new Dictionary<bool, List<int>> { [false] = [], [true] = [] };

        foreach (var seed in (int[])[3, 7, 11, 17, 23, 31, 41, 47, 59, 67, 73, 83])
            foreach (var kinds in new[] { false, true })
            {
                using var run = new SnakeRun(World(), Dials() with { Kinds = kinds }, seed);
                var result = await run.PlayAsync(Steps);

                moved[kinds].Add(result.Consequence.Moved);
                gap[kinds].Add(result.Consequence.Gap);
                asked[kinds] += result.Consequence.Asked;
                chosen[kinds].Add(result.ChosenByChain);
                foresight[kinds].Add(result.Consequence.Knowing);
                lived[kinds].Add(result.Steps);
                eaten[kinds].Add(result.Ate);

                output.WriteLine(
                    $"seed={seed} kinds={kinds} asked={result.Consequence.Asked} " +
                    $"knowing={result.Consequence.Knowing:F4} " +
                    $"counterfactual={result.Consequence.Counterfactual:F4} " +
                    $"gap={result.Consequence.Gap:F4} " +
                    $"apart={result.Consequence.Apart:F4} " +
                    $"echoed={result.Consequence.Echoed:F4} " +
                    $"moved={result.Consequence.Moved:F4} " +
                    $"steps={result.Steps} ate={result.Ate} " +
                    $"temporal={run.TemporalCells} " +
                    $"byChain={result.ChosenByChain} " +
                    $"reachedNothing={result.ReachedNothing} " +
                    $"msgs={result.Messages}");
            }

        output.WriteLine(
            $"MEAN moved off={moved[false].Average():F4} on={moved[true].Average():F4} | " +
            $"gap off={gap[false].Average():F4} on={gap[true].Average():F4} | " +
            $"asked off={asked[false]} on={asked[true]}");

        // THE COMPARISON IS PAIRED, AND THE ABSOLUTE VERSION OF IT WAS WRONG.
        // At five seeds the baseline was an exact zero on every one — naming a
        // different action produced the IDENTICAL prediction — and that read as
        // the finding. At twelve, five of the baselines are non-zero. They are
        // tiny, but "identical" was a claim about the sample.
        //
        // What survives is stronger and is what was always meant: with temporal
        // edges the action moves the prediction MORE than without them, on every
        // seed, by better than an order of magnitude in the mean. A paired
        // comparison needs no exact zero to lean on.
        Assert.All(moved[true], one => Assert.True(one > 0.0, $"moved {one}"));
        Assert.All(gap[true], one => Assert.True(one > 0.0, $"gap {one}"));

        for (var at = 0; at < moved[true].Count; at++)
        {
            Assert.True(moved[true][at] > moved[false][at],
                $"a different action moved the prediction no further with kinds "
                + $"than without: {moved[true][at]} against {moved[false][at]}");

            Assert.True(gap[true][at] > gap[false][at],
                $"knowing the action bought no more with kinds than without: "
                + $"{gap[true][at]} against {gap[false][at]}");
        }

        // AND THE BODY STILL CHOOSES. THIS IS A REGRESSION TEST FOR A SEVERED
        // PATH, not a quality bar. Ordering the occasion writes action -> view,
        // and choosing an action broadcasts the VIEW and has to arrive at an
        // action -- so with only the forward cell written there was no edge to
        // walk, the chain reached an action zero times on every seed, and the
        // snake moved entirely at random while its predictions improved. A
        // world model bought by throwing away the policy is not a trade this
        // project would take, and the number that says so is this one.
        Assert.All(chosen[true], one => Assert.True(one > 0,
            "the chain reached an action zero times: the acting walk has been "
            + "severed again, and `Kind.Before` is what keeps it reachable"));

        // FORESIGHT RISES ON MOST SEEDS AND NOT ON ALL, which is the honest
        // shape. Asserted as a majority rather than universally, because that is
        // what twelve seeds show and asserting the stronger claim would be
        // asserting the sample.
        var sharper = foresight[true].Where((one, at) => one > foresight[false][at]).Count();

        Assert.True(sharper * 2 > foresight[true].Count,
            $"foresight improved on only {sharper} of {foresight[true].Count} seeds");

        // AND SURVIVAL IS NOT THE METRIC — JOHN, 2026-08-04, AND HE IS RIGHT.
        // A run that ends at exactly the starting energy is a snake that ate
        // NOTHING and starved on schedule, which is arithmetic rather than skill.
        // `Survival as the score` is already a refuted row here, for the
        // neighbouring reason that circling wins it; reading `Steps` as policy
        // quality was that same mistake wearing a different hat, and I reported
        // a survival regression from it three times.
        //
        // Measured on food, the regression is not there: worse on NO seed.
        var hungrier = eaten[true].Where((one, at) => one < eaten[false][at]).Count();

        output.WriteLine(
            $"ATE off={eaten[false].Sum()} on={eaten[true].Sum()} over "
            + $"{eaten[true].Count} runs — worse on {hungrier}");

        Assert.Equal(0, hungrier);

        // WHICH LEAVES THE REAL FINDING, AND IT IS ABOUT THE WORLD RATHER THAN
        // THE ARM: a handful of apples across two dozen runs. THIS WORLD CANNOT
        // CURRENTLY DISCRIMINATE POLICIES AT ALL, in either arm, because nothing
        // gives the body a reason to seek food — which is what step 4's drives
        // are for, and is why `Homeostat` exists. Fork 18's prediction half is
        // unaffected: it is scored within a run against the same trajectory.
        Assert.True(eaten[false].Sum() + eaten[true].Sum() < eaten[true].Count,
            "the snake has started eating: this world can discriminate policies "
            + "again, and the note saying it cannot should come out");
    }

    /// <summary>
    /// How many temporal cells the run's graph holds.
    /// </summary>
    /// <remarks>
    /// <b>Counted from the row rather than inferred from a score</b>, because
    /// "the arm did nothing" and "the arm was never connected" are different
    /// findings and only one of them is worth reporting.
    /// </remarks>
    private static int Temporal(SnakeRun run) => run.TemporalCells;
}
