using System.Globalization;
using System.Text;

namespace OpenPlexus.Worlds;

/// <summary>
/// Everything a snake run can say about itself, printed at the end of it.
/// </summary>
/// <remarks>
/// <b>What is peculiar to snake, over the checks in <see cref="Measurement"/>.</b>
/// Snake is the one world here that acts rather than answering, so it has steps
/// and silence and predictions where the other two have questions.
/// </remarks>
public sealed record RunReport : Measurement
{
    public required RunResult Result { get; init; }

    /// <summary>The share of steps that produced no onset and so no thought.</summary>
    public double Silence => Result.Steps == 0 ? 0.0 : Result.Silent / (double)Result.Steps;

    /// <summary>What the graph foresaw of what CHANGED, over a blind draw.</summary>
    public double NoveltyGap => Result.Novelty.Precision - Result.Novelty.Blind;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Two, because snake's task is one hop.</b> An action code joins the
    /// occasion it was taken in, so a chain of two reaches one — and anything
    /// shallower means the walk is not walking.
    /// </remarks>
    protected override int Composes => 2;

    /// <inheritdoc/>
    protected override string Stalled => "no route walked at all";

    /// <inheritdoc/>
    protected override void Peculiar(List<string> wrong)
    {
        ArgumentNullException.ThrowIfNull(wrong);

        if (Result.Steps == 0) wrong.Add("the run took no steps");
        if (Result.Silent >= Result.Steps) wrong.Add("every step was silent");
        if (Result.Steps > 2 && Result.Novelty.Asked == 0)
            wrong.Add("nothing was ever predicted");

        // GUARDED BY SAMPLE SIZE, because a short run is not a broken one. A
        // three-step run genuinely has almost nothing to predict, and a check
        // that fires on that trains everyone to ignore the list.
        if (Result.Novelty.Guessed > 50 && Result.Novelty.Chance == 0)
            wrong.Add("the blind control never scored, so the gap means nothing");

        // FORK 18'S WIRING CHECK. A counterfactual that was never asked leaves
        // `Gap` at exactly zero, which reads identically to "the action is not in
        // the model" -- the two must not be confusable.
        if (Result.Steps > 2 && Result.Consequence.Asked == 0)
            wrong.Add("no consequence was ever predicted");

        // `Consequence.Differed` IS DELIBERATELY NOT COMPLAINED ABOUT, and that
        // was measured rather than decided. It fires on small graphs where the
        // top-ranked vision codes are the same whichever action is named --
        // knowing=0.900, counter=0.900, differed=0, with the action wired
        // correctly the whole time. It is reported and nothing more.
    }

    public override string ToString()
    {
        var report = new StringBuilder();

        report.Append(CultureInfo.InvariantCulture,
            $"steps={Result.Steps} alive={Result.Alive} ate={Result.Ate} ");
        report.Append(CultureInfo.InvariantCulture,
            $"silent={Silence:P0} chain={Result.ChosenByChain} echo={Result.EchoedLast} | ");
        report.Append(CultureInfo.InvariantCulture,
            $"nodes={Nodes} edges={Edges} spread=[{string.Join(",", Spread)}] | ");
        report.Append(CultureInfo.InvariantCulture,
            $"chains={{{Plumbing.Lengths}}} deepest={Deepest} | ");
        report.Append(CultureInfo.InvariantCulture,
            $"msgs={Messages} halted={Result.Halted} unbalanced={Unbalanced} "
            + $"unsettled={Unsettled} | ");
        report.Append(CultureInfo.InvariantCulture,
            $"foresaw={Result.Foresight.Precision:F3} blind={Result.Foresight.Blind:F3} ");
        report.Append(CultureInfo.InvariantCulture,
            $"novelGap={NoveltyGap:F4} | ");
        report.Append(CultureInfo.InvariantCulture,
            $"knowing={Result.Consequence.Knowing:F3} " +
            $"counter={Result.Consequence.Counterfactual:F3} " +
            $"actionGap={Result.Consequence.Gap:F4}");

        report.Append(Wrong);

        return report.ToString();
    }
}
