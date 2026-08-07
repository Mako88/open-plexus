using OpenPlexus.Codes;
using OpenPlexus.Commitments;

namespace OpenPlexus.Tests;

/// <summary>
/// The gate, the vote, and the two ways a commitment leaves.
/// </summary>
public sealed class PopulationTests
{
    private static Code Of(ulong value) => new(1, value);

    private static Code Says(ulong value) => new(2, value);

    private static HashSet<Code> Moment(params ulong[] codes) => [.. codes.Select(Of)];

    private static Commitment One(ulong expects, params ulong[] scope) =>
        new([.. scope.Select(Of)], Says(expects));

    /// <summary>Settles a commitment a given number of times, with a code present or not.</summary>
    private static Commitment Drilled(long hits, long misses, ulong marker, double inHits, double inMisses)
    {
        var one = One(1, 1);

        for (long settle = 0; settle < hits; settle++)
            one.Settle(
                Verdict.Hit,
                settle < hits * inHits ? Moment(1, marker) : Moment(1),
                0.1);

        for (long settle = 0; settle < misses; settle++)
            one.Settle(
                Verdict.Miss,
                settle < misses * inMisses ? Moment(1, marker) : Moment(1),
                0.1);

        return one;
    }

    // ---- the gate ----------------------------------------------------------

    [Fact]
    public void The_condition_added_is_the_one_the_hits_had()
    {
        // THE OPPOSITE OF WHAT IS EASY TO SAY, AND THE PLAN SAID IT THE WRONG WAY
        // ROUND. A conjunctive child `X and Z` keeps the firings where Z was there,
        // so Z has to be what the HITS had -- a code that is more present in the
        // misses is the right condition for a NEGATED one, which is rung two and is
        // not built. Backwards, this mints a child that is reliably wrong.
        var dials = new CommittingSettings();

        var separating = Drilled(hits: 200, misses: 200, marker: 7, inHits: 0.9, inMisses: 0.1);

        Assert.Equal(Of(7), Repair.Discriminator(separating, dials, null));

        // AND THE OTHER WAY ROUND IT REFUSES, rather than quietly adding the code
        // that selects the failures.
        var inverted = Drilled(hits: 200, misses: 200, marker: 7, inHits: 0.1, inMisses: 0.9);

        Assert.Null(Repair.Discriminator(inverted, dials, null));
    }

    [Fact]
    public void Nothing_is_repaired_before_there_is_enough_to_test()
    {
        // BELOW THE FLOOR NO TEST OF A PROPORTION HAS ANY POWER, so a gate that
        // admitted repairs there would be admitting them on nothing.
        var dials = new CommittingSettings();

        var thin = Drilled(hits: 10, misses: 5, marker: 7, inHits: 1.0, inMisses: 0.0);

        Assert.True(thin.Misses < dials.Floor);
        Assert.Null(Repair.Discriminator(thin, dials, null));

        var thick = Drilled(hits: 60, misses: 60, marker: 7, inHits: 1.0, inMisses: 0.0);

        Assert.NotNull(Repair.Discriminator(thick, dials, null));
    }

    [Fact]
    public void Searching_more_candidates_costs_more_to_clear_the_bar()
    {
        // THE CORRECTION IS THE PART THAT MATTERS. Testing four hundred candidates
        // and keeping the best clears any fixed bar on noise alone -- this is the
        // 715-names failure, and without the correction it arrives here.
        var dials = new CommittingSettings();

        var noise = new Random(1);
        var one = One(1, 1);

        // EVERY MARKER IS INDEPENDENT OF THE OUTCOME, so nothing here separates
        // anything and the honest answer is to refuse.
        for (var settle = 0; settle < 400; settle++)
        {
            var moment = new HashSet<Code> { Of(1) };

            for (ulong marker = 10; marker < 210; marker++)
                if (noise.Next(2) == 0) moment.Add(Of(marker));

            one.Settle(settle % 2 == 0 ? Verdict.Hit : Verdict.Miss, moment, 0.1);
        }

        Assert.Null(Repair.Discriminator(one, dials, null));

        // AND WITHOUT THE CORRECTION IT WOULD NOT REFUSE, which is what makes the
        // check above worth having rather than a description of a quiet world.
        var strongest = one.Separations
            .Max(seen => Repair.Divergence(
                seen.Value.InHits, one.Hits, seen.Value.InMisses, one.Misses));

        Assert.True(Normal.Tail(strongest) < dials.Alpha,
            "no candidate cleared the UNCORRECTED bar, so the correction was untested");
    }

