using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Thinking;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Kinship composition on somebody else's data — <b>the first world here whose
/// every question is relational and nothing else.</b>
/// </summary>
/// <remarks>
/// <b>CLEVR IS THE OPPOSITE WORLD AND THAT IS WHY THIS ONE EXISTS.</b> No kept
/// CLEVR question is spatial, so a relational mechanism there is row width bought
/// with noise — measured, and the position arm is a refuted row for it. Every
/// question here is a relation between two people, so whatever the role channel
/// can do shows here or nowhere.
/// </remarks>
public sealed class ClutrrTests(ITestOutputHelper output)
{
    /// <summary>Where the corpus is, or a failure that says how to get it.</summary>
    private static string Corpus
    {
        get
        {
            var corpus = Path.Combine(Tree.Repo(), "corpora", "clutrr_test.csv");

            Assert.True(File.Exists(corpus),
                $"the CLUTRR corpus is not at {corpus}. Fetch it with:\n"
                + "    bash corpora/fetch.sh");

            return corpus;
        }
    }

    private static ClutrrSettings World(
        int stories = 300, bool fleeting = false, bool roled = false) =>
        new() { Corpus = Corpus, Stories = stories, Fleeting = fleeting, Roled = roled };

    /// <summary>Asking again from what the last walk concluded, or not.</summary>
    /// <remarks>
    /// <b>ON THE QUESTION AND NOT ON A DIAL</b> — see <see cref="Question.Steps"/>.
    /// </remarks>
    private static Question Asking(int steps) => new() { Steps = steps };

    /// <summary>Right over asked on chains past three hops, and how deep it got.</summary>
    private static double Deep(ClutrrResult result) => result.Composed;

    // ---- what the corpus is, asserted rather than described -----------------

    [Fact]
    public void A_story_is_a_chain_of_stated_relations_and_a_pair_to_judge()
    {
        var world = new Clutrr(World(stories: 200));

        Assert.Equal(200, world.Stories.Count);

        foreach (var story in world.Stories)
        {
            // A CHAIN, NOT A SINGLE EDGE. One stated relation would be the answer
            // read straight back, which measures the parser.
            Assert.True(story.Hops >= 2);

            // AND THE QUERY PAIR IS NOT ONE OF THE STATED EDGES, or the question
            // would contain its own answer.
            Assert.DoesNotContain(
                story.Edges,
                edge => (edge.From, edge.To) == (story.Query.From, story.Query.To));

            Assert.True(story.Query.From < story.People);
            Assert.True(story.Query.To < story.People);
        }

        output.WriteLine($"{world} chance={world.Chance:F4}");
    }

    [Fact]
    public void A_person_belongs_to_one_story_and_is_never_shared()
    {
        // A CODE MINTED FROM THE NAME WOULD MAKE ONE NODE OUT OF EVERYBODY CALLED
        // JASON, which is the hub that destroyed the binding world before indexes
        // existed. The corpus reuses names across thousands of stories.
        var world = new Clutrr(World(stories: 300));

        var people = world.Stories
            .SelectMany(story => Enumerable.Range(0, story.People).Select(story.Who))
            .ToList();

        Assert.Equal(people.Count, people.Distinct().Count());
        Assert.All(people, code => Assert.Equal(Clutrr.Person, code.Modality));
    }

    [Fact]
    public void The_corpus_carries_chains_of_every_length_the_question_needs()
    {
        // WITHOUT THE LONG ONES THERE IS NOTHING TO COMPOSE. A two-hop chain states
        // its rule almost outright; the range is what separates recall from
        // composition, and a corpus of only short chains could not tell them apart.
        var world = new Clutrr(World(stories: 1200));

        var hops = world.Stories.Select(story => story.Hops).ToHashSet();

        Assert.Contains(2, hops);
        Assert.Contains(10, hops);
        Assert.True(hops.Count >= 8, $"only {hops.Count} distinct chain lengths");
    }

    [Fact]
    public void Reading_only_short_chains_is_a_narrower_world_and_not_a_broken_one()
    {
        var shallow = new Clutrr(World(stories: 1200) with { Longest = 3 });

        Assert.NotEmpty(shallow.Stories);
        Assert.All(shallow.Stories, story => Assert.True(story.Hops <= 3));
    }

    // ---- what the graph does with it ---------------------------------------

    [Fact]
    public void Every_short_chain_restates_its_own_answer_so_a_score_over_all_of_them_lies()
    {
        // THE CONTAMINATION, AND TWO COMMITS REPORTED IT AS COMPOSITION. When the
        // answer is `grandson` and a premise is also `grandson`, the answer's slot
        // code is already in a moment the graph just read -- so arriving at it
        // composes nothing. Every two-hop story restates; longer ones mostly do
        // not, so chain length was silently measuring contamination.
        var world = new Clutrr(World(stories: 300));

        var byHops = world.Stories
            .GroupBy(story => story.Hops)
            .OrderBy(group => group.Key)
            .ToList();

        foreach (var group in byHops)
            output.WriteLine($"  {group.Key,2} hops: "
                + $"{group.Count(story => story.Restated)}/{group.Count()} restated");

        Assert.All(
            byHops.Single(group => group.Key == 2),
            story => Assert.True(story.Restated));

        // AND THE LONG ONES MOSTLY DO NOT, which is what leaves anything to measure.
        var deep = world.Stories.Where(story => story.Hops >= 4).ToList();

        Assert.NotEmpty(deep);
        Assert.True(deep.Count(story => !story.Restated) > deep.Count / 2);
    }

