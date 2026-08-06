namespace OpenPlexus.Worlds;

/// <summary>One step of a world: what it showed, and what followed.</summary>
/// <typeparam name="TSeen">Whatever this world natively produces.</typeparam>
/// <remarks>
/// <b>NOT CODES, AND THAT IS THE WHOLE POINT.</b> A world says what happened in its
/// own terms — numbers, bits, whatever it has. Turning that into codes is the
/// TRANSLATION's job, and which translation is used is not a fact about the world.
/// </remarks>
public readonly record struct Turn<TSeen>
{
    /// <summary>What the world showed, in its own terms.</summary>
    public required TSeen Seen { get; init; }

    /// <summary>Which outcome followed, as a small whole number.</summary>
    /// <remarks>
    /// <b>A NUMBER RATHER THAN A <c>Code</c>, so a world never mints one.</b> The
    /// outcome coding is shared across every world, because a brain that learnt a
    /// different alphabet per world would not be one brain.
    /// </remarks>
    public required int Outcome { get; init; }
}

/// <summary>
/// A problem, and nothing about how it is perceived or learnt.
/// </summary>
/// <typeparam name="TSeen">Whatever this world natively produces.</typeparam>
/// <remarks>
/// <para>
/// <b>JOHN'S RULE, AND IT IS ABOUT WHAT WENT WRONG LAST TIME.</b> On `csharp` the
/// worlds grew dials that reached into the brain — `Ranking` was one thing on bAbI
/// and another on CLEVR, so a WORLD decided how the brain thought, and every score
/// was a comparison between two brains as much as between two problems.
/// </para>
/// <para>
/// <b>SO A WORLD MAY TURN ITS OWN DIALS AND NEVER THE BRAIN'S.</b> What a world
/// outputs is its business; what a brain does with it is not, and `SeparationTests`
/// fails the build if anything in here names a brain type.
/// </para>
/// </remarks>
public interface IWorld<TSeen>
{
    /// <summary>One step of the world.</summary>
    Turn<TSeen> Next();

    /// <summary>How many outcomes this world can produce.</summary>
    /// <remarks>
    /// <b>What a blind guess is against</b>, so no run has to be told the chance bar
    /// separately and then be told it wrong.
    /// </remarks>
    int Outcomes { get; }
}
