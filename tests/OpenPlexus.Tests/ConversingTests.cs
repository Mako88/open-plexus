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
    private sealed class Human(StringBuilder printed, int exchanges, int seed, int moves = 1)
        : TextReader
    {
        private readonly Random _draws = new(seed);

        private string? _answer;
        private string _asking = string.Empty;
        private int _step;
        private int _at;

        /// <summary>How many times it was asked something nobody could answer.</summary>
        public int Shrugged { get; private set; }

        /// <summary>How many times it confirmed a guess.</summary>
        public int Confirmed { get; private set; }

        /// <summary>How many times it corrected one.</summary>
        public int Corrected { get; private set; }

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
            if (_at >= exchanges) return null;

            switch (_step++)
            {
                case 0:
                    _answer = null;

                    // A topic runs for `moves` exchanges and then starts over. At one that is
                    // the world's own boundary every time and nothing is ever in two rooms; at
                    // more, the same person moves and the bag holds every room they have been
                    // in -- which is the one thing only order can separate.
                    if (_moved > 0) { _step = 2; return Moving(); }

                    _asking = Cast[_draws.Next(Cast.Length)];

                    return string.Empty;

                case 1:
                    return Moving();

                default:
                    _step = 0;
                    _at++;
                    _moved = (_moved + 1) % moves;
                    _answer = _known;

                    return $"where is {_asking}?";
            }
        }

        /// <summary>Putting the person somewhere, which may be somewhere they have been.</summary>
        private string Moving()
        {
            _known = Places[_draws.Next(Places.Length)];
            _answer = null;

            return $"{_asking} is in the {_known}";
        }

        private string _known = string.Empty;
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
            Placing placing = Placing.Once, bool wrapped = false)
    {
        var printed = new StringBuilder();
        var typing = new Human(printed, exchanges, seed, moves);
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

        // How many times the same person moves before the topic starts over. At one the world
        // draws a boundary every exchange and nobody is ever in two rooms; above it the bag
        // holds every room they have been in, which is the plan's own example and the one thing
        // a scope cannot say -- a scope is a SET, so what is left to separate the rooms is the
        // precedences rung three derives at the join.
        int[] moves = [1, 4, 8];

        var placings = new[]
        {
            (Placing.Once, false), (Placing.Once, true), (Placing.Latest, true),
            (Placing.Every, true), (Placing.Ended, true),
        };

        output.WriteLine($"{Seeds} seeds, {Exchanges} exchanges each, asking at the ceiling");
        output.WriteLine(
            $"{"moves",-7}{"placing",-16}{"told",14}{"right",8}{"codes",8}{"resident",10}"
            + $"{"minted",9}{"wanting",9}");

        var right = new Dictionary<(int Moves, Placing Placing, bool Wrapped), double>();
        var held = new Dictionary<(int Moves, Placing Placing, bool Wrapped), double>();

        foreach (var move in moves)
        {
            foreach (var (placing, wrapped) in placings)
            {
                var sure = new List<double>();
                var told = new List<double>();
                var codes = new List<double>();
                var resident = new List<double>();
                var minted = new List<double>();
                var wanting = new List<double>();

                for (var seed = 1; seed <= Seeds; seed++)
                {
                    var made = Made(
                        rate: 1.0, Exchanges, seed, moves: move, placing: placing,
                        wrapped: wrapped);

                    var tally = made.Bench.Run(
                        Exchanges * 2, sweep: 200, target: 0.9, window: 50);

                    told.Add(made.World.Told);
                    sure.Add(made.World.Told == 0
                        ? 0.0
                        : made.World.Confirmed / (double)made.World.Told);
                    codes.Add(tally.Codes);
                    resident.Add(tally.Resident);
                    minted.Add(tally.Minted);
                    wanting.Add(tally.Wanting);
                }

                right[(move, placing, wrapped)] = sure.Average();
                held[(move, placing, wrapped)] = resident.Average();

                output.WriteLine(
                    $"{move,-7}{$"{placing}{(wrapped ? "" : " (real)")}",-16}{Spread(told),14}"
                    + $"{sure.Average(),8:F3}{codes.Average(),8:F1}{resident.Average(),10:F1}"
                    + $"{minted.Average(),9:F1}{wanting.Average(),9:F3}");
            }
        }

        // The control that makes every other row readable. `Once` through the wrapper derives
        // the precedences by hand and `Once` through `Joined` derives them the way the library
        // does, so the two must agree exactly -- otherwise the arms below differ from the
        // default by a reimplementation nobody compared rather than by the rule being changed.
        foreach (var move in moves)
        {
            Assert.Equal(right[(move, Placing.Once, false)], right[(move, Placing.Once, true)], 6);
            Assert.Equal(held[(move, Placing.Once, false)], held[(move, Placing.Once, true)], 6);
        }

        // And where nothing is ever said twice, no treatment of a repeat can differ from any
        // other. Any gap at one move is a second change nobody declared. `Ended` is out of it
        // and has to be: its marker follows the last word whether or not anything repeated, so
        // it is not a treatment of a repeat and does not claim to be.
        foreach (var placing in new[] { Placing.Once, Placing.Latest, Placing.Every })
            Assert.Equal(right[(1, Placing.Once, false)], right[(1, placing, true)], 6);

        // Dropping a repeat collapses on a thing that moves. The margin is far inside the
        // measured gap, which is what stops it being a threshold written before the first run.
        Assert.True(right[(8, Placing.Once, false)] < 0.6,
            $"dropping a repeat scored {right[(8, Placing.Once, false)]:F3} over eight moves, "
            + "so the thing these arms exist to fix has stopped happening");

        Assert.True(right[(8, Placing.Latest, true)] > right[(8, Placing.Once, false)] + 0.2,
            $"keeping the last mention scored {right[(8, Placing.Latest, true)]:F3} against "
            + $"{right[(8, Placing.Once, false)]:F3} — the arm has stopped paying");

        // John's question, and the answer is no. Keeping EVERY mention preserves strictly more
        // of what was said and reads WORSE than dropping the repeat entirely, so the collapse
        // is not what was costing anything. More order codes give repair more to grab and what
        // it grabs is coincidence -- `wanting` goes to nought while the accuracy falls, which
        // is the ladder always finding a separating code and the code being noise.
        Assert.True(right[(8, Placing.Every, true)] < right[(8, Placing.Once, false)],
            $"keeping every mention scored {right[(8, Placing.Every, true)]:F3} against "
            + $"{right[(8, Placing.Once, false)]:F3} for dropping it — it has stopped being "
            + "worse, which is the finding this row carries");

        // And the marker meant to rescue it bought nothing, which is the other refutation. If
        // what `Every` lacked were a positive code for *nothing follows this*, this row would
        // move. It does not, so being unmarked was never the problem.
        Assert.True(Math.Abs(right[(8, Placing.Ended, true)] - right[(8, Placing.Every, true)]) < 0.05,
            $"the end marker moved the score from {right[(8, Placing.Every, true)]:F3} to "
            + $"{right[(8, Placing.Ended, true)]:F3}, so it has started paying");
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

    /// <summary>The mean alone, where the spread is printed in another column.</summary>
    private static double Average(IReadOnlyCollection<double> of) => of.Average();
}
