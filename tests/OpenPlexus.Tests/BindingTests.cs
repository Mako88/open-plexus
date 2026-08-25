using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The world this architecture provably cannot do, and the prediction registered
/// before it was ever run.
/// </summary>
/// <remarks>
/// <para>
/// <b>Pre-registered, 2026-08-03, before the first execution</b>: the unbound arm
/// scores EXACTLY AT CHANCE. Not poorly — at chance, because the two
/// situations it is asked to tell apart are literally the same input. If it
/// scored meaningfully above chance, the model of this architecture written in
/// the handoff would be wrong and the four borrowings planned on top of it would
/// need revisiting before any of them was built.
/// </para>
/// <para>
/// <b>Measured, 16 seeds, stamina 12</b>: 0.5240 ± 0.0268 against a chance of
/// 0.5000 — nine tenths of a standard error — while the control arm, which
/// differs only in a fact the counts can hold, scores 0.9167 ± 0.0095. Nearly
/// fourteen standard errors apart, on identical input.
/// </para>
/// <para>
/// <b>Re-baselined 2026-08-03 once <see cref="Seeds.Apart"/> reached
/// <see cref="Sweep"/>.</b> The first reading was 0.5064 ± 0.0213 against
/// 0.9247 ± 0.0072; the claim is unchanged and every error bar is wider, which
/// is what removing correlated seeds is supposed to do.
/// </para>
/// <para>
/// <b>The proof is the world tests, not the accuracy.</b> Two scenes with
/// opposite bindings emit the identical code sequence, which is asserted rather
/// than argued; the accuracy is what that identity looks like from the far end of
/// the system.
/// </para>
/// <para>
/// <b>And the prediction stands</b>, with the reason it was always conditional. An occasion
/// is a SET of co-occurring codes and no conjunction over one can separate the two scenes —
/// which is a claim about what the codes carry rather than about what a front end may say
/// beside them. <see cref="Codes.IQuantizer{TObservation}.Bind"/> is the front end saying
/// which codes are one object, and it had no reader on this branch until
/// <see cref="Commitments.Spanning"/>.
/// </para>
/// <para>
/// <b>Read, the unbound arm goes to 0.9938 +-0.0036</b> against a control at 0.5050 +-0.0109
/// on the withheld set, holding every one of the 144 rules the world has where the control
/// holds none — and the control is the bigger population, so it is not more search. Ignored,
/// the two are equal to the digit. The world emits the identical stream either way and
/// <see cref="The_two_arms_of_the_grouping_still_see_the_identical_input"/> asserts it.
/// </para>
/// <para>
/// <b>That is representability rather than composition</b>, and the distinction is the
/// world's own. The grouping IS the binding expressed as data, so a front end that segments
/// has told the machine the answer in the one form the codes cannot carry; what this says is
/// that the machine can act on it. <see cref="ComposedTests"/> is the world built to ask
/// whether it composes, and that one reads at chance.
/// </para>
/// </remarks>
public sealed class BindingTests(ITestOutputHelper output)
{
    private static BindingSettings World(bool bound, int concepts = 8, int codes = 3) =>
        Fixture.Binding(bound, concepts, codes);

    // ---- what the world is, asserted rather than described ------------------

    [Fact]
    public void The_two_arms_see_the_identical_input()
    {
        // The proof, and everything else here is a demonstration of it. The bound
        // world and the unbound world at the same seed emit the same codes in the
        // same order, scene after scene. Only which shape is answerable for which
        // colour differs -- and that lives nowhere in what the machine receives.
        var bound = new Binding(World(bound: true), seed: 1);
        var unbound = new Binding(World(bound: false), seed: 1);

        for (var i = 0; i < 2_000; i++)
            Assert.Equal(bound.Draw().Codes, unbound.Draw().Codes);
    }

    [Fact]
    public void And_the_answer_differs_about_half_the_time()
    {
        // THE COMPANION, and without it the test above passes for two worlds that
        // are simply the same world. Same input, different truth, on about half
        // the scenes -- which is what makes the task a coin flip from inside.
        var bound = new Binding(World(bound: true), seed: 1);
        var unbound = new Binding(World(bound: false), seed: 1);

        var differed = 0;
        for (var i = 0; i < 1_000; i++)
            if (!bound.Draw().Shapes.SequenceEqual(unbound.Draw().Shapes)) differed++;

        Assert.InRange(differed, 400, 600);
    }

