using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

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
public sealed class SensesTests(ITestOutputHelper output)
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

    /// <summary>
    /// The examination is a shape the stream never draws.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The withholding is a COMBINATION</b> rather than a sample, so the usual check —
    /// that a held item is not in the drawn bag — says nothing. What has to hold is that no
    /// drawn round ever shows a sight and asks about touch, however many are drawn, because
    /// that is the shape the whole reading rests on being unrehearsed.
    /// </para>
    /// <para>
    /// <b>And the held turns must actually be that shape</b>, which is the companion. A
    /// withholding that produced ordinary rounds would pass the first half and measure
    /// nothing, and the two halves fail for opposite reasons.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_examination_asks_a_question_no_drawn_round_asks()
    {
        var world = new Senses(Clean() with { Withheld = 200 }, seed: 1);

        for (var draw = 0; draw < 20_000; draw++)
        {
            var senses = world.Next().Seen.Codes.ToLookup(code => code.Modality);

            Assert.False(
                senses[Senses.Sight].Any() && senses[Senses.Asks].Any(code => code.Value == Senses.Touch),
                "a drawn round showed a sight and asked about touch, which is the "
                + "examination — so the held-out score is measuring a rehearsed shape");
        }

        Assert.Equal(200, world.Withheld.Count);

        foreach (var kept in world.Withheld)
        {
            var senses = kept.Seen.Codes.ToLookup(code => code.Modality);

            Assert.True(senses[Senses.Sight].Any(), "a held question showed no sight");
            Assert.True(senses[Senses.Sound].Any(), "a held question showed no sound");
            Assert.Empty(senses[Senses.Touch]);
            Assert.Contains(senses[Senses.Asks], code => code.Value == Senses.Touch);
        }
    }

    /// <summary>
    /// The cross-modal world reaches the commitment learner, and what it reaches is read
    /// against two bars.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The first run of this world</b> since the walk was deleted, and the first ever
    /// against a population of commitments. What it had before was a graph and a traversal;
    /// a subset test cannot walk, so what stands in for composition here is a rule keyed on
    /// the code the two occasion types share.
    /// </para>
    /// <para>
    /// <b>Two bars and not one</b>, because a score against chance would call a perfect
    /// population most of the way wrong. The answer is drawn uniformly among the asked
    /// sense's codes for that concept, so <see cref="Senses.Ceiling"/> is what perfect looks
    /// like and <see cref="Senses.Chance"/> is what nothing looks like.
    /// </para>
    /// <para>
    /// <b>The exam is the reading</b>, and the drawn score is the precondition. A population
    /// that cannot answer the sense it was shown has not learnt the world at all, and its
    /// exam number would say nothing about composition.
    /// </para>
    /// </remarks>
    /// <summary>
    /// <b>A scope spans two senses</b>, because a moment carries both.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The architecture's own line, asserted on a population rather than argued. Every input
    /// is an attribute of a CONCEPT, and binding a seen thing to a heard one is the link this
    /// design exists to make — so a world showing sight beside sound has to be able to leave
    /// a rule naming both, and nothing had ever checked that it does.
    /// </para>
    /// <para>
    /// <b>It is a fact about where modalities meet</b>, and that is the whole content. A scope
    /// is built from one moment, so two modalities reach one scope exactly when they reach one
    /// moment — which is what <see cref="Codes.Compound{TFrame}"/> is for and what
    /// <see cref="Senses"/> does natively. A composition that gave each modality its own
    /// moment would read nought here forever, and this repo briefly shipped one.
    /// </para>
    /// <para>
    /// <b>Repair is what reaches it</b>, because genesis mints one code. So a nought here is
    /// also the reading that says the second code never came from the other sense, which is
    /// the same claim from the mechanism's side.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_scope_spans_two_senses_because_a_moment_carries_both()
    {
        var world = new Senses(Clean(), seed: 1);
        var brain = new Brain(new CommittingSettings { Capacity = 4000 }, seed: 1);

        new Bench(new Watching<Coded>(world, new Passthrough<Coded>(one => one)), brain)
            .Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);

        var all = brain.Held.All.ToList();

        var crossing = all
            .Count(one => one.Scope.Select(code => code.Modality).Distinct().Count() > 1);

        Assert.True(crossing > 0,
            $"not one of {all.Count} resident scopes names two modalities, so nothing the "
            + "population holds is about a seen thing AND a heard one. A scope is built from "
            + "one moment, so this is nought whenever the modalities are not in the same "
            + "moment -- check the front end before the learner.");

        output.WriteLine(
            $"{crossing} of {all.Count} resident scopes span two senses "
            + $"({crossing / (double)all.Count:F3})");
    }

    [Fact]
    public void Senses_reaches_the_commitment_learner()
    {
        var world = new Senses(Clean() with { Withheld = 200 }, seed: 1);
        var brain = new Brain(new CommittingSettings { Capacity = 4000 }, seed: 1);

        var tally = new Bench(new Watching<Coded>(world, new Passthrough<Coded>(one => one)), brain)
            .Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);

        var unseen = Assert.IsType<Examined>(tally.Unseen);

        Assert.True(tally.Recent > 2.0 * world.Chance,
            $"the drawn stream scored {tally.Recent:F3} against a blind draw of "
            + $"{world.Chance:F3}, so the world is not reaching the learner and the exam "
            + "below says nothing");

        // Which names span two senses, which is the plan's own open leaf: rung five names
        // what CO-FIRES, and a seen thing and a heard thing do. Counted rather than asserted
        // on, because nought here is a finding and not a fault.
        var crossed = brain.Held.Names.Means
            .Count(one => one.Value.Select(code => code.Modality).Distinct().Count() > 1);

        output.WriteLine($"drawn      : {tally.Recent:F3}");
        output.WriteLine($"never asked: {unseen.Accuracy:F3} over {unseen.Asked}, "
            + $"{unseen.Silence:F3} silent");
        output.WriteLine($"bars       : ceiling {world.Ceiling:F3}, blind draw {world.Chance:F3}");
        output.WriteLine($"held       : {brain.Held.Count} commitments, "
            + $"{tally.Named} names over {tally.Eligible} eligible scopes, the gate spoke "
            + $"{tally.Spoke} of {tally.Asked} asks, {crossed} names cross-modal");
        output.WriteLine($"wanting    : {tally.Wanting:F3} of blamed rounds nothing separated");
    }

    /// <summary>
    /// What the cross-modal question is worth, against the control that destroys it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The scramble tests the DATA</b> rather than the code, which is why it is the arm
    /// beside the reading. Every mechanism runs identically and only the structure the world
    /// contains is destroyed, so a score surviving it was never coming from the pairing.
    /// </para>
    /// <para>
    /// <b>What would drop the arm</b> is the exam surviving the scramble, which would say
    /// the number is an artefact of the answer alphabet rather than of anything learnt.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_the_cross_modal_question_is_answered_at_all()
    {
        foreach (var codes in new[] { 1, 3 })
        {
            foreach (var clutter in new[] { 0, 4 })
            {
                foreach (var scrambled in new[] { false, true })
                {
                    var world = new Senses(
                        Fixture.Senses(
                            codes: codes, clutter: clutter, pool: clutter == 0 ? 0 : 64,
                            scrambled: scrambled, withheld: 200),
                        seed: 1);

                    var brain = new Brain(new CommittingSettings { Capacity = 4000 }, seed: 1);

                    var tally = new Bench(
                        new Watching<Coded>(world, new Passthrough<Coded>(one => one)),
                        brain)
                        .Run(rounds: 20_000, sweep: 1000, target: 0.9, window: 2000);

                    var crossed = brain.Held.Names.Means
                        .Count(one => one.Value.Select(c => c.Modality).Distinct().Count() > 1);

                    output.WriteLine(
                        $"codes {codes} clutter {clutter} "
                        + $"{(scrambled ? "scrambled" : "paired   ")} | "
                        + $"unseen {tally.Unseen?.Accuracy ?? 0.0:F3} "
                        + $"silent {tally.Unseen?.Silence ?? 0.0:F3} | "
                        + $"drawn {tally.Recent:F3} | "
                        + $"ceiling {world.Ceiling:F3} chance {world.Chance:F3} | "
                        + $"held {brain.Held.Count,5} names {tally.Named,3} of {tally.Eligible,5} eligible, spoke {tally.Speaking:F2} "
                        + $"crossed {crossed,3}");
                }
            }
        }
    }
}
