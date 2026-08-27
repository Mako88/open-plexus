using Plexus.Core.Knowledge;

namespace Plexus.Engine.Cognition;

/// <summary>
/// Comparing a prediction against what happened.
/// </summary>
/// <remarks>
/// The three outcomes are not two and a gap. A prediction of absence over a relation the
/// observation did not report exhaustively settles as an abstention, which is why
/// <see cref="ObservationDomain"/> is on the observation rather than assumed.
/// </remarks>
public sealed class Settler : ISettler
{
    public Settlement Settle(Prediction prediction, Observation outcome) =>
        throw new NotImplementedException();
}
