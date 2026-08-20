using OpenPlexus.Commitments;
using OpenPlexus.Bus;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// Every dial is either driven by something, or named here with the reason it is
/// not.
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S STANDING ASK: fewer knobs, more things that find their own
/// level.</b> The trouble with that as an aspiration is that nobody notices a
/// sixth dial arriving beside five — so this makes the count a number the build
/// checks, in the same way the doc has a word budget and the source has a clone
/// budget.
/// </para>
/// <para>
/// <b>Adding a dial fails this test until somebody says which it is.</b> Either
/// something sets it from what the run is doing, or there is a written reason it
/// cannot be — and "nobody has got to it yet" is a perfectly good reason, as long
/// as it is written down where it can be counted.
/// </para>
/// <para>
/// <b>The lesson from fork 23 is that the hard part is the SIGNAL, not the
/// controller.</b> Stamina got one because it had feedback available — did the
/// walk reach what it was narrowing to. Reflection's threshold has no such
/// signal: <c>Hunger</c> was inverted and <c>Thwarted</c> was the right shape and
/// swung too little. So "add a controller" nearly always decomposes into "find
/// the internal signal first", which is what step 2 is really for.
/// </para>
/// </remarks>
public sealed class DialTests
{
    /// <summary>Dials something already sets from what the run is doing.</summary>
    private static readonly Dictionary<string, string> Driven = new(StringComparer.Ordinal)
    {

    };

    /// <summary>Every dial in the tree, wherever its settings record lives.</summary>
    private static IEnumerable<System.Reflection.PropertyInfo> Census() =>
        typeof(CommittingSettings).GetProperties();

    /// <summary>
    /// Every dial the two-world bar applies to, as a name and the enum behind it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two sources, because the bar was reading one of them.</b> A brain dial is a
    /// property of <see cref="CommittingSettings"/> and always was; a TRANSLATION dial is an
    /// enum in <c>OpenPlexus.Codes</c> — <c>Joining</c> and <c>Feeling</c> today — and the
    /// check never saw one. That hole is how <c>Feeling</c> shipped on a single world with
    /// every guard green.
    /// </para>
    /// <para>
    /// <b>And <c>Feeling</c> passes for the wrong reason</b>, which is said here rather than
    /// banked. It is measured on <c>Homeostat</c> alone; <c>HomeostatTests</c> also names
    /// <c>Multiplexer</c>, in the check that a watched world refuses a chooser, and the bar
    /// counts worlds a FILE mentions. That is the attribution defect <c>PushbackTests</c>
    /// already has an entry for, arriving a second time — so closing the hole did not make
    /// this dial measured, it made the defect load-bearing for one more row.
    /// </para>
    /// <para>
    /// <b>And a world's own run dial is deliberately not here.</b> <c>Looking</c> and
    /// <c>Fronting</c> live on <c>ArrangedRun</c> and <c>GradedRun</c>, and the plan's line
    /// is that a world may turn its own dials and never the brain's — so a bar demanding a
    /// second world of them would be demanding that one world's setting be measured on
    /// another, which is the mixing the rule exists to prevent.
    /// </para>
    /// </remarks>
    private static IEnumerable<(string Name, Type Kind)> Arms() =>
        Census()
            .Where(one => one.PropertyType.IsEnum)
            .Select(one => (one.Name, Kind: one.PropertyType))
            .Concat(typeof(CommittingSettings).Assembly
                .GetExportedTypes()
                .Where(one => one.IsEnum && one.Namespace == "OpenPlexus.Codes")
                .Select(one => (Name: one.Name, Kind: one)));

