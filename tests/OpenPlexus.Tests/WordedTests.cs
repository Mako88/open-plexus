using OpenPlexus.Codes;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What a frozen word encoder knows about the spine world's alphabet, before anything learns.
/// </summary>
/// <remarks>
/// <para>
/// The traps list says a front-end arm has a ceiling computable with no learning and that it
/// costs milliseconds against a runner's hour, so it is taken first. The arm this prices mints
/// a repair condition standing for a region of the encoder's space; if no region of that space
/// separates the house's kinds of word, the arm is dead and nothing has to run to find out.
/// </para>
/// <para>
/// The house says its words as hashes, so <c>kitchen</c> and <c>garden</c> are exactly as
/// unrelated as <c>kitchen</c> and <c>apple</c>. Nothing in the world ever states that four of
/// its words are rooms. That is the regularity a direction through the encoder would carry and
/// the alphabet cannot.
/// </para>
/// <para>
/// The direction is the difference of the two class means and the cut is the midpoint between
/// them, which is what a repair partitioning its parent's hits from its misses would have to
/// work with. A stronger fit would price a mechanism nobody proposes to build.
/// </para>
/// </remarks>
public sealed class WordedTests(ITestOutputHelper output)
{
    private static string Encoders => Path.Combine(Tree.Repo(), "corpora", "encoders");

    /// <summary>A house holding every room, thing and person the world can draw.</summary>
    /// <remarks>
    /// Widest rather than the spine's six and four, because what is priced here is the
    /// alphabet and a narrower house would ask the question of fewer words than the encoder
    /// would face.
    /// </remarks>
    private static Roaming House() =>
        new(
            new RoamingSettings { Rooms = 8, Props = 8, People = 8, Steps = 1, Asked = 0, Chatting = 0 },
            seed: 1);

    /// <summary>
    /// The words of one of the house's kinds, taken from the world rather than written here.
    /// </summary>
    /// <param name="house">The house whose alphabet it is.</param>
    /// <param name="kind">The codes of that kind, on <c>Roaming.Named</c>'s standing.</param>
    /// <remarks>
    /// A second copy of the lists would be a second place they are said, and the world's own
    /// are the ones a run would meet. This inverts the alphabet the world published instead.
    /// </remarks>
    private static string[] Words(Roaming house, IReadOnlyList<Code> kind)
    {
        var spelling = new Dictionary<Code, string>();

        for (var at = 0; at < house.Vocabulary.Count; at++)
            if (house.Meaning(at) is { } code) spelling[code] = house.Vocabulary[at];

        return [.. kind.Select(one => spelling[one])];
    }

    /// <summary>
    /// The words that hold a sentence together, which the world does not publish as a kind.
    /// </summary>
    /// <remarks>
    /// Whatever the house says that is not a room, a thing or a person. Taken as a remainder
    /// so that a word added to the world joins this class rather than being silently left out
    /// of the study.
    /// </remarks>
    private static string[] Rest(Roaming house, params IReadOnlyList<Code>[] kinds)
    {
        var known = kinds.SelectMany(one => one).ToHashSet();

        var rest = new List<string>();

        for (var at = 0; at < house.Vocabulary.Count; at++)
            if (house.Meaning(at) is { } code && !known.Contains(code))
                rest.Add(house.Vocabulary[at]);

        return [.. rest];
    }

    /// <summary>
    /// Every word the spine world speaks is one token of the published vocabulary.
    /// </summary>
    /// <remarks>
    /// A word the encoder has to split is a word it says nothing about here, and a study that
    /// silently dropped those would be reporting on whichever words happened to survive. This
    /// fails rather than narrows.
    /// </remarks>
    [Fact]
    public void The_encoder_holds_every_word_the_house_says_whole()
    {
        using var encoder = new Worded(Encoders);

        var house = House();

        var split = new List<string>();

        foreach (var word in house.Vocabulary)
            if (!encoder.Knows(word)) split.Add(word);

        Assert.True(split.Count == 0,
            $"the published vocabulary splits {string.Join(", ", split)}, so the encoder says "
            + "nothing about them and a reading here would be about the rest");
    }

