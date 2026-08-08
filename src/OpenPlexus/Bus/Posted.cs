using System.Collections.Immutable;
using System.Net;
using System.Text;
using OpenPlexus.Codes;
using OpenPlexus.Thinking;
using SimpleHttpClient;
using SimpleHttpClient.Models;

namespace OpenPlexus.Bus;

/// <summary>Another machine, and where to reach it.</summary>
/// <param name="Host">Its base address, scheme and all.</param>
public readonly record struct Peer(string Host);

/// <summary>
/// The bus, over a wire, between processes that share nothing.
/// </summary>
/// <remarks>
/// <para>
/// <b>THIS IS THE FIRST THING IN THE PROJECT THAT CAN ACTUALLY BE DISTRIBUTED.</b>
/// <see cref="HybridBus"/> is a dictionary of clusters called through
/// <see cref="Task.Run(Action)"/> with delays sprinkled in, so C2 and C3 have been
/// honoured by a SIMULATION of a network for the life of the repo. Twenty phones cannot
/// run a dictionary lookup between them.
/// </para>
/// <para>
/// <b>AND `HybridBus` STAYS THE HARSHER TEST, WHICH IS THE PART THAT WILL BE ASSUMED THE
/// OTHER WAY ROUND.</b> It reorders deliveries on purpose because C2 says messages arrive
/// out of order; HTTP over TCP does not reorder within a connection, so a run over this
/// exercises LESS adversity than a run in one process. A green distributed run is
/// therefore not evidence that C2 is satisfied — the simulator is where that is measured,
/// and this is where it is measured that the bytes are right.
/// </para>
/// <para>
/// <b>WHERE A CLUSTER LIVES IS LEARNED AND NOT CONFIGURED.</b> A machine announces the
/// addresses it holds when they subscribe, so a roster of hosts is all any of them is
/// told — which is what lets a machine arrive late, and is the only shape that survives
/// C3, since a machine that dies and returns announces itself again.
/// </para>
/// <para>
/// <b>SENDS DO NOT WAIT ON RECEIVERS, exactly as the interface promises.</b> A fan-out to
/// twelve clusters is twelve posts in flight rather than twelve round trips end to end;
/// awaiting each would turn a broadcast into a queue and put the network's latency into
/// the search once per hop.
/// </para>
/// </remarks>
public sealed class Posted : IBus, IAsyncDisposable
{
    private readonly Dictionary<ClusterAddress, IReceiveEnvelopes> _clusters = [];
    private readonly Dictionary<MachineAddress, IReceiveReports> _machines = [];
    private readonly List<(IReceiveArrivals Machine, HashSet<Code> Codes)> _listeners = [];

    private readonly Dictionary<ClusterAddress, string> _elsewhere = [];
    private readonly Dictionary<MachineAddress, string> _reporting = [];

    private readonly HashSet<string> _peers;

    /// <summary>
    /// One client per peer, made once.
    /// </summary>
    /// <remarks>
    /// <b>NOT ONE PER SEND, WHICH IS THE PITFALL `SimpleHttpClient` EXISTS TO AVOID.</b>
    /// A client made per request leaks sockets into TIME_WAIT and eventually cannot open
    /// another; one held per host pools its connections, which is also what keeps a
    /// fan-out from paying a handshake per cluster.
    /// </remarks>
    private readonly Dictionary<string, ISimpleClient> _clients;
    private readonly HttpListener _door = new();
    private readonly CancellationTokenSource _closing = new();
    private readonly Lock _gate = new();

    private readonly string _me;

    /// <param name="me">This machine's own base address, which peers will post back to.</param>
    /// <param name="peers">Every other machine's base address.</param>
    /// <exception cref="ArgumentException">The address is not one a listener can hold.</exception>
    public Posted(string me, IEnumerable<Peer> peers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(me);
        ArgumentNullException.ThrowIfNull(peers);

        _me = me.TrimEnd('/');
        _peers = [.. peers.Select(one => one.Host.TrimEnd('/')).Where(one => one != _me)];
        _clients = _peers.ToDictionary(one => one, one => (ISimpleClient)new SimpleClient(one), StringComparer.Ordinal);

        _door.Prefixes.Add($"{_me}/");
    }

    /// <summary>Everything this machine has heard about, for the tests that ask.</summary>
    /// <remarks>
    /// <b>A machine's picture of the world is PARTIAL and that is not a fault</b> — it
    /// knows the clusters that have announced themselves to it, which is every one that
    /// was alive and reachable when it subscribed and no others.
    /// </remarks>
    public IReadOnlyCollection<ClusterAddress> Known
    {
        get { lock (_gate) return [.. _clusters.Keys.Concat(_elsewhere.Keys).Order()]; }
    }

    /// <inheritdoc/>
    public event Action<ClusterAddress>? Deaths;

