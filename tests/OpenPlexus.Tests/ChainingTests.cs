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

    /// <summary>
    /// Where the right answer RANKS once a second hop is allowed — <b>what a chain must be
    /// worth to win.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A backward step and a flooded moment are one arithmetic.</b> At depth one a
    /// rule is reachable exactly when its missing words are concluded by rules that fire,
    /// which is exactly when it fires in a moment holding every conclusion. So the search is
    /// not where a fourth shape could differ, and what is left is what a reached rule is WORTH.
    /// </para>
    /// <para>
    /// <b>A chain is worth its weakest link.</b> Crediting one with its own accuracy is what
    /// put eight outcomes at the ceiling: the rule concluding <c>faint</c> and the rule
    /// concluding <c>harsh</c> are both perfect and both reachable; what separates them is
    /// that one was handed <c>meow</c> by a certainty and the other was handed <c>bark</c> by
    /// a rule believed an eighth of the time. Taking the minimum drops the tie from 7.2 to
    /// 2.5 and the top weight from 1.000 to 0.500.
    /// </para>
    /// <para>
    /// <b>And the slot is an ORACLE here, taken off the lesson.</b> The four implied answers
    /// are alternatives that never co-occur, so a machine holding that category would choose
    /// among them and no others. Weighted, exactly one of them is left at the top and it is
    /// the right one on every question of every seed — which is a ceiling on what fork 129
    /// would buy rather than a mechanism, and it is why the two forks are one piece of work.
    /// </para>
    /// <para>
    /// <b>Twenty tellings destroys it and the reason is not the statistic.</b> The goal is at
    /// the top weight on every question at one telling and on none at twenty, and reading the
    /// lifetime rate rather than the recency-weighted estimate does not recover it — 4.5
    /// against 4.0, both with the goal off the top. The obvious explanation was that a
    /// front-loaded telling decays under recency, and it is refuted. Unexplained.
    /// </para>
    /// <para>
    /// <b>Admitting only rules the question TOUCHES is refuted.</b> Filtering the reached set
    /// to scopes sharing a word with the question left the advocate count, the rank and the
    /// tie identical to the digit under the unweighted reading, so it is the third refuted
    /// shape by another name.
    /// </para>
    /// </remarks>
    [Fact]
    public void Where_the_right_answer_ranks_once_a_second_hop_is_allowed()
    {
        const int Seeds = 3;

        var lesson = Lesson.Chained;
        var implied = lesson with { Exam = [.. lesson.Exam.Skip(lesson.Exam.Count / 2)] };

        output.WriteLine($"{Seeds} seeds, {implied.Exam.Count} implied questions");
        output.WriteLine(
            $"{"arm",-27}{"advocated",11}{"rank",7}{"first",7}{"topweight",11}"
            + $"{"tied",6}{"goaltied",10}{"byhits",8}{"bylength",10}{"inslot",8}{"byslot",8}");

        var ranks = new Dictionary<string, double>();

        // The two statistics a commitment keeps, which are a deliberate pair rather than one
        // number. `Reliability` is the lifetime rate and merges; `Accuracy` is the
        // recency-weighted local estimate the vote decides on, and it cannot track a world
        // with no episode boundary if it is a lifetime average. A lesson told and then
        // examined is front-loaded, so which of them is read is a live question here.
        var statistics = new (string Name, Func<Commitment, double> Of)[]
        {
            ("accuracy", one => one.Accuracy),
            ("reliability", one => one.Reliability),
        };

        foreach (var (statistic, of) in statistics)
        foreach (var tellings in new[] { 1, 20 })
        {
            var places = new List<double>();
            var advocated = new List<double>();
            var best = new List<double>();
            var tied = new List<double>();
            var first = 0;
            var shared = 0;
            var won = 0;
            var narrowed = 0;
            var slotted = new List<double>();
            var picked = 0;

            for (var seed = 1; seed <= Seeds; seed++)
            {
                var learnt = Ran(implied, Carrying.Never, tellings, seed);
                var all = learnt.Held.All.ToList();

                foreach (var quiz in implied.Exam)
                {
                    if (Wanted(learnt.World, quiz.Answer) is not { } goal) continue;

                    var asked = Babi.Words(quiz.Question).Select(Babi.Of).ToHashSet();

                    var fires = all.Where(one => Fires(one, asked, learnt.World)).ToList();

                    // What one hop makes available, as word indices. This is the flood and the
                    // backward step at once, which is the point of the paragraph above.
                    // HOW WELL each word is concluded rather than merely that it is, which
                    // is the half the first version of this dropped. A rule reached through a
                    // premise nothing believes is not worth what a rule reached through a
                    // certainty is, and crediting both with their own accuracy is what put
                    // eight outcomes at the ceiling.
                    var concluded = new Dictionary<int, double>();

                    foreach (var one in fires)
                    {
                        if (Brain.Meant(one.Expects) is not { } word) continue;

                        concluded[word] =
                            concluded.TryGetValue(word, out var so_far)
                                ? Math.Max(so_far, of(one))
                                : of(one);
                    }

                    // A chain is worth its weakest link, so a reached rule carries the minimum
                    // of its own accuracy and every premise it had to be handed.
                    var reached = new List<(Commitment One, double Weight)>();

                    foreach (var one in all)
                    {
                        if (Fires(one, asked, learnt.World)) continue;

                        var missing = Missing(one, asked, learnt.World);

                        if (missing.Count == 0 || !missing.All(concluded.ContainsKey)) continue;

                        var weakest = missing.Min(word => concluded[word]);

                        reached.Add((one, Math.Min(of(one), weakest)));
                    }

                    // The vote as the machine takes it: an expectation is worth its best
                    // advocate and no more, so a maximum per outcome and then a ranking.
                    var weights = new Dictionary<Code, double>();
                    var evidence = new Dictionary<Code, long>();
                    var length = new Dictionary<Code, int>();

                    var advocates = fires
                        .Select(one => (One: one, Weight: of(one)))
                        .Concat(reached);

                    foreach (var (one, weight) in advocates)
                    {
                        weights[one.Expects] =
                            weights.TryGetValue(one.Expects, out var so_far)
                                ? Math.Max(so_far, weight)
                                : weight;

                        // How much the best advocate has been TESTED, kept beside its
                        // accuracy rather than folded into it. One weight doing two jobs is
                        // this design's recurring fault and a number that cannot say which of
                        // them moved it is the shape of it.
                        evidence[one.Expects] =
                            evidence.TryGetValue(one.Expects, out var seen)
                                ? Math.Max(seen, one.Fired)
                                : one.Fired;

                        // And how SPECIFIC the best advocate is. A longer scope says more and
                        // covers less, which is the gradient subsumption reads in the other
                        // direction, and it is the third thing that could separate a tie.
                        length[one.Expects] =
                            length.TryGetValue(one.Expects, out var deep)
                                ? Math.Max(deep, one.Scope.Length)
                                : one.Scope.Length;
                    }

                    var ranked = weights
                        .OrderByDescending(one => one.Value)
                        .ThenBy(one => one.Key)
                        .Select(one => one.Key)
                        .ToList();

                    var place = ranked.IndexOf(goal);

                    advocated.Add(weights.Count);
                    places.Add(place < 0 ? weights.Count : place + 1);

                    if (place == 0) first++;

                    // How much of the ranking is a TIE, which a rank alone cannot say. Where
                    // several outcomes share the top weight the answer comes out of code
                    // order, and that is a different defect from the right rule being weak.
                    var top = weights.Count == 0 ? 0.0 : weights.Values.Max();

                    best.Add(top);
                    tied.Add(weights.Values.Count(one => one >= top - 1e-9));

                    if (weights.TryGetValue(goal, out var mine) && mine >= top - 1e-9)
                    {
                        shared++;

                        // And whether EVIDENCE separates what accuracy could not. Among the
                        // outcomes tied at the top, the one whose advocate has fired most is
                        // what a tie-break on testedness would pick, so this says outright
                        // whether such a rule would answer the question.
                        var among = weights
                            .Where(one => one.Value >= top - 1e-9)
                            .OrderByDescending(one => evidence[one.Key])
                            .ThenBy(one => one.Key)
                            .Select(one => one.Key)
                            .ToList();

                        if (among.Count > 0 && among[0] == goal) won++;

                        var deepest = weights
                            .Where(one => one.Value >= top - 1e-9)
                            .OrderByDescending(one => length[one.Key])
                            .ThenBy(one => one.Key)
                            .Select(one => one.Key)
                            .ToList();

                        if (deepest.Count > 0 && deepest[0] == goal) narrowed++;

                        // And whether knowing the SLOT collapses it. The four implied answers
                        // are alternatives -- they never co-occur and they fill one place --
                        // so a machine holding that category would only ever be choosing among
                        // them. An ORACLE reading, taken off the lesson rather than off
                        // anything derived, so it is a ceiling on what fork 129 could buy.
                        var alternatives = implied.Exam
                            .Select(one => Wanted(learnt.World, one.Answer))
                            .Where(one => one is not null)
                            .Select(one => one!.Value)
                            .ToHashSet();

                        var within = weights
                            .Where(one => one.Value >= top - 1e-9)
                            .Where(one => alternatives.Contains(one.Key))
                            .OrderByDescending(one => length[one.Key])
                            .ThenBy(one => one.Key)
                            .Select(one => one.Key)
                            .ToList();

                        slotted.Add(within.Count);

                        if (within.Count > 0 && within[0] == goal) picked++;
                    }
                }
            }

            ranks[$"{statistic} {tellings}"] = places.Average();

            output.WriteLine(
                $"{$"{statistic}, {tellings} telling(s)",-27}"
                + $"{advocated.Average(),11:F1}{places.Average(),7:F1}"
                + $"{first,7}{best.Average(),11:F3}{tied.Average(),6:F1}{shared,10}"
                + $"{won,8}{narrowed,10}"
                + $"{(slotted.Count == 0 ? 0.0 : slotted.Average()),8:F1}{picked,8}");
        }

        // The companion, so an arm that stopped running cannot read as one that tied.
        Assert.True(ranks.Count == 4, $"{ranks.Count} of 4 arms reported");

        // And the reading, asserted so it is not a printed line nobody was asked to look at.
        // The right answer taking first place is what every second-hop shape needs and none
        // has had; the day one does, this goes red and is a RESULT rather than a regression.
        Assert.True(ranks.Values.Min() > 1.0,
            "the right answer now ranks first once a second hop is allowed, so the vote can "
            + "carry a chain and the implied half should move. Say what changed and re-take "
            + "`LessonTests.A_conclusion_that_follows_from_two_statements_is_never_reached`");
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
