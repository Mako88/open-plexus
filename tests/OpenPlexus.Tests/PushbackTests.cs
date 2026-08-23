using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Standing disagreements with things this repo currently does — <b>John's, 2026-08-13, and
/// it is the pushback rule given somewhere to live.</b>
/// </summary>
/// <remarks>
/// <para>
/// CLAUDE.md asks for pushback the moment it is seen, and says hedging is the failure mode
/// rather than overstepping. That works inside a session and evaporates between them: a
/// disagreement stated in a reply is gone by the next context window, so the same premise
/// gets accepted again by somebody who never heard the objection. This is where one is
/// written down instead.
/// </para>
/// <para>
/// <b>It is not a list of work</b>, and that is the whole distinction.
/// <see cref="OutstandingTests"/> is work somebody has decided to do, so it is RED. This is
/// work nobody has decided to do, because there is a disagreement about whether it should be
/// done at all — so it is GREEN and it PRINTS. An entry here is a claim that something
/// currently in the repo is wrong or unjustified, plus what would settle it.
/// </para>
/// <para>
/// <b>And an entry leaves by being settled, never by being withdrawn quietly.</b> Either the
/// thing changes, or a measurement says the objection was wrong and the row moves to the
/// plan's refutation table. What is forbidden is deleting a row because nobody got round to
/// it — which is the failure this exists to prevent, and is why the count is asserted.
/// </para>
/// <para>
/// <b>The entries below are mine and several may be wrong.</b> That is the intended state:
/// a disagreement nobody has tested is exactly what this is for, and a list where every
/// entry turned out to be right would mean the bar for adding was far too high.
/// </para>
/// </remarks>
public sealed class PushbackTests(ITestOutputHelper output)
{
    /// <summary>What is being disagreed with, and what would end the disagreement.</summary>
    /// <param name="With">The thing in the repo the objection is about.</param>
    /// <param name="Claim">What is being said against it, in one sentence.</param>
    /// <param name="Settles">The measurement or decision that would close it either way.</param>
    private readonly record struct Standing(string With, string Claim, string Settles);

    private static readonly Standing[] Open =
    [
        new(
            With: "Asker.Everyone",
            Claim: "A round waits for every SLOT before deciding, and nothing has ever "
                + "measured whether it needs to. The wait is justified from C2 -- a "
                + "deadline cannot tell slow from absent -- but deciding on who ANSWERED "
                + "is not a deadline, and C3 already licenses a vote over survivors. What "
                + "the barrier costs is the whole distributed latency story.",
            Settles: "An arm that decides after the first K of N slots, swept on K, with "
                + "K=N as the baseline. Flat accuracy means the barrier is free to remove; "
                + "a cliff means the wait is the price and the objection was wrong."),

        new(
            With: "The remaining stranded worlds",
            Claim: "Snake and SnakeSense are now deleted, which was this entry's own "
                + "instruction, and the delete list it came with was wrong about two more. "
                + "Homeostat is the only world here that is acted in and is exactly what "
                + "Drives needs, so deleting it would close one red entry by destroying "
                + "what another needs; Composed.Segmented feeds the unread Bind channel, "
                + "which is live. Rhythm, Motif, Latent and Clevr are decided and wired, "
                + "each by a different road -- Rhythm for a question its own entry did not "
                + "name, Motif for the one it did, Latent by a prediction this entry made "
                + "and half lost, and Clevr by the plain one, its question being exactly "
                + "what its entry said. Composed is what is left, and it is not the shape "
                + "the instinct to wire for a green guard would damage: it is the front "
                + "end's own question, so what it wants is a decision rather than a "
                + "defence.",
            Settles: "A decision per world, recorded, and read against what the world "
                + "actually holds rather than off a list of names. Clevr showed what that "
                + "buys where the world is live: a runner, and a null result worth having "
                + "-- the held-back questions answered at the weighted chance bar while the "
                + "drawn stream is memorised, which is fork 25's ceiling on scenes nobody "
                + "here generated. Composed closes the same way, and its answer arrives in "
                + "a SUCCESSOR moment, so the seam it wants is the one still open."),

        new(
            With: "DialTests.Every_arm_is_measured_on_at_least_two_worlds",
            Claim: "It attributes worlds by matching the dial's NAME in a test file, which "
                + "just produced a false pass: Choosing read as two worlds because the "
                + "walk's run result had a property spelt the same. Every other dial on "
                + "that check is attributed the same way and none has been re-verified. "
                + "And a second failure mode turned up building Preferring, which the "
                + "attribution fix would not catch: the world was attributed correctly and "
                + "is INERT for that dial. Latent holds 313 eligible scopes and lets the "
                + "naming gate speak 0.5 times in twenty asks, so both arms come back "
                + "identical to every decimal printed -- the bar reads two worlds and one "
                + "of them cannot tell the arms apart. A world that constructs is not the "
                + "same as a world that discriminates. AND THE AUDIT CAME BACK CLEAN, which "
                + "is worth as much as the hole. Across all fourteen dials, every one except "
                + "the two already on the waiting list has an arm PAIR run on two worlds or "
                + "more, so no shipped default rests on a credit like Latent's and no "
                + "baseline is in doubt. The hole is real and nothing is currently in it.",
            Settles: "Match on the enum TYPE rather than the property name, then read the "
                + "diff. If nothing else moves, this was one collision and not a method "
                + "problem. The inert half wants a world to count only where the arms "
                + "produced a DIFFERENT reading on it, which the grids already print -- what "
                + "is missing is somewhere for a grid to record it. And it wants that rather "
                + "than another static pass: the one written for the audit credited a dial "
                + "off a `see cref` in prose, which is this same text-matching fault one "
                + "level out."),

        new(
            With: "Winnow, against rung two",
            Claim: "The plan says graded codes unbound rung two's candidate set and treats "
                + "that as a known price. Nothing measures it, and rung two is not built -- "
                + "so an unbuilt rung is carrying a cost attributed to a shipped default "
                + "on the strength of an argument.",
            Settles: "Count the candidates rung two would consider under Winnow against "
                + "under Banded, on one world. It needs no learner and costs minutes."),

        new(
            With: "The one-mechanism-an-area reduction, as it is listed",
            Claim: "The list names eight brain dials to delete and every one of them is "
                + "either a control or a winner. Repairing.AfterFailure, Coarsening.Never, "
                + "Forking.Repeated and Budgeting.Children each carry a sentence in their "
                + "own XML comment saying they are kept as a control or a check, and "
                + "Surprising.AnyFailure has a refutation row saying it survives as the arm. "
                + "Subsuming.Insignificant gains about five points and doubles the sound "
                + "rules on the noisy multiplexer, and Mending.Uncovered is the best thing "
                + "measured on the clean one -- so both are the winner on a world. The two "
                + "left are entangled with open questions rather than free: "
                + "Speaking.Experienced is the only reading of whether an untested rule "
                + "should decide a round, which is a defect the widening deletion left "
                + "behind rather than took with it, and Mending.Improving is fork 45's "
                + "driver. The same reading applies "
                + "to Joining.Resolved, which the list also has going: it beats every "
                + "backward-reading arm on Roaming at four people, 0.497 against 0.167 and "
                + "0.19 marginal, and it is the only mechanism standing for a whole "
                + "architecture requirement. Executing the list as written would delete "
                + "instruments and world-winners while the tree is already at one mechanism "
                + "an area, which is the opposite of what the wide build is for.",
            Settles: "The audit above is the whole of the objection, so it closes by "
                + "somebody reading it and either re-issuing a shorter list or agreeing "
                + "the dials are already reduced. What would refute it is a per-dial "
                + "reading showing one of the five self-declared controls is not compared "
                + "against anything -- a control nothing reads is a candidate wearing a "
                + "control's clothes, and that one really should go."),
        new(
            With: "CLAUDE.md's one-more-shape rule for a losing arm",
            Claim: "It measures the wrong thing. The target is attachment -- the person "
                + "adjusting an arm is the one who built it -- and a COUNT is uncorrelated "
                + "with whether anything is being learnt. The nine-shape second hop is the "
                + "case: shapes three to nine shared one diagnosis, the parts were not in the "
                + "population, so they were one shape repeated and a count rule stopped none "
                + "of them. It also prices a one-minute ceiling the same as a runner-hour "
                + "sweep, in a repo that refuses every other bar not set by measurement. What "
                + "would work instead is a rule on DIAGNOSIS: an adjustment must name why the "
                + "last shape lost and that reason must be one no earlier failure named. It "
                + "terminates faster than the count does, because diagnoses run out before "
                + "shapes do, and the refutation table's revival column is already the same "
                + "field pointing backwards. AND THE OBJECTION WAS RAISED BY SOMEBODY AT SHAPE "
                + "TWO, which is said here rather than left for John to notice.",
            Settles: "John's call, because it is his rule and the replacement is judgement "
                + "where the count is unarguable. What would refute it is a session that "
                + "claims a fresh diagnosis for a shape whose failure repeats an earlier one "
                + "-- if that happens twice, the count was the better bar and this row goes to "
                + "the plan's table."),
    ];

