namespace Plexus.Acceptance.Tests;

/// <summary>
/// The first vertical slice, one claim at a time.
/// </summary>
/// <remarks>
/// <para>
/// Nine steps and eight claims, and each claim needs a control that isolates it. One
/// end-to-end score over the whole sequence says a machine did well and never says which
/// mechanism did it, which is the reading that makes a wrong repair look like a fix.
/// </para>
/// <para>
/// Before any of these is believed, disconnect or invert the mechanism it names and show that
/// it goes red. A claim that stays green with its mechanism removed was never asking about
/// the mechanism.
/// </para>
/// </remarks>
public sealed class RelationalInterventionWorldTests
{
    [Fact]
    public void Identity_survives_an_irrelevant_attribute_change() =>
        Pending.Claim("the world, and an entity whose visible attribute changes");

    [Fact]
    public void A_role_bound_commitment_transfers_to_novel_fillers() =>
        Pending.Claim("the permuted-role step, with the variable-free control failing it");

    [Fact]
    public void A_displaced_fact_is_retrieved_under_a_fixed_budget() =>
        Pending.Claim("the step that pushes a required fact past the coalition budget");

    [Fact]
    public void The_agent_abstains_before_the_confound_is_resolved() =>
        Pending.Claim("Decision.Abstain naming the hypotheses it could not separate");

    [Fact]
    public void The_discriminating_intervention_is_chosen_over_a_surprising_irrelevance() =>
        Pending.Claim("both arms present in the same moment, so the choice is a choice");

    [Fact]
    public void The_intervention_result_is_used_to_reach_the_goal() =>
        Pending.Claim("planning over the operator the settled commitment licensed");

    [Fact]
    public void A_holder_leaving_and_rejoining_does_not_stop_evidence_converging() =>
        Pending.Claim("the departure step under the unreliable transport");

    [Fact]
    public void The_whole_sequence_runs_without_the_front_end_supplying_an_answer() =>
        Pending.Claim(
            "the ceiling reading: how often the answer is already present in the moment the "
            + "world produces, before anything has learnt. An arm may raise that and may "
            + "never do it silently");
}
