using Plexus.Core.Agency;
using Plexus.Core.Knowledge;
using Plexus.Core.Representation;

namespace Plexus.Worlds;

/// <summary>Which instance of a generated world this is.</summary>
public readonly record struct WorldSeed(int Value);

/// <summary>
/// Whatever the moments are being handed to.
/// </summary>
/// <remarks>
/// The world pushes. A brain that pulls has a request outstanding and therefore a deadline,
/// and a deadline the world did not set is a deadline the measurement did not intend.
/// </remarks>
public interface IMomentSink
{
    ValueTask ReceiveAsync(Observation observation, CancellationToken ct);
}

/// <summary>
/// A stream of moments and the actions that can be taken in it.
/// </summary>
/// <remarks>
/// <para>
/// The consequence of an action arrives on the sink like anything else, so nothing can tell
/// from the shape of the code whether a fact was observed or brought about. What says which
/// is <see cref="AcquisitionKind"/> on the moment.
/// </para>
/// <para>
/// The world reports its own observation domain. That is what lets an expectation of absence
/// be settled rather than assumed, and it is a property of the instrument rather than a
/// favour to the learner.
/// </para>
/// </remarks>
public interface IWorld
{
    WorldSeed Seed { get; }

    SourceId Source { get; }

    ObservationDomain Domain { get; }

    /// <summary>What can be done here, without saying what any of it means.</summary>
    ImmutableArray<Operator> Interventions { get; }

    ValueTask RunAsync(IMomentSink sink, CancellationToken ct);

    ValueTask ActAsync(PlannedAction action, IMomentSink sink, CancellationToken ct);
}

/// <summary>
/// The ground truth of a generated world, for the harness alone.
/// </summary>
/// <remarks>
/// A generated world knows what is true of it, which is what makes a claim about the learner
/// checkable. Nothing reachable from a brain may name this interface: a front end that can
/// see the answer is a front end that can hand it over.
/// </remarks>
public interface IEnumerableTruth
{
    ImmutableArray<GroundFact> Truth();

    ImmutableArray<Commitment> Laws();
}
