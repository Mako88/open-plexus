using System.Text;
using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>One examination question and the answer the lesson says it has.</summary>
public readonly record struct Quiz
{
    /// <summary>The question, written as it is typed.</summary>
    public required string Question { get; init; }

    /// <summary>The one word the lesson says answers it.</summary>
    public required string Answer { get; init; }
}

/// <summary>
/// A hand-written topic told once and a fixed set of questions about it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Told once and fixed, which is what a drawn world cannot be.</b> Every reading on this
/// conversation so far was taken against a freshly drawn topic, so an adjustment moved the
/// score and the world at the same time. One lesson holds the world still, and an arm is then
/// read against one thing.
/// </para>
/// <para>
/// <b>Hand-written rather than found, and it is John's.</b> A found text gives neither
/// enumerable ground truth nor a computable no-learning bar, and a reading with no bar under it
/// cannot be interpreted. The published-baseline exam stays bAbI's; this is the instrument an
/// adjustment is read on.
/// </para>
/// <para>
/// <b>A question repeats the statement's own words rather than inflecting them</b>, and that is
/// a choice made in the open. A word is one hash here, so <i>says</i> and <i>say</i> are as
/// unrelated as <i>says</i> and <i>kitchen</i> — a lesson asking <i>what does a cat say</i>
/// about a statement reading <i>a cat says meow</i> would be measuring fork <b>108</b> and
/// nothing else. Sub-word codes are the fork that would change this, and until they exist the
/// honest lesson is one whose question and statement share their words.
/// </para>
/// </remarks>
public sealed record Lesson
{
    /// <summary>What the lesson is about, for a session to print.</summary>
    public required string About { get; init; }

    /// <summary>The statements, in the order they are told.</summary>
    public required IReadOnlyList<string> Statements { get; init; }

    /// <summary>The questions, in the order they are put.</summary>
    public required IReadOnlyList<Quiz> Exam { get; init; }

    /// <summary>Statements told AFTER the telling that contradict it.</summary>
    /// <remarks>
    /// <b>John's, and it is the half a monotone counter cannot do.</b> Hits and misses are
    /// G-counters and nothing can retract, so a superseded belief is never deleted — it
    /// accrues misses while a newer commitment accrues hits, and the vote is what moves. How
    /// much correction that takes is the reading rather than the design.
    /// </remarks>
    public IReadOnlyList<string> Revisions { get; init; } = [];

    /// <summary>Four creatures and three properties each, with a category over them.</summary>
    /// <remarks>
    /// <para>
    /// <b>Interleaved rather than blocked, so recency is worth little.</b> Every creature's
    /// properties are told together, so by the time a question about the first one is put, three
    /// creatures' worth of statements sit in front of it — and the freshest statement sharing a
    /// word with the question is almost never the one that answers it. That is what
    /// <see cref="Tutor.Recency"/> counts.
    /// </para>
    /// <para>
    /// <b>And <i>is an animal</i> earns its line by answering nothing.</b> It puts one word in
    /// every creature's statements and in no answer, so a rule keyed on it covers the whole
    /// lesson and is right about nothing — which is the distractor a category is, said in the
    /// text rather than handed over.
    /// </para>
    /// </remarks>
    public static Lesson Creatures { get; } = new()
    {
        About = "four creatures and what is true of each",

        Statements =
        [
            "the cat is an animal.",
            "the cat covering is fur.",
            "the cat sound is meow.",
            "the cat food is fish.",
            "the dog is an animal.",
            "the dog covering is hair.",
            "the dog sound is bark.",
            "the dog food is meat.",
            "the bird is an animal.",
            "the bird covering is feathers.",
            "the bird sound is tweet.",
            "the bird food is seeds.",
            "the snake is an animal.",
            "the snake covering is scales.",
            "the snake sound is hiss.",
            "the snake food is mice.",
        ],

        Exam =
        [
            new Quiz { Question = "what is the cat sound?", Answer = "meow" },
            new Quiz { Question = "what is the bird food?", Answer = "seeds" },
            new Quiz { Question = "what is the snake covering?", Answer = "scales" },
            new Quiz { Question = "what is the dog food?", Answer = "meat" },
            new Quiz { Question = "what is the cat covering?", Answer = "fur" },
            new Quiz { Question = "what is the snake sound?", Answer = "hiss" },
            new Quiz { Question = "what is the bird covering?", Answer = "feathers" },
            new Quiz { Question = "what is the dog sound?", Answer = "bark" },
            new Quiz { Question = "what is the cat food?", Answer = "fish" },
            new Quiz { Question = "what is the snake food?", Answer = "mice" },
            new Quiz { Question = "what is the dog covering?", Answer = "hair" },
            new Quiz { Question = "what is the bird sound?", Answer = "tweet" },
        ],
    };

