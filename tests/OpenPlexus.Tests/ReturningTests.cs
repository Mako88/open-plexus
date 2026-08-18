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
/// <b>The two axes are independent and that is the whole design.</b> <c>Twinned</c> is a
/// fact about the world — whether two things look alike — and <c>Tagged</c> is a fact
/// about what the front end hands over. Crossing them gives four cells where an arm would
/// give one number and no way to read it: the untwinned row says the harness can score at
/// all, and the twinned row says what identity is worth once appearance has run out.
/// </para>
/// <para>
/// <b>And the cell nobody may ship is the one that makes the others legible.</b> A handed
/// index is exactly what John's ordering forbids — point a phone at a basket, look away,
/// look back, and nothing outside the learner may say it is the same basket. It is here
/// because a gap needs two ends, in the same way fork 88 priced rung four by handing the
/// selection over in a front end nobody was going to ship.
/// </para>
/// </remarks>
public sealed class ReturningTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    private static ReturningSettings World(
        bool twinned, bool tagged, bool placed = false, double wandering = 0.0,
        double drifting = 0.0) =>
        new()
        {
            Things = 8, Attributes = 3, CodesPerAttribute = 4, Hidden = 2,
            Twinned = twinned, Tagged = tagged, Placed = placed, Withheld = 300,
            Wandering = wandering, Drifting = drifting,
        };

    /// <summary>What a front end watching this world would have seen, in order.</summary>
    /// <param name="settings">Which world.</param>
    /// <param name="seed">Which stream.</param>
    /// <param name="sightings">How many moments to watch.</param>
    /// <remarks>
    /// <b>A separate instance at the same seed, so the groups are found in what a front end
    /// would have seen rather than in the examination.</b> It sees the same distribution and
    /// none of the withheld sightings.
    /// </remarks>
    private static List<IReadOnlySet<Code>> Watched(
        ReturningSettings settings, int seed, int sightings = 4000)
    {
        var watching = new Returning(settings, seed);

        return Enumerable.Range(0, sightings)
            .Select(_ => (IReadOnlySet<Code>)new HashSet<Code>(watching.Next().Seen.Codes))
            .ToList();
    }

    /// <summary>How many groups of each size a derivation found on one modality.</summary>
    /// <param name="found">What came back.</param>
    /// <param name="modality">Which channel to read.</param>
    private static List<IReadOnlySet<Code>> On(
        IReadOnlyList<IReadOnlySet<Code>> found, byte modality) =>
        found.Where(one => one.All(code => code.Modality == modality)).ToList();

    /// <summary>One cell of the grid, run and printed.</summary>
    /// <param name="settings">Which cell.</param>
    /// <param name="label">What to call it in the output.</param>
    /// <param name="categories">
    /// The vocabulary of alternatives to fold in, or nothing. <b>An axis and never a
    /// setting</b> — the same cell with and without them is what says whether a category
    /// buys anything.
    /// </param>
    /// <param name="coarsening">
    /// Whether subsumption may read a member's entailment of its category. <b>A second axis,
    /// and it crosses the first rather than extending it</b> — the fold puts a coarse code in
    /// the moment and this lets a rule about one absorb a rule about the other, so a cell
    /// moving with both on says nothing about which did it.
    /// </param>
    /// <param name="seed">
    /// What draws the world and the brain's control arm. <b>One number for both</b>, so a
    /// seed is a run rather than two independent draws that could be varied apart.
    /// </param>
    /// <remarks>
    /// <b>ONE <see cref="Categories"/> for the front end and the population both.</b> A
    /// category's code is derived from its members, so two vocabularies built from the same
    /// groups would agree — but a rewrite reading a vocabulary the front end is not folding
    /// mints rules that can never fire, and nothing downstream would report it.
    /// </remarks>
    private (double Exam, int Held) Cell(
        ReturningSettings settings, string label,
        Categories? categories = null, Coarsening coarsening = Coarsening.Never, int seed = 1)
    {
        var world = new Returning(settings, seed: seed);

        var brain = new Brain(
            new CommittingSettings { Capacity = 4000, Coarsening = coarsening }, seed: seed);

        brain.Held.Sorts = categories;

        IQuantizer<Coded> front = categories is null
            ? new Passthrough()
            : new Sorted<Coded>(new Passthrough(), categories);

        var tally = new Bench(new Body(new Watching<Coded>(world, front)), brain)
            .Run(Rounds, sweep: 1000, target: 0.95, window: 2000);

        var exam = tally.Unseen?.Accuracy ?? 0.0;

        output.WriteLine(
            $"{label,-34}| exam {exam:F3} | own {tally.Recent:F3} "
            + $"| appearance reaches {world.Appearance:F2} "
            + $"| held {brain.Held.Count,4}");

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

        // And the cell John's account predicts, which is the one worth the file. Twins are
        // identical in appearance and stand in different places, and nothing is handed
        // over. If a relation recovers what appearance lost, then an individual is reachable
        // as a bundle of relations rather than as a stored name — which is concept-before-
        // label run again for referents, and needs no new operator at all.
        var (placed, byPlace) = Cell(
            World(twinned: true, tagged: false, placed: true), "twinned anonymous placed");

        var byTag = Cell(World(twinned: true, tagged: true), "twinned tagged, re-read").Held;

        // The harness check first, and it is not a formality. Where every thing looks
        // different, appearance is a complete answer and the learner should find it with no
        // index at all. A world that scored badly HERE would be one where the codes are too
        // noisy to carry anything, and every number in the twinned row would then be about
        // the noise rather than about identity.
        Assert.True(scored[(false, false)] > 0.7,
            $"appearance alone reached {scored[(false, false)]:F3} where every thing looks "
            + "different, so the signal is too weak to read anything else off this world");

        // And the finding the world exists for. Twinned, appearance is exhausted by
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

        // And the control that says the landmark is not just more signal. A place is another
        // channel of codes, so a cell that improved could be improving because the moment
        // got wider rather than because a RELATION separated the twins. What rules that out
        // is that the twins share every appearance code by construction: the only thing a
        // place can be carrying is which of the two this is.
        Assert.True(placed >= scored[(true, false)],
            $"the landmark cell reached {placed:F3} against {scored[(true, false)]:F3} with "
            + "no landmark, so adding a channel that uniquely separates the twins made the "
            + "world harder — which is a fault in the harness rather than a finding");

        // And what it cost to get there, which is the finding rather than the score. Both
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

        // No bar on the anonymous twinned cell itself. That it sits near the pair's base
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
    /// <b>Rung five cannot reach this and the plan already said so.</b> It names what
    /// CO-FIRES, and a basket at two moments does not co-occur with itself. The looks one
    /// thing wears are the opposite shape: they never co-occur and they keep the same
    /// company, which is a category. So what would collapse a rule-per-appearance into a
    /// rule is <see cref="Alternating"/> and never abstraction.
    /// </para>
    /// <para>
    /// <b>And nothing is handed over, which is the difference from every ceiling in the
    /// grid above.</b> The groups are derived from drawn moments by exclusion and shared
    /// company. The world's own tables are never read — which matters especially here,
    /// because a landmark's codes are drawn per THING, so handing that grouping over would
    /// be the index of the forbidden cell wearing a different hat.
    /// </para>
    /// <para>
    /// <b>What would refute the route, said before it runs: if the derived categories do not
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

        // The modality is the readout and the world's tables are not. A look rides on one
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

        var sorting = new Categories(found);

        var (plain, byLook) = Cell(settings, "twinned anonymous placed");
        var (sorted, byCategory) = Cell(
            settings, "  the same, categories folded in", sorting, Coarsening.Never);

        var byTag = Cell(World(twinned: true, tagged: true), "twinned tagged, the floor").Held;

        output.WriteLine(
            $"categories move the score {sorted - plain:+0.000;-0.000} and the population "
            + $"{byCategory - byLook:+#;-#;0} ({byCategory / (double)byLook:F2}x), against a "
            + $"handed index's {byTag}");

        // The instrument check, and it is the one that would otherwise pass unnoticed. A
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

        // Finding one: the appearances are recovered exactly and nothing was handed over.
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

        // Finding two, and it is the limit rather than the result: substitutability finds a
        // kind and never an individual. A landmark is drawn per THING, so eight things means
        // eight groups of four -- and what comes back is four groups of eight, each holding
        // both twins' landmarks. That is not a bug in the derivation. Twins are substitutable
        // by construction, so every statistic over the moments is the same for both, and the
        // only thing separating them is that their landmarks predict different answers, which
        // is not in the input at all.
        //
        // So *the individual is a category over its appearances* is half right. The category
        // reaches the PAIR. Getting from the pair to the individual needs something that
        // reads what a code predicts, which is a gate rather than a derivation.
        var places = found.Where(one => one.All(code => code.Modality == 25)).ToList();

        Assert.True(places.Count == 4 && places.All(one => one.Count == 8),
            $"the landmark groups came back as {places.Count} of sizes "
            + $"{string.Join(",", places.Select(one => one.Count).Distinct().Order())}. Eight of "
            + "four would mean substitutability separated the twins, which it cannot -- and if "
            + "it now does, this file's account of what a category reaches is wrong");

        // Finding three: a category in the moment is not a rewrite of the rules, which is
        // where the compression went. The plain code stays beside the category on purpose, so
        // genesis still roots on it and repair still specialises on it -- the category is one
        // more thing available rather than a shorter way to say what is already held. The
        // population barely moves, and the 9.3x this was aimed at is untouched.
        //
        // So what is missing is the rewrite and not the name, which is fork 85 arriving from a
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

    /// <summary>
    /// Whether letting a category into a SCOPE compresses what folding it into the moment
    /// did not — <b>fork 85, and the second half of fork 97.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The fold is the control and the judge is the arm.</b> The test above establishes
    /// that a category in the moment moves the population by nearly nothing; this asks what
    /// happens when subsumption is allowed to see that a rule pinning a MEMBER is narrower
    /// than one pinning its category.
    /// </para>
    /// <para>
    /// <b>And the operator fork 85 actually asked for was built here and deleted.</b> It
    /// proposed the coarse claim as a new commitment with a fresh record — which is not
    /// optional, since carrying the parent's would keep a rule's evidence while widening what
    /// it claims — and it cost 60 rules over three seeds where the judge cost none. A
    /// proposal must fire <see cref="CommittingSettings.Floor"/> times before anything may
    /// judge it, and the claim it makes was already reachable, because genesis roots on the
    /// coarse code exactly as it roots on anything else in the moment.
    /// </para>
    /// <para>
    /// <b>WHAT WOULD REVIVE IT is a vocabulary the brain holds that no moment carries.</b>
    /// Every sentence above turns on the front end folding the category in; a category
    /// derived and kept inside the population would have nothing minting its rules.
    /// </para>
    /// <para>
    /// <b>And the score is a guard rather than the column.</b> The landmark cell already
    /// answers nearly everything, so nothing here can buy accuracy and anything can sell it
    /// — a population that compressed by generalising past what is true would show up as
    /// rules falling and the exam falling with them.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_a_category_in_a_scope_compresses_what_one_in_the_moment_did_not()
    {
        var settings = World(twinned: true, tagged: false, placed: true);

        // Three seeds, because a population count is the column and one of them would be a
        // coincidence. This repo has already had a lift live at one seed and vanish at two
        // others -- see the plan's caution about `Resolved(1)`. Each seed redraws the world
        // AND the derivation, so a category found on one stream is never carried onto
        // another's.
        var arms = new[]
        {
            ("no categories at all", Coarsening.Never),
            ("  folded into the moment", Coarsening.Never),
            ("  and the entailment judged", Coarsening.Judged),
        };

        var held = new int[arms.Length];
        var scored = new double[arms.Length];

        foreach (var seed in new[] { 1, 2, 3 })
        {
            // Derived from its own seed's world, so the groups are found in what a front end
            // watching THIS run would have seen. A vocabulary derived once and reused would
            // make seeds two and three a test of how well seed one's categories travel,
            // which is a different question and a much easier one.
            var sorting = new Categories(
                Alternating.From(Watched(settings, seed), company: 0.5, floor: 20));

            output.WriteLine($"seed {seed}, {sorting.Count} categories derived");

            for (var arm = 0; arm < arms.Length; arm++)
            {
                var cell = Cell(
                    settings, arms[arm].Item1, arm == 0 ? null : sorting, arms[arm].Item2, seed);

                scored[arm] += cell.Exam / 3.0;
                held[arm] += cell.Held;
            }
        }

        var byTag = Cell(World(twinned: true, tagged: true), "twinned tagged, the floor").Held;

        output.WriteLine(
            $"over three seeds: {held[0]} rules plain, {held[1]} folded and {held[2]} judged, "
            + $"against a handed index's {byTag} a seed");

        // The finding, and it is a correction rather than a result: what was missing was the
        // judge and not a rewrite. Genesis already roots on a category code, because it is in
        // the moment like any other -- so the population already held coarse rules, and held
        // every member's rule beside them because nothing could see that one entails the
        // other. Teaching subsumption the entailment is the whole of the compression fork 85
        // was after, and it proposes not one new claim to get it.
        //
        // And the operator fork 85 asked for is deleted, measured. Proposing the coarse claim
        // with a fresh record cost 60 rules over these three seeds where the judge costs
        // none: a proposal must fire `Floor` times before anything may judge it, and the
        // claim it makes was already reachable. The plan carries the revival row -- what
        // would bring it back is a vocabulary the brain holds that no moment carries.
        Assert.True(held[2] < held[1] / 2,
            $"judging the entailment holds {held[2]} rules against the fold's {held[1]}, so "
            + "the coarse rules genesis mints are not absorbing their members and this "
            + "file's account of where the compression comes from is wrong");

        // And the fold alone is the control that says so. A category in the moment moves the
        // population by nearly nothing, which is what made the judge look like a missing
        // rewrite in the first place.
        Assert.True(held[1] > held[0] * 0.8,
            $"folding categories into the moment took the population from {held[0]} to "
            + $"{held[1]}, so the fold compresses on its own now and the judge below is not "
            + "the mechanism this file credits");

        // The score guard, on every arm. A population that compressed by generalising past
        // what is true is the failure a rule count cannot tell from a success.
        for (var arm = 1; arm < arms.Length; arm++)
            Assert.True(Math.Abs(scored[arm] - scored[0]) < 0.15,
                $"{arms[arm].Item1.Trim()} scored {scored[arm]:F3} against {scored[0]:F3} "
                + "with no categories, so it did not compress a population -- it changed what "
                + "the population claims, and the rule count is measuring two things at once");
    }

    /// <summary>
    /// Whether continuity separates two things every statistic over the moments says are the
    /// same — <b>fork 106, John's, and the limit of <see cref="Alternating.From"/> attacked
    /// where it was found.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The derivation sees a bag of moments and no order, which is why the twins merged.</b>
    /// Substitutability is a fact about moments, and twins are substitutable by construction —
    /// so eight things came back as four groups of eight landmarks, each holding both twins'.
    /// Nothing about looking harder at moments can fix that.
    /// </para>
    /// <para>
    /// <b>So what is added is the stream's order and nothing else.</b> The world draws
    /// sightings in RUNS rather than uniformly, which changes which thing is met and changes
    /// nothing whatever about how a met thing is shown. A learner seeing one moment at a time
    /// is handed exactly what it was handed before.
    /// </para>
    /// <para>
    /// <b>What would kill it, said before it runs and carried from the handoff that named
    /// it: if the landmarks still come back four-of-eight, continuity does not separate
    /// substitutable things and the route back is a GATE reading what a proposal predicts.</b>
    /// That is the honest alternative and it is a different kind of mechanism — a derivation
    /// over what arrived against a judge of what a claim earns.
    /// </para>
    /// <para>
    /// <b>And the uniform stream is the control that says adhesion reads time.</b> Run on a
    /// world drawing independently, the same clause must not recover a thing — otherwise it
    /// is reading some other regularity and the runs were never what paid.
    /// </para>
    /// <para>
    /// <b>It does not come back empty, and that is a property of the bar rather than of the
    /// world.</b> A ratio against chance corrects for nothing, so out of every pair in the
    /// alphabet one clears it by luck — the same failure rung five's gate exists against and
    /// pays for with a correction by the count of candidates. What the control can say is
    /// that noise reaches a PAIR and structure reaches the thing, and the distance between
    /// those two is the finding rather than the emptiness.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_continuity_separates_things_that_statistics_cannot()
    {
        const double Adhesion = 1.5;
        const int Seeds = 3;

        var running = World(twinned: true, tagged: false, placed: true, wandering: 0.8);
        var uniform = World(twinned: true, tagged: false, placed: true);

        var stream = Watched(running, seed: 1);

        // Control one: the space derivation on the same runs. It reads an unordered bag, so
        // making the stream continuous must leave it exactly where it was -- and if it does
        // not, the runs changed the moments and the arm below is measuring two things.
        var blind = Alternating.From(stream, company: 0.5, floor: 20);

        // Control two: the time derivation on a uniform stream. Every pair adheres at chance
        // there, so this must find nothing.
        var chance = Alternating.Over(
            Watched(uniform, seed: 1), Adhesion, floor: 20, span: 1);

        var found = Alternating.Over(stream, Adhesion, floor: 20, span: 1);

        foreach (var (label, groups) in new (string, IReadOnlyList<IReadOnlySet<Code>>)[]
            { ("space, on runs", blind), ("time, on uniform", chance), ("time, on runs", found) })
            output.WriteLine(
                $"{label,-18}| {groups.Count,2} groups | looks "
                + $"{string.Join(",", On(groups, 23).Select(one => one.Count).OrderDescending())} "
                + $"| places "
                + $"{string.Join(",", On(groups, 25).Select(one => one.Count).OrderDescending())}");

        Assert.True(On(blind, 25).Count == 4 && On(blind, 25).All(one => one.Count == 8),
            $"the space derivation found {On(blind, 25).Count} landmark groups on a continuous "
            + "stream where it found four of eight on a uniform one, so the runs changed what "
            + "a moment holds and this file's account of them is wrong");

        // And what the control can say is that nothing is recovered, not that nothing is
        // found. An uncorrected ratio lets one pair through on luck; a thing is four codes,
        // so a group that reaches four is the world and a group of two is the tail.
        Assert.DoesNotContain(On(chance, 25), one => one.Count >= 4);

        // THE FINDING. Eight groups of four is one per THING, which is what four of eight
        // could not reach: the twins' landmarks are drawn from different alphabets and only
        // ever turn up beside their own.
        var places = On(found, 25);

        Assert.True(places.Count == 8 && places.All(one => one.Count == 4),
            $"the landmark groups came back as {places.Count} of sizes "
            + $"{string.Join(",", places.Select(one => one.Count).Distinct().Order())}. Four of "
            + "eight means continuity does not separate substitutable things, the twins are "
            + "beyond a derivation over what arrived, and the route back is a gate reading "
            + "what a proposal predicts");

        // And the appearances are still recovered, which is what says the time clause is an
        // addition rather than a replacement. Twins wear one look, so the look groups are the
        // same twelve of four the space derivation finds -- an arm that separated the twins
        // and lost the appearances would have traded one recovery for another.
        var looks = On(found, 23);

        Assert.True(looks.Count == 12 && looks.All(one => one.Count == 4),
            $"the appearance groups came back as {looks.Count} of sizes "
            + $"{string.Join(",", looks.Select(one => one.Count).Distinct().Order())} against "
            + "twelve of four from the space derivation, so reading time cost what reading "
            + "space already had");

        Assert.DoesNotContain(found, one => one.Select(code => code.Modality).Distinct().Count() > 1);

        // And what it is worth, which is a second question and not a corollary. Separating
        // the twins in a derivation is one thing; a population getting smaller because of it
        // is another, and the two have come apart here before -- the fold recovered the
        // appearances exactly and moved the rule count by nothing until the judge arrived.
        //
        // The space categories are the control and not the plain cell. Both arms fold a
        // vocabulary in and both let subsumption read it, so the ONLY difference between
        // them is whether the landmark groups are per thing or per pair. A plain cell alone
        // would be measuring the categories and the twins at once.
        int byNothing = 0, heldByPair = 0, heldByThing = 0, byTag = 0;
        double loose = 0.0, byPair = 0.0, byThing = 0.0;

        foreach (var seed in Enumerable.Range(1, Seeds))
        {
            // Derived from its own seed's stream, both ways. A vocabulary derived once and
            // reused would make the later seeds a test of how well the first one's
            // categories travel, which is an easier question than the one being asked.
            var watched = seed == 1 ? stream : Watched(running, seed);

            var bySpace = Alternating.From(watched, company: 0.5, floor: 20);
            var byTime = Alternating.Over(watched, Adhesion, floor: 20, span: 1);

            // The derivation checked at every seed and not only the first, because the two
            // vocabularies below are the arm and a seed where they came back the same shape
            // would be a cell comparing one thing with itself.
            Assert.True(
                On(bySpace, 25).Count == 4 && On(byTime, 25).Count == 8,
                $"at seed {seed} space found {On(bySpace, 25).Count} landmark groups and time "
                + $"{On(byTime, 25).Count}, against four and eight -- so the separation this "
                + "arm is built on is a fact about seed one");

            var perPair = new Categories(bySpace);
            var perThing = new Categories(byTime);

            var none = Cell(running, $"seed {seed}, no categories", seed: seed);

            var space = Cell(
                running, "  categories from space", perPair, Coarsening.Judged, seed);

            var time = Cell(
                running, "  categories from time", perThing, Coarsening.Judged, seed);

            var tagged = Cell(
                World(twinned: true, tagged: true, wandering: 0.8),
                "  tagged, the floor", seed: seed);

            byNothing += none.Held;
            heldByPair += space.Held;
            heldByThing += time.Held;
            byTag += tagged.Held;

            loose += none.Exam / Seeds;
            byPair += space.Exam / Seeds;
            byThing += time.Exam / Seeds;
        }

        output.WriteLine(
            $"over {Seeds} seeds, per-thing categories hold {heldByThing} rules against "
            + $"{heldByPair} per-pair, {byNothing} with none and a handed index's {byTag}");

        // THE SCORE GUARD FIRST. This cell already answers everything, so no arm here can buy
        // accuracy and any of them can sell it.
        foreach (var (name, score) in new[] { ("space", byPair), ("time", byThing) })
            Assert.True(Math.Abs(score - loose) < 0.15,
                $"the {name} categories scored {score:F3} against {loose:F3} with none, so "
                + "what moved the rule count was what the population CLAIMS rather than how "
                + "much of it there is");

        // And the finding, which is the one this arm exists for: a category that reaches the
        // individual compresses where one that reaches the pair cannot. A per-pair landmark
        // group says *one of these two things* and still needs the look to say which, so the
        // conjunction survives; a per-thing group says the thing, and the rule collapses onto
        // it. That is what minting an individual is worth, derived rather than handed.
        Assert.True(heldByThing < heldByPair,
            $"per-thing categories hold {heldByThing} rules against {heldByPair} per-pair, so "
            + "separating the twins bought nothing a population can spend -- the derivation "
            + "is better and the machine is not, which is the gap this assertion exists to "
            + "catch");

        // And it goes past the ceiling the grid above called one, which is the claim most
        // worth refuting here. A handed index is one contentless code standing for the thing
        // and it still needs conjoining with what the thing looks like; a derived category
        // over a thing's own landmarks is the same code AND it absorbs the look rules that
        // named its members. If this ever flips, the individual this recovers is worth less
        // than being told, and the whole route is a longer way to a smaller answer.
        Assert.True(heldByThing < byTag,
            $"per-thing categories hold {heldByThing} rules against a handed index's "
            + $"{byTag}, so a derived individual has stopped beating a given one and this "
            + "file's account of why is wrong");
    }

    /// <summary>
    /// Whether what continuity recovers is a THING or a place a thing never leaves —
    /// <b>the control this world's own doc asked for before any of it was measured.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A landmark fixed for the life of the world is pinned by a conjunction, which is
    /// rung one and already built.</b> So <i>adhesion recovers the individual</i> is a claim
    /// with an obvious cheaper explanation sitting under it: a thing's landmark codes adhere
    /// because the thing has one landmark forever, and what is being recovered is the place.
    /// Nothing in the reading above separates those two.
    /// </para>
    /// <para>
    /// <b>So the things move and everything else is held.</b> Same looks, same twins, same
    /// runs, same alphabet — a thing occasionally stands somewhere else, which is exactly
    /// the fact that makes a place stop being a name for it.
    /// </para>
    /// <para>
    /// <b>What would refute the identity reading, said before this runs: if the landmark
    /// groups survive drift, adhesion is following the thing through its move and the
    /// individual is real; if they collapse or scatter, what was recovered was the place and
    /// the earlier reading is a conjunction wearing identity's clothes.</b> The score is the
    /// second column — a place category can only predict a thing's hidden attribute while
    /// nobody else has stood there.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_what_continuity_recovers_survives_the_thing_moving()
    {
        const double Adhesion = 1.5;

        var fixedPlace = World(twinned: true, tagged: false, placed: true, wandering: 0.8);

        var lift = new Dictionary<double, double>();
        var shrunk = new Dictionary<double, double>();

        foreach (var drifting in new[] { 0.0, 0.005, 0.02 })
        {
            var settings = World(
                twinned: true, tagged: false, placed: true, wandering: 0.8, drifting: drifting);

            var byTime = Alternating.Over(
                Watched(settings, seed: 1), Adhesion, floor: 20, span: 1);

            var places = On(byTime, 25);

            var sorted = Cell(settings, "  folded and judged", new Categories(byTime), Coarsening.Judged);
            var plain = Cell(settings, "  no categories", seed: 1);

            lift[drifting] = sorted.Exam - plain.Exam;
            shrunk[drifting] = plain.Held / (double)sorted.Held;

            output.WriteLine(
                $"drift {drifting:F3} | {places.Count,2} landmark groups of "
                + $"{string.Join(",", places.Select(one => one.Count).Distinct().Order())} "
                + $"| exam {sorted.Exam:F3} against {plain.Exam:F3} "
                + $"| held {sorted.Held} against {plain.Held}");
        }

        // THE INSTRUMENT FIRST: at no drift this must reproduce the eight groups of four the
        // arm above found, or a place that CAN move changed the world where it was set not
        // to and every row here is about something else.
        var control = On(Alternating.Over(
            Watched(fixedPlace, seed: 1), Adhesion, floor: 20, span: 1), 25);

        Assert.True(control.Count == 8 && control.All(one => one.Count == 4),
            $"the no-drift control found {control.Count} landmark groups of sizes "
            + $"{string.Join(",", control.Select(one => one.Count).Distinct().Order())}, so "
            + "adding a place that can move changed the world where it was set not to move");

        // And the finding, which refutes the reading the arm above invites. The groups
        // SURVIVE drift at eight of four -- and they have to, because eight places hold four
        // codes each whoever is standing in them. What the group count cannot say, the score
        // does: the category stops predicting the moment its members stop belonging to one
        // thing, and at the higher drift it is worse than holding no category at all.
        //
        // So what adhesion recovers is a persistent source of codes and never an individual.
        // Where a thing never moves the two are the same set and the reading above is what a
        // thing is worth; where it moves they come apart, and the place is what was found.
        // That is this world's own doc arriving on time: a fixed landmark is pinned by a
        // conjunction, which is rung one and was built long ago.
        // And the fixed-place row is a control on the population rather than on the score,
        // because that cell answers everything with or without a category. Reading it as a
        // lift is the mistake this line exists to have already made: the two are 1.000
        // against 1.000, and what the category buys there is 40 rules against 180.
        Assert.True(shrunk[0.0] > 3.0,
            $"the fixed-place cell held a {shrunk[0.0]:F1}x smaller population with the "
            + "category, so the control that makes the drifting rows readable is not there");

        // THE TRIPWIRE. If a drifting cell ever lifts, adhesion has started following a thing
        // THROUGH its move, an individual is reachable after all, and every sentence above
        // about a persistent source is owed a re-take.
        Assert.True(lift[0.02] < 0.05,
            $"the drifting cell lifted {lift[0.02]:+0.000;-0.000} over no category, so "
            + "continuity is tracking a thing through a move and what this file calls a "
            + "place is an individual");
    }
}
