using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>Where a conversation is read from, where it is printed, and what it starts knowing.</summary>
/// <remarks>
/// <b>Two streams rather than a console</b>, so the same world is a session at a terminal and a
/// scripted transcript in a test. A world naming <see cref="System.Console"/> could only ever be
/// run by hand, which is a mechanism no check can reach.
/// </remarks>
public sealed record ConversingSettings
{
    /// <summary>Where the human types.</summary>
    public required TextReader Typed { get; init; }

    /// <summary>Where the machine's words are written.</summary>
    public required TextWriter Printed { get; init; }

    /// <summary>Words the outcome alphabet starts with.</summary>
    /// <remarks>
    /// <para>
    /// <b>Empty is allowed and is the honest start</b>, because a machine that has been told
    /// nothing has no vocabulary to answer out of. The alphabet grows as words arrive, so a
    /// seed only decides what can be said before the first line is typed.
    /// </para>
    /// <para>
    /// <b>Append-only, which is what keeps an index meaning one word</b>. An outcome is a
    /// position in this list and a commitment holds that position for its whole life, so a list
    /// that reordered would silently rewrite every rule the machine had learnt.
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> Words { get; init; } = [];
}

/// <summary>
/// A conversation with a human, where every typed line is a moment and the machine has to ask
/// for the settlements it wants.
/// </summary>
/// <remarks>
/// <para>
/// <b>The instrument the corpora cannot be</b>. A read corpus answers whether a population can
/// be grown from text; it cannot answer what happens when the thing supplying the settlements is
/// a person who mostly does not know the answer and mostly will not say. That is the case every
/// deployment of this design is, and no grid reaches it.
/// </para>
/// <para>
/// <b>An outcome is normally absent</b>, which is the whole shape of the thing. John's, and it
/// corrects an earlier design here that demanded a settlement every turn: in the world an outcome
/// cannot be known, so <see cref="Turn{TSeen}.Outcome"/> is nothing unless somebody says one. A
/// round that cannot settle takes no score, no genesis and no repair, so this world shows live
/// that being told things moves no counter.
/// </para>
/// <para>
/// <b>So the machine has to ask, and asking is a doing</b>. John's, and it is the correction that
/// gave this world its shape. A human volunteering an answer to every line is a quiz with the
/// examiner's hand in it; a machine that says <i>is it the kitchen</i> has obtained the
/// settlement rather than been handed it. <see cref="Doings"/> is twice the vocabulary because
/// asserting a word and asking about it are two different things to do with it, which is what
/// lets a scope name the asking and expect that a settlement follows.
/// </para>
/// <para>
/// <b>Every line is its own moment</b>, which is the first of the three pieces the plan puts on
/// the path to a conversation. A transcript arriving as one moment is a bag of words with no way
/// to say which statement a word came from; a statement that is a moment stands or falls on its
/// own, and the question after it is the next moment from the same source.
/// </para>
/// <para>
/// <b>What is predicted is a set</b>, and what is done is a set, so a sentence written out and
/// a motor moving are one shape. That is why this is acted in rather than watched, and
/// <see cref="IActed{TSeen}.Do"/> is where the world is told which word was chosen.
/// </para>
/// <para>
/// <b>It is an instrument and not a score</b>. It will read badly, and it is built to be read
/// rather than to pass — what it says is which piece of the path bites first.
/// </para>
/// </remarks>
public sealed class Conversing : IWorld<Recited>, IActed<Recited>
{
    /// <summary>What ends a session, typed on a line of its own.</summary>
    public const string Over = ".quit";

    private readonly ConversingSettings _settings;

    private readonly List<string> _vocabulary = [];
    private readonly Dictionary<string, int> _index = new(StringComparer.Ordinal);
    private readonly Dictionary<Code, int> _naming = [];

    // The topic so far, oldest first, and reversed on the way into a moment. `Recited` promises
    // newest first; building it that way round would be an insert at the front of a list for
    // every line typed.
    private readonly List<IReadOnlyList<Code>> _said = [];

    private Recited? _pending;
    private int? _settled;
    private Spoke _did;

    /// <summary>What the machine did with the last moment it was shown.</summary>
    /// <remarks>
    /// <b>Three states rather than a flag</b>, because staying quiet is a choice with a cost
    /// here. A machine that never asks learns nothing and a machine that always asks is a
    /// machine nobody will talk to twice.
    /// </remarks>
    private enum Spoke
    {
        /// <summary>Let the line go by.</summary>
        Nothing,

        /// <summary>Said a word out loud as a claim.</summary>
        Claim,

        /// <summary>Asked whether a word was the answer.</summary>
        Question,
    }

