using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Graph;

namespace OpenPlexus.Thinking;

/// <summary>
/// What the asker knows about its own question, and the machine does not.
/// </summary>
/// <remarks>
/// <para>
/// <b>RANKING BELONGS TO THE QUESTION, NOT TO THE MACHINE — and this is where it
/// moved to.</b> <see cref="Accumulate.Agreement"/> is right on a conjunction and
/// harmful on an indexed question, so it could never be a default and sweeping it
/// per world is the recurring fault. Fusing the two orders by position was tried
/// instead and is refuted: two candidates whose orders invert tie exactly, for
/// every damping constant, which is the case a fusion was wanted for. So the
/// asker says.
/// </para>
/// <para>
/// <b>It travels beside the grouping because the two are the same kind of
/// thing.</b> Both are facts about what is being asked that only the asker can
/// know — which origins are one attribute said several ways, and whether the
/// origins are equals whose agreement means something. Neither is a property of
/// the graph, and neither can be worked out from the codes.
/// </para>
/// <para>
/// <b>Null is a question that says nothing</b>, which is every question asked
/// before this existed: independent origins, ranked by strength. That is what
/// keeps every measurement taken up to now still standing.
/// </para>
/// </remarks>
public sealed record Question
{
    /// <summary>
    /// How several routes reaching one endpoint combine.
    /// </summary>
    /// <remarks>
    /// <b><see cref="Accumulate.Sum"/> unless the asker says otherwise</b>, and
    /// that is the control every earlier number was taken under.
    /// </remarks>
    public Accumulate Ranking { get; init; } = Accumulate.Sum;

    /// <summary>
    /// Which origins are the same thing said several ways.
    /// </summary>
    /// <remarks>
    /// <b>Null is every question whose origins really are independent.</b> A
    /// question is broadcast from every code of the attribute it names, so without
    /// this a shape reached from three redundant colour codes counts as three
    /// witnesses — which is not what was asked, and destroyed a working result
    /// when it was tried.
    /// </remarks>
    public IReadOnlyDictionary<Code, int>? Asking { get; init; }

    /// <summary>
    /// Which relation the walk may step through. <b>Null walks everything, which
    /// is every question asked before edge kinds existed.</b>
    /// </summary>
    /// <remarks>
    /// <b>ASKING WHAT FOLLOWS IS A DIFFERENT QUESTION FROM ASKING WHAT
    /// ACCOMPANIES</b>, and until the row could tell them apart it was not
    /// possible to say which was meant. A deeper walk for prediction was
    /// monotonically worse precisely because it could not: every extra hop
    /// reached more things that merely co-occurred and ranked them against the
    /// thing that actually came next.
    /// </remarks>
    public Graph.Kind? Through { get; init; }

    /// <summary>
    /// A relation PER HOP, in order — <b>step 9, and the first thing here that can
    /// carry what was learnt in one state to a similar one.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Through"/> NAMES ONE RELATION FOR THE WHOLE WALK, AND THAT IS
    /// THE LIMIT.</b> It answered fork 18 and it is how step 4's credit cell is
    /// walked, but a route under it can never change relation — so the graph can be
    /// asked <i>what followed this</i> or <i>what helped here</i> and never <i>what
    /// helped somewhere LIKE here</i>, which is a different relation at each hop.
    /// </para>
    /// <para>
    /// <b>THE SUCCESSOR REPRESENTATION IS ALREADY IN THE ROW.</b> Dayan's move is
    /// that two states are alike when they lead to similar futures, and
    /// <see cref="Graph.Kind.After"/> is a one-step count of exactly that. Walking
    /// <c>After</c> then <c>Before</c> lands on the states that share a successor
    /// with this one — <b>similarity DERIVED, with no metric on a code and no
    /// front end asked for anything</b>, which is why this comes before the
    /// unbuilt middle of step 8.
    /// </para>
    /// <para>
    /// <b>Querying a graph by a template of relations is Path Ranking</b> (Lao and
    /// Cohen), which is how knowledge-base completion has done this for years. What
    /// is new here is only that the hops are priced and the walk is bounded.
    /// </para>
    /// <para>
    /// <b>Null is every question ever asked</b>, and a path is refused alongside
    /// <see cref="Through"/> rather than combined with it: two restrictions on the
    /// same hop is one field doing two jobs, and this project has that fault
    /// already.
    /// </para>
    /// </remarks>
    public ImmutableArray<Graph.Kind>? Path { get; init; }

