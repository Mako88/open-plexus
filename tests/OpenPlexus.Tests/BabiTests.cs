using OpenPlexus.Codes;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The first world in this project that somebody else designed.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's ask, 2026-08-03: stop comparing only against our own numbers.</b>
/// Four worlds were built here by the same hands that built the mechanisms they
/// measure, so a good result on one of them can only say the mechanism does what
/// its author expected. The bAbI tasks were written by other people to isolate
/// capabilities nobody here chose, and they come with published baselines.
/// </para>
/// <para>
/// <b>The corpus is fetched, not vendored.</b> It is CC BY 3.0 and eleven
/// megabytes; <c>corpora/fetch.sh</c> gets it and <see cref="Corpus"/> says so
/// when it is not there.
/// </para>
/// </remarks>
public sealed class BabiTests(ITestOutputHelper output)
{
    /// <summary>
    /// Where the task files are, or a failure that says how to get them.
    /// </summary>
    /// <remarks>
    /// <b>Fails rather than skipping, which is this suite's standing rule.</b> A
    /// green run that quietly never asked the question is the failure every check
    /// here exists to avoid — see <see cref="Tree"/>.
    /// </remarks>
    private static string Corpus => Tree.Babi();

    private static BabiSettings World(int task, bool stories = true) => new()
    {
        Task = task, Corpus = Corpus, Stories = stories,
    };

    /// <summary>
    /// <b>Deep, because the tasks that suit this architecture are chains.</b>
    /// <i>Basic induction</i> reaches an answer through two intermediates, so a
    /// budget that only affords one hop would measure the corpus rather than the
    /// walk.
    /// </summary>
    // ---- what the corpus is, asserted rather than described -----------------

    [Fact]
    public void A_statement_is_its_words_and_a_question_carries_its_answer()
    {
        var read = Babi.Read(
        [
            "1 Mary moved to the bathroom.",
            "2 John went to the hallway.",
            "3 Where is Mary? \tbathroom\t1",
        ], stories: false);

        Assert.Equal(3, read.Count);

        Assert.Null(read[0].Answer);
        Assert.False(read[0].Asking);
        // Compared as sequences and not as ImmutableArray, which compares the
        // underlying array by reference and passes for nothing.
        Assert.Equal(
            new[] { "mary", "moved", "to", "the", "bathroom" }.Select(Babi.Of),
            read[0].Words.AsEnumerable());

        Assert.True(read[2].Asking);
        Assert.Equal("bathroom", read[2].Answer);
        Assert.Equal([Babi.Of("bathroom")], read[2].Answers.AsEnumerable());

        // The supporting fact IDS are dropped, and that is the whole ethic of
        // this world. They are the strong supervision the corpus authors ask
        // people to do without, and a route told which sentence to look at is
        // not doing the task.
        Assert.DoesNotContain(Babi.Of("1"), read[2].Words);
    }

    [Fact]
    public void A_story_begins_wherever_the_ids_reset()
    {
        var read = Babi.Read(
        [
            "1 Lily is a frog.",
            "2 Lily is green.",
            "1 Lily is a rhino.",
            "2 Lily is grey.",
        ], stories: true);

        Assert.Equal([0, 0, 1, 1], read.Select(line => line.Story));

        // And the story code is in the sentence, which is what makes it an
        // observed thing rather than an episode boundary. C4 forbids the second.
        Assert.Contains(Babi.Telling(0), read[0].Words);
        Assert.Contains(Babi.Telling(1), read[2].Words);
        Assert.NotEqual(Babi.Telling(0), Babi.Telling(1));
    }

    [Fact]
    public void Off_by_default_nothing_names_the_story()
    {
        var read = Babi.Read(["1 Lily is a frog."], stories: false);

        Assert.DoesNotContain(read[0].Words, code => code.Modality == Babi.Story);
    }

    [Fact]
    public void A_word_gets_the_same_code_in_every_process()
    {
        // string.GetHashCode IS RANDOMISED PER PROCESS, so a run built on it
        // would not reproduce itself -- which is the property fork 12 protects.
        // The literal is the answer, so a change to the hash fails here rather
        // than silently renumbering every code the world has ever emitted.
        Assert.Equal(Babi.Of("mary"), Babi.Of("Mary"));
        Assert.NotEqual(Babi.Of("mary"), Babi.Of("john"));
        Assert.Equal(2250482198492670294UL, Babi.Of("mary").Value);
    }

