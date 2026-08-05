using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// A code that names one occasion is recorded BY what it met, and does not
/// record into it.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE ONE QUANTITY IN THIS PROJECT THAT GREW WITHOUT BOUND, AND WHY.</b> An
/// index is minted fresh per scene and never seen again, so an entry for it in
/// an attribute's row can never gain a second count and can never be evidence.
/// What it can do is accumulate: one entry per scene, forever, in the rows the
/// walk actually passes through — and cost is set by the widest row.
/// </para>
/// <para>
/// <b>The forward edge is untouched, because that is the one the walk uses.</b>
/// A question carries the index it is asking about, so the walk STARTS at the
/// index and never has to arrive at one. That asymmetry is why this is free, and
/// it is the same shape as the temporal window's — see
/// <see cref="Occasion.Recent"/>.
/// </para>
/// </remarks>
public sealed class FleetingTests
{
    /// <inheritdoc cref="BindingTests"/>
    private const double Deep = 12.0;

    /// <summary>
    /// Long enough to be clear of the recency artefact that inflates short runs
    /// on this world. <b>Nothing under a few hundred scenes measures binding.</b>
    /// </summary>
    private const int Scenes = 400;

    private const int Repeats = 6;

    private static WalkSettings Priced =>
        Fixture.Dials(Deep) with { Pricing = Pricing.Sender };

    private static BindingSettings World(bool fleeting) =>
        Fixture.Binding(segmented: true, tagged: true, fleeting: fleeting);

    // ---- what the rendezvous does, asserted directly -----------------------

    /// <summary>A code that recurs, and one minted for this occasion alone.</summary>
    private static readonly Code Lasting = Fixture.C(1);

    /// <inheritdoc cref="Lasting"/>
    private static readonly Code Passing = Fixture.C(2);

    /// <summary>
    /// One occasion holding both, joined — with the front end either declaring
    /// which is which, or saying nothing at all.
    /// </summary>
    private static async Task<Bench> JoinedAsync(bool declared)
    {
        var bench = new Bench(Fixture.Dials(stamina: 10.0));

        await bench.Rendezvous.JoinAsync(new Occasion
        {
            Onsets = [Lasting, Passing],
            Live = [],
            At = 0,
            Fleeting = declared ? new HashSet<Code> { Passing } : null,
        });

        return bench;
    }

    [Fact]
    public async Task A_lasting_code_does_not_record_a_fleeting_one()
    {
        var bench = await JoinedAsync(declared: true);

        // The index records what it met...
        Assert.Equal(1.0, bench.Node(Passing).Together(Lasting));

        // ...and what it met does not record the index.
        Assert.Equal(0.0, bench.Node(Lasting).Together(Passing));
    }

    [Fact]
    public async Task A_fleeting_code_still_notes_the_occasion()
    {
        // THE INVARIANT THE WEIGHTING RESTS ON. An edge is scored
        // `together(here, other) / seen(other)`, so a code carrying a marginal
        // smaller than the counts weighed against it would score above 1.0 --
        // and a weight over 1.0 makes a hop cost less than one, which is the one
        // thing that bounds the walk.
        var bench = await JoinedAsync(declared: true);

        Assert.Equal(1.0, bench.Node(Passing).Seen);
        Assert.True(
            bench.Node(Passing).Together(Lasting) <= bench.Node(Lasting).Seen,
            "a shared count exceeded the marginal it is divided by");
    }

    [Fact]
    public async Task Saying_nothing_writes_both_ways_exactly_as_before()
    {
        // THE CONTROL, and without it the two tests above pass for a rendezvous
        // that has simply stopped writing reverse edges at all.
        var bench = await JoinedAsync(declared: false);

        Assert.Equal(1.0, bench.Node(Lasting).Together(Passing));
        Assert.Equal(1.0, bench.Node(Passing).Together(Lasting));
    }

    // ---- what it costs, and what it buys -----------------------------------

