using System.Diagnostics;
using System.Globalization;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Round across a socket — <b>fork 52's transport half</b>, and the first traffic in this
/// project that is not a thought.
/// </summary>
/// <remarks>
/// <para>
/// <b>The arithmetic half was closed without a wire</b> and said so in every file.
/// <c>SplitTests</c> proved the vote composes across holders and <c>SplitNamingTests</c>
/// proved merged counts recover the whole population's name; both carry a note that nothing
/// in them is late and nothing in them dies. This is where those two exchanges are put on
/// real ports between machines that share no object.
/// </para>
/// <para>
/// <b>And it is still not a test of C2</b>, which is the thing that will be assumed. TCP
/// does not reorder within a connection, so this exercises LESS adversity than
/// <see cref="HybridBus"/> does. Green here says the bytes and the routing are right.
/// </para>
/// <para>
/// <b>What is genuinely new here is C3.</b> Nothing in one process can die, so
/// <c>Abstain</c> reads zero for the same reason a check reads zero when it cannot fire —
/// an open defect in the plan since it was written. A holder whose machine has closed its
/// door is a death that actually happened, and the last test in this file is the third
/// outcome arriving.
/// </para>
/// <para>
/// <b>What is not re-proved is the merge itself.</b> Counts compose by integer addition,
/// so lateness and reordering provably cannot change what they add up to, and the death
/// threshold that costs rung five its name is measured over arrangements in
/// <c>SplitNamingTests</c> where it can be swept cheaply. Repeating either here would be a
/// slower way of asserting arithmetic.
/// </para>
/// </remarks>
public sealed class AskedTests(ITestOutputHelper output)
{
    /// <summary>
    /// Rounds, <b>and the five hundred past the last sweep</b> are the point of the number.
    /// </summary>
    /// <remarks>
    /// <b>A run ending on a sweep round</b> is read at its most exhausted, and this file's whole
    /// subject is what a trained population would name NEXT. At twenty thousand exactly,
    /// three seeds in eight have nothing left to say — so the assertions here stood on seed
    /// one happening to be one of the five that did, which is the single-seed ordering this
    /// repo's traps list already names. Five hundred rounds of repair past the last sweep
    /// rebuild a subject: six seeds in eight under the shipped naming and eight in eight when
    /// naming runs until refused, seed one included under both.
    /// </remarks>
    private const long Rounds = 20_500;

    /// <summary>
    /// Eleven bits, matching <c>SplitNamingTests</c> — <b>fork 34 says six mints nothing
    /// to split.</b>
    /// </summary>
    private const int Wide = 3;

    /// <summary>
    /// Six bits, matching <c>SplitTests</c>, which is the world step one is judged on.
    /// </summary>
    private const int Narrow = 2;

    /// <summary>
    /// A repair budget low enough that a third of the population cannot name alone —
    /// <b>this file's precondition, and the shipped default is past it.</b>
    /// </summary>
    /// <remarks>
    /// <b>The same number and the same reason as <c>SplitNamingTests</c></b>, which is why both
    /// say why rather than just saying 64. These tests ask what the WIRE costs rung
    /// five, so they need shards too small to certify a redundancy unaided — otherwise a
    /// holder names something before any bytes move and the exchange is credited with
    /// nothing. That is a property of how much repair ran, not of the sockets.
    /// </remarks>
    private const int Sparse = 64;

    /// <summary>
    /// The seed the window is read on — <b>pinned with the pair</b>, because it is part of the
    /// same precondition and was the part nobody wrote down.
    /// </summary>
    /// <remarks>
    /// <b>Seed one fell out of the window when the vote rule changed.</b> Under
    /// <see cref="Repairing.AfterFailure"/> the vote decides what repair may run on, so a
    /// readout change is a search change and *whole names, no shard names alone* moved with
    /// it. Four seeds in twelve satisfy it at this budget and timing — 4, 8, 9 and 10 — so a
    /// red here means taking another rather than re-tuning the pair, and
    /// <c>SplitNamingTests</c> pins the same one for the same reason.
    /// </remarks>
    private const int Subject = 4;

