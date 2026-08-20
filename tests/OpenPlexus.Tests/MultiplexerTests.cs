using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The first world, and the one step one is judged on.
/// </summary>
/// <remarks>
/// <b>Several cues arrive together and only some carry the outcome.</b> What is
/// asserted here is not that the world runs — it is that the world and its ANSWER
/// KEY agree, because the key is what makes the score something memorising cannot
/// reach, and a key that drifted from the world would quietly become the thing being
/// measured.
/// </remarks>
public sealed class MultiplexerTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private static MultiplexerSettings World(
        int address = 2, double noise = 0.0, int flip = 0) => new()
    {
        Address = address, Noise = noise, Switch = flip,
    };

    // ---- what the world is, asserted rather than described -----------------

    [Fact]
    public void Two_address_bits_is_six_wide_and_three_is_eleven()
    {
        Assert.Equal(6, new Multiplexer(World(address: 2), seed: 1).Bits);
        Assert.Equal(11, new Multiplexer(World(address: 3), seed: 1).Bits);
        Assert.Equal(37, new Multiplexer(World(address: 5), seed: 1).Bits);
    }

    [Fact]
    public void One_code_per_bit_and_it_carries_the_value()
    {
        // A code standing for a position alone would be true in every round and
        // would separate nothing, so the value has to be in the identity.
        var world = new Multiplexer(World(), seed: 1);

        for (var round = 0; round < 50; round++)
        {
            var cues = world.Next().Cues;

            Assert.Equal(world.Bits, cues.Length);
            Assert.Equal(cues.Length, cues.Distinct().Count());
            Assert.All(cues, code => Assert.Equal(Multiplexer.Bit, code.Modality));
        }

        Assert.NotEqual(Multiplexer.Of(3, 0), Multiplexer.Of(3, 1));
        Assert.NotEqual(Multiplexer.Of(3, 1), Multiplexer.Of(4, 1));
    }

    [Fact]
    public void A_round_and_a_rule_compare_by_what_they_say()
    {
        // The companion, and without it half this file passes by being unable to
        // fail. A synthesised record equality compares the array behind an
        // ImmutableArray by IDENTITY, so two rounds from the same draw read as
        // different and two different rules read as different for the wrong reason.
        // Every determinism and switching assertion below rests on this.
        var one = new Multiplexer(World(), seed: 21).Next();
        var two = new Multiplexer(World(), seed: 21).Next();

        Assert.Equal(one, two);
        Assert.Equal(one.GetHashCode(), two.GetHashCode());
        Assert.NotEqual(one, new Multiplexer(World(), seed: 22).Next());

        var truths = new Multiplexer(World(), seed: 21).Truths();

        Assert.Equal(truths[0], new Multiplexer(World(), seed: 99).Truths()[0]);
        Assert.NotEqual(truths[0], truths[1]);

        // And a scope is a set, so the same codes in any order are the same rule.
        Assert.Equal(
            truths[0],
            truths[0] with { Scope = [.. truths[0].Scope.Reverse().Order()] });

        // The trap also lives one level up, and it bit this file before it was
        // written down: an ImmutableArray OF rules compares by the identity of the
        // array too, so `Assert.Equal` on two keys fails whatever they hold and
        // `Assert.NotEqual` passes whatever they hold. Every key comparison below
        // goes through a list for that reason, and this is why.
        Assert.False(truths.Equals(new Multiplexer(World(), seed: 99).Truths()));
        Assert.Equal(truths.ToList(), new Multiplexer(World(), seed: 99).Truths().ToList());
    }

    // ---- the answer key, which is what the score rests on -------------------

    [Fact]
    public void The_key_holds_two_rules_per_address_and_pins_only_what_matters()
    {
        // The count is derived, not quoted: one rule per (address value, value of
        // the bit that address selects). Eight at six bits, sixteen at eleven.
        foreach (var address in new[] { 2, 3 })
        {
            var world = new Multiplexer(World(address), seed: 1);
            var truths = world.Truths();

            Assert.Equal(world.Data * 2, truths.Length);

            // Every scope is the address bits plus one data bit. A scope that pinned
            // a second data bit would still be correct and would no longer be the
            // rule the learner has to find, so the key would be scoring the wrong
            // target.
            Assert.All(truths, truth => Assert.Equal(address + 1, truth.Scope.Length));
            Assert.All(truths, truth => Assert.Equal(Multiplexer.Said, truth.Expects.Modality));

            Assert.Equal(truths.Length, truths.Select(truth => truth.Scope).Distinct().Count());
        }
    }

    [Fact]
    public void Exactly_one_rule_in_the_key_fires_and_it_says_what_the_world_said()
    {
        // The load-bearing test in this file. It asserts the world against the key
        // and the key against the world in one statement, so neither can drift into
        // agreeing with itself. If more than one rule fired, the key would be
        // ambiguous; if none did, it would be incomplete; if the one that fired
        // disagreed, the score would be measuring the key's bug.
        foreach (var address in new[] { 2, 3 })
        {
            var world = new Multiplexer(World(address), seed: 7);

            for (var round = 0; round < 500; round++)
            {
                var shown = world.Next();
                var cues = shown.Cues.ToHashSet();

                var fired = world.Truths().Where(truth => truth.Scope.All(cues.Contains)).ToList();

                Assert.Single(fired);
                Assert.Equal(shown.Answer, fired[0].Expects);
            }
        }
    }

    [Fact]
    public void Both_answers_come_up_and_a_blind_guess_scores_a_half()
    {
        // A world that said one thing nine times in ten would let a learner that
        // never fires beat chance, and the bar would be measuring the imbalance.
        var world = new Multiplexer(World(), seed: 3);

        var ones = 0;
        for (var round = 0; round < 2000; round++)
            if (world.Next().Answer == Multiplexer.Says(1)) ones++;

        Assert.InRange(ones / 2000.0, 0.45, 0.55);
        Assert.Equal(0.5, Multiplexer.Chance);
    }

    // ---- the two arms this world exists to carry ---------------------------

    [Fact]
    public void Noise_flips_what_is_emitted_and_never_what_is_true()
    {
        // The repair gate cannot be tested on a clean world, because there every
        // failure really is explained by some absent condition. So the world has to
        // be able to lie -- and it has to keep saying what the truth was, or a run
        // cannot report how much of its own failure it was handed.
        var world = new Multiplexer(World(noise: 0.2), seed: 5);

        var lied = 0;

        for (var round = 0; round < 2000; round++)
        {
            var shown = world.Next();
            var cues = shown.Cues.ToHashSet();

            var fired = world.Truths().Single(truth => truth.Scope.All(cues.Contains));

            // The key tracks the FUNCTION, so it agrees with `Answer` even in the
            // rounds where `Outcome` was flipped.
            Assert.Equal(shown.Answer, fired.Expects);

            if (shown.Outcome != shown.Answer) lied++;
        }

        Assert.InRange(lied / 2000.0, 0.17, 0.23);
    }

    [Fact]
    public void Switching_moves_the_target_and_the_key_moves_with_it()
    {
        // FORK 27. Monotone counters converge and cannot track, so the local
        // decaying estimate is either earning its keep on a world whose target moves
        // or earning it nowhere. Scoring a switched run against the key it started
        // with would measure the switch rather than the recovery.
        var world = new Multiplexer(World(flip: 100), seed: 9);

        var before = world.Truths().ToList();
        for (var round = 0; round < 100; round++) world.Next();

        // The mapping moves before the round it affects, so the flip has happened by
        // the time the hundred-and-first round is drawn.
        world.Next();
        var after = world.Truths().ToList();

        Assert.NotEqual(before, after);
        Assert.Equal(before.Count, after.Count);

        for (var round = 0; round < 300; round++)
        {
            var shown = world.Next();
            var cues = shown.Cues.ToHashSet();

            Assert.Equal(
                shown.Answer,
                world.Truths().Single(truth => truth.Scope.All(cues.Contains)).Expects);
        }
    }

    [Fact]
    public void A_run_that_never_switches_is_the_published_world()
    {
        // The first mapping is the identity whatever `Switch` SAYS, so a switching
        // run and a standard one are the same world until the first flip -- which is
        // what keeps the numbers comparable to anything published.
        var standard = new Multiplexer(World(), seed: 11);
        var switching = new Multiplexer(World(flip: 1000), seed: 11);

        Assert.Equal(standard.Truths().ToList(), switching.Truths().ToList());

        for (var round = 0; round < 200; round++)
            Assert.Equal(standard.Next(), switching.Next());
    }

    [Fact]
    public void A_fixed_seed_reproduces_a_run_exactly()
    {
        // Fork 12, which this project has already reopened once. A world that did
        // not reproduce would make every arm incomparable with every other, and the
        // failure would look like a mechanism.
        var one = new Multiplexer(World(noise: 0.1, flip: 50), seed: 13);
        var two = new Multiplexer(World(noise: 0.1, flip: 50), seed: 13);

        for (var round = 0; round < 500; round++) Assert.Equal(one.Next(), two.Next());

        Assert.Equal(one.Truths().ToList(), two.Truths().ToList());
        Assert.NotEqual(
            new Multiplexer(World(), seed: 13).Next(),
            new Multiplexer(World(), seed: 14).Next());
    }

    /// <summary>
    /// <b>The skewed world skews its outcomes and changes nothing else.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The true rule set is asserted identical, which is the half that makes it a
    /// control.</b> A world that skewed its outcomes by changing what is true of it would
    /// move two things at once, and every comparison against it would be unreadable — this
    /// repo's own trap about a setting that decides two independent things while being named
    /// for one. Only how often the answer is one may differ.
    /// </para>
    /// <para>
    /// <b>It arrived here with the vote arm it was built for</b>, and outlived it. The
    /// base-rate divisor is deleted; the world property it needed is a fact about the world
    /// and every skewed grid on this bench still rests on it.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(0.8, 3.0)]
    public void A_skewed_multiplexer_moves_its_outcomes_and_leaves_its_rules_alone(
        double skew, double least)
    {
        var even = new MultiplexerSettings { Address = 2 };
        var slanted = new MultiplexerSettings { Address = 2, Skew = skew };

        var world = new Multiplexer(slanted, seed: 1);

        // Counted per distinct answer rather than against a nominated one, so this says
        // nothing about which code means one and cannot be wrong about it.
        var seen = new Dictionary<Code, int>();
        const int Draws = 20_000;

        for (var draw = 0; draw < Draws; draw++)
        {
            var answer = world.Next().Answer;
            seen[answer] = seen.GetValueOrDefault(answer) + 1;
        }

        var ratio = seen.Values.Max() / (double)seen.Values.Min();
        var share = seen.Values.Max() / (double)Draws;

        output.WriteLine($"skew {skew:F2} | the commoner answer is {share:P1} of the time "
            + $"| outcome ratio {ratio:F2}x");

        Assert.True(ratio >= least,
            $"a skew of {skew:F2} produced an outcome ratio of {ratio:F2}x, under the "
            + $"{least:F2}x this setting exists to reach");

        // The rules are the same rules, which is what keeps soundness comparable across the
        // two arms. `Truths` reads the mapping and the mapping is drawn from the seed, so two
        // worlds on one seed must agree exactly whatever their bits do.
        //
        // Compared by content and not by the record, because `Truth` holds an
        // `ImmutableArray` and that type's equality is the identity of the underlying array
        // -- so two separately built keys with identical scopes are never equal and the
        // assertion would fail on a world it had no complaint about.
        static List<string> Shape(MultiplexerSettings settings) =>
            [.. new Multiplexer(settings, seed: 1).Truths()
                .Select(one => $"{string.Join("+", one.Scope.Order())}->{one.Expects}")
                .Order()];

        Assert.Equal(Shape(even), Shape(slanted));
    }
}
