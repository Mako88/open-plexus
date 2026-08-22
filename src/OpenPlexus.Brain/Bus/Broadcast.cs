namespace OpenPlexus.Bus;

/// <summary>Which broadcast a message belongs to.</summary>
/// <remarks>
/// <para>
/// Without this, <b>two questions in flight mix their chains and their death counts.</b>
/// Under continuous input there are always several in flight, so this is not optional.
/// </para>
/// <para>
/// <b>It lives with the bus and not with a learner</b>, which is where it was. It was
/// written for the walk and sat in <c>Thinking</c>, and by the time anybody looked the
/// commitment fleet depended on it across five files — <see cref="Ask"/>,
/// <see cref="IBus"/>, <see cref="HybridBus"/>, <see cref="Posted"/> and
/// <c>Machines.Asker</c>. Correlating a reply with the ask that caused it is a fact about
/// the transport, so this is the home it should have had: nothing about it is about how
/// anything learns.
/// </para>
/// </remarks>
internal readonly record struct BroadcastId(Guid Value)
{
    /// <summary>
    /// A fresh id, minted without asking anyone.
    /// </summary>
    /// <remarks>
    /// <b>C1 forbids a counter.</b> Any shared sequence would need every machine to agree
    /// on what comes next, so this is a value large enough that independent machines do
    /// not collide by accident.
    /// </remarks>
    public static BroadcastId New() => new(Guid.NewGuid());
}
