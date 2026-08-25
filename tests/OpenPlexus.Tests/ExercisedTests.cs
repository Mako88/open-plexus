using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// Which entries of THE ARCHITECTURE a spine run actually puts through their paces —
/// <b>phase two's second half, which nothing had ever asked.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The phase is two things and only one of them was measured.</b>
/// <see cref="DocsTests.WithoutMechanism"/> says every entry under WHAT IT
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
/// <b>And the arms are the world's own rather than one run.</b> The house can be watched
/// or acted in, and it walks, sits a survey and is talked about under both. The reading is
/// over every arm the spine supports, so an entry reached by either is reached; taking one
/// would report the arm rather than the world.
/// </para>
/// <para>
/// <b>One spine world, so both arms are the house.</b> This used to run
/// <see cref="Roaming"/> in both arms while the sentence above claimed two worlds, which
/// was a documented promise standing in for a check. The conversation is a phase of the
/// house now, so the sentence and the code say the same thing.
/// </para>
/// </remarks>
public sealed class ExercisedTests
{
    /// <summary>The brain the house is walked with.</summary>
    /// <remarks>
    /// <para>
    /// <b>Named rather than written inline.</b> It can be compared with what ships, and the
    /// spine ran two worlds on two brains and a check could not say which dials parted while
    /// each was a literal at its own call site. It is the brain's own defaults and a bound on
    /// what it may hold, so a dial the terminal turns is a dial the walk does not.
    /// </para>
    /// </remarks>
    internal static CommittingSettings Walking => new() { Capacity = 20_000 };

    /// <summary>Which dials the spine's brains disagree on, in name order.</summary>
    /// <remarks>
    /// <para>
    /// <b>Read off the deployment's SOURCE.</b> Not off a copy of its settings, and the
    /// spine is one world, so the only way two brains can still run on it is a terminal
    /// composing one while the measurements compose another. A second settings object here
    /// would be a copy that drifts, and it would read as parity the whole time.
    /// </para>
    /// <para>
    /// <b>A dial the terminal NAMES is one the walk does not</b>, because
    /// <see cref="Walking"/> hands the brain its own defaults and nothing else. So the check
    /// is that the deployment's settings block names the capacity and no dial at all —
    /// a bound on what the machine may hold is a resource and not a way of thinking.
    /// </para>
    /// </remarks>
    internal static IReadOnlyList<string> BrainsApart() =>
    [
        .. typeof(CommittingSettings)
            .GetProperties()
            .Where(one => one.Name != nameof(CommittingSettings.Capacity))
            .Where(one => Composed().Contains(one.Name, StringComparison.Ordinal))
            .Select(one => $"{one.Name} is turned by the terminal and defaulted by the walk")
            .Order(StringComparer.Ordinal),
    ];

    /// <summary>The settings block <c>OpenPlexus.Talk</c> hands its brain, as source.</summary>
    /// <remarks>
    /// <b>The block rather than the file</b>, because the project names dials for the front
    /// end and for what the machine wants, and neither is a brain dial. What is read is the
    /// one construction that decides how the brain thinks.
    /// </remarks>
    private static string Composed()
    {
        var source = File.ReadAllText(
            Path.Combine(Tree.Repo(), "src", "OpenPlexus.Talk", "Program.cs"));

        var opened = source.IndexOf(
            $"new {nameof(CommittingSettings)}", StringComparison.Ordinal);

        if (opened < 0) return string.Empty;

        var closed = source.IndexOf('}', opened);

        return closed < 0 ? source[opened..] : source[opened..closed];
    }

