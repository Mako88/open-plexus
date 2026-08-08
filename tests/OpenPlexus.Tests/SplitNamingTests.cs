using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whether rung five survives being split up — <b>the deployment case, and neither row
/// <c>FoldingTests</c> measured.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>EVERY HOLDER SEES EVERY MOMENT, WHICH IS WHAT THE BUS ALREADY DOES.</b> An input
/// machine broadcasts, so the observations are not what differs between two holders — the
/// COMMITMENTS are, because a ring places a code and a holder keeps what lands on it. That
/// makes <c>FoldingTests</c>' two rows the wrong pair for a deployment: one gave two
/// machines different observations and the other gave them identical everything.
/// </para>
/// <para>
/// <b>AND ABSTRACTION'S EVIDENCE IS THE POPULATION, WHICH IS PRECISELY WHAT GETS
/// SPLIT.</b> <see cref="Abstracting.Shared"/> reads every resident scope, counts which
/// pairs recur across them, and names the pair that beats what independent scopes would
/// have produced. Sharding cuts the scopes each holder can see, so it moves all three
/// terms at once: the count a pair must reach, the marginal frequencies it is tested
/// against, and the number of candidates the bar is corrected for.
/// </para>
/// <para>
/// <b>SO THE QUESTION IS NOT WHETHER TWO HOLDERS AGREE, IT IS WHETHER EITHER ONE STILL
/// SPEAKS.</b> The description-length bar wants a pair in three scopes and the gate wants
/// three scopes to exist at all. A twelfth of a population may hold neither, and rung five
/// would then not diverge across machines — it would go silent on all of them, which reads
/// from any score exactly like a mechanism that was never load-bearing.
/// </para>
/// </remarks>
public sealed class SplitNamingTests(ITestOutputHelper output)
{
    private const long Rounds = 20000;

    /// <summary>Eleven bits, because fork 34 says six mints nothing to split.</summary>
    private const int Address = 3;

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_sharding_a_population_does_to_what_it_can_name()
    {
        var dials = new CommittingSettings();
        var brain = new Brain(dials, seed: 1);

        new MultiplexerRun(new MultiplexerSettings { Address = Address }, brain, seed: 1)
            .Run(Rounds);

        var held = brain.Held;
        var all = held.All.ToList();

        // THE WHOLE POPULATION'S ANSWER IS THE BASELINE, and every split is measured
        // against it rather than against nothing. `Abstracting.Shared` proposes one pair,
        // so what a shard can do is agree with that, propose something else, or say
        // nothing -- and the three are completely different failures.
        var whole = Abstracting.Shared(all, dials);

        // WHAT `Shared` ACTUALLY READS, WHICH IS NOT THE RESIDENT COUNT AND IS THE NUMBER
        // THE CLIFF BELOW IS ABOUT. It proposes only from commitments past the experience
        // floor with a scope of two or more, and then wants three such scopes to exist and
        // a pair recurring across three of them. So the pool that gets split is this one,
        // and a resident count in the hundreds can sit on top of a pool in the tens.
        var eligible = all.Count(one => one.Seen >= dials.Floor && one.Scope.Length >= 2);

        output.WriteLine($"{all.Count} resident, {eligible} eligible to propose "
            + $"| whole population proposes "
            + $"{(whole is { } one ? Naming.Name(one).Value.ToString() : "nothing")}");

        Assert.True(all.Count > 20,
            $"only {all.Count} commitments resident — there is nothing here to shard");

        // AND THE BASELINE HAS TO SPEAK OR THE GRID BELOW IS UNREADABLE. A population that
        // names nothing whole cannot show sharding taking anything away, and every row
        // would read `silent` for a reason that has nothing to do with splitting.
        Assert.NotNull(whole);

        output.WriteLine(
            "holders | eligible a shard | proposing | distinct names | baseline kept");

        foreach (var holders in new[] { 1, 2, 3, 5, 12 })
        {
            var shards = new List<List<Commitment>>();

            for (var holder = 0; holder < holders; holder++) shards.Add([]);

            foreach (var commitment in all)
                shards[(int)(commitment.Identity.Value % (ulong)holders)].Add(commitment);

            var proposed = shards
                .Select(shard => Abstracting.Shared(shard, dials))
                .ToList();

            var spoke = proposed.Count(one => one is not null);

            var names = proposed
                .Where(one => one is not null)
                .Select(one => Naming.Name(one!.Value))
                .ToHashSet();

            var kept = whole is { } baseline && names.Contains(Naming.Name(baseline));

            var pool = shards.Average(shard =>
                shard.Count(one => one.Seen >= dials.Floor && one.Scope.Length >= 2));

            output.WriteLine(
                $"{holders,7} | {pool,16:F1} | {spoke,9} | {names.Count,14} | {kept,13}");
        }

        // ---- which of the two explanations it is --------------------------------
        //
        // THE OBVIOUS READING IS STARVATION AND THE COLUMN ABOVE REFUTES IT. `Shared`
        // wants three eligible scopes and a pair recurring across three of them; a shard
        // holding thirty-six clears both by a wide margin and still says nothing. So the
        // evidence is present and something else is refusing it.
        //
        // THE OTHER READING IS POWER, AND LOOSENING THE ONE BAR IS HOW TO TELL. `Shared`
        // ends on `Normal.Tail(z) * candidates <= Alpha`, and z carries a factor of the
        // square root of the scope count -- so splitting a population does not remove a
        // redundancy, it removes the ability to CERTIFY one. If a looser alpha revives the
        // shards, the pattern was in every one of them all along.
        //
        // AND THIS IS AN ARM RATHER THAN A PROPOSAL. A bar loosened until something passes
        // is the oldest way to manufacture a finding; nothing here suggests changing
        // `Alpha`, and the number below is diagnostic only.
        output.WriteLine("holders | proposing at alpha 0.05 | proposing at alpha 0.5");

        foreach (var holders in new[] { 3, 5, 12 })
        {
            var shards = new List<List<Commitment>>();

            for (var holder = 0; holder < holders; holder++) shards.Add([]);

            foreach (var commitment in all)
                shards[(int)(commitment.Identity.Value % (ulong)holders)].Add(commitment);

            var strict = shards.Count(shard => Abstracting.Shared(shard, dials) is not null);

            var loose = shards.Count(shard =>
                Abstracting.Shared(shard, dials with { Alpha = 0.5 }) is not null);

            output.WriteLine($"{holders,7} | {strict,22} | {loose,21}");
        }

        // NO BAR ON ANY OF IT, BECAUSE WHAT SHARDING SHOULD COST RUNG FIVE HAS NEVER BEEN
        // MEASURED and a threshold written before the first reading would be a prediction
        // dressed as a requirement. The grids are the finding.
    }

