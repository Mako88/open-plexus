using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>How the handing world is set up. Every number named and none defaulted.</summary>
public sealed record HandingSettings
{
    /// <summary>How many people are in the room.</summary>
    /// <remarks>
    /// <b>The answer alphabet, so it is the marginal's denominator and the first ceiling's
    /// value.</b> Two people would make a coin-flip and the whole ladder one step high;
    /// what the world is for is the DISTANCE between guessing at the marginal, guessing
    /// inside the right sentence, and reading it, so there has to be room between them.
    /// </remarks>
    /// <remarks>
    /// <b>And it is the number of things too, which is a constraint rather than a
    /// shorthand.</b> One sentence a thing, one person giving in each and one receiving,
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
/// A moment of text with the WORD ORDER of each sentence still on it — <b>the one fact
/// <see cref="Asking"/> throws away, and the fact roles are carried by.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A sentence is a sequence</b> and every text arm here reads it as a set, which is where
/// the binding dies. <i>mary gave the book to john</i> and <i>john gave the book to
/// mary</i> hold the same words, so no scope over a bag can tell them apart however the
/// bag is coded — and that is not a limitation of the learner, it is the front end
/// destroying the answer one call before anything could use it.
/// </para>
/// <para>
/// <b>Order is a fact about the signal and not a conclusion.</b> Which is the line this stays
/// the right side of. <see cref="Codes.Coded.Sequence"/> and
/// <see cref="IQuantizer{TFrame}.Order"/> already carry exactly this licence for every
/// other sense; <see cref="Asking"/> carries it ACROSS sentences and not WITHIN one, which
/// is an omission rather than a principle. What no world here may say is which word is the
/// giver — that is a role, and a role is what has to be learnt.
/// </para>
/// <para>
/// <b>AND <see cref="Bagged"/> is one property call.</b> So the arm that loses it is the
/// control rather than a different world. Every <see cref="Joining"/> arm, every
/// ceiling and the withheld exam apply unchanged through it, so a reading here stands
/// beside <see cref="Roaming"/>'s rather than starting a second scale.
/// </para>
/// </remarks>
public readonly record struct Recited
{
    /// <summary>The sentences in front of the question, newest first, words in order.</summary>
    public required IReadOnlyList<IReadOnlyList<Code>> Said { get; init; }

    /// <summary>The words of the question itself, in order.</summary>
    public required IReadOnlyList<Code> Asked { get; init; }

    /// <summary>
    /// Which of these words came from a step the learner CHOSE, or nothing where it chose
    /// none.
    /// </summary>
    /// <remarks>
    /// <b>What a world is allowed to say about provenance</b>, and it is a fact about the
    /// signal in the same way order is. The world says which words it was handed rather than
    /// drew; what that entails is the learner's to derive, and
    /// <see cref="Codes.Intervened"/> is where that happens.
    /// </remarks>
    public IReadOnlySet<Code>? Assigned { get; init; }

    /// <summary>The same moment with the order thrown away.</summary>
    /// <remarks>
    /// <b>What every existing text arm reads, and the ceiling it imposes is the marginal
    /// exactly.</b> <see cref="Handing"/> draws its givers and its takers as permutations,
    /// so the set of words in a story is the SAME SET in every draw — which makes the first
    /// rung of the ladder provable by construction rather than measured and argued over.
    /// </remarks>
    public Asking Bagged => new()
    {
        Story = [.. Said.Select(one => (IReadOnlySet<Code>)new HashSet<Code>(one))],
        Question = new HashSet<Code>(Asked),
    };
}

/// <summary>
/// People handing things to people, and a question about who ended up with one —
/// <b>a world built so that a bag of words cannot beat the marginal.</b> That is a fact about
/// the world rather than a finding about a run.
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S FORK 105, ISOLATED. Is a relation just a category?</b> Half yes: a category
/// over the arguments says <i>people hand things to people</i> more narrowly than a hole
/// does, and it retires most of rung four. What it cannot reach is BINDING — a subset test
/// over a sentence holding two people cannot say which of them is the one who now has the
/// thing, however either of them is named. This world is that sentence and nothing else.
/// </para>
/// <para>
/// <b>The ladder has three rungs and all three ceilings are exact by construction.</b> That is
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
public sealed class Handing : IWorld<Recited>, IWithholds<Recited>
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
    private readonly List<Turn<Recited>> _kept = [];

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
    /// The code for each person's word, in outcome order — <b>so a ceiling can be computed
    /// from the transcript rather than from the state.</b>
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
    public IReadOnlyList<Turn<Recited>> Withheld => _kept;

    /// <inheritdoc/>
    public Turn<Recited> Next() => Draw();

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
    private Turn<Recited> Draw()
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

        // NEWEST FIRST, WHICH IS WHAT `Recited` PROMISES, and it carries no information
        // here because the order was already shuffled.
        told.Reverse();

        return new Turn<Recited>
        {
            Seen = new Recited
            {
                Said = told,
                Asked = Say("who", "has", "the", Objects[about]),
            },
            Outcome = takers[about],
        };
    }
}
