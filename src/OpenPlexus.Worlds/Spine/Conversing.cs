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

    /// <summary>How much of the topic a moment holds.</summary>
    /// <remarks>
    /// <b>An arm rather than a setting</b>, because what a moment holds is the whole of what
    /// this world can be asked. It is a fact about what was shown and not about how to think,
    /// which is the line a world's own dials stay on.
    /// </remarks>
    public Carrying Carrying { get; init; } = Carrying.Always;

    /// <summary>What a told statement claims, so that being told can be wrong.</summary>
    /// <remarks>
    /// <b>An arm, and John's.</b> A statement that settles nothing takes no score, no genesis
    /// and no repair, so telling one moves no counter at all.
    /// </remarks>
    public Asserting Asserting { get; init; } = Asserting.Nothing;

    /// <summary>How many doings this world will take about one moment.</summary>
    /// <remarks>
    /// <para>
    /// <b>An arm, and the reason it is more than one</b>. A machine that speaks once about a
    /// moment cannot ask, be refused, and ask again, so a refusal costs it nothing and every
    /// reading of whether asking pays was taken on a machine that got one go.
    /// </para>
    /// <para>
    /// <b>Three because the gain saturates there</b>, and <c>ConversingTests</c> holds the
    /// grid. Against a human who never dictates an answer, one doing settles 0.649 of the
    /// exchanges and three settles 1.485; five reads 1.407, which is inside the spread of
    /// three. Against a human who answers every wrong guess the four cells are identical to
    /// the digit, because a settled round stops listening.
    /// </para>
    /// <para>
    /// <b>A ceiling, so a quiet chooser is not made to fill it.</b> The machine stops where it
    /// runs out of things to say, and what this decides is how long the human will sit there.
    /// </para>
    /// </remarks>
    public int Budget { get; init; } = 3;

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

    /// <summary>Which of the words typed at it name a THING.</summary>
    /// <remarks>
    /// <para>
    /// <b>Empty is the honest default, because a conversation cannot segment itself.</b> A
    /// world knows which object it drew a code for and a text world reading typed lines knows
    /// no such thing — so this is filled where whoever is typing can say, which is a lesson,
    /// and left empty where a person is at the keyboard. The same standing as a scene world
    /// reporting its objects.
    /// </para>
    /// <para>
    /// <b>And a statement is not one of them</b>, which is the whole reason this exists.
    /// <i>The cat covering is fur</i> is about the cat, so a moment of it reports the cat and
    /// leaves every other word in no part — where a statement reported as a part made a scope
    /// over the whole sentence read as being about one thing.
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> Things { get; init; } = [];
}

/// <summary>What of the topic so far a moment holds, beside the sentence it is.</summary>
/// <remarks>
/// <para>
/// <b>John's, and it is the question of whether anything is remembered.</b> Under
/// <see cref="Always"/> a question arrives with the story attached, so answering it is a lookup
/// over what is in front of the machine and nothing has to have persisted. Under the other two
/// the story is gone by the time the question is put, and what is left is whatever reading the
/// statements changed.
/// </para>
/// <para>
/// <b>The population is the only thing that crosses a moment</b>, which is what makes the
/// reading interpretable. <see cref="Codes.Joining.Resolved"/> looks like a memory and is
/// recomputed from the story inside the moment, so it dies with the history rather than
/// surviving it.
/// </para>
/// </remarks>
public enum Carrying
{
    /// <summary>Every moment holds the topic so far.</summary>
    /// <remarks>
    /// <b>The control, and every earlier reading on this world is here.</b> A question re-hands
    /// the whole story, so the machine is never asked to have remembered anything.
    /// </remarks>
    Always,

    /// <summary>A statement holds the topic so far and a question holds only itself.</summary>
    /// <remarks>
    /// <b>John's shape.</b> The telling accumulates, so a statement is read against what came
    /// before it; the examination arrives bare, so nothing in it says where anybody is.
    /// </remarks>
    Statements,

    /// <summary>Every moment holds only its own sentence.</summary>
    /// <remarks>
    /// <b>The far end, and the honest form of one statement one moment.</b> Nothing ever sees
    /// two sentences at once, so every relation across them has to be something the population
    /// came to hold.
    /// </remarks>
    Never,
}

