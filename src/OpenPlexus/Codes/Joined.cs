using OpenPlexus.Worlds;

namespace OpenPlexus.Codes;

/// <summary>What a translation does with a question and the story in front of it.</summary>
/// <remarks>
/// <b>A FACT ABOUT THE PIPE, WHICH IS NEITHER SIDE'S TO DECIDE</b> — the same place
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
    /// <b>THE CONTROL, AND IT HAS TO BE NAMED TO BE ONE.</b> A front end that only ever
    /// marks coincidences cannot say whether the marking is what paid — and this project
    /// has a standing rule that an arm only lives while it is compared.
    /// </remarks>
    Bagged,

    /// <summary>
    /// One extra code for every word the question and the story both used.
    /// </summary>
    /// <remarks>
    /// <b>THE SHARED THING NAMED, WHICH IS A LOOKUP DRESSED AS A BINDING.</b>
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
    /// <b>THE PLAN'S OWN CHEAPEST TEST OF THE MOST EXPENSIVE RUNG, AND THROWING THE
    /// IDENTITY AWAY IS THE WHOLE MECHANISM.</b> A variable is a thing whose identity does
    /// not matter to the rule that uses it, so a code meaning <i>whoever was asked about
    /// was mentioned</i> is a variable's shadow — a shared sub-code rather than a binding.
    /// <see cref="Named"/> holds the identity and is the control that says whether
    /// dropping it is what pays.
    /// </para>
    /// <para>
    /// <b>AND IT REACHES ONE RULE A PLACE WHERE <see cref="Named"/> REACHES ONE A PAIR.</b>
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
    /// <b>THE ABSENCE SAID POSITIVELY, WHICH IS THE ONLY WAY A CONJUNCTION CAN READ ONE.</b>
    /// <see cref="Anonymous"/> marks the rounds where the asked-about word was mentioned
    /// and leaves the others untouched — but the rounds a rule must be kept OFF are exactly
    /// the untouched ones, and a scope is a subset test with no way to say <i>and not
    /// this</i>. So the arm that names only the coincidence can gain a seat it already had
    /// and lose none it should.
    /// </para>
    /// <para>
    /// <b>AND IT IS JOHN'S OWN PROPOSAL FOR RUNG TWO, MOVED TO THE FRONT END.</b> Emitting
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
    /// <b>RECENCY AS A CODE, WHICH IS WHAT A SCOPE NEEDS TO EXPRESS *THE LATEST ONE*.</b> A
    /// scope is a subset test over a set, so <i>most recent</i> is unsayable however the
    /// bag is arranged — there is no position in a set. Minting <i>bedroom, one statement
    /// back</i> as its own code makes it sayable, and the learner is free to prefer it, to
    /// ignore it, or to specialise on it.
    /// </para>
    /// <para>
    /// <b>AND THE PLAIN WORD IS KEPT BESIDE IT, WHICH IS THE POINT RATHER THAN A HEDGE.</b>
    /// Emitting only the tagged form would make two occurrences of one word in different
    /// sentences unrelatable, which is the quantisation-boundary fault this repo already
    /// refuses. Several codes per reading so near readings overlap is
    /// <see cref="Winnow"/>'s own answer, arriving on time rather than position.
    /// </para>
    /// <para>
    /// <b>IT COSTS VOCABULARY, AND VOCABULARY IS THE MEMORY BUDGET HERE.</b> Residents times
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
    /// <b>BECAUSE FREQUENCY CANNOT SEPARATE A VERB FROM A NAME HERE, AND THAT IS
    /// ARITHMETIC RATHER THAN BAD LUCK.</b> Cutting the vocabulary at a rank was the first
    /// version and it is deleted: on this corpus <i>went</i> is written more often than any
    /// of the four people while <i>journeyed</i>, <i>travelled</i> and <i>moved</i> are
    /// written less than all of them. The verbs straddle the names, so no rank keeps the
    /// names as keys and drops the verbs — and a shared <i>went</i> supersedes statements
    /// about different people.
    /// </para>
    /// <para>
    /// <b>SO THE BACKGROUND IS TAKEN FROM THE STORY INSTEAD: a key is any word not in every
    /// statement of it.</b> Where each sentence says <i>went to the</i>, those three are
    /// background and <i>mary</i> is not — so <i>Mary went to the garden</i> supersedes
    /// <i>Mary went to the kitchen</i> and leaves <i>John went to the office</i> standing,
    /// with nothing told to the front end at all.
    /// </para>
    /// <para>
    /// <b>AND ITS FAILURE MODE IS NAMED BEFORE IT RUNS, BECAUSE IT IS THE SAME ONE.</b> One
    /// sentence saying <i>journeyed</i> drops <i>went</i> out of the intersection and makes
    /// it a key again, so a story mixing its verbs supersedes across people exactly as the
    /// deleted rank did. The ceiling column says how often that happens without anything
    /// having to learn.
    /// </para>
    /// </remarks>
    Distinguished,

    /// <summary>
    /// The newest statement the question names something in — <b>the store read at the key
    /// the question supplies, rather than at whichever key moved last.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>FORK 88, AND THE ARM THE DISPLACEMENT GRID POINTS AT.</b> Every situation arm
    /// before this keys on recency and is at its ceiling only where it keeps ONE statement,
    /// because a matcher that can ask only whether a code is present cannot choose between
    /// two states in the room. So the choosing is done where the key is: <i>where is
    /// mary</i> names <i>mary</i>, and the statement wanted is the newest one about her.
    /// </para>
    /// <para>
    /// <b>IT IS SELECTION AND NOT DISPLACEMENT, WHICH IS WHY IT IS A DIFFERENT ARM RATHER
    /// THAN A SETTING.</b> <see cref="Distinguished"/> asks what a newer statement made false and
    /// throws that away for good; this asks what the question is about and reads only that.
    /// One is a store maintained forwards, the other a lookup done backwards, and the grid
    /// says the first pays only by accident.
    /// </para>
    /// <para>
    /// <b>AND IT IS STILL NOT UNIFICATION, WHICH IS THE HONEST LIMIT.</b> A front end
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
    /// <b>FORK 96, AND WHAT HELD IT TO ONE HOP WAS NEVER THE LEARNER.</b>
    /// <see cref="Addressed"/> returns the first statement naming anything the question
    /// names, and stops. Where the question names the apple, the apple's newest statement
    /// says who picked it up and never where he went — so the answering word was not in the
    /// room, and no learner could have found it. Reading again at <i>john</i> is the hop.
    /// </para>
    /// <para>
    /// <b>AND THE CEILING SAYS THE ROOM WAS THE WHOLE SHORTFALL.</b> One hop leaves the answer
    /// present on a quarter of two-fact questions and a twelfth of three-fact ones; two hops
    /// reach near a half and a quarter, three reach four in seven and two in five.
    /// </para>
    /// <para>
    /// <b>ITS CONTROL IS A SPAN-MATCHED BAG AND NEVER ONE HOP, WHICH IS THE READING THAT COST
    /// THE MOST TO GET.</b> Every statement of this corpus says <i>to</i> and <i>the</i>, so a
    /// chain keyed on everything walks back a sentence at a time and never follows a referent
    /// — and about half of what the hops buy is exactly that. Scored against one hop this arm
    /// reads twice as good as it is.
    /// </para>
    /// <para>
    /// <b>THE KEY IS WHAT THE READING ADDED AND IS NOT THE STORY'S BACKGROUND</b>, which is
    /// <see cref="Distinguished"/>'s own answer to fork 95 reused rather than a second rule. A
    /// key already used is the hop just taken, so carrying one forward returns the same
    /// statement for ever; and a key every sentence contains is recency wearing a chain's
    /// name. <b>The two rules beaten are naive-everything and members-of-a-category</b>, both
    /// inside a standard error of this one and neither leading twice — see the revival row.
    /// </para>
    /// </remarks>
    Chained,
}

