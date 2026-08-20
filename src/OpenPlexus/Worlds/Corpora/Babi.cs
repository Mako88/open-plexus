using System.Collections.Immutable;
using System.Globalization;
using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>
/// Which of the twenty bAbI tasks to read, and how much of the front end is
/// allowed to help.
/// </summary>
public sealed record BabiSettings
{
    /// <summary>The task number, 1 to 20.</summary>
    public required int Task { get; init; }

    /// <summary>
    /// The directory holding the task files — the <c>en/</c> or <c>en-10k/</c>
    /// folder of the distributed tarball.
    /// </summary>
    public required string Corpus { get; init; }

    /// <summary>
    /// Whether the front end mints a fleeting code naming the story a sentence
    /// belongs to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Off is the control</b>, and it is a harsher world than it looks. The
    /// corpus resets its sentence ids to mark a new story, and C4 forbids
    /// treating that as an episode boundary — nothing here may stop, retrain or
    /// clear. So with this off, <i>Lily is a frog</i> in one story and <i>Lily is
    /// a rhino</i> in the next are counted into the same row and the graph has no
    /// way to tell they were separate occasions.
    /// </para>
    /// <para>
    /// <b>On, the story is an observed code like any other</b> — the same
    /// mechanism <see cref="Composed"/> uses per scene, which is why it is an arm
    /// rather than a default. It is not a boundary: no state is reset and the
    /// stream never stops. It is one more thing present in the moment, and the
    /// walk may use it or not.
    /// </para>
    /// </remarks>
    public bool Stories { get; init; } = true;
}

/// <summary>
/// One line of a bAbI task — a statement to learn from, or a question to answer.
/// </summary>
/// <remarks>
/// <b>The two are the same type on purpose.</b> They arrive interleaved in one
/// stream and the system is not told which is which until it is asked; a
/// statement is observed and a question is broadcast, and nothing about the
/// parsing distinguishes them beyond the corpus having written an answer down.
/// </remarks>
public sealed record Sentence
{
    /// <summary>Which story this line belongs to, counting from zero.</summary>
    public required int Story { get; init; }

    /// <summary>
    /// Every word of the line, as codes. <b>Includes the story code when
    /// <see cref="BabiSettings.Stories"/> is on.</b>
    /// </summary>
    public required ImmutableArray<Code> Words { get; init; }

    /// <summary>
    /// What the corpus says the answer is, verbatim. <b>Null on a statement.</b>
    /// </summary>
    public string? Answer { get; init; }

    /// <summary>
    /// The answer's own words, as codes. <b>More than one is a compound answer</b>
    /// — see <see cref="Babi.Compound"/>.
    /// </summary>
    public ImmutableArray<Code> Answers { get; init; } = [];

    /// <summary>
    /// The line as the corpus wrote it, without its number or its answer column.
    /// </summary>
    /// <remarks>
    /// <b>Kept so an answer can be read back in English</b>, which nothing could do before.
    /// A word becomes <see cref="Babi.Of"/>'s hash and a hash goes nowhere back, so a run
    /// that answered a question correctly could report a number and never the question. It
    /// is shown to nobody and to nothing that learns — see <see cref="Recalled"/>, which
    /// puts codes in the moment and this in the transcript.
    /// </remarks>
    public string? Text { get; init; }

    /// <summary>Whether this line is asking something.</summary>
    public bool Asking => Answer is not null;
}

