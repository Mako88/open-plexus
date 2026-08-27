namespace Plexus.Engine.Tests;

/// <summary>
/// Whether acting and watching are different evidence.
/// </summary>
public sealed class InterventionTests
{
    [Fact]
    public void Passive_correlation_does_not_settle_an_interventional_expectation() =>
        Pending.Claim(
            "AcquisitionKind reaching the settler, and the smallest confounded world");

    [Fact]
    public void An_intervention_selects_the_correct_conditional_effect() =>
        Pending.Claim("operators, intervention-marked observations, and the world that hides "
            + "one regime variable");

    [Fact]
    public void An_observational_predictor_stays_uncertain_on_the_same_evidence() =>
        Pending.Claim(
            "the control: the same run with the intervention marks stripped must not reach "
            + "the answer, or the world was never confounded");

    [Fact]
    public void An_expectation_of_absence_abstains_outside_a_closed_domain() =>
        Pending.Claim(
            "ObservationDomain reaching settlement, so DoesNotHold settles only where the "
            + "relation was reported exhaustively");
}
