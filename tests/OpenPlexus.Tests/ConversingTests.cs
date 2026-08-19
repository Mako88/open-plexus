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
    private sealed class Human(StringBuilder printed, int exchanges, int seed) : TextReader
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

                    return string.Empty;

                case 1:
                    var who = Cast[_draws.Next(Cast.Length)];
                    var where = Places[_draws.Next(Places.Length)];

                    _asking = who;
                    _answer = null;

                    // Where the answer is remembered, so the question below can be answered
                    // without parsing back what this line said.
                    _known = where;

                    return $"{who} is in the {where}";

                default:
                    _step = 0;
                    _at++;
                    _answer = _known;

                    return $"where is {_asking}?";
            }
        }

        private string _known = string.Empty;
    }

    private static (Conversing World, Bench Bench, Brain Brain, Curiosity Asking, Human Typing)
        Made(Curious wondering, int exchanges, double bar = 0.5, int seed = 1, int capacity = 2000)
    {
        var printed = new StringBuilder();
        var typing = new Human(printed, exchanges, seed);
        var brain = new Brain(new CommittingSettings { Capacity = capacity }, seed);

        var world = new Conversing(new ConversingSettings
        {
            Typed = typing,
            Printed = new StringWriter(printed),
        });

        var asking = new Curiosity(brain, wondering, bar, seed, world.Naming);

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

        var (world, bench, brain, asking, typing) = Made(Curious.Untested, Exchanges, bar: 1.5);

        var tally = bench.Run(Exchanges * 2, sweep: 200, target: 0.9, window: 50);

        output.WriteLine($"rounds     : {tally.Rounds} over {Exchanges} exchanges");
        output.WriteLine(
            $"speaking   : {asking.Claims} claims, {asking.Questions} questions of which "
            + $"{asking.Blind} blind, {asking.Silences} with nothing to say");
        output.WriteLine(
            $"asking     : {world.Asked} asked, {world.Told} answered, {world.Quiet} let go by");
        output.WriteLine(
            $"the human  : {typing.Confirmed} confirmed, {typing.Corrected} corrected, "
            + $"{typing.Shrugged} shrugged");
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
        Assert.Equal(world.Told, tally.Right + tally.Wrong + tally.Silent);
        Assert.True(world.Told > 0, "the machine never obtained a single settlement");

        // At least half of the rounds could never settle, and that is the shape of the world
        // rather than a failure of it. Every statement is a moment nobody has an answer to, so
        // asking about one buys a shrug -- and this arm asks about everything, which is why the
        // number lands exactly on the statements rather than above them.
        Assert.True(tally.Abstained >= Exchanges,
            $"{tally.Abstained} of {tally.Rounds} rounds settled nothing, against {Exchanges} "
            + "statements that no reply could ever settle");
    }

    [Fact]
    public void What_a_machine_asks_about_decides_what_it_can_learn()
    {
        const int Exchanges = 400;
        const int Seeds = 8;

        // The bars are not comparable across arms and are not meant to be. `Unsure` and `Coin`
        // read a fraction, `Untested` reads a weight, and `Always` reads nothing at all -- so
        // one number here would be four different questions sharing a name.
        var grid = new (Curious Wondering, double Bar)[]
        {
            (Curious.Always, 0.0),
            (Curious.Unsure, 0.5),
            (Curious.Untested, 0.9),
            (Curious.Coin, 0.5),
            (Curious.Coin, 0.25),
        };

        output.WriteLine($"{Seeds} seeds, {Exchanges} exchanges each. mean (spread over seeds)");
        output.WriteLine(
            $"{"arm",-14}{"asked",14}{"told",14}{"right",14}{"per ask",9}{"resident",10}");

        var ceiling = 0.0;

        foreach (var (wondering, bar) in grid)
        {
            var asked = new List<double>();
            var told = new List<double>();
            var right = new List<double>();
            var resident = new List<double>();

            for (var seed = 1; seed <= Seeds; seed++)
            {
                var made = Made(wondering, Exchanges, bar, seed);
                var tally = made.Bench.Run(Exchanges * 2, sweep: 200, target: 0.9, window: 50);

                asked.Add(made.World.Asked);
                told.Add(made.World.Told);
                right.Add(tally.Right);
                resident.Add(tally.Resident);
            }

            var named = $"{wondering} {bar:F2}";

            if (wondering is Curious.Always) ceiling = Mean(asked);

            output.WriteLine(
                $"{named,-14}{Spread(asked),14}{Spread(told),14}{Spread(right),14}"
                + $"{(Mean(asked) == 0 ? 0.0 : Mean(told) / Mean(asked)),9:F3}"
                + $"{Mean(resident),10:F1}");

            // The ceiling asks about every moment it has a word for, so nothing can ask more
            // often. That is the only ordering asserted: which arm is WORTH its asks wants the
            // spread read rather than a threshold written before the first run.
            Assert.True(Mean(asked) <= ceiling,
                $"{named} asked {Mean(asked):F1} times against a ceiling of {ceiling:F1} — the "
                + "ceiling is not the ceiling");
        }

        static double Mean(IReadOnlyCollection<double> of) => of.Average();

        static string Spread(IReadOnlyCollection<double> of)
        {
            var mean = of.Average();
            var error = of.Count < 2
                ? 0.0
                : Math.Sqrt(of.Sum(one => (one - mean) * (one - mean)) / (of.Count - 1))
                    / Math.Sqrt(of.Count);

            return $"{mean,8:F1} ±{error,-4:F1}";
        }
    }
}
