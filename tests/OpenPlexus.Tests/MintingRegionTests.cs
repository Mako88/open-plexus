using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whether a direction taken from a commitment's own tally picks out a kind of word.
/// </summary>
/// <remarks>
/// <para>
/// Fork 153's ceiling, and it is taken before the seam it would need is built. Putting drawn
/// cells of the encoder's space in the moment is refuted; what is left is a region whose
/// direction comes from the failures, which is the shape the spike on `architecture` won with.
/// Building that wants a front-end channel carrying vectors, a region store on the population
/// and a gate, and none of it is worth starting if the directions are noise.
/// </para>
/// <para>
/// The direction costs no new memory, which is what makes it a candidate here at all. A
/// commitment already holds, per code, how often that code was present in its hits and in its
/// misses. Weighting each word's vector by those two counts gives two centroids and their
/// difference is the direction, so nothing has to remember a moment and the tally goes on
/// merging the way it does.
/// </para>
/// <para>
/// What is asked of it is that the words on the hit side are a KIND rather than a list. A
/// direction that separates a parent's hits by naming the four words it happened to be right
/// about has memorised them, and a scope over it would fire for nothing new.
/// </para>
/// </remarks>
public sealed class MintingRegionTests(ITestOutputHelper output)
{
    private static string Encoders => Path.Combine(Tree.Repo(), "corpora", "encoders");

    private static RoamingSettings World() =>
        new() { Rooms = 6, Props = 4, People = 2, Steps = 40, Asked = 6, Chatting = 0 };

    /// <summary>
    /// The direction a commitment's own tally gives, and the words it puts on the hit side.
    /// </summary>
    /// <param name="one">The commitment.</param>
    /// <param name="vectors">The vector standing for each word code.</param>
    private static (float[] Direction, float Cut)? Direction(
        Commitment one, IReadOnlyDictionary<Code, float[]> vectors)
    {
        var width = vectors.Values.First().Length;

        var hits = new float[width];
        var misses = new float[width];

        long inHits = 0;
        long inMisses = 0;

        foreach (var (code, seen) in one.Separations)
        {
            if (!vectors.TryGetValue(code, out var vector)) continue;

            for (var d = 0; d < width; d++)
            {
                hits[d] += seen.InHits * vector[d];
                misses[d] += seen.InMisses * vector[d];
            }

            inHits += seen.InHits;
            inMisses += seen.InMisses;
        }

        // Both sides have to have been seen, or there is no difference to take. A parent that
        // never missed is not being repaired and one whose misses hold no word this front end
        // knows has nothing to say about them.
        if (inHits == 0 || inMisses == 0) return null;

        var direction = new float[width];
        var middle = new float[width];

        for (var d = 0; d < width; d++)
        {
            var here = hits[d] / inHits;
            var there = misses[d] / inMisses;

            direction[d] = here - there;
            middle[d] = (here + there) / 2f;
        }

        var cut = 0f;

        for (var d = 0; d < width; d++) cut += direction[d] * middle[d];

        return (direction, cut);
    }

