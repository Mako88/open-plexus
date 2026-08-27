using OpenPlexus.Codes;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// A source whose answers are a COIN — <b>the world nothing can learn.</b>
/// </summary>
/// <param name="inner">The world whose moments are passed through untouched.</param>
/// <param name="outcomes">How many answers it draws between.</param>
/// <param name="seed">The draw.</param>
/// <remarks>
/// <b>The control the curve needs to mean anything.</b> An instrument that reads flat on a
/// world nobody could learn and flat on a world the machine failed to learn says nothing
/// about either; what makes it a measurement is that the two are different worlds and one
/// of them is known. The moments are the inner world's, so the only thing that changed is
/// whether the answer can be predicted at all.
/// </remarks>
internal sealed class Coined(IInput inner, int outcomes, int seed) : IInput
{
    private readonly Random _draws = new(seed);

    /// <inheritdoc/>
    public byte Source => inner.Source;

    /// <inheritdoc/>
    public int Outcomes => outcomes;

    /// <inheritdoc/>
    public Pushed? Push() =>
        inner.Push() is not { } pushed
            ? null
            : pushed with { Followed = Brain.Says(_draws.Next(outcomes)) };
}
