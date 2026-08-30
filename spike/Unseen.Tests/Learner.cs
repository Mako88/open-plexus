namespace Unseen;

/// <summary>Where the condition a repair adds is drawn from.</summary>
/// <remarks>
/// Three arms that all do something, rather than a mechanism and its own absence. They differ
/// only in the hypothesis space the condition comes from; the trigger, the separation
/// criterion, the budget and everything downstream are identical.
/// </remarks>
public enum Proposing
{
    /// <summary>A code the moments already contained. What `commitments` does today.</summary>
    Present,

    /// <summary>A region of the encoder's space, minted as a new code.</summary>
    ByEncoder,

    /// <summary>A region of a direction drawn from nothing, minted the same way.</summary>
    ByChance,

    /// <summary>
    /// A region of the encoder's space, over the difference between the two arguments.
    /// </summary>
    /// <remarks>
    /// The arm that needs roles. A difference is only defined once something says which
    /// argument is subtracted from which, and a moment that is a set of codes cannot say it.
    /// </remarks>
    ByPair,
}

/// <summary>A code the machine made, standing for a region rather than a list.</summary>
/// <remarks>
/// This is the whole difference. A name over a list of members cannot fire for a member it has
/// never met. A region covers things nobody has seen yet by construction, which is what lets a
/// repaired rule transfer and is also why it cannot memorise.
/// </remarks>
public sealed record Region(
    int Code,
    float[] Direction,
    float Threshold,
    bool Above,
    bool OnDifference = false)
{
    public bool Fires(Thing subject, Thing? other)
    {
        ArgumentNullException.ThrowIfNull(subject);

        if (OnDifference && other is null) return false;

        var projection = 0f;
        for (var d = 0; d < Direction.Length; d++)
        {
            var value = OnDifference ? subject.Vector[d] - other!.Vector[d] : subject.Vector[d];
            projection += Direction[d] * value;
        }

        return Above ? projection > Threshold : projection < Threshold;
    }
}

/// <summary>A rule that can be wrong about something in particular.</summary>
public sealed class Commitment(int[] scope, int consequent)
{
    public int[] Scope { get; } = scope;

    public int Consequent { get; } = consequent;

    public long Hits { get; private set; }

    public long Misses { get; private set; }

    public long Fired => Hits + Misses;

    public double Rate => Fired == 0 ? 0 : (double)Hits / Fired;

    /// <summary>The steps it was right and wrong about, bounded.</summary>
    /// <remarks>
    /// The whole step rather than its subject, because an arm that reads the pair needs the
    /// second argument and an arm that reads the codes needs the moment.
    /// </remarks>
    public List<Step> Right { get; } = [];

    public List<Step> Wrong { get; } = [];

    /// <summary>Codes already spent on a child, so repair enumerates rather than repeating.</summary>
    public HashSet<int> Spent { get; } = [];

    public bool Applies(HashSet<int> moment) => Scope.All(moment.Contains);

    public void Saw(bool hit, Step step)
    {
        const int Keep = 400;

        if (hit)
        {
            Hits++;
            if (Right.Count < Keep) Right.Add(step);
        }
        else
        {
            Misses++;
            if (Wrong.Count < Keep) Wrong.Add(step);
        }
    }

    public string Name() => $"{{{string.Join(",", Scope)}}} -> {Consequent}";
}

