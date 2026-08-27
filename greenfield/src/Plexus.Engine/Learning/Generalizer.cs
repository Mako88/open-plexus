using Plexus.Core.Knowledge;

namespace Plexus.Engine.Learning;

/// <summary>
/// Dropping one scope pattern at a time.
/// </summary>
/// <remarks>
/// A generalisation clears the evidence policy like anything else. Widening a rule that
/// nothing has tested wider is how a population fills with claims nobody could refute.
/// </remarks>
public interface IGeneralizer
{
    IEnumerable<Commitment> ProposeGeneralizations(Commitment commitment);
}

public sealed class DropOnePatternGeneralizer(IEvidencePolicy policy) : IGeneralizer
{
    private readonly IEvidencePolicy _policy = policy;

    public IEnumerable<Commitment> ProposeGeneralizations(Commitment commitment) =>
        throw new NotImplementedException();
}
