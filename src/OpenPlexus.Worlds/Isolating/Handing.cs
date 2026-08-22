using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>How the handing world is set up. Every number named and none defaulted.</summary>
public sealed record HandingSettings
{
    /// <summary>How many people are in the room.</summary>
    /// <remarks>
    /// <b>The answer alphabet</b>, so it is the marginal's denominator and the first ceiling's
    /// value. Two people would make a coin-flip and the whole ladder one step high;
    /// what the world is for is the DISTANCE between guessing at the marginal, guessing
    /// inside the right sentence, and reading it, so there has to be room between them.
    /// </remarks>
    /// <remarks>
    /// <b>And it is the number of things too</b>, which is a constraint rather than a
    /// shorthand. One sentence a thing, one person giving in each and one receiving,
    /// means the sentences use every person exactly twice — and THAT is what makes the
    /// story's word set identical in every draw. Fewer things than people would leave
    /// somebody out of some stories and not others, and how often a word appears is exactly
    /// what a bag can read, so the first ceiling would stop being the marginal and every
    /// number off this world would be arguable. A second dial here would be a way to break
    /// the world silently.
    /// </remarks>
    public required int People { get; init; }

    /// <summary>How many questions are kept back and never drawn.</summary>
    public required int Withheld { get; init; }
}

/// <summary>
/// People handing things to people, and a question about who ended up with one —
/// <b>a world built so that a bag of words cannot win</b>. That is a fact about
/// the world rather than a finding about a run.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's fork 105, isolated. Is a relation just a category?</b> Half yes: a category
/// over the arguments says <i>people hand things to people</i> more narrowly than a hole
/// does, and it retires most of rung four. What it cannot reach is BINDING — a subset test
/// over a sentence holding two people cannot say which of them is the one who now has the
/// thing, however either of them is named. This world is that sentence and nothing else.
/// </para>
/// <para>
/// <b>The ladder has three rungs</b> and all three ceilings are exact by construction. That is
/// the whole reason to generate a world rather than read one.
/// </para>
/// <list type="number">
/// <item><b>A bag of the story's words reaches 1/<see cref="HandingSettings.People"/></b>,
/// which is the marginal. The givers are a permutation of the people and the takers are
/// another, so every draw emits the identical set of words and a bag has nothing to be
/// conditioned on at all.</item>
/// <item><b>Picking the right SENTENCE reaches one half.</b> The question names the thing
/// and exactly one sentence mentions it, so which sentence is decidable by overlap with no
/// learning — and that sentence names two people, one of whom is the answer.</item>
/// <item><b>Reading the ROLE reaches one.</b> The answer is a deterministic function of the
/// ordered sentence, so anything above a half is binding and nothing else.</item>
/// </list>
/// <para>
/// <b>So the middle rung is fork 88 arriving with something left over.</b> And that is the
/// point of the design. Intersecting the question with each statement is settled: it
/// answers bAbI's first task where the bag sits near the marginal. Here it is worth exactly
/// one half and not one, so the two mechanisms are separated by a world instead of by an
/// argument — a run that lands on 0.5 has selected and not bound, and a run that lands on
/// 1.0 has done both.
/// </para>
/// <para>
/// <b>And the template is deliberately the dullest English that carries a role.</b> The
/// role is carried by word order and by <i>to</i>, which is how English carries it, so a
/// rule that finds it is a rule about the signal. Handing over a role LABEL would be the
/// forbidden index in a different hat — the same standing as <c>ReturningTests</c>' tagged
/// cell, which exists to be the far end of a gap and may never ship.
/// </para>
/// <para>
/// <b>Watched rather than acted in, and speaking text on purpose</b>, for
/// <see cref="Roaming"/>'s reasons exactly: action and goals are both unbuilt, and a
/// reading that cannot be put beside the other text worlds is a second scale nobody can
/// compare across.
/// </para>
/// </remarks>
public sealed class Handing : IWorld<Coded>, IWithholds<Coded>
{
    /// <summary>The modality a word rides on.</summary>
    /// <remarks>
    /// <b>Its own rather than <see cref="Roaming"/>'s, for <see cref="Roaming"/>'s own
    /// reason.</b> Sharing one would make <i>the</i> here and <i>the</i> there the same
    /// code, and a population primed on one would be reading the other's words without
    /// anybody having decided that.
    /// </remarks>
    private const byte Word = 47;

