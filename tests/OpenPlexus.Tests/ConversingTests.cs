using System.Text;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The conversation harness, driven by a human who answers only what they know.
/// </summary>
/// <remarks>
/// <para>
/// <b>A scripted transcript cannot drive this</b>, and that is a fact about the world. The machine
/// consumes a reply only where it decided to ask, so which line of a script lands where depends on
/// what the population did — the human has to be a program that reacts rather than a list of
/// lines. Writing one is what forced the question of what somebody who does not know is supposed
/// to say.
/// </para>
/// <para>
/// <b>They say nothing</b>, and it is the common case rather than the awkward one. A statement the
/// machine asks about is a question nobody has an answer to, so the reply is blank and the round
/// settles nothing.
/// </para>
/// </remarks>
public sealed class ConversingTests(ITestOutputHelper output)
{
    private static readonly string[] Cast = ["mary", "john", "sandra", "daniel"];

    private static readonly string[] Places = ["kitchen", "garden", "office", "bedroom"];

    private static readonly string[] Colours = ["red", "blue", "green", "yellow"];

    /// <summary>What a topic is about, which is what decides whether recency can answer it.</summary>
    /// <remarks>
    /// <para>
    /// <b>Three worlds rather than three dials</b>. Which thing a topic moves around is a fact
    /// about the problem, so it accumulates as a world does and goes when its question shuts.
    /// </para>
    /// <para>
    /// <b>And <see cref="Props"/> is John's</b>. Two balls of different colours in two rooms, and
    /// the question names the colour — so the room mentioned last belongs to the OTHER ball, and
    /// the only thing pairing a colour with its room is that they shared a statement. That makes
    /// the recency bar wrong by construction rather than usually wrong, which
    /// <see cref="Crowded"/> only manages on average.
    /// </para>
    /// </remarks>
    private enum Telling
    {
        /// <summary>One person moving, so the freshest room is always where they are.</summary>
        Alone,

        /// <summary>Several people moving, so the freshest room is usually somebody else's.</summary>
        Crowded,

        /// <summary>Coloured balls, so the question names which one and the last room is another's.</summary>
        Props,
    }

