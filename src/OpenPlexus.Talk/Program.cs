using System.Globalization;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;

namespace OpenPlexus.Talk;

/// <summary>
/// The walked house, wired to a terminal so somebody can talk to it.
/// </summary>
/// <remarks>
/// <para>
/// <b>A deployment is chosen by whoever composes the system</b>, which is why this is a project
/// of its own rather than an entry point on the library. What front end a stream is read through,
/// how big a population is allowed to be and what the machine wants are all decisions taken
/// here, and a library taking them would be deciding how everything it is ever shown is
/// perceived.
/// </para>
/// <para>
/// <b>A person talks to a machine that has just explored somewhere</b>, which is what an exam on
/// facts cannot be. The machine walks a house, sits a survey on what was in it, and then the
/// conversation opens — so what is said is about somewhere it went rather than about a block of
/// text it was recited. A score on stated facts is reachable by a script holding the transcript;
/// this is not, and that is the whole reason the phase exists.
/// </para>
/// <para>
/// <b>Run it with <c>dotnet run --project src/OpenPlexus.Talk</c></b>. The walk scrolls past
/// with <c>&gt;</c> in front of it, the survey's questions with <c>=</c>, and then it is your
/// turn: type what you like, answer what it asks, and type <c>.quit</c> to stop.
/// </para>
/// <para>
/// <b>And the brain is the one the walk is measured on</b>, dial for dial. The spine is one
/// world now, so a terminal shipping its own settings would be a second brain inside the set
/// that matters most — which is the fault <c>OutstandingTests.The_spine_runs_one_brain</c>
/// exists to catch.
/// </para>
/// </remarks>
internal static class Program
{
    private static int Main(string[] args)
    {
        var capacity = Number(args, "--capacity", 20_000);
        var seed = Number(args, "--seed", 1);

        // The house, and every number here is a SIZE. A real house has some number of rooms,
        // things, people and steps, and choosing six rather than eight does not make it less
        // like a house -- where a setting that chose what KIND of question got asked would be
        // a world the real one is not.
        var rooms = Number(args, "--rooms", 6);
        var props = Number(args, "--props", 4);
        var people = Number(args, "--people", 4);
        var steps = Number(args, "--steps", 40);
        var asked = Number(args, "--asked", 6);
        var chatting = Number(args, "--chatting", 20);

        var joining = Given(args, "--joining") is { } read
            ? Enum.Parse<Joining>(read, ignoreCase: true)
            : Joining.Resolved;

        // The brain's own defaults and nothing else, which is the point rather than an
        // omission. `ExercisedTests.Walking` is this composition and the walk is measured on
        // it; a dial turned here and nowhere else would make every reading on the house a
        // comparison between two brains as much as between two problems.
        var brain = new Brain(new CommittingSettings { Capacity = capacity }, seed);

        var world = new Roaming(
            new RoamingSettings
            {
                Rooms = rooms,
                Props = props,
                People = people,
                Steps = steps,
                Asked = asked,
                Chatting = chatting,
                Typed = Console.In,
                Printed = Console.Out,
            },
            seed);

        // Handed in where the world and the brain meet, because which code an outcome is
        // about is a fact only the world holds. Without it `Supposing` is one vote.
        brain.Meaning = world.Meaning;

        var draw = new Random(seed);

        // Wanting to LEARN, which is fork 146's drive and the arm the walk turned on. There is
        // nothing else to want: a house is not a body with variables to be in trouble about,
        // so every advocated word is wanted equally and what ranks them is how much saying one
        // would teach.
        var drives = new Drives(
            brain.Held,
            doing: world.Naming,
            wanting: (_, _) => 1.0,
            untold: () => draw.Next(world.Doings),
            arm: Wanting.Learning);

        // ONE vocabulary for the front end and the population, which is what `Categories`
        // requires: a category the fold puts in a moment and one a scope is rewritten over
        // have to be the same code, or the rewrite names something no moment holds. It starts
        // empty and only ever grows, so nothing a session learns is ever renamed.
        var sorts = new Categories([]);

        brain.Held.Sorts = sorts;

        // What the machine BELIEVES, said first where it has a belief about the moment in
        // front of it, and the drive after. A machine with only a drive is one nobody can ask
        // anything: `Drives` ranks what to say by how much saying it would teach, which is a
        // question about the population rather than an answer to whoever is typing.
        var answering = new Answers(
            brain,
            saying: expects => Brain.Meant(expects) is { } word && word < world.Doings
                ? word
                : null,
            otherwise: Chooses.From(drives.Choose, drives.Cleared));

        var bench = new Bench(
            new Watching<Coded>(
                world,
                new Sorted<Coded>(
                    new Deriving<Coded>(
                        new Joined(joining, resolution: 3, freshest: true),
                        sorts,
                        Counting.Company,
                        Meeting.Rarely,
                        floor: Number(args, "--floor", 20),
                        every: Number(args, "--deriving", 2_000)),
                    sorts),
                acting: answering),
            brain);

        // Enough rounds for as many houses as were asked for, because a run stopping mid-walk
        // would end before anybody was spoken to.
        var houses = Number(args, "--houses", 3);
        var rounds = Number(args, "--rounds", houses * (steps + asked + chatting));

        Console.WriteLine(
            $"walking, {rounds} rounds, capacity {capacity}, seed {seed}, {rooms} rooms, "
            + $"{props} things, {people} people, {steps} steps, {asked} asked, {chatting} "
            + $"rounds of talking, joining {joining.ToString().ToLowerInvariant()}");

        Console.WriteLine(
            $"  `>` is what it can see, `=` is the survey, `.` is what it said, `?` is what it "
            + $"asked you. type `{Roaming.Over}` to stop.");

        Console.WriteLine();

        var tally = bench.Run(rounds, sweep: 1000, target: 0.9, window: 2000);

        Console.WriteLine();
        Console.WriteLine(
            $"rounds     : {tally.Rounds} of {rounds}, ended {world.Ended}, {world.Left} "
            + "capped steps left unwalked");
        Console.WriteLine(
            $"walking    : it ordered {world.Ordered} commands and the house carried out "
            + $"{world.Did}");
        Console.WriteLine(
            $"talking    : {world.Questions} asked of you, {world.Answered} answered");
        Console.WriteLine(
            $"speaking   : it said what it believed {answering.Said} times, the drive named "
            + $"the word {drives.Told} times and the draw {drives.Untold}");
        Console.WriteLine(
            $"settling   : {tally.Right} right, {tally.Wrong} wrong, {tally.Abstained} settled "
            + "nothing");
        Console.WriteLine(
            $"population : {tally.Resident} resident, {tally.Minted} minted, {tally.Repaired} "
            + "repaired");
        Console.WriteLine(
            $"vocabulary : {world.Vocabulary.Count} words — "
            + string.Join(" ", world.Vocabulary));

        // What the derivation cost, beside what it found. A vocabulary that grows without
        // paying is the arm's own refutation, and a group that fills gradually mints a
        // category at every size it passes through.
        Console.WriteLine(
            $"categories : {sorts.Count} groups — "
            + string.Join(" | ", sorts.Groups.Select(group => string.Join(
                " ",
                group.Select(code => world.Naming(code) is { } at
                    ? world.Vocabulary[at]
                    : "?")))));

        // Which gate refused, and it is printed BEFORE `wanting` because it says whether that
        // number means anything. `Searched` is the only one of the five that reaches the scope
        // language, so a run where nothing reaches it has not learnt that its language is too
        // weak -- it has learnt that its gates are strict, and `wanting` reads 0.000 either way.
        Console.WriteLine(
            $"repair     : {brain.Held.Wrong} wrong, {brain.Held.AtFloor} under the floor, "
            + $"{brain.Held.AtBudget} out of budget, {brain.Held.AtCovered} already covered, "
            + $"{brain.Held.AtImproving} not improving, {brain.Held.Searched} searched");

        // What the ladder could not do, which is the reading this harness exists for. A high
        // share here is the admission rule firing: the machine was blamed and nothing in the
        // scope language told the misses from the hits.
        Console.WriteLine(
            $"wanting    : {tally.Wanting.ToString("F3", CultureInfo.InvariantCulture)} of "
            + $"{tally.Blamed} blamed rounds nothing separated"
            + (tally.Blamed == 0 ? " -- nothing was ever asked, so this says nothing" : ""));

        return 0;
    }

    private static int Number(string[] args, string named, int fallback) =>
        Given(args, named) is { } value
            ? int.Parse(value, CultureInfo.InvariantCulture)
            : fallback;

    private static string? Given(string[] args, string named)
    {
        for (var at = 0; at < args.Length - 1; at++)
            if (string.Equals(args[at], named, StringComparison.Ordinal))
                return args[at + 1];

        return null;
    }
}
