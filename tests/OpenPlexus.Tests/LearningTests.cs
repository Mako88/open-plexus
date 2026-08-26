using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whether the machine gets BETTER as a run goes on, on every world that has a runner.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's, and it is the instrument this repo did not have.</b> Every reading here is a
/// comparison between two arms at one instant, so the suite is good at refuting a mechanism
/// and has no way at all to answer <i>is the machine better than it was</i>. A project that
/// cannot answer that cannot tell whether it is heading anywhere.
/// </para>
/// <para>
/// <b>A curve rather than a score.</b> A score cannot separate the two failures. A
/// machine that learns nothing and a machine on a world whose answers are unknowable read the
/// same number at the end of a run. They read differently across it: one is flat and the other
/// is flat, but a machine that DOES learn rises, and nothing else here would show that.
/// </para>
/// <para>
/// <b>And it cannot be faked by a moment's WIDTH</b>, which is the artefact that has eaten
/// most of the readings on the walked house. Width is a property of the composition and is
/// constant across a run, so it moves the whole curve up or down and cannot bend it.
/// </para>
/// <para>
/// <b>Across worlds rather than on one</b>, John's again. One world's curve is a verdict on
/// that world; the same curve on an enumerable world, on the spine world and on a world whose
/// answers are a coin is a verdict on the machine.
/// </para>
/// </remarks>
public sealed class LearningTests(ITestOutputHelper output)
{
    /// <summary>How many slices of a run the curve is read at.</summary>
    /// <remarks>
    /// <b>A SIZE, and five is enough to see a bend.</b> Two points say whether the end beat
    /// the start and nothing about the shape, which is what separates a machine still climbing
    /// from one that learnt everything in its first tenth and stopped.
    /// </remarks>
    private const int Slices = 5;

    /// <summary>
    /// A source whose answers are a COIN — <b>the world nothing can learn.</b>
    /// </summary>
    /// <param name="inner">The world whose moments are passed through untouched.</param>
    /// <param name="outcomes">How many answers it draws between.</param>
    /// <param name="seed">The draw.</param>
    /// <remarks>
    /// <b>The control the curve needs to mean anything.</b> An instrument that reads flat on a
    /// world nobody could learn and flat on a world the machine failed to learn says nothing
    /// about either; what makes it a measurement is that the two are different worlds and one
    /// of them is known. The moments are the inner world's, so the only thing that changed is
    /// whether the answer can be predicted at all.
    /// </remarks>
    private sealed class Coined(IInput inner, int outcomes, int seed) : IInput
    {
        private readonly Random _draws = new(seed);

        /// <inheritdoc/>
        public byte Source => inner.Source;

        /// <inheritdoc/>
        public int Outcomes => outcomes;

        /// <inheritdoc/>
        public Pushed? Push() =>
            inner.Push() is not { } pushed
                ? null
                : pushed with { Followed = Brain.Says(_draws.Next(outcomes)) };
    }

