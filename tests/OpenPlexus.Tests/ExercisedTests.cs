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
/// <b>And the arms are the worlds' own rather than one run.</b> Roaming asks two questions
/// and can be watched or acted in; the conversation is told a lesson once and examined on it.
/// The reading is over every arm the spine supports, so an entry reached by any of them is
/// reached. Taking one arm would report the arm rather than the world.
/// </para>
/// <para>
/// <b>Both spine worlds, because the sentence had no code behind it.</b> This ran
/// <see cref="Roaming"/> in both arms and never <see cref="Conversing"/>, while
/// <see cref="OutstandingTests.The_spine_world_exercises_every_entry_of_the_architecture"/>
/// said an entry counted as reached when either spine world showed it. A documented promise
/// is not a check, and the promise was the wrong half of this pair to trust.
/// </para>
/// </remarks>
public sealed class ExercisedTests
{
    /// <summary>The brain the house is walked with.</summary>
    /// <remarks>
    /// <para>
    /// <b>Named rather than written inline, so the two are comparable.</b> The spine ran two
    /// worlds on two brains; a check cannot say which dials part while each is a literal at
    /// its own call site.
    /// </para>
    /// <para>
    /// <b>And it is NOT <see cref="Talking"/>'s pair, which was tried and reverted.</b>
    /// <c>RoamingTests.What_the_conversations_two_dials_cost_the_walk</c> read the walk as
    /// indifferent — 0.612, 0.640 and 0.538 under the conversation's pair against 0.587,
    /// 0.642 and 0.538 under these, for two thirds of the residents — so the cheap road to
    /// one brain looked open.
    /// </para>
    /// <para>
    /// <b>The effect arm pays HERE rather than on the grid.</b> Under
    /// <c>Admitting.Testable</c> it falls from 105 residents and 131 repairs to 39 and 7, and
    /// the derivation stops reaching a scope — so <i>a concept a thing in its own right</i>
    /// goes unreached and the spine loses an entry of THE ARCHITECTURE to buy dial parity.
    /// The grid re-run with the question as an axis reads the same bar as a fifth of the
    /// population and no score, on both questions: it runs a bare front end and no chooser,
    /// and this composition runs a derived vocabulary and one that acts. The cost is in that
    /// interaction rather than in the bar.
    /// Parity is the means and coverage is the end, so the trade is refused and
    /// <c>OutstandingTests.The_spine_runs_one_brain</c> stays red with a measured reason
    /// rather than an unexamined one.
    /// </para>
    /// </remarks>
    internal static CommittingSettings Walking => new() { Capacity = 20_000 };

    /// <summary>The brain the conversation ships with, as <c>OpenPlexus.Talk</c> composes it.</summary>
    /// <remarks>
    /// <b>The deployment's own numbers</b>, which is what makes a difference from
    /// <see cref="Walking"/> a fact about the spine rather than about a fixture.
    /// </remarks>
    internal static CommittingSettings Talking => new()
    {
        Capacity = 20_000,
        Rooting = Rooting.Wholly,
        Crediting = Crediting.Birth,
        Admitting = Admitting.Testable,
    };

    /// <summary>Which dials the spine's two brains disagree on, in name order.</summary>
    /// <remarks>
    /// <b>Reflected rather than listed</b>, so a dial added to one composition and not the
    /// other appears here without anybody remembering to write it down. What it reads is the
    /// settings object each spine world hands its brain, which is where a world reaching into
    /// the brain would show.
    /// </remarks>
    internal static IReadOnlyList<string> BrainsApart() =>
    [
        .. typeof(CommittingSettings)
            .GetProperties()
            .Where(one => !Equals(one.GetValue(Walking), one.GetValue(Talking)))
            .Select(one => $"{one.Name} {one.GetValue(Walking)} vs {one.GetValue(Talking)}")
            .Order(StringComparer.Ordinal),
    ];

