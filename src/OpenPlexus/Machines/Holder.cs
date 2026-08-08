using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;

namespace OpenPlexus.Machines;

/// <summary>
/// A machine that holds commitments and answers what it is asked about them.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE FIRST THING IN THIS PROJECT THAT PUTS LEARNING ON A WIRE.</b> Fork 1 is open
/// because <c>LocalRendezvous</c> writes edges straight into locally-held clusters, so a
/// machine could think across a socket and not learn across one. What crosses here is what
/// the plan always said could: a count, and an expectation with a weight already computed.
/// </para>
/// <para>
/// <b>IT ANSWERS AND NEVER ASKS, WHICH IS WHAT KEEPS C1 STRUCTURAL RATHER THAN
/// CAREFUL.</b> Nothing on this class can read another holder's population, because
/// nothing on this class reaches off the machine at all — it is handed an
/// <see cref="Ask"/> and it posts an <see cref="Answer"/>. A holder that wanted to know
/// what its neighbours held would have to become an <see cref="Asker"/>, which is a
/// different object with a different address.
/// </para>
/// <para>
/// <b>AND THE MOMENT IS FOLDED HERE RATHER THAN BY THE ASKER.</b> A minted name is added
/// to a moment exactly when its members are present, so each holder folds through its own
/// <c>Naming</c> — which means a holder that has not yet learnt a name simply votes in the
/// longer vocabulary instead of receiving one it cannot interpret.
/// </para>
/// </remarks>
public sealed class Holder : IReceiveAsks
{
    private readonly Population _held;
    private readonly IBus _bus;

    /// <summary>
    /// One asker at a time reads the population.
    /// </summary>
    /// <remarks>
    /// <b>BECAUSE ASKS ARRIVE ON WHATEVER THREAD THE TRANSPORT CHOSE, AND A POPULATION IS
    /// NOT THREAD-SAFE.</b> Both buses dispatch deliveries concurrently on purpose, so two
    /// asks overlapping is the ordinary case and not the rare one. Reading a dictionary
    /// while another read walks it is the kind of fault that shows up as a corrupted
    /// answer rather than as an exception.
    /// </remarks>
    private readonly Lock _gate = new();

    /// <param name="address">Where this holder is asked.</param>
    /// <param name="held">What it holds.</param>
    /// <param name="bus">How its answers get back.</param>
    public Holder(MachineAddress address, Population held, IBus bus)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(bus);

        Address = address;
        _held = held;
        _bus = bus;
    }

    /// <inheritdoc/>
    public MachineAddress Address { get; }

    /// <summary>How many asks this holder has answered.</summary>
    /// <remarks>
    /// <b>REPORTED BECAUSE A HOLDER THAT WAS NEVER ASKED AND ONE THAT ANSWERED PERFECTLY
    /// LOOK ALIKE FROM THE MERGE.</b> A gathering counts what came back; only the holder
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
            };

            Answered++;
        }

        await _bus.SendAsync(ask.ReturnTo, answer, ct).ConfigureAwait(false);
    }

    /// <summary>What this holder's fired commitments have to say about a moment.</summary>
    /// <param name="ask">The question.</param>
    /// <remarks>
    /// <b>AN EMPTY TESTIMONY IS AN ANSWER AND NOT AN ABSENCE.</b> A holder none of whose
    /// commitments fired has been heard from, which the merge may not treat as a holder
    /// that died — see <see cref="Testimony.Silent"/>, and C3 for why the distinction is
    /// the whole point of asking.
    /// </remarks>
    private Testimony Speaking(Ask ask)
    {
        var moment = _held.Moment(new HashSet<Code>(ask.Moment));

        return _held.Speak(_held.Firing(moment));
    }
}
