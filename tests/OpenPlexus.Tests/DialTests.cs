using OpenPlexus.Commitments;
using OpenPlexus.Bus;
using OpenPlexus.Graph;
using OpenPlexus.Thinking;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

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
public sealed class DialTests(ITestOutputHelper output)
{
    /// <summary>Dials something already sets from what the run is doing.</summary>
    private static readonly Dictionary<string, string> Driven = new(StringComparer.Ordinal)
    {
        ["Stamina"] =
            "fork 24 — `Budget` hunts it from whether the walk reached what it "
            + "was narrowing to. Off by default, which is its own open question",

    };

    /// <summary>Every dial in the tree, wherever its settings record lives.</summary>
    private static IEnumerable<System.Reflection.PropertyInfo> Census() =>
        typeof(WalkSettings).GetProperties()
            .Concat(typeof(CommittingSettings).GetProperties());

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
            + "-- `Children` was refused for turning out to be free in disguise",

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

        ["Widening"] =
            "the ladder's other direction, and a mechanism against its own absence "
            + "rather than two rules that both do something -- which the shape "
            + "forbids, since there is no second way to not shorten a scope. "
            + "Measured ON from the baseline every earlier number was taken under, "
            + "which is what this repo's trap list asks for. NOT A LEVEL: whether "
            + "anything generalises is not a quantity. AND IT IS REFUTED AS BUILT, "
            + "which is why it ships off. `Unmissed` selects the commitments with "
            + "the LEAST evidence -- never having missed is nearly free for a "
            + "narrow rule, because a narrow rule barely fires -- and dropping a "
            + "code from a sound scope usually makes it unsound, so it mints about "
            + "four wrong rules per right one and each has wider reach than its "
            + "parent. It buys hard-round coverage at eleven bits and costs "
            + "accuracy on every world. The driver would be a gate that reads how "
            + "much a rule has been TESTED rather than whether it has been wrong",

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

        ["Fanout"] =
            "a choice between sending to every partner and sending above the row's "
            + "own shoulder. The quantity that could be hunted is the WIDTH, and "
            + "the row already sets that from its widest gap — what is left is "
            + "which of two rules applies, which is not a level",

        ["Pricing"] =
            "a choice between two C1-legal weightings rather than a continuum. "
            + "Which end weighs an edge is not a quantity that can be hunted",

        ["Horizon"] =
            "a backstop, and it has not fired since the cost became inverse. A "
            + "bound that never binds has nothing to tune against",

        ["Reflect"] =
            "OPEN, AND FORK 23 IS WHY. Two candidate signals for the threshold "
            + "were tried: `Hunger` inverted, `Thwarted` had the right shape and "
            + "swung too little. Needs the internal error signal of step 2",

        ["Doubt"] =
            "OPEN, AND THE MOST TRACTABLE OF THE SIX. Shrinkage strength is "
            + "estimated from data everywhere else it is used, and a node has the "
            + "data: every message arriving carries the sender's marginal, so a "
            + "node could shrink relative to how much evidence it typically sees. "
            + "Its own inbox, so C1-legal, and world-independent by construction",

        ["Toll"] =
            "a choice between two statistics to charge from rather than a "
            + "continuum, exactly as `Pricing` is. WHAT it prices in is not a "
            + "quantity that can be hunted; how deep to go already has a "
            + "controller, and that is `Stamina`",

        ["Row"] =
            "OPEN, AND IT IS A CAPACITY RATHER THAN A LEVEL. Cashed in at 32; "
            + "what a node can "
            + "AFFORD to hold is a fact about the machine, not about the run, so "
            + "there is nothing in the walk for it to be hunted from — the honest "
            + "driver is available memory. What a run CAN say is whether the cap "
            + "is biting, and `Widest` already reports that",

        ["Foresight"] =
            "OPEN, AND THE MOST TRACTABLE ONE LEFT. The prediction budget is "
            + "hand-set, yet its feedback is already computed every single step — "
            + "the graph's guess is scored against what actually arrived. That is "
            + "the signal fork 24 needed and it is sitting there unused",

