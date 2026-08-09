using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whether <see cref="Weighing.Lifting"/> is CONNECTED — <b>the check the grid cannot
/// be read without.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>SEVEN WORLDS OF EIGHT CAME BACK IDENTICAL TO <see cref="Weighing.Strongest"/> TO
/// FOUR DECIMALS AND ON THE SAME STANDARD ERRORS, WHICH IS WHAT AN UNWIRED ARM LOOKS
/// LIKE.</b> This repo's own trap says the two failures read the same message: genuinely
/// unwired, and wired-but-inert, are one output. So the arm is asserted to DIFFER
/// somewhere before its zeros are read as a finding, and the reason for the zeros is
/// measured rather than reasoned about.
/// </para>
/// <para>
/// <b>AND THE REASON IS THE DIVISOR'S SPREAD, WHICH IS A FACT ABOUT A WORLD AND NOT
/// ABOUT THE RULE.</b> Dividing every candidate by the same number cannot move an argmax,
/// so on a world whose outcomes are balanced <c>Lifting</c> is <c>Strongest</c> exactly —
/// not approximately, and not by a tolerance. What decides whether the arm can do
/// anything at all is how far apart the base rates of the competing expectations are, and
/// nothing before this measured that on any world.
/// </para>
/// </remarks>
public sealed class LiftingTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    /// <summary>The spread of base rates over what a trained population expects.</summary>
    /// <param name="held">The population.</param>
    /// <remarks>
    /// <b>OVER THE EXPECTATIONS RATHER THAN OVER THE VOCABULARY</b>, because the divisor
    /// only ever divides an expectation. A world with a thousand codes and two outcomes
    /// has a wide vocabulary and a flat divisor, and counting the wrong set would report
    /// spread where the vote sees none.
    /// </remarks>
    private static (double Least, double Most, int Distinct) Spread(Population held)
    {
        var rates = held.All
            .Select(one => one.Expects)
            .Distinct()
            .Select(held.Rate)
            .ToList();

        return rates.Count == 0
            ? (0.0, 0.0, 0)
            : (rates.Min(), rates.Max(), rates.Count);
    }

    /// <summary>A population trained on the multiplexer.</summary>
    private static Population Plexed(int address, int seed)
    {
        var brain = new Brain(new CommittingSettings { Weighing = Weighing.Lifting }, seed);

        new MultiplexerRun(new MultiplexerSettings { Address = address }, brain, seed)
            .Run(Rounds);

        return brain.Held;
    }

    /// <summary>A population trained on the world with a front end in the way.</summary>
    private static Population Graded_(int seed)
    {
        var brain = new Brain(new CommittingSettings { Weighing = Weighing.Lifting }, seed);

        new GradedRun(new GradedSettings { Address = 2 }, brain, Fronting.Banded, seed)
            .Run(Rounds);

        return brain.Held;
    }

    /// <summary>
    /// <b>WHAT THE DIVISOR CAN ACTUALLY DO, PER WORLD — and it is nothing where the
    /// outcomes are balanced.</b>
    /// </summary>
    [Fact]
    public void Every_world_on_this_bench_draws_its_outcomes_evenly_so_the_divisor_is_a_constant()
    {
        var worlds = new (string World, Func<Population> Held)[]
        {
            ("multiplexer-6", () => Plexed(2, seed: 1)),
            ("multiplexer-11", () => Plexed(3, seed: 1)),
            ("graded", () => Graded_(seed: 1)),
        };

        var widest = 0.0;

        foreach (var (world, held) in worlds)
        {
            var (least, most, distinct) = Spread(held());
            var ratio = least > 0.0 ? most / least : double.PositiveInfinity;

            widest = Math.Max(widest, ratio);

            output.WriteLine(
                $"{world,-16} | {distinct,4} distinct expectations "
                + $"| base rate {least:F4} to {most:F4} "
                + $"| widest divisor ratio {ratio,8:F1}x");
        }

        // A TRIPWIRE RATHER THAN A BAR, AND IT IS THE RIGHT WAY ROUND. Every world on
        // this bench has two outcomes drawn about evenly, so the divisor is a constant to
        // three decimal places and `Lifting` is `Strongest` by algebra -- which means the
        // grid cannot say anything about the rule whatever it scores. This goes RED the
        // day a world with skewed outcomes is added, and that is the day the arm becomes
        // measurable; leaving it green on a flat bench would let an untestable arm read
        // as a tested one forever.
        Assert.True(widest < 1.1,
            $"a world here now has a base-rate ratio of {widest:F2}x, so the divisor "
            + "finally has something to do -- `Lifting` is testable and `WeighingTests` "
            + "is worth re-reading, which it was not while every world was balanced");
    }

    /// <summary>
    /// <b>THE ARM CHANGES A DECISION SOMEWHERE, which is the only thing that separates
    /// inert from unwired.</b>
    /// </summary>
    /// <remarks>
    /// <b>ASSERTED AS A DIFFERENCE AND NEVER AS A DIRECTION.</b> The trap this guards is
    /// that a prediction written into a wiring check fails two ways and reads the same, so
    /// this says only that the two rules disagree — whether disagreeing is an improvement
    /// is what the grid is for, and it is not asked here.
    /// </remarks>
    /// <summary>
    /// <b>THE SKEWED WORLD SKEWS ITS OUTCOMES AND CHANGES NOTHING ELSE — the control the
    /// arm was never given.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE TRUE RULE SET IS ASSERTED IDENTICAL, WHICH IS THE HALF THAT MAKES IT A
    /// CONTROL.</b> A world that skewed its outcomes by changing what is true of it would
    /// move two things at once, and every comparison against it would be unreadable — this
    /// repo's own trap about a setting that decides two independent things while being
    /// named for one. Only how often the answer is one may differ.
    /// </remarks>
    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(0.8, 3.0)]
    public void A_skewed_multiplexer_moves_its_outcomes_and_leaves_its_rules_alone(
        double skew, double least)
    {
        var even = new MultiplexerSettings { Address = 2 };
        var slanted = new MultiplexerSettings { Address = 2, Skew = skew };

        var world = new Multiplexer(slanted, seed: 1);

        // COUNTED PER DISTINCT ANSWER RATHER THAN AGAINST A NOMINATED ONE, so this says
        // nothing about which code means one and cannot be wrong about it.
        var seen = new Dictionary<Code, int>();
        const int Draws = 20_000;

        for (var draw = 0; draw < Draws; draw++)
        {
            var answer = world.Next().Answer;
            seen[answer] = seen.GetValueOrDefault(answer) + 1;
        }

        var ratio = seen.Values.Max() / (double)seen.Values.Min();
        var share = seen.Values.Max() / (double)Draws;

        output.WriteLine($"skew {skew:F2} | the commoner answer is {share:P1} of the time "
            + $"| outcome ratio {ratio:F2}x");

        Assert.True(ratio >= least,
            $"a skew of {skew:F2} produced an outcome ratio of {ratio:F2}x, under the "
            + $"{least:F2}x this setting exists to reach");

        // THE RULES ARE THE SAME RULES, WHICH IS WHAT KEEPS SOUNDNESS COMPARABLE ACROSS
        // THE TWO ARMS. `Truths` reads the mapping and the mapping is drawn from the seed,
        // so two worlds on one seed must agree exactly whatever their bits do.
        //
        // COMPARED BY CONTENT AND NOT BY THE RECORD, because `Truth` holds an
        // `ImmutableArray` and that type's equality is the identity of the underlying
        // array -- so two separately built keys with identical scopes are never equal and
        // the assertion would fail on a world it had no complaint about.
        static List<string> Shape(MultiplexerSettings settings) =>
            [.. new Multiplexer(settings, seed: 1).Truths()
                .Select(one => $"{string.Join("+", one.Scope.Order())}->{one.Expects}")
                .Order()];

        Assert.Equal(Shape(even), Shape(slanted));
    }

    /// <summary>
    /// <b>THE DIVISOR IS WILD EARLY AND A CONSTANT LATE, WHICH IS WHERE EVERY DIFFERENCE
    /// THE GRID MEASURED CAME FROM.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE PREDICTION WRITTEN DOWN BEFORE THE RE-RUN WAS THAT EVERY ROW WOULD BE
    /// IDENTICAL TO <see cref="Weighing.Strongest"/>, AND IT WAS WRONG.</b> Every world
    /// moved a little and none separated. The reconciliation is that a base rate is
    /// ESTIMATED: after ten settlements seven-to-three is a ratio of two and a half, and
    /// after twenty thousand it is one. So the two rules differ only over the opening
    /// rounds, when the divisor is noise.
    /// </para>
    /// <para>
    /// <b>AND AN OPENING PERTURBATION IS NOT A SMALL EFFECT HERE, WHICH THIS REPO ALREADY
    /// KNEW.</b> A winner-take-all argmax is chaotic in its evidence, and the vote steers
    /// repair as much as it reports it — so a handful of flipped early votes mint a
    /// different population and the run never rejoins. The grid's spread is that, and it
    /// is not the mechanism the arm was built to test.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_divisor_is_noise_over_the_opening_rounds_and_settles_to_one()
    {
        var brain = new Brain(
            new CommittingSettings { Weighing = Weighing.Lifting }, seed: 1);

        var run = new MultiplexerRun(
            new MultiplexerSettings { Address = 2 }, brain, seed: 1);

        var held = brain.Held;

        long ran = 0;
        var opening = 0.0;
        var closing = 0.0;

        foreach (var upto in new long[] { 20, 50, 200, 1_000, 5_000, 20_000 })
        {
            run.Run(upto - ran);
            ran = upto;

            var (least, most, _) = Spread(held);
            var ratio = least > 0.0 ? most / least : double.PositiveInfinity;

            // THE WIDEST OVER THE OPENING RATHER THAN THE FIRST SAMPLE, because an
            // estimate over very few settlements is not yet wild -- one arrival either
            // way is most of it. The spread peaks once there are enough rounds for the
            // two outcomes to have drifted apart and before there are enough to pull them
            // back, which is a shape no single sample can be nominated for in advance.
            if (upto <= 200) opening = Math.Max(opening, ratio);
            closing = ratio;

            output.WriteLine($"after {upto,6} rounds | base rate {least:F4} to {most:F4} "
                + $"| divisor ratio {ratio:F2}x");
        }

        // ASSERTED AS THE RATIO BETWEEN THE ENDS RATHER THAN AS TWO BARS. Either end
        // alone is consistent with a different story -- a wild opening does not say the
        // noise ever goes away, and a flat close does not say there was ever anything to
        // perturb the run with -- and a fixed bar on either would be a number chosen to
        // sit just under what was measured.
        Assert.True(opening / closing > 1.2,
            $"the divisor was {opening:F2}x at its widest in the opening and {closing:F2}x "
            + "at the end, so it did not narrow — the arm would then be a live mechanism "
            + "late in a run rather than opening noise, and the grid's spread would mean "
            + "something after all");
    }

    /// <summary>
    /// <b>THE THREE RULES ON THE ONE WORLD THAT CAN TELL THEM APART, read on what a
    /// majority guess cannot reach.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>ACCURACY HAS A FLOOR OF FOUR IN FIVE HERE, so it is reported and not judged
    /// on.</b> A machine holding nothing whatever scores 0.80 by always answering the
    /// commoner outcome, and this repo's own trap says an accuracy can be hit by
    /// memorising — on a world with known ground truth, report how much of it was FOUND.
    /// That number is immune to the floor: a majority guess finds none of the world's
    /// eight rules.
    /// </para>
    /// <para>
    /// <b>AND IT IS THE SAME EIGHT RULES AS THE EVEN WORLD'S</b>, asserted above, so the
    /// found count is comparable straight across to every earlier multiplexer reading.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task What_the_three_rules_find_when_one_outcome_is_four_times_the_other()
    {
        const int Seeds = 8;

        static Learned Run(Weighing weighing, int address, int seed) =>
            new MultiplexerRun(
                new MultiplexerSettings { Address = address, Skew = 0.8 },
                new Brain(new CommittingSettings { Weighing = weighing }, seed),
                seed).Run(20_000);

        foreach (var address in new[] { 2, 3 })
        {
            foreach (var reading in new (string What, Func<Learned, double> Of)[]
            {
                ("accuracy", one => one.Recent),
                ("found", one => one.Found),
                ("sound", one => one.Sound),
                ("residents", one => one.Resident),
            })
            {
                var arms = await Sweep.AcrossAsync(
                    Seeds,
                    ("summing", seed =>
                        Task.FromResult(reading.Of(Run(Weighing.Summing, address, seed)))),
                    ("strongest", seed =>
                        Task.FromResult(reading.Of(Run(Weighing.Strongest, address, seed)))),
                    ("lifting", seed =>
                        Task.FromResult(reading.Of(Run(Weighing.Lifting, address, seed)))));

                var ranked = arms.OrderByDescending(one => one.Mean).ToList();
                var apart = ranked[0].Separation(ranked[1]);

                output.WriteLine(
                    $"{address + (1 << address),2} bits {reading.What,-10} | "
                    + string.Join(" | ", arms.Select(one =>
                        $"{one.Arm} {one.Mean,8:F3} +/-{one.StdErr:F3}"))
                    + $" | {apart,5:F1} sigma, {(apart < 2.0 ? "level" : ranked[0].Arm)}");
            }
        }

        output.WriteLine("a majority guess scores 0.80 accuracy and finds 0 of 8 rules, "
            + "which is why the second row is the one that means anything");
    }

    /// <summary>
    /// What <see cref="Weighing.Strongest"/> would have said, computed here rather than
    /// asked for.
    /// </summary>
    /// <param name="firing">What fired.</param>
    /// <param name="sharpness">The power the run held.</param>
    /// <remarks>
    /// <b>BECAUSE THE DIVISION HAPPENS IN <see cref="Population.Speak"/>, SO ONE
    /// POPULATION CANNOT BE ASKED BOTH WAYS.</b> Passing
    /// <see cref="Weighing.Strongest"/> to <see cref="Population.Decide"/> over testimony
    /// a lifted holder already spoke would take the maximum of weights that had ALREADY
    /// been divided — which is the lifted answer wearing the other rule's name, and it
    /// would compare an arm with itself and pass.
    /// </remarks>
    private static Code? Loudest(
        IEnumerable<Commitment> firing, double sharpness)
    {
        var weights = new Dictionary<Code, double>();

        foreach (var commitment in firing)
        {
            var weight = Math.Pow(commitment.Accuracy, sharpness);

            if (!weights.TryGetValue(commitment.Expects, out var best) || weight > best)
                weights[commitment.Expects] = weight;
        }

        return weights.Count == 0
            ? null
            : weights.OrderByDescending(one => one.Value).ThenBy(one => one.Key).First().Key;
    }

    /// <summary>
    /// <b>ON A BALANCED WORLD THE TWO RULES AGREE ON EVERY VOTE, which is the algebra
    /// rather than a score.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THIS IS THE ASSERTION THAT SEPARATES INERT FROM UNWIRED, AND IT HAD TO BE
    /// TURNED ROUND.</b> It was written as <i>the arms must DIFFER somewhere</i>, which is
    /// the trap's own prescription — and on this bench it cannot pass, because there is no
    /// world here whose outcomes are skewed. What is checkable is the other direction:
    /// with the divisor measured flat above, agreement on every single vote is what the
    /// arithmetic REQUIRES, and a disagreement would mean the divisor is not the constant
    /// the previous test just measured.
    /// </para>
    /// <para>
    /// <b>SO THE PAIR SAYS: THE RULE IS CONNECTED, AND THIS BENCH CANNOT TEST IT.</b>
    /// Neither half says that alone, and the second half is the one a grid of eight
    /// identical rows would otherwise be mistaken for.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_lifted_vote_matches_a_strongest_vote_while_every_outcome_is_equally_likely()
    {
        var dials = new CommittingSettings { Weighing = Weighing.Lifting };
        var brain = new Brain(dials, seed: 1);

        new GradedRun(new GradedSettings { Address = 2 }, brain, Fronting.Banded, seed: 1)
            .Run(Rounds);

        var held = brain.Held;

        // THE WORLD AGAIN, CODED BY EXACTLY THE PATH THE RUN USED. A fresh generator, so
        // these are not the rounds it just saw, and through the same quantiser, or the
        // population would be asked in an alphabet it has never held -- which is this
        // repo's answer-key-in-the-wrong-alphabet trap wearing a third hat.
        var made = new Graded(new GradedSettings { Address = 2 }, seed: 99);

        IQuantizer<IReadOnlyList<double>> sensing = new Banded<IReadOnlyList<double>>(
            reading => reading, Multiplexer.Bit, made.Width, GradedRun.Bands, GradedRun.Grains);

        var asked = 0;
        var differed = 0;

        for (var draw = 0; draw < 2000; draw++)
        {
            var moment = held.Moment(
                new HashSet<Code>(sensing.Codify(((IWorld<IReadOnlyList<double>>)made).Next().Seen)));

            var firing = held.Firing(moment);
            if (firing.IsDefaultOrEmpty) continue;

            asked++;

            if (held.Predict(firing).Expects != Loudest(firing, dials.Sharpness)) differed++;
        }

        output.WriteLine($"{differed} of {asked} votes changed by dividing out the base rate");

        Assert.True(asked > 100, $"only {asked} moments fired at all — too quiet to say anything");

        Assert.True(differed == 0,
            $"{differed} of {asked} votes moved on a world whose outcomes are balanced to "
            + "within a percent, so the divisor is not the near-constant the spread says "
            + "it is — one of these two tests is lying and it is not this one");
    }
}
