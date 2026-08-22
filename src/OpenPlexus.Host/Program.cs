using System.Globalization;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;

namespace OpenPlexus.Host;

/// <summary>
/// One machine of a fleet, and the harness that puts the questions to it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The first two-process run</b>, and what <c>Ported</c> has been standing in for. That
/// fixture brings a whole fleet up inside one process on real ports, which tests the wire and
/// not the deployment: every population is an object the harness can reach, so nothing has ever
/// had to work when it could not. Here a holder is a process, its population is nobody else's,
/// and the only thing crossing is a message.
/// </para>
/// <para>
/// <b>Two verbs rather than two programs.</b> <c>hold</c> runs one holder and <c>ask</c> runs
/// the harness, and they share the dials, the seed and the peer list because a fleet whose
/// machines were built from different numbers is a fleet measuring the difference. One program
/// makes that sharing the compiler's rather than a note in a README.
/// </para>
/// <para>
/// <b>Run it as N + 1 processes on one box</b>, the harness first or last, in any order —
/// a machine announces itself when it opens and answers an announcement with one, so the
/// roster converges whatever order the fleet came up in.
/// </para>
/// <code>
/// dotnet run --project src/OpenPlexus.Host -- hold --listen http://localhost:5001 \
///     --address holder-0 --at 0 --holders 2 \
///     --peers http://localhost:5000,http://localhost:5001,http://localhost:5002
/// </code>
/// <para>
/// <b>And every machine is told the whole peer list including itself</b>, which is one string
/// to get right rather than N. <c>Posted</c> drops its own address out of it.
/// </para>
/// </remarks>
internal static class Program
{
    /// <summary>How long the harness waits for a fleet that may never come up.</summary>
    /// <remarks>
    /// <b>The experimenter's patience and never the machine's.</b> Nothing in the library may
    /// decide a missing holder by a clock — <i>a miss decided by a deadline</i> carries a
    /// revival row saying never — so a fleet that loses an answer waits forever, correctly.
    /// This is the operator at a terminal giving up, which is outside the machine and always
    /// was, and it is generous enough to be uninteresting.
    /// </remarks>
    private static readonly TimeSpan Patience = TimeSpan.FromMinutes(10);

    private static async Task<int> Main(string[] args)
    {
        return (args.Length == 0 ? null : args[0]) switch
        {
            "hold" => await HoldAsync(args).ConfigureAwait(false),
            "ask" => await AskAsync(args).ConfigureAwait(false),
            _ => Usage(),
        };
    }

    private static int Usage()
    {
        Console.Error.WriteLine(
            "usage:\n"
            + "  hold --listen <url> --address <name> --at <k> --holders <n> --peers <urls>\n"
            + "  ask  --listen <url> --holders <n> --peers <urls> [--rounds n] [--address n]\n"
            + "\n"
            + "  --peers is every machine's base address, this one included, comma separated.\n"
            + "  --holders is how many holders the fleet has, and the two verbs must agree.\n"
            + "  --seed and --capacity must be identical on every machine.");

        return 2;
    }

    /// <summary>
    /// One holder, listening until it is stopped.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The placement is the only thing that makes two machines different.</b> Every holder
    /// is told the same moment and runs the identical round, so without
    /// <see cref="Population.Places"/> a fleet of twelve would hold twelve copies of one
    /// population — the same rules minted by every machine that was surprised, which is not a
    /// shard and is not a distribution.
    /// </para>
    /// <para>
    /// <b>And <see cref="Population.Placing"/> is left null, which is not an oversight.</b>
    /// That predicate simulates sharding inside one process by telling the repair gate which
    /// of the commitments it can see are notionally elsewhere. Here what a holder can see is
    /// genuinely only what it holds, so the simulation would be a second and disagreeing
    /// account of the same fact.
    /// </para>
    /// </remarks>
    private static async Task<int> HoldAsync(string[] args)
    {
        var listen = Required(args, "--listen");
        var peers = Peers(args);
        var holders = Number(args, "--holders", 1);
        var at = Number(args, "--at", 0);
        var address = Given(args, "--address") ?? $"holder-{at}";
        var slot = Given(args, "--slot");

        if (at < 0 || at >= holders)
            throw new ArgumentException(
                $"--at {at} is not a holder of a fleet of {holders}", nameof(args));

        var mine = (ulong)at;

        var held = new Population(Dials(args), Number(args, "--seed", 1))
        {
            Places = one => one.Identity.Value % (ulong)holders == mine,
        };

        await using var bus = new Posted(listen, peers);

        var holder = new Holder(new MachineAddress(address), held, bus, slot);

        using var subscription = bus.Subscribe(holder);

        await bus.OpenAsync().ConfigureAwait(false);

        Console.WriteLine(
            $"holder {address} of {holders} listening on {listen}, "
            + $"{peers.Count - 1} peer(s)"
            + (slot is null ? "" : $", slot {slot}"));

        await Stopped().ConfigureAwait(false);

        Console.WriteLine(
            $"holder {address} stopping: answered {holder.Answered}, "
            + $"{held.Count} resident, {bus.Dropped} dropped, {bus.Refused} refused");

        return 0;
    }