    /// <summary>A population trained on the multiplexer, and the dials it ran under.</summary>
    /// <param name="address">Address bits.</param>
    /// <remarks>
    /// <para>
    /// <b>The timing is pinned here</b> because this file is about the wire and not about the
    /// search. What these tests need is a population with a sub-scope worth naming, so
    /// that a shard failing to name it alone means something — and whether one exists is a
    /// property of the trained population, which every repair dial moves. Inheriting the
    /// default made a socket test depend on the search, and it went red the day the default
    /// changed with nothing wrong on either side of the wire.
    /// </para>
    /// <para>
    /// <b>And it is <see cref="Repairing.AfterFailure"/></b> rather than the shipped one, for
    /// the reason the precondition exists. That timing holds the larger population at
    /// eleven bits, so it reliably has structure left over once the run's own naming has
    /// taken what it wants. The pin is a fixture choice and says nothing about which timing
    /// is better; <c>RepairingTests</c> is where that is measured, and it finds naming
    /// alive under both on every seed.
    /// </para>
    /// <para>
    /// <b>And the pin did not reach the budget</b>, which is the same fault one dial along.
    /// The paragraph above was written the day the timing changed and it names a class of
    /// mistake — <i>whether a nameable sub-scope survives is a property of the trained
    /// population, which every repair dial moves</i> — while pinning exactly one member of
    /// that class. Raising the budget puts enough eligible scopes on each third that a
    /// holder names three things alone, and the precondition below wants nought.
    /// </para>
    /// </remarks>
    private static (CommittingSettings Dials, List<Commitment> All) Trained(
        int address)
    {
        var dials = new CommittingSettings
        {
            Repairing = Repairing.AfterFailure,
            Budget = Sparse,

            // And the forking rule, which is the third search dial to reach a wire test.
            // This file's precondition is a population with something LEFT to name, and
            // every dial that changes what repair builds moves it -- which is why the
            // timing and the budget are already pinned here. `Forking.Distinct` gives a
            // parent a different child per attempt and the population it leaves has
            // nothing in common with the one these counts were written against.
            Forking = Forking.Repeated,

            // And the root, for the same reason and one dial further out. This file's baseline
            // is what the WHOLE population names, and genesis minting the moment as a scope
            // leaves it with nothing left to name -- so the number every merge is read against
            // disappears. What splitting costs is the question; the root is not.
            Rooting = Rooting.Singly,
        };
        var brain = new Brain(dials, Subject);

        new MultiplexerRun(new MultiplexerSettings { Address = address }, brain, Subject)
            .Run(Rounds);

        return (dials, brain.Held.All.ToList());
    }

    /// <summary>
    /// Moments the population was never taught on, coded exactly as training coded them.
    /// </summary>
    /// <param name="address">Address bits.</param>
    /// <param name="many">How many to draw.</param>
    /// <remarks>
    /// <b>THROUGH <see cref="IWorld{TSeen}"/> rather than the world's own cues</b>, which
    /// is the answer-key-in-the-wrong-alphabet trap wearing a different hat: reading
    /// <c>Multiplexer.Assignment</c> directly skips the quantiser and produces moments the
    /// population has never been asked in that alphabet.
    /// </remarks>
    private static List<IReadOnlySet<Code>> Moments(int address, int many)
    {
        IWorld<IReadOnlyList<int>> world =
            new Multiplexer(new MultiplexerSettings { Address = address }, seed: 99);

        var sensing = new Bits(Multiplexer.Bit);

        var moments = new List<IReadOnlySet<Code>>(many);

        for (var draw = 0; draw < many; draw++)
            moments.Add(new HashSet<Code>(sensing.Codify(world.Next().Seen)));

        return moments;
    }

    // ---- what crosses ------------------------------------------------------

    /// <summary>
    /// <b>Merged counts cross real sockets and recover the whole population's name.</b>
    /// </summary>
    /// <remarks>
    /// The undistributed answer, reached by three machines that have never seen each
    /// other's commitments. <c>SplitNamingTests</c> asserts this over objects in one
    /// process; here every count is written to bytes, posted, read back and added up on a
    /// machine that holds nothing.
    /// </remarks>
    [Fact]
    public async Task Merged_counts_cross_real_sockets_and_name_what_the_whole_population_would()
    {
        var (dials, all) = Trained(Wide);

        var whole = Abstracting.Shared(all, dials);

        Assert.NotNull(whole);

        const int Holders = 3;

        await using var fleet = await Ported.OpenAsync(Fixture.Sharded(all, Holders), dials);

        // Alone they name nothing, asserted before the exchange. If a shard could name
        // something by itself this world does not show the problem being fixed, and the
        // line below would be crediting the wire with something free.
        foreach (var held in fleet.Held) Assert.Equal(0, held.Abstract());

        using var gathering = await fleet.Asker.AskAsync(Wanted.Counts);

        Assert.Equal(Holders, gathering.Asked);

        Assert.True(
            await Wired.ArrivedAsync(gathering.Everyone),
            $"only {gathering.Heard} of {gathering.Asked} holders answered over the wire");

        var merged = gathering.Merged();

        // The check that anything crossed at all. An empty table merges, names nothing and
        // would fail the line below for a reason that has nothing to do with the wire --
        // and `Recurrence` wrote exactly that shape of nothing before it grew a projection.
        Assert.True(merged.Written().Rows.Length > 0, "the merged table is empty");

        Assert.Equal(whole, Abstracting.Shared(merged, dials));

        output.WriteLine(
            $"{Holders} holders on {Holders + 1} ports | {merged.Scopes} scopes merged, "
            + $"{merged.Written().Rows.Length} rows | naming "
            + $"{Naming.Name(whole.Value).Value}, which is what the whole population named");
    }

