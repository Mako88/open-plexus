using OpenPlexus.Codes;
using OpenPlexus.Commitments;

namespace OpenPlexus.Worlds;

/// <summary>What one run of the multiplexer learnt, and what it cost.</summary>
public sealed record Learned
{
    /// <summary>Rounds run.</summary>
    public required long Rounds { get; init; }

    /// <summary>Predictions that matched what was emitted.</summary>
    public required long Right { get; init; }

    /// <summary>Predictions that did not.</summary>
    public required long Wrong { get; init; }

    /// <summary>
    /// Rounds where nothing fired, so there was no prediction to be wrong.
    /// </summary>
    /// <remarks>
    /// <b>REPORTED BESIDE THE SCORE, BECAUSE SILENCE IS A CONTROL ARM NOBODY MEANT
    /// TO RUN.</b> A learner that says nothing scores neither right nor wrong, and an
    /// accuracy taken over what it did say drifts upward for free.
    /// </remarks>
    public required long Silent { get; init; }

    /// <summary>Settlements that could not say.</summary>
    /// <remarks>
    /// <b>THIS READS ZERO IN A SINGLE PROCESS AND THAT IS NOT THE SAME AS ARMED.</b>
    /// Abstaining exists for C3, and nothing here can die, so the path is exercised
    /// by <c>PopulationTests</c> rather than by any run. Until the bus is back under
    /// this, the number below is a placeholder honestly labelled as one.
    /// </remarks>
    public required long Abstained { get; init; }

    /// <summary>Predictions over the last tenth of the run that were right.</summary>
    public required long Settled { get; init; }

    /// <summary>Predictions over the last tenth of the run that were answered at all.</summary>
    public required long Answered { get; init; }

    /// <summary>Commitments resident at the end.</summary>
    public required int Resident { get; init; }

    /// <summary>How many of the world's true rules are held EXACTLY.</summary>
    /// <remarks>
    /// <b>Against ONE basis, which is why it is not the headline.</b> The world admits
    /// several correct rule sets, so a learner that found a different one scores badly
    /// here while being right — see <see cref="Sound"/>.
    /// </remarks>
    public required int Found { get; init; }

    /// <summary>How many true rules there are, in that basis.</summary>
    public required int Truths { get; init; }

    /// <summary>Experienced commitments that are true of the world, whatever basis they are in.</summary>
    /// <remarks>
    /// <b>THE NUMBER STEP ONE IS ACTUALLY JUDGED ON.</b> Checked by enumerating every
    /// assignment a scope leaves open rather than by comparing against a chosen key,
    /// so it asks whether a rule is TRUE instead of whether it is the one expected.
    /// </remarks>
    public required int Sound { get; init; }

    /// <summary>Experienced commitments that are not.</summary>
    /// <remarks>
    /// <b>Reported beside <see cref="Sound"/> because a count of correct rules alone
    /// can be reached by minting everything.</b> What matters is the share.
    /// </remarks>
    public required int Unsound { get; init; }

    /// <summary>Experienced commitments too general to settle by enumeration.</summary>
    /// <remarks>
    /// <b>NOT FOLDED INTO THE BAD NEWS.</b> A one-code scope in a twenty-bit world
    /// leaves nineteen bits free, and counting what cannot be settled as unsound
    /// would make the share of true rules fall with the width of the world for a
    /// reason that has nothing to do with learning.
    /// </remarks>
    public required int Unchecked { get; init; }

    /// <summary>The round a trailing window first held the target, or zero if never.</summary>
    /// <remarks>
    /// <b>THE SCALING NUMBER, AND THE ONE THAT PREDICTS WHETHER ANY OF THIS REACHES
    /// PERCEPTION.</b> A final accuracy says how well a width was learnt; this says
    /// what it COST, and how that cost grows with the number of relevant bits is the
    /// whole question. A curve that is exponential in relevant bits is a dead end
    /// found cheaply on a generated world rather than expensively on a real one.
    /// </remarks>
    public required long Reached { get; init; }

    /// <summary>Children minted by repair.</summary>
    public required long Repaired { get; init; }

    /// <summary>Codes minted to stand for sub-scopes that kept recurring.</summary>
    /// <remarks>
    /// <b>Rung five's only visible output, and the thing that says whether the machine
    /// holds a CONCEPT rather than a pile of correct rules.</b> Zero means everything
    /// learnt was learnt separately.
    /// </remarks>
    public required int Named { get; init; }

    /// <summary>Names that stand for a set containing another name.</summary>
    /// <remarks>
    /// <b>THE ONLY NUMBER THAT SAYS WHETHER A SECOND LEVEL EXISTS.</b> Naming a
    /// hundred sets of raw codes is one level of structure however many there are;
    /// one name built on another is the recursion the whole rung is for, and it is
    /// what DreamCoder's bootstrapping means. Zero is a flat vocabulary.
    /// </remarks>
    public required int Stacked { get; init; }

