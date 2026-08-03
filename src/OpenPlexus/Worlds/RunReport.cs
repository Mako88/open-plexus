using System.Globalization;
using System.Text;

namespace OpenPlexus.Worlds;

/// <summary>
/// Everything a run can say about itself, printed at the end of it.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's ask, 2026-08-02, and the reason is the failure this project keeps
/// having:</b> a number is swept, it barely moves, and much later it turns out
/// something was not wired the way anyone thought. A run that reports its own
/// plumbing makes that visible on the spot instead of after the conclusions.
/// </para>
/// <para>
/// <b><see cref="Complaints"/> is the part that matters.</b> Every entry is a
/// quantity that would be out of range only if something had come unwired, and
/// a test asserts the list is empty. Adding a number here without a range says
/// nothing; the range is the check.
/// </para>
/// </remarks>
public sealed record RunReport
{
    public required RunResult Result { get; init; }

    /// <summary>Nodes across every cluster.</summary>
    public required int Nodes { get; init; }

    /// <summary>Partner entries across every node — the graph's size.</summary>
    public required int Edges { get; init; }

    /// <summary>How many nodes each cluster holds.</summary>
    public required IReadOnlyList<int> Spread { get; init; }

    /// <summary>
    /// How long the chains that came back were, by length.
    /// </summary>
    /// <remarks>
    /// <b>The single most revealing line.</b> A walk that only ever produces
    /// chains of length two is a one-hop lookup wearing the name of a flood,
    /// and nothing else in the report would show it.
    /// </remarks>
    public required IReadOnlyDictionary<int, int> ChainLengths { get; init; }

    /// <summary>The share of steps that produced no onset and so no thought.</summary>
    public double Silence => Result.Steps == 0 ? 0.0 : Result.Silent / (double)Result.Steps;

    /// <summary>How far a route actually walked, at most.</summary>
    public int Deepest => ChainLengths.Count == 0 ? 0 : ChainLengths.Keys.Max();

    /// <summary>What the graph foresaw of what CHANGED, over a blind draw.</summary>
    public double NoveltyGap => Result.Novelty.Precision - Result.Novelty.Blind;

    /// <summary>
    /// Everything that is out of the range it would be in if the system were
    /// wired the way it is supposed to be.
    /// </summary>
    public IReadOnlyList<string> Complaints
    {
        get
        {
            var wrong = new List<string>();

            if (Result.Steps == 0) wrong.Add("the run took no steps");
            if (Nodes == 0) wrong.Add("no node ever came into existence");
            if (Edges == 0) wrong.Add("no connection was ever formed");
            if (Result.Messages == 0) wrong.Add("the bus carried nothing");
            if (Result.Unbalanced > 0) wrong.Add($"{Result.Unbalanced} thoughts did not balance");

            // A route that never leaves its origin means the walk is not
            // walking, and every result would be about one-hop association.
            if (Deepest < 2) wrong.Add($"no route walked at all — deepest chain {Deepest}");

            if (Result.Silent >= Result.Steps) wrong.Add("every step was silent");
            if (Spread.Count(count => count > 0) < 2) wrong.Add("every node landed on one cluster");
            if (Result.Steps > 2 && Result.Novelty.Asked == 0)
                wrong.Add("nothing was ever predicted");

            // GUARDED BY SAMPLE SIZE, because a short run is not a broken one.
            // A three-step run genuinely has almost nothing to predict, and a
            // check that fires on that trains everyone to ignore the list.
            if (Result.Novelty.Guessed > 50 && Result.Novelty.Chance == 0)
                wrong.Add("the blind control never scored, so the gap means nothing");

            // FORK 18'S WIRING CHECK. A counterfactual that was never asked
            // leaves `Gap` at exactly zero, which reads identically to "the
            // action is not in the model" -- the two must not be confusable.
            if (Result.Steps > 2 && Result.Consequence.Asked == 0)
                wrong.Add("no consequence was ever predicted");

            return wrong;
        }
    }

    public override string ToString()
    {
        var report = new StringBuilder();
        var lengths = string.Join(" ", ChainLengths.OrderBy(e => e.Key).Select(e => $"{e.Key}:{e.Value}"));

        report.Append(CultureInfo.InvariantCulture,
            $"steps={Result.Steps} alive={Result.Alive} ate={Result.Ate} ");
        report.Append(CultureInfo.InvariantCulture,
            $"silent={Silence:P0} chain={Result.ChosenByChain} echo={Result.EchoedLast} | ");
        report.Append(CultureInfo.InvariantCulture,
            $"nodes={Nodes} edges={Edges} spread=[{string.Join(",", Spread)}] | ");
        report.Append(CultureInfo.InvariantCulture,
            $"chains={{{lengths}}} deepest={Deepest} | ");
        report.Append(CultureInfo.InvariantCulture,
            $"msgs={Result.Messages} halted={Result.Halted} unbalanced={Result.Unbalanced} | ");
        report.Append(CultureInfo.InvariantCulture,
            $"foresaw={Result.Foresight.Precision:F3} blind={Result.Foresight.Blind:F3} ");
        report.Append(CultureInfo.InvariantCulture,
            $"novelGap={NoveltyGap:F4} | ");
        report.Append(CultureInfo.InvariantCulture,
            $"knowing={Result.Consequence.Knowing:F3} " +
            $"counter={Result.Consequence.Counterfactual:F3} " +
            $"actionGap={Result.Consequence.Gap:F4}");

        if (Complaints.Count > 0)
            report.Append(CultureInfo.InvariantCulture, $" || WRONG: {string.Join("; ", Complaints)}");

        return report.ToString();
    }
}
