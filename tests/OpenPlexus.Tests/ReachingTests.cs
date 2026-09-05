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
    /// <param name="Deep">How many codes its scope names.</param>
    private readonly record struct Advocate(
        Code Expects, double Accuracy, long Seen, long Hits, int Deep);

    /// <summary>One exam round, as it stood when the vote was taken.</summary>
    /// <param name="Advocates">Everything that fired, snapshotted.</param>
    /// <param name="Answer">What actually followed.</param>
    /// <param name="Voted">Whether the machine got it right.</param>
    private readonly record struct Round_(
        IReadOnlyList<Advocate> Advocates, Code Answer, bool Voted);

    /// <summary>
    /// Every exam round of a run, snapshotted at the instant the vote was taken.
    /// </summary>
    /// <remarks>
    /// One run for both readings below, because a shape and a ranking read off two runs are
    /// two populations and the pair would say nothing about each other.
    /// </remarks>
    private static async Task<List<Round_>> Rounds()
    {
        var sat = new List<Round_>();

        for (var seed = 1; seed <= CeilingTests.Seeds; seed++)
        {
            var house = new Roaming(CeilingTests.Arming(), seed);
            var brain = new Brain(new CommittingSettings { Capacity = 4_000 }, seed);

            var watching = new Watching<Coded>(
                house, new Joined(Joining.Bagged), acting: Chooses.From(_ => null));

            var rounds = CeilingTests.Houses * 46;
            var loop = new Round(brain, rounds, sweep: 500, target: 0.9, window: 500);

            for (var round = 0; round < rounds; round++)
            {
                if (watching.Push() is not { } pushed) continue;

                var scoring = house.Sat && pushed.Followed is not null;

                // Copied out rather than held by reference, which is the whole point. The step
                // settles every one of these and mints new ones, so a reference read afterwards
                // is a different population.
                var advocates = scoring
                    ? brain.Held
                        .Firing(brain.Held.Moment(pushed.Codes), pushed.Grouping)
                        .Select(one => new Advocate(
                            one.Expects, one.Accuracy, one.Seen, one.Hits, one.Scope.Length))
                        .ToList()
                    : [];

                var was = loop.Right;

                await loop.StepAsync(pushed);

                if (!scoring || pushed.Followed is not { } answer) continue;

                sat.Add(new Round_(advocates, answer, loop.Right > was));
            }
        }

        return sat;
    }

    /// <summary>
    /// How DEEP the rules that fire are, how good they are, and where the answer lives.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The shape behind the ranking's failure. A short scope is a subset of nearly every
    /// moment, so it fires constantly and can only ever expect something common; a long one
    /// fires rarely and is where a specific answer would have to live. If the firing set is
    /// mostly short scopes then the population covers by construction and no ranking over it
    /// could do better.
    /// </para>
    /// <para>
    /// The right column is the one that decides it: among the rules expecting the ANSWER, how
    /// deep are they and how well tested. A population whose correct advocates are deep and
    /// thin is one whose knowledge is real and unfindable; one with no correct advocate worth
    /// ranking has not learnt the answer at all.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task How_deep_the_rules_that_fire_are_and_where_the_answer_lives()
    {
        var sat = await Rounds();

        output.WriteLine(
            $"{sat.Count} exam rounds over {CeilingTests.Seeds} seeds, snapshotted at the vote");

        output.WriteLine(
            $"{"depth",-8}{"firing",10}{"share",9}{"accuracy",10}{"seen",9}{"right",9}");

        var buckets = sat
            .SelectMany(one => one.Advocates.Select(each => (Round: one, Each: each)))
            .GroupBy(one => Math.Min(one.Each.Deep, 5))
            .OrderBy(one => one.Key)
            .ToList();

        var firing = buckets.Sum(one => one.Count());

        foreach (var bucket in buckets)
        {
            var deep = bucket.Key == 5 ? "5+" : bucket.Key.ToString();

            output.WriteLine(
                $"{deep,-8}{bucket.Count(),10}{bucket.Count() / (double)firing,9:F3}"
                + $"{bucket.Average(one => one.Each.Accuracy),10:F3}"
                + $"{bucket.Average(one => (double)one.Each.Seen),9:F1}"
                + $"{bucket.Count(one => one.Each.Expects == one.Round.Answer)
                    / (double)bucket.Count(),9:F3}");
        }

        // And the same question asked only of the advocates that were RIGHT, which is where a
        // population that knows the answer would keep it.
        var correct = sat
            .SelectMany(one => one.Advocates.Where(each => each.Expects == one.Answer))
            .ToList();

        output.WriteLine(
            $"the {correct.Count} advocates that expected the answer sit at depth "
            + $"{correct.Average(one => (double)one.Deep):F2}, accuracy "
            + $"{correct.Average(one => one.Accuracy):F3}, seen "
            + $"{correct.Average(one => (double)one.Seen):F1}");

        var wrong = sat
            .SelectMany(one => one.Advocates.Where(each => each.Expects != one.Answer))
            .ToList();

        output.WriteLine(
            $"the {wrong.Count} that did not sit at depth "
            + $"{wrong.Average(one => (double)one.Deep):F2}, accuracy "
            + $"{wrong.Average(one => one.Accuracy):F3}, seen "
            + $"{wrong.Average(one => (double)one.Seen):F1}");

        // Whether the population holds a RELIABLE rule at all. The blind bar is a rule this
        // language can say -- a scope naming the question's noun, expecting the commonest
        // answer for it -- so if nothing well-tested is as accurate as that bar, the machine
        // has failed to learn a regularity it could express and no ranking was ever going to
        // save it.
        var tested = sat
            .SelectMany(one => one.Advocates)
            .Where(one => one.Seen >= 50)
            .ToList();

        output.WriteLine(
            $"of {tested.Count} well-tested firings, "
            + $"{tested.Count(one => one.Accuracy >= 0.41) / (double)Math.Max(tested.Count, 1):F3} "
            + $"are as accurate as the blind bar, best {tested.Max(one => one.Accuracy):F3}");

        // And a ranker that will only listen to a well-tested advocate, which is the last
        // obvious one left after a maximum, a mean, a plurality, a bound and the most tested.
        var settled = sat.Count(one =>
            one.Advocates.Any(each => each.Seen >= 50)
            && one.Advocates
                .Where(each => each.Seen >= 50)
                .GroupBy(each => each.Expects)
                .Select(each => (Expects: each.Key, Weight: each.Max(a => a.Accuracy)))
                .OrderByDescending(each => each.Weight)
                .ThenBy(each => each.Expects)
                .First().Expects == one.Answer) / (double)sat.Count;

        output.WriteLine($"ranking only well-tested advocates reads {settled:F3}");

        Assert.True(sat.Count > 100);

        // The two sets have to differ somewhere, or nothing about an advocate says whether it
        // is about to be right and no ranking over them could ever work. That is the finding
        // this reading exists to make falsifiable.
        Assert.True(
            Math.Abs(correct.Average(one => one.Accuracy) - wrong.Average(one => one.Accuracy))
                > 0.01
            || Math.Abs(correct.Average(one => (double)one.Deep)
                - wrong.Average(one => (double)one.Deep)) > 0.05,
            "the advocates that expect the answer and the ones that do not are alike in depth "
            + "and in accuracy, so nothing a ranking can see separates them and the vote is "
            + "choosing at chance among things it cannot tell apart");
    }

    /// <summary>
    /// What the population holds at the vote, and what any honest ranking of it could reach.
    /// </summary>
    [Fact]
    public async Task Whether_a_ranking_of_the_firing_set_can_clear_the_blind_bar()
    {
        var sat = await Rounds();

        output.WriteLine(
            $"{sat.Count} exam rounds over {CeilingTests.Seeds} seeds, every column read "
            + "off the snapshot taken when the vote was");

        output.WriteLine(
            $"{"ranker",-12}{"score",9}");

        (string Name, Func<IReadOnlyList<Advocate>, Code> Pick)[] rankers =
        [
            // What the vote takes: an expectation is worth its best advocate's accuracy.
            ("strong", one => Best(one, each => each.Accuracy)),

            // A crowd counted rather than weighed, needing no evidence at all.
            ("plural", one => one
                .GroupBy(each => each.Expects)
                .OrderByDescending(each => each.Count())
                .ThenBy(each => each.Key)
                .First().Key),

            // The most TESTED advocate, ignoring whether it is any good.
            ("tested", one => Best(one, each => (double)each.Seen)),

            // Accuracy with the evidence behind it priced in.
            ("bounded", one => Best(one, Bounded)),

            // Best ON AVERAGE rather than at its best. A maximum is an extreme value, so an
            // expectation with more advocates wins it more often whatever they are worth; a
            // mean is not a sum and does not scale with the crowd either.
            ("meaned", one => one
                .GroupBy(each => each.Expects)
                .Select(each => (Expects: each.Key, Weight: each.Average(a => a.Accuracy)))
                .OrderByDescending(each => each.Weight)
                .ThenBy(each => each.Expects)
                .First().Expects),
        ];

        var voted = sat.Count(one => one.Voted) / (double)sat.Count;

        output.WriteLine($"{"the machine",-12}{voted,9:F3}");

        var scored = new Dictionary<string, double>(StringComparer.Ordinal);

        foreach (var (name, pick) in rankers)
        {
            var hits = sat.Count(one => one.Advocates.Count > 0 && pick(one.Advocates) == one.Answer);

            scored[name] = hits / (double)sat.Count;

            output.WriteLine($"{name,-12}{scored[name],9:F3}");
        }

        var blind = CeilingTests.Bars().Noun;

        output.WriteLine($"{"blind",-12}{blind,9:F3}");

        output.WriteLine(
            $"{"reached",-12}{sat.Count(one => one.Advocates.Any(each => each.Expects == one.Answer))
                / (double)sat.Count,9:F3} over "
            + $"{sat.Average(one => one.Advocates.Select(each => each.Expects).Distinct().Count()):F1} "
            + "distinct expectations a round, which is why it is coverage");

        Assert.True(sat.Count > 100);

        // The snapshot reproduces the machine, which is what says every other row here is a
        // thing that machine could have done instead. A `strong` away from the vote would mean
        // the readout is reading a population the vote never saw, which is the fault this file
        // was rewritten to remove.
        Assert.True(Math.Abs(scored["strong"] - voted) < 0.02,
            $"the vote's own rule replayed on the snapshot reads {scored["strong"]:F3} where "
            + $"the machine scored {voted:F3}. Those are the same rule on the same rounds, so "
            + "the snapshot is not the population that voted and no row here is a mechanism");

        // And none of them clears the blind bar, which is the finding. It is asserted in the
        // direction it was found: a ranker that started clearing it would fail here and fork
        // 155's answer would move back to the ranking.
        Assert.All(scored, one => Assert.True(one.Value < blind,
            $"`{one.Key}` reads {one.Value:F3} against a blind {blind:F3}, so a ranking of the "
            + "firing set now clears the bar and the loss is no longer the rules"));
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
