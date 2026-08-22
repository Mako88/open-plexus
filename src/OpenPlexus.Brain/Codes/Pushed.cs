namespace OpenPlexus.Codes;

/// <summary>Where a moment came from, and which one it is from there.</summary>
/// <remarks>
/// <para>
/// <b>A sequence per source</b>, and a source is a whole world rather than one of its
/// modalities. A brain judged on a generated world and on a text conversation at once has
/// two streams to settle, and what follows a moment of one is the next moment of that one.
/// A single counter over both would make one world's rate a fact about the other's.
/// </para>
/// <para>
/// <b>And it is what tells a repeat from a successor</b>, which is all C2 leaves a
/// receiver able to do. Late, duplicated and out of order are all permitted, and none of
/// them may be read as an answer to the moment before it.
/// </para>
/// </remarks>
public readonly record struct Stamp
{
    /// <summary>The source a lone input pushes on.</summary>
    /// <remarks>
    /// <b>One rather than nought</b>, so an unset source is not a valid one. A default
    /// <see langword="byte"/> is nought, and an input that forgot to say which stream it was
    /// would then silently share one with whichever other input did the same.
    /// </remarks>
    public const byte First = 1;

    /// <summary>Which source pushed it.</summary>
    /// <remarks>
    /// <b>A modality is not this</b>, and conflating them is the error this comment now
    /// exists to prevent. A <see cref="Code"/>'s modality says which alphabet a fragment is
    /// written in, and one moment carries several — a camera and a microphone on one world
    /// go into ONE moment, which is what makes a scope able to span them. This says which
    /// world the moment came from, and there is normally one.
    /// </remarks>
    public required byte Source { get; init; }

    /// <summary>Which moment it is from that source, counting up.</summary>
    public required long Sequence { get; init; }
}

/// <summary>One moment, as a source pushed it.</summary>
/// <remarks>
/// <para>
/// <b>Codes rather than the world's own terms</b>, because a world's modalities have to
/// compose in one alphabet. The translation is a third thing and it belongs at the join, so
/// what reaches the brain has already been read by a front end — <see cref="Compound{T}"/>
/// is where several of them merge into one moment.
/// </para>
/// <para>
/// <b>And the brain is not asked</b>. A push arrives when the source has something, which
/// is the arrangement a camera and a thermometer are already in and the one every world
/// here was denied by a <c>Next</c> that had to be called.
/// </para>
/// </remarks>
public readonly record struct Pushed
{
    /// <summary>Where it came from, and which one it is.</summary>
    public required Stamp From { get; init; }

    /// <summary>What is live, as the front end read it.</summary>
    /// <remarks>
    /// <b>Raw, before any minted name is folded in</b>. A name is reached by inference from
    /// the codes it stands for, so a moment folded before it was pushed would carry one
    /// machine's vocabulary onto machines that may not share it.
    /// </remarks>
    public required IReadOnlySet<Code> Codes { get; init; }

    /// <summary>
    /// Which of those codes the source says will not come back.
    /// </summary>
    /// <remarks>
    /// <b>A fact only the world holds</b>, which is why it travels rather than being worked
    /// out. A code seen once and a code about to be seen again look identical to a receiver
    /// until the second sighting, so nothing downstream can derive this — and what it buys
    /// is a table row not written, never a decision changed. An index minted for one scene
    /// is the case: it is needed while the scene is live and can never be a candidate for
    /// anything afterwards.
    /// </remarks>
    public IReadOnlySet<Code>? Fleeting { get; init; }

    /// <summary>
    /// Which THING each of those codes belongs to, where the source can say.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A fact only the front end holds</b>, on <see cref="Fleeting"/>'s reason and with a
    /// larger consequence. A retina hands the cortex an already-grouped signal and nothing
    /// downstream can recover the grouping from the codes: a red ball beside a blue box and a
    /// blue ball beside a red box are the identical set, so the binding is destroyed before
    /// anything the population does gets a chance to run.
    /// </para>
    /// <para>
    /// <b>And it cannot be derived into codes</b>, which is what makes it travel rather than
    /// be computed at the join the way a precedence is. Deriving a code per pair inside a
    /// group was built and refuted — it composed nothing on CLEVR while flooding repair's
    /// candidate set — and it is quadratic in the group's size where this is one int a code.
    /// </para>
    /// <para>
    /// <b>Codes in no group are unconstrained</b> rather than being one group of their own. A
    /// world may segment its objects and leave a question's codes outside every part, so this
    /// is what the source can say about the moment's shape rather than a partition of it.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<Code, int>? Grouping { get; init; }

    /// <summary>What the source says followed it, or nothing where it cannot say.</summary>
    /// <remarks>
    /// <b>Nothing is the third verdict rather than a miss</b>. Most moments in any real
    /// stream are followed by nothing anybody observes, and a settlement that could not say
    /// must cost a commitment exactly nothing — a monotone counter has no way to take a
    /// slur back.
    /// </remarks>
    public required Code? Followed { get; init; }
}