/// <summary>What a statement asserts, which is what makes being told falsifiable.</summary>
/// <remarks>
/// <para>
/// <b>What it is told must be falsifiable</b>, which is the architecture's own line and the one
/// a conversation had no answer to. Told and configured are indistinguishable from the inside,
/// so a fact the machine cannot fail on was installed rather than taught — and a statement that
/// settles nothing is exactly that, measured against its own control in <c>LessonTests</c>.
/// </para>
/// <para>
/// <b>And the answer word stays in the moment</b>, which is settled here rather than open.
/// <i>meow</i> is a word of the statement and <i>the answer is meow</i> is an outcome, and the
/// one-code commitment joining them is what genesis is for. What the statement does NOT hand
/// over is which of its words the next one will be about.
/// </para>
/// </remarks>
public enum Asserting
{
    /// <summary>Nothing, so a statement can only settle where the machine asked.</summary>
    /// <remarks>
    /// <b>The control, and every earlier reading is here.</b> A statement is a round that takes
    /// no score, which is what makes the telling free and also what makes it worthless.
    /// </remarks>
    Nothing,

    /// <summary>Every word in turn, each its own moment with that word left out.</summary>
    /// <remarks>
    /// <para>
    /// <b>John's one-shot, and the honest answer to which word a statement claims.</b> Every
    /// other arm here picks one, and picking needs a count the conversation has not got on
    /// first hearing — every word of a sentence said once has been said once, so the tie goes
    /// to whichever came first and the statement claims its opening word. Not picking removes
    /// the question rather than answering it.
    /// </para>
    /// <para>
    /// <b>A statement becomes as many moments as it has words</b>, which is the cost and is
    /// bounded by the sentence. The human still said it once; the machine does more with it.
    /// A moment carries one outcome, so claiming several means several moments and not a
    /// set — that is a separate question and it is open.
    /// </para>
    /// </remarks>
    Everything,

    /// <summary>Its rarest word so far, left out of the moment as well as claimed.</summary>
    /// <remarks>
    /// <para>
    /// <b>What a WIDE scope needs</b>, and it is the difference between the two claiming arms.
    /// A commitment over the whole moment fires only where every one of its codes is present,
    /// so a moment still holding the claimed word mints a rule that can never fire on a
    /// question — the question does not say the answer.
    /// </para>
    /// <para>
    /// <b>And it closes the tautology the other arm leaves open.</b> With the word still there,
    /// <i>meow predicts meow</i> is mintable, right on every statement round, useless, and well
    /// placed to outvote a rule that says something. That is fork <b>117</b> arriving through
    /// the telling rather than through the asking.
    /// </para>
    /// </remarks>
    Withheld,

    /// <summary>Its rarest word so far, which is what the statement is taken to claim.</summary>
    /// <remarks>
    /// <para>
    /// <b>Rarest rather than every word in turn</b>, which is <c>Predicting.Salient</c>'s rule
    /// and it is here for that rule's reason. Claiming every word spends most of the demand on
    /// <i>the</i> and <i>is</i>, where a bag predicts best and carries least.
    /// </para>
    /// <para>
    /// <b>And frequency is a count rather than a stop list.</b> No parser, tagger or written
    /// word set goes near it — it is how often this conversation has said a word, ties to the
    /// earliest. Counted as it goes, because a world cannot read ahead.
    /// </para>
    /// </remarks>
    Rarest,
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
public sealed class Conversing : IWorld<Coded>, IActed<Coded>
{
    /// <summary>What ends a session, typed on a line of its own.</summary>
    public const string Over = ".quit";

    private readonly ConversingSettings _settings;

    private readonly List<string> _vocabulary = [];
    private readonly Dictionary<string, int> _index = new(StringComparer.Ordinal);
    private readonly Dictionary<Code, int> _naming = [];

    // Which codes name a thing, as the lesson said. Built once from the words rather than
    // asked for a word at a time, because a moment is read far more often than a session is
    // composed.
    private readonly HashSet<Code> _nouns = [];

    // The topic so far, oldest first, and reversed on the way into a moment. `Coded` promises
    // newest first; building it that way round would be an insert at the front of a list for
    // every line typed.
    private readonly List<IReadOnlyList<Code>> _said = [];

