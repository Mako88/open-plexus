using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// A SLOT as a code — <b>route two of step 8's fork, and the half that buys
/// transfer.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>ROUTE ONE GAVE A FACT SOMEWHERE TO LIVE AND NOT A WAY TO APPLY IT.</b>
/// Knowing <i>north-of relates to south-of</i> does not tell a system to swap the
/// arguments. <see cref="Kind.Role"/> is the cell that does: <c>north-of/0</c>
/// against <c>south-of/1</c> says <b>whatever fills the first slot of one fills the
/// second slot of the other</b>, and that names no argument at all.
/// </para>
/// <para>
/// <b>SO ONE CELL ACCUMULATES ACROSS EVERY PAIR THE RELATION WAS EVER SEEN ON</b>,
/// which is exactly what a count between two landmark codes structurally cannot do
/// — the gap <c>BindingGapTests</c> holds open.
/// </para>
/// <para>
/// <b>AND IT STAYS ON THE RIGHT SIDE OF THE LINE.</b> The front end says <i>B fills
/// slot two</i>, which is an observation it genuinely has. It does not say
/// <i>south-of is north-of reversed</i> — that is the fact in question, and handing
/// it over would make this a lookup table.
/// </para>
/// </remarks>
public sealed class RoleTests(ITestOutputHelper output)
{
    private static readonly Kind Above = Kind.Of("north-of");
    private static readonly Kind Below = Kind.Of("south-of");

    private static Code Land(ulong which) => new(Modality: 7, which);

    /// <summary>
    /// One arrangement, told twice: <c>north-of(high, low)</c> and its inverse.
    /// </summary>
    private static async Task SeenAsync(LocalRendezvous join, Code high, Code low, long at)
    {
        await join.JoinAsync(new Occasion
        {
            Onsets = [high, low],
            Live = [],
            At = at,
            As = Above,
            Roles = new Dictionary<Code, int> { [high] = 0, [low] = 1 },
        });

        await join.JoinAsync(new Occasion
        {
            Onsets = [high, low],
            Live = [],
            At = at,
            As = Below,
            Roles = new Dictionary<Code, int> { [low] = 0, [high] = 1 },
        });
    }

    [Fact]
    public void A_slot_is_derived_and_is_not_the_relation_itself()
    {
        // AGREED WITHOUT ASKING, like everything else that has to be a code. The
        // slot is folded into the same hash, so two machines hold `north-of/0`
        // alike with no table to share.
        Assert.Equal(Above.Role(0), Kind.Of("north-of").Role(0));

        Assert.NotEqual(Above.Role(0), Above.Role(1));
        Assert.NotEqual(Above.Role(0), Below.Role(0));
        Assert.NotEqual(Above.Role(0), Above.Code);

        Assert.Equal(Kind.Relations, Above.Role(0).Modality);
    }

    [Fact]
    public async Task One_cell_gathers_every_pair_the_relation_was_seen_on()
    {
        // THE CLAIM ROUTE ONE COULD NOT MAKE. Four landmarks, two arrangements,
        // nothing in common between them -- and ONE cell counts both, because the
        // cell is about the slot rather than about what filled it.
        using var bench = new Bench(Fixture.Dials(stamina: 10.0));

        await SeenAsync(bench.Rendezvous, Land(1), Land(2), at: 1);
        await SeenAsync(bench.Rendezvous, Land(3), Land(4), at: 2);

        var slot = bench.Node(Above.Role(0));

        output.WriteLine(
            $"north-of/0 met {slot.Partners().Count} fillers, seen {slot.Seen:F0}");

        // BOTH high landmarks are in the first slot's row, and neither low one is.
        Assert.Equal(1.0, slot.Together(Land(1)));
        Assert.Equal(1.0, slot.Together(Land(3)));
        Assert.Equal(0.0, slot.Together(Land(2)));

        // AND THE SLOT ITSELF WAS SEEN TWICE, which is the count that names no
        // argument -- the thing a landmark-to-landmark cell can never be.
        Assert.Equal(2.0, slot.Seen);
    }

    [Fact]
    public async Task A_filler_meets_its_own_slot_and_no_other()
    {
        // THE `Groups` TRICK, AND WITHOUT IT THIS CHANNEL IS NOISE. In a plain
        // occasion every code pairs with every other, so a high landmark would join
        // the second slot as readily as the first and the slots would stop meaning
        // anything. `Roles` refuses that pairing exactly as `Groups` refuses a
        // colour joining both shapes.
        using var bench = new Bench(Fixture.Dials(stamina: 10.0));

        await SeenAsync(bench.Rendezvous, Land(1), Land(2), at: 1);

        // The high landmark fills north-of's FIRST slot and south-of's SECOND.
        Assert.Equal(1.0, bench.Node(Land(1)).Together(Above.Role(0)));
        Assert.Equal(1.0, bench.Node(Land(1)).Together(Below.Role(1)));

        // And neither of the others.
        Assert.Equal(0.0, bench.Node(Land(1)).Together(Above.Role(1)));
        Assert.Equal(0.0, bench.Node(Land(1)).Together(Below.Role(0)));
    }

