using OpenPlexus.Bus;
using OpenPlexus.Codes;

namespace OpenPlexus.Learning;

/// <summary>
/// The join, when the whole live set is on one machine.
/// </summary>
/// <remarks>
/// <para>
/// For each onset, pair it with everything live and tell each participant its
/// partners. Free, because nobody has to be found.
/// </para>
/// <para>
/// <b>IT DOES NOT TEST THE HARD PART, and that is why it is called Local.</b>
/// Two machines seeing different halves of the same moment is the case that
/// needs a real rendezvous — open fork 1. The shape on <c>master</c> is a
/// bucket owner computed by hash, noticing an overlap and then being discarded,
/// measured at exactly 1.0 messages per observation.
/// </para>
/// <para>
/// <b>Onsets change what that has to do.</b> Joining overlapping intervals is a
/// different job from joining matched instants, and it is a strictly easier
/// one: if a thing was visible for two seconds, 50ms of clock skew between
/// machines is irrelevant. Overlap is robust against C2 where coincidence is
/// brittle. The existing bucket join was built for the brittle version.
/// </para>
/// </remarks>
public sealed class LocalRendezvous : IRendezvous
{
    private readonly IBus _bus;
    private readonly Ring _ring;

    public LocalRendezvous(IBus bus, Ring ring) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task JoinAsync(Code onset, IReadOnlyCollection<Code> live, long now,
        CancellationToken ct = default) => throw new NotImplementedException();
}
