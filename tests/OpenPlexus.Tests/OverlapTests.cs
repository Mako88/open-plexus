using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// How much of a stream two machines have to share before they name the same things —
/// <b>fork 54, and the row <c>FoldingTests</c> could not reach.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>That file measured the two ends and called the middle the deployment case.</b>
/// Identical streams converge exactly and disjoint ones share almost nothing, which
/// between them establish that naming is deterministic in the EVIDENCE — and say nothing
/// about how much evidence two machines need in common. Twenty phones on one wifi share
/// most of what they see; twenty phones belonging to strangers share almost none, and the
/// whole question is where between those the mechanism stops agreeing with itself.
/// </para>
/// <para>
/// <b>So the overlap is the dial and it is the world's, not the brain's.</b> Each machine
/// draws each round either from a stream both can see or from one only it can, and the
/// share decides which. Nothing about the learner changes across the sweep, which is what
/// makes the column readable.
/// </para>
/// <para>
/// <b>And the ends are run as part of the sweep rather than cited from elsewhere.</b> An
/// overlap of one has to reproduce <c>FoldingTests</c>' converged row and an overlap of
/// nought its diverged one — if they do not, this world is not the one that file measured
/// and the middle of the curve means nothing.
/// </para>
/// </remarks>
public sealed class OverlapTests(ITestOutputHelper output)
{
    private const long Rounds = 20000;

    private const int Address = 3;

    /// <summary>
    /// A world that draws from a stream two machines share, or from one they do not.
    /// </summary>
    /// <remarks>
    /// <b>Indexed by round and never by a counter, which is the only way the shared half
    /// is actually shared.</b> Two machines advancing their own pointer into a common
    /// stream take DIFFERENT elements of it the moment their coins disagree once, so a
    /// shared stream consumed that way is two private streams with extra steps. Reading
    /// position <c>t</c> at round <c>t</c> means a round either of them takes from the
    /// common stream is the same round.
    /// </remarks>
    private sealed class Blended(
        IReadOnlyList<Turn<IReadOnlyList<int>>> common,
        IWorld<IReadOnlyList<int>> own,
        double overlap,
        int seed)
        : IWorld<IReadOnlyList<int>>
    {
        private readonly Random _coin = new(seed);

        private int _at;

        public int Outcomes => own.Outcomes;

        public Turn<IReadOnlyList<int>> Next()
        {
            var take = _coin.NextDouble() < overlap;
            var turn = take ? common[_at % common.Count] : own.Next();

            _at++;

            return turn;
        }
    }

    /// <summary>Trains one machine on a stream it shares with another by this much.</summary>
    /// <param name="common">The rounds both machines can draw.</param>
    /// <param name="overlap">The share of rounds drawn from it.</param>
    /// <param name="seed">This machine's private stream and its coin.</param>
    private static Population Trained(
        IReadOnlyList<Turn<IReadOnlyList<int>>> common, double overlap, int seed)
    {
        var brain = new Brain(new CommittingSettings(), seed);

        var world = new Blended(
            common,
            new Multiplexer(new MultiplexerSettings { Address = Address }, seed),
            overlap,
            seed);

        // THROUGH `Bench` rather than a loop written here. A second copy of predict,
        // score, settle, sweep, cover and repair is the one duplication that could
        // silently start learning something else, and the clone budget exists for it.
        new Bench(
            new Body(new Watching<IReadOnlyList<int>>(world, new Bits(Multiplexer.Bit))),
            brain).Run(Rounds);

        return brain.Held;
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void How_much_of_a_stream_two_machines_need_in_common()
    {
        IWorld<IReadOnlyList<int>> source =
            new Multiplexer(new MultiplexerSettings { Address = Address }, seed: 5);

        var common = new List<Turn<IReadOnlyList<int>>>((int)Rounds);

        for (var round = 0L; round < Rounds; round++) common.Add(source.Next());

        output.WriteLine("overlap | names | in common | folded apart");

        foreach (var overlap in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 })
        {
            var mine = Trained(common, overlap, seed: 1);
            var yours = Trained(common, overlap, seed: 2);

            var ours = mine.Names.Means.Select(one => one.Key).ToHashSet();
            var theirs = yours.Names.Means.Select(one => one.Key).ToHashSet();

            var shared = ours.Intersect(theirs).Count();
            var union = ours.Union(theirs).Count();

            var (_, apart) = FoldingTests.Compared(mine, yours);

            output.WriteLine(
                $"{overlap,7:F2} | {ours.Count,2} and {theirs.Count,2} | {shared,3} of {union,3} "
                + $"({(union == 0 ? 0.0 : shared / (double)union),5:P0}) | {apart,5}");
        }

        // The one assertion, and it is that the sweep has ends. At an overlap of one both
        // machines see the identical stream, so a private draw never happens and the two
        // must land on the same names -- if that row disagrees, `Blended` is not sharing
        // what it claims to and every row above it is measuring the wrong thing.
        var same = Trained(common, 1.0, seed: 1);
        var also = Trained(common, 1.0, seed: 2);

        Assert.Equal(
            same.Names.Means.Select(one => one.Key).ToHashSet(),
            also.Names.Means.Select(one => one.Key).ToHashSet());

        // No bar on the curve itself, because where naming stops converging has never been
        // measured and a threshold written before the first reading would be a prediction
        // dressed as a requirement. The grid is the finding.
    }
}
