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
/// <b>These are supposed to be failing.</b> Do not delete them and do not weaken them. Each
/// one closes by the work being done, and every one of them computes the state rather than
/// asserting a constant — so none can be satisfied by editing this file. If you have arrived
/// at a red suite and this is what is red, this file is the priority and not a regression.
/// </para>
/// <para>
/// <b>The idea is already this repo's</b> and it is recorded as having worked.
/// <see cref="ShapeTests"/> says of its own list: <i>it began as fifteen dials across seven
/// worlds and was a to-do list that failed the build, which is the only kind that gets
/// done.</i> It reached nought on the day it was written. What is new here is doing that
/// deliberately rather than as a side effect of one guard.
/// </para>
/// <para>
/// <b>And the cost is real, which is why the list is short.</b> A permanently red suite
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
    /// <b>Worlds with no runner</b>, and a member counts as an entry too.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every one was driven by a <c>*Run</c> in <c>Worlds/</c> and every one of those was the
    /// walk's, so the world data survived and the thing that turned it into a measurement did
    /// not. The commitment side's runner is <c>Bench</c>, and a world reaches it as a
    /// <c>Watching</c> inside a <c>Body</c>.
    /// <b>A world's unreachable MEMBERS are listed beside its type</b>, which is how much of
    /// each world is dead rather than merely undriven — and it is what tells a runner from a
    /// deletion. The count is printed rather than written here, because a count written into
    /// a remark rots the first time the list moves and this one already had.
    /// </para>
    /// <para>
    /// <b>And a runner is only half the answer</b>, which is the part that was first written
    /// wrong. <see cref="RemindingTests"/> prints the other half: an isolating world is
    /// DELETED when its question closes, and worlds accumulate exactly as dials do. So each
    /// name below is one of two things and this test does not care which — it closes when the
    /// entry leaves <see cref="DeadCodeTests"/>, by either road.
    /// </para>
    /// <para>
    /// <b>Nought</b>, and <c>Composed</c> came off by a road its own entry half had. That
    /// entry said the answer arrives in a successor moment and no <c>Turn</c> can say so. A
    /// turn cannot, and a fourth moment can: the three the world always had, then one
    /// carrying the conjunction that refers, so the referring values and the value asked for
    /// are still never in one moment. Every arm reads at chance.
    /// </para>
    /// <para>
    /// <b>And the seam it named is the right one</b>, which the run says rather than argues.
    /// A story moment carries no outcome, so nothing settles it — no genesis, no repair, and
    /// a monotone counter with nothing to count. Three quarters of every scene is inert and
    /// the whole population comes out of question rounds, so the INDEX linking the three
    /// moments changes not one count. <see cref="ComposedTests"/> asserts that, and it closes
    /// when a commitment is settled by its successor.
    /// </para>
    /// <para>
    /// <b>Green is the point rather than a reason to delete it.</b> A world arriving with no
    /// runner puts an entry back on <see cref="DeadCodeTests"/> and this goes red the same
    /// day, which is what a deadline is for.
    /// </para>
    /// <para>
    /// <b>And four have come off, by roads worth telling apart.</b> <c>Senses</c> got a
    /// runner for the question it was built for; <c>Rhythm</c> got one for a question its
    /// own entry never named, that entry having called it rung three's when the rung it can
    /// hold still is repair. A world's stated purpose is not the same as its live one.
    /// </para>
    /// <para>
    /// <b>And <c>Motif</c> and <c>Clevr</c> are the plain cases</b>, worth naming beside the
    /// other two. Each question was the one its entry said it was, so a runner is the whole
    /// of what either wanted — <c>Motif</c> had carried its own control since the day it was
    /// written, and <c>Clevr</c>'s runner returned a null result worth having: the held-back
    /// questions answered at the weighted chance bar while the drawn stream is memorised.
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
            $"{stranded.Count} entries across the worlds nothing can run: "
            + $"{string.Join(", ", stranded)}. Give the world a `Watching`, or DELETE it "
            + "because its question closed — then take its entry off `DeadCodeTests`. "
            + "This test is red on purpose and closes on that edit, not on this file.");
    }

    /// <summary>
    /// <b>Three channels lost their reader with the walk, and one is left.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IQuantizer{TObservation}.Bind"/>, <see cref="IQuantizer{TObservation}.Fleeting"/>
    /// and <see cref="IQuantizer{TObservation}.Forced"/> were implemented by every front end
    /// and read by nothing. The walk's occasion was their only consumer; a commitment's scope
    /// is a SET of codes with nowhere to put a group, a lifetime or an intervention.
    /// </para>
    /// <para>
    /// <b>A channel with no far end</b> is the shape this repo keeps finding read as built —
    /// <c>Surprise</c> and <c>Abstain</c> were both found wired and unable to fire, and
    /// <i>promiscuous on purpose</i> meant exhaustive for the life of the repo. Three world
    /// dials feed these and would go with them: <c>Binding.Segmented</c>,
    /// <c>Binding.Fleeting</c>, <c>Clevr.Segmented</c>, <c>Composed.Segmented</c>.
    /// </para>
    /// <para>
    /// <b>Closes either way, and the two are real alternatives.</b> Wire one to something
    /// that acts on it, or delete it with the dials that feed it. <c>Order</c> is the one of
    /// the four with a reader and is what rung three is made of, so it is not counted here.
    /// </para>
    /// <para>
    /// <b>And <c>Forced</c> has come off</b>, by the road this entry named. It was unsayable
    /// while every world was watched; <see cref="Worlds.IActed{TSeen}"/> let a world say
    /// which code it was handed, <see cref="Worlds.Roaming"/> says it, and
    /// <see cref="Codes.Intervened"/> derives a code beside each forced one so a scope may
    /// name the doing. Repair takes it on 193 of 318 resident scopes, so the reader reads
    /// something rather than merely existing.
    /// </para>
    /// <para>
    /// <b>And <c>Fleeting</c> has come off, by a road neither alternative named.</b> The
    /// honest question about it was whether anything would ever act on it, and
    /// <see cref="ClevrTests.Clevr_reaches_the_commitment_learner"/> answered it with a
    /// number: 93% of that run's table is rows for object and scene indexes, each minted
    /// fresh for one scene and unable to recur. The reader is a row a fleeting code does not
    /// get, and it travels on <c>Pushed</c> and through <c>ICouncil</c> to whoever writes it.
    /// </para>
    /// <para>
    /// <b>The prediction attached to it was wrong.</b> It was
    /// written before the build: the mark reaches the table and nothing else, so the arm dies
    /// if any other count moves. Repairs went from 3,169 to 18,733 and minted from 178 to
    /// 1,418. The table is not a cache — it is repair's candidate set — so there is no such
    /// thing as changing one and not the other, and most of what repair had been ranking were
    /// codes that could not fire again.
    /// </para>
    /// <para>
    /// <b>Which leaves <c>Bind</c>, and the road that looks obvious is refuted.</b> A
    /// <c>Belonging</c> was built on <see cref="Codes.Sequenced"/>'s seam — a derived code
    /// for each pair of codes inside one group, so a red ball beside a blue box stops
    /// producing the moment a blue ball beside a red box produces. That separation is real
    /// and it bought nothing on either world it reaches.
    /// </para>
    /// <para>
    /// <b>On CLEVR it flooded repair's candidate set and composed nothing.</b> The withheld
    /// set answered 0.281 and 0.312 against a 0.358 chance bar, so the null result
    /// <see cref="ClevrTests.Clevr_reaches_the_commitment_learner"/> records was untouched;
    /// the told arm's repairs went from 3,169 to 12,776 and the per-scene share of the table
    /// fell from 93% to 18%. Excluding the codes a front end marks fleeting was the one
    /// adjustment a losing arm gets, and it made that arm worse: 0.184 on the withheld set
    /// and 17,812 repairs.
    /// </para>
    /// <para>
    /// <b>On the spine world the channel was not filled at all</b>, which is the finding
    /// worth keeping. <c>Roaming</c> reads through <c>Joined</c> and that front end has no
    /// <c>Bind</c>, so the derivation was inert there and the run's time did not move.
    /// Teaching <c>Joined</c> that a statement is the thing a word belongs to did fill it,
    /// at three <see cref="LessonTests"/> going red, one entry of
    /// <see cref="ExercisedTests"/> going from reached to unreached, and the population
    /// rising from 1,102 to 1,258 with no score to pay for it.
    /// </para>
    /// <para>
    /// <b>So what is untested is rung four's matcher</b>, and it is the road this entry named
    /// first. What was refuted is deriving the grouping into codes, not constraining a
    /// variable's filling to a group — and the argument against that one is a cost rather
    /// than a reading: it puts the front end's group report on the wire beside every moment,
    /// which is what deriving into codes exists to avoid.
    /// </para>
    /// <para>
    /// <b>The cost is one int a code</b>, on a message that already carries every code, which
    /// is smaller than the objection above reads. <c>Fleeting</c> travels on <c>Pushed</c>
    /// and again as <c>Asked.Fleeting</c>, so a per-code channel beside the moment is a
    /// shape the wire already pays for; a grouping is a partition of the same codes. What
    /// deriving into codes avoided was a quadratic in the group's size, and this is linear.
    /// </para>
    /// <para>
    /// <b>And <c>Bind</c> has come off</b>, by the road this entry named last and not by rung
    /// four. <c>Binding</c> is an <c>IWorld</c> now — a question code derived from one
    /// object's colour, sitting in that object's part, and the answer is that object's shape
    /// — so <c>Bench</c> drives it and the channel has somewhere to be measured. The reader
    /// is <see cref="Commitments.Spanning"/>: a scope is about ONE thing, so genesis mints
    /// one over each and none fires across two.
    /// </para>
    /// <para>
    /// <b>Both halves or neither</b>, which is the part that had to be found by running it.
    /// A matcher refusing a scope that spans two things cannot help while nothing proposes a
    /// scope about one — genesis roots on single codes and repair adds a code that SEPARATES,
    /// and the code completing a thing is present whether or not the thing is bound that way,
    /// so it separates nothing a table counting co-occurrence can see.
    /// </para>
    /// <para>
    /// <b>0.9938 against 0.5050 on the withheld set</b>, against the control that has the
    /// grouping and does not read it, on the world whose two arms emit identical codes. The
    /// arm holds every one of the 144 rules the world has and the control holds none of them
    /// — and the control is the bigger population, so this is not more search.
    /// <see cref="BindingTests"/> carries the grid.
    /// </para>
    /// <para>
    /// <b>And the genesis gate is what had been hiding it.</b> Under
    /// <c>Surprising.Unaccounted</c> the same arm reads 0.5594: proposals stop at eighty
    /// because a lucky advocate expects what arrived half the time on a coin-flip world, so
    /// the population never sees more than 22 of the 144. Ungated genesis on its own buys
    /// nothing — 0.5050 for 2,328 residents — so what the grouping does is make an
    /// unaffordable proposal set worth having.
    /// </para>
    /// <para>
    /// <b>And <c>Composed</c> does not close with it</b>, which this entry had wrong. Its
    /// scene is three moments and its answer arrives in the third, so what it wants is
    /// settlement by successor rather than a reader for a grouping. The two were tied
    /// together only by the home that turned out to be untested.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_front_end_channel_is_read_by_nothing()
    {
        // Where a reader would be, rather than where a forward would be -- and scoping it
        // this way is what makes the check mean something. Everything in `Brain/Codes/` either
        // DECLARES these or passes them straight through: `Compound` merges what its senses
        // said, `Alternating` hands its inner one's answer back, and a front end returning
        // its own field is not a consumer. A reader is something that ACTS on the channel,
        // and that lives where the learner is.
        //
        // And it replaced a list of filenames to ignore, which was the fragile version: a
        // new forwarder in `Brain/Codes/` would have turned this green while nothing had changed,
        // and an unrelated `.Bind(` anywhere in `src` would have done the same.
        var sources = new[]
            {
                Path.Combine("OpenPlexus.Brain", "Commitments"),
                Path.Combine("OpenPlexus", "Machines"),
            }
            .SelectMany(where => Directory.GetFiles(
                Path.Combine(Tree.Repo(), "src", where),
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
            + "nowhere in the learner or the runners. Wire one to something that ACTS on "
            + "it, or delete it together with the world dials that feed it. This test is red "
            + "on purpose.");
    }

    /// <summary>
    /// <b>The prose is half out of the register.</b> The other half is by hand.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Capitals reached nought in one pass because a shouted sentence has exactly one correct
    /// rewrite. Bold sentences do not: where a lead clause ends is a judgement about which part
    /// of a claim a reader scans for, so no script can make it and 894 of them are left.
    /// </para>
    /// <para>
    /// <b>Here because a ratchet does not do what John asked</b>, and it is now half of the
    /// answer rather than the whole of it. A ratchet stops the count rising and says nothing
    /// about it ever falling, so this entry is red until they are all gone. What paces the fall
    /// is <see cref="ProseTests.Falls"/>: the ceiling drops by five a commit, so the debt is
    /// owed by the branch and no single commit is taxed. The straight reading — one commit in
    /// five must do prose work — was refused because it would block a one-line fix on a tone
    /// debt it has nothing to do with.
    /// </para>
    /// <para>
    /// <b>Most have an obvious cut point.</b> Taking it by script is not safe. 787
    /// of the 1,127 would fall under the cap by closing the bold at the first comma, and 340
    /// need the claim rewritten. A shouted sentence has one correct rewrite and that is why a
    /// script did 2,489 of them; a comma is only usually where a lead ends, so a scripted pass
    /// would trade one mechanical register for another. That is the thing being fixed.
    /// </para>
    /// <para>
    /// <b>The first pass measured that ratio rather than assuming it.</b> Of 45 done by hand at
    /// the schedule's baseline, the comma cut was right for 33 and wrong for 12 — and where it
    /// was wrong the claim came second, so the lead had to move rather than be trimmed. A
    /// script would have taken all 45.
    /// </para>
    /// <para>
    /// <b>Closes by two counts reaching nought.</b> It reads
    /// <see cref="ProseTests.BoldSentences"/> and <see cref="ProseTests.ShoutedLeads"/> rather
    /// than a list of its own. The second was added when a pass turned up thirteen shouted
    /// leads in three files that <c>ProseTests.Shouts</c> could not see — an inline tag breaks
    /// the run, and a four-word shout is shorter than the longest real label. Both are the same
    /// debt in the same register, so they close together rather than as two entries.
    /// </para>
    /// <para>
    /// The typography is all any check here can see; the reveal, the stinger and the corrective
    /// turn are written down in CLAUDE.md and nothing reaches them.
    /// </para>
    /// <para>
    /// <b>And both counts are nought.</b> The bold half came down over the
    /// passes <see cref="ProseTests.Falls"/> paces, five files at a time and eighty judgements
    /// at a sitting; the shouted leads went in one pass that looked for them. The ratio this
    /// entry claimed held throughout, which is what makes the estimate here worth keeping
    /// rather than deleting.
    /// </para>
    /// <para>
    /// <b>What the last pass found is where a script stops.</b> Forty-five leads lowercased
    /// mechanically left eleven spans half-shouted, the capitals carrying on after an inline
    /// tag the run had stopped at — the guard satisfied and the prose still in the register.
    /// Six more were invisible to it because a one-letter word ends a run, so <i>rather than A
    /// SHUFFLE</i> is a shout no version of this check can see. The entry closes on the two
    /// counts and the register is still a written rule.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_prose_is_out_of_the_engagement_register()
    {
        var bold = ProseTests.BoldSentences();
        var shouted = ProseTests.ShoutedLeads();

        output.WriteLine(
            bold + shouted == 0
                ? "no bold sentences and no shouted leads left"
                : $"{bold} bold sentences to cut back to a lead clause, {shouted} leads to "
                    + "lowercase");

        Assert.True(bold + shouted == 0,
            $"{bold} bold spans are a sentence rather than the lead clause bold is for, and "
            + $"{shouted} open in capitals. Cut each back to the claim a reader scans for and "
            + "lowercase the leads, then lower `ProseTests.Shouted` and `ProseTests.Opened` to "
            + "what the pass achieved. This test is red on purpose and closes on those counts, "
            + "not on this file.");
    }

    /// <summary>
    /// <b>`Drives` is the last idea owed off `csharp`</b>, and that branch is not coming back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Four things were ranked worth taking before the walk went. Three are done — the
    /// prediction control and the onset/offset rule became <see cref="RemindingTests"/>
    /// entries because there is nothing here to attach them to, and <c>Chunk</c>'s rule was
    /// built, measured and REFUTED. <c>Drives</c> is the one still owed: <b>a third factor
    /// from the body's own variables</b>, rather than a reward handed in.
    /// </para>
    /// <para>
    /// <b>Borrow the source of the signal and not the mechanism</b> — <c>csharp</c>'s own
    /// build of it lost, and the plan's refutation table is where that lives. What is worth
    /// inheriting is where the signal COMES FROM, which is a body with variables of its own
    /// to be in trouble about.
    /// </para>
    /// <para>
    /// <b>The world it needed was here all along</b>, and this entry twice said otherwise.
    /// <see cref="Worlds.Homeostat"/> has internal variables that drain at uneven rates, an
    /// act of attention that restores one, a predicate saying whether the body still holds,
    /// and readouts for which variable is in trouble and how fast.
    /// </para>
    /// <para>
    /// <b>What blocked it was the world interface and not the world</b>, which is the version
    /// of this note that turned out to be right. <see cref="Worlds.IWorld{TSeen}.Next"/> is a
    /// pull, so a body that takes an action had nowhere to be asked from;
    /// <see cref="Worlds.IActed{TSeen}"/> is that call, and <c>Drives</c> is the chooser over
    /// it. Both are built and the entry is green.
    /// </para>
    /// <para>
    /// <b>Two earlier readings of the blocker are left standing here on purpose</b>, because
    /// the trap is a doc naming the wrong one and this repo has been believed on a wrong one
    /// for a whole branch. The first said no world supplied a body; the second said a
    /// <c>Bench</c> could drive it. Both were written by enumerating names without reading
    /// what each held, which is the pass that also put <c>Recalled</c> on the stranded list
    /// when <c>Bench</c> had always driven it.
    /// </para>
    /// <para>
    /// <b>An idea ends by being built or dropped</b>, with a revival row, and this one
    /// was built. What it is worth is a separate question and the plan carries it: the chooser
    /// loses to both of its controls, and fork 111 is what that opened.
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

    /// <summary>
    /// <b>Phase two's second half</b> — a mechanism for every entry of THE ARCHITECTURE is
    /// the first, and the spine world exercising all of them is this.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The two halves are different questions and only one had a check.</b>
    /// <see cref="DocsTests.WithoutMechanism"/> reads the doc against itself:
    /// every entry carries a NOW leaf, so nothing is claimed with nothing under it. That says
    /// nothing about whether any run reaches the mechanism, and a mechanism no run reaches is
    /// the fault this repo keeps finding read as built.
    /// </para>
    /// <para>
    /// <b>The reading is <see cref="ExercisedTests"/>'s and the assertion is here</b>, which
    /// is the arrangement <see cref="Every_world_is_either_run_by_something_or_gone"/> already
    /// uses. Each entry names what a run would have to show for it, and an entry is reached
    /// when either of the world's arms shows it.
    /// </para>
    /// <para>
    /// <b>The spine is the CONVERSATION and `Roaming` is kept for the modalities</b> —
    /// John's, and it moves what this check is about rather than what it counts. An entry is
    /// reached when either spine world shows it, so the conversation growing into `Roaming`'s
    /// entries is what closes this rather than a second guard.
    /// </para>
    /// <para>
    /// <b>One is unreached, and it is not about this world.</b> A commitment's identity never
    /// sits inside another's scope because a <c>Committed</c> code is minted as a dictionary
    /// key and never enters a moment, so no genesis and no repair can root on one anywhere.
    /// Nesting is a property of the type rather than a mechanism, and the leaf claiming it
    /// says <i>expressible</i> and reads as built.
    /// </para>
    /// <para>
    /// <b>And the derivation entry closed on a reading that could not fail.</b> It asked
    /// whether <c>Population.Sorts</c> was set, which an EMPTY vocabulary satisfies — so
    /// handing the run a <see cref="Codes.Categories"/> turned it green with every counter in
    /// the run bit-identical to the control. It now asks that a group was learnt and that a
    /// category code reached a resident scope, which the first wiring did not do: the
    /// derivation was refusing every pair for meeting once, and a moment here spans three
    /// sentences.
    /// </para>
    /// <para>
    /// <b>It costs four and a half minutes</b>, being two runs of ten thousand rounds with a
    /// shuffled null taken five times over each. Three of those minutes are the shuffle, which
    /// is why nothing else calls it and why a derivation's cadence is a cost rather than a bar.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_requirement_has_a_mechanism()
    {
        var without = DocsTests.WithoutMechanism();

        foreach (var one in without) output.WriteLine($"no mechanism | {one}");

        Assert.True(without.Count == 0,
            $"{without.Count} of THE ARCHITECTURE's entries have no mechanism at all: "
            + string.Join("; ", without)
            + ". These are requirements the project has decided on and cannot yet meet, which "
            + "is a legitimate state and a RED one. Each closes by a NOW leaf arriving under "
            + "its route entry, and never by the line leaving the architecture.");
    }

    /// <summary>
    /// <b>The spine world reaches every entry of THE ARCHITECTURE.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Red again on the derivation entry</b>, and the grouping is what took it. It was
    /// green until the front end stopped flattening its parts. A word said in two statements
    /// used to be dropped from the grouping report and so constrained nothing, which let a
    /// scope naming it beside a word from a THIRD statement fire and read as being about that
    /// third one. Such a scope is about no one thing, and <see cref="Commitments.Spanning"/>
    /// now refuses it.
    /// </para>
    /// <para>
    /// <b>So the category scopes this entry looks for were spanning two statements</b>, which
    /// the front end's own report says they may not. That is the tightening working rather
    /// than a regression to undo, and what it exposed is that the mechanism was reached by a
    /// scope nobody would have written on purpose.
    /// </para>
    /// <para>
    /// <b>What closes it</b> is a category code reaching a scope about ONE statement. The
    /// derivation still learns groups, so the half that fails is the resident scope rather
    /// than the vocabulary — and a control run with the departure codes off is red the same
    /// way, so the absence derivation is not what did it.
    /// </para>
    /// <para>
    /// <b>And the second unreached entry</b> is a world's gap rather than a mechanism's. A
    /// moment can carry two of a kind and neither spine world ever sends one: a conversation's
    /// moment is a sentence and a question, which is one thing at a time. So the front end
    /// reports one part a moment for ever, and what would close it is a spine world whose
    /// moment holds two things — not a change to anything the brain does.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_spine_world_exercises_every_entry_of_the_architecture()
    {
        var unreached = ExercisedTests.StillUnreached(output.WriteLine);

        Assert.True(unreached.Count == 0,
            $"{unreached.Count} of THE ARCHITECTURE's {ExercisedTests.Asked} entries have a "
            + "mechanism the spine world never reaches:\n  "
            + string.Join("\n  ", unreached)
            + "\nPhase two is a SPINE WORLD exercising all of them, so each is phase-two work and "
            + "comes before rung four. This test is red on purpose and closes on the "
            + "mechanism being reached, not on this file.");
    }

    /// <summary>
    /// <b>Every mechanism in the brain is reached by something that drives it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The reading is <see cref="DrivenTests"/>'s and the assertion is here</b>, which is
    /// the arrangement the entry above already uses. That file says the unreached set has not
    /// changed and goes red the day a mechanism arrives dead; this one says the set is not
    /// empty, and it is the deadline.
    /// </para>
    /// <para>
    /// <b>Nought, and this entry is green.</b> Every group came off the same road: a
    /// deployment composing the mechanism rather than a fixture calling it.
    /// `OpenPlexus.Host` runs a holder and an asker over peers, which took eighteen of the
    /// fleet's twenty; <c>HybridBus</c> and its dial are apparatus and moved to
    /// <see cref="DrivenTests"/>'s instrument list. `OpenPlexus.Talk` composes a
    /// <c>Sorted</c> over a <c>Deriving</c> and hands the population the same
    /// <see cref="Codes.Categories"/>, which took the category machinery.
    /// </para>
    /// <para>
    /// <b>And rung four's matcher came off by being PROPOSED to.</b>
    /// <see cref="Commitments.Generalising"/> reads sibling groups off the residents, gates
    /// them on that vocabulary and adds the rule with a hole in it, so
    /// <c>Population.Firing</c> reaches the matcher on the ordinary path. What it is WORTH is
    /// a separate question and <see cref="GeneralisingTests"/> carries it: nothing, on the one
    /// world that can gate it, because a single hole is the drop <c>Widening</c> refuted.
    /// </para>
    /// <para>
    /// <b>Green is the point rather than a reason to delete it.</b> A mechanism arriving dead
    /// fails <see cref="DrivenTests"/> the day it lands, and this is the deadline that says
    /// the set is empty. It goes red again the moment something is built and left unreached.
    /// </para>
    /// <para>
    /// <b>And it says whether a reading can be believed.</b> A
    /// mechanism no run reaches was measured by a test calling it directly, so its number is
    /// about the call rather than about the machine.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_mechanism_in_the_brain_is_reached_by_something_that_drives_it()
    {
        var unreached = DrivenTests.Unreached();

        output.WriteLine(
            unreached.Count == 0
                ? "every mechanism in the brain is reached by something that drives it"
                : $"{unreached.Count} reached by nothing: {string.Join(", ", unreached)}");

        Assert.True(unreached.Count == 0,
            $"{unreached.Count} mechanism(s) under `Brain/` that no world, runner or harness "
            + $"reaches: {string.Join(", ", unreached)}. Each is wired into a run or deleted "
            + "with a revival row, and `DrivenTests.Waiting` carries what closes each group. "
            + "This test is red on purpose and closes on that work, not on this file.");
    }
}
