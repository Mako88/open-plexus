using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Learning;

/// <summary>
/// A code minted for a set that keeps arriving together — <b>step 3, and the
/// thing that lets the alphabet GROW.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>FORK 21 MINTS EDGES; THIS MINTS NODES.</b> Reflection writes a conclusion
/// back as an observation, so a route walked often enough becomes a direct edge —
/// but the alphabet it walks over is whatever the quantiser fixed at the start and
/// can never change. A recurring set has no name, so it is re-described from its
/// parts every single time it arrives.
/// </para>
/// <para>
/// <b>THE COST IS THE CLAIM, NOT THE ACCURACY.</b> A graph with no chunking
/// already completes a familiar set perfectly: the codes co-occur, so the counts
/// are exactly what they should be. What it cannot do is stop PAYING for it. A set
/// of size S written pairwise is S(S-1) directed entries and a fan-out of S-1 from
/// every member; one node standing for the set is S entries and a fan-out of one.
/// <see cref="Worlds.Motif.Compressed"/> is that target computed rather than
/// measured.
/// </para>
/// <para>
/// <b>THE CODE IS DERIVED FROM THE MEMBERS AND NEVER ASSIGNED.</b> A counter would
/// give two machines different codes for the same set, and the whole design rests
/// on a code meaning the same thing on every machine forever — the same property
/// that forbids a fitted codebook in step 8, and the same trick
/// <see cref="Bus.Ring"/> uses to agree on placement with nobody to ask. Hashing
/// the sorted members means two machines that independently notice the same set
/// mint the same code without exchanging a word, which is the only kind of minting
/// C1 permits.
/// </para>
/// <para>
/// <b>THE THRESHOLD IS DERIVED TOO — MINIMUM DESCRIPTION LENGTH, NOT A
/// CONSTANT.</b> Describing <c>n</c> occurrences of an S-code set costs
/// <c>n·S·log₂A</c> bits; naming it costs <c>S·log₂A</c> once to define plus
/// <c>n·log₂A</c> to use. Naming wins when <c>n(S-1) &gt; S</c>. <b>Nothing here
/// was chosen</b>, which is the point: a constant nobody set doing the cutting is
/// a refuted row already.
/// <para>
/// <b>THE CANDIDATE IS A PAIR, SO THE FIRST NAME ALWAYS COSTS THREE ARRIVALS</b> —
/// <c>n &gt; 2</c> at <c>S = 2</c> — however large the moment is. What reaches the
/// larger sets is COMPOSITION and not a lower bar: a merged candidate of three
/// mints when <c>n(3-1) &gt; 3</c>, which is its second arrival. The inequality is
/// untouched; what changed is that it now weighs a PART of the moment.
/// </para>
/// </para>
/// <para>
/// <b>AND A CHUNK MAY NOT COVER THE WHOLE MOMENT — the rule this class was
/// missing, and it is one rule closing two defects.</b> Substitution makes the
/// name the onset and the members merely live, and an occasion pairs onsets with
/// everything while never pairing live with live. That IS the compression: the
/// members stop pairing with each other. But when the name covers everything
/// present there is nothing left for it to be in relation WITH, so the only
/// entries written are name-to-member — which the definition of the name already
/// records. <b>A name standing for the entire moment is not a compression of that
/// moment; it is a deletion of it.</b>
/// <list type="bullet">
/// <item><b><see cref="Worlds.Senses"/> fell 0.8621 to 0.4138</b> the day
/// substitution became unconditional. A moment there is two codes, so every chunk
/// was the whole moment, and the sight–sound edge it destroyed is the entire
/// task — reaching touch from sight runs through sound and nowhere else. Under
/// this rule a two-code moment can never be chunked at all.</item>
/// <item><b><see cref="Graph.Accumulate.Agreement"/> read EXACTLY equal to
/// <see cref="Graph.Accumulate.Sum"/>.</b> A conjunction is several distinct
/// origins agreeing and one name in place of a moment is one origin. A chunk
/// leaving at least one other thing standing leaves a conjunction something to
/// be a conjunction OF.</item>
/// </list>
/// </para>
/// <para>
/// <b>SO THE CANDIDATE IS A PAIR AND NOT THE MOMENT, WHICH IS ALSO WHAT MAKES
/// SUB-MOMENT STRUCTURE REACHABLE AT ALL.</b> Enumerating the subsets of a moment
/// is exponential and no threshold rescues that, which is why this only ever
/// considered the whole of one. Pair-merging is the tractable road the literature
/// already took — Sequitur and BPE both — and it COMPOSES: once a pair has a name
/// the name is itself a candidate, so <c>{A,B}</c> becomes <c>AB</c> and then
/// <c>{AB,C}</c> becomes <c>ABC</c>, and a set of any size is reachable through
/// quadratic work per moment rather than exponential. <b>This finds parts that
/// keep happening together and not merely things that keep happening
/// identically</b>, which is the half that was missing and was written up plainly
/// as missing.
/// </para>
/// <para>
/// <b>A CHUNK IS NAMED BY ITS ORIGINAL MEMBERS AND NEVER BY THE PATH THAT REACHED
/// IT.</b> Two machines see different data, so they merge in different orders —
/// one reaches <c>{A,B,C}</c> as <c>AB</c> then <c>+C</c>, the other as
/// <c>BC</c> then <c>+A</c>. Naming a merge by its two halves would give those two
/// different codes for one set and the red-ball property would be gone with
/// nothing failing. Hashing the sorted ORIGINALS makes the name a pure function of
/// what is covered, so every path to a set arrives at the same code.
/// </para>
/// </remarks>
public sealed class Chunk
{
    /// <summary>
    /// The modality every minted code carries.
    /// </summary>
    /// <remarks>
    /// <b>Its own modality, so a chunk is never mistaken for a thing that was
    /// sensed.</b> A walk can be narrowed to what a front end produced, which is
    /// what <see cref="Thinking.Thought.BestOf"/> is for — and a minted code
    /// answering a question about what was SEEN would be a completion nobody could
    /// act on.
    /// </remarks>
    public const byte Minted = 200;

