using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What the repair gate would cost on a wire — <b>fork 56, priced before it is built.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The gate asks something no count can answer.</b> So it is a round trip and not a
/// merge. <c>Mending.Uncovered</c> needs to know whether anything firing narrows this
/// commitment, and <c>Narrows</c> is a structural test between two scopes. What comes back
/// is a boolean, which is why asking keeps C1 where reading a population would not.
/// </para>
/// <para>
/// <b>And a round trip is the expensive unit</b>, which is the one thing already measured.
/// <c>LatencyTests</c> put a hop at a hundred milliseconds between two houses. A query per
/// candidate per round at that price is not a detail — it decides whether this gate can
/// exist off a LAN at all, and the count is the whole question.
/// </para>
/// <para>
/// <b>The question is which part is cacheable</b>, and the two halves are not alike.
/// <i>Does anybody anywhere hold a specialisation of my scope</i> changes only when
/// somebody mints one. <i>Did it fire this round</i> changes every round. If most
/// candidates have no specialisation anywhere, the first answer is a permanent no and the
/// second is never asked — so what matters is not how many candidates there are but how
/// many of them anyone could ever cover.
/// </para>
/// </remarks>
public sealed class QueryCostTests(ITestOutputHelper output)
{
    private const long Rounds = 20000;

    private const int Asked = 2000;

    private const int Address = 3;

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void How_many_round_trips_the_gate_would_cost_and_how_many_survive_a_cache()
    {
        // BOTH SETTINGS NAMED. The gate was half of one enum and the timing the other
        // half, so naming only the gate would move this onto an arm it was never measured
        // on.
        var dials = new CommittingSettings
        {
            Mending = Mending.Uncovered,
            Repairing = Repairing.EveryRound,
        };
        var brain = new Brain(dials, seed: 1);

        new MultiplexerRun(new MultiplexerSettings { Address = Address }, brain, seed: 1)
            .Run(Rounds);

        var held = brain.Held;
        var all = held.All.ToList();

        IWorld<IReadOnlyList<int>> world =
            new Multiplexer(new MultiplexerSettings { Address = Address }, seed: 99);

        var sensing = new Bits(Multiplexer.Bit);

        var rounds = 0;
        var naive = 0;
        var cached = 0;
        var answered_yes = 0;

        // Who could ever cover whom, which is the population-level half and changes only
        // on a mint. Computed once here because nothing mints during a read-only walk --
        // and that is a limit of this measurement rather than a property of the mechanism,
        // said again below.
        var coverable = all
            .Where(one => all.Any(other => other.Narrows(one)))
            .Select(one => one.Identity)
            .ToHashSet();

        for (var ask = 0; ask < Asked; ask++)
        {
            var turn = world.Next();

            var moment = held.Moment(new HashSet<Code>(sensing.Codify(turn.Seen)));

            var firing = held.Firing(moment);
            if (firing.IsDefaultOrEmpty) continue;

            // What actually followed, and the first version of this file used a constant.
            // `Repair` only considers commitments that were WRONG, so an outcome that never
            // varies makes half the population permanently a candidate and the other half
            // permanently not -- a traffic number measured against a world that was not
            // running.
            var arrived = Brain.Says(turn.Outcome!.Value);

            rounds++;

            foreach (var one in firing)
            {
                if (one.Expects == arrived) continue;
                if (one.Misses < dials.Floor) continue;

                // The naive scheme asks for every candidate every round.
                naive++;

                // and the cached one asks only where the answer could be yes. A commitment
                // nothing in the population specialises can never be covered by anything
                // firing, so its holder needs no wire at all -- one permanent no, and the
                // per-round question is never reached.
                if (!coverable.Contains(one.Identity)) continue;

                cached++;

                if (firing.Any(other => other.Narrows(one))) answered_yes++;
            }
        }

        output.WriteLine($"{rounds} rounds with anything firing");
        output.WriteLine($"naive: {naive} queries ({naive / (double)rounds:F1} a round)");
        output.WriteLine($"cached: {cached} queries ({cached / (double)rounds:F1} a round), "
            + $"{1.0 - cached / (double)naive:P1} removed");
        output.WriteLine($"of the cached asks, {answered_yes} came back yes "
            + $"({(cached == 0 ? 0.0 : answered_yes / (double)cached):P1})");

        output.WriteLine(
            $"{coverable.Count} of {all.Count} residents are coverable by anything at all");

        // THE INSTRUMENT CHECK. A population where nothing specialises anything makes the
        // cache free for the one reason that would make the number meaningless, and a
        // measurement that removed a hundred percent of the traffic should be read as a
        // world with no structure rather than as a good cache.
        Assert.True(coverable.Count > 0,
            "nothing in this population narrows anything, so the gate can never fire and "
            + "the traffic it saves is the traffic it never had");

        Assert.True(naive > 0, "no repair candidate ever came up, so nothing was priced");

        // NO BAR ON THE TRAFFIC, because what a deployment can afford is a fact about
        // somebody's network and not about this design. The counts are the finding.
    }
}
