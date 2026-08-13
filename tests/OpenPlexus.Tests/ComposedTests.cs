using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The world where the answer was never observed, and the reference is a
/// conjunction.
/// </summary>
/// <remarks>
/// <b>This exists because the binding world is memorisable.</b> There the index
/// is grouped with the shape being asked about, so the occasion under question
/// wrote the answer down and a lookup table scores perfectly. Here <c>A₀ → C₀</c>
/// is never observed by anything, and the values are drawn fresh every scene, so
/// there is no lasting kind to fall back on either.
/// </remarks>
public sealed class ComposedTests
{
    private static ComposedSettings World(bool segmented = true, bool tagged = true) => new()
    {
        Values = 24, CodesPerValue = 3, Segmented = segmented, Tagged = tagged,
    };

    private const int Scenes = 400;

    private const int Repeats = 4;

    // ---- what the world is, asserted rather than described -----------------

    [Fact]
    public void No_moment_ever_shows_two_attributes_of_one_object()
    {
        // The whole experiment, and it is enforced here rather than left to a
        // caller to respect. If A and C were ever shown together the pair would
        // be observed, the task would be a lookup, and every number would be
        // measuring memorisation.
        var world = new Composed(World(), seed: 1);

        for (var scene = 0; scene < 50; scene++)
        {
            var episode = world.Next();

            Assert.Equal(Composed.Attributes.Count, episode.Moments.Count);

            foreach (var moment in episode.Moments)
            {
                var attributes = moment
                    .Where(code => code.Modality != Composed.Tag)
                    .Select(code => code.Modality)
                    .Distinct()
                    .ToList();

                Assert.Single(attributes);
            }
        }
    }

    [Fact]
    public void An_index_is_in_every_moment_and_is_fresh_every_scene()
    {
        // The index is the ONLY thing linking the three moments, so it has to
        // persist across them -- and it has to be new each scene, or it would
        // name a kind of object rather than this occasion of one.
        var world = new Composed(World(), seed: 1);
        var everSeen = new HashSet<Code>();

        for (var scene = 0; scene < 20; scene++)
        {
            var episode = world.Next();

            foreach (var moment in episode.Moments)
                Assert.All(episode.Tags, tag => Assert.Contains(tag, moment));

            Assert.All(episode.Tags, tag => Assert.True(everSeen.Add(tag),
                "an index came back in a later scene, so it names a kind"));
        }
    }

    [Fact]
    public void The_two_objects_never_share_a_value_within_one_attribute()
    {
        // A question naming a value both objects had refers to neither.
        var world = new Composed(World(), seed: 1);

        for (var scene = 0; scene < 50; scene++)
            Assert.All(world.Next().Values, byObject => Assert.Distinct(byObject));
    }

}
