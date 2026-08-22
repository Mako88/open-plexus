using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whether accuracy can tell a true rule from a false one — <b>the signal every other
/// mechanism here is built on</b>, measured for the first time.
/// </summary>
/// <remarks>
/// <para>
/// <b>Everything in this machine ranks by accuracy.</b> The vote weights by it, culling
/// orders by it, subsumption compares it, and repair blames the worst of it. So if a
/// world's true rules and its false ones have the SAME accuracy, every one of those
/// mechanisms is reading noise — and no gate, budget, weighing or deletion rule can
/// recover, because they all consult the same broken instrument.
/// </para>
/// <para>
/// <b>And skew is predicted to do exactly that</b>, which is why this exists. On a world
/// whose answer is one four times in five, a rule saying <i>expect one</i> is right about
/// eighty-five percent of the time whatever it conditions on — because when its own
/// reasoning does not apply, the base rate carries it anyway. A true rule is right a
/// hundred. Fifteen points is what separates knowing from guessing there, against nearly
/// forty on the balanced world.
/// </para>
/// <para>
/// <b>If the gap collapses the defect is upstream</b> of every arm tried tonight. Six
/// budget cells, three vote rules and two genesis gates were all measured on a world
/// where the fitness signal itself may not carry the distinction they were being asked to
/// make.
/// </para>
/// </remarks>
public sealed class SeparableTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    /// <summary>
    /// The accuracy of what is TRUE against the accuracy of what is not.
    /// </summary>
    /// <param name="address">Address bits.</param>
    /// <param name="skew">How often a data bit is one, or zero to leave them even.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    /// <remarks>
    /// <b>Experienced and checkable only, which is `Learned.Grade`'S OWN RULE.</b> A
    /// commitment the world cannot decide belongs in neither column, and one that has
    /// barely fired has an accuracy that is a guess rather than a measurement — counting
    /// either would put the answer in the noise this is trying to measure.
    /// </remarks>
    private (double Sound, double Unsound, int Sounds, int Unsounds) Gap(
        int address, double skew, int seed)
    {
        var settings = new MultiplexerSettings { Address = address, Skew = skew };
        var brain = new Brain(new CommittingSettings(), seed);

        new MultiplexerRun(settings, brain, seed).Run(Rounds);

        // The same mapping, reached from the same seed. `Truths` and `Sound` read which
        // data bit each address selects, and that is drawn from the seed -- so a world
        // built again from it answers about the world the run actually saw.
        var world = new Multiplexer(settings, seed);

        var floor = brain.Dials.Floor;

        var judged = brain.Held.All
            .Where(one => one.Seen >= floor)
            .Where(one => world.Checkable(one.Scope))
            .Select(one => (one.Accuracy, Sound: world.Sound(one.Scope, one.Expects)))
            .ToList();

        var sound = judged.Where(one => one.Sound).Select(one => one.Accuracy).ToList();
        var unsound = judged.Where(one => !one.Sound).Select(one => one.Accuracy).ToList();

        return (
            sound.Count == 0 ? 0.0 : sound.Average(),
            unsound.Count == 0 ? 0.0 : unsound.Average(),
            sound.Count,
            unsound.Count);
    }

    /// <summary>
    /// <b>The reading: how far apart truth and falsehood look, per world.</b>
    /// </summary>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task How_far_apart_a_true_rule_and_a_false_one_look()
    {
        const int Seeds = 8;

        output.WriteLine("the mean accuracy of resident rules the world calls true, "
            + "against the mean of those it calls false");
        output.WriteLine("");

        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (2, 0.8), (3, 0.8) })
        {
            var once = new Dictionary<int, (double Sound, double Unsound, int S, int U)>();

            (double Sound, double Unsound, int S, int U) Cached(int seed)
            {
                if (!once.TryGetValue(seed, out var got))
                    once[seed] = got = Gap(address, skew, seed);

                return got;
            }

            var readings = new (string What, Func<(double Sound, double Unsound, int S, int U), double> Of)[]
            {
                ("sound acc", one => one.Sound),
                ("unsound acc", one => one.Unsound),
                ("the gap", one => one.Sound - one.Unsound),
                ("sound n", one => one.S),
                ("unsound n", one => one.U),
            };

            foreach (var reading in readings)
            {
                var arm = await Sweep.ArmAsync(
                    reading.What,
                    Seeds,
                    seed => Task.FromResult(reading.Of(Cached(seed))));

                output.WriteLine(
                    $"{address + (1 << address),2} bits skew {skew:F1} {reading.What,-12} | "
                    + $"{arm.Mean,8:F4} +/-{arm.StdErr,7:F4} | n={arm.Seeds}");
            }

            output.WriteLine("");
        }
    }
}
