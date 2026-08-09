using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whether the repair budget is a search limit or a re-derivation limit —
/// <b>the question the lineage ladder handed over.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>COLLISIONS RUN TWENTY TO FIFTY TIMES THE BIRTHS AT EVERY MAJORITY RUNG, AND EACH ONE
/// SPENDS THE PARENT'S BUDGET.</b> <see cref="Population.Mend"/> records the child before
/// asking whether it was new, so a parent that keeps separating on the same code exhausts
/// sixty-four attempts on one distinct child. See <see cref="Budgeting"/>.
/// </para>
/// <para>
/// <b>THE PREDICTION IS WRITTEN DOWN BEFORE THE RUN AND IT IS SPECIFIC ENOUGH TO BE
/// WRONG.</b> If the budget is mostly spent on re-derivation, counting distinct children
/// takes <c>exhausted</c> towards nought and raises repairs and sound rules; if the budget
/// was not binding under this timing, every column stays inside its error bars and the
/// plan's standing puzzle about an interior optimum needs a different answer.
/// </para>
/// <para>
/// <b>AND IT IS ASKED UNDER <see cref="Repairing.EveryRound"/>, WHICH IS WHY IT IS A NEW
/// QUESTION.</b> Loosening the budget under the shipped timing bought nothing over eight
/// seeds — but the lineages that would have spent it were never blamed, so nothing was
/// waiting on the gate. Both timings are run here so that null is visible rather than
/// assumed.
/// </para>
/// <para>
/// <b>AND THE ANSWER IS THAT IT HAS NEVER BOUND ON CHILDREN AT ALL, WHICH IS ARITHMETIC
/// ONCE IT IS SAID OUT LOUD.</b> A child adds one code, so a parent's distinct children are
/// bounded by the vocabulary — twelve codes at six bits and twenty-two at eleven, against a
/// budget of sixty-four. <c>exhausted</c> is exactly nought in every cell under
/// <see cref="Budgeting.Children"/> because it cannot be anything else, so that arm is a
/// FREE budget and the tripwire below says when it stops being one.
/// </para>
/// <para>
/// <b>AND THE TWO FINDINGS COMPOSE INTO A SECOND PREDICTION, WHICH IS WHY ELEVEN BITS EVEN
/// IS IN THE GRID.</b> <see cref="Repairing.EveryRound"/> walks the culprits on every round
/// rather than on the wrong seventh of them, so a parent spends its attempts about seven
/// times faster — and there it repairs LESS and holds seventeen fewer sound rules than the
/// shipped timing, at nearly five standard errors. If attempts are what the budget counts,
/// that loss is this dial's and counting distinct children takes it back; if the loss
/// survives, the two are unrelated and the explanation above is wrong about that half.
/// </para>
/// </remarks>
public sealed class BudgetingTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;
    private const int Runs = 8;

    /// <summary>Fixed forever, and deliberately not any other sweep's word.</summary>
    private const uint Purpose = 0x5EED_0066;

    /// <summary>
    /// <b>WHAT THE BUDGET IS ACTUALLY SPENDING, UNDER BOTH TIMINGS.</b>
    /// </summary>
    [Fact]
    public void Whether_counting_distinct_children_instead_of_attempts_changes_anything()
    {
        var arms = new (string Name, CommittingSettings Dials)[]
        {
            ("afterfailure attempts", new CommittingSettings()),
            ("afterfailure children", new CommittingSettings
            {
                Budgeting = Budgeting.Children,
            }),
            ("everyround attempts", new CommittingSettings
            {
                Repairing = Repairing.EveryRound,
            }),
            ("everyround children", new CommittingSettings
            {
                Repairing = Repairing.EveryRound,
                Budgeting = Budgeting.Children,
            }),
        };

        foreach (var (address, skew) in new[] { (2, 0.8), (3, 0.8), (3, 0.0) })
        {
            // THE CEILING ON DISTINCT CHILDREN, WHICH IS ARITHMETIC AND NOT A RESULT. A
            // child adds one code to its parent's scope, so a parent can never have more
            // distinct children than the world has codes -- two per bit here. At sixty-four
            // the budget sits far above that on every multiplexer, so counting distinct
            // children cannot bind AT ALL and `Budgeting.Children` is a free budget wearing
            // a limit's name. That is why `exhausted` is exactly nought below rather than
            // nearly nought, and it is the whole explanation.
            var vocabulary = 2 * (address + (1 << address));
            var budget = new CommittingSettings().Budget;

            output.WriteLine($"=== {address + (1 << address)} bits, skew {skew:F1}, "
                + $"{Runs} seeds, budget {budget} against {vocabulary} codes");

            // A TRIPWIRE RATHER THAN A BAR, AND THE SAME SHAPE `LiftingTests` USES. This
            // goes red the day a world arrives whose vocabulary could reach the budget --
            // which is the day `Budgeting.Children` stops being a free budget and the
            // reading below has to be taken again rather than cited.
            Assert.True(vocabulary < budget,
                $"a world here now has {vocabulary} codes against a budget of {budget}, so "
                + "counting distinct children can finally bind -- the arm below is no "
                + "longer equivalent to no budget at all and its numbers must be retaken");
            output.WriteLine(
                $"arm                    {"exhausted",-20} {"repaired",-20} "
                + $"{"paying",-20} {"sound",-20} recent");

            foreach (var (name, dials) in arms)
            {
                var settings = new MultiplexerSettings { Address = address, Skew = skew };

                var exhausted = new List<double>();
                var repaired = new List<double>();
                var paying = new List<double>();
                var sound = new List<double>();
                var recent = new List<double>();

                for (var index = 1; index <= Runs; index++)
                {
                    var seed = Seeds.Apart(index, Purpose);

                    var learnt = new MultiplexerRun(
                        settings, new Brain(dials, seed), seed, census: true).Run(Rounds);

                    exhausted.Add(learnt.Exhausted);
                    repaired.Add(learnt.Repaired);
                    paying.Add(learnt.Census!.Paying);
                    sound.Add(learnt.Sound);
                    recent.Add(learnt.Recent);
                }

                output.WriteLine(
                    $"{name,-22} {Show(exhausted),-20} {Show(repaired),-20} "
                    + $"{Show(paying),-20} {Show(sound),-20} {recent.Average():F3}");
            }

            output.WriteLine("");
        }
    }

    /// <summary>A mean and its standard error, in one column.</summary>
    /// <param name="values">One reading, one entry a seed.</param>
    private static string Show(IReadOnlyList<double> values) =>
        $"{new Measured { Arm = "x", Values = values }.Mean,8:F2} "
        + $"+/- {new Measured { Arm = "x", Values = values }.StdErr:F2}";
}
