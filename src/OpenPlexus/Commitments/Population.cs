using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Commitments;

/// <summary>What the commitments that fired say should follow, and how strongly.</summary>
public readonly record struct Vote
{
    /// <summary>What was predicted, or nothing if nothing fired.</summary>
    public Code? Expects { get; init; }

    /// <summary>The winning expectation's total weight.</summary>
    public double Weight { get; init; }

    /// <summary>
    /// How far the winner led the runner-up.
    /// </summary>
    /// <remarks>
    /// <b>A confidence, for free.</b> A persistently thin margin is two conflated
    /// cases seen from the outside, which is the signal positing would be built on —
    /// so it is reported from the first run rather than added when it is wanted.
    /// </remarks>
    public double Margin { get; init; }
}

/// <summary>
/// Every commitment a machine holds, and the four things that happen to them.
/// </summary>
/// <remarks>
/// <para>
/// <b>MATCHING IS A BROADCAST TO THE CODES IN THE MOMENT, which is the shape the
/// distributed half already has.</b> A commitment is indexed at each code in its
/// scope, so a moment gathers candidates from the codes it holds and checks only
/// those. XCS scans its whole population; that does not survive scale, and this does
/// not have to.
/// </para>
/// <para>
/// <b>EVERYTHING ITERATED IS ORDERED.</b> A dictionary's order is not stable across
/// runs, and a result that moved with it would be a difference nobody chose — fork
/// 12 has already cost this project twice.
/// </para>
/// </remarks>
public sealed class Population
{
    private readonly CommittingSettings _dials;
    private readonly Random _blind;

    private readonly Naming _names = new();
    private readonly Dictionary<Code, Commitment> _byName = [];
    private readonly Dictionary<Code, List<Commitment>> _byCode = [];

    /// <summary>
    /// What each commitment has forked into, by name.
    /// </summary>
    /// <remarks>
    /// <b>NAMES RATHER THAN A COUNT, so a parent can be asked whether forking PAID.</b>
    /// The count alone answers only how many times it has been tried, which is what a
    /// budget needs and not what a decision does — see
    /// <see cref="Mending.Improving"/>. A child that has since been culled simply
    /// stops being found, which is the right answer: what is not resident is not
    /// evidence.
    /// </remarks>
    private readonly Dictionary<Code, List<Code>> _minted = [];

    /// <param name="dials">Every number the machinery is allowed to have.</param>
    /// <param name="seed">The control arm's generator, used only when it is running.</param>
    public Population(CommittingSettings dials, int seed)
    {
        ArgumentNullException.ThrowIfNull(dials);

        _dials = dials;
        _blind = new Random(seed);
    }

    /// <summary>Every number the brain was handed.</summary>
    /// <remarks>
    /// <b>READ BY <see cref="Cycle"/>, WHICH OWNS THE LOOP AND NOT THE DIALS.</b> Handing
    /// the same settings record to both would be two references to one object with two
    /// chances to be given different ones — and a learning loop configured differently
    /// from the population it drives is the sort of fault that shows up as a mechanism
    /// that does nothing.
    /// </remarks>
    public CommittingSettings Dials => _dials;

    /// <summary>How many commitments are resident.</summary>
    /// <remarks>
    /// <b>Reported beside every score, because an accuracy can be reached by
    /// memorising.</b> On a world whose true rule set is known, a learner at ten
    /// thousand commitments has not found the structure whatever it scores.
    /// </remarks>
    public int Count => _byName.Count;

    /// <summary>Every commitment, in a stable order.</summary>
    public IEnumerable<Commitment> All => _byName.Values.OrderBy(one => one.Identity);

    /// <summary>What has been given a name, and what each name stands for.</summary>
    public Naming Names => _names;

    /// <summary>
    /// A moment with every minted name whose members are present added to it.
    /// </summary>
    /// <param name="raw">What the front end said.</param>
    /// <remarks>
    /// <b>EVERYTHING DOWNSTREAM SEES THE FOLDED MOMENT.</b> Matching, covering and
    /// the tally all take it, so a name can be matched on, minted against, and chosen
    /// as a repair condition — which is what makes a second level of structure
    /// reachable rather than merely representable.
    /// </remarks>
    public IReadOnlySet<Code> Moment(IReadOnlySet<Code> raw) => _names.Fold(raw);

