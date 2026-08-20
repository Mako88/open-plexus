using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;

namespace OpenPlexus.Bus;

/// <summary>What a holder is being asked for.</summary>
/// <remarks>
/// <para>
/// <b>One ask and not two, so the C3 ACCOUNTING IS ONE MECHANISM.</b> A vote and a count
/// merge are different arithmetic and the same exchange — scatter to every holder, gather
/// what comes back, act on whoever answered. Two payload pairs would be two places for
/// <i>how many did I ask</i> to be tracked, and the plan's own trap list is about a
/// mechanism that is local or population-wide by accident.
/// </para>
/// <para>
/// <b>And what is wanted travels with the question rather than being inferred from which
/// field is filled.</b> A reader that guessed would read an empty moment as <i>counts</i>
/// and a holder that fired nothing as the same thing, which is exactly the
/// silence-versus-absence conflation <see cref="Weights.Silent"/> exists to refuse.
/// </para>
/// </remarks>
public enum Wanted
{
    /// <summary>What this holder's commitments advocate about a moment.</summary>
    Vote,

    /// <summary>How often each code and each pair recurs across this holder's scopes.</summary>
    Counts,

    /// <summary>
    /// What the settlement said, so this holder can learn from it — <b>the only ask that
    /// is a telling.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It is here rather</b> than in a message of its own because the accounting is the
    /// same accounting. A settlement goes to every holder at once and each says what
    /// it did with it, which is scatter and gather — and a second payload pair would be a
    /// second place for <i>how many did I tell</i> to be tracked. The plan's own trap list
    /// is about a mechanism that is local or population-wide by accident.
    /// </para>
    /// <para>
    /// <b>And it carries the moment again rather than naming the vote it settles.</b> A
    /// holder that had to remember what it fired on would be holding state keyed by an ask
    /// that C2 permits never to be followed up, and a settlement arriving for a forgotten
    /// vote would be a round that silently taught nobody. Re-matching costs the match
    /// again — which is nine tenths of this machine's clock, said out loud as the price.
    /// </para>
    /// </remarks>
    Settle,
}

/// <summary>One holder's counts, as told to the others.</summary>
/// <remarks>
/// <para>
/// <b>Every holder's table goes to every holder</b>, and each drops its own row.
/// <see cref="Commitments.Population.Abstract"/> takes what OTHERS counted and adds it to
/// what it counts here, so a table that already included this machine would weigh its own
/// scopes twice — silently, plausibly, and in the direction that makes a redundancy look
/// better certified than it is.
/// </para>
/// <para>
/// <b>So the exclusion happens at the reader rather than the writer.</b> One broadcast
/// carrying N tables is one message; N broadcasts each carrying the merge of the other N-1
/// is N messages that differ, which is a scatter that has stopped being a broadcast. What
/// it costs is that every holder receives its own counts back and throws them away.
/// </para>
/// </remarks>
public readonly record struct Tabled
{
    /// <summary>Which holder counted this.</summary>
    public required MachineAddress From { get; init; }

    /// <summary>
    /// Which slot that holder is in — <b>fork 62, and what the exclusion above is actually
    /// keyed on.</b>
    /// </summary>
    /// <remarks>
    /// <b>Because a replica's own row arrives under somebody else's name.</b> Two machines
    /// in one slot hold the identical population, so the row one of them sent IS the other's
    /// row — and a reader dropping only what it recognises as its own would absorb a perfect
    /// copy of its own scopes and then certify a redundancy it is the sole evidence for.
    /// Under R=1 a slot is a holder and this is <see cref="From"/> spelt differently, which
    /// is why nothing changes where there are no replicas.
    /// </remarks>
    public required string Slot { get; init; }

    /// <summary>What recurs across its scopes.</summary>
    public required Counts Counted { get; init; }
}

