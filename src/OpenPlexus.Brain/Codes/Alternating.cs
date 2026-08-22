namespace OpenPlexus.Codes;

/// <summary>
/// Codes that are ALTERNATIVES, found in what was seen rather than handed over — <b>fork
/// 97's category, derived.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>John's, and it is two clauses rather than one.</b> His account is a set of things
/// standing in the same relations to everything else; the clause that has to go with it is
/// that they NEVER CO-OCCUR. Without it the same rule groups a wheel and a door — both
/// stand in relations to a car — and that is a co-firing bundle, which is rung five and
/// folds the opposite way.
/// </para>
/// <para>
/// <b>So the test is exclusion and shared company</b>, and both are facts about the moments.
/// Nothing here reads a world's tables, a vocabulary or an outcome. Two codes that have
/// never been seen together and that keep the same company are alternatives, and that is
/// arithmetic over what arrived.
/// </para>
/// <para>
/// <b>And it is why the individual and the category are one mechanism</b> rather than two.
/// The looks a thing wears across sightings never co-occur — a basket at two moments does
/// not co-occur with itself, which is what makes rung five structurally unable to reach it —
/// and they keep the same company by construction. So a thing's appearances ARE a category,
/// and minting one is what collapses a rule per appearance into a rule.
/// </para>
/// <para>
/// <b>What it cannot do, said before anything reads it:</b> it cannot separate two things that
/// are substitutable. Twins wear one look by construction, so every statistic over the
/// moments is the same for both and a proposal covering both is what this returns. That is
/// not a fault to repair here — it is exactly what a PAYING gate is for, and the proposal
/// being wrong is how the gate finds out.
/// </para>
/// </remarks>
/// <summary>
/// What the grouper takes as evidence that two codes do not stand in one another's place.
/// </summary>
/// <remarks>
/// <para>
/// <b>An arm, because the two worlds measured disagree.</b> The clause was written as
/// <see cref="Never"/> — a pair is refused for meeting even once — which is <i>alternatives
/// never co-occur</i> taken literally, and it is right where a moment is one assertion. On
/// bAbI a line is a moment and names never share one.
/// </para>
/// <para>
/// <b>And it is wrong where a moment is a WINDOW.</b> On <c>Worlds.Roaming</c> a moment spans three
/// sentences, so two room words land in one constantly although a person is in one room, and
/// the clause refused 27 of 27 within-set pairs: every grouping returned nought at every bar.
/// </para>
/// <para>
/// <b>So neither is the rule and the grid decides.</b> What would settle it is a clause
/// reading the moment's own shape, which nothing computes — the honest form of that is a
/// world saying how much of an assertion a moment is, and a world reaching into a mechanism
/// is what this repo refuses.
/// </para>
/// </remarks>
public enum Meeting
{
    /// <summary>A pair that has met even once stands in no group. What shipped.</summary>
    Never,

    /// <summary>A pair that meets no more often than chance would have it meet.</summary>
    /// <remarks>
    /// A strict widening of <see cref="Never"/>: a pair that never met scores nought against
    /// a positive expectation and still passes. What it adds is the pairs that DO meet, in a
    /// window, no more often than two unrelated codes would.
    /// </remarks>
    Rarely,
}

/// <summary>
/// Which counts a chance bar is read off — <b>the axis, where <see cref="Meeting"/> is the
/// clause.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>An arm, and the readings are not substitutes.</b> Two of them read what a code keeps
/// company with inside a moment and one reads what it turns up beside across moments, which is
/// the distinction <see cref="Alternating.Over"/> already draws between space and time. A twin
/// wears its sibling's look, so company cannot part the two; a uniform stream adheres at
/// chance everywhere, so time finds nothing a bag of moments would have.
/// </para>
/// <para>
/// <b>What is shared is that none of them takes a level.</b> A bar somebody picked while
/// looking at one world is that world reaching into a mechanism, and this enum says which
/// counts the bar travels over rather than how high it sits.
/// </para>
/// </remarks>
public enum Counting
{
    /// <summary>What a code shares a MOMENT with, against a shuffle of the same counts.</summary>
    Company,

    /// <summary>
    /// The same company, each partner weighed by how SURPRISING it is — <b>and the null does
    /// not converge, which is why it is here.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Company"/> gets stricter without bound as evidence accumulates.</b> A
    /// shuffled vector is a multinomial over the alphabet's marginal, so as a code's company
    /// mass grows it converges ON that marginal — and two codes whose shuffles have both
    /// converged have a cosine of one, which nothing observed can beat. On bAbI's parted
    /// vocabulary the grouping covers twelve codes at five hundred moments and eight at twenty
    /// thousand, falling as the counts improve, where a level at 0.9 holds nineteen throughout.
    /// A test that refuses more the more it is told is broken rather than conservative.
    /// </para>
    /// <para>
    /// <b>Positive pointwise mutual information is what the shuffle was trying to remove.</b>
    /// It divides what was counted by what independence would have made it, so what the
    /// marginal explains is gone before the cosine is taken rather than after — and a shuffled
    /// vector, being the marginal, weighs nothing at all.
    /// </para>
    /// </remarks>
    Weighed,

    /// <summary>What a code turns up BESIDE, against what independence would have made it.</summary>
    Time,
}