    /// <summary>
    /// Somebody at a terminal, who answers a question about a question and shrugs at everything
    /// else.
    /// </summary>
    /// <remarks>
    /// <b>It watches what was printed, because that is all it is given</b>. A reply is wanted
    /// exactly where the last thing written has no newline after it, which is the shape of a
    /// prompt — so this reads the terminal the way a person does rather than being told out of
    /// band which read is which.
    /// </remarks>
    private sealed class Human(
        StringBuilder printed, int exchanges, int seed, int moves = 1,
        Telling telling = Telling.Alone)
        : TextReader
    {
        private readonly Random _draws = new(seed);
        private readonly Queue<(string Line, string? Answer)> _topic = new();

        private string? _answer;
        private int _at;

        /// <summary>How many times it was asked something nobody could answer.</summary>
        public int Shrugged { get; private set; }

        /// <summary>How many times it confirmed a guess.</summary>
        public int Confirmed { get; private set; }

        /// <summary>How many times it corrected one.</summary>
        public int Corrected { get; private set; }

        /// <summary>Questions put.</summary>
        public int Questions { get; private set; }

        /// <summary>
        /// How often the answer is the room mentioned LAST, which needs no learning at all.
        /// </summary>
        /// <remarks>
        /// <b>The ceiling a front-end arm has to beat, and it costs milliseconds</b>. John's
        /// question: is <c>Latest</c> tracking a thing that moves, or is it scoring a world where
        /// the freshest room is always the answer? Where one person moves alone it is always the
        /// answer and the arm can be read as having learnt nothing. This is the number that says
        /// which, and taking it after the grid rather than before it was the mistake.
        /// </remarks>
        public int Recency { get; private set; }

        public override string? ReadLine()
        {
            var whole = printed.ToString();
            var tail = whole[(whole.LastIndexOf('\n') + 1)..];

            return tail.StartsWith("  ? ", StringComparison.Ordinal)
                ? Replying(tail[4..].Trim())
                : Saying();
        }

        private string Replying(string guessed)
        {
            if (_answer is null)
            {
                Shrugged++;

                return string.Empty;
            }

            if (string.Equals(guessed, _answer, StringComparison.Ordinal))
            {
                Confirmed++;

                return "yes";
            }

            Corrected++;

            return _answer;
        }

        private string? Saying()
        {
            if (_topic.Count == 0)
            {
                if (_at >= exchanges) return null;

                Exchange();
            }

            var (line, answer) = _topic.Dequeue();

            _answer = answer;

            return line;
        }

        /// <summary>One exchange: a placement, then a question about somebody placed.</summary>
        /// <remarks>
        /// <para>
        /// <b>One statement and one question</b>, which is the shape every earlier row was taken
        /// on. Emitting a topic's placements in a block and asking once at the end is a
        /// different world — far fewer questions a round, and the question no longer sits
        /// between the moves — so crowding has to be the only thing that changes.
        /// </para>
        /// <para>
        /// <b>Crowded is what breaks the recency shortcut</b>. With one person moving alone the
        /// freshest room is always where they are, so an arm reading the last mention cannot be
        /// told from one that tracks anything. With several moving, the question is about
        /// whoever it is about and the answer is often not the last room said.
        /// </para>
        /// </remarks>
        private void Exchange()
        {
            // A topic starts over every `moves` exchanges, which is the corpus's story boundary
            // typed by hand. At one that is every exchange and nobody is ever in two rooms.
            if (_moved == 0)
            {
                _where.Clear();
                _placed.Clear();
                _only = Cast[_draws.Next(Cast.Length)];

                _topic.Enqueue((string.Empty, null));
            }

            var who = telling switch
            {
                Telling.Alone => _only,
                Telling.Crowded => Cast[_draws.Next(Cast.Length)],
                _ => Colours[_draws.Next(Colours.Length)],
            };

            var room = Places[_draws.Next(Places.Length)];

            _where[who] = room;
            _last = room;

            // Newest last, and moved to the end when it is placed again -- otherwise `_placed`
            // records who was FIRST seen last, and the ball this world means to exclude is not
            // the one at the end.
            _placed.Remove(who);
            _placed.Add(who);

            _topic.Enqueue((
                telling is Telling.Props
                    ? $"the {who} ball is in the {room}"
                    : $"{who} is in the {room}",
                null));

            // Uniformly over whoever is placed, EXCEPT on John's world, where the question is
            // deliberately about something that was put somewhere earlier. Drawing uniformly
            // there made it the crowded world with different nouns -- the recency bar came back
            // at the same 0.587, because what the bar measures does not care which words are
            // used. Naming an earlier ball is the whole of what makes it a different question.
            //
            // It is still right by coincidence about a quarter of the time, since the ball asked
            // about may happen to be in the room the newest one went to. That floor is the rooms
            // rather than the design.
            var asked = telling switch
            {
                Telling.Alone => _only,
                Telling.Crowded => _placed[_draws.Next(_placed.Count)],
                _ => _placed.Count > 1
                    ? _placed[_draws.Next(_placed.Count - 1)]
                    : _placed[0],
            };
            var answer = _where[asked];

            Questions++;
            if (string.Equals(answer, _last, StringComparison.Ordinal)) Recency++;

            _topic.Enqueue((
                telling is Telling.Props
                    ? $"where is the {asked} ball?"
                    : $"where is {asked}?",
                answer));

            _at++;
            _moved = (_moved + 1) % moves;
        }

        private readonly Dictionary<string, string> _where = new(StringComparer.Ordinal);
        private readonly List<string> _placed = [];
        private string _only = string.Empty;
        private string _last = string.Empty;
        private int _moved;
    }

    /// <summary>How a front end treats a word said more than once in one moment.</summary>
    /// <remarks>
    /// <para>
    /// <b>Fork 119's arms, taken here before any of them is taken anywhere</b>. <c>Joined</c> is
    /// shared by every text world, so a dial on it moves <c>Recalled</c>, <c>Roaming</c> and
    /// <c>Handing</c> at once. This is the cheap reading first.
    /// </para>
    /// <para>
    /// <b>And a precedence is a POSITIVE code</b>, which is what the last two arms are about.
    /// John's question: a word said twice is two facts, so why collapse it. The answer this grid
    /// exists to test is that keeping every mention preserves more and reaches less — with
    /// <i>mary in the kitchen</i> then <i>mary in the garden</i>, <c>the</c> precedes both rooms,
    /// and what marks the garden is that NOTHING follows it. That is a negative, and negation is
    /// rung two and unbuilt. Fork <b>30</b>.
    /// </para>
    /// </remarks>
    private enum Placing
    {
        /// <summary>Drop it, which is what `Joined` does today.</summary>
        Once,

