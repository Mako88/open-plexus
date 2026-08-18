using System.Collections.Immutable;
using System.Net;
using System.Text;
using OpenPlexus.Codes;
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
/// <b>This is the first thing in the project</b> that can actually be distributed.
/// <see cref="HybridBus"/> is a dictionary of holders called through
/// <see cref="Task.Run(Action)"/> with delays sprinkled in, so C2 and C3 have been
/// honoured by a SIMULATION of a network for the life of the repo. Twenty phones cannot
/// run a dictionary lookup between them.
/// </para>
/// <para>
/// <b>AND `HybridBus` stays the harsher test</b>, which is the part that will be assumed the
/// other way round. It reorders deliveries on purpose because C2 says messages arrive
/// out of order; HTTP over TCP does not reorder within a connection, so a run over this
/// exercises LESS adversity than a run in one process. A green distributed run is
/// therefore not evidence that C2 is satisfied — the simulator is where that is measured,
/// and this is where it is measured that the bytes are right.
/// </para>
/// <para>
/// <b>Where a holder lives is learned and not configured.</b> A machine announces the
/// addresses it holds when they subscribe, so a roster of hosts is all any of them is
/// told — which is what lets a machine arrive late, and is the only shape that survives
/// C3, since a machine that dies and returns announces itself again.
/// </para>
/// <para>
/// <b>Sends do not wait on receivers, exactly as the interface promises.</b> A fan-out to
/// twelve holders is twelve posts in flight rather than twelve round trips end to end;
/// awaiting each would turn a broadcast into a queue and put the network's latency into
/// the search once per hop.
/// </para>
/// <para>
/// <b>And that paragraph was false from the day it was written</b>, which is why it is still
/// here. Both fan-outs awaited each post in turn, so a broadcast cost the SUM of the
/// hops and the origin was paced by the slowest machine in the fleet — the exact failure
/// the sentence above describes, sitting underneath it. Nothing caught it because nothing
/// on the thinking path had ever been timed across a socket; it turned up when the LEARNING
/// path was, because that one is measured in milliseconds and a queue shows.
/// </para>
/// <para>
/// <b>So a documented promise is not a check</b>, and the only reason this one is true now is
/// that something put a clock on it. The cost of a fan-out is in
/// <c>AskedTests</c>: nine holders at two and a half times one, rather than at nine.
/// </para>
/// </remarks>
public sealed class Posted : IBus, IAsyncDisposable
{
    private readonly Dictionary<MachineAddress, IReceiveAsks> _holders = [];
    private readonly Dictionary<MachineAddress, IReceiveAnswers> _askers = [];

    /// <summary>
    /// Where an asker that is not here can be reached.
    /// </summary>
    /// <remarks>
    /// <b>Separate from <see cref="_holding"/></b>, because an ask is a broadcast and an
    /// answer is not. An answer goes to the one machine that asked, so this table only has to
    /// say where that one is; an ask goes to every holder, so the other one decides the
    /// denominator of a gathering. Folding them together would put every asker into the
    /// vote's population count, and each would then be a holder that never answers.
    /// </remarks>
    private readonly Dictionary<MachineAddress, string> _answering = [];

    private readonly Dictionary<MachineAddress, string> _holding = [];

    private readonly HashSet<string> _peers;

    /// <summary>
    /// One client per peer, made once.
    /// </summary>
    /// <remarks>
    /// <b>Not one per send, which is the pitfall `SimpleHttpClient` EXISTS TO AVOID.</b>
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
    /// <b>A bus that drops silently needs a count</b>, or the loss is a fact nothing can
    /// see. Every failed post here is swallowed on purpose — a machine that is not
    /// there is C3 rather than an error — and that reasoning is right for a DEATH and
    /// wrong for a hiccup. The two are the same event to <see cref="PostAsync"/> and they
    /// are completely different to whoever was waiting, because a fleet gathers from a
    /// denominator: one lost answer and the gathering never completes, forever, on a
    /// machine that is alive and idle.
    /// </para>
    /// <para>
    /// <b>And this repo's own trap list is about exactly this shape.</b> A cost can be in
    /// memory while every instrument watches time; here a loss was on the wire while every
    /// instrument watched arrivals. What could be seen was how many answers came back, and
    /// what could not was whether an ask ever left.
    /// </para>
    /// </remarks>
    public long Dropped => Interlocked.Read(ref _dropped);

