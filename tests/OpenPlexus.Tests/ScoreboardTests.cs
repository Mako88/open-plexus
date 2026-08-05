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
/// <b>IT ASSERTS THE SHAPE OF EACH LINE AND NOT THE CLAIM BEHIND IT.</b> Each
/// world already owns its own claims, asserted in its own file against its own
/// chance level; repeating them here would be a second copy to drift. What this
/// owns is the COMPARISON — and the table it prints, which is the artefact.
/// </para>
/// <para>
/// <b>THE FLOOR USED TO BE CHANCE, AND THAT LEFT A FACTOR OF TEN TO FALL
/// THROUGH.</b> Senses reads 0.86 against a chance of 0.08, so every mechanism in
/// it could have come unwired at once and this file would still have gone green.
/// A bar set where nothing can touch it is the <c>TRAPS</c> entry about a check
/// that is wired and unable to fire, and it was sitting in the one place whose
/// whole job is to notice a change that broke something everywhere.
/// </para>
/// <para>
/// <b>SO THE THREE ASSERTIONS ARE THREE DIFFERENT FAILURES, and none of them is a
/// quality bar.</b> A floor per world catches a mechanism going missing; a
/// ceiling on messages catches a walk that exploded; an empty complaint list
/// catches a world whose walk stopped walking while its score held up. <b>Every
/// number here is placed BELOW what the world reads today with room to spare</b>,
/// because a golden value that has to be edited after every honest improvement
/// gets edited without being read.
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
    /// <summary>One world's line on the board, and the two bars it must clear.</summary>
    /// <param name="Floor">
    /// The least this world may score ABOVE ITS OWN CHANCE. <b>Not a target</b> —
    /// it is placed roughly a third below what the world reads today, so an
    /// ordinary shift in either direction passes and a mechanism going missing
    /// does not.
    /// </param>
    /// <param name="Ceiling">
    /// The most this world may spend in messages. <b>Roughly twice what it spends
    /// today</b>, because the failure being caught is a walk that exploded rather
    /// than one that got dearer — <c>Pricing.Balanced</c> timed out entirely and
    /// the receiver arm on CLEVR was measured at 6.6× before it did.
    /// </param>
    private sealed record Line(string World, Questioned Result, double Floor, long Ceiling)
    {
        public double Above => Result.Accuracy - Result.Chance;
    }

    private static WalkSettings Dials(double stamina = 8.0) => Fixture.Dials(stamina: stamina);

    private static async Task<Line> SensesAsync()
    {
        using var run = new SensesRun(Fixture.Senses(concepts: 12), Dials(), seed: 3);

        // READS 0.3305 ABOVE A CHANCE OF 0.0833, AGAINST 0.7787 BEFORE 2026-08-05,
        // AND THE WINDOW IS NOT WHAT IS LEFT. With the span off the traffic is back
        // to 11,708 messages from 202,943, so the carried edges are gone — and the
        // node count is 141 against 106, which is thirty-five MINTED names.
        //
        // CHUNKING IS THE REMAINING LOSS, AND IT IS THE SAME MECHANISM TWICE. A
        // moment here is two codes, so a chunk covering it writes `name-sight` and
        // `name-sound` and DESTROYS the sight-sound edge, which is the entire task:
        // reaching touch from sight runs through sound and nowhere else. The plan
        // already names the fix — chunk candidates BELOW a whole moment.
        return new Line("senses", await run.RunAsync(300, every: 10), Floor: 0.22, Ceiling: 60_000);
    }

    private static async Task<Line> ComposedAsync()
    {
        using var run = new ComposedRun(
            new ComposedSettings { Values = 24, CodesPerValue = 3, Segmented = true, Tagged = true },
            Dials() with { Pricing = Pricing.Sender },
            seed: 3);

        // READS ABOUT 0.1997 ABOVE A CHANCE OF 0.0417.
        return new Line("composed", await run.RunAsync(300), Floor: 0.13, Ceiling: 200_000);
    }

    private static async Task<Line> MotifAsync()
    {
        using var run = new MotifRun(
            new MotifSettings { Symbols = 60, Motifs = 6, Size = 4, Density = 0.5 },
            Dials(),
            seed: 3);

        // READS 0.4138 ABOVE A CHANCE OF 0.0345, DOWN FROM 0.6552, AND CHUNKING IS
        // THE WHOLE OF IT — 0.8276 with the minting suppressed against 0.4483 with
        // it, on the world step 3 was built for. That is not a floor being lowered
        // to fit; it is an open question, and the number is what makes it visible.
        return new Line("motif", await run.RunAsync(300, every: 10), Floor: 0.27, Ceiling: 5_000_000);
        
    }

    private static async Task<Line> RhythmAsync()
    {
        using var run = new RhythmRun(
            new RhythmSettings { Symbols = 12, Period = 5, Violations = 0.1 },

            // THE SPAN IS THE TASK HERE, AND SAYING SO IS THE OPEN PROBLEM RATHER
            // THAN THE SOLUTION. Nothing overlaps on this world, so with no window
            // there are no temporal cells at all and it asks NOTHING. A global
            // window was tried on 2026-08-05 and broke three other worlds'
            // controls, so it is set here — by a test, which is the wrong side of
            // the line. See `WalkSettings.Span`: the brain should read this off
            // the stream.
            Dials() with { Span = 1 },
            seed: 3);

        // READS 0.8080 ABOVE A CHANCE OF 0.0833, UP FROM 0.1800 — the largest move
        // any world has made here, and it got CHEAPER doing it. The window, the
        // surprise gate and the recency preference all went on at once, and this is
        // the world all three are about. Measured at `Span = 0` it asks NOTHING AT
        // ALL, so the regression that took the window off it lands under every
        // bar here at once — see `The_floor_is_placed_where_a_lost_mechanism_trips_it`.
        return new Line("rhythm", await run.RunAsync(300), Floor: 0.54, Ceiling: 30_000);
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

        // READS ABOUT 0.1942 ABOVE A CHANCE OF 0.1667. The window costs this world
        // too — the refutation row named it — and it is off again.
        return new Line("babi", await run.RunAsync(400), Floor: 0.13, Ceiling: 1_200_000);
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

        // READS 0.0659 ABOVE A CHANCE OF 0.3626 — THE THINNEST MARGIN ON THE
        // BOARD, so its floor is the one doing the least work. That is the world
        // being honest rather than the bar being slack: real scenes, a chance
        // level a third of the way up, and a graph that has barely formed at 700.
        // READS 0.0960 ABOVE A CHANCE OF 0.3626, UP FROM 0.0659, ON A QUARTER OF
        // THE TRAFFIC — 9,096,347 messages to 2,161,860 and the widest row 701 to
        // 32. That is the row cap doing exactly what it was cashed in for, on the
        // only world here whose fan-out was ever large enough to need it.
        return new Line("clevr", await run.RunAsync(), Floor: 0.064, Ceiling: 5_000_000);
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

        // AND CLEARED ITS OWN FLOOR, WHICH IS NOT THE SAME AS BEATING CHANCE. A
        // world can lose every mechanism it has and still sit above a chance level
        // ten times below it, which is what this bar used to be.
        Assert.All(board, line => Assert.True(line.Above >= line.Floor,
            $"{line.World} is at {line.Above:F4} above chance, under its floor of "
            + $"{line.Floor:F4} — accuracy {line.Result.Accuracy:F4} against a "
            + $"chance of {line.Result.Chance:F4}. Something it depends on is gone, "
            + "or the floor is being moved without being read."));

        // AND DID NOT EXPLODE. The score says nothing about what the walk spent
        // getting there, and the two failures this project has actually had —
        // `Pricing.Balanced` and the receiver arm on CLEVR — were both a walk
        // whose cost ran away rather than one whose answers got worse.
        Assert.All(board, line => Assert.True(line.Result.Messages <= line.Ceiling,
            $"{line.World} spent {line.Result.Messages} messages against a ceiling "
            + $"of {line.Ceiling}. That is a walk that exploded, not one that got "
            + "dearer."));

        // AND IS NOT COMPLAINING. `Complaints` is where "the walk never left its
        // origin" lives, and a world CAN hold its score while its walk stops
        // walking — one-hop association is a real signal on several of these. The
        // list is printed above either way; this is what makes it a check.
        Assert.All(board, line => Assert.True(line.Result.Complaints.Count == 0,
            $"{line.World} is complaining: {string.Join("; ", line.Result.Complaints)}"));
    }

    /// <summary>
    /// The floor for <c>rhythm</c> is placed where the regression that prompted
    /// this file actually lands.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A BAR NOBODY HAS SEEN FIRE IS A BAR NOBODY KNOWS THE HEIGHT OF</b> —
    /// the <c>TRAPS</c> entry says to arm anything that has always read zero, and
    /// a floor is exactly that shape. So the known break is run against the known
    /// bar, here, once.
    /// </para>
    /// <para>
    /// <b>IT IS THE BREAK THAT ALREADY HAPPENED.</b> <c>RhythmRun</c> defaulted
    /// <see cref="WalkSettings.Span"/> to 1 until the dials moved to the brain,
    /// which defaults it to 0 — and on this world the window IS the task, so the
    /// migration took the whole thing off and 452 tests stayed green. <b>Measured:
    /// this world asks NOTHING at a span of nought</b>, so the drop is not a
    /// narrow miss of the floor but the entire line disappearing.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task The_floor_is_placed_where_a_lost_mechanism_trips_it()
    {
        var line = await RhythmAsync();

        using var without = new RhythmRun(
            new RhythmSettings { Symbols = 12, Period = 5, Violations = 0.1 },

            // THE ONE DIFFERENCE, AND IT HAS TO BE SAID OUT LOUD NOW. This was
            // `Dials()` — the migration's own default — until the span was cashed
            // in at one on 2026-08-05, at which point both arms became the same run
            // and this test failed saying its own floor was decorative. IT WAS
            // RIGHT: a control that silently becomes the treatment is the exact
            // trap this file exists to guard, and it caught itself.
            Dials() with { Span = 0 },
            seed: 3);

        var lost = await without.RunAsync(300);
        var above = lost.Accuracy - lost.Chance;

        output.WriteLine(
            $"rhythm with a window: {line.Above:F4} above chance over {line.Result.Asked} asked");
        output.WriteLine(
            $"rhythm without one:   {above:F4} above chance over {lost.Asked} asked");
        output.WriteLine($"the floor sits at     {line.Floor:F4}");

        Assert.True(above < line.Floor,
            $"the crippled world reads {above:F4} above chance, which its own floor "
            + $"of {line.Floor:F4} would let through. The floor is decorative.");
    }
}
