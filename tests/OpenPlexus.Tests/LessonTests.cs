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
    private static (Tally Tally, Tutor Tutor, Conversing World, Brain Brain) Ran(
        Lesson lesson, Carrying carrying, int seed, int passes, int capacity = 2000,
        Asserting asserting = Asserting.Nothing, int tellings = 1, int revising = 0,
        Rooting rooting = Rooting.Singly, Crediting crediting = Crediting.Nothing,
        Replying replying = Replying.Word, Admitting admitting = Admitting.Anything,
        Joining joining = Joining.Bagged, Deciding deciding = Deciding.Grounded)
    {
        var tutor = new Tutor(
            lesson, TextWriter.Null, passes, tellings, revising, replying: replying);

        var brain = new Brain(
            Committing(capacity, rooting, crediting, admitting) with { Deciding = deciding },
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
        var front = new Joined(joining);

        var watching = new Watching<Coded>(
            world, front,
            acting: Chooses.From(felt => Doing(curiosity.Choose(felt)), curiosity.Cleared));

        // Budgeted for the widest statement, because `Asserting.Everything` makes a sentence
        // one moment a word. A run stopping at the moment count would end before the exam.
        var rounds = asserting is Asserting.Everything
            ? tutor.Moments * tutor.Longest
            : tutor.Moments;

        var tally = new Bench(watching, brain).Run(rounds, sweep: 200, target: 0.9, window: 50);

        return (tally, tutor, world, brain);
    }

    /// <summary>The four arms this file compares, as the brain's own numbers.</summary>
    /// <remarks>
    /// <b>Named rather than written out at each call</b>, because they arrived together and are
    /// measured together — a test that spelt them out per arm would drift one from another.
    /// </remarks>
    private static CommittingSettings Committing(
        int capacity, Rooting rooting, Crediting crediting, Admitting admitting) => new()
        {
            Capacity = capacity,
            Rooting = rooting,
            Crediting = crediting,
            Admitting = admitting,
        };

    /// <summary>The join between what a chooser decided and how this world numbers its doings.</summary>
    private static int? Doing(Wondered said) =>
        said.Word is not { } word
            ? null
            : said.Asking ? Conversing.Asks(word) : Conversing.Asserts(word);

    /// <summary>
    /// A lesson's stream, and what the world numbered each arrival — <b>no learner in it.</b>
    /// </summary>
    /// <param name="lesson">What is told.</param>
    /// <param name="tellings">How many times.</param>
    /// <remarks>
    /// <para>
    /// <b>Extracted because <c>DuplicationTests</c> refused the second copy</b>, and it was
    /// right to: two readings over one lesson that built their own stream would be two streams
    /// the moment either changed a setting, and the whole point of reading them together is
    /// that they are over one world.
    /// </para>
    /// <para>
    /// <b>The arrival is the WORLD's outcome number rather than a code.</b> <c>Brain.Says</c>
    /// numbers an outcome and <c>Babi.Of</c> hashes a word, so comparing one against the other
    /// is a column that cannot fire — which is how this reading first came back with hundreds
    /// of pairs and nothing placed.
    /// </para>
    /// </remarks>
    private static (Conversing World, List<(HashSet<Code> Moment, int? Arrived)> Stream) Streamed(
        Lesson lesson, int tellings)
    {
        var tutor = new Tutor(lesson, TextWriter.Null, passes: 1, tellings: tellings);

        var world = new Conversing(new ConversingSettings
        {
            Typed = tutor,
            Printed = tutor.Printed,
            Carrying = Carrying.Never,
            Asserting = Asserting.Everything,
        });

        var front = new Joined(Joining.Bagged);
        var stream = new List<(HashSet<Code> Moment, int? Arrived)>();

        // Unsettled turns are kept, because what never settles is the question one reading
        // here asks. A moment nothing followed is exactly where a sentence's last word sits.
        for (var round = 0; round < tutor.Moments * tutor.Longest && !world.Ended; round++)
        {
            var turn = world.Next();

            stream.Add(([.. front.Codify(turn.Seen)], turn.Outcome));
        }

        return (world, stream);
    }

    /// <summary>
    /// Which slot each of a lesson's words sits in — <b>a SET, because a word can be in two.</b>
    /// </summary>
    /// <param name="lesson">Whose statements name the slots.</param>
    /// <remarks>
    /// <b><see cref="Lesson.Chained"/> is why this is not a function.</b> It says the cat's
    /// sound is meow and then the meow's loudness is faint, so <c>meow</c> is a value and a
    /// subject at once. Two words count as alternatives where they share any slot, which is the
    /// only reading that survives a word being both.
    /// </remarks>
    private static Dictionary<string, HashSet<int>> Slotted(Lesson lesson)
    {
        var slots = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);

        void Places(string word, int slot)
        {
            if (!slots.TryGetValue(word, out var at)) slots[word] = at = [];

            at.Add(slot);
        }

        foreach (var fact in lesson.Facts)
        {
            Places(fact.Subject, 0);
            Places(fact.Attribute, 1);
            Places(fact.Answer, 2);
        }

        return slots;
    }

    /// <summary>
    /// The same pairs with their partners shuffled — <b>the null a share is read against.</b>
    /// </summary>
    /// <param name="over">The pairs.</param>
    /// <param name="together">Whether two words count as one group.</param>
    /// <param name="purpose">This null's own mixer, so two readings do not share a shuffle.</param>
    /// <param name="shuffles">How many times.</param>
    /// <remarks>
    /// <para>
    /// <b>Every end stays and only its partner changes</b>, which is what makes this a control
    /// rather than a coin. A word in two hundred pairs is in two hundred pairs afterwards, so a
    /// word that arrives everywhere cannot buy a share by arriving everywhere.
    /// </para>
    /// <para>
    /// <b>The first null shuffled the LABELS and read too high.</b> Thirteen
    /// of twenty words sit in one slot, so a random labelling calls most pairs alike before
    /// anything is measured. The reading it gave was about the control.
    /// </para>
    /// </remarks>
    private static double Rewired(
        IReadOnlyList<(int Left, int Right)> over,
        Func<int, int, bool> together,
        uint purpose,
        int shuffles)
    {
        ArgumentNullException.ThrowIfNull(over);
        ArgumentNullException.ThrowIfNull(together);

        var found = 0.0;

        for (var shuffle = 0; shuffle < shuffles; shuffle++)
        {
            var rng = new Random(Seeds.Apart(shuffle + 1, purpose));
            var ends = over
                .SelectMany(pair => new[] { pair.Left, pair.Right })
                .OrderBy(_ => rng.Next())
                .ToList();

            var same = 0;

            for (var end = 0; end + 1 < ends.Count; end += 2)
                if (together(ends[end], ends[end + 1])) same++;

            found += same / (double)Math.Max(over.Count, 1) / shuffles;
        }

        return found;
    }

    /// <summary>
    /// Which pairs of arrivals are commoner across contexts than independence would have
    /// them — <b>the repo's own bar rather than a count somebody picked.</b>
    /// </summary>
    /// <param name="seen">What arrived in each context, one set a context.</param>
    /// <remarks>
    /// <para>
    /// <b>A fixed count is exactly the bar <c>DO NOT RE-TRY</c> already refuses.</b> An
    /// adhesion bar set as a ratio somebody picked lost to a corrected z over the same counts,
    /// and <i>seen in more than one context</i> is that same picked bar wearing a different
    /// name — it discriminated over a hundred and twenty contexts and admitted almost
    /// everything over ten thousand pairs.
    /// </para>
    /// <para>
    /// <b>So this is <see cref="Codes.Alternating"/>'s statistic, written the same way.</b> Two
    /// arrivals landing in one context as often as their own frequencies would have them says
    /// nothing; a Poisson tail over that expectation, multiplied by how many pairs were
    /// considered, is what stops a search of ten thousand candidates clearing any fixed bar by
    /// noise alone.
    /// </para>
    /// </remarks>
    private static List<(int Left, int Right)> Commoner(IReadOnlyList<HashSet<int>> seen)
    {
        var alpha = new CommittingSettings().Alpha;
        var contexts = seen.Count(one => one.Count > 1);

        if (contexts == 0) return [];

        var apart = new Dictionary<int, int>();
        var together = new Dictionary<(int, int), int>();

        foreach (var one in seen.Where(one => one.Count > 1))
        {
            var members = one.Order().ToList();

            foreach (var member in members)
                apart[member] = apart.GetValueOrDefault(member) + 1;

            for (var at = 0; at < members.Count; at++)
                for (var other = at + 1; other < members.Count; other++)
                {
                    var pair = (members[at], members[other]);

                    together[pair] = together.GetValueOrDefault(pair) + 1;
                }
        }

        var candidates = together.Count;

        return
        [
            .. together
                .Where(pair =>
                {
                    var expected = apart[pair.Key.Item1] / (double)contexts
                        * apart[pair.Key.Item2];

                    return expected > 0.0
                        && Normal.Tail((pair.Value - expected) / Math.Sqrt(expected))
                            * candidates <= alpha;
                })
                .OrderByDescending(pair => pair.Value)
                .Select(pair => (Left: pair.Key.Item1, Right: pair.Key.Item2)),
        ];
    }

    /// <summary>What share of the lesson's truths the population actually holds.</summary>
    /// <param name="brain">Whose population is read.</param>
    /// <param name="world">The conversation, for the outcome each word was numbered as.</param>
    /// <param name="lesson">The world's whole truth, whether or not it was examined.</param>
    /// <remarks>
    /// <para>
    /// <b>An accuracy says how many questions were answered</b> and this says how much was
    /// found, and the two come apart exactly where a population is memorising. Reporting
    /// only the first is this repo's own trap: on a world with known ground truth, report how
    /// much of it was found.
    /// </para>
    /// <para>
    /// <b>A fact is held where SOME commitment says it</b>, rather than where the vote does.
    /// Whether the right rule wins its round is what the accuracy already measures; this asks
    /// the prior question of whether the rule is there at all, and the two failures want
    /// separating — a population that never found the rule and one that found it and is
    /// outvoted read alike on a score.
    /// </para>
    /// <para>
    /// <b>The scope must NAME both halves and may name more.</b> A rule keyed on the subject
    /// and the property is the one the lesson states; a narrower one that also names the
    /// category is a specialisation of it and still says the fact. A rule naming only the
    /// subject says something the lesson does not.
    /// </para>
    /// </remarks>
    private static double Found(Brain brain, Conversing world, Lesson lesson)
    {
        var facts = lesson.Facts;

        if (facts.Count == 0) return 0.0;

        var held = 0;

        foreach (var fact in facts)
        {
            var subject = Babi.Of(fact.Subject);
            var attribute = Babi.Of(fact.Attribute);

            // The word as this world numbered it, which is the only place the answer alphabet
            // lives. A word the conversation never heard has no outcome and no rule can expect
            // it, which reads as the fact not being found -- and it is not.
            var answer = world.Vocabulary
                .Select((word, at) => (Word: word, At: at))
                .Where(one => string.Equals(one.Word, fact.Answer, StringComparison.Ordinal))
                .Select(one => (int?)one.At)
                .FirstOrDefault();

            if (answer is not { } outcome) continue;

            var says = Brain.Says(outcome);

            if (brain.Held.All.Any(one =>
                one.Expects == says
                && one.Scope.Contains(subject)
                && one.Scope.Contains(attribute)))
                held++;
        }

        return held / (double)facts.Count;
    }

    /// <summary>One arm read over several lessons, as the numbers every grid here wants.</summary>
    /// <param name="seeds">How many lessons and brains.</param>
    /// <param name="purpose">This grid's own mixer, so two grids do not share eight worlds.</param>
    /// <param name="written">The hand-written lesson rather than a drawn one.</param>
    /// <param name="carrying">How much of the topic a moment holds.</param>
    /// <param name="asserting">What a told statement claims.</param>
    /// <param name="tellings">How many times the lesson is told.</param>
    /// <param name="crediting">Whether a mint is credited with the round that made it.</param>
    /// <param name="rooting">What genesis mints a scope over.</param>
    /// <param name="admitting">What a separating condition must do besides separate.</param>
    /// <param name="deciding">Whether the machine answers with nothing behind the answer.</param>
    /// <param name="passes">
    /// How many times the examination is sat. <b>At nought the run stops before the
    /// questions</b>, which is what a probe reading the population the paper has not taught
    /// yet asks for; the score is nought there and means nothing.
    /// </param>
    /// <remarks>
    /// <b>Extracted because <c>DuplicationTests</c> refused the second copy</b>, which is the
    /// right refusal: three grids each with their own seed loop is three chances for one
    /// grid's worlds to differ from the grid it is read against.
    /// </remarks>
    private static (List<double> Right, List<double> Found, List<double> Resident,
        List<double> Silent, List<Seats> Seated) Over(
        int seeds, uint purpose, bool written, Carrying carrying, Asserting asserting,
        int tellings, Crediting crediting = Crediting.Nothing,
        Rooting rooting = Rooting.Singly, Admitting admitting = Admitting.Anything,
        int passes = 1, Deciding deciding = Deciding.Grounded)
    {
        var right = new List<double>();
        var found = new List<double>();
        var resident = new List<double>();
        var silent = new List<double>();
        var seated = new List<Seats>();

        for (var index = 0; index < seeds; index++)
        {
            // Mixed rather than counted, because .NET gives near-neighbour seeds streams that
            // agree far more than chance allows -- and that agreement comes straight off the
            // standard error, making every arm look more separated than it is. The mixing is a
            // pure function of the index, so the arms stay paired.
            var seed = Worlds.Seeds.Apart(index, purpose);

            var lesson = written
                ? Lesson.Creatures
                : Lesson.Drawn(subjects: 4, attributes: 3, seed);

            var ran = Ran(
                lesson, carrying, seed, passes, asserting: asserting, tellings: tellings,
                rooting: rooting, crediting: crediting, admitting: admitting,
                deciding: deciding);

            right.Add(passes == 0 ? 0.0 : Right(ran.Tutor, pass: 0));
            found.Add(Found(ran.Brain, ran.World, lesson));
            resident.Add(ran.Tally.Resident);

            // Beside the score, because a population whose voters do not cover a moment says
            // nothing at all -- and a silent arm scores well on the few rounds it answers.
            silent.Add(ran.Tally.Rounds == 0 ? 0.0 : ran.Tally.Silent / (double)ran.Tally.Rounds);

            // Taken here rather than in a second seed loop, because two loops over `Apart` is
            // two chances for one grid's eight lessons to stop being the grid it is read
            // against. It costs a front-end pass per question and nothing else.
            seated.Add(Seating(ran.Brain, ran.World, lesson, joining: Joining.Bagged));
        }

        return (right, found, resident, silent, seated);
    }

    /// <summary>How one arm's examination split, as shares of the questions put.</summary>
    /// <remarks>
    /// <b>Four shares of one partition</b>, so they sum to one and a sum that does not is the
    /// halves counting different events. See <see cref="Seating"/> for what separates them.
    /// </remarks>
    private readonly record struct Seats
    {
        /// <summary>The vote answered with the word the examination wanted.</summary>
        public required double Right { get; init; }

        /// <summary>Nothing that fired expected that word.</summary>
        public required double Absent { get; init; }

        /// <summary>Something did, and something else weighed more.</summary>
        public required double Outranked { get; init; }

        /// <summary>Something did, at the winner's weight exactly, and lost on code order.</summary>
        public required double Tied { get; init; }

        /// <summary>
        /// Of the tied questions, the share where the right rule said MORE.
        /// </summary>
        /// <remarks>
        /// <b>Whether specificity would break the tie the right way</b>, which is the one
        /// thing that separates a fixable tie from a coin. A tie is two rules at the same
        /// accuracy, so nothing in either record tells them apart and only the scopes can:
        /// the rule naming both halves of the question says something the rule naming one
        /// half does not. At a half this is chance and the idea is dead before it is built.
        /// </remarks>
        public required double Specific { get; init; }

        /// <summary>
        /// Of the tied questions, the share where the right rule had MORE advocates.
        /// </summary>
        /// <remarks>
        /// <b>The other thing that can tell two tied rules apart</b>, and the only other one
        /// there is. A tie means the best advocate on each side is equally accurate; what is
        /// left is how many commitments say the same thing, and on a question naming two words
        /// of a statement the right answer is the one both of them point at.
        /// </remarks>
        public required double Crowd { get; init; }
    }

    /// <summary>Why each of a lesson's questions went the way it did, asked offline.</summary>
    /// <param name="brain">The brain the run finished with.</param>
    /// <param name="world">The conversation, which is where the answer alphabet lives.</param>
    /// <param name="lesson">The lesson, for its examination.</param>
    /// <param name="joining">The front end the run read the questions through.</param>
    /// <remarks>
    /// <para>
    /// <b>Offline and read-only</b>, so it disturbs no recorded number.
    /// <see cref="Population.Moment"/>, <see cref="Population.Firing"/>,
    /// <see cref="Population.Predict"/> and <see cref="Population.Weigh"/> all read and none
    /// of them writes, and the question moment is rebuilt the way
    /// <c>Machines.Watching</c> builds one.
    /// </para>
    /// <para>
    /// <b>A question carries nothing beside itself here.</b> That is <c>Carrying.Never</c>
    /// exactly, and it is the arm every grid in this file reads, so a moment with an empty
    /// story is the moment the run actually saw. A carrying arm would want the story
    /// accumulated as the world accumulates it, which is state a probe does not hold.
    /// </para>
    /// </remarks>
    private static Seats Seating(
        Brain brain, Conversing world, Lesson lesson, Joining joining)
    {
        if (lesson.Exam.Count == 0)
            return new Seats
            {
                Right = 0.0, Absent = 0.0, Outranked = 0.0, Tied = 0.0, Specific = 0.0,
                Crowd = 0.0,
            };

        var front = new Joined(joining);
        var held = brain.Held;

        double right = 0.0, absent = 0.0, outranked = 0.0, tied = 0.0;
        double specific = 0.0, crowd = 0.0;

        foreach (var quiz in lesson.Exam)
        {
            var asked = Babi.Words(quiz.Question).Select(Babi.Of).ToList();

            var raw = (IReadOnlySet<Code>)new HashSet<Code>(
                front.Codify(Coded.From([], Grouped.Of(asked))));

            var firing = held.Firing(held.Moment(raw));
            var vote = held.Predict(firing);

            // The word as this world numbered it. A word the conversation never heard has no
            // outcome, so no rule can expect it and nothing that fired can have advocated it.
            if (world.Naming(Babi.Of(quiz.Answer)) is not { } outcome)
            {
                absent++;
                continue;
            }

            // The longest scope behind an expectation, which is how much of the question a
            // rule had to name before it would speak at all. Taken off the firing set rather
            // than off the vote, because a weight is all that crosses the seam and this is a
            // reading about what is behind one.
            int Longest(Code expects) => firing
                .Where(one => one.Expects == expects)
                .Select(one => one.Scope.Length)
                .DefaultIfEmpty(0)
                .Max();

            // How many commitments say it, which is the only other thing behind a weight. A
            // count rather than a sum of weights: `Weighing.Summing` is refuted and this is not
            // it, because nothing here outweighs anything -- it separates two sides that the
            // maximum has already declared equal.
            int Many(Code expects) => firing.Count(one => one.Expects == expects);

            var says = Brain.Says(outcome);

            if (vote.Expects == says)
            {
                right++;
                continue;
            }

            var advocate = held.Weigh(firing).Each
                .Where(one => one.Expects == says)
                .Select(one => (double?)one.Weight)
                .FirstOrDefault();

            if (advocate is not { } weight)
            {
                absent++;
            }
            else if (weight < vote.Weight)
            {
                outranked++;
            }
            else
            {
                tied++;

                if (vote.Expects is not { } won) continue;

                if (Longest(says) > Longest(won)) specific++;
                if (Many(says) > Many(won)) crowd++;
            }
        }

        var of = (double)lesson.Exam.Count;

        return new Seats
        {
            Right = right / of,
            Absent = absent / of,
            Outranked = outranked / of,
            Tied = tied / of,
            Specific = tied == 0.0 ? 0.0 : specific / tied,
            Crowd = tied == 0.0 ? 0.0 : crowd / tied,
        };
    }

    /// <summary>What share of one pass's questions were answered right.</summary>
    private static double Right(Tutor tutor, int pass) =>
        tutor.Put[pass] == 0 ? 0.0 : tutor.Confirmed[pass] / (double)tutor.Put[pass];

    /// <summary>
    /// What a statement claims, re-taken on a world that moves and with coverage beside the
    /// score.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every reading that put claiming in the code</b> was taken on one hand-written text.
    /// One text is what an adjustment is read against and it is also a single sample, so an
    /// arm that wins on four creatures and three properties may be winning on that lesson.
    /// Drawing the words puts a spread under it.
    /// </para>
    /// <para>
    /// <b>And coverage is the reading none of them had.</b> An accuracy says how many
    /// questions were answered; this says how much of the world was found, and the two come
    /// apart exactly where a population is memorising. A drawn lesson knows every truth it
    /// states, which is what makes the second number possible at all.
    /// </para>
    /// <para>
    /// <b>The kill line, written before the grid ran</b>: claiming every word that does not
    /// beat claiming nothing on a drawn lesson means the reading that put it in the code was
    /// about the hand-written text.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_a_statement_claims_read_on_a_lesson_that_is_drawn_rather_than_written()
    {
        const int Seeds = 8;

        // This grid's own, so its seeds do not coincide with another grid's and two readings
        // taken on the same eight worlds are not read as two samples.
        const uint Purpose = 0x0C1A_1DED;

        var scored = new Dictionary<Asserting, (double Mean, double Error)>();
        var covered = new Dictionary<Asserting, double>();

        output.WriteLine($"{Seeds} drawn lessons, 4 things and 3 properties, told once");
        output.WriteLine(
            "told lesson   claiming      right             found             resident");

        foreach (var many in new[] { 1, 8 })
        foreach (var written in new[] { true, false })
        foreach (var asserting in new[]
        {
            Asserting.Nothing, Asserting.Rarest, Asserting.Withheld, Asserting.Everything,
        })
        {
            var (right, found, resident, silent, _) = Over(
                Seeds, Purpose, written, Carrying.Never, asserting, many);

            var measured = new Measured { Arm = asserting.ToString(), Values = [.. right] };

            if (!written && many == 8)
            {
                scored[asserting] = (measured.Mean, measured.StdErr);
                covered[asserting] = found.Average();
            }

            output.WriteLine(
                $"{many,-4}{(written ? "written" : "drawn  "),-9}{asserting,-11}"
                + $"{Sweep.Spread(right),18}{Sweep.Spread(found),18}"
                + $"{resident.Average(),9:F1}");
        }

        // The kill line. Every reading that put claiming in the code was taken on one text,
        // and a drawn lesson is where that stops being a single sample.
        var (nothing, nothingError) = scored[Asserting.Nothing];
        var (everything, everythingError) = scored[Asserting.Everything];

        var gap = everything - nothing;
        var error = Math.Sqrt((nothingError * nothingError)
            + (everythingError * everythingError));

        Assert.True(gap - (3.0 * error) > 0.0,
            $"claiming every word answered {everything:F3} of the examination against "
            + $"{nothing:F3} for claiming nothing, a gap of {gap:F3} ±{error:F3} -- inside "
            + "three standard errors. The reading that put claiming in the code was about the "
            + "hand-written lesson rather than about the mechanism");

        // And claiming nothing finds nothing, which is the same statement one level down. A
        // statement that settles nothing takes no score, no genesis and no repair, so this is
        // nought by construction and is asserted because a nought nobody checks reads the same
        // as a mechanism that stopped running.
        Assert.Equal(0.0, covered[Asserting.Nothing]);

        // The reading coverage is here for, and it is not one a score can make. Every arm that
        // claims anything holds a rule for EVERY fact the lesson states, and they answer
        // wildly different shares of the examination -- so what separates them at this many
        // tellings is which rule wins its round rather than which rules were found. The arm
        // that scores best does it with an order of magnitude more population, which is a vote
        // advantage and not a discovery one.
        foreach (var asserting in new[]
        {
            Asserting.Rarest, Asserting.Withheld, Asserting.Everything,
        })
            Assert.Equal(1.0, covered[asserting]);

        Assert.True(scored[Asserting.Rarest].Mean < scored[Asserting.Everything].Mean,
            $"claiming the rarest word answered {scored[Asserting.Rarest].Mean:F3} and "
            + $"claiming every word {scored[Asserting.Everything].Mean:F3} while both hold "
            + "every rule the lesson states. These being level would mean the population is "
            + "what separates the arms, and it is not");
    }

    /// <summary>Why a question the population holds a rule for is answered wrong.</summary>
    /// <remarks>
    /// <para>
    /// <b>Coverage says the rule is there and a score says it lost.</b> Between those two
    /// readings sit three different failures, and every arm at the seat so far has been aimed
    /// without knowing which one it was hitting. This asks each question offline against the
    /// population a run finished with, and splits a wrong answer three ways.
    /// </para>
    /// <para>
    /// <b>Absent is not a vote problem at all.</b> Nothing that fired expects the right
    /// answer, so either no rule for it is resident or the one that is does not match the
    /// question's moment. A vote rule cannot reach that and a front end or a rung can.
    /// </para>
    /// <para>
    /// <b>Outranked is the seat as it has been described</b> — a rule expecting the right
    /// answer fired and something weighing more took the round. Accuracy, age, and how fast a
    /// young rule earns its evidence all live there.
    /// </para>
    /// <para>
    /// <b>Tied is the one nobody has looked for.</b> The vote is a maximum over accuracies and
    /// an accuracy saturates at one, so rules arrive at the ceiling together and
    /// <see cref="Population.Decide"/> breaks the tie by code order. A right rule losing that
    /// way is losing to a hash, and what would fix it is not what fixes the other two.
    /// </para>
    /// <para>
    /// <b>What would drop it</b>: absent taking most of the wrong answers means the seat is
    /// the wrong name for this and the work belongs at the front end. That is a reading either
    /// way, which is why the instrument is worth its minute.
    /// </para>
    /// <para>
    /// <b>And the two roots are read side by side</b> because that is what says whether the
    /// two ages are one failure. They are not: minting the whole moment as a scope closes the
    /// outranking at eight tellings under withheld claiming and leaves every other cell where
    /// it was, the tie at one telling included.
    /// </para>
    /// </remarks>
    [Fact]
    public void Why_a_question_whose_rule_is_held_is_answered_wrong()
    {
        const int Seeds = 8;

        // This grid's own mixer, so its eight lessons are not another grid's eight read twice.
        const uint Purpose = 0x0C1A_3DED;

        output.WriteLine($"{Seeds} drawn lessons, 4 things and 3 properties");
        output.WriteLine(
            $"{"root",-8}{"credit",-9}{"told",-6}{"claiming",-12}{"sat",9}{"right",9}"
            + $"{"absent",9}{"outranked",11}{"tied",9}{"specific",10}{"crowd",8}");

        var split =
            new Dictionary<(Rooting Root, Crediting Credit, int Told, Asserting Claiming), Seats>();

        var sat =
            new Dictionary<(Rooting Root, Crediting Credit, int Told, Asserting Claiming), double>();

        foreach (var rooting in new[] { Rooting.Singly, Rooting.Wholly })
        foreach (var crediting in new[] { Crediting.Nothing, Crediting.Birth })
        foreach (var many in new[] { 1, 8 })
        foreach (var asserting in new[]
        {
            Asserting.Rarest, Asserting.Withheld, Asserting.Everything,
        })
        {
            // `Deciding.Anyway` is PINNED, because this file's question is what the ranking
            // says and the shipped arm decides whether to say it at all. A probe splitting a
            // wrong answer by cause cannot read a round the machine declined -- and a fixture
            // inheriting a dial it counts is a moving default rewriting an experiment nobody
            // edited. What declining buys has its own grid.
            var (scored, _, _, _, _) = Over(
                Seeds, Purpose, written: false, Carrying.Never, asserting, many,
                rooting: rooting, crediting: crediting, deciding: Deciding.Anyway);

            // Read on a run that STOPS before the questions, which is the whole reason for the
            // second call. A settled question mints and repairs like any other round, so the
            // population a run finishes with has been taught by the paper it is being asked
            // about -- and the first version of this read 1.000 where the machine scored 0.750.
            var (_, _, _, _, seated) = Over(
                Seeds, Purpose, written: false, Carrying.Never, asserting, many,
                rooting: rooting, crediting: crediting, passes: 0,
                deciding: Deciding.Anyway);

            var seats = new Seats
            {
                Right = seated.Average(one => one.Right),
                Absent = seated.Average(one => one.Absent),
                Outranked = seated.Average(one => one.Outranked),
                Tied = seated.Average(one => one.Tied),

                // Weighed by how many ties each lesson had, so a lesson with one tie does not
                // count as much as a lesson with six. A plain mean over the seeds would be a
                // mean of proportions with different denominators.
                Specific = seated.Sum(one => one.Tied) == 0.0
                    ? 0.0
                    : seated.Sum(one => one.Tied * one.Specific) / seated.Sum(one => one.Tied),

                Crowd = seated.Sum(one => one.Tied) == 0.0
                    ? 0.0
                    : seated.Sum(one => one.Tied * one.Crowd) / seated.Sum(one => one.Tied),
            };

            split[(rooting, crediting, many, asserting)] = seats;
            sat[(rooting, crediting, many, asserting)] = scored.Average();

            output.WriteLine(
                $"{rooting.ToString().ToLowerInvariant(),-8}"
                + $"{crediting.ToString().ToLowerInvariant(),-9}{many,-6}"
                + $"{asserting.ToString().ToLowerInvariant(),-12}"
                + $"{scored.Average(),9:F3}{seats.Right,9:F3}"
                + $"{seats.Absent,9:F3}{seats.Outranked,11:F3}{seats.Tied,9:F3}"
                + $"{seats.Specific,10:F3}{seats.Crowd,8:F3}");
        }

        // The four shares are one partition of the examination. A statistic whose halves count
        // different events announces itself by exceeding one, and this repo has a line about it.
        foreach (var seats in split.Values)
            Assert.Equal(
                1.0, seats.Right + seats.Absent + seats.Outranked + seats.Tied, 3);

        // The control on the instrument rather than on the machine. The probe answers every
        // question against the population as it stood when the first one was put, and the score
        // is what the machine got as it went -- so the probe reads at or under the score
        // wherever the examination teaches, and a probe ABOVE it is reading a later machine.
        foreach (var (at, seats) in split)
            output.WriteLine(
                $"{at.Root.ToString().ToLowerInvariant()}, "
                + $"{at.Credit.ToString().ToLowerInvariant()}, told {at.Told}, "
                + $"{at.Claiming.ToString().ToLowerInvariant()}: sat {sat[at]:F3}, probe "
                + $"{seats.Right:F3}");

        // The kill line, written before the grid ran. If a wrong answer is usually one where
        // nothing expecting the right word even fired, the seat is the wrong name for this and
        // no vote rule reaches it.
        var wrong = split.Values.Sum(one => 1.0 - one.Right);
        var absent = split.Values.Sum(one => one.Absent);

        output.WriteLine($"absent {absent:F3} of {wrong:F3} wrong");

        Assert.True(absent < wrong / 2.0,
            $"{absent:F3} of {wrong:F3} wrong answers had nothing expecting the right word "
            + "fire at all, so most of what is called the seat is a rule that never reached "
            + "the moment. The work is at the front end rather than at the vote");

        // Once the lesson has been told enough, every wrong answer is a right rule OUTRANKED.
        // Nothing is missing and nothing ties, so what is left is a ranking failure exactly --
        // which is the thing the seat has always been described as and had never been shown.
        foreach (var asserting in new[]
        {
            Asserting.Rarest, Asserting.Withheld, Asserting.Everything,
        })
        {
            var seats = split[(Rooting.Singly, Crediting.Nothing, 8, asserting)];

            Assert.Equal(0.0, seats.Absent);
            Assert.Equal(0.0, seats.Tied);
            Assert.Equal(1.0 - seats.Right, seats.Outranked, 3);
        }

        // And told ONCE it is a different failure wearing the same score. Half of what claiming
        // the rarest word gets wrong is a tie, and a tie is not a ranking failure -- it is the
        // vote being handed two rules it has no way to tell apart.
        var blank = split[(Rooting.Singly, Crediting.Nothing, 1, Asserting.Rarest)];

        Assert.True(blank.Tied > blank.Right,
            $"tied is {blank.Tied:F3} against {blank.Right:F3} right, so the one-telling loss "
            + "has stopped being a tie and the two ages are one failure after all");

        // And the two ages are two failures, which is what the wide root says by closing only
        // one of them. Minting the whole moment as a scope takes the outranking at eight
        // tellings to nought under WITHHELD claiming and moves nothing anywhere else -- not at
        // one telling, and not at eight under the other two claiming arms.
        //
        // Both halves of that have a mechanism. It pays under withheld because that arm leaves
        // the claim out of the scope, so the wide mint is exactly the question's words and
        // fires on it; under the other arms the claim is IN the scope, and a scope holding the
        // answer cannot fire on a question that does not name it. And it does nothing at one
        // telling because a wide mint starts blank exactly as a narrow one does, so it joins
        // the tie rather than breaking it -- crediting is the dial that would change that and
        // this grid holds it off.
        //
        // So the wide root is an interaction with what a statement claims rather than a main
        // effect, and the tie at one telling is still unanswered by anything built.
        foreach (var asserting in new[]
        {
            Asserting.Rarest, Asserting.Withheld, Asserting.Everything,
        })
        foreach (var many in new[] { 1, 8 })
        {
            var narrow = split[(Rooting.Singly, Crediting.Nothing, many, asserting)];
            var wide = split[(Rooting.Wholly, Crediting.Nothing, many, asserting)];

            output.WriteLine(
                $"told {many}, {asserting.ToString().ToLowerInvariant()}: singly "
                + $"{narrow.Right:F3} right ({narrow.Outranked:F3} outranked, "
                + $"{narrow.Tied:F3} tied), wholly {wide.Right:F3} right "
                + $"({wide.Outranked:F3} outranked, {wide.Tied:F3} tied)");
        }

        Assert.True(
            split[(Rooting.Wholly, Crediting.Nothing, 8, Asserting.Withheld)].Right
                > split[(Rooting.Singly, Crediting.Nothing, 8, Asserting.Withheld)].Right,
            "the wide root answers no more of the examination than the shipped one where the "
            + "shipped one is outranked, so minting the whole moment reaches none of the "
            + "outranking either and nothing built here touches the seat");

        // And the negative beside it, because a mechanism that fixes one cell and is read as
        // fixing the failure is how a story outruns its evidence. The tie is where the loss is
        // at one telling and the wide root leaves every tied question tied.
        foreach (var asserting in new[]
        {
            Asserting.Rarest, Asserting.Withheld, Asserting.Everything,
        })
            Assert.Equal(
                split[(Rooting.Singly, Crediting.Nothing, 1, asserting)].Tied,
                split[(Rooting.Wholly, Crediting.Nothing, 1, asserting)].Tied,
                3);

        // Crediting CONVERTS a tie into an outranking and answers no more of the paper. A mint
        // told about the round that made it arrives at a perfect accuracy, and rules minted
        // earlier have fired and missed since -- so the newest is the strongest and the tie
        // breaks by RECENCY. That is not correctness, and the score says so: the same
        // questions come back right and the failures have changed their name.
        //
        // Its own remark predicted the other half of this, that every one-code mint would
        // arrive at a perfect accuracy TOGETHER and hand the tie to code order. What actually
        // happens is that they do not arrive together, because the population is being built
        // while it is being told.
        foreach (var asserting in new[] { Asserting.Rarest, Asserting.Withheld })
        {
            var uncredited = split[(Rooting.Singly, Crediting.Nothing, 1, asserting)];
            var credited = split[(Rooting.Singly, Crediting.Birth, 1, asserting)];

            Assert.Equal(0.0, credited.Tied);
            Assert.Equal(uncredited.Tied, credited.Outranked, 3);
            Assert.Equal(uncredited.Right, credited.Right, 3);
        }

        // And what pays is the THREE together, on drawn lessons rather than on the one written
        // text that reading was taken on. Claiming every word in turn, the whole moment as one
        // scope, and a mint credited with its own round: told once and never examined, it
        // answers the paper. Any two of them reach a fraction of it, and the table above is
        // where that can be read cell by cell.
        var once = split[(Rooting.Wholly, Crediting.Birth, 1, Asserting.Everything)];

        Assert.True(once.Right > 0.9,
            $"told once, the three arms together answered {once.Right:F3} of an examination "
            + "never sat, so the reading that put one-shot in the code was about the "
            + "hand-written lesson rather than about the mechanisms");

        // The negative that kills both obvious tie-breaks before either is built. In every tied
        // question the right rule and the winner have the same weight, the same scope length
        // and the same number of advocates -- so specificity, which is what a production system
        // resolves a conflict by, separates none of them, and neither does a crowd. Genesis
        // roots on ONE code, so at this age every rule in the population is one code expecting
        // one thing, and two of them are the same object to anything the vote can read.
        foreach (var (at, seats) in split)
        {
            Assert.Equal(0.0, seats.Specific);
            Assert.Equal(0.0, seats.Crowd);

            output.WriteLine(
                $"told {at.Told}, {at.Claiming.ToString().ToLowerInvariant()}: of "
                + $"{seats.Tied:F3} tied, {seats.Specific:F3} separable by scope and "
                + $"{seats.Crowd:F3} by advocates");
        }
    }

    /// <summary>
    /// Where the wide root stops paying on a first telling, told once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The obvious account is wrong</b>, and this is the control that says so. Genesis may
    /// not root on a code that has never been absent, and on a first hearing every word is
    /// new, so the wide root looked as though it could have nothing eligible to mint a
    /// conjunction over. It mints under every claiming arm, six to twenty-three of them.
    /// </para>
    /// <para>
    /// <b>So the scopes are there and the score does not move</b>, which puts the blocker
    /// after genesis rather than inside it. A wide scope that does not FIRE on the question
    /// and one that fires and ties are different failures, and this counts both.
    /// </para>
    /// <para>
    /// <b>And they fire, which refutes the second account too.</b> A scope holding the answer
    /// word would be mute on a question that does not name it, and half of them are a subset
    /// of some question anyway. So the wide root at one telling is neither a genesis problem
    /// nor a matching one: the scopes exist, they reach the paper, and the score does not move.
    /// </para>
    /// <para>
    /// <b>Which leaves the vote, where the split already put it.</b> A scope minted on a first
    /// telling has a blank record whatever its width, and `Seating` reads half of what
    /// claiming the rarest word gets wrong as a TIE. Two accounts died here and the reading
    /// they were aimed at did not move.
    /// </para>
    /// </remarks>
    [Fact]
    public void Where_the_wide_root_stops_paying_when_a_lesson_is_told_once()
    {
        const int Seeds = 4;
        const uint Purpose = 0x0C1A_5DED;

        output.WriteLine($"{Seeds} drawn lessons, told once, the wide root throughout");
        output.WriteLine($"{"claiming",-12}{"wide mints",12}{"firing",9}{"resident",10}");

        var wide = new Dictionary<Asserting, double>();
        var firing = new Dictionary<Asserting, double>();

        foreach (var asserting in new[]
        {
            Asserting.Rarest, Asserting.Withheld, Asserting.Everything,
        })
        {
            var minted = new List<double>();
            var fires = new List<double>();
            var resident = new List<double>();

            for (var index = 0; index < Seeds; index++)
            {
                var seed = Worlds.Seeds.Apart(index, Purpose);
                var lesson = Lesson.Drawn(subjects: 4, attributes: 3, seed);

                var ran = Ran(
                    lesson, Carrying.Never, seed, passes: 1, asserting: asserting, tellings: 1,
                    rooting: Rooting.Wholly);

                // Born of genesis and wider than one code, which is the wide root's mint and
                // nothing else. A repair's child is also wider than one code, so the birth is
                // what separates them -- and reading it off the scope length alone would count
                // repair's work as the root's.
                var births = ran.Brain.Held.Births;

                var wides = ran.Brain.Held.All
                    .Where(one =>
                        one.Scope.Length > 1
                        && births.TryGetValue(one.Identity, out var how)
                        && how is Birth.Genesis)
                    .ToList();

                minted.Add(wides.Count);

                // And how many of them the examination can reach, which is the half a count of
                // mints cannot say. A scope holding the answer word is a scope no question
                // naming the subject alone is a superset of, so it is minted and mute.
                var asked = lesson.Exam
                    .Select(quiz => (IReadOnlySet<Code>)new HashSet<Code>(
                        new Joined(Joining.Bagged).Codify(Coded.From(
                            [],
                            Grouped.Of(Babi.Words(quiz.Question).Select(Babi.Of))))))
                    .ToList();

                fires.Add(wides.Count(one => asked.Any(one.Fires)));

                resident.Add(ran.Tally.Resident);
            }

            wide[asserting] = minted.Average();
            firing[asserting] = fires.Average();

            output.WriteLine(
                $"{asserting.ToString().ToLowerInvariant(),-12}{minted.Average(),12:F1}"
                + $"{fires.Average(),9:F1}{resident.Average(),10:F1}");
        }

        // The refutation, and it is what this test is now for. Every arm mints wide scopes on
        // a first telling, so the varied gate is not the blocker and the account naming it was
        // wrong.
        Assert.All(
            new[] { Asserting.Rarest, Asserting.Withheld, Asserting.Everything },
            asserting => Assert.True(wide[asserting] > 0.0,
                $"{asserting} minted no wide scope at all on a first telling"));

        // And the second refutation. A wide scope holding the answer word would be mute on a
        // question that does not name it, and about half of them reach a question anyway. So
        // neither genesis nor matching is what keeps the wide root from paying at one telling
        // -- the scopes are minted, they fire, and the score does not move.
        Assert.All(
            new[] { Asserting.Rarest, Asserting.Withheld, Asserting.Everything },
            asserting => Assert.True(firing[asserting] > 0.0,
                $"{asserting} minted {wide[asserting]:F1} wide scopes and not one of them "
                + "fires on the examination, so a scope the paper cannot reach IS the "
                + "blocker after all"));
    }

    /// <summary>
    /// What declining to answer buys, when nothing behind the answer has been right.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>John's, and it answers the tie rather than breaking it.</b> The seat's remaining
    /// half was what separates two blank rules, and the honest answer is that nothing does and
    /// the machine should say so. A weight of nought means the best advocate has never been
    /// right about what it advocates, so the ranking under it is code order and the answer
    /// comes out of a hash.
    /// </para>
    /// <para>
    /// <b>Read on the paper's own mark</b>, which is the strictest test there is. A declined
    /// question and a wrong one are both unconfirmed, so this number can only FALL when the
    /// machine goes quiet — there is no way for silence to flatter it. Declining being level
    /// on it means every round given up was a round already being lost.
    /// </para>
    /// <para>
    /// <b>And it is read where evidence exists</b>, which the first attempt got wrong. Told
    /// once with no crediting every accuracy in the population is nought, so every weight is
    /// nought and the arm declines the whole paper — a cell where the comparison cannot say
    /// anything, and where the score it gives up was the chance bar anyway.
    /// </para>
    /// <para>
    /// <b>The kill line, written before the grid ran</b>: declining dies if it costs score on
    /// the paper's own mark. Silence that gives up a right answer is not honesty.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_declining_to_answer_buys_when_nothing_behind_the_answer_has_been_right()
    {
        const int Seeds = 8;
        const uint Purpose = 0x0C1A_6DED;

        int[] tellings = [1, 8];

        // The bar before any arm is read, which is this file's discipline. A score at or under
        // the commonest answer is a reading about the lesson rather than about the machine.
        var bar = Enumerable.Range(0, Seeds)
            .Select(index => Worlds.Seeds.Apart(index, Purpose))
            .Select(seed => Lesson.Drawn(subjects: 4, attributes: 3, seed))
            .Average(lesson => new Tutor(lesson, TextWriter.Null).Marginal
                / (double)lesson.Exam.Count);

        output.WriteLine($"{Seeds} drawn lessons, claiming the rarest word, marginal {bar:F3}");
        output.WriteLine(
            $"{"told",-6}{"deciding",-10}{"right",8}{"silent",9}{"resident",10}");

        var marked = new Dictionary<(int Told, Deciding How), double>();
        var silent = new Dictionary<(int Told, Deciding How), double>();

        foreach (var many in tellings)
        foreach (var deciding in new[] { Deciding.Anyway, Deciding.Grounded })
        {
            var right = new List<double>();
            var quiet = new List<double>();
            var resident = new List<double>();

            for (var index = 0; index < Seeds; index++)
            {
                var seed = Worlds.Seeds.Apart(index, Purpose);
                var lesson = Lesson.Drawn(subjects: 4, attributes: 3, seed);

                var ran = Ran(
                    lesson, Carrying.Never, seed, passes: 1, asserting: Asserting.Rarest,
                    tellings: many, deciding: deciding);

                right.Add(Right(ran.Tutor, pass: 0));
                resident.Add(ran.Tally.Resident);

                // The population's own count of rounds it said nothing on, which is the whole
                // run rather than the paper. It is the mechanism firing rather than the score,
                // and it is here so a level score cannot be a dial that never ran.
                quiet.Add(ran.Tally.Rounds == 0
                    ? 0.0
                    : ran.Tally.Silent / (double)ran.Tally.Rounds);
            }

            marked[(many, deciding)] = right.Average();
            silent[(many, deciding)] = quiet.Average();

            output.WriteLine(
                $"{many,-6}{deciding.ToString().ToLowerInvariant(),-10}{right.Average(),8:F3}"
                + $"{quiet.Average(),9:F3}{resident.Average(),10:F1}");
        }

        // The arm ran, and a level score with this unmoved would be a dial that did nothing.
        Assert.All(
            tellings,
            many => Assert.True(
                silent[(many, Deciding.Grounded)] > silent[(many, Deciding.Anyway)],
                $"told {many}, declining went quiet on no more rounds than answering anyway "
                + "did, so the arm never fired and the scores beside it say nothing"));

        // The kill line, on the paper's own mark, where silence can only lose. Told enough
        // that the population has evidence, giving up the rounds it has none for costs
        // nothing -- every one of them was a round already being lost.
        Assert.True(
            marked[(8, Deciding.Grounded)] >= marked[(8, Deciding.Anyway)],
            $"declining scored {marked[(8, Deciding.Grounded)]:F3} against "
            + $"{marked[(8, Deciding.Anyway)]:F3} for answering anyway, so the rounds it gave "
            + "up were rounds it was getting right and the silence costs real answers");

        // And told ONCE it gives up something worse than the commonest answer. Every accuracy
        // in the population is still nought there, so every weight is nought and the arm
        // declines the whole paper -- and what that forfeits scores UNDER the marginal, which
        // is what says the forfeited answers were a coin rather than knowledge. Stated as a
        // bar rather than as a comparison, because two arms either side of chance are not
        // being ranked against each other.
        Assert.True(marked[(1, Deciding.Anyway)] <= bar,
            $"told once, answering anyway scored {marked[(1, Deciding.Anyway)]:F3} against a "
            + $"marginal of {bar:F3}, so it is above chance and declining the whole paper "
            + "gives up something the machine knew");
    }

    /// <summary>What a moment carries, re-taken on a world that moves.</summary>
    /// <remarks>
    /// <para>
    /// <b>The same worry one axis over.</b> Width was measured on the single text claiming
    /// was, so the same question applies: is the ordering a fact about the mechanism, or about
    /// four creatures and three properties.
    /// </para>
    /// <para>
    /// <b>Read at the claiming arm that learns and stays small.</b> Claiming every word wins
    /// the examination with an order of magnitude more population, which would put this axis
    /// under a ceiling — every cell answering everything says nothing about width.
    /// </para>
    /// <para>
    /// <b>The kill line, written before the grid ran</b>: a width ordering that does not
    /// reproduce on drawn lessons was about the text.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_a_moment_carries_read_on_a_lesson_that_is_drawn_rather_than_written()
    {
        const int Seeds = 8;
        const uint Purpose = 0x0C1A_2DED;
        const int Tellings = 8;

        var scored = new Dictionary<(bool Written, Carrying Carrying), double>();

        output.WriteLine($"{Seeds} lessons, 4 things and 3 properties, told {Tellings} times");
        output.WriteLine("lesson   carrying    right             found             resident");

        foreach (var written in new[] { true, false })
        foreach (var carrying in new[] { Carrying.Always, Carrying.Statements, Carrying.Never })
        {
            var (right, found, resident, silent, _) = Over(
                Seeds, Purpose, written, carrying, Asserting.Withheld, Tellings);

            scored[(written, carrying)] = right.Average();

            output.WriteLine(
                $"{(written ? "written" : "drawn  "),-9}{carrying,-12}"
                + $"{Sweep.Spread(right),18}{Sweep.Spread(found),18}{resident.Average(),9:F1}");
        }

        // The width ordering, matched cell for cell against the written lesson. Asserting that
        // the two AGREE rather than which way round they go is what keeps this a check on the
        // world rather than a prediction written into a wiring test.
        foreach (var (one, other) in new[]
        {
            (Carrying.Always, Carrying.Statements),
            (Carrying.Statements, Carrying.Never),
            (Carrying.Always, Carrying.Never),
        })
            Assert.True(
                scored[(true, one)].CompareTo(scored[(true, other)])
                    == scored[(false, one)].CompareTo(scored[(false, other)]),
                $"{one} against {other} orders one way on the written lesson and another on "
                + "eight drawn ones, so the width reading was about the text rather than "
                + "about what a moment carries");
    }

    /// <summary>Whether crediting a mint with its own round pays on a world that moves.</summary>
    /// <remarks>
    /// <para>
    /// <b>Read where the arms are still apart.</b> Believing a rule a telling sooner is worth
    /// nothing once there have been enough tellings for both arms to believe it, so a grid at
    /// the width reading's eight tellings returns the same number in every cell — a check
    /// that cannot fail, which reads exactly like one that passes.
    /// </para>
    /// <para>
    /// <b>The kill line, written before the grid ran</b>: a credited arm that does not lead at
    /// the tellings where the two are apart means the written lesson was what the reading was
    /// about.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_crediting_a_mint_pays_on_a_lesson_that_is_drawn_rather_than_written()
    {
        const int Seeds = 8;
        const uint Purpose = 0x0C1A_3DED;

        int[] tellings = [1, 2, 3, 8];

        var blank = new Dictionary<int, double>();
        var credited = new Dictionary<int, double>();

        output.WriteLine($"{Seeds} drawn lessons, 4 things and 3 properties");
        output.WriteLine("told   blank             credited          found");

        foreach (var many in tellings)
        {
            var (one, _, _, _, _) = Over(
                Seeds, Purpose, written: false, Carrying.Never, Asserting.Withheld, many);

            var (other, found, _, _, _) = Over(
                Seeds, Purpose, written: false, Carrying.Never, Asserting.Withheld, many,
                Crediting.Birth);

            blank[many] = one.Average();
            credited[many] = other.Average();

            output.WriteLine(
                $"{many,-7}{Sweep.Spread(one),18}{Sweep.Spread(other),18}"
                + $"{Sweep.Spread(found),18}");
        }

        // Never behind, which is the weak half and the one that holds everywhere.
        Assert.All(tellings, many => Assert.True(credited[many] >= blank[many],
            $"at {many} telling(s) the credited arm read {credited[many]:F3} and the blank "
            + $"one {blank[many]:F3}, so believing a rule sooner cost something"));

        // And ahead somewhere, or the arms are one arm and the dial is decoration.
        Assert.Contains(tellings, many => credited[many] > blank[many]);
    }

    /// <summary>
    /// The three hand-set brain arms, on the generated world their entries are waiting for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Their own exit condition, said in <c>DialTests</c></b>: each is an arm rather than a
    /// default because it has one world's evidence, and each entry leaves when a generated
    /// world has put its two arms against each other. That world now exists, so this is the
    /// reading the entries asked for rather than an argument for shipping them.
    /// </para>
    /// <para>
    /// <b>Each arm is read where its OWN reading was taken</b>, and getting that wrong once
    /// cost this grid a wrong diagnosis. The admission bar was measured at twenty tellings
    /// with the whole-moment root, credited mints and every word claimed; put at eight
    /// tellings on the shipped root it reads ruinous, which is a fact about that combination
    /// and not a refutation of anything. A fixture inherits every dial it does not pin.
    /// </para>
    /// <para>
    /// <b>The kill lines, written before the grid ran</b>: the admission bar dies as a default
    /// if it costs score on drawn lessons or stops leaving the ladder's trigger something to
    /// fire on; the whole-moment root dies if it stops reaching further in fewer tellings.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_three_hand_set_arms_read_on_lessons_that_are_drawn()
    {
        const int Seeds = 8;
        const uint Purpose = 0x0C1A_4DED;

        int[] tellings = [2, 3, 8];

        output.WriteLine($"{Seeds} drawn lessons, 4 things and 3 properties");
        output.WriteLine("told  arm                right             found             resident");

        var scored = new Dictionary<(int Told, string Arm), double>();
        var residents = new Dictionary<(int Told, string Arm), double>();

        foreach (var many in tellings)
        {
            foreach (var (arm, rooting) in new[]
            {
                ("shipped", Rooting.Singly),
                ("wholly ", Rooting.Wholly),
            })
            {
                var (right, found, resident, silent, _) = Over(
                    Seeds, Purpose, written: false, Carrying.Never, Asserting.Withheld, many,
                    rooting: rooting);

                scored[(many, arm.Trim())] = right.Average();
                residents[(many, arm.Trim())] = resident.Average();

                output.WriteLine(
                    $"{many,-6}{arm,-19}{Sweep.Spread(right),18}{Sweep.Spread(found),18}"
                    + $"{resident.Average(),9:F1}");
            }
        }

        // The whole-moment root reaches further in fewer tellings, which is the conjunction
        // being STATED rather than found by failing.
        Assert.True(scored[(3, "wholly")] > scored[(3, "shipped")],
            $"at three tellings the wide root read {scored[(3, "wholly")]:F3} and the shipped "
            + $"one {scored[(3, "shipped")]:F3}, so minting the statement as one scope buys "
            + "nothing on a lesson that is drawn");

        // The two of them CROSSED, which is the cell every reading of either has assumed away.
        // The admission bar's reading was always taken beside the whole-moment root and the
        // root's beside the shipped bar, so neither says what happens when only one of them
        // ships -- and one of them shipping alone is exactly what a default change does.
        output.WriteLine(
            $"{Environment.NewLine}the two crossed, 8 tellings, withheld claiming");
        output.WriteLine("root      bar        right             found             resident");

        var crossed = new Dictionary<(Rooting, Admitting), double>();

        foreach (var rooting in new[] { Rooting.Singly, Rooting.Wholly })
        foreach (var admitting in new[] { Admitting.Anything, Admitting.Testable })
        {
            var (right, found, resident, silent, _) = Over(
                Seeds, Purpose, written: false, Carrying.Never, Asserting.Withheld, 8,
                rooting: rooting, admitting: admitting);

            crossed[(rooting, admitting)] = right.Average();

            output.WriteLine(
                $"{rooting,-10}{admitting,-11}{Sweep.Spread(right),18}"
                + $"{Sweep.Spread(found),18}{resident.Average(),9:F1}");
        }

        // The bar costs most of the examination under BOTH roots here, and its no-cost
        // reading was taken at twenty tellings with every word claimed and mints credited.
        // What that means is that the cost is a function of how YOUNG the population is: the
        // bar refuses a child that cannot clear the floor, and before enough tellings nothing
        // clears it, so repair is blocked exactly while the population is still being built.
        //
        // So the bar is free at saturation and expensive before it, which is not what *costs
        // no score on two lessons* says. That is the reason it does not ship as a default, and
        // it is a reason no reading taken beside one root and one telling count could give.
        Assert.All(
            new[] { Rooting.Singly, Rooting.Wholly },
            rooting => Assert.True(
                crossed[(rooting, Admitting.Testable)]
                    < crossed[(rooting, Admitting.Anything)],
                $"under the {rooting} root the admission bar cost nothing at eight tellings, "
                + "so it has stopped being a function of how young the population is and can "
                + "be read on its own"));

        // And it costs far less under the whole-moment root, so the two dials are not
        // independent either. A bar measured beside one root says nothing about the brain that
        // would result from shipping only the bar.
        Assert.True(
            crossed[(Rooting.Wholly, Admitting.Testable)]
                > crossed[(Rooting.Singly, Admitting.Testable)] * 2.0,
            "the bar reads alike under both roots, so there is no interaction to keep it from "
            + "shipping alone");

        // And the admission bar, at the settings its own reading was taken at rather than at
        // this grid's. Twenty tellings, the whole-moment root, credited mints, every word
        // claimed -- move any of those and the cell says something about the combination.
        const int Told = 20;

        output.WriteLine(
            $"{Environment.NewLine}the admission bar at {Told} tellings, wholly rooted, "
            + "credited, claiming everything");
        output.WriteLine("arm       right             found             resident");

        var bar = new Dictionary<Admitting,
            (double Right, double Error, double Found, double Resident)>();

        foreach (var admitting in new[] { Admitting.Anything, Admitting.Testable })
        {
            var (right, found, resident, silent, _) = Over(
                4, Purpose, written: false, Carrying.Never, Asserting.Everything, Told,
                Crediting.Birth, Rooting.Wholly, admitting);

            var measured = new Measured { Arm = $"{admitting}", Values = [.. right] };

            bar[admitting] =
                (measured.Mean, measured.StdErr, found.Average(), resident.Average());

            output.WriteLine(
                $"{admitting,-10}{Sweep.Spread(right),18}{Sweep.Spread(found),18}"
                + $"{resident.Average(),9:F1}");
        }

        // It holds every rule the lesson states, which is the strong half and the one a score
        // cannot say. The bar throws most of the population away and loses none of the world.
        Assert.Equal(bar[Admitting.Anything].Found, bar[Admitting.Testable].Found);

        Assert.True(bar[Admitting.Testable].Resident * 2 < bar[Admitting.Anything].Resident,
            $"the bar left {bar[Admitting.Testable].Resident:F1} residents against "
            + $"{bar[Admitting.Anything].Resident:F1} on drawn lessons, so the churn it was "
            + "named for has gone by some other road");

        // And what it costs the score is inside three standard errors of nothing. On the
        // written lesson it was equal to the digit; on drawn ones there is a spread, and this
        // asserts what a spread allows rather than what one text happened to show.
        var cost = bar[Admitting.Anything].Right - bar[Admitting.Testable].Right;

        var spread = Math.Sqrt((bar[Admitting.Anything].Error * bar[Admitting.Anything].Error)
            + (bar[Admitting.Testable].Error * bar[Admitting.Testable].Error));

        Assert.True(cost - (3.0 * spread) < 0.0,
            $"the bar cost {cost:F3} ±{spread:F3} of the examination on drawn lessons, which "
            + "is outside three standard errors. It costs score after all, so it is a trade "
            + "rather than the free cut the written lesson read as");
    }

    /// <summary>
    /// Whether the link a two-statement conclusion needs is HELD and unseated, or never
    /// formed at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Fork 125 has been refuted seven times</b> — three shapes of a second hop inside the
    /// brain and four of a front end that selects — and every one of them read nought on the
    /// implied half. What none of them could say is WHICH half was broken, because a score of
    /// nought looks the same whether the rule is missing or present and outvoted.
    /// </para>
    /// <para>
    /// <b>Coverage can say, and it could not before this session.</b> The question is whether
    /// any commitment expects <i>faint</i> with <i>cat</i> and <i>loudness</i> both in its
    /// scope. If one does, the seven refutations were about the seat and the vote work reaches
    /// them. If none does, they were about the link never forming, and no vote will help.
    /// </para>
    /// <para>
    /// <b>Read at the settings the implied half is measured at</b>, which is twenty tellings
    /// with every word claimed, the whole-moment root and credited mints. Anything else and
    /// the cell says something about a combination.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_a_two_statement_conclusion_is_held_and_unseated_or_never_formed()
    {
        const int Seeds = 3;

        var lesson = Lesson.Chained;
        var implied = lesson.Exam.Skip(lesson.Exam.Count / 2).ToList();

        output.WriteLine($"{Seeds} seeds, 20 tellings, claiming everything, wholly rooted");
        output.WriteLine("seed  answered  held");

        var answered = new List<double>();
        var held = new List<double>();

        for (var seed = 1; seed <= Seeds; seed++)
        {
            var ran = Ran(
                lesson with { Exam = implied }, Carrying.Never, seed, passes: 1,
                asserting: Asserting.Everything, tellings: 20, rooting: Rooting.Wholly,
                crediting: Crediting.Birth);

            var there = 0;

            foreach (var quiz in implied)
            {
                var words = Babi.Words(quiz.Question);

                // `what is the cat loudness` -- the thing and the property, which is every
                // code a rule for this question could root on.
                var subject = Babi.Of(words[3]);
                var attribute = Babi.Of(words[4]);

                var outcome = ran.World.Vocabulary
                    .Select((word, at) => (Word: word, At: at))
                    .Where(one => string.Equals(one.Word, quiz.Answer, StringComparison.Ordinal))
                    .Select(one => (int?)one.At)
                    .FirstOrDefault();

                if (outcome is not { } said) continue;

                if (ran.Brain.Held.All.Any(one =>
                    one.Expects == Brain.Says(said)
                    && one.Scope.Contains(subject)
                    && one.Scope.Contains(attribute)))
                    there++;
            }

            answered.Add(Right(ran.Tutor, pass: 0));
            held.Add(there / (double)implied.Count);

            output.WriteLine($"{seed,-6}{answered[^1],-10:F3}{held[^1]:F3}");
        }

        output.WriteLine(
            $"{Environment.NewLine}answered {Sweep.Spread(answered)}, "
            + $"held {Sweep.Spread(held)}");

        // No bar either way, because which of the two it is has never been read and a
        // threshold written before the first reading would be a prediction dressed as a
        // requirement. What is asserted is that the two numbers are not the same number --
        // if they were, coverage would be reporting the score by another name and could
        // separate nothing.
        Assert.True(
            Math.Abs(answered.Average() - held.Average()) > 0.0001
            || answered.Average() == 0.0,
            "what is answered and what is held read alike on the implied half, so this "
            + "instrument is measuring the score rather than the population and cannot say "
            + "which half of fork 125 is broken");
    }

    [Fact]
    public void A_drawn_lesson_states_every_truth_it_examines_and_no_word_twice()
    {
        var lesson = Lesson.Drawn(subjects: 4, attributes: 3, seed: 1);

        output.WriteLine(lesson.About);

        foreach (var line in lesson.Statements) output.WriteLine($"  {line}");

        output.WriteLine(string.Empty);

        foreach (var quiz in lesson.Exam)
            output.WriteLine($"  {quiz.Question} -> {quiz.Answer}");

        // One fact a subject-property pair, and the category lines state none. A generator
        // whose count drifted from its shape would put a grid under a world nobody described.
        Assert.Equal(4 * 3, lesson.Facts.Count);
        Assert.Equal((4 * 3) + 4, lesson.Statements.Count);

        // Every question is answered by a fact the lesson stated, which is what makes it an
        // examination rather than a guess. An answer key in the wrong alphabet scores nought
        // and looks like a verdict.
        var truths = lesson.Facts
            .ToDictionary(one => (one.Subject, one.Attribute), one => one.Answer);

        foreach (var quiz in lesson.Exam)
        {
            var words = Babi.Words(quiz.Question);

            Assert.Equal(quiz.Answer, truths[(words[3], words[4])]);
        }

        // And nothing is drawn twice, which is what stops a link the lesson never stated. A
        // value that was also a subject would let a rule reach an answer through a word that
        // happens to be spelt the same, and no reading could tell that from learning.
        var used = lesson.Statements
            .SelectMany(Babi.Words)
            .Where(word => word is not ("the" or "is" or "a"))
            .ToList();

        Assert.Equal(1 + 4 + 3 + (4 * 3), used.Distinct(StringComparer.Ordinal).Count());

        // Two seeds draw two lessons, or the world is not moving and the spread under every
        // reading is the spread of one text measured twice.
        Assert.NotEqual(
            lesson.Statements,
            Lesson.Drawn(subjects: 4, attributes: 3, seed: 2).Statements);

        // And one seed draws one lesson, twice.
        Assert.Equal(
            lesson.Statements,
            Lesson.Drawn(subjects: 4, attributes: 3, seed: 1).Statements);
    }

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

        var moments = new List<Coded>();

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
            Assert.Single(one.Said());
            Assert.Empty(one.Question());
        });

        Assert.Empty(moments[2].Said());
        Assert.NotEmpty(moments[2].Question());
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
            var (tally, tutor, _, _) = Ran(Lesson.Creatures, Carrying.Always, seed, Passes);

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

                // And every front end this repo has, on the arm that carries the story to the
                // question. A SELECTING front end is the one relevance mechanism here -- it is
                // what reads a second statement at the key the first supplied -- so if anything
                // available reaches a conclusion, that is where it would show.
                foreach (var reading in new[]
                {
                    Joining.Bagged, Joining.Chained, Joining.Distinguished, Joining.Resolved,
                })
                    two.Add(Right(
                        Ran(implied, Carrying.Statements, seed, passes: 1,
                            asserting: Asserting.Everything, tellings: many,
                            rooting: Rooting.Wholly, crediting: Crediting.Birth,
                            joining: reading).Tutor,
                        0));
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

    [Fact]
    public void Claiming_every_word_costs_a_population_that_grows_with_every_telling()
    {
        int[] tellings = [1, 3, 5, 10];

        // What one-shot is bought with, and it is the number that decides whether any of this
        // survives a corpus. A statement claims every word in turn, so each telling is as many
        // moments as the sentence has words — and every one of them may mint.
        var lesson = Lesson.Creatures;

        output.WriteLine($"{"tellings",-10}{"right",8}{"resident",10}{"minted",9}");

        var resident = new Dictionary<int, double>();

        foreach (var many in tellings)
        {
            var one = Ran(
                lesson, Carrying.Never, seed: 1, passes: 1, asserting: Asserting.Everything,
                tellings: many, rooting: Rooting.Wholly, crediting: Crediting.Birth);

            resident[many] = one.Tally.Resident;

            output.WriteLine(
                $"{many,-10}{Right(one.Tutor, 0),8:F3}{one.Tally.Resident,10}"
                + $"{one.Tally.Minted,9}");
        }

        // It is right from the first telling and keeps growing anyway, and the two columns say
        // which half grows. Minting SATURATES -- genesis has proposed everything the lesson can
        // propose -- while the resident count keeps climbing, so what grows is repair.
        //
        // And the reason is the claiming rule itself. A statement claims every word in turn, so
        // the rule learnt from one of its claims is WRONG on the others: `cat and sound predict
        // meow` fires on the round claiming `the` and misses. Every telling manufactures the
        // same failures again, repair specialises again, and nothing ever settles. That is the
        // shape that does not survive a corpus, and it is a cost of one-shot rather than of
        // this lesson.
        Assert.True(resident[10] > 3 * resident[1],
            $"the population grew from {resident[1]} to {resident[10]} over ten tellings, so "
            + "the churn this test is named for has stopped — say what removed it");

        // And the arm that claimed fewer words is refuted rather than open: it read 0.167 told
        // once against 1.000 and did not reach 1.000 until ten tellings. See the plan's row.
    }

    [Fact]
    public void The_tutor_never_answers_more_questions_than_it_put()
    {
        // A conservation law over the harness rather than a claim about the brain, and it
        // caught what several sessions of readings did not. A tutor holds the answer to the
        // question it has just put; if a moment arrives that nobody asked anything about, that
        // answer is live for it and a settlement lands where no question was.
        //
        // What put one there was reading a new line while a statement still owed moments: one
        // sentence is several under `Asserting.Everything`, so pulling early advanced the
        // source before its own sentence had finished arriving. `Conversing.Read` drains what
        // is owed first, and this is what says it still does.
        var lesson = Lesson.Creatures;
        var tutor = new Tutor(lesson, TextWriter.Null, passes: 3, tellings: 1);

        var brain = new Brain(
            new CommittingSettings
            {
                Capacity = 2000,
                Rooting = Rooting.Wholly,
                Crediting = Crediting.Birth,
            },
            seed: 1);

        var world = new Conversing(new ConversingSettings
        {
            Typed = tutor,
            Printed = tutor.Printed,
            Carrying = Carrying.Never,
            Asserting = Asserting.Everything,
        });

        var curiosity = new Curiosity(brain, rate: 1.0, seed: 1, world.Naming);

        var watching = new Watching<Coded>(
            world, new Joined(Joining.Bagged),
            acting: Chooses.From(felt => Doing(curiosity.Choose(felt)), curiosity.Cleared));

        new Bench(watching, brain)
            .Run(tutor.Moments * tutor.Longest, sweep: 200, target: 0.9, window: 50);

        var put = tutor.Put.Sum();

        output.WriteLine($"{put} questions put, {world.Told} answered, {world.Shrugged} shrugged");

        Assert.True(world.Told <= put,
            $"the tutor answered {world.Told} times having put {put} questions, so "
            + $"{world.Told - put} settlement(s) are attached to moments nobody asked anything "
            + "about. Every reading in this file is measured through the tutor, so none of "
            + "them can be read until this holds again.");
    }

    [Fact]
    public void An_answer_given_as_a_sentence_is_worth_what_the_word_alone_is()
    {
        const int Passes = 6;
        const int Seeds = 3;

        // John's, and it is what a person actually does. Asked `is it fur`, somebody answers
        // `the cat covering is fur` as readily as `fur` — and reading the first word of that
        // settles the round on `the`. The answer is now the reply's last word the question did
        // not already say, and the whole reply is told as a statement besides.
        //
        // Measured where the machine is WRONG often enough to be corrected, since a machine
        // that is right every time is never given a sentence at all.
        var lesson = Lesson.Creatures;

        output.WriteLine($"{Seeds} seeds, told once, corrections through a blank record");
        output.WriteLine($"{"pass",-6}{"word",9}{"sentence",10}");

        var word = new double[Passes];
        var sentence = new double[Passes];
        var told = new List<double>();
        var put = new List<double>();

        for (var seed = 1; seed <= Seeds; seed++)
        {
            var one = Ran(
                lesson, Carrying.Never, seed, passes: Passes, asserting: Asserting.Everything,
                tellings: 1, rooting: Rooting.Wholly);

            var other = Ran(
                lesson, Carrying.Never, seed, passes: Passes, asserting: Asserting.Everything,
                tellings: 1, rooting: Rooting.Wholly, replying: Replying.Sentence);

            told.Add(other.World.Told);
            put.Add(other.Tutor.Put.Sum());

            for (var pass = 0; pass < Passes; pass++)
            {
                word[pass] += Right(one.Tutor, pass) / Seeds;
                sentence[pass] += Right(other.Tutor, pass) / Seeds;
            }
        }

        for (var pass = 0; pass < Passes; pass++)
            output.WriteLine($"{pass + 1,-6}{word[pass],9:F3}{sentence[pass],10:F3}");

        output.WriteLine($"sentence arm: {told.Average():F1} answered of {put.Average():F1} put");

        // The same place by the same pass, so understanding a sentence costs the reading
        // nothing. It was worth nothing at all before: the first word of `the cat covering is
        // fur` is `the`.
        Assert.Equal(word[^1], sentence[^1]);

        Assert.True(sentence[^1] > 0.9,
            $"neither arm reached the examination, the last pass reading {sentence[^1]:F3}, so "
            + "this compares two failures rather than two answers");

        // And the conservation law holds through it, which is the thing that has to be checked
        // every time a reply grows a moment of its own.
        Assert.True(told.Average() <= put.Average(),
            $"the sentence arm answered {told.Average():F1} times having put {put.Average():F1} "
            + "questions");
    }

    [Fact]
    public void A_repair_that_cannot_be_judged_is_most_of_the_population_and_all_of_the_churn()
    {
        const int Tellings = 20;

        // Fork 86, and it is the one that killed ILP arriving here. The ladder extends only
        // when nothing in the current language separates the failures from the hits, and that
        // is the whole of what makes the bias EARNED rather than declared. Something always
        // separated: `wanting` read nought on a lesson whose answers a conjunction cannot
        // reach at all, so the trigger never fired where it was most needed.
        //
        // The second half of the bar needs no new number. A conjunctive child keeps the
        // parent's firings its condition was present in, so a condition present in fewer of
        // them than `Floor` mints a rule that can never clear the floor itself — one nothing
        // will ever be able to refute, which is what memorising is.
        var worlds = new[] { ("creatures", Lesson.Creatures), ("chained", Lesson.Chained) };

        output.WriteLine($"told {Tellings} times, one examination pass");
        output.WriteLine($"{"lesson",-11}{"admitting",-11}{"right",8}{"resident",10}{"wanting",9}");

        var right = new Dictionary<(string, Admitting), double>();
        var resident = new Dictionary<(string, Admitting), double>();
        var wanting = new Dictionary<(string, Admitting), double>();

        foreach (var (named, lesson) in worlds)
        {
            foreach (var admitting in new[] { Admitting.Anything, Admitting.Testable })
            {
                var one = Ran(
                    lesson, Carrying.Never, seed: 1, passes: 1, asserting: Asserting.Everything,
                    tellings: Tellings, rooting: Rooting.Wholly, crediting: Crediting.Birth,
                    admitting: admitting);

                right[(named, admitting)] = Right(one.Tutor, 0);
                resident[(named, admitting)] = one.Tally.Resident;
                wanting[(named, admitting)] = one.Tally.Wanting;

                output.WriteLine(
                    $"{named,-11}{admitting.ToString().ToLowerInvariant(),-11}"
                    + $"{Right(one.Tutor, 0),8:F3}{one.Tally.Resident,10}{one.Tally.Wanting,9:F3}");
            }
        }

        foreach (var (named, _) in worlds)
        {
            // It costs the score nothing on either world, which is what makes the rest of it
            // worth having rather than a trade.
            Assert.Equal(right[(named, Admitting.Anything)], right[(named, Admitting.Testable)]);

            // And it is most of the population. What the churn was buying is rules too small
            // for anything ever to judge.
            Assert.True(
                resident[(named, Admitting.Testable)] * 3
                    < resident[(named, Admitting.Anything)],
                $"on {named} the bar left {resident[(named, Admitting.Testable)]} residents "
                + $"against {resident[(named, Admitting.Anything)]}, so the churn it was named "
                + "for has gone by some other road");

            // And the ladder's trigger fires at last, on a population old enough to be asked.
            // Nought means the language never admits being short; anything above it is the
            // admission rule working where it could not before, including on a lesson a
            // conjunction cannot answer at all.
            //
            // A parent below the floor on EITHER side is refused as too young rather than
            // counted as a ceiling, which is what makes this number about the language. Before
            // that it read 1.000 at one telling, where nothing had been said twice.
            Assert.Equal(0.0, wanting[(named, Admitting.Anything)]);

            Assert.True(wanting[(named, Admitting.Testable)] > 0.1,
                $"on {named} the trigger read {wanting[(named, Admitting.Testable)]:F3}, so the "
                + "language still never says it is short");
        }
    }

    /// <summary>
    /// Why repair never fires at the telling the terminal ships — <b>the run is right fewer
    /// times than one commitment needs.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The reading above measures at twenty tellings and the deployment ships one</b>, and
    /// the two are not the same machine. <c>OpenPlexus.Talk</c> defaults to a single telling,
    /// and a session run that way settles 3,249 firings wrongly, refuses all 3,249 under the
    /// floor and reaches the candidate search nought times. Repair is present, wired and
    /// unable to fire.
    /// </para>
    /// <para>
    /// <b>And the cause is arithmetic rather than statistical</b>, which is what makes it
    /// worth a test of its own. <c>PastFloor</c> under <see cref="Admitting.Testable"/> wants
    /// <see cref="CommittingSettings.Floor"/> hits as well as that many misses, a
    /// commitment's hits can never exceed the rounds the run got right, and one telling of
    /// <see cref="Lesson.Creatures"/> gets eleven rounds right in total. No condition, no
    /// language and no separating code can reach a bar the whole run cannot supply the
    /// numerator for.
    /// </para>
    /// <para>
    /// <b>So <c>wanting</c> is a null reading wherever this holds</b>, and it reads 0.000
    /// exactly as it does where the language separates everything. That is this repo's own
    /// trap at the instrument: a check that cannot fire reads like a check that passes, and
    /// the harness now prints the gate census beside the number so the two are told apart.
    /// </para>
    /// <para>
    /// <b>The entry it feeds is carried by another arm.</b> <see cref="ExercisedTests"/> asks
    /// whether THE ARCHITECTURE's <i>understanding deepens without limit</i> is reached and
    /// takes any arm, so <c>Roaming</c> answers for a conversation that has never once
    /// specialised.
    /// </para>
    /// <para>
    /// <b>What this does not say is which way the floor should move.</b> Lowering it buys
    /// repair on a population nothing can judge, which is the churn the reading above deleted.
    /// The finding is that the conversation supplies too little settled evidence per rule for
    /// an evidence-gated mechanism, and that is a fact about the world rather than about the
    /// dial.
    /// </para>
    /// </remarks>
    [Fact]
    public void At_one_telling_the_run_is_right_too_few_times_for_any_commitment_to_be_repaired()
    {
        var floor = new CommittingSettings().Floor;

        output.WriteLine(
            $"creatures, one examination pass, the terminal's arms, floor {floor}");
        output.WriteLine(
            $"{"tellings",-10}{"right",8}{"wrong",8}{"at floor",10}{"searched",10}"
            + $"{"repaired",10}{"resident",10}");

        var searched = new Dictionary<int, long>();
        var right = new Dictionary<int, long>();

        foreach (var tellings in new[] { 1, 2, 5, 10, 20 })
        {
            var one = Ran(
                Lesson.Creatures, Carrying.Never, seed: 1, passes: 1,
                asserting: Asserting.Everything, tellings: tellings, rooting: Rooting.Wholly,
                crediting: Crediting.Birth, admitting: Admitting.Testable);

            var held = one.Brain.Held;

            searched[tellings] = held.Searched;
            right[tellings] = one.Tally.Right;

            output.WriteLine(
                $"{tellings,-10}{one.Tally.Right,8}{held.Wrong,8}{held.AtFloor,10}"
                + $"{held.Searched,10}{one.Tally.Repaired,10}{one.Tally.Resident,10}");
        }

        // A commitment's hits are a subset of the rounds the run got right, so a run right
        // fewer times than the floor cannot put one commitment past `Testable`'s hit side.
        // Computed from the run rather than pinned, so this closes by the world supplying more
        // settled evidence and never by editing the number.
        foreach (var (tellings, got) in right.Where(one => one.Value < floor))
            Assert.True(searched[tellings] == 0,
                $"at {tellings} telling(s) the run was right {got} times against a floor of "
                + $"{floor}, so no commitment can have cleared the hit side -- yet "
                + $"{searched[tellings]} reached the candidate search, which means the bar is "
                + "not the one being described here");

        // And the arms differ, which is what stops this being a reading about a mechanism that
        // is simply dead. Repeating the lesson is what buys the evidence, and it is the only
        // thing measured here that does.
        Assert.True(searched[20] > 0,
            $"repair reached the candidate search {searched[20]} times at twenty tellings, so "
            + "nothing in this grid separates a floor that is too high from a mechanism that "
            + "never runs");

        Assert.Equal(0, searched[1]);
    }

    /// <summary>
    /// Whether two rules one scope code apart name a pair of alternatives — <b>fork 80's
    /// ceiling, off a finished population.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Rung five names what CO-FIRES, and alternatives never do.</b> The plan carries that
    /// as the shape problem behind four refuted tries across two architectures, and
    /// <see cref="Codes.Counting"/>'s own remark says what is missing: a statistic whose null
    /// neither converges nor discards the partners the members share. Every reading tried so
    /// far has been over the MOMENT, where two substitutes are the one thing never there
    /// together. This reads the population instead and needs nothing built.
    /// </para>
    /// <para>
    /// <b>Two arms, and the second is what fork 80 is not.</b> Fork 80
    /// pairs residents that AGREE on what they expect and differ in one scope code, which is
    /// the redundancy reading — the code neither rule can see is doing no work. The other pairs
    /// residents that DISAGREE, which is the substitution reading: <c>the cat covering is</c>
    /// expecting <c>fur</c> beside <c>the dog covering is</c> expecting <c>hair</c> yields two
    /// pairs at once, and both are pairs of alternatives.
    /// </para>
    /// <para>
    /// <b>The null REWIRES the pairs and keeps every code's degree</b>, which the first null
    /// here did not and which is why its reading could not be believed. Shuffling which word
    /// sits in which slot leaves thirteen of twenty words in the value slot, so a random
    /// labelling calls most pairs same-slot for free, and a code appearing in hundreds of pairs
    /// carries its partners with it. Splitting every pair into its two ends, shuffling the ends
    /// and re-pairing them keeps each code in exactly as many pairs as it was in and destroys
    /// only who it was paired WITH. That is the one thing being asked about.
    /// </para>
    /// <para>
    /// <b>The first null is kept beside it rather than replaced</b>, because the two disagreeing
    /// is the whole reason to trust the second. A reading that moves when the control is
    /// repaired was a reading about the control.
    /// </para>
    /// <para>
    /// <b>A ceiling rather than a mechanism</b>, which is this repo's ordering: what the signal
    /// permits with no learning costs milliseconds against a runner's hour, and a grid cannot
    /// tell a rule that failed from a signal that was never there.
    /// </para>
    /// </remarks>
    [Fact]
    public void Two_rules_one_code_apart_name_a_pair_of_alternatives()
    {
        const int Tellings = 20;
        const int Shuffles = 20;

        var lesson = Lesson.Creatures;

        var one = Ran(
            lesson, Carrying.Never, seed: 1, passes: 1, asserting: Asserting.Everything,
            tellings: Tellings, rooting: Rooting.Wholly, crediting: Crediting.Birth,
            admitting: Admitting.Testable);

        // The lesson's own three slots, which is the only ground truth here. A pair inside one
        // is a pair of substitutes; a pair across two is not.
        var slots = new Dictionary<Code, int>();

        foreach (var fact in lesson.Facts)
        {
            slots[Babi.Of(fact.Subject)] = 0;
            slots[Babi.Of(fact.Attribute)] = 1;
            slots[Babi.Of(fact.Answer)] = 2;
        }

        var residents = one.Brain.Held.All.ToList();
        var agreeing = new List<(Code Left, Code Right)>();
        var disagreeing = new List<(Code Left, Code Right)>();

        for (var at = 0; at < residents.Count; at++)
        {
            for (var other = at + 1; other < residents.Count; other++)
            {
                if (residents[at].Scope.Length != residents[other].Scope.Length) continue;

                var left = residents[at].Scope.Except(residents[other].Scope).ToList();
                var right = residents[other].Scope.Except(residents[at].Scope).ToList();

                if (left.Count != 1 || right.Count != 1) continue;

                if (residents[at].Expects == residents[other].Expects)
                {
                    agreeing.Add((left[0], right[0]));
                    continue;
                }

                // The substitution reading yields BOTH pairs, which is the whole of why it is
                // a different shape rather than the same one filtered differently. The scopes
                // name the subjects that swap and the expectations name the values that swap
                // with them.
                disagreeing.Add((left[0], right[0]));
                disagreeing.Add((residents[at].Expects, residents[other].Expects));
            }
        }

        output.WriteLine($"{lesson.About}, told {Tellings} times, {residents.Count} residents");
        output.WriteLine(
            $"{"arm",-14}{"pairs",8}{"placed",8}{"same slot",11}{"rewired",9}{"labelled",10}");

        var shares = new Dictionary<string, (double Share, double Null, int Placed)>();

        foreach (var (named, pairs) in
            new[] { ("agreeing", agreeing), ("disagreeing", disagreeing) })
        {
            var placed = pairs
                .Where(pair => slots.ContainsKey(pair.Left) && slots.ContainsKey(pair.Right))
                .ToList();

            var share = placed.Count == 0
                ? 0.0
                : placed.Count(pair => slots[pair.Left] == slots[pair.Right])
                    / (double)placed.Count;

            // The label shuffle, kept only so the repair is visible. It preserves the slot
            // sizes and nothing else, so thirteen value words out of twenty make most pairs
            // same-slot before anything is measured.
            var labelled = 0.0;

            for (var shuffle = 0; shuffle < Shuffles; shuffle++)
            {
                var rng = new Random(Seeds.Apart(shuffle + 1, purpose: 80));
                var words = slots.Keys.ToList();
                var mixed = slots.Values.OrderBy(_ => rng.Next()).ToList();
                var swapped = words
                    .Select((word, at) => (word, slot: mixed[at]))
                    .ToDictionary(pair => pair.word, pair => pair.slot);

                labelled += placed.Count(pair => swapped[pair.Left] == swapped[pair.Right])
                    / (double)Math.Max(placed.Count, 1) / Shuffles;
            }

            // The rewiring, which is the null this reading is against. Every end stays, so a
            // code in two hundred pairs is in two hundred pairs afterwards; only its partner
            // changes.
            var rewired = 0.0;

            for (var shuffle = 0; shuffle < Shuffles; shuffle++)
            {
                var rng = new Random(Seeds.Apart(shuffle + 1, purpose: 81));
                var ends = placed
                    .SelectMany(pair => new[] { pair.Left, pair.Right })
                    .OrderBy(_ => rng.Next())
                    .ToList();

                var same = 0;

                for (var end = 0; end + 1 < ends.Count; end += 2)
                    if (slots[ends[end]] == slots[ends[end + 1]]) same++;

                rewired += same / (double)Math.Max(placed.Count, 1) / Shuffles;
            }

            shares[named] = (share, rewired, placed.Count);

            output.WriteLine(
                $"{named,-14}{pairs.Count,8}{placed.Count,8}{share,11:F3}{rewired,9:F3}"
                + $"{labelled,10:F3}");

            var spelt = one.World.Vocabulary;

            foreach (var group in placed
                .GroupBy(pair => (Low: Math.Min(pair.Left.Value, pair.Right.Value),
                                  High: Math.Max(pair.Left.Value, pair.Right.Value)))
                .OrderByDescending(group => group.Count())
                .Take(8))
            {
                var pair = group.First();
                var left = one.World.Naming(pair.Left) is { } at ? spelt[at] : "?";
                var right = one.World.Naming(pair.Right) is { } to ? spelt[to] : "?";

                output.WriteLine(
                    $"  {left,-10}{right,-10}{group.Count(),5} "
                    + $"{(slots[pair.Left] == slots[pair.Right] ? "same slot" : "across")}");
            }
        }

        Assert.True(shares["agreeing"].Placed > 0 && shares["disagreeing"].Placed > 0,
            "one arm found no pair between two words the lesson places, so this compares a "
            + "reading against nothing rather than against the other shape");

        // The reading, asserted rather than printed, because a number in a commit message is a
        // claim and this is the record. Both arms land BELOW a rewiring of their own pairs, so
        // the population's shape is dis-assortative by slot rather than silent about it: a
        // scope is a subject and an attribute among function words, and moving one code walks
        // between those two far more freely than it swaps one subject for another.
        //
        // It goes red the day either arm clears its own null, which is the day somebody should
        // look at this again. Nothing here can be satisfied by editing the file.
        foreach (var (named, reading) in shares)
            Assert.True(reading.Share < reading.Null,
                $"the {named} arm lands {reading.Share:F3} against a rewired null of "
                + $"{reading.Null:F3}, so the population's shape now carries a grouping it did "
                + "not when this was refuted -- read the DO NOT RE-TRY rows and take the arm "
                + "off them");
    }

    /// <summary>
    /// Whether what ARRIVES in one context names a pair of alternatives — <b>fork 129's
    /// ceiling, off the stream with no learner.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Fork 80 asked the population and the population said no.</b> Two residents one scope
    /// code apart are dis-assortative by slot, both shapes, both below a rewiring of their own
    /// pairs — the rows are in <c>DO NOT RE-TRY</c>. What that refutes is reading likeness off
    /// the SHAPE of what is held.
    /// </para>
    /// <para>
    /// <b>This reads the residual instead, which is a different signal.</b> Two substitutes
    /// never share a moment, so no counting of company reaches them; what they share is the
    /// slot they arrive in. A context leaving the subject unsaid is followed by <c>cat</c>,
    /// <c>dog</c>, <c>bird</c> and <c>snake</c> in turn. The alternatives are what a prediction
    /// from that context would be wrong about.
    /// </para>
    /// <para>
    /// <b>And it wants no brain</b>, which is why it comes before any mechanism. A context is a
    /// moment with one code dropped and an arrival is what the stream put next, so this is the
    /// world and the front end and nothing else.
    /// </para>
    /// <para>
    /// <b>Two lessons, because one is a story.</b> <see cref="Lesson.Chained"/> is the control
    /// that matters rather than a second helping of the same shape: a sound is a VALUE in one
    /// of its statements and a SUBJECT in the next, so a word sits in two slots at once and the
    /// ground truth stops being a function. A pair counts as same-slot where the two words
    /// share any slot, which is the only reading that survives a word being both.
    /// </para>
    /// <para>
    /// <b>The cut at recurrence was chosen after seeing the first grid</b>, and that is said
    /// rather than hidden. What justifies it is that a pair several contexts agree on is the
    /// claim itself; what would have made it a fit to one lesson is the second lesson, which is
    /// why the second lesson is here.
    /// </para>
    /// <para>
    /// <b>The null is a rewiring.</b> Every pair is split into its ends, the ends shuffled and
    /// re-paired, so each word stays in as many pairs as it was in and only its partner
    /// changes. A word that arrives everywhere cannot buy a share that way.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_arrives_in_one_context_names_a_pair_of_alternatives()
    {
        const int Tellings = 20;
        const int Shuffles = 20;

        output.WriteLine(
            $"told {Tellings} times, contexts are a moment with one code dropped");
        output.WriteLine(
            $"{"lesson",-11}{"moments",9}{"pairs",7}{"placed",8}{"share",8}{"null",7}"
            + $"{"cleared",8}{"share",8}{"null",7}");

        var recurring = new Dictionary<string, (double Share, double Null, int Count)>();
        var every = new Dictionary<string, (double Share, double Null)>();

        foreach (var (named, lesson) in
            new[] { ("creatures", Lesson.Creatures), ("chained", Lesson.Chained) })
        {
            var (world, stream) = Streamed(lesson, Tellings);
            var arrivals = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);

            foreach (var (moment, followed) in stream)
            {
                if (followed is not { } arrived) continue;

                var ordered = moment.Order().ToList();

                foreach (var dropped in ordered)
                {
                    var context = string.Join(
                        ",", ordered.Where(code => code != dropped).Select(code => code.Value));

                    if (!arrivals.TryGetValue(context, out var seen))
                        arrivals[context] = seen = [];

                    seen.Add(arrived);
                }
            }

            var slots = Slotted(lesson);
            var spelt = world.Vocabulary;

            HashSet<int>? Slot(int outcome) =>
                outcome >= 0 && outcome < spelt.Count
                    && slots.TryGetValue(spelt[outcome], out var at)
                        ? at
                        : null;

            bool Together(int left, int right) =>
                Slot(left) is { } one && Slot(right) is { } other && one.Overlaps(other);

            var pairs = new List<(int Left, int Right)>();

            foreach (var seen in arrivals.Values.Where(one => one.Count > 1))
            {
                var members = seen.Order().ToList();

                for (var at = 0; at < members.Count; at++)
                    for (var other = at + 1; other < members.Count; other++)
                        pairs.Add((members[at], members[other]));
            }

            var placed = pairs
                .Where(pair => Slot(pair.Left) is not null && Slot(pair.Right) is not null)
                .ToList();

            // The pairs commoner across contexts than their own frequencies would have them,
            // corrected for how many were considered. `Commoner` says why this is not a count.
            var repeated = Commoner([.. arrivals.Values])
                .Where(pair => Slot(pair.Left) is not null && Slot(pair.Right) is not null)
                .ToList();

            double Shared(List<(int Left, int Right)> over) => over.Count == 0
                ? 0.0
                : over.Count(pair => Together(pair.Left, pair.Right)) / (double)over.Count;

            every[named] = (Shared(placed), Rewired(placed, Together, 129, Shuffles));
            recurring[named] = (
                Shared(repeated), Rewired(repeated, Together, 130, Shuffles), repeated.Count);

            output.WriteLine(
                $"{named,-11}{stream.Count,9}{pairs.Count,7}{placed.Count,8}"
                + $"{every[named].Share,8:F3}{every[named].Null,7:F3}"
                + $"{repeated.Count,7}{recurring[named].Share,8:F3}"
                + $"{recurring[named].Null,7:F3}");

            foreach (var group in placed
                .GroupBy(pair => (Low: Math.Min(pair.Left, pair.Right),
                                  High: Math.Max(pair.Left, pair.Right)))
                .OrderByDescending(group => group.Count())
                .Take(10))
            {
                var pair = group.First();

                output.WriteLine(
                    $"  {spelt[pair.Left],-10}{spelt[pair.Right],-10}{group.Count(),5} "
                    + $"{(Together(pair.Left, pair.Right) ? "same slot" : "across")}");
            }
        }

        // The signal is asserted over EVERY pair, because that is all this reading can carry.
        // A hundred and twenty-seven contexts is too few for a corrected z over three hundred
        // candidate pairs -- six clear on one lesson and none on the other, and the six come
        // from two slots so a rewiring of them is degenerate at 1.000 as well. The gate has to
        // be read where the counts are, which is the residents' own scopes and the reading
        // below this one.
        foreach (var (named, reading) in every)
            Assert.True(reading.Share > reading.Null,
                $"on {named} what arrives in one context is same-slot {reading.Share:F3} "
                + $"against a rewiring at {reading.Null:F3}, so the residual carries no "
                + "grouping and the whole line of attack goes beside fork 80");

        // And the gate is under-powered here rather than absent, said with the number rather
        // than left for somebody to rediscover.
        foreach (var (named, reading) in recurring)
            output.WriteLine(
                $"the corrected z clears {reading.Count} on {named} -- too few contexts to "
                + "gate on, and `A_residents_own_scope` is where it is read");
    }

    /// <summary>
    /// Whether a resident's OWN scope is context enough to name a pair — <b>where the
    /// mechanism would plug in, if it plugs in anywhere.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A brain holds no such thing as every one-code-dropped moment.</b> It holds the
    /// scopes genesis and repair happened to mint, so the signal
    /// being there in principle says nothing about it being reachable in a run. Fork 80 is the
    /// warning: the population's SHAPE turned out dis-assortative where the stream is not.
    /// </para>
    /// <para>
    /// <b>So the contexts here are the residents themselves.</b> A commitment is a scope and an
    /// expectation, it fires wherever its scope is a subset of the moment, and what arrives
    /// across those firings is what it would be wrong about. That is a confusion pair per
    /// resident and it needs no table of contexts — which matters, because a per-context store
    /// is the blow-up fork 31 already names.
    /// </para>
    /// <para>
    /// <b>Replayed against the finished population rather than watched live</b>, which is the
    /// cheaper half of the same question and is honest about being post-hoc. What it cannot say
    /// is whether the pairs would have been available EARLY; what it can say is whether they
    /// are there at all, and a nought here would stop the mechanism before it was written.
    /// </para>
    /// <para>
    /// <b>And a resident firing on one thing only is skipped</b>, because a context followed by
    /// one arrival names no pair. The share of residents that are is printed, since that is the
    /// mechanism's real coverage and a reading over the rest would flatter it.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_residents_own_scope_is_context_enough_to_name_a_pair()
    {
        const int Tellings = 20;
        const int Shuffles = 20;

        output.WriteLine($"told {Tellings} times, contexts are the residents' own scopes");
        output.WriteLine(
            $"{"lesson",-11}{"resident",10}{"asked",7}{"placed",8}"
            + $"{"cleared",8}{"share",8}{"null",7}");

        var recurring = new Dictionary<string, (double Share, double Null, int Count)>();

        foreach (var (named, lesson) in
            new[] { ("creatures", Lesson.Creatures), ("chained", Lesson.Chained) })
        {
            var ran = Ran(
                lesson, Carrying.Never, seed: 1, passes: 1, asserting: Asserting.Everything,
                tellings: Tellings, rooting: Rooting.Wholly, crediting: Crediting.Birth,
                admitting: Admitting.Testable);

            // The same stream the ceiling read, so the two readings are over one world.
            var (world, stream) = Streamed(lesson, Tellings);
            var slots = Slotted(lesson);
            var spelt = world.Vocabulary;

            bool Together(int left, int right) =>
                left >= 0 && left < spelt.Count && right >= 0 && right < spelt.Count
                && slots.TryGetValue(spelt[left], out var one)
                && slots.TryGetValue(spelt[right], out var other)
                && one.Overlaps(other);

            var residents = ran.Brain.Held.All.ToList();
            var contexts = new List<HashSet<int>>();

            foreach (var resident in residents)
            {
                var seen = new HashSet<int>();

                foreach (var (moment, arrived) in stream)
                    if (arrived is { } followed && resident.Scope.All(moment.Contains))
                        seen.Add(followed);

                if (seen.Count > 1) contexts.Add(seen);
            }

            var asked = contexts.Count;

            bool Placed(int at) =>
                at >= 0 && at < spelt.Count && slots.ContainsKey(spelt[at]);

            var placed = contexts
                .SelectMany(seen =>
                {
                    var members = seen.Order().ToList();

                    return
                        from at in Enumerable.Range(0, members.Count)
                        from other in Enumerable.Range(at + 1, members.Count - at - 1)
                        select (Left: members[at], Right: members[other]);
                })
                .Where(pair => Placed(pair.Left) && Placed(pair.Right))
                .ToList();

            // The same bar the stream reading uses, so the two are one comparison.
            var repeated = Commoner(contexts)
                .Where(pair => Placed(pair.Left) && Placed(pair.Right))
                .ToList();

            var share = repeated.Count == 0
                ? 0.0
                : repeated.Count(pair => Together(pair.Left, pair.Right))
                    / (double)repeated.Count;

            var rewired = Rewired(repeated, Together, 131, Shuffles);

            recurring[named] = (share, rewired, repeated.Count);

            output.WriteLine(
                $"{named,-11}{residents.Count,10}{asked,7}{placed.Count,8}"
                + $"{repeated.Count,7}{share,8:F3}{rewired,7:F3}");

            foreach (var group in placed
                .GroupBy(pair => (Low: Math.Min(pair.Left, pair.Right),
                                  High: Math.Max(pair.Left, pair.Right)))
                .OrderByDescending(group => group.Count())
                .Take(8))
            {
                var pair = group.First();

                output.WriteLine(
                    $"  {spelt[pair.Left],-10}{spelt[pair.Right],-10}{group.Count(),5} "
                    + $"{(Together(pair.Left, pair.Right) ? "same slot" : "across")}");
            }
        }

        foreach (var (named, reading) in recurring)
        {
            Assert.True(reading.Count > 0,
                $"on {named} no resident's scope is followed by two placed words twice, so the "
                + "population holds no context a pair could be read off and the mechanism has "
                + "nowhere to plug in");

            Assert.True(reading.Share > reading.Null,
                $"on {named} pairs from a resident's own scope are same-slot {reading.Share:F3} "
                + $"against a rewiring at {reading.Null:F3}, so the signal the stream carries "
                + "is not reachable through the scopes a run actually mints");
        }
    }

    /// <summary>
    /// Which vocabulary a group off the misses is IN — <b>and nothing in the brain reads that
    /// one.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The ceilings said build it and this says out of what.</b> A commitment that fires
    /// and is wrong says what it wanted and what came, so the
    /// pair is two codes standing in one another's place — and both of them are OUTCOME codes,
    /// modality <c>Brain.Followed</c>, numbered by the world's own vocabulary index.
    /// </para>
    /// <para>
    /// <b><see cref="Categories"/> is a vocabulary of MOMENT codes and the two are not one.</b>
    /// A word reaches a scope as <c>Babi.Of</c>'s hash and reaches an expectation as
    /// <c>Brain.Says</c>'s index, so the machine holds two names for one word and only the
    /// world knows they are the same. Learning an outcome group into <c>Sorts</c> puts a code
    /// in the fold's alphabet that no moment can hold and asks <c>Coarser</c> a question about
    /// scopes it can never answer.
    /// </para>
    /// <para>
    /// <b>This is the reading that stopped the build</b>, and it is asserted rather than
    /// remembered. A mechanism was written, wired into settlement and the sweep, given a dial,
    /// and taken out again: <c>Under</c> reads <c>Sorts.Coarser</c> over the SCOPE alone, so
    /// the two arms of that dial could not have differed in anything a run does. A dial whose
    /// arms cannot come apart is this repo's oldest trap wearing a new mechanism's clothes.
    /// </para>
    /// <para>
    /// <b>What the misses name is a category over EXPECTATIONS</b>, and nothing consumes one
    /// yet. That is the build, and it is the architecture's own line rather than a detour: a
    /// goal is a set of codes wanted present, so a goal and a prediction are one type once
    /// what is expected is a set.
    /// </para>
    /// <para>
    /// <b>And the two alphabets are not one set under two names.</b> Nine of the twenty-three
    /// words this lesson can expect never reach a scope at all, and they are answers — a word
    /// only said at the end of a sentence has no successor to predict, so no moment carrying
    /// it ever settles and no rule is ever conditioned on having heard it.
    /// </para>
    /// <para>
    /// <b>So it can answer with a word it can never reason from</b>, which bounds a
    /// chain before any population does. A conclusion wanting one of those nine as its premise
    /// is unreachable however many rules are held, and that is the same wall
    /// <c>ChainingTests</c> reports from the other side.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_group_off_the_misses_is_in_the_outcome_alphabet_and_nothing_reads_that_one()
    {
        var lesson = Lesson.Creatures;

        var one = Ran(
            lesson, Carrying.Never, seed: 1, passes: 1, asserting: Asserting.Everything,
            tellings: 20, rooting: Rooting.Wholly, crediting: Crediting.Birth,
            admitting: Admitting.Testable);

        var expectations = one.Brain.Held.All.Select(held => held.Expects).Distinct().ToList();
        var scoped = one.Brain.Held.All.SelectMany(held => held.Scope).Distinct().ToList();

        var outcomes = expectations.Count(code => Brain.Meant(code) is not null);
        var crossing = scoped.Count(code => Brain.Meant(code) is not null);

        output.WriteLine($"{lesson.About}, told 20 times, {one.Tally.Resident} residents");
        output.WriteLine(
            $"expectations : {expectations.Count} distinct, {outcomes} of them outcome codes");
        output.WriteLine(
            $"scope codes  : {scoped.Count} distinct, {crossing} of them outcome codes");
        output.WriteLine(
            "a word is `Babi.Of`'s hash in a scope and `Brain.Says`'s index in an expectation, "
            + "so a group over one is unreadable in the other");

        // Every expectation is in the outcome alphabet, which is what makes a group off the
        // misses an outcome group. Computed rather than pinned, so it closes if the two
        // alphabets are ever made one.
        Assert.Equal(expectations.Count, outcomes);

        // And no scope code is, so `Categories` and a group off the misses share nothing. If
        // this ever goes red the vocabularies have met and the mechanism can be built.
        Assert.Equal(0, crossing);

        // What the fork actually costs, which naming it does not say. If every expectation is
        // a word that also reaches a scope, the two alphabets are one set under two names and
        // joining them is a renaming. If they are not, joining them changes what can be
        // expected and what can be perceived at once, and that is a decision rather than a
        // repair.
        var spoken = one.World.Vocabulary;

        var expected = expectations
            .Select(code => Brain.Meant(code))
            .Where(at => at is >= 0 && at < spoken.Count)
            .Select(at => spoken[at!.Value])
            .ToHashSet(StringComparer.Ordinal);

        var perceived = scoped
            .Select(code => one.World.Naming(code))
            .Where(at => at is not null)
            .Select(at => spoken[at!.Value])
            .ToHashSet(StringComparer.Ordinal);

        var both = expected.Intersect(perceived, StringComparer.Ordinal).Count();

        output.WriteLine(
            $"as words     : {expected.Count} expected, {perceived.Count} perceived, "
            + $"{both} in both, {expected.Except(perceived, StringComparer.Ordinal).Count()} "
            + "expected and never perceived");

        // How often each of them was actually THERE, because a word in no moment and a word in
        // hundreds of them are two different faults and the list alone cannot tell them apart.
        // My first account of this said the moments never settle, and they do.
        var (_, stream) = Streamed(lesson, 20);

        foreach (var word in expected
            .Except(perceived, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal))
        {
            var code = Babi.Of(word);

            var anywhere = stream.Count(turn => turn.Moment.Contains(code));
            var settling = stream.Count(
                turn => turn.Arrived is not null && turn.Moment.Contains(code));

            output.WriteLine(
                $"  expected only : {word,-10} in {anywhere,4} moments, {settling,4} of them "
                + "settled, 0 scopes");
        }

        foreach (var word in perceived
            .Except(expected, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal))
            output.WriteLine($"  perceived only: {word}");

        Assert.True(both > 0,
            "no word is both expected and perceived, so the two alphabets are not one set "
            + "under two names and fork 137 is a change to what can be expressed rather than "
            + "a renaming");

        // The half that prices the fork, and it is a ceiling on chaining rather than a fact
        // about names. A word the machine can expect and never perceive is one it can answer
        // with and never reason FROM, so any conclusion needing it as a premise is unreachable
        // whatever the population holds. This is asserted as an inequality because it closes
        // the day a front end puts those words in a scope, and that is the fix rather than
        // this file.
        Assert.True(expected.Count > both,
            $"every one of the {expected.Count} words the machine can expect also reaches a "
            + "scope, so nothing here bounds a chain and this reading has stopped saying "
            + "anything -- take it off fork 137");
    }

    /// <summary>
    /// What a sentence TERMINATOR would buy the nine words nothing can be conditioned on —
    /// <b>John's, and it is priced before it is built.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The period is split away as a word break and becomes nothing.</b>
    /// <c>Babi.Words</c> breaks on it, so a sentence's last word is followed by the next
    /// sentence's first — across a boundary the machine cannot see — or by nothing at all.
    /// John asked whether the full stop could be the thing predicted there, and it is the right
    /// question: a word with a successor to predict sits in a moment that SETTLES, and a
    /// settled moment is the only kind a scope is ever rooted on.
    /// </para>
    /// <para>
    /// <b>Priced without changing the world</b>, which is the ordering this repo keeps. What a
    /// terminator would buy is exactly the codes that reach a moment and never reach a settled
    /// one, and both sets are readable off the stream with no learner. A world change costs a
    /// re-take of every recorded number, so it is worth knowing the answer first.
    /// </para>
    /// <para>
    /// <b>And what it would cost is in the same reading.</b> Every sentence would end with the
    /// same arrival, so the terminator becomes the commonest outcome in the world and a rule
    /// predicting it is right constantly while teaching nothing — the plan already names that
    /// shape, the informative words being the unpredictable ones and the predictable ones being
    /// <c>to</c> and <c>the</c>. The share it would take is printed beside what it buys.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_a_sentence_terminator_would_buy_the_words_nothing_is_conditioned_on()
    {
        const int Tellings = 20;

        output.WriteLine($"told {Tellings} times, no world changed, no learner run");
        output.WriteLine(
            $"{"lesson",-11}{"turns",7}{"settled",9}{"in any",8}{"in settled",12}"
            + $"{"bought",8}{"cost",7}");

        var bought = new Dictionary<string, int>();

        foreach (var (named, lesson) in
            new[] { ("creatures", Lesson.Creatures), ("chained", Lesson.Chained) })
        {
            var (world, stream) = Streamed(lesson, Tellings);

            var anywhere = stream.SelectMany(turn => turn.Moment).ToHashSet();

            var settling = stream
                .Where(turn => turn.Arrived is not null)
                .SelectMany(turn => turn.Moment)
                .ToHashSet();

            // A moment nothing followed is where a terminator would land, so the turns it
            // would settle are the unsettled ones and the codes it would buy are theirs.
            var unsettled = stream.Count(turn => turn.Arrived is null);

            var gained = anywhere.Except(settling).ToList();

            bought[named] = gained.Count;

            // What it costs: the terminator becomes one more outcome and takes every turn it
            // settles, so this is its share of all settled turns after the change.
            var share = unsettled / (double)(stream.Count(turn => turn.Arrived is not null)
                + unsettled);

            output.WriteLine(
                $"{named,-11}{stream.Count,7}{stream.Count - unsettled,9}{anywhere.Count,8}"
                + $"{settling.Count,12}{gained.Count,8}{share,7:F3}");

            foreach (var code in gained.Take(12))
                output.WriteLine(
                    "  bought : "
                    + (world.Naming(code) is { } at ? world.Vocabulary[at] : code.ToString()));
        }

        // The reading, and it can fail either way. Nought bought says a sentence's last word is
        // in no moment at all, so a terminator settles nothing that was not already settled and
        // John's fix does not reach the nine; anything above it is the size of what it reaches.
        foreach (var (named, gained) in bought)
            Assert.True(gained > 0,
                $"on {named} every code that reaches a moment already reaches a settled one, so "
                + "a terminator would buy no word a scope could be conditioned on and the nine "
                + "are unreachable by this road");
    }

    /// <summary>
    /// Whether which words can ever be a PREMISE depends on the order the lesson was told
    /// in — <b>the control, because two accounts of this were already wrong.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Nine answer words never reach a scope, and it is not settlement.</b>
    /// Each of the nine sits in eighty moments, all eighty settled, and no resident is ever
    /// conditioned on one. A sentence terminator buys a single code and it is <c>what</c>. Both
    /// of those were measured after being asserted, which is why this one is a control rather
    /// than a third account.
    /// </para>
    /// <para>
    /// <b>The suspect is the surprise gate.</b> The tell is which words got through.
    /// <c>fur</c>, <c>meow</c> and <c>fish</c> reach scopes and they are the cat's — the FIRST
    /// creature told. Genesis mints only where nothing accounted for what arrived, so once a
    /// rule covers <i>the</i> as the next word the later sentences stop surprising, and repair
    /// narrows by a DISCRIMINATOR, which is the subject rather than the answer. An answer word
    /// would then be premisable only where it arrived before the population closed over it.
    /// </para>
    /// <para>
    /// <b>So reversing the telling order is the control.</b> If the premisable set follows the
    /// order, the cause is genesis rather than anything about answers, and what a machine can
    /// reason FROM is a fact about what it happened to hear first. If the same words come
    /// through both ways, the order is innocent and the account above joins the other two.
    /// </para>
    /// <para>
    /// <b>It is fork 135's territory arriving from a new side.</b> That fork says a lucky
    /// advocate blocks genesis and proposals stop; this says what the blocking COSTS is not
    /// population, it is which words can ever be premises.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_a_word_can_be_a_premise_follows_the_order_the_lesson_was_told_in()
    {
        const int Tellings = 20;

        var forward = Lesson.Creatures;
        var backward = forward with { Statements = [.. forward.Statements.Reverse()] };

        output.WriteLine($"told {Tellings} times, the same lesson in two orders");

        var reached = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var (named, lesson) in new[] { ("forward", forward), ("backward", backward) })
        {
            var one = Ran(
                lesson, Carrying.Never, seed: 1, passes: 1, asserting: Asserting.Everything,
                tellings: Tellings, rooting: Rooting.Wholly, crediting: Crediting.Birth,
                admitting: Admitting.Testable);

            var spelt = one.World.Vocabulary;

            var scoped = one.Brain.Held.All
                .SelectMany(held => held.Scope)
                .Distinct()
                .Select(code => one.World.Naming(code))
                .Where(at => at is not null)
                .Select(at => spelt[at!.Value])
                .ToHashSet(StringComparer.Ordinal);

            // The answers only, because the function words and the subjects reach a scope
            // either way and would drown the comparison.
            var answers = lesson.Facts.Select(fact => fact.Answer).ToHashSet(StringComparer.Ordinal);

            reached[named] = answers.Intersect(scoped, StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);

            output.WriteLine(
                $"{named,-9}: {one.Tally.Resident,4} residents, "
                + $"{reached[named].Count} of {answers.Count} answer words reach a scope -- "
                + string.Join(" ", reached[named].Order(StringComparer.Ordinal)));
        }

        Assert.True(reached["forward"].Count > 0 || reached["backward"].Count > 0,
            "no answer word reaches a scope in either order, so this compares two nothings and "
            + "the order cannot be the cause of anything");

        // Asserted as a difference rather than a direction, because a prediction written into a
        // wiring check fails two ways and reads the same. Which words came through in which
        // order is in the output and in the commit.
        Assert.False(
            reached["forward"].SetEquals(reached["backward"]),
            "the same answer words reach a scope whichever order the lesson is told in, so "
            + "genesis is not what decides which words can be a premise and the account in this "
            + "remark joins the two before it");
    }

}
