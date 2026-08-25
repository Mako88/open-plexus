using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>How the roaming world is set up. Every number named and none defaulted.</summary>
public sealed record RoamingSettings
{
    /// <summary>How many rooms the house has.</summary>
    /// <remarks>
    /// <b>The answer alphabet, so it is the marginal's denominator too.</b> Few rooms make
    /// the commonest answer strong and the exam easy for reasons that have nothing to do
    /// with tracking.
    /// </remarks>
    public required int Rooms { get; init; }

    /// <summary>How many things are lying about in it.</summary>
    public required int Props { get; init; }

    /// <summary>How many people are walking round it.</summary>
    /// <remarks>
    /// <para>
    /// <b>What makes the chain to the answer real, and one person cannot.</b> A thing's room is
    /// where whoever dropped it was standing, so the answer is reached by two hops: the thing
    /// names a person, the person names a room. With one person that middle hop is free — every
    /// action statement names the same word, so there is only ever one candidate and the walk
    /// is answerable by following anything at all.
    /// </para>
    /// <para>
    /// <b>And it is the axis fork 95 was waiting for.</b> Which key a fold should follow is
    /// unanswerable where the only key that connects statements is a verb; a second person
    /// makes the informative key exist, so a rule that picks it can be told from a rule that
    /// picks any of them. The company a fold arrives with is what this should cut.
    /// </para>
    /// </remarks>
    public required int People { get; init; }

    /// <summary>At most how many things happen before the survey.</summary>
    /// <remarks>
    /// <para>
    /// <b>What makes the answer move, which is the whole world.</b> At nought the opening
    /// placements are the answer and a bag reads them straight off; every step after that
    /// is a chance for the truth to change while the sentence that stated the old one is
    /// still sitting there in plain view.
    /// </para>
    /// <para>
    /// <b>A CAP rather than a length</b>, because how long somebody stays in a house is
    /// decided by whoever is walking it. <see cref="Enough"/> is what ends it sooner, and
    /// this is what stops a machine that never has enough from walking one house forever.
    /// </para>
    /// </remarks>
    public required int Steps { get; init; }

    /// <summary>How many questions the survey asks once the walk round the house is over.</summary>
    /// <remarks>
    /// <para>
    /// <b>A SIZE, which is what admits the survey to the target world.</b> A house
    /// can be asked about at any length and none of the lengths makes it less like a house,
    /// where a setting choosing WHICH single question gets asked is a switch and one of the
    /// three the target is not allowed to keep.
    /// </para>
    /// <para>
    /// <b>Four kinds rather than one, and they are asked in one exam.</b> Where a thing
    /// ended up, what a room held, how many things were in one, and what WOULD follow a
    /// doing — so a machine at the top of the exam has to have tracked a thing, read a
    /// room, counted and reasoned, rather than have found the one rule the single question
    /// rewarded.
    /// </para>
    /// <para>
    /// <b>And the fourth was never SAID.</b> The first three are facts the transcript
    /// carries, so a script that tracked it scores full marks on them and understands
    /// nothing. <i>If mary took the football where would the football be</i> is answered by
    /// knowing that picking a thing up puts it where the hands are, which no sentence in
    /// front of the machine states — it is <i>what would the world look like if I did X</i>
    /// arriving on the exam. The question names nobody's ROOM, so it does not carry its
    /// own answer, which the first shape of it did.
    /// </para>
    /// <para>
    /// <b>And counting is at the scope language's ceiling on purpose.</b> A conjunction of
    /// codes cannot say <i>two of these</i>, which is Monk-2's own lesson with a published
    /// number beside it. Leaving the kind out would be editing the exam until the machine
    /// could pass it, and the exam is the one thing here that may not move.
    /// </para>
    /// </remarks>
    public required int Asked { get; init; }

    /// <summary>How many rounds a person gets with the machine once the exam is over.</summary>
    /// <remarks>
    /// <para>
    /// <b>The last phase, and a SIZE like the other two.</b> The machine explores the house,
    /// sits the survey, and then somebody talks to it about what it saw — so a conversation
    /// is a window in one world's episode rather than a world of its own.
    /// </para>
    /// <para>
    /// <b>After the exam, because asking first restates the answer.</b> An answer given in
    /// the conversation joins the transcript, so an exam that followed it would be asking a
    /// question whose answer is the most recent statement — recency wearing a conversation's
    /// clothes. <c>RoamingTests</c> measured the lift that road produced before it was cut.
    /// </para>
    /// <para>
    /// <b>And the answerer is a PERSON rather than the house</b>, which is what an exam on
    /// facts cannot be. A score on stated facts is reachable by a script holding the
    /// transcript; what is said between somebody and the machine is not, and the plumbing is
    /// all a suite may check of it.
    /// </para>
    /// <para>
    /// <b>So it is nought wherever nobody is typing</b>, and <see cref="RoamingSettings.Typed"/>
    /// and <see cref="RoamingSettings.Printed"/> are required the moment it is not. A run with
    /// no person in it walks the house and sits the exam, which is every reading this world
    /// has ever taken.
    /// </para>
    /// </remarks>
    public required int Chatting { get; init; }

    /// <summary>Where the person's lines come from, or nothing where there is no person.</summary>
    /// <remarks>
    /// <b>A reader rather than a transcript</b>, so the same seam takes a terminal and a
    /// script. What is refused is a world that answers its own questions: the house knows
    /// where everything is and would hand the machine what it asked for, which is the
    /// experimenter supplying what the machine should go and get.
    /// </remarks>
    public TextReader? Typed { get; init; }

    /// <summary>Whether whoever is walking has had enough of this house.</summary>
    /// <remarks>
    /// <para>
    /// <b>Asked once a step</b>, and the walk ends the moment it says so. How long to stay
    /// somewhere is a fact about the visitor rather than about the house, so a length set
    /// here would be the experimenter deciding when the machine had seen enough.
    /// </para>
    /// <para>
    /// <b>Told rather than read</b>, which is the same standing <see cref="Typed"/> has.
    /// The world asks a question of whoever composed it and never reaches into anything;
    /// what answers it is a decision taken where the world and the brain meet.
    /// </para>
    /// <para>
    /// <b>And nothing means the cap decides</b>, which is the walk every reading before
    /// this was taken on.
    /// </para>
    /// </remarks>
    public Func<bool>? Enough { get; init; }

    /// <summary>Where the machine's words are shown, or nothing where nobody is reading.</summary>
    /// <remarks>
    /// <b>Printed in words, because a hash goes nowhere back.</b> A person asked to answer
    /// <i>? 41</i> cannot, and a session reporting how many questions were answered without
    /// ever saying which is a score with no way to be embarrassed by it.
    /// </remarks>
    public TextWriter? Printed { get; init; }
}

