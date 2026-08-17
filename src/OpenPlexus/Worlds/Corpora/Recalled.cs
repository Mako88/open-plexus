using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>Which text, how much of it is visible at once, and how much is held back.</summary>
public sealed record RecalledSettings
{
    /// <summary>
    /// Where the text is — the bAbI task directory, or the Tatoeba export.
    /// </summary>
    public required string Corpus { get; init; }

    /// <summary>
    /// Which of the twenty tasks, or <b>nought to read <see cref="Corpus"/> as plain
    /// English</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One setting because it is one question — which text.</b> Two paths and a flag
    /// would let a run name a bAbI directory and a sentence count at once and mean
    /// nothing by it; the tasks are numbered one to twenty, so nought was free.
    /// </para>
    /// <para>
    /// <b>And it is not a choice of corpus</b> so much as the only one left. bAbI's
    /// held-out half is 1.000 recalled — some two thousand distinct contexts exist and
    /// no more — so reading it twice is re-reading it, whatever any arm scores. See
    /// <c>PrimerTests.Is_reading_real_english_predictive_at_all</c>.
    /// </para>
    /// </remarks>
    public required int Task { get; init; }

    /// <summary>
    /// How many sentences of plain English to read. <b>Ignored unless <see cref="Task"/>
    /// is nought.</b>
    /// </summary>
    public int Sentences { get; init; }

    /// <summary>
    /// How many statements before the question are in the moment — <b>nought for every
    /// statement of the story so far.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the one dial worth having</b>, and it is an arm rather than a setting. A
    /// scope is a set of co-present codes, so a moment holding a whole story is a bag of
    /// words and <i>Mary went to the kitchen, Mary went to the garden, where is Mary</i>
    /// puts both places in it with nothing to tell them apart. A bounded span is the
    /// crudest possible way to hand the bag some recency, and the gap between the two
    /// readings is what sequence is worth on this text at the grain of a whole statement.
    /// Rung three works at the grain of a WORD and is orthogonal to it: this world reports
    /// order through <see cref="Recited"/> whatever the span is.
    /// </para>
    /// <para>
    /// <b>And it is a fact about what was shown</b>, rather than about how to think, which
    /// is the line a world's own dials have to stay on. How much of a page a reader is
    /// allowed to see is the experiment's business; what a reader does with it is not, and
    /// nothing here names a brain type.
    /// </para>
    /// </remarks>
    public int Span { get; init; }

    /// <summary>Stories from the end of the file that this world will never draw.</summary>
    /// <remarks>
    /// <b>Whole stories and not whole questions</b>, which is what makes the arms
    /// comparable. <see cref="Predicting.Asked"/> only ever draws questions, so holding
    /// back a question hides its story too; every other arm draws the STATEMENTS, and would
    /// train on the very sentence the examination is about. Holding the story back is the
    /// only rule that means the same thing to all four.
    /// </remarks>
    public int Withheld { get; init; }

    /// <summary>What the learner is asked to be wrong about.</summary>
    public Predicting Predicting { get; init; }
}

/// <summary>
/// What a text world asks the learner to predict — <b>the choice this whole comparison
/// exists to make</b>, and one nothing in the field has made for a learner like this.
/// </summary>
/// <remarks>
/// <para>
/// <b>Next-token against masked is a settled experiment somewhere else</b>, and it settles
/// nothing here. Masked prediction beats next-token on comprehension at equal scale,
/// and it does so because a gradient reaches both sides of the gap — which is a fact about
/// backpropagation over embeddings and has no analogue in a population of commitments that
/// are individually blamed. The prior is worth having and it is not an answer.
/// </para>
/// <para>
/// <b>So every arm predicts a word from one alphabet</b>, and every arm sits the same
/// examination. An objective judged on its own target is unfalsifiable — a next-word
/// arm scores well at next words and says nothing about whether it understood anything.
/// What is compared is the POPULATION each one grows, put to an identical set of withheld
/// questions none of them was trained on.
/// </para>
/// </remarks>
public enum Predicting
{
    /// <summary>
    /// A question and the story in front of it, expecting the answer.
    /// </summary>
    /// <remarks>
    /// <b>The control</b>, and arguably the one closest to how a child is taught. Word
    /// meaning is not mostly acquired by tabulating text — it arrives by ostension, with
    /// somebody pointing and naming. A question with a right answer is that shape.
    /// </remarks>
    Asked,

