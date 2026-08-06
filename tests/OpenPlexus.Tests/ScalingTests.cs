using OpenPlexus.Commitments;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What it costs to learn a wider world, which is the number that predicts whether
/// any of this reaches perception.
/// </summary>
/// <remarks>
/// <b>A FINAL ACCURACY SAYS HOW WELL A WIDTH WAS LEARNT AND NOT WHAT IT COST.</b> Six
/// bits, eleven and twenty differ in the number of relevant bits by one each time, so
/// how rounds-to-target grows across them is the exponent — and an exponent found
/// cheaply on a generated world is worth far more than the same discovery made
/// expensively on a real one.
/// </remarks>
public sealed class ScalingTests(ITestOutputHelper output)
{
    [Fact]
    public void The_cost_of_a_wider_world_is_reported_and_carries_no_bar()
    {
        // NO BAR, DELIBERATELY. A threshold here would invite tuning against the one
        // measurement whose whole value is being an honest read of the trend.
        var seen = new List<(int Address, Learned Learned)>();

        foreach (var address in new[] { 2, 3, 4 })
        {
            var learned = new MultiplexerRun(
                new MultiplexerSettings { Address = address },
                new CommittingSettings(),
                seed: 1).Run(60000);

            seen.Add((address, learned));

            output.WriteLine(
                $"address={address} bits={address + (1 << address)} "
                + $"reached={learned.Reached} recent={learned.Recent:F3} "
                + $"resident={learned.Resident} sound={learned.Sound} "
                + $"unsound={learned.Unsound} unchecked={learned.Unchecked} "
                + $"named={learned.Named} exhausted={learned.Exhausted}");
        }

        // WHAT IS ASSERTED IS THAT THE CURVE EXISTS TO BE READ, not where it goes.
        // A width that never reaches the target reports zero, and that is a reading
        // rather than a gap.
        Assert.Equal(3, seen.Count);
        Assert.All(seen, one => Assert.True(one.Learned.Rounds == 60000));

        // AND THE NARROWEST WORLD HAS TO GET THERE, or the measurement is of a
        // learner that does not work rather than of how cost grows.
        Assert.True(seen[0].Learned.Reached > 0, "six bits never held the target");
    }

    [Fact]
    public void What_cannot_be_settled_is_reported_and_not_counted_as_wrong()
    {
        // A ONE-CODE SCOPE IN A TWENTY-BIT WORLD LEAVES NINETEEN FREE, so folding
        // the uncheckable into the unsound would make the share of true rules fall
        // with the width of the world for a reason that has nothing to do with
        // learning -- a scaling result manufactured by its own statistic.
        var wide = new MultiplexerRun(
            new MultiplexerSettings { Address = 4 },
            new CommittingSettings(),
            seed: 1).Run(20000);

        output.WriteLine(
            $"sound={wide.Sound} unsound={wide.Unsound} unchecked={wide.Unchecked}");

        Assert.True(wide.Unchecked > 0, "nothing was too general to settle at twenty bits");

        // AND THE THREE ACCOUNT FOR EVERY EXPERIENCED COMMITMENT between them, so
        // none of them is quietly dropping a case.
        var narrow = new MultiplexerRun(
            new MultiplexerSettings { Address = 2 },
            new CommittingSettings(),
            seed: 1).Run(20000);

        Assert.Equal(0, narrow.Unchecked);
    }

    [Fact]
    public void Rounds_to_target_reads_a_trailing_window_and_not_a_running_total()
    {
        // A LIFETIME ACCURACY CANNOT CROSS A BAR IT SPENT THE EARLY ROUNDS BELOW, so
        // rounds-to-target read off a total would measure the length of the run. The
        // check is that a longer run reports the SAME crossing, which only a trailing
        // window can do.
        static Learned Run(long rounds) => new MultiplexerRun(
            new MultiplexerSettings { Address = 2 },
            new CommittingSettings(),
            seed: 3).Run(rounds);

        var shorter = Run(30000);
        var longer = Run(60000);

        output.WriteLine($"reached at {shorter.Reached} and {longer.Reached}");

        Assert.True(shorter.Reached > 0, "never held the target in thirty thousand rounds");
        Assert.Equal(shorter.Reached, longer.Reached);
    }
}
