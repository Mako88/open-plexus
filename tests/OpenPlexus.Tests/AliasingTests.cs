using OpenPlexus.Codes;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Two different readings may never be the same observation.
/// </summary>
/// <remarks>
/// <para>
/// <b>The budget for a failure class</b> that had already happened and said nothing.
/// <see cref="Banded{TFrame}"/> gives each dimension a block of modalities and assigns
/// them with <c>(byte)(first + which * spans)</c> — an unchecked cast. Past 256 it
/// WRAPPED, so on a reading of 192 dimensions the codes for dimension 0 and dimension
/// 128 were identical and two different pictures arrived at the learner as the same
/// moment. No exception, no warning, no failing test.
/// </para>
/// <para>
/// <b>It had never fired because nothing was wide enough.</b> <c>Graded</c> tops out
/// at twenty dimensions and <c>Tending</c> guarded the block itself — in that ONE
/// world, while the type everybody else built had no check at all. The first world
/// here to read a picture would have walked straight into it.
/// </para>
/// <para>
/// <b>So the guard lives on the type</b>, and this is what keeps it there. A silent
/// collision is the worst shape a bug can have in this design: every downstream number
/// stays plausible, and the learner is simply wrong about what it saw.
/// </para>
/// </remarks>
public sealed class AliasingTests(ITestOutputHelper output)
{
    /// <summary>What one dimension moving does to the code set.</summary>
    private static string Signature(Banded<IReadOnlyList<double>> sense, int width, int which)
    {
        var flat = Enumerable.Repeat(0.5, width).ToList();
        var baseline = sense.Codify(flat).ToHashSet();

        var moved = flat.ToList();
        moved[which] = 0.05;

        return string.Join(",", sense.Codify(moved)
            .ToHashSet()
            .Except(baseline)
            .Select(one => $"{one.Modality}:{one.Value}")
            .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void No_two_dimensions_of_a_reading_produce_the_same_codes()
    {
        // EVERY WIDTH THE GUARD ADMITS, and the guard is what stops the list being
        // longer. At two spans from modality 0 the block holds 128 dimensions.
        foreach (var width in new[] { 2, 11, 20, 64, 100, 128 })
        {
            var sense = new Banded<IReadOnlyList<double>>(
                reading => reading, first: 0, width, bands: 8, grains: 2);

            var seen = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var which = 0; which < width; which++)
            {
                var signature = Signature(sense, width, which);

                Assert.False(seen.TryGetValue(signature, out var earlier),
                    $"width {width}: dimensions {earlier} and {which} emit identical "
                    + "codes, so two different readings are one observation");

                seen[signature] = which;
            }

            output.WriteLine($"width {width,4}: {width} distinct dimension signatures");
        }
    }

    [Fact]
    public void A_reading_too_wide_for_its_modality_block_is_refused_at_construction()
    {
        // 129 dimensions at two spans from zero needs 258 modalities. Before the
        // guard this constructed happily and aliased dimension 0 with dimension 128.
        var thrown = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Banded<IReadOnlyList<double>>(
                reading => reading, first: 0, width: 129, bands: 8, grains: 2));

        output.WriteLine(thrown.Message);

        // AND THE COMPANION, or the check passes for a type that refuses everything.
        _ = new Banded<IReadOnlyList<double>>(
            reading => reading, first: 0, width: 128, bands: 8, grains: 2);
    }

