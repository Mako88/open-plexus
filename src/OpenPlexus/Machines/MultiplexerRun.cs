using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Machines;

/// <summary>A trial on the multiplexer, plus what only that world can say.</summary>
/// <remarks>
/// <b>Soundness and the answer key are world facts, so they are asked for here rather
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

    /// <summary>
    /// Of the sound ones, how many strictly contain a SHORTER rule that is also sound —
    /// <b>the chain having gone past a depth where it could have stopped.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Nothing in the machine knows when a scope is deep enough, and until this there
    /// was no number for it.</b> The mean scope of the carriers says how deep the rules
    /// that pay are; it cannot say whether that depth was NEEDED. This asks the world
    /// directly: drop one code, is the remainder still true? A rule where some drop leaves
    /// a truth is a rule that fires on fewer moments than a rule it already contains.
    /// </para>
    /// <para>
    /// <b>And it is a property of the population rather than of a lineage, which is why it
    /// needs no new plumbing.</b> Whether the parent it descended from was itself sound is
    /// a question about a history the brain does not keep; whether what it holds NOW is
    /// longer than it had to be is decidable from the scope and the world's enumeration.
    /// </para>
    /// <para>
    /// <b>And the floor on misses is the reason this could be nought.</b> Repair refuses a
    /// parent under <c>Floor</c> misses, and a sound rule on a clean world never misses at
    /// all — so on such a world the chain may already stop by construction, and the depth
    /// the carriers sit at would be the route rather than the overshoot. That is a
    /// different diagnosis from the one this reading was built to test, and only the
    /// number tells them apart.
    /// </para>
    /// <para>
    /// <b>Nothing where it was not asked for.</b> Because it is the most expensive reading here
    /// and it timed a CI shard out. One soundness check enumerates every assignment the
    /// scope leaves open — up to <c>2^Widest</c> — and this asks one per code of every sound
    /// rule, each with one MORE bit free than the check it came from. On a wide world that is
    /// hundreds of millions of assignments per graded run, and it is charged once per run
    /// rather than once per round, which is why nothing else noticed.
    /// </para>
    /// <para>
    /// <b>So it is null rather than nought where the census is off</b>, which is the same
    /// shape <c>Census</c> itself uses and the only one that cannot be misread. A zero here
    /// would say <i>nothing is over-specialised</i>, which is a finding, and the absence of a
    /// reading is not one.
    /// </para>
    /// </remarks>
    public required int? Overshot { get; init; }

    /// <summary>How many of the world's own rules are held exactly.</summary>
    public required int Found { get; init; }

    /// <summary>
    /// Of those, how many expect something OTHER than the commonest outcome — <b>the split
    /// that says whether a found rule could ever have paid.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A true rule expecting the commonest outcome fires only where guessing already
    /// works</b>, however accurate it is and however sound. So <see cref="Found"/> counts two
    /// unlike things at once, and two levers have now moved it a long way with
    /// <c>Census.Paying</c> flat — thirteen of sixteen against six under skew, and no more
    /// hard rounds carried. Neither reading could say whether the rules arriving were the
    /// useless kind.
    /// </para>
    /// <para>
    /// <b>Named for what it bounds rather than for what it counts, so it cannot be read as
    /// <c>Census.Paying</c>.</b> That one is a SHARE of the hard rounds actually carried;
    /// this is a COUNT of the world's rules that could in principle carry one, and a grid
    /// carrying both under one word would be two different questions in one column.
    /// </para>
    /// <para>
    /// <b>It is an upper bound on what could pay and never a count of what did</b>, which is
    /// the whole reason it sits beside <c>Census.Carried</c> rather than replacing it. A rule
    /// expecting the rare outcome still has to FIRE on a round the rare outcome arrives on,
    /// and a deep enough scope may never do so.
    /// </para>
    /// <para>
    /// <b>And it is nought where the census did not run, which is not the same as no such
    /// rule being held.</b> The commonest outcome is read off what arrived, so a run that was
    /// never censused has nothing to split by — this repo's own trap about a check that is
    /// wired and unable to fire, avoided by saying so here rather than by a number that looks
    /// like a finding.
    /// </para>
    /// </remarks>
    public required int Payable { get; init; }

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

    /// <inheritdoc cref="Tally.Eligible"/>
    public int Eligible => Tally.Eligible;

    /// <inheritdoc cref="Tally.Stackable"/>
    public int Stackable => Tally.Stackable;

    /// <inheritdoc cref="Tally.Speaking"/>
    public double Speaking => Tally.Speaking;

    /// <inheritdoc cref="Tally.PerEligible"/>
    public double PerEligible => Tally.PerEligible;

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
    /// <param name="detailed">
    /// Whether to take <see cref="Overshot"/>, which is the most expensive reading here.
    /// </param>
    /// <param name="sound">Whether a scope really does entail an expectation.</param>
    /// <remarks>
    /// <para>
    /// <b>Written once because the clone budget refused the second copy</b>, on the day
    /// the second enumerable world arrived — the same thing that happened to
    /// <see cref="Round"/>, and for a better reason than tidiness. Two copies
    /// of a grading pass are two places for <i>experienced</i>, <i>checkable</i> and
    /// <i>sound</i> to drift apart, and a soundness count meaning one thing on one world
    /// and something else on another is not comparable between them — which is most of
    /// the point of having more than one world.
    /// </para>
    /// <para>
    /// <b>The scopes are spelled back out before the world is asked.</b> A world knows
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
        Func<ImmutableArray<Code>, Code, bool> sound,
        bool detailed)
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

        // One drop at a time and not every subset, which is the cheap half and the only half
        // that matters. If any shorter sound rule is contained at all then some single drop
        // reaches a sound scope on the way down, because soundness here is a property of the
        // pinned bits and dropping an irrelevant one cannot make a true rule false. So the
        // one-code question answers the general one at a k-th of the cost.
        //
        // AND CHEAP IS RELATIVE: every drop frees one more bit than the check it came from,
        // so on a wide world this is the most expensive thing in the file. It runs where the
        // census runs and nowhere else.
        int? overshot = !detailed ? null : decidable.Count(one =>
            one.Scope.Length > 1
            && sound(one.Scope, one.Expects)
            && one.Scope.Any(dropped =>
            {
                var shorter = one.Scope.Where(code => code != dropped).ToImmutableArray();

                return checkable(shorter) && sound(shorter, one.Expects);
            }));

        return new Learned
        {
            Tally = tally,
            Sound = true_,
            Overshot = overshot,
            Unsound = decidable.Count - true_,
            Unchecked = experienced.Count - decidable.Count,
            Truths = truths.Length,
            Found = truths.Count(truth => Holds(held, truth)),

            // And the same walk split by whether the rule could ever have paid. Written as a
            // second `Count` over the same predicate rather than folded into one pass,
            // because the grading here is read far more often than it is run and a pair of
            // obvious walks is worth more than one clever one.
            Payable = tally.Census?.Commonest is not { } commonest
                ? 0
                : truths.Count(truth =>
                    truth.Expects != commonest && Holds(held, truth)),
        };
    }

    /// <summary>Whether the population holds one of the world's rules exactly.</summary>
    /// <remarks>
    /// <b>Pulled out because it is asked twice now</b> — once over every truth and once over
    /// the truths expecting something other than the commonest outcome. Two copies of a scope
    /// comparison is two chances for one of them to unfold a minted name and the other not
    /// to, which is a fault that would read as a finding about which rules were found.
    /// </remarks>
    private static bool Holds(Commitments.Population held, Worlds.Truth truth) =>
        held.All.Any(one => one.Expects == truth.Expects
            && held.Names.Unfold(one.Scope).SequenceEqual(truth.Scope));
}

/// <summary>Step one, end to end, on the world it is judged on.</summary>
public sealed class MultiplexerRun
{
    private readonly Multiplexer _world;
    private readonly Brain _brain;
    private readonly Bench _trial;
    private readonly bool _census;

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
        _census = census;

        _trial = new Bench(
            new Watching<IReadOnlyList<int>>(_world, new Bits(Multiplexer.Bit)),
            brain,
            sound: census ? _world.Sound : null);
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
            _world.Checkable, _world.Sound, _census);
    }
}
