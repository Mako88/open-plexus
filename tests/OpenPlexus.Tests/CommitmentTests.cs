using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;

namespace OpenPlexus.Tests;

/// <summary>
/// The primitive: what it is called, when it fires, and what a settlement does to it.
/// </summary>
public sealed class CommitmentTests
{
    private static Code Of(ulong value) => new(1, value);

    private static Commitment One(params ulong[] scope) =>
        new([.. scope.Select(Of)], Of(99));

    private static HashSet<Code> Moment(params ulong[] codes) => [.. codes.Select(Of)];

    // ---- what it is called -------------------------------------------------

    [Fact]
    public void A_name_comes_from_the_scope_and_not_from_the_path_that_reached_it()
    {
        // The plan said parent plus the condition added, and that gives one scope two
        // names. Two nodes adding the same pair of codes in a different order would
        // reach the same commitment and call it two things, so it would be its own
        // sibling -- on top of the sibling problem that is already expected.
        Assert.Equal(One(1, 2, 3).Identity, One(3, 1, 2).Identity);
        Assert.Equal(One(1, 2).Identity, One(2, 1, 1).Identity);

        Assert.NotEqual(One(1, 2).Identity, One(1, 2, 3).Identity);
        Assert.NotEqual(One(1, 2).Identity, new Commitment([Of(1), Of(2)], Of(98)).Identity);

        // And a scope may not be the prefix of another and reach the same name, which
        // is what folding the length in first is for.
        Assert.NotEqual(
            Commitment.Name([Of(1), Of(2)], Of(3)),
            Commitment.Name([Of(1)], Of(2)));
    }

    [Fact]
    public void It_is_named_in_the_modality_a_scope_can_hold()
    {
        // A COMMITMENT'S IDENTITY IS A `Code`, which is why metacognition and
        // abstraction need no new machinery: it can sit inside another scope.
        Assert.Equal(Commitment.Committed, One(1).Identity.Modality);

        var about = new Commitment([One(1).Identity], Of(50));

        Assert.True(about.Fires(new HashSet<Code> { One(1).Identity }));
    }

    [Fact]
    public void An_empty_scope_is_refused()
    {
        // A commitment with no scope fires always, which is not a commitment.
        Assert.Throws<ArgumentException>(() => new Commitment([], Of(1)));
        Assert.Throws<ArgumentException>(() => Commitment.Name([], Of(1)));
    }

    // ---- when it fires -----------------------------------------------------

    [Fact]
    public void It_fires_when_its_scope_is_a_subset_and_not_otherwise()
    {
        Assert.True(One(1, 2).Fires(Moment(1, 2, 3)));
        Assert.True(One(1, 2).Fires(Moment(1, 2)));
        Assert.False(One(1, 2).Fires(Moment(1, 3)));
        Assert.False(One(1, 2).Fires(Moment()));
    }

    [Fact]
    public void Narrowing_is_saying_everything_the_other_says_and_more()
    {
        Assert.True(One(1, 2).Narrows(One(1)));
        Assert.False(One(1).Narrows(One(1, 2)));
        Assert.False(One(1, 2).Narrows(One(1, 2)));
        Assert.False(One(1, 3).Narrows(One(2)));

        // And never across two expectations. A scope that says something else is not
        // a narrower version of this, it is a different claim.
        Assert.False(new Commitment([Of(1), Of(2)], Of(98)).Narrows(One(1)));
    }

    // ---- what a settlement does --------------------------------------------

    [Fact]
    public void A_hit_and_a_miss_move_counters_that_only_rise()
    {
        var one = One(1);

        one.Settle(Verdict.Hit, Moment(1, 5), 0.1);
        one.Settle(Verdict.Miss, Moment(1, 6), 0.1);
        one.Settle(Verdict.Hit, Moment(1, 5), 0.1);

        Assert.Equal(2, one.Hits);
        Assert.Equal(1, one.Misses);
        Assert.Equal(3, one.Fired);
        Assert.Equal(2 / 3.0, one.Reliability, 6);
    }

    [Fact]
    public void An_abstain_moves_nothing_but_its_own_counter()
    {
        // C3 requires this, and a run in one process cannot reach it -- nothing here
        // can die, so this is the only place the path is exercised at all. Without
        // it the counter reads zero for the reason a check reads zero when it is
        // wired and unable to fire.
        var one = One(1);

        one.Settle(Verdict.Hit, Moment(1, 5), 0.1);

        var accuracy = one.Accuracy;

        one.Settle(Verdict.Abstain, Moment(1, 7), 0.1);
        one.Settle(Verdict.Abstain, Moment(1, 7), 0.1);

        Assert.Equal(2, one.Abstains);
        Assert.Equal(1, one.Hits);
        Assert.Equal(0, one.Misses);
        Assert.Equal(1, one.Seen);
        Assert.Equal(accuracy, one.Accuracy);

        // And it leaves the tally alone. A settlement that could not say is not
        // evidence about which code separates anything, so letting it in would make
        // repair depend on how often the network was unwell.
        Assert.False(one.Separations.ContainsKey(Of(7)));
    }