    /// <summary>
    /// A direction through the encoder tells the house's kinds of word apart on words it was
    /// not shown, and the same arithmetic on vectors with no meaning in them does not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Leave one out, so the word being judged took no part in the direction that judges it.
    /// With eight words a class that is sixteen readings a pair, and the whole thing is one
    /// pass of the encoder over thirty-five words.
    /// </para>
    /// <para>
    /// The control is the identical computation on unit vectors drawn from nothing, which is
    /// the arm this repo would otherwise have credited the encoder for. Chance is a half.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_direction_through_the_encoder_separates_kinds_of_word_it_was_not_shown()
    {
        using var encoder = new Worded(Encoders);

        var house = House();

        var places = Words(house, house.Named);
        var things = Words(house, house.Called);
        var cast = Words(house, house.Walking);
        var grammar = Rest(house, house.Named, house.Called, house.Walking);

        (string Name, string[] Left, string[] Right)[] pairs =
        [
            ("rooms/things", places, things),
            ("rooms/people", places, cast),
            ("things/people", things, cast),
            ("rooms/grammar", places, grammar),
        ];

        output.WriteLine($"{"pair",-16}{"meaning",10}{"opaque",10}{"words",8}");

        var drawn = new Random(1);

        var meanings = new List<double>();
        var opaques = new List<double>();

        foreach (var (name, left, right) in pairs)
        {
            var meaning = Apart(
                [.. left.Select(encoder.Of)], [.. right.Select(encoder.Of)]);

            var opaque = Apart(
                [.. left.Select(_ => Noise(drawn, encoder.Width))],
                [.. right.Select(_ => Noise(drawn, encoder.Width))]);

            output.WriteLine(
                $"{name,-16}{meaning,10:F3}{opaque,10:F3}{left.Length + right.Length,8}");

            meanings.Add(meaning);
            opaques.Add(opaque);
        }

        // Every pair, rather than the best of four. A mean over the four would let one pair
        // the encoder happens to know carry three it does not, and what the arm needs is that
        // a repair can reach for a direction whichever kinds it is trying to tell apart.
        Assert.All(meanings, one => Assert.True(one > 0.80,
            $"a held-out word lands on the right side {one:F3} of the time, and an arm that "
            + "mints a region of this space as a repair condition needs the space to hold the "
            + "distinction before the learner is asked to find it"));

        // And the same arithmetic on nothing must not do it, or what was measured is the
        // leave-one-out procedure rather than the encoder.
        Assert.All(opaques, one => Assert.True(one < 0.70,
            $"vectors with no meaning in them separate at {one:F3}, so this reading is about "
            + "the method rather than about what the encoder knows"));
    }

    /// <summary>
    /// A cell of the encoder's space cut by hyperplanes drawn from nothing says nothing about
    /// the kind of word in it, where a direction through the same space says almost everything.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reading that refutes putting fixed regions of the space in the moment, and it is
    /// the reason the direction above is not enough on its own. The kinds are separable, so
    /// the information is there; a partition that does not look at the data does not find it,
    /// because the direction that separates is one direction and a drawn hyperplane in three
    /// hundred and eighty-four dimensions is very nearly at right angles to it.
    /// </para>
    /// <para>
    /// The pairs that share a cell are counted beside the rate, because the two arms do not
    /// cluster equally. The encoder's vectors sit in a narrow cone and fall in the same cells
    /// far more often than drawn ones do, so the arm has more shared pairs and learns no more
    /// from them.
    /// </para>
    /// <para>
    /// What it leaves is the shape the spike on `architecture` had: a direction minted from a
    /// commitment's own hits and misses, where the failures supply the labels a separating
    /// direction needs. That is repair's to do and a front end cannot.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_drawn_partition_of_the_encoders_space_does_not_reach_what_a_direction_does()
    {
        using var encoder = new Worded(Encoders);

        var house = House();

        var kind = new Dictionary<Code, string>();

        foreach (var one in house.Named) kind[one] = "room";
        foreach (var one in house.Called) kind[one] = "thing";
        foreach (var one in house.Walking) kind[one] = "person";

        var spelling = new Dictionary<Code, string>();

        for (var at = 0; at < house.Vocabulary.Count; at++)
            if (house.Meaning(at) is { } code) spelling[code] = house.Vocabulary[at];

        foreach (var one in spelling.Keys) kind.TryAdd(one, "grammar");

        var drawn = new Random(1);

        var words = spelling
            .Where(one => encoder.Knows(one.Value))
            .OrderBy(one => one.Value, StringComparer.Ordinal)
            .ToList();

        output.WriteLine($"{"arm",-10}{"together",10}{"apart",10}{"base",10}");

        var togethers = new Dictionary<string, double>();
        var baseline = 0.0;

        foreach (var arm in new[] { "meaning", "opaque" })
        {
            var vectors = words
                .Select(one => (one.Key, arm == "meaning"
                    ? encoder.Of(one.Value)
                    : Noise(drawn, encoder.Width)))
                .ToList();

            var near = Cells(vectors, seed: 1);

            var together = 0;
            var shared = 0;
            var same = 0;
            var pairs = 0;

            for (var one = 0; one < words.Count; one++)
            for (var two = one + 1; two < words.Count; two++)
            {
                var alike = kind[words[one].Key] == kind[words[two].Key];

                pairs++;
                if (alike) same++;

                if (!near[words[one].Key].Intersect(near[words[two].Key]).Any()) continue;

                shared++;
                if (alike) together++;
            }

            var rate = together / (double)shared;

            baseline = same / (double)pairs;

            output.WriteLine(
                $"{arm,-10}{rate,10:F3}{shared,10}{baseline,10:F3}");

            togethers[arm] = rate;
        }

        // The finding, asserted so it cannot quietly stop being true. Sharing a cell has to
        // be worth about nothing against the base rate -- and if a change to the cutting ever
        // makes this fail, the arm below is back on the table and this row leaves the
        // refutation table rather than the assertion being relaxed.
        Assert.True(togethers["meaning"] < baseline + 0.10,
            $"words sharing a cell of the encoder's space are now the same kind "
            + $"{togethers["meaning"]:F3} of the time against a base rate of {baseline:F3}, "
            + "so a drawn partition has started carrying something about the alphabet and the "
            + "arm refuted on this reading deserves re-taking");

        // And the direction through the same space does reach it, which is what makes the
        // line above a fact about the PARTITION rather than about the encoder. Both halves
        // have to be in one test or a later change could break the pair and pass.
        Assert.True(Apart(
                [.. house.Named.Select(one => encoder.Of(spelling[one]))],
                [.. house.Called.Select(one => encoder.Of(spelling[one]))]) > 0.80,
            "a direction no longer separates rooms from things either, so this reading is "
            + "about the encoder rather than about how the space was cut");
    }