    /// <summary>
    /// A statement with one word hidden, expecting the word that was hidden.
    /// </summary>
    /// <remarks>
    /// <b>John's co-occurrence, made falsifiable, which is the whole of what it needed.</b>
    /// Counting what occurs together can never be wrong; hiding one of them and demanding it
    /// back can. <b>And the hiding is not a detail</b> — a target still present in the moment
    /// is answerable by naming something already there, so nothing could ever be refuted and
    /// the one thing this architecture eats is gone.
    /// </remarks>
    Masked,

    /// <summary>
    /// The words of a statement so far, expecting the one that comes next.
    /// </summary>
    /// <remarks>
    /// <b>The arm nobody wants, here so the others mean something.</b> Without it a
    /// comparison between two objectives John already prefers would be a comparison with no
    /// floor. <b>And a scope is a SET</b>, so <i>the words so far</i> reaches the matcher as a
    /// bag — which is a real handicap on this arm rather than a thumb on the scale. Rung
    /// three softens it rather than removing it: the words are reported in order, so the
    /// precedences say which came after which and the scope still holds a set.
    /// </remarks>
    Next,

    /// <summary>
    /// Masked statements and asked questions, interleaved in the order written.
    /// </summary>
    /// <remarks>
    /// <b>John's curriculum, which is the one arm that is not a choice between the
    /// others.</b> Read the language, then answer about it — and C4 permits it, because a
    /// boundary the experimenter knows about is not a boundary the learner can detect. Here
    /// there is not even a boundary to detect: the two kinds of moment arrive interleaved,
    /// exactly as the corpus wrote them.
    /// </remarks>
    Mixed,

    /// <summary>
    /// A statement with its RAREST word hidden, expecting that word.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE FAIR FORM OF <see cref="Masked"/>, and it exists because the naive form
    /// failed for a reason that is not about co-occurrence.</b> Hiding every word in turn
    /// spends most of the demand on <i>the</i>, <i>to</i> and <i>back</i>, which is where a
    /// bag of words predicts best and carries least — so the population fills with the
    /// skeleton of English and answers a question with a preposition. Refuting an idea in
    /// the one form that was always going to lose is not refuting it.
    /// </para>
    /// <para>
    /// <b>And frequency is not a stop list</b>, which is the line this has to stay the right
    /// side of. No parser, tagger, template or hand-written word set goes near it — it
    /// is a count over the corpus, the same kind of fact <see cref="Babi.Commonest"/> is.
    /// What it is close to is worth saying out loud: a measured proxy for <i>informative</i>
    /// rather than a declared one, and the honest version of gating on surprise, which is a
    /// statistic only the brain holds.
    /// </para>
    /// </remarks>
    Salient,
}

/// <summary>
/// A moment of text, with the question kept apart from what was said before it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Which words were the question is a fact about the signal and not a conclusion</b>,
/// so a world is allowed to say it — the same licence <see cref="Codes.Coded.Groups"/>
/// carries for <i>these codes were one object</i>. What nothing here says is what to make
/// of the split, which is the front end's business and then the learner's.
/// </para>
/// <para>
/// <b>And unioning them at the world would have thrown it away before anybody could
/// choose.</b> A bag of words is what a scope sees, and a bag cannot be asked whether the
/// question named something the story named — so the one structural fact this task turns
/// on would have been destroyed by the world, one call before the translation that wants
/// it.
/// </para>
/// </remarks>
public readonly record struct Asking
{
    /// <summary>
    /// The statements in front of the question, newest first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Kept apart and ordered, because a bag cannot be asked which sentence a word came
    /// from.</b> A world saying <i>these words were one sentence, and it was the latest</i>
    /// is stating a fact about its signal — the same licence
    /// <see cref="Codes.Coded.Groups"/> and <see cref="Codes.Coded.Sequence"/> already
    /// carry. What to make of the order is the front end's, and every arm that wants none
    /// of it flattens this in one call.
    /// </para>
    /// <para>
    /// <b>Newest first so that a position means the same thing in every story.</b> Counting
    /// from the start would make <i>the second statement</i> a different distance from the
    /// question in a two-line story and a nine-line one, so a code minted on it would name
    /// two unrelated things.
    /// </para>
    /// </remarks>
    public required IReadOnlyList<IReadOnlySet<Code>> Story { get; init; }

    /// <summary>Every word of every statement, which is what a bag-of-words arm wants.</summary>
    public IReadOnlySet<Code> Words
    {
        get
        {
            var all = new HashSet<Code>();
            foreach (var one in Story) all.UnionWith(one);
            return all;
        }
    }

    /// <summary>The words of the question itself.</summary>
    public required IReadOnlySet<Code> Question { get; init; }
}