    /// <summary>
    /// <b>The list is printed every run</b>, which is the only thing that makes it work.
    /// </summary>
    /// <remarks>
    /// The failure this addresses is not not-knowing, it is not-consulting — the same one
    /// <see cref="RemindingTests"/> exists for. A disagreement in a file nobody opens is a
    /// disagreement nobody has.
    /// </remarks>
    [Fact]
    public void Every_standing_disagreement_says_what_would_settle_it()
    {
        foreach (var one in Open)
        {
            output.WriteLine($"— {one.With}");
            output.WriteLine($"    {one.Claim}");
            output.WriteLine($"    SETTLES: {one.Settles}");
            output.WriteLine(string.Empty);
        }

        // An objection with no way to end it is a complaint, and a complaint in a list that
        // only shrinks by agreement is a thing nobody can ever remove. Every row has to name
        // the measurement or the decision that closes it in EITHER direction.
        Assert.All(Open, one =>
        {
            Assert.False(string.IsNullOrWhiteSpace(one.With));
            Assert.True(one.Claim.Length > 80, $"{one.With}: too short to be an argument");
            Assert.True(one.Settles.Length > 40, $"{one.With}: no way to settle it");
        });

        // And it must not become a backlog. The point is that a live disagreement gets
        // settled, not that it gets catalogued -- so the cap is low on purpose and a full
        // list means settling one before adding another.
        Assert.True(Open.Length <= 6,
            $"{Open.Length} standing disagreements. Settle one before adding another; a "
            + "list this long is a backlog rather than an objection.");
    }

    /// <summary>
    /// <b>And the count is asserted, so a row cannot leave quietly.</b>
    /// </summary>
    /// <remarks>
    /// Lowering this is the deliberate edit that says a disagreement was settled, and the
    /// commit that does it has to say which way it went — the thing changed, or the objection
    /// was refuted and belongs in the plan's table. Without the number, a row could be
    /// deleted by anybody who found it inconvenient, which is every list this repo has ever
    /// had to put a budget on.
    /// </remarks>
    [Fact]
    public void A_disagreement_leaves_by_being_settled_and_not_by_being_dropped()
    {
        Assert.Equal(6, Open.Length);
    }
}