    /// <summary>The score over each fifth of one run, each fifth counted on its own.</summary>
    /// <param name="input">Whatever is pushing moments.</param>
    /// <param name="brain">The brain they are pushed into.</param>
    /// <param name="rounds">How long the run is.</param>
    /// <remarks>
    /// <para>
    /// <b>DISJOINT slices rather than a lifetime, because a lifetime average cannot fall.</b>
    /// C4 says there is no episode boundary, and a run's own history dragging its score is the
    /// same fault one seam over: what is wanted is how well it is doing NOW, five times over.
    /// A cumulative share cannot bend and so cannot show learning.
    /// </para>
    /// <para>
    /// <b>And NOT <c>Round.Recent</c>.</b> That was the first shape of this and it was broken.
    /// That field is documented as the share right over the last TENTH and is guarded by
    /// <c>round &gt;= _from</c>, so it is nought for four fifths of any run by construction.
    /// Sampled at fifths it read 0.000, 0.000, 0.000, 0.000 and then a number, on twelve runs
    /// out of twelve — a curve that is four zeroes and a score, printed by a test that passed.
    /// </para>
    /// <para>
    /// <b>Counted off the round counters rather than added here</b>, so a slice is exactly the
    /// rounds the loop scored in it. A slice where the population said nothing at all reads
    /// nought rather than dividing by nought, and that is a real reading: a machine with no
    /// rules answers nothing.
    /// </para>
    /// </remarks>
    /// <param name="counting">
    /// Whether the round just taken is one the curve reads, or nothing to read them all.
    /// <b>What it is for is the exam.</b> A house is 120 walked steps and 6 questions, so a
    /// curve over every round is a curve about predicting the walk and the exam is a fiftieth
    /// of it — and the exam is the thing this world exists to ask.
    /// </param>
    private static async Task<IReadOnlyList<double>> Curve(
        IInput input, Brain brain, int rounds, Func<bool>? counting = null)
    {
        var loop = new Round(brain, rounds, sweep: 500, target: 0.9, window: 500);

        var curve = new List<double>();
        var every = rounds / Slices;

        var hits = 0;
        var asked = 0;

        for (var round = 0; round < rounds; round++)
        {
            if (input.Push() is { } pushed)
            {
                var was = loop.Right;
                var missed = loop.Wrong;

                await loop.StepAsync(pushed);

                if (counting?.Invoke() ?? true)
                {
                    if (loop.Right > was) hits++;
                    if (loop.Right > was || loop.Wrong > missed) asked++;
                }
            }

            if ((round + 1) % every != 0 || curve.Count == Slices) continue;

            curve.Add(asked == 0 ? 0.0 : hits / (double)asked);

            (hits, asked) = (0, 0);
        }

        return curve;
    }

    /// <summary>The eleven-bit multiplexer, whose rules are conjunctions and enumerable.</summary>
    /// <param name="seed">What draws it.</param>
    /// <param name="coined">Whether its answers are replaced by a draw.</param>
    private static (IInput Input, Brain Brain, Func<bool>? Counting) Multiplexed(
        int seed, bool coined)
    {
        var world = new Multiplexer(new MultiplexerSettings { Address = 3 }, seed);
        var brain = new Brain(new CommittingSettings { Capacity = 20_000 }, seed);

        IInput input = new Watching<IReadOnlyList<int>>(world, new Bits(Multiplexer.Bit));

        // Every round is the question here, so there is nothing to count separately.
        return (coined ? new Coined(input, world.Outcomes, seed) : input, brain, null);
    }

    /// <summary>The walked house, at the composition the terminal ships.</summary>
    /// <param name="seed">What draws it.</param>
    /// <param name="coined">Whether its answers are replaced by a draw.</param>
    private static (IInput Input, Brain Brain, Func<bool>? Counting) Walked(
        int seed, bool coined)
    {
        var world = new Roaming(Fixture.House(asked: 6), seed);
        var brain = new Brain(new CommittingSettings { Capacity = 20_000 }, seed);

        IInput input = new Watching<Coded>(
            world,
            new Joined(Joining.Resolved, resolution: 3, freshest: true),
            acting: Chooses.From(_ => null));

        // The EXAM only. `Sat` is the world's own channel for whether the round just taken was
        // one of the survey's, on `Named`'s standing, and nothing that learns is shown it.
        return (
            coined ? new Coined(input, world.Outcomes, seed) : input,
            brain,
            () => world.Sat);
    }

