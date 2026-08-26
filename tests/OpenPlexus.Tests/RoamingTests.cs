using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whether the roaming world DEMANDS a situation model, asked before anything is built to
/// give it one — <b>fork 100's lesson applied on the way in rather than after.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>bAbI looked like it demanded reasoning</b> and its held-out half was all re-reading.
/// That cost this branch real time, and every score taken on it meant something other than
/// what it appeared to. So a new world is owed the same interrogation before a learner is
/// pointed at it, and the interrogation is cheap: the world knows its own state, so what a
/// shallow rule would reach is arithmetic rather than a training run.
/// </para>
/// <para>
/// <b>The three columns are the whole instrument.</b> The MARGINAL is always saying the
/// commonest room. The OPENING rule answers with the room the thing was first said to be
/// in, which is what a bag of the whole story reads straight off and is right exactly when
/// nothing moved. The LATEST rule answers with the most recent room word in the transcript,
/// which is recency and is what every displacement arm on this branch has actually been
/// doing. A perfect tracker is 1.000 by construction.
/// </para>
/// <para>
/// <b>So the world earns its keep</b> only if both shallow rules sit near the marginal. If
/// the opening rule is strong the walk is too short and the transcript answers itself; if
/// recency is strong the world is asking *what happened last* rather than *where is it
/// now*, and a situation model would be scored for something a one-line rule does.
/// </para>
/// </remarks>
public sealed class RoamingTests(ITestOutputHelper output)
{
    /// <summary>The house every reading here is taken in.</summary>
    /// <param name="steps">How long the walk is.</param>
    /// <param name="people">How many are walking it.</param>
    /// <param name="asked">How many survey questions follow the walk.</param>
    /// <param name="chatting">How many rounds a person gets once those are over.</param>
    /// <param name="person">Who is talking to it, where anybody is.</param>
    /// <remarks>
    /// <b>Named at each call rather than defaulted</b>, because a fixture inheriting a dial
    /// it does not pin is how a default moving rewrites an experiment nobody edited.
    /// </remarks>
    private static RoamingSettings World(
        int steps, int people, int asked = 0, int chatting = 0, Person? person = null) =>
        new()
        {
            Rooms = 6,
            Props = 4,
            People = people,
            Steps = steps,
            Asked = asked,
            Chatting = chatting,
            Typed = person,
            Printed = person?.Printed,
        };

    /// <summary>Which word each of this house's codes is, for reading a question's kind.</summary>
    /// <param name="house">The house whose alphabet it is.</param>
    /// <remarks>
    /// <b>An answer key on <c>Roaming.Named</c>'s standing</b>, and nothing that learns is ever
    /// shown it. What it is for is telling one KIND of exam question from another in the row,
    /// which is a fact about the vocabulary the world emitted.
    /// </remarks>
    private static Dictionary<Code, string> Named(Roaming house)
    {
        var words = new Dictionary<Code, string>();

        for (var one = 0; one < house.Vocabulary.Count; one++)
            words[house.Meaning(one)!.Value] = house.Vocabulary[one];

        return words;
    }

    /// <summary>The drive that wants to learn, over this house.</summary>
    /// <param name="brain">Whose population it reads.</param>
    /// <param name="house">Which house numbers the words.</param>
    /// <param name="draw">The fallback, for the rounds nothing is advocated.</param>
    /// <remarks>
    /// <b>Nothing to want beyond learning</b>, because a house is not a body with variables to
    /// be in trouble about. Every advocated word is wanted equally, so what ranks them is the
    /// term rather than a preference over outcomes.
    /// </remarks>
    /// <param name="arm">What it wants.</param>
    private static Drives Wanting(
        Brain brain,
        Roaming house,
        Random draw,
        Wanting arm = Machines.Wanting.Learning) =>
        new(
            brain.Held,
            doing: house.Naming,
            wanting: (_, _) => 1.0,
            untold: () => draw.Next(house.Doings),
            arm: arm);

    /// <summary>One exam question counted against its kind, where the round was one.</summary>
    /// <param name="world">The house being sat.</param>
    /// <param name="words">Which word each code is.</param>
    /// <param name="asked">How many of each kind were put, added to.</param>
    /// <param name="right">How many of each kind were got, added to.</param>
    /// <param name="better">Whether the loop scored this round right.</param>
    private static void Marked(
        Sitting world,
        IReadOnlyDictionary<Code, string> words,
        Dictionary<string, int> asked,
        Dictionary<string, int> right,
        bool better)
    {
        if (world.Asking is not { } code) return;

        var kind = words[code];

        asked[kind] = asked.GetValueOrDefault(kind) + 1;

        if (better) right[kind] = right.GetValueOrDefault(kind) + 1;
    }

    /// <summary>The house run into a brain, with nobody choosing for it.</summary>
    /// <param name="dials">How the brain is built.</param>
    /// <param name="seed">What draws the houses and the walks.</param>
    /// <remarks>
    /// <b>Watched rather than acted in</b>, because what a name is worth is about the
    /// population and a chooser puts a second thing in motion.
    /// </remarks>
    private static (Tally Tally, Brain Brain) Watched(CommittingSettings dials, int seed)
    {
        var brain = new Brain(dials, seed);

        // Somebody who says nothing, because nothing here speaks either: the arm is watched
        // rather than acted in, so no question is ever put and the conversation is six rounds
        // of an invitation nobody takes up. That is the same six moments this grid read
        // before the phase moved after the exam, which is what keeps the arms comparable.
        var quiet = new Person();

        var tally = new Bench(
            new Watching<Coded>(
                new Roaming(World(120, people: 2, asked: 6, chatting: 6, quiet), seed),
                new Joined(Joining.Resolved, resolution: 3, freshest: true),
                acting: Chooses.From(_ => null)),
            brain)
            .Run(10_000, sweep: 1000, target: 0.9, window: 2000);

        return (tally, brain);
    }

