using Plexus.Core.Agency;
using Plexus.Core.Cognition;

namespace Plexus.Engine.Agency;

/// <summary>
/// Paying for an answer only where the answer changes what happens next.
/// </summary>
/// <remarks>
/// The estimate is the expected reduction in decision loss, less the intervention cost, less
/// the delay cost. A surprising observation that no pending decision turns on scores nothing
/// here by construction.
/// </remarks>
public sealed class ValueOfInformationInquiry : IInquiryPolicy
{
    public Experiment? Choose(
        WorkingCoalition coalition,
        IReadOnlyCollection<Hypothesis> liveHypotheses,
        IReadOnlyCollection<Operator> availableInterventions) =>
        throw new NotImplementedException();
}

/// <summary>The control that asks, but not for a reason.</summary>
public sealed class RandomInquiry(int seed) : IInquiryPolicy
{
    private readonly int _seed = seed;

    public Experiment? Choose(
        WorkingCoalition coalition,
        IReadOnlyCollection<Hypothesis> liveHypotheses,
        IReadOnlyCollection<Operator> availableInterventions) =>
        throw new NotImplementedException();
}

/// <summary>
/// The control that chases surprise.
/// </summary>
/// <remarks>
/// It separates wanting to know from wanting to be less surprised, and the two come apart
/// exactly where an irrelevant novelty is the most surprising thing available.
/// </remarks>
public sealed class SurpriseSeekingInquiry : IInquiryPolicy
{
    public Experiment? Choose(
        WorkingCoalition coalition,
        IReadOnlyCollection<Hypothesis> liveHypotheses,
        IReadOnlyCollection<Operator> availableInterventions) =>
        throw new NotImplementedException();
}

/// <summary>The floor: never pay for information.</summary>
public sealed class NeverAsk : IInquiryPolicy
{
    public Experiment? Choose(
        WorkingCoalition coalition,
        IReadOnlyCollection<Hypothesis> liveHypotheses,
        IReadOnlyCollection<Operator> availableInterventions) =>
        throw new NotImplementedException();
}
