using System.Collections.Immutable;
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
/// SPLIT.</b> <see cref="Abstracting.Shared(Recurrence, CommittingSettings)"/> reads every resident scope, counts which
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
    /// <summary>
    /// Rounds, <b>and the five hundred past the last sweep are the point of the number.</b>
    /// </summary>
    /// <remarks>
    /// <b>A RUN ENDING ON A SWEEP ROUND IS READ AT ITS MOST EXHAUSTED, and this file's whole
    /// subject is what a trained population would name NEXT.</b> At twenty thousand exactly,
    /// three seeds in eight have nothing left to say — so the assertions here stood on seed
    /// one happening to be one of the five that did, which is the single-seed ordering this
    /// repo's traps list already names. Five hundred rounds of repair past the last sweep
    /// rebuild a subject: six seeds in eight under the shipped naming and eight in eight when
    /// naming runs until refused, seed one included under both.
    /// </remarks>
    private const long Rounds = 20_500;

    /// <summary>Eleven bits, because fork 34 says six mints nothing to split.</summary>
    private const int Address = 3;

    /// <summary>
    /// How much repair this world's population is built from — <b>pinned here rather than
    /// inherited, because it decides whether this file's question exists at all.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THIS FILE'S PRECONDITION IS THAT A SHARD ALONE NAMES NOTHING, AND A SEARCH DIAL
    /// BUYS PAST IT.</b> Every reading here is about what SPLITTING costs rung five, so it
    /// needs a population a third of which cannot certify a redundancy by itself. Raise the
    /// repair budget and each third holds enough eligible scopes to clear the gate unaided —
    /// three holders naming three things where the fixture requires three naming none.
    /// </para>
    /// <para>
    /// <b>SO THE CLAIM IS CONDITIONAL ON SHARD SIZE AND WAS NEVER WRITTEN THAT WAY.</b>
    /// <i>Splitting a population does not remove a redundancy, it removes the ability to
    /// certify one</i> is true of shards too small to reach the gate's counts, which is what
    /// this world was. It is not a fact about splitting as such, and the plan says so now.
    /// </para>
    /// <para>
    /// <b>AND THE TIMING IS PINNED BESIDE IT, BECAUSE THE TWO FAILURES ARE OPPOSITE AND ONE
    /// NUMBER CANNOT DODGE BOTH.</b> Under <see cref="Repairing.EveryRound"/> a budget of 64
    /// leaves the WHOLE population naming nothing, so the baseline assertion goes red; 256
    /// gives every third enough to name alone, so the precondition goes red instead. What
    /// this file needs is a population rich enough to name and shards too poor to — and that
    /// is the window <c>AskedTests</c> already pinned for the identical reason, which is why
    /// it was the only one of the three still green.
    /// </para>
    /// <para>
    /// <b>SO THE TWO FILES PIN THE SAME PAIR, AND THEY MEASURE ONE MECHANISM ON TWO SIDES OF
    /// A SOCKET.</b> A window that differed between them would make the in-process reading
    /// and the wire reading incomparable, which is the whole point of having both.
    /// </para>
    /// </remarks>
    private const int Sparse = 64;

    /// <inheritdoc cref="Sparse"/>
    private const Repairing Waiting = Repairing.AfterFailure;

    /// <summary>
    /// The seed the window is read on — <b>a fixture constant, and it moved when a dial
    /// nothing here names moved.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE PRECONDITION IS A PROPERTY OF ONE RUN AND EVERY SEARCH DIAL MOVES IT.</b>
    /// Seed one satisfied <i>whole names, no third names alone</i> until the vote rule
    /// changed — and under <see cref="Repairing.AfterFailure"/> the vote decides what repair
    /// may run on, so a readout change is a search change and the window moved with it. That
    /// is the row this file already carried, arriving from a direction nobody watched.
    /// </para>
    /// <para>
    /// <b>AND IT IS NOT A THIN WINDOW, WHICH IS WORTH KNOWING BEFORE THE NEXT HUNT.</b> Four
    /// seeds in twelve satisfy the precondition at this budget and timing — 4, 8, 9 and 10 —
    /// so a red here means finding another rather than re-tuning the pair. The assertions
    /// below say which half failed.
    /// </para>
    /// </remarks>
    private const int Subject = 4;

    /// <summary>A trained population, its dials, and the name it proposes whole.</summary>
    /// <remarks>
    /// <b>WRITTEN ONCE BECAUSE THE CLONE BUDGET REFUSED THE THIRD COPY.</b> Three tests
    /// here each need a population and the whole-population baseline it is measured
    /// against, and three copies of that setup is three places for the dials to drift —
    /// which would make three grids that look comparable and are not.
    /// </remarks>
    private static (CommittingSettings Dials, List<Commitment> All, ImmutableArray<Code>? Whole)
        Trained()
    {
        var dials = new CommittingSettings { Budget = Sparse, Repairing = Waiting };
        var brain = new Brain(dials, Subject);

        new MultiplexerRun(new MultiplexerSettings { Address = Address }, brain, Subject)
            .Run(Rounds);

        var all = brain.Held.All.ToList();

        return (dials, all, Abstracting.Shared(all, dials));
    }

    /// <inheritdoc cref="Fixture.Sharded"/>
    /// <param name="all">The whole population.</param>
    /// <param name="holders">How many machines to spread it over.</param>
    private static List<List<Commitment>> Sharded(IEnumerable<Commitment> all, int holders) =>
        Fixture.Sharded(all, holders);

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_sharding_a_population_does_to_what_it_can_name()
    {
        var (dials, all, _) = Trained();

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
        var eligible = all.Count(one => Recurrence.Eligible(one, dials));

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
            var shards = Sharded(all, holders);

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
            var shards = Sharded(all, holders);

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
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_dead_holder_costs_a_merged_name()
    {
        // C3 SAYS A CLUSTER VANISHING MID-THOUGHT IS NORMAL, AND EVERY FILE IN THIS ARC
        // HAS CARRIED A NOTE THAT NOTHING DIES IN IT. This is the smallest honest version
        // of that: a merge is only ever over the holders that answered, so a death is
        // counts that never arrive.
        //
        // AND IT SHOULD DEGRADE RATHER THAN BREAK, WHICH IS THE PREDICTION UNDER TEST.
        // Losing counts is losing scopes, and losing scopes is the power problem this file
        // already measured -- so a merge missing a holder should sit somewhere between the
        // whole population and a single shard, and the question is how many deaths it
        // takes to fall off.
        var (dials, all, whole) = Trained();

        Assert.NotNull(whole);

        const int Holders = 12;

        var shards = Sharded(all, Holders);

        output.WriteLine("dead | arrangements | still the whole population's name | silent");

        for (var dead = 0; dead < Holders; dead++)
        {
            var arrangements = 0;
            var kept = 0;
            var silent = 0;

            // EVERY WAY OF LOSING THAT MANY, UP TO A BOUND, because which holders die is
            // not something a deployment gets to choose and one arrangement would be one
            // sample of a distribution. Bounded by walking a stride through the
            // combinations rather than all of them, which is stated because a truncation
            // that goes unsaid reads as coverage.
            for (var first = 0; first < Holders; first++)
            {
                var gone = new HashSet<int>();

                for (var step = 0; step < dead; step++) gone.Add((first + step) % Holders);

                var merged = new Recurrence();

                for (var holder = 0; holder < Holders; holder++)
                    if (!gone.Contains(holder))
                        merged.Absorb(Recurrence.Of(shards[holder], dials));

                var said = Abstracting.Shared(merged, dials);

                arrangements++;

                if (said is null) silent++;
                else if (Naming.Name(said.Value) == Naming.Name(whole.Value)) kept++;
            }

            output.WriteLine(
                $"{dead,4} | {arrangements,12} | {kept,33} | {silent,6}");
        }

        // NO BAR, because how many deaths rung five should survive has never been measured
        // and a threshold written before the first reading would be a prediction dressed as
        // a requirement. The grid is the finding.
    }

    [Fact]
    public void Holders_told_each_others_counts_mint_one_name_where_alone_they_mint_none()
    {
        // THE PAYOFF OF THE WHOLE NAMING ARC, ASKED OF `Population` RATHER THAN OF
        // `Abstracting`. Every measurement so far has been of the gate in isolation; this
        // is the operator the learner actually calls, on populations that hold a shard
        // each, and it asks the two questions that matter together -- does a holder speak
        // at all, and do the holders agree.
        var (dials, all, whole) = Trained();

        Assert.NotNull(whole);

        const int Holders = 3;

        var holders = Sharded(all, Holders)
            .Select(shard =>
            {
                var held = new Population(dials, seed: 1);
                foreach (var commitment in shard) held.Add(commitment);
                return held;
            })
            .ToList();

        // COUNTED BEFORE ANYTHING IS ABSTRACTED, because `Abstract` rewrites scopes -- so a
        // holder that abstracted first would be telling the others about a population that
        // no longer exists. In a deployment this is one round of exchange and here it is
        // the same thing: everybody speaks from the same moment.
        var counted = holders.Select(held => Recurrence.Of(held.All, dials)).ToList();

        var minted = new List<Code>();

        for (var holder = 0; holder < Holders; holder++)
        {
            // ALONE FIRST, AND IT HAS TO COME BACK NOUGHT. If a shard can name something
            // by itself then this world does not show the problem being fixed, and the
            // line below would be crediting the exchange with something free.
            Assert.Equal(0, holders[holder].Abstract());

            var heard = new Recurrence();

            for (var other = 0; other < Holders; other++)
                if (other != holder) heard.Absorb(counted[other]);

            Assert.Equal(1, holders[holder].Abstract(heard) > 0 ? 1 : 0);

            minted.AddRange(holders[holder].Names.Means.Select(one => one.Key));
        }

        // ONE NAME BETWEEN THEM, WHICH IS THE CLAIM. Three holders minting three names is
        // three vocabularies and is the failure this arc has been about; three holders
        // minting one is the mechanism working.
        Assert.Single(minted.Distinct());

        // AND IT IS THE NAME THE WHOLE POPULATION WOULD HAVE MINTED, so the exchange
        // recovers the undistributed answer rather than merely agreeing on something.
        Assert.Equal(Naming.Name(whole.Value), minted[0]);

        output.WriteLine(
            $"{Holders} holders, {minted.Count} names minted, "
            + $"{minted.Distinct().Count()} distinct, matching the whole population");
    }

    [Fact]
    public void The_counts_survive_being_written_as_bytes_and_read_back()
    {
        // THE TYPE BUILT TO CROSS A WIRE COULD NOT CROSS ONE, AND NOTHING SAID SO.
        // `Recurrence` keys its pairs on a tuple and keeps both tables private, so a
        // serialiser writes the scope count and drops the tables -- `{"Scopes":110}`,
        // printed below. That is worse than an empty object, because it LOOKS like a
        // message: a plausible number, no throw, no warning, and a merge on the far side
        // that adds nothing to anything. Exactly how `Graph.Kind` arrived as `default` on
        // every edge until `Wire` grew a converter for it.
        //
        // SO THE ROUND TRIP IS ASSERTED ON THE ANSWER AND NOT ON THE BYTES. Two tables
        // comparing equal says the fields survived; the same NAME coming out the far side
        // says the statistic did, and that is the thing anything downstream depends on.
        var (dials, all, whole) = Trained();

        Assert.NotNull(whole);

        var here = Recurrence.Of(all, dials);

        var there = Recurrence.From(
            OpenPlexus.Bus.Wire.Read<Counts>(OpenPlexus.Bus.Wire.Write(here.Written())));

        Assert.Equal(here.Scopes, there.Scopes);

        Assert.Equal(whole, Abstracting.Shared(there, dials));

        // AND THE BYTES ARE THE SAME BYTES, because two machines writing one table must
        // write one string or nothing downstream can compare what crossed. A dictionary's
        // walk is not stable across processes, which is why `Written` orders.
        Assert.Equal(
            OpenPlexus.Bus.Wire.Write(here.Written()),
            OpenPlexus.Bus.Wire.Write(there.Written()));

        // THE CHECK THAT THE ROUND TRIP CARRIED ANYTHING AT ALL. An empty table writes,
        // reads and compares equal perfectly, and would pass every line above.
        Assert.True(here.Written().Rows.Length > 0,
            "nothing was counted, so the round trip above moved an empty table");

        // AND THE REASON THE PROJECTION EXISTS, ASSERTED RATHER THAN CLAIMED. Handing
        // `Recurrence` straight to the serialiser is the obvious thing to write and it
        // loses both tables in silence -- so this pins the failure the projection avoids.
        // If a future runtime makes the direct form work, this line fails, somebody reads
        // this comment, and `Written` can go. That is the outcome to want.
        var naive = OpenPlexus.Bus.Wire.Write(here);

        output.WriteLine($"a `Recurrence` handed straight to the wire is: {naive}");

        Assert.DoesNotContain("Rows", naive, StringComparison.Ordinal);
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
        // A sharded SUM was never bit-identical, because floating-point addition is not
        // associative -- that arm is deleted and integers have no such caveat either way,
        // so this is an equality and must never become a tolerance.
        var (dials, all, whole) = Trained();

        Assert.NotNull(whole);

        foreach (var holders in new[] { 2, 3, 5, 12 })
        {
            var shards = Sharded(all, holders);

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
