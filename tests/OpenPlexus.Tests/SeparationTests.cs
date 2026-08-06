using System.Text.RegularExpressions;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// A world is a problem. The brain is the thing being tuned. Neither reaches into the
/// other.
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S RULE, AND IT IS ABOUT WHAT WENT WRONG LAST TIME.</b> On `csharp` the
/// worlds grew dials that reached into the brain — `Ranking` was `Sum` on bAbI and
/// `Agreement` on CLEVR, so a WORLD decided how the brain thought. Every score was
/// then a comparison between two brains as much as between two problems, and nobody
/// could say which.
/// </para>
/// <para>
/// <b>A RULE NOBODY CHECKS IS A RULE THAT LASTS UNTIL THE NEXT WORLD.</b> This one
/// arrived in the code within an hour of being agreed and had already been broken:
/// the graded world's runner owned the choice of front end, the band count and the
/// projection geometry — three brain-side decisions living inside a world.
/// </para>
/// </remarks>
public sealed class SeparationTests
{
    /// <summary>Everything a world is not allowed to have heard of.</summary>
    /// <remarks>
    /// <b>The NEW brain only.</b> `csharp`'s walk types are named all over the old
    /// worlds and go when they go; naming them here would make this check a report on
    /// work already scheduled rather than a guard on work being done.
    /// </remarks>
    private static IEnumerable<string> Brainish() =>
        typeof(Commitment).Assembly
            .GetTypes()
            .Where(one => one.IsPublic)
            .Where(one =>
                one.Namespace == "OpenPlexus.Commitments"
                || one.Namespace == "OpenPlexus.Machines")
            .Select(one => one.Name.Split('`')[0])
            .Distinct(StringComparer.Ordinal);

    private static IEnumerable<KeyValuePair<string, string>> Worlds() =>
        Tree.Sources("src")
            .Where(path => path.Contains(
                $"{Path.DirectorySeparatorChar}Worlds{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .Select(path => new KeyValuePair<string, string>(path, File.ReadAllText(path)));

    [Fact]
    public void No_world_has_heard_of_the_brain()
    {
        var brainish = Brainish().ToList();

        Assert.NotEmpty(brainish);

        var trespass = new List<string>();

        foreach (var (path, text) in Worlds())
        {
            // THE WALK-ERA WORLDS ARE EXEMPT AND SAY SO. They name `csharp`'s brain
            // everywhere and go when it goes -- this guards what is being built now.
            if (!Path.GetFileName(path).StartsWith("Multiplexer", StringComparison.Ordinal)
                && !Path.GetFileName(path).StartsWith("Graded", StringComparison.Ordinal)
                && !Path.GetFileName(path).StartsWith("IWorld", StringComparison.Ordinal))
                continue;

            foreach (var name in brainish)
                if (Regex.IsMatch(text, @"\b" + Regex.Escape(name) + @"\b"))
                    trespass.Add($"{Path.GetFileName(path)} names {name}");
        }

        Assert.True(trespass.Count == 0,
            $"{trespass.Count} place(s) where a world reaches into the brain. Move the "
            + "decision to the join, or turn it into something the world outputs:\n  "
            + string.Join("\n  ", trespass));
    }

    [Fact]
    public void And_the_check_can_still_fail()
    {
        // THE COMPANION, AND WITHOUT IT THIS PASSES FOR A TYPE LIST THAT CAME BACK
        // EMPTY -- which is exactly how a separation rule rots into a comment.
        var brainish = Brainish().ToList();

        Assert.Contains("Population", brainish);
        Assert.Contains("CommittingSettings", brainish);
        Assert.Contains("Brain", brainish);
        Assert.Contains("Trial", brainish);

        Assert.DoesNotContain("Multiplexer", brainish);
        Assert.DoesNotContain("Graded", brainish);
    }

    [Fact]
    public void Every_world_says_its_outcome_in_the_same_alphabet()
    {
        // A BRAIN THAT LEARNT A DIFFERENT ALPHABET PER WORLD WOULD NOT BE ONE BRAIN,
        // and a commitment about an outcome would mean different things depending on
        // who was asking. The multiplexer keeps its own name for this because its
        // answer key is written in it; the two must agree or they will drift.
        for (var outcome = 0; outcome < 2; outcome++)
            Assert.Equal(Brain.Says(outcome), Multiplexer.Says(outcome));

        Assert.Equal(Brain.Followed, Multiplexer.Said);
    }

    [Fact]
    public void One_brain_can_be_handed_two_different_worlds()
    {
        // THE POINT OF THE WHOLE ARRANGEMENT, ASSERTED RATHER THAN DESCRIBED. The
        // same brain object, configured once, learns a symbolic world and a graded
        // one -- so switching world cannot switch brain, because there is only one.
        var brain = new Brain(new CommittingSettings(), seed: 1);

        var symbolic = new Trial<IReadOnlyList<int>>(
            new Multiplexer(new MultiplexerSettings { Address = 2 }, seed: 1),
            new Bits(Multiplexer.Bit),
            brain);

        var graded = new Trial<IReadOnlyList<double>>(
            new Graded(new GradedSettings { Address = 3, Crowding = 0.9 }, seed: 1),
            new Winnowing(Multiplexer.Bit, 11),
            brain);

        var first = symbolic.Run(4000);
        var second = graded.Run(4000);

        Assert.True(first.Rounds == 4000 && second.Rounds == 4000);

        // AND IT CARRIES WHAT IT LEARNT ACROSS, because the population is the brain's
        // and not the trial's. Whether that HELPS is a separate question nobody has
        // measured; that it is possible at all is what the seam buys.
        Assert.True(second.Resident > first.Resident);
    }
}