    /// <summary>
    /// Dials nothing drives, each with the reason. <b>A reason, not an excuse</b>
    /// — several of these say outright that nobody has found the signal yet.
    /// </summary>
    private static readonly Dictionary<string, string> HandSet = new(StringComparer.Ordinal)
    {
        ["Admitting"] =
            "what a separating condition must do besides separate, and the reason "
            + "it is an ARM has changed rather than gone. Two generated worlds have "
            + "weighed in and it is ahead on Monk-1 for a tenth fewer repairs; what "
            + "stops it shipping is that COSTING NO SCORE is a corner rather than a "
            + "property. `LessonTests` crosses it with `Rooting` and it costs most "
            + "of the examination under both roots at eight tellings, because a bar "
            + "refusing a child that cannot clear the floor blocks repair exactly "
            + "while the population is young. It is free at saturation. The entry "
            + "leaves when the bar is priced against how young a population is",

        ["Crediting"] =
            "whether a mint is credited with the round that made it, and it has had "
            + "its generated world. What came back is not a win: it CONVERTS a tie "
            + "into an outranking for the identical score, the newest mint being "
            + "the strongest because the older ones have fired and missed since. It "
            + "breaks a tie by recency, which is not correctness, and the risk its "
            + "own remark named -- every mint arriving at the ceiling together -- "
            + "does not happen, because the population is built while it is told. "
            + "The entry leaves when something separates two blank rules",

        ["Rooting"] =
            "how wide a scope genesis may mint, and it is an ARM rather than a "
            + "level -- there is no third value between one code and the whole "
            + "moment. The wide one is now the DEFAULT: it wins on drawn lessons, "
            + "reaching at three tellings what one code a commitment does not reach "
            + "at eight, and is a draw on `Arranged` seed for seed. `Singly` stays "
            + "as the control, because the wide arm mints everything it mints and "
            + "one scope besides -- three grids pin it to keep a baseline that "
            + "scope removes. The entry leaves when nothing needs the baseline",

        // ---- Arrived with the commitment branch ----------------------------

        ["Recency"] =
            "OPEN, AND FORK 27 IS THE WHOLE OF IT. How fast the local estimate "
            + "forgets is the one number that decides whether keeping a second "
            + "estimate beside the G-Counters was worth it. The switching "
            + "multiplexer is the world that could hunt it and it has not been run",

        ["Floor"] =
            "how many misses before a proportion can be tested at all. It is a "
            + "property of the TEST rather than of the world -- below it no "
            + "statistic has power, and that is arithmetic rather than a level",

        ["Alpha"] =
            "how much noise the separation bar admits. A choice about what counts "
            + "as evidence, and the correction beside it is what makes the number "
            + "mean anything; hunting it would be hunting a standard of proof",

        ["Budget"] =
            "OPEN, AND IT WAS CALLED A GUARD HERE UNTIL MEASUREMENT REFUTED THAT. "
            + "Removing it is WORSE at every width and raising it is better at the "
            + "widest, so it has an interior optimum and is a LEVEL. The optimum "
            + "moves with the number of relevant bits, and nothing reads that yet -- "
            + "the honest driver is whether a parent still has failures no child "
            + "covers, which the gate already computes and throws away",

        ["Capacity"] =
            "a capacity rather than a level, exactly as `Row` is. What a machine "
            + "can afford to hold is a fact about the machine and not about the "
            + "run, so there is nothing here for a controller to hunt",

        ["Mending"] =
            "OPEN, AND IT IS HALF OF WHAT IT USED TO BE. This was one setting "
            + "deciding a gate and a timing at once, and every reading of it moved "
            + "both -- so the census counted one name for two decisions and the "
            + "defect row calling it a dial whose optimum moves with the world was "
            + "taken on a list of crossed cells. What is left here is WHICH "
            + "commitments repair may touch. Its sign flips with the timing beside "
            + "it: every round on the clean multiplexer it leads, after a failure "
            + "on the same world it is six and a half standard errors behind no "
            + "gate at all, and on `Arranged` it is inert in both to three metrics. "
            + "Not a level, so not a controller's job; the driver would be "
            + "something that says whether specialising works on this world, which "
            + "is fork 45. AND THE WHOLE TWO-BY-TWO HAS NOW BEEN RUN rather than "
            + "read off four rows and a revival note: every round the gate is worth "
            + "about two standard errors, and ungated every round mints the FEWEST "
            + "children of the six while holding the fewest residents and the "
            + "fewest unsound rules -- so what it does every round is aim the "
            + "attempt rather than limit it, and ungated burns per-parent budgets "
            + "on parents something else already covers",

        ["Budgeting"] =
            "NOT A DIAL AND NOT A LEVEL -- IT IS WHICH QUESTION `Budget` ASKS, and "
            + "one of the two answers cannot bind at all on any world here. A child "
            + "adds ONE code, so a parent's distinct children are capped by the "
            + "vocabulary: twelve at six bits and twenty-two at eleven, against a "
            + "budget of sixty-four. So `Children` is a FREE budget wearing a "
            + "limit's name, `Attempts` is a re-derivation limit, and every number "
            + "this repo has ever taken under `Budget` was taken under the second. "
            + "That is also why its optimum moved with the relevant bits, which the "
            + "plan carried as a puzzle for the life of the branch -- how often a "
            + "parent re-derives is a function of width and nothing else about the "
            + "search is. A controller would be hunting a level that is not there. "
            + "`BudgetingTests` holds the grid and a tripwire that goes red the day "
            + "a world's vocabulary reaches the budget. AND A MOVING WORLD IS NOT "
            + "THAT WORLD, which was worth checking because a free budget is what "
            + "recovers when the target moves: `Children` at sixty-four is "
            + "BIT-IDENTICAL to no budget across twelve cells of two widths and two "
            + "worlds, so non-stationarity separates the SIZE of the budget and not "
            + "what it counts. The cell that would separate the two rules is still a "
            + "vocabulary reaching sixty-four and nothing else. AND A THIRD CELL "
            + "ARRIVES BECAUSE BOTH OF THE FIRST TWO ARE TOTALS, WHICH C4 REFUSES: "
            + "`Earned` pays one attempt per `Floor` misses, so the allowance grows "
            + "while a parent is being wrong and stops when it is not, and `Budget` "
            + "is not read at all. It is the only cell here that assumes nothing "
            + "about how long a parent lives, and whether it BINDS is the question "
            + "-- `Children` was refused for turning out to be free in disguise. "
            + "AND THE CELL THAT WOULD SEPARATE IT DOES NOT EXIST, WHICH IS "
            + "ARITHMETIC AND NOT A MISSING WORLD: `Repair` charges an attempt and "
            + "adds a name in the same two lines, and `Forking.Distinct` refuses a "
            + "parent every code it has spent -- so two codes are two scopes are "
            + "two identities, and the set and the counter move together forever. "
            + "`Children` is FREE under `Repeated` and a SYNONYM under `Distinct`, "
            + "with no third thing available; measured bit-identical on two worlds "
            + "and 36 against 452 repairs under the other rule. It is kept as "
            + "exactly that check, which goes red the day forking changes. AND A "
            + "FOURTH CELL ARRIVES BECAUSE THE THIRD READ FREE: `Earned` funds a "
            + "parent in proportion to how WRONG it is, so on `Arranged`, where "
            + "the truths are one code and every repair is damage, the parents "
            + "doing the most damage earn fastest -- 0.651 unseen at 95 sound "
            + "against the total's 0.725 at 709. `Curved` divides the same floors "
            + "and takes the SCARCER of hits and misses, so its allowance is "
            + "`Earned`'s times min(hits/misses, 1): inert on a parent right half "
            + "the time or better, a ninth of the grant for one right a round in "
            + "ten. It is fork 110, it is wired rather than measured, and what "
            + "would drop it is landing on `earned`'s row rather than the total's "
            + "-- a cap that never binds where it matters is `Children`'s "
            + "refutation a second time. `BudgetingTests` holds the check that the "
            + "two earned arms DIFFER, and `ArrangingTests` holds the grid",

        ["Repairing"] =
            "OPEN, AND IT ARRIVES BY SEPARATION RATHER THAN BY INVENTION, which is "
            + "why the count rising is the finding rather than the cost. It was "
            + "always being set -- `Uncovered` and `Improving` repaired every round "
            + "and `Outvoted` and `Neglected` waited for a failure -- and nothing "
            + "named it, so no comparison could move one axis without the other. "
            + "Separated, this is the load-bearing half: every-round repair leads "
            + "on both worlds measured, unseparated on the multiplexer and near two "
            + "standard errors on `Arranged`. Not a level either. The plan's own "
            + "argument says it should not need one -- an outvoted commitment "
            + "accrues its own hits and misses, and waiting for the vote to be "
            + "wrong is what stopped it spending them. AND IT IS NOT THE WHOLE "
            + "STORY EITHER, which the six-cell grid says and four rows could not: "
            + "ungated after a failure sits WITH the every-round group, so the "
            + "ruinous cell is the gate after a failure specifically rather than "
            + "the timing on its own",

        ["Subsuming"] =
            "OPEN, AND THE FIRST OF THESE WHOSE DIRECTION FOLLOWS FROM THE WORLD "
            + "RATHER THAN FROM ITS RULE STRUCTURE. Demanding that a narrower "
            + "commitment be SIGNIFICANTLY better before it survives is worth about "
            + "five points on the noisy multiplexer under every repair gate and "
            + "roughly doubles the sound rules; on a clean world it is level or "
            + "slightly behind, because there a hair of advantage is real signal "
            + "and a significance test throws it away. The honest driver would be "
            + "an estimate of how much the world lies, which nothing computes. AND "
            + "THE THIRD RULE IS REFUTED: weighing the advantage against the "
            + "DISTINCT occasions a commitment stands on takes the noisy "
            + "multiplexer to chance with no sound rules at all, because every "
            + "condition a scope adds halves the moments it can fire in -- so "
            + "independent evidence falls exponentially in DEPTH and the bar "
            + "becomes a cap set by the size of the world",

        ["Coarsening"] =
            "fork 85, and the half of it that survived. WHETHER subsumption may "
            + "read a member's entailment of its category. NOT A LEVEL: whether "
            + "one rule may absorb another is not a quantity. It is INERT without "
            + "a vocabulary, so `Never` is a control rather than a shipped "
            + "default -- a run never told which codes are alternatives cannot "
            + "tell the two apart, and every number recorded before this existed "
            + "is reproduced with it on. There is nothing for a controller to "
            + "hunt: the accuracy bar that judges every other subsumption judges "
            + "this one",

        ["Forking"] =
            "WHETHER A PARENT MAY PROPOSE A FORK IT HAS ALREADY MADE, and it is two "
            + "rules that both do something rather than a level. Repair is "
            + "deterministic and its table moves by one entry a firing, so the "
            + "argmax is stable for thousands of rounds and a parent arrives where "
            + "it already is -- twenty to fifty collisions a birth at every "
            + "majority rung, which is why `Budget` has always been a re-derivation "
            + "limit rather than a search limit. There is nothing here for a "
            + "controller to hunt: whether to repeat is not a quantity. AND IT WON "
            + "AND BECAME THE CODE, on four worlds and worse on none -- perfect at "
            + "six bits, two standard errors of hard-round coverage at eleven both "
            + "even and skewed, and two on `Arranged`'s withheld set where the "
            + "prediction was that it would be damage. `Repeated` stays as the "
            + "control rather than going with a revival row, because it is the "
            + "baseline every reading recorded before it sits on -- the one arm here "
            + "whose job is to be the old number",

        ["Choosing"] =
            "the control arm, and a choice between two rules that both do "
            + "something rather than a mechanism and its own absence. WHICH rule "
            + "picks the added condition is not a quantity",

        ["Surprising"] =
            "which rule decides a moment was unaccounted for, and not a quantity "
            + "either -- `AnyFailure` is the arm and it is what ran before this was "
            + "mounted at all. THE THING A CONTROLLER WOULD HUNT IS ALREADY GONE: "
            + "minting on every failure walks the whole code-to-outcome space, which "
            + "on winnowed CIFAR is 25,600 claims and reached 23,762. Gating on "
            + "whether ANYTHING proposed what arrived is self-limiting by "
            + "construction -- promiscuous while the population accounts for nothing "
            + "and quiet once it does -- so there is no level here to aim at",

        // ---- arrived from the worlds, 2026-08-04 ---------------------------
        //
        // none of these is new. Every one was already a dial, passed to a `*Run`
        // constructor where this census could not see it -- so the budget below
        // jumping from seven is the CHECK BEING FIXED rather than the system
        // growing knobs. Several had DIFFERENT DEFAULTS in different worlds,
        // which is the sharpest form of the fault: `Ranking` was `Sum` on bAbI
        // and `Agreement` on CLEVR, so a world decided how the brain thought.

    };

