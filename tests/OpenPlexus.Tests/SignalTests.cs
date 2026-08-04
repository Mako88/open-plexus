using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Does a candidate signal DISCRIMINATE? — <b>the check three controllers were
/// built without, and all three failed for want of it.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE HARD PART IS THE SIGNAL, NOT THE CONTROLLER — fork 23, three times
/// over.</b> `Adaptive` reflection on `Hunger` was INVERTED: it wrote most where
/// it helped least. `Thwarted` had the right shape and swung too little against
/// the effect. `Pricing`'s controller is refuted outright. <b>Not one of the
/// three failed because the controller was wrong.</b>
/// </para>
/// <para>
/// <b>AND ALL THREE COULD HAVE BEEN CAUGHT IN AN AFTERNOON BY ASKING ONE
/// QUESTION:</b> does the number move in OPPOSITE directions between a policy
/// that works and one that does not? A signal reading the same for both cannot
/// drive anything, whatever is hung off it. A signal reading BACKWARDS drives
/// everything the wrong way, which is worse and is what `Hunger` did.
/// </para>
/// <para>
/// <b>THE WORLD SUPPLIES THE TWO POLICIES AND NOTHING HAS TO BE ARGUED.</b>
/// <see cref="Attending.Lowest"/> holds the body completely and
/// <see cref="Attending.Blind"/> does not, so any signal worth driving from must
/// tell them apart. <b>This is a cheap gate in front of an expensive mistake</b>
/// — it does not say a signal is GOOD, only that it is not provably useless.
/// </para>
/// </remarks>
public sealed class SignalTests(ITestOutputHelper output)
{
    private static HomeostatSettings World() => new();

    private static Graph.WalkSettings Dials => Fixture.Dials(stamina: 4.0);

    private const int Steps = 400;

    [Fact]
    public async Task A_signal_worth_driving_from_tells_a_good_policy_from_a_bad_one()
    {
        using var ceiling = new HomeostatRun(World(), Dials, seed: 1);
        using var random = new HomeostatRun(World(), Dials, seed: 1);
        using var still = new HomeostatRun(World(), Dials, seed: 1);

        var best = await ceiling.RunAsync(Steps, Attending.Lowest);
        var blind = await random.RunAsync(Steps, Attending.Blind);
        var idle = await still.RunAsync(Steps, Attending.Idle);

        output.WriteLine($"{"policy",-8} {"viable",8} {"improving",10}");

        foreach (var one in (HomeostatResult[])[best, blind, idle])
            output.WriteLine($"{one.Choosing,-8} {one.Viable,8:F4} {one.Improving,10:F4}");

        // THE WORLD IS DOING ITS JOB, which has to hold before the signal can be
        // asked anything. A world where the two policies score the same tells
        // nothing about a signal that reads the same on both.
        Assert.True(best.Viable > blind.Viable + 0.2,
            $"the two policies are not far enough apart to audit a signal against: "
            + $"{best.Viable:F4} and {blind.Viable:F4}");

        // AND THE SIGNAL SEPARATES THEM, IN THE RIGHT DIRECTION. `Improving` is
        // the share of transitions that helped the most-at-risk variable, so a
        // policy that holds the body must show MORE of them than one that does
        // not. If this ever reverses, the signal is `Hunger` again — inverted,
        // and worse than useless because a controller would drive on it.
        Assert.True(best.Improving > blind.Improving,
            $"`Drives.Improving` does not tell the ceiling policy from a coin toss "
            + $"({best.Improving:F4} against {blind.Improving:F4}), so nothing can "
            + "be driven from it and it should be deleted rather than kept");

        output.WriteLine(
            $"separation: {best.Improving - blind.Improving:F4} "
            + $"({(blind.Improving <= 0 ? "—" : $"{best.Improving / blind.Improving:F2}x")})");
    }