    /// <summary>What one run of a spine world left behind.</summary>
    /// <param name="Arm">Which run it was, for the table.</param>
    /// <param name="Answered">
    /// How many of the house's conversation rounds it had an answer for, and
    /// <b>nought where the world is not the house</b>.
    /// </param>
    /// <param name="Tally">Every counter the bench reports.</param>
    /// <param name="Held">The population at the end of it.</param>
    /// <param name="Emitted">Which front-end modalities reached the brain.</param>
    /// <param name="Channels">Which of the front end's side channels were ever filled.</param>
    /// <param name="Told">Rounds a chooser read the population and named an action.</param>
    /// <param name="Supposals">
    /// What the second hop did — <b>because two failures read alike here</b>. An arm
    /// nothing reaches and an arm that changed nothing are the same unchanged table.
    /// </param>
    /// <param name="Parts">
    /// The most things the front end put in any one moment. <b>The most rather than a total</b>,
    /// because what it answers is whether TWO were ever sayable at once — a run reporting one
    /// part a moment for ever has a front end that segments and a moment that never held two,
    /// and a sum could not tell those apart.
    /// <b>And it is printed rather than read as the entry.</b> It was the entry's reading and
    /// it was satisfied by a run of no rounds at all, because the withheld pass binds whether
    /// or not the bench turns — a channel being filled by the harness standing in for the
    /// machine having done anything, which is the fault this file exists to catch.
    /// </param>
    private sealed record Watched(
        string Arm,
        long Answered,
        Tally Tally,
        Population Held,
        IReadOnlySet<byte> Emitted,
        IReadOnlySet<string> Channels,
        long Told,
        Supposed Supposals,
        int Parts);

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
    internal sealed class Noted(IQuantizer<Coded> inner) : IQuantizer<Coded>
    {
        /// <summary>Which modalities were emitted.</summary>
        public HashSet<byte> Emitted { get; } = [];

        /// <summary>Which side channels came back with anything in them.</summary>
        public HashSet<string> Channels { get; } = [];

