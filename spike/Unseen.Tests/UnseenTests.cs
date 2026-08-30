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
                var (train, test) = Nouns.Held(things, study, Seed + split);

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

    [Fact]
    public void Every_word_a_study_names_is_one_token()
    {
        // A word the vocabulary does not hold whole is dropped, and the drop is silent. On a
        // study built out of four balanced groups a silent drop unbalances the exam and moves
        // chance away from one half without saying so.
        using var encoder = new Encoder();
        var missing = Nouns.Every().Where(one => !encoder.Knows(one)).ToList();

        Assert.True(missing.Count == 0,
            $"{missing.Count} word(s) are not one token and would be dropped: "
            + string.Join(", ", missing));
    }

    [Fact]
    public void Composing_two_regions_pays_where_one_is_not_enough()
    {
        // The composition test, and it is not the one intended. `either` is alive-or-big-but-
        // not-both over one pool, built so no single direction could read it -- and a probe
        // reads it at better than eight in ten anyway. Four well-separated clusters in three
        // hundred and eighty-four dimensions can be paired any way at all by one hyperplane, so
        // a task that a single region cannot express is not something this pool can be made to
        // contain.
        //
        // What can be measured directly is whether composing pays. A learner capped at one
        // condition may use one region; an uncapped one may condition on its own earlier
        // condition. Everything else is held identical.
        using var encoder = new Encoder();

        output.WriteLine($"{Splits} splits, chance is 0.500");
        output.WriteLine("");
        output.WriteLine(
            $"{"property",-9} {"probe",6} {"present",8} {"chance",7} {"one",6} {"many",6} {"depth",6}");

        var reached = new Dictionary<string, (double One, double Many, double Depth)>();

        foreach (var study in Nouns.Composed())
        {
            var things = Nouns.All(encoder, study, Labelling.Real);

            var one = Runs(things, study, Proposing.ByEncoder, 200, maxScope: 2).Average();
            var many = Runs(things, study, Proposing.ByEncoder, 200).Average();
            var depth = Depth(things, study);

            reached[study.Name] = (one, many, depth);

            output.WriteLine(
                $"{study.Name,-9} {Probe(encoder, study),6:0.000} "
                + $"{Runs(things, study, Proposing.Present, 200).Average(),8:0.000} "
                + $"{Runs(things, study, Proposing.ByChance, 200).Average(),7:0.000} "
                + $"{one,6:0.000} {many,6:0.000} {depth,6:0.00}");
        }

        output.WriteLine("");
        output.WriteLine(
            "one is a learner allowed a single region; many may condition on its own earlier "
            + "condition. depth is the mean scope length of the rule that answered, so two is "
            + "genesis plus one repair.");

        Assert.True(reached["living"].Many > 0.7 && reached["large"].Many > 0.7,
            "the two properties the exclusive or is built from are not individually learnable, "
            + "so nothing measured on the exclusive or would mean anything");

        Assert.True(reached["either"].Depth > reached["living"].Depth,
            "the harder study did not draw deeper rules, so repair is not conditioning on its "
            + "own earlier conditions and there is no composition here to measure");
    }

    [Fact]
    public void A_new_sense_moves_the_ceiling_and_recombining_does_not()
    {
        // The ceiling found on the ladder is the encoder's, and nothing built out of the
        // encoder's own output can pass it -- information absent from a vector cannot be
        // recovered from that vector. What moves the bound is another channel. Spelling carries
        // the letters and nothing about meaning, so it should read `letter` and be useless on
        // `pours`, and having both should read both.
        using var encoder = new Encoder();

        var senses = new[] { Sense.Meaning, Sense.Spelling, Sense.Both };

        output.WriteLine($"{Splits} splits, chance is 0.500. probe / encoder arm.");
        output.WriteLine("");
        output.WriteLine($"{"property",-10} {"meaning",15} {"spelling",15} {"both",15}");

        var reached = new Dictionary<(string, Sense), double>();

        foreach (var study in Nouns.Ladder())
        {
            var cells = senses.Select(sense =>
            {
                var things = Nouns.All(encoder, study, Labelling.Real, sense);
                var arm = Runs(things, study, Proposing.ByEncoder, 200).Average();
                reached[(study.Name, sense)] = arm;
                return $"{Probe(encoder, study, sense),6:0.000} / {arm,6:0.000}";
            });

            output.WriteLine($"{study.Name,-10} {string.Join(" ", cells.Select(one => $"{one,15}"))}");
        }

        output.WriteLine("");
        output.WriteLine(
            "A sense that carries the information moves the bound; a sense that does not "
            + "carry it costs, because both halves are normalised to the same length and the "
            + "silent one is noise at equal weight.");

        Assert.True(reached[("letter", Sense.Spelling)] > reached[("letter", Sense.Meaning)] + 0.05,
            "a channel that carries the letters did not make a property of the letters more "
            + "learnable, so the bound is not where the senses are");

        Assert.True(reached[("pours", Sense.Spelling)] < reached[("pours", Sense.Meaning)] - 0.2,
            "spelling read a property of meaning, so it is not the clean second channel this "
            + "claim needs");

        Assert.True(reached[("pours", Sense.Both)] < reached[("pours", Sense.Meaning)] - 0.05,
            "adding a sense that says nothing about the property cost nothing, which would be "
            + "a happier world than the measured one and would remove the need for anything "
            + "that weighs a channel by whether it is paying");

        Assert.True(reached[("arbitrary", Sense.Both)] < 0.65,
            "two senses together read a partition by nothing at all, which would mean the "
            + "width rather than the content is doing the work");
    }

    [Fact]
    public void A_property_of_a_pair_needs_to_know_which_argument_is_which()
    {
        // The decision point. Two things are compared and the bigger wins, so no fact about
        // either one settles it. The exam asks each held-out thing against as many smaller
        // training things as larger ones, which makes a subject-only rule score one half by
        // construction rather than by luck.
        //
        // `pair` reads the difference between the two arguments. A difference only exists once
        // something says which is subtracted from which, and a moment that is a set of codes
        // cannot say it -- so the gap between `pair` and `encoder` is what roles are worth,
        // priced rather than argued.
        using var encoder = new Encoder();
        var things = Nouns.Sized(encoder);

        const int HeldOut = 5;
        const int Each = 3;

        var arms = new[]
        {
            Proposing.Present, Proposing.ByEncoder, Proposing.ByChance, Proposing.ByPair,
        };

        var scores = arms.ToDictionary(one => one, one => new List<double>());
        var probes = new List<double>();

        for (var split = 0; split < Splits; split++)
        {
            var seed = Seed + split;
            var (train, test) = Nouns.Middle(things, HeldOut, seed);
            var exam = Compared.Exam(test, train, Each, seed).ToList();

            probes.Add(Probe(train, exam));

            foreach (var arm in arms)
            {
                var learner = new Learner(arm, seed, 200);
                learner.Live(new Compared(train).Steps(Steps, seed), RepairEvery);
                scores[arm].Add((double)exam.Count(one => learner.Says(one) == one.Next) / exam.Count);
            }
        }

        output.WriteLine($"{Splits} splits, {HeldOut} held-out things, chance is 0.500");
        output.WriteLine("");
        output.WriteLine($"{"arm",-11} {"mean",6} {"worst",6} {"best",6}");
        output.WriteLine($"{"probe(pair)",-11} {probes.Average(),6:0.000} {probes.Min(),6:0.000} {probes.Max(),6:0.000}");

        foreach (var arm in arms)
        {
            var cell = scores[arm];
            output.WriteLine(
                $"{arm,-11} {cell.Average(),6:0.000} {cell.Min(),6:0.000} {cell.Max(),6:0.000}");
        }

        output.WriteLine("");
        output.WriteLine($"pair over subject-only : "
            + $"{scores[Proposing.ByPair].Average() - scores[Proposing.ByEncoder].Average():+0.000;-0.000}"
            + $"  (won {Won(scores[Proposing.ByPair], scores[Proposing.ByEncoder])}/{Splits})");

        Assert.True(probes.Average() > 0.65,
            "the encoder does not carry size well enough to read a comparison, so nothing "
            + "measured below says anything about roles");

        Assert.True(scores[Proposing.ByEncoder].Average() < 0.65,
            "a region over the subject alone answered a question about a pair, so the exam is "
            + "not balanced and the reading is worthless");

        Assert.True(
            scores[Proposing.ByPair].Average() > scores[Proposing.ByEncoder].Average() + 0.15,
            "reading the pair bought nothing over reading the subject, so this world does not "
            + "show that roles are needed and the case for a representation that has them has "
            + "to be made somewhere else");
    }

    /// <summary>What a linear probe over the difference reaches, handed the labels.</summary>
    private static double Probe(IReadOnlyList<Thing> train, IReadOnlyList<Step> exam)
    {
        var pairs = train.SelectMany(a => train.Where(b => a.Code != b.Code).Select(b => (a, b)))
            .ToList();

        var width = train[0].Vector.Length;
        var direction = new float[width];

        foreach (var (a, b) in pairs)
        {
            var sign = a.Code > b.Code ? 1f : -1f;
            for (var d = 0; d < width; d++)
                direction[d] += sign * (a.Vector[d] - b.Vector[d]) / pairs.Count;
        }

        direction = Encoder.Unit(direction);

        var right = exam.Count(step =>
        {
            var projection = 0f;
            for (var d = 0; d < width; d++)
                projection += direction[d] * (step.Subject.Vector[d] - step.Other!.Vector[d]);

            return (projection > 0) == (step.Next == Compared.Over);
        });

        return (double)right / exam.Count;
    }

    /// <summary>The mean scope length of the rule that answered on the exam.</summary>
    private static double Depth(IReadOnlyList<Thing> things, Study study)
    {
        var depths = new List<double>();

        for (var split = 0; split < Splits; split++)
        {
            var seed = Seed + split;
            var (train, test) = Nouns.Held(things, study, seed);
            var containers = Nouns.ContainerCodes();

            var learner = new Learner(Proposing.ByEncoder, seed, 200);
            learner.Live(new World(train, containers).Steps(Steps, seed), RepairEvery);

            depths.AddRange(new World(test, containers).Exam()
                .Select(step => learner.Chose(step))
                .Where(one => one is not null)
                .Select(one => (double)one!.Scope.Length));
        }

        return depths.Count == 0 ? 0 : depths.Average();
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
        int splits = Splits,
        int maxScope = 99)
    {
        var scores = new List<double>();

        for (var split = 0; split < splits; split++)
        {
            var seed = Seed + split;
            var (train, test) = Nouns.Held(things, study, seed);
            var containers = Nouns.ContainerCodes();

            var learner = new Learner(arm, seed, budget, maxScope);
            learner.Live(new World(train, containers).Steps(Steps, seed), RepairEvery);

            var exam = new World(test, containers).Exam().ToList();
            scores.Add((double)exam.Count(step => learner.Says(step) == step.Next) / exam.Count);
        }

        return scores;
    }

    /// <summary>What a linear probe reaches on one study when it is handed the labels.</summary>
    private static double Probe(Encoder encoder, Study study, Sense sense = Sense.Meaning)
    {
        var things = Nouns.All(encoder, study, Labelling.Real, sense);
        var right = 0;
        var total = 0;

        for (var split = 0; split < Splits; split++)
        {
            var (train, test) = Nouns.Held(things, study, Seed + split);

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
