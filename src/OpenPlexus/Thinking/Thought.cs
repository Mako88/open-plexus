using OpenPlexus.Codes;

namespace OpenPlexus.Thinking;

/// <summary>
/// One broadcast, on the machine that started it.
/// </summary>
/// <remarks>
/// <b>Readable at any time.</b> Under continuous input there is no moment
/// between thoughts, so the system acts on the best chain arrived so far and
/// later arrivals refine it. Nothing waits for completion.
/// </remarks>
public sealed class Thought
{
    private readonly BroadcastId _id;

    /// <summary>Endpoint code to what reached it.</summary>
    private readonly Dictionary<Code, Arrival> _arrivals = [];

    /// <summary>
    /// The accounting. <c>origins + splits - deaths == live</c> holds exactly
    /// in one process and does not across a network, which is why it is
    /// asserted rather than trusted.
    /// </summary>
    private int _live, _splits, _deaths;

    private readonly int _origins;

    public Thought(BroadcastId id, int origins) => throw new NotImplementedException();

    public BroadcastId Id => throw new NotImplementedException();

    /// <summary>
    /// Accumulates one arrival.
    /// </summary>
    /// <remarks>
    /// Keeps the <b>strongest single</b> chain as the explanation. A summed
    /// score is no route's strength, and keeping the last arrival would make
    /// the explanation whichever branch happened to finish last.
    /// </remarks>
    public void Receive(Arrival arrival) => throw new NotImplementedException();

    /// <summary>Folds in one node's termination report.</summary>
    public void Receive(Accounting accounting) => throw new NotImplementedException();

    /// <summary>The top arrivals right now.</summary>
    public IReadOnlyList<Arrival> Best(int count) => throw new NotImplementedException();

    /// <summary>
    /// Whether the accounting adds up. Asserted, never assumed.
    /// </summary>
    public bool Balanced() => throw new NotImplementedException();

    /// <summary>
    /// Whether every route has returned or died by the thought's own
    /// accounting. <b>Not a deadline</b> — a deadline is a constant nobody
    /// measured, and the death event is what makes one unnecessary.
    /// </summary>
    public bool Settled => throw new NotImplementedException();

    /// <summary>
    /// Drop the state. Called on settle, or on a death event for a machine this
    /// thought had routes through.
    /// </summary>
    /// <remarks>
    /// <b>Termination is housekeeping now, not correctness.</b> A thought
    /// stranded by a vanished machine leaks state instead of hanging the
    /// system, because nothing was waiting on it to finish.
    /// </remarks>
    public void Release() => throw new NotImplementedException();
}
