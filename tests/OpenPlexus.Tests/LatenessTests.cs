using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// A fleet under C2 made real — <b>fork 52's other half, and the one `Posted` structurally
/// cannot answer.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>EVERY DISTRIBUTED NUMBER IN THIS PROJECT IS OVER TCP, AND TCP DOES NOT REORDER WITHIN
/// A CONNECTION.</b> <c>FleetTests</c> says so in its own remarks and is right: green there
/// means the bytes, the routing and the arithmetic are correct, and says nothing whatever
/// about lateness. C2 says messages are late, jittered and out of order, and the whole
/// design rests on that being survivable — <b>which nothing had checked with a learner
/// attached.</b>
/// </para>
/// <para>
/// <b>THE WALK'S DELETION IS WHY THIS EXISTS NOW RATHER THAN EARLIER.</b>
/// <see cref="Lateness"/> had exactly one caller, a sweep measuring per-hop delay against a
/// thought's DEPTH — and a fleet round is two round trips rather than a depth, so that
/// measurement did not carry over and went with the walk. <c>DeadCodeTests</c> is what said
/// so out loud: the C2 injector was left with no caller at all, which would have made
/// <i>the constraints were all written for this</i> a claim with nothing behind it.
/// </para>
/// <para>
/// <b>A SIMULATED CONSTRAINT CAN BE HARSHER THAN THE REAL ONE, AND HERE THAT IS THE
/// POINT.</b> This repo's trap list carries that sentence as a warning about reading a
/// green <c>HybridBus</c> run as evidence about a network. Read the other way round it is
/// the design: the simulator is where C2 is exercised, and the socket is where it is
/// exercised that the bytes are right.
/// </para>
/// </remarks>
public sealed class LatenessTests(ITestOutputHelper output)
{
    /// <summary>Six bits, the world step one is judged on — as <c>FleetTests</c> uses.</summary>
    private const int Narrow = 2;

    /// <summary>
    /// Six hundred, and the number is the finding rather than a convenience.
    /// </summary>
    /// <remarks>
    /// <b>LATENESS COSTS WALL CLOCK PER ROUND AND NOT PER MESSAGE, WHICH IS WHY THIS IS NOT
    /// THREE THOUSAND.</b> A round's deliveries all go out at once and the round waits on
    /// its SLOWEST, so a fifth of messages delayed 25ms means most ROUNDS pay 25ms — twice,
    /// once for the vote and once for the settlement. Three thousand rounds ran past two
    /// minutes at <c>asked=3, heard=3</c>: every gathering closed and the clock was the only
    /// thing that ran out, which is the design behaving exactly as claimed and a test that
    /// could not be asserted on.
    /// </remarks>
    private const long Rounds = 600;

    private const int Holders = 3;

    /// <summary>
    /// A fifth of deliveries held a long way back.
    /// </summary>
    /// <remarks>
    /// <b>A SMALL SHARE DELAYED A LOT, which is <see cref="Lateness"/>'s own argument and
    /// the shape that actually stresses settling.</b> Delaying everything a little measures
    /// the scheduler; what a real network adds is a few messages arriving LONG after their
    /// siblings, and that is the case a gathering has to survive.
    /// </remarks>
    private static Lateness LateBy =>
        new(Share: 0.2, Delay: TimeSpan.FromMilliseconds(25), Seed: 7);

    /// <summary>
    /// One fleet on one simulated bus, run to the end.
    /// </summary>
    /// <param name="late">Lateness to inject, or nothing for the control.</param>
    /// <param name="seed">The world's generator and every holder's.</param>
    /// <remarks>
    /// <b>ONE BUS RATHER THAN N SOCKETS, WHICH IS THE WHOLE REASON THIS CAN BE ASKED.</b>
    /// Delay injected into <see cref="Posted"/> would be delay injected into an HTTP client,
    /// which is a different thing to measure and slower by orders of magnitude. Here the
    /// transport is a dictionary and a dispatcher, so the ONLY difference between the two
    /// arms is when a delivery runs.
    /// </remarks>
    private static async Task<(Tally Tally, long Delayed, int Asked, int Heard)> RunAsync(
        Lateness? late, int seed)
    {
        var dials = new CommittingSettings();

        var bus = new HybridBus(late);

        // FAULTS ARE THROWN RATHER THAN COLLECTED. A delivery that threw would leave a
        // gathering short forever, and a run that hangs is a worse answer than a run that
        // fails -- see `Fixture.Patience`, which exists for the same reason.
        bus.Faults += failure => throw failure;

        var holding = new List<Population>(Holders);
        var handles = new List<IDisposable>();

        for (var at = 0; at < Holders; at++)
        {
            var mine = (ulong)at;

            var held = new Population(dials, seed)
            {
                Places = one => one.Identity.Value % (ulong)Holders == mine,
            };

            holding.Add(held);
            handles.Add(bus.Subscribe(new Holder(new MachineAddress($"holder-{at}"), held, bus)));
        }

        var asker = new Asker(new MachineAddress("asker"), bus);
        handles.Add(bus.Subscribe(asker));

        var council = new Fleet(asker, dials);

        // A BRAIN IS HANDED IN AND ITS POPULATION IS NOT THE ONE THAT LEARNS, exactly as
        // `FleetTests` has it: `Trial` holds the world and the front end, and `RunAsync`
        // asks the COUNCIL rather than the brain beside it. Same world, same seed, same
        // front end, same dials across both arms -- the lateness is the only difference.
        var brain = new Brain(dials, seed);

        var world = new Multiplexer(new MultiplexerSettings { Address = Narrow }, seed);
        var trial = new Trial<IReadOnlyList<int>>(world, new Bits(Multiplexer.Bit), brain);

        try
        {
            var running = trial.RunAsync(council, holding, Rounds);

            // THE EXPERIMENTER'S PATIENCE AND NEVER THE MACHINE'S, exactly as `FleetTests`
            // has it. A fleet waits on its gathering forever by design -- correct, and a
            // suite that inherited it would hang instead of failing.
            if (await Task.WhenAny(running, Task.Delay(Fixture.Patience)) != running)
                Assert.Fail(
                    $"a fleet of {Holders} never finished {Rounds} rounds under "
                    + $"{(late is null ? "no lateness" : "lateness")} — it asked "
                    + $"{council.Asked} and heard {council.Heard}");

            return (await running, bus.Delayed, council.Asked, council.Heard);
        }
        finally
        {
            foreach (var handle in handles) handle.Dispose();
        }
    }

