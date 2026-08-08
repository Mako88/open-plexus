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
}