    /// <summary>
    /// What a dial is allowed to move: the RANKING, the PRICE, or both.
    /// </summary>
    /// <remarks>
    /// <b>One weight doing two jobs is this design's recurring fault.</b> And this is
    /// the cheap detector for it. An edge weight both ranks a partner and
    /// prices the hop to it, so a change meaning to improve one has twice now
    /// silently wrecked the other — `Pricing.Sender` moves the ranking while
    /// meaning to move the price, and `Doubt` applied to both destroyed the
    /// senses world before being narrowed to the score.
    /// <para>
    /// <b>The price leaves a fingerprint the ranking cannot fake:</b> what a hop
    /// costs decides where routes die, so it decides how many messages are sent.
    /// A ranking-only dial must therefore leave the message count EXACTLY
    /// unchanged on a fixed seed — same walk, same places, different mind about
    /// what it found. Anything else is the two jobs bleeding into each other.
    /// </para>
    /// </remarks>

    [Fact]
    public void Every_dial_is_either_driven_or_has_a_written_reason_it_is_not()
    {
        // Both settings types, because this file has already measured its own blind
        // spot once. It enumerated `WalkSettings` while eleven dials sat in `*Run`
        // constructors where it could not look. A second brain arriving with seven
        // knobs of its own would have repeated that exactly, and the census would
        // have reported the same thirteen while the real number was twenty.
        var dials = Census()
            .Select(one => one.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(dials);

        var accounted = Driven.Keys.Concat(HandSet.Keys).ToHashSet(StringComparer.Ordinal);

        var unexplained = dials.Except(accounted).Order(StringComparer.Ordinal).ToList();

        Assert.True(unexplained.Count == 0,
            $"new dial(s) with nothing said about them: {string.Join(", ", unexplained)}. "
            + "Give it a controller, or write down why it cannot have one.");

        // AND THE OTHER DIRECTION, or the lists rot into a record of dials that
        // used to exist -- which is the exact failure the doc's ticked boxes are
        // checked for.
        var gone = accounted.Except(dials).Order(StringComparer.Ordinal).ToList();

        Assert.True(gone.Count == 0,
            $"named here and no longer a dial: {string.Join(", ", gone)}");
    }

    [Fact]
    public void A_dial_is_not_in_both_lists()
    {
        var both = Driven.Keys.Intersect(HandSet.Keys, StringComparer.Ordinal).ToList();

        Assert.True(both.Count == 0,
            $"claimed as driven AND as hand-set: {string.Join(", ", both)}");
    }

    [Fact]
    public void The_number_of_hand_set_dials_is_visible_and_does_not_grow()
    {
        // The budget, and the point of the whole file. The number is arbitrary;
        // having one is not. It sits AT the current count rather than above it,
        // because unlike a doc there is no ordinary edit that should raise this —
        // every new hand-set dial is a decision worth arguing about, and every
        // one retired should lower the cap behind it.
        //
        // It has moved in both directions already, and that is the mechanism
        // working. `Doubt` arrived within the hour of this file being written
        // and failed the build until somebody said what it was. `Value` left
        // when `ArrivalValue.Lift` was deleted, because an enum with one member
        // is a dial that chooses nothing.
        //
        // RAISED TO SIX FOR `Toll`, which is the argument this file wants had.
        // It buys the split the plan has called outstanding three times over: a
        // sixth knob against the row entry finally doing one job.
        //
        // AND TO SEVEN FOR `Row`, which is a different kind of argument. It is not
        // a level to be found: what a node can afford to hold is a fact about the
        // machine rather than about the run, so it belongs with `Pricing` and
        // `Toll` as a knob nothing in the walk could hunt. What it buys is the only
        // forgetting this design has -- the bet is that nothing can be unlearned,
        // only outvoted, and until now there was no way to test whether that bet is
        // survivable.
        // It went to eighteen and then back to twelve, and neither move was about
        // knobs being added or removed. John moved the dials out of the worlds and
        // into the brain on 2026-08-04, and eleven of them had been `*Run`
        // constructor arguments -- somewhere this census could not look, because it
        // enumerates `WalkSettings`. The budget was measuring its own blind spot: a
        // file whose job is to notice a sixth dial arriving beside five could not
        // see eleven of them.
        //
        // Then six went away the same day, and that WAS a real fall. `Kinds`,
        // `Surprising`, `Gated`, `Chunking`, `Recent` and `IncludeEmpty` were all
        // on/off flags, and John's rule is that you build it and it is ON. A dial
        // with two positions where one of them is "not running" was never a level
        // to hunt, so removing it removes a question nobody could have answered.
        // `Budget` went with them: the controller is unconditional now, so it is
        // not a switch and not a level either.
        //
        // AND TO THIRTEEN FOR `Fanout`, which is the argument this file exists to
        // force. It is not a level: the WIDTH is the quantity that could be hunted
        // and a node already sets that from its own row's widest gap, so what is
        // left is which of two named rules applies -- the same shape as `Pricing`
        // and `Toll`. It arrived as a bool and as a swept integer beside it, and
        // both were wrong: `FlagTests` refuses an on/off switch and the integer was
        // a ruler kept after it had been read. What it buys is the only change
        // measured here with orders of magnitude in it, and it is BETTER rather
        // than merely cheaper on the one world where the chain is load-bearing.
        //
        // THIRTEEN IS THE HONEST NUMBER, and it is still much worse than seven.
        //
        // And to twenty for the commitment branch, which is seven at once and has to
        // be argued for rather than noted. Three of them are not levels at all --
        // `Floor` is a property of the test, `Capacity` is a fact about the machine,
        // `Choosing` is which of two rules applies. `Budget` is a guard that has
        // already been caught binding. That leaves `Recency`, `Alpha` and
        // `Sharpness` as knobs somebody will have to find a signal for, and two of
        // the three have one sitting unused: the switching world for the first and
        // the vote's own margin for the last.
        //
        // The census was extended in the same edit, and that matters more than the
        // count. A second brain with its own settings record would have been
        // invisible to this file exactly as eleven `*Run` arguments once were.
        //
        // AND TO TWENTY-ONE FOR `Surprising`, which is the same name coming back in a
        // different shape and owes an argument for that. It left in the cull above as
        // an on/off flag, and John's rule is right: a switch whose off position is
        // "not running" was never a level to hunt. What returns is two named rules
        // that BOTH mint -- `AnyFailure` on any failure at all, `Unaccounted` only
        // where nothing that fired proposed what arrived -- which is the shape
        // `Choosing` and `Fanout` already have, and it is a comparison rather than a
        // mechanism beside its own absence.
        //
        // It also earned the return, which a flag never did. On winnowed CIFAR the
        // old rule minted 414,087 commitments in twenty thousand rounds against
        // 23,296, ran seven and a half times slower, and scored LOWER. A dial nobody
        // can hunt is still worth having when the two rules differ by that much.
        // AND TO TWENTY-THREE FOR `Mending`, which is the third in a row and is why
        // the count rising twice in one session is the finding rather than the cost.
        // `Sharpness`, `Weighing` and `Mending` were read as each having a best value
        // that moves between two worlds with no combination best on both. Two of the three
        // are deleted now and the third turned out to be two settings crossed, so the row
        // this paragraph was written to explain does not survive any of them.
        //
        // Fork 37 names the signal all three are standing in for: whether a parent
        // still has failures no child covers. It is vote-independent and
        // world-independent, the repair gate already computes most of it, and it
        // separates the two cases these dials keep splitting the difference between.
        // The next entry in this list should be its DELETION of three of them.
        //
        // And back to twenty-four, which is the first time this number has fallen for
        // the right reason. `Rooting` was here for one commit while it was being compared
        // against the thing it replaced; it won at twelve seeds -- 7.4 standard errors
        // ahead where there is background, 0.2 apart where there is none -- so it BECAME
        // the code and its arm was deleted. An arm that has won and stayed an arm is dead
        // code with a comparison attached.
        // And to twenty-five, which is a name arriving rather than a decision. `Mending`
        // was one setting deciding a gate and a timing at once, so the census has been
        // counting one entry for two things the machine was always doing -- `Uncovered`
        // and `Improving` repaired every round while `Outvoted` and `Neglected` waited for
        // a failure, and no comparison against the list could move either axis alone.
        //
        // So the honest count went up when the conflation was removed, and this file is
        // the one place where that reads as progress rather than sprawl. A number that can
        // only fall is a number people keep by hiding things inside existing names, which
        // is exactly what happened here.
        //
        // And it did not fall to twenty-three, which is the measurement answering a
        // different question from the one this file asked. The trigger was `Strongest`
        // against `Summing` at ONE fixed power on more than one world, and at the shipped
        // power of five over eight worlds the sum led on NONE of them -- so by the rule as
        // written both entries went.
        //
        // The deletion was carried out and then undone, which is why this is evidence and
        // not a hesitation. With the sum gone, the six-bit multiplexer falls from 0.963 to
        // 0.926 on seed one and finds five of the world's eight rules where the sum finds
        // seven, holding 31 residents against 47. And `Abstracting.Shared` returns NOTHING
        // at eleven bits, so rung five names nothing and the three tests that measure the
        // counts merge lose their subject entirely.
        //
        // So the rule asked whether the sum wins and the answer came back once the vote
        // stopped steering the search. Under `Repairing.EveryRound` all three weighings
        // build populations equal PER SEED, so a vote arm is a readout at last -- and over
        // ten worlds `Strongest` leads on three and the sum on none. Both losers are
        // deleted with revival rows, and `Sharpness` goes with the arm it parameterised.
        //
        // Which is why the count falls by two here and the fall is the finding. Every
        // earlier reading of these two was taken while the vote decided what repair ran on,
        // so it moved the search and the readout at once; the timing separated them and the
        // question answered itself.
        //
        // AND TWENTY-SIX WAS `Widening`, which is the ladder's other direction arriving as
        // an arm rather than as a setting, and it is gone again -- see the fourteen below.
        // It is left in this history because the row it wrote is the rule working: the
        // count grows when a mechanism is added and MEASURED, and shrinks only when one is
        // deleted with a revival row, and this entry did both within a fortnight.
        //
        // AND TWENTY-EIGHT IS `Budgeting`, which is the only one here that adds no freedom
        // at all. It does not set a level or pick between two behaviours a world could
        // want differently -- it says which QUESTION `Budget` is asking, and the answer
        // turned out to be that the shipped one has never asked about children. A child
        // adds one code, so distinct children are capped by the vocabulary far below the
        // budget, and `Children` cannot bind on any world this repo has.
        //
        // So the honest end of this row is a deletion and it needs the world that would
        // show it. By this file's own rule an arm only lives while it is compared, and
        // `Children` is currently indistinguishable from removing the budget -- but
        // `BudgetTests` says removing it outright is worse at every width under the
        // shipped timing, so the two are not the same claim and the cell that separates
        // them is a world whose vocabulary reaches sixty-four. `BudgetingTests` carries
        // the tripwire that fires on the day one arrives.
        // AND `Minting` left as an arm, which took it back to twenty-eight -- but not by the
        // condition that was written down for it. That condition said the row goes if naming
        // until the gate refuses raises neither `named` nor `stacked` outside the seed
        // spread. It raised both, by a lot, on both worlds that name anything at all -- and
        // it is deleted anyway, because hard-round coverage fell 2.7 standard errors while
        // they rose. A pre-registered condition written on columns a skewed world can raise
        // is a pre-registration of the wrong question, and passing one is not a defence.
        //
        // AND TWENTY-SEVEN IS `Forking`, which arrives because the arm before it failed in
        // a direction that named this one. A two-code step made each attempt reach DEEPER
        // and lost coverage by overshooting the world's minimum sound depth; this makes each
        // attempt reach somewhere ELSE at the same depth, so the failure that closed fork 74
        // does not carry over. The count going up for a second search arm in one session is
        // the finding rather than the cost -- both were pre-registered for deletion.
        //
        // `Stepping` arrived and left without this number ever being pushed, which is the
        // first time that has happened and is what this file is for. Its entry said the
        // honest end was a deletion the day the reading landed; the reading landed and it
        // went. A repair stepping two codes at once loses hard-round coverage by two to four
        // standard errors on three worlds, and its carriers overshoot the world's minimum
        // sound depth by nine tenths of a code.
        //
        // And the count not moving is the point rather than an accident. A dial whose
        // deletion was pre-registered costs nothing to try, so the budget this file keeps is
        // a budget on dials that STAY -- which is the only version of it that does not make
        // measuring an idea more expensive than not measuring it.
        //
        // AND TWENTY-EIGHT IS `Coarsening`, WHICH ARRIVED AS `Recasting` with three positions
        // and lost two of them in the same session. Fork 85 asked for an operator that
        // PROPOSES the coarse claim; it was built, measured over three seeds, and cost 60
        // rules where reading the entailment cost none. So the two proposing positions are
        // deleted with a revival row and what is left is a judge.
        //
        // The count did not fall with them, and that is the honest bookkeeping. A dial that
        // arrives and loses most of itself in one session still leaves one behind, and this
        // file's own rule is that the budget is on dials that STAY.
        // AND `Sequencing` arrived and went in one session without this number staying up,
        // which is the second time that has happened and is what this file is for. Rung
        // three shipped for one commit as a three-armed dial defaulting to OFF, justified
        // by every recorded number being reproduced -- and John caught it. The closure lost
        // its comparison on cost, so it is deleted with a revival row; with one arm left
        // there is nothing to switch, and the mechanism is simply on. See
        // `A_dial_that_ships_off_has_a_refutation_behind_it`, which is the check that stops
        // the next one.
        // And fifteen is the walk going, which is the biggest fall this number has ever
        // taken and the least interesting. Thirteen of the twenty-eight were the walk's --
        // `Pricing`, `Toll`, `Doubt`, `Row`, `Span`, `Ranking`, `Carried`, `Depth`, `Names`,
        // `Fanout`, `Horizon`, `Reflect`, `Foresight` -- and they left with the code rather
        // than by being driven or refused. Nothing was solved by this drop, which is worth
        // saying because a budget falling usually means work was done.
        //
        // And to fourteen for `Widening`, which is the other kind of fall. It was refuted
        // on both of its arms on its own pre-registered ship gate and deleted with revival
        // rows, so the entry left by the road this file says a losing dial leaves by rather
        // than with a subsystem it happened to be attached to. `Owed` is empty behind it,
        // which is the first time nothing in the tree ships in a position that does
        // nothing.
        //
        // And to fifteen for `Rooting`, which is a RISE and is written down as one. How wide a
        // scope genesis may mint arrived as an arm rather than a level, with two values and
        // nothing between them, and it has been run on one world. A controller chosen on one
        // world's evidence is the fault this file exists for; the entry leaves when a generated
        // world has put the two arms against each other.
        //
        // And to sixteen for `Crediting`, which is the same rise for the same reason and is
        // written down as one. Both left the same session and both are waiting on a generated
        // world rather than on an argument.
        //
        // And to seventeen for `Admitting`, the same rise for the same reason. It is fork 86's
        // answer and it wins on the conversation by every column at once, which is the shape
        // that most deserves a second world before it becomes a default anywhere but the host.
        //
        // And down to sixteen, because `Speaking` is deleted. Its revival row asked for a
        // world where the right rule is present and merely outvoted; a drawn lesson at eight
        // tellings is one, coverage says every truth is held, and refusing the untested a vote
        // took the score from 1.000 to 0.125 while silence went from a thirtieth of the rounds
        // to a quarter. It does not reseat a wrong rule, it silences the right one -- a fact
        // stated a handful of times cannot make a rule that clears a floor of twenty.
        //
        // And `Rooting` and `Crediting` have had the reading their entries asked for, and both
        // STAY. A generated world has now put each pair against the other -- drawn lessons and
        // `Arranged` for the root, drawn lessons for the credit -- and what came back is an
        // interaction rather than two defaults. The wide root pays under withheld claiming and
        // nowhere else; crediting turns a tie into an outranking for the identical score; the
        // three arms together answer a paper never sat, told once.
        //
        // What stopped the root shipping was named work rather than a preference, and the work
        // is done. Flipping it hands genesis a sound multiplexer rule, so `StepOneTests`'
        // `blind.Sound` -- the assertion that says REPAIR did the learning rather than genesis
        // -- went from nought to one. `Population.Births` records the operator per commitment
        // and the assertion now says what it always meant, so the root is the default and
        // `Rooting` leaves this budget.
        //
        // And the count does NOT fall with it, which is the honest bookkeeping. `Rooting` is
        // still a dial nothing drives; what changed is which arm is the default. `Singly` is
        // not deleted because `Wholly` mints everything it mints and one scope besides -- so
        // the narrow arm is the control that says what the extra scope costs, and three grids
        // pin it by name to keep a baseline that scope removes. An arm pinned in three grids
        // is compared rather than parked.
        //
        // `Crediting` stays and its entry is rewritten. It has had its generated world and
        // what came back is not a win: it CONVERTS a tie into an outranking for the identical
        // score, the newest mint being the strongest because the older ones have missed since.
        // Breaking a tie by recency is not breaking it by correctness.
        Assert.Equal(16, HandSet.Count);
    }

    /// <summary>
    /// Every arm of every dial is set by something — <b>an arm nothing ever selects has
    /// never been compared, whatever its reason says.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE WEAKEST POSSIBLE FORM</b> OF <i>an arm only lives while it is compared</i>, AND
    /// THINGS STILL FAIL IT. This does not ask whether an arm won, or on how many
    /// worlds, or against what — only whether any test anywhere ever names it. `Fanout` has
    /// two arms and neither has ever been written down outside its own declaration, so it
    /// has been a choice nobody has ever made for the life of the branch.
    /// </remarks>
    [Fact]
    public void Every_arm_of_every_dial_is_selected_by_something()
    {
        var sources = Directory
            .GetFiles(Path.Combine(Tree.Repo(), "tests", "OpenPlexus.Tests"), "*.cs")
            .Where(one => Path.GetFileName(one) != "DialTests.cs")
            .Select(File.ReadAllText)
            .ToList();

        var unselected = new List<string>();

        foreach (var dial in Census())
        {
            if (!dial.PropertyType.IsEnum) continue;

            // The same exemption list as the two-world bar, because an arm nothing selects
            // and a dial nothing measures are one situation read at two grains -- and every
            // entry on it today is on the walk learner, which is going.
            if (Waiting.ContainsKey(dial.Name)
                || Waiting.ContainsKey(dial.PropertyType.Name)) continue;

            foreach (var arm in Enum.GetNames(dial.PropertyType))
            {
                var named = $"{dial.PropertyType.Name}.{arm}";

                if (!sources.Any(one => one.Contains(named, StringComparison.Ordinal)))
                    unselected.Add(named);
            }
        }

        Assert.True(unselected.Count == 0,
            $"arm(s) no test selects yet: {string.Join(", ", unselected)}. Nothing has "
            + "compared these, so nobody can say whether they help -- which is a gap in what "
            + "we know rather than a fault in the code. Give one a comparison and we learn "
            + "something; delete it with a revival row and we lose nothing, because the "
            + "revival row says exactly what would bring it back");
    }

    /// <summary>
    /// Every arm dial is measured on at least TWO worlds — <b>because one world's grid is a
    /// verdict on the world.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>John's bar</b>, and it is this repo's own trap said as a rule. <i>A grid of
    /// identical rows is a verdict on the worlds rather than on the arm</i>, and <i>a grid
    /// can rank arms on columns a skewed world raises for free</i>. So a mechanism measured
    /// in one place has a number and not a comparison, however many seeds it took.
    /// </para>
    /// <para>
    /// <b>By what the tests actually build.</b> And the first version of this check asked the
    /// reason text instead and was wrong. Reading the written reason for world names
    /// measures whether somebody happened to put them in backticks — eleven of fourteen
    /// dials failed it, including several measured on six worlds — so it would have bought
    /// a round of cosmetic edits and no coverage at all. What a test CONSTRUCTS is the
    /// fact; what its author wrote about it is not.
    /// </para>
    /// <para>
    /// <b>And the exemptions are a ratchet, on <c>DeadCodeTests</c>' PATTERN.</b> Each entry
    /// is one dial that fails today with what it is waiting for. The list may only shrink;
    /// adding to it wants John and a reason in the commit message.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_arm_is_measured_on_at_least_two_worlds()
    {
        var thin = new List<string>();

        foreach (var (name, kind) in Arms())
        {
            if (Waiting.ContainsKey(name) || Waiting.ContainsKey(kind.Name)) continue;

            var arms = Enum.GetNames(kind);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var path in Directory.GetFiles(
                Path.Combine(Tree.Repo(), "tests", "OpenPlexus.Tests"), "*.cs"))
            {
                if (Path.GetFileName(path) == "DialTests.cs") continue;

                var source = File.ReadAllText(path);

                // A file that never names the dial says nothing about it, whatever worlds it
                // builds. Both forms count: selecting an arm by name, and assigning the
                // property in a settings initialiser.
                if (!arms.Any(arm => source.Contains(
                        $"{kind.Name}.{arm}", StringComparison.Ordinal))
                    && !System.Text.RegularExpressions.Regex.IsMatch(
                        source, $@"\b{name}\s*[=:]"))
                    continue;

                foreach (var world in Worlds)
                    if (System.Text.RegularExpressions.Regex.IsMatch(
                            source, $@"\bnew {world}\s*[({{]|\bFixture\.{world}\b|\b{world}Settings\b|\b{world}Run\b"))
                        seen.Add(world);
            }

            if (seen.Count < 2) thin.Add($"{name} ({seen.Count})");
        }

        Assert.True(thin.Count == 0,
            $"dial(s) measured on one world so far: {string.Join(", ", thin)}. One world's "
            + "grid is a verdict on the world rather than on the arm, so a second world is "
            + "what turns a number into a comparison -- that is the whole of what this "
            + "wants. A second world, a deletion with a revival row, or an entry in "
            + "`Waiting` saying what it is waiting for are all good answers");
    }

