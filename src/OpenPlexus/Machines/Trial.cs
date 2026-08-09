using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Worlds;

namespace OpenPlexus.Machines;

/// <summary>
/// What the population said about observations the world never drew.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE ONLY NUMBER ON A PERCEPTUAL WORLD THAT SEPARATES LEARNING FROM
/// MEMORISING.</b> Where a world's true rule set can be enumerated, soundness answers
/// that question exactly and this is redundant. Where it cannot — which is every world
/// made of photographs, forever — a trailing accuracy over a bag drawn with
/// replacement is indistinguishable from a lookup table, and this is the whole of the
/// difference.
/// </para>
/// <para>
/// <b>AND THE SILENCE IS REPORTED BESIDE THE SCORE RATHER THAN FOLDED INTO IT.</b> A
/// population that fires on nothing it has not seen scores an excellent
/// <see cref="Accuracy"/> over the handful it does answer, which is a fallback arm
/// nobody meant to run. Both halves or neither.
/// </para>
/// </remarks>
public sealed record Examined
{
    /// <summary>Withheld observations put to the population.</summary>
    public required int Asked { get; init; }

    /// <summary>How many of them anything fired on.</summary>
    public required int Answered { get; init; }

    /// <summary>How many of those were right.</summary>
    public required int Right { get; init; }

    /// <summary>
    /// How many DISTINCT commitments were the best advocate for an answer given here.
    /// </summary>
    /// <remarks>
    /// <b>FORK 46, AND IT IS THE ONE READING THAT COULD MAKE FOUR SESSIONS OF ARMS MAKE
    /// SENSE.</b> Every gate, weighing and subsumption rule tried so far changes the
    /// POPULATION, and under <see cref="Commitments.Weighing.Strongest"/> the answer is
    /// its best advocate and no more. Small against <see cref="Answered"/> means a
    /// handful of rules are deciding the world and the rest are furniture — which would
    /// say the remaining gap is about which rule WINS rather than about which are held.
    /// </remarks>
    /// <remarks>
    /// <b>AND IT COULD ONLY EVER BE READ ON A WORLD THAT WITHHOLDS, WHICH WAS A LIMIT ON
    /// THE FINDING AND NOT A DETAIL.</b> A generated world held nothing back, so this was
    /// unmeasurable on the multiplexer — the one world where depth is genuinely needed,
    /// and therefore the one where a handful of deciders would mean something different.
    /// <b>Fork 48 closed that: <see cref="Worlds.MultiplexerSettings.Withheld"/> keeps
    /// assignments the world never draws</b>, so the reading exists on both kinds of
    /// world now and <i>a finding on one world</i> need no longer be all this is.
    /// </remarks>
    public required int Deciders { get; init; }

    /// <summary>The share of answered predictions that were right.</summary>
    public double Accuracy => Answered == 0 ? 0.0 : Right / (double)Answered;

    /// <summary>The share of withheld observations nothing fired on.</summary>
    public double Silence => Asked == 0 ? 0.0 : (Asked - Answered) / (double)Asked;
}

/// <summary>What a trial did, in terms every world shares.</summary>
/// <remarks>
/// <b>NOTHING WORLD-SPECIFIC LIVES HERE.</b> An answer key, a soundness check, a count
/// of true rules — those are facts about one problem, and a world is asked for them
/// separately. A shared report that grew a field per world would be the mixing this
/// arrangement exists to prevent.
/// </remarks>
public sealed record Tally
{
    /// <summary>Rounds run.</summary>
    public required long Rounds { get; init; }

    /// <summary>Predictions that matched what followed.</summary>
    public required long Right { get; init; }

    /// <summary>Predictions that did not.</summary>
    public required long Wrong { get; init; }

    /// <summary>Rounds where nothing fired, so there was no prediction to be wrong.</summary>
    public required long Silent { get; init; }

    /// <summary>Rounds the world could not say the outcome of.</summary>
    /// <remarks>
    /// <b>REPORTED BECAUSE A VERDICT NOBODY COUNTS IS A VERDICT NOBODY KNOWS FIRED.</b>
    /// `Abstain` was unreachable for the life of the branch and read exactly like a
    /// mechanism that was working and never needed — which is this repo's oldest trap, a
    /// check that is wired and unable to fire. Beside <see cref="Silent"/> it separates the
    /// population having nothing to say from the WORLD having nothing to say.
    /// </remarks>
    public required long Abstained { get; init; }

