using Plexus.Core.Agency;
using Plexus.Core.Cognition;

namespace Plexus.Engine.Agency;

/// <summary>
/// Bounded beam search over learned operators.
/// </summary>
/// <remarks>
/// A search state carries a set of possible worlds, the path cost, the predicted goal
/// satisfaction and the uncertainty. Taking the most likely successor at every step throws
/// away the alternatives the next observation was going to decide between.
/// </remarks>
public sealed class BeamPlanner : IPlanner
{
    public Decision Choose(
        WorkingCoalition coalition,
        IReadOnlyCollection<Operator> operators,
        PlanningBudget budget) =>
        throw new NotImplementedException();
}