    /// <summary>Whether a commitment with this name is resident.</summary>
    /// <param name="name">What the commitment is called.</param>
    public bool Holds(Code name) => _byName.ContainsKey(name);

    /// <summary>Adds a commitment, unless one by that name is already held.</summary>
    /// <param name="commitment">The commitment to hold.</param>
    /// <returns>Whether it was new.</returns>
    public bool Add(Commitment commitment)
    {
        ArgumentNullException.ThrowIfNull(commitment);

        if (!_byName.TryAdd(commitment.Identity, commitment)) return false;

        foreach (var code in commitment.Scope)
        {
            if (!_byCode.TryGetValue(code, out var at)) _byCode[code] = at = [];
            at.Add(commitment);
        }

        return true;
    }

    /// <summary>Every commitment whose scope is satisfied by this moment.</summary>
    /// <param name="moment">What is live.</param>
    public ImmutableArray<Commitment> Firing(IReadOnlySet<Code> moment)
    {
        ArgumentNullException.ThrowIfNull(moment);

        var seen = new HashSet<Code>();
        var firing = ImmutableArray.CreateBuilder<Commitment>();

        foreach (var code in moment.Order())
        {
            if (!_byCode.TryGetValue(code, out var at)) continue;

            foreach (var commitment in at)
                if (seen.Add(commitment.Identity) && commitment.Fires(moment))
                    firing.Add(commitment);
        }

        firing.Sort((left, right) => left.Identity.CompareTo(right.Identity));

        return firing.ToImmutable();
    }

    /// <summary>The accuracy-weighted vote of everything that fired.</summary>
    /// <param name="firing">What fired, from <see cref="Firing"/>.</param>
    /// <remarks>
    /// <para>
    /// <b>WEIGHTED BY ACCURACY AND NEVER BY HIT COUNT.</b> A commitment that has been
    /// right eight hundred times out of sixteen hundred is not better evidence than
    /// one right nine times out of ten — that is the strength-versus-accuracy
    /// refutation, and it arrives here rather than anywhere it would be expected.
    /// </para>
    /// <para>
    /// <b>An outvoted commitment is still settled.</b> Only the vote picks a winner;
    /// being right or wrong is something each commitment does on its own, which is
    /// what keeps C1 and stops the loudest one owning all the learning.
    /// </para>
    /// </remarks>
    public Vote Predict(ImmutableArray<Commitment> firing)
    {
        if (firing.IsDefaultOrEmpty) return default;

        var weights = new Dictionary<Code, double>();

        foreach (var commitment in firing)
        {
            var weight = Math.Pow(commitment.Accuracy, _dials.Sharpness);

            // A SUM SCALES WITH THE NUMBER OF ADVOCATES AND A MAXIMUM DOES NOT, which
            // is the whole of the difference. Raising accuracy to a power makes a crowd
            // need more members to outvote one commitment that is always right; it
            // never takes the count out of the decision. See `CommittingSettings.Weighing`.
            weights[commitment.Expects] =
                weights.TryGetValue(commitment.Expects, out var so_far)
                    ? _dials.Weighing == Weighing.Strongest
                        ? Math.Max(so_far, weight)
                        : so_far + weight
                    : weight;
        }

        // ORDERED BY WEIGHT AND THEN BY CODE, so a tie -- which is what every
        // moment is before anything has been settled -- breaks the same way on
        // every machine rather than however the dictionary was walked.
        var ranked = weights.OrderByDescending(one => one.Value).ThenBy(one => one.Key).ToList();

        return new Vote
        {
            Expects = ranked[0].Key,
            Weight = ranked[0].Value,
            Margin = ranked[0].Value - (ranked.Count > 1 ? ranked[1].Value : 0.0),
        };
    }

    /// <summary>Tells everything that fired what the settlement said.</summary>
    /// <param name="firing">What fired.</param>
    /// <param name="moment">What was live when it fired.</param>
    /// <param name="arrived">What followed, or nothing if the settlement could not say.</param>
    public void Settle(ImmutableArray<Commitment> firing, IReadOnlySet<Code> moment, Code? arrived)
    {
        ArgumentNullException.ThrowIfNull(moment);

        foreach (var commitment in firing)
            commitment.Settle(
                arrived is null ? Verdict.Abstain
                    : commitment.Expects == arrived ? Verdict.Hit : Verdict.Miss,
                moment,
                _dials.Recency);
    }