/// <summary>
/// What one withheld question said, in the English the corpus wrote it in.
/// </summary>
/// <remarks>
/// <b>The transcript and not the moment</b>, and nothing that learns ever sees it. A word
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
/// <b>No run has ever put text through this learner</b>, which is why this is small.
/// <see cref="Babi"/> has parsed the corpus into codes since before the commitment
/// primitive existed, and everything built on it was walk-shaped — an occasion writing
/// edges into clusters. Nothing joined the same words to a population of commitments,
/// so <i>can this learn from text at all</i> has been an argument rather than a number.
/// </para>
/// <para>
/// <b>A moment is words and an outcome is an index, which is what keeps one brain one
/// brain.</b> The answer is a position in this task's answer alphabet rather than a word
/// code, so <see cref="Machines.Brain.Says"/> maps it exactly as it maps a multiplexer
/// bit — and a compound answer is one string and therefore one outcome, which is how the
/// two structurally unanswerable tasks stop being a special case here.
/// </para>
/// <para>
/// <b>The answer word is in the moment and that is the point rather than a leak.</b>
/// <i>kitchen</i> is a word of the story and <i>the answer is kitchen</i> is an outcome,
/// and the one-code commitment joining them is exactly what genesis is for. What the
/// text does NOT hand over is which of the two places in the bag is the current one, and
/// that is the whole of the question this world asks.
/// </para>
/// <para>
/// <b>And what it measures when it fails is worth more than what it measures when it
/// passes.</b> <c>Tally.Wanting</c> is the share of blamed rounds nothing in the scope
/// language separates, computed every round since the branch began. A high reading here
/// is the ladder's own admission rule firing on text, which is the signal the design
/// exists for — and it is a different finding from a low score.
/// </para>
/// </remarks>
public sealed class Recalled : IWorld<Recited>, IWithholds<Recited>
{
    private readonly List<Turn<Recited>> _asked;
    private readonly ImmutableArray<Turn<Recited>> _kept;
    private int _at;

