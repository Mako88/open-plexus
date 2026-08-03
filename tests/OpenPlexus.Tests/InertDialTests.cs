using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// `Value` and `Accumulate` are wired and measurably do nothing here.
/// </summary>
/// <remarks>
/// <b>These assert the PRECONDITIONS for them to matter, not the effect.</b>
/// Both were swept in one run under a fixed policy and every arm landed within
/// a standard error of the others. The obvious explanations — that routes are
/// too few for `Sum` to differ from `Max`, and that marginals are too alike for
/// `Lift` to reorder anything — were both checked and both refuted. So the room
/// is there and the dials still do not use it, which is a stranger result than
/// either explanation and worth keeping visible.
/// </remarks>
public sealed class InertDialTests
{
    private static SnakeSettings World() => new()
    {
        Width = 15, Height = 15, Sight = 1,
        StartingEnergy = 200.0, EnergyPerStep = 1.0, EnergyPerFood = 30.0,
    };

    private static WalkSettings Dials() => new()
    {
        Stamina = 4.0, Foresight = 2.0, Value = ArrivalValue.Strength,
        Accumulate = Accumulate.Sum, Horizon = 50,
    };

    [Fact]
    public async Task Most_arrivals_are_reached_by_more_than_one_route()
    {
        // So `Sum` and `Max` have something to disagree about. Measured at 27%
        // reached by a single route over 3,274 arrivals.
        var ring = new Ring(seed: 42, replicas: 256);
        var local = new LocalClusters(ring);
        var bus = new HybridBus();
        var handles = new List<IDisposable>();

        for (var i = 0; i < 8; i++)
        {
            var address = new ClusterAddress($"c{i}");
            ring.Join(address);
            var cluster = new Cluster(address, bus, ring, Dials());
            local.Include(cluster);
            handles.Add(bus.Subscribe(cluster));
        }

        var eye = new InputMachine<SnakeFrame>(
            new MachineAddress("eye"), new SnakeSense(),
            new LocalRendezvous(local), bus, ring, Dials());
        handles.Add(bus.Subscribe(eye));

        var snake = new Snake(World(), seed: 3);
        var rng = new Random(3);
        Code? did = null;
        int single = 0, total = 0;

        for (var step = 0; step < 200 && snake.Alive; step++)
        {
            var thought = await eye.ObserveAsync(
                new SnakeFrame { View = snake.View(), Did = did }, step);
            await bus.WhenIdle().WaitAsync(TimeSpan.FromSeconds(5));

            if (thought is not null)
                foreach (var arrival in thought.Best(int.MaxValue))
                {
                    total++;
                    if (arrival.Routes == 1) single++;
                }

            var turn = (Turn)rng.Next(3);
            did = SnakeSense.Encode(turn);
            snake.Steer(turn);
        }

        var spread = local.All
            .SelectMany(cluster => cluster.Codes.Select(code =>
                cluster.TryGet(code, out var node) ? node.Seen : 0.0))
            .Where(seen => seen > 0)
            .ToArray();

        foreach (var handle in handles) handle.Dispose();

        Assert.True(total > 100, $"only {total} arrivals to look at");
        Assert.True(single < total / 2, $"{single} of {total} arrivals came by a single route");

        // And marginals differ by more than an order of magnitude, so `Lift`
        // has something to reorder.
        Assert.True(spread.Max() / spread.Min() > 10.0,
            $"marginals span only {spread.Max() / spread.Min():F1}x");
    }
}