        /// <summary>The most things the front end put in any one moment.</summary>
        /// <remarks>
        /// <b>Here rather than in <c>Fronted</c></b>, on this class's own reason: what the
        /// seam reports is a share of a moment the front end could place, and the most parts
        /// it ever said is a different question that only an instrument wants. A total could
        /// not answer it -- one part a moment for ever sums as high as two parts sometimes.
        /// </remarks>
        public int Parts { get; private set; }

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
        public IReadOnlyList<Grouped>? Bind(Coded observation)
        {
            var parts = Noting(inner.Bind(observation), nameof(Bind), one => one.Count > 0);

            if (parts is not null) Parts = Math.Max(Parts, parts.Count);

            return parts;
        }

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
    /// <param name="acting">Whether the learner takes the walk's steps.</param>
    /// <remarks>
    /// <b>The chooser is <see cref="Drives"/> where there is one</b>, rather than a coin. The
    /// entry being asked about is <i>original thought</i>, and a random chooser exercises the
    /// world's action channel while leaving the mechanism that is supposed to prefer one
    /// action over another untouched.
    /// </remarks>
    /// <param name="rounds">
    /// How many rounds to run. <b>Nought where a run that did nothing is wanted</b>, which is
    /// what <see cref="Every_entry_could_have_gone_unreached"/> reads — a bench asked for no
    /// rounds is the emptiest honest arm there is, and it cannot drift from the shape of a
    /// real one the way a hand-built <c>Tally</c> would.
    /// </param>
    private static Watched Run(bool acting, long rounds = 10_000)
    {
        // Walked, examined and then talked about, which is the whole of the one spine
        // world. The three phases are what reach different entries, so an arm that ran
        // only the walk would leave the exam and the telling unreached.
        //
        // And the person is scripted and often wrong, which is the point rather than a
        // shortcut. What the entry under `told must be settleable` asks is that a told
        // statement CARRIES a settlement and can be wrong about it; somebody answering
        // `kitchen` to everything supplies exactly that, and a stand-in that read the house
        // would be the world answering itself one seam over.
        var person = new Person(answers: ["kitchen", "garden", "apple", "one", string.Empty]);

        // Assigned below and closed over here, because the world asks the drive whether it has
        // had enough and the drive reads the brain the world is run into.
        Drives? drives = null;

        // And the walk ends when the MACHINE is done, wherever something is choosing. `Steps`
        // is the cap; a watched arm has no walker to have had enough, so the cap is its whole
        // length and always was.
        var world = new Roaming(
            Fixture.House(asked: 6, chatting: 6, person) with
            {
                Enough = acting ? () => drives?.Sated == true : null,
            },
            seed: 1);
        var brain = new Brain(Walking, seed: 1);
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

        // Handed in where the world and the brain meet, because which code an outcome is
        // about is a fact only the world holds. Without it `Supposing` cannot mark anything
        // and `relations are concepts too` goes unreached -- which is what the deletion of
        // the conversation world showed: that entry had been read off the only composition
        // that handed this in, and the house had never been asked for it.
        brain.Meaning = world.Meaning;

        // Which word an intervention code was derived from, built exactly rather than
        // guessed. The join holds both halves -- the world says which code a word is said as
        // and the brain says what it derives over a forced one -- so the mapping back is a
        // lookup, where a modulo over the hash used to answer with whatever word it landed on.
        var doings = Enumerable
            .Range(0, world.Doings)
            .ToDictionary(word => Intervened.Of(world.Meaning(word)!.Value), word => word);

        // A word and then something for it to be about, which is what a command and a
        // question both are. The fallback used to be one draw because a doing used to be one
        // number; a uniform draw over the whole alphabet would spend the run saying words no
        // parse can be made of, so what the acting arm exercised would be the parse refusing
        // rather than the world's own verbs.
        //
        // And the two question words are here for the BOOTSTRAP. `Drives` exploits and never
        // explores, so a word no commitment holds is not a candidate -- and no commitment
        // can hold `where` until something has said it. Without them the conversation runs
        // every episode and the machine never once puts a question, so the entry under
        // `what it is told must be settleable` reads unreached for want of a draw.
        var opening = new[] { "went", "took", "dropped", "where", "what" };

        var verbs = opening
            .Select(word => world.Vocabulary.ToList().IndexOf(word))
            .ToList();

        // Which of them wants a room rather than a thing: going somewhere and asking what a
        // room held.
        var asking = new[] { verbs[0], verbs[4] };

        var rooms = world.Named.Select(code => world.Naming(code)!.Value).ToList();
        var things = world.Called.Select(code => world.Naming(code)!.Value).ToList();

        var going = default(bool?);

        // Nothing to want and nothing told, which is stated rather than hidden. A house has no
        // felt bands, so every advocated action is wanted equally and what `Drives` is being
        // asked here is whether it can read the population at all -- and where the population
        // advocates none the fallback draws. Both halves are counted, so an arm that was its
        // own fallback all run reads as one.
        drives = new Drives(
            brain.Held,
            doing: code => doings.TryGetValue(code, out var word) ? word : null,
            wanting: (_, _) => 1.0,
            untold: () =>
            {
                if (going is { } went)
                {
                    var about = went ? rooms : things;

                    return about[falling.Next(about.Count)];
                }

                var verb = verbs[falling.Next(verbs.Count)];

                going = asking.Contains(verb);

                return verb;
            });

        // And the join derives what left, which is this world's own dial rather than a
        // default for every world -- the one default is refuted, and its row says a world
        // turns it on. A retraction is visible here: the store keyed on the freshest word
        // replaces the statement about a room the moment somebody leaves it, so the room word
        // stops being live and the departure is the event that says so. Off, the mechanism
        // under `it can say what does NOT hold` is reached by instruments alone and the spine
        // has never run it.
        var tally = new Bench(
            new Watching<Coded>(
                world,
                noted,
                acting: Chooses.From(
                    acting ? drives.Choose : _ => null,
                    () =>
                    {
                        drives.Cleared();

                        going = null;
                    }),
                departing: Departing.Left),
            brain)
            .Run(rounds, sweep: 1000, target: 0.9, window: 2000);

        return new Watched(
            acting ? "roaming acted" : "roaming watched",
            world.Answered, tally,
            brain.Held, noted.Emitted, noted.Channels, drives.Told, brain.Supposals,
            noted.Parts);
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

        new("A thing is one thing",
            "a scope was minted over ONE of the things a moment held",
            arms => Any(arms, one => one.Held.EverBorn.GetValueOrDefault(Birth.Bound) > 0)),

        new("It can say what does not hold",
            "a departure code reached a scope",
            arms => Any(arms, one => Scoped(one.Held, Departed.Left))),

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
            "somebody answered what the machine asked and the round was scored",
            arms => Any(arms, one => one.Answered > 0
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
    /// <b>Every arm runs whatever is asked</b>, because an entry reached by one and not the
    /// others is still reached and the caller cannot know which without all of them. Two runs
    /// of ten thousand rounds and a lesson is a few minutes, which is why nothing calls this
    /// twice.
    /// </para>
    /// </remarks>
    internal static IReadOnlyList<string> StillUnreached(Action<string> say)
    {
        ArgumentNullException.ThrowIfNull(say);

        var arms = new List<Watched>
        {
            // The house watched and the house acted in. A thing is MET here rather than
            // mentioned: seen and then named it holds two codes, so a scope over one of
            // them is a scope about one thing.
            // Four thousand rounds rather than ten, because what is wanted is that the
            // mechanisms are reached rather than a score. A walked house settles every
            // round, so it saturates a population far sooner.
            Run(acting: false, rounds: 4_000),
            Run(acting: true, rounds: 4_000),
        };

        var missed = new List<string>();

        foreach (var entry in Entries)
        {
            var ran = entry.Ran(arms);

            say($"{(ran ? "runs" : "NOT "),-5}| {entry.Line,-38}| {entry.Shows}");

            if (!ran) missed.Add($"{entry.Line} — wanted {entry.Shows}");
        }

        foreach (var arm in arms)
            say(
                $"{arm.Arm,-17}| held {arm.Tally.Resident} | repaired {arm.Tally.Repaired} "
                + $"| named {arm.Tally.Named} of {arm.Tally.Eligible} eligible "
                + $"({arm.Tally.PerEligible:F3}, spoke {arm.Tally.Speaking:F3}) "
                + $"| subsumed {arm.Tally.Subsumed} "
                + $"| abstained {arm.Tally.Abstained} | told {arm.Told} "
                + $"| supposed {arm.Supposals.Put}/{arm.Supposals.Refused}/{arm.Supposals.Moved} "
                + $"| marked {arm.Supposals.Marked} "
                + $"| parts {arm.Parts} "
                + $"| bound {arm.Held.EverBorn.GetValueOrDefault(Birth.Bound)} minted, "
                + $"{arm.Held.Births.Values.Count(one => one == Birth.Bound)} held "
                + $"| channels {string.Join(",", arm.Channels.Order())}");

        return missed;
    }

    /// <summary>
    /// <b>Every entry could have gone unreached</b>, which is the half a reading cannot say
    /// about itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A check that cannot fire reads exactly like a check that passes, and this file has been
    /// on the wrong side of that. <i>A concept a thing in its own right</i> asked whether
    /// <c>Population.Sorts</c> was set — which an EMPTY vocabulary satisfies, so handing the
    /// run a <see cref="Codes.Categories"/> turned it green with every counter in the run
    /// bit-identical to the control. It took a control to notice, and a control is exactly
    /// what nobody runs when a reading has gone green.
    /// </para>
    /// <para>
    /// <b>So every entry is asked of a run of ONE round</b>, and all but two have to say no.
    /// An entry that says yes on one round is satisfied by something the harness handed in
    /// rather than by the machine doing anything — the vocabulary was assigned before the
    /// bench started, so the old reading would have read true here with nothing derived.
    /// </para>
    /// <para>
    /// <b>The two are what one round honestly shows</b>, and they are named rather than
    /// tolerated. A front end really did manufacture symbols in that round, and none of the
    /// codes it emitted really was in the outcome alphabet. Both are facts about a round
    /// having happened; a third name arriving here is an entry that has stopped asking about
    /// a run, and it fails.
    /// </para>
    /// <para>
    /// <b>A bench asked for one round rather than a hand-built report.</b> Every field is the
    /// one a real arm would carry, so an entry reading a counter this does not have fails to
    /// compile rather than passing for free — and there is nothing to keep in step. Nought
    /// rounds would be better and the bench refuses it.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_entry_could_have_gone_unreached()
    {
        // What a single round shows without anything having been learnt, which is the whole
        // of what may read true below.
        string[] immediate =
        [
            "Every input an attribute of it",
            "Told, never architected",
        ];

        var nothing = new[]
        {
            Run(acting: false, rounds: 1),
            Run(acting: true, rounds: 1),
        };

        var free = Entries
            .Where(one => one.Ran(nothing))
            .Select(one => one.Line)
            .Except(immediate, StringComparer.Ordinal)
            .ToList();

        Assert.True(free.Count == 0,
            $"{free.Count} entr(ies) of THE ARCHITECTURE read as exercised by a run of ONE "
            + $"round: {string.Join(", ", free)}. Each is satisfied by something the harness "
            + "handed in rather than by a run doing anything, so it cannot tell a mechanism "
            + "that fired from one that is merely wired -- which is the fault this whole file "
            + "exists to catch, arriving inside it.");

        // And the two that may are still true, so the list is a claim about them rather than
        // a way of not being asked. One that stopped reading true would mean a single round no
        // longer reaches the front end at all, which is worth failing over.
        var shown = Entries.Where(one => one.Ran(nothing)).Select(one => one.Line).ToList();

        Assert.True(
            immediate.All(one => shown.Contains(one, StringComparer.Ordinal)),
            $"one round shows {string.Join(", ", shown)} against the {immediate.Length} named "
            + "here, so an entry that used to be reached by the front end alone no longer is "
            + "-- take it off the list rather than widening this");
    }

    /// <summary>How many entries there are to reach, for the message that counts them.</summary>
    internal static int Asked => Entries.Count;
}
