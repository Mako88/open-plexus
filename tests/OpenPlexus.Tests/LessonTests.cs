using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// One hand-written topic told once and a fixed set of questions about it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The instrument a drawn world cannot be, and it is John's.</b> Every earlier reading on
/// this conversation moved the world and the arm at once, because the topic was drawn fresh
/// each run. A lesson holds the world still, so a change is read against one thing.
/// </para>
/// <para>
/// <b>The bars are computed before any arm is run</b>, which is the discipline the last session
/// paid for. A score above a recency rule that needs no learning is a reading about the
/// machine; a score at or under it is a reading about the lesson.
/// </para>
/// </remarks>
public sealed class LessonTests(ITestOutputHelper output)
{
    private static (Tally Tally, Tutor Tutor, Conversing World) Ran(
        Lesson lesson, Carrying carrying, int seed, int passes, int capacity = 2000,
        Asserting asserting = Asserting.Nothing, int tellings = 1, int revising = 0,
        Rooting rooting = Rooting.Singly, Crediting crediting = Crediting.Nothing)
    {
        var tutor = new Tutor(lesson, TextWriter.Null, passes, tellings, revising);

        var brain = new Brain(
            new CommittingSettings
            {
                Capacity = capacity,
                Rooting = rooting,
                Crediting = crediting,
            },
            seed);

        var world = new Conversing(new ConversingSettings
        {
            Typed = tutor,
            Printed = tutor.Printed,
            Carrying = carrying,
            Asserting = asserting,
        });

        var curiosity = new Curiosity(brain, rate: 1.0, seed, world.Naming);

        // Named, because the front end is the next arm this file will want and a bench holding
        // it inline is a wiring nobody can vary.
        var front = new Joined(Joining.Bagged);

        var watching = new Watching<Recited>(
            world, front, acting: felt => Speaking(curiosity.Choose(felt)));

        // Budgeted for the widest statement, because `Asserting.Everything` makes a sentence
        // one moment a word. A run stopping at the moment count would end before the exam.
        var rounds = asserting is Asserting.Everything
            ? tutor.Moments * tutor.Longest
            : tutor.Moments;

        var tally = new Bench(watching, brain).Run(rounds, sweep: 200, target: 0.9, window: 50);

        return (tally, tutor, world);
    }

    /// <summary>The join between what a chooser decided and how this world numbers its doings.</summary>
    private static int? Speaking(Wondered said) =>
        said.Word is not { } word
            ? null
            : said.Asking ? Conversing.Asks(word) : Conversing.Asserts(word);

    /// <summary>What share of one pass's questions were answered right.</summary>
    private static double Right(Tutor tutor, int pass) =>
        tutor.Put[pass] == 0 ? 0.0 : tutor.Confirmed[pass] / (double)tutor.Put[pass];

    [Fact]
    public void A_paragraph_typed_at_once_arrives_one_sentence_a_moment()
    {
        // John's, and it is the first of the two things a conversation needed. A pasted
        // paragraph used to be one moment and therefore a bag of words with no way to say
        // which statement a word came from.
        var typed = new StringReader(
            "the cat sound is meow. the dog sound is bark. what is the cat sound?\n"
            + $"{Conversing.Over}\n");

        var world = new Conversing(new ConversingSettings
        {
            Typed = typed,
            Printed = TextWriter.Null,
            Carrying = Carrying.Never,
        });

        var moments = new List<Recited>();

        while (!world.Ended)
        {
            var turn = world.Next();

            if (world.Ended) break;

            moments.Add(turn.Seen);
            world.Do(null);
        }

        Assert.Equal(3, moments.Count);

        // Each statement is its own moment and holds nothing but its own words, and the
        // question is the third moment rather than a fourth line of the first.
        Assert.All(moments.Take(2), one =>
        {
            Assert.Single(one.Said);
            Assert.Empty(one.Asked);
        });

        Assert.Empty(moments[2].Said);
        Assert.NotEmpty(moments[2].Asked);
    }

