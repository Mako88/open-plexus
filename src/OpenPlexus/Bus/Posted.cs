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
/// <para>
/// <b>AND THAT PARAGRAPH WAS FALSE FROM THE DAY IT WAS WRITTEN, WHICH IS WHY IT IS STILL
/// HERE.</b> Both fan-outs awaited each post in turn, so a broadcast cost the SUM of the
/// hops and the origin was paced by the slowest machine in the fleet — the exact failure
/// the sentence above describes, sitting underneath it. Nothing caught it because nothing
/// on the thinking path had ever been timed across a socket; it turned up when the LEARNING
/// path was, because that one is measured in milliseconds and a queue shows.
/// </para>
/// <para>
/// <b>SO A DOCUMENTED PROMISE IS NOT A CHECK, AND THE ONLY REASON THIS ONE IS TRUE NOW IS
/// THAT SOMETHING PUT A CLOCK ON IT.</b> The cost of a fan-out is in
/// <c>AskedTests</c>: nine holders at two and a half times one, rather than at nine.
/// </para>
/// </remarks>
public sealed class Posted : IBus, IAsyncDisposable
{
    private readonly Dictionary<ClusterAddress, IReceiveEnvelopes> _clusters = [];
    private readonly Dictionary<MachineAddress, IReceiveReports> _machines = [];
    private readonly List<(IReceiveArrivals Machine, HashSet<Code> Codes)> _listeners = [];

    private readonly Dictionary<MachineAddress, IReceiveAsks> _holders = [];
    private readonly Dictionary<MachineAddress, IReceiveAnswers> _askers = [];

    private readonly Dictionary<ClusterAddress, string> _elsewhere = [];

    /// <summary>
    /// Where a machine that is not here can be reached.
    /// </summary>
    /// <remarks>
    /// <b>ONE TABLE FOR REPORTS AND ANSWERS BOTH, BECAUSE BOTH ARE ADDRESSED TO A
    /// MACHINE.</b> A second table keyed the same way by the same type is a second chance
    /// for one of them to be populated and the other not — and the failure would be a
    /// return path that silently drops, which is the hardest kind here to see.
    /// </remarks>
    private readonly Dictionary<MachineAddress, string> _reporting = [];

    private readonly Dictionary<MachineAddress, string> _holding = [];

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

    private long _dropped;
    private long _refused;

    /// <summary>
    /// Messages this machine could not hand over, and gave up on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A BUS THAT DROPS SILENTLY NEEDS A COUNT, OR THE LOSS IS A FACT NOTHING CAN
    /// SEE.</b> Every failed post here is swallowed on purpose — a machine that is not
    /// there is C3 rather than an error — and that reasoning is right for a DEATH and
    /// wrong for a hiccup. The two are the same event to <see cref="PostAsync"/> and they
    /// are completely different to whoever was waiting, because a fleet gathers from a
    /// denominator: one lost answer and the gathering never completes, forever, on a
    /// machine that is alive and idle.
    /// </para>
    /// <para>
    /// <b>AND THIS REPO'S OWN TRAP LIST IS ABOUT EXACTLY THIS SHAPE.</b> A cost can be in
    /// memory while every instrument watches time; here a loss was on the wire while every
    /// instrument watched arrivals. What could be seen was how many answers came back, and
    /// what could not was whether an ask ever left.
    /// </para>
    /// </remarks>
    public long Dropped => Interlocked.Read(ref _dropped);

    /// <summary>Messages this machine took delivery of and then could not act on.</summary>
    /// <remarks>
    /// <b>THE OTHER END OF THE SAME SILENCE, AND IT IS A DIFFERENT FAULT.</b> A drop is
    /// the sender failing to hand it over; this is the receiver accepting the bytes and
    /// throwing while reading or dispatching them. From the waiting asker they are one
    /// event — no answer — and only one of them is about the network.
    /// </remarks>
    public long Refused => Interlocked.Read(ref _refused);

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

