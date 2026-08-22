using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// A fleet as separate processes, which is the first arrangement nothing here can fake.
/// </summary>
/// <remarks>
/// <para>
/// <b>What <c>Ported</c> could not say.</b> That fixture brings a whole fleet up inside one
/// process on real ports, so every population is an object the harness can still reach — a
/// run that quietly read one directly would pass. Here a holder is an operating system
/// process and the only thing crossing is a message, so the wire is the only road there is.
/// </para>
/// <para>
/// <b>It starts <c>OpenPlexus.Host</c> rather than composing one</b>, because composing one
/// would put the deployment back in the suite and the deployment is the thing being asserted.
/// The command lines here are the ones a person would type.
/// </para>
/// <para>
/// <b>And the patience is the experimenter's.</b> Nothing in the library may decide a missing
/// holder by a clock, so a fleet that loses an answer waits forever and a suite that inherited
/// that would hang rather than fail. Every wait here is a test giving up, which is outside the
/// machine and always was.
/// </para>
/// </remarks>
public sealed class HostedTests(ITestOutputHelper output)
{
    /// <summary>How long a two-process run may take before the suite gives up.</summary>
    private static readonly TimeSpan Patience = TimeSpan.FromMinutes(3);

    /// <summary>
    /// <b>Two holders in two processes learn the multiplexer</b>, and nothing is lost.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The denominator is asserted rather than reported.</b> Nothing here decides a missing
    /// holder by a clock, so a fleet that quietly stopped asking one machine would learn from
    /// the rest and score perfectly well — and asked against heard is the only reading that
    /// says so.
    /// </para>
    /// <para>
    /// <b>And the score is asserted against chance rather than against a number.</b> The
    /// multiplexer answers one bit, so a run that wired up and learnt nothing sits at a half;
    /// what this asserts is that the loop over sockets is a learning loop, never how good it
    /// is. <c>FleetTests</c> is where the fleet is measured.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task A_fleet_of_two_processes_learns_over_the_wire()
    {
        const int Holders = 2;
        const int Rounds = 500;

        var ports = Enumerable.Range(0, Holders + 1).Select(_ => Wired.Free()).ToList();
        var peers = string.Join(",", ports);

        var running = new List<Process>();

        try
        {
            for (var at = 0; at < Holders; at++)
                running.Add(Started(
                    "hold",
                    "--listen", ports[at + 1],
                    "--at", at.ToString(CultureInfo.InvariantCulture),
                    "--holders", Holders.ToString(CultureInfo.InvariantCulture),
                    "--peers", peers));

            var asking = Started(
                "ask",
                "--listen", ports[0],
                "--holders", Holders.ToString(CultureInfo.InvariantCulture),
                "--peers", peers,
                "--rounds", Rounds.ToString(CultureInfo.InvariantCulture));

            running.Add(asking);

            var said = await Finished(asking);

            output.WriteLine(said);

            Assert.True(asking.ExitCode == 0, $"the harness exited {asking.ExitCode}:\n{said}");

            // Every holder answered every round it was asked. A fleet losing one machine
            // learns on and scores well, which is why this is the assertion and not the score.
            Assert.Contains(
                $"council    : asked {Holders}, heard {Holders}",
                said,
                StringComparison.Ordinal);

            // And the wire lost nothing after the fleet came up.
            Assert.Contains("bus        : 0 dropped, 0 refused", said, StringComparison.Ordinal);

            // And the harness held no population, so the columns about one are absent rather
            // than nought -- which is the distinction the run would otherwise get wrong.
            Assert.Contains("holding    : 0 populations here", said, StringComparison.Ordinal);

            var scored = Regex.Match(said, @"rounds\s+: (\d+), (\d+) right, (\d+) wrong");

            Assert.True(scored.Success, $"no score line in:\n{said}");

            var rounds = int.Parse(scored.Groups[1].Value, CultureInfo.InvariantCulture);
            var right = int.Parse(scored.Groups[2].Value, CultureInfo.InvariantCulture);
            var wrong = int.Parse(scored.Groups[3].Value, CultureInfo.InvariantCulture);

            Assert.Equal(Rounds, rounds);

            // Above chance on a one-bit answer, which is the whole claim: the loop over
            // sockets learns. A fleet that came up and never learnt sits at a half.
            Assert.True(right > wrong * 2,
                $"{right} right against {wrong} wrong is not a learning loop:\n{said}");

            // And the holders were asked for what they hold, which is the only account of a
            // population this process can have.
            Assert.Contains(
                $"fleet      : {Holders} of {Holders} holder(s) answered",
                said,
                StringComparison.Ordinal);
        }
        finally
        {
            foreach (var one in running)
            {
                try
                {
                    if (!one.HasExited) one.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // Already gone, which is the ordinary case for the harness.
                }

                one.Dispose();
            }
        }
    }

