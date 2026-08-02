using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Thinking;

namespace OpenPlexus.Graph;

/// <summary>
/// One code, and its own row of counts.
/// </summary>
/// <remarks>
/// <para>
/// Holds edges, holds no address, knows nothing about the network. There is no
/// list of other nodes here, no view of the graph, no total occasion count and
/// no clock shared with anyone. <b>A node knows its own row and nothing else</b>
/// — that is C1 holding, in one class.
/// </para>
/// <para>
/// <b>A connection is a count.</b> There is no edge object and no connect
/// operation anywhere in this design. An entry in <c>_together</c> going from
/// absent to 1 <i>is</i> the connection forming.
/// </para>
/// </remarks>
public sealed class Node
{
    /// <summary>This node's identity. Never changes.</summary>
    private readonly Code _code;

    /// <summary>
    /// Partner code to count: how many occasions that code and this one both
    /// fired on. <b>The node's whole row, and the only thing that learns.</b>
    /// </summary>
    private readonly Dictionary<Code, double> _together = [];

    /// <inheritdoc cref="WalkSettings"/>
    private readonly WalkSettings _settings;

    /// <summary>How many occasions this node fired on at all. Its own marginal.</summary>
    private double _seen;

    public Node(Code code, WalkSettings settings) => throw new NotImplementedException();

    /// <inheritdoc cref="_code"/>
    public Code Code => throw new NotImplementedException();

    /// <summary>
    /// How many occasions this node fired on. <b>Public because a neighbour
    /// needs it</b> to weigh an edge pointing here — see <see cref="IMarginals"/>.
    /// </summary>
    public double Seen => throw new NotImplementedException();

    // ---- learning: these two are the entirety of what changes over time ----

    /// <summary>"I fired on this occasion." Adds one to the marginal.</summary>
    public void Note() => throw new NotImplementedException();

    /// <summary>
    /// "That code fired on the same occasion I did." Adds one to that partner's
    /// entry, creating it if new.
    /// </summary>
    /// <remarks>
    /// <b>Writes only this node's row.</b> The partner writes its own. A node
    /// that quietly kept both directions would look identical from outside and
    /// would be holding data it does not own, which is the shared state C1
    /// forbids.
    /// </remarks>
    public void Observe(Code other) => throw new NotImplementedException();

    /// <summary>Reads back one cell of the row.</summary>
    public double Together(Code other) => throw new NotImplementedException();

    /// <summary>
    /// Every code this node has ever co-occurred with. The fan-out of one hop.
    /// </summary>
    public IReadOnlyCollection<Code> Partners() => throw new NotImplementedException();

    // ---- thinking ----------------------------------------------------------

    /// <summary>
    /// A message arrived. Work out what should be sent next.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In order: weigh every partner; price the step once for this node; drop
    /// partners with zero weight and partners <b>already in the arriving
    /// chain</b>; for each survivor work out <c>held - price + fuel</c> and
    /// drop it if that is not positive; build one outgoing message per survivor
    /// with this node appended to the chain and its strength multiplied by the
    /// edge weight; report <c>k-1</c> splits if <c>k</c> survived, or one death
    /// if none did.
    /// </para>
    /// <para>
    /// <b>Returns rather than sends.</b> See <see cref="Fired"/>.
    /// </para>
    /// </remarks>
    public Fired Fire(Message message, IMarginals marginals) =>
        throw new NotImplementedException();

    /// <summary>
    /// How strong the edge to a partner is: the shared count divided by
    /// <i>the partner's</i> marginal — <b>how well the partner predicts me.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// That direction is what refuses a thing present everywhere: it co-occurs
    /// with you constantly and predicts nothing in particular. Measured at
    /// 0.0000 for a distractor against 0.9800 for a real link, where every
    /// symmetrising rule admitted the distractor — 0 of 24.
    /// </para>
    /// <para>
    /// <b>This is the method that cannot work as written across machines</b>,
    /// and <see cref="IMarginals"/> is the seam where that shows.
    /// </para>
    /// </remarks>
    private double WeightOf(Code partner, IMarginals marginals) =>
        throw new NotImplementedException();

    /// <summary>
    /// What a route is paid for taking an edge of this weight.
    /// </summary>
    /// <remarks>
    /// <b>Deliberately separate from the score.</b> They are the same number
    /// under <see cref="Refuel.Strength"/>, which is why they looked like one
    /// thing until surprise needed them apart.
    /// </remarks>
    private double Fuel(double weight) => throw new NotImplementedException();

    /// <summary>
    /// What leaving this node costs, computed once from every partner weight.
    /// </summary>
    private double PriceOfAStep(IReadOnlyCollection<double> weights) =>
        throw new NotImplementedException();
}
