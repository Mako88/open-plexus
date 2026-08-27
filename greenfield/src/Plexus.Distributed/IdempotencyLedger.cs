namespace Plexus.Distributed;

/// <summary>
/// What happened to a message the last time it arrived.
/// </summary>
/// <remarks>
/// <para>
/// Deviation from the skeleton document. Section 15 proposes a boolean
/// <c>TryBeginAsync</c> plus a <c>CompleteAsync</c>, and section 15's own round rules then
/// require that retrying after an ambiguous result returns the recorded outcome. A boolean
/// cannot do that: a holder that crashed between the two calls has a message that is neither
/// retryable nor completed, and the caller is told the same thing either way.
/// </para>
/// <para>
/// So the ledger records the outcome, and the three answers to a claim are distinguishable:
/// the caller owns the work, somebody else owns it and has not finished, or here is what it
/// came to.
/// </para>
/// </remarks>
public interface IIdempotencyLedger<TOutcome>
{
    ValueTask<LedgerClaim<TOutcome>> ClaimAsync(MessageId message, CancellationToken ct);

    ValueTask CompleteAsync(MessageId message, TOutcome outcome, CancellationToken ct);

    /// <summary>
    /// Gives up a claim without recording an outcome.
    /// </summary>
    /// <remarks>
    /// Cancellation releases rather than completes. A cancelled message that stayed claimed
    /// is a message nothing can retry and nothing has done.
    /// </remarks>
    ValueTask ReleaseAsync(MessageId message, CancellationToken ct);
}

/// <summary>The answer to a claim on one message.</summary>
public abstract record LedgerClaim<TOutcome>
{
    private LedgerClaim() { }

    /// <summary>The caller owns the work and nobody has done it.</summary>
    public sealed record Granted : LedgerClaim<TOutcome>;

    /// <summary>Somebody else owns the work and has not recorded an outcome.</summary>
    public sealed record InFlight : LedgerClaim<TOutcome>;

    /// <summary>The work was done, and this is what it came to.</summary>
    public sealed record Settled(TOutcome Outcome) : LedgerClaim<TOutcome>;
}
