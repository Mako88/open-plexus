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

        var view = snake.View();
        var codes = new SnakeQuantizer().Codify(view);

        // EVERY CELL EMITS, EMPTY ONES INCLUDED -- John's call, 2026-08-04; the
        // audit that settled it is on `SnakeQuantizer.Modality`. This asserted
        // the opposite until then, and what it is worth asserting NOW is the
        // encoder's own claim: distinct cells cannot collide into one code,
        // because two situations reading as one observation is the exact
        // opposite of what a front end is for.
        Assert.Equal(view.Cells.Count, codes.Count);
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void An_empty_cell_emits_a_code_like_every_other()
    {
        var view = Viewing(
            new Seen(0, 0, Cell.Body),
            new Seen(1, 0, Cell.Empty),
            new Seen(2, 0, Cell.Food));

        var codes = new SnakeQuantizer().Codify(view);

        // THIS ASSERTED THE WITHHOLDING UNTIL JOHN OVERRULED IT, 2026-08-04. An
        // occasion is a clique, so codes per frame set how dense the graph is --
        // 46,536 routes halted with empty cells against 6 without. That was
        // measured under `Best` pricing, where the flood could enumerate every
        // simple path; inverse cost bounds the walk by construction, so the
        // saving is no longer being bought from anything. What withholding COST
        // is what settled it: 47% of steps producing no onset at all over sixty
        // seeds. See `SnakeQuantizer.Modality`.
        Assert.Equal(3, codes.Count);
        Assert.Equal(3, codes.Distinct().Count());

        // AND THE COMPANION THAT USED TO SIT HERE COULD NOT FIRE. It read
        // `view.Cells.Take(0)`, so it asked whether an EMPTY sequence contained
        // an empty cell and was true however the quantiser behaved -- a check
        // wired and unable to fire, which is a named trap in the plan and reads
        // as passing forever. What it meant to say is that nothing was dropped.
        Assert.Equal(view.Cells.Count, codes.Count);
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