    [Fact]
    public void A_compound_answer_parses_as_more_than_one_word()
    {
        var read = Babi.Read(["1 What is Mary carrying? \tmilk,football\t2 3"], stories: false);

        Assert.Equal(2, read[0].Answers.Length);
        Assert.Equal([Babi.Of("milk"), Babi.Of("football")], read[0].Answers.AsEnumerable());
    }

    // ---- what the corpus says about itself ---------------------------------

    [Fact]
    public void Every_one_of_the_twenty_tasks_is_there_and_asks_something()
    {
        foreach (var task in Enumerable.Range(1, 20))
        {
            var world = new Babi(World(task));

            Assert.NotEmpty(world.Lines);
            Assert.Contains(world.Lines, line => line.Asking);
            Assert.NotEmpty(world.Alphabet);

            // The majority-class baseline is the one that matters, and it is
            // well above uniform on every task -- which is why a score is
            // reported against it and not against 1/alphabet.
            Assert.True(world.Commonest >= world.Chance,
                $"task {task}: commonest {world.Commonest} below chance {world.Chance}");

            output.WriteLine(
                $"task {task,2}: lines={world.Lines.Count,5} " +
                $"asked={world.Lines.Count(line => line.Asking),4} " +
                $"alphabet={world.Alphabet.Count,3} compound={world.Compound,4} " +
                $"commonest={world.Commonest:F4} chance={world.Chance:F4}");
        }
    }

    /// <summary>
    /// The tasks whose answer is a word the world never shows.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Found by running all twenty and asking why six were exactly nought.</b>
    /// Not nearly nought — <i>exactly</i>, on every question, which is the shape of
    /// a thing that cannot happen rather than a thing done badly.
    /// </para>
    /// <para>
    /// <b>19 is here because the check put it here.</b> The list was written from
    /// the five tasks whose score was nought with nothing compound about them, and
    /// 19 was set aside as the compound case. It reaches none of its twelve answers
    /// either — a path like <c>n,w</c> is neither a word nor one answer — so it
    /// fails BOTH ways, and the honest count of what this design cannot express on
    /// this benchmark is six rather than five.
    /// </para>
    /// </remarks>
    private static readonly int[] Unspeakable = [6, 7, 10, 17, 18, 19];

    // ---- what the graph does with it ---------------------------------------

    /// <summary>How much of a task file a measurement reads.</summary>
    /// <remarks>
    /// <b>Enough to put a few hundred questions behind every arm.</b> A first pass
    /// at 300 sentences gave task 16 thirty questions, where a five-question
    /// difference is two standard errors and reads exactly like a mechanism —
    /// which is the trap this project has already fallen into once.
    /// </remarks>
    private const int Sentences = 800;

    private const int Repeats = 5;

    // ---- THE WINDOW ON bAbI, and why both its tests are gone ---------------
    //
    // two tests stood here. `The_window_costs_an_order_of_magnitude_and_scores_worse`
    // compared a span of nought against two, and
    // `Edge_kinds_are_the_windows_revival_condition_and_this_is_where_it_is_run`
    // compared the fused cell against the split one at three budgets. Both arms
    // are gone: `Span` has a floor of one since 2026-08-05 and the kinds are
    // unconditional, so neither comparison can be built.
    //
    // What they established, and it is a cost this world is now paying knowingly:
    //
    //   * the window is a loss here. It exists to give the graph temporal edges
    //     and measured null on snake, where what matters is what is visible now.
    //     A corpus of sentences in the order somebody wrote them is the opposite
    //     of that, and it was WORSE here rather than null — for something over
    //     five times the traffic.
    //
    //   * Edge kinds recover much of it and do not pay for it. Splitting the cell
    //     ranked better AND cost fewer messages at every budget swept, which is
    //     the opposite of what a wider row predicts — but carrying words across
    //     sentences stayed worse than not carrying them at all. The revival
    //     condition was RUN rather than waited for, and the refutation survived it
    //     narrowed.
    //
    //   * THE CONTROL WAS THE POINT. Splitting one cell in two halves the count in
    //     each, the count IS the weight, and the weight is the reciprocal of the
    //     hop price — so kinds make every temporal hop dearer whether or not they
    //     rank better, and a walk scoring higher on a third of the traffic may
    //     simply be a walk on a smaller budget. Sweeping against stamina is what
    //     separated the two.
    //
    // SO bAbI is expected to score below its old baseline and the scoreboard floor
    // records that as a decision rather than absorbing it. The refutation row's
    // revival condition is unchanged and now load-bearing: something has to make a
    // carried edge worth its ROW, because there is no longer a way to decline it.


