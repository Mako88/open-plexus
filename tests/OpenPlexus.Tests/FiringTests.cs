using System.Globalization;
using System.Reflection;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Which of the brain's own counters ever move — <b>the reading a reachability guard cannot
/// take.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>John's, 2026-08-21.</b> <see cref="DrivenTests"/> asks whether a run reaches a
/// mechanism, and a mechanism that is reached every round and cannot fire passes it.
/// <c>Surprise</c> and <c>Abstain</c> were both wired and unable to fire for the life of the
/// branch, and no reachability walk could have said so.
/// </para>
/// <para>
/// <b>So the brain reports and the guard reads what it reported.</b> Every number in
/// <see cref="Tally"/> is a mechanism saying it ran, so a counter that never leaves nought is
/// a mechanism that never did. Nothing here is instrumented for the test — what is asserted
/// is the run's own account of itself.
/// </para>
/// <para>
/// <b>And what the brain does not report is not asked about.</b> <see cref="Tally"/> was the
/// whole of the census while it held the learner's operators alone, so a front end that
/// emitted nothing and a front end nobody asked read identically. <see cref="Fronted"/> and
/// <see cref="Chosen"/> are the other two, and a mechanism that wants to be believed can
/// start by counting itself.
/// </para>
/// <para>
/// <b>Reflected rather than listed</b>, which is the property that makes it a guard. A
/// counter added to <see cref="Tally"/> arrives here on the day it is written and has to be
/// put on one side or the other, so a mechanism cannot be reported and unfired at once
/// without somebody saying so in <see cref="Quiet"/>.
/// </para>
/// <para>
/// <b>And the runs are generated worlds</b>, because the question is about the brain rather
/// than about a corpus. Six of them rather than one: sixteen counters are quiet on the
/// multiplexer alone and thirteen survive all six, so a single world would have called eight
/// mechanisms dead that were not. Widening the set further is what turns <i>quiet here</i>
/// into <i>quiet everywhere</i>, and every entry below is only as strong as the set.
/// </para>
/// <para>
/// <b>And a sub-record the run does not build</b> is not asked about either, which is the
/// same fault one level in. <see cref="Tally.Census"/> is null unless the world is asked
/// for its truth, so fourteen counters sat outside the reading while the file read as
/// covering everything reported. Two of the six turn it on, and the reflection reaches them
/// on the day they do.
/// </para>
/// </remarks>
public sealed class FiringTests(ITestOutputHelper output)
{
    /// <summary>Six bits, which is the world step one is judged on.</summary>
    private const int Narrow = 2;

    /// <summary>How many rounds the reading is taken over.</summary>
    private const long Rounds = 4000;

    /// <summary>What the crossing world's population is held at.</summary>
    /// <remarks>
    /// <b>The one dial any world here moves</b>, and it is the world's rather than the
    /// brain's shape that asks for it: two modalities in one moment is several times the
    /// codes, so a default capacity culls the run down to nothing worth reading.
    /// </remarks>
    private const int Capacity = 4000;

    /// <summary>A brain nothing has run yet.</summary>
    /// <param name="capacity">How many commitments it may hold, or the default.</param>
    /// <remarks>
    /// <b>One a world</b>, because a counter that moved on the world before this one would
    /// read as having moved on this one. Every entry below builds its own.
    /// </remarks>
    private static Brain One(int? capacity = null) =>
        new(capacity is { } held
                ? new CommittingSettings { Capacity = held }
                : new CommittingSettings(),
            seed: 1);

    /// <summary>
    /// Counters nought on this world, each with the reason.
    /// </summary>
    /// <remarks>
    /// <b>A reason rather than an excuse</b>, on <see cref="DeadCodeTests"/>'s pattern, and
    /// the list is exact in both directions. A counter that starts moving fails this file
    /// until its entry comes off, because an exemption nobody revisits is how a dead mechanism
    /// keeps its cover.
    /// </remarks>
    private static readonly IReadOnlyDictionary<string, string> Quiet =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Abstained"] =
                "the world could not say what followed, and a generated world always can. What "
                + "moves it is a world that goes quiet about its own outcome, which is the "
                + "spine world's shape rather than any generated one.",

