using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Machines;

/// <summary>
/// The MONK's problems, end to end, against a key that says what the answer can be.
/// </summary>
/// <remarks>
/// <para>
/// <b>The only world here whose ceiling is known before the run.</b> Elsewhere a
/// disappointing score leaves two explanations standing — the learner, or the language
/// it has to say its answer in — and no measurement separates them. <see cref="Monk"/>
/// enumerates both: the concept is 432 instances and the scope language is 2,880
/// conjunctions, so what a conjunction CAN say is decided rather than discovered.
/// </para>
/// <para>
/// <b>SO <see cref="Learned.Found"/> is read against a basis that is itself the
/// finding.</b> On <see cref="Puzzle.Two"/> the minimal basis is 254 rules of which 142
/// are whole six-attribute instances, because nothing shorter can soundly say yes — so a
/// learner that finds all of them has MEMORISED the concept and a learner that finds few
/// has not generalised, and for once those are not the same number.
/// </para>
/// </remarks>
public sealed class MonkRun
{
    private readonly Monk _world;
    private readonly Puzzle _puzzle;
    private readonly Brain _brain;
    private readonly Bench _trial;

    /// <param name="world">Which puzzle, and how much is held back.</param>
    /// <param name="brain">The one brain, already configured.</param>
    /// <param name="seed">The world's own generator.</param>
    /// <param name="census">
    /// Whether to partition the wrong rounds by cause — <b>the second world that can</b>, and
    /// the one whose language ceiling is known in advance.
    /// </param>
    /// <remarks>
    /// <b>Which makes this the only place the census can be read</b> against a known
    /// ceiling. On <see cref="Puzzle.Two"/> a conjunction cannot soundly say yes
    /// short of a whole instance, so an uncovered bucket there is the SCOPE LANGUAGE
    /// failing rather than covering failing — and on the multiplexer the same number
    /// means the opposite, because every rule it needs is expressible. Two worlds
    /// separate a diagnosis one cannot.
    /// </remarks>
    public MonkRun(MonkSettings world, Brain brain, int seed, bool census = false)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(brain);

        _world = new Monk(world, seed);
        _puzzle = world.Puzzle;
        _brain = brain;

        // One code per attribute and value, against a declared stride. The jacket has
        // four colours and the tie has two, so a stride taken from the widest attribute
        // is the only one that cannot alias -- which is the fault `Bits` already carried
        // once, when its only caller happened to be binary.
        _trial = new Bench(
            new Watching<IReadOnlyList<int>>(
                _world, new Bits(Monk.Attribute, Monk.Stride)),
            brain,
            sound: census
                ? (scope, expects) => Monk.Sound(world.Puzzle, scope, expects)
                : null);
    }

    /// <summary>What the brain holds.</summary>
    public Commitments.Population Held => _brain.Held;

    /// <summary>What a silent arm scores, which is not a half on the second puzzle.</summary>
    public double Chance => _world.Chance;

    /// <summary>Runs the world and learns from it.</summary>
    /// <param name="rounds">How many rounds.</param>
    /// <param name="sweep">How often to subsume, abstract and cull.</param>
    /// <param name="target">The trailing accuracy to wait for.</param>
    /// <param name="window">How many answered predictions that accuracy is over.</param>
    public Learned Run(long rounds, int sweep = 1000, double target = 0.9, int window = 2000)
    {
        var tally = _trial.Run(rounds, sweep, target, window);

        // Everything is checkable here and that is why this world was chosen. There are
        // 432 instances however little a scope pins, so `Unchecked` is nought by
        // construction rather than by luck -- and a soundness count quietly covering
        // only the small rules would be the sampled-instrument fault this project keeps
        // finding.
        return Learned.Grade(
            tally, Monk.Truths(_puzzle), _brain.Held, _brain.Dials.Floor,
            Monk.Checkable,
            (scope, expects) => Monk.Sound(_puzzle, scope, expects),

            // NO OVERSHOOT READING HERE. This world enumerates 432 instances however little
            // a scope pins, so the drop-by-drop walk is cheap -- but nothing reads the column
            // on this bench, and an instrument nobody reads is a cost with no reader.
            detailed: false);
    }
}
