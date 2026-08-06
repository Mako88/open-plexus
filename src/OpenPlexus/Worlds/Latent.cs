using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>How many hidden causes there are, and how many channels report them.</summary>
public sealed record LatentSettings
{
    /// <summary>
    /// How many states the hidden thing can be in.
    /// </summary>
    /// <remarks>
    /// <b>The chance bar is one over this</b>, since a question names the other
    /// channels and asks what the last one showed.
    /// </remarks>
    public int Causes { get; init; } = 12;

    /// <summary>
    /// How many observable channels report the hidden state.
    /// </summary>
    /// <remarks>
    /// <b>THE NUMBER THE WHOLE CLAIM IS ABOUT.</b> The channels' pairwise edges go
    /// as <c>k(k-1)/2</c> and a hub over them is <c>k</c>, so what a posited node
    /// saves grows with this while what it costs does not — see
    /// <see cref="Learning.Paying.Cheaper"/>, which is why four is the smallest
    /// group worth naming.
    /// </remarks>
    public int Channels { get; init; } = 6;
}

/// <summary>
/// A world whose structure is a thing that is never shown.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE FIRST WORLD HERE WHOSE BEST EXPLANATION IS NOT IN IT.</b> Every moment,
/// a hidden cause takes one of <see cref="LatentSettings.Causes"/> states and every
/// channel reports it. The channels therefore co-occur constantly and NONE OF THEM
/// CAUSES ANY OTHER — the thing that would explain them is never emitted, has no
/// code, and cannot be reached by any walk.
/// </para>
/// <para>
/// <b>SO IT IS THE MEASUREMENT `Thought.Grouped` HAS BEEN MISSING.</b> That method
/// finds origins which all reached one another, and every world already here either
/// has no latent structure or has one nobody stated — a hub minted over a group was
/// cheaper by arithmetic and unmeasurable in fact. Here the group is exactly the
/// channels, and the hub is exactly the cause.
/// </para>
/// <para>
/// <b>THE INTERESTING NUMBER IS COST AND NOT ACCURACY, exactly as on
/// <see cref="Motif"/>.</b> Pairwise counts already answer this perfectly — the
/// channels co-occur, so the edges are what they should be. What they cannot do is
/// stop paying: every moment writes <c>k(k-1)</c> row entries where a hub would
/// write <c>k</c>, and cost per thought is set by the widest row. Accuracy is here
/// to show the compression would not have cost anything.
/// </para>
/// <para>
/// <b>AND IT IS HONEST ABOUT WHAT A HUB WOULD BUY AND WHAT IT WOULD COST.</b> A
/// posited node makes the answer TWO hops where it was one, so a walk must afford
/// the extra step; what it saves is the fan-out, since a channel points at one hub
/// rather than at every sibling. Both directions are reported and neither is
/// assumed.
/// </para>
/// </remarks>
public sealed class Latent
{
    /// <summary>The modality every observable channel emits into.</summary>
    /// <remarks>
    /// <b>One for all the channels, with the channel folded into the value.</b> Two
    /// channels reporting one cause are different codes; a walk narrowed to this
    /// modality sees the observables and never the cause, which has no code at all.
    /// </remarks>
    public const byte Seen = 90;

    private readonly LatentSettings _world;
    private readonly Random _rng;

    /// <param name="world">How many causes, and how many channels report them.</param>
    /// <param name="seed">This run's own generator.</param>
    public Latent(LatentSettings world, int seed)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(world.Causes);

        // FOUR IS WHERE A HUB STARTS PAYING, so a world below it cannot exercise
        // the thing it exists to measure. Refused rather than left to read as a
        // mechanism that did nothing.
        if (world.Channels < 4)
            throw new ArgumentOutOfRangeException(nameof(world),
                $"{world.Channels} channels cannot pay for a hub -- three hold three "
                + "edges against a hub's three plus one. See Paying.Cheaper.");

        _world = world;
        _rng = new Random(Seeds.Apart(seed, 0x1A7E_0000));
    }

    /// <inheritdoc cref="LatentSettings.Channels"/>
    public int Channels => _world.Channels;

    /// <inheritdoc cref="LatentSettings.Causes"/>
    public int Causes => _world.Causes;

    /// <summary>What a blind guess would score.</summary>
    public double Chance => 1.0 / _world.Causes;

    /// <summary>
    /// What one channel shows when the hidden thing is in one state.
    /// </summary>
    /// <remarks>
    /// <b>Derived, so two machines reading the same stream agree</b> — and the
    /// channel is folded in, so no two channels ever emit the same code.
    /// </remarks>
    public static Code Shows(int channel, int cause)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(channel);
        ArgumentOutOfRangeException.ThrowIfNegative(cause);

        return new Code(Seen, Agreed.Mix(Agreed.Fold(
            Agreed.Fold(Agreed.Basis, (ulong)channel), (ulong)cause)));
    }

    /// <summary>Which state the hidden thing is in, and what every channel showed.</summary>
    /// <remarks>
    /// <b>The cause is returned so a test can check the world, and is NEVER
    /// emitted.</b> It has no code and joins no occasion — that is the whole point
    /// of it.
    /// </remarks>
    public (int Cause, ImmutableArray<Code> Shown) Moment()
    {
        var cause = _rng.Next(_world.Causes);

        return (cause,
            [.. Enumerable.Range(0, _world.Channels).Select(one => Shows(one, cause))]);
    }
}