    /// <summary>Messages this machine took delivery of and then could not act on.</summary>
    /// <remarks>
    /// <b>The other end of the same silence</b>, and it is a different fault. A drop is
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

    /// <summary>Every holder this machine could ask, for the tests that ask.</summary>
    /// <remarks>
    /// <para>
    /// <b>A machine's picture of the world is PARTIAL</b> and that is not a fault — it
    /// knows the holders that have announced themselves to it, which is every one that was
    /// alive and reachable when it subscribed and no others.
    /// </para>
    /// <para>
    /// <b>And it is the denominator of a gathering</b>, which is why the partial picture has to
    /// be reportable. How many HOLDERS exist decides how much of a population a vote was
    /// taken over, and a run that quietly asked eleven of twelve would score like one that
    /// asked all twelve and be wrong about something nothing reports.
    /// </para>
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
    /// Tells every peer which askers and holders live here.
    /// </summary>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <para>
    /// <b>A peer that cannot be reached is not an error</b>, which is C3 at startup. A
    /// machine that is not up yet, or never will be, must not stop this one from running —
    /// so a failed announcement is dropped and the peer simply does not know about these
    /// holders until the next one.
    /// </para>
    /// <para>
    /// <b>And it is a fan-out like every other one here</b>, which it was not until something
    /// timed a fleet coming up. This awaited each peer in turn, so a machine opening paid
    /// the SUM of its peers rather than the slowest of them — the identical defect the class
    /// remark describes at length for a broadcast, in the method underneath it, surviving the
    /// commit that fixed the other two.
    /// </para>
    /// <para>
    /// <b>And what it costs is paid exactly where it hurts most.</b> A peer that is not there
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
                Askers = [.. _askers.Keys.Select(one => one.Value)],
                Holders = [.. _holders.Keys.Select(one => one.Value)],
            };
    }

    /// <summary>Takes in what another machine says it holds.</summary>
    /// <param name="roster">What arrived.</param>
    private void Absorb(Roster roster)
    {
        lock (_gate)
        {
            foreach (var one in roster.Askers) _answering[new MachineAddress(one)] = roster.Host;
            foreach (var one in roster.Holders) _holding[new MachineAddress(one)] = roster.Host;
        }
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IReceiveAsks holder)
    {
        ArgumentNullException.ThrowIfNull(holder);

        // And leaving is silent here, which is the opposite of a cluster. See `IBus`: an
        // ask that reaches nobody is an answer that never arrives, and the asker counts
        // that for itself. A death notice would only ever cover the polite departures.
        //
        // AND `Unreached` is not one, which is why this is still silent with fork 53 built.
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

    /// <summary>Asks one holder without waiting for it.</summary>
    /// <param name="who">Which holder.</param>
    /// <param name="here">It, if it lives on this machine.</param>
    /// <param name="there">Where it lives, if it does not.</param>
    /// <param name="ask">The question.</param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <b>The one delivery on this bus whose failure somebody is waiting on</b>, which is why
    /// it is the one not simply swallowed. Every other path here loses a message and
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
                    // A holder on this machine that threw while taking the question, which
                    // is `Refused` rather than `Dropped` and is the same event to whoever
                    // asked: no answer to this ask is coming from there.
                }

                // And it comes off the roster, which is the walk's `died` path reached by
                // observation rather than by announcement. Measured before this line
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
                // And it comes back by announcing, which is the only way in and is a push.
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
    /// <b>A fault on this path has nowhere to go</b>, and an unobserved task is worse than a
    /// swallowed one. The caller has already been handed its answer — who was asked, or
    /// who is about to be sent to — so there is no return value left to carry a failure on.
    /// What reaches here is a holder departing inside the window between being listed and
    /// being sent to, which is C3 happening rather than a routing bug.
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

        // The asker records its gathering inside this window, before anything is asked. A
        // local holder answers by direct call and can be back before this method returns,
        // and an answer to an ask nobody remembers is dropped.
        ready?.Invoke(everyone);

        // Every holder asked at once and none of them waited on, which is fork 56'S PRICE.
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
            _answering.TryGetValue(to, out there);
        }

        if (here is not null)
        {
            await here.DeliverAsync(answer, ct).ConfigureAwait(false);
            return;
        }

        // An answer with nowhere to go is dropped, exactly as a report is. The asker may
        // have died between asking and being answered, which C3 says is ordinary — and
        // throwing here would make one machine's departure another machine's error.
        if (there is not null)
            await PostAsync(there, $"answer/{Uri.EscapeDataString(to.Value)}", answer, ct)
                .ConfigureAwait(false);
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
            // A malformed or undeliverable message is dropped and not returned as an
            // error. C2 makes a lost message indistinguishable from a late one, and the
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

            // An announcement is answered by one, and without that the class's own claim
            // about late arrival was half true. A machine announces when it opens, and a
            // peer that is not up yet drops it -- so the machine that opened FIRST tells
            // nobody and is told by everybody. Its own routes work and nothing can route
            // back to it, which for the thinking path is a lost report and for this one is
            // every answer undeliverable.
            //
            // One reply and never a second, which is what the separate path buys. A reply
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

            // AND THERE IS NO `died` PATH, WHICH IS C3 reached by observation rather than by
            // announcement. The walk had one because a route in flight toward a departed
            // cluster is stranded and the origin cannot write it off without being told; an
            // ask is written off by the sender watching it fail to leave -- see `Fire` --
            // which needs nothing of the dying machine and so covers the impolite departure
            // the notice never could.
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
            // C3: A machine that is not there is normal and not an error. The design says
            // a holder vanishing mid-round is expected, so a refused connection is the
            // same event arriving by a faster road than a timeout.
            //
            // And it is counted now, because the sentence above is true of a death and
            // false of a hiccup. See `Dropped`.
            Interlocked.Increment(ref _dropped);

            return false;
        }
    }

    /// <summary>What a machine holds, as it tells its peers.</summary>
    /// <remarks>
    /// <b>Internal</b>, because it is a shape on the wire and not part of what a bus offers.
    /// </remarks>
    /// <remarks>
    /// <b>Strings rather than the address types</b>, because a roster is the one message whose
    /// contents are addresses — and an address is a record struct over a string, so
    /// naming it here would buy a wrapper and no safety.
    /// </remarks>
    internal sealed record Roster
    {
        /// <summary>Where the machine sending this can be reached.</summary>
        public required string Host { get; init; }

        /// <summary>The askers it holds, which answers can be sent back to.</summary>
        public required ImmutableArray<string> Askers { get; init; }

        /// <summary>
        /// The holders of commitments it has, which can be asked.
        /// </summary>
        /// <remarks>
        /// <b>Announced separately from <see cref="Askers"/></b>, because an ask is a
        /// broadcast and an answer is not. An answer goes to the one machine that asked, so the
        /// roster only has to say where that one is; an ask goes to EVERY holder, so the
        /// roster is what decides the denominator of a gathering. Folding them together
        /// would put every asker into the vote's population count, and each of them would
        /// then be a holder that never answers.
        /// </remarks>
        public ImmutableArray<string> Holders { get; init; } = [];
    }

    /// <summary>
    /// Shuts the door. <b>Twice is the same as once.</b>
    /// </summary>
    /// <remarks>
    /// <b>And it threw the second time</b> until something killed a machine on purpose.
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

        // Aborted rather than closed, and the difference was minutes a machine.
        //
        // `Close` is the polite shutdown: it waits for the request queue to drain, and
        // every peer holds a keep-alive connection to this listener that nothing on this
        // side can shut. A fleet coming down therefore paid a wait per machine that grew
        // with how many peers were pointed at it -- a run of six hundred rounds took under
        // a second and the teardown around it took minutes, which is what made a grid of
        // twelve fleets look exactly like a deadlock.
        //
        // And abort is the honest semantics here anyway, which is why this is not a
        // workaround. C3 says a machine vanishing mid-thought is normal; a phone going into
        // a tunnel does not drain its request queue first. What this discards is exactly
        // what a death discards.
        _door.Abort();
        _closing.Dispose();

        // And the clients are not disposed, because they cannot be. `ISimpleClient` has no
        // `Dispose`, so the line that used to sit here -- `(client as IDisposable)?.Dispose()`
        // -- read as cleanup and did nothing at all, which is this repo's oldest shape of
        // defect wearing a tidy face. One client per host is what keeps a fan-out from
        // paying a handshake per peer; what it costs is that a machine's connection pool
        // outlives the machine, and only a process ending collects it.
    }

    private bool _shut;
}
