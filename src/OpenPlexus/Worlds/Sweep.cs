using System.Globalization;
using OpenPlexus.Graph;

namespace OpenPlexus.Worlds;

/// <summary>One cell of a sweep: the arm, and what it did.</summary>
public sealed record SweepRow
{
    public required int Horizon { get; init; }

    public required bool IncludeEmpty { get; init; }

    public required StepCost Cost { get; init; }

    public required int Seed { get; init; }

    public required RunResult Result { get; init; }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture,
        $"horizon={Horizon} empty={IncludeEmpty} cost={Cost} seed={Seed} " +
        $"steps={Result.Steps} chain={Result.ChosenByChain} nothing={Result.ReachedNothing} " +
        $"silent={Result.Silent} halted={Result.Halted} nodes={Result.Nodes} ate={Result.Ate}");
}

/// <summary>
/// Runs the arms that already exist and reports what they cost.
/// </summary>
/// <remarks>
/// <para>
/// <b>This decides nothing.</b> Every dial it moves is one the design already
/// has more than one answer for, and the point is to put numbers under open
/// fork 8 rather than to pick a side of it. A constant that never changes looks
/// like the background; this is what makes `Horizon` stop being one.
/// </para>
/// <para>
/// <b>Read `Steps` before anything else.</b> Random play dies in about five
/// steps, because the four actions are absolute directions and reversing into
/// the neck is instantly fatal — so most of these runs end almost immediately
/// and carry very little experience. Any comparison across arms is a comparison
/// of very short runs.
/// </para>
/// </remarks>
public static class Sweep
{
    /// <summary>Runs one arm.</summary>
    public static async Task<SweepRow> OnceAsync(
        int horizon,
        bool includeEmpty,
        StepCost cost,
        int seed,
        int steps,
        bool relative = true,
        CancellationToken ct = default)
    {
        var world = new SnakeSettings
        {
            Width = 15,
            Height = 15,
            Sight = 1,
            Relative = relative,
            StartingEnergy = 60.0,
            EnergyPerStep = 1.0,
            EnergyPerFood = 30.0,
        };

        var dials = new WalkSettings
        {
            Stamina = 4.0,
            Cost = cost,
            Charge = cost == StepCost.Constant ? 0.25 : 0.0,
            Refuel = Refuel.Strength,
            Value = ArrivalValue.Strength,
            Accumulate = Accumulate.Sum,
            Horizon = horizon,
        };

        using var run = new SnakeRun(world, dials, seed, includeEmpty: includeEmpty);

        return new SweepRow
        {
            Horizon = horizon,
            IncludeEmpty = includeEmpty,
            Cost = cost,
            Seed = seed,
            Result = await run.PlayAsync(steps, ct: ct).ConfigureAwait(false),
        };
    }

    /// <summary>Runs a grid, one arm at a time.</summary>
    /// <remarks>
    /// Serial on purpose. These runs are dominated by thread-pool work inside
    /// the bus, so running arms concurrently would make each one's timing a
    /// measurement of the others.
    /// </remarks>
    public static async Task<IReadOnlyList<SweepRow>> GridAsync(
        IReadOnlyCollection<int> horizons,
        IReadOnlyCollection<bool> includeEmpty,
        IReadOnlyCollection<StepCost> costs,
        IReadOnlyCollection<int> seeds,
        int steps,
        bool relative = true,
        CancellationToken ct = default)
    {
        var rows = new List<SweepRow>();

        foreach (var cost in costs)
            foreach (var empty in includeEmpty)
                foreach (var horizon in horizons)
                    foreach (var seed in seeds)
                        rows.Add(await OnceAsync(horizon, empty, cost, seed, steps, relative, ct)
                            .ConfigureAwait(false));

        return rows;
    }
}
