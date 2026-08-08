using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What background costs, before anything is built to refuse it — <b>fork 51's control
/// arm.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A CODE LIVE IN EVERY MOMENT SEPARATES NOTHING, AND REPAIR ALREADY KNOWS THAT.</b>
/// <see cref="Repair.Divergence"/> asks how often a code was present in hits against in
/// misses; present in all of both gives one and one, so its separation is nought and it
/// can never be chosen as a condition. The mechanism John asked after is, for repair,
/// already there.
/// </para>
/// <para>
/// <b>GENESIS AND THE TALLY HAVE NO SUCH GUARD, AND THIS MEASURES WHAT THAT COSTS.</b>
/// Genesis mints one commitment per live code on a surprise, so every always-on code
/// becomes a candidate for every outcome it ever sees — which is every outcome. And
/// <see cref="Commitment.Settle"/> stores an entry per non-scope code in every moment a
/// commitment fires on, so an always-on code is an entry in EVERY commitment's table,
/// permanently, with a divergence pinned at nought for the life of the run.
/// </para>
/// <para>
/// <b>MEASURED BEFORE MITIGATED, WHICH IS THE ORDER THIS PROJECT KEEPS HAVING TO
/// RELEARN.</b> A gate built first would be measured one-OFF-from-all-on against a world
/// nobody had characterised. What the grid below establishes is the shape of the cost —
/// and whether it is in the candidates, in the table, or in the score.
/// </para>
/// </remarks>
public sealed class ClutterCostTests(ITestOutputHelper output)
{
    private static double Recent(int clutter, bool gate, int seed) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = 2, Clutter = clutter },
            new Brain(
                new CommittingSettings { Rooting = gate ? Rooting.Varying : Rooting.Anything },
                seed),
            seed).Run(20_000).Recent;

    private static Learned Gated(int clutter) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = 2, Clutter = clutter },
            new Brain(new CommittingSettings { Rooting = Rooting.Varying }, seed: 1),
            seed: 1).Run(20_000);

    private static Learned Run(int clutter) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = 2, Clutter = clutter },
            new Brain(new CommittingSettings(), seed: 1),
            seed: 1).Run(20_000);

    /// <summary>
    /// <b>THE COST OF BACKGROUND, IN THE THREE PLACES IT COULD BE.</b>
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

        // THE ONE THING THAT MUST HOLD WHATEVER THE COST IS: background carries no
        // information, so it cannot make a rule true that was not, nor false that was.
        // A soundness count moving with clutter would mean the answer key had started
        // reading the noise, which is a bug in the instrument rather than a finding.
        Assert.All(arms, one => Assert.Equal(0, one.Unchecked));
    }

    /// <summary>
    /// <b>THE ARM: REFUSING TO ROOT ON A CODE THAT HAS NEVER BEEN ABSENT.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>ONE MECHANISM ON FROM A KNOWN BASELINE, WHICH IS THE ONLY ORDER THIS PROJECT
    /// ACCEPTS.</b> The grid above characterised the cost first, and it moved the fix:
    /// the damage is not the minting RATE, which the surprise gate already bounds, but
    /// that background becomes a PARENT whose children inherit a code that can never earn
    /// its place.
    /// </para>
    /// <para>
    /// <b>AND THE CLEAN COLUMN IS THE HALF THAT MATTERS.</b> A gate that helps where
    /// there is background and costs nothing where there is none is a gate; one that buys
    /// its win by damaging the clean world is <see cref="Mending"/> again — three rules,
    /// three worlds, three winners — and this project has enough of those.
    /// </para>
    /// </remarks>
    [Fact]
    public void Refusing_to_root_on_what_never_varies()
    {
        // TWELVE, BECAUSE THIS READING DECIDES WHETHER THE ARM BECOMES THE CODE. Five
        // said 2.7 standard errors on the cluttered world and half of one on the clean,
        // which is enough to keep measuring and not enough to delete the alternative --
        // and deleting the alternative is what happens next, so the number that justifies
        // it has to be the better one. `ONE SEED IS NOT A COMPARISON AND WILL HAPPILY
        // INVERT` is in the traps list; five is not much further along.
        int[] seeds = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

        var arms = Fixture.Abreast(
        [
            .. new[] { (0, false), (0, true), (8, false), (8, true) }
                .Select<(int Clutter, bool Gate), Func<Measured>>(one => () => new Measured
                {
                    Arm = $"clutter {one.Clutter} {(one.Gate ? "gated" : "open")}",
                    Values = [.. seeds.Select(seed => Recent(one.Clutter, one.Gate, seed))],
                }),
        ]);

        foreach (var arm in arms) output.WriteLine(arm.ToString());

        // THE GATE AGAINST ITS OWN CONTROL, ON THE WORLD IT IS FOR AND THE WORLD IT MUST
        // NOT DAMAGE. No bar on either: a threshold written before the first reading is a
        // prediction dressed as a check, and both of these are first readings.
        output.WriteLine(
            $"cluttered: {arms[3].Separation(arms[2]):F1} standard errors apart");
        output.WriteLine(
            $"clean:     {arms[1].Separation(arms[0]):F1} standard errors apart");
    }

    /// <summary>
    /// <b>AND THE GATE DOES WHAT IT SAYS, WHICH IS NOT A PREDICTION BUT A DEFINITION.</b>
    /// </summary>
    /// <remarks>
    /// The grid above reports what refusing background is WORTH, and that is a first
    /// reading over five seeds rather than a bar. This is the other half: whether the
    /// mechanism did the thing it is named for. A gate that improved a score while
    /// leaving background-rooted rules resident would be improving it for some other
    /// reason, and the number would be believed anyway.
    /// </remarks>
    [Fact]
    public void And_nothing_is_left_rooted_on_background_when_it_is_gated()
    {
        var run = new MultiplexerRun(
            new MultiplexerSettings { Address = 2, Clutter = 8 },
            new Brain(new CommittingSettings { Rooting = Rooting.Varying }, seed: 1),
            seed: 1);

        run.Run(20_000);

        var background = Enumerable.Range(6, 8)
            .Select(at => Codes.Bits.Of(Multiplexer.Bit, at, 1))
            .ToHashSet();

        var tainted = run.Held.All
            .Select(one => run.Held.Names.Unfold(one.Scope))
            .Count(scope => scope.Any(background.Contains));

        output.WriteLine($"{tainted} resident commitments still carry an always-present code");

        Assert.Equal(0, tainted);
    }

    /// <summary>
    /// <b>WHERE THE EXTRA TABLE ACTUALLY COMES FROM, RATHER THAN WHERE IT LOOKS LIKE IT
    /// SHOULD.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Minting barely moves with background — the surprise gate absorbs it — so the
    /// obvious story, that genesis floods the population with candidates, is wrong. What
    /// grows is the table, and the question this answers is whether the growth is
    /// commitments CARRYING a useless code or merely more commitments.
    /// </para>
    /// <para>
    /// <b>An always-on code cannot be chosen as a condition and can still be a ROOT.</b>
    /// Genesis mints one-code commitments per live code, so background becomes a parent —
    /// and every child repair hangs off it inherits the useless code forever, while being
    /// otherwise a perfectly good rule. That would show up exactly as this repo's grid
    /// showed it: sound rules RISING with background rather than falling.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_growth_is_rules_rooted_on_background_rather_than_more_rules()
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
            + $"always present ({tainted / (double)resident.Count:P0})");

        output.WriteLine(
            $"of those, {resident.Count(scope => scope.Any(background.Contains) && scope.Length > 1)}"
            + " have a real condition beside it");

        // NO BAR, BECAUSE THE SHARE IS THE READING. What must hold is only that the
        // question is answerable: if nothing were rooted on background the inference
        // above would be refuted, and that is worth being able to see.
        Assert.NotEmpty(resident);
    }

    /// <summary>
    /// <b>AND THE WORLD IS UNCHANGED UNDERNEATH IT, WHICH IS WHAT MAKES THE GRID
    /// READABLE.</b>
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

            // AND THE INFORMATIVE CUES ARE THE SAME CUES, so the extra codes are purely
            // additional rather than a different reading of the same world.
            Assert.All(bare.Cues, code => Assert.Contains(code, noisy.Cues));
        }

        Assert.Equal(plain.Informative, cluttered.Informative);
        Assert.Equal(plain.Bits + 8, cluttered.Bits);
    }
}
