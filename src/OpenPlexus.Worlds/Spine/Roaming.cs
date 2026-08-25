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

    /// <summary>How many things happen before the question is asked.</summary>
    /// <remarks>
    /// <b>What makes the answer move, which is the whole world.</b> At nought the opening
    /// placements are the answer and a bag reads them straight off; every step after that
    /// is a chance for the truth to change while the sentence that stated the old one is
    /// still sitting there in plain view.
    /// </remarks>
    public required int Steps { get; init; }

    /// <summary>How many questions are kept back and never drawn.</summary>
    public required int Withheld { get; init; }

    /// <inheritdoc cref="Worlds.Examining"/>
    /// <remarks>
    /// <b>Read under <see cref="Knowing.Recited"/> alone</b>, because a question about the
    /// walk is what a recital ends with and an explorer is settled step by step.
    /// </remarks>
    public required Examining Examining { get; init; }

    /// <inheritdoc cref="Worlds.Knowing"/>
    public required Knowing Knowing { get; init; }

    /// <inheritdoc cref="Worlds.Seeing"/>
    public required Seeing Seeing { get; init; }
}

/// <summary>How the machine comes to know what happened in the house.</summary>
/// <remarks>
/// <para>
/// <b>John's, and it is what the spine becoming ONE world turns on.</b> A machine recited to
/// is a machine reading somebody else's account; a machine that walks the house has to make
/// its own, and every mechanism between the two is the same one.
/// </para>
/// <para>
/// <b>The two are not arms of one question.</b> They are not scored against each other: one is
/// answered in words about a walk it was told, the other is settled by what it is shown while
/// it looks — different alphabets and different rounds, so a table holding both would be
/// comparing configurations as much as problems.
/// </para>
/// </remarks>
public enum Knowing
{
    /// <summary>The walk is recited to it whole. Every reading taken before this.</summary>
    Recited,

    /// <summary>It walks the house and is shown what is in front of it.</summary>
    Explored,
}

/// <summary>Whether what is SEEN of a thing and the word for it are one code.</summary>
/// <remarks>
/// <para>
/// <b>An arm and never a default, which is John's.</b> Shared, the crossing is free and the
/// front end has handed over the answer in the one form the codes cannot otherwise carry —
/// which is the fault <c>CeilingTests</c> exists to price. Apart, it is the same problem a
/// picture will pose, and that is the point.
/// </para>
/// <para>
/// <b>And it would otherwise be decided by whoever wrote the world first</b>, silently. A
/// world whose looks and words are one code reads as a learner that crossed two senses, and
/// nothing in a score can tell that from a front end that never had two.
/// </para>
/// <para>
/// <b>Shared also lets the machine's own words look like things.</b> A command is in the
/// moment, so a machine that said <i>garden</i> put the garden's code there whether or not it
/// can see one — the answer arriving through the action channel. That is inherent to one code
/// meaning two things rather than a fault in the wiring, and it is one more reason the arm is
/// priced rather than chosen.
/// </para>
/// </remarks>
public enum Seeing
{
    /// <summary>Two codes, so what a thing looks like and what it is called must be joined.</summary>
    Apart,

    /// <summary>One code, so a thing seen is already named.</summary>
    Shared,
}

/// <summary>
/// What the world asks about the walk it just recited — <b>where a thing ended up</b>, or
/// what the last thing it was told did.
/// </summary>
/// <remarks>
/// <para>
/// <b>The architecture line with nothing under it, given a mechanism however bad.</b> What
/// it is told must be falsifiable, and told and configured are indistinguishable from the
/// inside — so a statement the learner cannot be wrong about was installed in it rather than
/// taught to it. Under <see cref="Where"/> every statement in the transcript is background
/// the answer is read out of, and no commitment is ever about a statement.
/// </para>
/// <para>
/// <b>And it is a second QUESTION rather than a second world</b>, which is what keeps the
/// two comparable. The house, the scatter and the walk are drawn identically under both —
/// the same seed produces the same transcript — so a difference between the arms is the
/// question and cannot be the world.
/// </para>
/// <para>
/// <b>What it does not close is the store's own update rule</b>, and saying so is cheaper
/// than having it read as more than it is. The settlement is the world's ground truth about
/// its own state, so what becomes falsifiable is <i>this statement changes what is known</i>
/// and not <i>my store was right to overwrite</i>. Fork 104 wants the second and this is the
/// first, which is the half that needs no new machinery.
/// </para>
/// </remarks>
public enum Examining
{
    /// <summary>Where a thing ended up. Every reading taken before this existed.</summary>
    Where,