    [Fact]
    public void The_tally_is_over_what_came_along_and_never_over_the_scope()
    {
        // Every scope code is present in every firing by definition, so a tally over
        // the scope separates nothing at all -- it would be the code repair picks
        // every time, and it would add a condition already required.
        var one = One(1);

        one.Settle(Verdict.Hit, Moment(1, 5), 0.1);
        one.Settle(Verdict.Miss, Moment(1, 6), 0.1);
        one.Settle(Verdict.Miss, Moment(1, 6), 0.1);

        Assert.False(one.Separations.ContainsKey(Of(1)));

        Assert.Equal(new Separation { InHits = 1 }, one.Separations[Of(5)]);
        Assert.Equal(new Separation { InMisses = 2 }, one.Separations[Of(6)]);
    }

    [Fact]
    public void The_local_estimate_starts_where_the_evidence_is_and_then_forgets()
    {
        // Widrow-hoff from zero says a commitment right once is a tenth right, so a
        // fresh one is indistinguishable from a refuted one and loses every vote it
        // should win. Averaging until there is enough to forget is XCS's own
        // practice and introduces no number `Recency` did not already fix.
        var one = One(1);

        one.Settle(Verdict.Hit, Moment(1), 0.1);
        Assert.Equal(1.0, one.Accuracy, 6);

        one.Settle(Verdict.Miss, Moment(1), 0.1);
        Assert.Equal(0.5, one.Accuracy, 6);

        // And it tracks where the lifetime average cannot. After a long run of hits
        // and then a world that changed, the two answers come apart -- which is the
        // whole of why both are kept.
        for (var settle = 0; settle < 200; settle++) one.Settle(Verdict.Hit, Moment(1), 0.1);
        for (var settle = 0; settle < 40; settle++) one.Settle(Verdict.Miss, Moment(1), 0.1);

        Assert.True(one.Accuracy < 0.2, $"the local estimate did not track: {one.Accuracy:F3}");
        Assert.True(one.Reliability > 0.8, $"the lifetime average tracked: {one.Reliability:F3}");
    }

    [Fact]
    public void A_firing_is_not_an_observation_when_the_same_moment_keeps_coming_back()
    {
        // The one measure of generality here that is not built from accuracy. Every
        // repair gate tried so far reads observed accuracy or observed failure, and on
        // a world whose drawn set can be memorised all of them are fooled the same way.
        // A rule right four hundred times about ONE picture has fired four hundred
        // times and seen one thing, and nothing in the machine was counting that.
        var repeated = One(1);
        var varied = One(1);

        for (ulong settle = 0; settle < 400; settle++)
        {
            repeated.Settle(Verdict.Hit, Moment(1, 2), 0.1);
            varied.Settle(Verdict.Hit, Moment(1, 100 + settle), 0.1);
        }

        // Identical by every counter the design already had, which is the point.
        Assert.Equal(400L, repeated.Fired);
        Assert.Equal(400L, varied.Fired);
        Assert.Equal(repeated.Reliability, varied.Reliability, 6);

        Assert.Equal(1.0, repeated.Occasions, 1);

        // And the register saturates rather than counting, which costs nothing: a
        // proportion resting on two hundred independent readings has all the power it
        // will ever need, and what this has to tell apart is A FEW from many.
        Assert.True(varied.Occasions > 100,
            $"four hundred distinct moments read as {varied.Occasions:F1} occasions");
    }

    [Fact]
    public void And_the_occasions_are_counted_the_same_however_the_moment_is_walked()
    {
        // a moment is a set, so two walks of it must reach one word -- the same
        // property `Name` needs from a scope, and the reason the fold here is an XOR
        // rather than a running hash.
        var one = One(1);
        var other = One(1);

        one.Settle(Verdict.Hit, Moment(1, 7, 4), 0.1);
        other.Settle(Verdict.Hit, Moment(4, 1, 7), 0.1);

        Assert.Equal(one.Occasions, other.Occasions, 6);

        one.Settle(Verdict.Hit, Moment(1, 4, 7), 0.1);

        // And a moment already seen is not a second one.
        Assert.Equal(other.Occasions, one.Occasions, 6);
    }

    [Fact]
    public void Forgetting_drops_the_tally_and_keeps_what_decides_whether_it_fires()
    {
        // the table is what blows up, not the commitment. Fork 31 is whether it can
        // go and come back without changing what fires; this is the half that is
        // built -- that dropping it changes nothing a moment can see.
        var one = One(1, 2);

        one.Settle(Verdict.Hit, Moment(1, 2, 5), 0.1);
        Assert.NotEmpty(one.Separations);

        one.Forget();

        Assert.Empty(one.Separations);
        Assert.Equal(1, one.Hits);
        Assert.True(one.Fires(Moment(1, 2, 5)));
        Assert.Equal(One(1, 2).Identity, one.Identity);
    }

    [Fact]
    public void A_scope_is_held_in_order_so_two_of_them_can_be_compared()
    {
        var one = new Commitment([Of(9), Of(2), Of(5)], Of(1));

        Assert.Equal<IEnumerable<Code>>([Of(2), Of(5), Of(9)], one.Scope);
        Assert.Equal(ImmutableArray.Create(Of(2), Of(5), Of(9)).ToList(), one.Scope.ToList());
    }
}
