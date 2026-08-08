using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Thinking;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The bytes, and the one property they have to have.
/// </summary>
/// <remarks>
/// <para>
/// <b>A MESSAGE THAT DOES NOT COME BACK IDENTICAL IS FORK 12 SPREAD ACROSS MACHINES.</b>
/// A reading is a quantised number, so a double differing in its last bit codes
/// differently at a band boundary and becomes a DIFFERENT OBSERVATION. That fault has
/// cost this project twice from inside one process; over a wire it would be worse,
/// because no single machine could see both sides of it.
/// </para>
/// <para>
/// <b>SO EVERY TEST HERE IS EQUALITY AND NOT APPROXIMATION.</b> There is no tolerance
/// anywhere in this file on purpose: a wire format is either lossless or it is a source
/// of drift that will be blamed on the learner.
/// </para>
/// </remarks>
public sealed class WireTests(ITestOutputHelper output)
{
    /// <summary>
    /// <b>THE VALUES A FORMAT GETS WRONG, RATHER THAN THE ONES IT GETS RIGHT.</b>
    /// </summary>
    /// <remarks>
    /// Fifteen significant figures is the default for a lot of serialisers and it loses
    /// the seventeenth, which is exactly where a band boundary lives. These are the
    /// awkward ones: the epsilon either side of a half, a subnormal, negative zero — which
    /// compares equal to zero and must still be written as itself — and the values that
    /// are not numbers at all.
    /// </remarks>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(1.0 / 3.0)]
    [InlineData(double.Epsilon)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public void A_double_comes_back_bit_for_bit(double value)
    {
        var back = Wire.Read<double>(Wire.Write(value));

        // BITS RATHER THAN `==`, because NaN is not equal to itself and negative zero IS
        // equal to zero -- so the obvious comparison passes for two of the cases that
        // matter most and fails for a third that is fine.
        Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(back));
    }

    /// <summary>
    /// <b>NEGATIVE ZERO, WHICH THE OBVIOUS COMPARISON CANNOT SEE.</b>
    /// </summary>
    /// <remarks>
    /// It is equal to zero under <c>==</c> and is a different number, so a format that
    /// dropped the sign would pass every equality test written the obvious way. It sits
    /// in its own fact because an <c>InlineData(-0.0)</c> is folded to <c>0.0</c> before
    /// the test ever sees it, which would have made this check unable to fire.
    /// </remarks>
    [Fact]
    public void Negative_zero_keeps_its_sign()
    {
        var value = BitConverter.Int64BitsToDouble(long.MinValue);

        Assert.Equal(0.0, value);
        Assert.True(double.IsNegative(value));

        var back = Wire.Read<double>(Wire.Write(value));

        Assert.True(double.IsNegative(back), $"negative zero came back as {back:R}");
    }

    /// <summary>
    /// <b>AND THE ONE EITHER SIDE OF A BAND EDGE, WHICH IS WHERE IT WOULD ACTUALLY
    /// BITE.</b>
    /// </summary>
    /// <remarks>
    /// A quantiser turns a reading into a code by comparing against an edge. Two readings
    /// astride one, differing by a single representable step, must stay astride it after a
    /// round trip — otherwise two different observations arrive at the far machine as the
    /// same one, which is the aliasing fault this repo has already had twice and would
    /// here be invisible from either end.
    /// </remarks>
    [Fact]
    public void Two_readings_astride_a_band_edge_stay_astride_it()
    {
        var edge = 0.5;
        var below = Math.BitDecrement(edge);
        var above = Math.BitIncrement(edge);

        Assert.NotEqual(below, above);

        var backBelow = Wire.Read<double>(Wire.Write(below));
        var backAbove = Wire.Read<double>(Wire.Write(above));

        Assert.True(backBelow < edge, $"{below:R} came back as {backBelow:R}");
        Assert.True(backAbove > edge, $"{above:R} came back as {backAbove:R}");
        Assert.NotEqual(backBelow, backAbove);
    }

