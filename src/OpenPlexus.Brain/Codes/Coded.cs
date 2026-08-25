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

    /// <summary>The things a moment names, one part each.</summary>
    /// <param name="sentences">Every sentence of the moment, the question among them.</param>
    /// <param name="nouns">Which codes name a thing, which only the world can say.</param>
    /// <remarks>
    /// <para>
    /// <b>A sentence is a moment whose things are MENTIONED rather than present</b>, which is
    /// what lets one rule cover a walk that is recited and a walk that is looked at. A word
    /// naming a room, a thing or a person is that thing appearing in the moment; every other
    /// word names none and belongs to no part, where it constrains nothing.
    /// </para>
    /// <para>
    /// <b>One part a thing rather than one a mention</b>, and the difference decides what two
    /// of a kind means. Two sentences about the apple are one apple said twice, so a part per
    /// mention would report a moment as holding as many apples as it talked about — which is
    /// the multiplicity a front end is supposed to report, backwards.
    /// </para>
    /// <para>
    /// <b>In the order they are first named</b>, because two machines reading one transcript
    /// must build the identical parts and a set walks in whatever order it was filled in.
    /// </para>
    /// <para>
    /// <b>Here rather than in each text world</b>, for the reason <see cref="Parts"/> is here.
    /// Which of its words name a thing is the world's own fact; turning a list of sentences
    /// into the parts a front end reports is nobody's in particular.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<Grouped> Things(
        IReadOnlyList<IReadOnlyList<Code>> sentences, IReadOnlySet<Code> nouns)
    {
        ArgumentNullException.ThrowIfNull(sentences);
        ArgumentNullException.ThrowIfNull(nouns);

        var found = new List<Code>();
        var already = new HashSet<Code>();

        foreach (var sentence in sentences)
            foreach (var word in sentence)
                if (nouns.Contains(word) && already.Add(word)) found.Add(word);

        return [.. found.Select(word => Of([word]))];
    }

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
    /// The THINGS in the moment, or nothing where the world cannot say.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One idea rather than two, which is what it was.</b> This channel was called the
    /// moment's parts, and a scene world filled it with its objects while a text world
    /// filled it with its statements — so <c>Commitments.Spanning</c> read <i>one thing</i>
    /// on one world and <i>one sentence</i> on another under a single name. That is this
    /// repo's own two-ideas-one-name trap sitting inside the mechanism for <i>a thing is one
    /// thing</i>. The statements have their own channel now and this one means a thing
    /// everywhere.
    /// </para>
    /// <para>
    /// <b>A list of parts rather than a code-to-thing dictionary</b>, because a moment may
    /// hold two of a KIND and a dictionary names one thing per code. A code in two parts is
    /// in two things, so the number of parts holding it is its multiplicity.
    /// </para>
    /// <para>
    /// <b>Partial where a world can say some of it</b>, so a scene may segment its objects
    /// and leave the question's codes in no part at all. <see cref="Codes"/> is what the
    /// moment IS and this is what the world can say about its shape, so the two are not
    /// required to cover each other.
    /// </para>
    /// </remarks>
    public IReadOnlyList<Grouped>? Things { get; init; }

    /// <summary>
    /// The statements of the moment, newest first, or nothing where the world makes none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A text world's own shape, and no other world has one.</b> A statement is not a
    /// thing: <i>the apple is in the kitchen</i> mentions two things and is neither of them.
    /// Every <c>Codes.Joining</c> arm reads this — a bag unions it, a chain starts from the
    /// question and walks it — and none of them is asking which thing anything is about.
    /// </para>
    /// <para>
    /// <b>And the order carries what a separate sequence used to.</b> A world that can say
    /// what came first says it by ordering the statements and the codes inside them; a
    /// code-to-position dictionary said the same thing in a second place, was set by no
    /// world on the branch, and is gone. Deriving a precedence from this order is rung three
    /// arriving on every constructed world at once, which is an arm rather than a rename.
    /// </para>
    /// </remarks>
    public IReadOnlyList<Grouped>? Statements { get; init; }

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

    /// <summary>A moment made of statements, whose codes are what the statements hold.</summary>
    /// <param name="parts">The statements, newest first.</param>
    /// <param name="asked">The question, where the world asks one.</param>
    /// <param name="assigned">Which codes the world was told to emit rather than drew.</param>
    /// <param name="things">The things mentioned, where the world can say which words name one.</param>
    /// <remarks>
    /// <b>The flattening is done once here</b> rather than at each world, and what it yields
    /// is the moment as a bag — every word of every statement, which is what an arm that
    /// selects nothing reads. A world that partitions its moment has no second answer to
    /// give, so asking it for one would be the same list written twice and two places to get
    /// it wrong.
    /// </remarks>
    public static Coded From(
        IReadOnlyList<Grouped> parts,
        Grouped? asked = null,
        IReadOnlySet<Code>? assigned = null,
        IReadOnlyList<Grouped>? things = null)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var codes = new List<Code>();

        if (asked is { } question) codes.AddRange(question.Codes);

        foreach (var part in parts) codes.AddRange(part.Codes);

        return new Coded
        {
            Codes = codes,
            Statements = parts,
            Things = things,
            Asked = asked,
            Assigned = assigned,
        };
    }
}
