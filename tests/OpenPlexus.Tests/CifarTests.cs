using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The first world here whose readings were never symbols.
/// </summary>
/// <remarks>
/// <b>bAbI SHIPS WORDS, CLEVR SHIPS A SCENE GRAPH, CLUTRR SHIPS THE RELATION AS A
/// COLUMN.</b> Each hands over the front end this architecture claims to replace, so
/// no score on any of them has said anything about the interface. This ships photons,
/// and what a front end can do with them is the whole question.
/// </remarks>
public sealed class CifarTests(ITestOutputHelper output)
{
    private static CifarSettings World(int side = 8, bool grey = true, int images = 2000) =>
        new()
        {
            Corpus = Tree.Corpus("cifar-10-batches-bin"),
            Images = images,
            Withheld = 0,
            Side = side,
            Grey = grey,
        };

    [Fact]
    public void A_reading_is_the_declared_width_and_lies_in_the_unit_range()
    {
        foreach (var (side, grey) in new[] { (4, true), (8, true), (8, false), (32, true) })
        {
            var world = new Cifar(World(side, grey), seed: 1);

            Assert.Equal(side * side * (grey ? 1 : 3), world.Width);

            for (var round = 0; round < 200; round++)
            {
                var turn = world.Next();

                Assert.Equal(world.Width, turn.Seen.Count);
                Assert.All(turn.Seen, one => Assert.InRange(one, 0.0, 1.0));
                Assert.InRange(turn.Outcome!.Value, 0, Cifar.Classes - 1);
            }
        }
    }

    [Fact]
    public void Box_averaging_keeps_the_photons_a_subsample_would_throw_away()
    {
        // THE COARSE READING IS THE MEAN OF THE FINE ONE, which is what says the dial
        // is a loss of RESOLUTION rather than a differently-aliased picture. A
        // subsampling reader would pass every other test in this file and fail this.
        var fine = new Cifar(World(side: 32), seed: 4).Next();
        var coarse = new Cifar(World(side: 8), seed: 4).Next();

        for (var cell = 0; cell < 64; cell++)
        {
            var down = cell / 8;
            var across = cell % 8;

            var block = new List<double>();

            for (var y = down * 4; y < (down + 1) * 4; y++)
            for (var x = across * 4; x < (across + 1) * 4; x++)
                block.Add(fine.Seen[(y * 32) + x]);

            Assert.Equal(block.Average(), coarse.Seen[cell], 10);
        }
    }

    [Fact]
    public void The_same_seed_draws_the_same_stream()
    {
        var one = new Cifar(World(), seed: 9);
        var two = new Cifar(World(), seed: 9);

        for (var round = 0; round < 100; round++)
        {
            var (a, b) = (one.Next(), two.Next());

            Assert.Equal(a.Outcome, b.Outcome);
            Assert.Equal(a.Seen, b.Seen);
        }
    }

    [Fact]
    public void Every_class_is_drawn_and_none_dominates()
    {
        // THE TEST BATCH IS BALANCED BY CONSTRUCTION and the draw is uniform, so a
        // reader that mangled the record stride would show up here as a class that
        // never appears -- which is cheaper to find than as a score nobody can explain.
        var world = new Cifar(World(images: 10_000), seed: 2);
        var seen = new int[Cifar.Classes];

        for (var round = 0; round < 20_000; round++) seen[world.Next().Outcome!.Value]++;

        output.WriteLine(string.Join(", ",
            seen.Select((count, label) => $"{Cifar.Named(label)} {count}")));

        Assert.All(seen, count => Assert.InRange(count, 1600, 2400));
    }

    /// <summary>
    /// <b>THE CEILING, AND IT IS STRUCTURAL RATHER THAN A MATTER OF DEGREE.</b>
    /// </summary>
    /// <remarks>
    /// <see cref="Banded{TFrame}"/> gives every dimension its own block of modalities
    /// and a modality is one byte, so what is left unclaimed by the other worlds is
    /// twenty-six dimensions at two spans. <b>A five-by-five thumbnail is the largest
    /// picture the banded front end can be shown</b> — not the largest it does well
    /// on, the largest it can ADDRESS. <see cref="Winnowing"/> rides every code on one
    /// modality and has no such limit.
    /// </remarks>
    [Fact]
    public void The_banded_arm_cannot_be_pointed_at_a_picture_at_all()
    {
        var thrown = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CifarRun(World(side: 8), new Brain(new CommittingSettings(), 1), Fronting.Banded, 1));

        output.WriteLine(thrown.Message);

        // AND THE COMPANION, or the check above passes for a front end that refuses
        // everything. Four by four is sixteen dimensions and fits with room over.
        _ = new CifarRun(World(side: 4), new Brain(new CommittingSettings(), 1), Fronting.Banded, 1);
    }

    [Fact]
    public void Winnow_has_no_such_ceiling_and_says_the_picture_in_one_modality()
    {
        var sense = new Winnowing(CifarRun.Pixel, 64);
        var world = new Cifar(World(side: 8), seed: 1);

        var said = sense.Codify(world.Next().Seen);

        Assert.All(said, code => Assert.Equal(CifarRun.Pixel, code.Modality));

        // THE COST SIDE, SAID OUT LOUD. One winner per twenty cells over forty cells
        // per dimension is 128 codes for a 64-number reading -- and genesis mints a
        // candidate per live code on every surprise, so this is the number that
        // decides whether the front end is affordable and not the accuracy.
        output.WriteLine($"8x8 grey -> {said.Count} codes in one moment");
        Assert.Equal(128, said.Count);
    }
}