    /// <summary>
    /// <b>What switching the one broadening operator off costs.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Rung five is the only thing here that makes a scope SHORTER</b>, and a ladder that
    /// only discriminates is arbitrarily accurate and conceptless. So the arm asks what the
    /// rung is worth where the score has room to move, against a control that never names and
    /// a middle arm that names and refuses to let genesis root on what it named.
    /// </para>
    /// <para>
    /// <b>Two worlds, because one world's grid is a verdict on the world.</b> The walked house
    /// is the target and the eleven-bit multiplexer is the instrument whose ground truth can
    /// be enumerated, so a name that finds no new truth can be seen to find none.
    /// </para>
    /// <para>
    /// <b>Measured on the walked house, five seeds, ten thousand rounds.</b> Own 0.198, 0.199
    /// and 0.199 for named, unrooted and never; residents 1,657, 1,627 and 1,618; repairs
    /// 1,702, 1,659 and 1,649. Every column is inside noise, which is what the recited walk
    /// said before it was deleted — so the finding survived the world it was taken on.
    /// </para>
    /// <para>
    /// <b>And the spine mints ONE name a run</b>, so that null is weaker than it looks. An arm
    /// with nothing to bite on and an arm that bit and changed nothing read alike from a
    /// score, and the names column is what separates them.
    /// </para>
    /// <para>
    /// <b>The eleven-bit half is where the arms come apart</b>, and the middle one answers
    /// what the control could not. Sound rules 266.8, 176.4 and 58.2 for named, unrooted and
    /// never; unsound 285.8, 194.2 and 119.2; recent 0.990, 0.995 and 0.997. Found is 15.4
    /// under all three, to the digit.
    /// </para>
    /// <para>
    /// <b>So the ROOT is most of the inflation.</b> Refusing
    /// genesis the minted code takes a third of the extra sound rules away and half the extra
    /// unsound ones, and the truths found do not move — naming finds nothing a machine
    /// without it misses, whichever half of the delivery it keeps. Fork 129 changes which
    /// codes are named and keeps the delivery, so it inherits this.
    /// </para>
    /// <para>
    /// <b>And a held-out set is gone with the recital</b>, so the score reported is the
    /// trailing one. A fresh house every episode is what makes that legitimate: the world
    /// draws without replacement, so an online score is already an unseen score and the
    /// recurrence a withheld set exists to catch cannot happen.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_switching_the_broadening_operator_off_costs()
    {
        const int Seeds = 5;

        var arms = new[] { Broadening.Named, Broadening.Unrooted, Broadening.Never };

        var spine = arms.ToDictionary(
            arm => arm,
            arm => Enumerable.Range(1, Seeds).Select(seed => Spine(arm, seed)).ToList());

        output.WriteLine($"=== the walked house, {Seeds} seeds, 10,000 rounds ===");
        output.WriteLine(
            "arm     |            own |       resident |        repairs | names");

        foreach (var arm in arms)
        {
            var read = spine[arm];

            output.WriteLine(
                $"{arm,-8}| {Sweep.Spread([.. read.Select(one => one.Own)]),14} "
                + $"| {Sweep.Spread([.. read.Select(one => (double)one.Resident)], "F1"),14} "
                + $"| {Sweep.Spread([.. read.Select(one => (double)one.Repaired)], "F1"),14} "
                + $"| {read.Average(one => one.Named),5:F1}");
        }

        output.WriteLine("");
        output.WriteLine($"=== eleven bits, {Seeds} seeds, 20,000 rounds ===");
        output.WriteLine(
            "arm     |         recent |          sound |        unsound |          found | names");

        var bits = arms.ToDictionary(
            arm => arm,
            arm => Enumerable.Range(1, Seeds).Select(seed => Bits(arm, seed)).ToList());

        foreach (var arm in arms)
        {
            var read = bits[arm];

            output.WriteLine(
                $"{arm,-8}| {Sweep.Spread([.. read.Select(one => one.Recent)]),14} "
                + $"| {Sweep.Spread([.. read.Select(one => (double)one.Sound)], "F1"),14} "
                + $"| {Sweep.Spread([.. read.Select(one => (double)one.Unsound)], "F1"),14} "
                + $"| {Sweep.Spread([.. read.Select(one => (double)one.Found)], "F1"),14} "
                + $"| {read.Average(one => one.Named),5:F1}");
        }

        // The arm did what it says. `Never` naming anything is the whole of the control, and
        // a run where it minted would be measuring the sweep calendar instead.
        Assert.Equal(0, spine[Broadening.Never].Sum(one => one.Named));
        Assert.Equal(0, bits[Broadening.Never].Sum(one => one.Named));

        // And `Unrooted` still names, which is what makes it the middle arm rather than a
        // second control. It differs from `Named` in what genesis may reach for and in
        // nothing else, so a run where it stopped minting would be `Never` by another road.
        Assert.True(spine[Broadening.Unrooted].Sum(one => one.Named) > 0,
            "the unrooted arm minted nothing, so it is a second copy of the control rather "
            + "than the middle arm and the split it exists for has not happened");

        // And the shipped arm still does, which is the other half. A control against an arm
        // that had stopped firing would read level and say nothing.
        Assert.True(spine[Broadening.Named].Sum(one => one.Named) > 0,
            "the shipped arm minted nothing on the walked house, so this grid is comparing "
            + "two silences and the reading is about the world");

        return;

        static (double Own, int Resident, long Repaired, int Named) Spine(
            Broadening arm, int seed)
        {
            var (tally, _) = Watched(
                new CommittingSettings { Capacity = 20_000, Broadening = arm }, seed);

            return (tally.Recent, tally.Resident, tally.Repaired, tally.Named);
        }

        static Learned Bits(Broadening arm, int seed) =>
            new MultiplexerRun(
                new MultiplexerSettings { Address = 3 },
                new Brain(new CommittingSettings { Broadening = arm }, seed),
                seed)
                .Run(20_000);
    }

    /// <summary>
    /// A walked house shows the machine what is in front of it, and names one of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The machine explores rather than being recited to</b>, which is John's and is what
    /// the spine becoming one world turns on. Nothing narrates a step back to the walker that
    /// took it: what arrives is the room its own command put it in, so a moment is a
    /// consequence and the question <i>what would the world look like if I did X</i> is the
    /// one being answered.
    /// </para>
    /// <para>
    /// <b>And the naming is the SETTLEMENT</b>, which is ostension doing the job the plan
    /// gives it. One of the things in front of the machine is said out loud, and what the
    /// machine had to get right is which — so a round is scored on what it was shown rather
    /// than on a question at the end of a walk.
    /// </para>
    /// <para>
    /// <b>A thing met and then named holds TWO codes</b> where a look and a word are apart,
    /// which is what makes a scope over one of them a scope about one thing. A mentioned
    /// thing is the one word that names it, and a scope over one code is the root genesis
    /// already mints.
    /// </para>
    /// <para>
    /// <b>And repair reaches a look and its NAME before the binding can</b>, which is why a
    /// thing here shows a shade as well. <c>Spanning</c>'s generate half is written on the
    /// argument that a thing's scope is unreachable by repair: the code completing a thing is
    /// present whether or not the thing is bound that way, so it separates nothing. A name
    /// breaks that — it is absent until the world says it, so it separates the misses from
    /// the hits perfectly and repair takes it. A shade does not, being shared between things
    /// and present whenever any of them is in the room.
    /// </para>
    /// <para>
    /// <b>The binding is MINTED under both gates and SUBSUMED under both.</b> Under the
    /// shipped <c>Surprising.Unaccounted</c> genesis is called 1,639 times, runs 35 of them,
    /// and mints 49 scopes each over one thing; every one of the 49 is then deleted by
    /// subsumption and the run ends holding none. Ungated it mints 901 and subsumption takes
    /// 832, so 55 survive. The mechanism runs under both; what differs is how often, and 49
    /// mints at the ungated arm's survival would have bought about three.
    /// </para>
    /// <para>
    /// <b>Neither gate is what stops it.</b>
    /// Counting a code's absence BEFORE it arrived as variation — the one change fork 149
    /// proposed — moves the population from 1,139 to 1,162 and mints not one extra binding
    /// under either gate. What decides is subsumption, and it decides correctly on the
    /// evidence: genesis mints a thing's scope in the same call as the one-code roots it is
    /// built from, both are experienced past the floor, and the narrower one is never more
    /// accurate. A thing's shade is a FUNCTION of the thing — <c>Shades[thing % 4]</c> — so
    /// it is live whenever the look is, and a scope holding both fires exactly where the look
    /// alone fires. Sharedness is why a shade cannot carry a rule by itself; determinacy is
    /// why it adds nothing to one.
    /// </para>
    /// <para>
    /// <b>What was actually wrong was the INSTRUMENT.</b> <c>Population.Births</c> is dropped
    /// when a commitment is, for a memory reason written on it, so a count off it answers
    /// <i>did this survive</i>. The architecture entry asked it <i>did this happen</i>, and a
    /// whole item at the top of THE ORDER was written on the difference. <c>EverBorn</c>
    /// counts what was built, and both are printed here.
    /// </para>
    /// <para>
    /// <b>And what a binding wanted was two things OF A KIND</b>, which is the architecture
    /// line this entry sits under. A look used to name its own thing, so it identified it
    /// outright and no second attribute could add to it — both parts have to be ambiguous
    /// before the pair can say what neither does. <see cref="Roaming"/>'s looks are now four
    /// over eight props, adjacent so the shades differ, and the binding survives.
    /// </para>
    /// <para>
    /// <b>Measured on three seeds against the house that had none.</b> Surviving bindings go
    /// 0, 0, 0 to 2, 3, 4 under the shipped gate and 55, 43, 31 to 70, 55, 47 ungated, for a
    /// score of 0.175, 0.205, 0.205 against 0.200, 0.210, 0.210 — so posing the requirement
    /// costs the walk nothing and is what lets a scope over one thing hold its seat. The
    /// control is the same house with a look per thing, and it is a reading rather than a
    /// live arm: no world should fail to pose a line of THE ARCHITECTURE. Fork <b>149</b>.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_walked_house_shows_the_machine_what_is_in_front_of_it()
    {
        {
            var world = new Roaming(World(6, people: 2), seed: 4);

            var widest = 0;
            var deepest = 0;

            for (var round = 0; round < 24; round++)
            {
                var turn = world.Next();

                // Every round settles, because the world names something every time. A walk
                // that only settled at the end would be a recital with extra steps.
                Assert.NotNull(turn.Outcome);

                // And it asks no question, which is the difference between exploring and
                // being examined. The survey is a later item and does not exist yet.
                Assert.Null(turn.Seen.Asked);

                Assert.NotNull(turn.Seen.Things);
                Assert.NotNull(turn.Seen.Statements);

                widest = Math.Max(widest, turn.Seen.Statements.Count);
                deepest = Math.Max(deepest, turn.Seen.Things.Max(one => one.Codes.Count));

                // The word it named is a word of the house, and it is one the machine could
                // see: a name for something in another room would be a settlement nothing in
                // the moment could ever have been about.
                Assert.NotNull(world.Meaning(turn.Outcome.Value));
            }

            output.WriteLine(
                $"widest moment {widest} sightings | most codes a thing {deepest}");

            // A thing shows its look and its shade, and its word once it has been named.
            // Three, because a look is never a name here: what a thing is called has to
            // be joined to what it looks like, which is the crossing a picture will pose.
            Assert.Equal(3, deepest);
        }

        // And a scope over ONE of the things is minted where genesis is allowed to run, which
        // is the mechanism under `a thing is one thing` and the reading this world was built
        // to take. Two arms, because the shipped gate is what decides it.
        var bound = new Dictionary<Surprising, int>();
        var held = new Dictionary<Surprising, int>();
        var crossed = new Dictionary<Surprising, int>();

        foreach (var surprising in new[] { Surprising.Unaccounted, Surprising.AnyFailure })
        {
            var walked = new Roaming(World(20, people: 2), seed: 4);

            var brain = new Brain(
                new CommittingSettings { Capacity = 4_000, Surprising = surprising },
                seed: 1);

            var tally = new Bench(
                new Watching<Coded>(
                    walked, new Joined(Joining.Bagged), acting: Chooses.From(_ => null)),
                brain)
                .Run(2_000, sweep: 500, target: 0.9, window: 500);

            // A look and a word in one scope, which is the crossing being made rather than
            // handed over. Forty-six is the modality this world's words ride on and
            // forty-eight is what a thing looks like -- and a look is its KIND's now, so
            // this counts a crossing from a look several things share to one thing's name.
            crossed[surprising] = brain.Held.All.Count(one => one.Scope.Length == 2
                && one.Scope.Any(code => code.Modality == 48)
                && one.Scope.Any(code => code.Modality == 46));

            // What was BUILT rather than what survived, which is the distinction this
            // reading was written on the wrong side of. Both are printed: a mechanism that
            // mints and is then deleted and a mechanism that never runs are opposite
            // diagnoses, and a held count alone cannot tell them apart.
            bound[surprising] = (int)brain.Held.EverBorn.GetValueOrDefault(Birth.Bound);
            held[surprising] = brain.Held.Births.Values.Count(one => one == Birth.Bound);

            output.WriteLine(
                $"{surprising,-12}| held {tally.Resident} | crossed {crossed[surprising]} "
                + $"| bound {bound[surprising]} minted, {held[surprising]} still held");
        }

        // A look and its word do come to sit in one scope under either gate, which is what
        // says the crossing is made rather than handed over.
        Assert.All(crossed.Values, one => Assert.True(one > 0));

        // A scope over ONE of the things is minted under the SHIPPED gate, which is the
        // mechanism under `a thing is one thing` running on a spine world. Asserted on the
        // shipped arm alone, because a mechanism that only runs ungated is a mechanism the
        // machine does not have.
        Assert.True(bound[Surprising.Unaccounted] > 0,
            "the shipped gate minted no scope over one thing, so `Spanning`'s generate half "
            + "does not run on the spine and the reading above needs re-taking");

        // And one HOLDS ITS SEAT under the shipped gate, which is the capability the
        // architecture line asks for and what a look per thing made impossible: subsumption
        // weighs a binding against the roots genesis minted it beside, and a pair that fires
        // exactly where its own look fires says nothing extra and correctly goes.
        Assert.True(held[Surprising.Unaccounted] > 0,
            $"{bound[Surprising.Unaccounted]} bindings minted and none survived, so a scope "
            + "over one thing still says nothing its parts do not and two things of a kind "
            + "are not what a binding needed");

        // And subsumption still takes most of them, which is the constraint working. A run
        // where it stopped deleting them would be the population drifting to a rule per
        // instance, which is the memorising this design is otherwise careful about.
        Assert.All(bound.Keys, one => Assert.True(held[one] < bound[one],
            $"{one}: {held[one]} of {bound[one]} bindings survived, so subsumption has "
            + "stopped deleting them and what a held count means here has changed"));
    }

