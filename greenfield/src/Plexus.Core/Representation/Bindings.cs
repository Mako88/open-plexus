namespace Plexus.Core.Representation;

/// <summary>
/// The environment one expectation was grounded in, as a value.
/// </summary>
/// <remarks>
/// <para>
/// Deviation from the skeleton document. Section 5 proposes a single mutable
/// <c>BindingSet</c> class and section 7 then stores one on <c>Prediction</c>, which is a
/// record. A mutable class member gives that record reference equality and lets the caller
/// that produced a prediction go on editing the environment the prediction was issued
/// under. Section 3 forbids exactly that, so the type is split: this one is the value that
/// travels, and <see cref="BindingBuilder"/> is the scratch space the matcher backtracks in.
/// </para>
/// <para>
/// The pairs are held sorted by variable so that two environments built by different search
/// orders are the same value and canonically encode to the same bytes.
/// </para>
/// </remarks>
public sealed record Bindings
{
    public static readonly Bindings Empty = new() { Pairs = [] };

    public required ImmutableArray<KeyValuePair<VariableId, EntityId>> Pairs { get; init; }

    public bool TryGet(VariableId variable, out EntityId entity)
    {
        foreach (var pair in Pairs)
        {
            if (pair.Key != variable) continue;
            entity = pair.Value;
            return true;
        }

        entity = default;
        return false;
    }

    public bool Equals(Bindings? other) =>
        other is not null && Pairs.AsSpan().SequenceEqual(other.Pairs.AsSpan());

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var pair in Pairs)
        {
            hash.Add(pair.Key);
            hash.Add(pair.Value);
        }

        return hash.ToHashCode();
    }
}

/// <summary>
/// Scratch space for one branch of a match.
/// </summary>
/// <remarks>
/// One variable keeps one value: a second bind of the same variable to a different entity
/// fails rather than overwriting, which is what makes a conjunction of patterns a join.
/// </remarks>
public sealed class BindingBuilder
{
    private readonly Dictionary<VariableId, EntityId> _values = [];

    public bool TryGet(VariableId variable, out EntityId entity) =>
        _values.TryGetValue(variable, out entity);

    public bool TryBind(VariableId variable, EntityId entity)
    {
        if (_values.TryGetValue(variable, out var existing)) return existing == entity;

        _values.Add(variable, entity);
        return true;
    }

    public BindingBuilder Copy()
    {
        var copy = new BindingBuilder();
        foreach (var pair in _values) copy._values.Add(pair.Key, pair.Value);
        return copy;
    }

    /// <summary>The environment as a value, in variable order.</summary>
    public Bindings Freeze() => new()
    {
        Pairs = [.. _values.OrderBy(pair => pair.Key.Value)],
    };
}
