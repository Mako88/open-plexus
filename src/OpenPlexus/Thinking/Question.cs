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
    /// How many walks this question gets, <b>each starting from what the last one
    /// concluded.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>ONE IS EVERY QUESTION ASKED BEFORE THIS EXISTED, and it must stay the
    /// default, because asking again is RIGHT ON ONE WORLD AND HARMFUL ON
    /// ANOTHER.</b> On <see cref="Worlds.Clutrr"/> a single walk cannot reach the
    /// answer at all — it replies with a relation the story stated, which is wrong
    /// by construction, at every budget. On <see cref="Worlds.Clevr"/> a single
    /// walk already reaches it, and asking again throws that away to gamble on one
    /// intermediate: measured, it took the accuracy to roughly a third.
    /// </para>
    /// <para>
    /// <b>SO THE RULE IS THAT IT PAYS EXACTLY WHERE ONE WALK CANNOT REACH.</b> A
    /// second step discards the first walk's evidence and stakes everything on the
    /// intermediate being right, which is a good trade against a certainty of being
    /// wrong and a bad one against a working answer.
    /// </para>
    /// <para>
    /// <b>WHICH IS WHY IT LIVES HERE AND NOT ON A DIAL — the same argument
    /// <see cref="Accumulate.Agreement"/> settled.</b> No setting is right for both
    /// worlds, and sweeping it per world is the recurring fault. Only the asker
    /// knows whether it is asking something a single walk could arrive at.
    /// </para>
    /// <para>
    /// <b>AND MORE IS NOT BETTER.</b> Three steps measured worse than two on the
    /// world where two works, which is the rollout's compounding error.
    /// </para>
    /// </remarks>
    public int Steps { get; init; } = 1;

    /// <summary>
    /// How many of a walk's conclusions seed the next one. <b>Ignored unless
    /// <see cref="Steps"/> asks for more than one.</b>
    /// </summary>
    public int Width { get; init; } = 4;

    /// <summary>
    /// Which front end the next walk's origins must come from, when the asker can
    /// say. <b>Null takes the best of any kind.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE TWO WORLDS THAT HAND-ROLLED THIS BOTH NARROWED, AND ONE CANNOT.</b>
    /// <see cref="Worlds.Composed"/> and <see cref="Worlds.Clevr"/> re-ask from an
    /// INDEX, because they know the intermediate is an object; CLUTRR does not know
    /// what its intermediate is, so it takes whatever the walk found. Both are the
    /// same mechanism and the narrowing is the part only the asker can supply.
    /// </remarks>
    public byte? Between { get; init; }

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

}