    /// <summary>
    /// <b>A walked house is sat down and asked several verifiable things.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What makes an exam of a walk</b>, and it is the difference between a machine that
    /// was settled step by step and one that has to have kept something. The walk's own
    /// settlement is a naming of what is in front of it; the survey's questions are about a
    /// house it has finished walking and can no longer see.
    /// </para>
    /// <para>
    /// <b>Three kinds, asserted as three rather than as which</b>, because a prediction
    /// written into a wiring check fails two ways and reads the same. What matters is that no
    /// single rule answers the whole exam.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_walked_house_is_surveyed_on_what_was_in_it()
    {
        const int Steps = 20;
        const int Asked = 5;

        var world = new Roaming(
            World(Steps, people: 2, asked: Asked), seed: 4);

        var words = new Dictionary<Code, string>();

        for (var one = 0; one < world.Vocabulary.Count; one++)
            words[world.Meaning(one)!.Value] = world.Vocabulary[one];

        var kinds = new HashSet<string>(StringComparer.Ordinal);
        var walked = 0;
        var asked = 0;
        var exams = 0;

        for (var round = 0; round < (Steps + Asked) * 20; round++)
        {
            var turn = world.Next();

            // Every round settles under both halves. The walk names what is in front of the
            // machine and the survey answers with a word of the house, so a round that
            // settled on nothing would be a question with no answer.
            Assert.NotNull(turn.Outcome);
            Assert.NotNull(world.Meaning(turn.Outcome.Value));

            if (turn.Seen.Asked is not { } question)
            {
                walked++;

                continue;
            }

            asked++;

            kinds.Add(words[question.Codes[0]]);

            world.Do(0);

            // Deaf while the exam runs, because the walk is over. The last question ends the
            // exam and the house with it, so a word said after that one is the first word of
            // the next walk rather than a word about a house that is gone.
            if (asked % Asked == 0) exams++;
            else Assert.False(world.Listening);
        }

        output.WriteLine(
            $"{walked} steps walked, {asked} questions over {exams} exams, "
            + $"kinds {string.Join(", ", kinds.Order(StringComparer.Ordinal))}");

        // The exam is the length it was asked for, house after house, and the walk is the
        // length it was asked for too. A survey that ran short would be a house nobody could
        // be asked about reading as an exam that happened.
        Assert.Equal(20, exams);
        Assert.Equal(Asked * 20, asked);
        Assert.Equal(Steps * 20, walked);

        Assert.Equal(4, kinds.Count);
    }

    /// <summary>
    /// <b>A spoken step is taken where it can be, and marked.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What the machine does is a fact about the moment</b>, so its own words ride in the
    /// sighting and are marked as its doing. A world that took the words and reported the
    /// consequence as if nobody had spoken would leave a scope unable to name what it did.
    /// </para>
    /// <para>
    /// <b>And a wish the house cannot grant does nothing.</b>
    /// Substituting the nearest possible action would make a chooser's arm the world's own
    /// draw wearing the chooser's name, which is a fallback arm nobody meant to run.
    /// </para>
    /// <para>
    /// <b>One person, so nothing else moves.</b> A second walker takes a step of its own each
    /// round, and then a room that changed would say nothing about whose doing changed it.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_spoken_step_is_taken_where_it_can_be_and_marked_either_way()
    {
        var world = new Roaming(World(20, people: 1), seed: 1);
        var alphabet = world.Vocabulary.ToList();

        // Where the body is standing, read off the room's own look. A room is the first thing
        // in front of whoever is in it, so the first look of the newest sighting is the room.
        static Code Standing(Coded seen) =>
            seen.Statements![0].Codes.First(one => one.Modality == 48);

        // Declining leaves the world drawing its own step, and marks nothing.
        world.Do(null);

        Assert.Null(world.Next().Seen.Assigned);

        var before = Standing(world.Now);

        var elsewhere = new[] { "kitchen", "garden", "office", "bathroom" }
            .First(room => Kinds.Named(48, room) != before);

        // A verb with nothing to be about is not yet a command, which is what a command being
        // several words costs.
        world.Do(alphabet.IndexOf("went"));

        Assert.True(world.Listening);

        world.Do(alphabet.IndexOf(elsewhere));

        Assert.False(world.Listening);

        var moved = world.Next();

        // The machine is shown the room its own words put it in, and the words are marked as
        // its doing rather than as something the world said.
        Assert.NotNull(moved.Seen.Assigned);
        Assert.Equal(Kinds.Named(48, elsewhere), Standing(moved.Seen));

        // And a wish the house cannot grant spends the step and moves nothing. Nothing is
        // being carried after a walk to another room, so putting a thing down is impossible.
        world.Do(alphabet.IndexOf("dropped"));
        world.Do(alphabet.IndexOf("apple"));

        var waited = world.Next();

        Assert.NotNull(waited.Seen.Assigned);
        Assert.Equal(Kinds.Named(48, elsewhere), Standing(waited.Seen));

        output.WriteLine(
            $"walked to the {elsewhere} and stayed there through a wish it could not grant");
    }

