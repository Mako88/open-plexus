using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The composition, exercised with more than one sense in it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Written because the composition shipped uncomposed.</b> <see cref="Body"/> rotates a
/// cursor across its senses and asks every one before giving up, and every body in the repo
/// had exactly one sense — so the cursor, the inner loop and the quiet-sense path had never
/// run. That is the shape this project keeps finding read as built: <c>Surprise</c> and
/// <c>Abstain</c> were both wired and unable to fire, and <i>promiscuous on purpose</i> meant
/// exhaustive for the life of the branch.
/// </para>
/// <para>
/// <b>And it is the claim settlement by successor will rest on.</b> A sequence per source is
/// what makes the next moment from a sense the answer to its last one; a single counter over
/// everything would make one sensor's rate a fact about another's. That sentence is in
/// <see cref="Stamp"/>'s remarks and nothing had ever put it to a body with two clocks in it.
/// </para>
/// </remarks>
public sealed class BodyTests(ITestOutputHelper output)
{
    /// <summary>Six bits, which is the world step one is judged on.</summary>
    private const int Narrow = 2;

    /// <summary>A sense that has something to say every <c>every</c> pushes.</summary>
    /// <param name="source">Which stream it is.</param>
    /// <param name="every">How often it speaks — one for every push, two for every other.</param>
    /// <remarks>
    /// <para>
    /// <b>The one thing in the repo that is ever quiet</b>, and that is why it is here rather
    /// than in the library. A pulled world always has a next turn, so
    /// <see cref="Watching{TSeen}"/> never returns nothing — which left the path a real sensor
    /// takes untested at both ends: <see cref="Body.Push"/>'s inner loop, and the round
    /// <see cref="Bench"/> spends when no sense had anything.
    /// </para>
    /// <para>
    /// <b>Its codes name their own source</b>, so a moment can be traced back to the sense
    /// that pushed it. Two senses emitting the same codes is the realistic case and it is not
    /// the case that can be asserted about.
    /// </para>
    /// </remarks>
    private sealed class Ticking(byte source, int every) : IInput
    {
        private int _asked;
        private long _sequence;

        /// <inheritdoc/>
        public byte Source => source;

        /// <inheritdoc/>
        public int Outcomes => 2;

        /// <summary>How many times it was asked, whether or not it spoke.</summary>
        public int Asked => _asked;

        /// <summary>How many moments it pushed.</summary>
        public long Pushed => _sequence;

        /// <inheritdoc/>
        public Pushed? Push()
        {
            if (_asked++ % every != 0) return null;

            var at = _sequence++;

            return new Pushed
            {
                From = new Stamp { Source = source, Sequence = at },
                Codes = new HashSet<Code> { new(source, (ulong)(at % 4)) },
                Followed = Brain.Says((int)(at % 2)),
            };
        }
    }

    /// <summary>
    /// <b>Two senses take turns, and each keeps its own clock.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rotation asserted rather than described. Both senses speak on every push, so a
    /// body of two hands out alternating sources — and after an even number of pushes each
    /// has stamped exactly half of them, counting up from nought independently.
    /// </para>
    /// <para>
    /// <b>The independence is the half that matters.</b> A camera at two frames a second and
    /// a thermometer at one reading a minute are separate streams, and what follows a frame
    /// has to be the next frame. A shared counter would leave one sense's sequence full of
    /// gaps the other's pushes made, so its successor would never be the moment after it.
    /// </para>
    /// </remarks>
    [Fact]
    public void Two_senses_take_turns_and_each_keeps_its_own_clock()
    {
        var first = new Ticking(source: 7, every: 1);
        var second = new Ticking(source: 9, every: 1);

        var body = new Body(first, second);

        var sources = new List<byte>();
        var sequences = new Dictionary<byte, List<long>>();

        for (var at = 0; at < 10; at++)
        {
            var moment = body.Push();

            Assert.NotNull(moment);

            sources.Add(moment!.Value.From.Source);

            if (!sequences.TryGetValue(moment.Value.From.Source, out var seen))
                sequences[moment.Value.From.Source] = seen = [];

            seen.Add(moment.Value.From.Sequence);
        }

        // Alternating, from the first sense given. The order is fixed rather than fair on
        // purpose: a fixed seed reproduces a run exactly, and a schedule that depended on
        // which sense happened to be ready would cost that.
        Assert.Equal([7, 9, 7, 9, 7, 9, 7, 9, 7, 9], sources);

        // And each counted up from nought, which is what a sequence per source means. A
        // single counter over everything would give one sense 0, 2, 4, 6, 8 -- gaps the
        // other sense's pushes made -- and its successor would not be the moment after it.
        Assert.Equal([0L, 1L, 2L, 3L, 4L], sequences[7]);
        Assert.Equal([0L, 1L, 2L, 3L, 4L], sequences[9]);

        output.WriteLine(
            $"sources {string.Join("", sources)} | "
            + $"7 pushed {first.Pushed} | 9 pushed {second.Pushed}");
    }

