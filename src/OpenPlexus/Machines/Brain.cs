using OpenPlexus.Codes;
using OpenPlexus.Commitments;

namespace OpenPlexus.Machines;

/// <summary>What the brain did about one moment.</summary>
/// <remarks>
/// <para>
/// <b>An answer rather than a prediction</b>, which is what the seam changes. A pull asked
/// the population what would follow and the loop did the rest; a push hands it a moment and
/// gets back everything that moment cost — what is expected, and what the settlement added.
/// </para>
/// <para>
/// <b>Both halves in one answer, because on a fleet they arrive together.</b> A holder
/// settles, sweeps, covers and repairs on one telling, so splitting this would put two
/// round trips where the wire has one.
/// </para>
/// </remarks>
public readonly record struct Response
{
    /// <summary>Which moment this answers.</summary>
    public required Stamp To { get; init; }

    /// <summary>Whether the moment was taken at all.</summary>
    /// <remarks>
    /// <b>A brain that cannot take a moment abstains rather than mis-settles</b>, and that
    /// makes this a backpressure reading as well as a verdict. A source that pushes twice
    /// with one stamp, or out of order, is not offering a settlement for the moment held —
    /// so the alternative to refusing is counting an answer against a question it was never
    /// about.
    /// </remarks>
    public required bool Took { get; init; }

    /// <summary>What is expected to follow, and who says so.</summary>
    public required Vote Vote { get; init; }

    /// <summary>What the settlement added to the population.</summary>
    public required Learnt Learnt { get; init; }
}

/// <summary>
/// The one thing being tuned, and the only place a brain-side number lives.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's rule: one brain across every world.</b> On `csharp` the dials that
/// decided how thinking worked were passed to a world's runner, and several had
/// different defaults in different worlds — so switching world switched brain, and
/// every comparison between two problems was also a comparison between two machines.
/// </para>
/// <para>
/// <b>So <see cref="CommittingSettings"/> is constructed once</b>, outside any world, and
/// handed in. A world may turn its own dials as hard as it likes and cannot reach
/// one of these; `SeparationTests` fails the build if it tries.
/// </para>
/// <para>
/// <b>And what it is made of sits behind it.</b> One population or twenty machines is a
/// deployment fact, so the substrate is handed in and everything above this line is written
/// once. Nothing outside asks whether it is alone.
/// </para>
/// </remarks>
public sealed class Brain
{
    /// <summary>The modality every world's outcome is said in.</summary>
    /// <remarks>
    /// <b>Shared across worlds on purpose.</b> A brain that learnt a different
    /// alphabet per world would not be one brain — and a commitment about an outcome
    /// would mean something different depending on who was asking.
    /// </remarks>
    public const byte Followed = 101;

    private readonly ICouncil _substrate;

    private readonly Dictionary<byte, long> _seen = [];

    /// <summary>
    /// Whose claim held on the last moment of each source — <b>the one thing a scope can
    /// be about a commitment through.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Per source for the reason <see cref="_seen"/> is.</b> What follows a moment of one
    /// world is the next moment of that world, so a decider from a text conversation has no
    /// business in a generated world's moment.
    /// </para>
    /// <para>
    /// <b>And it is the DECIDER rather than everything that fired</b>, which C1 settles
    /// rather than the width. A holder knows only its own firings, so a moment carrying them
    /// would carry different codes on every machine — and only identical evidence converges
    /// on a name, which is measured here rather than assumed.
    /// <see cref="Vote.By"/> is the fleet's verdict and is the same code everywhere.
    /// </para>
    /// <para>
    /// <b>Metacognition, and where a self-model starts</b> — the plan's own words for this,
    /// evicted here when the leaf that carried them became a built mechanism. A scope holding
    /// an identity is a claim about the machine's own rules rather than about the world, and
    /// that is the whole of what the meta level means here.
    /// </para>
    /// </remarks>
    private readonly Dictionary<byte, Code> _held = [];

    /// <param name="dials">Every number the brain is allowed to have.</param>
    /// <param name="seed">The control arm's generator.</param>
    public Brain(CommittingSettings dials, int seed)
        : this(dials, seed, held => new Alone(held))
    {
    }

    /// <param name="dials">Every number the brain is allowed to have.</param>
    /// <param name="seed">The control arm's generator.</param>
    /// <param name="substrate">What holds the commitments, given this brain's population.</param>
    /// <remarks>
    /// <b>A factory rather than a council</b>, because a council of one is built over the
    /// population this constructor makes and nothing outside can hold it first. A fleet
    /// ignores the argument it is handed, which is the honest reading of a deployment where
    /// the commitments live on other machines.
    /// </remarks>
    public Brain(CommittingSettings dials, int seed, Func<Population, ICouncil> substrate)
    {
        ArgumentNullException.ThrowIfNull(dials);
        ArgumentNullException.ThrowIfNull(substrate);

        Dials = dials;
        Held = new Population(dials, seed);

        _substrate = substrate(Held);
    }

