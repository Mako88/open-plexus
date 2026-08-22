using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What rung four's proposal is worth once it runs inside a machine — <b>fork 102</b>, and
/// the first reading of an operator <see cref="UnifyingYieldTests"/> priced from outside.
/// </summary>
/// <remarks>
/// <para>
/// That file scored proposals against the siblings they would replace and found the gate:
/// a hole whose covered values are alternatives is no worse in 38 cases of 38, where a hole
/// punched anywhere else is no worse in 7%. This one puts the operator in the sweep, gates it
/// on the vocabulary a front end derived, and asks what a run is worth with it against the
/// same run without.
/// </para>
/// <para>
/// The control is the derivation being off rather than a dial, which is what
/// <see cref="Population.Generalise"/> being inert without <see cref="Population.Sorts"/>
/// buys. Both arms run the same code on the same rounds; one of them has a vocabulary to
/// gate with and the other has nothing, so nothing is proposed.
/// </para>
/// <para>
/// <b>The operator fires and buys nothing here.</b> It adds five to twenty rules with a hole
/// over eight seeds, none of them sound, and the world's own instruments do not move: the
/// same 15 or 16 of its rules are found, the sound count is level on seven seeds of eight,
/// and the trailing accuracy is level or a fraction down on all eight.
/// </para>
/// <para>
/// <b>And the reason is that one hole is a DROP.</b> An entry naming a variable once is
/// satisfied by any moment holding a code of that kind, so a rule with one is the same rule
/// with that condition removed — which is <c>Widening</c>, already refuted in three shapes.
/// What makes rung four a rung is a hole that REPEATS: <i>whichever word was asked about, and
/// that same word was told</i>.
/// </para>
/// <para>
/// <b>So the second reading follows the repeated hole</b>, link by link, on the world where
/// it means something. It wants a front end keeping the halves apart, a scope that
/// came to say one value twice, a sibling group over that shape, and a gate that admits it.
/// Each link is counted, so the reading names the empty one rather than reporting a nought.
/// </para>
/// </remarks>
public sealed class GeneralisingTests(ITestOutputHelper output)
{
    /// <summary>How many rounds each arm is learnt over.</summary>
    private const long Rounds = 4000;

    /// <summary>How many seeds each arm is read over.</summary>
    private const int Seeds = 8;

    /// <summary>What one arm of one seed left behind.</summary>
    /// <param name="Recent">The trailing accuracy.</param>
    /// <param name="Held">How many commitments are resident.</param>
    /// <param name="Sound">How many of them are true of the world.</param>
    /// <param name="Found">How many of the world's own rules were reached.</param>
    /// <param name="Holed">How many residents name a variable.</param>
    /// <param name="Sorts">How many categories the front end derived.</param>
    private readonly record struct Ran(
        double Recent, int Held, int Sound, int Found, int Holed, int Sorts);

    /// <summary>One arm of one seed.</summary>
    /// <param name="deriving">Whether the front end fills a vocabulary the gate can read.</param>
    /// <param name="seed">Which run.</param>
    /// <remarks>
    /// The derivation and never the FOLD, which is what keeps this a comparison.
    /// <see cref="Sorted{TObservation}"/> would put a category code in every moment and change
    /// what every rule in the run is written over; <see cref="Deriving{TObservation}"/> fills
    /// the vocabulary and leaves the moments exactly as the control sees them, so the only
    /// difference between the arms is whether the gate has a table to read.
    /// </remarks>
    private static Ran Run(bool deriving, int seed)
    {
        var brain = new Brain(new CommittingSettings { Capacity = 2000 }, seed);
        var world = new Multiplexer(new MultiplexerSettings { Address = 3 }, seed);

        var sorts = new Categories([]);

        IQuantizer<IReadOnlyList<int>> front = new Bits(Multiplexer.Bit);

        if (deriving)
        {
            front = new Deriving<IReadOnlyList<int>>(
                front, sorts, Counting.Company, Meeting.Never, floor: 20, every: 1000);

            brain.Held.Sorts = sorts;
        }

        var tally = new Bench(
            new Watching<IReadOnlyList<int>>(world, front), brain, sound: world.Sound)
            .Run(Rounds, sweep: 1000, target: 0.9, window: 2000);

        var graded = Learned.Grade(
            tally, world.Truths(), brain.Held, brain.Dials.Floor,
            world.Checkable, world.Sound, detailed: true);

        return new Ran(
            tally.Recent, tally.Resident, graded.Sound, graded.Found,
            brain.Held.All.Count(one => one.Varies), sorts.Count);
    }

