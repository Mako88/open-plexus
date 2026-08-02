using System.Collections.Immutable;
using System.Diagnostics;
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

    /// <summary>
    /// Guards this node's own row and marginal, and nothing else.
    /// </summary>
    /// <remarks>
    /// <b>Never held across a call to <see cref="IMarginals"/>.</b> Weighing an
    /// edge reads the partner's node, so a node holding its own lock while
    /// doing that would deadlock against a partner firing back at it — which
    /// is an ordinary case, since edges are mutual. <see cref="Fire"/> takes a
    /// snapshot and releases before it weighs anything.
    /// </remarks>
    private readonly Lock _gate = new();

    /// <summary>How many occasions this node fired on at all. Its own marginal.</summary>
    private double _seen;

    /// <param name="code">This node's identity.</param>
    /// <param name="settings">
    /// The dials. Validated here so a node cannot exist holding a contradictory
    /// pair — an argument that silently does nothing is a sweep arm that looks
    /// distinct and is not.
    /// </param>
    public Node(Code code, WalkSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.Stamina <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(settings),
                "a route with no stamina cannot take its first step");

        if (settings.Cost == StepCost.Constant && settings.Charge <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(settings),
                "StepCost.Constant needs a Charge above zero");

        if (settings.Cost != StepCost.Constant && settings.Charge != 0.0)
            throw new ArgumentException(
                "Charge is the price for StepCost.Constant and does nothing " +
                "otherwise; setting both would give a sweep an arm that is not one",
                nameof(settings));

        _code = code;
        _settings = settings;
    }

    /// <inheritdoc cref="_code"/>
    public Code Code => _code;

    /// <summary>
    /// How many occasions this node fired on. <b>Public because a neighbour
    /// needs it</b> to weigh an edge pointing here — see <see cref="IMarginals"/>.
    /// </summary>
    public double Seen
    {
        get { lock (_gate) return _seen; }
    }

    // ---- learning: these two are the entirety of what changes over time ----

    /// <summary>"I fired on this occasion." Adds one to the marginal.</summary>
    public void Note()
    {
        lock (_gate) _seen += 1.0;
    }

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
    public void Observe(Code other)
    {
        if (other == _code)
            throw new ArgumentException(
                "a code cannot be its own partner; counting one would make " +
                "every statistic read its own presence as evidence", nameof(other));

        lock (_gate) _together[other] = _together.GetValueOrDefault(other) + 1.0;
    }

    /// <summary>Reads back one cell of the row.</summary>
    public double Together(Code other)
    {
        lock (_gate) return _together.GetValueOrDefault(other);
    }

    /// <summary>
    /// Every code this node has ever co-occurred with. The fan-out of one hop.
    /// </summary>
    public IReadOnlyCollection<Code> Partners()
    {
        lock (_gate) return [.. _together.Keys];
    }

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
    /// with the partner appended to the chain and the strength multiplied by
    /// the edge weight; report <c>k-1</c> splits if <c>k</c> survived, or one
    /// death if none did.
    /// </para>
    /// <para>
    /// <b>Returns rather than sends.</b> See <see cref="Fired"/>.
    /// </para>
    /// <para>
    /// <b>A chain ends with the node the message is addressed to</b>, so the
    /// receiver is already in it when this runs. That is what makes the cycle
    /// check free — the partner is refused if it appears anywhere in the chain
    /// already being carried.
    /// </para>
    /// </remarks>
    public Fired Fire(Message message, IMarginals marginals)
    {
        ArgumentNullException.ThrowIfNull(marginals);

        if (message.To != _code)
            throw new ArgumentException(
                $"message addressed to {message.To} reached the node for {_code}",
                nameof(message));

        // SNAPSHOT FIRST, THEN WEIGH. Weighing reads partners' nodes, and
        // holding this node's lock while doing that deadlocks against a partner
        // firing back — which is ordinary, because edges are mutual.
        KeyValuePair<Code, double>[] row;
        double seen;
        lock (_gate)
        {
            row = [.. _together];
            seen = _seen;
        }

        // An origin message has not travelled, so nothing arrived here and
        // there is no edge to value. Its strength is the starting 1.0.
        var isOrigin = message.Chain.Length <= 1;

        // LIFT DIVIDES BY THIS NODE'S OWN MARGINAL, which is the receiver's to
        // read. PPMI's global occasion total is identical for every candidate
        // so it cancels in a ranking and never has to be known — which is what
        // makes rarity-weighting C1-legal where PPMI is not.
        var carried = !isOrigin && _settings.Value == ArrivalValue.Lift
            ? message.Carried / Math.Max(seen, 1.0)
            : message.Carried;

        var reached = isOrigin
            ? null
            : new Arrival
            {
                Endpoint = _code,
                Score = carried,
                Chain = message.Chain,
                Best = carried,
                Routes = 1,
            };

        // THE HORIZON. Reached only because the budget provably does not bound
        // this walk when weights are equal -- see WalkSettings.Horizon.
        if (message.Chain.Length >= _settings.Horizon)
        {
            return new Fired
            {
                Outgoing = [],
                Reached = reached,
                Accounting = new Accounting(message.Broadcast, 0, Deaths: 1, Halted: 1),
            };
        }

        var weights = new Dictionary<Code, double>(row.Length);
        var fuels = new Dictionary<Code, double>(row.Length);
        var affordable = new List<double>(row.Length);

        foreach (var (partner, together) in row)
        {
            var weight = WeightOf(together, partner, marginals);
            weights[partner] = weight;

            // THE FUEL AND THE SCORE ARE TWO QUANTITIES. The weight scores an
            // arrival; the fuel decides whether the route can keep going. Under
            // Refuel.Strength they are the same number, which is why they
            // looked like one thing until surprise needed them apart.
            var fuel = Fuel(weight);
            fuels[partner] = fuel;
            if (fuel > 0.0) affordable.Add(fuel);
        }

        var price = PriceOfAStep(affordable);
        var outgoing = ImmutableArray.CreateBuilder<Message>(weights.Count);

        foreach (var (partner, weight) in weights)
        {
            if (weight <= 0.0 || message.Chain.Contains(partner)) continue;

            var left = message.Held - price + fuels[partner];
            if (left <= 0.0) continue;

            outgoing.Add(message with
            {
                To = partner,
                Held = left,
                Chain = message.Chain.Add(partner),
                Carried = carried * weight,
            });
        }

        var children = outgoing.Count;

        return new Fired
        {
            Outgoing = outgoing.ToImmutable(),
            Reached = reached,

            // One route became `children` routes, so the live count moves by
            // the difference — a split is not the birth of `children` new ones.
            Accounting = new Accounting(
                message.Broadcast,
                Splits: children > 0 ? children - 1 : 0,
                Deaths: children == 0 ? 1 : 0),
        };
    }

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
    private static double WeightOf(double together, Code partner, IMarginals marginals)
    {
        var common = marginals.SeenOf(partner);
        return common <= 0.0 ? 0.0 : together / common;
    }

    /// <summary>
    /// What a route is paid for taking an edge of this weight.
    /// </summary>
    /// <remarks>
    /// <b>Deliberately separate from the score.</b> They are the same number
    /// under <see cref="Refuel.Strength"/>, which is why they looked like one
    /// thing until surprise needed them apart.
    /// </remarks>
    private double Fuel(double weight) => _settings.Refuel switch
    {
        Refuel.Strength => weight,

        // A route survives by walking edges that were unlikely. Local, because
        // it needs one node's own row and one marginal, where PPMI needs the
        // global total C1 forbids.
        Refuel.Surprise => weight > 0.0 && weight < 1.0 ? -Math.Log2(weight) : 0.0,

        _ => throw new UnreachableException(),
    };

    /// <summary>
    /// What leaving this node costs, computed once from every partner's fuel.
    /// </summary>
    private double PriceOfAStep(IReadOnlyCollection<double> affordable) => _settings.Cost switch
    {
        StepCost.Constant => _settings.Charge,

        // REFUTED and kept so the refutation can be re-run: about half a node's
        // edges are above its own mean, so a route taking above-mean steps
        // gains budget forever.
        StepCost.Local => affordable.Count == 0 ? 0.0 : affordable.Sum() / affordable.Count,

        // Opportunity cost: the step is charged what the best step here would
        // have cost. Budget is therefore non-increasing, and strictly
        // decreasing for anything but the strongest edge, so the walk is
        // bounded without a constant.
        StepCost.Best => affordable.Count == 0 ? 0.0 : affordable.Max(),

        _ => throw new UnreachableException(),
    };
}
