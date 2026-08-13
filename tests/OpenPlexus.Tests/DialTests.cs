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
/// <b>ADDING A DIAL FAILS THIS TEST UNTIL SOMEBODY SAYS WHICH IT IS.</b> Either
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
    /// Dials nothing drives, each with the reason. <b>A reason, not an excuse</b>
    /// — several of these say outright that nobody has found the signal yet.
    /// </summary>
    private static readonly Dictionary<string, string> HandSet = new(StringComparer.Ordinal)
    {
        // ---- ARRIVED WITH THE COMMITMENT BRANCH ----------------------------

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
            + "ARITHMETIC AND NOT A MISSING WORLD: `Mend` charges an attempt and "
            + "adds a name in the same two lines, and `Forking.Distinct` refuses a "
            + "parent every code it has spent -- so two codes are two scopes are "
            + "two identities, and the set and the counter move together forever. "
            + "`Children` is FREE under `Repeated` and a SYNONYM under `Distinct`, "
            + "with no third thing available; measured bit-identical on two worlds "
            + "and 36 against 452 repairs under the other rule. It is kept as "
            + "exactly that check, which goes red the day forking changes",

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

        ["Speaking"] =
            "whether a commitment may vote before it has been tested, and a "
            + "mechanism against its own absence because there is no second way "
            + "to not refuse. It needs NO NEW NUMBER, which is the only reason it "
            + "was buildable: `Floor` already means enough firings to judge a "
            + "proportion by, and subsumption and culling both refuse to weigh "
            + "anything beneath it while the vote never asked. AND IT IS INERT, "
            + "which is the finding. Untested rules DO decide wrong rounds -- "
            + "about a sixth of them at eleven bits -- and refusing them a vote "
            + "moves no metric on any world, because those rounds are uncovered "
            + "anyway and the seat passes to a different wrong rule. Smallest "
            + "exactly where the failure is total, so it is not the cause there "
            + "either. Not a level; nothing to hunt",

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

        ["Widening"] =
            "the ladder's other direction. It began as a mechanism against its own "
            + "absence and is two rules that do something now, because the failure "
            + "named the axis: WHAT SUMMONS a shortening, a clean record or two "
            + "clean rules agreeing a code is droppable. "
            + "Measured ON from the baseline every earlier number was taken under, "
            + "which is what this repo's trap list asks for. NOT A LEVEL: whether "
            + "anything generalises is not a quantity. AND IT IS REFUTED AS BUILT, "
            + "which is why it ships off. `Unmissed` selects the commitments with "
            + "the LEAST evidence -- never having missed is nearly free for a "
            + "narrow rule, because a narrow rule barely fires -- and dropping a "
            + "code from a sound scope usually makes it unsound, so it mints about "
            + "four wrong rules per right one and each has wider reach than its "
            + "parent. It buys hard-round coverage at eleven bits and costs "
            + "accuracy on every world. AND THE GATE THAT WAS SUPPOSED TO FIX IT "
            + "IS REFUTED: reading how much a rule has been TESTED is bit-identical "
            + "to not reading it, because `Floor` already demands twenty firings "
            + "and a perfect twenty is significant against every base rate under "
            + "0.88. What is wrong with it is the shortening rather than the "
            + "parent, and it pins the population at its capacity. SO THE SECOND "
            + "ARM ASKS THE POPULATION WHAT NO TABLE CAN: a rule's tally skips its "
            + "own scope codes, so nothing inside a commitment can name its "
            + "redundant one -- but two clean rules agreeing on everything except "
            + "one code, and on what follows, are evidence about that code from "
            + "outside either of them. Rung five's trigger pointing down instead of "
            + "up, and it proposes 34 shortenings where the other proposes 1715",

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

        // ---- ARRIVED FROM THE WORLDS, 2026-08-04 ---------------------------
        //
        // NONE OF THESE IS NEW. Every one was already a dial, passed to a `*Run`
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
    /// <b>ONE WEIGHT DOING TWO JOBS IS THIS DESIGN'S RECURRING FAULT, AND THIS IS
    /// THE CHEAP DETECTOR FOR IT.</b> An edge weight both ranks a partner and
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
        // BOTH SETTINGS TYPES, BECAUSE THIS FILE HAS ALREADY MEASURED ITS OWN BLIND
        // SPOT ONCE. It enumerated `WalkSettings` while eleven dials sat in `*Run`
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
        // THE BUDGET, AND THE POINT OF THE WHOLE FILE. The number is arbitrary;
        // having one is not. It sits AT the current count rather than above it,
        // because unlike a doc there is no ordinary edit that should raise this —
        // every new hand-set dial is a decision worth arguing about, and every
        // one retired should lower the cap behind it.
        //
        // IT HAS MOVED IN BOTH DIRECTIONS ALREADY, AND THAT IS THE MECHANISM
        // WORKING. `Doubt` arrived within the hour of this file being written
        // and failed the build until somebody said what it was. `Value` left
        // when `ArrivalValue.Lift` was deleted, because an enum with one member
        // is a dial that chooses nothing.
        //
        // RAISED TO SIX FOR `Toll`, WHICH IS THE ARGUMENT THIS FILE WANTS HAD.
        // It buys the split the plan has called outstanding three times over: a
        // sixth knob against the row entry finally doing one job.
        //
        // AND TO SEVEN FOR `Row`, WHICH IS A DIFFERENT KIND OF ARGUMENT. It is not
        // a level to be found: what a node can afford to hold is a fact about the
        // machine rather than about the run, so it belongs with `Pricing` and
        // `Toll` as a knob nothing in the walk could hunt. What it buys is the only
        // forgetting this design has -- the bet is that nothing can be unlearned,
        // only outvoted, and until now there was no way to test whether that bet is
        // survivable.
        // IT WENT TO EIGHTEEN AND THEN BACK TO TWELVE, AND NEITHER MOVE WAS ABOUT
        // KNOBS BEING ADDED OR REMOVED. John moved the dials out of the worlds and
        // into the brain on 2026-08-04, and eleven of them had been `*Run`
        // constructor arguments -- somewhere this census could not look, because it
        // enumerates `WalkSettings`. THE BUDGET WAS MEASURING ITS OWN BLIND SPOT: a
        // file whose job is to notice a sixth dial arriving beside five could not
        // see eleven of them.
        //
        // THEN SIX WENT AWAY THE SAME DAY, and that WAS a real fall. `Kinds`,
        // `Surprising`, `Gated`, `Chunking`, `Recent` and `IncludeEmpty` were all
        // on/off flags, and John's rule is that you build it and it is ON. A dial
        // with two positions where one of them is "not running" was never a level
        // to hunt, so removing it removes a question nobody could have answered.
        // `Budget` went with them: the controller is unconditional now, so it is
        // not a switch and not a level either.
        //
        // AND TO THIRTEEN FOR `Fanout`, WHICH IS THE ARGUMENT THIS FILE EXISTS TO
        // FORCE. It is not a level: the WIDTH is the quantity that could be hunted
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
        // AND TO TWENTY FOR THE COMMITMENT BRANCH, WHICH IS SEVEN AT ONCE AND HAS TO
        // BE ARGUED FOR RATHER THAN NOTED. Three of them are not levels at all --
        // `Floor` is a property of the test, `Capacity` is a fact about the machine,
        // `Choosing` is which of two rules applies. `Budget` is a guard that has
        // already been caught binding. That leaves `Recency`, `Alpha` and
        // `Sharpness` as knobs somebody will have to find a signal for, and two of
        // the three have one sitting unused: the switching world for the first and
        // the vote's own margin for the last.
        //
        // THE CENSUS WAS EXTENDED IN THE SAME EDIT, and that matters more than the
        // count. A second brain with its own settings record would have been
        // invisible to this file exactly as eleven `*Run` arguments once were.
        //
        // AND TO TWENTY-ONE FOR `Surprising`, WHICH IS THE SAME NAME COMING BACK IN A
        // DIFFERENT SHAPE AND OWES AN ARGUMENT FOR THAT. It left in the cull above as
        // an on/off flag, and John's rule is right: a switch whose off position is
        // "not running" was never a level to hunt. What returns is two named rules
        // that BOTH mint -- `AnyFailure` on any failure at all, `Unaccounted` only
        // where nothing that fired proposed what arrived -- which is the shape
        // `Choosing` and `Fanout` already have, and it is a comparison rather than a
        // mechanism beside its own absence.
        //
        // IT ALSO EARNED THE RETURN, WHICH A FLAG NEVER DID. On winnowed CIFAR the
        // old rule minted 414,087 commitments in twenty thousand rounds against
        // 23,296, ran seven and a half times slower, and scored LOWER. A dial nobody
        // can hunt is still worth having when the two rules differ by that much.
        // AND TO TWENTY-THREE FOR `Mending`, WHICH IS THE THIRD IN A ROW AND IS WHY
        // THE COUNT RISING TWICE IN ONE SESSION IS THE FINDING RATHER THAN THE COST.
        // `Sharpness`, `Weighing` and `Mending` were read as each having a best value
        // that moves between two worlds with no combination best on both. Two of the three
        // are deleted now and the third turned out to be two settings crossed, so the row
        // this paragraph was written to explain does not survive any of them.
        //
        // FORK 37 NAMES THE SIGNAL ALL THREE ARE STANDING IN FOR: whether a parent
        // still has failures no child covers. It is vote-independent and
        // world-independent, the repair gate already computes most of it, and it
        // separates the two cases these dials keep splitting the difference between.
        // The next entry in this list should be its DELETION of three of them.
        //
        // AND BACK TO TWENTY-FOUR, WHICH IS THE FIRST TIME THIS NUMBER HAS FALLEN FOR
        // THE RIGHT REASON. `Rooting` was here for one commit while it was being compared
        // against the thing it replaced; it won at twelve seeds -- 7.4 standard errors
        // ahead where there is background, 0.2 apart where there is none -- so it BECAME
        // the code and its arm was deleted. An arm that has won and stayed an arm is dead
        // code with a comparison attached.
        // AND TO TWENTY-FIVE, WHICH IS A NAME ARRIVING RATHER THAN A DECISION. `Mending`
        // was one setting deciding a gate and a timing at once, so the census has been
        // counting one entry for two things the machine was always doing -- `Uncovered`
        // and `Improving` repaired every round while `Outvoted` and `Neglected` waited for
        // a failure, and no comparison against the list could move either axis alone.
        //
        // SO THE HONEST COUNT WENT UP WHEN THE CONFLATION WAS REMOVED, and this file is
        // the one place where that reads as progress rather than sprawl. A number that can
        // only fall is a number people keep by hiding things inside existing names, which
        // is exactly what happened here.
        //
        // AND IT DID NOT FALL TO TWENTY-THREE, WHICH IS THE MEASUREMENT ANSWERING A
        // DIFFERENT QUESTION FROM THE ONE THIS FILE ASKED. The trigger was `Strongest`
        // against `Summing` at ONE fixed power on more than one world, and at the shipped
        // power of five over eight worlds the sum led on NONE of them -- so by the rule as
        // written both entries went.
        //
        // THE DELETION WAS CARRIED OUT AND THEN UNDONE, WHICH IS WHY THIS IS EVIDENCE AND
        // NOT A HESITATION. With the sum gone, the six-bit multiplexer falls from 0.963 to
        // 0.926 on seed one and finds five of the world's eight rules where the sum finds
        // seven, holding 31 residents against 47. And `Abstracting.Shared` returns NOTHING
        // at eleven bits, so rung five names nothing and the three tests that measure the
        // counts merge lose their subject entirely.
        //
        // SO THE RULE ASKED WHETHER THE SUM WINS AND THE ANSWER CAME BACK ONCE THE VOTE
        // STOPPED STEERING THE SEARCH. Under `Repairing.EveryRound` all three weighings
        // build populations equal PER SEED, so a vote arm is a readout at last -- and over
        // ten worlds `Strongest` leads on three and the sum on none. Both losers are
        // deleted with revival rows, and `Sharpness` goes with the arm it parameterised.
        //
        // WHICH IS WHY THE COUNT FALLS BY TWO HERE AND THE FALL IS THE FINDING. Every
        // earlier reading of these two was taken while the vote decided what repair ran on,
        // so it moved the search and the readout at once; the timing separated them and the
        // question answered itself.
        //
        // AND TWENTY-SIX IS `Widening`, WHICH IS THE LADDER'S OTHER DIRECTION ARRIVING AS
        // AN ARM RATHER THAN AS A SETTING. It ships OFF, so every number recorded before
        // it existed still stands and it is measured on from a known baseline -- and it is
        // already refuted as built, which is the honest reason it is a census entry rather
        // than a default: the count grows when a mechanism is added and MEASURED, and
        // shrinks only when one is deleted with a revival row.
        //
        // AND TWENTY-EIGHT IS `Budgeting`, WHICH IS THE ONLY ONE HERE THAT ADDS NO FREEDOM
        // AT ALL. It does not set a level or pick between two behaviours a world could
        // want differently -- it says which QUESTION `Budget` is asking, and the answer
        // turned out to be that the shipped one has never asked about children. A child
        // adds one code, so distinct children are capped by the vocabulary far below the
        // budget, and `Children` cannot bind on any world this repo has.
        //
        // SO THE HONEST END OF THIS ROW IS A DELETION AND IT NEEDS THE WORLD THAT WOULD
        // SHOW IT. By this file's own rule an arm only lives while it is compared, and
        // `Children` is currently indistinguishable from removing the budget -- but
        // `BudgetTests` says removing it outright is worse at every width under the
        // shipped timing, so the two are not the same claim and the cell that separates
        // them is a world whose vocabulary reaches sixty-four. `BudgetingTests` carries
        // the tripwire that fires on the day one arrives.
        // AND `Minting` LEFT AS AN ARM, WHICH TOOK IT BACK TO TWENTY-EIGHT -- BUT NOT BY THE
        // CONDITION THAT WAS WRITTEN DOWN FOR IT. That condition said the row goes if naming
        // until the gate refuses raises neither `named` nor `stacked` outside the seed
        // spread. It raised both, by a lot, on both worlds that name anything at all -- and
        // it is deleted anyway, because hard-round coverage fell 2.7 standard errors while
        // they rose. A pre-registered condition written on columns a skewed world can raise
        // is a pre-registration of the wrong question, and passing one is not a defence.
        //
        // AND TWENTY-SEVEN IS `Forking`, WHICH ARRIVES BECAUSE THE ARM BEFORE IT FAILED IN
        // A DIRECTION THAT NAMED THIS ONE. A two-code step made each attempt reach DEEPER
        // and lost coverage by overshooting the world's minimum sound depth; this makes each
        // attempt reach somewhere ELSE at the same depth, so the failure that closed fork 74
        // does not carry over. The count going up for a second search arm in one session is
        // the finding rather than the cost -- both were pre-registered for deletion.
        //
        // `Stepping` ARRIVED AND LEFT WITHOUT THIS NUMBER EVER BEING PUSHED, WHICH IS THE
        // FIRST TIME THAT HAS HAPPENED AND IS WHAT THIS FILE IS FOR. Its entry said the
        // honest end was a deletion the day the reading landed; the reading landed and it
        // went. A repair stepping two codes at once loses hard-round coverage by two to four
        // standard errors on three worlds, and its carriers overshoot the world's minimum
        // sound depth by nine tenths of a code.
        //
        // AND THE COUNT NOT MOVING IS THE POINT RATHER THAN AN ACCIDENT. A dial whose
        // deletion was pre-registered costs nothing to try, so the budget this file keeps is
        // a budget on dials that STAY -- which is the only version of it that does not make
        // measuring an idea more expensive than not measuring it.
        //
        // AND TWENTY-EIGHT IS `Coarsening`, WHICH ARRIVED AS `Recasting` WITH THREE POSITIONS
        // AND LOST TWO OF THEM IN THE SAME SESSION. Fork 85 asked for an operator that
        // PROPOSES the coarse claim; it was built, measured over three seeds, and cost 60
        // rules where reading the entailment cost none. So the two proposing positions are
        // deleted with a revival row and what is left is a judge.
        //
        // THE COUNT DID NOT FALL WITH THEM, AND THAT IS THE HONEST BOOKKEEPING. A dial that
        // arrives and loses most of itself in one session still leaves one behind, and this
        // file's own rule is that the budget is on dials that STAY.
        // AND `Sequencing` ARRIVED AND WENT IN ONE SESSION WITHOUT THIS NUMBER STAYING UP,
        // which is the second time that has happened and is what this file is for. Rung
        // three shipped for one commit as a three-armed dial defaulting to OFF, justified
        // by every recorded number being reproduced -- and John caught it. The closure lost
        // its comparison on cost, so it is deleted with a revival row; with one arm left
        // there is nothing to switch, and the mechanism is simply on. See
        // `A_dial_that_ships_off_has_a_refutation_behind_it`, which is the check that stops
        // the next one.
        // AND FIFTEEN IS THE WALK GOING, WHICH IS THE BIGGEST FALL THIS NUMBER HAS EVER
        // TAKEN AND THE LEAST INTERESTING. Thirteen of the twenty-eight were the walk's --
        // `Pricing`, `Toll`, `Doubt`, `Row`, `Span`, `Ranking`, `Carried`, `Depth`, `Names`,
        // `Fanout`, `Horizon`, `Reflect`, `Foresight` -- and they left with the code rather
        // than by being driven or refused. NOTHING WAS SOLVED BY THIS DROP, which is worth
        // saying because a budget falling usually means work was done.
        Assert.Equal(15, HandSet.Count);
    }

    /// <summary>
    /// Every arm of every dial is set by something — <b>an arm nothing ever selects has
    /// never been compared, whatever its reason says.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE WEAKEST POSSIBLE FORM OF <i>AN ARM ONLY LIVES WHILE IT IS COMPARED</i>, AND
    /// THINGS STILL FAIL IT.</b> This does not ask whether an arm won, or on how many
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

            // THE SAME EXEMPTION LIST AS THE TWO-WORLD BAR, because an arm nothing selects
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
    /// <b>JOHN'S BAR, AND IT IS THIS REPO'S OWN TRAP SAID AS A RULE.</b> <i>A grid of
    /// identical rows is a verdict on the worlds rather than on the arm</i>, and <i>a grid
    /// can rank arms on columns a skewed world raises for free</i>. So a mechanism measured
    /// in one place has a number and not a comparison, however many seeds it took.
    /// </para>
    /// <para>
    /// <b>BY WHAT THE TESTS ACTUALLY BUILD, AND THE FIRST VERSION OF THIS CHECK ASKED THE
    /// REASON TEXT INSTEAD AND WAS WRONG.</b> Reading the written reason for world names
    /// measures whether somebody happened to put them in backticks — eleven of fourteen
    /// dials failed it, including several measured on six worlds — so it would have bought
    /// a round of cosmetic edits and no coverage at all. What a test CONSTRUCTS is the
    /// fact; what its author wrote about it is not.
    /// </para>
    /// <para>
    /// <b>AND THE EXEMPTIONS ARE A RATCHET, ON <c>DeadCodeTests</c>' PATTERN.</b> Each entry
    /// is one dial that fails today with what it is waiting for. The list may only shrink;
    /// adding to it wants John and a reason in the commit message.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_arm_is_measured_on_at_least_two_worlds()
    {
        var thin = new List<string>();

        foreach (var dial in Census())
        {
            if (!dial.PropertyType.IsEnum) continue;
            if (Waiting.ContainsKey(dial.Name)
                || Waiting.ContainsKey(dial.PropertyType.Name)) continue;

            var arms = Enum.GetNames(dial.PropertyType);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var path in Directory.GetFiles(
                Path.Combine(Tree.Repo(), "tests", "OpenPlexus.Tests"), "*.cs"))
            {
                if (Path.GetFileName(path) == "DialTests.cs") continue;

                var source = File.ReadAllText(path);

                // A FILE THAT NEVER NAMES THE DIAL SAYS NOTHING ABOUT IT, whatever worlds it
                // builds. Both forms count: selecting an arm by name, and assigning the
                // property in a settings initialiser.
                if (!arms.Any(arm => source.Contains(
                        $"{dial.PropertyType.Name}.{arm}", StringComparison.Ordinal))
                    && !System.Text.RegularExpressions.Regex.IsMatch(
                        source, $@"\b{dial.Name}\s*[=:]"))
                    continue;

                foreach (var world in Worlds)
                    if (System.Text.RegularExpressions.Regex.IsMatch(
                            source, $@"\bnew {world}\s*[({{]|\bFixture\.{world}\b|\b{world}Settings\b|\b{world}Run\b"))
                        seen.Add(world);
            }

            if (seen.Count < 2) thin.Add($"{dial.Name} ({seen.Count})");
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
            .GetFiles(Path.Combine(Tree.Repo(), "src", "OpenPlexus", "Worlds"), "*.cs")
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

        ["Speaking"] =
            "`Multiplexer` ONLY, and this one IS owed. Whether an untested commitment may "
            + "vote is not a question about any world's vocabulary, so nothing stops it "
            + "being taken on `Arranged` or `Roaming` beyond nobody having done it. Its own "
            + "reason already says the finding is that excluding them moves no metric -- a "
            + "null result on one world, which is the weakest thing a grid can say",

    };

    /// <summary>
    /// No dial ships in a do-nothing position — <b>full stop, and a written reason is not a
    /// way past it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>JOHN'S RULE, AND IT REPLACED A WEAKER ONE OF MINE THE SAME DAY.</b> My first
    /// version let a dial ship OFF if its type was named in the plan's refutation table —
    /// on the reasoning that a refuted mechanism legitimately ships off. He pointed out the
    /// hole: <i>writing a reason is easy</i>, and a check whose escape hatch is prose is a
    /// check you can talk your way around.
    /// </para>
    /// <para>
    /// <b>AND THE STRONGER RULE IS ALSO THE SIMPLER ONE, BECAUSE A DIAL IS ONLY EVER ONE OF
    /// TWO THINGS.</b> Either it is a NEW ability, in which case it is on — there is no
    /// other reason to have built it — and it is kept while it is being made to work, or
    /// deleted when it will not. Or it REPLACES something, in which case both arms are live
    /// while they are compared, and afterwards the winner is the code and the loser is
    /// gone. <b>Neither road ends at a dial whose default does nothing.</b>
    /// </para>
    /// <para>
    /// <b>SO A DIAL THAT WOULD SHIP OFF IS A DIAL THAT SHOULD NOT EXIST</b>, and the fix is
    /// to delete the mechanism with a revival row rather than to explain the default. The
    /// code is not lost — it is in the history, and the revival row is what says when to go
    /// and get it.
    /// </para>
    /// <para>
    /// <b>AND DELETION IS NOT THE ONLY MOVE AVAILABLE: ADJUSTING A LOSING ARM IS ALLOWED.</b>
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

            // `Never` BY NAME, WHICH IS THIS REPO'S OWN WORD FOR THE POSITION WHERE NOTHING
            // HAPPENS. A check inferring which arm is inert would be guessing at behaviour
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
    /// <b>THIS LIST MAY ONLY SHRINK, AND AN ENTRY IS CLEARED BY DELETING THE DIAL RATHER
    /// THAN BY IMPROVING ITS REASON.</b> That is the whole difference between this and the
    /// check it replaced. Nothing new may be added — a mechanism arriving today ships on.
    /// </remarks>
    private static readonly Dictionary<string, string> Owed = new(StringComparer.Ordinal)
    {
        ["Widening"] =
            "THREE ARMS AND NOTHING LEFT TO CHOOSE BETWEEN. `Significant` is already deleted "
            + "with a revival row; `Unmissed` is refuted as built -- it selects the rules "
            + "with the LEAST evidence, mints about four wrong rules per right one, and pins "
            + "the population at capacity. So what ships is `Never`, which is a dial whose "
            + "only live position does nothing, which is not a dial. CLEARED BY DELETING IT "
            + "with a revival row, or -- John's third answer -- by adjusting the arm and "
            + "running it again, since what is wrong with it is the SHORTENING rather than "
            + "the parent it picks",
    };

    /// <summary>
    /// A dial shipping in its DO-NOTHING position is named in the plan's refutation table —
    /// <b>the budget for building something better and leaving it switched off.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THERE ARE EXACTLY TWO HONEST REASONS TO SHIP `Never`, AND ONE OF THEM LEAVES A
    /// TRACE.</b> Either the mechanism LOST its comparison — in which case this repo's own
    /// rule says the loser is deleted and leaves a revival row — or it is the first thing of
    /// its kind and must be on. What is forbidden is the third: built, better, and left off
    /// so that the numbers already recorded do not have to be re-taken. <i>An arm only lives
    /// while it is compared</i>, and <i>a better brain beats intact numbers</i>.
    /// </para>
    /// <para>
    /// <b>SO THE CHECK IS THE TRACE RATHER THAN THE INTENT, WHICH IS THE ONLY PART A BUILD
    /// CAN READ.</b> A refuted mechanism is named in DO NOT RE-TRY with what would revive it;
    /// a mechanism switched off to protect a baseline is named nowhere, because there is
    /// nothing to say. The second is what this fails on.
    /// </para>
    /// <para>
    /// <b>IT IS WRITTEN AGAINST A CASE THAT ALREADY PASSES AND A CASE THAT ALREADY
    /// FAILED.</b> `Widening` ships `Never` and is refuted twice over in the table, so it
    /// passes. `Sequencing` shipped `Never` for one commit with no row anywhere, because
    /// there was no refutation — it had WON — and this is the check that would have said so
    /// before John had to.
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

            // THE DECLARING TYPE RATHER THAN A HAND-KEPT LIST, so a settings record nobody
            // told this check about is still asked. `Activator` because a required member is
            // a compile-time check and this never writes one.
            var settings = Activator.CreateInstance(dial.DeclaringType!);
            var shipped = dial.GetValue(settings)?.ToString();

            // `Never` BY NAME, WHICH IS THIS REPO'S OWN WORD FOR THE POSITION WHERE NOTHING
            // HAPPENS. A check inferring which arm is inert would be guessing at behaviour
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
