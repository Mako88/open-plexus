namespace OpenPlexus.Codes;

/// <summary>
/// That a code was DONE rather than seen, as a code — <b>rung three's trick</b>, played
/// on the one channel that had no reader.
/// </summary>
/// <remarks>
/// <para>
/// <b>The distinction is <c>P(y | x)</c> against <c>P(y | do(x))</c></b>, and no amount of
/// counting the first yields the second. <see cref="IQuantizer{TObservation}.Forced"/> has
/// reported which codes were assigned rather than selected since the day it was written, and
/// nothing has ever read it — so a scope naming a code the learner chose and a scope naming
/// the same code the world drew were the same scope, with their evidence added together.
/// </para>
/// <para>
/// <b>The obvious build is the expensive one</b>, exactly as it was for precedence. A scope
/// entry saying <i>this one was done</i> wants a scope that is no longer an array of codes, a
/// <see cref="Commitments.Commitment.Fires"/> that is no longer a subset test, a repair that
/// proposes provenance and a wire format that carries it.
/// </para>
/// <para>
/// <b>So it is derived into a code instead and nothing downstream changes.</b> A moment
/// carrying <c>go</c> because the learner chose it carries this beside it, so a scope naming
/// the plain code fires either way and a scope naming this one fires only under
/// intervention. Repair reaches for it exactly where the two come apart, which is the whole
/// of what a causal claim is here.
/// </para>
/// <para>
/// <b>Beside the code and never instead of it</b>, which is the line this repo has already
/// paid to learn. A front end that FUSED a word's position into its code cost the identity —
/// an input is an attribute of a thing and never the thing — and provenance fused into an
/// action code would do the same to the action. So the moment grows by one code per forced
/// one and loses nothing.
/// </para>
/// <para>
/// <b>And it is the learner that derives it</b>, which is the seam
/// <see cref="Sequenced"/> sits the right side of. A world says which code it was handed;
/// turning that into <i>this was an intervention</i> is a derivation, and a world doing it
/// would be deciding what the learner may conclude from having acted.
/// </para>
/// <para>
/// <b>There is no dial</b>, and it is inert wherever a front end reports nothing forced —
/// which is every watched world, and is a fact about the sense rather than a setting on the
/// brain.
/// </para>
/// </remarks>
public static class Intervened
{
    /// <summary>The modality an intervention rides on.</summary>
    /// <remarks>
    /// <b>Its own, beside <see cref="Sequenced.Ordered"/></b>, and for the same reason: this
    /// is DERIVED from a code rather than emitted by a sense, so a world able to produce one
    /// would be writing the learner's rules.
    /// </remarks>
    public const byte Did = 209;

    /// <summary>What the doing of one code is called, on every machine.</summary>
    /// <param name="done">The code that was assigned rather than selected.</param>
    /// <remarks>
    /// <b>Derived from the code</b>, so two machines reach the same name with nothing to ask.
    /// <see cref="Agreed"/> rather than <see cref="object.GetHashCode"/>, which is randomised
    /// per process — a codebook resting on that would mean two machines quietly disagreeing
    /// about what an intervention IS.
    /// </remarks>
    public static Code Of(Code done)
    {
        var hash = Agreed.Fold(Agreed.Basis, done.Modality);

        hash = Agreed.Fold(hash, done.Value);

        return new Code(Did, Agreed.Mix(hash));
    }

    /// <summary>Whether a code says something was done rather than reporting a sense.</summary>
    /// <param name="code">The code to ask about.</param>
    /// <remarks>
    /// <b>What genesis reads, and it is the one gate this needs.</b> An intervention is a
    /// SPECIALISATION and never a root: <i>I did something</i> with no idea what followed is
    /// a rule about agency rather than about the world, and a population rooting on these
    /// fills with them the moment a chooser is wired. <see cref="Sequenced.Names"/> carries
    /// the identical argument for a precedence.
    /// </remarks>
    public static bool Names(Code code) => code.Modality == Did;

    /// <summary>One code for each thing the learner was handed rather than shown.</summary>
    /// <param name="forced">What the front end said was assigned.</param>
    /// <remarks>
    /// <b>Sorted, because a set's walk does not survive a run.</b> A moment whose contents
    /// depended on how a hash set happened to enumerate would be a difference nobody chose,
    /// which is what <c>DeterminismTests</c> exists to refuse.
    /// </remarks>
    public static IEnumerable<Code> From(IReadOnlySet<Code> forced)
    {
        ArgumentNullException.ThrowIfNull(forced);

        return forced.Order().Select(Of);
    }
}
