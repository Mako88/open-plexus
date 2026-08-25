using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whether the roaming world DEMANDS a situation model, asked before anything is built to
/// give it one — <b>fork 100's lesson applied on the way in rather than after.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>bAbI looked like it demanded reasoning</b> and its held-out half was all re-reading.
/// That cost this branch real time, and every score taken on it meant something other than
/// what it appeared to. So a new world is owed the same interrogation before a learner is
/// pointed at it, and the interrogation is cheap: the world knows its own state, so what a
/// shallow rule would reach is arithmetic rather than a training run.
/// </para>
/// <para>
/// <b>The three columns are the whole instrument.</b> The MARGINAL is always saying the
/// commonest room. The OPENING rule answers with the room the thing was first said to be
/// in, which is what a bag of the whole story reads straight off and is right exactly when
/// nothing moved. The LATEST rule answers with the most recent room word in the transcript,
/// which is recency and is what every displacement arm on this branch has actually been
/// doing. A perfect tracker is 1.000 by construction.
/// </para>
/// <para>
/// <b>So the world earns its keep</b> only if both shallow rules sit near the marginal. If
/// the opening rule is strong the walk is too short and the transcript answers itself; if
/// recency is strong the world is asking *what happened last* rather than *where is it
/// now*, and a situation model would be scored for something a one-line rule does.
/// </para>
/// </remarks>
public sealed class RoamingTests(ITestOutputHelper output)
{
    /// <summary>The house every reading here is taken in.</summary>
    /// <param name="steps">How long the walk is.</param>
    /// <param name="people">How many are walking it.</param>
    /// <remarks>
    /// <b>One person is the cell every earlier reading was taken at</b>, and it is named at
    /// each call rather than defaulted. A fixture inheriting a dial it does not pin is how a
    /// default moving rewrites an experiment nobody edited.
    /// </remarks>
    private static RoamingSettings World(int steps, int people) =>
        new()
        {
            Rooms = 6,
            Props = 4,
            People = people,
            Steps = steps,
            Withheld = 600,
            Examining = Examining.Where,
        };

    /// <summary>What the rules that need no learning reach on one house.</summary>
    /// <param name="world">The house.</param>
    /// <remarks>
    /// <para>
    /// <b>The marginal is always saying the commonest room.</b> The opening rule answers with
    /// the room the thing was first said to be in, which is what a bag of the whole story reads
    /// straight off and is right exactly when nothing moved. The latest rule answers with the
    /// most recent room word in the transcript, which is recency and is what every displacement
    /// arm on this branch has been doing. A perfect tracker is 1.000 by construction.
    /// </para>
    /// <para>
    /// <b>And reachable is the instrument check rather than a ceiling.</b> A transcript its own
    /// answering word is missing from is unanswerable, and every column beside it would be
    /// measuring that instead of what it says.
    /// </para>
    /// </remarks>
    private static (int Asked, double Marginal, double Opening, double Latest, int Reachable)
        Shallow(Roaming world)
    {
        var rooms = world.Named.ToList();
        var props = world.Called;

        var asked = 0;
        var marginal = new int[rooms.Count];
        var opening = 0;
        var latest = 0;
        var reachable = 0;

        foreach (var turn in world.Withheld)
        {
            if (turn.Outcome is not { } answer) continue;

            asked++;
            marginal[answer]++;

            var story = turn.Seen.Said();

            // Which thing is being asked about, read off the question. The question is a
            // handful of words and exactly one of them is a thing, so this is the front end's
            // own intersection rather than the world being asked.
            var about = props.FirstOrDefault(one => turn.Seen.Question().Contains(one));

            // `Said` is newest first, so the oldest statement naming this thing is the
            // placement that opened the episode -- which is what a bag holding the whole
            // transcript has in front of it and no reason to discount.
            var placed = story.LastOrDefault(
                one => one.Contains(about) && rooms.Any(room => one.Contains(room)));

            if (placed is not null
                && rooms.FindIndex(placed.Contains) is var was && was == answer) opening++;

            // Keyed on nothing: the newest statement holding any room word at all, which is
            // what a displacement arm reaches when the key it was given is a word every
            // sentence contains.
            var newest = story.FirstOrDefault(one => rooms.Any(room => one.Contains(room)));

            if (newest is not null
                && rooms.FindIndex(newest.Contains) is var now && now == answer) latest++;

            if (story.Any(one => one.Contains(rooms[answer]))) reachable++;
        }

        return (
            asked, marginal.Max() / (double)asked, opening / (double)asked,
            latest / (double)asked, reachable);
    }

    /// <summary>Every translation this file takes a ceiling on, named.</summary>
    /// <remarks>
    /// <b>The store walks its depth axis and the rest are one cell</b>, so the grid is
    /// a cross rather than a list — and depth nought is the store's own control, where an
    /// entry is the statement that wrote it and nothing else. <b>Written once because two
    /// ceilings read it</b>, and a grid that differed between them would make the two columns
    /// unreadable against each other.
    /// </remarks>
    private static IEnumerable<(string Name, Joined Joined)> Arms() =>
        Enum.GetValues<Joining>()
            .Where(one => one != Joining.Resolved)
            .Select(one => (Name: one.ToString(), Joined: new Joined(one)))
            .Concat(Enumerable.Range(0, 4).Select(depth =>
                (Name: $"Resolved({depth})",
                 Joined: new Joined(Joining.Resolved, resolution: depth))))
            .Concat(Enumerable.Range(1, 3).Select(depth =>
                (Name: $"Freshest({depth})",
                 Joined: new Joined(Joining.Resolved, resolution: depth, freshest: true))));

    [Fact]
    public void Whether_the_walk_makes_the_transcript_stop_answering_itself()
    {
        var shallow = new Dictionary<int, (double Marginal, double Opening, double Latest)>();

        foreach (var steps in new[] { 0, 4, 12, 30, 60, 120 })
        {
            var world = new Roaming(World(steps, people: 1), seed: 1);

            var (asked, marginal, opening, latest, reachable) = Shallow(world);

            output.WriteLine(
                $"steps {steps,3} | asked {asked,4} | marginal {marginal:F3} "
                + $"| opening {opening:F3} | latest {latest:F3} "
                + $"| answer present {reachable / (double)asked:F3}");

            shallow[steps] = (marginal, opening, latest);

            Assert.True(reachable == asked,
                $"the answering room word is missing from {asked - reachable} transcripts, so "
                + "those questions cannot be answered by anything and the world is broken "
                + "rather than hard");
        }

        // THE BAR, AND `Steps` turns out to be a dial rather than a setting. It walks the
        // shallow ceiling from a transcript that answers itself outright down to one where
        // nothing shallow beats guessing, and it does that while the marginal and the
        // recency rule stay flat -- which is one axis moving one thing, and is what a
        // benchmark with a parser, a vocabulary and a quest length all varying could not
        // have given.
        //
        // So the deepest cell is where a learner should be run, and the bar is that both
        // shallow rules have arrived at the marginal there. If either lifts off it again
        // the world has stopped demanding a situation model and every score taken on it is
        // owed a re-take.
        var (marginalAt, openingAt, latestAt) = shallow[120];

        output.WriteLine(
            $"at 120 steps the opening rule reads {openingAt:F3} and recency {latestAt:F3} "
            + $"against a marginal of {marginalAt:F3}, where a tracker reads 1.000");

        Assert.True(openingAt < marginalAt + 0.05,
            $"the opening statement still reaches {openingAt:F3} against a marginal of "
            + $"{marginalAt:F3} on the longest walk, so the transcript answers itself and "
            + "this world does not demand a situation model");

        Assert.True(latestAt < marginalAt + 0.05,
            $"recency reaches {latestAt:F3} against a marginal of {marginalAt:F3}, so the "
            + "world is asking what happened LAST rather than where the thing is now -- and "
            + "a one-line rule would be scored as a situation model");

        // And the shallow ceiling falls rather than jumps, which is what makes `Steps` worth
        // having as an axis instead of two worlds. A dial that went straight from easy to
        // impossible would be a switch, and nothing could be read off the middle of it.
        Assert.True(shallow[0].Opening > shallow[12].Opening
            && shallow[12].Opening > shallow[30].Opening
            && shallow[30].Opening > shallow[120].Opening,
            "the shallow ceiling does not fall monotonically with the length of the walk, so "
            + "`Steps` is not the one axis this file reports it as");
    }

    /// <summary>
    /// What each translation leaves in the room, taken before any learner runs — <b>the
    /// trap list's own rule</b>, that a front-end arm's ceiling costs milliseconds against a
    /// runner's hour.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two columns and they bracket one strategy rather than the learner.</b> PRESENT is
    /// whether the answering room word survives the translation at all; PINNED is whether it
    /// is the ONLY room word left, and a moment with one room word in it is answerable by a
    /// rule as simple as <i>say the room you can see</i>. What lies between them is how much
    /// choosing is left to do.
    /// </para>
    /// <para>
    /// <b>And presence is not a cap on a learner</b>, which corrects what this file used to
    /// say. A commitment expects an OUTCOME code, and no word of this world supplies
    /// one — so a moment the answering room word is missing from is still answerable, by a
    /// rule that recognises the moment and names the room. What caps an arm is
    /// <see cref="What_the_word_order_takes_out_of_the_conflated_moments"/>: two moments that
    /// are one set are one moment to every commitment there could be.
    /// </para>
    /// <para>
    /// <b>So a high present with a low pinned is the selection problem.</b> Which is where every
    /// arm on this branch has already been. The bag holds every room word in the house
    /// after a long walk, so it is 1.000 present and near nought pinned — the answer is
    /// there and nothing says which it is. An arm that raises PINNED is doing the thing a
    /// situation model is for.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_each_translation_leaves_in_the_room()
    {
        var world = new Roaming(World(120, people: 1), seed: 1);

        var rooms = world.Named.ToHashSet();
        var pinning = new Dictionary<string, double>();
        var reaching = new Dictionary<string, double>();

        foreach (var (name, joined) in Arms())
        {
            var asked = 0;
            var present = 0;
            var pinned = 0;
            var seen = 0;

            foreach (var turn in world.Withheld)
            {
                if (turn.Outcome is not { } answer) continue;

                asked++;

                var moment = joined.Codify(turn.Seen).ToHashSet();
                var left = rooms.Where(moment.Contains).ToList();

                seen += left.Count;

                if (left.Contains(world.Named[answer]))
                {
                    present++;

                    if (left.Count == 1) pinned++;
                }
            }

            pinning[name] = pinned / (double)asked;
            reaching[name] = present / (double)asked;

            output.WriteLine(
                $"{name,-14}| present {present / (double)asked:F3} "
                + $"| pinned {pinned / (double)asked:F3} | rooms left {seen / (double)asked:F2}");
        }

        // The bag is the control and it has to read this way or the instrument is wrong.
        // Every room word of a six-room house is said during a hundred and twenty steps, so
        // a translation that keeps all of them has the answer and cannot say which. If this
        // ever pins anything the world has stopped being the one the ceiling grid measured.
        Assert.True(pinning[nameof(Joining.Bagged)] < 0.05,
            $"the plain bag pins the answer on {pinning[nameof(Joining.Bagged)]:F3} of questions, so "
            + "the moment is not holding every room word and this column is measuring "
            + "something other than what it says");

        // And the finding, which is that resolving at update time is what puts the answer in
        // the room and nothing else on this branch does. Every backward-reading arm leaves it
        // present on a quarter of questions or fewer, because the newest statement about a
        // thing is *john dropped the apple* and there is no room in it -- the room is in a
        // statement about JOHN, which is not about the apple at all and no lookup keyed on
        // the apple can reach. One hop of the store reaches it, and the depth is a dial on
        // how far.
        //
        // What it costs is company, which is the selection problem arriving again smaller.
        // The fold goes through every key of a statement and the story's own background
        // calls a VERB a key, so the room john was in comes in beside the room the last
        // unrelated *went* mentioned. That is fork 95 unsolved, priced here rather than
        // argued: the gap between `present` and `pinned` is what a better key rule is worth.
        for (var depth = 1; depth < 4; depth++)
            Assert.True(reaching[$"Resolved({depth})"] > reaching[$"Resolved({depth - 1})"],
                $"a resolution depth of {depth} leaves the answer present on "
                + $"{reaching[$"Resolved({depth})"]:F3} of questions against "
                + $"{reaching[$"Resolved({depth - 1})"]:F3} one hop shallower, so folding "
                + "further is not reaching further and the depth is not the axis this reports");

        Assert.True(reaching["Resolved(1)"] > reaching[nameof(Joining.Distinguished)] + 0.2,
            $"one hop of the store reaches {reaching["Resolved(1)"]:F3} against "
            + $"{reaching[nameof(Joining.Distinguished)]:F3} for the best backward-reading arm, "
            + "so maintaining a store forwards buys nothing a lookup does not and the whole "
            + "mechanism is a one-hop chain by a longer road");

        // And which key the fold follows is worth more than how far it follows it, which is
        // not what the depth axis alone suggested. Folding through every key reaches further
        // and arrives with company; following the ONE key whose entry moved most recently
        // reaches nearly as far and arrives nearly alone. Recency over the store knows
        // nothing whatever about the text -- it does not know a verb from a name, which is
        // the thing fork 95 could not be told -- and it separates them anyway, because in
        // *john dropped the apple* john moved a statement ago and *dropped* has not moved
        // since the last drop.
        //
        // So the bar is dominance and not a level. The deepest freshest arm must beat the
        // all-keys fold on BOTH columns at once, which is what says the rule is a selection
        // and not a trade. If it ever stops, following one key is buying reach at the price
        // of choice like everything else here and this file's account of it is wrong.
        Assert.True(reaching["Freshest(3)"] > reaching["Resolved(1)"]
            && pinning["Freshest(3)"] > pinning["Resolved(1)"],
            $"following the freshest key reaches {reaching["Freshest(3)"]:F3} and pins "
            + $"{pinning["Freshest(3)"]:F3}, against {reaching["Resolved(1)"]:F3} and "
            + $"{pinning["Resolved(1)"]:F3} for folding through all of them -- so it does not "
            + "dominate, and choosing a key is a trade rather than the answer to fork 95");
    }

