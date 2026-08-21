using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What background costs, and the invariant that keeps it out — <b>fork 51.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A code live in every moment separates nothing</b>, and repair already knows that.
/// <see cref="Conditions.Divergence"/> asks how often a code was present in hits against in
/// misses; present in all of both gives one and one, so its separation is nought and it
/// can never be chosen as a condition. The mechanism John asked after is, for repair,
/// already there.
/// </para>
/// <para>
/// <b>Genesis had no such guard and now does</b>, which is what the grid below is against.
/// It used to mint one commitment per live code on a surprise, so background became a
/// ROOT and every child hanging off it inherited a code that could never earn its place —
/// half the resident population, on eight bits of it. <see cref="Population.Genesis"/>
/// refuses to root on a code that has never once been absent.
/// </para>
/// <para>
/// <b>And the tally still has no guard</b>, which is the half of fork 51 left open.
/// <see cref="Commitment.Settle"/> stores an entry per non-scope code in every moment a
/// commitment fires on, so an always-on code is an entry in EVERY commitment's table with
/// a divergence pinned at nought for the life of the run. The grid still shows the table
/// roughly doubling under background, and that is what remains.
/// </para>
/// </remarks>
public sealed class ClutterCostTests(ITestOutputHelper output)
{
    private static Learned Run(int clutter) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = 2, Clutter = clutter },
            new Brain(new CommittingSettings(), seed: 1),
            seed: 1).Run(20_000);

    /// <summary>
    /// <b>The cost of background, in the three places it could be.</b>
    /// </summary>
    /// <remarks>
    /// No bar. What the numbers say is which of minting, the table, or the score moves
    /// with background — and a threshold written before the first reading is a prediction
    /// dressed as a check.
    /// </remarks>
    [Fact]
    public void What_a_code_that_is_always_there_costs_the_run()
    {
        var arms = Fixture.Abreast(
            () => Run(0), () => Run(2), () => Run(4), () => Run(8));

        foreach (var (clutter, got) in new[] { 0, 2, 4, 8 }.Zip(arms))
            output.WriteLine(
                $"clutter {clutter,2} | recent {got.Recent:F3} · minted {got.Tally.Minted,6} "
                + $"· resident {got.Resident,4} · separations {got.Tally.Separations,7} "
                + $"· repaired {got.Repaired,5} · sound {got.Sound,3}");

        // The one thing that must hold whatever the cost is: background carries no
        // information, so it cannot make a rule true that was not, nor false that was.
        // A soundness count moving with clutter would mean the answer key had started
        // reading the noise, which is a bug in the instrument rather than a finding.
        Assert.All(arms, one => Assert.Equal(0, one.Unchecked));
    }

    /// <summary>
    /// <b>Nothing is ever rooted on background</b>, which is the invariant and not a
    /// score.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is what the gate is named for, asked directly.</b> Before it, half the
    /// resident population at eight bits of background carried an always-present code, and
    /// three quarters of those were otherwise perfectly good rules dragging one they had
    /// inherited from a parent that should never have existed. A mechanism that improved
    /// a score while leaving them resident would be improving it for some other reason,
    /// and the number would be believed anyway.
    /// </para>
    /// <para>
    /// <b>And the count is nought rather than small.</b> A code that has never been absent
    /// cannot be a root, and a scope only grows by conditions repair chooses — which can
    /// never be background, since its separation is nought by construction. So there is no
    /// road by which one gets in, and anything above nought means a road exists that this
    /// file does not know about.
    /// </para>
    /// </remarks>
    [Fact]
    public void Nothing_is_ever_rooted_on_a_code_that_has_not_varied()
    {
        var run = new MultiplexerRun(
            new MultiplexerSettings { Address = 2, Clutter = 8 },
            new Brain(new CommittingSettings(), seed: 1),
            seed: 1);

        run.Run(20_000);

        // THE BACKGROUND CODES THEMSELVES: positions past the informative ones, always one.
        var background = Enumerable.Range(6, 8)
            .Select(at => Codes.Bits.Of(Multiplexer.Bit, at, 1))
            .ToHashSet();

        var resident = run.Held.All
            .Select(one => run.Held.Names.Unfold(one.Scope))
            .ToList();

        var tainted = resident.Count(scope => scope.Any(background.Contains));

        output.WriteLine(
            $"{tainted} of {resident.Count} resident commitments carry a code that is "
            + "always present");

        // And the population is not empty, so a nought above is the gate working rather
        // than the run having learnt nothing at all -- which would satisfy the assertion
        // for the wrong reason and is exactly the shape of a check that cannot fire.
        Assert.NotEmpty(resident);

        Assert.Equal(0, tainted);
    }

    /// <summary>
    /// <b>And the world is unchanged underneath it</b>, which is what makes the grid
    /// readable.
    /// </summary>
    /// <remarks>
    /// Clutter takes nothing from the generator and the answer function ignores it, so
    /// the sequence of informative assignments is identical at every setting. Any
    /// difference above is the LEARNER meeting background, never the world having become
    /// a different world — which is the confound that would otherwise make every row
    /// incomparable.
    /// </remarks>
    [Fact]
    public void Background_changes_nothing_about_what_the_world_asks()
    {
        var plain = new Multiplexer(new MultiplexerSettings { Address = 2 }, seed: 7);
        var cluttered = new Multiplexer(
            new MultiplexerSettings { Address = 2, Clutter = 8 }, seed: 7);

        for (var round = 0; round < 2_000; round++)
        {
            var bare = plain.Next();
            var noisy = cluttered.Next();

            Assert.Equal(bare.Answer, noisy.Answer);
            Assert.Equal(bare.Outcome, noisy.Outcome);

            // And the informative cues are the same cues, so the extra codes are purely
            // additional rather than a different reading of the same world.
            Assert.All(bare.Cues, code => Assert.Contains(code, noisy.Cues));
        }

        Assert.Equal(plain.Informative, cluttered.Informative);
        Assert.Equal(plain.Bits + 8, cluttered.Bits);
    }
}
