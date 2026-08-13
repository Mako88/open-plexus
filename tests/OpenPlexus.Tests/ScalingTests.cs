using OpenPlexus.Machines;
using OpenPlexus.Commitments;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What it costs to learn a wider world, which is the number that predicts whether
/// any of this reaches perception.
/// </summary>
/// <remarks>
/// <b>A final accuracy says how well a width was learnt and not what it cost.</b> Six
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
                new Brain(new CommittingSettings(), 1),
                seed: 1).Run(60000);

            seen.Add((address, learned));

            output.WriteLine(
                $"address={address} bits={address + (1 << address)} "
                + $"reached={learned.Reached} recent={learned.Recent:F3} "
                + $"resident={learned.Resident} sound={learned.Sound} "
                + $"unsound={learned.Unsound} unchecked={learned.Unchecked} "
                + $"named={learned.Named}/{learned.Eligible} "
                + $"spoke={learned.Speaking:F2} exhausted={learned.Exhausted}");
        }

        // What is asserted is that the curve exists to be read, not where it goes.
        // A width that never reaches the target reports zero, and that is a reading
        // rather than a gap.
        Assert.Equal(3, seen.Count);
        Assert.All(seen, one => Assert.True(one.Learned.Rounds == 60000));

        // And the narrowest world has to get there, or the measurement is of a
        // learner that does not work rather than of how cost grows.
        Assert.True(seen[0].Learned.Reached > 0, "six bits never held the target");
    }

    [Fact]
    public void What_cannot_be_settled_is_reported_and_not_counted_as_wrong()
    {
        // A one-code scope in a twenty-bit world leaves nineteen free, so folding
        // the uncheckable into the unsound would make the share of true rules fall
        // with the width of the world for a reason that has nothing to do with
        // learning -- a scaling result manufactured by its own statistic.
        var wide = new MultiplexerRun(
            new MultiplexerSettings { Address = 4 },
            new Brain(new CommittingSettings(), 1),
            seed: 1).Run(20000);

        output.WriteLine(
            $"sound={wide.Sound} unsound={wide.Unsound} unchecked={wide.Unchecked}");

        Assert.True(wide.Unchecked > 0, "nothing was too general to settle at twenty bits");

        // And the three account for every experienced commitment between them, so
        // none of them is quietly dropping a case.
        var narrow = new MultiplexerRun(
            new MultiplexerSettings { Address = 2 },
            new Brain(new CommittingSettings(), 1),
            seed: 1).Run(20000);

        Assert.Equal(0, narrow.Unchecked);
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_rounds_to_target_costs_as_the_relevant_bits_grow()
    {
        // The number this project says predicts whether any of it reaches perception, and it
        // has only ever been read on one seed. The check above runs three widths at seed one
        // and asserts the curve exists to be read; what it cannot do is say whether the
        // ordering between two widths is a fact or a draw. This repo's own trap: one seed is
        // not a comparison and will happily invert.
        //
        // And rounds-to-target is a censored measurement, which is the part a mean gets
        // wrong. A seed that never holds the target inside the cap reports nought, and
        // averaging that in reads as a width that learnt FAST. Averaging it out reads as a
        // width that learnt fast too, because the slow seeds are exactly the ones dropped. So
        // the count that reached is printed beside the mean and the mean says it is
        // conditional on reaching -- neither number means anything without the other.
        const long Cap = 100_000;
        const int Seeds = 8;

        output.WriteLine($"{Seeds} seeds, cap {Cap} rounds, target 0.9 on a trailing window");
        output.WriteLine(" bits | reached | rounds to target (of those) | recent | sound");

        foreach (var address in new[] { 2, 3, 4 })
        {
            var reached = new List<double>();
            var recent = new List<double>();
            var sound = new List<double>();

            for (var seed = 1; seed <= Seeds; seed++)
            {
                var learned = new MultiplexerRun(
                    new MultiplexerSettings { Address = address },
                    new Brain(new CommittingSettings(), seed),
                    seed).Run(Cap);

                if (learned.Reached > 0) reached.Add(learned.Reached);

                recent.Add(learned.Recent);
                sound.Add(learned.Sound);
            }

            output.WriteLine(
                $"{address + (1 << address),5} | {reached.Count,3}/{Seeds} | "
                + $"{(reached.Count == 0 ? "none reached" : Sweep.Spread(reached, "F0")),27} "
                + $"| {recent.Average(),6:F3} | {sound.Average(),5:F1}");
        }

        // NO BAR. What the exponent should be has never been measured, and a threshold
        // written before the first reading with error bars would be the answer rather than
        // the finding. A width where no seed reaches is a reading and not a gap.
    }

    [Fact]
    public void Rounds_to_target_reads_a_trailing_window_and_not_a_running_total()
    {
        // A lifetime accuracy cannot cross a bar it spent the early rounds below, so
        // rounds-to-target read off a total would measure the length of the run. The
        // check is that a longer run reports the SAME crossing, which only a trailing
        // window can do.
        static Learned Run(long rounds) => new MultiplexerRun(
            new MultiplexerSettings { Address = 2 },
            new Brain(new CommittingSettings(), 1),
            seed: 3).Run(rounds);

        var shorter = Run(30000);
        var longer = Run(60000);

        output.WriteLine($"reached at {shorter.Reached} and {longer.Reached}");

        Assert.True(shorter.Reached > 0, "never held the target in thirty thousand rounds");
        Assert.Equal(shorter.Reached, longer.Reached);
    }
}
