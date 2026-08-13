using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// English through the commitment learner, which nothing has ever done.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's prototype ordering puts text first and the containers after it</b>, and
/// this is the measurement that says whether the containers are worth building. The
/// fleet is already proven to learn across sockets; what is unproven is that words
/// teach this learner anything at all, and no arrangement of processes changes that.
/// </para>
/// <para>
/// <b>A scope is a set and a sentence is a sequence</b>, which is the whole question. The
/// span arms are the cheapest reading of what a bag costs at the grain of a statement, and
/// rung three is the answer at the grain of a word — this world speaks
/// <see cref="Recited"/>, so the precedences of each sentence reach every moment here.
/// </para>
/// </remarks>
public sealed class RecalledTests(ITestOutputHelper output)
{
    private static RecalledSettings World(
        int task, int span = 1, int withheld = 40, Predicting predicting = Predicting.Asked) =>
        new()
        {
            Corpus = Tree.Babi(), Task = task, Span = span,
            Withheld = withheld, Predicting = predicting,
        };

    /// <summary>
    /// The cast, the rooms and the props, <b>as an answer key and never as an input.</b>
    /// </summary>
    /// <remarks>
    /// <b>Nothing is ever told which words are names.</b> These exist so that what a
    /// statistic returns can be SCORED, exactly as the multiplexer's enumerated truth scores
    /// a rule without ever reaching the learner. <b>Verbs are deliberately absent</b>: a verb
    /// group is a real finding and writing a verb key after seeing one would be the
    /// experimenter deciding what counts as a category once the answer was on the screen.
    /// </remarks>
    private static readonly IReadOnlyList<KeyValuePair<string, IReadOnlySet<string>>> Key =
    [
        new("people", new HashSet<string>(StringComparer.Ordinal)
            { "mary", "john", "sandra", "daniel" }),
        new("places", new HashSet<string>(StringComparer.Ordinal)
            { "bathroom", "bedroom", "garden", "hallway", "kitchen", "office" }),
        new("props", new HashSet<string>(StringComparer.Ordinal)
            { "football", "apple", "milk" }),
    ];

    /// <summary>The moment as the bagged control sees it, which is every word once.</summary>
    private static IEnumerable<Code> Codify(Recited recited) =>
        new Joined(Joining.Bagged).Codify(recited).Order();

    private static (Recalled World, Trial<Recited> Trial, Brain Brain) Made(
        RecalledSettings settings, Joining joining = Joining.Bagged, int capacity = 2000,
        IReadOnlyList<IReadOnlySet<Code>>? categories = null, int seed = 1, int hops = 2,
        bool banded = false)
    {
        var brain = new Brain(new CommittingSettings { Capacity = capacity }, seed);
        var world = new Recalled(settings);

        return (
            world,
            new Trial<Recited>(world, new Joined(joining, categories, hops, banded), brain),
            brain);
    }

    /// <summary>
    /// The same words this corpus always handed over, with nothing said about where they
    /// stood.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The control for rung three</b>, and it is a front end because there is no dial. A
    /// machine turns whatever order it is given into precedences, always, and whether a
    /// sense can tell word order is a fact about the sense. <c>HandingTests</c> arranges its
    /// own arms the same way and for the same reason.
    /// </para>
    /// <para>
    /// <b>It delegates the codes rather than rebuilding them</b>, so the two arms cannot
    /// drift apart over what a moment holds. <see cref="Joined.Codify(Recited)"/> is
    /// <see cref="Joined.Codify(Asking)"/> of the same moment bagged, so this is exactly the
    /// reading every text number on this branch was taken under.
    /// </para>
    /// </remarks>
    private sealed class Unordered(Joining joining) : IQuantizer<Recited>
    {
        private readonly Joined _through = new(joining);

        /// <inheritdoc/>
        public byte Modality => _through.Modality;

        /// <inheritdoc/>
        public IReadOnlyCollection<Code> Codify(Recited observation) =>
            _through.Codify(observation);
    }

    [Fact]
    public void A_moment_is_the_question_and_as_much_of_the_story_as_the_span_allows()
    {
        var whole = new Recalled(World(task: 1, span: 0, withheld: 40));
        var last = new Recalled(World(task: 1, span: 1, withheld: 40));

        Assert.Equal(whole.Questions, last.Questions);
        Assert.Equal(whole.Outcomes, last.Outcomes);

        // The same questions in the same order expecting the same answers, so the only
        // thing the arm changes is how many words are in the room. An arm that also moved
        // the answer key would be two changes read as one.
        for (var one = 0; one < whole.Questions; one++)
        {
            var wide = whole.Next();
            var near = last.Next();

            Assert.Equal(wide.Outcome, near.Outcome);
            Assert.True(near.Seen.Bagged.Words.Count <= wide.Seen.Bagged.Words.Count);
        }

        output.WriteLine($"task 1: {whole.Questions} questions, {whole.Outcomes} answers");
        output.WriteLine($"vocabulary: {string.Join(" ", whole.Vocabulary)}");
        output.WriteLine($"commonest: {whole.Commonest:F3}, blind draw: {1.0 / whole.Outcomes:F3}");
    }

    /// <summary>
    /// How much of the held-out set the drawn stream already says word for word.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The withheld turns are never drawn and their moments are</b>, which is a caveat on
    /// the unseen number rather than a fault in the withholding. bAbI is generated
    /// from templates over a small cast, so two stories reach the same bag of words often
    /// — and where they do, a held-out question is one the population has answered before
    /// under a different name.
    /// </para>
    /// <para>
    /// <b>So this counts it instead of forbidding it.</b> Forbidding it would mean
    /// dropping the collisions from the examination, which is the experimenter deciding
    /// which questions count after seeing which ones repeat. The number goes beside the
    /// score, and a reading of the unseen accuracy has to be read next to it.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_withheld_question_can_be_word_for_word_one_that_was_asked()
    {
        var world = new Recalled(World(task: 1, withheld: 40));

        Assert.NotEmpty(world.Withheld);

        var kept = world.Withheld
            .Select(one => string.Join(",", Codify(one.Seen)))
            .ToList();

        var drawn = new HashSet<string>(StringComparer.Ordinal);
        for (var draw = 0; draw < world.Questions; draw++)
            drawn.Add(string.Join(",", Codify(world.Next().Seen)));

        var twinned = kept.Count(one => drawn.Contains(one));

        // Asserted as a range rather than a value, because what this guards is that the
        // number is READ. A silent nought would mean the examination is clean and a silent
        // hundred would mean it measures nothing, and both have to be visible.
        Assert.InRange(twinned, 0, kept.Count);

        output.WriteLine(
            $"{twinned} of {kept.Count} withheld moments appear word for word among the "
            + $"{drawn.Count} distinct moments drawn");
    }

    /// <summary>
    /// Text teaches this learner, on questions it was never asked.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The bar is the marginal and not the blind draw</b>, because the blind draw is too
    /// easy to clear. Six answers make chance a sixth and the commonest answer is a
    /// fifth, so a population that has learnt nothing but which word comes up most is
    /// already at the second number — and a check against the first would pass on it.
    /// </para>
    /// <para>
    /// <b>And the held-out number carries the bar rather than the drawn one.</b> A
    /// question drawn twenty times over is one a population may have memorised, and the
    /// twin count beside it says a chunk of the examination is word for word something
    /// already asked. What survives both is that it answers questions from stories it was
    /// never told.
    /// </para>
    /// </remarks>
    [Fact]
    public void Text_reaches_the_commitment_learner()
    {
        var (world, trial, brain) = Made(World(task: 1, span: 1));
        var tally = trial.Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);

        var unseen = Assert.IsType<Examined>(tally.Unseen);

        // Twice the marginal, which the reading clears by half again. Written under the
        // grid rather than over it: the drawn and unseen numbers came back near three
        // times `Commonest` and this is the room a default change is allowed to cost
        // before somebody has to look at it.
        Assert.True(unseen.Accuracy > 2.0 * world.Commonest,
            $"answers never asked scored {unseen.Accuracy:F3} against a marginal of "
            + $"{world.Commonest:F3} — text stopped reaching the learner");

        Assert.True(tally.Recent > 2.0 * world.Commonest,
            $"the drawn stream scored {tally.Recent:F3} against {world.Commonest:F3}");

        // The caveat printed beside the score and in the same test, because they were once
        // taken under different settings and quoted together -- a twin count read at one
        // span next to an accuracy read at another, which flatters the accuracy by exactly
        // the amount nobody could see. Two numbers that must be read together belong in one
        // fact, where no future reader can pair the wrong ones.
        var drawn = new HashSet<string>(StringComparer.Ordinal);
        for (var draw = 0; draw < world.Questions; draw++)
            drawn.Add(string.Join(",", Codify(world.Next().Seen)));

        var twinned = world.Withheld.Count(one =>
            drawn.Contains(string.Join(",", Codify(one.Seen))));

