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
/// Learning across a socket — <b>fork 52's transport half, and the first traffic in this
/// project that is not a thought.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE ARITHMETIC HALF WAS CLOSED WITHOUT A WIRE AND SAID SO IN EVERY FILE.</b>
/// <c>SplitTests</c> proved the vote composes across holders and <c>SplitNamingTests</c>
/// proved merged counts recover the whole population's name; both carry a note that nothing
/// in them is late and nothing in them dies. This is where those two exchanges are put on
/// real ports between machines that share no object.
/// </para>
/// <para>
/// <b>AND IT IS STILL NOT A TEST OF C2, WHICH IS THE THING THAT WILL BE ASSUMED.</b> TCP
/// does not reorder within a connection, so this exercises LESS adversity than
/// <see cref="HybridBus"/> does. Green here says the bytes and the routing are right.
/// </para>
/// <para>
/// <b>WHAT IS GENUINELY NEW HERE IS C3.</b> Nothing in one process can die, so
/// <c>Abstain</c> reads zero for the same reason a check reads zero when it cannot fire —
/// an open defect in the plan since it was written. A holder whose machine has closed its
/// door is a death that actually happened, and the last test in this file is the third
/// outcome arriving.
/// </para>
/// <para>
/// <b>WHAT IS NOT RE-PROVED IS THE MERGE ITSELF.</b> Counts compose by integer addition,
/// so lateness and reordering provably cannot change what they add up to, and the death
/// threshold that costs rung five its name is measured over arrangements in
/// <c>SplitNamingTests</c> where it can be swept cheaply. Repeating either here would be a
/// slower way of asserting arithmetic.
/// </para>
/// </remarks>
public sealed class AskedTests(ITestOutputHelper output)
{
    private const long Rounds = 20000;

    /// <summary>
    /// Eleven bits, matching <c>SplitNamingTests</c> — <b>fork 34 says six mints nothing
    /// to split.</b>
    /// </summary>
    private const int Wide = 3;

    /// <summary>
    /// Six bits, matching <c>SplitTests</c>, which is the world step one is judged on.
    /// </summary>
    private const int Narrow = 2;

    /// <summary>A population trained on the multiplexer, and the dials it ran under.</summary>
    /// <param name="address">Address bits.</param>
    /// <param name="weighing">How advocates for one expectation combine.</param>
    private static (CommittingSettings Dials, List<Commitment> All) Trained(
        int address, Weighing weighing = Weighing.Summing)
    {
        var dials = new CommittingSettings { Weighing = weighing };
        var brain = new Brain(dials, seed: 1);

        new MultiplexerRun(new MultiplexerSettings { Address = address }, brain, seed: 1)
            .Run(Rounds);

        return (dials, brain.Held.All.ToList());
    }

    /// <summary>
    /// Moments the population was never taught on, coded exactly as training coded them.
    /// </summary>
    /// <param name="address">Address bits.</param>
    /// <param name="many">How many to draw.</param>
    /// <remarks>
    /// <b>THROUGH <see cref="IWorld{TSeen}"/> RATHER THAN THE WORLD'S OWN CUES</b>, which
    /// is the answer-key-in-the-wrong-alphabet trap wearing a different hat: reading
    /// <c>Multiplexer.Round</c> directly skips the quantiser and produces moments the
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

    /// <summary>
    /// One asker and several holders, each in its own <see cref="Posted"/> on its own port.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>SEPARATE BUSES AND SEPARATE PORTS IS WHAT MAKES THIS A DISTRIBUTED TEST AT
    /// ALL.</b> Two holders on one bus would share a dictionary, which is the arrangement
    /// every measurement in this project has been taken under and the one
    /// <see cref="Posted"/> exists to stop being the only one.
    /// </para>
    /// <para>
    /// <b>THE ASKER OPENS FIRST AND THE ANNOUNCE-BACK IS WHY THAT IS SAFE.</b> A machine
    /// announces when it opens and a peer that is not up yet drops it, so whoever opens
    /// first would otherwise tell nobody where it is — and every answer to it would have
    /// nowhere to go. <see cref="Posted"/> answers an announcement with one, so the roster
    /// converges whatever order the fleet came up in.
    /// </para>
    /// </remarks>
    private sealed class Fleet : IAsyncDisposable
    {
        private readonly List<IDisposable> _handles = [];
        private readonly List<Posted> _machines = [];
        private Posted _asking = null!;

        /// <summary>The machine that puts the questions.</summary>
        public Asker Asker { get; private set; } = null!;