    /// <summary>The share of answered predictions right over the last tenth.</summary>
    public required double Recent { get; init; }

    /// <summary>
    /// How much of the winner's weight its lead over the runner-up accounted for.
    /// </summary>
    /// <remarks>
    /// <b>ARMED HERE FOR THE FIRST TIME, AND THE PLAN SAID IT ALREADY WAS.</b> The
    /// margin has been computed every round for the life of the branch and read by
    /// nothing — see <see cref="Commitments.Cycle.Confidence"/>. Near nought it says the
    /// answer is being settled by how many advocates each side had rather than by how
    /// accurate any of them is, which is the one failure the vote's whole shape exists
    /// to prevent and the one thing no score reports.
    /// </remarks>
    public required double Confidence { get; init; }

    /// <summary>The round a trailing window first held the target, or zero if never.</summary>
    public required long Reached { get; init; }

    /// <summary>Children minted by repair.</summary>
    public required long Repaired { get; init; }

    /// <summary>Narrower commitments a general one took the place of.</summary>
    /// <remarks>
    /// <b>BESIDE <see cref="Repaired"/> BECAUSE THEY ARE THE TWO DIRECTIONS.</b> Repair
    /// is the only thing here that makes a scope longer and subsumption is the only one
    /// that prefers it shorter, so a population's drift toward one rule per instance is
    /// the difference between these two numbers and was visible in neither.
    /// </remarks>
    public required long Subsumed { get; init; }

    /// <summary>Commitments minted by genesis, before anything culled them.</summary>
    /// <remarks>
    /// <b>The rate genesis ran at, which <see cref="Resident"/> cannot show.</b> A
    /// population held at capacity looks identical whether covering minted two hundred
    /// commitments or two hundred thousand, and the difference between those is the
    /// difference between learning and enumerating.
    /// </remarks>
    public required long Minted { get; init; }

    /// <summary>Commitments resident at the end.</summary>
    public required int Resident { get; init; }

    /// <summary>
    /// Entries in every resident commitment's tally, added up.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE OBJECT THE PLAN PREDICTED WOULD BLOW UP, COUNTED RATHER THAN FEARED.</b>
    /// <i>The table is what blows up, not the commitments</i> — commitments times distinct
    /// codes, both large under population coding. A CIFAR run was memory-bound and every
    /// instrument on it watched time, so what actually ended the run was invisible to all
    /// of them.
    /// </para>
    /// <para>
    /// <b>ENTRIES AND NOT BYTES, BECAUSE A COUNT IS EXACT AND REPRODUCIBLE AND A BYTE
    /// FIGURE IS NEITHER.</b> Asking the runtime for its heap gives a number that moves
    /// with collection timing and with everything else in the process — so it could not be
    /// barred, and a fixed seed would not reproduce it. Each entry is two longs behind a
    /// dictionary slot; multiply if a byte figure is wanted, and the multiplier is a fact
    /// about the runtime rather than about the run.
    /// </para>
    /// <para>
    /// <b>AND IT IS THE HALF OF THE COST THAT CAN BE BARRED.</b> <see cref="Spent"/> is a
    /// wall clock and must never be asserted on; this cannot drift with the machine, so a
    /// budget on it would hold.
    /// </para>
    /// </remarks>
    public required long Separations { get; init; }

    /// <summary>Where the wall clock went, by phase.</summary>
    public required Spent Spent { get; init; }

    /// <summary>
    /// How many DISTINCT moments a resident commitment stands on, on average.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>REPORTED RATHER THAN ACTED ON, WHICH IS <see cref="Confidence"/>'S POSITION AND
    /// FOR A BETTER REASON.</b> Weighing a subsumption against this instead of against
    /// firings was measured and refuted — see the plan's revival row — so nothing decides
    /// on it. What it answers is whether a population's evidence is WIDE or merely LONG,
    /// and no other number here can: a run that has settled a million times over four
    /// hundred distinct moments and one that has seen a million different ones report the
    /// same everything else.
    /// </para>
    /// <para>
    /// <b>AND THE FIRST THING IT SAID WAS THAT THE OBVIOUS STORY WAS WRONG.</b> The
    /// children that cost <see cref="Worlds.Arranged"/> a quarter of its score stand on
    /// many distinct scenes, not on one drawn repeatedly — so they are true of what was
    /// shown and false of what was not, which is a fault no statistic over drawn data can
    /// see. <see cref="Commitment.Occasions"/> carries the cost: one word per commitment.
    /// </para>
    /// </remarks>
    public required double Occasions { get; init; }