    /// <summary>Opens the door and tells everyone what this machine holds.</summary>
    /// <param name="ct">Cancellation.</param>
    public async Task OpenAsync(CancellationToken ct = default)
    {
        _door.Start();

        _ = Task.Run(() => AnswerAsync(_closing.Token), CancellationToken.None);

        await AnnounceAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Tells every peer which clusters and machines live here.
    /// </summary>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <b>A PEER THAT CANNOT BE REACHED IS NOT AN ERROR, WHICH IS C3 AT STARTUP.</b> A
    /// machine that is not up yet, or never will be, must not stop this one from running —
    /// so a failed announcement is dropped and the peer simply does not know about these
    /// clusters until the next one.
    /// </remarks>
    private async Task AnnounceAsync(CancellationToken ct = default)
    {
        Roster mine;
        lock (_gate) mine = new Roster
        {
            Host = _me,
            Clusters = [.. _clusters.Keys.Select(one => one.Value)],
            Machines = [.. _machines.Keys.Select(one => one.Value)],
        };

        foreach (var peer in _peers) await PostAsync(peer, "announce", mine, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IReceiveEnvelopes cluster)
    {
        ArgumentNullException.ThrowIfNull(cluster);

        lock (_gate) _clusters.Add(cluster.Address, cluster);

        return new Leaves(() =>
        {
            lock (_gate) _clusters.Remove(cluster.Address);

            // LEAVING IS NOT SILENT, which is the whole reason this is a bus. Every peer
            // is told, because a route heading into a cluster that has gone is stranded
            // and the origin can only write it off if somebody says so.
            foreach (var peer in _peers)
                _ = PostAsync(peer, $"died/{Uri.EscapeDataString(cluster.Address.Value)}", "", default);

            Deaths?.Invoke(cluster.Address);
        });
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IReceiveReports machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        lock (_gate) _machines.Add(machine.Address, machine);

        return new Leaves(() => { lock (_gate) _machines.Remove(machine.Address); });
    }

    /// <inheritdoc/>
    public IDisposable Listen(IReceiveArrivals machine, IReadOnlyCollection<Code> codes)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(codes);

        var entry = (machine, new HashSet<Code>(codes));

        lock (_gate) _listeners.Add(entry);

        return new Leaves(() => { lock (_gate) _listeners.Remove(entry); });
    }

    /// <inheritdoc/>
    public async ValueTask SendAsync(
        ClusterAddress to, Envelope envelope, CancellationToken ct = default)
    {
        IReceiveEnvelopes? here;
        string? there;

        lock (_gate)
        {
            _clusters.TryGetValue(to, out here);
            _elsewhere.TryGetValue(to, out there);
        }

        if (here is not null)
        {
            await here.DeliverAsync(envelope, ct).ConfigureAwait(false);
            return;
        }

        if (there is null) throw Unreachable(to.Value);

        await PostAsync(there, $"envelope/{Uri.EscapeDataString(to.Value)}", envelope, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask<IReadOnlyCollection<ClusterAddress>> BroadcastAsync(
        Envelope envelope,
        CancellationToken ct = default,
        Action<IReadOnlyCollection<ClusterAddress>>? ready = null)
    {
        var everyone = Known;

        // THE ORIGIN RECORDS ITS THOUGHT INSIDE THIS WINDOW, before anything is asked. A
        // cluster can report back before this method returns, and a report for a broadcast
        // nobody has heard of is dropped.
        ready?.Invoke(everyone);

        foreach (var cluster in everyone)
            await SendAsync(cluster, envelope with { To = cluster }, ct).ConfigureAwait(false);

        return everyone;
    }

    /// <inheritdoc/>
    public async ValueTask SendAsync(
        MachineAddress to, Report report, CancellationToken ct = default)
    {
        IReceiveReports? here;
        string? there;

        lock (_gate)
        {
            _machines.TryGetValue(to, out here);
            _reporting.TryGetValue(to, out there);
        }

        if (here is not null)
        {
            await here.DeliverAsync(report, ct).ConfigureAwait(false);
            return;
        }

        // A REPORT WITH NOWHERE TO GO IS DROPPED RATHER THAN THROWN. The machine that
        // asked may have died mid-thought, which C3 says is normal; a route that cannot
        // report is exactly the stranded route the accounting already knows how to lose.
        if (there is not null)
            await PostAsync(there, $"report/{Uri.EscapeDataString(to.Value)}", report, ct)
                .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask PublishAsync(Settled settled, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settled);

        await DeliverLocallyAsync(settled, ct).ConfigureAwait(false);

        // AND TO EVERY PEER, BECAUSE THE SENDER DOES NOT KNOW WHO LISTENS. Fork 11 routes
        // a finished thought by the CODES it reached rather than by an address, so the
        // only way to honour that across machines is for each to decide for itself.
        foreach (var peer in _peers) await PostAsync(peer, "settled", settled, ct).ConfigureAwait(false);
    }

    private async Task DeliverLocallyAsync(Settled settled, CancellationToken ct)
    {
        List<IReceiveArrivals> wanting;

        lock (_gate)
            wanting =
            [
                .. _listeners
                    .Where(one => settled.Arrivals.Any(reached => one.Codes.Contains(reached.Endpoint)))
                    .Select(one => one.Machine),
            ];

        foreach (var machine in wanting)
            await machine.DeliverAsync(settled, ct).ConfigureAwait(false);
    }

    /// <summary>What arrived, and who it is for.</summary>
    private async Task AnswerAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext asked;

            try { asked = await _door.GetContextAsync().ConfigureAwait(false); }
            catch (HttpListenerException) { return; }
            catch (ObjectDisposedException) { return; }

            _ = Task.Run(() => TakeAsync(asked, ct), CancellationToken.None);
        }
    }

    private async Task TakeAsync(HttpListenerContext asked, CancellationToken ct)
    {
        try
        {
            using var body = new StreamReader(asked.Request.InputStream, Encoding.UTF8);

            var sent = await body.ReadToEndAsync(ct).ConfigureAwait(false);
            var path = asked.Request.Url?.AbsolutePath.Trim('/') ?? string.Empty;

            await ActOnAsync(path, sent, ct).ConfigureAwait(false);

            asked.Response.StatusCode = (int)HttpStatusCode.Accepted;
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // A MALFORMED OR UNDELIVERABLE MESSAGE IS DROPPED AND NOT RETURNED AS AN
            // ERROR. C2 makes a lost message indistinguishable from a late one, and the
            // sender is not waiting on this answer anyway -- so failing loudly here would
            // only produce noise nobody reads.
            asked.Response.StatusCode = (int)HttpStatusCode.Accepted;
        }
        finally
        {
            asked.Response.Close();
        }
    }

    private async Task ActOnAsync(string path, string sent, CancellationToken ct)
    {
        var at = path.IndexOf('/', StringComparison.Ordinal);
        var what = at < 0 ? path : path[..at];
        var who = at < 0 ? string.Empty : Uri.UnescapeDataString(path[(at + 1)..]);

        switch (what)
        {
            case "envelope":
                IReceiveEnvelopes? cluster;
                lock (_gate) _clusters.TryGetValue(new ClusterAddress(who), out cluster);

                if (cluster is not null)
                    await cluster.DeliverAsync(Wire.Read<Envelope>(sent), ct).ConfigureAwait(false);
                break;

            case "report":
                IReceiveReports? machine;
                lock (_gate) _machines.TryGetValue(new MachineAddress(who), out machine);

                if (machine is not null)
                    await machine.DeliverAsync(Wire.Read<Report>(sent), ct).ConfigureAwait(false);
                break;

            case "settled":
                await DeliverLocallyAsync(Wire.Read<Settled>(sent), ct).ConfigureAwait(false);
                break;

            case "announce":
                var roster = Wire.Read<Roster>(sent);

                lock (_gate)
                {
                    foreach (var one in roster.Clusters) _elsewhere[new ClusterAddress(one)] = roster.Host;
                    foreach (var one in roster.Machines) _reporting[new MachineAddress(one)] = roster.Host;
                }

                break;

            case "died":
                var gone = new ClusterAddress(who);

                lock (_gate) _elsewhere.Remove(gone);

                Deaths?.Invoke(gone);
                break;

            default: break;
        }
    }

    private async Task PostAsync(string host, string path, object what, CancellationToken ct)
    {
        try
        {
            if (!_clients.TryGetValue(host, out var client)) return;

            await client.MakeRequest(
                new SimpleRequest($"/{path}", HttpMethod.Post) { StringBody = Wire.Write(what) },
                ct).ConfigureAwait(false);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // C3: A MACHINE THAT IS NOT THERE IS NORMAL AND NOT AN ERROR. The design says
            // a cluster vanishing mid-thought is expected, so a refused connection is the
            // same event arriving by a faster road than a timeout.
        }
    }

    private static InvalidOperationException Unreachable(string cluster) =>
        new($"no machine holds cluster '{cluster}', and none has announced one");

    /// <summary>What a machine holds, as it tells its peers.</summary>
    /// <remarks>
    /// <b>Internal, because it is a shape on the wire and not part of what a bus offers.</b>
    /// </remarks>
    /// <remarks>
    /// <b>Strings rather than the address types, because a roster is the one message whose
    /// contents are addresses</b> — and an address is a record struct over a string, so
    /// naming it here would buy a wrapper and no safety.
    /// </remarks>
    internal sealed record Roster
    {
        /// <summary>Where the machine sending this can be reached.</summary>
        public required string Host { get; init; }

        /// <summary>The clusters it holds.</summary>
        public required ImmutableArray<string> Clusters { get; init; }

        /// <summary>The machines it holds.</summary>
        public required ImmutableArray<string> Machines { get; init; }
    }

    private sealed class Leaves(Action going) : IDisposable
    {
        private bool _gone;

        public void Dispose()
        {
            if (_gone) return;

            _gone = true;
            going();
        }
    }

    /// <summary>Shuts the door.</summary>
    public async ValueTask DisposeAsync()
    {
        await _closing.CancelAsync().ConfigureAwait(false);

        _door.Close();
        _closing.Dispose();

        foreach (var client in _clients.Values) (client as IDisposable)?.Dispose();
    }
}
