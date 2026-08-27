using Plexus.Core.Agency;
using Plexus.Core.Knowledge;
using Plexus.Core.Representation;

namespace Plexus.Worlds;

/// <summary>
/// How big this instance of the world is.
/// </summary>
/// <remarks>
/// Sizes rather than switches. Every field is a count, so making the world less like the real
/// one is not something this record can express: there is no flag here that turns a
/// distinction off, only a number that says how many of something there are.
/// </remarks>
public sealed record WorldSize
{
    public required int Entities { get; init; }

    public required int Locations { get; init; }

    public required int Containers { get; init; }

    /// <summary>How many attributes an entity carries that nothing depends on.</summary>
    /// <remarks>
    /// The irrelevant attribute is what identity has to survive, so it is a count and not a
    /// mode. A world with nought of them cannot ask the question.
    /// </remarks>
    public required int IrrelevantAttributes { get; init; }

    /// <summary>How many unobserved variables the consequence depends on.</summary>
    public required int HiddenRegimes { get; init; }
}

/// <summary>
/// One continuing world small enough to enumerate and rich enough to run the whole loop.
/// </summary>
/// <remarks>
/// <para>
/// The learner is told no human label for anything here. Entities, locations, the container
/// relation and the move action are stable anonymous codes, and their meaning is whatever the
/// facts they take part in support.
/// </para>
/// <para>
/// The sequence the world is built to produce: entities occupy locations; one entity changes
/// an irrelevant attribute and stays the same entity; the same relation arrives with novel
/// fillers and permuted roles; a relevant fact leaves working capacity and has to be
/// retrieved by structural key; passive observation supports two rival causal explanations;
/// one available intervention separates them; the outcome settles the causal commitment; a
/// holder disappears and rejoins while evidence goes on converging.
/// </para>
/// <para>
/// Each of those needs an isolating control. One score over the whole sequence says a machine
/// did well and never says which mechanism did it.
/// </para>
/// </remarks>
public sealed class RelationalInterventionWorld(WorldSeed seed, WorldSize size)
    : IWorld, IEnumerableTruth
{
    private readonly WorldSize _size = size;

    public WorldSeed Seed { get; } = seed;

    public SourceId Source => throw new NotImplementedException();

    public ObservationDomain Domain => throw new NotImplementedException();

    public ImmutableArray<Operator> Interventions => throw new NotImplementedException();

    public ValueTask RunAsync(IMomentSink sink, CancellationToken ct) =>
        throw new NotImplementedException();

    public ValueTask ActAsync(PlannedAction action, IMomentSink sink, CancellationToken ct) =>
        throw new NotImplementedException();

    public ImmutableArray<GroundFact> Truth() => throw new NotImplementedException();

    public ImmutableArray<Commitment> Laws() => throw new NotImplementedException();
}
