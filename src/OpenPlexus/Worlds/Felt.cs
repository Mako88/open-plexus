using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>
/// What a body's felt state is, <b>as something a run can count distinct copies
/// of.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE NUMBER IT FEEDS IS THE ONE THAT EXPLAINS STEP 4.</b> A policy cannot be
/// conditional on a state the graph cannot tell apart, and a credit cell keyed on
/// the state that earned it cannot cover states arriving faster than experience
/// does. Both readings are the count of DISTINCT felt states, so every embodied
/// world wants it and every one of them wrote it out itself.
/// </para>
/// <para>
/// <b>Modality and value, never the object's identity.</b> Two occasions are the
/// same state when the codes match, which is exactly what the graph can see —
/// anything finer would count states the graph has no way to distinguish, and
/// anything coarser would hide ones it can.
/// </para>
/// </remarks>
public static class Felt
{
    /// <summary>This felt state, as a comparable key.</summary>
    /// <param name="codes">What the body feels right now.</param>
    public static string Key(IEnumerable<Code> codes)
    {
        ArgumentNullException.ThrowIfNull(codes);

        return string.Join(",", codes.Select(code => $"{code.Modality}:{code.Value}"));
    }
}

/// <summary>
/// What an embodied run keeps about its body between steps — <b>the drive signal
/// and how many distinct states it has ever been in.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>EXTRACTED BECAUSE THE CLONE BUDGET CAUGHT IT.</b> Two bodies opened their
/// step loops with the same three lines — feel, key, record — and a third would
/// have written them again. <b>The two numbers belong together anyway</b>: the
/// credit says what the last transition was worth and the state count says how
/// many places that worth has to cover, and step 4's remaining gap is exactly the
/// ratio between them.
/// </para>
/// <para>
/// <b>The drive's reach is the world's own arithmetic and never a constant</b>, so
/// it is asked for rather than assumed — see <see cref="Learning.Drives"/>.
/// </para>
/// </remarks>
public sealed class Sensing
{
    private readonly Learning.Drives _drives;
    private readonly HashSet<string> _states = new(StringComparer.Ordinal);

    /// <param name="reach">
    /// The change in the most-at-risk variable that earns the full band.
    /// </param>
    public Sensing(double reach) => _drives = new Learning.Drives(reach);

    /// <inheritdoc cref="Learning.Drives.Credit"/>
    public double Credit => _drives.Credit;

    /// <inheritdoc cref="Learning.Drives.Improving"/>
    public double Improving => _drives.Improving;

    /// <summary>
    /// How many DISTINCT states the body has been in, as the graph sees them.
    /// </summary>
    /// <remarks>
    /// <b>A policy cannot be conditional on a state the graph cannot tell apart</b>,
    /// and a credit cell keyed on the state that earned it cannot cover states
    /// arriving faster than experience does.
    /// </remarks>
    public int States => _states.Count;

    /// <summary>
    /// Feel the body, and record where it is.
    /// </summary>
    /// <remarks>
    /// <b>Called once per step, BEFORE the occasion for that step is written.</b>
    /// The credit left behind belongs to the transition that just happened, so what
    /// it weights is the PREVIOUS occasion — an act is priced by what followed it.
    /// </remarks>
    /// <param name="at">Every internal variable's current value.</param>
    /// <param name="felt">What the body feels, as codes.</param>
    public void Note(IReadOnlyList<double> at, IEnumerable<Code> felt)
    {
        _drives.Feel(at);
        _states.Add(Felt.Key(felt));
    }
}
