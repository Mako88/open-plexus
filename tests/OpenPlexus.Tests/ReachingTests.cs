using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Which half loses to the commonest answer: the rules, or the ranking over them.
/// </summary>
/// <remarks>
/// <para>
/// Fork 155. The learner reads under a rule that never looked at the house, on every kind of
/// question, so the rules it ranks are worth less when they fire than ignoring them would be.
/// That is one of two faults and the two want different repairs, so nothing should be built
/// until it is known which.
/// </para>
/// <para>
/// Every ranker here is scored on a SNAPSHOT taken before the round is stepped, holding each
/// firing commitment's expectation and what was known about it at the instant the vote was
/// taken. That is the whole discipline of this file. A commitment is a mutable object and an
/// immutable array of them is immutable in its membership alone, so reading an accuracy after
/// the step reads a population that has been settled and has had genesis mint a rule on the
/// round it was just shown the answer to.
/// </para>
/// <para>
/// The first shape of this did exactly that and read a ranker at 0.500 against the vote's
/// 0.326, which is hindsight wearing a mechanism's clothes. The snapshot is what makes a
/// column here a thing a machine could actually run.
/// </para>
/// <para>
/// The reached column is the other half of the discipline. Hundreds of rules fire on an exam
/// round and the exam has a couple of dozen answers, so a firing set expecting nearly all of
/// them holds the right one always and carries no information. It is coverage rather than a
/// ceiling, and the distinct count beside it is what says so.
/// </para>
/// </remarks>
public sealed class ReachingTests(ITestOutputHelper output)
{
    /// <summary>What was known about one advocate at the instant the vote was taken.</summary>
    /// <param name="Expects">What it says will follow.</param>
    /// <param name="Accuracy">Its merged accuracy.</param>
    /// <param name="Seen">How often it has fired.</param>
    /// <param name="Hits">How often it was right.</param>
    private readonly record struct Advocate(Code Expects, double Accuracy, long Seen, long Hits);

