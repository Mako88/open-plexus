using System.Collections.Immutable;

namespace OpenPlexus.Codes;

/// <summary>
/// Several front ends reading one frame — <b>the shape a body has, and the
/// reason a sensor is not a machine.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A sensor per machine would not do this</b>, and that is the whole point. A
/// front end is asked for a MOMENT — see <see cref="Machines.Watching{TSeen}"/> — and
/// a moment is what puts codes together, so a camera on one machine and a
/// microphone on another would never co-fire, and the sight–sound pairing that
/// <see cref="Worlds.Senses"/> exists to measure could not be reached at all.
/// <b>Cross-modal binding needs the streams in ONE moment</b>, which means one
/// machine holding several front ends rather than several machines holding one
/// each.
/// </para>
/// <para>
/// <b>Each reads the whole frame and takes the part it knows</b>, which is why
/// no splitter is needed: selecting a stream is a front end reading its own
/// field. A separate router would only earn its keep if ONE stream carried
/// several modalities that had to be torn apart, and nothing here does that yet.
/// </para>
/// <para>
/// <b>Groups are offset per front end.</b> Because a group number only means
/// something inside the thing that said it. Two front ends both calling
/// something "object 0" mean different objects, and merging them unchanged would
/// assert the binding <see cref="Worlds.Binding"/> exists to withhold — the same
/// fault a chunk spanning two groups makes, from the other end.
/// </para>
/// </remarks>
/// <typeparam name="TFrame">What the body reads, all streams together.</typeparam>
public sealed class Compound<TFrame> : IQuantizer<TFrame>
{
    private readonly ImmutableArray<IQuantizer<TFrame>> _senses;

    /// <param name="senses">
    /// The front ends, in a fixed order. <b>The order is part of the
    /// definition</b> — group offsets are assigned by position, so shuffling them
    /// renumbers every group this body has ever reported.
    /// </param>
    public Compound(IEnumerable<IQuantizer<TFrame>> senses)
    {
        ArgumentNullException.ThrowIfNull(senses);

        _senses = [.. senses];

        if (_senses.IsEmpty)
            throw new ArgumentException("a body with no senses reads nothing", nameof(senses));
    }

    /// <summary>
    /// <b>The first front end's, and nothing reads it.</b>
    /// </summary>
    /// <remarks>
    /// A compound has no single modality by construction — that is what it is for.
    /// Anything that needs to know reads <see cref="Code.Modality"/>, which the codes
    /// carry themselves.
    /// </remarks>
    public byte Modality => _senses[0].Modality;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(TFrame observation)
    {
        var codes = ImmutableArray.CreateBuilder<Code>();

        foreach (var sense in _senses) codes.AddRange(sense.Codify(observation));

        return codes.ToImmutable();
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<Code, int>? Bind(TFrame observation)
    {
        Dictionary<Code, int>? groups = null;
        var offset = 0;

        foreach (var sense in _senses)
        {
            var mine = sense.Bind(observation);
            if (mine is null || mine.Count == 0) continue;

            groups ??= [];

            var highest = offset;

            foreach (var (code, group) in mine)
            {
                groups[code] = offset + group;
                highest = Math.Max(highest, offset + group);
            }

            offset = highest + 1;
        }

        return groups;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Not offset, because an order is a claim about one clock.</b> Two front
    /// ends numbering from zero each say "mine came first", which is a claim about
    /// the frame and not about each other — so a body whose streams must be
    /// ordered TOGETHER has to say so on one scale. Offsetting would invent a
    /// sequencing nobody stated.
    /// </remarks>
    public IReadOnlyDictionary<Code, int>? Order(TFrame observation)
    {
        Dictionary<Code, int>? sequence = null;

        foreach (var sense in _senses)
        {
            var mine = sense.Order(observation);
            if (mine is null || mine.Count == 0) continue;

            sequence ??= [];
            foreach (var (code, at) in mine) sequence[code] = at;
        }

        return sequence;
    }

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Fleeting(TFrame observation)
    {
        HashSet<Code>? passing = null;

        foreach (var sense in _senses)
        {
            var mine = sense.Fleeting(observation);
            if (mine is null || mine.Count == 0) continue;

            passing ??= [];
            passing.UnionWith(mine);
        }

        return passing;
    }
}
