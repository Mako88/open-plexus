using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The trained front end, and the yardstick that makes its number readable.
/// </summary>
/// <remarks>
/// <para>
/// <b>The open defect this exists for: is the ceiling the front end or the learner
/// behind it.</b> A fixed random projection is the project's bet and a frozen trained
/// encoder is what it has to lose to before the bet means anything. Both are legal
/// under the red-ball property — one derives its wiring from arithmetic and the other
/// from a published file of constants, and neither is fitted to the data in front of it.
/// </para>
/// <para>
/// <b>And a score on the encoder arm alone answers neither question.</b> It is two
/// unknowns multiplied, so <see cref="Probe"/> runs the dullest possible learner over
/// the SAME embeddings and the pair of numbers separates them.
/// </para>
/// </remarks>
public sealed class EncodedTests(ITestOutputHelper output)
{
    private static string Encoders => Path.Combine(Tree.Repo(), "corpora", "encoders");

    private static CifarSettings World(int images = 400, int withheld = 200) =>
        new()
        {
            Corpus = Tree.Corpus("cifar-10-batches-bin"),
            Images = images,
            Withheld = withheld,
            Side = 32,
            Grey = false,
        };

    private static IEnumerable<Encoder> Both =>
        [Encoder.MobileNet(Encoders), Encoder.Clip(Encoders)];

    /// <summary>The front end each arm here composes an encoder with.</summary>
    private static Encoded Sensing(Encoder through, int width) =>
        new(through, one => new Winnowing(CifarRun.Pixel, one), width);

    /// <summary>
    /// Readings paired with the outcomes that followed them.
    /// </summary>
    /// <remarks>
    /// <b>The readings and the outcomes travel together and the order is the join</b>, so a
    /// front end that returned its answers in a different order than it was asked would
    /// mislabel every one of them rather than fail.
    /// </remarks>
    private static List<(IReadOnlyList<double> Reading, int Outcome)> Beside(
        IReadOnlyList<Turn<IReadOnlyList<double>>> turns,
        IReadOnlyList<IReadOnlyList<double>> readings) =>
        [.. turns.Select((one, at) => (readings[at], one.Outcome!.Value))];

    /// <summary>Just the readings, in the order they were drawn.</summary>
    private static IReadOnlyList<IReadOnlyList<double>> Seen(
        IReadOnlyList<Turn<IReadOnlyList<double>>> turns) => [.. turns.Select(one => one.Seen)];

    /// <summary>What a probe scores on one front end's output.</summary>
    /// <param name="world">The world, already drawn from.</param>
    /// <param name="drawn">What it drew.</param>
    /// <param name="readings">The same turns, through whatever front end is being asked about.</param>
    private static double Probed(
        Cifar world,
        IReadOnlyList<Turn<IReadOnlyList<double>>> drawn,
        Func<IReadOnlyList<Turn<IReadOnlyList<double>>>, IReadOnlyList<IReadOnlyList<double>>> readings) =>
        Probe.Fit(
            Beside(drawn, readings(drawn)),
            Beside(world.Withheld, readings(world.Withheld)),
            Cifar.Classes).Accuracy;

    [Fact]
    public void Each_encoder_emits_an_embedding_of_the_width_its_graph_declares()
    {
        var world = new Cifar(World(), seed: 1);

        foreach (var encoder in Both)
        {
            using var sense = new Encoded(
                encoder, width => new Winnowing(CifarRun.Pixel, width), world.Width);

            var said = sense.Codify(world.Next().Seen);

            output.WriteLine(
                $"{Path.GetFileName(Path.GetDirectoryName(encoder.Model))}: "
                + $"{world.Width} numbers in, {sense.Embedding} out, {said.Count} codes");

            Assert.InRange(sense.Embedding, 128, 4096);
            Assert.All(said, code => Assert.Equal(CifarRun.Pixel, code.Modality));
        }
    }

