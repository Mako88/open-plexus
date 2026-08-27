using Plexus.Core.Agency;
using Plexus.Core.Cognition;
using Plexus.Core.Knowledge;
using Plexus.Core.Memory;

namespace Plexus.Engine.Cognition;

/// <summary>What one round decided and what it owes the outside world.</summary>
/// <remarks>
/// The durable changes are returned rather than published from inside the cycle. Publishing
/// from the middle of a round is what makes a failure halfway through leave a fleet in a
/// state no replay can reach.
/// </remarks>
public sealed record CycleResult(
    Decision Decision,
    ImmutableArray<Prediction> Issued,
    ImmutableArray<Settlement> Settled,
    ImmutableArray<Commitment> Learned,
    StateUpdateResult State);

public interface ICognitiveCycle
{
    ValueTask<CycleResult> RunAsync(
        Observation observation,
        ImmutableArray<Goal> activeGoals,
        CancellationToken ct);
}

/// <summary>
/// The nine operations of one round, in order.
/// </summary>
/// <remarks>
/// <para>
/// The coordinator sequences services and holds no policy of its own. A learning rule that
/// lives here is a rule no arm can be swapped out of.
/// </para>
/// <para>
/// Settlement failing must not stop the observation reaching episodic memory. State that has
/// to agree goes in one unit of work; what has to leave the process goes in an outbox.
/// </para>
/// </remarks>
public sealed class CognitiveCycle(
    ISettler settler,
    ICurrentState currentState,
    IEpisodeStore episodes,
    IRetriever retriever,
    IAttentionPolicy attention,
    IPredictor predictor,
    IInquiryPolicy inquiry,
    IPlanner planner,
    EngineSettings settings) : ICognitiveCycle
{
    private readonly ISettler _settler = settler;
    private readonly ICurrentState _currentState = currentState;
    private readonly IEpisodeStore _episodes = episodes;
    private readonly IRetriever _retriever = retriever;
    private readonly IAttentionPolicy _attention = attention;
    private readonly IPredictor _predictor = predictor;
    private readonly IInquiryPolicy _inquiry = inquiry;
    private readonly IPlanner _planner = planner;
    private readonly EngineSettings _settings = settings;

    public ValueTask<CycleResult> RunAsync(
        Observation observation,
        ImmutableArray<Goal> activeGoals,
        CancellationToken ct) =>
        // 1. Settle the predictions issued last round against this observation.
        // 2. Append the observation to episodic memory.
        // 3. Apply its state claims, with supersession and conflict made explicit.
        // 4. Retrieve a bounded set of semantic artifacts.
        // 5. Admit what the round can afford, and assemble the coalition.
        // 6. Match commitments and issue grounded predictions.
        // 7. Propose actions and informative interventions.
        // 8. Plan within the budget.
        // 9. Return the decision and the durable changes to publish.
        throw new NotImplementedException();
}
