using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The gate, the vote, and the two ways a commitment leaves.
/// </summary>
public sealed class PopulationTests(ITestOutputHelper output)
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
        // The opposite of what is easy to say, and the plan said it the wrong way
        // round. A conjunctive child `X and Z` keeps the firings where Z was there,
        // so Z has to be what the HITS had -- a code that is more present in the
        // misses is the right condition for a NEGATED one, which is rung two and is
        // not built. Backwards, this mints a child that is reliably wrong.
        var dials = new CommittingSettings();

        var separating = Drilled(hits: 200, misses: 200, marker: 7, inHits: 0.9, inMisses: 0.1);

        Assert.Equal(Of(7), Conditions.Discriminator(separating, dials, null));

        // And the other way round it refuses, rather than quietly adding the code
        // that selects the failures.
        var inverted = Drilled(hits: 200, misses: 200, marker: 7, inHits: 0.1, inMisses: 0.9);

        Assert.Null(Conditions.Discriminator(inverted, dials, null));
    }

    [Fact]
    public void Nothing_is_repaired_before_there_is_enough_to_test()
    {
        // Below the floor no test of a proportion has any power, so a gate that
        // admitted repairs there would be admitting them on nothing.
        var dials = new CommittingSettings();

        var thin = Drilled(hits: 10, misses: 5, marker: 7, inHits: 1.0, inMisses: 0.0);

        Assert.True(thin.Misses < dials.Floor);
        Assert.Null(Conditions.Discriminator(thin, dials, null));

        var thick = Drilled(hits: 60, misses: 60, marker: 7, inHits: 1.0, inMisses: 0.0);

        Assert.NotNull(Conditions.Discriminator(thick, dials, null));
    }

    [Fact]
    public void Searching_more_candidates_costs_more_to_clear_the_bar()
    {
        // The correction is the part that matters. Testing four hundred candidates
        // and keeping the best clears any fixed bar on noise alone -- this is the
        // 715-names failure, and without the correction it arrives here.
        var dials = new CommittingSettings();

        var noise = new Random(1);
        var one = One(1, 1);

        // Every marker is independent of the outcome, so nothing here separates
        // anything and the honest answer is to refuse.
        for (var settle = 0; settle < 400; settle++)
        {
            var moment = new HashSet<Code> { Of(1) };

            for (ulong marker = 10; marker < 210; marker++)
                if (noise.Next(2) == 0) moment.Add(Of(marker));

            one.Settle(settle % 2 == 0 ? Verdict.Hit : Verdict.Miss, moment, 0.1);
        }

        Assert.Null(Conditions.Discriminator(one, dials, null));

        // And without the correction it would not refuse, which is what makes the
        // check above worth having rather than a description of a quiet world.
        var strongest = one.Separations
            .Max(seen => Conditions.Divergence(
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
            if (Conditions.Discriminator(one, dials, new Random(draw)) is { } code) drawn.Add(code);

        Assert.Equal([Of(6)], drawn);
    }

    [Fact]
    public void The_tail_and_the_divergence_are_the_statistics_they_claim_to_be()
    {
        // A bar nobody can check is a bar nobody can argue about.
        Assert.Equal(0.5, Normal.Tail(0.0), 6);
        Assert.Equal(0.15865525, Normal.Tail(1.0), 6);
        Assert.Equal(0.02275013, Normal.Tail(2.0), 6);
        Assert.Equal(0.00134990, Normal.Tail(3.0), 6);

        Assert.Equal(1.0, Normal.Erfc(0.0), 6);

        // positive when the hits lead, which is the direction repair depends on.
        Assert.True(Conditions.Divergence(90, 100, 10, 100) > 0);
        Assert.True(Conditions.Divergence(10, 100, 90, 100) < 0);
        Assert.Equal(0.0, Conditions.Divergence(50, 100, 50, 100), 6);

        // And nothing to say where there is nothing to say it from.
        Assert.Equal(0.0, Conditions.Divergence(0, 0, 5, 10));
    }

    // ---- the vote ----------------------------------------------------------

    [Fact]
    public void Many_mediocre_commitments_do_not_outvote_one_accurate_one()
    {
        // The strength-versus-accuracy refutation arrives through the vote, which is
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

        // And the old second half of this check is gone with the dial it turned. It ran the
        // same population at a sharpness of one and asserted the crowd won, which was what
        // said the power was doing the work. The power was a workaround for a summed vote
        // and both are deleted -- a maximum cannot be outvoted by a count at any power, so
        // there is no setting left that brings the fault back.
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
        // The direction that is easy to get backwards, and the plan had it backwards.
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
        // A limit carried rather than discovered later, and the measurement that made it
        // worth carrying is a NEGATIVE. A child right four hundred times about one
        // moment has one observation, and both rules read it as four hundred: `Weaker`
        // keeps it because a hair of advantage saves it, `Insignificant` keeps it
        // because four hundred firings make that hair significant.
        //
        // And weighing the advantage against the occasions instead was built and
        // refuted. It removes exactly this child, and on the world the whole story was
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

        // And a culled commitment stops firing, or the index outlives the thing it
        // points at and a moment matches something nobody holds.
        Assert.Empty(held.Firing(Moment(1)));
    }

    [Fact]
    public void Covering_mints_one_code_at_a_time_and_only_what_is_missing()
    {
        var held = Varied(1, 2, 3);

        // NOTHING FIRED, so every gate agrees the moment was unaccounted for.
        Assert.Equal(3, held.Genesis(Moment(1, 2, 3), Says(1), []));
        Assert.Equal(3, held.Count);
        Assert.Equal(0, held.Genesis(Moment(1, 2, 3), Says(1), []));

        Assert.All(held.All, one => Assert.Single(one.Scope));

        // A different outcome is a different claim, so the same moment mints again.
        Assert.Equal(3, held.Genesis(Moment(1, 2, 3), Says(0), []));
    }

    /// <summary>
    /// <b>A failure the population already had an account of is not surprising.</b>
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
            var held = Varied(new CommittingSettings { Surprising = rule }, 1, 2, 3);

            // ONE COMMITMENT THAT PROPOSES OUTCOME 1, and a moment where it fires.
            held.Add(One(1, 1));

            var moment = Moment(1, 2, 3);
            var firing = held.Firing(moment);

            Assert.Single(firing);

            // The vote said 1 and 1 arrived is not a failure at all; what is being
            // tested is the failure where something DID propose what arrived.
            var minted = held.Genesis(moment, Says(1), firing);

            if (rule == Surprising.Unaccounted)
                Assert.Equal(0, minted);
            else
                Assert.Equal(2, minted);

            // And neither rule is satisfied by an outcome nobody proposed, or the gate
            // would be refusing genesis outright rather than refusing restatements.
            Assert.Equal(3, held.Genesis(moment, Says(7), firing));
        }
    }

    /// <summary>
    /// <b>A perfect record at the miss floor is already significant.</b> So a gate reading one
    /// has nothing left to refuse.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It outlived the mechanism it was written for.</b> <c>Widening.Significant</c> was
    /// to refuse a clean record that is not significant against the base rate, by the pooled
    /// two-proportion z repair already owns and at the same
    /// <see cref="CommittingSettings.Alpha"/> — <i>zero misses over twenty firings is not the
    /// same claim as zero over four hundred</i>. It was built and came back bit-identical to
    /// the ungated arm on all four cells of its grid, and widening went with it.
    /// </para>
    /// <para>
    /// <b>The reason is arithmetic rather than a world, which is why the check stays.</b> A
    /// perfect record over n firings clears a one-sided bar at <c>alpha</c> for every base
    /// rate below roughly <c>n / (n + 2.71)</c>. At the shipped floor of twenty that is 0.88,
    /// and the most skewed world on this bench draws four in five — so ANY future gate
    /// charging a clean record for its significance is inert here before it is built.
    /// </para>
    /// <para>
    /// <b>So the revival condition is a number rather than a hope.</b> And this is what would
    /// spot it. The day a world's commonest outcome passes the boundary below, or the day
    /// the floor drops far enough to move the boundary under a world already here, that gate
    /// stops being inert. The plan's widening rows cite this test for it.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_clean_record_at_the_floor_is_significant_against_every_world_here()
    {
        var dials = new CommittingSettings();

        // A clean record at exactly the floor, which is the thinnest one such a gate can
        // ever see. Anything it would refuse it would refuse here first.
        var thinnest = dials.Floor;

        double Refused(double rate)
        {
            const long Trials = 1_000_000L;

            return Normal.Tail(
                Conditions.Ahead(thinnest, thinnest, (long)(rate * Trials), Trials));
        }

        foreach (var rate in new[] { 0.5, 0.8, 0.85, 0.9, 0.95 })
            output.WriteLine($"  base rate {rate:F2} -> p={Refused(rate):F4}");

        // The worlds this bench has, named rather than assumed. `Multiplexer`'s answer is a
        // data bit, so its commonest outcome IS the skew, and 0.8 is the steepest tilt any
        // grid here runs.
        Assert.True(Refused(0.5) <= dials.Alpha);
        Assert.True(Refused(0.8) <= dials.Alpha,
            "the steepest world on this bench now refuses a clean record at the floor, so a "
            + "gate charging one for its significance is no longer inert -- see the plan's "
            + "widening revival rows");

        // AND WHERE IT WOULD BITE, so the check fails from BOTH sides. A bar that refuses
        // nothing anywhere is not a bar, and asserting only the inert half would pass for
        // free if `Ahead` ever returned a constant.
        Assert.True(Refused(0.95) > dials.Alpha);

        // The boundary itself, which is the number the revival row cites. Below it the
        // floor has already paid for the significance; above it a gate has something to
        // say. It moves with the floor and with nothing else.
        var boundary = thinnest / (thinnest + 2.71);

        output.WriteLine($"boundary at floor {thinnest}: {boundary:F4}");

        Assert.True(Refused(boundary - 0.02) <= dials.Alpha);
        Assert.True(Refused(boundary + 0.02) > dials.Alpha);
    }

    /// <summary>
    /// A population that has already seen these codes come and go.
    /// </summary>
    /// <param name="dials">Every number the machinery is allowed to have.</param>
    /// <param name="codes">The codes to establish as varying.</param>
    /// <remarks>
    /// <para>
    /// <b>Genesis will not root on a code that has never been absent.</b> So a population
    /// asked to cover before it has witnessed anything mints NOTHING. That is the
    /// gate working rather than a fault — but a test calling <see cref="Population.Genesis"/>
    /// straight out of the constructor is asking it to root on codes it has seen exactly
    /// once each, and it correctly declines.
    /// </para>
    /// <para>
    /// <b>So the precondition is established rather than assumed.</b> One moment holding
    /// the codes and one holding none of them is all it takes: after the second, every one
    /// of them has been absent, and every one is eligible.
    /// </para>
    /// </remarks>
    private static Population Varied(CommittingSettings dials, params ulong[] codes)
    {
        var held = new Population(dials, seed: 1);

        held.Witness(Moment(codes));
        held.Witness(new HashSet<Code>());

        return held;
    }

    private static Population Varied(params ulong[] codes) => Varied(new CommittingSettings(), codes);
}
