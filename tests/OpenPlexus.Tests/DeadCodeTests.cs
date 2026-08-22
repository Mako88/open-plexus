using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using OpenPlexus.Codes;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Every public member is either CALLED, or named here with the reason it is
/// not — <b>a budget</b>, like the dials and the doc and the clones and the row.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's call, 2026-08-04: dead code is the worst.</b> Nothing in this build
/// fails when a public member loses its last caller, so a mechanism can be
/// written, documented, cited in the plan, and never once run — and it reads
/// exactly like a mechanism that works. <b>Two were found by hand the day this
/// was written</b>: `Question.Conjoining`, the conjunction question, which had
/// never had a caller since the day it was written; and `Drives.Improving`, an
/// internal signal computed every step and read by nothing, which had been
/// described to John as one of three signals the system has.
/// </para>
/// <para>
/// <b>The shape is <c>DialTests</c>'s, on purpose.</b> Either something uses it,
/// or somebody has written down why not — and "nobody has got to it yet" is a
/// perfectly good reason as long as it is written where it can be counted. What
/// is not allowed is silence.
/// </para>
/// <para>
/// <b>Use is textual and comments do not count.</b> A <c>cref</c> naming a type
/// keeps its documentation honest and is exactly how a dead mechanism stays
/// looking alive, so the scan strips comments before it looks. <b>That is the
/// whole trick</b>: `Code.Prefix` is cited by fork 3 in prose and called by
/// nothing.
/// </para>
/// </remarks>
public sealed class DeadCodeTests(ITestOutputHelper output)
{
    /// <summary>
    /// Public members with no caller, each with the reason it survives.
    /// </summary>
    /// <remarks>
    /// <b>A REASON, NOT AN EXCUSE.</b> Several of these say outright that the
    /// thing should be wired or deleted and nobody has done it — which is the
    /// point of writing it down rather than the failure of it.
    /// </remarks>
    /// <remarks>
    /// <b>Empty, and the three ways it got there are worth keeping.</b> Sixteen
    /// entries were resolved rather than re-explained, and none of the three moves
    /// was "write a better reason":
    /// <list type="bullet">
    /// <item><b>Made non-public.</b> Five were used inside their own file and
    /// nowhere else — a world's own modality byte, its own cue, its own code
    /// constructors. A member with no caller outside its type was never public
    /// code; it was a private detail with the wrong keyword, and the budget is what
    /// noticed.</item>
    /// <item><b>Deleted.</b> Three had no caller anywhere at all. <c>Code.Prefix</c>
    /// was written for open fork 3, cited in prose, and never once run — and step
    /// 8's grains reached the same idea by another road, so the fork can write its
    /// three lines again if it ever wants them.</item>
    /// <item><b>Asserted.</b> Eight were computed every run and printed in a
    /// <c>ToString</c>, which is precisely how a quantity goes on looking alive:
    /// shown to whoever reads the output and free to be wrong forever. Each now has
    /// a test comparing it to something.</item>
    /// </list>
    /// <b>The list being empty is not the point</b> and should not become one. A
    /// written reason is a perfectly good outcome — "nobody has got to it yet"
    /// included. What is not allowed is silence, and what this file buys is that
    /// the next unwired mechanism arrives ALONE rather than among sixteen.
    /// </remarks>
    private static readonly Dictionary<string, string> Unused = new(StringComparer.Ordinal)
    {
        // ---- Members of the stranded worlds --------------------------------
        //
        // this list was at nought and the walk's deletion put fifteen on it, which is the
        // only honest way to record what the deletion cost. The type list below says which
        // worlds have no runner; this says how much of each one is unreachable
        // rather than merely undriven. Every entry here was called by a `*Run` in
        // `Worlds/`, and every one of those runs was the walk's.
        //
        // Each leaves by its world getting a `Watching`, NOT BY ANYBODY EDITING THIS. And the
        // budget below is back off nought for the first time since it reached it, which
        // should read as a debt rather than as a threshold being relaxed.

        // Five of `Homeostat`'s six came off together, which is what the entry said would
        // happen: each leaves by its world getting a runner. `IActed` is that runner's
        // missing half -- `Act`, `Attending` and `Lowest` are called by the front end and
        // the oracle, and `Viable` and `Idling` by the control that says the world
        // discriminates. What was left was the reverse mapping.
        //
        // And the sixth is off now, by the condition it was written with rather than by a
        // rewrite of it. That entry said it leaves when something asks a commitment what it
        // would DO rather than what would follow; `Drives` splits a scope's action code out
        // with `Attended` and does exactly that. `Sensed` arrived beside it as the other
        // inverse a preference needs -- which variable a felt band is about -- and is called
        // by the same arm.

        // And `Rhythm`'s two came off together, by the condition this list is written with.
        // `Beat` is the modality every moment of that stream rides and `Turned` is how many
        // times the world redrew its answer, so both are read by the arm that prices how
        // fast a turning world is tracked. Neither entry was rewritten.

        // ---- And the one that came straight back off, which is the check working -----
        //
        // `HybridBus.Delayed` spent one commit on this list. C2 is the constraint the whole
        // design rests on and its only injector was left uncalled by the walk going --
        // `Lateness` delays a share of deliveries on purpose and `Delayed` counts how many
        // were actually held back, because a jitter arm that delayed nothing is a control
        // wearing the arm's name. Its one caller had been the walk's latency sweep, which
        // measured per-hop delay against a thought's DEPTH; a fleet round is two round trips
        // and not a depth, so that measurement did not carry over.
        //
        // I wrote the reason and then wrote the caller, and this file caught the stale
        // entry. `LatenessTests` runs a fleet over `HybridBus` with a fifth of its traffic
        // delayed, which is fork 52's open half -- every distributed number here is over
        // `Posted`, and TCP does not reorder within a connection. The entry is gone rather
        // than reworded, which is the only way it is allowed to leave.

        ["Multiplexer.Widest"] =
            "THE SOUNDNESS INSTRUMENT'S OWN BOUND, and it reads as uncalled because its one "
            + "caller shares its file -- the own-file rule cannot see a caller sitting "
            + "beside it, which is the exemption `Felt` carries on the type list.",
    };