    /// <summary>
    /// <b>A person talks to the walked house once its exam is over.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The last phase, and it is where the machine OBTAINS a settlement.</b> The walk
    /// settles by ostension, which is the world choosing what to name; here the machine asks
    /// and somebody answers, and what it may ask for is exactly what the exam asked about.
    /// </para>
    /// <para>
    /// <b>What settles the round is what the PERSON said</b>, which is the whole of the
    /// check. The house knows where the apple is and never says; a world that answered from
    /// its own state would be the experimenter supplying what the machine should go and get,
    /// and the answer here is a word this house was never going to choose.
    /// </para>
    /// <para>
    /// <b>And after the exam rather than before it</b>, because an answer given in the
    /// conversation joins the transcript. An exam that followed one would be asking a
    /// question whose answer is the most recent statement, which is recency wearing a
    /// conversation's clothes.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_person_talks_to_the_walked_house_after_its_exam()
    {
        const int Steps = 20;
        const int Asked = 5;
        const int Chatting = 5;

        // Two lines a round where a question is put -- one the person volunteers, one the
        // answer -- and `cellar` is the answer to everything so that a settlement matching it
        // cannot have come from the house.
        var person = new Person(says: [string.Empty], answers: ["cellar"]);

        var world = new Roaming(
            World(Steps, people: 2, asked: Asked, chatting: Chatting, person), seed: 4);

        var alphabet = world.Vocabulary.ToList();

        for (var step = 0; step < Steps; step++)
        {
            // The walk asks nothing, which is the difference between exploring and being
            // spoken to.
            Assert.Null(world.Now.Asked);

            world.Do(null);
            world.Next();
        }

        // The exam comes first and nobody has been spoken to yet.
        for (var one = 0; one < Asked; one++)
        {
            Assert.NotNull(world.Now.Asked);

            world.Do(null);
            world.Next();
        }

        Assert.Empty(person.Replied);

        // And the conversation opens with the world's turn, so a phase the signal did not
        // mark would be one nothing outside the world could tell had started.
        var opener = world.Now.Asked;

        Assert.NotNull(opener);

        Assert.Equal(
            [
                world.Meaning(alphabet.IndexOf("what"))!.Value,
                world.Meaning(alphabet.IndexOf("next"))!.Value,
            ],
            opener.Value.Codes);

        world.Do(alphabet.IndexOf("where"));

        // Still listening, because a verb with nothing to be about is no question.
        Assert.True(world.Listening);

        world.Do(alphabet.IndexOf("apple"));

        Assert.False(world.Listening);

        var turn = world.Next();

        Assert.NotNull(turn.Outcome);

        // The person was asked in words, and what settled the round is the word they said.
        Assert.Equal(["where apple"], person.Asked);
        Assert.Equal("cellar", world.Vocabulary[turn.Outcome.Value]);
        Assert.Equal(1, world.Answered);

        // And it is a word this house does not have -- six rooms stop at the hallway -- so
        // the settlement cannot have come from the house's own state under any reading.
        Assert.DoesNotContain("cellar", alphabet);

        // And what they said joined the transcript, or the machine asked and kept nothing.
        var cellar = world.Meaning(turn.Outcome.Value)!.Value;

        Assert.Contains(world.Now.Statements!, one => one.Codes.Contains(cellar));

        // A round it says nothing in settles on nothing, so a conversation the machine has no
        // question for costs a commitment exactly nothing.
        world.Do(null);

        Assert.Null(world.Next().Outcome);

        // And a person who leaves ends the conversation rather than the run. The house is
        // dropped, so what follows is a fresh walk.
        person.Leaving = true;

        world.Do(null);
        world.Next();

        Assert.True(world.Ended);
        Assert.Null(world.Now.Asked);

        output.WriteLine(
            $"{Chatting} rounds offered, answered `{world.Vocabulary[turn.Outcome.Value]}` by "
            + $"the person after {Asked} exam questions");
    }

    /// <summary>A chooser that asks where each thing ended up and says nothing else.</summary>
    /// <param name="opener">The code the world's own turn carries, so the walk is left alone.</param>
    /// <param name="where">The word that opens the question.</param>
    /// <param name="things">The words for the things, in turn.</param>
    /// <remarks>
    /// <b>The chat's CEILING rather than a chooser worth shipping.</b> What is wanted first is
    /// what a conversation is worth to a machine that asks perfectly, which costs one run and
    /// bounds everything a chooser that has to LEARN to ask could reach. <c>Curiosity</c>
    /// cannot form one of these questions today — its blind draw is over the words in the
    /// moment, and no sighting holds <i>where</i>.
    /// </remarks>
    private sealed class Asks(Code opener, int where, IReadOnlyList<int> things) : IChooses
    {
        private int _said;
        private int _turn;

        public int? Choose(IReadOnlyCollection<Code> felt)
        {
            ArgumentNullException.ThrowIfNull(felt);

            // Quiet unless the world has taken its turn, so the walk is the walk the control
            // takes and the arms differ in the conversation alone.
            if (!felt.Contains(opener)) return null;

            return _said++ switch
            {
                0 => where,
                1 => things[_turn++ % things.Count],
                _ => null,
            };
        }

        public void Cleared() => _said = 0;
    }