    [Fact]
    public async Task And_a_landmark_never_seen_in_the_inverse_still_reaches_its_slot()
    {
        // TRANSFER, WHICH IS THE WHOLE POINT AND THE THING ROUTE ONE COULD NOT DO.
        // Two arrangements are seen both ways round. A third is seen ONE WAY ONLY --
        // there is no south-of observation involving it at all.
        //
        // The new landmark is nonetheless two hops from the inverse's second slot,
        // through the first slot it shares with every landmark that HAS been seen
        // both ways. The path carries no argument: it is filler -> north-of/0 ->
        // some other filler -> south-of/1, and every pair ever observed reinforces
        // the middle of it.
        using var bench = new Bench(Fixture.Dials(stamina: 10.0));

        await SeenAsync(bench.Rendezvous, Land(1), Land(2), at: 1);
        await SeenAsync(bench.Rendezvous, Land(3), Land(4), at: 2);

        // The new pair, told ONE WAY. Nothing here says anything about south-of.
        await bench.Rendezvous.JoinAsync(new Occasion
        {
            Onsets = [Land(5), Land(6)],
            Live = [],
            At = 3,
            As = Above,
            Roles = new Dictionary<Code, int> { [Land(5)] = 0, [Land(6)] = 1 },
        });

        // IT HAS NEVER MET THE INVERSE. This is the assertion that makes the next
        // one mean something.
        Assert.Equal(0.0, bench.Node(Land(5)).Together(Below.Role(1)));

        // AND YET IT IS IN THE FIRST SLOT'S ROW, alongside the two landmarks that
        // DID go both ways -- so the inverse is reachable from it without any
        // observation of the inverse involving it.
        var slot = bench.Node(Above.Role(0));

        Assert.Equal(1.0, slot.Together(Land(5)));

        var reached = slot.Partners()
            .Where(one => bench.Node(one).Together(Below.Role(1)) > 0.0)
            .ToList();

        output.WriteLine(
            $"from north-of/0, {reached.Count} of {slot.Partners().Count} fillers "
            + "carry the inverse");

        Assert.NotEmpty(reached);
        Assert.DoesNotContain(Land(5), reached);
    }

    // ---- the readout -------------------------------------------------------

    /// <summary>A front end that hands back exactly what it was given.</summary>
    private sealed class Handed : IQuantizer<Code[]>
    {
        public byte Modality => 7;

        public IReadOnlyCollection<Code> Codify(Code[] frame) => frame;
    }

    [Fact]
    public async Task A_walk_from_a_new_filler_arrives_at_the_slot_it_belongs_in()
    {
        // THE READOUT, AND IT NEEDS NO NEW MECHANISM. A role is a CODE, so a walk
        // can arrive at one and `BestOf` can narrow to the reserved modality
        // relations live on. Asking "what slot does this belong in" turns out to be
        // an ordinary question.
        //
        // THE DISCRIMINATING CLAIM IS THE SLOT AND NOT THE RELATION. Reaching
        // `south-of` at all is easy -- it is all over the graph. Reaching
        // `south-of/1` ABOVE `south-of/0` is the binding: it says the new landmark
        // goes in the SECOND slot, which is the half a fact about relations alone
        // could never supply.
        var dials = Fixture.Dials(stamina: 20.0, horizon: 8);

        using var bench = new Bench(dials, listening: true);

        var machine = new Machines.InputMachine<Code[]>(
            new Bus.MachineAddress("eye"), new Handed(),
            bench.Rendezvous, bench.Bus, bench.Ring, dials);

        bench.Subscribe(machine);

        // Four arrangements, each told BOTH ways.
        for (var pair = 0; pair < 4; pair++)
            await SeenAsync(
                bench.Rendezvous, Land((ulong)((pair * 2) + 10)),
                Land((ulong)((pair * 2) + 11)), at: pair);

        // And a fifth told ONE WAY. Nothing anywhere says this landmark is south
        // of anything.
        var fresh = Land(90);

        await bench.Rendezvous.JoinAsync(new Occasion
        {
            Onsets = [fresh, Land(91)],
            Live = [],
            At = 9,
            As = Above,
            Roles = new Dictionary<Code, int> { [fresh] = 0, [Land(91)] = 1 },
        });

        Assert.Equal(0.0, bench.Node(fresh).Together(Below.Role(1)));

        // THROUGH THE ROLE EDGE AND NOTHING ELSE. Walking `With` as well reaches
        // the other filler of this very arrangement in one hop and lands on the
        // WRONG slot at least as strongly as the right one -- measured, and it is
        // step 9's refutation arriving in a new place.
        var thought = await machine.ThinkAsync(
            [fresh], dials.Stamina, new Thinking.Question { Through = Kind.Fills });
        await bench.Bus.WhenIdle().WaitAsync(Fixture.Patience);

        var slots = thought!.BestOf(Kind.Relations, 8);

        foreach (var arrival in slots)
            output.WriteLine(
                $"{Name(arrival.Endpoint)} {arrival.Score:F4}");

        Assert.NotEmpty(slots);

        var second = slots.FirstOrDefault(one => one.Endpoint == Below.Role(1));
        var first = slots.FirstOrDefault(one => one.Endpoint == Below.Role(0));

        Assert.True(second is not null,
            "the walk never reached the inverse's second slot, so the fact is in "
            + "the graph and cannot be spent");

        Assert.True(first is null || second.Score > first.Score,
            $"the walk reaches the inverse's FIRST slot at least as strongly as its "
            + "second, so it has learnt the relation and not the binding");
    }

    /// <summary>Which slot a relation code is, for a readable failure.</summary>
    private static string Name(Code code) =>
        code == Above.Code ? "north-of"
        : code == Below.Code ? "south-of"
        : code == Above.Role(0) ? "north-of/0"
        : code == Above.Role(1) ? "north-of/1"
        : code == Below.Role(0) ? "south-of/0"
        : code == Below.Role(1) ? "south-of/1"
        : code.ToString();
}
