using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whether a chain the question never named is in the population — <b>the ceiling on fork
/// 115.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Reading a commitment BACKWARDS is the proposal.</b> Take the code a question wants,
/// collect the commitments that expect it, and ask what their scopes are missing; where a
/// missing word is itself concluded by a rule that does fire, that is a chain. The search
/// starts at the answer, so the question not naming the intermediate is no obstacle.
/// </para>
/// <para>
/// <b>Which is exactly what killed forward chaining.</b> Three shapes were built and refuted
/// together, with a bridge between an outcome code and the world's own already in place, and
/// the diagnosis was that answering <i>what is the cat loudness</i> needs a rule whose scope
/// holds <c>sound</c> — a word the question never says. Making every conclusion live then put
/// most of the vocabulary in the moment, so the second vote was the first vote with noise.
/// </para>
/// <para>
/// <b>So the refutation named the escape and nothing built it.</b> Its row asks for relevance
/// or a sub-question, and a backward step is the first of those: at most one word enters a
/// step, chosen because a rule that concludes the wanted code asked for it. This says whether
/// the population holds anything for such a step to walk.
/// </para>
/// <para>
/// <b>And it searches from the answer, which no run knows.</b> A mechanism has the question
/// and must produce an outcome, so it would walk this backwards from each CANDIDATE answer
/// and keep the ones it can justify. That is bounded — one word enters per candidate rather
/// than the whole vocabulary — and it is more work than this reading does. So the chain
/// existing is what is established here, and the chain being findable without the answer is
/// what the mechanism owes.
/// </para>
/// <para>
/// <b>Two instrument faults are recorded rather than quietly fixed.</b> The first column
/// counted scope codes that some other resident EXPECTS, which compares a front-end code
/// against an outcome code and is false by construction — a check that cannot fire reading
/// exactly like one that passes, inside an instrument written to price a mechanism. The second
/// let a rule that already fires stand in for a chain, and every question read as reached
/// while the run scored nought.
/// </para>
/// </remarks>
public sealed class ChainingTests(ITestOutputHelper output)
{
    /// <summary>What a run of the implied half left behind.</summary>
    /// <param name="Held">The population at the end of it.</param>
    /// <param name="World">The conversation, for the alphabet it numbered words in.</param>
    /// <param name="Tally">Every counter the bench reports.</param>
    private sealed record Learnt(Population Held, Conversing World, Tally Tally);

    /// <summary>One run of a lesson, composed the way the terminal ships it.</summary>
    /// <param name="lesson">What is told and then examined.</param>
    /// <param name="carrying">How much of the topic a moment holds.</param>
    /// <param name="tellings">How many times the lesson is told.</param>
    /// <param name="seed">The seed.</param>
    /// <remarks>
    /// <b>The shipped arms rather than the defaults</b>, because what is being asked is
    /// whether the machine as it stands mints a concluding rule. A composition nobody deploys
    /// would answer about an arrangement instead.
    /// </remarks>
    private static Learnt Ran(Lesson lesson, Carrying carrying, int tellings, int seed)
    {
        var tutor = new Tutor(lesson, TextWriter.Null, passes: 1, tellings);

        var brain = new Brain(
            new CommittingSettings
            {
                Capacity = 2000,
                Rooting = Rooting.Wholly,
                Crediting = Crediting.Birth,
                Admitting = Admitting.Testable,
            },
            seed);

        var world = Fixture.Talking(tutor, carrying);

        var curiosity = new Curiosity(brain, rate: 1.0, seed, world.Naming);

        var tally = new Bench(
            new Watching<Coded>(
                world,
                new Joined(Joining.Bagged),
                acting: Chooses.From(
                    felt => Doing(curiosity.Choose(felt)), curiosity.Cleared)),
            brain)
            .Run(tutor.Moments * tutor.Longest, sweep: 200, target: 0.9, window: 50);

        return new Learnt(brain.Held, world, tally);
    }

    /// <summary>The join between what a chooser decided and how this world numbers a doing.</summary>
    private static int? Doing(Wondered said) =>
        said.Word is not { } word
            ? null
            : said.Asking ? Conversing.Asks(word) : Conversing.Asserts(word);

