using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

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
public sealed class ComposedTests(ITestOutputHelper output)
{
    private static ComposedSettings World(bool segmented = true, bool tagged = true) => new()
    {
        Values = 24, CodesPerValue = 3, Segmented = segmented, Tagged = tagged,
    };

    /// <summary>How many scenes a run sees, at four moments each.</summary>
    private const int Scenes = 4_000;

    /// <summary>How many answered predictions the trailing accuracy is over.</summary>
    /// <remarks>
    /// <b>The denominator the bar wants</b>, and reading it off the scene count instead was
    /// wrong by a factor of two in the error. <c>Recent</c> is an average over this many
    /// answered predictions, so a standard error taken over everything the run asked claims a
    /// precision the statistic does not have — which is a statistic whose halves count
    /// different things, one line further out than usual.
    /// </remarks>
    private const int Window = 2_000;

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
            var episode = world.Draw();

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
            var episode = world.Draw();

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
            Assert.All(world.Draw().Values, byObject => Assert.Distinct(byObject));
    }

    [Fact]
    public void A_question_is_a_moment_of_its_own_and_never_shows_the_answer()
    {
        // The runner's half of the rule above. Three moments carry an attribute each and the
        // fourth carries the conjunction that refers, so the referring values and the value
        // asked for are still never in one moment -- which is the whole design, now that
        // something pushes it.
        var world = new Composed(World(), seed: 1);

        for (var moment = 0; moment < 200; moment++)
        {
            var codes = world.Next().Seen.Codes;

            if (!codes.Any(code => code.Modality == Composed.Asks)) continue;

            Assert.Contains(codes, code => code.Modality == Composed.First);
            Assert.Contains(codes, code => code.Modality == Composed.Second);
            Assert.DoesNotContain(codes, code => code.Modality == Composed.Third);
            Assert.DoesNotContain(codes, code => code.Modality == Composed.Tag);
        }
    }

    // ---- what it measures ---------------------------------------------------

    /// <summary>
    /// Whether the binding result composes, on the world built to ask exactly that.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The honest follow-up to <see cref="BindingTests"/></b>, taken the moment that
    /// world stopped reading at chance. There a grouping lifted the withheld set from 0.5225
    /// to 0.9988 — and the occasion under question wrote its own answer down, so what it
    /// showed is that a binding can be REPRESENTED. This asks whether one can be composed,
    /// and the answer is that it cannot.
    /// </para>
    /// <para>
    /// <b>Registered before the run</b>: every arm sits on chance. <c>A₀ → C₀</c> is
    /// observed by nothing and the values are drawn fresh each scene, so a conclusion needs
    /// two moments joined through an index and nothing puts what fired back in the moment.
    /// What would have refuted it is the grouped arm clearing chance by three standard
    /// errors, which is why the two controls are here rather than the number alone.
    /// </para>
    /// <para>
    /// <b>And this is a null result rather than a failure</b>, on <c>Clevr</c>'s footing. The
    /// world reaches the learner, the questions are asked, and something answers them — the
    /// score is what says composition did not happen, and closing this assertion is progress.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_the_binding_composes_where_the_answer_was_never_observed()
    {
        var arms = new (string Arm, bool Segmented, bool Tagged)[]
        {
            ("grouped, indexed", true, true),
            ("ungrouped, indexed", false, true),
            ("grouped, no index", true, false),
        };

        var taken = new Dictionary<string, Tally>();

        foreach (var (arm, segmented, tagged) in arms)
        {
            var world = new Composed(World(segmented, tagged), seed: 1);
            var brain = new Brain(new CommittingSettings { Capacity = 4000 }, seed: 1);

            var tally = new Bench(
                    new Watching<Coded>(world, new Passthrough<Coded>(one => one)), brain)
                .Run(rounds: Scenes * 4, sweep: 1000, target: 0.99, window: Window);

            var probe = new Composed(World(segmented, tagged), seed: 1);
            var first = Enumerable.Range(0, 4).Select(_ => probe.Next().Seen).ToList();

            output.WriteLine(
                $"{arm} | moments {string.Join(" ", first.Select(one => one.Codes.Count))} "
                + $"| parts {string.Join(" ", first.Select(one => one.Groups?.Count ?? -1))} "
                + $"| codes {tally.Codes:F2}");

            output.WriteLine(
                $"{arm} | chance {world.Chance:F3} | drawn {tally.Recent:F3} | whole "
                + $"{tally.Right}/{tally.Right + tally.Wrong} of {tally.Rounds} rounds, "
                + $"{tally.Silent} silent | held {tally.Resident}, {tally.Repaired} repairs, "
                + $"{tally.Minted} minted");

            // Three standard errors of the chance rate over what the reading is an average
            // OF, so the width is the statistic's rather than a constant somebody chose.
            // Going red means something composed -- take the number and raise this.
            var spread = 3.0 * Math.Sqrt(
                world.Chance * (1.0 - world.Chance) / Window);

            Assert.True(tally.Recent < world.Chance + spread,
                $"{arm} reads {tally.Recent:F3} against a {world.Chance:F3} bar, which is "
                + $"more than three standard errors ({spread:F3}) above it. This assertion "
                + "records a null result and closing it is progress.");

            taken[arm] = tally;
        }

        // And WHY it is nought, which is the half a score cannot say. A story moment carries
        // no outcome, so nothing settles it -- no genesis, no repair, and a monotone counter
        // with nothing to count. Three quarters of every scene is therefore inert, and the
        // whole population is built out of question rounds.
        //
        // The index is what proves it. It is in every story moment and in no question, and it
        // is the ONLY thing linking the three -- so a run that holds the identical population
        // with it and without it is a run where the linking never had a chance to happen.
        // These two worlds emit different moments, which the line above prints.
        //
        // This closes when a commitment is settled by the SUCCESSOR moment from its source.
        // `OutstandingTests` named that seam and this is the measurement that says it named
        // the right one.
        Assert.Equal(taken["grouped, indexed"].Resident, taken["grouped, no index"].Resident);
        Assert.Equal(taken["grouped, indexed"].Minted, taken["grouped, no index"].Minted);
        Assert.Equal(taken["grouped, indexed"].Right, taken["grouped, no index"].Right);
    }
}
