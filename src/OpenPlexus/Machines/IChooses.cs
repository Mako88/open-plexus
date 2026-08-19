using OpenPlexus.Codes;

namespace OpenPlexus.Machines;

/// <summary>What decides which doing to take about a moment.</summary>
/// <remarks>
/// <para>
/// <b>A type rather than a delegate</b>, because a chooser has a moment's worth of state. A
/// world that takes several doings about one moment asks this more than once with the same
/// codes in front of it, so a chooser with no memory of what it just said returns that same
/// thing until the budget runs out. What was one call is now a conversation, and a
/// conversation needs somewhere to stand.
/// </para>
/// <para>
/// <b>It reads codes and never the world's own terms</b>, which is the seam it shares with
/// the brain. An oracle is a chooser that was handed the answer, a control is one that draws
/// uniformly, and a learner is one that reads a population — three arms over one interface
/// rather than three kinds of bench.
/// </para>
/// <para>
/// <b>And it names no world.</b> What it hands back is one of that world's doings as a
/// number, and which numbers mean what is the world's business.
/// </para>
/// </remarks>
public interface IChooses
{
    /// <summary>What to do about the state the world is in, or nothing to say no more.</summary>
    /// <param name="felt">The codes that state reads as.</param>
    /// <remarks>
    /// <b>Nothing ENDS the moment rather than skipping a turn</b>, which is what makes a
    /// budget a ceiling. A chooser out of things to say and a chooser that never had any are
    /// the same answer, and the world hears one quiet round either way.
    /// </remarks>
    int? Choose(IReadOnlyCollection<Code> felt);

    /// <summary>The moment is over, so whatever was remembered about it goes.</summary>
    /// <remarks>
    /// <para>
    /// <b>Called once a moment by whoever runs the loop</b>, which is the only place that
    /// knows where one moment stops. A chooser working the boundary out for itself would be
    /// comparing one moment's codes with the last one's, and two identical moments in a row
    /// are a thing a real stream does.
    /// </para>
    /// <para>
    /// <b>A chooser with nothing to forget does nothing here</b>, and that is a correct
    /// implementation rather than an inert one. Drawing uniformly is the same draw whether or
    /// not anything happened last time.
    /// </para>
    /// </remarks>
    void Cleared();
}

/// <summary>Choosers built out of something smaller.</summary>
public static class Chooses
{
    /// <summary>A chooser that is one function and remembers nothing.</summary>
    /// <param name="choosing">What to do about the codes in front of it.</param>
    /// <param name="cleared">
    /// What to forget at the end of a moment, or nothing where there is nothing to forget.
    /// </param>
    /// <remarks>
    /// <b>For the arms that genuinely are one expression</b> — a control drawing uniformly, an
    /// oracle reading the answer, a world that is acted in by nobody. Writing a type for each
    /// of those would be ceremony, and the state that made this an interface is state some
    /// choosers do not have.
    /// </remarks>
    public static IChooses From(Func<IReadOnlyCollection<Code>, int?> choosing, Action? cleared = null)
    {
        ArgumentNullException.ThrowIfNull(choosing);

        return new Function(choosing, cleared);
    }

    private sealed class Function(Func<IReadOnlyCollection<Code>, int?> choosing, Action? cleared)
        : IChooses
    {
        public int? Choose(IReadOnlyCollection<Code> felt) => choosing(felt);

        public void Cleared() => cleared?.Invoke();
    }
}
