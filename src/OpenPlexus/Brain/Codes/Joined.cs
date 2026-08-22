using OpenPlexus.Worlds;

namespace OpenPlexus.Codes;

/// <summary>
/// A moment's parts as SETS — <b>the shape every story arm reads.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A reading rather than a moment</b>, which is why it is derived and never carried. A
/// world hands over parts in order; every arm below asks whether a word was in a statement,
/// so turning each part into a set once a call is the difference between one pass and one
/// per lookup. The order stays on <see cref="Coded.Groups"/>, where
/// <see cref="Joined.Order"/> reads it.
/// </para>
/// <para>
/// <b>And it is not <see cref="Joining.Bagged"/></b>, which is the arm that reads every
/// statement. This is the shape all four arms read, including the three that select.
/// </para>
/// </remarks>
public readonly record struct Storied
{
    /// <summary>The statements, newest first.</summary>
    public required IReadOnlyList<IReadOnlySet<Code>> Story { get; init; }

    /// <summary>The words of the question.</summary>
    public required IReadOnlySet<Code> Question { get; init; }

    /// <summary>Every word of every statement, which is what a bag-of-words arm wants.</summary>
    public IReadOnlySet<Code> Words
    {
        get
        {
            var all = new HashSet<Code>();

            foreach (var one in Story) all.UnionWith(one);

            return all;
        }
    }

    /// <summary>One moment as the sets the arms read.</summary>
    /// <param name="moment">What the world showed.</param>
    public static Storied Of(Coded moment) =>
        new()
        {
            Story = moment.Groups is { } parts
                ? [.. parts.Select(one => (IReadOnlySet<Code>)new HashSet<Code>(one.Codes))]
                : [],
            Question = moment.Asked is { } asked
                ? new HashSet<Code>(asked.Codes)
                : new HashSet<Code>(),
        };
}

/// <summary>What a translation does with a question and the story in front of it.</summary>
/// <remarks>
/// <b>A fact about the pipe, which is neither side's to decide</b> — the same place
/// <see cref="Machines.Fronting"/> sits. The world says which words were the question and
/// stops there; what is made of that split is chosen here, and the learner is told
/// nothing about which arm it is running under.
/// </remarks>
public enum Joining
{
    /// <summary>
    /// Both halves in one bag, which is every reading taken before this existed.
    /// </summary>
    /// <remarks>
    /// <b>The control, and it has to be named to be one.</b> A front end that only ever
    /// marks coincidences cannot say whether the marking is what paid — and this project
    /// has a standing rule that an arm only lives while it is compared.
    /// </remarks>
    Bagged,

    /// <summary>
    /// The same displacement, keyed on what this story does not share — <b>no corpus
    /// statistic, no dial, and no threshold to straddle.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Because frequency cannot separate a verb from a name here</b>, and that is
    /// arithmetic rather than bad luck. Cutting the vocabulary at a rank was the first
    /// version and it is deleted: on this corpus <i>went</i> is written more often than any
    /// of the four people while <i>journeyed</i>, <i>travelled</i> and <i>moved</i> are
    /// written less than all of them. The verbs straddle the names, so no rank keeps the
    /// names as keys and drops the verbs — and a shared <i>went</i> supersedes statements
    /// about different people.
    /// </para>
    /// <para>
    /// <b>So the background is taken from the story instead</b>: a key is any word not in every
    /// statement of it. Where each sentence says <i>went to the</i>, those three are
    /// background and <i>mary</i> is not — so <i>Mary went to the garden</i> supersedes
    /// <i>Mary went to the kitchen</i> and leaves <i>John went to the office</i> standing,
    /// with nothing told to the front end at all.
    /// </para>
    /// <para>
    /// <b>And its failure mode is named before it runs</b>, because it is the same one. One
    /// sentence saying <i>journeyed</i> drops <i>went</i> out of the intersection and makes
    /// it a key again, so a story mixing its verbs supersedes across people exactly as the
    /// deleted rank did. The ceiling column says how often that happens without anything
    /// having to learn.
    /// </para>
    /// </remarks>
    Distinguished,

