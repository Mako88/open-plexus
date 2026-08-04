using OpenPlexus.Codes;
using OpenPlexus.Graph;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What a count between two codes cannot say about a RELATION — <b>step 8's
/// variable binding, written down as a scoreboard before anything is built against
/// it.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE ROW CAN NAME A RELATION AND CANNOT HOLD A FACT ABOUT ONE.</b> Since
/// <see cref="Kind"/> stopped being an enum a front end can mint <i>north-of</i>
/// without a recompile, and every entry is <c>(partner, relation) → count</c>. But
/// the count is still about the PAIR. There is nowhere to put <i>north-of is the
/// inverse of south-of</i>, or <i>north-of is transitive</i>, or anything else that
/// is true of the relation whatever its arguments.
/// </para>
/// <para>
/// <b>SO EXPERIENCE DOES NOT TRANSFER ACROSS ARGUMENTS, AND THAT IS STRUCTURAL
/// RATHER THAN A SHORTAGE OF DATA.</b> A thousand observations of
/// <c>north-of(a, b)</c> say nothing whatever about <c>north-of(e, f)</c>. This is
/// the same wall the credit arms hit from the other side: the state count keeps
/// growing and a cell keyed on the state that earned it never covers them, because
/// nothing carries what was learnt in one state across to a similar one.
/// </para>
/// <para>
/// <b>THESE TESTS ASSERT THE GAP, WHICH MEANS THEY FAIL WHEN IT CLOSES.</b> That is
/// this project's idiom for a bar not yet cleared — step 4's arms assert that they
/// still lose to random — and it is worth more than a skipped test nobody runs. A
/// mechanism that lifts any of these should make the assertion below say so
/// instead.
/// </para>
/// </remarks>
public sealed class BindingGapTests(ITestOutputHelper output)
{
    private static Code C(ulong value) => Fixture.C(value);

    private static readonly Kind Above = Kind.Of("north-of");
    private static readonly Kind Below = Kind.Of("south-of");

    /// <summary>
    /// Two landmarks whose relation is observed many times, both ways round.
    /// </summary>
    /// <remarks>
    /// <b>THE INVERSE IS OBSERVED AND NOT INFERRED</b>, which is the point: the
    /// graph is given every pair in both relations, so nothing below is failing for
    /// want of evidence about the pairs it saw.
    /// </remarks>
    private static Bench Taught()
    {
        var bench = new Bench(Fixture.Dials(stamina: 10.0));

        foreach (var (above, below) in (( ulong Above, ulong Below)[])[(1, 2), (3, 4)])
            for (var round = 0; round < 50; round++)
            {
                bench.Node(C(above)).Note();
                bench.Node(C(below)).Note();

                bench.Node(C(above)).Observe(C(below), 1.0, Above, when: round);
                bench.Node(C(below)).Observe(C(above), 1.0, Below, when: round);
            }

        return bench;
    }

    [Fact]
    public void A_relation_learnt_on_two_pairs_says_nothing_about_a_third()
    {
        // THE GAP, IN ONE ASSERTION. `north-of` has been seen fifty times on one
        // pair and fifty on another, so whatever there is to know about the
        // relation has been shown. A new pair arrives and the graph holds exactly
        // nothing about it -- not a weak belief, NOTHING, because a count between
        // two codes cannot be about anything but those two codes.
        using var bench = Taught();

        // The new pair is observed ONCE, so the codes exist and the walk has
        // somewhere to stand. What is being asked is whether the RELATION carries.
        bench.Node(C(5)).Note();
        bench.Node(C(6)).Note();
        bench.Node(C(5)).Observe(C(6), 1.0, Above, when: 99);

        var learnt = bench.Node(C(1)).Together(C(2), Above);
        var fresh = bench.Node(C(5)).Together(C(6), Above);

        output.WriteLine($"taught pair {learnt:F0}, new pair {fresh:F0}");

        Assert.Equal(50.0, learnt);
        Assert.Equal(1.0, fresh);

        // AND THE INVERSE IS THE SHARP CASE. Every taught pair was shown both ways
        // round, so `north-of` and `south-of` are perfectly anti-symmetric in
        // everything the graph has seen. The new pair was shown one way only, and
        // the graph cannot fill in the other -- the fact that would let it is about
        // the RELATIONS and there is nowhere to keep it.
        var inverse = bench.Node(C(6)).Together(C(5), Below);

        output.WriteLine($"inverse of the new pair: {inverse:F0}");

        Assert.Equal(0.0, inverse);
    }

    [Fact]
    public void And_the_relations_themselves_are_never_partners()
    {
        // WHY THERE IS NOWHERE TO KEEP IT, STATED AS A PROPERTY RATHER THAN A
        // COMPLAINT. A relation is a KEY into a row and never a code in one, so
        // `north-of` and `south-of` -- which co-occur on every single observation
        // above -- have no cell between them and no marginal of their own. Nothing
        // in this design has ever counted a relation.
        using var bench = Taught();

        // Every code that has a row here is a landmark. Not one is a relation.
        var partners = bench.Node(C(1)).Partners();

        output.WriteLine(
            $"partners of the first landmark: {string.Join(",", partners.Select(one => one.Value))}");

        Assert.All(partners, one => Assert.True(one.Value <= 6,
            "a relation has appeared as a partner, so relations are now codes and "
            + "the gap this file records has begun to close"));
    }

    [Fact]
    public void What_would_lift_it_has_to_survive_the_fifth_thing_argument()
    {
        // NOT A MEASUREMENT — A CONSTRAINT ON THE ANSWER, kept here because the
        // obvious fixes all fail it and the next person to reach for one should
        // meet that first.
        //
        // THE FRONT END IS ALREADY HANDED FOUR THINGS and the plan's standing rule
        // is that a fifth needs an argument against the other four rather than only
        // for itself. `Occasion.Groups` says which codes belong to which object;
        // a role channel saying which SLOT each filler occupies is the natural
        // fifth, and it is the `Groups` trick a fifth time.
        //
        // AND THE TRAP IS HANDING OVER THE ANSWER. Every one of the four tests
        // whether the graph can USE a fact, never whether it can DISCOVER it -- so
        // a role channel is legitimate and a channel that says "south-of is
        // north-of reversed" is not, because that IS the fact in question. The line
        // is between telling the graph what it is looking at and telling it what to
        // conclude.
        //
        // THE OTHER ROUTE IS MAKING A RELATION A CODE, so `north-of` and `south-of`
        // can be partners in an ordinary row and a fact about relations is an
        // ordinary count. That needs no new channel at all and `Kind.Of` already
        // derives the stable identity a code would need. What it does NOT solve on
        // its own is applying the learnt inverse to particular arguments, which is
        // the binding half and the reason this is step 8 rather than an afternoon.
        Assert.NotEqual(Above, Below);

        // The identity a relation-as-code would be built from already exists and is
        // already agreed by every machine without asking one.
        Assert.Equal(Above, Kind.Of("north-of"));
    }
}
