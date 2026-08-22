using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What would ever PROPOSE a scope naming no argument — <b>the half fork 33's price did
/// not touch.</b> And the only thing still standing between rung four and a run.
/// </summary>
/// <remarks>
/// <para>
/// <b>A scope handed in by an experimenter is architecture.</b> Which is the one thing this
/// design may not do. The matcher is priced and cheap; what nothing here can do is
/// arrive at <i>whichever word</i> on its own. So the operator has to be summoned by
/// something the population already contains, and the obvious candidate is the one rung
/// five is already summoned by: REDUNDANCY.
/// </para>
/// <para>
/// <b>And it is anti-unification</b>, which is the dual of repair and not a new idea.
/// Where several commitments expect the same thing and differ in exactly one position of
/// their scope, the thing they share is a rule with a hole in it — <i>whoever was asked
/// about</i> rather than <i>mary</i>, <i>john</i>, <i>sandra</i> and <i>daniel</i>
/// separately. Repair walks specific-to-general nowhere; this is the step that would.
/// </para>
/// <para>
/// <b>So the question before the build</b> is whether there is anything to fire on. A
/// population holding no two commitments that differ in one position gives the operator
/// nothing, and rung four would then be blocked by the SHAPE of what is learnt rather
/// than by the operator being unbuilt — a different problem with a different fix. That is
/// a count, and this file takes it.
/// </para>
/// <para>
/// <b>And the trigger is abundant and mostly noise</b>, which is why the second test exists.
/// A hole punched on every sibling group is WORSE than the rules it replaces about nine
/// times in ten — it fires roughly twice as often and pays for it. So the operator needs a
/// gate exactly as genesis does, and how many siblings a hole covers is not one.
/// </para>
/// <para>
/// <b>What is, is fork 97 arriving where nobody put it:</b> the values a hole covers must be
/// ALTERNATIVES. A hole over codes that never co-occur is a variable; a hole over codes
/// that do is a coincidence of position. That is a fact about the MOMENTS rather than about
/// the rules, which is why nothing in the population could have supplied it — and it is the
/// same definition of a category John proposed for a different operator entirely.
/// </para>
/// </remarks>
public sealed class UnifyingYieldTests(ITestOutputHelper output)
{
    /// <summary>How many rounds each population is learnt over.</summary>
    private const long Rounds = 4000;

    /// <summary>
    /// How many independent populations the proposals are pooled over.
    /// </summary>
    /// <remarks>
    /// <b>Seeds rather than rounds, which is what the thin column needed.</b> The share of
    /// sibling groups whose covered values are ALTERNATIVES is under one in a hundred, so
    /// one population lands a handful of them and a rate over a handful carries nothing.
    /// Running longer would multiply the cases and leave every one of them a fact about the
    /// same learnt world; separate seeds multiply them and buy independence at the same
    /// price.
    /// </remarks>
    private const int Populations = 8;

    /// <summary>
    /// Sibling groups, and how big they get — <b>commitments expecting one thing</b> and
    /// differing in exactly one position of an otherwise identical scope.
    /// </summary>
    /// <param name="all">The population to read.</param>
    /// <remarks>
    /// <b>Keyed on the scope with one position blanked</b>, which is the hole itself. Two
    /// commitments land in the same group exactly when one variable would cover both, so
    /// a group of size N is a proposal to replace N rules with one — and the size is what
    /// says whether the replacement is worth making rather than a rename.
    /// </remarks>
    /// <param name="anchored">
    /// Whether to count only groups whose members keep a real code beside the hole.
    /// <b>The instrument check</b>, and it is the same one twice already caught today. A
    /// scope of ONE code with that code blanked becomes <i>whichever code of this kind,
    /// expect Y</i> — which fires on every moment holding any word at all and is a rule
    /// about nothing. Counting those as siblings would make the trigger look abundant on
    /// exactly the population where it has least to say.
    /// </param>
    private static List<int> Siblings(IReadOnlyList<Commitment> all, bool anchored) =>
    [
        .. Generalising.Siblings(all, anchored ? 2 : 1)
            .Select(group => group.Members.Count)
            .OrderDescending(),
    ];

