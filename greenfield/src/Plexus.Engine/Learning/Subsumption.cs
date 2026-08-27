using Plexus.Core.Knowledge;

namespace Plexus.Engine.Learning;

/// <summary>How two commitments stand to each other.</summary>
public enum SubsumptionResult
{
    Unrelated,
    LeftCoversRight,
    RightCoversLeft,
    Equivalent,
}

/// <summary>
/// Keeping the more general rule where the evidence cannot separate them.
/// </summary>
/// <remarks>
/// The general one is kept only where it covers strictly more bindings and the predictive
/// evidence is indistinguishable. Where the evidence does separate them, both stay.
/// </remarks>
public interface ISubsumption
{
    SubsumptionResult Compare(Commitment left, Commitment right);
}

public sealed class BindingCoverageSubsumption : ISubsumption
{
    public SubsumptionResult Compare(Commitment left, Commitment right) =>
        throw new NotImplementedException();
}
