using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// Which entries of THE ARCHITECTURE a <see cref="Roaming"/> run actually puts through their
/// paces — <b>phase two's second half, which nothing had ever asked.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The phase is two things and only one of them was measured.</b>
/// <see cref="DocsTests.Every_requirement_has_a_mechanism"/> says every entry under WHAT IT
/// MUST DO carries a NOW leaf, so a mechanism exists for each. That is a fact about the doc
/// and the code. Whether the spine world REACHES any of them is a fact about a run, and a
/// mechanism no run reaches is this repo's oldest trap wearing phase two's clothes.
/// </para>
/// <para>
/// <b>An entry is exercised when a Roaming run leaves evidence of it</b>, and the evidence is
/// named beside each one rather than inferred. A counter that moved, a scope that holds a
/// kind of code, a channel the front end filled. What is refused is reading a mechanism as
/// exercised because it is wired: <c>Surprise</c> and <c>Abstain</c> were both wired and
/// unable to fire for the life of the branch.
/// </para>
/// <para>
/// <b>And the arms are the world's own rather than one run.</b> Roaming asks two questions
/// and can be watched or acted in, so the reading is over the arms it supports — an entry
/// reached by any of them is reached. Taking one arm would report the arm rather than the
/// world.
/// </para>
/// </remarks>
public sealed class ExercisedTests
{
    /// <summary>What one run of the world left behind.</summary>
    /// <param name="Examining">Which question the house was asked.</param>
    /// <param name="Tally">Every counter the bench reports.</param>
    /// <param name="Held">The population at the end of it.</param>
    /// <param name="Emitted">Which front-end modalities reached the brain.</param>
    /// <param name="Channels">Which of the front end's side channels were ever filled.</param>
    /// <param name="Told">Rounds a chooser read the population and named an action.</param>
    private sealed record Watched(
        Examining Examining,
        Tally Tally,
        Population Held,
        IReadOnlySet<byte> Emitted,
        IReadOnlySet<string> Channels,
        long Told);

    /// <summary>
    /// A front end that says what it emitted — <b>the only way to see a channel filled and
    /// read by nothing.</b>
    /// </summary>
    /// <param name="inner">The translation being watched.</param>
    /// <remarks>
    /// <b>A decorator rather than a counter inside <see cref="Joined"/></b>, because a
    /// world's translation is not the place to put an instrument. What it records is what
    /// crossed the seam, which a population cannot say: a code the front end emitted and
    /// nothing ever scoped is invisible from the other end.
    /// </remarks>
    private sealed class Noted(IQuantizer<Coded> inner) : IQuantizer<Coded>
    {
        /// <summary>Which modalities were emitted.</summary>
        public HashSet<byte> Emitted { get; } = [];

        /// <summary>Which side channels came back with anything in them.</summary>
        public HashSet<string> Channels { get; } = [];

        /// <inheritdoc/>
        public byte Modality => inner.Modality;

        /// <inheritdoc/>
        public IReadOnlyCollection<Code> Codify(Coded observation)
        {
            var codes = inner.Codify(observation);

            foreach (var code in codes) Emitted.Add(code.Modality);

            return codes;
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<Code, int>? Bind(Coded observation) =>
            Noting(inner.Bind(observation), nameof(Bind), one => one.Count > 0);

        /// <inheritdoc/>
        public IReadOnlyDictionary<Code, int>? Order(Coded observation) =>
            Noting(inner.Order(observation), nameof(Order), one => one.Count > 0);

        /// <inheritdoc/>
        public IReadOnlySet<Code>? Fleeting(Coded observation) =>
            Noting(inner.Fleeting(observation), nameof(Fleeting), one => one.Count > 0);

        /// <inheritdoc/>
        public IReadOnlySet<Code>? Forced(Coded observation) =>
            Noting(inner.Forced(observation), nameof(Forced), one => one.Count > 0);

        /// <summary>Records a channel as filled when it came back with something in it.</summary>
        /// <param name="answered">What the channel said.</param>
        /// <param name="channel">Which channel it was.</param>
        /// <param name="filled">Whether what it said counts as filled.</param>
        /// <typeparam name="T">What the channel returns.</typeparam>
        private T? Noting<T>(T? answered, string channel, Func<T, bool> filled)
            where T : class
        {
            if (answered is not null && filled(answered)) Channels.Add(channel);

            return answered;
        }
    }