    /// <summary>What a record or a runtime generates and nobody writes.</summary>
    private static readonly HashSet<string> Generated = new(StringComparer.Ordinal)
    {
        "Equals", "GetHashCode", "ToString", "Deconstruct", "PrintMembers",
        "CompareTo", "Dispose", "DisposeAsync", "GetEnumerator", "Clone",
    };

    [Fact]
    public void Every_public_member_is_called_or_has_a_written_reason_it_is_not()
    {
        var source = Sources();

        var orphans = new List<string>();

        foreach (var type in Tree.Declared())
        {
            foreach (var member in Members(type))
            {
                var name = $"{type.Name}.{member}";

                // Its own declaration is not a use. Every other file counts, which
                // includes the tests -- a member exercised only by a test is doing
                // something, even if only holding a claim in place.
                var used = source
                    .Where(file => !file.Key.EndsWith($"{type.Name}.cs", StringComparison.Ordinal))
                    .Any(file => Regex.IsMatch(file.Value, Calls(member)));

                if (used || Unused.ContainsKey(name)) continue;

                orphans.Add(name);
            }
        }

        foreach (var one in orphans.Order(StringComparer.Ordinal)) output.WriteLine(one);

        Assert.True(orphans.Count == 0,
            $"{orphans.Count} public member(s) nothing calls and nobody has "
            + "explained. Wire it, delete it, or write down why it stays:\n  "
            + string.Join("\n  ", orphans.Order(StringComparer.Ordinal).Take(20)));
    }

    [Fact]
    public void And_the_list_does_not_rot_into_a_record_of_what_used_to_be_dead()
    {
        // THE OTHER DIRECTION, and without it the list becomes a graveyard of
        // members that have since been wired up — which is the exact failure the
        // doc's ticked boxes and the fork index are both checked for.
        var source = Sources();

        var revived = new List<string>();

        foreach (var (name, _) in Unused)
        {
            var member = name.Split('.')[1];
            var owner = name.Split('.')[0];

            if (source
                .Where(file => !file.Key.EndsWith($"{owner}.cs", StringComparison.Ordinal))
                .Any(file => Regex.IsMatch(file.Value, Calls(member))))
                revived.Add(name);
        }

        Assert.True(revived.Count == 0,
            $"named here as unused and now called: {string.Join(", ", revived)}. "
            + "Take it off the list.");
    }

