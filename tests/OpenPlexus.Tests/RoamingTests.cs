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
    /// <param name="chatting">How many rounds of talking about it come before those.</param>
    /// <remarks>
    /// <b>Named at each call rather than defaulted</b>, because a fixture inheriting a dial
    /// it does not pin is how a default moving rewrites an experiment nobody edited.
    /// </remarks>
    private static RoamingSettings World(
        int steps, int people, int asked = 0, int chatting = 0) =>
        new()
        {
            Rooms = 6,
            Props = 4,
            People = people,
            Steps = steps,
            Asked = asked,
            Chatting = chatting,
        };

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

        var tally = new Bench(
            new Watching<Coded>(
                new Roaming(World(120, people: 2, asked: 6, chatting: 6), seed),
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

        Assert.Equal(3, kinds.Count);
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
    /// <b>The house is talked about between the walk and the exam.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The middle phase, and it is where the machine OBTAINS a settlement.</b> The walk
    /// settles by ostension, which is the world choosing what to name; here the machine asks
    /// and the house answers, and what it may ask for is exactly what the exam asks about.
    /// </para>
    /// <para>
    /// <b>Checked against itself rather than against a key</b>, because nothing outside the
    /// world knows where the apple ended up. The house stands still while it is talked about,
    /// so one question asked twice must answer twice the same — and the answer must be a word
    /// for a room, which is a fact about the alphabet.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_walked_house_is_talked_about_before_it_is_examined()
    {
        const int Steps = 20;
        const int Chatting = 5;

        var world = new Roaming(
            World(Steps, people: 2, asked: 5, chatting: Chatting), seed: 4);

        var alphabet = world.Vocabulary.ToList();
        var rooms = new HashSet<Code>(world.Named);

        for (var step = 0; step < Steps; step++)
        {
            // The walk asks nothing, which is the difference between exploring and being
            // spoken to.
            Assert.Null(world.Now.Asked);

            world.Do(null);
            world.Next();
        }

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

        var answers = new List<int>();

        // Asked twice, because the house stands still while it is talked about and a second
        // answer that differed would mean the walk was still running under the conversation.
        for (var again = 0; again < 2; again++)
        {
            world.Do(alphabet.IndexOf("where"));

            // Still listening, because a verb with nothing to be about is no question.
            Assert.True(world.Listening);

            world.Do(alphabet.IndexOf("apple"));

            Assert.False(world.Listening);

            var turn = world.Next();

            Assert.NotNull(turn.Outcome);

            // The answer is a word for a ROOM, which is what a question about where a thing
            // ended up can be answered with.
            Assert.Contains(world.Meaning(turn.Outcome.Value)!.Value, rooms);

            answers.Add(turn.Outcome.Value);
        }

        Assert.Equal(answers[0], answers[1]);

        // And what it was told joined the transcript, or the machine asked and kept nothing.
        // The answer stands in one statement with the thing it is about.
        var apple = world.Meaning(alphabet.IndexOf("apple"))!.Value;
        var told = world.Meaning(answers[0])!.Value;

        Assert.Contains(
            world.Now.Statements!,
            one => one.Codes.Contains(apple) && one.Codes.Contains(told));

        // A round it says nothing in settles on nothing, so a conversation the machine has no
        // question for costs a commitment exactly nothing.
        world.Do(null);

        Assert.Null(world.Next().Outcome);

        output.WriteLine(
            $"{Chatting} rounds of talking, answered {alphabet[answers[0]]} twice for the apple");
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
    /// <b>What asking the house before the exam is worth.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The arms differ in the conversation and in nothing else.</b> Both walk the same
    /// number of houses with the same chooser, which is quiet until the world takes its turn;
    /// one gets no turns and the other gets six, and the exam after them is the same exam.
    /// </para>
    /// <para>
    /// <b>Read per kind, because only one of the three is asked about.</b> The chooser asks
    /// where each thing ended up, so <i>where</i> is the kind the conversation covers and the
    /// other two say whether a transcript with more room words in it helps or hurts.
    /// </para>
    /// <para>
    /// <b>And the lift it reads is the reason the phase goes.</b> John's, and it corrects what
    /// this reading was first written up as. An answer given here joins the transcript, so the
    /// exam that follows asks a question whose answer is the most recent statement — the
    /// three-fold rise on <i>where</i> is recency wearing a conversation's clothes, and it is
    /// this repo's own <i>a corpus can contain its own answer</i> one seam over.
    /// </para>
    /// <para>
    /// <b>What it says about the WALK is the finding worth keeping.</b> A machine that had
    /// tracked where things ended up would not need telling again a round later, so the size
    /// of the lift is the size of what the walk failed to teach. The conversation moves to
    /// after the exam and its answerer becomes a person.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task What_talking_about_the_house_first_is_worth_to_the_exam()
    {
        const int Houses = 130;
        const int Steps = 40;
        const int Asked = 6;
        const int Chatting = 6;

        output.WriteLine(
            $"{Houses} houses, {Steps} steps and {Asked} questions each, asking perfectly");

        output.WriteLine($"{"chatting",-10}{"kind",-8}{"asked",8}{"right",8}{"score",9}");

        var scored = new Dictionary<(int Chatting, string Kind), (int Asked, int Right)>();

        foreach (var chatting in new[] { 0, Chatting })
        {
            var house = new Roaming(
                World(Steps, people: 2, asked: Asked, chatting: chatting),
                seed: 4);

            var alphabet = house.Vocabulary.ToList();

            var words = new Dictionary<Code, string>();

            for (var one = 0; one < house.Vocabulary.Count; one++)
                words[house.Meaning(one)!.Value] = house.Vocabulary[one];

            var world = new Sitting(house, null);

            var brain = new Brain(new CommittingSettings { Capacity = 4_000 }, seed: 1);

            var watching = new Watching<Coded>(
                world,
                new Joined(Joining.Bagged),
                acting: new Asks(
                    house.Meaning(alphabet.IndexOf("next"))!.Value,
                    alphabet.IndexOf("where"),
                    [.. house.Called.Select(one => house.Naming(one)!.Value)]));

            var rounds = Houses * (Steps + chatting + Asked);

            var loop = new Round(brain, rounds, sweep: 500, target: 0.9, window: 500);

            var asked = new Dictionary<string, int>(StringComparer.Ordinal);
            var right = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var round = 0; round < rounds; round++)
            {
                if (watching.Push() is not { } pushed) continue;

                var was = loop.Right;

                await loop.StepAsync(pushed);

                // The conversation's own rounds are reported beside the exam's rather than
                // among them: the machine asked those and the world asked these, and a table
                // holding both would average two problems.
                var kind = world.Asking is { } code ? words[code] : world.Talked ? "chat" : null;

                if (kind is null) continue;

                asked[kind] = asked.GetValueOrDefault(kind) + 1;

                if (loop.Right > was) right[kind] = right.GetValueOrDefault(kind) + 1;
            }

            foreach (var kind in asked.Keys.Order(StringComparer.Ordinal))
            {
                var hits = right.GetValueOrDefault(kind);

                output.WriteLine(
                    $"{chatting,-10}{kind,-8}{asked[kind],8}{hits,8}"
                    + $"{hits / (double)asked[kind],9:F3}");

                scored[(chatting, kind)] = (asked[kind], hits);
            }
        }

        // Three kinds under both arms and a conversation under one of them.
        Assert.Equal(7, scored.Count);

        Assert.All(scored.Values, one => Assert.True(one.Asked > 100));

        // The kind the conversation covered moves, or asking about a thing and being told
        // where it is buys the machine nothing it can use when the same question is put back
        // to it. Asserted as a difference rather than a direction, because a transcript that
        // grew by six statements is a wider moment as well as a better-informed one.
        var (quiet, unhelped) = scored[(0, "where")];
        var (talked, helped) = scored[(Chatting, "where")];

        var here = unhelped / (double)quiet;
        var there = helped / (double)talked;

        var apart = Math.Abs(here - there) / Math.Sqrt(
            (here * (1.0 - here) / quiet) + (there * (1.0 - there) / talked));

        output.WriteLine(
            $"where moves {here:F3} to {there:F3}, {apart:F1} standard errors");

        Assert.True(apart > 3.0,
            $"the conversation asked where every thing was and the exam's own `where` kind "
            + $"moved {here:F3} to {there:F3}, {apart:F1} standard errors — so being told the "
            + "answer and then being asked the question changes nothing the machine does");
    }

    /// <summary>
    /// <b>What a machine wanting to LEARN says about the house.</b>
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
    /// </remarks>
    [Fact]
    public async Task What_a_machine_that_wants_to_learn_asks_the_house()
    {
        const int Houses = 30;
        const int Steps = 40;
        const int Asked = 6;
        const int Chatting = 6;
        const int Seeds = 3;

        output.WriteLine(
            $"{Houses} houses a seed over {Seeds} seeds, {Steps} steps and {Asked} asked");

        output.WriteLine(
            $"{"spoken",-8}{"wanting",-10}{"seed",-6}{"asked",8}{"right",8}{"score",9}");

        var scored =
            new Dictionary<(int Chatting, string Arm, string Kind), (int Asked, int Right)>();

        var questions = new Dictionary<(int Chatting, string Arm), (int Talked, int Spoke)>();
        var perSeed = new Dictionary<(int Chatting, string Arm, int Seed), double>();
        var advocated = 0L;

        foreach (var chatting in new[] { 0, Chatting })
        foreach (var arm in new[] { "uniform", "learning" })
        foreach (var seed in Enumerable.Range(1, Seeds))
        {
            var rounds = Houses * (Steps + chatting + Asked);

            var house = new Roaming(
                World(Steps, people: 2, asked: Asked, chatting: chatting),
                seed);

            var words = new Dictionary<Code, string>();

            for (var one = 0; one < house.Vocabulary.Count; one++)
                words[house.Meaning(one)!.Value] = house.Vocabulary[one];

            var world = new Sitting(house, null);

            var brain = new Brain(new CommittingSettings { Capacity = 2_000 }, seed);

            var draw = new Random(seed);

            var drives = new Drives(
                brain.Held,
                doing: house.Naming,

                // Nothing to want, because a house is not a body with variables to be in
                // trouble about. What is being asked is whether the LEARNING term can pick a
                // word, so every advocated word is wanted equally under the other arm.
                wanting: (_, _) => 1.0,
                untold: () => draw.Next(house.Doings),
                arm: Wanting.Learning);

            var watching = new Watching<Coded>(
                world,
                new Joined(Joining.Bagged),
                acting: arm == "learning"
                    ? Chooses.From(drives.Choose, drives.Cleared)
                    : Chooses.From(_ => draw.Next(house.Doings)));

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

                if (world.Asking is not { } code) continue;

                var kind = words[code];

                asked[kind] = asked.GetValueOrDefault(kind) + 1;

                if (loop.Right > was) right[kind] = right.GetValueOrDefault(kind) + 1;
            }

            foreach (var kind in asked.Keys)
            {
                var had = scored.GetValueOrDefault((chatting, arm, kind));

                scored[(chatting, arm, kind)] =
                    (had.Asked + asked[kind], had.Right + right.GetValueOrDefault(kind));
            }

            var before = questions.GetValueOrDefault((chatting, arm));

            questions[(chatting, arm)] = (before.Talked + talked, before.Spoke + spoke);

            // Per seed, so the direction can be COUNTED rather than read off a total one
            // seed could have carried on its own.
            var here = right.Values.Sum() / (double)asked.Values.Sum();

            perSeed[(chatting, arm, seed)] = here;

            output.WriteLine(
                $"{chatting,-8}{arm,-10}{seed,-6}{asked.Values.Sum(),8}"
                + $"{right.Values.Sum(),8}{here,9:F3}"
                + (chatting == 0 ? string.Empty : $"   spoke {spoke} of {talked}")
                + (arm == "learning"
                    ? $", drive named {drives.Told} and the draw {drives.Untold}"
                    : string.Empty));

            if (arm == "learning") advocated += drives.Told;
        }

        var leads = new Dictionary<int, int>();

        foreach (var chatting in new[] { 0, Chatting })
        {
            foreach (var arm in new[] { "uniform", "learning" })
            {
                var rows = scored.Where(one => one.Key.Chatting == chatting
                    && one.Key.Arm == arm).ToList();

                var put = rows.Sum(one => one.Value.Asked);
                var hit = rows.Sum(one => one.Value.Right);

                output.WriteLine(
                    $"{chatting,-8}{arm,-10}{"all",-6}{put,8}{hit,8}{hit / (double)put,9:F3}"
                    + $"   spoke {questions[(chatting, arm)].Spoke} of "
                    + $"{questions[(chatting, arm)].Talked}");
            }

            leads[chatting] = Enumerable.Range(1, Seeds).Count(seed =>
                perSeed[(chatting, "learning", seed)] > perSeed[(chatting, "uniform", seed)]);

            output.WriteLine(
                $"the drive leads on {leads[chatting]} seeds of {Seeds} at {chatting} spoken");
        }

        // Every house's exam was sat under every cell, whatever the walk before it looked
        // like. Which KINDS got asked is a fact about where the machine ended up walking and
        // is not the same number under two choosers.
        Assert.Equal(4, questions.Count);

        Assert.All(
            questions.Keys,
            one => Assert.Equal(
                Houses * Asked * Seeds,
                scored.Where(row => row.Key.Chatting == one.Chatting && row.Key.Arm == one.Arm)
                    .Sum(row => row.Value.Asked)));

        // The population advocated a word on rounds of its own, or the drive was its own
        // fallback all run and this table is the control printed twice. A fallback is a
        // control arm nobody meant to run, and silence drifts an arm toward the random bar
        // for free.
        Assert.True(advocated > 0,
            "`Wanting.Learning` never once named the word: every round was decided by the "
            + "uniform draw it falls back to, so the two arms here are one arm and nothing "
            + "in the table is about the drive");

        // And the conversation is what the drive's asking is about, so a machine given no
        // conversation at all has none of it to do. A cell that spoke where nothing was
        // spoken would mean the phase ran when it was set to nought.
        Assert.Equal(0, questions[(0, "uniform")].Talked);
        Assert.Equal(0, questions[(0, "learning")].Talked);
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
    /// <b>Read per kind</b>, because the three do not have one ceiling between them. Counting
    /// is at the scope language's own bound and reads its marginal under both arms; where a
    /// thing ended up and what a room held are the halves the walk can be about.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task What_a_learner_scores_on_the_survey_against_the_wrong_houses_exam()
    {
        const int Rounds = 6_000;
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

        // Three kinds under both arms, or the table is a verdict on which questions got asked
        // rather than on the machine.
        Assert.Equal(6, scored.Count);

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
        // three rather than on all of them, because the kinds do not have one ceiling
        // between them -- and it does not invert when the machine improves, because it asks
        // whether the pairing matters rather than whether the answer was right.
        var apart = new[] { "how", "what", "where" }.Max(kind =>
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