    /// <summary>
    /// Whether counting company recovers a word class on real English, with nothing learnt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The oldest result in distributional semantics, asked of this front end's codes: two
    /// words are alike where they keep the same company, and a word class is what that
    /// recovers. It is what an embedding gives a sequence model for free, and the reason to
    /// ask it here is that nothing in this repo has ever run <see cref="Alternating"/> on
    /// text — every reading it has is from a generated world whose statistics are clean.
    /// </para>
    /// <para>
    /// Task one is the cleanest case the corpus has. A line is <i>mary moved to the
    /// bathroom</i>, so a name never shares a line with a name and a place never with a
    /// place, which is the exclusion clause satisfied by the grammar rather than by luck.
    /// Both classes keep the same company by construction. If shared company recovers a word
    /// class anywhere it recovers it here, and a null result here is a null result for the
    /// idea rather than for the corpus.
    /// </para>
    /// <para>
    /// What would drop it, written before it was run: the two classes coming back mixed with
    /// each other or with the function words. The company of <i>the</i> and <i>to</i> is
    /// every content word in the corpus, so an unweighted overlap has an obvious way to
    /// decide that everything is alike — and the answer to that is to weight company by how
    /// surprising it is, which is not built.
    /// </para>
    /// </remarks>
    [Fact]
    public void Whether_shared_company_recovers_a_word_class_on_real_english()
    {
        var (moments, word) = Read(task: 1);

        var groups = Alternating.From(moments, company: 0.5, floor: 20);

        output.WriteLine($"{moments.Count} lines, {word.Count} words said in them");

        foreach (var group in groups.OrderByDescending(one => one.Count))
            output.WriteLine(
                $"  {group.Count,2}  "
                + string.Join(" ", group.Select(code => word.GetValueOrDefault(code, "?")).Order()));

        var names = new[] { "mary", "john", "sandra", "daniel" };
        var places = new[] { "kitchen", "bedroom", "bathroom", "hallway", "garden", "office" };

        var held = groups
            .Select(group => group.Select(code => word.GetValueOrDefault(code, "?")).ToHashSet())
            .ToList();

        var forNames = held.OrderByDescending(one => one.Count(names.Contains)).FirstOrDefault() ?? [];
        var forPlaces = held.OrderByDescending(one => one.Count(places.Contains)).FirstOrDefault() ?? [];

        output.WriteLine(
            $"names: {forNames.Count(names.Contains)} of {names.Length} in one group of "
            + $"{forNames.Count} | places: {forPlaces.Count(places.Contains)} of {places.Length} "
            + $"in one group of {forPlaces.Count}");

        Assert.True(forNames.Count(names.Contains) >= 3,
            $"the best group held {forNames.Count(names.Contains)} of the four names, so shared "
            + "company does not recover a word class on real English and the likeness half of "
            + "this design needs company WEIGHTED by how surprising it is");

        Assert.True(forPlaces.Count(places.Contains) >= 4,
            $"the best group held {forPlaces.Count(places.Contains)} of the six places");

        // And they must be DIFFERENT groups, because one group holding both is not two word
        // classes recovered -- it is the derivation deciding that everything is alike, which
        // is exactly what unweighted company is expected to do where the function words reach
        // every line.
        // And a guard written AFTER the reading rather than a bar written before it, in the
        // shape `ScalingTests` uses. What came back was four groups and every one of them a
        // word class: the four names exactly, the four motion verbs exactly, the six places
        // with `is` among them, and `to` against `where` -- which are the statement and
        // question markers, alternatives that never share a line and keep the same company.
        // `the` reached no group at all, being in every line and so co-occurring with
        // everything, which is the exclusion clause refusing a background word without
        // anything having to weight it.
        Assert.True(forNames.All(names.Contains),
            $"the names group came back as {string.Join(" ", forNames.Order())}, which is not "
            + "only names -- it held exactly the four when this was written");

        Assert.False(forNames.SetEquals(forPlaces),
            "the names and the places came back as one group, so what was recovered is that "
            + "every content word keeps the same company rather than two classes");
    }

