using System.Globalization;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;

namespace OpenPlexus.Talk;

/// <summary>
/// The conversation harness, wired to a terminal.
/// </summary>
/// <remarks>
/// <para>
/// <b>A deployment is chosen by whoever composes the system</b>, which is why this is a project
/// of its own rather than an entry point on the library. What front end a stream is read through,
/// how big a population is allowed to be and how curious the machine is are all decisions taken
/// here, and a library taking them would be deciding how everything it is ever shown is
/// perceived.
/// </para>
/// <para>
/// <b>Run it with <c>dotnet run --project src/OpenPlexus.Talk</c></b>. Type statements, type
/// questions, leave a line blank to start a new topic, and type <c>.quit</c> to stop.
/// </para>
/// <para>
/// <b>Or hand it a lesson with <c>--lesson creatures</c></b> and a scripted person tells the
/// topic once and then examines, which is the same interface driven by something repeatable.
/// The no-learning bars are printed BEFORE the run, because every wrong turn on this world came
/// from reading a score before the bar it had to beat.
/// </para>
/// </remarks>
internal static class Program
{
    private static int Main(string[] args)
    {
        var capacity = Number(args, "--capacity", 2000);
        // The three arms that were measured to win, shipped rather than left off, and all three
        // are load-bearing: a statement claims every word in turn so that nothing has to pick
        // one, genesis mints the whole remaining scope rather than finding it by failing, and a
        // mint is credited with the round that made it so a correct rule is believed without
        // hearing the sentence twice. Together they answer a lesson told ONCE; any two of them
        // reach a fraction of it. No generated world has weighed in -- see `DialTests`.
        var rooting = Given(args, "--rooting") is { } wide
            ? Enum.Parse<Rooting>(wide, ignoreCase: true)
            : Rooting.Wholly;
        var crediting = Given(args, "--crediting") is { } paid
            ? Enum.Parse<Crediting>(paid, ignoreCase: true)
            : Crediting.Birth;
        var admitting = Given(args, "--admitting") is { } bar
            ? Enum.Parse<Admitting>(bar, ignoreCase: true)
            : Admitting.Testable;
        var seed = Number(args, "--seed", 1);
        var rate = Fraction(args, "--asking", 0.25);
        var passes = Number(args, "--passes", 3);
        var tellings = Number(args, "--tellings", 1);
        var clarifying = Number(args, "--clarifying", 0);
        var revising = Number(args, "--revising", 0);
        var replying = Given(args, "--replying") is { } how
            ? Enum.Parse<Replying>(how, ignoreCase: true)
            : Replying.Word;
        var carrying = Carried(args);
        var joining = Given(args, "--joining") is { } read
            ? Enum.Parse<Joining>(read, ignoreCase: true)
            : Joining.Bagged;
        // Claiming by default, because a statement that claims nothing cannot teach anything
        // and a session at a terminal is mostly statements.
        //
        // And the asking rate stays low FOR THE PERSON rather than for the population. A claim
        // prints an answer and costs nothing; an ask opens a prompt and eats the next line they
        // type, so a machine asking on every moment is one nobody will talk to twice. Pass
        // `--asking 1.0` where the reply is scripted and the settlements are the point.
        var asserting = Given(args, "--asserting") is { } claim
            ? Enum.Parse<Asserting>(claim, ignoreCase: true)
            : Asserting.Everything;
        var lesson = Taught(args);

        // The control, and it is the only thing that says whether the telling taught anything.
        // A machine examined over and over learns the examination by being corrected on it, so
        // an accuracy with no un-told arm beside it cannot be read as comprehension.
        if (lesson is not null && Given(args, "--told") == "no")
            lesson = lesson with { Statements = [] };

        var brain = new Brain(
            new CommittingSettings
            {
                Capacity = capacity,
                Rooting = rooting,
                Crediting = crediting,
                Admitting = admitting,
            },
            seed);

        // The tutor owns the writer, because seeing the prompt is how it knows a reply is
        // wanted. A session with nobody scripted prints straight at the console.
        var tutor = lesson is null ? null : new Tutor(
            lesson, Console.Out, passes, tellings, revising, clarifying, Console.In, replying);

        var world = new Conversing(new ConversingSettings
        {
            Typed = tutor ?? Console.In,
            Printed = tutor?.Printed ?? Console.Out,
            Carrying = carrying,
            Asserting = asserting,

            // Nothing where a person is typing, because a conversation cannot segment
            // itself. A lesson knows which of its words it is about and a keyboard does
            // not, so the terminal reports things exactly where somebody can say what they
            // are.
            Things = tutor?.Things ?? [],
        });

        // Handed in where the world and the brain meet, because which code an outcome is
        // about is a fact only the world holds. Without it `Supposing` is one vote.
        brain.Meaning = world.Meaning;
        var curiosity = new Curiosity(brain, rate, seed, world.Naming);

        // ONE vocabulary for the front end and the population, which is what `Categories`
        // requires: a category the fold puts in a moment and one a scope is rewritten over
        // have to be the same code, or the rewrite names something no moment holds. It starts
        // empty and only ever grows, so nothing a session learns is ever renamed.
        //
        // Company rather than time, and `Rarely` rather than `Never`. A typed sentence is a
        // window rather than one assertion, so two words that are alternatives land in one
        // moment constantly -- the clause that refuses a pair for meeting once returns nought
        // on every text stream measured. And the codes a conversation wants grouped never
        // turn up beside each other; what they share is the company they keep.
        var sorts = new Categories([]);

        brain.Held.Sorts = sorts;

        var bench = new Bench(
            new Watching<Coded>(
                world,
                new Sorted<Coded>(
                    new Deriving<Coded>(
                        new Joined(joining),
                        sorts,
                        Counting.Company,
                        Meeting.Rarely,
                        floor: Number(args, "--floor", 5),
                        every: Number(args, "--deriving", 50)),
                    sorts),
                acting: Chooses.From(
                    felt => Doing(curiosity.Choose(felt)), curiosity.Cleared)),
            brain);

        // Budgeted for the widest statement, because `Asserting.Everything` makes a sentence
        // one moment a word. A run stopping at the moment count would end before the
        // examination; the spare rounds after the lesson go by empty and are reported.
        var rounds = tutor is null
            ? Number(args, "--rounds", 400)
            : asserting is Asserting.Everything || replying is Replying.Sentence
                ? tutor.Moments * tutor.Longest
                : tutor.Moments;

        Console.WriteLine(
            $"talking, {rounds} rounds, capacity {capacity}, seed {seed}, asking "
            + $"{rate.ToString("F2", CultureInfo.InvariantCulture)} of the time, carrying "
            + $"{carrying.ToString().ToLowerInvariant()}, asserting "
            + $"{asserting.ToString().ToLowerInvariant()}, rooting "
            + $"{rooting.ToString().ToLowerInvariant()}, crediting "
            + $"{crediting.ToString().ToLowerInvariant()}, admitting "
            + $"{admitting.ToString().ToLowerInvariant()}, joining "
            + $"{joining.ToString().ToLowerInvariant()}");

        if (lesson is null || tutor is null)
        {
            Console.WriteLine(
                "  a sentence is a moment. end one with `?` to ask, leave a line blank for a "
                + $"new topic, type `{Conversing.Over}` to stop.");
            Console.WriteLine(
                "  the machine says `. word` to claim and `? word` to ask. answer an ask with "
                + "yes, no, or the word.");
        }
        else
        {
            Console.WriteLine(
                $"  lesson: {lesson.About} — {lesson.Statements.Count} statements told {tellings} "
                + $"times, then {lesson.Exam.Count} questions {passes} times over.");

            if (clarifying > 0)
                Console.WriteLine(
                    $"  and {clarifying} moments in between are yours — answer what it asks, "
                    + $"say what you like, `{Tutor.Done}` to move on.");

            // Before anything is run, which is the whole point of printing them here. A score
            // at or under either of these is a reading about the lesson and not about the
            // machine.
            Console.WriteLine(
                $"  bars   : recency {Share(tutor.Recency, lesson.Exam.Count)}, marginal "
                + $"{Share(tutor.Marginal, lesson.Exam.Count)} — both need no learning.");
        }

        Console.WriteLine();

        var tally = bench.Run(rounds, sweep: 200, target: 0.9, window: 50);

        Console.WriteLine();
        Console.WriteLine($"lines      : {tally.Rounds} of {rounds} rounds, ended {world.Ended}");
        Console.WriteLine(
            $"speaking   : {curiosity.Claims} claims, {curiosity.Questions} questions, "
            + $"{curiosity.Silences} with nothing to say");
        Console.WriteLine(
            $"asking     : {world.Asked} asked, {world.Told} answered, {world.Quiet} let go by");
        Console.WriteLine(
            $"settling   : {tally.Right} right, {tally.Wrong} wrong, {tally.Abstained} settled "
            + "nothing");
        Console.WriteLine(
            $"population : {tally.Resident} resident, {tally.Minted} minted, {tally.Repaired} "
            + "repaired");
        Console.WriteLine(
            $"vocabulary : {world.Vocabulary.Count} words — "
            + string.Join(" ", world.Vocabulary));

        // What the derivation cost, beside what it found. A vocabulary that grows without
        // paying is the arm's own refutation, and a group that fills gradually mints a
        // category at every size it passes through.
        Console.WriteLine(
            $"categories : {sorts.Count} groups — "
            + string.Join(" | ", sorts.Groups.Select(group => string.Join(
                " ",
                group.Select(code => world.Naming(code) is { } at
                    ? world.Vocabulary[at]
                    : "?")))));

        if (tutor is not null)
        {
            // One row a pass, because the first pass and the rest answer different questions.
            // The first says whether being TOLD the statements taught anything; the rest say
            // what being CORRECTED teaches, and averaging the two hides both.
            for (var pass = 0; pass < passes; pass++)
                Console.WriteLine(
                    $"pass {pass + 1,-6} : {tutor.Confirmed[pass]} of {tutor.Put[pass]} right, "
                    + $"{Share(tutor.Confirmed[pass], tutor.Put[pass])}");

            Console.WriteLine(
                $"the tutor  : {tutor.Corrected} corrected, {tutor.Shrugged} shrugged at");
        }

        // Which gate refused, and it is printed BEFORE `wanting` because it says whether that
        // number means anything. `Searched` is the only one of the five that reaches the scope
        // language, so a run where nothing reaches it has not learnt that its language is too
        // weak -- it has learnt that its gates are strict, and `wanting` reads 0.000 either way.
        Console.WriteLine(
            $"repair     : {brain.Held.Wrong} wrong, {brain.Held.AtFloor} under the floor, "
            + $"{brain.Held.AtBudget} out of budget, {brain.Held.AtCovered} already covered, "
            + $"{brain.Held.AtImproving} not improving, {brain.Held.Searched} searched");

        // What the ladder could not do, which is the reading this harness exists for. A high
        // share here is the admission rule firing on typed English: the machine was blamed and
        // nothing in the scope language told the misses from the hits.
        Console.WriteLine(
            $"wanting    : {tally.Wanting.ToString("F3", CultureInfo.InvariantCulture)} of "
            + $"{tally.Blamed} blamed rounds nothing separated"
            + (tally.Blamed == 0 ? " -- nothing was ever asked, so this says nothing" : ""));

        return 0;
    }

