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

    /// <summary>
    /// <b>Whether a CONJUNCTION can name a drawn word</b>, which is a different ceiling from
    /// the probe's and the one rung one actually has to clear.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A probe is not a rule learner</b>, and fork 43 is the whole of that gap. The
    /// reading above says a linear probe over a shared codebook recovers which word was
    /// drawn; a commitment cannot weigh evidence, so what it needs is a set of codes present
    /// in EVERY drawing of one word and in no drawing of any other. That set either exists
    /// or it does not, and no learning is involved in asking.
    /// </para>
    /// <para>
    /// <b>Bare codes only, because a placed one pins a patch.</b> A word that moves lands its
    /// parts in different patches, so a placed code cannot be in every drawing of it. The
    /// bare half of <see cref="Tiling"/> is what survives the offset, and this asks whether
    /// the bare half alone is enough to separate sixteen words.
    /// </para>
    /// <para>
    /// <b>The DEPTH is the reading</b>, rather than whether one exists. Repair grows a scope
    /// one code at a time from a gate wanting twenty misses first, so a sound scope tens of
    /// codes long is unreachable however sound it is. What blocks a rung can be its cost
    /// rather than its language, and the two are indistinguishable from a score.
    /// </para>
    /// <para>
    /// <b>What would drop this arm</b>: every word separable at one code. Then the shape
    /// sense is a lookup wearing a codebook, the ceiling says nothing about binding, and the
    /// world is what to build instead of this.
    /// </para>
    /// <para>
    /// <b>And what would block fork 107 at rung one</b>: no word separable at any depth.
    /// Then a conjunction cannot name a moved shape however the world is arranged, and
    /// binding needs rung five to mint a name over co-firing patches BEFORE it needs a world
    /// to bind in.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_a_conjunction_can_name_of_a_word_that_arrived_as_pixels()
    {
        const byte Patch = 110;

        var every = Drawings(seed: 1);

        output.WriteLine($"{Words.Length} words, {every.Count} drawings, bare codes only");
        output.WriteLine(
            $"{"tiling",-10}{"bare",8}{"always",8}{"at 1",7}{"at 2",7}{"at 8",7}"
            + $"{"at any",8}{"least",7}{"common",8}");

        var named = new Dictionary<int, int>();
        var least = new Dictionary<int, int>();
        var specific = new Dictionary<int, double>();

        foreach (var tile in new[] { 3, 4, 6, 8 })
        {
            var (cells, _, _) = Winnowing.Sheet(tile * tile);
            var tiling = new Tiling(Patch, Lettering.Side, tile);

            // The bare half of each drawing, which is the part independent of where it sat.
            var bare = every
                .Select(one => (
                    Codes: tiling
                        .Codify(one.Pixels)
                        .Where(code => code.Value < (ulong)cells)
                        .Select(code => code.Value)
                        .ToHashSet(),
                    one.Word))
                .ToList();

            var vocabulary = bare.SelectMany(one => one.Codes).ToHashSet().Count;
            var depths = new List<int>();
            var always = 0.0;

            // What EVERY word has in every drawing, which is the control on the account of
            // why a patch size fails. If the codes that survive an offset are the ones
            // nobody is distinguished by, the always-set and this set are the same size --
            // and that is a measurement rather than a story about blank patches.
            HashSet<ulong>? common = null;

            for (var word = 0; word < Words.Length; word++)
            {
                var mine = bare.Where(one => one.Word == word).Select(one => one.Codes).ToList();
                var theirs = bare.Where(one => one.Word != word).Select(one => one.Codes).ToList();

                // Present in every drawing of this word, which is the only place a sound
                // conjunction for it can come from.
                var kept = mine.Skip(1).Aggregate(
                    new HashSet<ulong>(mine[0]),
                    (standing, one) => { standing.IntersectWith(one); return standing; });

                always += kept.Count;

                if (common is null) common = [.. kept];
                else common.IntersectWith(kept);

                // Which of the other words' drawings each candidate still admits. A
                // conjunction fires where every one of its codes is present, so a pair
                // admits exactly the drawings both of its codes admit.
                var through = kept.ToDictionary(
                    code => code,
                    code => theirs
                        .Select((one, at) => (one, at))
                        .Where(pair => pair.one.Contains(code))
                        .Select(pair => pair.at)
                        .ToHashSet());

                // The exact answer, and it is what turns a greedy nought into a statement. A
                // conjunction of EVERY always-present code is the deepest sound scope this
                // word has, so if that one still admits another word's drawing then no
                // conjunction over bare codes separates it at any depth whatsoever.
                if (through.Count == 0
                    || through.Values.Aggregate(
                        new HashSet<int>(through.Values.First()),
                        (standing, admits) =>
                        {
                            standing.IntersectWith(admits);
                            return standing;
                        }).Count > 0) continue;

                depths.Add(Separates(through));
            }

            named[tile] = depths.Count;
            specific[tile] = (always / Words.Length) - common!.Count;
            least[tile] = depths.Count == 0 ? 0 : depths.Order().ElementAt(depths.Count / 2);

            output.WriteLine(
                $"{"tiling " + tile,-10}{vocabulary,8}{always / Words.Length,8:F1}"
                + $"{depths.Count(depth => depth <= 1),7}"
                + $"{depths.Count(depth => depth <= 2),7}"
                + $"{depths.Count(depth => depth <= 8),7}"
                + $"{depths.Count,8}{least[tile],7}{common!.Count,8}");
        }

        var best = named.MaxBy(one => one.Value);

        output.WriteLine(
            $"the best tiling names {best.Value} of {Words.Length} words soundly, at "
            + $"{best.Key} pixels a patch and a median of {least[best.Key]} codes a scope");

        Assert.True(best.Value > 0,
            "the conjunction of every always-present bare code still admits another word's "
            + $"drawing, for all {Words.Length} words at every patch size. So no conjunction "
            + "over bare codes separates a moved word AT ANY DEPTH, which is rung one's "
            + "ceiling rather than a search failure -- fork 107 would need a name minted over "
            + "co-firing patches before it needed a world to bind in");

        // And the mechanism, as one quantity rather than as a story. What a conjunction has
        // to work with is the codes that survive the offset AND are not shared by every
        // word, which is `always` less `common`. The first draft of this asserted that a
        // failing size is one where the survivors are universal; the 3-pixel row refused it
        // at 0.70, because that size fails the other way -- its codebook holds 81 parts over
        // the whole world, so a part is common to too many words while being universal to
        // none. Two accounts, one column, and this is the column.
        var own = named.Keys.ToDictionary(tile => tile, tile => specific[tile]);

        output.WriteLine(
            "word-specific codes surviving the offset: "
            + string.Join(", ", own.Select(one => $"{one.Key} at {one.Value:F1}")));

        Assert.True(own[best.Key] == own.Values.Max(),
            $"the patch size that names words does not hold the most word-specific codes "
            + "through the offset, so what a conjunction has to work with is not what "
            + "decides whether a size works, and the account here is wrong");

        foreach (var (tile, count) in named)
            if (count == 0)
                Assert.True(own[tile] < own[best.Key] / 4.0,
                    $"a {tile}-pixel patch names no word while leaving {own[tile]:F1} "
                    + $"word-specific codes through the offset, against {own[best.Key]:F1} "
                    + "for the size that works. That is close enough that the column is not "
                    + "what separates them");
    }

    /// <summary>
    /// The smallest conjunction a greedy search finds that lets nothing else through.
    /// </summary>
    /// <param name="through">Which other drawings each candidate code still admits.</param>
    /// <remarks>
    /// <b>Greedy past two, because the exact answer is set cover.</b> One and two are
    /// exhaustive and therefore exact; deeper, the code admitting fewest is taken and
    /// extended, which can only ever OVERSTATE the depth a conjunction needs. So the number
    /// is read as *no deeper than* rather than as the minimum.
    /// <para>
    /// <b>Only ever called where the whole set separates</b>, so it terminates: taking every
    /// candidate in turn ends at an empty admitting set, and the loop is bounded by the
    /// candidates whatever the greedy step chooses.
    /// </para>
    /// </remarks>
    private static int Separates(Dictionary<ulong, HashSet<int>> through)
    {
        if (through.Values.Any(admits => admits.Count == 0)) return 1;

        var codes = through.Keys.ToList();

        for (var one = 0; one < codes.Count; one++)
            for (var two = one + 1; two < codes.Count; two++)
                if (!through[codes[one]].Overlaps(through[codes[two]]))
                    return 2;

        var standing = through.Values.MinBy(admits => admits.Count)!.ToHashSet();
        var taken = new HashSet<ulong>();

        for (var depth = 2; depth <= through.Count; depth++)
        {
            var next = through
                .Where(one => !taken.Contains(one.Key))
                .Select(one => (one.Key, Left: standing.Intersect(one.Value).ToHashSet()))
                .MinBy(one => one.Left.Count);

            taken.Add(next.Key);
            standing = next.Left;

            if (standing.Count == 0) return depth;
        }

        return through.Count;
    }
}
