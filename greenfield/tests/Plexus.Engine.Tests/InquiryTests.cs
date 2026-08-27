namespace Plexus.Engine.Tests;

/// <summary>
/// Whether the machine pays for information for the right reason.
/// </summary>
public sealed class InquiryTests
{
    [Fact]
    public void The_chosen_question_distinguishes_hypotheses_that_change_the_decision() =>
        Pending.Claim("ValueOfInformationInquiry over live hypotheses");

    [Fact]
    public void Irrelevant_surprise_does_not_win_the_inquiry_budget() =>
        Pending.Claim(
            "the arm that separates wanting to know from wanting to be less surprised, "
            + "against SurpriseSeekingInquiry on the same world");

    [Fact]
    public void Nothing_is_paid_where_both_answers_lead_to_the_same_action() =>
        Pending.Claim(
            "the expected-decision-improvement term actually reaching nought, checked on a "
            + "world where the rival hypotheses agree about what to do");

    [Fact]
    public void The_agent_abstains_before_the_confounding_evidence_is_resolved() =>
        Pending.Claim(
            "Decision.Abstain carrying the hypotheses it could not separate, which is what "
            + "makes an abstention a signature rather than an absence of output");

    [Fact]
    public void Never_asking_loses_on_the_world_that_requires_an_intervention() =>
        Pending.Claim("the NeverAsk floor, without which the inquiry claim has no denominator");
}