    /// <summary>
    /// How many moments are the same set wanting different answers, with the word order and
    /// without it — <b>what rung three is worth here, taken before a learner runs.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A conflated moment is one no scope can ever separate</b>, which is the ceiling
    /// <c>HandingTests</c> demonstrates with a single pair and this counts. A commitment fires
    /// on a subset of a set, so two moments that are the same set are one moment to every
    /// commitment there could be: whatever either expects, it is wrong about one of them. The
    /// fraction of questions sitting in such a group is a cap on any learner reading that arm,
    /// and it costs milliseconds.
    /// </para>
    /// <para>
    /// <b>And it caps in one direction only</b>, which the four-person learner settled and is
    /// recorded here because the column is read here. A high figure is a proof of a ceiling; a
    /// low one is not a floor. <see cref="Joining.Distinguished"/> and one hop of the store both
    /// conflate under a hundredth of moments at four people, and read the marginal and two and a
    /// half times it. Neither does the uniqueness of a moment explain the gap: over 0.99 of both
    /// arms' moments are distinct. What a subset test needs is a recurring SUB-scope, and
    /// nothing here counts those.
    /// </para>
    /// <para>
    /// <b>The precedences are derived here exactly as the machine derives them</b>, through
    /// <see cref="Sequenced.From"/> off the front end's own report — so this is the moment a
    /// holder would broadcast rather than a model of it. What the column says is how much of
    /// the conflation is undone by the one fact a bag of a sentence cannot carry.
    /// </para>
    /// <para>
    /// <b>The kill line was written before the run</b>: order buys nothing here unless it
    /// lowers the conflated fraction of the arm that leads. Rung three was measured on the world
    /// built for it, where roles are carried by order and by nothing else. A world whose verbs
    /// already separate its relations may have no use for it at all, and that is the result
    /// this instrument is able to return.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_the_word_order_takes_out_of_the_conflated_moments()
    {
        var world = new Roaming(World(120, people: 1), seed: 1);

        var bagged = new Dictionary<string, double>();
        var ordered = new Dictionary<string, double>();

        foreach (var (name, joined) in Arms())
        {
            var plain = new Dictionary<string, List<int>>();
            var carried = new Dictionary<string, List<int>>();
            var asked = 0;
            var placed = 0;
            var pairs = 0;

            foreach (var turn in world.Withheld)
            {
                if (turn.Outcome is not { } answer) continue;

                asked++;

                var moment = joined.Codify(turn.Seen).ToHashSet();
                var whole = new HashSet<Code>(moment);

                // The same guard `Watching` applies, so an arm reporting one position or none
                // carries exactly the codes it always carried. A report of one word entails
                // no precedence at all, which is why the count and not the null matters.
                if (joined.Order(turn.Seen) is { Count: > 1 } order)
                {
                    placed += order.Count;

                    foreach (var precedence in Sequenced.From(order))
                        if (whole.Add(precedence)) pairs++;
                }

                Group(plain, moment).Add(answer);
                Group(carried, whole).Add(answer);
            }

            bagged[name] = Conflated(plain, asked);
            ordered[name] = Conflated(carried, asked);

            output.WriteLine(
                $"{name,-14}| conflated {bagged[name]:F3} | with order {ordered[name]:F3} "
                + $"| placed {placed / (double)asked:F1} | pairs {pairs / (double)asked:F1}");
        }

        // The bag is the control and it says the thing this world already knows in a column
        // rather than in a comment: after 120 statements every word of a tiny vocabulary is
        // present, so every moment is the SAME set and all of them are conflated. That is why
        // its population came back empty -- a constant moment surprises nothing.
        Assert.True(bagged[nameof(Joining.Bagged)] > 0.99,
            $"the bag conflates {bagged[nameof(Joining.Bagged)]:F3} of questions, so the moment "
            + "is no longer a constant and this world has stopped being the one every other "
            + "reading here was taken on");

        // And the order cannot save it, which is the half worth asserting. Word order inside a
        // statement says nothing about WHICH statement, so an arm that kept every statement
        // hands over a report where almost every word was said more than once and is dropped.
        // Rung three needs a selection in front of it.
        Assert.True(ordered[nameof(Joining.Bagged)] > 0.99,
            $"the order takes the bag's conflation to {ordered[nameof(Joining.Bagged)]:F3}, so a "
            + "precedence over a whole transcript is separating moments after all and this "
            + "file's account of why selection comes first is wrong");

        // And the finding, which is that rung three is worth something on a world nobody built
        // for it. Every arm that selects more than one statement conflates less with the order
        // than without it, so the kill line is not reached: `Handing` measured a rung that
        // carries roles by position, and here the same rung separates a placement from a
        // movement -- `in` then a room against `to` then a room, which is the one distinction
        // a folded bag of two statements cannot hold.
        //
        // A one-hop chain is the exception that says what the rung needs, and it is left out
        // of the bar rather than excused: it keeps ONE statement, and one statement about a
        // thing being picked up names no room whichever way round its words stood.
        var narrowing = new[]
        {
            nameof(Joining.Distinguished), nameof(Joining.Chained),
            "Resolved(1)", "Resolved(2)", "Resolved(3)",
            "Freshest(1)", "Freshest(2)", "Freshest(3)",
        };

        foreach (var name in narrowing)
            Assert.True(ordered[name] < bagged[name],
                $"{name} conflates {ordered[name]:F3} of questions with the word order against "
                + $"{bagged[name]:F3} without it, so rung three separates nothing this arm "
                + "left conflated and the order report is not worth its derivation here");

        // The strongest cell, and it is a level rather than a comparison because what it caps
        // is a learner nothing has run yet. One hop of the store plus the order leaves a
        // twentieth of the questions in a moment no scope can separate, against three
        // quarters for the same arm reading a bag -- so a run short of that is short of the
        // front end rather than at it.
        Assert.True(ordered["Resolved(1)"] < 0.20,
            $"one hop of the store with the order conflates {ordered["Resolved(1)"]:F3}, so the "
            + "ceiling the learner arm is read against has moved and every score taken on it "
            + "is owed a re-take");
    }

    /// <summary>
    /// What a second person does to the company a fold arrives with — <b>fork 95's question
    /// on a world where the informative key exists.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>With one person the middle hop is free</b>, which is what made the key rule
    /// unfalsifiable. A thing's room is where whoever dropped it stood, so the answer is
    /// reached by following the thing to a person and the person to a room. One person is
    /// named by every action statement, so his entry accumulates the whole walk and following
    /// him is the same as following anything.
    /// </para>
    /// <para>
    /// <b>So the axis is what picking the right key is worth.</b> Four people means
    /// four narrow entries where there was one wide one, and the company a fold arrives with
    /// is what should fall. If it does not, then following the freshest key was never about
    /// reaching a person and this file's account of fork 95 is wrong.
    /// </para>
    /// <para>
    /// <b>The walk is held at the length every other reading here uses</b>, so a transcript is
    /// the same size whoever is walking it. That does make each person's own walk shorter, and
    /// the shallow columns are printed at every cell for exactly that reason: a world that got
    /// easier rather than differently hard says so in the opening rule.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_a_second_person_does_to_the_key_the_fold_must_follow()
    {
        var company = new Dictionary<(int People, string Arm), List<double>>();
        var pinning = new Dictionary<(int People, string Arm), List<double>>();
        var peopled = new Dictionary<(int People, string Arm), List<double>>();
        var twinned = new Dictionary<(int People, string Arm), List<double>>();
        var distinct = new Dictionary<(int People, string Arm), List<double>>();

        var reading = new[]
        {
            nameof(Joining.Distinguished), nameof(Joining.Chained),
            "Resolved(1)", "Resolved(3)", "Freshest(1)", "Freshest(3)",
        };

        foreach (var people in new[] { 1, 2, 4 })
        {
            foreach (var name in reading)
            {
                company[(people, name)] = [];
                pinning[(people, name)] = [];
                peopled[(people, name)] = [];
                twinned[(people, name)] = [];
                distinct[(people, name)] = [];
            }

            // Seeds, because a column read on one house is an anecdote and this axis moves
            // things by hundredths. The first version of this fact rested a bar on one seed
            // and a gap of eleven thousandths, which is the trap this repo names first.
            foreach (var seed in new[] { 1, 2, 3 })
            {
                var world = new Roaming(World(120, people), seed);

                var rooms = world.Named.ToList();
                var cast = world.Walking.ToHashSet();

                // The same shallow rules the first fact reads, at this cell rather than at one
                // person. A world whose opening placement is answering again is a world where
                // the walk has become too short to move anything, and every column below it
                // would be measuring the length of a walk.
                var (asked, marginal, opening, _, reachable) = Shallow(world);

                output.WriteLine(
                    $"people {people} | seed {seed} | asked {asked,4} | marginal {marginal:F3} "
                    + $"| opening {opening:F3} | answer present {reachable / (double)asked:F3}");

                Assert.True(reachable == asked,
                    $"with {people} people the answering room word is missing from "
                    + $"{asked - reachable} transcripts, so those questions cannot be answered "
                    + "by anything and a second person has broken the world rather than "
                    + "deepened it");

                Assert.True(opening < marginal + 0.05,
                    $"with {people} people the opening statement reaches {opening:F3} against a "
                    + $"marginal of {marginal:F3}, so the walk is no longer moving things and "
                    + "this axis is measuring its own length");

                foreach (var (name, joined) in Arms().Where(one => reading.Contains(one.Name)))
                {
                    var pinned = 0;
                    var left = 0;
                    var whose = 0;
                    var carried = new Dictionary<string, List<int>>();

                    foreach (var turn in world.Withheld)
                    {
                        if (turn.Outcome is not { } answer) continue;

                        var moment = joined.Codify(turn.Seen).ToHashSet();
                        var here = rooms.Where(moment.Contains).ToList();
                        var whole = new HashSet<Code>(moment);

                        left += here.Count;

                        // And how many people the moment leaves, which is the same ambiguity one
                        // level up: a fold holding two people is a fold that cannot say whose
                        // room it is holding, however few rooms it kept.
                        whose += cast.Count(moment.Contains);

                        if (here.Count == 1 && here[0] == rooms[answer]) pinned++;

                        // And the column that actually caps a learner, carried alongside the two
                        // that cap reading the answer off the room. See the conflation grid: a
                        // commitment expects an outcome code, so a moment with no room word in it
                        // is still answerable and a moment that is another's twin is not.
                        if (joined.Order(turn.Seen) is { Count: > 1 } order)
                            foreach (var precedence in Sequenced.From(order)) whole.Add(precedence);

                        Group(carried, whole).Add(answer);
                    }

                    company[(people, name)].Add(left / (double)asked);
                    pinning[(people, name)].Add(pinned / (double)asked);
                    peopled[(people, name)].Add(whose / (double)asked);
                    twinned[(people, name)].Add(Conflated(carried, asked));

                    // How many different moments the questions came in, which is what says
                    // whether a low conflation means SEPARATED or merely one-off. A share near
                    // one is a moment per question, and a rule minted on one of those has a
                    // single observation for ever.
                    distinct[(people, name)].Add(carried.Count / (double)asked);
                }
            }

            foreach (var name in reading)
            {
                output.WriteLine(
                    $"  {name,-14}| pinned {pinning[(people, name)].Min():F3} to "
                    + $"{pinning[(people, name)].Max():F3} | rooms left "
                    + $"{company[(people, name)].Min():F2} to {company[(people, name)].Max():F2}"
                    + $" | people left {peopled[(people, name)].Min():F2} to "
                    + $"{peopled[(people, name)].Max():F2} | conflated "
                    + $"{twinned[(people, name)].Min():F3} to {twinned[(people, name)].Max():F3}"
                    + $" | distinct {distinct[(people, name)].Min():F3} to "
                    + $"{distinct[(people, name)].Max():F3}");
            }
        }

        // The finding, and it is about which key rather than how many people. Following the
        // freshest key arrives with less company the moment there is a key worth following,
        // because a person named by a quarter of the action statements holds a quarter of the
        // walk -- so the entry the fold reaches through is narrow for a reason the rule knows
        // nothing about. That is fork 95's answer measured rather than argued.
        //
        // WORST AGAINST BEST, which is the form this file uses everywhere a lead has to
        // survive the seed spread: the four-person cell at its widest must still be narrower
        // than the one-person cell at its narrowest.
        Assert.True(company[(4, "Freshest(1)")].Max() < company[(1, "Freshest(1)")].Min(),
            $"the freshest key arrives with {company[(4, "Freshest(1)")].Max():F2} rooms at four "
            + $"people at its widest against {company[(1, "Freshest(1)")].Min():F2} at one at its "
            + "narrowest, so a second person is not narrowing what the fold drags along and "
            + "following one key was never about reaching a person");

        // And what it PINS does not separate over three seeds, 0.169 to 0.187 at one person
        // against 0.180 to 0.191 at four, so there is no bar on it here. The company falling
        // while the answer stays is the whole of what this cell says.

        // WHICH PERSON THE FOLD REACHED, and this is the column that says the key rule is doing
        // what it is named for. The comparison is against the fold at the SAME DEPTH, which is
        // the only other arm differing from it in nothing but the key rule -- at four people it
        // reaches 2.14 people in the moment at its worst against 3.01 for folding through all
        // of them. So recency over the store narrows towards one person without knowing that a
        // person is a thing, which is fork 95's answer in the units of the question.
        //
        // Against `Chained` it does NOT, and that was asserted here until the placements were
        // stated: a lookup keeping one statement holds few people for a reason that has nothing
        // to do with choosing. The bar was comparing two mechanisms and reading the answer off
        // how much text each kept, which is the trap about a comparison that moves two things.
        Assert.True(peopled[(4, "Freshest(1)")].Max() < peopled[(4, "Resolved(1)")].Min(),
            $"at four people folding through every key reaches "
            + $"{peopled[(4, "Resolved(1)")].Min():F2} people at its fewest against "
            + $"{peopled[(4, "Freshest(1)")].Max():F2} at the most for the freshest key, so "
            + "following the key that moved last is not selecting a person at all");

        // The premise the four-person learner's refutation rests on, pinned where the columns
        // are taken. Two arms whose caps and whose uniqueness are both indistinguishable read a
        // third of each other, so if either half of that stops holding the refutation is owed a
        // re-take rather than quietly surviving in a comment.
        //
        // And no other arm can stand in this cell, which is the reading that stopped a
        // deletion. `Distinguished` was listed for removal as a joining that loses to
        // `Chained` everywhere -- it does, 0.152 to 0.166 against 0.177 to 0.191 at four
        // people, both at or under a marginal near 0.19. What it does that nothing else does
        // is conflate 0.000 to 0.004 while scoring the marginal, and `Chained` conflates
        // 0.121 to 0.139 here, an order of magnitude away. So the arm that would be deleted
        // for being worse is the one holding half of the only comparison that refutes the
        // conflation column as a ranking, and there is no substitute in the tree.
        foreach (var name in new[] { nameof(Joining.Distinguished), "Resolved(1)" })
        {
            Assert.True(twinned[(4, name)].Max() < 0.01 && distinct[(4, name)].Min() > 0.99,
                $"at four people {name} conflates {twinned[(4, name)].Max():F3} of moments and "
                + $"{distinct[(4, name)].Min():F3} of them are distinct, so it no longer sits "
                + "where a cap and a uniqueness are both near their limit and the reading that "
                + "says neither ranks an arm has lost one of its two cells");
        }

        // What a second person does to the background rule, which is the finding nobody was
        // looking for and is larger than the one that was. `Distinguished` calls a word not in
        // every statement a key, so a room is a key -- and with four people walking, a newer
        // statement about a room supersedes the placement of a thing that is still sitting
        // there. It pins an order of magnitude less at four people than at one.
        Assert.True(pinning[(4, nameof(Joining.Distinguished))].Max()
            < pinning[(1, nameof(Joining.Distinguished))].Min() / 4,
            $"the background rule pins {pinning[(4, nameof(Joining.Distinguished))].Max():F3} at "
            + $"four people against {pinning[(1, nameof(Joining.Distinguished))].Min():F3} at "
            + "one, so a second person is not what breaks displacement and the account above is "
            + "wrong about why");

        // So the arms come apart on this axis, which is what an axis is for. At four people one
        // hop through the freshest key leads every other arm here at its worst against their
        // best -- including the same rule folding three hops, which arrives with more company
        // than it started with.
        //
        // And this column is not a prediction, which the learner at this cell settled and is
        // worth saying where the number is printed. `pinned` got the depth comparison right and
        // the key comparison wrong: `Resolved(1)` pins 0.029 here and OUTSCORES the freshest key
        // at 0.180, because it leaves two rooms in the moment against one and a quarter and a
        // commitment that recognises a moment does not need the answer word inside it. What this
        // caps is reading the answer off the room, which is one strategy rather than the learner.
        // The conflated column is the cap, and it got the same two comparisons the other way
        // round -- so neither ranks these arms and both bound them.
        foreach (var name in reading.Where(one => one != "Freshest(1)"))
            Assert.True(pinning[(4, "Freshest(1)")].Min() > pinning[(4, name)].Max(),
                $"at four people {name} pins {pinning[(4, name)].Max():F3} at its best against "
                + $"{pinning[(4, "Freshest(1)")].Min():F3} at the worst for one hop through the "
                + "freshest key, so following the key that moved last is not what this world "
                + "rewards and the spine's next tier is aimed at the wrong arm");
    }