    /// <summary>One arm of the world, run into a brain.</summary>
    /// <param name="examining">Which question the house is asked.</param>
    /// <param name="acting">Whether the learner takes the walk's last step.</param>
    /// <remarks>
    /// <b>The chooser is <see cref="Drives"/> where there is one</b>, rather than a coin. The
    /// entry being asked about is <i>original thought</i>, and a random chooser exercises the
    /// world's action channel while leaving the mechanism that is supposed to prefer one
    /// action over another untouched.
    /// </remarks>
    private static Watched Run(Examining examining, bool acting)
    {
        var world = new Roaming(Fixture.House(examining), seed: 1);
        var brain = new Brain(new CommittingSettings { Capacity = 20_000 }, seed: 1);
        var falling = new Random(1);

        // ONE vocabulary for the fold and the population, which is the seam `Categories`
        // draws: a category the front end emits and one a scope is rewritten over have to be
        // the same code or the rewrite names something no moment ever holds. The derivation
        // sits UNDER the fold, so what it counts is what the world sent.
        var sorts = new Categories([]);

        brain.Held.Sorts = sorts;

        var noted = new Noted(new Sorted<Coded>(
            new Deriving<Coded>(
                new Joined(Joining.Resolved, resolution: 3, freshest: true),
                sorts,
                Counting.Company,
                Meeting.Rarely,
                floor: 20,
                every: 2_000),
            sorts));

        // Nothing to want and nothing told, which is stated rather than hidden. A house has no
        // felt bands, so every advocated action is wanted equally and what `Drives` is being
        // asked here is whether it can read the population at all -- and where the population
        // advocates none the fallback draws. Both halves are counted, so an arm that was its
        // own fallback all run reads as one.
        var drives = new Drives(
            brain.Held,
            doing: code => Intervened.Names(code) ? (int)(code.Value % 3UL) : null,
            wanting: (_, _) => 1.0,
            untold: () => falling.Next(3));

        var tally = new Bench(
            new Watching<Coded>(
                world,
                noted,
                acting: Chooses.From(acting ? drives.Choose : _ => null)),
            brain)
            .Run(10_000, sweep: 1000, target: 0.9, window: 2000);

        return new Watched(
            examining, tally, brain.Held, noted.Emitted, noted.Channels, drives.Told);
    }

    /// <summary>One entry of THE ARCHITECTURE, and what a run must show for it.</summary>
    /// <param name="Line">The entry, in the words the plan gives it.</param>
    /// <param name="Shows">What would count as having exercised it.</param>
    /// <param name="Ran">Whether any arm showed it.</param>
    private sealed record Entry(
        string Line, string Shows, Func<IReadOnlyList<Watched>, bool> Ran);

    /// <summary>Whether any arm satisfies a reading.</summary>
    /// <param name="arms">The arms.</param>
    /// <param name="showed">What one arm would have to show.</param>
    private static bool Any(IReadOnlyList<Watched> arms, Func<Watched, bool> showed) =>
        arms.Any(showed);

    /// <summary>Whether any resident scope holds a code of a modality.</summary>
    /// <param name="held">The population.</param>
    /// <param name="modality">The modality.</param>
    private static bool Scoped(Population held, byte modality) =>
        held.All.Any(one => one.Scope.Any(code => code.Modality == modality));

    /// <summary>The entries, in the order THE ARCHITECTURE gives them.</summary>
    /// <remarks>
    /// <b>Written out rather than parsed from the plan</b>, because what each one needs a run
    /// to show is a judgement about mechanisms and the plan says nothing about counters. The
    /// COUNT is held against the doc by a companion, so an entry added there and not here
    /// fails rather than passing unread.
    /// </remarks>
    private static IReadOnlyList<Entry> Entries =>
    [
        new("Understand concepts",
            "a commitment fired and was scored",
            arms => Any(arms, one => one.Tally.Right + one.Tally.Wrong > 0)),

        new("A concept a thing in its own right",
            "the derivation learnt a group and a scope was written over one",
            arms => Any(arms, one => one.Held.Sorts is { Count: > 0 }
                && Scoped(one.Held, Joined.Grouped))),

        new("Every input an attribute of it",
            "a front end manufactured symbols from the signal",
            arms => Any(arms, one => one.Tally.Codes > 0.0)),

        new("Relations are concepts too",
            "a commitment's own identity sat inside another's scope",
            arms => Any(arms, one => Scoped(one.Held, Commitment.Committed))),

        new("Concept and label independent",
            "rung five minted a name for a shared sub-scope",
            arms => Any(arms, one => one.Tally.Named > 0)),

        new("Understanding deepens without limit",
            "repair minted a narrower child through the gate",
            arms => Any(arms, one => one.Tally.Repaired > 0)),

        new("Which aspects are temporal",
            "rung three's precedence codes reached a scope",
            arms => Any(arms, one => Scoped(one.Held, Sequenced.Ordered))),

        new("Several grains at once",
            "subsumption kept a general rule over a narrower one",
            arms => Any(arms, one => one.Tally.Subsumed > 0)),

        new("Malleability is the record",
            "a resident's local estimate parted from its lifetime rate",
            arms => Any(arms, one => one.Held.All.Any(commitment =>
                commitment.Fired > 0
                && Math.Abs(commitment.Accuracy - commitment.Reliability) > 1e-9))),

        new("Learns by being wrong",
            "blame reached a commitment and a round abstained",
            arms => Any(arms, one => one.Tally.Blamed > 0 && one.Tally.Abstained > 0)),

        new("Told, never architected",
            "no code the front end emitted was in the outcome alphabet",
            arms => Any(arms, one =>
                one.Emitted.Count > 0 && !one.Emitted.Contains(Brain.Followed))),

        new("What it is told must be settleable",
            "the effect question scored what the statement it was told did",
            arms => Any(arms, one => one.Examining == Examining.Effect
                && one.Tally.Right + one.Tally.Wrong > 0)),

        new("Original thought",
            "a chooser read the population and a scope named the doing",
            arms => Any(arms, one => one.Told > 0 && Scoped(one.Held, Intervened.Did))),
    ];

