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

    /// <summary>Stories from the end of the file that this world will never draw.</summary>
    /// <remarks>
    /// <b>WHOLE STORIES AND NOT WHOLE QUESTIONS, WHICH IS WHAT MAKES THE ARMS
    /// COMPARABLE.</b> <see cref="Predicting.Asked"/> only ever draws questions, so holding
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
/// exists to make, and one nothing in the field has made for a learner like this.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>NEXT-TOKEN AGAINST MASKED IS A SETTLED EXPERIMENT SOMEWHERE ELSE AND SETTLES
/// NOTHING HERE.</b> Masked prediction beats next-token on comprehension at equal scale,
/// and it does so because a gradient reaches both sides of the gap — which is a fact about
/// backpropagation over embeddings and has no analogue in a population of commitments that
/// are individually blamed. The prior is worth having and it is not an answer.
/// </para>
/// <para>
/// <b>SO EVERY ARM PREDICTS A WORD FROM ONE ALPHABET, AND EVERY ARM SITS THE SAME
/// EXAMINATION.</b> An objective judged on its own target is unfalsifiable — a next-word
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
    /// <b>THE CONTROL, AND ARGUABLY THE ONE CLOSEST TO HOW A CHILD IS TAUGHT.</b> Word
    /// meaning is not mostly acquired by tabulating text — it arrives by ostension, with
    /// somebody pointing and naming. A question with a right answer is that shape.
    /// </remarks>
    Asked,

    /// <summary>
    /// A statement with one word hidden, expecting the word that was hidden.
    /// </summary>
    /// <remarks>
    /// <b>JOHN'S CO-OCCURRENCE, MADE FALSIFIABLE, WHICH IS THE WHOLE OF WHAT IT NEEDED.</b>
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
    /// <b>THE ARM NOBODY WANTS, HERE SO THE OTHERS MEAN SOMETHING.</b> Without it a
    /// comparison between two objectives John already prefers would be a comparison with no
    /// floor. <b>And a scope is a SET, so *the words so far* reaches this learner as a bag
    /// with the order thrown away</b> — which is a real handicap on this arm rather than a
    /// thumb on the scale, and it is the same missing rung three everything else here wants.
    /// </remarks>
    Next,

    /// <summary>
    /// Masked statements and asked questions, interleaved in the order written.
    /// </summary>
    /// <remarks>
    /// <b>JOHN'S CURRICULUM, WHICH IS THE ONE ARM THAT IS NOT A CHOICE BETWEEN THE
    /// OTHERS.</b> Read the language, then answer about it — and C4 permits it, because a
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
    /// <b>THE FAIR FORM OF <see cref="Masked"/>, AND IT EXISTS BECAUSE THE NAIVE FORM
    /// FAILED FOR A REASON THAT IS NOT ABOUT CO-OCCURRENCE.</b> Hiding every word in turn
    /// spends most of the demand on <i>the</i>, <i>to</i> and <i>back</i>, which is where a
    /// bag of words predicts best and carries least — so the population fills with the
    /// skeleton of English and answers a question with a preposition. Refuting an idea in
    /// the one form that was always going to lose is not refuting it.
    /// </para>
    /// <para>
    /// <b>AND FREQUENCY IS NOT A STOP LIST, WHICH IS THE LINE THIS HAS TO STAY THE RIGHT
    /// SIDE OF.</b> No parser, tagger, template or hand-written word set goes near it — it
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
/// <b>WHICH WORDS WERE THE QUESTION IS A FACT ABOUT THE SIGNAL AND NOT A CONCLUSION</b>,
/// so a world is allowed to say it — the same licence <see cref="Codes.Coded.Groups"/>
/// carries for <i>these codes were one object</i>. What nothing here says is what to make
/// of the split, which is the front end's business and then the learner's.
/// </para>
/// <para>
/// <b>AND UNIONING THEM AT THE WORLD WOULD HAVE THROWN IT AWAY BEFORE ANYBODY COULD
/// CHOOSE.</b> A bag of words is what a scope sees, and a bag cannot be asked whether the
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
    /// <b>KEPT APART AND ORDERED, BECAUSE A BAG CANNOT BE ASKED WHICH SENTENCE A WORD CAME
    /// FROM.</b> A world saying <i>these words were one sentence, and it was the latest</i>
    /// is stating a fact about its signal — the same licence
    /// <see cref="Codes.Coded.Groups"/> and <see cref="Codes.Coded.Sequence"/> already
    /// carry. What to make of the order is the front end's, and every arm that wants none
    /// of it flattens this in one call.
    /// </para>
    /// <para>
    /// <b>NEWEST FIRST SO THAT A POSITION MEANS THE SAME THING IN EVERY STORY.</b> Counting
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
public sealed class Recalled : IWorld<Asking>, IWithholds<Asking>
{
    private readonly List<Turn<Asking>> _asked;
    private readonly ImmutableArray<Turn<Asking>> _kept;
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