    [Fact]
    public void What_a_rule_with_one_hole_in_it_is_worth_on_the_world_that_can_gate_one()
    {
        var arms = new Dictionary<bool, List<Ran>> { [false] = [], [true] = [] };

        foreach (var deriving in new[] { false, true })
            foreach (var seed in Enumerable.Range(1, Seeds))
            {
                var ran = Run(deriving, seed);

                arms[deriving].Add(ran);

                output.WriteLine(
                    $"{(deriving ? "gated " : "control"),-7} seed {seed} | recent {ran.Recent:F3} "
                    + $"| held {ran.Held,4} | sound {ran.Sound,3} | found {ran.Found,3} "
                    + $"| categories {ran.Sorts,2} | holed {ran.Holed,3}");
            }

        foreach (var (deriving, ran) in arms)
            output.WriteLine(
                $"{(deriving ? "gated " : "control"),-7} mean  | recent "
                + $"{ran.Average(one => one.Recent):F3} | held {ran.Average(one => one.Held):F0} "
                + $"| sound {ran.Average(one => one.Sound):F1} "
                + $"| found {ran.Average(one => one.Found):F1} "
                + $"| holed {ran.Average(one => one.Holed):F1}");

        // The operator RUNS, which is the first thing to hold down and what every other
        // reading here rests on. A rung nothing reaches is measured by whatever called it
        // directly, and that number is about the call.
        Assert.All(arms[true], one => Assert.True(one.Sorts > 0,
            "the front end derived no categories, so the gate had nothing to read and this "
            + "file is measuring an empty control against an empty control"));

        Assert.True(arms[true].Sum(one => one.Holed) > 0,
            $"no rule with a hole in it was ever added over {Seeds} seeds, so rung four is "
            + "wired and unable to fire");

        Assert.All(arms[false], one => Assert.Equal(0, one.Holed));

        // And the instrument says so when it cannot see. `Multiplexer.Sound` refuses a
        // modality it does not know, and a variable entry is one -- so before it was taught to
        // pass over one, every rule this rung built was unsound by construction and the column
        // read like a verdict about the learner. This is the check that it can see them: a
        // holed scope grades rather than throwing or counting as unchecked.
        Assert.True(arms[true].Sum(one => one.Sound) > 0,
            "no gated arm holds a sound rule at all, so the answer key is not reading the "
            + "population's alphabet and every column here is about the key");

        // The finding, held down in the direction it came out. The rung buys nothing on this
        // world: the same rules of the world are found and the accuracy does not rise. If
        // this flips it is owed a re-take rather than a deletion -- a hole that pays would be
        // the first evidence that a single-position variable is worth more than the drop it
        // is equivalent to.
        Assert.True(
            arms[true].Average(one => one.Found) <= arms[false].Average(one => one.Found),
            $"the gated arm found {arms[true].Average(one => one.Found):F1} of the world's "
            + $"rules against {arms[false].Average(one => one.Found):F1}, so a rule with one "
            + "hole in it now reaches something the propositional learner does not and this "
            + "file's account of why is wrong");

        Assert.True(
            arms[true].Average(one => one.Recent) <= arms[false].Average(one => one.Recent),
            $"the gated arm scores {arms[true].Average(one => one.Recent):F3} against "
            + $"{arms[false].Average(one => one.Recent):F3}, so the rung pays here and the "
            + "account above -- that one hole is the drop `Widening` already refuted -- is "
            + "what needs re-reading");
    }

    /// <summary>What one arm of the join reading left behind.</summary>
    /// <param name="Recent">The trailing accuracy.</param>
    /// <param name="Held">How many commitments are resident.</param>
    /// <param name="Twice">How many of them say one value under two modalities.</param>
    /// <param name="Groups">How many sibling groups the residents offer.</param>
    /// <param name="Repeated">How many of those groups would give a hole that repeats.</param>
    /// <param name="Admitted">How many groups the vocabulary admits.</param>
    /// <param name="Joined">How many of the admitted ones repeat.</param>
    /// <param name="Resident">How many residents name a variable in two places.</param>
    /// <param name="Fired">How often those residents fired and were answered.</param>
    /// <param name="Sorts">How many categories were derived.</param>
    private readonly record struct Chained(
        double Recent, int Held, int Twice, int Groups, int Repeated, int Admitted,
        int Joined, int Resident, long Fired, int Sorts);

    /// <summary>One arm of the join reading.</summary>
    /// <param name="joining">How the question and the story are read.</param>
    private static Chained Join(Joining joining)
    {
        var brain = new Brain(new CommittingSettings { Capacity = 2000 }, seed: 1);

        // The newest statement alone, because the join has to be able to FAIL. On this task
        // the question always names somebody the whole story mentions, so over the whole story
        // *the word asked about was told* is true of every question ever asked -- which reads
        // as a variable binding for free and is only the story being wide.
        var world = new Recalled(new RecalledSettings
        {
            Corpus = Tree.Babi(), Task = 1, Span = 1, Withheld = 40,
            Predicting = Predicting.Asked,
        });

        var sorts = new Categories([]);

        var front = new Deriving<Coded>(
            new Joined(joining), sorts, Counting.Company, Meeting.Rarely,
            floor: 20, every: 2000);

        brain.Held.Sorts = sorts;

        var tally = new Bench(new Watching<Coded>(world, front), brain)
            .Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);