    /// <summary>Every world this repo has, by name, for the check above.</summary>
    private static readonly HashSet<string> Worlds =
        [.. Directory
            .GetFiles(
                Path.Combine(Tree.Repo(), "src", "OpenPlexus", "Worlds"),
                "*.cs",
                SearchOption.AllDirectories)
            .Select(one => Path.GetFileNameWithoutExtension(one))
            .Where(one => !one.EndsWith("Run", StringComparison.Ordinal))];

    /// <summary>
    /// Dials that fail the two-world bar today, with what each is waiting for.
    /// </summary>
    /// <remarks>
    /// <b>THE LIST MAY ONLY SHRINK.</b> It is not a place to put a new dial — a mechanism
    /// arriving today can be measured on two worlds today, and one that cannot is one
    /// nobody can rank. These are the ones that predate the bar.
    /// </remarks>
    private static readonly Dictionary<string, string> Waiting = new(StringComparer.Ordinal)
    {
        ["Coarsening"] =
            "`Returning` ONLY, and it is the one dial where that may be honest rather than "
            + "owed: it reads a vocabulary of alternatives and no other world hands one in, "
            + "so it is INERT everywhere else by construction. What it is waiting for is a "
            + "second world whose front end derives its own categories -- which is "
            + "`Alternating`'s wiring, and blocked on when a front end re-derives",

        ["Choosing"] =
            "`Multiplexer` ONLY, AND IT ALWAYS WAS -- this entry is a check that was passing "
            + "for a wrong reason, found by the walk going. Its second world was "
            + "`HomeostatTests`, which named `Choosing` as a field of the walk's run result "
            + "holding an `Attending`, and the attribution matches on the WORD. So a dial "
            + "measured on one world read as measured on two because an unrelated type had "
            + "a property spelt the same. What it is waiting for is `Arranged` or `Roaming`, "
            + "which nothing stops beyond nobody having done it -- and the sharper lesson is "
            + "that a name-matching census cannot tell a dial from its homonym",

    };

