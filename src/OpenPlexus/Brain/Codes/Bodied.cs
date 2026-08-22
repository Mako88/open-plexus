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
/// is correct rather than lazy. A body quantises its own variables, so a drive is already a
/// band by the time the moment arrives — there is no signal left to quantise, and the only
/// decision at this seam is whether the action stays in the moment.
/// </para>
/// <para>
/// <b>And what it decides about is a MARK rather than a world</b>, which is what lets it read
/// any body at all. A world says which of its codes it was told to emit rather than drew, on
/// <see cref="Coded.Assigned"/>'s licence; this drops them or keeps them. Nothing here says
/// attending is good or which variable matters — that connection is exactly what a learner is
/// being asked to find.
/// </para>
/// </remarks>
/// <param name="feeling">Whether the moment says what was done in it.</param>
public sealed class Bodied(Feeling feeling) : IQuantizer<Coded>
{
    /// <summary>
    /// <b>Zero, and it is never read.</b>
    /// </summary>
    /// <remarks>
    /// The body minted these codes and carries the modality it minted them with, so there is
    /// no answer here for a front end that codes nothing to give. Naming the body's would put
    /// one world's constant inside the brain for a number nothing asks for.
    /// </remarks>
    public byte Modality => 0;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>The blind arm drops what the world MARKED as done</b>, rather than being handed a
    /// moment with the doing left out. A world that emitted two moments for two arms would be
    /// two worlds, and the difference between them would be the world's rather than the
    /// translation's.
    /// </remarks>
    public IReadOnlyCollection<Code> Codify(Coded observation)
    {
        if (feeling == Feeling.Acted || observation.Assigned is not { Count: > 0 } done)
            return observation.Codes;

        return [.. observation.Codes.Where(one => !done.Contains(one))];
    }
}