    [Fact]
    public void A_question_holding_the_whole_story_leaves_nothing_able_to_root_a_rule()
    {
        const int Passes = 20;
        const int Seeds = 4;

        // Genesis may not root on a code that has never once been absent, which is the gate
        // that stopped a population filling with rules about the world existing. A topic that
        // accumulates puts every word said so far into every later moment, so on this lesson
        // that gate refuses every code and the population never starts.
        var last = new List<double>();

        for (var seed = 1; seed <= Seeds; seed++)
        {
            var (tally, tutor, _) = Ran(Lesson.Creatures, Carrying.Always, seed, Passes);

            output.WriteLine(
                $"seed {seed}: {tally.Minted} minted, {tally.Resident} resident, last pass "
                + $"{Right(tutor, Passes - 1):F3}");

            Assert.Equal(0, tally.Minted);

            last.Add(Right(tutor, Passes - 1));
        }

        // And with nothing minted the examination is a blind draw over the words in front of
        // the machine, which the same arm with a bare question clears at 1.000 — so the empty
        // population is what the gap is about rather than the lesson being hard.
        output.WriteLine($"last pass, mean over seeds: {last.Average():F3}");

        Assert.True(last.Average() < 0.5,
            $"the population is empty and the last pass still reads {last.Average():F3}, so "
            + "something other than the commitments is answering");
    }

    [Fact]
    public void A_statement_claiming_nothing_teaches_nothing_however_often_it_is_told()
    {
        const int Passes = 20;
        const int Seeds = 4;

        var lesson = Lesson.Creatures;
        var untold = lesson with { Statements = [] };

        // The bars first, before a single arm is read. Every wrong turn on this world came
        // from reading a grid before the no-learning ceiling it had to beat.
        var bar = new Tutor(lesson, TextWriter.Null).Recency / (double)lesson.Exam.Count;
        var marginal = new Tutor(lesson, TextWriter.Null).Marginal / (double)lesson.Exam.Count;

        output.WriteLine(
            $"{lesson.Statements.Count} statements, {lesson.Exam.Count} questions, {Passes} "
            + $"passes, {Seeds} seeds");
        output.WriteLine($"bars: recency {bar:F3}, marginal {marginal:F3}");
        output.WriteLine($"{"pass",-6}{"told",10}{"untold",10}");

        var told = new double[Passes];
        var without = new double[Passes];
        var minted = new List<double>();

        for (var seed = 1; seed <= Seeds; seed++)
        {
            var one = Ran(lesson, Carrying.Statements, seed, Passes);
            var other = Ran(untold, Carrying.Statements, seed, Passes);

            minted.Add(one.Tally.Minted);

            for (var pass = 0; pass < Passes; pass++)
            {
                told[pass] += Right(one.Tutor, pass) / Seeds;
                without[pass] += Right(other.Tutor, pass) / Seeds;
            }
        }

        for (var pass = 0; pass < Passes; pass++)
            output.WriteLine($"{pass + 1,-6}{told[pass],10:F3}{without[pass],10:F3}");

        output.WriteLine($"minted: {minted.Average():F1}");

        // The examination is learnt, and it is learnt well above both bars. A machine holding
        // a twelve-way mapping it is corrected into is a real reading and it is not the one
        // this test is named for.
        Assert.True(told[^1] > bar + 0.5,
            $"the last pass reads {told[^1]:F3} against a recency bar of {bar:F3}, so nothing "
            + "here is above a rule that needs no learning at all");

        // And the telling buys none of it, which is the finding this arm exists to hold. A
        // statement that claims nothing carries no settlement, so a round spent being told one
        // moves no counter and the machine that heard the statements is exactly the machine
        // that did not. `Asserting.Rarest` is the arm that changes it, below.
        Assert.True(told.Sum() <= without.Sum() + 0.5,
            $"told totalled {told.Sum():F3} over the passes and untold {without.Sum():F3}. "
            + "The statements have started to teach something, which is a result rather than "
            + "a regression — re-take this reading and say what changed.");
    }