/// <summary>
/// A house, people walking round it, and things that get picked up and put down —
/// <b>TextWorld's shape, generated rather than ported</b>, so the ground truth can be
/// enumerated.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's, 2026-08-12, and the answer to the question he asked with it.</b> TextWorld
/// and Crafter both came up; Crafter is pixels and reward, which puts two unbuilt
/// subsystems in front of the thing being measured, and this branch has already read that
/// a photographic front end is the ceiling rather than the learner. TextWorld's shape is
/// rooms, things and a small verb set, and every one of its demands is on the list of what
/// is missing here.
/// </para>
/// <para>
/// <b>Built rather than ported, which is this repo's own rule —</b> borrow the problem, not
/// the mechanism. A benchmark varies its parser, its vocabulary, its quest length and
/// its room count at once, and a number off it cannot be attributed to any of them. What
/// is wanted is the property, one axis at a time, with the state small enough to enumerate
/// — which is the same reason <see cref="Multiplexer"/> earns its keep and no corpus can.
/// </para>
/// <para>
/// <b>And watched before acted in, which is the sequencing that matters.</b> Nothing here
/// acts: action and goals are both on the capability list as unbuilt, so an interactive
/// world would demand a policy, a goal and a retracting store at once. A scripted walk
/// generates the transcript and the brain predicts, which exercises individuals,
/// retraction and relations with no new machinery. Acting is a later arm on this same
/// world rather than a different world.
/// </para>
/// <para>
/// <b>It speaks <see cref="Codes.Coded"/></b>, which is a moment with the word order
/// still on it. The whole text front end applies unchanged — every
/// <see cref="Joining"/> arm reads its parts as sets and gets the codes it always
/// got, so a reading here stands beside bAbI's rather than starting a second scale nobody
/// can compare across. What the shape adds is the order report, which is the one thing a bag
/// of a sentence cannot carry and the thing rung three is made of.
/// </para>
/// <para>
/// <b>And this is the spine, so it is the world that grows.</b> Order is the first tier of
/// it and twins are the next; an acting arm is the last. A world built to isolate one
/// question is still built freely and goes when that question shuts, because only a
/// constructed world can prove a ceiling — what a growing world buys is that each tier is
/// read against the one below it on the same scale.
/// </para>
/// <para>
/// <b>What it fixes about bAbI is the property that disqualified it.</b> Some two thousand
/// distinct contexts exist there and no more, so reading it twice is re-reading it. This
/// draws a fresh house, a fresh scatter of things and a fresh walk every episode, so the
/// held-out half is genuinely unseen and a score cannot be a lookup.
/// </para>
/// <para>
/// <b>Twins are the next axis and are deliberately not here.</b> Two things wearing one
/// word is where <see cref="Returning"/>'s finding would land, and it needs a way to ASK
/// about one of two identical things — which is a relative clause, which is rung four.
/// Adding it before the base world is read would be two unanswered questions in one grid.
/// </para>
/// </remarks>
public sealed class Roaming : IWorld<Coded>, IActed<Coded>
{
    /// <summary>The modality a word rides on.</summary>
    /// <remarks>
    /// <b>Its own rather than <see cref="Babi"/>'s, because the two are different
    /// vocabularies.</b> Sharing one would make <i>kitchen</i> here and <i>kitchen</i> there
    /// the same code, and a population primed on one would be reading the other's words
    /// without anybody having decided that.
    /// </remarks>
    private const byte Word = 46;

    /// <summary>The modality what a thing LOOKS like rides on.</summary>
    /// <remarks>
    /// <b>Its own, so a look is never a name.</b> A world whose looks and words were one
    /// code would hand the crossing over, and a score off it would read as a learner that
    /// joined two senses when nothing in it ever had two.
    /// </remarks>
    private const byte Look = 48;

    /// <summary>The modality a thing's SHADE rides on.</summary>
    /// <remarks>
    /// <b>Its own modality</b>, because a shade is a second way a thing shows through one
    /// sense rather than a second name for it.
    /// </remarks>
    private const byte Shade = 49;

    /// <summary>The shades a thing can be, and several things are each of them.</summary>
    /// <remarks>
    /// <para>
    /// <b>A second way a thing shows through one sense</b>, which is the architecture's own
    /// line: every input is an attribute of a thing and never the thing. A seen thing
    /// reporting exactly one code is the degenerate case, and it is the one that made a
    /// thing's scope reachable by repair — a look and a later NAME separate the misses from
    /// the hits, so repair takes the binding before genesis can mint it.
    /// </para>
    /// <para>
    /// <b>Shared between things on purpose.</b> A shade a thing had to itself would be a
    /// second name for it, and a scope over the two would say nothing a scope over one did
    /// not.
    /// </para>
    /// <para>
    /// <b>And sharing a shade is HALF of that</b>, which this said was all of it. Sharing stops a shade carrying a rule alone; it does nothing about
    /// the look, and a look that named its own thing carried one perfectly well — so the pair
    /// said nothing extra and subsumption deleted every binding genesis minted.
    /// <see cref="Looks"/> is the other half.
    /// </para>
    /// <para>
    /// <b>And it carries no ontology.</b> A world reporting that a thing is a PERSON would be
    /// handing over the category rung five is supposed to find; a shade is a reading off the
    /// signal, and what it stands for is the machine's to work out.
    /// </para>
    /// </remarks>
    private static readonly string[] Shades = ["pale", "dark", "warm", "cold"];

    /// <summary>The looks a thing can have, and two things are each of them.</summary>
    /// <remarks>
    /// <para>
    /// <b>Two things OF A KIND, which is the architecture's own words.</b> A thing is one
    /// thing, told from another of its kind in the same moment — and a house where every look
    /// named its own thing never posed that. A look identified its thing outright, so no
    /// second attribute could add to it: genesis minted a scope over one thing and
    /// subsumption deleted every one for saying nothing extra, on three seeds of three.
    /// </para>
    /// <para>
    /// <b>Sharing a look is what makes the pair a conjunction</b>, and a shade alone was
    /// never going to. <see cref="Shades"/>' argument is half right: sharing a shade stops a
    /// shade carrying a rule by itself, and it does not stop the look carrying one. Both
    /// parts have to be ambiguous before the pair can say what neither does.
    /// </para>
    /// <para>
    /// <b>Paired adjacently so the shades differ.</b> A palette rounded over everything gives
    /// consecutive things consecutive shades, so <c>prop / 2</c> puts two things of a kind in
    /// different shades and <c>prop % 4</c> would put them in the same one — which is the
    /// pairing that looks equivalent and mints nothing.
    /// </para>
    /// <para>
    /// <b>And it carries no ontology</b>, on <see cref="Shades"/>' own terms. Round and flat
    /// are readings off a signal; which things are round is this house's arbitrary fact, as
    /// which are pale is, and what a look stands for is the machine's to work out.
    /// </para>
    /// <para>
    /// <b>It hands over no more than a look per thing did.</b> A look rides its own modality
    /// and never enters the answer vocabulary, so the alphabet and the marginal are the same
    /// numbers on both sides, and <c>CeilingTests</c> reads 0.890 apart either way. An arm
    /// may raise what the front end gives away and must never do it quietly.
    /// </para>
    /// </remarks>
    private static readonly string[] Looks = ["round", "flat", "tall", "small"];

    /// <summary>What holds a thing that is lying on the floor.</summary>
    private const int Nobody = -1;

    /// <summary>What a person types to leave the conversation.</summary>
    /// <remarks>
    /// <b>A stop word rather than an empty line</b>, because a blank line is a round somebody
    /// had nothing to say in and both have to be sayable. It is honoured wherever a line is
    /// read, including the reply to a question — a session that could not be left while the
    /// machine was asking is one nobody would start.
    /// </remarks>
    public const string Over = ".quit";

    private static readonly string[] Places =
    [
        "kitchen", "garden", "office", "bathroom", "bedroom", "hallway", "cellar", "attic",
    ];

    private static readonly string[] Things =
    [
        "apple", "football", "milk", "book", "lamp", "kettle", "hat", "brush",
    ];

    /// <summary>Who is walking about, in outcome order for nothing.</summary>
    /// <remarks>
    /// <b>John first</b>, so a walk of one person is the walk this world always had. Every
    /// reading taken before a second person existed was taken on a transcript naming him, and
    /// keeping him at the front means those readings are still about the same sentences.
    /// </remarks>
    private static readonly string[] Cast =
    [
        "john", "mary", "sandra", "daniel", "fred", "julie", "bill", "emma",
    ];

    /// <summary>The words this world says that name no room, thing or person.</summary>
    /// <remarks>
    /// <b>Verbs among them, which is what makes a command sayable.</b> A machine that could
    /// only name a room would be picking from a menu the world wrote; naming the verb and
    /// what it is about out of the same alphabet the world speaks is the whole of the
    /// channel.
    /// </remarks>
    private static readonly string[] Grammar =
    [
        "the", "is", "in", "to", "what", "next", "where", "went", "took", "dropped", "waited",
    ];

    /// <summary>The words the survey adds, and it adds them only where there is a survey.</summary>
    /// <remarks>
    /// <b>Apart from <see cref="Grammar"/> so no earlier reading moves.</b> The alphabet is
    /// what a blind guess is against and what a chooser may say, so putting <i>how</i> and
    /// the numbers in every house would have changed the marginal of every walk taken before
    /// the survey existed.
    /// </remarks>
    private static readonly string[] Asking = ["how", "many", "if", "would", "be"];

