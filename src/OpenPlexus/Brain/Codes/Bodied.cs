using OpenPlexus.Worlds;

namespace OpenPlexus.Codes;

/// <summary>Whether a moment says what was DONE in it.</summary>
/// <remarks>
/// <b>A fact about the pipe, which is neither side's to decide</b> — the same place
/// <see cref="Joining"/> sits. The body says which variable was attended to, because that is
/// what happened; whether the learner is told is a translation's choice, and it is the control
/// that says whether an action is doing any work at all.
/// </remarks>
public enum Feeling
{
    /// <summary>
    /// The body's own bands and nothing about what was done — <b>the control, and it has to be
    /// named to be one.</b>
    /// </summary>
    /// <remarks>
    /// <b>A world model and a base rate read alike without this.</b> Which variable
    /// is worst next is largely decided by which drains fastest, so a population told only
    /// the state can score well by learning the drain order and never learning that acting
    /// changes anything. What separates the two is whether adding the action to the moment
    /// buys score, and there is nothing to subtract it from unless the arm exists.
    /// </remarks>
    Blind,

    /// <summary>The bands, and the code for what was done about them.</summary>
    /// <remarks>
    /// <b>One code in the moment and no new machinery</b>, which is why an action goes here
    /// rather than beside the scope. A commitment's scope is a subset test over a set, so
    /// <i>variable two was low and I attended to nought</i> is expressible the moment the
    /// action is a code — and the expectation is then the consequence, which is <i>what would
    /// the world look like if I did X</i> written in the machinery that already exists.
    /// </remarks>
    Acted,
}

/// <summary>
/// The translation between a body's felt state, what was done about it, and codes.
/// </summary>
/// <remarks>
/// <para>
/// <b>It codes nothing and selects nothing</b>, which makes it the thinnest front end here and
/// is correct rather than lazy. A drive is already a band by the time
/// <see cref="Homeostat.Feels"/> has run, because the body quantises its own variables — so
/// there is no signal left to quantise and the only decision at this seam is whether the
/// action joins the moment.
/// </para>
/// <para>
/// <b>And it says what is being looked at</b>, rather than what to conclude. <i>This was
/// attended to</i> is a fact about what happened, in the same licence a front end has for
/// <i>these codes were one object</i>. Nothing here says attending is good, which variable
/// matters, or that <c>Act:2</c> has anything to do with <c>Need+2</c> — that connection is
/// exactly what a learner is being asked to find.
/// </para>
/// </remarks>
/// <param name="feeling">Whether the moment says what was done in it.</param>
public sealed class Bodied(Feeling feeling) : IQuantizer<Bodily>
{
    /// <summary>The modality a felt body rides on.</summary>
    /// <remarks>
    /// <b><see cref="Homeostat"/>'s own, because the body minted these codes.</b> A front end
    /// that re-badged them would make one state two codes depending on which translation ran,
    /// which is the fault the whole modality scheme exists to prevent.
    /// </remarks>
    public byte Modality => Homeostat.Act;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(Bodily observation)
    {
        if (feeling == Feeling.Blind || observation.Did is not { } which)
            return observation.Felt;

        var said = new List<Code>(observation.Felt.Length + 1);

        said.AddRange(observation.Felt);
        said.Add(Homeostat.Attending(which));

        return said;
    }
}
