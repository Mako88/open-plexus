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
        Weighing weighing = Weighing.Summing) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address, Noise = noise },
            new Brain(
                new CommittingSettings { Sharpness = sharpness, Weighing = weighing }, seed),
            seed).Run(Rounds);

    [Fact]
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

            foreach (var weighing in new[] { Weighing.Summing, Weighing.Strongest })
            foreach (var mending in new[] { Mending.Outvoted, Mending.Uncovered, Mending.Neglected })
            {
                var recent = new List<double>();
                var sound = new List<double>();
                var found = new List<double>();
                var repaired = new List<double>();
                var resident = new List<double>();

                foreach (var seed in new[] { 1, 2, 3 })
                {
                    var learned = new MultiplexerRun(
                        new MultiplexerSettings { Address = address, Noise = noise },
                        new Brain(
                            new CommittingSettings { Weighing = weighing, Mending = mending },
                            seed),
                        seed).Run(Rounds);

                    recent.Add(learned.Recent);
                    sound.Add(learned.Sound);
                    found.Add(learned.Found);
                    repaired.Add(learned.Repaired);
                    resident.Add(learned.Resident);
                }

                output.WriteLine(
                    $"  {weighing,-9} {mending,-8} | recent {recent.Average():F3} "
                    + $"[{string.Join(" ", recent.Select(one => one.ToString("F3")))}] | "
                    + $"sound {sound.Average():F0} resident {resident.Average():F0} | "
                    + $"repaired {repaired.Average():F0} | of the key: {found.Average():F1}");
            }
        }

        Assert.True(true);
    }

    [Fact]
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