    /// <summary>
    /// How often a held-out word lands on its own side of the mean-difference cut.
    /// </summary>
    /// <param name="left">One class.</param>
    /// <param name="right">The other.</param>
    private static double Apart(float[][] left, float[][] right)
    {
        var right_ = right;
        var hits = 0;
        var asked = 0;

        for (var side = 0; side < 2; side++)
        {
            var mine = side == 0 ? left : right_;
            var theirs = side == 0 ? right_ : left;

            for (var out_ = 0; out_ < mine.Length; out_++)
            {
                var kept = mine.Where((_, at) => at != out_).ToArray();

                var here = Mean(kept);
                var there = Mean(theirs);

                var direction = new float[here.Length];
                var middle = new float[here.Length];

                for (var d = 0; d < here.Length; d++)
                {
                    direction[d] = here[d] - there[d];
                    middle[d] = (here[d] + there[d]) / 2f;
                }

                var cut = Dot(direction, middle);

                if (Dot(direction, mine[out_]) > cut) hits++;

                asked++;
            }
        }

        return hits / (double)asked;
    }

    /// <summary>The mean of some vectors, componentwise.</summary>
    /// <param name="vectors">The vectors, all one width.</param>
    private static float[] Mean(float[][] vectors)
    {
        var mean = new float[vectors[0].Length];

        foreach (var one in vectors)
            for (var d = 0; d < mean.Length; d++)
                mean[d] += one[d] / vectors.Length;

        return mean;
    }

    /// <summary>The inner product of two vectors.</summary>
    /// <param name="left">One.</param>
    /// <param name="right">The other.</param>
    private static float Dot(float[] left, float[] right)
    {
        var total = 0f;

        for (var d = 0; d < left.Length; d++) total += left[d] * right[d];

        return total;
    }

    /// <summary>How many hyperplanes deep the finest cell is cut.</summary>
    private const int Bits = 5;

    /// <summary>The coarsest cell said, in hyperplanes.</summary>
    /// <remarks>
    /// One hyperplane splits the vocabulary in half, so a code half the words carry is
    /// background rather than a neighbourhood. That was the first shape of the refuted arm and
    /// this is the second, which is the one the reading below is about.
    /// </remarks>
    private const int Coarsest = 2;

    /// <summary>
    /// Each word's cells of a space cut by hyperplanes drawn from nothing, at every grain.
    /// </summary>
    /// <param name="vectors">Each word's code and the vector standing for it.</param>
    /// <param name="seed">Which hyperplanes.</param>
    /// <remarks>
    /// The refuted mechanism, kept here as the instrument that refuted it. A cell is built one
    /// hyperplane at a time and a code taken at each grain from the coarsest on, so two words
    /// share a code exactly when they fall the same side of every plane up to that depth.
    /// </remarks>
    private static Dictionary<Code, List<Code>> Cells(
        IReadOnlyList<(Code Word, float[] Vector)> vectors, int seed)
    {
        var width = vectors[0].Vector.Length;
        var drawn = new Random(seed);

        var planes = new float[Bits][];

        for (var bit = 0; bit < Bits; bit++)
        {
            var plane = new float[width];

            for (var d = 0; d < width; d++) plane[d] = (float)((drawn.NextDouble() * 2.0) - 1.0);

            planes[bit] = plane;
        }

        var cells = new Dictionary<Code, List<Code>>();

        foreach (var (word, vector) in vectors)
        {
            var near = new List<Code>();
            var cell = Hashing.Basis;

            for (var bit = 0; bit < Bits; bit++)
            {
                var total = 0f;

                for (var d = 0; d < width; d++) total += planes[bit][d] * vector[d];

                cell = Hashing.Fold(cell, total >= 0f ? 1UL : 0UL);

                if (bit + 1 >= Coarsest) near.Add(new Code(45, Hashing.Mix(cell)));
            }

            cells[word] = near;
        }

        return cells;
    }

    /// <summary>A unit vector drawn from nothing, of the same width as a reading.</summary>
    /// <param name="drawn">The stream.</param>
    /// <param name="width">How many numbers.</param>
    private static float[] Noise(Random drawn, int width)
    {
        var vector = new float[width];

        for (var d = 0; d < width; d++) vector[d] = (float)((drawn.NextDouble() * 2.0) - 1.0);

        return Worded.Unit(vector);
    }
}