    /// <summary>The outcome meaning nobody knew.</summary>
    /// <remarks>
    /// <para>
    /// <b>A shrug is an arrival and not a silence</b>, which is what makes it settleable. C2
    /// forbids a miss decided by a deadline because late and absent cannot be told apart; a
    /// human who was asked and said they did not know has said something, and the machine was
    /// there when they said it.
    /// </para>
    /// <para>
    /// <b>And it is what makes asking learnable</b>. An ask that got nothing used to settle
    /// nothing, so a wasted question and a silence scored alike and no scope could ever come to
    /// expect that asking here pays. With this the machine is wrong about something when it
    /// asks a question nobody can answer, which is the whole of what it eats.
    /// </para>
    /// <para>
    /// <b>Nought, and no typed word ever reaches it</b>. It is seeded before anything is heard
    /// and is kept out of the word index, so nothing the human types names it and a blind
    /// question can never be about it.
    /// </para>
    /// </remarks>
    public const int Nothing = 0;

    /// <param name="settings">Where the conversation is read from and printed to.</param>
    public Conversing(ConversingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;

        // First, so it is outcome nought whatever else arrives. It goes into the vocabulary and
        // into neither map, which is what keeps it unreachable from anything typed.
        _vocabulary.Add("(nobody knew)");

        foreach (var word in settings.Words) Heard(word);
    }

    /// <summary>Whether the human has finished.</summary>
    /// <remarks>
    /// <b>The end of the typing rather than the end of the run</b>. A bench is driven by a round
    /// count and has no way to be told to stop, so whoever composed the session reads this and
    /// reports how much of its budget the conversation actually used.
    /// </remarks>
    public bool Ended { get; private set; }

    /// <summary>How many times the machine has asked something.</summary>
    public long Asked { get; private set; }

    /// <summary>How many of those asks the human answered.</summary>
    /// <remarks>
    /// <b>Printed beside <see cref="Asked"/> rather than folded into it</b>, because the gap
    /// between them is a reading nothing else here can take. A question the human declined is a
    /// round the machine spent its one chance to learn on and got nothing back.
    /// </remarks>
    public long Told { get; private set; }

    /// <summary>How many of its asks got <see cref="Nothing"/> back.</summary>
    /// <remarks>
    /// <b>The cost of asking</b>, and it is a settlement rather than a waste. A machine that
    /// keeps asking questions nobody can answer is a machine being told, over and over, that it
    /// is asking in the wrong place.
    /// </remarks>
    public long Shrugged { get; private set; }

    /// <summary>How many times it expected that nobody would know, and said nothing.</summary>
    public long Declined { get; private set; }

    /// <summary>How many lines the machine let go by without saying anything.</summary>
    /// <remarks>
    /// <b>The control's other half</b>. A chooser that asks about everything and one that asks
    /// about nothing both read as a machine that is talking, and only this separates them.
    /// </remarks>
    public long Quiet { get; private set; }

    /// <summary>Every word heard so far, in the order the outcome index numbers them.</summary>
    public IReadOnlyList<string> Vocabulary => _vocabulary;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>The words heard so far, which is nought before anything is said</b>. A conversation has
    /// no answer alphabet handed to it up front, so the chance bar this divides into moves as the
    /// vocabulary grows and is only worth reading at the end.
    /// </remarks>
    public int Outcomes => _vocabulary.Count;

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>Twice the vocabulary, because a word can be asserted or asked about</b>. Nothing at all
    /// is the third option and it is the absence of a doing rather than a doing, which
    /// <see cref="IActed{TSeen}.Do"/> already expresses.
    /// </para>
    /// <para>
    /// <b>And the pairing is the point rather than an encoding</b>. <see cref="Asserts"/> and
    /// <see cref="Asks"/> name the two, a moment carries which one was done as an ordinary code,
    /// and a scope may therefore say <i>asking about this pays</i>. A machine whose only route to
    /// a settlement is a hand-written rule about when to ask has had the interesting half
    /// written for it.
    /// </para>
    /// </remarks>
    public int Doings => 2 * _vocabulary.Count;

