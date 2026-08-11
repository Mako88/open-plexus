using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>Which text, how much of it is visible at once, and how much is held back.</summary>
public sealed record RecalledSettings
{
    /// <summary>The bAbI task directory, the same one <see cref="BabiSettings.Corpus"/> names.</summary>
    public required string Corpus { get; init; }

    /// <summary>Which of the twenty tasks.</summary>
    public required int Task { get; init; }

    /// <summary>
    /// How many statements before the question are in the moment — <b>nought for every
    /// statement of the story so far.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THIS IS THE ONE DIAL WORTH HAVING AND IT IS AN ARM RATHER THAN A SETTING.</b> A
    /// scope is a set of co-present codes, so a moment holding a whole story is a bag of
    /// words and <i>Mary went to the kitchen, Mary went to the garden, where is Mary</i>
    /// puts both places in it with nothing to tell them apart. A bounded span is the
    /// crudest possible way to hand the bag some recency, and the gap between the two
    /// readings is what sequence is worth on this text before rung three exists to
    /// measure it properly.
    /// </para>
    /// <para>
    /// <b>AND IT IS A FACT ABOUT WHAT WAS SHOWN RATHER THAN ABOUT HOW TO THINK</b>, which
    /// is the line a world's own dials have to stay on. How much of a page a reader is
    /// allowed to see is the experiment's business; what a reader does with it is not, and
    /// nothing here names a brain type.
    /// </para>
    /// </remarks>
    public int Span { get; init; }

    /// <summary>Questions from the end of the file that this world will never draw.</summary>
    public int Withheld { get; init; }
}

/// <summary>
/// What one withheld question said, in the English the corpus wrote it in.
/// </summary>
/// <remarks>
/// <b>THE TRANSCRIPT AND NOT THE MOMENT, AND NOTHING THAT LEARNS EVER SEES IT.</b> A word
/// reaches the population as a hash and a hash goes nowhere back, so a run could report
/// that it answered four questions in five and never say which — which is a score with no
/// way to be embarrassed by it. This is here so an answer can be printed in words beside
/// the one the corpus expected.
/// </remarks>
public sealed record Quizzed
{
    /// <summary>The statements that were in the moment, in the order written.</summary>
    public required string Story { get; init; }

    /// <summary>The question itself.</summary>
    public required string Question { get; init; }

    /// <summary>What the corpus says the answer is.</summary>
    public required string Answer { get; init; }
}

/// <summary>
/// A story in English and a question about it, put to a learner whose scope is a SET.
/// </summary>
/// <remarks>
/// <para>
/// <b>NO RUN HAS EVER PUT TEXT THROUGH THIS LEARNER, WHICH IS WHY THIS IS SMALL.</b>
/// <see cref="Babi"/> has parsed the corpus into codes since before the commitment
/// primitive existed, and everything built on it was walk-shaped — an occasion writing
/// edges into clusters. Nothing joined the same words to a population of commitments,
/// so <i>can this learn from text at all</i> has been an argument rather than a number.
/// </para>
/// <para>
/// <b>A MOMENT IS WORDS AND AN OUTCOME IS AN INDEX, WHICH IS WHAT KEEPS ONE BRAIN ONE
/// BRAIN.</b> The answer is a position in this task's answer alphabet rather than a word
/// code, so <see cref="Machines.Brain.Says"/> maps it exactly as it maps a multiplexer
/// bit — and a compound answer is one string and therefore one outcome, which is how the
/// two structurally unanswerable tasks stop being a special case here.
/// </para>
/// <para>
/// <b>THE ANSWER WORD IS IN THE MOMENT AND THAT IS THE POINT RATHER THAN A LEAK.</b>
/// <i>kitchen</i> is a word of the story and <i>the answer is kitchen</i> is an outcome,
/// and the one-code commitment joining them is exactly what genesis is for. What the
/// text does NOT hand over is which of the two places in the bag is the current one, and
/// that is the whole of the question this world asks.
/// </para>
/// <para>
/// <b>AND WHAT IT MEASURES WHEN IT FAILS IS WORTH MORE THAN WHAT IT MEASURES WHEN IT
/// PASSES.</b> <c>Tally.Wanting</c> is the share of blamed rounds nothing in the scope
/// language separates, computed every round since the branch began. A high reading here
/// is the ladder's own admission rule firing on text, which is the signal the design
/// exists for — and it is a different finding from a low score.
/// </para>
/// </remarks>
public sealed class Recalled : IWorld<Coded>, IWithholds<Coded>
{
    private readonly List<Turn<Coded>> _asked;
    private readonly ImmutableArray<Turn<Coded>> _kept;
    private int _at;

