using Plexus.Core.Cognition;
using Plexus.Core.Knowledge;
using Plexus.Core.Representation;

namespace Plexus.Core.Agency;

public readonly record struct GoalId(SemanticId Value);

public readonly record struct ActionId(SemanticId Value);

public readonly record struct OperatorId(SemanticId Value);

/// <summary>
/// A state the machine is trying to bring about.
/// </summary>
/// <remarks>
/// A goal, a fact and a prediction are three types. Observing that something desirable holds,
/// or predicting that it will, grants no authority to act, and one type for all three is how
/// that authority gets granted by accident.
/// </remarks>
public sealed record Goal(
    GoalId Id,
    FactPattern Desired,
    double Priority,
    Provenance Provenance);

/// <summary>What one action costs, as far as anything knows.</summary>
public readonly record struct CostEstimate(double Expected, double Uncertainty);

/// <summary>An action the machine can take, with what it needs and what it is thought to do.</summary>
public sealed record Operator(
    OperatorId Id,
    FactPattern Action,
    ImmutableArray<FactPattern> Preconditions,
    ImmutableArray<ExpectationTemplate> Effects,
    CostEstimate Cost,
    EvidenceRecord Evidence);

public sealed record PlannedAction(
    ActionId Id,
    OperatorId Operator,
    Bindings Bindings);

/// <summary>One commitment and where its evidence currently stands.</summary>
public sealed record Hypothesis(
    CommitmentId Commitment,
    EvidenceVerdict Verdict);

/// <summary>An intervention chosen for what its outcome would separate.</summary>
public sealed record Experiment(
    PlannedAction Action,
    ImmutableArray<Hypothesis> Distinguishes,
    double ExpectedDecisionImprovement,
    double Cost);

/// <summary>What one cognitive cycle decided to do.</summary>
public abstract record Decision
{
    private Decision() { }

    public sealed record Act(PlannedAction Action) : Decision;

    public sealed record Ask(Experiment Experiment) : Decision;

    public sealed record Answer(ImmutableArray<GroundFact> Facts) : Decision;

    /// <summary>
    /// Declining to act, and what was live when it declined.
    /// </summary>
    /// <remarks>
    /// Deviation from the skeleton document, which carries a <c>string Reason</c>. A string
    /// cannot be checked, so an abstention would be indistinguishable from a machine that
    /// simply produced nothing. Naming the hypotheses that could not be separated makes the
    /// abstention a signature a test can require.
    /// </remarks>
    public sealed record Abstain(ImmutableArray<Hypothesis> Unresolved) : Decision;
}

public readonly record struct PlanningBudget(int MaximumStates, int MaximumDepth);

/// <summary>
/// Choosing between acting, asking, answering and abstaining.
/// </summary>
/// <remarks>
/// The first planner is bounded beam search. A search state holds a set of possible worlds
/// rather than one, so alternatives survive a step instead of the most likely successor being
/// taken as settled.
/// </remarks>
public interface IPlanner
{
    Decision Choose(
        WorkingCoalition coalition,
        IReadOnlyCollection<Operator> operators,
        PlanningBudget budget);
}

/// <summary>
/// Choosing what to find out.
/// </summary>
/// <remarks>
/// The value of an experiment is the decision loss it removes, less what it costs and what
/// waiting for it costs. An outcome that changes nothing the machine was going to do is worth
/// nothing however surprising it is, and the controls are random inquiry, surprise-seeking
/// and never asking.
/// </remarks>
public interface IInquiryPolicy
{
    Experiment? Choose(
        WorkingCoalition coalition,
        IReadOnlyCollection<Hypothesis> liveHypotheses,
        IReadOnlyCollection<Operator> availableInterventions);
}
