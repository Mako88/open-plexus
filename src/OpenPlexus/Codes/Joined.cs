using OpenPlexus.Worlds;

namespace OpenPlexus.Codes;

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
    /// One extra code for every word the question and the story both used.
    /// </summary>
    /// <remarks>
    /// <b>The shared thing named, which is a lookup dressed as a binding.</b>
    /// <i>Mary is in both</i> becomes its own code, so a scope can hold <i>the asked
    /// person was mentioned</i> AND which person it was. That is one rule per person per
    /// place, which is sound and finite and is not a variable.
    /// </remarks>
    Named,

    /// <summary>
    /// One code saying the question and the story shared a word, and never which.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The plan's own cheapest test of the most expensive rung</b>, and throwing the
    /// identity away is the whole mechanism. A variable is a thing whose identity does
    /// not matter to the rule that uses it, so a code meaning <i>whoever was asked about
    /// was mentioned</i> is a variable's shadow — a shared sub-code rather than a binding.
    /// <see cref="Named"/> holds the identity and is the control that says whether
    /// dropping it is what pays.
    /// </para>
    /// <para>
    /// <b>And it reaches one rule a place where <see cref="Named"/> REACHES ONE A PAIR.</b>
    /// If the coincidence is what carries the task, this is the arm that generalises across
    /// people it has never seen — and if it is not, the two arms come back together and the
    /// signal was the identity all along.
    /// </para>
    /// </remarks>
    Anonymous,

    /// <summary>
    /// A code when they shared a word and a different code when they shared none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The absence said positively</b>, which is the only way a conjunction can read one.
    /// <see cref="Anonymous"/> marks the rounds where the asked-about word was mentioned
    /// and leaves the others untouched — but the rounds a rule must be kept OFF are exactly
    /// the untouched ones, and a scope is a subset test with no way to say <i>and not
    /// this</i>. So the arm that names only the coincidence can gain a seat it already had
    /// and lose none it should.
    /// </para>
    /// <para>
    /// <b>And it is John's own proposal for rung two</b>, moved to the front end. Emitting
    /// <i>Z was absent</i> as its own code needs no negation in the scope language and no
    /// new matcher. What it costs there is a settled occasion; what it costs here is that
    /// the front end must know which absence is worth a name, and a coincidence is one it
    /// can compute without knowing anything about the text.
    /// </para>
    /// </remarks>
    Either,

    /// <summary>
    /// Every word plainly, and again tagged with how many statements back it was.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Recency as a code</b>, which is what a scope needs to express *the latest one*. A
    /// scope is a subset test over a set, so <i>most recent</i> is unsayable however the
    /// bag is arranged — there is no position in a set. Minting <i>bedroom, one statement
    /// back</i> as its own code makes it sayable, and the learner is free to prefer it, to
    /// ignore it, or to specialise on it.
    /// </para>
    /// <para>
    /// <b>And the plain word is kept beside it</b>, which is the point rather than a hedge.
    /// Emitting only the tagged form would make two occurrences of one word in different
    /// sentences unrelatable, which is the quantisation-boundary fault this repo already
    /// refuses. Several codes per reading so near readings overlap is
    /// <see cref="Winnow"/>'s own answer, arriving on time rather than position.
    /// </para>
    /// <para>
    /// <b>It costs vocabulary, and vocabulary is the memory budget here.</b> Residents times
    /// codes is what a holder carries, so banding multiplies the alphabet by the number of
    /// bands — which is why the bands are few and the oldest is a single catch-all rather
    /// than a code per depth.
    /// </para>
    /// </remarks>
    Recent,


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
    /// The newest statement the question names something in — <b>the store read at the key
    /// the question supplies</b>, rather than at whichever key moved last.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Fork 88, and the arm the displacement grid points at.</b> Every situation arm
    /// before this keys on recency and is at its ceiling only where it keeps ONE statement,
    /// because a matcher that can ask only whether a code is present cannot choose between
    /// two states in the room. So the choosing is done where the key is: <i>where is
    /// mary</i> names <i>mary</i>, and the statement wanted is the newest one about her.
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
    /// </remarks>
    Addressed,

    /// <summary>
    /// The same lookup, taken again at the key the LAST reading supplied — <b>a chain of
    /// statements rather than one.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Fork 96</b>, and what held it to one hop was never the learner.
    /// <see cref="Addressed"/> returns the first statement naming anything the question
    /// names, and stops. Where the question names the apple, the apple's newest statement
    /// says who picked it up and never where he went — so the answering word was not in the
    /// room, and no learner could have found it. Reading again at <i>john</i> is the hop.
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
    /// back dropping superseded statements, <see cref="Addressed"/> walks back to the first
    /// one the question names, <see cref="Chained"/> walks back again at the key that found.
    /// All three are a LOOKUP over a fixed text, so nothing that was read ever changed
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
public sealed class Joined : IQuantizer<Asking>
{
    /// <summary>The modality a coincidence rides on.</summary>
    /// <remarks>
    /// <b>Its own, so a marked word is not the word.</b> Sharing <see cref="Babi"/>'s
    /// modality would make <i>mary</i> and <i>mary was also asked about</i> the same code,
    /// which is the distinction this whole arm exists to make.
    /// </remarks>
    public const byte Both = 42;

