using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Learning;

/// <summary>
/// What fired together, and when. What crosses the wire on the learning path.
/// </summary>
/// <remarks>
/// The counterpart of <see cref="Thinking.Message"/>, which is the thinking
/// path. <b>Two kinds of traffic, and the Python conflates them</b> because one
/// process and one dictionary make them the same code path — which is how a
/// design ends up with no account of where its graph came from.
/// </remarks>
public sealed record Occasion
{
    public required ImmutableArray<Code> Codes { get; init; }
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
    /// A code just started while these were already live. Make sure every one
    /// of them ends up with the others in its row.
    /// </summary>
    /// <remarks>
    /// <b>Not onset-with-onset.</b> A sound starting while a ball is already
    /// visible must connect, and that is the cross-modal binding the design
    /// exists for. Counted once per onset, never per tick.
    /// </remarks>
    Task JoinAsync(Code onset, IReadOnlyCollection<Code> live, long now,
        CancellationToken ct = default);
}