    /// <summary>Every holder this machine could ask, for the tests that ask.</summary>
    /// <remarks>
    /// <b>SEPARATE FROM <see cref="Known"/> BECAUSE IT IS THE DENOMINATOR OF A
    /// GATHERING.</b> How many clusters exist decides how many replies a thought waits
    /// for; how many HOLDERS exist decides how much of a population a vote was taken over,
    /// and a run that quietly asked eleven of twelve would score like one that asked all
    /// twelve and be wrong about something nothing reports.
    /// </remarks>
    public IReadOnlyCollection<MachineAddress> Holding
    {
        get
        {
            lock (_gate)
                return
                [
                    .. _holders.Keys.Concat(_holding.Keys)
                        .Distinct()
                        .OrderBy(one => one.Value, StringComparer.Ordinal),
                ];
        }
    }

    /// <inheritdoc/>
    public event Action<ClusterAddress>? Deaths;

    /// <inheritdoc/>
    public event Action<BroadcastId, MachineAddress>? Unreached;

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
    /// <para>
    /// <b>A PEER THAT CANNOT BE REACHED IS NOT AN ERROR, WHICH IS C3 AT STARTUP.</b> A
    /// machine that is not up yet, or never will be, must not stop this one from running —
    /// so a failed announcement is dropped and the peer simply does not know about these
    /// clusters until the next one.
    /// </para>
    /// <para>
    /// <b>AND IT IS A FAN-OUT LIKE EVERY OTHER ONE HERE, WHICH IT WAS NOT UNTIL SOMETHING
    /// TIMED A FLEET COMING UP.</b> This awaited each peer in turn, so a machine opening paid
    /// the SUM of its peers rather than the slowest of them — the identical defect the class
    /// remark describes at length for a broadcast, in the method underneath it, surviving the
    /// commit that fixed the other two.
    /// </para>
    /// <para>
    /// <b>AND WHAT IT COSTS IS PAID EXACTLY WHERE IT HURTS MOST.</b> A peer that is not there
    /// costs a connect giving up — four seconds on a Windows loopback, and a real timeout on
    /// a wifi — so a fleet coming up is the one moment when most posts fail, and serialising
    /// it multiplied that by the number of machines. Twenty phones where two are off is the
    /// arrangement this is for.
    /// </para>
    /// </remarks>
    private async Task AnnounceAsync(CancellationToken ct = default)
    {
        var mine = Mine();

        await Task.WhenAll(_peers.Select(peer => PostAsync(peer, "announce", mine, ct)))
            .ConfigureAwait(false);
    }

    /// <summary>What this machine holds, as it would tell anyone.</summary>
    private Roster Mine()
    {
        lock (_gate)
            return new Roster
            {
                Host = _me,
                Clusters = [.. _clusters.Keys.Select(one => one.Value)],

                // THE UNION, BECAUSE A MACHINE IS ANNOUNCED FOR WHERE IT CAN BE REACHED
                // AND NOT FOR WHAT IT DOES WITH WHAT ARRIVES. An asker that takes answers
                // and no reports is still a machine at an address, and announcing only the
                // reporting half would leave every answer to it undeliverable.
                Machines =
                [
                    .. _machines.Keys.Concat(_askers.Keys).Select(one => one.Value).Distinct(),
                ],

                Holders = [.. _holders.Keys.Select(one => one.Value)],
            };
    }

