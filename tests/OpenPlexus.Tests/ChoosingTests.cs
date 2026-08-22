using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whether repair's CHOICE of condition beats drawing one at random — <b>the plan's own
/// stated kill condition for the bet</b>, on the world where repair fails.
/// </summary>
/// <remarks>
/// <para>
/// <b>The plan has said since the branch began what would end it.</b> <i>A control arm
/// where Z is drawn at random from the codes present in the misses. If discriminative-Z
/// does not beat it, repair does nothing and the bet is dead.</i> Fork 55 answered that on
/// the even multiplexer over twelve seeds and left it open on every other world — and the
/// skewed one did not exist to ask.
/// </para>
/// <para>
/// <b>And every selection rule tried tonight has failed</b>, which is why this is the
/// question now. The budget, two correctness gates, generalisation and an experience
/// bar all change WHICH resident rule gets the seat, and none of them moves the number
/// that matters. Three fifths to all of every failure is a round with no true rule
/// present at all, so the pool is what is wrong rather than the choosing from it — and
/// repair is the only thing that fills the pool.
/// </para>
/// <para>
/// <b>Read on <see cref="Census.Paying"/> and not on children minted.</b> Fork 55 was
/// settled on how many children each arm produced, which says the arms DIFFER and not
/// that either is any good. A random condition also mints children.
/// </para>
/// </remarks>
public sealed class ChoosingTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    private const int Seeds = 8;

    /// <param name="address">Address bits.</param>
    /// <param name="skew">How often a data bit is one, or zero to leave them even.</param>
    /// <param name="choosing">Which rule picks the added condition.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    private static Learned Run(int address, double skew, Choosing choosing, int seed) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address, Skew = skew },
            new Brain(new CommittingSettings { Choosing = choosing }, seed),
            seed,
            census: true).Run(Rounds);

    /// <summary>
    /// <b>Discriminative-z against random-z, on the reading that cannot be gamed.</b>
    /// </summary>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task Whether_choosing_the_condition_beats_drawing_one_at_random()
    {
        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (2, 0.8), (3, 0.8) })
        {
            output.WriteLine($"--- {address + (1 << address)} bits, skew {skew:F1} ---");

            foreach (var choosing in new[] { Choosing.Separating, Choosing.Present })
            {
                var once = new Dictionary<int, Learned>();

                Learned Cached(int seed)
                {
                    if (!once.TryGetValue(seed, out var learned))
                        once[seed] = learned = Run(address, skew, choosing, seed);

                    return learned;
                }

                foreach (var reading in new (string What, Func<Learned, double> Of)[]
                {
                    ("paying", one => one.Census!.Paying),
                    ("recent", one => one.Recent),
                    ("found", one => one.Found),
                    ("sound", one => one.Sound),
                    ("unsound", one => one.Unsound),
                    ("repaired", one => one.Tally.Repaired),
                })
                {
                    await Fixture.ReadAsync(
                        output, choosing.ToString(), Seeds, Cached, reading);
                }

                output.WriteLine("");
            }
        }

        output.WriteLine("the plan's rule: if choosing does not beat drawing at random, "
            + "repair does nothing and the bet is dead");
    }
}
