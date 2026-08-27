using Plexus.Core.Knowledge;

namespace Plexus.Engine.Learning;

/// <summary>
/// Adding one discriminating pattern to a commitment that is sometimes wrong.
/// </summary>
/// <remarks>
/// The repaired commitment takes its identity from its own scope and expectation, never from
/// the identity of what it was repaired out of plus the added condition. Two holders that
/// reach the same rule with the same condition by different routes must land on one identity,
/// or the evidence for one rule is spread over two.
/// </remarks>
public interface IRepairer
{
    IEnumerable<Commitment> ProposeRepairs(
        Commitment parent,
        IReadOnlyCollection<Observation> supporting,
        IReadOnlyCollection<Observation> contradicting);
}

/// <summary>
/// One condition at a time, priced against the whole candidate family.
/// </summary>
/// <remarks>
/// A repair that pays only for its own optional look while hundreds of other candidates are
/// tried the same round is not a valid bar, which is why the accounting sits behind
/// <see cref="IEvidencePolicy"/> rather than in here.
/// </remarks>
public sealed class OneConditionRepairer(IEvidencePolicy policy) : IRepairer
{
    private readonly IEvidencePolicy _policy = policy;

    public IEnumerable<Commitment> ProposeRepairs(
        Commitment parent,
        IReadOnlyCollection<Observation> supporting,
        IReadOnlyCollection<Observation> contradicting) =>
        throw new NotImplementedException();
}