    /// <summary>
    /// <b>What decides the words the machine says about the house.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Fork 146's drive on the spine world, and fork 151's first reading.</b>
    /// <c>Commitment.Progress</c> rises while a rule is being learnt and lets go both when the
    /// rule is mastered and when the channel is noise, and <c>Wanting.Learning</c> ranks what
    /// to say by it. Nothing had ever turned the arm on.
    /// </para>
    /// <para>
    /// <b>Against a machine drawing its words uniformly</b>, which is the same fallback the
    /// drive falls back to. So an arm that never advocated anything would read as its control
    /// exactly, and <c>Drives.Told</c> is counted so that cannot pass unseen.
    /// </para>
    /// <para>
    /// <b>What the conversation makes askable is a whole question.</b> A scope holding one
    /// word and a look advocates that word, so <i>where</i> and the thing come from two
    /// commitments and the moment's budget puts them in one sentence — which is why nothing
    /// here needs a scope to name a command.
    /// </para>
    /// <para>
    /// <b>The conversation is after the exam and the answerer is a person</b>, so the two
    /// columns answer two questions rather than one. The exam is what the WALK taught and
    /// nothing said afterwards can reach it; how many askable questions the machine put is
    /// what the drive is read on, and the person answering is what settles them.
    /// </para>
    /// <para>
    /// <b>And the arm that says NOTHING is the control.</b> Every other row needs it. Saying
    /// nothing is not standing still: the world draws the body's step where the machine
    /// names none, so <c>declining</c> moves every round and speaks never. Without it a
    /// chooser that walks badly and a chooser whose words leave the body waiting are the
    /// same low score.
    /// </para>
    /// <para>
    /// <b>And it WINS, which is the finding.</b> 0.337 saying nothing, against 0.250 under
    /// the drive, 0.233 saying what it believes and 0.119 drawing uniformly — and the
    /// silent arm is the one whose body moved most, because the world walks it. So moving
    /// does not cost the exam and SPEAKING does: every word the machine says joins the
    /// moment as a code that predicts nothing, and the more it says the worse it reads.
    /// </para>
    /// <para>
    /// <b>What it costs is the WIDTH of the moment it makes.</b> Silence hands the brain
    /// 21.7 codes a moment, the drive 24.5, the belief 24.1 and a uniform talker 45.5 —
    /// twice as wide for a third of the score. A moment is a SET, so a chooser that repeats
    /// itself barely widens one and a chooser saying six distinct words every round doubles
    /// it.
    /// </para>
    /// <para>
    /// <b>Which is the whole of what the drive was buying.</b> <c>narrow</c> says ONE
    /// uniformly drawn word a moment and reads 0.250, the same 135 of 540 the drive reads,
    /// leading it on one seed and trailing on two. So <c>Wanting.Learning</c> is level with
    /// a coin at six words a moment, and every earlier reading that had it ahead was
    /// against a six-word talker — a control far worse than a one-word one, which is this
    /// repo's own <i>a fallback is a control arm nobody meant to run</i> arriving on the
    /// chooser.
    /// </para>
    /// <para>
    /// <b>And <c>narrow</c> ties it while never once commanding.</b> One word cannot be a
    /// verb and a thing, so its body waited every step where the drive's moved 207 times.
    /// Two arms that differ in whether the machine walks at all and score the same say the
    /// walk is not what the exam is reading.
    /// </para>
    /// <para>
    /// <b>The drive on a BUDGET of one word wins.</b> <c>sparing</c> reads
    /// 0.369 at 14.4 codes a moment — narrower than silence, because a body that waits sees
    /// one room over and over where a body the world walks meets the whole house. It is
    /// 1.1 standard errors ahead of saying nothing, so the two are level and everything
    /// that talks more is behind them.
    /// </para>
    /// <para>
    /// <b>So the exam reads the moment's WIDTH and little else.</b> 14.4 reads
    /// 0.369, 21.7 reads 0.337, 24.1 and 24.5 read 0.233 and 0.250, 32.3 reads 0.250 and
    /// 45.5 reads 0.119. Which words, whether the machine walks, whether it commands and
    /// what it wants all sit inside that ordering rather than across it.
    /// </para>
    /// <para>
    /// <b>And it is not the machine's own words that cost</b>, which <c>sparing</c> settles:
    /// it says one every round and has the narrowest moments of all, where the silent arm
    /// says none and has wider ones. What costs is the count of DISTINCT codes the brain is
    /// handed, whoever put them there.
    /// </para>
    /// <para>
    /// <b>So the best machine here stands in a corner repeating itself</b>,
    /// and that is a reductio rather than a result. What it prices is the brain against wide
    /// moments rather than the chooser against the house, and no comparison between two
    /// choosers on this world means anything until the width is held.
    /// </para>
    /// <para>
    /// <b>And the exam is the same exam under every arm</b>, which is what makes that
    /// readable. All four are asked about the same fourteen or fifteen distinct answers
    /// across all six rooms, so no arm was examined on a narrower house than another —
    /// which was the first explanation and it is refuted.
    /// </para>
    /// <para>
    /// <b>A cost to record rather than grounds to stop acting.</b> The silent arm obtains
    /// nothing: it never asks, so it never has a settlement it went and got, and it cannot
    /// be talked to at all. What the number prices is the acting channel, and every reading
    /// on this world that compared two choosers without it read a cost as a difference
    /// between them.
    /// </para>
    /// <para>
    /// <b>And the fourth arm says what it BELIEVES first.</b> <see cref="Answers"/> is a
    /// requirement rather than an arm — a machine nobody can ask anything is not something
    /// a person can talk to — so what this reads is its COST rather than whether it wins.
    /// The belief and the drive answer different questions and there is no scale on which
    /// they trade, so the belief goes first and the drive takes the rounds it has nothing
    /// to say about.
    /// </para>
    /// <para>
    /// <b>And the cost is small.</b> Thirty houses a seed over three seeds: the exam reads
    /// 0.119 under the draw, 0.250 under the drive and 0.233 under the belief, and the
    /// belief is behind on two seeds and ahead on one — 0.244 against 0.239, 0.328 against
    /// 0.350, and 0.128 against 0.161. Three seeds cannot tell those apart.
    /// </para>
    /// <para>
    /// <b>And it was read as FREE on the three-kind exam</b>, which is the correction worth
    /// keeping rather than the number. That exam read 0.283 against 0.278 with the belief
    /// ahead on two seeds of three; adding the question no transcript states moved the
    /// order. A reading is conditional on the exam that produced it as much as on the brain.
    /// </para>
    /// <para>
    /// <b>What it buys is a third again as many askable questions</b>, 242 of 540
    /// conversation rounds against the drive's 184 and the draw's 145. A belief is not a
    /// question, so an arm spending its budget answering could have crowded the asking out
    /// and did the opposite.
    /// </para>
    /// <para>
    /// <b>It believes about a fifth of what it says</b> — 1,503, 1,500 and 1,505 beliefs
    /// against some 8,000 doings a seed, the rest falling to the drive. A count that moved
    /// is what separates an arm that bit and changed nothing from an arm nothing reached,
    /// and no score here could tell those apart.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task What_decides_the_words_the_machine_says_about_the_house()
    {
        const int Houses = 30;
        const int Steps = 40;
        const int Asked = 6;
        const int Chatting = 6;
        const int Seeds = 3;

        // Somebody who answers in room words and shrugs one time in five. The answers are
        // theirs rather than the house's, so what a settlement teaches here is what a
        // person said -- which is the whole of what the answerer being a person means.
        string[] replies = ["kitchen", "garden", "office", "bathroom", string.Empty];

        output.WriteLine(
            $"{Houses} houses a seed over {Seeds} seeds, {Steps} steps and {Asked} asked");

        output.WriteLine(
            $"{"spoken",-8}{"wanting",-10}{"seed",-6}{"asked",8}{"right",8}{"score",9}");

        var scored = new Dictionary<(string Arm, string Kind), (int Asked, int Right)>();

        var questions = new Dictionary<string, (int Talked, int Spoke)>(StringComparer.Ordinal);
        var perSeed = new Dictionary<(string Arm, int Seed), double>();
        var advocated = 0L;

        // Commands the machine managed and commands the house carried out, per arm. The
        // column exists because a chooser that never says a verb reads exactly like one
        // that explores badly: both are a machine standing in one room, and the exam
        // cannot tell them apart.
        var commanded = new Dictionary<string, (long Ordered, long Did)>(StringComparer.Ordinal);

        // How many DISTINCT things each arm was examined about, and how many rooms it
        // stood in. A question is only asked about a thing the machine has been given a
        // word for, so an arm that walked further is examined on a wider set it knows less
        // well -- which would make the exam column a comparison between two exams as much
        // as between two choosers.
        var about = new Dictionary<string, HashSet<Code>>(StringComparer.Ordinal);
        var stood = new Dictionary<string, HashSet<Code>>(StringComparer.Ordinal);

        // How big a moment the arm hands the brain, summed and counted. The machine's own
        // words ride IN the moment so a chooser that says six of them adds six codes and the
        // derived doing beside each -- and a population spending its capacity on codes that
        // predict nothing is the leading explanation for why speaking costs the exam.
        var wide = new Dictionary<string, (long Codes, long Moments)>(StringComparer.Ordinal);

        var believed = 0L;

        foreach (var arm in new[]
        {
            "declining", "uniform", "narrow", "learning", "sparing", "believing",
        })
        foreach (var seed in Enumerable.Range(1, Seeds))
        {
            // One drawn word a moment rather than six, which is the control that separates
            // the drive saying BETTER words from the drive saying FEWER of them. A moment is
            // a set and the drive repeats itself, so its moments are barely wider than
            // silence while a uniform talker says six distinct words every round.
            var once = false;
            var budget = 0;

            var rounds = Houses * (Steps + Chatting + Asked);

            var house = new Roaming(
                World(
                    Steps,
                    people: 2,
                    asked: Asked,
                    chatting: Chatting,
                    new Person(answers: replies)),
                seed);

            var words = Named(house);
            var world = new Sitting(house, null);
            var brain = new Brain(new CommittingSettings { Capacity = 2_000 }, seed);
            var draw = new Random(seed);
            var drives = Wanting(brain, house, draw);

            // Handed in where the world and the brain meet, because which code an outcome
            // is about is a fact only the world holds. Without it the belief has no word
            // to be said as and the third arm is its own fallback.
            brain.Meaning = house.Meaning;

            var answers = new Answers(
                brain,
                saying: expects => Brain.Meant(expects) is { } word
                    && word < house.Doings
                        ? word
                        : null,
                otherwise: Chooses.From(drives.Choose, drives.Cleared));

            var watching = new Watching<Coded>(
                world,
                new Joined(Joining.Bagged),
                acting: arm switch
                {
                    "learning" => Chooses.From(drives.Choose, drives.Cleared),

                    // The drive on a budget of ONE word a moment, which is what separates a
                    // chooser that picks better words from one that picks fewer. `narrow`
                    // says one word drawn uniformly, so the pair differ in the pick and in
                    // nothing else.
                    "sparing" => Chooses.From(
                        felt =>
                        {
                            if (budget > 0) return null;

                            budget++;

                            return drives.Choose(felt);
                        },
                        () =>
                        {
                            budget = 0;

                            drives.Cleared();
                        }),
                    "believing" => answers,

                    // Saying nothing is not standing still: the world draws the body's step
                    // where the machine names none, so this arm MOVES every round and says
                    // nothing. It is what separates a chooser that walks badly from one whose
                    // words leave the body waiting.
                    "declining" => Chooses.From(_ => null),
                    "narrow" => Chooses.From(
                        _ =>
                        {
                            if (once) return null;

                            once = true;

                            return draw.Next(house.Doings);
                        },
                        () => once = false),
                    _ => Chooses.From(_ => draw.Next(house.Doings)),
                });

            var loop = new Round(brain, rounds, sweep: 500, target: 0.9, window: 500);

            var asked = new Dictionary<string, int>(StringComparer.Ordinal);
            var right = new Dictionary<string, int>(StringComparer.Ordinal);

            var talked = 0;
            var spoke = 0;

            for (var round = 0; round < rounds; round++)
            {
                if (watching.Push() is not { } pushed) continue;

                var was = loop.Right;

                await loop.StepAsync(pushed);

                if (world.Talked)
                {
                    // A conversation round the world could answer is a question the machine
                    // managed to PUT, which is the whole of what fork 151 asks. A round that
                    // settled on nothing is a round it said nothing askable in.
                    spoke += pushed.Followed is null ? 0 : 1;
                    talked++;

                    continue;
                }

                Marked(world, words, asked, right, loop.Right > was);

                // What the exam was ABOUT, which is the answer it was scored against, and
                // which room the body was standing in. Both on `Roaming.Named`'s standing
                // and nothing that learns is shown either.
                if (world.Asking is not null && pushed.Followed is { } answer)
                {
                    if (!about.TryGetValue(arm, out var seen)) about[arm] = seen = [];

                    seen.Add(answer);
                }

                if (house.Standing is { } room)
                {
                    if (!stood.TryGetValue(arm, out var rooms)) stood[arm] = rooms = [];

                    rooms.Add(room);
                }

                var width = wide.GetValueOrDefault(arm);

                wide[arm] = (width.Codes + pushed.Codes.Count, width.Moments + 1);
            }

            foreach (var kind in asked.Keys)
            {
                var had = scored.GetValueOrDefault((arm, kind));

                scored[(arm, kind)] =
                    (had.Asked + asked[kind], had.Right + right.GetValueOrDefault(kind));
            }

            var before = questions.GetValueOrDefault(arm);

            questions[arm] = (before.Talked + talked, before.Spoke + spoke);

            var ordered = commanded.GetValueOrDefault(arm);

            commanded[arm] =
                (ordered.Ordered + house.Ordered, ordered.Did + house.Did);

            // Per seed, so the direction can be COUNTED rather than read off a total one
            // seed could have carried on its own.
            var here = right.Values.Sum() / (double)asked.Values.Sum();

            perSeed[(arm, seed)] = here;

            output.WriteLine(
                $"{Chatting,-8}{arm,-10}{seed,-6}{asked.Values.Sum(),8}"
                + $"{right.Values.Sum(),8}{here,9:F3}"
                + $"   spoke {spoke} of {talked}"
                + (arm is "learning" or "sparing"
                    ? $", drive named {drives.Told} and the draw {drives.Untold}"
                    : string.Empty)
                + (arm == "believing"
                    ? $", believed {answers.Said} and had nothing {answers.Quiet}"
                    : string.Empty));

            if (arm is "learning" or "sparing") advocated += drives.Told;
            if (arm == "believing") believed += answers.Said;
        }

        foreach (var arm in new[]
        {
            "declining", "uniform", "narrow", "learning", "sparing", "believing",
        })
        {
            var rows = scored.Where(one => one.Key.Arm == arm).ToList();

            var put = rows.Sum(one => one.Value.Asked);
            var hit = rows.Sum(one => one.Value.Right);

            output.WriteLine(
                $"{Chatting,-8}{arm,-10}{"all",-6}{put,8}{hit,8}{hit / (double)put,9:F3}"
                + $"   spoke {questions[arm].Spoke} of {questions[arm].Talked}"
                + $", commanded {commanded[arm].Ordered} and moved {commanded[arm].Did}"
                + $", asked about {about.GetValueOrDefault(arm)?.Count ?? 0} distinct "
                + $"answers across {stood.GetValueOrDefault(arm)?.Count ?? 0} rooms stood in"
                + $", moments {wide[arm].Codes / (double)wide[arm].Moments:F1} codes wide");
        }

        var leads = Enumerable.Range(1, Seeds).Count(seed =>
            perSeed[("learning", seed)] > perSeed[("uniform", seed)]);

        // Counted in BOTH directions, because a small sample hides a real effect as readily
        // as it invents one and a count one way reads as a verdict either way.
        var over = Enumerable.Range(1, Seeds).Count(seed =>
            perSeed[("believing", seed)] > perSeed[("learning", seed)]);

        // Level counted apart from behind, because a tie is not a loss and a count that
        // read it as one would report a direction nothing showed.
        var level = Enumerable.Range(1, Seeds)
            .Count(seed => perSeed[("believing", seed)] == perSeed[("learning", seed)]);

        output.WriteLine(
            $"the drive leads the draw on {leads} seeds of {Seeds}, and saying what it "
            + $"believes leads the drive on {over}, is level on {level} and trails on "
            + $"{Seeds - over - level}");

        // Every house's exam was sat under every arm, whatever the walk before it looked
        // like. Which KINDS got asked is a fact about where the machine ended up walking and
        // is not the same number under four choosers.
        Assert.Equal(6, questions.Count);

        Assert.All(
            questions.Keys,
            one => Assert.Equal(
                Houses * Asked * Seeds,
                scored.Where(row => row.Key.Arm == one).Sum(row => row.Value.Asked)));

        // The population advocated a word on rounds of its own, or the drive was its own
        // fallback all run and this table is the control printed twice. A fallback is a
        // control arm nobody meant to run, and silence drifts an arm toward the random bar
        // for free.
        Assert.True(advocated > 0,
            "`Wanting.Learning` never once named the word: every round was decided by the "
            + "uniform draw it falls back to, so the two arms here are one arm and nothing "
            + "in the table is about the drive");

        // And the conversation ran, or the asking column is about a phase that never
        // happened. Every house offers its rounds whether or not the machine takes them up.
        Assert.All(questions.Values, one => Assert.True(one.Talked > 0));

        // Every arm walked SOMEWHERE of its own accord, or the column above is about a
        // machine that stood in the room it was dropped in and the exam is scored on a
        // house it watched rather than explored. Asserted per arm because the arms differ
        // in exactly the thing that decides it -- which word gets said.
        Assert.All(
            commanded.Where(one => one.Key is not ("declining" or "narrow" or "sparing")),
            one => Assert.True(one.Value.Did > 0,
                $"the `{one.Key}` arm carried out no command at all across "
                + $"{Houses * Seeds} houses, so the machine never once moved itself and "
                + "every score under it is about a walk somebody else took"));

        // The belief was SAID, or the third arm is the second one under a second name and
        // every row of it is the drive's printed twice. A chooser that believed nothing
        // all run reads exactly like one whose belief never differed, and no score here
        // can tell those apart.
        Assert.True(believed > 0,
            "`Answers` never once had a belief about the moment in front of it, so the "
            + "`believing` arm was `Drives` all run and the two rows are one row. The "
            + "usual cause is `Brain.Meaning` not being handed in, which leaves every "
            + "expectation with no word to be said as.");

        // And it CHANGED what the machine said, which is the half a count of beliefs
        // cannot give. A chooser whose belief was always the word the drive would have
        // picked anyway is the drive under a second name, and every row of it is the
        // second row printed twice. Asserted as a DIFFERENCE rather than a direction: a
        // requirement is not an arm, so which way this goes is a cost to record.
        Assert.True(
            Enumerable.Range(1, Seeds).Any(seed =>
                perSeed[("believing", seed)] != perSeed[("learning", seed)])
            || questions["believing"] != questions["learning"],
            "the `believing` arm scored and spoke exactly as `learning` did on every seed, "
            + "so saying what it believes never once changed what it said and the two rows "
            + "are one row. Either the belief is always the drive's own pick or the "
            + "chooser is not reaching the world.");
    }

