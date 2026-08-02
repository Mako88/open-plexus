using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>
/// Snake's front end: one code per visible cell.
/// </summary>
/// <remarks>
/// <para>
/// A code carries the cell's offset from the head and its contents. <b>One-hot
/// over contents</b> — see <see cref="Cell"/> for why a hyperplane is wrong
/// here.
/// </para>
/// <para>
/// No seed is needed because nothing is fitted: the mapping from
/// (offset, contents) to a code is a fixed transform, which is legal precisely
/// because a constant is not a codebook. Every machine produces the same code
/// for the same cell, forever, with nothing to synchronise.
/// </para>
/// </remarks>
public sealed class SnakeQuantizer : IQuantizer<SnakeView>
{
    /// <inheritdoc/>
    public byte Modality => throw new NotImplementedException();

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Open question, deliberately not settled here:</b> whether an
    /// <see cref="Cell.Empty"/> cell emits a code at all. Emitting them makes
    /// empty space a first-class observation and costs a code per cell;
    /// withholding them makes the view sparse and makes "nothing there" mean
    /// nothing rather than something. Under onsets the cost is small either
    /// way, since a cell that stays empty is silent.
    /// </remarks>
    public IReadOnlyCollection<Code> Codify(SnakeView view) =>
        throw new NotImplementedException();
}
