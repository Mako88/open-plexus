using System.Text.RegularExpressions;
using OpenPlexus.Codes;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The outstanding work, as tests that are RED until it is done — <b>John's, 2026-08-13, and
/// it is a to-do list that fails the build.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THESE ARE SUPPOSED TO BE FAILING. DO NOT DELETE THEM AND DO NOT WEAKEN THEM.</b> Each
/// one closes by the work being done, and every one of them computes the state rather than
/// asserting a constant — so none can be satisfied by editing this file. If you have arrived
/// at a red suite and this is what is red, this file is the priority and not a regression.
/// </para>
/// <para>
/// <b>THE IDEA IS ALREADY THIS REPO'S AND IT IS RECORDED AS HAVING WORKED.</b>
/// <see cref="ShapeTests"/> says of its own list: <i>it began as fifteen dials across seven
/// worlds and was a to-do list that failed the build, which is the only kind that gets
/// done.</i> It reached nought on the day it was written. What is new here is doing that
/// deliberately rather than as a side effect of one guard.
/// </para>
/// <para>
/// <b>AND THE COST IS REAL, WHICH IS WHY THE LIST IS SHORT.</b> A permanently red suite
/// destroys the signal — <i>the red set was not stable, and a suite whose failures come and
/// go cannot be the baseline anything is measured against.</i> So the rule for adding here
/// is stricter than for adding anywhere else: an entry must be work somebody has decided to
/// do, computable without judgement, and closeable. A question nobody has answered yet
/// belongs in the plan as <b>OPEN</b>, not here.
/// </para>
/// </remarks>
public sealed class OutstandingTests(ITestOutputHelper output)
{
    /// <summary>
    /// <b>ELEVEN WORLDS HAVE NO RUNNER — 23 ENTRIES, BECAUSE MEMBERS COUNT TOO.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every one was driven by a <c>*Run</c> in <c>Worlds/</c> and every one of those was the
    /// walk's, so the world data survived and the thing that turned it into a measurement did
    /// not. The commitment side's generic runner is <c>Trial</c> and nothing is wired to it.
    /// <b>The count is 23 rather than 11 because a world's unreachable MEMBERS are listed
    /// beside its type</b> — that second number is how much of each world is dead rather
    /// than merely undriven, and it is what tells a `Trial` from a deletion.
    /// </para>
    /// <para>
    /// <b>AND A `Trial` IS ONLY HALF THE ANSWER, WHICH IS THE PART THAT WAS FIRST WRITTEN
    /// WRONG.</b> <see cref="RemindingTests"/> prints the other half: an isolating world is
    /// DELETED when its question closes, and worlds accumulate exactly as dials do. So each
    /// name below is one of two things and this test does not care which — it closes when the
    /// entry leaves <see cref="DeadCodeTests"/>, by either road.
    /// </para>
    /// <para>
    /// <b>SOME ARE OBVIOUSLY LIVE AND SOME ARE OBVIOUSLY NOT.</b> <c>Senses</c> is the
    /// cross-modal pairing nothing has ever run, <c>Motif</c> is rung five's redundancy
    /// manufactured on purpose, <c>Rhythm</c> is rung three's, <c>Latent</c> is fork 39's.
    /// <c>Snake</c> and <c>SnakeSense</c> were built for prediction-conditional-on-action,
    /// which is forks 18 and 20 — both settled, both deleted with the walk.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_world_is_either_run_by_something_or_gone()
    {
        var stranded = DeadCodeTests.StillStranded;

        output.WriteLine(
            stranded.Count == 0
                ? "every world is reachable"
                : $"{stranded.Count} stranded: {string.Join(", ", stranded)}");

        Assert.True(stranded.Count == 0,
            $"{stranded.Count} entries across eleven worlds nothing can run: "
            + $"{string.Join(", ", stranded)}. Give the world a `Trial`, or DELETE it "
            + "because its question closed — then take its entry off `DeadCodeTests`. "
            + "This test is red on purpose and closes on that edit, not on this file.");
    }