    [Fact]
    public void The_binding_coin_is_fair_and_its_spread_is_honest()
    {
        // The trap that cost a false five-sigma result, 2026-08-03, kept as a
        // check so it cannot come back. A seeded Random in .NET normalises by
        // magnitude, so consecutive seeds produce nearly the same stream: over
        // seeds 1..8 the swap count landed in 19..23 of 39, a spread of about 1.3
        // where a fair coin gives 3.1. A standard error taken ACROSS those seeds
        // is then far too small, and this world's headline read as five sigma
        // below chance when it was sitting on it.
        //
        // So both halves are asserted. Fair on its own is not enough -- the
        // broken seeding was fair too, and still wrong.
        var counts = Enumerable.Range(1, 32).Select(seed =>
        {
            var world = new Binding(World(bound: false), seed);
            var swaps = 0;

            for (var moment = 0; moment < 400; moment++)
            {
                var scene = world.Draw();

                // The questions are asked on every tenth scene, so that is the
                // subsample whose fairness actually decides a score.
                if (moment % 10 == 0 && moment != 0 && scene.Shapes[0] != scene.Colours[0])
                    swaps++;
            }

            return (double)swaps;
        }).ToList();

        var measured = new Measured { Arm = "swap rate", Values = counts };

        // Fair: 39 draws a seed, so half is 19.5 and the pooled error is small.
        Assert.True(Math.Abs(measured.Mean - 19.5) < 3 * measured.StdErr + 0.5,
            $"{measured} against 19.5 of 39");

        // AND HONEST: a fair coin over 39 draws has a standard deviation of 3.12,
        // so anything much under that means the seeds are agreeing with each
        // other rather than sampling.
        var spread = measured.StdErr * Math.Sqrt(counts.Count);
        Assert.True(spread > 2.0,
            $"the per-seed spread is {spread:F2} where a fair coin gives 3.12, " +
            "so these seeds are not independent");
    }

    [Fact]
    public void A_scene_is_two_objects_of_two_different_kinds()
    {
        var world = new Binding(World(bound: false), seed: 2);

        for (var i = 0; i < 1_000; i++)
        {
            var scene = world.Draw();

            Assert.Equal(2, scene.Objects);
            Assert.Equal(4, scene.Codes.Count);
            Assert.NotEqual(scene.Colours[0], scene.Colours[1]);

            // Both objects' shapes are present, and they are the two kinds in the
            // scene. The binding permutes them; it never introduces a third.
            Assert.Equal([.. scene.Colours.Order()], [.. scene.Shapes.Order()]);
        }
    }

    [Fact]
    public void A_scene_shows_two_colours_and_two_shapes()
    {
        var world = new Binding(World(bound: false), seed: 2);

        for (var i = 0; i < 500; i++)
        {
            var attributes = world.Draw().Codes
                .GroupBy(code => code.Modality)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.Equal(2, attributes[Binding.Colour]);
            Assert.Equal(2, attributes[Binding.Shape]);
        }
    }

    [Fact]
    public void Two_concepts_never_share_a_code()
    {
        var world = new Binding(World(bound: false, concepts: 6, codes: 4), seed: 3);

        var all = (from attribute in (byte[])[Binding.Colour, Binding.Shape]
                   from concept in Enumerable.Range(0, 6)
                   from code in world.Of(attribute, concept)
                   select code).ToArray();

        Assert.Equal(all.Length, all.Distinct().Count());
        Assert.All(all, code => Assert.Equal(
            (int)(code.Value / 1000), Binding.Concept(code)));
    }

    // ---- what it measures ---------------------------------------------------

    [Fact]
    public void A_tag_without_its_group_is_refused_rather_than_accepted()
    {
        // An arm that looks distinct and is not is how this project has fooled
        // itself before. An ungrouped tag pairs with every code in the scene, so
        // it indexes nothing and the arm would quietly measure the untagged one.
        Assert.Throws<ArgumentException>(() =>
            new Binding(World(bound: false) with { Tagged = true }, seed: 1));
    }

    [Fact]
    public void The_question_points_at_a_thing_and_never_at_a_colour()
    {
        // The reading rests on this. Both colours are in every scene, so a question that sat
        // outside every part would name a colour and not an object -- and the two shapes
        // would then be equally good answers to it, with no grouping able to say otherwise.
        var world = new Binding(World(bound: false) with { Segmented = true }, seed: 4);

        for (var i = 0; i < 500; i++)
        {
            var scene = world.Draw();

            Assert.NotNull(scene.Groups);
            Assert.Equal(scene.Asked, scene.Groups[scene.Question]);
            Assert.Equal(Binding.Asks, scene.Question.Modality);

            // And it is derived from that object's colour rather than from its shape, or the
            // question would be the answer written down.
            Assert.Equal(scene.Colours[scene.Asked], Binding.Concept(scene.Question));
        }
    }