    /// <summary>
    /// <b>The holder host is a real program</b>, and it says so when it is asked wrongly.
    /// </summary>
    /// <remarks>
    /// <b>The companion to the run above.</b> Without it this file passes for a host binary
    /// that cannot be found and a run that never started, because every assertion up there is
    /// about text that would then never arrive — and a test that cannot find its subject
    /// reports a failure about the subject.
    /// </remarks>
    [Fact]
    public async Task And_the_host_is_there_to_be_started()
    {
        using var asked = Started("what");

        var said = await Finished(asked);

        Assert.Equal(2, asked.ExitCode);
        Assert.Contains("usage:", said, StringComparison.Ordinal);
        Assert.Contains("hold --listen", said, StringComparison.Ordinal);
    }

    /// <summary>Starts the host with a command line.</summary>
    /// <param name="args">What a person would type after the program name.</param>
    private static Process Started(params string[] args)
    {
        var started = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        started.ArgumentList.Add(Binary());

        foreach (var one in args) started.ArgumentList.Add(one);

        return Process.Start(started)
            ?? throw new InvalidOperationException("the host did not start");
    }

    /// <summary>Everything a process said, once it has stopped saying it.</summary>
    /// <param name="one">The process.</param>
    private static async Task<string> Finished(Process one)
    {
        var said = new StringBuilder();

        var output = one.StandardOutput.ReadToEndAsync();
        var errors = one.StandardError.ReadToEndAsync();

        using var patience = new CancellationTokenSource(Patience);

        try
        {
            await one.WaitForExitAsync(patience.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            one.Kill(entireProcessTree: true);
        }

        said.Append(await output.ConfigureAwait(false));
        said.Append(await errors.ConfigureAwait(false));

        return said.ToString();
    }

    /// <summary>
    /// Where the host's own binary is.
    /// </summary>
    /// <remarks>
    /// <b>Its own output rather than the copy beside this one.</b> A project reference brings
    /// the assembly here and leaves the runtime configuration behind, so the copy is a library
    /// and the original is a program. Throws rather than skipping — a test that cannot find
    /// its subject and quietly passes reports green for a question it never asked.
    /// </remarks>
    private static string Binary()
    {
        var built = Path.Combine(Tree.Repo(), "src", "OpenPlexus.Host", "bin");

        if (!Directory.Exists(built))
            throw new DirectoryNotFoundException(
                $"`OpenPlexus.Host` has not been built: nothing at {built}");

        // The same configuration this test was built in, because a Debug suite reading a
        // stale Release binary would be measuring whatever was there last.
        var configured = AppContext.BaseDirectory.Contains(
            $"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal)
            ? "Release"
            : "Debug";

        var binary = Directory
            .GetFiles(built, "OpenPlexus.Host.dll", SearchOption.AllDirectories)
            .FirstOrDefault(one => one.Contains(
                $"{Path.DirectorySeparatorChar}{configured}{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal));

        return binary
            ?? throw new FileNotFoundException(
                $"no {configured} build of `OpenPlexus.Host` under {built}");
    }
}