    /// <summary>Mints a one-code commitment for everything live, if the moment surprised.</summary>
    /// <param name="moment">What was live.</param>
    /// <param name="arrived">What followed it.</param>
    /// <param name="firing">What fired, for asking whether anything accounted for it.</param>
    /// <returns>How many were new, and zero where nothing was surprising.</returns>
    /// <remarks>
    /// <para>
    /// <b>PROMISCUOUS ON PURPOSE, AND THE GATES DO THE WORK.</b> Popper is generate,
    /// test, constrain; blame and repair are the second and third, and without this
    /// there is no first — nothing to be wrong, so nothing to learn from. One code
    /// rather than the whole moment, because a whole-moment scope never fires twice
    /// and a covering probability is a mode declaration wearing a hat.
    /// </para>
    /// <para>
    /// <b>AND THE GATE THE PLAN NAMED HAD NEVER BEEN MOUNTED, so <i>promiscuous</i>
    /// meant EXHAUSTIVE.</b> Minting on every failure walks the whole
    /// <c>code → outcome</c> space given enough failures: on winnowed CIFAR that space
    /// is 25,600 and the population reached 23,762 against a capacity of 2,000. See
    /// <see cref="Surprising"/> for why <i>nothing proposed it</i> is the condition and
    /// <i>the vote was wrong</i> is not.
    /// </para>
    /// </remarks>
    public int Cover(IReadOnlySet<Code> moment, Code arrived, ImmutableArray<Commitment> firing)
    {
        ArgumentNullException.ThrowIfNull(moment);

        // THE GATE IS READ HERE BECAUSE THIS IS WHERE THE DIALS LIVE. Putting it in
        // `Cycle` would give the learning loop a second opinion about the brain's
        // numbers, and there is exactly one place those are allowed to be read.
        if (_dials.Surprising == Surprising.Unaccounted
            && !firing.IsDefaultOrEmpty
            && firing.Any(one => one.Expects == arrived))
            return 0;

        var minted = 0;

        foreach (var code in moment.Order())
            if (Add(new Commitment([code], arrived))) minted++;

        return minted;
    }

    /// <summary>Repairs the worst commitment that just failed, if any has earned it.</summary>
    /// <param name="firing">What fired.</param>
    /// <param name="arrived">What followed.</param>
    /// <returns>The child that was minted, or nothing.</returns>
    /// <remarks>
    /// <b>THIS IS BLAME, AND IN STEP ONE BLAME IS NOT A RANKING PROBLEM.</b> Every
    /// commitment that fired is right or wrong on its own, so the culprit is simply
    /// the one that was wrong and is worst at its job. Ranking a chain of entailments
    /// is what blame becomes when depth comes off the cap, and diffusion is the
    /// failure waiting there.
    /// </remarks>
    public Commitment? Mend(ImmutableArray<Commitment> firing, Code arrived)
    {
        var culprits = firing
            .Where(one => one.Expects != arrived)
            .Where(one => one.Misses >= _dials.Floor)
            .Where(one => Children(one.Identity) < _dials.Budget)

            // FORK 37'S DRIVER, AND IT IS LAST BECAUSE IT IS THE EXPENSIVE ONE. `Where`
            // is lazy, so this runs only for commitments that already cleared the floor
            // and the budget -- a handful, against the hundreds that fire. Putting it
            // first would make the instrument the cost of the run.
            .Where(one =>
                _dials.Mending == Mending.Outvoted
                || !firing.Any(other => other.Narrows(one)))

            // AND THE PER-COMMITMENT HALF, WHICH ONLY `Improving` ASKS. Last again, for
            // the same reason the child test is: it walks a parent's children, and
            // `Where` is lazy, so it runs for the handful that have already cleared
            // everything else rather than for the hundreds that fire.
            .Where(one => _dials.Mending != Mending.Improving || Improves(one))

            .OrderBy(one => one.Accuracy)
            .ThenBy(one => one.Identity);

        foreach (var culprit in culprits)
        {
            if (Repair.Discriminator(culprit, _dials, _blind) is not { } added) continue;

            var child = new Commitment([.. culprit.Scope, added], culprit.Expects);

            if (!_minted.TryGetValue(culprit.Identity, out var born))
                _minted[culprit.Identity] = born = [];

            born.Add(child.Identity);

            if (Add(child)) return child;
        }

        return null;
    }

