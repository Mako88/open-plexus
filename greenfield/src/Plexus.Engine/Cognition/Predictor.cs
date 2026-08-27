using Plexus.Core.Cognition;
using Plexus.Core.Knowledge;
using Plexus.Core.Representation;

namespace Plexus.Engine.Cognition;

/// <summary>
/// Matching commitments against the moment and issuing grounded expectations.
/// </summary>
/// <remarks>
/// One prediction is one commitment at one opportunity, so a commitment that matches three
/// ways issues three predictions and is settled three times.
/// </remarks>
public sealed class Predictor(IUnifier unifier) : IPredictor
{
    private readonly IUnifier _unifier = unifier;

    public ImmutableArray<Prediction> Predict(WorkingCoalition coalition) =>
        throw new NotImplementedException();
}
