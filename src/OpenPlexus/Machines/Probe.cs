namespace OpenPlexus.Machines;

/// <summary>What a linear probe got, and how much it was shown.</summary>
internal sealed record Probed
{
    /// <summary>Observations it was fitted on.</summary>
    public required int Trained { get; init; }

    /// <summary>Observations it was scored on, which it never saw.</summary>
    public required int Tested { get; init; }

    /// <summary>The share of those it got right.</summary>
    public required double Accuracy { get; init; }
}

/// <summary>
/// The dullest thing that could work on a set of features, as a yardstick.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is a control arm and not a competitor</b>, and without it the encoder
/// measurement cannot be read. The open defect asks whether the ceiling is the
/// front end or the learner behind it. A commitment population scoring 0.6 on CLIP
/// features answers neither question on its own: 0.6 could be an excellent front end
/// under a weak learner or the reverse, and the two are indistinguishable from one
/// number. Running the simplest possible learner over the SAME features separates them.
/// </para>
/// <para>
/// <b>The published number is not a substitute for it.</b> A linear probe on frozen
/// CLIP scores about 95% on CIFAR-10 — through a different preprocessing pipeline, at
/// full resolution, over the full training set, with a solver nobody here is running.
/// Holding this project's score against somebody else's setup measures the difference
/// between two setups. The bar has to be measured on the same bench or it is decoration.
/// </para>
/// <para>
/// <b>And it is allowed to train, because it is not the architecture.</b> C4 forbids
/// the MACHINE depending on a train-then-test boundary. A yardstick is not the machine
/// — it is a thing the experimenter runs to find out how hard the problem is, and if
/// it were held to the architecture's constraints it would stop being a yardstick and
/// become a second unmeasured learner.
/// </para>
/// <para>
/// <b>Multinomial logistic regression by plain SGD</b>, which is the standard linear
/// probe and is deliberately not tuned. A probe that had been optimised would move the
/// bar for reasons that have nothing to do with the features.
/// </para>
/// </remarks>
internal static class Probe
{
    /// <summary>
    /// Fits a softmax classifier on one set of readings and scores it on another.
    /// </summary>
    /// <param name="train">Readings to fit on, with their outcomes.</param>
    /// <param name="test">Readings to score on, which it never sees while fitting.</param>
    /// <param name="outcomes">How many outcomes there are.</param>
    /// <param name="passes">How many times to walk the training set.</param>
    /// <param name="rate">The step size.</param>
    /// <param name="seed">The shuffle's generator.</param>
    /// <exception cref="ArgumentException">A set is empty or the widths disagree.</exception>
    /// <remarks>
    /// <b>The shuffle is seeded and the walk is ordered</b>, so two runs of this agree
    /// exactly. A yardstick that moved between readings of it would put its own
    /// spread into every comparison it was used for, and fork 12 is what that costs.
    /// </remarks>
    public static Probed Fit(
        IReadOnlyList<(IReadOnlyList<double> Reading, int Outcome)> train,
        IReadOnlyList<(IReadOnlyList<double> Reading, int Outcome)> test,
        int outcomes,
        int passes = 30,
        double rate = 0.05,
        int seed = 1)
    {
        ArgumentNullException.ThrowIfNull(train);
        ArgumentNullException.ThrowIfNull(test);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(outcomes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(passes);

        if (train.Count == 0 || test.Count == 0)
            throw new ArgumentException("a probe needs something to fit and something to score");

        var width = train[0].Reading.Count;

        if (train.Concat(test).Any(one => one.Reading.Count != width))
            throw new ArgumentException(
                $"every reading must be {width} wide, and one was not", nameof(train));

        var (fitting, fitted) = Dense(train, width);
        var (scoring, scored) = Dense(test, width);

        var weights = new double[outcomes][];
        for (var outcome = 0; outcome < outcomes; outcome++) weights[outcome] = new double[width];

        var bias = new double[outcomes];

        var order = Enumerable.Range(0, train.Count).ToArray();
        var shuffle = new Random(seed);
        var scores = new double[outcomes];

        for (var pass = 0; pass < passes; pass++)
        {
            // FISHER-YATES FROM A SEEDED GENERATOR. Walking the set in corpus order
            // would fit the last class hardest, and CIFAR's batches are not shuffled.
            for (var at = order.Length - 1; at > 0; at--)
            {
                var swap = shuffle.Next(at + 1);
                (order[at], order[swap]) = (order[swap], order[at]);
            }

            foreach (var at in order)
            {
                var reading = fitting[at];
                var outcome = fitted[at];

                Softmax(weights, bias, reading, scores);

                for (var which = 0; which < outcomes; which++)
                {
                    var error = scores[which] - (which == outcome ? 1.0 : 0.0);

                    if (error == 0.0) continue;

                    var row = weights[which];

                    for (var feature = 0; feature < width; feature++)
                        row[feature] -= rate * error * reading[feature];

                    bias[which] -= rate * error;
                }
            }
        }

        var right = 0;

        for (var at = 0; at < scoring.Length; at++)
        {
            Softmax(weights, bias, scoring[at], scores);

            var best = 0;
            for (var which = 1; which < outcomes; which++)
                if (scores[which] > scores[best]) best = which;

            if (best == scored[at]) right++;
        }

        return new Probed
        {
            Trained = train.Count,
            Tested = test.Count,
            Accuracy = right / (double)test.Count,
        };
    }

    /// <summary>
    /// The readings copied out into flat arrays, with their outcomes beside them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The interface was the cost</b>, and it was two thirds of the slowest test in the
    /// suite. The fit is 30 passes over every reading against every outcome, so a
    /// 3,072-wide raw CIFAR probe indexes a reading about two billion times — and every
    /// one of those went through <see cref="IReadOnlyList{T}"/>, which is a virtual call
    /// the JIT cannot inline, cannot elide a bounds check on, and cannot hoist anything
    /// out of. Copied once into <see cref="double"/>[] it is ordinary array indexing.
    /// </para>
    /// <para>
    /// <b>And it is a copy rather than a cleverer loop</b> because the arithmetic must not
    /// move. The same values are added in the same order, so every score this probe
    /// has ever reported is reproduced to the last bit — which is the only kind of
    /// speed-up a yardstick is allowed to have. Vectorising the dot product would be
    /// faster still and would re-associate the sum, and a probe that changed its answer
    /// when it got quicker would put its own drift into every comparison it anchors.
    /// </para>
    /// </remarks>
    private static (double[][] Readings, int[] Outcomes) Dense(
        IReadOnlyList<(IReadOnlyList<double> Reading, int Outcome)> set, int width)
    {
        var readings = new double[set.Count][];
        var outcomes = new int[set.Count];

        for (var at = 0; at < set.Count; at++)
        {
            var (reading, outcome) = set[at];
            var row = new double[width];

            for (var feature = 0; feature < width; feature++) row[feature] = reading[feature];

            readings[at] = row;
            outcomes[at] = outcome;
        }

        return (readings, outcomes);
    }

    /// <summary>The class probabilities for one reading, into a reused buffer.</summary>
    /// <remarks>
    /// <b>The maximum is subtracted before exponentiating</b>, which is the standard
    /// guard: an unshifted softmax overflows to infinity on confident weights and the
    /// probe then reports NaN rather than a score.
    /// </remarks>
    private static void Softmax(
        double[][] weights, double[] bias, double[] reading, double[] into)
    {
        var highest = double.NegativeInfinity;

        for (var which = 0; which < into.Length; which++)
        {
            var row = weights[which];
            var total = bias[which];

            for (var feature = 0; feature < reading.Length; feature++)
                total += row[feature] * reading[feature];

            into[which] = total;
            highest = Math.Max(highest, total);
        }

        var sum = 0.0;

        for (var which = 0; which < into.Length; which++)
        {
            into[which] = Math.Exp(into[which] - highest);
            sum += into[which];
        }

        for (var which = 0; which < into.Length; which++) into[which] /= sum;
    }
}
