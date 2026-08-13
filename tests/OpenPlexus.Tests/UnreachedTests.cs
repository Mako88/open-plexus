using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// A holder vanishing mid-round — <b>fork 53, and it is <c>DepartureTests</c>' mechanism
/// carried over rather than a new one.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The walk has solved this since the day the event bus was built.</b> A thought knows
/// how many of its routes are heading into each cluster, so a departure writes off exactly
/// those and the thought settles by its own accounting. What the commitment fleet had
/// instead was a denominator it could only watch: an asker knew it had asked twelve and
/// heard eight, and had nothing whatever to do about the four.
/// </para>
/// <para>
/// <b>And the missing term was not a death notice, which is why the asymmetry in
/// <see cref="Bus.IBus"/> Was right and still left a fleet stopped.</b> A holder that
/// crashed sends nothing, so a design waiting to be told would work for the polite
/// departures alone. What is observable without anybody's cooperation is the sender's own
/// failure to hand the question over — a refused connection, which is a fact rather than an
/// elapsed time, and which settles the matter for that ask exactly: a machine that was not
/// given a question cannot answer it.
/// </para>
/// <para>
/// <b>So what is closed here is every round after the one a holder died in, and not the
/// round it died in.</b> A holder that took the question and went before answering is owed
/// for the rest of the run, correctly — late and absent are one thing under C2, and only a
/// deadline separates them. That half is fork 62's and its condition is completeness rather
/// than a clock. The distinction matters because a fleet that recovers on the NEXT round is
/// a fleet that keeps learning through deaths, which is what twenty phones on one wifi need.
/// </para>
/// <para>
/// <b>And nothing here is a test of C2.</b> TCP does not reorder within a connection, so
/// what these say is that the write-off is exact and that a run survives it — and nothing
/// about lateness, which <see cref="Bus.HybridBus"/> is where to ask.
/// </para>
/// </remarks>
public sealed class UnreachedTests(ITestOutputHelper output)
{
    /// <summary>Six bits, which is the world step one is judged on.</summary>
    private const int Narrow = 2;

    /// <summary>A population trained on the multiplexer, and the dials it ran under.</summary>
    /// <remarks>
    /// <b>The defaults, because nothing here asks anything about the search.</b>
    /// <c>AskedTests</c> pins three repair dials so that a shard reliably has a nameable
    /// sub-scope left over, which is a precondition about what was LEARNT; these tests need
    /// only a population that fires, and pinning dials they do not read would be inheriting
    /// a fixture's reasons along with its numbers.
    /// </remarks>
    private static (CommittingSettings Dials, List<Commitment> All) Trained()
    {
        var dials = new CommittingSettings();
        var brain = new Brain(dials, seed: 1);

        new MultiplexerRun(new MultiplexerSettings { Address = Narrow }, brain, seed: 1)
            .Run(4000);

        return (dials, brain.Held.All.ToList());
    }

    /// <summary>
    /// <b>A holder the ask never reached is written off, and the round finishes.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The one that could not be written before: every earlier test of a death polls
    /// <see cref="Gathering.Heard"/> until it stops changing, because
    /// <see cref="Gathering.Everyone"/> was a promise nothing could keep once a machine had
    /// gone. Awaiting it here is the whole assertion — a poll would pass against a gathering
    /// that never completes at all.
    /// </para>
    /// <para>
    /// <b>And the denominator is unchanged, which is the half a write-off could silently
    /// destroy.</b> The obvious build subtracts the departed from what was asked, and then a
    /// fleet of four that lost one reads exactly like a fleet of three — the C3 instrument
    /// erased by the mechanism that was supposed to use it. <see cref="Gathering.Asked"/> is
    /// what was tried, <see cref="Gathering.Unreached"/> is what the sender watched fail to
    /// leave, and the two are reported apart.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task A_holder_the_ask_never_reached_is_written_off_and_the_round_finishes()
    {
        var (dials, all) = Trained();

        const int Holders = 4;

        await using var fleet = await Ported.OpenAsync(Fixture.Sharded(all, Holders), dials);

        await fleet.KillAsync(0);

        using var gathering = await fleet.Asker.AskAsync(Wanted.Counts);

        Assert.True(
            await Wired.ArrivedAsync(gathering.Everyone),
            $"the gathering never completed: {gathering.Heard} of {gathering.Asked} "
            + $"answered and {gathering.Unreached} were written off");

        Assert.Equal(Holders, gathering.Asked);
        Assert.Equal(Holders - 1, gathering.Heard);
        Assert.Equal(1, gathering.Unreached);

        // And it is not whole, which is the difference a finished round must still carry. A
        // vote over three survivors of four decided as though four had spoken is the failure
        // this instrument exists to make visible, and finishing the round is not permitted
        // to cost it.
        Assert.False(gathering.Whole);

        output.WriteLine(
            $"{gathering.Asked} asked | {gathering.Heard} answered | "
            + $"{gathering.Unreached} written off | whole: {gathering.Whole}");
    }

