using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// A holder that TOOK the question and never answered — <b>fork 62, and the half
/// <c>UnreachedTests</c> is explicit about not reaching.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The two deaths are not one death</b>, which is the whole reason this file exists beside
/// that one. A machine whose door is shut refuses the connection, so the sender watches
/// the question fail to leave and writes it off exactly — no clock, no politeness, no guess.
/// A machine that accepted the question and went is silent in a way nothing can observe:
/// late and absent are one thing under C2, and the only mechanism that separates them is a
/// deadline, which this project carries a revival row against saying never.
/// </para>
/// <para>
/// <b>So the answer is not to observe harder</b>, it is to not need that machine. Partition
/// the population into slots and give each slot R machines holding the identical shard. A
/// round is complete when every SLOT has spoken or been written off entirely, rather than
/// every holder — so a slot survives its own member dying mid-question and the round
/// finishes on evidence that is complete rather than on evidence that is timely.
/// </para>
/// <para>
/// <b>And the replicas cost nothing to keep in sync</b>, which is why this is affordable at
/// all. Every machine is told the same moment and the same settlement, and where a
/// commitment is placed is a fact about the commitment — so two machines in one slot mint
/// the same children independently and stay identical with no message between them. That
/// they do is asserted below rather than assumed, because it is also a free check on fork
/// 12 across a wire.
/// </para>
/// <para>
/// <b>The death is a muted holder rather than a raced kill</b>, and that is a measurement
/// decision. See <see cref="Ported.Mute"/>: killing a machine mid-round means winning a
/// race against a socket, so which round it landed in would vary and a green suite would be
/// evidence about scheduling. A holder that accepts every question and answers none is the
/// same condition with the timing removed and made permanent, which is strictly harsher.
/// </para>
/// </remarks>
public sealed class SlotTests(ITestOutputHelper output)
{
    /// <summary>Six bits, which is the world step one is judged on.</summary>
    private const int Narrow = 2;

    /// <summary>
    /// <b>A holder that took the question and went quiet</b> stops the round for good.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The control</b>, and without it every test below is a claim that something was fixed
    /// with no evidence that it was ever broken. This is the same fleet with one machine
    /// a slot, which is what fork 53 shipped — and the write-off it ships cannot reach this
    /// at all, because there is nothing to write off: the ask was handed over, acknowledged,
    /// and nobody is coming back.
    /// </para>
    /// <para>
    /// <b>AND <see cref="Gathering.Unreached"/> reading nought is the assertion that says
    /// so.</b> A run that stopped because the post failed would be fork 53 not working; this
    /// stops with every message delivered, which is the case only a slot reaches.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task A_holder_that_took_the_question_and_went_quiet_stops_a_round_with_no_replica()
    {
        var dials = new CommittingSettings();

        await using var fleet = await Ported.OpenAsync(slots: 3, replicas: 1, dials, seed: 1);

        fleet.Mute(0);

        using var gathering = await fleet.Asker.AskAsync(Wanted.Counts);

        Assert.False(
            await Wired.ArrivedAsync(gathering.Everyone),
            "a slot of one whose only holder never answers completed anyway, so something "
            + "decided a missing holder by something other than an answer");

        Assert.Equal(3, gathering.Asked);
        Assert.Equal(2, gathering.Heard);

        // NOTHING WAS WRITTEN OFF, which is what makes this the OTHER death. The question
        // left, was accepted and was acknowledged; fork 53's signal never fires.
        Assert.Equal(0, gathering.Unreached);
        Assert.False(gathering.Whole);

        output.WriteLine(
            $"3 slots of 1 | {gathering.Heard} answered, {gathering.Unreached} written off "
            + "| the round never finished, which is correct");
    }

