using System.Diagnostics;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What distance costs — <b>the number that decides whether any of this can run over the
/// internet.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>C2 SAYS MESSAGES ARE LATE AND HAS NEVER SAID HOW LATE.</b> The design tolerates
/// lateness by construction, and that has been read as tolerating ANY lateness — which
/// is a claim about correctness being quietly reused as a claim about throughput. A walk
/// hop that costs a microsecond in one process costs a hundred milliseconds between two
/// houses, and a thought is several hops deep.
/// </para>
/// <para>
/// <b>AND THE ANSWER DECIDES THE DEPLOYMENT RATHER THAN A DIAL.</b> Twenty phones on one
/// wifi are a millisecond apart; twenty phones belonging to strangers are a hundred. If
/// the wall clock per round grows with the per-hop delay times the DEPTH of a thought,
/// then the LAN case is fine and the internet case is not, and no amount of tuning
/// changes that — it is the speed of light and the queueing on somebody's router.
/// </para>
/// <para>
/// <b>EVERY DELIVERY IS DELAYED HERE, WHICH IS WHAT A NETWORK DOES.</b> The existing
/// lateness test holds back two percent by a long way, because it is about the case that
/// outlives a thought's patience. This is the other shape: everything, by the same
/// amount, which is distance rather than misfortune.
/// </para>
/// <para>
/// <b>A SWEEP AND NOT A CHECK.</b> There is no bar here — a threshold on somebody's
/// deployment written before the first reading would be a prediction dressed as a
/// requirement. It prints a grid and the grid is the finding.
/// </para>
/// </remarks>
public sealed class LatencyTests(ITestOutputHelper output)
{
    /// <summary>
    /// <b>WALL CLOCK AND ACCURACY AGAINST PER-HOP DELAY.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE CLOCK IS THE READING AND THE ACCURACY COLUMN IS NOT.</b> At sixty rounds
    /// every arm returns the same score to three decimals, which means the learner has
    /// not diverged rather than that lateness is free — a run this short cannot separate
    /// them, and reporting the flat column as <i>latency costs no accuracy</i> would be
    /// a check that cannot fire being read as a pass. <c>SensesTests</c> is where the
    /// accuracy question is asked, over seeds, at a length that can answer it.
    /// </para>
    /// <para>
    /// <b>AND THE SHORT DELAYS ARE MEASURING THE OPERATING SYSTEM'S TIMER.</b>
    /// <see cref="Task.Delay(TimeSpan)"/> cannot resolve below about fifteen
    /// milliseconds on Windows, so the one and five millisecond rows come back at the
    /// same cost as each other and neither is the number it asked for. The rows that
    /// mean anything are the ones well above the floor — and between them the cost per
    /// round is about four and a half times the per-hop delay, which is the DEPTH of a
    /// thought showing up as a multiplier.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task What_a_hop_costs_when_the_machines_are_far_apart()
    {
        var world = new SensesSettings { Concepts = 12, CodesPerSense = 3, Noise = 0.1 };

        foreach (var far in new[] { 0, 1, 5, 25, 100 })
        {
            // EVERY DELIVERY, BECAUSE DISTANCE IS NOT A MISFORTUNE THAT BEFALLS SOME
            // MESSAGES. A share below one measures a network that is mostly local, which
            // is the case already covered.
            var late = far == 0
                ? (Bus.Lateness?)null
                : new Bus.Lateness(Share: 1.0, Delay: TimeSpan.FromMilliseconds(far), Seed: 7);

            var clock = Stopwatch.StartNew();

            using var run = new SensesRun(world, Fixture.Dials(6.0), seed: 1, late: late);
            var got = await run.RunAsync(60, every: 10);

            clock.Stop();

            output.WriteLine(
                $"{far,4} ms a hop | {clock.ElapsedMilliseconds,7} ms for 60 rounds "
                + $"({clock.ElapsedMilliseconds / 60.0,7:F1} ms a round) "
                + $"| accuracy {got.Accuracy:F3}");
        }
    }
}
