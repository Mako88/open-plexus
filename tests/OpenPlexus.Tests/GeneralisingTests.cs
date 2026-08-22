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
/// that same word was told</i>. Anti-unification over sibling groups cannot propose one,
/// because siblings differ in exactly one position by construction.
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
}
