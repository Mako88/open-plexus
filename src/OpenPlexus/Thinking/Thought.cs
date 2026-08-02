using OpenPlexus.Codes;
using OpenPlexus.Graph;

namespace OpenPlexus.Thinking;

/// <summary>
/// One broadcast, on the machine that started it.
/// </summary>
/// <remarks>
/// <b>Readable at any time.</b> Under continuous input there is no moment
/// between thoughts, so the system acts on the best chain arrived so far and
/// later arrivals refine it. Nothing waits for completion.
/// </remarks>
public sealed class Thought
{
    private readonly BroadcastId _id;

    /// <summary>Endpoint code to what reached it.</summary>
    private readonly Dictionary<Code, Arrival> _arrivals = [];

    /// <summary>
    /// How evidence from several routes reaching one endpoint is combined.
    /// </summary>
    private readonly Accumulate _accumulate;

    private readonly int _origins;

    /// <summary>
    /// The accounting. <c>origins + splits - deaths == live</c> holds exactly
    /// in one process and does not across a network, which is why it is
    /// asserted rather than trusted.
    /// </summary>
    private int _live, _splits, _deaths, _halted;

    private bool _released;

    /// <summary>
    /// Guards everything below. Reports arrive from several clusters at once,
    /// on whatever thread the bus dispatched them to.
    /// </summary>
    private readonly Lock _gate = new();

    /// <param name="id">Which thought this is. Reports carrying another id are refused.</param>
    /// <param name="origins">
    /// How many codes the broadcast started from. Every origin is one live
    /// route before anything has been heard back.
    /// </param>
    /// <param name="accumulate">How several routes reaching one endpoint combine.</param>
    public Thought(BroadcastId id, int origins, Accumulate accumulate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(origins);

        _id = id;
        _origins = origins;
        _accumulate = accumulate;
        _live = origins;
    }

    public BroadcastId Id => _id;

    /// <summary>Routes still travelling by this thought's own accounting.</summary>
    public int Live
    {
        get { lock (_gate) return _live; }
    }

    /// <summary>How many routes forked into more than one.</summary>
    public int Splits
    {
        get { lock (_gate) return _splits; }
    }

    /// <summary>How many routes ended without reaching anywhere new.</summary>
    public int Deaths
    {
        get { lock (_gate) return _deaths; }
    }

    /// <summary>
    /// Routes killed by the horizon rather than by economics.
    /// </summary>
    /// <remarks>
    /// <b>A walk that hit the horizon looks exactly like one that finished
    /// unless this is reported.</b>
    /// </remarks>
    public int Halted
    {
        get { lock (_gate) return _halted; }
    }

    /// <summary>How many distinct endpoints have been reached.</summary>
    public int Endpoints
    {
        get { lock (_gate) return _arrivals.Count; }
    }

    /// <summary>
    /// Whether the state has been dropped. A released thought accepts nothing
    /// further and answers with nothing.
    /// </summary>
    public bool Released
    {
        get { lock (_gate) return _released; }
    }

    /// <summary>
    /// Accumulates one arrival.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Keeps the <b>strongest single</b> chain as the explanation. A summed
    /// score is no route's strength, and keeping the last arrival would make
    /// the explanation whichever branch happened to finish last.
    /// </para>
    /// <para>
    /// <b>An arrival for a released thought is dropped rather than refused.</b>
    /// C2 says late is normal, and there is nothing left for it to refine.
    /// </para>
    /// </remarks>
    public void Receive(Arrival arrival)
    {
        ArgumentNullException.ThrowIfNull(arrival);

        lock (_gate)
        {
        if (_released) return;

        if (!_arrivals.TryGetValue(arrival.Endpoint, out var standing))
        {
            _arrivals[arrival.Endpoint] = arrival;
            return;
        }

        var stronger = arrival.Best > standing.Best;

        _arrivals[arrival.Endpoint] = new Arrival
        {
            Endpoint = arrival.Endpoint,

            // Many weak agreeing routes should outrank one strong route, which
            // is the claim `Sum` makes and `Max` does not — 0.1234 against
            // 0.0834 on the typed walk.
            Score = _accumulate == Accumulate.Max
                ? Math.Max(standing.Score, arrival.Score)
                : standing.Score + arrival.Score,

            Chain = stronger ? arrival.Chain : standing.Chain,
            Best = stronger ? arrival.Best : standing.Best,
            Routes = standing.Routes + arrival.Routes,
        };
        }
    }

