namespace Plexus.Distributed.Tests;

/// <summary>
/// The round rules, each written as the failure it prevents.
/// </summary>
/// <remarks>
/// Three of these are defects the existing implementation carries today, which is why they
/// are here rather than assumed: shared round state between an ask and its settlement,
/// ingestion that marks a moment seen before the work it triggers has finished, and a fleet
/// that trusts rather than checks that every holder was launched with the same dials.
/// </remarks>
public sealed class DistributedRoundTests
{
    [Fact]
    public void Concurrent_rounds_do_not_exchange_moments() =>
        Pending.Claim(
            "everything a round needs carried in its envelope, and no fleet-wide "
            + "current-moment field for a second round to overwrite");

    [Fact]
    public void A_cancelled_request_can_be_retried_and_settled_once() =>
        Pending.Claim(
            "IIdempotencyLedger.ReleaseAsync on cancellation, so a cancelled message is "
            + "neither settled nor permanently claimed");

    [Fact]
    public void A_retry_after_an_ambiguous_result_returns_the_recorded_outcome() =>
        Pending.Claim(
            "LedgerClaim.Settled carrying the outcome, which a boolean ledger cannot do");

    [Fact]
    public void A_late_vote_cannot_change_a_returned_decision() =>
        Pending.Claim(
            "a deadline that closes decision collection and leaves evidence collection open");

    [Fact]
    public void A_duplicate_settlement_does_not_move_the_evidence_twice() =>
        Pending.Claim(
            "SettlementDelta carrying the sender's new counts rather than an increment");

    [Fact]
    public void Evidence_converges_to_one_value_under_every_delivery_order() =>
        Pending.Claim(
            "the property test over generated loss, duplication and reordering schedules");

    [Fact]
    public void A_configuration_mismatch_is_refused_rather_than_absorbed() =>
        Pending.Claim(
            "the fingerprint checked on every message, and a Refusal counted separately from "
            + "a message that was lost");

    [Fact]
    public void A_holder_that_leaves_and_rejoins_does_not_lose_its_shard() =>
        Pending.Claim("per-holder evidence shards surviving a departure and a return");
}