    // What is left of the last typed line. A line may hold several sentences and each is its
    // own moment, so the reading of a line and the pushing of a moment are not one call.
    private readonly Queue<string> _sentences = new();

    // The statement now being expanded, as the words and which of them each moment claims.
    // `Everything` makes a sentence several moments, and a moment carries one outcome.
    private readonly Queue<(IReadOnlyList<string> Words, int Claim)> _claims = new();

    private Coded? _pending;
    private int? _settled;

    // What the statement now pending claims, where this world claims anything. Kept beside the
    // moment rather than folded into `_settled`, because a reply and a claim arrive at different
    // times and a statement that was asked about would otherwise settle twice.
    private int? _asserted;

    // How often each word has been said, which is what `Asserting.Rarest` reads. Counted as the
    // conversation goes, because a world cannot read ahead.
    private readonly Dictionary<string, int> _often = new(StringComparer.Ordinal);

    // What the machine did about the moment on the table, as two facts rather than one state.
    // A moment takes several doings, so it may claim AND ask, and an enum with one value at a
    // time would have to pick which of the two happened.
    private bool _claimed;
    private bool _questioned;

    // How many doings this moment has taken, and whether it will take another at all. The
    // count is the human's patience and the flag is everything that ends a moment early: a
    // settlement, because the round carries one outcome and a second answer would be one
    // obtained and thrown away, and a decline, because that is the machine saying it has
    // nothing to say here.
    private int _doings;
    private bool _finished;

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

        foreach (var word in settings.Things) _nouns.Add(Babi.Of(word));
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

    /// <summary>How many of its asks were answered with the word it had guessed.</summary>
    /// <remarks>
    /// <para>
    /// <b>Accuracy on the answerable rounds, taken where it can be taken</b>. A trailing accuracy
    /// over the whole run counts every round where the machine correctly expected that nobody
    /// knew, and half the moments here are ones nobody can answer — so a skewed column raises it
    /// for free, which is this repo's own trap.
    /// </para>
    /// <para>
    /// <b>A confirmation is the guess being right, exactly</b>, which is why this can live on the
    /// world at all. The world never sees a vote; it sees which word it was asked to put and
    /// whether the human said yes to it, and those are the same thing.
    /// </para>
    /// <para>
    /// <b>And a blind ask is in here too</b>, which is the caveat rather than a defect. A
    /// question about a word nothing predicted is a draw and not a guess, so a run whose asks
    /// are mostly blind reads this low for a reason that is not the population's — read it
    /// beside <c>Curiosity.Blind</c>.
    /// </para>
    /// </remarks>
    public long Confirmed { get; private set; }

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

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>While the round is unsettled and the human's patience holds</b>, which is the whole
    /// of what a second doing is for. An ask that is refused settles nothing, so the machine
    /// may name another word; an ask that is answered settles the round, and a moment carries
    /// one outcome — so a third and fourth answer would be settlements obtained and thrown
    /// away, which is worse than never having asked.
    /// </para>
    /// <para>
    /// <b>So this is inert wherever nothing refuses.</b> A human who answers every wrong guess
    /// by saying the answer settles the round on the first ask however large the budget, and
    /// the reading is then the one-doing reading exactly. What the budget buys is bounded by
    /// how often the machine is told <i>no</i> and nothing else.
    /// </para>
    /// </remarks>
    public bool Listening => !_finished && _doings < _settings.Budget;

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
    public Coded Now => _pending ??= Read();

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
        if (Ended)
        {
            _finished = true;
            return;
        }

        if (doing is not { } chosen)
        {
            Quiet++;
            _finished = true;

            return;
        }

        _doings++;

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

            // Declining is the machine saying it has nothing to say about this moment, so it
            // ends the moment rather than costing one of its doings. A machine that declined
            // and then asked anyway would be spending a budget on a moment it had just
            // written off.
            _finished = true;

            return;
        }

        if (chosen % 2 == 0)
        {
            _claimed = true;
            _settings.Printed.WriteLine($"  . {word}");

            return;
        }

        _questioned = true;
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
            _finished = true;