    [Fact]
    public void The_budget_is_visible_and_does_not_grow()
    {
        // THE POINT OF THE FILE. The number is what it is today; having one is what
        // stops the next unwired mechanism arriving unnoticed beside these. IT
        // SHOULD ONLY EVER FALL — every entry is something to wire or delete.
        //
        // It was at nought and the walk's deletion put it at fifteen, which is the
        // deliberate edit that comment asked to read as one. Nought made this the
        // strictest the check can be: the next member to lose its last caller failed
        // outright with nowhere to sit quietly. That is given up here, and what is bought
        // for it is a countable record of what deleting the walk stranded.
        //
        // It was fifteen for one commit. Thirteen are one fact repeated — a world whose only
        // runner was the walk's — and they come off as those worlds are decided. The
        // fifteenth was `HybridBus.Delayed` and it is already gone, which is what the
        // entries above and below are each supposed to do.
        //
        // And it only ever falls from here. Nought is the destination and this is a
        // detour with a map, not a new resting place.
        //
        // Ten, and four came off by `Snake` and `SnakeSense` being DELETED rather than
        // wired. Their question was prediction conditional on action, which is forks 18 and
        // 20 -- both settled, and `csharp` disqualified survival as a score and refuted
        // absolute actions under an unrotated view besides. `PushbackTests` had the decision
        // recorded and waiting; this is somebody taking it.
        //
        // Five, and the other five came off the way the entries said they would: by their
        // world getting a runner. `Homeostat` had six here and every one of them was
        // unreachable because `IWorld.Next` is a pull, so a body that takes an action had
        // nowhere to be asked from. `IActed` is that call, and `Act`, `Attending`, `Lowest`,
        // `Viable` and `Idling` are all read the moment a world can be acted in. The sixth
        // is the mapping run backwards and has a reason of its own now.
        //
        // Four, and the sixth came off by the condition its own entry carried rather than by
        // that entry being rewritten. It said `Attended` leaves when something asks a
        // commitment what it would DO rather than what would follow; `Drives` splits an
        // action code out of a scope and does that. A member whose reason names the thing
        // that would read it is a reason that expires, which is the only kind worth writing.
        //
        // Two, and `Rhythm`'s pair left the same way. Its entry said each leaves by the world
        // getting a runner; `Watching` drives it through `IWorld` now, and the arm that prices
        // how fast a turning world is tracked reads the modality and the turn count both.
        // One, and `Composed.Third` left by its world getting a runner, which is the road
        // its entry named. A question is a fourth moment carrying the conjunction that
        // refers, so what is asked FOR has a caller outside the file that declares it -- and
        // the world reads at chance under every arm, which is a null result rather than a
        // wiring. What is left on the list is `Multiplexer.Widest` alone.
        Assert.Single(Unused);
    }

