using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>How wide the graded world is, and how close its readings sit to the line.</summary>
public sealed record GradedSettings
{
    /// <summary>How many address dimensions. The world is <c>Address + 2^Address</c> wide.</summary>
    public int Address { get; init; } = 2;

    /// <summary>
    /// How tightly readings crowd the decision line, in 0..1.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>And high is the easy end, which is the opposite of what it was written
    /// down as.</b> Crowded against the line, every reading falls in one of the two
    /// bands either side of it, so a banded front end emits what is almost a bit and
    /// the interface costs nearly nothing.
    /// </para>
    /// <para>
    /// <b>Spread is the hard end</b>, and it is the one the plan cares about. At
    /// zero a reading is anywhere in its half, so one dimension speaks in many codes
    /// and the same rule has to be learnt again in each — which is the fragmentation
    /// a symbol's lack of partial credit causes, and it is what population coding is
    /// supposed to answer.
    /// </para>
    /// </remarks>
    public double Crowding { get; init; } = 0.9;
}

/// <summary>One round of the graded world.</summary>
public readonly record struct Sample
{
    /// <summary>One real number per dimension, in 0..1.</summary>
    public required ImmutableArray<double> Reading { get; init; }

    /// <summary>What the multiplexer says about the dimensions read as bits.</summary>
    public required Code Outcome { get; init; }
}

/// <summary>
/// The multiplexer with its bits turned back into readings.
/// </summary>
/// <remarks>
/// <para>
/// <b>The world the project's actual bet needs, and the one `csharp` NEVER HAD.</b>
/// Its claim is that this family of systems died at the interface with perception and
/// that a substrate manufacturing its own symbols repairs it. Every measurement so far
/// has been on cues that were ALREADY symbols, so it has said nothing about that at
/// all. Here a front end has to make the symbols, and which front end is the arm.
/// </para>
/// <para>
/// <b>It is the same function on purpose.</b> Address dimensions select which data
/// dimension carries the outcome, exactly as before — so what changes between this and
/// the multiplexer is the interface and nothing else, and a difference in the score is
/// attributable rather than interesting.
/// </para>
/// <para>
/// <b>And spread is what makes it hard, not crowding.</b> A dimension whose readings
/// range over its whole half speaks in many codes, so the same rule has to be learnt
/// again in each of them — the fragmentation a symbol's lack of partial credit causes.
/// Readings crowded against the line collapse into two bands and are nearly bits
/// already, which is the EASY end and was written down here as the hard one.
/// </para>
/// </remarks>
public sealed class Graded : IWorld<IReadOnlyList<double>>
{
    /// <inheritdoc/>
    public int Outcomes => 2;

    /// <summary>The same round, in the shape every world shares.</summary>
    Turn<IReadOnlyList<double>> IWorld<IReadOnlyList<double>>.Next()
    {
        var shown = Next();

        return new Turn<IReadOnlyList<double>>
        {
            Seen = shown.Reading,
            Outcome = (int)shown.Outcome.Value,
        };
    }

    private readonly GradedSettings _settings;
    private readonly Random _rng;

    /// <param name="settings">The shape of the world.</param>
    /// <param name="seed">The world's own generator.</param>
    public Graded(GradedSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Address, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Address, 4);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Crowding);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Crowding, 1.0);

        _settings = settings;
        _rng = new Random(seed);
    }

    /// <summary>How many data dimensions there are.</summary>
    public int Data => 1 << _settings.Address;

    /// <summary>How many dimensions are read in one round.</summary>
    public int Width => _settings.Address + Data;

    /// <summary>What a blind guess scores.</summary>
    public static double Chance => 0.5;

    /// <summary>One round of the world.</summary>
    public Sample Next()
    {
        var bits = new int[Width];
        var reading = new double[Width];

        for (var which = 0; which < Width; which++)
        {
            bits[which] = _rng.Next(2);

            // How far from the line, and never which side. Crowding moves a reading
            // toward the decision boundary without ever crossing it, so the FUNCTION
            // is untouched and only the CODING changes. A world where this changed
            // the answer would be measuring difficulty and calling it interface risk.
            var room = 0.5 * (1.0 - _settings.Crowding);
            var from = _rng.NextDouble() * room;

            reading[which] = bits[which] == 1 ? 0.5 + from + 1e-9 : 0.5 - from - 1e-9;
        }

        var address = 0;
        for (var which = 0; which < _settings.Address; which++)
            address = (address << 1) | bits[which];

        return new Sample
        {
            Reading = [.. reading],
            Outcome = Multiplexer.Says(bits[_settings.Address + address]),
        };
    }
}
