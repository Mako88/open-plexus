using OpenPlexus.Graph;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// You build the thing and it is ON. There is no switch to turn it off.
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S RULE, 2026-08-04, AND IT IS STRICTER THAN THE DEAD-CODE BUDGET.</b>
/// Dead code is code nothing calls. This is about code everything calls and
/// nothing RUNS — built, tested, referenced, and switched off, so it passes every
/// check here while doing nothing for anybody. <c>Surprise</c> was in that state
/// for weeks: unit-tested, measured as an arm, and wired into exactly one world.
/// </para>
/// <para>
/// <b>A TOGGLE BETWEEN TWO NAMED ALTERNATIVES IS FINE. AN ON/OFF FLAG IS NOT.</b>
/// <see cref="Pricing"/> chooses which end of an edge weighs it; <see cref="Toll"/>
/// chooses what a hop is charged from. Both ends do something, and the comparison
/// is between two real behaviours. A <c>bool</c> compares a behaviour against its
/// own absence, which means the absence is a permanent resident of the code.
/// </para>
/// <para>
/// <b>SO THE LIFECYCLE IS: BUILD IT, MEASURE IT AGAINST ITS NEIGHBOUR, DELETE THE
/// LOSER.</b> That is the rule this project already applies to arms — see the
/// revival table, and <c>Attending.Marked</c>, which was collapsed the same day
/// this was written. What changes here is that a flag no longer gets to sit in the
/// middle of that lifecycle indefinitely, which is where all six of the ones below
/// were living.
/// </para>
/// <para>
/// <b>AND THE DISGUISED ONES COUNT.</b> A nullable dial whose null means "off", or
/// a number whose zero means "off", is the same flag wearing a different type —
/// <see cref="WalkSettings.Row"/>, <see cref="WalkSettings.Reflect"/> and
/// <see cref="WalkSettings.Span"/> are all switches. They are listed by hand
/// because no reflection can tell "null is off" from "null is a sensible absence".
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
    /// <b>SORTED BY WHAT THE EVIDENCE ALREADY SAYS, because "turn everything on"
    /// is three different jobs and only one of them is easy.</b> Checked against
    /// the refutation table on 2026-08-04: NONE of these is a refuted loser. Every
    /// arm the table refuted was already deleted from the code, and what is left
    /// there is doc comments the table itself cites.
    /// </remarks>
    private static readonly HashSet<string> Switches = new(StringComparer.Ordinal)
    {
        // ---- WON ALREADY, SO THE FLAG SHOULD GO AND THE BEHAVIOUR STAY ------
        //
        // `Doubt` repairs the rare-coincidence defect and is free where it is not
        // needed -- measured identical to ten places on the clean world. `Row` was
        // proved free under a heavy tail and near-free on a flat one, and is the
        // difference between possible and impossible on text. `Names` is the
        // table's own "REVIVED at one code".
        //
        // THE COST OF CASHING THESE IN IS THAT EVERY BASELINE MOVES, which is why
        // they are still here rather than done in passing.
        "Doubt", "Row", "Names",

        // ---- WORLD-DEPENDENT, WHICH IS NOT THE SAME AS OFF ------------------
        //
        // `Span` is refuted on snake and bAbI and is THE WHOLE TASK on `Rhythm`,
        // where nothing overlaps and there are no temporal cells without it. So it
        // is not a loser and it is not a winner; it is a claim about the STREAM.
        // Deleting it would take `Rhythm` with it. `Kinds` only means anything
        // beside it.
        //
        // AND THIS PAIR IS WHAT YESTERDAY'S REGRESSION WAS MADE OF: a per-world
        // default is a scattered piece of KNOWLEDGE, not just a scattered knob.
        "Span", "Kinds",

        // ---- UNRESOLVED, AND EACH NEEDS ITS TOGGLE RUN ----------------------
        //
        // `IncludeEmpty` is the table's "revived, no clear winner since".
        // `Reflect` is recorded as a trade. `Surprising` and `Gated` bite on
        // `Rhythm` and are inert on text. `Chunking`, `Recent`, `Budget` and
        // `Foresight` have each been measured on exactly one world.
        "Surprising", "Gated", "Chunking", "Recent", "IncludeEmpty", "Reflect",
        "Budget", "Foresight",
    };

    [Fact]
    public void No_dial_is_an_on_off_switch()
    {
        var flags = typeof(WalkSettings)
            .GetProperties()
            .Where(one => one.PropertyType == typeof(bool))
            .Select(one => one.Name)
            .ToList();

        output.WriteLine(
            $"{flags.Count} raw boolean dial(s): {string.Join(", ", flags.Order(StringComparer.Ordinal))}");

        // A BOOLEAN DIAL IS DETECTABLE AND THEREFORE ENFORCEABLE, which is why it
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
        // THE BUDGET, AT THE COUNT ON THE DAY THE RULE WAS MADE. Every exit is a
        // decision: won and became unconditional, or lost and was deleted. There
        // is no edit that should raise this, because raising it means somebody
        // built something and left a way to not run it.
        Assert.True(Switches.Count <= 13,
            $"{Switches.Count} on/off flags, which is more than when this rule was "
            + "made. Something was built with a way to switch it off.");

        output.WriteLine(
            $"{Switches.Count} switches outstanding. Each leaves by winning "
            + "(the flag goes, the behaviour stays) or losing (the mechanism goes, "
            + "a revival row stays).");
    }
}
