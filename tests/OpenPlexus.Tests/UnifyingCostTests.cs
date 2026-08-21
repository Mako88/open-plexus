using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What a scope naming no argument costs to match — <b>fork 33</b>, and the gate the plan
/// put in front of rung four rather than behind it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The worth is already priced and the cost is not.</b> <c>Joining.Chained</c> at one
/// hop did the selection in the front end and answered bAbI's first task outright where the bag
/// sits at its marginal, so what a unifying matcher would BUY is a number this repo
/// already has. What it would cost has been an argument — <i>it breaks the indexing that
/// makes matching cheap</i> — and an argument is what this file replaces.
/// </para>
/// <para>
/// <b>And the first finding is about the front end</b> rather than the matcher.
/// <c>Joined</c> unions the question's words into the story's bag under one modality, so
/// <i>the word asked about</i> and <i>the word told</i> are the same code and a repeated
/// variable binds against nothing at all. A variable needs two places to stand in; on
/// this corpus the moment has one. So the tagging is done HERE, as an instrument, and
/// what is measured is what the rung would cost given a front end that keeps the halves
/// apart — said out loud because a cost taken on a moment where nothing could ever unify
/// is a cost of zero and means nothing.
/// </para>
/// <para>
/// <b>And the argument is refuted by the second reading</b> rather than by the first. The
/// per-match cost is small — a variable costs its candidate set, which is the moment
/// restricted to one modality, and a refusal costs all of it where a binding stops at the
/// first that fits. What was supposed to be expensive is the INDEX, and the index turns
/// out to be nearly a no-op already: matching visits four residents in five a round on
/// text and nearly nine in ten on the multiplexer, because scopes are short and a moment
/// holds much of the alphabet. A scope naming only variables joins a scan list whose cost
/// is the remainder.
/// </para>
/// <para>
/// <b>No bar on the costs themselves.</b> What a deployment can afford is not decided
/// here, and the point of pricing a rung before designing its escalation policy is that
/// the policy reads the number rather than the number being chosen to fit a policy. The
/// one bar is on the finding above, so that a world where the index DOES earn its keep
/// fails this file instead of quietly inheriting its conclusion.
/// </para>
/// </remarks>
public sealed class UnifyingCostTests(ITestOutputHelper output)
{
    /// <summary>How many questions the per-match costs are averaged over.</summary>
    private const int Asked = 400;

    /// <summary>How many rounds the population priced for indexability is learnt over.</summary>
    private const long Rounds = 4000;

    /// <summary>
    /// The modality the question's words are repeated under — <b>the instrument</b>, and the
    /// one thing here that is not in the library.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>And the question's words are not also put in the story's bag.</b> The first
    /// version of this file did and it cost the reading. <c>Joined</c> unions them, so
    /// every asked word is a told word by construction — and under that moment <i>the word
    /// asked about was told</i> is true of every question ever asked. It fired 400 times
    /// out of 400 at one candidate tried, which reads as unification being free and is
    /// only the union being tautological.
    /// </para>
    /// <para>
    /// <b>So a variable needs two places that can disagree</b>, and keeping the halves
    /// apart is the whole of what the front end has to supply. The world has kept them
    /// apart since it was written; nothing has to be inferred to emit this.
    /// </para>
    /// </remarks>
    private const byte Wondered = 44;

    /// <summary>
    /// The modality the newest statement's words are repeated under — <b>the second place a
    /// variable can stand</b>, and the one that can DISAGREE.
    /// </summary>
    /// <remarks>
    /// <b>Because on this task the question always names somebody the story mentions.</b>
    /// <i>The word asked about was told</i> is therefore true of every question ever asked
    /// here — 400 of 400, measured — so it prices a binding and can never price a refusal,
    /// and half the cost of this rung is what a refusal costs. <i>The word asked about is
    /// in the LATEST statement</i> is the same shape and is contingent, which is what makes
    /// the refusal column real. <c>Joined.Bands</c> already emits this distinction for the
    /// arm that wanted it; it is repeated here rather than borrowed so the probe carries
    /// its own instrument.
    /// </remarks>
    private const byte Latest = 45;

    /// <summary>The story's words, plus the question's and the newest statement's, tagged.</summary>
    /// <param name="asking">The question and the story in front of it.</param>
    private static HashSet<Code> Tagged(Asking asking)
    {
        var moment = new HashSet<Code>(asking.Words);

        foreach (var word in asking.Question) moment.Add(new Code(Wondered, word.Value));

        if (asking.Story.Count > 0)
            foreach (var word in asking.Story[0]) moment.Add(new Code(Latest, word.Value));

        return moment;
    }

