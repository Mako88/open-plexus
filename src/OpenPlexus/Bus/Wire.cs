using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenPlexus.Bus;

/// <summary>
/// What a message looks like once it has to leave the process.
/// </summary>
/// <remarks>
/// <para>
/// <b>What makes the distributed claim more than a claim.</b> <see cref="HybridBus"/>
/// holds a dictionary of holders and calls them, injecting lateness and jitter and loss so
/// that C2 and C3 are exercised — but the delivery is a method call, so every constraint
/// about a NETWORK would be honoured by a simulation of one. Twenty phones cannot run a
/// dictionary lookup between them, and this is the file that says what crosses instead.
/// </para>
/// <para>
/// <b>The one thing this has to get exactly right is doubles</b>, and fork 12 is why. A
/// reading is a QUANTISED number, so a value that comes back differing in its last bit
/// codes differently at a band boundary and becomes a different observation. That fault
/// has cost this project twice already — once from a graph's intra-op parallelism, once
/// from a transiently-zero live count — and a lossy wire format would be the third, with
/// the damage spread across machines where no single one could see it.
/// </para>
/// <para>
/// <b>System.Text.Json writes the shortest string that round-trips</b>, so a double
/// survives exactly rather than to fifteen places. That is a guarantee of the runtime
/// rather than of this file, so <c>WireTests</c> asserts it against the awkward values
/// instead of trusting it — infinities, subnormals, negative zero, and the epsilon
/// either side of a band edge.
/// </para>
/// <para>
/// <b>And it is JSON rather than anything faster on purpose, for now.</b> The bytes on
/// this wire are a moment and what a holder makes of it, not a corpus; the profile that
/// matters is `Separations` and the search, and neither is here. A binary format is a
/// change to make when something measures the wire, which nothing yet does.
/// </para>
/// </remarks>
public static class Wire
{
    /// <summary>How everything on the wire is written and read.</summary>
    /// <remarks>
    /// <b>No indenting and no camel case, because both ends are this code.</b> A wire
    /// format that reads nicely is a wire format shaped for a human who is not there.
    /// </remarks>
    private static readonly JsonSerializerOptions Shape = new()
    {
        IncludeFields = false,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    /// <summary>One message, as the bytes that carry it.</summary>
    /// <typeparam name="T">What is being sent.</typeparam>
    /// <param name="what">The message.</param>
    public static string Write<T>(T what) => JsonSerializer.Serialize(what, Shape);

    /// <summary>The message those bytes carry.</summary>
    /// <typeparam name="T">What is expected.</typeparam>
    /// <param name="bytes">What arrived.</param>
    /// <exception cref="JsonException">The bytes are not that message.</exception>
    /// <returns>The message.</returns>
    public static T Read<T>(string bytes) =>
        JsonSerializer.Deserialize<T>(bytes, Shape)
        ?? throw new JsonException($"a null {typeof(T).Name} arrived, which is not a message");
}
