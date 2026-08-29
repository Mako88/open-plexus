using Xunit.Abstractions;

namespace Unseen;

/// <summary>
/// Does a frozen encoder tell a symbolic learner what to invent, and where does it stop?
/// </summary>
/// <remarks>
/// Three readings. Whether the encoder arm beats its controls at all; how far down a ladder of
/// less and less lexicalised properties it keeps working; and how much of the win is the
/// geometry rather than the arm simply having been given fewer candidates to sift.
/// </remarks>
public sealed class UnseenTests(ITestOutputHelper output)
{
    private const int Steps = 1200;
    private const int RepairEvery = 50;
    private const int Splits = 20;
    private const int Seed = 20260829;

    private static readonly Proposing[] Arms =
        [Proposing.Present, Proposing.ByEncoder, Proposing.ByChance];

    [Fact]
    public void The_encoder_separates_each_kind_by_itself()
    {
        // What a linear probe gets when it is handed the labels, which is the ceiling the
        // learner is working towards without them. A rung the probe cannot do is a rung
        // nothing downstream can do either, and knowing which is which is the point of the
        // ladder.
        using var encoder = new Encoder();

        output.WriteLine($"{"property",-10} {"probe",6}  held-out words placed correctly");

        foreach (var study in Nouns.Ladder())
        {
            var things = Nouns.All(encoder, study, Labelling.Real);
            var right = 0;
            var total = 0;

            for (var split = 0; split < Splits; split++)
            {
                var (train, test) = Nouns.Held(things, study.HeldOutEach, Seed + split);

                var direction = Encoder.Unit(Centroid(train.Where(one => one.Pours))
                    .Zip(Centroid(train.Where(one => !one.Pours)), (a, b) => a - b)
                    .ToArray());

                var cut = (train.Where(one => one.Pours).Average(one => Dot(direction, one))
                    + train.Where(one => !one.Pours).Average(one => Dot(direction, one))) / 2;

                right += test.Count(one => (Dot(direction, one) > cut) == one.Pours);
                total += test.Count;
            }

            output.WriteLine($"{study.Name,-10} {(double)right / total,6:0.000}  {right}/{total}");
        }

        var pours = Probe(encoder, Nouns.Pours);
        var arbitrary = Probe(encoder, Nouns.Arbitrary);

        Assert.True(pours > 0.6, "the encoder cannot separate liquids from solids");

        Assert.True(arbitrary < 0.6,
            "a partition of the same words by nothing at all is being predicted, which would "
            + "mean the probe is reading something other than the property");
    }

