using OpenPlexus.Codes;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The first world in this project that somebody else designed.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's ask, 2026-08-03: stop comparing only against our own numbers.</b>
/// Four worlds were built here by the same hands that built the mechanisms they
/// measure, so a good result on one of them can only say the mechanism does what
/// its author expected. The bAbI tasks were written by other people to isolate
/// capabilities nobody here chose, and they come with published baselines.
/// </para>
/// <para>
/// <b>The corpus is fetched, not vendored.</b> It is CC BY 3.0 and eleven
/// megabytes; <c>corpora/fetch.sh</c> gets it and <see cref="Corpus"/> says so
/// when it is not there.
/// </para>
/// </remarks>
public sealed class BabiTests(ITestOutputHelper output)
{
    /// <summary>
    /// Where the task files are, or a failure that says how to get them.
    /// </summary>
    /// <remarks>
    /// <b>Fails rather than skipping, which is this suite's standing rule.</b> A
    /// green run that quietly never asked the question is the failure every check
    /// here exists to avoid — see <see cref="Tree"/>.
    /// </remarks>
    private static string Corpus => Tree.Babi();

    private static BabiSettings World(int task, bool stories = true) => new()
    {
        Task = task, Corpus = Corpus, Stories = stories,
    };

    /// <summary>
    /// <b>Deep, because the tasks that suit this architecture are chains.</b>
    /// <i>Basic induction</i> reaches an answer through two intermediates, so a
    /// budget that only affords one hop would measure the corpus rather than the
    /// walk.
    /// </summary>
    // ---- what the corpus is, asserted rather than described -----------------

    [Fact]
    public void A_statement_is_its_words_and_a_question_carries_its_answer()
    {
        var read = Babi.Read(
        [
            "1 Mary moved to the bathroom.",
            "2 John went to the hallway.",
            "3 Where is Mary? \tbathroom\t1",
        ], stories: false);

        Assert.Equal(3, read.Count);

        Assert.Null(read[0].Answer);
        Assert.False(read[0].Asking);
        // Compared as sequences and not as ImmutableArray, which compares the
        // underlying array by reference and passes for nothing.
        Assert.Equal(
            new[] { "mary", "moved", "to", "the", "bathroom" }.Select(Babi.Of),
            read[0].Words.AsEnumerable());

        Assert.True(read[2].Asking);
        Assert.Equal("bathroom", read[2].Answer);
        Assert.Equal([Babi.Of("bathroom")], read[2].Answers.AsEnumerable());

        // The supporting fact IDS are dropped, and that is the whole ethic of
        // this world. They are the strong supervision the corpus authors ask
        // people to do without, and a route told which sentence to look at is
        // not doing the task.
        Assert.DoesNotContain(Babi.Of("1"), read[2].Words);
    }

    [Fact]
    public void A_story_begins_wherever_the_ids_reset()
    {
        var read = Babi.Read(
        [
            "1 Lily is a frog.",
            "2 Lily is green.",
            "1 Lily is a rhino.",
            "2 Lily is grey.",
        ], stories: true);

        Assert.Equal([0, 0, 1, 1], read.Select(line => line.Story));

        // And the story code is in the sentence, which is what makes it an
        // observed thing rather than an episode boundary. C4 forbids the second.
        Assert.Contains(Babi.Telling(0), read[0].Words);
        Assert.Contains(Babi.Telling(1), read[2].Words);
        Assert.NotEqual(Babi.Telling(0), Babi.Telling(1));
    }

    [Fact]
    public void Off_by_default_nothing_names_the_story()
    {
        var read = Babi.Read(["1 Lily is a frog."], stories: false);

        Assert.DoesNotContain(read[0].Words, code => code.Modality == Babi.Story);
    }

    [Fact]
    public void A_word_gets_the_same_code_in_every_process()
    {
        // string.GetHashCode IS RANDOMISED PER PROCESS, so a run built on it
        // would not reproduce itself -- which is the property fork 12 protects.
        // The literal is the answer, so a change to the hash fails here rather
        // than silently renumbering every code the world has ever emitted.
        Assert.Equal(Babi.Of("mary"), Babi.Of("Mary"));
        Assert.NotEqual(Babi.Of("mary"), Babi.Of("john"));
        Assert.Equal(2250482198492670294UL, Babi.Of("mary").Value);
    }

    [Fact]
    public void A_compound_answer_parses_as_more_than_one_word()
    {
        var read = Babi.Read(["1 What is Mary carrying? \tmilk,football\t2 3"], stories: false);

        Assert.Equal(2, read[0].Answers.Length);
        Assert.Equal([Babi.Of("milk"), Babi.Of("football")], read[0].Answers.AsEnumerable());
    }

