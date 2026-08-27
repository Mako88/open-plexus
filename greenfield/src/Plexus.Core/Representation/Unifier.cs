namespace Plexus.Core.Representation;

/// <summary>
/// Matching that returns the environment rather than a yes or no.
/// </summary>
/// <remarks>
/// A boolean result loses what the expectation would be grounded in, and role-correct
/// transfer to novel fillers is exactly that environment.
/// </remarks>
public interface IUnifier
{
    bool TryMatch(FactPattern pattern, GroundFact fact, BindingBuilder bindings);

    IEnumerable<Bindings> MatchAll(
        IReadOnlyList<FactPattern> patterns,
        IReadOnlyCollection<GroundFact> facts);

    GroundFact Ground(FactPattern pattern, Bindings bindings);
}

/// <summary>
/// Indexed backtracking over a conjunction of patterns.
/// </summary>
/// <remarks>
/// <para>
/// The intended order is: take the pattern with the fewest candidate facts, try each
/// candidate against a copy of the current environment, recurse into the rest, and yield the
/// complete environments. Facts are indexed by relation and arity; an argument-position
/// index is added when a measurement asks for one.
/// </para>
/// <para>
/// Deviation from the skeleton document, and a small one. The document lists
/// <c>Unifier.cs</c> under <c>Plexus.Core</c>, which puts the one piece of behaviour in the
/// project otherwise defined as types and contracts. It is left here so the layout matches
/// what was proposed, and it is the first thing to move if <c>Core</c> is to stay inert.
/// </para>
/// </remarks>
public sealed class Unifier : IUnifier
{
    public bool TryMatch(FactPattern pattern, GroundFact fact, BindingBuilder bindings) =>
        throw new NotImplementedException();

    public IEnumerable<Bindings> MatchAll(
        IReadOnlyList<FactPattern> patterns,
        IReadOnlyCollection<GroundFact> facts) =>
        throw new NotImplementedException();

    public GroundFact Ground(FactPattern pattern, Bindings bindings) =>
        throw new NotImplementedException();
}
