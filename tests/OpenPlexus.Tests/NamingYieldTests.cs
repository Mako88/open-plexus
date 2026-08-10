using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Where rung five's yield goes as its material grows — <b>a partition of the silences,
/// which is the half of this mechanism nothing has ever reported.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE FINDING THIS FILE EXISTS TO EXPLAIN IS THAT MORE MATERIAL MINTS FEWER NAMES.</b>
/// <see cref="BudgetCurveTests"/> reads the repair budget across the curve and rung five's
/// count falls as the budget rises, while <c>Tally.Eligible</c> — the scopes it is offered —
/// rises with it. That is the opposite of what a redundancy detector should do, and every
/// instrument on it so far has been a count of the times it SPOKE.
/// </para>
/// <para>
/// <b>AND A COUNT OF SILENCES WOULD BE NO BETTER, BECAUSE FIVE DIFFERENT THINGS END IN
/// ONE.</b> <see cref="Abstracting.Propose"/> charges each ask to the first bar that stopped
/// it: too few scopes, no pair, no pair recurring, a pair no commoner than chance, or a
/// pair that cannot be certified once the search is corrected for. The first three are the
/// population being thin. The last two are the STATISTIC, and they point in opposite
/// directions — one says the redundancy is not there, the other says it is there and
/// unprovable.
/// </para>
/// <para>
/// <b>THE HYPOTHESIS UNDER TEST, WRITTEN DOWN BEFORE THE GRID SO IT CAN BE WRONG.</b> The
/// gate ends on a tail divided among the candidates, so it tightens from both sides as a
/// population grows: more eligible scopes drawn from more lineages dilute any one pair's
/// SHARE, which is what z is computed on, and more distinct pairs multiply the correction
/// directly. If that is what is happening, the refusals move into
/// <see cref="Refused.Uncertain"/> and <see cref="Refused.Independent"/> as the budget
/// rises, and <see cref="Proposed.Candidates"/> rises with them. If instead the refusals
/// stay in <see cref="Refused.Rare"/>, the material grew without any pair recurring more,
/// and the cause is what repair is building rather than what the gate is testing.
/// </para>
/// <para>
/// <b>NOTHING HERE PROPOSES LOOSENING A BAR.</b> A gate relaxed until something passes is
/// the oldest way to manufacture a finding, and this repo's own row about
/// <c>MDL alone</c> is what that costs. These are readings.
/// </para>
/// </remarks>
public sealed class NamingYieldTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    /// <summary>Matched to <see cref="BudgetCurveTests"/>, so the rows are comparable.</summary>
    private const int Seeds = 8;

    /// <inheritdoc cref="BudgetCurveTests"/>
    private const int Unlimited = int.MaxValue;

    /// <summary>Eleven bits, because fork 34 says six mints nothing to explain.</summary>
    private const int Address = 3;

    private static Code Of(ulong value) => new(1, value);

    /// <summary>A table written by hand, so a bar can be reached that no world reaches.</summary>
    /// <param name="scopes">How many scopes were counted.</param>
    /// <param name="alone">Each code and how many scopes held it.</param>
    /// <param name="together">Each pair and how many scopes held both.</param>
    /// <remarks>
    /// <b>SYNTHETIC ON PURPOSE, AND SAID SO RATHER THAN DISGUISED.</b> Three of the six
    /// verdicts want a population shaped in a way no run reaches often, and waiting for a
    /// world to produce one is how a branch of a gate sits unexercised for the life of a
    /// repo — this repo's own trap, twice. <see cref="Recurrence.From"/> is the wire's own
    /// entry point, so nothing here reaches past what a holder could be told.
    /// </remarks>
    private static Recurrence Table(
        int scopes,
        (ulong Code, int Seen)[] alone,
        (ulong Left, ulong Right, int Seen)[] together) =>
        Recurrence.From(new Counts
        {
            Scopes = scopes,
            Rows =
            [
                .. alone.Select(one => new Tallied
                {
                    Left = Of(one.Code),
                    Right = null,
                    Seen = one.Seen,
                }),
                .. together.Select(one => new Tallied
                {
                    Left = Of(one.Left),
                    Right = Of(one.Right),
                    Seen = one.Seen,
                }),
            ],
        });

    /// <summary>A commitment experienced enough to be allowed an opinion.</summary>
    private static Commitment Seasoned(params ulong[] scope)
    {
        var one = new Commitment([.. scope.Select(Of)], new Code(2, 1));

        var moment = new HashSet<Code>(scope.Select(Of));

        for (var settle = 0; settle < 40; settle++) one.Settle(Verdict.Hit, moment, 0.1);

        return one;
    }

    [Fact]
    public void Every_bar_the_naming_gate_can_refuse_on_is_reachable()
    {
        // A CHECK CAN BE WIRED AND UNABLE TO FIRE, WHICH READS AS PASSING. A partition
        // whose cells are never exercised is a partition that will quietly stop being one
        // the first time the gate is edited, and every share taken off it would still add
        // up. So each verdict is reached here deliberately, and the two that no world
        // reaches are reached from a hand-written table rather than left as prose.
        var dials = new CommittingSettings();

        // FEWER THAN THREE SCOPES TO COUNT OVER. Two commitments cannot repay a name
        // however much they share, so the gate never gets as far as its statistic.
        Assert.Equal(
            Refused.Scarce,
            Abstracting.Propose(Recurrence.Of([Seasoned(1, 2), Seasoned(1, 2)], dials), dials).Refused);

        // SCOPES THAT CONTRIBUTE NO PAIR AT ALL, which `Recurrence.Of` cannot produce
        // because `Eligible` wants two codes -- so this is a fact about the CALLER, and a
        // reading that ever lands here was taken over scopes some other rule admitted.
        Assert.Equal(
            Refused.Unpaired,
            Abstracting.Propose(Table(5, [(1, 5), (2, 5)], []), dials).Refused);

        // PAIRS, AND NONE OF THEM TWICE. Three scopes sharing nothing is the population
        // being varied rather than the statistic being weak, and the two read alike from
        // any count of names.
        Assert.Equal(
            Refused.Rare,
            Abstracting.Propose(
                Recurrence.Of([Seasoned(1, 2), Seasoned(3, 4), Seasoned(5, 6)], dials),
                dials).Refused);

        // A PAIR THAT REPAYS AND IS RARER THAN CHANCE WOULD MAKE IT. Both codes in half
        // the scopes and the pair in a twentieth: independent scopes would have thrown up
        // four times as many, so there is no redundancy here to certify.
        var independent = Abstracting.Propose(
            Table(100, [(1, 50), (2, 50)], [(1, 2, 5)]), dials);

        Assert.Equal(Refused.Independent, independent.Refused);

        // AND THE PEAK IS SIGNED, WHICH IS THE WHOLE REASON THIS VERDICT EXISTS. The
        // selection loop started at nought and took strict improvements, so a population
        // whose every pair is rarer than chance reported exactly what a population with no
        // repaying pair reported. Those are opposite findings.
        Assert.True(independent.Strongest < 0.0,
            $"the peak came back {independent.Strongest}, so this is not the negative case");

        Assert.Equal(1, independent.Repaying);

        // A PAIR COMMONER THAN CHANCE THAT CANNOT BE CERTIFIED. Both codes in a fifth of
        // the scopes and the pair in a twentieth, which is above independence and nowhere
        // near far enough above it to survive the correction.
        var uncertain = Abstracting.Propose(Table(100, [(1, 20), (2, 20)], [(1, 2, 5)]), dials);

        Assert.Equal(Refused.Uncertain, uncertain.Refused);
        Assert.True(uncertain.Strongest > 0.0, "this cell was meant to be above independence");
        Assert.Null(uncertain.Named);

        // AND THE ONE THAT SPEAKS, so the file is not six ways of measuring a silence.
        var spoke = Abstracting.Propose(
            Recurrence.Of(
                [Seasoned(1, 2, 5), Seasoned(1, 2, 6), Seasoned(1, 2, 7), Seasoned(1, 2, 8)],
                dials),
            dials);

        Assert.Equal(Refused.Nothing, spoke.Refused);
        Assert.Equal<IEnumerable<Code>>([Of(1), Of(2)], spoke.Named!.Value);
    }

    [Fact]
    public void The_reading_says_exactly_what_the_gate_the_learner_calls_says()
    {
        // TWO COPIES OF A GATE IS TWO CHANCES FOR AN INSTRUMENT TO DESCRIBE A MACHINE THAT
        // IS NOT RUNNING, and this repo has paid for that twice. `Shared` is one field of
        // `Read` rather than a second implementation, and this is what pins it: every
        // table below is asked both ways and the answers have to be the same object.
        var dials = new CommittingSettings();

        foreach (var counted in (Recurrence[])
        [
            Recurrence.Of([Seasoned(1, 2), Seasoned(1, 2)], dials),
            Recurrence.Of([Seasoned(1, 2), Seasoned(3, 4), Seasoned(5, 6)], dials),
            Recurrence.Of(
                [Seasoned(1, 2, 5), Seasoned(1, 2, 6), Seasoned(1, 2, 7)], dials),
            Table(100, [(1, 50), (2, 50)], [(1, 2, 5)]),
            Table(100, [(1, 20), (2, 20)], [(1, 2, 5)]),
        ])
        {
            Assert.Equal(Abstracting.Shared(counted, dials), Abstracting.Propose(counted, dials).Named);
        }
    }

    [Fact]
    public void Every_ask_is_charged_to_exactly_one_bar()
    {
        // THE PARTITION ASSERTED RATHER THAN CLAIMED, on a real run. This repo's own trap
        // is that five shares summing to the candidates read as complete while the lineage
        // that mattered was absent from the denominator -- so the arithmetic is checked
        // where the counters are actually driven, and not in the type that declares them.
        var brain = new Brain(new CommittingSettings(), seed: 1);

        var learned = new MultiplexerRun(
            new MultiplexerSettings { Address = Address }, brain, seed: 1).Run(4_000);

        var tally = learned.Tally;

        Assert.True(tally.Asked > 0, "rung five was never asked, so there is no partition here");

        Assert.Equal(
            tally.Asked,
            tally.Spoke + tally.AtScarce + tally.AtUnpaired + tally.AtRare
                + tally.AtIndependent + tally.AtUncertain);

        // AND THE DENOMINATOR IS THE SWEEP CALENDAR RATHER THAN THE SEARCH, which is what
        // makes a share comparable between two cells that built different populations. If
        // this ever stops holding, every grid below is comparing how often anybody looked.
        Assert.Equal(tally.Asked, brain.Held.Asked);

        output.WriteLine(
            $"asked={tally.Asked} spoke={tally.Spoke} scarce={tally.AtScarce} "
            + $"unpaired={tally.AtUnpaired} rare={tally.AtRare} "
            + $"independent={tally.AtIndependent} uncertain={tally.AtUncertain}");

        // AND THE LAST READING IS THE STATE THE RUN FINISHED IN, which is what separates
        // the two mechanisms that both end in `Uncertain`.
        var lately = brain.Held.Lately;

        Assert.NotNull(lately);

        output.WriteLine(
            $"finished with {lately.Value.Scopes} scopes, {lately.Value.Candidates} candidate "
            + $"pairs, {lately.Value.Repaying} repaying, peak z {lately.Value.Strongest:F3}");
    }

    /// <param name="budget">How many separation attempts one parent may ever spend.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    /// <remarks>
    /// <b>THE SHIPPED TIMING, UNSWEPT.</b> The budget read as inert under the old timing
    /// because the lineages that would have spent it were never blamed, so crossing the two
    /// here would re-run a question already closed rather than ask this one.
    /// </remarks>
    private static (Learned Learned, Proposed? Lately) Run(int budget, int seed)
    {
        var brain = new Brain(new CommittingSettings { Budget = budget }, seed);

        var learned = new MultiplexerRun(
            new MultiplexerSettings { Address = Address }, brain, seed).Run(Rounds);

        return (learned, brain.Held.Lately);
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task Which_bar_takes_rung_fives_yield_as_the_repair_budget_grows()
    {
        var budgets = new[] { 8, 16, 32, 64, 128, 256, Unlimited };

        // THE GRID MUST BRACKET WHAT SHIPS, or it is a curve about somebody else's brain.
        Assert.Contains(new CommittingSettings().Budget, budgets);

        output.WriteLine($"=== {Address + (1 << Address)} bits, even, {Seeds} seeds, "
            + $"{Rounds} rounds, every-round repair ===");

        foreach (var budget in budgets)
        {
            // ONE RUN PER SEED, SHARED BY EVERY READING BELOW. Readings asked
            // independently would run one configuration many times and print one
            // measurement as though it were many.
            var once = new Dictionary<int, (Learned Learned, Proposed? Lately)>();

            (Learned Learned, Proposed? Lately) Cached(int seed)
            {
                if (!once.TryGetValue(seed, out var ran)) once[seed] = ran = Run(budget, seed);

                return ran;
            }

            var cell = budget == Unlimited ? "free" : budget.ToString();

            await Fixture.ReadAsync(output, cell, Seeds, seed => Cached(seed).Learned,
                // THE TWO NUMBERS THE PUZZLE IS BETWEEN. More scopes offered, fewer names
                // minted, and no account anywhere of what happened in between.
                ("eligible", one => one.Eligible),
                ("named", one => one.Named),
                // THE PARTITION. Asked is the sweep calendar and is expected flat, which is
                // what makes the five beneath it shares rather than counts.
                ("asked", one => one.Tally.Asked),
                ("spoke", one => one.Tally.Spoke),
                ("scarce", one => one.Tally.AtScarce),
                ("rare", one => one.Tally.AtRare),
                ("independent", one => one.Tally.AtIndependent),
                ("uncertain", one => one.Tally.AtUncertain));

            // AND THE COUNTS BEHIND THE LAST TWO, BECAUSE THE BAR TIGHTENS FROM BOTH SIDES.
            // A tail divided among the candidates gets harder when the evidence weakens and
            // when the search widens, and the refusal is the same word for both. These are
            // the two that tell them apart, so the grid can say WHICH.
            foreach (var (what, of) in new (string What, Func<Proposed, double> Of)[]
            {
                ("last scopes", one => one.Scopes),
                ("last pairs", one => one.Candidates),
                ("last repaying", one => one.Repaying),
                ("last peak z", one => double.IsInfinity(one.Strongest) ? 0.0 : one.Strongest),
            })
            {
                var read = Enumerable.Range(1, Seeds)
                    .Select(seed => Cached(seed).Lately)
                    .Where(one => one is not null)
                    .Select(one => of(one!.Value))
                    .ToList();

                output.WriteLine(
                    $"  {cell,-15} {what,-13} | {(read.Count == 0 ? 0.0 : read.Average()),10:F3} "
                    + $"| n={read.Count}");
            }

            output.WriteLine("");
        }

        // NO BAR ON ANY OF IT. Which bar should take rung five's yield has never been
        // measured, and a threshold written before the first reading is a prediction
        // dressed as a requirement. The grid is the finding.
    }
}
