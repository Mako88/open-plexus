using OpenPlexus.Graph;
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
        ["Accumulate"] =
            "NOT A LEVEL TO FIND — a property of the QUESTION. A conjunction "
            + "wants agreement between origins and an indexed question does not, "
            + "and the asker knows which it is asking. It belongs on the question "
            + "rather than on the machine, and that is the open work",

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

        ["Foresight"] =
            "OPEN, AND THE MOST TRACTABLE ONE LEFT. The prediction budget is "
            + "hand-set, yet its feedback is already computed every single step — "
            + "the graph's guess is scored against what actually arrived. That is "
            + "the signal fork 24 needed and it is sitting there unused",
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
        ["Accumulate"] = true,
        ["Doubt"] = true,
    };

    [Theory]
    [InlineData("Accumulate")]
    [InlineData("Doubt")]
    public async Task A_ranking_dial_does_not_touch_the_price(string dial)
    {
        Assert.True(Ranking[dial], $"{dial} is not claimed to be ranking-only");

        var plain = Fixture.Dials(stamina: 8.0);

        var moved = dial switch
        {
            "Accumulate" => plain with { Accumulate = Accumulate.Agreement },
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
        Assert.Equal(6, HandSet.Count);
    }
}
