using Plexus.Core.Knowledge;

namespace Plexus.Engine.Learning;

/// <summary>
/// Proposing commitments from adjacent observations.
/// </summary>
/// <remarks>
/// <para>
/// Bounded enumeration over what changed. A variable is introduced where two or more ground
/// examples share relational structure with different entities, which is the only place
/// generality is claimed rather than assumed.
/// </para>
/// <para>
/// Deviation from the skeleton document, and only in where it lives. Every other contract in
/// this design is declared in <c>Plexus.Core</c>; the learning contracts are declared in
/// <c>Plexus.Engine</c> because the document puts them there. If nothing outside the engine
/// ever implements them the split is harmless, and if something does, they move.
/// </para>
/// </remarks>
public interface ICandidateGenerator
{
    IEnumerable<Commitment> Generate(Observation before, Observation after);
}

public sealed class AdjacentObservationCandidates : ICandidateGenerator
{
    public IEnumerable<Commitment> Generate(Observation before, Observation after) =>
        throw new NotImplementedException();
}