    /// <summary>
    /// <b>A vote taken across three machines</b> is the vote one machine would have taken.
    /// </summary>
    /// <remarks>
    /// Bit-identical and not within a tolerance, because under
    /// the aggregate is a maximum and a maximum composes
    /// exactly — the same claim <c>SplitTests</c> makes over objects, now made over bytes
    /// that were serialised, posted, and reconstructed by a machine holding no commitments
    /// at all.
    /// </remarks>
    [Fact]
    public async Task A_vote_crossing_sockets_decides_exactly_as_one_population_would()
    {
        var (dials, all) = Trained(Narrow);

        // A fresh whole population rather than the trained one. The trained population
        // carries whatever naming table the run minted and the shards are given none, so a
        // moment folded through it would fire differently for a reason that is not the
        // wire. Built the same way as the shards, it differs from them in nothing but who
        // holds what -- which is the only difference this test is entitled to see.
        var whole = new Population(dials, seed: 1);

        foreach (var commitment in all) whole.Add(commitment);

        const int Holders = 3;

        await using var fleet = await Ported.OpenAsync(Fixture.Sharded(all, Holders), dials);

        var compared = 0;
        var contested = 0;

        foreach (var moment in Moments(Narrow, many: 60))
        {
            var firing = whole.Firing(moment);
            if (firing.IsDefaultOrEmpty) continue;

            var here = whole.Predict(firing);

            using var gathering = await fleet.Asker.AskAsync(Wanted.Vote, moment);

            Assert.True(
                await Wired.ArrivedAsync(gathering.Everyone),
                $"only {gathering.Heard} of {gathering.Asked} holders answered");

            var there = gathering.Decide(Deciding.Grounded);

            Assert.Equal(here.Expects, there.Expects);
            Assert.Equal(here.By, there.By);
            Assert.Equal(here.Weight, there.Weight);
            Assert.Equal(here.Margin, there.Margin);

            compared++;

            // Whether the split was ever load-bearing, which the equalities cannot say. A
            // moment where one holder speaks is merged by every rule alike, so a file full
            // of those would assert nothing and pass.
            if (gathering.Spoke > 1) contested++;
        }

        output.WriteLine($"{compared} votes crossed, {contested} genuinely spread");

        Assert.True(compared > 30, $"only {compared} moments fired anything");

        Assert.True(contested > 5,
            $"only {contested} moments put advocates on more than one machine, so the "
            + "merge was never asked to combine anything and this test is a tautology");
    }

    // ---- what a death costs ------------------------------------------------