    /// <summary>How many children a commitment has minted.</summary>
    /// <param name="name">What the commitment is called.</param>
    private int Children(Code name) =>
        _minted.TryGetValue(name, out var born) ? born.Count : 0;

    /// <summary>
    /// Whether forking this commitment has ever produced a better one.
    /// </summary>
    /// <param name="parent">The commitment being considered for repair.</param>
    /// <remarks>
    /// <b>TRUE WHERE IT HAS NEVER BEEN TRIED, because no evidence is not evidence
    /// against.</b> A parent with no living children has learnt nothing about whether
    /// splitting it helps, and refusing there would turn this into a way of never
    /// repairing rather than a way of stopping when it stops paying.
    /// </remarks>
    private bool Improves(Commitment parent)
    {
        if (!_minted.TryGetValue(parent.Identity, out var born)) return true;

        var living = false;

        foreach (var name in born)
        {
            if (!_byName.TryGetValue(name, out var child)) continue;

            living = true;
            if (child.Accuracy > parent.Accuracy) return true;
        }

        return !living;
    }

    /// <summary>How many commitments have spent their whole repair budget.</summary>
    /// <param name="budget">The budget each was given.</param>
    /// <remarks>
    /// <b>A GUARD HAS TO BE SHOWN NOT TO BE GUARDING, or it is a level nobody
    /// admitted to setting.</b> At eight this bound before it guarded: repair stopped
    /// while the world was still unlearnt, and the flat count of children read
    /// exactly like a ceiling on the mechanism. Anything above zero here means the
    /// number below is deciding what gets learnt.
    /// </remarks>
    public int Exhausted(int budget) => All.Count(one => Children(one.Identity) >= budget);

    /// <summary>
    /// Drops a specific commitment where a general one is at least as good.
    /// </summary>
    /// <returns>How many were dropped.</returns>
    /// <remarks>
    /// <para>
    /// <b>THE GENERAL ONE WINS, WHICH IS THE DIRECTION THAT IS EASY TO GET
    /// BACKWARDS.</b> If a scope and a narrower version of it are equally accurate,
    /// the narrower says nothing extra, needs more evidence to say it, and covers
    /// fewer moments — so keeping it is how a population drifts toward holding one
    /// rule per instance, which is the memorisation this design is otherwise careful
    /// about. XCS's subsumption is this way round for the same reason.
    /// </para>
    /// <para>
    /// <b>Both have to be experienced.</b> Absorbing a child on the strength of a
    /// parent that has barely fired would be deleting evidence in favour of a guess.
    /// </para>
    /// </remarks>
    public int Subsume()
    {
        var experienced = All.Where(one => one.Seen >= _dials.Floor).ToList();

        var doomed = new List<Commitment>();

        foreach (var specific in experienced)
            foreach (var general in experienced)
                if (specific.Narrows(general) && general.Accuracy >= specific.Accuracy)
                {
                    doomed.Add(specific);
                    break;
                }

        foreach (var one in doomed) Remove(one);

        return doomed.Count;
    }

    /// <summary>
    /// Gives a name to the sub-scope most worth one, and rewrites what holds it.
    /// </summary>
    /// <returns>How many commitments were said shorter.</returns>
    /// <remarks>
    /// <para>
    /// <b>THE ONLY OPERATOR HERE THAT GOES UP.</b> Everything else narrows: covering
    /// mints one-code claims, repair adds conditions, subsumption and culling remove.
    /// Without this the machine can be arbitrarily accurate and hold no concept —
    /// every rule of the world learnt, and no name for the thing they share.
    /// </para>
    /// <para>
    /// <b>A REWRITE IS NOT A NEW CLAIM, so the record moves with it.</b> The
    /// commitment entails exactly the moments it did before, because the name is
    /// added to a moment precisely when its members are all there.
    /// </para>
    /// </remarks>
    public int Abstract()
    {
        if (Abstracting.Shared(All, _dials) is not { } shared) return 0;

        var name = _names.Mint(shared);

        var said = 0;

        foreach (var one in All.ToList())
        {
            if (!shared.All(one.Scope.Contains)) continue;

            var scope = one.Scope.Where(code => !shared.Contains(code)).Append(name).ToImmutableArray();

            var shorter = new Commitment(scope, one.Expects);

            // A COLLISION IS A MERGE NOBODY ASKED FOR. Two commitments can be the
            // same claim once the name replaces the members, and taking the record
            // of whichever was rewritten last would be a coin toss deciding what is
            // believed -- so the second is left alone rather than silently folded.
            if (Holds(shorter.Identity)) continue;

            shorter.Carry(one);

            Remove(one);
            Add(shorter);

            said++;
        }

        return said;
    }

