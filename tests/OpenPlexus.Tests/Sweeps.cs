namespace OpenPlexus.Tests;

/// <summary>
/// The mark on a fact that MEASURES rather than checks, so CI can leave it alone.
/// </summary>
/// <remarks>
/// <para>
/// <b>A sweep asserts nothing and that is the point of it.</b> A threshold written
/// before the first run is a prediction dressed as a check, so the grids in
/// `ArrangingTests` and `SharpeningTests` print their numbers and pass unconditionally.
/// The findings then live in the commit that produced them, which is this repo's rule.
/// </para>
/// <para>
/// <b>And they took the suite past its forty-five minute ceiling</b> in one session.
/// Between them they are several hundred runs of twenty and thirty thousand rounds, all
/// serial because `Parallelism` requires it — so leaving them in the default filter
/// spends most of an hour of somebody else's machine producing output nobody reads, and
/// then kills the run that would have reported the real failures. That is exactly the
/// shape the workflow's own comment about cancelled runs warns about.
/// </para>
/// <para>
/// <b>They are excluded rather than deleted, and they still compile.</b> A measurement
/// nobody can re-run is a number nobody can check, so the harness stays and the build
/// keeps it honest — a `cref` that rots or a call site that stops existing still fails.
/// What it does not do is run unasked. `dotnet test --filter "kind=sweep"` is how to
/// take the measurement again.
/// </para>
/// </remarks>
public static class Sweeps
{
    /// <summary>The trait a measurement carries.</summary>
    public const string Kind = "kind";

    /// <summary>What that trait says.</summary>
    public const string Name = "sweep";
}
