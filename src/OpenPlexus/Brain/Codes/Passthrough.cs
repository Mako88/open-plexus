namespace OpenPlexus.Codes;

/// <summary>
/// The front end for a world that is already coded — <b>it quantises nothing,</b> and
/// it is here so that not quantising is a CHOICE the pattern can express.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's call, 2026-08-05: the test worlds are not going away.</b> A world
/// built to isolate one mechanism should be allowed to feed the graph directly —
/// what it is testing is not the front end, and making it invent a signal first
/// would put a second thing between the mechanism and the measurement. So the
/// no-op is a first-class member of the family rather than a gap in it.
/// </para>
/// <para>
/// <b>And it is brain-side like every other quantiser</b>, which is the whole point
/// of writing it down once. While each world owned a private nested copy,
/// "this world does not quantise" was indistinguishable from "nobody has got to
/// this world yet" — and <see cref="Winnow"/> could be built, documented and
/// measured without ever reaching a world, because there was no shared place a
/// front end was supposed to be. <b>One name for the no-op makes</b> the worlds that
/// SHOULD have a real one visible by subtraction.
/// </para>
/// <para>
/// <b>THE RED-BALL PROPERTY HOLDS TRIVIALLY.</b> Nothing is fitted because nothing
/// is computed: the same codes go in and come out on every machine forever, which
/// is the strongest form of the guarantee <see cref="IQuantizer{TObservation}"/>
/// asks for and the least interesting.
/// </para>
/// </remarks>
public sealed class Passthrough : IQuantizer<Coded>, IQuantizer<Worlds.Crossed>
{
    /// <summary>
    /// <b>Zero, and it is never read.</b>
    /// </summary>
    /// <remarks>
    /// The codes carry the modality their world minted them with, so there is no
    /// single answer here and nothing asks for one — anything that needs to know reads
    /// <see cref="Code.Modality"/> and never a front end's.
    /// </remarks>
    public byte Modality => 0;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(Coded observation) => observation.Codes;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Built from the parts</b>, and a code in two of them belongs to neither. The channel
    /// asks which THING a code belongs to and a dictionary can name one, so a code the world
    /// put in two parts has no answer to give — leaving it out says that, and picking the
    /// first would assert a binding the world never claimed. That is the whole reason a
    /// moment carries a list of parts rather than this dictionary.
    /// </remarks>
    public IReadOnlyDictionary<Code, int>? Bind(Coded observation)
    {
        if (observation.Groups is not { Count: > 0 } parts) return null;

        var one = new Dictionary<Code, int>();
        var twice = new HashSet<Code>();

        for (var part = 0; part < parts.Count; part++)
            foreach (var code in parts[part].Codes)
                if (!one.TryAdd(code, part) && one[code] != part) twice.Add(code);

        foreach (var code in twice) one.Remove(code);

        return one.Count == 0 ? null : one;
    }

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Fleeting(Coded observation) => observation.Passing;

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Forced(Coded observation) => observation.Assigned;

    /// <summary>The spoken half of a crossing moment, which is already codes.</summary>
    /// <remarks>
    /// <b>The same no-op through a second door.</b> A crossing moment carries one sense the
    /// world constructed and one it drew, so the constructed half wants exactly what this
    /// class already is and the drawn half wants <see cref="Tiling"/>.
    /// </remarks>
    public IReadOnlyCollection<Code> Codify(Worlds.Crossed observation) => observation.Said;
}