/// <summary>
/// The translation between a question, the story in front of it, and codes.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE COINCIDENCE IS COMPUTED HERE BECAUSE IT IS NOT A FACT ABOUT THE TEXT.</b> That
/// <i>mary</i> occurs in both halves is arithmetic over two sets; that it MATTERS is a
/// claim, and a world making it would be a world deciding what the brain perceives. What
/// the world says is which words were the question, which is what it saw.
/// </para>
/// <para>
/// <b>AND NOTHING HERE KNOWS WHAT A PERSON IS, which is what keeps it a front end.</b> No
/// stop list, no parser, no notion that <i>mary</i> is an actor and <i>garden</i> a place
/// — it intersects two sets of hashes. The same call would mark <i>the</i> if a question
/// ever used it, and on this corpus none does.
/// </para>
/// </remarks>
public sealed class Joined : IQuantizer<Asking>
{
    /// <summary>The modality a coincidence rides on.</summary>
    /// <remarks>
    /// <b>ITS OWN, SO A MARKED WORD IS NOT THE WORD.</b> Sharing <see cref="Babi"/>'s
    /// modality would make <i>mary</i> and <i>mary was also asked about</i> the same code,
    /// which is the distinction this whole arm exists to make.
    /// </remarks>
    public const byte Both = 42;

