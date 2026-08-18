using OpenPlexus.Machines;
using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Rung five: the only operator here that goes up.
/// </summary>
/// <remarks>
/// <b>Everything else narrows.</b> Genesis mints one-code claims, repair adds
/// conditions, subsumption and culling remove — so without this the machine can be
/// arbitrarily accurate and hold no concept at all.
/// </remarks>
public sealed class AbstractingTests(ITestOutputHelper output)
{
    private static Code Of(ulong value) => new(1, value);

    private static Code Says(ulong value) => new(2, value);

    private static HashSet<Code> Moment(params ulong[] codes) => [.. codes.Select(Of)];

    /// <summary>A commitment with enough experience to be allowed an opinion.</summary>
    private static Commitment Seasoned(ulong expects, params ulong[] scope)
    {
        var one = new Commitment([.. scope.Select(Of)], Says(expects));

        var moment = new HashSet<Code>(scope.Select(Of));

        for (var settle = 0; settle < 40; settle++) one.Settle(Verdict.Hit, moment, 0.1);

        return one;
    }

    // ---- what a name is ----------------------------------------------------

    [Fact]
    public void A_name_comes_from_its_members_and_from_nothing_else()
    {
        // Two nodes that notice the same redundancy must mint the same code without
        // speaking, or a name means two things in two places and the whole point of
        // having one is gone.
        Assert.Equal(
            Naming.Name([Of(1), Of(2)]),
            Naming.Name([Of(2), Of(1)]));

        Assert.NotEqual(Naming.Name([Of(1), Of(2)]), Naming.Name([Of(1), Of(3)]));
        Assert.Equal(Naming.Meant, Naming.Name([Of(1), Of(2)]).Modality);

        // A name for fewer than two codes says nothing, and minting one would be a
        // rename dressed as an abstraction.
        Assert.Throws<ArgumentException>(() => Naming.Name([Of(1)]));
        Assert.Throws<ArgumentException>(() => Naming.Name([Of(1), Of(1)]));
    }

    [Fact]
    public void A_name_is_reached_by_inference_and_never_written_as_a_partner()
    {
        // NOTHING EMITS A MINTED CODE. `csharp` broke two controls learning that
        // letting a name join the occasion it completes makes its only partner its
        // own last member -- so a moment GAINS the name when its members are there.
        var names = new Naming();
        var name = names.Mint([Of(1), Of(2)]);

        Assert.Contains(name, names.Fold(Moment(1, 2, 3)));
        Assert.DoesNotContain(name, names.Fold(Moment(1, 3)));

        // And the fold runs to a fixed point, which is the whole of the
        // bootstrapping: a name reached this round can complete a larger one in the
        // same moment, so a second level exists rather than being declared.
        var above = names.Mint([name, Of(3)]);

        Assert.Contains(above, names.Fold(Moment(1, 2, 3)));
        Assert.DoesNotContain(above, names.Fold(Moment(1, 2)));
    }

    [Fact]
    public void Unfolding_spells_a_name_back_out_however_deep_it_goes()
    {
        // What a soundness check has to be asked in. A world knows nothing about
        // minted codes, so a rewrite that changed what a commitment CLAIMS shows up
        // here as a rule that stopped being true.
        var names = new Naming();

        var pair = names.Mint([Of(1), Of(2)]);
        var above = names.Mint([pair, Of(3)]);

        Assert.Equal<IEnumerable<Code>>([Of(1), Of(2)], names.Unfold([pair]));
        Assert.Equal<IEnumerable<Code>>([Of(1), Of(2), Of(3)], names.Unfold([above]));
        Assert.Equal<IEnumerable<Code>>([Of(1), Of(2), Of(4)], names.Unfold([pair, Of(4)]));

        // A code nobody named is itself.
        Assert.Equal<IEnumerable<Code>>([Of(7)], names.Unfold([Of(7)]));
        Assert.True(names.Knows(pair));
        Assert.False(names.Knows(Of(7)));
    }

    // ---- what earns one ----------------------------------------------------

    [Fact]
    public void A_pair_that_keeps_recurring_earns_a_name()
    {
        var dials = new CommittingSettings();

        var held = new List<Commitment>
        {
            Seasoned(1, 1, 2, 5),
            Seasoned(1, 1, 2, 6),
            Seasoned(0, 1, 2, 7),
            Seasoned(0, 1, 2, 8),
        };

        Assert.Equal<IEnumerable<Code>>([Of(1), Of(2)], Abstracting.Shared(held, dials)!.Value);
    }

