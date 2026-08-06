using OpenPlexus.Codes;
using OpenPlexus.Learning;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// A name for a SEQUENCE that keeps recurring — <c>Chunk</c>'s sibling.
/// </summary>
/// <remarks>
/// <b>THE CANDIDATES ARE THE DIFFERENCE AND NOT THE HASH.</b> A chunk merges pairs
/// within ONE moment; a macro merges CONSECUTIVE pairs across several. Naming in
/// order is one line — these are about whether the right things get named.
/// </remarks>
public sealed class MacroTests(ITestOutputHelper output)
{
    private static Code C(ulong value) => Fixture.C(value);

    /// <summary>Feeds a sequence, and reports the last macro each pass completed.</summary>
    private static List<Code> Show(Macro macro, IReadOnlyList<Code> stream, int times)
    {
        var made = new List<Code>();

        for (var pass = 0; pass < times; pass++)
        {
            macro.Broke();
            foreach (var code in stream)
                if (macro.Notice(code) is { } one) made.Add(one);
        }

        return made;
    }

    [Fact]
    public void An_order_that_keeps_recurring_earns_a_name()
    {
        var macro = new Macro();

        // A, B, then filler drawn from a wide alphabet so the pair is not merely
        // two common things meeting.
        var stream = new List<Code> { C(1), C(2) };
        for (var noise = 0; noise < 6; noise++) stream.Add(C((ulong)(100 + noise)));

        var made = Show(macro, stream, times: 30);

        output.WriteLine($"{macro} applied={macro.Applied} made={made.Count}");

        Assert.True(macro.Coined > 0, "a sequence shown thirty times earned no name");
        Assert.NotEmpty(made);
    }

    [Fact]
    public void And_the_ORDER_is_what_it_names()
    {
        // `a then b` AND `b then a` ARE DIFFERENT FACTS, where `{a, b}` is one fact
        // however it is written. This is the whole of what separates this from
        // `Chunk`, which sorts before folding.
        var forward = new Macro();
        var backward = new Macro();

        Show(forward, [C(1), C(2), C(50)], times: 30);
        Show(backward, [C(2), C(1), C(50)], times: 30);

        var one = Show(forward, [C(1), C(2), C(50)], times: 1);
        var other = Show(backward, [C(2), C(1), C(50)], times: 1);

        Assert.NotEmpty(one);
        Assert.NotEmpty(other);
        Assert.NotEqual(one[0], other[0]);
    }

    [Fact]
    public void A_longer_sequence_is_reached_by_REPEATED_pairing()
    {
        // THE WHOLE REASON A TWO-SYMBOL WINDOW IS ENOUGH. `a→b` mints M₁, and then
        // `M₁→c` mints M₂ covering all three -- so nothing has to choose how long a
        // macro may be, exactly as byte-pair encoding reaches long tokens.
        var macro = new Macro();

        var stream = new List<Code> { C(1), C(2), C(3) };
        for (var noise = 0; noise < 6; noise++) stream.Add(C((ulong)(100 + noise)));

        Show(macro, stream, times: 60);

        var made = Show(macro, stream, times: 1);

        output.WriteLine($"{macro} made={made.Count} "
            + $"widest={made.Select(one => macro.Members(one).Length).DefaultIfEmpty(0).Max()}");

        Assert.Contains(made, one => macro.Members(one).Length >= 3);

        // AND THE MEMBERS ARE IN ORDER, which is what a caller unpacking one needs.
        var longest = made.OrderByDescending(one => macro.Members(one).Length).First();
        Assert.Equal([C(1), C(2), C(3)], macro.Members(longest).Take(3));
    }