            return;
        }

        var answer = Answering(told, word);

        if (answer == Nothing) Shrugged++;
        else if (answer is not null) Told++;

        // A reply that settled anything ends the moment, so the first settlement is the round's
        // and there is never a second to choose between. Only a refusal leaves the moment open,
        // which is exactly the case a second doing exists for: `no` says the answer is not this
        // word and says nothing about what it is, so the machine may name another.
        if (answer is null) return;

        _settled = answer;
        _finished = true;
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
    /// <b>Anything else is taken as the answer</b>, and a whole sentence is allowed. A person
    /// asked <i>is it fur</i> answers <i>the cat covering is fur</i> as readily as <i>fur</i>,
    /// and reading only the first word of that settles the round on <i>the</i>.
    /// </para>
    /// <para>
    /// <b>So the answer is the reply's last word</b> the QUESTION did not already say. Both
    /// halves of that are about how a reply is read rather than about what text means:
    /// subtracting the question's words is arithmetic over two sets, the licence
    /// <see cref="Codes.Joined"/> already carries, and taking the last of what is left sits
    /// beside <i>yes means the guess was right</i> — a convention of this harness, which never
    /// reaches the learner as a claim about English.
    /// </para>
    /// <para>
    /// <b>And a reply is a fact as well as an answer</b>, which is the half that used to be
    /// thrown away. Somebody who says <i>the cat covering is fur</i> has told the machine
    /// something, so the sentence is queued and arrives as its own moment exactly as a typed
    /// statement would.
    /// </para>
    /// </remarks>
    private int? Answering(string told, string word)
    {
        var words = Babi.Words(told);

        // Nobody knew, which is a settlement rather than the absence of one.
        if (words.Count == 0) return Nothing;

        if (string.Equals(words[0], "yes", StringComparison.Ordinal))
        {
            Confirmed++;

            return Heard(word);
        }

        // And a refusal is neither. `No` says the answer is not this word and says nothing about
        // what it is, and the counters here are monotone with no way to record a negative -- so
        // recording one would be inventing evidence. Fork **30** is where that is answered.
        if (string.Equals(words[0], "no", StringComparison.Ordinal)) return null;

        // Told as well as answered, where there was a sentence to tell. Queued rather than
        // pushed, so it arrives through the one path that makes moments.
        if (words.Count > 1)
            foreach (var sentence in Sentences(told))
                _sentences.Enqueue(sentence);

        return Heard(Answered(words));
    }

    /// <summary>Which word of a reply is the answer to the question on the table.</summary>
    /// <param name="words">The reply, as words.</param>
    /// <remarks>
    /// <b>The last one the question did not already say</b>, and the last word where it said
    /// all of them. A one-word reply shares nothing with the question and reaches this
    /// unchanged, which is what keeps every earlier reading on this world comparable.
    /// </remarks>
    private string Answered(IReadOnlyList<string> words)
    {
        if (_pending is not { Asked: { Codes: { Count: > 0 } asked } }) return words[^1];

        var said = new HashSet<Code>(asked);

        for (var at = words.Count - 1; at >= 0; at--)
            if (!said.Contains(Babi.Of(words[at])))
                return words[at];

        return words[^1];
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
    public Turn<Coded> Next()
    {
        var moment = _pending ?? Read();

        _pending = null;

        // A statement's own claim, where it made one, and the reply otherwise. A claim wins
        // because a reply to an ask about a statement is a shrug, and a shrug arriving after a
        // claim would settle the round on `nobody knew` and throw the telling away.
        var outcome = _asserted ?? _settled;

        _settled = null;
        _asserted = null;

        // The moment's slate, cleared where the moment ends. `Do` may run several times about
        // one of them and must not be the place this happens, which is how the first
        // settlement stays the round's.
        _doings = 0;
        _finished = false;

        // Both, where the moment got both. A machine that claimed and then asked has done two
        // things and a moment saying only one of them would be the world reporting less than
        // happened -- and which one it dropped would be this file's choice rather than the
        // machine's.
        var spoken = new List<string>(2);

        if (_claimed) spoken.Add("said");
        if (_questioned) spoken.Add("asked");

        _claimed = false;
        _questioned = false;

        if (spoken.Count == 0) return new Turn<Coded> { Seen = moment, Outcome = outcome };

        var spoke = Said(spoken);

        return new Turn<Coded>
        {
            Seen = Coded.From(
                [Grouped.Of(spoke), .. moment.Statements ?? []],
                moment.Asked,
                new HashSet<Code>(spoke),
                moment.Things),
            Outcome = outcome,
        };
    }

    /// <summary>The next sentence, as a moment.</summary>
    /// <remarks>
    /// <para>
    /// <b>A sentence is a moment and a typed line may hold several</b>. John's, and it is what
    /// lets a paragraph be pasted in: what arrives is one moment a sentence, in the order they
    /// were written, so each one is read against whatever the ones before it left behind.
    /// A line with no stop in it is one sentence and reaches the machine exactly as it did
    /// before.
    /// </para>
    /// <para>
    /// <b>A blank line starts a new topic</b>, which is the corpus's story boundary typed by
    /// hand. Nothing about the machine changes at one; what changes is which words the world says
    /// are in front of it.
    /// </para>
    /// <para>
    /// <b>A sentence ending in a question mark is a question</b>, and everything else is a
    /// statement. That is the whole grammar, and it is a fact about the signal rather than a
    /// conclusion — the same licence a corpus has to say which of its lines were asked.
    /// </para>
    /// <para>
    /// <b>And what a moment carries beside its own words is <see cref="Carrying"/></b>, so the
    /// question of whether anything was remembered is asked here and answered by a reading.
    /// </para>
    /// </remarks>
    private Coded Read()
    {
        while (true)
        {
            // A statement mid-expansion FIRST, where one sentence is several moments. Reading a
            // new line while claims are still owed advances the source before its own sentence
            // has finished arriving -- and on a scripted source that means the next question is
            // put while the previous statement is still being shown, so its answer is live for
            // moments nobody asked anything about. That is a leak rather than an ordering
            // preference, and it is what `OutstandingTests` caught by arithmetic.
            if (_claims.Count > 0)
            {
                var (said, at) = _claims.Dequeue();

                _asserted = _index[said[at]];

                return Moment(
                    Said([.. said.Where((_, where) => where != at)]), asking: false,
                    coded: null);
            }

            if (_sentences.Count == 0)
            {
                var line = _settings.Typed.ReadLine();

                if (line is null || string.Equals(line.Trim(), Over, StringComparison.Ordinal))
                {
                    Ended = true;

                    // Nothing said, rather than the last thing said again. A world with nobody
                    // typing at it has no moment to push, and repeating one would have the
                    // machine answering a question it has already been settled on.
                    return Coded.From([]);
                }

                var text = line.Trim();

                if (text.Length == 0)
                {
                    _said.Clear();
                    _settings.Printed.WriteLine("  (new topic)");
                    continue;
                }

                foreach (var sentence in Sentences(text)) _sentences.Enqueue(sentence);

                continue;
            }

            var one = _sentences.Dequeue();
            var words = Babi.Words(one);

            if (words.Count == 0) continue;

            foreach (var word in words) Heard(word);

            var coded = Said(words);
            var asking = one.EndsWith('?');

            if (!asking)
            {
                _said.Add(coded);

                foreach (var word in words) _often[word] = _often.GetValueOrDefault(word) + 1;

                // One moment a claim, where the arm claims more than one word of a sentence.
                if (Claims(words) is { Count: > 0 } claims)
                {
                    foreach (var where in claims) _claims.Enqueue((words, where));

                    continue;
                }

                var claim = _settings.Asserting is Asserting.Nothing ? null : Claimed(words);

                _asserted = claim is { } at ? _index[words[at]] : null;

                // Left out where the arm withholds it, so a scope over what is left can fire on
                // a question that names the same things.
                if (claim is { } drop && _settings.Asserting is Asserting.Withheld)
                    coded = Said([.. words.Where((_, where) => where != drop)]);
            }

            return Moment(coded, asking, coded);
        }
    }

    /// <summary>One sentence's codes as a moment, with whatever the arm carries beside it.</summary>
    /// <param name="sentence">The words of this moment.</param>
    /// <param name="asking">Whether it is a question.</param>
    /// <param name="coded">
    /// The whole sentence as told, or nothing where this moment is one of several from it.
    /// </param>
    /// <remarks>
    /// <b>`_said` accumulates whatever the arm carries</b>, so the carrying arms differ in what
    /// a moment holds and in nothing else. A world that stopped accumulating would be two
    /// worlds wearing one name.
    /// </remarks>
    private Coded Moment(IReadOnlyList<Code> sentence, bool asking, IReadOnlyList<Code>? coded)
    {
        var before = new List<IReadOnlyList<Code>>();

        if (Carries(asking))
            for (var back = _said.Count - 1; back >= 0; back--) before.Add(_said[back]);
        else if (!asking)
            before.Add(sentence);

        var asked = asking ? coded ?? sentence : null;

        return Coded.From(
            [.. before.Select(Grouped.Of)],
            asked is null ? null : Grouped.Of(asked),
            things: Grouped.Things(
                asked is null ? before : [.. before, asked], _nouns));
    }

    /// <summary>Whether this moment is handed the topic in front of it.</summary>
    private bool Carries(bool asking) => _settings.Carrying switch
    {
        Carrying.Always => true,
        Carrying.Statements => !asking,
        _ => false,
    };

    /// <summary>Which of a statement's words each get a moment, where several do.</summary>
    /// <remarks>
    /// <b>Empty where one word is claimed or none is</b>, which leaves the sentence a single
    /// moment and the arm reading it exactly as it did before this existed.
    /// </remarks>
    private IReadOnlyList<int> Claims(IReadOnlyList<string> words)
    {
        if (words.Count < 2) return [];

        if (_settings.Asserting is Asserting.Everything)
            return [.. Enumerable.Range(0, words.Count)];

        return [];
    }

    /// <summary>Where in a statement the word it is taken to claim sits.</summary>
    /// <remarks>
    /// <b>The one this conversation has said least often</b>, ties to the earliest, which is
    /// arbitrary and fixed. A statement of one word claims nothing, because a claim with its
    /// whole scope removed is a claim about nothing being present.
    /// </remarks>
    private int? Claimed(IReadOnlyList<string> words) => words.Count < 2
        ? null
        : words
            .Select((word, at) => (word, at))
            .OrderBy(one => _often.GetValueOrDefault(one.word, 0))
            .ThenBy(one => one.at)
            .First().at;

    /// <summary>One typed line, cut into the sentences it holds.</summary>
    /// <remarks>
    /// <para>
    /// <b>A stop, a question mark or an exclamation ends one</b>, and the mark stays on the end
    /// of what it closed so that questionhood is still read off the text. That is the whole
    /// rule, and it is the same licence the final <c>?</c> already carried — a fact about how
    /// the signal was written rather than anything about what it means.
    /// </para>
    /// <para>
    /// <b>Trailing text with no mark on it is a sentence too</b>, because most lines typed at a
    /// terminal end without one. Dropping it would silently swallow the commonest thing a
    /// person types.
    /// </para>
    /// </remarks>
    private static IReadOnlyList<string> Sentences(string text)
    {
        var found = new List<string>();
        var from = 0;

        for (var at = 0; at < text.Length; at++)
        {
            if (text[at] is not ('.' or '?' or '!')) continue;

            var one = text[from..(at + 1)].Trim();

            if (one.Length > 0) found.Add(one);

            from = at + 1;
        }

        var last = text[from..].Trim();

        if (last.Length > 0) found.Add(last);

        return found;
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

    /// <summary>Which code a word is SAID as, by where it sits in the alphabet.</summary>
    /// <param name="word">Where the word sits in <see cref="Vocabulary"/>.</param>
    /// <remarks>
    /// <para>
    /// <b>The inverse of <see cref="Naming"/></b>, and the pair is the whole of what a word
    /// being two codes costs. A machine is handed an outcome as an index and a moment holds a
    /// hash, so putting an answer back into a moment is a translation only the world can do.
    /// </para>
    /// <para>
    /// <b>Nothing outside the alphabet</b>, which a caller has to be able to ask for. An
    /// outcome index is a small whole number and there is no promise this world has heard a
    /// word for every one of them.
    /// </para>
    /// </remarks>
    public Code? Meaning(int word) =>
        word >= 0 && word < _vocabulary.Count ? Babi.Of(_vocabulary[word]) : null;

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