        // ---- ARRIVED FROM THE WORLDS, 2026-08-04 ---------------------------
        //
        // NONE OF THESE IS NEW. Every one was already a dial, passed to a `*Run`
        // constructor where this census could not see it -- so the budget below
        // jumping from seven is the CHECK BEING FIXED rather than the system
        // growing knobs. Several had DIFFERENT DEFAULTS in different worlds,
        // which is the sharpest form of the fault: `Ranking` was `Sum` on bAbI
        // and `Agreement` on CLEVR, so a world decided how the brain thought.

        ["Span"] =
            "a capacity rather than a level, like `Row`. How far back to carry is "
            + "a claim about the STREAM, and the refutation row says it costs its "
            + "row without paying — so what it needs is a reason to exist, not a "
            + "controller. It is ON everywhere since 2026-08-04 and known to hurt "
            + "bAbI, which is the pressure that makes the reason worth finding",

        ["Ranking"] =
            "a choice between accumulation rules rather than a continuum, exactly "
            + "as `Pricing` and `Toll` are. WHICH rule is not a quantity",





        ["Carried"] =
            "OPEN. What a carried occasion is worth against a simultaneous one is "
            + "a genuine continuum and nothing hunts it — but the refuted "
            + "carried-edge discount row says a weight was not what the window "
            + "needed, so this waits on that being answered rather than tuned",


        ["Depth"] =
            "OPEN, AND THE PLAN CALLS IT OUT. Every rollout step is a whole walk, "
            + "so depth currently borrows `Stamina`'s budget. It wants its own "
            + "control, and that is an item rather than an excuse",


        ["Names"] =
            "CASHED IN AT ONE CODE. Coarse ranking informs and fine does not, so "
            + "the number is a finding rather than a level anybody should hunt",
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
    private static readonly Dictionary<string, bool> Ranking = new(StringComparer.Ordinal)
    {
        ["Doubt"] = true,
    };

    [Theory]
    [InlineData("Doubt")]
    public async Task A_ranking_dial_does_not_touch_the_price(string dial)
    {
        Assert.True(Ranking[dial], $"{dial} is not claimed to be ranking-only");

        var plain = Fixture.Dials(stamina: 8.0);

        var moved = dial switch
        {
            // MOVED OFF THE DEFAULT RATHER THAN ON FROM ZERO. Doubt was cashed in
            // at 8.0 on 2026-08-04, so `with { Doubt = 8.0 }` is now the SAME
            // record and this assertion compared a run against itself.
            "Doubt" => plain with { Doubt = 32.0 },
            _ => throw new ArgumentOutOfRangeException(nameof(dial)),
        };

        Assert.NotEqual(plain, moved);

        Assert.Equal(await MessagesAsync(plain), await MessagesAsync(moved));
    }

    /// <summary>
    /// The companion, and without it the check above passes for a harness that
    /// cannot see a price change at all.
    /// </summary>
    [Fact]
    public async Task And_a_price_dial_does()
    {
        var shallow = await MessagesAsync(Fixture.Dials(stamina: 4.0));
        var deep = await MessagesAsync(Fixture.Dials(stamina: 8.0));

        Assert.NotEqual(shallow, deep);
    }

    /// <summary>
    /// <b><see cref="Toll"/> IS THE FIRST DIAL CLAIMED TO MOVE THE PRICE AND
    /// NOTHING ELSE</b>, and this is the same fingerprint read the other way
    /// round.
    /// </summary>
    /// <remarks>
    /// <b>A price dial must move the traffic, or it is connected to nothing</b> —
    /// which is the failure `ThinkAsync`'s stamina survived three measurements as.
    /// The ranking half is asserted where it can be seen directly: the weight a
    /// partner is believed at is untouched by this dial, so a walk under either
    /// toll ranks the same partners in the same order and only gets a different
    /// distance for its money.
    /// </remarks>
    [Fact]
    public async Task The_traffic_toll_moves_the_price()
    {
        var evidence = Fixture.Dials(stamina: 8.0);
        var traffic = evidence with { Toll = Toll.Traffic };

        Assert.NotEqual(evidence, traffic);

        Assert.NotEqual(await MessagesAsync(evidence), await MessagesAsync(traffic));
    }

