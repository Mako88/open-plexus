using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The three ceilings <see cref="Handing"/> was built to separate, taken with no learning at
/// all — <b>fork 105's ladder priced before a rung of it is built.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A FRONT-END ARM HAS A CEILING COMPUTABLE WITH NO LEARNING AND IT COSTS MILLISECONDS
/// AGAINST A RUNNER'S HOUR</b>, which is this repo's own trap and the reason this file
/// exists before any matcher does. A grid cannot tell a rule that failed to bind from a
/// front end that threw the binding away one call earlier; these three facts can, because
/// each is exact.
/// </para>
/// <para>
/// <b>AND THEY ARE FACTS RATHER THAN MEASUREMENTS, WHICH IS WHY THEY ARE ASSERTED TIGHT AND
/// NOT PRINTED.</b> Nothing here is a sweep: the bag is the same bag in every draw, the
/// right sentence is the only one holding the asked word, and the answer is the last word of
/// it. If any of the three ever stops holding, the world has drifted away from the question
/// it was built to ask and every number taken on it is owed a re-take.
/// </para>
/// <para>
/// <b>THE THIRD PROBE READS A POSITION AND MAY NEVER SHIP, WHICH IS SAID HERE RATHER THAN
/// DISCOVERED LATER.</b> It knows the template, so it is the far end of a gap in exactly the
/// standing of <c>ReturningTests</c>' tagged cell and fork 88's handed selection. What it is
/// for is to say that 1.0 is reachable at all, so a learner sitting at 0.5 is known to be
/// short of the world rather than at it.
/// </para>
/// </remarks>
public sealed class HandingTests
{
    private const int People = 4;
    private const int Draws = 500;

    private static Handing World(int seed = 1) =>
        new(new HandingSettings { People = People, Withheld = 0 }, seed);

    /// <summary>Everything the story said, with the order thrown away.</summary>
    /// <param name="told">One moment.</param>
    private static HashSet<Code> Bag(Recited told)
    {
        var all = new HashSet<Code>();

        foreach (var one in told.Said) all.UnionWith(one);

        return all;
    }

    /// <summary>Which sentence shares most words with the question.</summary>
    /// <param name="told">One moment.</param>
    /// <remarks>
    /// <b>FORK 88'S MECHANISM, WHICH IS SETTLED AND IS PRICED HERE AGAINST A WORLD THAT
    /// LEAVES IT SHORT.</b> Intersecting the question with each statement answers bAbI's
    /// first task; on this world it is worth one half and never one, and separating the two
    /// by a world rather than by an argument is the whole reason this file is not a grid.
    /// </remarks>
    private static int Selected(Recited told)
    {
        var asked = new HashSet<Code>(told.Asked);
        var best = 0;
        var most = -1;

        for (var at = 0; at < told.Said.Count; at++)
        {
            var shared = told.Said[at].Distinct().Count(asked.Contains);

            if (shared <= most) continue;

            most = shared;
            best = at;
        }

        return best;
    }

    /// <summary>
    /// <b>THE BAG IS THE SAME BAG IN EVERY DRAW, so nothing conditioned on it can beat the
    /// marginal.</b> The first ceiling, and it is proved rather than measured.
    /// </summary>
    /// <remarks>
    /// <b>THIS IS THE PROPERTY THE WHOLE WORLD RESTS ON.</b> The givers are a permutation of
    /// the people and the takers are another, so every person is said exactly twice and
    /// every thing exactly once, whoever handed what to whom. A learner reading
    /// <see cref="Recited.Bagged"/> is therefore looking at a constant and answering from
    /// nothing — which is what makes any lift off the marginal on this world attributable to
    /// binding and to nothing else.
    /// </remarks>
    [Fact]
    public void The_story_says_the_same_words_however_the_things_were_handed_over()
    {
        var world = World();
        var first = Bag(world.Next().Seen);

        // EVERY PERSON, EVERY THING, AND `gave`, `the`, `to`. Written out rather than read
        // off the world, so a world that quietly stopped saying one of them fails here.
        Assert.Equal((People * 2) + 3, first.Count);

        for (var draw = 1; draw < Draws; draw++)
            Assert.Equal(first, Bag(world.Next().Seen));
    }