    /// <summary>How many times each candidate set has been seen together.</summary>
    private readonly Dictionary<ulong, int> _seen = [];

    /// <summary>What each candidate covers, in original codes.</summary>
    private readonly Dictionary<ulong, ImmutableArray<Code>> _members = [];

    /// <summary>Sets that have paid for their own name.</summary>
    private readonly HashSet<ulong> _minted = [];

    /// <summary>How often each name has actually stood in for something.</summary>
    /// <remarks>
    /// <b>A MINTED NAME THAT IS NEVER FOLDED HAS NEVER ENTERED THE GRAPH.</b> It
    /// costs no row entry and joins no occasion — it is a candidate that passed a
    /// threshold and then lost every merge it was offered for, and counting it as
    /// alphabet overstates what the detector did.
    /// <para>
    /// <b>AND THE UTILITY PROBLEM IS THE SAME INEQUALITY COUNTED ON USES</b> —
    /// Minton and SOAR, and it needs no new constant. Minting asks whether a set
    /// is seen often enough to be worth a symbol; KEEPING it asks whether the
    /// symbol was then USED often enough to repay the definition, which is
    /// <c>u(S-1) &gt; S</c> over uses rather than over sightings. A name applied
    /// once is a row entry earning its keep on nothing.
    /// </para>
    /// </remarks>
    private readonly Dictionary<ulong, int> _used = [];

    /// <summary>How often each thing has been in hand when pairs were counted.</summary>
    /// <remarks>
    /// <b>THE MARGINALS, AND WITHOUT THEM THE THRESHOLD CANNOT SEE CHANCE.</b>
    /// See <see cref="Count"/>.
    /// </remarks>
    private readonly Dictionary<Code, int> _occurs = [];

    /// <summary>How many times pairs have been counted at all.</summary>
    private long _rounds;

    private readonly Lock _gate = new();

    /// <summary>How many sets have been minted.</summary>
    public int Coined
    {
        get { lock (_gate) return _minted.Count; }
    }

    /// <summary>How many minted names have ever stood in for anything.</summary>
    /// <remarks>
    /// <b>THE ALPHABET THAT ACTUALLY GREW</b>, as against the one that passed a
    /// threshold. See <see cref="_used"/>.
    /// </remarks>
    public int Applied
    {
        get
        {
            lock (_gate)
                return _used.Count(one =>
                    (long)one.Value * (_members[one.Key].Length - 1) > _members[one.Key].Length);
        }
    }