    /// <summary>
    /// <b>The machine gets better within a run, and a coin does not.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What it asks is whether the last fifth beats the first</b>, on each world and on the
    /// same world with its answers replaced by a draw. A rise on both is an instrument reading
    /// its own trailing window warming up; a rise on one is learning.
    /// </para>
    /// <para>
    /// <b>Red or green is a claim about the INSTRUMENT.</b> Not about the machine. What
    /// is asserted is the multiplexer, because a machine that cannot climb where the rules are
    /// conjunctions cannot climb anywhere and a curve that cannot see it is broken. What the
    /// machine is worth is the table, and the table is printed.
    /// </para>
    /// <para>
    /// <b>THE BRAIN LEARNS.</b> On the multiplexer it reads 0.723, 0.942, 0.975, 0.982, 0.989
    /// against a coin's 0.510, 0.510, 0.505, 0.501, 0.515. That is the first evidence on this
    /// branch that the loop works at all, and it was unavailable before this file because every
    /// other reading here is a comparison between two arms at one instant.
    /// </para>
    /// <para>
    /// <b>And the walked house does not improve.</b> Its exam reads 0.298, 0.292, 0.333, 0.219,
    /// 0.229 on one seed and 0.306, 0.302, 0.385, 0.354, 0.323 on the other — flat, and falling
    /// on the first. Whatever the machine knows about the house it knows inside the first fifth
    /// and does not add to over eight thousand more rounds.
    /// </para>
    /// <para>
    /// <b>Which is not the same as learning nothing there.</b> It sits near 0.30 where the same
    /// world with its answers replaced by a draw sits near 0.02, so a great deal was learnt —
    /// early, and then it stopped. A world that is exhausted after two thousand rounds and a
    /// machine that cannot learn are different diagnoses and this separates them.
    /// </para>
    /// <para>
    /// <b>And two seeds cannot see a small rise.</b> The coin's own slices swing 0.04 on the
    /// same counts, so anything under about a twentieth is invisible here and slow learning
    /// cannot be ruled out. What can be ruled out is a rise the size of the multiplexer's.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Whether_the_machine_gets_better_as_a_run_goes_on()
    {
        // Twelve runs, so this is minutes rather than seconds and the sizes are chosen to
        // keep it in the suite rather than to reach any particular score.
        const int Rounds = 10_000;
        const int Seeds = 2;

        output.WriteLine($"{Rounds} rounds a seed over {Seeds} seeds, {Slices} slices");
        output.WriteLine($"{"world",-14}{"answers",-9}{"curve",-44}{"rise",8}");

        var rises = new Dictionary<(string World, bool Coined), List<double>>();

        foreach (var (name, build) in
            new (string Name, Func<int, bool, (IInput, Brain, Func<bool>?)> Build)[]
        {
            ("multiplexer", Multiplexed),
            ("the house", Walked),
        })
        foreach (var coined in new[] { false, true })
        {
            foreach (var seed in Enumerable.Range(1, Seeds))
            {
                var (input, brain, counting) = build(seed, coined);

                var curve = await Curve(input, brain, Rounds, counting);

                var rise = curve[^1] - curve[0];

                if (!rises.TryGetValue((name, coined), out var all))
                    rises[(name, coined)] = all = [];

                all.Add(rise);

                output.WriteLine(
                    $"{name,-14}{(coined ? "a coin" : "its own"),-9}"
                    + $"{string.Join("  ", curve.Select(one => one.ToString("F3"))),-44}"
                    + $"{rise,8:F3}");
            }
        }

        foreach (var ((name, coined), all) in rises)
            output.WriteLine(
                $"{name} on {(coined ? "a coin" : "its own answers")} rises "
                + $"{all.Average():F3} on average, on {all.Count(one => one > 0)} seeds of "
                + $"{all.Count}");

        // The MULTIPLEXER calibrates the instrument, and it is the only world asserted on.
        // Its rules are conjunctions and enumerable, so a machine that cannot climb there
        // cannot climb anywhere and a curve that cannot show it climbing is broken. A tenth is
        // a wide bar for a world that goes from a coin's 0.5 to nearly 1.0.
        Assert.True(
            rises[("multiplexer", false)].Average()
                > rises[("multiplexer", true)].Average() + 0.1,
            $"the multiplexer's curve rises {rises[("multiplexer", false)].Average():F3} "
            + $"against {rises[("multiplexer", true)].Average():F3} when its answers are a "
            + "coin. A world whose rules are conjunctions is the one this machine is known to "
            + "learn, so either it has stopped learning or this curve cannot see it -- and "
            + "every other row here is unreadable until that is settled.");

        // And the house is PRINTED rather than asserted. A world where the curve cannot
        // separate the machine from a coin is a reading about that world, not a failure of
        // this file -- and asserting it would have this go red for a world being exhausted,
        // which is a thing to know rather than a thing to fix here. The first shape of this
        // asserted both and passed on rounding, 0.006 against 0.006.
    }
}
