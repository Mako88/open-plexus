namespace Plexus.Distributed;

/// <summary>
/// What the wire is allowed to do to a message.
/// </summary>
/// <remarks>
/// Seeded and reproducible. A fault schedule that varies between runs turns a real ordering
/// defect into a flaky test, and a flaky test gets disabled.
/// </remarks>
public sealed record TransportFaults
{
    public required int Seed { get; init; }

    public required double LossRate { get; init; }

    public required double DuplicationRate { get; init; }

    public required int MaximumDelayMilliseconds { get; init; }
}

/// <summary>Handing one envelope to one holder, badly, on purpose.</summary>
public interface ITransport
{
    ValueTask<TResult> SendAsync<TPayload, TResult>(
        NodeAddress destination,
        Envelope<TPayload> envelope,
        Func<Envelope<TPayload>, CancellationToken, ValueTask<TResult>> deliver,
        CancellationToken ct);
}

public readonly record struct NodeAddress(string Value);

/// <summary>
/// Delay, duplication, loss and cancellation under a fixed seed.
/// </summary>
/// <remarks>
/// The faults are the measurement rather than an obstacle to it. Evidence has to converge to
/// the same durable value under every delivery order this produces, and the reference is the
/// deterministic run with the faults switched off.
/// </remarks>
public sealed class UnreliableTransport(TransportFaults faults) : ITransport
{
    private readonly TransportFaults _faults = faults;

    public ValueTask<TResult> SendAsync<TPayload, TResult>(
        NodeAddress destination,
        Envelope<TPayload> envelope,
        Func<Envelope<TPayload>, CancellationToken, ValueTask<TResult>> deliver,
        CancellationToken ct) =>
        throw new NotImplementedException();
}
