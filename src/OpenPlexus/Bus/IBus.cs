using OpenPlexus.Thinking;

namespace OpenPlexus.Bus;

/// <summary>Something that can be handed envelopes. A cluster, in practice.</summary>
/// <remarks>
/// The bus takes this rather than a <c>Cluster</c> so that <b>it does not know
/// what a cluster is</b> — transport and graph stay separable, and the bus is
/// testable without a single node existing.
/// </remarks>
public interface IReceiveEnvelopes
{
    ClusterAddress Address { get; }

    Task DeliverAsync(Envelope envelope, CancellationToken ct = default);
}

/// <summary>Something that can be handed reports. A machine, in practice.</summary>
public interface IReceiveReports
{
    MachineAddress Address { get; }

    Task DeliverAsync(Report report, CancellationToken ct = default);
}

/// <summary>
/// Something that wants to hear about finished thoughts reaching codes it cares
/// about. <b>An output machine, in practice — fork 11.</b>
/// </summary>
/// <remarks>
/// <b>Separate from <see cref="IReceiveReports"/> on purpose.</b> A report is
/// mid-flight bookkeeping addressed to the one machine doing the accounting; this
/// is a finished result addressed to nobody in particular and routed by what it
/// reached. A machine can be both, and an output machine is only this.
/// </remarks>
public interface IReceiveArrivals
{
    MachineAddress Address { get; }

    Task DeliverAsync(Settled settled, CancellationToken ct = default);
}

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
    /// A cluster becomes reachable. Disposing the handle leaves the bus, and
    /// <b>leaving is not silent</b> — it fires a death event, which is the
    /// whole reason this is a bus rather than point-to-point sends.
    /// </summary>
    IDisposable Subscribe(IReceiveEnvelopes cluster);

    /// <summary>A machine becomes reachable, so reports can come back to it.</summary>
    IDisposable Subscribe(IReceiveReports machine);

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
    /// A machine asks to hear about finished thoughts that reached any of these
    /// codes — <b>fork 11, and the routing is by CODE rather than by address.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is what lets a second output machine exist.</b> The asker publishes
    /// once and does not know who listens; every machine that registered an
    /// interest gets the same finished thought, so N actuators act on one
    /// broadcast without a coordinator and without any of them holding the
    /// <see cref="Thought"/>.
    /// </para>
    /// <para>
    /// <b>The graph is not involved.</b> A node never learns that one of its codes
    /// means an action, which is what keeps an arbitrary actuator attachable.
    /// </para>
    /// </remarks>
    IDisposable Listen(IReceiveArrivals machine, IReadOnlyCollection<Codes.Code> codes);

    /// <summary>
    /// Get this envelope to that cluster. The thinking path, outbound.
    /// </summary>
    /// <remarks>
    /// <b>Returns before delivery happens.</b> A sender never waits on a
    /// receiver — that is what makes a fan-out to many clusters parallel rather
    /// than a queue.
    /// </remarks>
    ValueTask SendAsync(ClusterAddress to, Envelope envelope, CancellationToken ct = default);

    /// <summary>
    /// Put this envelope to every cluster at once.
    /// </summary>
    /// <remarks>
    /// <b>Returns who it went to</b>, because the sender has to know how many
    /// replies to expect — under a broadcast it cannot work that out from the
    /// ring, and the whole point is that it did not need an address.
    /// </remarks>
    /// <param name="envelope">The messages, one per origin code.</param>
    /// <param name="ct">Cancellation.</param>
    /// <param name="ready">
    /// Called with the clusters about to be asked, <b>before any of them is
    /// asked</b>. An origin has to record its thought inside this window: a
    /// cluster can report back before <c>BroadcastAsync</c> returns, and a
    /// report for an unknown broadcast is dropped.
    /// </param>
    ValueTask<IReadOnlyCollection<ClusterAddress>> BroadcastAsync(
        Envelope envelope,
        CancellationToken ct = default,
        Action<IReadOnlyCollection<ClusterAddress>>? ready = null);

    /// <summary>Get this report back to the machine that started the thought.</summary>
    ValueTask SendAsync(MachineAddress to, Report report, CancellationToken ct = default);

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
    /// Announce a finished thought to every machine listening for a code it
    /// reached — <b>fork 11.</b>
    /// </summary>
    /// <remarks>
    /// <b>Nothing happens if nobody is listening</b>, which is every measurement
    /// taken before this existed: publishing is what the harness used to do by
    /// direct call, and a run with no output machine simply has no listeners.
    /// </remarks>
    ValueTask PublishAsync(Settled settled, CancellationToken ct = default);

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
    /// <b>AND THE HOLE LEFT IS THE DEPARTURE THAT HAPPENS AFTER THE ASK ARRIVES.</b> A
    /// holder that took the question and died before answering is owed forever, correctly,
    /// because late and absent are one thing under C2 and nothing but a deadline separates
    /// them. Fork 62 is that half, and its condition is completeness rather than a clock.
    /// </para>
    /// </remarks>
    event Action<BroadcastId, MachineAddress>? Unreached;
}
