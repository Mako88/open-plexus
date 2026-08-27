namespace Plexus.Core.Representation;

/// <summary>A constant or a variable in one argument position.</summary>
public abstract record Term
{
    private Term() { }

    public sealed record Constant(EntityId Entity) : Term;

    public sealed record Variable(VariableId VariableId) : Term;
}
