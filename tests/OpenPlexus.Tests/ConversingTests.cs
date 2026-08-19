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

    private static (Conversing World, Bench Bench, Brain Brain, Curiosity Asking, Human Typing)
        Made(double rate, int exchanges, int seed = 1, int capacity = 2000, int moves = 1)
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

        var bench = new Bench(
            new Watching<Recited>(
                world,
                new Joined(Joining.Bagged),
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
    public void A_thing_that_moves_puts_two_rooms_in_the_bag_and_only_order_separates_them()
    {
        const int Exchanges = 400;
        const int Seeds = 8;

        // How many times the same person moves before the topic starts over. At one the world
        // draws a boundary every exchange and nobody is ever in two rooms; above it the bag
        // holds every room they have been in, which is the plan's own example and the one thing
        // a scope cannot say -- a scope is a SET, so what is left to separate the rooms is the
        // precedences rung three derives at the join.
        int[] moves = [1, 2, 4, 8];

        output.WriteLine($"{Seeds} seeds, {Exchanges} exchanges each, asking at the ceiling");
        output.WriteLine(
            $"{"moves",-7}{"told",14}{"right",8}{"blind",8}{"resident",10}{"minted",9}"
            + $"{"wanting",9}");

        var right = new List<double>();

        foreach (var move in moves)
        {
            var sure = new List<double>();
            var told = new List<double>();
            var blind = new List<double>();
            var resident = new List<double>();
            var minted = new List<double>();
            var wanting = new List<double>();

            for (var seed = 1; seed <= Seeds; seed++)
            {
                var made = Made(rate: 1.0, Exchanges, seed, moves: move);
                var tally = made.Bench.Run(Exchanges * 2, sweep: 200, target: 0.9, window: 50);

                told.Add(made.World.Told);
                sure.Add(made.World.Told == 0
                    ? 0.0
                    : made.World.Confirmed / (double)made.World.Told);
                blind.Add(made.Asking.Blind);
                resident.Add(tally.Resident);
                minted.Add(tally.Minted);
                wanting.Add(tally.Wanting);
            }

            right.Add(sure.Average());

            output.WriteLine(
                $"{move,-7}{Spread(told),14}{sure.Average(),8:F3}{blind.Average(),8:F0}"
                + $"{resident.Average(),10:F1}{minted.Average(),9:F1}{wanting.Average(),9:F3}");
        }

        // Every arm answers something, which is the only thing asserted. What a move costs is
        // read off the column rather than pinned to a threshold nobody has measured yet, and a
        // prediction written into a wiring check fails two ways and reads the same.
        Assert.All(right, one => Assert.InRange(one, 0.0, 1.0));
        Assert.True(right[0] > 0.5,
            $"the standing case scored {right[0]:F3}, so the comparison has no floor under it");
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