    /// <summary>What one run of a spine world left behind.</summary>
    /// <param name="Arm">Which run it was, for the table.</param>
    /// <param name="Examining">
    /// Which question the house was asked, and <b>nothing where the world is not the
    /// house</b>. A conversation has one question and it is the lesson's.
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
        Examining? Examining,
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
    /// <param name="examining">Which question the house is asked.</param>
    /// <param name="acting">Whether the learner takes the walk's last step.</param>
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
    /// <param name="knowing">Whether the walk is recited to the machine or walked by it.</param>
    /// <param name="seeing">Whether a look and a word are one code.</param>
    private static Watched Run(
        Examining examining, bool acting, long rounds = 10_000,
        Knowing knowing = Knowing.Recited, Seeing seeing = Seeing.Apart)
    {
        var world = new Roaming(Fixture.House(examining, knowing, seeing), seed: 1);
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

        // Which word an intervention code was derived from, built exactly rather than
        // guessed. The join holds both halves -- the world says which code a word is said as
        // and the brain says what it derives over a forced one -- so the mapping back is a
        // lookup, where a modulo over the hash used to answer with whatever word it landed on.
        var doings = Enumerable
            .Range(0, world.Doings)
            .ToDictionary(word => Intervened.Of(world.Meaning(word)!.Value), word => word);

        // A verb and then something for it to be about, which is what a command is. The
        // fallback used to be one draw because a doing used to be one number; a uniform draw
        // over the whole alphabet would spend the run saying words no command can be parsed
        // out of, so what the acting arm exercised would be the parse refusing rather than
        // the world's verbs.
        var verbs = new[] { "went", "took", "dropped" }
            .Select(word => world.Vocabulary.ToList().IndexOf(word))
            .ToList();

        var rooms = world.Named.Select(code => world.Naming(code)!.Value).ToList();
        var things = world.Called.Select(code => world.Naming(code)!.Value).ToList();

        var going = default(bool?);

        // Nothing to want and nothing told, which is stated rather than hidden. A house has no
        // felt bands, so every advocated action is wanted equally and what `Drives` is being
        // asked here is whether it can read the population at all -- and where the population
        // advocates none the fallback draws. Both halves are counted, so an arm that was its
        // own fallback all run reads as one.
        var drives = new Drives(
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

                going = verb == verbs[0];

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
            $"roaming {knowing.ToString().ToLowerInvariant()} "
            + $"{examining.ToString().ToLowerInvariant()}",
            knowing is Knowing.Explored ? null : examining, tally,
            brain.Held, noted.Emitted, noted.Channels, drives.Told, brain.Supposals,
            noted.Parts);
    }