    /// <summary>The same four creatures, with three facts changed after the telling.</summary>
    /// <remarks>
    /// <para>
    /// <b>John's, and a monotone counter cannot do it by deleting.</b> Three
    /// creatures are given a new food, covering and sound after the lesson has been told, and
    /// the examination expects the new ones. Nothing retracts the old belief: it accrues misses
    /// while a newer commitment accrues hits, and what moves is the vote.
    /// </para>
    /// <para>
    /// <b>Three changed and nine left alone, so it controls itself.</b> A machine
    /// that simply forgot everything on being contradicted would lose the nine, and a machine
    /// that could not be corrected would lose the three — those are opposite failures and one
    /// number over twelve questions would read the same for both.
    /// </para>
    /// </remarks>
    public static Lesson Corrected { get; } = Creatures with
    {
        About = "the same four creatures, with three facts changed later",

        Revisions =
        [
            "the cat food is milk.",
            "the bird covering is down.",
            "the snake sound is rattle.",
        ],

        Exam =
        [
            .. Creatures.Exam.Select(one => one.Question switch
            {
                "what is the cat food?" => one with { Answer = "milk" },
                "what is the bird covering?" => one with { Answer = "down" },
                "what is the snake sound?" => one with { Answer = "rattle" },
                _ => one,
            }),
        ],
    };

    /// <summary>Four creatures whose loudness is never stated and follows from two facts.</summary>
    /// <remarks>
    /// <para>
    /// <b>Whether it can reach a conclusion nobody told it</b>, which is the question the score
    /// on <see cref="Creatures"/> cannot answer. Every fact there is stated outright, so a
    /// perfect reading is a lookup with a good index. Here the sound of each creature is stated
    /// and the loudness of each SOUND is stated, and the loudness of the CREATURE is not — it
    /// follows from the two and appears in no statement beside the thing it is about.
    /// </para>
    /// <para>
    /// <b>Half the exam is stated and half is not, so the run controls itself.</b> Four
    /// questions ask what a statement says and four ask what two statements imply. One number
    /// over eight would read the same for a machine that chains and one that cannot, and the
    /// split says which — a half is one hop, and anything above it is two.
    /// </para>
    /// <para>
    /// <b>And every answer is distinct</b>, so the marginal is an eighth and a machine saying
    /// one word every time cannot climb.
    /// </para>
    /// </remarks>
    public static Lesson Chained { get; } = new()
    {
        About = "four creatures, their sounds, and how loud each SOUND is",

        Statements =
        [
            "the cat sound is meow.",
            "the meow loudness is faint.",
            "the dog sound is bark.",
            "the bark loudness is harsh.",
            "the bird sound is tweet.",
            "the tweet loudness is shrill.",
            "the snake sound is hiss.",
            "the hiss loudness is soft.",
        ],

        Exam =
        [
            // Stated outright, which is the control half.
            new Quiz { Question = "what is the cat sound?", Answer = "meow" },
            new Quiz { Question = "what is the dog sound?", Answer = "bark" },
            new Quiz { Question = "what is the bird sound?", Answer = "tweet" },
            new Quiz { Question = "what is the snake sound?", Answer = "hiss" },

            // Never stated, and reachable only by putting two statements together.
            new Quiz { Question = "what is the cat loudness?", Answer = "faint" },
            new Quiz { Question = "what is the dog loudness?", Answer = "harsh" },
            new Quiz { Question = "what is the bird loudness?", Answer = "shrill" },
            new Quiz { Question = "what is the snake loudness?", Answer = "soft" },
        ],
    };
}