    /// <summary>
    /// And the walk is still bounded under it — <b>the one failure that takes the
    /// process with it.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE REFUTED <c>StepCost</c> ARMS DIED OF EXACTLY THIS</b>, at five
    /// million messages on a twelve-clique where inverse cost took 1,111. A clique
    /// of equal weights is the shape that kills a cost which can reach zero: every
    /// edge is perfect, so under anything proportional to the weight a route pays
    /// nothing and the fan-out is factorial. Here the cheapest hop still costs the
    /// one that is added to the log.
    /// </remarks>
    [Fact]
    public void And_the_walk_is_still_bounded_where_the_evidence_toll_degenerates()
    {
        var evidence = Sweep(Toll.Evidence);
        var traffic = Sweep(Toll.Traffic);

        output.WriteLine($"12-clique, stamina 8: evidence={evidence} traffic={traffic}");

        // WHAT THE CLIQUE ACTUALLY MEASURES, AND IT IS NOT WHAT IT WAS BUILT FOR.
        // Every node here noted once and met eleven partners once, so every weight
        // is EXACTLY 1.0 -- and `1 / weight` at weight one is the constant cost
        // that `StepCost.Constant` was refuted as. So the control is not merely
        // dearer here, it is the refuted arm in disguise: a budget of 8 buys 8
        // free hops out of a fan-out of 11 and the growth is factorial.
        //
        // THAT IS THE REFUTATION ROW'S REVIVAL CONDITION READ OUT LOUD -- "a bound
        // not relying on positive cost at weight 1.0". Inverse cost relies on it,
        // and this is the shape where it is not there to rely on.
        Assert.True(evidence > 1_000_000,
            $"the control was expected to run away on an equal-weight clique and "
            + $"passed only {evidence} messages — if this has been fixed, the "
            + "argument for a traffic toll is weaker than it was written up as");

        // AND THE TRAFFIC TOLL HOLDS EXACTLY WHERE THE OTHER LETS GO, because a
        // row of eleven costs `1 + log2(11)` whatever the counts in it say.
        Assert.True(traffic < 1_000,
            $"the traffic toll was not bounded: {traffic} messages");
    }

    /// <summary>
    /// One walk over a 12-clique of equal weights, driven by hand.
    /// </summary>
    /// <remarks>
    /// <b>BY HAND RATHER THAN OVER A BUS</b>, because what is counted is what the
    /// NODES produce. A bus would put delivery, ordering and settling into a
    /// number that is meant to be arithmetic.
    /// </remarks>
    private static long Sweep(Toll toll)
    {
        var dials = Fixture.Dials(stamina: 8.0) with { Toll = toll };

        var clique = Enumerable.Range(1, 12).Select(one => Fixture.C((ulong)one)).ToList();
        var nodes = clique.ToDictionary(code => code, code => new Node(code, dials));

        foreach (var code in clique)
        {
            nodes[code].Note();
            foreach (var other in clique.Where(other => other != code)) nodes[code].Observe(other);
        }

        var queue = new Queue<Message>();

        queue.Enqueue(new Message
        {
            Broadcast = BroadcastId.New(),
            ReturnTo = new MachineAddress("in"),
            To = clique[0],
            Held = dials.Stamina,
            Chain = [clique[0]],
            Carried = 1.0,
        });

        var sent = 0L;

        // A CEILING SO A RUNAWAY IS A FAILING NUMBER RATHER THAN A HUNG SUITE.
        // The chain's cycle check bounds this shape at 11!/3! either way, so
        // nothing here can actually reach it — it is the backstop for a change
        // that has lost the cycle check as well.
        while (queue.Count > 0 && sent < 5_000_000)
        {
            var message = queue.Dequeue();

            foreach (var outgoing in nodes[message.To].Fire(message).Outgoing)
            {
                sent++;
                queue.Enqueue(outgoing);
            }
        }

        return sent;
    }

    /// <summary>One fixed run, so the only thing that differs is the dial.</summary>
    private static async Task<long> MessagesAsync(WalkSettings dials)
    {
        using var run = new SensesRun(Fixture.Senses(concepts: 12), dials, seed: 3);
        return (await run.RunAsync(300, every: 10).ConfigureAwait(false)).Messages;
    }

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
        Assert.Equal(26, HandSet.Count);
    }
}