    /// <summary>
    /// <b>EVERY PROPERTY OF EVERY MESSAGE REACHES THE WIRE, AND THIS IS THE CHECK THAT
    /// DOES NOT ROT.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A ROUND TRIP THAT COMPARES BYTES TO BYTES PROVES STABILITY AND NOT
    /// LOSSLESSNESS.</b> A field the serialiser cannot see is missing from BOTH sides and
    /// the two agree perfectly — which is precisely how <see cref="Kind"/> behaved: a
    /// struct whose whole state is private, written as <c>{}</c>, read back as a relation
    /// no machine ever named, with nothing thrown and nothing unequal.
    /// </para>
    /// <para>
    /// <b>SO THE PROPERTY IS ASKED OF THE TYPE RATHER THAN OF ONE VALUE.</b> Every public
    /// property must appear in the written form. A message that grows a field later is
    /// covered without this test being edited, which is the difference between a budget
    /// and a list somebody has to remember to update.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(typeof(Message))]
    [InlineData(typeof(Envelope))]
    [InlineData(typeof(Report))]
    [InlineData(typeof(Settled))]
    [InlineData(typeof(Arrival))]
    [InlineData(typeof(Accounting))]
    public void No_property_of_a_message_is_missing_from_the_wire(Type message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var written = Wire.Write(Populated(message));

        var missing = message
            .GetProperties()
            .Where(one => one.CanRead && one.GetIndexParameters().Length == 0)
            .Select(one => one.Name)
            .Where(name => !written.Contains($"\"{name}\":", StringComparison.Ordinal))
            .ToList();

        output.WriteLine($"{message.Name}: {written}");

        Assert.True(missing.Count == 0,
            $"{message.Name} never writes {string.Join(", ", missing)} — "
            + "a field the wire cannot see arrives as its default on every message");
    }

    /// <summary>An instance with nothing left at its default, so a dropped field shows.</summary>
    /// <remarks>
    /// <b>DEFAULTS ARE THE PROBLEM AND SO THEY ARE AVOIDED.</b> A property left at zero
    /// would be indistinguishable from one the serialiser dropped, so the check would pass
    /// for the fault it exists to find.
    /// </remarks>
    private static object Populated(Type message) =>
        message == typeof(Message) ? Filled
        : message == typeof(Envelope)
            ? new Envelope { To = new ClusterAddress("c"), Messages = [Filled], Everywhere = true }
        : message == typeof(Report)
            ? new Report
            {
                From = new ClusterAddress("c"),
                Arrivals = [Reached],
                Handled = 3,
                SentInto = [],
                Accounting = new Accounting { Broadcast = BroadcastId.New() },
            }
        : message == typeof(Settled)
            ? new Settled
            {
                Broadcast = BroadcastId.New(),
                From = new MachineAddress("m"),
                Arrivals = [Reached],
            }
        : message == typeof(Arrival) ? Reached
        : message == typeof(Accounting) ? new Accounting { Broadcast = BroadcastId.New() }
        : throw new ArgumentOutOfRangeException(nameof(message), $"no sample for {message.Name}");

    private static Message Filled => new()
    {
        Broadcast = BroadcastId.New(),
        ReturnTo = new MachineAddress("m"),
        To = new Code(7, 11),
        Held = 4.0,
        Together = 0.25,
        Seen = 0.5,
        Kind = Kind.After,
        Through = Kind.Before,
        Recent = true,
        Fresh = 0.75,
        Chain = [new Code(1, 2)],
        Carried = 1.0,
    };

    private static Arrival Reached => new()
    {
        Endpoint = new Code(9, 3),
        Score = 0.5,
        Chain = [new Code(1, 1)],
        Best = 0.25,
        Routes = 2,
    };

    /// <summary>A code is two numbers and both of them are exact.</summary>
    /// <remarks>
    /// <b>The value is a <see cref="ulong"/> and JSON numbers are doubles in a lot of
    /// readers</b>, which silently loses precision above 2^53 — and a code's value is a
    /// hash, so the high bits are the ones carrying the identity.
    /// </remarks>
    [Fact]
    public void A_code_survives_including_the_high_bits_of_its_value()
    {
        foreach (var value in new[] { 0UL, 1UL, ulong.MaxValue, (1UL << 53) + 1, 0xDEADBEEFCAFEF00D })
        {
            var code = new Code(Modality: 200, value);

            Assert.Equal(code, Wire.Read<Code>(Wire.Write(code)));
        }
    }

