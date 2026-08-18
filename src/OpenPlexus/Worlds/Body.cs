using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>Every sense pushing at one brain, composed.</summary>
/// <remarks>
/// <para>
/// <b>A world is a set of inputs and this is the set.</b> The north star is a camera, audio,
/// temperature and motion arriving at one brain, and that is one of these with four
/// <see cref="IInput"/>s in it. A bench reading a corpus is the same type with one, which is
/// what keeps the numbers taken here comparable with the ones taken on a body.
/// </para>
/// <para>
/// <b>Named for the body rather than for the world</b>, and the reason is that
/// <c>IWorld</c> already means a problem here. Two ideas sharing one name has bitten this
/// repo before — `Choosing` read as measured on two worlds because an unrelated type had a
/// property spelt the same — so the composition takes the north star's own word for the
/// thing that carries the senses.
/// </para>
/// <para>
/// <b>The order senses are polled in is fixed and rotates.</b> A fixed seed reproduces a run
/// exactly and that is a property nothing here may cost, so the schedule is a cursor rather
/// than a thread apiece. What a genuinely concurrent body does differently is a question
/// about a deployment, and it is asked on a deployment.
/// </para>
/// </remarks>
public sealed class Body
{
    /// <summary>The source a body's first sense pushes on.</summary>
    /// <remarks>
    /// <b>One rather than nought, so an unset source is not a valid one.</b> A default
    /// <see langword="byte"/> is nought, and a sense that forgot to say which stream it was
    /// would then silently share one with whichever other sense did the same.
    /// </remarks>
    public const byte First = 1;

    private readonly IReadOnlyList<IInput> _senses;

    private int _at;

    /// <param name="senses">Every source pushing at the brain.</param>
    public Body(params IInput[] senses)
    {
        ArgumentNullException.ThrowIfNull(senses);

        if (senses.Length == 0)
            throw new ArgumentException("a body with no senses pushes nothing", nameof(senses));

        var sources = senses.Select(one => one.Source).ToHashSet();

        if (sources.Count != senses.Length)
            throw new ArgumentException(
                "two senses share a source, so each would be settling the other's moments "
                + "-- give every input its own", nameof(senses));

        _senses = senses;
    }

    /// <summary>What is pushing.</summary>
    public IReadOnlyList<IInput> Senses => _senses;

    /// <summary>What a blind guess is against.</summary>
    /// <remarks>
    /// <b>The widest alphabet any sense can settle in</b>, because a chance bar has to be
    /// the one a population could not beat by guessing. A body whose senses settle in
    /// different alphabets has no single bar and this is the conservative one.
    /// </remarks>
    public int Outcomes => _senses.Max(one => one.Outcomes);

    /// <summary>The next moment from whichever sense has one.</summary>
    /// <remarks>
    /// <b>Every sense is asked once before this gives up</b>, so a body whose only busy
    /// sense sits last in the list still pushes. Returning nothing means no sense had
    /// anything, which is a quiet body rather than a finished one.
    /// </remarks>
    public Pushed? Push()
    {
        for (var tried = 0; tried < _senses.Count; tried++)
        {
            var sense = _senses[_at];

            _at = (_at + 1) % _senses.Count;

            if (sense.Push() is { } moment) return moment;
        }

        return null;
    }
}
