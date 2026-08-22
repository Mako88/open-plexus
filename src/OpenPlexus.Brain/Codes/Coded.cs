namespace OpenPlexus.Codes;

/// <summary>
/// Codes a world says are one part of a moment — <b>a sentence, an object, a reading.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A list rather than a set</b>, because order inside a part is a fact some worlds have
/// and none of them could state. A story's words came in an order and a scene's do not, so
/// the ones that have nothing to say hand over whatever order they built and nothing reads
/// it.
/// </para>
/// <para>
/// <b>And it is why a part is a type</b> rather than a group number. A dictionary from code
/// to group says one code belongs to one part, and a word appearing in two statements is
/// the ordinary case in every text world there is — which is the whole of why those worlds
/// each wrote their own moment record instead of using this one.
/// </para>
/// </remarks>
public readonly record struct Grouped
{
    /// <summary>The codes of this part, in order where the order means anything.</summary>
    public required IReadOnlyList<Code> Codes { get; init; }

    /// <summary>One part out of some codes.</summary>
    /// <param name="codes">What is in it.</param>
    public static Grouped Of(IEnumerable<Code> codes) => new() { Codes = [.. codes] };

    /// <summary>The parts a code-to-group report describes, or nothing where it said none.</summary>
    /// <param name="grouped">Which thing each code belongs to.</param>
    /// <remarks>
    /// <para>
    /// <b>Here rather than in each world</b>, because two of them wrote it and a third would
    /// have. A world knows which object it drew a code for and the dictionary is the shape
    /// that falls out of drawing; the parts are the shape a front end reports, and turning one
    /// into the other is neither world's business.
    /// </para>
    /// <para>
    /// <b>Ordered by group and then by code</b>, so two machines reading one report build the
    /// identical parts. A dictionary's order does not survive a run, and a part compares by
    /// what it holds in the order it holds it.
    /// </para>
    /// <para>
    /// <b>Partial is normal.</b> A world segments what it can and leaves the rest out, so the
    /// parts are what it can say about the moment's shape rather than a partition of it.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<Grouped>? Parts(IReadOnlyDictionary<Code, int>? grouped) =>
        grouped is not { Count: > 0 }
            ? null
            :
            [
                .. grouped
                    .GroupBy(one => one.Value)
                    .OrderBy(one => one.Key)
                    .Select(one => Of(one.Select(each => each.Key).Order())),
            ];

    /// <summary>Whether two parts hold the same codes in the same order.</summary>
    /// <param name="other">The other part.</param>
    /// <remarks>
    /// <b>By what it holds and not by which list holds it</b>, which the compiler will not do
    /// on its own. A record struct over a list compares the REFERENCE, so two parts built
    /// separately out of the same words are never equal — this repo's own trap, already paid
    /// for once on an <c>ImmutableArray</c> in a report, and a type whose whole content is a
    /// list is where it fires next.
    /// </remarks>
    public bool Equals(Grouped other) =>
        ReferenceEquals(Codes, other.Codes) || Codes.SequenceEqual(other.Codes);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var code in Codes) hash.Add(code);

        return hash.ToHashCode();
    }
}