        /// <summary>Keep the last mention, which turns *most recent* into a positive fact.</summary>
        Latest,

        /// <summary>Keep every mention, so the moment holds the whole order it was said in.</summary>
        Every,

        /// <summary>Every mention, and a marker after the last, so *newest* is positive again.</summary>
        Ended,
    }

    /// <summary>
    /// The same front end with one of <see cref="Placing"/>'s treatments of a repeated word.
    /// </summary>
    /// <remarks>
    /// <b>The precedences ride in on `Codify` and `Order` says nothing</b>, which is not a trick
    /// so much as the only way to ask the question. `IQuantizer.Order` returns ONE position a
    /// code, so a word at two positions is not expressible through that interface at all — which
    /// is itself part of the answer, since John's proposal needs the interface widened rather
    /// than the rule changed. Deriving them here asks what the codes would be worth before
    /// anything is widened for them.
    /// </remarks>
    private sealed class Placed(Joined through, Placing placing) : IQuantizer<Recited>
    {
        /// <summary>What follows the last thing said, so being last is a code and not an absence.</summary>
        private static readonly Code End = Kinds.Named(49, "end-of-moment");

        public byte Modality => through.Modality;

        public IReadOnlySet<Code>? Fleeting(Recited observation) =>
            ((IQuantizer<Recited>)through).Fleeting(observation);

        public IReadOnlySet<Code>? Forced(Recited observation) => through.Forced(observation);

        public IReadOnlyDictionary<Code, int>? Bind(Recited observation) =>
            ((IQuantizer<Recited>)through).Bind(observation);

        /// <summary>Nothing, because the precedences are already in the moment.</summary>
        public IReadOnlyDictionary<Code, int>? Order(Recited observation) => null;

        public IReadOnlyCollection<Code> Codify(Recited observation)
        {
            var said = new HashSet<Code>(through.Codify(observation));

            foreach (var code in Precedences(observation)) said.Add(code);

            return said;
        }

        private IEnumerable<Code> Precedences(Recited observation)
        {
            // `Said` is newest first, so this walks it backwards to read oldest first. One slot
            // a word, which is what makes a repeat expressible at all.
            var slots = new List<Code>();

            for (var one = observation.Said.Count - 1; one >= 0; one--)
                foreach (var word in observation.Said[one])
                    slots.Add(word);

            if (slots.Count < 2) return [];

            if (placing is Placing.Every or Placing.Ended) return Running(slots);

            var placed = new Dictionary<Code, int>();
            var twice = new HashSet<Code>();

            for (var at = 0; at < slots.Count; at++)
                if (!placed.TryAdd(slots[at], at)) twice.Add(slots[at]);

            if (placing is Placing.Once) foreach (var word in twice) placed.Remove(word);
            else foreach (var at in Enumerable.Range(0, slots.Count)) placed[slots[at]] = at;

            // The real derivation rather than a copy of it, so an arm cannot differ from the
            // default by a reimplementation nobody compared.
            return placed.Count > 1 ? Sequenced.From(placed) : [];
        }

        private IEnumerable<Code> Running(IReadOnlyList<Code> slots)
        {
            for (var at = 0; at < slots.Count - 1; at++)
                if (slots[at] != slots[at + 1])
                    yield return Sequenced.Of(slots[at], slots[at + 1]);

            if (placing is Placing.Ended) yield return Sequenced.Of(slots[^1], End);
        }
    }

    private static (Conversing World, Bench Bench, Brain Brain, Curiosity Asking, Human Typing)
        Made(double rate, int exchanges, int seed = 1, int capacity = 2000, int moves = 1,
            Placing placing = Placing.Once, bool wrapped = false,
            Telling telling = Telling.Alone)
    {
        var printed = new StringBuilder();
        var typing = new Human(printed, exchanges, seed, moves, telling);
        var brain = new Brain(new CommittingSettings { Capacity = capacity }, seed);

        var world = new Conversing(new ConversingSettings
        {
            Typed = typing,
            Printed = new StringWriter(printed),
        });

        var asking = new Curiosity(brain, rate, seed, world.Naming);

        var joined = new Joined(Joining.Bagged);

        var bench = new Bench(
            new Watching<Recited>(
                world,
                wrapped ? new Placed(joined, placing) : joined,
                acting: felt => Speaking(asking.Choose(felt))),
            brain);

        return (world, bench, brain, asking, typing);
    }