    [Fact]
    public void And_a_public_type_the_library_itself_never_names_is_not_wired()
    {
        // The hole the member scan cannot see, and `Winnow` fell straight through
        // it: built, documented, measured, and reaching NO WORLD -- while every
        // member read as used because `WinnowTests` names them. A member scan asks
        // whether anything calls a method; it never asks whether the library
        // itself has heard of the type.
        //
        // And tests do not count here, which they deliberately do above. That
        // asymmetry is the point rather than an inconsistency: a world's
        // `RunAsync` exists for the harness to call, so a test IS its caller and
        // counting it is right. Nothing exists for a test to CONSTRUCT -- a type
        // the library never names is a mechanism wired to nothing, whatever its
        // own tests do with it.
        var source = Sources()
            .Where(file => file.Key.Contains(
                $"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .ToList();

        var orphans = new List<string>();

        foreach (var type in Tree.Declared())
        {
            var name = type.Name.Split('`')[0];

            var named = source
                .Where(file => !file.Key.EndsWith($"{name}.cs", StringComparison.Ordinal))
                .Any(file => Regex.IsMatch(file.Value, @"\b" + Regex.Escape(name) + @"\b"));

            if (!named && !Unwired.ContainsKey(name)) orphans.Add(name);
        }

        Assert.True(orphans.Count == 0,
            $"{orphans.Count} public type(s) nothing in `src` names. Wire it, "
            + "delete it, or write down why it stays: "
            + string.Join(", ", orphans.Order(StringComparer.Ordinal)));
    }

    /// <summary>
    /// Every excuse names something that is still here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An excuse for a type that is gone</b> is a check that produces false PASSES, which
    /// is the one failure this file exists to prevent. Eleven of them had accumulated: eight
    /// runners that went with the walk, and <c>Felt</c>, <c>Marked</c> and <c>LatentRun</c>
    /// deleted for their own reasons. Each was inert on the day it went stale and each was a
    /// pardon waiting for a type of that name to come back — a <c>BindingRun</c> written
    /// tomorrow and wired to nothing would have been excused by a line written for a
    /// different one.
    /// </para>
    /// <para>
    /// <b>And nothing had ever asked.</b> The list above is checked in both directions — a
    /// member named unused and since called comes off — and the type list was checked in one.
    /// A budget counts entries and says nothing about whether an entry means anything.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_excuse_is_written_for_a_type_that_is_gone()
    {
        var declared = Tree.Declared()
            .Select(type => type.Name.Split('`')[0])
            .ToHashSet(StringComparer.Ordinal);

        var superstitions = Unwired.Keys
            .Where(name => !declared.Contains(name))
            .Order(StringComparer.Ordinal)
            .ToList();

        output.WriteLine(
            superstitions.Count == 0
                ? "every excuse names a type that is still here"
                : $"{superstitions.Count} stale: {string.Join(", ", superstitions)}");

        Assert.True(superstitions.Count == 0,
            $"{superstitions.Count} excuse(s) naming no public type: "
            + $"{string.Join(", ", superstitions)}. The type is gone and the line pardons "
            + "whatever comes back under its name, which is a false pass. Delete the entry.");
    }

    /// <summary>Public types the library never names, each with its reason.</summary>
    /// <remarks>
    /// <para>
    /// <b>Entry points and orphans</b>, and telling the two apart is the whole value of the
    /// check. A world's run exists for a harness to call, so a test IS its caller and the
    /// library never naming it is correct. `Winnow` is not that: it is a mechanism, and a
    /// mechanism the library has never heard of is wired to nothing however thoroughly its
    /// own tests exercise it.
    /// </para>
    /// <para>
    /// <b>The count is printed rather than written here</b>, which is the correction this
    /// remark is. It said <i>ten entry points and one orphan</i> and had said so through
    /// eleven entries going stale and several more arriving —
    /// <see cref="No_excuse_is_written_for_a_type_that_is_gone"/> is what asks now, and a
    /// number in a comment is what it replaced.
    /// </para>
    /// </remarks>
    private static readonly Dictionary<string, string> Unwired = new(StringComparer.Ordinal)
    {
        ["HomeostatRun"] = Harness,
        ["CrossingRun"] = Harness,

        // A chooser is composed rather than constructed, which is `Composed`'s reason one
        // layer in. `Watching` takes what to act with as a delegate on purpose -- a random
        // chooser, an oracle and a learner are three arms over one seam, and a library that
        // named one of them would be the library deciding which. The preference it ranks by
        // is a fact about a body, so `src` naming this would also be `src` deciding what a
        // world is for. `HomeostatTests` is what runs it, against both controls.
        ["Drives"] =
            "A CHOOSER IS CHOSEN BY WHOEVER COMPOSES THE RUN, so the library naming one "
            + "would decide which of three arms over `IActed`'s seam is the policy -- the "
            + "same fault as a world naming a brain type, one layer out. `Watching` takes it as "
            + "a delegate and `HomeostatTests` supplies it beside the oracle and the draw.",
        ["MultiplexerRun"] = Harness,
        ["GradedRun"] = Harness,
        ["CifarRun"] = Harness,
        ["ArrangedRun"] = Harness,
        ["MonkRun"] = Harness,

        ["Posted"] = "A TRANSPORT IS CHOSEN BY WHOEVER COMPOSES THE SYSTEM, so the "
            + "library naming one would be the library deciding how it is deployed -- "
            + "the same fault as a world naming a brain type, one layer out. `IBus` is "
            + "what `src` knows about; which bus is a container's or a harness's "
            + "decision, and `HybridBus` sits the same way.",

        ["Leaves"] = "used by `Joins`, which shares its file — the own-file rule cannot "
            + "see a caller sitting beside it. It arrived here when the scan stopped reading "
            + "public types and started reading declared ones; it was `internal` before the "
            + "learner was, so it had never been asked about.",

        ["Bodied"] = "A FRONT END IS CHOSEN AT THE JOIN, so the library naming one would "
            + "be the library deciding how a world is perceived -- the same line `Posted` "
            + "sits on one layer out. `IQuantizer` is what `src` knows about; which "
            + "translation runs is a harness's decision, and `Joined` and `Bits` are on "
            + "this footing without being on this list only because a `*Run` still names "
            + "them.",

        // And both entries changed their reason rather than their status, which is the
        // more interesting outcome. `Asker` came off this list because `Fleet` names it,
        // and the two that remain are not unmounted any more -- they are the deployment,
        // and a library that named its own deployment would be deciding it.
        //
        // `Holder` read as wired for exactly as long as a tuple field was spelt like
        // it. `HybridBus.AskAsync` named its second element `Holder`, and a scan for
        // whether the library NAMES a type answers yes to that for free. Renaming the
        // field is what put it on this list, and it is the sharper half of the finding: a
        // budget can be satisfied by a coincidence.
        ["Holder"] = Composed,
        ["Fleet"] = Composed,

        // ---- The walk's deletion left eight worlds with no runner -----------
        //
        // and this is the one place the whole deletion is visible as a cost. Each of these
        // had a `*Run` in `Worlds/` that drove it, and every one of those was the walk's --
        // so the world data survived and the thing that turned it into a measurement did
        // not. They are NOT on `Roaming`'s footing: that one is driven by `Watching` through
        // `IWorld` and has a caller, and these have none at all.
        //
        // And the question is not only *which gets a `Watching` first*, which is how this was
        // first written and is half the rule. `RemindingTests` carries the other half: An
        // isolating world is deleted when its question closes, and worlds accumulate exactly
        // as dials do. So each of these is one of two things and the list does not yet say
        // which -- a world whose question is LIVE and wants a runner, or a world whose
        // question closed with the walk and wants deleting.
        //
        // SOME ARE OBVIOUSLY LIVE: `Motif` was rung five's redundancy manufactured on
        // purpose and `Latent` is fork 39's. `Snake` and `SnakeSense` were the obviously-not
        // pair -- prediction conditional on action, forks 18 and 20, both settled -- and
        // they are gone rather than wired. `Rhythm` came off by the third road, which is
        // that its live question was not the one its own entry named: it was written for a
        // prediction budget that is dead, and what it is worth is being the only world whose
        // ANSWER moves.
        //
        // And `Motif` came off by the first road, which is the plain one: its question was
        // live, so it got a `Watching`. What decided the mapping was that the world already
        // carried its own control -- `Motifs` at nought is the same stream with nothing
        // recurring in it -- so the run that makes it reachable is also the run that asks
        // whether the naming gate reads recurrence or reads the stream.
        //
        // And two of the four on that delete list should not go. `Homeostat` is the only
        // world here that is acted in and is what `Drives` needs, and `Composed.Segmented`
        // is one of the dials feeding the unread `Bind` channel, which is a live entry. A
        // handoff enumerated them as closed without reading what each held.
        //
        // What is forbidden is leaving them here while nobody decides, which is the same
        // sentence the dial rule uses. Deleting them all to make a guard green would delete
        // live questions; keeping them all is a world budget nobody is paying.
        ["Clevr"] = "A WORLD, ON `Roaming`'S FOOTING, AND IT CAME OFF THIS LIST BY GETTING "
            + "A RUNNER AND A NULL RESULT. `Watching` drives it through `IWorld` now: a "
            + "round is one QUESTION rather than one scene, the moment being the scene's "
            + "codes plus a `Refers` code per filter of the question's chain and one `Asks` "
            + "code saying which attribute is wanted. It came off by the first road, which "
            + "is that its question was the one its entry named -- and the answer is that "
            + "the held-back questions are answered at the weighted chance bar while the "
            + "drawn stream is memorised. `ClevrTests` holds the reading and the bar it must "
            + "not cross.",

        ["Composed"] =
            "A WORLD, ON `Clevr`'S FOOTING, AND IT CAME OFF THE STRANDED LIST BY GETTING A "
            + "RUNNER AND A NULL RESULT. `Watching` drives it through `IWorld` now: four "
            + "moments a scene, the three the world always had and a fourth carrying the "
            + "conjunction that refers, so the referring values and the value asked for are "
            + "still never in one moment. Every arm sits on chance -- and the reading worth "
            + "having is that the INDEX changes nothing, because a story moment carries no "
            + "outcome and nothing settles it. `ComposedTests` holds it and it closes when a "
            + "commitment is settled by its successor.",

        ["Homeostat"] =
            "A WORLD, ON `Roaming`'S FOOTING SINCE `IActed` EXISTS, and it is the first "
            + "world here that is acted in rather than watched. `Watching` drives it through "
            + "`IActed` -- the same seam `IWorld` sits on, with a chooser handed in at the "
            + "join -- so the library naming it would be the library naming a world, which "
            + "is the fault this list exists to catch and not a debt it records.",

        ["Latent"] = "A WORLD, ON `Roaming`'S FOOTING, AND IT CAME OFF THE STRANDED LIST BY "
            + "GETTING A RUNNER AND A DIAL. `Watching` drives it through `IWorld` now: a moment "
            + "is every channel but the last and the outcome is what the last one reported. "
            + "It came off by a fourth road, which is that a standing objection had predicted "
            + "the runner would exercise nothing and was half right. As shipped every channel "
            + "reported the cause deterministically, so genesis answered at one code, repair "
            + "was never asked, and nought was ever eligible -- measured, not argued. A "
            + "channel that can LIE is what makes the group necessary, which is what a latent "
            + "cause was always for: under noise no single channel settles the answer and "
            + "hundreds of scopes are eligible. `LatentSettings.Noise` at nought is the "
            + "control and it is the world the objection described. `LatentTests` is its "
            + "caller.",

        ["Motif"] = "A WORLD, ON `Roaming`'S FOOTING, AND IT CAME OFF THE STRANDED LIST BY "
            + "GETTING A RUNNER. `Watching` drives it through `IWorld` now: a moment is the "
            + "cue half of a set and the outcome is one of the codes that set withheld, "
            + "drawn uniformly. It is the world where rung five's redundancy is "
            + "manufactured on purpose, and its control is the same stream with nothing "
            + "recurring in it -- which is this repo's oldest refutation put to the current "
            + "gate rather than to the one it killed. `MotifTests` is its caller.",

        ["Recalled"] = "A WORLD, ON `Roaming`'S FOOTING, and it was on the stranded list "
            + "by mistake. Both halves of that reason are false of it: `Watching` drives it "
            + "through `IWorld` -- `RecalledTests.Made` builds a `Watching` over it -- "
            + "and its tests assert what is LEARNT rather than what the world is, every "
            + "text reading on this branch having come off them. It went on the list when "
            + "the walk's deletion enumerated the worlds, and nothing checked which of them "
            + "the walk had actually been driving. A budget can be satisfied by a "
            + "coincidence, and so can a debt.",

        ["Rhythm"] = "A WORLD, ON `Roaming`'S FOOTING, AND IT CAME OFF THE STRANDED LIST BY "
            + "GETTING A RUNNER. `Watching` drives it through `IWorld` now: a moment is the "
            + "symbol the stream sounded last and the outcome is the one that followed. It "
            + "is the only world here whose ANSWER moves, and the only one where a scope "
            + "cannot be made longer -- so repair, rung three and rung five are all held "
            + "still while the vote and the local decaying estimate are not. `RhythmTests` "
            + "is its caller.",
        ["Senses"] = "A WORLD, ON `Roaming`'S FOOTING, AND IT CAME OFF THIS LIST BY "
            + "GETTING A RUNNER. `Watching` drives it through `IWorld` now: an occasion shows "
            + "two senses and asks about one of them, and the examination shows a sight and "
            + "a sound and asks what the thing FEELS like -- a combination the stream draws "
            + "nought times. `SensesTests` is its caller.",

        ["HybridBus"] = Composed,

        // ---- A front end is chosen at the join ------------------------------
        //
        // the third thing, and the plan says so in those words. Whether a reading is banded
        // or winnowed is neither a fact about the problem nor a setting on the brain, so it
        // is picked where the two meet -- `ShapeTests` admits `fronting` and `through` on a
        // world's runner for exactly this reason. A library that named one would be
        // deciding how everything it is ever shown gets perceived.
        ["Passthrough"] = Join,

        ["Roaming"] = "A WORLD, ON THE SAME FOOTING AS `Returning`: `Watching` drives it "
            + "through `IWorld`, so there is no run for `src` to name and naming the world "
            + "itself would be the library knowing which problem it is pointed at. "
            + "`RoamingTests` is its caller.",

        ["Returning"] = "A WORLD, AND THE LIBRARY NAMES `IWorld` RATHER THAN ANY OF "
            + "THEM. It has no run of its own because `Watching` drives it directly, so "
            + "there is not even a harness entry point for `src` to mention -- and a "
            + "world the library named would be the library knowing what problem it is "
            + "being pointed at, which is the fault `SeparationTests` guards from the "
            + "other side. `ReturningTests` is its caller.",

        ["Handing"] = "A WORLD, ON `Roaming`'S FOOTING, and it has no `Watching` behind it "
            + "yet for a reason of its own: what has been taken on it so far is the three "
            + "CEILINGS, which need no learner at all. It is fork 105 isolated -- a "
            + "sentence naming two people where a bag of words is provably at the marginal, "
            + "a selector provably at one half, and the order provably at one. Running a "
            + "population over it before those three were established would have produced a "
            + "number nobody could attribute. `HandingTests` is its caller.",

        ["Lettering"] = "A SENSOR PRICED BEFORE ONE IS BUILT ON IT, which is fork 107's "
            + "own instruction: the crossing that keeps ground truth enumerable is a word "
            + "SEEN against a word read, and a front end has a ceiling computable with no "
            + "learning. Something in `src` drawing words would mean the world had been "
            + "built, when what the measurement is FOR is deciding whether it can be. "
            + "`LetteringTests` reads it: sixteen words at one place probe at 1.000, the "
            + "same words moved probe at 0.006 on their own pixels, and a shared codebook "
            + "recovers 0.539 of them -- so the codes carry which word was drawn and the "
            + "fork is not blocked at the front end. The day a world pushes these moments "
            + "is the day it comes off.",

        ["Alternating"] = "A DERIVATION MEASURED BEFORE IT IS ADMITTED, on the same "
            + "footing as `Unifying`. It finds the groups of codes that are alternatives, "
            + "which is what a category would be minted over -- and something in `src` "
            + "calling it would mean the operator had been admitted, when what the "
            + "measurement is FOR is deciding whether to admit it. `ReturningTests` reads "
            + "it: the appearances come back exactly and the twins do not, so a category "
            + "reaches a kind and never an individual. A category MAY enter a scope now -- "
            + "`Categories` carries the vocabulary and `Population.Recast` reads it -- and this "
            + "entry stays because the DERIVATION is still the experimenter's. The day a "
            + "front end runs it on its own stream is the day it comes off.",

        ["Unifying"] = "A PRICE AND NOT YET A MECHANISM, which is fork 33's own "
            + "instruction: probe unification's cost BEFORE the ladder's escalation "
            + "policy is designed, not after. Something in `src` calling it would mean "
            + "rung four had been admitted -- and the admission is the decision the "
            + "price exists to inform, so wiring it before that decision is taken would "
            + "be answering the question by building the answer. `UnifyingCostTests` is "
            + "what it is for; the day repair may propose a scope naming no argument is "
            + "the day this entry comes off.",

        ["Probe"] = "A CONTROL ARM, SO THE LIBRARY NAMING IT WOULD BE THE FAULT. It "
            + "is the dullest learner there is, run over the same features the "
            + "commitment population reads, and what it measures is how much of a "
            + "problem is in the FRONT END rather than in the learner. Something "
            + "inside `src` calling it would mean the architecture had started "
            + "consulting its own yardstick, which is the one thing it must never do.",

        // `Winnow` was the entry this check was written for, and it is gone because
        // it is now mounted rather than because the reason was reworded. `GradedRun`
        // consumes it as one of two front-end arms, so the library has finally heard
        // of the type its own plan called its defence.
    };

    /// <summary>Why a world's run is not named by the library.</summary>
    /// <summary>
    /// Everything still on this file's lists for the STRANDED reason — <b>the outstanding
    /// item, in one place</b>, so `OutstandingTests` and this file cannot disagree about it.
    /// </summary>
    /// <remarks>
    /// <b>Derived rather than listed, which is the whole value.</b> An entry leaves by being
    /// deleted from the dictionaries above — because its world got a runner, or because its
    /// world was deleted — and the red test that demands it closes on the same edit. A second
    /// hand-kept list would be a second thing to forget.
    /// </remarks>
    internal static IReadOnlyCollection<string> StillStranded =>
    [
        .. Unwired.Where(one => one.Value == Stranded).Select(one => one.Key)
            .Concat(Unused.Where(one => one.Value == Stranded).Select(one => one.Key))
            .Order(StringComparer.Ordinal),
    ];

    /// <summary>Why a world whose only runner was the walk's is named by nothing.</summary>
    private const string Stranded =
        "A WORLD WHOSE RUNNER WENT WITH THE WALK. The data is intact and nothing drives it "
        + "-- unlike `Roaming`, which `Watching` drives through `IWorld`. It wants one or "
        + "a deletion depending on whether its question is still live, and until one or the "
        + "other its tests assert what the WORLD is and nothing about what is learnt.";

    /// <summary>Why a front end is not named by the library that reads through it.</summary>
    private const string Join =
        "A TRANSLATION IS A THIRD THING AND BELONGS AT THE JOIN. Which front end a stream is "
        + "read through is neither a fact about the problem nor a setting on the brain, so a "
        + "library naming one would decide how everything it is ever shown is perceived. "
        + "`ShapeTests` admits `fronting` on a runner for the same reason.";

    private const string Harness =
        "a world's run is the HARNESS's entry point, so a test is its rightful "
        + "caller and the library naming it would be the surprise.";

    /// <summary>
    /// Why a role in a deployment is not named by the library it is deployed from.
    /// </summary>
    /// <remarks>
    /// <b>The same reason `Posted` carries, one layer up</b>, and it replaced a better-known
    /// one. These two used to say <i>fork 52's transport is built and the learner is
    /// not on it</i>, which was the honest state and is exactly what this list is for.
    /// The learner is on it now, and what is left is not a gap: a library that constructed
    /// its own holders would be a library that had decided how many machines there are.
    /// </remarks>
    private const string Composed =
        "A DEPLOYMENT IS CHOSEN BY WHOEVER COMPOSES THE SYSTEM, so the library naming one "
        + "would be the library deciding how it is run -- the same fault as a world naming "
        + "a brain type, one layer out. `ICouncil` is what `src` knows about, and whether "
        + "the council behind it is one population or twelve machines on twelve ports is a "
        + "container's decision. `Posted` sits the same way, and `FleetTests` is what runs "
        + "a whole learner over these.";

    /// <summary>
    /// What using a member actually looks like, as against merely spelling it.
    /// </summary>
    /// <remarks>
    /// <b>A bare word is not a use</b>, and the companion check caught me assuming it
    /// was. <c>Better</c>, <c>Same</c>, <c>Symbol</c> and <c>Thing</c> are
    /// ordinary words appearing as unrelated identifiers all over the tree, so
    /// matching the name alone reports a dead member as live — <b>a check that
    /// produces false PASSES</b>, which is the one failure this file exists to
    /// prevent. A use is a member access, a named argument, or an initialiser.
    /// </remarks>
    private static string Calls(string member) =>
        @"\." + Regex.Escape(member) + @"\b|\b" + Regex.Escape(member) + @"\s*[:=][^=]";

    /// <summary>Every source file, with comments stripped.</summary>
    /// <remarks>
    /// <b>A <c>cref</c> IS NOT A CALL, and that is the whole trick.</b> Doc
    /// comments are how a dead mechanism goes on looking alive — the compiler
    /// checks that they RESOLVE, which is a guarantee about spelling and not about
    /// use.
    /// </remarks>
    private static Dictionary<string, string> Sources()
    {
        var root = Tree.Repo();

        return Directory
            .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))

            // AND NOT THIS FILE, which names every member on the list and would
            // otherwise report all of them as used — the list keeping itself
            // alive, which is the funniest way this check could be worth nothing.
            .Where(path => !path.EndsWith("DeadCodeTests.cs", StringComparison.Ordinal))
            .ToDictionary(
                path => path,
                path => Regex.Replace(File.ReadAllText(path), @"^\s*///.*$", "", RegexOptions.Multiline));
    }

    /// <summary>The public members of one type that a caller could name.</summary>
    private static IEnumerable<string> Members(Type type)
    {
        const BindingFlags Declared =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
            | BindingFlags.DeclaredOnly;

        foreach (var one in type.GetMembers(Declared))
        {
            if (one is MethodBase { IsSpecialName: true }) continue;
            if (one.GetCustomAttribute<CompilerGeneratedAttribute>() is not null) continue;
            if (Generated.Contains(one.Name)) continue;
            if (one.Name.StartsWith('<') || one.Name.StartsWith("op_", StringComparison.Ordinal))
                continue;

            // Constructors are named by their type, and the type's own use is a
            // different question from a member's.
            if (one is ConstructorInfo) continue;

            // An enum member is a value rather than a call, and a refuted arm being
            // deleted is what `Attending` and `Gardening` are already policed by.
            if (type.IsEnum) continue;

            yield return one.Name;
        }
    }
}