    /// <summary>Codes minted to stand for sub-scopes that kept recurring.</summary>
    public required int Named { get; init; }

    /// <summary>Names that stand for a set containing another name.</summary>
    public required int Stacked { get; init; }

    /// <summary>Commitments that have spent their whole repair budget.</summary>
    public required int Exhausted { get; init; }

    /// <summary>Rounds where repair had a commitment it was allowed to fix.</summary>
    public required long Blamed { get; init; }

    /// <inheritdoc cref="Commitments.Population.Unseparated"/>
    public required long Unseparated { get; init; }

    /// <inheritdoc cref="Commitments.Population.Absented"/>
    public required long Absented { get; init; }

    /// <summary>
    /// The share of repairable rounds the current language could not separate.
    /// </summary>
    /// <remarks>
    /// <b>THE LADDER'S TRIGGER, AS A NUMBER RATHER THAN AN ARGUMENT.</b> The plan says a
    /// rung is admitted when and only when no expression in the current language separates
    /// the failures from the hits, and that choosing one before a failure asks is
    /// hand-specified bias by a side door. This is what asking looks like: near nought the
    /// scope language is finding conditions and a rung would be a guess, near one it is
    /// being handed failures it cannot describe.
    /// </remarks>
    public double Wanting => Blamed == 0 ? 0.0 : Unseparated / (double)Blamed;

    /// <summary>How many codes one round produced, on average.</summary>
    /// <remarks>
    /// <b>The cost side of a front end.</b> One allowed to say four times as much has
    /// four times as much to search, so a score without this rewards whoever talks more.
    /// </remarks>
    public required double Codes { get; init; }

    /// <summary>
    /// What it said about what it was never shown, or nothing if the world showed
    /// everything.
    /// </summary>
    /// <remarks>
    /// <b>NOTHING RATHER THAN ZERO WHERE A WORLD WITHHOLDS NOTHING.</b> A generated
    /// world cannot contain its own answer and has nothing to hold back, so a zero here
    /// would read as a learner that generalises to nothing at all — which is a check
    /// that cannot fire reading as a failure instead of as absent.
    /// </remarks>
    public required Examined? Unseen { get; init; }

    /// <summary>
    /// The wrong rounds split by cause, or nothing where the world cannot say what is
    /// true.
    /// </summary>
    /// <remarks>
    /// <b>NOTHING RATHER THAN ZERO, for the same reason <see cref="Unseen"/> is.</b> A
    /// census of nought outvoted and nought uncovered is what a perfect run looks like and
    /// also what an unasked question looks like, and those are opposite readings.
    /// </remarks>
    public Census? Census { get; init; }
}

/// <summary>
/// The WRONG rounds, partitioned by cause — <b>the reading no counter here has ever
/// carried.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>EVERY FAILURE IN THIS REPO HAS BEEN ONE INCREMENT OF ONE NUMBER, AND FOUR SESSIONS
/// OF ARMS WERE AIMED WITHOUT IT.</b> Gates, weighing rules and subsumption bars all
/// attack the case where the right rule is HELD and loses. Nothing measured whether that
/// is where the failures are, so every one of those arms may have been aimed at a bucket
/// with almost nothing in it — which would explain a table of level results better than
/// any of the mechanisms did.
/// </para>
/// <para>
/// <b>THE PARTITION IS EXACT RATHER THAN HEURISTIC, AND ONLY A WORLD WITH ENUMERABLE
/// TRUTH CAN GIVE IT.</b> A sound commitment that fires is right by definition, so on a
/// world that never lies a wrong answer means an UNSOUND rule won. What separates the two
/// diagnoses is whether a sound advocate for the right answer was in the room at all.
/// </para>
/// <para>
/// <b>AND THE SPLIT DECIDES WHERE WORK GOES.</b> <see cref="Outvoted"/> is a readout
/// failure and the vote or subsumption owns it; <see cref="Uncovered"/> is a coverage
/// failure and genesis or repair owns it. They have opposite fixes, and a rule that helps
/// one can cost the other.
/// </para>
/// </remarks>
public sealed record Census
{
    /// <summary>Wrong rounds where a SOUND advocate for the right answer fired and lost.</summary>
    /// <remarks>
    /// <b>THE RULE WAS PRESENT, CORRECT, AND MATCHED THE MOMENT.</b> Nothing was missing
    /// and nothing needed minting — the population knew and did not say so, which is the
    /// failure every arm tried so far was built for.
    /// </remarks>
    public required long Outvoted { get; init; }