            ["Refused"] =
                "a moment the brain would not take, which is backpressure. A bench offers one "
                + "moment at a time, so nought is the right answer here and moving would mean "
                + "the bench pushed twice on one stamp.",

            ["Unseen.Silence"] =
                "the share of withheld moments nothing fired on, so nought is a population "
                + "covering its whole exam. Moving is a reading rather than a fault.",

            ["AtCovered"] =
                "a repair refused because the failing case is covered already. `AtFloor` and "
                + "`AtBudget` are what refuse on every world here, so whether this ground is "
                + "ever reached is unmeasured.",

            ["AtImproving"] =
                "a repair refused because the child did not improve on its parent, which is "
                + "the gate's other lower rung and reads the same way as `AtCovered`.",

            ["AtIndependent"] =
                "a naming gate refusal, the members firing independently of one another. "
                + "`AtUncertain` and `AtRare` are what refuse here.",

            ["Chosen.Quiet"] =
                "the chooser had nothing to say about a moment, so the world was told so. The "
                + "one acted world here draws a doing every round, which is what an arm that "
                + "always speaks reports.",

            ["Chosen.Again"] =
                "the chooser spoke twice about one moment. `IActed.Listening` is false on "
                + "every world but `Conversing`, so the loop that lifted the one-doing ceiling "
                + "is reachable on the spine world alone and every reading of whether asking "
                + "pays was taken under that ceiling.",

            ["Fronted.Ordered"] =
                "rung three's precedences, derived where the moment is formed. Only a front "
                + "end reading a moment's PARTS reports an order and only the text worlds "
                + "have parts, so `Handing`, `Recalled`, `Conversing` and `Roaming` reach the "
                + "rung and no generated world in this set does.",

            ["Fronted.Doings"] =
                "an intervention code, derived from what the front end said was forced. Three "
                + "worlds mark a doing on `Coded.Assigned` and only two front ends pass it on: "
                + "`Passthrough` does and `Bodied` does not, so the homeostat marks its doing "
                + "and the learner is never told it was one. Turning that on is an arm.",

            ["Fronted.Fleeting"] =
                "a code the front end says names this occasion and cannot recur. `Clevr` and "
                + "`Binding` set `Coded.Passing` and no other world does, so one generated "
                + "world would move it and neither is driven by a bench.",

            ["AtUnpaired"] =
                "a naming gate refusal, no pair to name at all. `Stackable` moves, so eligible "
                + "scopes exist and this is not the ground they fail on.",

