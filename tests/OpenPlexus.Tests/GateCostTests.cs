using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What a blinded repair gate costs — <b>fork 55, and the answer is not the one the
/// question assumed.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>`SplitRepairTests` reported a rate and a rate is not a score.</b> It measured how
/// often a sharded holder repairs where the whole population would have refused, and said
/// in its own commit that nothing there measures what that does to accuracy. A gate that
/// is wrong harmlessly and a gate that is wrong expensively look identical from a
/// disagreement count, and only one of them is a reason to build the query in fork 56.
/// </para>
/// <para>
/// <b>And it under-repairs rather than over-repairing.</b> Which is why the title of this file
/// is not the question it was opened with. <c>Repair</c> mints once a round, so the gate
/// never controlled how MANY repairs happen — only which commitment gets the one attempt.
/// Admitting covered commitments puts low-accuracy generals at the front of a list ordered
/// by accuracy ascending and they consume it. The gate aims repair; it does not limit it.
/// </para>
/// <para>
/// <b>One mechanism on from a known baseline, which is why <see cref="Population.Placing"/>
/// REACHES ONLY THE GATE.</b> Firing, voting and settling are untouched, so a run with a
/// placement differs from one without in the repair gate ALONE. Sharding a world properly
/// and comparing it against a whole one would move four things at once and the score could
/// not say which of them did it.
/// </para>
/// <para>
/// <b>And the arms are paired seed for seed.</b> One seed is not a comparison and will
/// happily invert — winnowing beat bands on seed one and lost to them over five, which is
/// the finding this suite writes on every sweep it runs.
/// </para>
/// </remarks>
public sealed class GateCostTests(ITestOutputHelper output)
{
    private const long Rounds = 20000;

    private const int Address = 3;

    /// <summary>
    /// Twelve, <b>because five separated nothing and every arm moved the same way.</b>
    /// </summary>
    /// <remarks>
    /// <b>Raised after reading, which is worth saying out loud.</b> Five seeds put every
    /// metric between one and two standard errors of the whole-population arm — all in the
    /// same direction and none of it separated. Adding seeds to resolve a consistent
    /// direction is not the same as adding them until something passes, and the difference
    /// is that no bar here moves and none was ever set.
    /// </remarks>
    private const int Seeds = 12;

    private readonly Dictionary<(int Holders, int Seed), Learned> _ran = [];

    /// <summary>Runs the world with the repair gate seeing one holder's worth.</summary>
    /// <remarks>
    /// <para>
    /// <b>`Mending.Uncovered` explicitly, because the default has no gate to blind.</b>
    /// <c>Mending.Outvoted</c> ships as the default and short-circuits the narrows
    /// check outright, so a sweep run on defaults returns three identical arms — which is
    /// what the first version of this file did, and the wiring assertion below is what
    /// caught it. Fork 37 keeps `Uncovered` as a live arm, best on the clean multiplexer
    /// and ruinous on `Arranged`, so this measures an arm and not the machine.
    /// </para>
    /// <para>
    /// <b>And each configuration is run once.</b> Four metrics off one sweep is four
    /// twenty-thousand-round runs of the same thing unless something remembers, and an
    /// instrument that costs four times what it needs to is an instrument nobody runs.
    /// </para>
    /// </remarks>
    /// <summary>
    /// The gate, on the timing it was measured with.
    /// </summary>
    /// <remarks>
    /// <b>Both settings named, because one of them used to be implied.</b>
    /// <c>Mending.Uncovered</c> meant the gate AND every-round repair while it was one
    /// enum; naming only the gate now would silently move every reading in this file onto
    /// the after-failure timing, where the same gate is six and a half standard errors
    /// worse. Written once because two copies is two chances for one of them to drift.
    /// </remarks>
    private static CommittingSettings Gated => new()
    {
        Mending = Mending.Uncovered,
        Repairing = Repairing.EveryRound,
    };

