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
    public required Examining Examining { get; init; }
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
/// <b>WHAT IT FIXES ABOUT bAbI is the property that disqualified it.</b> Some two thousand
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

    private readonly RoamingSettings _settings;
    private readonly Random _walks;
    private readonly List<Turn<Coded>> _kept = [];

    /// <summary>The walk drawn as far as its last step, waiting to be told what it is.</summary>
    private Walk? _open;

    /// <summary>What the chooser asked for, or nothing where it declined to intervene.</summary>
    private int? _wish;

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

        _settings = settings;
        _walks = new Random(seed);

        for (var back = 0; back < settings.Withheld; back++) _kept.Add(Draw());
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Two under <see cref="Examining.Effect"/></b>, because what a blind guess is against
    /// is the answer alphabet and the effect question's is <i>moved</i> and <i>did not</i>.
    /// A world reporting its room count there would price every arm against the wrong bar.
    /// </remarks>
    public int Outcomes =>
        _settings.Examining == Examining.Effect ? 2 : _settings.Rooms;

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
    public IReadOnlyList<Turn<Coded>> Withheld => _kept;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>The walk <see cref="Now"/> opened, finished by whatever <see cref="Do"/> was told</b>
    /// — and drawn whole here where nothing asked. A chooser is spent by the step that
    /// follows it, so neither the walk nor the wish survives into the next episode.
    /// </remarks>
    public Turn<Coded> Next()
    {
        var walk = _open ?? Open();
        var wish = _wish;

        (_open, _wish) = (null, null);

        return Close(walk, wish);
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

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Go, take or drop</b>, which is the verb set the scripted walk already draws from.
    /// Who does it and what they do it to stay the world's, because a chooser reading codes
    /// cannot know which thing is loose in the room somebody is standing in — and a world
    /// that let it name one would be answering the question through the action channel.
    /// </remarks>
    public int Doings => 3;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Never, because a doing here is a STEP</b>, and a moment is where the walker stands.
    /// Two steps about one moment is two moments with the middle one unreported, so listening
    /// twice would hide half the walk from the learner while the chooser saw all of it.
    /// </remarks>
    public bool Listening => false;

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
            _open ??= Open();

            return Coded.From(
                [.. Enumerable.Reverse(_open.Told).Select(Grouped.Of)],
                Grouped.Of(Said("what", "next")));
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
        if (doing is { } wanted)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(wanted, nameof(doing));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(wanted, Doings, nameof(doing));
        }

        _wish = doing;
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

    /// <summary>One step of the walk, chosen or drawn.</summary>
    /// <param name="walk">The house and where everything in it stands.</param>
    /// <param name="wish">Which of <see cref="Doings"/>, or nothing to let the walk draw.</param>
    private void Step(Walk walk, int? wish)
    {
        var (at, held, here, told) = walk;

        // Whose turn it is, and one person is nobody to choose between. A draw over one
        // option decides nothing, so it is not taken -- which also leaves the walk of a
        // one-person house the walk every earlier reading was taken on.
        var who = _settings.People == 1 ? 0 : _walks.Next(_settings.People);

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

        // And a CHOSEN one is not filtered, because filtering it would hand a chooser the
        // world's knowledge of what is possible through the back of the interface. What an
        // impossible wish gets is a step that happens and does nothing.
        var doing = wish ?? can[_walks.Next(can.Count)];

        if (wish is not null && !can.Contains(doing))
        {
            told.Add(Said(Cast[who], "waited"));

            return;
        }

        switch (doing)
        {
            case 1:
                var taken = loose[_walks.Next(loose.Count)];

                held[taken] = who;

                told.Add(Said(Cast[who], "took", "the", Things[taken]));

                break;

            case 2:
                var dropped = holding[_walks.Next(holding.Count)];

                held[dropped] = Nobody;
                at[dropped] = here[who];

                told.Add(Said(Cast[who], "dropped", "the", Things[dropped]));

                break;

            default:
                here[who] = _walks.Next(_settings.Rooms);

                told.Add(Said(Cast[who], "went", "to", "the", Places[here[who]]));

                break;
        }
    }

    /// <summary>One house, one walk round it, and one question about what happened in it.</summary>
    private Turn<Coded> Draw() => Close(Open(), null);

    /// <summary>The walk's last step, and the question about what it left behind.</summary>
    /// <param name="walk">The house drawn as far as its last step.</param>
    /// <param name="wish">Which of <see cref="Doings"/>, or nothing to let the walk draw.</param>
    private Turn<Coded> Close(Walk walk, int? wish)
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
        var chose = wish is not null && _settings.Steps > 0;

        if (_settings.Steps > 0)
        {
            var before = Placed(at, held, here);

            Step(walk, wish);

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
                    chose ? new HashSet<Code>(last) : null),
                Outcome = moved ? 1 : 0,
            };
        }

        told.Reverse();

        return new Turn<Coded>
        {
            // The chosen step is a STATEMENT here rather than the question, so what is
            // marked is the words of it wherever they ended up. A `where is` question is
            // about the state the walk left behind and names nothing the learner did.
            Seen = Coded.From(
                [.. told.Select(Grouped.Of)],
                Grouped.Of(Said("where", "is", "the", Things[about])),
                chose ? new HashSet<Code>(told[0]) : null),
            Outcome = held[about] == Nobody ? at[about] : null,
        };
    }
}