    /// <summary>
    /// The newest statement the question names something in, and then the same lookup again
    /// at the key THAT supplied — <b>the store read at the key the question gives</b>, walked
    /// as far as the hops allow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Fork 88 at one hop, fork 96 past it</b>, and one mechanism either way. Every
    /// situation arm before this keys on recency and is at its ceiling only where it keeps
    /// ONE statement, because a matcher that can ask only whether a code is present cannot
    /// choose between two states in the room. So the choosing is done where the key is:
    /// <i>where is mary</i> names <i>mary</i>, and the statement wanted is the newest one
    /// about her. The hop count is the axis, and one hop is the arm that used to be
    /// spelt separately — two names for one rule, which cost a session and is now a check.
    /// </para>
    /// <para>
    /// <b>It is selection and not displacement</b>, which is why it is a different arm rather
    /// than a setting. <see cref="Distinguished"/> asks what a newer statement made false and
    /// throws that away for good; this asks what the question is about and reads only that.
    /// One is a store maintained forwards, the other a lookup done backwards, and the grid
    /// says the first pays only by accident.
    /// </para>
    /// <para>
    /// <b>And it is still not unification, which is the honest limit.</b> A front end
    /// intersecting two sets is arithmetic; a scope naming no argument is a matcher this
    /// repo does not have. What this can say is what such a matcher would be WORTH, which
    /// is the number fork 33 wants before anybody pays for one.
    /// </para>
    /// <para>
    /// <b>Falls back to the whole bag where the question names nothing said</b>, because a
    /// moment with no statement in it can answer nothing at all, and an arm that went silent
    /// would be scoring its own abstentions.
    /// </para>
    /// <para>
    /// And what a second hop is for: where the question names the apple, the apple's
    /// newest statement says who picked it up and never where he went — so the answering word
    /// was not in the room, and no learner could have found it. Reading again at <i>john</i>
    /// is the hop.
    /// </para>
    /// <para>
    /// <b>And the ceiling says the room was the whole shortfall.</b> One hop leaves the answer
    /// present on a quarter of two-fact questions and a twelfth of three-fact ones; two hops
    /// reach near a half and a quarter, three reach four in seven and two in five.
    /// </para>
    /// <para>
    /// <b>Its control is a span-matched bag and never one hop</b>, which is the reading that cost
    /// the most to get. Every statement of this corpus says <i>to</i> and <i>the</i>, so a
    /// chain keyed on everything walks back a sentence at a time and never follows a referent
    /// — and about half of what the hops buy is exactly that. Scored against one hop this arm
    /// reads twice as good as it is.
    /// </para>
    /// <para>
    /// <b>The key is what the reading added</b>, and is not the story's background, which is
    /// <see cref="Distinguished"/>'s own answer to fork 95 reused rather than a second rule. A
    /// key already used is the hop just taken, so carrying one forward returns the same
    /// statement for ever; and a key every sentence contains is recency wearing a chain's
    /// name. <b>The two rules beaten are naive-everything and members-of-a-category</b>, both
    /// inside a standard error of this one and neither leading twice — see the revival row.
    /// </para>
    /// </remarks>
    Chained,

