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
}

/// <summary>
/// A house, a person walking round it, and things that get picked up and put down —
/// <b>TextWorld's shape, generated here rather than ported, so the ground truth can be
/// enumerated.</b>
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
/// <b>Built rather than ported, which is this repo's own rule — borrow the problem, not
/// the mechanism.</b> A benchmark varies its parser, its vocabulary, its quest length and
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
/// <b>It speaks <see cref="Recited"/></b>, which is <see cref="Asking"/> with the word order
/// still on it. The whole text front end applies unchanged — every
/// <see cref="Joining"/> arm reads <see cref="Recited.Bagged"/> and gets the codes it always
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
public sealed class Roaming : IWorld<Recited>, IWithholds<Recited>
{
    /// <summary>The modality a word rides on.</summary>
    /// <remarks>
    /// <b>Its own rather than <see cref="Babi"/>'s, because the two are different
    /// vocabularies.</b> Sharing one would make <i>kitchen</i> here and <i>kitchen</i> there
    /// the same code, and a population primed on one would be reading the other's words
    /// without anybody having decided that.
    /// </remarks>
    private const byte Word = 46;

    private static readonly string[] Places =
    [
        "kitchen", "garden", "office", "bathroom", "bedroom", "hallway", "cellar", "attic",
    ];

    private static readonly string[] Things =
    [
        "apple", "football", "milk", "book", "lamp", "kettle", "hat", "brush",
    ];

    private readonly RoamingSettings _settings;
    private readonly Random _walks;
    private readonly List<Turn<Recited>> _kept = [];

    /// <param name="settings">How the world is set up.</param>
    /// <param name="seed">What draws the houses and the walks.</param>
    public Roaming(RoamingSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Rooms, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Rooms, Places.Length);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Props);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Props, Things.Length);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Steps);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Withheld);

        _settings = settings;
        _walks = new Random(seed);

        for (var back = 0; back < settings.Withheld; back++) _kept.Add(Draw());
    }

    /// <inheritdoc/>
    public int Outcomes => _settings.Rooms;

    /// <summary>
    /// The code for each room's word, in outcome order — <b>so a ceiling can be computed
    /// from the transcript rather than from the state.</b>
    /// </summary>
    /// <remarks>
    /// <b>What it said and never what to conclude, which is the line a world stays on.</b>
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

    /// <inheritdoc/>
    public IReadOnlyList<Turn<Recited>> Withheld => _kept;

    /// <inheritdoc/>
    public Turn<Recited> Next() => Draw();

    /// <summary>The codes for one sentence, in the order the words were said.</summary>
    /// <param name="words">The words of it, in order.</param>
    /// <remarks>
    /// <b>A list rather than a set, on the licence <see cref="Recited"/> carries.</b> Order is
    /// a fact about the signal; which word is the room and which the thing is a role, and no
    /// world here may say that. A front end that wants none of the order flattens this in one
    /// call.
    /// </remarks>
    private static IReadOnlyList<Code> Said(params string[] words) =>
        [.. words.Select(word => Kinds.Named(Word, word))];

    /// <summary>One house, one walk round it, and one question about where a thing ended up.</summary>
    private Turn<Recited> Draw()
    {
        // Where everything starts, stated out loud. Without the opening placements the
        // answer is not derivable from the transcript at all, and the world would be asking
        // about something it never said.
        var at = new int[_settings.Props];
        var carried = new bool[_settings.Props];

        // Newest first, which is what `Recited` promises. So the list is built forwards and
        // reversed at the end rather than each statement being inserted at the front, which
        // is the same order written the cheap way round.
        var told = new List<IReadOnlyList<Code>>();

        for (var prop = 0; prop < _settings.Props; prop++)
        {
            at[prop] = _walks.Next(_settings.Rooms);

            told.Add(Said("the", Things[prop], "is", "in", "the", Places[at[prop]]));
        }

        var here = _walks.Next(_settings.Rooms);

        for (var step = 0; step < _settings.Steps; step++)
        {
            var holding = Enumerable.Range(0, _settings.Props).Where(one => carried[one]).ToList();

            var loose = Enumerable.Range(0, _settings.Props)
                .Where(one => !carried[one] && at[one] == here)
                .ToList();

            // Move, take or drop, and the choice is between what is possible rather than
            // between three. A walk that tried to drop what it was not holding would emit a
            // sentence the world's own state contradicts, which is a transcript nothing
            // could be scored against.
            var can = new List<int> { 0 };
            if (loose.Count > 0) can.Add(1);
            if (holding.Count > 0) can.Add(2);

            switch (can[_walks.Next(can.Count)])
            {
                case 1:
                    var taken = loose[_walks.Next(loose.Count)];

                    carried[taken] = true;

                    told.Add(Said("john", "took", "the", Things[taken]));

                    break;

                case 2:
                    var dropped = holding[_walks.Next(holding.Count)];

                    carried[dropped] = false;
                    at[dropped] = here;

                    told.Add(Said("john", "dropped", "the", Things[dropped]));

                    break;

                default:
                    here = _walks.Next(_settings.Rooms);

                    told.Add(Said("john", "went", "to", "the", Places[here]));

                    break;
            }
        }

        // ASKED ABOUT SOMETHING PUT DOWN, so the answer is a room. A thing still in hand is
        // wherever john is, which is a different question with a different ceiling, and
        // mixing the two would average two problems into one number.
        var settled = Enumerable.Range(0, _settings.Props).Where(one => !carried[one]).ToList();

        var about = settled.Count > 0
            ? settled[_walks.Next(settled.Count)]
            : _walks.Next(_settings.Props);

        told.Reverse();

        return new Turn<Recited>
        {
            Seen = new Recited
            {
                Said = told,
                Asked = Said("where", "is", "the", Things[about]),
            },
            Outcome = carried[about] ? null : at[about],
        };
    }
}