    /// <summary>How many distinct candidates have been noticed at all.</summary>
    /// <remarks>
    /// <b>THE DENOMINATOR THAT SAYS WHETHER MINTING WAS SELECTIVE.</b> A detector
    /// that mints nearly everything it sees has found no structure — it has just
    /// renamed the stream, and the compression is arithmetic rather than real.
    /// <para>
    /// <b>IT COUNTS PAIRS NOW AND NOT MOMENTS, so it is a much larger number and
    /// against a much larger denominator.</b> A moment of S codes offers S(S-1)/2
    /// candidates rather than one, and the ratio to <see cref="Coined"/> is what
    /// stays comparable.
    /// </para>
    /// </remarks>
    public int Noticed
    {
        get { lock (_gate) return _seen.Count; }
    }

    /// <summary>
    /// What a moment became once its named parts were folded up.
    /// </summary>
    /// <param name="Codes">
    /// The moment after substitution — <b>originals that were not absorbed, plus
    /// a name for each part that was.</b>
    /// </param>
    /// <param name="Names">
    /// Each name that appeared, against the original codes it covers.
    /// <b>Empty when nothing was folded</b>, which is the common case and the one
    /// a caller should be able to test cheaply.
    /// </param>
    public readonly record struct Substitution(
        ImmutableArray<Code> Codes,
        ImmutableDictionary<Code, ImmutableArray<Code>> Names)
    {
        /// <summary>A moment that kept every code it arrived with.</summary>
        public static Substitution Of(IReadOnlyCollection<Code> codes) =>
            new([.. codes], ImmutableDictionary<Code, ImmutableArray<Code>>.Empty);

        /// <summary>Whether anything was folded at all.</summary>
        public bool Folded => !Names.IsEmpty;

        /// <summary>Every original code that is now inside a name.</summary>
        public IEnumerable<Code> Absorbed => Names.Values.SelectMany(one => one);
    }

    /// <summary>
    /// Takes one moment and folds up whatever part of it has earned a name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Counting happens on every call and minting only crosses the threshold
    /// once</b>, so a set that keeps arriving keeps returning the same code — the
    /// code is a function of the members and nothing about the count reaches it.
    /// </para>
    /// <para>
    /// <b>THE MERGE LOOP COUNTS THE REDUCED SET AS WELL AS THE ORIGINAL ONE, and
    /// that is what makes chunks above a pair reachable.</b> Once <c>{A,B}</c>
    /// folds, the next pass sees <c>{AB,C}</c> and counts THAT pair, so a set of
    /// three earns a name by the same inequality that gave the pair one.
    /// </para>
    /// <para>
    /// <b>A pair is counted even where it cannot be used.</b> A two-code moment can
    /// never be folded — the name would cover all of it — but the pair is evidence
    /// wherever it appears, and refusing to count it here would make a candidate's
    /// count depend on which moments happened to be small.
    /// </para>
    /// </remarks>
    /// <param name="moment">Everything present, onsets and what was already live.</param>
    /// <param name="onsets">
    /// Which of them started now. <b>The relations among THESE are what a fold
    /// destroys and nothing recreates</b> — see the guard on <see cref="Fold"/>.
    /// </param>
    /// <param name="groups">
    /// Which codes the front end said were one thing, or null where it cannot say.
    /// <b>A CHUNK MAY NOT SPAN TWO OF THEM</b> — see <see cref="Fold"/>.
    /// </param>
    public Substitution Notice(
        IReadOnlyCollection<Code> moment,
        IReadOnlySet<Code> onsets,
        IReadOnlyDictionary<Code, int>? groups = null)
    {
        ArgumentNullException.ThrowIfNull(moment);
        ArgumentNullException.ThrowIfNull(onsets);

        if (moment.Count < 2) return Substitution.Of(moment);

        lock (_gate) return Fold(moment, onsets, groups, counting: true);
    }