/// <summary>
/// The smallest learner that can run the comparison.
/// </summary>
/// <remarks>
/// Genesis proposes the minimal rule, evidence accumulates, and repair specialises whatever is
/// sometimes wrong. Nothing here generalises, abstracts, subsumes, plans or asks; the spike is
/// about one step of one mechanism and everything else would only make the reading harder to
/// attribute.
/// </remarks>
/// <param name="proposing">Where the condition a repair adds is drawn from.</param>
/// <param name="seed">The generator behind the chance arm.</param>
/// <param name="maxChildren">
/// How many children any arm may make in a whole run, and a dial rather than a constant
/// because the control's score moves with it. The first reading of this spike did not have it
/// and was not a comparison: a child minted from a chance direction usually has a mixed
/// record, so it is repairable and has children of its own, while a child minted from the
/// encoder is right about everything and stops. The control ran 12,933 candidates against the
/// arm's 50 and scored 0.750 by searching, which is the multiple-comparisons problem wearing
/// an experiment's clothes.
/// </param>
/// <param name="maxScope">
/// How many conditions a rule may carry. Two is genesis plus one repair, so a learner capped
/// there can use one region and no more; leaving it open lets repair condition on its own
/// earlier condition. The difference between the two is the only direct measurement of whether
/// composing regions buys anything, since a task that needs it cannot be built by hand -- four
/// well-separated clusters in three hundred and eighty-four dimensions can be split any way at
/// all by one hyperplane.
/// </param>
public sealed class Learner(
    Proposing proposing,
    int seed,
    int maxChildren = 200,
    int maxScope = 99)
{
    private const int Floor = 20;
    private const int ChildrenPerParent = 40;

    private readonly List<Commitment> _population = [];
    private readonly List<Region> _regions = [];
    private readonly HashSet<string> _known = [];
    private readonly Random _chance = new(seed);
    private int _nextRegion = 1000;
    private int _children;

    public int Rules => _population.Count;

    public int Regions => _regions.Count;

    /// <summary>Run the stream, learning as it goes.</summary>
    public void Live(IEnumerable<Step> steps, int repairEvery)
    {
        ArgumentNullException.ThrowIfNull(steps);

        var at = 0;

        foreach (var step in steps)
        {
            at++;

            var moment = Moment(step);

            foreach (var one in _population.Where(one => one.Applies(moment)).ToList())
                one.Saw(one.Consequent == step.Next, step);

            Seed(step.Next);

            if (at % repairEvery == 0) Repair();
        }

        Repair();
    }

    /// <summary>What the machine says about one step it has never met.</summary>
    /// <returns>The predicted code, or nothing where no rule it trusts applies.</returns>
    public int? Says(Step step) => Chose(step)?.Consequent;

    /// <summary>
    /// Which rule answered, so the report can say how deep the answer was.
    /// </summary>
    /// <remarks>
    /// A scope of one is the rule genesis made. Two is one repair, and it is the whole result
    /// of the ladder so far. Three or more would mean repair conditioned on its own earlier
    /// condition, which is the only route the mechanism has to a property no single direction
    /// separates.
    /// </remarks>
    public Commitment? Chose(Step step)
    {
        ArgumentNullException.ThrowIfNull(step);

        var moment = Moment(step);

        return _population
            .Where(one => one.Fired >= Floor && one.Applies(moment))
            .OrderByDescending(one => one.Rate)
            .ThenByDescending(one => one.Scope.Length)
            .ThenByDescending(one => one.Fired)
            .ThenBy(one => one.Consequent)
            .FirstOrDefault();
    }

    /// <summary>The codes true of this step, including every region that fires.</summary>
    private HashSet<int> Moment(Step step)
    {
        var moment = new HashSet<int>(step.Now);

        foreach (var region in _regions.Where(one => one.Fires(step.Subject, step.Other)))
            moment.Add(region.Code);

        return moment;
    }

    /// <summary>The minimal rule for one outcome, made once.</summary>
    private void Seed(int consequent) => Add(new Commitment([World.Put], consequent));

    private bool Add(Commitment one)
    {
        if (!_known.Add(one.Name())) return false;
        _population.Add(one);
        return true;
    }

    /// <summary>
    /// Every rule that is sometimes wrong gets one child, if the arm can find one.
    /// </summary>
    /// <remarks>
    /// The condition must be present where the parent was right and absent where it was wrong,
    /// so the child fires only where the parent already worked. That is what makes the child a
    /// narrowing rather than a new claim.
    /// </remarks>
    private void Repair()
    {
        foreach (var parent in _population.ToList())
        {
            if (_children >= maxChildren) return;
            if (parent.Fired < Floor) continue;
            if (parent.Hits == 0 || parent.Misses == 0) continue;
            if (parent.Spent.Count >= ChildrenPerParent) continue;
            if (parent.Scope.Length >= maxScope) continue;

            var condition = proposing switch
            {
                Proposing.Present => FromPresent(parent),
                Proposing.ByEncoder => FromDirection(parent, Separating(parent, false), false),
                Proposing.ByPair => FromDirection(parent, Separating(parent, true), true),
                Proposing.ByChance => FromDirection(parent, Random(), false),
                _ => null,
            };

            if (condition is not int code) continue;

            parent.Spent.Add(code);

            if (Add(new Commitment([.. parent.Scope.Append(code).Order()], parent.Consequent)))
                _children++;
        }
    }

    /// <summary>The code that best marks out where the parent was right.</summary>
    private int? FromPresent(Commitment parent)
    {
        var right = parent.Right.SelectMany(one => one.Now).CountBy(one => one).ToDictionary();
        var wrong = parent.Wrong.SelectMany(one => one.Now).CountBy(one => one).ToDictionary();

        var best = right.Keys
            .Where(code => !parent.Scope.Contains(code) && !parent.Spent.Contains(code))
            .Select(code => (
                Code: code,
                Score: ((double)right[code] / parent.Right.Count)
                    - ((double)wrong.GetValueOrDefault(code) / Math.Max(1, parent.Wrong.Count))))
            .Where(one => one.Score > 0)
            .OrderByDescending(one => one.Score)
            .ThenBy(one => one.Code)
            .FirstOrDefault();

        return best.Code == 0 ? null : best.Code;
    }

    /// <summary>
    /// The direction from what it got wrong towards what it got right.
    /// </summary>
    /// <param name="parent">The rule whose record is being read.</param>
    /// <param name="paired">
    /// Whether to read the difference between the two arguments rather than the subject alone.
    /// This is the whole of what roles buy: the same arithmetic over a value that only exists
    /// once something says which argument comes first.
    /// </param>
    private static float[]? Separating(Commitment parent, bool paired)
    {
        if (parent.Right.Count == 0 || parent.Wrong.Count == 0) return null;
        if (paired && parent.Right.Concat(parent.Wrong).Any(one => one.Other is null)) return null;

        var width = parent.Right[0].Subject.Vector.Length;
        var direction = new float[width];

        foreach (var one in parent.Right)
            for (var d = 0; d < width; d++) direction[d] += Read(one, d, paired) / parent.Right.Count;

        foreach (var one in parent.Wrong)
            for (var d = 0; d < width; d++) direction[d] -= Read(one, d, paired) / parent.Wrong.Count;

        return Encoder.Unit(direction);
    }

    /// <summary>One coordinate of what a step looks like to an arm.</summary>
    private static float Read(Step step, int at, bool paired) =>
        paired ? step.Subject.Vector[at] - step.Other!.Vector[at] : step.Subject.Vector[at];

    /// <summary>A direction drawn from nothing, which is the floor the encoder must beat.</summary>
    private float[]? Random()
    {
        var width = _population.SelectMany(one => one.Right.Concat(one.Wrong))
            .Select(one => one.Subject.Vector.Length)
            .FirstOrDefault();

        if (width == 0) return null;

        var direction = new float[width];
        for (var d = 0; d < width; d++) direction[d] = (float)((_chance.NextDouble() * 2.0) - 1.0);

        return Encoder.Unit(direction);
    }

    /// <summary>
    /// Mints a region on a direction, cut where the two sides separate best.
    /// </summary>
    /// <remarks>
    /// The cut is the midpoint of the two mean projections. A better cut exists and would make
    /// the arm look better; the midpoint is chosen because the same rule can be applied to the
    /// chance direction, and an arm that beats its control by a cleverer threshold has not
    /// shown what it claims to.
    /// </remarks>
    private int? FromDirection(Commitment parent, float[]? direction, bool paired)
    {
        if (direction is null) return null;
        if (parent.Right.Count == 0 || parent.Wrong.Count == 0) return null;

        var right = parent.Right.Average(one => Project(direction, one, paired));
        var wrong = parent.Wrong.Average(one => Project(direction, one, paired));

        if (Math.Abs(right - wrong) < 1e-6) return null;

        var region = new Region(
            Code: _nextRegion++,
            Direction: direction,
            Threshold: (float)((right + wrong) / 2.0),
            Above: right > wrong,
            OnDifference: paired);

        _regions.Add(region);
        return region.Code;
    }

    private static double Project(float[] direction, Step step, bool paired)
    {
        var total = 0.0;
        for (var d = 0; d < direction.Length; d++) total += direction[d] * Read(step, d, paired);
        return total;
    }
}