/// <summary>
/// The bAbI tasks (Weston et al., 2015), read off disk and turned into codes.
/// </summary>
/// <remarks>
/// <para>
/// <b>The point of this world is that nobody here designed it.</b> Every other
/// world in this project was built by the same hands that built the mechanism it
/// measures, so a result on one of them can only ever say the mechanism does what
/// its author expected. Twenty tasks written by somebody else, with published
/// baselines, cannot flatter this architecture by construction.
/// </para>
/// <para>
/// <b>The tasks are not one task.</b> They were chosen to isolate different
/// capabilities, and this architecture has no reason to be uniform across them —
/// <i>basic deduction</i> and <i>basic induction</i> are chains through an
/// intermediate, which is exactly the senses world's shape on somebody else's
/// data, while <i>single supporting fact</i> asks where somebody is <b>now</b> and
/// a co-occurrence count has no notion of now. <b>The per-task profile is the
/// result,</b> and an average over twenty would hide the only interesting thing
/// here.
/// </para>
/// <para>
/// <b>Nothing is engineered per task.</b> The corpus README asks specifically that
/// the same learner be used across all twenty without task-specific tricks, so the
/// front end here is as dumb as it can be: a sentence is its words, a word is the
/// hash of its letters, and no stop-list, parser, tagger or template goes
/// anywhere near it. The common words are left in deliberately — <i>the</i> and
/// <i>to</i> occur with everything, and a learner that cannot survive that has not
/// been put to real text. It is also the wall the plan records: the informative
/// words are the unpredictable ones and the predictable ones are these.
/// </para>
/// <para>
/// <b>The corpus is not in this repository.</b> It is CC BY 3.0 and eleven
/// megabytes, so it is fetched into a gitignored <c>corpora/</c> and
/// <c>BabiTests</c> says exactly how when it is missing.
/// </para>
/// </remarks>
public sealed class Babi
{
    /// <summary>Every word of the text, whatever part of speech it is.</summary>
    /// <remarks>
    /// <b>One modality, and that is the honest choice.</b> Splitting actors from
    /// places from objects would be a parser the system did not earn, and it would
    /// hand the narrowing step the answer's type for free.
    /// </remarks>
    private const byte Word = 40;

    /// <summary>The fleeting code naming one story. See <see cref="BabiSettings.Stories"/>.</summary>
    public const byte Story = 41;

    private readonly BabiSettings _settings;

    /// <param name="settings">Which task, from where, and with which arms on.</param>
    /// <exception cref="FileNotFoundException">The task file is not there.</exception>
    public Babi(BabiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Task, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Task, 20);

        _settings = settings;

        Lines = Read(File.ReadAllLines(Find(settings.Corpus, settings.Task)), settings.Stories);

        var answers = Lines.Where(line => line.Asking).ToList();

        Alphabet = answers
            .Select(line => line.Answer!)
            .ToHashSet(StringComparer.Ordinal);

        Compound = answers.Count(line => line.Answers.Length != 1);