    [Fact]
    public void A_statement_claiming_its_rarest_word_teaches_the_examination_before_it_is_sat()
    {
        const int Seeds = 3;

        int[] tellings = [1, 5, 8, 10, 20];

        var lesson = Lesson.Creatures;
        var untold = lesson with { Statements = [] };

        // The bars first, and neither of them moves with how often the lesson is told.
        var bar = new Tutor(lesson, TextWriter.Null).Recency / (double)lesson.Exam.Count;
        var marginal = new Tutor(lesson, TextWriter.Null).Marginal / (double)lesson.Exam.Count;

        output.WriteLine($"{Seeds} seeds, one examination pass, never sat before");
        output.WriteLine($"bars: recency {bar:F3}, marginal {marginal:F3}");
        output.WriteLine($"{"tellings",-10}{"told",8}{"untold",8}{"minted",8}{"repaired",10}");

        var reached = new Dictionary<int, double>();
        var without = new Dictionary<int, double>();

        foreach (var many in tellings)
        {
            var told = new List<double>();
            var none = new List<double>();
            var minted = new List<double>();
            var repaired = new List<double>();

            for (var seed = 1; seed <= Seeds; seed++)
            {
                var one = Ran(
                    lesson, Carrying.Never, seed, passes: 1,
                    asserting: Asserting.Rarest, tellings: many);

                var other = Ran(
                    untold, Carrying.Never, seed, passes: 1,
                    asserting: Asserting.Rarest, tellings: many);

                told.Add(Right(one.Tutor, 0));
                none.Add(Right(other.Tutor, 0));
                minted.Add(one.Tally.Minted);
                repaired.Add(one.Tally.Repaired);
            }

            reached[many] = told.Average();
            without[many] = none.Average();

            output.WriteLine(
                $"{many,-10}{told.Average(),8:F3}{none.Average(),8:F3}{minted.Average(),8:F1}"
                + $"{repaired.Average(),10:F1}");
        }

        // Told enough times, the examination is answered the first time it is put — so what
        // the machine holds came from being told rather than from being corrected. That is
        // the whole difference from the arm above.
        Assert.True(reached[20] > 0.9,
            $"twenty tellings reached {reached[20]:F3} on an examination never sat before");

        // And the control stays at the floor, which is what says the statements are what did
        // it. The same run with them DELETED sees the identical number of questions.
        Assert.True(without[20] <= marginal,
            $"the untold arm reached {without[20]:F3} with no statements at all, so something "
            + "other than the telling is answering");

        // Once is not enough and repetition is what earns it, which is John's. The gate is the
        // repair floor rather than the lesson: a rule rooted on one word must miss twenty times
        // before it may be narrowed, and the rows show the repairs arriving with the score.
        Assert.True(reached[1] <= marginal,
            $"one telling reached {reached[1]:F3}, so the repetition is buying nothing and the "
            + "threshold this test is named for has moved");
    }

