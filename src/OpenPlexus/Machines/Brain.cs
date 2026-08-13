using OpenPlexus.Codes;
using OpenPlexus.Commitments;

namespace OpenPlexus.Machines;

/// <summary>
/// The one thing being tuned, and the only place a brain-side number lives.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's rule: one brain across every world.</b> On `csharp` the dials that
/// decided how thinking worked were passed to a world's runner, and several had
/// different defaults in different worlds — so switching world switched brain, and
/// every comparison between two problems was also a comparison between two machines.
/// </para>
/// <para>
/// <b>SO <see cref="CommittingSettings"/> is constructed once, outside any world, and
/// handed in.</b> A world may turn its own dials as hard as it likes and cannot reach
/// one of these; `SeparationTests` fails the build if it tries.
/// </para>
/// </remarks>
public sealed class Brain
{
    /// <summary>The modality every world's outcome is said in.</summary>
    /// <remarks>
    /// <b>Shared across worlds on purpose.</b> A brain that learnt a different
    /// alphabet per world would not be one brain — and a commitment about an outcome
    /// would mean something different depending on who was asking.
    /// </remarks>
    public const byte Followed = 101;

    /// <param name="dials">Every number the brain is allowed to have.</param>
    /// <param name="seed">The control arm's generator.</param>
    public Brain(CommittingSettings dials, int seed)
    {
        ArgumentNullException.ThrowIfNull(dials);

        Dials = dials;
        Held = new Population(dials, seed);
    }

    /// <summary>Every number the brain is allowed to have.</summary>
    public CommittingSettings Dials { get; }

    /// <summary>What it holds.</summary>
    public Population Held { get; }

    /// <summary>The code for one outcome.</summary>
    /// <param name="outcome">Which outcome, as a small whole number.</param>
    public static Code Says(int outcome)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(outcome);
        return new Code(Followed, (ulong)outcome);
    }
}
