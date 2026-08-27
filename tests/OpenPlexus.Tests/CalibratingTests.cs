using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whether a bar that is CALIBRATED still needs paying for — <b>fork 152's two halves,
/// crossed.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Two corrections that both address the same symptom by different routes.</b>
/// <see cref="Correcting.Gates"/> divides alpha by the parents a round searches, which
/// tightens a threshold. <see cref="Testing.Exact"/> replaces the statistic the threshold is
/// applied to. A population filled with noise could be either, and only the cross says which
/// -- or whether one makes the other redundant.
/// </para>
/// <para>
/// <b>And it is a sweep because it is a grid.</b> Four cells over two worlds and two answer
/// sources is sixteen runs before seeds, which is a runner's work rather than a push's.
/// <c>LearningTests</c> carries the live question at two arms; this carries the four.
/// </para>
/// <para>
/// <b>It prints and asserts nothing</b>, which is what a sweep is for. A threshold written
/// before the first run is a prediction wearing a check's clothes.
/// </para>
/// </remarks>
public sealed class CalibratingTests(ITestOutputHelper output)
{
    /// <summary>How long each arm runs for.</summary>
    private const int Rounds = 10_000;

    /// <summary>How many draws each cell is read over.</summary>
    private const int Seeds = 3;

    /// <summary>What one cell of the grid came back with.</summary>
    /// <param name="Held">Resident commitments at the end.</param>
    /// <param name="Repaired">Children the bar admitted across the run.</param>
    /// <param name="Thin">Of those, ones the parent had too few hits to earn.</param>
    /// <param name="Score">The share right over the last fifth.</param>
    private readonly record struct Cell(double Held, double Repaired, double Thin, double Score);

    /// <summary>One run of one world under one pair of arms.</summary>
    /// <param name="world">Which world.</param>
    /// <param name="coined">Whether its answers are replaced by a draw.</param>
    /// <param name="correcting">What the bar is paid for out of.</param>
    /// <param name="testing">Which test the bar admits on.</param>
    /// <param name="seed">The draw.</param>
    private static async Task<Cell> Run(
        string world, bool coined, Correcting correcting, Testing testing, int seed)
    {
        var dials = new CommittingSettings
        {
            Capacity = 20_000,
            Correcting = correcting,
            Testing = testing,
        };

        var brain = new Brain(dials, seed);

        IInput input;
        Func<bool> counting;
        int outcomes;

        if (world == "multiplexer")
        {
            var built = new Multiplexer(new MultiplexerSettings { Address = 3 }, seed);
            input = new Watching<IReadOnlyList<int>>(built, new Bits(Multiplexer.Bit));
            counting = () => true;
            outcomes = built.Outcomes;
        }
        else
        {
            var built = new Roaming(Fixture.House(asked: 6), seed);

            input = new Watching<Coded>(
                built,
                new Joined(Joining.Resolved, resolution: 3, freshest: true),
                acting: Chooses.From(_ => null));

            counting = () => built.Sat;
            outcomes = built.Outcomes;
        }

        if (coined) input = new Coined(input, outcomes, seed);

        var loop = new Round(brain, Rounds, sweep: 500, target: 0.9, window: 500);

        var hits = 0;
        var asked = 0;
        var from = Rounds - (Rounds / 5);

        for (var round = 0; round < Rounds; round++)
        {
            if (input.Push() is not { } pushed) continue;

            var was = loop.Right;
            var missed = loop.Wrong;

            await loop.StepAsync(pushed);

            if (round < from || !counting()) continue;

            if (loop.Right > was) hits++;
            if (loop.Right > was || loop.Wrong > missed) asked++;
        }

        return new Cell(
            brain.Held.Count,
            loop.Repaired,
            brain.Held.Thin,
            asked == 0 ? 0.0 : hits / (double)asked);
    }

    /// <summary>
    /// What each correction buys where the other is already running.
    /// </summary>
    [Trait(Sweeps.Kind, Sweeps.Name)]
    [Fact]
    public async Task Whether_a_calibrated_bar_still_needs_paying_for()
    {
        output.WriteLine($"{Rounds} rounds over {Seeds} seeds, last fifth scored");
        output.WriteLine(
            $"{"world",-13}{"answers",-9}{"paid",-11}{"test",-13}"
            + $"{"held",10}{"repaired",12}{"thin",10}{"score",9}");

        foreach (var world in new[] { "multiplexer", "the house" })
        foreach (var coined in new[] { false, true })
        foreach (var correcting in new[] { Correcting.Candidates, Correcting.Gates })
        foreach (var testing in new[] { Testing.Approximated, Testing.Exact })
        {
            var cells = new List<Cell>();

            foreach (var seed in Enumerable.Range(1, Seeds))
                cells.Add(await Run(world, coined, correcting, testing, seed));

            output.WriteLine(
                $"{world,-13}{(coined ? "a coin" : "its own"),-9}{correcting,-11}{testing,-13}"
                + $"{cells.Average(one => one.Held),10:F0}"
                + $"{cells.Average(one => one.Repaired),12:F0}"
                + $"{cells.Average(one => one.Thin),10:F0}"
                + $"{cells.Average(one => one.Score),9:F3}");
        }
    }
}
