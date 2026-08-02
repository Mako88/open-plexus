namespace OpenPlexus.Bus;

/// <summary>Where a cluster's envelopes are sent.</summary>
public readonly record struct ClusterAddress(string Value);

/// <summary>
/// Where a machine's arrivals and death reports are sent.
/// </summary>
/// <remarks>
/// <b>Machines carry the addresses; nodes do not.</b> A machine holds no edges
/// and is in no walk, which is why an arbitrary sensor or actuator can be
/// attached without the graph knowing what it is.
/// </remarks>
public readonly record struct MachineAddress(string Value);