    /// <summary>How many of something there were, as a word.</summary>
    /// <remarks>
    /// <b>As many as the house has things, and one more for none</b>, so the answer
    /// alphabet is a fact about the house's size rather than a number chosen here. A word for
    /// nine in a house of four things would be an answer no question could have.
    /// </remarks>
    private static readonly string[] Counts =
    [
        "none", "one", "two", "three", "four", "five", "six", "seven", "eight",
    ];

    /// <summary>Going somewhere.</summary>
    private const string Went = "went";

    /// <summary>Picking something up.</summary>
    private const string Took = "took";

    /// <summary>Putting something down.</summary>
    private const string Dropped = "dropped";

    /// <summary>
    /// How many words the world hears about one moment before it acts on what it has.
    /// </summary>
    /// <remarks>
    /// <b>The world's own longest statement, rather than a number picked here.</b> <i>the
    /// apple is in the kitchen</i> is six words, so a machine is allowed to say as much
    /// about a step as the world says about one — and something has to bound it, because a
    /// chooser that never runs out and a world that never stops listening are a loop with no
    /// end in it.
    /// </remarks>
    private const int Patience = 6;

    private readonly RoamingSettings _settings;
    private readonly Random _walks;

    private readonly List<string> _vocabulary = [];
    private readonly Dictionary<Code, int> _naming = [];
    private readonly Dictionary<string, int> _rooms = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _things = new(StringComparer.Ordinal);

    /// <summary>What shade each thing shows, by where it sits among everything there is.</summary>
    private readonly Dictionary<string, Code> _shades = new(StringComparer.Ordinal);

    /// <summary>The words that name a thing, as against the ones that name none.</summary>
    /// <remarks>
    /// <b>A room is a thing</b>, which is a claim rather than a convenience. The kitchen is
    /// as much a thing as the apple in it — <i>the apple is in the kitchen</i> is a relation
    /// between two of them — and a world that reported rooms as background would be saying
    /// which of its own nouns are worth having concepts of.
    /// </remarks>
    private readonly HashSet<Code> _nouns = [];

    /// <summary>What the machine said about this moment, or nothing where it said none.</summary>
    private List<int>? _spoken;

    // The house being walked, how many steps of it are left, and what has been met in it.
    // Explored only: a recital opens and closes a house inside one turn and needs none of it.
    private Walk? _house;
    private int _left;

    // The survey's questions, oldest first, drawn when the conversation runs out. The house
    // stays alive while they are being asked, because what they are about is its transcript.
    private readonly List<Question> _asking = [];

    // How many rounds of talking about the house are left, once its exam is over.
    private int _chats;

    // What the person said this round, or nothing where they have not been read yet. Empty
    // and read are different states, which is why this is nullable rather than counted: a
    // blank line is a round they said nothing in and must not cost a second read.
    private IReadOnlyList<Code>? _heard;

    private readonly List<string> _order = [];
    private readonly Dictionary<string, List<Code>> _met = new(StringComparer.Ordinal);

    // The moment the open walk reads as, built once. A doing accumulates words and moves
    // nothing, so a machine that says three of them is looking at one state -- and the
    // caller reads this once a doing, so a fresh build a look would be a whole moment's
    // worth of work for a state that did not change. Cleared with the walk it is of.
    private Coded? _shown;