    /// <summary>
    /// Whether the statement the walk ended on moved anything.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The verb decides two of the three cases</b> and the store decides the third. A
    /// thing taken is where its holder already stood and a thing dropped lands in the room
    /// the dropper is already in, so <i>took</i> and <i>dropped</i> never move anything.
    /// <i>Went</i> moves a thing exactly when the walker is carrying one and the room is
    /// different — which needs the transcript read backwards to a take that has no word in
    /// common with the question.
    /// </para>
    /// <para>
    /// <b>So the verb-only ceiling is what this is read against</b>, and it is computed
    /// before any learner runs. A conjunctive rule over the question's own words reaches
    /// <i>took</i> and <i>dropped</i> for nothing, and everything above that ceiling is
    /// binding. If the learner lands on it, the reading is that the headroom here is rung
    /// four's — which is a finding rather than a failure, and this repo's own rule is that
    /// a front-end ceiling costing milliseconds is taken FIRST.
    /// </para>
    /// </remarks>
    Effect,
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
public sealed class Roaming : IWorld<Coded>, IWithholds<Coded>, IActed<Coded>
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
    /// <b>Unused under <see cref="Seeing.Shared"/></b>, where a look is the word. That is the
    /// whole of the arm: one modality or two, with the same house behind it.
    /// </remarks>
    private const byte Look = 48;

    /// <summary>The modality a thing's SHADE rides on.</summary>
    /// <remarks>
    /// <b>Its own under both arms</b>, because the arm is about a look and a name rather than
    /// about how much of a thing a sense reports. A shade that followed the sharing would
    /// move two things at once.
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
    private readonly List<Turn<Coded>> _kept = [];

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

    /// <summary>The walk drawn as far as its last step, waiting to be told what it is.</summary>
    private Walk? _open;

    /// <summary>What the machine said about this moment, or nothing where it said none.</summary>
    private List<int>? _spoken;

