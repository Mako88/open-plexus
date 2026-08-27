namespace Plexus.Core.Representation;

/// <summary>
/// The identity of a semantic artifact, derived from its canonical bytes.
/// </summary>
/// <remarks>
/// A hundred and twenty-eight bits, and never a runtime hash. Two holders must arrive at the
/// same identity for the same artifact in separate processes, so nothing derived from
/// <see cref="object.GetHashCode"/>, array object identity, insertion order or endianness may
/// reach this value.
/// </remarks>
public readonly record struct SemanticId(ulong High, ulong Low)
{
    public override string ToString() => $"{High:x16}{Low:x16}";
}

public readonly record struct RelationId(SemanticId Value);

public readonly record struct EntityId(SemanticId Value);

public readonly record struct GroundingId(SemanticId Value);

public readonly record struct VariableId(int Value);

/// <summary>
/// An entity is an identity and the groundings that introduced it.
/// </summary>
/// <remarks>
/// It carries no attributes. What the entity means is obtained by querying the facts and
/// commitments it participates in, which is what stops a concept being a record the
/// experimenter filled in.
/// </remarks>
public sealed record ConceptRecord(
    EntityId Id,
    ImmutableHashSet<GroundingId> Groundings);