    /// <summary>The one code <see cref="Joining.Anonymous"/> emits.</summary>
    /// <remarks>
    /// <b>A CONSTANT, BECAUSE ITS WHOLE CONTENT IS THAT IT IS THE SAME ONE EVERY TIME.</b>
    /// A code derived from what was shared would be <see cref="Joining.Named"/> by a side
    /// door, and the arm would stop measuring what it is for.
    /// </remarks>
    public static readonly Code Coincided = new(Both, 0);

    /// <summary>The code <see cref="Joining.Either"/> emits when there was no coincidence.</summary>
    /// <remarks>
    /// <b>ONE, BECAUSE NOUGHT IS ALREADY THE OTHER ANSWER TO THE SAME QUESTION.</b> They
    /// are mutually exclusive by construction, so a moment carrying both would be a bug
    /// this arm could not otherwise see.
    /// </remarks>
    public static readonly Code Sundered = new(Both, 1);

    /// <summary>How many statements back get their own band before the rest share one.</summary>
    /// <remarks>
    /// <b>THREE, WHICH IS THE SHALLOWEST DEPTH THE MEASURED WORLD NEEDS.</b> Two supporting
    /// facts is a named bAbI task, so a band for the latest and one for the one before it
    /// is the minimum that could express either — and everything older shares a code
    /// because a story is not bounded and an alphabet has to be.
    /// </remarks>
    public const int Bands = 3;

    /// <summary>The modality a category rides on.</summary>
    /// <remarks>
    /// <para>
    /// <b>ITS OWN, AND NOT <c>Naming.Meant</c>, BECAUSE THE TWO FOLD THE OPPOSITE
    /// WAY.</b> A minted name is present when ALL its members are, which is what makes it a
    /// name for a co-firing set; a category is present when ANY member is, which is what
    /// makes it a name for a set of alternatives. Sharing a modality would let a soundness
    /// check spell one out as the other.
    /// </para>
    /// <para>
    /// <b>AND IT IS A FRONT END SAYING WHAT IT IS LOOKING AT RATHER THAN WHAT TO CONCLUDE.</b>
    /// <i>This code is one of a set that never co-occurs and keeps one company</i> is
    /// arithmetic over what was seen, in the same licence <see cref="Codes.Coded.Groups"/>
    /// carries for <i>these codes were one object</i>. Which category matters, and to what,
    /// is left entirely to the learner.
    /// </para>
    /// </remarks>
    public const byte Sorted = 43;