/// <summary>
/// The human side of a scripted conversation — tells a <see cref="Lesson"/>, answers what it
/// was asked about where it knows, and then examines.
/// </summary>
/// <remarks>
/// <para>
/// <b>A person who reacts rather than a list of lines</b>, which is forced by the world. The
/// machine consumes a reply only where it decided to ask, so which line of a script lands where
/// depends on what the population did — a transcript played back would answer questions that
/// were never put.
/// </para>
/// <para>
/// <b>It watches what was printed, because that is all it is given.</b> A reply is wanted
/// exactly where the last thing written has no newline after it, which is the shape of a prompt.
/// <see cref="Printed"/> is the writer a session hands to
/// <see cref="ConversingSettings.Printed"/> so that this can read the terminal the way a person
/// does.
/// </para>
/// <para>
/// <b>Nobody knows the answer to a statement</b>, which is the common case and not the awkward
/// one. A machine asking about a word of <i>the cat sound is meow</i> is asking a question the
/// lesson has no answer to, so the reply is blank and the round settles on the shrug.
/// </para>
/// <para>
/// <b>And the exam is put more than once on purpose.</b> The first pass is the only one that
/// says whether being told the statements taught anything; every pass after it says what being
/// corrected teaches, which is a different question with the same accuracy on it.
/// </para>
/// </remarks>
public sealed class Tutor : TextReader
{
    private readonly Lesson _lesson;
    private readonly int _passes;
    private readonly int _tellings;
    private readonly int _revising;
    private readonly int _clarifying;
    private readonly TextReader? _person;
    private readonly Watched _printed;
    private readonly int[] _put;
    private readonly int[] _confirmed;

    private string? _answer;
    private int _told;
    private int _at;
    private int _pass;
    private int _revised;
    private int _clarified;

    // Whether the keyboard is the person's rather than the script's. While it is, every read
    // goes straight through -- a line to say, and the answer to whatever the machine asks.
    private bool _theirs;

    // Which pass the question on the table was PUT on, which is not always the current one. A
    // pass rolls over as its last question is handed out, so a reply arriving after that would
    // otherwise be scored against the pass that has not started.
    private int _asking;