    [Fact]
    public void A_belief_is_replaced_by_being_contradicted_and_it_costs_a_quarter_of_installing_it()
    {
        const int Seeds = 3;
        const int Tellings = 20;

        int[] revisings = [0, 3, 4, 5, 10];

        // John's, and it is the half a monotone counter cannot do. Nothing here deletes the old
        // belief: hits and misses are G-counters, so a superseded commitment keeps everything it
        // ever counted and simply starts missing, while a newer one minted on the contradiction
        // starts hitting. What moves is the vote.
        var lesson = Lesson.Corrected;

        // Three of the twelve are changed and nine are left alone, so the run carries its own
        // control. Forgetting everything on being contradicted and being uncorrectable are
        // opposite failures, and one number over twelve questions reads the same for both.
        var changed = lesson.Revisions.Count;

        output.WriteLine(
            $"{Seeds} seeds, told {Tellings} times, {changed} of {lesson.Exam.Count} facts "
            + "changed afterwards, one examination pass");
        output.WriteLine($"{"revising",-10}{"right",8}{"of",8}");

        var right = new Dictionary<int, double>();

        foreach (var many in revisings)
        {
            var scored = new List<double>();

            for (var seed = 1; seed <= Seeds; seed++)
            {
                var one = Ran(
                    lesson, Carrying.Never, seed, passes: 1, asserting: Asserting.Rarest,
                    tellings: Tellings, revising: many);

                scored.Add(one.Tutor.Confirmed[0]);
            }

            right[many] = scored.Average();

            output.WriteLine($"{many,-10}{scored.Average(),8:F1}{lesson.Exam.Count,8}");
        }

        // Never contradicted, it answers the nine it was told and misses the three it was not.
        // That is the floor this reads against, and it is exact rather than approximate.
        Assert.Equal(lesson.Exam.Count - changed, right[0]);

        // Contradicted enough, every one of the three flips and none of the nine moves. A
        // machine that lost the nine would read below this, not above it.
        Assert.Equal(lesson.Exam.Count, right[10]);

        // And correcting is far cheaper than installing, which is the reading. Twenty tellings
        // put the belief there and five take it out, because installing must clear the repair
        // gate's twenty misses to narrow a rule and correcting only has to out-vote one that is
        // already narrow.
        Assert.True(right[5] > right[3],
            $"five contradictions scored {right[5]:F1} and three scored {right[3]:F1}, so "
            + "repetition is no longer what moves a held belief");

        Assert.True(right[3] <= lesson.Exam.Count - changed,
            $"three contradictions already scored {right[3]:F1}, so a belief is being replaced "
            + "more cheaply than it was installed by more than this test claims");
    }

    [Fact]
    public void Minting_the_whole_statement_as_one_scope_cuts_what_a_fact_costs_but_not_to_one()
    {
        const int Seeds = 3;

        int[] tellings = [1, 2, 3, 8, 10];

        // John's: twenty misses is not what a fact costs, it is what DISCOVERING THE
        // CONJUNCTION costs. Genesis mints one code a commitment, so `cat AND sound -> meow` is
        // reachable only by narrowing `cat -> meow` after it has failed enough times on a
        // question the statement already answered. An assertion is not a guess -- it hands over
        // the scope and the claim together, so it should be able to mint the conjunction.
        var lesson = Lesson.Creatures;
        var bar = new Tutor(lesson, TextWriter.Null).Marginal / (double)lesson.Exam.Count;

        output.WriteLine($"{Seeds} seeds, one examination pass, marginal {bar:F3}");
        output.WriteLine($"{"tellings",-10}{"singly",9}{"wholly",9}{"minted",9}{"repaired",10}");

        var narrow = new Dictionary<int, double>();
        var wide = new Dictionary<int, double>();

        foreach (var many in tellings)
        {
            var one = new List<double>();
            var other = new List<double>();
            var minted = new List<double>();
            var repaired = new List<double>();

            for (var seed = 1; seed <= Seeds; seed++)
            {
                var narrowly = Ran(
                    lesson, Carrying.Never, seed, passes: 1, asserting: Asserting.Withheld,
                    tellings: many);

                var widely = Ran(
                    lesson, Carrying.Never, seed, passes: 1, asserting: Asserting.Withheld,
                    tellings: many, rooting: Rooting.Wholly);

                one.Add(Right(narrowly.Tutor, 0));
                other.Add(Right(widely.Tutor, 0));
                minted.Add(widely.Tally.Minted);
                repaired.Add(widely.Tally.Repaired);
            }

            narrow[many] = one.Average();
            wide[many] = other.Average();

            output.WriteLine(
                $"{many,-10}{one.Average(),9:F3}{other.Average(),9:F3}{minted.Average(),9:F1}"
                + $"{repaired.Average(),10:F1}");
        }

        // The wide arm reaches the same place in fewer tellings, which is the reading. It is
        // the conjunction being STATED rather than found by failing.
        Assert.True(wide[3] > narrow[3],
            $"at three tellings the wide arm read {wide[3]:F3} and the narrow one "
            + $"{narrow[3]:F3}, so minting the statement as one scope is buying nothing");

        Assert.True(wide[8] > narrow[8],
            $"at eight tellings the wide arm read {wide[8]:F3} and the narrow one "
            + $"{narrow[8]:F3}");

        // And it does NOT reach one telling, which is the half still owed. Minting saturates
        // after two tellings, so the rule exists long before it is believed -- what the extra
        // tellings buy is the VOTE, a fresh commitment having no statistics with which to
        // outrank the one-code rules it was minted beside. That is the provisional-weight
        // defect `CommittingSettings.Speaking` already names, arriving here.
        Assert.True(wide[1] <= bar,
            $"one telling read {wide[1]:F3} on the wide arm, so a fact now costs a single "
            + "telling and the reading this test is named for has moved");
    }