    /// <summary>
    /// <b>A FIFTH OF THE TRAFFIC ARRIVES LATE AND THE RUN IS BIT-IDENTICAL.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE ARM IS ASSERTED TO HAVE RUN BEFORE ANYTHING ELSE IS READ, and that ordering is
    /// the point rather than tidiness.</b> A jitter arm that delayed nothing is a control
    /// wearing the arm's name — this repo has shipped that exact failure, which is why
    /// <see cref="HybridBus.Delayed"/> is counted at all. A green test here with
    /// <c>Delayed</c> at nought would be the strongest possible evidence for a claim it
    /// never examined.
    /// </para>
    /// <para>
    /// <b>AND IDENTICAL IS A STRONGER RESULT THAN <i>STILL LEARNS</i>, WITH A MECHANISM
    /// BEHIND IT RATHER THAN A HOPE.</b> <see cref="Fleet.AskAsync"/> awaits
    /// <c>gathering.Everyone</c> before it decides anything, so nothing in a round is read
    /// until every holder has answered — <b>a round is a BARRIER</b>, and within one, arrival
    /// order cannot reach the outcome. Delay therefore moves the clock and nothing else,
    /// which is what <i>lateness is survivable</i> turns out to mean here.
    /// </para>
    /// <para>
    /// <b>SO THIS CANNOT SHOW WHAT IT LOOKS LIKE IT SHOWS, AND THAT IS THE HALF WORTH
    /// WRITING DOWN.</b> C2 says messages are late, jittered AND OUT OF ORDER. Because a
    /// round is a barrier, no message from one round can arrive during the next — so the
    /// simulator's reordering never reaches the learner, and out-of-order delivery is not
    /// tested here by anything. What is tested is lateness, which is the half a barrier is
    /// exposed to.
    /// </para>
    /// <para>
    /// <b>AND THE COST IS WALL CLOCK PER ROUND, WHICH A BARRIER MAKES INEVITABLE.</b> A round
    /// waits on its slowest delivery, so one late message costs the whole round — the fleet
    /// is paced by its unluckiest holder, twice a round. That is the price of the property
    /// above rather than a defect, and it is what fork 62's slots would buy back.
    /// </para>
    /// <para>
    /// <b>WHAT WOULD REFUTE IT: the two tallies differing at all.</b> Under a barrier they
    /// cannot, so a difference means something has started reading a round before it closed
    /// — which would be a real change to what C2 can do to this design, and would deserve
    /// finding out this way rather than as a flake somewhere else.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Lateness_costs_the_clock_and_changes_not_one_answer()
    {
        var late = await RunAsync(LateBy, seed: 1);
        var control = await RunAsync(late: null, seed: 1);

        output.WriteLine(
            $"late     | delayed={late.Delayed} right={late.Tally.Right} "
            + $"wrong={late.Tally.Wrong} recent={late.Tally.Recent:F4} rounds={late.Tally.Rounds}");

        output.WriteLine(
            $"on-time  | delayed={control.Delayed} right={control.Tally.Right} "
            + $"wrong={control.Tally.Wrong} recent={control.Tally.Recent:F4} "
            + $"rounds={control.Tally.Rounds}");

        Assert.True(late.Delayed > 0,
            "nothing was actually held back, so this measured the control twice and called "
            + "one of them the arm — see `Lateness`, which counts for exactly this reason");

        // AND THE CONTROL IS ASSERTED TO BE ONE, which is the mirror of the line above.
        Assert.Equal(0L, control.Delayed);

        // THE DENOMINATOR, WHICH IS WHERE A GATHERING THAT COULD NOT CLOSE WOULD SHOW
        // FIRST -- it would come up short here rather than score badly, and a fleet that
        // quietly stopped asking one holder learns from the rest perfectly well.
        Assert.Equal(Holders, late.Asked);
        Assert.Equal(Holders, late.Heard);

        // THE WHOLE TALLY, RATHER THAN A THRESHOLD ON ONE FIGURE. A bar like *above chance*
        // would pass for a fleet that lateness had damaged and left merely competent; a
        // record that compares every field it has is the assertion the barrier argument
        // actually licenses.
        Assert.Equal(control.Tally, late.Tally);
    }
}