    /// <summary>
    /// What a region minted from a failing commitment's tally stands for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A real run of the walked house, then every resident commitment that has missed enough
    /// to be repaired is asked for its direction and the words it admits are read off. The
    /// score is how concentrated those words are in one of the house's kinds, against what the
    /// same count of words drawn at random would give.
    /// </para>
    /// <para>
    /// The words are the alphabet's, never the parent's own. A direction is only worth minting
    /// if it says something about words the parent has not been right about yet, so the
    /// admitted set is taken over every word the house says.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task What_a_direction_from_a_commitments_own_tally_stands_for()
    {
        using var encoder = new Worded(Encoders);

        var house = new Roaming(World(), seed: 1);

        var spelling = new Dictionary<Code, string>();

        for (var at = 0; at < house.Vocabulary.Count; at++)
            if (house.Meaning(at) is { } code) spelling[code] = house.Vocabulary[at];

        var kind = new Dictionary<Code, string>();

        foreach (var one in house.Named) kind[one] = "room";
        foreach (var one in house.Called) kind[one] = "thing";
        foreach (var one in house.Walking) kind[one] = "person";
        // The words that hold a sentence together are left OUT of the purity question rather
        // than made a fourth kind. They are two thirds of the alphabet, so a region admitting
        // them indiscriminately would score two thirds pure for free and the measure would be
        // about the vocabulary's shape rather than about the direction.

        var vectors = spelling
            .Where(one => encoder.Knows(one.Value))
            .ToDictionary(one => one.Key, one => encoder.Of(one.Value));

        var brain = new Brain(new CommittingSettings { Capacity = 4_000 }, seed: 1);

        var watching = new Watching<Coded>(
            house, new Joined(Joining.Bagged), acting: Chooses.From(_ => null));

        var rounds = 40 * 46;
        var loop = new Round(brain, rounds, sweep: 500, target: 0.9, window: 500);

        for (var round = 0; round < rounds; round++)
            if (watching.Push() is { } pushed) await loop.StepAsync(pushed);

        // The base rate among the words the question is asked of, so the concentration below
        // is read against the alphabet this run holds rather than against a third.
        var baseline = kind.Values
            .GroupBy(one => one)
            .Max(one => one.Count()) / (double)kind.Count;

        var purities = new List<double>();
        var admitted = new List<int>();

        foreach (var one in brain.Held.All.Where(one => one.Misses >= 20 && one.Hits > 0))
        {
            if (Direction(one, vectors) is not { } found) continue;

            var side = new List<Code>();

            foreach (var (code, vector) in vectors)
            {
                var projection = 0f;

                for (var d = 0; d < vector.Length; d++)
                    projection += found.Direction[d] * vector[d];

                if (projection > found.Cut) side.Add(code);
            }

            // A region admitting everything or nothing says nothing, and a bar that counted
            // those would read a hundred per cent pure for a region standing for one word.
            if (side.Count < 2 || side.Count >= vectors.Count) continue;

            var content = side.Where(kind.ContainsKey).ToList();

            // A region admitting one content word or none cannot be pure or impure about
            // them, and counting it as perfect is how a region standing for a single word
            // would read best of all.
            if (content.Count < 2) continue;

            purities.Add(
                content.GroupBy(each => kind[each]).Max(each => each.Count())
                    / (double)content.Count);

            admitted.Add(content.Count);
        }

        output.WriteLine(
            $"{brain.Held.Count} held, {purities.Count} of them repairable and with a "
            + $"two-sided direction, over {kind.Count} content words of the "
            + $"{vectors.Count} the encoder knows");

        output.WriteLine(
            $"{"measure",-24}{"value",10}");

        output.WriteLine($"{"commonest kind, content",-24}{baseline,10:F3}");

        if (purities.Count > 0)
        {
            output.WriteLine(
                $"{"commonest kind, region",-24}{purities.Average(),10:F3}");

            output.WriteLine($"{"content words admitted",-24}{admitted.Average(),10:F1}");

            output.WriteLine(
                $"{"regions over 0.60 pure",-24}"
                + $"{purities.Count(one => one > 0.60) / (double)purities.Count,10:F3}");
        }

        // A direction has to be available at all, or the arm is refused by the tally rather
        // than by what the tally says and no reading here is about the encoder.
        Assert.True(purities.Count > 0,
            "no resident commitment has both misses over the floor and a direction with a "
            + "word on each side, so nothing on this world could ever mint a region and the "
            + "reading is about the run rather than the mechanism");

        // The finding, asserted rather than printed, so the premise fork 153 rests on cannot
        // quietly stop holding. A direction no better than the base rate would mean the tally
        // does not recover a kind and the fork wants a different source for its labels.
        Assert.True(purities.Average() > baseline + 0.10,
            $"a region minted from a commitment's own tally is {purities.Average():F3} of one "
            + $"kind against a base rate of {baseline:F3}, so the failures do not partition "
            + "the words well enough to aim a direction and fork 153 is refused on the same "
            + "ground the drawn partition was");
    }
}
