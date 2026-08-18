namespace OpenPlexus.Codes;

/// <summary>Where a moment came from, and which one it is from there.</summary>
/// <remarks>
/// <para>
/// <b>A sequence per source</b>, because two senses run on two clocks. A camera at two
/// frames a second and a thermometer at one reading a minute are separate streams, and
/// what follows a frame is the next frame rather than whatever happened to arrive next.
/// A single counter over everything would make one sense's rate a fact about another's.
/// </para>
/// <para>
/// <b>And it is what tells a repeat from a successor</b>, which is all C2 leaves a
/// receiver able to do. Late, duplicated and out of order are all permitted, and none of
/// them may be read as an answer to the moment before it.
/// </para>
/// </remarks>
public readonly record struct Stamp
{
    /// <summary>Which source pushed it.</summary>
    /// <remarks>
    /// <b>A modality is not this</b>, and the two are close enough to be worth separating.
    /// A <see cref="Code"/>'s modality says which alphabet a fragment is written in; this
    /// says which stream it arrived on. Two cameras emit the same modality and are two
    /// sources, and one camera's frame carries codes from several modalities at once.
    /// </remarks>
    public required byte Source { get; init; }

    /// <summary>Which moment it is from that source, counting up.</summary>
    public required long Sequence { get; init; }
}

/// <summary>One moment, as a source pushed it.</summary>
/// <remarks>
/// <para>
/// <b>Codes rather than the world's own terms</b>, because a world is a composition of
/// senses and they have to compose in one alphabet. The translation is a third thing and
/// it belongs at the join, so what reaches the brain has already been read by a front end
/// — which is why nothing here is generic over what a world natively produces.
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

    /// <summary>What the source says followed it, or nothing where it cannot say.</summary>
    /// <remarks>
    /// <b>Nothing is the third verdict rather than a miss</b>. Most moments in any real
    /// stream are followed by nothing anybody observes, and a settlement that could not say
    /// must cost a commitment exactly nothing — a monotone counter has no way to take a
    /// slur back.
    /// </remarks>
    public required Code? Followed { get; init; }
}
