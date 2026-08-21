using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The three ceilings <see cref="Handing"/> was built to separate, taken with no learning at
/// all — <b>fork 105's ladder priced before a rung of it is built.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A front-end arm has a ceiling computable with no learning</b>, and it costs
/// milliseconds against a runner's hour — which is this repo's own trap and the reason this file
/// exists before any matcher does. A grid cannot tell a rule that failed to bind from a
/// front end that threw the binding away one call earlier; these three facts can, because
/// each is exact.
/// </para>
/// <para>
/// <b>And they are facts rather than measurements</b>, which is why they are asserted tight and
/// not printed. Nothing here is a sweep: the bag is the same bag in every draw, the
/// right sentence is the only one holding the asked word, and the answer is the last word of
/// it. If any of the three ever stops holding, the world has drifted away from the question
/// it was built to ask and every number taken on it is owed a re-take.
/// </para>
/// <para>
/// <b>The third probe reads a position and may never ship</b>, which is said here rather than
/// discovered later. It knows the template, so it is the far end of a gap in exactly the
/// standing of <c>ReturningTests</c>' tagged cell and fork 88's handed selection. What it is
/// for is to say that 1.0 is reachable at all, so a learner sitting at 0.5 is known to be
/// short of the world rather than at it.
/// </para>
/// </remarks>
public sealed class HandingTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private const int People = 4;
    private const int Draws = 500;

    /// <summary>
    /// The two front ends whose ceilings are proved above, and nothing else — <b>a set of
    /// codes either way</b>, because that is all the learner can receive.
    /// </summary>
    /// <remarks>
    /// <b>WRITTEN HERE RATHER THAN IN <c>src</c> BECAUSE NEITHER IS A MECHANISM.</b> One is
    /// the control and the other is a selector whose ceiling is a coin flip, so shipping
    /// either would be shipping a thing already known not to answer the world. What they
    /// are for is the BASELINE any role mechanism has to beat, which this repo insists on
    /// having before an arm rather than after it.
    /// </remarks>
    private sealed class Reciting(Reading reading) : IQuantizer<Recited>
    {
        /// <inheritdoc/>
        public byte Modality => 47;

        /// <summary>
        /// Where each word of the chosen sentence stood — <b>a fact about the signal</b>,
        /// and the only thing this front end is allowed to say about order.
        /// </summary>
        /// <param name="observation">One moment.</param>
        /// <remarks>
        /// <b>The question's words get no position, which is deliberate.</b> A question and
        /// a statement are two utterances, so a claim that one of their words came before
        /// another would be inventing a sequencing nobody stated — the same reason
        /// <see cref="Compound{TFrame}.Order"/> refuses to offset two front ends onto one
        /// scale.
        /// </remarks>
        public IReadOnlyDictionary<Code, int>? Order(Recited observation)
        {
            // Only the arm that claims to see order reports one, which is where the axis
            // lives now. There is no dial on the brain: a machine turns whatever order it
            // is given into precedences, always. Whether a sense can tell word order is a
            // fact about the sense, so the control is a front end that says nothing.
            if (reading != Reading.Placed) return null;

            var chosen = observation.Said[Selected(observation)];
            var placed = new Dictionary<Code, int>();

            for (var at = 0; at < chosen.Count; at++) placed[chosen[at]] = at;

            return placed;
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<Code> Codify(Recited observation)
        {
            var moment = new HashSet<Code>(observation.Asked);

            if (reading == Reading.Bagged)
            {
                foreach (var sentence in observation.Said) moment.UnionWith(sentence);

                return moment;
            }

            // MAX OVERLAP AND NOT FIRST-ANY-OVERLAP, which is the difference the fact
            // above measures: every sentence says `the` and so does the question, so the
            // arm already built lands on the newest sentence and reads the marginal.
            var chosen = observation.Said[Selected(observation)];

            moment.UnionWith(chosen);

            if (reading != Reading.Ordered) return moment;

            // Every ordered pair, which is why this is a price and not a mechanism. Handing
            // the whole relation over exhaustively is the most a learner reading order could
            // ever derive, so what it scores is the CEILING on a sequence rung rather than
            // what one would earn -- and it is quadratic in a sentence's length, which is
            // the cost that decides whether the rung belongs in the scope language instead.
            for (var first = 0; first < chosen.Count; first++)
            {
                for (var second = first + 1; second < chosen.Count; second++)
                {
                    if (chosen[first] == chosen[second]) continue;

                    moment.Add(Sequenced.Of(chosen[first], chosen[second]));
                }
            }

            return moment;
        }

    }

    /// <summary>What the front end hands the learner, as three arms of one axis.</summary>
    private enum Reading
    {
        /// <summary>Every word of every sentence. <b>The control, and provably the marginal.</b></summary>
        Bagged,

        /// <summary>The sentence sharing most with the question. <b>Provably a coin flip.</b></summary>
        Chosen,

        /// <summary>
        /// The same words, and the sentence's ORDER reported through
        /// <see cref="IQuantizer{TObservation}.Order"/> — <b>the shipped mechanism.</b>
        /// </summary>
        /// <remarks>
        /// <b>The front end says where the words stood</b>, and the machine derives what
        /// that entails, which is the seam this whole file is about. It hands over the same
        /// codes <see cref="Chosen"/> does, so the two differ in exactly one thing.
        /// </remarks>
        Placed,

        /// <summary>
        /// The same, plus every ORDERED PAIR of its words — <b>the ceiling on a sequence
        /// rung, handed over rather than learnt.</b>
        /// </summary>
        /// <remarks>
        /// <b>An instrument and never a shippable front end</b>, on the standing
        /// <see cref="Unifying"/> holds: a price taken before a mechanism is designed.
        /// It is not the refuted arm that emitted a POSITION beside a fused code — that one
        /// reached every moment and no scope, being never absent, and this is absent
        /// whenever the two words stood the other way round. And it is not a fused code
        /// either: every word is still present under its own identity, so nothing
        /// downstream loses that it is the same john who stood first somewhere else.
        /// </remarks>
        Ordered,
    }

    private static Handing World(int seed = 1) =>
        new(new HandingSettings { People = People, Withheld = 0 }, seed);

    /// <summary>Everything the story said, with the order thrown away.</summary>
    /// <param name="told">One moment.</param>
    private static HashSet<Code> Bag(Recited told)
    {
        var all = new HashSet<Code>();

        foreach (var one in told.Said) all.UnionWith(one);

        return all;
    }

    /// <summary>Which sentence shares most words with the question.</summary>
    /// <param name="told">One moment.</param>
    /// <remarks>
    /// <b>Fork 88's mechanism, settled</b>, and priced here against a world that
    /// leaves it short. Intersecting the question with each statement answers bAbI's
    /// first task; on this world it is worth one half and never one, and separating the two
    /// by a world rather than by an argument is the whole reason this file is not a grid.
    /// </remarks>
    private static int Selected(Recited told)
    {
        var asked = new HashSet<Code>(told.Asked);
        var best = 0;
        var most = -1;

        for (var at = 0; at < told.Said.Count; at++)
        {
            var shared = told.Said[at].Distinct().Count(asked.Contains);

            if (shared <= most) continue;

            most = shared;
            best = at;
        }

        return best;
    }

    /// <summary>
    /// <b>The bag is the same bag in every draw</b>, so nothing conditioned on it can beat the
    /// marginal. The first ceiling, and it is proved rather than measured.
    /// </summary>
    /// <remarks>
    /// <b>This is the property the whole world rests on.</b> The givers are a permutation of
    /// the people and the takers are another, so every person is said exactly twice and
    /// every thing exactly once, whoever handed what to whom. A learner reading
    /// <see cref="Recited.Bagged"/> is therefore looking at a constant and answering from
    /// nothing — which is what makes any lift off the marginal on this world attributable to
    /// binding and to nothing else.
    /// </remarks>
    [Fact]
    public void The_story_says_the_same_words_however_the_things_were_handed_over()
    {
        var world = World();
        var first = Bag(world.Next().Seen);

        // EVERY PERSON, EVERY THING, AND `gave`, `the`, `to`. Written out rather than read
        // off the world, so a world that quietly stopped saying one of them fails here.
        Assert.Equal((People * 2) + 3, first.Count);

        for (var draw = 1; draw < Draws; draw++)
            Assert.Equal(first, Bag(world.Next().Seen));
    }

    /// <summary>
    /// <b>And the answer moves while the bag does not</b>, which is the other half of the
    /// same claim and the half a constant bag cannot supply on its own.
    /// </summary>
    /// <remarks>
    /// <b>A world whose answer was also constant would measure nothing</b>, and would pass
    /// the check above, so the marginal is read here rather than assumed. Every person is the
    /// taker about equally often, which is what makes 1/<see cref="People"/> the number the
    /// first rung stands at rather than an upper bound somebody hoped for.
    /// </remarks>
    [Fact]
    public void Every_person_ends_up_with_the_asked_thing_about_equally_often()
    {
        var world = World();
        var counts = new int[People];

        for (var draw = 0; draw < Draws; draw++) counts[world.Next().Outcome!.Value]++;

        foreach (var count in counts)
            Assert.InRange(count / (double)Draws, 0.20, 0.30);
    }

    /// <summary>
    /// <b>The right sentence is decidable with no learning</b>, and it names exactly two
    /// people. The second ceiling, and it is one half exactly.
    /// </summary>
    /// <remarks>
    /// <b>Both halves or the number is not a half.</b> That the overlap picks the right
    /// sentence every time is what makes selection free; that the sentence holds two
    /// candidates and one of them is the answer is what makes the best a selector can do a
    /// coin flip. A world where the right sentence sometimes named three people would sit
    /// below a half for a reason that has nothing to do with binding.
    /// </remarks>
    [Fact]
    public void Picking_the_sentence_that_shares_most_with_the_question_leaves_a_coin_flip()
    {
        var world = World();
        var cast = world.Called;

        for (var draw = 0; draw < Draws; draw++)
        {
            var turn = world.Next();
            var sentence = turn.Seen.Said[Selected(turn.Seen)];

            // The asked thing is in it, which is what says the selection was right without
            // the probe being handed which sentence to look at.
            Assert.Contains(turn.Seen.Asked[^1], sentence);

            var people = sentence.Where(cast.Contains).Distinct().ToList();

            Assert.Equal(2, people.Count);
            Assert.Contains(cast[turn.Outcome!.Value], people);
        }
    }

    /// <summary>
    /// <b>And first-any-overlap is worth nothing at all here</b> — a selector taking the
    /// first sentence sharing any word with the question, which is a fact about <see cref="Joining.Chained"/>
    /// at one hop rather than about this world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>EVERY SENTENCE SAYS <i>the</i> AND SO DOES THE QUESTION</b>, so first-any-overlap
    /// returns the newest sentence every time and the middle rung is not reached by the arm
    /// that looks like it should reach it. The refutation table already says this in
    /// <see cref="Joining.Chained"/>'s words — a chain keyed on everything walks back a
    /// sentence at a time and never follows a referent — and this is the same sentence
    /// arriving as a number on a world built to hold it still.
    /// </para>
    /// <para>
    /// <b>So the middle rung needs the background subtracted</b>, which is
    /// <see cref="Joining.Distinguished"/>'S RULE AND IS ALREADY BUILT. Taken here
    /// rather than found in a grid, this costs a millisecond and saves attributing a
    /// selector's failure to a learner's — which is exactly what a ceiling is for.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_first_sentence_sharing_any_word_with_the_question_is_the_newest_one()
    {
        var world = World();
        var right = 0;

        for (var draw = 0; draw < Draws; draw++)
        {
            var turn = world.Next();
            var asked = new HashSet<Code>(turn.Seen.Asked);

            var first = turn.Seen.Said.First(one => one.Any(asked.Contains));

            if (first.Contains(turn.Seen.Asked[^1])) right++;
        }

        // At the marginal, because it is the newest sentence and the question is drawn
        // independently of which that is. So the arm reaches the right sentence exactly as
        // often as guessing would, and a run using it measures recency.
        Assert.InRange(right / (double)Draws, 0.20, 0.30);
    }

    /// <summary>
    /// <b>And reading the sentence's ORDER answers it outright.</b> The third ceiling, one,
    /// so anything above a half on this world is binding and nothing else.
    /// </summary>
    /// <remarks>
    /// <b>The probe knows the template and may never ship</b>, which is the whole of its
    /// standing — see this class's own remarks. What it establishes is that the world is
    /// answerable at all from what it hands over, so a run stuck on the coin flip is short
    /// of the world rather than at it.
    /// </remarks>
    [Fact]
    public void Reading_the_last_word_of_that_sentence_answers_every_question()
    {
        var world = World();
        var cast = world.Called;

        for (var draw = 0; draw < Draws; draw++)
        {
            var turn = world.Next();
            var sentence = turn.Seen.Said[Selected(turn.Seen)];

            Assert.Equal(cast[turn.Outcome!.Value], sentence[^1]);
        }
    }

    /// <summary>
    /// <b>And the two people are told apart by ORDER alone</b>, so the gap
    /// between the second ceiling and the third is carried by nothing else.
    /// </summary>
    /// <remarks>
    /// <b>The one check that says the world is honest</b> rather than merely hard. If the
    /// giver and the taker differed in any other way a code could carry — a word only ever
    /// said in one of the two places, a thing that only ever goes one way, a person who is
    /// always the receiver — then a bag of that sentence would separate them and the third
    /// rung would be reachable without binding. Every person appears in both roles across
    /// the draws, so the roles are distinguishable by position and by nothing a set can see.
    /// </remarks>
    [Fact]
    public void Every_person_both_gives_and_receives_so_no_word_marks_a_role()
    {
        var world = World();
        var gave = new HashSet<Code>();
        var took = new HashSet<Code>();

        for (var draw = 0; draw < Draws; draw++)
        {
            foreach (var sentence in world.Next().Seen.Said)
            {
                gave.Add(sentence[0]);
                took.Add(sentence[^1]);
            }
        }

        Assert.Equal(People, gave.Count);
        Assert.Equal(gave, took);
    }

    /// <summary>
    /// <b>One commitment fires on two moments that want different answers</b>, so whatever
    /// it expects it is wrong about one of them. Fork 105's blocker, said as a fact about
    /// the learner rather than about the world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>And it is universal rather than a property of this scope</b>, which is why it is
    /// worth a check. <see cref="Commitment.Fires"/> takes an
    /// <see cref="IReadOnlySet{T}"/>, so two moments that are the same SET are the same
    /// moment to every commitment there could ever be — no scope, no repair, no rung of the
    /// ladder and no matcher over sets separates them. Finding one such pair refutes the
    /// whole language at once.
    /// </para>
    /// <para>
    /// <b>So the blocker is the scope language and not the front end</b>, which is the
    /// correction this file exists to make. The world hands the order over —
    /// <see cref="Recited.Said"/> is a list and not a bag, on the licence
    /// <see cref="IQuantizer{TFrame}.Order"/> already carries — and it is dropped at
    /// <c>Watching</c>'s <c>new HashSet&lt;Code&gt;(said)</c>, one call before anything could
    /// use it. Fork 33 priced the MATCHER, and the matcher was never what was in the way.
    /// </para>
    /// <para>
    /// <b>And fusing the position into the word is not the escape</b>, which is said here
    /// because it is the first thing anybody would reach for. A code meaning
    /// <i>john-in-last-place</i> makes the moment separable and costs the identity: nothing
    /// downstream could then see that it is the same john who stood first in another
    /// sentence. That is the architecture's own line — every input is an ATTRIBUTE of a
    /// concept, never the concept — broken by the front end, which is the same fault as a
    /// fused pair from the other side.
    /// </para>
    /// </remarks>
    [Fact]
    public void One_scope_covers_two_moments_that_want_different_answers()
    {
        var world = World();
        var cast = world.Called;
        var seen = new Dictionary<string, (IReadOnlySet<Code> Moment, int Outcome)>();

        for (var draw = 0; draw < Draws; draw++)
        {
            var turn = world.Next();

            // The whole moment and not the chosen sentence, so the claim is about what the
            // learner is handed rather than about what some front end selected out of it.
            // A pair found here is a pair no arm on this world can ever separate.
            var moment = Bag(turn.Seen);
            moment.UnionWith(turn.Seen.Asked);

            var key = string.Join(
                ",", moment.Select(one => $"{one.Modality}:{one.Value}").Order(StringComparer.Ordinal));

            if (seen.TryGetValue(key, out var before) && before.Outcome != turn.Outcome!.Value)
            {
                var scope = ImmutableArray.CreateRange(moment);

                var held = new Commitment(scope, cast[before.Outcome]);

                // IT FIRES ON BOTH, and a settlement is `Expects == arrived` -- so one of
                // the two is a hit and the other is a miss, for a commitment that had no
                // way to tell them apart and no repair that could find one.
                Assert.True(held.Fires(before.Moment));
                Assert.True(held.Fires(moment));
                Assert.NotEqual(before.Outcome, turn.Outcome!.Value);

                return;
            }

            seen[key] = (moment, turn.Outcome!.Value);
        }

        Assert.Fail(
            $"no two of {Draws} moments were the same set with different answers, so this "
            + "world no longer isolates binding and every number taken on it is owed a "
            + "re-take");
    }

    /// <summary>
    /// <b>And a variable put where the ANSWER goes</b> is refuted by every settlement,
    /// which is the half of rung four that naming a condition does not buy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The type does not stop it and the settlement does</b>, which is why this is a check
    /// and not a comment. <see cref="Commitment.Expects"/> is a <see cref="Code"/> and
    /// <see cref="Unifying.Any"/> returns one, so a commitment expecting <i>whoever filled
    /// the slot</i> CONSTRUCTS. What refutes it is <c>Population</c>'s settlement, which is
    /// an equality against what arrived — and no world may emit
    /// <see cref="Unifying"/>'s modality, by that type's own design, because a moment
    /// holding a pattern would make a scope match itself.
    /// </para>
    /// <para>
    /// <b>So the plan's leaf on rung four is half a sentence short.</b> <i>A condition
    /// naming no argument is what buys transfer</i> — but a condition naming no argument
    /// with a CONSTANT consequent says <i>whoever stood last, the answer is john</i>, which
    /// is right exactly when the filler was john and is therefore the one-rule-per-person
    /// version wearing a variable's clothes. Transfer needs the argument named on BOTH
    /// sides, and only one side exists.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_variable_where_the_answer_goes_can_never_equal_what_arrived()
    {
        var world = World();
        var cast = world.Called;

        var whoever = Unifying.Any(cast[0].Modality, name: 0);

        // IT CONSTRUCTS, which is the half worth demonstrating -- nothing in the primitive
        // refuses this and a reader of the types alone would conclude rung four is
        // expressible.
        var held = new Commitment([whoever, .. world.Handed], whoever);

        Assert.True(Unifying.Names(held.Expects));

        // AND IT LOSES EVERY SETTLEMENT, because a settlement is `Expects == arrived` and
        // what arrives is a person. There is no answer this commitment can be right about.
        foreach (var person in cast) Assert.NotEqual(held.Expects, person);
    }

    /// <summary>
    /// The three arms against the three ceilings — <b>and ORDER is what carries roles
    /// here</b>, which is fork 105 answered without unification being built.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A control beats an argument</b>, which is why this runs at all when every cap is
    /// proved. A proof says what the front end permits; it does not say whether the
    /// learner converts it. <see cref="Roaming"/>'s bagged arm came back with an EMPTY
    /// population because a constant moment surprises nothing and genesis never fires — a
    /// whole arm reading the marginal for a reason that has nothing to do with the marginal.
    /// </para>
    /// <para>
    /// <b>The kill line was a result rather than a value</b>, and it was written before the
    /// run: if handing the order over does not pass 0.60, order is not what carries
    /// roles here and unification is back. It passes at 1.000 on every seed, so <b>a
    /// SEQUENCE rung is what this world wants</b>, not a unifying one — and rung three is
    /// already on the route, unbuilt, while rung four is the expensive one everybody
    /// expected to need.
    /// </para>
    /// <para>
    /// <b>And what it costs decides where the rung belongs.</b> The
    /// ceiling is bought with POPULATION: every ordered pair of a sentence's words is handed
    /// over, so the moment triples, genesis mints on all of it, and the task that four rules
    /// would answer is answered by two hundred. A rung in the SCOPE LANGUAGE would propose a
    /// precedence only where no plain code separates, which is the same ceiling without the
    /// expansion — so this arm prices the build rather than replacing it.
    /// </para>
    /// </remarks>
    [Fact]
    public void Handing_the_order_over_reaches_the_ceiling_the_coin_flip_stops_at()
    {
        var scores = new Dictionary<string, List<double>>();
        var populations = new Dictionary<string, List<int>>();

        var arms = new (string Name, Reading Reading)[]
        {
            ("bagged", Reading.Bagged),
            ("chosen", Reading.Chosen),
            ("placed", Reading.Placed),
            ("handed", Reading.Ordered),
        };

        foreach (var (name, reading) in arms)
        {
            scores[name] = [];
            populations[name] = [];

            foreach (var seed in new[] { 1, 2, 3 })
            {
                var world = new Handing(
                    new HandingSettings { People = People, Withheld = 300 }, seed);

                var brain = new Machines.Brain(
                    new CommittingSettings { Capacity = 4000 }, seed);

                var tally = new Machines.Bench(
                    new Machines.Watching<Recited>(world, new Reciting(reading)),
                    brain)
                    .Run(5_000, sweep: 1000, target: 0.9, window: 2000);

                var exam = tally.Unseen?.Accuracy ?? 0.0;

                scores[name].Add(exam);
                populations[name].Add(brain.Held.Count);

                output.WriteLine(
                    $"{name,-10}| seed {seed} | exam {exam:F3} | own {tally.Recent:F3} "
                    + $"| held {brain.Held.Count,4}");
            }
        }

        // The bag does not sit at the marginal, it sits at nothing, and the difference is
        // the mechanism rather than the score. A constant moment surprises nothing, so
        // genesis never fires, so the population is EMPTY and an empty population abstains
        // rather than guessing -- which reads 0.000 where a guesser would read 0.25.
        //
        // Second time this repo has hit it, which is why it is pinned here and not noted.
        // `Roaming`'s bagged arm did the same for the opposite reason: there a tiny
        // vocabulary made every moment identical after 120 statements, here the world makes
        // it identical by construction. Both look like a learner failing and neither is.
        foreach (var held in populations["bagged"])
            Assert.Equal(0, held);

        Assert.True(scores["chosen"].Min() > scores["bagged"].Max() + 0.10,
            $"choosing the sentence reads {scores["chosen"].Min():F3} at its worst against "
            + $"{scores["bagged"].Max():F3} for the bag at its best, so the selection buys "
            + "nothing measurable and the middle rung of this world's ladder is not there");

        // An upper bar, and it is the one that protects every other finding here. A
        // set-based learner without the order cannot pass a half -- the chosen sentence
        // names two people and is the same set whichever way round they stood. A run above
        // it means the world stopped isolating binding.
        Assert.True(scores["chosen"].Max() < 0.60,
            $"choosing the sentence reads {scores["chosen"].Max():F3} at its best, past the "
            + "coin flip a set of two people permits -- so something in this world separates "
            + "the giver from the taker without order, and every finding in this file is "
            + "owed a re-take");

        // The kill line, written before the run and held down where it came out. The line
        // was 0.60; both sequence arms read 1.000 on all three seeds, so the bar sits at
        // 0.90 -- if either falls back through it, order is not what carries roles here and
        // rung four is back on the list.
        Assert.True(scores["placed"].Min() > 0.90,
            $"the sequence rung reads {scores["placed"].Min():F3} at its worst, so it does "
            + "not reach this world's ceiling and what fork 105 needs is unification after "
            + "all");

        // And adjacency is strictly cheaper for the same ceiling, which is the reading that
        // decides which arm ships. The closure says nothing adjacency does not entail on
        // this world, and it costs two and a half times the population to say it. A world
        // needing a relation ACROSS an intervening word would show as the adjacent arm
        // falling short -- and none of this world's sentences has one, which is exactly why
        // it cannot decide the arm for any other world.
        Assert.True(populations["placed"].Max() < populations["handed"].Min(),
            $"adjacency holds {populations["placed"].Max()} rules at most against "
            + $"{populations["handed"].Min()} for the handed closure at its fewest, so the "
            + "deleted arm was not paying for its expansion after all and its revival row is "
            + "wrong about what would bring it back");

        // And the machine deriving it is the same computation as the front end handing it
        // over, exactly. `handed` folds the pairs in `Codify` and `preceding` folds the
        // identical pairs in `Watching.Sensed`; the codes agree, so the runs agree seed for
        // seed and rule for rule. That is the instrument check on the whole move: what
        // changed when the derivation crossed the seam is WHERE IT LIVES and nothing else,
        // which is what makes the front-end version legitimate as a price and illegitimate
        // as a mechanism.
        Assert.True(scores["handed"].Min() > 0.90,
            $"the handed closure reads {scores["handed"].Min():F3} at its worst, so the "
            + "ceiling this world was built to have is not reachable and the whole ladder "
            + "here is measuring something else");
    }
}