    /// <summary>
    /// <b>What ending the walk when the machine has had enough costs.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Fork 152, and it is a REQUIREMENT rather than an arm.</b> John's: how long somebody
    /// stays in a house is decided by whoever is walking it, so a walk whose length is a
    /// number the experimenter set is the target world carrying a decision that is not its
    /// own. <c>Steps</c> is a cap now and <c>Drives.Sated</c> is what ends it sooner.
    /// </para>
    /// <para>
    /// <b>The want has to have RISEN before it can be flat.</b>
    /// <c>Commitment.Progress</c> is nought for a rule that has been mastered and nought for
    /// one nobody has learnt yet, so a machine that had just arrived would read as finished —
    /// which would shorten the walk to suit the brain, and the target world is the one place
    /// that may never happen. A machine whose want never goes positive walks to the cap.
    /// </para>
    /// <para>
    /// <b>Every arm gets the same ROUNDS rather than the same houses</b>, because that is
    /// what the trade is. A walk that ends early spends the rest of its budget on another
    /// house, so what is being asked is whether more houses seen less thoroughly is worth
    /// more or less than fewer seen to a cap.
    /// </para>
    /// <para>
    /// <b>And the third arm is the control the other two need.</b> A shorter walk is an
    /// EASIER exam — every step is a chance for the truth to move while the sentence that
    /// stated the old one is still in view — so ending early raises the score for a reason
    /// that has nothing to do with knowing when to stop. <c>matched</c> is the plain cap set
    /// to the length the sated arm actually walked, per seed, so what is left between them
    /// is the CHOICE of when to leave rather than the leaving.
    /// </para>
    /// <para>
    /// <b>And the check that can FAIL is that it fired.</b> An arm that never once ended a
    /// walk early is the cap wearing a second name, and every column of it is the control
    /// printed twice — which no score here could tell apart.
    /// </para>
    /// <para>
    /// <b>Eight thousand rounds a seed over three seeds:</b> 0.332 sated, 0.274 against the
    /// full cap and 0.330 against the matched one, and the sated arm walked 25, 24 and 26
    /// steps of a cap of 40. So the whole of what ending early buys is the shorter walk,
    /// and choosing WHEN to leave is worth nothing over being capped at the same length.
    /// </para>
    /// <para>
    /// <b>And it read the other way on the three-kind exam</b>, which is the correction
    /// worth keeping. There it was 0.353 against 0.293 and led the matched control on three
    /// seeds of three; adding the question no transcript states took the whole lead away.
    /// Three seeds cannot tell 0.332 from 0.330 either way — per seed it is 0.274 against
    /// 0.376, 0.417 against 0.305 and 0.300 against 0.309, which is a spread far wider than
    /// the difference. What would settle it is seeds, and a grid this size is a sweep's
    /// work rather than a suite's.
    /// </para>
    /// <para>
    /// <b>The requirement stands whichever way it goes.</b> A walk whose length is a number
    /// the experimenter set is the target world carrying a decision that is not its own, so
    /// a losing reading is a cost to record rather than grounds to put the count back.
    /// </para>
    /// <para>
    /// <b>Which is why the control came first.</b> It was taken before the number went
    /// down. A first pass had the two arms alone and read the whole gap as what ending
    /// early buys — and a third of it is the exam getting easier, because a walk with fewer
    /// steps in it has had fewer chances for the truth to move away from the sentence that
    /// stated it.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task What_ending_the_walk_when_the_machine_has_had_enough_costs()
    {
        const int Rounds = 8_000;
        const int Steps = 40;
        const int Asked = 6;
        const int Seeds = 3;

        output.WriteLine(
            $"{Rounds} rounds a seed over {Seeds} seeds, a cap of {Steps} steps and {Asked} "
            + "asked");

        output.WriteLine(
            $"{"ending",-9}{"seed",-6}{"asked",8}{"right",8}{"score",9}{"unwalked",10}");

        var scored = new Dictionary<(string Arm, string Kind), (int Asked, int Right)>();
        var perSeed = new Dictionary<(string Arm, int Seed), double>();
        var unwalked = new Dictionary<string, long>(StringComparer.Ordinal);

        // How long the sated arm walked, per seed, so the matched control can be set to it.
        // Sated runs first for that reason and for no other.
        var walked = new Dictionary<int, int>();

        foreach (var arm in new[] { "sated", "capped", "matched" })
        foreach (var seed in Enumerable.Range(1, Seeds))
        {
            Drives? drives = null;

            var house = new Roaming(
                World(
                    arm == "matched" ? walked[seed] : Steps,
                    people: 2,
                    asked: Asked) with
                {
                    Enough = arm == "sated" ? () => drives?.Sated == true : null,
                },
                seed);

            var words = Named(house);
            var world = new Sitting(house, null);
            var brain = new Brain(new CommittingSettings { Capacity = 2_000 }, seed);
            var draw = new Random(seed);

            drives = Wanting(brain, house, draw);

            var watching = new Watching<Coded>(
                world,
                new Joined(Joining.Bagged),
                acting: Chooses.From(drives.Choose, drives.Cleared));

            var loop = new Round(brain, Rounds, sweep: 500, target: 0.9, window: 500);

            var asked = new Dictionary<string, int>(StringComparer.Ordinal);
            var right = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var round = 0; round < Rounds; round++)
            {
                if (watching.Push() is not { } pushed) continue;

                var was = loop.Right;

                await loop.StepAsync(pushed);

                Marked(world, words, asked, right, loop.Right > was);
            }

            foreach (var kind in asked.Keys)
            {
                var had = scored.GetValueOrDefault((arm, kind));

                scored[(arm, kind)] =
                    (had.Asked + asked[kind], had.Right + right.GetValueOrDefault(kind));
            }

            unwalked[arm] = unwalked.GetValueOrDefault(arm) + house.Left;

            // The mean walk this seed actually took, which is what the matched control is
            // capped at. A house asks `Asked` questions, so the count of them says how
            // many houses were walked without the world having to report it.
            if (arm == "sated")
            {
                var houses = Math.Max(1, asked.Values.Sum() / Asked);

                walked[seed] = Math.Max(
                    1, (int)Math.Round(((houses * (double)Steps) - house.Left) / houses));
            }

            var here = right.Values.Sum() / (double)asked.Values.Sum();

            perSeed[(arm, seed)] = here;

            output.WriteLine(
                $"{arm,-9}{seed,-6}{asked.Values.Sum(),8}{right.Values.Sum(),8}{here,9:F3}"
                + $"{house.Left,10}"
                + (arm == "sated" ? $"   walked {walked[seed]} of {Steps}" : string.Empty));
        }