    /// <summary>
    /// The harness, which holds no population and asks the ones that do.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The learning loop is <see cref="Bench"/>'s and exists exactly once.</b> What changes
    /// here is the substrate: <c>Fleet</c> is the council rather than <c>Alone</c>, so a vote
    /// is a gathering over sockets and a repair is placed on whoever holds the parent. Nothing
    /// above that seam is told which it is.
    /// </para>
    /// <para>
    /// <b>The population counts come back nought and that is honest</b>, this process holding
    /// none. <see cref="Tally.Holding"/> says so, and what the fleet holds is asked for and
    /// printed underneath — a number about the holders rather than about the harness.
    /// </para>
    /// </remarks>
    private static async Task<int> AskAsync(string[] args)
    {
        var listen = Required(args, "--listen");
        var peers = Peers(args);
        var holders = Number(args, "--holders", 1);
        var dials = Dials(args);
        var seed = Number(args, "--seed", 1);
        var rounds = Number(args, "--rounds", 4000);
        var address = Number(args, "--address", 2);

        await using var bus = new Posted(listen, peers);

        var asker = new Asker(new MachineAddress("asker"), bus);
        using var subscription = bus.Subscribe(asker);

        await bus.OpenAsync().ConfigureAwait(false);

        Console.WriteLine($"asker listening on {listen}, waiting for {holders} holder(s)");

        if (!await Until(() => bus.Holding.Count >= holders).ConfigureAwait(false))
        {
            Console.Error.WriteLine(
                $"only {bus.Holding.Count} of {holders} holders announced in {Patience}");

            return 1;
        }

        // One ask thrown away, which is the barrier the roster alone cannot be. Knowing where
        // the holders are says the outbound half converged; nothing observable says the holders
        // know where to send an answer, and that is the direction the announce-back exists for.
        // A round trip completing is the only honest check of it.
        using (var warming = await asker.AskAsync(Wanted.Counts).ConfigureAwait(false))
        {
            if (await Task.WhenAny(warming.Everyone, Task.Delay(Patience)).ConfigureAwait(false)
                != warming.Everyone)
            {
                Console.Error.WriteLine(
                    $"{warming.Heard} of {warming.Asked} answers came back, so the return path "
                    + "never converged");

                return 1;
            }
        }

        var council = new Fleet(asker, dials);
        var brain = new Brain(dials, seed, _ => council);

        var world = new Multiplexer(new MultiplexerSettings { Address = address }, seed);

        var trial = new Bench(
            new Watching<IReadOnlyList<int>>(world, new Bits(Multiplexer.Bit)), brain);

        Console.WriteLine(
            $"running {rounds} rounds of the {address}-address multiplexer over "
            + $"{bus.Holding.Count} holder(s)");

        // No local populations, because they are on other machines. `Bench` reports that
        // rather than summing an empty list into a column of noughts.
        var running = trial.RunAsync([], rounds);

        if (await Task.WhenAny(running, Task.Delay(Patience)).ConfigureAwait(false) != running)
        {
            Console.Error.WriteLine(
                $"the fleet never finished {rounds} rounds — it asked {council.Asked} and "
                + $"heard {council.Heard}, and the bus has lost {bus.Dropped + bus.Refused}");

            return 1;
        }

        var tally = await running.ConfigureAwait(false);

        // What the fleet holds, asked for rather than read, because this process holds none.
        using var counted = await asker.AskAsync(Wanted.Counts).ConfigureAwait(false);

        await Task.WhenAny(counted.Everyone, Task.Delay(Patience)).ConfigureAwait(false);

        Report(tally, council, bus, counted);

        return 0;
    }