    /// <summary>Saying a word out loud as a claim.</summary>
    /// <param name="word">Where the word sits in <see cref="Vocabulary"/>.</param>
    public static int Asserts(int word)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(word);
        return 2 * word;
    }

    /// <summary>Asking whether a word is the answer.</summary>
    /// <param name="word">Where the word sits in <see cref="Vocabulary"/>.</param>
    public static int Asks(int word)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(word);
        return (2 * word) + 1;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Reading the next line is what this does</b>, so it moves the conversation on in the
    /// sense that a lazily opened walk does. What it must not do is take a second line for one
    /// round, which is why the line read here is the one <see cref="Next"/> hands back.
    /// </remarks>
    public Recited Now => _pending ??= Read();

    /// <inheritdoc/>
    /// <param name="doing">
    /// An <see cref="Asserts"/> or an <see cref="Asks"/>, or nothing to stay quiet.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>Printed in words, because a hash goes nowhere back</b>. A session reporting that it
    /// answered four questions in five and never saying which is a score with no way to be
    /// embarrassed by it.
    /// </para>
    /// <para>
    /// <b>Only an ask stops for a reply</b>. An assertion is the machine talking and the human
    /// may ignore it; a question waits, and the answer to it is the one settlement this world
    /// ever produces.
    /// </para>
    /// </remarks>
    public void Do(int? doing)
    {
        _settled = null;
        _did = Spoke.Nothing;

        if (Ended) return;

        if (doing is not { } chosen)
        {
            Quiet++;
            return;
        }

        ArgumentOutOfRangeException.ThrowIfNegative(chosen, nameof(doing));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(chosen, Doings, nameof(doing));

        var word = _vocabulary[chosen / 2];

        // The machine expecting that nobody knows is the machine deciding not to ask, and it is
        // the point of making a shrug an outcome. A question about `(nobody knew)` is not a
        // question, so this is the one place a doing turns back into staying quiet -- learnt
        // rather than wired, because what put it there was being wrong on the statements.
        if (chosen / 2 == Nothing)
        {
            Declined++;
            Quiet++;

            return;
        }

        if (chosen % 2 == 0)
        {
            _did = Spoke.Claim;
            _settings.Printed.WriteLine($"  . {word}");

            return;
        }

        _did = Spoke.Question;
        Asked++;

        _settings.Printed.Write($"  ? {word} ");
        _settings.Printed.Flush();

        var told = _settings.Typed.ReadLine();

        // The prompt is closed whatever came back, and leaving it open was a real defect rather
        // than a cosmetic one. A prompt is a line with no newline after it, which is the only
        // thing that separates asking for a reply from saying something -- so an unclosed one
        // makes the very next read look like a second ask, and the human answers twice while
        // the conversation stands still.
        _settings.Printed.WriteLine();

        // And a reply may end the session, which is not a courtesy. A reply is read from the
        // same stream the conversation is, so a session that only honoured this where it
        // expected a statement would be a session that could not be left while it was asking.
        if (told is null || string.Equals(told.Trim(), Over, StringComparison.Ordinal))
        {
            Ended = true;
            return;
        }

        _settled = Answering(told, word);

        if (_settled == Nothing) Shrugged++;
        else if (_settled is not null) Told++;
    }

    /// <summary>What a reply to <c>? word</c> settles on.</summary>
    /// <remarks>
    /// <para>
    /// <b>Yes settles on the word that was asked about</b>, which is what makes a question worth
    /// asking rather than worth answering in full. A human confirming is cheaper than a human
    /// dictating, and the machine gets the same code either way.
    /// </para>
    /// <para>
    /// <b>No settles on nothing</b>, and that is deliberate rather than a gap. A refusal says the
    /// answer is not this word and says nothing about what it is; the counters here are monotone
    /// and there is no way to record a negative, so recording one would be inventing evidence.
    /// Fork <b>30</b> is where that is answered and it is a rung rather than a world.
    /// </para>
    /// <para>
    /// <b>Anything else is taken as the answer</b>, so a human who knows may simply say it. The
    /// first word only, exactly as a corpus answer is read — a reply longer than an outcome can
    /// hold is wrong out loud rather than wrong quietly.
    /// </para>
    /// </remarks>
    private int? Answering(string told, string word)
    {
        var words = Babi.Words(told);

        // Nobody knew, which is a settlement rather than the absence of one.
        if (words.Count == 0) return Nothing;

        if (string.Equals(words[0], "yes", StringComparison.Ordinal)) return Heard(word);

        // And a refusal is neither. `No` says the answer is not this word and says nothing about
        // what it is, and the counters here are monotone with no way to record a negative -- so
        // recording one would be inventing evidence. Fork **30** is where that is answered.
        if (string.Equals(words[0], "no", StringComparison.Ordinal)) return null;

        return Heard(words[0]);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>The line <see cref="Now"/> read, with whatever the asking got back</b>. The two are one
    /// round: a session that read a second line here would be settling a moment the machine was
    /// never shown.
    /// </para>
    /// <para>
    /// <b>And what the machine did is in the moment</b>, as its own statement. That is what makes
    /// asking nameable: a scope holding <i>asked</i> can expect that a settlement follows, so
    /// whether asking pays is something the population can learn rather than something a chooser
    /// was told. The doing is marked assigned as well as said, which is how
    /// <see cref="Codes.Intervened"/> tells <c>do(x)</c> from <c>x</c>.
    /// </para>
    /// <para>
    /// <b>The word it asked about is deliberately not in there</b>, and leaving it out is the
    /// whole reason this is written down. A confirmation settles on the word that was asked, so
    /// a moment carrying both would hold its own answer — genesis would mint <i>I asked about
    /// this and the answer was this</i>, which is right on every confirmed round, useless, and
    /// well placed to outvote every rule that says something. Whether a guess can be in the
    /// moment it is then scored on is fork <b>117</b>.
    /// </para>
    /// </remarks>
    public Turn<Recited> Next()
    {
        var moment = _pending ?? Read();

        _pending = null;

        var outcome = _settled;

        _settled = null;

        var spoke = _did switch
        {
            Spoke.Claim => Said(["said"]),
            Spoke.Question => Said(["asked"]),
            _ => null,
        };

        _did = Spoke.Nothing;

        if (spoke is null) return new Turn<Recited> { Seen = moment, Outcome = outcome };

        return new Turn<Recited>
        {
            Seen = new Recited
            {
                Said = [spoke, .. moment.Said],
                Asked = moment.Asked,
                Assigned = new HashSet<Code>(spoke),
            },
            Outcome = outcome,
        };
    }

    /// <summary>The next line, as a moment.</summary>
    /// <remarks>
    /// <para>
    /// <b>A blank line starts a new topic</b>, which is the corpus's story boundary typed by
    /// hand. Nothing about the machine changes at one; what changes is which words the world says
    /// are in front of it.
    /// </para>
    /// <para>
    /// <b>A line ending in a question mark is a question</b>, and everything else is a statement.
    /// That is the whole grammar, and it is a fact about the signal rather than a conclusion —
    /// the same licence a corpus has to say which of its lines were asked.
    /// </para>
    /// </remarks>
    private Recited Read()
    {
        while (true)
        {
            var line = _settings.Typed.ReadLine();

            if (line is null || string.Equals(line.Trim(), Over, StringComparison.Ordinal))
            {
                Ended = true;

                // Nothing said, rather than the last thing said again. A world with nobody
                // typing at it has no moment to push, and repeating one would have the machine
                // answering a question it has already been settled on.
                return new Recited { Said = [], Asked = [] };
            }

            var text = line.Trim();

            if (text.Length == 0)
            {
                _said.Clear();
                _settings.Printed.WriteLine("  (new topic)");
                continue;
            }

            var words = Babi.Words(text);

            if (words.Count == 0) continue;

            foreach (var word in words) Heard(word);

            var coded = Said(words);
            var asking = text.EndsWith('?');

            if (!asking) _said.Add(coded);

            var before = new List<IReadOnlyList<Code>>();

            for (var one = _said.Count - 1; one >= 0; one--) before.Add(_said[one]);

            return new Recited { Said = before, Asked = asking ? coded : [] };
        }
    }

    /// <summary>Which outcome a code stands for, or nothing where it stands for none.</summary>
    /// <param name="code">A code from a moment.</param>
    /// <remarks>
    /// <para>
    /// <b>What a machine with no rules needs</b>, in order to ask anything at all. A question is
    /// about a word, and until something fires there is no word the population can offer — so a
    /// chooser that could only ask about its own expectations could never ask, never settle,
    /// never mint, and never come to have an expectation. That is a bootstrap lock and it is
    /// measured rather than supposed: the first build of this world spent eight hundred rounds
    /// with nothing to say.
    /// </para>
    /// <para>
    /// <b>Handed to a chooser rather than read by one</b>, the way <c>Drives</c> takes the
    /// mapping from a code to a doing. A chooser naming this world would put one world's
    /// vocabulary in front of every other one.
    /// </para>
    /// </remarks>
    public int? Naming(Code code) => _naming.TryGetValue(code, out var at) ? at : null;

    /// <summary>Where a word sits in the outcome alphabet, adding it if it is new.</summary>
    private int Heard(string word)
    {
        if (_index.TryGetValue(word, out var at)) return at;

        _index[word] = at = _vocabulary.Count;
        _naming[Babi.Of(word)] = at;
        _vocabulary.Add(word);

        return at;
    }

    /// <summary>One line as codes.</summary>
    /// <remarks>
    /// <b>The same code a corpus gives the same word</b>, which is what lets a primer and a
    /// conversation be one vocabulary. A world minting its own word codes would leave whatever it
    /// had been read unreachable from anything typed at it, and the two would be two machines
    /// wearing one name.
    /// </remarks>
    private static IReadOnlyList<Code> Said(IReadOnlyList<string> words) =>
        [.. words.Select(Babi.Of)];
}