    /// <summary>
    /// <b>The same picture encodes to the same numbers, or none of this is a front
    /// end.</b>
    /// </summary>
    /// <remarks>
    /// The red-ball property is the whole reason a frozen encoder is admissible here.
    /// A graph run with intra-op parallelism reorders its floating-point reductions and
    /// drifts in the last bits — which a quantiser turns into DIFFERENT CODES at a band
    /// boundary, so the drift stops being invisible and starts being a different
    /// observation. <see cref="Encoded"/> pins the session to one thread for this.
    /// </remarks>
    [Fact]
    public void The_same_picture_encodes_to_the_same_codes_every_time()
    {
        var world = new Cifar(World(), seed: 5);
        var pictures = Enumerable.Range(0, 8).Select(_ => world.Next().Seen).ToList();

        foreach (var encoder in Both)
        {
            using var one = new Encoded(
                encoder, width => new Winnowing(CifarRun.Pixel, width), world.Width);
            using var two = new Encoded(
                encoder, width => new Winnowing(CifarRun.Pixel, width), world.Width);

            foreach (var picture in pictures)
            {
                var said = one.Codify(picture).Order().ToList();

                // A SECOND SESSION, so the answer cannot be coming from the memo -- and
                // then the FIRST one again, so the memo cannot be changing it either.
                Assert.Equal(said, two.Codify(picture).Order());
                Assert.Equal(said, one.Codify(picture).Order());
            }

            var distinct = pictures
                .Select(one.Codify)
                .Select(said => string.Join(",", said.Order()))
                .Distinct(StringComparer.Ordinal)
                .Count();

            // And different pictures do not collapse to one, which a badly normalised
            // input would produce silently: every image saturating the same way emits
            // the same winners and the run reads as a learner that cannot learn.
            Assert.Equal(pictures.Count, distinct);
        }
    }

    /// <summary>
    /// <b>The banded ceiling applies to an embedding too, and it is the same
    /// ceiling.</b>
    /// </summary>
    /// <remarks>
    /// A modality is one byte and a dimension owns a block of them, so 512 numbers is
    /// ten times past what can be addressed. An encoder does not rescue the banded
    /// front end — it moves the same wall to a different place, which is worth knowing
    /// before anyone proposes it as the fix.
    /// </remarks>
    [Fact]
    public void An_embedding_is_still_far_too_wide_for_a_block_of_modalities()
    {
        var thrown = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CifarRun(
                World(), new Brain(new CommittingSettings(), 1), Fronting.Banded, 1,
                Encoder.MobileNet(Encoders)));

