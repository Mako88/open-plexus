using Plexus.Core.Knowledge;
using Plexus.Core.Memory;
using Plexus.Core.Representation;

namespace Plexus.Engine.Memory;

/// <summary>
/// Current state held by key, with supersession and conflict made explicit.
/// </summary>
/// <remarks>
/// Two sources at the same key with different facts leave the key
/// <see cref="ClaimStatus.Conflicting"/>. Arrival order does not decide it, because a
/// disagreement resolved by arrival order is a disagreement nothing can see.
/// </remarks>
public sealed class KeyedCurrentState(IStateKeyPolicy keys) : ICurrentState
{
    private readonly IStateKeyPolicy _keys = keys;

    public IReadOnlyCollection<StateClaim> Read(StateKey key) =>
        throw new NotImplementedException();

    public IReadOnlyCollection<GroundFact> Snapshot() =>
        throw new NotImplementedException();

    public StateUpdateResult Apply(Observation observation) =>
        throw new NotImplementedException();

    /// <summary>The key one fact lands on, under the policy this state was built with.</summary>
    private StateKey KeyOf(GroundFact fact) =>
        new()
        {
            Relation = fact.Relation,
            KeyArguments = [.. _keys.KeyPositions(fact.Relation).Select(at => fact.Arguments[at])],
        };
}
