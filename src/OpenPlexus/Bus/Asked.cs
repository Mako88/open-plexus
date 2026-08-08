using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Thinking;

namespace OpenPlexus.Bus;

/// <summary>What a holder is being asked for.</summary>
/// <remarks>
/// <para>
/// <b>ONE ASK AND NOT TWO, SO THE C3 ACCOUNTING IS ONE MECHANISM.</b> A vote and a count
/// merge are different arithmetic and the same exchange — scatter to every holder, gather
/// what comes back, act on whoever answered. Two payload pairs would be two places for
/// <i>how many did I ask</i> to be tracked, and the plan's own trap list is about a
/// mechanism that is local or population-wide by accident.
/// </para>
/// <para>
/// <b>AND WHAT IS WANTED TRAVELS WITH THE QUESTION rather than being inferred from which
/// field is filled.</b> A reader that guessed would read an empty moment as <i>counts</i>
/// and a holder that fired nothing as the same thing, which is exactly the
/// silence-versus-absence conflation <see cref="Testimony.Silent"/> exists to refuse.
/// </para>
/// </remarks>
public enum Wanted
{
    /// <summary>What this holder's commitments advocate about a moment.</summary>
    Vote,

    /// <summary>How often each code and each pair recurs across this holder's scopes.</summary>
    Counts,
}

/// <summary>
/// A question put to every holder at once, and never a call that waits for one.
/// </summary>
/// <remarks>
/// <para>
/// <b>PUSHED RATHER THAN PULLED, WHICH IS JOHN'S RULE AND ALSO THE PLAN'S.</b> The obvious
/// build is a request whose response body is the answer, and it fails two ways at once:
/// <see cref="Posted"/> promises that a send does not wait on a receiver, and an awaited
/// request decides a missing holder by the client's timeout — which is <i>a miss decided by
/// a deadline</i>, a refutation this project already carries a revival row for. An ask goes
/// out, answers come back as their own messages, and nothing here holds a clock.
/// </para>
/// <para>
/// <b>AND IT IS WHAT LETS A DEATH BE SILENCE INSTEAD OF A TIMEOUT.</b> C3 says a holder
/// vanishing mid-thought is normal; under a push it simply never answers, and a vote taken
/// over whoever did answer can come back with nothing at all. That silence is what
/// <c>Abstain</c> has been waiting for since it was written — see the plan's open defect,
/// where the third outcome reads zero because nothing in one process can die.
/// </para>
/// <para>
/// <b>ITS DEPTH IS ONE ROUND TRIP AND THAT IS FORK 56'S PRICE, not a claim about
/// blocking.</b> Every holder is asked at once and every answer travels on its own, so the
/// scatter and the gather are one hop each however the bytes are carried.
/// </para>
/// </remarks>
public sealed record Ask
{
    /// <summary>
    /// Which ask this is, so an answer can be folded into the right gathering.
    /// </summary>
    /// <remarks>
    /// <b><see cref="BroadcastId"/> RATHER THAN A SECOND ID TYPE OF ITS OWN.</b> An ask is
    /// a broadcast to every holder and an answer belongs to exactly one of them, which is
    /// what that type already means; minting a near-identical one is the clone this repo
    /// keeps a budget against.
    /// </remarks>
    public required BroadcastId Broadcast { get; init; }

    /// <summary>Where the answer goes.</summary>
    public required MachineAddress ReturnTo { get; init; }

    /// <inheritdoc cref="Wanted"/>
    public required Wanted Wants { get; init; }

    /// <summary>
    /// What is live, for a <see cref="Wanted.Vote"/>. <b>Empty for anything else.</b>
    /// </summary>
    /// <remarks>
    /// <b>RAW, AND EACH HOLDER FOLDS IT THROUGH ITS OWN NAMES.</b> A minted name is
    /// reached by inference from the codes it stands for, so a moment folded by the asker
    /// would carry the asker's vocabulary onto machines that may not share it — and
    /// whether they share it is precisely what the counts merge exists to establish. See
    /// <see cref="Population.Moment"/>.
    /// </remarks>
    public ImmutableArray<Code> Moment { get; init; } = [];
}

/// <summary>
/// What one holder says back — <b>everything C1 permits to leave a machine, and no more.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>NEITHER FIELD IS A COMMITMENT AND THAT IS THE WHOLE OF WHY THIS IS ALLOWED.</b> A
/// <see cref="Testimony"/> is expectations and weights already computed from the speaker's
/// own accuracy; a <see cref="Counts"/> is how often codes co-occurred across scopes. A
/// reader learns what is claimed and how often something recurred, and never what the
/// claimant is made of.
/// </para>
/// <para>
/// <b>BOTH ARE NULLABLE AND THE ASK SAYS WHICH TO READ.</b> A holder that fired nothing
/// answers with a <see cref="Testimony"/> whose advocates are empty, which is
/// <see cref="Testimony.Silent"/> and IS an answer; a holder that was never asked for a
/// vote answers with no testimony at all. Collapsing those two into one absent field is
/// the conflation C3 turns into a wrong number.
/// </para>
/// </remarks>
public sealed record Answer
{
    /// <summary>Which ask this answers.</summary>
    public required BroadcastId Broadcast { get; init; }

    /// <summary>Which holder is speaking.</summary>
    /// <remarks>
    /// <b>SO A GATHERING COUNTS HOLDERS RATHER THAN MESSAGES.</b> Under C2 a message can
    /// arrive twice, and a merge that added the same holder's counts twice would weigh one
    /// machine's scopes double — which is silent, plausible, and exactly the shape of
    /// failure the wire format tests were written for.
    /// </remarks>
    public required MachineAddress From { get; init; }

    /// <summary>What its commitments advocate, for a <see cref="Wanted.Vote"/>.</summary>
    public Testimony? Said { get; init; }

    /// <summary>What recurs across its scopes, for <see cref="Wanted.Counts"/>.</summary>
    public Counts? Counted { get; init; }
}