        foreach (var arm in new[] { "sated", "capped", "matched" })
        {
            var rows = scored.Where(one => one.Key.Arm == arm).ToList();

            var put = rows.Sum(one => one.Value.Asked);
            var hit = rows.Sum(one => one.Value.Right);

            output.WriteLine(
                $"{arm,-9}{"all",-6}{put,8}{hit,8}{hit / (double)put,9:F3}"
                + $"{unwalked[arm],10}");
        }

        // Counted in both directions, because a small sample hides a real effect as readily
        // as it invents one. The comparison that matters is against the MATCHED cap: the
        // full one differs in walk length as well as in who decided it.
        var over = Enumerable.Range(1, Seeds)
            .Count(seed => perSeed[("sated", seed)] > perSeed[("capped", seed)]);

        var apart = Enumerable.Range(1, Seeds)
            .Count(seed => perSeed[("sated", seed)] > perSeed[("matched", seed)]);

        output.WriteLine(
            $"ending early leads the full cap on {over} seeds of {Seeds} and trails on "
            + $"{Seeds - over}; against the matched cap it leads on {apart} and trails on "
            + $"{Seeds - apart}");

        // It FIRED, or the arm is the cap under a second name and every column of it is the
        // control printed twice. A want that never rose and a walk that never ended early are
        // the same unchanged table from outside.
        Assert.True(unwalked["sated"] > 0,
            "no walk ended before its cap in three runs of eight thousand rounds, so "
            + "`Drives.Sated` never once read true and this grid is the capped arm twice. "
            + "The want has to go positive before it can be flat, so the usual cause is a "
            + "population that never learnt anything rather than a rule that never fires.");

        // And the cap still bounds it, or a machine that has had enough of everything walks
        // no house at all and the exam is asked about a scatter it never saw.
        Assert.Equal(0, unwalked["capped"]);
        Assert.Equal(0, unwalked["matched"]);

