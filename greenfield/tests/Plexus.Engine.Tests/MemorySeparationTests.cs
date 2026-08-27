namespace Plexus.Engine.Tests;

/// <summary>
/// Four memory responsibilities that may share a database and may not share semantics.
/// </summary>
public sealed class MemorySeparationTests
{
    [Fact]
    public void A_newer_state_supersedes_the_current_value_without_rewriting_the_episode() =>
        Pending.Claim("KeyedCurrentState.Apply and an append-only EpisodeStore");

    [Fact]
    public void Independent_sources_may_leave_a_state_explicitly_conflicted() =>
        Pending.Claim(
            "ClaimStatus.Conflicting reached rather than arrival order picking a winner");

    [Fact]
    public void Unrelated_state_is_untouched_by_a_correction() =>
        Pending.Claim("keying, and the control that a wide overwrite would fail");

    [Fact]
    public void A_multi_valued_relation_keeps_both_of_its_facts() =>
        Pending.Claim(
            "IStateKeyPolicy over argument positions. Keyed on the first argument alone, a "
            + "container relation holds one thing, which is the reading that decides whether "
            + "the document's StateKey(Relation, Subject) survives");

    [Fact]
    public void A_correction_is_findable_from_the_observation_it_corrects() =>
        Pending.Claim("linked observations, since the episode may not be edited");
}