    /// <summary>
    /// The other spine world, run into a brain — <b>the conversation, told a lesson once and
    /// then examined on it.</b>
    /// </summary>
    /// <param name="rounds">
    /// How many rounds to run, and <b>the whole lesson where nothing is said</b>. A
    /// conversation's length is the tutor's rather than a number picked here: a run cut short
    /// ends before the examination, so what it reaches would be a fact about the cut.
    /// </param>
    /// <param name="lesson">Which lesson is told, defaulting to the one every fact is stated in.</param>
    /// <param name="tellings">How many times it is told.</param>
    /// <param name="admitting">
    /// Which admission bar the repair gate holds, and <b>the axis that decides whether this
    /// arm repairs at all.</b> <c>OpenPlexus.Talk</c> passes
    /// <see cref="Admitting.Testable"/> where the brain's own default is
    /// <see cref="Admitting.Anything"/>, and under the deployment's choice this world reads
    /// <c>repaired 0</c> at one telling and at five. So every mechanism repair is the only
    /// road to is unreachable on the shipped composition, for a reason that is the bar rather
    /// than the mechanism.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>The deployment's own composition rather than a fixture's</b>, which is the only kind
    /// that says anything. <c>OpenPlexus.Talk</c> ships a <c>Sorted</c> over a
    /// <c>Deriving</c>, the three arms that were measured to win, and a
    /// <see cref="Curiosity"/> that reads the population — so this reaches what a session at a
    /// terminal reaches and not what a test could arrange.
    /// </para>
    /// <para>
    /// <b>Every doing is an ask or a claim</b>, so the chooser's own count is what says a
    /// population was read. A conversation has no body and no verb, so <c>Told</c> here is
    /// words spoken rather than actions taken and the entry it feeds reads nothing off it.
    /// </para>
    /// </remarks>
    private static Watched Talked(
        int? rounds = null, Lesson? lesson = null, int tellings = 1,
        Admitting admitting = Admitting.Testable)
    {
        var told = lesson ?? Lesson.Creatures;
        var tutor = new Tutor(told, TextWriter.Null, tellings: tellings);

        var brain = new Brain(Talking with { Admitting = admitting }, seed: 1);

        var world = Fixture.Talking(tutor);

        // ONE vocabulary for the fold and the population, which is the seam `Categories`
        // draws and is the same wiring the terminal ships.
        var sorts = new Categories([]);

        brain.Held.Sorts = sorts;

        var noted = new Noted(new Sorted<Coded>(
            new Deriving<Coded>(
                new Joined(Joining.Bagged),
                sorts,
                Counting.Company,
                Meeting.Rarely,
                floor: 5,
                every: 50),
            sorts));

        // Handed in where the world and the brain meet, because which code an outcome is
        // about is a fact only the world holds. Without it `Supposing` is one vote.
        brain.Meaning = world.Meaning;
        var curiosity = new Curiosity(brain, rate: 1.0, seed: 1, world.Naming);

        // Budgeted for the widest statement, because `Asserting.Everything` makes a sentence
        // one moment a word. A run stopping at the moment count ends before the examination.
        var tally = new Bench(
            new Watching<Coded>(
                world,
                noted,
                acting: Chooses.From(
                    felt => Doing(curiosity.Choose(felt)), curiosity.Cleared)),
            brain)
            .Run(
                rounds ?? (tutor.Moments * tutor.Longest),
                sweep: 200, target: 0.9, window: 50);

        return new Watched(
            $"conversing x{tellings} {admitting.ToString().ToLowerInvariant()}",
            null, tally, brain.Held, noted.Emitted, noted.Channels,
            curiosity.Claims + curiosity.Questions, brain.Supposals, noted.Parts);
    }

    /// <summary>The join between what a chooser decided and how this world numbers a doing.</summary>
    private static int? Doing(Wondered said) =>
        said.Word is not { } word
            ? null
            : said.Asking ? Conversing.Asks(word) : Conversing.Asserts(word);

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
            Run(Examining.Where, acting: false),
            Run(Examining.Effect, acting: true),

            // And a house the machine WALKS, which is where a thing is met rather than
            // mentioned. A thing seen and then named holds two codes, so a scope over one of
            // them is a scope about one thing -- and a mentioned thing is the one word that
            // names it, which is the root genesis already mints.
            // Fewer rounds than the recited arms, because what is wanted here is that the
            // mechanisms are reached rather than a score. A walked house settles every round
            // where a recital settles once, so it saturates a population far sooner -- and at
            // ten thousand it doubled the time this whole reading takes for no entry gained.
            Run(Examining.Where, acting: true, rounds: 4_000, knowing: Knowing.Explored),

            Talked(),

            // And a conversation that REPAIRS, which the arm above does not. The deployment
            // passes `Admitting.Testable` and under it this world reads `repaired 0` however
            // many times the lesson is told -- so every mechanism repair is the only road to
            // is unreachable on that composition. This one is the brain as it is built, on the
            // chained lesson, which is where the second hop has something to compose.
            Talked(lesson: Lesson.Chained, tellings: 5, admitting: Admitting.Anything),
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
            Run(Examining.Where, acting: false, rounds: 1),
            Talked(rounds: 1),
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
