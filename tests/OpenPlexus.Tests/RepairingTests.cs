using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What breaking the vote's hold over repair costs — <b>the other half of the lineage
/// reading, and the half that decides whether it changes anything.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b><c>LineageTests</c> SAYS THE DEFECT IS A COUPLING AND <see cref="Repairing"/> IS
/// THE DIAL THAT REMOVES IT.</b> Under <see cref="Repairing.AfterFailure"/> repair runs
/// only on a round the vote got wrong, so under skew almost all blame lands on the
/// majority lineages and the rules that would carry the hard rounds are never offered.
/// <see cref="Repairing.EveryRound"/> touches the vote in no way and takes hard-round
/// coverage from 4% to 97% at six bits.
/// </para>
/// <para>
/// <b>THE KILL CONDITION IS WRITTEN DOWN BEFORE THE RUN, BECAUSE THE PLAN ALREADY HOLDS
/// EVIDENCE AGAINST IT.</b> Fork 58 says the gate's sign flips with the timing and that
/// on <see cref="Worlds.Arranged"/> repairing without waiting for the vote minted 1,349
/// children where the waiting arm minted nine, taking a perfect withheld score to 0.752.
/// So this is a default candidate only if it costs nothing on the worlds where the vote
/// is already right; if it buys the skewed world by ruining the even ones, it is a
/// diagnostic that confirms the mechanism and not a change to what ships.
/// </para>
/// </remarks>
public sealed class RepairingTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;
    private const int Runs = 8;

    /// <summary>Fixed forever — see <c>Sweep</c>, whose purpose this deliberately is not.</summary>
    private const uint Purpose = 0x5EED_0065;

    private static readonly (string Name, CommittingSettings Dials)[] Arms =
    [
        ("afterfailure", new CommittingSettings()),
        ("everyround", new CommittingSettings { Repairing = Repairing.EveryRound }),
    ];

    /// <summary>
    /// <b>WHAT THE COUPLING COSTS AND WHAT REMOVING IT COSTS, ON EVERY WIDTH AND SKEW.</b>
    /// </summary>
    [Fact]
    public void Repairing_without_waiting_for_the_vote_across_the_multiplexer_grid()
    {
        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (2, 0.8), (3, 0.8) })
        {
            var settings = new MultiplexerSettings { Address = address, Skew = skew };

            output.WriteLine($"=== {address + (1 << address)} bits, skew {skew:F1}, "
                + $"{Runs} seeds");

            var taken = new Dictionary<string, Dictionary<string, Measured>>();

            foreach (var (name, dials) in Arms) taken[name] = Take(settings, dials);

            output.WriteLine(
                $"reading    {"afterfailure",-22} {"everyround",-22} sigma");

            foreach (var reading in taken["afterfailure"].Keys)
            {
                var was = taken["afterfailure"][reading];
                var now = taken["everyround"][reading];

                output.WriteLine(
                    $"{reading,-10} {Show(was),-22} {Show(now),-22} "
                    + $"{now.Separation(was),5:F1}");
            }

            output.WriteLine("");
        }
    }

    /// <summary>
    /// <b>THE WORLD THE PLAN SAYS THIS RUINS, ASKED DIRECTLY.</b>
    /// </summary>
    /// <remarks>
    /// <b>SEPARATE FROM THE GRID BECAUSE IT IS THE EXPENSIVE HALF</b>, and because a
    /// refutation arriving here says something different from a null on the multiplexer:
    /// the multiplexer says whether the arm PAYS and this says whether it may ship.
    /// </remarks>
    [Fact]
    public void Repairing_without_waiting_for_the_vote_on_the_world_it_is_predicted_to_ruin()
    {
        var small = new ArrangedSettings { Side = 3, Cell = 3, Clutter = 1, Hold = 4 };

        output.WriteLine($"=== arranged, tiled, {Runs} seeds");
        output.WriteLine($"arm          {"unseen",-22} {"sound",-22} "
            + $"{"unsound",-22} {"residents",-22} repaired");

        Measured? against = null;

        foreach (var (name, dials) in Arms)
        {
            var unseen = new List<double>();
            var sound = new List<double>();
            var unsound = new List<double>();
            var residents = new List<double>();
            var repaired = new List<double>();

            for (var index = 1; index <= Runs; index++)
            {
                var seed = Seeds.Apart(index, Purpose);
                var run = new ArrangedRun(small, new Brain(dials, seed), Looking.Tiled, seed);

                var got = run.Run(Rounds);

                unseen.Add(got.Tally.Unseen!.Accuracy);
                sound.Add(got.Rules.Sound);
                unsound.Add(got.Rules.Unsound);
                residents.Add(got.Tally.Resident);
                repaired.Add(got.Tally.Repaired);
            }

            var mine = new Measured { Arm = name, Values = unseen };

            output.WriteLine(
                $"{name,-12} {Show(mine),-22} "
                + $"{Show(new Measured { Arm = name, Values = sound }),-22} "
                + $"{Show(new Measured { Arm = name, Values = unsound }),-22} "
                + $"{Show(new Measured { Arm = name, Values = residents }),-22} "
                + $"{repaired.Average():F0}"
                + (against is null ? string.Empty : $"  ({mine.Separation(against):F1} sigma)"));

            against ??= mine;
        }
    }

    /// <summary>Every reading one arm produces on one world, across the seeds.</summary>
    /// <param name="settings">The world.</param>
    /// <param name="dials">The arm.</param>
    /// <remarks>
    /// <b>THE SEEDS ARE MIXED AND THE ARMS SHARE THEM</b>, which is what makes the second
    /// column a control rather than a second experiment — see <c>Sweep.ArmAsync</c>, whose
    /// discipline this borrows and whose purpose word it deliberately does not reuse.
    /// </remarks>
    private static Dictionary<string, Measured> Take(
        MultiplexerSettings settings, CommittingSettings dials)
    {
        var readings = new Dictionary<string, List<double>>
        {
            ["paying"] = [], ["recent"] = [], ["found"] = [],
            ["sound"] = [], ["unsound"] = [], ["residents"] = [], ["repaired"] = [],
        };

        for (var index = 1; index <= Runs; index++)
        {
            var seed = Seeds.Apart(index, Purpose);
            var run = new MultiplexerRun(settings, new Brain(dials, seed), seed, census: true);

            var learnt = run.Run(Rounds);

            readings["paying"].Add(learnt.Census!.Paying);
            readings["recent"].Add(learnt.Recent);
            readings["found"].Add(learnt.Found);
            readings["sound"].Add(learnt.Sound);
            readings["unsound"].Add(learnt.Unsound);
            readings["residents"].Add(learnt.Resident);
            readings["repaired"].Add(learnt.Repaired);
        }

        return readings.ToDictionary(
            one => one.Key,
            one => new Measured { Arm = one.Key, Values = one.Value });
    }

    /// <summary>A mean and its standard error, in one column.</summary>
    /// <param name="measured">What was taken.</param>
    private static string Show(Measured measured) =>
        $"{measured.Mean,9:F3} +/- {measured.StdErr:F3}";
}
