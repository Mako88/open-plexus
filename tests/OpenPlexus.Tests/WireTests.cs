using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
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
    /// <b>EVERY PROPERTY OF EVERY MESSAGE REACHES THE WIRE, AND THIS IS THE CHECK THAT
    /// DOES NOT ROT.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A ROUND TRIP THAT COMPARES BYTES TO BYTES PROVES STABILITY AND NOT
    /// LOSSLESSNESS.</b> A field the serialiser cannot see is missing from BOTH sides and
    /// the two agree perfectly — which is precisely how the walk's edge kind behaved: a
    /// struct whose whole state was private, written as <c>{}</c>, read back as a relation
    /// no machine ever named, with nothing thrown and nothing unequal. <b>That type is gone
    /// and the trap is not</b>, which is why this theory outlived the messages it was
    /// written for.
    /// </para>
    /// <para>
    /// <b>SO THE PROPERTY IS ASKED OF THE TYPE RATHER THAN OF ONE VALUE.</b> Every public
    /// property must appear in the written form. A message that grows a field later is
    /// covered without this test being edited, which is the difference between a budget
    /// and a list somebody has to remember to update.
    /// </para>
    /// <para>
    /// <b>AND THE LIST IS THE WHOLE ASK AND ANSWER TREE RATHER THAN THE TWO TOP TYPES.</b>
    /// A nested payload is where a private table or a tuple key hides — <see cref="Counts"/>
    /// is what rung five reads off another machine, and a holder that shipped it as
    /// <c>{}</c> would name nothing and report no error.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(typeof(Ask))]
    [InlineData(typeof(Answer))]
    [InlineData(typeof(Tabled))]
    [InlineData(typeof(Testimony))]
    [InlineData(typeof(Advocacy))]
    [InlineData(typeof(Counts))]
    [InlineData(typeof(Tallied))]
    [InlineData(typeof(Learnt))]
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
    /// for the fault it exists to find. Every optional member of <see cref="Answer"/> is
    /// filled here for that reason, even though no single answer ever carries all three.
    /// </remarks>
    private static object Populated(Type message) =>
        message == typeof(Ask) ? FilledAsk
        : message == typeof(Answer) ? FilledAnswer
        : message == typeof(Tabled) ? FilledTabled
        : message == typeof(Testimony) ? FilledTestimony
        : message == typeof(Advocacy) ? FilledAdvocacy
        : message == typeof(Counts) ? FilledCounts
        : message == typeof(Tallied) ? FilledTallied
        : message == typeof(Learnt) ? (object)FilledLearnt
        : throw new ArgumentOutOfRangeException(nameof(message), $"no sample for {message.Name}");

    private static Advocacy FilledAdvocacy => new()
    {
        Expects = new Code(5, 12345678901234567),
        Weight = 1.0 / 3.0,
        By = new Code(6, 77),
    };

    private static Testimony FilledTestimony => new() { Advocates = [FilledAdvocacy] };

    private static Tallied FilledTallied => new()
    {
        Left = new Code(1, 2),
        Right = new Code(3, 4),
        Seen = 9,
    };

    private static Counts FilledCounts => new() { Scopes = 11, Rows = [FilledTallied] };

    private static Tabled FilledTabled => new()
    {
        From = new MachineAddress("holder-1"),
        Slot = "slot-a",
        Counted = FilledCounts,
    };

    private static Learnt FilledLearnt => new()
    {
        Minted = 1,
        Repaired = 2,
        Subsumed = 3,
        Widened = 4,
    };

    private static Ask FilledAsk => new()
    {
        Broadcast = BroadcastId.New(),
        ReturnTo = new MachineAddress("asker-1"),
        Wants = Wanted.Settle,
        Moment = [new Code(1, 2), new Code(3, 4)],
        Arrived = new Code(7, 8),
        Wrong = true,
        Sweeping = true,
        Counted = [FilledTabled],
    };

    private static Answer FilledAnswer => new()
    {
        Broadcast = BroadcastId.New(),
        From = new MachineAddress("holder-1"),
        Said = FilledTestimony,
        Counted = FilledCounts,
        Did = FilledLearnt,
    };

    /// <summary>
    /// <b>A WHOLE ASK, WHICH IS WHAT ACTUALLY GOES DOWN THE WIRE.</b>
    /// </summary>
    /// <remarks>
    /// The pieces above are the parts that go wrong; this is the thing that has to arrive.
    /// A settlement ask is the fullest one there is — it carries the moment, what actually
    /// arrived, whether the vote was wrong, and on a sweep round the whole fleet's tables.
    /// </remarks>
    [Fact]
    public void An_ask_arrives_as_the_ask_that_was_sent()
    {
        var ask = FilledAsk;

        var back = Wire.Read<Ask>(Wire.Write(ask));

        output.WriteLine(Wire.Write(ask));

        // NOT `Assert.Equal(ask, back)`, AND THE REASON IS A TRAP THIS REPO HAS ALREADY
        // PAID FOR ONCE. A synthesised record equality compares `ImmutableArray<T>` by the
        // identity of the array behind it, so two asks holding identical moments are never
        // equal and the assertion could only ever fail. `Multiplexer.Round` carries a
        // hand-written `Equals` with the same note.
        Assert.Equal(Wire.Write(ask), Wire.Write(back));

        // AND THE LEAVES BY HAND, because writing the same bytes twice proves the trip is
        // STABLE and not that it is LOSSLESS -- a field dropped by the serialiser is
        // dropped identically on both sides and compares equal. That is exactly how the
        // walk's edge kind behaved before it had a converter.
        Assert.Equal(ask.Broadcast, back.Broadcast);
        Assert.Equal(ask.ReturnTo, back.ReturnTo);
        Assert.Equal(ask.Wants, back.Wants);
        Assert.Equal(ask.Arrived, back.Arrived);
        Assert.Equal(ask.Wrong, back.Wrong);
        Assert.Equal(ask.Sweeping, back.Sweeping);
        Assert.Equal(ask.Moment.ToArray(), back.Moment.ToArray());

        Assert.Equal(ask.Counted[0].From, back.Counted[0].From);
        Assert.Equal(ask.Counted[0].Slot, back.Counted[0].Slot);
        Assert.Equal(ask.Counted[0].Counted.Scopes, back.Counted[0].Counted.Scopes);
        Assert.Equal(
            ask.Counted[0].Counted.Rows.ToArray(), back.Counted[0].Counted.Rows.ToArray());
    }

    /// <summary>An answer is what a holder makes of a moment, and it comes back whole.</summary>
    /// <remarks>
    /// <b>THE WEIGHT IS THE FIELD THIS TEST IS REALLY ABOUT.</b> An advocate's weight is a
    /// recency-weighted accuracy, so it is a double that decides an argmax — and a vote
    /// merged from a weight that lost its last bit on the wire would pick a different rule
    /// on some rounds and agree on the rest, which is the hardest kind of wrong to see.
    /// </remarks>
    [Fact]
    public void An_answer_arrives_as_the_answer_that_was_sent()
    {
        var answer = FilledAnswer;

        var back = Wire.Read<Answer>(Wire.Write(answer));

        Assert.Equal(Wire.Write(answer), Wire.Write(back));

        Assert.Equal(answer.Broadcast, back.Broadcast);
        Assert.Equal(answer.From, back.From);
        Assert.Equal(answer.Did, back.Did);
        Assert.Equal(answer.Counted!.Scopes, back.Counted!.Scopes);
        Assert.Equal(answer.Counted.Rows.ToArray(), back.Counted.Rows.ToArray());

        var said = answer.Said!.Value.Advocates[0];
        var got = back.Said!.Value.Advocates[0];

        Assert.Equal(said.Expects, got.Expects);
        Assert.Equal(said.By, got.By);
        Assert.Equal(
            BitConverter.DoubleToInt64Bits(said.Weight),
            BitConverter.DoubleToInt64Bits(got.Weight));
    }

    /// <summary>
    /// <b>SILENCE IS A THING A HOLDER SAYS, AND IT HAS TO SURVIVE THE TRIP.</b>
    /// </summary>
    /// <remarks>
    /// A holder that fired nothing has been HEARD FROM; a holder that died has not, and the
    /// merge may not treat them alike — see <see cref="Testimony.Silent"/>. An empty array
    /// that came back null would turn every silence into a death and quietly lower the
    /// denominator C3 is read off.
    /// </remarks>
    [Fact]
    public void A_holder_that_fired_nothing_still_arrives_as_having_spoken()
    {
        var quiet = new Answer
        {
            Broadcast = BroadcastId.New(),
            From = new MachineAddress("holder-2"),
            Said = new Testimony { Advocates = [] },
        };

        var back = Wire.Read<Answer>(Wire.Write(quiet));

        Assert.NotNull(back.Said);
        Assert.True(back.Said!.Value.Silent);
    }
}