    /// <summary>
    /// Whether the transcript can be followed to the answer at all — <b>the world's own claim
    /// checked</b>, rather than asserted in a comment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The probe knows the shape of the walk and may never ship</b>, which is
    /// <c>HandingTests</c>'s standing for the same kind of instrument. It follows the true
    /// chain: the newest statement naming the thing and a person is the drop that settled it,
    /// and the room is the newest statement older than that one naming the same person and a
    /// room. A thing never picked up is where it was first said to be.
    /// </para>
    /// <para>
    /// <b>And a chain that runs out is the number worth having.</b> A person's starting room is
    /// drawn and never stated, so somebody who takes a thing and puts it down before moving has
    /// put it somewhere no rule can name. That share is a hard cap on every arm and on every
    /// learner, and it is not the same thing as the answering word being absent — the word is
    /// present whenever any statement happens to mention that room.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_the_true_chain_can_be_followed_to_the_answer()
    {
        foreach (var people in new[] { 1, 2, 4 })
        {
            var world = new Roaming(World(120, people), seed: 1);

            var rooms = world.Named.ToList();
            var props = world.Called;
            var cast = world.Walking.ToHashSet();

            var asked = 0;
            var followed = 0;
            var right = 0;

            foreach (var turn in world.Withheld)
            {
                if (turn.Outcome is not { } answer) continue;

                asked++;

                var story = turn.Seen.Said();
                var about = props.FirstOrDefault(one => turn.Seen.Question().Contains(one));

                // The newest statement naming the thing and a person, which is the event that
                // settled where it is. A thing still in a hand is never asked about, so this
                // statement is a drop whenever it exists at all -- no verb has to be read.
                var dropped = -1;

                for (var at = 0; at < story.Count && dropped < 0; at++)
                    if (story[at].Contains(about) && story[at].Any(cast.Contains)) dropped = at;

                if (dropped < 0)
                {
                    // Never handled, so the opening placement still stands. `Said` is newest
                    // first, so the oldest statement naming the thing is the one that placed it.
                    var placed = story.LastOrDefault(
                        one => one.Contains(about) && rooms.Any(room => one.Contains(room)));

                    if (placed is null) continue;

                    followed++;

                    if (rooms.FindIndex(placed.Contains) == answer) right++;

                    continue;
                }

                var who = story[dropped].First(cast.Contains);
                var moved = -1;

                // Older only, which is what makes this the chain rather than a lookup. Where
                // the person stood when the thing was put down is the last move BEFORE that
                // statement, and a later move took them somewhere the thing did not go.
                for (var at = dropped + 1; at < story.Count && moved < 0; at++)
                    if (story[at].Contains(who) && story[at].Any(room => rooms.Contains(room)))
                        moved = at;

                if (moved < 0) continue;

                followed++;

                if (rooms.FindIndex(story[moved].Contains) == answer) right++;
            }

            output.WriteLine(
                $"people {people} | asked {asked,4} | followed {followed / (double)asked:F3} "
                + $"| right {right / (double)followed:F3} "
                + $"| right of all {right / (double)asked:F3}");

            // The half that says the probe is following the world rather than guessing at it.
            // Where the chain completes it is the answer, exactly, at every cell -- so a gap
            // in the column beside it is a transcript that cannot be followed and never a
            // rule that followed it wrongly.
            Assert.True(right == followed,
                $"the chain completed on {followed} questions and answered {right} of them, so "
                + "following the drop to the person to the room is not what this world does and "
                + "every ceiling in this file is measuring against the wrong ground truth");

            // And the cap the gap is. A person's starting room is drawn and never stated, so
            // one who takes a thing and puts it down before moving has put it somewhere no rule
            // can name: nought at one and two people, five thousandths at four. It stays a
            // recorded cap rather than a repair, because stating the starting rooms would add a
            // statement to every transcript and move every reading ever taken on this world for
            // a twentieth of a point. The bar is here so that a change making it large fails.
            Assert.True(followed / (double)asked > 0.99,
                $"with {people} people the chain runs out on "
                + $"{1.0 - (followed / (double)asked):F3} of questions, so a real share of this "
                + "world is unanswerable by anything and every ceiling beside it is being read "
                + "as if 1.000 were reachable");
        }
    }

    /// <summary>Which questions share one moment, keyed so two machines agree.</summary>
    /// <param name="groups">Every moment seen so far.</param>
    /// <param name="moment">One moment.</param>
    private static List<int> Group(Dictionary<string, List<int>> groups, HashSet<Code> moment)
    {
        var key = string.Join(
            ",", moment.Select(one => $"{one.Modality}:{one.Value}").Order(StringComparer.Ordinal));

        if (!groups.TryGetValue(key, out var answers)) groups[key] = answers = [];

        return answers;
    }

    /// <summary>What share of the questions sit in a moment that wants two answers.</summary>
    /// <param name="groups">Every moment seen, and which answers it was asked for.</param>
    /// <param name="asked">How many questions there were.</param>
    private static double Conflated(Dictionary<string, List<int>> groups, int asked) =>
        groups.Values.Where(answers => answers.Distinct().Count() > 1).Sum(answers => answers.Count)
        / (double)asked;

    /// <summary>
    /// What the learner reads where nothing shallow works — <b>the first score on a world
    /// whose held-out half is genuinely unseen.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>At 120 steps</b>, which is the cell the ceiling grid chose rather than a round
    /// number. The opening rule and recency both sit at the marginal there, so anything
    /// over it is tracking rather than reading the transcript off.
    /// </para>
    /// <para>
    /// <b>And the vocabulary is tiny whatever the walk's length.</b> Which is the property that
    /// makes the bag arm mean something. Six rooms, four things and a handful of
    /// function words, so a bagged moment is the same size after 120 statements as after
    /// four — every word is present and none of them says WHEN. A bag here cannot be near
    /// the marginal by accident.
    /// </para>
    /// </remarks>
    /// <summary>What each arm's learner scores on the held-out half, over three seeds.</summary>
    /// <param name="arms">The translations to run.</param>
    /// <param name="people">How many walk the house.</param>
    /// <param name="examining">Which question the house is asked.</param>
    /// <param name="acting">
    /// What to do about the state the walk is in, or nothing to leave it drawing its own
    /// steps — <b>the arm every earlier reading was taken on.</b>
    /// </param>
    /// <remarks>
    /// <b>Seeds, because a comparison on one run is an anecdote.</b> The world and the brain
    /// take the same seed, so an arm's whole run moves together rather than the house being
    /// redrawn under a fixed population. <b>Written once because two grids read it</b>, and
    /// two loops that drifted apart would make their columns unreadable against each other.
    /// </remarks>
    /// <param name="dials">
    /// The brain, or nothing for the one the spine walks this house with —
    /// <b><see cref="ExercisedTests.Walking"/> rather than a literal here</b>, so a dial that
    /// converges moves this grid with it and a reading in this file is about the brain the
    /// spine runs rather than about a fixture's.
    /// </param>
    /// <param name="departing">
    /// Whether the join derives a code for what was live and is not now — <b>off everywhere
    /// this world has ever run</b>, so the architecture entry it serves has been reached by
    /// instruments alone.
    /// </param>
    /// <param name="absences">
    /// Told, per seed, how many residents name a departure — <b>the reading the dial owes
    /// rather than a score.</b> A code reaching every moment and no scope is what refuted
    /// fork 36, and a count is the only thing that separates it from the dial being off.
    /// </param>
    private Dictionary<string, List<double>> Scored(
        IEnumerable<(string Name, Joined Joined)> arms,
        int people,
        Examining examining,
        IChooses? acting = null,
        CommittingSettings? dials = null,
        Departing departing = Departing.Never,
        Action<int>? absences = null)
    {
        var scores = new Dictionary<string, List<double>>();

        foreach (var (name, joined) in arms)
        {
            scores[name] = [];

            foreach (var seed in new[] { 1, 2, 3 })
            {
                var world = new Roaming(
                    World(120, people) with { Examining = examining }, seed);

                var brain = new Brain(dials ?? ExercisedTests.Walking, seed);

                var tally = new Bench(
                    new Watching<Coded>(
                        world,
                        joined,
                        acting: acting ?? Chooses.From(_ => null),
                        departing: departing),
                    brain)
                    .Run(10_000, sweep: 1000, target: 0.9, window: 2000);

                var exam = tally.Unseen?.Accuracy ?? 0.0;

                scores[name].Add(exam);

                var absent = brain.Held.All.Count(one => one.Scope.Any(Departed.Names));

                absences?.Invoke(absent);

                output.WriteLine(
                    $"{name,-12}| seed {seed} | exam {exam:F3} | own {tally.Recent:F3} "
                    + $"| held {brain.Held.Count} | repaired {tally.Repaired} "
                    + $"| absences {absent}");
            }
        }

        foreach (var (name, taken) in scores)
            output.WriteLine($"{name,-12}| worst {taken.Min():F3} | best {taken.Max():F3}");

        return scores;
    }

    /// <summary>
    /// What reading a statement as a THING costs the spine world — <b>the reading
    /// <see cref="Joined"/>'s parts owe the moment they exist.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The dial had a reader and no filler.</b> <see cref="Spanning.Thing"/> ships as the
    /// brain's default and was chosen on <c>Worlds.Binding</c>, an instrument whose parts are
    /// objects in a scene. No text front end ever filled the channel, so every score in this
    /// file was taken under a mechanism that could not fire — the control by accident rather
    /// than by design.
    /// </para>
    /// <para>
    /// <b>What would refute the default</b>, said before the run: this world's answer is
    /// reached by two hops through two statements, so a scope confined to one of them cannot
    /// express it. If <see cref="Spanning.Thing"/> reads below <see cref="Spanning.Anything"/>
    /// at its best against the other's worst, a statement is the wrong grain of thing for text
    /// and the default is an instrument's answer applied to the target.
    /// </para>
    /// <para>
    /// <b>One arm rather than the grid</b>, and it is the leading one. What is being separated
    /// is the dial and not the translation, so a second translation would move two things at
    /// once for no extra reading.
    /// </para>
    /// <para>
    /// <b>Measured, three seeds</b>: 0.587, 0.642 and 0.538 confined against 0.589, 0.620 and
    /// 0.539 unconfined. Two thousandths behind on two seeds and two hundredths ahead on the
    /// third, so the prediction that a two-hop answer would be forbidden is refuted at this
    /// arm.
    /// </para>
    /// <para>
    /// <b>And the parts column says the dial had plenty to do</b>, which is the half that
    /// stops this being read as a mechanism with nothing to bite on: every moment of five
    /// hundred holds more than one part and the most is seven. It bites and it is not free
    /// either — the confined arm holds 653, 986 and 841 residents against 800, 949 and 1,033,
    /// so a ninth of the population goes and no answer moves. What it refuses is worth
    /// nothing, which is a stronger reading than the score alone.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_reading_a_statement_as_a_thing_costs_the_spine_world()
    {
        var arm = new (string Name, Joined Joined)[]
        {
            ("Freshest(3)", new Joined(Joining.Resolved, resolution: 3, freshest: true)),
        };

        // How often the arm reports two things at all, taken before any learner runs. A dial
        // that changed nothing and a dial that had nothing to change read alike from a score,
        // which is this repo's own trap, and it costs milliseconds to tell them apart.
        var world = new Roaming(World(120, people: 1) with { Examining = Examining.Where }, 1);
        var parts = new List<int>();

        for (var round = 0; round < 500; round++)
            parts.Add(arm[0].Joined.Bind(world.Next().Seen)?.Count ?? 0);

        output.WriteLine(
            $"parts a moment   | most {parts.Max()} "
            + $"| over one {parts.Count(one => one > 1) / (double)parts.Count:F3}");

        var thing = Scored(arm, people: 1, Examining.Where)["Freshest(3)"];

        var anything = Scored(
            arm, people: 1, Examining.Where,
            dials: ExercisedTests.Walking with { Spanning = Spanning.Anything })["Freshest(3)"];

        output.WriteLine(
            $"one thing a scope | worst {thing.Min():F3} | best {thing.Max():F3}");
        output.WriteLine(
            $"any thing a scope | worst {anything.Min():F3} | best {anything.Max():F3}");

        // Worst against best, which is this file's own bar and cannot be gamed by a run that
        // landed well. Stated as the refutation rather than as the expectation: what is
        // asserted is that a text world does not lose the exam to a dial chosen on a scene.
        Assert.True(thing.Max() >= anything.Min(),
            $"confining a scope to one statement reads {thing.Max():F3} at its best against "
            + $"{anything.Min():F3} at the unconfined arm's worst, so a statement is the wrong "
            + "grain of THING for text and `Spanning.Thing` is an instrument's answer shipped "
            + "as the target's default");
    }

    /// <summary>
    /// What a code for what LEFT does on the spine world — <b>the entry no spine run has
    /// ever reached.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A requirement rather than an arm.</b> <c>Departed</c> is the one mechanism under
    /// <i>it can say what does NOT hold</i> and there is no alternative to it, so a losing
    /// score is a cost to record rather than grounds to switch it off. What it owes is a
    /// check that can FAIL, and this is that check.
    /// </para>
    /// <para>
    /// <b>Why this world and not the conversation.</b> A retraction here is visible: a
    /// forward store keyed on the freshest word replaces the statement about a room the
    /// moment somebody leaves it, so the room word stops being live and the departure says
    /// so. Under <c>Asserting.Everything</c> a sentence is one moment a word, so every word
    /// departs every round and the code separates nothing.
    /// </para>
    /// <para>
    /// <b>What would refute the siting</b>, said before the run: no resident scope naming a
    /// departure. Then the code reaches the moment and no scope, which is fork 36's
    /// refutation one seam over, and the entry wants another world rather than this dial.
    /// </para>
    /// <para>
    /// <b>Measured, three seeds</b>: 284, 523 and 593 residents name a departure, against a
    /// population of 986, 1,453 and 1,411 — so between a fifth and two fifths of what the
    /// machine holds is a rule about something having stopped. It reaches a scope easily and
    /// the siting stands.
    /// </para>
    /// <para>
    /// <b>And it costs half again the population for a hundredth.</b> The
    /// exam reads 0.587, 0.642 and 0.548 against 0.587, 0.642 and 0.538 quiet — identical
    /// twice and ten thousandths ahead once — while the population goes from 653, 986 and 841
    /// to 986, 1,453 and 1,411. That is the refuted every-world default's own cost arriving
    /// where a world asked for it, and it is recorded rather than acted on: this entry has one
    /// mechanism and no alternative, so a price is a fact about saying an absence and never a
    /// reason to stop being able to.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_code_for_what_left_does_on_the_spine_world()
    {
        var arm = new (string Name, Joined Joined)[]
        {
            ("Freshest(3)", new Joined(Joining.Resolved, resolution: 3, freshest: true)),
        };

        var scoped = new List<int>();

        var quiet = Scored(arm, people: 1, Examining.Where)["Freshest(3)"];

        var leaving = Scored(
            arm, people: 1, Examining.Where, departing: Departing.Left,
            absences: scoped.Add)["Freshest(3)"];

        output.WriteLine($"no departures | worst {quiet.Min():F3} | best {quiet.Max():F3}");
        output.WriteLine(
            $"departures    | worst {leaving.Min():F3} | best {leaving.Max():F3} "
            + $"| absences scoped {string.Join(", ", scoped)}");

        // The score is recorded beside the count rather than gated on it, which is what a
        // requirement's reading looks like. A cost here is a fact about what saying an
        // absence costs this world and never a reason to stop being able to say one.
        Assert.True(scoped.Sum() > 0,
            $"a departure code was derived on every round and reached {scoped.Sum()} resident "
            + "scopes over three seeds, so the moment carries it and nothing can name it -- "
            + "which is the code-reaches-no-scope refutation, and the entry needs a world "
            + "whose absences a conjunction can use rather than this dial");
    }

