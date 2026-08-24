using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What a moment carrying what has just LEFT is worth, and what it costs.
/// </summary>
/// <remarks>
/// <para>
/// <b>The mechanism for <i>it can say what does NOT hold</i>.</b> Every scope this branch has
/// ever held is a conjunction of things PRESENT, so a belief whose negation is unsayable is
/// one the machine cannot be precise about — and the population is already a disjunction of
/// conjunctions, so one absence literal is the whole propositional gap.
/// </para>
/// <para>
/// <b>Two worlds, and one of them should gain nothing.</b> <c>Rhythm</c> is a stream where
/// what sounded last decides what sounds next, so a departure is about the thing the answer
/// depends on. The multiplexer draws every bit afresh, so a departure there is a code with no
/// question behind it and what it buys is a wider moment — which is the cost, taken on a world
/// that cannot pay it back.
/// </para>
/// <para>
/// <b>And the reading is printed rather than asserted</b>, on both worlds and at several
/// seeds. What IS asserted is that the derivation does what it claims, which is falsifiable
/// without being a score: a departure appears for a code that was live and is not, and for no
/// other code at all.
/// </para>
/// </remarks>
public sealed class DepartingTests(ITestOutputHelper output)
{
    /// <summary>
    /// <b>A departure names what left and nothing else.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The three states, asserted.</b> A code in neither moment was never here and gets
    /// nothing — that is the state a mark per absent code would have turned into an entry
    /// per vocabulary item forever, which is the always-present code arriving in the negative.
    /// A code in both is still here. A code in the first and not the second has left, and it
    /// is the only one of the three that is an event.
    /// </para>
    /// <para>
    /// <b>And a derived code is not itself watched for leaving</b>, or the alphabet grows a
    /// level a moment. A departure that departs is a claim about the derivation and not about
    /// the world.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_departure_is_derived_for_what_left_and_for_nothing_else()
    {
        Code stayed = Kinds.Named(1, "stayed");
        Code went = Kinds.Named(1, "went");
        Code came = Kinds.Named(1, "came");
        Code never = Kinds.Named(1, "never");

        IReadOnlySet<Code> last = new HashSet<Code> { stayed, went };
        IReadOnlySet<Code> now = new HashSet<Code> { stayed, came };

        var leaving = Departed.From(last, now).ToList();

        Assert.Equal([Departed.Of(went)], leaving);

        Assert.DoesNotContain(Departed.Of(stayed), leaving);
        Assert.DoesNotContain(Departed.Of(came), leaving);
        Assert.DoesNotContain(Departed.Of(never), leaving);

        // Every machine, forever, and never a randomised hash. Two nodes watching one stream
        // that disagreed about what a departure IS would hold two populations.
        Assert.Equal(Departed.Of(went), Departed.Of(went));
        Assert.NotEqual(Departed.Of(went), Departed.Of(stayed));
        Assert.True(Departed.Names(Departed.Of(went)));
        Assert.False(Departed.Names(went));

        // A departure of a departure is refused, so the alphabet cannot grow a level a round.
        Assert.Empty(Departed.From(new HashSet<Code> { Departed.Of(went) }, now));
    }

    /// <summary>
    /// <b>The examination is asked in the run's own alphabet.</b>
    /// </summary>
    /// <remarks>
    /// <b>The trap this mechanism walks into if nobody looks.</b> A withheld question with no
    /// predecessor can carry no departure, so a scope naming one could never fire on the
    /// examination — the arm would then read at its control's score for a reason that has
    /// nothing to do with generalising, which is exactly what the grouping's own remark
    /// records. A world draws its withheld turns consecutively, so each one HAS a
    /// predecessor, and this asserts the questions carry departures rather than assuming it.
    /// </remarks>
    [Fact]
    public void A_withheld_question_carries_departures_as_the_live_stream_does()
    {
        var world = new Returning(
            new ReturningSettings
            {
                Things = 8, Attributes = 3, CodesPerAttribute = 4, Hidden = 2,
                Twinned = true, Tagged = false, Placed = false, Withheld = 200,
                Wandering = 0.0, Drifting = 0.0,
            },
            seed: 1);

        var watching = new Watching<Coded>(world, new Passthrough<Coded>(one => one));

        var exam = watching.Exam;

        Assert.NotEmpty(exam);

        var carrying = exam.Count(one => one.Codes.Any(Departed.Names));

        output.WriteLine($"withheld {exam.Count} | carrying a departure {carrying}");

        // All but the first, which has nothing before it. Asserted as a share rather than a
        // count, because how many the world withholds is the world's business.
        Assert.True(carrying >= exam.Count - 1,
            $"{carrying} of {exam.Count} withheld questions carry a departure. Every one but "
            + "the first has a predecessor, so a shortfall means the examination is being "
            + "asked in a different alphabet from the run");

        // And the control carries none, so the two arms differ in the moment and not in the
        // stream. Same world, same seed, same withheld turns.
        var off = new Watching<Coded>(
            new Returning(
                new ReturningSettings
                {
                    Things = 8, Attributes = 3, CodesPerAttribute = 4, Hidden = 2,
                    Twinned = true, Tagged = false, Placed = false, Withheld = 200,
                    Wandering = 0.0, Drifting = 0.0,
                },
                seed: 1),
            new Passthrough<Coded>(one => one),
            departing: Departing.Never);

        Assert.All(off.Exam, one => Assert.DoesNotContain(one.Codes, Departed.Names));
    }

