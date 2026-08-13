using System.Collections.Immutable;
using System.Runtime.InteropServices;
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

    /// <summary>
    /// The best advocate for what won, or nothing if nothing fired.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>WHO DECIDED, WHICH NO REPORT HAS EVER ASKED.</b> Every instrument here counts
    /// the POPULATION — how many are resident, how many are sound, how many were minted
    /// and subsumed — and under the vote an expectation is worth
    /// its best advocate and no more. So a population can be reshuffled at length while
    /// the same few commitments answer every question, and nothing would show it.
    /// </para>
    /// <para>
    /// <b>AND THAT IS FORK 46, ARRIVING FROM A NULL RESULT.</b> Two subsumption rules on
    /// <see cref="Worlds.Arranged"/> left populations differing by a sixth in residents
    /// and a seventh in unsound rules, and returned the identical withheld score on every
    /// seed. Either the deciders are the same handful in both, or that is a coincidence
    /// worth the same surprise.
    /// </para>
    /// <para>
    /// <b>WELL DEFINED UNDER BOTH WEIGHINGS, WHICH IS WHY IT IS THE BEST ADVOCATE RATHER
    /// THAN THE WINNER.</b> A sum has no single winner to name; the strongest advocate
    /// for the winning expectation exists either way, and under
    /// the vote is a maximum, it IS the decision.
    /// </para>
    /// </remarks>
    public Code? By { get; init; }
}

/// <summary>One expectation's case, as the holder that made it would put it.</summary>
/// <remarks>
/// <b>THIS IS WHAT C1 ALLOWS TO CROSS AND THE COMMITMENT ITSELF IS NOT.</b> The plan says
/// a commitment records its OWN hits and misses and TELLS anyone who needs them at the
/// moment it speaks — so what travels is an expectation, a weight already computed from
/// the speaker's own accuracy, and the name of its best advocate. A reader learns what is
/// claimed and never what the claimant is made of.
/// </remarks>
public readonly record struct Advocacy
{
    /// <summary>What is expected to follow.</summary>
    public required Code Expects { get; init; }

    /// <summary>What the holder's own commitments put behind it.</summary>
    public required double Weight { get; init; }

    /// <summary>The strongest single commitment behind it, by identity.</summary>
    public required Code By { get; init; }
}

/// <summary>
/// Everything one holder has to say about a moment — <b>the wire payload of a vote.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE VOTE IS A FOLD AND ITS AGGREGATION IS ASSOCIATIVE, WHICH IS WHY THIS TYPE CAN
/// EXIST AT ALL.</b> The vote keyed weights by expectation and
/// combined them with a maximum or a sum; both compose in any order, so a holder can
/// combine its own commitments first and a reader can combine the results. That is not a
/// property this design was given — it is one it turned out to have, and fork 52 is
/// cheap because of it. See <see cref="Population.Speak"/> and
/// <see cref="Population.Decide"/>, which are the two halves it splits into.
/// </para>
/// <para>
/// <b>AND ORDERED BY EXPECTATION, BECAUSE THE MERGE MUST NOT DEPEND ON WHO SPOKE
/// FIRST.</b> Under C2 the partials arrive in whatever order the network chose. Fork 12
/// has cost this project twice, and a vote that moved with delivery order would be the
/// third time.
/// </para>
/// </remarks>
public readonly record struct Testimony
{
    /// <summary>What the holder's commitments advocate, ordered by expectation.</summary>
    public required ImmutableArray<Advocacy> Advocates { get; init; }

    /// <summary>Nothing fired here, which is a real thing to say and not an absence.</summary>
    /// <remarks>
    /// <b>SILENCE AND ABSENCE ARE DIFFERENT AND C3 IS THE WHOLE REASON.</b> A holder that
    /// answered with nothing has been heard from; a holder that died mid-vote has not, and
    /// the merge may not treat them alike — one closes the count and the other is what
    /// <c>Abstain</c> exists for.
    /// </remarks>
    public bool Silent => Advocates.IsDefaultOrEmpty;
}

/// <summary>What one parent's repair budget has gone on.</summary>
/// <inheritdoc cref="Budgeting"/>
internal sealed class Forks
{
    /// <summary>Every separation this parent has made, re-derivations included.</summary>
    public long Attempts { get; set; }

    /// <summary>The distinct children it has reached.</summary>
    public HashSet<Code> Names { get; } = [];

    /// <summary>The codes it has already forked on.</summary>
    /// <remarks>
    /// <b>NOT DERIVABLE FROM <see cref="Names"/>, WHICH IS WHY IT IS A SECOND SET.</b> A
    /// child's identity is a hash of its whole scope and its expectation, so the code that
    /// was added to reach it cannot be read back out — and a parent that wants to propose
    /// somewhere NEW has to be told where it has been.
    /// </remarks>
    public HashSet<Code> Codes { get; } = [];
}

/// <summary>How a commitment came to be held.</summary>
/// <remarks>
/// <b>Named at the call site rather than inferred from the scope's length</b>, because
/// a one-code scope can arrive from genesis or from a widening and a two-code one from
/// repair or from a rename — and a ledger that guessed would report the operator it
/// expected instead of the one that ran.
/// </remarks>
public enum Birth
{
    /// <summary>Genesis minted it on a surprise.</summary>
    Covered,

    /// <summary>Repair added a condition to a parent.</summary>
    Repaired,

    /// <summary>A generalisation dropped a code from a parent.</summary>
    Widened,

    /// <summary>The same claim said shorter, after a name was minted.</summary>
    Renamed,

}

/// <summary>How a commitment stopped being held.</summary>
/// <inheritdoc cref="Birth"/>
public enum Loss
{
    /// <summary>A general commitment took its place.</summary>
    Subsumed,

    /// <summary>There were too many and it was among the least accurate.</summary>
    Culled,

    /// <summary>It was rewritten over a minted name, so a <see cref="Birth.Renamed"/> answers it.</summary>
    Renamed,
}

