using System.Globalization;
using System.Text;

namespace OpenPlexus.Tests;

/// <summary>
/// One arm of a sweep, measured over seeds.
/// </summary>
/// <remarks>
/// <b>THE SPREAD IS NOT OPTIONAL HERE, AND THAT IS THE POINT OF THE CLASS.</b>
/// Every sweep in this project's history that reported a bare mean has had to be
/// retracted or hedged later — "chain loses to repeat" at 30 seeds survived to
/// 200 as "indistinguishable", and a fork-21 table went into the docs with
/// "spread not computed" written across it. A harness that cannot report a mean
/// without its standard error cannot make that mistake again.
/// </remarks>
public sealed record Measured
{
    public required string Arm { get; init; }

    /// <summary>Every seed's value, kept so a claim can be re-derived.</summary>
    public required IReadOnlyList<double> Values { get; init; }

    public int Seeds => Values.Count;

    public double Mean => Values.Count == 0 ? 0.0 : Values.Average();

    /// <summary>
    /// The standard error of the mean. <b>Zero seeds and one seed both give
    /// zero</b>, which is honest: a single measurement has no spread, and the
    /// separation test below will refuse to call anything significant.
    /// </summary>
    public double StdErr
    {
        get
        {
            if (Values.Count < 2) return 0.0;

            var mean = Mean;
            var variance = Values.Sum(one => (one - mean) * (one - mean)) / (Values.Count - 1);
            return Math.Sqrt(variance / Values.Count);
        }
    }

    /// <summary>
    /// How many standard errors separate this arm from another.
    /// </summary>
    /// <remarks>
    /// <b>The number this project keeps working out by hand and getting wrong.</b>
    /// Returns 0 when neither arm has any spread to speak of, so two arms
    /// measured once each are never reported as different.
    /// </remarks>
    public double Separation(Measured other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var spread = Math.Sqrt((StdErr * StdErr) + (other.StdErr * other.StdErr));
        return spread <= 0.0 ? 0.0 : Math.Abs(Mean - other.Mean) / spread;
    }

    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"{Arm}: {Mean:F4} +-{StdErr:F4} (n={Seeds})");
}

/// <summary>
/// The standard sweep harness. <b>John's ask, 2026-08-02.</b>
/// </summary>
/// <remarks>
/// <para>
/// Every measurement in this project was previously a throwaway test file that
/// printed a line and was deleted, which meant the seed loop, the averaging and
/// the spread were re-written each time — and the spread was usually the part
/// that got dropped.
/// </para>
/// <para>
/// <b>Arms run in seed order and never share a generator.</b> A control that
/// draws from the same sequence as the arm it is controlling is not a control:
/// adding a measurement to one arm moves the other arm's trajectory, which has
/// already happened here once.
/// </para>
/// </remarks>
public static class Sweep
{
    /// <summary>Runs one arm across seeds 1..<paramref name="seeds"/>.</summary>
    public static async Task<Measured> ArmAsync(
        string arm, int seeds, Func<int, Task<double>> run)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(arm);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seeds);
        ArgumentNullException.ThrowIfNull(run);

        var values = new List<double>(seeds);

        for (var seed = 1; seed <= seeds; seed++)
            values.Add(await run(seed).ConfigureAwait(false));

        return new Measured { Arm = arm, Values = values };
    }

    /// <summary>Runs every arm across the same seeds.</summary>
    public static async Task<IReadOnlyList<Measured>> AcrossAsync(
        int seeds, params (string Arm, Func<int, Task<double>> Run)[] arms)
    {
        ArgumentNullException.ThrowIfNull(arms);

        var measured = new List<Measured>(arms.Length);

        foreach (var (arm, run) in arms)
            measured.Add(await ArmAsync(arm, seeds, run).ConfigureAwait(false));

        return measured;
    }

    /// <summary>
    /// A markdown table, ready to paste into the architecture doc.
    /// </summary>
    /// <remarks>
    /// <b>Separations are against the first arm</b>, which is the convention the
    /// docs already use: the first row is the control or the current default,
    /// and every other row says how far from it it landed.
    /// </remarks>
    public static string Table(IReadOnlyList<Measured> arms)
    {
        ArgumentNullException.ThrowIfNull(arms);
        if (arms.Count == 0) return "(no arms)";

        var against = arms[0];
        var table = new StringBuilder();

        table.AppendLine(CultureInfo.InvariantCulture,
            $"| arm | mean | stderr | seeds | sigma vs {against.Arm} |");
        table.AppendLine("|---|---|---|---|---|");

        foreach (var arm in arms)
        {
            var sigma = ReferenceEquals(arm, against)
                ? "—"
                : arm.Separation(against).ToString("F1", CultureInfo.InvariantCulture);

            table.AppendLine(CultureInfo.InvariantCulture,
                $"| {arm.Arm} | {arm.Mean:F4} | {arm.StdErr:F4} | {arm.Seeds} | {sigma} |");
        }

        return table.ToString();
    }
}
