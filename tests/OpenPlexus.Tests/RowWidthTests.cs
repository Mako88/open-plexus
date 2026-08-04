using System.Runtime.CompilerServices;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What one row entry COSTS, in bytes — <b>a budget, like the dials and the doc
/// and the clones.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE ROW IS THE SCALING WALL AND ITS WIDTH WAS NEVER A NUMBER ANYBODY
/// CHECKED.</b> Cost per thought is set by the widest row, not by the node count,
/// and every structural idea this project has wants to put something in it: edge
/// kinds did, supersession did, eviction metadata will, and dynamic relations
/// would. <b>The plan says the row gets widened ONCE</b> — and a rule like that
/// needs a number, or the second widening arrives looking like the first.
/// </para>
/// <para>
/// <b>PADDING IS WHY THIS HAS TO BE MEASURED RATHER THAN COUNTED.</b> A field that
/// looks like four bytes may cost eight or nothing at all depending on what sits
/// beside it, so <i>how much would this cost</i> is not answerable by adding up
/// the types. <see cref="Unsafe.SizeOf{T}"/> is what the runtime actually
/// allocates.
/// </para>
/// </remarks>
public sealed class RowWidthTests(ITestOutputHelper output)
{
    /// <summary>
    /// What a relation cost while it was still an enum.
    /// </summary>
    /// <remarks>
    /// <b>THE SHAPE THAT WAS REPLACED, KEPT SO THE CLAIM CAN STILL FAIL.</b>
    /// <see cref="Kind"/> now derives a <see cref="ulong"/> name and this file used
    /// to measure that against the enum — but once the derived name was adopted,
    /// comparing <see cref="Edge"/> to a hand-written copy of itself was a check
    /// that could not fire, which is what TRAPS names. Measuring against what was
    /// actually given up is the version that can.
    /// </remarks>
    private enum Fixed
    {
        With,
    }

    private readonly record struct Enumerated(Code Partner, Fixed Relation);

    private readonly record struct Coded(Code Partner, Code Relation);

    [Fact]
    public void The_row_entry_costs_what_it_is_budgeted()
    {
        var key = Unsafe.SizeOf<Edge>();
        var value = Unsafe.SizeOf<Tie>();

        output.WriteLine($"Code   {Unsafe.SizeOf<Code>(),3}");
        output.WriteLine($"Edge   {key,3}  (Code + derived Kind)");
        output.WriteLine($"Tie    {value,3}  (count + when)");
        output.WriteLine($"entry  {key + value,3}");
        output.WriteLine($"was    {Unsafe.SizeOf<Enumerated>(),3}  (Code + enum relation)");
        output.WriteLine($"Coded  {Unsafe.SizeOf<Coded>(),3}  (Code + Code relation)");

        // THE BUDGET. The number is what it is today; having one is the point, and
        // it moves only when somebody decides it should — which is the same
        // mechanism the doc's word count and the dial count run on.
        Assert.Equal(24, key);
        Assert.Equal(16, value);
    }

    [Fact]
    public void A_relation_named_by_a_derived_number_costs_nothing_extra()
    {
        // THE ANSWER TO "WHAT DOES DYNAMIC COST", AND IT IS A SURPRISE: nothing.
        // The enum was an `int`, and the four bytes it needed were followed by four
        // bytes of padding to align the struct — so the `ulong` naming the relation
        // landed exactly in the space the enum was already wasting.
        //
        // SO THE ALPHABET OF RELATIONS GREW FOR FREE, and the argument against it
        // was never memory. It is that a derived name must be agreed by every
        // machine with nothing to ask, which is `Chunk`'s trick and not a new
        // problem.
        Assert.Equal(Unsafe.SizeOf<Enumerated>(), Unsafe.SizeOf<Edge>());
    }

    [Fact]
    public void And_giving_a_relation_its_own_modality_costs_a_third_more()
    {
        // THE OTHER SHAPE, AND IT IS NOT FREE. A full `Code` carries a modality as
        // well as a value, which would let a walk be narrowed to a FAMILY of
        // relations — every evaluative one, say — the way endpoints are narrowed
        // today. That capability costs eight bytes on the one structure that
        // cannot afford them.
        var entry = Unsafe.SizeOf<Edge>() + Unsafe.SizeOf<Tie>();
        var coded = Unsafe.SizeOf<Coded>() + Unsafe.SizeOf<Tie>();

        output.WriteLine($"entry {entry} -> {coded} ({(coded - entry) / (double)entry:P0})");

        Assert.True(coded > entry,
            "a relation carrying a modality has stopped costing more than one "
            + "carrying only a name, so the cheap option buys nothing and this "
            + "trade-off no longer exists");
    }
}