public sealed class Alternating
{
    private readonly Dictionary<Code, int> _seen = [];
    private readonly Dictionary<Code, Dictionary<Code, int>> _withs = [];
    private readonly Dictionary<(Code Mine, Code Theirs), int> _near = [];
    private readonly List<IReadOnlySet<Code>> _held = [];
    private readonly int _span;

    private int _next;
    private long _moments;
    private bool _settled;

    /// <param name="span">
    /// How many moments either side count as near, for <see cref="Counting.Time"/>. Fixed at
    /// construction because it decides how far back a moment has to be held, and a window
    /// widened part way through a stream would leave the earlier counts answering a narrower
    /// question than the later ones.
    /// </param>
    /// <remarks>
    /// <para>
    /// The derivation held open, so a brain may read it while it runs. The plan's entry is
    /// that deriving offline orphans every scope holding a category, and a live derivation is
    /// the half of that answer which is not about the store. <see cref="From"/> and
    /// <see cref="Over"/> are this object driven over a whole stream, so there is one
    /// implementation and every batch reading is unchanged by construction.
    /// </para>
    /// <para>
    /// Every count here is monotone. How often a code was seen and how often two turned up
    /// near each other are G-Counters, and what a code shared a moment with is a grow-only
    /// set, so two machines watching different streams merge with no coordinator. That is C1
    /// rather than a convenience, and it is why the accumulator is counts and the grouping is
    /// a pure function of them.
    /// </para>
    /// <para>
    /// What it costs is memory, and the cost is in the company rather than in the pairs. A
    /// code's company is every code it ever shared a moment with, which on a wide alphabet
    /// approaches the alphabet. Nothing here bounds it and no reading has priced it.
    /// </para>
    /// <para>
    /// A moment handed over is kept by reference for as long as the window needs it, so a
    /// caller that mutates a set it has already given up changes counts taken before the edit.
    /// </para>
    /// </remarks>
    public Alternating(int span = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(span);

        _span = span;
    }

    /// <summary>How many moments have been folded in.</summary>
    public long Moments => _moments;

    /// <summary>Folds one moment into the counts.</summary>
    /// <param name="moment">What was seen at once.</param>
    /// <remarks>
    /// The exclusion clause and the company are facts about this moment alone and are taken
    /// here. Adhesion is not: a moment cannot be scored against the moments after it until
    /// they arrive, so it is held until its forward window is complete and folded then. That
    /// lag is the whole difference between reading time online and reading it from a list,
    /// and it is one window deep.
    /// </remarks>
    public void Watch(IReadOnlySet<Code> moment)
    {
        ArgumentNullException.ThrowIfNull(moment);

        if (_settled)
            throw new InvalidOperationException("a settled stream takes no more moments");

        _moments++;

        foreach (var one in moment)
        {
            _seen[one] = _seen.GetValueOrDefault(one) + 1;

            if (!_withs.TryGetValue(one, out var kept)) _withs[one] = kept = [];

            foreach (var other in moment)
                if (!other.Equals(one))
                    kept[other] = kept.GetValueOrDefault(other) + 1;
        }

        _held.Add(moment);

        // A moment is ready once every moment that could be near it has arrived, and it is
        // never revisited after -- so what is held is the window and not the stream.
        while (_held.Count - _next > _span) Fold(_held.Count - 1);

        while (_next > _span)
        {
            _held.RemoveAt(0);
            _next--;
        }
    }

    /// <summary>Says no more moments are coming, so the last of them get their short window.</summary>
    /// <remarks>
    /// The experimenter's and never the learner's, and it exists so a run over a list reaches
    /// the counts a run over a list always did. A brain is never settled, which is what C4
    /// says; what it changes is the last few moments of a stream and nothing else.
    /// </remarks>
    public void Settle()
    {
        if (_settled) return;

        while (_next < _held.Count) Fold(_held.Count - 1);

        _settled = true;
    }

    /// <summary>Folds the next held moment against the window it now has.</summary>
    /// <param name="last">The furthest moment that may count as near, which the tail truncates.</param>
    private void Fold(int last)
    {
        var at = _next++;

        // Counted once a moment an other, not once a neighbour. A code appearing in both
        // moments either side of this one is one piece of evidence about adjacency, and
        // counting it twice would let a long run of one thing manufacture its own
        // significance.
        var window = new HashSet<Code>();

        for (var step = Math.Max(0, at - _span); step <= Math.Min(last, at + _span); step++)
            if (step != at) window.UnionWith(_held[step]);

        foreach (var one in _held[at])
            foreach (var other in window)
                if (!other.Equals(one))
                {
                    _near.TryGetValue((one, other), out var already);
                    _near[(one, other)] = already + 1;
                }
    }

    /// <summary>The groups read off what shares a moment, as <see cref="From"/> reads them.</summary>
    /// <param name="company">
    /// <inheritdoc cref="From" path="/param[@name='company']"/>
    /// </param>
    /// <param name="floor">
    /// <inheritdoc cref="From" path="/param[@name='floor']"/>
    /// </param>
    /// <param name="meeting"><inheritdoc cref="Meeting" path="/summary"/></param>
    public IReadOnlyList<IReadOnlySet<Code>> BySpace(
        double company, int floor, Meeting meeting = Meeting.Never)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(floor);

