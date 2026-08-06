using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Thinking;

namespace OpenPlexus.Worlds;

/// <summary>What one CLUTRR run scored.</summary>
/// <remarks>
/// <b>The shared base decides what a result complains about</b>, which is
/// <c>DuplicationTests</c>'s rule and the reason the range checks are not
/// restated here.
/// </remarks>
public sealed record ClutrrResult : Questioned
{
    /// <summary>Whether people were declared fleeting.</summary>
    public required bool Carried { get; init; }

    /// <summary>
    /// Right and asked, broken down by how many hops the chain was.
    /// </summary>
    /// <remarks>
    /// <b>THE WHOLE POINT, AND A HEADLINE ACCURACY HIDES IT.</b> A two-hop chain
    /// states its rule almost outright and a ten-hop chain can only be answered by
    /// composing rules learned on other stories, so one number over both says
    /// nothing about whether anything composed. See <see cref="Composed"/>.
    /// </remarks>
    public required ImmutableArray<(int Hops, int Asked, int Right)> ByHops { get; init; }

    /// <summary>
    /// Accuracy on chains longer than three hops — <b>the only number that can
    /// show composition rather than recall.</b>
    /// </summary>
    /// <remarks>
    /// <b>Three, because that is where the corpus's own training split stops.</b>
    /// CLUTRR's generalisation setting trains on two and three and tests to ten,
    /// so a chain of four is the shortest one nobody could have been told the
    /// answer to. The split means nothing to a system with no training phase, but
    /// the LENGTH still marks where recall stops being available.
    /// </remarks>
    public double Composed
    {
        get
        {
            var deep = ByHops.Where(one => one.Hops > 3).ToList();
            var asked = deep.Sum(one => one.Asked);

            return asked == 0 ? 0.0 : deep.Sum(one => one.Right) / (double)asked;
        }
    }

    /// <inheritdoc/>
    protected override string Shown => "stories";

    /// <summary>
    /// A person, a slot, a person — <b>three, because a chain is only a chain once
    /// a route has left the pair it started from.</b>
    /// </summary>
    protected override int Composes => 3;

    /// <inheritdoc/>
    protected override string Stalled => "no route composed anything";

    /// <inheritdoc/>
    protected override void Beyond(List<string> wrong)
    {
        ArgumentNullException.ThrowIfNull(wrong);

        // A RUN THAT NEVER REACHED A RELATION HAS NOT ASKED THE QUESTION. Every
        // answer here is a relation, so total silence is the walk failing to afford
        // one hop rather than the corpus being hard -- and it reads as a chance
        // score either way unless it is said. See the named trap: a silence has two
        // causes wanting opposite fixes.
        if (Asked > 0 && Silent == Asked)
            wrong.Add("no walk ever reached a relation, so nothing was answered");
    }

    public override string ToString() =>
        $"fleeting={Carried} | stories={Moments} asked={Asked} right={Right} "
        + $"silent={Silent} | accuracy={Accuracy:F4} composed={Composed:F4} "
        + $"chance={Chance:F4} | nodes={Nodes} edges={Edges} widest={Widest} | "
        + $"msgs={Messages} halted={Halted} unsettled={Unsettled}{Wrong}";
}

