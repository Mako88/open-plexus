using Xunit.Abstractions;

namespace Unseen;

/// <summary>
/// Does a frozen encoder tell a symbolic learner what to invent?
/// </summary>
/// <remarks>
/// <para>
/// Three ways of choosing the condition a repair adds, crossed with whether the vectors mean
/// anything, over twenty held-out splits. Everything else is identical between cells: the
/// world, the stream, the repair trigger, the separation criterion, and the budget of two
/// hundred children a run.
/// </para>
/// <para>
/// One cell should light up. If more than one does, the encoder is not what is doing the work;
/// if none does, the idea is refuted for the price of an afternoon.
/// </para>
/// </remarks>
public sealed class UnseenTests(ITestOutputHelper output)
{
    private const int Steps = 1200;
    private const int RepairEvery = 50;
    private const int HeldOutEach = 6;
    private const int Splits = 20;
    private const int Seed = 20260829;

    [Fact]
    public void The_encoder_separates_the_two_kinds_at_all()
    {
        // The experiment is impossible if it does not, and that would be worth knowing before
        // reading anything into the arms. This asks the encoder directly, which is the one
        // question in this file that is not about the learner.
        using var encoder = new Encoder();
        var things = Nouns.All(encoder, Labelling.Real, out var dropped);

        output.WriteLine($"words the vocabulary does not hold whole: {dropped.Length}"
            + (dropped.Length == 0 ? "" : $" ({string.Join(", ", dropped)})"));

        var right = 0;
        var total = 0;

        for (var split = 0; split < Splits; split++)
        {
            var (train, test) = Nouns.Split(things, HeldOutEach, Seed + split);

            var direction = Encoder.Unit(Centroid(train.Where(one => one.Pours))
                .Zip(Centroid(train.Where(one => !one.Pours)), (a, b) => a - b)
                .ToArray());

            var cut = (train.Where(one => one.Pours).Average(one => Dot(direction, one))
                + train.Where(one => !one.Pours).Average(one => Dot(direction, one))) / 2;

            right += test.Count(one => (Dot(direction, one) > cut) == one.Pours);
            total += test.Count;
        }

        output.WriteLine(
            $"a centroid direction from the training words puts {right}/{total} held-out words "
            + $"on the correct side across {Splits} splits ({(double)right / total:0.000})");

        Assert.True(right > total * 0.6,
            "the encoder cannot separate these two kinds, so nothing downstream can either");
    }

    [Fact]
    public void One_arm_transfers_to_words_it_has_never_seen()
    {
        using var encoder = new Encoder();

        // The vectors do not depend on the split, so the encoder runs forty times rather than
        // forty-eight hundred.
        var vocabulary = new[] { Labelling.Real, Labelling.Opaque }
            .ToDictionary(one => one, one => Nouns.All(encoder, one, out _));

        var arms = new[] { Proposing.Present, Proposing.ByEncoder, Proposing.ByChance };
        var scores = new Dictionary<(Labelling, Proposing), List<double>>();
        var made = new Dictionary<(Labelling, Proposing), List<int>>();

        foreach (var labelling in vocabulary.Keys)
            foreach (var arm in arms)
            {
                scores[(labelling, arm)] = [];
                made[(labelling, arm)] = [];
            }

        for (var split = 0; split < Splits; split++)
        {
            foreach (var labelling in vocabulary.Keys)
                foreach (var arm in arms)
                {
                    var (score, rules) = Run(vocabulary[labelling], arm, Seed + split);
                    scores[(labelling, arm)].Add(score);
                    made[(labelling, arm)].Add(rules);
                }
        }

        output.WriteLine(
            $"{Splits} held-out splits, {Steps} steps each, {HeldOutEach * 2} unseen words a "
            + "split, chance is 0.500");
        output.WriteLine("");
        output.WriteLine($"{"labels",-8} {"arm",-11} {"mean",6} {"worst",6} {"best",6} {"rules",6}");

        foreach (var labelling in vocabulary.Keys)
            foreach (var arm in arms)
            {
                var cell = scores[(labelling, arm)];
                output.WriteLine(
                    $"{labelling,-8} {arm,-11} {cell.Average(),6:0.000} {cell.Min(),6:0.000} "
                    + $"{cell.Max(),6:0.000} {made[(labelling, arm)].Average(),6:0}");
            }

        var encoded = scores[(Labelling.Real, Proposing.ByEncoder)];
        var present = scores[(Labelling.Real, Proposing.Present)];
        var chance = scores[(Labelling.Real, Proposing.ByChance)];
        var opaque = scores[(Labelling.Opaque, Proposing.ByEncoder)];

        var beatsChance = encoded.Zip(chance, (a, b) => a > b).Count(one => one);
        var beatsOpaque = encoded.Zip(opaque, (a, b) => a > b).Count(one => one);

        output.WriteLine("");
        output.WriteLine($"encoder over remembering : {encoded.Average() - present.Average():+0.000;-0.000}");
        output.WriteLine($"encoder over chance      : {encoded.Average() - chance.Average():+0.000;-0.000}"
            + $"  (won {beatsChance}/{Splits} splits)");
        output.WriteLine($"real over opaque         : {encoded.Average() - opaque.Average():+0.000;-0.000}"
            + $"  (won {beatsOpaque}/{Splits} splits)");

        Assert.True(encoded.Average() > present.Average() + 0.15,
            "a region minted from the encoder did not beat a condition drawn from codes "
            + "already present, which is the whole claim");

        Assert.True(encoded.Average() > chance.Average() + 0.15,
            "a region minted from the encoder did not beat one minted from a direction drawn "
            + "from nothing, so the geometry is not what is doing the work");

        Assert.True(encoded.Average() > opaque.Average() + 0.15,
            "the same arm did as well on vectors that mean nothing, so something other than "
            + "the encoder is carrying the result");
    }

    private static (double Score, int Rules) Run(
        IReadOnlyList<Thing> things,
        Proposing arm,
        int seed)
    {
        var (train, test) = Nouns.Split(things, HeldOutEach, seed);
        var containers = Nouns.ContainerCodes();

        var learner = new Learner(arm, seed);
        learner.Live(new World(train, containers).Steps(Steps, seed), RepairEvery);

        var exam = new World(test, containers).Exam().ToList();
        var right = exam.Count(step => learner.Says(step) == step.Next);

        return ((double)right / exam.Count, learner.Rules);
    }

    private static float[] Centroid(IEnumerable<Thing> things)
    {
        var all = things.ToList();
        var centroid = new float[all[0].Vector.Length];

        foreach (var one in all)
            for (var d = 0; d < centroid.Length; d++) centroid[d] += one.Vector[d] / all.Count;

        return centroid;
    }

    private static double Dot(float[] direction, Thing thing)
    {
        var total = 0.0;
        for (var d = 0; d < direction.Length; d++) total += direction[d] * thing.Vector[d];
        return total;
    }
}
