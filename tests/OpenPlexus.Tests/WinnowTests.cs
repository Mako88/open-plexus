using OpenPlexus.Codes;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The random-projection front end — <b>step 8's other half, and the first thing
/// here that reads a NUMBER rather than a symbol.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE CLAIM THAT MATTERS IS LOCALITY SENSITIVITY AND IT IS ONE TEST.</b>
/// Everything else in this file is wiring: the tag is the right size, the same
/// reading gives the same tag, a different reading gives a different one. Those
/// would all pass for an ordinary hash, which is precisely what would be useless
/// — <b>a hash that scattered near inputs would be a front end doing the opposite
/// of its job</b>, and the refutation table's whole point is that a likeness has
/// to come from outside the counting.
/// </para>
/// <para>
/// <b>SO THE MEASUREMENT IS THE GRADIENT, not a threshold.</b> Overlap has to
/// FALL as readings move apart, and it has to fall smoothly rather than off a
/// cliff — a front end that returned the same tag for everything within a radius
/// and nothing beyond it would be a quantiser with the fitting hidden in the
/// radius.
/// </para>
/// </remarks>
public sealed class WinnowTests(ITestOutputHelper output)
{
    /// <summary>The shape the fly runs at, scaled to something a test can read.</summary>
    /// <remarks>
    /// <b>The RATIOS are the fly's and the sizes are not.</b> Fifty inputs to two
    /// thousand cells is a fortyfold expansion, six of fifty sampled, and about a
    /// twentieth surviving — those three are what the paper's guarantee is about,
    /// so they are what is held.
    /// </remarks>
    private const int Inputs = 20;
    private const int Cells = 800;
    private const int Samples = 3;
    private const int Winners = 40;

    private const byte Modality = 77;

    private static Winnow Front() => new(Modality, Inputs, Cells, Samples, Winners);

    /// <summary>A reading, from a generator the test owns.</summary>
    private static double[] Reading(Random rng) =>
        [.. Enumerable.Range(0, Inputs).Select(_ => rng.NextDouble())];

    /// <summary>How much two tags have in common, as a share of one tag.</summary>
    private static double Overlap(IEnumerable<Code> left, IEnumerable<Code> right) =>
        left.Intersect(right).Count() / (double)Winners;

    // ---- the wiring, before any claim about what it buys ---------------------

    [Fact]
    public void A_tag_is_exactly_as_sparse_as_it_was_asked_to_be()
    {
        var tag = Front().Of(Reading(new Random(1)));

        Assert.Equal(Winners, tag.Length);
        Assert.Equal(Winners, tag.Distinct().Count());
        Assert.All(tag, code => Assert.Equal(Modality, code.Modality));
    }

    [Fact]
    public void The_same_reading_gives_the_same_tag_from_a_front_end_built_twice()
    {
        // THE RED-BALL PROPERTY, AND IT IS THE ONE THIS DESIGN CANNOT TRADE. Two
        // machines build their own front end from the shape numbers alone -- there
        // is no seed to agree on and nothing is sent -- so this standing in for
        // "another machine" is the whole of what it means.
        var reading = Reading(new Random(2));

        // AS SEQUENCES, because `ImmutableArray<T>.Equals` compares the underlying
        // ARRAY REFERENCE rather than the contents -- two separately built tags
        // holding identical codes are not `Equal`, and asserting it that way tests
        // nothing about the codes at all.
        Assert.Equal(Front().Of(reading).ToArray(), Front().Of(reading).ToArray());
    }

    [Fact]
    public void A_reading_of_the_wrong_length_is_refused_rather_than_truncated()
    {
        // A DIFFERENT SENSE, AND QUIETLY READING THE FIRST FEW NUMBERS WOULD EMIT
        // CODES THAT LOOK LIKE THIS ONE'S AND MEAN SOMETHING ELSE.
        Assert.Throws<ArgumentException>(() => Front().Of(new double[Inputs + 1]));
    }

    [Fact]
    public void Winnowing_to_everything_is_refused_because_it_is_not_winnowing()
    {
        // THE ONE ADAPTIVE STEP CAN BE SWITCHED OFF BY ARITHMETIC, which is what a
        // named trap here calls a check that is wired and unable to fire: with
        // every cell surviving, every reading gets the same tag.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Winnow(Modality, Inputs, Cells, Samples, Cells));
    }

    [Fact]
    public void Every_cell_listens_to_a_few_inputs_and_never_to_one_twice()
    {
        // SPARSE IS THE HALF THE FLY CONTRIBUTES. A cell reading everything fires
        // on the total and says nothing about shape, so the tag would stop varying
        // with the arrangement and start varying only with the sum.
        var front = Front();
        var reading = new double[Inputs];

        // Each input alone, so a cell's answer says exactly which inputs it holds.
        var listeners = new HashSet<int>[Inputs];

        for (var input = 0; input < Inputs; input++)
        {
            Array.Clear(reading);
            reading[input] = 1.0;
            listeners[input] = [.. front.Of(reading).Select(code => (int)code.Value)];
        }

        // NO INPUT IS INAUDIBLE. An input no cell listens to is a number the front
        // end silently discards, which is a sense that is wired and dead.
        Assert.All(listeners, heard => Assert.NotEmpty(heard));

        // AND NO TWO INPUTS ARE THE SAME INPUT. If two produced identical winners
        // the projection would be collapsing dimensions it was meant to spread.
        for (var one = 0; one < Inputs; one++)
            for (var other = one + 1; other < Inputs; other++)
                Assert.NotEqual(listeners[one], listeners[other]);
    }