        // ONE ALPHABET FOR EVERY ARM, AND IT IS THE VOCABULARY RATHER THAN THE ANSWER SET.
        // A masked arm predicts any word of a statement and an asked arm predicts an answer,
        // so numbering only the answers would give the two objectives different outcome
        // spaces -- and an accuracy is not comparable across two different spaces. Sorted
        // ordinal because the index IS the outcome, and a set whose order is not promised
        // would make two runs of one seed disagree about what the machine said.
        var seen = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var line in text.Lines)
        {
            foreach (var word in Babi.Words(line.Text ?? string.Empty)) seen.Add(word);
            if (line.Answer is { } answer) foreach (var word in Babi.Words(answer)) seen.Add(word);
        }

        Vocabulary = [.. seen];

        // A COUNT OVER THE CORPUS IS A FACT ABOUT THE SIGNAL, WHICH IS THE ONLY REASON THIS
        // MAY LEAVE THE WORLD AT ALL. How often a word is written is the same kind of thing
        // <see cref="Commonest"/> already is and the same statistic
        // <see cref="Predicting.Salient"/> already picks on -- it says nothing about what a
        // word MEANS, which is the line a world may not cross. What reads it is the join,
        // where a translation is allowed to know something about the signal it is
        // translating.
        var counted = new Dictionary<Code, int>();

        var index = Vocabulary
            .Select((word, at) => (word, at))
            .ToDictionary(one => one.word, one => one.at, StringComparer.Ordinal);

        // HOW OFTEN EACH WORD IS WRITTEN, COUNTED OVER THE WHOLE FILE INCLUDING THE HELD
        // STORIES. Which words a language uses often is a fact about the language rather
        // than about this examination, and taking it from the drawn half alone would make
        // the arm's choice move with how much was withheld.
        var rarity = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var line in text.Lines)
            foreach (var word in Babi.Words(line.Text ?? string.Empty))
                rarity[word] = rarity.GetValueOrDefault(word) + 1;

        foreach (var (word, count) in rarity) counted[Babi.Of(word)] = count;

        Frequency = counted;

        // WHICH STORIES ARE HELD BACK, DECIDED BEFORE A SINGLE TURN IS BUILT. Every line of
        // them is skipped by every arm, so no objective can train on a sentence another
        // objective's examination is about.
        var stories = text.Lines.Count == 0 ? 0 : text.Lines[^1].Story + 1;
        var keeping = Math.Max(0, stories - settings.Withheld);

        var told = new List<Turn<Asking>>();
        var quizzed = new List<Turn<Asking>>();
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

            var held = line.Story >= keeping;

            if (!line.Asking)
            {
                said.Add(line.Words);
                wording.Add(line.Text ?? string.Empty);

                if (!held) Reading(told, settings.Predicting, line, index, rarity);
                continue;
            }

            var from = settings.Span == 0 ? 0 : Math.Max(0, said.Count - settings.Span);

            // NEWEST FIRST, which is the walk backwards from the question rather than
            // forwards from the story's start.
            var before = new List<IReadOnlySet<Code>>();
            for (var one = said.Count - 1; one >= from; one--)
                before.Add(new HashSet<Code>(said[one]));

            // THE ANSWER AS A WORD RATHER THAN AS AN ANSWER, which is what lets an arm that
            // never saw a question sit this examination at all. A compound answer takes its
            // first word and is wrong by construction, which is the honest reading -- the
            // two tasks whose answers are pairs were always unanswerable here.
            var answering = Babi.Words(line.Answer!);

            var turn = new Turn<Asking>
            {
                Seen = new Asking { Story = before, Question = new HashSet<Code>(line.Words) },
                Outcome = index[answering[0]],
            };

            if (held)
            {
                quizzed.Add(turn);

                // THE SAME SLICE THE MOMENT WAS BUILT FROM, so a transcript can never show a
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
    /// <b>NOTHING AT ALL UNDER <see cref="Predicting.Asked"/>, WHICH IS THE POINT OF THE
    /// CONTROL.</b> A statement with no question attached settles nothing, and a round that
    /// settles nothing takes no score, no genesis and no repair — so plain English moves not
    /// one counter. That is the whole reason a primer needed an objective before it could
    /// teach anything here.
    /// </remarks>
    private static void Reading(
        List<Turn<Asking>> told,
        Predicting predicting,
        Sentence line,
        IReadOnlyDictionary<string, int> index,
        IReadOnlyDictionary<string, int> rarity)
    {
        if (predicting is Predicting.Asked) return;

        var words = Babi.Words(line.Text ?? string.Empty);
        if (words.Count < 2) return;

        // ONE MOMENT A STATEMENT RATHER THAN ONE A WORD, which is most of what this arm
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

            // THE MOMENT IS EVERY OTHER WORD, OR EVERY EARLIER ONE, AND THE DIFFERENCE IS
            // THE WHOLE EXPERIMENT. Both hand the learner a bag and demand one word back;
            // only the second one throws away half the evidence to do it.
            var moment = new HashSet<Code>();

            for (var one = 0; one < words.Count; one++)
            {
                if (one == at) continue;
                if (predicting is Predicting.Next && one > at) continue;

                moment.Add(Babi.Of(words[one]));
            }

            if (moment.Count == 0) continue;

            told.Add(new Turn<Asking>
            {
                Seen = new Asking { Story = [moment], Question = new HashSet<Code>() },
                Outcome = index[words[at]],
            });
        }
    }

    /// <summary>
    /// How often each word is written in the corpus, the held stories included.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>WHAT A JOIN NEEDS TO TELL A KEY FROM A FUNCTION WORD, AND THE ONLY THING IT IS
    /// TOLD.</b> <see cref="Codes.Joining.Situated"/> has to decide which shared word means
    /// two statements are about the same thing, and <i>to</i> is shared by every sentence in
    /// the corpus while <i>mary</i> is not. Nothing here says which is which — it hands over
    /// a count and the arm's own dial decides where to cut.
    /// </para>
    /// <para>
    /// <b>OVER THE WHOLE FILE, FOR THE REASON THE RARITY BEHIND
    /// <see cref="Predicting.Salient"/> IS.</b> Which words a language uses often is a fact
    /// about the language rather than about this examination, so taking it from the drawn
    /// half alone would make a front end's behaviour move with how much was withheld.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<Code, int> Frequency { get; }

    /// <summary>Every word the task uses, in the order the outcome index numbers them.</summary>
    /// <remarks>
    /// <b>THE OUTCOME SPACE EVERY ARM SHARES</b>, so a masked arm and an asked arm are
    /// scored against the same alphabet and their accuracies mean the same thing.
    /// </remarks>
    public ImmutableArray<string> Vocabulary { get; }

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
    public int Outcomes => Vocabulary.Length;

    /// <summary>
    /// What always giving the commonest answer would score ON THE EXAMINATION.
    /// </summary>
    /// <remarks>
    /// <b>TAKEN OVER THE WITHHELD SET AND NEVER OVER THE DRAWN ONE, WHICH IS THE ONLY WAY
    /// FOUR OBJECTIVES SHARE A BAR.</b> Each arm draws a different stream — masked moments,
    /// next-word moments, questions — so a marginal read off what an arm SAW would be four
    /// different numbers, and every score would be measured against its own arm's skew.
    /// The examination is identical for all of them, so its marginal is too.
    /// </remarks>
    public double Commonest => _kept.Length == 0 ? 0.0 : _kept
        .GroupBy(one => one.Outcome!.Value)
        .Max(group => group.Count()) / (double)_kept.Length;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>IN THE ORDER WRITTEN AND WRAPPING, so the stream is the file read over and
    /// over.</b> Drawing at random would be a different world — the corpus puts a
    /// question after the statements that answer it, and reordering that is the one
    /// property a story has.
    /// </remarks>
    public Turn<Asking> Next()
    {
        var turn = _asked[_at];
        _at = (_at + 1) % _asked.Count;
        return turn;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>THE QUESTIONS OF WHOLE STORIES THIS WORLD NEVER TELLS, AND IT IS THE SAME SET
    /// UNDER EVERY OBJECTIVE.</b> Holding back a question alone would work for
    /// <see cref="Predicting.Asked"/>, which never draws a statement, and would leak
    /// outright for every other arm — the masked stream would carry the very sentence the
    /// examination is about. So a whole story goes, and the exam does not move when the
    /// objective does.
    /// </remarks>
    public IReadOnlyList<Turn<Asking>> Withheld => _kept;
}