    /// <summary>What the run did, and where each number came from.</summary>
    /// <param name="tally">The loop's own account.</param>
    /// <param name="council">The substrate, which counts what it asked and heard.</param>
    /// <param name="bus">The transport, which counts what it lost.</param>
    /// <param name="counted">A last gathering, for what the fleet holds.</param>
    /// <remarks>
    /// <b>The denominator is printed beside the score</b>, because nothing here decides a
    /// missing holder by a clock — so a run that quietly stopped asking one machine learns
    /// from the rest, scores perfectly well, and is wrong about something no accuracy reports.
    /// Asked against heard is the only reading that says so.
    /// </remarks>
    private static void Report(Tally tally, Fleet council, Posted bus, Gathering counted)
    {
        Console.WriteLine(
            $"rounds     : {tally.Rounds}, {tally.Right} right, {tally.Wrong} wrong, "
            + $"{tally.Silent} silent, {tally.Abstained} abstained");
        Console.WriteLine(
            $"recent     : {tally.Recent.ToString("F3", CultureInfo.InvariantCulture)} "
            + $"over the last tenth, confidence "
            + tally.Confidence.ToString("F3", CultureInfo.InvariantCulture));
        Console.WriteLine(
            $"repair     : {tally.Repaired} repaired, {tally.Minted} minted, "
            + $"{tally.Subsumed} subsumed");
        Console.WriteLine($"council    : asked {council.Asked}, heard {council.Heard}");
        Console.WriteLine($"bus        : {bus.Dropped} dropped, {bus.Refused} refused");

        // And the population is somewhere else, which is the whole point of the run.
        Console.WriteLine(
            $"holding    : {tally.Holding} populations here, so every count above about a "
            + "population is this process and not the fleet");

        var tables = counted.Tables();

        Console.WriteLine(
            $"fleet      : {counted.Heard} of {counted.Asked} holder(s) answered, "
            + $"{tables.Sum(one => (long)one.Counted.Rows.Length)} rows over "
            + $"{tables.Sum(one => (long)one.Counted.Scopes)} scopes");
    }

    /// <summary>The brain's numbers, which every machine of a fleet must agree on.</summary>
    /// <remarks>
    /// <b>Identical on both verbs and unenforceable across processes</b>, which is said here
    /// rather than hidden. A holder built from different dials than the harness is a fleet
    /// measuring the disagreement, and nothing on the wire carries a dial to check it against.
    /// What makes it survivable is that this is one program: the same flags parse the same way
    /// on both sides, so getting it wrong takes a different command line rather than a
    /// different build.
    /// </remarks>
    private static CommittingSettings Dials(string[] args) => new()
    {
        Capacity = Number(args, "--capacity", 2000),
    };

    /// <summary>Every machine's base address, this one included.</summary>
    private static IReadOnlyList<Peer> Peers(string[] args) =>
    [
        .. Required(args, "--peers")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(one => new Peer(one)),
    ];

    /// <summary>Waits for something to become true, or gives up.</summary>
    /// <param name="settled">What is being waited for.</param>
    private static async Task<bool> Until(Func<bool> settled)
    {
        var until = Environment.TickCount64 + (long)Patience.TotalMilliseconds;

        while (Environment.TickCount64 < until)
        {
            if (settled()) return true;

            await Task.Delay(25).ConfigureAwait(false);
        }

        return settled();
    }

    /// <summary>Waits for the operator to stop the process.</summary>
    private static Task Stopped()
    {
        var stopping = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Console.CancelKeyPress += (_, stopped) =>
        {
            stopped.Cancel = true;
            stopping.TrySetResult();
        };

        AppDomain.CurrentDomain.ProcessExit += (_, _) => stopping.TrySetResult();

        return stopping.Task;
    }

    private static int Number(string[] args, string named, int fallback) =>
        Given(args, named) is { } value
            ? int.Parse(value, CultureInfo.InvariantCulture)
            : fallback;

    private static string Required(string[] args, string named) =>
        Given(args, named)
        ?? throw new ArgumentException($"{named} is required", nameof(args));

    private static string? Given(string[] args, string named)
    {
        for (var at = 0; at < args.Length - 1; at++)
            if (string.Equals(args[at], named, StringComparison.Ordinal))
                return args[at + 1];

        return null;
    }
}