    /// <summary>
    /// <b>A holder whose machine has gone is silence</b>, and the asker can see it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// C3 says a machine vanishing mid-thought is normal rather than an error, and until
    /// there were sockets nothing could vanish. The asker keeps the denominator — how many
    /// it asked — so a merge over the survivors is distinguishable from a merge over
    /// everyone, which <see cref="Population.Decide"/> deliberately cannot tell.
    /// </para>
    /// <para>
    /// <b>And nothing waits on a clock for it.</b> The dead holder never times out; the ask
    /// is watched failing to leave, which writes it off exactly, and the answer is whatever
    /// the survivors said. A build that awaited its holders would have decided this by the
    /// client's timeout, which is <i>a miss decided by a deadline</i> and carries a revival
    /// row saying never.
    /// </para>
    /// <para>
    /// <b>And this file polls for the count rather than awaiting the gathering</b>, which is
    /// left as it was on purpose. The poll was the only shape available before fork 53
    /// and it asserts the numerator and the denominator, which is what this test is about;
    /// that the round now FINISHES is a different claim and <c>UnreachedTests</c> is where it
    /// is made, against a gathering that would hang if the write-off were removed.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task A_dead_holder_is_asked_and_never_answers_and_the_asker_knows_the_difference()
    {
        var (dials, all) = Trained(Wide);

        const int Holders = 4;

        await using var fleet = await Ported.OpenAsync(Fixture.Sharded(all, Holders), dials);

        // What each holder had answered before the death -- one apiece, from the warm-up
        // ask that `Ported` throws away.
        var before = fleet.Holders.Select(one => one.Answered).ToList();

        await fleet.KillAsync(0);

        using var gathering = await fleet.Asker.AskAsync(Wanted.Counts);

        // Still asked, which is the point. A holder leaving is silent on this bus by
        // design -- a machine that crashed could not have sent a death notice, so a design
        // needing one would work only for the departures that were polite.
        Assert.Equal(Holders, gathering.Asked);

        Assert.True(
            await Wired.UntilAsync(() => gathering.Heard == Holders - 1),
            $"{gathering.Heard} of {gathering.Asked} answered, and three were expected");

        Assert.False(gathering.Whole);

        // And the silence is at the dead end rather than on the return path, which the
        // count above cannot say. A holder that answered and whose answer was lost and a
        // holder that never heard the ask look identical from the asker, and they are
        // completely different faults -- one is the wire and one is C3.
        Assert.Equal(before[0], fleet.Holders[0].Answered);

        for (var at = 1; at < Holders; at++)
            Assert.Equal(before[at] + 1, fleet.Holders[at].Answered);

        var over = Abstracting.Shared(gathering.Merged(), dials);

        output.WriteLine(
            $"{Holders} asked, {gathering.Heard} answered, {gathering.Merged().Scopes} "
            + $"scopes merged | survivors name "
            + $"{(over is { } one ? Naming.Name(one).Value.ToString(CultureInfo.InvariantCulture) : "nothing")}");

        // No bar on what the survivors name, and the reason is a measurement rather than
        // caution. `SplitNamingTests` swept this over every arrangement of deaths and
        // found that past about a quarter of holders gone the merge proposes a name the
        // whole population would not -- so a bar here would be asserting either that a
        // known failure does not happen or that it does, on one arrangement of one seed.
        // The merge is integer addition and the wire cannot change what it adds up to;
        // what this test is entitled to assert is the accounting above.
    }

