using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Learning;
using OpenPlexus.Thinking;

namespace OpenPlexus.Machines;

/// <summary>
/// The world boundary on the way in.
/// </summary>
/// <remarks>
/// <b>Holds an address, holds no edges, is in no walk.</b> That is why an
/// arbitrary sensor can be attached without the graph knowing what it is, and
/// it is what keeps modality entirely outside the graph — so partitioning by
/// modality stays a deployment choice rather than a rewrite.
/// </remarks>
/// <typeparam name="TFrame">What this machine's sensor produces.</typeparam>
public sealed class InputMachine<TFrame>
{
    private readonly MachineAddress _address;
    private readonly IQuantizer<TFrame> _quantizer;
    private readonly LiveSet _liveSet = new();
    private readonly IRendezvous _rendezvous;
    private readonly IBus _bus;
    private readonly Ring _ring;

    /// <summary>Thoughts this machine started and has not released.</summary>
    private readonly Dictionary<BroadcastId, Thought> _thoughts = [];

    public InputMachine(
        MachineAddress address,
        IQuantizer<TFrame> quantizer,
        IRendezvous rendezvous,
        IBus bus,
        Ring ring) => throw new NotImplementedException();

    /// <summary>
    /// The whole input path, in one place.
    /// </summary>
    /// <remarks>
    /// Quantise the frame; diff against the live set for onsets and offsets;
    /// <b>learn</b> by joining each onset with what was already live;
    /// <b>think</b> by starting a thought from the onsets. Persistence produces
    /// neither — a stable scene is silent.
    /// </remarks>
    public Task<Thought?> ObserveAsync(TFrame frame, long now, CancellationToken ct = default) =>
        throw new NotImplementedException();

    /// <summary>
    /// Opens a thought, mints a broadcast id, and sends one message per code to
    /// its owning cluster.
    /// </summary>
    public Task<Thought> ThinkAsync(IReadOnlyCollection<Code> origins, CancellationToken ct = default) =>
        throw new NotImplementedException();

    /// <summary>An arrival came back for one of this machine's thoughts.</summary>
    public void Receive(BroadcastId broadcast, Arrival arrival) =>
        throw new NotImplementedException();

    /// <summary>A node's termination report came back.</summary>
    public void Receive(Accounting accounting) => throw new NotImplementedException();

    /// <summary>
    /// A machine left. Release any thought that had routes through it.
    /// </summary>
    private void OnDeath(MachineAddress gone) => throw new NotImplementedException();
}
