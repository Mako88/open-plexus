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