    /// <summary>The join between what a chooser decided and how this world numbers its doings.</summary>
    private static int? Speaking(Wondered said) =>
        said.Word is not { } word
            ? null
            : said.Asking ? Conversing.Asks(word) : Conversing.Asserts(word);

    [Fact]
    public void Asserting_a_word_and_asking_about_it_are_two_different_doings()
    {
        // The whole reason `Doings` is twice the vocabulary. If these collided a scope naming
        // the asking would be naming the claiming as well, and whether asking pays could never
        // be learnt because the two would be one code.
        for (var word = 0; word < 8; word++)
        {
            Assert.NotEqual(Conversing.Asserts(word), Conversing.Asks(word));
            Assert.Equal(word, Conversing.Asserts(word) / 2);
            Assert.Equal(word, Conversing.Asks(word) / 2);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => Conversing.Asserts(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Conversing.Asks(-1));
    }

    [Fact]
    public void An_outcome_code_says_which_outcome_and_a_word_code_says_none()
    {
        // The inverse has to be able to refuse, because every code in a moment is a candidate
        // for what a commitment expects. A reader that assumed would read a word of the story
        // as an outcome index and be confidently wrong.
        for (var outcome = 0; outcome < 8; outcome++)
            Assert.Equal(outcome, Brain.Meant(Brain.Says(outcome)));

        Assert.Null(Brain.Meant(Babi.Of("kitchen")));
    }


    [Fact]
    public void A_conversation_reaches_the_commitment_learner()
    {
        const int Exchanges = 400;

        var (world, bench, brain, asking, typing) = Made(rate: 1.0, Exchanges);

        var tally = bench.Run(Exchanges * 2, sweep: 200, target: 0.9, window: 50);

        output.WriteLine($"rounds     : {tally.Rounds} over {Exchanges} exchanges");
        output.WriteLine(
            $"speaking   : {asking.Claims} claims, {asking.Questions} questions of which "
            + $"{asking.Blind} blind, {asking.Silences} with nothing to say");
        output.WriteLine(
            $"asking     : {world.Asked} asked, {world.Told} answered, {world.Shrugged} "
            + $"shrugged at, {world.Declined} declined, {world.Quiet} let go by");
        output.WriteLine(
            $"the human  : {typing.Confirmed} confirmed, {typing.Corrected} corrected, "
            + $"{typing.Shrugged} shrugged");
        output.WriteLine(
            $"answerable : {world.Confirmed} of {world.Told} guesses confirmed, "
            + $"{(world.Told == 0 ? 0.0 : world.Confirmed / (double)world.Told):F3}, against "
            + $"{asking.Blind} blind asks");
        output.WriteLine(
            $"settling   : {tally.Right} right, {tally.Wrong} wrong, {tally.Silent} nothing "
            + $"fired, {tally.Abstained} settled nothing");
        output.WriteLine(
            $"population : {tally.Resident} resident, {tally.Minted} minted, {tally.Repaired} "
            + $"repaired, {brain.Held.Count} held");
        output.WriteLine(
            $"vocabulary : {world.Vocabulary.Count} — {string.Join(" ", world.Vocabulary)}");
        output.WriteLine($"wanting    : {tally.Wanting:F3} of {tally.Blamed} blamed");

        // Every line the human typed became a moment, which is the first of the three pieces
        // the plan puts on the path. Two an exchange: the statement and the question.
        Assert.Equal(Exchanges * 2, tally.Rounds);

        // Every settlement came from an ask, and nothing else in this world can produce one.
        // The three-way split is the point: a reply the population had no expectation for is
        // scored as silence rather than as a miss, so a sum of right and wrong alone would
        // read as settlements going missing.
        Assert.Equal(world.Told + world.Shrugged, tally.Right + tally.Wrong + tally.Silent);
        Assert.True(world.Told > 0, "the machine never obtained a single settlement");

        // And a shrug is one of them, which is what makes asking learnable at all. An ask that
        // got nothing back used to settle nothing, so a wasted question and a silence scored
        // alike and no scope could come to expect that asking here pays.
        Assert.True(world.Shrugged > 0,
            "no ask came back empty, so nothing ever told the machine it asked in the wrong "
            + "place");

        // A round settles nothing exactly where the machine did not ask, which is what a shrug
        // being an outcome changed. Before it, every statement was an unsettleable round and
        // half the run could not be learnt from whatever the machine did.
        Assert.Equal(tally.Rounds - (world.Told + world.Shrugged), tally.Abstained);
    }

    [Fact]
    public void How_often_a_machine_asks_barely_changes_what_an_ask_is_worth()
    {
        const int Exchanges = 400;
        const int Seeds = 8;

        // Every rate down from the ceiling, which is what asking about everything is. The two
        // arms that read the vote to pick their moments are deleted and their row is in the
        // plan's table -- both lost to a coin here, by ten times and by fifty per ask.
        double[] rates = [1.0, 0.5, 0.25, 0.1];

        output.WriteLine($"{Seeds} seeds, {Exchanges} exchanges each. mean (spread over seeds)");
        output.WriteLine(
            $"{"rate",-6}{"asked",14}{"told",14}{"shrugged",14}{"per ask",9}"
            + $"{"skips stmt",12}{"skips q",9}{"right",8}{"blind",8}{"resident",10}");

        var worth = new List<double>();
        var learnt = new List<(double Rate, double Statements, double Questions)>();

        foreach (var rate in rates)
        {
            var asked = new List<double>();
            var told = new List<double>();
            var right = new List<double>();
            var resident = new List<double>();
            var shrugged = new List<double>();
            var duck = new List<double>();
            var miss = new List<double>();
            var sure = new List<double>();
            var blind = new List<double>();

            for (var seed = 1; seed <= Seeds; seed++)
            {
                var made = Made(rate, Exchanges, seed);
                var tally = made.Bench.Run(Exchanges * 2, sweep: 200, target: 0.9, window: 50);

                asked.Add(made.World.Asked);
                told.Add(made.World.Told);
                right.Add(tally.Right);
                resident.Add(tally.Resident);
                shrugged.Add(made.World.Shrugged);

                // Every question moment it asked about was answered and every statement moment
                // it asked about was shrugged at, so what it declined is what is left of each.
                // No counter is needed for it and one would be a second way to be wrong.
                duck.Add((Exchanges - made.World.Shrugged) / (double)Exchanges);
                miss.Add((Exchanges - made.World.Told) / (double)Exchanges);

                // Accuracy on the rounds a reply could settle, which the trailing one cannot
                // say: it counts every round where the machine correctly expected that nobody
                // knew, and half of these moments are ones nobody can answer.
                sure.Add(made.World.Told == 0 ? 0.0 : made.World.Confirmed / (double)made.World.Told);
                blind.Add(made.Asking.Blind);
            }

            var perAsk = Average(asked) == 0 ? 0.0 : Average(told) / Average(asked);

            worth.Add(perAsk);
            learnt.Add((rate, Average(duck), Average(miss)));

            output.WriteLine(
                $"{rate,-6:F2}{Spread(asked),14}{Spread(told),14}{Spread(shrugged),14}"
                + $"{perAsk,9:F3}{Average(duck),12:F3}{Average(miss),9:F3}{Average(sure),8:F3}"
                + $"{Average(blind),8:F0}{Average(resident),10:F1}");
        }

        // The two skip columns are what a shrug being an outcome bought, and the ceiling is
        // where they can be read. At a rate of one the coin never declines anything, so every
        // skip is the population's own -- it expected that nobody would know and said nothing.
        // Below the ceiling the coin does most of the skipping and swamps the gap, which is
        // why only this row is asserted on.
        var (_, statements, questions) = learnt[0];

        Assert.True(statements > questions,
            $"at the ceiling the machine skipped {statements:F3} of the statements and "
            + $"{questions:F3} of the questions. A machine that had learnt nothing about where "
            + "a reply CAN settle would skip both alike, so these being level means the shrug "
            + "stopped being an outcome or stopped reaching the population");

        // Asking less still costs asks rather than their worth, which is the reading the rates
        // were run for before any of this.
        Assert.All(worth, one => Assert.InRange(one, 0.3, 0.9));

        static double Average(IReadOnlyCollection<double> of) => of.Average();
    }

    [Fact]
    public void What_a_front_end_does_with_a_word_said_twice_decides_whether_a_thing_can_be_tracked()
    {
        const int Exchanges = 400;
        const int Seeds = 8;
        const int Moves = 8;

        var placings = new[]
        {
            (Placing.Once, false), (Placing.Latest, true), (Placing.Every, true),
        };

        output.WriteLine(
            $"{Seeds} seeds, {Exchanges} exchanges each, asking at the ceiling, topics of "
            + $"{Moves}");
        output.WriteLine(
            $"{"world",-9}{"placing",-9}{"recency",9}{"right",16}{"vs base",16}{"codes",8}"
            + $"{"resident",10}");

        var right = new Dictionary<(Telling Telling, Placing Placing), double>();
        var over = new Dictionary<(Telling Telling, Placing Placing), (double Gap, double Error)>();
        var recency = new Dictionary<Telling, double>();

        foreach (var telling in new[] { Telling.Alone, Telling.Crowded, Telling.Props })
        {
            foreach (var (placing, wrapped) in placings)
            {
                var sure = new List<double>();
                var fresh = new List<double>();
                var codes = new List<double>();
                var resident = new List<double>();
                var wanting = new List<double>();

                for (var seed = 1; seed <= Seeds; seed++)
                {
                    var made = Made(
                        rate: 1.0, Exchanges, seed, moves: Moves, placing: placing,
                        wrapped: wrapped, telling: telling);

                    var tally = made.Bench.Run(
                        Exchanges * 2, sweep: 200, target: 0.9, window: 50);

                    sure.Add(made.World.Told == 0
                        ? 0.0
                        : made.World.Confirmed / (double)made.World.Told);
                    fresh.Add(made.Typing.Questions == 0
                        ? 0.0
                        : made.Typing.Recency / (double)made.Typing.Questions);
                    codes.Add(tally.Codes);
                    resident.Add(tally.Resident);
                    wanting.Add(tally.Wanting);
                }

                right[(telling, placing)] = sure.Average();
                recency[telling] = fresh.Average();

                // The gap a seed at a time rather than the difference of two means, because a
                // spread on the difference is what says whether an ordering is real and a
                // spread on each half separately does not.
                var gaps = sure.Zip(fresh, (one, bar) => one - bar).ToList();

                over[(telling, placing)] = (gaps.Average(), Deviation(gaps));

                output.WriteLine(
                    $"{telling,-9}{placing,-9}{fresh.Average(),9:F3}{Error(sure),16}"
                    + $"{Error(gaps),16}{codes.Average(),8:F1}{resident.Average(),10:F1}");
            }
        }

        // The bars have to be far apart or nothing below separates anything. Alone, the freshest
        // room is always the answer; on John's world the question names WHICH ball, so the room
        // said last belongs to another one.
        Assert.True(recency[Telling.Alone] > 0.95,
            $"alone: the freshest room answers {recency[Telling.Alone]:F3} of the questions, so "
            + "this world stopped being the degenerate case the others are read against");

        Assert.True(recency[Telling.Props] < 0.45,
            $"props: the freshest room still answers {recency[Telling.Props]:F3}, so the "
            + "shortcut was not broken by naming which ball");

        // `Latest` still beats `Once` where a thing moves alone, which is the earlier reading.
        Assert.True(right[(Telling.Alone, Placing.Latest)] > right[(Telling.Alone, Placing.Once)],
            $"keeping the last mention scored {right[(Telling.Alone, Placing.Latest)]:F3} "
            + $"against {right[(Telling.Alone, Placing.Once)]:F3} for dropping it");

        // Where recency IS the answer, no arm beats it. A front end can recover the shortcut and
        // it cannot get past it, which is what the first two worlds are for.
        foreach (var telling in new[] { Telling.Alone, Telling.Crowded })
            foreach (var (placing, _) in placings)
                Assert.True(over[(telling, placing)].Gap < 0,
                    $"{placing} on {telling} cleared the recency bar by "
                    + $"{over[(telling, placing)].Gap:F3}. Read the row and take the finding");

        // And where it is NOT, every arm beats it by several standard errors. That is the first
        // evidence on this branch of anything using more than how recently a word was said: on
        // John's world the question names WHICH ball, so the freshest room belongs to another
        // one and a machine reading recency alone cannot get here.
        foreach (var (placing, _) in placings)
        {
            var (gap, error) = over[(Telling.Props, placing)];

            Assert.True(gap - (3.0 * error) > 0.0,
                $"{placing} on John's world cleared the recency bar by {gap:F3} ±{error:F3}, "
                + "which is inside three standard errors. The one thing here that is not a "
                + "recency proxy has stopped working");
        }

        // What is NOT asserted is an ordering among those three. They sit within about one
        // standard error of each other, so which repeat the front end keeps is not what is
        // doing the work -- and a grid that ranked them here would be ranking noise.
    }

    [Fact]
    public void Rung_three_goes_blind_on_exactly_the_word_a_moving_thing_repeats()
    {
        // The mechanism under the column above, asserted rather than argued. `Joined.Order`
        // places a word by which statement it was in and then DROPS every word it placed
        // twice, because a word in two statements has no one position -- which is right, and
        // which means a precedence exists only over words said once in the moment.
        //
        // A thing that moves repeats its own name in every statement and repeats a room the
        // moment it goes back to one. So the words order can separate are the rooms visited
        // once, and the word it cannot is the room visited twice: order is blind on exactly
        // the case it was wanted for.
        var front = new Joined(Joining.Bagged);

        var visited = new[] { "garden", "office", "kitchen", "kitchen" };

        // Newest first, which is what `Recited` promises.
        var said = visited
            .Reverse()
            .Select(where => (IReadOnlyList<Code>)
                [.. new[] { "mary", "is", "in", "the", where }.Select(Babi.Of)])
            .ToList();

        var placed = front.Order(new Recited
        {
            Said = said,
            Asked = [.. new[] { "where", "is", "mary" }.Select(Babi.Of)],
        });

        Assert.NotNull(placed);

        // Said once, so each has a position and a precedence can say which came after which.
        Assert.Contains(Babi.Of("garden"), placed);
        Assert.Contains(Babi.Of("office"), placed);

        // Said twice, so it has none -- and it is the room mary is actually in.
        Assert.DoesNotContain(Babi.Of("kitchen"), placed);

        // As are the words every statement carries, which is the same rule doing the same
        // thing and is why a bag of function words costs nothing here.
        foreach (var word in new[] { "mary", "is", "in", "the" })
            Assert.DoesNotContain(Babi.Of(word), placed);

        output.WriteLine(
            $"{visited.Length} statements, {placed.Count} words placed: "
            + string.Join(" ", placed.OrderBy(one => one.Value).Select(one => one.Key)));
    }

    /// <summary>A mean over seeds, with the standard error of it.</summary>
    /// <remarks>
    /// <b>One place, because two grids read the same column</b>. A second copy is a second
    /// thing to get wrong, and `DuplicationTests` is what said so.
    /// </remarks>
    private static string Spread(IReadOnlyCollection<double> of)
    {
        var mean = of.Average();
        var error = of.Count < 2
            ? 0.0
            : Math.Sqrt(of.Sum(one => (one - mean) * (one - mean)) / (of.Count - 1))
                / Math.Sqrt(of.Count);

        return $"{mean,8:F1} ±{error,-4:F1}";
    }

    /// <summary>The standard error of a mean.</summary>
    private static double Deviation(IReadOnlyCollection<double> of)
    {
        if (of.Count < 2) return 0.0;

        var mean = of.Average();

        return Math.Sqrt(of.Sum(one => (one - mean) * (one - mean)) / (of.Count - 1))
            / Math.Sqrt(of.Count);
    }

    /// <summary>A mean and its standard error, at three figures.</summary>
    /// <remarks>
    /// <b>Separate from <c>Spread</c> because a count and a rate want different widths</b>, and a
    /// rate printed to one decimal says nothing at all.
    /// </remarks>
    private static string Error(IReadOnlyCollection<double> of)
    {
        return $"{of.Average(),7:F3} ±{Deviation(of),-6:F3}";
    }

    /// <summary>The mean alone, where the spread is printed in another column.</summary>
    private static double Average(IReadOnlyCollection<double> of) => of.Average();
}
