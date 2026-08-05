using OpenPlexus.Graph;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The first world in this project that somebody else designed.
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S ASK, 2026-08-03: STOP COMPARING ONLY AGAINST OUR OWN NUMBERS.</b>
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
    private static string Corpus
    {
        get
        {
            var corpus = Path.Combine(Tree.Repo(), "corpora", "tasks_1-20_v1-2", "en");

            Assert.True(Directory.Exists(corpus),
                $"the bAbI corpus is not at {corpus}. Fetch it with:\n"
                + "    bash corpora/fetch.sh");

            return corpus;
        }
    }

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
    private static WalkSettings Dials => Fixture.Dials(stamina: 8.0);

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
        // COMPARED AS SEQUENCES AND NOT AS ImmutableArray, which compares the
        // underlying array by reference and passes for nothing.
        Assert.Equal(
            new[] { "mary", "moved", "to", "the", "bathroom" }.Select(Babi.Of),
            read[0].Words.AsEnumerable());

        Assert.True(read[2].Asking);
        Assert.Equal("bathroom", read[2].Answer);
        Assert.Equal([Babi.Of("bathroom")], read[2].Answers.AsEnumerable());

        // THE SUPPORTING FACT IDS ARE DROPPED, and that is the whole ethic of
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

        // AND THE STORY CODE IS IN THE SENTENCE, which is what makes it an
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

            // THE MAJORITY-CLASS BASELINE IS THE ONE THAT MATTERS, and it is
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
    /// <b>FOUND BY RUNNING ALL TWENTY AND ASKING WHY SIX WERE EXACTLY NOUGHT.</b>
    /// Not nearly nought — <i>exactly</i>, on every question, which is the shape of
    /// a thing that cannot happen rather than a thing done badly.
    /// </para>
    /// <para>
    /// <b>19 IS HERE BECAUSE THE CHECK PUT IT HERE.</b> The list was written from
    /// the five tasks whose score was nought with nothing compound about them, and
    /// 19 was set aside as the compound case. It reaches none of its twelve answers
    /// either — a path like <c>n,w</c> is neither a word nor one answer — so it
    /// fails BOTH ways, and the honest count of what this design cannot express on
    /// this benchmark is six rather than five.
    /// </para>
    /// </remarks>
    private static readonly int[] Unspeakable = [6, 7, 10, 17, 18, 19];

    [Fact]
    public void Six_tasks_answer_with_a_word_the_corpus_never_shows()
    {
        // THE CEILING THIS ARCHITECTURE HAS, STATED AS A PROPERTY OF THE CORPUS SO
        // IT COSTS NO WALK TO CHECK. An answer here is a CODE THE WALK ARRIVED AT,
        // and a code enters the graph only by being observed in a sentence. `yes`,
        // `no`, `maybe` and the counting words are never in a sentence -- they
        // appear only in the answer column. So there is no node to arrive at, and
        // no budget, pricing or depth can conjure one.
        //
        // THIS IS NOT A BUG AND IT IS NOT TUNING. It is the price of answering by
        // ARRIVING somewhere: the system can only ever say what it has seen, and a
        // yes/no question asks it to produce a token the world does not contain.
        // SIX of the twenty are unreachable, so any mean over all twenty is really
        // a mean over fourteen with six structural zeroes dragging it -- which is
        // the difference between 0.2507 and 0.3582, and the reason to report both.
        foreach (var task in Enumerable.Range(1, 20))
        {
            var world = new Babi(World(task));

            var shown = world.Lines
                .SelectMany(line => line.Words)
                .ToHashSet();

            var reachable = world.Alphabet.Count(answer => shown.Contains(Babi.Of(answer)));

            output.WriteLine(
                $"task {task,2}: {reachable,2} of {world.Alphabet.Count,2} answers "
                + $"are words the corpus ever shows");

            if (Unspeakable.Contains(task))
                Assert.True(reachable == 0,
                    $"task {task} can now reach {reachable} of its answers, so the "
                    + "structural zero has an exception and the ceiling claim needs "
                    + "re-reading rather than re-asserting");
            else
                Assert.True(reachable > 0,
                    $"task {task} cannot reach ANY of its answers either, so the "
                    + "list above is incomplete and more of the benchmark is out of "
                    + "reach than it says");
        }
    }

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

    private static async Task<double> ScoreAsync(
        int task,
        WalkSettings dials,
        int seed,
        int span = 0,
        Accumulate ranking = Accumulate.Sum)
    {
        using var run = new BabiRun(World(task), dials, seed, span, ranking);
        return (await run.RunAsync(Sentences).ConfigureAwait(false)).Accuracy;
    }

    [Fact]
    public async Task Sender_buys_at_a_low_budget_what_receiver_needs_a_high_one_for()
    {
        // THE CHECK THAT SHOULD HAVE COME FIRST, AND IT CHANGES WHAT THE SENDER
        // RESULT MEANS. Two pricings compared at ONE stamina is a comparison of
        // the stamina: on  the receiver arm looked beaten until it was
        // given budget, and then it won both halves outright.
        //
        // Here receiver climbs with stamina toward sender rather than sitting
        // below it, so the lift is a BUDGET effect and not a better ranking. What
        // makes sender the right default anyway is the cost: a bAbI task uses a
        // few dozen words, so the graph is close to complete and traffic goes
        // roughly as fan-out to the power of depth. The budget that rescued
        // receiver on CLEVR cannot be paid here at all -- a single run at stamina
        // 64 did not finish in the time three whole sweeps took.
        var climbing = new List<double>();

        foreach (var stamina in new[] { 8.0, 16.0 })
        {
            using var probe = new BabiRun(
                World(16), Fixture.Dials(stamina: stamina), seed: 1);

            var seen = await probe.RunAsync(400);
            climbing.Add(seen.Accuracy);

            output.WriteLine($"stamina={stamina} receiver acc={seen.Accuracy:F4} msgs={seen.Messages}");
        }

        Assert.True(climbing[1] > climbing[0],
            $"receiver did not climb with budget, so the sender lift is a better "
            + $"ranking after all: {climbing[0]} to {climbing[1]}");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    public async Task Sender_pricing_is_the_one_arm_this_corpus_moves_under(int task)
    {
        // THE EXTERNAL EVIDENCE FOR A PROMOTION THE PLAN CALLS OVERDUE. Sender
        // pricing was invented for the tag experiment and has only ever been
        // measured on worlds built here; this is somebody else's corpus saying
        // the same thing, which is the whole reason for reading one.
        var arms = await Sweep.AcrossAsync(Repeats,
            ("receiver", seed => ScoreAsync(task, Dials, seed)),
            ("sender", seed => ScoreAsync(task, Dials with { Pricing = Pricing.Sender }, seed)));

        output.WriteLine(Sweep.Table(arms));

        var receiver = arms[0];
        var sender = arms[1];

        Assert.True(sender.Mean > receiver.Mean,
            $"sender pricing did not lift task {task}: {sender} against {receiver}");

        Assert.True(sender.Separation(receiver) > 3.0,
            $"the lift on task {task} is not separated: {sender} against {receiver}");
    }

    [Fact]
    public async Task Ranking_alone_does_not_move_this_corpus()
    {
        // THE COMPANION, AND WITHOUT IT THE TEST ABOVE ONLY SAYS "SOMETHING
        // MOVED". Agreement and doubt are the two dials that touch the ranking and
        // not the price. Agreement is inert here to six decimal places and doubt
        // shifts the score by a single question in two hundred and sixty-six --
        // against a third of the questions for sender. So the lift above is the
        // PRICE changing where routes die, and not a different mind about what
        // was found.
        var arms = await Sweep.AcrossAsync(Repeats,
            ("receiver", seed => ScoreAsync(1, Dials, seed)),
            ("agreement", seed =>
                ScoreAsync(1, Dials, seed, ranking: Accumulate.Agreement)),
            ("doubt", seed => ScoreAsync(1, Dials with { Doubt = 8.0 }, seed)),
            ("sender", seed => ScoreAsync(1, Dials with { Pricing = Pricing.Sender }, seed)));

        output.WriteLine(Sweep.Table(arms));

        var control = arms[0].Mean;

        Assert.All(arms.Skip(1).Take(2), ranking => Assert.True(
            Math.Abs(ranking.Mean - control) < 0.01,
            $"a ranking-only dial moved this corpus: {ranking} against {arms[0]}"));

        // AND THE PRICE DIAL IS AN ORDER OF MAGNITUDE PAST THEM, which is what
        // makes the comparison mean anything rather than saying the harness
        // cannot see a difference at all.
        Assert.True(arms[3].Mean - control > 0.1,
            $"the price dial did not separate from the ranking ones: {arms[3]}");
    }

    [Fact]
    public async Task The_window_costs_an_order_of_magnitude_and_scores_worse()
    {
        // THE REVIVAL CONDITION SAID "NEVER RUN WHERE IT WORKED", AND THIS IS
        // WHERE IT SHOULD HAVE WORKED. The window exists to give the graph
        // temporal edges; it measured null on snake, where what matters is what is
        // visible now. A corpus of sentences in the order somebody wrote them is
        // the opposite of that, and it is worse here rather than null.
        using var without = new BabiRun(World(1), Dials, seed: 1);
        using var with = new BabiRun(World(1), Dials, seed: 1, span: 2);

        var plain = await without.RunAsync(Sentences);
        var carried = await with.RunAsync(Sentences);

        output.WriteLine($"span=0 {plain}");
        output.WriteLine($"span=2 {carried}");

        Assert.True(carried.Accuracy < plain.Accuracy,
            $"the window did not cost accuracy: {carried.Accuracy} against {plain.Accuracy}");

        Assert.True(carried.Messages > plain.Messages * 5,
            $"the window was not the traffic it was measured to be: " +
            $"{carried.Messages} against {plain.Messages}");
    }

    [Fact]
    public async Task Edge_kinds_are_the_windows_revival_condition_and_this_is_where_it_is_run()
    {
        // THE ROW SAID THE REVIVAL CONDITION WAS EDGE KINDS, AND THIS IS THE
        // WORLD IT POINTED AT. A carried word was written into the same cell as
        // a word from the same sentence, so `follows` was added to `accompanies`
        // and the walk ranked the sum. Rhythm cannot measure this -- nothing
        // there is ever simultaneous, so every cell is temporal already and
        // splitting them is an isomorphism.
        // AND THE CONTROL MATTERS MORE THAN THE COMPARISON. Splitting one cell in
        // two halves the count in each, and the count IS the weight, and the
        // weight is the reciprocal of the hop price -- so kinds make every
        // temporal hop dearer whether or not they rank better. A walk that
        // scores higher on a third of the traffic may simply be a walk on a
        // smaller budget, and on a near-clique a smaller budget is known to
        // help. So the arm is swept against the budget it is confounded with.
        foreach (var stamina in new[] { 4.0, 6.0, 8.0 })
        {
            var dials = Fixture.Dials(stamina: stamina);

            using var fused = new BabiRun(World(1), dials, seed: 1, span: 2);
            using var split = new BabiRun(World(1), dials, seed: 1, span: 2, kinds: true);

            var together = await fused.RunAsync(Sentences);
            var apart = await split.RunAsync(Sentences);

            output.WriteLine($"stamina={stamina} kinds=off {together}");
            output.WriteLine($"stamina={stamina} kinds=on  {apart}");

            // THE LIFT SURVIVES THE CONTROL, at every budget swept. Lowering the
            // budget on the fused arm does not reproduce the split arm's score --
            // it makes it worse -- so this is a ranking effect and not the cost
            // artifact it is confounded with.
            Assert.True(apart.Accuracy >= together.Accuracy,
                $"kinds did not help at stamina {stamina}: " +
                $"{apart.Accuracy} against {together.Accuracy}");

            // AND IT IS CHEAPER, which is the opposite of what a bigger row
            // predicts. Separating the cells halves each count, so a temporal hop
            // costs more and the walk stops wandering down edges that meant two
            // things at once.
            Assert.True(apart.Messages < together.Messages,
                $"kinds did not pay for themselves at stamina {stamina}: " +
                $"{apart.Messages} against {together.Messages}");
        }

        // AND THE ROW IS WIDER FOR IT, which is the price named on `Tie` and the
        // one that meets the scaling wall.
        using var priced = new BabiRun(World(1), Dials, seed: 1, span: 2, kinds: true);
        using var plain = new BabiRun(World(1), Dials, seed: 1);

        var split2 = await priced.RunAsync(Sentences);
        var none = await plain.RunAsync(Sentences);

        output.WriteLine($"span=0 baseline  {none}");

        Assert.True(split2.Widest > none.Widest,
            $"the split did not widen the row: {split2.Widest} against {none.Widest}");

        // THE REFUTATION STANDS, NARROWED. Edge kinds recover much of what the
        // window cost and do not pay for it: carrying words across sentences is
        // still worse on this task than not carrying them at all. The revival
        // condition has been RUN rather than merely waited for.
        Assert.True(split2.Accuracy < none.Accuracy,
            $"the window is no longer a loss on bAbI: " +
            $"{split2.Accuracy} against {none.Accuracy}");
    }

    [Fact]
    public async Task A_closed_vocabulary_makes_a_graph_the_walk_cannot_compose_in()
    {
        // THE STRUCTURAL FINDING, AND IT IS WHY THE SCORES HERE ARE WHAT THEY
        // ARE. A bAbI task uses a few dozen words, so nearly every word co-occurs
        // with nearly every other and the graph is close to complete. A route then
        // spends its whole budget on breadth: at stamina 8 almost every chain that
        // comes back is one hop, and paying thirty times the messages for stamina
        // 16 does not change that -- it buys more of the same first hop.
        //
        // This is the same shape as the refuted `StepCost.Best` row, which was
        // measured on a 12-clique. The difference is that nobody chose this
        // clique: it is what a small closed vocabulary IS.
        using var run = new BabiRun(World(15), Dials, seed: 1);
        var result = await run.RunAsync(Sentences);

        output.WriteLine(result.ToString());

        var arrivals = result.ChainLengths.Values.Sum();
        var direct = result.ChainLengths.GetValueOrDefault(2);

        Assert.True(direct > arrivals * 0.9,
            $"the walk composed more than expected here, which is worth knowing: " +
            $"{direct} of {arrivals} arrivals were one hop");

        // AND THE DENSITY THAT CAUSES IT, so a future run on a corpus with a real
        // vocabulary can be told apart from this one at a glance.
        Assert.True(result.Edges > result.Nodes * 8,
            $"the graph is not the near-clique this was measuring: " +
            $"{result.Edges} edges over {result.Nodes} nodes");
    }
}