/// <summary>
/// A world's output when the world's output is already codes — <b>the shape a
/// constructed world hands over,</b> and the reason it needs no quantiser of its
/// own.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nine worlds each wrote their own front end</b>, and eight of them did nothing.
/// <c>Seeing</c>, <c>Feeling</c> twice, <c>Hearing</c>, <c>Looking</c>,
/// <c>Reading</c> and three <c>Passthrough</c>s — private nested classes returning
/// the observation they were handed. That is not nine front ends; it is one
/// missing type copied nine times, and while it sat inside the worlds there was
/// nowhere for a real quantiser to live either.
/// </para>
/// <para>
/// <b>A world with no signal is a legitimate world</b>, and also a limit — John,
/// 2026-08-05. <c>Worlds.Motif</c>'s task is compression of token sets
/// and <c>Worlds.Senses</c>'s is the sight–sound pairing; rendering either
/// as pixels would measure a different thing. So feeding codes straight in stays
/// available on purpose. <b>What it costs</b> is that such a world can never tell
/// anybody whether the quantisers work, which is worth knowing before the
/// payoff of a quantiser is estimated from a suite where five of nine worlds
/// cannot exercise one.
/// </para>
/// <para>
/// <b>The optional three are not passthrough</b> and that is why they are here.
/// Grouping and fleetingness are things ONLY a front end can know — see
/// <see cref="IQuantizer{TObservation}.Bind"/> — and four worlds do know them. A
/// world saying <i>these codes were one object</i> is stating a fact about its
/// signal, which is allowed; a world saying how finely to band it is deciding how
/// the brain thinks, which is not.
/// </para>
/// </remarks>
public readonly record struct Coded
{
    /// <summary>The codes present in this observation.</summary>
    public required IReadOnlyCollection<Code> Codes { get; init; }

    /// <summary>
    /// The parts of the moment, in order, or nothing where the world cannot say.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A list of parts rather than a code-to-group dictionary</b>, which is the shape
    /// three worlds each rebuilt for themselves because the dictionary could not hold what
    /// they had. One code belongs to one group in a dictionary, and a word said in two
    /// statements is what every text world is made of.
    /// </para>
    /// <para>
    /// <b>Partial where a world groups some of what it shows</b>, so a scene may segment its
    /// objects and leave the question's codes in no part at all. <see cref="Codes"/> is what
    /// the moment IS and this is what the world can say about its shape, so the two are not
    /// required to cover each other.
    /// </para>
    /// <para>
    /// <b>And the order carries what a separate sequence used to.</b> A world that can say
    /// what came first says it by ordering the parts and the codes inside them; a
    /// code-to-position dictionary said the same thing in a second place, was set by no
    /// world on the branch, and is gone. Deriving a precedence from this order is rung three
    /// arriving on every constructed world at once, which is an arm rather than a rename.
    /// </para>
    /// </remarks>
    public IReadOnlyList<Grouped>? Groups { get; init; }

    /// <summary>
    /// The question this moment asks, or nothing where the world asks none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Apart from the parts rather than the first of them</b>, and the alternative was
    /// numbering it group nought. Every reader that treats the parts as a story would then
    /// have to know to skip one, and three of them in one front end already do different
    /// things with the split -- a bag unions both, a chain starts from the question and walks
    /// the story, and a background intersects the story and must not see the question at all.
    /// An index convention that three readers must each honour is a second name for a
    /// distinction the type can simply keep.
    /// </para>
    /// <para>
    /// <b>And it is a part like any other</b>, so a world that can order the question's words
    /// says so the same way it says it for a statement.
    /// </para>
    /// </remarks>
    public Grouped? Asked { get; init; }

    /// <inheritdoc cref="IQuantizer{TObservation}.Fleeting"/>
    public IReadOnlySet<Code>? Passing { get; init; }

    /// <inheritdoc cref="IQuantizer{TObservation}.Forced"/>
    public IReadOnlySet<Code>? Assigned { get; init; }

    /// <summary>Codes and nothing else, which is most of them.</summary>
    public static Coded Of(IReadOnlyCollection<Code> codes) => new() { Codes = codes };

    /// <summary>One code, for a world whose moment is a single symbol.</summary>
    public static Coded Of(Code code) => new() { Codes = [code] };

    /// <summary>A moment made of parts, whose codes are what the parts hold.</summary>
    /// <param name="parts">The statements, newest first.</param>
    /// <param name="asked">The question, where the world asks one.</param>
    /// <param name="assigned">Which codes the world was told to emit rather than drew.</param>
    /// <remarks>
    /// <b>The flattening is done once here</b> rather than at each world, and what it yields
    /// is the moment as a bag — every word of every part, which is what an arm that selects
    /// nothing reads. A world that partitions its moment has no second answer to give, so
    /// asking it for one would be the same list written twice and two places to get it wrong.
    /// </remarks>
    public static Coded From(
        IReadOnlyList<Grouped> parts,
        Grouped? asked = null,
        IReadOnlySet<Code>? assigned = null)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var codes = new List<Code>();

        if (asked is { } question) codes.AddRange(question.Codes);

        foreach (var part in parts) codes.AddRange(part.Codes);

        return new Coded
        {
            Codes = codes,
            Groups = parts,
            Asked = asked,
            Assigned = assigned,
        };
    }
}