    /// <summary>
    /// Whether weighing company by how often a partner turned up finds what a bare set of it
    /// finds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two statistics for one idea were in this repo at once and only one of them shipped.
    /// <see cref="Alternating.BySpace"/> takes the share of company two codes share, as a set,
    /// so a partner seen once weighs what a partner seen a thousand times weighs. The
    /// statistic in <c>RecalledTests</c> takes the cosine of the counted company, and it is
    /// the one that priced a category at five points under the bag.
    /// </para>
    /// <para>
    /// So the reading that pays was taken on an object the shipped mechanism is not, and this
    /// is the comparison that says whether that matters. Both are now readings on one
    /// accumulator over one set of counts, differing in the one thing.
    /// </para>
    /// <para>
    /// What it decides: one of the two goes. Where they agree the counts are carrying nothing
    /// on this corpus and the simpler reading stays; where the weighed one finds more, it
    /// becomes the derivation and the bare set leaves with a revival row.
    /// </para>
    /// </remarks>
    /// <summary>
    /// What a likeness bar means on one vocabulary and not on another.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The statistic works on both worlds and the THRESHOLD does not travel</b>, which is
    /// a different problem from the one it looks like. <c>RoamingTests</c> recovers that
    /// world's three sets under <see cref="Codes.Alternating.ByCompany"/> at any bar from
    /// 0.50 to 0.90. Here the same call finds nothing at 0.50 and recovers cleaner word
    /// classes than the shipped statistic does at 0.10.
    /// </para>
    /// <para>
    /// <b>Because a cosine bar is not comparable across alphabets.</b> This corpus says about
    /// twenty words, so a code's company is a short sparse vector and two members of a class
    /// share few surprising partners; <c>Roaming</c> says far more, so the vectors are longer
    /// and the same relationship reads higher. The number is a fact about the vocabulary and
    /// the grouping is a fact about the world.
    /// </para>
    /// <para>
    /// <b>So this is the reading a mechanism has to answer</b>, and it is why nothing is
    /// wired yet. A bar handed in per world is a world reaching into the brain, which this
    /// repo refuses; what is wanted is a rule that reads the same on any alphabet, and the
    /// obvious shape is the one the naming gate already uses — a bar corrected for how many
    /// pairs were looked at rather than a level chosen for a corpus.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_a_likeness_bar_means_on_one_vocabulary_and_not_another()
    {
        var (moments, word) = Read(task: 1);

        var watching = new Alternating();

        foreach (var moment in moments) watching.Watch(moment);

        var names = new[] { "mary", "john", "sandra", "daniel" };
        var places = new[] { "kitchen", "bedroom", "bathroom", "hallway", "garden", "office" };

        output.WriteLine($"{moments.Count} lines, {word.Count} words said in them");
        output.WriteLine("alike | groups | names | places | biggest group");

        var best = 0;

        foreach (var alike in new[] { 0.50, 0.40, 0.30, 0.20, 0.10 })
        {
            var groups = watching.ByCompany(alike, floor: 20)
                .Select(group => group.Select(code => word.GetValueOrDefault(code, "?")).ToHashSet())
                .ToList();

            var held = groups.Count == 0
                ? 0
                : groups.Max(one => one.Count(names.Contains))
                    + groups.Max(one => one.Count(places.Contains));

            best = Math.Max(best, held);

            output.WriteLine(
                $"{alike,5:F2} | {groups.Count,6} "
                + $"| {(groups.Count == 0 ? 0 : groups.Max(one => one.Count(names.Contains))),5} "
                + $"| {(groups.Count == 0 ? 0 : groups.Max(one => one.Count(places.Contains))),6} "
                + $"| {(groups.Count == 0 ? "-" : string.Join(" ", groups.OrderByDescending(one => one.Count).First().Order()))}");
        }

        // The bar, and it is on the statistic rather than on the number. Weighing company by
        // how surprising it is recovers this corpus's word classes at SOME bar, so a run
        // where no bar reaches them says the statistic fails on real English rather than
        // that the threshold is awkward.
        Assert.True(best >= 8,
            $"the best bar recovered {best} of the four names and six places between them, so "
            + "weighing company by surprise does not find a word class on real English at any "
            + "threshold and the reading that says the number is the problem is wrong");

        // And no bar on WHICH threshold. That it differs from the one another world wants is
        // the finding; choosing one here would be this file deciding a mechanism's dial.
    }

