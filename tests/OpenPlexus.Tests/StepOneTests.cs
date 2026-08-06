using OpenPlexus.Machines;
using OpenPlexus.Commitments;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Step one, end to end, on the world it is judged on.
/// </summary>
/// <remarks>
/// <b>THE ARM IS THE POINT OF THIS FILE.</b> A learner that specialises will get
/// better at almost anything, so a score on its own says nothing about whether
/// CHOOSING the condition did any work. Every claim here is made against the arm that
/// adds a condition drawn at random from the ones present in the failures.
/// </remarks>
public sealed class StepOneTests(ITestOutputHelper output)
{
    private const int Rounds = 30000;

    private static Learned Run(int address, Choosing choosing, int seed) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address },
            new Brain(new CommittingSettings { Choosing = choosing }, seed),
            seed).Run(Rounds);

    [Fact]
    public void It_learns_the_six_bit_multiplexer()
    {
        var learned = Run(address: 2, choosing: Choosing.Separating, seed: 1);

        output.WriteLine(
            $"recent={learned.Recent:F3} resident={learned.Resident} "
            + $"sound={learned.Sound} unsound={learned.Unsound} found={learned.Found}");

        Assert.True(learned.Recent > 0.97, $"only {learned.Recent:F3} over the last tenth");

        // AN ACCURACY CAN BE REACHED BY MEMORISING, so the count goes beside it. The
        // world's own basis holds eight rules; a learner at ten thousand commitments
        // has not found the structure whatever it scores.
        Assert.True(learned.Resident < 100, $"{learned.Resident} commitments resident");

        // AND SOUNDNESS IS THE NUMBER THIS IS ACTUALLY JUDGED ON -- rules that are
        // TRUE of the world, checked by enumeration rather than against one basis.
        //
        // A RAW COUNT IS THE WRONG BAR AND WAS SET AT ONE FOR A WHILE. Subsumption
        // compresses, so a population that got BETTER holds fewer sound rules than
        // one that never dropped a redundant specific -- and the bar then punishes
        // exactly the mechanism it was meant to reward. The share is what survives
        // compression.
        Assert.True(learned.Sound > 10, $"only {learned.Sound} sound commitments");

        Assert.True(
            learned.Sound / (double)(learned.Sound + learned.Unsound) > 0.3,
            $"{learned.Sound} sound against {learned.Unsound} not");

        // SILENCE IS A CONTROL ARM NOBODY MEANT TO RUN. One round, before anything
        // has been minted, is the whole of it.
        Assert.True(learned.Silent <= 1, $"silent on {learned.Silent} rounds");
    }

    [Fact]
    public void Choosing_the_condition_beats_choosing_any_condition_present()
    {
        // THE SINGLE MOST IMPORTANT NUMBER IN STEP ONE. If discriminative-Z does not
        // beat random-Z, repair is doing nothing and the bet is dead -- every other
        // assertion in this file would be measuring the narrowing that ANY added
        // condition buys.
        //
        // FIVE SEEDS AND COUNTED IN BOTH DIRECTIONS, because a small sample can look
        // like a mechanism and can hide a real effect just as easily.
        var beaten = 0;

        foreach (var seed in new[] { 1, 2, 3, 4, 5 })
        {
            var gated = Run(address: 2, choosing: Choosing.Separating, seed);
            var blind = Run(address: 2, choosing: Choosing.Present, seed);

            output.WriteLine(
                $"seed={seed} gated={gated.Recent:F3}/{gated.Sound} "
                + $"blind={blind.Recent:F3}/{blind.Sound}");

            if (gated.Recent > blind.Recent) beaten++;

            // AND THE SHARPER STATEMENT: the blind arm never learns a rule that is
            // TRUE. It is not merely behind on score -- it holds nothing sound at
            // all, at any budget, while minting far more than the gated arm does.
            Assert.Equal(0, blind.Sound);
        }

        Assert.Equal(5, beaten);
    }

    [Fact]
    public void The_arm_that_cannot_choose_overfits_instead()
    {
        // WHAT AN UNGATED REPAIR ACTUALLY DOES, rather than what it fails to do. It
        // specialises without limit: more children, more residents, and nothing true
        // to show for either. This is ILP's cause of death reproduced on purpose.
        var gated = Run(address: 3, choosing: Choosing.Separating, seed: 1);
        var blind = Run(address: 3, choosing: Choosing.Present, seed: 1);

        output.WriteLine(
            $"gated resident={gated.Resident} repaired={gated.Repaired} sound={gated.Sound}");
        output.WriteLine(
            $"blind resident={blind.Resident} repaired={blind.Repaired} sound={blind.Sound}");

        Assert.True(blind.Repaired > gated.Repaired * 5,
            $"the blind arm minted {blind.Repaired} against {gated.Repaired}");

        Assert.Equal(0, blind.Sound);
        Assert.True(gated.Sound > 20, $"only {gated.Sound} sound commitments");
    }

    [Fact]
    public void Eleven_bits_is_reported_and_carries_no_bar()
    {
        // THE SCALING NUMBER. What predicts whether any of this reaches perception is
        // how the cost grows with the number of relevant bits, and a bar here would
        // only encourage tuning against it before that curve is known.
        var six = Run(address: 2, choosing: Choosing.Separating, seed: 1);
        var eleven = Run(address: 3, choosing: Choosing.Separating, seed: 1);

        output.WriteLine($"six  recent={six.Recent:F3} sound={six.Sound} resident={six.Resident}");
        output.WriteLine(
            $"eleven recent={eleven.Recent:F3} sound={eleven.Sound} resident={eleven.Resident}");

        Assert.True(eleven.Sound > 0, "eleven bits learnt nothing true at all");
    }

    [Fact]
    public void A_fixed_seed_reproduces_a_run_exactly()
    {
        // FORK 12, WHICH THIS PROJECT HAS ALREADY REOPENED TWICE. Repair decides from
        // a dictionary of tallies and the population is walked every round, so an
        // unstable iteration order would move the learned structure without anything
        // failing.
        Assert.Equal(Run(address: 2, choosing: Choosing.Separating, seed: 8), Run(address: 2, choosing: Choosing.Separating, seed: 8));
        Assert.Equal(Run(address: 2, choosing: Choosing.Present, seed: 8), Run(address: 2, choosing: Choosing.Present, seed: 8));

        Assert.NotEqual(Run(address: 2, choosing: Choosing.Separating, seed: 8), Run(address: 2, choosing: Choosing.Separating, seed: 9));
    }
}