    /// <summary>Takes in what another machine says it holds.</summary>
    /// <param name="roster">What arrived.</param>
    private void Absorb(Roster roster)
    {
        lock (_gate)
        {
            foreach (var one in roster.Clusters) _elsewhere[new ClusterAddress(one)] = roster.Host;
            foreach (var one in roster.Machines) _reporting[new MachineAddress(one)] = roster.Host;
            foreach (var one in roster.Holders) _holding[new MachineAddress(one)] = roster.Host;
        }
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
    public IDisposable Subscribe(IReceiveAsks holder)
    {
        ArgumentNullException.ThrowIfNull(holder);

        // AND LEAVING IS SILENT HERE, WHICH IS THE OPPOSITE OF A CLUSTER. See `IBus`: an
        // ask that reaches nobody is an answer that never arrives, and the asker counts
        // that for itself. A death notice would only ever cover the polite departures.
        //
        // AND `Unreached` IS NOT ONE, WHICH IS WHY THIS IS STILL SILENT WITH FORK 53 BUILT.
        // That event fires where an ask fails to leave rather than where a holder goes, so
        // it says nothing about departures and everything about deliveries -- the impolite
        // death and the dropped message reach it by the same road, which is the road that
        // does not need the dying machine to do anything.
        return Joins.At(_gate, _holders, holder.Address, holder);
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IReceiveAnswers asker)
    {
        ArgumentNullException.ThrowIfNull(asker);

        return Joins.At(_gate, _askers, asker.Address, asker);
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
    /// <remarks>
    /// <para>
    /// <b>FIRE AND FORGET, AND IT AWAITED EACH POST IN TURN UNTIL SOMETHING TIMED IT.</b>
    /// The class remark two hundred lines up has always said a fan-out to twelve clusters
    /// is twelve posts in flight rather than twelve round trips end to end. It was twelve
    /// round trips: every send awaited its peer's acknowledgement before the next one left,
    /// so a broadcast cost the SUM of the hops and the origin was paced by the slowest
    /// machine in the fleet.
    /// </para>
    /// <para>
    /// <b>WHICH PUT THE NETWORK'S LATENCY INTO THE SEARCH ONCE PER CLUSTER, exactly as that
    /// remark warned.</b> Nothing measured it because nothing on the thinking path has ever
    /// been timed across a real socket — the depth-of-a-thought number was taken on a wire
    /// whose fan-out was a queue.
    /// </para>
    /// <para>
    /// <b>AND A LOCAL CLUSTER IS DISPATCHED TOO, WHICH IS WHAT MAKES IT ACTUALLY FIRE AND
    /// FORGET.</b> Delivering to a cluster that happens to live here by direct call inside
    /// the loop would leave one machine in the fleet still able to pace a broadcast, and
    /// which one would depend on where the ring put things. <see cref="HybridBus"/> has
    /// always dispatched both alike.
    /// </para>
    /// </remarks>
    public ValueTask<IReadOnlyCollection<ClusterAddress>> BroadcastAsync(
        Envelope envelope,
        CancellationToken ct = default,
        Action<IReadOnlyCollection<ClusterAddress>>? ready = null)
    {
        var everyone = Known;

        // THE ORIGIN RECORDS ITS THOUGHT INSIDE THIS WINDOW, before anything is asked. A
        // cluster can report back before this method returns, and a report for a broadcast
        // nobody has heard of is dropped.
        ready?.Invoke(everyone);

        foreach (var cluster in everyone) Fire(cluster, envelope with { To = cluster }, ct);

        return ValueTask.FromResult(everyone);
    }

    /// <summary>Sends without waiting, and without a fault nobody will ever read.</summary>
    /// <param name="to">Which cluster.</param>
    /// <param name="envelope">What it gets.</param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <b>THE THROW HAS TO GO SOMEWHERE, AND ON THIS PATH THERE IS NOWHERE.</b>
    /// <see cref="SendAsync(ClusterAddress, Envelope, CancellationToken)"/> refuses a
    /// cluster no machine has announced, and a broadcast only ever addresses what
    /// <see cref="Known"/> held a moment ago — so the one way to reach that throw is a
    /// cluster departing inside the window, which is C3 happening rather than a routing
    /// bug. An unobserved faulted task would be the alternative, and it reports to nobody.
    /// </remarks>
    private void Fire(ClusterAddress to, Envelope envelope, CancellationToken ct) =>
        Away(() => SendAsync(to, envelope, ct).AsTask(), ct);

    /// <summary>Asks one holder without waiting for it.</summary>
    /// <param name="who">Which holder.</param>
    /// <param name="here">It, if it lives on this machine.</param>
    /// <param name="there">Where it lives, if it does not.</param>
    /// <param name="ask">The question.</param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <b>THE ONE DELIVERY ON THIS BUS WHOSE FAILURE SOMEBODY IS WAITING ON, WHICH IS WHY
    /// IT IS THE ONE NOT SIMPLY SWALLOWED.</b> Every other path here loses a message and
    /// costs whoever needed it a fact; this one loses a message and costs an asker its
    /// denominator, so the gathering never completes and a fleet that is alive and idle
    /// stops for good. See <see cref="IBus.Unreached"/>.
    /// </remarks>
    private void Fire(
        MachineAddress who, IReceiveAsks? here, string? there, Ask ask, CancellationToken ct) =>
        Away(
            async () =>
            {
                try
                {
                    if (here is not null)
                    {
                        await here.DeliverAsync(ask, ct).ConfigureAwait(false);
                        return;
                    }

                    if (await PostAsync(
                            there!, $"ask/{Uri.EscapeDataString(who.Value)}", ask, ct)
                        .ConfigureAwait(false))
                        return;
                }
                catch (Exception) when (!ct.IsCancellationRequested)
                {
                    // A HOLDER ON THIS MACHINE THAT THREW WHILE TAKING THE QUESTION, which
                    // is `Refused` rather than `Dropped` and is the same event to whoever
                    // asked: no answer to this ask is coming from there.
                }

                // AND IT COMES OFF THE ROSTER, WHICH IS THE WALK'S `died` PATH REACHED BY
                // OBSERVATION RATHER THAN BY ANNOUNCEMENT. Measured before this line
                // existed: a post to a machine that has closed its door costs a flat four
                // seconds on loopback -- the transport's own give-up, not a wait anybody
                // here chose -- so a fleet that kept asking a dead holder paid that every
                // round and a four-thousand-round run became four hours. The write-off
                // alone makes a fleet CORRECT after a death and this is what makes it
                // usable.
                //
                // ONLY THE REMOTE HALF, because a local holder that threw is alive and
                // still subscribed; unsubscribing it here would delete a machine over a
                // fault in one answer.
                //
                // AND IT COMES BACK BY ANNOUNCING, WHICH IS THE ONLY WAY IN AND IS A PUSH.
                // `Absorb` re-enters a holder the moment its machine opens, so a phone out
                // of a tunnel rejoins by saying so. What this cannot recover is a machine
                // that stayed up while one message to it was lost -- it is out until it
                // announces again, which is the hiccup being charged a death's price. That
                // is the trade fork 53 makes and it is written in the plan as one.
                if (there is not null) lock (_gate) _holding.Remove(who);

                Unreached?.Invoke(ask.Broadcast, who);
            },
            ct);

    /// <summary>
    /// Runs a delivery on its own and drops whatever it throws.
    /// </summary>
    /// <param name="delivery">What to do.</param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <b>A FAULT ON THIS PATH HAS NOWHERE TO GO, AND AN UNOBSERVED TASK IS WORSE THAN A
    /// SWALLOWED ONE.</b> The caller has already been handed its answer — who was asked, or
    /// who is about to be sent to — so there is no return value left to carry a failure on.
    /// What reaches here is a cluster or a holder departing inside the window between being
    /// listed and being sent to, which is C3 happening rather than a routing bug.
    /// </remarks>
    private static void Away(Func<Task> delivery, CancellationToken ct) =>
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await delivery().ConfigureAwait(false);
                }
                catch (Exception) when (!ct.IsCancellationRequested)
                {
                }
            },
            CancellationToken.None);

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
    public ValueTask<IReadOnlyCollection<MachineAddress>> AskAsync(
        Ask ask,
        CancellationToken ct = default,
        Action<IReadOnlyCollection<MachineAddress>>? ready = null)
    {
        ArgumentNullException.ThrowIfNull(ask);

        List<(MachineAddress Who, IReceiveAsks? Here, string? There)> going;

        lock (_gate)
            going =
            [
                .. _holders.Select(one => (one.Key, (IReceiveAsks?)one.Value, (string?)null))
                    .Concat(_holding
                        .Where(one => !_holders.ContainsKey(one.Key))
                        .Select(one => (one.Key, (IReceiveAsks?)null, (string?)one.Value)))
                    .OrderBy(one => one.Item1.Value, StringComparer.Ordinal),
            ];

        IReadOnlyCollection<MachineAddress> everyone = [.. going.Select(one => one.Who)];

        // THE ASKER RECORDS ITS GATHERING INSIDE THIS WINDOW, before anything is asked. A
        // local holder answers by direct call and can be back before this method returns,
        // and an answer to an ask nobody remembers is dropped.
        ready?.Invoke(everyone);

        // EVERY HOLDER ASKED AT ONCE AND NONE OF THEM WAITED ON, WHICH IS FORK 56'S PRICE.
        // The gate's query is about nine asks a round, all askable at once, so ONE round
        // trip -- and a fan-out that awaited each peer's acknowledgement would cost nine
        // however concurrent the answers were.
        foreach (var (who, here, there) in going)
            Fire(who, here, there, ask, ct);

        return ValueTask.FromResult(everyone);
    }

    /// <inheritdoc/>
    public async ValueTask SendAsync(
        MachineAddress to, Answer answer, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(answer);

        IReceiveAnswers? here;
        string? there;

        lock (_gate)
        {
            _askers.TryGetValue(to, out here);
            _reporting.TryGetValue(to, out there);
        }

        if (here is not null)
        {
            await here.DeliverAsync(answer, ct).ConfigureAwait(false);
            return;
        }

        // AN ANSWER WITH NOWHERE TO GO IS DROPPED, exactly as a report is. The asker may
        // have died between asking and being answered, which C3 says is ordinary — and
        // throwing here would make one machine's departure another machine's error.
        if (there is not null)
            await PostAsync(there, $"answer/{Uri.EscapeDataString(to.Value)}", answer, ct)
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
        //
        // ALL AT ONCE, FOR THE REASON `AnnounceAsync` GIVES. This is the widest fan-out on
        // the bus -- every machine, not every machine holding something -- and it awaited
        // each peer in turn, so publishing a finished thought was paced by the whole fleet
        // in series while the class remark above promised otherwise.
        await Task.WhenAll(_peers.Select(peer => PostAsync(peer, "settled", settled, ct)))
            .ConfigureAwait(false);
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
            //
            // AND COUNTED, FOR THE REASON `Refused` GIVES: nobody reads noise, and nobody
            // could read a nought either.
            Interlocked.Increment(ref _refused);

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

            case "ask":
                IReceiveAsks? holder;
                lock (_gate) _holders.TryGetValue(new MachineAddress(who), out holder);

                if (holder is not null)
                    await holder.DeliverAsync(Wire.Read<Ask>(sent), ct).ConfigureAwait(false);
                break;

            case "answer":
                IReceiveAnswers? asker;
                lock (_gate) _askers.TryGetValue(new MachineAddress(who), out asker);

                if (asker is not null)
                    await asker.DeliverAsync(Wire.Read<Answer>(sent), ct).ConfigureAwait(false);
                break;

            // AN ANNOUNCEMENT IS ANSWERED BY ONE, AND WITHOUT THAT THE CLASS'S OWN CLAIM
            // ABOUT LATE ARRIVAL WAS HALF TRUE. A machine announces when it opens, and a
            // peer that is not up yet drops it -- so the machine that opened FIRST tells
            // nobody and is told by everybody. Its own routes work and nothing can route
            // back to it, which for the thinking path is a lost report and for this one is
            // every answer undeliverable.
            //
            // ONE REPLY AND NEVER A SECOND, WHICH IS WHAT THE SEPARATE PATH BUYS. A reply
            // that was itself answered would be two machines announcing at each other for
            // the life of the run; `announced` is the same message with no reply owed, so
            // the exchange closes in one round trip whoever opened first.
            case "announce":
                var asking = Wire.Read<Roster>(sent);

                Absorb(asking);

                await PostAsync(asking.Host, "announced", Mine(), ct).ConfigureAwait(false);
                break;

            case "announced":
                Absorb(Wire.Read<Roster>(sent));
                break;

            case "died":
                var gone = new ClusterAddress(who);

                lock (_gate) _elsewhere.Remove(gone);

                Deaths?.Invoke(gone);
                break;

            default: break;
        }
    }

    /// <summary>Posts one message to one peer, and says whether it was handed over.</summary>
    /// <param name="host">Which peer.</param>
    /// <param name="path">What kind of message.</param>
    /// <param name="what">The message.</param>
    /// <param name="ct">Cancellation.</param>
    /// <returns>
    /// Whether the peer took it. <b>Read on the ask path alone, and ignored everywhere
    /// else</b> — a report or an envelope that does not arrive is a loss somebody counts,
    /// while an ask that does not arrive is an answer somebody is waiting on forever.
    /// </returns>
    private async Task<bool> PostAsync(string host, string path, object what, CancellationToken ct)
    {
        try
        {
            if (!_clients.TryGetValue(host, out var client)) return false;

            await client.MakeRequest(
                new SimpleRequest($"/{path}", HttpMethod.Post) { StringBody = Wire.Write(what) },
                ct).ConfigureAwait(false);

            return true;
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // C3: A MACHINE THAT IS NOT THERE IS NORMAL AND NOT AN ERROR. The design says
            // a cluster vanishing mid-thought is expected, so a refused connection is the
            // same event arriving by a faster road than a timeout.
            //
            // AND IT IS COUNTED NOW, BECAUSE THE SENTENCE ABOVE IS TRUE OF A DEATH AND
            // FALSE OF A HICCUP. See `Dropped`.
            Interlocked.Increment(ref _dropped);

            return false;
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

        /// <summary>
        /// The holders of commitments it has, which can be asked.
        /// </summary>
        /// <remarks>
        /// <b>ANNOUNCED SEPARATELY FROM <see cref="Machines"/> BECAUSE AN ASK IS A
        /// BROADCAST AND A REPORT IS NOT.</b> A report goes to one named machine, so the
        /// roster only has to say where that one is; an ask goes to EVERY holder, so the
        /// roster is what decides the denominator of a gathering. Folding them together
        /// would put every input machine and every actuator into the vote's population
        /// count, and each of them would then be a holder that never answers.
        /// </remarks>
        public ImmutableArray<string> Holders { get; init; } = [];
    }

    /// <summary>
    /// Shuts the door. <b>Twice is the same as once.</b>
    /// </summary>
    /// <remarks>
    /// <b>AND IT THREW THE SECOND TIME UNTIL SOMETHING KILLED A MACHINE ON PURPOSE.</b>
    /// <see cref="CancellationTokenSource.CancelAsync"/> on a disposed source is an
    /// <see cref="ObjectDisposedException"/>, so a harness that took a machine down mid-run
    /// and then tore the fleet down could not do both — which reads as the C3 test failing
    /// while every assertion in it has already passed. A machine dying and later being
    /// cleaned up is the ORDINARY sequence for this class, not a misuse of it.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            if (_shut) return;
            _shut = true;
        }

        await _closing.CancelAsync().ConfigureAwait(false);

        // ABORTED RATHER THAN CLOSED, AND THE DIFFERENCE WAS MINUTES A MACHINE.
        //
        // `Close` is the polite shutdown: it waits for the request queue to drain, and
        // every peer holds a keep-alive connection to this listener that nothing on this
        // side can shut. A fleet coming down therefore paid a wait per machine that grew
        // with how many peers were pointed at it -- a run of six hundred rounds took under
        // a second and the teardown around it took minutes, which is what made a grid of
        // twelve fleets look exactly like a deadlock.
        //
        // AND ABORT IS THE HONEST SEMANTICS HERE ANYWAY, WHICH IS WHY THIS IS NOT A
        // WORKAROUND. C3 says a machine vanishing mid-thought is normal; a phone going into
        // a tunnel does not drain its request queue first. What this discards is exactly
        // what a death discards.
        _door.Abort();
        _closing.Dispose();

        // AND THE CLIENTS ARE NOT DISPOSED, BECAUSE THEY CANNOT BE. `ISimpleClient` has no
        // `Dispose`, so the line that used to sit here -- `(client as IDisposable)?.Dispose()`
        // -- read as cleanup and did nothing at all, which is this repo's oldest shape of
        // defect wearing a tidy face. One client per host is what keeps a fan-out from
        // paying a handshake per peer; what it costs is that a machine's connection pool
        // outlives the machine, and only a process ending collects it.
    }

    private bool _shut;
}