    /// <summary>
    /// <b>A quiet sense costs the body nothing while another has something.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Body.Push"/>'s inner loop, which had never run. Senses at different rates
    /// is the arrangement the north star is in — camera, audio, temperature and motion do not
    /// arrive together — and a body that gave up on the first quiet sense would push at its
    /// slowest one's rate.
    /// </para>
    /// <para>
    /// <b>And the quiet one is still asked</b>, which is the half a count on the busy sense
    /// cannot see. A body that had quietly stopped polling a slow sensor would read exactly
    /// like this from the moments alone.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_quiet_sense_costs_the_body_nothing_while_another_speaks()
    {
        var busy = new Ticking(source: 7, every: 1);
        var slow = new Ticking(source: 9, every: 4);

        var body = new Body(busy, slow);

        var pushed = 0;

        for (var at = 0; at < 12; at++)
            if (body.Push() is not null) pushed++;

        // Every push produced a moment, because the busy sense always had one. The body
        // never returned nothing, so nothing here is the quiet path being read as an end.
        Assert.Equal(12, pushed);

        // The slow sense spoke a quarter of the times it was asked, and it WAS asked. Both
        // halves: a body polling only its busy sense would leave the second count at nought.
        Assert.True(slow.Asked > 0, "the slow sense was never asked");
        Assert.Equal(slow.Asked / 4 + (slow.Asked % 4 > 0 ? 1 : 0), slow.Pushed);

        Assert.Equal(12, busy.Pushed + slow.Pushed);

        output.WriteLine(
            $"{pushed} moments | busy asked {busy.Asked} pushed {busy.Pushed} | "
            + $"slow asked {slow.Asked} pushed {slow.Pushed}");
    }

    /// <summary>
    /// <b>A body with nothing to say returns nothing</b>, and the round is spent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The other end of the same path, and the one <see cref="Bench"/> has a branch for. No
    /// world in the repo is ever quiet, so the <c>continue</c> that spends a round on an empty
    /// body has never been taken — and a loop that spun waiting instead would hang a run on a
    /// sensor that had gone silent.
    /// </para>
    /// <para>
    /// <b>Read on the rounds rather than on a score</b>, because a run that took no moments
    /// scores nothing and looks like a population that learnt nothing. <c>Tally.Rounds</c> is
    /// the loop's count and not the number asked for, which is what makes the difference
    /// visible from outside.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_body_with_nothing_to_say_spends_the_round_rather_than_waiting()
    {
        // Quiet on two pushes in every three, and the only sense there is.
        var sometimes = new Ticking(source: 7, every: 3);

        var brain = new Brain(new CommittingSettings(), seed: 1);
        var bench = new Bench(new Body(sometimes), brain);

        const long Rounds = 300;

        var tally = bench.Run(Rounds, sweep: 1000, target: 0.9, window: 50);

        // A third of the rounds carried a moment and the rest were spent, which is the
        // branch being taken rather than a loop going round again.
        Assert.Equal(Rounds / 3, tally.Rounds);
        Assert.Equal(Rounds, sometimes.Asked);

        // And nothing was refused, which separates a quiet sense from a repeated stamp.
        // Both leave the round uncounted and only one of them is the brain declining.
        Assert.Equal(0, tally.Refused);

        output.WriteLine(
            $"{Rounds} rounds asked | {tally.Rounds} carried a moment | "
            + $"{tally.Refused} refused | sense asked {sometimes.Asked}");
    }

