using OpenPlexus.Codes;
using OpenPlexus.Graph;
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
    public async Task One_composition_carries_and_two_do_not()
    {
        // THE FINDING, AND IT IS THE ROLLOUT GAP IN A NEW PLACE. A two-hop chain
        // needs ONE composition applied and the graph does it far above chance. A
        // three-hop chain needs the result of that composition to become the input
        // to the next one, and there is nothing here that can hold a conclusion and
        // ask again from it -- so it scores nought, not badly.
        //
        // THE BUDGET IS PART OF THE CLAIM. At stamina 8 the walk cannot afford to
        // reach an answer at all and every length reads silent, which is the named
        // trap about a silence having two causes. This is the one that spends.
        using var run = new ClutrrRun(
            World(stories: 300), Fixture.Dials(stamina: 32.0), seed: 1);

        var result = await run.RunAsync();

        output.WriteLine(result.ToString());
        foreach (var (hops, asked, right) in result.ByHops)
            output.WriteLine($"  {hops,2} hops: {right,3}/{asked,-3} "
                + $"{(asked == 0 ? 0 : right / (double)asked):F3}");

        var shallow = result.ByHops.Single(one => one.Hops == 2);
        var deeper = result.ByHops.Where(one => one.Hops >= 3).ToList();

        Assert.True(shallow.Asked > 0, "no two-hop chain was asked about at all");

        // ONE HOP OF COMPOSITION, WELL CLEAR OF GUESSING.
        Assert.True(
            shallow.Right / (double)shallow.Asked > result.Chance * 3,
            $"two-hop chains did not beat chance: {shallow.Right} of {shallow.Asked}");

        // AND TWO HOPS OF IT, AT OR BELOW GUESSING. Asserted as the CEILING it is,
        // so anything that lifts it lands here as a failure and gets read.
        var tried = deeper.Sum(one => one.Asked);
        var got = deeper.Sum(one => one.Right);

        Assert.True(tried > 0, "no chain longer than two was asked about at all");
        Assert.True(got / (double)tried < result.Chance,
            $"chains past two hops beat chance -- the ceiling moved, read why: "
            + $"{got} of {tried}");
    }

    [Fact]
    public async Task The_role_channel_lifts_the_deep_chains_and_grouping_does_not()
    {
        // THE FIRST MEASUREMENT OF `Kind.Role` ON DATA NOBODY HERE GENERATED, and
        // the first at all through a front end rather than a hand-built occasion.
        //
        // BOTH ARMS WRITE THE SAME PAIR. Grouping puts a person beside the slot
        // code they fill and the pair lands under `With`; the role channel derives
        // the slot and the pair lands under `Fills`. So this is one mechanism
        // measured ON from a baseline that already works, which is the trap the
        // plan names -- not a mechanism measured against nothing.
        var arms = new List<(bool Roled, ClutrrResult Result)>();

        foreach (var roled in new[] { false, true })
        {
            using var run = new ClutrrRun(
                World(stories: 300, roled: roled), Fixture.Dials(stamina: 32.0), seed: 1);

            var result = await run.RunAsync();
            arms.Add((roled, result));

            output.WriteLine($"roled={roled,-5} {result}");
            foreach (var (hops, asked, right) in result.ByHops)
                output.WriteLine($"    {hops,2} hops: {right,3}/{asked}");
        }

        var grouped = arms[0].Result;
        var filled = arms[1].Result;

        // GROUPING CANNOT REACH PAST THE SECOND HOP. Asserted as the FLOOR it is.
        Assert.True(Deep(grouped) < grouped.Chance,
            $"the grouping baseline composed after all: {Deep(grouped)}");

        // AND THE ROLE CHANNEL CAN. This is the whole claim the cell was built on:
        // a count between two people is about those two people, and a count between
        // a relation's slots names nobody -- so it applies to people never seen
        // together.
        Assert.True(Deep(filled) > filled.Chance,
            $"the role channel did not beat chance on deep chains: {Deep(filled)}");

        Assert.True(Deep(filled) > Deep(grouped) * 4,
            $"the role channel did not clearly beat grouping on deep chains: "
            + $"{Deep(filled)} against {Deep(grouped)}");

        // AND IT IS NOT JUST LOUDER. A walk that answered more by saying more would
        // show up here, and it does not: it is less silent AND more right.
        Assert.True(filled.Silent < grouped.Silent);
        Assert.True(filled.Accuracy > grouped.Accuracy);
    }
}
