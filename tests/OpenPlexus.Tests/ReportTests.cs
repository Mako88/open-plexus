using OpenPlexus.Graph;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The anti-drift check. Every run says what its own plumbing did, and this
/// asserts none of it is out of range.
/// </summary>
/// <remarks>
/// <b>This is the test that would have caught what the audit caught by hand.</b>
/// A number swept, barely moving, because something was not wired the way anyone
/// thought — that is the failure, and a report nobody looks at does not prevent
/// it. `Complaints` is looked at here, every run, automatically.
/// </remarks>
public sealed class ReportTests
{
    private static SnakeSettings World() => Fixture.Snake(energy: 200.0);

    private static WalkSettings Dials(double stamina = 4.0) => Fixture.Dials(stamina);

    [Fact]
    public async Task Nothing_in_a_run_is_out_of_range()
    {
        for (var seed = 1; seed <= 10; seed++)
        {
            using var run = new SnakeRun(World(), Dials(), seed);
            var report = await run.ReportAsync(1000);

            Assert.True(report.Complaints.Count == 0,
                $"seed {seed}: {string.Join("; ", report.Complaints)}\n{report}");
        }
    }

    [Fact]
    public async Task Both_front_ends_stay_in_range()
    {
        // The empty-cell arms are the two configurations actually in use, and
        // an unwiring that only showed up in one of them would be missed.
        foreach (var empty in (bool[])[false, true])
        {
            using var run = new SnakeRun(World(), Dials(), seed: 3, includeEmpty: empty);
            var report = await run.ReportAsync(500);

            Assert.True(report.Complaints.Count == 0,
                $"empty={empty}: {string.Join("; ", report.Complaints)}\n{report}");
        }
    }

    [Fact]
    public async Task The_report_notices_a_walk_that_is_not_walking()
    {
        // THE COMPANION, and without it the checks above pass for a Complaints
        // that can never say anything. A stamina under 1 cannot afford any hop
        // at all, so no route leaves its origin -- and the report must say so
        // rather than reporting a healthy-looking run of nothing.
        using var run = new SnakeRun(World(), Dials(stamina: 0.5), seed: 3);
        var report = await run.ReportAsync(300);

        Assert.Contains(report.Complaints, c => c.Contains("no route walked"));
    }

    [Fact]
    public void An_unfinished_walk_is_complained_about_rather_than_absorbed()
    {
        // THE COMPANION FOR FORK 22'S CHECK, and it is needed twice over.
        //
        // Snake read every thought on `QuietAsync` alone and never once asked
        // whether the walk had finished -- the bus going quiet is not a finish
        // signal, which is the trap this project named and then left standing in
        // its oldest world. `Homeostat` had the same hole from the other side: it
        // called `SettleAsync` and threw the answer away.
        //
        // ARMING BOTH MOVED NO NUMBER -- ten seeds of a thousand steps report
        // zero. That is the good outcome and it is exactly why this test exists:
        // a check that always reads zero is indistinguishable from a check that
        // cannot fire, and the second is what the last two commits were about.
        var wired = new HomeostatResult
        {
            Choosing = Attending.Chain,
            Steps = 10,
            Held = 5,
            Silent = 0,
            Choices = 0,
            Improving = 0.0,
            Attended = [1, 1, 1, 1],
            States = 3,
            Idling = 19,
            Unsettled = 3,
            Plumbing = new Plumbing
            {
                Nodes = 4,
                Edges = 6,
                Widest = 2,
                Spread = [2, 2],
                ChainLengths = new Dictionary<int, int> { [2] = 4 },
                Messages = 40,
                Unbalanced = 0,
            },
        };

        Assert.Contains(wired.Complaints, c => c.Contains("before they had finished"));
        Assert.Empty((wired with { Unsettled = 0 }).Complaints);
    }

    [Fact]
    public async Task A_report_says_how_far_routes_actually_went()
    {
        using var run = new SnakeRun(World(), Dials(), seed: 3);
        var report = await run.ReportAsync(500);

        // Measured at stamina 4: chains of length 2 and 3, never longer,
        // because a step costs 1/weight and typical weights are near 0.5.
        Assert.True(report.Deepest >= 2, $"deepest chain was {report.Deepest}");
        Assert.NotEmpty(report.ChainLengths);
    }
}
