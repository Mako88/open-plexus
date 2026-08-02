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
        var quantizer = new SnakeQuantizer(includeEmpty: true);
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
            new SnakeQuantizer(includeEmpty: true).Codify(view),
            new SnakeQuantizer(includeEmpty: true).Codify(view));
    }

    [Fact]
    public void The_same_contents_at_a_different_offset_is_a_different_code()
    {
        var quantizer = new SnakeQuantizer(includeEmpty: true);

        var here = quantizer.Codify(Viewing(new Seen(1, 0, Cell.Food))).Single();
        var there = quantizer.Codify(Viewing(new Seen(0, 1, Cell.Food))).Single();

        // A collision would make two different situations one observation,
        // which is the opposite of what centring exists to give.
        Assert.NotEqual(here, there);
    }

    [Fact]
    public void Different_contents_at_the_same_offset_is_a_different_code()
    {
        var quantizer = new SnakeQuantizer(includeEmpty: true);

        var food = quantizer.Codify(Viewing(new Seen(1, 0, Cell.Food))).Single();
        var wall = quantizer.Codify(Viewing(new Seen(1, 0, Cell.Wall))).Single();

        Assert.NotEqual(food, wall);
    }

    [Fact]
    public void Negative_offsets_do_not_collide_with_positive_ones()
    {
        var quantizer = new SnakeQuantizer(includeEmpty: true);

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

        var codes = new SnakeQuantizer(includeEmpty: true).Codify(snake.View());

        Assert.Equal(25, codes.Count);
        Assert.Equal(25, codes.Distinct().Count());
    }

    [Fact]
    public void Withholding_empty_drops_only_the_empty_cells()
    {
        var view = Viewing(
            new Seen(0, 0, Cell.Body),
            new Seen(1, 0, Cell.Empty),
            new Seen(2, 0, Cell.Food));

        var withEmpty = new SnakeQuantizer(includeEmpty: true).Codify(view);
        var without = new SnakeQuantizer(includeEmpty: false).Codify(view);

        Assert.Equal(3, withEmpty.Count);

        // Both arms exist because which is right is an open question. The
        // companion half: the non-empty codes are untouched by the choice.
        Assert.Equal(2, without.Count);
        Assert.All(without, code => Assert.Contains(code, withEmpty));
    }

    [Fact]
    public void The_modality_is_carried_on_every_code()
    {
        var codes = new SnakeQuantizer(includeEmpty: true)
            .Codify(Viewing(new Seen(0, 0, Cell.Body), new Seen(1, 1, Cell.Wall)));

        // Two front ends never produce the same code, so a picture and a sound
        // cannot collide by accident.
        Assert.All(codes, code => Assert.Equal(SnakeQuantizer.Vision, code.Modality));
    }
}