    /// <summary>
    /// <b>And the answer moves while the bag does not</b>, which is the other half of the
    /// same claim and the half a constant bag cannot supply on its own.
    /// </summary>
    /// <remarks>
    /// <b>A WORLD WHOSE ANSWER WAS ALSO CONSTANT WOULD PASS THE CHECK ABOVE AND MEASURE
    /// NOTHING</b>, so the marginal is read here rather than assumed. Every person is the
    /// taker about equally often, which is what makes 1/<see cref="People"/> the number the
    /// first rung stands at rather than an upper bound somebody hoped for.
    /// </remarks>
    [Fact]
    public void Every_person_ends_up_with_the_asked_thing_about_equally_often()
    {
        var world = World();
        var counts = new int[People];

        for (var draw = 0; draw < Draws; draw++) counts[world.Next().Outcome!.Value]++;

        foreach (var count in counts)
            Assert.InRange(count / (double)Draws, 0.20, 0.30);
    }

    /// <summary>
    /// <b>The right sentence is decidable with no learning, and it names exactly two
    /// people.</b> The second ceiling, and it is one half exactly.
    /// </summary>
    /// <remarks>
    /// <b>BOTH HALVES OR THE NUMBER IS NOT A HALF.</b> That the overlap picks the right
    /// sentence every time is what makes selection free; that the sentence holds two
    /// candidates and one of them is the answer is what makes the best a selector can do a
    /// coin flip. A world where the right sentence sometimes named three people would sit
    /// below a half for a reason that has nothing to do with binding.
    /// </remarks>
    [Fact]
    public void Picking_the_sentence_that_shares_most_with_the_question_leaves_a_coin_flip()
    {
        var world = World();
        var cast = world.Called;

        for (var draw = 0; draw < Draws; draw++)
        {
            var turn = world.Next();
            var sentence = turn.Seen.Said[Selected(turn.Seen)];

            // THE ASKED THING IS IN IT, which is what says the selection was right without
            // the probe being handed which sentence to look at.
            Assert.Contains(turn.Seen.Asked[^1], sentence);

            var people = sentence.Where(cast.Contains).Distinct().ToList();

            Assert.Equal(2, people.Count);
            Assert.Contains(cast[turn.Outcome!.Value], people);
        }
    }

    /// <summary>
    /// <b>And reading the sentence's ORDER answers it outright.</b> The third ceiling, one,
    /// so anything above a half on this world is binding and nothing else.
    /// </summary>
    /// <remarks>
    /// <b>THE PROBE KNOWS THE TEMPLATE AND MAY NEVER SHIP</b>, which is the whole of its
    /// standing — see this class's own remarks. What it establishes is that the world is
    /// answerable at all from what it hands over, so a run stuck on the coin flip is short
    /// of the world rather than at it.
    /// </remarks>
    [Fact]
    public void Reading_the_last_word_of_that_sentence_answers_every_question()
    {
        var world = World();
        var cast = world.Called;

        for (var draw = 0; draw < Draws; draw++)
        {
            var turn = world.Next();
            var sentence = turn.Seen.Said[Selected(turn.Seen)];

            Assert.Equal(cast[turn.Outcome!.Value], sentence[^1]);
        }
    }

    /// <summary>
    /// <b>And the two people in that sentence are told apart by ORDER alone</b>, so the gap
    /// between the second ceiling and the third is carried by nothing else.
    /// </summary>
    /// <remarks>
    /// <b>THE ONE CHECK THAT SAYS THE WORLD IS HONEST RATHER THAN MERELY HARD.</b> If the
    /// giver and the taker differed in any other way a code could carry — a word only ever
    /// said in one of the two places, a thing that only ever goes one way, a person who is
    /// always the receiver — then a bag of that sentence would separate them and the third
    /// rung would be reachable without binding. Every person appears in both roles across
    /// the draws, so the roles are distinguishable by position and by nothing a set can see.
    /// </remarks>
    [Fact]
    public void Every_person_both_gives_and_receives_so_no_word_marks_a_role()
    {
        var world = World();
        var gave = new HashSet<Code>();
        var took = new HashSet<Code>();

        for (var draw = 0; draw < Draws; draw++)
        {
            foreach (var sentence in world.Next().Seen.Said)
            {
                gave.Add(sentence[0]);
                took.Add(sentence[^1]);
            }
        }

        Assert.Equal(People, gave.Count);
        Assert.Equal(gave, took);
    }
}
