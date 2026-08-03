namespace OpenPlexus.Worlds;

/// <summary>What several concurrent thoughts agreed on.</summary>
/// <typeparam name="T">Whatever a walk is being asked to name.</typeparam>
/// <param name="Chosen">The winner, or null if every vote was silent.</param>
/// <param name="Disagreed">
/// Whether the votes that had an opinion named more than one thing.
/// </param>
/// <remarks>
/// <b><paramref name="Disagreed"/> IS THE WIRING CHECK ON VOTING, AND IT IS THE
/// POINT OF MEASURING IT AT ALL.</b> "Voting changed nothing" is also exactly what
/// a disconnected dial looks like, so the outcome cannot be the check. False at
/// one vote by construction.
/// </remarks>
public readonly record struct Vote<T>(T? Chosen, bool Disagreed)
    where T : struct;

/// <summary>
/// Most votes wins, and silence does not get one.
/// </summary>
/// <remarks>
/// <para>
/// <b>It exists because delivery is concurrent</b>, so an identical question does
/// not always get an identical answer, and a single ask carries noise a result
/// could hide behind either way. Redundancy is the ordinary answer to C2, and it
/// costs queries rather than coordination.
/// </para>
/// <para>
/// <b>All three worlds had their own copy of this tally</b>, over three different
/// vote types, differing in nothing but the type. Voting is also the one place
/// where a subtle difference between the copies — counting silence, or breaking a
/// tie on arrival order — would move a headline number without failing anything.
/// </para>
/// </remarks>
public static class Majority
{
    /// <summary>
    /// Counts the votes that had an opinion and returns the most popular.
    /// </summary>
    /// <remarks>
    /// <b>A WALK THAT REACHED NOTHING HAS NO OPINION</b>, and counting it would
    /// let the quietest arm decide.
    /// <para>
    /// <b>Ties break on the value itself</b>, so the answer does not depend on
    /// which thought happened to finish first — which is the very thing being
    /// voted on.
    /// </para>
    /// </remarks>
    public static Vote<T> Of<T>(IEnumerable<T?> votes)
        where T : struct, IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(votes);

        var tally = new Dictionary<T, int>();

        foreach (var vote in votes)
            if (vote is { } opinion) tally[opinion] = tally.GetValueOrDefault(opinion) + 1;

        if (tally.Count == 0) return new Vote<T>(null, false);

        var chosen = tally
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key)
            .First().Key;

        // NOT UNANIMOUS AMONG THE VOTES THAT HAD AN OPINION. One distinct answer
        // means every walk that reached anything reached the same thing.
        return new Vote<T>(chosen, tally.Count > 1);
    }
}