        return Grouped(
            _seen, floor, _withs,
            (mine, theirs, group) =>
                Shared(_withs[mine].Keys.ToHashSet(), _withs[theirs].Keys.ToHashSet(), group)
                >= company,
            meeting);
    }

    /// <summary>
    /// The groups read off company WEIGHED by how often each partner turned up.
    /// </summary>
    /// <param name="alike">
    /// How alike two codes' company must be, as the cosine of their count vectors. A scale-free
    /// number, so it says the same thing on a corpus and on a generated world where a share of
    /// a set does not.
    /// </param>
    /// <param name="floor">
    /// <inheritdoc cref="From" path="/param[@name='floor']"/>
    /// </param>
    /// <remarks>
    /// <para>
    /// An arm against <see cref="BySpace"/> and never an addition. The two ask the same
    /// question of the same counts and differ in one thing: whether a partner seen once counts
    /// for as much as a partner seen a thousand times. Discarding the counts is what
    /// <see cref="BySpace"/> does, and it is the older and weaker half of the pair.
    /// </para>
    /// <para>
    /// It is here because the repo had already measured this shape and not in this class. The
    /// statistic in <c>RecalledTests</c> that priced a category at five points under the bag
    /// is a cosine over counted company, so the reading that pays was taken on an object
    /// <see cref="BySpace"/> is not. One of the two goes when they are compared.
    /// </para>
    /// <para>
    /// The exclusion clause is untouched and the grouping is the same walk, which is what makes
    /// this a comparison rather than two mechanisms.
    /// </para>
    /// </remarks>
    /// <param name="meeting"><inheritdoc cref="Meeting" path="/summary"/></param>
    public IReadOnlyList<IReadOnlySet<Code>> ByLikeness(
        double alike, int floor, Meeting meeting = Meeting.Never)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(floor);

        return Grouped(
            _seen, floor, _withs,
            (mine, theirs, _) => Likeness(_withs[mine], _withs[theirs]) >= alike,
            meeting);
    }

    /// <summary>How many shuffled streams a pair must beat to be admitted.</summary>
    /// <remarks>
    /// <b>Nineteen, which is a test rather than a threshold.</b> A pair
    /// beating every draw is what <c>p &lt; 0.05</c> looks like when the null is sampled rather
    /// than integrated, so the number says how fine the test is and never how alike two codes
    /// must be. Raising it makes the same test finer; there is no value of it that makes a
    /// world's codes easier to group.
    /// </remarks>
    private const int Draws = 19;

    /// <summary>Where the shuffle's generator comes from, so every machine draws the same.</summary>
    /// <remarks>
    /// <b>Derived by arithmetic rather than handed in</b>, for <c>Winnow</c>'s reason. A
    /// codebook that must be identical on every machine forever cannot rest on a seed somebody
    /// passes, and a generator per code and per draw is what makes the table the same however
    /// the alphabet is walked.
    /// </remarks>
    private const int Salt = 0x5F3A9C;

    /// <summary>
    /// The groups whose company is closer than a shuffled stream would make it.
    /// </summary>
    /// <param name="floor">
    /// <inheritdoc cref="From" path="/param[@name='floor']"/>
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>The bar travels, which is the whole of what this adds.</b>
    /// <see cref="ByLikeness"/> asks whether a cosine clears a level, and the level is a
    /// number somebody picked while looking at one world — 0.9 on bAbI and 0.5 for the set
    /// reading, neither calibrated against the other. A level is a world reaching into a
    /// mechanism by the back door, one seam out from the rule this repo already refuses.
    /// </para>
    /// <para>
    /// <b>So the bar is what the same counts produce shuffled.</b> Each
    /// code keeps how often it was seen and how much company it kept, and its partners are
    /// redrawn from the alphabet's own marginal. Two codes are alternatives where their
    /// observed company is closer than every one of <see cref="Draws"/> such redraws, which
    /// is a number about the stream and about nothing else.
    /// </para>
    /// <para>
    /// <b>And the null is the one that matters.</b> Every code's shuffled company points at
    /// the marginal, so shuffled cosines are HIGH rather than near nought — which is why a
    /// hand-picked bar had to be 0.9 to say anything. What a pair must now beat is exactly
    /// the part of that cosine that mere frequency explains.
    /// </para>
    /// <para>
    /// <b>What it costs is a draw per occurrence per code.</b> A code with a thousand
    /// occurrences and seven partners a moment is seven thousand draws, nineteen times over,
    /// and the derivation is a batch call rather than something a round pays for.
    /// </para>
    /// </remarks>
    /// <param name="counting"><inheritdoc cref="Counting" path="/summary"/></param>
    /// <param name="meeting"><inheritdoc cref="Meeting" path="/summary"/></param>
    public IReadOnlyList<IReadOnlySet<Code>> ByChance(
        Counting counting, int floor, Meeting meeting = Meeting.Never)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(floor);

        return counting switch
        {
            Counting.Company => Alike(floor, meeting, weighed: false),
            Counting.Weighed => Alike(floor, meeting, weighed: true),
            _ => Near(floor, meeting),
        };
    }

    /// <summary>The company reading, where the null is sampled.</summary>
    /// <param name="floor">How often a code must have been seen to be grouped.</param>
    /// <param name="weighed">
    /// Whether each partner is weighed by how surprising it is before the cosine is taken.
    /// <b>The same shuffle either way</b>, so the two arms differ in the statistic and in
    /// nothing else — a shuffle drawn differently would move both the observed side and the
    /// null and leave nobody able to say which.
    /// </param>
    /// <param name="meeting"><inheritdoc cref="Meeting" path="/summary"/></param>
    private IReadOnlyList<IReadOnlySet<Code>> Alike(int floor, Meeting meeting, bool weighed)
    {
        var shuffled = Shuffled(floor);

        if (weighed) return Lifted(shuffled, floor, meeting);

        return Grouped(
            _seen, floor, _withs,
            (mine, theirs, _) =>
            {
                var observed = Likeness(_withs[mine], _withs[theirs]);

                for (var draw = 0; draw < Draws; draw++)
                    if (Likeness(shuffled[mine][draw], shuffled[theirs][draw]) >= observed)
                        return false;

                return true;
            },
            meeting);
    }

    /// <summary>The same shuffle, with every partner weighed before the cosine is taken.</summary>
    /// <param name="shuffled">Each eligible code's company as chance would have written it.</param>
    /// <param name="floor">How often a code must have been seen to be grouped.</param>
    /// <param name="meeting"><inheritdoc cref="Meeting" path="/summary"/></param>
    /// <remarks>
    /// <b>The null is lifted by the same arithmetic as the observation</b>, which is the whole
    /// of what makes this a comparison. Weighing only the observed side would divide out the
    /// marginal on one half of the test and leave it on the other, so a pair would clear the
    /// bar for being weighed rather than for being alike.
    /// </remarks>
    private IReadOnlyList<IReadOnlySet<Code>> Lifted(
        Dictionary<Code, Dictionary<Code, int>[]> shuffled, int floor, Meeting meeting)
    {
        var observed = _withs.Keys.ToDictionary(code => code, Pointwise);

        var drawn = shuffled.ToDictionary(
            one => one.Key,
            one => one.Value
                .Select(table => Weigh(table, _seen.GetValueOrDefault(one.Key)))
                .ToArray());

        return Grouped(
            _seen, floor, _withs,
            (mine, theirs, _) =>
            {
                var alike = Likeness(observed[mine], observed[theirs]);

                for (var draw = 0; draw < Draws; draw++)
                    if (Likeness(drawn[mine][draw], drawn[theirs][draw]) >= alike) return false;

                return true;
            },
            meeting);
    }

    /// <summary>How much of the admission may be chance, before the candidates are counted.</summary>
    /// <remarks>
    /// <b>A twentieth, which is what <see cref="Draws"/> already commits to.</b> Nineteen draws
    /// beaten outright is a one-sided test at 0.05 with the null sampled; this is the same test
    /// with the null integrated, so the two readings sit at one bar rather than at two somebody
    /// would have to reconcile.
    /// </remarks>
    private const double Alpha = 0.05;

    /// <summary>
    /// The groups that turn up beside each other more often than independence explains, once
    /// the pairs the walk considered are paid for.
    /// </summary>
    /// <param name="floor">How often a code must have been seen to be grouped.</param>
    /// <param name="meeting"><inheritdoc cref="Meeting" path="/summary"/></param>
    /// <remarks>
    /// <para>
    /// <b>Adhesion against a bar nobody picked.</b> The reading this replaced asked whether
    /// adhesion cleared a ratio somebody chose — 1.5, on one world — and a ratio corrects for
    /// nothing, so out of every pair in an alphabet one clears it by luck. What is asked here
    /// is whether the excess is larger than the pairs SEARCHED would produce, which is
    /// repair's own correction over a different candidate set.
    /// </para>
    /// <para>
    /// <b>Integrated rather than sampled, unlike <see cref="Alike"/>.</b> A cosine's null has
    /// no closed form and a pair count's does: adjacency under independence is a count with a
    /// known mean and a known spread, so a shuffle would be paying for a draw to find out
    /// something arithmetic already says. What it buys is a derivation cheap enough to run
    /// while a stream is running.
    /// </para>
    /// </remarks>
    private IReadOnlyList<IReadOnlySet<Code>> Near(int floor, Meeting meeting)
    {
        var eligible = _seen.Count(one => one.Value >= floor);
        var candidates = Math.Max(1, eligible * (eligible - 1) / 2);

        var total = (double)_moments;
        var width = (2 * _span) + 1.0;

        return Grouped(
            _seen, floor, _withs,
            (mine, theirs, _) =>
            {
                _near.TryGetValue((mine, theirs), out var beside);

                var expected = _seen[mine] / total * (_seen[theirs] / total) * width * total;

                return expected > 0.0
                    && Commitments.Normal.Tail((beside - expected) / Math.Sqrt(expected))
                        * candidates <= Alpha;
            },
            meeting);
    }

    /// <summary>Each eligible code's company as chance alone would have written it.</summary>
    /// <param name="floor">How often a code must have been seen to be drawn for.</param>
    /// <remarks>
    /// <b>The marginal and the mass are both preserved</b>, which is what makes this a shuffle
    /// rather than a random vector. A code keeps exactly the company mass it had — every
    /// (occurrence, partner) pair it was in — and each partner is redrawn from how often the
    /// alphabet's codes were seen. So what is destroyed is which codes kept company with
    /// which, and nothing else.
    /// </remarks>
    private Dictionary<Code, Dictionary<Code, int>[]> Shuffled(int floor)
    {
        var alphabet = _seen.Keys.Order().ToList();
        var running = new long[alphabet.Count];

        var total = 0L;

        for (var at = 0; at < alphabet.Count; at++)
        {
            total += _seen[alphabet[at]];
            running[at] = total;
        }

        var made = new Dictionary<Code, Dictionary<Code, int>[]>();

        foreach (var one in _seen.Where(one => one.Value >= floor).Select(one => one.Key))
        {
            var mass = _withs.TryGetValue(one, out var kept) ? kept.Values.Sum() : 0;
            var drawn = new Dictionary<Code, int>[Draws];

            for (var draw = 0; draw < Draws; draw++)
            {
                // A generator per code and per draw, so the table is the same whatever order
                // the alphabet is walked in and whatever else was drawn first.
                //
                // `Hashing` and never `HashCode.Combine`, which is seeded once per PROCESS.
                // The first version used it and two runs of one seed derived different
                // categories -- so the vocabulary was not a function of the stream, and two
                // machines watching the same moments would have disagreed about what a
                // category IS. That is the rule `Naming.Name` and `Joined.Category` already
                // stand on, arriving here by the door it always uses.
                var random = new Random((int)(Hashing.Mix(
                    Hashing.Fold(
                        Hashing.Fold(
                            Hashing.Fold(Hashing.Fold(Hashing.Basis, Salt), one.Modality),
                            one.Value),
                        (ulong)draw))
                    & 0x7FFFFFFF));

                var company = new Dictionary<Code, int>();

                for (var at = 0; at < mass; at++)
                {
                    Code other;

                    // A code is never its own company, here or in what was observed.
                    do
                    {
                        other = alphabet[Landed(running, random.NextInt64(total))];
                    }
                    while (other.Equals(one) && alphabet.Count > 1);

                    company[other] = company.GetValueOrDefault(other) + 1;
                }

                drawn[draw] = company;
            }

            made[one] = drawn;
        }

        return made;
    }

    /// <summary>Which code a draw against the running totals landed on.</summary>
    /// <param name="running">The cumulative counts, in the alphabet's own order.</param>
    /// <param name="drawn">A number under the total.</param>
    private static int Landed(long[] running, long drawn)
    {
        var at = Array.BinarySearch(running, drawn);

        return at >= 0 ? Math.Min(at + 1, running.Length - 1) : ~at;
    }

    /// <summary>
    /// Whether two codes meet no more often than independence would have them meet.
    /// </summary>
    /// <param name="mine">One code.</param>
    /// <param name="theirs">The other.</param>
    /// <remarks>
    /// <para>
    /// <b>What the admission clause used to say as an absolute.</b> It refused a pair that
    /// had co-occurred even ONCE, which is <i>alternatives never co-occur</i> taken
    /// literally — and a moment is a WINDOW rather than an assertion. On <c>Worlds.Roaming</c> a
    /// moment spans three sentences, so two room words land in one constantly although a
    /// person is in one room, and the clause refused 27 of 27 within-set pairs.
    /// </para>
    /// <para>
    /// <b>So it asks about the RATE instead</b>, which is the same question the naming gate's
    /// z asks: a pair meets as often as chance would have it, or less. It is a strict
    /// widening of what was there — a pair that never met scores nought against a positive
    /// expectation and still passes — so nothing the old clause admitted is refused now.
    /// </para>
    /// </remarks>
    /// <param name="meeting"><inheritdoc cref="Meeting" path="/summary"/></param>
    private bool Apart(Code mine, Code theirs, Meeting meeting)
    {
        var together = _withs.TryGetValue(mine, out var kept)
            ? kept.GetValueOrDefault(theirs)
            : 0;

        if (meeting == Meeting.Never) return together == 0;

        // Multiplied out rather than divided, so a code seen nought times cannot divide by
        // it and the comparison stays in whole numbers where it can.
        return together * (double)_moments
            <= _seen.GetValueOrDefault(mine) * (double)_seen.GetValueOrDefault(theirs);
    }

    /// <summary>
    /// Groups codes whose company is alike once each partner is weighed by how surprising it
    /// is — <b>the arm the other two are missing.</b>
    /// </summary>
    /// <param name="alike">How alike two profiles must be.</param>
    /// <param name="floor">How many moments a code must appear in to be grouped at all.</param>
    /// <remarks>
    /// <para>
    /// <see cref="BySpace"/> discards the counts and <see cref="ByLikeness"/> keeps them raw.
    /// Raw counts are dominated by how often a code appears, so a common partner counts for
    /// as much as a rare one and every profile looks alike to every other — measured on
    /// <c>Worlds.Roaming</c> at 0.993 within the room words against 0.965 across the sets, which is
    /// no separation at all.
    /// </para>
    /// <para>
    /// <b>Positive pointwise mutual information is the correction</b>, and it is the same
    /// shape this repo already uses twice: divide what was counted by what independence would
    /// have made it. The same counts then read 0.983 within against 0.302 across.
    /// </para>
    /// </remarks>
    /// <param name="meeting"><inheritdoc cref="Meeting" path="/summary"/></param>
    public IReadOnlyList<IReadOnlySet<Code>> ByCompany(
        double alike, int floor, Meeting meeting = Meeting.Never)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(floor);

        var lifted = _withs.Keys.ToDictionary(code => code, Pointwise);

        return Grouped(
            _seen, floor, _withs,
            (mine, theirs, _) => Likeness(lifted[mine], lifted[theirs]) >= alike,
            meeting);
    }

    /// <summary>One code's company, each partner weighed by how surprising it is.</summary>
    /// <param name="code">Whose company to weigh.</param>
    /// <remarks>
    /// <b>Positive only, which is what the P in PPMI is.</b> A partner rarer than
    /// independence says the two avoid each other, and a vector of avoidances is a different
    /// claim from a vector of what they share.
    /// </remarks>
    private Dictionary<Code, double> Pointwise(Code code) =>
        Weigh(_withs.GetValueOrDefault(code) ?? [], _seen.GetValueOrDefault(code));

    /// <summary>One company table, each partner weighed by how surprising it is.</summary>
    /// <param name="kept">What turned up with it, and how often.</param>
    /// <param name="mine">How often the code itself was seen.</param>
    /// <remarks>
    /// <b>Taking the table rather than the code</b>, so a SHUFFLED table is weighed by the same
    /// arithmetic as an observed one. The marginals stay the real ones either way, because a
    /// shuffle redraws which codes kept company and never how often any of them was seen.
    /// </remarks>
    private Dictionary<Code, double> Weigh(IReadOnlyDictionary<Code, int> kept, int mine)
    {
        var lifted = new Dictionary<Code, double>();

        if (mine == 0) return lifted;

        foreach (var (mate, together) in kept)
        {
            var theirs = _seen.GetValueOrDefault(mate);

            if (theirs == 0) continue;

            var value = Math.Log(together * (double)_moments / (mine * (double)theirs));

            if (value > 0.0) lifted[mate] = value;
        }

        return lifted;
    }

    /// <summary>The cosine of two codes' company, however each partner is weighed.</summary>
    private static double Likeness(
        IReadOnlyDictionary<Code, double> mine, IReadOnlyDictionary<Code, double> theirs)
    {
        var dot = 0.0;

        foreach (var (code, weight) in mine)
            if (theirs.TryGetValue(code, out var had)) dot += weight * had;

        var left = Math.Sqrt(mine.Values.Sum(weight => weight * weight));
        var right = Math.Sqrt(theirs.Values.Sum(weight => weight * weight));

        return left == 0.0 || right == 0.0 ? 0.0 : dot / (left * right);
    }

    /// <summary>The cosine of two codes' company, counted.</summary>
    /// <param name="mine">One code's company and how often each partner turned up.</param>
    /// <param name="theirs">The other's.</param>
    /// <remarks>
    /// Group members are not excluded here, where <see cref="Shared"/> excludes them. A cosine
    /// is dominated by the partners that turn up most and alternatives are by construction
    /// absent from each other's company, so the dimensions they would occupy are nought on
    /// both sides and contribute nothing either way.
    /// </remarks>
    private static double Likeness(
        IReadOnlyDictionary<Code, int> mine, IReadOnlyDictionary<Code, int> theirs)
    {
        var dot = 0.0;

        foreach (var (code, count) in mine)
            if (theirs.TryGetValue(code, out var had)) dot += count * (double)had;

        var left = Math.Sqrt(mine.Values.Sum(count => count * (double)count));
        var right = Math.Sqrt(theirs.Values.Sum(count => count * (double)count));

        return left == 0.0 || right == 0.0 ? 0.0 : dot / (left * right);
    }

    /// <summary>
    /// The groups of alternatives in a stream of moments.
    /// </summary>
    /// <param name="moments">What was seen, each moment a set of codes.</param>
    /// <param name="company">
    /// How much of a code's company must be shared before two codes count as keeping the
    /// same. <b>A dial, and it is the front end's rather than the brain's</b> — what counts
    /// as the same company is a fact about how finely a stream is being read, which is the
    /// same standing <see cref="Winnow"/>'s resolution carries.
    /// </param>
    /// <param name="floor">
    /// How many times a code must have been seen before it may join a group. <b>Because a
    /// code seen</b> once has never co-occurred with anything and keeps whatever company it
    /// arrived in, so it clears both clauses trivially and would group with everything.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>Greedy, and its order is the codes' own rather than the stream's.</b> Two machines
    /// seeing the same moments in different orders must reach the same groups or a category
    /// means one thing here and another there — the rule <c>Hashing</c> stands on, and the
    /// reason a fitted quantiser is refused.
    /// </para>
    /// <para>
    /// <b>A member must clear both clauses against every member already in</b>, never against
    /// the first alone. A chain of pairwise-similar codes reaches arbitrarily far, which
    /// is single-link clustering's own failure and would return one group holding the whole
    /// alphabet.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<IReadOnlySet<Code>> From(
        IEnumerable<IReadOnlySet<Code>> moments, double company, int floor)
    {
        ArgumentNullException.ThrowIfNull(moments);

        var watching = new Alternating();

        foreach (var moment in moments) watching.Watch(moment);

        return watching.BySpace(company, floor);
    }

    /// <summary>
    /// The same question asked of a stream that has an ORDER — <b>fork 106, John's</b>, and the
    /// one clause a bag of moments cannot carry.
    /// </summary>
    /// <param name="moments">What was seen, in the order it was seen.</param>
    /// <param name="floor">
    /// <inheritdoc cref="From" path="/param[@name='floor']"/>
    /// </param>
    /// <param name="span">
    /// How many moments either side count as near. <b>The window is what makes time
    /// readable at all</b>, and one is the smallest thing that is not the moment itself.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>Exclusion stays in the moment and company moves to the window</b>, which is the whole
    /// construction. Widening the exclusion would refuse exactly the codes this is for —
    /// a thing seen twice running shows two of its own looks in adjacent moments, so they
    /// co-occur in any window and would fail the clause that makes them alternatives.
    /// </para>
    /// <para>
    /// <b>And the test is adhesion rather than shared company</b>, because shared company is
    /// exactly what twins have. Two twins wear one look, so their landmarks keep the same
    /// company however wide the window is — that is <see cref="From"/>'s measured limit. What
    /// runs give is that a thing's OWN codes turn up beside each other far more often than
    /// chance and a twin's never do, which is a statement about a pair rather than about the
    /// company either keeps.
    /// </para>
    /// <para>
    /// <b>So a uniform stream must return nothing</b>, and that is the control rather than a
    /// failure. Where sightings are drawn independently, every pair adheres at chance, so
    /// this refuses everything <see cref="From"/> would have found. The two are not
    /// substitutes: one reads space and one reads time.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<IReadOnlySet<Code>> Over(
        IEnumerable<IReadOnlySet<Code>> moments, int floor, int span)
    {
        ArgumentNullException.ThrowIfNull(moments);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(span);

        var watching = new Alternating(span);

        foreach (var moment in moments) watching.Watch(moment);

        // The list has ended, so the last moments get the short window a list always gave
        // them. A brain never reaches this call, which is the one difference between the two
        // ways of driving the same counts.
        watching.Settle();

        return watching.ByChance(Counting.Time, floor);
    }

    /// <summary>The greedy grouping both derivations share.</summary>
    /// <param name="seen">How often each code turned up.</param>
    /// <param name="floor">How often before a code may join a group.</param>
    /// <param name="withs">What each code shared a MOMENT with — the exclusion clause.</param>
    /// <param name="keeps">Whether two codes keep close enough company to be alternatives.</param>
    /// <remarks>
    /// <para>
    /// <b>Greedy, and its order is the codes' own rather than the stream's.</b> Two machines
    /// seeing the same moments in different orders must reach the same groups or a category
    /// means one thing here and another there — the rule <c>Hashing</c> stands on, and the
    /// reason a fitted quantiser is refused.
    /// </para>
    /// <para>
    /// <b>A member must clear both clauses against every member already in</b>, never against
    /// the first alone. A chain of pairwise-similar codes reaches arbitrarily far, which
    /// is single-link clustering's own failure and would return one group holding the whole
    /// alphabet.
    /// </para>
    /// <para>
    /// <b>And the exclusion clause is here rather than in either caller.</b> Because it is the
    /// half of John's account that is not negotiable. What the two derivations differ in
    /// is what counts as keeping the same company; that alternatives never co-occur is the
    /// same claim in both.
    /// </para>
    /// </remarks>
    /// <param name="meeting"><inheritdoc cref="Meeting" path="/summary"/></param>
    private IReadOnlyList<IReadOnlySet<Code>> Grouped(
        Dictionary<Code, int> seen,
        int floor,
        Dictionary<Code, Dictionary<Code, int>> withs,
        Func<Code, Code, IReadOnlySet<Code>, bool> keeps,
        Meeting meeting)
    {
        var codes = seen.Where(one => one.Value >= floor).Select(one => one.Key).Order().ToList();

        var taken = new HashSet<Code>();
        var groups = new List<IReadOnlySet<Code>>();

        foreach (var one in codes)
        {
            if (taken.Contains(one)) continue;

            var group = new HashSet<Code> { one };

            foreach (var other in codes)
            {
                if (taken.Contains(other) || group.Contains(other)) continue;

                if (group.All(member =>
                    Apart(member, other, meeting) && keeps(member, other, group)))
                    group.Add(other);
            }

            if (group.Count < 2) continue;

            foreach (var member in group) taken.Add(member);

            groups.Add(group);
        }

        return groups;
    }

    /// <summary>How much of two codes' company is the same.</summary>
    /// <param name="mine">One code's company.</param>
    /// <param name="theirs">The other's.</param>
    /// <param name="group">
    /// What is already in the group. <b>Excluded from both sides</b>, because members are
    /// alternatives and therefore never each other's company — counting their absence as a
    /// difference would penalise exactly the codes that belong together.
    /// </param>
    private static double Shared(
        IReadOnlySet<Code> mine, IReadOnlySet<Code> theirs, IReadOnlySet<Code> group)
    {
        var both = 0;
        var either = 0;

        foreach (var one in mine)
        {
            if (group.Contains(one)) continue;

            either++;
            if (theirs.Contains(one)) both++;
        }

        foreach (var one in theirs)
            if (!group.Contains(one) && !mine.Contains(one)) either++;

        return either == 0 ? 0.0 : both / (double)either;
    }
}