    [Fact]
    public void Pure_noise_earns_far_fewer_names_than_structure()
    {
        // THE CONTROL THAT COST `Chunk` A WHOLE MECHANISM TO LEARN. Description
        // length alone is satisfied by any pair frequent enough to be worth a
        // symbol -- including one frequent only because both halves are -- and on
        // `Motif` the pure-noise control minted 715 names against the structured
        // world's 245. A detector finding more structure in noise has found none.
        var structured = new Macro();
        var noisy = new Macro();

        var order = new Random(20260806);
        var alphabet = Enumerable.Range(1, 8).Select(one => C((ulong)one)).ToArray();

        for (var pass = 0; pass < 200; pass++)
        {
            structured.Notice(C(1));
            structured.Notice(C(2));
            structured.Notice(alphabet[order.Next(alphabet.Length)]);

            for (var at = 0; at < 3; at++)
                noisy.Notice(alphabet[order.Next(alphabet.Length)]);
        }

        output.WriteLine($"structured={structured} noisy={noisy}");

        Assert.True(structured.Coined > noisy.Coined,
            $"noise minted {noisy.Coined} names against structure's "
            + $"{structured.Coined} -- the null model is not doing its job");
    }

    [Fact]
    public void A_one_off_sequence_earns_nothing()
    {
        // MINIMUM DESCRIPTION LENGTH: a pair seen once saves one symbol once and
        // costs two to define, so it has not paid.
        var macro = new Macro();

        foreach (var code in (Code[])[C(1), C(2), C(3), C(4)]) macro.Notice(code);

        Assert.Equal(0, macro.Coined);
    }

    [Fact]
    public void A_break_in_the_stream_drops_what_is_in_hand_and_not_what_was_learnt()
    {
        // A GAP IS NOT EVIDENCE AGAINST A SEQUENCE -- counts only ever rise. What
        // it must prevent is minting ACROSS a discontinuity nobody claimed was
        // continuous.
        var macro = new Macro();

        var stream = new List<Code> { C(1), C(2) };
        for (var noise = 0; noise < 6; noise++) stream.Add(C((ulong)(100 + noise)));

        Show(macro, stream, times: 40);
        var learnt = macro.Coined;

        macro.Broke();

        Assert.Equal(learnt, macro.Coined);

        // AND NOTHING IS IN HAND, so the very next code cannot pair with whatever
        // preceded the break.
        Assert.Null(macro.Notice(C(2)));
    }

    [Fact]
    public void Two_detectors_mint_the_same_name_for_the_same_sequence()
    {
        // THE RED-BALL PROPERTY. Derived by the arithmetic every code here agrees
        // by, so two machines that independently notice the same order agree with
        // nothing to ask.
        var stream = new List<Code> { C(1), C(2) };
        for (var noise = 0; noise < 6; noise++) stream.Add(C((ulong)(100 + noise)));

        var one = new Macro();
        var other = new Macro();

        Show(one, stream, times: 30);
        Show(other, stream, times: 30);

        var mine = Show(one, stream, times: 1);
        var theirs = Show(other, stream, times: 1);

        Assert.NotEmpty(mine);
        Assert.Equal(mine, theirs);
    }

    [Fact]
    public void A_macro_is_never_mistaken_for_a_chunk_or_for_a_sense()
    {
        // A SET AND AN ORDER ARE DIFFERENT CLAIMS, so a walk narrowed to one must
        // not reach the other -- the argument that gives `Chunk` its own modality.
        var macro = new Macro();

        var stream = new List<Code> { C(1), C(2) };
        for (var noise = 0; noise < 6; noise++) stream.Add(C((ulong)(100 + noise)));

        var made = Show(macro, stream, times: 40);

        Assert.NotEmpty(made);
        Assert.All(made, one => Assert.Equal(Macro.Made, one.Modality));
        Assert.NotEqual(Chunk.Minted, Macro.Made);
    }

    [Fact]
    public void Something_that_was_never_minted_covers_nothing()
    {
        // `Members` HAS TO SAY "NOT MINE" RATHER THAN GUESS, or a caller unpacking
        // an ordinary code would be handed somebody else's sequence.
        var macro = new Macro();

        Assert.Empty(macro.Members(C(1)));
        Assert.Empty(macro.Members(new Code(Macro.Made, 999)));
    }
}
