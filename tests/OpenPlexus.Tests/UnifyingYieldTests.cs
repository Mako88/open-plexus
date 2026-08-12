using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What would ever PROPOSE a scope naming no argument — <b>the half fork 33's price did
/// not touch, and the only thing still standing between rung four and a run.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A SCOPE HANDED IN BY AN EXPERIMENTER IS ARCHITECTURE, WHICH IS THE ONE THING THIS
/// DESIGN MAY NOT DO.</b> The matcher is priced and cheap; what nothing here can do is
/// arrive at <i>whichever word</i> on its own. So the operator has to be summoned by
/// something the population already contains, and the obvious candidate is the one rung
/// five is already summoned by: REDUNDANCY.
/// </para>
/// <para>
/// <b>AND IT IS ANTI-UNIFICATION, WHICH IS THE DUAL OF REPAIR AND NOT A NEW IDEA.</b>
/// Where several commitments expect the same thing and differ in exactly one position of
/// their scope, the thing they share is a rule with a hole in it — <i>whoever was asked
/// about</i> rather than <i>mary</i>, <i>john</i>, <i>sandra</i> and <i>daniel</i>
/// separately. Repair walks specific-to-general nowhere; this is the step that would.
/// </para>
/// <para>
/// <b>SO THE QUESTION BEFORE THE BUILD IS WHETHER THERE IS ANYTHING TO FIRE ON.</b> A
/// population holding no two commitments that differ in one position gives the operator
/// nothing, and rung four would then be blocked by the SHAPE of what is learnt rather
/// than by the operator being unbuilt — a different problem with a different fix. That is
/// a count, and this file takes it.
/// </para>
/// <para>
/// <b>NO BAR, BECAUSE NOTHING IS BEING COMPARED YET.</b> The counts say whether the
/// operator has a trigger. What it would then be WORTH is fork 88's number and already
/// taken; what it would cost is fork 33's and already taken.
/// </para>
/// </remarks>
public sealed class UnifyingYieldTests(ITestOutputHelper output)
{
    /// <summary>How many rounds each population is learnt over.</summary>
    private const long Rounds = 4000;

    /// <summary>
    /// Sibling groups, and how big they get — <b>commitments expecting one thing and
    /// differing in exactly one position of an otherwise identical scope.</b>
    /// </summary>
    /// <param name="all">The population to read.</param>
    /// <remarks>
    /// <b>KEYED ON THE SCOPE WITH ONE POSITION BLANKED, WHICH IS THE HOLE ITSELF.</b> Two
    /// commitments land in the same group exactly when one variable would cover both, so
    /// a group of size N is a proposal to replace N rules with one — and the size is what
    /// says whether the replacement is worth making rather than a rename.
    /// </remarks>
    /// <param name="anchored">
    /// Whether to count only groups whose members keep a real code beside the hole.
    /// <b>The instrument check, and it is the same one twice already caught today.</b> A
    /// scope of ONE code with that code blanked becomes <i>whichever code of this kind,
    /// expect Y</i> — which fires on every moment holding any word at all and is a rule
    /// about nothing. Counting those as siblings would make the trigger look abundant on
    /// exactly the population where it has least to say.
    /// </param>
    private static List<int> Siblings(IReadOnlyList<Commitment> all, bool anchored)
    {
        var groups = new Dictionary<string, HashSet<Code>>(StringComparer.Ordinal);

        foreach (var one in all)
        {
            if (anchored && one.Scope.Length < 2) continue;

            for (var hole = 0; hole < one.Scope.Length; hole++)
            {
                // THE HOLE'S MODALITY IS PART OF THE KEY AND THE VALUE IS NOT. A variable
                // is *whichever code of this kind*, so two commitments differing in a word
                // are siblings and two differing in a word against a place are not — the
                // second pair shares no rule with a hole in it, it just happens to have
                // the same length.
                var key = string.Join(
                    ",",
                    one.Scope.Select((code, at) => at == hole
                        ? $"?{code.Modality}"
                        : $"{code.Modality}:{code.Value}"));

                key = $"{one.Expects.Modality}:{one.Expects.Value}|{hole}|{key}";

                if (!groups.TryGetValue(key, out var members)) groups[key] = members = [];

                members.Add(one.Identity);
            }
        }

        return [.. groups.Values.Select(one => one.Count).Where(size => size > 1).OrderDescending()];
    }

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

