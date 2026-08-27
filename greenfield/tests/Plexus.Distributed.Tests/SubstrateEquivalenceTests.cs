using Plexus.Engine;
using Plexus.Worlds;

namespace Plexus.Distributed.Tests;

/// <summary>
/// One holder and a one-holder fleet, on every dial and every seeded world.
/// </summary>
/// <remarks>
/// <para>
/// The comparison is over ordered semantic output. Diagnostic timestamps and transport
/// identifiers are normalised away; semantic ordering, prediction identity, evidence and
/// abstention are not, because those are the output.
/// </para>
/// <para>
/// It is a grid over dials rather than one run because the defect it exists to catch is a
/// dial that the local path passes on and the distributed path drops. That has already
/// happened once in the existing implementation, on the dial that decides whether a
/// commitment may be believed without being grounded.
/// </para>
/// </remarks>
public sealed class SubstrateEquivalenceTests
{
    /// <summary>
    /// Every dial crossed with every seed, which is what makes this a grid.
    /// </summary>
    /// <remarks>
    /// It is empty until the engine can run at all, and empty theory data is a test that
    /// cannot fail. The companion fact below is what keeps that from reading as green.
    /// </remarks>
    public static TheoryData<EngineSettings, WorldSeed> AllDialsAndSeededWorlds() => new();

    [Theory]
    [MemberData(nameof(AllDialsAndSeededWorlds))]
    public void One_holder_and_a_one_holder_fleet_are_semantically_identical(
        EngineSettings settings,
        WorldSeed seed)
    {
        Assert.NotNull(settings);
        Assert.NotEqual(0, seed.Value);

        Pending.Claim("LocalEngine and DistributedEngine over one holder");
    }

    [Fact]
    public void The_grid_covers_every_dial_and_is_not_empty() =>
        Pending.Claim(
            "the grid itself. A theory with no data passes, so the count of cases is asserted "
            + "against the product of the dial values and the seed list");

    [Fact]
    public void A_dial_the_local_path_honours_and_the_fleet_drops_fails_this() =>
        Pending.Claim(
            "the companion: invert one dial on the distributed path only and show the "
            + "equivalence reading goes red, or it was never comparing the dials at all");

    [Fact]
    public void Normalisation_does_not_reach_the_semantic_output() =>
        Pending.Claim(
            "the check on the normaliser: strip a prediction identity and this must fail, "
            + "because a normaliser that hides a difference makes the equivalence vacuous");
}