    /// <summary>
    /// What the population holds at the vote, and what any honest ranking of it could reach.
    /// </summary>
    [Fact]
    public async Task Whether_a_ranking_of_the_firing_set_can_clear_the_blind_bar()
    {
        output.WriteLine(
            $"{CeilingTests.Houses} houses a seed over {CeilingTests.Seeds} seeds, "
            + "the machine saying nothing, every column read BEFORE the step");

        output.WriteLine(
            $"{"seed",-6}{"asked",8}{"voted",9}{"strong",9}{"plural",9}{"tested",9}"
            + $"{"bounded",9}{"reached",9}{"distinct",10}");

        var total = (Asked: 0, Voted: 0, Strong: 0, Plural: 0, Tested: 0, Bounded: 0,
            Reached: 0, Distinct: 0L);

        for (var seed = 1; seed <= CeilingTests.Seeds; seed++)
        {
            var house = new Roaming(CeilingTests.Arming(), seed);
            var brain = new Brain(new CommittingSettings { Capacity = 4_000 }, seed);

            var watching = new Watching<Coded>(
                house, new Joined(Joining.Bagged), acting: Chooses.From(_ => null));

            var rounds = CeilingTests.Houses * 46;
            var loop = new Round(brain, rounds, sweep: 500, target: 0.9, window: 500);

            var seen = (Asked: 0, Voted: 0, Strong: 0, Plural: 0, Tested: 0, Bounded: 0,
                Reached: 0, Distinct: 0L);

            for (var round = 0; round < rounds; round++)
            {
                if (watching.Push() is not { } pushed) continue;

                var scoring = house.Sat && pushed.Followed is not null;

                // Copied out rather than held by reference, which is the whole point. The
                // step settles every one of these and mints new ones, so a reference read
                // afterwards is a different population.
                var advocates = scoring
                    ? brain.Held
                        .Firing(brain.Held.Moment(pushed.Codes), pushed.Grouping)
                        .Select(one => new Advocate(
                            one.Expects, one.Accuracy, one.Seen, one.Hits))
                        .ToList()
                    : [];

                var was = loop.Right;

                await loop.StepAsync(pushed);

                if (!scoring) continue;
                if (pushed.Followed is not { } answer) continue;

                seen.Asked++;
                seen.Distinct += advocates.Select(one => one.Expects).Distinct().Count();

                if (loop.Right > was) seen.Voted++;
                if (advocates.Any(one => one.Expects == answer)) seen.Reached++;

                if (advocates.Count == 0) continue;

                // What the vote takes: an expectation is worth its best advocate's accuracy,
                // the highest wins, ties by code.
                if (Best(advocates, one => one.Accuracy) == answer) seen.Strong++;

                // A crowd counted rather than weighed, which needs no evidence at all.
                if (advocates
                        .GroupBy(one => one.Expects)
                        .OrderByDescending(one => one.Count())
                        .ThenBy(one => one.Key)
                        .First().Key == answer)
                    seen.Plural++;

                // The most TESTED advocate, ignoring whether it is any good. A rule that has
                // fired a thousand times is a different bet from one that has fired twice.
                if (Best(advocates, one => one.Seen) == answer) seen.Tested++;

                // Accuracy discounted by how little is behind it -- the lower end of a Wilson
                // interval at roughly two standard errors. A rule right once out of once
                // reads near a half where one right ninety-nine times of a hundred reads near
                // ninety-five, so evidence enters the ranking without a second dial.
                if (Best(advocates, Bounded) == answer) seen.Bounded++;
            }

            output.WriteLine(
                $"{seed,-6}{seen.Asked,8}{seen.Voted / (double)seen.Asked,9:F3}"
                + $"{seen.Strong / (double)seen.Asked,9:F3}"
                + $"{seen.Plural / (double)seen.Asked,9:F3}"
                + $"{seen.Tested / (double)seen.Asked,9:F3}"
                + $"{seen.Bounded / (double)seen.Asked,9:F3}"
                + $"{seen.Reached / (double)seen.Asked,9:F3}"
                + $"{seen.Distinct / (double)seen.Asked,10:F1}");

            total = (
                total.Asked + seen.Asked, total.Voted + seen.Voted,
                total.Strong + seen.Strong, total.Plural + seen.Plural,
                total.Tested + seen.Tested, total.Bounded + seen.Bounded,
                total.Reached + seen.Reached, total.Distinct + seen.Distinct);
        }

        var voted = total.Voted / (double)total.Asked;
        var strong = total.Strong / (double)total.Asked;
        var bounded = total.Bounded / (double)total.Asked;

        output.WriteLine(
            $"{"all",-6}{total.Asked,8}{voted,9:F3}{strong,9:F3}"
            + $"{total.Plural / (double)total.Asked,9:F3}"
            + $"{total.Tested / (double)total.Asked,9:F3}"
            + $"{bounded,9:F3}{total.Reached / (double)total.Asked,9:F3}"
            + $"{total.Distinct / (double)total.Asked,10:F1}");

        output.WriteLine(
            $"a blind rule reads {CeilingTests.Bars().Noun:F3} on the same stream");

        Assert.True(total.Asked > 100);

        // The snapshot reproduces the machine, which is what says every other column here is
        // a thing that machine could have done instead. A `strong` above the vote would mean
        // the readout is reading a population the vote never saw -- which is exactly the fault
        // this file was rewritten to remove.
        Assert.True(Math.Abs(strong - voted) < 0.02,
            $"the vote's own rule replayed on the snapshot reads {strong:F3} where the machine "
            + $"scored {voted:F3}. Those are the same rule on the same rounds, so the snapshot "
            + "is not the population that voted and no column here is a mechanism");

        // And the reached column is coverage rather than a ceiling, asserted in the direction
        // it was found so a firing set that became selective would fail here and the column
        // would start meaning something.
        Assert.True(total.Distinct / (double)total.Asked > 8.0,
            $"the firing set now expects {total.Distinct / (double)total.Asked:F1} different "
            + "answers a round, where it expected most of the exam's alphabet when this was "
            + "written -- so the reached column may now mean something and wants re-reading");
    }

    /// <summary>The expectation of the advocate that leads on one reading.</summary>
    /// <param name="advocates">What fired.</param>
    /// <param name="by">What to lead on.</param>
    /// <remarks>
    /// Ties break by the expectation's own code, which is what <c>Population.Decide</c> does,
    /// so a tie comes out the same on every machine rather than however a list was walked.
    /// </remarks>
    private static Code Best(
        IReadOnlyList<Advocate> advocates, Func<Advocate, double> by) =>
        advocates
            .GroupBy(one => one.Expects)
            .Select(one => (Expects: one.Key, Weight: one.Max(by)))
            .OrderByDescending(one => one.Weight)
            .ThenBy(one => one.Expects)
            .First().Expects;

    /// <summary>
    /// Accuracy with the evidence behind it priced in, as a Wilson lower bound.
    /// </summary>
    /// <param name="one">The advocate.</param>
    /// <remarks>
    /// The arm the refutation table's own row points at. A lifetime average is refused as the
    /// deciding statistic because it cannot track; what is being asked here is the other
    /// complaint, that it says the same thing after one firing as after a thousand.
    /// </remarks>
    private static double Bounded(Advocate one)
    {
        if (one.Seen == 0) return 0.0;

        const double Z = 2.0;

        var n = (double)one.Seen;
        var p = one.Hits / n;
        var z = (Z * Z) / n;

        return ((p + (z / 2.0)) - (Z * Math.Sqrt(((p * (1.0 - p)) + (z / 4.0)) / n)))
            / (1.0 + z);
    }
}