    [Fact]
    public void The_blind_arm_draws_from_the_codes_present_in_the_failures()
    {
        // THE FAIREST CONTROL AVAILABLE. An arm drawing from every code would lose
        // to anything at all, and beating a straw man says nothing.
        var dials = new CommittingSettings { Choosing = Choosing.Present };

        var one = One(1, 1);

        for (var settle = 0; settle < 40; settle++) one.Settle(Verdict.Hit, Moment(1, 5), 0.1);
        for (var settle = 0; settle < 40; settle++) one.Settle(Verdict.Miss, Moment(1, 6), 0.1);

        var drawn = new HashSet<Code>();

        for (var draw = 0; draw < 50; draw++)
            if (Repair.Discriminator(one, dials, new Random(draw)) is { } code) drawn.Add(code);

        Assert.Equal([Of(6)], drawn);
    }

    [Fact]
    public void The_tail_and_the_divergence_are_the_statistics_they_claim_to_be()
    {
        // A BAR NOBODY CAN CHECK IS A BAR NOBODY CAN ARGUE ABOUT.
        Assert.Equal(0.5, Normal.Tail(0.0), 6);
        Assert.Equal(0.15865525, Normal.Tail(1.0), 6);
        Assert.Equal(0.02275013, Normal.Tail(2.0), 6);
        Assert.Equal(0.00134990, Normal.Tail(3.0), 6);

        Assert.Equal(1.0, Normal.Erfc(0.0), 6);

        // POSITIVE WHEN THE HITS LEAD, which is the direction repair depends on.
        Assert.True(Repair.Divergence(90, 100, 10, 100) > 0);
        Assert.True(Repair.Divergence(10, 100, 90, 100) < 0);
        Assert.Equal(0.0, Repair.Divergence(50, 100, 50, 100), 6);

        // AND NOTHING TO SAY WHERE THERE IS NOTHING TO SAY IT FROM.
        Assert.Equal(0.0, Repair.Divergence(0, 0, 5, 10));
    }

    // ---- the vote ----------------------------------------------------------

    [Fact]
    public void Many_mediocre_commitments_do_not_outvote_one_accurate_one()
    {
        // THE STRENGTH-VERSUS-ACCURACY REFUTATION ARRIVES THROUGH THE VOTE, which is
        // where nobody looks for it: a plain sum lets three commitments that are
        // right half the time beat one that is always right, so the population's
        // COUNT decides and its accuracy does not.
        var held = new Population(new CommittingSettings(), seed: 1);

        var accurate = One(1, 1);
        for (var settle = 0; settle < 60; settle++) accurate.Settle(Verdict.Hit, Moment(1), 0.1);
        held.Add(accurate);

        foreach (ulong which in (ulong[])[2, 3, 4])
        {
            var mediocre = One(0, which);

            for (var settle = 0; settle < 60; settle++)
                mediocre.Settle(settle % 2 == 0 ? Verdict.Hit : Verdict.Miss, Moment(which), 0.1);

            held.Add(mediocre);
        }

        var vote = held.Predict(held.Firing(Moment(1, 2, 3, 4)));

        Assert.Equal(Says(1), vote.Expects);
        Assert.True(vote.Margin > 0);

        // AND AT A SHARPNESS OF ONE THE FAULT IS BACK, which is what says the dial is
        // doing the work rather than decorating it.
        var plain = new Population(new CommittingSettings { Sharpness = 1.0 }, seed: 1);

        foreach (var one in held.All) plain.Add(one);

        Assert.Equal(Says(0), plain.Predict(plain.Firing(Moment(1, 2, 3, 4))).Expects);
    }