    /// <summary>
    /// This question, having been checked for a restriction set two ways.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>ONE RESTRICTION PER HOP OR ONE FOR THE WHOLE WALK, NEVER BOTH.</b> Two
    /// fields narrowing the same choice is this design's recurring fault read from
    /// the other end, and a caller that set both would silently get whichever
    /// <see cref="Graph.Node.Fire"/> happened to check second.
    /// </para>
    /// <para>
    /// <b>HERE RATHER THAN AT THE MACHINE, so it can be asserted without a bus.</b>
    /// A rule enforced only along the path that happens to reach it is a rule with
    /// one caller, and the next caller finds out by measuring something strange.
    /// </para>
    /// </remarks>
    public Question Checked()
    {
        if (Through is not null && Path is not null)
            throw new ArgumentException(
                "a question names one relation for the whole walk or one per hop, "
                + "not both — see Path");

        // A PATH OF NO HOPS IS A WALK THAT CANNOT LEAVE ITS ORIGIN, which would
        // report as the graph having nothing to say rather than as the question
        // asking nothing.
        if (Path is { IsEmpty: true })
            throw new ArgumentException(
                "a path of no hops asks nothing: the walk would die at its origin "
                + "and report silence");

        return this;
    }

    /// <summary>
    /// A question about what helped where something of this was ALSO true —
    /// <b>step 4's credit, spent in a state that never earned any.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>TWO HOPS, AND BOTH CELLS ALREADY EXIST.</b> Out to whatever has shared a
    /// moment with this — which is the whole of what one code has in common with
    /// another before any similarity is defined — and from there to what turned out
    /// to be worth doing. <b><see cref="Worthwhile"/> asks the credit cell of THIS
    /// code and falls silent when it is empty</b>, which is most of the time,
    /// because a cell keyed on the state it was earned in never covers a state
    /// count that keeps growing.
    /// </para>
    /// <para>
    /// <b><see cref="Graph.Kind.With"/> IS THE ONE RELATION EVERY WORLD WRITES</b>,
    /// so this is the form that works anywhere. <see cref="Downstream"/> is the
    /// sharper sibling and needs a world with temporal cells.
    /// </para>
    /// </remarks>
    public static Question Alike() => new()
    {
        Path = [Graph.Kind.With, Graph.Kind.Helped],
    };

    /// <summary>
    /// A question about what is worth FINDING OUT here — <b>step 10, and the
    /// question a body with nothing to go on should ask.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE SAME WIDTH AS <see cref="Worthwhile"/> AND A DIFFERENT STATISTIC</b>,
    /// which is what keeps it clear of step 9's refutation: widening a walk until
    /// it stops being silent re-introduces the behaviour policy, and this does not
    /// widen anything. It ranks by the share of times an act taught the machine
    /// something rather than by the share of times it helped the body. See
    /// <see cref="Graph.Kind.Informed"/>.
    /// </remarks>
    public static Question Curious() => new() { Through = Graph.Kind.Informed };

    /// <summary>
    /// A question about what helps where this is HEADING, rather than where it is
    /// — <b>one step of lookahead, and it is step 11 in miniature.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>NOT A LIKENESS AT ALL, WHICH IS WHY IT IS WORTH TRYING AFTER ONE
    /// FAILED.</b> <see cref="Alike"/> spends credit earned in states that resemble
    /// this one, and a shared moment turned out to resemble nearly everything. This
    /// asks something narrower and better posed: <i>what usually follows here, and
    /// what was worth doing THERE.</i>
    /// </para>
    /// <para>
    /// <b>TWO HOPS, NO NEW CELL, AND NO NEW EDGE KIND.</b>
    /// <see cref="Graph.Kind.After"/> is what usually follows and
    /// <see cref="Graph.Kind.Helped"/> is what was worth doing — both already
    /// written. What it needs is a world that HAS temporal cells, which is a
    /// carried window and nothing more.
    /// </para>
    /// <para>
    /// <b>IT IS A ROLLOUT OF DEPTH ONE, EXPRESSED AS A QUERY.</b> Step 11's
    /// complaint is that this design predicts one frame and stops; this is the
    /// cheapest thing shaped like planning that the graph can already answer, and
    /// it costs a walk rather than a simulator.
    /// </para>
    /// </remarks>
    public static Question Ahead() => new()
    {
        Path = [Graph.Kind.After, Graph.Kind.Helped],
    };

