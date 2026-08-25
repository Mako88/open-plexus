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
/// <b>So this measures what a judgement cannot</b>: whether the answer is already there. A
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
    private static (double Present, int Asked) Ceiling(Joining joining, int task) =>
        Ceiling(new Joined(joining), task);

    /// <summary>The same, for a front end that is not one arm.</summary>
    /// <param name="sensing">The translation between the story and the brain.</param>
    /// <param name="task">Which bAbI task.</param>
    /// <remarks>
    /// <b>A front end rather than an enum value</b>, so a composition can be priced by the
    /// instrument that prices the arms. Nothing here knows how the moment was made.
    /// </remarks>
    private static (double Present, int Asked) Ceiling(IQuantizer<Coded> sensing, int task)
    {
        var world = new Recalled(new RecalledSettings
        {
            Corpus = Tree.Babi(),
            Task = task,
            Predicting = Predicting.Asked,

            // Enough held back to have an examination, and the same slice for every arm.
            Withheld = 20,
        });

        var watching = new Watching<Coded>(world, sensing);

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

    /// <summary>
    /// How often a walked house has already handed over the word it is about to ask for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The target world's own ceiling, and a look is never a name.</b> What a thing
    /// looks like and what it is called are two codes, so the answer to <i>which of these is
    /// being named</i> has to be joined rather than read — which is the problem a picture will
    /// pose, reached rather than designed away.
    /// </para>
    /// <para>
    /// <b>And it is not nought either</b>, which is what makes the crossing learnable rather
    /// than impossible. A thing named once keeps its word in the transcript, so meeting it
    /// again puts its look and its name in one moment, and that co-firing is the whole of
    /// what a crossing has to be built out of.
    /// </para>
    /// <para>
    /// <b>Read on the bag</b>, because a front end is not what is being priced. A selecting
    /// front end would put its own reading between the world and this number, and what is
    /// wanted is what the world hands over.
    /// </para>
    /// <para>
    /// <b>What the reading says is where the crossing BITES</b>: a thing's first meeting, and
    /// nothing else. A house of six rooms re-meets what it has already been told the name of
    /// most of the time, so the gap between this and one is the whole of what is hard — and a
    /// walk whose moment did not carry the house behind it would price it quite differently.
    /// </para>
    /// <para>
    /// <b>An arm that made it ONE lived here and is deleted.</b> A world whose look and word
    /// were one code read 1.000 against 0.890, and it was a switch on the target world rather
    /// than a size — so it went with the other two. What is left is the absolute reading,
    /// which is the half that could ever fail.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_a_walked_house_hands_over_of_the_word_it_will_ask_for()
    {
        const int Rounds = 2_000;

        var front = new Joined(Joining.Bagged);

        var world = new Roaming(
            Fixture.House(), seed: 1);

        var present = 0;
        var settled = 0;

        for (var round = 0; round < Rounds; round++)
        {
            var turn = world.Next();

            if (turn.Outcome is not { } outcome) continue;

            settled++;

            // The answer as the front end would have to see it, which is the word the
            // outcome names rather than the outcome code itself.
            if (world.Meaning(outcome) is { } answer
                && front.Codify(turn.Seen).Contains(answer))
            {
                present++;
            }
        }

        var handed = present / (double)settled;

        output.WriteLine($"a walked house, {Rounds} steps, nobody choosing");
        output.WriteLine($"the answer was already in the moment {handed:F3} of {settled}");

        // Below one, or the world hands the answer over every time and nothing below it can
        // be read as learning at all. This is the check the class exists for.
        Assert.True(handed < 1.0,
            $"the walked house handed the answer over on every one of {settled} settled "
            + "rounds, so naming what is in front of it is reading rather than crossing and "
            + "no score off this world is about a learner");

        // And above nothing, or the crossing has nothing to be learnt from: a look and a name
        // that never share a moment can never come to be joined by counting.
        Assert.True(handed > 0.0,
            "a look and the word for it never once arrived together, so no co-firing could "
            + "ever join them and the crossing is unlearnable rather than hard");
    }

    /// <summary>
    /// <b>What a compound of two readings of ONE sense costs</b>, before anything has learnt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A moment is a union, so an answer-present set is a union.</b> Each channel reads
    /// the same signal and emits into the same alphabet, so the answer is in the compound
    /// moment whenever it was in either channel's — which makes the ceiling of a compound at
    /// least the highest of its channels, and higher wherever they hand it over on different
    /// occasions. That is arithmetic and not a measurement, and this is where it is asserted.
    /// </para>
    /// <para>
    /// <b>And what it prices is the proposal rather than the type.</b>
    /// <see cref="Compound{TFrame}"/> exists for a body with several SENSES, where the
    /// channels emit into disjoint modalities and the outcome lives in one — so a second
    /// sense cannot add the answer and this reading does not touch it. What it refuses is
    /// several readings of one sense, where the alphabets are shared.
    /// </para>
    /// <para>
    /// <b>Which is the bag by a longer road.</b> <see cref="Joining.Bagged"/> is the control
    /// that hands everything over and reads 1.000 here; adding channels walks toward it
    /// monotonically, and three selecting arms reach the bag's width at a fraction of its
    /// ceiling. The refutation is in the commit that deleted the arm.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_compound_of_one_sense_never_lowers_the_ceiling_and_usually_raises_it()
    {
        const int Task = 2;

        // The three arms that SELECT. The bag hands the answer over every time, so a pair
        // holding it is 1.000 whatever the other channel does and says nothing about
        // composition.
        var selecting = new[] { Joining.Distinguished, Joining.Chained, Joining.Resolved };

        var alone = selecting.ToDictionary(one => one, one => Ceiling(one, Task).Present);

        output.WriteLine($"bAbI task {Task}, twenty stories withheld");
        output.WriteLine($"{"channels",-34}{"answer present",16}");

        foreach (var one in selecting)
            output.WriteLine($"{one.ToString().ToLowerInvariant(),-34}{alone[one],16:F3}");

        var raised = 0;

        for (var first = 0; first < selecting.Length; first++)
            for (var second = first + 1; second < selecting.Length; second++)
            {
                var pair = new[] { selecting[first], selecting[second] };

                var present = Ceiling(
                    new Compound<Coded>(pair.Select(one => new Joined(one))), Task).Present;

                var best = pair.Max(one => alone[one]);

                output.WriteLine($"{string.Join('+', pair),-34}{present,16:F3}");

                Assert.True(present >= best,
                    $"{string.Join('+', pair)} hands the answer over {present:F3} of the time "
                    + $"and its best channel alone reaches {best:F3}. A moment is a union, so "
                    + "this cannot happen -- something is dropping codes at the merge.");

                if (present > best) raised++;
            }

        // And the check can fire. Two channels that hand the answer over on exactly the same
        // occasions would raise nothing, so a reading where none of the three pairs rose
        // would mean the arms are the same selection under three names.
        Assert.Equal(3, raised);
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
            Things = lesson.Things,
        });

        var present = 0;
        var asked = 0;

        foreach (var one in lesson.Exam)
        {
            var turn = world.Next();

            if (world.Ended) break;

            world.Do(null);
            asked++;

            var moment = new HashSet<Code>(turn.Seen.Question());

            foreach (var said in turn.Seen.Said()) moment.UnionWith(said);

            if (moment.Contains(Babi.Of(one.Answer))) present++;
        }

        output.WriteLine($"{present} of {asked} bare questions already hold their answer");

        Assert.Equal(lesson.Exam.Count, asked);
        Assert.Equal(0, present);
    }

    /// <summary>
    /// <b>What the walked house's survey is worth before anything has learnt.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Taken before a learner runs, which is this repo's own ordering.</b> A grid cannot
    /// tell a rule that dropped the wrong sentence from a learner that failed to use the
    /// right one, and an exam whose answers are already lying in the moment prices the front
    /// end rather than the population.
    /// </para>
    /// <para>
    /// <b>The marginal is the control the survey was given.</b> A machine that walked ANOTHER
    /// house reads this exam's question and its own transcript, and its transcript says
    /// nothing about this house — so the best it can do is the commonest answer of the kind
    /// it was asked. Two are printed: over the kind, and over the kind and the noun the
    /// question names, which is the sharper of the two and the one an arm has to beat.
    /// </para>
    /// <para>
    /// <b>And the recency rule is the roof the WALK puts on it.</b> Answering with the most
    /// recent word of the answer's own kind is what a bag reads straight off, and the gap
    /// between it and one is what moved after the machine last looked.
    /// </para>
    /// <para>
    /// <b>The counting kind reads nought on both, and it is meant to.</b> A number
    /// word is never said by the house, so it is never in the moment and no recency rule can
    /// reach it — and a conjunction of codes cannot say <i>two of these</i> either, which is
    /// Monk-2's own ceiling arriving on the spine world. Leaving the kind out would be
    /// editing the exam until it could be passed.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_the_walked_houses_survey_is_worth_before_anything_learns()
    {
        const int Rounds = 12_600;
        const int Asked = 6;

        var world = new Roaming(
            Fixture.House(asked: Asked),
            seed: 1);

        // What the world SAID, never what to conclude. The same standing as `Named`: a probe
        // is allowed to know which codes are this world's words, and nothing that learns is
        // ever shown it.
        var words = new Dictionary<Code, string>();

        for (var one = 0; one < world.Vocabulary.Count; one++)
            words[world.Meaning(one)!.Value] = world.Vocabulary[one];

        var sat = new List<(string Kind, Code About, Code Answer, Coded Seen)>();

        for (var round = 0; round < Rounds; round++)
        {
            var turn = world.Next();

            if (turn.Seen.Asked is not { } question) continue;

            sat.Add((
                words[question.Codes[0]],
                question.Codes[^1],
                world.Meaning(turn.Outcome!.Value)!.Value,
                turn.Seen));
        }

        output.WriteLine(
            $"a walked house, {Rounds} steps, {Asked} questions an exam, nobody choosing");

        output.WriteLine(
            $"{"kind",-6}{"asked",8}{"marginal",10}{"by noun",10}{"present",10}{"latest",10}");

        var priced = new Dictionary<string, (int Asked, double Present, double Latest)>();

        foreach (var kind in sat.Select(one => one.Kind).Distinct().Order(StringComparer.Ordinal))
        {
            var these = sat.Where(one => one.Kind == kind).ToList();

            // The answer alphabet of this kind, read off what was asked rather than declared
            // here. A list written into the test would be the world's own answer key copied
            // into a second place for the two to drift apart.
            var alphabet = new HashSet<Code>(these.Select(one => one.Answer));

            var marginal = these
                .GroupBy(one => one.Answer)
                .Max(one => one.Count()) / (double)these.Count;

            var noun = these
                .GroupBy(one => one.About)
                .Sum(group => group.GroupBy(one => one.Answer).Max(one => one.Count()))
                / (double)these.Count;

            var present = these.Count(one => one.Seen.Codes.Contains(one.Answer))
                / (double)these.Count;

            var latest = these.Count(one => Latest(one.Seen, alphabet) == one.Answer)
                / (double)these.Count;

            output.WriteLine(
                $"{kind,-6}{these.Count,8}{marginal,10:F3}{noun,10:F3}{present,10:F3}"
                + $"{latest,10:F3}");

            priced[kind] = (these.Count, present, latest);
        }

        // Three kinds, or the survey is one question wearing three names. What makes an exam
        // of a walk is that no single rule answers all of it.
        Assert.Equal(3, priced.Count);

        Assert.All(priced.Values, one => Assert.True(one.Asked > 0));

        // And the counting kind is at the language's ceiling before anything runs: a number
        // word is never said by the house, so it is in no moment and no recency rule reaches
        // it. This is the falsifiable half -- a house that started saying its own counts
        // would fail here.
        Assert.Equal(0.0, priced["how"].Present);
        Assert.Equal(0.0, priced["how"].Latest);

        // And the other two are not free either, or the exam is answered by the transcript
        // and a score off it says nothing about what was understood.
        Assert.True(priced["where"].Latest < 1.0 && priced["what"].Latest < 1.0,
            "the most recent word of the answer's own kind answers the whole exam, so the "
            + "survey is recency wearing an exam's clothes");
    }

    /// <summary>The most recent code of one alphabet in a moment's statements.</summary>
    /// <param name="seen">The moment.</param>
    /// <param name="alphabet">The codes that could be an answer of this kind.</param>
    /// <remarks>
    /// <b>Newest statement first and the last such code within it</b>, which is what recency
    /// means where a statement is several words. A sighting names the room it is of before
    /// the things in it, so the last match is the one nearest the moment.
    /// </remarks>
    private static Code? Latest(Coded seen, IReadOnlySet<Code> alphabet)
    {
        foreach (var statement in seen.Statements ?? [])
            for (var at = statement.Codes.Count - 1; at >= 0; at--)
                if (alphabet.Contains(statement.Codes[at])) return statement.Codes[at];

        return null;
    }
}