        Commonest = answers.Count == 0 ? 0.0 : answers
            .GroupBy(line => line.Answer!, StringComparer.Ordinal)
            .Max(group => group.Count()) / (double)answers.Count;
    }

    /// <summary>The whole task file, statements and questions in the order written.</summary>
    public IReadOnlyList<Sentence> Lines { get; }

    /// <summary>Every distinct answer string the task ever expects.</summary>
    public IReadOnlySet<string> Alphabet { get; }

    /// <summary>
    /// Questions whose answer is more than one word.
    /// </summary>
    /// <remarks>
    /// <b>Structurally unanswerable here, and counted rather than excluded.</b> A
    /// walk returns the one endpoint it ranked first, so <i>lists and sets</i> and
    /// <i>path finding</i> — whose answers are comma-joined pairs — cannot be got
    /// right however good the graph is. Dropping them from the denominator would
    /// quietly raise the score for a reason that has nothing to do with the graph,
    /// so they are scored wrong and reported here, and the two numbers together
    /// say which it was.
    /// </remarks>
    public int Compound { get; }

    /// <summary>
    /// What always answering the single most common answer would score.
    /// </summary>
    /// <remarks>
    /// <b>The baseline that matters, and it is well above uniform chance.</b>
    /// Uniform over the answer alphabet is what <see cref="Chance"/> reports and
    /// it is too easy to beat — the answers are not uniformly distributed, so a
    /// system that has learnt nothing but the marginal already clears it. This is
    /// what a memoriser with no reference to the story scores.
    /// </remarks>
    public double Commonest { get; }

    /// <summary>A blind draw from the answer alphabet.</summary>
    public double Chance => Alphabet.Count == 0 ? 0.0 : 1.0 / Alphabet.Count;

    /// <summary>The code for a word, wherever it appears.</summary>
    /// <remarks>
    /// <b>Lowercased first, and that is the only normalising done to the text.</b>
    /// <i>Mary</i> at the start of a sentence and <i>mary</i> inside one are the
    /// same word; everything beyond that would be a parser this system did not
    /// earn.
    /// </remarks>
    public static Code Of(string word)
    {
        ArgumentNullException.ThrowIfNull(word);
        return Kinds.Named(Word, word.ToLowerInvariant());
    }

    /// <summary>The fleeting code for one story.</summary>
    public static Code Telling(int story)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(story);
        return new Code(Story, (ulong)story);
    }

    /// <summary>
    /// The words of a line, lowercased and stripped of everything that is not a
    /// letter or a digit.
    /// </summary>
    public static IReadOnlyList<string> Words(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return [.. text
            .Split(Breaks, StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.ToLowerInvariant())];
    }

    /// <summary>
    /// Everything that ends a word. <b>The comma is in here because it is what
    /// joins a compound answer</b> — <c>milk,football</c> is two answers and has
    /// to parse as two.
    /// </summary>
    private static readonly char[] Breaks =
        [' ', '\t', '\r', '\n', '.', '?', '!', ',', ';', ':'];

    /// <summary>
    /// Turns the lines of a task file into sentences.
    /// </summary>
    /// <remarks>
    /// <b>The format is <c>id text</c>, and a tab means a question</b>:
    /// <c>id question[tab]answer[tab]supporting ids</c>. A story begins wherever
    /// the id resets to 1, which is the only boundary the corpus marks — and the
    /// supporting ids are deliberately dropped, because using them is the strong
    /// supervision the corpus authors ask people to do without.
    /// </remarks>
    /// <param name="lines">The file, as read.</param>
    /// <param name="stories">Whether to mint the fleeting story code.</param>
    public static IReadOnlyList<Sentence> Read(IEnumerable<string> lines, bool stories)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var read = new List<Sentence>();
        var story = -1;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var space = line.IndexOf(' ', StringComparison.Ordinal);
            if (space <= 0) continue;

            if (!int.TryParse(
                    line.AsSpan(0, space), NumberStyles.None, CultureInfo.InvariantCulture, out var id))
                continue;

            if (id == 1) story++;

            var text = line[(space + 1)..];
            var parts = text.Split('\t');

            var words = Words(parts[0]).Select(Of).ToList();
            if (stories) words.Add(Telling(story));

            read.Add(new Sentence
            {
                Story = story,
                Words = [.. words],
                Answer = parts.Length > 1 ? parts[1].Trim() : null,
                Answers = parts.Length > 1 ? [.. Words(parts[1]).Select(Of)] : [],
                Text = parts[0].Trim(),
            });
        }

        return read;
    }

    /// <summary>The task file for a task number, whatever it is called.</summary>
    /// <remarks>
    /// <b>Matched on the <c>qaN_</c> prefix rather than the full name</b>, because
    /// the names carry the task's description and nothing should have to keep a
    /// copy of those in sync with the tarball.
    /// </remarks>
    /// <param name="corpus">The directory of task files.</param>
    /// <param name="task">Which task, 1 to 20.</param>
    /// <exception cref="FileNotFoundException">No training file for that task.</exception>
    private static string Find(string corpus, int task)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpus);

        if (!Directory.Exists(corpus))
            throw new FileNotFoundException(
                $"the bAbI corpus is not at {corpus} — see BabiTests for how to fetch it");

        var prefix = $"qa{task.ToString(CultureInfo.InvariantCulture)}_";

        var file = Directory
            .EnumerateFiles(corpus, "*_train.txt")
            .Where(path => Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .FirstOrDefault();

        return file ?? throw new FileNotFoundException(
            $"no {prefix}*_train.txt under {corpus}");
    }

    /// <summary>Which task this is.</summary>
    public int Task => _settings.Task;

    /// <inheritdoc cref="BabiSettings.Stories"/>
    public bool Stories => _settings.Stories;
}
