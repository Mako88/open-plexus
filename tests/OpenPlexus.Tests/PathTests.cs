using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Thinking;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Step 9 — <b>a walk that changes relation as it goes, and the first thing here
/// that carries credit into a state it was never earned in.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE GAP THIS IS AGAINST.</b> Step 4's credit lives in a cell keyed on the
/// state it was earned in, and the state count keeps growing, so coverage never
/// arrives — the arm is silent for most steps and quadrupling the run moves
/// neither its silence nor its score. <b>Nothing carried what was learnt in one
/// state to a similar one, because nothing could say two states were similar.</b>
/// </para>
/// <para>
/// <b>DAYAN'S ANSWER, AND THE ROW ALREADY HOLDS IT.</b> Two states are alike when
/// they lead to similar futures, and <see cref="Kind.After"/> is a one-step count
/// of exactly that. So <c>After</c> then <c>Before</c> lands on the states sharing
/// a successor with this one, and <c>Helped</c> from there is what was worth doing
/// in them. <b>No metric on a code, and nothing asked of the front end.</b>
/// </para>
/// <para>
/// <b>DRIVEN BY HAND.</b> What is asserted is which partners a node will and will
/// not emit, and that is <see cref="Node.Fire"/>'s answer rather than the bus's.
/// </para>
/// </remarks>
public sealed class PathTests(ITestOutputHelper output)
{
    /// <summary>The state credit was never earned in.</summary>
    private static readonly Code Here = Fixture.C(1);

    /// <summary>What follows both states. <b>The only thing they share.</b></summary>
    private static readonly Code Next = Fixture.C(2);

    /// <summary>
    /// A state that shares a moment AND a future with this one, and DID earn
    /// credit.
    /// </summary>
    private static readonly Code Sibling = Fixture.C(3);

    /// <summary>What was worth doing there.</summary>
    private static readonly Code Worked = Fixture.C(4);

    /// <summary>
    /// The graph the whole file turns on.
    /// </summary>
    /// <remarks>
    /// <b><see cref="Here"/> HAS NO <see cref="Kind.Helped"/> ENTRY AT ALL</b>, and
    /// that is the point rather than an omission: it is a state the body has been
    /// in and never yet learnt anything about. Everything the walk finds about it
    /// has to arrive through <see cref="Sibling"/>.
    /// </remarks>
    private static Dictionary<Code, Node> Graph(WalkSettings dials)
    {
        var nodes = new[] { Here, Next, Sibling, Worked }
            .ToDictionary(code => code, code => new Node(code, dials));

        foreach (var node in nodes.Values) node.Note();

        // They have shared a moment, which is the cheap notion of alike.
        nodes[Here].Observe(Sibling);
        nodes[Sibling].Observe(Here);

        // AND THEY LEAD TO THE SAME NEXT THING, which is the dear one — and it
        // records both as having come before it.
        nodes[Here].Observe(Next, kind: Kind.After);
        nodes[Sibling].Observe(Next, kind: Kind.After);
        nodes[Next].Observe(Here, kind: Kind.Before);
        nodes[Next].Observe(Sibling, kind: Kind.Before);

        // AND ONLY ONE OF THEM HAS EVER BEEN SOMEWHERE GOOD.
        nodes[Sibling].Observe(Worked, kind: Kind.Helped);

        return nodes;
    }

    [Fact]
    public void The_credit_of_a_state_this_one_is_like_is_unreachable_without_a_path()
    {
        // THE CONTROL, AND IT IS THE WHOLE PREMISE. `Worthwhile` walks `Helped`
        // and nothing else, so from a state that never earned credit there is no
        // first hop to take -- which is exactly why step 4's arm falls silent.
        var reached = Reach(Question.Worthwhile());

        output.WriteLine($"worthwhile: {Named(reached)}");

        Assert.Empty(reached);
    }

    [Fact]
    public void A_path_through_a_shared_moment_reaches_it()
    {
        var reached = Reach(Question.Alike());

        output.WriteLine($"alike: {Named(reached)}");

        // THE CLAIM: credit earned somewhere else, spent here, linked only by the
        // two states having once been true at the same time.
        Assert.Contains(Worked, reached);
    }

