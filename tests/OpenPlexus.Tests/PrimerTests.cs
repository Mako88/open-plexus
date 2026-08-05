using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Plain English, and why it does not rescue the six structural noughts.
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S ASK, 2026-08-04: give it basic English first, then re-run what
/// failed, in the same run with no reset.</b> The reasoning was exactly right —
/// six bAbI tasks score nought because <i>yes</i>, <i>no</i>, <i>maybe</i> and the
/// counting words never occur as words, so there is no node to arrive at. Show it
/// English and the node exists.
/// </para>
/// <para>
/// <b>THE NODE DOES START EXISTING AND THE SCORE DOES NOT MOVE, which is a
/// finding about COST rather than about the idea.</b> Two facts collide. Real
/// English is a heavier-tailed distribution than anything
/// <see cref="SensesSettings.Skew"/> was pushed to — the commonest word is in half
/// of all sentences — so ingesting it is ruinous. And the words the tasks need are
/// RARE: the largest slice affordable holds <i>yes</i> a handful of times, which is
/// the single-accident population <see cref="WalkSettings.Doubt"/> exists to
/// disbelieve. Affordable is far below useful, and the gap is orders of magnitude.
/// </para>
/// <para>
/// <b>So this file asserts the WALL rather than a lift.</b> That is the honest
/// artifact: the experiment is blocked by throughput on heavy-tailed data, which
/// is the same wall <see cref="TailTests"/> found and what the plan's hierarchy and
/// Space-Saving items are for.
/// </para>
/// </remarks>
public sealed class PrimerTests(ITestOutputHelper output)
{
    private static string Corpus
    {
        get
        {
            var corpus = Path.Combine(Tree.Repo(), "corpora", "tatoeba_eng.tsv");

            Assert.True(File.Exists(corpus),
                $"the Tatoeba English export is not at {corpus}. Fetch it with:\n"
                + "    bash corpora/fetch.sh");

            return corpus;
        }
    }

    private static Primer Priming(int sentences) =>
        new(new PrimerSettings { Corpus = Corpus, Sentences = sentences });

    /// <summary>The words the six unreachable tasks answer with.</summary>
    private static readonly string[] Wanted = ["yes", "no", "maybe", "one", "two", "three"];

    [Fact]
    public void The_english_is_read_as_the_same_codes_the_tasks_ask_about()
    {
        // THE PROPERTY THE WHOLE EXPERIMENT RESTS ON. A primer with its own
        // tokenizer would mint a different code for `yes` and demonstrate nothing:
        // the word has to land on the NODE THE TASK WILL ASK ABOUT.
        var read = Primer.Read(["1\teng\tYes, I do.", "2\teng\tNo."], wanted: 2);

        Assert.Single(read);
        Assert.Contains(Babi.Of("yes"), read[0].Words);
        Assert.Equal([Babi.Of("yes"), Babi.Of("i"), Babi.Of("do")], read[0].Words.AsEnumerable());

        // AND A ONE-WORD LINE IS DROPPED, because a sentence joins its codes to
        // each other and a single code joins to nothing. `No.` is that line.
        Assert.DoesNotContain(read, line => line.Words.Length < 2);
    }

    [Fact]
    public void The_affordable_slice_holds_the_words_it_needs_only_a_handful_of_times()
    {
        // THE MEASUREMENT THAT EXPLAINS THE NULL RESULT, and it needs no walk at
        // all. A thousand sentences is about the most that can be swallowed at a
        // cap the questions still work under -- and it holds `yes` a handful of
        // times. Four accidental sentences is not a word the graph knows; it is
        // the rare coincidence `Doubt` was invented to discount.
        var primer = Priming(1000);
        var seen = new HashSet<Code>(primer.Lines.SelectMany(line => line.Words));

        var counts = Wanted.ToDictionary(
            word => word,
            word => primer.Lines.Count(line => line.Words.Contains(Babi.Of(word))));

        output.WriteLine(string.Join("  ", counts.Select(one => $"{one.Key}={one.Value}")));

        // THE NODE EXISTS, which is the half John's reasoning predicted and got
        // right. Every one of these was absent from bAbI entirely.
        Assert.All(Wanted, word => Assert.Contains(Babi.Of(word), seen));

        // AND IT IS FAR TOO THIN TO BE REACHED, which is the half that makes the
        // score sit still. If this ever stops holding, the primer got big enough
        // to matter and the null result above must be re-run rather than cited.
        Assert.True(counts["yes"] < 20,
            $"`yes` now appears {counts["yes"]} times in the affordable slice, so "
            + "it is no longer a single-accident node and the experiment that "
            + "found no lift needs re-running");
    }

    [Fact]
    public async Task Swallowing_english_is_priced_by_the_row_cap_and_nothing_else()
    {
        // THE WALL, AND IT IS THE SAME ONE `TailTests` FOUND ON A SYNTHETIC SKEW.
        // Real English is the heavy tail arriving for free: the commonest word is
        // in half of all sentences, so its row holds nearly the whole vocabulary
        // and `Node.Fire` emits one message per partner.
        //
        // MEASURED: twenty-five sentences of English cost an order of magnitude
        // more at a cap of 32 than at 8, and four hundred at 32 does not finish
        // inside the bus's thirty-second patience at all. The cap is not a saving
        // here, it is the difference between possible and not.
        var dear = await CostAsync(cap: 32);
        var cheap = await CostAsync(cap: 8);

        output.WriteLine($"cap=32 {dear} messages, cap=8 {cheap} messages");

        Assert.True(dear > cheap * 5,
            $"the row cap stopped pricing the English ({dear} against {cheap}), so "
            + "ingesting a heavy tail is no longer what this file says it is");
    }

    /// <summary>What a small slice of English costs to swallow, in messages.</summary>
    /// <remarks>
    /// <b>Twenty-five sentences and two of the task, because the English is the
    /// measurement.</b> Anything slower here is the corpus rather than the
    /// questions, and a bigger slice at the dearer cap does not finish.
    /// </remarks>
    private static async Task<long> CostAsync(int cap)
    {
        using var run = new BabiRun(
            new BabiSettings
            {
                Task = 6,
                Corpus = Path.Combine(Tree.Repo(), "corpora", "tasks_1-20_v1-2", "en-10k"),
                Stories = true,
            },
            Fixture.Dials(stamina: 8.0) with { Pricing = Pricing.Sender, Row = cap },
            seed: 1,
            primer: Priming(25));

        return (await run.RunAsync(2).ConfigureAwait(false)).Messages;
    }
}
