using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// An examination may never teach, and what is withheld may never be drawn.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE BUDGET FOR THE ONE INSTRUMENT A PERCEPTUAL WORLD CAN HAVE.</b> Soundness by
/// enumeration is the sharp measurement and no world made of photographs will ever
/// support it, so on <see cref="Cifar"/> the held-out score is the whole of the
/// difference between a learner and a lookup table. An examination that moved a single
/// counter would be a second training run wearing the word <i>held-out</i>, and it
/// would read as a PASS — better numbers, no error, nothing to see.
/// </para>
/// <para>
/// <b>AND IT IS EXACTLY THE FAILURE CLASS THIS REPO KEEPS FINDING.</b> A check that
/// cannot fire reads as passing; a leak into a measurement reads as a result. Both are
/// silent, so both get a budget rather than a fix.
/// </para>
/// </remarks>
public sealed class WithheldTests(ITestOutputHelper output)
{
    private static CifarSettings World(int images = 2000, int withheld = 500) =>
        new()
        {
            Corpus = Tree.Corpus("cifar-10-batches-bin"),
            Images = images,
            Withheld = withheld,
            Side = 8,
            Grey = true,
        };

    /// <summary>Everything about one commitment that learning would move.</summary>
    private static string Record(Population held) =>
        string.Join("\n", held.All.Select(one =>
            $"{one.Identity} {one.Hits} {one.Misses} {one.Abstains} {one.Seen} "
            + $"{one.Accuracy:R} {one.Separations.Count}"));

    [Fact]
    public void What_is_withheld_is_never_drawn()
    {
        var world = new Cifar(World(images: 1000, withheld: 500), seed: 3);

        Assert.Equal(500, world.Withheld.Count);
        Assert.Equal(1000, world.Held);

        // COMPARED BY READING AND NOT BY INDEX, because the world hands out readings
        // and an index would only prove the bookkeeping agrees with itself.
        var kept = world.Withheld
            .Select(one => string.Join(",", one.Seen.Select(x => x.ToString("R"))))
            .ToHashSet(StringComparer.Ordinal);

        for (var draw = 0; draw < 20_000; draw++)
        {
            var seen = string.Join(",", world.Next().Seen.Select(x => x.ToString("R")));

            Assert.DoesNotContain(seen, kept);
        }

        output.WriteLine($"20,000 draws, none of the {kept.Count} withheld readings among them");
    }

    [Fact]
    public void An_examination_moves_nothing_in_the_population()
    {
        var brain = new Brain(new CommittingSettings(), 1);
        var world = new Cifar(World(), seed: 1);
        var trial = new Trial<IReadOnlyList<double>>(
            world, new Winnowing(CifarRun.Pixel, world.Width), brain);

        trial.Run(rounds: 3000, sweep: 1000, target: 0.5, window: 500);

        var before = Record(brain.Held);
        var names = brain.Held.Names.Count;
        var count = brain.Held.Count;

        // THREE TIMES, because a mutation that is idempotent would survive one call and
        // a counter that only moves on the first pass is still a counter that moved.
        var said = Enumerable.Range(0, 3).Select(_ => trial.Examine()).ToList();

        Assert.Equal(before, Record(brain.Held));
        Assert.Equal(names, brain.Held.Names.Count);
        Assert.Equal(count, brain.Held.Count);

        // AND THE ANSWER IS THE SAME EVERY TIME, which is the same claim read from the
        // other side: a population that did not move cannot say two different things.
        Assert.All(said, one => Assert.Equal(said[0], one));

        output.WriteLine($"population {count} commitments, {names} names, unchanged over 3 examinations");
    }

    [Fact]
    public void The_held_out_score_is_reported_beside_its_silence()
    {
        var brain = new Brain(new CommittingSettings(), 1);
        var world = new Cifar(World(), seed: 1);
        var trial = new Trial<IReadOnlyList<double>>(
            world, new Winnowing(CifarRun.Pixel, world.Width), brain);

        var tally = trial.Run(rounds: 3000, sweep: 1000, target: 0.5, window: 500);

        var unseen = Assert.IsType<Examined>(tally.Unseen);

        Assert.Equal(500, unseen.Asked);
        Assert.InRange(unseen.Accuracy, 0.0, 1.0);
        Assert.InRange(unseen.Silence, 0.0, 1.0);

        output.WriteLine($"drawn bag  : {tally.Recent:F3} over the last tenth");
        output.WriteLine($"never drawn: {unseen.Accuracy:F3} answered, {unseen.Silence:F3} silent");
        output.WriteLine($"gap        : {tally.Recent - unseen.Accuracy:+0.000;-0.000}");
        output.WriteLine($"chance     : {Cifar.Chance:F3}");
    }

