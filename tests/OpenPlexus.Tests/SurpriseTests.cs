using OpenPlexus.Codes;
using OpenPlexus.Learning;

namespace OpenPlexus.Tests;

/// <summary>
/// The signed prediction error: what happened unexpectedly, and <b>what was
/// expected and did not happen.</b>
/// </summary>
/// <remarks>
/// <b>THE NEGATIVE HALF IS THE ONE UNDER TEST HERE.</b> The positive half is
/// measured on <c>Rhythm</c>, where it is the traffic saving; this is about
/// absence, which broadcasts nothing and can therefore only be checked by
/// reading it.
/// </remarks>
public sealed class SurpriseTests
{
    private static Code C(ulong value) => Fixture.C(value);

    // ---- both halves of one subtraction -----------------------------------

    [Fact]
    public void What_was_expected_and_did_not_arrive_comes_back_beside_what_did()
    {
        var surprise = new Surprise();
        surprise.Expect([C(1), C(2)]);

        var moment = surprise.Residual([C(2), C(3)]);

        // C(3) arrived and nobody asked for it; C(1) was asked for and never came.
        Assert.Equal([C(3)], moment.Surprising);
        Assert.Equal([C(1)], moment.Absent);
    }

    [Fact]
    public void A_moment_nobody_predicted_is_absent_of_nothing()
    {
        // THE FLOOR, and it is what keeps `Overreach` a rate about predictions
        // rather than a count of quiet moments: expecting nothing cannot be
        // wrong, so a system with no predictor at all reads zero here and not
        // one.
        var surprise = new Surprise();

        var moment = surprise.Residual([C(1)]);

        Assert.Empty(moment.Absent);
        Assert.Equal(0, surprise.Predicted);
        Assert.Equal(0.0, surprise.Overreach);
    }

    [Fact]
    public void An_expectation_is_about_the_next_moment_only()
    {
        // The mirror of what `Expect` replacing rather than accumulating means
        // for the positive half: an expectation the world did not meet is spent
        // when the moment closes, and is not still owed at the next one.
        var surprise = new Surprise();
        surprise.Expect([C(1)]);

        surprise.Residual([C(9)]);
        surprise.Residual([C(9)]);

        // Counted once, against the moment it was made for.
        Assert.Equal(1, surprise.Absent);
        Assert.Equal(1, surprise.Predicted);
    }

    // ---- the failure the positive half cannot see -------------------------

    [Fact]
    public void A_predictor_that_names_everything_is_perfect_by_rate_and_caught_by_absence()
    {
        // THE WHOLE REASON ABSENCE IS WORTH COMPUTING. Expect the alphabet and
        // every onset is foreseen, so the machine falls permanently silent on a
        // rate of one -- which is indistinguishable from a solved world in the
        // positive half alone.
        var everything = new Surprise();
        var honest = new Surprise();

        for (var moment = 0; moment < 10; moment++)
        {
            everything.Expect(Enumerable.Range(1, 20).Select(value => C((ulong)value)));
            honest.Expect([C(7)]);

            everything.Residual([C(7)]);
            honest.Residual([C(7)]);
        }

        // Identical, and both perfect. Nothing here says one of them is a liar.
        Assert.Equal(1.0, everything.Rate);
        Assert.Equal(honest.Rate, everything.Rate);
        Assert.Equal(everything.Silent, honest.Silent);

        // AND THE NEGATIVE HALF SEPARATES THEM COMPLETELY.
        Assert.Equal(0.0, honest.Overreach);
        Assert.True(everything.Overreach > 0.9,
            $"a predictor naming twenty codes to catch one reads {everything.Overreach}");
    }

    [Fact]
    public void A_predictor_that_is_always_wrong_buys_its_silence_on_nothing()
    {
        // The other end of the same dial: the expectation is noise, so nothing
        // is ever suppressed and the mechanism only costs.
        var surprise = new Surprise();

        for (var moment = 0; moment < 10; moment++)
        {
            surprise.Expect([C(1)]);
            surprise.Residual([C(2)]);
        }

        Assert.Equal(0.0, surprise.Rate);
        Assert.Equal(1.0, surprise.Overreach);
        Assert.Equal(0, surprise.Silent);
    }

    [Fact]
    public void The_two_rates_are_not_the_same_number_read_twice()
    {
        // A HALF-RIGHT PREDICTOR, and the two quantities disagree because they
        // are asking different questions: one is about the onsets, the other is
        // about the predictions, and the sets differ at both ends.
        var surprise = new Surprise();
        surprise.Expect([C(1), C(2)]);

        surprise.Residual([C(2), C(3), C(4)]);

        // One onset of three was foreseen; one prediction of two went unmet.
        Assert.Equal(1.0 / 3.0, surprise.Rate, 10);
        Assert.Equal(0.5, surprise.Overreach, 10);
    }
}