    private static readonly string[] Cast =
    [
        "mary", "john", "sandra", "daniel", "fred", "julie", "bill", "emma",
    ];

    private static readonly string[] Objects =
    [
        "apple", "football", "milk", "book", "lamp", "kettle", "hat", "brush",
    ];

    private readonly HandingSettings _settings;
    private readonly Random _draws;
    private readonly List<Turn<Coded>> _kept = [];

    /// <param name="settings">How the world is set up.</param>
    /// <param name="seed">What draws the people, the things and the order.</param>
    public Handing(HandingSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.People, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.People, Cast.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.People, Objects.Length);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Withheld);

        _settings = settings;
        _draws = new Random(seed);

        for (var back = 0; back < settings.Withheld; back++) _kept.Add(Draw());
    }

    /// <inheritdoc/>
    public int Outcomes => _settings.People;

    /// <summary>
    /// The code for each person's word, in outcome order — <b>so a ceiling comes off the transcript</b>
    /// rather than off the state.
    /// </summary>
    /// <remarks>
    /// <b>What it said and never what to conclude</b>, which is
    /// <see cref="Roaming.Named"/>'s standing exactly. A probe asking <i>which people does
    /// this sentence mention</i> has to know which codes are people, and that is a fact
    /// about the vocabulary this world emitted. Nothing that learns is ever shown it.
    /// </remarks>
    public IReadOnlyList<Code> Called =>
        [.. Cast.Take(_settings.People).Select(one => Kinds.Named(Word, one))];

    /// <summary>The code for each thing's word, in thing order. <b>The same standing.</b></summary>
    public IReadOnlyList<Code> Handed =>
        [.. Objects.Take(_settings.People).Select(one => Kinds.Named(Word, one))];

    /// <inheritdoc/>
    public IReadOnlyList<Turn<Coded>> Withheld => _kept;

    /// <inheritdoc/>
    public Turn<Coded> Next() => Draw();

    /// <summary>The codes for one sentence, in the order the words were said.</summary>
    /// <param name="words">The words of it, in order.</param>
    private static IReadOnlyList<Code> Say(params string[] words) =>
        [.. words.Select(word => Kinds.Named(Word, word))];

    /// <summary>A permutation of the first <paramref name="many"/> whole numbers.</summary>
    /// <param name="many">How long it is.</param>
    private int[] Shuffled(int many)
    {
        var order = Enumerable.Range(0, many).ToArray();

        for (var at = many - 1; at > 0; at--)
        {
            var with = _draws.Next(at + 1);

            (order[at], order[with]) = (order[with], order[at]);
        }

        return order;
    }

    /// <summary>One room, one round of handing over, and one question about who has what.</summary>
    private Turn<Coded> Draw()
    {
        // Givers and takers are both permutations, which is what makes the first ceiling a
        // fact rather than a measurement. Drawing each pair independently would let a
        // person turn up more often than another, and how often a word appears is exactly
        // what a bag CAN read -- so the marginal would stop being the bag's ceiling and
        // every number off this world would be arguable.
        var givers = Shuffled(_settings.People);
        var takers = Shuffled(_settings.People);

        // And nobody hands a thing to themself, so the two permutations are redrawn until
        // they disagree everywhere it matters. A sentence naming one person twice would
        // hold ONE candidate rather than two, and the middle rung's one half would be an
        // average over sentences of two different shapes.
        while (Enumerable.Range(0, _settings.People).Any(one => givers[one] == takers[one]))
            takers = Shuffled(_settings.People);

        var order = Shuffled(_settings.People);
        var told = new List<IReadOnlyList<Code>>();

        // Shuffled, so that which sentence answers the question is not its position. A
        // fixed order would be answerable by counting from the end, which is recency and
        // has been measured elsewhere -- it would put a second mechanism in a world built
        // to hold exactly one.
        foreach (var thing in order)
        {
            told.Add(Say(
                Cast[givers[thing]], "gave", "the", Objects[thing], "to", Cast[takers[thing]]));
        }

        var about = _draws.Next(_settings.People);

        // NEWEST FIRST, WHICH IS WHAT A MOMENT'S PARTS PROMISE, and it carries no information
        // here because the order was already shuffled.
        told.Reverse();

        return new Turn<Coded>
        {
            Seen = Coded.From(
                [.. told.Select(Grouped.Of)],
                Grouped.Of(Say("who", "has", "the", Objects[about]))),
            Outcome = takers[about],
        };
    }
}
