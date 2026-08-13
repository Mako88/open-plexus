using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;

namespace OpenPlexus.Machines;

/// <summary>
/// A machine that holds commitments and answers what it is asked about them.
/// </summary>
/// <remarks>
/// <para>
/// <b>The first thing in this project that put learning on a wire.</b> The walk wrote its
/// edges straight into locally-held clusters, so a machine could think across a socket and
/// not learn across one — it was deleted with that still true. What crosses here is what
/// the plan always said could: a count, and an expectation with a weight already computed.
/// </para>
/// <para>
/// <b>It answers and never asks, which is what keeps C1 STRUCTURAL RATHER THAN
/// CAREFUL.</b> Nothing on this class can read another holder's population, because
/// nothing on this class reaches off the machine at all — it is handed an
/// <see cref="Ask"/> and it posts an <see cref="Answer"/>. A holder that wanted to know
/// what its neighbours held would have to become an <see cref="Asker"/>, which is a
/// different object with a different address.
/// </para>
/// <para>
/// <b>And the moment is folded here rather than by the asker.</b> A minted name is added
/// to a moment exactly when its members are present, so each holder folds through its own
/// <c>Naming</c> — which means a holder that has not yet learnt a name simply votes in the
/// longer vocabulary instead of receiving one it cannot interpret.
/// </para>
/// </remarks>
public sealed class Holder : IReceiveAsks
{
    private readonly Population _held;
    private readonly Alone _round;
    private readonly IBus _bus;

    /// <summary>Which slot this holder is in.</summary>
    /// <remarks>
    /// <b>Handed in like <see cref="Population.Places"/> and never announced, which is fork
    /// 62's one design decision.</b> A holder does not learn its partition from the wire and
    /// could not — the bus does not know what a population is, let alone that two of them
    /// are copies. It is told once, by whoever composed the fleet, and the only thing it
    /// does with it is decide which row of a swept table is its own.
    /// </remarks>
    private readonly string _slot;

    /// <summary>
    /// One asker at a time reads the population.
    /// </summary>
    /// <remarks>
    /// <b>Because asks arrive on whatever thread the transport chose, and a population is
    /// not thread-safe.</b> Both buses dispatch deliveries concurrently on purpose, so two
    /// asks overlapping is the ordinary case and not the rare one. Reading a dictionary
    /// while another read walks it is the kind of fault that shows up as a corrupted
    /// answer rather than as an exception.
    /// </remarks>
    private readonly Lock _gate = new();

    /// <param name="address">Where this holder is asked.</param>
    /// <param name="held">What it holds.</param>
    /// <param name="bus">How its answers get back.</param>
    /// <param name="slot">
    /// Which slot it is in, or nothing where the fleet is not partitioned — <b>a holder
    /// alone is a slot of one named after itself.</b>
    /// </param>
    public Holder(MachineAddress address, Population held, IBus bus, string? slot = null)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(bus);

        Address = address;
        _held = held;
        _round = new Alone(held);
        _bus = bus;
        _slot = slot ?? address.Value;
    }

    /// <inheritdoc/>
    public MachineAddress Address { get; }

    /// <summary>How many asks this holder has answered.</summary>
    /// <remarks>
    /// <b>Reported because a holder that was never asked and one that answered perfectly
    /// look alike from the merge.</b> A gathering counts what came back; only the holder
    /// can say whether it was reached at all, and the difference is which end of the wire
    /// a silence happened at.
    /// </remarks>
    public long Answered { get; private set; }

    /// <inheritdoc/>
    public async Task DeliverAsync(Ask ask, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ask);

        Answer answer;

        lock (_gate)
        {
            answer = new Answer
            {
                Broadcast = ask.Broadcast,
                From = Address,
                Said = ask.Wants == Wanted.Vote ? Speaking(ask) : null,
                Counted = ask.Wants == Wanted.Counts
                    ? Recurrence.Of(_held.All, _held.Dials).Written()
                    : null,
                Did = ask.Wants == Wanted.Settle ? Settling(ask) : null,
            };

            Answered++;
        }

        await _bus.SendAsync(ask.ReturnTo, answer, ct).ConfigureAwait(false);
    }

    /// <summary>What this holder's fired commitments have to say about a moment.</summary>
    /// <param name="ask">The question.</param>
    /// <remarks>
    /// <para>
    /// <b>An empty testimony is an answer and not an absence.</b> A holder none of whose
    /// commitments fired has been heard from, which the merge may not treat as a holder
    /// that died — see <see cref="Testimony.Silent"/>, and C3 for why the distinction is
    /// the whole point of asking.
    /// </para>
    /// <para>
    /// <b>And asking notes the moment, which is not a side effect so much as the point.</b>
    /// A code is counted live in the very moment genesis may be asked about it — see
    /// <c>Population.Witness</c> — and on a fleet the vote is the only thing that carries a
    /// moment to a holder before the settlement does. A holder that only noted what it was
    /// told to settle would judge every code by a table that had not seen it.
    /// </para>
    /// </remarks>
    private Testimony Speaking(Ask ask)
    {
        _round.Ask(new HashSet<Code>(ask.Moment));

        return _round.Said;
    }

    /// <summary>Learns from what the settlement said, and reports what that added.</summary>
    /// <param name="ask">The telling.</param>
    /// <remarks>
    /// <para>
    /// <b>It re-matches rather than remembering, and the cost is the match.</b> The ask
    /// carries the moment again, so this holder re-derives what fired instead of keeping
    /// state keyed by a vote C2 permits never to be followed up. Matching is nine tenths of
    /// this machine's clock and a distributed round now pays it twice; that is the price of
    /// a holder that cannot be left holding a settlement for a moment it has forgotten.
    /// </para>
    /// <para>
    /// <b>AND THE ROUND ITSELF IS <see cref="Alone"/>'S, which is the one thing this file
    /// must not reimplement.</b> Settle, sweep, repair, cover, repair — in that order,
    /// because the order is what makes a distributed run comparable with the hundred runs
    /// taken in one process. Two copies of a learning loop is the duplication that could
    /// silently start learning two different things, and it would arrive here.
    /// </para>
    /// <para>
    /// <b>A duplicate telling would be counted twice, and that is a named limit rather
    /// than a handled case.</b> TCP does not deliver a message twice within a connection,
    /// so <see cref="Posted"/> cannot show it; <see cref="HybridBus"/> can, and nothing
    /// here would notice. A monotone counter cannot retract, so what it would cost is a
    /// commitment believing itself more experienced than it is.
    /// </para>
    /// </remarks>
    private Learnt Settling(Ask ask)
    {
        _round.Ask(new HashSet<Code>(ask.Moment));

        Recurrence? heard = null;

        // Its own slot's row dropped, which is why every table goes to everyone. `Abstract`
        // adds what it is told to what it counts here, so a merge including this machine
        // would weigh its own scopes twice -- see `Tabled`. And it is the SLOT rather than
        // the address because a replica's own row arrives under its twin's name: two
        // machines fed one stream mint the same children and hold the same table, so
        // dropping only what this machine signed would absorb an exact copy of it.
        if (ask.Sweeping && ask.Counted.Length > 0)
        {
            heard = new Recurrence();

            foreach (var tabled in ask.Counted
                .Where(one => one.Slot != _slot)
                .OrderBy(one => one.From.Value, StringComparer.Ordinal))
                heard.Absorb(Recurrence.From(tabled.Counted));
        }

        return _round.Tell(ask.Arrived, ask.Wrong, ask.Sweeping, heard);
    }
}
