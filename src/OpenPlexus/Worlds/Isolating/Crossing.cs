using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>
/// How the crossing world is set up. Every number named and none defaulted.
/// </summary>
public sealed record CrossingSettings
{
    /// <summary>How many words there are, taken from the front of the vocabulary.</summary>
    /// <remarks>
    /// <b>Sixteen is what a conjunction can carry</b>, and `LetteringTests` is where that
    /// number comes from: a sound scope over the codes that survive an offset exists for most
    /// of sixteen words, for a falling share of thirty-two, and for fewer words in absolute
    /// terms at sixty-four. A wider world here would read a front end's ceiling as a learner
    /// failing to bind, which is the one confusion this world exists to avoid.
    /// </remarks>
    public required int Words { get; init; }

    /// <summary>How many distinct facts a word can have.</summary>
    /// <remarks>
    /// <b>What the crossing is asked FOR</b>, and the denominator of its chance bar. A fact
    /// is a bare code with no structure, because what is being measured is whether the shape
    /// reaches it at all rather than anything about what a fact is.
    /// </remarks>
    public required int Facts { get; init; }

    /// <summary>How many pixels apart two drawings of a word are.</summary>
    /// <remarks>
    /// <b>The world's dial and not the front end's.</b> How far a thing moves is a fact about
    /// the world; how finely the movement is coded is a fact about the translation, and
    /// putting the second here would let a world decide how the brain perceives.
    /// </remarks>
    public required int Stride { get; init; }

    /// <summary>
    /// Draw a word that is not the one being named beside it.
    /// </summary>
    /// <remarks>
    /// <b>The control, and it destroys the DATA rather than the code.</b> Every mechanism
    /// runs identically and only the structure the world contains goes: a shape then stands
    /// beside a symbol it has nothing to do with. A crossing score that survives this was
    /// never coming from binding.
    /// </remarks>
    public bool Scrambled { get; init; }

    /// <summary>How many questions of each kind are held back and never drawn.</summary>
    /// <remarks>
    /// <b>Of EACH kind, because the two exams are read against each other.</b> One asks for
    /// the symbol and one for the fact, both at an offset the world never draws, so the only
    /// thing between them is whether the answer is in the sense that was shown.
    /// </remarks>
    public required int Asked { get; init; }
}

/// <summary>
/// One moment of the crossing world: what was drawn, and what was said.
/// </summary>
/// <remarks>
/// <para>
/// <b>One frame carrying both senses</b>, which is what a moment is. A shape arriving on one
/// stream and a symbol on another would never co-fire, and co-firing is the whole of what
/// this world is built to present — see <see cref="Codes.Compound{TFrame}"/>.
/// </para>
/// <para>
/// <b>The shape is nullable and the saying is not</b>, because a moment may be words alone —
/// a fact told about a word nobody is looking at — and never pixels alone. Every moment here
/// says at least which sense is being asked about.
/// </para>
/// </remarks>
public readonly record struct Crossed
{
    /// <summary>The word as it was drawn, or nothing where none was.</summary>
    public IReadOnlyList<double>? Shape { get; init; }

    /// <summary>What was said in symbols: a word, a fact, and which sense is asked.</summary>
    public required IReadOnlyCollection<Code> Said { get; init; }
}

