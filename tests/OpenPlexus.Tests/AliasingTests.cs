using OpenPlexus.Codes;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Two different readings may never be the same observation.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE BUDGET FOR A FAILURE CLASS THAT HAD ALREADY HAPPENED AND SAID NOTHING.</b>
/// <see cref="Banded{TFrame}"/> gives each dimension a block of modalities and assigns
/// them with <c>(byte)(first + which * spans)</c> — an unchecked cast. Past 256 it
/// WRAPPED, so on a reading of 192 dimensions the codes for dimension 0 and dimension
/// 128 were identical and two different pictures arrived at the learner as the same
/// moment. No exception, no warning, no failing test.
/// </para>
/// <para>
/// <b>IT HAD NEVER FIRED BECAUSE NOTHING WAS WIDE ENOUGH.</b> <c>Graded</c> tops out
/// at twenty dimensions and <c>Tending</c> guarded the block itself — in that ONE
/// world, while the type everybody else built had no check at all. The first world
/// here to read a picture would have walked straight into it.
/// </para>
/// <para>
/// <b>SO THE GUARD LIVES ON THE TYPE AND THIS IS WHAT KEEPS IT THERE.</b> A silent
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
        // 129 DIMENSIONS AT TWO SPANS FROM ZERO NEEDS 258 MODALITIES. Before the
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
        // OR THE DECLARED WIDTH IS A PROMISE NOBODY KEEPS, and the construction-time
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
    /// <b><see cref="Winnow"/> HAS NO SUCH CEILING, AND IT IS STRUCTURAL.</b>
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
}