    private readonly Joining _joining;
    private readonly IReadOnlyList<IReadOnlySet<Code>> _categories;
    private readonly int _hops;
    private readonly bool _banded;

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
    /// <b>An axis rather than a second arm, because the chain without it is the control that
    /// says whether the banding is what pays</b> — and the chain alone is already measured to
    /// raise what is in the room and not what is answered.
    /// </param>
    public Joined(
        Joining joining, IReadOnlyList<IReadOnlySet<Code>>? categories = null, int hops = 2,
        bool banded = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(hops, 1);

        _joining = joining;
        _categories = categories ?? [];
        _hops = hops;
        _banded = banded;
    }

    /// <inheritdoc/>
    public byte Modality => Both;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(Asking observation)
    {
        var said = new HashSet<Code>(observation.Words);
        said.UnionWith(observation.Question);

        if (_joining == Joining.Recent) return Sorting(Banding(said, observation));

        if (_joining == Joining.Distinguished) return Sorting(Situating(observation));

        if (_joining == Joining.Addressed) return Sorting(Addressing(said, observation));

        if (_joining == Joining.Chained) return Sorting(Chaining(said, observation));

        if (_joining == Joining.Bagged) return Sorting(said);

        // THE INTERSECTION IS TAKEN OVER THE HALVES AND NEVER OVER THE UNION, which reads
        // as pedantry until the union has already lost the distinction. Every code in the
        // bag is in the bag; only the two halves know which are in both.
        var shared = observation.Question.Where(observation.Words.Contains).ToList();

        // THE ONE ARM THAT SPEAKS WHEN THERE IS NOTHING TO SAY, which is its whole point.
        // Every other arm falls through to the plain bag here, and the bag is what cannot
        // be conditioned off.
        if (shared.Count == 0)
        {
            if (_joining == Joining.Either) said.Add(Sundered);
            return said;
        }

        if (_joining == Joining.Named) foreach (var one in shared) said.Add(new Code(Both, one.Value));
        else said.Add(Coincided);

        return Sorting(said);
    }

    /// <summary>
    /// The moment with a code added for every category any of whose members is in it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>ANY AND NEVER ALL, WHICH IS THE WHOLE DIFFERENCE FROM RUNG FIVE.</b> The members
    /// are alternatives and by construction never co-occur, so a fold demanding all of them
    /// would fire on nothing at all. <b>The plain code stays beside the category</b>, because
    /// emitting only the category would make <i>mary</i> and <i>john</i> the same word — and
    /// a general rule is worth having only while a particular one is still sayable, which is
    /// the specificity gradient this repo has been circling.
    /// </para>
    /// <para>
    /// <b>ONE PASS AND NOT A FIXED POINT, unlike <c>Naming.Fold</c>.</b> A category
    /// over categories is expressible and nothing mints one yet, so iterating would be
    /// machinery with no caller — and a loop that cannot turn twice is a loop written for a
    /// mechanism that does not exist.
    /// </para>
    /// </remarks>
    private HashSet<Code> Sorting(HashSet<Code> moment)
    {
        foreach (var category in _categories)
            foreach (var member in category)
                if (moment.Contains(member))
                {
                    moment.Add(Category(category));
                    break;
                }

        return moment;
    }

