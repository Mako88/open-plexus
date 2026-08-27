namespace Plexus.Core.Tests;

/// <summary>
/// Whether a rule transfers by binding or by having seen the entities before.
/// </summary>
/// <remarks>
/// Each of these needs its control disconnected before it is believed. A conjunction of codes
/// with no variables passes a transfer test whenever the new fillers happen to co-occur, so
/// the control is the arm that must fail.
/// </remarks>
public sealed class RoleBindingTests
{
    [Fact]
    public void A_relation_transfers_to_novel_fillers_without_reversing_roles() =>
        Pending.Claim("indexed unification and grounding through Bindings");

    [Fact]
    public void One_variable_keeps_one_value_across_every_pattern() =>
        Pending.Claim(
            "the join: a second bind of one variable to a different entity fails the branch "
            + "rather than overwriting");

    [Fact]
    public void A_conjunction_without_variables_cannot_solve_the_permutation() =>
        Pending.Claim(
            "the control that must fail, and the one that decides whether the transfer claim "
            + "means anything");

    [Fact]
    public void Matching_returns_the_environment_rather_than_a_yes() =>
        Pending.Claim(
            "MatchAll yielding complete environments, which is what role-correct grounding "
            + "needs and what a boolean throws away");

    [Fact]
    public void Renaming_a_variable_does_not_change_which_facts_match() =>
        Pending.Claim("unification invariance under variable renaming, as a property test");

    [Fact]
    public void Two_environments_built_in_different_search_orders_are_one_value() =>
        Pending.Claim(
            "Bindings sorted by variable, so an environment reached by a different branch "
            + "order is the same value and encodes to the same bytes");
}