    [Fact]
    public void Whether_weighing_company_finds_what_a_bare_set_of_it_finds()
    {
        var (moments, word) = Read(task: 1);

        var watching = new Alternating();

        foreach (var moment in moments) watching.Watch(moment);

        // One accumulator and one set of counts, so the only thing that differs between the
        // two rows is whether a partner's count is read or discarded.
        var bare = watching.BySpace(company: 0.5, floor: 20);
        var weighed = watching.ByLikeness(alike: 0.9, floor: 20);

        static string Said(IReadOnlySet<Code> group, IReadOnlyDictionary<Code, string> word) =>
            string.Join(" ", group.Select(code => word.GetValueOrDefault(code, "?")).Order());

        foreach (var (label, groups) in new (string, IReadOnlyList<IReadOnlySet<Code>>)[]
            { ("a set of company", bare), ("company weighed", weighed) })
        {
            output.WriteLine($"{label}: {groups.Count} groups");

            foreach (var group in groups.OrderByDescending(one => one.Count))
                output.WriteLine($"  {group.Count,2}  {Said(group, word)}");
        }

        // Reported rather than barred, because what the counts are worth on this corpus has
        // never been measured and a threshold written before the first reading would be the
        // answer rather than the finding. What is asserted is that both found SOMETHING, so a
        // row of agreement cannot be two derivations agreeing about nothing.
        Assert.NotEmpty(bare);
        Assert.NotEmpty(weighed);

        var same = bare.Select(one => Said(one, word)).Order().SequenceEqual(
            weighed.Select(one => Said(one, word)).Order());

        output.WriteLine(
            same
                ? "the two agree exactly, so the counts carry nothing here"
                : "the two differ, so discarding the counts is a decision and not a detail");
    }