    [Fact]
    public void A_mint_credited_with_the_round_that_made_it_is_believed_a_telling_sooner()
    {
        const int Seeds = 3;

        int[] tellings = [1, 2, 3, 8];

        // Genesis proposes a scope drawn from the live moment expecting what actually arrived,
        // so a mint is right about the round it was born on. `Population.Settle` has already
        // run by then, so it is never told — and an accuracy starting at nought against a vote
        // that is a maximum over accuracies means a correct rule sits mute until the same thing
        // is said twice.
        var lesson = Lesson.Creatures;
        var bar = new Tutor(lesson, TextWriter.Null).Marginal / (double)lesson.Exam.Count;

        output.WriteLine($"{Seeds} seeds, one examination pass, marginal {bar:F3}, wide scopes");
        output.WriteLine($"{"tellings",-10}{"blank",9}{"credited",10}");

        var blank = new Dictionary<int, double>();
        var credited = new Dictionary<int, double>();

        foreach (var many in tellings)
        {
            var one = new List<double>();
            var other = new List<double>();

            for (var seed = 1; seed <= Seeds; seed++)
            {
                one.Add(Right(
                    Ran(lesson, Carrying.Never, seed, passes: 1,
                        asserting: Asserting.Withheld, tellings: many,
                        rooting: Rooting.Wholly).Tutor,
                    0));

                other.Add(Right(
                    Ran(lesson, Carrying.Never, seed, passes: 1,
                        asserting: Asserting.Withheld, tellings: many,
                        rooting: Rooting.Wholly, crediting: Crediting.Birth).Tutor,
                    0));
            }

            blank[many] = one.Average();
            credited[many] = other.Average();

            output.WriteLine($"{many,-10}{one.Average(),9:F3}{other.Average(),10:F3}");
        }

        // A telling sooner, and it is the vote rather than the mint that moved. Both arms hold
        // the same population at two tellings; only one of them believes it.
        Assert.True(credited[2] > blank[2],
            $"at two tellings the credited arm read {credited[2]:F3} and the blank one "
            + $"{blank[2]:F3}, so crediting a mint with its own round is buying nothing");

        Assert.All(tellings, many => Assert.True(credited[many] >= blank[many],
            $"crediting cost something at {many} telling(s): {credited[many]:F3} against "
            + $"{blank[many]:F3}"));

        // And ONE telling still fails, for a reason that is neither the vote nor the gate. The
        // claim is the statement's rarest word SO FAR, and on first hearing every word of the
        // conversation has been said once — so the tie goes to the earliest and the statement
        // claims its first word rather than its last. What picks the claim is fork 123, and it
        // is what one-shot is now waiting on.
        Assert.True(credited[1] <= bar,
            $"one telling read {credited[1]:F3}, so a fact now costs a single telling and this "
            + "reading has moved");
    }

