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
    /// What a relation would cost if it stopped being an enum.
    /// </summary>
    /// <remarks>
    /// <b>JOHN'S ASK: a static enum cannot grow, so the system cannot mint a new
    /// relation without a recompile.</b> Two shapes answer it — a derived
    /// <see cref="ulong"/> name, hashed from the relation's definition so two
    /// machines agree with nothing to ask, or a full <see cref="Code"/>, which
    /// also gives relations a modality of their own and so lets a walk be narrowed
    /// to a FAMILY of relations the way <c>BestOf</c> narrows endpoints.
    /// </remarks>
    private readonly record struct Named(Code Partner, ulong Relation);

    private readonly record struct Coded(Code Partner, Code Relation);

    [Fact]
    public void The_row_entry_costs_what_it_is_budgeted()
    {
        var key = Unsafe.SizeOf<Edge>();
        var value = Unsafe.SizeOf<Tie>();

        output.WriteLine($"Code   {Unsafe.SizeOf<Code>(),3}");
        output.WriteLine($"Edge   {key,3}  (Code + Kind)");
        output.WriteLine($"Tie    {value,3}  (count + when)");
        output.WriteLine($"entry  {key + value,3}");
        output.WriteLine($"Named  {Unsafe.SizeOf<Named>(),3}  (Code + ulong relation)");
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
        // `Kind` is an enum over `int`, and the four bytes it needs are followed by
        // four bytes of padding to align the struct — so a `ulong` naming the
        // relation lands exactly in the space the enum was already wasting.
        //
        // SO THE ALPHABET OF RELATIONS CAN GROW FOR FREE, and the argument against
        // it is not memory. It is that a derived name must be agreed by every
        // machine with nothing to ask, which is `Chunk`'s trick and not a new
        // problem.
        Assert.Equal(Unsafe.SizeOf<Edge>(), Unsafe.SizeOf<Named>());
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
