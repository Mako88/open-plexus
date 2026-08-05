using OpenPlexus.Codes;
using OpenPlexus.Graph;
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

    /// <summary>
    /// <b>Sender pricing, and shallow.</b> Both were measured: the receiver arm
    /// scores lower at every depth tried, and stamina is very nearly an exponent
    /// on the message count — doubling it triples the traffic and buys nothing.
    /// </summary>
    private static WalkSettings Dials =>
        Fixture.Dials(stamina: 8.0)
            with { Pricing = Pricing.Sender };

    /// <summary>The ranking this world was measured under before agreement.</summary>
    /// <summary>The ranking this world was measured under before agreement.</summary>
    private const Accumulate Summed = Accumulate.Sum;

    private const int Scenes = 400;

    private const int Repeats = 4;

    // ---- what the world is, asserted rather than described -----------------

    [Fact]
    public void No_moment_ever_shows_two_attributes_of_one_object()
    {
        // THE WHOLE EXPERIMENT, AND IT IS ENFORCED HERE RATHER THAN LEFT TO A
        // CALLER TO RESPECT. If A and C were ever shown together the pair would
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

    // ---- the ceiling, and the controls -------------------------------------

    [Fact]
    public async Task Handed_the_index_the_walk_composes_perfectly()
    {
        // THE CEILING, AND IT IS WHAT SAYS THE SECOND HOP IS NOT THE PROBLEM.
        // `index → answer` was observed, so this arm is memorisable on purpose --
        // it exists so that a low conjunction score can be attributed to the
        // REFERENCE rather than to the composition behind it.
        var ceiling = await AccuracyAsync(Refer.Index);

        Assert.True(ceiling.Mean > 0.95, $"{ceiling}");
    }

    [Fact]
    public async Task Without_an_index_nothing_links_the_moments_and_it_scores_zero()
    {
        // THE CONTROL THE DESIGN RESTS ON, and it fails harder than predicted:
        // grouped but untagged, the two objects' attributes are in different
        // groups and the index is not there to join them, so NO EDGE IS WRITTEN
        // AT ALL. The graph is empty rather than merely unhelpful.
        var untagged = await AccuracyAsync(Refer.Conjunction, World(tagged: false));

        Assert.Equal(0.0, untagged.Mean);
    }

    [Fact]
    public async Task An_index_reachable_from_both_objects_wastes_the_second_attribute()
    {
        // MEASURED, AND IT DOES NOT FAIL THE WAY THE DESIGN PREDICTED. Ungrouped,
        // `A₀` pairs with `tag₁` as readily as with `tag₀`, so BOTH indexes get
        // the conjunction's double support and the pair of them tie. The score
        // does not collapse to chance -- it collapses to what ONE attribute gets
        // on its own, which is the honest statement of what grouping buys.
        //
        // The binding world refuses to construct this pair at all; here it is a
        // control, and a control that cannot be constructed is one nobody ran.
        var ungrouped = await AccuracyAsync(
            Refer.Conjunction, World(segmented: false, tagged: true));

        var grouped = await AccuracyAsync(Refer.Conjunction);

        Assert.True(ungrouped.Mean < grouped.Mean, $"{ungrouped} against {grouped}");
        Assert.True(ungrouped.Separation(grouped) > 1.5,
            $"{ungrouped} against {grouped} is only "
            + $"{ungrouped.Separation(grouped):F1} standard errors");
    }

    [Fact]
    public async Task The_conjunction_beats_one_attribute_alone()
    {
        // THE HEADLINE, AND THE CLAIM IS DELIBERATELY NARROW. Both arms are well
        // above a blind guess, so this is NOT the predicted "one attribute sits
        // at chance" -- see the note on `Single`. What it does say is that adding
        // the second referring attribute moves the score, which is the only part
        // of the design that a conjunction is actually required for.
        var conjunction = await AccuracyAsync(Refer.Conjunction);
        var single = await AccuracyAsync(Refer.Single);

        Assert.True(conjunction.Mean > single.Mean, $"{conjunction} against {single}");
        Assert.True(conjunction.Separation(single) > 2.0,
            $"{conjunction} against {single} is only "
            + $"{conjunction.Separation(single):F1} standard errors");

        // AND BOTH ARE FAR ABOVE A BLIND GUESS, which is what says the walk is
        // composing rather than guessing -- the answer is ranked over the whole
        // alphabet, so chance here is one in twenty-four rather than one in two.
        Assert.True(conjunction.Mean > 4.0 / World().Values, $"{conjunction}");
    }

    [Fact]
    public async Task Narrowing_twice_beats_one_broadcast_and_the_second_stage_loses_nothing()
    {
        // THE FINDING, AND IT IS A LIMIT ON THE ARCHITECTURE RATHER THAN A DIAL.
        // The conjunction's evidence -- the right index reached from BOTH
        // attributes where every other index was reached from one -- lives in the
        // origin's tally for that index and never travels through it. Two routes
        // arriving at a node fire it twice and fan out independently, so what made
        // it the winner is not carried onward.
        //
        // Reading the index back and asking again recovers some of that, and the
        // decomposition says where the rest went: the score lands almost exactly
        // on the rate at which the right index is ranked first, so the SECOND hop
        // is nearly lossless and the REFERENCE is the whole deficit.
        var narrowed = await MeasureAsync(Refer.Narrowed);
        var single = await MeasureAsync(Refer.Conjunction);

        Assert.True(narrowed.Accuracy.Mean > single.Accuracy.Mean,
            $"{narrowed.Accuracy} against {single.Accuracy}");

        // The second stage is nearly lossless: what it scores is what it pointed
        // at. If these ever come apart, the second hop has started failing too
        // and the diagnosis above has expired.
        Assert.True(Math.Abs(narrowed.Accuracy.Mean - narrowed.Reference) < 0.1,
            $"scored {narrowed.Accuracy.Mean:F3} having pointed right "
            + $"{narrowed.Reference:F3} of the time");
    }

    private static async Task<(Measured Accuracy, double Reference)> MeasureAsync(
        Refer refer,
        WalkSettings? dials = null,
        ComposedSettings? world = null,
        Accumulate ranking = Accumulate.Agreement)
    {
        var reference = 0.0;

        var accuracy = await Sweep.ArmAsync($"{refer}", Repeats, async seed =>
        {
            using var run = new ComposedRun(world ?? World(), (dials ?? Dials) with { Ranking = ranking }, seed);
            var result = await run.RunAsync(Scenes, refer, every: 10).ConfigureAwait(false);

            reference += result.Reference / Repeats;
            return result.Accuracy;
        }).ConfigureAwait(false);

        return (accuracy, reference);
    }

    [Fact]
    public async Task Ranking_by_agreement_is_what_picks_the_index()
    {
        // THE CHANGE THAT MADE THE REFERENCE WORK, and it is a better ranking
        // rather than another dial. The conjunction's evidence is that the right
        // index was reached from BOTH attributes where every other index was
        // reached from one -- and `Sum` adds path strengths, which vary far more
        // between routes than the count of origins does.
        //
        // Measured: it roughly doubles the rate at which the right index is
        // ranked first, at every run length tried, with the message count
        // BIT-IDENTICAL because nothing about the walk changes.
        var agreeing = await MeasureAsync(Refer.Narrowed);
        var summed = await MeasureAsync(Refer.Narrowed, ranking: Summed);

        Assert.True(agreeing.Reference > summed.Reference * 1.4,
            $"pointed right {agreeing.Reference:F2} of the time against "
            + $"{summed.Reference:F2}, which is not the lift agreement should give");

        Assert.True(agreeing.Accuracy.Separation(summed.Accuracy) > 2.0,
            $"{agreeing.Accuracy} against {summed.Accuracy}");
    }

    [Fact]
    public async Task A_single_origin_question_is_untouched_by_it()
    {
        // THE CONTROL THAT SAYS THE LIFT IS THE CONJUNCTION'S. With one origin
        // there is nothing to agree, so this arm MUST be unmoved -- otherwise the
        // ranking change is doing something general and the conjunction claim is
        // not attributable.
        var agreeing = await MeasureAsync(Refer.Single);
        var summed = await MeasureAsync(Refer.Single, ranking: Summed);

        Assert.Equal(summed.Accuracy.Mean, agreeing.Accuracy.Mean, precision: 10);
    }

    [Fact]
    public async Task Where_the_conjunction_is_unambiguous_the_world_is_answered()
    {
        // THE RESULT THIS WORLD WAS BUILT FOR. A memoriser scores EXACTLY ZERO
        // here -- `A₀ → C₀` was never observed by anything, and the values are
        // drawn fresh each scene so there is no lasting kind to fall back on.
        //
        // THE REMAINING FAILURES ARE THE WORLD'S OWN AMBIGUITY, NOT THE WALK'S.
        // Two scenes sharing both referring values are genuinely indistinguishable
        // by a conjunction, and how often that happens goes as `scenes / values²`.
        // Widening the alphabet removes the clashes and the score follows: the
        // rate of picking the right index tracks `1 / (1 + clashes)` closely at
        // every width tried. This runs where clashes are rare.
        var wide = World() with { Values = 96 };
        var answered = await MeasureAsync(Refer.Narrowed, world: wide);

        Assert.True(answered.Accuracy.Mean > 0.85,
            $"{answered.Accuracy} against a chance of {1.0 / 96:F4}");

        // And it is the reference doing it, not a lucky second hop.
        Assert.True(Math.Abs(answered.Accuracy.Mean - answered.Reference) < 0.1,
            $"scored {answered.Accuracy.Mean:F3} having pointed right "
            + $"{answered.Reference:F3} of the time");
    }

    private static Task<Measured> AccuracyAsync(Refer refer, ComposedSettings? world = null) =>
        Sweep.ArmAsync($"{refer}", Repeats, async seed =>
        {
            using var run = new ComposedRun(world ?? World(), Dials, seed);
            var result = await run.RunAsync(Scenes, refer, every: 10).ConfigureAwait(false);

            return result.Accuracy;
        });
}
