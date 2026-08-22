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
/// 2026-08-05. <see cref="Worlds.Motif"/>'s task is compression of token sets
/// and <see cref="Worlds.Senses"/>'s is the sight–sound pairing; rendering either
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

    /// <inheritdoc cref="IQuantizer{TObservation}.Fleeting"/>
    public IReadOnlySet<Code>? Passing { get; init; }

    /// <inheritdoc cref="IQuantizer{TObservation}.Forced"/>
    public IReadOnlySet<Code>? Assigned { get; init; }

    /// <summary>Codes and nothing else, which is most of them.</summary>
    public static Coded Of(IReadOnlyCollection<Code> codes) => new() { Codes = codes };

    /// <summary>One code, for a world whose moment is a single symbol.</summary>
    public static Coded Of(Code code) => new() { Codes = [code] };
}
