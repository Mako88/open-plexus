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

    /// <summary>The modality the drawn word's codes ride on.</summary>
    /// <remarks>
    /// <b>One number for every reading in this file</b>, so two of them cannot end up
    /// comparing front ends that differ in something nobody meant to vary.
    /// </remarks>
    private const byte Patch = 110;

    /// <summary>
    /// The first sixteen of <see cref="Lettering.Vocabulary"/>, which the probe is read on.
    /// </summary>
    /// <remarks>
    /// <b>A prefix rather than a list of its own</b>, so the sweep below reads the identical
    /// sixteen at its sixteen-word row. Two lists that had to be kept in step would drift the
    /// first time either was edited, and a chance of one in sixteen is what the probe's score
    /// is read against.
    /// </remarks>
    private static readonly string[] Words = [.. Lettering.Vocabulary.Take(16)];

    /// <summary>Every drawing of every word, and which of them are withheld.</summary>
    /// <param name="seed">The split's generator, which is not a front end's.</param>
    /// <param name="words">Which words to draw. <see cref="Words"/> where none is given.</param>
    /// <remarks>
    /// <b>Drawn once and shared by every arm</b>, so two front ends are read on the identical
    /// rasters and the identical split. Redrawing per arm would put the split's own spread
    /// into a comparison that is supposed to be about the arms.
    /// </remarks>
    private static List<(IReadOnlyList<double> Pixels, int Word, bool Shown)> Drawings(
        int seed, IReadOnlyList<string>? words = null)
    {
        var these = words ?? Words;
        var (room, drop) = Lettering.Room(3);
        var coins = new Random(seed);
        var every = new List<(IReadOnlyList<double>, int, bool)>();

        // Every second offset in each direction, which is a clock cost and NOT a free one.
        // The whole grid is four times the drawings and seven times the clock, and the
        // smallest patch is the cell that moves: three pixels reads 0.450 there against 0.058
        // here, while four reads 0.584 against 0.539. A codebook read over more patches needs
        // more sightings to fill, so the cheap grid understates the small ones and the gate
        // below is taken on the best cell rather than on the shape of the column.
        for (var word = 0; word < these.Count; word++)
            for (var across = 0; across <= room; across += Stride)
                for (var down = 0; down <= drop; down += Stride)
                    every.Add((
                        Lettering.Draw(these[word], across, down),
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
        var without = new Dictionary<int, Probed>();

        foreach (var tile in new[] { 3, 4, 6, 8 })
        {
            // One codebook for every patch and every drawing, which is what makes a code MEAN
            // a part wherever it turns up. Built fresh per tile size because the codebook is
            // the thing being sized.
            var whole = Reading(every, new Tiling(Patch, Lettering.Side, tile));
            var half = Reading(every, new Tiling(Patch, Lettering.Side, tile, placed: false));

            coded[tile] = whole.Read;
            without[tile] = half.Read;

            output.WriteLine(
                $"{"tiling " + tile,-16}{whole.Features,10}"
                + $"{whole.Read.Accuracy,10:F3}{whole.Read.Tested,9}");

            output.WriteLine(
                $"{"  bare only",-16}{half.Features,10}"
                + $"{half.Read.Accuracy,10:F3}{half.Read.Tested,9}");
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

        // And what the PLACED half is worth, which the conjunction reading below makes worth
        // asking. A placed code pins a patch, so it cannot sit in a sound scope for a word
        // that moves -- measured dead weight for a rule learner. Whether a probe agrees is a
        // different question, and the two halves of one front end need not.
        output.WriteLine(
            $"at {best.Key} pixels a patch the bare half alone reads "
            + $"{without[best.Key].Accuracy:F3} against {best.Value.Accuracy:F3} for both, "
            + $"on {without[best.Key].Tested} withheld drawings");

        Assert.True(without[best.Key].Accuracy > best.Value.Accuracy + 0.2,
            $"the bare half alone read {without[best.Key].Accuracy:F3} against "
            + $"{best.Value.Accuracy:F3} for both halves, so saying a winner a second time "
            + "with its patch is no longer costing this reading. `Tiling`'s placed arm is "
            + "why `CrossingRun` turns it off, and that reason has gone");
    }


    /// <summary>What a probe makes of one tiling's codes, with or without the placed half.</summary>
    /// <param name="every">Every drawing, with the word it is of and whether it was shown.</param>
    /// <param name="tiling">The front end, built once and read over every drawing.</param>
    /// <remarks>
    /// <b>The placed half is the arm</b>, and the conjunction reading below is why it is worth
    /// asking. A placed code cannot sit in a sound scope for a word that moves, so it is
    /// measured dead weight for a rule learner — what this says is whether it is dead weight
    /// for a probe too, or whether the two halves disagree about it.
    /// </remarks>
    private static (Probed Read, int Features) Reading(
        List<(IReadOnlyList<double> Pixels, int Word, bool Shown)> every,
        Tiling tiling)
    {
        var features = new Dictionary<Code, int>();
        var said = new List<(IReadOnlyCollection<Code> Codes, int Word, bool Shown)>();

        foreach (var (raster, word, shown) in every)
        {
            var codes = tiling.Codify(raster).ToList();

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

        return (Probe.Fit(fitting, scoring, Words.Length), features.Count);
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
    /// one code at a time behind a gate wanting twenty misses first, so a sound scope tens of
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
        var every = Drawings(seed: 1);

        output.WriteLine($"{Words.Length} words, {every.Count} drawings, bare codes only");
        output.WriteLine(
            $"{"tiling",-10}{"bare",8}{"always",8}{"at 1",7}{"at 2",7}{"at 8",7}"
            + $"{"at any",8}{"least",7}{"own",8}");

        var named = new Dictionary<int, int>();
        var specific = new Dictionary<int, double>();

        foreach (var tile in new[] { 3, 4, 6, 8 })
        {
            var bare = Bare(every, tile);
            var read = Naming(bare, Words.Length);

            named[tile] = read.Depths.Count;
            specific[tile] = read.Own;

            output.WriteLine(
                $"{"tiling " + tile,-10}{bare.SelectMany(one => one.Codes).ToHashSet().Count,8}"
                + $"{read.Always,8:F1}"
                + $"{read.Depths.Count(depth => depth <= 1),7}"
                + $"{read.Depths.Count(depth => depth <= 2),7}"
                + $"{read.Depths.Count(depth => depth <= 8),7}"
                + $"{read.Depths.Count,8}{read.Least,7}{read.Own,8:F1}");
        }

        var best = named.MaxBy(one => one.Value);

        output.WriteLine(
            $"the best tiling names {best.Value} of {Words.Length} words soundly, at "
            + $"{best.Key} pixels a patch");

        Assert.True(best.Value > 0,
            "the conjunction of every always-present bare code still admits another word's "
            + $"drawing, for all {Words.Length} words at every patch size. So no conjunction "
            + "over bare codes separates a moved word AT ANY DEPTH, which is rung one's "
            + "ceiling rather than a search failure -- fork 107 would need a name minted over "
            + "co-firing patches before it needed a world to bind in");

        // And the mechanism, as one quantity rather than as a story. What a conjunction has
        // to work with is the codes that survive the offset AND are not shared by every
        // word, which is the `own` column. The first draft of this asserted that a failing
        // size is one where the survivors are universal; the 3-pixel row refused it, because
        // that size fails the other way -- its codebook holds 81 parts over the whole world,
        // so a part is common to too many words while being universal to none. Two accounts,
        // one column, and this is the column.
        Assert.True(specific[best.Key] == specific.Values.Max(),
            "the patch size that names words does not hold the most word-specific codes "
            + "through the offset, so what a conjunction has to work with is not what "
            + "decides whether a size works, and the account here is wrong");

        foreach (var (tile, count) in named)
            if (count == 0)
                Assert.True(specific[tile] < specific[best.Key] / 4.0,
                    $"a {tile}-pixel patch names no word while leaving {specific[tile]:F1} "
                    + $"word-specific codes through the offset, against "
                    + $"{specific[best.Key]:F1} for the size that works. That is close enough "
                    + "that the column is not what separates them");
    }

    /// <summary>
    /// <b>How far a conjunction's reach survives a bigger vocabulary</b>, at the one patch
    /// size that reaches anything.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The number that decides how big the crossing's world may be.</b> Nine words in
    /// sixteen is a share of THESE words until something says how it moves. A soundness
    /// condition gets strictly harder as words are added, because every new word is another
    /// drawing a scope must not admit.
    /// </para>
    /// <para>
    /// <b>Four pixels a patch and nothing else</b>, since the reading above says the other
    /// three separate nothing at any depth. Sweeping a size that names nought would be four
    /// rows of nought wearing a curve.
    /// </para>
    /// <para>
    /// <b>The drop condition was the count rising or holding flat</b>, written before the
    /// grid ran, and the blocking one was the count FALLING as words are added. The second is
    /// what happened, so what this fixes is the SIZE of the world rather than whether there
    /// is one — and the assertions hold the reading rather than the guess.
    /// </para>
    /// </remarks>
    [Fact]
    public void How_far_a_conjunction_reaches_as_the_vocabulary_grows()
    {
        const int Tile = 4;

        var bare = Bare(Drawings(seed: 1, Lettering.Vocabulary), Tile);

        output.WriteLine($"{Tile} pixels a patch, every offset drawn");
        output.WriteLine($"{"words",-8}{"named",8}{"share",8}{"least",8}{"own",8}");

        var reach = new Dictionary<int, int>();
        var deep = new Dictionary<int, int>();

        foreach (var words in new[] { 2, 4, 8, 16, 32, 64 })
        {
            // The first `words` of the list, which is a prefix and never a choice. Picking
            // the words that separate is the experimenter putting the answer in, and this
            // file's whole standing rests on the split being about position and not words.
            var read = Naming(bare.Where(one => one.Word < words).ToList(), words);

            reach[words] = read.Depths.Count;
            deep[words] = read.Least;

            output.WriteLine(
                $"{words,-8}{read.Depths.Count,8}{(double)read.Depths.Count / words,8:F2}"
                + $"{read.Least,8}{read.Own,8:F1}");
        }

        // THE READING, and the blocking condition written above is the one that fired. The
        // count rises to thirty-two words and turns over by sixty-four while the depth needed
        // climbs the whole way, so a conjunction over bare tiling codes does not scale with a
        // vocabulary. What that changes is the SIZE of the world rather than whether there is
        // one: sixteen words is a crossing a conjunction can carry most of, and a world built
        // on a hundred would read a front end's ceiling as a learner's failure to bind.
        //
        // Asserted rather than printed, so a front end that fixes this cannot do it quietly.
        // Either half going green is news and the record above has to be re-read.
        Assert.True(reach[64] < reach[32],
            $"a conjunction names {reach[64]} of sixty-four words against {reach[32]} of "
            + "thirty-two, so the count no longer turns over. The front end has changed and "
            + "the account of why fork 107's world is sixteen words wide is stale");

        Assert.True(deep[64] > deep[16],
            $"a sound scope needs {deep[64]} codes at sixty-four words and {deep[16]} at "
            + "sixteen, so depth no longer climbs with the vocabulary. That was the other "
            + "half of why this does not scale, and it is the half repair pays for");
    }

    /// <summary>Every drawing as the codes that survive being moved.</summary>
    /// <param name="every">The drawings, with the word each is of.</param>
    /// <param name="tile">How many pixels across one patch is.</param>
    /// <remarks>
    /// <b>The front end's own arm rather than a filter written here.</b> A placed code pins a
    /// patch, so a word drawn two pixels along emits none of the ones it emitted before, and
    /// a conjunction over them could only ever be sound for a word that never moved. Asking
    /// <see cref="Tiling"/> for the bare half is what stops this reading and the probe's
    /// disagreeing about what bare means.
    /// </remarks>
    private static List<(HashSet<ulong> Codes, int Word)> Bare(
        List<(IReadOnlyList<double> Pixels, int Word, bool Shown)> every, int tile)
    {
        var tiling = new Tiling(Patch, Lettering.Side, tile, placed: false);

        return every
            .Select(one => (
                Codes: tiling.Codify(one.Pixels).Select(code => code.Value).ToHashSet(),
                one.Word))
            .ToList();
    }

    /// <summary>
    /// How much of a vocabulary a sound conjunction can name, and how deep it has to be.
    /// </summary>
    /// <param name="bare">Every drawing, as the codes that survive an offset.</param>
    /// <param name="words">How many words the vocabulary holds.</param>
    /// <returns>
    /// The depth each separable word needed, the median of those, how many codes survive an
    /// offset for the average word, and how many of those it does not share with every word.
    /// </returns>
    private static (List<int> Depths, int Least, double Always, double Own) Naming(
        List<(HashSet<ulong> Codes, int Word)> bare, int words)
    {
        var depths = new List<int>();
        var always = 0.0;

        // What EVERY word has in every drawing, which is the control on the account of why a
        // patch size fails. If the codes that survive an offset are the ones nobody is
        // distinguished by, this set and the always-set are the same size -- and that is a
        // measurement rather than a story about blank patches.
        HashSet<ulong>? common = null;

        for (var word = 0; word < words; word++)
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

            // Which of the other words' drawings each candidate still admits. A conjunction
            // fires where every one of its codes is present, so a pair admits exactly the
            // drawings both of its codes admit.
            var through = kept.ToDictionary(
                code => code,
                code => theirs
                    .Select((one, at) => (one, at))
                    .Where(pair => pair.one.Contains(code))
                    .Select(pair => pair.at)
                    .ToHashSet());

            // The exact answer, and it is what turns a greedy nought into a statement. A
            // conjunction of EVERY always-present code is the deepest sound scope this word
            // has, so if that one still admits another word's drawing then no conjunction
            // over bare codes separates it at any depth whatsoever.
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

        return (
            depths,
            depths.Count == 0 ? 0 : depths.Order().ElementAt(depths.Count / 2),
            always / words,
            (always / words) - common!.Count);
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