    /// <summary>
    /// Whether a backward step finds a chain — <b>the kill line for fork 115.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The implied half of <c>Lesson.Chained</c> reads nought</b> at one, five and twenty
    /// tellings, against a marginal of 0.125 and a recency bar of 0.250. That reading is
    /// <see cref="LessonTests.A_conclusion_that_follows_from_two_statements_is_never_reached"/>'s;
    /// this one asks why, and there are two answers. Either the concluding rule exists and
    /// nothing reaches it, which is the loop's fault and is what backward reading would
    /// repair, or it was never minted, which is genesis's and is not.
    /// </para>
    /// <para>
    /// <b>Expecting the answer is the whole test.</b> A commitment that expects the implied
    /// answer code is a rule that would say it if its scope were met; one that expects
    /// anything else cannot produce that answer down any road. So the count of residents
    /// expecting each implied answer is the ceiling on every mechanism that decides which rule
    /// speaks.
    /// </para>
    /// <para>
    /// <b>And firing is counted apart from chaining</b>, because the two are different rules
    /// and one of them is nearly free. Genesis roots on every varied code, so <c>the</c> and
    /// <c>is</c> each carry a rule to every answer the lesson has; those fire on any question
    /// and lose the vote. A chain is a rule that does NOT fire and whose missing words are
    /// concluded by rules that do.
    /// </para>
    /// <para>
    /// <b>Both arms of what a moment carries</b>, because the two fail differently and a
    /// reading on one would be about that choice. Under <see cref="Carrying.Statements"/> both
    /// facts are in front of the machine and every code is always present, so genesis roots on
    /// nothing; under <see cref="Carrying.Never"/> they are never co-present at all. The plan
    /// carries those as separate entries and this reads them together.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_a_backward_step_finds_a_chain_the_question_never_named()
    {
        const int Seeds = 3;

        var lesson = Lesson.Chained;
        var implied = lesson with { Exam = [.. lesson.Exam.Skip(lesson.Exam.Count / 2)] };

        output.WriteLine($"{Seeds} seeds, {implied.Exam.Count} implied questions");
        output.WriteLine(
            $"{"carrying",-12}{"tellings",9}{"resident",10}{"concluding",12}{"fires",7}"
            + $"{"chained",9}");

        var chained = new Dictionary<(Carrying Carrying, int Tellings), double>();
        var concluding = new Dictionary<(Carrying Carrying, int Tellings), double>();

        foreach (var carrying in new[] { Carrying.Never, Carrying.Statements })
        {
            foreach (var tellings in new[] { 1, 20 })
            {
                var rules = new List<double>();
                var straight = new List<double>();
                var hops = new List<double>();
                var resident = new List<double>();

                for (var seed = 1; seed <= Seeds; seed++)
                {
                    var learnt = Ran(implied, carrying, tellings, seed);
                    var all = learnt.Held.All.ToList();

                    var found = 0;
                    var reached = 0;
                    var concludes = 0;

                    foreach (var quiz in implied.Exam)
                    {
                        if (Wanted(learnt.World, quiz.Answer) is not { } goal) continue;

                        // What the question itself says, as codes. A CEILING rather than the
                        // moment: the front end puts codes of its own in a question that a
                        // statement's scope never held, so treating a scope as met here is
                        // the most generous reading there is. A nought under it is decisive
                        // and a number above it is an upper bound.
                        var asked = Babi.Words(quiz.Question)
                            .Select(Babi.Of)
                            .ToHashSet();

                        var says = all.Where(one => one.Expects == goal).ToList();

                        concludes += says.Count;

                        // Rules that already fire on the question's own words, and they are
                        // the VAGUE ones -- genesis roots on every varied code, so `the` and
                        // `is` each carry a rule to every answer in the lesson. The run scores
                        // nought on this half while these fire, which is the whole point of
                        // counting them separately: the concluding rule is present, fires, and
                        // is outvoted, so existence was never the thing missing.
                        if (says.Any(one => Fires(one, asked, learnt.World))) found++;

                        // And the chain, which is a different rule. A scope code the question
                        // never said is a SUB-GOAL, and the chain holds where every one of
                        // them is concluded by a rule that does fire. That is what backward
                        // reading would walk, and it is why the question not naming the code
                        // is no obstacle -- the search starts at the answer.
                        if (says.Any(one => Chains(one, asked, learnt.World, all))) reached++;
                    }

                    rules.Add(concludes);
                    straight.Add(found);
                    hops.Add(reached);
                    resident.Add(learnt.Tally.Resident);
                }

                concluding[(carrying, tellings)] = rules.Average();
                chained[(carrying, tellings)] = hops.Average();

                output.WriteLine(
                    $"{carrying.ToString().ToLowerInvariant(),-12}{tellings,9}"
                    + $"{resident.Average(),10:F1}{rules.Average(),12:F1}"
                    + $"{straight.Average(),8:F1}{hops.Average(),9:F1}");
            }
        }

        var most = concluding.Values.Max();
        var reachable = chained.Values.Max();

        output.WriteLine(
            reachable > 0.0
                ? $"a backward step reaches {reachable:F1} of {implied.Exam.Count} implied "
                  + "answers, so the chain is in the population and the reach is what is "
                  + "missing. Fork 115 is priced"
                : "no backward step reaches an implied answer under any arm, so the parts of "
                  + "the chain are not both there and fork 115 would walk an empty graph");

        // The companion, so a reading that stopped running cannot pass for one that said
        // nothing. Both arms at both tellings, or the table above is not the table claimed.
        Assert.True(chained.Count == 4,
            $"{chained.Count} of 4 cells reported, so the grid did not run");

        // The concluding rule exists, which is what says the wall is the reach rather than
        // genesis. Asserted so a run where nothing concludes cannot be read as this one.
        Assert.True(most > 0.0,
            "no resident expects an implied answer under any arm, so the concluding rule is "
            + "not being minted and the work is genesis rather than the reach. That is a "
            + "different finding from the one this file was written for, so re-read it");

        // And the reading itself, or this is a table that cannot fail on the number it is
        // about. A chain leaving the population changes what fork 115 is worth, and it would
        // otherwise show as a printed line nobody was asked to look at.
        Assert.True(reachable > 0.0,
            "no backward step reaches an implied answer under any arm. The chain has left the "
            + "population, so fork 115 would walk an empty graph and the work moves to what "
            + "mints the parts. Say what changed and re-price it");
    }

