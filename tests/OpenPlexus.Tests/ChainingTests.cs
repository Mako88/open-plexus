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

            foreach (var (all, _, world, _, goal, asked) in Asking(implied, tellings, Seeds))
            {
                {
                    var fires = all.Where(one => Fires(one, asked, world)).ToList();

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
                        if (Fires(one, asked, world)) continue;

                        var missing = Missing(one, asked, world);

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
                            .Select(one => Wanted(world, one.Answer))
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

    /// <summary>
    /// What the weakest link is worth as the telling repeats — <b>why the chain wins once and
    /// then stops.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The premise is the suspect.</b> Answering <i>what is the cat loudness</i> wants
    /// <c>meow</c> handed over by a rule that fires on the question, and the only such rule is
    /// rooted on <c>cat</c>. What that rule is believed to be worth is the weakest link, so if
    /// it stops being a certainty the chain stops being worth more than its rivals.
    /// </para>
    /// <para>
    /// <b>And <c>Asserting.Everything</c> is what would do it.</b> A statement claims every
    /// word in turn, so <i>the cat sound is meow</i> is five moments and <c>cat</c> is present
    /// in four of them, expecting <c>the</c>, <c>sound</c>, <c>is</c> and <c>meow</c> one time
    /// each. The true rate of <c>cat</c> to <c>meow</c> is a quarter, and a rule born on the
    /// round that made it starts at one because nothing has contradicted it yet.
    /// </para>
    /// <para>
    /// <b>So the reading is one number twice</b>, and it decides whether the twelve-of-twelve
    /// above is a mechanism or an artifact of when the rule was born.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_the_weakest_link_is_worth_as_the_telling_repeats()
    {
        const int Seeds = 3;

        var lesson = Lesson.Chained;
        var implied = lesson with { Exam = [.. lesson.Exam.Skip(lesson.Exam.Count / 2)] };

        // One question, followed all the way down, because a mean over four would hide which
        // link moved. The cat's loudness needs `meow`, and `cat` is the only word of the
        // question any statement about `meow` holds.
        var quiz = implied.Exam[0];

        output.WriteLine($"{Seeds} seeds, on \"{quiz.Question}\" wanting {quiz.Answer}");
        output.WriteLine(
            $"{"tellings",-10}{"premise",9}{"fired",7}{"concluder",11}{"fired",7}{"chain",7}");

        var premises = new Dictionary<int, double>();

        foreach (var tellings in new[] { 1, 20 })
        {
            var link = new List<double>();
            var tested = new List<double>();
            var rule = new List<double>();
            var opportunity = new List<double>();

            foreach (var (all, _, world, _, goal, asked) in
                Asking(implied, tellings, Seeds).Where(one => one.Quiz == quiz))
            {
                // The best premise: whatever fires on the question and concludes a word some
                // rule expecting the answer is missing.
                var wants = all
                    .Where(one => one.Expects == goal)
                    .SelectMany(one => Missing(one, asked, world))
                    .ToHashSet();

                var supplying = all
                    .Where(one => Fires(one, asked, world))
                    .Where(one => Brain.Meant(one.Expects) is { } word && wants.Contains(word))
                    .OrderByDescending(one => one.Accuracy)
                    .ToList();

                var concluding = all
                    .Where(one => one.Expects == goal)
                    .Where(one => Missing(one, asked, world).Count > 0)
                    .OrderByDescending(one => one.Accuracy)
                    .ToList();

                if (supplying.Count == 0 || concluding.Count == 0) continue;

                link.Add(supplying[0].Accuracy);
                tested.Add(supplying[0].Fired);
                rule.Add(concluding[0].Accuracy);
                opportunity.Add(concluding[0].Fired);
            }

            premises[tellings] = link.Count == 0 ? 0.0 : link.Average();

            output.WriteLine(
                $"{tellings,-10}{premises[tellings],9:F3}{tested.Average(),7:F1}"
                + $"{rule.Average(),11:F3}{opportunity.Average(),7:F1}"
                + $"{Math.Min(premises[tellings], rule.Average()),7:F3}");
        }

        Assert.True(premises.Count == 2, "an arm did not report");

        // The reading. A premise that falls as the telling repeats says the chain's weight was
        // a birth credit rather than evidence, and the twelve-of-twelve above is an artifact of
        // when a rule was minted. Level, and something else moved.
        output.WriteLine(
            premises[20] < premises[1] - 1e-9
                ? $"the weakest link falls from {premises[1]:F3} to {premises[20]:F3}, so the "
                  + "chain was carried by a rule nothing had contradicted yet"
                : $"the weakest link holds at {premises[20]:F3}, so the premise is not what "
                  + "the repeated telling took away and the collapse is elsewhere");
    }

    /// <summary>
    /// What being able to put ONE word in its own next moment would buy — <b>the ceiling on a
    /// thought channel.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>John's, and the reading above is what makes it specific.</b> The rule that supplies
    /// <c>meow</c> as a certainty needs <c>sound</c>, the question never says <c>sound</c>,
    /// and nothing can conclude it — grounding it needs <c>meow</c>, which is what was being
    /// grounded. So the word has to be SUPPLIED rather than inferred, and a machine that can
    /// place a code in its own next moment is what supplies it.
    /// </para>
    /// <para>
    /// <b>A hypothesis rather than a conclusion, which is what keeps it settleable.</b> The
    /// machine is not told that the cat has a sound; it puts <c>sound</c> in front of itself
    /// to see what fires, and what fires is scored the ordinary way. A word placed that turns
    /// out to lead nowhere costs a round, and the plan's rule is that being told must be
    /// falsifiable — so being told by yourself must be too.
    /// </para>
    /// <para>
    /// <b>One word and one step.</b> That is what the numbers ask for: the chain is three
    /// links and two of them are already certainties, so what the channel has to reach is
    /// the one leaf nothing grounds. Allowing more would price a search rather than this.
    /// </para>
    /// <para>
    /// <b>And the ceiling cannot price it, which is the reading.</b> A placed word taken as
    /// TRUE grounds the answer at 1.000 on every question and grounds five rivals with it, so
    /// a machine that may assume a word can assume its way to anything. A placed word worth
    /// what the population already says is inference under another name and reads 0.500 and
    /// 0.303, which is the vague premise again. Neither takes first place once.
    /// </para>
    /// <para>
    /// <b>So what a placement is worth is EMPIRICAL.</b> It is not a prior a population can
    /// supply, and no reading over a finished run can say it: the value of putting a word in
    /// front of yourself is whether the chain it opens goes on to settle, which is learnt
    /// over rounds. That is a mechanism with a score rather than a search with a weight, and
    /// it is why this file stops here.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_placing_one_word_in_its_own_next_moment_would_buy()
    {
        const int Seeds = 3;

        var lesson = Lesson.Chained;
        var implied = lesson with { Exam = [.. lesson.Exam.Skip(lesson.Exam.Count / 2)] };

        output.WriteLine($"{Seeds} seeds, {implied.Exam.Count} implied questions");
        output.WriteLine(
            $"{"placing",-12}{"tellings",10}{"grounded",10}{"rivals",8}{"weight",8}{"first",7}");

        var grounded = new Dictionary<(string, int), int>();

        foreach (var believed in new[] { true, false })
        foreach (var tellings in new[] { 1, 20 })
        {
            var reached = 0;
            var first = 0;
            var weights = new List<double>();
            var rivals = new List<double>();

            foreach (var (all, _, world, _, goal, asked) in Asking(implied, tellings, Seeds))
            {
                {
                    // Every outcome, weighted by the best chain that grounds it when the
                    // machine may place ONE word of its own. The rival outcomes get the same
                    // licence, which is what makes this a comparison rather than a favour.
                    var best = new Dictionary<Code, double>();

                    foreach (var one in all)
                    {
                        var weight = Grounded(one, asked, world, all, 1, believed);

                        if (weight <= 0.0) continue;

                        best[one.Expects] =
                            best.TryGetValue(one.Expects, out var so_far)
                                ? Math.Max(so_far, weight)
                                : weight;
                    }

                    if (!best.TryGetValue(goal, out var mine)) continue;

                    reached++;
                    weights.Add(mine);

                    var top = best.Values.Max();
                    var tied = best.Count(one => one.Value >= top - 1e-9);

                    // How many OTHER outcomes the same licence grounds just as well, which is
                    // the half a weight on the right answer cannot say. A placement that
                    // reaches the answer and every rival has reached nothing.
                    rivals.Add(tied);

                    if (mine >= top - 1e-9 && tied == 1) first++;
                }
            }

            grounded[(believed ? "believed" : "inferred", tellings)] = reached;

            output.WriteLine(
                $"{(believed ? "believed" : "inferred"),-12}{tellings,10}{reached,10}"
                + $"{(rivals.Count == 0 ? 0.0 : rivals.Average()),8:F1}"
                + $"{(weights.Count == 0 ? 0.0 : weights.Average()),8:F3}{first,7}");
        }

        Assert.True(grounded.Count == 4, $"{grounded.Count} of 4 arms reported");
    }

    /// <summary>
    /// What the ORDINARY vote says once one word is supposed — <b>the reading the weights
    /// above stand in for.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A chain weight is not a vote.</b> This file held only weights: both ceilings above
    /// score a chain by an arithmetic written here — a believed place is worth one, an inferred
    /// one is worth what some rule already says — and a mechanism has neither. It puts a code in
    /// the moment and the machine votes the ordinary way, so what decides is
    /// <see cref="Population.Predict"/> over <see cref="Population.Firing"/> and nothing else.
    /// </para>
    /// <para>
    /// <b>Three columns answering three questions.</b> Whether ANY single word makes the
    /// ordinary vote name the goal is the ceiling on the whole family, and a nought there kills
    /// it with no mechanism written. How MANY words do it says whether the choice is a choice: a
    /// question where every supposition wins is one where supposing carries no information. And
    /// the machine's own top answer supposed back is the refuted shape, run beside the other two
    /// as the control it never had.
    /// </para>
    /// <para>
    /// <b>The candidate set is the whole vocabulary.</b> That is the generous half: a mechanism
    /// would bound it by a backward read, so a word that works here may be one no backward read
    /// proposes. A nought is decisive and a number above it is an upper bound, which is the
    /// arrangement <see cref="Missing"/> already uses.
    /// </para>
    /// <para>
    /// <b>And the last column is a trap check rather than a finding.</b> Putting back the code
    /// the vote handed over, untranslated, reaches the goal on nought of twelve where the same
    /// word through the world's alphabet reaches twelve. An expectation is an index and a scope
    /// holds a hash, which is fork 137. It is checked here because a reading that got the
    /// alphabet wrong would print nought and read as a verdict; the refuted arm translated, so
    /// this does not explain that nought.
    /// </para>
    /// <para>
    /// <b>What is left unexplained is the gap between this and the run.</b> The winner supposed
    /// back reaches the goal on twelve of twelve here and scored nought in a run of the same
    /// lesson. Neither the alphabet, the choice of word nor the second vote's arithmetic is what
    /// separates them, so what is left is what a question's moment carries beyond its own words.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_the_ordinary_vote_says_once_one_word_is_supposed()
    {
        const int Seeds = 3;

        var lesson = Lesson.Chained;
        var implied = lesson with { Exam = [.. lesson.Exam.Skip(lesson.Exam.Count / 2)] };

        output.WriteLine($"{Seeds} seeds, {implied.Exam.Count} implied questions");
        output.WriteLine(
            $"{"tellings",-10}{"asked",7}{"bare",6}{"supposed",10}{"words",7}{"working",9}"
            + $"{"winner",8}{"as said",9}");

        var supposing = new Dictionary<int, int>();
        var bareRight = new Dictionary<int, int>();
        var selective = new Dictionary<int, double>();
        var ownAnswer = new Dictionary<int, int>();
        var ownTop = new Dictionary<int, int>();
        var untranslatedBy = new Dictionary<int, int>();
        var questions = new Dictionary<int, int>();

        foreach (var tellings in new[] { 1, 20 })
        {
            var asked = 0;
            var bare = 0;
            var reached = 0;
            var winner = 0;
            var words = 0;
            var working = new List<double>();
            var shown = new List<string>();
            var circular = 0;
            var untranslated = 0;

            foreach (var put in Asking(implied, tellings, Seeds))
            {
                asked++;

                var vote = Voted(put, null);

                if (vote?.Expects == put.Goal) bare++;

                var wins = 0;
                var tried = 0;
                var found = new List<string>();

                for (var at = 0; at < put.World.Vocabulary.Count; at++)
                {
                    var word = Babi.Of(put.World.Vocabulary[at]);

                    if (put.Asked.Contains(word)) continue;

                    tried++;

                    if (Voted(put, word)?.Expects != put.Goal) continue;

                    wins++;
                    found.Add(put.World.Vocabulary[at]);

                    // The circularity check, and it is the one that decides whether any of
                    // this is worth reading. A supposition of the ANSWER word that reaches
                    // the answer is the corpus containing its own answer, one seam in.
                    if (string.Equals(
                        put.World.Vocabulary[at], put.Quiz.Answer, StringComparison.Ordinal))
                        circular++;
                }

                words = Math.Max(words, tried);

                if (wins > 0) reached++;

                working.Add(wins);

                if (shown.Count < 4)
                    shown.Add(
                        $"    \"{put.Quiz.Question}\" wants {put.Quiz.Answer}, "
                        + $"bare says {Named(put, vote?.Expects)}, "
                        + $"supposing {string.Join(" or ", found)} reaches it");

                // The refuted shape as a control: the machine's own top answer, put back in
                // front of itself. Nought here beside a number in the column left of it is
                // what would say the family survives its own refutation.
                if (vote?.Expects is { } said
                    && Brain.Meant(said) is { } meant
                    && meant < put.World.Vocabulary.Count
                    && Voted(put, Babi.Of(put.World.Vocabulary[meant]))?.Expects == put.Goal)
                    winner++;

                // And the same word in the OTHER alphabet, which is fork 137's whole claim.
                // An expectation is an index and a scope holds a hash, so putting back what
                // the vote handed over reaches no scope at all.
                if (vote?.Expects is { } raw && Voted(put, raw)?.Expects == put.Goal)
                    untranslated++;
            }

            supposing[tellings] = reached;
            bareRight[tellings] = bare;
            selective[tellings] = working.DefaultIfEmpty(0.0).Average();
            ownAnswer[tellings] = circular;
            ownTop[tellings] = winner;
            untranslatedBy[tellings] = untranslated;
            questions[tellings] = asked;

            output.WriteLine(
                $"{tellings,-10}{asked,7}{bare,6}{reached,10}{words,7}"
                + $"{working.DefaultIfEmpty(0.0).Average(),9:F1}{winner,8}"
                + $"{untranslated,9}");

            // Which word, because a column saying one of thirteen works cannot say whether
            // that one is the intermediate or the answer itself. The second would be the
            // corpus containing its own answer and the reading would be worth nothing.
            foreach (var line in shown) output.WriteLine(line);
        }

        Assert.True(supposing.Count == 2 && bareRight.Count == 2,
            $"{supposing.Count} of 2 tellings reported, so the grid did not run");

        // The premise the rest of the file rests on, read here rather than assumed. A bare
        // question naming its own implied answer would mean the chain is already reached and
        // every reading above is about something else.
        Assert.True(bareRight.Values.Sum() == 0,
            $"{bareRight.Values.Sum()} implied questions are answered by the bare vote, so "
            + "the second hop is not what this half is short of. Re-read the file");

        // The reading itself. A nought here kills every shape that supposes a word, because
        // no choice of word reaches the answer under the ordinary vote.
        Assert.True(supposing.Values.All(one => one == questions[1]),
            $"supposing one word reaches the goal on {supposing[1]} and {supposing[20]} of "
            + $"{questions[1]} implied questions rather than all of them. The ceiling on the "
            + "family has moved, so say what changed and re-price fork 28");

        // And it is a CHOICE rather than a licence. Thirteen candidates and about one of them
        // works, so a mechanism that supposes at random reaches almost nothing and the value
        // is in which word.
        Assert.True(selective.Values.Max() < 2.0,
            $"about {selective.Values.Max():F1} of the candidate words reach the goal, so "
            + "supposing is closer to a licence than a choice and a chooser buys less than "
            + "this reading assumed");

        // The corpus containing its own answer, which is what would make all of it worthless.
        Assert.True(ownAnswer.Values.Sum() == 0,
            $"the answer word itself is a working supposition on {ownAnswer.Values.Sum()} "
            + "questions, so the reading is circular and says nothing about a chain");

        // And the refuted shape, run as a control. The machine's own top answer supposed back
        // reaches the goal here while the same shape scored nought in a run, so what separates
        // them is not the choice of word.
        // The alphabet, and it is a check on this reading rather than a reading. A supposition
        // put back untranslated cannot reach any scope, so a run that got this wrong would
        // print nought everywhere and look like a verdict on the mechanism.
        Assert.True(untranslatedBy.Values.Sum() == 0,
            $"the code the vote handed over reaches the goal on {untranslatedBy.Values.Sum()} "
            + "questions when it is put back untranslated. A scope holds a hash and an "
            + "expectation is an index, so a number here means fork 137 has moved");

        Assert.True(ownTop.Values.Min() > 0,
            $"the machine's own top answer supposed back reaches the goal on {ownTop[1]} and "
            + $"{ownTop[20]} of {questions[1]}. A nought would put the refuted shape and this "
            + "ceiling in agreement, which is a different finding");
    }

    /// <summary>The word an outcome code stands for in this world, for a printed row.</summary>
    /// <param name="put">The question and its world.</param>
    /// <param name="said">The outcome code, or nothing where nothing was said.</param>
    private static string Named(Put put, Code? said) =>
        said is { } one && Brain.Meant(one) is { } at && at < put.World.Vocabulary.Count
            ? put.World.Vocabulary[at]
            : "nothing";

    /// <summary>
    /// What the ordinary vote says about a question, with one word supposed or with none.
    /// </summary>
    /// <param name="put">The question, its run and the population that votes.</param>
    /// <param name="supposed">
    /// The word the machine puts in front of itself, or nothing for the bare question.
    /// </param>
    /// <remarks>
    /// <b>Folded through <see cref="Population.Moment"/> like any other moment</b>, so a
    /// supposed code reaches a minted name exactly as a heard one does. A supposition that
    /// skipped the fold would be a different kind of input, and the whole claim is that it is
    /// not one.
    /// </remarks>
    private static Vote? Voted(Put put, Code? supposed)
    {
        var raw = new HashSet<Code>(put.Asked);

        if (supposed is { } one) raw.Add(one);

        var firing = put.Population.Firing(put.Population.Moment(raw));

        return firing.IsDefaultOrEmpty ? null : put.Population.Predict(firing);
    }

    /// <summary>
    /// What a commitment is worth once grounded, allowing a number of words to be PLACED.
    /// </summary>
    /// <param name="one">The commitment.</param>
    /// <param name="asked">What the question itself says.</param>
    /// <param name="world">The conversation, for the word a code stands for.</param>
    /// <param name="all">Every resident.</param>
    /// <param name="placed">
    /// How many missing words the machine may supply itself, PER PATH. The word that has to be
    /// placed is a step down rather than at the top — the certain rule supplying <c>meow</c>
    /// is the one missing <c>sound</c> — so a budget spent only at the root prices nothing.
    /// </param>
    /// <param name="believed">
    /// Whether a placed word is taken as true. <b>The whole question, and both answers are
    /// bad.</b> Believed, the machine may assume its way to anything; not believed, a
    /// placement is worth what some rule already says and is inference by another name.
    /// </param>
    /// <param name="depth">How many steps of inference are left.</param>
    /// <remarks>
    /// <b>A chain is worth its weakest link.</b> A placed word is worth one where it is
    /// believed, because the machine put it there rather than concluding it. What the
    /// placement costs is a round if the chain leads nowhere, which is a settlement.
    /// </remarks>
    private static double Grounded(
        Commitment one,
        IReadOnlySet<Code> asked,
        Conversing world,
        IReadOnlyList<Commitment> all,
        int placed,
        bool believed,
        int depth = 2)
    {
        var missing = Missing(one, asked, world);

        if (missing.Count == 0) return one.Accuracy;

        if (depth <= 0 || missing.Count > placed + depth) return 0.0;

        var worth = one.Accuracy;
        var spare = placed;

        foreach (var word in missing)
        {
            var supplied = all
                .Where(other => other.Expects == Brain.Says(word))
                .Select(other =>
                    Grounded(other, asked, world, all, spare, believed, depth - 1))
                .DefaultIfEmpty(0.0)
                .Max();

            // Placing it against inferring it, rather than inferring it wherever that is
            // possible at all. The first version took any inference over a placement and so
            // walked the vague premise every time -- a search that prefers a rule believed a
            // quarter of the time to a word it could simply put there.
            var put = believed && spare > 0 ? 1.0 : 0.0;

            if (put > supplied)
            {
                spare--;
                worth = Math.Min(worth, put);
                continue;
            }

            if (supplied > 0.0)
            {
                worth = Math.Min(worth, supplied);
                continue;
            }

            if (spare <= 0) return 0.0;

            spare--;
        }

        return worth;
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

    /// <summary>One question put to one run, with everything a reading needs.</summary>
    /// <param name="Held">Every resident at the end of that run.</param>
    /// <param name="Population">
    /// The same residents as the thing that VOTES over them.
    /// </param>
    /// <param name="World">The conversation, for the word a code stands for.</param>
    /// <param name="Quiz">The question, for a reading that wants only one of them.</param>
    /// <param name="Goal">The outcome code the right answer is.</param>
    /// <param name="Asked">What the question itself says, as codes.</param>
    /// <remarks>
    /// <b>The population is here beside the list</b>, because a weight is not a vote. Every
    /// reading in this file before <see cref="What_the_ordinary_vote_says_once_one_word_is_supposed"/>
    /// scored a chain by an arithmetic of its own, and a mechanism would have to win the
    /// ordinary vote instead. So one reading needs the thing that decides rather than the
    /// residents it decides over.
    /// </remarks>
    private readonly record struct Put(
        IReadOnlyList<Commitment> Held,
        Population Population,
        Conversing World,
        Quiz Quiz,
        Code Goal,
        IReadOnlySet<Code> Asked);

    /// <summary>
    /// Every implied question of every seed, run and set up — <b>one place, because three
    /// readings wanted the same six lines.</b>
    /// </summary>
    /// <param name="implied">The lesson, examined on its implied half.</param>
    /// <param name="tellings">How many times it is told.</param>
    /// <param name="seeds">How many seeds.</param>
    /// <remarks>
    /// <b>A run a seed rather than a run a question</b>, which is what the loop order buys. A
    /// question is put to the population the whole lesson left behind, so re-running per
    /// question would cost four runs to read four questions of one.
    /// </remarks>
    private static IEnumerable<Put> Asking(Lesson implied, int tellings, int seeds)
    {
        for (var seed = 1; seed <= seeds; seed++)
        {
            var learnt = Ran(implied, Carrying.Never, tellings, seed);
            var all = learnt.Held.All.ToList();

            foreach (var quiz in implied.Exam)
            {
                if (Wanted(learnt.World, quiz.Answer) is not { } goal) continue;

                yield return new Put(
                    all,
                    learnt.Held,
                    learnt.World,
                    quiz,
                    goal,
                    Babi.Words(quiz.Question).Select(Babi.Of).ToHashSet());
            }
        }
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