    /// <summary>
    /// A store maintained FORWARDS through the story, read at the question's words — <b>what
    /// is known about a thing now</b>, rather than which sentence mentioned it last.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every arm above reads the transcript backwards at question time</b>, which is the
    /// property they share and the one that caps them. <see cref="Distinguished"/> walks
    /// back dropping superseded statements, <see cref="Chained"/> walks back to the first
    /// one the question names and then again at the key that found.
    /// Both are a LOOKUP over a fixed text, so nothing that was read ever changed
    /// anything — and the plan's own statement of the problem is that reading a statement
    /// must change something.
    /// </para>
    /// <para>
    /// <b>So this one is maintained oldest first</b>, holds one value per key, and that is
    /// retraction. A newer statement naming a key REPLACES what was held about it rather
    /// than sitting in front of it, so the store says what is true now and the transcript
    /// says what was said. That is the store the monotone counters forbid, built where they
    /// cannot reach it — beside the population rather than inside it.
    /// </para>
    /// <para>
    /// <b>And what makes it more than a record is the resolution depth</b>, which is an axis on
    /// <see cref="Joined"/> And carries its own control at nought. At depth nought an
    /// entry is the statement and nothing else, which for <i>john dropped the apple</i> is
    /// john and dropping and no room at all. Past that an entry also takes what was known
    /// about the statement's OTHER keys, so the apple's entry absorbs wherever john was — an
    /// inference performed while reading, which is forks 89, 91 and 93 as one question.
    /// </para>
    /// <para>
    /// <b>And it is John's account of an individual run forwards.</b> <see cref="Worlds.Returning"/>
    /// found a thing is re-identifiable as the bundle of relations it stands in rather than as
    /// a stored name, on a landmark that never moved. Here the landmark moves, and what holds
    /// the bundle together across the movement is the entry — so the individual is what the
    /// store accumulates rather than an index anybody handed over.
    /// </para>
    /// </remarks>
    Resolved,
}

/// <summary>
/// The translation between a question, the story in front of it, and codes.
/// </summary>
/// <remarks>
/// <para>
/// <b>The coincidence is computed here</b>, because it is not a fact about the text. That
/// <i>mary</i> occurs in both halves is arithmetic over two sets; that it MATTERS is a
/// claim, and a world making it would be a world deciding what the brain perceives. What
/// the world says is which words were the question, which is what it saw.
/// </para>
/// <para>
/// <b>And nothing here knows what a person is</b>, which is what keeps it a front end. No
/// stop list, no parser, no notion that <i>mary</i> is an actor and <i>garden</i> a place
/// — it intersects two sets of hashes. The same call would mark <i>the</i> if a question
/// ever used it, and on this corpus none does.
/// </para>
/// </remarks>
public sealed class Joined : IQuantizer<Coded>
{
    /// <summary>The modality a coincidence rides on.</summary>
    /// <remarks>
    /// <b>Its own, so a marked word is not the word.</b> Sharing <see cref="Babi"/>'s
    /// modality would make <i>mary</i> and <i>mary was also asked about</i> the same code,
    /// which is the distinction this whole arm exists to make.
    /// </remarks>
    public const byte Both = 42;



    /// <summary>How many statements back get their own band before the rest share one.</summary>
    /// <remarks>
    /// <b>Three, which is the shallowest depth the measured world needs.</b> Two supporting
    /// facts is a named bAbI task, so a band for the latest and one for the one before it
    /// is the minimum that could express either — and everything older shares a code
    /// because a story is not bounded and an alphabet has to be.
    /// </remarks>
    public const int Bands = 3;

    /// <summary>The modality a category rides on.</summary>
    /// <remarks>
    /// <para>
    /// <b>Its own, and not <c>Naming.Meant</c></b>, because the two fold the opposite
    /// way. A minted name is present when ALL its members are, which is what makes it a
    /// name for a co-firing set; a category is present when ANY member is, which is what
    /// makes it a name for a set of alternatives. Sharing a modality would let a soundness
    /// check spell one out as the other.
    /// </para>
    /// <para>
    /// <b>And it is a front end saying what it is looking at</b>, rather than what to conclude.
    /// <i>This code is one of a set that never co-occurs and keeps one company</i> is
    /// arithmetic over what was seen, in the same licence <see cref="Codes.Coded.Groups"/>
    /// carries for <i>these codes were one object</i>. Which category matters, and to what,
    /// is left entirely to the learner.
    /// </para>
    /// </remarks>
    public const byte Sorted = 43;

    private readonly Joining _joining;
    private readonly Categories _categories;
    private readonly int _hops;
    private readonly bool _banded;
    private readonly int _resolution;
    private readonly bool _freshest;