    /// <summary>What a category of these members is called, on every machine, forever.</summary>
    /// <param name="members">The alternatives it stands for, in any order.</param>
    /// <remarks>
    /// <b>DERIVED FROM THE MEMBERS AND NEVER FROM A POSITION IN A LIST.</b> Two front ends
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
    /// <b>NEWEST FIRST, WHICH IS THE ORDER THE WORLD ALREADY HANDS THEM OVER.</b> Walking
    /// backwards from the question is what makes <i>superseded</i> decidable in one pass: a
    /// statement is dead if something already kept claimed one of its keys, and everything
    /// already kept is newer than it by construction.
    /// </para>
    /// <para>
    /// <b>A DROPPED STATEMENT CLAIMS NOTHING, WHICH IS NOT A DETAIL.</b> If <i>Mary went to
    /// the kitchen</i> dies on <i>mary</i>, its <i>kitchen</i> must stay free — an older
    /// sentence about the kitchen was superseded by nothing, and letting a corpse claim keys
    /// would kill it. Only survivors write.
    /// </para>
    /// <para>
    /// <b>AND THE QUESTION'S WORDS GO IN UNCONDITIONALLY, because the question is not part
    /// of the situation.</b> It supersedes nothing and is superseded by nothing; it is what
    /// the situation is being asked about.
    /// </para>
    /// </remarks>
    private HashSet<Code> Situating(Asking observation)
    {
        var said = new HashSet<Code>(observation.Question);
        var claimed = new HashSet<Code>();
        var keys = new List<Code>();

        // THE BACKGROUND IS THE STORY'S OWN, and there is no other kind here any more. A
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
    /// <b>NEWEST FIRST MAKES THIS ONE PASS AND ONE STATEMENT.</b> The story arrives newest
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

        // NOTHING THE QUESTION NAMES WAS EVER SAID, so there is no store entry to read and
        // the bag is what is left. See the arm's remarks: going silent would score the
        // abstention rather than the mechanism.
        return said;
    }

    /// <summary>
    /// The question, and a chain of statements each found at the key the last one supplied.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>OLDER ONLY, BECAUSE THE CHAIN RUNS BACKWARDS IN TIME.</b> Whatever a statement
    /// mentions was established before it, so the search resumes past the statement just
    /// taken — and searching the whole story again would let anything later answer, which is
    /// the recency this arm exists to beat.
    /// </para>
    /// <para>
    /// <b>AND A CHAIN THAT RUNS OUT STOPS RATHER THAN FALLING BACK.</b> A hop with no key left
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

            // AND WHICH HOP FOUND IT, WHERE THE ARM ASKS FOR IT. A scope is a subset test
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

        // NOTHING THE QUESTION NAMES WAS EVER SAID, so there is no store entry to read at all
        // and the bag is what is left -- the same fallback Addressing takes, and for the same
        // reason: going silent would score the abstention rather than the mechanism.
        return read == 0 ? said : moment;
    }

    /// <summary>
    /// The words every statement of this story uses, which are the ones that key nothing.
    /// </summary>
    /// <remarks>
    /// <b>THE INTERSECTION AND NOT A COUNT, because a threshold is the thing that failed.</b>
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
    /// <b>THE QUESTION'S OWN WORDS ARE NOT BANDED, because they are not in the story and a
    /// band on them would say something false.</b> A depth is a distance from the question,
    /// so the question sits at no distance from itself.
    /// </remarks>
    private static HashSet<Code> Banding(HashSet<Code> said, Asking observation)
    {
        for (var back = 0; back < observation.Story.Count; back++)
        {
            // EVERYTHING PAST THE LAST BAND SHARES ITS CODE, so an alphabet stays finite
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
    /// <b>NOT THE ORDER OF THE WORDS, WHICH THIS WORLD HAS AND THIS ARM DELIBERATELY DOES
    /// NOT USE.</b> Sequence is rung three and is not built; handing an order to a matcher
    /// that cannot read one would be a number moving for a reason nobody could name.
    /// </remarks>
    public IReadOnlyDictionary<Code, int>? Order(Asking observation) => null;

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Fleeting(Asking observation) => null;

    /// <inheritdoc/>
    public Graph.Kind? Relating(Asking observation) => null;

    /// <inheritdoc/>
    public IReadOnlyDictionary<Code, int>? Filling(Asking observation) => null;

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Forced(Asking observation) => null;
}
