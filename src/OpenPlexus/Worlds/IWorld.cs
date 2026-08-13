namespace OpenPlexus.Worlds;

/// <summary>One step of a world: what it showed, and what followed.</summary>
/// <typeparam name="TSeen">Whatever this world natively produces.</typeparam>
/// <remarks>
/// <b>Not codes, and that is the whole point.</b> A world says what happened in its
/// own terms — numbers, bits, whatever it has. Turning that into codes is the
/// TRANSLATION's job, and which translation is used is not a fact about the world.
/// </remarks>
public readonly record struct Turn<TSeen>
{
    /// <summary>What the world showed, in its own terms.</summary>
    public required TSeen Seen { get; init; }

    /// <summary>
    /// Which outcome followed, as a small whole number — <b>or nothing, where the
    /// settlement could not say.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A NUMBER RATHER THAN A <c>Code</c>, so a world never mints one.</b> The
    /// outcome coding is shared across every world, because a brain that learnt a
    /// different alphabet per world would not be one brain.
    /// </para>
    /// <para>
    /// <b>And nullable, because a world that does not know had no way to say so.</b> Most
    /// moments in any real stream are followed by nothing anybody observes, and this type
    /// could only express a stream where every one of them is. What was missing was a
    /// world's ability to say <i>nothing followed that I saw</i>.
    /// </para>
    /// <para>
    /// <b>It says what is being looked at and never what to conclude</b>, which is the line
    /// every other thing a world is allowed to report stays on. What anything downstream
    /// does about an unobserved outcome is not the world's business, and nothing here names
    /// it.
    /// </para>
    /// <para>
    /// <b>Every world that always knows its outcome is unchanged</b>, because an
    /// <see langword="int"/> still assigns.
    /// </para>
    /// </remarks>
    public required int? Outcome { get; init; }
}

/// <summary>
/// A problem, and nothing about how it is perceived or learnt.
/// </summary>
/// <typeparam name="TSeen">Whatever this world natively produces.</typeparam>
/// <remarks>
/// <para>
/// <b>John's rule, and it is about what went wrong last time.</b> On `csharp` the
/// worlds grew dials that reached into the brain — `Ranking` was one thing on bAbI
/// and another on CLEVR, so a WORLD decided how the brain thought, and every score
/// was a comparison between two brains as much as between two problems.
/// </para>
/// <para>
/// <b>So a world may turn its own dials and never the brain's.</b> What a world
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

/// <summary>
/// A world built from a finite bag, holding some of it back.
/// </summary>
/// <typeparam name="TSeen">Whatever this world natively produces.</typeparam>
/// <remarks>
/// <para>
/// <b>C4 constrains the learner and not the experimenter, and conflating the two is
/// why this was missing.</b> <i>No episode boundary, so nothing may depend on
/// train-then-test</i> forbids the MACHINE knowing about a boundary — it may not wait
/// for one, switch behaviour at one, or be scored on a lifetime average that assumes
/// one. Nothing in it forbids the person running the experiment from keeping some
/// observations back and asking, from outside, what the population would have said.
/// The learner is never told and cannot tell.
/// </para>
/// <para>
/// <b>Without it a world that draws with replacement scores its own recurrence.</b>
/// <see cref="Cifar"/> draws ten thousand images forever, so at forty thousand rounds
/// each has been seen four times and memorising a moment's winner set is entirely
/// affordable. That already happened here — an unbounded population's score tracked how
/// often an image RECURRED — and it was mitigated by bounding the population rather
/// than measured. This measures it.
/// </para>
/// <para>
/// <b>And it is the only anti-memorisation instrument a perceptual world can have.</b>
/// The sharp one is soundness by enumeration, which needs a rule set to enumerate;
/// <see cref="Multiplexer"/> has one and no world made of photographs ever will. On
/// <see cref="Cifar"/> the choice is this or nothing but a score that cannot be
/// distinguished from a lookup table.
/// </para>
/// </remarks>
public interface IWithholds<TSeen>
{
    /// <summary>
    /// Observations this world will never draw.
    /// </summary>
    /// <remarks>
    /// <b>Fixed at construction and never touched by <see cref="IWorld{TSeen}.Next"/>,
    /// which is what makes the number mean anything.</b> A held-out set the world could
    /// wander into would measure the same thing the trailing accuracy does, more slowly.
    /// </remarks>
    IReadOnlyList<Turn<TSeen>> Withheld { get; }
}