    // ---- and what it is actually for ----------------------------------------

    [Fact]
    public void A_louder_reading_of_the_same_shape_is_the_same_tag()
    {
        // CONCENTRATION INVARIANCE — the fly's divisive normalisation, and the
        // reason the tag says WHAT and not HOW MUCH. A smell twice as strong is
        // the same smell; a garden uniformly damper is the same arrangement.
        var front = Front();
        var reading = Reading(new Random(3));

        var plain = front.Of(reading);
        var louder = front.Of([.. reading.Select(one => one * 3.0)]);
        var raised = front.Of([.. reading.Select(one => one + 0.5)]);

        // Sequences rather than arrays — see the note on the determinism test.
        Assert.Equal(plain.ToArray(), louder.ToArray());
        Assert.Equal(plain.ToArray(), raised.ToArray());
    }

    [Fact]
    public void Two_unrelated_readings_do_not_share_a_tag()
    {
        // THE DISCRIMINATION, and without it the test below passes for a front end
        // that returns one tag for everything.
        var front = Front();
        var rng = new Random(4);

        var shared = 0.0;
        const int pairs = 60;

        for (var pair = 0; pair < pairs; pair++)
            shared += Overlap(front.Of(Reading(rng)), front.Of(Reading(rng)));

        var mean = shared / pairs;

        // A TAG IS A TWENTIETH OF THE CELLS, so two tags drawn independently would
        // share about a twentieth by chance alone. Well under a quarter is the bar
        // that says unrelated readings land apart.
        output.WriteLine($"unrelated readings share {mean:P1} of a tag");

        Assert.True(mean < 0.25,
            $"unrelated readings share {mean:P1} of their tags, so this is not "
            + "discriminating between them");
    }

    [Fact]
    public void Overlap_falls_as_readings_move_apart_and_that_is_the_whole_point()
    {
        // THE CLAIM. Dasgupta, Stevens and Navlakha's result is that this circuit
        // is locality-sensitive hashing -- near inputs get overlapping tags -- and
        // it is the ONLY property that makes this a front end rather than a hash.
        //
        // IT IS WHAT THE REFUTATION TABLE ASKED FOR AND NOTHING SHORT OF IT. Every
        // likeness the graph can derive is built out of co-occurrence and carries
        // the behaviour policy with it, which is why `Kindred`, `Foreseeing` and
        // `Backing` all died. THIS LIKENESS THE GRAPH DID NOT COMPUTE: it is a
        // fact about the reading, decided before anything was counted.
        var front = Front();
        var rng = new Random(5);

        double[] steps = [0.0, 0.05, 0.15, 0.35, 0.75];
        var overlap = new double[steps.Length];

        const int trials = 60;

        for (var trial = 0; trial < trials; trial++)
        {
            var reading = Reading(rng);
            var tag = front.Of(reading);

            // ONE DIRECTION PER TRIAL, walked at every distance. Drawing a fresh
            // direction per step would let the distances differ in WHICH way they
            // moved as well as how far, and the curve would be over two things.
            var direction = Reading(rng).Select(one => one - 0.5).ToArray();

            for (var step = 0; step < steps.Length; step++)
            {
                var moved = reading
                    .Select((one, at) => one + (direction[at] * steps[step]))
                    .ToArray();

                overlap[step] += Overlap(tag, front.Of(moved));
            }
        }

        for (var step = 0; step < steps.Length; step++) overlap[step] /= trials;

        output.WriteLine($"{"moved",8} {"overlap",9}");
        for (var step = 0; step < steps.Length; step++)
            output.WriteLine($"{steps[step],8:F2} {overlap[step],9:P1}");

        // A READING THAT DID NOT MOVE IS THE SAME READING.
        Assert.Equal(1.0, overlap[0]);

        // AND EVERY STEP AWAY SHARES STRICTLY LESS THAN THE ONE BEFORE IT. This is
        // the gradient rather than a threshold: a front end that held one tag
        // across a radius and scattered beyond it would be a quantiser with the
        // fitting hidden in the radius, and it would pass a bar on the endpoints.
        for (var step = 1; step < steps.Length; step++)
            Assert.True(overlap[step] < overlap[step - 1],
                $"moving {steps[step]:F2} shared {overlap[step]:P1} against "
                + $"{overlap[step - 1]:P1} at {steps[step - 1]:F2}, so overlap is "
                + "not falling with distance");

        // AND A SMALL MOVE KEEPS MOST OF THE TAG, which is the half that makes two
        // states MEET in the graph. If a nudge scattered the tag there would be no
        // generalisation to have, whatever the ordering said.
        Assert.True(overlap[1] > 0.5,
            $"a small move kept only {overlap[1]:P1} of the tag, so near readings "
            + "do not meet");
    }
}