    /// <summary>
    /// Folds in one node's termination report.
    /// </summary>
    /// <remarks>
    /// Dijkstra-Scholten's shape: a route splitting into <c>k</c> reports
    /// <c>k-1</c>, a route dying reports one death, and the thought is over
    /// when the live count reaches zero. <b>Refused if it belongs to another
    /// broadcast</b> — mixing two thoughts' death counts is exactly what the
    /// broadcast id exists to prevent.
    /// </remarks>
    public void Receive(Accounting accounting)
    {
        if (accounting.Broadcast != _id)
            throw new ArgumentException(
                $"accounting for {accounting.Broadcast} reached the thought for {_id}",
                nameof(accounting));

        lock (_gate)
        {
            if (_released) return;

            _splits += accounting.Splits;
            _deaths += accounting.Deaths;
            _halted += accounting.Halted;
            _live += accounting.Splits - accounting.Deaths;
        }
    }

    /// <summary>
    /// The top arrivals right now.
    /// </summary>
    /// <remarks>
    /// <b>Readable mid-flight</b>, which is what continuous operation requires:
    /// the system acts on what has arrived so far and later arrivals refine it.
    /// <para>
    /// Ties break on the shorter chain, then on the endpoint so the order is
    /// deterministic. That is the agreed brevity rule and it costs nothing
    /// here, but <b>it only ever fires on an exact score tie</b>, so it is not
    /// an implementation of brevity as a ranking principle.
    /// </para>
    /// </remarks>
    public IReadOnlyList<Arrival> Best(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        lock (_gate) return [.. Ranked().Take(count)];
    }

    /// <summary>
    /// The top arrivals whose endpoint is one of these codes.
    /// </summary>
    /// <remarks>
    /// <b>This is "arrival narrows" — selection is routing.</b> The candidates
    /// are exactly the chains that reached the asking machine's own codes, so
    /// nothing extra decides what is eligible. Ranking among them is a separate
    /// question and currently only the score.
    /// <para>
    /// Not <c>Best(n)</c> then filter: the top n overall can contain none of
    /// these codes while a chain that did reach one sits just below the cut.
    /// </para>
    /// </remarks>
    public IReadOnlyList<Arrival> BestAmong(IReadOnlyCollection<Code> codes, int count)
    {
        ArgumentNullException.ThrowIfNull(codes);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var wanted = codes as HashSet<Code> ?? [.. codes];
        lock (_gate) return [.. Ranked().Where(a => wanted.Contains(a.Endpoint)).Take(count)];
    }

    private IEnumerable<Arrival> Ranked() =>
        _arrivals.Values
            .OrderByDescending(a => a.Score)
            .ThenBy(a => a.Chain.Length)
            .ThenBy(a => a.Endpoint);

    /// <summary>
    /// Whether the accounting adds up. Asserted, never assumed.
    /// </summary>
    /// <remarks>
    /// <b>In one process this catches a slip, not a network fault.</b> The live
    /// count is maintained by the same call that moves splits and deaths, so
    /// this holds by construction unless the two paths diverge. Across a
    /// network it cannot hold at all — C2 loses reports, and a lost death would
    /// leave the count above zero forever. That is why nothing waits on it.
    /// </remarks>
    public bool Balanced()
    {
        lock (_gate) return _origins + _splits - _deaths == _live;
    }

    /// <summary>
    /// Whether every route has returned or died by the thought's own
    /// accounting. <b>Not a deadline</b> — a deadline is a constant nobody
    /// measured, and the death event is what makes one unnecessary.
    /// </summary>
    public bool Settled
    {
        get { lock (_gate) return _live == 0; }
    }

    /// <summary>
    /// Drop the state. Called on settle, or on a death event for a machine this
    /// thought had routes through.
    /// </summary>
    /// <remarks>
    /// <b>Termination is housekeeping now, not correctness.</b> A thought
    /// stranded by a vanished machine leaks state instead of hanging the
    /// system, because nothing was waiting on it to finish.
    /// </remarks>
    public void Release()
    {
        lock (_gate)
        {
            _arrivals.Clear();
            _released = true;
        }
    }
}
