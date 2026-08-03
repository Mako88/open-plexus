using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using OpenPlexus.Graph;
using OpenPlexus.Thinking;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// Asking the graph what it expects to see next, given what it is about to do.
/// </summary>
/// <remarks>
/// <b>These test the mechanism, not the score.</b> The score is a measurement
/// and lives in docs/architecture.md with its sample size — and right now it is
/// a null: the graph predicts WORSE than drawing blind from the same alphabet.
/// See fork 19 for why, which is that the graph holds no temporal edges at all.
/// </remarks>
public sealed class ForesightTests
{
    private static SnakeSettings World() => new()
    {
        Width = 15, Height = 15, Sight = 1,
        StartingEnergy = 80.0, EnergyPerStep = 1.0, EnergyPerFood = 30.0,
    };

    private static WalkSettings Dials() => new()
    {
        Stamina = 4.0, Value = ArrivalValue.Strength,
        Accumulate = Accumulate.Sum, Horizon = 50,
    };

    [Fact]
    public async Task Every_step_asks_and_every_answer_is_settled()
    {
        using var run = new SnakeRun(World(), Dials(), seed: 3);
        var result = await run.PlayAsync(300);

        // PREQUENTIAL: a guess is settled against the next observation before
        // anything is counted, so the graph never sees the answer before being
        // asked. One question per step, less the first, which has nothing to
        // settle and the last, which is never settled.
        Assert.True(result.Foresight.Asked >= result.Steps - 2,
            $"{result.Foresight.Asked} questions over {result.Steps} steps");

        Assert.True(result.Foresight.Guessed > 0);
    }

    [Fact]
    public async Task A_blind_guess_is_drawn_and_scored_alongside()
    {
        // A control tests the DATA, not the code. Without it a hit rate is a
        // number with nothing to be better than -- and on a small alphabet
        // guessing blind scores well above zero, which is exactly what it does.
        using var run = new SnakeRun(World(), Dials(), seed: 3);
        var result = await run.PlayAsync(300);

        Assert.True(result.Foresight.Chance > 0, "the control never scored at all");
        Assert.True(result.Foresight.Blind > 0.0);
    }

    [Fact]
    public void A_prediction_that_names_nothing_is_not_a_question()
    {
        // An empty guess must not count as an asked-and-missed, or a graph that
        // said nothing would look like one that was wrong.
        var foresight = new Foresight();

        foresight.Settle([], [new Code(1, 1)], []);

        Assert.Equal(0, foresight.Asked);
    }

    [Fact]
    public void A_prediction_that_names_something_is_scored_both_ways()
    {
        // The companion. Without it the test above passes for a Foresight that
        // never counts anything at all.
        var foresight = new Foresight();
        var seen = new Code(1, 1);
        var missed = new Code(1, 2);

        foresight.Settle([seen, missed], [seen], [missed]);

        Assert.Equal(1, foresight.Asked);
        Assert.Equal(1, foresight.Hit);
        Assert.Equal(2, foresight.Guessed);
        Assert.Equal(1, foresight.Right);
        Assert.Equal(0, foresight.Chance);
        Assert.Equal(0.5, foresight.Precision);
    }

    [Fact]
    public void Narrowing_returns_only_that_sense()
    {
        var thought = new Thought(BroadcastId.New(), 1, Accumulate.Sum);

        foreach (var code in (Code[])[new(1, 10), new(1, 11), new(2, 20)])
            thought.Receive(new Arrival
            {
                Endpoint = code, Score = 1.0, Chain = [code], Best = 1.0, Routes = 1,
            });

        var seen = thought.BestOf(1, 10);

        // Narrowed to a sense it is a prediction; narrowed to a machine's codes
        // it is an action. Letting the other modality through would mean
        // predicting that the body is about to see itself move.
        Assert.All(seen, a => Assert.Equal(1, a.Endpoint.Modality));

        // The companion: it returns something, so the filter is not simply
        // emptying the list.
        Assert.Equal(2, seen.Count);
        Assert.Single(thought.BestOf(2, 10));
    }

    [Fact]
    public async Task A_different_action_asks_a_different_question()
    {
        // THE COUNTERFACTUAL, which is the whole distinction between a world
        // model and a sequence model. Two candidate actions over the same state
        // must produce different predictions, or the action is decoration.
        var ring = new Ring(seed: 42, replicas: 64);
        var local = new LocalClusters(ring);
        var bus = new HybridBus();
        var handles = new List<IDisposable>();

        foreach (var name in (string[])["a", "b", "c", "d"])
        {
            var address = new ClusterAddress(name);
            ring.Join(address);
            var cluster = new Cluster(address, bus, ring, Dials());
            local.Include(cluster);
            handles.Add(bus.Subscribe(cluster));
        }

        var state = new Code(1, 100);
        var left = SnakeSense.Encode(Turn.Left);
        var right = SnakeSense.Encode(Turn.Right);
        var afterLeft = new Code(1, 200);
        var afterRight = new Code(1, 300);

        // Left has always been followed by one thing and right by another.
        var rendezvous = new LocalRendezvous(local);
        for (var i = 0; i < 20; i++)
        {
            await rendezvous.JoinAsync(new Occasion
            {
                Onsets = [state, left, afterLeft], Live = [], At = i * 2,
            });
            await rendezvous.JoinAsync(new Occasion
            {
                Onsets = [state, right, afterRight], Live = [], At = (i * 2) + 1,
            });
        }

        var eye = new InputMachine<Code[]>(
            new MachineAddress("eye"), new Straight(), new Nothing(), bus, ring, Dials());
        handles.Add(bus.Subscribe(eye));

        async Task<IReadOnlyList<Code>> Asking(Code action)
        {
            var thought = await eye.ThinkAsync([state, action]);
            await bus.WhenQuiet().WaitAsync(TimeSpan.FromSeconds(5));
            return [.. thought.BestOf(1, 4).Select(a => a.Endpoint)];
        }

        var expectingLeft = await Asking(left);
        var expectingRight = await Asking(right);
        foreach (var handle in handles) handle.Dispose();

        // CONDITIONING SHOWS UP AS RANK, NOT AS EXCLUSION, and that is the
        // mechanism rather than a shortfall in it. The state alone reaches both
        // outcomes -- it has co-occurred with each of them twenty times -- so a
        // broadcast carrying the state cannot help but reach both. What the
        // action does is make one of them cheaper to get to.
        //
        // `master` puts it exactly: a broadcast expresses preference as
        // ECONOMICS, not as selection. A sequential walk ranks its frontier and
        // picks; a broadcast cannot rank anything globally, but it can make one
        // kind of edge cheaper to keep walking.
        Assert.True(
            expectingLeft.ToList().IndexOf(afterLeft) < expectingLeft.ToList().IndexOf(afterRight),
            "asking with Left did not rank the thing Left leads to first");

        // The companion: the mirror holds, so this is conditioning rather than
        // one code simply outranking the other everywhere.
        Assert.True(
            expectingRight.ToList().IndexOf(afterRight) < expectingRight.ToList().IndexOf(afterLeft),
            "asking with Right did not rank the thing Right leads to first");
    }

    private sealed class Straight : IQuantizer<Code[]>
    {
        public byte Modality => 1;

        public IReadOnlyCollection<Code> Codify(Code[] observation) => observation;
    }

    private sealed class Nothing : IRendezvous
    {
        public ValueTask JoinAsync(Occasion occasion, CancellationToken ct = default) =>
            ValueTask.CompletedTask;
    }
}
