namespace OpenPlexus.Bus;

/// <summary>
/// A subscription handle that runs one action once.
/// </summary>
/// <remarks>
/// <b>Once</b>, because a double dispose is ordinary and a double departure is not. A
/// <see langword="using"/> inside a method that also disposes on a path out is normal C#;
/// firing the action twice would announce a death that already happened, and on this bus a
/// second departure notice for something that has since come back would remove it again.
/// </remarks>
/// <param name="going">What to do when the handle is dropped.</param>
internal sealed class Leaves(Action going) : IDisposable
{
    private bool _gone;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_gone) return;

        _gone = true;
        going();
    }
}

/// <summary>
/// Putting something in a table under its address, and taking it out again.
/// </summary>
/// <remarks>
/// <b>Extracted because the duplication budget refused the second copy</b>, and it was
/// right. Both buses gained the same two subscriptions in one edit and wrote them the
/// same way twice — which is the shape that drifts: one bus removing on departure and the
/// other not would be a holder that answers after it has left, on one transport only, and
/// nothing would say so.
/// </remarks>
internal static class Joins
{
    /// <summary>Holds one until the handle is dropped.</summary>
    /// <typeparam name="T">What is being held.</typeparam>
    /// <param name="gate">The table's lock.</param>
    /// <param name="table">Where it goes.</param>
    /// <param name="address">What it is called.</param>
    /// <param name="one">The thing itself.</param>
    /// <returns>The handle that removes it.</returns>
    public static IDisposable At<T>(
        Lock gate, Dictionary<MachineAddress, T> table, MachineAddress address, T one)
    {
        lock (gate) table.Add(address, one);

        return new Leaves(() => { lock (gate) table.Remove(address); });
    }
}
