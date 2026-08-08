using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The language ceiling, decided rather than measured.
/// </summary>
/// <remarks>
/// <para>
/// <b>STEP EIGHT IS <i>THE RUNG THE FAILURES DEMAND, AND NEVER THE RUNG THAT SOUNDS
/// NEXT</i>, AND NO FAILURE HAD ASKED.</b> On the multiplexer twelve one-code rules
/// cover everything held out, so the scope language is nowhere near binding there. This
/// world is built so a failure can ask, and it asks BEFORE a learner is run: the concept
/// and the language are both finite, so what a conjunction can and cannot say about the
/// MONK's problems is a thing to be enumerated rather than a thing to be discovered from
/// a disappointing score.
/// </para>
/// <para>
/// <b>AND THE TWO CONTROLS ARE THE POINT OF HAVING THREE PUZZLES.</b> A learner falling
/// short on <see cref="Puzzle.Two"/> alone says nothing — a ceiling on the language and
/// a poor learner look identical from one number, which is this project's oldest
/// complaint about its own measurements. <see cref="Puzzle.One"/> and
/// <see cref="Puzzle.Three"/> are reachable, so failing THERE means the learner.
/// </para>
/// </remarks>
public sealed class MonkTests(ITestOutputHelper output)
{
    [Fact]
    public void The_bag_is_the_published_one_and_every_instance_is_distinct()
    {
        Assert.Equal(432, Monk.Everything.Length);

        // 3 x 3 x 2 x 3 x 4 x 2, which is the published attribute set and the reason
        // every instrument on this world is exact rather than sampled.
        Assert.Equal(432, Monk.Widths.Aggregate(1, (product, one) => product * one));

        Assert.Equal(
            432,
            Monk.Everything.Select(one => string.Join(",", one)).Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// <b>THE BAR IS NOT A HALF ON THE ONE PUZZLE THAT MATTERS.</b>
    /// </summary>
    /// <remarks>
    /// <see cref="Puzzle.Two"/> is 142 positive of 432, so an arm that never says yes
    /// scores 0.6713 — and a run reporting 0.68 against an assumed chance of 0.5 would
    /// read as having learnt most of the problem while having learnt none of it. That is
    /// the fallback-as-control-arm trap with the arithmetic pre-done.
    /// </remarks>
    [Fact]
    public void What_a_silent_arm_scores_is_counted_rather_than_assumed()
    {
        Assert.Equal(216, Monk.Everything.Count(one => Monk.Holds(Puzzle.One, one)));
        Assert.Equal(142, Monk.Everything.Count(one => Monk.Holds(Puzzle.Two, one)));
        Assert.Equal(228, Monk.Everything.Count(one => Monk.Holds(Puzzle.Three, one)));

        Assert.Equal(0.5000, Monk.Bar(Puzzle.One), 4);
        Assert.Equal(0.6713, Monk.Bar(Puzzle.Two), 4);
        Assert.Equal(0.5278, Monk.Bar(Puzzle.Three), 4);
    }

    /// <summary>
    /// <b>THE CONCEPTS ARE THE PUBLISHED ONES, CHECKED AGAINST THEIR OWN SHORT RULES.</b>
    /// </summary>
    /// <remarks>
    /// An encoding slip here would be invisible everywhere else — the counts above would
    /// still be counts and the lattice below would still be a lattice, both of some other
    /// problem. <see cref="Puzzle.One"/> is <c>head = body or jacket is red</c>, so the
    /// minimal sound rules that say YES must be exactly those four, and they are
    /// recovered by enumeration rather than asserted from the paper.
    /// </remarks>
    [Fact]
    public void The_first_puzzle_s_own_concept_falls_out_of_the_enumeration()
    {
        var yes = Monk.Truths(Puzzle.One)
            .Where(truth => truth.Expects == Monk.Says(holds: true))
            .ToList();

        Assert.Equal(4, yes.Count);

        // `jacket is red` on its own, which is one code and one code only.
        Assert.Single(yes, truth => truth.Scope.Length == 1);
        Assert.Contains(yes, truth => truth.Scope.SequenceEqual(new[] { Monk.Of(4, 0) }));

        // AND `head = body`, WHICH A CONJUNCTION SAYS THREE TIMES BECAUSE IT CANNOT SAY
        // IT ONCE. Equality between two attributes is not a thing a scope can express;
        // what it can express is each of the three ways of satisfying it.
        Assert.Equal(3, yes.Count(truth => truth.Scope.Length == 2));

        foreach (var value in new[] { 0, 1, 2 })
            Assert.Contains(yes, truth =>
                truth.Scope.Contains(Monk.Of(0, value)) && truth.Scope.Contains(Monk.Of(1, value)));
    }

    /// <summary>
    /// <b>THE FINDING: ON THE SECOND PUZZLE THE ONLY SOUND WAY TO SAY YES IS TO NAME AN
    /// INSTANCE.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// There is no sound conjunction predicting the concept at depth one, two, three,
    /// four or five. Every one of the 142 minimal sound rules that says YES pins all six
    /// attributes — which is a single instance, and a single instance covers no other.
    /// <b>So a population cannot generalise on the positive side of this concept at all;
    /// it can only memorise, one robot at a time.</b>
    /// </para>
    /// <para>
    /// <b>AND THE SHORTEST SOUND RULE OF ANY KIND IS DEPTH THREE, WHICH IS CHECKABLE BY
    /// HAND.</b> The twenty of them are the twenty ways to choose three of six attributes
    /// and pin all three to their first value: that forces the count to at least three,
    /// so the answer is NO whatever the other three do. Twenty is <c>C(6,3)</c>, and a
    /// count that came out as anything else would mean the concept had been mis-encoded.
    /// </para>
    /// </remarks>
    [Fact]
    public void Nothing_below_a_whole_instance_can_soundly_say_yes_on_the_second()
    {
        var truths = Monk.Truths(Puzzle.Two);

        var yes = truths.Where(one => one.Expects == Monk.Says(holds: true)).ToList();
        var no = truths.Where(one => one.Expects == Monk.Says(holds: false)).ToList();

        output.WriteLine($"minimal sound rules: {truths.Length} ({yes.Count} yes, {no.Count} no)");

        foreach (var depth in truths.Select(one => one.Scope.Length).Distinct().Order())
            output.WriteLine(
                $"  depth {depth}: {truths.Count(one => one.Scope.Length == depth)}"
                + $" ({truths.Count(one => one.Scope.Length == depth && one.Expects == Monk.Says(true))} yes)");

        // THE CEILING. Every sound YES pins all six.
        Assert.Equal(142, yes.Count);
        Assert.All(yes, truth => Assert.Equal(Monk.Widths.Length, truth.Scope.Length));

        // AND THE SHORTEST SOUND RULE AT ALL IS THREE, WHICH IS C(6,3) OF THEM.
        Assert.Equal(3, no.Min(one => one.Scope.Length));
        Assert.Equal(20, no.Count(one => one.Scope.Length == 3));
    }

    /// <summary>
    /// <b>THE CONTROLS, AND THEY RUN THE OTHER WAY.</b>
    /// </summary>
    /// <remarks>
    /// A basis of 22 rules and one of 12 against one of 254, of which 142 are whole
    /// instances. <b>The size of the minimal basis IS the language ceiling</b>, and it is
    /// a fact about the pairing of concept and language rather than about any learner —
    /// which is what makes it worth knowing before a run rather than after one.
    /// </remarks>
    [Fact]
    public void A_reachable_concept_has_a_small_basis_and_the_counting_one_does_not()
    {
        var bases = new[] { Puzzle.One, Puzzle.Two, Puzzle.Three }
            .Select(puzzle => (puzzle, truths: Monk.Truths(puzzle)))
            .ToList();

        foreach (var (puzzle, truths) in bases)
            output.WriteLine(
                $"{puzzle,-5} | {truths.Length,3} minimal sound rules, "
                + $"shortest {truths.Min(one => one.Scope.Length)}, "
                + $"deepest {truths.Max(one => one.Scope.Length)}, "
                + $"{truths.Count(one => one.Scope.Length == Monk.Widths.Length)} whole instances");

        Assert.Equal(22, bases[0].truths.Length);
        Assert.Equal(254, bases[1].truths.Length);
        Assert.Equal(12, bases[2].truths.Length);

        // THE ONE THAT MATTERS: the reachable puzzles need no whole instance and the
        // counting one needs 142 of them.
        Assert.Equal(0, bases[0].truths.Count(one => one.Scope.Length == Monk.Widths.Length));
        Assert.Equal(0, bases[2].truths.Count(one => one.Scope.Length == Monk.Widths.Length));
        Assert.Equal(142, bases[1].truths.Count(one => one.Scope.Length == Monk.Widths.Length));
    }

    /// <summary>
    /// <b>EVERY RULE THE KEY CALLS TRUE IS TRUE, AND A CONTRADICTION IS NOT.</b>
    /// </summary>
    /// <remarks>
    /// The second half is the one that matters: a scope pinning one attribute to two
    /// values is satisfied by nothing and so entails everything vacuously. Calling that
    /// sound would let a learner score by minting contradictions, which is a way of
    /// passing a soundness check without holding a single true rule.
    /// </remarks>
    [Fact]
    public void The_soundness_check_agrees_with_the_key_and_refuses_a_contradiction()
    {
        foreach (var truth in Monk.Truths(Puzzle.Two))
            Assert.True(Monk.Sound(Puzzle.Two, truth.Scope, truth.Expects));

        ImmutableArray<Code> impossible = [Monk.Of(0, 0), Monk.Of(0, 1)];

        Assert.False(Monk.Sound(Puzzle.Two, impossible, Monk.Says(holds: true)));
        Assert.False(Monk.Sound(Puzzle.Two, impossible, Monk.Says(holds: false)));

        // AND A CODE FROM ANOTHER WORLD IS NOT CHECKABLE HERE, so a minted name reaching
        // this key is refused rather than silently called unsound.
        Assert.False(Monk.Checkable([new Code(Modality: 9, 1)]));
    }

    /// <summary>
    /// <b>WHAT IS WITHHELD IS NEVER DRAWN, AND THE SPLIT IS A POSITION.</b>
    /// </summary>
    /// <remarks>
    /// The same rule <see cref="Cifar"/> follows and for the same reason: a held-out set
    /// chosen by the world's own generator moves with the seed, and two seeds would then
    /// be scored against two different questions.
    /// </remarks>
    [Fact]
    public void The_withheld_instances_are_never_drawn()
    {
        var world = new Monk(new MonkSettings { Puzzle = Puzzle.Two, Withheld = 132 }, seed: 1);

        Assert.Equal(132, world.Withheld.Count);

        var held = world.Withheld
            .Select(one => string.Join(",", one.Seen))
            .ToHashSet(StringComparer.Ordinal);

        for (var draw = 0; draw < 20_000; draw++)
            Assert.DoesNotContain(string.Join(",", world.Next().Seen), held);

        Assert.Equal(0.6713, world.Chance, 4);
        Assert.Equal(2, world.Outcomes);
    }

    /// <summary>
    /// <b>THE LEARNER AGAINST THE CEILING, ON ALL THREE, WITH THE BAR BESIDE EVERY
    /// SCORE.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>NO THRESHOLD, BECAUSE A BAR WRITTEN BEFORE THE FIRST RUN IS A PREDICTION
    /// DRESSED AS A CHECK.</b> What this asserts is only what the enumeration above has
    /// already decided — that the basis differs — and what it PRINTS is the grid the
    /// finding gets read off.
    /// </para>
    /// <para>
    /// <b>What to look for.</b> On <see cref="Puzzle.Two"/> a score near 0.6713 with a
    /// large resident count is the ceiling arriving exactly as predicted: the population
    /// naming positives one at a time, which cannot cover a withheld instance. A score
    /// well above it would mean the prediction is wrong and is the more interesting
    /// outcome.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_the_learner_does_against_a_ceiling_that_is_already_known()
    {
        foreach (var puzzle in new[] { Puzzle.One, Puzzle.Two, Puzzle.Three })
        {
            var run = new MonkRun(
                new MonkSettings { Puzzle = puzzle, Withheld = 132 },
                new Brain(new CommittingSettings(), seed: 1),
                seed: 1);

            var got = run.Run(20_000);

            output.WriteLine(
                $"{puzzle,-5} | recent {got.Recent:F3} against a silent arm's {run.Chance:F3} "
                + $"· resident {got.Resident} ({got.Repaired} repaired) "
                + $"· sound {got.Sound} unsound {got.Unsound} unchecked {got.Unchecked} "
                + $"· found {got.Found} of {got.Truths}");

            // THE ONE THING THIS WORLD GUARANTEES: every rule is decidable here, so a
            // soundness count that quietly skipped some would be a silent instrument.
            Assert.Equal(0, got.Unchecked);

            // AND THE INSTRUMENT IS ARMED, WHICH IT WAS NOT WHEN THIS FILE WAS WRITTEN.
            // The answer key was first built in the world's own outcome alphabet rather
            // than the shared one, so every rule it called true expected a code the
            // population can never hold: `sound` and `found` read NOUGHT on all three
            // puzzles and looked exactly like a learner holding nothing true. A count
            // that cannot rise is not a measurement of the learner, and this is the
            // assertion that says it can.
            Assert.True(got.Sound > 0, $"{puzzle} found no sound rule at all — is the key blind again?");
            Assert.True(got.Found > 0, $"{puzzle} matched none of the {got.Truths} minimal rules");
        }
    }
}
