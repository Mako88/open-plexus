namespace OpenPlexus.Codes;

/// <summary>
/// A world's output when the world's output is already codes — <b>the shape a
/// constructed world hands over, and the reason it needs no quantiser of its
/// own.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>NINE WORLDS EACH WROTE THEIR OWN FRONT END AND EIGHT OF THEM DID NOTHING.</b>
/// <c>Seeing</c>, <c>Feeling</c> twice, <c>Hearing</c>, <c>Looking</c>,
/// <c>Reading</c> and three <c>Passthrough</c>s — private nested classes returning
/// the observation they were handed. That is not nine front ends; it is one
/// missing type copied nine times, and while it sat inside the worlds there was
/// nowhere for a real quantiser to live either.
/// </para>
/// <para>
/// <b>A WORLD WITH NO SIGNAL IS A LEGITIMATE WORLD AND ALSO A LIMIT — John,
/// 2026-08-05.</b> <see cref="Worlds.Motif"/>'s task is compression of token sets
/// and <see cref="Worlds.Senses"/>'s is the sight–sound pairing; rendering either
/// as pixels would measure a different thing. So feeding codes straight in stays
/// available on purpose. <b>What it costs is that such a world can never tell
/// anybody whether the quantisers work</b>, which is worth knowing before the
/// payoff of a quantiser is estimated from a suite where five of nine worlds
/// cannot exercise one.
/// </para>
/// <para>
/// <b>THE OPTIONAL THREE ARE NOT PASSTHROUGH AND THAT IS WHY THEY ARE HERE.</b>
/// Grouping, order and fleetingness are things ONLY a front end can know — see
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

    /// <inheritdoc cref="IQuantizer{TObservation}.Bind"/>
    public IReadOnlyDictionary<Code, int>? Groups { get; init; }

    /// <inheritdoc cref="IQuantizer{TObservation}.Order"/>
    public IReadOnlyDictionary<Code, int>? Sequence { get; init; }

    /// <inheritdoc cref="IQuantizer{TObservation}.Fleeting"/>
    public IReadOnlySet<Code>? Passing { get; init; }

    /// <inheritdoc cref="IQuantizer{TObservation}.Forced"/>
    public IReadOnlySet<Code>? Assigned { get; init; }

    /// <summary>Codes and nothing else, which is most of them.</summary>
    public static Coded Of(IReadOnlyCollection<Code> codes) => new() { Codes = codes };

    /// <summary>One code, for a world whose moment is a single symbol.</summary>
    public static Coded Of(Code code) => new() { Codes = [code] };
}