    [Fact]
    public void Merging_the_counts_gets_the_whole_populations_name_back()
    {
        // WHAT THE GRID ABOVE POINTS AT, BUILT. `Recurrence` is the two frequency tables
        // the gate reads and nothing else -- how often each code appeared across scopes
        // and how often each pair appeared together. A holder counts its own and the
        // merge adds them, which is what a G-Counter is and why this design already
        // trusts the shape.
        //
        // AND EXACTLY RATHER THAN NEARLY, WHICH IS THE WHOLE POINT OF IT BEING COUNTS.
        // `Population.Decide` carries a caveat that a sharded sum is not bit-identical
        // under `Summing`, because floating-point addition is not associative. Integers
        // have no such caveat, so this is an equality and must never become a tolerance.
        var dials = new CommittingSettings();
        var brain = new Brain(dials, seed: 1);

        new MultiplexerRun(new MultiplexerSettings { Address = Address }, brain, seed: 1)
            .Run(Rounds);

        var all = brain.Held.All.ToList();

        var whole = Abstracting.Shared(all, dials);

        Assert.NotNull(whole);

        foreach (var holders in new[] { 2, 3, 5, 12 })
        {
            var shards = new List<List<Commitment>>();

            for (var holder = 0; holder < holders; holder++) shards.Add([]);

            foreach (var commitment in all)
                shards[(int)(commitment.Identity.Value % (ulong)holders)].Add(commitment);

            // COUNTED WHERE THE COMMITMENTS ARE AND MERGED WHERE NOTHING IS, so no holder
            // ever sees another's scopes -- which is the C1 claim this whole arrangement
            // exists to keep, and it is kept by what the type can carry rather than by
            // anybody being careful.
            var merged = new Recurrence();

            foreach (var shard in shards) merged.Absorb(Recurrence.Of(shard, dials));

            Assert.Equal(whole, Abstracting.Shared(merged, dials));
        }
    }

    [Fact]
    public void And_the_merge_does_not_care_what_order_the_holders_answered_in()
    {
        // C2 SAYS THE ORDER IS THE NETWORK'S CHOICE, and a result that moved with a choice
        // nobody made is fork 12 -- reopened twice already, and this is a third door.
        // Integer addition is commutative, so the assertion is that the code actually uses
        // it that way rather than that arithmetic works.
        //
        // AND THE TIE-BREAK IS THE HALF THAT WAS ACTUALLY BROKEN. `Shared` walked its pair
        // table with a strict improvement test, so two pairs at the same z resolved by
        // whichever the dictionary reached first -- stable in one process and nothing at
        // all across two tables merged in different orders. Ordering the walk is what
        // makes this test able to pass.
        var dials = new CommittingSettings();
        var brain = new Brain(dials, seed: 1);

        new MultiplexerRun(new MultiplexerSettings { Address = Address }, brain, seed: 1)
            .Run(Rounds);

        var all = brain.Held.All.ToList();

        var shards = new List<List<Commitment>>();

        for (var holder = 0; holder < 5; holder++) shards.Add([]);

        foreach (var commitment in all)
            shards[(int)(commitment.Identity.Value % 5UL)].Add(commitment);

        var counted = shards.Select(shard => Recurrence.Of(shard, dials)).ToList();

        var forwards = new Recurrence();
        foreach (var one in counted) forwards.Absorb(one);

        var backwards = new Recurrence();
        foreach (var one in Enumerable.Reverse(counted)) backwards.Absorb(one);

        Assert.Equal(forwards.Scopes, backwards.Scopes);

        Assert.Equal(
            Abstracting.Shared(forwards, dials),
            Abstracting.Shared(backwards, dials));
    }
}
