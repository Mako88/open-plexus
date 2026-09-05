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
/// The split is one call. Every commitment firing on an exam round is asked what it expects,
/// and the share of rounds where ANY of them expects the right answer is the ceiling a perfect
/// ranker over this population would reach. Against the vote's own score that separates a
/// population which does not hold the answer from a ranking that fails to find it.
/// </para>
/// <para>
/// It is an upper bound rather than an achievable score, and the difference matters. A
/// ranker choosing among the firing set with hindsight is not a mechanism; what the number
/// says is whether there is anything for a mechanism to reach.
/// </para>
/// </remarks>
public sealed class ReachingTests(ITestOutputHelper output)
{
    /// <summary>
    /// How often the answer is reachable, how often the vote reaches it, and how often
    /// nothing fires at all.
    /// </summary>
    /// <remarks>
    /// The same settings, seeds and chooser every other reading on this world uses, so the
    /// numbers here sit beside the bar without a second run having to be taken.
    /// </remarks>
    [Fact]
    public async Task Whether_the_answer_is_in_the_firing_set_at_all()
    {
        output.WriteLine(
            $"{CeilingTests.Houses} houses a seed over {CeilingTests.Seeds} seeds, "
            + "the machine saying nothing");

        output.WriteLine(
            $"{"seed",-6}{"asked",8}{"reached",9}{"voted",9}{"plural",9}{"strong",9}"
            + $"{"distinct",10}{"firing",9}");

        var total = (Asked: 0, Reached: 0, Voted: 0, Plural: 0, Strong: 0,
            Distinct: 0L, Firing: 0L);

        for (var seed = 1; seed <= CeilingTests.Seeds; seed++)
        {
            var house = new Roaming(CeilingTests.Arming(), seed);
            var brain = new Brain(new CommittingSettings { Capacity = 4_000 }, seed);

            var watching = new Watching<Coded>(
                house, new Joined(Joining.Bagged), acting: Chooses.From(_ => null));

            var rounds = CeilingTests.Houses * 46;
            var loop = new Round(brain, rounds, sweep: 500, target: 0.9, window: 500);

            var seen = (Asked: 0, Reached: 0, Voted: 0, Plural: 0, Strong: 0,
                Distinct: 0L, Firing: 0L);

            for (var round = 0; round < rounds; round++)
            {
                if (watching.Push() is not { } pushed) continue;

                var was = loop.Right;

                // Read BEFORE the step, because what is wanted is the population that
                // decided this round rather than the one repair left behind it. Read-only,
                // and it is the same call the vote makes.
                var alight = house.Sat && pushed.Followed is not null
                    ? brain.Held.Firing(brain.Held.Moment(pushed.Codes), pushed.Grouping)
                    : [];

                await loop.StepAsync(pushed);

                if (!house.Sat) continue;
                if (pushed.Followed is not { } answer) continue;

                seen.Asked++;
                seen.Firing += alight.Length;
                seen.Distinct += alight.Select(one => one.Expects).Distinct().Count();

                if (alight.Any(one => one.Expects == answer)) seen.Reached++;
                if (loop.Right > was) seen.Voted++;

                if (alight.Length == 0) continue;

                // The commonest expectation among the rules that fired, which is a ranker
                // needing no accuracy and no evidence -- a crowd counted rather than weighed.
                var plural = alight
                    .GroupBy(one => one.Expects)
                    .OrderByDescending(one => one.Count())
                    .ThenBy(one => one.Key)
                    .First().Key;

                if (plural == answer) seen.Plural++;

                // And the most accurate rule that fired, which is roughly what the shipped
                // vote takes. Ties by identity, so the pick does not depend on a walk order.
                var strong = alight
                    .OrderByDescending(one => one.Accuracy)
                    .ThenBy(one => one.Identity)
                    .First().Expects;

                if (strong == answer) seen.Strong++;
            }

            output.WriteLine(
                $"{seed,-6}{seen.Asked,8}{seen.Reached / (double)seen.Asked,9:F3}"
                + $"{seen.Voted / (double)seen.Asked,9:F3}"
                + $"{seen.Plural / (double)seen.Asked,9:F3}"
                + $"{seen.Strong / (double)seen.Asked,9:F3}"
                + $"{seen.Distinct / (double)seen.Asked,10:F1}"
                + $"{seen.Firing / (double)seen.Asked,9:F1}");

            total = (
                total.Asked + seen.Asked,
                total.Reached + seen.Reached,
                total.Voted + seen.Voted,
                total.Plural + seen.Plural,
                total.Strong + seen.Strong,
                total.Distinct + seen.Distinct,
                total.Firing + seen.Firing);
        }

        var reached = total.Reached / (double)total.Asked;
        var voted = total.Voted / (double)total.Asked;

        output.WriteLine(
            $"{"all",-6}{total.Asked,8}{reached,9:F3}{voted,9:F3}"
            + $"{total.Plural / (double)total.Asked,9:F3}"
            + $"{total.Strong / (double)total.Asked,9:F3}"
            + $"{total.Distinct / (double)total.Asked,10:F1}"
            + $"{total.Firing / (double)total.Asked,9:F1}");

        output.WriteLine(
            $"a blind rule reads {CeilingTests.Bars().Noun:F3} on the same stream");

        Assert.True(total.Asked > 100);

        // The reached column is COVERAGE and is recorded as worthless rather than read as a
        // ceiling. Hundreds of rules fire and they expect most of the answers the exam can
        // ask for between them, so holding the right one is what a bag of everything does.
        // Asserted in the direction it was found, so a change that made the firing set
        // selective would fail here and the column would become worth reading.
        Assert.True(total.Distinct / (double)total.Asked > 8.0,
            $"the firing set now expects {total.Distinct / (double)total.Asked:F1} different "
            + "answers a round, where it expected most of the exam's alphabet when this was "
            + "written -- so the reached column may now mean something and wants re-reading");

        // The finding. A ranker a machine could actually run, taking the most accurate rule
        // that fired, beats the shipped vote on the same population and the same rounds. So
        // the fault is the RANKING rather than the rules: the population holds enough for a
        // simpler rule over it to do better than what decides today.
        var strongest = total.Strong / (double)total.Asked;

        Assert.True(strongest > voted,
            $"the most accurate firing rule reads {strongest:F3} against the vote's {voted:F3}, "
            + "so the ranking is no longer leaving anything on the table and fork 155's "
            + "answer has changed");

        // And it is a readout rather than a shipped arm, which is the one thing this must not
        // be read as. The vote decides what repair runs on, so a machine deciding this way
        // would grow a different population and this number is not what it would score. What
        // is measured is that the population it GREW holds more than the vote takes from it.
        Assert.True(reached >= strongest);

        // The ceiling is a ceiling, or the reading is arithmetically impossible and the two
        // halves are counting different rounds.
        Assert.True(reached >= voted,
            $"the vote reaches {voted:F3} where the firing set holds the answer {reached:F3} "
            + "of the time, which cannot happen: it would mean the vote answered correctly on "
            + "rounds where nothing firing expected the answer");
    }
}
