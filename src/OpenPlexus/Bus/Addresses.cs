namespace OpenPlexus.Bus;

/// <summary>
/// Where a machine's asks and answers are sent.
/// </summary>
/// <remarks>
/// <b>THE ONLY ADDRESS THERE IS, AND THAT IS C1 BEING STRUCTURAL RATHER THAN A
/// SIMPLIFICATION.</b> Nothing on this bus is addressed more finely than a machine, so
/// nothing can name another machine's commitment — a holder is asked what it makes of a
/// moment and answers in its own words, which is the only thing anyone is ever told.
/// </remarks>
public readonly record struct MachineAddress(string Value);