    [Fact]
    public void The_two_arms_of_the_grouping_still_see_the_identical_input()
    {
        // The same proof one seam further out. Reading the grouping changes what a scope
        // MEANS and never what arrives, so the arm and its control are two runs of the
        // learner over one stream -- which is what the trap about an arm that changes a
        // code's value asks for and what the Monk comparison did not have.
        var told = new Binding(World(bound: false) with { Segmented = true }, seed: 1);
        var silent = new Binding(World(bound: false), seed: 1);

        for (var i = 0; i < 1_000; i++)
        {
            var one = told.Next();
            var other = silent.Next();

            Assert.Equal(one.Seen.Codes, other.Seen.Codes);
            Assert.Equal(one.Outcome, other.Outcome);

            Assert.NotNull(one.Seen.Things);
            Assert.Null(other.Seen.Things);
        }
    }

    /// <summary>
    /// <b>Two of a kind at once reach the brain as two things.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The check <see cref="Codes.IQuantizer{TObservation}.Bind"/>'s parts owe.</b> A
    /// moment's codes are a SET, so one red ball and two red balls are the identical set and
    /// nothing in it can say which was shown. The parts are where the difference lives, and
    /// this asserts that it survives the front end rather than being argued to.
    /// </para>
    /// <para>
    /// <b>And the shape it replaces went quiet</b>, reporting less the more of a kind it saw. The
    /// channel was a code-to-thing dictionary, which names one thing per code, so a code in
    /// two parts had no answer to give and was dropped — leaving a two-ball scene with no
    /// grouping at all. A one-ball scene reported one thing. That is the front end going
    /// quiet exactly where multiplicity is, and it is asserted here as the identity of the
    /// codes beside the difference in the parts.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_moment_holding_two_of_a_kind_says_two_things_over_the_same_codes()
    {
        Code red = Kinds.Named(Binding.Colour, "red");
        Code ball = Kinds.Named(Binding.Shape, "ball");

        var one = Coded.From([Grouped.Of([red, ball])]);
        var two = Coded.From([Grouped.Of([red, ball]), Grouped.Of([red, ball])]);

        var front = new Passthrough<Coded>(seen => seen);

        // The set cannot tell them apart, which is the whole reason the parts have to.
        Assert.Equal(new HashSet<Code>(one.Codes), new HashSet<Code>(two.Codes));

        var told = ((IQuantizer<Coded>)front).Bind(one);
        var twice = ((IQuantizer<Coded>)front).Bind(two);

        Assert.NotNull(told);
        Assert.NotNull(twice);

        Assert.Single(told);
        Assert.Equal(2, twice.Count);

        // And both things hold the same codes, which is what makes them two of a KIND rather
        // than two things. A front end that had to make them differ to say there were two
        // would be minting an instance into a code, and then nothing would recur.
        Assert.Equal(twice[0], twice[1]);
    }