    // ---- what the corpus says about itself ---------------------------------

    [Fact]
    public void Every_one_of_the_twenty_tasks_is_there_and_asks_something()
    {
        foreach (var task in Enumerable.Range(1, 20))
        {
            var world = new Babi(World(task));

            Assert.NotEmpty(world.Lines);
            Assert.Contains(world.Lines, line => line.Asking);
            Assert.NotEmpty(world.Alphabet);

            // The majority-class baseline is the one that matters, and it is
            // well above uniform on every task -- which is why a score is
            // reported against it and not against 1/alphabet.
            Assert.True(world.Commonest >= world.Chance,
                $"task {task}: commonest {world.Commonest} below chance {world.Chance}");

            output.WriteLine(
                $"task {task,2}: lines={world.Lines.Count,5} " +
                $"asked={world.Lines.Count(line => line.Asking),4} " +
                $"alphabet={world.Alphabet.Count,3} compound={world.Compound,4} " +
                $"commonest={world.Commonest:F4} chance={world.Chance:F4}");
        }
    }

    /// <summary>
    /// The tasks whose answer is a word the world never shows.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Found by running all twenty and asking why six were exactly nought.</b>
    /// Not nearly nought — <i>exactly</i>, on every question, which is the shape of
    /// a thing that cannot happen rather than a thing done badly.
    /// </para>
    /// <para>
    /// <b>19 is here because the check put it here.</b> The list was written from
    /// the five tasks whose score was nought with nothing compound about them, and
    /// 19 was set aside as the compound case. It reaches none of its twelve answers
    /// either — a path like <c>n,w</c> is neither a word nor one answer — so it
    /// fails BOTH ways, and the honest count of what this design cannot express on
    /// this benchmark is six rather than five.
    /// </para>
    /// </remarks>
    private static readonly int[] Unspeakable = [6, 7, 10, 17, 18, 19];

    // ---- what the graph does with it ---------------------------------------

    /// <summary>How much of a task file a measurement reads.</summary>
    /// <remarks>
    /// <b>Enough to put a few hundred questions behind every arm.</b> A first pass
    /// at 300 sentences gave task 16 thirty questions, where a five-question
    /// difference is two standard errors and reads exactly like a mechanism —
    /// which is the trap this project has already fallen into once.
    /// </remarks>
    private const int Sentences = 800;

    private const int Repeats = 5;

    // ---- THE WINDOW ON bAbI, and why both its tests are gone ---------------
    //
    // two tests stood here. `The_window_costs_an_order_of_magnitude_and_scores_worse`
    // compared a span of nought against two, and
    // `Edge_kinds_are_the_windows_revival_condition_and_this_is_where_it_is_run`
    // compared the fused cell against the split one at three budgets. Both arms
    // are gone: `Span` has a floor of one since 2026-08-05 and the kinds are
    // unconditional, so neither comparison can be built.
    //
    // What they established, and it is a cost this world is now paying knowingly:
    //
    //   * the window is a loss here. It exists to give the graph temporal edges
    //     and measured null on snake, where what matters is what is visible now.
    //     A corpus of sentences in the order somebody wrote them is the opposite
    //     of that, and it was WORSE here rather than null — for something over
    //     five times the traffic.
    //
    //   * Edge kinds recover much of it and do not pay for it. Splitting the cell
    //     ranked better AND cost fewer messages at every budget swept, which is
    //     the opposite of what a wider row predicts — but carrying words across
    //     sentences stayed worse than not carrying them at all. The revival
    //     condition was RUN rather than waited for, and the refutation survived it
    //     narrowed.
    //
    //   * THE CONTROL WAS THE POINT. Splitting one cell in two halves the count in
    //     each, the count IS the weight, and the weight is the reciprocal of the
    //     hop price — so kinds make every temporal hop dearer whether or not they
    //     rank better, and a walk scoring higher on a third of the traffic may
    //     simply be a walk on a smaller budget. Sweeping against stamina is what
    //     separated the two.
    //
    // SO bAbI is expected to score below its old baseline and the scoreboard floor
    // records that as a decision rather than absorbing it. The refutation row's
    // revival condition is unchanged and now load-bearing: something has to make a
    // carried edge worth its ROW, because there is no longer a way to decline it.


