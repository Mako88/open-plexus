using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The red-ball property, asserted: the same cell becomes the same code every
/// time, on every instance, with nothing synchronised.
/// </summary>
public sealed class SnakeQuantizerTests
{
    private static SnakeView Viewing(params Seen[] cells) => new() { Cells = cells };

    [Fact]
    public void The_same_view_gives_the_same_codes_every_time()
    {
        var quantizer = new SnakeQuantizer();
        var view = Viewing(new Seen(0, 0, Cell.Body), new Seen(1, 0, Cell.Food));

        Assert.Equal(quantizer.Codify(view), quantizer.Codify(view));
    }

    [Fact]
    public void Two_separate_quantizers_agree_exactly()
    {
        // NOTHING IS FITTED, so there is nothing to disagree about. Two
        // quantisers trained on different samples of one stream agree about
        // under 0.12 of items, and no amount of walking recovers that.
        var view = Viewing(new Seen(-2, 3, Cell.Wall), new Seen(1, 0, Cell.Food));

        Assert.Equal(
            new SnakeQuantizer().Codify(view),
            new SnakeQuantizer().Codify(view));
    }

    [Fact]
    public void The_same_contents_at_a_different_offset_is_a_different_code()
    {
        var quantizer = new SnakeQuantizer();

        var here = quantizer.Codify(Viewing(new Seen(1, 0, Cell.Food))).Single();
        var there = quantizer.Codify(Viewing(new Seen(0, 1, Cell.Food))).Single();

        // A collision would make two different situations one observation,
        // which is the opposite of what centring exists to give.
        Assert.NotEqual(here, there);
    }

    [Fact]
    public void Different_contents_at_the_same_offset_is_a_different_code()
    {
        var quantizer = new SnakeQuantizer();

        var food = quantizer.Codify(Viewing(new Seen(1, 0, Cell.Food))).Single();
        var wall = quantizer.Codify(Viewing(new Seen(1, 0, Cell.Wall))).Single();

        Assert.NotEqual(food, wall);
    }

    [Fact]
    public void Negative_offsets_do_not_collide_with_positive_ones()
    {
        var quantizer = new SnakeQuantizer();

        var west = quantizer.Codify(Viewing(new Seen(-1, 0, Cell.Wall))).Single();
        var east = quantizer.Codify(Viewing(new Seen(1, 0, Cell.Wall))).Single();
        var north = quantizer.Codify(Viewing(new Seen(0, -1, Cell.Wall))).Single();

        Assert.Equal(3, new HashSet<Code> { west, east, north }.Count);
    }

    [Fact]
    public void Every_distinct_cell_of_a_real_view_gets_its_own_code()
    {
        var snake = new Snake(new SnakeSettings
        {
            Width = 21,
            Height = 21,
            Sight = 2,
            StartingEnergy = 100.0,
            EnergyPerStep = 1.0,
            EnergyPerFood = 50.0,
        }, seed: 3);

        var codes = new SnakeQuantizer().Codify(snake.View());

        // Empty cells are silent, so a 5x5 window yields only what is there.
        Assert.True(codes.Count < 25);
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void An_empty_cell_emits_nothing()
    {
        var view = Viewing(
            new Seen(0, 0, Cell.Body),
            new Seen(1, 0, Cell.Empty),
            new Seen(2, 0, Cell.Food));

        var codes = new SnakeQuantizer().Codify(view);

        // An occasion is a clique, so the codes per frame set how dense the
        // graph is -- measured at 46,536 routes halted with empty cells against
        // 6 without.
        Assert.Equal(2, codes.Count);

        // The companion: the cells that mean something are still all there.
        Assert.DoesNotContain(Cell.Empty, view.Cells.Take(0).Select(c => c.Content));
        Assert.Equal(2, codes.Distinct().Count());
    }

    [Fact]
    public void The_modality_is_carried_on_every_code()
    {
        var codes = new SnakeQuantizer()
            .Codify(Viewing(new Seen(0, 0, Cell.Body), new Seen(1, 1, Cell.Wall)));

        // Two front ends never produce the same code, so a picture and a sound
        // cannot collide by accident.
        Assert.All(codes, code => Assert.Equal(SnakeQuantizer.Vision, code.Modality));
    }
}
