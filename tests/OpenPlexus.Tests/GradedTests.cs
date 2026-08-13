using OpenPlexus.Codes;
using OpenPlexus.Machines;
using OpenPlexus.Commitments;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The only measurement here that touches what the project actually claims.
/// </summary>
/// <remarks>
/// <b>Every other world hands the learner its symbols.</b> This one makes it earn
/// them, and which front end earns them is the arm — one code per band against a
/// sparse set of winners read across dimensions together.
/// </remarks>
public sealed class GradedTests(ITestOutputHelper output)
{
    private static Tally Run(Fronting fronting, double crowding, int seed = 1, int address = 3) =>
        new GradedRun(
            new GradedSettings { Address = address, Crowding = crowding },
            new Brain(new CommittingSettings(), seed),
            fronting,
            seed).Run(40000);

    [Fact]
    public void Crowding_moves_a_reading_toward_the_line_and_never_across_it()
    {
        // A world where this changed the answer would be measuring difficulty and
        // calling it interface risk, so the function has to be untouched by it.
        foreach (var crowding in new[] { 0.0, 0.9, 1.0 })
        {
            var world = new Graded(new GradedSettings { Address = 2, Crowding = crowding }, seed: 1);

            for (var round = 0; round < 400; round++)
            {
                var shown = world.Next();

                Assert.Equal(6, shown.Reading.Length);
                Assert.All(shown.Reading, one => Assert.InRange(one, 0.0, 1.0));

                // The bits the world meant are recoverable from the readings, which
                // is what says the answer is still the multiplexer's.
                var bits = shown.Reading.Select(one => one > 0.5 ? 1 : 0).ToArray();
                var address = (bits[0] << 1) | bits[1];

                Assert.Equal(Multiplexer.Says(bits[2 + address]), shown.Outcome);
            }
        }
    }

    [Fact]
    public void The_front_end_is_an_arm_and_both_ends_do_something()
    {
        // The comparison the plan has called its defence and never run. `Winnow` has
        // been built, documented and mounted on no world for the whole life of the
        // repo -- this is the first time anything consumes it.
        //
        // And the code count goes beside the score, because a front end allowed to
        // say four times as much has four times as much to search, and a comparison
        // that ignored that would be rewarding whoever talks more.
        foreach (var crowding in new[] { 0.0, 0.9 })
            foreach (var fronting in new[] { Fronting.Banded, Fronting.Winnowed })
            {
                var sensed = Run(fronting, crowding);

                output.WriteLine(
                    $"crowding={crowding} {fronting} recent={sensed.Recent:F3} "
                    + $"reached={sensed.Reached} resident={sensed.Resident} "
                    + $"codes={sensed.Codes:F1} named={sensed.Named} silent={sensed.Silent}");

                Assert.True(sensed.Codes > 0, "the front end said nothing");
            }
    }

    [Fact]
    public void The_world_is_learnable_once_the_coding_is_nearly_a_bit()
    {
        // The end-to-end claim, at the easy end of the interface. Readings crowded
        // against the line fall in two bands, so what reaches the learner is almost
        // the symbolic world -- and it scores like it. If this failed, the pipeline
        // would be broken rather than the interface being expensive.
        var crowded = Run(Fronting.Banded, crowding: 0.9);

        output.WriteLine($"crowded and banded: {crowded.Recent:F3} at {crowded.Reached}");

        Assert.True(crowded.Recent > 0.85, $"only {crowded.Recent:F3} on the easy coding");
    }

    [Fact]
    public void And_spreading_the_readings_is_what_the_interface_costs()
    {
        // The number this whole world exists to produce, and it carries no bar. The
        // same function, the same learner, and the only difference is that one
        // dimension now speaks in many codes instead of nearly one.
        //
        // Both front ends are far below what the symbolic world reached. That is the
        // interface cost measured for the first time in this repo, and it is the
        // thing the project claims to be good at.
        var spread = new[] { Fronting.Banded, Fronting.Winnowed }
            .Select(fronting => (fronting, Sensed: Run(fronting, crowding: 0.0)))
            .ToList();

        foreach (var (fronting, sensed) in spread)
            output.WriteLine(
                $"spread {fronting}: {sensed.Recent:F3} codes={sensed.Codes:F1} "
                + $"resident={sensed.Resident}");

        Assert.All(spread, one => Assert.True(
            one.Sensed.Recent > Graded.Chance,
            $"{one.fronting} did not beat a blind guess: {one.Sensed.Recent:F3}"));
    }

    [Fact]
    public void The_sheet_is_capped_by_how_many_distinct_wirings_exist()
    {
        // Six numbers sampled six at a time have exactly one wiring, so every cell
        // would fire identically on every reading and the tag would separate nothing.
        // `Winnow` refuses that outright, which is how this was found -- the fixed
        // geometry that works for a fly is degenerate on a narrow reading.
        var (narrow, reach, winners) = Winnowing.Sheet(6);

        Assert.Equal(2, reach);
        Assert.True(narrow <= 15, $"{narrow} cells from only fifteen distinct wirings");
        Assert.True(winners >= 2, "a tag of fewer than two winners is not a population");

        // And a wider reading gets the sheet it asked for, so the cap is a floor
        // effect rather than a ceiling on everything.
        var (wide, _, _) = Winnowing.Sheet(20);

        Assert.Equal(20 * 40, wide);
        Assert.True(wide > narrow);
    }

    [Fact]
    public void The_winnowed_arm_cannot_be_stressed_by_crowding_at_all()
    {
        // A finding rather than a check, and it is why this world does not yet test
        // what it was built to test. `Winnow` reads dimensions TOGETHER through a
        // projection, and crowding contracts every dimension toward the same point --
        // which leaves the relative pattern untouched. The tag does not move, so the
        // arm is invariant to the one dial this world has.
        //
        // What would stress it is a world whose dimensions move independently, which
        // this one does not have. Said here so the flat numbers are read as a
        // property of the pairing rather than as a result about population coding.
        var arms = Fixture.Abreast(
            () => Run(Fronting.Winnowed, 0.0), () => Run(Fronting.Winnowed, 0.9),
            () => Run(Fronting.Banded, 0.0), () => Run(Fronting.Banded, 0.9));

        Assert.Equal(arms[0], arms[1]);

        Assert.NotEqual(arms[2], arms[3]);
    }

    [Fact]
    public void A_fixed_seed_reproduces_a_graded_run_exactly()
    {
        // FORK 12 AGAIN, and a front end is a new place for it to break: `Winnow`
        // takes no seed on purpose, so two runs differing here would mean the
        // projection was not a constant of the design after all.
        // And the two copies run at the same time now, which asks more rather than less.
        // A learner that reached its numbers through anything ambient — a static, a
        // shared buffer, a clock — would have been free to agree with itself while the
        // two runs were consecutive. Side by side it is not.
        var arms = Fixture.Abreast(
            () => Run(Fronting.Winnowed, 0.9, seed: 5), () => Run(Fronting.Winnowed, 0.9, seed: 5),
            () => Run(Fronting.Banded, 0.9, seed: 5), () => Run(Fronting.Banded, 0.9, seed: 5));

        Assert.Equal(arms[0], arms[1]);
        Assert.Equal(arms[2], arms[3]);
    }
}
