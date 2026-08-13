namespace OpenPlexus.Codes;

/// <summary>
/// The front end for a world that is already coded — <b>it quantises nothing, and
/// it is here so that not quantising is a CHOICE the pattern can express.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S CALL, 2026-08-05: THE TEST WORLDS ARE NOT GOING AWAY.</b> A world
/// built to isolate one mechanism should be allowed to feed the graph directly —
/// what it is testing is not the front end, and making it invent a signal first
/// would put a second thing between the mechanism and the measurement. So the
/// no-op is a first-class member of the family rather than a gap in it.
/// </para>
/// <para>
/// <b>AND IT IS BRAIN-SIDE LIKE EVERY OTHER QUANTISER, WHICH IS THE WHOLE POINT
/// OF WRITING IT DOWN ONCE.</b> While each world owned a private nested copy,
/// "this world does not quantise" was indistinguishable from "nobody has got to
/// this world yet" — and <see cref="Winnow"/> could be built, documented and
/// measured without ever reaching a world, because there was no shared place a
/// front end was supposed to be. <b>One name for the no-op makes the worlds that
/// SHOULD have a real one visible by subtraction.</b>
/// </para>
/// <para>
/// <b>THE RED-BALL PROPERTY HOLDS TRIVIALLY.</b> Nothing is fitted because nothing
/// is computed: the same codes go in and come out on every machine forever, which
/// is the strongest form of the guarantee <see cref="IQuantizer{TObservation}"/>
/// asks for and the least interesting.
/// </para>
/// </remarks>
public sealed class Passthrough : IQuantizer<Coded>
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
    public IReadOnlyDictionary<Code, int>? Bind(Coded observation) => observation.Groups;

    /// <inheritdoc/>
    public IReadOnlyDictionary<Code, int>? Order(Coded observation) => observation.Sequence;

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Fleeting(Coded observation) => observation.Passing;

    /// <inheritdoc/>

    /// <inheritdoc/>

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Forced(Coded observation) => observation.Assigned;
}