    /// <summary>
    /// What the conversation's two dials cost the walk — <b>the cheaper road to one brain
    /// across the spine.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The spine runs two brains and this is the half nobody measured.</b>
    /// <c>OpenPlexus.Talk</c> ships <c>Crediting.Birth</c> and <c>Admitting.Testable</c> where
    /// the walk takes the brain's defaults, so every score off either world is a comparison
    /// between brains as much as between problems.
    /// <c>OutstandingTests.The_spine_runs_one_brain</c> is red for it.
    /// </para>
    /// <para>
    /// <b>Both were chosen on the conversation and neither was refused here</b>, which is the
    /// asymmetry. A conversation says a thing once, so a mint that waits to hear it twice sits
    /// mute — that is what <c>Birth</c> is for, and <c>LessonTests</c> holds its reading. This
    /// house repeats itself constantly, so the same dial may be free or may flatten the vote
    /// among a hundred rules that all arrive perfect.
    /// </para>
    /// <para>
    /// <b>What would refute the convergence</b>, said before the run: the conversation's pair
    /// reading below the walk's own defaults, worst against best. Then the two worlds want
    /// different brains for a measured reason and fork 147 is the only road left, rather than
    /// the cheap one.
    /// </para>
    /// <para>
    /// <b>And the repair column is what a score cannot say.</b>
    /// <c>LessonTests.What_the_admission_bar_costs_the_conversation_it_is_shipped_on</c> reads
    /// the bar as exam-neutral there and finds it removes repair entirely, so converging on
    /// the bar would be converging on a spine that never runs the one operator that deepens a
    /// rule. Which value to converge on is decided by that column and not by the exam.
    /// </para>
    /// <para>
    /// <b>Measured, and the convergence it appeared to license was refused.</b> Four cells
    /// within four thousandths at both ends, and the bar takes the walk from 811, 1,236 and
    /// 1,035 repairs to 565, 921 and 708 while holding two thirds of the rules — so this grid
    /// says converge. <c>ExercisedTests</c> says do not: this reads the WHERE question and the
    /// EFFECT arm falls from 105 residents and 131 repairs to 39 and 7 under the same bar,
    /// taking an entry of THE ARCHITECTURE off the spine with it.
    /// </para>
    /// <para>
    /// <b>So the QUESTION is an axis here now</b>, and it was the reading this grid owed. A
    /// dial crossed against one of a world's two questions is a dial measured on half its
    /// world, which is this file's own trap read one level out.
    /// </para>
    /// <para>
    /// <b>And the effect question's exam is indifferent too</b>: 0.803, 0.858 and 0.828
    /// ungated against 0.805, 0.855 and 0.827 under the bar, where the where question reads
    /// 0.587, 0.642 and 0.538 against 0.612, 0.640 and 0.534. Eight cells and nothing outside
    /// the seed spread, so the exam cannot decide this dial on either half of this world.
    /// </para>
    /// <para>
    /// <b>What moves is what the run HOLDS.</b> The bar takes the effect arm from 164, 193 and
    /// 254 residents to 152, 168 and 194, and its repairs from 216, 242 and 276 to 159, 190
    /// and 209 — a fifth of the population and a quarter of the ladder for no answer. A kill
    /// line written on the exam passes over all of that, which is what this grid did twice.
    /// </para>
    /// <para>
    /// <b>And a fifth is not what the spine's own composition reads.</b> There the same bar
    /// takes the effect arm to 39 residents and 7 repairs and the derivation stops reaching a
    /// scope. This grid runs a bare front end and no chooser; the spine runs a derived
    /// vocabulary and one that acts. So the cost is in the interaction rather than in the bar,
    /// and a dial priced on the simpler machine is priced on the wrong one.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_the_conversations_two_dials_cost_the_walk()
    {
        var arm = new (string Name, Joined Joined)[]
        {
            ("Freshest(3)", new Joined(Joining.Resolved, resolution: 3, freshest: true)),
        };

        var cells = new List<(string Name, List<double> Scores)>();

        // BOTH QUESTIONS, because the first version of this asked only the where one and its
        // verdict was wrong. The house is drawn identically under both, so a difference
        // between them is the question -- and the effect arm is where the bar was measured to
        // take an entry of THE ARCHITECTURE off the spine.
        foreach (var examining in new[] { Examining.Where, Examining.Effect })
            foreach (var crediting in new[] { Crediting.Nothing, Crediting.Birth })
                foreach (var admitting in new[] { Admitting.Anything, Admitting.Testable })
                {
                    var name = $"{examining}|{crediting}+{admitting}";

                    output.WriteLine(name);

                    cells.Add((
                        name,
                        Scored(
                            arm, people: 1, examining,
                            dials: ExercisedTests.Walking with
                            {
                                Crediting = crediting,
                                Admitting = admitting,
                            })["Freshest(3)"]));
                }

        foreach (var (name, taken) in cells)
            output.WriteLine($"{name,-30}| worst {taken.Min():F3} | best {taken.Max():F3}");

        var walks = cells.Single(one => one.Name == "Where|Nothing+Anything").Scores;
        var talks = cells.Single(one => one.Name == "Where|Birth+Testable").Scores;

        var acts = cells.Single(one => one.Name == "Effect|Nothing+Anything").Scores;
        var bars = cells.Single(one => one.Name == "Effect|Birth+Testable").Scores;

        // Worst against best, this file's own bar. What is asserted is that the walk does not
        // LOSE to the pair the conversation ships, which is what makes converging on that pair
        // a change of one composition rather than a trade between two worlds.
        Assert.True(talks.Max() >= walks.Min(),
            $"the conversation's pair reads {talks.Max():F3} at its best against "
            + $"{walks.Min():F3} for the walk's own defaults at their worst on the where "
            + "question, so the two spine worlds want different brains for a measured reason "
            + "and one composition cannot serve both -- which leaves fork 147 as the only road "
            + "to one brain");

        // And the same on the other question, asserted separately rather than folded in. A
        // pair that serves one question of a world and not the other is not a pair that
        // serves the world, and a single bar over both would let one carry the other.
        Assert.True(bars.Max() >= acts.Min(),
            $"on the effect question the conversation's pair reads {bars.Max():F3} at its best "
            + $"against {acts.Min():F3} for the walk's own defaults at their worst, so the "
            + "convergence the where question licenses is refused by the other half of the "
            + "same world");
    }

