using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What becomes of a lineage between its seed and a finished rule — <b>the one link in
/// the chain nothing has instrumented.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>SIX EXPLANATIONS DIED TO CONTROLS AND EVERY ONE OF THEM CHANGED SELECTION.</b> The
/// minority seeds are present, repair's choice of condition beats a random draw by the
/// whole distance between sixteen sound rules and none, repair runs hundreds of times, and
/// true rules come out at perfect accuracy — while not one of them fires on a round the
/// base rate gets wrong. So this is a ledger and not a seventh story: it changes no
/// behaviour and answers where in the ladder a minority lineage stops.
/// </para>
/// <para>
/// <b>A SEED IS ONE CODE AND A TRUE RULE IS <c>Address + 1</c>, so something has to survive
/// two specialisations at six bits and three at eleven.</b> Whether anything ever reaches
/// the last rung, and what removes it if it does, is what
/// <see cref="Population.Lineages"/> was built to say — see <see cref="Lifetime"/> for why
/// an expectation and a scope length name a lineage and its rung exactly.
/// </para>
/// </remarks>
public sealed class LineageTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;
    private const int Seeds = 4;

    /// <summary>
    /// <b>THE LADDER, RUNG BY RUNG, SPLIT BY THE OUTCOME THE LINEAGE EXPECTS.</b>
    /// </summary>
    [Fact]
    public void Where_a_minority_lineage_stops_between_its_seed_and_a_true_rule()
    {
        foreach (var (address, skew) in new[] { (2, 0.0), (2, 0.8), (3, 0.8) })
        {
            var settings = new MultiplexerSettings { Address = address, Skew = skew };

            output.WriteLine(
                $"=== {address + (1 << address)} bits, skew {skew:F1}, "
                + $"a true rule is {address + 1} codes, over {Seeds} seeds");
            output.WriteLine(
                "class    rung   blamed searched  covered repaired  collided"
                + "  subsumed renamed  resident  sound  seeds");

            var ledger = new Dictionary<(bool Minority, int Depth), Lifetime>();
            var resident = new Dictionary<(bool Minority, int Depth), int>();
            var sound = new Dictionary<(bool Minority, int Depth), int>();
            var reached = new Dictionary<(bool Minority, int Depth), int>();

            for (var seed = 1; seed <= Seeds; seed++)
            {
                var brain = new Brain(new CommittingSettings(), seed);
                var run = new MultiplexerRun(settings, brain, seed, census: true);

                run.Run(Rounds);

                var minority = Minority(settings);
                var held = brain.Held;

                // THE BALANCE, AND IT IS THE LEDGER'S OWN CHECK. Births minus losses at
                // one expectation and length is how many of that shape are resident,
                // walked from a completely different table -- so a call site this ledger
                // has missed says so here rather than under-reporting a death forever.
                var standing = held.All
                    .GroupBy(one => (one.Expects, one.Scope.Length))
                    .ToDictionary(one => one.Key, one => one.Count());

                foreach (var (shape, life) in held.Lineages)
                {
                    Assert.Equal(
                        standing.GetValueOrDefault(shape),
                        (int)(life.Born - life.Lost));
                }

                // AND THE SECOND CHECK IS AGAINST A COUNTER WRITTEN FOR A DIFFERENT
                // QUESTION. `Wrong` and `Searched` partition the same walk by GATE where
                // this partitions it by LINEAGE, so the two totals agree exactly or one of
                // them is describing a machine that is not running.
                Assert.Equal(held.Wrong, held.Lineages.Values.Sum(one => one.Blamed));
                Assert.Equal(held.Searched, held.Lineages.Values.Sum(one => one.Searched));

                foreach (var (shape, life) in held.Lineages)
                {
                    var at = (Minority: shape.Expects == minority, shape.Depth);

                    ledger[at] = Merge(ledger.GetValueOrDefault(at), life);

                    if (life.Born > 0) reached[at] = reached.GetValueOrDefault(at) + 1;
                }

                var world = new Multiplexer(settings, seed);

                foreach (var one in held.All)
                {
                    var at = (Minority: one.Expects == minority, one.Scope.Length);
                    var scope = held.Names.Unfold(one.Scope);

                    resident[at] = resident.GetValueOrDefault(at) + 1;

                    if (one.Seen >= brain.Dials.Floor
                        && world.Checkable(scope)
                        && world.Sound(scope, one.Expects))
                        sound[at] = sound.GetValueOrDefault(at) + 1;
                }
            }

            foreach (var at in ledger.Keys.OrderBy(one => one.Minority).ThenBy(one => one.Depth))
            {
                var life = ledger[at];

                output.WriteLine(
                    $"{(at.Minority ? "minority" : "majority"),-8} {at.Depth,4}  "
                    + $"{life.Blamed,7} {life.Searched,8}  "
                    + $"{life.Covered,7} {life.Repaired,8}  {life.Collided,8}"
                    + $"  {life.Subsumed,8} {life.Rewritten,7}"
                    + $"  {resident.GetValueOrDefault(at),8} {sound.GetValueOrDefault(at),6}"
                    + $"  {reached.GetValueOrDefault(at),5}");
            }

            output.WriteLine("");
        }
    }

    /// <summary>
    /// <b>WHETHER WHAT REDIRECTS BLAME IS WHAT PAYS, WHICH IS THE READING'S OWN KILL
    /// CONDITION.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE LADDER SAYS A MINORITY LINEAGE IS NEVER OFFERED TO REPAIR, AND THAT IS AN
    /// OBSERVATION RATHER THAN A MECHANISM.</b> <see cref="Repairing.AfterFailure"/> runs
    /// <see cref="Population.Mend"/> only on a round the VOTE got wrong; under skew nearly
    /// every such round is a minority-outcome round, and on one of those a
    /// minority-expecting commitment expected CORRECTLY and cannot be a culprit. So the
    /// only rounds repair may run on are the rounds where minority rules are right.
    /// </para>
    /// <para>
    /// <b>TWO ARMS REACH THAT COUPLING FROM OPPOSITE ENDS AND NEITHER WAS BUILT FOR IT.</b>
    /// <see cref="Weighing.Lifting"/> makes the vote say the rare answer, so the vote
    /// starts being wrong on MAJORITY rounds and the blame lands on minority lineages.
    /// <see cref="Repairing.EveryRound"/> removes the coupling outright. If this reading is
    /// right both raise minority blame; if minority blame is flat under the arm that takes
    /// hard-round coverage from nought to nearly all, the reading is wrong and goes the way
    /// of the other six.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_the_arms_that_pay_are_the_arms_that_redirect_blame()
    {
        var arms = new (string Name, CommittingSettings Dials)[]
        {
            ("shipped", new CommittingSettings()),
            ("lifting", new CommittingSettings { Weighing = Weighing.Lifting }),
            ("everyround", new CommittingSettings { Repairing = Repairing.EveryRound }),
        };

        foreach (var (address, skew) in new[] { (2, 0.8), (3, 0.8) })
        {
            var settings = new MultiplexerSettings { Address = address, Skew = skew };
            var minority = Minority(settings);
            var rung = address + 1;

            output.WriteLine($"=== {address + (1 << address)} bits, skew {skew:F1}, "
                + $"a true rule is {rung} codes, over {Seeds} seeds");
            output.WriteLine(
                "arm           blamed  minority  share   repaired@rung  sound@rung"
                + "   paying  recent");

            foreach (var (name, dials) in arms)
            {
                long blamed = 0, mine = 0, repaired = 0;
                var sound = 0;
                double paying = 0, recent = 0;

                for (var seed = 1; seed <= Seeds; seed++)
                {
                    var brain = new Brain(dials, seed);
                    var run = new MultiplexerRun(settings, brain, seed, census: true);

                    var learnt = run.Run(Rounds);
                    var held = brain.Held;
                    var world = new Multiplexer(settings, seed);

                    blamed += held.Lineages.Values.Sum(one => one.Blamed);

                    mine += held.Lineages
                        .Where(one => one.Key.Expects == minority)
                        .Sum(one => one.Value.Blamed);

                    repaired += held.Lineages
                        .Where(one => one.Key.Expects == minority && one.Key.Depth == rung)
                        .Sum(one => one.Value.Repaired);

                    sound += held.All.Count(one =>
                        one.Expects == minority
                        && one.Scope.Length == rung
                        && one.Seen >= dials.Floor
                        && world.Checkable(held.Names.Unfold(one.Scope))
                        && world.Sound(held.Names.Unfold(one.Scope), one.Expects));

                    paying += learnt.Census!.Paying;
                    recent += learnt.Recent;
                }

                output.WriteLine(
                    $"{name,-10} {blamed,9} {mine,9} {mine / (double)blamed,6:P1}"
                    + $"  {repaired,13} {sound,11}"
                    + $"   {paying / Seeds,6:P1}  {recent / Seeds,6:F3}");
            }

            output.WriteLine("");
        }
    }

    /// <summary>
    /// <b>THE MECHANISM AS A CURVE RATHER THAN AS TWO POINTS.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE READING SO FAR IS AN EVEN WORLD AND A SKEWED ONE, WHICH IS TWO POINTS AND A
    /// STORY JOINING THEM.</b> If blame starvation is really what stops the lineages, then
    /// the minority's share of blame should fall smoothly as the world tilts, hard-round
    /// coverage should fall with it, and the arm that does not consult the vote should be
    /// flat across the whole range. Three curves with one shape is a mechanism; a step
    /// between two cells is a pair of measurements.
    /// </para>
    /// <para>
    /// <b>AND IT CAN COME OUT WRONG IN A WAY THE TWO POINTS COULD NOT.</b> A threshold — the
    /// shipped arm holding up to some tilt and then collapsing — would say something is
    /// switching rather than starving, and the explanation would need the switch. What is
    /// predicted is monotone and gradual in both.
    /// </para>
    /// </remarks>
    [Fact]
    public void How_blame_and_coverage_fall_together_as_the_world_tilts()
    {
        output.WriteLine($"=== 6 bits, {Seeds} seeds, blame share is the minority's");
        output.WriteLine(
            "skew   drawn    afterfailure                     everyround");
        output.WriteLine(
            "               blame   paying  recent            blame   paying  recent");

        foreach (var skew in new[] { 0.0, 0.5, 0.65, 0.8, 0.9 })
        {
            var settings = new MultiplexerSettings { Address = 2, Skew = skew };
            var minority = Minority(settings);

            var row = new List<string>();
            var drawn = 0.0;

            foreach (var repairing in new[] { Repairing.AfterFailure, Repairing.EveryRound })
            {
                var dials = new CommittingSettings { Repairing = repairing };

                double blame = 0, paying = 0, recent = 0;

                for (var seed = 1; seed <= Seeds; seed++)
                {
                    var brain = new Brain(dials, seed);
                    var learnt = new MultiplexerRun(settings, brain, seed, census: true)
                        .Run(Rounds);

                    var all = brain.Held.Lineages.Values.Sum(one => one.Blamed);

                    var mine = brain.Held.Lineages
                        .Where(one => one.Key.Expects == minority)
                        .Sum(one => one.Value.Blamed);

                    blame += all == 0 ? 0.0 : mine / (double)all;
                    paying += learnt.Census!.Paying;
                    recent += learnt.Recent;

                    // HOW OFTEN THE RARE OUTCOME ACTUALLY ARRIVED, taken from the census
                    // rather than from the setting -- `Skew` is a property of the BITS and
                    // the outcome's rate is what the multiplexer makes of them, which is
                    // not the same number and would misread the whole x axis.
                    drawn = learnt.Census.Hard / (double)learnt.Rounds;
                }

                row.Add($"{blame / Seeds,6:P1}  {paying / Seeds,6:P1}  {recent / Seeds,6:F3}");
            }

            output.WriteLine(
                $"{skew,4:F2}  {drawn,6:P1}   {row[0],-32}  {row[1]}");
        }
    }

    /// <summary>Adds one seed's ledger into a running total.</summary>
    /// <param name="into">What has been summed so far.</param>
    /// <param name="one">This seed's counts.</param>
    private static Lifetime Merge(Lifetime into, Lifetime one) => new()
    {
        Covered = into.Covered + one.Covered,
        Repaired = into.Repaired + one.Repaired,
        Widened = into.Widened + one.Widened,
        Reborn = into.Reborn + one.Reborn,
        Subsumed = into.Subsumed + one.Subsumed,
        Culled = into.Culled + one.Culled,
        Rewritten = into.Rewritten + one.Rewritten,
        Collided = into.Collided + one.Collided,
        Blamed = into.Blamed + one.Blamed,
        Searched = into.Searched + one.Searched,
    };

    /// <summary>
    /// The outcome this world produces LEAST often, read from what it draws.
    /// </summary>
    /// <param name="settings">The world's shape.</param>
    /// <remarks>
    /// <b>DRAWN RATHER THAN DECLARED, which is the discipline <c>Census.Hard</c> uses.</b>
    /// A harness that read the skew off the settings would be naming the minority by a
    /// fact the learner cannot see, and on a world whose skew is nought the answer is a
    /// coin toss that has to be taken from the same place either way.
    /// </remarks>
    private static Code Minority(MultiplexerSettings settings)
    {
        var world = new Multiplexer(settings, seed: 99);
        var drawn = new Dictionary<Code, int>();

        for (var draw = 0; draw < 5_000; draw++)
        {
            var answer = world.Next().Answer;
            drawn[answer] = drawn.GetValueOrDefault(answer) + 1;
        }

        return drawn.OrderBy(one => one.Value).ThenBy(one => one.Key).First().Key;
    }
}
