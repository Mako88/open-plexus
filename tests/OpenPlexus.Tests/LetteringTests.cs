using OpenPlexus.Codes;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What a front end can still tell about a word once it is pixels — <b>fork 107's ceiling,
/// taken before anything is built on it.</b>
/// </summary>
/// <param name="output">Where the rows go.</param>
/// <remarks>
/// <para>
/// <b>The front end's ceiling comes first</b>, and this repo has a line about why: an arm at
/// the front end has a ceiling computable with no learning, and it costs milliseconds against
/// a runner's hour. A grid cannot tell a learner that failed to bind two senses from a front
/// end that never carried one of them.
/// </para>
/// <para>
/// <b>So this asks nothing about binding.</b> It asks whether the codes over a drawn word
/// carry WHICH WORD it is, against the pixels they were made from. What the population does
/// with two senses is the next question and it is not this one.
/// </para>
/// <para>
/// <b>The offsets are the exam and never the words</b>, which is what makes it a reading
/// rather than a lookup. Every word is seen while fitting; what is withheld is where it sat,
/// so a probe that scores has generalised over position and one that has not has memorised
/// pixel addresses. A split by word would ask something else entirely.
/// </para>
/// <para>
/// <b>What would drop it</b>, written before the grid ran: pixels themselves reading near
/// chance. That is the renderer or the offsets broken rather than a finding about any front
/// end, and every number beside it would be about the fault.
/// </para>
/// <para>
/// <b>And what would block fork 107</b>: the codes reading near chance where the pixels read
/// high. Then the crossing cannot be attempted, because one of its two senses does not carry
/// the thing the other is supposed to bind to — a front end's failure rather than a learner's.
/// </para>
/// </remarks>
public sealed class LetteringTests(ITestOutputHelper output)
{
    /// <summary>How many pixels apart the drawn offsets are.</summary>
    private const int Stride = 2;

    /// <summary>Three letters each, so a drawn word leaves room to move in both directions.</summary>
    private static readonly string[] Words =
    [
        "CAT", "DOG", "BOX", "CUP", "HAT", "PEN", "BED", "SUN",
        "MAP", "JAR", "KEY", "FAN", "RUG", "NET", "POT", "WEB",
    ];

    /// <summary>Every drawing of every word, and which of them are withheld.</summary>
    /// <param name="seed">The split's generator, which is not a front end's.</param>
    /// <remarks>
    /// <b>Drawn once and shared by every arm</b>, so two front ends are read on the identical
    /// rasters and the identical split. Redrawing per arm would put the split's own spread
    /// into a comparison that is supposed to be about the arms.
    /// </remarks>
    private static List<(IReadOnlyList<double> Pixels, int Word, bool Shown)> Drawings(int seed)
    {
        var (room, drop) = Lettering.Room(3);
        var coins = new Random(seed);
        var every = new List<(IReadOnlyList<double>, int, bool)>();

        // Every second offset in each direction, which is a clock cost and NOT a free one.
        // The whole grid is four times the drawings and seven times the clock, and the
        // smallest patch is the cell that moves: three pixels reads 0.450 there against 0.058
        // here, while four reads 0.584 against 0.539. A codebook read over more patches needs
        // more sightings to fill, so the cheap grid understates the small ones and the gate
        // below is taken on the best cell rather than on the shape of the column.
        for (var word = 0; word < Words.Length; word++)
            for (var across = 0; across <= room; across += Stride)
                for (var down = 0; down <= drop; down += Stride)
                    every.Add((
                        Lettering.Draw(Words[word], across, down),
                        word,
                        coins.NextDouble() >= 0.25));

        return every;
    }

