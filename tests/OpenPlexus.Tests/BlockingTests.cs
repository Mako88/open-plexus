using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Step 2's second half — <b>learning proportional to prediction ERROR, and the
/// blocking effect that a pure co-occurrence count cannot produce.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE READ PATH HAS BEEN GATED SINCE `Surprise` LANDED AND THE WRITE PATH NEVER
/// WAS.</b> The reason recorded in the code was that an expected onset must still
/// move the counts or the graph stops getting better at what it already predicts —
/// which is an argument about precision, and the price of it is <b>blocking</b>.
/// </para>
/// <para>
/// <b>KAMIN'S RESULT, AND IT IS THE CLASSIC FAILURE OF CONTIGUITY.</b> Train A to
/// predict X. Then present A and B together, still followed by X. B acquires almost
/// nothing — because X was already predicted, so there was no error for B to learn
/// from. A count of co-occurrence hands B the full association, since B and X really
/// did co-occur every time. <b>Rescorla and Wagner's whole claim is that learning is
/// proportional to prediction error</b>, and that is what this gate is.
/// </para>
/// <para>
/// <b>THE WEIGHT NEEDS NO DIAL.</b> The share of a moment's onsets that surprised is
/// the weight, so a wholly expected moment is worth nothing to learn from and a
/// wholly unexpected one is worth exactly what it always was — and scaling the WHOLE
/// occasion rather than picking codes out of it keeps <c>together &lt;= seen</c>.
/// </para>
/// </remarks>
public sealed class BlockingTests(ITestOutputHelper output)
{
    private static Code C(ulong value) => Fixture.C(value);

    private static readonly Code First = C(1);
    private static readonly Code Added = C(2);
    private static readonly Code Outcome = C(9);

    /// <summary>
    /// A machine whose sense hands over exactly the codes it is given.
    /// </summary>
    private sealed class Handed : IQuantizer<IReadOnlyCollection<Code>>
    {
        public byte Modality => 1;

        public IReadOnlyCollection<Code> Codify(IReadOnlyCollection<Code> frame) => frame;
    }

    /// <summary>
    /// Trains <c>First -> Outcome</c>, then adds <c>Added</c> alongside it.
    /// </summary>
    /// <remarks>
    /// <b>THE EXPECTATION IS SET BY HAND, and that is the honest way to do it.</b>
    /// What is under test is whether a MET expectation gates the write, not whether
    /// this graph is any good at forming one — using the walk's own guess would
    /// confound the two and leave a null unattributable.
    /// </remarks>
    private static async Task<Bench> TrainedAsync()
    {
        // THE WINDOW IS NAMED HERE BECAUSE THIS BENCH IS A SEQUENCE, and leaving it
        // to the brain's default of NOUGHT is what made every number in this file
        // read 0.00. A cue and its outcome are handed over as CONSECUTIVE moments,
        // so `First` and `Outcome` are never simultaneous and the only thing that
        // can join them is a carried one. With no window there is no edge to block,
        // no edge to keep, and the whole experiment measures an empty graph -- which
        // is precisely what `RhythmTests` asserts from the other side, that a span
        // of nought leaves that world with no edges at all.
        //
        // NOUGHT IS THE RIGHT DEFAULT AND THIS IS THE STATED EXCEPTION. See
        // `WalkSettings.Span`: it is a claim about the STREAM rather than a switch,
        // nought keeps every world's control valid, and a stream whose moments
        // genuinely follow one another says so. This one does.
        var dials = Fixture.Dials(stamina: 10.0) with { Span = 1 };

        var bench = new Bench(dials);

        // THE MACHINE CARRIES ITS OWN SURPRISE AND ITS OWN SPAN NOW. Both were
        // constructor arguments until 2026-08-04; the gate is unconditional and
        // the span comes off the settings, so there is nothing to hand in.
        var machine = new InputMachine<IReadOnlyCollection<Code>>(
            new MachineAddress("cue"), new Handed(), bench.Rendezvous,
            bench.Bus, bench.Ring, dials);

        var at = 0L;

        // PHASE ONE: the first cue alone, and the outcome is a surprise every time
        // because nothing is ever expected of it.
        for (var round = 0; round < 20; round++)
        {
            await machine.ObserveAsync([First], at++);
            await machine.ObserveAsync([Outcome], at++);
        }

        // PHASE TWO: the second cue arrives alongside the first, and the outcome is
        // now EXPECTED -- which is the whole of the experiment.
        for (var round = 0; round < 20; round++)
        {
            await machine.ObserveAsync([First, Added], at++);

            machine.Expects.Expect([Outcome]);
            await machine.ObserveAsync([Outcome], at++);
        }

        return bench;
    }