    [Fact]
    public void Told_once_and_never_examined_before_it_answers_every_question()
    {
        const int Seeds = 3;

        // John's, and it is what the whole day was for: stating something once should be
        // enough. Three mechanisms had to be in place and none of them is sufficient alone.
        var lesson = Lesson.Creatures;
        var untold = lesson with { Statements = [] };
        var bar = new Tutor(lesson, TextWriter.Null).Marginal / (double)lesson.Exam.Count;

        output.WriteLine($"{Seeds} seeds, told ONCE, one examination pass, marginal {bar:F3}");
        output.WriteLine($"{"rooting",-10}{"crediting",-12}{"right",8}{"resident",10}");

        var reached = new Dictionary<(Rooting, Crediting), double>();

        foreach (var rooting in new[] { Rooting.Singly, Rooting.Wholly })
        {
            foreach (var crediting in new[] { Crediting.Nothing, Crediting.Birth })
            {
                var right = new List<double>();
                var resident = new List<double>();

                for (var seed = 1; seed <= Seeds; seed++)
                {
                    var one = Ran(
                        lesson, Carrying.Never, seed, passes: 1,
                        asserting: Asserting.Everything, tellings: 1, rooting: rooting,
                        crediting: crediting);

                    right.Add(Right(one.Tutor, 0));
                    resident.Add(one.Tally.Resident);
                }

                reached[(rooting, crediting)] = right.Average();

                output.WriteLine(
                    $"{rooting.ToString().ToLowerInvariant(),-10}"
                    + $"{crediting.ToString().ToLowerInvariant(),-12}{right.Average(),8:F3}"
                    + $"{resident.Average(),10:F1}");
            }
        }

        var without = new List<double>();

        for (var seed = 1; seed <= Seeds; seed++)
            without.Add(Right(
                Ran(untold, Carrying.Never, seed, passes: 1, asserting: Asserting.Everything,
                    tellings: 1, rooting: Rooting.Wholly, crediting: Crediting.Birth).Tutor,
                0));

        output.WriteLine($"nothing told at all: {without.Average():F3}");

        // Told once, it answers all twelve on an examination it has never sat.
        Assert.True(reached[(Rooting.Wholly, Crediting.Birth)] > 0.9,
            $"told once it reached {reached[(Rooting.Wholly, Crediting.Birth)]:F3}");

        // And with the statements DELETED it answers none, which is what says the telling did
        // it. The same run sees the identical questions.
        Assert.True(without.Average() <= bar,
            $"the untold arm reached {without.Average():F3} with no statements at all");

        // Every one of the three is load-bearing, and this is the table that says so. Claiming
        // every word in turn removes the need to pick one; minting the whole scope states the
        // conjunction instead of finding it by failing; crediting the minting round lets a
        // correct rule be believed without hearing the sentence twice. Any two of them reach a
        // fraction of what three do.
        Assert.True(reached[(Rooting.Singly, Crediting.Birth)] < 0.9,
            "one-code mints alone reach one telling, so minting the whole scope is not needed "
            + "and the wide arm should go");

        Assert.True(reached[(Rooting.Wholly, Crediting.Nothing)] <= bar,
            "a blank record reaches one telling, so crediting is not needed and it should go");
    }

    [Fact]
    public void Told_once_and_contradicted_once_it_holds_the_correction_and_keeps_the_rest()
    {
        const int Seeds = 3;

        // The two halves of John's ask, put together. A fact told once is held; a fact
        // contradicted once is replaced; and nothing else moves either time.
        var lesson = Lesson.Corrected;
        var changed = lesson.Revisions.Count;

        output.WriteLine($"{Seeds} seeds, told once, one examination pass");
        output.WriteLine($"{"revising",-10}{"right",8}{"of",8}");

        var right = new Dictionary<int, double>();

        foreach (var many in new[] { 0, 1 })
        {
            var scored = new List<double>();

            for (var seed = 1; seed <= Seeds; seed++)
                scored.Add(Ran(
                    lesson, Carrying.Never, seed, passes: 1, asserting: Asserting.Everything,
                    tellings: 1, revising: many, rooting: Rooting.Wholly,
                    crediting: Crediting.Birth).Tutor.Confirmed[0]);

            right[many] = scored.Average();

            output.WriteLine($"{many,-10}{scored.Average(),8:F1}{lesson.Exam.Count,8}");
        }

        // Never contradicted, it answers the nine it was told and misses the three it was not,
        // which is exact rather than approximate.
        Assert.Equal(lesson.Exam.Count - changed, right[0]);

        // Contradicted ONCE, all three flip and none of the nine moves. Under the arm that
        // picks a statement's claim by a corpus count this took five contradictions against
        // twenty tellings — see the test above, which holds that reading.
        Assert.Equal(lesson.Exam.Count, right[1]);
    }

