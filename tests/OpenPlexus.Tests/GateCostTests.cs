using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What over-repairing actually costs — <b>fork 55, and it is this session's own loosest
/// claim being closed rather than a new question.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>`SplitRepairTests` REPORTED A RATE AND A RATE IS NOT A SCORE.</b> It measured how
/// often a sharded holder repairs where the whole population would have refused, and said
/// in its own commit that nothing there measures what that does to accuracy. A gate that
/// is wrong harmlessly and a gate that is wrong expensively look identical from a
/// disagreement count, and only one of them is a reason to build the query in fork 56.
/// </para>
/// <para>
/// <b>ONE MECHANISM ON FROM A KNOWN BASELINE, WHICH IS WHY <see cref="Population.Placing"/>
/// REACHES ONLY THE GATE.</b> Firing, voting and settling are untouched, so a run with a
/// placement differs from one without in the repair gate ALONE. Sharding a world properly
/// and comparing it against a whole one would move four things at once and the score could
/// not say which of them did it.
/// </para>
/// <para>
/// <b>AND THE ARMS ARE PAIRED SEED FOR SEED.</b> One seed is not a comparison and will
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
    /// <b>RAISED AFTER READING, WHICH IS WORTH SAYING OUT LOUD.</b> Five seeds put every
    /// metric between one and two standard errors of the whole-population arm — all in the
    /// same direction and none of it separated. Adding seeds to resolve a consistent
    /// direction is not the same as adding them until something passes, and the difference
    /// is that no bar here moves and none was ever set.
    /// </remarks>
    private const int Seeds = 12;

    private readonly Dictionary<(int Holders, int Seed), Learned> _ran = [];

    /// <summary>Runs the world with the repair gate seeing one holder's worth.</summary>
    /// <param name="holders">How many machines the population is spread over.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    /// <remarks>
    /// <para>
    /// <b>`Mending.Uncovered` EXPLICITLY, BECAUSE THE DEFAULT HAS NO GATE TO BLIND.</b>
    /// <see cref="Mending.Outvoted"/> ships as the default and short-circuits the narrows
    /// check outright, so a sweep run on defaults returns three identical arms — which is
    /// what the first version of this file did, and the wiring assertion below is what
    /// caught it. Fork 37 keeps `Uncovered` as a live arm, best on the clean multiplexer
    /// and ruinous on `Arranged`, so this measures an arm and not the machine.
    /// </para>
    /// <para>
    /// <b>AND EACH CONFIGURATION IS RUN ONCE.</b> Four metrics off one sweep is four
    /// twenty-thousand-round runs of the same thing unless something remembers, and an
    /// instrument that costs four times what it needs to is an instrument nobody runs.
    /// </para>
    /// </remarks>
    private Learned Run(int holders, int seed)
    {
        if (_ran.TryGetValue((holders, seed), out var already)) return already;

        var brain = new Brain(new CommittingSettings { Mending = Mending.Uncovered }, seed);

        // SET BEFORE THE RUN AND NEVER DURING IT. A placement that changed mid-run would
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
        // ACCURACY IS THE HEADLINE AND IS NOT THE ONLY THING ASKED, because an accuracy
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

        // THE WIRING CHECK, AND IT IS THE ONLY ASSERTION HERE. `Placing` connected to
        // nothing would leave every arm identical and every table reading no cost -- a dial
        // declared, documented, passed everywhere and wired to nought, which this project
        // has lost a stamina dial to for the life of three measurements.
        //
        // AND IT ASSERTS A DIFFERENCE RATHER THAN A DIRECTION, BECAUSE THE FIRST VERSION
        // ASSERTED A DIRECTION AND GOT IT BACKWARDS. Blinding the gate admits MORE
        // candidates and mints FEWER children -- `Mend` returns after one successful mint,
        // so the gate never controlled how many repairs happen, only which commitment gets
        // the attempt. A prediction written into a wiring check fails for two completely
        // different reasons and reads the same either way.
        Assert.True(Math.Abs(repaired[2].Mean - repaired[0].Mean) > 0.0,
            $"blinding the gate minted {repaired[2].Mean:F1} children against "
            + $"{repaired[0].Mean:F1} whole — `Placing` is not reaching `Mend`");

        // NO BAR ON THE SCORE, because what blinding the gate SHOULD cost has never been
        // measured and a threshold written before the first reading would be a prediction
        // dressed as a requirement. The tables are the finding.
    }
}