    /// <summary>
    /// <b>One departure writes off one holder and leaves the rest owed.</b>
    /// </summary>
    /// <remarks>
    /// <b>The companion, and without it every assertion in this file passes for a machine
    /// that abandons the whole fleet the moment anything fails.</b> <c>DepartureTests</c>
    /// carries the identical pair for the same reason. What makes it real here is that the
    /// survivors' answers are what completed the gathering — the write-off removed one claim
    /// and the other three were waited for, in full, exactly as they always were.
    /// </remarks>
    [Fact]
    public async Task One_departure_writes_off_one_holder_and_the_rest_are_still_waited_for()
    {
        var (dials, all) = Trained();

        const int Holders = 4;

        await using var fleet = await Ported.OpenAsync(Fixture.Sharded(all, Holders), dials);

        var before = fleet.Holders.Select(one => one.Answered).ToList();

        await fleet.KillAsync(2);

        using var gathering = await fleet.Asker.AskAsync(Wanted.Counts);

        Assert.True(
            await Wired.ArrivedAsync(gathering.Everyone),
            $"the gathering never completed: {gathering.Heard} of {gathering.Asked} "
            + $"answered and {gathering.Unreached} were written off");

        Assert.Equal(1, gathering.Unreached);

        // The silence is at the dead end rather than on the return path, and every other
        // holder took the question and answered it. A machine writing off whatever it felt
        // like would be indistinguishable from this one by any count on the asker.
        Assert.Equal(before[2], fleet.Holders[2].Answered);

        for (var at = 0; at < Holders; at++)
            if (at != 2)
                Assert.Equal(before[at] + 1, fleet.Holders[at].Answered);

        // And the merge still carries something, so what finished is a round with evidence
        // in it rather than an empty one that completed by giving up on everybody.
        Assert.True(gathering.Merged().Scopes > 0, "the survivors merged an empty table");
    }

    /// <summary>
    /// <b>Losing every holder finishes the round, and the vote comes back with no answer.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>DepartureTests</c>' second shape — <i>losing every cluster settles the thought</i>
    /// — put to the fleet, and it is the case the whole event mechanism was introduced for.
    /// Nothing here can reply, so before the write-off this round could only have been ended
    /// by a deadline, and a deadline is a constant nobody measured.
    /// </para>
    /// <para>
    /// <b>And it is the third outcome by a second road.</b> <c>AskedTests</c> arms
    /// <c>Abstain</c> by killing every advocate and leaving a crowd answering, which is the
    /// harder and more interesting arrangement; this reaches it by having nobody left to ask
    /// at all. Both are C3, and only one of them used to terminate.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Losing_every_holder_finishes_the_round_and_the_vote_has_no_expectation()
    {
        var (dials, all) = Trained();

        const int Holders = 3;

        await using var fleet = await Ported.OpenAsync(Fixture.Sharded(all, Holders), dials);

        IWorld<IReadOnlyList<int>> world =
            new Multiplexer(new MultiplexerSettings { Address = Narrow }, seed: 99);

        IReadOnlySet<Code> moment = new HashSet<Code>(
            new Bits(Multiplexer.Bit).Codify(world.Next().Seen));

        // THE VOTE BEFORE THE DEATHS, so the silence below is a change rather than a fleet
        // that never spoke. An abstention asserted without this is a moment nothing fires on.
        using (var living = await fleet.Asker.AskAsync(Wanted.Vote, moment))
        {
            Assert.True(await Wired.ArrivedAsync(living.Everyone), "the living fleet never answered");
            Assert.NotNull(living.Decide().Expects);
        }

        for (var at = 0; at < Holders; at++) await fleet.KillAsync(at);

        using var bereaved = await fleet.Asker.AskAsync(Wanted.Vote, moment);

        Assert.True(
            await Wired.ArrivedAsync(bereaved.Everyone),
            $"the gathering never completed with every holder gone: {bereaved.Heard} "
            + $"answered and {bereaved.Unreached} were written off");

        Assert.Equal(Holders, bereaved.Asked);
        Assert.Equal(0, bereaved.Heard);
        Assert.Equal(Holders, bereaved.Unreached);
        Assert.Null(bereaved.Decide().Expects);

        output.WriteLine(
            $"every one of {Holders} holders gone | {bereaved.Unreached} written off, "
            + "round finished, no expectation");
    }