    [Fact]
    public void A_pair_in_two_scopes_does_not_repay_the_cost_of_naming_it()
    {
        // THE DESCRIPTION-LENGTH BAR. Saying what a pair means costs two entries and
        // saves one per scope holding it, so below three scopes a name is a longer
        // way of saying the same thing.
        var dials = new CommittingSettings();

        var held = new List<Commitment>
        {
            Seasoned(1, 1, 2, 5),
            Seasoned(1, 1, 2, 6),
            Seasoned(0, 3, 4, 7),
        };

        Assert.Null(Abstracting.Shared(held, dials));
    }

    [Fact]
    public void Nothing_is_named_where_the_scopes_are_independent()
    {
        // THE OTHER BAR, AND `Paying`'S ACTUAL FINDING. Description length alone
        // minted 715 names on a pure-noise control, because a shorter description of
        // noise is still shorter. So a pair has to beat what independent scopes would
        // have thrown up anyway.
        var dials = new CommittingSettings();

        var noise = new Random(4);

        var held = Enumerable.Range(0, 120)
            .Select(_ => Seasoned(
                1,
                (ulong)noise.Next(30),
                (ulong)noise.Next(30) + 30,
                (ulong)noise.Next(30) + 60))
            .ToList();

        Assert.Null(Abstracting.Shared(held, dials));
    }

    [Fact]
    public void Only_experienced_commitments_get_to_propose()
    {
        // A scope minted this round is not evidence that anything recurs -- it is
        // evidence that covering ran.
        var dials = new CommittingSettings();

        var green = new List<Commitment>
        {
            new([Of(1), Of(2), Of(5)], Says(1)),
            new([Of(1), Of(2), Of(6)], Says(1)),
            new([Of(1), Of(2), Of(7)], Says(1)),
        };

        Assert.Null(Abstracting.Shared(green, dials));
    }

    // ---- what a rewrite does and does not change ---------------------------

    [Fact]
    public void A_rewrite_says_the_same_thing_shorter_and_keeps_its_record()
    {
        var held = new Population(new CommittingSettings(), seed: 1);

        foreach (var one in (Commitment[])
            [Seasoned(1, 1, 2, 5), Seasoned(1, 1, 2, 6), Seasoned(0, 1, 2, 7)])
            held.Add(one);

        var before = held.All.Single(one => one.Scope.Length == 3 && one.Expects == Says(0));

        Assert.Equal(3, held.Abstract());
        Assert.Equal(1, held.Names.Count);

        var after = held.All.Single(one => one.Expects == Says(0));

        // Shorter, and saying the same thing -- which `Unfold` is what checks.
        Assert.Equal(2, after.Scope.Length);
        Assert.Equal<IEnumerable<Code>>(before.Scope, held.Names.Unfold(after.Scope));

        // AND IT KEEPS ITS RECORD, because a mechanism whose reward is losing its
        // evidence is one nobody would run.
        Assert.Equal(before.Hits, after.Hits);
        Assert.Equal(before.Seen, after.Seen);
        Assert.Equal(before.Accuracy, after.Accuracy);

        // It still fires on exactly the moments it did, once the moment is folded.
        Assert.Single(held.Firing(held.Moment(Moment(1, 2, 7))));
        Assert.Empty(held.Firing(held.Moment(Moment(1, 7))));
    }

    [Fact]
    public void A_scope_still_speaking_the_members_stands_in_no_relation_to_one_that_took_the_name()
    {
        // WHAT REFUTED `Chunk`'S rule when it was ported here, and a live defect
        // underneath it. On the walk, a name covering the whole MOMENT destroyed the
        // pairing it was meant to compress and `Senses` fell 0.8621 to 0.4138, so the
        // rule there was that a fold must leave something standing. The scope version
        // of that rule -- refuse the rewrite where the name would cover the whole
        // scope -- was built, measured and deleted, because the two things are not the
        // same shape: a scope rewrite destroys no evidence at all. Counting happened
        // first, `Unfold` recovers the claim, and the commitment fires on exactly the
        // moments it did.
        //
        // What it does instead is split the population into two vocabularies.
        // `Commitment.Narrows` is a subset test over codes and does not unfold, so a
        // commitment left holding `{A,B}` and its own children holding `{name,C}` have
        // no code in common and stand in NO relation -- subsumption never looks at
        // them, and no instrument here would say so. Eight seeds on the eleven-bit
        // multiplexer: it fired on six, `sound` moved +4.8 against a standard error of
        // 9.9 which is nothing, and `unsound` rose on all five seeds that moved it at
        // all, +1 to +41, with `resident` up on five of six. More rules held and no
        // more of them true.
        //
        // And the rewrite already has one path that leaves a scope behind -- the
        // identity collision in `Population.Abstract`, which is rare and is this same
        // split. That is why this is pinned here rather than left with the arm.
        var names = new Naming();
        var name = names.Mint([Of(1), Of(2)]);

        var members = new Commitment([Of(1), Of(2)], Says(1));
        var took = new Commitment([name, Of(6)], Says(1));

        Assert.False(took.Narrows(members),
            "these are comparable after all, so the split this pins has been closed");

        // And unfolding is what would restore it — the revival condition, and the same
        // grain `Population.Under` already reaches for to read a category's entailment.
        var spelled = names.Unfold(took.Scope);

        Assert.All(names.Unfold(members.Scope), code => Assert.Contains(code, spelled));
        Assert.True(spelled.Length > members.Scope.Length);
    }