    /// <summary>What a population offers the operator, printed as one row.</summary>
    /// <param name="what">Which world the population came from.</param>
    /// <param name="all">The population to read.</param>
    private int Report(string what, IReadOnlyList<Commitment> all)
    {
        var loose = Siblings(all, anchored: false);
        var real = Siblings(all, anchored: true);

        var deep = all.Count(one => one.Scope.Length > 1);

        output.WriteLine(
            $"{what,-12}| {all.Count,4} residents, {deep,4} of them past one code "
            + $"({all.Sum(one => one.Scope.Length) / (double)all.Count:F2} a scope)");

        output.WriteLine(
            $"{"",12}| any hole      {loose.Count,4} groups, largest "
            + $"{(loose.Count == 0 ? 0 : loose[0]),3}, {loose.Sum()} memberships");

        output.WriteLine(
            $"{"",12}| anchored hole {real.Count,4} groups, largest "
            + $"{(real.Count == 0 ? 0 : real[0]),3}, {real.Sum()} memberships "
            + $"— {(deep == 0 ? 0.0 : real.Sum() / (double)deep):P0} of the scopes that have one");

        return real.Count;
    }

    [Fact]
    public void Whether_a_learnt_population_holds_anything_a_variable_would_cover()
    {
        var text = new Brain(new CommittingSettings { Capacity = 2000 }, seed: 1);

        new Bench(
            new Watching<Coded>(
                new Recalled(new RecalledSettings
                {
                    Corpus = Tree.Babi(),
                    Task = 1,
                    Span = 0,
                    Withheld = 40,
                    Predicting = Predicting.Asked,
                }),
                new Joined(Joining.Bagged)),
            text)
            .Run(Rounds, sweep: 1000, target: 0.9, window: 2000);

        var onText = Report("babi 1", text.Held.All.ToList());

        // The multiplexer beside it, and it is the world the argument was always about.
        // *These positions are the address, whatever they say* is the concept rung five
        // provably cannot name — zero of 258 minted names grouped the address — and it is
        // a hole in a scope rather than a set of codes. If sibling groups exist anywhere
        // they exist here, and if they do not the trigger is what is missing.
        var bits = new Brain(new CommittingSettings { Capacity = 2000 }, seed: 1);

        new MultiplexerRun(new MultiplexerSettings { Address = 3 }, bits, seed: 1).Run(Rounds);

        var onBits = Report("multiplexer", bits.Held.All.ToList());

        // The one thing held down, and it is the trigger's existence rather than its size.
        // If no anchored group survives on either world then anti-unification has nothing
        // to fire on, rung four is blocked by the SHAPE of what is learnt rather than by an
        // unbuilt operator, and the fix is a different one entirely. The counts are the
        // finding; that they are not zero is what this asserts.
        Assert.True(onText > 0 || onBits > 0,
            "no population holds two commitments differing in one position of a scope with "
            + "context in it, so there is nothing a variable would cover and the operator "
            + "would never fire — rung four's block is the population, not the admission");
    }