    /// <summary>What an absence buys and what it costs, on a world that can use one.</summary>
    /// <remarks>
    /// <para>
    /// <b>The kill line, written before the run.</b> <c>Rhythm</c> is a stream where what
    /// sounded last decides what sounds next, so it is the world an absence should pay on. If
    /// the arm does not beat its control there by more than the pooled error, a departure is
    /// buying nothing where it has the best case it will get, and the mechanism is a wider
    /// moment for its own sake.
    /// </para>
    /// <para>
    /// <b>And the multiplexer is the cost rather than a second bar.</b> Every bit is drawn
    /// afresh, so a departure there is a code with no question behind it — what it should do
    /// is hold the population up and the score flat or down. That is printed rather than
    /// asserted, because a mechanism is allowed to cost something on a world it was not for.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_an_absence_buys_on_a_cyclic_stream_and_costs_on_independent_draws()
    {
        var cyclic = new Dictionary<Departing, Measured>();

        foreach (var arm in new[] { Departing.Never, Departing.Left })
        {
            var scores = new List<double>();
            var held = new List<int>();

            foreach (var seed in Enumerable.Range(1, 4))
            {
                var world = new Rhythm(
                    new RhythmSettings { Symbols = 12, Period = 5, Violations = 0.1 }, seed);

                var brain = new Brain(new CommittingSettings { Capacity = 4000 }, seed);

                var tally = new Bench(
                    new Watching<Coded>(
                        world, new Passthrough<Coded>(one => one), departing: arm),
                    brain)
                    .Run(20_000, sweep: 1000, target: 0.9, window: 2000);

                scores.Add(tally.Rounds == 0 ? 0.0 : tally.Right / (double)tally.Rounds);
                held.Add(brain.Held.Count);
            }

            cyclic[arm] = new Measured { Arm = $"rhythm {arm}", Values = scores };

            output.WriteLine(
                $"rhythm      {arm,-6} | {cyclic[arm]} | held {string.Join(",", held)}");
        }

        foreach (var arm in new[] { Departing.Never, Departing.Left })
        {
            var scores = new List<double>();
            var held = new List<int>();

            foreach (var seed in Enumerable.Range(1, 4))
            {
                var world = new Multiplexer(new MultiplexerSettings { Address = 2 }, seed);

                var brain = new Brain(new CommittingSettings { Capacity = 4000 }, seed);

                var tally = new Bench(
                    new Watching<IReadOnlyList<int>>(
                        world, new Bits(Multiplexer.Bit), departing: arm),
                    brain)
                    .Run(20_000, sweep: 1000, target: 0.9, window: 2000);

                scores.Add(tally.Rounds == 0 ? 0.0 : tally.Right / (double)tally.Rounds);
                held.Add(brain.Held.Count);
            }

            output.WriteLine(
                $"multiplexer {arm,-6} | {new Measured { Arm = $"mux {arm}", Values = scores }}"
                + $" | held {string.Join(",", held)}");
        }

        var control = cyclic[Departing.Never];
        var reading = cyclic[Departing.Left];
        var pooled = Math.Sqrt(
            (control.StdErr * control.StdErr) + (reading.StdErr * reading.StdErr));

        Assert.True(reading.Mean - control.Mean > pooled,
            $"an absence buys {reading.Mean - control.Mean:F4} on the one world built around "
            + $"what came before, against a pooled error of {pooled:F4}. {reading} against "
            + $"{control}. This is the kill line `Departing` was built with: a departure that "
            + "does not pay HERE is a wider moment for its own sake");
    }
}
