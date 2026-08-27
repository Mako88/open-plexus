namespace Plexus.Distributed;

/// <summary>
/// One question and every message that answers it.
/// </summary>
/// <remarks>
/// Every interaction is scoped to a round. A holder that reads fleet-wide current-moment
/// state instead of what the request carried is a holder that can answer one round with
/// another round's moment, which is the defect the existing implementation carries today.
/// </remarks>
public readonly record struct RoundId(Guid Value);