    [Fact]
    public void One_arm_transfers_to_words_it_has_never_seen()
    {
        using var encoder = new Encoder();

        var vocabulary = new[] { Labelling.Real, Labelling.Opaque }
            .ToDictionary(one => one, one => Nouns.All(encoder, Nouns.Pours, one));

        var scores = Grid(vocabulary, Nouns.Pours);

        output.WriteLine(
            $"{Splits} held-out splits, {Steps} steps each, "
            + $"{Nouns.Pours.HeldOutEach * 2} unseen words a split, chance is 0.500");
        output.WriteLine("");
        output.WriteLine($"{"labels",-8} {"arm",-11} {"mean",6} {"worst",6} {"best",6}");

        foreach (var ((labelling, arm), cell) in scores)
        {
            output.WriteLine(
                $"{labelling,-8} {arm,-11} {cell.Average(),6:0.000} {cell.Min(),6:0.000} "
                + $"{cell.Max(),6:0.000}");
        }

        var encoded = scores[(Labelling.Real, Proposing.ByEncoder)];
        var present = scores[(Labelling.Real, Proposing.Present)];
        var chance = scores[(Labelling.Real, Proposing.ByChance)];
        var opaque = scores[(Labelling.Opaque, Proposing.ByEncoder)];

        output.WriteLine("");
        output.WriteLine($"encoder over remembering : {encoded.Average() - present.Average():+0.000;-0.000}");
        output.WriteLine($"encoder over chance      : {encoded.Average() - chance.Average():+0.000;-0.000}"
            + $"  (won {Won(encoded, chance)}/{Splits})");
        output.WriteLine($"real over opaque         : {encoded.Average() - opaque.Average():+0.000;-0.000}"
            + $"  (won {Won(encoded, opaque)}/{Splits})");

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

    [Fact]
    public void The_borrowed_prior_runs_out_where_language_does()
    {
        // The ladder. `pours` and `alive` are things English has a word for; `thin` needs two
        // properties at once; `letter` is real, learnable and not a meaning; `arbitrary` is the
        // same forty words split by nothing. Where the encoder arm falls to its controls is
        // where the borrowed prior stops, and that boundary is the most useful number here.
        using var encoder = new Encoder();

        output.WriteLine($"{Splits} splits a rung, chance is 0.500");
        output.WriteLine("");
        output.WriteLine(
            $"{"property",-10} {"probe",6} {"present",8} {"encoder",8} {"chance",7} {"gap",7} {"won",5}");

        var reached = new Dictionary<string, (double Encoder, double Chance)>();

        foreach (var study in Nouns.Ladder())
        {
            var things = Nouns.All(encoder, study, Labelling.Real);
            var cells = Arms.ToDictionary(arm => arm, arm => Runs(things, study, arm, 200));

            var encoded = cells[Proposing.ByEncoder];
            var chance = cells[Proposing.ByChance];
            reached[study.Name] = (encoded.Average(), chance.Average());

            output.WriteLine(
                $"{study.Name,-10} {Probe(encoder, study),6:0.000} "
                + $"{cells[Proposing.Present].Average(),8:0.000} {encoded.Average(),8:0.000} "
                + $"{chance.Average(),7:0.000} {encoded.Average() - chance.Average(),7:+0.000;-0.000} "
                + $"{Won(encoded, chance),5}");
        }

        Assert.True(reached["pours"].Encoder > reached["pours"].Chance + 0.15,
            "the top of the ladder stopped working, which would refute the reading the ladder "
            + "was built to extend");

        Assert.True(reached["arbitrary"].Encoder < reached["arbitrary"].Chance + 0.15,
            "a partition by nothing at all is being transferred to unseen words, so the arm is "
            + "not reading meaning out of the encoder and something else explains every rung "
            + "above");
    }

    [Fact]
    public void The_control_catches_up_when_its_budget_grows()
    {
        // How much of the win is the geometry, and how much is the arm having been handed
        // fewer candidates to sift. A chance direction that fits the training words will
        // partly transfer, because the vectors have real structure and a direction that fits
        // thirty of them correlates with the one that matters. More draws, more of that.
        using var encoder = new Encoder();
        var things = Nouns.All(encoder, Nouns.Pours, Labelling.Real);

        const int Fewer = 10;
        int[] budgets = [200, 800, 3200, 12800];

        output.WriteLine($"{Fewer} splits a budget, chance is 0.500");
        output.WriteLine("");
        output.WriteLine($"{"budget",7} {"chance",7} {"encoder",8} {"gap",7}");

        var gaps = new List<(int Budget, double Gap)>();

        foreach (var budget in budgets)
        {
            var chance = Runs(things, Nouns.Pours, Proposing.ByChance, budget, Fewer);
            var encoded = Runs(things, Nouns.Pours, Proposing.ByEncoder, budget, Fewer);
            var gap = encoded.Average() - chance.Average();

            gaps.Add((budget, gap));
            output.WriteLine(
                $"{budget,7} {chance.Average(),7:0.000} {encoded.Average(),8:0.000} {gap,7:+0.000;-0.000}");
        }

        output.WriteLine("");
        output.WriteLine(
            "A gap that shrinks as the budget grows means part of the win was the arm needing "
            + "fewer draws. A gap that holds means the geometry is doing it.");

        Assert.True(gaps[^1].Gap > 0,
            $"a chance direction caught the encoder at a budget of {gaps[^1].Budget}, so the "
            + "arm's advantage is a search-budget advantage rather than a geometric one");
    }

    private static Dictionary<(Labelling, Proposing), List<double>> Grid(
        Dictionary<Labelling, IReadOnlyList<Thing>> vocabulary,
        Study study)
    {
        var scores = new Dictionary<(Labelling, Proposing), List<double>>();

        foreach (var labelling in vocabulary.Keys)
            foreach (var arm in Arms)
                scores[(labelling, arm)] = Runs(vocabulary[labelling], study, arm, 200);

        return scores;
    }

    private static List<double> Runs(
        IReadOnlyList<Thing> things,
        Study study,
        Proposing arm,
        int budget,
        int splits = Splits)
    {
        var scores = new List<double>();

        for (var split = 0; split < splits; split++)
        {
            var seed = Seed + split;
            var (train, test) = Nouns.Held(things, study.HeldOutEach, seed);
            var containers = Nouns.ContainerCodes();

            var learner = new Learner(arm, seed, budget);
            learner.Live(new World(train, containers).Steps(Steps, seed), RepairEvery);

            var exam = new World(test, containers).Exam().ToList();
            scores.Add((double)exam.Count(step => learner.Says(step) == step.Next) / exam.Count);
        }

        return scores;
    }

    /// <summary>What a linear probe reaches on one study when it is handed the labels.</summary>
    private static double Probe(Encoder encoder, Study study)
    {
        var things = Nouns.All(encoder, study, Labelling.Real);
        var right = 0;
        var total = 0;

        for (var split = 0; split < Splits; split++)
        {
            var (train, test) = Nouns.Held(things, study.HeldOutEach, Seed + split);

            var direction = Encoder.Unit(Centroid(train.Where(one => one.Pours))
                .Zip(Centroid(train.Where(one => !one.Pours)), (a, b) => a - b)
                .ToArray());

            var cut = (train.Where(one => one.Pours).Average(one => Dot(direction, one))
                + train.Where(one => !one.Pours).Average(one => Dot(direction, one))) / 2;

            right += test.Count(one => (Dot(direction, one) > cut) == one.Pours);
            total += test.Count;
        }

        return (double)right / total;
    }

    private static int Won(List<double> arm, List<double> control) =>
        arm.Zip(control, (a, b) => a > b).Count(one => one);

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
