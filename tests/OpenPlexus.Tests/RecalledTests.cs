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
/// <b>JOHN'S PROTOTYPE ORDERING PUTS TEXT FIRST AND THE CONTAINERS AFTER IT</b>, and
/// this is the measurement that says whether the containers are worth building. The
/// fleet is already proven to learn across sockets; what is unproven is that words
/// teach this learner anything at all, and no arrangement of processes changes that.
/// </para>
/// <para>
/// <b>A SCOPE IS A SET AND A SENTENCE IS A SEQUENCE, WHICH IS THE WHOLE QUESTION.</b>
/// Rung three is not built, so a moment holding a story is a bag of words — and the two
/// span arms below are the cheapest possible reading of what that costs, taken before
/// anything is built to fix it.
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
    /// <b>NOTHING IS EVER TOLD WHICH WORDS ARE NAMES.</b> These exist so that what a
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
    private static IEnumerable<Code> Codify(Asking asking) =>
        new Joined(Joining.Bagged).Codify(asking).Order();

    private static (Recalled World, Trial<Asking> Trial, Brain Brain) Made(
        RecalledSettings settings, Joining joining = Joining.Bagged, int capacity = 2000,
        IReadOnlyList<IReadOnlySet<Code>>? categories = null, int seed = 1)
    {
        var brain = new Brain(new CommittingSettings { Capacity = capacity }, seed);
        var world = new Recalled(settings);

        return (world, new Trial<Asking>(world, new Joined(joining, categories), brain), brain);
    }

    [Fact]
    public void A_moment_is_the_question_and_as_much_of_the_story_as_the_span_allows()
    {
        var whole = new Recalled(World(task: 1, span: 0, withheld: 40));
        var last = new Recalled(World(task: 1, span: 1, withheld: 40));

        Assert.Equal(whole.Questions, last.Questions);
        Assert.Equal(whole.Outcomes, last.Outcomes);

        // THE SAME QUESTIONS IN THE SAME ORDER EXPECTING THE SAME ANSWERS, so the only
        // thing the arm changes is how many words are in the room. An arm that also moved
        // the answer key would be two changes read as one.
        for (var one = 0; one < whole.Questions; one++)
        {
            var wide = whole.Next();
            var near = last.Next();

            Assert.Equal(wide.Outcome, near.Outcome);
            Assert.True(near.Seen.Words.Count <= wide.Seen.Words.Count);
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
    /// <b>THE WITHHELD TURNS ARE NEVER DRAWN AND THEIR MOMENTS ARE, WHICH IS A CAVEAT ON
    /// THE UNSEEN NUMBER RATHER THAN A FAULT IN THE WITHHOLDING.</b> bAbI is generated
    /// from templates over a small cast, so two stories reach the same bag of words often
    /// — and where they do, a held-out question is one the population has answered before
    /// under a different name.
    /// </para>
    /// <para>
    /// <b>SO THIS COUNTS IT INSTEAD OF FORBIDDING IT.</b> Forbidding it would mean
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

        // ASSERTED AS A RANGE RATHER THAN A VALUE, because what this guards is that the
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
    /// <b>THE BAR IS THE MARGINAL AND NOT THE BLIND DRAW, because the blind draw is too
    /// easy to clear.</b> Six answers make chance a sixth and the commonest answer is a
    /// fifth, so a population that has learnt nothing but which word comes up most is
    /// already at the second number — and a check against the first would pass on it.
    /// </para>
    /// <para>
    /// <b>AND THE HELD-OUT NUMBER CARRIES THE BAR RATHER THAN THE DRAWN ONE.</b> A
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

        // TWICE THE MARGINAL, WHICH THE READING CLEARS BY HALF AGAIN. Written under the
        // grid rather than over it: the drawn and unseen numbers came back near three
        // times `Commonest` and this is the room a default change is allowed to cost
        // before somebody has to look at it.
        Assert.True(unseen.Accuracy > 2.0 * world.Commonest,
            $"answers never asked scored {unseen.Accuracy:F3} against a marginal of "
            + $"{world.Commonest:F3} — text stopped reaching the learner");

        Assert.True(tally.Recent > 2.0 * world.Commonest,
            $"the drawn stream scored {tally.Recent:F3} against {world.Commonest:F3}");

        // THE CAVEAT PRINTED BESIDE THE SCORE AND IN THE SAME TEST, because they were once
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
    /// It answers in English, which is the thing a number cannot show.
    /// </summary>
    /// <remarks>
    /// <b>THE POINT OF THIS IS THAT IT CAN BE READ AND THEREFORE DISBELIEVED.</b> A score
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

            // THE OUTCOME CODE BACK INTO A WORD, WHICH IS THE ONLY PLACE THE MAPPING RUNS
            // BACKWARDS. `Brain.Says` is a code per index and the world's alphabet is that
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

        // WHAT IT REACHES FOR, COUNTED, BECAUSE A SCORE OF EXACTLY NOUGHT IS A DIAGNOSIS
        // WAITING TO BE READ. An arm answering every question wrongly and never abstaining
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
    /// <b>THE ARM IS WORTH NOTHING IF THE MARKER IS NOT WHERE IT SAYS IT IS</b>, and a
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

        // NAMED CARRIES WHICH WORD IT WAS, so it is not the anonymous code under another
        // name — the whole difference between the two arms is this one assertion.
        Assert.DoesNotContain(Joined.Coincided, new Joined(Joining.Named).Codify(matching));
        Assert.Contains(
            new Code(Joined.Both, Babi.Of("mary").Value),
            new Joined(Joining.Named).Codify(matching));

        // AND THE ABSENCE IS SAID OUT LOUD, which is the only arm that speaks when nothing
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
    /// <b>THE MECHANISM ASSERTED ON A MOMENT BUILT BY HAND, BECAUSE A GRID CANNOT TELL A
    /// DISPLACEMENT RULE THAT DOES NOTHING FROM ONE THAT DOES THE WRONG THING.</b> Both come
    /// back as a score, and this repo has read an unwired mechanism as a refutation before.
    /// Three statements: Mary moves twice and John moves once.
    /// </para>
    /// <para>
    /// <b>AND THE TWO ENDS OF THE DIAL ARE ASSERTED TO BE THE TWO CONTROLS</b>, which is the
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

        // MARY'S OLD PLACE IS GONE AND HER NEW ONE IS THERE, which is the whole claim.
        Assert.Contains(Babi.Of("garden"), situated);
        Assert.DoesNotContain(Babi.Of("kitchen"), situated);

        // AND JOHN IS UNTOUCHED, which is what separates displacement from a narrower view.
        // A one-statement span would have taken his office as well.
        Assert.Contains(Babi.Of("office"), situated);
        Assert.Contains(Babi.Of("john"), situated);

        // AND READING AT THE QUESTION'S KEY TAKES MARY'S NEWEST AND NOTHING ELSE, which is a
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
    /// <b>THE MEASURED FAILURE IS A NEAR-PERFECT READER AND A HOPELESS SELECTOR, AND EVERY
    /// ARM SO FAR HAS TRIED TO HELP IT SELECT.</b> A narrow view picks the sentence by hand
    /// and reaches the ceiling; a recency band hands the position over in the alphabet and
    /// buys about half of that. This arm does not help it select at all — it overwrites, so
    /// that by the time the bag is built there is one place for Mary in it and selecting is
    /// not required.
    /// </para>
    /// <para>
    /// <b>THE KILL CONDITION, WRITTEN BEFORE THE ARM RAN: if no setting of the dial beats
    /// <see cref="Joining.Recent"/> at the whole story, drop it.</b> Displacement would then
    /// be buying nothing a position code does not already buy, and the situation model would
    /// be answering a question this world does not ask. Beating the BAG is not enough — the
    /// bottom of this dial is a one-statement span, so an arm that only beat the bag would
    /// be reporting the span arm under a new name.
    /// </para>
    /// <para>
    /// <b>AND THE CAPACITY IS AN AXIS FOR THE REASON THE RECENCY GRID FOUND.</b> That arm's
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

            // THE DISPLACEMENT ARM, WHICH KEYS ON RECENCY AND HAS NO DIAL because the
            // story supplies its own background.
            Row(capacity, Joining.Distinguished);

            // AND THE ONE THE CEILING SAYS SHOULD WIN OUTRIGHT. PRE-REGISTERED: it hands
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
    /// <b>THE CONTROL IS IN THE FILE, WHICH IS THE ONLY REASON THE OTHER TWO MEAN
    /// ANYTHING.</b> <see cref="Joining.Bagged"/> is every reading taken before this
    /// existed, so the three run the same world, the same seed and the same brain and
    /// differ in one call.
    /// </para>
    /// <para>
    /// <b>AND THE TWO ARMS SEPARATE A LOOKUP FROM A VARIABLE.</b>
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
    /// <b>THE CEILING SAYS THIS LEARNER IS A NEAR-PERFECT READER AND A HOPELESS SELECTOR,
    /// AND THIS IS THE FIRST MECHANISM AIMED AT THE SECOND HALF.</b> Shown one statement it
    /// answers all but a hair of what is present; shown the whole story, where everything
    /// is present, it takes under a third. What it cannot do is say WHICH sentence, because
    /// a scope is a subset test over a set and a set has no positions.
    /// </para>
    /// <para>
    /// <b>THE PREDICTION, WRITTEN BEFORE THE ARM RAN: banded at the whole story it should
    /// reach at least what the one-statement view reached.</b> The narrow view wins by
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
        // AND THE CAPACITY IS AN AXIS RATHER THAN A CONSTANT, BECAUSE THE FIRST READING OF
        // THIS ARM CAME BACK PINNED AT THE CAP. Banding multiplies the alphabet, so the
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
    /// against, and without it no number here means anything.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A SCORE WITH NO CEILING BESIDE IT CANNOT SEPARATE A POOR LEARNER FROM A POOR
    /// VIEW.</b> At one statement of span the moment is the last thing said and the
    /// question, so where the last statement is about somebody else the answering word is
    /// not present — and nothing the population could ever hold would put it there. That
    /// share is a fact about the WORLD and the span, decided before any learning happens.
    /// </para>
    /// <para>
    /// <b>IT IS A CEILING RATHER THAN A TARGET, AND IT IS GENEROUS.</b> Being present is
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

            // NEVER NOUGHT, WHICH WOULD MEAN NOBODY COULD ANSWER THIS EXAM AT ALL. One is
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
    /// <b>THE DISTINCTION THE GRID CANNOT DRAW, AND IT COSTS NO LEARNING.</b> Reading at the
    /// question's key answers task one outright and lands on the base rate at task two, and
    /// a score alone cannot say whether the statement it retrieved HELD the answer. Where
    /// the question names the apple and the apple's newest statement says who picked it up,
    /// the answering word was never in the room and no learner could have found it.
    /// </para>
    /// <para>
    /// <b>WHICH IS THE WHOLE SHAPE OF WHAT IS MISSING.</b> One hop of retrieval reaches the
    /// statement the question names; a second hop would have to read at a key that FIRST
    /// reading supplied. A ceiling near nought here says the arm is at its ceiling again and
    /// the fault is the view, exactly as it was at every width on task one.
    /// </para>
    /// <para>
    /// <b>AND IT IS NOT A BOUND ON THE SCORE, WHICH THIS READING IS THE FIRST TO SHOW.</b> An
    /// outcome is an index rather than a word in the room, so a population collects the base
    /// rate by expecting the commonest answer with nothing present to read — and where the
    /// marginal is above this column, a score SITS ABOVE IT with no fault anywhere. Read the
    /// two together or a working arm looks broken.
    /// </para>
    /// </remarks>
    [Fact]
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
    /// <b>THE CAVEAT ON THE PERFECT SCORE, MADE INTO A MEASUREMENT.</b> Task one is named
    /// <i>single supporting fact</i>, so a front end that retrieves one statement by the
    /// question's key is close to that task's own definition — and a grid that only ever ran
    /// there would be reporting the corpus's structure as the arm's result. Tasks two and
    /// three need two statements and three.
    /// </para>
    /// <para>
    /// <b>PRE-REGISTERED, AND THE FAILURE IS THE INFORMATIVE OUTCOME.</b> One statement
    /// cannot carry two supporting facts, so this should fall hard at task two — and where
    /// it falls to is the number worth having: down to the bag says retrieval buys nothing
    /// without chaining, and part of the way says one hop of it is already worth something.
    /// </para>
    /// <para>
    /// <b>AND THE BAG RUNS BESIDE IT AT EVERY TASK</b>, because the tasks are not equally
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
    /// <b>THE SHARPER INSTRUMENT, AND IT COSTS NO LEARNING AT ALL.</b> A grid can only say
    /// that an arm scored badly; it cannot say whether the rule dropped the wrong statement
    /// or the learner failed to use the right one. This asks the question directly — after
    /// displacement, is the answering word still in the room? — and it is decided by the
    /// front end alone, on the withheld set, before a single commitment exists.
    /// </para>
    /// <para>
    /// <b>SO THE TWO COLUMNS TOGETHER ARE THE WHOLE VERDICT ON THE RULE.</b> The bag always
    /// contains its own answer, so a ceiling under one is displacement destroying something
    /// it should have kept. What makes a rule GOOD is throwing a great deal away while that
    /// ceiling holds — the same reading `Span` gets, from a mechanism that is allowed to
    /// keep more than one sentence.
    /// </para>
    /// <para>
    /// <b>AND THE THREE ROWS ARE ONE COMPARISON RATHER THAN THREE READINGS.</b> Displacement
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

        // AND THE ARM THAT READS THE STORE AT THE KEY THE QUESTION SUPPLIES, in the same
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

        // THE DOMINANCE PUT IN THE TEST, because it is the reading the whole arc turns on.
        // Reading the store at the key the question supplies keeps LESS than the narrowest
        // recency rule and loses no answer at all, which no displacement setting managed at
        // any budget. A recency rule is at its ceiling only where it keeps one statement,
        // and this keeps one statement AND has no ceiling to be short of.
        Assert.Equal(world.Withheld.Count, aimed);
        Assert.True(cost < held, $"addressed kept {cost} against displacement's {held}");

        // AND THE CONTROL THAT SAYS WHETHER THE KEY IS DOING ANYTHING AT ALL, matched
        // question by question on how many words survived. Both columns moving together is
        // what removal AT A RATE looks like, and a rate is what dropping statements blindly
        // gives — so without this the two rules above cannot be told from a coin.
        //
        // IT LIVES HERE RATHER THAN IN `Joining` ON PURPOSE. A control arm shipped in the
        // front end would be an arm to delete later; a control computed in the test that
        // reads it costs nothing and cannot be mistaken for a mechanism.
        var draw = new Random(1);
        var blind = 0;
        var spent = 0;

        for (var one = 0; one < world.Withheld.Count; one++)
        {
            var asking = world.Withheld[one].Seen;
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

        // THE READING PUT IN THE TEST RATHER THAN IN A COMMIT MESSAGE. A key that beat the
        // control on the ceiling while keeping MORE would be buying its advantage with
        // budget; this keeps strictly less and answers strictly more, which is the only
        // shape that says the choice of what to drop is doing the work.
        Assert.True(keyed > guessing, $"keyed {keyed:F3} did not beat blind {guessing:F3}");
        Assert.True(held <= spent, $"keyed kept {held} against blind {spent}");
    }

    /// <summary>
    /// Which objective grows the population that answers best — <b>John's question, and
    /// one the field cannot answer for a learner shaped like this.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE EXAMINATION DOES NOT MOVE WHEN THE OBJECTIVE DOES, WHICH IS THE WHOLE OF THE
    /// DESIGN.</b> An objective scored on its own target is unfalsifiable — a next-word arm
    /// hits next words and says nothing about understanding. So every arm predicts a word
    /// from one vocabulary, whole stories are held back from all four alike, and the same
    /// withheld questions are put to whatever each one grew.
    /// </para>
    /// <para>
    /// <b>SO THREE OF THE FOUR SIT AN EXAM THEY WERE NEVER TRAINED FOR</b>, which is
    /// precisely the transfer question: did it learn the language, or this examination? A
    /// masked arm has never seen a question in its life.
    /// </para>
    /// <para>
    /// <b>AND THE DRAWN COLUMN IS NOT COMPARABLE ACROSS ARMS AND IS PRINTED ANYWAY.</b>
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
    /// <b>A GRID RATHER THAN A CHECK, so it prints and asserts nothing.</b> <c>Span</c> is
    /// the crudest possible dose of sequence and rung three is not built, so this is the
    /// reading that says what building it would be worth.
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
    /// A category fires on ANY member, and its code is the same one on every machine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE ONE PROPERTY THAT SEPARATES THIS FROM RUNG FIVE, AND NOTHING ELSE ASSERTS
    /// IT.</b> A minted name stands for a set that CO-FIRES and appears when all of it does;
    /// a category stands for a set of ALTERNATIVES, which by construction never co-occur — so
    /// an all-members fold would fire on nothing at all and the arm would read as inert
    /// rather than as broken. <b>A mechanism that cannot fire looks exactly like one that
    /// does not help</b>, which is a trap this repo has already paid for twice.
    /// </para>
    /// <para>
    /// <b>AND THE CODE IS DERIVED FROM THE MEMBERS, WHICH IS THE CONSTRAINT AND NOT A
    /// STYLE.</b> Two front ends counting the same statements in a different order must reach
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

        // A DIFFERENT SET IS A DIFFERENT CATEGORY, so dropping a member cannot silently
        // rewrite what every scope holding the old name was claiming.
        Assert.NotEqual(
            Joined.Category(people),
            Joined.Category(new HashSet<Code>(people) { Babi.Of("daniel") }));

        Assert.Throws<ArgumentException>(() => Joined.Category(new HashSet<Code> { Babi.Of("mary") }));

        // ONE STATEMENT BUILT BY HAND RATHER THAN DRAWN, because a drawn moment is a whole
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

        // ONE MEMBER IS ENOUGH AND THE OTHERS ARE NOT THERE, which is the whole assertion. A
        // fold demanding all three would leave this moment untouched and the two sets would
        // come back equal.
        Assert.Single(bare, people.Contains);
        Assert.Contains(Joined.Category(people), sorted);
        Assert.DoesNotContain(Joined.Category(people), bare);

        // AND THE PLAIN WORD SURVIVES BESIDE IT. Replacing a name with its category would
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
    /// <b>THE KILL TEST, AND IT IS RUN BEFORE THE MINTER RATHER THAN AFTER IT.</b> Minting a
    /// category cannot beat being given one, so the arm that hands the learner every category
    /// the statistic finds is an upper bound on the whole of fork 97. If the score does not
    /// move here, no operator that computes these sets more cleverly can make it move, and
    /// the design dies for the price of one grid instead of a mechanism.
    /// </para>
    /// <para>
    /// <b>AND THE CATEGORIES ARE THE ONES THE STATISTIC PROPOSES AND NEVER THE ANSWER
    /// KEY.</b> <see cref="Key"/> scores what comes back and is not consulted here — an arm
    /// handed <i>person</i> and <i>place</i> by an experimenter would price a category nobody
    /// can compute, which is a ceiling for a mechanism that does not exist.
    /// </para>
    /// <para>
    /// <b>AND WHAT WOULD DROP IT WAS WRITTEN BEFORE IT RAN, AND WAS A BAD LINE:</b> clearing
    /// the control by more than the seed spread. <b>The seed spread here is nought</b> — three
    /// seeds returned the identical exam score in every cell — so the rule admits any gain at
    /// all, and the verdict has to be read against the MARGINAL instead. Written down rather
    /// than quietly replaced, because a kill line rewritten after the numbers is not one.
    /// </para>
    /// <para>
    /// <b>THE KILL DID NOT FIRE, AND THE CATEGORIES COME BACK PERFECT.</b> The statistic
    /// proposes the six rooms, the four names, the four motion verbs and the three props, and
    /// on the task that has both it separates the taking verbs from the dropping ones — all
    /// of it off the raw text with no learner and no key.
    /// </para>
    /// <para>
    /// <b>AND IT PAYS FIVE TIMES MORE UNDER THE BAG THAN UNDER THE ADDRESSED FRONT END, WHICH
    /// IS THE READING THE SECOND ARM EXISTS FOR.</b> Addressed hands the learner one statement
    /// and has already done the selecting, so a category has almost nothing left to
    /// generalise over and buys a point or two; the bag hands over the whole story and the
    /// category buys five, on both tasks that need more than one fact.
    /// </para>
    /// <para>
    /// <b>AND UNDER THE BAG IT IS BOUGHT FOR NOTHING</b>, where under the addressed arm it
    /// costs a third more population. The task that sits at its capacity keeps the identical
    /// budget and scores better with it, so what the category buys there is not more rules —
    /// it is the same rules saying more.
    /// </para>
    /// <para>
    /// <b>AND NO ARM CLEARS ITS MARGINAL ON THE TWO TASKS THAT NEED TWO FACTS, WHICH IS THE
    /// LIMIT AND NOT A FOOTNOTE.</b> A category generalises across the members of one slot; it
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

            // EVERY CODE THE CORPUS EVER WROTE, cut by the same statistic and with nothing
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

            // ONE SEED, AND THAT IS A MEASUREMENT RATHER THAN A SHORTCUT. Three seeds were
            // run first and every cell returned the identical exam score to three places, so
            // the seed reaches nothing this world exercises — which means a seed spread
            // cannot be the yardstick here and the reading has to be taken against the
            // marginal instead.
            //
            // AND BOTH FRONT ENDS, BECAUSE THE ADDRESSED ONE HAS ALREADY DONE THE NARROWING.
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
    /// <b>THE CONTROL BEFORE THE OPERATOR, AND IT COSTS ONE RUN.</b> Fork 98 says the
    /// statistic is there in the TEXT; fork 97 mints the category from the POPULATION, which
    /// is a different store and may not contain the same thing at all. The operator is the
    /// slot that varies across a family of otherwise-identical rules, so if no two resident
    /// commitments differ in exactly one scope position there is nothing for it to see and it
    /// dies before it is written.
    /// </para>
    /// <para>
    /// <b>AND THE TRANSCRIPT IS THE INSTRUMENT RATHER THAN THE COUNT.</b> A family count says
    /// how many and never of what, and a population that families up on <i>to</i> and
    /// <i>the</i> reads identically to one that families up on the cast — which is the
    /// function-word failure that has already cost this branch a whole objective.
    /// </para>
    /// <para>
    /// <b>AND THE POPULATION ALONE CANNOT SUPPLY A CATEGORY, WHICH IS THE FIRST HALF OF THE
    /// ANSWER.</b> Every slot it offers is a bag holding names, motion verbs, rooms, props,
    /// function words and minted names at once, because a slot is only ever <i>everything
    /// that predicted this answer</i>. Read off the population and nothing else, the operator
    /// would mint that bag.
    /// </para>
    /// <para>
    /// <b>AND FORK 98's STATISTIC CUTS IT AND NEVER ONCE MISCUTS IT, WHICH IS THE SECOND.</b>
    /// Every group holding any word of the key holds words of ONE category — seventy-one of
    /// them over the three tasks, and not one straddles two. So the two halves are one
    /// mechanism: the population says WHERE a slot is and the text says WHAT belongs in it,
    /// and neither says both.
    /// </para>
    /// <para>
    /// <b>AND THE CATEGORY COMES BACK WHOLE ONLY WHERE THE LEARNER IS FAILING, WHICH INVERTS
    /// THE ORDER THIS WAS PLANNED IN.</b> The task the arm answers outright holds twenty-seven
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

            // THE FRAME IS THE SCOPE WITH ONE POSITION TAKEN OUT, AND THE EXPECTATION STAYS
            // IN. Two rules agreeing on everything but the answer are not a family varying in
            // a slot, they are a contradiction — and grouping those would mint a category out
            // of exactly the disagreement the design wants kept.
            //
            // AND A ONE-CODE SCOPE LEAVES AN EMPTY FRAME, WHICH IS ADMITTED RATHER THAN
            // SKIPPED. Covering mints one code and nothing longer, so a run whose population
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

            // AND THE SLOT IS THEN PARTITIONED BY FORK 98's STATISTIC, WHICH IS THE WHOLE
            // PROPOSAL. The population says WHERE a slot is and never what belongs in it; the
            // text says which codes are alternatives and never that any rule wanted them. A
            // group is what survives both — never in one statement, and keeping company alike
            // enough that fork 98's measured chasm cannot be straddled.
            foreach (var frame in varying)
                foreach (var group in Grouped(slots[frame], company))
                    if (group.Count >= 2)
                        recovered.Add(string.Join(" ", group.Select(Spell).Order(StringComparer.Ordinal)));

            // PURE MEANS NO GROUP STRADDLES TWO OF THE KEY'S CATEGORIES, and a group of verbs
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

            // NEVER MIXED, WHICH IS THE ASSERTION. The population's slot is a bag of names,
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
    /// <b>A STATED BAR, AND IT IS ONLY SAFE BECAUSE FORK 98 MEASURED THE GAP IT SITS IN.</b>
    /// A category's members keep company alike to within a thousandth and the nearest
    /// non-member is a third of the scale away, so anything between the two reads the same —
    /// and a constant chosen inside a measured chasm is a different object from one tuned
    /// until a number came out. <b>A world with a narrower gap would need a test rather than
    /// a constant, and this is where that would be found out.</b>
    /// </para>
    /// <para>
    /// <b>COMPONENTS RATHER THAN CLIQUES, WHICH IS THE LOOSER READING ON PURPOSE.</b> Demanding
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
    /// <b>ONE ROW A STATEMENT AND NEVER A MOMENT</b>, because a moment repeats every
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
    /// <b>THE PROBE THAT DECIDES WHETHER THE MINTER IS BUILT AT ALL.</b> Rung five names
    /// what CO-FIRES; a category is the complement of that — a set of codes that are
    /// ALTERNATIVES, never both in one statement, standing in the same company when they
    /// stand alone. If that statistic cannot separate people from places on a corpus
    /// generated from a handful of templates over a cast of four, it will not separate
    /// anything anywhere, and the operator dies here rather than after it is written.
    /// </para>
    /// <para>
    /// <b>AND IT COSTS NO LEARNING, WHICH IS THE PATTERN THAT HAS ALREADY EARNED ITS
    /// KEEP.</b> A front-end instrument taking milliseconds killed one key rule and bounded
    /// the headline result before either grid returned — see
    /// <see cref="Whether_a_second_supporting_fact_is_even_in_the_room"/>. The population is
    /// never built here and no round is ever run.
    /// </para>
    /// <para>
    /// <b>THE CAST IS AN ANSWER KEY AND NEVER AN INPUT.</b> Nothing below is told which
    /// words are names; the two sets exist so the clusters the statistic returns can be
    /// SCORED, exactly as the multiplexer's enumerated truth scores a rule without ever
    /// reaching the learner.
    /// </para>
    /// <para>
    /// <b>AND IT PASSES OUTRIGHT, ON BOTH CATEGORIES AND ALL THREE TASKS.</b> Every member's
    /// nearest alternatives are the rest of its category and nothing else, and the margin is
    /// not a threshold but a chasm — the far side of the boundary sits around a third of the
    /// scale below the near side, with the category itself packed against one.
    /// </para>
    /// <para>
    /// <b>THE FILTER ALONE IS NOT THE MECHANISM, WHICH ONE TASK WOULD HAVE HIDDEN.</b>
    /// Never-in-one-statement offers a person exactly the three other people everywhere, and
    /// offers a place exactly the five other places only where the task has nothing but
    /// places to offer. Where a task adds objects and handling verbs the same filter offers
    /// nineteen and is barely a quarter pure, and the company statistic is doing every bit of
    /// the separating.
    /// </para>
    /// <para>
    /// <b>AND THE RANKING IS PERFECT WHILE THE STOPPING HAS NO RULE, WHICH IS WHAT THE MINTER
    /// INHERITS.</b> The two cuts below are exactly complementary — the largest fall is right
    /// on every row where something has to be excluded and wrong on every row where nothing
    /// does, and TAKE EVERYTHING is right in precisely the opposite rows. Neither is a rule,
    /// and their being disjoint is the assertion rather than an observation.
    /// </para>
    /// <para>
    /// <b>SO WHAT IS MISSING IS A BAR AND NOT A BETTER GAP</b>, which is the shape the repair
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

                    // AN ALTERNATIVE IS A CODE THIS ONE WAS NEVER IN A STATEMENT WITH, which
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

                    // WHERE THE LIST FALLS AWAY, WHICH IS THE ONLY CUT A MINTER COULD
                    // ACTUALLY MAKE. Taking the answer's own size is the experimenter
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

                // WHAT THE FILTER DID AND WHAT THE RANKING DID, KEPT APART. Every member is
                // an alternative of every other by construction, so the recall of the filter
                // is one whatever happens and says nothing; its PURITY is what falls as a
                // task adds objects, and the difference between the two columns is exactly
                // the work the company statistic is doing.
                Assert.Equal(1.0, ranked / (double)floor);

                // AND THE TWO CUTS NEVER BOTH WORK AND NEVER BOTH FAIL, which is the finding
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
