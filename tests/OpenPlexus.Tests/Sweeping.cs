using System.Globalization;
using System.Text;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// One arm of a sweep, measured over seeds.
/// </summary>
/// <remarks>
/// <b>The spread is not optional here</b>, and that is the point of the class.
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
    /// <para>
    /// <b>The number this project keeps working out by hand and getting wrong.</b>
    /// Two arms measured once each have no spread and are never reported as
    /// different, however far apart their single readings landed.
    /// </para>
    /// <para>
    /// <b>Zero spread over many seeds is the opposite case</b>, and returning 0 there
    /// was wrong. Arms that were measured repeatedly, never varied, and landed
    /// on different numbers — 1.0000 against 0.0000 — are <i>perfectly</i>
    /// separated, not indistinguishable. That reading forced at least one test to
    /// assert on bare means with a paragraph explaining why, which is a workaround
    /// for a defect rather than a finding.
    /// </para>
    /// </remarks>
    /// <returns>
    /// Standard errors between the means; <see cref="double.PositiveInfinity"/>
    /// when repeated measurement found no spread at all and the means still
    /// differ; 0 when there is not enough data to say anything.
    /// </returns>
    public double Separation(Measured other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var apart = Math.Abs(Mean - other.Mean);
        var spread = Math.Sqrt((StdErr * StdErr) + (other.StdErr * other.StdErr));

        if (spread > 0.0) return apart / spread;

        // Not enough data to have a spread at all. One reading apiece says
        // nothing about whether a second pair would land the same way.
        if (Seeds < 2 || other.Seeds < 2) return 0.0;

        // Measured repeatedly and never varied. Identical means are identical;
        // different means are as separated as anything can be.
        return apart == 0.0 ? 0.0 : double.PositiveInfinity;
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
    /// <summary>
    /// What a sweep's seeds are for, as far as <see cref="Seeds.Apart"/> is
    /// concerned. Arbitrary, and fixed forever — changing it renumbers every
    /// measurement in the project.
    /// </summary>
    private const uint Purpose = 0x5EED_0001;

    /// <summary>The standard error of a column, for a grid that puts a bar on one.</summary>
    /// <param name="read">One value a seed.</param>
    /// <remarks>
    /// <see cref="Spread"/>'s own arithmetic, reached rather than re-derived, because a bar
    /// read against a spread computed twice is a bar against whichever definition its author
    /// assumed. That is one of this repo's own traps and it has fired here before.
    /// </remarks>
    public static double Error(IReadOnlyList<double> read)
    {
        ArgumentNullException.ThrowIfNull(read);

        return new Measured { Arm = string.Empty, Values = [.. read] }.StdErr;
    }

    /// <summary>
    /// A column of per-seed readings as <c>mean +/- standard error</c>, for the grids that
    /// print themselves rather than going through <see cref="Table"/>.
    /// </summary>
    /// <param name="read">One value a seed.</param>
    /// <param name="format">How to render both numbers.</param>
    /// <remarks>
    /// <b>Here because it was written by hand three times</b>, and `DuplicationTests` refused
    /// the third. Not every grid fits <see cref="AcrossAsync"/> — some cross two axes, some
    /// print a curve along a row — but every one of them still owes a spread, and three
    /// private copies of a standard error is three chances for one grid's bars to mean
    /// something different from the grid it is read against. <see cref="Measured"/> already
    /// computes both; this only formats them.
    /// </remarks>
    public static string Spread(IReadOnlyList<double> read, string format = "F3")
    {
        ArgumentNullException.ThrowIfNull(read);

        var measured = new Measured { Arm = string.Empty, Values = [.. read] };

        return $"{measured.Mean.ToString(format, CultureInfo.InvariantCulture)} "
            + $"+/-{measured.StdErr.ToString(format, CultureInfo.InvariantCulture)}";
    }

    /// <summary>
    /// Runs one arm across <paramref name="seeds"/> seeds.
    /// </summary>
    /// <remarks>
    /// <b>The counter is mixed before it reaches the run</b>, and that is not
    /// cosmetic. This used to hand out 1, 2, 3… directly, and .NET's seeded
    /// <see cref="Random"/> gives near-neighbour seeds streams that agree with
    /// each other far more than chance allows. <see cref="Measured.StdErr"/> is
    /// computed across exactly these seeds, so that agreement came straight off
    /// the standard error and made every arm look more significant than it was.
    /// See <see cref="Seeds.Apart"/> for the measurement.
    /// <para>
    /// <b>Every arm still gets the same sequence</b>, because the mixing is a
    /// pure function of the index — so arms remain paired seed for seed, which
    /// is what makes a control a control.
    /// </para>
    /// </remarks>
    public static async Task<Measured> ArmAsync(
        string arm, int seeds, Func<int, Task<double>> run)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(arm);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seeds);
        ArgumentNullException.ThrowIfNull(run);

        var values = new List<double>(seeds);

        for (var index = 1; index <= seeds; index++)
            values.Add(await run(Seeds.Apart(index, Purpose)).ConfigureAwait(false));

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