    /// <param name="lesson">The topic and the questions.</param>
    /// <param name="printed">Where the machine's words should end up.</param>
    /// <param name="passes">How many times the examination is put.</param>
    /// <param name="tellings">
    /// How many times the statements are told before the examination — <b>repetition, and it
    /// is an axis rather than a setting</b>. Being told once and being told twenty times are
    /// different amounts of evidence for the same claim, and which of them a machine needs is
    /// the reading.
    /// </param>
    /// <param name="revising">How many times the contradicting statements are told after it.</param>
    /// <param name="clarifying">
    /// How many moments the person gets between the telling and the examination —
    /// <b>John's, and it is where the back and forth happens</b>. The machine's questions are
    /// not scriptable, so this is the window a person answers them in and adds whatever the
    /// answers make them want to add.
    /// </param>
    /// <param name="person">Whose keyboard the clarifying window reads.</param>
    public Tutor(
        Lesson lesson, TextWriter printed, int passes = 1, int tellings = 1,
        int revising = 0, int clarifying = 0, TextReader? person = null)
    {
        ArgumentNullException.ThrowIfNull(lesson);
        ArgumentNullException.ThrowIfNull(printed);
        ArgumentOutOfRangeException.ThrowIfLessThan(passes, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(tellings);
        ArgumentOutOfRangeException.ThrowIfNegative(revising);
        ArgumentOutOfRangeException.ThrowIfNegative(clarifying);

        if (clarifying > 0 && person is null)
            throw new ArgumentNullException(nameof(person),
                "a clarifying window with nobody at the keyboard would read end-of-stream and "
                + "close on its first line, which is a window that reads as having run");

        _lesson = lesson;
        _passes = passes;
        _tellings = tellings;
        _revising = revising;
        _clarifying = clarifying;
        _person = person;
        _printed = new Watched(printed);
        _put = new int[passes];
        _confirmed = new int[passes];
    }

    /// <summary>Where a session should print, so that a prompt can be seen.</summary>
    public TextWriter Printed => _printed;

    /// <summary>The most words any one statement has.</summary>
    /// <remarks>
    /// <b>What a run has to budget for where a statement is several moments.</b>
    /// <see cref="Asserting.Everything"/> makes a sentence one moment a word, so a round count
    /// taken off <see cref="Moments"/> alone would stop the run before the examination.
    /// </remarks>
    public int Longest => _lesson.Statements.Count == 0
        ? 1
        : _lesson.Statements.Max(one => Math.Max(1, Babi.Words(one).Count));

    /// <summary>How many moments the whole lesson is, which is how long a run has to be.</summary>
    public int Moments =>
        (_lesson.Statements.Count * _tellings) + (_lesson.Revisions.Count * _revising)
        + _clarifying + (_lesson.Exam.Count * _passes);

    /// <summary>What closes the clarifying window early, typed on a line of its own.</summary>
    public const string Done = ".done";

    /// <summary>How many times it was asked about something nobody could answer.</summary>
    public int Shrugged { get; private set; }

    /// <summary>How many times it corrected a guess.</summary>
    public int Corrected { get; private set; }

    /// <summary>How many examination questions were put on each pass.</summary>
    public IReadOnlyList<int> Put => _put;

    /// <summary>How many of each pass's guesses were right.</summary>
    public IReadOnlyList<int> Confirmed => _confirmed;

    /// <summary>
    /// How many examination answers the freshest mention would have got right, over one pass.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The no-learning ceiling, and it is taken before any arm is read.</b> Every wrong turn
    /// on this world came from reading a grid before the bar it had to beat: a rise off a world
    /// where the freshest word is always the answer is a front end reaching a shortcut rather
    /// than a machine that learnt.
    /// </para>
    /// <para>
    /// <b>The rule is the latest answer-word in a statement sharing a key</b>, where a key is a
    /// word that is not in every statement. It needs no learning, no population and no
    /// settlement, so anything at or below it is a reading about the lesson rather than about
    /// the machine.
    /// </para>
    /// </remarks>
    public int Recency
    {
        get
        {
            var statements = _lesson.Statements
                .Select(one => new HashSet<string>(Babi.Words(one), StringComparer.Ordinal))
                .ToList();

            if (statements.Count == 0) return 0;

            // A key is a word that is not in every statement, which is the background rule the
            // front end already stands on. Nothing here knows what a noun is.
            var background = new HashSet<string>(statements[0], StringComparer.Ordinal);

            foreach (var one in statements) background.IntersectWith(one);

            var answers = new HashSet<string>(
                _lesson.Exam.Select(one => one.Answer), StringComparer.Ordinal);

            var right = 0;

            foreach (var asked in _lesson.Exam)
            {
                var keys = new HashSet<string>(
                    Babi.Words(asked.Question), StringComparer.Ordinal);

                keys.ExceptWith(background);

                string? freshest = null;

                for (var back = statements.Count - 1; back >= 0 && freshest is null; back--)
                {
                    if (!statements[back].Overlaps(keys)) continue;

                    foreach (var word in Babi.Words(_lesson.Statements[back]))
                        if (answers.Contains(word)) freshest = word;
                }

                if (string.Equals(freshest, asked.Answer, StringComparison.Ordinal)) right++;
            }

            return right;
        }
    }

    /// <summary>How many the commonest answer alone would have got right, over one pass.</summary>
    /// <remarks>
    /// <b>The other bar, and a skewed column raises it for free.</b> A lesson whose answers are
    /// evenly spread has nothing to hand a machine that says the same word every time, which is
    /// what makes an accuracy on it worth reading.
    /// </remarks>
    public int Marginal => _lesson.Exam.Count == 0
        ? 0
        : _lesson.Exam
            .GroupBy(one => one.Answer, StringComparer.Ordinal)
            .Max(group => group.Count());

    /// <inheritdoc/>
    /// <remarks>
    /// <b>A prompt or a turn to speak</b>, told apart by whether the last thing printed was
    /// closed. That is the whole protocol, and it is deliberately the one a person at a terminal
    /// has rather than a flag passed out of band.
    /// </remarks>
    public override string? ReadLine()
    {
        var tail = _printed.Line;

        return tail.StartsWith("  ? ", StringComparison.Ordinal)
            ? Replying(tail[4..].Trim())
            : Saying();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing) _printed.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>What to say back to <c>? word</c>.</summary>
    private string Replying(string guessed)
    {
        // Their question to answer, not the script's. A machine that asked during the window
        // is asking the person, and what they say is the settlement.
        if (_theirs) return _person!.ReadLine() ?? string.Empty;

        if (_answer is null)
        {
            Shrugged++;

            return string.Empty;
        }

        if (string.Equals(guessed, _answer, StringComparison.Ordinal))
        {
            _confirmed[_asking]++;

            return "yes";
        }

        Corrected++;

        return _answer;
    }

    /// <summary>The next thing the lesson has to say, or nothing where it is finished.</summary>
    private string? Saying()
    {
        if (_told < _lesson.Statements.Count * _tellings)
        {
            _answer = null;

            return _lesson.Statements[_told++ % _lesson.Statements.Count];
        }

        if (_revised < _lesson.Revisions.Count * _revising)
        {
            _answer = null;

            return _lesson.Revisions[_revised++ % _lesson.Revisions.Count];
        }

        if (_clarified < _clarifying)
        {
            if (!_theirs)
            {
                _theirs = true;

                _printed.WriteLine();
                _printed.WriteLine(
                    $"  (your turn — up to {_clarifying} more lines, `{Done}` to move on to the "
                    + "questions)");
            }

            var line = _person!.ReadLine();

            if (line is not null && !string.Equals(line.Trim(), Done, StringComparison.Ordinal))
            {
                _clarified++;
                _answer = null;

                return line;
            }

            // Closed, either by the person or by the stream ending. The budget is spent so the
            // window cannot re-open, and the run's spare rounds go by empty.
            _clarified = _clarifying;
            _theirs = false;

            _printed.WriteLine("  (the questions)");
            _printed.WriteLine();
        }

        if (_pass >= _passes) return null;

        var asked = _lesson.Exam[_at];

        _answer = asked.Answer;
        _asking = _pass;
        _put[_pass]++;

        if (++_at >= _lesson.Exam.Count)
        {
            _at = 0;
            _pass++;
        }

        return asked.Question;
    }

    /// <summary>A writer that forwards everything and remembers the line still open.</summary>
    /// <remarks>
    /// <b>What lets one tutor drive a terminal and a test alike.</b> A prompt is a line with no
    /// newline after it, so knowing whether a reply is wanted means watching what was written —
    /// and a session that told the tutor out of band which read was which would be a session no
    /// person could take the tutor's place in.
    /// </remarks>
    private sealed class Watched(TextWriter inner) : TextWriter
    {
        private readonly StringBuilder _line = new();

        public string Line => _line.ToString();

        public override Encoding Encoding => inner.Encoding;

        public override void Write(char value)
        {
            if (value == '\n') _line.Clear();
            else if (value != '\r') _line.Append(value);

            inner.Write(value);
        }

        public override void Flush() => inner.Flush();

        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();

            base.Dispose(disposing);
        }
    }
}
