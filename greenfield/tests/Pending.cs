namespace Plexus.Testing;

/// <summary>
/// A claim nobody has built yet, written as a test that fails until somebody does.
/// </summary>
/// <remarks>
/// <para>
/// An empty <c>[Fact]</c> passes. A suite of them reports green for a set of questions it
/// never asked, and the next session reads that green as evidence that nothing is owed.
/// </para>
/// <para>
/// So every claim in the skeleton is red on purpose and says what would close it. This is the
/// increment-0 contract freeze: the tests compile against the proposed public surface and
/// fail for the mechanisms that are genuinely missing.
/// </para>
/// </remarks>
public static class Pending
{
    public static void Claim(string what) =>
        Assert.Fail($"pending: {what}");
}
