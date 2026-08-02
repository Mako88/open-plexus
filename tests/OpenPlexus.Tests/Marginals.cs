using OpenPlexus.Codes;
using OpenPlexus.Graph;

namespace OpenPlexus.Tests;

/// <summary>
/// Somebody else's marginals, stated outright.
/// </summary>
/// <remarks>
/// Stating them is the point. A partner's marginal is the number a node cannot
/// know for itself across machines — open fork 2 — so a test that had to build
/// real neighbours to get one would be testing the wrong thing.
/// </remarks>
internal sealed class Marginals : IMarginals
{
    private readonly Dictionary<Code, double> _seen = [];

    public Marginals Set(Code code, double seen)
    {
        _seen[code] = seen;
        return this;
    }

    public double SeenOf(Code code) => _seen.GetValueOrDefault(code);
}
