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

    private readonly Joining _joining;

    /// <param name="joining">What to do with the two halves.</param>
    public Joined(Joining joining) => _joining = joining;

    /// <inheritdoc/>
    public byte Modality => Both;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(Asking observation)
    {
        var said = new HashSet<Code>(observation.Story);
        said.UnionWith(observation.Question);

        if (_joining == Joining.Bagged) return said;

        // THE INTERSECTION IS TAKEN OVER THE HALVES AND NEVER OVER THE UNION, which reads
        // as pedantry until the union has already lost the distinction. Every code in the
        // bag is in the bag; only the two halves know which are in both.
        var shared = observation.Question.Where(observation.Story.Contains).ToList();

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