    // ---- THE CONTROL, AND WHY IT IS NO LONGER RUNNABLE ----------------------
    //
    // TWO TESTS STOOD ABOVE THIS ONE AND BOTH NEEDED AN UNGATED BENCH:
    // `A_count_of_co_occurrence_hands_the_added_cue_the_full_association` was the
    // contiguity control, and `And_gating_the_write_by_surprise_blocks_it` was the
    // comparison. Step 2's second half became unconditional on 2026-08-04, so
    // there is no ungated arm to build.
    //
    // WHAT THEY ESTABLISHED: with a pure co-occurrence count the added cue picked
    // up the FULL association — it really was adjacent to the outcome on every one
    // of the twenty later rounds, so the count was not wrong about the contiguity,
    // only about what it meant. Gating the write by surprise cut that to under
    // half. That is Kamin blocking, and it is a known empirical failure of
    // contiguity rather than a quirk of this design.
    //
    // WHAT SURVIVES BELOW is the companion, and it is the one that still has
    // something to say: blocking is a claim about the ADDED cue and not about
    // learning in general, so the first cue must keep what it earned in phase one.
    // Without it a gate that simply stopped writing would have passed.

    [Fact]
    public async Task And_the_first_cue_keeps_what_it_earned_before_the_second_arrived()
    {
        // THE COMPANION, AND WITHOUT IT THE TEST ABOVE PASSES FOR A GATE THAT
        // SIMPLY STOPPED WRITING. Blocking is a claim about the ADDED cue and not
        // about learning in general -- the first cue's association was built in
        // phase one, when every outcome was a surprise, and it must survive.
        using var bench = await TrainedAsync();

        var first = bench.Node(First).Together(Outcome);
        var added = bench.Node(Added).Together(Outcome);

        output.WriteLine($"gated: first->outcome {first:F2}, added->outcome {added:F2}");

        Assert.True(first > added,
            $"the cue that earned its association did not keep it: {first:F2} "
            + $"against the blocked cue's {added:F2}");

        Assert.True(first > 5.0,
            $"gating stopped the graph learning anything at all ({first:F2}), so "
            + "the blocking above is silence rather than selectivity");
    }

    // ---- AND WHAT IT COSTS ON A WORLD --------------------------------------
    //
    // `On_a_world_with_no_redundant_cue_it_can_only_cost` stood here and swept the
    // gate on and off over six seeds of `Rhythm`. It is gone with the arm.
    //
    // WHAT IT FOUND, and it is the reason a world is owed rather than a dial:
    // gating the write saved about a twentieth of the traffic and gave up rather
    // more than that in accuracy. THE REASON WAS THE WORLD AND NOT THE MECHANISM
    // — `Rhythm` shows ONE SYMBOL PER MOMENT, so there is never a second cue
    // standing beside a first, and blocking is a claim about exactly that
    // arrangement. There was nothing for the gate to block, so all that could be
    // measured was what it costs to stop reinforcing what is already predicted.
    //
    // The plan names the missing world: one where several cues arrive together and
    // only some carry the outcome.
}
