using OpenPlexus.Codes;
using OpenPlexus.Commitments;
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

        var priced = Priced(sat);

        foreach (var kind in priced.Keys.Order(StringComparer.Ordinal))
        {
            var one = priced[kind];

            output.WriteLine(
                $"{kind,-6}{one.Asked,8}{one.Marginal,10:F3}{one.Noun,10:F3}{one.Present,10:F3}"
                + $"{one.Latest,10:F3}");

        }

        // Four kinds, or the survey is one question wearing four names. What makes an exam
        // of a walk is that no single rule answers all of it.
        Assert.Equal(4, priced.Count);

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

    /// <summary>What one kind of question is worth to a rule that never looked.</summary>
    /// <param name="Asked">How many were put.</param>
    /// <param name="Marginal">The commonest answer of the kind.</param>
    /// <param name="Noun">The commonest answer for the noun the question names.</param>
    /// <param name="Present">How often the answer is somewhere in the moment.</param>
    /// <param name="Latest">The most recent word of the answer's own kind.</param>
    public readonly record struct Blind(
        int Asked, double Marginal, double Noun, double Present, double Latest);

    /// <summary>
    /// The blind rules priced per kind, off the questions a run put.
    /// </summary>
    /// <param name="sat">Each question's kind, the noun it named, its answer and its moment.</param>
    /// <remarks>
    /// One computation for both readings here, because two copies of a bar are two bars that
    /// drift. The answer alphabet of a kind is read off what was asked rather than declared,
    /// since a list written into a test is the world's answer key copied somewhere it can go
    /// stale.
    /// </remarks>
    private static IReadOnlyDictionary<string, Blind> Priced(
        IReadOnlyList<(string Kind, Code About, Code Answer, Coded Seen)> sat)
    {
        var priced = new Dictionary<string, Blind>(StringComparer.Ordinal);

        foreach (var kind in sat.Select(one => one.Kind).Distinct().Order(StringComparer.Ordinal))
        {
            var these = sat.Where(one => one.Kind == kind).ToList();
            var alphabet = new HashSet<Code>(these.Select(one => one.Answer));

            priced[kind] = new Blind(
                these.Count,
                these.GroupBy(one => one.Answer).Max(one => one.Count())
                    / (double)these.Count,
                these.GroupBy(one => one.About)
                    .Sum(group => group.GroupBy(one => one.Answer).Max(one => one.Count()))
                    / (double)these.Count,
                these.Count(one => one.Seen.Codes.Contains(one.Answer)) / (double)these.Count,
                these.Count(one => Latest(one.Seen, alphabet) == one.Answer)
                    / (double)these.Count);
        }

        return priced;
    }

    /// <summary>
    /// The world the SPINE runs, named once so a reading cannot be taken on another.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A hundred and twenty steps, six questions and six rounds of conversation nobody takes
    /// up, which is what <c>RoamingTests</c>'s dial grid walks and what the deployment talks
    /// to. Named here for the reason <see cref="Fronting"/> is: a session's readings were taken
    /// at forty steps with no conversation and a doubled capacity, and every one of them was
    /// about a machine nobody runs.
    /// </para>
    /// <para>
    /// The person is quiet, so the conversation is six moments of an invitation nobody
    /// answers. That is the arm this world is watched under, and it keeps the acting channel
    /// out of a reading about the population.
    /// </para>
    /// </remarks>
    public static RoamingSettings Arming()
    {
        var quiet = new Person();

        return new RoamingSettings
        {
            Rooms = 6,
            Props = 4,
            People = 2,
            Steps = 120,
            Asked = 6,
            Chatting = 6,
            Typed = quiet,
            Printed = quiet.Printed,
        };
    }

    /// <summary>The brain the spine runs, which is its own defaults and nothing else.</summary>
    /// <remarks>
    /// A capacity written into a test is a dial the deployment does not turn, and a population
    /// cap decides how much of what repair mints survives — so a reading taken at twice the
    /// default is a reading about a different machine.
    /// </remarks>
    internal static CommittingSettings Dialling() => new();

    /// <summary>How many rounds the spine is run for.</summary>
    public const int Running = 10_000;

    /// <summary>
    /// The front end the spine SHIPS, named once so a reading cannot be taken on another.
    /// </summary>
    /// <remarks>
    /// <para>
    /// `OpenPlexus.Talk` composes this and it is what the deployment runs. Naming it here is
    /// the same discipline <see cref="Arming"/> carries for the world's size, and it is here
    /// for the same reason: a whole session's readings were taken on <c>Bagged</c>, copied out
    /// of a control that uses it deliberately, and every number was about a machine nobody
    /// ships.
    /// </para>
    /// <para>
    /// The resolution and the freshest flag are part of it. A store read at depth nought is a
    /// different mechanism from one read at three, so a front end named without them is half
    /// a name.
    /// </para>
    /// </remarks>
    public static Joined Fronting() =>
        new(Joining.Resolved, resolution: 3, freshest: true);

    /// <summary>
    /// The front end named here is the one the deployment composes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The check this file needed and did not have. A whole session's target-world readings
    /// were taken on <c>Bagged</c>, copied out of a control that uses it deliberately, while
    /// the terminal ships <c>Resolved</c> at depth three — so the machine that was measured
    /// and the machine that runs were two machines, and the headline moved when they were made
    /// one.
    /// </para>
    /// <para>
    /// It reads the terminal's own source, which is what <c>ExercisedTests.BrainsApart</c>
    /// already does for the brain's dials. That guard covers a dial the terminal turns and the
    /// walk defaults; this covers the seam one step out, where the front end is composed.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_front_end_measured_here_is_the_one_the_terminal_ships()
    {
        var source = File.ReadAllText(
            Path.Combine(Tree.Repo(), "src", "OpenPlexus.Talk", "Program.cs"));

        var shipped = source.Contains(
            "new Joined(joining, resolution: 3, freshest: true)", StringComparison.Ordinal)
            && source.Contains(": Joining.Resolved;", StringComparison.Ordinal);

        output.WriteLine(
            shipped
                ? "the terminal composes Resolved at depth three, freshest, and so does this"
                : "the terminal composes something else");

        Assert.True(shipped,
            "`OpenPlexus.Talk` no longer composes `Joining.Resolved` at resolution three with "
            + "`freshest`, which is what `Fronting` returns and what every reading in this file "
            + "is taken on. A measurement on a front end the deployment does not run is a "
            + "measurement of a machine nobody has. Move `Fronting` to whatever ships.");
    }

    /// <summary>How many houses a seed, and how many seeds, every reading here uses.</summary>
    public const int Houses = 40;

    /// <summary>How many seeds.</summary>
    public const int Seeds = 3;

    /// <summary>
    /// What a machine that never looked at this house can reach, per kind and weighted.
    /// </summary>
    /// <remarks>
    /// Three rules, none of which learns anything: say the commonest answer of the kind, say
    /// the commonest answer for the noun the question names, and say the most recent word of
    /// the answer's own kind. An arm is worth reading only above the highest of them.
    /// </remarks>
    public static (double Marginal, double Noun, double Latest,
        IReadOnlyDictionary<string, Blind> Kinds) Bars()
    {
        var sat = new List<(string Kind, Code About, Code Answer, Coded Seen)>();

        for (var seed = 1; seed <= Seeds; seed++)
        {
            var world = new Roaming(Arming(), seed);

            var words = new Dictionary<Code, string>();

            for (var one = 0; one < world.Vocabulary.Count; one++)
                words[world.Meaning(one)!.Value] = world.Vocabulary[one];

            for (var round = 0; round < Running; round++)
            {
                var turn = world.Next();

                if (turn.Seen.Asked is not { } question) continue;
                if (!world.Sat) continue;

                sat.Add((
                    words[question.Codes[0]],
                    question.Codes[^1],
                    world.Meaning(turn.Outcome!.Value)!.Value,
                    turn.Seen));
            }
        }

        var kinds = Priced(sat);

        var weighted = (Marginal: 0.0, Noun: 0.0, Latest: 0.0);

        foreach (var one in kinds.Values)
            weighted = (
                weighted.Marginal + (one.Marginal * one.Asked),
                weighted.Noun + (one.Noun * one.Asked),
                weighted.Latest + (one.Latest * one.Asked));

        return (
            weighted.Marginal / sat.Count,
            weighted.Noun / sat.Count,
            weighted.Latest / sat.Count,
            kinds);
    }

    /// <summary>
    /// What the learner scores on the same stream, per kind, with its silence counted.
    /// </summary>
    /// <remarks>
    /// The machine says nothing to the house, which is the arm measured best on this world, so
    /// the acting channel is out of the comparison and the transcript is the world's alone.
    /// </remarks>
    public static async Task<(double Score, double Spoken, int Asked, int Silent,
        IReadOnlyDictionary<string, (int Asked, int Right)> Kinds)> Scored()
    {
        var total = (Asked: 0, Right: 0, Silent: 0);
        var kinds = new Dictionary<string, (int Asked, int Right)>(StringComparer.Ordinal);

        for (var seed = 1; seed <= Seeds; seed++)
        {
            var house = new Roaming(Arming(), seed);
            var brain = new Brain(Dialling(), seed);

            var words = new Dictionary<Code, string>();

            for (var one = 0; one < house.Vocabulary.Count; one++)
                words[house.Meaning(one)!.Value] = house.Vocabulary[one];

            var watching = new Watching<Coded>(
                house, Fronting(), acting: Chooses.From(_ => null));

            var loop = new Round(brain, Running, sweep: 1000, target: 0.9, window: 2000);

            for (var round = 0; round < Running; round++)
            {
                // Read BEFORE the push, because `Roaming.Now` builds the moment about to be
                // shown rather than the one just answered. Reading it after attributed every
                // answer to the NEXT question and dropped a sixth of them, which flipped this
                // grid's per-kind ordering while the total stayed put.
                var question = house.Now.Asked;

                if (watching.Push() is not { } pushed) continue;

                var was = (loop.Right, loop.Silent);

                await loop.StepAsync(pushed);

                if (!house.Sat) continue;

                total.Asked++;

                var hit = loop.Right > was.Right;

                if (hit) total.Right++;
                if (loop.Silent > was.Silent) total.Silent++;

                if (question is not { } put) continue;

                var kind = words[put.Codes[0]];
                var had = kinds.GetValueOrDefault(kind);

                kinds[kind] = (had.Asked + 1, had.Right + (hit ? 1 : 0));
            }
        }

        return (
            total.Right / (double)total.Asked,
            total.Right / (double)Math.Max(total.Asked - total.Silent, 1),
            total.Asked,
            total.Silent,
            kinds);
    }

    /// <summary>
    /// The blind bar and the learner's score on one stream, for whoever needs the pair.
    /// </summary>
    /// <remarks>
    /// Read by <see cref="OutstandingTests"/>'s deadline as well as printed here, because a
    /// bar written down in two places is two bars that drift apart.
    /// </remarks>
    public static async Task<(double Blind, double Learner, int Silent)> Against()
    {
        var scored = await Scored();

        return (Bars().Noun, scored.Score, scored.Silent);
    }

    /// <summary>
    /// The blind bars on the settings the arms run at, printed per kind.
    /// </summary>
    [Fact]
    public void What_the_survey_is_worth_on_the_settings_the_arms_run_at()
    {
        var bars = Bars();

        output.WriteLine(
            $"{Running} rounds a seed over {Seeds} seeds, the spine's own world");

        output.WriteLine(
            $"{"kind",-6}{"asked",8}{"marginal",10}{"by noun",10}{"latest",10}");

        foreach (var kind in bars.Kinds.Keys.Order(StringComparer.Ordinal))
        {
            var one = bars.Kinds[kind];

            output.WriteLine(
                $"{kind,-6}{one.Asked,8}{one.Marginal,10:F3}{one.Noun,10:F3}{one.Latest,10:F3}");
        }

        output.WriteLine(
            $"{"all",-6}{bars.Kinds.Values.Sum(one => one.Asked),8}"
            + $"{bars.Marginal,10:F3}{bars.Noun,10:F3}{bars.Latest,10:F3}");

        Assert.Equal(4, bars.Kinds.Count);

        // The by-noun bar is the sharper of the two blind ones, or the pair is computed
        // wrongly. Arithmetic rather than a finding: knowing the noun cannot make a guess
        // worse, and this is asserted so the two cannot drift.
        Assert.True(bars.Noun >= bars.Marginal,
            $"the noun-conditioned bar is {bars.Noun:F3} against a marginal of "
            + $"{bars.Marginal:F3}, so one of the two is computed wrongly");
    }

    /// <summary>
    /// What the learner scores against those bars, per kind, on one stream.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The comparison this project has not had. Every reading here is an arm against another
    /// arm at one instant, and the question a bar answers is a different one: is the machine
    /// worth more than a rule that never looked at the house.
    /// </para>
    /// <para>
    /// Silence is the confound and is why the spoken half is printed. A blind rule always
    /// answers; the machine may decline, and a declined round scores nothing. So a score under
    /// the bar means one of two things and the split says which.
    /// </para>
    /// <para>
    /// Per kind as well, because a total hides which question the machine loses on and one of
    /// the four is at the language's ceiling before anything runs — a conjunction of codes
    /// cannot say <i>two of these</i>.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task What_the_learner_scores_against_a_rule_that_never_looked()
    {
        var bars = Bars();
        var scored = await Scored();

        output.WriteLine(
            $"{Running} rounds a seed over {Seeds} seeds, the spine's own world");

        output.WriteLine(
            $"{"kind",-6}{"asked",8}{"learner",10}{"marginal",10}{"by noun",10}");

        foreach (var kind in scored.Kinds.Keys.Order(StringComparer.Ordinal))
        {
            var mine = scored.Kinds[kind];
            var bar = bars.Kinds[kind];

            output.WriteLine(
                $"{kind,-6}{mine.Asked,8}{mine.Right / (double)mine.Asked,10:F3}"
                + $"{bar.Marginal,10:F3}{bar.Noun,10:F3}");
        }

        output.WriteLine(
            $"{"all",-6}{scored.Asked,8}{scored.Score,10:F3}{bars.Marginal,10:F3}"
            + $"{bars.Noun,10:F3}");

        output.WriteLine(
            $"silent on {scored.Silent} of {scored.Asked}, so {scored.Spoken:F3} where it spoke");

        Assert.Equal(4, scored.Kinds.Count);

        // Every question the exam put is attributed to a kind, or the split is about whichever
        // ones the reader caught. This is what found the misattribution: reading the kind after
        // the step lost 120 of 720 and inverted the ordering.
        Assert.Equal(scored.Asked, scored.Kinds.Values.Sum(one => one.Asked));

        // And silence has to be small enough that the two halves answer one question, or the
        // learner and the bar are not comparable and the deadline reading this is about
        // abstention rather than about accuracy.
        Assert.True(scored.Silent < scored.Asked / 10,
            $"the machine declined {scored.Silent} of {scored.Asked}, so its score and a rule "
            + "that always answers are not the same measurement");
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
