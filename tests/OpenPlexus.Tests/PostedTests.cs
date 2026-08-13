using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Two machines that share nothing but a socket.
/// </summary>
/// <remarks>
/// <para>
/// <b>EVERY DISTRIBUTED CLAIM THIS PROJECT HAS MADE HAS BEEN MEASURED IN ONE PROCESS.</b>
/// <see cref="HybridBus"/> is a dictionary and a <see cref="Task.Delay(int)"/>, so C1, C2
/// and C3 have been honoured against a simulation of a network. These tests use real
/// sockets on real ports, so a message that does not serialise, does not route, or does
/// not arrive fails here rather than on twenty phones.
/// </para>
/// <para>
/// <b>AND THEY ARE NOT A TEST OF C2, WHICH IS THE THING THAT WILL BE ASSUMED.</b> TCP does
/// not reorder within a connection, so this exercises LESS adversity than the simulator
/// does. Green here means the bytes and the routing are right; the ordering constraint is
/// measured where it is injected on purpose.
/// </para>
/// </remarks>
public sealed class PostedTests(ITestOutputHelper output)
{
    /// <inheritdoc cref="Wired.Free"/>
    private static string Free() => Wired.Free();

    /// <summary>
    /// <b>NO TWO MACHINES ARE OFFERED THE SAME PORT, AND THEY CAN ALL HOLD IT AT ONCE.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE BUDGET FOR A FAILURE CLASS THAT ARRIVED ON CI AND CANNOT ARRIVE HERE ON
    /// DEMAND.</b> <see cref="Wired.Free"/> releases a port before handing it over, because
    /// the listener that wants it could not otherwise bind — so between one call and the next
    /// the kernel is entitled to offer the same one again, and a fleet asking for five in a
    /// row got two the same. What it looks like is a machine failing to open with a message
    /// about an existing registration, in a shard where every test brings up a fleet.
    /// </para>
    /// <para>
    /// <b>AND THE SECOND HALF IS WHAT THE FIRST DOES NOT SAY.</b> Distinct integers are not
    /// the requirement — simultaneously bindable ones are, which is what a fleet actually
    /// does with them. Binding them all at once asks the question in the form the failure
    /// took, and a set of distinct-but-unusable ports would pass the count and fail this.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_two_machines_are_offered_one_port_and_all_of_them_bind_at_once()
    {
        const int Wanted = 24;

        var hosts = Enumerable.Range(0, Wanted).Select(_ => Free()).ToList();

        Assert.Equal(Wanted, hosts.Distinct(StringComparer.Ordinal).Count());

        var doors = new List<System.Net.HttpListener>();

        try
        {
            foreach (var host in hosts)
            {
                var door = new System.Net.HttpListener();

                door.Prefixes.Add($"{host}/");
                door.Start();

                doors.Add(door);
            }
        }
        finally
        {
            foreach (var door in doors) door.Abort();
        }

        output.WriteLine($"{Wanted} ports handed out and held at once");
    }

    /// <summary>
    /// A peer that takes a message and is slow about saying so.
    /// </summary>
    /// <param name="host">Where it listens.</param>
    /// <param name="taking">How long it holds each request before answering.</param>
    /// <remarks>
    /// <b>SLOWNESS RATHER THAN ABSENCE, AND THAT IS WHAT MAKES THE READING PORTABLE.</b> The
    /// obvious way to show a fan-out is a queue is to point it at peers that are not there
    /// and time it — which measures the platform's connect timeout, four seconds on a Windows
    /// loopback and nothing at all on a Linux runner. A peer that answers deliberately late
    /// costs the same everywhere, so the comparison is about the sender.
    /// </remarks>
    private sealed class Dawdles(string host, TimeSpan taking) : IDisposable
    {
        private readonly System.Net.HttpListener _door = Opened(host);

        /// <summary>How many messages it has been handed.</summary>
        public int Took;

        private static System.Net.HttpListener Opened(string at)
        {
            var door = new System.Net.HttpListener();

            door.Prefixes.Add($"{at}/");
            door.Start();

            return door;
        }

        /// <summary>Answers, slowly, until it is shut.</summary>
        public async Task ServeAsync()
        {
            while (_door.IsListening)
            {
                System.Net.HttpListenerContext asked;

                try { asked = await _door.GetContextAsync().ConfigureAwait(false); }
                catch (Exception) { return; }

                Interlocked.Increment(ref Took);

                _ = Task.Run(async () =>
                {
                    await Task.Delay(taking).ConfigureAwait(false);

                    asked.Response.StatusCode = 202;
                    asked.Response.Close();
                });
            }
        }

        /// <inheritdoc/>
        public void Dispose() => _door.Abort();
    }

    /// <summary>
    /// <b>A MACHINE COMING UP PAYS THE SLOWEST PEER AND NOT THE SUM OF THEM.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE THIRD TIME THIS FAN-OUT DEFECT HAS BEEN FOUND IN ONE FILE, AND THE FIRST TIME
    /// ANYTHING CHECKS FOR IT.</b> <c>Posted</c>'s own header has always said a fan-out is
    /// posts in flight rather than round trips end to end; it was false of both fan-outs
    /// until something timed the learning path, and it stayed false of the announcement and
    /// the publish underneath the sentence that fixed them.
    /// </para>
    /// <para>
    /// <b>AND THE ANNOUNCEMENT IS THE WORST PLACE FOR IT, WHICH IS WHY IT IS THE ONE TIMED
    /// HERE.</b> Coming up is the one moment when most posts fail, because the peers are
    /// coming up too — so a serial announce multiplies whatever a missing machine costs by
    /// the size of the fleet. Twenty phones where two are off is the arrangement this is for.
    /// </para>
    /// <para>
    /// <b>THE BAR IS THE ARITHMETIC OF A QUEUE RATHER THAN A DURATION.</b> Six peers each
    /// holding a request a fifth of a second is 1.2 seconds in series and about a fifth in
    /// flight; asserting under half the serial cost fails a queue and passes anything
    /// concurrent, without asserting how fast this machine is.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task A_machine_announces_to_every_peer_at_once_rather_than_in_turn()
    {
        const int Peers = 6;

        var taking = TimeSpan.FromMilliseconds(200);

        var hosts = Enumerable.Range(0, Peers).Select(_ => Free()).ToList();
        var slow = hosts.Select(one => new Dawdles(one, taking)).ToList();

        try
        {
            foreach (var one in slow) _ = one.ServeAsync();

            await using var machine = new Posted(Free(), hosts.Select(one => new Peer(one)));

            var clock = System.Diagnostics.Stopwatch.StartNew();

            await machine.OpenAsync();

            var spent = clock.Elapsed;

            // EVERY PEER WAS ACTUALLY TOLD, which is the half a fire-and-forget could
            // silently lose -- a machine that announced to nobody is the fastest of all.
            Assert.All(slow, one => Assert.Equal(1, one.Took));

            var queued = taking * Peers;

            Assert.True(spent < queued / 2,
                $"announcing to {Peers} peers each dawdling {taking.TotalMilliseconds:F0} ms "
                + $"took {spent.TotalMilliseconds:F0} ms against {queued.TotalMilliseconds:F0} "
                + "ms of them in series, so the fan-out is a queue");

            output.WriteLine(
                $"{Peers} peers at {taking.TotalMilliseconds:F0} ms each | announced in "
                + $"{spent.TotalMilliseconds:F0} ms against {queued.TotalMilliseconds:F0} ms "
                + "in series");
        }
        finally
        {
            foreach (var one in slow) one.Dispose();
        }
    }

}
