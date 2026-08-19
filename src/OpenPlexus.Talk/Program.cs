using System.Globalization;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;

namespace OpenPlexus.Talk;

/// <summary>
/// The conversation harness, wired to a terminal.
/// </summary>
/// <remarks>
/// <para>
/// <b>A deployment is chosen by whoever composes the system</b>, which is why this is a project
/// of its own rather than an entry point on the library. What front end a stream is read through,
/// how big a population is allowed to be and how curious the machine is are all decisions taken
/// here, and a library taking them would be deciding how everything it is ever shown is
/// perceived.
/// </para>
/// <para>
/// <b>Run it with <c>dotnet run --project src/OpenPlexus.Talk</c></b>. Type statements, type
/// questions, leave a line blank to start a new topic, and type <c>.quit</c> to stop.
/// </para>
/// </remarks>
internal static class Program
{
    private static int Main(string[] args)
    {
        var rounds = Number(args, "--rounds", 400);
        var capacity = Number(args, "--capacity", 2000);
        var seed = Number(args, "--seed", 1);
        var rate = Fraction(args, "--asking", 0.25);

        var brain = new Brain(new CommittingSettings { Capacity = capacity }, seed);

        var world = new Conversing(new ConversingSettings
        {
            Typed = Console.In,
            Printed = Console.Out,
        });

        var curiosity = new Curiosity(brain, rate, seed, world.Naming);

        var bench = new Bench(
            new Watching<Recited>(
                world,
                new Joined(Joining.Bagged),
                acting: felt => Speaking(curiosity.Choose(felt))),
            brain);

        Console.WriteLine(
            $"talking, {rounds} rounds, capacity {capacity}, seed {seed}, asking "
            + $"{rate.ToString("F2", CultureInfo.InvariantCulture)} of the time");
        Console.WriteLine(
            "  a line is a moment. end a line with `?` to ask, leave one blank for a new "
            + $"topic, type `{Conversing.Over}` to stop.");
        Console.WriteLine(
            "  the machine says `. word` to claim and `? word` to ask. answer an ask with "
            + "yes, no, or the word.");
        Console.WriteLine();

        var tally = bench.Run(rounds, sweep: 200, target: 0.9, window: 50);

        Console.WriteLine();
        Console.WriteLine($"lines      : {tally.Rounds} of {rounds} rounds, ended {world.Ended}");
        Console.WriteLine(
            $"speaking   : {curiosity.Claims} claims, {curiosity.Questions} questions, "
            + $"{curiosity.Silences} with nothing to say");
        Console.WriteLine(
            $"asking     : {world.Asked} asked, {world.Told} answered, {world.Quiet} let go by");
        Console.WriteLine(
            $"settling   : {tally.Right} right, {tally.Wrong} wrong, {tally.Abstained} settled "
            + "nothing");
        Console.WriteLine(
            $"population : {tally.Resident} resident, {tally.Minted} minted, {tally.Repaired} "
            + "repaired");
        Console.WriteLine(
            $"vocabulary : {world.Vocabulary.Count} words — "
            + string.Join(" ", world.Vocabulary));

        // What the ladder could not do, which is the reading this harness exists for. A high
        // share here is the admission rule firing on typed English: the machine was blamed and
        // nothing in the scope language told the misses from the hits.
        Console.WriteLine(
            $"wanting    : {tally.Wanting.ToString("F3", CultureInfo.InvariantCulture)} of "
            + $"{tally.Blamed} blamed rounds nothing separated");

        return 0;
    }

    /// <summary>An action index for what the machine decided to say.</summary>
    /// <remarks>
    /// <b>The join</b>, and it is one line because that is all the coupling there is. A chooser
    /// hands back a word and an intent; how a world numbers its doings is that world's business,
    /// and this is where the two meet.
    /// </remarks>
    private static int? Speaking(Wondered said) =>
        said.Word is not { } word
            ? null
            : said.Asking ? Conversing.Asks(word) : Conversing.Asserts(word);

    private static int Number(string[] args, string named, int fallback) =>
        Given(args, named) is { } value
            ? int.Parse(value, CultureInfo.InvariantCulture)
            : fallback;

    private static double Fraction(string[] args, string named, double fallback) =>
        Given(args, named) is { } value
            ? double.Parse(value, CultureInfo.InvariantCulture)
            : fallback;

    private static string? Given(string[] args, string named)
    {
        for (var at = 0; at < args.Length - 1; at++)
            if (string.Equals(args[at], named, StringComparison.Ordinal))
                return args[at + 1];

        return null;
    }
}
