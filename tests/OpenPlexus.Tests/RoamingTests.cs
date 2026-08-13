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
/// <b>bAbI looked like it demanded reasoning and its held-out half was all re-reading.</b>
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
/// <b>So the world earns its keep only if both shallow rules sit near the marginal.</b> If
/// the opening rule is strong the walk is too short and the transcript answers itself; if
/// recency is strong the world is asking *what happened last* rather than *where is it
/// now*, and a situation model would be scored for something a one-line rule does.
/// </para>
/// </remarks>
public sealed class RoamingTests(ITestOutputHelper output)
{
    private static RoamingSettings World(int steps) =>
        new() { Rooms = 6, Props = 4, Steps = steps, Withheld = 600 };

    [Fact]
    public void Whether_the_walk_makes_the_transcript_stop_answering_itself()
    {
        var shallow = new Dictionary<int, (double Marginal, double Opening, double Latest)>();

        foreach (var steps in new[] { 0, 4, 12, 30, 60, 120 })
        {
            var world = new Roaming(World(steps), seed: 1);

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

                var story = turn.Seen.Story;

                // Which thing is being asked about, read off the question. The question is a
                // set of words and exactly one of them is a thing, so this is the front
                // end's own intersection rather than the world being asked.
                var about = props.FirstOrDefault(one => turn.Seen.Question.Contains(one));

                // THE OPENING RULE. `Story` is newest first, so the oldest statement naming
                // this thing is the placement that opened the episode -- which is what a bag
                // holding the whole transcript has in front of it and no reason to discount.
                var placed = story
                    .LastOrDefault(one => one.Contains(about) && rooms.Any(one.Contains));

                if (placed is not null
                    && rooms.FindIndex(placed.Contains) is var was && was == answer) opening++;

                // The recency rule, keyed on nothing. The newest statement holding any room
                // word at all, which is what a displacement arm reaches when the key it was
                // given is a word every sentence contains.
                var newest = story.FirstOrDefault(one => rooms.Any(one.Contains));

                if (newest is not null
                    && rooms.FindIndex(newest.Contains) is var now && now == answer) latest++;

                // And whether the answer is in the room at all, which is the instrument check
                // rather than a ceiling. A world whose answering word is absent from the
                // transcript is unanswerable and every column above would be measuring that.
                if (story.Any(one => one.Contains(rooms[answer]))) reachable++;
            }

            output.WriteLine(
                $"steps {steps,3} | asked {asked,4} | marginal {marginal.Max() / (double)asked:F3} "
                + $"| opening {opening / (double)asked:F3} | latest {latest / (double)asked:F3} "
                + $"| answer present {reachable / (double)asked:F3}");

            shallow[steps] = (
                marginal.Max() / (double)asked, opening / (double)asked, latest / (double)asked);

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
    /// trap list's own rule, that a front-end arm's ceiling costs milliseconds against a
    /// runner's hour.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two columns and they bracket the learner rather than predicting it.</b> PRESENT is
    /// whether the answering room word survives the translation at all, which is an UPPER
    /// bound: a moment the answer is missing from cannot be answered by anything. PINNED is
    /// whether it is the ONLY room word left, which is a LOWER bound: a moment with one room
    /// word in it is answerable by a rule as simple as <i>say the room you can see</i>. What
    /// a learner has to work in is the gap between them.
    /// </para>
    /// <para>
    /// <b>So a high present with a low pinned is the selection problem, which is where every
    /// arm on this branch has already been.</b> The bag holds every room word in the house
    /// after a long walk, so it is 1.000 present and near nought pinned — the answer is
    /// there and nothing says which it is. An arm that raises PINNED is doing the thing a
    /// situation model is for.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_each_translation_leaves_in_the_room()
    {
        var world = new Roaming(World(120), seed: 1);

        var rooms = world.Named.ToHashSet();
        var pinning = new Dictionary<string, double>();
        var reaching = new Dictionary<string, double>();

        // The store walks its depth axis and every other arm is one cell, so the grid is a
        // cross rather than a list -- and depth nought is the store's own control, where an
        // entry is the statement that wrote it and nothing else.
        var arms = Enum.GetValues<Joining>()
            .Where(one => one != Joining.Resolved)
            .Select(one => (Name: one.ToString(), Joined: new Joined(one)))
            .Concat(Enumerable.Range(0, 4).Select(depth =>
                (Name: $"Resolved({depth})",
                 Joined: new Joined(Joining.Resolved, resolution: depth))))
            .Concat(Enumerable.Range(1, 3).Select(depth =>
                (Name: $"Freshest({depth})",
                 Joined: new Joined(Joining.Resolved, resolution: depth, freshest: true))));

        foreach (var (name, joined) in arms)
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
            + "mechanism is `Addressed` by a longer road");

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
    /// What the learner reads where nothing shallow works — <b>the first score on a world
    /// whose held-out half is genuinely unseen.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>At 120 steps, which is the cell the ceiling grid chose rather than a round
    /// number.</b> The opening rule and recency both sit at the marginal there, so anything
    /// over it is tracking rather than reading the transcript off.
    /// </para>
    /// <para>
    /// <b>And the vocabulary is tiny whatever the walk's length, which is the property that
    /// makes the bag arm mean something.</b> Six rooms, four things and a handful of
    /// function words, so a bagged moment is the same size after 120 statements as after
    /// four — every word is present and none of them says WHEN. A bag here cannot be near
    /// the marginal by accident.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_learner_reads_where_the_transcript_stops_answering_itself()
    {
        // The backward-reading arm that leads, and the store's two axes crossed. `Bagged`,
        // `Recent` and `Addressed` are not here because they are compared in the ceiling
        // grid and all three sit at the marginal; `Chained` is the one worth beating, being
        // the best any lookup over the transcript reached on this world.
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

        var scores = new Dictionary<string, List<double>>();

        foreach (var (name, joined) in arms)
        {
            scores[name] = [];

            // Seeds, because the claim is a comparison and a comparison on one run is an
            // anecdote. The world and the brain take the same seed, so an arm's whole run
            // moves together rather than the house being redrawn under a fixed population.
            foreach (var seed in new[] { 1, 2, 3 })
            {
                var world = new Roaming(World(120), seed);
                var brain = new Brain(new CommittingSettings { Capacity = 20_000 }, seed);

                var tally = new Trial<Asking>(world, joined, brain)
                    .Run(10_000, sweep: 1000, target: 0.9, window: 2000);

                var exam = tally.Unseen?.Accuracy ?? 0.0;

                scores[name].Add(exam);

                output.WriteLine(
                    $"{name,-12}| seed {seed} | exam {exam:F3} | own {tally.Recent:F3} "
                    + $"| held {brain.Held.Count}");
            }
        }

        foreach (var (name, taken) in scores)
            output.WriteLine($"{name,-12}| worst {taken.Min():F3} | best {taken.Max():F3}");

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
        // scores, which is what makes the two tests in this file one instrument. A moment
        // the answering word is missing from is unanswerable, so `present` in the grid above
        // caps each arm exactly -- and what an arm CONVERTS of its own cap is where the
        // learner's part of this shows. An arm reaching further and scoring lower is failing
        // to choose, which is the selection problem and not a failure to read.
    }
}