    [Fact]
    public void A_reading_that_is_not_the_declared_width_is_refused_when_it_arrives()
    {
        // Or the declared width is a promise nobody keeps, and the construction-time
        // guard is decoration -- a sense built for 8 and handed 400 wraps exactly as
        // it did before.
        var sense = new Banded<IReadOnlyList<double>>(
            reading => reading, first: 0, width: 8, bands: 8, grains: 2);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sense.Codify(Enumerable.Repeat(0.5, 400).ToList()));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sense.Codify(Enumerable.Repeat(0.5, 7).ToList()));

        Assert.Equal(16, sense.Codify(Enumerable.Repeat(0.5, 8).ToList()).Count);
    }

    /// <summary>
    /// <b><see cref="Winnow"/> Has no such ceiling, and it is structural.</b>
    /// </summary>
    /// <remarks>
    /// Every code it emits rides on ONE modality and the sheet is addressed by the
    /// code's VALUE, which is 64 bits rather than 8. That is the difference between a
    /// front end that can be pointed at a picture and one that cannot, and it is a
    /// difference in kind rather than in quality.
    /// </remarks>
    [Fact]
    public void Winnow_addresses_a_reading_no_block_of_modalities_could_hold()
    {
        foreach (var width in new[] { 64, 256, 1024, 3072 })
        {
            var sense = new Winnowing(modality: 200, width);
            var reading = Enumerable.Range(0, width).Select(one => one / (double)width).ToList();

            var said = sense.Codify(reading);

            Assert.All(said, code => Assert.Equal(200, code.Modality));

            var (cells, _, winners) = Winnowing.Sheet(width);

            output.WriteLine($"width {width,5}: {cells,6} cells, {said.Count,5} codes in one moment");

            Assert.Equal(winners, said.Count);
        }
    }

    [Fact]
    public void A_row_of_whole_numbers_cannot_pack_two_positions_onto_one_code()
    {
        // THE SAME FAULT AS `Banded`'S wrap, in the simplest translation there is.
        // `Bits` documented itself as reading whole numbers and packed
        // `(position << 1) | value`, so position one holding nought and position nought
        // holding two were THE SAME CODE. Two attributes conflated, silently, and every
        // downstream number still plausible.
        //
        // It had never fired because its only caller was bits. Which is this repo's own
        // trap about a guard mounted on one caller, arriving in a packing instead --
        // and the first world with a three-valued attribute would have walked into it.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Bits.Of(modality: 1, position: 0, value: 2));

        // And the wide packing is injective, which is what the guard is protecting.
        // Six positions of four values apiece is `Monk`'s widest attribute, and every
        // one of the twenty-four must be its own code.
        var seen = new HashSet<Code>();

        for (var position = 0; position < 6; position++)
        for (var value = 0; value < 4; value++)
            Assert.True(seen.Add(Bits.Of(modality: 1, position, value, stride: 4)),
                $"position {position} value {value} collided with something already said");

        Assert.Equal(24, seen.Count);

        // And the default is byte-for-byte what was there, so no measurement taken on a
        // binary world moves. Asserted rather than believed, because "this changes
        // nothing" is the claim that most needs a check behind it.
        for (var position = 0; position < 8; position++)
        for (var value = 0; value < 2; value++)
            Assert.Equal(
                new Code(1, (ulong)((position << 1) | value)),
                Bits.Of(modality: 1, position, value));
    }

    /// <summary>
    /// <b>The other spelling is injective too</b>, and it puts the value where a variable
    /// can reach it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Bits"/> packs the position into the value, so two positions holding one
    /// thing are two values under one modality; <see cref="Slotted"/> puts the position in
    /// the modality, so they are one value under two. Which of those a front end says decides
    /// whether <c>Commitments.Generalising</c> can ever join them, since it groups a scope's
    /// positions by the value they carry.
    /// </para>
    /// <para>
    /// <b>And it exhausts the byte instead of the value</b>, which is the cost stated as a
    /// refusal rather than as a remark. A row wider than the modalities left above its first
    /// would wrap, which is <see cref="Banded{TFrame}"/>'s fault by the other road.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_other_spelling_moves_the_position_into_the_modality()
    {
        // The whole difference, in one comparison. Position nought and position one both
        // holding value two.
        Assert.NotEqual(
            Bits.Of(modality: 20, position: 0, value: 2, stride: 4).Value,
            Bits.Of(modality: 20, position: 1, value: 2, stride: 4).Value);

        Assert.Equal(
            Slotted.Of(first: 20, position: 0, value: 2).Value,
            Slotted.Of(first: 20, position: 1, value: 2).Value);

        Assert.NotEqual(
            Slotted.Of(first: 20, position: 0, value: 2).Modality,
            Slotted.Of(first: 20, position: 1, value: 2).Modality);

        // And it is injective across the same grid `Bits` is asserted on above.
        var seen = new HashSet<Code>();

        for (var position = 0; position < 6; position++)
        for (var value = 0; value < 4; value++)
            Assert.True(seen.Add(Slotted.Of(first: 20, position, value)),
                $"position {position} value {value} collided with something already said");

        Assert.Equal(24, seen.Count);

        // And it reads back to what it was made from, which is what a world's answer key
        // decodes with.
        for (var position = 0; position < 6; position++)
        for (var value = 0; value < 4; value++)
        {
            var code = Slotted.Of(first: 20, position, value);

            Assert.Equal(position, Slotted.Position(first: 20, code));
            Assert.Equal(value, Slotted.Value(code));
        }

        // The cost, refused rather than wrapped. Ten positions from 250 would run off the
        // end of the byte and land on somebody else's alphabet.
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slotted(first: 250, positions: 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => Slotted.Of(first: 250, position: 10, value: 0));

        // And a reading wider than the positions declared is refused when it arrives, on
        // `Banded`'s rule one file along.
        var sense = new Slotted(first: 20, positions: 6);

        Assert.Equal(6, sense.Span);
        Assert.Equal(6, sense.Codify([0, 1, 2, 0, 3, 1]).Count);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => sense.Codify([0, 1, 2, 0, 3, 1, 0]));

        output.WriteLine(
            "a position rides the modality and the value stands alone, so two positions "
            + "holding one thing carry one value under two modalities");
    }
}