    /// <summary>
    /// <b>A fleet goes on learning after a holder dies mid-run — the north star's blocker,
    /// and the reading that says it is gone.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every test above is one exchange. This is a whole learner: four machines, a death at
    /// the halfway mark, and four thousand more rounds that used to be unreachable — the run
    /// would have stopped at the first ask after the death, forever, on a fleet that was
    /// otherwise alive and idle.
    /// </para>
    /// <para>
    /// <b>The second half's accuracy is printed and barred low, because what is being
    /// asserted is that it ran.</b> A quarter of the population went with the machine that
    /// held it, and what that costs a curve is a measurement rather than a check — the fleet
    /// re-mints what it lost, from a placement that now has a hole in it, and how quickly is
    /// exactly the kind of number this file must not invent a threshold for. Four-way chance
    /// is a quarter; the bar says the survivors are learning and nothing more.
    /// </para>
    /// <para>
    /// <b>And the round the death lands in is still owed, which is why the kill is between
    /// runs rather than inside one.</b> A holder that took a question and died before
    /// answering it stops that round for good, and no amount of write-off reaches it — see
    /// this file's header, and fork 62. Killing between two runs asks the question this
    /// mechanism actually answers: whether a fleet that has LOST a machine can go on.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task A_fleet_goes_on_learning_after_a_holder_dies()
    {
        const int Holders = 4;
        const long Half = 4000;

        var dials = new CommittingSettings();

        var brain = new Brain(dials, seed: 1);
        var world = new Multiplexer(new MultiplexerSettings { Address = Narrow }, seed: 1);
        var trial = new Trial<IReadOnlyList<int>>(world, new Bits(Multiplexer.Bit), brain);

        await using var fleet = await Ported.OpenAsync(Holders, dials, seed: 1);
        var council = new Fleet(fleet.Asker, dials);

        var before = await Ran(trial.RunAsync(council, fleet.Held, Half), Holders, "before the death");

        Assert.Equal(Holders, council.Heard);

        var lost = fleet.Held[1].Count;

        await fleet.KillAsync(1);

        var after = await Ran(trial.RunAsync(council, fleet.Held, Half), Holders, "after the death");

        // And the denominator came down once, by observation. The first round after the
        // death asked four and heard three; every round after that asked the three that are
        // there, because a holder whose post failed comes off the roster. A fleet that had
        // quietly stopped asking somebody would read exactly like this -- which is why the
        // moment it happened is asserted above, on an ask that was tried and failed.
        Assert.Equal(Holders - 1, council.Asked);
        Assert.Equal(Holders - 1, council.Heard);

        Assert.True(after.Recent > 0.5,
            $"a fleet of {Holders} that lost one scored {after.Recent:F3} over the last "
            + "tenth, which is not far enough above four-way chance to be learning");

        output.WriteLine(
            $"{Holders} holders | {Half} rounds before, recent {before.Recent:F3} | "
            + $"holder 1 dies holding {lost} commitments | {Half} rounds after, "
            + $"recent {after.Recent:F3} | asked {council.Asked}, heard {council.Heard}");
    }

