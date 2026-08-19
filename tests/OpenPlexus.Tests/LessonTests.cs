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
        Lesson lesson, Carrying carrying, int seed, int passes, int capacity = 2000)
    {
        var tutor = new Tutor(lesson, TextWriter.Null, passes);

        var brain = new Brain(new CommittingSettings { Capacity = capacity }, seed);

        var world = new Conversing(new ConversingSettings
        {
            Typed = tutor,
            Printed = tutor.Printed,
            Carrying = carrying,
        });

        var curiosity = new Curiosity(brain, rate: 1.0, seed, world.Naming);

        // Named, because the front end is the next arm this file will want and a bench holding
        // it inline is a wiring nobody can vary.
        var front = new Joined(Joining.Bagged);

        var watching = new Watching<Recited>(
            world, front, acting: felt => Speaking(curiosity.Choose(felt)));

        var tally = new Bench(watching, brain)
            .Run(tutor.Moments, sweep: 200, target: 0.9, window: 50);

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
    public void Being_told_the_statements_teaches_the_machine_nothing()
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

        // And the telling buys none of it, which is the finding. A statement carries no
        // settlement, so a round spent being told one moves no counter and the machine that
        // heard the statements is exactly the machine that did not.
        //
        // This fails the day being told changes something, which is the point of asserting it.
        Assert.True(told.Sum() <= without.Sum() + 0.5,
            $"told totalled {told.Sum():F3} over the passes and untold {without.Sum():F3}. "
            + "The statements have started to teach something, which is a result rather than "
            + "a regression — re-take this reading and say what changed.");
    }
}
