using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Machines;

/// <summary>Which translation turns a reading into codes.</summary>
/// <remarks>
/// <b>A fact about the pipe, which is neither side's to decide.</b> Putting this
/// inside a world would let a world choose what the brain perceives; putting it inside
/// the brain would make the brain know about worlds. It lives at the join.
/// </remarks>
public enum Fronting
{
    /// <summary>One code per band, per dimension, per grain.</summary>
    Banded,

    /// <summary>A sparse set of winners over a random projection of every dimension.</summary>
    Winnowed,
}

/// <summary>The graded world, learnt through a front end that has to make the symbols.</summary>
/// <remarks>
/// <b>No soundness here, and that is the price of the interface.</b> A scope over
/// banded or winnowed codes does not pin anything, so the exact enumeration that made
/// the multiplexer's score basis-independent has nothing to enumerate over.
/// </remarks>
public sealed class GradedRun
{
    /// <summary>How finely the banded arm cuts a dimension.</summary>
    public const int Bands = 8;

    /// <summary>How many coarser retellings the banded arm adds.</summary>
    public const int Grains = 2;

    private readonly Trial<IReadOnlyList<double>> _trial;

    /// <param name="world">The shape of the world.</param>
    /// <param name="brain">The one brain, already configured.</param>
    /// <param name="fronting">Which translation makes the symbols.</param>
    /// <param name="seed">The world's own generator.</param>
    public GradedRun(GradedSettings world, Brain brain, Fronting fronting, int seed)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(brain);

        var made = new Graded(world, seed);

        IQuantizer<IReadOnlyList<double>> sensing = fronting == Fronting.Winnowed
            ? new Winnowing(Multiplexer.Bit, made.Width)
            : new Banded<IReadOnlyList<double>>(
                reading => reading, Multiplexer.Bit, made.Width, Bands, Grains);

        _trial = new Trial<IReadOnlyList<double>>(made, sensing, brain);
    }

    /// <summary>Runs the world and learns from it.</summary>
    /// <param name="rounds">How many rounds.</param>
    /// <param name="sweep">How often to subsume, abstract and cull.</param>
    /// <param name="target">The trailing accuracy to wait for.</param>
    /// <param name="window">How many answered predictions that accuracy is over.</param>
    public Tally Run(long rounds, int sweep = 1000, double target = 0.9, int window = 2000) =>
        _trial.Run(rounds, sweep, target, window);
}
