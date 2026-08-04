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
    /// Whether a credit cell is read against the BASE RATE rather than on its own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>IT IS THE QUESTION'S CALL AND NOT A DIAL, for the reason at the top of
    /// this file.</b> Whether the background matters is a fact about what is being
    /// asked — a prediction wants the association, a choice wants the contrast —
    /// and a dial wanting different values in different worlds is this design's
    /// recurring fault wearing a new hat.
    /// </para>
    /// <para>
    /// <b>False is every question asked before the base rate was recorded</b>, so
    /// <see cref="Worthwhile"/> stays exactly as it was measured and remains this
    /// arm's control.
    /// </para>
    /// </remarks>
    public bool Contrasted { get; init; }

    /// <summary>
    /// Whether the walk prefers what was touched RECENTLY — <b>supersession's
    /// second consumer, and the only way this design has of preferring a current
    /// fact to a stale one.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>NOTHING HERE DECAYS, SO A SUPERSEDED FACT IS AS LOUD AS A CURRENT
    /// ONE.</b> A count that stopped rising still stands at whatever it reached, and
    /// in a world that changes that is exactly the wrong behaviour. Eroding it is
    /// not available — that breaks convergence — so the only move left is to let a
    /// question say it cares when, and <see cref="Graph.Tie.When"/> has ridden
    /// beside every count since edge kinds landed waiting to be asked.
    /// </para>
    /// <para>
    /// <b>THE QUESTION'S CALL AND NOT A DIAL, and here that is load-bearing rather
    /// than tidy.</b> <i>What usually follows this</i> wants the association however
    /// old; <i>what should I do now</i> wants what is still true. Those are
    /// different questions about one row, and a dial would have to be wrong for one
    /// of them in every world.
    /// </para>
    /// <para>
    /// <b>False is every question asked before this existed.</b>
    /// </para>
    /// </remarks>
    public bool Recent { get; init; }

    /// <summary>
    /// <see cref="Worthwhile"/> AGAINST WHAT WOULD HAVE HAPPENED ANYWAY — <b>ΔP,
    /// and the one thing a hit rate structurally cannot say.</b>
    /// </summary>
    /// <remarks>
    /// <b>ONE THING CHANGES BETWEEN THIS AND <see cref="Worthwhile"/>.</b> The same
    /// cells are written and the same relation is walked; what moves is that a
    /// partner is believed by how much it RAISES the chance of improvement rather
    /// than by how often it accompanied one. See <see cref="Graph.Node.Contingency"/>.
    /// </remarks>
    public static Question Contingent() =>
        new() { Through = Graph.Kind.Helped, Contrasted = true };
}