    /// <summary>
    /// <b>Two senses on one source are refused</b>, and so is a body with none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two guards on the constructor, neither of which anything reached. Sharing a source
    /// is the failure that would be silent: each sense would be settling the other's moments,
    /// and under settlement by successor the answer to a frame would be whatever the
    /// thermometer said next.
    /// </para>
    /// <para>
    /// <b>And it is refused at construction rather than reported</b>, because there is no
    /// reading that would show it. Two senses interleaved on one source produce a stamp
    /// sequence that counts up perfectly and means nothing.
    /// </para>
    /// </remarks>
    [Fact]
    public void Two_senses_sharing_a_source_are_refused_and_so_is_an_empty_body()
    {
        var shared = Assert.Throws<ArgumentException>(() =>
            new Body(new Ticking(source: 7, every: 1), new Ticking(source: 7, every: 1)));

        Assert.Contains("share a source", shared.Message, StringComparison.Ordinal);

        var empty = Assert.Throws<ArgumentException>(() => new Body());

        Assert.Contains("no senses", empty.Message, StringComparison.Ordinal);

        output.WriteLine("a shared source and an empty body are both refused");
    }

    /// <summary>
    /// <b>One brain learns two senses at once</b>, and neither settlement lands on the other.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The whole arrangement, on two real worlds rather than on a test input.
    /// <c>SeparationTests</c> already hands one brain two worlds and it runs them one after
    /// the other; this interleaves them, which is what a body does and what nothing had done.
    /// </para>
    /// <para>
    /// <b>Nothing is refused, which is the assertion.</b> Two senses whose sequences were kept
    /// anywhere but on the sense itself would have half of them declined — that is exactly
    /// how a stamp taken from the loop's round counter failed, and it failed silently, with
    /// the population still there and no longer learning.
    /// </para>
    /// <para>
    /// <b>And the population carries both</b>, read on the two modalities the front ends emit.
    /// A brain that had learnt one sense and refused the other would score perfectly well on
    /// the one it kept.
    /// </para>
    /// </remarks>
    [Fact]
    public void One_brain_learns_two_senses_interleaved()
    {
        var brain = new Brain(new CommittingSettings(), seed: 1);

        var bench = new Bench(
            new Body(
                new Watching<IReadOnlyList<int>>(
                    new Multiplexer(new MultiplexerSettings { Address = Narrow }, seed: 1),
                    new Bits(Multiplexer.Bit),
                    source: Body.First),
                new Watching<Coded>(
                    new Motif(new MotifSettings(), seed: 1),
                    new Passthrough(),
                    source: Body.First + 1)),
            brain);

        var tally = bench.Run(8000, sweep: 1000, target: 0.9, window: 1000);

        Assert.Equal(8000, tally.Rounds);
        Assert.Equal(0, tally.Refused);

        // Both senses are in the population, read on the modalities their front ends emit.
        // A brain that refused one of them would hold scopes from the other alone.
        var modalities = brain.Held.All
            .SelectMany(one => one.Scope)
            .Select(one => one.Modality)
            .ToHashSet();

        Assert.True(modalities.Count > 1,
            $"the population holds codes from one modality only: "
            + $"{string.Join(", ", modalities.Order())}");

        Assert.True(tally.Recent > 0.0,
            "nothing was answered over the last tenth of an interleaved run");

        // And NO scope crosses the two, which is a fact about the composition rather than
        // about this run. A scope is built from one moment and a moment comes from one
        // source, so two senses pushing separately can never appear in one scope however
        // long the run goes on. `Senses` crosses two senses inside ONE moment, which is why
        // that world reaches names spanning both and this arrangement cannot.
        //
        // Asserted rather than deduced, because the architecture wants the opposite: every
        // input is an attribute of a CONCEPT, and binding a seen ball to a heard `ball` is
        // the link this design exists to make. Recorded here so that the day something
        // fuses moments across sources, this is the check that goes red.
        var crossing = brain.Held.All
            .Count(one => one.Scope.Select(code => code.Modality).Distinct().Count() > 1);

        Assert.Equal(0, crossing);

        output.WriteLine(
            $"{tally.Rounds} rounds | {tally.Refused} refused | recent {tally.Recent:F3} | "
            + $"{brain.Held.Count} resident over modalities "
            + $"{string.Join(",", modalities.Order())} | {crossing} scopes cross a sense");
    }
}
