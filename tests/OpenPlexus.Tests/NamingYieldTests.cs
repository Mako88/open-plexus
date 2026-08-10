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

        // AND EVERY PROPOSAL IS A DISTINCT NAME, which is the shipped behaviour stated as an
        // identity rather than as a count that would read zero forever. A pair already named
        // is not a candidate, so a proposal can only ever mint something new -- and if that
        // stops holding, either the skip has come undone or names are arriving from
        // somewhere this file does not know about.
        Assert.Equal(tally.Spoke, learned.Named);

        // AND THE LAST READING IS THE STATE THE RUN FINISHED IN, which is what separates
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
    /// <b>THE REUSE IS THE POINT AND IT IS WHAT A BIGGER BUDGET BUYS.</b> Repair adds one
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
        // A CONTROL RATHER THAN AN ARGUMENT, AND THE LEARNER IS NOT IN IT. Every reading of
        // rung five in this repo has been taken through a population, so a yield that falls
        // could be the gate or could be what repair was building. This is the gate alone,
        // fed tables by hand, with one term moved at a time -- which is the only way to say
        // whether the mechanism is even CAPABLE of the behaviour before asking whether it is
        // what happened.
        //
        // AND THE TWO TERMS ARE MOVED SEPARATELY BECAUSE THE BAR TIGHTENS FROM BOTH SIDES.
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
        // AND THE GATE WALKS FROM NAMING IT TO CALLING IT ANTI-CORRELATED. Certifying goes
        // first and the sign goes second, because z is computed on a SHARE: a pair held at
        // twelve scopes is a quarter of fifty and a sixtieth of eight hundred, while the
        // independence it is tested against is built from marginals that did not move.
        Assert.Equal(Refused.Nothing, walk[0]);
        Assert.Equal(Refused.Independent, walk[^1]);

        // SO THE GATE IS SCALE-RELATIVE IN THE DIRECTION THE OPEN DEFECT DESCRIBES, and
        // that is a fact about the arithmetic rather than about any run. Whether a
        // population actually grows this way is the sweep's question and not this one.
        Assert.Contains(Refused.Uncertain, walk);

        output.WriteLine("");
        output.WriteLine("--- the search, with the evidence held exactly still ---");
        output.WriteLine("other pairs | peak z | corrected | verdict");

        var widened = new List<Refused>();

        foreach (var others in new[] { 0, 10, 25, 50, 100 })
        {
            // FILLER PAIRS THAT REPAY AND LOSE, which is what a candidate has to be for
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

        // THE EVIDENCE FOR THE REAL PAIR IS BYTE-FOR-BYTE THE SAME ON EVERY ROW and the
        // peak does not move, so anything that changes here is the correction alone. That
        // is the second way to lose a redundancy, and it is the one that gets WORSE the
        // more a population has learnt to talk about.
        Assert.Equal(Refused.Nothing, widened[0]);
        Assert.Equal(Refused.Uncertain, widened[^1]);

        // AND IT NEVER REACHES `Independent`, WHICH IS WHAT MAKES THE TWO SEPARABLE IN A
        // GRID TAKEN THROUGH A RUN. Widening the search cannot make a pair look rarer than
        // chance; only diluting the evidence can. So `independent` rising is the first
        // mechanism and `uncertain` rising with a flat peak is the second.
        Assert.DoesNotContain(Refused.Independent, widened);
    }

    [Fact]
    public void A_code_paired_with_itself_is_refused_rather_than_believed()
    {
        // A TYPE BUILT TO CROSS A WIRE TAKES WHATEVER ARRIVED, AND THIS ROW CANNOT BE MADE
        // LOCALLY. `Recurrence.Of` walks a scope that is `Distinct().Order()`, so no pair it
        // builds has a code twice -- but `From` reads a table a sender wrote, and the gate
        // had no opinion about one.
        //
        // AND IT DOES NOT MERELY SLIP THROUGH, IT WINS. A self-pair is seen exactly as often
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

        // AND IT IS NOT COUNTED AS A CANDIDATE EITHER, since the correction is for the
        // search actually performed and this was never a candidate.
        Assert.Equal(1, read.Candidates);
    }

    [Fact]
    public void Skipping_named_pairs_reaches_a_bar_no_world_reaches()
    {
        // A CODE PATH GUARDED BY A CAP IS UNTESTED UNTIL SOMETHING REACHES THE CAP, and
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

        // AND THE SHIPPED ARM IS UNTOUCHED BY THE SAME CALL, which is the thing that would
        // otherwise change every number this repo has ever taken. `Anything` skips nothing,
        // so it still corrects for every pair in the table -- asserted against the table
        // rather than against a remembered figure.
        var pairs = counted.Written().Rows.Count(one => one.Right is not null);

        // AND PASSING NO VOCABULARY SKIPS NOTHING, which is what a merge and a test want:
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
        // WHETHER A TRAINED POPULATION STILL HAS A SUBJECT. Four distributed naming fixtures
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
                // THE FIXTURE'S OWN WINDOW, NOT THE SHIPPED ONE. `SplitNamingTests` pins a
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
        // THE VOCABULARY HAS NEVER BEEN READ, ONLY COUNTED. Every naming number in this repo
        // is a count or a ratio of counts; not one of them says what a single name MEANS. A
        // rung whose whole claim is representation cannot be judged by how many things it
        // named any more than a learner can be judged by how many rules it holds.
        //
        // AND THIS WORLD CAN ANSWER IT EXACTLY, WHICH IS WHY IT IS ASKED HERE. A code is a
        // position and a value packed together -- `(position << 1) | value` -- so a name
        // unfolds to a set of pinned bits, and the first `Address` positions ARE the address.
        // A name grouping address bits alone is *this is address so-and-so*, which is the
        // nearest thing to the concept the plan said this rung was for.
        //
        // WHAT IT CANNOT FIND IS ALSO DECIDABLE, AND FORK 34 SAYS SO. *Position p, whatever
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
            var members = 0.0;
            var names = 0;

            for (var seed = 1; seed <= Seeds; seed++)
            {
                var brain = new Brain(new CommittingSettings(), seed);

                new MultiplexerRun(
                    new MultiplexerSettings { Address = address, Skew = skew }, brain, seed)
                    .Run(Rounds);

                foreach (var name in brain.Held.Names.Means.Select(one => one.Key))
                {
                    // SPELLED ALL THE WAY BACK OUT, because a name may stand for a set
                    // containing a name and the question is about the BITS underneath.
                    var unfolded = brain.Held.Names.Unfold([name]);

                    var positions = unfolded
                        .Where(one => one.Modality == Multiplexer.Bit)
                        .Select(one => (int)(one.Value >> 1))
                        .ToList();

                    if (positions.Count == 0) continue;

                    names++;
                    members += positions.Count;

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
                + $"| one position twice {spanning} | mean bits {(names == 0 ? 0 : members / names):F2}");
        }

        // NO BAR. What a vocabulary SHOULD look like has never been measured, and a threshold
        // written before the first reading would be the answer rather than the finding.
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void How_long_the_sound_rules_it_holds_are_and_how_many_stand_on_a_name()
    {
        // A SOUND RULE IS NOT A GOOD RULE, AND NOTHING IN THIS REPO SEPARATED THEM UNTIL THIS
        // GRID. The shortest scope that can be true of an eleven-bit multiplexer pins the
        // three address bits and the one data bit they select -- four codes. Anything longer
        // pins bits the truth does not depend on: still sound, still never wrong, and firing
        // on a narrower slice of the world every code it adds. A count of true rules cannot
        // tell those apart and a run's score need not either.
        //
        // AND THE COLUMN BESIDE IT IS WHERE THEY COME FROM. A minted name is ADDED to every
        // moment holding its members, so it is a code like any other and repair may add one --
        // which makes a name a step of TWO codes past a bar that is paid once. That is the
        // mechanism that deleted `Minting.UntilRefused`: asking until the gate refused took
        // the share of sound rules standing on a name from 67% to 89% and the sound rules of
        // six codes or more from 132 to 334, while the rules of the minimal four rose by a
        // third. More of the world, said less usefully. See the plan's revival row.
        //
        // SO THIS IS KEPT AS THE STANDING READING OF WHAT THE SHIPPED ARM BUILDS, because the
        // over-specialisation it measures is not a fact about the deleted loop -- the shipped
        // arm already holds a mean of 5.30 codes where four would do.
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

                    // THE SCOPE AS HELD AND NOT AS SPELLED OUT, which is the whole question.
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
        // THE PROPERTY THE WHOLE NAMING-OVER-A-WIRE ARC EXISTS FOR. Three holders minting
        // three different sets of names is three languages, and nothing downstream of that
        // means anything.
        //
        // AND IT IS ASSERTED THROUGH `Abstract` RATHER THAN AT THE GATE, WHICH IS WHY IT LIVES
        // HERE AND NOT IN `SplitNamingTests`. That file asks the gate a question over merged
        // counts; this runs the whole operator, so the REWRITE is in scope -- a holder that
        // agreed on the name and then rewrote a different set of scopes would pass there and
        // fail here.
        //
        // THE SHIPPED DIALS, BECAUSE A POOR POPULATION CANNOT SHOW THIS. An earlier take used
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
            + $"{holders.Sum(held => held.Names.Count)} | {vocabularies[0]}");

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
        // THE CEILING NOTHING HAS EVER REPORTED, AND IT IS NOT IN THE GATE AT ALL.
        // `Abstract` mints at most ONE name per call and is called once a sweep round, so a
        // twenty-thousand-round run at the default cadence offers rung five exactly twenty
        // chances. `named` cannot exceed twenty however much a population holds.
        //
        // WHICH MAKES *NAMES PER ELIGIBLE SCOPE* A RATIO WITH A CAPPED NUMERATOR. The
        // denominator is what repair built and grows with the budget without bound; the
        // numerator is a calendar constant. It MUST fall once the gate saturates, and it
        // would fall in exactly the same way if abstraction were perfect.
        //
        // SO THE READING IS TAKEN AGAINST THE CADENCE RATHER THAN AGAINST THE BUDGET. If
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

        // AND `sweep` IS NOT ONE AXIS, WHICH THE GRID SHOWS RATHER THAN HIDES. `Council`
        // widens, subsumes, abstracts and culls inside one branch, so asking rung five more
        // often also culls more often -- and `eligible` FALLS as the cadence tightens, which
        // is the denominator being moved by a mechanism this reading is not about. A cell
        // that separated them would need abstraction on its own calendar, and there is no
        // such dial. This repo's own trap: a setting deciding two independent things while
        // being named for one.
        //
        // SO WHAT THIS GRID SETTLES IS THE CEILING AND NOT THE SLOPE. At the shipped cadence
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
        // THE ARM ABOVE IS A MODEL AND THIS IS THE THING ITSELF. Two aggregate columns can
        // both fall for reasons that have nothing to do with each other -- a run holding a
        // DIFFERENT best pair at every budget would print exactly the dilution signature
        // while no redundancy was ever diluted. So the pair is fixed at the bottom of the
        // ladder and followed, which is the only version of this question that has one
        // subject.
        //
        // AND IT IS THE COUNTS AND NOT THE VERDICT THAT ARE READ, because a verdict is the
        // two terms already multiplied together. `seen` against `scopes` is the share; the
        // marginals are what independence is built from; the two of them are the whole
        // mechanism and either can be flat while the other moves.
        // AND OVER SEEDS, BECAUSE ONE SEED IS NOT A COMPARISON AND WILL HAPPILY INVERT.
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

            // THE TABLE READ THROUGH THE FORM THAT CROSSES A WIRE, because that is the
            // public one and a probe reaching past it would be measuring something a holder
            // could not be told.
            var rows = counted.Written().Rows;

            // THE SUBJECT IS CHOSEN ONCE, AT THE SMALLEST BUDGET, and never re-chosen. What
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
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task Which_bar_takes_rung_fives_yield_as_the_repair_budget_grows()
    {
        // THE SAME BUDGETS AND THE SAME FOUR WORLDS `BudgetCurveTests` READS, taken from one
        // place so the two grids are comparable by construction rather than by both files
        // having been written carefully.
        //
        // AND ALL FOUR, BECAUSE THE FIRST TAKE OF THIS GRID RAN ON ONE OF THEM AND CAME BACK
        // WITH THE OPPOSITE OF THE OPEN DEFECT. On eleven bits even, names RISE with the
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
            // ONE RUN PER SEED, SHARED BY EVERY READING BELOW. Readings asked
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