    [Fact]
    public async Task Nothing_composes_and_what_it_says_instead_is_always_something_it_was_told()
    {
        // THE REAL FINDING, AND IT REPLACES TWO CLAIMS THAT WERE CONTAMINATION.
        // On a story whose answer is stated nowhere in it, the graph scores NOUGHT
        // -- and that is not silence and not budget. It answers, confidently, with
        // a relation the story itself stated, which is wrong BY CONSTRUCTION since
        // a fresh story's answer is by definition not one of those.
        //
        // SO THE DEFECT IS NOT "CANNOT AFFORD TO REACH IT". The walk is anchored to
        // the codes in front of it and a composed answer needs it to prefer a
        // relation that is NOT. This is the plan's "an answer that is no code it has
        // ever seen", arriving from a new direction and sharper: the answer code
        // EXISTS in the graph, written by other stories -- it is simply never
        // preferred over what is locally present.
        var arms = new List<ClutrrResult>();

        foreach (var roled in new[] { false, true })
        {
            using var run = new ClutrrRun(
                World(stories: 300, roled: roled), Fixture.Dials(stamina: 32.0), seed: 1);

            var result = await run.RunAsync();
            arms.Add(result);
            output.WriteLine($"roled={roled,-5} {result}");
        }

        var grouped = arms[0];
        var filled = arms[1];

        foreach (var arm in arms)
        {
            // NOUGHT. Asserted as the floor it is, so anything that composes lands
            // here as a failure and gets read rather than passing quietly.
            Assert.Equal(0, arm.Fresh.Right);

            // AND EVERYTHING IT DID SAY WAS AN ECHO. This is what separates "cannot
            // reach the answer" from "reaches the wrong kind of thing".
            var spoke = arm.Fresh.Asked - arm.Fresh.Silent;

            Assert.True(spoke > 0, "the walk was silent on every fresh story");
            Assert.Equal(spoke, arm.Echoed);
        }

        // THE ROLE CHANNEL IS STILL A REAL WIN, AND IT IS A WIN AT RECALL. It roughly
        // doubles the share of restated answers found, and it is less silent as well
        // as more right -- so it is not merely louder. What it does not do is
        // compose, and the previous commit said it did.
        Assert.True(filled.Recall > grouped.Recall * 1.5,
            $"the role channel did not clearly beat grouping at recall: "
            + $"{filled.Recall} against {grouped.Recall}");

        Assert.True(filled.Fresh.Silent < grouped.Fresh.Silent);
    }

    [Fact]
    public async Task Asking_again_from_what_it_concluded_is_the_first_thing_that_composes()
    {
        // THE FLOOR MOVED, AND ONLY BY LETTING A CONCLUSION BE ASKED FROM. One walk
        // answers fresh stories by echoing a relation the story stated, which is
        // wrong by construction -- nought right, every time. A second walk starting
        // from what the first concluded breaks the echo and lands answers that were
        // never in front of it.
        //
        // THE DENOMINATOR IS WHAT IT ANSWERED, NOT WHAT IT WAS ASKED, and saying so
        // is the honest part: this walk is silent on most fresh stories at any
        // budget tried, so a share over all of them would report the silence and
        // call it the mechanism. The silence is asserted separately below.
        var arms = new List<(int Steps, ClutrrResult Result)>();

        foreach (var steps in new[] { 1, 2 })
        {
            using var run = new ClutrrRun(
                World(stories: 300), Fixture.Dials(stamina: 32.0), seed: 1);

            var result = await run.RunAsync(Asking(steps));
            arms.Add((steps, result));

            var spoke = result.Fresh.Asked - result.Fresh.Silent;
            output.WriteLine($"steps={steps} fresh {result.Fresh.Right}/{spoke} answered "
                + $"(of {result.Fresh.Asked} asked) echoed={result.Echoed} :: {result}");
        }

        var once = arms[0].Result;
        var twice = arms[1].Result;

        var spokeOnce = once.Fresh.Asked - once.Fresh.Silent;
        var spokeTwice = twice.Fresh.Asked - twice.Fresh.Silent;

        Assert.True(spokeOnce > 0 && spokeTwice > 0, "an arm was silent on every fresh story");

        // ONE WALK CANNOT BE RIGHT HERE, AND NOT BY BAD LUCK. It answers with a
        // relation the story stated and a fresh story's answer is never one of
        // those, so nought is structural.
        Assert.Equal(0, once.Fresh.Right);
        Assert.Equal(spokeOnce, once.Echoed);

        // TWO WALKS BREAK THE ECHO. That is the mechanism, apart from the score.
        Assert.True(twice.Echoed < spokeTwice,
            "the second walk still echoed everything it was told");

        // AND ANSWER WELL ABOVE GUESSING, conditional on answering at all. Held as a
        // floor, so a regression that puts the echo back lands here.
        Assert.True(twice.Fresh.Right / (double)spokeTwice > twice.Chance * 3,
            $"asking again did not beat chance: {twice.Fresh.Right} of {spokeTwice} "
            + $"against a chance of {twice.Chance}");

        // AND THE SILENCE IS REPORTED BESIDE THE SCORE, which the plan names as a
        // trap in its own right: most fresh stories get no answer at all, so this
        // is a mechanism unlocked and not a world solved.
        Assert.True(twice.Fresh.Silent > twice.Fresh.Asked / 2,
            "the coverage problem has gone away and nobody noticed");
    }
}