    /// <summary>
    /// No dial ships in a do-nothing position — <b>full stop, and a written reason is not a
    /// way past it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>John's rule</b>, and it replaced a weaker one of mine the same day. My first
    /// version let a dial ship OFF if its type was named in the plan's refutation table —
    /// on the reasoning that a refuted mechanism legitimately ships off. He pointed out the
    /// hole: <i>writing a reason is easy</i>, and a check whose escape hatch is prose is a
    /// check you can talk your way around.
    /// </para>
    /// <para>
    /// <b>And the stronger rule is also the simpler one.</b> Because a dial is only ever one of
    /// two things. Either it is a NEW ability, in which case it is on — there is no
    /// other reason to have built it — and it is kept while it is being made to work, or
    /// deleted when it will not. Or it REPLACES something, in which case both arms are live
    /// while they are compared, and afterwards the winner is the code and the loser is
    /// gone. <b>Neither road ends at a dial whose default does nothing.</b>
    /// </para>
    /// <para>
    /// <b>So a dial that would ship off is a dial that should not exist</b>, and the fix is
    /// to delete the mechanism with a revival row rather than to explain the default. The
    /// code is not lost — it is in the history, and the revival row is what says when to go
    /// and get it.
    /// </para>
    /// <para>
    /// <b>And deletion is not the only move available: adjusting a losing arm is allowed.</b>
    /// A mechanism that lost as built may be worth one more shape before it goes, and this
    /// repo has read that as <i>delete immediately</i> more than once. What is not allowed
    /// is leaving it switched off while nobody decides.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_dial_ships_in_a_position_that_does_nothing()
    {
        var idle = new List<string>();

        foreach (var dial in Census())
        {
            if (!dial.PropertyType.IsEnum || Owed.ContainsKey(dial.Name)) continue;

            var settings = Activator.CreateInstance(dial.DeclaringType!);

            // `Never` by name, which is this repo's own word for the position where nothing
            // happens. A check inferring which arm is inert would be guessing at behaviour
            // from a type; the naming convention is a decision somebody made on purpose and
            // is what a reader goes by too.
            if (dial.GetValue(settings)?.ToString() == "Never") idle.Add(dial.Name);
        }

        Assert.True(idle.Count == 0,
            $"dial(s) shipping in a position that does nothing: {string.Join(", ", idle)}. "
            + "A dial is either a new ability -- in which case turn it on, that is why it "
            + "was built -- or a replacement, in which case both arms run until one wins and "
            + "the loser goes. Neither ends here. If the mechanism lost, delete it with a "
            + "revival row: the code stays in the history and the row says when to fetch it. "
            + "And if it lost as BUILT rather than as an idea, adjusting it and running again "
            + "is a perfectly good third answer");
    }

