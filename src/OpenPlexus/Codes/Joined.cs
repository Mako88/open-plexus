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
    /// Only the statements nothing newer has superseded — <b>a situation instead of a
    /// transcript.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE SELECTION PROBLEM DOES NOT GET SOLVED HERE, IT GETS DISSOLVED.</b> Every arm
    /// before this one hands the learner a bag with two places in it and some way of hinting
    /// which is meant — a narrower view, a recency band, a coincidence marker. This asks a
    /// different question: <i>Mary went to the kitchen</i> is not a fact to be found later,
    /// it is an instruction to overwrite where Mary is. If the overwriting happens before the
    /// bag is built there is only ever ONE place for Mary in it, and the near-perfect reader
    /// this world has already measured reads it perfectly.
    /// </para>
    /// <para>
    /// <b>SUPERSEDED MEANS SHARING A KEY WITH SOMETHING NEWER, AND A KEY IS ANY WORD THE
    /// CORPUS DOES NOT USE CONSTANTLY.</b> Walking the story newest first, a statement is
    /// dropped when a statement already kept used one of its words. <i>Mary went to the
    /// garden</i> kills <i>Mary went to the kitchen</i> on <i>mary</i>, and leaves <i>John
    /// moved to the garden</i> standing because the only words those two share are the ones
    /// every sentence shares.
    /// </para>
    /// <para>
    /// <b>WHICH IS WHY THE COMMONEST WORDS ARE EXCLUDED, AND WHY THAT EXCLUSION IS A DIAL
    /// RATHER THAN A LIST.</b> No stop list, no parser and no notion that <i>mary</i> is a
    /// person: the join is told how often each word is written and nothing else, which is
    /// the same measured proxy for <i>informative</i> that <see cref="Worlds.Predicting.Salient"/>
    /// already stands on.
    /// </para>
    /// <para>
    /// <b>AND THE DIAL'S TWO ENDS ARE THE TWO CONTROLS, WHICH IS WHAT STOPS THIS BEING A
    /// FREE WIN.</b> Exclude nothing and every sentence shares <i>the</i> with every other,
    /// so only the newest survives and the arm IS a one-statement span. Exclude everything
    /// and no statement has a key, so nothing is superseded and the arm IS
    /// <see cref="Bagged"/>. Anything this buys has to be bought in the middle, against both
    /// of its own ends.
    /// </para>
    /// <para>
    /// <b>IT IS A FRONT END DOING A SITUATION MODEL'S WORK, WHICH IS AN UPPER BOUND AND NOT
    /// THE MECHANISM.</b> A displacement rule computed once over a story that arrived whole
    /// is not a store that survives a stream, cannot be wrong about anything, and earns no
    /// blame — so what this can say is whether holding one state per thing is worth having
    /// at all. If it is not, nothing built inside the learner would have been either.
    /// </para>
    /// </remarks>
    Situated,

    /// <summary>
    /// The same displacement, keyed on what this story does not share — <b>no corpus
    /// statistic, no dial, and no threshold to straddle.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>BECAUSE FREQUENCY CANNOT SEPARATE A VERB FROM A NAME HERE, AND THAT IS
    /// ARITHMETIC RATHER THAN BAD LUCK.</b> <see cref="Situated"/> cuts the vocabulary at a
    /// rank, and on this corpus <i>went</i> is written more often than any of the four
    /// people while <i>journeyed</i>, <i>travelled</i> and <i>moved</i> are written less
    /// than all of them. The verbs straddle the names, so no rank keeps the names as keys
    /// and drops the verbs — and a shared <i>went</i> supersedes statements about different
    /// people.
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
    /// it a key again, so a story mixing its verbs supersedes across people exactly as
    /// <see cref="Situated"/> does. The ceiling column says how often that happens without
    /// anything having to learn.
    /// </para>
    /// </remarks>
    Distinguished,
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

    private readonly Joining _joining;
    private readonly HashSet<Code> _constant;

    /// <param name="joining">What to do with the two halves.</param>
    /// <param name="frequency">
    /// How often the corpus writes each word, which only <see cref="Joining.Situated"/>
    /// reads. <b>A count and never a meaning</b> — see <see cref="Worlds.Recalled.Frequency"/>.
    /// </param>
    /// <param name="constant">
    /// How many of the commonest words are too constant to key on.
    /// </param>
    /// <remarks>
    /// <b>THE COMMONEST SET IS BUILT ONCE HERE RATHER THAN PER MOMENT.</b> It is a fact
    /// about the corpus, so recomputing it per question would be the same answer at the
    /// price of a sort a moment — and this runs on every round of every arm.
    /// <b>Ties break on <see cref="Code"/> order</b>, because a set whose membership
    /// depended on a dictionary's walk would make two runs of one seed disagree about
    /// what the front end emitted.
    /// </remarks>
    public Joined(
        Joining joining,
        IReadOnlyDictionary<Code, int>? frequency = null,
        int constant = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(constant);

        _joining = joining;

        _constant = frequency is null
            ? []
            : [.. frequency
                .OrderByDescending(one => one.Value)
                .ThenBy(one => one.Key)
                .Take(constant)
                .Select(one => one.Key)];
    }

    /// <inheritdoc/>
    public byte Modality => Both;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(Asking observation)
    {
        var said = new HashSet<Code>(observation.Words);
        said.UnionWith(observation.Question);

        if (_joining == Joining.Recent) return Banding(said, observation);

        if (_joining is Joining.Situated or Joining.Distinguished)
            return Situating(observation);

        if (_joining == Joining.Bagged) return said;

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

        return said;
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

        // WHERE THE BACKGROUND COMES FROM IS THE WHOLE DIFFERENCE BETWEEN THE TWO ARMS, and
        // it is one line because the displacement below is identical. One is handed a rank
        // over the corpus; the other works out, from this story alone, which words every
        // sentence in it uses.
        var constant = _joining == Joining.Distinguished ? Shared(observation) : _constant;

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
