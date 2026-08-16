using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whether an untested commitment should get a vote — <b>the defect widening surfaced,
/// which was never about widening.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A rule right once weighs one and beats a rule right ninety-five times in a
/// hundred.</b> The accuracy estimate averages over a commitment's opening firings rather
/// than running from zero, which is right — from zero a fresh rule is indistinguishable
/// from a refuted one. But nothing anywhere separates that provisional number from an
/// earned one, and the vote raises it to a power.
/// </para>
/// <para>
/// <b>So every fresh repair child has decided rounds it had no standing to decide, since
/// the branch began.</b> Widening made it loud enough to notice, by putting eighteen
/// hundred untested rules into a population of six hundred, and the defect outlived that
/// operator's deletion because it was never widening's.
/// </para>
/// <para>
/// <b>And silence is the failure mode to watch, which is why it is reported here.</b>
/// Refusing the young a vote can leave a moment with nobody to speak for it, and a
/// population that answers fewer rounds scores well on the ones it does — a fallback
/// arm nobody meant to run. A rise in <c>Silent</c> beside a rise in accuracy is that
/// fault and not a result.
/// </para>
/// </remarks>
public sealed class SpeakingTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    private const int Seeds = 8;

    /// <param name="address">Address bits.</param>
    /// <param name="skew">How often a data bit is one, or zero to leave them even.</param>
    /// <param name="speaking">Whether the untested may vote.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    private static Learned Run(
        int address, double skew, Speaking speaking, int seed) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address, Skew = skew },
            new Brain(new CommittingSettings { Speaking = speaking }, seed),
            seed,
            census: true).Run(Rounds);

    /// <summary>
    /// <b>Whether refusing the untested a vote reaches the rounds guessing misses.</b>
    /// </summary>
    /// <remarks>
    /// <b>It was crossed with widening, and the other axis is gone.</b> That operator was
    /// refuted on its own ship gate and deleted, so the two cells asking whether an
    /// experienced-only vote rescued the flood went with it. What is left is the question
    /// the crossing was only ever half of: whether refusing the untested a vote reaches the
    /// rounds guessing misses at all.
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task Whether_refusing_the_untested_a_vote_reaches_the_hard_rounds()
    {
        var cells = new (string Cell, Speaking Speaking)[]
        {
            ("anyone", Speaking.Anyone),
            ("experienced", Speaking.Experienced),
        };

        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (2, 0.8), (3, 0.8) })
        {
            output.WriteLine($"--- {address + (1 << address)} bits, skew {skew:F1} ---");

            foreach (var (cell, speaking) in cells)
            {
                var once = new Dictionary<int, Learned>();

                Learned Cached(int seed)
                {
                    if (!once.TryGetValue(seed, out var learned))
                        once[seed] = learned = Run(address, skew, speaking, seed);

                    return learned;
                }

                foreach (var reading in new (string What, Func<Learned, double> Of)[]
                {
                    ("paying", one => one.Census!.Paying),
                    ("recent", one => one.Recent),

                    // The explanation and not the result. A gate can help for reasons
                    // that have nothing to do with the story told about it -- fewer
                    // voters is a different population and a different population scores
                    // differently. This is the number that says whether untested rules
                    // were actually deciding wrong rounds, and under `Experienced` it is
                    // nought by construction, which is the point.
                    ("untested", one => one.Census!.Untested),

                    // Beside the score and never under it. A population that refuses to
                    // answer is not a population that answers well.
                    ("silent", one => one.Silent / (double)one.Rounds),
                    ("sound", one => one.Sound),
                    ("unsound", one => one.Unsound),
                    ("residents", one => one.Resident),
                })
                {
                    var arm = await Sweep.ArmAsync(
                        reading.What,
                        Seeds,
                        seed => Task.FromResult(reading.Of(Cached(seed))));

                    output.WriteLine(
                        $"  {cell,-18} {reading.What,-10} | {arm.Mean,10:F3} "
                        + $"+/-{arm.StdErr,8:F3} | n={arm.Seeds}");
                }

                output.WriteLine("");
            }
        }
    }

    /// <summary>
    /// <b>THE ARM CHANGES A DECISION, asserted as a difference and never as a direction.</b>
    /// </summary>
    [Fact]
    public void Refusing_the_untested_a_vote_changes_what_the_machine_answers()
    {
        var anyone = Run(2, skew: 0.0, Speaking.Anyone, seed: 1);
        var earned = Run(2, skew: 0.0, Speaking.Experienced, seed: 1);

        output.WriteLine($"anyone      | recent {anyone.Recent:F4} "
            + $"| silent {anyone.Silent} | residents {anyone.Resident}");
        output.WriteLine($"experienced | recent {earned.Recent:F4} "
            + $"| silent {earned.Silent} | residents {earned.Resident}");

        // Silence is in the disjunction because declining to answer is an answer changed,
        // and it is the only reading this arm is guaranteed to move. Refusing a voter takes
        // its seat away; whether anything ELSE then wins the round is a fact about who was
        // left, and the plan's own revival row says the seat usually passes to another wrong
        // rule. So a check standing on `recent` and `residents` alone reports the gate
        // unwired exactly when the gate is working and the population is rich enough to
        // cover for it -- which is what a bigger repair budget produced, at seven times the
        // silence and not one point of accuracy.
        Assert.True(
            anyone.Recent != earned.Recent
            || anyone.Resident != earned.Resident
            || anyone.Silent != earned.Silent,
            "refusing the untested a vote changed nothing at all, so either every "
            + "commitment that fires is already past the floor or the gate is not wired");
    }
}
