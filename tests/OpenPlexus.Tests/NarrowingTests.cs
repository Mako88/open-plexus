using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What a condition BUYS against what it COSTS, rung by rung — <b>fork 68, and the
/// instrument before the argument.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Repair tests whether the added code separates and never whether it was worth
/// adding.</b> The gate runs a two-proportion test on Z's rate in the misses against its
/// rate in the hits, corrected for the candidates considered — so <i>is this a real
/// discriminator</i> is answered and <i>did the rule pay too much for it</i> is not asked
/// anywhere. Every condition roughly halves where a commitment can fire, and nothing reads
/// that half.
/// </para>
/// <para>
/// <b>So this reports the two numbers together and asserts nothing.</b> Accuracy by rung is
/// the gain and firings by rung is the price, and a criterion weighing one against the other
/// would need no per-world constant — which is what <see cref="CommittingSettings.Budget"/>
/// is at every value, including the one that ships. A grid first, because this repo's own
/// rule is that the instrument comes before the seventh story.
/// </para>
/// <para>
/// <b>And the prediction is written down before the run.</b> If each rung costs about half
/// the reach for a small accuracy gain, deep children are poor value and a gain-against-reach
/// rule would prune them — which is the same conclusion subsumption reaches by a different
/// road and would explain why it is the population's main exit. If accuracy climbs steeply
/// with depth, the narrowing is earned and fork 68 is answered no.
/// </para>
/// <para>
/// <b>The mean is over commitments and not over firings, which matters at depth.</b> A rung
/// holding many rules that each fire twice would otherwise read as a rung nobody uses, when
/// what it is is a rung spread thin — and those want opposite conclusions.
/// </para>
/// </remarks>
public sealed class NarrowingTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    /// <summary><b>Named `Runs` because `Seeds` is a TYPE here</b> — see `Seeds.Apart`.</summary>
    private const int Runs = 8;

    /// <summary>Fixed forever, and deliberately not any other sweep's word.</summary>
    private const uint Purpose = 0x5EED_0068;

    /// <summary>
    /// <b>What each rung gains in accuracy and spends in reach.</b>
    /// </summary>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_condition_buys_against_what_it_costs()
    {
        foreach (var (address, skew) in new[] { (3, 0.8), (3, 0.0), (2, 0.0) })
        {
            output.WriteLine($"=== {address + (1 << address)} bits, skew {skew:F1}, "
                + $"{Runs} seeds, {Rounds} rounds ===");
            output.WriteLine(
                $"{"rung",4} {"rules",8} {"accuracy",10} {"firings",12} {"reach kept",12}");

            // One run a seed, and every rung read off the same population. Asking per rung
            // would run the identical configuration once a rung and report one measurement
            // as several -- the discipline `BudgetTests` records.
            var byRung = new Dictionary<int, (List<double> Accuracy, List<double> Firings, List<int> Rules)>();

            for (var index = 1; index <= Runs; index++)
            {
                var seed = Seeds.Apart(index, Purpose);

                var brain = new Brain(new CommittingSettings(), seed);

                new MultiplexerRun(
                    new MultiplexerSettings { Address = address, Skew = skew },
                    brain,
                    seed).Run(Rounds);

                // SPELLED BACK OUT, because a minted name hides how long a scope really is.
                // A rung counted on the folded scope would put a named pair at depth one and
                // read the deepest rules as the shallowest.
                foreach (var group in brain.Held.All
                    .Where(one => one.Seen > 0)
                    .GroupBy(one => brain.Held.Names.Unfold(one.Scope).Length))
                {
                    if (!byRung.TryGetValue(group.Key, out var kept))
                        byRung[group.Key] = kept = ([], [], []);

                    kept.Accuracy.Add(group.Average(one => one.Accuracy));
                    kept.Firings.Add(group.Average(one => (double)one.Seen));
                    kept.Rules.Add(group.Count());
                }
            }

            var previous = 0.0;

            foreach (var rung in byRung.Keys.Order())
            {
                var (accuracy, firings, rules) = byRung[rung];

                var reach = firings.Average();

                // What the rung below still reaches, which is the price in its own terms. A
                // firing count falls with depth for two unrelated reasons -- a longer scope
                // matches less, and a younger rule has had less time -- and this separates
                // neither. It is the ratio the fork is about; the confound is named here so
                // nobody reads it as clean.
                var kept_ = previous == 0.0 ? 1.0 : reach / previous;

                output.WriteLine(
                    $"{rung,4} {rules.Average(),8:F1} {accuracy.Average(),10:F3} "
                    + $"{reach,12:F1} {kept_,12:F3}");

                previous = reach;
            }

            output.WriteLine("");
        }
    }

    /// <summary>
    /// <b>The overshoot reading fires, and it is not the same number at both widths.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A column that is always nought is a check that cannot fire</b>, which this repo
    /// has now found twice by arming something that had always read zero. So the instrument
    /// is asserted to have a subject before any row above is read as a comparison.
    /// </para>
    /// <para>
    /// <b>It arrived here with the widening deletion</b>, having been written for the grid
    /// that priced an operator making scopes shorter. That operator is gone and the reading
    /// is not: a sound rule containing a shorter sound rule is fork 68's question asked of
    /// the population rather than of a rung, and fork 75 wants the same number.
    /// </para>
    /// <para>
    /// <b>And it is one seed a width on purpose, because this asks whether the number
    /// EXISTS and not how big it is.</b> How much a population overshoots is the grid's
    /// question and carries error bars there.
    /// </para>
    /// <para>
    /// <b>What it would mean for the two to be equal is worth saying first.</b> Repair
    /// refuses a parent under <c>Floor</c> misses and a sound rule on a clean world never
    /// misses, so a chain that only ever narrows what is still wrong cannot pass a sound
    /// depth by that route — it can only arrive at one along a lineage that was never sound.
    /// A width where this reads nought is a width where every route was short enough for
    /// that to be the whole story.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_overshoot_reading_has_a_subject_before_any_row_is_read()
    {
        Learned Held(int address) =>
            new MultiplexerRun(
                new MultiplexerSettings { Address = address, Skew = 0.0 },
                new Brain(new CommittingSettings(), seed: 1),
                seed: 1,
                census: true).Run(Rounds);

        var narrow = Held(2);
        var wide = Held(3);

        output.WriteLine(
            $"6 bits: {narrow.Overshot} of {narrow.Sound} sound over-specialised");
        output.WriteLine(
            $"11 bits: {wide.Overshot} of {wide.Sound} sound over-specialised");

        Assert.True(wide.Overshot > 0,
            "no sound commitment at eleven bits contains a shorter sound one, so either the "
            + "chain always stops at a minimum or the reading is not wired");

        Assert.NotEqual(narrow.Overshot, wide.Overshot);
    }
}