    private Learned Run(int holders, int seed)
    {
        if (_ran.TryGetValue((holders, seed), out var already)) return already;

        var brain = new Brain(Gated, seed);

        // Set before the run and never during it. A placement that changed mid-run would
        // be a different machine either side of the change, and the score would be an
        // average over two of them.
        if (holders > 1)
            brain.Held.Placing = one => one.Identity.Value % (ulong)holders;

        var learned = new MultiplexerRun(
            new MultiplexerSettings { Address = Address }, brain, seed).Run(Rounds);

        _ran[(holders, seed)] = learned;

        return learned;
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task What_the_repair_gate_going_blind_costs_the_score()
    {
        // Accuracy is the headline and is not the only thing asked, because an accuracy
        // can be reached by memorising and the gate's whole job is to stop the population
        // growing rules it does not need. If blinding it costs nothing on the score and
        // adds hundreds of residents, that is a cost this suite already knows how to read.
        var accuracy = await Sweep.AcrossAsync(Seeds,
            ("whole", seed => Task.FromResult(Run(1, seed).Recent)),
            ("3 holders", seed => Task.FromResult(Run(3, seed).Recent)),
            ("12 holders", seed => Task.FromResult(Run(12, seed).Recent)));

        output.WriteLine("recent accuracy");
        output.WriteLine(Sweep.Table(accuracy));

        var resident = await Sweep.AcrossAsync(Seeds,
            ("whole", seed => Task.FromResult((double)Run(1, seed).Resident)),
            ("3 holders", seed => Task.FromResult((double)Run(3, seed).Resident)),
            ("12 holders", seed => Task.FromResult((double)Run(12, seed).Resident)));

        output.WriteLine("resident commitments");
        output.WriteLine(Sweep.Table(resident));

        var sound = await Sweep.AcrossAsync(Seeds,
            ("whole", seed => Task.FromResult((double)Run(1, seed).Sound)),
            ("3 holders", seed => Task.FromResult((double)Run(3, seed).Sound)),
            ("12 holders", seed => Task.FromResult((double)Run(12, seed).Sound)));

        output.WriteLine("sound commitments");
        output.WriteLine(Sweep.Table(sound));

        var repaired = await Sweep.AcrossAsync(Seeds,
            ("whole", seed => Task.FromResult((double)Run(1, seed).Repaired)),
            ("3 holders", seed => Task.FromResult((double)Run(3, seed).Repaired)),
            ("12 holders", seed => Task.FromResult((double)Run(12, seed).Repaired)));

        output.WriteLine("children minted by repair");
        output.WriteLine(Sweep.Table(repaired));

        // The wiring check, and it is the only assertion here. `Placing` connected to
        // nothing would leave every arm identical and every table reading no cost -- a dial
        // declared, documented, passed everywhere and wired to nought, which this project
        // has lost a stamina dial to for the life of three measurements.
        //
        // And it asserts a difference rather than a direction, because the first version
        // asserted a direction and got it backwards. Blinding the gate admits MORE
        // candidates and mints FEWER children -- `Repair` returns after one successful mint,
        // so the gate never controlled how many repairs happen, only which commitment gets
        // the attempt. A prediction written into a wiring check fails for two completely
        // different reasons and reads the same either way.
        Assert.True(Math.Abs(repaired[2].Mean - repaired[0].Mean) > 0.0,
            $"blinding the gate minted {repaired[2].Mean:F1} children against "
            + $"{repaired[0].Mean:F1} whole — `Placing` is not reaching `Repair`");

        // NO BAR ON THE SCORE, because what blinding the gate SHOULD cost has never been
        // measured and a threshold written before the first reading would be a prediction
        // dressed as a requirement. The tables are the finding.
    }

    /// <summary>
    /// Four, <b>because a scene world is not a bit world and the same sweep costs an
    /// order more.</b>
    /// </summary>
    /// <remarks>
    /// <b>Written down because the first version reused twelve and was not costed.</b>
    /// Thirty-six runs of <c>Arranged</c> is not a sweep anybody runs twice, and an
    /// instrument nobody runs is worth nothing however careful it is. Four resolves a
    /// DIRECTION and not a separation — so nothing here may be reported as a cost, only as
    /// which way it went and whether that is worth the seeds to settle.
    /// </remarks>
    private const int SceneSeeds = 4;

    private readonly Dictionary<(int Holders, int Seed), Grounded> _arranged = [];

    /// <summary>The same three arms on the world where the gate itself is the problem.</summary>
    /// <param name="holders">How many machines the population is spread over.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    private Grounded OnArranged(int holders, int seed)
    {
        if (_arranged.TryGetValue((holders, seed), out var already)) return already;

        var brain = new Brain(Gated, seed);

        if (holders > 1)
            brain.Held.Placing = one => one.Identity.Value % (ulong)holders;

        var got = new ArrangedRun(
            new ArrangedSettings { Side = 3, Cell = 3, Clutter = 1, Hold = 4 },
            brain,
            Looking.Whole,
            seed).Run(Rounds);

        _arranged[(holders, seed)] = got;

        return got;
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task And_the_same_blinding_on_the_world_where_the_gate_is_the_problem()
    {
        // Fork 55 was answered on one world and said so. On the multiplexer, blinding the
        // gate is worse on every metric -- and this project's standing finding is that
        // `Mending.Uncovered` wins the clean multiplexer and RUINS `Arranged`. A mechanism
        // that helps in one place and hurts in another cannot have one answer to *what
        // does losing it cost*, and reporting the multiplexer's number as the cost would
        // be a dial cashed in citing a finding from one world in ten.
        //
        // So the prediction is an inversion, which is why this is worth the run. If the
        // gate is what sinks this world, a holder that cannot see far enough to apply it
        // should do BETTER blind -- and a grid that came back flat would say the gate is
        // not what sinks it after all, which is worth as much.
        //
        // And the withheld set is the score here and not the trailing one. `Arranged` is
        // where this repo learnt that a drawn score can be perfect while the withheld one
        // is not, so the trained number would agree with itself and say nothing.
        var withheld = await Sweep.AcrossAsync(SceneSeeds,
            ("whole", seed => Task.FromResult(OnArranged(1, seed).Tally.Unseen?.Accuracy ?? 0.0)),
            ("3 holders", seed => Task.FromResult(OnArranged(3, seed).Tally.Unseen?.Accuracy ?? 0.0)),
            ("12 holders", seed => Task.FromResult(OnArranged(12, seed).Tally.Unseen?.Accuracy ?? 0.0)));

        output.WriteLine("withheld accuracy");
        output.WriteLine(Sweep.Table(withheld));

        var sound = await Sweep.AcrossAsync(SceneSeeds,
            ("whole", seed => Task.FromResult((double)OnArranged(1, seed).Rules.Sound)),
            ("3 holders", seed => Task.FromResult((double)OnArranged(3, seed).Rules.Sound)),
            ("12 holders", seed => Task.FromResult((double)OnArranged(12, seed).Rules.Sound)));

        output.WriteLine("sound rules");
        output.WriteLine(Sweep.Table(sound));

        var repaired = await Sweep.AcrossAsync(SceneSeeds,
            ("whole", seed => Task.FromResult((double)OnArranged(1, seed).Tally.Repaired)),
            ("3 holders", seed => Task.FromResult((double)OnArranged(3, seed).Tally.Repaired)),
            ("12 holders", seed => Task.FromResult((double)OnArranged(12, seed).Tally.Repaired)));

        output.WriteLine("children minted by repair");
        output.WriteLine(Sweep.Table(repaired));

        // The wiring check again, and a difference rather than a direction. The whole
        // point of running a second world is that the direction is what might invert.
        Assert.True(Math.Abs(repaired[2].Mean - repaired[0].Mean) > 0.0,
            $"blinding minted {repaired[2].Mean:F1} against {repaired[0].Mean:F1} whole — "
            + "`Placing` is not reaching `Repair` on this world");

        // And the withheld set has to exist, or every row above is a nullable defaulting to
        // nought and three arms of zero agree perfectly.
        Assert.True(withheld[0].Mean > 0.0,
            "no withheld score came back, so every arm is a default and the grid is empty");
    }
}
