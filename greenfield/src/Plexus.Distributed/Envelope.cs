using Plexus.Core.Knowledge;

namespace Plexus.Distributed;

/// <summary>
/// What every message carries whatever it is carrying.
/// </summary>
/// <remarks>
/// <para>
/// The configuration fingerprint travels with every message rather than being agreed once at
/// the handshake. A fleet whose holders were launched with different dials produces a run
/// that is scientifically void and looks entirely plausible, and the only moment that is
/// cheap to check is the moment a message arrives.
/// </para>
/// <para>
/// Deviation from the skeleton document, and a small one. The non-generic
/// <see cref="IEnvelope"/> exists so a transport can route and log a message without knowing
/// its payload type. Without it the unreliable transport has to be generic all the way down,
/// or has to reflect.
/// </para>
/// </remarks>
public sealed record Envelope<TPayload> : IEnvelope
{
    public required MessageId Message { get; init; }

    public required RoundId Round { get; init; }

    public required NodeId Sender { get; init; }

    public required ConfigurationFingerprint Configuration { get; init; }

    public required TPayload Payload { get; init; }
}

/// <summary>What a transport can see of a message without opening it.</summary>
public interface IEnvelope
{
    MessageId Message { get; }

    RoundId Round { get; }

    NodeId Sender { get; }

    ConfigurationFingerprint Configuration { get; }
}