    /// <summary>Wrong rounds where nothing sound advocating the right answer fired at all.</summary>
    /// <remarks>
    /// <b>NO VOTE RULE CAN REACH THESE, WHICH IS WHY THEY ARE COUNTED APART.</b> A merge
    /// cannot promote an advocate that was never in the room, so a change to how weights
    /// combine is inert on every round in this bucket however good it is.
    /// </remarks>
    public required long Uncovered { get; init; }

    /// <summary>
    /// Of the <see cref="Outvoted"/>, how many were lost to a winner with a LONGER scope.
    /// </summary>
    /// <remarks>
    /// <b>THE OVER-SPECIALISATION HYPOTHESIS AS A NUMBER RATHER THAN A STORY.</b> The plan
    /// says the vote prefers the narrower rule every round while subsumption prefers the
    /// general one every thousandth, so a child displaces its parent as decider and then
    /// answers what it has never seen. If that is what is happening this is most of
    /// <see cref="Outvoted"/>; if it is near nought the story is wrong and the winner is
    /// something else.
    /// </remarks>
    public required long Deeper { get; init; }

    /// <summary>Wrong rounds counted here at all.</summary>
    public long Wrong => Outvoted + Uncovered;

    /// <summary>The share of wrong rounds a vote rule could in principle have saved.</summary>
    public double Reachable => Wrong == 0 ? 0.0 : Outvoted / (double)Wrong;
}

/// <summary>
/// A world, a translation, and the brain — joined here and nowhere else.
/// </summary>
/// <typeparam name="TSeen">Whatever the world natively produces.</typeparam>
/// <remarks>
/// <para>
/// <b>THE SEAM IS ONE CALL WIDE IN EACH DIRECTION.</b> A world says what happened in
/// its own terms; a quantiser turns that into codes; the brain learns from codes. No
/// world knows a brain exists, and the brain knows nothing about where its codes came
/// from — which is what lets the SAME brain, configured once, run every world.
/// </para>
/// <para>
/// <b>THE TRANSLATION IS CHOSEN HERE, WHICH IS NEITHER SIDE'S BUSINESS TO DECIDE.</b>
/// Whether a reading is banded or winnowed is a fact about the pipe. Putting that
/// choice inside a world is how a world starts deciding what the brain perceives, and
/// putting it inside the brain is how the brain starts knowing about worlds.
/// </para>
/// </remarks>
public sealed class Trial<TSeen>
{
    private readonly IWorld<TSeen> _world;
    private readonly IQuantizer<TSeen> _sensing;
    private readonly Brain _brain;
    private readonly Func<ImmutableArray<Code>, Code, bool>? _sound;

    /// <param name="world">The problem.</param>
    /// <param name="sensing">The translation between it and the brain.</param>
    /// <param name="brain">The one brain, already configured.</param>
    /// <param name="sound">
    /// Whether a scope-and-expectation is TRUE of the world, or nothing where the world
    /// cannot say — <b>the oracle the failure census is made of.</b>
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>A DELEGATE RATHER THAN A WORLD, BECAUSE A TRIAL MAY NOT KNOW WHICH WORLD IT IS
    /// RUNNING.</b> Naming <c>Multiplexer</c> here would put one world's vocabulary in
    /// front of every other one and would fail <c>SeparationTests</c> from the other
    /// direction — a question only some worlds can answer arrives as a function some
    /// callers pass.
    /// </para>
    /// <para>
    /// <b>AND IT IS OFF UNLESS ASKED FOR, BECAUSE IT COSTS A SECOND MATCH EVERY ROUND.</b>
    /// Matching is nine tenths of the clock on a narrow world, so a census left on by
    /// default would roughly double every run this repo has ever timed — and it says
    /// nothing on a world whose truth cannot be enumerated anyway.
    /// </para>
    /// </remarks>
    public Trial(
        IWorld<TSeen> world,
        IQuantizer<TSeen> sensing,
        Brain brain,
        Func<ImmutableArray<Code>, Code, bool>? sound = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(sensing);
        ArgumentNullException.ThrowIfNull(brain);

        _world = world;
        _sensing = sensing;
        _brain = brain;
        _sound = sound;
    }