    /// <summary>
    /// Whether counting company recovers a word class on real English, with nothing learnt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The oldest result in distributional semantics, asked of this front end's codes: two
    /// words are alike where they keep the same company, and a word class is what that
    /// recovers. It is what an embedding gives a sequence model for free, and the reason to
    /// ask it here is that nothing in this repo has ever run <see cref="Alternating"/> on
    /// text — every reading it has is from a generated world whose statistics are clean.
    /// </para>
    /// <para>
    /// Task one is the cleanest case the corpus has. A line is <i>mary moved to the
    /// bathroom</i>, so a name never shares a line with a name and a place never with a
    /// place, which is the exclusion clause satisfied by the grammar rather than by luck.
    /// Both classes keep the same company by construction. If shared company recovers a word
    /// class anywhere it recovers it here, and a null result here is a null result for the
    /// idea rather than for the corpus.
    /// </para>
    /// <para>
    /// What would drop it, written before it was run: the two classes coming back mixed with
    /// each other or with the function words. The company of <i>the</i> and <i>to</i> is
    /// every content word in the corpus, so an unweighted overlap has an obvious way to
    /// decide that everything is alike — and the answer to that is to weight company by how
    /// surprising it is, which is not built.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_shared_company_recovers_a_word_class_on_real_english()
    {
        var babi = new Babi(World(task: 1));

        // One line is one moment, and the answer is left out because it is not said in the
        // line -- a code that only ever arrives as an answer keeps no company at all.
        var moments = babi.Lines
            .Select(line => (IReadOnlySet<Code>)new HashSet<Code>(line.Words))
            .ToList();

        // Built from the lines rather than from `Alphabet`, which is the ANSWER vocabulary
        // and holds six words here. A map that covers the answers only would print most of
        // the groups as question marks and read as a derivation that found nothing.
        var word = babi.Lines
            .Where(line => line.Text is not null)
            .SelectMany(line => Babi.Words(line.Text!))
            .Distinct()
            .ToDictionary(Babi.Of, one => one);

        var groups = Alternating.From(moments, company: 0.5, floor: 20);

        output.WriteLine($"{moments.Count} lines, {babi.Alphabet.Count} words in the alphabet");

        foreach (var group in groups.OrderByDescending(one => one.Count))
            output.WriteLine(
                $"  {group.Count,2}  "
                + string.Join(" ", group.Select(code => word.GetValueOrDefault(code, "?")).Order()));

        var names = new[] { "mary", "john", "sandra", "daniel" };
        var places = new[] { "kitchen", "bedroom", "bathroom", "hallway", "garden", "office" };

        var held = groups
            .Select(group => group.Select(code => word.GetValueOrDefault(code, "?")).ToHashSet())
            .ToList();

        var forNames = held.OrderByDescending(one => one.Count(names.Contains)).FirstOrDefault() ?? [];
        var forPlaces = held.OrderByDescending(one => one.Count(places.Contains)).FirstOrDefault() ?? [];

        output.WriteLine(
            $"names: {forNames.Count(names.Contains)} of {names.Length} in one group of "
            + $"{forNames.Count} | places: {forPlaces.Count(places.Contains)} of {places.Length} "
            + $"in one group of {forPlaces.Count}");

        Assert.True(forNames.Count(names.Contains) >= 3,
            $"the best group held {forNames.Count(names.Contains)} of the four names, so shared "
            + "company does not recover a word class on real English and the likeness half of "
            + "this design needs company WEIGHTED by how surprising it is");

        Assert.True(forPlaces.Count(places.Contains) >= 4,
            $"the best group held {forPlaces.Count(places.Contains)} of the six places");

        // And they must be DIFFERENT groups, because one group holding both is not two word
        // classes recovered -- it is the derivation deciding that everything is alike, which
        // is exactly what unweighted company is expected to do where the function words reach
        // every line.
        // And a guard written AFTER the reading rather than a bar written before it, in the
        // shape `ScalingTests` uses. What came back was four groups and every one of them a
        // word class: the four names exactly, the four motion verbs exactly, the six places
        // with `is` among them, and `to` against `where` -- which are the statement and
        // question markers, alternatives that never share a line and keep the same company.
        // `the` reached no group at all, being in every line and so co-occurring with
        // everything, which is the exclusion clause refusing a background word without
        // anything having to weight it.
        Assert.True(forNames.All(names.Contains),
            $"the names group came back as {string.Join(" ", forNames.Order())}, which is not "
            + "only names -- it held exactly the four when this was written");

        Assert.False(forNames.SetEquals(forPlaces),
            "the names and the places came back as one group, so what was recovered is that "
            + "every content word keeps the same company rather than two classes");
    }
}