    [Fact]
    public void Nothing_is_named_twice()
    {
        var held = new Population(new CommittingSettings(), seed: 1);

        foreach (var one in (Commitment[])
            [Seasoned(1, 1, 2, 5), Seasoned(1, 1, 2, 6), Seasoned(0, 1, 2, 7)])
            held.Add(one);

        Assert.Equal(3, held.Abstract());

        // The pair is gone from every scope, so there is nothing left to propose and
        // the second pass has to find nothing rather than mint a synonym.
        Assert.Equal(0, held.Abstract());
        Assert.Equal(1, held.Names.Count);
    }

    // ---- and whether it happens on a world ---------------------------------

    [Fact]
    public void Six_bits_has_nothing_this_rung_can_name_and_eleven_does()
    {
        // The plan said this world had an answer key for abstraction -- that the
        // address bits recur across the rules, so an address is the thing to notice.
        // IT IS WRONG, and building the rung is what showed it.
        //
        // A code here carries a position and a value together, so the only thing a
        // pair can name is one address VALUE: *the address is zero-zero*. That
        // appears in exactly two rules, because each address selects one data bit
        // and that bit has two values. Two is below the description-length bar, and
        // rightly: a name costing two entries to define cannot repay itself across
        // two uses.
        //
        // And the structure the world actually has is over positions rather than
        // values -- *these bits are the address, whatever they say* -- which is a
        // variable, and a variable is rung FOUR. Naming sets of codes cannot reach
        // it at all.
        //
        // Eleven bits names anyway, and not the thing the plan expected. Its scopes
        // are wider and its correct rules more numerous, so sub-scopes DO recur
        // across three and more of them -- what repays is whatever the population
        // happens to share, which is a fact about what was learnt rather than about
        // what the world is made of. Rung five is not idle here; it is answering a
        // different question from the one the plan asked it.
        var six = new MultiplexerRun(
            new MultiplexerSettings { Address = 2 }, new Brain(new CommittingSettings(), 1), seed: 1).Run(30000);

        var eleven = new MultiplexerRun(
            new MultiplexerSettings { Address = 3 }, new Brain(new CommittingSettings(), 1), seed: 1).Run(30000);

        foreach (var learned in (Learned[])[six, eleven])
            output.WriteLine(
                $"named={learned.Named}/{learned.Eligible} spoke={learned.Speaking:F2} "
                + $"stacked={learned.Stacked} sound={learned.Sound} "
                + $"unsound={learned.Unsound} resident={learned.Resident} "
                + $"recent={learned.Recent:F3} found={learned.Found}/{learned.Truths}");

        Assert.Equal(0, six.Named);
        Assert.True(eleven.Named > 0, "eleven bits found nothing worth a name either");

        // And naming costs nothing its truth. Every rewritten scope is checked
        // against the world spelled back out, so a rewrite that changed what a
        // commitment claims would land here.
        Assert.True(six.Sound > 0, "the world stopped being learnable at all");
        Assert.True(eleven.Sound > 50, $"only {eleven.Sound} sound where names were minted");
    }

    [Fact]
    public void And_it_names_the_moment_a_sub_scope_actually_repays()
    {
        // The mechanism is not what is missing, which is worth asserting beside the
        // refusal above so the two are not confused. Given a population whose scopes
        // really do share a pair, the name arrives, the scopes shorten, and what each
        // one claims is unchanged.
        var held = new Population(new CommittingSettings(), seed: 1);

        foreach (var tail in (ulong[])[5, 6, 7, 8, 9])
            held.Add(Seasoned(tail % 2, 1, 2, tail));

        var before = held.All.Select(one => one.Scope.Length).Sum();

        Assert.Equal(5, held.Abstract());

        var after = held.All.Select(one => one.Scope.Length).Sum();

        output.WriteLine($"scope entries {before} -> {after} plus a name of two");

        // Shorter by more than the name costs to define, which is the bar it cleared.
        Assert.True(after + 2 < before, $"{after} plus two is no better than {before}");

        // And every one still claims what it claimed: two codes now, three when
        // spelled back out.
        Assert.All(held.All, one => Assert.Equal(2, one.Scope.Length));
        Assert.All(held.All, one => Assert.Equal(3, held.Names.Unfold(one.Scope).Length));
    }
}