    /// <param name="settings">How the world is set up.</param>
    /// <param name="seed">What draws the houses and the walks.</param>
    public Roaming(RoamingSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Rooms, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Rooms, Places.Length);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Props);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Props, Things.Length);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.People);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.People, Cast.Length);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Steps);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Asked);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Chatting);

        // A conversation with nobody in it is the house answering itself, which is the one
        // thing this phase may not be. Refused at construction rather than read as no rounds,
        // because a run asking for a conversation and silently getting none is a composition
        // that reads as the arm it is not.
        if (settings.Chatting > 0 && (settings.Typed is null || settings.Printed is null))
            throw new ArgumentException(
                "a conversation is with a PERSON, so `Typed` and `Printed` are required "
                + "wherever `Chatting` is not nought",
                nameof(settings));

        _settings = settings;
        _walks = new Random(seed);

        // The alphabet the machine may speak, which is the one this house speaks. A word for
        // a room that does not exist would be a doing that can never be done, and a machine
        // spending its say on one would be measured against a menu rather than a house.
        foreach (var word in Grammar) Heard(word);

        for (var room = 0; room < settings.Rooms; room++)
        {
            _rooms[Places[room]] = room;

            _nouns.Add(Kinds.Named(Word, Places[room]));

            Heard(Places[room]);
        }

        for (var prop = 0; prop < settings.Props; prop++)
        {
            _things[Things[prop]] = prop;

            _nouns.Add(Kinds.Named(Word, Things[prop]));

            Heard(Things[prop]);
        }

        foreach (var one in Cast.Take(settings.People))
        {
            _nouns.Add(Kinds.Named(Word, one));

            Heard(one);
        }

        // And the words an exam needs, where there is one to sit or one to talk about. Last,
        // so a house without either speaks the alphabet it always spoke and every reading
        // taken on one still stands.
        if (settings.Asked > 0 || settings.Chatting > 0)
        {
            foreach (var word in Asking) Heard(word);

            foreach (var word in Counts.Take(settings.Props + 1)) Heard(word);
        }

        // Round the palette over everything there is, so a room, a prop and a person can wear
        // one shade and no thing has one to itself.
        var everything = Places.Concat(Things).Concat(Cast).ToList();

        for (var thing = 0; thing < everything.Count; thing++)
            _shades[everything[thing]] =
                Kinds.Named(Shade, Shades[thing % Shades.Length]);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>The whole alphabet, because the answer is a WORD.</b> The walk names one of the
    /// things in front of the machine, the conversation answers what it asked, and the
    /// survey answers in a room, a thing or a number — so what a blind guess is against is
    /// everything the house can say.
    /// </remarks>
    public int Outcomes => _vocabulary.Count;

    /// <summary>
    /// The code for each room's word, in outcome order — <b>so a ceiling comes off the transcript</b>
    /// rather than off the state.
    /// </summary>
    /// <remarks>
    /// <b>What it said and never what to conclude</b>, which is the line a world stays on.
    /// A probe asking <i>which room word was mentioned last</i> needs to know which codes
    /// are room words, and that is a fact about the vocabulary this world emitted rather
    /// than a hint about the answer. Nothing that learns is ever shown it — the same
    /// standing as <c>RecalledTests</c>'s answer key, which names the cast and reaches no
    /// population.
    /// </remarks>
    public IReadOnlyList<Code> Named =>
        [.. Places.Take(_settings.Rooms).Select(place => Kinds.Named(Word, place))];

    /// <summary>The code for each thing's word, in prop order. <b>The same standing.</b></summary>
    public IReadOnlyList<Code> Called =>
        [.. Things.Take(_settings.Props).Select(thing => Kinds.Named(Word, thing))];

    /// <summary>The code for each person's word, in cast order. <b>The same standing.</b></summary>
    /// <remarks>
    /// <b>What a probe following the chain needs</b>, and it is a fact about the vocabulary. Asking whether
    /// this world is answerable by following the thing to a person and the person to a room
    /// needs to know which codes are people. Nothing that learns is ever shown it.
    /// </remarks>
    public IReadOnlyList<Code> Walking =>
        [.. Cast.Take(_settings.People).Select(one => Kinds.Named(Word, one))];

    /// <summary>Whether the round just taken was one of the survey's questions.</summary>
    /// <remarks>
    /// <b>An instrument's channel, on <see cref="Named"/>'s standing.</b> The world's own
    /// turn in the conversation is a question too, so a reader telling the exam from the
    /// conversation by counting a question's words would be reading a coincidence. Nothing
    /// that learns is ever shown this.
    /// </remarks>
    public bool Sat { get; private set; }

    /// <summary>How many of the conversation's rounds somebody had an answer for.</summary>
    /// <remarks>
    /// <b>An instrument's channel, on <see cref="Sat"/>'s standing.</b> A round the machine
    /// said nothing askable in settles on nothing and costs a commitment nothing, so a
    /// conversation that ran and a conversation that was answered are two different facts and
    /// a tally cannot tell them apart.
    /// </remarks>
    public long Answered { get; private set; }

    /// <summary>How many questions the machine put to the person.</summary>
    public long Questions { get; private set; }

    /// <summary>Which room the body is standing in, or nothing where no house is open.</summary>
    /// <remarks>
    /// <b>An instrument's channel, on <see cref="Named"/>'s standing.</b> How much of a house
    /// a chooser covered is a fact about the walk it took, and two arms that stood in
    /// different numbers of rooms were examined about different things -- which no exam score
    /// can say. Nothing that learns is ever shown this.
    /// </remarks>
    public Code? Standing => _house is null
        ? null
        : Kinds.Named(Word, Places[_house.Here[Body]]);

    /// <summary>Rounds the machine's words made a command at all.</summary>
    /// <remarks>
    /// <b>An instrument's channel, on <see cref="Sat"/>'s standing.</b> A chooser that never
    /// says a verb and a chooser that says one the world refuses are two different failures
    /// and no score can tell them apart -- both read as a machine that stood still. Nothing
    /// that learns is ever shown this.
    /// </remarks>
    public long Ordered { get; private set; }

    /// <summary>Rounds one of those commands was possible and was carried out.</summary>
    /// <remarks>
    /// <b>The other half of <see cref="Ordered"/>.</b> A wish it could not grant spends the step
    /// and moves nothing, so the gap between the two is how often the machine asked for
    /// something the house could not do.
    /// </remarks>
    public long Did { get; private set; }

    /// <summary>How many capped steps went unwalked because the machine had had enough.</summary>
    /// <remarks>
    /// <b>An instrument's channel, on <see cref="Sat"/>'s standing.</b> A walk that ended
    /// early and a walk that ran to its cap are two different episodes, and no score can
    /// say which happened — so an arm that never once ended early would read exactly like
    /// the cap it replaced. Nothing that learns is ever shown this.
    /// </remarks>
    public long Left { get; private set; }

    /// <summary>Whether the person has left.</summary>
    /// <remarks>
    /// <b>The walk carries on without them</b>, because a house is walked whether or not
    /// anybody is watching. What ends is the conversation: a phase whose answerer has gone is
    /// one where the world would be reading a stream with nothing left in it, and a read that
    /// returns nothing for ever is a loop rather than a world.
    /// </remarks>
    public bool Ended { get; private set; }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>A walked step, a turn of the conversation, or an exam question</b>, in that order
    /// and drawn from the same house. A chooser is
    /// spent by the round that follows it, so no wish survives into the next one.
    /// </remarks>
    public Turn<Coded> Next()
    {
        Sat = false;

        return Walked();
    }

    /// <summary>The codes for one sentence, in the order the words were said.</summary>
    /// <param name="words">The words of it, in order.</param>
    /// <remarks>
    /// <b>A list rather than a set, on the licence <see cref="Codes.Coded"/> carries.</b> Order is
    /// a fact about the signal; which word is the room and which the thing is a role, and no
    /// world here may say that. A front end that wants none of the order flattens this in one
    /// call.
    /// </remarks>
    private static IReadOnlyList<Code> Said(params string[] words) =>
        [.. words.Select(word => Kinds.Named(Word, word))];

    /// <summary>Which room every thing is in, whether it is on a floor or in a hand.</summary>
    /// <param name="at">Where each loose thing lies.</param>
    /// <param name="held">Who is holding each thing, or <see cref="Nobody"/>.</param>
    /// <param name="here">Where each person is standing.</param>
    /// <remarks>
    /// <b>A thing in a hand is where the hand is</b>, which is the whole of why the effect
    /// question needs the transcript rather than the verb. Nothing in <i>john went to the
    /// garden</i> names the football, and the football is in the garden.
    /// </remarks>
    private static int[] Placed(int[] at, int[] held, int[] here) =>
        [.. at.Select((room, one) => held[one] == Nobody ? room : here[held[one]])];

    /// <summary>A house, a scatter and a walk, drawn as far as its last step.</summary>
    /// <param name="At">Where each loose thing lies.</param>
    /// <param name="Held">Who is holding each thing, or <see cref="Nobody"/>.</param>
    /// <param name="Here">Where each person is standing.</param>
    /// <param name="Told">The statements so far, oldest first.</param>
    private sealed record Walk(
        int[] At, int[] Held, int[] Here, List<IReadOnlyList<Code>> Told);

    /// <summary>Every word this house speaks, in the order a doing numbers them.</summary>
    public IReadOnlyList<string> Vocabulary => _vocabulary;

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>A word rather than a verb index.</b> The machine says words and the world
    /// parses a command out of them, where three opaque verbs made the argument the
    /// world's: a chooser picking
    /// <i>take</i> was told which thing by the draw, so what it had learnt could never be
    /// about a particular thing. Saying <i>took</i> and <i>the apple</i> is one claim about
    /// one thing, which is what a scope can be about.
    /// </para>
    /// <para>
    /// <b>And the alphabet is the house's whole vocabulary rather than the commands.</b> A
    /// menu of legal commands is the world telling the machine which words are verbs and
    /// which rooms exist, which is a fact it should have to learn. What a word the parse
    /// cannot use costs is a step spent waiting.
    /// </para>
    /// <para>
    /// <b>Who does it stays the world's</b>, because a chooser reading codes cannot know
    /// which thing is loose in the room somebody is standing in — and a world that let it
    /// name the walker would be choosing between people on the machine's behalf.
    /// </para>
    /// </remarks>
    public int Doings => _vocabulary.Count;

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>While what was said is not yet a command</b>, which is what a command being
    /// several words costs. A step is still one step and a moment is still
    /// where the walker stands — what listening twice buys is the rest of one sentence
    /// rather than a second sentence, so no part of the walk goes unreported.
    /// </para>
    /// <para>
    /// <b>And it stops the moment the parse succeeds</b>, so a machine that says the verb
    /// and the thing in two words is not made to fill the rest of a budget. Everything after
    /// a complete command would be words about a step already decided.
    /// </para>
    /// <para>
    /// <b>And deaf while the survey is running</b>, because the walk is over. A machine
    /// still filling a command's words there would be acting on a house it has finished
    /// walking.
    /// </para>
    /// </remarks>
    public bool Listening =>
        _asking.Count == 0
        && _spoken is { Count: > 0 } spoken
        && spoken.Count < Patience
        && (_chats > 0 ? !Wondered(spoken) : Parse(spoken) is null);

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>The house as it stands before the round is taken</b>, so what is read is the
    /// state an action is taken in rather than one it has already been taken in. Asking
    /// for it draws the house where none is open, and the same walk is what
    /// <see cref="Next"/> then steps.
    /// </para>
    /// <para>
    /// <b>The question slot says what is being asked</b>, never what to conclude. In the
    /// conversation it is the world's own turn and in the exam it is the question; on a
    /// walked step there is none, because nothing is being asked.
    /// </para>
    /// </remarks>
    public Coded Now
    {
        get
        {
            if (_shown is { } already) return already;

            if (_asking.Count == 0 && _chats > 0) Spoke(_house!);

            return (_shown = _asking.Count > 0
                ? Surveying()
                : _chats > 0
                    ? Talking(_house!, [])
                    : Sighted(_house ??= Housed(), null)).Value;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>Nothing means the learner does not intervene</b>, rather than the walk standing
    /// still. The people here are walking whether or not anything is choosing for them, so
    /// declining leaves the world drawing its own last step — which is the arm every reading
    /// taken before this existed was taken on, down to the draw.
    /// </para>
    /// <para>
    /// <b>And a verb that cannot be done is done as waiting</b>, said out loud. Substituting
    /// the nearest possible action would make a chooser's arm the world's own draw wearing
    /// the chooser's name, and dropping the step silently would leave the effect question
    /// answering about a statement nobody made.
    /// </para>
    /// </remarks>
    public void Do(int? doing)
    {
        if (doing is not { } said)
        {
            _spoken = null;

            return;
        }

        ArgumentOutOfRangeException.ThrowIfNegative(said, nameof(doing));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(said, Doings, nameof(doing));

        (_spoken ??= []).Add(said);
    }

    /// <summary>Which word a code is, or nothing where it is none of this house's.</summary>
    /// <param name="code">A code from a moment.</param>
    /// <remarks>
    /// <b>What a machine with no rules needs.</b> A command
    /// is words, and until something fires there is no word the population can offer — so a
    /// chooser that could only say its own expectations would never act, never settle and
    /// never come to have one. Handed to a chooser rather than read by one, because a
    /// chooser naming this world would put one house's vocabulary in front of every other.
    /// </remarks>
    public int? Naming(Code code) => _naming.TryGetValue(code, out var at) ? at : null;

    /// <summary>Which code a word is SAID as, by where it sits in the alphabet.</summary>
    /// <param name="word">Where the word sits in <see cref="Vocabulary"/>.</param>
    /// <remarks>
    /// <b>The inverse of <see cref="Naming"/></b>, and the pair is what a word being an index
    /// on one side and a hash on the other costs. Nothing outside the alphabet, which a
    /// caller has to be able to ask for.
    /// </remarks>
    public Code? Meaning(int word) =>
        word >= 0 && word < _vocabulary.Count
            ? Kinds.Named(Word, _vocabulary[word])
            : null;

    /// <summary>Where a word sits in the alphabet, adding it if it is new.</summary>
    private int Heard(string word)
    {
        if (_naming.TryGetValue(Kinds.Named(Word, word), out var at)) return at;

        _naming[Kinds.Named(Word, word)] = at = _vocabulary.Count;

        _vocabulary.Add(word);

        return at;
    }

    /// <summary>A verb and the one thing or room it was said about.</summary>
    /// <param name="Verb">Which of <see cref="Went"/>, <see cref="Took"/>, <see cref="Dropped"/>.</param>
    /// <param name="About">The room it names under <see cref="Went"/>, and the thing otherwise.</param>
    private sealed record Command(string Verb, int About);

    /// <summary>The command in what was said, or nothing where there is none in it.</summary>
    /// <param name="spoken">The words the machine said about this moment, in order.</param>
    /// <remarks>
    /// <para>
    /// <b>The first verb and the first thing it could be about</b>, and the words between
    /// them are ignored rather than refused. <i>went to the garden</i> and <i>garden went</i>
    /// are the same command here, because word order is a fact about the signal and the
    /// grammar of a command is not something this world is entitled to teach by refusing.
    /// </para>
    /// <para>
    /// <b>And a verb with nothing to be about is no command</b> rather than a drawn
    /// argument. Filling the gap would make the machine's arm the world's own draw wearing
    /// the machine's name, which is a fallback arm nobody meant to run.
    /// </para>
    /// </remarks>
    private Command? Parse(IReadOnlyList<int> spoken)
    {
        var words = spoken.Select(one => _vocabulary[one]).ToList();

        var verb = words.FirstOrDefault(
            word => word is Went or Took or Dropped);

        if (verb is null) return null;

        var named = verb == Went ? _rooms : _things;

        foreach (var word in words)
            if (named.TryGetValue(word, out var about)) return new Command(verb, about);

        return null;
    }

    /// <summary>One step of the walk, commanded or drawn.</summary>
    /// <param name="walk">The house and where everything in it stands.</param>
    /// <param name="spoken">What the machine said, or nothing to let the walk draw.</param>
    /// <param name="walker">Whose step it is, or nothing to draw one.</param>
    /// <remarks>
    /// <para>
    /// <b>A wish is not filtered against what is possible</b>, because filtering it would
    /// hand a chooser the world's knowledge of what is possible through the back of the
    /// interface. What an impossible command gets is a step that happens and does nothing.
    /// </para>
    /// <para>
    /// <b>And it is never said out loud.</b> The machine is shown what its step left in
    /// front of it rather than told what it did, so a sentence here would be the world
    /// narrating the machine to itself.
    /// </para>
    /// </remarks>
    private void Step(Walk walk, IReadOnlyList<int>? spoken, int? walker = null)
    {
        var (at, held, here, _) = walk;

        // Whose turn it is, and one person is nobody to choose between. A draw over one
        // option decides nothing, so it is not taken -- which also leaves the walk of a
        // one-person house the walk every earlier reading was taken on.
        var who = walker
            ?? (_settings.People == 1 ? 0 : _walks.Next(_settings.People));

        if (spoken is not null)
        {
            if (Parse(spoken) is { } wanted)
            {
                Ordered++;

                if (Possible(walk, who, wanted))
                {
                    Did++;

                    Done(walk, who, wanted);
                }
            }

            return;
        }

        var holding = Enumerable.Range(0, _settings.Props)
            .Where(one => held[one] == who)
            .ToList();

        var loose = Enumerable.Range(0, _settings.Props)
            .Where(one => held[one] == Nobody && at[one] == here[who])
            .ToList();

        // Move, take or drop, and a drawn choice is between what is possible rather than
        // between three. A walk that tried to drop what it was not holding would emit a
        // sentence the world's own state contradicts, which is a transcript nothing could be
        // scored against.
        var can = new List<int> { 0 };
        if (loose.Count > 0) can.Add(1);
        if (holding.Count > 0) can.Add(2);

        var drawn = can[_walks.Next(can.Count)] switch
        {
            1 => new Command(Took, loose[_walks.Next(loose.Count)]),
            2 => new Command(Dropped, holding[_walks.Next(holding.Count)]),
            _ => new Command(Went, _walks.Next(_settings.Rooms)),
        };

        Done(walk, who, drawn);
    }

    /// <summary>Whether a walker could do this now.</summary>
    /// <param name="walk">The house and where everything in it stands.</param>
    /// <param name="who">Whose step it is.</param>
    /// <param name="doing">What was asked for.</param>
    private static bool Possible(Walk walk, int who, Command doing) =>
        doing.Verb switch
        {
            Took => walk.Held[doing.About] == Nobody
                && walk.At[doing.About] == walk.Here[who],
            Dropped => walk.Held[doing.About] == who,
            _ => true,
        };

    /// <summary>The step, done, and the sentence for it.</summary>
    /// <param name="walk">The house and where everything in it stands.</param>
    /// <param name="who">Whose step it is.</param>
    /// <param name="doing">What is done, which this world has already found possible.</param>
    /// <remarks>
    /// <b>The sentence is handed back rather than said</b>, because an explorer is not told
    /// what it just did. Doing and saying were one call while every walk was recited, and a
    /// world that narrated a step to the machine that took it would be handing over the half
    /// it is supposed to see.
    /// </remarks>
    private static IReadOnlyList<Code> Done(Walk walk, int who, Command doing)
    {
        var (at, held, here, _) = walk;

        switch (doing.Verb)
        {
            case Took:
                held[doing.About] = who;

                return Said(Cast[who], "took", "the", Things[doing.About]);

            case Dropped:
                held[doing.About] = Nobody;
                at[doing.About] = here[who];

                return Said(Cast[who], "dropped", "the", Things[doing.About]);

            default:
                here[who] = doing.About;

                return Said(Cast[who], "went", "to", "the", Places[doing.About]);
        }
    }

    /// <summary>Whose body the machine is walking.</summary>
    /// <remarks>
    /// <b>The first of the cast</b>, so a house of one person is the machine alone in it. A
    /// body drawn per house would make what the machine is called a fact about the episode,
    /// and every rule it learnt about itself would be about somebody else next time.
    /// </remarks>
    private const int Body = 0;

    /// <summary>The code for what a thing LOOKS like.</summary>
    /// <param name="name">The thing.</param>
    /// <remarks>
    /// <b>Its KIND's and never its own</b>, so two things of a kind look alike and what a
    /// thing is called has to be joined to what it looks like. That is the crossing a
    /// picture will pose, reached rather than designed away.
    /// </remarks>
    private static Code Seen(string name) => Kinds.Named(Look, Alike(name));

    /// <summary>What a thing looks like, which several things share.</summary>
    /// <param name="name">The thing.</param>
    /// <remarks>
    /// <b>Props alone</b>, because two rooms of a kind are one room to a walker and a second
    /// person who looks like the first is the individual this world has no mechanism for.
    /// What is wanted is the smallest house that poses the architecture's line.
    /// </remarks>
    private static string Alike(string name)
    {
        var at = Array.IndexOf(Things, name);

        return at < 0 ? name : Looks[at / 2 % Looks.Length];
    }

    /// <summary>What the body can see from where it stands, the room first.</summary>
    /// <param name="walk">The house and where everything in it stands.</param>
    /// <remarks>
    /// <b>A thing in a hand is where the hand is</b>, which is <see cref="Placed"/>'s rule and
    /// the reason it is reused here. What somebody standing in this room is carrying is in
    /// this room, and a machine looking at the room is looking at it.
    /// </remarks>
    private List<string> Before(Walk walk)
    {
        var room = walk.Here[Body];
        var placed = Placed(walk.At, walk.Held, walk.Here);
        var found = new List<string> { Places[room] };

        for (var prop = 0; prop < _settings.Props; prop++)
            if (placed[prop] == room) found.Add(Things[prop]);

        // From one rather than from nought, because the body is not a thing in front of it.
        for (var one = 1; one < _settings.People; one++)
            if (walk.Here[one] == room) found.Add(Cast[one]);

        return found;
    }

    /// <summary>What one code says about a thing, kept with everything else about it.</summary>
    /// <param name="name">The thing.</param>
    /// <param name="code">What has just been said or seen of it.</param>
    /// <remarks>
    /// <b>One part a thing rather than one a sighting</b>, which is what makes a look and a
    /// name the same thing rather than two. A thing met and then named holds both, and a
    /// scope over the pair is a scope about one thing.
    /// </remarks>
    private void Meets(string name, Code code)
    {
        if (!_met.TryGetValue(name, out var codes))
        {
            _met[name] = codes = [];

            _order.Add(name);
        }

        if (!codes.Contains(code)) codes.Add(code);
    }

    /// <summary>A look at the room, recorded and added to what has been seen.</summary>
    /// <param name="walk">The house and where everything in it stands.</param>
    /// <param name="doing">The machine's own words, which are part of what happened.</param>
    private List<Code> Sight(Walk walk, IReadOnlyList<Code> doing)
    {
        var sighting = new List<Code>(doing);

        foreach (var name in Before(walk))
        {
            var look = Seen(name);

            sighting.Add(look);

            Meets(name, look);

            // And what else the sense says of it. A thing showing one code is a thing a
            // scope has nothing to bind, so the second attribute is what makes the binding
            // a binding rather than the root genesis already mints.
            var shade = _shades[name];

            sighting.Add(shade);

            Meets(name, shade);
        }

        walk.Told.Add(sighting);

        return sighting;
    }

    /// <summary>A fresh house with the body in it, and its first look at the room.</summary>
    private Walk Housed()
    {
        _order.Clear();
        _met.Clear();

        _left = _settings.Steps;

        var at = new int[_settings.Props];
        var held = new int[_settings.Props];

        Array.Fill(held, Nobody);

        var here = new int[_settings.People];

        for (var prop = 0; prop < _settings.Props; prop++)
            at[prop] = _walks.Next(_settings.Rooms);

        for (var one = 0; one < _settings.People; one++)
            here[one] = _walks.Next(_settings.Rooms);

        var walk = new Walk(at, held, here, []);

        // The opening look, which nothing names. A machine asked what it can see before it has
        // seen anything would have an empty moment on the first step of every house.
        Sight(walk, []);

        return walk;
    }

    /// <summary>Everything seen so far, newest first, and one part a thing met.</summary>
    /// <param name="walk">The house being walked.</param>
    /// <param name="chose">The machine's own words, or nothing where it said none.</param>
    private Coded Sighted(Walk walk, IReadOnlySet<Code>? chose) =>
        Coded.From(
            [.. Enumerable.Reverse(walk.Told).Select(Grouped.Of)],
            assigned: chose,
            things: [.. _order.Select(name => Grouped.Of(_met[name]))]);

    /// <summary>
    /// One step of the house the machine is walking, and what it left in front of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The step first and the look after</b>, which is what makes a moment a consequence.
    /// A machine shown the room it is about to leave would be predicting what it can already
    /// see; shown the room its own command put it in, it is answering <i>what would the world
    /// look like if I did X</i>.
    /// </para>
    /// <para>
    /// <b>And the naming is the SETTLEMENT</b>, which is ostension doing the job the plan says
    /// it does. The world says one of the things in front of the machine out loud, and what
    /// the machine had to get right is which. How often the answer is already in the moment
    /// is what <c>CeilingTests</c> prices, and it is never all of them.
    /// </para>
    /// <para>
    /// <b>The name joins the transcript AFTER the round it settled</b>, or the answer would be
    /// sitting in the moment it is the answer to. What that leaves is a thing met once and
    /// named once, so the second time it is seen its word is already in front of the machine
    /// and the crossing has something to be learnt from.
    /// </para>
    /// </remarks>
    private Turn<Coded> Walked()
    {
        if (_asking.Count > 0) return Surveyed();

        if (_chats > 0) return Chatted();

        var walk = _house ??= Housed();
        var spoken = _spoken;

        (_spoken, _shown) = (null, null);

        // The machine's own words, in the moment rather than beside it, so a scope may name
        // what it did and expect the consequence.
        var doing = spoken is null
            ? []
            : spoken.Select(one => Kinds.Named(Word, _vocabulary[one])).ToList();

        Step(walk, spoken, Body);

        // And one of the others moves, so the house does not stand still while it is walked.
        // Drawn from everybody but the body, because a resident taking the machine's turn
        // would be the world moving it twice.
        if (_settings.People > 1)
            Step(walk, null, 1 + _walks.Next(_settings.People - 1));

        var sighting = Sight(walk, doing);
        var seen = Before(walk);
        var named = seen[_walks.Next(seen.Count)];
        var word = Kinds.Named(Word, named);

        var turn = new Turn<Coded>
        {
            Seen = Sighted(walk, doing.Count > 0 ? new HashSet<Code>(doing) : null),
            Outcome = _naming[word],
        };

        sighting.Add(word);

        Meets(named, word);

        // The walk in words, for whoever is going to talk about it. Nothing that learns is
        // shown this -- the machine gets the codes and always did -- and somebody who never
        // saw the house has nothing to hold a conversation about.
        Narrate(spoken, seen, named);

        // The walk is over, so the exam opens and the conversation follows it. Either the
        // cap ran out or whoever is walking has had enough of the house, and which of the
        // two it was is `Left`.
        if (--_left <= 0 || _settings.Enough?.Invoke() == true)
        {
            Left += _left > 0 ? _left : 0;

            Examined(walk);
        }

        return turn;
    }

    /// <summary>One step of the walk, said out loud for whoever is watching it.</summary>
    /// <param name="spoken">What the machine said about this step, or nothing.</param>
    /// <param name="seen">What is in front of it now.</param>
    /// <param name="named">Which of those the world named.</param>
    /// <remarks>
    /// <b>A view rather than a channel</b>, and it carries nothing the moment does not. What
    /// is in front of the machine and what the world named are both already in the codes it
    /// is handed; this is the same two facts in English, for somebody who is going to be
    /// asked about them afterwards.
    /// </remarks>
    private void Narrate(IReadOnlyList<int>? spoken, IReadOnlyList<string> seen, string named)
    {
        // Nothing once they have gone, because the run does not stop when they do. A walk
        // that went on printing at somebody who left would fill a terminal nobody is at.
        if (Ended || _settings.Printed is not { } shown) return;

        if (spoken is { Count: > 0 })
            shown.WriteLine($"  . {string.Join(" ", spoken.Select(one => _vocabulary[one]))}");

        shown.WriteLine($"  > {string.Join(", ", seen)} — this is a {named}");
    }

    /// <summary>The exam for this house, or straight on to the conversation where it poses none.</summary>
    /// <param name="walk">The house, at the state the walk left it in.</param>
    private void Examined(Walk walk)
    {
        _asking.AddRange(Survey(walk));

        if (_asking.Count == 0) Opened();
    }

    /// <summary>The conversation about the house, or the house dropped where nobody is in it.</summary>
    /// <remarks>
    /// <b>The one phase that needs somebody else.</b> A person who
    /// has left leaves no rounds behind them, so a session ended mid-house walks the rest of
    /// the run exactly as a session nobody ever joined does.
    /// </remarks>
    private void Opened()
    {
        _chats = Ended ? 0 : _settings.Chatting;

        if (_chats == 0) _house = null;
    }

    /// <summary>Whether what the machine said is a question somebody could answer.</summary>
    /// <param name="spoken">The words it has said about this moment, in order.</param>
    /// <remarks>
    /// <para>
    /// <b>The survey's own three forms and no others</b>, so what may be asked is what will be
    /// examined. A conversation whose questions could not be the exam's would be a second
    /// problem in the middle of one world.
    /// </para>
    /// <para>
    /// <b>Order-insensitive, on <see cref="Parse"/>'s reason.</b> Word order is a fact about
    /// the signal, and a world refusing <i>apple where</i> would be teaching a grammar by
    /// refusing rather than saying anything.
    /// </para>
    /// <para>
    /// <b>Whether rather than what</b>, which is what the answerer being a person changed.
    /// The house used to read the kind out of this and look the answer up; nothing here knows
    /// the answer now, so all this decides is when the machine has finished a question and
    /// the prompt goes out.
    /// </para>
    /// </remarks>
    private bool Wondered(IReadOnlyList<int> spoken)
    {
        var words = spoken.Select(one => _vocabulary[one]).ToList();

        // Where before how before what, because a machine that said two of them said one
        // question and the world may not draw which. The order is arbitrary and fixed.
        if (words.Contains("where")) return Pointed(words, _things) is not null;

        if (words.Contains("how") || words.Contains("many"))
            return Pointed(words, _rooms) is not null;

        if (words.Contains("what")) return Pointed(words, _rooms) is not null;

        return false;
    }

    /// <summary>The first of these words that names one of those, or nothing.</summary>
    /// <param name="words">What was said, in order.</param>
    /// <param name="named">The rooms or the things.</param>
    private static int? Pointed(IReadOnlyList<string> words, IReadOnlyDictionary<string, int> named)
    {
        foreach (var word in words)
            if (named.TryGetValue(word, out var about)) return about;

        return null;
    }

    /// <summary>What the person said back, and the word of it that settles the round.</summary>
    /// <param name="asked">What the machine said, in order.</param>
    /// <remarks>
    /// <para>
    /// <b>The last word the question did not already say</b>, which is how a whole sentence
    /// answers as readily as a word. Somebody asked <i>where is the apple</i> answers <i>the
    /// apple is in the kitchen</i> as often as <i>kitchen</i>, and reading the first word of
    /// that would settle the round on <i>the</i>. Both halves are arithmetic over two sets
    /// rather than a claim about English, and neither reaches the learner.
    /// </para>
    /// <para>
    /// <b>A blank reply is a shrug and settles nothing</b>, because the counters here are
    /// monotone and there is no way to record that an answer was withheld. Recording one
    /// would be inventing evidence, which is fork <b>30</b>'s question rather than a world's.
    /// </para>
    /// <para>
    /// <b>And a reply is a fact as well as an answer.</b> Somebody who says <i>the apple is
    /// in the kitchen</i> has told the machine something, so the whole sentence joins the
    /// transcript and the word it settles on is drawn from it.
    /// </para>
    /// </remarks>
    private (IReadOnlyList<Code> Sentence, int Outcome)? Replied(IReadOnlyList<string> asked)
    {
        Questions++;

        _settings.Printed!.Write($"  ? {string.Join(" ", asked)} ");
        _settings.Printed.Flush();

        var told = _settings.Typed!.ReadLine();

        // The prompt is closed whatever came back. A prompt is a line with no newline after
        // it, so leaving one open makes the very next thing printed look like a second ask
        // and the person answers twice while the conversation stands still.
        _settings.Printed.WriteLine();

        if (told is null || string.Equals(told.Trim(), Over, StringComparison.Ordinal))
        {
            Ended = true;

            return null;
        }

        var words = Babi.Words(told);

        if (words.Count == 0) return null;

        var said = new HashSet<string>(asked, StringComparer.Ordinal);

        var answer = words.LastOrDefault(word => !said.Contains(word)) ?? words[^1];

        return (Said([.. words]), Heard(answer));
    }

    /// <summary>One round of talking about the house the exam is over in.</summary>
    /// <remarks>
    /// <para>
    /// <b>The machine's own words are the question of the moment</b>, and marked as its
    /// doing. Nothing else here asks, so a round where it said nothing that parses is a round
    /// that settles on nothing and costs a commitment exactly nothing.
    /// </para>
    /// <para>
    /// <b>And the answer joins the transcript after the round it settled</b>, which is the
    /// naming's own rule. An answer sitting in the moment it is the answer to would be the
    /// world handing over what it was about to ask for.
    /// </para>
    /// </remarks>
    private Turn<Coded> Chatted()
    {
        var walk = _house!;
        var spoken = _spoken;

        // Once a round whether or not a chooser looked, so the person is read exactly as
        // often as they are talked to. A composition with nothing acting in it still holds a
        // conversation, and one where a chooser read the moment first must not read a second
        // line for the same round.
        Spoke(walk);

        (_spoken, _shown, _heard) = (null, null, null);

        var said = spoken is null
            ? []
            : spoken.Select(one => Kinds.Named(Word, _vocabulary[one])).ToList();

        var asked = spoken is not null && Wondered(spoken);

        var answer = asked
            ? Replied([.. spoken!.Select(one => _vocabulary[one])])
            : null;

        // Everything else it said, shown rather than swallowed. A person watching a machine
        // that only ever speaks when it has a well-formed question would be watching a
        // machine that says nothing most rounds, which is not what happened.
        //
        // Read off whether it ASKED rather than off whether it was answered, or a question
        // somebody shrugged at would be printed a second time as a claim.
        if (!asked && spoken is { Count: > 0 })
            _settings.Printed!.WriteLine(
                $"  . {string.Join(" ", spoken.Select(one => _vocabulary[one]))}");

        var turn = new Turn<Coded> { Seen = Talking(walk, said), Outcome = answer?.Outcome };

        if (answer is { } told)
        {
            walk.Told.Add(told.Sentence);

            Answered++;
        }

        // The rounds go with the house, so a person who left mid-conversation does not leave
        // a count behind that would have the next round reading a stream nobody is at.
        if (--_chats <= 0 || Ended)
        {
            _chats = 0;
            _house = null;
        }

        return turn;
    }

    /// <summary>The person's line for this round, read at most once.</summary>
    /// <param name="walk">The house their statement is about.</param>
    /// <remarks>
    /// <para>
    /// <b>Read where the moment is BUILT rather than where it is settled</b>, so what
    /// somebody said is in front of the machine when it decides what to say back. A line read
    /// after the machine had spoken would be a conversation where nobody is ever replied to.
    /// </para>
    /// <para>
    /// <b>And it joins the transcript rather than sitting beside it</b>, because a person
    /// saying <i>the apple is in the cellar</i> has told the machine something about the
    /// house. What the exam was scored on is over, so a statement here cannot restate an
    /// answer that has already been marked.
    /// </para>
    /// <para>
    /// <b>A blank line is a round with nothing in it</b>, which is somebody letting the
    /// machine carry on. The house is what the moment is about either way, so a round nobody
    /// spoke in still shows the transcript and still takes a word back.
    /// </para>
    /// </remarks>
    private void Spoke(Walk walk)
    {
        if (_heard is not null) return;

        _heard = [];

        if (Ended) return;

        // The turn is offered rather than taken silently, because a stream that reads a line
        // without saying so is one a person sits in front of not knowing it is waiting for
        // them. It is the same unterminated line a question is put on.
        _settings.Printed!.Write("  you ");
        _settings.Printed.Flush();

        var line = _settings.Typed!.ReadLine();

        _settings.Printed.WriteLine();

        if (line is null || string.Equals(line.Trim(), Over, StringComparison.Ordinal))
        {
            Ended = true;

            return;
        }

        var words = Babi.Words(line);

        if (words.Count == 0) return;

        foreach (var word in words) Heard(word);

        walk.Told.Add(_heard = Said([.. words]));
    }

    /// <summary>What the machine is looking at while the house is being talked about.</summary>
    /// <param name="walk">The house, standing still.</param>
    /// <param name="said">Its own words this round, newest of all.</param>
    /// <remarks>
    /// <b>The world's turn is the invitation.</b> Somebody has come in and the machine may
    /// speak, and a phase the signal did not mark would be one nothing outside the world
    /// could tell had started.
    /// </remarks>
    private Coded Talking(Walk walk, IReadOnlyList<Code> said)
    {
        var parts = new List<Grouped>();

        if (said.Count > 0) parts.Add(Grouped.Of(said));

        parts.AddRange(Enumerable.Reverse(walk.Told).Select(Grouped.Of));

        return Coded.From(
            parts,
            Grouped.Of(Said("what", "next")),
            said.Count > 0 ? new HashSet<Code>(said) : null,
            [.. _order.Select(name => Grouped.Of(_met[name]))]);
    }

    /// <summary>One question of the survey and the word that answers it.</summary>
    /// <param name="Sentence">What is asked, in the order the words are said.</param>
    /// <param name="Outcome">Which word of the alphabet is right.</param>
    private sealed record Question(IReadOnlyList<Code> Sentence, int Outcome);

    /// <summary>Whether the machine has been given a word for this.</summary>
    /// <param name="name">A room, a thing or a person.</param>
    /// <remarks>
    /// <b>What bounds which questions may be asked.</b> A question about a thing whose name
    /// never reached the machine is one nothing in it could answer, and an exam holding those
    /// measures how often the naming happened to land rather than what was understood.
    /// </remarks>
    private bool Worded(string name) =>
        _met.TryGetValue(name, out var codes) && codes.Contains(Kinds.Named(Word, name));

    /// <summary>The exam for the house that has just been walked.</summary>
    /// <param name="walk">The house, at the state the walk left it in.</param>
    /// <remarks>
    /// <para>
    /// <b>Three kinds, and the draw is between whichever this house can pose.</b> A
    /// house where nothing was ever named poses none, and one where every room holds two
    /// things poses no question about what a room held.
    /// </para>
    /// <para>
    /// <b>What a room held is asked of a room holding ONE thing</b>, because the
    /// answer is a single word. A room with two in it has two right answers and one answer
    /// key, so a machine that said the other one would be marked wrong for being right —
    /// which is the experimenter's knife rather than the exam.
    /// </para>
    /// <para>
    /// <b>And every answer is the state the walk ENDED in</b>, whether or not the machine was
    /// there to see it change. Somebody else moving a thing after the machine last looked
    /// makes a question unanswerable, and how much of the exam that is is a ceiling this
    /// world's own instrument takes before any learner runs — a world that only asked what
    /// the machine had seen would be an exam edited until it could be passed.
    /// </para>
    /// <para>
    /// <b>Except the fourth, whose answer is a state the walk never reached.</b> It is
    /// asked of a LOOSE thing and of somebody standing somewhere else, so the answer is
    /// where that person is and the transcript says that about the person and never about
    /// the thing — what reaches it is knowing that picking something up moves it.
    /// </para>
    /// </remarks>
    private List<Question> Survey(Walk walk)
    {
        var asked = new List<Question>();

        if (_settings.Asked == 0) return asked;

        var placed = Placed(walk.At, walk.Held, walk.Here);

        var things = Enumerable.Range(0, _settings.Props)
            .Where(prop => Worded(Things[prop]))
            .ToList();

        var rooms = Enumerable.Range(0, _settings.Rooms)
            .Where(room => Worded(Places[room]))
            .ToList();

        var alone = rooms
            .Where(room => placed.Count(at => at == room) == 1
                && Worded(Things[Array.IndexOf(placed, room)]))
            .ToList();

        // A loose thing and somebody who is NOT in the room with it. What makes the
        // consequence question askable: picking a thing up puts it where the hands are, so
        // the answer is where that person is standing and no sentence says it about the
        // thing. Somebody already in the room would make the answer where the thing
        // already is, which is the transcript's own fact wearing a consequence's clothes.
        // The body is left out because the exam is about the house rather than about the
        // machine, and nothing here names it as a person.
        var reachable = Enumerable
            .Range(0, _settings.Props)
            .Where(prop => walk.Held[prop] == Nobody && Worded(Things[prop]))
            .SelectMany(prop => Enumerable
                .Range(1, _settings.People - 1)
                .Where(who => walk.Here[who] != placed[prop]
                    && Worded(Cast[who])
                    && Worded(Places[walk.Here[who]]))
                .Select(who => (Prop: prop, Who: who)))
            .ToList();

        for (var one = 0; one < _settings.Asked; one++)
        {
            var kinds = new List<int>();

            if (things.Count > 0) kinds.Add(0);
            if (alone.Count > 0) kinds.Add(1);
            if (rooms.Count > 0) kinds.Add(2);
            if (reachable.Count > 0) kinds.Add(3);

            if (kinds.Count == 0) break;

            switch (kinds[_walks.Next(kinds.Count)])
            {
                case 0:
                {
                    var prop = things[_walks.Next(things.Count)];

                    asked.Add(new Question(
                        Said("where", "is", "the", Things[prop]),
                        _naming[Kinds.Named(Word, Places[placed[prop]])]));

                    break;
                }

                case 1:
                {
                    var room = alone[_walks.Next(alone.Count)];

                    asked.Add(new Question(
                        Said("what", "is", "in", "the", Places[room]),
                        _naming[Kinds.Named(Word, Things[Array.IndexOf(placed, room)])]));

                    break;
                }

                case 2:
                {
                    var room = rooms[_walks.Next(rooms.Count)];

                    asked.Add(new Question(
                        Said("how", "many", "in", "the", Places[room]),
                        _naming[Kinds.Named(
                            Word, Counts[placed.Count(at => at == room)])]));

                    break;
                }

                default:
                {
                    var (prop, who) = reachable[_walks.Next(reachable.Count)];

                    asked.Add(new Question(
                        Said(
                            "if", Cast[who], "took", "the", Things[prop], "where", "would",
                            "the", Things[prop], "be"),
                        _naming[Kinds.Named(Word, Places[walk.Here[who]])]));

                    break;
                }
            }
        }

        return asked;
    }

    /// <summary>The moment the survey's next question is asked in.</summary>
    /// <remarks>
    /// <b>The walk's transcript and the question over it</b>, and the questions do not join
    /// the transcript. An earlier question sitting in a later one's background would put room
    /// words the exam chose into the moment the exam is scored on, so what a question is
    /// answered against would depend on which questions came before it.
    /// </remarks>
    private Coded Surveying() =>
        Coded.From(
            [.. Enumerable.Reverse(_house!.Told).Select(Grouped.Of)],
            Grouped.Of(_asking[0].Sentence),
            things: [.. _order.Select(name => Grouped.Of(_met[name]))]);

    /// <summary>One question of the survey, asked and settled.</summary>
    /// <remarks>
    /// <b>The last question drops the house, rather than the last step</b>, because
    /// the transcript the exam is about is the house's. A machine that said something while
    /// the exam was running said it about a walk that is over, so it is dropped with the
    /// house rather than carried into the next one.
    /// </remarks>
    private Turn<Coded> Surveyed()
    {
        Sat = true;

        var turn = new Turn<Coded>
        {
            Seen = _shown ?? Surveying(),
            Outcome = _asking[0].Outcome,
        };

        // The exam in words, on the walk's narration and its standing. What a person watching
        // may not be told is what the MACHINE said, which is the brain's and never crosses
        // this seam -- so this says what was asked rather than how it went.
        if (!Ended)
            _settings.Printed?.WriteLine(
                "  = " + string.Join(
                    " ",
                    _asking[0].Sentence.Select(code => _vocabulary[_naming[code]])));

        _asking.RemoveAt(0);

        (_spoken, _shown) = (null, null);

        if (_asking.Count == 0) Opened();

        return turn;
    }

}
