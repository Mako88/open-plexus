using OpenPlexus.Graph;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// What happens to the senses world when most of what it sees is irrelevant.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE SIZE DIAL, AND IT IS NOT A NEW WORLD ON PURPOSE.</b> Scale is a change
/// to a world rather than a task of its own, so the rule about running the world
/// that exists applies. The task, the question and the chance level are all
/// untouched — clutter carries its own modality and can never be an answer.
/// </para>
/// <para>
/// <b>The two extremes test opposite claims, and only one of them held.</b> A
/// small pool makes every irrelevant code ubiquitous, which manufactures exactly
/// the ever-present background the forward weighting exists to refuse. A large
/// pool makes each one rare — and rare accidental co-occurrence is what a bigger
/// world produces more of.
/// </para>
/// </remarks>
public sealed class ClutterTests
{
    private const int Moments = 400;

    private const int Repeats = 5;

    private static WalkSettings Dials(double doubt = 0.0) =>
        Fixture.Dials(stamina: 8.0) with { Doubt = doubt };

    [Fact]
    public async Task An_ever_present_background_costs_messages_and_changes_nothing()
    {
        // THE ANTI-HUB PROPERTY, TESTED WHERE IT COULD HAVE FAILED. A code
        // present at every single moment co-occurs with everything, and
        // `together(here, other) / seen(other)` is supposed to make exactly that
        // a WEAK partner. The widest row goes from six to over a hundred and the
        // score does not move at all.
        var clean = await AccuracyAsync(Fixture.Senses(concepts: 12));

        var crowded = await AccuracyAsync(
            Fixture.Senses(concepts: 12, clutter: 2, pool: 4));

        Assert.Equal(clean.Mean, crowded.Mean, precision: 10);
    }

    [Fact]
    public async Task A_rare_coincidence_is_believed_far_too_readily()
    {
        // THE DEFECT THE SIZE DIAL FOUND, and it is in the learning rule rather
        // than in any world. `together / seen` is a maximum-likelihood estimate
        // with no confidence in it: a code seen ONCE, which happened to co-occur
        // that once, scores 1.0 -- the strongest edge the system can hold, on a
        // single accident.
        //
        // A larger pool makes each clutter code rarer, so this gets WORSE the
        // bigger the world is, which is the opposite of what scaling should do.
        var clean = await AccuracyAsync(Fixture.Senses(concepts: 12));

        var sparse = await AccuracyAsync(
            Fixture.Senses(concepts: 12, clutter: 2, pool: 2000));

        Assert.True(sparse.Mean < clean.Mean - 0.1,
            $"{sparse} against {clean} — the defect this dial exists to expose "
            + "has stopped reproducing, so the fix below is being credited for "
            + "repairing nothing");
    }

    [Fact]
    public async Task Doubt_repairs_it_and_costs_the_clean_world_nothing()
    {
        // SHRINKAGE, WHICH IS ONLY NEW HERE. Adding a constant to the denominator
        // pulls a thinly-evidenced ratio toward zero and leaves a well-evidenced
        // one alone -- Laplace's rule, the Dirichlet prior, the smoothing in IDF
        // and the saturation in BM25 are all this same move.
        var clean = await AccuracyAsync(Fixture.Senses(concepts: 12));

        var repaired = await AccuracyAsync(
            Fixture.Senses(concepts: 12, clutter: 2, pool: 2000), Dials(doubt: 8.0));

        Assert.True(repaired.Mean > clean.Mean - 0.05, $"{repaired} against {clean}");

        // AND IT IS FREE WHERE IT IS NOT NEEDED, which is what makes it a
        // candidate for promotion rather than one more thing to sweep.
        var untouched = await AccuracyAsync(Fixture.Senses(concepts: 12), Dials(doubt: 8.0));

        Assert.Equal(clean.Mean, untouched.Mean, precision: 10);
    }

    [Fact]
    public async Task Doubt_moves_the_ranking_and_not_the_price()
    {
        // THE SEPARATION IS THE WHOLE OF WHY IT WORKS, and it was measured the
        // other way round first. One weight was doing two jobs: it ranks a
        // partner AND it says what the hop costs. Shrinking both made every hop
        // dearer, the walk starved before it could compose, and the senses world
        // fell from most questions right to almost none.
        //
        // Applied to the score alone, the message count is IDENTICAL -- which is
        // what says the walk went to exactly the same places and only changed its
        // mind about what it found there.
        var priced = await MessagesAsync(Dials());
        var believed = await MessagesAsync(Dials(doubt: 8.0));

        Assert.Equal(priced, believed);
    }

    private static Task<Measured> AccuracyAsync(SensesSettings world, WalkSettings? dials = null) =>
        Sweep.ArmAsync("clutter", Repeats, async seed =>
        {
            using var run = new SensesRun(world, dials ?? Dials(), seed);
            return (await run.RunAsync(Moments, every: 10).ConfigureAwait(false)).Accuracy;
        });

    private static async Task<long> MessagesAsync(WalkSettings dials)
    {
        var world = Fixture.Senses(concepts: 12, clutter: 2, pool: 2000);

        using var run = new SensesRun(world, dials, seed: 3);
        return (await run.RunAsync(Moments, every: 10).ConfigureAwait(false)).Messages;
    }
}
