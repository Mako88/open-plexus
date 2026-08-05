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

    /// <remarks>
    /// <b>AN EMPTY CELL EMITS A CODE, AND THERE IS NO ARM THAT DOES NOT — John's
    /// call, 2026-08-04.</b> Withholding them was worth four orders of magnitude
    /// when `Best` pricing let the flood enumerate every simple path; inverse cost
    /// bounds the walk by construction, so that reason is gone. What withholding
    /// costs instead was audited: with only the body visible in open space the
    /// codes are often identical frame after frame and <b>47% of steps produce no
    /// onset at all</b> — measured over 60 seeds, where a single-seed reading said
    /// 89% and was not a measurement. Including them takes silence to 16%, at ten
    /// times the messages.
    /// </remarks>
    /// <inheritdoc/>
    public byte Modality => Vision;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(SnakeView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        var codes = new List<Code>(view.Cells.Count);
        foreach (var cell in view.Cells)
        {
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