    // The house being walked, how many steps of it are left, and what has been met in it.
    // Explored only: a recital opens and closes a house inside one turn and needs none of it.
    private Walk? _house;
    private int _left;

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
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Steps);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Withheld);

        // The effect question is about a STEP, and a walk of no steps ends on a placement.
        // Asking what a placement did is asking about the round before the world started,
        // which is a question with no answer rather than an answer of nought.
        if (settings.Examining == Examining.Effect)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Steps);

        if (settings.Knowing is Knowing.Explored)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Steps);

            // Both refused rather than ignored, because a setting read by nothing is a
            // setting somebody will read a result off. An explorer is settled by what it is
            // shown at each step, so there is no question at the end of a house to choose
            // between and no held-out one to draw -- the survey is the item after this and
            // does not exist yet.
            if (settings.Examining != Examining.Where)
                throw new ArgumentException(
                    "an explored house asks no question at the end of the walk, so the "
                    + "examining arm decides nothing here", nameof(settings));

            if (settings.Withheld != 0)
                throw new ArgumentException(
                    "an explored house has no question to hold back: a withheld one would "
                    + "be about a house the machine never walked, which nothing could "
                    + "answer. The survey is what makes an exam of this", nameof(settings));
        }

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

        // Round the palette over everything there is, so a room, a prop and a person can wear
        // one shade and no thing has one to itself.
        var everything = Places.Concat(Things).Concat(Cast).ToList();

        for (var thing = 0; thing < everything.Count; thing++)
            _shades[everything[thing]] =
                Kinds.Named(Shade, Shades[thing % Shades.Length]);

        for (var back = 0; back < settings.Withheld; back++) _kept.Add(Draw());
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Two under <see cref="Examining.Effect"/></b>, because what a blind guess is against
    /// is the answer alphabet and the effect question's is <i>moved</i> and <i>did not</i>.
    /// A world reporting its room count there would price every arm against the wrong bar.
    /// </remarks>
    public int Outcomes => _settings.Knowing is Knowing.Explored
        ? _vocabulary.Count
        : _settings.Examining == Examining.Effect ? 2 : _settings.Rooms;

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

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Empty under <see cref="Knowing.Explored"/></b>, and the constructor refuses a
    /// setting that asks for otherwise. A held-out question there would be about a house the
    /// machine never walked.
    /// </remarks>
    public IReadOnlyList<Turn<Coded>> Withheld => _kept;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>The walk <see cref="Now"/> opened, finished by whatever <see cref="Do"/> was told</b>
    /// — and drawn whole here where nothing asked. A chooser is spent by the step that
    /// follows it, so neither the walk nor the wish survives into the next episode.
    /// </remarks>
    public Turn<Coded> Next()
    {
        if (_settings.Knowing is Knowing.Explored) return Walked();

        var walk = _open ?? Open();
        var said = _spoken;

        (_open, _spoken, _shown) = (null, null, null);

        return Close(walk, said);
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
    /// </remarks>
    public bool Listening =>
        _spoken is { Count: > 0 } spoken
        && spoken.Count < Patience
        && Parse(spoken) is null;

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>The walk with its last step still to happen</b>, so what is read is the state an
    /// action is taken in rather than one it has already been taken in. Asking for it draws
    /// the house, the scatter and every step but the last, and the same walk is what
    /// <see cref="Next"/> then finishes — a caller that never asks gets the walk drawn whole
    /// on the spot and the identical sequence out of the generator either way.
    /// </para>
    /// <para>
    /// <b>The question slot says what is being asked</b>, never what to conclude. Which
    /// thing the walk will be asked about is not drawn yet under
    /// <see cref="Examining.Where"/>, and putting it here would show the chooser a question
    /// the learner has not been asked.
    /// </para>
    /// </remarks>
    public Coded Now
    {
        get
        {
            if (_shown is { } already) return already;

            if (_settings.Knowing is Knowing.Explored)
                return (_shown = Sighted(_house ??= Housed(), null)).Value;

            _open ??= Open();

            var asked = Said("what", "next");

            return (_shown = Coded.From(
                [.. Enumerable.Reverse(_open.Told).Select(Grouped.Of)],
                Grouped.Of(asked),
                things: Grouped.Things([.. _open.Told, asked], _nouns))).Value;
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

    /// <summary>The house, the scatter, and every step but the one still to be chosen.</summary>
    private Walk Open()
    {
        // Where everything starts, stated out loud. Without the opening placements the
        // answer is not derivable from the transcript at all, and the world would be asking
        // about something it never said.
        var at = new int[_settings.Props];

        // Who is holding each thing, or nobody. A flag would have been enough for one person
        // and says the wrong thing for two: a thing in a hand is somewhere, and which hand it
        // is in is what the room it lands in depends on.
        var held = new int[_settings.Props];

        Array.Fill(held, Nobody);

        // Newest first, which is what a moment's parts promise. So the list is built forwards and
        // reversed at the end rather than each statement being inserted at the front, which
        // is the same order written the cheap way round.
        var told = new List<IReadOnlyList<Code>>();

        for (var prop = 0; prop < _settings.Props; prop++)
        {
            at[prop] = _walks.Next(_settings.Rooms);

            told.Add(Said("the", Things[prop], "is", "in", "the", Places[at[prop]]));
        }

        var here = new int[_settings.People];

        // And where each person starts, stated out loud for the reason the things' placements
        // are. A person who picks a thing up and puts it down before ever moving has put it
        // somewhere no rule could name, so five thousandths of the questions at four people
        // were unanswerable by anything -- measured, not supposed. Saying it costs one
        // statement a person and closes the hole.
        //
        // The draw is unchanged and so is every house and every walk: this consumes no
        // randomness, it says out loud what was already drawn. What moves is the transcript.
        for (var one = 0; one < _settings.People; one++)
        {
            here[one] = _walks.Next(_settings.Rooms);

            told.Add(Said(Cast[one], "is", "in", "the", Places[here[one]]));
        }

        var walk = new Walk(at, held, here, told);

        // Every step but the last, because the last is the one an action gets to be. A walk
        // of no steps has no last step and this loop runs nowhere, which is the same walk it
        // always was.
        for (var step = 0; step < _settings.Steps - 1; step++) Step(walk, null);

        return walk;
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
    /// <b>And it is said out loud only where the walk is RECITED.</b> An explorer is shown
    /// what its step left in front of it rather than told what it did, so a sentence there
    /// would be the world narrating the machine to itself.
    /// </para>
    /// </remarks>
    private void Step(Walk walk, IReadOnlyList<int>? spoken, int? walker = null)
    {
        var (at, held, here, told) = walk;

        // Whose turn it is, and one person is nobody to choose between. A draw over one
        // option decides nothing, so it is not taken -- which also leaves the walk of a
        // one-person house the walk every earlier reading was taken on.
        var who = walker
            ?? (_settings.People == 1 ? 0 : _walks.Next(_settings.People));

        var saying = _settings.Knowing is Knowing.Recited;

        if (spoken is not null)
        {
            var sentence = Parse(spoken) is { } wanted && Possible(walk, who, wanted)
                ? Done(walk, who, wanted)
                : Said(Cast[who], "waited");

            if (saying) told.Add(sentence);

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

        var said = Done(walk, who, drawn);

        if (saying) told.Add(said);
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
    /// <b>Its KIND's under <see cref="Seeing.Apart"/> and its own under
    /// <see cref="Seeing.Shared"/></b>, where the look IS the word. Sharing a look there
    /// would give two things one name and shrink the answer alphabet, which is a different
    /// world rather than the same one seen differently.
    /// </remarks>
    private Code Seen(string name) =>
        _settings.Seeing is Seeing.Shared
            ? Kinds.Named(Word, name)
            : Kinds.Named(Look, Alike(name));

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
    /// name the same thing rather than two. Under <see cref="Seeing.Shared"/> they are one
    /// code and a part holds one; apart, a thing met and then named holds two, and a scope
    /// over it is a scope about one thing.
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
    /// the machine had to get right is which. Under <see cref="Seeing.Shared"/> the answer is
    /// already in the moment, because a look IS the word — which is what makes the arm an arm
    /// and what <c>CeilingTests</c> is for.
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

        if (--_left <= 0) _house = null;

        return turn;
    }

    /// <summary>One house, one walk round it, and one question about what happened in it.</summary>
    private Turn<Coded> Draw() => Close(Open(), null);

    /// <summary>The walk's last step, and the question about what it left behind.</summary>
    /// <param name="walk">The house drawn as far as its last step.</param>
    /// <param name="spoken">What the machine said, or nothing to let the walk draw.</param>
    private Turn<Coded> Close(Walk walk, IReadOnlyList<int>? spoken)
    {
        var (at, held, here, told) = walk;

        // Whether the walk's LAST statement moved anything, which is the effect question's
        // whole answer. Taken around that one step rather than around every one: the vector
        // costs a pass over the props and only the final step is ever asked about.
        var moved = false;

        // A wish with no step to spend it on marked nothing, which is the difference between
        // a chooser that was asked and one that was heard. `Open` adds the placements before
        // any walking, so without this the opening statement of a no-step walk would be
        // reported as the learner's doing.
        var chose = spoken is not null && _settings.Steps > 0;

        if (_settings.Steps > 0)
        {
            var before = Placed(at, held, here);

            Step(walk, spoken);

            moved = !before.SequenceEqual(Placed(at, held, here));
        }

        // Asked about something put down, so the answer is a room. A thing still in hand is
        // wherever its holder is, which is a different question with a different ceiling, and
        // mixing the two would average two problems into one number.
        var settled = Enumerable.Range(0, _settings.Props)
            .Where(one => held[one] == Nobody)
            .ToList();

        // Drawn under both arms and used by one, which is what keeps the two comparable. The
        // effect question has no thing to pick, and skipping the draw would leave the walks
        // aligned for one episode and diverging from the second -- two transcripts differing
        // by one draw read identically from every column, so nothing else here could say so.
        var about = settled.Count > 0
            ? settled[_walks.Next(settled.Count)]
            : _walks.Next(_settings.Props);

        if (_settings.Examining == Examining.Effect)
        {
            // The statement the walk ended on is the QUESTION rather than the last line of
            // the transcript, so the learner is answering about something it is being told
            // now and not about something it has already read. Leaving it in `Said` as well
            // would put the answer's own sentence in the background, which is the shape a
            // corpus containing its own answer has.
            var last = told[^1];

            told.RemoveAt(told.Count - 1);
            told.Reverse();

            return new Turn<Coded>
            {
                // The step the learner chose, said in the words it came out as. A wish the
                // house could not grant comes out as waiting, and that sentence is as much
                // the learner's doing as a granted one -- so what is marked is the statement
                // the choice PRODUCED rather than the verb it asked for.
                Seen = Coded.From(
                    [.. told.Select(Grouped.Of)],
                    Grouped.Of(last),
                    chose ? new HashSet<Code>(last) : null,
                    Grouped.Things([.. told, last], _nouns)),
                Outcome = moved ? 1 : 0,
            };
        }

        told.Reverse();

        var asking = Said("where", "is", "the", Things[about]);

        return new Turn<Coded>
        {
            // The chosen step is a STATEMENT here rather than the question, so what is
            // marked is the words of it wherever they ended up. A `where is` question is
            // about the state the walk left behind and names nothing the learner did.
            Seen = Coded.From(
                [.. told.Select(Grouped.Of)],
                Grouped.Of(asking),
                chose ? new HashSet<Code>(told[0]) : null,
                Grouped.Things([.. told, asking], _nouns)),
            Outcome = held[about] == Nobody ? at[about] : null,
        };
    }
}
