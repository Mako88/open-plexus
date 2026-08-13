using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Which seeds genesis never mints — <b>the question the choosing control left standing.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Repair chooses well and builds true rules that pay nothing, so what it is handed is
/// the suspect.</b> Covering mints <c>{c} → arrived</c> and its whole reachable space is
/// the vocabulary times the outcomes — twenty-four rules at six bits, of which it mints
/// seventeen and then stops for the rest of the run. Repair can only specialise what is
/// there, so a seed never minted is a rule never reachable, forever.
/// </para>
/// <para>
/// <b>And the prediction is specific enough to be wrong.</b> On a world whose answer is
/// the commoner outcome four times in five, a minority round is rare and the gate closes
/// as soon as ANYTHING proposes that outcome — so the seeds that go missing should be the
/// minority ones, which are exactly the seeds a rule covering the hard rounds would have
/// to be built from. If the missing seeds are spread evenly across both outcomes instead,
/// this explanation goes the way of the other five.
/// </para>
/// </remarks>
public sealed class SeedingTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    /// <summary>
    /// <b>The one-code population, split by what it expects.</b>
    /// </summary>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Which_outcome_the_seeds_genesis_never_mints_belong_to()
    {
        output.WriteLine("roots: resident commitments whose scope is a single code");
        output.WriteLine("split by the outcome they expect, against the world's own rate");
        output.WriteLine("");

        foreach (var (address, skew) in new[] { (2, 0.0), (2, 0.8), (3, 0.8) })
        {
            var settings = new MultiplexerSettings { Address = address, Skew = skew };
            var brain = new Brain(new CommittingSettings(), seed: 1);

            new MultiplexerRun(settings, brain, seed: 1, census: true).Run(Rounds);

            // WHAT THE WORLD ACTUALLY DREW, so the majority is read from the stream
            // rather than assumed from the setting -- the same discipline `Census.Hard`
            // uses, and for the same reason.
            var world = new Multiplexer(settings, seed: 99);
            var drawn = new Dictionary<Code, int>();

            for (var draw = 0; draw < 5_000; draw++)
            {
                var answer = world.Next().Answer;
                drawn[answer] = drawn.GetValueOrDefault(answer) + 1;
            }

            var commonest = drawn.OrderByDescending(one => one.Value).First().Key;

            var roots = brain.Held.All
                .Where(one => one.Scope.Length == 1)
                .GroupBy(one => one.Expects)
                .ToDictionary(one => one.Key, one => one.Count());

            // The whole space one-code genesis can ever reach, per outcome: one rule for
            // each (position, value) pair the world emits.
            var reachable = 2 * (address + (1 << address));

            foreach (var (outcome, share) in drawn.OrderByDescending(one => one.Value))
            {
                var held = roots.GetValueOrDefault(outcome);

                output.WriteLine(
                    $"{address + (1 << address),2} bits skew {skew:F1} | "
                    + $"{(outcome == commonest ? "majority" : "minority"),-8} "
                    + $"| drawn {share / 5000.0,6:P1} "
                    + $"| roots held {held,3}/{reachable}");
            }

            output.WriteLine("");
        }
    }
}
