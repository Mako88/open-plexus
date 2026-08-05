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

        ["Budget"] =
            "the controller itself, so it is the switch rather than a level",
    };

    /// <summary>
    /// Dials nothing drives, each with the reason. <b>A reason, not an excuse</b>
    /// — several of these say outright that nobody has found the signal yet.
    /// </summary>
    private static readonly Dictionary<string, string> HandSet = new(StringComparer.Ordinal)
    {
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
            "OPEN, AND IT IS A CAPACITY RATHER THAN A LEVEL. What a node can "
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
            + "a claim about the stream, and the refutation row says it costs its "
            + "row without paying — so what it needs is a reason to exist, not a "
            + "controller",

        ["Ranking"] =
            "a choice between accumulation rules rather than a continuum, exactly "
            + "as `Pricing` and `Toll` are. WHICH rule is not a quantity",

        ["Kinds"] =
            "a structural claim rather than a level — whether a carried edge gets "
            + "its own cell. On or off, with nothing between to hunt",

        ["Surprising"] =
            "OPEN, AND MEASURED INERT ON TEXT. The gate suppresses what was "
            + "predicted, and independent sentences predict nothing of each other. "
            + "It is on or off rather than a level; what it needs is a world whose "
            + "stream repeats, and `Rhythm` is the one that has it",

        ["Gated"] =
            "the write-path half of `Surprising`, and on or off for the same "
            + "reason. It is a claim about LEARNING where the other is about COST",

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
            "Doubt" => plain with { Doubt = 8.0 },
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
        var dials = typeof(WalkSettings)
            .GetProperties()
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
        // AND TO TWELVE, WHICH IS NOT FIVE NEW KNOBS. John moved the dials out
        // of the worlds and into the brain on 2026-08-04, and every one of these
        // already existed as a `*Run` constructor argument -- somewhere this census
        // enumerates `WalkSettings` and therefore could not look. THE BUDGET WAS
        // MEASURING ITS OWN BLIND SPOT: a file whose whole job is to notice a sixth
        // dial arriving beside five could not see five of them, with more still to come as the
        // remaining worlds are migrated.
        //
        // THE NUMBER IS THE HONEST ONE NOW, and it is much worse than seven. That
        // is the argument this file exists to force.
        Assert.Equal(12, HandSet.Count);
    }
}