    /// <summary>
    /// What each likeness reading recovers across the whole range of its threshold.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The comparison the head-to-head could not make. Reading the two at 0.5 and 0.9 says
    /// they differ and cannot say which is better, because a share of a set and a cosine are
    /// not the same scale and two hand-picked points move two things at once. Sweeping both
    /// puts each reading at its own best and compares those.
    /// </para>
    /// <para>
    /// Scored against a key, which is allowed here and nowhere else. Nothing the derivation
    /// sees is told the key; it exists to score what came back, in the way
    /// <c>RecalledTests</c> scores its own groups. A derivation handed the classes would be
    /// pricing a mechanism that does not exist.
    /// </para>
    /// <para>
    /// Purity is the column that decides. A group holding two of the key's classes is a
    /// category that claims something false the moment a scope is rewritten over it, and
    /// recovery bought with mixing is not recovery — so the reading to keep is the one whose
    /// best PURE row covers most of the key, and the loser leaves with a revival row.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_the_two_likeness_readings_recover_at_every_threshold()
    {
        var key = new Dictionary<string, string[]>
        {
            ["names"] = ["mary", "john", "sandra", "daniel"],
            ["places"] = ["kitchen", "bedroom", "bathroom", "hallway", "garden", "office"],
            ["verbs"] = ["went", "moved", "journeyed", "travelled"],
        };

        // The best each reading reached on each task, which is what the remark says decides
        // it. Collected rather than eyeballed, because this file printed both readings for
        // the life of the branch and asserted neither -- so the claim that one beats the
        // other lived in a commit message and in nobody's build.
        var best = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var task in new[] { 1, 2, 3 })
        {
            var (moments, word) = Read(task);

            var watching = new Alternating();

            foreach (var moment in moments) watching.Watch(moment);

            output.WriteLine($"task {task}, {moments.Count} lines, {word.Count} words said");
            output.WriteLine(" reading | at    | groups | judged | pure | covered");

            foreach (var (label, at, groups) in
                new[] { 0.3, 0.4, 0.5, 0.6, 0.7 }
                    .Select(one => ("a set  ", one, watching.BySpace(one, floor: 20)))
                    .Concat(new[] { 0.7, 0.8, 0.9, 0.95, 0.99 }
                        .Select(one => ("weighed", one, watching.ByLikeness(one, floor: 20)))))
            {
                var said = groups
                    .Select(group => group
                        .Select(code => word.GetValueOrDefault(code, "?")).ToHashSet())
                    .ToList();

                // A group is judged where it touches the key at all and pure where everything
                // in it comes from one class. Untouched groups are neither: the key does not
                // cover the function words, and counting them as impure would score the key.
                var judged = 0;
                var pure = 0;
                var covered = new Dictionary<string, int>();

                foreach (var group in said)
                {
                    var touched = key.Where(one => group.Any(one.Value.Contains)).ToList();

                    if (touched.Count == 0) continue;

                    judged++;

                    if (touched.Count > 1 || !group.All(touched[0].Value.Contains)) continue;

                    pure++;
                    covered[touched[0].Key] = Math.Max(
                        covered.GetValueOrDefault(touched[0].Key), group.Count);
                }

                var reach = key.Sum(one => covered.GetValueOrDefault(one.Key));

                best[label] = Math.Max(best.GetValueOrDefault(label), reach);

                output.WriteLine(
                    $" {label} | {at,-5:F2} | {groups.Count,6} | {judged,6} | {pure,4} | "
                    + string.Join(" ", key.Select(one =>
                        $"{one.Key} {covered.GetValueOrDefault(one.Key)}/{one.Value.Length}")));
            }
        }

        // NO BAR ON A THRESHOLD, because what either reading is worth across its range was
        // never measured and a number written before the first grid would be the answer
        // rather than the finding. What IS asserted is the comparison the remark says
        // decides it: the best pure row of each reading, and which of the two is higher.
        //
        // It is the reading rather than a prediction -- the weighed one reaches every class
        // of the key and the set one reaches no place at any threshold on any task. Putting
        // it here is what stops the claim living in a commit message alone.
        output.WriteLine(
            $"best covered: weighed {best["weighed"]}, a set {best["a set  "]}");

        Assert.True(best["weighed"] > best["a set  "],
            $"the weighed reading covers {best["weighed"]} of the key at its best row and "
            + $"the bare set covers {best["a set  "]}, so the set is no longer the loser "
            + "here. Fork 131 rests on weighed company taking text, and that is what this "
            + "asserts -- re-read it before the fork's framing is trusted again");
    }

    /// <summary>One task as a stream of moments, and what word each code is.</summary>
    /// <param name="task">Which task to read.</param>
    /// <remarks>
    /// <para>
    /// One line is one moment, and the answer is left out because it is not said in the line
    /// — a code that only ever arrives as an answer keeps no company at all.
    /// </para>
    /// <para>
    /// The naming is built from the lines rather than from <c>Alphabet</c>, which is the
    /// ANSWER vocabulary and holds six words here. A map covering the answers alone prints
    /// most of a grouping as question marks, which reads as a derivation that found nothing.
    /// </para>
    /// </remarks>
    private static (List<IReadOnlySet<Code>> Moments, Dictionary<Code, string> Word) Read(int task)
    {
        var babi = new Babi(World(task));

        return (
            [.. babi.Lines.Select(line => (IReadOnlySet<Code>)new HashSet<Code>(line.Words))],
            babi.Lines
                .Where(line => line.Text is not null)
                .SelectMany(line => Babi.Words(line.Text!))
                .Distinct()
                .ToDictionary(Babi.Of, one => one));
    }
}