            ["Stacked"] =
                "a name standing for a set that holds another name, which is rung five "
                + "stacking on itself. `Named` moves and `Stackable` moves, so the candidates "
                + "are there and nothing has taken one on a generated world.",
        };

    /// <summary>Every world the reading is taken over, and what each reported.</summary>
    /// <remarks>
    /// <para>
    /// <b>Six cheap generated worlds rather than one</b>, because a counter quiet on one
    /// world says nothing. The multiplexer is what step one is judged on, the MONK's puzzle
    /// is where a scope language has a known ceiling, and the arranged world is the one whose
    /// front end has to make its own symbols. What is still quiet across all six is a
    /// mechanism nothing generated can reach.
    /// </para>
    /// <para>
    /// <b>And the three added cover shapes the first three share.</b> The graded
    /// world is read through a winnow, so its codes are a sparse set of winners rather than
    /// one code per attribute; the crossing world is two modalities merged into one moment;
    /// and the homeostat is acted in, so a chooser decides what the next moment is about. All
    /// three are cheap and none is a corpus.
    /// </para>
    /// <para>
    /// <b>What they bought is one counter</b>, <c>AtScarce</c> on the graded world, which is
    /// the naming gate refusing a pair too thinly seen — a refusal a front end minting its own
    /// symbols reaches and one code per attribute does not. Eight are quiet across all six.
    /// </para>
    /// </remarks>
    private static IEnumerable<(string World, Tally Tally)> Ran()
    {
        yield return (
            "multiplexer",
            new MultiplexerRun(
                new MultiplexerSettings { Address = Narrow }, One(), seed: 1, census: true)
                .Run(Rounds).Tally);

        yield return (
            "monk",
            new MonkRun(new MonkSettings(), One(), seed: 1, census: true).Run(Rounds).Tally);

        yield return (
            "arranged",
            new ArrangedRun(new ArrangedSettings(), One(), Looking.Whole, seed: 1)
                .Run(Rounds).Tally);

        yield return (
            "graded",
            new GradedRun(new GradedSettings(), One(), Fronting.Winnowed, seed: 1).Run(Rounds));

        yield return (
            "crossing",
            new CrossingRun(
                new CrossingSettings
                {
                    Words = 16, Facts = 4, Stride = 4, Asked = 64, Scrambled = false,
                },
                One(Capacity),
                seed: 1)
                .Run(Rounds).Learnt);

        yield return (
            "homeostat",
            new HomeostatRun(
                new HomeostatSettings(), One(), Regulating.Driven, Feeling.Acted, seed: 1)
                .Run(Rounds).Tally);
    }

    /// <summary>Every number the brain reported, by name.</summary>
    /// <param name="tally">What the run came to.</param>
    /// <remarks>
    /// <b>Nested reports are read through</b>, so where the clock went is counted the same way
    /// the counts are. A phase that took no time is a phase that did not happen.
    /// </remarks>
    private static IEnumerable<(string Name, double Value)> Counters(object tally)
    {
        foreach (var one in tally.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = one.GetValue(tally);

            if (value is null) continue;

            if (one.PropertyType == typeof(long) || one.PropertyType == typeof(int)
                || one.PropertyType == typeof(double))
            {
                yield return (one.Name, Convert.ToDouble(value, CultureInfo.InvariantCulture));
                continue;
            }

            if (one.PropertyType.IsClass && one.PropertyType.Namespace?.StartsWith(
                    "OpenPlexus", StringComparison.Ordinal) == true)
                foreach (var inner in Counters(value))
                    yield return ($"{one.Name}.{inner.Name}", inner.Value);
        }
    }

    /// <summary>
    /// <b>Every counter the brain reports moves</b>, or an entry says why it does not.
    /// </summary>
    [Fact]
    public void Every_counter_the_brain_reports_either_moves_or_says_why_not()
    {
        var moving = new Dictionary<string, string>(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (world, tally) in Ran())
            foreach (var (name, value) in Counters(tally))
            {
                seen.Add(name);

                if (value != 0.0 && !moving.ContainsKey(name)) moving[name] = world;
            }

        Assert.NotEmpty(seen);

        var still = seen.Except(moving.Keys, StringComparer.Ordinal).ToList();

        foreach (var name in seen.Order(StringComparer.Ordinal))
            output.WriteLine(
                moving.TryGetValue(name, out var world)
                    ? $"  fired  {name,-22} first on {world}"
                    : $"  quiet  {name,-22} on every world here");

        var arrived = still.Except(Quiet.Keys, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToList();

        var moved = Quiet.Keys.Except(still, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToList();

        Assert.True(arrived.Count == 0,
            $"{arrived.Count} counter(s) at nought over {Rounds} rounds: "
            + $"{string.Join(", ", arrived)}. Each is a mechanism the run reported and never "
            + "ran. Wire it, delete it, or put it in `Quiet` with the world it does fire on.");

        Assert.True(moved.Count == 0,
            $"{moved.Count} counter(s) listed as quiet and moving: {string.Join(", ", moved)}. "
            + "Take each entry off `Quiet`, which only means something while it is exact.");
    }
}