    /// <summary>
    /// What the parent a hole proposes would be WORTH, against the siblings it replaces —
    /// <b>the reading that kills rung four cheaply if it dies.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A variable is a generalisation and every operator here narrows.</b> So the risk is
    /// named before the number: a scope with a hole in it fires far more often than any
    /// sibling it covers, and firing more can only cost accuracy. If the parent is
    /// worse than the siblings it replaces then repair would refuse it exactly as it
    /// refuses any other child that does not clear the bars, and rung four would mint
    /// nothing that survives however cheap the matcher is.
    /// </para>
    /// <para>
    /// <b>On the multiplexer</b>, because the concept is known and rung five provably missed
    /// it. <i>These positions are the address, whatever they say</i> is a hole in a
    /// scope and not a set of codes, and not one minted name in 258 grouped the address.
    /// So this is the same target approached by the operator whose shape actually fits it,
    /// which is what makes the comparison worth taking rather than a repeat.
    /// </para>
    /// <para>
    /// <b>And it is scored on moments the population never learnt on</b>, from a world at a
    /// different seed. A parent scored on what its siblings were fitted to would be asking
    /// whether a generalisation reproduces its own training set, which it does by
    /// construction.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_the_parent_a_hole_proposes_is_worth_against_the_siblings_it_replaces()
    {
        // Pooled over independent populations, which is the cheapest way to get the case
        // count up and also the right one. Running one population longer would grow the
        // proposals and leave every one of them a fact about the same learnt world;
        // separate seeds grow the count and buy independence with it. The alternatives
        // column is the reason — at one seed it lands on a handful of cases, and this repo
        // has already been caught reading a ratio over a few rounds.
        //
        // The groups are keyed per population and never across them. Two commitments from
        // two brains are not siblings however alike their scopes look; a hole is a proposal
        // to replace rules that one population actually holds at once.
        var proposals = new List<Generalising.Sibling>();

        foreach (var seed in Enumerable.Range(1, Populations))
        {
            var brain = new Brain(new CommittingSettings { Capacity = 2000 }, seed);

            new MultiplexerRun(new MultiplexerSettings { Address = 3 }, brain, seed).Run(Rounds);

            // The groups again, kept this time rather than counted, and taken from the
            // operator rather than rebuilt here -- so a change to what it considers a sibling
            // moves this reading with it instead of leaving the file scoring a shape the
            // machine stopped proposing. Anchored, because a hole with no context beside it
            // is a rule about nothing and would drag the parent's score down for a reason
            // that is this file's rather than the operator's.
            //
            // Kept per population and never across. Two commitments from two brains are not
            // siblings however alike their scopes look; a hole is a proposal to replace rules
            // that one population actually holds at once.
            proposals.AddRange(Generalising.Siblings(brain.Held.All));
        }

        IWorld<IReadOnlyList<int>> world =
            new Multiplexer(new MultiplexerSettings { Address = 3 }, seed: 99);

        var sensing = new Bits(Multiplexer.Bit);

        var moments = new List<(HashSet<Code> Moment, Code Arrived)>();

        for (var ask = 0; ask < 2000; ask++)
        {
            var turn = world.Next();

            moments.Add((
                new HashSet<Code>(sensing.Codify(turn.Seen)), Brain.Says(turn.Outcome!.Value)));
        }

        var indexed = moments.Select(one => Unifying.Index(one.Moment)).ToList();

        // And the same question asked of a vocabulary the machine could hold, which is what
        // turns the gate below from a reading into a mechanism. The exclusivity column is
        // computed here from the moments themselves, and nothing inside a population can do
        // that -- a commitment holds counts about its own firings and never about which codes
        // shared a moment. A `Categories` can, and `Deriving` is what fills one.
        //
        // Stricter than the column it is being read against, on purpose. A category demands
        // that the covered values never co-occur AND that they keep company a shuffle would
        // not explain, so a gate reading it admits a subset of what exclusivity admits. If the
        // subset is as clean the gate is buildable; if it is empty the vocabulary is the wrong
        // instrument and fork 102 wants a different one.
        var watching = new Alternating();

        foreach (var (moment, _) in moments) watching.Watch(moment);

        watching.Settle();

        var sorts = new Categories(watching.ByChance(Counting.Company, floor: 20));

        output.WriteLine($"{sorts.Count} categories derived over the same {moments.Count} moments");

        var better = 0;
        var scored = 0;

        var byGroup = new Dictionary<int, (int Scored, int Better)>();
        var byKind = new Dictionary<bool, (int Scored, int Better)>();
        var bySort = new Dictionary<bool, (int Scored, int Better)>();

        double parentAccuracy = 0;
        double childAccuracy = 0;
        double parentFires = 0;
        double childFires = 0;

        foreach (var group in proposals)
        {
            var members = group.Members;

            var holed = Generalising.Rule(group);

            var scope = holed.Scope;
            var expects = holed.Expects;

            var fired = 0;
            var right = 0;

            for (var at = 0; at < moments.Count; at++)
            {
                if (!Unifying.Fires(scope, moments[at].Moment, indexed[at]).Fired) continue;

                fired++;
                if (moments[at].Arrived == expects) right++;
            }

            // The siblings on the same moments, pooled. What the parent replaces is the
            // whole group, so what it is compared against is the group's own record on
            // exactly these moments and never its lifetime counters — those were kept on
            // moments it was fitted to.
            var kidsFired = 0;
            var kidsRight = 0;

            foreach (var child in members)
                for (var at = 0; at < moments.Count; at++)
                {
                    if (!child.Fires(moments[at].Moment)) continue;

                    kidsFired++;
                    if (moments[at].Arrived == expects) kidsRight++;
                }

            if (fired == 0 || kidsFired == 0) continue;

            scored++;

            var parent = right / (double)fired;
            var kids = kidsRight / (double)kidsFired;

            parentAccuracy += parent;
            childAccuracy += kids;
            parentFires += fired;
            childFires += kidsFired / (double)members.Count;

            if (parent >= kids) better++;

            // And by how many siblings the hole covers, which is the first gate anybody
            // would reach for. A hole over two rules is two rules that happen to differ; a
            // hole over six is the same rule written six times, which is what a variable
            // IS. If the share that survives rises with the group then the gate is a count
            // and costs nothing; if it is flat, the operator needs a real bar and the count
            // is a distraction.
            // And whether the values the hole covers are alternatives, which is fork 97'S
            // Definition of a category arriving where nobody put it. *Bit three is nought*
            // and *bit three is one* never co-occur; *bit three is nought* and *bit five is
            // one* do. A hole over the first pair is a variable and over the second is a
            // coincidence of position, and the two are told apart by a fact about the
            // moments rather than by anything about the rules.
            var covered = Generalising.Covered(group);

            var exclusive = covered.Count > 1
                && moments.All(one => covered.Count(one.Moment.Contains) <= 1);

            byKind.TryAdd(exclusive, (0, 0));
            byKind[exclusive] = (byKind[exclusive].Scored + 1,
                byKind[exclusive].Better + (parent >= kids ? 1 : 0));

            // The same question a machine could answer: every covered value stands in ONE
            // derived category. A code with no coarser form answers no, which is what makes
            // this a lookup rather than a judgement.
            var sorted = Generalising.Admits(group, sorts);

            bySort.TryAdd(sorted, (0, 0));
            bySort[sorted] = (bySort[sorted].Scored + 1,
                bySort[sorted].Better + (parent >= kids ? 1 : 0));

            var bucket = Math.Min(members.Count, 6);

            byGroup.TryAdd(bucket, (0, 0));
            byGroup[bucket] = (byGroup[bucket].Scored + 1,
                byGroup[bucket].Better + (parent >= kids ? 1 : 0));
        }

        output.WriteLine($"{proposals.Count} anchored proposals, {scored} of them scoreable");

        output.WriteLine(
            $"parent   | {parentAccuracy / scored:F3} accurate | fires {parentFires / scored:F0} "
            + $"of {moments.Count}");

        output.WriteLine(
            $"siblings | {childAccuracy / scored:F3} accurate | fires {childFires / scored:F0} "
            + "of the same, each");

        output.WriteLine(
            $"the parent is no worse than its group in {better} of {scored} "
            + $"({better / (double)scored:P0})");

        foreach (var (size, tally) in byGroup.OrderBy(one => one.Key))
            output.WriteLine(
                $"  covering {size}{(size == 6 ? "+" : " ")} siblings | {tally.Better,3} of "
                + $"{tally.Scored,3} no worse ({tally.Better / (double)tally.Scored:P0})");

        // And this column is the gate, which it was too thin to be until the proposals were
        // pooled over independent populations. Fork 97's definition of a category is exactly
        // what a hole wants — the values it covers should be ALTERNATIVES — and it separates
        // the proposals that pay from the ones that do not about as cleanly as anything in
        // this repo separates anything.
        output.WriteLine("and whether those values are alternatives — THE GATE:");

        foreach (var (exclusive, tally) in byKind.OrderByDescending(one => one.Key))
            output.WriteLine(
                $"  values {(exclusive ? "never co-occur" : "co-occur     ")} | {tally.Better,3} of "
                + $"{tally.Scored,3} no worse ({tally.Better / (double)tally.Scored:P0})");

        output.WriteLine("and whether a DERIVED category covers them, which a machine can ask:");

        foreach (var (sorted, tally) in bySort.OrderByDescending(one => one.Key))
            output.WriteLine(
                $"  values {(sorted ? "share a category" : "do not         ")} | {tally.Better,3} of "
                + $"{tally.Scored,3} no worse ({tally.Better / (double)tally.Scored:P0})");

        Assert.True(scored > 0, "no proposal ever fired, so nothing was scored");

        // The finding, held down in the direction it came out. A hole proposed blindly is
        // WORSE than the siblings it replaces: it fires about twice as often and pays for
        // it, and it is no worse in about one case in ten. So anti-unification firing on
        // every sibling group is refuted, and rung four's admission needs a gate exactly
        // as genesis does — promiscuous proposal is fine, unbarred admission is not.
        //
        // If this flips the conclusion is owed a re-take rather than a deletion, because a
        // population learnt under different search dials could hold sibling groups that
        // are real generalisations rather than coincidences of one position.
        Assert.True(better < scored / 2,
            $"the parent was no worse in {better} of {scored} proposals, so generalising "
            + "every sibling group is no longer refuted and the gate this file asks for "
            + "may not be needed");

        // And no bar on the size column, which is where a bar would be a guess. What it
        // says is that the share surviving is flat across groups of two, three and four
        // and that the larger buckets are too thin to read — so a count is not the gate.

        // The gate, and it is a comparison rather than a level because a level from one grid
        // is not a claim anything refutes. A hole whose covered values never co-occur is a
        // variable; one whose values do is a coincidence of position, and the two are told
        // apart by a fact about the MOMENTS rather than by anything about the rules — which
        // is why nothing in the population could have supplied it.
        //
        // Its price is said beside it: the gate admits a small fraction of proposals, so
        // anti-unification under it fires rarely. That is the shape every gate here has —
        // promiscuous proposal, and the gate doing the work — and it is the opposite of a
        // gate that admits most of what it sees and hopes the vote sorts it out.
        var alternating = byKind.GetValueOrDefault(true);
        var overlapping = byKind.GetValueOrDefault(false);

        Assert.True(alternating.Scored >= 30,
            $"only {alternating.Scored} proposals covered values that never co-occur, which is "
            + "too few to read a rate off -- raise `Populations` rather than reading it");

        Assert.True(
            alternating.Better / (double)alternating.Scored
                > 3 * (overlapping.Better / (double)overlapping.Scored),
            $"a hole over values that never co-occur is no worse in "
            + $"{alternating.Better / (double)alternating.Scored:P0} of {alternating.Scored} cases "
            + $"against {overlapping.Better / (double)overlapping.Scored:P0} where they do, so "
            + "whether the covered values are alternatives is not the gate this file reports "
            + "and fork 102 is open again");

        // And the same gate asked as a LOOKUP, which is the difference between a reading and
        // a mechanism. Exclusivity is computed above from the moments themselves and nothing
        // inside a population can do that; a `Categories` the front end derived is a table the
        // brain already holds, and asking whether the covered values share a coarser form is
        // one dictionary hit.
        //
        // The bar is that the two columns AGREE rather than that the second clears a level.
        // The category is the stricter test -- it wants the values to keep company a shuffle
        // cannot explain as well as never meeting -- so a world where the extra clause throws
        // real proposals away would fail here and be worth knowing about. On this one it
        // throws away none.
        var sortedGate = bySort.GetValueOrDefault(true);
        var unsorted = bySort.GetValueOrDefault(false);

        Assert.True(sortedGate == alternating && unsorted == overlapping,
            $"a derived category covers the values of {sortedGate.Scored} proposals against "
            + $"{alternating.Scored} whose values simply never meet, and is no worse in "
            + $"{sortedGate.Better} against {alternating.Better}. The two columns are supposed "
            + "to be the same question asked twice, so where they part the vocabulary is "
            + "refusing proposals the gate admits and rung four's admission cannot read it");
    }
}