    [Fact]
    public void Nothing_firing_is_a_silence_rather_than_a_prediction()
    {
        var held = new Population(new CommittingSettings(), seed: 1);

        held.Add(One(1, 1));

        Assert.Null(held.Predict(held.Firing(Moment(2, 3))).Expects);
        Assert.Empty(held.Firing(Moment(2, 3)));
    }

    // ---- how one leaves ----------------------------------------------------

    [Fact]
    public void Where_a_general_is_as_good_the_specific_one_goes()
    {
        // THE DIRECTION THAT IS EASY TO GET BACKWARDS, AND THE PLAN HAD IT BACKWARDS.
        // If a scope and a narrower version of it are equally accurate, the narrower
        // says nothing extra, needs more evidence to say it, and covers fewer
        // moments -- so keeping it is how a population drifts toward one rule per
        // instance, which is the memorising this design is otherwise careful about.
        var held = new Population(new CommittingSettings(), seed: 1);

        var general = One(1, 1);
        var specific = One(1, 1, 2);

        for (var settle = 0; settle < 60; settle++)
        {
            general.Settle(Verdict.Hit, Moment(1), 0.1);
            specific.Settle(Verdict.Hit, Moment(1, 2), 0.1);
        }

        held.Add(general);
        held.Add(specific);

        Assert.Equal(1, held.Subsume());

        Assert.True(held.Holds(general.Identity));
        Assert.False(held.Holds(specific.Identity));
    }

    [Fact]
    public void A_specific_one_that_is_actually_better_stays()
    {
        var held = new Population(new CommittingSettings(), seed: 1);

        var general = One(1, 1);
        var specific = One(1, 1, 2);

        for (var settle = 0; settle < 60; settle++)
        {
            general.Settle(settle % 2 == 0 ? Verdict.Hit : Verdict.Miss, Moment(1), 0.1);
            specific.Settle(Verdict.Hit, Moment(1, 2), 0.1);
        }

        held.Add(general);
        held.Add(specific);

        Assert.Equal(0, held.Subsume());
        Assert.True(held.Holds(specific.Identity));
    }

    [Fact]
    public void Neither_rule_can_see_that_a_child_has_only_ever_been_right_about_one_thing()
    {
        // A LIMIT CARRIED RATHER THAN DISCOVERED LATER, and the measurement that made it
        // worth carrying is a NEGATIVE. A child right four hundred times about one
        // moment has one observation, and both rules read it as four hundred: `Weaker`
        // keeps it because a hair of advantage saves it, `Insignificant` keeps it
        // because four hundred firings make that hair significant.
        //
        // AND WEIGHING THE ADVANTAGE AGAINST THE OCCASIONS INSTEAD WAS BUILT AND
        // REFUTED. It removes exactly this child, and on the world the whole story was
        // about it removed no more children than these two do and reached the same score
        // seed for seed -- so what sinks that world is not this. See the plan's revival
        // row before building it again.
        var general = One(1, 1);
        var specific = One(1, 1, 2);

        for (ulong settle = 0; settle < 400; settle++)
        {
            general.Settle(
                settle % 5 == 0 ? Verdict.Miss : Verdict.Hit, Moment(1, 100 + settle), 0.1);

            // THE SAME MOMENT EVERY TIME, which is what a rule that has stored a corner
            // of the drawn set actually looks like from inside.
            specific.Settle(Verdict.Hit, Moment(1, 2), 0.1);
        }

        Assert.Equal(400L, specific.Fired);
        Assert.Equal(1.0, specific.Occasions, 1);
        Assert.True(general.Occasions > 100, $"the parent stands on {general.Occasions:F1}");

        foreach (var subsuming in new[] { Subsuming.Weaker, Subsuming.Insignificant })
        {
            var held = new Population(new CommittingSettings { Subsuming = subsuming }, seed: 1);

            held.Add(general);
            held.Add(specific);

            Assert.Equal(0, held.Subsume());
        }
    }