    /// <summary>
    /// Dials shipping off today, each owed a deletion rather than an explanation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This list may only shrink.</b> And an entry is cleared by deleting the dial rather
    /// than by improving its reason. That is the whole difference between this and the
    /// check it replaced. Nothing new may be added — a mechanism arriving today ships on.
    /// </para>
    /// <para>
    /// <b>It is empty, and it got there by its own rule.</b> `Widening` was the only entry
    /// and it left by the deletion the entry asked for rather than by a better reason being
    /// written under it. The dictionary stays because the exemption it grants is what
    /// <see cref="No_dial_ships_in_a_position_that_does_nothing"/> reads, and a list
    /// deleted the day it reaches nought cannot be seen to have reached nought.
    /// </para>
    /// </remarks>
    private static readonly Dictionary<string, string> Owed = new(StringComparer.Ordinal)
    {
    };

    /// <summary>
    /// A dial shipping in its DO-NOTHING position is named in the plan's refutation table —
    /// <b>the budget for building something better and leaving it switched off.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>There are exactly two honest reasons to ship `Never`, and one of them leaves a
    /// trace.</b> Either the mechanism LOST its comparison — in which case this repo's own
    /// rule says the loser is deleted and leaves a revival row — or it is the first thing of
    /// its kind and must be on. What is forbidden is the third: built, better, and left off
    /// so that the numbers already recorded do not have to be re-taken. <i>An arm only lives
    /// while it is compared</i>, and <i>a better brain beats intact numbers</i>.
    /// </para>
    /// <para>
    /// <b>So the check is the trace rather than the intent.</b> Which is the only part a build
    /// can read. A refuted mechanism is named in DO NOT RE-TRY with what would revive it;
    /// a mechanism switched off to protect a baseline is named nowhere, because there is
    /// nothing to say. The second is what this fails on.
    /// </para>
    /// <para>
    /// <b>It has no subject today</b>, which is said here rather than left to be discovered.
    /// `Widening` was the last dial shipping `Never` and it is deleted, so this passes over an
    /// empty set — a tripwire rather than a reading. It was written against a case that
    /// passed and a case that failed: `Widening` was refuted twice over in the table, and
    /// `Sequencing` shipped `Never` for one commit with no row anywhere because there was no
    /// refutation — it had WON — and this is the check that would have said so before John
    /// had to.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_dial_that_ships_off_has_a_refutation_behind_it()
    {
        var plan = File.ReadAllText(Path.Combine(Tree.Repo(), "docs", "plan.md"));
        var unexplained = new List<string>();

        foreach (var dial in Census())
        {
            if (!dial.PropertyType.IsEnum) continue;

            // The declaring type rather than a hand-kept list, so a settings record nobody
            // told this check about is still asked. `Activator` because a required member is
            // a compile-time check and this never writes one.
            var settings = Activator.CreateInstance(dial.DeclaringType!);
            var shipped = dial.GetValue(settings)?.ToString();

            // `Never` by name, which is this repo's own word for the position where nothing
            // happens. A check inferring which arm is inert would be guessing at behaviour
            // from a type; the naming convention is a decision somebody made on purpose and
            // is what a reader goes by too.
            if (shipped != "Never") continue;

            if (!plan.Contains(dial.PropertyType.Name, StringComparison.Ordinal))
                unexplained.Add(dial.PropertyType.Name);
        }

        Assert.True(unexplained.Count == 0,
            $"dial(s) shipping in their do-nothing position with no refutation recorded: "
            + $"{string.Join(", ", unexplained)}. If the mechanism lost, delete the loser "
            + "and leave a revival row in DO NOT RE-TRY -- that is the good outcome and the "
            + "row is what makes it reusable. If it did NOT lose, it has earned its place: "
            + "turn it on and re-take whatever goes red, because the old numbers are safe in "
            + "the commits and a better brain is worth more than an intact baseline");
    }
}
