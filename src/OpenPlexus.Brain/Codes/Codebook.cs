using System.Globalization;

namespace OpenPlexus.Codes;

/// <summary>
/// A published quantisation of a frozen encoder's space, computed once over English.
/// </summary>
/// <remarks>
/// <para>
/// The escape the refutation table names. A quantiser fitted per machine is refused because
/// two machines fitted on different samples code the same input differently, and the revival
/// condition written beside it is a codebook reaching the same answer from any sample order.
/// This is that: the sample is a fixed slice of the encoder's own published vocabulary in the
/// order the file gives it, the seed is fixed and the iteration count is fixed, so the answer
/// does not depend on what any machine happened to see.
/// </para>
/// <para>
/// It is fitted to ENGLISH and never to a world. The words it clusters are the commonest few
/// thousand of the published vocabulary, so a world's own alphabet is a few dozen of them and
/// moving to another world does not move a single centroid. That is the same standing the
/// encoder itself has, and it is what separates this from a codebook fitted to the data in
/// front of it.
/// </para>
/// <para>
/// Drawn hyperplanes were tried first and are refuted: cells cut that way track an alphabet's
/// kinds no better than the base rate, because the direction that separates two kinds is one
/// direction and a drawn plane in a few hundred dimensions is very nearly at right angles to
/// it. Clustering looks at where the words actually are, which is the difference.
/// </para>
/// <para>
/// Several grains, because how fine a useful neighbourhood is depends on how far apart the
/// words are and nothing here knows that. A word carries one code per grain and the coarse one
/// entails the fine one, which is the gradient subsumption already reads.
/// </para>
/// </remarks>
public sealed class Codebook
{
    /// <summary>The modality a codebook cell is said in.</summary>
    /// <remarks>
    /// Its own, so a cell can never collide with a word, a grouping or a question.
    /// </remarks>
    public const byte Cell = 46;

    /// <summary>How many words the codebook is computed over.</summary>
    /// <remarks>
    /// The commonest of the published vocabulary, taken in file order. WordPiece builds its
    /// vocabulary by frequency, so a prefix of it is the common words without a second corpus
    /// having to be read to find out which those are.
    /// </remarks>
    public const int Words = 6_000;

    /// <summary>How many cells at each grain.</summary>
    /// <remarks>
    /// Sixty-four cells over six thousand words is about ninety words a cell and a thousand is
    /// about six, so the three span from a broad kind down to a handful of near-synonyms. What
    /// grain a repair wants is the thing being measured rather than something to pick.
    /// </remarks>
    public static IReadOnlyList<int> Grains => [64, 256, 1_024];

    /// <summary>How many passes the clustering takes.</summary>
    /// <remarks>
    /// Fixed rather than run to convergence, because <i>converged</i> is a threshold on a
    /// float and this has to give the same answer on every machine forever. Fifteen passes on
    /// a space this size moves the last one very little.
    /// </remarks>
    private const int Passes = 15;

    private readonly List<float[][]> _centroids = [];

    /// <param name="encoder">The frozen encoder whose space this quantises.</param>
    /// <param name="cache">Where the computed centroids are kept between runs.</param>
    /// <remarks>
    /// Cached because it is the same answer every time and costs a minute to reach. The cache
    /// is a convenience and never an input: deleting it changes nothing about what comes back,
    /// which is what makes it safe under the rule a memo has to satisfy.
    /// </remarks>
    public Codebook(Worded encoder, string cache)
    {
        ArgumentNullException.ThrowIfNull(encoder);
        ArgumentNullException.ThrowIfNull(cache);

        if (File.Exists(cache))
        {
            Read(cache);
            return;
        }

        var vectors = Vocabulary(encoder).Select(encoder.Of).ToArray();

        foreach (var cells in Grains) _centroids.Add(Cluster(vectors, cells));

        Write(cache);
    }

    /// <summary>
    /// The words the codebook is computed over, in the order the published file gives them.
    /// </summary>
    /// <param name="encoder">The encoder whose vocabulary it is.</param>
    /// <remarks>
    /// Lowercase, three letters or more, and no word-piece continuations. The order is the
    /// file's rather than sorted, because the file's order is the frequency order this relies
    /// on to mean *the common words* and it is as canonical as sorting is.
    /// </remarks>
    public static IReadOnlyList<string> Vocabulary(Worded encoder)
    {
        ArgumentNullException.ThrowIfNull(encoder);

        return [.. encoder.Tokens
            .Where(one => one.Length >= 3 && one.All(char.IsAsciiLetterLower))
            .Take(Words)];
    }

