using OpenPlexus.Graph;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Every question-answering world, one run each, one table — <b>the board, in one
/// place.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S ASK, 2026-08-04, AND IT NAMES A REAL GAP.</b> Nothing here ran more
/// than three worlds at once, so "is this change good across the board" could only
/// be answered by reading eight test files and hoping they were run under
/// comparable settings. The edge-kinds arm was measured in two worlds and
/// described as winning everywhere, which nobody could check cheaply.
/// </para>
/// <para>
/// <b>IT ASSERTS ALMOST NOTHING ON PURPOSE.</b> Each world already owns its own
/// claims, asserted in its own file against its own chance level; repeating them
/// here would be a second copy to drift. What this owns is the COMPARISON — that
/// every world still runs, still beats its own chance, and still reports what it
/// is complaining about — and the table it prints, which is the artefact.
/// </para>
/// <para>
/// <b>Small runs, deliberately.</b> This is a board to read at a glance and to
/// re-read after a structural change, not a source of headline numbers — those
/// belong to each world's own file at the sizes that world needs. <b>A short run
/// on `Binding` measures RECENCY and not binding</b>, which is why it is not here.
/// </para>
/// </remarks>
public sealed class ScoreboardTests(ITestOutputHelper output)
{
    /// <summary>One world's line on the board.</summary>
    private sealed record Line(string World, Questioned Result)
    {
        public double Above => Result.Accuracy - Result.Chance;
    }

    private static WalkSettings Dials(double stamina = 8.0) => Fixture.Dials(stamina: stamina);

    private static async Task<Line> SensesAsync()
    {
        using var run = new SensesRun(Fixture.Senses(concepts: 12), Dials(), seed: 3);
        return new Line("senses", await run.RunAsync(300, every: 10));
    }

    private static async Task<Line> ComposedAsync()
    {
        using var run = new ComposedRun(
            new ComposedSettings { Values = 24, CodesPerValue = 3, Segmented = true, Tagged = true },
            Dials() with { Pricing = Pricing.Sender },
            seed: 3);

        return new Line("composed", await run.RunAsync(300));
    }

    private static async Task<Line> MotifAsync()
    {
        using var run = new MotifRun(
            new MotifSettings { Symbols = 60, Motifs = 6, Size = 4, Density = 0.5 },
            Dials(),
            seed: 3);

        return new Line("motif", await run.RunAsync(300, every: 10));
    }

    private static async Task<Line> RhythmAsync()
    {
        using var run = new RhythmRun(
            new RhythmSettings { Symbols = 12, Period = 5, Violations = 0.1 },
            Dials(),
            seed: 3);

        return new Line("rhythm", await run.RunAsync(300));
    }

    private static async Task<Line> BabiAsync()
    {
        using var run = new BabiRun(
            new BabiSettings
            {
                Task = 1,
                Stories = true,
                Corpus = Path.Combine(Tree.Repo(), "corpora", "tasks_1-20_v1-2", "en"),
            },
            Dials() with { Pricing = Pricing.Sender },
            seed: 3);

        return new Line("babi", await run.RunAsync(400));
    }

    private static async Task<Line> ClevrAsync()
    {
        using var run = new ClevrRun(
            new ClevrSettings
            {
                Corpus = Path.Combine(Tree.Repo(), "corpora", "CLEVR_v1.0"),

                // THE SIZE ITS OWN FILE MEASURES IT AT, and not a smaller one.
                // At 120 scenes this world reads BELOW chance — the graph has
                // barely formed and the line would be a misleading entry on a
                // board whose whole purpose is comparison at a glance. Same
                // hazard that keeps `Binding` off here entirely.
                Scenes = 700,
                Segmented = true,
                Tagged = true,
            },

            // SENDER, WHICH IS THE ARM THIS WORLD'S OWN FILE MEASURES AS THE ONE
            // THAT WORKS. Under the receiver arm a fresh per-scene index has
            // `seen = 1`, so every attribute accumulates one maximally-cheap
            // partner per scene and the fan-out explodes — read here as a widest
            // row of 701 over 700 scenes and a score far below chance.
            Dials() with { Pricing = Pricing.Sender },
            seed: 3);

        return new Line("clevr", await run.RunAsync());
    }

    [Fact]
    public async Task Every_world_still_runs_and_still_beats_its_own_chance()
    {
        var board = new List<Line>
        {
            await SensesAsync(),
            await ComposedAsync(),
            await MotifAsync(),
            await RhythmAsync(),
            await BabiAsync(),
            await ClevrAsync(),
        };

        output.WriteLine(
            $"{"world",-10} {"acc",8} {"chance",8} {"above",8} {"asked",7} "
            + $"{"msgs",12} {"nodes",7} {"widest",7}");

        foreach (var line in board)
            output.WriteLine(
                $"{line.World,-10} {line.Result.Accuracy,8:F4} {line.Result.Chance,8:F4} "
                + $"{line.Above,8:F4} {line.Result.Asked,7} {line.Result.Messages,12} "
                + $"{line.Result.Nodes,7} {line.Result.Widest,7}");

        foreach (var line in board)
            foreach (var complaint in line.Result.Complaints)
                output.WriteLine($"  {line.World}: {complaint}");

        // EVERY WORLD ASKED SOMETHING. A world that silently stopped asking is the
        // failure this file exists to make visible — a green suite where one world
        // has quietly become a no-op reads exactly like a healthy one.
        Assert.All(board, line => Assert.True(line.Result.Asked > 0,
            $"{line.World} asked nothing at all"));

        // AND BEAT ITS OWN CHANCE. Not a quality bar — each world owns its real
        // thresholds — but a floor that catches a change which broke the walk
        // everywhere at once, which is exactly what a board is for.
        Assert.All(board, line => Assert.True(line.Above > 0.0,
            $"{line.World} is at or below chance: {line.Result.Accuracy} "
            + $"against {line.Result.Chance}"));
    }

}