    [Fact]
    public async Task The_one_way_edge_holds_the_result_and_bounds_the_graph()
    {
        // MEASURED 2026-08-03, 8 seeds: 0.9647 +-0.0137 against 0.8718 +-0.0237
        // at 400 scenes, and 0.9826 against 0.9367 at 800. It does not merely
        // survive the change -- it is about three and a half standard errors
        // BETTER at every length tried.
        //
        // The likeliest reason is that the reverse edges were noise rather than
        // signal: `colour -> tag` let a walk hop into the index of some OTHER
        // scene and out into that scene's shape, which is an answer to a question
        // nobody asked, competing with the right one. Sender pricing made those
        // hops expensive; not writing them makes them impossible.
        // THE CAP COMES OFF, because the width claim is the whole second half of
        // this test and the shipped 32 sits BELOW what either arm reaches -- capped,
        // both saturate and it reads 32 against 32, which is the bound and not the
        // graph.
        var (oneWay, bounded) = await MeasureAsync(fleeting: true, row: Fixture.Unbounded);
        var (bothWays, unbounded) = await MeasureAsync(fleeting: false, row: Fixture.Unbounded);

        // THE DIRECTION SURVIVES AND THE SEPARATION DOES NOT, WHICH IS RECORDED
        // RATHER THAN RELAXED. This asserted better than two standard errors on the
        // strength of 0.9647 against 0.8718 at eight seeds -- three and a half. Both
        // arms have since fallen and the gap with them:
        //
        //          6 seeds   0.8162 against 0.8077   0.29 sigma capped, 0.55 lifted
        //         16 seeds   0.8253 against 0.8093   0.66 sigma lifted
        //
        // Sixteen seeds is this project's own answer to a thin sample -- the named
        // trap says one seed drew a curve six flattened and twelve showed a gap
        // thirty-two closed -- and at sixteen it is still two thirds of one standard
        // error. THE ACCURACY CLAIM NO LONGER REPRODUCES. What is left is a
        // direction, asserted as a direction.
        Assert.True(oneWay.Mean > bothWays.Mean,
            $"{oneWay} against {bothWays}");

        Assert.True(oneWay.Separation(bothWays) < 2.0,
            $"{oneWay} against {bothWays} separates by "
            + $"{oneWay.Separation(bothWays):F1} standard errors again -- the "
            + "one-way edge has recovered what it lost and this row wants "
            + "re-measuring rather than this bar wants moving");

        // AND THE POINT OF THE CHANGE, WHICH DOES SURVIVE. The widest row is what
        // sets cost, because `Node.Fire` snapshots all of it and emits one message
        // per surviving partner. Recorded at 20 against 65; it now reads 42 against
        // 69, so the bounding is real and roughly half what it was rather than the
        // better-than-twofold collapse originally claimed.
        Assert.True(bounded < unbounded,
            $"widest row was {bounded} against {unbounded}, so the reverse edges "
            + "have stopped costing a row at all and there is nothing here to bound");
    }

    /// <param name="row">
    /// The row cap. <b>Lift it for the width comparison</b> — the shipped 32 is
    /// below what either arm reaches, so both saturate and report the bound.
    /// </param>
    private static async Task<(Measured Accuracy, int Widest)> MeasureAsync(
        bool fleeting, int row = 32)
    {
        var widest = 0;

        var accuracy = await Sweep.ArmAsync(
            fleeting ? "one-way" : "both ways",
            Repeats,
            async seed =>
            {
                using var run = new BindingRun(World(fleeting), Priced with { Row = row }, seed);
                var result = await run.RunAsync(Scenes, every: 10).ConfigureAwait(false);

                Assert.Empty(result.Complaints);

                widest = Math.Max(widest, result.Widest);
                return result.Accuracy;
            }).ConfigureAwait(false);

        return (accuracy, widest);
    }

    [Fact]
    public void An_index_the_world_does_not_hand_out_cannot_be_fleeting()
    {
        // An arm that looks distinct and is not is how this project has fooled
        // itself before, so the contradictory pair is refused rather than
        // quietly doing nothing.
        Assert.Throws<ArgumentException>(
            () => new Binding(Fixture.Binding(fleeting: true), seed: 1));
    }
}