/// <summary>
/// CLUTRR, run against the graph — <b>the first world here whose every question
/// is relational and nothing else.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>SCORED PREQUENTIALLY, WHICH IS WHAT MAKES THE ANSWER BEING WRITTEN BACK
/// LEGITIMATE.</b> Each story is asked BEFORE any of it is written, then the whole
/// story — its stated edges and the pair the question was about — is joined. So
/// the graph is never marked on a story it has already seen, and the composition
/// rules it accumulates are the only thing that can carry to the next one. There
/// is no training phase; C4 forbids one.
/// </para>
/// <para>
/// <b>THE PEOPLE ARE FLEETING AND THE RELATIONS ARE NOT.</b> A person exists in one
/// story, so a lasting node growing a row entry per person would grow forever —
/// the fault measured on the binding world, where the widest row went from
/// fourteen to a hundred and six with no sign of levelling. A relation is the
/// opposite: the walk has to ARRIVE at one, so it must last.
/// </para>
/// <para>
/// <b>THE SLOTS ARE SAID THROUGH <see cref="Learning.Occasion.Groups"/> AND NOT
/// THROUGH <see cref="Learning.Occasion.Roles"/>, AND THAT IS A GAP AND NOT A
/// CHOICE.</b> <see cref="Kind.Role"/> is the mechanism built for this, and
/// nothing in the library can reach it: <see cref="Coded"/> carries four of the
/// five front-end channels and <c>Roles</c> is not one of them, so no front end
/// has ever written a role cell and every number on <c>BindingGapTests</c> comes
/// from an occasion a test constructed by hand. Grouping a filler with its slot
/// code writes the same pair under <see cref="Kind.With"/> instead of
/// <see cref="Kind.Fills"/>, which is a baseline the real channel can then be
/// measured against rather than a replacement for it.
/// </para>
/// </remarks>
public sealed class ClutrrRun : IDisposable
{
    private readonly Fabric _fabric;
    private readonly Machines.InputMachine<Coded> _reading;
    private readonly Clutrr _world;
    private readonly WalkSettings _dials;