    /// <summary>What a blind guess scores on this world.</summary>
    public double Chance => 1.0 / _world.Outcomes;

    /// <summary>Runs the world through the translation into the brain.</summary>
    /// <param name="rounds">How many rounds.</param>
    /// <param name="sweep">How often to subsume, abstract and cull.</param>
    /// <param name="target">The trailing accuracy <see cref="Tally.Reached"/> waits for.</param>
    /// <param name="window">How many answered predictions that accuracy is over.</param>
    /// <remarks>
    /// <b>The held-out examination is taken at the END and is also callable at any
    /// point</b> — see <see cref="Examine()"/>. A curve over it says something a single
    /// endpoint cannot: whether the gap to the drawn bag OPENS as the population fills,
    /// which is what memorising looks like from outside.
    /// </remarks>
    public Tally Run(long rounds, int sweep = 1000, double target = 0.9, int window = 2000)
    {
        var running = RunAsync(
            new Alone(_brain.Held), [_brain.Held], rounds, sweep, target, window);

        // A COUNCIL THAT WOULD HAVE MADE THIS WAIT IS REFUSED RATHER THAN WAITED FOR, and
        // that is what keeps sync-over-async to one line that cannot fire. `Alone`
        // completes every task before returning it, so this is a completed task being
        // unwrapped; a fleet reaches `RunAsync` directly and never comes through here. If
        // the check ever throws, something has quietly put a wire under the loop that a
        // hundred synchronous call sites are still driving.
        if (!running.IsCompleted)
            throw new InvalidOperationException(
                "the council did not answer on the calling thread, so this run would be "
                + "blocking a thread on a wire — call `RunAsync`");

        return running.GetAwaiter().GetResult();
    }