    /// <param name="settings">Which text, how much is visible, and how much is held back.</param>
    /// <exception cref="FileNotFoundException">The corpus is not there.</exception>
    public Recalled(RecalledSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Span);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Withheld);

        var text = new Babi(new BabiSettings
        {
            Task = settings.Task,
            Corpus = settings.Corpus,

            // OFF, BECAUSE A CODE NAMING ONE STORY IS PRESENT ONCE AND NEVER AGAIN. Genesis
            // roots on a code that was live when the outcome started, so a per-story code
            // mints a commitment that can never fire a second time -- one resident a story,
            // for a population that is bounded.
            Stories = false,
        });

        // ORDINAL AND SORTED, BECAUSE THE INDEX IS THE OUTCOME. A hash set's order is not
        // promised across runs, and an answer alphabet that renumbered itself would make
        // two runs of the same seed disagree about what the machine said -- which is fork
        // 12's property broken by a detail nothing else would ever look at.
        Answers = [.. text.Alphabet.Order(StringComparer.Ordinal)];

        var index = Answers
            .Select((answer, at) => (answer, at))
            .ToDictionary(one => one.answer, one => one.at, StringComparer.Ordinal);

        var told = new List<Turn<Coded>>();
        var wrote = new List<Quizzed>();
        var story = -1;
        var said = new List<ImmutableArray<Code>>();
        var wording = new List<string>();

        foreach (var line in text.Lines)
        {
            // A NEW STORY FORGETS THE LAST ONE, which is the corpus's own boundary and not
            // an episode the learner can see. Nothing about the machine changes here; what
            // changes is which words the world says are in front of it.
            if (line.Story != story)
            {
                story = line.Story;
                said.Clear();
                wording.Clear();
            }

            if (!line.Asking)
            {
                said.Add(line.Words);
                wording.Add(line.Text ?? string.Empty);
                continue;
            }

            var seen = new HashSet<Code>(line.Words);

            var from = settings.Span == 0 ? 0 : Math.Max(0, said.Count - settings.Span);
            for (var one = from; one < said.Count; one++) seen.UnionWith(said[one]);

            told.Add(new Turn<Coded>
            {
                Seen = Coded.Of(seen),
                Outcome = index[line.Answer!],
            });

            // THE SAME SLICE THE MOMENT WAS BUILT FROM, so a transcript can never show a
            // statement the population was not given. A readback taken from the whole story
            // while the moment held one line would read as a machine that ignored what it
            // was told, which is a bug in the printing wearing a finding's clothes.
            wrote.Add(new Quizzed
            {
                Story = string.Join(" ", wording.Skip(from)),
                Question = line.Text ?? string.Empty,
                Answer = line.Answer!,
            });
        }

        var back = Math.Min(settings.Withheld, told.Count);

        _kept = [.. told.Skip(told.Count - back)];
        _asked = [.. told.Take(told.Count - back)];
        Transcript = [.. wrote.Skip(wrote.Count - back)];

        if (_asked.Count == 0)
            throw new ArgumentException(
                $"task {settings.Task} has {told.Count} questions and {back} are held back, "
                + "so there is nothing left to learn from", nameof(settings));
    }

    /// <summary>Every distinct answer the task expects, in the order the outcome index uses.</summary>
    public ImmutableArray<string> Answers { get; }

    /// <summary>
    /// What each withheld question said, in the same order <see cref="Withheld"/> holds them.
    /// </summary>
    /// <remarks>
    /// <b>ONE ROW A WITHHELD TURN, WHICH IS WHAT LETS A SCORE BE READ AS A CONVERSATION.</b>
    /// Nothing that learns is given any of it — see <see cref="Quizzed"/>.
    /// </remarks>
    public ImmutableArray<Quizzed> Transcript { get; }

    /// <summary>How many questions this world will draw from.</summary>
    public int Questions => _asked.Count;

    /// <inheritdoc/>
    public int Outcomes => Answers.Length;

    /// <summary>
    /// What always answering the commonest answer would score.
    /// </summary>
    /// <remarks>
    /// <b>THE BAR THAT MATTERS, AND IT IS WELL ABOVE A BLIND DRAW.</b> bAbI answers are
    /// skewed, so a population that has learnt nothing but the marginal already clears
    /// <c>1/Outcomes</c> — and on a task with six answers that is the difference between a
    /// result and a decoration.
    /// </remarks>
    public double Commonest => _asked.Count == 0 ? 0.0 : _asked
        .GroupBy(one => one.Outcome!.Value)
        .Max(group => group.Count()) / (double)_asked.Count;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>IN THE ORDER WRITTEN AND WRAPPING, so the stream is the file read over and
    /// over.</b> Drawing at random would be a different world — the corpus puts a
    /// question after the statements that answer it, and reordering that is the one
    /// property a story has.
    /// </remarks>
    public Turn<Coded> Next()
    {
        var turn = _asked[_at];
        _at = (_at + 1) % _asked.Count;
        return turn;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>THE LAST QUESTIONS OF THE FILE, WHICH ARE WHOLE STORIES THIS WORLD NEVER
    /// TELLS.</b> A question drawn from the middle would leave its own statements in the
    /// stream, so the words of the withheld answer would have been seen — which measures
    /// recall of a sentence rather than generalisation from one.
    /// </remarks>
    public IReadOnlyList<Turn<Coded>> Withheld => _kept;
}