    [Fact]
    public void What_a_scope_naming_no_argument_costs_to_match()
    {
        var world = new Recalled(new RecalledSettings
        {
            Corpus = Tree.Babi(), Task = 1, Span = 0, Withheld = 40,
            Predicting = Predicting.Asked,
        });

        // The modality words arrive on, read off the signal rather than named. `Babi` keeps
        // it private and a test is not a reason to publish it — a probe that had to widen
        // the library to take a reading would be changing what it measures.
        var told = world.Withheld[0].Seen.Bagged.Words.First().Modality;

        // The four shapes, and the constant one is the control rather than a fifth arm. A
        // subset test tries exactly one membership per scope code, so a two-code constant
        // scope costs two — and every number below is against that.
        //
        // A join is the shape that matters. One variable standing in two places is *the
        // word asked about was told*, which is `Joining.Anonymous` with the identity kept
        // rather than thrown away, and it is the cheapest thing rung four can say that
        // rungs one to three cannot say at all.
        var join = ImmutableArray.Create(
            Unifying.Any(Wondered, 0), Unifying.Any(told, 0));

        // The same join written the other way round, and it is an arm rather than a
        // duplicate. `Fill` draws its candidates from the FIRST entry naming a variable, so
        // asking from the question's side enumerates the three words asked about and asking
        // from the story's side enumerates every word in the story. Same scope, same
        // answers, and the distance between the two columns is exactly the saving a matcher
        // gets for free by starting at the most constrained side.
        var wide = ImmutableArray.Create(
            Unifying.Any(told, 0), Unifying.Any(Wondered, 0));

        // And the one that can say no. Everything above binds on every question this world
        // asks, so between them they price a hit and never a refusal — and a population is
        // mostly refusals. This is the same join against the newest statement instead of
        // the whole story, which is contingent.
        var latest = ImmutableArray.Create(
            Unifying.Any(Wondered, 0), Unifying.Any(Latest, 0));

        var two = ImmutableArray.Create(
            Unifying.Any(Wondered, 0), Unifying.Any(told, 0),
            Unifying.Any(Wondered, 1), Unifying.Any(told, 1));

        var moments = 0;
        var words = 0;
        var asked = 0;

        var shapes = new[] { ("join", join), ("wide", wide), ("latest", latest), ("two vars", two) };

        var bound = new int[shapes.Length];
        var boundTried = new int[shapes.Length];
        var refusedTried = new int[shapes.Length];

        for (var ask = 0; ask < Asked; ask++)
        {
            var turn = world.Next();
            var moment = Tagged(turn.Seen.Bagged);

            moments++;
            words += moment.Count;
            asked += turn.Seen.Asked.Count;

            var index = Unifying.Index(moment);

            for (var shape = 0; shape < shapes.Length; shape++)
            {
                var read = Unifying.Fires(shapes[shape].Item2, moment, index);

                if (read.Fired)
                {
                    bound[shape]++;
                    boundTried[shape] += read.Tried;
                }
                else
                {
                    refusedTried[shape] += read.Tried;
                }
            }
        }

        output.WriteLine(
            $"{moments} moments | {words / (double)moments:F1} codes a moment, "
            + $"{asked / (double)moments:F1} of them asked about");

        // Bound and refused are reported apart because they are not the same cost and the
        // population is mostly the second. A binding is found and the search stops; a
        // refusal is the whole candidate set enumerated and every partner checked, and
        // almost every resident refuses almost every round. An average over both would be
        // an average weighted by this file's choice of scopes rather than by a population.
        for (var shape = 0; shape < shapes.Length; shape++)
        {
            var refused = moments - bound[shape];

            output.WriteLine(
                $"{shapes[shape].Item1,-9}| bound {bound[shape],4}/{moments} at "
                + $"{(bound[shape] == 0 ? 0.0 : boundTried[shape] / (double)bound[shape]):F1} tried "
                + $"| refused {refused,4} at "
                + $"{(refused == 0 ? 0.0 : refusedTried[shape] / (double)refused):F1} tried");
        }

        output.WriteLine("subset   | 1 tried a code, bound or refused, by construction");

        // The instrument check, and it is the one that caught two readings. A join that can
        // never fail costs nothing to decide and a join that can never fire costs nothing
        // to refuse; either way the number would be about the moment rather than about
        // unification. The contingent shape is the one both columns are demanded of, and
        // `join` is deliberately not — that it binds every time is a finding about the
        // corpus, printed above rather than asserted away.
        Assert.True(bound[0] > 0, "no join ever bound, so the bound column is empty");

        var contingent = Array.FindIndex(shapes, one => one.Item1 == "latest");

        Assert.True(bound[contingent] > 0 && bound[contingent] < moments,
            $"the contingent shape bound {bound[contingent]} of {moments}, so one of the two "
            + "columns is empty and the cost of the other is all this file measured");
    }

