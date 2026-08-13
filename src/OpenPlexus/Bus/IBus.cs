namespace OpenPlexus.Bus;

/// <summary>
/// Something that holds commitments and can be asked what it makes of them —
/// <b>fork 52, and the only traffic on this bus.</b>
/// </summary>
/// <remarks>
/// <b>THE BUS CARRIES WHAT WAS LEARNT AND NOTHING ELSE, WHICH IS FORK 1 SETTLING BY
/// DELETION.</b> The walk could think across a wire and could only learn at home, so it had
/// a second half here — envelopes into locally-held clusters, routes, reports, deaths. It is
/// gone, and what stayed is the half that was always the whole point: a holder is asked what
/// it makes of a moment and says so, so a fleet learns what one machine learns.
/// </remarks>
public interface IReceiveAsks
{
    MachineAddress Address { get; }

    Task DeliverAsync(Ask ask, CancellationToken ct = default);
}

/// <summary>
/// Something that asked, and is owed answers.
/// </summary>
/// <remarks>
/// <b>THE RETURN PATH IS A MESSAGE AND NOT A RESPONSE BODY.</b> An asker does not block on
/// its holders — see <see cref="Ask"/> for why that matters more here than anywhere else on
/// this bus — so an answer arrives as its own delivery, addressed to whoever asked and
/// correlated by the ask's own id. <i>Push, never pull</i>: an awaited response body is a
/// deadline by the back door, and a deadline is what C2 says cannot be trusted.
/// </remarks>
public interface IReceiveAnswers
{
    MachineAddress Address { get; }

    Task DeliverAsync(Answer answer, CancellationToken ct = default);
}

/// <summary>
/// How anything reaches anything else.
/// </summary>
/// <remarks>
/// <b>THE ADDRESSABLE THING IS A MACHINE, WHICH IS C3'S UNIT NOW THAT THE WALK IS GONE.</b>
/// The constraint is stated over a cluster because a cluster was what owned nodes and
/// forwarded routes; nothing here owns either. What can vanish mid-round is a holder, and
/// every write-off below is holder-shaped for that reason.
/// </remarks>
public interface IBus
{
    /// <summary>
    /// A holder becomes askable. <b>Disposing the handle is a death, and a death here is
    /// silence rather than an event.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THERE IS NO DEATH NOTICE, AND THE WALK'S IS WHAT SAYS WHY.</b> A route in flight
    /// toward a departed cluster was stranded and the origin could not write it off without
    /// being told, so the walk announced departures. An ask that reaches nobody costs an
    /// answer that never arrives, which is a thing the asker can see for itself by counting
    /// — and a holder that crashed could not have sent a notice anyway, so a design that
    /// needed one would work only for the departures that were polite.
    /// </para>
    /// <para>
    /// <b>AND WHAT THE ASKER COULD SEE FOR ITSELF WAS THE NUMERATOR, WHICH IS WHERE FORK 53
    /// SAT FOR A MONTH.</b> Counting the answers that came back tells a fleet it is missing
    /// somebody and never lets it stop waiting; the term that does is
    /// <see cref="Unreached"/>, which is the same silence observed one step earlier and from
    /// the sending end, where politeness is not required.
    /// </para>
    /// </remarks>
    IDisposable Subscribe(IReceiveAsks holder);

    /// <summary>An asker becomes reachable, so answers can come back to it.</summary>
    IDisposable Subscribe(IReceiveAnswers asker);

    /// <summary>
    /// Put this question to every holder at once — <b>the scatter half of fork 52.</b>
    /// </summary>
    /// <remarks>
    /// <b>RETURNS WHO WAS ASKED AND NEVER WHAT THEY SAID.</b> That is the whole difference
    /// between this and the request-and-wait build it replaced: an asker learns the
    /// denominator here and the numerator later, so a holder that never answers costs an
    /// answer rather than a timeout. What to do with a partial gathering is the caller's
    /// question, because only the caller knows what it asked for.
    /// </remarks>
    /// <param name="ask">The question.</param>
    /// <param name="ct">Cancellation.</param>
    /// <param name="ready">
    /// Called with the holders about to be asked, <b>before any of them is asked</b>. An
    /// asker has to record its gathering inside this window: a holder can answer before
    /// this method returns, and an answer to an ask nobody remembers is dropped.
    /// </param>
    ValueTask<IReadOnlyCollection<MachineAddress>> AskAsync(
        Ask ask,
        CancellationToken ct = default,
        Action<IReadOnlyCollection<MachineAddress>>? ready = null);

    /// <summary>Get this answer back to whoever asked. The gather half.</summary>
    ValueTask SendAsync(MachineAddress to, Answer answer, CancellationToken ct = default);

    /// <summary>
    /// One holder was never handed one ask, so no answer to that ask is owed from it —
    /// <b>fork 53, and it is the walk's write-off rather than a new idea.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>NOT A DEATH NOTICE, WHICH IS WHY IT NAMES AN ASK AND NOT JUST A MACHINE.</b> The
    /// walk's said a cluster was gone and every route into it stranded; this says one
    /// question never left. That is the smaller claim and the exact one — a holder that was
    /// not handed a question cannot answer it, and whether it is dead, wedged or merely
    /// behind a wire that lost this one message does not change that by a bit.
    /// </para>
    /// <para>
    /// <b>SO IT NEEDS NO POLITENESS AND NO CLOCK, WHICH IS WHAT THE ASYMMETRY ABOVE WAS
    /// RIGHT ABOUT AND WHAT IT LEFT UNFINISHED.</b> A holder that crashed sends nothing, and
    /// a refused connection is that arriving by a faster road than a timeout — the sender's
    /// own failure to hand over, observed by the sender. The asker counting for itself was
    /// the right instinct; what it could count was the numerator, and this is the term that
    /// lets the denominator come down.
    /// </para>
    /// <para>
    /// <b>AND THE HOLE LEFT IS THE DEPARTURE THAT HAPPENS AFTER THE ASK ARRIVES, WHICH IS
    /// FORK 62 AND IS NOT ON THIS BUS AT ALL.</b> A holder that took the question and died
    /// is owed forever, correctly — late and absent are one thing under C2 and nothing but a
    /// deadline separates them. What closes it is a partition into slots of R identical
    /// holders, handed to <c>Machines.Asker</c> by whoever composed the fleet. Announcing it
    /// here would teach the transport what a holder HOLDS, which is the separation this
    /// interface exists to keep.
    /// </para>
    /// </remarks>
    event Action<BroadcastId, MachineAddress>? Unreached;
}