        var all = brain.Held.All;

        var groups = Generalising.Siblings(all);
        var admitted = groups.Where(one => Generalising.Admits(one, sorts)).ToList();

        var joins = all.Where(one => one.Scope.Count(Unifying.Names) > 1).ToList();

        return new Chained(
            tally.Recent,
            all.Count,
            all.Count(one => one.Scope.GroupBy(code => code.Value).Any(group => group.Count() > 1)),
            groups.Count,
            groups.Count(one => one.Holes.Count > 1),
            admitted.Count,
            admitted.Count(one => one.Holes.Count > 1),
            joins.Count,
            joins.Sum(one => one.Fired),
            sorts.Count);
    }

    /// <summary>
    /// How far the join gets on the world it was designed for, link by link.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Joining.Parted"/> is what the whole chain rests on.</b> Every other arm
    /// unions the question's words into the story's bag, so a moment holds each word once and
    /// no scope can ever say one value twice — the first link is nought by construction and
    /// nothing after it can be read. This arm says a question word in its own modality, which
    /// is the two places a variable can stand in.
    /// </para>
    /// <para>
    /// <b>It reaches the proposal and stops at the gate.</b> The parted arm holds 74 scopes
    /// saying one value twice and offers ten sibling groups over that shape, and the
    /// vocabulary admits none of them: the four categories it derives do not cover the values
    /// the hole would stand for. Whether the gate is even the right one here is open — it was
    /// measured on single holes, where a hole is a DROP and the covered values being
    /// alternatives is what says the drop is safe. A repeated hole constrains rather than
    /// drops, so what it needs admitting on is a different question that owes its own reading.
    /// </para>
    /// <para>
    /// <b>And the first version of this reading was not reproducible</b>, which is worth
    /// keeping here because it looked exactly like a chaotic learner. Two runs of one seed
    /// gave 98 admitted proposals and 114, and the number that mattered went 4 and 0 — the
    /// shuffle drew its null from <c>HashCode.Combine</c>, which the runtime seeds per
    /// process. <see cref="DeterminismTests.No_code_in_the_library_derives_a_value_from_a_randomised_hash"/>
    /// is what stops it happening again.
    /// </para>
    /// </remarks>
    [Fact]
    public void How_far_a_hole_that_repeats_gets_on_the_world_it_was_designed_for()
    {
        var read = new Dictionary<Joining, Chained>();

        foreach (var joining in new[] { Joining.Bagged, Joining.Parted })
        {
            read[joining] = Join(joining);

            var one = read[joining];

            output.WriteLine(
                $"{joining,-7}| recent {one.Recent:F3} | held {one.Held,4} "
                + $"| saying a value twice {one.Twice,4} | sibling groups {one.Groups,5} "
                + $"| repeated {one.Repeated,4} | admitted {one.Admitted,4} "
                + $"| joined {one.Joined,3} | resident joins {one.Resident,3} "
                + $"| fired {one.Fired,5} | categories {one.Sorts,2}");
        }

        // The front end's own contribution, and it is the link nothing else could supply. A
        // bag holds each word once however often it was said, so a scope over one cannot name
        // a value twice at all -- that is not a small number, it is nought by construction.
        Assert.Equal(0, read[Joining.Bagged].Twice);

        Assert.True(read[Joining.Parted].Twice > 0,
            "keeping the halves apart produced no scope saying one value twice, so the front "
            + "end is not supplying the two places a variable stands in and every link after "
            + "this one is unreadable");

        // And the proposer reaches the shape, which is the link that says anti-unification
        // over a VALUE rather than a position was the right generalisation.
        Assert.True(read[Joining.Parted].Repeated > 0,
            "no sibling group would give a hole that repeats, so the residents hold the shape "
            + "and the proposer does not reach it");

        Assert.Equal(0, read[Joining.Bagged].Repeated);

        // And the gate is where it stops, held down so the day it moves is visible. The
        // vocabulary derives four categories over this stream and none of them covers the
        // values a join would stand for, so ten proposals of the right shape are refused --
        // which is an open question about the GATE rather than a fault in any link above it.
        // It was measured on holes that DROP, where the covered values being alternatives is
        // what says the drop is safe; a repeated hole constrains instead, and what it should
        // be admitted on owes its own reading.
        Assert.Equal(0, read[Joining.Parted].Joined);
        Assert.Equal(0, read[Joining.Bagged].Joined);

        Assert.Equal(0, read[Joining.Parted].Resident);
        Assert.Equal(0, read[Joining.Parted].Fired);
    }
}
