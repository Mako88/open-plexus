using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Learning;

/// <summary>
/// One moment's worth of change: what started, and what was already there.
/// </summary>
/// <remarks>
/// <para>
/// The counterpart of <see cref="Thinking.Message"/>, which is the thinking
/// path. <b>Two kinds of traffic, and the Python conflates them</b> because one
/// process and one dictionary make them the same code path — which is how a
/// design ends up with no account of where its graph came from.
/// </para>
/// <para>
/// <b>A frame's onsets are ONE occasion, not one each.</b> They came from a
/// single observation, and an occasion is a set: everything in one moment met
/// everything else and nothing came first. Splitting them would count a pair of
/// simultaneous onsets twice.
/// </para>
/// </remarks>
public sealed record Occasion
{
    /// <summary>What just started. <b>Empty means nothing happened</b> — a
    /// stable scene is silent, and silence is the point of onsets.</summary>
    public required ImmutableArray<Code> Onsets { get; init; }

    /// <summary>What was already there when they started.</summary>
    public required ImmutableArray<Code> Live { get; init; }

    /// <summary>When, by the observing machine's own clock.</summary>
    public required long At { get; init; }
}

/// <summary>
/// How a node learns who it fired with.
/// </summary>
/// <remarks>
/// <b>The rendezvous is where connections are formed</b>, and it is the half of
/// the design that has no implementation on <c>master</c> either.
/// </remarks>
public interface IRendezvous
{
    /// <summary>
    /// Make sure everything in this occasion ends up in the others' rows.
    /// </summary>
    /// <remarks>
    /// <b>Not onset-with-onset only.</b> A sound starting while a ball is
    /// already visible must connect, and that is the cross-modal binding the
    /// design exists for. Counted once per occasion, never per tick.
    /// </remarks>
    ValueTask JoinAsync(Occasion occasion, CancellationToken ct = default);
}
