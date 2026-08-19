using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Commitments;

/// <summary>
/// A scope entry that names no argument, and the matcher it forces — <b>rung four,
/// priced before the ladder's escalation policy rather than after it. Fork 33.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Rungs one to three keep a subset test and this one does not.</b>
/// <see cref="Commitment.Fires"/> asks whether every code of a scope is in the moment,
/// which is a membership test per code and nothing else. An entry saying <i>whichever
/// code of this modality, and the SAME one wherever this entry repeats</i> cannot be
/// answered that way: what it matches has to be chosen, and the choice has to be
/// consistent across the entries sharing a name. That is unification, and it is the
/// distance between a propositional learner and a relational one.
/// </para>
/// <para>
/// <b>And an entry is still a <see cref="Code"/>, which is the one thing worth keeping
/// from the cheap matcher.</b> A scope stays an array of codes, so identity, ordering,
/// subsumption and everything that reads a scope carry over unchanged. What varies is
/// carried in the modality, exactly as <see cref="Naming.Meant"/> carries a name.
/// </para>
/// <para>
/// <b>One entry naming a variable constrains nothing, which is why the cost is not
/// obvious.</b> <i>Whichever word</i> alone is satisfied by any moment holding a word at
/// all. It is the REPETITION that says something — <i>whichever word was asked about,
/// and that same word was told</i> — so the shape whose cost matters is a join, and a
/// join is what has no index.
/// </para>
/// <para>
/// <b>Which is the half the front end has to supply and on text does not.</b>
/// <see cref="Codes.Joined"/> unions the question's words into the story's bag under one
/// modality, so <i>asked</i> and <i>told</i> are the same code and a repeated entry
/// binds against nothing. A front end computing that join with the identity thrown away was
/// measured and is deleted; keeping the identity is what a variable is, and it needs the two
/// halves to be tellable apart in the moment.
/// </para>
/// </remarks>
public static class Unifying
{
    /// <summary>The modality a scope entry naming no argument lives in.</summary>
    /// <remarks>
    /// <b>Its own, beside <see cref="Commitment.Committed"/> and
    /// <see cref="Naming.Meant"/>.</b> A pattern is not a code the world can emit and must
    /// never be confused for one: a moment holding a pattern would make a scope match
    /// itself, and a front end that could emit one would be writing the learner's rules.
    /// </remarks>
    private const byte Whatever = 209;

    /// <summary>
    /// The entry meaning <i>whichever code of this modality, called <paramref name="name"/>.</i>
    /// </summary>
    /// <param name="modality">What kind of code may fill it.</param>
    /// <param name="name">
    /// Which variable this is. <b>Two entries carrying the same name must be filled by the
    /// same value</b>, and that is the whole of what unification adds.
    /// </param>
    /// <remarks>
    /// <b>The modality rides in the high half and the name in the low one</b>, so a pattern
    /// is derived from what it says rather than counted out — two machines reaching the
    /// same scope write the same entry with nothing to ask, which is the property every
    /// code here has.
    /// </remarks>
    public static Code Any(byte modality, int name)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(name);