    [Fact]
    public void And_so_does_a_path_through_a_shared_future()
    {
        var reached = Reach(Question.Downstream());

        output.WriteLine($"downstream: {Named(reached)}");

        // THE SHARPER ONE, and the only link it uses is that both states lead to
        // the same place. Nothing here is a co-occurrence.
        Assert.Contains(Worked, reached);
    }

    [Fact]
    public void A_path_walks_its_relations_in_order_and_no_others()
    {
        // THE COMPANION, AND WITHOUT IT THE TEST ABOVE PASSES FOR A PATH THAT IS
        // IGNORED -- an unrestricted walk reaches the same endpoint through the
        // same graph. What has to be shown is that the ORDER is load-bearing.
        //
        // Reversed, the first hop asks for a `Helped` edge out of a state that has
        // none, and the walk dies where it stands.
        var reversed = Reach(new Question
        {
            Path = [Kind.Helped, Kind.Before, Kind.After],
        });

        output.WriteLine($"reversed: {Named(reversed)}");

        Assert.Empty(reversed);
    }

    [Fact]
    public void A_route_that_has_walked_the_whole_path_stops()
    {
        // A SHORTER PATH MUST NOT OVERSHOOT. Two hops land on the states this one
        // is like, and the walk is finished there -- carrying on would answer a
        // question nobody asked, and would let the BUDGET set the depth where the
        // question already did.
        var reached = Reach(new Question { Path = [Kind.After, Kind.Before] });

        output.WriteLine($"two hops: {Named(reached)}");

        Assert.Contains(Sibling, reached);
        Assert.DoesNotContain(Worked, reached);
    }

    [Fact]
    public void The_budget_is_not_what_stopped_it()
    {
        // AND THE COMPANION TO THAT: a walk that stopped because it ran out of
        // money would look identical from outside. Ten times the stamina reaches
        // exactly the same set, so what bounded it was the question.
        Assert.Equal(
            Reach(Question.Downstream(), stamina: 8.0),
            Reach(Question.Downstream(), stamina: 80.0));
    }

    [Theory]
    [MemberData(nameof(Contradictory))]
    public void A_question_cannot_restrict_one_hop_two_ways(Question question)
    {
        Assert.Throws<ArgumentException>(question.Checked);
    }

    public static TheoryData<Question> Contradictory() =>
    [
        new Question { Through = Kind.Helped, Path = [Kind.After] },
        new Question { Path = ImmutableArray<Kind>.Empty },
    ];

    [Fact]
    public void And_the_questions_that_are_fine_are_still_fine()
    {
        // THE COMPANION, or the check above passes for a rule that refuses
        // everything.
        Assert.NotNull(Question.Alike().Checked());
        Assert.NotNull(Question.Worthwhile().Checked());
        Assert.NotNull(new Question().Checked());
    }

    /// <summary>
    /// Every endpoint a walk from <see cref="Here"/> arrives at, driven by hand.
    /// </summary>
    private static IReadOnlyList<Code> Reach(Question question, double stamina = 8.0)
    {
        var nodes = Graph(Fixture.Dials(stamina));

        var queue = new Queue<Message>();
        var reached = new List<Code>();

        queue.Enqueue(new Message
        {
            Broadcast = BroadcastId.New(),
            ReturnTo = new MachineAddress("in"),
            To = Here,
            Held = stamina,
            Chain = [Here],
            Carried = 1.0,
            Through = question.Through,
            Path = question.Path,
        });

        while (queue.Count > 0)
        {
            var message = queue.Dequeue();
            var fired = nodes[message.To].Fire(message);

            if (fired.Reached is { } arrival) reached.Add(arrival.Endpoint);

            foreach (var outgoing in fired.Outgoing) queue.Enqueue(outgoing);
        }

        return reached;
    }

    private static string Named(IReadOnlyList<Code> codes) =>
        codes.Count == 0 ? "(nothing)" : string.Join(", ", codes.Select(Name));

    private static string Name(Code code) =>
        code == Here ? "here"
        : code == Next ? "next"
        : code == Sibling ? "sibling"
        : code == Worked ? "worked"
        : code.ToString();
}
