using OpenPlexus.Commitments;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// You build the thing and it is ON. There is no switch to turn it off.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's rule, 2026-08-04, and it is stricter than the dead-code budget.</b>
/// Dead code is code nothing calls. This is about code everything calls and
/// nothing RUNS — built, tested, referenced, and switched off, so it passes every
/// check here while doing nothing for anybody. <c>Surprise</c> was in that state
/// for weeks: unit-tested, measured as an arm, and wired into exactly one world.
/// </para>
/// <para>
/// <b>A toggle between two named alternatives is fine. An on/off flag is not.</b>
/// <see cref="Mending"/> chooses which failures a repair may run on;
/// <see cref="Repairing"/> chooses when it runs. Both ends do something, and the
/// comparison is between two real behaviours. A <c>bool</c> compares a behaviour against its
/// own absence, which means the absence is a permanent resident of the code.
/// </para>
/// <para>
/// <b>So the lifecycle is: build it, measure it against its neighbour, delete the
/// loser.</b> That is the rule this project already applies to arms — see the
/// revival table, and <c>Attending.Marked</c>, which was collapsed the same day
/// this was written. What changes here is that a flag no longer gets to sit in the
/// middle of that lifecycle indefinitely, which is where all six of the ones below
/// were living.
/// </para>
/// <para>
/// <b>And the disguised ones count.</b> A nullable dial whose null means "off", or
/// a number whose zero means "off", is the same flag wearing a different type —
/// a nullable cap whose null means <i>unbounded</i> is the sharpest case, and the walk
/// carried three of them. They are listed by hand because no reflection can tell
/// "null is off" from "null is a sensible absence".
/// </para>
/// </remarks>
public sealed class FlagTests(ITestOutputHelper output)
{
    /// <summary>
    /// On/off flags still on the brain, each waiting to be turned on for good or
    /// deleted as the loser.
    /// </summary>
    /// <remarks>
    /// <b>THIS LIST MAY ONLY SHRINK, and every exit from it is one of exactly two
    /// doors.</b> Either the thing wins and becomes unconditional — the flag goes
    /// and the behaviour stays — or it loses and the whole mechanism goes with a
    /// revival row. Nothing leaves this list by being renamed.
    /// </remarks>
    /// <remarks>
    /// <b>Sorted by what the evidence already says.</b> Because "turn everything on"
    /// is three different jobs and only one of them is easy. Checked against
    /// the refutation table on 2026-08-04: NONE of these is a refuted loser. Every
    /// arm the table refuted was already deleted from the code, and what is left
    /// there is doc comments the table itself cites.
    /// </remarks>
    private static readonly HashSet<string> Switches = new(StringComparer.Ordinal)
    {
        // ---- The one that is a trade and not a winner or a loser ------------
        //
        // John's call, 2026-08-04: everything else went ON and the way to switch it
        // off went with it. `Reflect` stays a toggle because fork 21's own note
        // says why -- the risk is that the system learns its own hallucinations,
        // confirmation bias literally, and null is the control that says whether it
        // is doing that. The threshold has no signal yet (fork 23: `Hunger`
        // inverted, `Thwarted` swung too little), so this is a mechanism that
        // cannot yet be told when to stop, which is not the same as one that lost.
        "Reflect",
    };

    [Fact]
    public void No_dial_is_an_on_off_switch()
    {
        // The brain's settings, which are now all of them. A second settings record
        // arrived once with a control arm as a `bool` that this file could not see --
        // the same blind spot the dial census has already been caught in. One record
        // is not a reason to stop looking; it is a reason this line is worth reading.
        var flags = typeof(CommittingSettings).GetProperties()
            .Where(one => one.PropertyType == typeof(bool))
            .Select(one => one.Name)
            .ToList();

        output.WriteLine(
            $"{flags.Count} raw boolean dial(s): {string.Join(", ", flags.Order(StringComparer.Ordinal))}");

        // A boolean dial is detectable and therefore enforceable, which is why it
        // is asserted separately from the hand-kept list below. Anything new fails
        // here the moment it is added.
        var fresh = flags.Except(Switches, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToList();

        Assert.True(fresh.Count == 0,
            $"new on/off flag(s): {string.Join(", ", fresh)}. Build it and leave it "
            + "ON, or make it a toggle between two NAMED alternatives that both do "
            + "something. A `bool` compares a behaviour against its own absence, "
            + "and the absence then lives in the code forever.");
    }

    [Fact]
    public void The_number_of_switches_only_ever_falls()
    {
        // The budget, at the count on the day the rule was made. Every exit is a
        // decision: won and became unconditional, or lost and was deleted. There
        // is no edit that should raise this, because raising it means somebody
        // built something and left a way to not run it.
        Assert.True(Switches.Count <= 1,
            $"{Switches.Count} on/off flags, which is more than when this rule was "
            + "made. Something was built with a way to switch it off.");

        output.WriteLine(
            $"{Switches.Count} switches outstanding. Each leaves by winning "
            + "(the flag goes, the behaviour stays) or losing (the mechanism goes, "
            + "a revival row stays).");
    }
}
