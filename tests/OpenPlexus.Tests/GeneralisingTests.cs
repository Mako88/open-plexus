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
/// <b>And subsumption takes none of them</b>, which is what the rung costs. A holed parent
/// is added beside the siblings it covers and every one of them is still resident at the
/// end — 31.5 standing beside 9.6 parents, on all eight seeds. The rule that keeps the
/// general one where both are equally accurate cannot reach a parent with no record yet,
/// because a fresh commitment starts blind and re-earns its statistics.
/// </para>
/// <para>
/// <b>And it buys nothing because one hole is a DROP.</b> An entry naming a variable once is
/// satisfied by any moment holding a code of that kind, so a rule with one is the same rule
/// with that condition removed — which is <c>Widening</c>, already refuted in three shapes.
/// What makes rung four a rung is a hole that REPEATS: <i>whichever word was asked about, and
/// that same word was told</i>.
/// </para>
/// <para>
/// <b>So the second reading follows the repeated hole</b>, link by link, on the world where
/// it means something. It wants a front end keeping the halves apart, a scope that came to say
/// one value twice, a sibling group over that shape, a gate that admits it, and a resident
/// that fires. Each link is counted, so the reading names the empty one rather than reporting
/// a nought — which is how the two that were empty got found.
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
    /// <param name="Undisplaced">
    /// How many of the siblings a holed parent covers are still resident beside it.
    /// <b>What subsumption did not take</b>, which is the half of the add-only rule nothing
    /// had read. A parent that covers its siblings and does not displace them is a second
    /// copy of what the population already held.
    /// </param>
    private readonly record struct Ran(
        double Recent, int Held, int Sound, int Found, int Holed, int Sorts, int Undisplaced);

    /// <summary>
    /// Every resident a holed parent generalises that is still resident beside it.
    /// </summary>
    /// <param name="held">The population a run finished with.</param>
    /// <remarks>
    /// <para>
    /// <b>By the constants rather than by position</b>, which is forced and not a shortcut. A
    /// scope is sorted canonically and a variable entry rides its own modality, so the hole
    /// sits somewhere the filled value does not — and comparing position by position would
    /// call every sibling a stranger. A sibling is a resident of the same length expecting
    /// the same thing and holding every constant the parent holds.
    /// </para>
    /// <para>
    /// <b>A repeated hole is covered by the same test.</b> Two variable entries leave two
    /// positions free, and a sibling filling both with one value still holds every constant
    /// and still has the same length.
    /// </para>
    /// </remarks>
    private static int Undisplaced(Population held)
    {
        var all = held.All.ToList();

        var standing = 0;

        foreach (var parent in all.Where(one => one.Varies))
        {
            var constants = parent.Scope.Where(code => !Unifying.Names(code)).ToHashSet();

            standing += all.Count(one =>
                !one.Varies
                && one.Scope.Length == parent.Scope.Length
                && one.Expects == parent.Expects
                && constants.All(one.Scope.Contains));
        }

        return standing;
    }

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
            brain.Held.All.Count(one => one.Varies), sorts.Count, Undisplaced(brain.Held));
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
                    + $"| categories {ran.Sorts,2} | holed {ran.Holed,3} "
                    + $"| siblings kept {ran.Undisplaced,4}");
            }

        foreach (var (deriving, ran) in arms)
            output.WriteLine(
                $"{(deriving ? "gated " : "control"),-7} mean  | recent "
                + $"{ran.Average(one => one.Recent):F3} | held {ran.Average(one => one.Held):F0} "
                + $"| sound {ran.Average(one => one.Sound):F1} "
                + $"| found {ran.Average(one => one.Found):F1} "
                + $"| holed {ran.Average(one => one.Holed):F1} "
                + $"| siblings kept {ran.Average(one => one.Undisplaced):F1}");

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
    /// <param name="Apart">
    /// How many of the repeated groups cover values that never met in a moment. <b>The gate's
    /// own definition asked of the stream</b> rather than of the vocabulary, which is what
    /// says whether the refusal is the GATE or the derivation that fills it.
    /// </param>
    /// <param name="Sorts">How many categories were derived.</param>
    private readonly record struct Chained(
        double Recent, int Held, int Twice, int Groups, int Repeated, int Admitted,
        int Joined, int Resident, long Fired, int Apart, int Sorts);

    /// <summary>A front end that keeps every moment it emitted.</summary>
    /// <param name="inner">The translation being watched.</param>
    /// <remarks>
    /// <b>Because exclusivity is a fact about the moments</b> and the population holds no such
    /// thing. <see cref="Deriving{TObservation}"/> counts them privately and gives back only
    /// what it grouped, so asking whether two codes ever met wants the stream itself.
    /// </remarks>
    private sealed class Kept(IQuantizer<Coded> inner) : IQuantizer<Coded>
    {
        /// <summary>Every moment, in the order it was emitted.</summary>
        public List<IReadOnlySet<Code>> Moments { get; } = [];

        /// <inheritdoc/>
        public byte Modality => inner.Modality;

        /// <inheritdoc/>
        public IReadOnlyCollection<Code> Codify(Coded observation)
        {
            var codes = inner.Codify(observation);

            Moments.Add(new HashSet<Code>(codes));

            return codes;
        }

        /// <inheritdoc/>
        public IReadOnlyList<Grouped>? Bind(Coded observation) => inner.Bind(observation);

        /// <inheritdoc/>
        public IReadOnlyDictionary<Code, int>? Order(Coded observation) => inner.Order(observation);

        /// <inheritdoc/>
        public IReadOnlySet<Code>? Fleeting(Coded observation) => inner.Fleeting(observation);

        /// <inheritdoc/>
        public IReadOnlySet<Code>? Forced(Coded observation) => inner.Forced(observation);
    }

    /// <summary>One arm of the join reading.</summary>
    /// <param name="joining">How the question and the story are read.</param>
    /// <param name="deriving">
    /// Whether the front end fills a vocabulary the gate can read. <b>The rung's own
    /// control</b>, since <see cref="Population.Generalise"/> proposes nothing without one —
    /// so the same front end, the same moments and the same rounds run with the rung on and
    /// off, and what parts them is rung four alone.
    /// </param>
    private static Chained Join(Joining joining, bool deriving = true)
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

        var kept = new Kept(new Joined(joining));

        IQuantizer<Coded> front = kept;

        if (deriving)
        {
            front = new Deriving<Coded>(
                kept, sorts, Counting.Weighed, Meeting.Rarely, floor: 20, every: 2000);

            brain.Held.Sorts = sorts;
        }

        var tally = new Bench(new Watching<Coded>(world, front), brain)
            .Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);

        var all = brain.Held.All;

        var groups = Generalising.Siblings(all);
        var admitted = groups.Where(one => Generalising.Admits(one, sorts)).ToList();

        var joins = all.Where(one => one.Scope.Count(Unifying.Names) > 1).ToList();

        var repeated = groups.Where(one => one.Holes.Count > 1).ToList();

        return new Chained(
            tally.Recent,
            all.Count,
            all.Count(one => one.Scope.GroupBy(code => code.Value).Any(group => group.Count() > 1)),
            groups.Count,
            repeated.Count,
            admitted.Count,
            admitted.Count(one => one.Holes.Count > 1),
            joins.Count,
            joins.Sum(one => one.Fired),
            repeated.Count(one => Apart(Generalising.Covered(one), kept.Moments)),
            sorts.Count);
    }

    /// <summary>Whether no moment ever held two of these codes at once.</summary>
    /// <param name="covered">The values a hole would stand for.</param>
    /// <param name="moments">Every moment the front end emitted.</param>
    private static bool Apart(
        IReadOnlyList<Code> covered, IReadOnlyList<IReadOnlySet<Code>> moments) =>
        covered.Count > 1
        && moments.All(one => covered.Count(one.Contains) <= 1);

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
    /// <b>Every link holds and the rung runs.</b> The parted arm holds 79 scopes saying one
    /// value twice, offers thirteen sibling groups over that shape, and every one of the
    /// thirteen covers values that NEVER met in any moment — the gate's own definition of a
    /// category, asked of the stream. The vocabulary admits eight, sixteen rules with a
    /// variable in two places are resident, and they were answered 7,097 times. That is the
    /// thing rungs one to three cannot say at all, said by a run.
    /// </para>
    /// <para>
    /// <b>Two things had to change first, and neither was the rung.</b> The
    /// derivation's bar had to be weighed: raw counts against a shuffled null get stricter
    /// without bound as evidence accumulates, so this vocabulary's grouping fell from twelve
    /// codes at five hundred moments to eight at twenty thousand. Weighing the same counts
    /// takes it the other way — two, two, seventeen, nineteen — and nineteen is what a
    /// hand-picked 0.9 reaches at every length. And the gate had to stop asking
    /// <see cref="Categories.Coarser"/>, which is a lookup built for the fold: a member keeps
    /// the first group to claim it, so two values one derived group holds together can report
    /// different coarser forms.
    /// </para>
    /// <para>
    /// <b>What the rung is worth here is nothing, at one seed.</b> 0.688 against a control's
    /// 0.687 for 320 more rules, the control being the same front end on the same rounds with
    /// no vocabulary for the gate to read. Being able to say a thing and being paid for saying
    /// it are two questions, and this file has now answered the first.
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
        var read = new Dictionary<string, Chained>(StringComparer.Ordinal);

        foreach (var (label, joining, deriving) in new (string, Joining, bool)[]
        {
            ("control", Joining.Parted, false),
            ("bagged ", Joining.Bagged, true),
            ("parted ", Joining.Parted, true),
        })
        {
            read[label] = Join(joining, deriving);

            var one = read[label];

            output.WriteLine(
                $"{label,-7}| recent {one.Recent:F3} | held {one.Held,4} "
                + $"| saying a value twice {one.Twice,4} | sibling groups {one.Groups,5} "
                + $"| repeated {one.Repeated,4} | admitted {one.Admitted,4} "
                + $"| joined {one.Joined,3} | resident joins {one.Resident,3} "
                + $"| fired {one.Fired,5} | of the repeated, never met {one.Apart,3} "
                + $"| categories {one.Sorts,2}");
        }

        // The front end's own contribution, and it is the link nothing else could supply. A
        // bag holds each word once however often it was said, so a scope over one cannot name
        // a value twice at all -- that is not a small number, it is nought by construction.
        Assert.Equal(0, read["bagged "].Twice);

        Assert.True(read["parted "].Twice > 0,
            "keeping the halves apart produced no scope saying one value twice, so the front "
            + "end is not supplying the two places a variable stands in and every link after "
            + "this one is unreadable");

        // And the proposer reaches the shape, which is the link that says anti-unification
        // over a VALUE rather than a position was the right generalisation.
        Assert.True(read["parted "].Repeated > 0,
            "no sibling group would give a hole that repeats, so the residents hold the shape "
            + "and the proposer does not reach it");

        Assert.Equal(0, read["bagged "].Repeated);

        // And the clause the gate is written to test is satisfied by every one of them, which
        // is what said the earlier refusal was the vocabulary rather than the rule.
        Assert.Equal(read["parted "].Repeated, read["parted "].Apart);

        // And the vocabulary admits them, which it did not until the bar was weighed and the
        // gate stopped asking a lookup built for the fold.
        Assert.True(read["parted "].Joined > 0,
            "the vocabulary admitted no repeated hole, so the categories it derives do not "
            + "cover the values the join stands for");

        Assert.Equal(0, read["bagged "].Joined);

        // And a rule with a variable in TWO places is resident and has been answered, which is
        // rung four doing the thing rungs one to three cannot do at all. The matcher is reached
        // on the ordinary path -- `Population.Firing` -- rather than by anything in this file.
        Assert.True(read["parted "].Resident > 0,
            "no rule with a variable in two places is resident, so the proposal was admitted "
            + "and never added -- the sweep runs after the last proposal, or culling takes them");

        Assert.True(read["parted "].Fired > 0,
            $"{read["parted "].Resident} rules with a variable in two places are resident and "
            + "none was ever answered, so the join is held and unable to fire");

        // And the control holds none of them, which is what makes the three rows a comparison.
        Assert.Equal(0, read["control"].Resident);
    }
}
