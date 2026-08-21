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
/// <b>The finding this file exists to explain</b>: more material mints fewer names.
/// <see cref="BudgetCurveTests"/> reads the repair budget across the curve and rung five's
/// count falls as the budget rises, while <c>Tally.Eligible</c> — the scopes it is offered —
/// rises with it. That is the opposite of what a redundancy detector should do, and every
/// instrument on it so far has been a count of the times it SPOKE.
/// </para>
/// <para>
/// <b>And a count of silences would be no better, because five different things end in
/// one.</b> <see cref="Abstracting.Propose"/> charges each ask to the first bar that stopped
/// it: too few scopes, no pair, no pair recurring, a pair no commoner than chance, or a
/// pair that cannot be certified once the search is corrected for. The first three are the
/// population being thin. The last two are the STATISTIC, and they point in opposite
/// directions — one says the redundancy is not there, the other says it is there and
/// unprovable.
/// </para>
/// <para>
/// <b>The hypothesis under test</b>, written down before the grid so it can be wrong. The
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
/// <b>Nothing here proposes loosening a bar.</b> A gate relaxed until something passes is
/// the oldest way to manufacture a finding, and this repo's own row about
/// <c>MDL alone</c> is what that costs. These are readings.
/// </para>
/// </remarks>
public sealed class NamingYieldTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    /// <summary>Matched to <see cref="BudgetCurveTests"/>, so the rows are comparable.</summary>
    private const int Seeds = 8;

    /// <summary>What mixes a seed index, so near neighbours do not share a stream.</summary>
    /// <remarks>
    /// <see cref="Sweep.Spread"/>'s own file says why and holds the measurement: .NET's seeded
    /// <see cref="Random"/> gives 1, 2, 3 streams that agree far more than chance allows, and
    /// a standard error taken across them inherits that agreement. Its own constant is private
    /// and the grids here do not go through <c>ArmAsync</c>, so the value is repeated rather
    /// than reached — a different constant would be a different draw, which is fine, and the
    /// same one keeps these rows comparable with the sweeps that do go through it.
    /// </remarks>
    private const uint Purpose = 0x5EED_0001;

    /// <inheritdoc cref="BudgetCurveTests"/>
    private const int Unlimited = int.MaxValue;

    /// <summary>Eleven bits, because fork 34 says six mints nothing to explain.</summary>
    private const int Address = 3;

    private static Code Of(ulong value) => new(1, value);

    /// <summary>A table written by hand, so a bar can be reached that no world reaches.</summary>
    /// <param name="scopes">How many scopes were counted.</param>
    /// <param name="alone">Each code and how many scopes held it.</param>
    /// <param name="together">Each pair and how many scopes held both.</param>
    /// <param name="deep">
    /// Each pair and how many of those scopes were long enough to survive being named, where
    /// a row wants that set apart from <c>Seen</c>.
    /// <b>Every scope counts as deep where this is not given</b>, which is the reading that
    /// leaves the arms that do not look at it saying exactly what they said before. A table
    /// written to separate savings from surviving savings has to say so.
    /// </param>
    /// <remarks>
    /// <b>Synthetic on purpose, and said so rather than disguised.</b> Three of the six
    /// verdicts want a population shaped in a way no run reaches often, and waiting for a
    /// world to produce one is how a branch of a gate sits unexercised for the life of a
    /// repo — this repo's own trap, twice. <see cref="Recurrence.From"/> is the wire's own
    /// entry point, so nothing here reaches past what a holder could be told.
    /// </remarks>
    private static Recurrence Table(
        int scopes,
        (ulong Code, int Seen)[] alone,
        (ulong Left, ulong Right, int Seen)[] together,
        (ulong Left, ulong Right, int Deep)[]? deep = null) =>
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
                    Deep = 0,
                }),
                .. together.Select(one => new Tallied
                {
                    Left = Of(one.Left),
                    Right = Of(one.Right),
                    Seen = one.Seen,
                    Deep = deep?.FirstOrDefault(
                        row => row.Left == one.Left && row.Right == one.Right).Deep
                        ?? one.Seen,
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
        // A check can be wired and unable to fire, which reads as passing. A partition
        // whose cells are never exercised is a partition that will quietly stop being one
        // the first time the gate is edited, and every share taken off it would still add
        // up. So each verdict is reached here deliberately, and the two that no world
        // reaches are reached from a hand-written table rather than left as prose.
        var dials = new CommittingSettings();

        // Fewer than three scopes to count over. Two commitments cannot repay a name
        // however much they share, so the gate never gets as far as its statistic.
        Assert.Equal(
            Refused.Scarce,
            Abstracting.Propose(Recurrence.Of([Seasoned(1, 2), Seasoned(1, 2)], dials), dials).Refused);

        // Scopes that contribute no pair at all, which `Recurrence.Of` cannot produce
        // because `Eligible` wants two codes -- so this is a fact about the CALLER, and a
        // reading that ever lands here was taken over scopes some other rule admitted.
        Assert.Equal(
            Refused.Unpaired,
            Abstracting.Propose(Table(5, [(1, 5), (2, 5)], []), dials).Refused);

        // Pairs, and none of them twice. Three scopes sharing nothing is the population
        // being varied rather than the statistic being weak, and the two read alike from
        // any count of names.
        Assert.Equal(
            Refused.Rare,
            Abstracting.Propose(
                Recurrence.Of([Seasoned(1, 2), Seasoned(3, 4), Seasoned(5, 6)], dials),
                dials).Refused);

        // A pair that repays and is rarer than chance would make it. Both codes in half
        // the scopes and the pair in a twentieth: independent scopes would have thrown up
        // four times as many, so there is no redundancy here to certify.
        var independent = Abstracting.Propose(
            Table(100, [(1, 50), (2, 50)], [(1, 2, 5)]), dials);

        Assert.Equal(Refused.Independent, independent.Refused);

        // And the peak is signed, which is the whole reason this verdict exists. The
        // selection loop started at nought and took strict improvements, so a population
        // whose every pair is rarer than chance reported exactly what a population with no
        // repaying pair reported. Those are opposite findings.
        Assert.True(independent.Strongest < 0.0,
            $"the peak came back {independent.Strongest}, so this is not the negative case");

        Assert.Equal(1, independent.Repaying);

        // A pair commoner than chance that cannot be certified. Both codes in a fifth of
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
        // Two copies of a gate is two chances for an instrument to describe a machine that
        // is not running, and this repo has paid for that twice. `Shared` is one field of
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

    /// <summary>
    /// <b>A name count is capped by the sweep calendar</b>, so it is not a yield.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The reading behind a constant that was written up as a finding.</b> Eight cells of
    /// a bAbI grid came back at exactly seventeen names across two tasks, two spans and two
    /// capacities — a number no dial in the grid could have moved, because rung five is asked
    /// once a sweep and answers with at most one pair. Twenty thousand rounds at a sweep of a
    /// thousand admits twenty names and no more.
    /// </para>
    /// <para>
    /// <b>So the cap is shown causally rather than argued.</b> The same world over the same
    /// rounds at two sweep periods differs in what it may mint by the ratio of the periods,
    /// and the tighter calendar is the one that mints more. An arithmetic account of a
    /// constant is not the same as a demonstration of it, which is this repo's own point
    /// about an explanation that is true and still not the cause.
    /// </para>
    /// <para>
    /// <b>What this does not say is that asking more often is better.</b>
    /// <c>Minting.UntilRefused</c> was measured and refuted — every count rose while
    /// hard-round coverage fell — so the cap is recorded here as a property of the
    /// instrument and not as a dial anybody should turn.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_name_count_is_bounded_by_how_often_the_rung_was_asked()
    {
        const long rounds = 4_000;

        var readings = new List<(int Sweep, Learned Learned)>();

        foreach (var sweep in new[] { 1_000, 250 })
            readings.Add((sweep, new MultiplexerRun(
                    new MultiplexerSettings { Address = Address },
                    new Brain(new CommittingSettings(), seed: 1),
                    seed: 1)
                .Run(rounds, sweep)));

        foreach (var (sweep, learned) in readings)
            output.WriteLine(
                $"sweep {sweep,5} | asked {learned.Tally.Asked,3} "
                + $"named {learned.Named,3} of {learned.Eligible,4} eligible | "
                + $"spoke {learned.Speaking:F2} per eligible {learned.PerEligible:F3}");

        // THE CALENDAR EXACTLY, which is the half that makes the cap a cap. `Asked` is not
        // a function of the population, the world or any dial on the brain -- it is the
        // number of sweeps that happened, so two cells run over the same rounds got the
        // same number of chances however differently they learnt.
        Assert.All(readings, one =>
            Assert.Equal(rounds / one.Sweep, one.Learned.Tally.Asked));

        // And no cell can hold more names than it had asks. This is what makes an absolute
        // count unreadable between two grids: a cell at its ceiling and a cell that found
        // nothing worth naming are distinguishable only by the denominator.
        Assert.All(readings, one =>
            Assert.True(one.Learned.Named <= one.Learned.Tally.Asked,
                $"sweep {one.Sweep} minted {one.Learned.Named} names off "
                + $"{one.Learned.Tally.Asked} asks, so a name arrived from somewhere this "
                + "file does not know about"));

        // And the tighter calendar mints more, which is the causal half. Arithmetic says
        // the ceiling moved; this says the run actually walked into it, so the cap is what
        // was limiting the count rather than the population running out of redundancy.
        var (loose, tight) = (readings[0].Learned, readings[1].Learned);

        Assert.True(tight.Named > loose.Named,
            $"a sweep of {readings[1].Sweep} minted {tight.Named} names against "
            + $"{loose.Named} at {readings[0].Sweep}, so the count was not calendar-bound "
            + "here and the cap reading needs taking again");
    }

    [Fact]
    public void Every_ask_is_charged_to_exactly_one_bar()
    {
        // The partition asserted rather than claimed, on a real run. This repo's own trap
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

        // And the denominator is the sweep calendar rather than the search, which is what
        // makes a share comparable between two cells that built different populations. If
        // this ever stops holding, every grid below is comparing how often anybody looked.
        Assert.Equal(tally.Asked, brain.Held.Asked);

        output.WriteLine(
            $"asked={tally.Asked} spoke={tally.Spoke} scarce={tally.AtScarce} "
            + $"unpaired={tally.AtUnpaired} rare={tally.AtRare} "
            + $"independent={tally.AtIndependent} uncertain={tally.AtUncertain}");

        // And every proposal is a distinct name, which is the shipped behaviour stated as an
        // identity rather than as a count that would read zero forever. A pair already named
        // is not a candidate, so a proposal can only ever mint something new -- and if that
        // stops holding, either the skip has come undone or names are arriving from
        // somewhere this file does not know about.
        Assert.Equal(tally.Spoke, learned.Named);

        // And the last reading is the state the run finished in, which is what separates
        // the two mechanisms that both end in `Uncertain`.
        var lately = brain.Held.Lately;

        Assert.NotNull(lately);

        output.WriteLine(
            $"finished with {lately.Value.Scopes} scopes, {lately.Value.Candidates} candidate "
            + $"pairs, {lately.Value.Repaying} repaying, peak z {lately.Value.Strongest:F3}");
    }

    /// <summary>
    /// A table holding one fixed redundancy in a population of a given size, where the
    /// redundant codes are reused elsewhere at a fixed rate.
    /// </summary>
    /// <param name="scopes">How many scopes were counted.</param>
    /// <param name="share">What fraction of them each of the two codes appears in.</param>
    /// <param name="together">How many scopes hold BOTH, which is the redundancy itself.</param>
    /// <remarks>
    /// <b>The reuse is the point</b>, and it is what a bigger budget buys. Repair adds one
    /// code from a vocabulary of twenty-two, so a population that grows is a population
    /// re-deriving the same codes in more combinations — each code's own share holds up
    /// while any PARTICULAR pair's share falls. Growing a population whose extra scopes used
    /// fresh codes would be a different experiment and an easier one.
    /// </remarks>
    private static Recurrence Diluted(int scopes, double share, int together)
    {
        var each = (int)(scopes * share);

        return Table(scopes, [(1, each), (2, each)], [(1, 2, together)]);
    }

    [Fact]
    public void The_naming_gate_loses_a_fixed_redundancy_as_the_population_around_it_grows()
    {
        // A control rather than an argument, and the learner is not in it. Every reading of
        // rung five in this repo has been taken through a population, so a yield that falls
        // could be the gate or could be what repair was building. This is the gate alone,
        // fed tables by hand, with one term moved at a time -- which is the only way to say
        // whether the mechanism is even CAPABLE of the behaviour before asking whether it is
        // what happened.
        //
        // And the two terms are moved separately because the bar tightens from both sides.
        // `Normal.Tail(z) * candidates <= Alpha` gets harder when z falls and when the
        // candidate count rises, and a refusal is the same word either way. A grid sweeping
        // both at once has a dead column and this repo has already paid for one.
        var dials = new CommittingSettings();

        output.WriteLine("--- the evidence, with the search held at one candidate ---");
        output.WriteLine("scopes | pair in | codes in | peak z | corrected | verdict");

        var walk = new List<Refused>();

        foreach (var scopes in new[] { 50, 100, 200, 400, 800 })
        {
            var read = Abstracting.Propose(Diluted(scopes, 0.3, 12), dials);

            walk.Add(read.Refused);

            output.WriteLine(
                $"{scopes,6} | {12,7} | {(int)(scopes * 0.3),8} | {read.Strongest,6:F2} "
                + $"| {read.Corrected,9:F3} | {read.Refused}");
        }

        // THE REDUNDANCY NEVER CHANGED. Twelve scopes hold the pair at every row, and each
        // code keeps appearing in three scopes in ten -- so nothing about what these two
        // codes DO together moved. What moved is how much else there was.
        //
        // And the gate walks from naming it to calling it anti-correlated. Certifying goes
        // first and the sign goes second, because z is computed on a SHARE: a pair held at
        // twelve scopes is a quarter of fifty and a sixtieth of eight hundred, while the
        // independence it is tested against is built from marginals that did not move.
        Assert.Equal(Refused.Nothing, walk[0]);
        Assert.Equal(Refused.Independent, walk[^1]);

        // So the gate is scale-relative in the direction the open defect describes, and
        // that is a fact about the arithmetic rather than about any run. Whether a
        // population actually grows this way is the sweep's question and not this one.
        Assert.Contains(Refused.Uncertain, walk);

        output.WriteLine("");
        output.WriteLine("--- the search, with the evidence held exactly still ---");
        output.WriteLine("other pairs | peak z | corrected | verdict");

        var widened = new List<Refused>();

        foreach (var others in new[] { 0, 10, 25, 50, 100 })
        {
            // Filler pairs that repay and lose, which is what a candidate has to be for
            // this arm to isolate anything. Below the description-length bar they would not
            // be counted at all; above the real pair they would take the argmax and this
            // would be a grid about which pair wins.
            var read = Abstracting.Propose(
                Table(
                    200,
                    [
                        (1, 40),
                        (2, 40),
                        .. Enumerable.Range(0, others)
                            .SelectMany(one => new[]
                            {
                                ((ulong)(100 + one), 20),
                                ((ulong)(300 + one), 20),
                            }),
                    ],
                    [
                        (1, 2, 16),
                        .. Enumerable.Range(0, others)
                            .Select(one => ((ulong)(100 + one), (ulong)(300 + one), 3)),
                    ]),
                dials);

            widened.Add(read.Refused);

            output.WriteLine(
                $"{others,11} | {read.Strongest,6:F2} | {read.Corrected,9:F3} | {read.Refused}");
        }

        // The evidence for the real pair is byte-for-byte the same on every row and the
        // peak does not move, so anything that changes here is the correction alone. That
        // is the second way to lose a redundancy, and it is the one that gets WORSE the
        // more a population has learnt to talk about.
        Assert.Equal(Refused.Nothing, widened[0]);
        Assert.Equal(Refused.Uncertain, widened[^1]);

        // AND IT NEVER REACHES `Independent`, which is what makes the two separable in a
        // grid taken through a run. Widening the search cannot make a pair look rarer than
        // chance; only diluting the evidence can. So `independent` rising is the first
        // mechanism and `uncertain` rising with a flat peak is the second.
        Assert.DoesNotContain(Refused.Independent, widened);
    }

    [Fact]
    public void A_code_paired_with_itself_is_refused_rather_than_believed()
    {
        // A type built to cross a wire takes whatever arrived, and this row cannot be made
        // locally. `Recurrence.Of` walks a scope that is `Distinct().Order()`, so no pair it
        // builds has a code twice -- but `From` reads a table a sender wrote, and the gate
        // had no opinion about one.
        //
        // And it does not merely slip through, it wins. A self-pair is seen exactly as often
        // as the code, so its share is p against an expectation of p squared and its z is
        // enormous: it takes the argmax from every honest pair in the table, and the name
        // minted for it throws because a name for fewer than two codes says nothing. A
        // sender's bug crashing a receiver.
        var dials = new CommittingSettings();

        var poisoned = Table(
            100,
            [(1, 90), (2, 40), (3, 40)],
            [(1, 1, 90), (2, 3, 30)]);

        var read = Abstracting.Propose(poisoned, dials);

        // THE HONEST PAIR WINS, which is the assertion that says the bad row was skipped
        // rather than merely survived -- a throw and a wrong winner are both possible here
        // and only one of them is loud.
        Assert.Equal<IEnumerable<Code>>([Of(2), Of(3)], read.Named!.Value);

        // And it is not counted as a candidate either, since the correction is for the
        // search actually performed and this was never a candidate.
        Assert.Equal(1, read.Candidates);
    }

    [Fact]
    public void Skipping_named_pairs_reaches_a_bar_no_world_reaches()
    {
        // A code path guarded by a cap is untested until something reaches the cap, and
        // skipping named pairs adds a second route to `Unpaired` that a run may never take:
        // a population that has named every pair it holds has no candidate left, which is a
        // completely different state from holding no pair at all.
        var counted = Recurrence.Of(
            [Seasoned(1, 2, 5), Seasoned(1, 2, 6), Seasoned(1, 2, 7), Seasoned(1, 2, 8)],
            new CommittingSettings());

        var names = new Naming();

        // EVERY PAIR THOSE SCOPES CONTAIN, so nothing is left to consider. Naming only the
        // winner would leave the runners-up and test something else.
        foreach (var row in counted.Written().Rows.Where(one => one.Right is not null))
            names.Mint([row.Left, row.Right!.Value]);

        Assert.Equal(
            Refused.Unpaired,
            Abstracting.Propose(counted, new CommittingSettings(), names).Refused);

        // And the shipped arm is untouched by the same call, which is the thing that would
        // otherwise change every number this repo has ever taken. `Anything` skips nothing,
        // so it still corrects for every pair in the table -- asserted against the table
        // rather than against a remembered figure.
        var pairs = counted.Written().Rows.Count(one => one.Right is not null);

        // And passing no vocabulary skips nothing, which is what a merge and a test want:
        // there is no one population whose names those would be. Asserted against the table
        // rather than against a remembered figure, because the correction now multiplies by
        // the candidates SEARCHED and an off-by-one there would move every naming number in
        // the repo with nothing going red.
        var whole = Abstracting.Propose(counted, new CommittingSettings());

        Assert.Equal(pairs, whole.Candidates);
        Assert.Equal(Refused.Nothing, whole.Refused);
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_a_run_past_its_last_sweep_has_anything_left_to_name()
    {
        // Whether a trained population still has a subject. Four distributed naming fixtures
        // ask a finished run what it would name next, and a run ends ON a sweep round -- so
        // the population they read is the one at its most exhausted, and whether they have a
        // question to ask at all is a fact about where the run stopped rather than about
        // anything those files are testing.
        output.WriteLine("rounds past last sweep | seeds with something to name | eligible");

        foreach (var past in new[] { 0, 100, 250, 500, 1000 })
        {
            var speaking = 0;
            var eligible = 0.0;
            var which = new List<int>();

            for (var seed = 1; seed <= Seeds; seed++)
            {
                // The fixture's own window, not the shipped one. `SplitNamingTests` pins a
                // deliberately poor population -- rich enough to name whole and too poor for
                // a third of it to name alone -- and it is that window this has to read.
                var dials = new CommittingSettings
                {
                    Budget = 64,
                    Repairing = Repairing.AfterFailure,
                };
                var brain = new Brain(dials, seed);

                new MultiplexerRun(
                    new MultiplexerSettings { Address = Address }, brain, seed)
                    .Run(Rounds + past);

                var all = brain.Held.All.ToList();

                if (Abstracting.Shared(all, dials) is not null)
                {
                    speaking++;
                    which.Add(seed);
                }

                eligible += all.Count(one => Recurrence.Eligible(one, dials));
            }

            output.WriteLine(
                $"{past,22} | {speaking,28} | {eligible / Seeds,8:F1} "
                + $"| seeds {string.Join(" ", which)}");
        }

        // NO BAR. How far past a sweep a fixture has to stop is what this reports, and a
        // threshold written first would be the answer rather than the reading.
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_the_minted_names_actually_stand_for()
    {
        // The vocabulary has never been read, only counted. Every naming number in this repo
        // is a count or a ratio of counts; not one of them says what a single name MEANS. A
        // rung whose whole claim is representation cannot be judged by how many things it
        // named any more than a learner can be judged by how many rules it holds.
        //
        // And this world can answer it exactly, which is why it is asked here. A code is a
        // position and a value packed together -- `(position << 1) | value` -- so a name
        // unfolds to a set of pinned bits, and the first `Address` positions ARE the address.
        // A name grouping address bits alone is *this is address so-and-so*, which is the
        // nearest thing to the concept the plan said this rung was for.
        //
        // What it cannot find is also decidable, and fork 34 says so. *Position p, whatever
        // it says* would need both values of one bit in one name, and a scope pinning a bit
        // both ways is satisfied by nothing -- so no scope holds that pair, and no pair
        // counted from scopes can ever be it. Reported rather than argued: if the column
        // below is ever non-zero, that reasoning is wrong.
        foreach (var (address, skew) in Fixture.Curve)
        {
            var pure = 0;
            var mixed = 0;
            var data = 0;
            var spanning = 0;
            var placed = 0;
            var grouped = 0;
            var members = 0.0;
            var names = 0;

            for (var seed = 1; seed <= Seeds; seed++)
            {
                var brain = new Brain(new CommittingSettings(), seed);

                new MultiplexerRun(
                    new MultiplexerSettings { Address = address, Skew = skew },
                    brain,
                    seed)
                    .Run(Rounds);

                foreach (var name in brain.Held.Names.Means.Select(one => one.Key))
                {
                    // Spelled all the way back out, because a name may stand for a set
                    // containing a name and the question is about the BITS underneath.
                    var unfolded = brain.Held.Names.Unfold([name]);

                    var positions = unfolded
                        .Where(one => one.Modality == Multiplexer.Bit)
                        .Select(one => (int)(one.Value >> 1))
                        .ToList();

                    // The coarse codes, which are the whole point of the graded arm and which
                    // this loop used to drop on the floor. A name made of them alone is
                    // *these positions, whatever they say* -- the concept the row above says
                    // no scope can hold, arriving as a code rather than as a pair.
                    var places = unfolded
                        .Where(one => one.Modality == Multiplexer.Place)
                        .Select(one => (int)one.Value)
                        .ToList();

                    if (positions.Count == 0 && places.Count == 0) continue;

                    names++;
                    members += positions.Count + places.Count;

                    if (positions.Count == 0)
                    {
                        placed++;

                        if (places.TrueForAll(one => one < address)) grouped++;

                        continue;
                    }

                    var addressed = positions.Count(one => one < address);

                    if (addressed == positions.Count) pure++;
                    else if (addressed == 0) data++;
                    else mixed++;

                    // BOTH VALUES OF ONE POSITION, which is the rung-four shape this rung is
                    // not supposed to be able to reach.
                    if (positions.Count != positions.Distinct().Count()) spanning++;
                }
            }

            output.WriteLine($"=== {address + (1 << address)} bits, skew {skew:F1}, "
                + $"{Seeds} seeds ===");
            output.WriteLine(
                $"  {names} names | address only {pure} | data only {data} | mixed {mixed} "
                + $"| one position twice {spanning} | positions only {placed} "
                + $"| the address as positions {grouped} "
                + $"| mean bits {(names == 0 ? 0 : members / names):F2}");
        }

        // NO BAR. What a vocabulary SHOULD look like has never been measured, and a threshold
        // written before the first reading would be the answer rather than the finding.
    }

    /// <summary>
    /// <b>The coarse code reaches the moment and no scope.</b> And every operator refuses it for
    /// the property that makes it worth having.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The graded arm is wired at the front end</b> and inert behind it. A grid of
    /// nearly-identical rows could not have told that apart from a null result. Naming counts
    /// pairs over SCOPES, so a code that never enters a scope can never enter a name — and
    /// nothing in this machine can put this one in one.
    /// </para>
    /// <para>
    /// <b>Genesis refuses it for never having been absent.</b> A position is live in every
    /// moment, so <c>Varied</c> is false and it can never be a root — the gate that stopped
    /// background becoming a parent, doing exactly its job to the one code that wanted
    /// through.
    /// </para>
    /// <para>
    /// <b>And repair refuses it for separating nothing.</b> It is present in every hit and
    /// every miss, so its two-proportion z is nought and it can never be the added
    /// condition. Widening only removes codes. There is no third door.
    /// </para>
    /// <para>
    /// <b>So the property that makes it nameable is the property every operator refuses it
    /// for</b>, and that is a fact about where naming looks rather than about the front end.
    /// The plan has always said a minted name is <i>over co-firing codes</i>; the
    /// implementation counts co-occurring SCOPE MEMBERS. Those are different sets, and the
    /// difference is the whole of fork 34.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_code_that_is_never_absent_separates_nothing_and_so_reaches_no_scope()
    {
        // REPAIR'S DOOR. A code present in every hit and every miss has the same share on
        // both sides, so the pooled two-proportion z is nought however many times it was
        // seen -- it can never be the argmax and could never clear the bar if it were.
        var everywhere = Conditions.Divergence(inHits: 400, hits: 400, inMisses: 100, misses: 100);

        // AND ONE THAT VARIES, or the line above passes for free on a `Divergence` that
        // returns nought to everything.
        var sometimes = Conditions.Divergence(inHits: 380, hits: 400, inMisses: 20, misses: 100);

        output.WriteLine($"always present z={everywhere:F3} | discriminating z={sometimes:F3}");

        Assert.Equal(0.0, everywhere);
        Assert.True(sometimes > 0.0);

        // GENESIS'S DOOR, and it is the same property read the other way. `Varied` asks
        // whether a code has ever been absent since it appeared, and a code live in every
        // moment cannot root a commitment -- the gate that stopped background becoming a
        // parent, which is exactly what an always-present code is.
        var held = new Population(new CommittingSettings(), seed: 1);

        var always = new Code(Multiplexer.Bit, 0);
        var varies = new Code(Multiplexer.Bit, 1);

        for (var moment = 0; moment < 40; moment++)
        {
            var live = moment % 2 == 0
                ? new HashSet<Code> { always, varies }
                : [always];

            held.Witness(live);
            held.Genesis(live, new Code(Multiplexer.Said, 0), []);
        }

        var rooted = held.All.Select(one => one.Scope[0]).ToHashSet();

        output.WriteLine($"{held.Count} minted | rooted on {rooted.Count} distinct codes");

        Assert.Contains(varies, rooted);

        // The finding, asserted rather than guessed. If this goes red an always-present code
        // has found a door, and the account of why fork 36 failed is wrong -- a far more
        // interesting failure than a passing test.
        Assert.DoesNotContain(always, rooted);
    }

    /// <summary>
    /// <b>A coarse code changes what can be named</b> and nothing about what is true.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The trap this guards would have marked the experiment's own subject wrong.</b>
    /// <c>Multiplexer.Sound</c> returned false for any code that was not a bit, and
    /// <c>Checkable</c> never looked at modality at all — so a graded scope would have sailed
    /// past the first and been scored UNSOUND by the second, which reads exactly like a
    /// learner minting rubbish. An answer key in the wrong alphabet, on this repo's own trap
    /// list, caught before the arm was run rather than after.
    /// </para>
    /// <para>
    /// <b>And the property is the one thing the whole arm rests on.</b> A code for the
    /// position with its value thrown away is true in every round, so a rule carrying one
    /// claims exactly what the same rule without it claims. If that ever stops holding, the
    /// front end has started saying what to conclude rather than what it is looking at.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_coarse_code_constrains_nothing_and_leaves_soundness_where_it_was()
    {
        var world = new Multiplexer(new MultiplexerSettings { Address = 2 }, seed: 1);

        // The shortest truth six bits has: both address bits pinned, and the data bit they
        // select pinned with them. Built from the world's own key so it cannot drift.
        var truth = world.Truths()[0];

        Assert.True(world.Checkable(truth.Scope));
        Assert.True(world.Sound(truth.Scope, truth.Expects));

        var carrying = truth.Scope.Add(new Code(Multiplexer.Place, 5));

        Assert.True(world.Checkable(carrying),
            "a scope pinning everything it pinned before is no longer checkable, so a coarse "
            + "code is being counted as though it constrained a position");

        Assert.True(world.Sound(carrying, truth.Expects),
            "adding a code that is true in every round made a true rule false, which is the "
            + "answer key and the population speaking different alphabets");

        // AND THE OTHER DIRECTION, or the check passes for free on a `Sound` that says yes to
        // everything. A scope of coarse codes alone pins nothing at all, so six bits are free
        // and it cannot entail an answer.
        var nothing = ImmutableArray.Create(
            new Code(Multiplexer.Place, 0), new Code(Multiplexer.Place, 1));

        Assert.False(world.Sound(nothing, truth.Expects));
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void How_long_the_sound_rules_it_holds_are_and_how_many_stand_on_a_name()
    {
        // A sound rule is not a good rule, and nothing in this repo separated them until this
        // grid. The shortest scope that can be true of an eleven-bit multiplexer pins the
        // three address bits and the one data bit they select -- four codes. Anything longer
        // pins bits the truth does not depend on: still sound, still never wrong, and firing
        // on a narrower slice of the world every code it adds. A count of true rules cannot
        // tell those apart and a run's score need not either.
        //
        // And the column beside it is where they come from. A minted name is ADDED to every
        // moment holding its members, so it is a code like any other and repair may add one --
        // which makes a name a step of TWO codes past a bar that is paid once. That is the
        // mechanism that deleted `Minting.UntilRefused`: asking until the gate refused took
        // the share of sound rules standing on a name from 67% to 89% and the sound rules of
        // six codes or more from 132 to 334, while the rules of the minimal four rose by a
        // third. More of the world, said less usefully. See the plan's revival row.
        //
        // So this is kept as the standing reading of what the shipped arm builds, because the
        // over-specialisation it measures is not a fact about the deleted loop -- the shipped
        // arm already holds a mean of 5.30 codes where four would do.
        //
        // And it is now a check on a claim whose assertion was deleted. `LiftingTests` used to
        // pin that three weighings build populations equal PER SEED under
        // `Repairing.EveryRound`; two of the three are gone, so with one rule left the
        // identity cannot be stated and the property reverted to an argument about the code.
        // This grid is the empirical half of it: taken under a summed vote it read 420 sound
        // rules at eleven bits skewed, 66.9% of them standing on a name, a mean of 5.30 codes
        // and a length spread of 152/136/132. Written down BEFORE the same grid was taken
        // under the best-advocate vote, because a population that turns out to differ would
        // mean the vote reaches the search after all and every arm comparison in this repo is
        // back open.
        foreach (var (address, skew) in Fixture.Curve)
        {
            var sound = 0;
            var standing = 0;
            var members = 0.0;
            var lengths = new int[7];

            for (var seed = 1; seed <= Seeds; seed++)
            {
                var settings = new MultiplexerSettings { Address = address, Skew = skew };
                var dials = new CommittingSettings();
                var brain = new Brain(dials, seed);

                new MultiplexerRun(settings, brain, seed).Run(Rounds);

                var held = brain.Held;

                // THE SAME THREE FILTERS `Learned.Grade` APPLIES, IN THE SAME ORDER, so the
                // `sound` column here is the `sound` column there and the two grids can be
                // read against each other. A soundness count taken over a different
                // denominator is a different number wearing this one's name.
                var world = new Multiplexer(settings, seed);

                foreach (var one in held.All.Where(one => one.Seen >= dials.Floor))
                {
                    var unfolded = held.Names.Unfold(one.Scope);

                    if (!world.Checkable(unfolded)) continue;
                    if (!world.Sound(unfolded, one.Expects)) continue;

                    sound++;
                    members += unfolded.Length;
                    lengths[Math.Min(unfolded.Length, 6)]++;

                    // The scope as held and not as spelled out, which is the whole question.
                    // A rule that reached four bits through a name is a rule repair got to in
                    // two steps rather than three.
                    if (one.Scope.Any(held.Names.Knows)) standing++;
                }
            }

            output.WriteLine($"=== {address + (1 << address)} bits, skew {skew:F1}, "
                + $"{Seeds} seeds, {Rounds} rounds ===");

            output.WriteLine("  sound | on a name | mean codes | unfolded length 1..6+");

            output.WriteLine(
                $"{sound,7} | {(sound == 0 ? 0.0 : standing / (double)sound),9:P1} "
                + $"| {(sound == 0 ? 0.0 : members / sound),10:F2} | "
                + string.Join(" ", lengths.Skip(1).Select(one => $"{one,5}")));

            output.WriteLine("");
        }

        // NO BAR. How far past the minimum a population should sit has never been measured,
        // and a threshold written before the first reading would be the answer rather than
        // the finding.
    }

    [Fact]
    public void Three_holders_told_the_same_counts_mint_the_same_name_and_rewrite_alike()
    {
        // The property the whole naming-over-a-wire arc exists for. Three holders minting
        // three different sets of names is three languages, and nothing downstream of that
        // means anything.
        //
        // AND IT IS ASSERTED THROUGH `Abstract` rather than at the gate, which is why it lives
        // here and not in `SplitNamingTests`. That file asks the gate a question over merged
        // counts; this runs the whole operator, so the REWRITE is in scope -- a holder that
        // agreed on the name and then rewrote a different set of scopes would pass there and
        // fail here.
        //
        // The shipped dials, because a poor population cannot show this. An earlier take used
        // `SplitNamingTests`' window -- a budget of 64 after a failure -- and every holder
        // minted exactly one name whatever was running, which is a fixture too thin to reach
        // the mechanism reading exactly like a mechanism that does not misbehave.
        var dials = new CommittingSettings();
        var brain = new Brain(dials, seed: 1);

        new MultiplexerRun(
            new MultiplexerSettings { Address = Address }, brain, seed: 1).Run(Rounds);

        var all = brain.Held.All.ToList();

        var holders = Fixture.Sharded(all, 3)
            .Select(shard =>
            {
                var held = new Population(dials, seed: 1);
                foreach (var one in shard) held.Add(one);
                return held;
            })
            .ToList();

        // COUNTED BEFORE ANYTHING IS ABSTRACTED, which is the one round of exchange a
        // deployment gets. Everybody speaks from the same moment.
        var counted = holders
            .Select(held => Recurrence.Of(held.All, dials))
            .ToList();

        var vocabularies = new List<string>();

        for (var holder = 0; holder < holders.Count; holder++)
        {
            var heard = new Recurrence();

            for (var other = 0; other < holders.Count; other++)
                if (other != holder) heard.Absorb(counted[other]);

            holders[holder].Abstract(heard);

            vocabularies.Add(string.Join(
                ",", holders[holder].Names.Means.Select(one => one.Key.Value).Order()));
        }

        output.WriteLine($"vocabularies {vocabularies.Distinct().Count()} | names "
            + $"{holders.Sum(held => held.Names.Count)} over "
            + $"{holders.Sum(held => held.All.Count(one => Recurrence.Eligible(one, dials)))} "
            + $"eligible scopes | {vocabularies[0]}");

        Assert.Single(vocabularies.Distinct());

        // AND SOMETHING WAS ACTUALLY NAMED, so a green line above is convergence rather than
        // three holders that all said nothing. A population with no redundancy converges for
        // free and would pin nothing whatever.
        Assert.All(holders, held => Assert.True(held.Names.Count > 0,
            "no holder named anything, so agreement here is agreement about silence"));
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_bounds_rung_fives_yield_is_how_often_it_is_asked()
    {
        // The ceiling nothing has ever reported, and it is not in the gate at all.
        // `Abstract` mints at most ONE name per call and is called once a sweep round, so a
        // twenty-thousand-round run at the default cadence offers rung five exactly twenty
        // chances. `named` cannot exceed twenty however much a population holds.
        //
        // Which makes *names per eligible scope* a ratio with a capped numerator. The
        // denominator is what repair built and grows with the budget without bound; the
        // numerator is a calendar constant. It MUST fall once the gate saturates, and it
        // would fall in exactly the same way if abstraction were perfect.
        //
        // So the reading is taken against the cadence rather than against the budget. If
        // names track the asks, the bound is the calendar and the open defect is about a
        // denominator. If they flatten, there really is a limit in the material and the
        // question survives.
        output.WriteLine($"{Seeds} seeds, {Rounds} rounds, 11 bits even, budget 256");
        output.WriteLine("sweep every | asked | spoke | named | eligible | names/eligible");

        foreach (var every in new[] { 2000, 1000, 500, 250, 125 })
        {
            var asked = new List<double>();
            var spoke = new List<double>();
            var named = new List<double>();
            var eligible = new List<double>();

            for (var seed = 1; seed <= Seeds; seed++)
            {
                var brain = new Brain(new CommittingSettings(), seed);

                var learned = new MultiplexerRun(
                    new MultiplexerSettings { Address = Address }, brain, seed)
                    .Run(Rounds, sweep: every);

                asked.Add(learned.Tally.Asked);
                spoke.Add(learned.Tally.Spoke);
                named.Add(learned.Named);
                eligible.Add(learned.Eligible);
            }

            output.WriteLine(
                $"{every,11} | {asked.Average(),5:F1} | {spoke.Average(),5:F1} "
                + $"| {named.Average(),5:F1} | {eligible.Average(),8:F1} "
                + $"| {named.Average() / eligible.Average(),14:F3}");
        }

        // AND `sweep` is not one axis, which the grid shows rather than hides. `Council`
        // widens, subsumes, abstracts and culls inside one branch, so asking rung five more
        // often also culls more often -- and `eligible` FALLS as the cadence tightens, which
        // is the denominator being moved by a mechanism this reading is not about. A cell
        // that separated them would need abstraction on its own calendar, and there is no
        // such dial. This repo's own trap: a setting deciding two independent things while
        // being named for one.
        //
        // So what this grid settles is the ceiling and not the slope. At the shipped cadence
        // rung five gets twenty chances in twenty thousand rounds and mints eleven or twelve
        // distinct names, so the count is within a factor of two of a bound that has nothing
        // to do with redundancy -- and every naming number in this repo was taken at that one
        // value of a dial nothing has ever swept.

        // NO BAR. What the cadence should cost rung five has never been measured, and a
        // threshold written before the first reading is a prediction dressed as a
        // requirement. The grid is the finding.
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void And_a_real_populations_best_pair_is_followed_up_the_budget_ladder()
    {
        // The arm above is a model and this is the thing itself. Two aggregate columns can
        // both fall for reasons that have nothing to do with each other -- a run holding a
        // DIFFERENT best pair at every budget would print exactly the dilution signature
        // while no redundancy was ever diluted. So the pair is fixed at the bottom of the
        // ladder and followed, which is the only version of this question that has one
        // subject.
        //
        // And it is the counts and not the verdict that are read, because a verdict is the
        // two terms already multiplied together. `seen` against `scopes` is the share; the
        // marginals are what independence is built from; the two of them are the whole
        // mechanism and either can be flat while the other moves.
        // And over seeds, because one seed is not a comparison and will happily invert.
        // The first take of this grid ran on seed one and its z column went 2.21, 1.01,
        // 1.82, 1.34 -- which is not a trend, and reading the ends of it as one would have
        // been the single-run ordering this repo's traps list already names. The pair is
        // chosen per seed, since each run holds its own structure.
        var budgets = new[] { 16, 64, 256, Unlimited };

        var scoped = budgets.ToDictionary(one => one, _ => new List<double>());
        var paired = budgets.ToDictionary(one => one, _ => new List<double>());
        var shared = budgets.ToDictionary(one => one, _ => new List<double>());
        var apart = budgets.ToDictionary(one => one, _ => new List<double>());
        var peaked = budgets.ToDictionary(one => one, _ => new List<double>());

        for (var seed = 1; seed <= Seeds; seed++)
        {
            Follow(seed);
        }

        output.WriteLine($"{Seeds} seeds, {Rounds} rounds, 11 bits even, one pair per seed "
            + "chosen at the smallest budget and followed");

        output.WriteLine("budget |  scopes |   pairs |   share | expected |       z");

        foreach (var budget in budgets)
        {
            output.WriteLine(
                $"{(budget == Unlimited ? "free" : budget.ToString()),6} "
                + $"| {scoped[budget].Average(),7:F1} | {paired[budget].Average(),7:F1} "
                + $"| {shared[budget].Average(),7:F3} | {apart[budget].Average(),8:F3} "
                + $"| {Sweep.Spread(peaked[budget], "F2")}");
        }

        // NO BAR. What a redundancy's counts should do as a population grows has never been
        // measured, and a threshold written before the first reading is a prediction dressed
        // as a requirement. The grid is the finding.
        return;

        void Follow(int seed)
        {
            (Code Left, Code Right)? followed = null;

            foreach (var budget in budgets)
            {
                var dials = new CommittingSettings { Budget = budget };
                var brain = new Brain(dials, seed);

                new MultiplexerRun(
                    new MultiplexerSettings { Address = Address }, brain, seed).Run(Rounds);

            var counted = Recurrence.Of(brain.Held.All, dials);
            var read = Abstracting.Propose(counted, dials);

            // The table read through the form that crosses a wire, because that is the
            // public one and a probe reaching past it would be measuring something a holder
            // could not be told.
            var rows = counted.Written().Rows;

            // The subject is chosen once, at the smallest budget, and never re-chosen. What
            // the gate proposes there is the redundancy this world's structure actually
            // holds; every row after it is that same pair being asked about in a bigger
            // population.
            followed ??= read.Named is { } first
                ? (first[0], first[1])
                : rows.Where(one => one.Right is not null)
                    .OrderByDescending(one => one.Seen)
                    .ThenBy(one => one.Left).ThenBy(one => one.Right!.Value)
                    .Select(one => (one.Left, one.Right!.Value))
                    .FirstOrDefault();

            var pair = followed!.Value;

            var seen = rows
                .Where(one => one.Left == pair.Left && one.Right == pair.Right)
                .Sum(one => one.Seen);

            var left = rows.Where(one => one.Right is null && one.Left == pair.Left)
                .Sum(one => one.Seen);

            var right = rows.Where(one => one.Right is null && one.Left == pair.Right)
                .Sum(one => one.Seen);

            var scopes = counted.Scopes;

            var share = scopes == 0 ? 0.0 : seen / (double)scopes;

            var expected = scopes == 0
                ? 0.0
                : left / (double)scopes * (right / (double)scopes);

            var z = expected is <= 0.0 or >= 1.0
                ? double.NaN
                : (share - expected) / Math.Sqrt(expected * (1.0 - expected) / scopes);

                scoped[budget].Add(scopes);
                paired[budget].Add(rows.Count(one => one.Right is not null));
                shared[budget].Add(share);
                apart[budget].Add(expected);
                peaked[budget].Add(double.IsNaN(z) ? 0.0 : z);
            }
        }
    }

    /// <param name="budget">How many separation attempts one parent may ever spend.</param>
    /// <param name="address">Address bits.</param>
    /// <param name="skew">How often a data bit is one, or zero to leave them even.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    /// <remarks>
    /// <b>THE SHIPPED TIMING, UNSWEPT.</b> The budget read as inert under the old timing
    /// because the lineages that would have spent it were never blamed, so crossing the two
    /// here would re-run a question already closed rather than ask this one.
    /// </remarks>
    private static (Learned Learned, Proposed? Lately) Run(
        int address, double skew, int budget, int seed)
    {
        var brain = new Brain(new CommittingSettings { Budget = budget }, seed);

        var learned = new MultiplexerRun(
            new MultiplexerSettings { Address = address, Skew = skew }, brain, seed).Run(Rounds);

        return (learned, brain.Held.Lately);
    }

    [Fact]
    public void The_gate_ranks_by_coupling_and_never_by_what_a_name_would_shorten()
    {
        // A control rather than an argument, and it asks the smallest question that can kill
        // the line: is the gate even CAPABLE of preferring a pair with almost nothing to
        // rewrite. Two pairs, both clearing every bar, and the winner is the whole reading.
        //
        // The wide one is a redundancy in the sense the rung was built for -- four hundred
        // scopes hold each code and two hundred hold both, so a name for it shortens two
        // hundred scopes. The narrow one is four scopes that always agree. Naming that
        // shortens four.
        //
        // And both repay, which is what makes this a question about the argmax rather than
        // about a bar. The description-length bar wants three scopes and each has at least
        // that, so nothing here is reached by relaxing anything.
        // Three pairs and three rules, so each takes a different one and the control
        // separates them all at once. The wide pair is held by two hundred scopes and every
        // one of them is exactly two codes long, so naming it saves two hundred entries and
        // removes two hundred commitments from the eligible set. The middle pair is held by
        // fifty scopes that all survive. The rare pair is four scopes that always agree.
        var counted = Table(
            2000,
            [(1, 400), (2, 400), (3, 4), (4, 4), (5, 100), (6, 100)],
            [(1, 2, 200), (3, 4, 4), (5, 6, 50)],
            [(1, 2, 0), (3, 4, 4), (5, 6, 50)]);

        var read = Abstracting.Propose(counted, new CommittingSettings());

        output.WriteLine($"{read.Candidates} candidates, {read.Repaying} repaying, "
            + $"peak z {read.Strongest:F2}, corrected {read.Corrected:E2}, {read.Refused}");

        // Each one alone, so the reading says what the gate would have concluded about the
        // loser had it been the only pair on offer. A winner is only interesting where the
        // runner-up was certifiable too -- otherwise the argmax picked the one thing that
        // passed and there is no preference to report.
        var wide = Abstracting.Propose(
            Table(2000, [(1, 400), (2, 400)], [(1, 2, 200)]), new CommittingSettings());

        var narrow = Abstracting.Propose(
            Table(2000, [(3, 4), (4, 4)], [(3, 4, 4)]), new CommittingSettings());

        output.WriteLine($"  wide alone:   z {wide.Strongest,6:F2}  {wide.Refused}");
        output.WriteLine($"  narrow alone: z {narrow.Strongest,6:F2}  {narrow.Refused}");

        Assert.Equal(Refused.Nothing, wide.Refused);
        Assert.Equal(Refused.Nothing, narrow.Refused);

        // So both are nameable and the gate has to choose. It chooses on z, and z is a
        // share tested against a product of marginals -- which is a COUPLING statistic. Two
        // codes that never appear apart score higher the rarer they are, because the
        // independence they are measured against shrinks with the square of their marginals
        // while their share shrinks only linearly.
        Assert.True(narrow.Strongest > wide.Strongest,
            $"the rare pair scores {narrow.Strongest:F2} and the wide one "
            + $"{wide.Strongest:F2}, so coupling and redundancy rank the same way here and "
            + "this control has nothing to isolate");

        // And the mint goes to the pair with the least to rewrite. That is the gate's own
        // description-length argument inverted: a name costs two entries to say what it
        // means and saves one in every scope holding it, so what it repays is `seen`, and
        // `seen` is the one thing the selection never reads.
        Assert.Equal([Of(3), Of(4)], read.Named);

        // And the reading says so in one number, which is what makes this readable off a
        // run rather than only off a table written to show it. `Shortens` is the winner's
        // own scope count, and the loser's is two hundred.
        Assert.Equal(4, read.Shortens);
        Assert.Equal(200, wide.Shortens);

        // And the gate can say what it passed over, which is what makes this readable off a
        // population nobody built. Both pairs clear the corrected bar, so the widest
        // certifiable one is the two-hundred-scope redundancy the argmax did not take.
        Assert.Equal(200, read.Available);

        // And the arm takes a third one, over the identical table and the identical bars.
        // All three repaid and all three cleared the corrected tail; what moves is which of
        // the certified pairs gets the one mint an ask allows.
        //
        // It is neither the coupling winner nor the savings winner, which is the whole point
        // of the shape. Raw savings takes the two-hundred-scope pair and every one of those
        // scopes leaves the eligible set with it -- refuted on eleven bits at 3.4 standard
        // errors of stacking, and the plan's row says so. This takes the fifty that stay.
        var surviving = Abstracting.Propose(
            counted, new CommittingSettings { Preferring = Preferring.Surviving });

        output.WriteLine(
            $"  under Surviving: shortening {surviving.Shortens}, {surviving.Refused}");

        Assert.Equal([Of(5), Of(6)], surviving.Named);
        Assert.Equal(50, surviving.Shortens);

        // And the two arms speak on the same asks, which is what makes the grid readable.
        // The bars sit in front of the ranking, so a refusal count that ever differs is this
        // arm reaching something it was not built to touch.
        Assert.Equal(read.Refused, surviving.Refused);
        Assert.Equal(read.Candidates, surviving.Candidates);
        Assert.Equal(read.Repaying, surviving.Repaying);
        Assert.Equal(read.Strongest, surviving.Strongest);

        // No bar on a run. Whether a real population holds this shape is the next question
        // and this control cannot answer it -- what it settles is that the arithmetic
        // permits a mint that shortens four scopes to beat one that shortens two hundred.
    }

    [Fact]
    public void What_a_mint_has_left_to_rewrite_on_a_population_that_learnt_one()
    {
        // The control above is arithmetic and this is the thing itself. A table written to
        // show a preference proves the gate CAN take a pair with nothing to rewrite; whether
        // a real population hands it that choice is a separate question and this is it.
        //
        // And a finished run is the population at its widest, which is the case the dilution
        // reading says is worst -- more scopes drawn from more lineages, so any one pair's
        // share is smaller and the correction divides among more candidates. Asking it what
        // it would name NEXT reads the gate against the material it actually ends up with.
        //
        // What would drop this line: `shortens` coming back at a good share of `scopes`.
        // Then the argmax is not reaching for rare pairs on real material, the control is a
        // curiosity about the arithmetic, and the inert mint has some other cause.
        var shortened = new List<int>();
        var available = new List<int>();
        var rewrote = new List<int>();
        var scoped = new List<int>();

        output.WriteLine("seed | eligible | ask | shortens |  widest | rewrote | share");

        for (var seed = 1; seed <= 4; seed++)
        {
            var dials = new CommittingSettings();
            var brain = new Brain(dials, seed);

            new MultiplexerRun(new MultiplexerSettings { Address = Address }, brain, seed)
                .Run(30_000);

            var eligible = brain.Held.All.Count(one => Recurrence.Eligible(one, dials));

            // Asked until it refuses or twenty times, because one ask is one pair and the
            // question is about what the gate reaches for rather than about which pair
            // happened to be first. The cap is a runtime bound and nothing reads it.
            for (var ask = 1; ask <= 20; ask++)
            {
                var said = brain.Held.Abstract();

                if (brain.Held.Lately is not { Refused: Refused.Nothing } spoke) break;

                shortened.Add(spoke.Shortens);
                available.Add(spoke.Available);
                rewrote.Add(said);
                scoped.Add(spoke.Scopes);

                output.WriteLine(
                    $"{seed,4} | {eligible,8} | {ask,3} | {spoke.Shortens,8} | "
                    + $"{spoke.Available,6} | {said,7} "
                    + $"| {spoke.Shortens / (double)spoke.Scopes,5:F3}");
            }
        }

        // The instrument, and it is the whole of the bar for now. A population that names
        // nothing makes every column above a reading about that.
        Assert.NotEmpty(shortened);

        output.WriteLine("");
        output.WriteLine(
            $"{shortened.Count} mints: shortens {shortened.Average():F1} of "
            + $"{scoped.Average():F0} scopes, rewrote {rewrote.Average():F1}, "
            + $"at the description-length bar of three on "
            + $"{shortened.Count(one => one <= 3)} of them");

        output.WriteLine(
            $"  the widest certifiable pair would have shortened {available.Average():F1}, "
            + $"and the argmax took it on "
            + $"{shortened.Where((one, at) => one == available[at]).Count()} of "
            + $"{shortened.Count}");

        // So the answer transfers off the table and onto a population nobody built for it.
        // The gate is not sitting at the description-length floor -- five mints of eighty
        // are -- and it is not taking what the floor was an argument for either. Both
        // candidates were certified over the same search and against the same corrected
        // bar; the one that got the mint is the more strongly coupled one.
        //
        // No threshold on the size of the gap. What ranking on savings is WORTH is an arm
        // and this is not it, so a bar chosen to fit the first reading of it would be the
        // answer written before the question.
        Assert.True(shortened.Average() < available.Average(),
            $"a mint shortened {shortened.Average():F1} scopes against a widest certifiable pair at "
            + $"{available.Average():F1}, so coupling and savings pick the same pair here and "
            + "there is nothing for a ranking arm to recover");

        Assert.True(
            shortened.Where((one, at) => one == available[at]).Count() * 2 < shortened.Count,
            "the argmax took the widest certifiable pair on most asks, so the preference "
            + "the control isolates is not what a real population hands the gate");
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_ranking_a_name_on_savings_buys_over_ranking_it_on_coupling()
    {
        // The arm, on two worlds, because one world's grid is a verdict on the world. The
        // multiplexer at eleven bits has ground truth so soundness is readable, and `Latent`
        // under noise is where repair grows scopes over channels that always co-occur.
        //
        // And `Latent` turns out to say nothing, which is worth carrying rather than
        // swapping out. It holds 313 eligible scopes and the gate speaks 0.5 times in twenty
        // asks, so there is almost nothing for a ranking to rank -- both arms come back
        // identical to every decimal printed. That is a reading about the world and about
        // the gate's refusals on it, and the multiplexer is what carries the comparison.
        //
        // What would kill `Saving`: rewriting more and moving no outcome column on either
        // world. A name that shortens more scopes and leaves the score, the soundness and
        // the stacking where they were has bought population churn and nothing else, and it
        // goes with a revival row.
        //
        // And the arm has a cost written down before the first reading, because `Stackable`'s
        // own remark describes it. Naming a pair that is the WHOLE of a scope takes that
        // commitment out of the eligible set forever -- two codes become one name, and a
        // one-code scope contributes no pair. So a ranking reaching for the pair held by the
        // most scopes eats rung five's own trigger faster than one reaching for a rare tight
        // pair, and `Stacked` is where that would show.
        //
        // Which is why the outcome columns and the material ones are both here. `Saving`
        // leading on soundness and losing on stacking is a real result and a different one
        // from either arm simply winning.
        //
        // And the seeds are MIXED rather than counted. `Sweep.ArmAsync` does this and says
        // why: .NET's seeded `Random` gives near-neighbour seeds streams that agree with each
        // other more than chance allows, and a standard error computed across 1..8 takes that
        // agreement straight off the spread. The first take of this grid used raw seeds, so
        // its columns were paired correctly and its errors were too small to read.
        //
        // And the refusal counts are read on every row rather than assumed. Both bars sit in
        // front of the ranking, so the two arms should speak on the same asks -- a refusal
        // count that moves means this arm reached something it was not built to touch, and
        // then the grid is not a comparison of rankings at all.
        var arms = new[] { Preferring.Coupled, Preferring.Surviving };

        // Run once and read many times. Each cell is a full learning run, so recomputing one
        // for an assertion would double the grid's cost to say something the same rows
        // already carry.
        var drawn = Enumerable.Range(1, Seeds)
            .Select(one => Worlds.Seeds.Apart(one, Purpose))
            .ToList();

        var bits = arms.ToDictionary(
            arm => arm,
            arm => drawn.Select(seed => Bits(arm, seed)).ToList());

        output.WriteLine($"=== eleven bits, {Seeds} seeds, {Rounds} rounds ===");
        output.WriteLine(
            "arm     |         recent |            sound |          unsound "
            + "|         stacked |          rewritten | found | names");

        foreach (var arm in arms)
        {
            var read = bits[arm];

            output.WriteLine(
                $"{arm,-8}| {Sweep.Spread([.. read.Select(one => one.Learnt.Recent)]),14} "
                + $"| {Sweep.Spread([.. read.Select(one => (double)one.Learnt.Sound)], "F1"),16} "
                + $"| {Sweep.Spread([.. read.Select(one => (double)one.Learnt.Unsound)], "F1"),16} "
                + $"| {Sweep.Spread([.. read.Select(one => (double)one.Learnt.Stacked)], "F1"),15} "
                + $"| {Sweep.Spread([.. read.Select(one => (double)one.Rewritten)], "F1"),18} "
                + $"| {read.Average(one => one.Learnt.Found),5:F1} "
                + $"| {read.Average(one => one.Learnt.Named),5:F1}");
        }

        output.WriteLine("");
        output.WriteLine($"=== Latent, six channels, twelve causes, noise 0.1, {Seeds} seeds ===");
        output.WriteLine(
            "arm     |  recent | resident | repairs | eligible | names | stacked "
            + "| rewritten | asked | spoke");

        foreach (var arm in arms)
        {
            var read = drawn.Select(seed => Causes(arm, seed)).ToList();

            output.WriteLine(
                $"{arm,-8}| {read.Average(one => one.Tally.Recent),7:F3} "
                + $"| {read.Average(one => one.Tally.Resident),8:F1} "
                + $"| {read.Average(one => one.Tally.Repaired),7:F1} "
                + $"| {read.Average(one => one.Tally.Eligible),8:F1} "
                + $"| {read.Average(one => one.Tally.Named),5:F1} "
                + $"| {read.Average(one => one.Tally.Stacked),7:F1} "
                + $"| {read.Average(one => one.Rewritten),9:F1} "
                + $"| {read.Average(one => one.Tally.Asked),5:F1} "
                + $"| {read.Average(one => one.Tally.Spoke),5:F1}");
        }

        // The one bar, and it is on the instrument rather than on the result. What separates
        // the arms has to be the ranking, so a run where they are asked different numbers of
        // times is measuring the cadence instead and every column beside it is about that.
        Assert.Equal(
            bits[Preferring.Coupled].Select(one => one.Asked),
            bits[Preferring.Surviving].Select(one => one.Asked));

        // What the previous arm read here, kept because it is what `Surviving` is answering
        // and the plan's refutation row is the record. `Saving` ranked on raw savings:
        //
        //   arm              recent            sound          unsound      stacked   rewritten
        //   Coupled  0.993 +/-0.001   277.1 +/-14.6    273.5 +/-18.7   5.8 +/-0.5   432 +/-29
        //   Saving   0.995 +/-0.001   321.8 +/-23.3    263.5 +/-11.9   3.6 +/-0.4   569 +/-38
        //
        // It rewrote 32% more, lost stacking at 3.4 standard errors, and gained sound rules
        // at 1.6. And `found` was 15.6 of 16.0 truths on both arms to the decimal with
        // `recent` at 0.993 against 0.995, so the outcome columns are AT CEILING here and
        // could not have shown a win either way. This world decides the arm on its
        // structural columns and says nothing about the rest.
        //
        // So what `Surviving` has to show is the stacking held while the rewrites rise. A
        // row that reads like `Saving`'s means the restriction did not bite and the idea
        // goes with the build this time.
        //
        // No bar on what it buys, and the one above is on the instrument. What the arm is
        // worth is the reading, and a threshold written before it would be the answer put in
        // front of the question.
        return;

        (Learned Learnt, long Rewritten, long Asked, long Spoke) Bits(Preferring arm, int seed)
        {
            var brain = new Brain(new CommittingSettings { Preferring = arm }, seed);

            var learnt = new MultiplexerRun(
                new MultiplexerSettings { Address = Address }, brain, seed).Run(Rounds);

            return (
                learnt,
                brain.Held.Lineages.Values.Sum(one => one.Rewritten),
                brain.Held.Asked,
                brain.Held.Spoke);
        }

        (Tally Tally, long Rewritten) Causes(Preferring arm, int seed)
        {
            var brain = new Brain(
                new CommittingSettings { Capacity = 4000, Preferring = arm }, seed);

            var tally = new Bench(
                new Watching<Coded>(
                    new Latent(
                        new LatentSettings { Channels = 6, Causes = 12, Noise = 0.1 }, seed),
                    new Passthrough()),
                brain)
                .Run(Rounds, sweep: 1000, target: 0.9, window: 2000);

            return (tally, brain.Held.Lineages.Values.Sum(one => one.Rewritten));
        }
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task Which_bar_takes_rung_fives_yield_as_the_repair_budget_grows()
    {
        // The same budgets and the same four worlds `BudgetCurveTests` READS, taken from one
        // place so the two grids are comparable by construction rather than by both files
        // having been written carefully.
        //
        // And all four, because the first take of this grid ran on one of them and came back
        // with the opposite of the open defect. On eleven bits even, names RISE with the
        // budget and the peak z rises with them -- so whatever world the falling yield was
        // read on, it is not that one. A partition taken where the effect is absent explains
        // nothing, however clean it is.
        foreach (var (address, skew) in Fixture.Curve)
        {
            output.WriteLine($"=== {address + (1 << address)} bits, skew {skew:F1}: every ask "
                + $"charged to a bar, {Seeds} seeds, {Rounds} rounds ===");

            foreach (var budget in Fixture.Budgets) await Cell(address, skew, budget);
        }

        return;

        async Task Cell(int address, double skew, int budget)
        {
            // One run per seed, shared by every reading below. Readings asked
            // independently would run one configuration many times and print one
            // measurement as though it were many.
            var once = new Dictionary<int, (Learned Learned, Proposed? Lately)>();

            (Learned Learned, Proposed? Lately) Cached(int seed)
            {
                if (!once.TryGetValue(seed, out var ran))
                    once[seed] = ran = Run(address, skew, budget, seed);

                return ran;
            }

            var cell = budget == Unlimited ? "free" : budget.ToString();

            await Fixture.ReadAsync(output, cell, Seeds, seed => Cached(seed).Learned,
                // The two numbers the puzzle is between. More scopes offered, fewer names
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

            // And the counts behind the last two, because the bar tightens from both sides.
            // A tail divided among the candidates gets harder when the evidence weakens and
            // when the search widens, and the refusal is the same word for both. These are
            // the two that tell them apart, so the grid can say which.
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

        // no bar on any of it. Which bar should take rung five's yield has never been
        // measured, and a threshold written before the first reading is a prediction
        // dressed as a requirement. The grid is the finding.
    }
}