        /// <summary>What each holder holds, in holder order.</summary>
        public List<Population> Held { get; } = [];

        /// <summary>The holders themselves, in holder order.</summary>
        public List<Holder> Holders { get; } = [];

        /// <summary>Brings up an asker and one holder per shard, and waits for the roster.</summary>
        /// <param name="shards">A population, already placed on holders.</param>
        /// <param name="dials">The brain's numbers.</param>
        public static async Task<Fleet> OpenAsync(
            IReadOnlyList<List<Commitment>> shards, CommittingSettings dials)
        {
            var fleet = new Fleet();

            var hosts = Enumerable.Range(0, shards.Count + 1).Select(_ => Wired.Free()).ToList();
            var peers = hosts.Select(one => new Peer(one)).ToList();

            fleet._asking = new Posted(hosts[0], peers);
            fleet.Asker = new Asker(new MachineAddress("asker"), fleet._asking);
            fleet._handles.Add(fleet._asking.Subscribe(fleet.Asker));

            for (var at = 0; at < shards.Count; at++)
            {
                var bus = new Posted(hosts[at + 1], peers);
                var held = new Population(dials, seed: 1);

                foreach (var commitment in shards[at]) held.Add(commitment);

                var holder = new Holder(new MachineAddress($"holder-{at}"), held, bus);

                fleet._handles.Add(bus.Subscribe(holder));
                fleet._machines.Add(bus);
                fleet.Held.Add(held);
                fleet.Holders.Add(holder);
            }

            await fleet._asking.OpenAsync().ConfigureAwait(false);

            foreach (var bus in fleet._machines) await bus.OpenAsync().ConfigureAwait(false);

            if (!await Wired.UntilAsync(() => fleet._asking.Holding.Count == shards.Count)
                .ConfigureAwait(false))
                throw new InvalidOperationException(
                    $"only {fleet._asking.Holding.Count} of {shards.Count} holders announced");

            // AND ONE ASK THROWN AWAY, WHICH IS THE BARRIER THE ROSTER ALONE CANNOT BE.
            // Knowing where the holders are says the outbound half converged; nothing
            // observable says the holders know where to send an answer, and that is the
            // direction the announce-back exists for. A round trip completing is the only
            // honest check of it, and doing it here keeps every measurement below from
            // paying for the first one.
            using var warming = await fleet.Asker
                .AskAsync(Wanted.Counts).ConfigureAwait(false);

            if (!await Wired.ArrivedAsync(warming.Everyone).ConfigureAwait(false))
                throw new InvalidOperationException(
                    $"{warming.Heard} of {warming.Asked} answers came back, so the return "
                    + "path never converged");

            return fleet;
        }

        /// <summary>
        /// One holder's machine closes its door — <b>C3, and not a polite departure.</b>
        /// </summary>
        /// <param name="which">Which holder dies.</param>
        /// <remarks>
        /// <b>THE WHOLE MACHINE RATHER THAN THE SUBSCRIPTION, BECAUSE THOSE ARE DIFFERENT
        /// DEATHS.</b> Dropping a subscription leaves a listener that accepts the ask and
        /// routes it nowhere; closing the door refuses the connection, which is what a
        /// phone going into a tunnel does. The second is the one the design claims to
        /// survive, and it is the harsher of the two.
        /// </remarks>
        public async Task KillAsync(int which)
        {
            await _machines[which].DisposeAsync().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            foreach (var handle in _handles) handle.Dispose();

            foreach (var bus in _machines) await bus.DisposeAsync().ConfigureAwait(false);

            await _asking.DisposeAsync().ConfigureAwait(false);
        }
    }

    // ---- what crosses ------------------------------------------------------

    /// <summary>
    /// <b>MERGED COUNTS CROSS REAL SOCKETS AND RECOVER THE WHOLE POPULATION'S NAME.</b>
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

        await using var fleet = await Fleet.OpenAsync(Fixture.Sharded(all, Holders), dials);

        // ALONE THEY NAME NOTHING, ASSERTED BEFORE THE EXCHANGE. If a shard could name
        // something by itself this world does not show the problem being fixed, and the
        // line below would be crediting the wire with something free.
        foreach (var held in fleet.Held) Assert.Equal(0, held.Abstract());

        using var gathering = await fleet.Asker.AskAsync(Wanted.Counts);

        Assert.Equal(Holders, gathering.Asked);

        Assert.True(
            await Wired.ArrivedAsync(gathering.Everyone),
            $"only {gathering.Heard} of {gathering.Asked} holders answered over the wire");