    /// <summary>
    /// The other two candidates, through the same gate — <b>and one of them
    /// passes for a reason that disqualifies it from half of what it was wanted
    /// for.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THREE INTERNAL SIGNALS WERE CLAIMED AND ONLY ONE HAD BEEN AUDITED</b>, so
    /// these are the other two. `Rhythm` supplies the good and bad policies the
    /// way `Homeostat` does: a carried window predicts what follows, and a span of
    /// nought writes no temporal cell at all in a world where nothing is ever
    /// simultaneous, so it can predict nothing.
    /// </para>
    /// <para>
    /// <b><see cref="Learning.Surprise.Rate"/> PASSES AND IS PARTLY CIRCULAR.</b> It
    /// is the share of onsets that were foreseen, so on a world scored by
    /// prediction it separates the two policies almost by definition — which makes
    /// it a fine signal for driving something ELSE (a beam, a budget, when to
    /// explore) and a poor one for driving prediction itself, where it would be
    /// measuring its own output. <b>Worth saying, because "it discriminates" is not
    /// the same claim as "it can drive this".</b>
    /// </para>
    /// <para>
    /// <b><see cref="Learning.Surprise.Overreach"/> CANNOT BE AUDITED BY THIS PAIR,
    /// AND IS ALREADY AUDITED ELSEWHERE.</b> It is a ratio over predictions MADE
    /// and the bad policy here makes none, so its nought is an empty denominator
    /// rather than a reading. <b>But `SurpriseTests` does the audit properly and
    /// always did</b>: a predictor naming twenty codes to catch one reads a rate of
    /// exactly one, identical to an honest predictor's, and `Overreach` separates
    /// them completely. <b>That is the right level for it</b> — the claim is
    /// arithmetic, so a constructed pair demonstrates it better than a world could.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task The_other_two_signals_through_the_same_gate()
    {
        var world = new RhythmSettings { Symbols = 12, Period = 5, Violations = 0.1 };

        using var carried = new RhythmRun(world, Dials, seed: 1, span: 1, surprising: true);
        using var flat = new RhythmRun(world, Dials, seed: 1, span: 0, surprising: true);

        var predicts = await carried.RunAsync(300);
        var cannot = await flat.RunAsync(300);

        output.WriteLine($"{"span",5} {"acc",8} {"rate",8} {"overreach",10}");

        foreach (var one in (RhythmResult[])[predicts, cannot])
            output.WriteLine(
                $"{one.Span,5} {one.Accuracy,8:F4} {one.Expecting,8:F4} "
                + $"{one.Overreached,10:F4}");

        // THE WORLD DOES ITS JOB FIRST, as always.
        Assert.True(predicts.Accuracy > cannot.Accuracy,
            $"a carried window no longer predicts better than none "
            + $"({predicts.Accuracy:F4} against {cannot.Accuracy:F4}), so there is "
            + "no good-and-bad pair here to audit a signal against");

        // AND THE SIGNAL SEPARATES THEM.
        Assert.True(predicts.Expecting > cannot.Expecting,
            $"`Surprise.Rate` does not tell a predictor from one that cannot "
            + $"predict ({predicts.Expecting:F4} against {cannot.Expecting:F4})");

        // `Overreach` IS NOT AUDITABLE BY THIS PAIR, and it does not need to be.
        // It is a ratio over PREDICTIONS MADE and the bad policy here makes none,
        // so its nought is an empty denominator rather than a reading -- the two
        // numbers sitting far apart says nothing about discrimination.
        //
        // ITS REAL AUDIT IS IN `SurpriseTests` AND ALWAYS WAS: a predictor naming
        // twenty codes to catch one reads a rate of exactly one, identical to an
        // honest predictor's, and `Overreach` tells them apart completely. That is
        // the right level for an arithmetic claim, and a world arm would be weaker
        // evidence rather than stronger.
        Assert.Equal(0.0, cannot.Overreached);

        Assert.True(cannot.Expecting == 0.0,
            "the arm that was meant to predict nothing has started predicting, so "
            + "the note above about an empty denominator no longer holds and "
            + "`Overreach` may now be auditable by this pair after all");
    }

    [Fact]
    public async Task And_a_signal_that_cannot_tell_them_apart_is_named_as_such()
    {
        // THE COMPANION, AND WITHOUT IT THIS FILE PASSES FOR AN AUDIT THAT ACCEPTS
        // ANYTHING. `Silent` is a real quantity every run reports and it is NOT a
        // candidate to drive from: the arms that never consult the graph are silent
        // on nothing at all by construction, so it reads identically for the best
        // policy here and the worst.
        using var ceiling = new HomeostatRun(World(), Dials, seed: 1);
        using var random = new HomeostatRun(World(), Dials, seed: 1);

        var best = await ceiling.RunAsync(Steps, Attending.Lowest);
        var blind = await random.RunAsync(Steps, Attending.Blind);

        output.WriteLine($"silent: lowest={best.Silent} blind={blind.Silent}");

        Assert.Equal(best.Silent, blind.Silent);
    }
}
