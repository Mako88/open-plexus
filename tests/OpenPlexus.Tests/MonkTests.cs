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
/// <b>Step eight is <i>the rung the failures demand</i>.</b> And never the rung that sounds
/// next, AND NO FAILURE HAD ASKED. On the multiplexer twelve one-code rules
/// cover everything held out, so the scope language is nowhere near binding there. This
/// world is built so a failure can ask, and it asks BEFORE a learner is run: the concept
/// and the language are both finite, so what a conjunction can and cannot say about the
/// MONK's problems is a thing to be enumerated rather than a thing to be discovered from
/// a disappointing score.
/// </para>
/// <para>
/// <b>And the two controls are the point of having three puzzles.</b> A learner falling
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
    /// <b>The bar is not a half on the one puzzle that matters.</b>
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
    /// <b>The concepts are the published ones, checked against their own short rules.</b>
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

        // AND `head = body`, which a conjunction says three times because it cannot say
        // it once. Equality between two attributes is not a thing a scope can express;
        // what it can express is each of the three ways of satisfying it.
        Assert.Equal(3, yes.Count(truth => truth.Scope.Length == 2));

        foreach (var value in new[] { 0, 1, 2 })
            Assert.Contains(yes, truth =>
                truth.Scope.Contains(Monk.Of(0, value)) && truth.Scope.Contains(Monk.Of(1, value)));
    }

    /// <summary>
    /// <b>The finding</b>: on the second puzzle the only sound way to say yes is to name an
    /// instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There is no sound conjunction predicting the concept at depth one, two, three,
    /// four or five. Every one of the 142 minimal sound rules that says YES pins all six
    /// attributes — which is a single instance, and a single instance covers no other.
    /// <b>So a population cannot generalise</b> on the positive side of this concept at all;
    /// it can only memorise, one robot at a time.
    /// </para>
    /// <para>
    /// <b>And the shortest sound rule of any kind is depth three</b>, which is checkable by
    /// hand. The twenty of them are the twenty ways to choose three of six attributes
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

        // And the shortest sound rule at all is three, which is C(6,3) OF THEM.
        Assert.Equal(3, no.Min(one => one.Scope.Length));
        Assert.Equal(20, no.Count(one => one.Scope.Length == 3));
    }

    /// <summary>
    /// <b>The controls, and they run the other way.</b>
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
    /// <b>Every rule the key calls true is true</b>, and a contradiction is not.
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

        // And a code from another world is not checkable here, so a minted name reaching
        // this key is refused rather than silently called unsound.
        Assert.False(Monk.Checkable([new Code(Modality: 9, 1)]));
    }

    /// <summary>
    /// <b>What is withheld is never drawn, and the split is a position.</b>
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
    /// <b>The learner against the ceiling, on all three</b>, with the bar beside every
    /// score.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>No threshold</b>, because a bar written before the first run is a prediction
    /// dressed as a check. What this asserts is only what the enumeration above has
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

            // The one thing this world guarantees: every rule is decidable here, so a
            // soundness count that quietly skipped some would be a silent instrument.
            Assert.Equal(0, got.Unchecked);

            // And the instrument is armed, which it was not when this file was written.
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

    /// <summary>
    /// <b>Whether the failures are asking for a rung</b> — fork 50, as a number.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The ladder's rule is decidable and was being read by nothing.</b> The plan says
    /// a rung is admitted when and only when no expression in the current language
    /// separates the failures from the hits, and that choosing one before a failure asks is
    /// hand-specified bias by a side door — the fault that killed ILP. That condition is
    /// exactly <c>Conditions.Discriminator</c> coming back empty, which has happened every run
    /// since the branch began and has never been counted. <c>Tally.Wanting</c> is the count.
    /// </para>
    /// <para>
    /// <b>The prediction, written before the first reading and not in an assertion.</b>
    /// <see cref="Puzzle.Two"/> is a counting concept: EXACTLY TWO of six attributes hold
    /// their first value, which a conjunction cannot say at any depth. So its failures
    /// should be the ones nothing separates, and <see cref="Puzzle.One"/> and
    /// <see cref="Puzzle.Three"/> — a disjunction of two conjunctions, and the same with
    /// noise — should be far lower. The multiplexer is the control from another world
    /// entirely, where the true rules ARE conjunctions and this should be near nought.
    /// </para>
    /// <para>
    /// <b>A number flat across all four</b> says the rung is not what is missing.
    /// That is the outcome worth most: it would say `Monk-2`'s ceiling is the floor, the
    /// budget or the gates refusing to repair rather than the language failing to describe,
    /// and every argument for rung two so far has been an argument.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task Whether_the_failures_are_asking_for_a_rung()
    {
        const int Seeds = 8;

        var worlds = new (string World, Func<int, Tally> Run)[]
        {
            ("monk-1", seed => Monked(Puzzle.One, seed)),
            ("monk-2", seed => Monked(Puzzle.Two, seed)),
            ("monk-3", seed => Monked(Puzzle.Three, seed)),
            ("plex-6", seed => Plexed(2, seed)),
            ("plex-11", seed => Plexed(3, seed)),
        };

        output.WriteLine(
            $"the share of repairable rounds nothing separates, and of THOSE the share an "
            + $"absence would, over {Seeds} seeds");

        foreach (var (world, run) in worlds)
        {
            // Cached, so both shares are read off the same runs on the same seeds. `Sweep`
            // mixes the seed counter before handing it over, because near-neighbour seeds
            // agree more than chance allows and that agreement comes straight off the
            // standard error -- so the sequence has to come from there rather than from a
            // loop here, and the cache is what lets a second reading share it.
            var ran = new Dictionary<int, Tally>();

            Tally At(int seed)
            {
                if (!ran.TryGetValue(seed, out var got)) ran[seed] = got = run(seed);
                return got;
            }

            var wanting = await Sweep.ArmAsync(
                world, Seeds, seed => Task.FromResult(At(seed).Wanting));

            // And the second share is over the first's numerator, so a seed that separated
            // everything contributes no opinion about absence rather than a nought. A
            // nought there would say an absence did not help on a seed where nothing needed
            // helping, which is reading a silence as a refusal.
            var absence = new Measured
            {
                Arm = world,
                Values =
                [
                    .. ran.Values
                        .Where(one => one.Unseparated > 0)
                        .Select(one => one.Absented / (double)one.Unseparated),
                ],
            };

            output.WriteLine(
                $"{world,-9} | wanting {wanting.Mean:F3} +/-{wanting.StdErr:F3} "
                + $"| absence would {absence.Mean,6:F3} +/-{absence.StdErr:F3} "
                + $"over {absence.Seeds} of {Seeds} seeds");
        }

        // The instrument check, and it is the one this file already has a paragraph about.
        // `Wanting` is a ratio over `Blamed`, so a run where nothing was ever repairable
        // reports nought and reads exactly like a language that described everything.
        Assert.True(Monked(Puzzle.Two, 1).Blamed > 0,
            "repair was never offered a culprit, so `Wanting` is a ratio over nothing");

        // NO BAR ON EITHER SHARE. Which rung a failure demands has never been measured, so
        // a threshold written before the first reading would be a prediction dressed as a
        // requirement -- and this suite already had one of those refuted tonight.
    }

    /// <summary>
    /// The three arms `DialTests` holds hand-set, on worlds that are not the conversation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Their entries name a bar</b>, and a drawn lesson does not clear it. Each says it
    /// leaves when a generated world has put its two arms against each other, and the worry it
    /// names is a controller choosing on ONE world's evidence. <c>Lesson.Drawn</c> varies the
    /// words and not the shape, and the learner is word-blind — three of four claiming arms
    /// read identically written against drawn — so it puts a spread under the conversation
    /// rather than adding a second world.
    /// </para>
    /// <para>
    /// <b>These two are that second world.</b> The multiplexer's true rules ARE conjunctions
    /// and it cannot contain its own answer; Monk-1 is a published symbolic benchmark. Neither
    /// is told, neither has a vocabulary, and nothing in either is a statement — so an arm
    /// that only pays where somebody is talking shows up here as nothing.
    /// </para>
    /// <para>
    /// <b>The kill line, written before the grid ran</b>: an arm that wins on the conversation
    /// and loses on a world whose rules are conjunctions is a setting for that conversation
    /// rather than a default for the brain.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_hand_set_arms_read_on_worlds_that_are_not_a_conversation()
    {
        const int Seeds = 3;

        var arms = new (string Named, CommittingSettings Dials)[]
        {
            ("shipped ", new CommittingSettings()),
            ("wholly  ", new CommittingSettings { Rooting = Rooting.Wholly }),
            ("credited", new CommittingSettings { Crediting = Crediting.Birth }),
            ("testable", new CommittingSettings { Admitting = Admitting.Testable }),
        };

        output.WriteLine($"{Seeds} seeds, 20,000 rounds, one arm off the shipped brain");
        output.WriteLine("world        arm         drawn             held      repaired");

        var scored = new Dictionary<(string World, string Arm), double>();
        var repairs = new Dictionary<(string World, string Arm), double>();

        foreach (var (world, run) in new (string Named, Func<int, CommittingSettings, Tally> Run)[]
        {
            ("plex-3     ", (seed, dials) => Plexed(3, seed, dials)),
            ("monk-1     ", (seed, dials) => Monked(Puzzle.One, seed, dials)),
        })
        {
            foreach (var (named, dials) in arms)
            {
                var drawn = new List<double>();
                var held = new List<double>();
                var repaired = new List<double>();

                for (var seed = 1; seed <= Seeds; seed++)
                {
                    var tally = run(seed, dials);

                    drawn.Add(tally.Recent);
                    held.Add(tally.Resident);
                    repaired.Add(tally.Repaired);
                }

                scored[(world.Trim(), named.Trim())] = drawn.Average();
                repairs[(world.Trim(), named.Trim())] = repaired.Average();

                output.WriteLine(
                    $"{world,-13}{named,-12}{Sweep.Spread(drawn),18}{held.Average(),9:F1}"
                    + $"{repaired.Average(),10:F1}");
            }
        }

        // No bar on any score, and that is deliberate rather than a gap. Which way these go
        // has never been measured off the conversation, so a threshold written before the
        // first reading would be a prediction dressed as a requirement -- a fault this file
        // has already had refuted once.
        //
        // The wiring is asserted on the REPAIR count rather than on the score, and that is the
        // correction this grid needed. An address-two multiplexer reads 1.000 for every arm,
        // so a check on the score there fails at a ceiling and says nothing about whether the
        // arm ran -- which is this repo's own trap about a grid of identical rows being a
        // verdict on the world.
        foreach (var world in new[] { "plex-3", "monk-1" })
            Assert.True(
                new[] { "wholly", "credited", "testable" }
                    .Any(arm => Math.Abs(repairs[(world, arm)] - repairs[(world, "shipped")])
                        > 0.5),
                $"on {world} every arm repaired exactly as often as the shipped brain, so "
                + "none of the three is wired to this run at all");
    }

    /// <summary>
    /// <b>The split hides no attribute value</b>, which the tail split did and nothing read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The budget for the failure class rather than the fix for one instance of it. A withheld
    /// set that holds back a whole attribute value scores a learner on a distribution it never
    /// saw, and the number it produces is stuck — <see cref="Puzzle.One"/> read 0.7273 on six
    /// runs across two spellings and three seeds, which is what <i>the jacket is red, else
    /// no</i> scores on that tail. A held-out score that cannot move reads exactly like a
    /// learner that will not generalise.
    /// </para>
    /// <para>
    /// <b>Both halves, and every value on both.</b> Asking only that the withheld set is wide
    /// would pass a split that held back all but one instance of a value, which is the same
    /// fault at a different size.
    /// </para>
    /// </remarks>
    [Fact]
    public void Neither_half_of_the_split_hides_an_attribute_value()
    {
        var world = new Monk(new MonkSettings { Puzzle = Puzzle.One, Withheld = 132 }, seed: 1);

        var held = world.Withheld.Select(one => one.Seen).ToList();

        var drawn = Monk.Everything
            .Where((_, at) => !Monk.Back(at, 132))
            .ToList();

        Assert.Equal(132, held.Count);
        Assert.Equal(Monk.Everything.Length - 132, drawn.Count);

        for (var attribute = 0; attribute < Monk.Widths.Length; attribute++)
            for (var value = 0; value < Monk.Widths[attribute]; value++)
            {
                var at = attribute;
                var of = value;

                var inHeld = held.Count(one => one[at] == of);
                var inDrawn = drawn.Count(one => one[at] == of);

                output.WriteLine($"attribute {at} value {of} | drawn {inDrawn,4} held {inHeld,4}");

                Assert.True(inHeld > 0 && inDrawn > 0,
                    $"attribute {at} value {of} appears {inDrawn} times among the drawn and "
                    + $"{inHeld} times among the withheld, so the exam is scoring a value the "
                    + "run barely saw rather than the concept");
            }
    }

    /// <summary>
    /// <b>The clean rule is refused by the proposer</b>, whatever the spelling buys.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Monk-1's first disjunct is <c>head = body</c> and nothing else, which under
    /// <see cref="Spelling.Split"/> is a two-entry scope with both entries naming one
    /// variable. <c>Generalising.Siblings</c> will not propose it: a group whose hole covers
    /// every position of the scope is skipped, because a scope of variables alone is reached
    /// by no code in any moment and <c>Population.Firing</c> walks the code index.
    /// </para>
    /// <para>
    /// So what the rung can reach here is <c>head = body</c> with a constant beside it, one
    /// rule per value of whichever attribute the constant pins. That is sound and it is
    /// narrower than the truth, and it is the ceiling the sweep's numbers are read against —
    /// stated by enumeration rather than inferred from a score.
    /// </para>
    /// <para>
    /// <b>What lifts it is a scan list</b>, whose cost is unpriced. It is not a spelling
    /// question and no arm here touches it.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_rule_that_is_only_a_variable_twice_is_beyond_the_proposer()
    {
        // The two-entry scope Monk-1's first disjunct actually is, built by hand -- nothing
        // proposes it, which is the point.
        var clean = new[]
        {
            Unifying.Any(Monk.Attribute, name: 0),
            Unifying.Any((byte)(Monk.Attribute + 1), name: 0),
        }.ToImmutableArray();

        // It is TRUE of the world, so the ceiling is the proposer's rather than the key's.
        Assert.True(
            Monk.Sound(Puzzle.One, clean, Monk.Says(holds: true), Spelling.Split),
            "head equals body is not sound on Monk-1, so the key is not binding a repeated "
            + "name and every join column in this file is about the key");

        // And the ground rules it would be read off offer no sibling group at all. Three
        // scopes, one per shared head-and-body value, and the hole would stand in both of
        // the two positions each has -- which is the case `Siblings` skips.
        var bare = Enumerable.Range(0, 3)
            .Select(value => new Commitment(
                [
                    Monk.Of(attribute: 0, value, Spelling.Split),
                    Monk.Of(attribute: 1, value, Spelling.Split),
                ],
                Monk.Says(holds: true)))
            .ToList();

        Assert.All(bare, one => Assert.True(
            Monk.Sound(Puzzle.One, one.Scope, one.Expects, Spelling.Split)));

        Assert.Empty(Generalising.Siblings(bare));

        // While the same three with one attribute pinned beside them do offer one, and that
        // is what the rung can actually reach here -- the truth with a condition it does not
        // need, one rule per value of whatever the condition pins.
        var beside = Enumerable.Range(0, 3)
            .Select(value => new Commitment(
                [
                    Monk.Of(attribute: 0, value, Spelling.Split),
                    Monk.Of(attribute: 1, value, Spelling.Split),
                    Monk.Of(attribute: 2, value: 1, Spelling.Split),
                ],
                Monk.Says(holds: true)))
            .ToList();

        var offered = Generalising.Siblings(beside);

        Assert.NotEmpty(offered);
        Assert.Contains(offered, one => one.Holes.Count > 1);

        // And what it proposes off them is sound, so the ceiling is the missing generality
        // rather than a wrong rule.
        var proposed = Generalising.Rule(offered.First(one => one.Holes.Count > 1));

        Assert.True(
            Monk.Sound(Puzzle.One, proposed.Scope, proposed.Expects, Spelling.Split));

        output.WriteLine(
            $"head = body is sound and offers no sibling group at length 2; with one "
            + $"attribute pinned beside it, {offered.Count} groups are offered and what they "
            + "propose is sound");
    }

    /// <summary>What one spelling of one puzzle left behind.</summary>
    /// <param name="Recent">The trailing accuracy, on instances the run drew.</param>
    /// <param name="Unseen">
    /// The accuracy on the 132 held back. <b>The column this reading is about</b> — Monk-1 is
    /// answered at 0.997 in sample by a population naming instances one at a time, so the
    /// drawn score has no room to move and says nothing about whether anything generalised.
    /// </param>
    /// <param name="Silence">The share of the held-out instances nothing fired on.</param>
    /// <param name="Chance">What always naming the commoner answer scores.</param>
    /// <param name="Held">How many commitments are resident.</param>
    /// <param name="Sound">How many of them are true of the world.</param>
    /// <param name="Found">How many of the world's own minimal rules were reached.</param>
    /// <param name="Truths">How many there are to find.</param>
    /// <param name="Twice">How many residents say one value twice.</param>
    /// <param name="Groups">How many sibling groups the residents offer.</param>
    /// <param name="Repeated">How many of those would give a hole that repeats.</param>
    /// <param name="Joined">How many repeated ones the vocabulary admits.</param>
    /// <param name="Resident">How many residents name a variable in two places.</param>
    /// <param name="Fired">How often those residents fired and were answered.</param>
    /// <param name="Truest">
    /// How many of those joins are TRUE of the world. <b>The column that says whether the
    /// rung reached the concept</b> or only a rule shaped like it — a join that fires often
    /// and is unsound is the drop rung four already gets marked down for.
    /// </param>
    /// <param name="Shortest">The shortest join that is sound, or nought where none is.</param>
    /// <param name="Sorts">How many categories the front end derived.</param>
    private readonly record struct Spelt(
        double Recent, double Unseen, double Silence, double Chance,
        int Held, int Sound, int Found, int Truths,
        int Twice, int Groups, int Repeated, int Joined, int Resident, long Fired,
        int Truest, int Shortest, int Sorts);

    /// <summary>One puzzle under one spelling, with rung four's gate fed.</summary>
    /// <param name="puzzle">Which of the three.</param>
    /// <param name="spelling">How an attribute and its value are said.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    /// <remarks>
    /// <b>Its own bench rather than <see cref="MonkRun"/></b>, because the gate needs a
    /// vocabulary and the ordinary runner has none. <c>Population.Generalise</c> is inert
    /// without one, so a reading taken through the shipped runner would report nought holed
    /// rules under both spellings and say nothing whatever about the spelling.
    /// </remarks>
    private static Spelt Spell(Puzzle puzzle, Spelling spelling, int seed)
    {
        var settings = new MonkSettings
        {
            Puzzle = puzzle, Spelling = spelling, Withheld = 132,
        };

        var world = new Monk(settings, seed);
        var brain = new Brain(new CommittingSettings { Capacity = 2000 }, seed);

        var sorts = new Categories([]);

        IQuantizer<IReadOnlyList<int>> inner = spelling == Spelling.Split
            ? new Slotted(Monk.Attribute, Monk.Widths.Length)
            : new Bits(Monk.Attribute, Monk.Stride);

        // Never, because two values of one attribute cannot both hold -- which is the
        // exclusivity the gate is asking about, and it is true of this world by construction
        // under either spelling.
        var front = new Deriving<IReadOnlyList<int>>(
            inner, sorts, Counting.Company, Meeting.Never, floor: 20, every: 1000);

        brain.Held.Sorts = sorts;

        var tally = new Bench(new Watching<IReadOnlyList<int>>(world, front), brain)
            .Run(20_000, sweep: 1000, target: 0.9, window: 2000);

        var graded = Learned.Grade(
            tally, Monk.Truths(puzzle, spelling), brain.Held, brain.Dials.Floor,
            scope => Monk.Checkable(scope, spelling),
            (scope, expects) => Monk.Sound(puzzle, scope, expects, spelling),
            detailed: false);

        var all = brain.Held.All;

        var groups = Generalising.Siblings(all);
        var repeated = groups.Where(one => one.Holes.Count > 1).ToList();

        var joins = all.Where(one => one.Scope.Count(Unifying.Names) > 1).ToList();

        var truest = joins
            .Where(one => Monk.Sound(puzzle, one.Scope, one.Expects, spelling))
            .ToList();

        return new Spelt(
            tally.Recent,
            tally.Unseen?.Accuracy ?? 0.0,
            tally.Unseen?.Silence ?? 0.0,
            world.Chance,
            all.Count,
            graded.Sound,
            graded.Found,
            graded.Truths,
            all.Count(one => one.Scope
                .GroupBy(code => code.Value)
                .Any(group => group.Count() > 1)),
            groups.Count,
            repeated.Count,
            repeated.Count(one => Generalising.Admits(one, sorts)),
            joins.Count,
            joins.Sum(one => one.Fired),
            truest.Count,
            truest.Count == 0 ? 0 : truest.Min(one => one.Scope.Length),
            sorts.Count);
    }

    /// <summary>
    /// <b>What each spelling makes SAYABLE</b>, link by link and before any question of
    /// worth — fork 133.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Monk-1 is <c>head = body</c> or <c>jacket is red</c>, and the first disjunct is a
    /// variable standing in two places. Whether that is expressible at all is a fact about
    /// the front end rather than about the learner: <see cref="Spelling.Fused"/> packs the
    /// attribute into the value, so head-round and body-round are two different values and
    /// no name joins them; <see cref="Spelling.Split"/> puts the attribute in the modality
    /// and leaves them one value under two.
    /// </para>
    /// <para>
    /// <b>Each link is counted</b>, so an empty one is named rather than reported as a
    /// nought. The front end has to produce a scope saying one value twice, the proposer
    /// has to reach that shape, the vocabulary has to admit it, and the rule has to be
    /// resident and to fire. <c>GeneralisingTests</c> found the two empty links on the
    /// conversation this way.
    /// </para>
    /// <para>
    /// <b>Whether it is WORTH anything is the sweep beside this</b>, which is one seed's work
    /// times forty-eight. This one asserts only what a single seed can hold down.
    /// </para>
    /// </remarks>
    [Fact]
    public void Only_the_split_spelling_lets_a_variable_stand_in_two_places()
    {
        var fused = Spell(Puzzle.One, Spelling.Fused, seed: 1);
        var split = Spell(Puzzle.One, Spelling.Split, seed: 1);

        foreach (var (named, one) in new[] { ("fused", fused), ("split", split) })
            output.WriteLine(
                $"{named} | unseen {one.Unseen:F3} silent {one.Silence:F3} | held {one.Held,4} "
                + $"| sound {one.Sound,3} | twice {one.Twice,4} | groups {one.Groups,4} "
                + $"| repeated {one.Repeated,3} | admitted {one.Joined,3} "
                + $"| resident joins {one.Resident,3} | fired {one.Fired,6} "
                + $"| sound joins {one.Truest,3} shortest {one.Shortest} "
                + $"| categories {one.Sorts,2}");

        // Both arms have a vocabulary for the gate to read, or this is an empty control
        // against an empty control and the spelling is not what parts them.
        Assert.True(fused.Sorts > 0 && split.Sorts > 0);

        // The two keys are the same size, or a column compared across the arms is scored
        // against two different questions. The scope language does not move with the
        // spelling; only the names do.
        Assert.Equal(fused.Truths, split.Truths);

        // The front end's own contribution, and it is nought by construction rather than
        // small. One modality for every attribute cannot put one value in two positions.
        Assert.Equal(0, fused.Twice);
        Assert.Equal(0, fused.Repeated);
        Assert.Equal(0, fused.Resident);

        // And the split spelling reaches every link. A scope that says one value twice, a
        // rule with a variable in two places resident, and that rule answered -- which is
        // the thing rungs one to three cannot say at all, said on a published bench.
        Assert.True(split.Twice > 0,
            "the split spelling produced no scope saying one value twice, so the front end "
            + "is not supplying the two places a variable stands in");

        Assert.True(split.Resident > 0,
            $"{split.Twice} scopes say a value twice and no rule with a variable in two "
            + "places is resident, so the proposal was admitted and never added");

        Assert.True(split.Fired > 0,
            $"{split.Resident} rules with a variable in two places are resident and none was "
            + "ever answered, so the join is held and unable to fire");
    }

    /// <summary>
    /// <b>Whether a variable reaches the concept once the attribute leaves the value</b> —
    /// fork 133, on the bench whose baselines are published.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Monk-1 is <c>head = body</c> or <c>jacket is red</c>, and the first disjunct is a
    /// variable standing in two places — rung four's own shape. Whether it is SAYABLE is a
    /// fact about the front end rather than about the learner:
    /// <see cref="Spelling.Fused"/> packs the attribute into the value, so head-round and
    /// body-round are two different values and no name joins them;
    /// <see cref="Spelling.Split"/> puts the attribute in the modality and leaves them one
    /// value under two.
    /// </para>
    /// <para>
    /// <b>What would drop this arm</b>: a split spelling scoring no higher on the 132 held
    /// back. A rise in resident count or in holed rules is not the reading —
    /// <c>GeneralisingTests</c> has already found that a hole which fires more often and buys
    /// nothing is what rung four does by default.
    /// </para>
    /// <para>
    /// <b>And <see cref="Learned.Found"/> cannot be the column</b>, which is worth writing
    /// down because it was the obvious one to reach for. That count matches residents against
    /// the world's minimal CONJUNCTIONS, and a rule with a hole in it is not one of them at
    /// any depth — so it is flat across the two arms by construction rather than by finding.
    /// </para>
    /// <para>
    /// <b>All three puzzles, because the spelling is the world's and not Monk-1's.</b> An arm
    /// that wins where a variable is the concept and loses where it is not would be a setting
    /// for one puzzle rather than a spelling for the world. <see cref="Puzzle.Two"/> is the
    /// one where a variable cannot help at all, the concept being a count, so what it reads
    /// is the population the spelling costs.
    /// </para>
    /// <para>
    /// <b>A sweep</b>, because eight seeds of three puzzles of two arms is forty-eight runs.
    /// <see cref="Only_the_split_spelling_lets_a_variable_stand_in_two_places"/> is what the
    /// suite runs, and it holds down the links a single seed can settle.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_a_variable_reaches_the_concept_once_the_attribute_leaves_the_value()
    {
        const int Seeds = 8;

        var read = new Dictionary<(Puzzle, Spelling), List<Spelt>>();

        foreach (var puzzle in new[] { Puzzle.One, Puzzle.Two, Puzzle.Three })
            foreach (var spelling in new[] { Spelling.Fused, Spelling.Split })
            {
                var runs = new List<Spelt>();

                foreach (var seed in Enumerable.Range(1, Seeds))
                {
                    var one = Spell(puzzle, spelling, seed);

                    runs.Add(one);

                    output.WriteLine(
                        $"{puzzle,-5} {spelling,-5} seed {seed} | recent {one.Recent:F3} "
                        + $"| unseen {one.Unseen:F3} silent {one.Silence:F3} "
                        + $"against {one.Chance:F3} | held {one.Held,4} | sound {one.Sound,4} "
                        + $"| found {one.Found,3} of {one.Truths,3} "
                        + $"| twice {one.Twice,4} | groups {one.Groups,4} "
                        + $"| repeated {one.Repeated,3} | admitted {one.Joined,3} "
                        + $"| resident joins {one.Resident,3} | fired {one.Fired,6} "
                        + $"| sound joins {one.Truest,3} shortest {one.Shortest} "
                        + $"| categories {one.Sorts,2}");
                }

                read[(puzzle, spelling)] = runs;
            }

        // The front end's own contribution, and it is the link nothing after it can be read
        // without. A fused spelling cannot put one value in two positions at all -- that is
        // nought by construction rather than a small number.
        Assert.Equal(0, read[(Puzzle.One, Spelling.Fused)].Sum(one => one.Repeated));

        // And the gate has something to read under both, or the two arms are an empty
        // control against an empty control.
        foreach (var spelling in new[] { Spelling.Fused, Spelling.Split })
            Assert.All(read[(Puzzle.One, spelling)], one => Assert.True(one.Sorts > 0,
                $"the {spelling} arm derived no categories, so rung four proposed nothing "
                + "and the spelling is not what this row is measuring"));

        // The two keys have to be the same size or the `Found` columns are scored against
        // two different questions. The scope language does not move with the spelling --
        // only the names do -- so this is a statement about the change rather than about
        // the learner.
        foreach (var puzzle in new[] { Puzzle.One, Puzzle.Two, Puzzle.Three })
            Assert.Equal(
                read[(puzzle, Spelling.Fused)][0].Truths,
                read[(puzzle, Spelling.Split)][0].Truths);

        // And the exam is answered at all, or the row above is an accuracy over a handful.
        Assert.All(
            read[(Puzzle.One, Spelling.Split)],
            one => Assert.True(one.Silence < 1.0,
                "the split arm fired on nothing it had not seen, so its unseen accuracy is "
                + "over an empty set"));

        foreach (var puzzle in new[] { Puzzle.One, Puzzle.Two, Puzzle.Three })
            output.WriteLine(
                $"{puzzle,-5} mean | unseen fused "
                + $"{read[(puzzle, Spelling.Fused)].Average(one => one.Unseen):F3} "
                + $"split {read[(puzzle, Spelling.Split)].Average(one => one.Unseen):F3} "
                + $"| silent fused "
                + $"{read[(puzzle, Spelling.Fused)].Average(one => one.Silence):F3} "
                + $"split {read[(puzzle, Spelling.Split)].Average(one => one.Silence):F3} "
                + $"| held fused {read[(puzzle, Spelling.Fused)].Average(one => one.Held):F0} "
                + $"split {read[(puzzle, Spelling.Split)].Average(one => one.Held):F0}");
    }

    /// <summary>One Monk run, reported in the terms every world shares.</summary>
    /// <param name="puzzle">Which of the three.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    /// <param name="dials">The brain, or the shipped one where an arm is not being read.</param>
    private static Tally Monked(Puzzle puzzle, int seed, CommittingSettings? dials = null) =>
        new MonkRun(
            new MonkSettings { Puzzle = puzzle, Withheld = 132 },
            new Brain(dials ?? new CommittingSettings(), seed),
            seed).Run(20_000).Tally;

    /// <summary>The control from a world whose true rules ARE conjunctions.</summary>
    /// <param name="address">Address bits.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    /// <param name="dials">The brain, or the shipped one where an arm is not being read.</param>
    private static Tally Plexed(int address, int seed, CommittingSettings? dials = null) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address },
            new Brain(dials ?? new CommittingSettings(), seed),
            seed).Run(20_000).Tally;
}