    /// <summary>
    /// <b>A scope about no ONE thing does not fire</b>, however many things share its codes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The reader's half of the same mechanism.</b> <see cref="Spanning.Thing"/> asks
    /// whether some one thing accounts for a scope, which is an intersection over the things
    /// each code is in. Where every code was in exactly one thing that was the same test as
    /// equality; where a code is in two, equality had nothing to compare.
    /// </para>
    /// <para>
    /// <b>And the failure it fixes</b> is the dial switching itself off. The flattening
    /// dropped a shared code, so a scope naming one of those and a code from a THIRD thing
    /// saw one grouped code, read as being about that third thing, and fired. The more of a
    /// kind a scene held the less of it was grouped, so <see cref="Spanning.Thing"/>
    /// degenerated towards <see cref="Spanning.Anything"/> exactly where it was needed.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_scope_spanning_a_shared_thing_and_another_does_not_fire()
    {
        Code red = Kinds.Named(Binding.Colour, "red");
        Code ball = Kinds.Named(Binding.Shape, "ball");
        Code blue = Kinds.Named(Binding.Colour, "blue");
        Code box = Kinds.Named(Binding.Shape, "box");

        // Two red balls and a blue box: `red` and `ball` are each in two things, `blue` and
        // `box` in one.
        IReadOnlyList<Grouped> things =
        [
            Grouped.Of([red, ball]),
            Grouped.Of([red, ball]),
            Grouped.Of([blue, box]),
        ];

        var answer = Brain.Says(0);
        var moment = new HashSet<Code> { red, ball, blue, box };

        var held = new Population(
            new CommittingSettings { Spanning = Spanning.Thing }, seed: 1);

        var kind = new Commitment([red, ball], answer);
        var other = new Commitment([blue, box], answer);
        var across = new Commitment([red, blue], answer);

        foreach (var one in new[] { kind, other, across }) Assert.True(held.Add(one));

        var firing = held.Firing(moment, things).Select(one => one.Identity).ToHashSet();

        // Each is about one thing -- the kind about either of two, which is what a shared
        // code buys and what the intersection reads.
        Assert.Contains(kind.Identity, firing);
        Assert.Contains(other.Identity, firing);

        // And this one is about no thing at all. Under the flattening it was about the box.
        Assert.DoesNotContain(across.Identity, firing);

        // The control still sees all three, so what moved is what a scope MEANS and not what
        // arrived -- the same separation the arms above are read under.
        var ignoring = new Population(
            new CommittingSettings { Spanning = Spanning.Anything }, seed: 1);

        foreach (var one in new[] { kind, other, across })
            Assert.True(ignoring.Add(new Commitment(one.Scope, answer)));

        Assert.Equal(3, ignoring.Firing(moment, things).Length);
    }

    // ---- what it measures ---------------------------------------------------

    /// <summary>How many concepts, and how many codes each attribute of one shows.</summary>
    /// <remarks>
    /// <b>Small, because the question is representability and not scale.</b> Every
    /// (colour, shape) pair needs its own rule and each attribute shows three codes, so the
    /// rules a perfect population would hold is concepts squared times nine — 576 at four,
    /// which a capacity of four thousand holds with room to spare.
    /// </remarks>
    private const int Concepts = 4;

    /// <summary>One arm of the grid, run whole.</summary>
    /// <param name="Arm">What is being run.</param>
    /// <param name="Tally">What the run scored on the stream.</param>
    /// <param name="Unseen">What it scored on the scenes it was never shown.</param>
    /// <param name="Held">How many commitments are resident at the end.</param>
    /// <param name="Sound">Resident rules that are right about this world whatever it shows.</param>
    /// <param name="Found">How many DISTINCT ones of those, which is what coverage means.</param>
    /// <param name="Lengths">How many residents there are at each scope length.</param>
    private sealed record Run(
        string Arm,
        Tally Tally,
        Examined Unseen,
        int Held,
        int Sound,
        int Found,
        SortedDictionary<int, int> Lengths)
    {
        /// <summary>What share of the answered withheld scenes were right.</summary>
        public double Withheld =>
            Unseen.Answered == 0 ? 0.0 : Unseen.Right / (double)Unseen.Answered;
    }

