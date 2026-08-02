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
/// anything</i>. Snake will be the first time one does.
/// </para>
/// <para>
/// The agreed shape is three steps — arrival narrows, prediction ranks, brevity
/// breaks ties — and only the first is built here. Prediction ranking needs a
/// predictor that does not exist yet, and inventing an internal score in its
/// place is exactly the move this design refuses.
/// </para>
/// </remarks>
public sealed class OutputMachine
{
    private readonly MachineAddress _address;

    /// <summary>The codes that mean an action. For snake, four.</summary>
    private readonly IReadOnlyCollection<Code> _codes = [];

    public OutputMachine(MachineAddress address, IReadOnlyCollection<Code> codes) =>
        throw new NotImplementedException();

    /// <inheritdoc cref="_address"/>
    public MachineAddress Address => throw new NotImplementedException();

    /// <summary>
    /// <b>Arrival narrows, then rank.</b> The candidates are exactly the chains
    /// that reached one of this machine's codes — selection is routing, not a
    /// separate mechanism — and among those the best-scoring wins for now.
    /// </summary>
    /// <returns>
    /// The chosen action code, or null when nothing reached this machine at
    /// all. <b>Null is a real answer</b>: the only honest one for a situation
    /// nothing was ever written about.
    /// </returns>
    public Code? Choose(Thought thought) => throw new NotImplementedException();
}