    /// <summary>The one code <see cref="Joining.Anonymous"/> emits.</summary>
    /// <remarks>
    /// <b>A constant</b>, because its whole content is that it is the same one every time.
    /// A code derived from what was shared would be <see cref="Joining.Named"/> by a side
    /// door, and the arm would stop measuring what it is for.
    /// </remarks>
    public static readonly Code Coincided = new(Both, 0);

    /// <summary>The code <see cref="Joining.Either"/> emits when there was no coincidence.</summary>
    /// <remarks>
    /// <b>One, because nought is already the other answer to the same question.</b> They
    /// are mutually exclusive by construction, so a moment carrying both would be a bug
    /// this arm could not otherwise see.
    /// </remarks>
    public static readonly Code Sundered = new(Both, 1);

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
    private readonly Sorting _categories;
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
        _categories = new Sorting(categories ?? []);
        _hops = hops;
        _banded = banded;
        _resolution = resolution;
        _freshest = freshest;
    }

    /// <inheritdoc/>
    public byte Modality => Both;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(Asking observation)
    {
        var said = new HashSet<Code>(observation.Words);
        said.UnionWith(observation.Question);

        if (_joining == Joining.Recent) return _categories.Folded(Banding(said, observation));

        if (_joining == Joining.Distinguished) return _categories.Folded(Situating(observation));

        if (_joining == Joining.Addressed) return _categories.Folded(Addressing(said, observation));

        if (_joining == Joining.Chained) return _categories.Folded(Chaining(said, observation));

        if (_joining == Joining.Resolved) return _categories.Folded(Storing(said, observation));

        if (_joining == Joining.Bagged) return _categories.Folded(said);

        // The intersection is taken over the halves and never over the union, which reads
        // as pedantry until the union has already lost the distinction. Every code in the
        // bag is in the bag; only the two halves know which are in both.
        var shared = observation.Question.Where(observation.Words.Contains).ToList();

        // The one arm that speaks when there is nothing to say, which is its whole point.
        // Every other arm falls through to the plain bag here, and the bag is what cannot
        // be conditioned off.
        if (shared.Count == 0)
        {
            if (_joining == Joining.Either) said.Add(Sundered);
            return said;
        }

        if (_joining == Joining.Named) foreach (var one in shared) said.Add(new Code(Both, one.Value));
        else said.Add(Coincided);

        return _categories.Folded(said);
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

        var hash = Agreed.Fold(Agreed.Basis, (ulong)members.Count);

        foreach (var code in members.Order())
        {
            hash = Agreed.Fold(hash, code.Modality);
            hash = Agreed.Fold(hash, code.Value);
        }

        return new Code(Sorted, Agreed.Mix(hash));
    }

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
    private HashSet<Code> Situating(Asking observation)
    {
        var said = new HashSet<Code>(observation.Question);
        var claimed = new HashSet<Code>();
        var keys = new List<Code>();

        // The background is the story's own, and there is no other kind here any more. A
        // rank over the corpus was the first version and it is deleted -- see the revival
        // row: the motion verbs straddle the names, so no rank separates them.
        var constant = Shared(observation);

        foreach (var statement in observation.Story)
        {
            keys.Clear();

            var superseded = false;

            foreach (var one in statement)
            {
                if (constant.Contains(one)) continue;

                if (claimed.Contains(one)) { superseded = true; break; }

                keys.Add(one);
            }

            if (superseded) continue;

            foreach (var key in keys) claimed.Add(key);

            said.UnionWith(statement);
        }

        return said;
    }

    /// <summary>
    /// The question, and the newest statement that names something the question names.
    /// </summary>
    /// <remarks>
    /// <b>Newest first makes this one pass and one statement.</b> The story arrives newest
    /// first, so the first statement intersecting the question IS the newest one about
    /// whatever was asked — no scoring, no comparison, and nothing to tie-break.
    /// <b>The intersection is against the question's words and never against the bag</b>,
    /// which is the same line <see cref="Joining.Named"/> stands on: every word of the story
    /// is in the story, so only the two halves know which are in both.
    /// </remarks>
    private static HashSet<Code> Addressing(HashSet<Code> said, Asking observation)
    {
        foreach (var statement in observation.Story)
        {
            var names = false;

            foreach (var one in statement)
                if (observation.Question.Contains(one)) { names = true; break; }

            if (!names) continue;

            var found = new HashSet<Code>(observation.Question);
            found.UnionWith(statement);

            return found;
        }

        // Nothing the question names was ever said, so there is no store entry to read and
        // the bag is what is left. See the arm's remarks: going silent would score the
        // abstention rather than the mechanism.
        return said;
    }

    /// <summary>
    /// The question, and a chain of statements each found at the key the last one supplied.
    /// </summary>
    /// <remarks>
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
    private HashSet<Code> Chaining(HashSet<Code> said, Asking observation)
    {
        var background = Shared(observation);
        var moment = new HashSet<Code>(observation.Question);
        var key = observation.Question;
        var from = 0;
        var read = 0;

        for (var hop = 0; hop < _hops; hop++)
        {
            var at = -1;

            for (var one = from; one < observation.Story.Count && at < 0; one++)
                foreach (var code in observation.Story[one])
                    if (key.Contains(code)) { at = one; break; }

            if (at < 0) break;

            moment.UnionWith(observation.Story[at]);
            read++;

            // And which hop found it, where the arm asks for it. A scope is a subset test
            // over a set, so *the statement the question named* and *the one that named*
            // are the same words in one bag and unsayable apart -- which is why a chain
            // that fetched the right sentence bought nothing. The band makes the
            // difference sayable, and the plain word stays beside it so two occurrences of
            // one word at different depths remain relatable.
            if (_banded)
                foreach (var code in observation.Story[at])
                    moment.Add(new Code(Both, unchecked(code.Value * Bands + (ulong)Math.Min(hop, Bands - 1) + 2)));

            key = new HashSet<Code>(observation.Story[at]
                .Where(code => !key.Contains(code) && !background.Contains(code)));

            from = at + 1;
        }

        // Nothing the question names was ever said, so there is no store entry to read at all
        // and the bag is what is left -- the same fallback Addressing takes, and for the same
        // reason: going silent would score the abstention rather than the mechanism.
        return read == 0 ? said : moment;
    }

    /// <summary>
    /// The question, and what a forward store holds about the things the question names.
    /// </summary>
    /// <param name="said">The plain bag, which is what a store with nothing to say falls back to.</param>
    /// <param name="observation">The story and the question.</param>
    /// <remarks>
    /// <para>
    /// <b>Oldest first</b>, which is the opposite of every other arm here and is the point.
    /// <see cref="Asking.Story"/> arrives newest first because a distance from the question
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
    /// line <see cref="Addressing"/> stands on: the front end intersects two sets and never
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
    /// </remarks>
    private HashSet<Code> Storing(HashSet<Code> said, Asking observation)
    {
        var levels = new Dictionary<Code, HashSet<Code>>[_resolution + 1];

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
            var written = new HashSet<Code>[_resolution + 1][];

            for (var depth = 0; depth <= _resolution; depth++)
            {
                written[depth] = new HashSet<Code>[keys.Count];

                for (var one = 0; one < keys.Count; one++)
                {
                    var value = new HashSet<Code>(statement);

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

        var moment = new HashSet<Code>(observation.Question);
        var read = 0;

        foreach (var one in observation.Question)
            if (levels[_resolution].TryGetValue(one, out var held))
            {
                moment.UnionWith(held);
                read++;
            }

        return read == 0 ? said : moment;
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
    private static HashSet<Code> Shared(Asking observation)
    {
        if (observation.Story.Count == 0) return [];

        var every = new HashSet<Code>(observation.Story[0]);

        for (var one = 1; one < observation.Story.Count; one++)
            every.IntersectWith(observation.Story[one]);

        return every;
    }

    /// <summary>
    /// Every word again, carrying how many statements back it was said.
    /// </summary>
    /// <remarks>
    /// <b>The question's own words are not banded</b>, because they are not in the story and a
    /// band on them would say something false. A depth is a distance from the question,
    /// so the question sits at no distance from itself.
    /// </remarks>
    private static HashSet<Code> Banding(HashSet<Code> said, Asking observation)
    {
        for (var back = 0; back < observation.Story.Count; back++)
        {
            // Everything past the last band shares its code, so an alphabet stays finite
            // over a story that is not. The catch-all says *older than this* rather than
            // *this far back*, which is the honest reading of what it can support.
            var band = Math.Min(back, Bands - 1);

            foreach (var one in observation.Story[back])
                said.Add(new Code(Both, unchecked(one.Value * Bands + (ulong)band + 2)));
        }

        return said;
    }

    /// <inheritdoc/>
    /// <remarks><b>Nothing, which is what a world that says none of these gets.</b></remarks>
    public IReadOnlyDictionary<Code, int>? Bind(Asking observation) => null;

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>Not the order of the words</b>, and the reason changed the day rung three was
    /// built. It used to be that a sequence had nowhere to go —
    /// <see cref="Sequenced"/> now takes one and turns it into a code, so an order reported
    /// here would be read rather than ignored.
    /// </para>
    /// <para>
    /// <b>What blocks it is that <see cref="Worlds.Asking.Story"/> is a list of sets.</b>
    /// A statement's words arrive here already unordered, so this arm has nothing to report
    /// — the order is destroyed by the shape of what the world hands over, one type before
    /// the front end. <see cref="Worlds.Recited"/> is the same moment with the word order
    /// still on it, and every text world here would need that shape before rung three could
    /// be measured on one.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<Code, int>? Order(Asking observation) => null;

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Fleeting(Asking observation) => null;

    /// <inheritdoc/>

    /// <inheritdoc/>

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Forced(Asking observation) => null;
}