    /// <summary>
    /// Folds a set with what is already named, <b>without counting the look as an
    /// arrival.</b>
    /// </summary>
    /// <remarks>
    /// <b>For asking a question about a set rather than observing one.</b> A
    /// question is not evidence that the set occurred, and letting it count would
    /// mean the act of asking could mint the very chunk being asked about.
    /// </remarks>
    public Substitution Named(IReadOnlyCollection<Code> codes)
    {
        ArgumentNullException.ThrowIfNull(codes);

        if (codes.Count < 2) return Substitution.Of(codes);

        // A QUESTION HAS NO ONSETS, so every code in it counts as one. Asking
        // about a set is asking about the whole of it, and a name swallowing all
        // of it would answer with a code standing for the question.
        lock (_gate) return Fold(codes, codes.ToHashSet(), null, counting: false);
    }

    /// <summary>
    /// The merge loop — <b>count the pairs, fold the best named one, repeat.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE GUARD IS ABOUT THE ONSETS AND NOT ABOUT THE MOMENT, and getting
    /// that wrong cost most of the fix.</b> Refusing only to cover the whole
    /// moment took <see cref="Worlds.Senses"/> from 0.4138 back to 0.6103 and no
    /// further, against a pre-chunking 0.8077 — because a moment is the onsets
    /// PLUS whatever was still live, so a two-onset moment with one code carried
    /// over reads as three things, and a fold could swallow both onsets while
    /// leaving the carried code standing. The count guard was satisfied; the
    /// sight–sound edge was still destroyed.
    /// </para>
    /// <para>
    /// <b>WHAT A FOLD DESTROYS IS EXACTLY THE RELATIONS AMONG WHAT IT ABSORBS.</b>
    /// An occasion pairs onsets with everything and never live with live, so
    /// absorbing every onset into ONE name removes every pair the moment was
    /// evidence for and replaces them with name-to-member — which the name's own
    /// definition already records. So the rule is that the onsets may not all end
    /// up inside a single name: <b>at least two things must still carry one</b>,
    /// unless only one onset arrived and there was never a pair to keep.
    /// Counting happens first regardless, so evidence is never lost — only the
    /// substitution is refused.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// <b>AND A NAME MAY NOT SPAN TWO GROUPS, WHICH IS A REGRESSION THIS CLASS
    /// CAUSED AND NOT A PRECAUTION.</b> <see cref="Worlds.Binding"/> gates pairing
    /// by object so that a colour never joins the other object's shape, and its
    /// graph collapsed twelvefold when it did. A minted name is a code the front
    /// end never saw, so it appears in no group, so it paired with EVERYTHING —
    /// and the collapse went from 144 edges against 1,751 to 3,054 against 3,258.
    /// <b>A chunk covering two objects asserts exactly the binding the world says
    /// did not happen</b>, so it is refused; a chunk inside one object inherits it,
    /// which <see cref="Machines.InputMachine{TFrame}"/> writes back into the
    /// grouping.
    /// </remarks>
    private Substitution Fold(
        IReadOnlyCollection<Code> moment,
        IReadOnlySet<Code> onsets,
        IReadOnlyDictionary<Code, int>? groups,
        bool counting)
    {
        var current = moment.Distinct().Order().ToList();

        // WHAT EACH THING IN HAND COVERS. An original covers itself; a name covers
        // what it was minted for. This is what keeps the naming a function of the
        // originals rather than of the merge path.
        var covers = current.ToDictionary(one => one, one => ImmutableArray.Create(one));

        var names = ImmutableDictionary.CreateBuilder<Code, ImmutableArray<Code>>();

        // HOW MANY THINGS MUST STILL CARRY AN ONSET WHEN THIS IS DONE. Two, so a
        // pair of onsets can never collapse into one origin -- or one, where only
        // one onset arrived and there was no pair to protect in the first place.
        var wanted = Math.Min(2, current.Count(one => onsets.Contains(one)));

        while (current.Count >= 2)
        {
            if (counting) Count(current, covers);

            // A FOLD FROM TWO COVERS THE WHOLE MOMENT. See above.
            if (current.Count < 3) break;

            if (!Best(current, covers, onsets, wanted, groups,
                    out var left, out var right, out var key))
                break;

            var members = _members[key];
            var name = new Code(Minted, key);

            current.Remove(left);
            current.Remove(right);
            current.Add(name);
            current.Sort();

            covers[name] = members;
            _used[key] = _used.GetValueOrDefault(key) + 1;

            // A NAME MAY SWALLOW A NAME, so what is reported is what SURVIVED. An
            // inner name is not in the moment any more and telling a caller it was
            // would move codes to live that are not there at all.
            names.Remove(left);
            names.Remove(right);
            names[name] = members;
        }

        return new Substitution([.. current], names.ToImmutable());
    }

