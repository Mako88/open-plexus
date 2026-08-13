namespace OpenPlexus.Bus;

/// <summary>
/// Something that holds commitments and can be asked what it makes of them —
/// <b>fork 52, and the only traffic on this bus that is LEARNING rather than thinking.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>EVERYTHING ELSE HERE CARRIES A THOUGHT ACROSS A WIRE AND NOTHING CARRIES WHAT WAS
/// LEARNT.</b> Fork 1 is open for exactly that reason: an occasion writes its edges into
/// locally-held clusters, so a machine can think across a wire and cannot learn across
/// one. A whole commitment learner now runs over this — see <c>Machines.Fleet</c> — so
/// what is left of that fork is the walk.
/// </para>
/// <para>
/// <b>SEPARATE FROM <see cref="IReceiveEnvelopes"/> BECAUSE A HOLDER IS NOT A CLUSTER.</b>
/// A cluster owns nodes and forwards routes; a holder owns commitments and answers
/// questions about them. They are addressed differently, they die differently, and the
/// only thing they share is a socket.
/// </para>
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
/// its holders — see <see cref="Ask"/> for why that matters more here than anywhere else
/// on this bus — so an answer arrives the way a <see cref="Report"/> does, addressed to
/// whoever asked and correlated by the ask's own id.
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
/// <b>The cluster subscribes, not the node.</b> A node is still reachable by
/// any broadcast; the cluster is the envelope, and that is what lets 200
/// partners across 12 clusters cost 12 sends.
/// </remarks>
public interface IBus
{
    /// <summary>
    /// A holder becomes askable. <b>Disposing the handle is a death, and a death here is
    /// silence rather than an event.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>UNLIKE A CLUSTER LEAVING, AND THE ASYMMETRY IS THE POINT.</b> A route in flight
    /// toward a dead cluster is stranded and the origin cannot write it off without being
    /// told, so <see cref="Deaths"/> exists. An ask that reaches nobody costs an answer
    /// that never arrives, which is a thing the asker can see for itself by counting —
    /// and a holder that crashed could not have sent a death notice anyway, so a design
    /// that needed one would work only for the departures that were polite.
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
    /// A cluster left. Routes that were heading into it are never coming back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Cluster granularity, not machine.</b> A route is stranded by the
    /// departure of whatever holds its next node, and that is a cluster; a
    /// machine leaving is every one of its clusters leaving.
    /// </para>
    /// <para>
    /// <b>What a thought should DO with this is not decided</b> — see open
    /// fork 5. A thought does not track which clusters its routes are sitting
    /// in, so it cannot tell whether a given death affects it.
    /// </para>
    /// </remarks>
    event Action<ClusterAddress>? Deaths;

    /// <summary>
    /// One holder was never handed one ask, so no answer to that ask is owed from it —
    /// <b>fork 53, and it is the walk's write-off rather than a new idea.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>NOT A DEATH NOTICE, WHICH IS WHY IT NAMES AN ASK AND NOT JUST A MACHINE.</b>
    /// <see cref="Deaths"/> says a cluster is gone and every route into it is stranded; this
    /// says one question never left. That is the smaller claim and the exact one — a holder
    /// that was not handed a question cannot answer it, and whether it is dead, wedged or
    /// merely behind a wire that lost this one message does not change that by a bit.
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
