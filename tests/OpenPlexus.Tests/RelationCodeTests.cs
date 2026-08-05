using OpenPlexus.Codes;
using OpenPlexus.Graph;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// A relation as a CODE — <b>route one of step 8's fork, and the hub check that
/// gates it.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE CLAIM: a fact about a relation becomes an ordinary count.</b> A relation
/// is a key into a row and never a partner in one, so <i>north-of is the inverse of
/// south-of</i> has nowhere to live even though the two co-occur on every single
/// observation. <see cref="Kind.Code"/> puts a relation on its own reserved
/// modality and the existing machinery does the rest.
/// </para>
/// <para>
/// <b>THE RISK IS THAT IT IS A SUPERHUB, AND THIS FILE IS THAT CHECK.</b> A
/// relation co-occurs with EVERY code it ever related, so its row grows without
/// bound. Grains failed exactly here — a coarse code weighed nearly one against its
/// own fine code, the hop was free, and depth exploded. <b>This asks whether the
/// relation-level fact can still be read off a row like that</b> before anything is
/// built on top of it.
/// </para>
/// </remarks>
public sealed class RelationCodeTests(ITestOutputHelper output)
{
    private static readonly Kind Above = Kind.Of("north-of");
    private static readonly Kind Below = Kind.Of("south-of");

    private const int Pairs = 200;

    /// <summary>
    /// The relation node as observation would leave it: paired once with every
    /// landmark it ever related, and on every occasion with its own inverse.
    /// </summary>
    private static Node Related(int cap = Fixture.Unbounded)
    {
        var node = new Node(Above.Code, Fixture.Dials(stamina: 10.0) with { Row = cap });

        for (var pair = 0; pair < Pairs; pair++)
        {
            // Two landmarks met once each in this relation, and the inverse
            // relation met on the same occasion -- which is what makes it the
            // inverse rather than merely another relation.
            node.Note();

            node.Observe(Fixture.C((ulong)(pair * 2)), 1.0, Kind.With, when: pair);
            node.Observe(Fixture.C((ulong)((pair * 2) + 1)), 1.0, Kind.With, when: pair);
            node.Observe(Below.Code, 1.0, Kind.With, when: pair);
        }

        return node;
    }

    [Fact]
    public void A_relation_has_an_identity_a_code_can_be_built_from()
    {
        // IT COSTS NOTHING NEW, which is the whole appeal of this route. The
        // identity is the one `Kind.Of` already derives and every machine already
        // agrees on without asking.
        Assert.Equal(Above.Code, Kind.Of("north-of").Code);
        Assert.NotEqual(Above.Code, Below.Code);

        // ON A RESERVED MODALITY, so a relation can never collide with something a
        // sense produced.
        Assert.Equal(Kind.Relations, Above.Code.Modality);
    }

    [Fact]
    public void The_inverse_outranks_four_hundred_landmarks_on_the_same_row()
    {
        // THE GATE. The relation's row holds four hundred landmarks it met once and
        // one relation it met every time. If the fact about relations cannot be read
        // off a row that shape, route one is dead before it starts.
        var node = Related();

        var fired = node.Fire(Fixture.Origin(Above.Code));

        var ranked = fired.Outgoing
            .OrderByDescending(one => one.Together)
            .Select(one => one.To)
            .ToList();

        output.WriteLine(
            $"row {node.Entries} entries; strongest partner is "
            + $"{(ranked[0] == Below.Code ? "the inverse relation" : "a landmark")}");

        Assert.Equal(Below.Code, ranked[0]);

        // AND BY A MARGIN THAT IS NOT A TIE. Co-occurring on every occasion against
        // co-occurring once is the whole signal, and it survives the row being four
        // hundred wide.
        Assert.Equal((double)Pairs, node.Together(Below.Code));
        Assert.Equal(1.0, node.Together(Fixture.C(0)));
    }

    [Fact]
    public void But_the_row_is_the_superhub_the_grains_warned_about()
    {
        // THE COST, AND IT IS THE REASON THIS IS A GATE RATHER THAN A GREEN LIGHT.
        // A relation node's row grows with every code it ever related, and
        // `Node.Fire` emits one message per ENTRY -- so walking INTO a relation
        // fans out to everything that relation has ever touched. That is what made
        // grains explode: a hop that costs nothing and reaches everything.
        var node = Related();

        var fired = node.Fire(Fixture.Origin(Above.Code));

        output.WriteLine($"fan-out from one relation node: {fired.Outgoing.Length}");

        Assert.True(fired.Outgoing.Length > Pairs,
            "the relation node is no longer a hub, so the bound below is solving a "
            + "problem that has gone away");
    }

    [Fact]
    public void And_the_row_cap_is_what_makes_it_affordable()
    {
        // THE MITIGATION ALREADY EXISTS, which is what makes route one cheap. A
        // bounded row caps the fan-out at the cap, and the relation-level fact is
        // exactly the entry a recency-ordered eviction keeps: the inverse is touched
        // on EVERY occasion and a landmark on one.
        var bounded = Related(cap: 16);

        var fired = bounded.Fire(Fixture.Origin(Above.Code));

        output.WriteLine(
            $"bounded row {bounded.Entries} entries, fan-out {fired.Outgoing.Length}, "
            + $"inverse held {bounded.Together(Below.Code):F0}");

        Assert.Equal(16, bounded.Entries);
        Assert.True(fired.Outgoing.Length <= 16);

        // AND THE FACT SURVIVED THE EVICTION, which is the part that could have gone
        // either way -- a cap that dropped the inverse would bound the cost and
        // throw away the only thing the row was for.
        Assert.True(bounded.Together(Below.Code) > 0.0,
            "bounding the row evicted the relation-level fact, so the cap and this "
            + "route are in conflict and the eviction rule needs to know better");
    }
}