    /// <summary>Runs the world through the translation into whoever holds the commitments.</summary>
    /// <param name="council">One population, or a fleet of them.</param>
    /// <param name="holding">
    /// Whose commitments to report on — <b>one machine's, or every machine's.</b>
    /// </param>
    /// <param name="rounds">How many rounds.</param>
    /// <param name="sweep">How often to subsume, abstract and cull.</param>
    /// <param name="target">The trailing accuracy <see cref="Tally.Reached"/> waits for.</param>
    /// <param name="window">How many answered predictions that accuracy is over.</param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <para>
    /// <b>THE WORLD STILL PULLS AND THE ROUNDS STILL COME FROM THIS LOOP, WHICH IS FORK 53
    /// LEFT OPEN RATHER THAN ANSWERED.</b> <see cref="IWorld{TSeen}.Next"/> is asked for
    /// the next observation the moment the last one is settled, so a fleet's round is as
    /// long as its slowest holder and never longer. What a learner whose rounds arrived on
    /// the world's schedule would do differently is a different harness, and this is the
    /// one that keeps every recorded number comparable.
    /// </para>
    /// <para>
    /// <b>THE POPULATIONS ARE HANDED IN RATHER THAN READ OFF THE COUNCIL, AND C1 IS
    /// WHY.</b> An asker cannot report what a fleet holds because it may not know:
    /// residents, tables and names are facts about machines it is only allowed to ask
    /// questions of. Whoever composed the fleet holds those references, which is the
    /// experimenter standing outside the machine — the same position <c>Examine</c> has
    /// always been read from.
    /// </para>
    /// <para>
    /// <b>AND EVERY POPULATION FIGURE IS AN AGGREGATE, WHICH FOR ONE MACHINE IS ITSELF.</b>
    /// Residents, tables and exhausted rules add up; occasions is the mean over every
    /// resident anywhere; names are counted DISTINCT across holders, because two machines
    /// minting the same name is the mechanism working and counting it twice would read as
    /// the opposite.
    /// </para>
    /// </remarks>
    public async Task<Tally> RunAsync(
        ICouncil council,
        IReadOnlyList<Population> holding,
        long rounds,
        int sweep = 1000,
        double target = 0.9,
        int window = 2000,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(council);
        ArgumentNullException.ThrowIfNull(holding);

        var cycle = new Cycle(council, rounds, sweep, target, window);

        long codes = 0;
        long outvoted = 0, uncovered = 0, deeper = 0;

        // ONE POPULATION OR NONE, BECAUSE THE CENSUS ASKS WHAT A MACHINE HELD. On a fleet
        // no single population is the one that voted, and reaching into all of them to
        // find out would be the experimenter answering a question C1 says a machine may
        // not ask. Left null, every count below stays nought and the report says so.
        var censusing = _sound is not null && holding.Count == 1 ? holding[0] : null;

        for (long round = 0; round < rounds; round++)
        {
            var turn = _world.Next();

            var said = _sensing.Codify(turn.Seen);
            codes += said.Count;

            // TAKEN BEFORE THE STEP, BECAUSE THE STEP TEACHES. `Settle`, `Cover` and
            // `Mend` all move the population, so the same three read-only calls after it
            // would be asking a different machine what it thought a moment ago. These are
            // the same calls `Examine` is built out of and they change nothing.
            if (censusing is not null && turn.Outcome is { } expected)
            {
                var arrived = Brain.Says(expected);
                var firing = censusing.Firing(censusing.Moment(new HashSet<Code>(said)));
                var vote = censusing.Predict(firing);

                if (!firing.IsDefaultOrEmpty && vote.Expects != arrived)
                {
                    // A SOUND COMMITMENT THAT FIRES IS RIGHT BY DEFINITION, so the only
                    // question a wrong round raises is whether one of them was in the room
                    // and lost. Everything else is a round no vote rule could have saved.
                    var advocate = firing.FirstOrDefault(one =>
                        one.Expects == arrived && _sound!(one.Scope, one.Expects));

                    if (advocate is null) uncovered++;
                    else
                    {
                        outvoted++;

                        // THE WINNER'S OWN SCOPE, FOUND BY THE IDENTITY THE VOTE ALREADY
                        // REPORTS. `Vote.By` names the best advocate for the side that
                        // won, so no second search is needed to ask whether a child beat
                        // a parent.
                        var won = firing.FirstOrDefault(one => one.Identity == vote.By);

                        if (won is not null && won.Scope.Length > advocate.Scope.Length)
                            deeper++;
                    }
                }
            }

            // A ROUND THE WORLD COULD NOT SETTLE PASSES NOTHING RATHER THAN A NUMBER, and
            // that is the whole of what arms `Abstain`. Every world that always knows its
            // outcome reaches the same call it always did.
            await cycle.StepAsync(
                new HashSet<Code>(said),
                turn.Outcome is { } outcome ? Brain.Says(outcome) : null,
                ct).ConfigureAwait(false);
        }

        return new Tally
        {
            Rounds = rounds,
            Right = cycle.Right,
            Wrong = cycle.Wrong,
            Silent = cycle.Silent,
            Abstained = cycle.Abstained,
            Recent = cycle.Recent,
            Confidence = cycle.Confidence,
            Reached = cycle.Reached,
            Repaired = cycle.Repaired,
            Subsumed = cycle.Subsumed,
            Minted = cycle.Minted,
            Resident = holding.Sum(held => held.Count),
            Separations = holding.Sum(
                held => held.All.Sum(one => (long)one.Separations.Count)),
            Spent = cycle.Spent,
            Occasions = holding.Sum(held => held.Count) == 0
                ? 0.0
                : holding.SelectMany(held => held.All).Average(one => one.Occasions),
            Named = holding
                .SelectMany(held => held.Names.Means.Select(one => one.Key))
                .Distinct()
                .Count(),
            Stacked = holding.Sum(held =>
                held.Names.Means.Count(one => one.Value.Any(held.Names.Knows))),
            Exhausted = holding.Sum(held => held.Exhausted(_brain.Dials.Budget)),
            Blamed = holding.Sum(held => held.Blamed),
            Unseparated = holding.Sum(held => held.Unseparated),
            Absented = holding.Sum(held => held.Absented),
            Codes = codes / (double)rounds,
            Unseen = Examine(holding),
            Census = censusing is null
                ? null
                : new Census { Outvoted = outvoted, Uncovered = uncovered, Deeper = deeper },
        };
    }

