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
    /// <param name="categories">
    /// Groups of alternatives to fold in, or nothing. <b>An axis and never a setting</b> —
    /// the same cell with and without them is what says whether a category buys anything.
    /// </param>
    private (double Exam, int Held) Cell(
        ReturningSettings settings, string label,
        IReadOnlyList<IReadOnlySet<Code>>? categories = null)
    {
        var world = new Returning(settings, seed: 1);
        var brain = new Brain(new CommittingSettings { Capacity = 4000 }, seed: 1);

        IQuantizer<Coded> front = categories is null
            ? new Passthrough()
            : new Sorted<Coded>(new Passthrough(), categories);

        var tally = new Trial<Coded>(world, front, brain)
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

    /// <summary>
    /// Whether the appearances a thing wears can be found to be ONE CATEGORY from the
    /// moments alone — <b>John's account of a category, and the reason the individual and
    /// the category are one mechanism rather than two.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>RUNG FIVE CANNOT REACH THIS AND THE PLAN ALREADY SAID SO.</b> It names what
    /// CO-FIRES, and a basket at two moments does not co-occur with itself. The looks one
    /// thing wears are the opposite shape: they never co-occur and they keep the same
    /// company, which is a category. So what would collapse a rule-per-appearance into a
    /// rule is <see cref="Alternating"/> and never abstraction.
    /// </para>
    /// <para>
    /// <b>AND NOTHING IS HANDED OVER, WHICH IS THE DIFFERENCE FROM EVERY CEILING IN THE
    /// GRID ABOVE.</b> The groups are derived from drawn moments by exclusion and shared
    /// company. The world's own tables are never read — which matters especially here,
    /// because a landmark's codes are drawn per THING, so handing that grouping over would
    /// be the index of the forbidden cell wearing a different hat.
    /// </para>
    /// <para>
    /// <b>WHAT WOULD REFUTE THE ROUTE, SAID BEFORE IT RUNS: if the derived categories do not
    /// cut the rule count, then a name over alternatives does not compress and the
    /// individual-as-category account is wrong however tidy it sounds.</b> The score is the
    /// second column and not the first — the placed cell already answers everything, so what
    /// is being asked for is the 9.3x.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_the_looks_a_thing_wears_are_findable_as_one_category()
    {
        var settings = World(twinned: true, tagged: false, placed: true);

        // DRAWN FROM ITS OWN WORLD, so the groups are found in what a front end would have
        // seen rather than in the examination. A separate instance at the same seed sees the
        // same distribution and none of the withheld sightings.
        var watching = new Returning(settings, seed: 1);

        var moments = Enumerable.Range(0, 4000)
            .Select(_ => (IReadOnlySet<Code>)new HashSet<Code>(watching.Next().Seen.Codes))
            .ToList();

        var found = Alternating.From(moments, company: 0.5, floor: 20);

        // THE MODALITY IS THE READOUT AND THE WORLD'S TABLES ARE NOT. A look rides on one
        // modality and a landmark on another, so how many groups of each kind were found,
        // and whether any group MIXED them, says what the derivation recovered without
        // anything having to be told which code is which thing.
        var byKind = found
            .GroupBy(one => string.Join("+", one.Select(code => code.Modality).Distinct().Order()))
            .OrderBy(one => one.Key, StringComparer.Ordinal);

        output.WriteLine($"{found.Count} groups derived from {moments.Count} sightings");

        foreach (var kind in byKind)
            output.WriteLine(
                $"  modality {kind.Key,-6}| {kind.Count(),3} groups | sizes "
                + $"{string.Join(",", kind.Select(one => one.Count).OrderDescending().Take(8))}");

        var (plain, byLook) = Cell(settings, "twinned anonymous placed");
        var (sorted, byCategory) = Cell(settings, "  the same, categories folded in", found);

        var byTag = Cell(World(twinned: true, tagged: true), "twinned tagged, the floor").Held;

        output.WriteLine(
            $"categories move the score {sorted - plain:+0.000;-0.000} and the population "
            + $"{byCategory - byLook:+#;-#;0} ({byCategory / (double)byLook:F2}x), against a "
            + $"handed index's {byTag}");

        // THE INSTRUMENT CHECK, AND IT IS THE ONE THAT WOULD OTHERWISE PASS UNNOTICED. A
        // derivation returning nothing, or returning one group holding the alphabet, would
        // make the cell below identical to the cell above and the whole reading would be a
        // no-op reported as a result.
        Assert.True(found.Count > 1,
            $"the derivation found {found.Count} groups, so folding them in changes nothing "
            + "and the two cells below are the same cell printed twice");

        Assert.True(found.Max(one => one.Count) <= moments.Count / 100,
            $"a derived group holds {found.Max(one => one.Count)} codes, which is a large "
            + "share of the alphabet and is single-link clustering running away rather than "
            + "a category");

        // FINDING ONE: THE APPEARANCES ARE RECOVERED EXACTLY AND NOTHING WAS HANDED OVER.
        // Every group is a modality's own, so exclusion and shared company never once
        // confused where a thing stands with what it looks like -- and the look groups come
        // back one per attribute per appearance, at the size the world draws them from. That
        // is John's account of a category, computed from moments.
        var looks = found.Where(one => one.All(code => code.Modality == 23)).ToList();

        Assert.True(looks.Count == 12 && looks.All(one => one.Count == 4),
            $"the derivation recovered {looks.Count} appearance groups of sizes "
            + $"{string.Join(",", looks.Select(one => one.Count).Distinct().Order())}, where the "
            + "world draws four alternatives for each of three attributes over four distinct "
            + "appearances -- so it is not finding what a thing wears");

        Assert.DoesNotContain(found, one => one.Select(code => code.Modality).Distinct().Count() > 1);

        // FINDING TWO, AND IT IS THE LIMIT RATHER THAN THE RESULT: SUBSTITUTABILITY FINDS A
        // KIND AND NEVER AN INDIVIDUAL. A landmark is drawn per THING, so eight things means
        // eight groups of four -- and what comes back is four groups of eight, each holding
        // both twins' landmarks. That is not a bug in the derivation. Twins are substitutable
        // by construction, so every statistic over the moments is the same for both, and the
        // only thing separating them is that their landmarks predict different answers, which
        // is not in the input at all.
        //
        // SO *THE INDIVIDUAL IS A CATEGORY OVER ITS APPEARANCES* IS HALF RIGHT. The category
        // reaches the PAIR. Getting from the pair to the individual needs something that
        // reads what a code predicts, which is a gate rather than a derivation.
        var places = found.Where(one => one.All(code => code.Modality == 25)).ToList();

        Assert.True(places.Count == 4 && places.All(one => one.Count == 8),
            $"the landmark groups came back as {places.Count} of sizes "
            + $"{string.Join(",", places.Select(one => one.Count).Distinct().Order())}. Eight of "
            + "four would mean substitutability separated the twins, which it cannot -- and if "
            + "it now does, this file's account of what a category reaches is wrong");

        // FINDING THREE: A CATEGORY IN THE MOMENT IS NOT A REWRITE OF THE RULES, WHICH IS
        // WHERE THE COMPRESSION WENT. The plain code stays beside the category on purpose, so
        // genesis still roots on it and repair still specialises on it -- the category is one
        // more thing available rather than a shorter way to say what is already held. The
        // population barely moves, and the 9.3x this was aimed at is untouched.
        //
        // SO WHAT IS MISSING IS THE REWRITE AND NOT THE NAME, which is fork 85 arriving from a
        // new direction: a coarse name can only enter a scope as a NEW claim with a fresh
        // record, and nothing here mints that claim.
        Assert.True(Math.Abs(sorted - plain) < 0.05,
            $"folding categories in moved the score by {sorted - plain:+0.000;-0.000}, so the "
            + "fold is not the free addition this file reports and the population column is "
            + "measuring two changes at once");

        Assert.True(byCategory > 2 * byTag,
            $"the categories cell holds {byCategory} rules against a handed index's {byTag}. If "
            + "that gap has closed then a name over alternatives now compresses on its own, the "
            + "rewrite arrived from somewhere, and every claim in this file is owed a re-take");
    }
}
