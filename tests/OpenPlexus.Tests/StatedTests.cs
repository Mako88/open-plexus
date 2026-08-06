using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;

namespace OpenPlexus.Tests;

/// <summary>
/// A relation instance as a NODE, with a role-typed arm to each thing it relates.
/// </summary>
/// <remarks>
/// <b>WHAT AN EDGE CANNOT DO.</b> <see cref="Edge"/> is a partner and a
/// <see cref="Kind"/>, so a relation here is binary, closed-vocabulary and has no
/// address. These assert the three things a node buys back: more than two
/// arguments, an order among them, and a code a walk can actually arrive at.
/// </remarks>
public sealed class StatedTests
{
    private static readonly Kind Gave = Kind.Of("gave");

    private static Code C(ulong value) => Fixture.C(value);

    [Fact]
    public void A_relation_of_THREE_arguments_exists_at_all()
    {
        // THE GAP, IN ONE LINE. There is no way to write `gave(alice, bob, book)`
        // as an edge -- an edge relates a pair. A node with three arms does.
        var moments = Stated.Star(Gave, [C(1), C(2), C(3)], at: 7);

        // ONE ARM EACH, PLUS THE TYPE-LEVEL MOMENT.
        Assert.Equal(4, moments.Length);

        foreach (var arm in moments.Take(3))
        {
            Assert.Equal(2, arm.Codes.Count);
            Assert.Equal(Gave, arm.Relating);
            Assert.NotNull(arm.Filling);
        }
    }

    [Fact]
    public void Each_arm_says_WHICH_slot_its_filler_occupies()
    {
        // THE ROLE CHANNEL IS THE WHOLE POINT -- see Kind.Role. A count against
        // `gave/1` names no argument, so it accumulates across every pair that
        // ever fills that slot and applies to pairs never seen.
        var moments = Stated.Star(Gave, [C(1), C(2), C(3)], at: 7);

        for (var slot = 0; slot < 3; slot++)
            Assert.Equal(slot, moments[slot].Filling![C((ulong)slot + 1)]);
    }

    [Fact]
    public void The_arms_are_a_STAR_and_never_a_clique()
    {
        // THE FINDING THAT COST THE MOST, ASSERTED SO IT CANNOT COME BACK. An
        // occasion pairs everything in it, so one moment holding the instance and
        // every filler would also write filler-to-filler and filler-to-type --
        // and a relation co-occurring with everything it ever related is the
        // superhub `Kind.Code` warns about. Measured: it timed the walk out.
        var moments = Stated.Star(Gave, [C(1), C(2), C(3)], at: 7);

        // NO MOMENT HOLDS TWO FILLERS, which is what "star" means here.
        var fillers = new[] { C(1), C(2), C(3) };

        foreach (var moment in moments)
            Assert.True(moment.Codes.Count(fillers.Contains) <= 1,
                "a moment held two fillers, so this is a clique and the relation "
                + "is about to become a superhub");
    }

    [Fact]
    public void The_instance_is_fleeting_in_every_moment_it_appears_in()
    {
        // A LASTING INSTANCE GROWS THE TYPE'S ROW BY AN ENTRY PER STATEMENT
        // FOREVER, which is the hub this exists to avoid. The caller gets no say.
        var moments = Stated.Star(
            Gave, [C(1), C(2)], at: 7, lasting: new HashSet<Code> { C(1), C(2) });

        var instance = Stated.Instance(Gave, [C(1), C(2)], at: 7);

        foreach (var moment in moments)
            Assert.Contains(instance, moment.Passing!);
    }

    [Fact]
    public void And_a_filler_the_world_calls_lasting_is_left_to_accumulate()
    {
        // THE OTHER HALF, or the check above passes for a version that makes
        // everything fleeting and nothing can ever be learnt about a filler.
        var moments = Stated.Star(
            Gave, [C(1), C(2)], at: 7, lasting: new HashSet<Code> { C(1) });

        Assert.DoesNotContain(C(1), moments[0].Passing!);
        Assert.Contains(C(2), moments[1].Passing!);
    }

    [Fact]
    public void The_type_is_joined_so_an_occasion_can_reach_the_rule()
    {
        // AN INSTANCE IS SEEN ONCE AND ACCUMULATES NOTHING, so without this a walk
        // arriving at one learns only that this happened. The type-level cell is
        // what carries across statements.
        var moments = Stated.Star(Gave, [C(1), C(2)], at: 7);

        Assert.Contains(Gave.Code, moments[^1].Codes);
        Assert.Null(moments[^1].Relating);
    }

    [Fact]
    public void The_same_statement_mints_the_same_instance_on_any_machine()
    {
        // THE RED-BALL PROPERTY. Derived by arithmetic from the relation, the
        // fillers and the clock -- nothing counted out, nothing drawn, no table.
        Assert.Equal(
            Stated.Instance(Gave, [C(1), C(2)], at: 7),
            Stated.Instance(Gave, [C(1), C(2)], at: 7));
    }

    [Fact]
    public void And_the_ORDER_of_the_fillers_changes_it()
    {
        // `gave(alice, bob)` IS NOT `gave(bob, alice)`, which is the opposite of
        // `Chunk` -- whose members name a SET and are therefore sorted.
        Assert.NotEqual(
            Stated.Instance(Gave, [C(1), C(2)], at: 7),
            Stated.Instance(Gave, [C(2), C(1)], at: 7));
    }

    [Fact]
    public void Saying_the_same_thing_twice_is_TWO_occasions()
    {
        // OR THE INSTANCE ACCUMULATES A COUNT, and a node whose whole job is to be
        // seen once stops being one. The type-level cell is what carries repeats.
        Assert.NotEqual(
            Stated.Instance(Gave, [C(1), C(2)], at: 7),
            Stated.Instance(Gave, [C(1), C(2)], at: 8));
    }

    [Fact]
    public void A_relation_relating_nothing_is_refused()
    {
        // A node with no arms says nothing the type-level cell does not already
        // say, and it would still cost a code and a row.
        Assert.Throws<ArgumentException>(() => Stated.Star(Gave, [], at: 7));
    }

    [Fact]
    public void Two_relations_of_the_same_pair_at_one_moment_stay_apart()
    {
        // THE RELATION IS IN THE HASH, not merely the arguments -- otherwise
        // stating two different things about one pair at one instant would fold
        // onto a single occasion.
        Assert.NotEqual(
            Stated.Instance(Gave, [C(1), C(2)], at: 7),
            Stated.Instance(Kind.Of("owed"), [C(1), C(2)], at: 7));
    }
}
