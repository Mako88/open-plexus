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
internal readonly record struct Response
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
internal sealed class Brain
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

    /// <summary>The code for one outcome.</summary>
    /// <param name="outcome">Which outcome, as a small whole number.</param>
    public static Code Says(int outcome)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(outcome);
        return new Code(Followed, (ulong)outcome);
    }

    /// <summary>Which outcome a code says followed, or nothing where it says something else.</summary>
    /// <param name="said">A code a commitment expects.</param>
    /// <remarks>
    /// <b>The inverse of <see cref="Says"/>, and it has to be able to refuse</b>. Every code in
    /// a moment is a candidate for what a commitment expects, so a reader that assumed the
    /// answer would read a word of the story as an outcome index and be confidently wrong.
    /// </remarks>
    public static int? Meant(Code said) =>
        said.Modality == Followed ? (int)said.Value : null;

    /// <summary>What this brain would say about a moment, without taking it.</summary>
    /// <param name="felt">The codes a moment would arrive as.</param>
    /// <remarks>
    /// <para>
    /// <b>Read-only</b>, and it is the same three calls the failure census is built out of. It
    /// moves no counter, mints nothing and settles nothing, so asking twice and asking once are
    /// the same question.
    /// </para>
    /// <para>
    /// <b>What this machine holds rather than what a fleet would say</b>. A council's answer is
    /// a scatter and a gather, and taking one here would put a wire under a caller that has no
    /// idea it is asking anybody. A chooser in one process is reading the whole population; a
    /// chooser over a fleet is reading its own share and the difference is a measurement rather
    /// than a bug.
    /// </para>
    /// </remarks>
    public Vote Voting(IReadOnlyCollection<Code> felt)
    {
        ArgumentNullException.ThrowIfNull(felt);

        var moment = Held.Moment(felt as IReadOnlySet<Code> ?? new HashSet<Code>(felt));

        return Held.Predict(Held.Firing(moment));
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

        var vote = await _substrate
            .AskAsync(moment.Codes, moment.Fleeting, moment.Grouping, ct)
            .ConfigureAwait(false);

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

        return new Response { To = moment.From, Took = true, Vote = vote, Learnt = learnt };
    }
}