    /// <summary>
    /// What reading the grouping is worth, against the control that has it and ignores it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The kill line, written before the run.</b> <see cref="Spanning.Thing"/> must beat
    /// its control on the withheld set by more than the seed spread. If it does not, the arm
    /// is deleted and <see cref="Codes.IQuantizer{TObservation}.Bind"/> goes with it — the
    /// channel has no other proposed reader, and the one that was built is refuted.
    /// </para>
    /// <para>
    /// <b>The control is the same codes with the mechanism off</b>, which is the sharpest
    /// form this comparison can take and the form the Monk comparison did not have. Nothing
    /// the world emits moves between the two arms:
    /// <see cref="The_two_arms_of_the_grouping_still_see_the_identical_input"/> asserts it.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_reading_the_grouping_is_worth_where_the_codes_cannot_say()
    {
        // EIGHT, because the shipped pairing's lead is the narrowest thing this grid
        // reports and four seeds cannot separate two from three standard errors. The arm
        // against its control at the ungated end is wide enough that seeds do not decide it.
        int[] seeds = [1, 2, 3, 4, 5, 6, 7, 8];

        var arms =
            new (string Arm, bool Bound, bool Segmented, Spanning Spanning, Surprising Gate)[]
            {
                ("bound, ungrouped     ", true, false, Spanning.Anything, Surprising.Unaccounted),
                ("unbound, ungrouped   ", false, false, Spanning.Anything, Surprising.Unaccounted),
                ("unbound, ignored     ", false, true, Spanning.Anything, Surprising.Unaccounted),
                ("unbound, read        ", false, true, Spanning.Thing, Surprising.Unaccounted),
                ("unbound, ignored, any", false, true, Spanning.Anything, Surprising.AnyFailure),
                ("unbound, read, any   ", false, true, Spanning.Thing, Surprising.AnyFailure),
            };

        var measured = new Dictionary<string, Measured>();
        var taken = new Dictionary<string, List<Run>>();

        foreach (var (arm, bound, segmented, spanning, gate) in arms)
        {
            var runs = seeds
                .Select(seed => Learnt(arm, bound, segmented, spanning, gate, seed))
                .ToList();

            taken[arm] = runs;
            measured[arm] = new Measured
            {
                Arm = arm,
                Values = [.. runs.Select(one => one.Withheld)],
            };

            output.WriteLine(
                $"{arm} | withheld {measured[arm]} | drawn "
                + $"{runs.Average(one => one.Tally.Recent):F3} | held "
                + $"{runs.Average(one => (double)one.Held):F0} | answered "
                + $"{runs.Average(one => (double)one.Unseen.Answered):F0} of "
                + $"{runs.Average(one => (double)one.Unseen.Asked):F0} | sound "
                + $"{runs.Average(one => (double)one.Sound):F0}, {runs.Average(one => (double)one.Found):F0} "
                + $"of {Concepts * Concepts * 9} the world has");

            output.WriteLine(
                $"{arm} |   minted {runs.Average(one => (double)one.Tally.Minted):F0}, "
                + $"repaired {runs.Average(one => (double)one.Tally.Repaired):F0}, "
                + $"lengths {string.Join(" ", runs[0].Lengths.Select(one => $"{one.Key}:{one.Value}"))}");
        }

        output.WriteLine($"informed chance is {Binding.Chance:F3}");

        // The grouping changes nothing until it is read, asserted rather than argued. These
        // two runs differ in one thing: the front end reports which codes are one object.
        // Bit-identical scores are what says the control is a control, and that the arm
        // below is a mechanism rather than a second stream.
        Assert.Equal(
            measured["unbound, ungrouped   "].Mean,
            measured["unbound, ignored     "].Mean);

        // The reading. The arm against the control that has the grouping and ignores it,
        // both under the same genesis gate, on a world whose codes cannot say which shape
        // belongs to which colour.
        var read = measured["unbound, read, any   "];
        var control = measured["unbound, ignored, any"];
        var spread = Math.Sqrt((read.StdErr * read.StdErr) + (control.StdErr * control.StdErr));

        Assert.True(read.Mean - control.Mean > 5 * spread,
            $"{read} against {control} is under five pooled standard errors ({spread:F4}). "
            + "This is the kill line `Spanning` was built with: the grouping must beat the "
            + "control that has it and does not read it, or the dial goes and "
            + "`IQuantizer.Bind` goes with it -- the channel has no other proposed reader "
            + "and the one that was built is refuted.");

        // And it is not a lucky vote: the population holds every rule the world has. A score
        // this high with a fraction of them would be memorising the drawn stream and being
        // asked easy questions, which is what the withheld set exists to catch.
        var whole = Concepts * Concepts * 9;
        var worst = taken["unbound, read, any   "].Min(one => one.Found);

        output.WriteLine($"the worst seed of the arm holds {worst} of {whole}");

        Assert.True(worst > 0.95 * whole,
            $"the worst seed holds {worst} of the {whole} rules this world has, which is "
            + "under nineteen in twenty. A score this high on a fraction of them would be "
            + "the drawn stream memorised and the withheld set asked easy questions.");

        // What the genesis gate costs, which is the other half of the reading and was not
        // what this grid was built to find. `Surprising.Unaccounted` stops genesis whenever
        // anything that fired expects what arrived, and on a coin-flip world a lucky
        // advocate does that half the time -- so the proposals dry up at eighty and the
        // mechanism is starved rather than refuted.
        var gated = measured["unbound, read        "];

        Assert.True(read.Mean - gated.Mean > 0.3,
            $"{read} against {gated}: the gate used to cost this world most of its score, and "
            + "a gap this small means the gate stopped mattering. Take the reading again.");

        // And what the shipped pairing is worth, which is a much smaller number and is here
        // so that nobody reads the figure above as the default's score. `Surprising.Unaccounted`
        // is what ships, and under it the arm leads its control by four standard errors rather
        // than forty. The dial ships on that and the rest is conditional on a gate that does
        // not.
        //
        // Four seeds read this at 2.1 and eight read it at 4.4, which is the reason the seed
        // count went up rather than an argument for the number. A small sample hides a real
        // effect as readily as it invents one, and this grid's other arms are wide enough
        // that nobody would have looked.
        var shipped = measured["unbound, read        "];
        var beside = measured["unbound, ignored     "];
        var narrow = Math.Sqrt(
            (shipped.StdErr * shipped.StdErr) + (beside.StdErr * beside.StdErr));

        output.WriteLine(
            $"at the shipped gate: {shipped.Mean - beside.Mean:F4} ahead, "
            + $"{(shipped.Mean - beside.Mean) / narrow:F1} pooled standard errors");

        Assert.True(shipped.Mean > beside.Mean,
            $"{shipped} does not lead {beside}, so the dial's shipped setting is behind its "
            + "own control and the reading that put it there was taken at a gate nothing "
            + "uses. Take the pairing back to John.");

        // And ungated genesis buys nothing on its own, which is what makes the line above an
        // interaction rather than two mechanisms added up. The control mints far more and
        // holds more, and it answers at chance.
        Assert.True(control.Mean < Binding.Chance + (3 * control.StdErr),
            $"{control} is above the {Binding.Chance:F3} bar, so ungated genesis is composing "
            + "something on its own and the grouping is not what lifted the arm.");
    }