    /// <summary>
    /// The same question asked through shared FUTURES rather than shared moments —
    /// <b>the successor representation, walked.</b>
    /// </summary>
    /// <remarks>
    /// <b>THREE HOPS, AND IT IS A STRICTLY BETTER NOTION OF ALIKE WHERE IT CAN BE
    /// ASKED.</b> Out to what usually follows this; back from there to everything
    /// else that leads to the same place — which is Dayan's claim that two states
    /// are alike when their futures are, and it is a fact about consequences rather
    /// than about co-occurrence; then from those to what was worth doing.
    /// <b>Sharing a moment is cheap and sharing a future is not</b>, so this
    /// generalises where <see cref="Alike"/> merely spreads.
    /// </remarks>
    public static Question Downstream() => new()
    {
        Path = [Graph.Kind.After, Graph.Kind.Before, Graph.Kind.Helped],
    };

    /// <summary>A question about what USUALLY FOLLOWS what is being asked.</summary>
    /// <remarks>
    /// <b>The temporal walk, and it is one line because the row now holds the
    /// distinction.</b>
    /// </remarks>
    public static Question Following() => new() { Through = Graph.Kind.After };

    /// <summary>A question about what USUALLY PRECEDED what is being asked.</summary>
    /// <remarks>
    /// <b>THE QUESTION AN ACTUATOR ASKS.</b> Choosing what to do broadcasts the
    /// situation and has to arrive at an action, which is walking from a
    /// consequence back to its cause — the opposite direction from prediction, and
    /// it is a different question rather than the same one read backwards.
    /// </remarks>
    public static Question Preceding() => new() { Through = Graph.Kind.Before };

    /// <summary>A question about WHAT IS WORTH DOING here, not what was done.</summary>
    /// <remarks>
    /// <b>The contrastive question, and the whole of step 4's second attempt.</b>
    /// Walking the ordinary cell ranks by <i>P(act | state)</i>, which is the
    /// behaviour policy that wrote it — so the walk recommends whatever it did
    /// last time, and in a body that is precisely what put the body here. This
    /// walks the other cell instead. See <see cref="Graph.Kind.Helped"/>.
    /// </remarks>
    public static Question Worthwhile() => new() { Through = Graph.Kind.Helped };

    /// <summary>
    /// The same question, ranked by how many ORIGINS agree rather than by summed
    /// route strength — <b>the answer to a graph with hubs in it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>GRAINS PUT HUBS IN THE GRAPH ON PURPOSE, AND <see cref="Accumulate.Sum"/>
    /// CANNOT COPE WITH THEM.</b> A coarse code is reached from many of the codes
    /// felt at once, and from it many routes run on to the same action — so one
    /// piece of evidence arrives by a dozen paths and is counted a dozen times.
    /// <b>Measured on `Tending`: the arm collapses onto a single action</b>, moving
    /// left on 393 of 400 steps and watering twice, which is step 4's original
    /// one-action failure arriving by a new road.
    /// </para>
    /// <para>
    /// <b><see cref="Accumulate.Agreement"/> counts DISTINCT ORIGINS and is exactly
    /// the fix</b>: many routes from one origin are one piece of evidence arriving
    /// several ways, and it says so. Nothing new travels for it — the chain already
    /// carries its origin for the cycle check.
    /// </para>
    /// </remarks>
    public static Question Agreed() => new()
    {
        Through = Graph.Kind.Helped,
        Ranking = Accumulate.Agreement,
    };

    /// <summary>A question that ranks by agreement between its origins.</summary>
    /// <remarks>
    /// <b>The conjunction: the thing meant is the one every origin reached.</b>
    /// Modality is the grouping key, because it is what distinguishes one
    /// attribute from another.
    /// </remarks>
    public static Question Conjoining(IEnumerable<Code> origins)
    {
        ArgumentNullException.ThrowIfNull(origins);

        return new Question
        {
            Ranking = Accumulate.Agreement,
            Asking = origins.ToDictionary(code => code, code => (int)code.Modality),
        };
    }
}