    /// <param name="world">How much to read.</param>
    /// <param name="dials">The walk.</param>
    /// <param name="seed">The ring's seed.</param>
    /// <param name="clusters">How many clusters the codes are spread over.</param>
    /// <param name="replicas">Ring replicas per cluster.</param>
    public ClutrrRun(
        ClutrrSettings world,
        WalkSettings dials,
        int seed,
        int clusters = 8,
        int replicas = 256)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        _world = new Clutrr(world);
        _dials = dials;
        _fabric = new Fabric(dials, seed, clusters, replicas);
        _reading = _fabric.Watching("reading", dials);
    }

    /// <summary>The world this run is reading.</summary>
    public Clutrr World => _world;

    /// <summary>
    /// The codes a walk may answer with — <b>every relation's first slot.</b>
    /// </summary>
    /// <remarks>
    /// <b>SLOT NOUGHT, BECAUSE THE QUESTION IS ASKED FROM THE FIRST PERSON.</b>
    /// <i>What is A to B</i> broadcasts A, and A is what fills the first slot of
    /// the answer — so arriving at <c>grandson/0</c> is the answer <i>grandson</i>.
    /// The plain relation code is never written by anything here, so narrowing to
    /// it would narrow to nothing.
    /// </remarks>
    private ImmutableArray<Code> Answers =>
        [.. _world.Relations.Select(relation => relation.Role(0))];

    /// <summary>Shows every story and asks what is asked about it.</summary>
    /// <param name="ct">Cancellation.</param>
    public async Task<ClutrrResult> RunAsync(CancellationToken ct = default)
    {
        int asked = 0, right = 0, silent = 0, unbalanced = 0, unsettled = 0;
        long halted = 0;

        var chains = new Chains();
        var byHops = new Dictionary<int, (int Asked, int Right)>();
        var answers = Answers;
        var at = 0L;

        foreach (var story in _world.Stories)
        {
            // THE PREMISES FIRST, AND THAT IS NOT A LEAK. A question here is about
            // THIS story's people, and a person exists in one story -- so asking
            // before the chain is written walks from codes that have no row at all,
            // which scored silent on every question of every length. What must be
            // withheld is the ANSWER, not the story it is asked about.
            foreach (var edge in story.Edges)
                await ShowAsync(story, edge.From, edge.To, edge.Relation, at++, ct)
                    .ConfigureAwait(false);

            var answered = await AskAsync(story, answers, ct).ConfigureAwait(false);

            halted += answered.Halted;
            if (!answered.Balanced) unbalanced++;
            if (!answered.Settled) unsettled++;
            chains.Fold(answered.Reached);

            var answer = answered.Answer;

            asked++;
            var correct = answer is { } reached && reached == story.Answer.Role(0);

            if (answer is null) silent++;
            if (correct) right++;

            var tally = byHops.GetValueOrDefault(story.Hops);
            byHops[story.Hops] = (tally.Asked + 1, tally.Right + (correct ? 1 : 0));

            // AND THE ANSWER ONLY AFTER IT WAS GUESSED. The query edge is what
            // turns a chain into a rule: without it the graph sees the hops and
            // never what they add up to. Written here, it can only ever help the
            // NEXT story, which is what makes the score prequential.
            await ShowAsync(
                story, story.Query.From, story.Query.To, story.Answer, at++, ct)
                .ConfigureAwait(false);
        }

        _fabric.Failures();

        return new ClutrrResult
        {
            Carried = _world.Carried,
            Moments = _world.Stories.Count,
            Asked = asked,
            Right = right,
            Silent = silent,
            Chance = _world.Chance,
            Halted = halted,
            Reflections = Reflections.Of(_dials, 0),
            Plumbing = _fabric.Facts(chains, unbalanced),
            Unsettled = unsettled,
            ByHops =
                [.. byHops
                    .OrderBy(one => one.Key)
                    .Select(one => (one.Key, one.Value.Asked, one.Value.Right))],
        };
    }

    /// <summary>
    /// Shows one stated relation as a moment: <b>two people, each beside the slot
    /// it fills.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE GROUPING IS WHAT KEEPS THE TWO FILLERS APART, and without it the
    /// binding is destroyed in one hop.</b> Both people are in the moment, so
    /// ungrouped they would pair with each other and with both slots — and a walk
    /// from the first person would reach the SECOND slot through the second person
    /// as fast as it reached its own. That is step 9's refutation in a new place;
    /// the group gate is the remedy already built for it.
    /// </remarks>
    private async Task ShowAsync(
        Story story, int from, int to, Kind relation, long at, CancellationToken ct)
    {
        var left = story.Who(from);
        var right = story.Who(to);

        var codes = ImmutableArray.Create(left, relation.Role(0), right, relation.Role(1));

        var groups = new Dictionary<Code, int>
        {
            [left] = 0,
            [relation.Role(0)] = 0,
            [right] = 1,
            [relation.Role(1)] = 1,
        };

        await _reading.ObserveAsync(
            new Coded
            {
                Codes = codes,
                Groups = groups,

                // A PERSON IS OF THIS STORY AND NOTHING ELSE -- but declaring that
                // severs the chain, so it is the arm and not the default. See
                // ClutrrSettings.Fleeting.
                Passing = _world.Carried ? new HashSet<Code> { left, right } : null,
            },
            at, ct: ct).ConfigureAwait(false);

        await _fabric.QuietAsync(ct).ConfigureAwait(false);
    }

    /// <summary>One question, with the plumbing left attached.</summary>
    private readonly record struct Answering(
        Code? Answer,
        int Halted,
        bool Balanced,
        bool Settled,
        IReadOnlyList<Arrival> Reached);

    /// <summary>Asks which relation holds between the pair the question names.</summary>
    private async Task<Answering> AskAsync(
        Story story, ImmutableArray<Code> answers, CancellationToken ct)
    {
        var origins = ImmutableArray.Create(
            story.Who(story.Query.From), story.Who(story.Query.To));

        var thought = await _reading
            .ThinkAsync(origins, _dials.Stamina, ct: ct).ConfigureAwait(false);

        var settled = await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);

        var reached = thought.BestAmong(answers, 1);

        var answered = new Answering(
            reached.Count == 0 ? null : reached[0].Endpoint,
            thought.Halted,
            thought.Balanced(),
            settled,
            thought.Best(int.MaxValue));

        _reading.Forget(thought.Id);

        return answered;
    }

    public void Dispose() => _fabric.Dispose();
}