        // And the matched control is genuinely shorter, or it is the full cap under a
        // third name and the separation this grid rests on was never taken.
        Assert.All(
            Enumerable.Range(1, Seeds),
            seed => Assert.True(walked[seed] < Steps,
                $"the sated arm walked {walked[seed]} steps of a {Steps} cap on seed "
                + $"{seed}, so the matched control is the full cap and nothing here "
                + "separates ending early from walking a shorter house"));
    }

    /// <summary>A walked house whose exam may be ANOTHER house's.</summary>
    /// <param name="walked">The house the machine walks and is settled by.</param>
    /// <param name="instead">The house whose questions it is asked, or nothing.</param>
    /// <remarks>
    /// <b>The control THE ORDER names, built by swapping the question.</b> A machine that walked another house sitting this exam and this machine
    /// sitting another house's exam are the same broken pairing, and the second is reachable
    /// without running two brains. The transcript, the things and the walk are the machine's
    /// own; only the question and its answer come from a house it never saw.
    /// </remarks>
    private sealed class Sitting(Roaming walked, Roaming? instead)
        : IWorld<Coded>, IActed<Coded>
    {
        /// <summary>The first code of the EXAM question just asked, or nothing otherwise.</summary>
        /// <remarks>
        /// <b>Read off the world's own channel rather than off the question's words</b>,
        /// because the world takes a turn in the conversation and that is a question too.
        /// </remarks>
        public Code? Asking { get; private set; }

        /// <summary>Whether the round just taken was one of the conversation's.</summary>
        public bool Talked { get; private set; }

        public int Outcomes => walked.Outcomes;

        public int Doings => walked.Doings;

        public bool Listening => walked.Listening;

        public Coded Now => walked.Now;

        public void Do(int? doing) => walked.Do(doing);

        public Turn<Coded> Next()
        {
            var turn = walked.Next();

            // Stepped in lock-step whether or not this round is a question, because two
            // houses of one size run their walks and their exams on the same rounds and a
            // world asked only sometimes would drift out of alignment by the second house.
            var other = instead?.Next();

            Asking = null;
            Talked = false;

            if (turn.Seen.Asked is not { } question) return turn;

            if (!walked.Sat)
            {
                Talked = true;

                return turn;
            }

            Asking = question.Codes[0];

            if (other is not { Seen.Asked: { } swapped } sat) return turn;

            Asking = swapped.Codes[0];

            return new Turn<Coded>
            {
                Seen = Coded.From(
                    turn.Seen.Statements!, swapped, things: turn.Seen.Things),
                Outcome = sat.Outcome,
            };
        }
    }

    /// <summary>
    /// <b>What a learner scores on the survey, against the wrong house's exam.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The control the survey was owed, and stronger than the marginal.</b>
    /// <c>CeilingTests</c> prices what a rule keyed on the question alone reaches; this pairs
    /// a real walk with another house's questions, so everything the machine holds is intact
    /// and only the pairing is broken. A survey the crossed arm answers as well as the paired
    /// one is measuring the alphabet rather than the walk.
    /// </para>
    /// <para>
    /// <b>Read per kind</b>, because the four do not have one ceiling between them. Counting
    /// is at the scope language's own bound and reads its marginal under both arms; where a
    /// thing ended up, what a room held and what WOULD be in one are what the walk can be
    /// about.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task What_a_learner_scores_on_the_survey_against_the_wrong_houses_exam()
    {
        // Four times what it was, and the exam gaining a fourth kind is why. Four kinds
        // over one budget is three quarters of the questions a row used to hold, and the
        // arms are separated by a hair over three standard errors at this size -- so the
        // gap stopped clearing the bar at six thousand for want of sample rather than
        // because the exam had stopped reading the walk. Twelve thousand reads 2.1 and
        // this reads 3.2, which is the square root the arithmetic predicts.
        const int Rounds = 24_000;
        const int Steps = 40;
        const int Asked = 6;

        var settings = World(Steps, people: 2, asked: Asked);

        var words = new Dictionary<Code, string>();

        var alphabet = new Roaming(settings, seed: 4);

        for (var one = 0; one < alphabet.Vocabulary.Count; one++)
            words[alphabet.Meaning(one)!.Value] = alphabet.Vocabulary[one];

        output.WriteLine(
            $"a walked house, {Rounds} rounds, {Steps} steps and {Asked} questions a house");

        output.WriteLine($"{"arm",-9}{"kind",-8}{"asked",8}{"right",8}{"silent",8}{"score",9}");

        var scored = new Dictionary<(string Arm, string Kind), (int Asked, int Right)>();

        foreach (var arm in new[] { "paired", "crossed" })
        {
            var world = new Sitting(
                new Roaming(settings, seed: 4),
                arm == "crossed" ? new Roaming(settings, seed: 9) : null);

            var brain = new Brain(
                new CommittingSettings { Capacity = 4_000 }, seed: 1);

            var watching = new Watching<Coded>(
                world, new Joined(Joining.Bagged), acting: Chooses.From(_ => null));

            var loop = new Round(brain, Rounds, sweep: 500, target: 0.9, window: 500);

            var asked = new Dictionary<string, int>(StringComparer.Ordinal);
            var right = new Dictionary<string, int>(StringComparer.Ordinal);
            var silent = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var round = 0; round < Rounds; round++)
            {
                if (watching.Push() is not { } pushed) continue;

                var was = (loop.Right, loop.Silent);

                await loop.StepAsync(pushed);

                // A walked step settles by ostension and is not the exam. What is read here
                // is the questions alone, which is the whole point of there being a survey.
                if (world.Asking is not { } code) continue;

                var kind = words[code];

                asked[kind] = asked.GetValueOrDefault(kind) + 1;

                if (loop.Right > was.Right) right[kind] = right.GetValueOrDefault(kind) + 1;
                if (loop.Silent > was.Silent) silent[kind] = silent.GetValueOrDefault(kind) + 1;
            }

            foreach (var kind in asked.Keys.Order(StringComparer.Ordinal))
            {
                var hits = right.GetValueOrDefault(kind);

                output.WriteLine(
                    $"{arm,-9}{kind,-8}{asked[kind],8}{hits,8}"
                    + $"{silent.GetValueOrDefault(kind),8}{hits / (double)asked[kind],9:F3}");

                scored[(arm, kind)] = (asked[kind], hits);
            }
        }

        // Four kinds under both arms, or the table is a verdict on which questions got asked
        // rather than on the machine.
        Assert.Equal(8, scored.Count);

        Assert.All(scored.Values, one => Assert.True(one.Asked > 100));

        // The crossed arm may not beat the exam's own marginal by anything a sample this size
        // could not produce. It reads a transcript that says nothing about the house it is
        // being asked about, so an arm above the commonest answer means the QUESTION is
        // carrying its own answer and the exam is measuring the alphabet.
        foreach (var kind in new[] { "how", "what", "where" })
        {
            var (count, hits) = scored[("crossed", kind)];

            // The marginal `CeilingTests` measured on this exam, taken there before anything
            // learnt and read here rather than recomputed, so the two cannot drift apart.
            var marginal = kind switch { "how" => 0.579, "what" => 0.319, _ => 0.252 };

            var error = Math.Sqrt(marginal * (1.0 - marginal) / count);

            Assert.True(hits / (double)count < marginal + (3.0 * error),
                $"the crossed arm reached {hits / (double)count:F3} on {kind} against a "
                + $"marginal of {marginal:F3}, which is more than the question alone can "
                + "carry -- so the exam is answerable without having walked the house");
        }

        // And the two arms come apart somewhere, or the exam reads nothing about the walk at
        // all and a score off it is a score off the alphabet. Asserted on the widest of the
        // four rather than on all of them, because the kinds do not have one ceiling
        // between them -- and it does not invert when the machine improves, because it asks
        // whether the pairing matters rather than whether the answer was right.
        var apart = new[] { "how", "if", "what", "where" }.Max(kind =>
        {
            var (was, hit) = scored[("paired", kind)];
            var (crossed, other) = scored[("crossed", kind)];

            var here = hit / (double)was;
            var there = other / (double)crossed;

            return Math.Abs(here - there) / Math.Sqrt(
                (here * (1.0 - here) / was) + (there * (1.0 - there) / crossed));
        });

        output.WriteLine($"the arms are {apart:F1} standard errors apart at their widest");

        Assert.True(apart > 3.0,
            $"the widest kind separates the paired exam from another house's by {apart:F1} "
            + "standard errors, so nothing the machine took from the walk is being read by "
            + "the survey and the exam is measuring the alphabet");
    }
}