    /// <summary>
    /// <b>A holder that could not be reached is not asked again, and that is what makes a
    /// run possible rather than merely correct.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The number that put this line in the code, and it is the transport's rather than
    /// the design's.</b> A post to a machine that has closed its door costs a flat four
    /// seconds on loopback — a connect giving up, measured at 4.09s an ask over ten asks
    /// against 2.6ms with everybody alive. The write-off ends the round; it cannot end it
    /// before the transport admits the question did not go, so a fleet still asking a dead
    /// holder pays four seconds every round forever.
    /// </para>
    /// <para>
    /// <b>So the roster loses it, which is the walk's other half and was always part of the
    /// port.</b> A cluster death removes it from where routes are addressed; this removes a
    /// holder from where asks are addressed, reached by watching a post fail rather than by
    /// being told. What it costs is written down beside it: a machine that stayed up while
    /// one message to it was lost is out until it announces again.
    /// </para>
    /// <para>
    /// <b>And the cost is asserted as a shape rather than a duration.</b> A threshold in
    /// milliseconds fails the build on a busy machine, which this repo has a line about. The
    /// claim is that the second ask is not paying what the first one did, which is a
    /// comparison rather than a clock.
    /// </para>
    /// <para>
    /// <b>And that comparison is the platform's rather than the mechanism's, which it took a
    /// red shard to find out.</b> Four seconds is what a Windows loopback connect spends
    /// giving up; a Linux runner answers a closed port with a reset immediately, so the FIRST
    /// ask costs a millisecond and there is nothing left for the second to be half of. The
    /// check then fails on a machine where the fault it guards against cannot happen — which
    /// is the mirror of a check that cannot fire, and worse, because it reads as a defect.
    /// </para>
    /// <para>
    /// <b>So the counts are the assertion and the clock is a report wherever there is a clock
    /// to read.</b> <see cref="Gathering.Asked"/> coming down and
    /// <see cref="Gathering.Unreached"/> reading nought already say the dead holder was not
    /// asked again, exactly, on every platform — <i>anything asserting a COST must assert a
    /// count</i>, and the count was here all along.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task A_holder_that_could_not_be_reached_is_not_asked_again()
    {
        var (dials, all) = Trained();

        const int Holders = 3;

        await using var fleet = await Ported.OpenAsync(Fixture.Sharded(all, Holders), dials);

        await fleet.KillAsync(0);

        var clock = System.Diagnostics.Stopwatch.StartNew();

        using (var first = await fleet.Asker.AskAsync(Wanted.Counts))
        {
            Assert.True(await Wired.ArrivedAsync(first.Everyone), "the first ask never finished");

            // The dead holder is asked exactly once, which is the only way anybody could
            // find out. A roster dropping it without trying would be a machine deciding who
            // is alive from something other than an observation.
            Assert.Equal(Holders, first.Asked);
            Assert.Equal(1, first.Unreached);
        }

        var learning = clock.Elapsed.TotalMilliseconds;

        clock.Restart();

        using var second = await fleet.Asker.AskAsync(Wanted.Counts);

        Assert.True(await Wired.ArrivedAsync(second.Everyone), "the second ask never finished");

        // AND NOT AGAIN AFTER THAT. The denominator is the survivors now, nothing is written
        // off because nothing was tried and failed, and the round costs what a round costs.
        Assert.Equal(Holders - 1, second.Asked);
        Assert.Equal(0, second.Unreached);
        Assert.True(second.Whole);

        var after = clock.Elapsed.TotalMilliseconds;

        // And the clock is only asked where the first ask paid something. A platform whose
        // refused connect is instant leaves nothing for the second ask to be half of, and
        // demanding it there fails the build over a cost that does not exist. Which case
        // this run was is printed rather than swallowed, because a comparison that quietly
        // did not happen is a check that cannot fire.
        const double Payable = 100.0;

        if (learning > Payable)
            Assert.True(after < learning / 2,
                $"the second ask cost {after:F0} ms against the first's {learning:F0}, so "
                + "the dead holder is still being waited on");

        output.WriteLine(
            $"first ask {learning:F0} ms, asking {Holders} | second ask {after:F0} ms, "
            + $"asking {second.Asked} | "
            + (learning > Payable
                ? "the refused connect cost enough to compare"
                : $"the refused connect cost under {Payable:F0} ms here, so the counts "
                + "carry this and the clock does not"));
    }

    /// <summary>Runs a fleet, and fails rather than hanging if it stops.</summary>
    /// <param name="running">The run.</param>
    /// <param name="holders">How many machines, for the message.</param>
    /// <param name="when">Which half, for the message.</param>
    /// <remarks>
    /// <b>The experimenter's patience and never the machine's, which is <c>FleetTests</c>'
    /// Rule and is unchanged by any of this.</b> A holder that took a question and died
    /// still owes an answer forever, so a suite that inherited the wait would hang rather
    /// than fail. What the write-off removes is the case where a fleet stops with every
    /// surviving machine idle — not the general promise, which was never one.
    /// </remarks>
    private static async Task<Tally> Ran(Task<Tally> running, int holders, string when)
    {
        if (await Task.WhenAny(running, Task.Delay(Ported.Patience)) == running)
            return await running;

        Assert.Fail($"a fleet of {holders} never finished its rounds {when}");

        throw new InvalidOperationException("unreachable");
    }
}