    /// <summary>
    /// Counts every pair in hand, and mints the ones that have paid for themselves.
    /// </summary>
    /// <remarks>
    /// <b>QUADRATIC IN THE MOMENT AND THAT IS THE WHOLE COST OF REACHING BELOW
    /// IT.</b> Enumerating subsets is exponential; enumerating pairs and letting
    /// them compose is not, and it reaches the same sets over repeated arrivals.
    /// </remarks>
    private void Count(List<Code> current, Dictionary<Code, ImmutableArray<Code>> covers)
    {
        // THE MARGINALS AND THE DENOMINATOR, TAKEN OVER THE SAME POPULATION AS THE
        // JOINT COUNTS BELOW -- one round of counting, whether it is a moment's
        // first pass or a later one over what the merges left.
        _rounds++;
        foreach (var one in current) _occurs[one] = _occurs.GetValueOrDefault(one) + 1;

        for (var i = 0; i < current.Count; i++)
        {
            for (var j = i + 1; j < current.Count; j++)
            {
                var members = covers[current[i]].AddRange(covers[current[j]]).Sort();
                var key = Name(members);

                var count = _seen.GetValueOrDefault(key) + 1;
                _seen[key] = count;
                _members[key] = members;

                if (_minted.Contains(key)) continue;

                // MINIMUM DESCRIPTION LENGTH, and every term of it is the world's
                // own arithmetic. See the note on this class.
                if ((long)count * (members.Length - 1) <= members.Length) continue;

                // AND IT MUST ALSO BEAT CHANCE, WHICH DESCRIPTION LENGTH NEVER
                // ASKED. MDL weighs naming against NOT naming and is satisfied by
                // any pair frequent enough to be worth a symbol -- including one
                // that is frequent only because both its halves are. Measured on
                // `Motif`: the structured world minted 245 names for 6 recurring
                // sets and THE PURE-NOISE CONTROL MINTED 715. A detector finding
                // three times more structure in noise than in signal has found
                // none, and since this exists to make the graph SMALLER, the
                // control's 4,676 edges against the structured world's 2,733 is
                // the mechanism running backwards.
                if (count <= Chance(current[i], current[j])) continue;

                _minted.Add(key);
            }
        }
    }

    /// <summary>
    /// What two things would co-occur by accident, <b>and the spread around it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE EXPECTATION IS THE PRODUCT OF THE MARGINALS, which is independence
    /// and nothing more.</b> Two things each in hand often will meet often without
    /// either telling you anything about the other, and that is the entire
    /// population of the noise this was minting.
    /// </para>
    /// <para>
    /// <b>THE SPREAD IS THE SQUARE ROOT, WHICH IS THE NULL MODEL'S OWN AND NOT A
    /// CHOICE.</b> Occurrences of an independent pair are Poisson about that
    /// expectation, so the deviation is <c>√λ</c> by the distribution's own
    /// arithmetic — the same move the description-length inequality makes, applied
    /// to the question it never asked.
    /// </para>
    /// <para>
    /// <b>THE THREE IS BORROWED AND IS THE ONE CHOSEN NUMBER HERE — say so rather
    /// than dress it up.</b> Three standard errors is already this project's bar
    /// for believing a difference: <see cref="Worlds.Senses"/>'s headline claim is
    /// asserted against exactly that, so a name is now held to the same standard
    /// as a result. It is a constant nobody derived, which is a refuted row's
    /// shape, and the honest defence is only that it is the bar already in use. A
    /// sweep is what would settle it.
    /// </para>
    /// </remarks>
    private double Chance(Code left, Code right)
    {
        if (_rounds == 0) return 0.0;

        var expected =
            (double)_occurs.GetValueOrDefault(left) * _occurs.GetValueOrDefault(right) / _rounds;

        return expected + (3 * Math.Sqrt(expected));
    }