    [Fact]
    public void The_worst_go_when_there_is_no_room()
    {
        var held = new Population(new CommittingSettings { Capacity = 2 }, seed: 1);

        for (ulong which = 1; which <= 4; which++)
        {
            var one = One(1, which);

            // The higher the code, the more accurate -- so the order culling should
            // take is known rather than incidental.
            for (var settle = 0; settle < 60; settle++)
                one.Settle(
                    (ulong)(settle % 4) < which ? Verdict.Hit : Verdict.Miss, Moment(which), 0.1);

            held.Add(one);
        }

        Assert.Equal(4, held.Count);
        Assert.Equal(2, held.Cull());
        Assert.Equal(2, held.Count);

        Assert.False(held.Holds(One(1, 1).Identity));
        Assert.True(held.Holds(One(1, 4).Identity));

        // AND A CULLED COMMITMENT STOPS FIRING, or the index outlives the thing it
        // points at and a moment matches something nobody holds.
        Assert.Empty(held.Firing(Moment(1)));
    }

    [Fact]
    public void Covering_mints_one_code_at_a_time_and_only_what_is_missing()
    {
        var held = new Population(new CommittingSettings(), seed: 1);

        // NOTHING FIRED, so every gate agrees the moment was unaccounted for.
        Assert.Equal(3, held.Cover(Moment(1, 2, 3), Says(1), []));
        Assert.Equal(3, held.Count);
        Assert.Equal(0, held.Cover(Moment(1, 2, 3), Says(1), []));

        Assert.All(held.All, one => Assert.Single(one.Scope));

        // A DIFFERENT OUTCOME IS A DIFFERENT CLAIM, so the same moment mints again.
        Assert.Equal(3, held.Cover(Moment(1, 2, 3), Says(0), []));
    }

    /// <summary>
    /// <b>A FAILURE THE POPULATION ALREADY HAD AN ACCOUNT OF IS NOT SURPRISING.</b>
    /// </summary>
    /// <remarks>
    /// Something fired and proposed what arrived and was outvoted; that is a claim the
    /// population holds and weighed badly, which is repair's business. Minting there
    /// fills the population with restatements of what it already says — and on a wide
    /// front end it walks the whole <c>code → outcome</c> space.
    /// </remarks>
    [Fact]
    public void Genesis_is_gated_on_nothing_having_proposed_what_arrived()
    {
        foreach (var rule in new[] { Surprising.Unaccounted, Surprising.AnyFailure })
        {
            var held = new Population(new CommittingSettings { Surprising = rule }, seed: 1);

            // ONE COMMITMENT THAT PROPOSES OUTCOME 1, and a moment where it fires.
            held.Add(One(1, 1));

            var moment = Moment(1, 2, 3);
            var firing = held.Firing(moment);

            Assert.Single(firing);

            // THE VOTE SAID 1 AND 1 ARRIVED IS NOT A FAILURE AT ALL; what is being
            // tested is the failure where something DID propose what arrived.
            var minted = held.Cover(moment, Says(1), firing);

            if (rule == Surprising.Unaccounted)
                Assert.Equal(0, minted);
            else
                Assert.Equal(2, minted);

            // AND NEITHER RULE IS SATISFIED BY AN OUTCOME NOBODY PROPOSED, or the gate
            // would be refusing genesis outright rather than refusing restatements.
            Assert.Equal(3, held.Cover(moment, Says(7), firing));
        }
    }
}