    /// <summary>Drops the least accurate commitments when there are too many.</summary>
    /// <returns>How many were dropped.</returns>
    /// <remarks>
    /// <para>
    /// <b>A capacity rather than a level, exactly as `csharp`'s row cap is.</b> What
    /// a machine can afford to hold is a fact about the machine and not about the
    /// run, so there is nothing here for a controller to hunt.
    /// </para>
    /// <para>
    /// <b>AND IT USED TO FILTER TO <c>Seen >= Floor</c> FIRST, WHICH INVERTED IT
    /// ENTIRELY.</b> Inexperienced commitments were immortal, so the ask —
    /// <c>Count - Capacity</c> — routinely exceeded the whole eligible list and the
    /// accuracy ordering never got to choose. Every commitment was deleted the moment
    /// it had enough evidence to be judged, good or bad. On CIFAR the population's
    /// <c>Seen</c> topped out at 19 against a floor of 20, for ten thousand
    /// commitments over forty thousand rounds: not one ever crossed.
    /// </para>
    /// <para>
    /// <b>IT COST MORE THAN HALF THE SCORE — 0.240 against 0.550 at ten-way chance
    /// of 0.100</b>, and it had never fired anywhere else because no earlier world
    /// overshot the capacity at all. <c>Graded</c> holds 371 commitments and the
    /// multiplexer 203, so both sit under the cap and returned identical numbers with
    /// this path disabled. The first world wide enough to reach the cap is the first
    /// one that could have found this.
    /// </para>
    /// <para>
    /// <b>SO EXPERIENCE PROTECTS THE ACCURATE RATHER THAN CONDEMNING EVERYONE.</b> A
    /// commitment with no evidence sorts as if it were exactly average, which is what
    /// it is — XCS deletes young classifiers too and only declines to let their
    /// unformed fitness scale the odds. Making them immortal was the departure.
    /// </para>
    /// </remarks>
    public int Cull()
    {
        if (Count <= _dials.Capacity) return 0;

        // AN UNJUDGED COMMITMENT SORTS AT THE MEDIAN OF THE JUDGED, WHICH IS THE ONLY
        // WORLD-INDEPENDENT PLACE TO PUT IT. A fixed midpoint of 0.5 would be a claim
        // about the world: on a ten-way problem the judged sit near 0.2, so half would
        // rank ABOVE every commitment carrying evidence and the young would be immortal
        // again by another route. The median introduces no dial and cannot be wrong
        // about a world it has not seen.
        var judged = All
            .Where(one => one.Seen >= _dials.Floor)
            .Select(one => one.Accuracy)
            .Order()
            .ToList();

        var unjudged = judged.Count == 0 ? 0.0 : judged[judged.Count / 2];

        var doomed = All
            .OrderBy(one => one.Seen >= _dials.Floor ? one.Accuracy : unjudged)
            .ThenBy(one => one.Seen >= _dials.Floor ? 1 : 0)
            .ThenBy(one => one.Identity)
            .Take(Count - _dials.Capacity)
            .ToList();

        foreach (var one in doomed) Remove(one);

        return doomed.Count;
    }

    private void Remove(Commitment commitment)
    {
        if (!_byName.Remove(commitment.Identity)) return;

        foreach (var code in commitment.Scope)
            if (_byCode.TryGetValue(code, out var at))
            {
                at.RemoveAll(one => one.Identity == commitment.Identity);
                if (at.Count == 0) _byCode.Remove(code);
            }

        _minted.Remove(commitment.Identity);
    }
}