    /// <summary>Every word in a commitment's scope the question does not say.</summary>
    /// <param name="one">The commitment.</param>
    /// <param name="asked">What the question itself says.</param>
    /// <param name="world">The conversation, for the word a code stands for.</param>
    /// <remarks>
    /// <b>A code the world cannot name is skipped</b>, which is the generous half of the
    /// ceiling. Those are the front end's own codes rather than words, and whether a question
    /// carries the ones a statement carried is what a real run would decide. So a nought below
    /// is decisive and a number above it is an upper bound.
    /// </remarks>
    private static List<int> Missing(
        Commitment one, IReadOnlySet<Code> asked, Conversing world) =>
        [
            .. one.Scope
                .Where(code => !asked.Contains(code))
                .Select(world.Naming)
                .Where(word => word is not null)
                .Select(word => word!.Value),
        ];

    /// <summary>Whether a commitment fires on the question's own words.</summary>
    /// <param name="one">The commitment.</param>
    /// <param name="asked">What the question itself says.</param>
    /// <param name="world">The conversation, for the word a code stands for.</param>
    private static bool Fires(Commitment one, IReadOnlySet<Code> asked, Conversing world) =>
        Missing(one, asked, world).Count == 0;

    /// <summary>
    /// Whether a commitment is reachable in one backward step — <b>it does not fire and its
    /// missing words do.</b>
    /// </summary>
    /// <param name="one">The commitment.</param>
    /// <param name="asked">What the question itself says.</param>
    /// <param name="world">The conversation, for the word a code stands for.</param>
    /// <param name="all">Every resident, for the rule that would conclude a sub-goal.</param>
    /// <remarks>
    /// <b>The search starts at the answer and never at the question</b>, which is the whole
    /// difference from the three refuted forward shapes. Those made every conclusion live and
    /// drowned the vote in most of the vocabulary; here a sub-goal exists only because a rule
    /// concluding the wanted code asked for it, so at most one word a step enters.
    /// </remarks>
    private static bool Chains(
        Commitment one,
        IReadOnlySet<Code> asked,
        Conversing world,
        IReadOnlyList<Commitment> all)
    {
        var missing = Missing(one, asked, world);

        if (missing.Count == 0) return false;

        return missing.All(word =>
            all.Any(other =>
                other.Expects == Brain.Says(word) && Fires(other, asked, world)));
    }

    /// <summary>The outcome code for a word, where the world came to know it.</summary>
    /// <param name="world">The conversation, whose vocabulary is the outcome alphabet.</param>
    /// <param name="answer">The word wanted.</param>
    private static Code? Wanted(Conversing world, string answer)
    {
        for (var at = 0; at < world.Vocabulary.Count; at++)
            if (string.Equals(world.Vocabulary[at], answer, StringComparison.Ordinal))
                return Brain.Says(at);

        return null;
    }
}
