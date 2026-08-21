using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Machines;

/// <summary>Which chooser decides what the body does.</summary>
/// <remarks>
/// <para>
/// <b>Three arms over one seam</b>, which <see cref="IActed{TSeen}"/> named before anything
/// filled it. The oracle reads the world's own state and is the ceiling; the uniform draw is
/// the floor; the population-reading chooser is the learner between them. A number against
/// neither of the other two says nothing, so all three are run the same way.
/// </para>
/// <para>
/// <b>And it is a run dial rather than a brain one.</b> Which arm holds the body is a fact
/// about this world's experiment, so it lives here beside <see cref="Fronting"/> rather than
/// on <see cref="Commitments.CommittingSettings"/> — a bar demanding a second world of it
/// would be demanding that one world's setting be measured on another.
/// </para>
/// </remarks>
public enum Regulating
{
    /// <summary>Whichever variable is furthest from where it should be.</summary>
    Aimed,

    /// <summary>A variable drawn uniformly, knowing nothing.</summary>
    Uniform,

    /// <summary>What the population says will follow each action.</summary>
    Driven,
}

/// <summary>
/// What a run of the body came to, plus the two things only this world can say.
/// </summary>
public sealed record Regulated
{
    /// <summary>Every counter the bench reports.</summary>
    public required Tally Tally { get; init; }

    /// <summary>The share of steps the body stayed inside its bounds.</summary>
    /// <remarks>
    /// <b>One instrument for every arm, sampled at one point in the round.</b> Scoring the
    /// learner on <see cref="Tally.Recent"/> and the controls on a separate loop would be a
    /// statistic whose halves count different things, which is one of this repo's named traps
    /// and reads as a comparison.
    /// </remarks>
    public required double Viable { get; init; }

    /// <summary>Whether the body was still inside its bounds when the run stopped.</summary>
    public required bool Standing { get; init; }

    /// <summary>
    /// Rounds a commitment named the action, and nought for an arm with no population.
    /// </summary>
    public required long Told { get; init; }

    /// <summary>
    /// Rounds the fallback decided, and nought for an arm with no population.
    /// </summary>
    /// <remarks>
    /// <b>Reported beside the score</b>, because a fallback is a control arm nobody meant to
    /// run. A chooser told nothing on most rounds is its own control, and no score can tell
    /// the two apart.
    /// </remarks>
    public required long Untold { get; init; }
}

/// <summary>A body held inside its bounds, learnt while it is acted in.</summary>
/// <remarks>
/// <para>
/// <b>The preference is built here because it belongs at the join.</b> What a body wants is a
/// fact about that body; a chooser deciding it would be the library deciding what a world is
/// for, which is the same fault as a world naming a brain type one layer out.
/// </para>
/// <para>
/// <b>And the world is watched through the same seam whichever arm runs</b>, so the arms
/// differ in the chooser and in nothing else. An arm scored through its own loop is the
/// comparison this world exists to make, taken twice with two rulers.
/// </para>
/// </remarks>
public sealed class HomeostatRun
{
    private readonly Homeostat _body;
    private readonly Bench _trial;
    private readonly Drives? _drives;

    private int _viable;
    private int _steps;

    /// <param name="world">The shape of the body.</param>
    /// <param name="brain">The one brain, already configured.</param>
    /// <param name="arm">Which chooser decides what to do.</param>
    /// <param name="feeling">
    /// Whether the moment says what was done as well as what was felt.
    /// </param>
    /// <param name="seed">The generator behind the uniform draw and the fallback.</param>
    public HomeostatRun(
        HomeostatSettings world,
        Brain brain,
        Regulating arm,
        Feeling feeling,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(brain);

        _body = new Homeostat(world);

        var draw = new Random(seed);

        IChooses chosen;

        switch (arm)
        {
            case Regulating.Aimed:
                chosen = Chooses.From(_ => _body.Lowest);
                break;

            case Regulating.Uniform:
                chosen = Chooses.From(_ => draw.Next(_body.Doings));
                break;

            default:
                _drives = new Drives(
                    brain.Held,
                    code => code.Modality == Homeostat.Act ? Homeostat.Attended(code) : null,
                    Wants,
                    () => draw.Next(_body.Doings));

                chosen = Chooses.From(_drives.Choose);
                break;
        }

        _trial = new Bench(
            new Watching<Bodily>(
                _body,
                new Bodied(feeling),
                acting: Chooses.From(
                    felt =>
                    {
                        // Sampled before the step, and identically for every arm. Which side
                        // of the action it is read on shifts all three together, so the
                        // ordering is the arms' and not the sampling point's.
                        _steps++;
                        if (_body.Viable) _viable++;

                        return chosen.Choose(felt);
                    },
                    chosen.Cleared)),
            brain);
    }

    /// <summary>What a blind guess scores on this world.</summary>
    public double Chance => _trial.Chance;

    /// <summary>
    /// <b>How much this body wants an outcome</b>, read off what it feels and nothing else.
    /// </summary>
    /// <param name="expects">The outcome a commitment expects to follow.</param>
    /// <param name="felt">What the front end said about the state being acted in.</param>
    /// <remarks>
    /// <para>
    /// <b>The outcome here is which variable is lowest</b>, so an action is wanted by how well
    /// off the variable it leaves at the bottom is. Attending to the worst variable raises it
    /// and hands the bottom to a better one, so a body that prefers a high bottom is a body
    /// that regulates — and nothing about regulating is written down, it falls out of the
    /// preference.
    /// </para>
    /// <para>
    /// <b>And it reads the felt bands rather than <see cref="Homeostat.At"/></b>, which is the
    /// whole difference between this and the oracle. The band is what the front end emitted;
    /// the value is what the world holds. A drive on the second would be a reward handed in
    /// wearing a preference's name.
    /// </para>
    /// <para>
    /// <b>An outcome the body cannot feel is wanted least</b>, rather than treated as absent.
    /// A missing band means the expectation names no variable this state reported, and ranking
    /// it with the rest would let a claim about nothing win a round.
    /// </para>
    /// </remarks>
    private static double Wants(Code expects, IReadOnlyCollection<Code> felt)
    {
        // The outcome's own number, recovered the way it was made. `Brain.Says` is the one
        // place an outcome becomes a code, so reading it back here rather than by arithmetic
        // on the modality keeps the two ends of the mapping together.
        foreach (var code in felt)
        {
            if (Homeostat.Sensed(code) is not int which) continue;
            if (Brain.Says(which) != expects) continue;

            return code.Value;
        }

        return double.NegativeInfinity;
    }

    /// <summary>Runs the world and learns from it.</summary>
    /// <param name="rounds">How many rounds.</param>
    /// <param name="sweep">How often to subsume, abstract and cull.</param>
    /// <param name="target">The trailing accuracy to wait for.</param>
    /// <param name="window">How many answered predictions that accuracy is over.</param>
    public Regulated Run(long rounds, int sweep = 500, double target = 0.9, int window = 1000)
    {
        var tally = _trial.Run(rounds, sweep, target, window);

        return new Regulated
        {
            Tally = tally,
            Viable = _steps == 0 ? 0.0 : _viable / (double)_steps,
            Standing = _body.Viable,
            Told = _drives?.Told ?? 0,
            Untold = _drives?.Untold ?? 0,
        };
    }
}
