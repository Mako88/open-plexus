using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The same dial, on the world step one is judged on.
/// </summary>
/// <remarks>
/// <para>
/// <b>A FINDING ON ONE WORLD IS A FINDING ABOUT ONE WORLD, AND THIS REPO HAS PAID FOR
/// FORGETTING THAT.</b> A dial wired to one world in ten was cashed in here as though it
/// were general, and it is on the traps list because of it. So if the vote's power is
/// what holds the arranged world short of its target, the multiplexer is where that
/// stops being a fact about a grid of glyphs.
/// </para>
/// <para>
/// <b>AND IT HAS THE SHARPER INSTRUMENT OF THE TWO.</b> Soundness here is settled by
/// enumerating the assignments a scope leaves open, so a rule can be asked whether it is
/// TRUE rather than whether it agrees with a basis somebody chose. The open defect this
/// aims at is written in exactly those terms: MORE OF WHAT IT HOLDS IS UNSOUND THAN
/// SOUND, while the score holds — is the vote robust to them, or are they why it stops
/// short?
/// </para>
/// </remarks>
public sealed class SharpeningTests(ITestOutputHelper output)
{
    private const int Rounds = 30_000;

    private static Learned Run(
        int address,
        double sharpness,
        int seed,
        double noise = 0.0,
        Weighing weighing = Weighing.Summing,
        int rounds = Rounds) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address, Noise = noise },
            new Brain(
                new CommittingSettings { Sharpness = sharpness, Weighing = weighing }, seed),
            seed).Run(rounds);

    /// <summary>Everything a run did, other than how confident it said it was.</summary>
    /// <param name="learned">What the run reported.</param>
    /// <remarks>
    /// <b>SPELLED OUT RATHER THAN COMPARED AS A RECORD, BECAUSE THE ONE EXCLUSION IS THE
    /// FINDING.</b> <c>Tally.Confidence</c> is a lead divided by a weight and both are
    /// accuracies raised to the power under test, so it moves by construction; every other
    /// number here is something the machine DID. A record equality would fail on the one
    /// field that is supposed to differ and say nothing about the rest.
    /// </remarks>
    private static object Did(Learned learned) => new
    {
        learned.Recent,
        learned.Sound,
        learned.Unsound,
        learned.Unchecked,
        learned.Found,
        learned.Resident,
        learned.Silent,
        learned.Reached,
        learned.Repaired,
        learned.Named,
        learned.Stacked,
        learned.Exhausted,
    };

    /// <summary>
    /// <b>UNDER <see cref="Weighing.Strongest"/> THE VOTE'S POWER DECIDES NOTHING AT ALL,
    /// AND UNDER A SUM IT DECIDES A GREAT DEAL.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE ARGUMENT IS ONE LINE AND THE CHECK IS HERE BECAUSE THE PLAN ACTED ON THE
    /// OTHER READING.</b> Under <c>Strongest</c> an expectation is worth
    /// <c>max(a_i)^S</c>, and <c>x^S</c> is strictly increasing on the unit interval — so
    /// <c>max(a_i^S) = (max a_i)^S</c> and the ORDER of expectations by weight is the order
    /// by best accuracy, whatever S is. The winner, its best advocate, and therefore every
    /// hit, miss, repair and mint are identical.
    /// </para>
    /// <para>
    /// <b>WHICH MAKES `Sharpness` A DIAL OF <c>Summing</c> RATHER THAN A DIAL OF THE
    /// BRAIN.</b> The plan's open defect says three dials in a row have a best value that
    /// moves with the world — <c>Sharpness</c>, <c>Weighing</c> and <c>Mending</c> — and
    /// two of those three are not independent axes: one of them switches the other off. A
    /// grid swept over both has a whole column in which nothing varies.
    /// </para>
    /// <para>
    /// <b>AND `DialTests` HAS THE NEAR-MISS WRITTEN DOWN, WHICH IS WHY THIS IS A CHECK AND
    /// NOT A REMARK.</b> It records <c>Strongest</c> as <i>structurally the limit of high
    /// sharpness</i>, which is true of <c>Summing</c> as S grows and is a weaker claim than
    /// the one above: at the limit a dial stops MATTERING MUCH, and here it stops existing.
    /// </para>
    /// <para>
    /// <b>THE SUM IS THE CONTROL AND IT IS THE HALF THAT CAN FAIL.</b> Three identical runs
    /// would also be what an unwired dial produces, and this repo has a trap for exactly
    /// that — a check can be wired and unable to fire. If the power moves nothing under a
    /// sum either, it is connected to nothing and the first half means nothing.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_votes_power_decides_nothing_under_Strongest_and_the_sum_is_the_control()
    {
        // SHORTER THAN THE SWEEPS ABOVE ON PURPOSE. This asserts an ALGEBRAIC identity, and
        // a longer run cannot make it more true -- what a longer run would buy is a bigger
        // population for the sum to disagree over, and the control below already separates.
        const int Short = 8000;

        var powers = new[] { 1.0, 5.0, 20.0 };

        foreach (var address in new[] { 2, 3 })
        {
            var strongest = powers
                .Select(power => Run(address, power, seed: 1,
                    weighing: Weighing.Strongest, rounds: Short))
                .ToList();

            foreach (var one in strongest)
                Assert.Equal(Did(strongest[0]), Did(one));

            // AND THE READOUT DOES MOVE, which is what says the dial reached the vote at
            // all rather than being dropped on the floor before it. If this were equal too,
            // the equality above would be the trivial kind.
            Assert.True(
                strongest.Select(one => one.Tally.Confidence).Distinct().Count() > 1,
                "the confidence is identical at every power, so `Sharpness` is not reaching "
                + "the vote and the identity above is being asserted of nothing");

            var summing = powers
                .Select(power => Run(address, power, seed: 1,
                    weighing: Weighing.Summing, rounds: Short))
                .ToList();

            Assert.True(
                summing.Select(Did).Distinct().Count() > 1,
                $"at {(1 << address) + address} bits the power changes nothing under a sum "
                + "either, so this file's control cannot fire");

            output.WriteLine(
                $"{(1 << address) + address} bits | strongest: "
                + $"{strongest.Select(Did).Distinct().Count()} distinct run of "
                + $"{powers.Length}, confidence "
                + $"{string.Join(" ", strongest.Select(one => one.Tally.Confidence.ToString("F3")))}"
                + $" | summing: {summing.Select(Did).Distinct().Count()} distinct, recent "
                + $"{string.Join(" ", summing.Select(one => one.Recent.ToString("F3")))}");
        }
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_a_sharper_vote_is_worth_anything_where_the_rules_can_be_checked()
    {
        foreach (var address in new[] { 2, 3 })
        {
            output.WriteLine($"{(1 << address) + address} bits:");

            foreach (var sharpness in new[] { 1.0, 5.0, 20.0 })
            {
                var recent = new List<double>();
                var sound = new List<double>();
                var unsound = new List<double>();
                var repaired = new List<double>();
                var found = new List<double>();
                var lead = new List<double>();

                foreach (var seed in new[] { 1, 2, 3 })
                {
                    var learned = Run(address, sharpness, seed);

                    recent.Add(learned.Recent);
                    sound.Add(learned.Sound);
                    unsound.Add(learned.Unsound);
                    repaired.Add(learned.Repaired);
                    found.Add(learned.Found);
                    lead.Add(learned.Tally.Confidence);
                }

                // REPAIR IS REPORTED BESIDE THE SCORE BECAUSE THE VOTE STEERS IT. Blame
                // ranks the provenance that arrived, and which commitments are in a
                // prediction's provenance is decided by who won -- so a sharper vote
                // does not merely read the population differently, it changes which
                // commitment is specialised next. On a world whose true rules take
                // three codes to say, that is the search and not the readout.
                output.WriteLine(
                    $"  sharpness {sharpness,4} | recent {recent.Average():F3} "
                    + $"[{string.Join(" ", recent.Select(one => one.ToString("F3")))}] | "
                    + $"sound {sound.Average():F0} unsound {unsound.Average():F0} | "
                    + $"repaired {repaired.Average():F0} | "
                    + $"lead {lead.Average():F3} | "
                    + $"of the key: {found.Average():F1}");
            }
        }

        // NO BAR. What this is for is saying whether the dial does the same thing on two
        // worlds; a threshold written before the first run would be a prediction
        // dressed as a check, and five is the value every number in this repo was
        // measured under.
        Assert.True(true);
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void An_expectation_worth_its_best_advocate_rather_than_all_of_them()
    {
        // THE POINT OF THE ARM IS THAT IT NEEDS NO PER-WORLD NUMBER. A sum over N
        // advocates scales with N however steeply each is weighted, so `Sharpness` only
        // ever makes a crowd need more members -- it never takes the count out of the
        // decision, and the peak therefore moves with the world. A maximum is
        // scale-free: a thousand mediocre rules cannot outvote one that is always right
        // at any power at all.
        //
        // AND A DIAL WITH A PER-WORLD OPTIMUM IS A WORLD REACHING INTO THE BRAIN BY THE
        // BACK DOOR, which is the one thing this design says it will not have. So the
        // answer cannot be to tune the power; it has to be a vote whose shape does not
        // need tuning, or an argument for why one number is right everywhere.
        //
        // WHAT IT MIGHT COST IS THE AVERAGING A CROWD BUYS, and the noisy rows are
        // where that would show. Measured here rather than reasoned about, because the
        // last prediction about noise in this file was wrong in the direction nobody
        // guessed.
        foreach (var (address, noise) in new[] { (2, 0.0), (3, 0.0), (2, 0.15) })
        {
            output.WriteLine($"{(1 << address) + address} bits, noise {noise:F2}:");

            foreach (var weighing in new[] { Weighing.Summing, Weighing.Strongest })
            {
                var recent = new List<double>();
                var sound = new List<double>();
                var found = new List<double>();
                var repaired = new List<double>();
                var lead = new List<double>();

                foreach (var seed in new[] { 1, 2, 3 })
                {
                    var learned = Run(address, sharpness: 5.0, seed, noise, weighing);

                    recent.Add(learned.Recent);
                    sound.Add(learned.Sound);
                    found.Add(learned.Found);
                    repaired.Add(learned.Repaired);
                    lead.Add(learned.Tally.Confidence);
                }

                output.WriteLine(
                    $"  {weighing,-9} | recent {recent.Average():F3} "
                    + $"[{string.Join(" ", recent.Select(one => one.ToString("F3")))}] | "
                    + $"sound {sound.Average():F0} | repaired {repaired.Average():F0} | "
                    + $"lead {lead.Average():F3} | of the key: {found.Average():F1}");
            }
        }

        Assert.True(true);
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Repair_that_waits_for_a_commitment_to_be_wrong_rather_than_the_vote()
    {
        // THE INCONSISTENCY, ARGUED FROM THE PLAN'S OWN TEXT BEFORE IT WAS MEASURED. An
        // outvoted commitment still accrues its own hits and misses -- and then could
        // never spend them, because repair ran only on a round the WINNER lost. So a
        // commitment that fired, was wrong, and was outvoted banked a miss it could not
        // act on, and how hard the machine searched became a function of how good its
        // answers already were.
        //
        // AND THAT IS WHY NO SINGLE `Sharpness` SERVES BOTH WORLDS. Concentrating the
        // vote is the right readout everywhere measured and the wrong search on a world
        // whose true rules take three codes to say. Decoupling them is the only move
        // that could give both.
        //
        // THE PREDICTION: `Strongest` with `Earned` keeps 1.000 on `Arranged` AND
        // recovers the clean multiplexer toward what a plain sum gets. If it does not,
        // the coupling was not the cause and the extra gate was load-bearing for a
        // reason nobody has named.
        foreach (var (address, noise) in new[] { (2, 0.0), (3, 0.0), (2, 0.15) })
        {
            output.WriteLine($"{(1 << address) + address} bits, noise {noise:F2}:");

            foreach (var weighing in new[] { Weighing.Strongest })
            foreach (var mending in new[] { Mending.Outvoted, Mending.Uncovered, Mending.Improving })
            foreach (var subsuming in new[] { Subsuming.Weaker, Subsuming.Insignificant })
            {
                var recent = new List<double>();
                var sound = new List<double>();
                var found = new List<double>();
                var repaired = new List<double>();
                var resident = new List<double>();
                var subsumed = new List<double>();
                var occasions = new List<double>();

                foreach (var seed in new[] { 1, 2, 3 })
                {
                    var learned = new MultiplexerRun(
                        new MultiplexerSettings { Address = address, Noise = noise },
                        new Brain(
                            new CommittingSettings
                            {
                                Weighing = weighing,
                                Mending = mending,
                                Subsuming = subsuming,
                            },
                            seed),
                        seed).Run(Rounds);

                    recent.Add(learned.Recent);
                    sound.Add(learned.Sound);
                    found.Add(learned.Found);
                    repaired.Add(learned.Repaired);
                    resident.Add(learned.Resident);
                    subsumed.Add(learned.Tally.Subsumed);
                    occasions.Add(learned.Tally.Occasions);
                }

                output.WriteLine(
                    $"  {mending,-9} {subsuming,-13} | recent {recent.Average():F3} "
                    + $"[{string.Join(" ", recent.Select(one => one.ToString("F3")))}] | "
                    + $"sound {sound.Average():F0} resident {resident.Average():F0} | "
                    + $"repaired {repaired.Average():F0} subsumed {subsumed.Average():F0} | "
                    + $"occasions {occasions.Average():F1} | of the key: {found.Average():F1}");
            }
        }

        Assert.True(true);
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Where_a_run_actually_spends_itself()
    {
        // NOTHING HAS EVER MEASURED THIS, WHICH IS THE POINT OF THE FACT. A CIFAR run was
        // memory-bound and every instrument on it watched the clock; the traps list
        // records that as a cost being invisible to all of them. The truth was worse --
        // nothing watched EITHER, so a run spending its life in a quadratic sweep and one
        // spending it in the per-code tally report the same everything.
        //
        // AND THE READING THAT MATTERS IS WHICH PHASE, NEVER THE TOTAL. Matching, the
        // tally and the sweep want three completely different fixes, and picking one
        // before this existed would have been picking by taste.
        //
        // `Separations` IS THE OTHER HALF AND IT IS THE REPRODUCIBLE ONE. Entries rather
        // than bytes, because a heap figure moves with collection timing and could never
        // be barred; this cannot drift with the machine, so a budget on it would hold.
        foreach (var address in new[] { 2, 3 })
        foreach (var mending in new[] { Mending.Outvoted, Mending.Uncovered })
        {
            var learned = new MultiplexerRun(
                new MultiplexerSettings { Address = address },
                new Brain(new CommittingSettings { Mending = mending }, seed: 1),
                seed: 1).Run(Rounds);

            var spent = learned.Tally.Spent;

            output.WriteLine(
                $"{(1 << address) + address,2} bits {mending,-9} | "
                + $"resident {learned.Resident,5} separations {learned.Tally.Separations,8} "
                + $"({learned.Tally.Separations / (double)Math.Max(learned.Resident, 1):F0} a rule) | "
                + $"firing {spent.Firing,8:F0} settling {spent.Settling,8:F0} "
                + $"sweeping {spent.Sweeping,8:F0} covering {spent.Covering,7:F0} "
                + $"mending {spent.Mending,8:F0} ms");
        }

        // NO BAR ON EITHER, AND FOR TWO DIFFERENT REASONS. A duration is not reproducible
        // under a fixed seed and a threshold on one would fail the build on a busy
        // machine. `Separations` COULD be barred and is not yet, because a threshold
        // written before the first run is a prediction dressed as a check.
        Assert.True(true);
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void And_whether_a_sharp_vote_is_only_sharp_because_nothing_lies_to_it()
    {
        // THE WAY THE RESULT COULD BE A TRAP, TESTED RATHER THAN CAVEATED. Raising the
        // power concentrates the vote on whatever looks most accurate, which is exactly
        // right when a perfect record means a true rule -- and exactly wrong when it
        // can mean a lucky one. On a clean world those are the same thing. Noise is the
        // only place they come apart, and this repo already has one dial that turned
        // out to be a level with an interior optimum rather than a guard.
        //
        // SO THE CLAIM WORTH MAKING IS NOT "SHARPER IS BETTER". It is whether the peak
        // MOVES with the noise, which is a different shape of finding and is the one
        // that would stop somebody raising the default and quietly losing Monk-3.
        foreach (var noise in new[] { 0.0, 0.05, 0.15 })
        {
            output.WriteLine($"noise {noise:F2} (a perfect learner scores {1.0 - noise:F2}):");

            foreach (var sharpness in new[] { 5.0, 10.0, 20.0 })
            {
                var recent = new List<double>();
                var sound = new List<double>();
                var unsound = new List<double>();

                foreach (var seed in new[] { 1, 2, 3 })
                {
                    var learned = Run(address: 2, sharpness, seed, noise);

                    recent.Add(learned.Recent);
                    sound.Add(learned.Sound);
                    unsound.Add(learned.Unsound);
                }

                output.WriteLine(
                    $"  sharpness {sharpness,4} | recent {recent.Average():F3} "
                    + $"[{string.Join(" ", recent.Select(one => one.ToString("F3")))}] | "
                    + $"sound {sound.Average():F0} unsound {unsound.Average():F0}");
            }
        }

        Assert.True(true);
    }
}
