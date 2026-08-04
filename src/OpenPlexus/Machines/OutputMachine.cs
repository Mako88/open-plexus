using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Thinking;

namespace OpenPlexus.Machines;

/// <summary>
/// The world boundary on the way out.
/// </summary>
/// <remarks>
/// <para>
/// <b>THIS IS THE LEAST DESIGNED PART OF THE SYSTEM.</b> On <c>master</c>,
/// nothing in the whole package turns a chain into an output: the walk hands
/// back what it reached and stops, every action in every experiment is a
/// hand-written policy or a random choice, and <i>no chain has ever caused
/// anything</i>.
/// </para>
/// <para>
/// The agreed shape is three steps — arrival narrows, prediction ranks, brevity
/// breaks ties — and <b>only the first is built.</b> Prediction ranking needs a
/// predictor that does not exist yet, and inventing an internal score in its
/// place is exactly the move this design refuses.
/// </para>
/// </remarks>
public sealed class OutputMachine : IReceiveArrivals
{
    private readonly MachineAddress _address;

    /// <summary>The codes that mean an action. For snake, four.</summary>
    private readonly HashSet<Code> _codes;

    /// <summary>
    /// What has been published to this machine and not yet taken.
    /// </summary>
    /// <remarks>
    /// <b>Keyed by broadcast, because several thoughts are in flight at once</b>
    /// — that is what <see cref="BroadcastId"/> is for, and it is also what makes
    /// concurrent output expressible rather than merely unwritten.
    /// </remarks>
    private readonly Dictionary<BroadcastId, Settled> _heard = [];

    private readonly Lock _gate = new();

    public OutputMachine(MachineAddress address, IReadOnlyCollection<Code> codes)
    {
        ArgumentNullException.ThrowIfNull(codes);
        ArgumentOutOfRangeException.ThrowIfZero(codes.Count);

        _address = address;
        _codes = [.. codes];
    }

    public MachineAddress Address => _address;

    /// <summary>The codes this machine can be asked for.</summary>
    public IReadOnlyCollection<Code> Codes => _codes;

    /// <summary>
    /// <b>Arrival narrows, then rank.</b>
    /// </summary>
    /// <remarks>
    /// The candidates are exactly the chains that reached one of this machine's
    /// codes — <b>selection is routing</b>, not a separate mechanism — and
    /// among those the best-scoring wins for now.
    /// </remarks>
    /// <returns>
    /// The chosen action code, or null when nothing reached this machine at
    /// all. <b>Null is a real answer</b>: the only honest one for a situation
    /// nothing was ever written about, and the caller has to decide what to do
    /// with a system that has nothing to say.
    /// </returns>
    public Code? Choose(Thought thought)
    {
        ArgumentNullException.ThrowIfNull(thought);

        var reached = thought.BestAmong(_codes, 1);
        return reached.Count == 0 ? null : reached[0].Endpoint;
    }

    /// <summary>
    /// The winning chain as well as the code, for anything that wants to see
    /// the reasoning rather than just the answer.
    /// </summary>
    public Arrival? Explain(Thought thought)
    {
        ArgumentNullException.ThrowIfNull(thought);

        var reached = thought.BestAmong(_codes, 1);
        return reached.Count == 0 ? null : reached[0];
    }

    // ---- fork 11: the same decision, reached without holding the thought ----

    /// <inheritdoc/>
    /// <remarks>
    /// <b>ALREADY SETTLED WHEN IT GETS HERE.</b> This machine does no settle
    /// arithmetic of its own — it cannot, without either a second copy of the
    /// loop or reading a walk that has not finished, which is fork 22's trap. The
    /// machine that owned the thought knew, and said.
    /// </remarks>
    public Task DeliverAsync(Settled settled, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settled);
        ct.ThrowIfCancellationRequested();

        lock (_gate) _heard[settled.Broadcast] = settled;

        return Task.CompletedTask;
    }

    /// <summary>
    /// The chosen action for a thought that was published to this machine, and
    /// <b>forgetting it in the same breath.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The same rule as <see cref="Choose(Thought)"/> — arrival narrows, then
    /// rank</b> — over arrivals that arrived by address instead of by direct
    /// call. Nothing about the decision changes; what changes is that this
    /// machine no longer has to be handed the asker's thought, which is what
    /// makes a second one possible.
    /// </para>
    /// <para>
    /// <b>Taken once.</b> A published thought is a finished result, not a
    /// standing fact, and leaving it in would let one broadcast drive an action
    /// twice and grow the map forever.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The chosen code, or null when nothing was published for that broadcast or
    /// nothing in it reached this machine. <b>Null is a real answer.</b>
    /// </returns>
    public Code? Take(BroadcastId broadcast)
    {
        Settled? settled;
        lock (_gate)
        {
            if (!_heard.Remove(broadcast, out settled)) return null;
        }

        var best = settled.Arrivals
            .Where(arrival => _codes.Contains(arrival.Endpoint))
            .OrderByDescending(arrival => arrival.Score)
            .ToList();

        return best.Count == 0 ? null : best[0].Endpoint;
    }

    /// <summary>How many published thoughts are waiting to be taken.</summary>
    /// <remarks>
    /// <b>Read it.</b> A machine nobody takes from grows without bound, and a
    /// count that only ever climbs is how that becomes visible instead of
    /// becoming a leak.
    /// </remarks>
    public int Waiting
    {
        get { lock (_gate) return _heard.Count; }
    }
}
