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
/// <b>THE TWO DEATHS ARE NOT ONE DEATH, WHICH IS THE WHOLE REASON THIS FILE EXISTS BESIDE
/// THAT ONE.</b> A machine whose door is shut refuses the connection, so the sender watches
/// the question fail to leave and writes it off exactly — no clock, no politeness, no guess.
/// A machine that accepted the question and went is silent in a way nothing can observe:
/// late and absent are one thing under C2, and the only mechanism that separates them is a
/// deadline, which this project carries a revival row against saying never.
/// </para>
/// <para>
/// <b>SO THE ANSWER IS NOT TO OBSERVE HARDER, IT IS TO NOT NEED THAT MACHINE.</b> Partition
/// the population into slots and give each slot R machines holding the identical shard. A
/// round is complete when every SLOT has spoken or been written off entirely, rather than
/// every holder — so a slot survives its own member dying mid-question and the round
/// finishes on evidence that is complete rather than on evidence that is timely.
/// </para>
/// <para>
/// <b>AND THE REPLICAS COST NOTHING TO KEEP IN SYNC, WHICH IS WHY THIS IS AFFORDABLE AT
/// ALL.</b> Every machine is told the same moment and the same settlement, and where a
/// commitment is placed is a fact about the commitment — so two machines in one slot mint
/// the same children independently and stay identical with no message between them. That
/// they do is asserted below rather than assumed, because it is also a free check on fork
/// 12 across a wire.
/// </para>
/// <para>
/// <b>THE DEATH IS A MUTED HOLDER RATHER THAN A RACED KILL, AND THAT IS A MEASUREMENT
/// DECISION.</b> See <see cref="Ported.Mute"/>: killing a machine mid-round means winning a
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
    /// <b>A HOLDER THAT TOOK THE QUESTION AND WENT QUIET STOPS THE ROUND FOR GOOD.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE CONTROL, AND WITHOUT IT EVERY TEST BELOW IS A CLAIM THAT SOMETHING WAS FIXED
    /// WITH NO EVIDENCE THAT IT WAS EVER BROKEN.</b> This is the same fleet with one machine
    /// a slot, which is what fork 53 shipped — and the write-off it ships cannot reach this
    /// at all, because there is nothing to write off: the ask was handed over, acknowledged,
    /// and nobody is coming back.
    /// </para>
    /// <para>
    /// <b>AND <see cref="Gathering.Unreached"/> READING NOUGHT IS THE ASSERTION THAT SAYS
    /// SO.</b> A run that stopped because the post failed would be fork 53 not working; this
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
    /// <b>A SLOT WHOSE OTHER MACHINE ANSWERS FINISHES THE ROUND, AND THE DEATH COSTS
    /// NOTHING.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same silence as the control above, on a fleet where the shard it was holding is
    /// held somewhere else too. Nothing was observed about the quiet machine and nothing had
    /// to be — completeness is a question about the POPULATION, and the population is all
    /// present.
    /// </para>
    /// <para>
    /// <b>AND THE SECOND REPLICA'S ANSWER IS DROPPED RATHER THAN ADDED, WHICH IS THE HALF A
    /// COMPLETION COULD SILENTLY DESTROY.</b> <see cref="Gathering.Merged"/> and
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

        // ONE VOICE A SLOT COUNTED, AND THE OTHER SLOT'S SECOND MACHINE ECHOING IS WHAT
        // SAYS THE REPLICAS ARE REAL. A fleet declared with two machines a slot where one
        // never speaks passes every death test by being lucky about which machine died.
        Assert.Equal(2, gathering.Heard);
        Assert.Equal(1, gathering.Echoed);
        Assert.Equal(0, gathering.Unreached);

        // AND IT IS NOT WHOLE, because one machine that was asked did not answer. Finishing
        // the round is not permitted to cost the instrument that says so.
        Assert.False(gathering.Whole);

        output.WriteLine(
            $"2 slots of 2, one machine muted | {gathering.Heard} slots heard, "
            + $"{gathering.Echoed} echoed, {gathering.Unreached} written off | round finished");
    }

    /// <summary>
    /// <b>AND NOTHING IS DROPPED WHERE THERE IS NOTHING TO DROP, WHICH IS R=1 BEING THE OLD
    /// MACHINE EXACTLY.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE BUDGET AGAINST THE WAY THIS CHANGE COULD BE WRONG AND STILL LOOK RIGHT.</b> A
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
    /// <b>A FLEET GOES ON LEARNING WHILE A MACHINE IT IS STILL ASKING NEVER ANSWERS — the
    /// round a phone dies inside, and the last hard blocker on the north star.</b>
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
    /// <b>AND THE DENOMINATOR DOES NOT COME DOWN HERE, WHICH IS THE DIFFERENCE FROM FORK 53
    /// SAID IN A NUMBER.</b> A refused connection takes a holder off the roster; a holder
    /// that accepts and is quiet stays on it forever, correctly, because nothing has been
    /// observed about it. The fleet asks four and hears two and finishes anyway.
    /// </para>
    /// <para>
    /// <b>THE SCORE IS PRINTED AND BARRED LOW, for the reason its neighbour gives.</b> What
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

        var before = await Ran(trial.RunAsync(council, fleet.Held, Half), "before the silence");

        Assert.Equal(Slots * Replicas, council.Asked);
        Assert.Equal(Slots, council.Heard);

        var lost = fleet.Held[0].Count;

        fleet.Mute(0);

        var after = await Ran(trial.RunAsync(council, fleet.Held, Half), "after the silence");

        // STILL ASKED AND STILL NOT HEARD FROM, every round to the end of the run. A fleet
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
    /// <b>TWO MACHINES IN ONE SLOT HOLD THE SAME POPULATION, HAVING SENT EACH OTHER
    /// NOTHING.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE PROPERTY THE WHOLE RUNG RESTS ON, AND IT IS A FREE CHECK ON FORK 12 ACROSS A
    /// WIRE.</b> Redundancy that had to be kept in sync would be a coordinator, which is the
    /// thing this design does not have; what makes a slot affordable is that identical
    /// evidence produces identical populations, so a replica is a copy nobody copied.
    /// </para>
    /// <para>
    /// <b>AND IT IS ASSERTED ON THE IDENTITIES RATHER THAN ON THE COUNT.</b> Two populations
    /// of the same SIZE holding different rules is exactly what a divergence would look like
    /// from a count, and a commitment's identity derives from its scope — so equal identity
    /// sets is the strongest statement available without a rule crossing a wire, which C1
    /// forbids.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Two_machines_in_one_slot_hold_the_same_population_having_sent_nothing()
    {
        const int Slots = 2;
        const int Replicas = 2;

        var (trial, raised, council) = await RaisedAsync(Slots, Replicas);

        await using var fleet = raised;

        await Ran(trial.RunAsync(council, fleet.Held, 2000), "with everybody answering");

        for (var slot = 0; slot < Slots; slot++)
        {
            var first = Identities(fleet.Held[(slot * Replicas) + 0]);

            for (var copy = 1; copy < Replicas; copy++)
            {
                var other = Identities(fleet.Held[(slot * Replicas) + copy]);

                Assert.True(first.SetEquals(other),
                    $"slot {slot}'s replicas diverged: {first.Count} against {other.Count} "
                    + $"commitments, {first.Except(other).Count()} held only by the first");
            }
        }

        // AND THE SLOTS ARE NOT ALL HOLDING ONE THING, or the check above is satisfied by a
        // fleet where the placement never split anything.
        Assert.NotEqual(Identities(fleet.Held[0]), Identities(fleet.Held[Replicas]));

        output.WriteLine(
            $"{Slots} slots of {Replicas} | "
            + string.Join(
                " | ",
                Enumerable.Range(0, Slots)
                    .Select(slot => $"slot {slot}: {fleet.Held[slot * Replicas].Count} held")));
    }

    /// <summary>Brings up a partitioned fleet on the multiplexer, with a learner over it.</summary>
    /// <param name="slots">How many partitions of the population.</param>
    /// <param name="replicas">How many machines hold each one.</param>
    /// <returns>The trial, the fleet the caller must dispose, and the council over it.</returns>
    /// <remarks>
    /// <b>THE FLEET IS HANDED BACK RATHER THAN DISPOSED HERE, which is what the duplication
    /// budget costs and it is worth it.</b> Two tests wrote these six lines identically and
    /// the check refused the second copy — rightly, because a difference between them would
    /// read as a difference the replication caused.
    /// </remarks>
    private static async Task<(Trial<IReadOnlyList<int>> Trial, Ported Fleet, Fleet Council)>
        RaisedAsync(int slots, int replicas)
    {
        var dials = new CommittingSettings();

        var brain = new Brain(dials, seed: 1);
        var world = new Multiplexer(new MultiplexerSettings { Address = Narrow }, seed: 1);
        var trial = new Trial<IReadOnlyList<int>>(world, new Bits(Multiplexer.Bit), brain);

        var fleet = await Ported.OpenAsync(slots, replicas, dials, seed: 1);

        return (trial, fleet, new Fleet(fleet.Asker, dials));
    }

    /// <summary>Every commitment a population holds, by name.</summary>
    private static HashSet<Code> Identities(Population held) =>
        [.. held.All.Select(one => one.Identity)];

    /// <summary>Runs a fleet, and fails rather than hanging if it stops.</summary>
    /// <param name="running">The run.</param>
    /// <param name="when">Which half, for the message.</param>
    /// <remarks>
    /// <b>THE EXPERIMENTER'S PATIENCE AND NEVER THE MACHINE'S, WHICH IS <c>FleetTests</c>'
    /// RULE AND IS UNCHANGED BY ANY OF THIS.</b> A slot every one of whose machines went
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