    /// <summary>
    /// <b>A death taking the last advocate silences the vote</b>, which is `Abstain` ARMED.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The plan carries this as an open defect: <i>`Abstain` is unarmed in any run —
    /// nothing in one process can die, so C3's third outcome is exercised only by unit
    /// tests.</i> And it records that splitting the vote did not arm it either, because
    /// losing a holder changes an answer and never silences one while something else
    /// advocates.
    /// </para>
    /// <para>
    /// <b>So the moment is chosen rather than drawn</b>, and that is the only way to reach
    /// it. What is needed is a moment whose advocates all sit on machines that then
    /// die. Waiting for one to turn up by chance is waiting on a coincidence; finding the
    /// scarcest one by asking the shards locally and then killing exactly those holders is
    /// the same event, arranged. C4 permits it explicitly — the constraint is on the
    /// LEARNER, and nothing the machine does depends on what was read here.
    /// </para>
    /// <para>
    /// <b>And it takes more than one death</b>, which is the plan being right rather than this
    /// test being weak. The first version of this looked for a moment whose every
    /// advocate sat on ONE machine and found none in four hundred draws: at this width the
    /// population is dense enough that something always advocates from somewhere else,
    /// which is exactly why splitting the vote did not arm the third outcome. How many
    /// deaths it actually takes is printed, because that number is the finding.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task A_death_that_takes_the_last_advocate_leaves_the_vote_with_no_answer()
    {
        var (dials, all) = Trained(Narrow);

        // Twelve, so killing every advocate still leaves a crowd answering. The point is
        // not that a fleet went quiet -- that would be reachable by unplugging everything
        // -- it is that holders answered, in numbers, and none of them had anything to say.
        const int Holders = 12;

        var shards = Fixture.Sharded(all, Holders);

        var speakers = shards.Select(shard =>
        {
            var held = new Population(dials, seed: 1);
            foreach (var commitment in shard) held.Add(commitment);
            return held;
        }).ToList();

        IReadOnlySet<Code>? scarcest = null;
        var advocating = new List<int>();

        foreach (var moment in Moments(Narrow, many: 2000))
        {
            var speaking = Enumerable.Range(0, Holders)
                .Where(at => !speakers[at].Weigh(speakers[at].Firing(moment)).Silent)
                .ToList();

            // A moment nothing fires on is silence without a death, and it would pass every
            // assertion below while showing nothing at all. What is wanted is a vote that
            // had an answer and stopped having one.
            if (speaking.Count == 0) continue;

            if (scarcest is null || speaking.Count < advocating.Count)
                (scarcest, advocating) = (moment, speaking);

            if (advocating.Count == 1) break;
        }

        Assert.NotNull(scarcest);

        Assert.True(advocating.Count < Holders,
            $"every one of {Holders} holders advocates on the scarcest moment drawn, so "
            + "there is no death here that leaves anyone answering");

        await using var fleet = await Ported.OpenAsync(shards, dials);

        // THE VOTE BEFORE THE DEATHS, so what follows is a change rather than a machine
        // that never worked. An assertion that something went silent is worth nothing
        // unless it was speaking a moment earlier.
        using (var living = await fleet.Asker.AskAsync(Wanted.Vote, scarcest))
        {
            Assert.True(
                await Wired.ArrivedAsync(living.Everyone),
                $"only {living.Heard} of {living.Asked} answered before the deaths");

            Assert.Equal(advocating.Count, living.Spoke);
            Assert.NotNull(living.Decide(Deciding.Grounded).Expects);
        }

        foreach (var gone in advocating) await fleet.KillAsync(gone);

        var surviving = Holders - advocating.Count;

        using var bereaved = await fleet.Asker.AskAsync(Wanted.Vote, scarcest);

        Assert.Equal(Holders, bereaved.Asked);

        Assert.True(
            await Wired.UntilAsync(() => bereaved.Heard == surviving),
            $"{bereaved.Heard} of {bereaved.Asked} answered after the deaths, and "
            + $"{surviving} were expected");

        var vote = bereaved.Decide(Deciding.Grounded);

        // THE THIRD OUTCOME. Every surviving holder answered, none of them had anything to
        // advocate, and the vote comes back with no expectation at all -- which is not a
        // wrong answer and not a late one, and is the case the design has carried a name
        // for since before it could happen.
        Assert.Equal(0, bereaved.Spoke);
        Assert.Null(vote.Expects);

        output.WriteLine(
            $"{advocating.Count} of {Holders} holders advocated on the scarcest moment "
            + $"in 2000 draws | after killing them: {bereaved.Heard} of {bereaved.Asked} "
            + $"answered, {bereaved.Spoke} spoke, no expectation");
    }

    // ---- what distance costs -----------------------------------------------

    /// <summary>
    /// <b>What a round of asks costs on a real wire</b> — fork 56, measured rather than
    /// priced.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Fork 56 priced the repair gate's query at about nine asks a round, all askable at
    /// once, so one round trip. That was arithmetic over a message count; this is a clock
    /// over loopback, and loopback is the floor rather than the answer — a LAN adds its
    /// own delay and the internet adds a great deal more.
    /// </para>
    /// <para>
    /// <b>No bar</b>, because a duration is not reproducible and a threshold on one fails the
    /// build on a busy machine. This project already has a line about a wall clock
    /// turning reproducibility red. The number is the finding; the only assertion is that
    /// the instrument had something to measure.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task What_a_round_of_asks_costs_over_a_socket()
    {
        var (dials, all) = Trained(Narrow);

        var moments = Moments(Narrow, many: 100);

        output.WriteLine("holders | asks | ms an ask, scatter to whole gathering");

        foreach (var holders in new[] { 1, 3, 9 })
        {
            await using var fleet = await Ported.OpenAsync(Fixture.Sharded(all, holders), dials);

            var clock = Stopwatch.StartNew();
            var asks = 0;

            foreach (var moment in moments)
            {
                using var gathering = await fleet.Asker.AskAsync(Wanted.Vote, moment);

                Assert.True(
                    await Wired.ArrivedAsync(gathering.Everyone),
                    $"only {gathering.Heard} of {gathering.Asked} answered");

                asks++;
            }

            clock.Stop();

            output.WriteLine(
                $"{holders,7} | {asks,4} | {clock.Elapsed.TotalMilliseconds / asks,38:F2}");

            Assert.Equal(moments.Count, asks);
        }

        // And the shape of the number is the finding rather than its size. If nine holders
        // cost about what one holder costs, the fan-out is genuinely in flight at once and
        // the depth is one round trip; if it grows with the count, the scatter is a queue
        // wearing a broadcast's name -- which is what `BroadcastAsync` does today on the
        // thinking path, awaiting each post in turn against its own documentation.
    }
}