    /// <summary>
    /// <b>NOTHING RATHER THAN ZERO WHERE A WORLD WITHHOLDS NOTHING.</b>
    /// </summary>
    /// <remarks>
    /// A generated world cannot contain its own answer and has nothing to hold back, so
    /// a zero would read as a learner that generalises to nothing — a check that cannot
    /// fire reading as a failure rather than as absent, which is this repo's oldest
    /// trap said the other way round.
    /// </remarks>
    [Fact]
    public void A_world_that_withholds_nothing_reports_nothing_rather_than_zero()
    {
        var run = new GradedRun(
            new GradedSettings { Address = 2 },
            new Brain(new CommittingSettings(), 1),
            Fronting.Winnowed,
            seed: 1);

        Assert.Null(run.Run(rounds: 2000, sweep: 500, target: 0.9, window: 500).Unseen);
    }

    /// <summary>
    /// <b>AND A WORLD THAT CAN WITHHOLD WHILE WITHHOLDING NOTHING REPORTS THE SAME.</b>
    /// </summary>
    /// <remarks>
    /// The distinction above used to be carried by the interface alone — a world either
    /// held things back or did not implement <c>IWithholds</c> — and that stopped being
    /// true the moment the multiplexer gained a dial for it. An examination of an empty
    /// set answers nothing, so every count is nought and the accuracy with them, which
    /// reads as a population generalising to NOTHING rather than as a question nobody
    /// asked.
    /// </remarks>
    [Fact]
    public void A_world_that_could_withhold_and_does_not_reports_nothing_either()
    {
        var run = new MultiplexerRun(
            new MultiplexerSettings { Address = 2 },
            new Brain(new CommittingSettings(), 1),
            seed: 1);

        Assert.Null(run.Run(rounds: 2000, sweep: 500, target: 0.9, window: 500).Tally.Unseen);
    }

    /// <summary>
    /// <b>FORK 48: THE ONE WORLD WHERE DEPTH IS NEEDED CAN HOLD SOMETHING BACK NOW.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// A generated world can withhold without the learner being able to tell, because
    /// there is no boundary to notice — the world simply never emits those assignments,
    /// which is what C4 asks of the MACHINE and says nothing about the experimenter.
    /// </para>
    /// <para>
    /// <b>The assertion is that the instrument exists and is not blind</b>, not what it
    /// reads. A bar written before the first run is a prediction dressed as a check.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_multiplexer_can_hold_assignments_back_and_be_examined_on_them()
    {
        var run = new MultiplexerRun(
            new MultiplexerSettings { Address = 2, Withheld = 16 },
            new Brain(new CommittingSettings(), 1),
            seed: 1);

        var got = run.Run(rounds: 20_000);
        var unseen = got.Tally.Unseen;

        Assert.NotNull(unseen);

        output.WriteLine(
            $"drawn {got.Recent:F3} · withheld {unseen.Accuracy:F3} over {unseen.Asked} "
            + $"assignments, silent {unseen.Silence:F3}, {unseen.Deciders} deciders");

        Assert.Equal(16, unseen.Asked);

        // THE INSTRUMENT IS ARMED RATHER THAN MERELY PRESENT. An examination that answers
        // nothing reports nought and looks exactly like a learner that generalises to
        // nothing, which is the trap the test above is about.
        Assert.True(unseen.Answered > 0, "the examination answered nothing at all");
    }

    /// <summary>
    /// <b>AND WHAT IS WITHHELD IS GENUINELY NEVER DRAWN.</b>
    /// </summary>
    /// <remarks>
    /// The draw rejects and redraws rather than picking out of what is left, because
    /// choosing an index would take one number from the generator where the bit-by-bit
    /// draw takes several — so every measurement this world has ever produced would shift
    /// under a dial that was supposed to be off. This is the half that says the rejection
    /// actually rejects.
    /// </remarks>
    [Fact]
    public void The_withheld_assignments_are_never_drawn_however_long_it_runs()
    {
        var world = new Multiplexer(
            new MultiplexerSettings { Address = 2, Withheld = 16 }, seed: 4);

        var held = new Multiplexer(new MultiplexerSettings { Address = 2, Withheld = 16 }, seed: 4)
            .Withheld
            .Select(one => string.Join(",", one.Seen))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(16, held.Count);

        for (var draw = 0; draw < 20_000; draw++)
        {
            var shown = world.Next();

            var bits = string.Join(",", shown.Cues
                .OrderBy(code => Bits.Position(code))
                .Select(code => Bits.Value(code)));

            Assert.DoesNotContain(bits, held);
        }
    }
}
