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
public sealed class Passthrough<TFrame> : IQuantizer<TFrame>
{
    private readonly Func<TFrame, Coded> _reading;

    /// <param name="reading">
    /// The already-coded part of a frame — <b>a projection rather than a field</b>, on
    /// <see cref="Banded{TFrame}"/>'s pattern. A world whose whole moment is codes hands
    /// itself over; a body reading one frame several ways gives each sense the part it knows,
    /// and what a frame IS belongs to whoever composed the body rather than to the brain.
    /// </param>
    public Passthrough(Func<TFrame, Coded> reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        _reading = reading;
    }

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
    public IReadOnlyCollection<Code> Codify(TFrame frame) => _reading(frame).Codes;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>The parts as the world drew them</b>, which is a passthrough now that the channel
    /// takes the shape the world already had. It used to flatten them into a code-to-thing
    /// dictionary and drop every code that landed in two parts, so a scene of two red balls
    /// reported no things at all while a scene of one reported one. That is the front end
    /// saying LESS the more of a kind it is shown, and it destroyed multiplicity at the one
    /// seam that had it.
    /// </remarks>
    public IReadOnlyList<Grouped>? Bind(TFrame frame) =>
        _reading(frame).Things is { Count: > 0 } parts ? parts : null;

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Fleeting(TFrame frame) => _reading(frame).Passing;

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Forced(TFrame frame) => _reading(frame).Assigned;
}
