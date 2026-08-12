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
/// <b>bAbI LOOKED LIKE IT DEMANDED REASONING AND ITS HELD-OUT HALF WAS ALL RE-READING.</b>
/// That cost this branch real time, and every score taken on it meant something other than
/// what it appeared to. So a new world is owed the same interrogation before a learner is
/// pointed at it, and the interrogation is cheap: the world knows its own state, so what a
/// shallow rule would reach is arithmetic rather than a training run.
/// </para>
/// <para>
/// <b>THE THREE COLUMNS ARE THE WHOLE INSTRUMENT.</b> The MARGINAL is always saying the
/// commonest room. The OPENING rule answers with the room the thing was first said to be
/// in, which is what a bag of the whole story reads straight off and is right exactly when
/// nothing moved. The LATEST rule answers with the most recent room word in the transcript,
/// which is recency and is what every displacement arm on this branch has actually been
/// doing. A perfect tracker is 1.000 by construction.
/// </para>
/// <para>
/// <b>SO THE WORLD EARNS ITS KEEP ONLY IF BOTH SHALLOW RULES SIT NEAR THE MARGINAL.</b> If
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

                // WHICH THING IS BEING ASKED ABOUT, READ OFF THE QUESTION. The question is a
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

                // THE RECENCY RULE, KEYED ON NOTHING. The newest statement holding any room
                // word at all, which is what a displacement arm reaches when the key it was
                // given is a word every sentence contains.
                var newest = story.FirstOrDefault(one => rooms.Any(one.Contains));

                if (newest is not null
                    && rooms.FindIndex(newest.Contains) is var now && now == answer) latest++;

                // AND WHETHER THE ANSWER IS IN THE ROOM AT ALL, which is the instrument check
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

        // THE BAR, AND `Steps` TURNS OUT TO BE A DIAL RATHER THAN A SETTING. It walks the
        // shallow ceiling from a transcript that answers itself outright down to one where
        // nothing shallow beats guessing, and it does that while the marginal and the
        // recency rule stay flat -- which is one axis moving one thing, and is what a
        // benchmark with a parser, a vocabulary and a quest length all varying could not
        // have given.
        //
        // SO THE DEEPEST CELL IS WHERE A LEARNER SHOULD BE RUN, and the bar is that both
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

        // AND THE SHALLOW CEILING FALLS RATHER THAN JUMPS, which is what makes `Steps` worth
        // having as an axis instead of two worlds. A dial that went straight from easy to
        // impossible would be a switch, and nothing could be read off the middle of it.
        Assert.True(shallow[0].Opening > shallow[12].Opening
            && shallow[12].Opening > shallow[30].Opening
            && shallow[30].Opening > shallow[120].Opening,
            "the shallow ceiling does not fall monotonically with the length of the walk, so "
            + "`Steps` is not the one axis this file reports it as");
    }

    /// <summary>
    /// What the learner reads where nothing shallow works — <b>the first score on a world
    /// whose held-out half is genuinely unseen.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>AT 120 STEPS, WHICH IS THE CELL THE CEILING GRID CHOSE RATHER THAN A ROUND
    /// NUMBER.</b> The opening rule and recency both sit at the marginal there, so anything
    /// over it is tracking rather than reading the transcript off.
    /// </para>
    /// <para>
    /// <b>AND THE VOCABULARY IS TINY WHATEVER THE WALK'S LENGTH, WHICH IS THE PROPERTY THAT
    /// MAKES THE BAG ARM MEAN SOMETHING.</b> Six rooms, four things and a handful of
    /// function words, so a bagged moment is the same size after 120 statements as after
    /// four — every word is present and none of them says WHEN. A bag here cannot be near
    /// the marginal by accident.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_learner_reads_where_the_transcript_stops_answering_itself()
    {
        foreach (var joining in new[]
            { Joining.Bagged, Joining.Recent, Joining.Addressed, Joining.Chained })
        {
            var world = new Roaming(World(120), seed: 1);
            var brain = new Brain(new CommittingSettings { Capacity = 20_000 }, seed: 1);

            var tally = new Trial<Asking>(world, new Joined(joining), brain)
                .Run(10_000, sweep: 1000, target: 0.9, window: 2000);

            var exam = tally.Unseen?.Accuracy ?? 0.0;

            output.WriteLine(
                $"{joining,-12}| exam {exam:F3} | own {tally.Recent:F3} "
                + $"| held {brain.Held.Count}");
        }

        // AND THE BAGGED ARM COMES BACK WITH AN EMPTY POPULATION, WHICH IS A FINDING ABOUT
        // THE FRONT END RATHER THAN A SCORE. Six rooms, four things and a few function
        // words means that after 120 statements essentially every word of the vocabulary is
        // present in every moment -- so the bag is the SAME MOMENT every round, nothing is
        // ever surprising, and genesis never fires at all. A constant moment mints nothing.
        //
        // IT IS THE OPPOSITE END OF THE FAULT THE ENGLISH ARMS HIT. There a growing
        // vocabulary outran the cap; here a tiny one makes the moment a constant. Both are
        // the front end deciding what the learner can possibly see, and both look like the
        // learner failing.

        // NO BAR YET, AND SAYING SO IS THE POINT. This is the first reading any learner has
        // taken on this world; a bar written beside it would be a level chosen from one run
        // rather than a claim anything refutes. What the columns are FOR is the next
        // session, and the ceiling grid above is what they are read against.
    }
}
