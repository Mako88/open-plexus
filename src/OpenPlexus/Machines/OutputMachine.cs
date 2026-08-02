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
public sealed class OutputMachine
{
    private readonly MachineAddress _address;

    /// <summary>The codes that mean an action. For snake, four.</summary>
    private readonly HashSet<Code> _codes;

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
}