        output.WriteLine($"drawn      : {tally.Recent:F3}");
        output.WriteLine($"never asked: {unseen.Accuracy:F3} over {unseen.Asked}, "
            + $"{unseen.Silence:F3} silent");
        output.WriteLine($"marginal   : {world.Commonest:F3}, blind draw {1.0 / world.Outcomes:F3}");
        output.WriteLine($"held       : {brain.Held.Count} commitments, {brain.Held.Names.Count} names");
        output.WriteLine($"wanting    : {tally.Wanting:F3} of blamed rounds nothing separated");
        output.WriteLine($"twins      : {twinned} of {unseen.Asked} exam moments appear "
            + $"word for word among the {drawn.Count} distinct moments drawn");
    }

    /// <summary>
    /// The order the corpus wrote reaches the moment, as that sentence's own adjacent pairs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Armed rather than assumed, which is this repo's oldest trap.</b> A rung wired and
    /// unable to fire reads exactly like a rung that fired and bought nothing, and
    /// <c>Surprise</c> and <c>Abstain</c> were both found in that state after a long time
    /// looking correct. Rung three is inert wherever a front end reports no order, so the
    /// question of whether English reports any has to be a check rather than a reading of
    /// the code.
    /// </para>
    /// <para>
    /// <b>The expected pairs come from the words</b> rather than from the report, so
    /// the two have to agree about what the corpus said. A word said twice is at neither
    /// place — <see cref="Joined.Order(Recited)"/>'s own rule — so the pairs are the adjacent
    /// ones among the words this statement says once.
    /// </para>
    /// </remarks>
    [Fact]
    public void Word_order_reaches_a_moment_of_real_english()
    {
        var world = new Recalled(World(task: 1, span: 1));
        var front = new Joined(Joining.Bagged);

        var reported = 0;
        var pairs = 0;

        for (var draw = 0; draw < world.Questions; draw++)
        {
            var seen = world.Next().Seen;

            if (front.Order(seen) is not { } order) continue;

            reported++;

            var said = seen.Said[0];
            var once = said.Where(word => said.Count(other => other == word) == 1).ToList();

            var wanted = new List<Code>();
            for (var at = 0; at + 1 < once.Count; at++)
                wanted.Add(Sequenced.Of(once[at], once[at + 1]));

            Assert.Equal(wanted, [.. Sequenced.From(order)]);

            pairs += wanted.Count;
        }

        // EVERY question moment and not merely some, because a statement of one distinct
        // word would report nothing and this corpus writes none. A partial count here would
        // mean the rung fires on a subset nobody chose.
        Assert.Equal(world.Questions, reported);

        output.WriteLine($"order reported on {reported} of {world.Questions} moments");
        output.WriteLine($"precedences: {pairs / (double)reported:F2} a moment");
    }

    /// <summary>
    /// What the word order is worth, against the same words with the order thrown away.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The first time rung three is asked about English</b> rather than about a generated
    /// sentence. <see cref="Handing"/> proves the three ceilings by
    /// construction and answers whether the mechanism works; this asks whether a corpus
    /// somebody else wrote contains an order worth reading.
    /// </para>
    /// <para>
    /// <b>The held-out column is the one to read</b>, and the drawn one is the trap.
    /// Widening what a moment says buys the drawn score and sells the held-out one, measured
    /// twice already on this world under two unrelated mechanisms, and a precedence per
    /// adjacent pair is a widening whatever else it is.
    /// </para>
    /// <para>
    /// <b>What would drop the arm</b> is the held-out accuracy falling while the population
    /// grows, which is that same pattern arriving a third time.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_word_order_pays_on_real_english()
    {
        foreach (var task in new[] { 1, 2 })
        {
            foreach (var span in new[] { 1, 2 })
            {
                foreach (var ordered in new[] { false, true })
                {
                    var unseen = new List<double>();
                    var drawn = new List<double>();
                    var held = new List<int>();
                    var named = new List<int>();
                    var silent = new List<double>();

                    for (var seed = 1; seed <= 5; seed++)
                    {
                        var brain = new Brain(new CommittingSettings { Capacity = 2000 }, seed);
                        var world = new Recalled(World(task, span));

                        var trial = ordered
                            ? new Trial<Recited>(world, new Joined(Joining.Bagged), brain)
                            : new Trial<Recited>(world, new Unordered(Joining.Bagged), brain);

                        var tally = trial.Run(
                            rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);

                        unseen.Add(tally.Unseen?.Accuracy ?? 0.0);
                        silent.Add(tally.Unseen?.Silence ?? 0.0);
                        drawn.Add(tally.Recent);
                        held.Add(brain.Held.Count);
                        named.Add(brain.Held.Names.Count);
                    }

                    output.WriteLine(
                        $"task {task} span {span} {(ordered ? "ordered" : "bagged ")} | "
                        + $"unseen {unseen.Min():F3}-{unseen.Max():F3} "
                        + $"silent {silent.Min():F3}-{silent.Max():F3} | "
                        + $"drawn {drawn.Min():F3}-{drawn.Max():F3} | "
                        + $"held {held.Min(),5}-{held.Max(),5} "
                        + $"names {named.Min(),3}-{named.Max(),3}");
                }
            }
        }
    }

    /// <summary>
    /// It answers in English, which is the thing a number cannot show.
    /// </summary>
    /// <remarks>
    /// <b>The point is that it can be read and therefore disbelieved.</b> A score
    /// says four in five and never which four, so a population answering every question
    /// with the commonest word and a population that has learnt the task produce the same
    /// line of output. The transcript is where that stops being true.
    /// </remarks>
    [Theory]
    [InlineData(Predicting.Asked)]
    [InlineData(Predicting.Masked)]
    [InlineData(Predicting.Salient)]
    public void And_it_answers_in_words(Predicting predicting)
    {
        var (world, trial, brain) = Made(World(task: 1, span: 1, predicting: predicting));

        trial.Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);

        var said = new List<string>();

        for (var one = 0; one < world.Withheld.Count; one++)
        {
            var asked = world.Transcript[one];
            var moment = brain.Held.Moment(new HashSet<Code>(Codify(world.Withheld[one].Seen)));
            var vote = brain.Held.Predict(brain.Held.Firing(moment));

            // The outcome code back into a word, which is the only place the mapping runs
            // backwards. `Brain.Says` is a code per index and the world's alphabet is that
            // index, so a search over the alphabet is exact rather than a lookup that could
            // drift out of step with the thing it names.
            var answer = vote.Expects is not { } expects
                ? "(silent)"
                : world.Vocabulary.FirstOrDefault(
                    one_ => Brain.Says(world.Vocabulary.IndexOf(one_)) == expects) ?? "(unknown)";

            said.Add($"{(answer == asked.Answer ? " " : "x")} {asked.Story} | "
                + $"{asked.Question} -> {answer} (corpus says {asked.Answer})");
        }

        // IT ANSWERED SOMETHING, which is the one thing this has to establish for the
        // printing below to mean anything. What it answered is scored by the fact above.
        Assert.DoesNotContain(said, one => one.Contains("(unknown)", StringComparison.Ordinal));

        // What it reaches for, counted, because a score of exactly nought is a diagnosis
        // waiting to be read. An arm answering every question wrongly and never abstaining
        // is not guessing badly -- it is answering something else consistently, and which
        // something is the finding.
        var reached = said
            .Select(one => one.Split("-> ")[1].Split(' ')[0])
            .GroupBy(one => one, StringComparer.Ordinal)
            .OrderByDescending(one => one.Count())
            .Take(4);

        output.WriteLine($"{predicting}: {string.Join(", ", reached.Select(g => $"{g.Key} x{g.Count()}"))}");

        foreach (var line in said.Take(8)) output.WriteLine(line);
    }

    /// <summary>
    /// The coincidence code says what it claims, on a moment built by hand.
    /// </summary>
    /// <remarks>
    /// <b>The arm is worth nothing if the marker is misplaced</b>, and a
    /// front end that silently marked nothing would read as <i>the coincidence does not
    /// pay</i> — the same conclusion from a wire that was never connected. Two moments,
    /// one sharing a word and one not.
    /// </remarks>
    [Fact]
    public void The_coincidence_is_marked_only_where_there_is_one()
    {
        var matching = new Asking
        {
            Story = [new HashSet<Code> { Babi.Of("mary"), Babi.Of("garden") }],
            Question = new HashSet<Code> { Babi.Of("where"), Babi.Of("mary") },
        };

        var missing = matching with
        {
            Question = new HashSet<Code> { Babi.Of("where"), Babi.Of("john") },
        };

        Assert.DoesNotContain(Joined.Coincided, new Joined(Joining.Bagged).Codify(matching));
        Assert.Contains(Joined.Coincided, new Joined(Joining.Anonymous).Codify(matching));
        Assert.DoesNotContain(Joined.Coincided, new Joined(Joining.Anonymous).Codify(missing));

        // Named carries which word it was, so it is not the anonymous code under another
        // name — the whole difference between the two arms is this one assertion.
        Assert.DoesNotContain(Joined.Coincided, new Joined(Joining.Named).Codify(matching));
        Assert.Contains(
            new Code(Joined.Both, Babi.Of("mary").Value),
            new Joined(Joining.Named).Codify(matching));

        // And the absence is said out loud, which is the only arm that speaks when nothing
        // coincided. `Sundered` and `Coincided` are exclusive by construction.
        Assert.Contains(Joined.Sundered, new Joined(Joining.Either).Codify(missing));
        Assert.DoesNotContain(Joined.Sundered, new Joined(Joining.Either).Codify(matching));
        Assert.Contains(Joined.Coincided, new Joined(Joining.Either).Codify(matching));

        output.WriteLine($"matched: {new Joined(Joining.Either).Codify(matching).Count} codes");
        output.WriteLine($"missed : {new Joined(Joining.Either).Codify(missing).Count} codes");
    }

    /// <summary>
    /// A newer statement supersedes an older one about the same thing, and only that one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The mechanism asserted on a moment built by hand</b>, because a grid cannot tell a
    /// displacement rule that does nothing from one that does the wrong thing. Both come
    /// back as a score, and this repo has read an unwired mechanism as a refutation before.
    /// Three statements: Mary moves twice and John moves once.
    /// </para>
    /// <para>
    /// <b>And the two ends of the dial are the two controls</b>, which is the
    /// property that stops the arm being a free win. Keying on every word collapses it to
    /// the newest statement; keying on none collapses it to the bag.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_newer_statement_supersedes_an_older_one_about_the_same_thing()
    {
        var story = new Asking
        {
            // NEWEST FIRST, which is the order the world hands them over.
            Story =
            [
                new HashSet<Code> { Babi.Of("mary"), Babi.Of("to"), Babi.Of("garden") },
                new HashSet<Code> { Babi.Of("john"), Babi.Of("to"), Babi.Of("office") },
                new HashSet<Code> { Babi.Of("mary"), Babi.Of("to"), Babi.Of("kitchen") },
            ],
            Question = new HashSet<Code> { Babi.Of("where"), Babi.Of("mary") },
        };

        // `went` AND `to` ARE IN EVERY STATEMENT, so the story's own intersection makes
        // them background and `mary` a key with nothing told to the front end at all.
        var situated = new Joined(Joining.Distinguished).Codify(story);

        // Mary's old place is gone and her new one is there, which is the whole claim.
        Assert.Contains(Babi.Of("garden"), situated);
        Assert.DoesNotContain(Babi.Of("kitchen"), situated);

        // AND JOHN IS UNTOUCHED, which is what separates displacement from a narrower view.
        // A one-statement span would have taken his office as well.
        Assert.Contains(Babi.Of("office"), situated);
        Assert.Contains(Babi.Of("john"), situated);

        // And reading at the question's key takes Mary's newest and nothing else, which is a
        // different statement from the newest overall wherever somebody else spoke last.
        var addressed = new Joined(Joining.Addressed).Codify(story);

        Assert.Contains(Babi.Of("garden"), addressed);
        Assert.DoesNotContain(Babi.Of("kitchen"), addressed);
        Assert.DoesNotContain(Babi.Of("office"), addressed);
        Assert.DoesNotContain(Babi.Of("john"), addressed);

        output.WriteLine(
            $"bag {new Joined(Joining.Bagged).Codify(story).Count} "
            + $"| distinguished {situated.Count} | addressed {addressed.Count}");
    }

    /// <summary>
    /// Whether holding one state per thing dissolves the selection this learner cannot do.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The measured failure is a near-perfect reader and a hopeless selector</b>, and every
    /// arm so far has tried to help it select. A narrow view picks the sentence by hand
    /// and reaches the ceiling; a recency band hands the position over in the alphabet and
    /// buys about half of that. This arm does not help it select at all — it overwrites, so
    /// that by the time the bag is built there is one place for Mary in it and selecting is
    /// not required.
    /// </para>
    /// <para>
    /// <b>The kill condition, written before the arm ran</b>: if no setting of the dial beats
    /// <see cref="Joining.Recent"/> at the whole story, drop it. Displacement would then
    /// be buying nothing a position code does not already buy, and the situation model would
    /// be answering a question this world does not ask. Beating the BAG is not enough — the
    /// bottom of this dial is a one-statement span, so an arm that only beat the bag would
    /// be reporting the span arm under a new name.
    /// </para>
    /// <para>
    /// <b>And the capacity is an axis</b>, for the reason the recency grid found. That arm's
    /// gain evaporated as the population was allowed to grow, which is what said the extra
    /// alphabet was being spent memorising. This one REMOVES codes rather than adding them,
    /// so if it is real its gain should go the other way — and a single capacity could not
    /// tell.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_a_situation_dissolves_the_selection()
    {
        foreach (var capacity in new[] { 2000, 8000 })
        {
            // THE TWO CONTROLS FIRST, so the row they have to beat is printed in the same
            // grid rather than cited from another one taken under other dials.
            foreach (var joining in new[] { Joining.Bagged, Joining.Recent })
                Row(capacity, joining);

            // The displacement arm, which keys on recency and has no dial because the
            // story supplies its own background.
            Row(capacity, Joining.Distinguished);

            // And the one the ceiling says should win outright. Pre-registered: it hands
            // over one statement with a ceiling of one, and this learner is measured at
            // ninety-nine per cent of its ceiling wherever it is handed one statement — so
            // anything short of the nineties says the reader finding was conditional on
            // something nobody has named.
            Row(capacity, Joining.Addressed);
        }

        void Row(int capacity, Joining joining)
        {
            var (world, trial, brain) = Made(World(task: 1, span: 0), joining, capacity);

            var tally = trial.Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);
            var unseen = tally.Unseen;

            output.WriteLine(
                $"cap {capacity,4} {joining,-13} | "
                + $"exam {unseen?.Accuracy ?? 0.0:F3} silent {unseen?.Silence ?? 0.0:F3} | "
                + $"own {tally.Recent:F3} | marginal {world.Commonest:F3} | "
                + $"held {brain.Held.Count,5} names {brain.Held.Names.Count,4} "
                + $"wanting {tally.Wanting:F3}");
        }
    }

    /// <summary>
    /// Whether naming the coincidence between a question and a story is what was missing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The control is in the file</b>, which is the only reason the other two mean
    /// anything. <see cref="Joining.Bagged"/> is every reading taken before this
    /// existed, so the three run the same world, the same seed and the same brain and
    /// differ in one call.
    /// </para>
    /// <para>
    /// <b>And the two arms separate a lookup from a variable.</b>
    /// <see cref="Joining.Named"/> keeps which word was shared and reaches one rule per
    /// person per place; <see cref="Joining.Anonymous"/> throws the identity away and
    /// reaches one rule per place, which is what a variable buys. If they come back level
    /// the identity was never the cost.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_naming_the_coincidence_pays()
    {
        foreach (var task in new[] { 1, 2 })
        {
            foreach (var joining in new[] { Joining.Bagged, Joining.Named, Joining.Anonymous, Joining.Either })
            {
                var (world, trial, brain) = Made(World(task, span: 1), joining);
                var tally = trial.Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);
                var unseen = tally.Unseen;

                output.WriteLine(
                    $"task {task} {joining,-9} | "
                    + $"drawn {tally.Recent:F3} unseen {unseen?.Accuracy ?? 0.0:F3} "
                    + $"silent {unseen?.Silence ?? 0.0:F3} | commonest {world.Commonest:F3} | "
                    + $"held {brain.Held.Count,5} names {brain.Held.Names.Count,4} "
                    + $"wanting {tally.Wanting:F3}");
            }
        }
    }

    /// <summary>
    /// Whether recency as a CODE recovers what a narrow view was throwing away.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The ceiling says this learner reads near-perfectly and selects hopelessly</b>,
    /// and this is the first mechanism aimed at the second half. Shown one statement it
    /// answers all but a hair of what is present; shown the whole story, where everything
    /// is present, it takes under a third. What it cannot do is say WHICH sentence, because
    /// a scope is a subset test over a set and a set has no positions.
    /// </para>
    /// <para>
    /// <b>The prediction, written before the arm ran</b>: banded at the whole story it should
    /// reach at least what the one-statement view reached. The narrow view wins by
    /// throwing information away, and a band hands the same information over while keeping
    /// the rest — so anything short of that says the learner cannot use recency even when
    /// it is spelled out in its own alphabet, which is a finding about the LEARNER and not
    /// about the front end.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_recency_as_a_code_recovers_the_selection()
    {
        // And the capacity is an axis rather than a constant, because the first reading of
        // this arm came back pinned at the cap. Banding multiplies the alphabet, so the
        // banded population saturated where the control sat at a twentieth of the limit --
        // and a comparison where one arm is against a wall and the other is not measures
        // the wall. Both arms get both caps, which is the only way to tell which it was.
        foreach (var capacity in new[] { 2000, 8000 })
        {
            foreach (var span in new[] { 0, 2 })
            foreach (var joining in new[] { Joining.Bagged, Joining.Recent })
            {
                var (world, trial, brain) = Made(World(task: 1, span: span), joining, capacity);
                var tally = trial.Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);
                var unseen = tally.Unseen;

                output.WriteLine(
                    $"cap {capacity,4} span {span} {joining,-7} | exam {unseen?.Accuracy ?? 0.0:F3} "
                    + $"silent {unseen?.Silence ?? 0.0:F3} | own {tally.Recent:F3} | "
                    + $"marginal {world.Commonest:F3} | held {brain.Held.Count,5} "
                    + $"names {brain.Held.Names.Count,4} wanting {tally.Wanting:F3}");
            }
        }
    }

    /// <summary>
    /// How often the answer is in the room at all — <b>the ceiling a score has to be read
    /// against</b>, and without it no number here means anything.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Without a ceiling, a poor learner and a poor view read alike.</b> At one statement of span the moment is the last thing said and the
    /// question, so where the last statement is about somebody else the answering word is
    /// not present — and nothing the population could ever hold would put it there. That
    /// share is a fact about the WORLD and the span, decided before any learning happens.
    /// </para>
    /// <para>
    /// <b>It is a ceiling rather than a target, and it is generous.</b> Being present is
    /// necessary and nowhere near sufficient — two places in the room and no way to choose
    /// clears this bar and answers wrongly half the time. So a learner AT the ceiling is
    /// doing everything the view allows, and a learner far under it is leaving something on
    /// the table that better learning could take.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_the_span_makes_answerable_at_all()
    {
        foreach (var span in new[] { 0, 1, 2, 3 })
        {
            var world = new Recalled(World(task: 1, span: span));

            var reachable = 0;

            for (var one = 0; one < world.Withheld.Count; one++)
            {
                var moment = new HashSet<Code>(Codify(world.Withheld[one].Seen));

                if (moment.Contains(Babi.Of(world.Transcript[one].Answer))) reachable++;
            }

            var ceiling = reachable / (double)world.Withheld.Count;

            // Never nought, which would mean nobody could answer this exam at all. One is
            // a legitimate reading and the important one: the whole story always contains
            // its own answer, so a span that shows everything has no information ceiling
            // whatever -- and the score there is entirely about the learner and the bag.
            Assert.InRange(ceiling, 0.01, 1.0);

            output.WriteLine(
                $"span {span,-2} | answer present in {ceiling:F3} of {world.Withheld.Count} "
                + $"exam moments, over {world.Questions} drawn");
        }
    }

    /// <summary>
    /// Whether a second supporting fact is out of REACH or merely unlearnt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The distinction the grid cannot draw, and it costs no learning.</b> Reading at the
    /// question's key answers task one outright and lands on the base rate at task two, and
    /// a score alone cannot say whether the statement it retrieved HELD the answer. Where
    /// the question names the apple and the apple's newest statement says who picked it up,
    /// the answering word was never in the room and no learner could have found it.
    /// </para>
    /// <para>
    /// <b>Which is the whole shape of what is missing.</b> One hop of retrieval reaches the
    /// statement the question names; a second hop would have to read at a key that FIRST
    /// reading supplied. A ceiling near nought here says the arm is at its ceiling again and
    /// the fault is the view, exactly as it was at every width on task one.
    /// </para>
    /// <para>
    /// <b>And it is not a bound on the score</b>, which this reading is the first to show. An
    /// outcome is an index rather than a word in the room, so a population collects the base
    /// rate by expecting the commonest answer with nothing present to read — and where the
    /// marginal is above this column, a score SITS ABOVE IT with no fault anywhere. Read the
    /// two together or a working arm looks broken.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_a_second_supporting_fact_is_even_in_the_room()
    {
        foreach (var task in new[] { 1, 2, 3 })
        {
            var world = new Recalled(World(task: task, span: 0));
            var asked = new Joined(Joining.Addressed);

            var reachable = 0;

            for (var one = 0; one < world.Withheld.Count; one++)
            {
                var moment = new HashSet<Code>(asked.Codify(world.Withheld[one].Seen));

                if (moment.Contains(Babi.Of(world.Transcript[one].Answer))) reachable++;
            }

            output.WriteLine(
                $"task {task} addressed | answer present "
                + $"{reachable / (double)world.Withheld.Count:F3} of {world.Withheld.Count}");
        }
    }

    /// <summary>
    /// Whether reading at the question's key survives needing more than one statement.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The caveat on the perfect score, made into a measurement.</b> Task one is named
    /// <i>single supporting fact</i>, so a front end that retrieves one statement by the
    /// question's key is close to that task's own definition — and a grid that only ever ran
    /// there would be reporting the corpus's structure as the arm's result. Tasks two and
    /// three need two statements and three.
    /// </para>
    /// <para>
    /// <b>Pre-registered, and the failure is the informative outcome.</b> One statement
    /// cannot carry two supporting facts, so this should fall hard at task two — and where
    /// it falls to is the number worth having: down to the bag says retrieval buys nothing
    /// without chaining, and part of the way says one hop of it is already worth something.
    /// </para>
    /// <para>
    /// <b>And the bag runs beside it at every task</b>, because the tasks are not equally
    /// hard and a score that fell because the corpus got harder would read exactly like an
    /// arm that stopped working.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_addressing_survives_more_than_one_supporting_fact()
    {
        foreach (var task in new[] { 1, 2, 3 })
        foreach (var joining in new[] { Joining.Bagged, Joining.Addressed })
        {
            var (world, trial, brain) = Made(World(task: task, span: 0), joining);
            var tally = trial.Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);
            var unseen = tally.Unseen;

            output.WriteLine(
                $"task {task} {joining,-9} | exam {unseen?.Accuracy ?? 0.0:F3} "
                + $"silent {unseen?.Silence ?? 0.0:F3} | own {tally.Recent:F3} | "
                + $"marginal {world.Commonest:F3} | held {brain.Held.Count,5} "
                + $"names {brain.Held.Names.Count,4} wanting {tally.Wanting:F3}");
        }
    }

    /// <summary>
    /// What displacement throws away, before anything has learnt anything.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The sharper instrument, and it costs no learning at all.</b> A grid can only say
    /// that an arm scored badly; it cannot say whether the rule dropped the wrong statement
    /// or the learner failed to use the right one. This asks the question directly — after
    /// displacement, is the answering word still in the room? — and it is decided by the
    /// front end alone, on the withheld set, before a single commitment exists.
    /// </para>
    /// <para>
    /// <b>So the two columns together are the whole verdict on the rule.</b> The bag always
    /// contains its own answer, so a ceiling under one is displacement destroying something
    /// it should have kept. What makes a rule GOOD is throwing a great deal away while that
    /// ceiling holds — the same reading `Span` gets, from a mechanism that is allowed to
    /// keep more than one sentence.
    /// </para>
    /// <para>
    /// <b>And the three rows are one comparison rather than three readings.</b> Displacement
    /// keyed on recency, retrieval keyed on the question, and a blind draw matched on
    /// budget — so an arm that keeps less AND answers more than the blind one has a key
    /// doing real work, and an arm that beats displacement on both columns at a smaller
    /// budget is reaching something recency cannot.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_displacement_throws_away_before_anything_learns()
    {
        var world = new Recalled(World(task: 1, span: 0));

        var bagged = new Joined(Joining.Bagged);
        var whole = 0;

        for (var one = 0; one < world.Withheld.Count; one++)
            whole += bagged.Codify(world.Withheld[one].Seen).Count;

        var told = new Joined(Joining.Distinguished);

        var found = 0;
        var held = 0;

        for (var one = 0; one < world.Withheld.Count; one++)
        {
            var moment = new HashSet<Code>(told.Codify(world.Withheld[one].Seen));

            held += moment.Count;

            if (moment.Contains(Babi.Of(world.Transcript[one].Answer))) found++;
        }

        output.WriteLine(
            $"distinguished | answer present {found / (double)world.Withheld.Count:F3} | "
            + $"kept {held / (double)whole:F3} of the bag");

        // And the arm that reads the store at the key the question supplies, in the same
        // two columns. It keeps ONE statement, so its budget is the narrowest here — and if
        // its ceiling is high while its budget is small, that is the pair no recency rule
        // has managed and the whole reason fork 88 is worth taking.
        var asked = new Joined(Joining.Addressed);

        var aimed = 0;
        var cost = 0;

        for (var one = 0; one < world.Withheld.Count; one++)
        {
            var moment = new HashSet<Code>(asked.Codify(world.Withheld[one].Seen));

            cost += moment.Count;

            if (moment.Contains(Babi.Of(world.Transcript[one].Answer))) aimed++;
        }

        output.WriteLine(
            $"addressed     | answer present {aimed / (double)world.Withheld.Count:F3} | "
            + $"kept {cost / (double)whole:F3} of the bag");

        // The dominance put in the test, because it is the reading the whole arc turns on.
        // Reading the store at the key the question supplies keeps LESS than the narrowest
        // recency rule and loses no answer at all, which no displacement setting managed at
        // any budget. A recency rule is at its ceiling only where it keeps one statement,
        // and this keeps one statement AND has no ceiling to be short of.
        Assert.Equal(world.Withheld.Count, aimed);
        Assert.True(cost < held, $"addressed kept {cost} against displacement's {held}");

        // And the control that says whether the key is doing anything at all, matched
        // question by question on how many words survived. Both columns moving together is
        // what removal AT A RATE looks like, and a rate is what dropping statements blindly
        // gives — so without this the two rules above cannot be told from a coin.
        //
        // It lives here rather than in `Joining` ON PURPOSE. A control arm shipped in the
        // front end would be an arm to delete later; a control computed in the test that
        // reads it costs nothing and cannot be mistaken for a mechanism.
        var draw = new Random(1);
        var blind = 0;
        var spent = 0;

        for (var one = 0; one < world.Withheld.Count; one++)
        {
            var asking = world.Withheld[one].Seen.Bagged;
            var budget = told.Codify(asking).Count;

            var shuffled = asking.Story.OrderBy(_ => draw.Next()).ToList();
            var moment = new HashSet<Code>(asking.Question);

            foreach (var statement in shuffled)
            {
                if (moment.Count >= budget) break;
                moment.UnionWith(statement);
            }

            spent += moment.Count;

            if (moment.Contains(Babi.Of(world.Transcript[one].Answer))) blind++;
        }

        var guessing = blind / (double)world.Withheld.Count;
        var keyed = found / (double)world.Withheld.Count;

        output.WriteLine(
            $"blind         | answer present {guessing:F3} | "
            + $"kept {spent / (double)whole:F3} of the bag");

        // The reading put in the test rather than in a commit message. A key that beat the
        // control on the ceiling while keeping MORE would be buying its advantage with
        // budget; this keeps strictly less and answers strictly more, which is the only
        // shape that says the choice of what to drop is doing the work.
        Assert.True(keyed > guessing, $"keyed {keyed:F3} did not beat blind {guessing:F3}");
        Assert.True(held <= spent, $"keyed kept {held} against blind {spent}");
    }

    /// <summary>
    /// Which objective grows the population that answers best — <b>John's question</b>, and
    /// one the field cannot answer for a learner shaped like this.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The examination does not move when the objective does</b>, which is the whole of the
    /// design. An objective scored on its own target is unfalsifiable — a next-word arm
    /// hits next words and says nothing about understanding. So every arm predicts a word
    /// from one vocabulary, whole stories are held back from all four alike, and the same
    /// withheld questions are put to whatever each one grew.
    /// </para>
    /// <para>
    /// <b>So three of the four sit an exam they never trained for</b>, which is
    /// precisely the transfer question: did it learn the language, or this examination? A
    /// masked arm has never seen a question in its life.
    /// </para>
    /// <para>
    /// <b>And the drawn column is not comparable across arms</b>, and is printed anyway.
    /// Each one draws a different stream, so its trailing accuracy is against its own
    /// skew — it says whether an arm learnt ITS OWN task, which is what separates <i>the
    /// objective is hopeless</i> from <i>the objective works and does not transfer</i>.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Which_objective_grows_the_best_population()
    {
        foreach (var predicting in new[]
            { Predicting.Asked, Predicting.Masked, Predicting.Next, Predicting.Mixed, Predicting.Salient })
        {
            var (world, trial, brain) = Made(World(task: 1, predicting: predicting));
            var tally = trial.Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);
            var unseen = tally.Unseen;

            output.WriteLine(
                $"{predicting,-7} | exam {unseen?.Accuracy ?? 0.0:F3} "
                + $"silent {unseen?.Silence ?? 0.0:F3} over {unseen?.Asked ?? 0} | "
                + $"own task {tally.Recent:F3} over {world.Questions} moments | "
                + $"marginal {world.Commonest:F3} draw {1.0 / world.Outcomes:F3} | "
                + $"held {brain.Held.Count,5} names {brain.Held.Names.Count,4} "
                + $"wanting {tally.Wanting:F3}");
        }
    }

    /// <summary>
    /// What a bag of words costs, over two tasks and three doses of recency.
    /// </summary>
    /// <remarks>
    /// <b>A grid rather than a check, so it prints and asserts nothing.</b> <c>Span</c> is
    /// the crudest possible dose of sequence, at the grain of a whole statement, so this is
    /// the reading that says what recency alone is worth.
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_bag_of_words_costs()
    {
        foreach (var task in new[] { 1, 2 })
        {
            foreach (var span in new[] { 0, 1, 2, 3 })
            {
                var (world, trial, brain) = Made(World(task, span));
                var tally = trial.Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);
                var unseen = tally.Unseen;

                output.WriteLine(
                    $"task {task} span {span,-2} | "
                    + $"drawn {tally.Recent:F3} unseen {unseen?.Accuracy ?? 0.0:F3} "
                    + $"silent {unseen?.Silence ?? 0.0:F3} | "
                    + $"commonest {world.Commonest:F3} chance {1.0 / world.Outcomes:F3} | "
                    + $"held {brain.Held.Count,5} names {brain.Held.Names.Count,4} "
                    + $"wanting {tally.Wanting:F3}");
            }
        }
    }

    /// <summary>
    /// What the chain SCORES, against the span-matched bag rather than against one hop.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The control is the whole design of this grid.</b> A chain of N statements is also a
    /// moment of N statements, and widening a moment is already known here to buy the drawn
    /// score and sell the held-out one. The ceiling probe says about half of what the hops
    /// reach is recency, so an arm read against one hop reports twice what it is worth — the
    /// bag at the same span is what says which mechanism paid.
    /// </para>
    /// <para>
    /// <b>And the ceiling is not a bound on the score</b>, which this exam has already caught
    /// once. An outcome is an index rather than a word in the room, so a population
    /// collects the base rate expecting the commonest answer with nothing present to read —
    /// and task three scores above its own one-hop ceiling for exactly that reason. Read the
    /// marginal beside every cell or a working arm looks broken and a dead one looks fine.
    /// </para>
    /// <para>
    /// <b>The ceiling rose two to three times; the score did not follow</b>, which is the
    /// result. The chain wins its span-matched control on exactly one cell of six and
    /// loses or ties everywhere else, and the plain bag at span three is the best arm on both
    /// tasks that need more than one fact. Retrieval was necessary and is not sufficient.
    /// </para>
    /// <para>
    /// <b>And where one statement is enough, a second one is damage</b> — a quarter of a perfect
    /// score. Task one falls from answering everything to answering three in four the
    /// moment a hop it does not need is taken. That is the sharpest reading here and it is
    /// what the assertion below holds.
    /// </para>
    /// <para>
    /// <b>Because the bottleneck moved from the room to the bag</b>, which this doc already
    /// predicted in another place. Every arm reading more than one statement pins the
    /// population at its capacity, the drawn score climbs while the held-out one does not, and
    /// silence appears from nowhere — <i>widening the moment buys the drawn score and sells
    /// the held-out one</i>, reproduced exactly by a mechanism built for a different reason.
    /// </para>
    /// <para>
    /// <b>And the cause is that a scope is a set</b>, so two statements in the room is the
    /// SELECTION problem again, whole. Nothing in a bag says which statement a word came
    /// from, so a chain that fetched the right sentence hands the matcher no way to use it —
    /// the same sentence this doc writes about a situation model.
    /// </para>
    /// <para>
    /// <b>And banding by hop confirms it</b>, which is the one place a diagnosis here has been
    /// paid out rather than argued. Tagging each word with which hop found it makes
    /// <i>the statement the question named</i> and <i>the one that named</i> different codes,
    /// and the two-fact task goes from a fifth to a third — the best arm on it, half again
    /// what the chain alone reached and clear of its marginal, where nothing else was.
    /// </para>
    /// <para>
    /// <b>So the chain wanted rung three and not a deeper hop</b>, and the retrieval was only ever
    /// half a mechanism. Silence collapses with it, from a sixth of the exam to a
    /// two-hundredth, because an arm that can tell its statements apart stops abstaining on
    /// the rounds that held two.
    /// </para>
    /// <para>
    /// <b>And it reaches two facts and not three</b>, which is the honest edge. The three-fact
    /// task sits level with the plain bag and under its own marginal at every depth, so what
    /// is measured here is a mechanism that scales to the task it was aimed at and stops.
    /// </para>
    /// <para>
    /// <b>And the band cap is not what stops it</b>, which is worth saying because it is the
    /// obvious suspect. <see cref="Joined.Bands"/> is three and three hops use bands nought, one
    /// and two, all distinct — the cap first bites at a FOURTH hop. What separates the two
    /// tasks is CONVERSION: the two-fact task turns near three quarters of its answer-present
    /// ceiling into score and the three-fact task under half.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_chain_scores_against_a_bag_of_the_same_width()
    {
        foreach (var task in new[] { 1, 2, 3 })
        {
            var scored = new Dictionary<string, double>(StringComparer.Ordinal);

            foreach (var (label, settings, joining, hops) in new (string, RecalledSettings, Joining, int)[]
            {
                ("bag span 1  ", World(task, span: 1), Joining.Bagged, 1),
                ("bag span 2  ", World(task, span: 2), Joining.Bagged, 1),
                ("bag span 3  ", World(task, span: 3), Joining.Bagged, 1),
                ("addressed   ", World(task, span: 0), Joining.Addressed, 1),
                ("chained x2  ", World(task, span: 0), Joining.Chained, 2),
                ("chained x3  ", World(task, span: 0), Joining.Chained, 3),
                ("banded x2   ", World(task, span: 0), Joining.Chained, 2),
                ("banded x3   ", World(task, span: 0), Joining.Chained, 3),
            })
            {
                var (world, trial, brain) = Made(
                    settings, joining, hops: hops, banded: label.StartsWith("banded", StringComparison.Ordinal));
                var tally = trial.Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);

                scored[label.Trim()] = tally.Unseen?.Accuracy ?? 0.0;

                output.WriteLine(
                    $"task {task} {label} | exam {tally.Unseen?.Accuracy ?? 0.0:F3} "
                    + $"silent {tally.Unseen?.Silence ?? 0.0:F3} | own {tally.Recent:F3} | "
                    + $"marginal {world.Commonest:F3} | held {brain.Held.Count,5} "
                    + $"wanting {tally.Wanting:F3}");
            }

            // A hop not needed is damage, and task one is where that is unmistakable: the
            // addressed arm answers everything and every deeper arm answers less. This is
            // asserted rather than printed because it is the finding that stops a future
            // session shipping the chain as a default on a ceiling reading alone.
            if (task == 1)
            {
                Assert.Equal(1.0, scored["addressed"]);
                Assert.True(
                    scored["chained x2"] < scored["addressed"],
                    $"a second hop cost nothing on task 1: {scored["chained x2"]:F3}");

                Assert.True(
                    scored["banded x2"] > scored["chained x2"],
                    "banding did not recover what the unioned chain gave away");
            }

            // And on the two-fact task the banded chain is the best arm there is, which is the
            // claim worth failing the build over. Every other arm here is a bag of some width
            // or a chain the matcher cannot read, and one of those leading would mean the
            // mechanism is width and never the hop.
            if (task == 2)
                Assert.True(
                    scored["banded x2"] > scored.Where(one => one.Key != "banded x2").Max(one => one.Value),
                    $"the banded chain did not lead on task 2: {scored["banded x2"]:F3}");
        }
    }

    /// <summary>
    /// What the learner converts of the signal real English has and bAbI has not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The first reading on a corpus that is not templated</b>, and it is only possible
    /// because of what <c>PrimerTests.Is_reading_real_english_predictive_at_all</c>
    /// found. On bAbI, selecting the informative words IS selecting the
    /// unpredictable ones — a held-out predictor reaching 89% of that corpus's ceiling
    /// elsewhere scores 0.170 on its rooms against a blind draw of 0.173. On Tatoeba
    /// the implication inverts: informative words 59x their blind draw, function words
    /// 1.2x. So a gate can pay here and could never have paid there.
    /// </para>
    /// <para>
    /// <b>WHICH MAKES <see cref="Predicting.Salient"/> AGAINST <see cref="Predicting.Masked"/>
    /// A real comparison for the first time.</b> The two were compared on bAbI and the
    /// gate could only ever have lost there, because the words it selects were drawn at
    /// random over a template. This is the same pair on a text where the gate is
    /// selecting the words that carry something.
    /// </para>
    /// <para>
    /// <b>And the examination is the objective on sentences never read</b>, which is forced
    /// rather than chosen. Plain English writes no questions, so there is nothing
    /// else a withheld sentence could be asked. That makes this arm's accuracy NOT
    /// comparable with any bAbI reading in this file — a different exam is a different
    /// number, and only the two English arms may be read against each other.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_reading_real_english_converts()
    {
        var scored = new Dictionary<Predicting, double>();
        var saturated = new List<string>();

        const int Capacity = 20_000;

        foreach (var predicting in new[] { Predicting.Masked, Predicting.Salient })
        {
            // THE SIZING `Whether_capacity_binds_on_english` proved unsaturated, rather than
            // a bigger one. Two arms have now been compared at a cap they both sat ON --
            // 8,000 against 2,855 words, then 30,000 against 4,648 -- and a saturated
            // population compares nothing however large it is. The grid says 2,855 words
            // settle at 18,267, so this is the same corpus at a cap known to clear it.
            // Chasing the vocabulary upwards was the mistake; matching it is the fix.
            var (world, trial, brain) = Made(English(sentences: 2_000, predicting), capacity: Capacity);

            var tally = trial.Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);
            var exam = tally.Unseen?.Accuracy ?? 0.0;

            scored[predicting] = exam - world.Commonest;

            // The check this failure class earned, after three sweeps spent on it. Twice the
            // sizing chased the vocabulary and was caught by it; the third time the
            // vocabulary MATCHED and both arms still pinned, because the two objectives do
            // not read the same corpus the same number of times -- 15,312 questions against
            // 1,800 from the same 2,855 words. A capacity sized on one arm cannot size the
            // other, and a saturated population compares nothing however the sizing was
            // reasoned about. Collected rather than thrown, so both rows still print.
            if (brain.Held.Count >= Capacity)
                saturated.Add(
                    $"{predicting} held {brain.Held.Count} at a cap of {Capacity} after "
                    + $"{world.Questions} questions");

            output.WriteLine(
                $"english {predicting,-8} | exam {exam:F3} silent {tally.Unseen?.Silence ?? 0.0:F3} "
                + $"| own {tally.Recent:F3} | marginal {world.Commonest:F3} "
                + $"| over {exam - world.Commonest:+0.000;-0.000} "
                + $"| {world.Questions} read, {world.Outcomes} words | held {brain.Held.Count}");
        }

        // The gate has to pay here or it pays nowhere. It lost on bAbI for a reason that
        // has since been measured and is about the corpus rather than about gating, and
        // this is the text where the words it picks are the ones carrying something. If
        // it still does not lead, the reason is the gate and `Salient` should go.
        //
        // And a pass here is not a win while both sit at or under their marginal, which is
        // why the margins are printed either way. TWICE NOW a sizing has produced exactly
        // that with `held` on the cap -- Masked at -0.036 and Salient at +0.000, which
        // reads as the gate leading and means only that neither population was allowed to
        // finish growing. Read `held` before reading `exam`, every time.
        // And it is asked before the comparison rather than noted under it. The assertion
        // below passed on a saturated grid twice and the pass meant nothing both times,
        // which is precisely how a reading about a cap gets read as a reading about a gate.
        // Raising the cap is not the fix and has failed twice: what this demands is arms
        // whose LOAD is matched, or a comparison that does not need them to be.
        Assert.Empty(saturated);

        Assert.True(scored[Predicting.Salient] > scored[Predicting.Masked],
            $"the gate did not lead on real English either ({scored[Predicting.Salient]:+0.000;-0.000} "
            + $"against {scored[Predicting.Masked]:+0.000;-0.000} over the marginal), so the "
            + "corpus was never what was wrong with it");
    }

    /// <summary>
    /// Whether the population's cap is what holds the English arm down, or the learner is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The control that says how every other English reading may be read.</b> The first
    /// one came back under its marginal on both objectives with the population pinned at
    /// its cap — and a saturated population cannot be evidence about a corpus, only about
    /// the cap. bAbI never raised the question because thirty words fit anywhere.
    /// </para>
    /// <para>
    /// <b>And it is one axis</b>: how many commitments may be held, nothing else moving.
    /// Same sentences, same objective, same rounds, same seed. Where the population stops
    /// growing before it hits the cap is where the reading is the learner's rather than
    /// the ceiling's, and that is the point every other English arm should be sized above.
    /// </para>
    /// <para>
    /// <b>What the first grid showed</b>, and the reason the assertion is the shape it is:
    /// the exam rises with the cap, clears the marginal once the cap stops binding, and
    /// sits an order below what the same text scores under a plain bag predictor. The
    /// score to watch is not the exam but the DISTANCE from it to <c>own</c>: the learner
    /// answers what it read far better than what it did not, which is the failure this
    /// arm exists to make visible and is nothing the corpus can be blamed for.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_capacity_binds_on_english()
    {
        var cleared = new Dictionary<int, bool>();

        foreach (var capacity in new[] { 2_000, 8_000, 20_000 })
        {
            var (world, trial, brain) = Made(
                English(sentences: 2_000, Predicting.Salient), capacity: capacity);

            var tally = trial.Run(rounds: 10_000, sweep: 1000, target: 0.9, window: 2000);
            var exam = tally.Unseen?.Accuracy ?? 0.0;

            cleared[capacity] = exam > world.Commonest;

            output.WriteLine(
                $"capacity {capacity,6} | exam {exam:F3} | own {tally.Recent:F3} "
                + $"| marginal {world.Commonest:F3} | held {brain.Held.Count,6} "
                + $"| {world.Questions} read, {world.Outcomes} words");
        }

        // The claim worth failing the build over, and it is about the CAP rather than about
        // the score. Reading real English beats always saying the commonest answer once the
        // population is allowed to be big enough, and does not when it is not. If the small
        // cap ever clears too, the cap stopped binding and every arm here may be sized down.
        Assert.False(cleared[2_000], "a cap of 2,000 now clears the marginal on English, so "
            + "it stopped binding and the other English arms are sized larger than they need");

        Assert.True(cleared[20_000], "reading real English no longer beats its marginal even "
            + "with the cap off, so what blocks the primer route is the learner rather than "
            + "the corpus — and fork 100's reading says the signal is there to be had");
    }

    /// <summary>Plain English, sized so it runs in the suite rather than in a sweep.</summary>
    /// <param name="sentences">How many sentences of the export to read.</param>
    /// <param name="predicting">What the learner is asked to be wrong about.</param>
    private static RecalledSettings English(int sentences, Predicting predicting) =>
        new()
        {
            Corpus = Path.Combine(Tree.Repo(), "corpora", "tatoeba_eng.tsv"),
            Task = 0,
            Sentences = sentences,
            Withheld = sentences / 10,
            Span = 0,
            Predicting = predicting,
        };

    /// <summary>
    /// The English world is one sentence a story, and its exam is sentences never read.
    /// </summary>
    /// <remarks>
    /// <b>The structure rather than the score</b>, because the score is a sweep and this has
    /// to fail the build. Two things could quietly go wrong and read as a result:
    /// sentences sharing a story would let a span reach into somebody else's full stop,
    /// and an exam drawn from sentences that were also read would be recall wearing
    /// comprehension's clothes. Both are asserted here and neither costs a second.
    /// </remarks>
    [Fact]
    public void Plain_english_is_one_sentence_a_story_and_examined_on_sentences_never_read()
    {
        var world = new Recalled(English(sentences: 400, Predicting.Salient));

        // ONE TARGET A SENTENCE UNDER `Salient`, and the held tenth is not among them.
        // 400 sentences less the 40 withheld is what may be read, and the withheld ones
        // are the whole of the examination.
        Assert.Equal(360, world.Questions);
        Assert.True(world.Outcomes > 400, $"only {world.Outcomes} distinct words in 400 sentences");

        // And nothing read is anything examined. A sentence is its own story here, so
        // withholding the last forty stories withholds forty whole sentences -- which is
        // the property the bAbI world gets from withholding whole stories.
        var read = new HashSet<int>();
        for (var one = 0; one < world.Questions; one++)
            if (world.Next().Outcome is { } outcome) read.Add(outcome);

        output.WriteLine(
            $"400 sentences: {world.Questions} read over {world.Outcomes} words, "
            + $"{read.Count} distinct answers, marginal {world.Commonest:F3}");

        // The marginal is the floor every English reading is against, and on the rarest
        // word of a sentence it is nearly nothing -- which is the whole reason this
        // corpus can show a lift where bAbI's six rooms could not.
        Assert.True(world.Commonest < 0.1,
            $"the commonest rarest-word answer now takes {world.Commonest:F3} of the exam, "
            + "so plain English acquired a modal answer and the floor moved");
    }

    /// <summary>
    /// Which word is worth predicting, decided by ALTERNATIVES rather than by rarity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Fork 91, and the front-end instrument for it.</b> A masked objective spends its
    /// population on function words because a bag predicts those best, and demanding the
    /// RAREST word instead rescues the kind of answer and not the score. Fork 95 says why
    /// arithmetically: on this corpus the commonest motion verb outranks every name while the
    /// rarer verbs fall below all of them, so no rank keeps the names and drops the verbs.
    /// </para>
    /// <para>
    /// <b>So the rule proposed here is not a rank at all</b>: a word is worth predicting if
    /// something ELSE could have been there instead. That is the alternation statistic
    /// already measured — <i>the</i> and <i>to</i> belong to no category because they stand
    /// with everything, and a word with no alternative was never a choice, so predicting it
    /// cannot be informative. It is the surprise gate's question asked where no learning is
    /// needed to answer it.
    /// </para>
    /// <para>
    /// <b>And what would drop it is written before it ran:</b> picking the same targets as
    /// rarity already picks. A gate that changes no target changes no population, whatever
    /// story is told about why it should.
    /// </para>
    /// <para>
    /// <b>It did not pick the same targets and the arm dies anyway</b>, for a better reason than
    /// the one pre-registered. Rarity already selects content words perfectly on the
    /// one-fact task — every target a room, not a preposition among them — so target
    /// selection was never what was broken, and the plan's own note that it <i>rescues the
    /// kind of answer and not the score</i> should have been read that way.
    /// </para>
    /// <para>
    /// <b>What is broken is that the objective is very nearly noise</b>, and the ceiling column
    /// is the first thing to say so. A PERFECT predictor of the rarest-word objective
    /// scores barely above a blind draw over the six rooms, because this corpus draws its
    /// rooms at random over a template — nothing in <i>Mary went to the</i> carries where she
    /// went, and no learner may be blamed for failing to find it.
    /// </para>
    /// <para>
    /// <b>And the ungated arm's ceiling is half again the gated ones</b>, which explains the
    /// function words rather than condemning them. <i>to</i> and <i>the</i> are the only
    /// predictable words in the corpus, so a masked objective spending its population on them
    /// is not a pathology — <b>it is the arm finding the only signal on offer.</b>
    /// </para>
    /// <para>
    /// <b>So no gate can pay here</b>, which is a wall and not a dial. Selecting informative
    /// targets IS selecting unpredictable ones on this corpus, and the two cannot be had at
    /// once. <b>A primer needs a text where reading is genuinely predictive</b>, which is a
    /// property of the corpus rather than of the objective or the learner.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Which_word_is_worth_predicting()
    {
        foreach (var task in new[] { 1, 2 })
        {
            var (naming, company) = Counted(task);

            var sorted = Grouped(new HashSet<Code>(company.Keys), company)
                .Where(group => group.Count >= 2)
                .SelectMany(group => group)
                .ToHashSet();

            var text = new Babi(new BabiSettings { Corpus = Tree.Babi(), Task = task, Stories = false });

            var rarity = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var line in text.Lines)
                foreach (var word in Babi.Words(line.Text ?? string.Empty))
                    rarity[word] = rarity.GetValueOrDefault(word) + 1;

            var ceiling = new Dictionary<string, double>(StringComparer.Ordinal);

            foreach (var rule in new[] { "every word", "the rarest", "has alternatives" })
            {
                var picked = new Dictionary<string, int>(StringComparer.Ordinal);

                var contexts = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);

                foreach (var line in text.Lines)
                {
                    if (line.Asking) continue;

                    var words = Babi.Words(line.Text ?? string.Empty);
                    if (words.Count < 2) continue;

                    IEnumerable<string> targets = rule switch
                    {
                        "every word" => words,
                        "the rarest" => [words
                            .Select((word, at) => (word, at))
                            .OrderBy(one => rarity.GetValueOrDefault(one.word, 0))
                            .ThenBy(one => one.at)
                            .First().word],

                        // Every word that had an alternative, and not one of them. The rule is
                        // a filter rather than a rank, so it does not have to choose between
                        // two words that were both real choices — which is the whole
                        // difference from a frequency cut.
                        _ => words.Where(one => sorted.Contains(Babi.Of(one))),
                    };

                    foreach (var word in targets)
                    {
                        picked[word] = picked.GetValueOrDefault(word) + 1;

                        var context = string.Join(
                            ",", words.Where((one, at) => one != word).Order(StringComparer.Ordinal));

                        if (!contexts.TryGetValue(context, out var seen))
                            contexts[context] = seen = new Dictionary<string, int>(StringComparer.Ordinal);

                        seen[word] = seen.GetValueOrDefault(word) + 1;
                    }
                }

                // And whether the target was predictable at all, which is the ceiling
                // question asked of an OBJECTIVE rather than of a front end. Statements
                // sharing a context are grouped and the commonest target in each is counted:
                // that is what a perfect predictor of this objective would score, so a low
                // number means the arm was set an unanswerable task and no gate can help it.
                var best = 0;
                var asked = 0;

                foreach (var group in contexts)
                {
                    asked += group.Value.Values.Sum();
                    best += group.Value.Values.Max();
                }

                var total = picked.Values.Sum();

                var carrying = picked
                    .Where(one => Key.Any(key => key.Value.Contains(one.Key)))
                    .Sum(one => one.Value);

                output.WriteLine(
                    $"task {task} {rule,-16} | {total,6} targets, {carrying / (double)Math.Max(1, total):F3} "
                    + $"of them a name, room or prop | a perfect predictor of this objective "
                    + $"scores {best / (double)Math.Max(1, asked):F3} | commonest "
                    + string.Join(" ", picked.OrderByDescending(one => one.Value).Take(6)
                        .Select(one => $"{one.Key}:{one.Value}")));
            }
        }
    }

    /// <summary>
    /// Whether a SECOND hop puts the answer in the room, and which key rule gets it there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The ceiling before the mechanism</b>, which is the third time that order has paid
    /// today. One hop leaves the answer in the room on a quarter of task two and a
    /// twelfth of task three, so the retrieval and not the learner is what those tasks are
    /// short of. What no grid can say is whether a second hop would FIND it, and that is
    /// decided with nothing learnt and in milliseconds.
    /// </para>
    /// <para>
    /// <b>And the key rule is the whole arm rather than a detail</b>, which fork 95 already paid
    /// to discover. Every statement of this corpus says <i>to</i> and <i>the</i>, so a
    /// second hop keyed on whatever the first statement contained walks to the statement
    /// before it and calls recency a chain. The rules below differ only in which words of
    /// the first reading are allowed to be the next key.
    /// </para>
    /// <para>
    /// <b>And what would drop it is written before it ran:</b> no rule raising the
    /// answer-present column above what one hop already reaches. A learner cannot find what
    /// is not in the room, so a flat ceiling kills the arm whatever a score would have said.
    /// </para>
    /// <para>
    /// <b>THE KILL DID NOT FIRE.</b> Two hops takes task two from a quarter to near a half and
    /// task three from a twelfth to a quarter; three hops reaches four questions in seven and
    /// two in five. The room is where the answers were missing, exactly as the one-hop column
    /// said.
    /// </para>
    /// <para>
    /// <b>And about half of that is recency rather than chaining</b>, which the control says and
    /// the headline would not have. Every statement here says <i>to</i> and <i>the</i>, so
    /// a chain keyed on everything can walk back a sentence at a time and never follow a
    /// referent — which is <c>span</c> under a longer name. Taking the newest three outright
    /// already carries half of task two and two fifths of task three.
    /// </para>
    /// <para>
    /// <b>So chaining's own margin is a tenth at two hops</b> on task two, and
    /// it shrinks with depth, to under three points by three hops on task three. It is real
    /// and it is far smaller than the ceiling rise, <b>and the arm must therefore be scored
    /// against a span-matched control</b> rather than against one hop — widening the moment is
    /// already known here to buy the drawn score and sell the held-out one.
    /// </para>
    /// <para>
    /// <b>And no key rule wins twice</b>, which is why none is shipped on this evidence.
    /// Naive-everything leads at three hops, not-background at two, and in-a-category on task
    /// three at two — every gap inside about one standard error on two hundred questions. The
    /// hop is what pays; which word carries it is not yet decided by anything.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_a_second_hop_puts_the_answer_in_the_room()
    {
        foreach (var task in new[] { 1, 2, 3 })
        {
            var world = new Recalled(World(task: task, span: 0));
            var (naming, company) = Counted(task);

            var sorted = Grouped(new HashSet<Code>(company.Keys), company)
                .Where(group => group.Count >= 2)
                .SelectMany(group => group)
                .ToHashSet();

            var ceiling = new Dictionary<string, double>(StringComparer.Ordinal);

            foreach (var rule in new[] { "the newest", "everything", "not background", "in a category" })
            foreach (var hops in new[] { 1, 2, 3 })
            {
                var reached = 0;
                var read = 0;

                for (var one = 0; one < world.Withheld.Count; one++)
                {
                    var asking = world.Withheld[one].Seen.Bagged;
                    var answer = Babi.Of(world.Transcript[one].Answer);

                    var background = new HashSet<Code>(asking.Story.Count == 0 ? [] : asking.Story[0]);
                    for (var at = 1; at < asking.Story.Count; at++) background.IntersectWith(asking.Story[at]);

                    var moment = new HashSet<Code>(asking.Question);
                    var key = asking.Question;
                    var from = 0;

                    for (var hop = 0; hop < hops; hop++)
                    {
                        // The control, and it had to be named to be one. Every statement of
                        // this corpus says *to* and *the*, so a chain keyed on everything the
                        // last one held can walk back a sentence at a time and never once
                        // follow a referent — which is `span` under a longer name, already
                        // built and already measured. This arm takes the newest statements
                        // outright, so the two columns say which mechanism is paying.
                        var at = rule == "the newest"
                            ? (from < asking.Story.Count ? from : -1)
                            : Reading(asking, key, from);

                        if (at < 0) break;

                        moment.UnionWith(asking.Story[at]);
                        read++;

                        // The next key is what this reading supplied and never what the last
                        // one already held — a key already used is the hop just taken, so
                        // carrying it forward returns the same statement for ever.
                        var next = asking.Story[at].Where(code => !key.Contains(code));

                        next = rule switch
                        {
                            "not background" => next.Where(code => !background.Contains(code)),
                            "in a category" => next.Where(sorted.Contains),
                            _ => next,
                        };

                        // OLDER ONLY, because the chain runs backwards in time: what a
                        // statement mentions was established before it, and searching
                        // forwards would let anything later answer.
                        key = new HashSet<Code>(next);
                        from = at + 1;
                    }

                    if (moment.Contains(answer)) reached++;
                }

                ceiling[$"{rule}|{hops}"] = reached / (double)world.Withheld.Count;

                output.WriteLine(
                    $"task {task} {rule,-14} {hops} hop | answer present "
                    + $"{reached / (double)world.Withheld.Count:F3} of {world.Withheld.Count} | "
                    + $"statements read {read / (double)world.Withheld.Count:F2}");
            }

            // Following a key beats taking the newest, at every depth past the first and on
            // every task — but only the best rule does, and reading three statements by
            // recency alone already carries most of what three hops carry. So the claim
            // asserted is the ordering and never the size of the gap.
            foreach (var hops in new[] { 2, 3 })
                Assert.True(
                    new[] { "everything", "not background", "in a category" }
                        .Max(rule => ceiling[$"{rule}|{hops}"]) >= ceiling[$"the newest|{hops}"],
                    $"task {task} at {hops} hops: no key rule beat taking the newest");
        }
    }

    /// <summary>
    /// Which statement, at or after <paramref name="from"/>, names anything in the key.
    /// </summary>
    /// <remarks>
    /// <b>Newest first is the order the world hands them over</b>, so the first match IS the
    /// newest one about whatever was asked, with no scoring and nothing to tie-break. Returns
    /// a negative where nothing matches, which is a real case rather than an error: a
    /// question naming something never said has no store entry to read.
    /// </remarks>
    private static int Reading(Asking asking, IReadOnlySet<Code> key, int from)
    {
        for (var at = from; at < asking.Story.Count; at++)
            foreach (var one in asking.Story[at])
                if (key.Contains(one)) return at;

        return -1;
    }

    /// <summary>
    /// A category fires on ANY member, and its code is the same one on every machine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The one property that separates this from rung five</b>, and nothing else asserts
    /// it. A minted name stands for a set that CO-FIRES and appears when all of it does;
    /// a category stands for a set of ALTERNATIVES, which by construction never co-occur — so
    /// an all-members fold would fire on nothing at all and the arm would read as inert
    /// rather than as broken. <b>A mechanism that cannot fire looks like one that
    /// does not help</b>, which is a trap this repo has already paid for twice.
    /// </para>
    /// <para>
    /// <b>And the code is derived from the members</b>, which is the constraint and not a
    /// style. Two front ends counting the same statements in a different order must reach
    /// the same code without speaking, or a category means one thing on one machine and
    /// another on the next.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_category_fires_on_any_member_and_is_named_by_what_it_holds()
    {
        var people = new HashSet<Code> { Babi.Of("mary"), Babi.Of("john"), Babi.Of("sandra") };
        var shuffled = new HashSet<Code> { Babi.Of("sandra"), Babi.Of("mary"), Babi.Of("john") };

        Assert.Equal(Joined.Category(people), Joined.Category(shuffled));
        Assert.Equal(Joined.Sorted, Joined.Category(people).Modality);

        // A different set is a different category, so dropping a member cannot silently
        // rewrite what every scope holding the old name was claiming.
        Assert.NotEqual(
            Joined.Category(people),
            Joined.Category(new HashSet<Code>(people) { Babi.Of("daniel") }));

        Assert.Throws<ArgumentException>(() => Joined.Category(new HashSet<Code> { Babi.Of("mary") }));

        // One statement built by hand rather than drawn, because a drawn moment is a whole
        // story and holds several names at once — which is a fine moment and a useless
        // instrument, since a fold demanding all members might fire on it by luck.
        var said = new Asking
        {
            Story = [new HashSet<Code> { Babi.Of("mary"), Babi.Of("went"), Babi.Of("kitchen") }],
            Question = new HashSet<Code>(),
        };

        var bare = new HashSet<Code>(new Joined(Joining.Bagged).Codify(said));
        var sorted = new HashSet<Code>(
            new Joined(Joining.Bagged, new List<IReadOnlySet<Code>> { people }).Codify(said));

        // One member is enough and the others are not there, which is the whole assertion. A
        // fold demanding all three would leave this moment untouched and the two sets would
        // come back equal.
        Assert.Single(bare, people.Contains);
        Assert.Contains(Joined.Category(people), sorted);
        Assert.DoesNotContain(Joined.Category(people), bare);

        // And the plain word survives beside it. Replacing a name with its category would
        // make mary and john the same word, which is the general end of the gradient eating
        // the particular one — the collapse this repo has already measured in the one store
        // that has a gradient.
        Assert.Equal(bare.Count + 1, sorted.Count);
        Assert.Subset(sorted, bare);
    }

    /// <summary>
    /// Whether a category is worth anything to this learner, handed over FREE.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The kill test</b>, and it is run before the minter rather than after it. Minting a
    /// category cannot beat being given one, so the arm that hands the learner every category
    /// the statistic finds is an upper bound on the whole of fork 97. If the score does not
    /// move here, no operator that computes these sets more cleverly can make it move, and
    /// the design dies for the price of one grid instead of a mechanism.
    /// </para>
    /// <para>
    /// <b>And the categories are the ones the statistic proposes</b>, never the answer
    /// key. <see cref="Key"/> scores what comes back and is not consulted here — an arm
    /// handed <i>person</i> and <i>place</i> by an experimenter would price a category nobody
    /// can compute, which is a ceiling for a mechanism that does not exist.
    /// </para>
    /// <para>
    /// <b>And what would drop it was written before it ran</b>, and was a bad line: clearing
    /// the control by more than the seed spread. <b>The seed spread here is nought</b> — three
    /// seeds returned the identical exam score in every cell — so the rule admits any gain at
    /// all, and the verdict has to be read against the MARGINAL instead. Written down rather
    /// than quietly replaced, because a kill line rewritten after the numbers is not one.
    /// </para>
    /// <para>
    /// <b>The kill did not fire, and the categories come back perfect.</b> The statistic
    /// proposes the six rooms, the four names, the four motion verbs and the three props, and
    /// on the task that has both it separates the taking verbs from the dropping ones — all
    /// of it off the raw text with no learner and no key.
    /// </para>
    /// <para>
    /// <b>And it pays five times more under the bag</b> than under the addressed front end,
    /// which is the reading the second arm exists for. Addressed hands the learner one statement
    /// and has already done the selecting, so a category has almost nothing left to
    /// generalise over and buys a point or two; the bag hands over the whole story and the
    /// category buys five, on both tasks that need more than one fact.
    /// </para>
    /// <para>
    /// <b>And under the bag it is bought for nothing</b>, where under the addressed arm it
    /// costs a third more population. The task that sits at its capacity keeps the identical
    /// budget and scores better with it, so what the category buys there is not more rules —
    /// it is the same rules saying more.
    /// </para>
    /// <para>
    /// <b>And no arm clears its marginal on the two-fact tasks</b>, which is the
    /// limit and not a footnote. A category generalises across the members of one slot; it
    /// does not add a hop, and a hop is what those tasks are missing. So this prices fork 97
    /// and leaves fork 96 exactly where it was.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_a_category_handed_over_free_is_worth_anything()
    {
        foreach (var task in new[] { 1, 2, 3 })
        {
            var (naming, company) = Counted(task);

            // Every code the corpus ever wrote, cut by the same statistic and with nothing
            // told to it. Restricting the candidates to the words that fill a population's
            // slot would make the front end depend on a learner that has not run yet, and
            // the point of this arm is to price the ceiling rather than the route to it.
            var categories = Grouped(new HashSet<Code>(company.Keys), company)
                .Where(group => group.Count >= 2)
                .Select(group => (IReadOnlySet<Code>)new HashSet<Code>(group))
                .ToList();

            foreach (var category in categories)
                output.WriteLine(
                    $"task {task} | category [{string.Join(" ", category.Order().Select(one => naming[one]))}]");

            // One seed, and that is a measurement rather than a shortcut. Three seeds were
            // run first and every cell returned the identical exam score to three places, so
            // the seed reaches nothing this world exercises — which means a seed spread
            // cannot be the yardstick here and the reading has to be taken against the
            // marginal instead.
            //
            // And both front ends, because the addressed one has already done the narrowing.
            // It hands the learner one statement, where a category has almost nothing left to
            // generalise over; the bag hands it the whole story. An arm measured only under
            // the front end that solved the problem would be reporting the front end.
            foreach (var joining in new[] { Joining.Bagged, Joining.Addressed })
            {
                var scored = new double[2];

                foreach (var sorted in new[] { false, true })
                {
                    var (world, trial, brain) = Made(
                        World(task, span: 0), joining, categories: sorted ? categories : null);

                    var tally = trial.Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);

                    scored[sorted ? 1 : 0] = tally.Unseen?.Accuracy ?? 0.0;

                    output.WriteLine(
                        $"task {task} {joining,-9} categories {(sorted ? "on " : "off")} | "
                        + $"exam {tally.Unseen?.Accuracy ?? 0.0:F3} | own {tally.Recent:F3} | "
                        + $"marginal {world.Commonest:F3} | held {brain.Held.Count,5} "
                        + $"names {brain.Held.Names.Count,4} wanting {tally.Wanting:F3}");
                }

                // NEVER WORSE, IN EVERY CELL, which is the claim worth asserting rather than
                // any one gain. A code the learner is free to ignore SHOULD cost nothing, and
                // an arm that quietly took a point off somewhere would mean the extra
                // alphabet was crowding the population — the exact fault that killed the
                // mixed objective and the banding gain.
                Assert.True(
                    scored[1] >= scored[0],
                    $"task {task} {joining}: categories on {scored[1]:F3} under off {scored[0]:F3}");
            }
        }
    }

    /// <summary>
    /// Whether the population holds a FAMILY for the category minter to read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The control before the operator, and it costs one run.</b> Fork 98 says the
    /// statistic is there in the TEXT; fork 97 mints the category from the POPULATION, which
    /// is a different store and may not contain the same thing at all. The operator is the
    /// slot that varies across a family of otherwise-identical rules, so if no two resident
    /// commitments differ in exactly one scope position there is nothing for it to see and it
    /// dies before it is written.
    /// </para>
    /// <para>
    /// <b>And the transcript is the instrument rather than the count.</b> A family count says
    /// how many and never of what, and a population that families up on <i>to</i> and
    /// <i>the</i> reads identically to one that families up on the cast — which is the
    /// function-word failure that has already cost this branch a whole objective.
    /// </para>
    /// <para>
    /// <b>And the population alone cannot supply a category</b>, which is the first half of the
    /// answer. Every slot it offers is a bag holding names, motion verbs, rooms, props,
    /// function words and minted names at once, because a slot is only ever <i>everything
    /// that predicted this answer</i>. Read off the population and nothing else, the operator
    /// would mint that bag.
    /// </para>
    /// <para>
    /// <b>And fork 98's statistic cuts it and never once miscuts it</b>, which is the second.
    /// Every group holding any word of the key holds words of ONE category — seventy-one of
    /// them over the three tasks, and not one straddles two. So the two halves are one
    /// mechanism: the population says WHERE a slot is and the text says WHAT belongs in it,
    /// and neither says both.
    /// </para>
    /// <para>
    /// <b>And the category comes back whole only where the learner is failing</b>, which inverts
    /// the order this was planned in. The task the arm answers outright holds twenty-seven
    /// rules and returns half the cast; the task it scores below its own marginal on holds
    /// twelve hundred and returns all four names and all three props. A solved task needs no
    /// family, so it has none to read — <b>the operator has most to see exactly where it is
    /// most wanted</b>, and a first measurement taken on task one alone would have read as a
    /// weak result.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_the_population_holds_a_family_for_a_category_to_be_read_off()
    {
        foreach (var task in new[] { 1, 2, 3 })
        {
            var (world, trial, brain) = Made(World(task, span: 0), Joining.Addressed);
            var tally = trial.Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);

            var (naming, company) = Counted(task);

            string Spell(Code code) =>
                naming.TryGetValue(code, out var word) ? word
                : brain.Held.Names.Knows(code) ? $"name#{code.Value % 1000}"
                : $"?{code.Value % 1000}";

            string Answering(Commitment one) => (int)one.Expects.Value is var at
                && at < world.Vocabulary.Length ? world.Vocabulary[at] : $"#{one.Expects.Value}";

            // The frame is the scope with one position taken out, and the expectation stays
            // in. Two rules agreeing on everything but the answer are not a family varying in
            // a slot, they are a contradiction — and grouping those would mint a category out
            // of exactly the disagreement the design wants kept.
            //
            // And a one-code scope leaves an empty frame, which is admitted rather than
            // skipped. Covering mints one code and nothing longer, so a run whose population
            // is mostly one-code rules would report no family at all under a length bar — and
            // the family it would have missed is every code that predicts one answer.
            var families = new Dictionary<string, List<Commitment>>(StringComparer.Ordinal);
            var slots = new Dictionary<string, HashSet<Code>>(StringComparer.Ordinal);

            foreach (var one in brain.Held.All)
                for (var at = 0; at < one.Scope.Length; at++)
                {
                    var rest = one.Scope.Where((_, other) => other != at).Order();
                    var frame = $"{one.Expects.Value}<{string.Join(",", rest)}";

                    if (!families.TryGetValue(frame, out var had)) families[frame] = had = [];
                    if (!slots.TryGetValue(frame, out var filling)) slots[frame] = filling = [];

                    had.Add(one);
                    filling.Add(one.Scope[at]);
                }

            var varying = families.Keys
                .Where(frame => slots[frame].Count >= 2)
                .OrderByDescending(frame => slots[frame].Count)
                .ToList();

            var recovered = new HashSet<string>(StringComparer.Ordinal);

            foreach (var frame in varying.Take(8))
            {
                var one = families[frame][0];

                output.WriteLine(
                    $"task {task} | {{{string.Join(" ", one.Scope.Where(code => !slots[frame].Contains(code)).Select(Spell))}}}"
                    + $" + [{string.Join(" ", slots[frame].Order().Select(Spell))}] -> {Answering(one)}");
            }

            // And the slot is then partitioned by fork 98's statistic, which is the whole
            // proposal. The population says WHERE a slot is and never what belongs in it; the
            // text says which codes are alternatives and never that any rule wanted them. A
            // group is what survives both — never in one statement, and keeping company alike
            // enough that fork 98's measured chasm cannot be straddled.
            foreach (var frame in varying)
                foreach (var group in Grouped(slots[frame], company))
                    if (group.Count >= 2)
                        recovered.Add(string.Join(" ", group.Select(Spell).Order(StringComparer.Ordinal)));

            // Pure means no group straddles two of the key's categories, and a group of verbs
            // is not scored either way — there is no verb key and inventing one would be the
            // experimenter deciding what a category is after seeing the groups.
            var judged = 0;
            var pure = 0;
            var largest = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var group in recovered.Order(StringComparer.Ordinal))
            {
                var members = group.Split(' ');
                var touched = Key.Where(one => members.Any(one.Value.Contains)).ToList();

                if (touched.Count > 0)
                {
                    judged++;
                    if (touched.Count == 1 && members.All(touched[0].Value.Contains))
                    {
                        pure++;
                        largest[touched[0].Key] = Math.Max(largest.GetValueOrDefault(touched[0].Key), members.Length);
                    }
                }

                output.WriteLine($"task {task} | group [{group}]");
            }

            // Never mixed, which is the assertion. The population's slot is a bag of names,
            // verbs, rooms and objects at once, so a statistic that could not tell them apart
            // would show up here as one group holding two of the key's categories.
            Assert.Equal(judged, pure);

            output.WriteLine(
                $"task {task} | held {brain.Held.Count} of them {brain.Held.All.Count(one => one.Scope.Length >= 2)} "
                + $"longer than one | frames {families.Count} varying {varying.Count} "
                + $"widest {(varying.Count == 0 ? 0 : slots[varying[0]].Count)} | "
                + $"groups {recovered.Count} judged {judged} pure {pure} | biggest "
                + string.Join(" ", Key.Select(one =>
                    $"{one.Key} {largest.GetValueOrDefault(one.Key)}/{one.Value.Count}"))
                + $" | exam {tally.Unseen?.Accuracy ?? 0.0:F3} marginal {world.Commonest:F3}");
        }
    }

    /// <summary>
    /// A slot cut into the groups whose members are ALTERNATIVES, by fork 98's statistic.
    /// </summary>
    /// <param name="slot">The codes that filled one position across a family of rules.</param>
    /// <param name="company">What company each code keeps, from <see cref="Counted"/>.</param>
    /// <remarks>
    /// <para>
    /// <b>A stated bar</b>, and it is only safe because fork 98 measured the gap it sits in.
    /// A category's members keep company alike to within a thousandth and the nearest
    /// non-member is a third of the scale away, so anything between the two reads the same —
    /// and a constant chosen inside a measured chasm is a different object from one tuned
    /// until a number came out. <b>A world with a narrower gap would need a test</b>, rather than
    /// a constant, and this is where that would be found out.
    /// </para>
    /// <para>
    /// <b>Components rather than cliques, which is the looser reading on purpose.</b> Demanding
    /// every pair agree would hide a category behind one leaky member; joining through any
    /// link cannot, so what this returns is the most generous grouping the statistic allows
    /// and a mixed group is a real refutation rather than a strictness artefact.
    /// </para>
    /// </remarks>
    private static List<List<Code>> Grouped(
        IReadOnlySet<Code> slot, IReadOnlyDictionary<Code, Dictionary<Code, int>> company)
    {
        const double Alike = 0.9;

        var ours = slot.Where(company.ContainsKey).Order().ToList();
        var home = new Dictionary<Code, int>();

        foreach (var one in ours) home[one] = home.Count;

        foreach (var one in ours)
            foreach (var other in ours)
            {
                if (one.CompareTo(other) >= 0 || company[one].ContainsKey(other)) continue;
                if (RecalledTests.Alike(company[one], company[other]) < Alike) continue;

                var (from, to) = (home[other], home[one]);
                if (from == to) continue;

                foreach (var member in ours) if (home[member] == from) home[member] = to;
            }

        return [.. ours.GroupBy(one => home[one]).Select(group => group.ToList())];
    }

    /// <summary>
    /// What word each code is, and what company each code kept, over one task's statements.
    /// </summary>
    /// <remarks>
    /// <b>One row a statement and never a moment</b>, because a moment repeats every
    /// statement in front of it once per question after it — which would weight a sentence by
    /// how many questions happened to follow, and that is a fact about the corpus's
    /// punctuation rather than about the words.
    /// </remarks>
    private static (Dictionary<Code, string> Naming, Dictionary<Code, Dictionary<Code, int>> Company)
        Counted(int task)
    {
        var text = new Babi(new BabiSettings { Corpus = Tree.Babi(), Task = task, Stories = false });

        var naming = new Dictionary<Code, string>();
        var company = new Dictionary<Code, Dictionary<Code, int>>();

        foreach (var line in text.Lines)
        {
            if (line.Asking) continue;

            foreach (var word in Babi.Words(line.Text ?? string.Empty)) naming[Babi.Of(word)] = word;

            var statement = new HashSet<Code>(line.Words);

            foreach (var one in statement)
            {
                if (!company.TryGetValue(one, out var row)) company[one] = row = [];
                foreach (var other in statement)
                    if (other != one) row[other] = row.GetValueOrDefault(other) + 1;
            }
        }

        return (naming, company);
    }

    /// <summary>
    /// How alike the company two codes keep is, counted over statements they were never
    /// both in.
    /// </summary>
    private static double Alike(IReadOnlyDictionary<Code, int> one, IReadOnlyDictionary<Code, int> other)
    {
        var dot = 0.0;
        foreach (var (code, count) in one)
            if (other.TryGetValue(code, out var had)) dot += count * (double)had;

        var left = Math.Sqrt(one.Values.Sum(count => count * (double)count));
        var right = Math.Sqrt(other.Values.Sum(count => count * (double)count));

        return left == 0.0 || right == 0.0 ? 0.0 : dot / (left * right);
    }

    /// <summary>
    /// Whether a category falls out of the text before anything has learnt anything.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The probe that decides whether the minter is built at all.</b> Rung five names
    /// what CO-FIRES; a category is the complement of that — a set of codes that are
    /// ALTERNATIVES, never both in one statement, standing in the same company when they
    /// stand alone. If that statistic cannot separate people from places on a corpus
    /// generated from a handful of templates over a cast of four, it will not separate
    /// anything anywhere, and the operator dies here rather than after it is written.
    /// </para>
    /// <para>
    /// <b>And it costs no learning</b>, which is the pattern that has already earned its
    /// keep. A front-end instrument taking milliseconds killed one key rule and bounded
    /// the headline result before either grid returned — see
    /// <see cref="Whether_a_second_supporting_fact_is_even_in_the_room"/>. The population is
    /// never built here and no round is ever run.
    /// </para>
    /// <para>
    /// <b>The cast is an answer key and never an input.</b> Nothing below is told which
    /// words are names; the two sets exist so the clusters the statistic returns can be
    /// SCORED, exactly as the multiplexer's enumerated truth scores a rule without ever
    /// reaching the learner.
    /// </para>
    /// <para>
    /// <b>And it passes outright, on both categories and all three tasks.</b> Every member's
    /// nearest alternatives are the rest of its category and nothing else, and the margin is
    /// not a threshold but a chasm — the far side of the boundary sits around a third of the
    /// scale below the near side, with the category itself packed against one.
    /// </para>
    /// <para>
    /// <b>The filter alone is not the mechanism</b>, which one task would have hidden.
    /// Never-in-one-statement offers a person exactly the three other people everywhere, and
    /// offers a place exactly the five other places only where the task has nothing but
    /// places to offer. Where a task adds objects and handling verbs the same filter offers
    /// nineteen and is barely a quarter pure, and the company statistic is doing every bit of
    /// the separating.
    /// </para>
    /// <para>
    /// <b>And the ranking is perfect while the stopping has no rule</b>, which is what the minter
    /// inherits. The two cuts below are exactly complementary — the largest fall is right
    /// on every row where something has to be excluded and wrong on every row where nothing
    /// does, and TAKE EVERYTHING is right in precisely the opposite rows. Neither is a rule,
    /// and their being disjoint is the assertion rather than an observation.
    /// </para>
    /// <para>
    /// <b>So what is missing is a bar and not a better gap</b>, which is the shape the repair
    /// gate already has. Choosing the argmax is easy there too; what decides whether to take
    /// it at all is a separation test corrected for how many candidates were considered, and
    /// a category wants the same question asked the other way round — could these two have
    /// been drawn from one distribution.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_alternatives_recover_the_cast_with_nothing_learnt()
    {
        foreach (var task in new[] { 1, 2, 3 })
        {
            var (naming, company) = Counted(task);

            foreach (var (key, wanted) in Key.Where(one => one.Key != "props"))
            {
                var offered = 0;
                var kin = 0;
                var ranked = 0;
                var exact = 0;
                var interior = 0;
                var members = 0;

                foreach (var word in wanted.Order(StringComparer.Ordinal))
                {
                    var code = Babi.Of(word);
                    if (!company.TryGetValue(code, out var mine)) continue;

                    members++;

                    // An alternative is a code this one was never in a statement with, which
                    // is the whole of the first half of the statistic. Ordered by a total
                    // order under the score so two runs cannot disagree about a tie.
                    var alternatives = company.Keys
                        .Where(other => other != code && !mine.ContainsKey(other))
                        .OrderByDescending(other => Alike(mine, company[other]))
                        .ThenByDescending(other => company[other].Values.Sum())
                        .ThenBy(other => other)
                        .ToList();

                    offered += alternatives.Count;
                    kin += alternatives.Count(other => wanted.Contains(naming[other]));

                    var top = alternatives.Take(wanted.Count - 1).ToList();
                    ranked += top.Count(other => wanted.Contains(naming[other]));

                    // Where the list falls away, which is the only cut a minter could
                    // actually make. Taking the answer's own size is the experimenter
                    // holding the knife: nothing inside the machine knows a category has
                    // four members. The biggest consecutive drop is size-free and dial-free,
                    // and TAKE EVERYTHING is one of the cuts it is allowed to choose --
                    // which is what the trailing fall to nothing stands for.
                    var scored = alternatives
                        .Select(other => Alike(mine, company[other]))
                        .ToList();

                    // AND BOTH RULES ARE READ, because the answer turns entirely on whether
                    // TAKE EVERYTHING is one of the cuts on offer, and that was the
                    // experimenter's choice rather than the data's.
                    var cut = 0;
                    var inner = 0;
                    var fall = double.NegativeInfinity;
                    var within = double.NegativeInfinity;

                    for (var at = 0; at < scored.Count; at++)
                    {
                        var last = at + 1 == scored.Count;
                        var drop = scored[at] - (last ? 0.0 : scored[at + 1]);

                        if (drop > fall)
                        {
                            fall = drop;
                            cut = at + 1;
                        }

                        if (last || drop <= within) continue;

                        within = drop;
                        inner = at + 1;
                    }

                    if (wanted.SetEquals(alternatives.Take(cut).Select(other => naming[other]).Append(word)))
                        exact++;

                    if (wanted.SetEquals(alternatives.Take(inner).Select(other => naming[other]).Append(word)))
                        interior++;

                    // THREE PAST THE CUT, so the transcript shows what the ranking REFUSED
                    // and not only what it took. A list ending exactly at the answer's size
                    // cannot be told from one that had nothing else to offer.
                    output.WriteLine(
                        $"task {task} {word,-8} | {alternatives.Count,3} alternatives, cut {cut,2} | "
                        + string.Join(" ", alternatives
                            .Select((other, at) =>
                                (at == cut ? "|| " : string.Empty)
                                + $"{naming[other]}:{scored[at]:F3}")));
                }

                var floor = Math.Max(1, members * (wanted.Count - 1));

                // What the filter did and what the ranking did, kept apart. Every member is
                // an alternative of every other by construction, so the recall of the filter
                // is one whatever happens and says nothing; its PURITY is what falls as a
                // task adds objects, and the difference between the two columns is exactly
                // the work the company statistic is doing.
                Assert.Equal(1.0, ranked / (double)floor);

                // And the two cuts never both work and never both fail, which is the finding
                // rather than a coincidence: one of them is right on every member of every
                // row, and WHICH one is decided by whether the filter left anything to
                // exclude. A minter reading only the fall would be perfect on people and
                // take every object on places.
                Assert.Equal(members, exact + interior);

                output.WriteLine(
                    $"task {task} {key,-6} | in the top {wanted.Count - 1,-2} "
                    + $"{ranked / (double)floor:F3} | cut at the fall "
                    + $"{exact / (double)Math.Max(1, members):F3} inside it "
                    + $"{interior / (double)Math.Max(1, members):F3} | the filter alone offers "
                    + $"{offered / (double)Math.Max(1, members):F1} a member at purity "
                    + $"{kin / (double)Math.Max(1, offered):F3}");
            }
        }
    }
}