    /// <summary>
    /// The best already-named pair in hand, <b>chosen the same way on every
    /// machine.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE MOST-SEEN PAIR, AND THE TIE BREAKS ON THE NAME.</b> Two candidates
    /// standing equal is what a small or repetitive world produces constantly, and
    /// leaving those to the enumeration order would make the fold depend on
    /// something nobody chose — which is the fault this project has now found in
    /// the walk's accumulator and in the fly's inhibition both.
    /// </remarks>
    private bool Best(
        List<Code> current,
        Dictionary<Code, ImmutableArray<Code>> covers,
        IReadOnlySet<Code> onsets,
        int wanted,
        IReadOnlyDictionary<Code, int>? groups,
        out Code left,
        out Code right,
        out ulong key)
    {
        left = default;
        right = default;
        key = 0;

        var best = -1;

        // WHAT CARRIES AN ONSET RIGHT NOW. A fold turns two of these into one, so
        // it may only run while that still leaves enough standing.
        var bearing = current.Count(one => covers[one].Any(onsets.Contains));

        for (var i = 0; i < current.Count; i++)
        {
            for (var j = i + 1; j < current.Count; j++)
            {
                var members = covers[current[i]].AddRange(covers[current[j]]).Sort();
                var candidate = Name(members);

                if (!_minted.Contains(candidate)) continue;

                // THE ONSET GUARD. Merging two bearers leaves one where there were
                // two; merging a bearer with a bystander leaves the count alone.
                var merging =
                    (covers[current[i]].Any(onsets.Contains) ? 1 : 0)
                    + (covers[current[j]].Any(onsets.Contains) ? 1 : 0);

                if (merging == 2 && bearing - 1 < wanted) continue;

                // AND IT MAY NOT SPAN TWO OBJECTS. See the note on `Fold`.
                if (Spans(members, groups)) continue;

                var count = _seen.GetValueOrDefault(candidate);
                if (count < best || (count == best && candidate >= key)) continue;

                best = count;
                key = candidate;
                left = current[i];
                right = current[j];
            }
        }

        return best >= 0;
    }

    /// <summary>
    /// Whether a candidate reaches across two things the front end kept apart.
    /// </summary>
    /// <remarks>
    /// <b>AN UNGROUPED MEMBER IS NOT A SECOND GROUP.</b> A front end may group
    /// some of a moment and not the rest — <see cref="Worlds.Clevr"/> does — and
    /// treating "no group" as its own would forbid a name covering an object and
    /// the background it arrived with, which is not a binding claim about two
    /// objects at all.
    /// </remarks>
    private static bool Spans(
        IReadOnlyCollection<Code> members, IReadOnlyDictionary<Code, int>? groups)
    {
        if (groups is null) return false;

        int? seen = null;

        foreach (var member in members)
        {
            if (!groups.TryGetValue(member, out var group)) continue;
            if (seen is { } already && already != group) return true;

            seen = group;
        }

        return false;
    }

    /// <summary>
    /// The name of a set: <b>a pure function of its members, order-free.</b>
    /// </summary>
    /// <remarks>
    /// <b>SORTED FIRST, BECAUSE AN OCCASION IS A SET.</b> The same codes arriving
    /// in a different order are the same moment, and a hash that disagreed would
    /// mint one code per permutation — which is the alphabet growing without
    /// bound rather than growing usefully.
    /// <para>
    /// <b><see cref="Agreed"/> is the same arithmetic <see cref="Bus.Ring"/>
    /// places codes with</b>, and shared rather than copied for the reason named
    /// there: two machines must mint the same name with nothing to ask.
    /// </para>
    /// </remarks>
    private static ulong Name(IReadOnlyCollection<Code> codes)
    {
        var hash = Agreed.Basis;

        foreach (var code in codes.Order())
        {
            hash = Agreed.Fold(hash, code.Modality);
            hash = Agreed.Fold(hash, code.Value);
        }

        return Agreed.Mix(hash);
    }

    public override string ToString() => $"coined={Coined} noticed={Noticed}";
}
