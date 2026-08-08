using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// <c>Mending</c> is a two-by-two read as a list — <b>and the comparison every finding
/// about it rests on moves both axes at once.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE SETTING DECIDES TWO INDEPENDENT THINGS AND IS NAMED FOR ONE.</b> Whether repair
/// runs every round or only after the vote was wrong is decided in <c>Cycle</c>;
/// whether a commitment something else already narrows may be repaired at all is decided
/// in <c>Population.Mend</c>. <see cref="Mending.Outvoted"/> is after-failure with no
/// gate, <see cref="Mending.Uncovered"/> is every-round WITH the gate — so the two differ
/// in both, and no measurement of them can say which half did anything.
/// </para>
/// <para>
/// <b><see cref="Mending.Neglected"/> IS THE CELL THAT SEPARATES THEM AND HAS NEVER BEEN
/// READ AS ONE.</b> It waits for the failure like <c>Outvoted</c> and takes the gate like
/// <c>Uncovered</c>, so <c>Outvoted</c> against it isolates the GATE and it against
/// <c>Uncovered</c> isolates WHEN. The fourth cell — every round with no gate — does not
/// exist as a setting, and this file does not add one.
/// </para>
/// <para>
/// <b>AND THE REASON TO ASK NOW IS THAT THE GATE TURNED OUT TO BE NEARLY INERT
/// SOMEWHERE.</b> <c>GateCostTests</c> blinds the gate on <c>Arranged</c> and moves repair
/// by four tenths of a percent, against thirty on the multiplexer — so whatever makes
/// <c>Uncovered</c> behave differently on that world, it is not mostly the thing it is
/// named after. That is a documented finding resting on a conflation, which is worth more
/// than a new one.
/// </para>
/// </remarks>
public sealed class MendingTests(ITestOutputHelper output)
{
    private const long Rounds = 20000;

    private const int Address = 3;

    private const int Seeds = 12;

    private readonly Dictionary<(Mending Arm, int Seed), Learned> _ran = [];

    private Learned Run(Mending arm, int seed)
    {
        if (_ran.TryGetValue((arm, seed), out var already)) return already;

        var learned = new MultiplexerRun(
            new MultiplexerSettings { Address = Address },
            new Brain(new CommittingSettings { Mending = arm }, seed),
            seed).Run(Rounds);

        _ran[(arm, seed)] = learned;

        return learned;
    }

    /// <summary>Every arm on one metric, in the order that makes the axes readable.</summary>
    /// <param name="metric">What to pull out of a run.</param>
    /// <remarks>
    /// <b>ORDERED SO EACH ROW DIFFERS FROM THE ONE ABOVE IT IN EXACTLY ONE THING.</b>
    /// <c>Outvoted</c> to <c>Neglected</c> adds the gate; <c>Neglected</c> to
    /// <c>Uncovered</c> moves repair off the failure branch; <c>Uncovered</c> to
    /// <c>Improving</c> adds the did-forking-ever-pay test. Every separation printed is
    /// against the first row, so the reading is cumulative and the differences between
    /// adjacent rows are what the axes cost.
    /// </remarks>
    private Task<IReadOnlyList<Measured>> Across(Func<Learned, double> metric) =>
        Sweep.AcrossAsync(Seeds,
            ("after failure, no gate", seed => Task.FromResult(metric(Run(Mending.Outvoted, seed)))),
            ("after failure, gate", seed => Task.FromResult(metric(Run(Mending.Neglected, seed)))),
            ("every round, gate", seed => Task.FromResult(metric(Run(Mending.Uncovered, seed)))),
            ("every round, gate, paid", seed => Task.FromResult(metric(Run(Mending.Improving, seed)))));

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task Which_half_of_the_setting_is_doing_the_work()
    {
        foreach (var (name, metric) in new (string, Func<Learned, double>)[]
        {
            ("recent accuracy", one => one.Recent),
            ("resident commitments", one => one.Resident),
            ("sound commitments", one => one.Sound),
            ("children minted by repair", one => one.Repaired),
        })
        {
            output.WriteLine(name);
            output.WriteLine(Sweep.Table(await Across(metric)));
        }

        // THE ONE ASSERTION, AND IT IS THAT THE TWO AXES ARE NOT THE SAME AXIS. If adding
        // the gate and moving repair off the failure branch produced the same population,
        // then `Mending` really is a list and this file is measuring one thing twice --
        // which would be worth knowing and is the only outcome that makes the decomposition
        // pointless.
        var repaired = await Across(one => one.Repaired);

        Assert.True(
            Math.Abs(repaired[1].Mean - repaired[0].Mean) > 0.0
            && Math.Abs(repaired[2].Mean - repaired[1].Mean) > 0.0,
            $"the arms mint {repaired[0].Mean:F1}, {repaired[1].Mean:F1} and "
            + $"{repaired[2].Mean:F1} children — two of them are the same machine, so one "
            + "of the axes this file claims to separate does not exist");

        // NO BAR ON ANY SCORE. Which arm should win has been measured elsewhere and is a
        // fact about a world; what this asks is only which HALF of the setting the
        // difference lives in, and a threshold would be a prediction dressed as a
        // requirement.
    }
}