/// <summary>
/// A word seen and a word read, and a fact never shown beside the seeing — <b>fork 107.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The crossing a camera asks, where ground truth is still enumerable.</b> A drawn word
/// and a written one are two attributes of one thing, which is THE ARCHITECTURE's third
/// entry; a camera poses that and takes soundness, overshoot and hard-round coverage with it,
/// because none of those survives a world that cannot be enumerated. Here the world knows
/// exactly which word it drew and which fact belongs to it.
/// </para>
/// <para>
/// <b>Two occasions, and a shape is never shown beside a fact.</b> One shows the drawing and
/// the word and asks which word; the other shows the word and a fact and asks the fact. That
/// absence is the experiment and it is enforced here rather than left to a caller.
/// </para>
/// <para>
/// <b>And two exams that differ in exactly one thing.</b> Both draw a word at an offset the
/// stream never uses. <see cref="Moved"/> asks which WORD it is, which needs the shape sense
/// to survive a position it has not seen. <see cref="Withheld"/> asks the word's FACT, which
/// needs that AND the crossing. A crossing score is unreadable without the first beside it,
/// because a shape that does not survive the offset fails both and looks like a binding
/// failure in the second.
/// </para>
/// <para>
/// <b>What the crossing exam is up against, said plainly</b>: the fact is reachable from the
/// symbol and the symbol is not in the moment, so answering it wants either a name minted
/// over the co-firing shape and symbol, or a conclusion put back into the moment it was
/// reached in. The first is rung five and the second is fork 28's horizon. A nought here is
/// therefore a reading on those rather than news, and the world is built to say which.
/// </para>
/// </remarks>
public sealed class Crossing : IWorld<Crossed>, IWithholds<Crossed>
{
    /// <summary>The drawn word, which a front end has to make symbols out of.</summary>
    /// <remarks>
    /// <b>Carried as pixels rather than as codes</b>, so this is the one world here that can
    /// say whether a quantiser works. A world handing over codes it made itself would be
    /// measuring the learner and the front end not at all.
    /// </remarks>
    public const byte Shape = 130;

    /// <summary>The word as a symbol, which is what a text front end emits.</summary>
    public const byte Symbol = 131;

    /// <summary>Something known about a word, and never shown beside its shape.</summary>
    public const byte Fact = 132;

    /// <summary>
    /// Which sense the question is about — <b>ostension, and the plan calls it the signal.</b>
    /// </summary>
    /// <remarks>
    /// <b>It says what is being looked at and never what to conclude.</b> A moment holding a
    /// drawing and a word has two things it could be asked and no way to say which, so
    /// without this every arm would be scored on a question nobody put. What nothing here
    /// says is which code answers it.
    /// </remarks>
    public const byte Asks = 133;

    private readonly CrossingSettings _settings;
    private readonly Random _rng;

    /// <summary>Draws the examinations, and <b>nothing else</b>.</summary>
    /// <remarks>
    /// <b>Its own stream</b>, so holding more back does not move the task's draws. Building
    /// the exams off the main generator would make how many were held decide which words the
    /// run then saw, and two arms differing only in <see cref="CrossingSettings.Asked"/>
    /// would be two different worlds.
    /// </remarks>
    private readonly Random _quizzing;

    private readonly List<(int Across, int Down)> _places = [];
    private readonly (int Across, int Down) _kept;
    private readonly List<Turn<Crossed>> _crossing = [];
    private readonly List<Turn<Crossed>> _moved = [];

