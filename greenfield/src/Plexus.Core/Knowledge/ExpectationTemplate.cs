using Plexus.Core.Representation;

namespace Plexus.Core.Knowledge;

/// <summary>What a commitment says will be the case.</summary>
/// <remarks>
/// Negation is explicit. Absence of a fact is not its negation, and
/// <see cref="DoesNotHold"/> settles only where the observation reports the relation
/// exhaustively.
/// </remarks>
public abstract record ExpectationTemplate
{
    private ExpectationTemplate() { }

    public sealed record Holds(FactPattern Pattern) : ExpectationTemplate;

    public sealed record DoesNotHold(FactPattern Pattern) : ExpectationTemplate;
}