/// <summary>
/// What happened to every commitment of one shape — <b>the step between a seed and a
/// finished rule, which nothing has ever counted.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>EVERY LINK IN THE CHAIN IS VERIFIED AND THE CHAIN PRODUCES NOTHING, which is what
/// makes a ledger the next thing rather than a seventh explanation.</b> The minority seeds
/// are present, the separation test beats its control by the whole distance between
/// sixteen sound rules and none, repair runs hundreds of times, true rules come out at
/// perfect accuracy — and not one of them fires on a round the base rate gets wrong. What
/// has never been watched is the middle: a seed is one code and a true rule is three or
/// four, so something has to survive two or three specialisations, and whether anything
/// does is unmeasured.
/// </para>
/// <para>
/// <b>AND THE EXPECTATION IS INVARIANT DOWN A LINEAGE, WHICH IS WHAT MAKES THIS CHEAP.</b>
/// <see cref="Population.Mend"/> builds <c>[..parent.Scope, added]</c> with the PARENT'S
/// expectation and <see cref="Population.Widen"/> drops a code and keeps it, so every
/// descendant of a minority-outcome seed expects the minority outcome forever. No parent
/// pointer is needed to ask which lineage something belongs to — the expectation IS the
/// root's, and the scope's length is how far it has got.
/// </para>
/// <para>
/// <b>THE COUNTS BALANCE, AND THAT IS THE CHECK.</b> Births minus losses at one
/// expectation and length is exactly how many of that shape are resident, computed by
/// walking a completely different table — so a ledger that has missed a call site says so
/// rather than quietly under-reporting a death.
/// </para>
/// </remarks>
public readonly record struct Lifetime
{
    /// <inheritdoc cref="Birth.Covered"/>
    public long Covered { get; init; }

    /// <inheritdoc cref="Birth.Repaired"/>
    public long Repaired { get; init; }

    /// <inheritdoc cref="Birth.Widened"/>
    public long Widened { get; init; }

    /// <inheritdoc cref="Birth.Renamed"/>
    public long Reborn { get; init; }


    /// <inheritdoc cref="Loss.Subsumed"/>
    public long Subsumed { get; init; }

    /// <inheritdoc cref="Loss.Culled"/>
    public long Culled { get; init; }

    /// <inheritdoc cref="Loss.Renamed"/>
    public long Rewritten { get; init; }

    /// <summary>
    /// Repairs that reached this shape and found it already held.
    /// </summary>
    /// <remarks>
    /// <b>A COLLISION IS THE POPULATION HAVING GOT THERE FIRST, and it is counted apart
    /// from a birth because reading it as either one is wrong.</b> As a birth it would
    /// double-count a rule that exists once; as nothing at all it would hide the case
    /// where repair spends its whole budget re-deriving what it already holds — which
    /// would look exactly like a budget that was too small.
    /// </remarks>
    public long Collided { get; init; }

    /// <summary>
    /// Firings at this shape that expected something other than what arrived.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>WHETHER A LINEAGE IS EVEN OFFERED TO THE MACHINERY, WHICH EVERY GATE COUNT SO
    /// FAR HAS ASSUMED.</b> <see cref="Population.Wrong"/> and the five shares under it
    /// partition the candidates that reached <see cref="Population.Mend"/>; none of them
    /// says which lineages reached it at all. A lineage that is never blamed is never
    /// repaired, and from every existing instrument that is indistinguishable from a
    /// lineage the gates refused.
    /// </para>
    /// <para>
    /// <b>AND IT IS THE ONE NUMBER THE VOTE CAN REACH FROM THE GENERATE SIDE.</b> Under
    /// <see cref="Repairing.AfterFailure"/> repair runs only on a round the VOTE got wrong,
    /// so what may be blamed is decided by what the population already answers correctly —
    /// which is not a fact about the commitment being repaired.
    /// </para>
    /// </remarks>
    public long Blamed { get; init; }

    /// <summary>Of those, how many cleared every gate and were offered the search.</summary>
    public long Searched { get; init; }

    /// <summary>Everything that ever entered at this shape.</summary>
    public long Born => Covered + Repaired + Widened + Reborn;

    /// <summary>Everything that ever left it.</summary>
    public long Lost => Subsumed + Culled + Rewritten;
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

    /// <summary>
    /// Per code: the moment it first appeared, and how many it has been live in.
    /// </summary>
    /// <remarks>
    /// <b>TWO NUMBERS, BECAUSE ONE CANNOT TELL ABSENT FROM NOT-YET-ARRIVED.</b> A code
    /// live in every moment it has existed for has varied in nothing; a code live in
    /// every moment SINCE ROUND FOUR HUNDRED is a code that arrived late, which is a
    /// completely different thing and would otherwise read the same.
    /// </remarks>
    private readonly Dictionary<Code, (long First, long Live)> _liveness = [];

    private long _moments;


    private long _wrong, _atFloor, _atBudget, _atCovered, _atImproving, _reached;

    /// <summary>Firing commitments that expected something other than what arrived.</summary>
    /// <remarks>
    /// <para>
    /// <b>THE DENOMINATOR REPAIR HAS NEVER HAD.</b> <see cref="Blamed"/> counts ROUNDS
    /// where something was repairable and <see cref="Unseparated"/> counts rounds where
    /// nothing separated — so between them they say how often the language ran out, and
    /// nothing at all about how many candidates the GATES threw away before the language
    /// was ever consulted.
    /// </para>
    /// <para>
    /// <b>AND THE FIVE BELOW PARTITION THIS EXACTLY</b>, each candidate charged to the
    /// first gate that refused it, so a share that is large is a gate that is deciding
    /// the run.
    /// </para>
    /// </remarks>
    public long Wrong => _wrong;

    /// <summary>Of those, refused for having missed too few times.</summary>
    public long AtFloor => _atFloor;

    /// <summary>Refused for having spent their whole repair budget.</summary>
    public long AtBudget => _atBudget;

    /// <summary>Refused because a child already covers the failure.</summary>
    public long AtCovered => _atCovered;

    /// <summary>Refused because forking them has never yet paid.</summary>
    public long AtImproving => _atImproving;

    /// <summary>Cleared every gate and was offered the candidate search.</summary>
    /// <remarks>
    /// <b>THE ONLY ONE OF THE FIVE THAT REACHES THE SCOPE LANGUAGE AT ALL</b>, so the
    /// ladder's trigger is a statement about this number and not about
    /// <see cref="Wrong"/>. A run where almost nothing reaches here has not discovered
    /// that its language is too weak; it has discovered that its gates are strict.
    /// </remarks>
    public long Searched => _reached;

    private long _asked, _spoke;
    private long _atScarce, _atUnpaired, _atRare, _atIndependent, _atUncertain;

    /// <summary>
    /// How many times rung five was asked for a name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE DENOMINATOR EVERY NAMING NUMBER IN THIS REPO HAS BEEN AN ABSOLUTE
    /// AGAINST.</b> <see cref="Naming.Count"/> says how many names exist and nothing about
    /// how many chances produced them, so a cell asked more often and a cell answering more
    /// often are one number. This is fixed by the sweep calendar rather than by the search,
    /// which is what makes it comparable across every dial that moves the population.
    /// </para>
    /// <para>
    /// <b>AND <see cref="Spoke"/> PLUS THE FIVE REFUSALS PARTITION IT EXACTLY</b>, each ask
    /// charged to the first bar that stopped it — the same shape as the repair gate's five,
    /// for the same reason. A share that is large is a bar that is deciding the run.
    /// </para>
    /// </remarks>
    public long Asked => _asked;

    /// <summary>Of those, the asks that proposed a name.</summary>
    /// <remarks>
    /// <b>NOT THE SAME AS NAMES MINTED, AND THE GAP IS A REWRITE THAT COLLIDED.</b> A
    /// proposal always mints; what it then fails to do is shorten anything, because the
    /// rewritten claim is already held.
    /// </remarks>
    public long Spoke => _spoke;

    /// <inheritdoc cref="Refused.Scarce"/>
    public long AtScarce => _atScarce;

    /// <inheritdoc cref="Refused.Unpaired"/>
    public long AtUnpaired => _atUnpaired;

    /// <inheritdoc cref="Refused.Rare"/>
    public long AtRare => _atRare;

    /// <inheritdoc cref="Refused.Independent"/>
    public long AtIndependent => _atIndependent;

    /// <inheritdoc cref="Refused.Uncertain"/>
    public long AtUncertain => _atUncertain;

    /// <summary>
    /// What the gate saw the last time it was asked, or nothing where it never was.
    /// </summary>
    /// <remarks>
    /// <b>THE COUNTS BEHIND THE REFUSAL, BECAUSE TWO OPPOSITE MECHANISMS LAND ON
    /// <see cref="Refused.Uncertain"/>.</b> The bar is a tail divided among the candidates,
    /// so it tightens when the evidence weakens AND when the search widens — and a count of
    /// refusals cannot tell a population that stopped sharing anything from one that started
    /// sharing too many things. <see cref="Proposed.Strongest"/> and
    /// <see cref="Proposed.Candidates"/> separate them.
    /// <b>One reading and not a running mean</b>: at the end of a run this is the state the
    /// population finished in, which is what every other resident count here already is.
    /// </remarks>
    public Proposed? Lately { get; private set; }

    private readonly Dictionary<Code, Commitment> _byName = [];
    private readonly Dictionary<Code, List<Commitment>> _byCode = [];

    /// <summary>
    /// What each commitment has forked into, by name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>NAMES RATHER THAN A COUNT, so a parent can be asked whether forking PAID.</b>
    /// The count alone answers only how many times it has been tried, which is what a
    /// budget needs and not what a decision does — see
    /// <see cref="Mending.Improving"/>. A child that has since been culled simply
    /// stops being found, which is the right answer: what is not resident is not
    /// evidence.
    /// </para>
    /// <para>
    /// <b>AND BOTH, BECAUSE THEY ARE DIFFERENT NUMBERS AND THIS WAS A LIST THAT HELD ONE OF
    /// THEM TWICE.</b> A repair reaching a scope the population already holds appended the
    /// same name again, so the names were a multiset — the attempt count wearing the shape
    /// of a child set. Counting the distinct entries of that list on every gate was
    /// quadratic in a parent's attempts and made the instrument the cost of the run, which
    /// is the reason this is two fields rather than one derived from the other.
    /// </para>
    /// </remarks>
    private readonly Dictionary<Code, Forks> _minted = [];

    /// <summary>What each child's PARENT would have added second, by the child's name.</summary>
    /// <remarks>
    /// <b>AN INSTRUMENT AND NOT A MECHANISM, so nothing reads it to decide anything.</b> See
    /// <see cref="Agreed"/>.
    /// </remarks>
    private readonly Dictionary<Code, Code> _runners = [];

    private long _agreed, _differed;

    /// <inheritdoc cref="Lifetime"/>
    private readonly Dictionary<(Code Expects, int Depth), Lifetime> _lineage = [];

    /// <summary>
    /// Which holder a commitment sits on, or nothing while everything is in one place.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE GATE ON REPAIR IS THE ONE MECHANISM THAT ASKS ABOUT COMMITMENTS IT DOES NOT
    /// OWN.</b> <see cref="Mending.Uncovered"/> refuses a repair when another firing
    /// commitment already narrows it, and in one process <c>firing</c> is everything that
    /// fired. On a ring it is only what this holder was placed with — measured in
    /// <c>SplitRepairTests</c>, where at twelve holders a holder can see about a twelfth
    /// of what covers it.
    /// </para>
    /// <para>
    /// <b>SO THIS IS THE DEPLOYMENT ARRIVING RATHER THAN A TEST HOOK.</b> A holder has a
    /// placement whether or not anything asks it, and the gate is where the answer first
    /// differs. Left null nothing changes and the mechanism is exactly what it was, which
    /// is the baseline every arm is measured from.
    /// </para>
    /// <para>
    /// <b>AND IT REACHES NOTHING ELSE ON PURPOSE.</b> Firing, voting and settling are
    /// untouched by it, so a run with this set differs from one without in the repair gate
    /// ALONE — measuring one mechanism on from a known baseline rather than a sharded
    /// world against a whole one, where four things moved and the score could not say
    /// which.
    /// </para>
    /// </remarks>
    public Func<Commitment, ulong>? Placing { get; set; }

    /// <summary>
    /// Whether a commitment genesis proposes belongs on this machine, or nothing where it
    /// is the only machine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>GENESIS IS THE ONE OPERATOR THAT WOULD RUN N TIMES ON A FLEET AND MINT ONE
    /// THING.</b> Every holder sees every observation, so every holder is surprised by the
    /// same failure and proposes the same one-code rules — a fleet of twelve holding twelve
    /// copies of one population, which is not a shard and is not a distribution. Placement
    /// is what makes the copies disjoint, and it is a fact about the commitment rather than
    /// about the moment, so every machine reaches the same answer without being told.
    /// </para>
    /// <para>
    /// <b>AND IT GATES GENESIS ALONE, WHICH DECIDES WHERE A REPAIR'S CHILD LIVES.</b> A
    /// child hashes wherever it hashes, and refusing it here would delete it outright —
    /// nothing else mints it, because repair is the only thing that proposes a scope longer
    /// than one code. So a child stays with its parent, which is also what makes
    /// <see cref="Mending.Uncovered"/> answerable locally: the one commitment that could
    /// cover a child is on the same machine as the child. Fork 3 is whether that locality
    /// is worth what a uniform ring gives up.
    /// </para>
    /// <para>
    /// <b>AND THE ASYMMETRY HAS A PRICE NOBODY DESIGNED, WHICH IS FORK 60.</b> <c>Mend</c>
    /// mints at most one child per call and the loop calls it once a round per POPULATION —
    /// so a fleet of three repairs up to three times a round where one machine repairs
    /// once, while genesis, being placed, mints exactly what one machine would. How hard a
    /// fleet searches is then a function of how many machines it has, which is a deployment
    /// reaching into the brain one level out from the rule about worlds. Measured at eleven
    /// bits, where it PAYS.
    /// </para>
    /// <para>
    /// <b>NULL IS EVERY MEASUREMENT EVER TAKEN</b>, so a one-process run does not pay a
    /// predicate for a distribution it does not have.
    /// </para>
    /// </remarks>
    public Func<Commitment, bool>? Places { get; set; }

    /// <summary>
    /// Which codes are ALTERNATIVES, or nothing where the front end has not said.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE SAME OBJECT THE FRONT END FOLDS, AND THAT IS A REQUIREMENT RATHER THAN A
    /// CONVENIENCE.</b> A category reaches a moment only because <see cref="Sorting.Folded"/>
    /// put it there, so a population handed a different vocabulary would read an entailment
    /// between codes no moment ever holds together.
    /// </para>
    /// <para>
    /// <b>AND IT IS TOLD RATHER THAN DERIVED, WHICH IS FORK 84'S SEAM AND NOT A HOLE.</b>
    /// What the coarser form of a code is is a fact about how a stream is being read; what
    /// may be DONE with it is the brain's, and is <see cref="Subsume"/>.
    /// </para>
    /// <para>
    /// <b>NULL IS EVERY MEASUREMENT TAKEN BEFORE THIS EXISTED</b>, and it short-circuits
    /// both readers, so a run without categories pays nothing for them.
    /// </para>
    /// </remarks>
    public Sorting? Sorts { get; set; }

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

    /// <summary>
    /// Children whose own first repair chose the code their PARENT'S table ranked second.
    /// </summary>
    /// <remarks>
    /// <b>FORK 74 IN TWO COUNTERS.</b> A four-code truth costs three miss floors because a
    /// fresh child re-earns one before adding the next; if a parent's table already knows
    /// which code the child will want, the chain could be walked in one pass. Read against
    /// <see cref="Differed"/> — a high share means the saving is real, a low one means
    /// conditioning on the first code is what the second choice needed.
    /// </remarks>
    public long Agreed => _agreed;

    /// <inheritdoc cref="Agreed"/>
    /// <summary>Children whose own first repair chose something else.</summary>
    public long Differed => _differed;

    /// <summary>How many commitments are resident.</summary>
    /// <remarks>
    /// <b>Reported beside every score, because an accuracy can be reached by
    /// memorising.</b> On a world whose true rule set is known, a learner at ten
    /// thousand commitments has not found the structure whatever it scores.
    /// </remarks>
    public int Count => _byName.Count;

    /// <summary>Every commitment, in a stable order.</summary>
    public IEnumerable<Commitment> All => _byName.Values.OrderBy(one => one.Identity);

    /// <inheritdoc cref="Lifetime"/>
    /// <remarks>
    /// <b>KEYED BY EXPECTATION AND SCOPE LENGTH, WHICH IS A LINEAGE AND ITS RUNG.</b> See
    /// <see cref="Lifetime"/> for why those two are enough to say which lineage a
    /// commitment belongs to without any commitment holding a pointer to its parent.
    /// </remarks>
    public IReadOnlyDictionary<(Code Expects, int Depth), Lifetime> Lineages => _lineage;

    /// <summary>Notes that a commitment of this shape entered.</summary>
    /// <param name="commitment">What was added.</param>
    /// <param name="how">Which operator added it.</param>
    private void Born(Commitment commitment, Birth how)
    {
        ref var life = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _lineage, (commitment.Expects, commitment.Scope.Length), out _);

        life = how switch
        {
            Birth.Covered => life with { Covered = life.Covered + 1 },
            Birth.Repaired => life with { Repaired = life.Repaired + 1 },
            Birth.Widened => life with { Widened = life.Widened + 1 },
            _ => life with { Reborn = life.Reborn + 1 },
        };
    }

    /// <summary>Notes a repair that reached a shape already held.</summary>
    /// <param name="child">The child that was proposed.</param>
    private void Collided(Commitment child)
    {
        ref var life = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _lineage, (child.Expects, child.Scope.Length), out _);

        life = life with { Collided = life.Collided + 1 };
    }

    /// <summary>Notes a firing that expected the wrong thing, and how far it got.</summary>
    /// <param name="one">The commitment that was wrong.</param>
    /// <param name="searched">Whether it cleared every gate.</param>
    private void Charge(Commitment one, bool searched)
    {
        ref var life = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _lineage, (one.Expects, one.Scope.Length), out _);

        life = life with
        {
            Blamed = life.Blamed + 1,
            Searched = life.Searched + (searched ? 1 : 0),
        };
    }

    /// <summary>What has been given a name, and what each name stands for.</summary>
    public Naming Names => _names;

    /// <summary>
    /// A moment with every minted name whose members are present added to it.
    /// </summary>
    /// <param name="raw">What the front end said.</param>
    /// <remarks>
    /// <para>
    /// <b>EVERYTHING DOWNSTREAM SEES THE FOLDED MOMENT.</b> Matching, covering and
    /// the tally all take it, so a name can be matched on, minted against, and chosen
    /// as a repair condition — which is what makes a second level of structure
    /// reachable rather than merely representable.
    /// </para>
    /// <para>
    /// <b>AND A PRECEDENCE IS ALREADY IN <paramref name="raw"/> BY THE TIME IT GETS HERE,
    /// WHICH IS A DECISION ABOUT THE WIRE AND NOT ABOUT THE FOLD.</b> Rung three is derived
    /// where the moment is FORMED — see <see cref="Machines.Trial{TSeen}"/> — because a
    /// fleet broadcasts the moment as a set of codes and a
    /// precedence IS one. Deriving it here instead would mean every holder needed the
    /// front end's order report on the wire beside the moment it already has.
    /// </para>
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

        return Decide([Speak(firing)]);
    }


    /// <summary>
    /// What this holder's fired commitments have to say, with nothing of the commitments
    /// in it — <b>the half of a vote that a machine may compute alone.</b>
    /// </summary>
    /// <param name="firing">What fired, from <see cref="Firing"/>.</param>
    /// <remarks>
    /// <para>
    /// <b>WEIGHTED BY ACCURACY AND NEVER BY HIT COUNT</b>, which is the
    /// strength-versus-accuracy refutation and belongs here because here is where a
    /// weight is made. A commitment right eight hundred times in sixteen hundred is not
    /// better evidence than one right nine times in ten.
    /// </para>
    /// <para>
    /// <b>AND WHAT LEAVES IS AN ACCURACY AND NOTHING DONE TO IT.</b> A power used to be
    /// applied here, on the argument that a brain dial is the speaker's business; the dial
    /// is gone, and with it the way two holders disagreeing about it would have been
    /// undetectable.
    /// </para>
    /// </remarks>
    public Testimony Speak(ImmutableArray<Commitment> firing)
    {
        if (firing.IsDefaultOrEmpty) return new Testimony { Advocates = [] };

        var weights = new Dictionary<Code, double>();

        // THE BEST ADVOCATE PER EXPECTATION, KEPT WHATEVER THE AGGREGATE IS. Under
        // `Strongest` this is the decision itself; under `Summing` it is who spoke
        // loudest for the side that won. Firing is already in identity order, so a tie
        // resolves the same way on every machine.
        var loudest = new Dictionary<Code, (double Weight, Code By)>();

        foreach (var commitment in firing)
        {
            // A PROVISIONAL WEIGHT IS NOT AN EARNED ONE, AND ONLY THIS READER EVER
            // CONFLATED THEM. Below the floor an accuracy is an average over a handful of
            // firings, so a commitment right once carries a perfect one -- and subsumption
            // and culling both already refuse to weigh anything down here. Skipped rather
            // than discounted, because a discount is a number and the floor is not.
            if (_dials.Speaking == Speaking.Experienced && commitment.Seen < _dials.Floor)
                continue;

            // ACCURACY ITSELF, WITH NO POWER OVER IT. A power was applied here, and it was
            // the workaround for a summed vote's shape: a sum over N advocates scales with N
            // however steeply each is weighted, so raising it only made a crowd need more
            // members. A maximum does not scale with N at all, and raising a maximum to a
            // power is monotone -- it cannot move an argmax. Both are deleted rather than
            // defaulted, and the plan carries their revival rows.
            var weight = commitment.Accuracy;

            if (!loudest.TryGetValue(commitment.Expects, out var best) || weight > best.Weight)
                loudest[commitment.Expects] = (weight, commitment.Identity);

            // AN EXPECTATION IS WORTH ITS BEST ADVOCATE AND NO MORE, which is the whole of
            // the strength-versus-accuracy refutation arriving through the vote. A thousand
            // mediocre rules cannot outvote one that is always right, and the NUMBER of
            // voters stops being part of the answer at any scale.
            weights[commitment.Expects] =
                weights.TryGetValue(commitment.Expects, out var so_far)
                    ? Math.Max(so_far, weight)
                    : weight;
        }

        return new Testimony { Advocates = Spell(weights, loudest) };
    }

    /// <summary>Puts a weight table into the one order everything downstream assumes.</summary>
    /// <param name="weights">Weight per expectation.</param>
    /// <param name="loudest">Best advocate per expectation.</param>
    private static ImmutableArray<Advocacy> Spell(
        Dictionary<Code, double> weights,
        Dictionary<Code, (double Weight, Code By)> loudest)
    {
        var advocates = ImmutableArray.CreateBuilder<Advocacy>(weights.Count);

        foreach (var expects in weights.Keys.Order())
            advocates.Add(new Advocacy
            {
                Expects = expects,
                Weight = weights[expects],
                By = loudest[expects].By,
            });

        return advocates.MoveToImmutable();
    }

    /// <summary>
    /// The vote, out of everything that was heard — <b>one holder or twenty, by the same
    /// arithmetic.</b>
    /// </summary>
    /// <param name="heard">What each holder said. Order is not read.</param>
    /// <remarks>
    /// <para>
    /// <b>STATIC AND HOLDING NOTHING, BECAUSE THE MERGER IS NOT A PARTICIPANT.</b> Whoever
    /// takes the vote may hold no commitments at all — an input machine asking a question
    /// is the ordinary case — and a method that reached into a population to finish a vote
    /// would make the asker a twenty-first voter without saying so.
    /// </para>
    /// <para>
    /// <b>THE MERGE IS CANONICAL IN THE CONTRIBUTIONS AND NOT IN THE ARRIVALS.</b> Every
    /// advocacy for one expectation is collected, then ordered by weight and identity
    /// before anything is combined — so a partial that arrived last folds in exactly where
    /// it would have folded in had it arrived first. C2 says the order is the network's
    /// choice; fork 12 says a result that moves with a choice nobody made is a defect, and
    /// it has been reopened twice already.
    /// </para>
    /// <para>
    /// <b>AND A SHARDED VOTE IS BIT-IDENTICAL TO A WHOLE ONE, WHICH A SUM COULD NEVER
    /// BE.</b> Floating-point addition is not associative, and a holder summing its own
    /// advocates before the merge saw them made <c>(a+b)+c</c> and <c>a+(b+c)</c> the two
    /// arrangements — differing in the last bits, so the split was only ever asserted
    /// approximately. A maximum of maxima is a maximum exactly, at any number of holders.
    /// </para>
    /// </remarks>
    public static Vote Decide(IReadOnlyCollection<Testimony> heard)
    {
        ArgumentNullException.ThrowIfNull(heard);

        var cases = new Dictionary<Code, List<Advocacy>>();

        foreach (var testimony in heard)
        {
            // A SILENT HOLDER IS SKIPPED AND A DEAD ONE NEVER ARRIVED, and the difference
            // is invisible here on purpose — whether enough was heard to decide at all is
            // the caller's question, since only the caller knows how many it asked.
            if (testimony.Silent) continue;

            foreach (var advocacy in testimony.Advocates)
            {
                if (!cases.TryGetValue(advocacy.Expects, out var at))
                    cases[advocacy.Expects] = at = [];

                at.Add(advocacy);
            }
        }

        if (cases.Count == 0) return default;

        var weights = new Dictionary<Code, double>(cases.Count);
        var loudest = new Dictionary<Code, (double Weight, Code By)>(cases.Count);

        foreach (var (expects, at) in cases)
        {
            at.Sort(static (left, right) =>
                right.Weight.CompareTo(left.Weight) is var order && order != 0
                    ? order
                    : left.By.CompareTo(right.By));

            loudest[expects] = (at[0].Weight, at[0].By);

            // A MAXIMUM, AND THE MERGE NEEDS TO KNOW NOTHING ELSE. What arrives is a
            // weight per expectation per holder; the best of them is the answer, and
            // taking a maximum of maxima is the same operation however many holders spoke.
            // That is what makes the vote split exactly rather than approximately.
            weights[expects] = at[0].Weight;
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
            By = loudest[ranked[0].Key].By,
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
        {
            // AND THE SECOND GATE ASKS WHICH CODE RATHER THAN WHETHER AT ALL. A code that
            // has never once been absent separates nothing and cannot ever win a repair,
            // but it can still be a ROOT -- and every child hanging off it inherits the
            // useless code while being otherwise a perfectly good rule. Half the resident
            // population, on eight bits of pure background.
            //
            // NOT A DIAL, BECAUSE THERE IS NO LEVEL IN IT AND BECAUSE IT WON. Measured
            // over twelve seeds against the arm that rooted on anything: 7.4 standard
            // errors ahead where there is background and 0.2 apart where there is none.
            // A base rate against a threshold would have needed the threshold, and
            // nothing computed inside a run can say what it should be -- *has it ever
            // been absent* has one answer and needs nothing.
            if (!Varied(code)) continue;

            // AND A PRECEDENCE IS A SPECIALISATION AND NEVER A ROOT, which is the same
            // argument the line above makes about a code that has never been absent. `this
            // stood before that` with no idea what either of them is about is a rule about
            // grammar rather than about the world -- and with the order folded in, the
            // moment holds a precedence for every pair, so rooting on them would fill the
            // population with pairs the day the rung is switched on.
            //
            // REPAIR MAY STILL CHOOSE ONE, which is the whole point: a precedence enters a
            // scope where a plain code does not separate the misses from the hits, which is
            // what the ladder's admission asks and the only place this rung belongs.
            if (Sequenced.Names(code)) continue;

            var proposed = new Commitment([code], arrived);

            // AND THE THIRD GATE ASKS WHOSE IT IS, which is nothing at all on one machine.
            // See `Places`: every holder is surprised by the same failure and proposes the
            // same rules, so without this a fleet holds N copies of one population.
            if (Places is not null && !Places(proposed)) continue;

            if (!Add(proposed)) continue;

            Born(proposed, Birth.Covered);
            minted++;
        }

        return minted;
    }

    /// <summary>
    /// Notes which codes were live, so absence can be told from never-having-appeared.
    /// </summary>
    /// <param name="moment">What is live.</param>
    /// <remarks>
    /// <b>ONE ADD PER LIVE CODE AND NO SWEEP OVER WHAT IS KNOWN.</b> The obvious way to
    /// ask whether a code has ever been absent is to walk everything known and subtract
    /// the moment, which is linear in the vocabulary on every round and would cost more
    /// on a wide world than the thing it is trying to save. Counting how many moments a
    /// code has been live, against how many have passed since it first appeared, answers
    /// the same question by arithmetic.
    /// </remarks>
    public void Witness(IReadOnlySet<Code> moment)
    {
        ArgumentNullException.ThrowIfNull(moment);

        _moments++;

        foreach (var code in moment)
        {
            ref var seen = ref CollectionsMarshal.GetValueRefOrAddDefault(
                _liveness, code, out var existed);

            if (!existed) seen = (First: _moments, Live: 0);

            seen = seen with { Live = seen.Live + 1 };
        }
    }

    /// <summary>Whether a code has been absent from a moment since it first appeared.</summary>
    /// <param name="code">The code to ask about.</param>
    /// <remarks>
    /// <b>A code live in every moment since it arrived has varied in nothing</b>, so its
    /// presence is not evidence about anything and a commitment rooted on it is a
    /// commitment about the world existing.
    /// </remarks>
    private bool Varied(Code code) =>
        _liveness.TryGetValue(code, out var seen) && seen.Live < (_moments - seen.First + 1);

    /// <summary>
    /// Rounds where repair had a commitment it was allowed to fix.
    /// </summary>
    /// <remarks>
    /// <b>THE DENOMINATOR OF THE LADDER'S TRIGGER, AND WITHOUT IT THE NUMERATOR SAYS
    /// NOTHING.</b> A run that never repairs because the floor and the budget refuse
    /// everything looks, from the score, exactly like a run whose language cannot express
    /// what separates its failures — and those are opposite diagnoses. One says the gates
    /// are too tight and the other says the rung is needed.
    /// </remarks>
    public long Blamed { get; private set; }

    /// <summary>
    /// Of those, rounds where <b>no condition cleared the bar for any culprit.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THIS IS THE SIGNAL THE WHOLE DESIGN EXISTS FOR, AND IT HAS BEEN COMPUTED EVERY
    /// ROUND AND READ BY NOTHING.</b> The plan says the language extends when, and only
    /// when, no expression in the current one separates the failures from the hits — and
    /// that <i>is decidable and already computed</i>, because it is exactly what
    /// <see cref="Repair.Discriminator"/> returning nothing means. Choosing a rung before
    /// this number is read is hand-specified bias by a side door, which is the fault that
    /// killed ILP.
    /// </para>
    /// <para>
    /// <b>AND IT IS NOT MERELY <i>REPAIR FOUND NOTHING</i>, WHICH IS A WEAKER EVENT.</b> A
    /// child that clears the bar and collides with a name already held is the language
    /// REACHING and the population already holding the answer; counting that here would
    /// read a success as a ceiling. Only a round where every eligible culprit was offered
    /// the whole candidate set and none of it separated counts.
    /// </para>
    /// </remarks>
    public long Unseparated { get; private set; }

    /// <summary>
    /// Of the rounds nothing separated, how many an ABSENCE would have separated.
    /// </summary>
    /// <remarks>
    /// <b>THE HALF OF FORK 50 A SHARE CANNOT ANSWER.</b> <see cref="Unseparated"/> says the
    /// language is short and says nothing about what would lengthen it — a counting
    /// concept, a negation and a disjunction all produce failures a conjunction cannot
    /// describe. This asks the one of those three that is cheap to ask: whether a code the
    /// commitment has seen before and is now MISSING separates its misses from its hits.
    /// High, and rung two is demanded specifically; low, and negation is not the answer and
    /// the demand is for something far more expensive.
    /// </remarks>
    public long Absented { get; private set; }

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
        // WHERE THE CANDIDATES DIE, COUNTED BEFORE THE CHAIN RATHER THAN INSIDE IT. The
        // chain below is lazy and stops at the first child it manages to add, so counting
        // from within it would report where the EXAMINED ones died and call it where the
        // gates refuse -- two different numbers, and only the second says what a gate
        // excludes. This walks every wrong commitment once, in the gate order, so each is
        // charged to its FIRST refusal and the expensive tests still run only for the
        // handful that reach them.
        foreach (var one in firing)
        {
            if (one.Expects == arrived) continue;

            _wrong++;

            var searched = false;

            if (!PastFloor(one)) _atFloor++;
            else if (!PastBudget(one)) _atBudget++;
            else if (!Uncovered(one, firing)) _atCovered++;
            else if (!PastImproving(one)) _atImproving++;
            else
            {
                _reached++;
                searched = true;
            }

            // AND THE SAME WALK CHARGED TO A LINEAGE RATHER THAN TO A GATE. The five
            // shares above say which gate refuses candidates; this says which lineages
            // arrived to be refused, and a lineage that is never blamed reads identically
            // to one every gate turned away. Read off the chain rather than recomputed,
            // because the expensive two gates are why that chain is ordered as it is.
            Charge(one, searched);
        }

        var culprits = firing
            .Where(one => one.Expects != arrived)
            .Where(PastFloor)
            .Where(PastBudget)

            // FORK 37'S DRIVER, AND IT IS LAST BECAUSE IT IS THE EXPENSIVE ONE. `Where`
            // is lazy, so this runs only for commitments that already cleared the floor
            // and the budget -- a handful, against the hundreds that fire. Putting it
            // first would make the instrument the cost of the run.
            .Where(one => Uncovered(one, firing))

            // AND THE PER-COMMITMENT HALF, WHICH ONLY `Improving` ASKS. Last again, for
            // the same reason the child test is: it walks a parent's children, and
            // `Where` is lazy, so it runs for the handful that have already cleared
            // everything else rather than for the hundreds that fire.
            .Where(PastImproving)

            .OrderBy(one => one.Accuracy)
            .ThenBy(one => one.Identity);

        // COUNTED ON EVERY PATH OUT, INCLUDING THE ONE THAT SUCCEEDS. See `Unseparated`:
        // the two flags separate "nothing was allowed to be repaired" from "everything
        // allowed was offered the whole candidate set and none of it separated", and the
        // second is the only thing that may ever summon a rung.
        var blamed = false;
        var separated = false;
        var absent = false;

        try
        {
            foreach (var culprit in culprits)
            {
                blamed = true;

                // WHERE THIS PARENT HAS ALREADY FORKED, WHICH ONLY `Distinct` READS. A
                // parent with no ledger yet has forked nowhere, so a missing entry and an
                // empty set are the same answer and neither needs a branch here.
                _minted.TryGetValue(culprit.Identity, out var ledger);

                if (Repair.Discriminator(culprit, _dials, _blind, ledger?.Codes)
                    is not { } added)
                {
                    // AND A REFUSAL BY THE ARM IS NOT A CEILING IN THE LANGUAGE, WHICH IS THE
                    // ONE PLACE `Forking.Distinct` COULD HAVE CORRUPTED A SIGNAL SILENTLY.
                    // `Unseparated` is the ladder's trigger -- no expression in the current
                    // language separates the failures from the hits -- and a parent that has
                    // already forked on every code in its table comes back empty for a
                    // completely different reason. Counting that as a ceiling would summon a
                    // rung on evidence that the search had merely been everywhere, which is
                    // the hand-specified-bias failure arriving through an instrument.
                    // ASKED AGAIN WITHOUT THE LEDGER, and only on the path that already came
                    // back empty -- a second walk of one table on a small share of rounds,
                    // which is the same price the absence probe below is charged.
                    // AND ONLY WHERE THE SEARCH IS DETERMINISTIC, or asking twice would draw
                    // twice. The control arm picks its condition from `_blind`, so a second
                    // call there would consume the stream and make the arm's random sequence
                    // a function of how often a parent had exhausted its codes -- fork 12 by
                    // a door nobody would think to check.
                    if (ledger is not null
                        && _dials.Forking == Forking.Distinct
                        && _dials.Choosing == Choosing.Separating
                        && Repair.Discriminator(culprit, _dials, _blind) is not null)
                    {
                        separated = true;
                        continue;
                    }

                    // THE PROBE, AND IT MINTS NOTHING. See `Absented`: asked only where the
                    // present-code search came back empty, so it costs a second walk of one
                    // table on a small share of a small share of rounds -- and it may not
                    // change what this method does, or the instrument would be the rung.
                    if (!absent && Repair.Absent(culprit, _dials) is not null) absent = true;

                    continue;
                }

                // THE LANGUAGE REACHED, WHICH IS TRUE EVEN IF THE CHILD IS ALREADY HELD.
                // A collision is the population having got there first, and reading it as
                // a ceiling would turn a success into a demand for a rung.
                separated = true;

                // FORK 74'S READING, AND IT MINTS NOTHING. What the parent's table would have
                // picked SECOND is recorded against the child, so that when the child later
                // repairs from its OWN table the two choices can be compared. Agreement means
                // one table could have picked both codes and the chain is a saving waiting to
                // be taken; disagreement means conditioning on the first code is what the
                // second choice needed, which is what a chain buys and one pass cannot.
                // AND ONCE PER CHILD, WHICH IS WHAT THE COUNTER CLAIMS TO BE. A child may be
                // repaired many times; comparing on every one of them would weigh a
                // much-repaired lineage more heavily than a lineage repaired once, and the
                // question is about a table predicting a choice rather than about how often
                // a parent is chosen. The entry is spent when it is read.
                if (_runners.Remove(culprit.Identity, out var predicted))
                {
                    if (predicted == added) _agreed++;
                    else _differed++;
                }

                var child = new Commitment([.. culprit.Scope, added], culprit.Expects);

                if (Repair.Runner(culprit, _dials) is { } runner)
                    _runners[child.Identity] = runner;

                if (ledger is null) _minted[culprit.Identity] = ledger = new Forks();

                ledger.Attempts++;
                ledger.Names.Add(child.Identity);

                // AND WHERE IT WENT, CHARGED WHETHER OR NOT THE CHILD WAS NEW. A collision
                // is this parent having reached that scope already, so recording it only on
                // a birth would let `Distinct` propose the same place forever whenever the
                // population got there first.
                ledger.Codes.Add(added);

                if (Add(child))
                {
                    Born(child, Birth.Repaired);
                    return child;
                }

                // THE RUNG WAS REACHED AND SOMETHING WAS ALREADY STANDING ON IT. Counted
                // here rather than left silent: a lineage whose whole budget goes on
                // re-deriving what the population holds reads, from every other
                // instrument, as a budget that was too small.
                Collided(child);
            }

            return null;
        }
        finally
        {
            if (blamed)
            {
                Blamed++;

                if (!separated)
                {
                    Unseparated++;
                    if (absent) Absented++;
                }
            }
        }
    }

    /// <summary>
    /// Proposes each experienced never-wrong commitment with one code taken out —
    /// <b>the only thing here that makes a scope shorter.</b>
    /// </summary>
    /// <returns>How many were new.</returns>
    /// <remarks>
    /// <para>
    /// <b>THE MIRROR OF <see cref="Mend"/>, AND ON THE SWEEP RATHER THAN ON A FAILURE.</b>
    /// A failure is what summons a repair; nothing summons a generalisation, because the
    /// commitment asking for one is by construction the one that has never been wrong. So
    /// its trigger is redundancy in the same sense rung five's is, and it runs where the
    /// other periodic operators run.
    /// </para>
    /// <para>
    /// <b>AND IT ADDS WITHOUT REMOVING, exactly as repair does.</b> The narrow parent
    /// keeps its counts and its place; if the shorter rule is equally accurate,
    /// <see cref="Subsume"/> is already the mechanism that drops the longer one, and if it
    /// is not, its own accuracy will say so and culling already reads that. Nothing here
    /// decides — it proposes, and the bars that exist judge.
    /// </para>
    /// <para>
    /// <b>A ONE-CODE SCOPE IS LEFT ALONE, because the empty scope fires on everything and
    /// says nothing.</b> A commitment that matches every moment is a base rate wearing a
    /// rule's clothes, and this design has a trap for exactly that shape.
    /// </para>
    /// </remarks>
    public int Widen()
    {
        if (_dials.Widening == Widening.Never) return 0;

        var widened = 0;

        // A COPY, BECAUSE `Add` WRITES TO WHAT THIS WALKS. Every other sweep operator
        // here takes one for the same reason, and a run that mutated mid-walk would be
        // ordering-dependent in a way fork 12 has already been reopened over twice.
        var eligible = All.Where(Widenable).ToList();

        // HOW MANY CLEAN PARENTS HAVE TO AGREE, AND IT IS THE WHOLE DIFFERENCE BETWEEN THE
        // TWO ARMS. One is every drop of every clean rule; two is only the drops that two
        // clean rules reach together, which is a code they DISAGREE about while agreeing on
        // everything else and on what follows.
        var counted = _dials.Widening == Widening.Shared ? Agreement(eligible) : null;

        foreach (var one in eligible)
            foreach (var dropped in one.Scope)
            {
                var proposed = Shortened(one, dropped);

                if (counted is not null
                    && (!counted.TryGetValue(proposed.Identity, out var parents)
                        || parents < 2))
                    continue;

                // THE PLACEMENT GATE AGAIN, AND IT HAS TO BE ASKED HERE TOO. A shorter
                // scope has a different minimum code, so a fleet would otherwise mint the
                // same generalisation on every holder that could see the parent.
                if (Places is not null && !Places(proposed)) continue;

                if (!Add(proposed)) continue;

                Born(proposed, Birth.Widened);
                widened++;
            }

        return widened;
    }

    /// <summary>Whether anything could ever propose a shorter version of this.</summary>
    /// <param name="one">The commitment being considered.</param>
    /// <remarks>
    /// <b>A ONE-CODE SCOPE IS LEFT ALONE, because the empty scope fires on everything and
    /// says nothing</b> — a commitment matching every moment is a base rate wearing a rule's
    /// clothes. The rest is <see cref="Widening.Unmissed"/>'s own condition, written once so
    /// that the tally and the walk cannot come to ask different questions.
    /// </remarks>
    private bool Widenable(Commitment one) =>
        one.Scope.Length >= 2 && one.Seen >= _dials.Floor && one.Misses == 0;

    /// <summary>The same claim with one code taken out.</summary>
    /// <param name="one">What to shorten.</param>
    /// <param name="dropped">The code to leave out.</param>
    private static Commitment Shortened(Commitment one, Code dropped) =>
        new([.. one.Scope.Where(code => code != dropped)], one.Expects);

    /// <summary>How many eligible commitments each shortening would come from.</summary>
    /// <param name="eligible">Everything that could be shortened at all.</param>
    /// <remarks>
    /// <b>KEYED ON THE SHORTENED CLAIM'S IDENTITY, WHICH IS WHY IT NEEDS NO PAIRWISE
    /// WALK.</b> Two clean rules differing in exactly one code and expecting the same thing
    /// reach the same shorter scope, and a scope's identity is a hash of itself — so
    /// agreement falls out of a dictionary over one pass rather than out of comparing every
    /// resident against every other.
    /// </remarks>
    private static Dictionary<Code, int> Agreement(IEnumerable<Commitment> eligible)
    {
        var counted = new Dictionary<Code, int>();

        foreach (var one in eligible)
            foreach (var dropped in one.Scope)
            {
                var name = Shortened(one, dropped).Identity;

                counted[name] = counted.TryGetValue(name, out var already) ? already + 1 : 1;
            }

        return counted;
    }

    /// <summary>Whether a commitment has missed enough times to be worth repairing.</summary>
    /// <remarks>
    /// <b>WRITTEN ONCE AND READ BY TWO CALLERS, WHICH IS THE POINT.</b> The chain that
    /// DECIDES and the pass that COUNTS have to ask the identical question or the census
    /// describes a machine that is not running — the same drift <c>Learned.Grade</c> was
    /// written once to avoid.
    /// </remarks>
    private bool PastFloor(Commitment one) => one.Misses >= _dials.Floor;

    /// <inheritdoc cref="PastFloor"/>
    /// <summary>Whether a commitment has any of its repair budget left.</summary>
    /// <remarks>
    /// <b>THE ALLOWANCE IS A CONSTANT UNDER TWO OF THE THREE RULES AND A FUNCTION OF THE
    /// PARENT UNDER THE THIRD.</b> <see cref="Budgeting.Earned"/> pays one attempt per
    /// <see cref="CommittingSettings.Floor"/> misses, so a parent that stops being wrong
    /// stops earning and one that becomes wrong again is funded — which is what a design
    /// forbidding episode boundaries needs and what a total cannot be.
    /// </remarks>
    private bool PastBudget(Commitment one) => Children(one.Identity) < Allowed(one);

    /// <summary>How many attempts this parent may have made by now.</summary>
    /// <param name="one">The parent.</param>
    /// <inheritdoc cref="Budgeting"/>
    private int Allowed(Commitment one) =>
        _dials.Budgeting == Budgeting.Earned
            ? (int)(one.Misses / _dials.Floor)
            : _dials.Budget;

    /// <inheritdoc cref="PastFloor"/>
    /// <summary>Whether no child already covers this commitment's failure.</summary>
    private bool Uncovered(Commitment one, ImmutableArray<Commitment> firing) =>
        _dials.Mending == Mending.Ungated
        || !firing.Any(other => Beside(other, one) && other.Narrows(one));

    /// <inheritdoc cref="PastFloor"/>
    /// <summary>Whether forking this commitment has ever paid, where that is asked.</summary>
    private bool PastImproving(Commitment one) =>
        _dials.Mending != Mending.Improving || Improves(one);

    /// <summary>Whether one holder can see both of these at once.</summary>
    /// <param name="one">A commitment that might cover the other.</param>
    /// <param name="other">The commitment being considered for repair.</param>
    /// <remarks>
    /// <b>TRUE OF EVERYTHING WHILE <see cref="Placing"/> IS NULL</b>, so the in-process
    /// machine is not paying a comparison for a distribution it does not have.
    /// </remarks>
    private bool Beside(Commitment one, Commitment other) =>
        Placing is null || Placing(one) == Placing(other);

    /// <summary>What a commitment's repair budget has been spent on.</summary>
    /// <param name="name">What the commitment is called.</param>
    /// <inheritdoc cref="Budgeting"/>
    /// <remarks>
    /// <b>ONLY <see cref="Budgeting.Children"/> COUNTS NAMES.</b> <see cref="Budgeting.Earned"/>
    /// limits the same thing <see cref="Budgeting.Attempts"/> does — how often a parent has
    /// tried — and differs in what it is allowed to spend rather than in what spends it.
    /// </remarks>
    private int Children(Code name) =>
        !_minted.TryGetValue(name, out var born) ? 0
        : _dials.Budgeting == Budgeting.Children ? born.Names.Count
        : (int)born.Attempts;

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

        foreach (var name in born.Names)
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
                if (Under(specific, general) && Absorbs(general, specific))
                {
                    doomed.Add(specific);
                    break;
                }

        foreach (var one in doomed) Remove(one, Loss.Subsumed);

        return doomed.Count;
    }

    /// <summary>
    /// Whether one commitment's scope entails another's, categories included.
    /// </summary>
    /// <param name="specific">The commitment that would go.</param>
    /// <param name="general">The commitment that would stay.</param>
    /// <remarks>
    /// <para>
    /// <b>WITHOUT THIS THE REWRITE PROPOSES AND NOTHING EVER JUDGES.</b>
    /// <see cref="Commitment.Narrows"/> is a subset test and wants a strictly longer scope,
    /// so a rule pinning a member and a rule pinning its category — the same length, no code
    /// in common — stand in no relation at all and both are held forever. The compression
    /// fork 85 was after is precisely the specific being taken, so the entailment has to be
    /// readable at the same grain the front end folds at.
    /// </para>
    /// <para>
    /// <b>AND IT IS ONE-DIRECTIONAL BY CONSTRUCTION, WHICH IS WHAT STOPS TWO CLAIMS ABSORBING
    /// EACH OTHER.</b> A member entails its category and a category entails no member, so
    /// nothing that pins <i>this look</i> and nothing that pins <i>that kind</i> can each be
    /// the other's specific. A pair that could would be added to <c>doomed</c> twice and both
    /// removed, which is the claim disappearing rather than being generalised.
    /// </para>
    /// <para>
    /// <b>NULL <see cref="Sorts"/> OR <see cref="Coarsening.Never"/> IS EXACTLY THE OLD
    /// TEST</b>, so every reading taken before categories existed is reproduced by this line
    /// rather than beside it — and a run that folds categories into the moment without
    /// turning this on is the control that says what the FOLD alone was worth.
    /// </para>
    /// </remarks>
    private bool Under(Commitment specific, Commitment general)
    {
        if (specific.Narrows(general)) return true;

        if (Sorts is null
            || _dials.Coarsening == Coarsening.Never
            || specific.Expects != general.Expects
            || specific.Scope.Length < general.Scope.Length)
            return false;

        var coarsely = false;

        foreach (var code in general.Scope)
        {
            if (specific.Scope.Contains(code)) continue;

            var pinned = false;

            foreach (var mine in specific.Scope)
                if (Sorts.Coarser(mine) == code) { pinned = true; break; }

            if (!pinned) return false;

            coarsely = true;
        }

        // AT LEAST ONE CODE MET COARSELY, OR THIS IS THE SUBSET TEST ABOVE ANSWERING TWICE.
        // Equal-length scopes that are subsets are the same scope, so without this a
        // commitment would be its own specific and every resident would be doomed.
        return coarsely;
    }

    /// <summary>
    /// Whether a general commitment takes a narrower one's place.
    /// </summary>
    /// <param name="general">The commitment that would stay.</param>
    /// <param name="specific">The commitment that would go.</param>
    /// <remarks>
    /// <b>UNDER <see cref="Subsuming.Weaker"/> A HAIR OF ADVANTAGE SAVES THE CHILD, AND
    /// THE CLAIM THAT IT ALWAYS HAS ONE IS REFUTED BY <see cref="Lineages"/>.</b> The
    /// argument was that a child fires less often and has stored more of what it fired on,
    /// so <i>equally accurate</i> is a measure-zero event and this path is unreachable —
    /// and the ladder counts it firing on about four repair children in five, at every rung
    /// and on every world measured. A child that has specialised on the wrong code is
    /// exactly as accurate as its parent and is absorbed, which is this working rather than
    /// failing. The other rule asks the child to be SIGNIFICANTLY better against its own
    /// smaller sample, by the two-proportion test the repair gate already uses.
    /// </remarks>
    private bool Absorbs(Commitment general, Commitment specific)
    {
        if (_dials.Subsuming == Subsuming.Weaker)
            return general.Accuracy >= specific.Accuracy;

        var ahead = Repair.Ahead(
            specific.Hits, specific.Fired, general.Hits, general.Fired);

        return Normal.Tail(ahead) > _dials.Alpha;
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
    /// <para>
    /// <b>AND THIS IS THE ONE OPERATOR THAT CANNOT DECIDE LOCALLY, WHICH IS WHY IT TAKES
    /// AN ARGUMENT.</b> Its statistic is the whole population's, so a holder counting only
    /// its own residents goes silent — measured in <c>SplitNamingTests</c>, where three
    /// shards holding thirty-six eligible scopes each name nothing at all. Nothing else in
    /// this class asks for anything from off the machine.
    /// </para>
    /// <para>
    /// <b>AND SHARING OBSERVATIONS INSTEAD WOULD NOT DO, WHICH IS WHAT MAKES THE ARGUMENT
    /// LOAD-BEARING RATHER THAN CONVENIENT.</b> <c>OverlapTests</c> puts two machines on
    /// three quarters of one stream and they agree on names as badly as machines sharing
    /// none — the gate picks one pair by argmax, so a small difference in evidence changes
    /// the winner and everything built on it. Counts are the only thing that converges
    /// them.
    /// </para>
    /// </remarks>
    /// <param name="heard">
    /// What other holders counted, or nothing where this machine is alone.
    /// <b>Theirs and never this one's</b> — these are added to what is counted here, so a
    /// table that already included this holder would weigh it twice.
    /// </param>
    public int Abstract(Recurrence? heard = null)
    {
        // ONE NAME AN ASK, AND THAT CEILING IS LOAD-BEARING RATHER THAN AN OVERSIGHT.
        // Asking until the gate refused was built, measured over four worlds and eight seeds,
        // and deleted: it minted three times the names and held two thirds more rules TRUE of
        // the world while covering FEWER of the rounds the base rate gets wrong. See the
        // plan's revival row -- what the extra names bought was over-specialisation, because
        // a name is a step of two codes past a bar that is paid once.
        var counted = Recurrence.Of(All, _dials);

        if (heard is not null) counted.Absorb(heard);

        // COUNTED ON EVERY PATH OUT, INCLUDING THE ONE THAT SUCCEEDS, so the five refusals
        // and `Spoke` add to `Asked` and a share can be read as a share. A partition that
        // is only counted where it fails is a partition of nothing.
        var reading = Abstracting.Propose(counted, _dials, _names);

        Lately = reading;
        _asked++;

        switch (reading.Refused)
        {
            case Refused.Nothing: _spoke++; break;
            case Refused.Scarce: _atScarce++; break;
            case Refused.Unpaired: _atUnpaired++; break;
            case Refused.Rare: _atRare++; break;
            case Refused.Independent: _atIndependent++; break;
            case Refused.Uncertain: _atUncertain++; break;
        }

        if (reading.Named is not { } shared) return 0;

        _names.Mint(shared);

        var said = 0;
        var name = Naming.Name(shared);

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

            Remove(one, Loss.Renamed);

            Add(shorter);
            Born(shorter, Birth.Renamed);

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

        foreach (var one in doomed) Remove(one, Loss.Culled);

        return doomed.Count;
    }

    /// <summary>Drops a commitment and says which operator dropped it.</summary>
    /// <param name="commitment">What is going.</param>
    /// <param name="loss">Which operator is dropping it.</param>
    /// <remarks>
    /// <b>THE REASON IS A PARAMETER RATHER THAN A GUESS FROM THE CALLER'S NAME</b>, for
    /// the reason <see cref="Birth"/> gives: <see cref="Abstract"/> removes and adds the
    /// same claim, and a ledger that read that as a death would report a lineage dying at
    /// exactly the rung where it was compressed.
    /// </remarks>
    private void Remove(Commitment commitment, Loss loss)
    {
        if (!_byName.Remove(commitment.Identity)) return;

        ref var life = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _lineage, (commitment.Expects, commitment.Scope.Length), out _);

        life = loss switch
        {
            Loss.Subsumed => life with { Subsumed = life.Subsumed + 1 },
            Loss.Culled => life with { Culled = life.Culled + 1 },
            _ => life with { Rewritten = life.Rewritten + 1 },
        };

        foreach (var code in commitment.Scope)
            if (_byCode.TryGetValue(code, out var at))
            {
                at.RemoveAll(one => one.Identity == commitment.Identity);
                if (at.Count == 0) _byCode.Remove(code);
            }

        _minted.Remove(commitment.Identity);

        // AND THE INSTRUMENT'S TABLE WITH IT, OR IT IS A LEAK RATHER THAN A READING. `_runners`
        // holds one entry per child ever born and nothing else would ever drop it -- on a
        // world that mints hundreds of thousands it would outgrow the population it is about.
        // This doc's own row: a cost can be in memory while every instrument watches time.
        _runners.Remove(commitment.Identity);
    }
}