    /// <summary>An action index for what the machine decided to say.</summary>
    /// <remarks>
    /// <b>The join</b>, and it is one line because that is all the coupling there is. A chooser
    /// hands back a word and an intent; how a world numbers its doings is that world's business,
    /// and this is where the two meet.
    /// </remarks>
    private static int? Doing(Wondered said) =>
        said.Word is not { } word
            ? null
            : said.Asking ? Conversing.Asks(word) : Conversing.Asserts(word);

    /// <summary>Which lesson to be told, or nothing to be typed at.</summary>
    private static Lesson? Taught(string[] args) =>
        Given(args, "--lesson") is not { } named
            ? null
            : string.Equals(named, "creatures", StringComparison.OrdinalIgnoreCase)
                ? Lesson.Creatures
                : string.Equals(named, "corrected", StringComparison.OrdinalIgnoreCase)
                    ? Lesson.Corrected
                    : string.Equals(named, "chained", StringComparison.OrdinalIgnoreCase)
                        ? Lesson.Chained
                        : throw new ArgumentException(
                            $"no lesson called `{named}`", nameof(args));

    /// <summary>How much of the topic a moment holds.</summary>
    /// <remarks>
    /// <b>Bare by default, because the other two are measured to mint nothing.</b> A moment
    /// carrying the topic so far leaves every word said always-present, and genesis may not
    /// root on a code that has never been absent — so a session that accumulates never starts
    /// a population at all. <c>LessonTests</c> holds the reading.
    /// </remarks>
    private static Carrying Carried(string[] args) =>
        Given(args, "--carrying") is not { } named
            ? Carrying.Never
            : Enum.Parse<Carrying>(named, ignoreCase: true);

    private static string Share(int of, int over) => over == 0
        ? "0.000"
        : (of / (double)over).ToString("F3", CultureInfo.InvariantCulture);

    private static int Number(string[] args, string named, int fallback) =>
        Given(args, named) is { } value
            ? int.Parse(value, CultureInfo.InvariantCulture)
            : fallback;

    private static double Fraction(string[] args, string named, double fallback) =>
        Given(args, named) is { } value
            ? double.Parse(value, CultureInfo.InvariantCulture)
            : fallback;

    private static string? Given(string[] args, string named)
    {
        for (var at = 0; at < args.Length - 1; at++)
            if (string.Equals(args[at], named, StringComparison.Ordinal))
                return args[at + 1];

        return null;
    }
}