    /// <summary>
    /// The cells one vector falls in, coarsest first.
    /// </summary>
    /// <param name="vector">A reading from the same encoder.</param>
    public IReadOnlyList<Code> Cells(float[] vector)
    {
        ArgumentNullException.ThrowIfNull(vector);

        var cells = new List<Code>(_centroids.Count);

        for (var grain = 0; grain < _centroids.Count; grain++)
        {
            var nearest = Nearest(_centroids[grain], vector);

            var hash = Hashing.Fold(
                Hashing.Fold(Hashing.Basis, (ulong)Grains[grain]), (ulong)nearest);

            cells.Add(new Code(Cell, Hashing.Mix(hash)));
        }

        return cells;
    }

    /// <summary>Which centroid a vector is closest to.</summary>
    /// <param name="centroids">The centroids of one grain.</param>
    /// <param name="vector">The reading.</param>
    /// <remarks>
    /// Ties go to the lower index, which matters rather than being tidy: a tie broken by
    /// whichever came first in a dictionary walk is reproducible in one process and arbitrary
    /// across two, and that is a trap this repo already has written down.
    /// </remarks>
    private static int Nearest(float[][] centroids, float[] vector)
    {
        var nearest = 0;
        var closest = float.MaxValue;

        for (var one = 0; one < centroids.Length; one++)
        {
            var distance = 0f;

            for (var d = 0; d < vector.Length; d++)
            {
                var apart = centroids[one][d] - vector[d];
                distance += apart * apart;
            }

            if (distance >= closest) continue;

            closest = distance;
            nearest = one;
        }

        return nearest;
    }

    /// <summary>
    /// Lloyd's algorithm from a seeded spread-out start.
    /// </summary>
    /// <param name="vectors">What is being clustered.</param>
    /// <param name="cells">How many centroids.</param>
    /// <remarks>
    /// <para>
    /// The start is k-means++ off a fixed seed rather than a draw, because the result has to be
    /// the same file on every machine. An empty cell keeps its centroid rather than being
    /// reseeded, so the pass count alone decides the answer.
    /// </para>
    /// <para>
    /// Assignment is parallel over POINTS, which are independent of one another and are written
    /// to their own slots. That is a different parallelism from the one that reorders a
    /// floating-point reduction, and the sum inside a point is untouched.
    /// </para>
    /// </remarks>
    private static float[][] Cluster(float[][] vectors, int cells)
    {
        var width = vectors[0].Length;
        var drawn = new Random(1);

        var centroids = new float[cells][];
        centroids[0] = [.. vectors[drawn.Next(vectors.Length)]];

        var spread = new float[vectors.Length];
        Array.Fill(spread, float.MaxValue);

        for (var one = 1; one < cells; one++)
        {
            var total = 0.0;

            for (var at = 0; at < vectors.Length; at++)
            {
                var distance = 0f;

                for (var d = 0; d < width; d++)
                {
                    var apart = centroids[one - 1][d] - vectors[at][d];
                    distance += apart * apart;
                }

                spread[at] = Math.Min(spread[at], distance);
                total += spread[at];
            }

            var want = drawn.NextDouble() * total;
            var taken = vectors.Length - 1;

            for (var at = 0; at < vectors.Length; at++)
            {
                want -= spread[at];

                if (want > 0) continue;

                taken = at;
                break;
            }

            centroids[one] = [.. vectors[taken]];
        }

        var owner = new int[vectors.Length];

        for (var pass = 0; pass < Passes; pass++)
        {
            Parallel.For(0, vectors.Length, at => owner[at] = Nearest(centroids, vectors[at]));

            var sums = new float[cells][];
            var counts = new int[cells];

            for (var one = 0; one < cells; one++) sums[one] = new float[width];

            for (var at = 0; at < vectors.Length; at++)
            {
                counts[owner[at]]++;

                for (var d = 0; d < width; d++) sums[owner[at]][d] += vectors[at][d];
            }

            for (var one = 0; one < cells; one++)
            {
                if (counts[one] == 0) continue;

                for (var d = 0; d < width; d++) centroids[one][d] = sums[one][d] / counts[one];
            }
        }

        return centroids;
    }

    /// <summary>Reads the centroids back.</summary>
    /// <param name="cache">The file.</param>
    private void Read(string cache)
    {
        using var reading = new StreamReader(cache);

        foreach (var cells in Grains)
        {
            var grain = new float[cells][];

            for (var one = 0; one < cells; one++)
            {
                grain[one] =
                [
                    .. (reading.ReadLine() ?? throw new InvalidDataException(
                            $"'{cache}' is short of a centroid. Delete it and it is rebuilt."))
                        .Split(' ')
                        .Select(each => float.Parse(each, CultureInfo.InvariantCulture)),
                ];
            }

            _centroids.Add(grain);
        }
    }

    /// <summary>Writes the centroids out.</summary>
    /// <param name="cache">The file.</param>
    private void Write(string cache)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(cache)!);

        using var writing = new StreamWriter(cache);

        foreach (var grain in _centroids)
            foreach (var one in grain)
                writing.WriteLine(string.Join(
                    ' ', one.Select(each => each.ToString("R", CultureInfo.InvariantCulture))));
    }
}