    /// <summary>Every number the brain is allowed to have.</summary>
    public CommittingSettings Dials { get; }

    /// <summary>What it holds.</summary>
    public Population Held { get; }

    /// <summary>Where the wall clock went, by phase.</summary>
    public Spent Spent => _substrate.Spent;

    /// <summary>
    /// The relation that held on this source's last moment, or nothing — <b>what the next
    /// moment carries over and above the world.</b>
    /// </summary>
    /// <param name="source">Which stream.</param>
    /// <remarks>
    /// <b>Because an instrument re-matching a moment must match the one ASKED.</b>
    /// The failure census fires the population at a moment of its own making
    /// before the round runs, and with this hidden it fired at a moment the brain never put
    /// — so a round decided by a scope holding an identity was neither outvoted nor
    /// uncovered and fell out of a partition that is asserted to be exact. Two rounds of a
    /// multiplexer run, which is what an exact partition is for.
    /// </remarks>
    public Code? Standing(byte source) =>
        _held.TryGetValue(source, out var relation) ? relation : null;

    /// <summary>The code for one outcome.</summary>
    /// <param name="outcome">Which outcome, as a small whole number.</param>
    public static Code Says(int outcome)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(outcome);
        return new Code(Followed, (ulong)outcome);
    }

    /// <summary>Takes one moment, says what it expects, and settles what the source said.</summary>
    /// <param name="moment">What a source pushed.</param>
    /// <param name="sweeping">Whether to subsume, abstract and cull on this one.</param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <para>
    /// <b>The vote is taken before the settlement is known</b>, which is the deployment
    /// arriving rather than a rearrangement. A machine does not learn that the world is
    /// about to go quiet and then decline to predict: the moment arrives, it says what it
    /// expects, and the settlement either comes or does not.
    /// </para>
    /// <para>
    /// <b>The sweep's calendar is not worked out here.</b> A fleet whose members swept on
    /// their own count would sweep at different moments, and rung five's evidence is the
    /// whole population — so it is one flag from whoever is driving, and the driver is the
    /// one thing that sees every source.
    /// </para>
    /// <para>
    /// <b>Asynchronous because a gathering arrives when it arrives.</b> The vote is a
    /// scatter to every holder and a gather of whatever comes back. In one process nothing
    /// here ever yields, and <see cref="Alone"/> completes every task before returning it.
    /// </para>
    /// </remarks>
    /// <returns>What was expected, and what the settlement added.</returns>
    public async ValueTask<Response> ReceiveAsync(
        Pushed moment, bool sweeping, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(moment.Codes);

        if (_seen.TryGetValue(moment.From.Source, out var last)
            && moment.From.Sequence <= last)
            return new Response
            {
                To = moment.From,
                Took = false,
                Vote = default,
                Learnt = new Learnt { Minted = 0, Repaired = 0, Subsumed = 0 },
            };

        _seen[moment.From.Source] = moment.From.Sequence;

        // The relation that held on the last moment of this source, said out loud as a code
        // so a scope can be ABOUT it. A commitment's identity is a `Code` already, which is
        // what makes this no new machinery -- the leaf claiming relations nest was claiming a
        // property of the type, and nothing had ever put one in a moment for a scope to root
        // on. Inert on a source's first moment and on any round nothing held, which is rung
        // three's arrangement for a front end that reports no order.
        //
        // HELD rather than merely decided, which is what makes it the meta level. A relation
        // that obtained is a thing another relation can be about; a rule that spoke and was
        // wrong is what repair already reads, and it reads it where the statistics are.
        var codes = Standing(moment.From.Source) is { } relation
            ? new HashSet<Code>(moment.Codes) { relation }
            : moment.Codes;

        var vote = await _substrate
            .AskAsync(codes, moment.Fleeting, ct)
            .ConfigureAwait(false);

        // Spent by the moment it is read on, so a stale decider never sits in a stream that
        // has gone quiet. What replaces it is written below, once the settlement is known.
        _held.Remove(moment.From.Source);

        // Wrong is the fleet's verdict and never a holder's, which is why it is told rather
        // than worked out where genesis runs. A shard that had nothing to say about a moment
        // the population as a whole answered correctly has not witnessed a failure, and
        // covering there would mint on every machine that happened to be quiet -- the
        // ungated genesis refutation arriving through the distribution.
        var learnt = await _substrate
            .TellAsync(
                moment.Followed,
                wrong: moment.Followed is { } arrived && vote.Expects != arrived,
                sweeping,
                ct)
            .ConfigureAwait(false);

        // And whose claim held is known here, in the same round it was made, so the code
        // reaching the next moment is one behind rather than two. A round the source could
        // not settle leaves nothing: an abstain is not a relation that held, and writing one
        // there would make silence look like agreement.
        if (moment.Followed is { } settled
            && vote.Expects == settled
            && vote.By is { } decider)
            _held[moment.From.Source] = decider;

        return new Response { To = moment.From, Took = true, Vote = vote, Learnt = learnt };
    }
}