    /// <param name="settings">Which text, how much is visible, and how much is held back.</param>
    /// <exception cref="FileNotFoundException">The corpus is not there.</exception>
    public Recalled(RecalledSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Span);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Withheld);

        var lines = settings.Task == 0
            ? Primer.Numbered(settings.Corpus, settings.Sentences)
            : new Babi(new BabiSettings
            {
                Task = settings.Task,
                Corpus = settings.Corpus,

                // Off, because a code naming one story is present once and never again.
                // Genesis roots on a code that was live when the outcome started, so a
                // per-story code mints a commitment that can never fire a second time --
                // one resident a story, for a population that is bounded.
                Stories = false,
            }).Lines;

        // Whether the text asks anything, which decides what the examination can be and
        // is not a dial. bAbI writes questions, so the exam is the withheld questions.
        // Plain English writes none, so the only thing a withheld sentence can be
        // examined on is the objective itself -- the word that was hidden from it.
        var nothingAsks = !lines.Any(line => line.Asking);

        // One alphabet for every arm, and it is the vocabulary rather than the answer set.
        // A masked arm predicts any word of a statement and an asked arm predicts an answer,
        // so numbering only the answers would give the two objectives different outcome
        // spaces -- and an accuracy is not comparable across two different spaces. Sorted
        // ordinal because the index IS the outcome, and a set whose order is not promised
        // would make two runs of one seed disagree about what the machine said.
        var seen = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var line in lines)
        {
            foreach (var word in Babi.Words(line.Text ?? string.Empty)) seen.Add(word);
            if (line.Answer is { } answer) foreach (var word in Babi.Words(answer)) seen.Add(word);
        }

        Vocabulary = [.. seen];


        var index = Vocabulary
            .Select((word, at) => (word, at))
            .ToDictionary(one => one.word, one => one.at, StringComparer.Ordinal);

        // How often each word is written, counted over the whole file including the held
        // stories. Which words a language uses often is a fact about the language rather
        // than about this examination, and taking it from the drawn half alone would make
        // the arm's choice move with how much was withheld.
        var rarity = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var line in lines)
            foreach (var word in Babi.Words(line.Text ?? string.Empty))
                rarity[word] = rarity.GetValueOrDefault(word) + 1;

        // Which stories are held back, decided before a single turn is built. Every line of
        // them is skipped by every arm, so no objective can train on a sentence another
        // objective's examination is about.
        var stories = lines.Count == 0 ? 0 : lines[^1].Story + 1;
        var keeping = Math.Max(0, stories - settings.Withheld);

        var told = new List<Turn<Recited>>();
        var quizzed = new List<Turn<Recited>>();
        var wrote = new List<Quizzed>();
        var story = -1;
        var said = new List<ImmutableArray<Code>>();
        var wording = new List<string>();

        foreach (var line in lines)
        {
            // A new story forgets the last one, which is the corpus's own boundary and not
            // an episode the learner can see. Nothing about the machine changes here; what
            // changes is which words the world says are in front of it.
            if (line.Story != story)
            {
                story = line.Story;
                said.Clear();
                wording.Clear();
            }

            var held = line.Story >= keeping;

            if (!line.Asking)
            {
                said.Add(line.Words);
                wording.Add(line.Text ?? string.Empty);

                // A withheld sentence of a text that asks nothing is the examination, and
                // this is the one place the two corpora part. On bAbI a held statement is
                // dropped, because its story's QUESTIONS are what the exam is made of and
                // training on the statement would be training on the answer. Plain English
                // writes no questions, so dropping the held statements would leave nothing
                // to sit -- the exam is the same objective on sentences never read.
                if (!held) Reading(told, settings.Predicting, line, index, rarity);
                else if (nothingAsks) Reading(quizzed, settings.Predicting, line, index, rarity);

                continue;
            }

            var from = settings.Span == 0 ? 0 : Math.Max(0, said.Count - settings.Span);

            // NEWEST FIRST, which is the walk backwards from the question rather than
            // forwards from the story's start.
            var before = new List<IReadOnlyList<Code>>();
            for (var one = said.Count - 1; one >= from; one--)
                before.Add(said[one]);

            // The answer as a word rather than as an answer, which is what lets an arm that
            // never saw a question sit this examination at all. A compound answer takes its
            // first word and is wrong by construction, which is the honest reading -- the
            // two tasks whose answers are pairs were always unanswerable here.
            var answering = Babi.Words(line.Answer!);

            var turn = new Turn<Recited>
            {
                Seen = new Recited { Said = before, Asked = line.Words },
                Outcome = index[answering[0]],
            };

            if (held)
            {
                quizzed.Add(turn);

                // The same slice the moment was built from, so a transcript can never show a
                // statement the population was not given. A readback taken from the whole
                // story while the moment held one line would read as a machine that ignored
                // what it was told, which is a bug in the printing wearing a finding's clothes.
                wrote.Add(new Quizzed
                {
                    Story = string.Join(" ", wording.Skip(from)),
                    Question = line.Text ?? string.Empty,
                    Answer = line.Answer!,
                });
            }
            else if (settings.Predicting is Predicting.Asked or Predicting.Mixed) told.Add(turn);
        }

        _kept = [.. quizzed];
        _asked = told;
        Transcript = [.. wrote];

        if (_asked.Count == 0)
            throw new ArgumentException(
                $"task {settings.Task} under {settings.Predicting} with {settings.Withheld} "
                + "stories held back leaves nothing to learn from", nameof(settings));
    }

    /// <summary>
    /// Turns one statement into whatever the objective asks the learner to be wrong about.
    /// </summary>
    /// <remarks>
    /// <b>NOTHING AT ALL UNDER <see cref="Predicting.Asked"/>, which is the point of the
    /// control.</b> A statement with no question attached settles nothing, and a round that
    /// settles nothing takes no score, no genesis and no repair — so plain English moves not
    /// one counter. That is the whole reason a primer needed an objective before it could
    /// teach anything here.
    /// </remarks>
    private static void Reading(
        List<Turn<Recited>> told,
        Predicting predicting,
        Sentence line,
        IReadOnlyDictionary<string, int> index,
        IReadOnlyDictionary<string, int> rarity)
    {
        if (predicting is Predicting.Asked) return;

        var words = Babi.Words(line.Text ?? string.Empty);
        if (words.Count < 2) return;

        // One moment a statement rather than one a word, which is most of what this arm
        // changes. `Masked` demands every word of every sentence and so demands `the` far
        // more often than any place; this demands one word, and picks the one the corpus
        // says least often. Ties go to the earliest, which is arbitrary and fixed.
        var only = predicting is Predicting.Salient
            ? words.Select((word, at) => (word, at))
                .OrderBy(one => rarity.GetValueOrDefault(one.word, 0))
                .ThenBy(one => one.at)
                .First().at
            : -1;

        for (var at = 0; at < words.Count; at++)
        {
            if (only >= 0 && at != only) continue;

            // The moment is every other word, or every earlier one, and the difference is
            // the whole experiment. Both hand the learner a bag and demand one word back;
            // only the second one throws away half the evidence to do it.
            //
            // In the order they were written, with the hidden word simply missing rather
            // than marked. A masked statement is still a sentence, so the words either side
            // of the gap did stand that way round -- and closing the gap is what the
            // learner is asked about, not a claim this world makes.
            var moment = new List<Code>();

            for (var one = 0; one < words.Count; one++)
            {
                if (one == at) continue;
                if (predicting is Predicting.Next && one > at) continue;

                moment.Add(Babi.Of(words[one]));
            }

            if (moment.Count == 0) continue;

            told.Add(new Turn<Recited>
            {
                Seen = new Recited { Said = [moment], Asked = [] },
                Outcome = index[words[at]],
            });
        }
    }

    /// <summary>Every word the task uses, in the order the outcome index numbers them.</summary>
    /// <remarks>
    /// <b>The outcome space every arm shares</b>, so a masked arm and an asked arm are
    /// scored against the same alphabet and their accuracies mean the same thing.
    /// </remarks>
    public ImmutableArray<string> Vocabulary { get; }

    /// <summary>
    /// What each withheld question said, in the same order <see cref="Withheld"/> holds them.
    /// </summary>
    /// <remarks>
    /// <b>One row a withheld turn</b>, which is what lets a score be read as a conversation.
    /// Nothing that learns is given any of it — see <see cref="Quizzed"/>.
    /// </remarks>
    public ImmutableArray<Quizzed> Transcript { get; }

    /// <summary>How many questions this world will draw from.</summary>
    public int Questions => _asked.Count;

    /// <inheritdoc/>
    public int Outcomes => Vocabulary.Length;

    /// <summary>
    /// What always giving the commonest answer would score ON THE EXAMINATION.
    /// </summary>
    /// <remarks>
    /// <b>Taken over the withheld set and never over the drawn one, which is the only way
    /// four objectives share a bar.</b> Each arm draws a different stream — masked moments,
    /// next-word moments, questions — so a marginal read off what an arm SAW would be four
    /// different numbers, and every score would be measured against its own arm's skew.
    /// The examination is identical for all of them, so its marginal is too.
    /// </remarks>
    public double Commonest => _kept.Length == 0 ? 0.0 : _kept
        .GroupBy(one => one.Outcome!.Value)
        .Max(group => group.Count()) / (double)_kept.Length;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>In the order written and wrapping, so the stream is the file read over and
    /// over.</b> Drawing at random would be a different world — the corpus puts a
    /// question after the statements that answer it, and reordering that is the one
    /// property a story has.
    /// </remarks>
    public Turn<Recited> Next()
    {
        var turn = _asked[_at];
        _at = (_at + 1) % _asked.Count;
        return turn;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>The questions of whole stories this world never tells, and it is the same set
    /// under every objective.</b> Holding back a question alone would work for
    /// <see cref="Predicting.Asked"/>, which never draws a statement, and would leak
    /// outright for every other arm — the masked stream would carry the very sentence the
    /// examination is about. So a whole story goes, and the exam does not move when the
    /// objective does.
    /// </remarks>
    public IReadOnlyList<Turn<Recited>> Withheld => _kept;
}
