using Plexus.Core.Cognition;
using Plexus.Core.Memory;

namespace Plexus.Engine.Memory;

/// <summary>
/// The procedure store, which holds nothing in the first slice.
/// </summary>
/// <remarks>
/// It exists so that consolidation has a seam to arrive at. An empty store that is asked and
/// returns nothing is honest; a missing store makes the absence invisible.
/// </remarks>
public sealed class ProcedureStore : IProcedureStore
{
    public IAsyncEnumerable<Procedure> FindApplicableAsync(
        WorkingCoalition coalition,
        RetrievalBudget budget,
        CancellationToken ct) =>
        throw new NotImplementedException();
}