    [Fact]
    public void A_conclusion_that_follows_from_two_statements_is_never_reached()
    {
        const int Seeds = 3;

        int[] tellings = [1, 5, 20];

        // John's question, and the one a perfect score on `Creatures` cannot answer: can it
        // reach something nobody told it. Every fact there is stated outright, so answering
        // them all is a lookup with a good index.
        var lesson = Lesson.Chained;
        var half = lesson.Exam.Count / 2;

        // The two halves as their own examinations, so the split is measured rather than
        // inferred from a total that both contribute to.
        var stated = lesson with { Exam = [.. lesson.Exam.Take(half)] };
        var implied = lesson with { Exam = [.. lesson.Exam.Skip(half)] };

        var bar = new Tutor(lesson, TextWriter.Null).Recency / (double)lesson.Exam.Count;
        var marginal = new Tutor(lesson, TextWriter.Null).Marginal / (double)lesson.Exam.Count;

        output.WriteLine($"{Seeds} seeds, one examination pass, {half} questions a half");
        output.WriteLine($"bars: recency {bar:F3}, marginal {marginal:F3}");
        output.WriteLine($"{"tellings",-10}{"stated",9}{"implied",9}{"wanting",9}{"repaired",10}");

        var told = new Dictionary<int, double>();
        var reached = new Dictionary<int, double>();

        foreach (var many in tellings)
        {
            var one = new List<double>();
            var two = new List<double>();
            var wanting = new List<double>();
            var repaired = new List<double>();

            for (var seed = 1; seed <= Seeds; seed++)
            {
                var direct = Ran(
                    stated, Carrying.Never, seed, passes: 1, asserting: Asserting.Everything,
                    tellings: many, rooting: Rooting.Wholly, crediting: Crediting.Birth);

                var hops = Ran(
                    implied, Carrying.Never, seed, passes: 1, asserting: Asserting.Everything,
                    tellings: many, rooting: Rooting.Wholly, crediting: Crediting.Birth);

                one.Add(Right(direct.Tutor, 0));
                two.Add(Right(hops.Tutor, 0));
                wanting.Add(hops.Tally.Wanting);
                repaired.Add(hops.Tally.Repaired);
            }

            told[many] = one.Average();
            reached[many] = two.Average();

            output.WriteLine(
                $"{many,-10}{one.Average(),9:F3}{two.Average(),9:F3}{wanting.Average(),9:F3}"
                + $"{repaired.Average(),10:F1}");
        }

        // What a statement says is answered from one telling, which is the control half and
        // says the machinery is working.
        Assert.True(told[1] > 0.9,
            $"the stated half read {told[1]:F3} at one telling, so the control is broken and "
            + "nothing below it can be interpreted");

        // And what two statements IMPLY is never reached, at any amount of repetition. A round
        // is three calls -- fold the minted names in, collect every commitment whose scope is a
        // subset of the moment, vote -- and no step puts what fired back into the moment and
        // fires again. So a conclusion needing two facts is reachable only where both are
        // already in front of the machine, and here neither statement holds the question's
        // words together.
        //
        // This closes the day something chains. Forks 28 and 32.
        Assert.All(tellings, many => Assert.True(reached[many] <= marginal,
            $"the implied half read {reached[many]:F3} at {many} telling(s) against a marginal "
            + $"of {marginal:F3}. Something is chaining, which is a result rather than a "
            + "regression -- say what, and re-take this reading"));

        // And repetition does not creep towards it either, which is what says the ceiling is
        // structural rather than a matter of evidence.
        Assert.Equal(reached[1], reached[20]);
    }
}
