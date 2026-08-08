using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenPlexus.Bus;

/// <summary>
/// What a message looks like once it has to leave the process.
/// </summary>
/// <remarks>
/// <para>
/// <b>NOTHING IN THIS PROJECT HAS EVER PRODUCED A BYTE, WHICH IS WHY THE DISTRIBUTED
/// CLAIM IS STILL A CLAIM.</b> <see cref="HybridBus"/> holds a dictionary of clusters and
/// calls them, injecting lateness and jitter and loss so that C2 and C3 are exercised —
/// but the delivery is a method call, so every constraint about a NETWORK is currently
/// being honoured by a simulation of one. Twenty phones cannot run a dictionary lookup
/// between them.
/// </para>
/// <para>
/// <b>THE ONE THING THIS HAS TO GET EXACTLY RIGHT IS DOUBLES, AND FORK 12 IS WHY.</b> A
/// reading is a QUANTISED number, so a value that comes back differing in its last bit
/// codes differently at a band boundary and becomes a different observation. That fault
/// has cost this project twice already — once from a graph's intra-op parallelism, once
/// from a transiently-zero live count — and a lossy wire format would be the third, with
/// the damage spread across machines where no single one could see it.
/// </para>
/// <para>
/// <b>System.Text.Json WRITES THE SHORTEST STRING THAT ROUND-TRIPS</b>, so a double
/// survives exactly rather than to fifteen places. That is a guarantee of the runtime
/// rather than of this file, so <c>WireTests</c> asserts it against the awkward values
/// instead of trusting it — infinities, subnormals, negative zero, and the epsilon
/// either side of a band edge.
/// </para>
/// <para>
/// <b>AND IT IS JSON RATHER THAN ANYTHING FASTER ON PURPOSE, FOR NOW.</b> The bytes on
/// this wire are a thought's routes, not a corpus; the profile that matters is
/// `Separations` and the search, and neither is here. A binary format is a change to make
/// when something measures the wire, which nothing yet does.
/// </para>
/// </remarks>
public static class Wire
{
    /// <summary>How everything on the wire is written and read.</summary>
    /// <remarks>
    /// <b>NO INDENTING AND NO CAMEL CASE, BECAUSE BOTH ENDS ARE THIS CODE.</b> A wire
    /// format that reads nicely is a wire format shaped for a human who is not there.
    /// </remarks>
    private static readonly JsonSerializerOptions Shape = new()
    {
        IncludeFields = false,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new Relation() },
    };

    /// <summary>
    /// <see cref="Graph.Kind"/>, whose entire state is private and would otherwise be
    /// written as nothing at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>WITHOUT THIS EVERY EDGE KIND ARRIVES AS <see langword="default"/>, SILENTLY.</b>
    /// A relation is a <see langword="readonly"/> <see langword="record"/>
    /// <see langword="struct"/> around one private number, so a serialiser looking for
    /// public members finds none, writes <c>{}</c>, and reads back a relation that
    /// matches nothing any machine has ever named. Nothing throws. The walk simply starts
    /// receiving messages whose kind is a relation that does not exist.
    /// </para>
    /// <para>
    /// <b>AND IT WAS FOUND BY THE TEST RATHER THAN BY THE FAULT</b>, which is the whole
    /// argument for writing the round trip down before writing the transport that needs
    /// it. Over a wire this is fork 12 with the two halves on different machines, where
    /// neither end can see that they disagree.
    /// </para>
    /// </remarks>
    private sealed class Relation : JsonConverter<Graph.Kind>
    {
        public override Graph.Kind Read(
            ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
            Graph.Kind.From(reader.GetUInt64());

        public override void Write(
            Utf8JsonWriter writer, Graph.Kind value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteNumberValue(value.Name);
        }
    }

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
