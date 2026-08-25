using OpenPlexus.Codes;

namespace OpenPlexus.Machines;

/// <summary>
/// A chooser that says what the machine BELIEVES, and hands over where it believes nothing.
/// </summary>
/// <param name="brain">Whose belief is read.</param>
/// <param name="saying">
/// Which doing says an expected outcome, or nothing where none does. <b>The world's business
/// and never this one's</b> — an outcome is a number in the brain's own alphabet and a doing
/// is a number in the world's, and a chooser that assumed the two were the same would be one
/// world's arrangement wearing an interface's clothes.
/// </param>
/// <param name="otherwise">What decides the rounds it has nothing to say about.</param>
/// <remarks>
/// <para>
/// <b>The other half of a conversation.</b> A person asks the machine something and wants
/// what the machine holds; <see cref="Drives"/> ranks what to say by how much saying it would
/// TEACH, which is a question about the population rather than an answer to anybody. So a
/// machine with only a drive is one nobody can ask anything, and <i>shows it understood by
/// answering on it</i> is the first north star's own words.
/// </para>
/// <para>
/// <b>Read-only, which is what makes it safe to ask.</b> <see cref="Brain.Voting"/> mints
/// nothing and settles nothing, so consulting it is not the machine having learnt something.
/// It is also the one road <c>Supposing</c>'s second hop has to a chooser: the vote it
/// returns is the one a supposition reached.
/// </para>
/// <para>
/// <b>Belief first and the fallback after</b>, rather than the two weighed against each
/// other. There is no scale on which <i>how sure I am</i> and <i>how much this would teach
/// me</i> trade, and inventing one would be a number nobody chose deciding what the machine
/// says. What bounds the belief is that it is said ONCE about a moment.
/// </para>
/// <para>
/// <b>And it is a REQUIREMENT rather than an arm.</b> Answering from what is understood is an
/// entry of THE ARCHITECTURE, so a losing reading is a cost to record rather than grounds to
/// delete it. What it owes instead is a check that can fail — that it CHANGED what the
/// machine said, an arm whose belief was always the drive's own pick being the drive under a
/// second name. <c>RoamingTests</c> holds it, beside what the belief costs the exam.
/// </para>
/// </remarks>
internal sealed class Answers(Brain brain, Func<Code, int?> saying, IChooses otherwise)
    : IChooses
{
    // What it has already said about the moment on the table. The population does not move
    // between two calls, so without this a world that takes several doings a moment hears the
    // same belief until the budget is gone. `Drives` carries the same set for the same reason,
    // and the two are separate on purpose: what the drive may still say is not decided by what
    // the belief already said.
    private readonly HashSet<int> _already = [];

    /// <summary>Rounds it had a belief about the moment and said it.</summary>
    /// <remarks>
    /// <b>Reported beside anything read off this</b>, because a chooser that believed nothing
    /// all run is its fallback wearing a second name — and no score can tell those apart.
    /// </remarks>
    public long Said { get; private set; }

    /// <summary>Rounds it believed nothing and the fallback decided.</summary>
    public long Quiet { get; private set; }

    /// <inheritdoc/>
    public int? Choose(IReadOnlyCollection<Code> felt)
    {
        if (Believed(felt) is { } word && _already.Add(word))
        {
            Said++;

            return word;
        }

        Quiet++;

        return otherwise.Choose(felt);
    }

    /// <summary>The doing that says what this machine expects, or nothing.</summary>
    /// <param name="felt">The moment it is looking at.</param>
    private int? Believed(IReadOnlyCollection<Code> felt) =>
        brain.Voting(felt).Expects is { } expects ? saying(expects) : null;

    /// <inheritdoc/>
    public void Cleared()
    {
        _already.Clear();

        otherwise.Cleared();
    }
}
