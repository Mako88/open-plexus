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
/// <b>The setting decides two independent things and is named for one.</b> Whether repair
/// runs every round or only after the vote was wrong is decided in <c>Cycle</c>;
/// whether a commitment something else already narrows may be repaired at all is decided
/// in <c>Population.Repair</c>. <c>Mending.Outvoted</c> is after-failure with no
/// gate, <see cref="Mending.Uncovered"/> is every-round WITH the gate — so the two differ
/// in both, and no measurement of them can say which half did anything.
/// </para>
/// <para>
/// <b><c>Mending.Neglected</c> is the cell that separates them and has never been
/// read as one.</b> It waits for the failure like <c>Outvoted</c> and takes the gate like
/// <c>Uncovered</c>, so <c>Outvoted</c> against it isolates the GATE and it against
/// <c>Uncovered</c> isolates WHEN. The fourth cell — every round with no gate — is not a
/// setting today and WAS one: <c>Mending.Earned</c> sat exactly there, before the failure
/// return, and its revival row records that <c>Uncovered</c> dominated it — <i>the same
/// rule with the redundant repairs removed</i>, which is this axis under the mechanism's
/// own vocabulary. So the gate helps every round and hurts after a failure, and the sign of
/// a mechanism depends on the budget beside it.
/// </para>
/// <para>
/// <b>And the reason to ask now is that the gate turned out to be nearly inert
/// somewhere.</b> <c>GateCostTests</c> blinds the gate on <c>Arranged</c> and moves repair
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
    /// <b>Ordered so each row differs from the one above it in exactly one thing.</b>
    /// Row one to row two adds the gate; row two to row three moves repair off the failure
    /// branch; row three to row four adds the did-forking-ever-pay test. Every separation
    /// printed is against the first row, so the reading is cumulative and the differences
    /// between adjacent rows are what the axes cost.
    /// </remarks>
    /// <remarks>
    /// <b>This file is why the setting is two settings now.</b> The four rows were four
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

        // The one assertion, and it is that the two axes are not the same axis. If adding
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
        // A hypothesis under test rather than a sweep for its own sake. `Mending.Uncovered`
        // is recorded as ruinous on `Arranged`, and `GateCostTests` shows the gate nearly
        // inert there -- so if that verdict is real it has to be the every-round half doing
        // it, and the two after-failure rows should land together while the two every-round
        // rows land together somewhere else.
        //
        // And the prediction is not in an assertion, for the reason this suite has already
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

        // The instrument check, and it is about the withheld set existing. Every row above
        // reads a nullable, so a world that withheld nothing would print four zeroes and
        // agree with itself perfectly.
        var withheld = await Across(
            SceneSeeds, (gate, when, seed) => OnArranged(gate, when, seed).Tally.Unseen?.Accuracy ?? 0.0);

        Assert.True(withheld[0].Mean > 0.0,
            "no withheld score came back, so every arm is a default and the grid is empty");

        // Four seeds resolves a direction and not a separation, so nothing here may be
        // reported as a cost -- only as which rows landed together.
    }

    /// <summary>
    /// <b>The two cells nothing has ever run — the whole grid, at last.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Unreachable rather than refused, which is the difference that makes this worth
    /// doing.</b> One enum decided both axes and no value of it landed on <i>ungated,
    /// every round</i> or <i>improving, after a failure</i> — so those two are not arms
    /// somebody tried and dropped, they are corners of a two-by-two that could not be
    /// spelled. <see cref="Fixture.Reachable"/> holds them apart from
    /// <see cref="Fixture.Repairs"/> so the four rows every commit is labelled by are the
    /// same four rows.
    /// </para>
    /// <para>
    /// <b>The prediction, written before the first reading and not in an assertion.</b>
    /// The timing is the load-bearing axis — every-round repair leads on both worlds
    /// measured — and the gate's sign flips with it: every round <c>Uncovered</c> beat
    /// <c>Earned</c>, and after a failure the gate is six and a half standard errors
    /// BEHIND no gate at all. If the timing is what carries and the gate is a second-order
    /// correction, <i>every round, no gate</i> should sit at or above the best row here
    /// and nothing has ever looked at it.
    /// </para>
    /// <para>
    /// <b>And the other new cell is the one that should be worst, which is what makes it
    /// a control rather than a sixth guess.</b> <i>Improving, after a failure</i> stacks
    /// the two brakes that were each measured as costly on their own — waiting for the
    /// vote to be wrong, and then also asking whether forking has ever paid. A grid where
    /// it is not last would say the two brakes are not additive, which is a fact about
    /// them nothing has established either.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task The_two_cells_the_split_made_reachable()
    {
        var all = Fixture.Repairs.Concat(Fixture.Reachable).ToArray();

        foreach (var (name, metric) in new (string, Func<Learned, double>)[]
        {
            ("recent accuracy", one => one.Recent),
            ("sound commitments", one => one.Sound),
            ("unsound commitments", one => one.Unsound),
            ("resident commitments", one => one.Resident),
            ("children minted by repair", one => one.Repaired),
        })
        {
            output.WriteLine(name);

            output.WriteLine(Sweep.Table(await Sweep.AcrossAsync(Seeds,
                [.. all.Select<(string Arm, Mending Gate, Repairing When),
                    (string, Func<int, Task<double>>)>(
                    one => (one.Arm, seed => Task.FromResult(
                        metric(Run(one.Gate, one.When, seed)))))])));
        }

        // The one assertion is that the new cells are reachable at all, which is not
        // trivial and is exactly the shape of failure this repo keeps finding: a
        // combination that compiles, runs, and quietly behaves as one of the cells beside
        // it would print six rows and mean four. If ungated-every-round mints the same
        // number of children as gated-every-round, the gate is not being read.
        var repaired = await Sweep.AcrossAsync(Seeds,
            [.. all.Select<(string Arm, Mending Gate, Repairing When),
                (string, Func<int, Task<double>>)>(
                one => (one.Arm, seed => Task.FromResult(
                    (double)Run(one.Gate, one.When, seed).Repaired)))]);

        Assert.NotEqual(repaired[2].Mean, repaired[4].Mean);
        Assert.NotEqual(repaired[1].Mean, repaired[5].Mean);

        // NO BAR ON ANY SCORE, because the prediction above is a prediction. A threshold
        // written before the first reading fails identically whether the wiring is broken
        // or the guess is backwards.
    }
}