    /// <summary>
    /// <b>A slot whose other machine answers finishes the round</b>, and the death costs
    /// nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same silence as the control above, on a fleet where the shard it was holding is
    /// held somewhere else too. Nothing was observed about the quiet machine and nothing had
    /// to be — completeness is a question about the POPULATION, and the population is all
    /// present.
    /// </para>
    /// <para>
    /// <b>And the second replica's answer is dropped rather than added</b>, which is the half a
    /// completion could silently destroy. <see cref="Gathering.Merged"/> and
    /// <see cref="Gathering.Added"/> both ADD what they are handed, so two identical shards
    /// counted twice is one machine's scopes weighed double — the exact fault
    /// <see cref="Gathering"/>'s own header describes for a duplicated message, arriving by
    /// deployment instead. <see cref="Gathering.Echoed"/> is what makes the drop visible
    /// rather than silent.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task A_slot_whose_other_machine_answers_finishes_the_round()
    {
        var dials = new CommittingSettings();

        await using var fleet = await Ported.OpenAsync(slots: 2, replicas: 2, dials, seed: 1);

        fleet.Mute(0);

        using var gathering = await fleet.Asker.AskAsync(Wanted.Counts);

        Assert.True(
            await Wired.ArrivedAsync(gathering.Everyone),
            $"the gathering never completed: {gathering.Heard} of {gathering.Asked} "
            + $"answered, {gathering.Echoed} echoed, {gathering.Unreached} written off");

        Assert.Equal(4, gathering.Asked);

        // One voice a slot counted, and the other slot's second machine echoing is what
        // says the replicas are real. A fleet declared with two machines a slot where one
        // never speaks passes every death test by being lucky about which machine died.
        Assert.Equal(2, gathering.Heard);
        Assert.Equal(0, gathering.Unreached);

        // WAITED FOR RATHER THAN READ, because the round does not wait for it. `Everyone`
        // completes when every SLOT has spoken once, and the echo is the second replica of a
        // slot that has already spoken -- so the answer that proves the replicas exist is by
        // construction still in flight when the round it belongs to finishes. Reading it on
        // the next line passed on this machine and went red once on a runner, which is a check
        // whose truth depends on a race it does not control.
        Assert.True(
            await Wired.UntilAsync(() => gathering.Echoed == 1),
            $"the second replica never echoed: {gathering.Heard} of {gathering.Asked} "
            + $"answered, {gathering.Echoed} echoed, {gathering.Unreached} written off. A "
            + "fleet declared with two machines a slot where one never answers is a fleet of "
            + "one wearing redundancy's name");

        // AND IT IS NOT WHOLE, because one machine that was asked did not answer. Finishing
        // the round is not permitted to cost the instrument that says so.
        Assert.False(gathering.Whole);

        output.WriteLine(
            $"2 slots of 2, one machine muted | {gathering.Heard} slots heard, "
            + $"{gathering.Echoed} echoed, {gathering.Unreached} written off | round finished");
    }

    /// <summary>
    /// <b>And nothing is dropped where there is nothing to drop</b>, which is R=1 BEING THE OLD
    /// MACHINE EXACTLY.
    /// </summary>
    /// <remarks>
    /// <b>The budget against the way this change could be wrong</b> and still look right. A
    /// slot condition that quietly deduplicated on an unpartitioned fleet would silence
    /// holders on every measurement this project has ever taken over a wire, and every one of
    /// them would still finish and still score. What says it did not is that a fleet of slots
    /// of one hears everybody and echoes nobody.
    /// </remarks>
    [Fact]
    public async Task A_fleet_of_slots_of_one_hears_everybody_and_echoes_nobody()
    {
        var dials = new CommittingSettings();

        await using var fleet = await Ported.OpenAsync(slots: 4, replicas: 1, dials, seed: 1);

        using var gathering = await fleet.Asker.AskAsync(Wanted.Counts);

        Assert.True(await Wired.ArrivedAsync(gathering.Everyone), "the gathering never completed");

        Assert.Equal(4, gathering.Asked);
        Assert.Equal(4, gathering.Heard);
        Assert.Equal(0, gathering.Echoed);
        Assert.True(gathering.Whole);
    }

    /// <summary>
    /// <b>A fleet learns on while a machine it still asks never answers</b> — the round a
    /// phone dies inside, and the last hard blocker on the north star.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>UnreachedTests</c>' whole-learner test kills a machine BETWEEN two runs, because
    /// the round a death lands in was owed forever and no write-off could reach it. This
    /// mutes one mid-run and keeps asking it every round for the rest of the run: the ask is
    /// accepted every time and no answer ever comes, which is the death that cannot be
    /// observed, arriving four thousand times instead of once.
    /// </para>
    /// <para>
    /// <b>And the denominator does not come down here</b>, which is the difference from fork 53
    /// said in a number. A refused connection takes a holder off the roster; a holder
    /// that accepts and is quiet stays on it forever, correctly, because nothing has been
    /// observed about it. The fleet asks four and hears two and finishes anyway.
    /// </para>
    /// <para>
    /// <b>The score is printed and barred low</b>, for the reason its neighbour gives. What
    /// is being asserted is that the run happened at all — it could not have before this, on
    /// a fleet where every surviving machine was alive and idle. Four-way chance is a
    /// quarter.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task A_fleet_goes_on_learning_while_a_holder_it_still_asks_never_answers()
    {
        const int Slots = 2;
        const int Replicas = 2;
        const long Half = 4000;

        var (trial, raised, council) = await RaisedAsync(Slots, Replicas);

        await using var fleet = raised;

        var before = await Ran(trial.RunAsync(fleet.Held, Half), "before the silence");

        Assert.Equal(Slots * Replicas, council.Asked);
        Assert.Equal(Slots, council.Heard);

        var lost = fleet.Held[0].Count;

        fleet.Mute(0);

        var after = await Ran(trial.RunAsync(fleet.Held, Half), "after the silence");

        // Still asked and still not heard from, every round to the end of the run. A fleet
        // that had quietly stopped asking it would read as three machines and would be
        // fork 53's mechanism doing this rather than fork 62's.
        Assert.Equal(Slots * Replicas, council.Asked);
        Assert.Equal(Slots, council.Heard);

        Assert.True(after.Recent > 0.5,
            $"a fleet of {Slots} slots of {Replicas} scored {after.Recent:F3} over the last "
            + "tenth with one machine silent, which is not far enough above four-way chance "
            + "to be learning");

        output.WriteLine(
            $"{Slots} slots of {Replicas} | {Half} rounds before, recent {before.Recent:F3} | "
            + $"holder 0 goes quiet holding {lost} commitments | {Half} rounds after, "
            + $"recent {after.Recent:F3} | asked {council.Asked}, heard {council.Heard}");
    }

