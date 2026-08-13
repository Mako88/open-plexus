using OpenPlexus.Codes;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;


/// <summary>
/// The second world, and the property the whole experiment rests on.
/// </summary>
/// <remarks>
/// <b>If sight and touch ever appeared together the result would be
/// meaningless</b> — the question would be a lookup rather than a composition,
/// and nothing downstream could tell the difference. So it is asserted here
/// rather than trusted.
/// </remarks>
public sealed class SensesTests
{
    private static SensesSettings Clean(int concepts = 8, int codes = 3) =>
        Fixture.Senses(concepts, codes);

    [Fact]
    public void Sight_and_touch_are_never_shown_together()
    {
        var world = new Senses(Clean(), seed: 1);

        for (var i = 0; i < 5_000; i++)
        {
            var moment = world.Moment();
            var senses = moment.Select(code => code.Modality).ToHashSet();

            Assert.False(senses.Contains(Senses.Sight) && senses.Contains(Senses.Touch),
                "a moment showed sight and touch at once, which makes the task a lookup");
        }
    }

    [Fact]
    public void Both_pairings_do_occur()
    {
        // The companion. Without it the test above passes for a world that only
        // ever shows one sense, or nothing at all.
        var world = new Senses(Clean(), seed: 1);
        var pairings = new HashSet<string>();

        for (var i = 0; i < 500; i++)
            pairings.Add(string.Join(
                ",", world.Moment().Select(c => c.Modality).Distinct().Order()));

        Assert.Contains($"{Senses.Sight},{Senses.Sound}", pairings);
        Assert.Contains($"{Senses.Sound},{Senses.Touch}", pairings);
        Assert.Equal(2, pairings.Count);
    }

    [Fact]
    public void A_clean_moment_is_two_senses_of_one_concept()
    {
        var world = new Senses(Clean(), seed: 2);

        for (var i = 0; i < 500; i++)
        {
            var moment = world.Moment();
            Assert.Equal(2, moment.Count);
            Assert.Single(moment.Select(Senses.Concept).Distinct());
        }
    }

    [Fact]
    public void Noise_puts_another_concept_in_the_moment()
    {
        // The companion to the test above, and the reason it specifies clean:
        // real co-occurrence is noisy, and a world without any rewards a
        // mechanism that cannot tolerate it.
        var noisy = new Senses(new SensesSettings
        {
            Concepts = 8, CodesPerSense = 3, Noise = 1.0,
        }, seed: 2);

        var strayed = 0;
        for (var i = 0; i < 200; i++)
            if (noisy.Moment().Select(Senses.Concept).Distinct().Count() > 1) strayed++;

        Assert.True(strayed > 100, $"only {strayed} of 200 moments carried a stray code");
    }

    [Fact]
    public void Every_sense_of_a_concept_says_which_concept_it_is()
    {
        var world = new Senses(Clean(concepts: 5, codes: 4), seed: 3);

        foreach (var sense in (byte[])[Senses.Sight, Senses.Sound, Senses.Touch])
            for (var concept = 0; concept < 5; concept++)
            {
                var codes = world.Of(sense, concept);

                Assert.Equal(4, codes.Count);
                Assert.All(codes, code => Assert.Equal(concept, Senses.Concept(code)));
                Assert.All(codes, code => Assert.Equal(sense, code.Modality));
            }
    }

    [Fact]
    public void Two_concepts_never_share_a_code()
    {
        // A collision would make two things one thing, which is the opposite of
        // what a front end is for.
        var world = new Senses(Clean(concepts: 6, codes: 4), seed: 3);

        var all = (from sense in (byte[])[Senses.Sight, Senses.Sound, Senses.Touch]
                   from concept in Enumerable.Range(0, 6)
                   from code in world.Of(sense, concept)
                   select code).ToArray();

        Assert.Equal(all.Length, all.Distinct().Count());
    }

}
