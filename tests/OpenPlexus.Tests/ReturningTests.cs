using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What an INDIVIDUAL would be worth, measured before one is built — <b>John's basket, as a
/// grid with both ends of the gap in it.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE TWO AXES ARE INDEPENDENT AND THAT IS THE WHOLE DESIGN.</b> <c>Twinned</c> is a
/// fact about the world — whether two things look alike — and <c>Tagged</c> is a fact
/// about what the front end hands over. Crossing them gives four cells where an arm would
/// give one number and no way to read it: the untwinned row says the harness can score at
/// all, and the twinned row says what identity is worth once appearance has run out.
/// </para>
/// <para>
/// <b>AND THE CELL NOBODY MAY SHIP IS THE ONE THAT MAKES THE OTHERS LEGIBLE.</b> A handed
/// index is exactly what John's ordering forbids — point a phone at a basket, look away,
/// look back, and nothing outside the learner may say it is the same basket. It is here
/// because a gap needs two ends, in the same way fork 88 priced rung four by handing the
/// selection over in a front end nobody was going to ship.
/// </para>
/// </remarks>
public sealed class ReturningTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    private static ReturningSettings World(bool twinned, bool tagged, bool placed = false) =>
        new()
        {
            Things = 8, Attributes = 3, CodesPerAttribute = 4, Hidden = 2,
            Twinned = twinned, Tagged = tagged, Placed = placed, Withheld = 300,
        };

    /// <summary>One cell of the grid, run and printed.</summary>
    /// <param name="settings">Which cell.</param>
    /// <param name="label">What to call it in the output.</param>
    private (double Exam, int Held) Cell(ReturningSettings settings, string label)
    {
        var world = new Returning(settings, seed: 1);
        var brain = new Brain(new CommittingSettings { Capacity = 4000 }, seed: 1);

        var tally = new Trial<Coded>(world, new Passthrough(), brain)
            .Run(Rounds, sweep: 1000, target: 0.95, window: 2000);

        var exam = tally.Unseen?.Accuracy ?? 0.0;

        output.WriteLine(
            $"{label,-30}| exam {exam:F3} | own {tally.Recent:F3} "
            + $"| appearance reaches {world.Appearance:F2} | held {brain.Held.Count}");

        return (exam, brain.Held.Count);
    }

    [Fact]
    public void What_an_individual_is_worth_once_appearance_has_run_out()
    {
        var scored = new Dictionary<(bool Twinned, bool Tagged), double>();

        foreach (var twinned in new[] { false, true })
            foreach (var tagged in new[] { false, true })
                scored[(twinned, tagged)] = Cell(
                    World(twinned, tagged),
                    $"{(twinned ? "twinned" : "distinct")} {(tagged ? "tagged" : "anonymous")}").Exam;

        // AND THE CELL JOHN'S ACCOUNT PREDICTS, WHICH IS THE ONE WORTH THE FILE. Twins are
        // identical in appearance and stand in different places, and nothing is handed
        // over. If a relation recovers what appearance lost, then an individual is reachable
        // as a bundle of relations rather than as a stored name — which is concept-before-
        // label run again for referents, and needs no new operator at all.
        var (placed, byPlace) = Cell(
            World(twinned: true, tagged: false, placed: true), "twinned anonymous placed");

        var byTag = Cell(World(twinned: true, tagged: true), "twinned tagged, re-read").Held;

        // THE HARNESS CHECK FIRST, AND IT IS NOT A FORMALITY. Where every thing looks
        // different, appearance is a complete answer and the learner should find it with no
        // index at all. A world that scored badly HERE would be one where the codes are too
        // noisy to carry anything, and every number in the twinned row would then be about
        // the noise rather than about identity.
        Assert.True(scored[(false, false)] > 0.7,
            $"appearance alone reached {scored[(false, false)]:F3} where every thing looks "
            + "different, so the signal is too weak to read anything else off this world");

        // AND THE FINDING THE WORLD EXISTS FOR. Twinned, appearance is exhausted by
        // construction: two things wear one look and carry different answers, so nothing
        // that sees a moment at a time can beat the pair's base rate. The index is the only
        // thing separating them, and the distance is what minting an individual would buy.
        var gap = scored[(true, true)] - scored[(true, false)];

        output.WriteLine(
            $"an individual is worth {gap:+0.000;-0.000} on the twinned row "
            + $"({scored[(true, true)]:F3} against {scored[(true, false)]:F3})");

        Assert.True(gap > 0.2,
            $"a handed index bought {gap:+0.000;-0.000} where appearance runs out, so this "
            + "world does not pose the problem it was built for — either the twins are "
            + "separable by something else or the index is doing nothing, and both are "
            + "instrument faults rather than findings");

        output.WriteLine(
            $"a landmark recovers {placed - scored[(true, false)]:+0.000;-0.000} of it "
            + $"({(gap == 0 ? 0.0 : (placed - scored[(true, false)]) / gap):P0} of the way "
            + "to a handed index)");

        // AND THE CONTROL THAT SAYS THE LANDMARK IS NOT JUST MORE SIGNAL. A place is another
        // channel of codes, so a cell that improved could be improving because the moment
        // got wider rather than because a RELATION separated the twins. What rules that out
        // is that the twins share every appearance code by construction: the only thing a
        // place can be carrying is which of the two this is.
        Assert.True(placed >= scored[(true, false)],
            $"the landmark cell reached {placed:F3} against {scored[(true, false)]:F3} with "
            + "no landmark, so adding a channel that uniquely separates the twins made the "
            + "world harder — which is a fault in the harness rather than a finding");

        // AND WHAT IT COST TO GET THERE, WHICH IS THE FINDING RATHER THAN THE SCORE. Both
        // cells answer everything, and one does it with an order of magnitude more rules —
        // because a handed index is ONE code standing for the thing while a relation has to
        // be conjoined with every appearance the thing ever wears. So what minting an
        // individual buys here is not accuracy, it is COMPRESSION, and compression is what
        // a rule that transfers is made of.
        output.WriteLine(
            $"and it cost {byPlace} rules against the index's {byTag} "
            + $"({byPlace / (double)byTag:F1}x)");

        Assert.True(byPlace > byTag,
            $"the landmark cell held {byPlace} rules and the index cell {byTag}, so an index "
            + "is not buying compression here and the case for minting one is the score "
            + "alone — which the landmark already matches");

        // NO BAR ON THE ANONYMOUS TWINNED CELL ITSELF. That it sits near the pair's base
        // rate is the point rather than a result, and pinning a level would turn the
        // world's own arithmetic into a claim about the learner.
    }
}
