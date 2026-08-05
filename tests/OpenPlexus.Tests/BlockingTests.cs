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
    private static async Task<Bench> TrainedAsync(bool gated)
    {
        var bench = new Bench(Fixture.Dials(stamina: 10.0));

        var surprise = new Surprise();

        var machine = new InputMachine<IReadOnlyCollection<Code>>(
            new MachineAddress("cue"), new Handed(), bench.Rendezvous,
            bench.Bus, bench.Ring, Fixture.Dials(stamina: 10.0),
            span: 1, surprise: surprise, gated: gated);

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

            surprise.Expect([Outcome]);
            await machine.ObserveAsync([Outcome], at++);
        }

        return bench;
    }

    [Fact]
    public async Task A_count_of_co_occurrence_hands_the_added_cue_the_full_association()
    {
        // THE CONTROL, AND IT IS THE BEHAVIOUR THIS PROJECT HAS TODAY. `Added` and
        // `Outcome` were adjacent on every one of the twenty later rounds, so a
        // contiguity count says they are strongly associated -- and it is not wrong
        // about the contiguity, only about what it means.
        using var bench = await TrainedAsync(gated: false);

        var learnt = bench.Node(Added).Together(Outcome);

        output.WriteLine($"ungated: added->outcome {learnt:F2}");

        Assert.True(learnt > 15.0,
            $"the added cue picked up only {learnt:F2}, so this control is not "
            + "demonstrating the contiguity it exists to demonstrate");
    }

    [Fact]
    public async Task And_gating_the_write_by_surprise_blocks_it()
    {
        // THE CAPABILITY, AND IT IS WHAT STEP 2'S SECOND HALF IS ACTUALLY FOR. The
        // outcome was expected on every round the added cue was present, so those
        // moments carried no error -- and a system that learns from error alone has
        // nothing to write. The added cue stays a bystander.
        using var gated = await TrainedAsync(gated: true);
        using var plain = await TrainedAsync(gated: false);

        var blocked = gated.Node(Added).Together(Outcome);
        var free = plain.Node(Added).Together(Outcome);

        output.WriteLine($"gated {blocked:F2} against ungated {free:F2}");

        Assert.True(blocked < free / 2.0,
            $"gating the write did not block the added cue: {blocked:F2} against "
            + $"{free:F2}");
    }

    [Fact]
    public async Task And_the_first_cue_keeps_what_it_earned_before_the_second_arrived()
    {
        // THE COMPANION, AND WITHOUT IT THE TEST ABOVE PASSES FOR A GATE THAT
        // SIMPLY STOPPED WRITING. Blocking is a claim about the ADDED cue and not
        // about learning in general -- the first cue's association was built in
        // phase one, when every outcome was a surprise, and it must survive.
        using var bench = await TrainedAsync(gated: true);

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

    // ---- and what it costs on a world ---------------------------------------

    /// <summary>One arm on the stream, averaged over seeds.</summary>
    private static async Task<(double Accuracy, double Messages)> StreamAsync(bool gated)
    {
        int[] seeds = [1, 2, 3, 5, 8, 13];

        double accuracy = 0.0, messages = 0.0;

        foreach (var seed in seeds)
        {
            using var run = new Worlds.RhythmRun(
                new Worlds.RhythmSettings { Symbols = 12, Period = 5, Violations = 0.1 },
                Fixture.Dials(stamina: 3.0) with
                {
                    Span = 1, Surprising = true, Gated = gated,
                },
                seed);

            var result = await run.RunAsync(900);

            accuracy += result.Accuracy;
            messages += result.Messages;
        }

        return (accuracy / seeds.Length, messages / seeds.Length);
    }

    [Fact]
    public async Task On_a_world_with_no_redundant_cue_it_can_only_cost()
    {
        // THE PLAN PREDICTED THE PAYOFF WOULD BE COST, AND ON THIS WORLD IT IS
        // BARELY THAT. Gating the write saves about a twentieth of the traffic and
        // gives up rather more than that in accuracy.
        //
        // AND THE REASON IS THE WORLD RATHER THAN THE MECHANISM, which is the same
        // lesson `Choices` taught this morning. `Rhythm` shows ONE SYMBOL PER
        // MOMENT, so there is never a second cue standing beside a first -- and
        // blocking is a claim about exactly that arrangement. There is nothing here
        // for the gate to block, so all that is left to measure is what it costs
        // to stop reinforcing what you already predict.
        //
        // WHAT WOULD SHOW THE PAYOFF is a world where several cues arrive together
        // and only some of them carry the outcome. Nothing here is that world.
        var plain = await StreamAsync(gated: false);
        var gated = await StreamAsync(gated: true);

        output.WriteLine(
            $"plain  acc={plain.Accuracy:F4} msgs={plain.Messages:F0}");
        output.WriteLine(
            $"gated  acc={gated.Accuracy:F4} msgs={gated.Messages:F0}");

        Assert.True(gated.Messages < plain.Messages,
            $"gating the write stopped saving anything at all: "
            + $"{gated.Messages:F0} against {plain.Messages:F0}");

        Assert.True(gated.Accuracy < plain.Accuracy,
            $"gating the write now BUYS accuracy on a world with no redundant cue "
            + $"({gated.Accuracy:F4} against {plain.Accuracy:F4}), which the "
            + "explanation above does not cover and wants understanding");
    }
}
