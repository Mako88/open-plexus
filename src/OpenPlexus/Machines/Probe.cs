namespace OpenPlexus.Machines;

/// <summary>What a linear probe got, and how much it was shown.</summary>
public sealed record Probed
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
/// <b>THIS IS A CONTROL ARM AND NOT A COMPETITOR, AND WITHOUT IT THE ENCODER
/// MEASUREMENT CANNOT BE READ.</b> The open defect asks whether the ceiling is the
/// front end or the learner behind it. A commitment population scoring 0.6 on CLIP
/// features answers neither question on its own: 0.6 could be an excellent front end
/// under a weak learner or the reverse, and the two are indistinguishable from one
/// number. Running the simplest possible learner over the SAME features separates them.
/// </para>
/// <para>
/// <b>THE PUBLISHED NUMBER IS NOT A SUBSTITUTE FOR IT.</b> A linear probe on frozen
/// CLIP scores about 95% on CIFAR-10 — through a different preprocessing pipeline, at
/// full resolution, over the full training set, with a solver nobody here is running.
/// Holding this project's score against somebody else's setup measures the difference
/// between two setups. The bar has to be measured on the same bench or it is decoration.
/// </para>
/// <para>
/// <b>AND IT IS ALLOWED TO TRAIN, BECAUSE IT IS NOT THE ARCHITECTURE.</b> C4 forbids
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
public static class Probe
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
    /// <b>THE SHUFFLE IS SEEDED AND THE WALK IS ORDERED, so two runs of this agree
    /// exactly.</b> A yardstick that moved between readings of it would put its own
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
                var (reading, outcome) = train[at];

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

        foreach (var (reading, outcome) in test)
        {
            Softmax(weights, bias, reading, scores);

            var best = 0;
            for (var which = 1; which < outcomes; which++)
                if (scores[which] > scores[best]) best = which;

            if (best == outcome) right++;
        }

        return new Probed
        {
            Trained = train.Count,
            Tested = test.Count,
            Accuracy = right / (double)test.Count,
        };
    }

    /// <summary>The class probabilities for one reading, into a reused buffer.</summary>
    /// <remarks>
    /// <b>The maximum is subtracted before exponentiating</b>, which is the standard
    /// guard: an unshifted softmax overflows to infinity on confident weights and the
    /// probe then reports NaN rather than a score.
    /// </remarks>
    private static void Softmax(
        double[][] weights, double[] bias, IReadOnlyList<double> reading, double[] into)
    {
        var highest = double.NegativeInfinity;

        for (var which = 0; which < into.Length; which++)
        {
            var row = weights[which];
            var total = bias[which];

            for (var feature = 0; feature < reading.Count; feature++)
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