        return new Code(Whatever, ((ulong)modality << 32) | (uint)name);
    }

    /// <summary>Whether this entry names a variable rather than a code.</summary>
    /// <param name="entry">The scope entry to ask about.</param>
    public static bool Names(Code entry) => entry.Modality == Whatever;

    /// <summary>
    /// The values present in a moment, by modality — <b>the per-round index a subset test
    /// never needed.</b>
    /// </summary>
    /// <param name="moment">What is live.</param>
    /// <remarks>
    /// <b>Built once a round and not once a commitment, which is where the honest
    /// accounting of this rung starts.</b> The cheap matcher reads the moment as a set and
    /// pays nothing to prepare it; a variable has to enumerate what could fill it, and
    /// enumerating means grouping. So the rung costs one pass over the moment before any
    /// commitment is looked at, and the probe reports it separately for that reason.
    /// </remarks>
    public static Dictionary<byte, List<ulong>> Index(IReadOnlySet<Code> moment)
    {
        ArgumentNullException.ThrowIfNull(moment);

        var index = new Dictionary<byte, List<ulong>>();

        foreach (var code in moment)
        {
            if (!index.TryGetValue(code.Modality, out var values))
                index[code.Modality] = values = [];

            values.Add(code.Value);
        }

        // Sorted, because the first binding found decides and a dictionary's order does
        // not survive a run. A matcher whose answer moved with the iteration order would
        // be a difference nobody chose, which is what `DeterminismTests` exists to refuse.
        foreach (var values in index.Values) values.Sort();

        return index;
    }

    /// <summary>What a unifying match did, and what it cost to find out.</summary>
    /// <param name="Fired">Whether some consistent filling of every variable was found.</param>
    /// <param name="Tried">
    /// How many candidate values were considered. <b>The number fork 33 wants</b> — a
    /// subset test tries exactly one thing per scope code, so this against the scope's
    /// length is the whole comparison.
    /// </param>
    /// <param name="Binding">
    /// What each variable was filled with, by name, or empty where nothing fired.
    /// </param>
    public readonly record struct Bound(bool Fired, int Tried, ImmutableArray<ulong> Binding);

    /// <summary>Whether a scope with variables in it is satisfied, and at what cost.</summary>
    /// <param name="scope">Codes that must be present, and entries that name a variable.</param>
    /// <param name="moment">What is live.</param>
    /// <param name="index">The moment's values by modality, from <see cref="Index"/>.</param>
    /// <remarks>
    /// <para>
    /// <b>The constants are tested first and that is not an optimisation.</b> A scope
    /// mixing constants with variables is indexable by its constants exactly as it is now,
    /// so the population's <c>_byCode</c> still reaches it — and a scope of variables
    /// ALONE is reached by nothing and has to be scanned. Which scopes are all-variable is
    /// therefore the fact that decides whether this rung keeps the index or loses it.
    /// </para>
    /// <para>
    /// <b>The first consistent filling wins, which is a decision and not a detail.</b> A
    /// variable scope can be satisfied several ways in one moment, and each way is a
    /// different grounded expectation. Taking the first in code order keeps the matcher
    /// decidable and order-independent; what a vote should do with the others is the
    /// question this probe deliberately does not answer, because it is about worth rather
    /// than cost.
    /// </para>
    /// </remarks>
    public static Bound Fires(
        ImmutableArray<Code> scope, IReadOnlySet<Code> moment, Dictionary<byte, List<ulong>> index)
    {
        ArgumentNullException.ThrowIfNull(moment);
        ArgumentNullException.ThrowIfNull(index);

        if (scope.IsDefaultOrEmpty) return new Bound(false, 0, []);

        var names = 0;

        foreach (var entry in scope)
        {
            if (!Names(entry))
            {
                if (!moment.Contains(entry)) return new Bound(false, 0, []);

                continue;
            }

            names = Math.Max(names, (int)(entry.Value & uint.MaxValue) + 1);
        }

        if (names == 0) return new Bound(true, 0, []);

        var filling = new ulong[names];
        var filled = new bool[names];
        var tried = 0;

        var fired = Fill(scope, moment, index, filling, filled, 0, ref tried);

        return new Bound(fired, tried, fired ? [.. filling] : []);
    }

    /// <summary>Fills one variable and recurses, backtracking where nothing consistent fits.</summary>
    /// <param name="scope">The scope being matched.</param>
    /// <param name="moment">What is live.</param>
    /// <param name="index">The moment's values by modality.</param>
    /// <param name="filling">What each variable has been filled with so far.</param>
    /// <param name="filled">Which variables have been filled.</param>
    /// <param name="name">Which variable this call is filling.</param>
    /// <param name="tried">Candidate values considered, accumulated across the search.</param>
    /// <remarks>
    /// <b>In name order rather than by smallest candidate set.</b> And said out loud because
    /// it is a ceiling on the reading. Choosing the most constrained variable first is
    /// the standard saving and it would make this cheaper than it is measured to be. The
    /// probe wants the cost of the rung rather than the cost of one implementation of it,
    /// so the number reported is an upper bound, and a real matcher may beat it.
    /// </remarks>
    private static bool Fill(
        ImmutableArray<Code> scope,
        IReadOnlySet<Code> moment,
        Dictionary<byte, List<ulong>> index,
        ulong[] filling,
        bool[] filled,
        int name,
        ref int tried)
    {
        if (name == filling.Length) return true;

        // Where this variable may be drawn from. Any entry naming it will do -- they all
        // have to agree in the end, so starting from the first is a choice about order and
        // never about which answers exist.
        byte modality = 0;
        var found = false;

        foreach (var entry in scope)
        {
            if (!Names(entry) || (int)(entry.Value & uint.MaxValue) != name) continue;

            modality = (byte)(entry.Value >> 32);
            found = true;

            break;
        }

        // A name nothing mentions is satisfied by anything, which can only happen where a
        // scope skips a number, and is answered rather than refused so that the search
        // never depends on how the names were counted out.
        if (!found)
        {
            filled[name] = true;

            return Fill(scope, moment, index, filling, filled, name + 1, ref tried);
        }

        if (!index.TryGetValue(modality, out var candidates)) return false;

        foreach (var value in candidates)
        {
            tried++;

            filling[name] = value;
            filled[name] = true;

            if (!Agrees(scope, moment, filling, filled)) continue;

            if (Fill(scope, moment, index, filling, filled, name + 1, ref tried)) return true;
        }

        filled[name] = false;

        return false;
    }

    /// <summary>Whether every filled entry of a scope is present under the current filling.</summary>
    /// <param name="scope">The scope being matched.</param>
    /// <param name="moment">What is live.</param>
    /// <param name="filling">What each variable has been filled with so far.</param>
    /// <param name="filled">Which variables have been filled.</param>
    private static bool Agrees(
        ImmutableArray<Code> scope, IReadOnlySet<Code> moment, ulong[] filling, bool[] filled)
    {
        foreach (var entry in scope)
        {
            if (!Names(entry)) continue;

            var name = (int)(entry.Value & uint.MaxValue);

            if (!filled[name]) continue;

            if (!moment.Contains(new Code((byte)(entry.Value >> 32), filling[name]))) return false;
        }

        return true;
    }
}