    /// <summary>
    /// <b>Whether a drawn word is still readable</b> once a front end has coded it.
    /// </summary>
    [Fact]
    public void What_a_front_end_leaves_of_a_word_that_arrived_as_pixels()
    {
        const byte Patch = 110;

        var every = Drawings(seed: 1);
        var chance = 1.0 / Words.Length;

        output.WriteLine(
            $"{Words.Length} words, {every.Count} drawings, "
            + $"{every.Count(one => !one.Shown)} withheld, chance {chance:F3}");

        output.WriteLine($"{"reading",-16}{"features",10}{"withheld",10}{"scored",9}");

        // The instrument check, and it is a SEPARATE reading rather than the one below. Every
        // word drawn at one place, split by nothing but the coin -- so the withheld raster is
        // one the probe has already seen pixel for pixel. It asks only whether these glyphs
        // are distinguishable at all, and a fault in the font or the canvas shows here.
        var still = new List<(IReadOnlyList<double>, int)>();
        var again = new List<(IReadOnlyList<double>, int)>();
        var flip = new Random(2);

        for (var word = 0; word < Words.Length; word++)
            for (var copy = 0; copy < 16; copy++)
                (flip.NextDouble() >= 0.25 ? still : again)
                    .Add((Lettering.Draw(Words[word], 0, 0), word));

        var fixedly = Probe.Fit(still, again, Words.Length);

        output.WriteLine(
            $"{"pixels, still",-16}{Lettering.Side * Lettering.Side,10}"
            + $"{fixedly.Accuracy,10:F3}{fixedly.Tested,9}");

        var onDrawn = new List<(IReadOnlyList<double>, int)>();
        var onUnseen = new List<(IReadOnlyList<double>, int)>();

        foreach (var (raster, word, shown) in every)
            (shown ? onDrawn : onUnseen).Add((raster, word));

        var pixels = Probe.Fit(onDrawn, onUnseen, Words.Length);

        output.WriteLine(
            $"{"pixels, moved",-16}{Lettering.Side * Lettering.Side,10}"
            + $"{pixels.Accuracy,10:F3}{pixels.Tested,9}");

        var coded = new Dictionary<int, Probed>();

        foreach (var tile in new[] { 3, 4, 6, 8 })
        {
            // One codebook for every patch and every drawing, which is what makes a code MEAN
            // a part wherever it turns up. Built fresh per tile size because the codebook is
            // the thing being sized.
            var tiling = new Tiling(Patch, Lettering.Side, tile);
            var features = new Dictionary<Code, int>();
            var said = new List<(IReadOnlyCollection<Code> Codes, int Word, bool Shown)>();

            foreach (var (raster, word, shown) in every)
            {
                var codes = tiling.Codify(raster);

                foreach (var code in codes)
                    if (!features.ContainsKey(code)) features[code] = features.Count;

                said.Add((codes, word, shown));
            }

            var fitting = new List<(IReadOnlyList<double>, int)>();
            var scoring = new List<(IReadOnlyList<double>, int)>();

            foreach (var (codes, word, shown) in said)
            {
                // An indicator per code, which is what a commitment's scope reads. Anything
                // richer would hand this side something the population never had.
                var indicator = new double[features.Count];
                foreach (var code in codes) indicator[features[code]] = 1.0;

                (shown ? fitting : scoring).Add((indicator, word));
            }

            coded[tile] = Probe.Fit(fitting, scoring, Words.Length);

            output.WriteLine(
                $"{"tiling " + tile,-16}{features.Count,10}"
                + $"{coded[tile].Accuracy,10:F3}{coded[tile].Tested,9}");
        }

        // The instrument first, and it is not a formality. Sixteen words drawn at one place
        // are sixteen distinct rasters, so anything short of reading them off is a fault in
        // the font or the canvas and every number below would be about that.
        Assert.True(fixedly.Accuracy > 0.95,
            $"a probe read {fixedly.Accuracy:F3} of sixteen words drawn at one place, so the "
            + "glyphs are not distinguishable and nothing else here can be read");

        // And fork 107's gate. A crossing needs both senses to carry the thing being bound,
        // so a front end reading near chance where the pixels read high blocks the experiment
        // at the front end rather than at the learner.
        var best = coded.MaxBy(one => one.Value.Accuracy);

        output.WriteLine(
            $"the best tiling reads {best.Value.Accuracy:F3} against {pixels.Accuracy:F3} "
            + $"for the pixels it was made from, at {best.Key} pixels a patch");

        Assert.True(best.Value.Accuracy > 4.0 * chance,
            $"the best front end read {best.Value.Accuracy:F3} against a chance of "
            + $"{chance:F3} while the pixels read {pixels.Accuracy:F3}, so the codes do not "
            + "carry which word was drawn and fork 107 is blocked at the front end rather "
            + "than at binding");

        // And what the codebook is worth, which is the finding rather than the gate. A linear
        // probe over pixel addresses cannot be moved, and it does WORSE than guessing once the
        // word is: the addresses a word lit while fitting belong to other words when it shifts,
        // so the features are not merely useless but misleading. One codebook read at every
        // patch is what makes a part mean the same thing wherever it lands.
        Assert.True(best.Value.Accuracy > pixels.Accuracy + 0.2,
            $"a shared codebook read {best.Value.Accuracy:F3} against {pixels.Accuracy:F3} "
            + "for the pixel addresses under it, so the front end is not what carries a moved "
            + "word and the account of why patches pay is wrong");
    }
}