    /// <summary>
    /// Asks the population about observations the world never drew, and teaches it
    /// nothing.
    /// </summary>
    /// <returns>What it said, or nothing where the world withholds nothing.</returns>
    /// <remarks>
    /// <para>
    /// <b>EVERY CALL HERE IS READ-ONLY AND THAT IS LOAD-BEARING RATHER THAN
    /// INCIDENTAL.</b> <see cref="Population.Moment"/> folds names without minting one,
    /// <see cref="Population.Firing"/> gathers candidates without touching them, and
    /// <c>Population.Predict</c> reads accuracies it does not write.
    /// <c>Settle</c>, <c>Cover</c> and <c>Mend</c> are the three that teach and none of
    /// them is called — an examination that moved a single counter would be a second
    /// training run wearing the word <i>held-out</i>, and the number would be worth
    /// less than no number.
    /// </para>
    /// <para>
    /// <b>AND C4 IS UNTOUCHED, WHICH IS THE POINT THAT WAS BEING MISSED.</b> The
    /// constraint is that the MACHINE may not depend on an episode boundary. It does
    /// not: nothing here is fed back, the run does not pause, and the population cannot
    /// tell this happened. The person reading the number is outside the machine, and
    /// always was.
    /// </para>
    /// </remarks>
    public Examined? Examine() => Examine([_brain.Held]);

    /// <summary>The same examination, put to however many machines hold the population.</summary>
    /// <param name="holding">Whose commitments to ask.</param>
    /// <remarks>
    /// <b>EACH FOLDS THE MOMENT THROUGH ITS OWN NAMES AND SPEAKS FOR ITSELF, AND THE MERGE
    /// IS THE SAME ARITHMETIC THE WIRE USES.</b> <c>Population.Predict</c> is
    /// <c>Decide</c> over one testimony, so a fleet of one reaches the identical vote by
    /// the identical route — which is what makes a distributed examination comparable with
    /// every one taken before there was a wire, rather than a second instrument.
    /// </remarks>
    private Examined? Examine(IReadOnlyList<Population> holding)
    {
        if (_world is not IWithholds<TSeen> withholding) return null;

        // AND A WORLD THAT CAN WITHHOLD BUT IS NOT WITHHOLDING REPORTS ABSENT RATHER THAN
        // NOUGHT, WHICH IS THE SAME DISTINCTION ONE LAYER IN. It used to be carried by the
        // interface alone — a world either held things back or did not implement this —
        // and that stopped being true the moment `Multiplexer` gained a dial for it. An
        // empty examination answers nothing, so every count is nought and the accuracy
        // with them, which reads as a population that generalises to NOTHING rather than
        // as a question nobody asked. `WithheldTests` names that trap and this is where it
        // would have arrived from.
        if (withholding.Withheld.Count == 0) return null;

        var answered = 0;
        var right = 0;
        var deciders = new HashSet<Code>();

        // AND A WITHHELD OBSERVATION WITH NO OUTCOME IS NOT ASKED AT ALL, because there is
        // nothing to score it against. Counting it would report as SILENCE -- a population
        // that declined to answer -- when what happened is that the examiner had no answer
        // key for that row, which is a completely different fact.
        var answerable = withholding.Withheld.Where(one => one.Outcome is not null).ToList();

        if (answerable.Count == 0) return null;

        foreach (var turn in answerable)
        {
            var said_ = _sensing.Codify(turn.Seen);

            var heard = holding
                .Select(held =>
                {
                    var moment = held.Moment(new HashSet<Code>(said_));
                    return held.Speak(held.Firing(moment));
                })
                .ToList();

            var vote = Population.Decide(heard, _brain.Dials.Weighing);

            if (vote.Expects is not { } said) continue;

            answered++;
            if (vote.By is { } by) deciders.Add(by);
            if (said == Brain.Says(turn.Outcome!.Value)) right++;
        }

        return new Examined
        {
            Asked = answerable.Count,
            Answered = answered,
            Right = right,
            Deciders = deciders.Count,
        };
    }
}
