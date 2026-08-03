using OpenPlexus.Codes;
using OpenPlexus.Thinking;

namespace OpenPlexus.Tests;

/// <summary>
/// Fork 18's metric, scored against cases with a known right answer.
/// </summary>
public sealed class ConsequenceTests
{
    private static Code C(ulong value) => new(Modality: 1, value);

    [Fact]
    public void Knowing_the_action_beats_naming_a_different_one()
    {
        var score = new Consequence();

        // The true action's prediction lands; the counterfactual's does not.
        score.Settle([C(1)], [C(9)], [C(1)], [C(9)], [C(1)]);

        Assert.Equal(1.0, score.Knowing);
        Assert.Equal(0.0, score.Counterfactual);
        Assert.Equal(1.0, score.Gap);
    }

    [Fact]
    public void A_model_that_ignores_the_action_scores_a_gap_of_zero()
    {
        // THE COMPANION, AND IT IS THE FAILURE THE METRIC EXISTS TO CATCH. Both
        // arms predict perfectly, so precision alone would call this a triumph.
        // The gap says the action is not in the model at all.
        var score = new Consequence();

        score.Settle([C(1)], [C(1)], [C(1)], [C(9)], [C(1)]);

        Assert.Equal(1.0, score.Knowing);
        Assert.Equal(1.0, score.Counterfactual);
        Assert.Equal(0.0, score.Gap);
    }

    [Fact]
    public void A_step_where_either_arm_named_nothing_is_not_counted()
    {
        // Otherwise the gap would move with how often each arm went silent, and
        // silence is a property of the budget rather than of the model.
        var score = new Consequence();

        score.Settle([], [C(1)], [], [C(1)], [C(1)]);
        score.Settle([C(1)], [], [C(1)], [C(1)], [C(1)]);

        Assert.Equal(0, score.Asked);
    }

    [Fact]
    public void A_step_where_both_arms_named_something_is_counted()
    {
        // The companion. Without it the test above passes for an implementation
        // that counts nothing ever.
        var score = new Consequence();

        score.Settle([C(1)], [C(2)], [C(1)], [C(3)], [C(1)]);

        Assert.Equal(1, score.Asked);
    }

    [Fact]
    public void The_blind_control_is_scored_on_the_same_moment()
    {
        var score = new Consequence();

        score.Settle([C(1)], [C(2)], [C(1)], [C(1)], [C(1)]);

        Assert.Equal(1.0, score.Blind);
    }

    [Fact]
    public void Two_arms_naming_the_same_codes_are_counted_as_not_differing()
    {
        var score = new Consequence();

        score.Settle([C(1)], [C(1)], [C(1)], [C(9)], [C(1)]);

        Assert.Equal(1, score.Asked);
        Assert.Equal(0, score.Differed);
    }

    [Fact]
    public void Two_arms_naming_different_codes_are_counted_as_differing()
    {
        var score = new Consequence();

        score.Settle([C(1)], [C(2)], [C(1)], [C(9)], [C(1)]);

        Assert.Equal(1, score.Differed);
    }

    [Fact]
    public void Order_alone_does_not_count_as_differing()
    {
        // The two arms are compared as SETS. Ranking is a separate question and
        // a re-ordering is not evidence the action reached anything.
        var score = new Consequence();

        score.Settle([C(1), C(2)], [C(2), C(1)], [C(1), C(2)], [C(9)], [C(1)]);

        Assert.Equal(0, score.Differed);
    }

    [Fact]
    public void A_gap_can_be_negative_and_is_not_clamped()
    {
        // If naming the WRONG action predicted better, that is a real and very
        // interesting result. Clamping it to zero would report "the action is
        // not in the model" for something considerably stranger than that.
        var score = new Consequence();

        score.Settle([C(9)], [C(1)], [C(9)], [C(9)], [C(1)]);

        Assert.True(score.Gap < 0.0, $"gap {score.Gap}");
    }

    // ---- the third arm, which is what kills the surviving mutation ---------

    [Fact]
    public void A_difference_no_bigger_than_the_jitter_says_the_action_did_nothing()
    {
        // THE MUTATION THAT SURVIVED THREE ATTEMPTS. Delivery is concurrent, so
        // asking the SAME question twice already lands somewhere else; here the
        // counterfactual is exactly that far away and no further, which is what
        // removing the action from the broadcast produces.
        var score = new Consequence();

        score.Settle([C(1), C(2)], [C(1), C(3)], [C(1), C(4)], [C(9)], [C(1)]);

        Assert.Equal(score.Echoed, score.Apart, 6);
        Assert.Equal(0.0, score.Moved, 6);

        // AND `Differed` CANNOT SEE IT, which is the whole reason the third arm
        // exists — it reads this as the action working.
        Assert.Equal(1, score.Differed);
    }

    [Fact]
    public void A_difference_bigger_than_the_jitter_says_the_action_is_in_the_walk()
    {
        // The companion. Asking twice lands in the same place; naming a different
        // action moves both codes.
        var score = new Consequence();

        score.Settle([C(1), C(2)], [C(3), C(4)], [C(1), C(2)], [C(9)], [C(1)]);

        Assert.Equal(0.0, score.Echoed, 6);
        Assert.Equal(4.0, score.Apart, 6);
        Assert.True(score.Moved > 0.0, $"moved {score.Moved}");
    }

    [Fact]
    public void The_distance_is_symmetric_because_neither_side_is_the_reference()
    {
        // A prediction naming three codes the other missed is exactly as far away
        // as one that missed three the other named.
        var one = new Consequence();
        var other = new Consequence();

        one.Settle([C(1)], [C(1), C(2), C(3)], [C(1)], [C(9)], [C(1)]);
        other.Settle([C(1), C(2), C(3)], [C(1)], [C(1), C(2), C(3)], [C(9)], [C(1)]);

        Assert.Equal(one.Apart, other.Apart, 6);
    }
}
