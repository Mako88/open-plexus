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
/// <b>It is not a list of work and that is the whole distinction.</b>
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
                + "which is live. Four have no decision recorded at all -- Clevr, Latent, "
                + "Motif and Rhythm -- and the instinct is still to wire them because that "
                + "turns OutstandingTests green.",
            Settles: "A decision per world, recorded, and read against what the world "
                + "actually holds rather than off a list of names. This closes by somebody "
                + "choosing for the four left, not by a measurement."),

        new(
            With: "DialTests.Every_arm_is_measured_on_at_least_two_worlds",
            Claim: "It attributes worlds by matching the dial's NAME in a test file, which "
                + "just produced a false pass: Choosing read as two worlds because the "
                + "walk's run result had a property spelt the same. Every other dial on "
                + "that check is attributed the same way and none has been re-verified.",
            Settles: "Match on the enum TYPE rather than the property name, then read the "
                + "diff. If nothing else moves, this was one collision and not a method "
                + "problem."),

        new(
            With: "ProseTests.Rate, the prose decay schedule",
            Claim: "It bills a COMMIT, and a commit is a poor proxy for work. CLAUDE.md says "
                + "to push whenever and never to hold one back, so a session that commits in "
                + "small increments pays five times the prose tax of one that commits in "
                + "lumps, for the same research. Two rules now pull opposite ways and the "
                + "cheaper one is to commit less often, which is the habit this repo spent a "
                + "workflow file learning to avoid.",
            Settles: "The count is taken and the spread is fortyfold: 78, 2, 16, 28, 65, 34, "
                + "17, 17, 6, 6, 66, 3, 24, 20, 34, 82, 25, 77, 43, 42, 23 commits per "
                + "session on this branch, clustered on gaps over two hours. So a commit is "
                + "not a fair unit and that half of the objection stands. The fix it names "
                + "does not follow, on two counts the objection did not weigh: the schedule "
                + "is at 963 against a count of 894, so it binds on nothing and the ratchet "
                + "is the live ceiling; and a commit's distance from a baseline is fixed "
                + "where a session inferred from timestamps moves under a rebase, which "
                + "trades a fair unit for an unreproducible verdict. What is left open is "
                + "whether to key on lines of prose touched, which is fair and fixed both. "
                + "This entry closes when the schedule first binds, or on that third clock."),

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
                + "Speaking.Experienced is half of the grid that answers what Widening "
                + "should be, which is one of the two decisions the reduction was waiting "
                + "on, and Mending.Improving is fork 45's driver. The same reading applies "
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
    ];

    /// <summary>
    /// <b>The list is printed every run, which is the only thing that makes it work.</b>
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
