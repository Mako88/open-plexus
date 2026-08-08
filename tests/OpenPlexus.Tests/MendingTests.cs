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
/// <c>Uncovered</c> isolates WHEN. The fourth cell — every round with no gate — is not a
/// setting today and WAS one: <c>Mending.Earned</c> sat exactly there, before the failure
/// return, and its revival row records that <c>Uncovered</c> dominated it — <i>the same
/// rule with the redundant repairs removed</i>, which is this axis under the mechanism's
/// own vocabulary. So the gate helps every round and hurts after a failure, and the sign of
/// a mechanism depends on the budget beside it.
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

    private readonly Dictionary<(Mending Gate, Repairing When, int Seed), Learned> _ran = [];

    private Learned Run(Mending gate, Repairing when, int seed)
    {
        if (_ran.TryGetValue((gate, when, seed), out var already)) return already;

        var learned = new MultiplexerRun(
            new MultiplexerSettings { Address = Address },
            new Brain(
                new CommittingSettings { Mending = gate, Repairing = when }, seed),
            seed).Run(Rounds);

        _ran[(gate, when, seed)] = learned;

        return learned;
    }

    /// <summary>Every arm on one metric, in the order that makes the axes readable.</summary>
    /// <param name="seeds">How many seeds each arm is run over.</param>
    /// <param name="of">What one arm on one seed is worth — the run and the metric together.</param>
    /// <remarks>
    /// <b>ORDERED SO EACH ROW DIFFERS FROM THE ONE ABOVE IT IN EXACTLY ONE THING.</b>
    /// Row one to row two adds the gate; row two to row three moves repair off the failure
    /// branch; row three to row four adds the did-forking-ever-pay test. Every separation
    /// printed is against the first row, so the reading is cumulative and the differences
    /// between adjacent rows are what the axes cost.
    /// </remarks>
    /// <remarks>
    /// <b>THIS FILE IS WHY THE SETTING IS TWO SETTINGS NOW.</b> The four rows were four
    /// values of one enum, and reading them as a list is what produced <i>a dial whose best
    /// value moves with the world</i>; ordered so each row differs in one thing, they are a
    /// two-by-two grid with a cell missing. <see cref="Fixture.Repairs"/> holds the same
    /// four arrangements as the pairs they always were, so every number this file has
    /// printed stays comparable with every number it prints next.
    /// </remarks>
    private static Task<IReadOnlyList<Measured>> Across(
        int seeds, Func<Mending, Repairing, int, double> of) =>
        Sweep.AcrossAsync(seeds,
            [.. Fixture.Repairs.Select<(string Arm, Mending Gate, Repairing When),
                (string, Func<int, Task<double>>)>(
                one => (one.Arm, seed => Task.FromResult(of(one.Gate, one.When, seed))))]);

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
            output.WriteLine(Sweep.Table(await Across(Seeds, (gate, when, seed) => metric(Run(gate, when, seed)))));
        }

        // THE ONE ASSERTION, AND IT IS THAT THE TWO AXES ARE NOT THE SAME AXIS. If adding
        // the gate and moving repair off the failure branch produced the same population,
        // then `Mending` really is a list and this file is measuring one thing twice --
        // which would be worth knowing and is the only outcome that makes the decomposition
        // pointless.
        var repaired = await Across(Seeds, (gate, when, seed) => Run(gate, when, seed).Repaired);

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

    /// <summary>Four, because a scene world costs an order more than a bit world.</summary>
    private const int SceneSeeds = 4;

    private readonly Dictionary<(Mending Gate, Repairing When, int Seed), Grounded> _arranged = [];

    private Grounded OnArranged(Mending gate, Repairing when, int seed)
    {
        if (_arranged.TryGetValue((gate, when, seed), out var already)) return already;

        var got = new ArrangedRun(
            new ArrangedSettings { Side = 3, Cell = 3, Clutter = 1, Hold = 4 },
            new Brain(
                new CommittingSettings { Mending = gate, Repairing = when }, seed),
            Looking.Whole,
            seed).Run(Rounds);

        _arranged[(gate, when, seed)] = got;

        return got;
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task And_which_half_makes_the_setting_ruinous_on_the_other_world()
    {
        // A HYPOTHESIS UNDER TEST RATHER THAN A SWEEP FOR ITS OWN SAKE. `Mending.Uncovered`
        // is recorded as ruinous on `Arranged`, and `GateCostTests` shows the gate nearly
        // inert there -- so if that verdict is real it has to be the every-round half doing
        // it, and the two after-failure rows should land together while the two every-round
        // rows land together somewhere else.
        //
        // AND THE PREDICTION IS NOT IN AN ASSERTION, for the reason this suite has already
        // paid for once: a check that encodes a guess fails identically whether the wiring
        // is broken or the guess is backwards.
        foreach (var (name, metric) in new (string, Func<Grounded, double>)[]
        {
            ("withheld accuracy", one => one.Tally.Unseen?.Accuracy ?? 0.0),
            ("sound rules", one => one.Rules.Sound),
            ("children minted by repair", one => one.Tally.Repaired),
        })
        {
            output.WriteLine(name);
            output.WriteLine(Sweep.Table(
                await Across(SceneSeeds, (gate, when, seed) => metric(OnArranged(gate, when, seed)))));
        }

        // THE INSTRUMENT CHECK, AND IT IS ABOUT THE WITHHELD SET EXISTING. Every row above
        // reads a nullable, so a world that withheld nothing would print four zeroes and
        // agree with itself perfectly.
        var withheld = await Across(
            SceneSeeds, (gate, when, seed) => OnArranged(gate, when, seed).Tally.Unseen?.Accuracy ?? 0.0);

        Assert.True(withheld[0].Mean > 0.0,
            "no withheld score came back, so every arm is a default and the grid is empty");

        // FOUR SEEDS RESOLVES A DIRECTION AND NOT A SEPARATION, so nothing here may be
        // reported as a cost -- only as which rows landed together.
    }
}