    /// <param name="arm">What to call it.</param>
    /// <param name="bound">Whether a colour keeps its shape for the life of the world.</param>
    /// <param name="segmented">Whether the front end says which codes are one object.</param>
    /// <param name="spanning">Whether the learner reads the grouping.</param>
    /// <param name="gate">What it takes for genesis to run at all.</param>
    /// <param name="seed">The seed for both the world and the brain.</param>
    private static Run Learnt(
        string arm,
        bool bound,
        bool segmented,
        Spanning spanning,
        Surprising gate,
        int seed)
    {
        var world = new Binding(
            Fixture.Binding(bound: bound, concepts: Concepts, codes: 3, segmented: segmented)
                with { Withheld = 200 },
            seed);

        var brain = new Brain(
            new CommittingSettings
            {
                Capacity = 4000,
                Spanning = spanning,
                Surprising = gate,
            },
            seed);

        var tally = new Bench(
                new Watching<Coded>(world, new Passthrough<Coded>(one => one)), brain)
            .Run(rounds: 20_000, sweep: 1000, target: 0.99, window: 2000);

        Assert.NotNull(tally.Unseen);

        // What a perfect population holds, enumerated rather than guessed, and it is what
        // the grouping makes sayable at all. The question and a shape, both in the asked
        // object's part: under `Spanning.Thing` that fires only where the shape is the
        // asked object's, so the answer is that shape's concept whatever else is in the
        // scene. Every colour code and slot against every shape code and slot is nine per
        // pair of concepts, so the world has `Concepts` squared times nine.
        //
        // A colour code beside them is sound and redundant -- the question already names
        // that colour, and it is in the same part by construction -- so the two lengths are
        // one rule and are counted as one. Counting them apart would report coverage twice
        // for the deeper copy of a rule the shorter one already states.
        var sound = 0;
        var found = new HashSet<(int, int, int, int)>();

        // And it is only sound under the rule, which is why the control is not counted at
        // all rather than counted and read across. The same scope under `Spanning.Anything`
        // fires on both of the scene's shapes and is right half the time, so one column
        // would mean two things -- and a statistic whose halves count different events is
        // this repo's own trap.
        foreach (var one in spanning is Spanning.Thing ? brain.Held.All : [])
        {
            var asks = one.Scope.FirstOrDefault(code => code.Modality == Binding.Asks);
            var shape = one.Scope.FirstOrDefault(code => code.Modality == Binding.Shape);

            if (asks.Modality != Binding.Asks
                || shape.Modality != Binding.Shape
                || one.Expects != Brain.Says(Binding.Concept(shape))) continue;

            // And nothing else in it, or the rule is narrower than the world and says less
            // than this count would claim for it.
            if (one.Scope.Any(code =>
                    code != asks && code != shape && code.Value != asks.Value)) continue;

            sound++;
            found.Add((
                Binding.Concept(asks), (int)(asks.Value % 1000),
                Binding.Concept(shape), (int)(shape.Value % 1000)));
        }

        var lengths = new SortedDictionary<int, int>();

        foreach (var one in brain.Held.All)
            lengths[one.Scope.Length] = lengths.GetValueOrDefault(one.Scope.Length) + 1;

        return new Run(
            arm, tally, tally.Unseen, brain.Held.All.Count, sound, found.Count, lengths);
    }
}
