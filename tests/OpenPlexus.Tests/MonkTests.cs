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
    /// What the genesis gate costs where the world's whole rule set is enumerable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Fork 135's third world</b>, and the one where coverage is checkable rather than
    /// inferred. <c>Surprising.Unaccounted</c> stops genesis whenever anything that fired
    /// expected what arrived, and an outvoted advocate being right by luck is enough — so
    /// proposals dry up while most of the world is unfound. Here <c>Found</c> against
    /// <c>Truths</c> says exactly how much.
    /// </para>
    /// <para>
    /// <b>What the two gates really are.</b> Genesis only runs at all where the VOTE missed,
    /// so <c>AnyFailure</c> is <i>the decider had no account of this</i> and
    /// <c>Unaccounted</c> is <i>nobody holding anything had one</i>. The plan's own trap says
    /// a gate on what is HELD cannot reach what DECIDES, and this is that line pointed at
    /// genesis rather than at the seat.
    /// </para>
    /// <para>
    /// <b>What would drop the arm</b>, said before the run: <c>AnyFailure</c> finding no more
    /// of the world's rules than <c>Unaccounted</c> on any puzzle, or costing withheld
    /// accuracy on one where the language is not the ceiling. <see cref="Puzzle.Two"/> is a
    /// counting concept a conjunction cannot say, so more rules found there is memorising and
    /// is not evidence either way.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_the_genesis_gate_costs_where_the_rule_set_is_enumerable()
    {
        foreach (var puzzle in new[] { Puzzle.One, Puzzle.Two, Puzzle.Three })
        {
            foreach (var gate in new[] { Surprising.Unaccounted, Surprising.AnyFailure })
            {
                var unseen = new List<double>();
                var found = new List<double>();

                foreach (var seed in new[] { 1, 2, 3, 4, 5, 6, 7, 8 })
                {
                    var run = new MonkRun(
                        new MonkSettings { Puzzle = puzzle, Withheld = 132 },
                        new Brain(new CommittingSettings { Surprising = gate }, seed),
                        seed);

                    var got = run.Run(20_000);

                    unseen.Add(got.Tally.Unseen?.Accuracy ?? 0.0);
                    found.Add(got.Found);

                    output.WriteLine(
                        $"  {puzzle,-5} {gate,-12} seed {seed} | withheld "
                        + $"{got.Tally.Unseen?.Accuracy ?? 0.0:F3} against {run.Chance:F3} "
                        + $"| drawn {got.Recent:F3} | found {got.Found}/{got.Truths} "
                        + $"| sound {got.Sound} unsound {got.Unsound} "
                        + $"| resident {got.Resident} minted {got.Tally.Minted}");
                }

                output.WriteLine(
                    $"  {puzzle,-5} {gate,-12} MEAN withheld "
                    + $"{new Measured { Arm = "withheld", Values = unseen }} | found "
                    + $"{new Measured { Arm = "found", Values = found }}");
            }

            output.WriteLine("");
        }

        // The bars are asserted in the fact above. This prints what the gate did to a world
        // whose whole rule set can be counted.
        Assert.True(true);
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
        //
        // And the first reading says this instrument cannot answer its own question. Monk-2
        // is here because it is a counting concept a conjunctive scope CANNOT express -- the
        // language-ceiling probe with a published number -- and `Wanting` reads about 0.009
        // on it, the same order as every world whose concept the language covers outright.
        // An instrument built to say which rung a failure demands does not see a ceiling it
        // is provably standing on.
        //
        // The reason is structural rather than a calibration. `Unseparated` counts rounds
        // where the search came back EMPTY, and a conjunctive search almost never does,
        // because narrowing is always available: a longer scope separates misses from hits
        // on nearly any finite sample whether or not it is the rule. So a low share here is
        // equally consistent with the language being adequate and with the machine
        // memorising, which is the specialise-only failure the plan already names.
        //
        // What follows for rung two is that the failures are not asking for it. The second
        // column is conditional on the first, so monk-2's 0.279 is 0.279 of 0.009 and
        // monk-3's 0.817 is that share of a denominator near nought on the five seeds that
        // had one at all.
        //
        // Asserted on the world whose ceiling is KNOWN, because that is the cell that says
        // the instrument is blind rather than the world easy. The day monk-2 reads high
        // here, `Unseparated` has started detecting a ceiling and this whole reading is
        // worth re-taking.
        Assert.All(
            Enumerable.Range(1, Seeds),
            seed => Assert.True(
                Monked(Puzzle.Two, seed).Wanting < 0.1,
                $"monk-2 seed {seed} reports the language running out on "
                + $"{Monked(Puzzle.Two, seed).Wanting:F3} of repairable rounds. The probe has "
                + "started seeing the ceiling it is standing on, so re-read the rung question "
                + "-- the reading it was answered with says it could not"));
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
    /// runs across two arms and three seeds, which is what <i>the jacket is red, else no</i>
    /// scores on that tail. A held-out score that cannot move reads exactly like a learner
    /// that will not generalise.
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