    /// <param name="joining">What to do with the two halves.</param>
    /// <param name="categories">
    /// Sets of codes that are ALTERNATIVES, each earning one extra code in any moment
    /// holding any of its members. <b>An independent axis and never a
    /// <see cref="Joining"/> value</b>, because a setting that decided two things while
    /// being named for one is a trap this repo has already paid for — every arm below is a
    /// cell of a grid rather than a point on a line.
    /// </param>
    /// <param name="hops">
    /// How many statements <see cref="Joining.Chained"/> may read, each at the key the last
    /// one supplied. <b>An independent axis, and read beside a span-matched control</b> — a
    /// deeper chain is also a wider moment, and widening is already known here to buy the
    /// drawn score and sell the held-out one.
    /// </param>
    /// <param name="banded">
    /// Whether <see cref="Joining.Chained"/> also tags each word with WHICH HOP found it.
    /// <b>An axis rather than a second arm</b>, because the chain without it is the control that
    /// says whether the banding is what pays — and the chain alone is already measured to
    /// raise what is in the room and not what is answered.
    /// </param>
    /// <param name="resolution">
    /// How many hops of the store <see cref="Joining.Resolved"/> folds into an entry as it
    /// writes it. <b>An axis carrying its own control at nought</b>, where an entry is the
    /// statement and nothing else — so whether resolving is what pays is read off the same
    /// arm rather than against a differently-named one. <b>Capped rather than transitive on
    /// purpose</b>, for the reason the entailment depth is capped: an unbounded fold reaches
    /// everything the story ever said, which is the bag by a longer road.
    /// </param>
    /// <param name="freshest">
    /// Whether <see cref="Joining.Resolved"/> folds through ONE of a statement's other keys —
    /// the one whose entry was written most recently — rather than through all of them.
    /// <b>An axis rather than a second arm</b>, and the cheapest thing that could answer fork
    /// 95. The story's own background calls every word not in every statement a key, so a
    /// VERB is a key and folding through it drags the last unrelated statement it appeared in
    /// along; which key is the one worth following is the question that rule cannot answer.
    /// <b>Recency over the store knows nothing about the text</b> — in
    /// <i>john dropped the apple</i> john has been written a statement ago and <i>dropped</i>
    /// not since the last drop, so the freshest is the one that moved.
    /// </param>
    public Joined(
        Joining joining, IReadOnlyList<IReadOnlySet<Code>>? categories = null, int hops = 2,
        bool banded = false, int resolution = 1, bool freshest = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(hops, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(resolution);

        _joining = joining;
        _categories = new Categories(categories ?? []);
        _hops = hops;
        _banded = banded;
        _resolution = resolution;
        _freshest = freshest;
    }

    /// <inheritdoc/>
    public byte Modality => Both;


    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(Coded moment) => Codify(Storied.Of(moment));

    /// <summary>The same, off the reading every arm shares.</summary>
    /// <param name="observation">The moment as sets.</param>
    private IReadOnlyCollection<Code> Codify(Storied observation)
    {
        var read = Read(observation);
        var said = new HashSet<Code>(observation.Question);

        foreach (var at in read.Count == 0 ? Every(observation) : read)
            said.UnionWith(observation.Story[at]);

        // Only the chain adds anything to what was read, and only when it is banding by
        // hop. Every other arm differs in WHICH statements it read rather than in what it
        // says about them, which is what `Read` decides and this does not.
        return _categories.Folded(
            _joining == Joining.Chained && _banded ? Banded(said, observation, read) : said);
    }

    /// <summary>What a category of these members is called, on every machine, forever.</summary>
    /// <param name="members">The alternatives it stands for, in any order.</param>
    /// <remarks>
    /// <b>Derived from the members and never from a position in a list.</b> Two front ends
    /// that noticed the same alternation must reach the same code without speaking, which is
    /// the rule <c>Naming.Name</c> stands on and the reason parent-plus-condition once
    /// gave one scope two names. An index would make a category mean one thing here and
    /// another on the machine that counted its statements in a different order.
    /// </remarks>
    public static Code Category(IReadOnlySet<Code> members)
    {
        ArgumentNullException.ThrowIfNull(members);

        if (members.Count < 2)
            throw new ArgumentException("a category of fewer than two codes says nothing", nameof(members));

        var hash = Hashing.Fold(Hashing.Basis, (ulong)members.Count);

        foreach (var code in members.Order())
        {
            hash = Hashing.Fold(hash, code.Modality);
            hash = Hashing.Fold(hash, code.Value);
        }

        return new Code(Sorted, Hashing.Mix(hash));
    }

    /// <summary>Which statements of the story this arm read, in the order it read them.</summary>
    /// <param name="observation">The story and the question.</param>
    /// <remarks>
    /// <para>
    /// <b>Every arm here selects statements</b>, which is what lets one report serve two
    /// channels. A moment is the question plus the words of whatever was selected, so
    /// <see cref="Codify(Coded)"/> unions and <see cref="Order"/> positions — off
    /// the same list, so the two cannot disagree about what was read.
    /// </para>
    /// <para>
    /// <b>An empty list means the arm selected nothing</b>, and the caller reads everything.
    /// That is the fallback three of them already had, for the reason written on each: an arm
    /// going silent would be scoring its own abstentions rather than its mechanism. <b>The
    /// failure is reported rather than papered over</b>, because <see cref="Joining.Chained"/>
    /// bands what its chain read and a chain that read nothing must band nothing.
    /// </para>
    /// </remarks>
    private List<int> Read(Storied observation) => _joining switch
    {
        Joining.Distinguished => Situating(observation),
        Joining.Chained => Chaining(observation),
        Joining.Resolved => Storing(observation),
        _ => Every(observation),
    };

    /// <summary>Every statement, which is what an arm that selects none of them reads.</summary>
    /// <param name="observation">The story and the question.</param>
    private static List<int> Every(Storied observation) =>
        [.. Enumerable.Range(0, observation.Story.Count)];

    /// <summary>
    /// The question, and every statement of the story nothing newer has superseded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Newest first, which is the order the world already hands them over.</b> Walking
    /// backwards from the question is what makes <i>superseded</i> decidable in one pass: a
    /// statement is dead if something already kept claimed one of its keys, and everything
    /// already kept is newer than it by construction.
    /// </para>
    /// <para>
    /// <b>A dropped statement claims nothing, which is not a detail.</b> If <i>Mary went to
    /// the kitchen</i> dies on <i>mary</i>, its <i>kitchen</i> must stay free — an older
    /// sentence about the kitchen was superseded by nothing, and letting a corpse claim keys
    /// would kill it. Only survivors write.
    /// </para>
    /// <para>
    /// <b>And the question's words go in unconditionally</b>, because the question is not part
    /// of the situation. It supersedes nothing and is superseded by nothing; it is what
    /// the situation is being asked about.
    /// </para>
    /// </remarks>
    private List<int> Situating(Storied observation)
    {
        var kept = new List<int>();
        var claimed = new HashSet<Code>();
        var keys = new List<Code>();

        // The background is the story's own, and there is no other kind here any more. A
        // rank over the corpus was the first version and it is deleted -- see the revival
        // row: the motion verbs straddle the names, so no rank separates them.
        var constant = Shared(observation);

        for (var at = 0; at < observation.Story.Count; at++)
        {
            keys.Clear();

            var superseded = false;

            foreach (var one in observation.Story[at])
            {
                if (constant.Contains(one)) continue;

                if (claimed.Contains(one)) { superseded = true; break; }

                keys.Add(one);
            }

            if (superseded) continue;

            foreach (var key in keys) claimed.Add(key);

            kept.Add(at);
        }

        return kept;
    }

    /// <summary>
    /// The question, and a chain of statements each found at the key the last one supplied.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Newest first makes each hop one pass and one statement. The story arrives newest
    /// first, so the first statement intersecting the key IS the newest one about whatever
    /// that key names — no scoring, no comparison, and nothing to tie-break.
    /// <b>The first hop reads against the question's words</b> and never against the bag, and
    /// every word of the story is in the story, so only the two halves know which are in both.
    /// </para>
    /// <para>
    /// <b>Older only, because the chain runs backwards in time.</b> Whatever a statement
    /// mentions was established before it, so the search resumes past the statement just
    /// taken — and searching the whole story again would let anything later answer, which is
    /// the recency this arm exists to beat.
    /// </para>
    /// <para>
    /// <b>And a chain that runs out stops rather than falling back.</b> A hop with no key left
    /// and a hop whose key names nothing older are both <i>the store has no more to say</i>,
    /// and widening to the bag there would quietly turn the deepest arm into the control on
    /// exactly the questions it found hardest.
    /// </para>
    /// </remarks>
    private List<int> Chaining(Storied observation)
    {
        var background = Shared(observation);
        var chain = new List<int>();
        var key = observation.Question;
        var from = 0;

        for (var hop = 0; hop < _hops; hop++)
        {
            var at = -1;

            for (var one = from; one < observation.Story.Count && at < 0; one++)
                foreach (var code in observation.Story[one])
                    if (key.Contains(code)) { at = one; break; }

            if (at < 0) break;

            chain.Add(at);

            key = new HashSet<Code>(observation.Story[at]
                .Where(code => !key.Contains(code) && !background.Contains(code)));

            from = at + 1;
        }

        // Nothing the question names was ever said, so there is no store entry to read at all
        // and the bag is what is left -- the same fallback Addressing takes, and for the same
        // reason: going silent would score the abstention rather than the mechanism.
        return chain;
    }

    /// <summary>The chain's words again, each carrying which hop found it.</summary>
    /// <param name="said">The moment so far.</param>
    /// <param name="observation">The story and the question.</param>
    /// <param name="chain">Which statement each hop read, in hop order.</param>
    /// <remarks>
    /// <b>Which hop found it, where the arm asks for it.</b> A scope is a subset test over a
    /// set, so <i>the statement the question named</i> and <i>the one that named</i> are the
    /// same words in one bag and unsayable apart — which is why a chain that fetched the right
    /// sentence bought nothing. The band makes the difference sayable, and the plain word
    /// stays beside it so two occurrences of one word at different depths remain relatable.
    /// <b>The hop is the position in the chain</b>, so this reads what the walk already
    /// recorded rather than re-walking it.
    /// </remarks>
    private static HashSet<Code> Banded(HashSet<Code> said, Storied observation, List<int> chain)
    {
        for (var hop = 0; hop < chain.Count; hop++)
        {
            foreach (var code in observation.Story[chain[hop]])
            {
                said.Add(new Code(
                    Both, unchecked(code.Value * Bands + (ulong)Math.Min(hop, Bands - 1) + 2)));
            }
        }

        return said;
    }

    /// <summary>
    /// Which statements a forward store holds about the things the question names.
    /// </summary>
    /// <param name="observation">The story and the question.</param>
    /// <remarks>
    /// <para>
    /// <b>Oldest first</b>, which is the opposite of every other arm here and is the point.
    /// <see cref="Coded.Groups"/> arrives newest first because a distance from the question
    /// means the same thing in every story; a store is maintained in the order the world
    /// happened, so this walks it backwards to go forwards.
    /// </para>
    /// <para>
    /// <b>Every new entry is computed from the store as it stood before</b>, so
    /// the result does not depend on the order the keys of one statement are enumerated
    /// in. Writing as it goes would make a moment a fact about a hash set's iteration
    /// order, which is fork 12's failure wearing a front end's clothes.
    /// </para>
    /// <para>
    /// <b>The read is at the question's words</b>, and not at one of them, which is the same
    /// line <see cref="Chaining"/> stands on: the front end intersects two sets and never
    /// decides which member of the question is the subject. A question naming nothing the
    /// store has an entry for falls back to the bag, because an arm that went silent would
    /// be scoring its own abstentions.
    /// </para>
    /// <para>
    /// <b>One store per depth</b>, which is what makes the fold capped rather than transitive.
    /// Level nought is the statement alone; level <i>i</i> is the statement plus the OTHER
    /// keys' level <i>i-1</i> as it stood before this statement. Folding a key's own level
    /// <i>i</i> back in would make an entry grow without bound over a story, and the first
    /// version of this did exactly that — see the revival row.
    /// </para>
    /// <para>
    /// <b>And never against its own old entry at any depth</b>, which is what makes this a store
    /// rather than an accumulation. Replacement IS the retraction; keeping the old value
    /// means nothing is ever forgotten, and a moment that forgets nothing is the bag.
    /// </para>
    /// <para>
    /// <b>An entry is which statements wrote it and not their words</b>, which is exact rather
    /// than a summary: every value here is a union of whole statements, so the words are
    /// recoverable from the indices and the word order with them. It is what lets
    /// <see cref="Order"/> position a folded moment at all — a store of sets has
    /// thrown the order away one call before the report is asked for.
    /// </para>
    /// </remarks>
    private List<int> Storing(Storied observation)
    {
        var levels = new Dictionary<Code, HashSet<int>>[_resolution + 1];

        for (var depth = 0; depth <= _resolution; depth++) levels[depth] = [];

        var background = Shared(observation);
        var keys = new List<Code>();

        // When each key was last written, which is what the freshest rule reads and nothing
        // else does. A statement index rather than a clock, so it is a fact about the story
        // rather than about the machine that read it.
        var wrote = new Dictionary<Code, int>();

        for (var back = observation.Story.Count - 1; back >= 0; back--)
        {
            var statement = observation.Story[back];

            keys.Clear();

            foreach (var one in statement) if (!background.Contains(one)) keys.Add(one);

            // The one other key worth following, where the arm asks for one. Ties are broken
            // by the code's own order rather than by which arrived first, because two keys
            // written by one statement must resolve the same way on every machine -- the
            // rule `Category` stands on, and the reason a position in a list is never a name.
            var through = default(Code?);

            if (_freshest)
                foreach (var one in keys)
                    if (wrote.TryGetValue(one, out var when)
                        && (through is not { } best
                            || when > wrote[best]
                            || (when == wrote[best] && one.CompareTo(best) > 0)))
                        through = one;

            // Written after every value is computed, never during. See the remarks: an entry
            // that could see a sibling written by the same statement would make the moment
            // depend on a hash set's enumeration order, which is fork 12's failure.
            var written = new HashSet<int>[_resolution + 1][];

            for (var depth = 0; depth <= _resolution; depth++)
            {
                written[depth] = new HashSet<int>[keys.Count];

                for (var one = 0; one < keys.Count; one++)
                {
                    var value = new HashSet<int> { back };

                    if (depth > 0)
                        foreach (var other in keys)
                            if (!other.Equals(keys[one])
                                && (!_freshest || other.Equals(through))
                                && levels[depth - 1].TryGetValue(other, out var held))
                                value.UnionWith(held);

                    written[depth][one] = value;
                }
            }

            for (var depth = 0; depth <= _resolution; depth++)
                for (var one = 0; one < keys.Count; one++)
                    levels[depth][keys[one]] = written[depth][one];

            // Counted forwards, so a larger number is newer. `Story` arrives newest first and
            // this walk runs it backwards, so the loop's own index is a distance from the
            // question and reads the opposite way round from the order things happened.
            foreach (var key in keys) wrote[key] = observation.Story.Count - back;
        }

        var read = new HashSet<int>();

        foreach (var one in observation.Question)
            if (levels[_resolution].TryGetValue(one, out var held)) read.UnionWith(held);

        // Newest first, which is the order every other arm here returns and the order
        // `Story` itself arrives in. The store was walked the other way round to maintain
        // it, so this is where that gets undone rather than left for a caller to know about.
        return [.. read.OrderBy(at => at)];
    }

    /// <summary>
    /// The words every statement of this story uses, which are the ones that key nothing.
    /// </summary>
    /// <remarks>
    /// <b>The intersection and not a count</b>, because a threshold is the thing that failed.
    /// A word in every sentence distinguishes no two of them by construction, which is the
    /// property wanted — and unlike a rank it cannot put a verb on the wrong side of the
    /// names. <b>A story of one statement has itself as its background</b>, so nothing is a
    /// key and nothing is superseded, which is right: there is nothing older to displace.
    /// </remarks>
    private static HashSet<Code> Shared(Storied observation)
    {
        if (observation.Story.Count == 0) return [];

        var every = new HashSet<Code>(observation.Story[0]);

        for (var one = 1; one < observation.Story.Count; one++)
            every.IntersectWith(observation.Story[one]);

        return every;
    }


    /// <inheritdoc/>
    /// <summary>
    /// Where the words of the statements this arm read stood, oldest first.
    /// </summary>
    /// <param name="observation">One moment, with the word order still on it.</param>
    /// <remarks>
    /// <para>
    /// <b>Only what the arm read</b>, which is what makes the report say anything at all. A
    /// whole transcript says every word many times over, so a position map of it is a map of a
    /// word's LAST occurrence and nothing else. Selection is what leaves few enough statements
    /// for a position to be a fact — so rung three fires here only where a situation model has
    /// already narrowed the moment, and the bagged arm gets no order because it deserves none.
    /// </para>
    /// <para>
    /// <b>A word said twice is reported at neither place.</b> Position is one number a code,
    /// so a repeated word would take its last occurrence and claim to have stood only there.
    /// Dropping it is what makes <i>in</i> then <i>kitchen</i> derivable across the
    /// <i>the</i> between them, which is the precedence that separates a placement from a
    /// movement on <see cref="Worlds.Roaming"/>.
    /// </para>
    /// <para>
    /// <b>Oldest first, because the transcript happened that way round.</b>
    /// <see cref="Coded.Groups"/> arrives newest first so that a distance from the question
    /// means one thing in every story; a precedence means <i>this was said before that</i>,
    /// which is the other direction. A pair spanning two statements says the older one's last
    /// surviving word came before the newer one's first, and the arm's own selection is what
    /// put them next to each other.
    /// </para>
    /// <para>
    /// <b>And the question's words get no position</b>, which is
    /// <c>HandingTests</c>'s line exactly. A question and a statement are two utterances, so
    /// claiming one of their words came before another would invent a sequencing nobody
    /// stated.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<Code, int>? Order(Coded observation)
    {
        // The arm is run again rather than remembered, and the front end stays a function of
        // its input. A cached last answer would be state on a type whose whole contract is
        // that the same input gives the same codes on every machine forever.
        if (observation.Groups is not { Count: > 0 } said) return null;

        var read = Read(Storied.Of(observation));

        if (read.Count == 0) return null;

        var placed = new Dictionary<Code, int>();
        var twice = new HashSet<Code>();
        var at = 0;

        foreach (var statement in read.OrderByDescending(one => one))
        {
            foreach (var word in said[statement].Codes)
            {
                if (!placed.TryAdd(word, at)) twice.Add(word);

                at++;
            }
        }

        foreach (var word in twice) placed.Remove(word);

        return placed.Count > 1 ? placed : null;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>Passed through rather than derived</b>, which is the line this side of the seam
    /// sits on. Which words the learner was handed is a fact about the signal and the world
    /// is the only thing that knows it; what that entails is
    /// <see cref="Intervened"/>'s, and a front end deriving it would be deciding what a
    /// learner may conclude from having acted.
    /// </para>
    /// <para>
    /// <b>And it is the arm's own words rather than the world's</b>, which matters because a
    /// selecting arm may not have read the statement at all. The intersection is taken so a
    /// chosen statement the arm dropped marks nothing — the provenance of a word nobody saw
    /// is not a fact about this moment.
    /// </para>
    /// </remarks>
    public IReadOnlySet<Code>? Forced(Coded observation)
    {
        if (observation.Assigned is not { Count: > 0 } assigned) return null;

        var said = new HashSet<Code>(Codify(observation));

        said.IntersectWith(assigned);

        return said.Count > 0 ? said : null;
    }
}