    [Fact]
    public void How_much_of_a_learnt_population_would_keep_its_index()
    {
        var brain = new Brain(new CommittingSettings { Capacity = 2000 }, seed: 1);
        var world = new Recalled(new RecalledSettings
        {
            Corpus = Tree.Babi(), Task = 1, Span = 0, Withheld = 40,
            Predicting = Predicting.Asked,
        });

        new Bench(new Watching<Recited>(world, new Joined(Joining.Bagged)), brain)
            .Run(Rounds, sweep: 1000, target: 0.9, window: 2000);

        var all = brain.Held.All.ToList();

        Assert.True(all.Count > 0, "nothing was learnt, so there is no population to price");

        // What variabilising one code would cost the index. `Population.Firing` reaches a
        // commitment through any real code in its scope, so a scope keeps its index while
        // it keeps one — and a scope of length one that gives up its only code is reached
        // by nothing and has to be scanned every round.
        var singles = all.Count(one => one.Scope.Length == 1);

        var lengths = all.Sum(one => one.Scope.Length) / (double)all.Count;

        output.WriteLine($"{all.Count} residents | {lengths:F2} codes a scope");
        output.WriteLine(
            $"{singles} of them are one code ({singles / (double)all.Count:P1}), so "
            + "variabilising it leaves nothing to index by");
        output.WriteLine(
            $"{all.Count - singles} would keep at least one constant and stay indexable");

        // And what the index is worth today, which is the denominator the share above is
        // meaningless without. `Population.Firing` reaches a commitment only through a code
        // its scope holds, so what it visits a round is the residents sharing a code with
        // the moment — and a scope naming only variables shares none, so it would be
        // visited EVERY round on top of that. The cost of the rung is therefore the scan
        // list against this number and never against the population.
        var visited = 0;
        var rounds = 0;

        foreach (var turn in world.Withheld)
        {
            var moment = new HashSet<Code>(turn.Seen.Bagged.Words);

            moment.UnionWith(turn.Seen.Asked);

            rounds++;
            visited += all.Count(one => one.Scope.Any(moment.Contains));
        }

        output.WriteLine(
            $"matching visits {visited / (double)rounds:F1} of {all.Count} residents a round, "
            + $"so an all-variable scope costs a visit no index can save");

        // And the same reading on a world where the index should win, because a share taken
        // on one world is a fact about that world. The multiplexer's moments are narrow and
        // its alphabet is tiny, which is the arrangement an index is FOR — so if the share
        // is low there and high on text, what the index buys is a property of the signal
        // rather than of the design, and rung four's price differs by world.
        var narrow = new Brain(new CommittingSettings { Capacity = 2000 }, seed: 1);

        new MultiplexerRun(new MultiplexerSettings { Address = 3 }, narrow, seed: 1).Run(Rounds);

        var bits = narrow.Held.All.ToList();
        var sensing = new Bits(Multiplexer.Bit);

        IWorld<IReadOnlyList<int>> multiplexer =
            new Multiplexer(new MultiplexerSettings { Address = 3 }, seed: 99);

        var narrowVisited = 0;
        var narrowWords = 0;

        for (var ask = 0; ask < Asked; ask++)
        {
            var moment = new HashSet<Code>(sensing.Codify(multiplexer.Next().Seen));

            narrowWords += moment.Count;
            narrowVisited += bits.Count(one => one.Scope.Any(moment.Contains));
        }

        output.WriteLine(
            $"multiplexer | {bits.Count} residents, {narrowWords / (double)Asked:F1} codes a "
            + $"moment | matching visits {narrowVisited / (double)Asked:F1} "
            + $"({narrowVisited / (double)Asked / bits.Count:P1})");

        // The one bar in this file, and it holds the finding rather than a level. What
        // gated rung four for the whole branch is the sentence *unification breaks the
        // indexing that makes matching cheap*, and both readings say the indexing is
        // already not making matching cheap: four residents in five are visited a round on
        // text and nearly nine in ten on the multiplexer, because scopes are short and a
        // moment holds much of the alphabet. So what a scan list adds is the remainder, and
        // the remainder is small.
        //
        // If this goes red the conclusion is owed a re-take rather than a repair. A
        // population of long scopes over a wide alphabet would make the index earn its
        // keep, and rung four's price would be a different number on that world.
        Assert.True(visited / (double)rounds > all.Count / 2.0,
            $"matching visits {visited / (double)rounds:F1} of {all.Count} residents a round "
            + "on text, so the index IS saving most of the population and the scan list a "
            + "variable scope would join is no longer a small addition");

        Assert.True(narrowVisited / (double)Asked > bits.Count / 2.0,
            $"matching visits {narrowVisited / (double)Asked:F1} of {bits.Count} residents a "
            + "round on the multiplexer, so the same re-take is owed there");

        // NO BAR. The share is the finding, and what an escalation policy should do about a
        // scan list is exactly what fork 33 was asked before anybody designed one.
    }
}