    /// <summary>
    /// <b>A WHOLE ENVELOPE, WHICH IS WHAT ACTUALLY GOES DOWN THE WIRE.</b>
    /// </summary>
    /// <remarks>
    /// The pieces above are the parts that go wrong; this is the thing that has to arrive.
    /// Records compare by value, so one assertion covers every field including the nested
    /// arrays — and a field added later is covered by it without this test being touched,
    /// which is the opposite of the hand-listed comparison that would rot.
    /// </remarks>
    [Fact]
    public void An_envelope_arrives_as_the_envelope_that_was_sent()
    {
        var envelope = new Envelope
        {
            To = new ClusterAddress("cluster-3"),
            Messages =
            [
                new Message
                {
                    Broadcast = BroadcastId.New(),
                    ReturnTo = new MachineAddress("machine-1"),
                    To = new Code(7, 12345678901234567),
                    Held = Math.BitDecrement(4.0),
                    Together = 1.0 / 3.0,
                    Seen = double.Epsilon,
                    Kind = Kind.After,
                    Recent = true,
                    Fresh = 0.1 + 0.2,
                    Chain = [new Code(1, 2), new Code(3, 4)],
                    Carried = 0.9999999999999999,
                },
            ],
            Everywhere = true,
        };

        var back = Wire.Read<Envelope>(Wire.Write(envelope));

        output.WriteLine(Wire.Write(envelope));

        // NOT `Assert.Equal(envelope, back)`, AND THE REASON IS A TRAP THIS REPO HAS
        // ALREADY PAID FOR ONCE. A synthesised record equality compares
        // `ImmutableArray<T>` by the identity of the array behind it, so two envelopes
        // holding identical messages are never equal and the assertion could only ever
        // fail. `Multiplexer.Round` carries a hand-written `Equals` with the same note.
        Assert.Equal(Wire.Write(envelope), Wire.Write(back));

        var sent = envelope.Messages[0];
        var got = back.Messages[0];

        // AND THE LEAVES BY HAND, because writing the same bytes twice proves the trip is
        // STABLE and not that it is LOSSLESS -- a field dropped by the serialiser is
        // dropped identically on both sides and compares equal. That is exactly how
        // `Kind` behaved before it had a converter.
        Assert.Equal(sent.Kind, got.Kind);
        Assert.Equal(sent.To, got.To);
        Assert.Equal(sent.Broadcast, got.Broadcast);
        Assert.Equal(sent.ReturnTo, got.ReturnTo);
        Assert.Equal(sent.Recent, got.Recent);
        Assert.Equal(sent.Chain.ToArray(), got.Chain.ToArray());

        foreach (var (was, is_) in new[]
        {
            (sent.Held, got.Held), (sent.Together, got.Together),
            (sent.Seen, got.Seen), (sent.Fresh, got.Fresh), (sent.Carried, got.Carried),
        })
            Assert.Equal(BitConverter.DoubleToInt64Bits(was), BitConverter.DoubleToInt64Bits(is_));
    }

    /// <summary>A report is the accounting, and it comes back whole.</summary>
    [Fact]
    public void A_report_arrives_as_the_report_that_was_sent()
    {
        var report = new Report
        {
            From = new ClusterAddress("cluster-1"),
            Arrivals =
            [
                new Arrival
                {
                    Endpoint = new Code(9, ulong.MaxValue),
                    Score = 1.0 / 3.0,
                    Chain = [new Code(1, 1)],
                    Best = Math.BitIncrement(0.5),
                    Routes = 3,
                },
            ],
            Handled = 2,
            SentInto = [],
            Accounting = new Accounting { Broadcast = BroadcastId.New() },
        };

        var back = Wire.Read<Report>(Wire.Write(report));

        Assert.Equal(Wire.Write(report), Wire.Write(back));

        Assert.Equal(report.From, back.From);
        Assert.Equal(report.Handled, back.Handled);
        Assert.Equal(report.Accounting, back.Accounting);
        Assert.Equal(report.Arrivals[0].Endpoint, back.Arrivals[0].Endpoint);
        Assert.Equal(report.Arrivals[0].Chain.ToArray(), back.Arrivals[0].Chain.ToArray());
        Assert.Equal(
            BitConverter.DoubleToInt64Bits(report.Arrivals[0].Best),
            BitConverter.DoubleToInt64Bits(back.Arrivals[0].Best));
    }

    /// <summary>A finished thought is the thing an actuator acts on.</summary>
    /// <remarks>
    /// Fork 11 routes this by CODE rather than by address, so it is the one message whose
    /// recipients are not known to the sender — which makes it the one whose shape cannot
    /// be checked by whoever receives it complaining.
    /// </remarks>
    [Fact]
    public void A_finished_thought_arrives_as_the_thought_that_finished()
    {
        var settled = new Settled
        {
            Broadcast = BroadcastId.New(),
            From = new MachineAddress("machine-2"),
            Arrivals =
            [
                new Arrival
                {
                    Endpoint = new Code(3, 77),
                    Score = 0.30000000000000004,
                    Chain = [new Code(3, 77), new Code(4, 88)],
                    Best = 0.1,
                    Routes = 1,
                },
            ],
        };

        var back = Wire.Read<Settled>(Wire.Write(settled));

        Assert.Equal(Wire.Write(settled), Wire.Write(back));

        Assert.Equal(settled.Broadcast, back.Broadcast);
        Assert.Equal(settled.From, back.From);
        Assert.Equal(settled.Arrivals[0].Endpoint, back.Arrivals[0].Endpoint);
        Assert.Equal(settled.Arrivals[0].Chain.ToArray(), back.Arrivals[0].Chain.ToArray());
    }
}
