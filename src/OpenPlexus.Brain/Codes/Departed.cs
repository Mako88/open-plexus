namespace OpenPlexus.Codes;

/// <summary>
/// The code that says one has just STOPPED being live — <b>negation, as an event.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Three states and only one of them happens.</b> A code may never have been here, may be
/// here, or may have been here and just left. The first is not an event: nothing observed it,
/// and a moment that carried a mark for every code the machine has never seen would hold an
/// entry per vocabulary item forever — which is the always-present code that genesis is
/// already forbidden to root on, arriving in the negative. The third is an event, and it is
/// the one a stream can report.
/// </para>
/// <para>
/// <b>A code rather than a channel beside the moment</b>, on rung three's reason and with the
/// same consequence. A fleet broadcasts a moment as a set of codes, so anything that has to
/// reach every holder must be one — a grouping cannot cross a wire and a precedence can. So a
/// departure is derived where the moment is FORMED, and matching, the tally, repair and the
/// wire are untouched.
/// </para>
/// <para>
/// <b>And what it buys is the propositional gap.</b> The population is already a disjunction
/// of conjunctions, so a scope that can name an absence makes it a disjunction of
/// conjunctions of literals — which says every Boolean function there is, and XOR needs
/// nothing of its own. That is the whole of what <i>it can say what does NOT hold</i> asks
/// for at this grain.
/// </para>
/// <para>
/// <b>What it does not buy is a negated CONCLUSION.</b> An expectation is one code, so
/// <i>X implies NOT Z</i> is unsayable however the scope is written — a departure reaches the
/// premise and never the claim. That is a separate gap with its own entry, and naming it here
/// is what stops this being read as the whole answer.
/// </para>
/// </remarks>
public static class Departed
{
    /// <summary>The modality a departure rides on.</summary>
    /// <remarks>
    /// <b>Its own, beside <see cref="Sequenced.Ordered"/> and <see cref="Intervened.Did"/></b>,
    /// and for their reason. A departure is DERIVED from a code and the moment before it
    /// rather than emitted by a sense, so a world able to produce one would be writing the
    /// learner's rules.
    /// </remarks>
    public const byte Left = 207;

    /// <summary>What the departure of a code is called, on every machine, forever.</summary>
    /// <param name="code">The code that has stopped being live.</param>
    /// <remarks>
    /// <b>Derived from the code alone</b>, so two machines watching one stream reach the same
    /// name with nothing to ask. <see cref="Hashing"/> rather than
    /// <see cref="object.GetHashCode"/>, which is randomised per process — a codebook resting
    /// on that would mean two machines quietly disagreeing about what a departure IS.
    /// </remarks>
    public static Code Of(Code code)
    {
        var hash = Hashing.Fold(Hashing.Basis, code.Modality);

        hash = Hashing.Fold(hash, code.Value);

        return new Code(Left, Hashing.Mix(hash));
    }

    /// <summary>Whether a code is a departure rather than one a front end emitted.</summary>
    /// <param name="code">The code to ask about.</param>
    /// <remarks>
    /// <b>The shape <see cref="Sequenced.Names"/> and <see cref="Intervened.Names"/> have</b>,
    /// and here for their reason: an operator needs to tell a derived code from a world's own
    /// without holding what derived it, which is what a fleet needs.
    /// </remarks>
    public static bool Names(Code code) => code.Modality == Left;

    /// <summary>Every departure between one moment and the next.</summary>
    /// <param name="last">What was live.</param>
    /// <param name="now">What is live.</param>
    /// <remarks>
    /// <para>
    /// <b>What LEFT and never what is missing</b>, which is the difference the three states
    /// are about. A code absent from both moments produces nothing, so the derivation costs
    /// what the world changed rather than what the vocabulary holds — and a stream that has
    /// gone still emits no departures at all rather than emitting the whole alphabet.
    /// </para>
    /// <para>
    /// <b>And a derived code is not itself watched for leaving.</b> A departure that departs
    /// is a claim about the derivation rather than about the world, and admitting one would
    /// let the alphabet grow a level every moment. So what is read is what a sense emitted,
    /// which is the same rule that stops a precedence being an argument to a precedence.
    /// </para>
    /// </remarks>
    public static IEnumerable<Code> From(IReadOnlySet<Code> last, IReadOnlySet<Code> now)
    {
        ArgumentNullException.ThrowIfNull(last);
        ArgumentNullException.ThrowIfNull(now);

        // Ordered, so two machines forming one moment walk it the same way. A set's order is
        // stable in a process and arbitrary across a merge, and a moment is compared by what
        // it holds -- fork 12's fault, and it arrives wherever a table is walked.
        foreach (var code in last.Order())
        {
            if (now.Contains(code)) continue;

            if (Names(code) || Sequenced.Names(code) || Intervened.Names(code)) continue;

            yield return Of(code);
        }
    }
}

/// <summary>
/// Whether a moment carries what has just stopped being live.
/// </summary>
/// <remarks>
/// <para>
/// <b>The mechanism for <i>it can say what does NOT hold</i></b>, and the arm is against
/// having no negation at all rather than against a second way of writing one. Every reading
/// this branch has taken was on a machine whose scopes are conjunctions of things PRESENT.
/// </para>
/// <para>
/// <b>And what it costs is the moment's width</b>, which is why it is a dial. A world whose
/// moment is a sentence has every word of the last sentence depart, so the moment roughly
/// doubles and genesis' candidate set with it — the shape that refuted deriving a code per
/// pair inside a thing. Whether an absence pays for that is a measurement and not an
/// argument.
/// </para>
/// </remarks>
public enum Departing
{
    /// <summary>Nothing is derived for leaving, and the moment before is not read.</summary>
    /// <remarks>
    /// <b>The control</b>, and every number recorded before this existed was taken under it.
    /// It has to be named to be one: a run that simply lacked the mechanism and a run that
    /// has it turned off are the same run, and only one of them is a comparison.
    /// </remarks>
    Never,

    /// <summary>A code live in the moment before and not in this one gets a departure.</summary>
    Left,
}
