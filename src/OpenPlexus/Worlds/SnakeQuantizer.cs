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
/// because a constant is not a codebook. <b>Every machine produces the same
/// code for the same cell, forever, with nothing to synchronise</b> — the
/// red-ball property, which is the whole reason a quantiser is not trained.
/// </para>
/// </remarks>
public sealed class SnakeQuantizer : IQuantizer<SnakeView>
{
    /// <summary>Vision. Two modalities never collide.</summary>
    public const byte Vision = 1;

    /// <inheritdoc/>
    public byte Modality => Vision;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(SnakeView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        var codes = new List<Code>(view.Cells.Count);
        foreach (var cell in view.Cells)
        {
            // EMPTY CELLS EMIT NOTHING. An occasion is a clique, so the number
            // of codes per frame sets how dense the graph is -- measured at
            // 46,536 routes halted with them against 6 without.
            if (cell.Content == Cell.Empty) continue;
            codes.Add(Encode(cell));
        }

        return codes;
    }

    /// <summary>
    /// Packs an offset and a content into one value.
    /// </summary>
    /// <remarks>
    /// The offsets occupy separate bit ranges from each other and from the
    /// content, so no two distinct cells can collide — <b>a collision here
    /// would make two different situations one observation</b>, which is the
    /// opposite of the property centring exists to give.
    /// </remarks>
    internal static Code Encode(Seen cell)
    {
        var dx = (ulong)(ushort)(short)cell.Dx;
        var dy = (ulong)(ushort)(short)cell.Dy;
        return new Code(Vision, (dx << 24) | (dy << 8) | (byte)cell.Content);
    }
}