/// <summary>
/// Another front end, deriving its own vocabulary of alternatives from what it emits.
/// </summary>
/// <typeparam name="TObservation">What the world hands over.</typeparam>
/// <remarks>
/// <para>
/// <b>The half of the answer the store could not give.</b> A vocabulary derived once over a
/// stream and handed in is an experimenter's, and the plan's entry against it is that
/// re-deriving orphans every scope written over a category. This derives while the stream
/// runs and puts what it finds into a <see cref="Categories"/> that only ever grows, so
/// nothing is ever renamed and no scope is ever orphaned.
/// </para>
/// <para>
/// <b>It derives and never folds</b>, which is why it is a second decorator rather than a
/// setting on <see cref="Sorted{TObservation}"/>. Deriving a vocabulary and reading one are
/// two axes, and a grid crossing them is what says which of the two a cell is measuring —
/// a handed vocabulary that is also folded would move both at once.
/// </para>
/// <para>
/// <b>So the two compose over one store</b>, the derivation underneath and the fold above:
/// <c>new Sorted(new Deriving(inner, held, ...), held)</c>. The same object both times, since
/// a category the front end folds and one the brain may rewrite over have to be the same code.
/// </para>
/// <para>
/// <b>The codes it watches are the inner ones</b> and never the folded ones. A category
/// entering the moment would then be company for its own members and evidence for the next
/// derivation, which is a name reached by inference being written back as a partner — the
/// arrangement that broke two controls on <c>Worlds.Rhythm</c>.
/// </para>
/// </remarks>
/// <param name="inner">The front end this wraps.</param>
/// <param name="held">The vocabulary to fill, which is the one the fold and the brain read.</param>
/// <param name="counting">
/// <inheritdoc cref="Counting" path="/summary"/>
/// </param>
/// <param name="meeting"><inheritdoc cref="Meeting" path="/summary"/></param>
/// <param name="floor">
/// <inheritdoc cref="Alternating.From" path="/param[@name='floor']"/>
/// </param>
/// <param name="every">
/// How many observations between derivations. <b>A cost rather than a bar</b> — the grouping
/// is a pure function of the counts, so this decides how soon a group is noticed and never
/// which groups exist. It is stated because a periodic sweep whose cadence is implicit runs
/// at whatever rate its condition does, which is a trap this repo has already paid for.
/// </param>
/// <param name="span">
/// <inheritdoc cref="Alternating(int)" path="/param[@name='span']"/>
/// </param>
public sealed class Deriving<TObservation>(
    IQuantizer<TObservation> inner,
    Categories held,
    Counting counting,
    Meeting meeting,
    int floor,
    int every,
    int span = 1)
    : IQuantizer<TObservation>
{
    private readonly Alternating _watching = new(span);

    private long _seen;

    /// <summary>How many groups have been learnt.</summary>
    public int Learnt => held.Count;

    /// <inheritdoc/>
    public byte Modality => inner.Modality;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(TObservation observation)
    {
        var codes = inner.Codify(observation);

        _watching.Watch(new HashSet<Code>(codes));

        // Counted here rather than inside the derivation, so the cadence is a rate over
        // observations and not over whatever else might have been true.
        if (++_seen % every == 0)
            foreach (var group in _watching.ByChance(counting, floor, meeting))
                held.Learn(group);

        return codes;
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<Code, int>? Bind(TObservation observation) => inner.Bind(observation);

    /// <inheritdoc/>
    public IReadOnlyDictionary<Code, int>? Order(TObservation observation) => inner.Order(observation);

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Fleeting(TObservation observation) => inner.Fleeting(observation);

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Forced(TObservation observation) => inner.Forced(observation);
}

/// <summary>
/// Another front end, with a code added for every category any of whose members is in the
/// moment — <b>the ANY fold, which is what makes a category not a name.</b>
/// </summary>
/// <typeparam name="TObservation">What the world hands over.</typeparam>
/// <remarks>
/// <b>A decorator</b>, so the categories are an axis on every world rather than a feature of
/// one. <see cref="Joined"/> carries its own because it was built before this existed
/// and its arms cross with the categories; anything reaching a world that is already coded
/// needs the fold without the text machinery around it.
/// </remarks>
/// <param name="inner">The front end this wraps.</param>
/// <param name="categories">
/// The vocabulary of alternatives. <b>The SAME object the population is handed</b>, since a
/// category the front end folds and one the brain may rewrite over have to be the same code
/// or the rewrite names something no moment ever holds.
/// </param>
public sealed class Sorted<TObservation>(IQuantizer<TObservation> inner, Categories categories)
    : IQuantizer<TObservation>
{
    /// <inheritdoc/>
    public byte Modality => inner.Modality;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(TObservation observation) =>
        categories.Folded(new HashSet<Code>(inner.Codify(observation)));

    /// <inheritdoc/>
    public IReadOnlyDictionary<Code, int>? Bind(TObservation observation) => inner.Bind(observation);

    /// <inheritdoc/>
    public IReadOnlyDictionary<Code, int>? Order(TObservation observation) => inner.Order(observation);

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Fleeting(TObservation observation) => inner.Fleeting(observation);

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Forced(TObservation observation) => inner.Forced(observation);
}
