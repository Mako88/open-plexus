using System.Reflection;
using System.Runtime.CompilerServices;
using Plexus.Core.Representation;
using Plexus.Distributed;
using Plexus.Engine;
using Plexus.Worlds;

namespace Plexus.Acceptance.Tests;

/// <summary>
/// Invariants of the layout, checked rather than agreed.
/// </summary>
/// <remarks>
/// These are the two rules of the skeleton that a reviewer cannot hold in their head across
/// forty files: which project may name which, and which record may be compared with
/// <c>==</c>.
/// </remarks>
public sealed class StructureTests
{
    private static IReadOnlyList<Assembly> Product() =>
    [
        typeof(SemanticId).Assembly,
        typeof(EngineSettings).Assembly,
        typeof(RoundId).Assembly,
        typeof(WorldSeed).Assembly,
    ];

    [Fact]
    public void The_representation_project_names_nothing_else()
    {
        var named = typeof(SemanticId).Assembly
            .GetReferencedAssemblies()
            .Select(one => one.Name)
            .Where(name => name is not null && name.StartsWith("Plexus", StringComparison.Ordinal))
            .ToList();

        Assert.True(named.Count == 0,
            "Plexus.Core is the standalone representation layer and references "
            + $"{string.Join(", ", named)}.");
    }

    [Fact]
    public void A_world_cannot_name_the_brain()
    {
        var named = typeof(WorldSeed).Assembly
            .GetReferencedAssemblies()
            .Select(one => one.Name)
            .Where(name => name is "Plexus.Engine" or "Plexus.Distributed")
            .ToList();

        Assert.True(named.Count == 0,
            "A world reaching the engine is a world that can be edited until the brain scores "
            + $"better. Plexus.Worlds references {string.Join(", ", named)}.");
    }

    /// <summary>
    /// Every record carrying a sequence decides its own equality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Red on purpose, and it is the largest single finding in the skeleton as proposed. A
    /// generated record equals compares an <see cref="ImmutableArray{T}"/> member by the
    /// underlying array's object identity, so two records built from equal contents are
    /// unequal and one record round-tripped through a copy is unequal to itself.
    /// </para>
    /// <para>
    /// There are two ways to close an entry, and which one is right differs per type. A
    /// durable artifact writes its equality out, or takes it from its identity, which is
    /// derived from canonical bytes. A transient one that nothing should ever compare stops
    /// being a <c>record</c>, because the point of the keyword is the equality it generates.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_record_carrying_a_sequence_decides_its_own_equality()
    {
        var undecided = Product()
            .SelectMany(one => one.GetTypes())
            .Where(IsRecord)
            .Where(CarriesASequence)
            .Where(one => !DecidesItsOwnEquality(one))
            .Select(one => one.FullName ?? one.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(undecided.Count == 0,
            $"{undecided.Count} record(s) carry a sequence and take the generated equality, "
            + "which compares the sequence by object identity:\n  "
            + string.Join("\n  ", undecided));
    }

    /// <summary>The companion, without which the check above could match nothing and pass.</summary>
    /// <remarks>
    /// The two controls are a record that decides and a record that does not, so a detector
    /// that has stopped recognising either shape fails here rather than reading as a clean
    /// tree.
    /// </remarks>
    [Fact]
    public void The_equality_check_can_still_fire()
    {
        Assert.True(IsRecord(typeof(Undecided)));
        Assert.True(CarriesASequence(typeof(Undecided)));
        Assert.False(DecidesItsOwnEquality(typeof(Undecided)));

        Assert.True(DecidesItsOwnEquality(typeof(GroundFact)));
        Assert.True(CarriesASequence(typeof(GroundFact)));
    }

    private sealed record Undecided(ImmutableArray<int> Values);

    private static bool IsRecord(Type type) =>
        !type.IsNested
            ? type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null
            : type.GetMethod("<Clone>$", BindingFlags.NonPublic | BindingFlags.Public
                | BindingFlags.Instance) is not null;

    private static bool CarriesASequence(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Any(one => IsASequence(one.PropertyType));

    private static bool IsASequence(Type type)
    {
        if (type.IsArray) return true;
        if (!type.IsGenericType) return false;

        var open = type.GetGenericTypeDefinition();

        return open == typeof(ImmutableArray<>)
            || open == typeof(ImmutableHashSet<>)
            || open == typeof(ImmutableList<>)
            || open == typeof(ImmutableSortedSet<>)
            || open == typeof(ImmutableDictionary<,>)
            || open == typeof(ImmutableSortedDictionary<,>);
    }

    /// <summary>
    /// Whether the equality on this record was written rather than generated.
    /// </summary>
    /// <remarks>
    /// A synthesised member carries <see cref="CompilerGeneratedAttribute"/> and a written one
    /// does not, which is the only signal that separates them at runtime.
    /// </remarks>
    private static bool DecidesItsOwnEquality(Type type)
    {
        var equals = type.GetMethod(
            "Equals",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
            binder: null,
            types: [type],
            modifiers: null);

        return equals is not null
            && equals.GetCustomAttribute<CompilerGeneratedAttribute>() is null;
    }
}