    /// <summary>Commitments that have spent their whole repair budget.</summary>
    /// <remarks>
    /// <b>A GUARD HAS TO BE SHOWN NOT TO BE GUARDING.</b> Anything above zero means
    /// the budget is deciding what gets learnt rather than catching a runaway.
    /// </remarks>
    public required int Exhausted { get; init; }

    /// <summary>The share of answered predictions that were right, over the whole run.</summary>
    public double Accuracy => Right + Wrong == 0 ? 0.0 : Right / (double)(Right + Wrong);

    /// <summary>The same over the last tenth, which is what a learner is judged on.</summary>
    /// <remarks>
    /// <b>A lifetime accuracy on a learning run is mostly a record of not having
    /// learnt yet</b>, so it measures the length of the run as much as the mechanism.
    /// </remarks>
    public double Recent => Answered == 0 ? 0.0 : Settled / (double)Answered;
}

/// <summary>
/// Step one, end to end: cues arrive, something is predicted, the settlement says
/// whether it was right, and what was wrong gets repaired.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE HORIZON IS ONE OCCASION, WHICH IS FORK 28.</b> A round's cues are the
/// moment and what follows them is the very next thing — decidable, order
/// independent, and the simplest arrangement that can fail. A miss is *the settlement
/// closed and this was not in it* rather than *this did not arrive by now*, so
/// nothing here has a clock in it.
/// </para>
/// <para>
/// <b>COVERING AND REPAIR BOTH RUN ONLY ON A FAILURE.</b> Minting on every round
/// would fill the population with restatements of moments already predicted, and
/// repairing on every round would spend the whole budget on commitments that are
/// working.
/// </para>
/// </remarks>
public sealed class MultiplexerRun
{
    private readonly Multiplexer _world;
    private readonly Population _held;
    private readonly int _floor;
    private readonly int _budget;

    /// <param name="world">The shape of the world.</param>
    /// <param name="dials">Every number the machinery is allowed to have.</param>
    /// <param name="seed">The run's own generator.</param>
    public MultiplexerRun(MultiplexerSettings world, CommittingSettings dials, int seed)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        _world = new Multiplexer(world, seed);
        _held = new Population(dials, seed);
        _floor = dials.Floor;
        _budget = dials.Budget;
    }

    /// <summary>What the machine holds, for anything that wants to look.</summary>
    public Population Held => _held;

    /// <summary>Runs the world and learns from it.</summary>
    /// <param name="rounds">How many rounds.</param>
    /// <param name="sweep">How often to subsume, abstract and cull.</param>
    /// <param name="target">The trailing accuracy <see cref="Learned.Reached"/> waits for.</param>
    /// <param name="window">How many answered predictions that trailing accuracy is over.</param>
    public Learned Run(long rounds, int sweep = 1000, double target = 0.9, int window = 2000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rounds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sweep);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(window);

        var cycle = new Cycle(_held, rounds, sweep, target, window);

        for (long round = 0; round < rounds; round++)
        {
            var shown = _world.Next();

            // FOLDED ONCE AND USED EVERYWHERE. Matching, covering and the tally all
            // take the same moment, so a minted name can be matched on, minted
            // against and chosen as a repair condition -- which is what makes a
            // second level of structure reachable rather than merely representable.
            cycle.Step(_held.Moment(shown.Cues.ToHashSet()), shown.Outcome);
        }

        var truths = _world.Truths();

        // SPELLED BACK OUT BEFORE THE WORLD IS ASKED. A world knows nothing about
        // minted codes, so a rule written in them can only be checked once its names
        // are expanded -- and a rewrite that changed what a commitment CLAIMS would
        // show up right here as a rule that had stopped being true.
        var experienced = _held.All
            .Where(one => one.Seen >= _floor)
            .Select(one => (Scope: _held.Names.Unfold(one.Scope), one.Expects))
            .ToList();

        var checkable = experienced.Where(one => _world.Checkable(one.Scope)).ToList();

        var sound = checkable.Count(one => _world.Sound(one.Scope, one.Expects));

        return new Learned
        {
            Sound = sound,
            Unsound = checkable.Count - sound,
            Unchecked = experienced.Count - checkable.Count,
            Reached = cycle.Reached,
            Exhausted = _held.Exhausted(_budget),
            Rounds = rounds,
            Right = cycle.Right,
            Wrong = cycle.Wrong,
            Silent = cycle.Silent,
            Abstained = _held.All.Sum(one => one.Abstains),
            Settled = cycle.Settled,
            Answered = cycle.Answered,
            Resident = _held.Count,
            Found = truths.Count(truth => _held.All.Any(
                one => one.Expects == truth.Expects
                    && _held.Names.Unfold(one.Scope).SequenceEqual(truth.Scope))),
            Named = _held.Names.Count,
            Stacked = _held.Names.Means.Count(
                one => one.Value.Any(member => _held.Names.Knows(member))),
            Truths = truths.Length,
            Repaired = cycle.Repaired,
        };
    }
}
