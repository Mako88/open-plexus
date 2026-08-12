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

    /// <summary>The moment as the bagged control sees it, which is every word once.</summary>
    private static IEnumerable<Code> Codify(Asking asking) =>
        new Joined(Joining.Bagged).Codify(asking).Order();

    private static (Recalled World, Trial<Asking> Trial, Brain Brain) Made(
        RecalledSettings settings,
        Joining joining = Joining.Bagged,
        int capacity = 2000,
        int constant = 0)
    {
        var brain = new Brain(new CommittingSettings { Capacity = capacity }, 1);
        var world = new Recalled(settings);

        return (
            world,
            new Trial<Asking>(world, new Joined(joining, world.Frequency, constant), brain),
            brain);
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

        // `to` IS THE ONLY CONSTANT, standing for the function words a real corpus writes
        // into every sentence. Without excluding it every statement shares a key with every
        // other and only the newest survives, which is the dial's own bottom end.
        var frequency = new Dictionary<Code, int>
        {
            [Babi.Of("to")] = 100,
            [Babi.Of("mary")] = 10,
            [Babi.Of("john")] = 10,
            [Babi.Of("garden")] = 5,
            [Babi.Of("office")] = 5,
            [Babi.Of("kitchen")] = 5,
        };

        var situated = new Joined(Joining.Situated, frequency, constant: 1).Codify(story);

        // MARY'S OLD PLACE IS GONE AND HER NEW ONE IS THERE, which is the whole claim.
        Assert.Contains(Babi.Of("garden"), situated);
        Assert.DoesNotContain(Babi.Of("kitchen"), situated);

        // AND JOHN IS UNTOUCHED, which is what separates displacement from a narrower view.
        // A one-statement span would have taken his office as well.
        Assert.Contains(Babi.Of("office"), situated);
        Assert.Contains(Babi.Of("john"), situated);

        // KEYING ON EVERY WORD IS THE NEWEST STATEMENT AND NOTHING ELSE, because `to` is
        // then a key and every statement shares it.
        var narrow = new Joined(Joining.Situated, frequency, constant: 0).Codify(story);

        Assert.DoesNotContain(Babi.Of("office"), narrow);
        Assert.DoesNotContain(Babi.Of("kitchen"), narrow);
        Assert.Contains(Babi.Of("garden"), narrow);

        // AND KEYING ON NOTHING IS THE BAG, code for code.
        var wide = new Joined(Joining.Situated, frequency, constant: frequency.Count).Codify(story);

        Assert.Equal(
            new Joined(Joining.Bagged).Codify(story).Order(),
            wide.Order());

        output.WriteLine(
            $"bag {wide.Count} | situated {situated.Count} | newest only {narrow.Count}");
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
                Row(capacity, joining, constant: 0);

            // AND THE DIAL SWEPT ACROSS ITS OWN TWO ENDS. Nought is a one-statement span
            // and the largest is the bag, so an interior row is the only place this can
            // pay.
            foreach (var constant in new[] { 0, 2, 4, 8, 16, 32 })
                Row(capacity, Joining.Situated, constant);
        }

        void Row(int capacity, Joining joining, int constant)
        {
            var (world, trial, brain) = Made(
                World(task: 1, span: 0), joining, capacity, constant);

            var tally = trial.Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);
            var unseen = tally.Unseen;

            output.WriteLine(
                $"cap {capacity,4} {joining,-9} constant {constant,2} | "
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
}