    /// <summary>
    /// <b>Two machines in one slot do not hold the same population</b>, and what separates them
    /// is the completeness condition itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This was written to assert identity</b> and it failed on the first machine that was not
    /// mine. The plan's claim is that replicas fed one stream mint the same children
    /// independently and stay identical, so redundancy costs coordination nothing — 88
    /// commitments against 89 with twenty held by only one of them, on a runner, where the
    /// same code is bit-identical here. A green local run said the claim held; it said the
    /// machine was fast.
    /// </para>
    /// <para>
    /// <b>And the mechanism is the slot</b>, which makes this a cost of fork 62 rather than a bug
    /// in it. A round finishes when ONE replica answers, so the other is never waited on
    /// — and asks are dispatched per arrival rather than in order, so a replica running
    /// behind can be handed the next moment before the last settlement. Identical evidence
    /// converges; evidence in a different ORDER does not, and nothing was making the order
    /// the same except this machine being quick enough that it never came up.
    /// </para>
    /// <para>
    /// <b>So what is asserted is what survives</b>: both replicas learn, and the slots are
    /// different from each other. The divergence is printed rather than barred, because a
    /// threshold on how far two populations drift is a prediction and this has been measured
    /// exactly once. What it costs is fork 62's open half — a failover replica is a similar
    /// population and not the same one.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Two_machines_in_one_slot_drift_apart_because_only_one_is_waited_for()
    {
        const int Slots = 2;
        const int Replicas = 2;

        var (trial, raised, council) = await RaisedAsync(Slots, Replicas);

        await using var fleet = raised;

        await Ran(trial.RunAsync(fleet.Held, 2000), "with everybody answering");

        for (var slot = 0; slot < Slots; slot++)
        {
            var first = Identities(fleet.Held[slot * Replicas]);

            // BOTH REPLICAS LEARNT, which is the half a drift could hide. A machine that was
            // never asked anything holds nothing and agrees with nobody, and that would read
            // here as the largest divergence of all rather than as a fleet with a dead arm.
            Assert.NotEmpty(first);

            for (var copy = 1; copy < Replicas; copy++)
            {
                var other = Identities(fleet.Held[(slot * Replicas) + copy]);

                Assert.NotEmpty(other);

                var shared = first.Intersect(other).Count();

                output.WriteLine(
                    $"slot {slot}, replica {copy}: {first.Count} against {other.Count} "
                    + $"commitments, {shared} shared, {first.Count - shared} held only by "
                    + "the first");
            }
        }

        // And the slots are not all holding one thing, or every reading above is taken on a
        // fleet whose placement never split anything.
        Assert.NotEqual(Identities(fleet.Held[0]), Identities(fleet.Held[Replicas]));
    }

    /// <summary>Brings up a partitioned fleet on the multiplexer, with a learner over it.</summary>
    /// <param name="slots">How many partitions of the population.</param>
    /// <param name="replicas">How many machines hold each one.</param>
    /// <returns>The trial, the fleet the caller must dispose, and the council over it.</returns>
    /// <remarks>
    /// <b>The fleet is handed back rather than disposed here</b>, which is what the duplication
    /// budget costs and it is worth it. Two tests wrote these six lines identically and
    /// the check refused the second copy — rightly, because a difference between them would
    /// read as a difference the replication caused.
    /// </remarks>
    private static async Task<(Bench Bench, Ported Fleet, Fleet Council)>
        RaisedAsync(int slots, int replicas)
    {
        var dials = new CommittingSettings();

        var fleet = await Ported.OpenAsync(slots, replicas, dials, seed: 1);

        var (trial, council) = Fixture.Multiplexed(fleet, dials, Narrow);

        return (trial, fleet, council);
    }

    /// <summary>Every commitment a population holds, by name.</summary>
    private static HashSet<Code> Identities(Population held) =>
        [.. held.All.Select(one => one.Identity)];

    /// <summary>Runs a fleet, and fails rather than hanging if it stops.</summary>
    /// <param name="running">The run.</param>
    /// <param name="when">Which half, for the message.</param>
    /// <remarks>
    /// <b>The experimenter's patience and never the machine's</b>, which is <c>FleetTests</c>'
    /// Rule and is unchanged by any of this. A slot every one of whose machines went
    /// quiet still owes an answer forever, so a suite that inherited the wait would hang
    /// rather than fail.
    /// </remarks>
    private static async Task<Tally> Ran(Task<Tally> running, string when)
    {
        if (await Task.WhenAny(running, Task.Delay(Ported.Patience)) == running)
            return await running;

        Assert.Fail($"a fleet never finished its rounds {when}");

        throw new InvalidOperationException("unreachable");
    }
}