        new Trial<Asking>(
            new Recalled(new RecalledSettings
            {
                Corpus = Tree.Babi(), Task = 1, Span = 0, Withheld = 40,
                Predicting = Predicting.Asked,
            }),
            new Joined(Joining.Bagged),
            text)
            .Run(Rounds, sweep: 1000, target: 0.9, window: 2000);

        var onText = Report("babi 1", text.Held.All.ToList());

        // THE MULTIPLEXER BESIDE IT, AND IT IS THE WORLD THE ARGUMENT WAS ALWAYS ABOUT.
        // *These positions are the address, whatever they say* is the concept rung five
        // provably cannot name — zero of 258 minted names grouped the address — and it is
        // a hole in a scope rather than a set of codes. If sibling groups exist anywhere
        // they exist here, and if they do not the trigger is what is missing.
        var bits = new Brain(new CommittingSettings { Capacity = 2000 }, seed: 1);

        new MultiplexerRun(new MultiplexerSettings { Address = 3 }, bits, seed: 1).Run(Rounds);

        var onBits = Report("multiplexer", bits.Held.All.ToList());

        // THE ONE THING HELD DOWN, AND IT IS THE TRIGGER'S EXISTENCE RATHER THAN ITS SIZE.
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
    /// <b>A VARIABLE IS A GENERALISATION AND EVERY OPERATOR HERE NARROWS, so the risk is
    /// named before the number: a scope with a hole in it fires far more often than any
    /// sibling it covers, and firing more can only cost accuracy.</b> If the parent is
    /// worse than the siblings it replaces then repair would refuse it exactly as it
    /// refuses any other child that does not clear the bars, and rung four would mint
    /// nothing that survives however cheap the matcher is.
    /// </para>
    /// <para>
    /// <b>ON THE MULTIPLEXER, BECAUSE THE CONCEPT IS KNOWN AND RUNG FIVE PROVABLY MISSED
    /// IT.</b> <i>These positions are the address, whatever they say</i> is a hole in a
    /// scope and not a set of codes, and not one minted name in 258 grouped the address.
    /// So this is the same target approached by the operator whose shape actually fits it,
    /// which is what makes the comparison worth taking rather than a repeat.
    /// </para>
    /// <para>
    /// <b>AND IT IS SCORED ON MOMENTS THE POPULATION NEVER LEARNT ON</b>, from a world at a
    /// different seed. A parent scored on what its siblings were fitted to would be asking
    /// whether a generalisation reproduces its own training set, which it does by
    /// construction.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_the_parent_a_hole_proposes_is_worth_against_the_siblings_it_replaces()
    {
        var brain = new Brain(new CommittingSettings { Capacity = 2000 }, seed: 1);

        new MultiplexerRun(new MultiplexerSettings { Address = 3 }, brain, seed: 1).Run(Rounds);

        var all = brain.Held.All.ToList();

        // THE GROUPS AGAIN, KEPT THIS TIME RATHER THAN COUNTED. Same key as `Siblings`, and
        // anchored for the same reason — a hole with no context beside it is a rule about
        // nothing and would drag the parent's score down for a reason that is this file's
        // rather than the operator's.
        var groups = new Dictionary<string, List<Commitment>>(StringComparer.Ordinal);

        foreach (var one in all.Where(one => one.Scope.Length > 1))
        {
            for (var hole = 0; hole < one.Scope.Length; hole++)
            {
                var key = $"{one.Expects.Modality}:{one.Expects.Value}|{hole}|" + string.Join(
                    ",",
                    one.Scope.Select((code, at) => at == hole
                        ? $"?{code.Modality}"
                        : $"{code.Modality}:{code.Value}"));

                if (!groups.TryGetValue(key, out var members)) groups[key] = members = [];

                members.Add(one);
            }
        }

        var proposals = groups
            .Where(one => one.Value.Count > 1)
            .OrderByDescending(one => one.Value.Count)
            .ThenBy(one => one.Key, StringComparer.Ordinal)
            .ToList();

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

        var better = 0;
        var scored = 0;

        var byGroup = new Dictionary<int, (int Scored, int Better)>();
        var byKind = new Dictionary<bool, (int Scored, int Better)>();

        double parentAccuracy = 0;
        double childAccuracy = 0;
        double parentFires = 0;
        double childFires = 0;

        foreach (var (key, members) in proposals)
        {
            var hole = int.Parse(key.Split('|')[1]);

            var scope = members[0].Scope
                .Select((code, at) => at == hole ? Unifying.Any(code.Modality, 0) : code)
                .ToImmutableArray();

            var expects = members[0].Expects;

            var fired = 0;
            var right = 0;

            for (var at = 0; at < moments.Count; at++)
            {
                if (!Unifying.Fires(scope, moments[at].Moment, indexed[at]).Fired) continue;

                fired++;
                if (moments[at].Arrived == expects) right++;
            }

            // THE SIBLINGS ON THE SAME MOMENTS, POOLED. What the parent replaces is the
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

            // AND BY HOW MANY SIBLINGS THE HOLE COVERS, WHICH IS THE FIRST GATE ANYBODY
            // WOULD REACH FOR. A hole over two rules is two rules that happen to differ; a
            // hole over six is the same rule written six times, which is what a variable
            // IS. If the share that survives rises with the group then the gate is a count
            // and costs nothing; if it is flat, the operator needs a real bar and the count
            // is a distraction.
            // AND WHETHER THE VALUES THE HOLE COVERS ARE ALTERNATIVES, WHICH IS FORK 97'S
            // DEFINITION OF A CATEGORY ARRIVING WHERE NOBODY PUT IT. *Bit three is nought*
            // and *bit three is one* never co-occur; *bit three is nought* and *bit five is
            // one* do. A hole over the first pair is a variable and over the second is a
            // coincidence of position, and the two are told apart by a fact about the
            // moments rather than by anything about the rules.
            var covered = members.Select(child => child.Scope[hole]).Distinct().ToList();

            var exclusive = covered.Count > 1
                && moments.All(one => covered.Count(one.Moment.Contains) <= 1);

            byKind.TryAdd(exclusive, (0, 0));
            byKind[exclusive] = (byKind[exclusive].Scored + 1,
                byKind[exclusive].Better + (parent >= kids ? 1 : 0));

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

        // AND THIS COLUMN IS A LEAD RATHER THAN A FINDING, WHICH IS SAID HERE BECAUSE IT
        // WILL READ AS A FINDING. Fork 97's definition of a category is exactly what a hole
        // wants — the values it covers should be ALTERNATIVES — and on this world almost no
        // sibling group is one, because repair adds a discriminating code rather than a
        // pair. A rate over a handful of cases carries nothing whichever way it falls, and
        // this repo has already been caught once reading a ratio over a few rounds.
        output.WriteLine("and whether those values are alternatives — TOO FEW TO READ:");

        foreach (var (exclusive, tally) in byKind.OrderByDescending(one => one.Key))
            output.WriteLine(
                $"  values {(exclusive ? "never co-occur" : "co-occur     ")} | {tally.Better,3} of "
                + $"{tally.Scored,3} no worse ({tally.Better / (double)tally.Scored:P0})");

        Assert.True(scored > 0, "no proposal ever fired, so nothing was scored");

        // THE FINDING, HELD DOWN IN THE DIRECTION IT CAME OUT. A hole proposed blindly is
        // WORSE than the siblings it replaces: it fires about twice as often and pays for
        // it, and it is no worse in about one case in ten. So anti-unification firing on
        // every sibling group is refuted, and rung four's admission needs a gate exactly
        // as genesis does — promiscuous proposal is fine, unbarred admission is not.
        //
        // IF THIS FLIPS THE CONCLUSION IS OWED A RE-TAKE RATHER THAN A DELETION, because a
        // population learnt under different search dials could hold sibling groups that
        // are real generalisations rather than coincidences of one position.
        Assert.True(better < scored / 2,
            $"the parent was no worse in {better} of {scored} proposals, so generalising "
            + "every sibling group is no longer refuted and the gate this file asks for "
            + "may not be needed");

        // AND NO BAR ON THE SIZE COLUMN, WHICH IS WHERE A BAR WOULD BE A GUESS. What it
        // says is that the share surviving is flat across groups of two, three and four
        // and that the larger buckets are too thin to read — so a count is not the gate,
        // and what is remains open.
    }
}