    public Crossing(CrossingSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Words, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Words, Lettering.Vocabulary.Count);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Facts, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Stride, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Asked);

        _settings = settings;
        _rng = new Random(seed);
        _quizzing = new Random(Seeds.Apart(seed, 0xC205_5111));

        var (room, drop) = Lettering.Room(Lettering.Vocabulary[0].Length);

        for (var across = 0; across <= room; across += settings.Stride)
            for (var down = 0; down <= drop; down += settings.Stride)
                _places.Add((across, down));

        // The one place the stream never draws, and both exams use it. A held-out POSITION
        // rather than a held-out word, because holding a word back would leave its fact
        // unlearnable for a second reason and the two would be inseparable.
        if (_places.Count < 2)
            throw new ArgumentException(
                "a stride this wide leaves one place to draw at, so no offset can be held "
                + "back and the exams would be rounds the stream had already drawn",
                nameof(settings));

        _kept = _places[^1];
        _places.RemoveAt(_places.Count - 1);

        for (var back = 0; back < settings.Asked; back++)
        {
            var word = _quizzing.Next(settings.Words);

            _crossing.Add(Exam(word, Fact));
            _moved.Add(Exam(word, Symbol));
        }
    }

    /// <summary>How many places the stream draws a word at.</summary>
    public int Places => _places.Count;

    /// <summary>Which fact belongs to a word. <b>Fixed by the word and never drawn.</b></summary>
    /// <remarks>
    /// <b>Derived rather than sampled</b>, so two machines given the same settings agree about
    /// what is true. A table filled by a generator would make the world's content depend on
    /// how many rounds had been run before anybody asked.
    /// </remarks>
    private int FactOf(int word) => word % _settings.Facts;

    /// <summary>Every code either symbol sense can produce, which is the answer alphabet.</summary>
    /// <remarks>
    /// <b>One alphabet over both, so answering in the wrong sense is expressible.</b> An
    /// outcome space per sense would make the commonest failure this world can have —
    /// naming a word where a fact was asked — impossible to say, and a score cannot report
    /// what it cannot represent.
    /// <para>
    /// <b>The shape sense is never an answer</b>, because answering it means producing a SET
    /// of patch codes and predicting a set is unbuilt. That is a limit on the question and
    /// not on the world.
    /// </para>
    /// </remarks>
    public int Outcomes => _settings.Words + _settings.Facts;

    /// <summary>What a blind guess is worth.</summary>
    public double Chance => 1.0 / Outcomes;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>The crossing exam</b>: a drawing at an unseen offset, and what is that word's fact.
    /// The shape and the fact have never shared a moment, so nothing that fires on the shape
    /// has ever been scored against a fact.
    /// </remarks>
    public IReadOnlyList<Turn<Crossed>> Withheld => _crossing;

    /// <summary>
    /// The other exam: the same drawing at the same unseen offset, and which WORD is it.
    /// </summary>
    /// <remarks>
    /// <b>The diagnostic the crossing cannot be read without.</b> It differs from
    /// <see cref="Withheld"/> in one thing — whether the answer is in the sense that was
    /// shown — so a shape that does not survive an unseen offset scores nought on both, and
    /// only a gap between them is a statement about binding.
    /// </remarks>
    public IReadOnlyList<Turn<Crossed>> Moved => _moved;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Half the moments are a drawing beside its word</b> and half are a word beside its
    /// fact, so both links are told at the same rate and neither is starved by the draw.
    /// </remarks>
    public Turn<Crossed> Next()
    {
        var word = _rng.Next(_settings.Words);

        if (_rng.Next(2) == 0)
        {
            // The binding occasion. The drawing and the word, and the question is which word
            // -- which is present, so a drawn round never rehearses either examination.
            var drawn = _settings.Scrambled ? _rng.Next(_settings.Words) : word;
            var place = _places[_rng.Next(_places.Count)];

            return new Turn<Crossed>
            {
                Seen = new Crossed
                {
                    Shape = Lettering.Draw(Lettering.Vocabulary[drawn], place.Across, place.Down),
                    Said = [new Code(Symbol, (ulong)word), new Code(Asks, Symbol)],
                },
                Outcome = word,
            };
        }

        // The telling occasion. A word and its fact, with no drawing anywhere near it.
        return new Turn<Crossed>
        {
            Seen = new Crossed
            {
                Said =
                [
                    new Code(Symbol, (ulong)word),
                    new Code(Fact, (ulong)FactOf(word)),
                    new Code(Asks, Fact),
                ],
            },
            Outcome = _settings.Words + FactOf(word),
        };
    }

    /// <summary>One examination: a drawing at the offset the stream never uses.</summary>
    /// <param name="word">Which word is drawn.</param>
    /// <param name="asked">Which sense the question is about.</param>
    private Turn<Crossed> Exam(int word, byte asked) =>
        new()
        {
            Seen = new Crossed
            {
                Shape = Lettering.Draw(Lettering.Vocabulary[word], _kept.Across, _kept.Down),
                Said = [new Code(Asks, asked)],
            },
            Outcome = asked == Symbol ? word : _settings.Words + FactOf(word),
        };
}