/// <summary>
/// A question put to every holder at once, and never a call that waits for one.
/// </summary>
/// <remarks>
/// <para>
/// <b>Pushed rather than pulled, which is John's rule and also the plan's.</b> The obvious
/// build is a request whose response body is the answer, and it fails two ways at once:
/// <see cref="Posted"/> promises that a send does not wait on a receiver, and an awaited
/// request decides a missing holder by the client's timeout — which is <i>a miss decided by
/// a deadline</i>, a refutation this project already carries a revival row for. An ask goes
/// out, answers come back as their own messages, and nothing here holds a clock.
/// </para>
/// <para>
/// <b>And it is what lets a death be silence instead of a timeout.</b> C3 says a holder
/// vanishing mid-thought is normal; under a push it simply never answers, and a vote taken
/// over whoever did answer can come back with nothing at all. That silence is what
/// <c>Abstain</c> has been waiting for since it was written — see the plan's open defect,
/// where the third outcome reads zero because nothing in one process can die.
/// </para>
/// <para>
/// <b>Its depth is one round trip and that is fork 56'S PRICE</b>, not a claim about
/// blocking. Every holder is asked at once and every answer travels on its own, so the
/// scatter and the gather are one hop each however the bytes are carried.
/// </para>
/// </remarks>
public sealed record Ask
{
    /// <summary>
    /// Which ask this is, so an answer can be folded into the right gathering.
    /// </summary>
    /// <remarks>
    /// <b><see cref="BroadcastId"/> Rather than a second ID type of its own.</b> An ask is
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
    /// <b>Raw, and each holder folds it through its own names.</b> A minted name is
    /// reached by inference from the codes it stands for, so a moment folded by the asker
    /// would carry the asker's vocabulary onto machines that may not share it — and
    /// whether they share it is precisely what the counts merge exists to establish. See
    /// <see cref="Population.Moment"/>.
    /// </remarks>
    public ImmutableArray<Code> Moment { get; init; } = [];

    /// <summary>
    /// Which of those codes the source says will not come back. <b>Empty where it did not
    /// say.</b>
    /// </summary>
    /// <remarks>
    /// <b>It reaches the table and nothing else</b>, so a holder on an older build that
    /// ignored the field would hold a bigger table and take every identical decision. That
    /// is the property that makes it safe to send: a mark that could change a vote would be
    /// a world reaching into the brain over a wire.
    /// </remarks>
    public ImmutableArray<Code> Fleeting { get; init; } = [];

    /// <summary>
    /// What followed the moment, for a <see cref="Wanted.Settle"/> — <b>nothing where the
    /// settlement could not say, which is the third verdict.</b>
    /// </summary>
    public Code? Arrived { get; init; }

    /// <summary>
    /// Whether the vote missed, for a <see cref="Wanted.Settle"/>.
    /// </summary>
    /// <remarks>
    /// <b>The fleet's verdict and never a holder's, which is why it travels.</b> Genesis is
    /// gated on the population having no account of what happened, and a shard that had
    /// nothing to say about a moment the fleet answered correctly has not witnessed a
    /// failure. Letting each holder decide for itself would mint on every machine that
    /// happened to be quiet, which is ungated genesis arriving through the distribution.
    /// </remarks>
    public bool Wrong { get; init; }

    /// <summary>Whether this is a sweep round, for a <see cref="Wanted.Settle"/>.</summary>
    /// <remarks>
    /// <b>The calendar is the loop's and the sweep is the holder's.</b> A fleet whose
    /// members counted their own rounds would sweep at different moments, and rung five's
    /// evidence is the whole population at one instant.
    /// </remarks>
    public bool Sweeping { get; init; }

    /// <inheritdoc cref="Tabled"/>
    /// <remarks><b>Empty except on a sweep round.</b></remarks>
    public ImmutableArray<Tabled> Counted { get; init; } = [];
}

/// <summary>
/// What one holder says back — <b>everything C1 permits to leave a machine, and no more.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Neither field is a commitment</b>, and that is the whole of why this is allowed. A
/// <see cref="Weights"/> is expectations and weights already computed from the speaker's
/// own accuracy; a <see cref="Counts"/> is how often codes co-occurred across scopes. A
/// reader learns what is claimed and how often something recurred, and never what the
/// claimant is made of.
/// </para>
/// <para>
/// <b>Both are nullable and the ask says which to read.</b> A holder that fired nothing
/// answers with a <see cref="Weights"/> whose advocates are empty, which is
/// <see cref="Weights.Silent"/> and IS an answer; a holder that was never asked for a
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
    /// <b>So a gathering counts holders rather than messages.</b> Under C2 a message can
    /// arrive twice, and a merge that added the same holder's counts twice would weigh one
    /// machine's scopes double — which is silent, plausible, and exactly the shape of
    /// failure the wire format tests were written for.
    /// </remarks>
    public required MachineAddress From { get; init; }

    /// <summary>What its commitments advocate, for a <see cref="Wanted.Vote"/>.</summary>
    public Weights? Said { get; init; }

    /// <summary>What recurs across its scopes, for <see cref="Wanted.Counts"/>.</summary>
    public Counts? Counted { get; init; }

    /// <summary>What it added, for a <see cref="Wanted.Settle"/>.</summary>
    /// <remarks>
    /// <b>Three counts and not a commitment</b>, which is the same rule as everything else
    /// here. A reader learns how many rules a machine minted and never what any of them
    /// is. Without it a fleet's <c>Minted</c>, <c>Repaired</c> and <c>Subsumed</c> would all
    /// read nought while the machines behind it were learning — the shape of failure where
    /// a mechanism works and nothing can see that it did.
    /// </remarks>
    public Commitments.Learnt? Did { get; init; }
}