    /// <summary>The entries the plan lists under WHAT IT MUST DO, in its own words.</summary>
    /// <remarks>
    /// <b>Read off the doc rather than shared with <see cref="DocsTests"/></b>, whose readers
    /// are its own. Two lines of markdown here is cheaper than opening that file's shape up
    /// to a second caller, and this one asks a narrower question: how many entries are there.
    /// </remarks>
    private static IReadOnlyList<string> Listed()
    {
        var lines = File.ReadAllLines(Path.Combine(Tree.Docs(), "plan.md"));

        var branch = Array.FindIndex(lines, line =>
            line.StartsWith("- ", StringComparison.Ordinal)
            && line.Contains("WHAT IT MUST DO", StringComparison.Ordinal));

        Assert.True(branch >= 0, "the plan has no `WHAT IT MUST DO` branch");

        return
        [
            .. lines
                .Skip(branch + 1)
                .TakeWhile(line => !line.StartsWith("- ", StringComparison.Ordinal))
                .Where(line => line.StartsWith("  - ", StringComparison.Ordinal))
                .Select(line => line[4..].Trim()),
        ];
    }

    /// <summary>
    /// <b>The list here is the doc's list</b>, so an architecture line added to one and not
    /// the other fails rather than going unexercised in silence.
    /// </summary>
    /// <remarks>
    /// <b>The promise this backs is in the remark on the list itself.</b> Writing the entries
    /// out was a judgement call about what a run must show, and the cost of writing them out
    /// is that the doc can grow an entry this file never hears about — which is a mechanism
    /// nothing asks about reading exactly like one that passed. A documented promise is not a
    /// check, and this is the check.
    /// </remarks>
    [Fact]
    public void Every_entry_of_the_architecture_is_asked_about()
    {
        var listed = Listed();
        var asked = Entries.Select(one => one.Line).ToList();

        Assert.NotEmpty(listed);

        Assert.True(listed.Count == asked.Count,
            $"the plan lists {listed.Count} entries under WHAT IT MUST DO and this file asks "
            + $"about {asked.Count}:\n  plan: {string.Join(" | ", listed)}"
            + $"\n  here: {string.Join(" | ", asked)}"
            + "\nAdd the entry here with what a run must show for it, in the plan's order.");
    }

    /// <summary>
    /// What the spine world's arms exercise, line by line — <b>the reading, and the entries
    /// no arm reached.</b>
    /// </summary>
    /// <param name="say">Where each line of the table goes.</param>
    /// <remarks>
    /// <para>
    /// <b>The assertion is <see cref="OutstandingTests"/>'s and the run is this file's</b>,
    /// which is <c>DeadCodeTests.StillStranded</c>'s arrangement and for its reason. The red
    /// set is named, so a second file failing on purpose makes every other red ambiguous —
    /// and the work here closes by the mechanisms being reached rather than by this file.
    /// </para>
    /// <para>
    /// <b>Both arms run whatever is asked</b>, because an entry reached by one and not the
    /// other is still reached and the caller cannot know which without both. Two runs of ten
    /// thousand rounds is a minute and a half, which is why nothing calls this twice.
    /// </para>
    /// </remarks>
    internal static IReadOnlyList<string> StillUnreached(Action<string> say)
    {
        ArgumentNullException.ThrowIfNull(say);

        var arms = new List<Watched>
        {
            Run(Examining.Where, acting: false),
            Run(Examining.Effect, acting: true),
        };

        var missed = new List<string>();

        foreach (var entry in Entries)
        {
            var ran = entry.Ran(arms);

            say($"{(ran ? "runs" : "NOT "),-5}| {entry.Line,-38}| {entry.Shows}");

            if (!ran) missed.Add($"{entry.Line} — wanted {entry.Shows}");
        }

        foreach (var (name, arm) in new[] { ("watched", arms[0]), ("acted", arms[1]) })
            say(
                $"{name,-8}| held {arm.Tally.Resident} | repaired {arm.Tally.Repaired} "
                + $"| named {arm.Tally.Named} of {arm.Tally.Eligible} eligible "
                + $"({arm.Tally.PerEligible:F3}, spoke {arm.Tally.Speaking:F3}) "
                + $"| subsumed {arm.Tally.Subsumed} "
                + $"| abstained {arm.Tally.Abstained} | told {arm.Told} "
                + $"| channels {string.Join(",", arm.Channels.Order())}");

        return missed;
    }

    /// <summary>How many entries there are to reach, for the message that counts them.</summary>
    internal static int Asked => Entries.Count;
}
