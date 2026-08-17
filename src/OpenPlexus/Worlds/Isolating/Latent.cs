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
    /// <b>The number the whole claim is about.</b> The channels' pairwise relations
    /// go as <c>k(k-1)/2</c> and one name over them is <c>k</c>, so what a minted
    /// name saves grows with this while what it costs does not — which is why four
    /// is the smallest group worth naming, and why this world is rung five's.
    /// </remarks>
    public int Channels { get; init; } = 6;

    /// <summary>
    /// The share of reports in which a channel shows the wrong state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Zero is the control</b>, and it is the shape this world shipped in. A channel
    /// that never lies makes every other channel redundant in the sense that costs
    /// nothing: one code settles the answer, so genesis mints a one-code commitment that
    /// is never wrong, repair is never asked, and no scope longer than one code ever
    /// exists. Rung five's trigger is redundancy across scopes and there are no scopes to
    /// hold it. Measured rather than argued — see <c>LatentTests</c>.
    /// </para>
    /// <para>
    /// <b>So a lying channel is what makes the group necessary</b>, which is what this
    /// world claims to be about. Under noise no single channel settles the answer and
    /// agreement between several does, so repair grows scopes over channels that always
    /// co-occur — and a sub-scope shared between two such scopes is exactly the hub the
    /// cause would have been.
    /// </para>
    /// </remarks>
    public double Noise { get; init; } = 0.1;
}

/// <summary>
/// A world whose structure is a thing that is never shown.
/// </summary>
/// <remarks>
/// <para>
/// <b>The first world here whose best explanation is not in it.</b> Every moment,
/// a hidden cause takes one of <see cref="LatentSettings.Causes"/> states and every
/// channel reports it. The channels therefore co-occur constantly and none of them
/// causes any other — the thing that would explain them is never emitted, has no
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
/// <b>The interesting number is cost and not accuracy, exactly as on
/// <see cref="Motif"/>.</b> Pairwise counts already answer this perfectly — the
/// channels co-occur, so the edges are what they should be. What they cannot do is
/// stop paying: every moment writes <c>k(k-1)</c> row entries where a hub would
/// write <c>k</c>, and cost per thought is set by the widest row. Accuracy is here
/// to show the compression would not have cost anything.
/// </para>
/// <para>
/// <b>And it is honest about what a hub would buy and what it would cost.</b> A
/// posited node makes the answer TWO hops where it was one, so a walk must afford
/// the extra step; what it saves is the fan-out, since a channel points at one hub
/// rather than at every sibling. Both directions are reported and neither is
/// assumed.
/// </para>
/// </remarks>
public sealed class Latent : IWorld<Coded>
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
        ArgumentOutOfRangeException.ThrowIfNegative(world.Noise);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(world.Noise, 1.0);

        // A lie has to have somewhere to go, so one state and a lying channel are
        // incompatible rather than merely uninteresting.
        if (world.Noise > 0 && world.Causes < 2)
            throw new ArgumentOutOfRangeException(nameof(world),
                "a channel cannot show the wrong state where there is only one state");

        // Four is where a hub starts paying, so a world below it cannot exercise
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

    /// <summary>Which state the hidden thing is in, and which state each channel reported.</summary>
    /// <remarks>
    /// <para>
    /// <b>The cause is returned so a test can check the world</b>, and is never emitted.
    /// It has no code and joins no occasion — that is the whole point of it.
    /// </para>
    /// <para>
    /// <b>A lying channel shows another state and never nothing</b>, so a lie is
    /// indistinguishable from the truth at the code, which is what makes agreement between
    /// channels the only evidence there is. A silent channel would be a lie the learner
    /// could see.
    /// </para>
    /// </remarks>
    public (int Cause, ImmutableArray<int> Reported) Draw()
    {
        var cause = _rng.Next(_world.Causes);

        var reported = new int[_world.Channels];

        for (var channel = 0; channel < _world.Channels; channel++)
        {
            if (_world.Noise > 0 && _rng.NextDouble() < _world.Noise)
            {
                // Uniform over the other states, drawn as an offset so no state is ever
                // its own lie and no state is lied onto more often than another.
                reported[channel] = (cause + 1 + _rng.Next(_world.Causes - 1)) % _world.Causes;
                continue;
            }

            reported[channel] = cause;
        }

        return (cause, [.. reported]);
    }

    /// <inheritdoc cref="Draw"/>
    public (int Cause, ImmutableArray<Code> Shown) Moment()
    {
        var (cause, reported) = Draw();

        return (cause,
            [.. Enumerable.Range(0, _world.Channels).Select(one => Shows(one, reported[one]))]);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>The states of the hidden thing</b>, because the last channel shows one of them
    /// and a question naming the other channels asks which. The answer is a state rather
    /// than the cause, and the two are the same number only where nothing lies.
    /// </remarks>
    public int Outcomes => _world.Causes;

    /// <summary>
    /// What a model that knew the generative process would score.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An upper bound rather than an attainable score</b>, and it is stated that way
    /// because the two come apart here. Knowing the cause exactly still leaves the withheld
    /// channel free to lie, and it does so at <see cref="LatentSettings.Noise"/> — so no
    /// model of any kind passes one minus that. Inferring the cause from the other channels
    /// is nearly free at a low noise rate and is not free, so the attainable score sits
    /// under this by however much the inference misses.
    /// </para>
    /// <para>
    /// <b>The gap to <see cref="Marginal"/> is the whole of what the channels carry</b>,
    /// which is what makes the bar worth computing. Accuracy is here to show a hub would
    /// have cost nothing, exactly as on <see cref="Motif"/>.
    /// </para>
    /// </remarks>
    public double Ceiling => 1.0 - _world.Noise;

    /// <summary>Always naming one state, whichever it is.</summary>
    /// <remarks>
    /// <b>The same as <see cref="Chance"/> here</b>, because the cause is drawn uniformly
    /// and a lie is drawn uniformly over the rest — so every state arrives as the withheld
    /// channel's report exactly one time in <see cref="LatentSettings.Causes"/>, at any
    /// noise rate. Both are reported because they come apart the moment either draw is
    /// skewed, and a bar that silently equals another is a bar nobody can read.
    /// </remarks>
    public double Marginal => 1.0 / _world.Causes;

    /// <summary>
    /// One moment through the seam every world shares: every channel but the last, and
    /// what the last one showed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The withheld channel is the last one</b> rather than a drawn one, so the question
    /// is the same question every round and the cue never varies in shape. Which channel is
    /// held back cannot matter, the channels being interchangeable by construction.
    /// </para>
    /// <para>
    /// <b>The outcome is what that channel reported</b>, which is the cause where it told
    /// the truth and another state where it did not. Answering the cause instead would hand
    /// the learner a fact no channel carries, and the ceiling would then be a number the
    /// world does not have.
    /// </para>
    /// <para>
    /// <b>The cause is still never emitted.</b> It has no code, joins no moment, and reaches
    /// the learner only as the thing several channels are evidence about.
    /// </para>
    /// </remarks>
    public Turn<Coded> Next()
    {
        var (_, reported) = Draw();

        return new Turn<Coded>
        {
            Seen = Coded.Of(
                [.. Enumerable.Range(0, _world.Channels - 1)
                    .Select(one => Shows(one, reported[one]))]),
            Outcome = reported[^1],
        };
    }
}