    /// <summary>
    /// What the genesis gate costs the spine world — <b>one of the dials read only where
    /// the answer does not count.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The refutation behind it warns, and does not read this world.</b>
    /// Minting on every failure walks the whole <c>code → outcome</c> space given enough
    /// failures: on winnowed CIFAR that space is 25,600 and the population reached 23,762
    /// against a capacity of 2,000. CIFAR is an instrument, and what a gate costs where the
    /// alphabet is a house's vocabulary is a different question.
    /// </para>
    /// <para>
    /// <b>Both questions, because the last dial grid asked one and erred.</b>
    /// A gate on what genesis mints is not obviously question-shaped, which is exactly what
    /// was assumed about <c>Admitting</c> before the effect arm was measured.
    /// </para>
    /// <para>
    /// <b>What would refute the shipped gate</b>, said before the run: the ungated arm reading
    /// above it, worst against best, on either question. Then <c>Unaccounted</c> is refusing
    /// proposals this world needed and the CIFAR row is a fact about a wide alphabet rather
    /// than about the mechanism.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_the_genesis_gate_costs_the_spine_world()
    {
        var arm = new (string Name, Joined Joined)[]
        {
            ("Freshest(3)", new Joined(Joining.Resolved, resolution: 3, freshest: true)),
        };

        var cells = new List<(string Name, List<double> Scores)>();

        foreach (var examining in new[] { Examining.Where, Examining.Effect })
            foreach (var surprising in new[] { Surprising.Unaccounted, Surprising.AnyFailure })
            {
                var name = $"{examining}|{surprising}";

                output.WriteLine(name);

                cells.Add((
                    name,
                    Scored(
                        arm, people: 1, examining,
                        dials: ExercisedTests.Walking with { Surprising = surprising })
                        ["Freshest(3)"]));
            }

        foreach (var (name, taken) in cells)
            output.WriteLine($"{name,-30}| worst {taken.Min():F3} | best {taken.Max():F3}");

        foreach (var examining in new[] { Examining.Where, Examining.Effect })
        {
            var gated = cells.Single(one => one.Name == $"{examining}|Unaccounted").Scores;
            var open = cells.Single(one => one.Name == $"{examining}|AnyFailure").Scores;

            Assert.True(gated.Max() >= open.Min(),
                $"on the {examining} question the shipped gate reads {gated.Max():F3} at its "
                + $"best against {open.Min():F3} for the ungated arm at its worst, so "
                + "`Unaccounted` is refusing proposals this world needed and the reading that "
                + "put it in the code was a fact about a wide alphabet rather than about the "
                + "mechanism");
        }
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_learner_reads_where_the_transcript_stops_answering_itself()
    {
        // The backward-reading arm that leads, and the store's two axes crossed. `Bagged`
        // and the one-hop chain are not here because they are compared in the ceiling grid and
        // both sit at the marginal; `Chained` at its default depth is the one worth beating,
        // being the best any lookup over the transcript reached on this world.
        var arms = new (string Name, Joined Joined)[]
        {
            (nameof(Joining.Chained), new Joined(Joining.Chained)),
        }
        .Concat(new[] { 1, 3 }.SelectMany(depth => new[]
        {
            ($"Resolved({depth})", new Joined(Joining.Resolved, resolution: depth)),
            ($"Freshest({depth})",
                new Joined(Joining.Resolved, resolution: depth, freshest: true)),
        }))
        .ToList();

        var scores = Scored(arms, people: 1, Examining.Where);

        // The bar is two comparisons and neither is a level. First, that following the
        // freshest key beats folding through all of them AT THE SAME DEPTH -- which isolates
        // the key rule, the two arms differing in nothing else. Second, that it beats the
        // best lookup over the transcript, which is what says a forward store is worth
        // building at all.
        //
        // WORST AGAINST BEST, so a lead has to survive the seed that suited it least meeting
        // the seed that suited its rival most. That is a cruder test than a standard error
        // and it cannot be gamed by a run that happened to land well.
        Assert.True(scores["Freshest(3)"].Min() > scores["Resolved(3)"].Max(),
            $"following the freshest key reads {scores["Freshest(3)"].Min():F3} at its worst "
            + $"against {scores["Resolved(3)"].Max():F3} at the all-keys fold's best, so which "
            + "key the fold follows is inside the seed spread and the ceiling grid's account "
            + "of what limits this learner is wrong");

        Assert.True(scores["Freshest(3)"].Min() > scores[nameof(Joining.Chained)].Max(),
            $"the store reads {scores["Freshest(3)"].Min():F3} at its worst against "
            + $"{scores[nameof(Joining.Chained)].Max():F3} for the best backward-reading arm at "
            + "its best, so maintaining a store forwards buys no score over a lookup and this "
            + "whole mechanism is `Chained` by a longer road");

        // And what the word order bought, which is the largest single move any front-end change
        // has made on this world. The same five arms, the same seeds and the same rounds read
        // 0.348 at the best before the world spoke `Coded`; the store with the key rule now
        // reads over half, which is three times the marginal of 0.193.
        //
        // A level rather than a comparison, because what it records is a cap being converted.
        // The bar sits well under the 0.548 it came in at: what it protects against is a
        // silent return to the era where nothing here cleared twice the marginal.
        Assert.True(scores["Freshest(3)"].Min() > 0.45,
            $"the store with the key rule and the order reads {scores["Freshest(3)"].Min():F3} at "
            + "its worst, back inside the range this world read before it was told which word "
            + "came first -- so the order has stopped being converted and the conflation "
            + "ceiling is no longer describing this learner");

        // And the arm the ceiling picked out, which is the prediction worth recording because it
        // was made before the run. Order takes `Resolved(1)`'s conflation from 0.748 to 0.099 --
        // the largest fall of any arm -- and it is the arm whose score moved most, out of
        // `Chained`'s own spread and clear of it. Before the order it read 0.166 at its worst
        // against `Chained`'s 0.215 at its best, so this comparison inverted.
        Assert.True(scores["Resolved(1)"].Min() > scores[nameof(Joining.Chained)].Max(),
            $"folding through every key reads {scores["Resolved(1)"].Min():F3} at its worst "
            + $"against {scores[nameof(Joining.Chained)].Max():F3} for the lookup at its best, so "
            + "the arm whose conflation the order cut furthest is not the arm whose score moved "
            + "furthest and the ceiling column predicts nothing about a learner");

        // And a cap is not a prediction, which is the correction the same grid makes to itself.
        // `Resolved(3)` had the largest fall of all -- 0.941 conflated to 0.027 -- and gained
        // four hundredths, because its moment arrives with four rooms of company and separating
        // two moments is not choosing between what they hold. What an arm CONVERTS of its cap is
        // still where the learner's part of this shows.
        Assert.True(scores["Resolved(3)"].Max() < scores["Resolved(1)"].Min(),
            $"three hops read {scores["Resolved(3)"].Max():F3} at their best against "
            + $"{scores["Resolved(1)"].Min():F3} for one at its worst, so a deeper fold is no "
            + "longer paying for the company it brings and the account of why the order helped "
            + "it least is wrong");

        // And the bagged arm comes back with an empty population, which is a finding about
        // the front end rather than a score. Six rooms, four things and a few function
        // words means that after 120 statements essentially every word of the vocabulary is
        // present in every moment -- so the bag is the SAME MOMENT every round, nothing is
        // ever surprising, and genesis never fires at all. A constant moment mints nothing.
        //
        // It is the opposite end of the fault the English arms hit. There a growing
        // vocabulary outran the cap; here a tiny one makes the moment a constant. Both are
        // the front end deciding what the learner can possibly see, and both look like the
        // learner failing.

        // And every arm is read against its own ceiling rather than against the others'
        // scores, which is what makes the facts in this file one instrument. The cap is the
        // conflated column and never `present`: a commitment expects an outcome code that no
        // word of this world supplies, so a moment missing the answering room word is still
        // answerable and only a moment that is another moment's twin is not. An arm reaching
        // further and scoring lower is failing to choose, which is the selection problem.
    }

    /// <summary>
    /// The same learner where four people walk the house — <b>the rank inversion the ceiling
    /// predicts, run as its own measurement.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Pre-registered before the run, in one line</b>: at four people
    /// <c>Freshest(1)</c> should beat <c>Freshest(3)</c>, which is the opposite of the order
    /// they come in at one person. What says so is
    /// <see cref="What_a_second_person_does_to_the_key_the_fold_must_follow"/> — one hop
    /// through the freshest key pins 0.180 there where three hops pin 0.128, and three hops
    /// arrive with 3.39 people against 1.28.
    /// </para>
    /// <para>
    /// <b>And what would refute the ceiling is the inversion failing to appear.</b> If depth
    /// still wins where the company it drags along has trebled, then what a fold pins says
    /// nothing about what a learner scores, and the columns that chose the arms in this file
    /// are decoration.
    /// </para>
    /// <para>
    /// <b>Its own runner rather than a cell of the one above</b>, because the list in
    /// <c>sweeps.yml</c> is one entry a runner and a class holding two independent grids is two
    /// runners' work sitting on one.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_learner_reads_when_four_people_walk_the_house()
    {
        var arms = new (string Name, Joined Joined)[]
        {
            ("Resolved(1)", new Joined(Joining.Resolved, resolution: 1)),
            ("Freshest(1)", new Joined(Joining.Resolved, resolution: 1, freshest: true)),
            ("Freshest(3)", new Joined(Joining.Resolved, resolution: 3, freshest: true)),

            // And the two the conflation column made interesting at this cell rather than at
            // one person. `Distinguished` conflates 0.004 of moments here against 0.766 at one
            // person, so its cap is wide open on a world where it pins almost nothing -- which
            // is the sharpest test there is of whether a cap says anything about a score.
            (nameof(Joining.Distinguished), new Joined(Joining.Distinguished)),
            (nameof(Joining.Chained), new Joined(Joining.Chained)),
        };

        var scores = Scored(arms, people: 4, Examining.Where);

        // The bar that holds whichever way the inversion goes, and it is the one worth having
        // first: a world with four people is still a world this learner reads. The marginal is
        // about 0.19 at every cell of the people axis, so an arm under twice it has stopped
        // tracking and the inversion would be a comparison between two failures.
        Assert.True(scores["Freshest(1)"].Min() > 0.38,
            $"one hop through the freshest key reads {scores["Freshest(1)"].Min():F3} at its worst "
            + "with four people walking, under twice the marginal -- so a second person has "
            + "taken the world out of this learner's reach and the ceiling that says it is "
            + "reachable is measuring something a run cannot convert");

        // The pre-registered half, and it held: 0.439 at its worst against 0.401 at three hops'
        // best. Depth is what a second person makes expensive, which is what the company column
        // said before the run -- three hops arrive with 3.39 people in the moment against 1.28.
        Assert.True(scores["Freshest(1)"].Min() > scores["Freshest(3)"].Max(),
            $"one hop reads {scores["Freshest(1)"].Min():F3} at its worst against "
            + $"{scores["Freshest(3)"].Max():F3} for three at their best, so the inversion the "
            + "people grid predicted is not there and what a fold pins says nothing about depth");

        // And the half that was refuted, which is the more useful one. Folding through every key
        // leads here, 0.484 at its worst against the freshest key's 0.471 at its best -- so the
        // key rule's whole advantage does not survive a second person, and it was the only thing
        // separating `Freshest` from `Resolved` at one person.
        //
        // The prediction leaned on `pinned`, one commit after this same file recorded that a
        // read-it-off column does not cap a learner. `Resolved(1)` pins 0.029 against 0.180 and
        // wins, because it reaches further: two rooms in the moment against one and a quarter,
        // and a commitment that recognises a moment does not need the answer word in it.
        Assert.True(scores["Resolved(1)"].Min() > scores["Freshest(1)"].Max(),
            $"folding through every key reads {scores["Resolved(1)"].Min():F3} at its worst "
            + $"against {scores["Freshest(1)"].Max():F3} for the freshest key at its best, so the "
            + "key rule's advantage does survive a second person after all and fork 95's answer "
            + "is what it looked like at one person");

        // And the reading that refutes the cap as a ranking outright, which is why these two arms
        // were added. `Distinguished` and `Resolved(1)` conflate under a hundredth of moments at
        // four people -- the same cap, to within a seed -- and they read 0.167 and 0.497. One is
        // the marginal and the other is two and a half times it.
        //
        // So a LOW conflation says nothing, and this is the direction the column cannot be read
        // in. High conflation is a proof of a ceiling; low conflation is not a floor.
        //
        // Why it is low for both is not the answer either, and that was measured rather than
        // assumed after the first explanation offered here turned out to be a story. Both arms
        // have essentially every moment DISTINCT at four people, over 0.99 of questions, so
        // neither is separating anything -- the moments simply never repeat. Uniqueness does not
        // rank them any better than conflation does.
        //
        // What is left, and what nothing here measures, is whether a moment's SUB-SCOPES recur.
        // A commitment fires on a subset, so generalisation never needed whole moments to
        // repeat; `Distinguished` holds 133 rules where the store holds 934 on moments that are
        // equally unique. That column is the next instrument this file is missing.
        Assert.True(scores["Resolved(1)"].Min() > scores[nameof(Joining.Distinguished)].Max() * 2,
            $"the background rule reads {scores[nameof(Joining.Distinguished)].Max():F3} at its "
            + $"best against {scores["Resolved(1)"].Min():F3} for the store at its worst, so two "
            + "arms whose conflation is equally near nought are no longer far apart -- and the "
            + "column can be read as a floor after all");

        // Both backward-reading arms sit at the marginal here, which is a fact about a second
        // person rather than about either rule. A lookup over the transcript answers *which
        // statement mentioned this* and the answer is in a statement about somebody else.
        foreach (var name in new[] { nameof(Joining.Distinguished), nameof(Joining.Chained) })
            Assert.True(scores[name].Max() < 0.25,
                $"{name} reads {scores[name].Max():F3} at its best with four people, off the "
                + "marginal of about 0.19 -- so reading the transcript backwards is tracking "
                + "something after all and the forward store is not what pays here");
    }

    /// <summary>
    /// <b>What the effect question is worth</b> before anything learns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Taken before any learner runs, which is this repo's own rule.</b> A front-end
    /// ceiling costs milliseconds against a runner's hour, and a grid cannot tell a rule that
    /// answered from the question's own words from a learner that used the transcript.
    /// </para>
    /// <para>
    /// <b>Three columns, and they come back as TWO.</b> The marginal is always saying the
    /// commoner answer. The verb ceiling is the best constant per verb, which is every
    /// conjunctive rule over the question alone — and it lands exactly on the marginal,
    /// because <i>went</i> is the commonest verb and still moves nothing four times in five.
    /// <i>Took</i> and <i>dropped</i> are free and buy no accuracy, since the constant they
    /// license is the constant everything else already licenses.
    /// </para>
    /// <para>
    /// <b>Which corrects what was written here before the reading</b>, and makes the world
    /// better rather than worse. The prediction was a verb ceiling ABOVE the marginal
    /// with the binding cases above that, so a learner would have had two bars and the lower
    /// one reachable by a one-code rule. There is no cheap rule in between: every point over
    /// the marginal is a walker carrying something, which no word of the question names.
    /// </para>
    /// <para>
    /// <b>The verb is read by POSITION and never by an answer key.</b> A question is
    /// <c>who verb ...</c>, so grouping on its second code needs no vocabulary — and the
    /// length separates <i>went</i> from the other two, five words against four, which is a
    /// fact about the transcript rather than a hint about the answer.
    /// </para>
    /// </remarks>
    /// <summary>One watched run of the house, into a brain built on the dials given.</summary>
    /// <param name="dials">What the brain is set to. The arm under test lives here.</param>
    /// <param name="seed">What draws the house and the brain.</param>
    /// <remarks>
    /// <b>Watched rather than acted in</b>, which is what the naming questions want. What a
    /// name is worth is about the population, and a chooser puts a second thing in motion.
    /// Shared by the two readings above so neither can drift into measuring a different run —
    /// `DuplicationTests` refused the second copy, correctly.
    /// </remarks>
    private static (Tally Tally, Brain Brain) Watched(CommittingSettings dials, int seed)
    {
        var brain = new Brain(dials, seed);

        var tally = new Bench(
            new Watching<Coded>(
                new Roaming(World(120, people: 4), seed),
                new Joined(Joining.Resolved, resolution: 3, freshest: true),
                acting: Chooses.From(_ => null)),
            brain)
            .Run(10_000, sweep: 1000, target: 0.9, window: 2000);

        return (tally, brain);
    }

    /// <summary>
    /// What rung five's names DO on the spine world, before anything asks what they buy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The ceiling taken first, which is cheaper than an arm.</b> Turning the rung off
    /// wants a dial, and a dial wants two worlds and a grid; whether a name is ever USED
    /// wants a run and no new machinery. A name nothing fires on is decorative whatever a
    /// grid would say, and then the arm is not worth building.
    /// </para>
    /// <para>
    /// <b>Every rung-five reading here has been on an isolating world</b> — the
    /// multiplexer, whose outcome columns are at ceiling whatever the rung does, and
    /// <see cref="Worlds.Latent"/>, where the gate speaks under once in twenty asks. This is
    /// the first taken where the score has room to move.
    /// </para>
    /// <para>
    /// <b>A name is a code of its own modality</b>, so a scope standing on one is found by
    /// looking rather than by remembering what was minted. That matters because a rewritten
    /// scope can be culled or subsumed afterwards, and a count of MINTS says nothing about
    /// what is still resident at the end.
    /// </para>
    /// <para>
    /// <b>And the whole of the reading is the matched table</b>, which is why the raw one is
    /// printed beside it rather than instead of it. A scope standing on a name is narrower
    /// than its code count says, the name standing for a pair — and narrow rules are more
    /// accurate almost by construction, which is the memorising this design is otherwise
    /// careful about. So a raw named-against-plain gap is what specificity alone produces,
    /// and matching on the UNFOLDED length is what holds specificity still.
    /// </para>
    /// <para>
    /// <b>What would say the rung is decorative here</b>: named scopes resident and never
    /// firing. Then no vote has seen one, the mint changed the population's spelling and
    /// nothing else, and an on-off arm would be measuring nothing.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_rung_fives_names_do_on_the_spine_world()
    {
        // Over seeds, because one seed is not a comparison and this repo's traps list says it
        // will happily invert. The first take of this ran on seed one and read named against
        // plain at 0.729 to 0.244, which is a length distribution rather than a finding.
        const int Seeds = 5;

        var raw = new List<(int Fired, double Named, double Plain)>();

        var matched =
            new Dictionary<int, (List<double> Named, List<double> Plain, int On, int Of)>();

        var minted = new List<int>();
        var stacked = new List<int>();

        for (var seed = 1; seed <= Seeds; seed++)
        {
            var (tally, brain) = Watched(
                new CommittingSettings { Capacity = 20_000 }, seed);

            var held = brain.Held.All.ToList();
            var names = brain.Held.Names;

            var standing = held
                .Where(one => one.Scope.Any(Naming.Names))
                .ToList();

            var bare = held.Except(standing).ToList();

            minted.Add(tally.Named);
            stacked.Add(tally.Stacked);
            raw.Add((standing.Count(one => one.Seen > 0), Rate(standing), Rate(bare)));

            output.WriteLine(
                $"seed {seed} | {tally.Named,2} names over {tally.Eligible,4} eligible, "
                + $"{tally.Stackable,3} stackable, {tally.Stacked} stacked | "
                + $"{held.Count,5} resident, {standing.Count,4} on a name | "
                + $"raw {Rate(standing):F3} named to {Rate(bare):F3} plain | "
                + $"exam {tally.Unseen?.Accuracy ?? 0.0:F3}");

            foreach (var group in held.GroupBy(one => names.Unfold(one.Scope).Length))
            {
                var named = group
                    .Where(one => one.Scope.Any(Naming.Names))
                    .ToList();

                var plain = group.Except(named).ToList();

                if (!matched.TryGetValue(group.Key, out var cell))
                    matched[group.Key] = cell = ([], [], 0, 0);

                if (named.Count > 0) cell.Named.Add(Rate(named));
                if (plain.Count > 0) cell.Plain.Add(Rate(plain));

                matched[group.Key] = cell with
                {
                    On = cell.On + named.Count,
                    Of = cell.Of + plain.Count,
                };
            }
        }

        output.WriteLine("");
        output.WriteLine(
            $"raw, {Seeds} seeds | named {Sweep.Spread([.. raw.Select(one => one.Named)])} "
            + $"| plain {Sweep.Spread([.. raw.Select(one => one.Plain)])}");

        output.WriteLine("");
        output.WriteLine(
            "unfolded | named scopes         accuracy | plain scopes         accuracy");

        foreach (var (length, cell) in matched.OrderBy(one => one.Key))
            output.WriteLine(
                $"{length,8} | {cell.On,6} "
                + $"{(cell.Named.Count == 0 ? "-" : Sweep.Spread(cell.Named)),22} "
                + $"| {cell.Of,6} "
                + $"{(cell.Plain.Count == 0 ? "-" : Sweep.Spread(cell.Plain)),22}");

        // The instrument. A run that minted nothing makes every column above a reading about
        // the sweep calendar rather than about what a name does.
        Assert.True(minted.Sum() > 0,
            "rung five minted nothing on any seed, so this world is not offering it material "
            + "and the reading is about the world");

        // And the ceiling this exists to take. A resident scope that never fires has never
        // reached a vote, so a population whose named scopes are all like that has spent the
        // rung on spelling.
        Assert.True(raw.Sum(one => one.Fired) > 0,
            "scopes stand on a minted name and not one has ever fired, so no vote has seen a "
            + "name and rung five is decorative on the spine. An on-off arm would be "
            + "measuring nothing");

        // And the recursion, which is fork 112 asked on a third bench and answered the other
        // way. This bar was written expecting nought -- `Motif` leaves a named scope too
        // short to carry a name and `Latent`'s deep scopes never reach the floor -- and it
        // fired: one name stands on a name on two seeds of five. So the spine is not blocked
        // the way either isolating world is, it is thin, and thin has a different cause from
        // blocked.
        Assert.True(stacked.Sum() > 0,
            "no name stands on a name on any seed, so the recursion has stopped happening on "
            + "the spine. It ran at one on two seeds of five when this was written, and a "
            + "nought here is the third blockage fork 112 expected rather than the thinness "
            + "that was found");

        // WHAT THE MATCHED TABLE SAID, five seeds, and it is the whole of the reading:
        //
        //   unfolded   named  accuracy         plain  accuracy         apart
        //   1              0  -                  316  0.168 +/-0.000       -
        //   2             14  0.662 +/-0.046    1465  0.574 +/-0.007    +1.9
        //   3            202  0.674 +/-0.025    2202  0.742 +/-0.022    -2.0
        //   4            232  0.791 +/-0.026     358  0.729 +/-0.026    +1.7
        //   5             23  0.928 +/-0.028      82  0.531 +/-0.109    +3.5
        //   6              0  -                   10  0.215 +/-0.134       -
        //
        // The raw gap is 0.701 to 0.241 and none of it survives. Named scopes sit at three
        // and four; the plain group carries 316 one-code scopes at 0.168 and 1465 two-code
        // at 0.574, so the three-fold is the length distribution and nothing else.
        //
        // And matched, naming is a wash where the evidence is. It is two standard errors
        // WORSE at three, where most of both groups live, and 1.7 better at four. The 3.5 at
        // five is 23 scopes against 82 with a spread of 0.109 on the comparison, which is the
        // cell to re-read rather than the cell to believe.
        //
        // So a name buys reach rather than accuracy: it is how a scope gets to length five at
        // all, and at a length a plain scope also reaches it is level or behind. Whether
        // reach is worth anything is what an on-off arm would answer, and this says the arm
        // is worth building -- which is what it was for.
        //
        // No bar on the table. A threshold written before the first reading of it would be
        // the answer put in front of the question, and the two bars above are on the
        // instrument rather than on the result.
        return;

        static double Rate(List<Commitment> of)
        {
            var hits = of.Sum(one => one.Hits);
            var misses = of.Sum(one => one.Misses);

            return hits + misses == 0 ? 0.0 : hits / (double)(hits + misses);
        }
    }

    /// <summary>
    /// What the machine loses when the one operator that broadens is switched off.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The control rung five has never had</b>, on the two worlds that mint. Every other
    /// mechanism here was measured against its own absence before it shipped; this one has
    /// been unconditional since it was built, so nothing anywhere says what the machine would
    /// do without it.
    /// </para>
    /// <para>
    /// <b>The spine carries the comparison and the multiplexer is beside it.</b> `Roaming`
    /// scores well under its ceiling, so a loss has room to show; eleven bits mints three
    /// times as many names and its outcome columns are AT CEILING, so it can only speak on
    /// the structural ones. Two worlds, and it is said which is which rather than both being
    /// read as votes.
    /// </para>
    /// <para>
    /// <b>What kills the rung</b>, written before the run: `Never` matching `Named` on both
    /// worlds. Then naming buys nothing measurable, the machine is specialise-only in effect,
    /// and the doc's own line applies — <i>a specialise-only machine is arbitrarily accurate
    /// and conceptless</i>. That would not be a dial dying; it would be the hierarchy claim
    /// wanting a different mechanism.
    /// </para>
    /// <para>
    /// <b>And what would make the reading unreadable</b> is the arms differing in anything
    /// but naming. The sweep still subsumes and still culls under `Never`, so the calendar is
    /// held still; the counts below are printed so a run where they came apart says so.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_switching_the_broadening_operator_off_costs()
    {
        const int Seeds = 5;

        var arms = new[] { Broadening.Named, Broadening.Unrooted, Broadening.Never };

        var spine = arms.ToDictionary(
            arm => arm,
            arm => Enumerable.Range(1, Seeds).Select(seed => Spine(arm, seed)).ToList());

        output.WriteLine($"=== Roaming, {Seeds} seeds, 10,000 rounds ===");
        output.WriteLine(
            "arm     |           exam |            own |       resident |        repairs | names");

        foreach (var arm in arms)
        {
            var read = spine[arm];

            output.WriteLine(
                $"{arm,-8}| {Sweep.Spread([.. read.Select(one => one.Exam)]),14} "
                + $"| {Sweep.Spread([.. read.Select(one => one.Own)]),14} "
                + $"| {Sweep.Spread([.. read.Select(one => (double)one.Resident)], "F1"),14} "
                + $"| {Sweep.Spread([.. read.Select(one => (double)one.Repaired)], "F1"),14} "
                + $"| {read.Average(one => one.Named),5:F1}");
        }

        output.WriteLine("");
        output.WriteLine($"=== eleven bits, {Seeds} seeds, 20,000 rounds ===");
        output.WriteLine(
            "arm     |         recent |          sound |        unsound |          found | names");

        var bits = arms.ToDictionary(
            arm => arm,
            arm => Enumerable.Range(1, Seeds).Select(seed => Bits(arm, seed)).ToList());

        foreach (var arm in arms)
        {
            var read = bits[arm];

            output.WriteLine(
                $"{arm,-8}| {Sweep.Spread([.. read.Select(one => one.Learnt.Recent)]),14} "
                + $"| {Sweep.Spread([.. read.Select(one => (double)one.Learnt.Sound)], "F1"),14} "
                + $"| {Sweep.Spread([.. read.Select(one => (double)one.Learnt.Unsound)], "F1"),14} "
                + $"| {Sweep.Spread([.. read.Select(one => (double)one.Learnt.Found)], "F1"),14} "
                + $"| {read.Average(one => one.Learnt.Named),5:F1}");
        }

        // The arm did what it says. `Never` naming anything is the whole of the control, and
        // a run where it minted would be measuring the sweep calendar instead.
        Assert.Equal(0, spine[Broadening.Never].Sum(one => one.Named));
        Assert.Equal(0, bits[Broadening.Never].Sum(one => one.Learnt.Named));

        // And `Unrooted` still names, which is what makes it the middle arm rather than a
        // second control. It differs from `Named` in what genesis may reach for and in
        // nothing else, so a run where it stopped minting would be `Never` by another road.
        Assert.True(spine[Broadening.Unrooted].Sum(one => one.Named) > 0,
            "the unrooted arm minted nothing, so it is a second copy of the control rather "
            + "than the middle arm and the split it exists for has not happened");

        // And the shipped arm still does, which is the other half. A control against an arm
        // that had stopped firing would read level and say nothing.
        Assert.True(spine[Broadening.Named].Sum(one => one.Named) > 0,
            "the shipped arm minted nothing on the spine, so this grid is comparing two "
            + "silences and the reading is about the world");

        // WHAT THE CONTROL SAID, five seeds, and the pre-registered kill did not merely fire
        // -- the absence WINS on one of the two worlds.
        //
        //   Roaming            exam             own         resident         repairs  names
        //   Named    0.373 +/-0.007  0.364 +/-0.005   985.2 +/-42.4  1398.0 +/-57.6    8.2
        //   Never    0.372 +/-0.007  0.363 +/-0.005   953.6 +/-36.5  1365.6 +/-50.3    0.0
        //
        //   11 bits          recent           sound          unsound           found  names
        //   Named    0.990 +/-0.002  266.8 +/-15.1   285.8 +/-20.7   15.4 +/-0.2      18.6
        //   Never    0.997 +/-0.001   58.2 +/-3.1    119.2 +/-8.8    15.4 +/-0.2       0.0
        //
        // On the spine every column is inside noise. Switching off the one operator that
        // broadens costs nothing on the world where the score has the most room to move.
        //
        // On eleven bits it is worse than level. Naming holds 4.6 times the sound rules and
        // 2.4 times the unsound ones, FINDS EXACTLY THE SAME 15.4 truths, and scores 3.1
        // standard errors BEHIND its own absence. `Found` against `Sound` is what separates
        // those: the extra sound rules are re-spellings of truths the machine already had,
        // not truths it would otherwise have missed.
        //
        // And the mechanism for the inflation is not the rewrite, which is population-neutral
        // -- one scope out, one shorter scope in. It is that a minted name is a CODE in the
        // moment, so genesis may root on it and repair may add it, and the alphabet growing
        // grows everything downstream of it. Rung five pays for a wider vocabulary in
        // population and is repaid in nothing.
        //
        // So the reading is about the SHAPE of the rung rather than about its gate, its
        // ranking or its cadence -- three sessions have now moved those and none of them
        // moved this. The plan says the same thing from the other end and said it first: a
        // name over ALTERNATIVES is what a category wants, rung five names what CO-FIRES, and
        // alternatives never co-occur. This is that argument with a number under it.
        //
        // Nothing here says delete the rung. The refutation table's own row is that a ladder
        // which only discriminates is conceptless, and the answer to that is rung five OR the
        // hierarchy claim goes -- so what this refutes is the BUILD, and fork 129 is the
        // unrun idea about the shape.

        // And the third arm splits what the control bundled. Two things happen when a name
        // is minted: the scopes holding its members are rewritten, which is
        // population-neutral, and the name enters the moment as a code genesis may root on.
        // `Never` removed both at once, so the reading above cannot say which of them the
        // machine was paying for. `Unrooted` keeps the rewrite and refuses the root.
        //
        // It matters beyond this rung. Fork 129 changes WHICH codes are named and keeps the
        // delivery, so a minted category would enter the moment and be rooted on exactly as
        // a minted pair is. If the delivery is the fault then 129 inherits it whatever its
        // statistic does, and that is much cheaper to learn here than there.
        //
        // What would say the delivery is innocent: `Unrooted` landing on `Named`'s numbers.
        // Then the inflation was not the root and the whole of the cost is somewhere the
        // rewrite put it.

        // And the two bars the reading now carries, so it goes red the day the rung starts
        // paying. Naming finding MORE of the world's true rules than its absence would put
        // its yield somewhere other than in re-spellings, which is the whole of the finding.
        Assert.True(
            bits[Broadening.Named].Average(one => (double)one.Learnt.Found)
                <= bits[Broadening.Never].Average(one => (double)one.Learnt.Found),
            "naming now finds more of the world's true rules than its absence does, so the "
            + "reading that put rung five's entire yield in re-spellings is stale");

        // And the spine's exam being level rather than merely close, read against the
        // control's own spread rather than a number chosen here. A lead inside one standard
        // error is what `level` means and what this run found.
        var lead = spine[Broadening.Named].Average(one => one.Exam)
            - spine[Broadening.Never].Average(one => one.Exam);

        var spread = Sweep.Error([.. spine[Broadening.Never].Select(one => one.Exam)]);

        output.WriteLine("");
        output.WriteLine($"spine lead {lead:F4} against a control spread of {spread:F4}");

        Assert.True(lead <= spread,
            $"naming leads its absence by {lead:F4} on the spine exam against a control "
            + $"spread of {spread:F4}, so the rung has started paying on the world with the "
            + "most room and this reading is stale");

        return;

        static (double Exam, double Own, int Resident, long Repaired, int Named) Spine(
            Broadening arm, int seed)
        {
            var (tally, _) = Watched(
                new CommittingSettings { Capacity = 20_000, Broadening = arm }, seed);

            return (
                tally.Unseen?.Accuracy ?? 0.0,
                tally.Recent,
                tally.Resident,
                tally.Repaired,
                tally.Named);
        }

        static (Learned Learnt, int Seed) Bits(Broadening arm, int seed)
        {
            var brain = new Brain(new CommittingSettings { Broadening = arm }, seed);

            return (
                new MultiplexerRun(new MultiplexerSettings { Address = 3 }, brain, seed)
                    .Run(20_000),
                seed);
        }
    }

    // Positive pointwise mutual information over the company table, which is the known
    // correction for the reading beside it. A cosine over RAW counts is dominated by how
    // often a code appears, so a common one looks alike to everything and a null can be
    // a fact about the measure rather than about the population. This divides each cell
    // by what independence would have made it, which is the same shape the naming gate's
    // z uses and which the plan already names for fork 131.
    static Dictionary<Code, Dictionary<Code, double>> Pointwise(
        Dictionary<Code, Dictionary<Code, double>> raw)
    {
        var down = new Dictionary<Code, double>();
        var whole = 0.0;

        foreach (var (_, over) in raw)
            foreach (var (mate, weight) in over)
            {
                down.TryGetValue(mate, out var so_far);
                down[mate] = so_far + weight;
                whole += weight;
            }

        var found = new Dictionary<Code, Dictionary<Code, double>>();

        foreach (var (code, over) in raw)
        {
            var along = over.Values.Sum();

            if (along <= 0.0) continue;

            var lifted = new Dictionary<Code, double>();

            foreach (var (mate, weight) in over)
            {
                var expected = along * down[mate] / whole;

                if (expected <= 0.0) continue;

                var value = Math.Log(weight / expected);

                // POSITIVE only, which is what the P in PPMI is. A cell rarer than
                // independence says the two avoid each other, and a vector of those is a
                // different claim from a vector of what they share.
                if (value > 0.0) lifted[mate] = value;
            }

            if (lifted.Count > 0) found[code] = lifted;
        }

        return found;
    }

    static double Cosine(Dictionary<Code, double> left, Dictionary<Code, double> right)
    {
        var over = 0.0;

        foreach (var (code, weight) in left)
            if (right.TryGetValue(code, out var also)) over += weight * also;

        var one = Math.Sqrt(left.Values.Sum(value => value * value));
        var two = Math.Sqrt(right.Values.Sum(value => value * value));

        return one == 0.0 || two == 0.0 ? 0.0 : over / (one * two);
    }

    /// <summary>
    /// One seed's within-set and across-set likeness, appended to what is being collected.
    /// </summary>
    /// <param name="profiles">Each code's profile, however it was read.</param>
    /// <param name="sets">The answer key: which codes are alternatives to which.</param>
    /// <param name="into">Within-set means, by set name.</param>
    /// <param name="other">Across-set means.</param>
    /// <remarks>
    /// <b>Shared by the population reading and the moment one</b>, which is what makes them
    /// comparable: the two differ in what a profile IS and in nothing else.
    /// `DuplicationTests` refused the second copy, correctly, and the arithmetic being in one
    /// place is worth more than the refusal — a within-set mean computed two ways is two
    /// definitions for one column.
    /// </remarks>
    private static void Compare(
        Dictionary<Code, Dictionary<Code, double>> profiles,
        (string What, IReadOnlyList<Code> Codes)[] sets,
        Dictionary<string, List<double>> into,
        List<double> other)
    {
        foreach (var (what, codes) in sets)
        {
            var have = codes.Where(profiles.ContainsKey).ToList();

            var pairs = have
                .SelectMany((left, at) => have.Skip(at + 1)
                    .Select(right => Cosine(profiles[left], profiles[right])))
                .ToList();

            if (pairs.Count > 0) into[what].Add(pairs.Average());
        }

        var mixed = new List<double>();

        for (var left = 0; left < sets.Length; left++)
            for (var right = left + 1; right < sets.Length; right++)
                foreach (var one in sets[left].Codes.Where(profiles.ContainsKey))
                    foreach (var two in sets[right].Codes.Where(profiles.ContainsKey))
                        mixed.Add(Cosine(profiles[one], profiles[two]));

        if (mixed.Count > 0) other.Add(mixed.Average());
    }

    /// <summary>
    /// Whether the population separates ALTERNATIVES at all — <b>fork 129's ceiling, taken
    /// before any of its mechanism is built.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The idea is that two codes are alike where the commitments naming them EXPECT the same
    /// things. It never asks whether they co-occurred, which is the one thing alternatives
    /// never do — and which is why rung five, naming what CO-FIRES, is the wrong shape for a
    /// category.
    /// </para>
    /// <para>
    /// <b>A ceiling rather than an arm</b>, which is the cheapest thing that can kill the
    /// fork. If a learnt population does not already separate room words from thing words by
    /// what their commitments expect, then the statistic has no signal to find and no gate
    /// built on it can help. Nothing here mints anything or changes what any run does.
    /// </para>
    /// <para>
    /// <b>The world supplies the answer key and no learner sees it.</b>
    /// <see cref="Worlds.Roaming.Named"/>, <see cref="Worlds.Roaming.Called"/> and
    /// <see cref="Worlds.Roaming.Walking"/> are the vocabulary the world emitted, which is a
    /// fact about the transcript rather than a hint about an answer — the standing every
    /// instrument here has. The three sets are alternatives BY CONSTRUCTION: a person is in
    /// one room, so the room words substitute for one another and never co-occur as an
    /// answer.
    /// </para>
    /// <para>
    /// <b>What kills the fork</b>: within-set likeness landing on across-set likeness. Then
    /// what a commitment expects carries nothing about which codes are alternatives, and
    /// reading likeness off the population is a different way of finding the same nothing the
    /// moment gave.
    /// </para>
    /// <para>
    /// <b>And it is weighted two ways</b> because the profile is a choice and not a given.
    /// By residency, every commitment naming a code counts once; by hits, a commitment counts
    /// for what actually followed it. The second is what <i>expects</i> should mean, and the
    /// first is what a population that has not settled yet can offer. A fork whose signal
    /// only appears under one of them is a fork about the weighting.
    /// </para>
    /// </remarks>
    /// <summary>Which way round a code's profile is read.</summary>
    private enum Reading
    {
        /// <summary>The outcomes the commitments naming a code expect. Fork 129 as written.</summary>
        Expects,

        /// <summary>The scope codes of the commitments that expect it. Its dual.</summary>
        ExpectedBy,

        /// <summary>The other codes in the scopes it appears in. The distributional one.</summary>
        Beside,
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_the_population_separates_alternatives_by_what_they_expect()
    {
        const int Seeds = 5;

        var within = new Dictionary<string, List<double>>();
        var across = new List<double>();
        var byHits = new Dictionary<string, List<double>>();
        var acrossHits = new List<double>();
        var byContext = new Dictionary<string, List<double>>();
        var acrossContext = new List<double>();
        var byMates = new Dictionary<string, List<double>>();
        var acrossMates = new List<double>();
        var byPointwise = new Dictionary<string, List<double>>();
        var acrossPointwise = new List<double>();

        for (var seed = 1; seed <= Seeds; seed++)
        {
            var world = new Roaming(World(120, people: 4), seed);

            var (_, brain) = Watched(new CommittingSettings { Capacity = 20_000 }, seed);

            var held = brain.Held.All.ToList();

            var sets = new (string What, IReadOnlyList<Code> Codes)[]
            {
                ("rooms", world.Named),
                ("things", world.Called),
                ("people", world.Walking),
            };

            foreach (var (what, _) in sets)
            {
                within.TryAdd(what, []);
                byHits.TryAdd(what, []);
                byContext.TryAdd(what, []);
                byMates.TryAdd(what, []);
                byPointwise.TryAdd(what, []);
            }

            // One profile a code, and the two directions are the whole question. FORWARD is
            // the fork as written: over the outcomes the commitments naming a code expect.
            // BACKWARD is its dual: over the scope codes of the commitments that expect it.
            //
            // A code no commitment reaches has no profile and is left out rather than
            // counted as a vector of noughts, which would read as alike to every other
            // empty one.
            Dictionary<Code, Dictionary<Code, double>> Profiles(
                Func<Commitment, double> weigh, Reading reading)
            {
                var found = new Dictionary<Code, Dictionary<Code, double>>();

                foreach (var one in held)
                {
                    var weight = weigh(one);

                    if (weight <= 0.0) continue;

                    // The third direction, and it is the distributional one. A code's profile
                    // is the OTHER codes in the scopes it appears in -- its company rather
                    // than its consequence. It is what the linguistics analogy actually says,
                    // and it does not ask whether two codes co-occurred: two words with the
                    // same neighbours are alike however rarely they meet.
                    if (reading == Reading.Beside)
                    {
                        foreach (var code in one.Scope)
                        {
                            if (!found.TryGetValue(code, out var beside))
                                found[code] = beside = [];

                            foreach (var mate in one.Scope)
                            {
                                if (mate == code) continue;

                                beside.TryGetValue(mate, out var held_so_far);
                                beside[mate] = held_so_far + weight;
                            }
                        }

                        continue;
                    }

                    if (reading == Reading.Expects)
                    {
                        foreach (var code in one.Scope)
                        {
                            if (!found.TryGetValue(code, out var over))
                                found[code] = over = [];

                            over.TryGetValue(one.Expects, out var so_far);
                            over[one.Expects] = so_far + weight;
                        }

                        continue;
                    }

                    if (!found.TryGetValue(one.Expects, out var by))
                        found[one.Expects] = by = [];

                    foreach (var code in one.Scope)
                    {
                        by.TryGetValue(code, out var so_far);
                        by[code] = so_far + weight;
                    }
                }

                return found;
            }

            foreach (var (weigh, reading, pointwise, into, other) in
                new (Func<Commitment, double> Weigh, Reading Reading, bool Pointwise,
                    Dictionary<string, List<double>> Into, List<double> Other)[]
                {
                    (_ => 1.0, Reading.Expects, false, within, across),
                    (one => one.Hits, Reading.Expects, false, byHits, acrossHits),
                    (one => one.Hits, Reading.ExpectedBy, false, byContext, acrossContext),
                    (_ => 1.0, Reading.Beside, false, byMates, acrossMates),
                    (_ => 1.0, Reading.Beside, true, byPointwise, acrossPointwise),
                })
            {
                var profiles = Profiles(weigh, reading);

                if (pointwise) profiles = Pointwise(profiles);

                Compare(profiles, sets, into, other);
            }
        }

        output.WriteLine($"cosine over what the commitments naming a code expect, {Seeds} seeds");
        output.WriteLine("direction   | rooms          | things         | people         | across");

        foreach (var (what, into, other) in
            new (string What, Dictionary<string, List<double>> Into, List<double> Other)[]
            {
                ("expects", within, across),
                ("expects/hit", byHits, acrossHits),
                ("expected-by", byContext, acrossContext),
                ("beside", byMates, acrossMates),
                ("beside/ppmi", byPointwise, acrossPointwise),
            })
        {
            output.WriteLine(
                $"{what,-12}| {Sweep.Spread(into["rooms"]),14} "
                + $"| {Sweep.Spread(into["things"]),14} "
                + $"| {Sweep.Spread(into["people"]),14} "
                + $"| {Sweep.Spread(other),14}");
        }

        // The instrument. A run where no set had two codes with profiles has nothing to
        // compare and every column above is about that.
        Assert.NotEmpty(across);
        Assert.NotEmpty(within["rooms"]);

        // No bar on the gap. Whether the population separates alternatives is the reading,
        // and a threshold written before the first take of it would be the fork's answer put
        // in front of its question.
        return;

    }

    /// <summary>
    /// The same question asked of the MOMENTS rather than of the population.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The distinction the reading beside this one conflated.</b> Fork 129 says to read
    /// likeness off the population rather than off the moment, and every row over there does.
    /// But a scope is not a sample of a code's company — it is what repair BUILT, so a scope
    /// holding a room word exists because it predicted something, and the company in scopes
    /// is a heavily selected sample of the company in moments.
    /// </para>
    /// <para>
    /// <b>And company in a moment is not co-occurrence of the pair</b>, which is the thing the
    /// plan's objection is about. Two room words never appear together, and both appear beside
    /// the same verbs and the same names — so a SECOND-order statistic over moments can
    /// separate them while a first-order one cannot. That is what distributional semantics is
    /// and it is the setup this fork's idea comes from.
    /// </para>
    /// <para>
    /// <b>No learner, so it is a fact about the world's transcripts.</b> The world is pushed
    /// through the front end and nothing else, which makes this a ceiling on ANY mechanism
    /// reading company: if the signal is absent here it is absent everywhere downstream, and
    /// if it is present here then what is missing is the mechanism rather than the signal.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_the_moments_separate_alternatives_by_the_company_they_keep()
    {
        // Two seeds and ten thousand moments, which is far more than the reading needs. The
        // separation is two thirds of the scale against a spread in the thousandths, so this
        // is sized to stay in the suite rather than to resolve anything.
        const int Seeds = 2;
        const int Moments = 10_000;

        var raw = new Dictionary<string, List<double>>();
        var lifted = new Dictionary<string, List<double>>();
        var acrossRaw = new List<double>();
        var acrossLifted = new List<double>();

        for (var seed = 1; seed <= Seeds; seed++)
        {
            var world = new Roaming(World(120, people: 4), seed);
            var front = new Joined(Joining.Resolved, resolution: 3, freshest: true);

            var together = new Dictionary<Code, Dictionary<Code, double>>();

            for (var at = 0; at < Moments; at++)
            {
                var codes = front.Codify(world.Next().Seen).Distinct().ToList();

                foreach (var code in codes)
                {
                    if (!together.TryGetValue(code, out var beside))
                        together[code] = beside = [];

                    foreach (var mate in codes)
                    {
                        if (mate == code) continue;

                        beside.TryGetValue(mate, out var so_far);
                        beside[mate] = so_far + 1.0;
                    }
                }
            }

            var sets = new (string What, IReadOnlyList<Code> Codes)[]
            {
                ("rooms", world.Named),
                ("things", world.Called),
                ("people", world.Walking),
            };

            foreach (var (what, _) in sets)
            {
                raw.TryAdd(what, []);
                lifted.TryAdd(what, []);
            }

            foreach (var (profiles, into, other) in
                new (Dictionary<Code, Dictionary<Code, double>> Profiles,
                    Dictionary<string, List<double>> Into, List<double> Other)[]
                {
                    (together, raw, acrossRaw),
                    (Pointwise(together), lifted, acrossLifted),
                })
            {
                Compare(profiles, sets, into, other);
            }
        }

        output.WriteLine(
            $"cosine over the company a code keeps IN A MOMENT, {Seeds} seeds, "
            + $"{Moments} moments, no learner");

        output.WriteLine("weighting | rooms          | things         | people         | across");

        foreach (var (what, into, other) in
            new (string What, Dictionary<string, List<double>> Into, List<double> Other)[]
            {
                ("counts", raw, acrossRaw),
                ("ppmi", lifted, acrossLifted),
            })
        {
            output.WriteLine(
                $"{what,-10}| {Sweep.Spread(into["rooms"]),14} "
                + $"| {Sweep.Spread(into["things"]),14} "
                + $"| {Sweep.Spread(into["people"]),14} "
                + $"| {Sweep.Spread(other),14}");
        }

        // The instrument. A front end emitting nothing makes every column above a reading
        // about the encoding rather than about the transcripts.
        Assert.NotEmpty(acrossRaw);
        Assert.NotEmpty(raw["rooms"]);

        // AND THE BAR, because this one is not a null. Under PPMI a room word is 0.986 alike
        // to another room word and 0.302 alike to a thing or a person -- two thirds of the
        // scale, against spreads in the thousandths.
        //
        // Under raw counts it is flat at 0.993 against 0.965, which is the frequency
        // domination PPMI exists to remove: a common code shares company with everything, so
        // a cosine over counts says every pair is alike and means nothing by it.
        //
        // So the signal is in the MOMENTS and the population destroys it. Fork 129 says to
        // read likeness off the population RATHER than the moment, and the reading beside
        // this one is that fork getting nothing from four statistics. A scope is what repair
        // built rather than a sample of what a code keeps company with.
        //
        // And *alternatives never co-occur* is about FIRST-order co-occurrence, which is
        // what rung five reads. Two room words never appear together and both appear beside
        // the same verbs and the same names, so the second-order statistic never needs them
        // to meet. That is the whole of what this changes.
        foreach (var (what, within) in lifted)
            Assert.True(within.Average() > acrossLifted.Average() + 0.2,
                $"{what} are {within.Average():F3} alike to each other against "
                + $"{acrossLifted.Average():F3} across the sets, so the company a code keeps "
                + "in a moment no longer separates alternatives and the reading that put the "
                + "signal there rather than in the population is stale");

        // And the raw row being flat, which is what says the correction is load-bearing
        // rather than decorative. A pass here with the bar above red would mean PPMI stopped
        // mattering; a pass with it green means both halves still hold.
        Assert.True(
            raw["rooms"].Average() - acrossRaw.Average() < 0.2,
            "a cosine over raw counts now separates the sets too, so frequency domination is "
            + "no longer what flattens it and PPMI is doing less than this reading says");
    }

    /// <summary>
    /// How present a CATEGORY would be, if one were minted over a set of alternatives.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The delivery question, asked before any delivery is built.</b> The reading beside
    /// this one says the alternatives are recoverable from the moments. What a mechanism would
    /// then DO with them is a separate question, and the obvious answer — fold a category code
    /// into the moment whenever any member fires, the way a name folds in when all its members
    /// do — has a refutation waiting for it.
    /// </para>
    /// <para>
    /// <b>Genesis rooting on a code that has never varied is refuted</b> at 7.4 standard
    /// errors and the plan carries the row. A category over alternatives is exactly the shape that
    /// risks it: the members substitute for one another, so if the world names one of them
    /// every moment then the category is in every moment and separates nothing.
    /// </para>
    /// <para>
    /// <b>So it is measured rather than argued.</b> A category present in nearly every moment
    /// cannot be a root and its delivery has to be something other than a code — the coarser
    /// form a scope projects to, which is where forks 83 to 85 already sit. A category present
    /// in half of them varies and a code is available.
    /// </para>
    /// </remarks>
    [Fact]
    public void How_often_a_category_over_alternatives_would_be_in_the_moment()
    {
        const int Moments = 10_000;

        var world = new Roaming(World(120, people: 4), seed: 1);
        var front = new Joined(Joining.Resolved, resolution: 3, freshest: true);

        var sets = new (string What, IReadOnlyList<Code> Codes)[]
        {
            ("rooms", world.Named),
            ("things", world.Called),
            ("people", world.Walking),
        };

        var present = sets.ToDictionary(one => one.What, _ => 0);
        var members = sets.ToDictionary(one => one.What, _ => 0);

        for (var at = 0; at < Moments; at++)
        {
            var codes = front.Codify(world.Next().Seen).ToHashSet();

            foreach (var (what, of) in sets)
            {
                var held = of.Count(codes.Contains);

                if (held == 0) continue;

                present[what]++;
                members[what] += held;
            }
        }

        output.WriteLine($"{Moments} moments, no learner");
        output.WriteLine("category | in the moment | members when present");

        foreach (var (what, _) in sets)
            output.WriteLine(
                $"{what,-9}| {present[what] / (double)Moments,13:F3} "
                + $"| {(present[what] == 0 ? 0.0 : members[what] / (double)present[what]),20:F2}");

        // The instrument. A set the world never emits says nothing about a category over it.
        Assert.All(sets, one => Assert.True(present[one.What] > 0,
            $"{one.What} never reached a moment, so this world is not emitting the set and "
            + "the reading is about the front end"));

        // No bar on the rate. What it has to be for a code to work is a fact about the
        // delivery nobody has designed yet, and a threshold here would be that design
        // asserted rather than chosen.
    }

    /// <summary>
    /// Whether the SHIPPED grouping recovers the alternatives, against the answer key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The reading above is a hand-built table</b> and this is the object that would ship.
    /// <see cref="Codes.Alternating"/> already accumulates a code's company over moments —
    /// the same counts, kept by the class the plan says nothing wires — and
    /// <see cref="Codes.Alternating.ByLikeness"/> groups on a cosine over them. So the
    /// question is not whether the machinery exists. It is whether its statistic finds what
    /// the transcripts hold.
    /// </para>
    /// <para>
    /// <b>Scored against the sets the world names</b>, and a group is credited to whichever
    /// set most of it belongs to. Purity is how much of a group is that set and reach is how
    /// much of the set the groups between them found — the two directions of the same
    /// question, because a grouper that puts every code in one bag scores perfectly on the
    /// second and a grouper that never joins anything scores perfectly on the first.
    /// </para>
    /// <para>
    /// <b>The threshold is swept rather than chosen</b>, because a cosine bar picked here
    /// would be this reading deciding its own answer. What the sweep is for is whether ANY
    /// bar separates the sets, which is a property of the statistic rather than of the number.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_the_shipped_grouping_recovers_the_alternatives()
    {
        const int Moments = 10_000;

        var world = new Roaming(World(120, people: 4), seed: 1);
        var front = new Joined(Joining.Resolved, resolution: 3, freshest: true);

        var alternating = new Alternating();

        for (var at = 0; at < Moments; at++)
            alternating.Watch(front.Codify(world.Next().Seen).ToHashSet());

        // A second stream on the same seed, for the diagnosis below. The grouper's own tables
        // are private, so whether a pair ever met is re-derived from an identical run rather
        // than read out of it.
        var watching = new Roaming(World(120, people: 4), seed: 1);
        var beside = new Joined(Joining.Resolved, resolution: 3, freshest: true);

        alternating.Settle();

        var sets = new (string What, IReadOnlySet<Code> Codes)[]
        {
            ("rooms", world.Named.ToHashSet()),
            ("things", world.Called.ToHashSet()),
            ("people", world.Walking.ToHashSet()),
        };

        var known = sets.SelectMany(one => one.Codes).ToHashSet();

        output.WriteLine($"{Moments} moments, {alternating.Moments} watched");
        output.WriteLine("how          alike | groups | biggest | purity | reach");

        var best = new Dictionary<string, double>();

        foreach (var (how, group_by) in
            new (string How, Func<double, IReadOnlyList<IReadOnlySet<Code>>> By)[]
            {
                ("space/never", one => alternating.BySpace(one, 20, Meeting.Never)),
                ("count/never", one => alternating.ByLikeness(one, 20, Meeting.Never)),
                ("ppmi/never", one => alternating.ByCompany(one, 20, Meeting.Never)),
                ("space/rarely", one => alternating.BySpace(one, 20, Meeting.Rarely)),
                ("count/rarely", one => alternating.ByLikeness(one, 20, Meeting.Rarely)),
                ("ppmi/rarely", one => alternating.ByCompany(one, 20, Meeting.Rarely)),

                // The bar read off the counts rather than picked, so the level it is handed
                // is ignored and it prints as one row repeated. What it is being read against
                // is the best of the four hand-picked ones above.
                ("chance/never", _ => alternating.ByChance(Counting.Company, 20, Meeting.Never)),
                ("chance/rarely", _ => alternating.ByChance(Counting.Company, 20, Meeting.Rarely)),

                // And the same bar read off TIME rather than company, which is the other arm
                // of `Counting` and the one `Deriving` fills a vocabulary from. It is here
                // because an arm measured on one world is a verdict on that world: this is
                // the second, and what it says about a corpus-shaped stream is not what
                // `Returning`'s runs say about a moving thing.
                ("time/never", _ => alternating.ByChance(Counting.Time, 20, Meeting.Never)),
                ("time/rarely", _ => alternating.ByChance(Counting.Time, 20, Meeting.Rarely)),

                // And the same bar over company WEIGHED, which is here because the raw one
                // gets stricter as evidence accumulates -- both codes' shuffles converge on
                // the alphabet's marginal, so their cosine goes to one and nothing observed
                // can beat it. This world is ten thousand moments deep, so whether the reading
                // above was taken past that point is exactly what these two rows say.
                ("weighed/never", _ => alternating.ByChance(Counting.Weighed, 20, Meeting.Never)),
                ("weighed/rarely", _ => alternating.ByChance(Counting.Weighed, 20, Meeting.Rarely)),
            })
        foreach (var alike in new[] { 0.5, 0.8, 0.9, 0.95 })
        {
            var groups = group_by(alike)
                .Where(group => group.Any(known.Contains))
                .ToList();

            if (groups.Count == 0)
            {
                output.WriteLine($"{how,-13}{alike,5:F2} |      0 |       - |      - |     -");
                continue;
            }

            var scored = groups
                .Select(group =>
                {
                    var mine = group.Where(known.Contains).ToList();

                    var (what, held) = sets
                        .Select(one => (one.What, Held: mine.Count(one.Codes.Contains)))
                        .OrderByDescending(one => one.Held)
                        .First();

                    return (What: what, Held: held, Of: mine.Count);
                })
                .Where(one => one.Held > 0)
                .ToList();

            var purity = scored.Count == 0
                ? 0.0
                : scored.Sum(one => one.Held) / (double)scored.Sum(one => one.Of);

            var reach = scored
                .GroupBy(one => one.What)
                .Sum(group => group.Max(one => one.Held))
                / (double)known.Count;

            best[how] = Math.Max(best.GetValueOrDefault(how), purity * reach);

            output.WriteLine(
                $"{how,-13}{alike,5:F2} | {groups.Count,6} | {groups.Max(one => one.Count),7} "
                + $"| {purity,6:F3} | {reach,5:F3}");
        }

        output.WriteLine("");

        foreach (var (how, score) in best.OrderByDescending(one => one.Value))
            output.WriteLine($"{how,-13}| best purity times reach {score:F3}");

        // And where it went, because a nought at every bar is not a reading about the
        // threshold. `Grouped` admits a code to a group only where it has NEVER ONCE
        // co-occurred with every member -- the *alternatives never co-occur* objection
        // written as an absolute -- so this counts how many of the answer key's own pairs
        // that clause refuses.
        var together = 0;
        var pairs = 0;

        foreach (var (_, codes) in sets)
        {
            var of = codes.ToList();

            for (var left = 0; left < of.Count; left++)
                for (var right = left + 1; right < of.Count; right++)
                {
                    pairs++;

                    if (Beside(of[left], of[right])) together++;
                }
        }

        output.WriteLine(
            $"{together} of {pairs} within-set pairs have co-occurred at least once");

        Assert.True(alternating.Moments == Moments,
            $"{alternating.Moments} moments reached the grouper of {Moments} pushed, so this "
            + "is a reading about the wiring rather than about the statistic");

        // The fact the diagnosis rested on, kept because it is what the clause change was
        // for. A moment here spans three sentences, so two room words land in one window
        // constantly even though a person is in ONE room. Under the clause this replaced --
        // refusing a pair that had co-occurred even once -- every one of these was refused
        // and every grouping returned nought at every bar.
        Assert.Equal(pairs, together);

        // And the bars, because this is no longer a null. The clause change alone takes the
        // grouping from NOUGHT at every threshold to three groups at a purity of 1.000 and a
        // reach of 0.929 -- thirteen of the fourteen codes the world names, in three groups
        // that are exactly its three sets.
        //
        //   how     alike | groups | biggest | purity | reach
        //   space    0.50 |      3 |      12 |  0.923 | 0.857
        //   counted  0.90 |      3 |       8 |  1.000 | 0.929
        //   ppmi     0.50 |      3 |       6 |  1.000 | 0.929
        //
        // So the fork's wall was one line. What blocked it was never the statistic being
        // hard; it was an admission test that refused a pair for meeting once.
        Assert.True(best["ppmi/rarely"] > 0.8,
            $"weighing company by how surprising it is recovers {best["ppmi/rarely"]:F3} of the "
            + "answer key, so the grouping no longer finds the alternatives this world names "
            + "and the clause change is not what unlocked it");

        // And the older arm loses, which is what its own remark asked for: `BySpace` discards
        // the counts, and one of the two goes when they are compared. It is kept until a
        // second world says the same, because one world's grid is a verdict on the world.
        Assert.True(best.GetValueOrDefault("space/rarely") < best["ppmi/rarely"],
            $"discarding the counts scores {best.GetValueOrDefault("space/rarely"):F3} against {best["ppmi/rarely"]:F3} "
            + "for weighing them, so the arm written to lose has stopped losing");

        // No bar separating the two weighted arms. They tie on the peak here, and PPMI is
        // flat from 0.50 to 0.90 where counting needs 0.90 -- a difference in how much a
        // threshold matters rather than in what is found. Ranking them wants a second world.
        return;

        bool Beside(Code one, Code two)
        {
            for (var at = 0; at < Moments; at++)
            {
                var codes = beside.Codify(watching.Next().Seen).ToHashSet();

                if (codes.Contains(one) && codes.Contains(two)) return true;
            }

            return false;
        }
    }

    [Fact]
    public void What_the_effect_question_is_worth_before_anything_learns()
    {
        var world = new Roaming(
            World(120, people: 4) with { Examining = Examining.Effect }, seed: 1);

        var asked = Enumerable.Range(0, 2_000).Select(_ => world.Next()).ToList();

        var moved = asked.Count(one => one.Outcome == 1);

        var byVerb = asked
            .GroupBy(one => one.Seen.Question()[1])
            .Select(group => (
                Words: group.First().Seen.Question().Count,
                Count: group.Count(),
                Moved: group.Count(one => one.Outcome == 1)))
            .OrderByDescending(one => one.Count)
            .ToList();

        var ceiling = byVerb.Sum(one => Math.Max(one.Moved, one.Count - one.Moved))
            / (double)asked.Count;

        output.WriteLine($"asked {asked.Count} | moved {moved / (double)asked.Count:F3} "
            + $"| marginal {Math.Max(moved, asked.Count - moved) / (double)asked.Count:F3} "
            + $"| verb ceiling {ceiling:F3}");

        foreach (var (words, count, up) in byVerb)
            output.WriteLine(
                $"  {words}-word verb | {count,5} asked | moved {up / (double)count:F3}");

        // The instrument has a subject, which is the first thing to establish. A question
        // whose answer never varies is a constant wearing a question's clothes.
        Assert.InRange(moved / (double)asked.Count, 0.02, 0.98);

        // And the verb does not answer it, which is the whole reason this world is worth
        // asking. If a constant per verb were perfect the effect question would be a verb
        // classifier and every score on it would be a front-end reading.
        Assert.True(ceiling < 0.99,
            $"a constant per verb answers {ceiling:F3} of the effect question, so it is "
            + "answerable from the question's own words and the transcript is decoration");

        // And it is ON the marginal, which is the property that makes one bar readable. A
        // verb ceiling ABOVE the marginal would put a one-code rule between guessing and
        // binding, and a learner sitting between them would be unattributable. This asserts
        // the gap is nought so that any point over the marginal is the carrying case.
        var marginal = Math.Max(moved, asked.Count - moved) / (double)asked.Count;

        Assert.True(ceiling - marginal < 0.005,
            $"a constant per verb reads {ceiling:F3} against a marginal of {marginal:F3}, so "
            + "there is a cheap rule between guessing and binding after all and a learner "
            + "between the two bars says nothing about either");
    }

    /// <summary>
    /// <b>The effect question changes what is asked</b>, never the house it is about.
    /// </summary>
    /// <remarks>
    /// <b>A world whose second question redrew the walk</b> would make its two arms
    /// incomparable, and nothing else here could say so — two transcripts differing by
    /// one draw read identically from every column. The walk is asserted identical and the
    /// question asserted different, which is the pair of failures that look like one.
    /// </remarks>
    [Fact]
    public void The_effect_question_is_asked_about_the_same_walk()
    {
        var where = new Roaming(World(120, people: 4), seed: 7).Next();
        var effect = new Roaming(
            World(120, people: 4) with { Examining = Examining.Effect }, seed: 7).Next();

        // The effect arm holds the walk's last statement back as its question, so its
        // transcript is one shorter and the rest is word for word the same.
        Assert.Equal(where.Seen.Said().Count - 1, effect.Seen.Said().Count);

        foreach (var (mine, theirs) in effect.Seen.Said().Zip(where.Seen.Said().Skip(1)))
            Assert.Equal(theirs, mine);

        Assert.Equal(where.Seen.Said()[0], effect.Seen.Question());
        Assert.NotEqual(where.Seen.Asked, effect.Seen.Asked);

        Assert.Equal(6, new Roaming(World(120, people: 4), seed: 7).Outcomes);
        Assert.Equal(
            2,
            new Roaming(World(120, people: 4) with { Examining = Examining.Effect }, seed: 7)
                .Outcomes);
    }

    /// <summary>
    /// Declining to intervene is the walk the world always drew, statement for statement.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The spine's last tier</b>, and the first thing it owes is that it cost nothing.
    /// Every reading on this world was taken watched, and a seam that shifted the draw by one
    /// call would have made all of them incomparable with everything taken after it — two
    /// transcripts differing by one draw read identically from every column, so nothing but
    /// this could say so.
    /// </para>
    /// <para>
    /// <b>Nothing means the learner does not intervene</b>, rather than the walk standing
    /// still. The people here walk whether or not anything chooses for them, so declining
    /// leaves the world drawing its own last step — which is what makes the watched arm a
    /// chooser rather than a different world.
    /// </para>
    /// <para>
    /// <b>And the state is read before every step</b>, which is what could have shifted it.
    /// <see cref="IActed{TSeen}.Now"/> draws the house and every step but the last so that
    /// there is a state to be read at all, and <see cref="IWorld{TSeen}.Next"/> finishes that
    /// same walk. A caller that never asks gets the walk drawn whole, and the assertion is
    /// that the two roads consume the generator identically.
    /// </para>
    /// </remarks>
    [Fact]
    public void Declining_to_intervene_is_the_walk_the_world_always_drew()
    {
        var watched = new Roaming(World(120, people: 4), seed: 7);
        var acted = new Roaming(World(120, people: 4), seed: 7);

        for (var episode = 0; episode < 40; episode++)
        {
            // The order `Watching` uses: read the state, decide, then take the turn. Reading it
            // is what draws the walk as far as its last step, so this is the road that could
            // differ and the other one is the road every earlier reading took.
            _ = acted.Now;
            acted.Do(null);

            var one = watched.Next();
            var two = acted.Next();

            Assert.Equal(one.Outcome, two.Outcome);
            Assert.Equal(one.Seen.Asked, two.Seen.Asked);
            Assert.Equal(one.Seen.Said().Count, two.Seen.Said().Count);

            foreach (var (mine, theirs) in one.Seen.Said().Zip(two.Seen.Said()))
                Assert.Equal(mine, theirs);
        }

        // And the held-out half is drawn where no chooser exists, so it asks about steps
        // nobody picked. That is stated rather than hidden: an exam over actions drawn
        // uniformly is what says a model of consequences survives past whatever the policy
        // happened to prefer.
        Assert.Equal(watched.Withheld.Count, acted.Withheld.Count);
    }

    /// <summary>
    /// A chosen verb is what the walk does, and one that cannot be done is a wait.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An impossible wish is not quietly turned into a possible one.</b> Substituting the
    /// nearest action would make the chooser's arm the world's own draw wearing the chooser's
    /// name, which is a fallback arm nobody meant to run — and dropping the step would leave
    /// the effect question answering about a statement nobody made.
    /// </para>
    /// <para>
    /// <b>Going somewhere is the one that is always possible</b>, so it is the verb this can
    /// assert on without arranging the house first. Taking needs something loose underfoot
    /// and dropping needs a full hand, and at the opening of a walk neither is guaranteed.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_chosen_verb_is_done_where_it_can_be_and_waited_where_it_cannot()
    {
        var went = Kinds.Named(46, "went");
        var waited = Kinds.Named(46, "waited");

        var going = new Roaming(
            World(1, people: 1) with { Examining = Examining.Effect }, seed: 5);

        var dropping = new Roaming(
            World(1, people: 1) with { Examining = Examining.Effect }, seed: 5);

        for (var episode = 0; episode < 40; episode++)
        {
            _ = going.Now;
            going.Do(0);

            _ = dropping.Now;
            dropping.Do(2);

            // The effect question holds the last statement back as what it asks about, so
            // the chosen step is exactly the question and nothing has to be dug out of the
            // transcript to read it.
            Assert.Contains(went, going.Next().Seen.Question());

            // And a walk of one step opens with nobody holding anything, so dropping is
            // never possible on it and the wish is refused every time.
            Assert.Contains(waited, dropping.Next().Seen.Question());
        }

        Assert.Equal(3, new Roaming(World(120, people: 4), seed: 5).Doings);
    }

    /// <summary>
    /// <b>Whether a statement it is being told</b> can be learnt about at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The architecture line that had nothing under it, asked as a question.</b> Under
    /// <see cref="Examining.Where"/> a statement is background and the learner is never
    /// wrong about one; here it commits to what the statement DOES and the world settles it.
    /// Whether the commitment is any good is what this grid is for.
    /// </para>
    /// <para>
    /// <b>One bar, and the ceiling grid is what makes it one.</b> A constant per verb lands
    /// on the marginal, so there is no cheap rule between guessing and binding — every point
    /// over about 0.86 is a walker carrying something, which no word of the question names.
    /// </para>
    /// <para>
    /// <b>What would drop this world was written before the grid ran</b>, and it fired.
    /// Every arm landing on the marginal within the seed spread was to say the effect
    /// question is rung four's and this world reaches it no better than <c>Roaming</c>'s
    /// other one does. They landed BELOW it — 0.838 to 0.855 against a marginal of 0.865,
    /// three seeds each, the store and the backward lookup alike.
    /// </para>
    /// <para>
    /// <b>Below rather than on is the sharper reading</b>, and it is not the one predicted.
    /// A population blind to the carrying case would sit ON the constant by answering *did
    /// not move* everywhere; sitting under it means commitments are answering *moved* on
    /// evidence that does not support it. What is scarce here is not coverage.
    /// </para>
    /// <para>
    /// <b>So this ranks no arms and is kept as a ceiling probe</b>, which is a different job
    /// and is said here so nobody reads a future row off it as a comparison. What it now
    /// prices is rung four, on a bar computed with no learning and a headroom named exactly:
    /// a walker carrying something, which no word of the question names.
    /// </para>
    /// <para>
    /// <b>And it leaves the mechanism itself unmeasured</b>, which is the honest gap. A told
    /// statement settles now and this is the only world that asks one to, so whether the
    /// machinery is any good at what a statement DOES cannot be told apart from whether this
    /// question needs an unbuilt rung. An effect question a conjunction could answer would
    /// separate them, and that is the next world rather than the next arm.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_what_a_statement_does_can_be_learnt_from_the_transcript()
    {
        // The same arms the where question is read on, so a difference between the two grids
        // is the question. `Chained` is the best lookup over the transcript and the two
        // `Resolved` cells are the store at the depths its own grid separates.
        var arms = new (string Name, Joined Joined)[]
        {
            (nameof(Joining.Chained), new Joined(Joining.Chained)),
            ("Resolved(1)", new Joined(Joining.Resolved, resolution: 1)),
            ("Freshest(3)", new Joined(Joining.Resolved, resolution: 3, freshest: true)),
        };

        var scores = Scored(arms, people: 4, Examining.Effect);

        foreach (var (name, taken) in scores)
            output.WriteLine($"{name,-12}| worst {taken.Min():F3} | best {taken.Max():F3}");

        // A TRIPWIRE RATHER THAN A BAR, and it is pointed the way the reading went. The
        // marginal is 0.865 at these dials, measured in the ceiling grid beside this one,
        // and the best of nine runs here is 0.855. So this fails the day something reaches
        // the carrying case -- which is the day this world stops being a ceiling probe and
        // starts ranking arms, and the day the leaf above needs rewriting.
        Assert.True(scores.Values.Max(one => one.Max()) < 0.865,
            "an arm has moved the effect question off its marginal, so the headroom that was "
            + "rung four's is reachable after all. Read the rows above before the plan's leaf "
            + "-- this world was kept as a probe on the reading that nothing reaches it");
    }

    /// <summary>
    /// Whether a scope ever takes the provenance of a step, given the chance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="IQuantizer{TObservation}.Forced"/> had no reader for the life of the
    /// branch</b>, so a scope naming a code the learner chose and one naming the same code
    /// the world drew were the same scope with their evidence added together. That is
    /// <c>P(y | x)</c> standing in for <c>P(y | do(x))</c>, and no amount of counting the
    /// first yields the second. <see cref="Intervened"/> is the reader, on rung three's seam:
    /// the moment carries a derived code beside each forced one, so a scope may name the
    /// doing and repair may reach for it.
    /// </para>
    /// <para>
    /// <b>Genesis is barred from rooting on one</b>, exactly as it is barred from rooting on
    /// a precedence. <i>I did something</i> with no idea what followed is a rule about agency
    /// rather than about the world, so the only way one of these enters a scope is repair
    /// choosing it — which happens where the plain code fails to separate the misses from
    /// the hits, and that is what a causal claim IS here.
    /// </para>
    /// <para>
    /// <b>What would drop the arm, written before the run.</b> If no resident scope names an
    /// intervention code under a chooser, then nothing here comes apart between doing and
    /// seeing, the reader buys nothing, and the channel goes with a revival row saying a
    /// world where a common cause makes them differ would bring it back.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_chosen_step_is_marked_and_the_population_may_read_it()
    {
        var scopes = 0;
        var resident = 0;
        var repairs = 0L;

        // Two choosers, and the second is what the assertion rests on. Acting EVERY round
        // makes the question the chosen statement every round, so a doing is present exactly
        // where its word is in the question -- and a scope taking it would be separating on
        // position wearing provenance's name, which is this repo's own trap about two arms
        // that score alike not being one mechanism. Acting on a coin makes the question the
        // last statement either way, so the doing is the only thing that moved.
        foreach (var (name, often) in new[] { ("always", 1.0), ("half", 0.5) })
        {
            var world = new Roaming(
                World(120, people: 4) with { Examining = Examining.Effect }, seed: 1);

            var brain = new Brain(new CommittingSettings { Capacity = 20_000 }, seed: 1);
            var picking = new Random(1);

            var tally = new Bench(
                new Watching<Coded>(
                    world,
                    new Joined(Joining.Resolved, resolution: 1),
                    acting: Chooses.From(
                        _ => picking.NextDouble() < often ? picking.Next(3) : null)),
                brain)
                .Run(10_000, sweep: 1000, target: 0.9, window: 2000);

            var naming = brain.Held.All.Count(one => one.Scope.Any(Intervened.Names));

            output.WriteLine(
                $"{name,-7}| held {tally.Resident} commitments, {tally.Repaired} repairs, "
                + $"{naming} scopes naming a doing | drawn {tally.Recent:F3}");

            (scopes, resident, repairs) = (naming, tally.Resident, tally.Repaired);
        }

        // The moment carries them, which is the half this asserts outright. A run where the
        // world marked nothing would leave the population figure at nought for a reason that
        // has nothing to do with what repair chose.
        var marked = new Roaming(
            World(4, people: 4) with { Examining = Examining.Effect }, seed: 1);

        _ = marked.Now;
        marked.Do(0);

        Assert.NotNull(marked.Next().Seen.Assigned);

        // And declining marks nothing, which is what keeps the watched arm the arm every
        // earlier reading was taken on.
        var declined = new Roaming(
            World(4, people: 4) with { Examining = Examining.Effect }, seed: 1);

        _ = declined.Now;
        declined.Do(null);

        Assert.Null(declined.Next().Seen.Assigned);

        Assert.True(scopes > 0,
            $"{resident} commitments were held over {repairs} repairs and not "
            + "one scope names a doing, so nothing here comes apart between doing a thing "
            + "and seeing it. Delete `Forced` and `Intervened` with a revival row naming a "
            + "world where a common cause makes them differ.");
    }
}
