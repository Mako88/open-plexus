using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Machines;

/// <summary>A trial on the multiplexer, plus what only that world can say.</summary>
/// <remarks>
/// <b>SOUNDNESS AND THE ANSWER KEY ARE WORLD FACTS, so they are asked for here rather
/// than carried in <see cref="Tally"/>.</b> A shared report that grew a field per
/// world would put every world's vocabulary in front of every other one.
/// </remarks>
public sealed record Learned
{
    /// <summary>What the trial did, in terms every world shares.</summary>
    public required Tally Tally { get; init; }

    /// <summary>Experienced commitments that are true of the world, in any basis.</summary>
    public required int Sound { get; init; }

    /// <summary>Experienced commitments that are not.</summary>
    public required int Unsound { get; init; }

    /// <summary>Experienced commitments too general to settle by enumeration.</summary>
    public required int Unchecked { get; init; }

    /// <summary>How many of the world's own rules are held exactly.</summary>
    public required int Found { get; init; }

    /// <summary>How many rules that basis holds.</summary>
    public required int Truths { get; init; }

    /// <summary>The share of answered predictions right over the last tenth.</summary>
    public double Recent => Tally.Recent;

    /// <summary>Commitments resident at the end.</summary>
    public int Resident => Tally.Resident;

    /// <summary>Rounds where nothing fired.</summary>
    public long Silent => Tally.Silent;

    /// <summary>Rounds the world could not say the outcome of.</summary>
    public long Abstained => Tally.Abstained;

    /// <inheritdoc cref="Tally.Census"/>
    public Census? Census => Tally.Census;

    /// <summary>The round a trailing window first held the target.</summary>
    public long Reached => Tally.Reached;

    /// <summary>Children minted by repair.</summary>
    public long Repaired => Tally.Repaired;

    /// <summary>Codes minted to stand for recurring sub-scopes.</summary>
    public int Named => Tally.Named;

    /// <summary>Names standing for a set containing another name.</summary>
    public int Stacked => Tally.Stacked;

    /// <summary>Commitments that have spent their whole repair budget.</summary>
    public int Exhausted => Tally.Exhausted;

    /// <summary>Rounds run.</summary>
    public long Rounds => Tally.Rounds;

    /// <summary>
    /// Grades what a population holds against a world that can say what is true.
    /// </summary>
    /// <param name="tally">What the run reported.</param>
    /// <param name="truths">The world's answer key.</param>
    /// <param name="held">What the brain holds.</param>
    /// <param name="floor">How much a commitment must have seen before it is judged.</param>
    /// <param name="checkable">Whether the world can decide a scope exactly.</param>
    /// <param name="sound">Whether a scope really does entail an expectation.</param>
    /// <remarks>
    /// <para>
    /// <b>WRITTEN ONCE BECAUSE THE CLONE BUDGET REFUSED THE SECOND COPY</b>, on the day
    /// the second enumerable world arrived — the same thing that happened to
    /// <see cref="Commitments.Cycle"/>, and for a better reason than tidiness. Two copies
    /// of a grading pass are two places for <i>experienced</i>, <i>checkable</i> and
    /// <i>sound</i> to drift apart, and a soundness count meaning one thing on one world
    /// and something else on another is not comparable between them — which is most of
    /// the point of having more than one world.
    /// </para>
    /// <para>
    /// <b>THE SCOPES ARE SPELLED BACK OUT BEFORE THE WORLD IS ASKED.</b> A world knows
    /// nothing about minted codes, so a rule written in them can only be checked once its
    /// names are expanded — and a rewrite that changed what a commitment CLAIMS would
    /// show up right here as a rule that had stopped being true.
    /// </para>
    /// </remarks>
    internal static Learned Grade(
        Tally tally,
        ImmutableArray<Worlds.Truth> truths,
        Commitments.Population held,
        long floor,
        Func<ImmutableArray<Code>, bool> checkable,
        Func<ImmutableArray<Code>, Code, bool> sound)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(checkable);
        ArgumentNullException.ThrowIfNull(sound);

        var experienced = held.All
            .Where(one => one.Seen >= floor)
            .Select(one => (Scope: held.Names.Unfold(one.Scope), one.Expects))
            .ToList();

        var decidable = experienced.Where(one => checkable(one.Scope)).ToList();
        var true_ = decidable.Count(one => sound(one.Scope, one.Expects));

        return new Learned
        {
            Tally = tally,
            Sound = true_,
            Unsound = decidable.Count - true_,
            Unchecked = experienced.Count - decidable.Count,
            Truths = truths.Length,
            Found = truths.Count(truth => held.All.Any(
                one => one.Expects == truth.Expects
                    && held.Names.Unfold(one.Scope).SequenceEqual(truth.Scope))),
        };
    }
}

/// <summary>Step one, end to end, on the world it is judged on.</summary>
public sealed class MultiplexerRun
{
    private readonly Multiplexer _world;
    private readonly Brain _brain;
    private readonly Trial<IReadOnlyList<int>> _trial;

    /// <param name="world">The shape of the world.</param>
    /// <param name="brain">The one brain, already configured.</param>
    /// <param name="seed">The world's own generator.</param>
    /// <param name="census">
    /// Whether to partition the wrong rounds by cause — <b>off by default, because it
    /// costs a second match every round.</b>
    /// </param>
    public MultiplexerRun(MultiplexerSettings world, Brain brain, int seed, bool census = false)
    {
        ArgumentNullException.ThrowIfNull(brain);

        _world = new Multiplexer(world, seed);
        _brain = brain;

        _trial = new Trial<IReadOnlyList<int>>(
            _world, new Bits(Multiplexer.Bit), brain, census ? _world.Sound : null);
    }

    /// <summary>What the brain holds.</summary>
    public Commitments.Population Held => _brain.Held;

    /// <summary>Runs the world and learns from it.</summary>
    /// <param name="rounds">How many rounds.</param>
    /// <param name="sweep">How often to subsume, abstract and cull.</param>
    /// <param name="target">The trailing accuracy to wait for.</param>
    /// <param name="window">How many answered predictions that accuracy is over.</param>
    public Learned Run(long rounds, int sweep = 1000, double target = 0.9, int window = 2000)
    {
        var tally = _trial.Run(rounds, sweep, target, window);

        return Learned.Grade(
            tally, _world.Truths(), _brain.Held, _brain.Dials.Floor,
            _world.Checkable, _world.Sound);
    }
}