    /// <summary>
    /// <b>THREE FRONT-END CHANNELS HAVE NO READER SINCE THE WALK WENT.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IQuantizer{TObservation}.Bind"/>, <see cref="IQuantizer{TObservation}.Fleeting"/>
    /// and <see cref="IQuantizer{TObservation}.Forced"/> are implemented by every front end
    /// and read by nothing. The walk's occasion was their only consumer; a commitment's scope
    /// is a SET of codes with nowhere to put a group, a lifetime or an intervention.
    /// </para>
    /// <para>
    /// <b>A CHANNEL WITH NO FAR END IS THE SHAPE THIS REPO KEEPS FINDING READ AS BUILT</b> —
    /// <c>Surprise</c> and <c>Abstain</c> were both found wired and unable to fire, and
    /// <i>promiscuous on purpose</i> meant exhaustive for the life of the repo. Three world
    /// dials feed these and would go with them: <c>Binding.Segmented</c>,
    /// <c>Binding.Fleeting</c>, <c>Clevr.Segmented</c>, <c>Composed.Segmented</c>.
    /// </para>
    /// <para>
    /// <b>CLOSES EITHER WAY, AND THE TWO ARE REAL ALTERNATIVES.</b> Wire one to something
    /// that acts on it — rung four's binding is the obvious home for <c>Bind</c> — or delete
    /// it with the dials that feed it. <c>Order</c> is the one of the four with a reader and
    /// is what rung three is made of, so it is not counted here.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_front_end_channel_is_read_by_nothing()
    {
        // WHERE A READER WOULD BE, RATHER THAN WHERE A FORWARD WOULD BE -- and scoping it
        // this way is what makes the check mean something. Everything in `Codes/` either
        // DECLARES these or passes them straight through: `Compound` merges what its senses
        // said, `Alternating` hands its inner one's answer back, and a front end returning
        // its own field is not a consumer. A reader is something that ACTS on the channel,
        // and that lives where the learner is.
        //
        // AND IT REPLACED A LIST OF FILENAMES TO IGNORE, which was the fragile version: a
        // new forwarder in `Codes/` would have turned this green while nothing had changed,
        // and an unrelated `.Bind(` anywhere in `src` would have done the same.
        var sources = new[] { "Commitments", "Machines" }
            .SelectMany(where => Directory.GetFiles(
                Path.Combine(Tree.Repo(), "src", "OpenPlexus", where),
                "*.cs",
                SearchOption.AllDirectories))
            .Select(File.ReadAllText)
            .ToList();

        var unread = new[] { "Bind", "Fleeting", "Forced" }
            .Where(channel => !sources.Any(text =>
                Regex.IsMatch(text, $@"\.{channel}\s*\(")))
            .ToList();

        output.WriteLine(
            unread.Count == 0
                ? "every front-end channel has a consumer"
                : $"{unread.Count} with no reader: {string.Join(", ", unread)}");

        Assert.True(unread.Count == 0,
            $"{string.Join(", ", unread)} on `IQuantizer` are implemented everywhere and read "
            + "nowhere in `Commitments/` or `Machines/`. Wire one to something that ACTS on "
            + "it, or delete it together with the world dials that feed it. This test is red "
            + "on purpose.");
    }

    /// <summary>
    /// <b>`Drives` IS THE LAST IDEA OWED OFF `csharp`, AND THAT BRANCH IS NOT COMING BACK.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Four things were ranked worth taking before the walk went. Three are done — the
    /// prediction control and the onset/offset rule became <see cref="RemindingTests"/>
    /// entries because there is nothing here to attach them to, and <c>Chunk</c>'s rule was
    /// built, measured and REFUTED. <c>Drives</c> is the one still owed: <b>a third factor
    /// computed from the body's own variables rather than a reward handed in.</b>
    /// </para>
    /// <para>
    /// <b>BORROW THE SOURCE OF THE SIGNAL AND NOT THE MECHANISM</b> — <c>csharp</c>'s own
    /// build of it lost, and the plan's refutation table is where that lives. What is worth
    /// inheriting is where the signal COMES FROM, which is the half no world here supplies:
    /// every world is watched rather than acted in, so nothing has a body with variables of
    /// its own to be in trouble about.
    /// </para>
    /// <para>
    /// <b>CLOSES BY BUILDING IT OR BY DROPPING IT WITH A REVIVAL ROW</b>, which are the only
    /// two endings this repo allows an idea. Either way the plan leaf goes, and this test
    /// reads the plan rather than a list of its own.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_last_idea_owed_off_csharp_is_taken_or_dropped()
    {
        var plan = File.ReadAllText(Path.Combine(Tree.Repo(), "docs", "plan.md"));

        var owed = plan.Contains("`Drives` is the one idea still owed", StringComparison.Ordinal);

        output.WriteLine(owed ? "Drives: still owed" : "Drives: settled");

        Assert.False(owed,
            "`Drives` is still recorded as owed off `csharp`, and that branch is the only "
            + "place it exists. Build it — a third factor from the body's own variables — or "
            + "drop it with a revival row saying what would bring it back, and take the leaf "
            + "out of the plan. This test is red on purpose and reads the plan, not a list.");
    }
}
