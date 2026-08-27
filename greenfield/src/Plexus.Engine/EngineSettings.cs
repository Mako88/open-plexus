using Plexus.Core.Cognition;
using Plexus.Core.Memory;

namespace Plexus.Engine;

/// <summary>
/// Every dial one brain runs under.
/// </summary>
/// <remarks>
/// Required and without defaults, deliberately. A dial with a default is a dial a caller can
/// forget to pass, and a caller that forgets is running a different brain from the one being
/// measured while reading as though it were the same.
/// </remarks>
public sealed record EngineSettings
{
    public required ResourceBudget Round { get; init; }

    public required RetrievalBudget Retrieval { get; init; }

    public required Retrieving Retrieving { get; init; }

    public required Asking Asking { get; init; }
}

/// <summary>Which retriever is in the loop, controls included.</summary>
/// <remarks>
/// The controls are dials rather than test doubles so that a comparison runs the same code
/// path with one thing changed.
/// </remarks>
public enum Retrieving
{
    Nothing,
    Randomly,
    Structurally,
}

/// <summary>Which inquiry policy is in the loop, controls included.</summary>
public enum Asking
{
    Never,
    Randomly,
    BySurprise,
    ByValueOfInformation,
}
