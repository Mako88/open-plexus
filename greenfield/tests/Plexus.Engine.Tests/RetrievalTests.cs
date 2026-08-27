namespace Plexus.Engine.Tests;

/// <summary>
/// Whether structure is doing the retrieving, measured against controls that cost the same.
/// </summary>
/// <remarks>
/// Recall and downstream decision quality are separate readings. A retriever can raise one
/// while costing the other, and a single score cannot say which happened.
/// </remarks>
public sealed class RetrievalTests
{
    [Fact]
    public void A_displaced_fact_is_retrieved_under_a_fixed_budget() =>
        Pending.Claim("StructuralRetriever, and a world that puts the fact out of reach");

    [Fact]
    public void Structural_retrieval_beats_no_retrieval_on_held_out_seeds() =>
        Pending.Claim("the floor arm, which only shows that more artifacts help");

    [Fact]
    public void Structural_retrieval_beats_the_same_number_of_arbitrary_artifacts() =>
        Pending.Claim(
            "the arm that decides it: RandomRetriever at the same budget, on held-out seeds");

    [Fact]
    public void Recall_and_decision_quality_are_reported_separately() =>
        Pending.Claim("two readings rather than one, and the run that prints both");

    [Fact]
    public void Retrieval_is_the_same_for_the_same_query_and_seed() =>
        Pending.Claim(
            "deterministic ranking with an explicit tie break, since two holders ranking ties "
            + "differently retrieve different sets from the same store");
}
