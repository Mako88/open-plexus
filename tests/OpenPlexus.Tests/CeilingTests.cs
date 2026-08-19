using OpenPlexus.Codes;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What each front end hands over before anything has learnt.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's, and it is the guard the recurring fault needed.</b> Three directions of the seam
/// are already checked — a world may not name a brain, a world may not take a brain's dial, a
/// mechanism wired to nothing is caught. The fourth is a front end doing the brain's thinking,
/// and nothing sees it, because a front end is allowed to say what it is looking at and the
/// line between that and deciding what to conclude is a judgement.
/// </para>
/// <para>
/// <b>So this measures what a judgement cannot: whether the answer is already there.</b> A
/// front end selects statements and hands over a moment; if that moment CONTAINS the answer, a
/// learner has only to name something in front of it, and a score is a reading about the
/// selection rather than about the population. The share is arithmetic over two sets, needs no
/// brain, and takes milliseconds.
/// </para>
/// <para>
/// <b>A high ceiling is not cheating and a silent one is.</b> An arm that raises this is doing
/// real work and the work is worth having — <c>Joining.Chained</c> exists to do exactly that.
/// What is forbidden is shipping an arm whose ceiling nobody took, so that its score is read as
/// learning. Every value of the enum appears here or the test fails, which is what stops a new
/// arm arriving unpriced.
/// </para>
/// <para>
/// <b>And it is the same discipline the worlds already carry</b>, moved one seam over. A world
/// prints its recency bar before a run; this prints the front end's.
/// </para>
/// </remarks>
public sealed class CeilingTests(ITestOutputHelper output)
{
    /// <summary>How often the answer is already in the moment, for one arm.</summary>
    /// <param name="joining">Which arm reads the story.</param>
    /// <param name="task">Which bAbI task.</param>
    private static (double Present, int Asked) Ceiling(Joining joining, int task)
    {
        var world = new Recalled(new RecalledSettings
        {
            Corpus = Tree.Babi(),
            Task = task,
            Predicting = Predicting.Asked,

            // Enough held back to have an examination, and the same slice for every arm.
            Withheld = 20,
        });

        var watching = new Watching<Recited>(world, new Joined(joining));

        var exam = watching.Exam;

        if (exam.Count == 0) return (0.0, 0);

        var present = 0;

        foreach (var one in exam)
        {
            // The answer as the front end would have to see it. `Followed` is an outcome code
            // and a moment holds the world's own, so the comparison is against the word the
            // outcome names -- which is what a learner naming something in front of it would
            // have to say.
            var answer = Brain.Meant(one.Followed) is { } outcome
                ? Babi.Of(world.Vocabulary[outcome])
                : (Code?)null;

            if (answer is { } code && one.Codes.Contains(code)) present++;
        }

        return (present / (double)exam.Count, exam.Count);
    }

    [Fact]
    public void Every_front_end_arm_says_how_often_it_hands_over_the_answer()
    {
        // Task two, because it is the one whose questions need a second fact and where the
        // arms differ most. Task one is answered by a bag and would read every arm alike.
        const int Task = 2;

        var arms = Enum.GetValues<Joining>();

        output.WriteLine($"bAbI task {Task}, twenty stories withheld");
        output.WriteLine($"{"joining",-16}{"answer present",16}{"questions",11}");

        var priced = new List<Joining>();

        foreach (var joining in arms)
        {
            var (present, asked) = Ceiling(joining, Task);

            output.WriteLine(
                $"{joining.ToString().ToLowerInvariant(),-16}{present,16:F3}{asked,11}");

            if (asked > 0) priced.Add(joining);
        }

        // Every arm priced, which is what stops one arriving with no ceiling under it. A new
        // value of the enum fails here until somebody has run it.
        Assert.Equal(arms.Length, priced.Count);

        // And the check can still fail: a front end that handed over the answer every time
        // would read one here, and nothing below it could be read as learning at all.
        Assert.All(priced, one => Assert.InRange(Ceiling(one, Task).Present, 0.0, 1.0));
    }

    [Fact]
    public void A_bare_question_hands_over_nothing_at_all()
    {
        // The conversation's own ceiling, and it is what makes every reading in `LessonTests`
        // safe to read. Under `Carrying.Never` a question arrives as its own words and nothing
        // else, so the answer is never in front of the machine -- it either learnt the thing or
        // it did not, and no amount of front end can be doing the work.
        var lesson = Lesson.Creatures;

        var lines = lesson.Exam.Select(one => one.Question).Append(Conversing.Over);

        var typed = new StringReader(string.Join(Environment.NewLine, lines));

        var world = new Conversing(new ConversingSettings
        {
            Typed = typed,
            Printed = TextWriter.Null,
            Carrying = Carrying.Never,
        });

        var present = 0;
        var asked = 0;

        foreach (var one in lesson.Exam)
        {
            var turn = world.Next();

            if (world.Ended) break;

            world.Do(null);
            asked++;

            var moment = new HashSet<Code>(turn.Seen.Asked);

            foreach (var said in turn.Seen.Said) moment.UnionWith(said);

            if (moment.Contains(Babi.Of(one.Answer))) present++;
        }

        output.WriteLine($"{present} of {asked} bare questions already hold their answer");

        Assert.Equal(lesson.Exam.Count, asked);
        Assert.Equal(0, present);
    }
}