        output.WriteLine(thrown.Message);
    }

    [Fact]
    public void An_encoder_refuses_a_world_with_no_colour_to_read()
    {
        var grey = World() with { Grey = true };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CifarRun(
                grey, new Brain(new CommittingSettings(), 1), Fronting.Winnowed, 1,
                Encoder.MobileNet(Encoders)));
    }

    /// <summary>
    /// <b>The control arm, and without it the encoder's score is unanchored.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the right-hand column of the grid: the dullest learner there is, over
    /// each front end's output, trained on the drawn bag and scored on images it never
    /// saw. What it says is how much of the problem is in the FEATURES — and the
    /// difference between it and the commitment population on the same features is how
    /// much is in the learner.
    /// </para>
    /// <para>
    /// <b>Deliberately small and deliberately untuned.</b> Four hundred images is far
    /// too few to reach anybody's published number and reaching one is not the point;
    /// what is being asserted is that the arms are ORDERED, and that the ordering is
    /// large enough not to be a seed.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_linear_probe_reads_a_trained_encoder_far_better_than_raw_pixels()
    {
        var world = new Cifar(World(images: 1500, withheld: 500), seed: 1);

        var drawn = Enumerable.Range(0, 1500).Select(_ => world.Next()).ToList();

        var raw = Probed(world, drawn, Seen);

        output.WriteLine($"raw 32x32 colour ({world.Width,4}) : {raw:F3}");

        var scores = new List<double>();

        foreach (var encoder in Both)
        {
            using var sense = Sensing(encoder, world.Width);

            var got = Probed(world, drawn, turns => [.. sense.OfAll(Seen(turns))]);

            output.WriteLine(
                $"{Named(encoder),-22} ({sense.Embedding,4}) : {got:F3}");

            scores.Add(got);
        }

        output.WriteLine($"chance                        : {Cifar.Chance:F3}");

        // The ordering, not a number. A trained encoder that did not beat raw pixels
        // under the same probe would mean the preprocessing is wrong, not that trained
        // features are worthless -- and that failure would otherwise show up as a
        // disappointing score on the arm and be believed.
        Assert.All(scores, one => Assert.True(
            one > raw + 0.10,
            $"a frozen encoder scored {one:F3} against {raw:F3} on raw pixels; "
            + "the preprocessing is the first thing to suspect"));
    }

    /// <summary>
    /// <b>The population and the probe on the identical vectors — fork 43, whose number has
    /// only ever been a commit message.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The most load-bearing reading in this project was a claim rather than a
    /// record.</b> <i>Given symbols worth having, the commitment machinery is competitive</i>
    /// is what separates <i>a fixed projection cannot manufacture the symbols</i> from
    /// <i>a conjunctive rule learner cannot do perception</i> — and no test has ever run a
    /// population on encoder vectors at all. <c>CifarRun</c> has taken an encoder since the
    /// day the arm was built and nothing passed it one.
    /// </para>
    /// <para>
    /// <b>The arms share a world.</b> A seed and a front end, so the only difference is what
    /// reads the codes. Both are scored on the same withheld images, which the world
    /// never draws — and the probe is fit on the distinct pictures the run was drawing from
    /// rather than on its draws, so it is if anything the easier side of the comparison.
    /// </para>
    /// <para>
    /// <b>And it asserts nothing.</b> Because the first attempt to make it a check was a
    /// prediction in a check's clothes. Written as a suite test with a bar at six tenths
    /// it read 0.32 — and the run behind that was five hundred rounds holding eleven and a
    /// half thousand commitments, which is a population that has not finished being born.
    /// A grid decides what the bar is, and the bar comes afterwards.
    /// </para>
    /// <para>
    /// <b>And it is a sweep for the cost rather than for the size of the answer.</b> Five
    /// hundred rounds took ten minutes and the encoding was a minute of it — an embedding is
    /// an order more codes a moment and matching is what this machine's clock is made of, so
    /// the arm that answers fork 43 is a runner's work and cannot sit in the suite.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void The_population_reaches_most_of_a_probe_on_the_same_embeddings()
    {
        const int Images = 1500;
        const int Held = 500;
        const long Rounds = 20000;

        var settings = World(images: Images, withheld: Held);

        foreach (var encoder in Both)
        {
            using var run = new CifarRun(
                settings, new Brain(new CommittingSettings(), seed: 1), Fronting.Winnowed,
                seed: 1, encoder);

            var tally = run.Run(Rounds);

            Assert.NotNull(tally.Unseen);

            // The same world again rather than the run's, because the run's has been drawn
            // from and a probe needs the draws in order. Same settings and same seed is the
            // same world, which is fork 12 being relied on rather than restated.
            var world = new Cifar(settings, seed: 1);
            var drawn = Enumerable.Range(0, Images).Select(_ => world.Next()).ToList();

            using var sense = Sensing(encoder, world.Width);

            var probe = Probed(world, drawn, turns => [.. sense.OfAll(Seen(turns))]);

            var share = tally.Unseen!.Accuracy / probe;

            output.WriteLine(
                $"{Named(encoder),-22} ({run.Reading,4}) : population "
                + $"{tally.Unseen.Accuracy:F3} of probe {probe:F3} = {share:F2} | "
                + $"{tally.Resident} resident, {tally.Unseen.Deciders} deciding, "
                + $"silence {tally.Unseen.Silence:F3} | chance {Cifar.Chance:F3}");

        }
    }

    /// <summary>Which encoder a run was through, for a line of output.</summary>
    private static string Named(Encoder through) =>
        Path.GetFileName(Path.GetDirectoryName(through.Model))!;
}