        var merged = gathering.Merged();

        // THE CHECK THAT ANYTHING CROSSED AT ALL. An empty table merges, names nothing and
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
    /// <b>A VOTE TAKEN ACROSS THREE MACHINES IS THE VOTE ONE MACHINE WOULD HAVE TAKEN.</b>
    /// </summary>
    /// <remarks>
    /// Bit-identical and not within a tolerance, because under
    /// <see cref="Weighing.Strongest"/> the aggregate is a maximum and a maximum composes
    /// exactly — the same claim <c>SplitTests</c> makes over objects, now made over bytes
    /// that were serialised, posted, and reconstructed by a machine holding no commitments
    /// at all.
    /// </remarks>
    [Fact]
    public async Task A_vote_crossing_sockets_decides_exactly_as_one_population_would()
    {
        var (dials, all) = Trained(Narrow, Weighing.Strongest);

        // A FRESH WHOLE POPULATION RATHER THAN THE TRAINED ONE. The trained population
        // carries whatever naming table the run minted and the shards are given none, so a
        // moment folded through it would fire differently for a reason that is not the
        // wire. Built the same way as the shards, it differs from them in nothing but who
        // holds what -- which is the only difference this test is entitled to see.
        var whole = new Population(dials, seed: 1);

        foreach (var commitment in all) whole.Add(commitment);

        const int Holders = 3;

        await using var fleet = await Fleet.OpenAsync(Fixture.Sharded(all, Holders), dials);

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

            var there = gathering.Decide(Weighing.Strongest);

            Assert.Equal(here.Expects, there.Expects);
            Assert.Equal(here.By, there.By);
            Assert.Equal(here.Weight, there.Weight);
            Assert.Equal(here.Margin, there.Margin);

            compared++;

            // WHETHER THE SPLIT WAS EVER LOAD-BEARING, WHICH THE EQUALITIES CANNOT SAY. A
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
    /// <b>A HOLDER WHOSE MACHINE HAS GONE IS SILENCE, AND THE ASKER CAN SEE IT.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// C3 says a machine vanishing mid-thought is normal rather than an error, and until
    /// there were sockets nothing could vanish. The asker keeps the denominator — how many
    /// it asked — so a merge over the survivors is distinguishable from a merge over
    /// everyone, which <see cref="Population.Decide"/> deliberately cannot tell.
    /// </para>
    /// <para>
    /// <b>AND NOTHING WAITS ON A CLOCK FOR IT.</b> The dead holder is never written off and
    /// never times out; it simply does not appear in the gathering, and the answer is
    /// whatever the survivors said. A build that awaited its holders would have decided
    /// this by the client's timeout, which is <i>a miss decided by a deadline</i> and
    /// carries a revival row saying never.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task A_dead_holder_is_asked_and_never_answers_and_the_asker_knows_the_difference()
    {
        var (dials, all) = Trained(Wide);

        const int Holders = 4;

        await using var fleet = await Fleet.OpenAsync(Fixture.Sharded(all, Holders), dials);

        // WHAT EACH HOLDER HAD ANSWERED BEFORE THE DEATH -- one apiece, from the warm-up
        // ask that `Fleet` throws away.
        var before = fleet.Holders.Select(one => one.Answered).ToList();

        await fleet.KillAsync(0);

        using var gathering = await fleet.Asker.AskAsync(Wanted.Counts);

        // STILL ASKED, WHICH IS THE POINT. A holder leaving is silent on this bus by
        // design -- a machine that crashed could not have sent a death notice, so a design
        // needing one would work only for the departures that were polite.
        Assert.Equal(Holders, gathering.Asked);

        Assert.True(
            await Wired.UntilAsync(() => gathering.Heard == Holders - 1),
            $"{gathering.Heard} of {gathering.Asked} answered, and three were expected");

        Assert.False(gathering.Whole);

        // AND THE SILENCE IS AT THE DEAD END RATHER THAN ON THE RETURN PATH, which the
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

        // NO BAR ON WHAT THE SURVIVORS NAME, and the reason is a measurement rather than
        // caution. `SplitNamingTests` swept this over every arrangement of deaths and
        // found that past about a quarter of holders gone the merge proposes a name the
        // whole population would not -- so a bar here would be asserting either that a
        // known failure does not happen or that it does, on one arrangement of one seed.
        // The merge is integer addition and the wire cannot change what it adds up to;
        // what this test is entitled to assert is the accounting above.
    }

    /// <summary>
    /// <b>A DEATH TAKING THE LAST ADVOCATE SILENCES THE VOTE, WHICH IS `Abstain` ARMED.</b>
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
    /// <b>SO THE MOMENT IS CHOSEN RATHER THAN DRAWN, AND THAT IS THE ONLY WAY TO REACH
    /// IT.</b> What is needed is a moment whose advocates all sit on machines that then
    /// die. Waiting for one to turn up by chance is waiting on a coincidence; finding the
    /// scarcest one by asking the shards locally and then killing exactly those holders is
    /// the same event, arranged. C4 permits it explicitly — the constraint is on the
    /// LEARNER, and nothing the machine does depends on what was read here.
    /// </para>
    /// <para>
    /// <b>AND IT TAKES MORE THAN ONE DEATH, WHICH IS THE PLAN BEING RIGHT RATHER THAN THIS
    /// TEST BEING WEAK.</b> The first version of this looked for a moment whose every
    /// advocate sat on ONE machine and found none in four hundred draws: at this width the
    /// population is dense enough that something always advocates from somewhere else,
    /// which is exactly why splitting the vote did not arm the third outcome. How many
    /// deaths it actually takes is printed, because that number is the finding.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task A_death_that_takes_the_last_advocate_leaves_the_vote_with_no_answer()
    {
        var (dials, all) = Trained(Narrow, Weighing.Strongest);

        // TWELVE, SO KILLING EVERY ADVOCATE STILL LEAVES A CROWD ANSWERING. The point is
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
                .Where(at => !speakers[at].Speak(speakers[at].Firing(moment)).Silent)
                .ToList();

            // A MOMENT NOTHING FIRES ON IS SILENCE WITHOUT A DEATH, and it would pass every
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

        await using var fleet = await Fleet.OpenAsync(shards, dials);

        // THE VOTE BEFORE THE DEATHS, so what follows is a change rather than a machine
        // that never worked. An assertion that something went silent is worth nothing
        // unless it was speaking a moment earlier.
        using (var living = await fleet.Asker.AskAsync(Wanted.Vote, scarcest))
        {
            Assert.True(
                await Wired.ArrivedAsync(living.Everyone),
                $"only {living.Heard} of {living.Asked} answered before the deaths");

            Assert.Equal(advocating.Count, living.Spoke);
            Assert.NotNull(living.Decide(Weighing.Strongest).Expects);
        }

        foreach (var gone in advocating) await fleet.KillAsync(gone);

        var surviving = Holders - advocating.Count;

        using var bereaved = await fleet.Asker.AskAsync(Wanted.Vote, scarcest);

        Assert.Equal(Holders, bereaved.Asked);

        Assert.True(
            await Wired.UntilAsync(() => bereaved.Heard == surviving),
            $"{bereaved.Heard} of {bereaved.Asked} answered after the deaths, and "
            + $"{surviving} were expected");

        var vote = bereaved.Decide(Weighing.Strongest);

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
    /// <b>WHAT A ROUND OF ASKS COSTS ON A REAL WIRE — fork 56, measured rather than
    /// priced.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Fork 56 priced the repair gate's query at about nine asks a round, all askable at
    /// once, so one round trip. That was arithmetic over a message count; this is a clock
    /// over loopback, and loopback is the floor rather than the answer — a LAN adds its
    /// own delay and the internet adds a great deal more.
    /// </para>
    /// <para>
    /// <b>NO BAR, BECAUSE A DURATION IS NOT REPRODUCIBLE AND A THRESHOLD ON ONE FAILS THE
    /// BUILD ON A BUSY MACHINE.</b> This project already has a line about a wall clock
    /// turning reproducibility red. The number is the finding; the only assertion is that
    /// the instrument had something to measure.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task What_a_round_of_asks_costs_over_a_socket()
    {
        var (dials, all) = Trained(Narrow, Weighing.Strongest);

        var moments = Moments(Narrow, many: 100);

        output.WriteLine("holders | asks | ms an ask, scatter to whole gathering");

        foreach (var holders in new[] { 1, 3, 9 })
        {
            await using var fleet = await Fleet.OpenAsync(Fixture.Sharded(all, holders), dials);

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

        // AND THE SHAPE OF THE NUMBER IS THE FINDING RATHER THAN ITS SIZE. If nine holders
        // cost about what one holder costs, the fan-out is genuinely in flight at once and
        // the depth is one round trip; if it grows with the count, the scatter is a queue
        // wearing a broadcast's name -- which is what `BroadcastAsync` does today on the
        // thinking path, awaiting each post in turn against its own documentation.
    }
}
