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

    /// <summary>How a stated relation was written into the graph.</summary>
    public required Slots Slots { get; init; }

    /// <summary>How many walks each question got.</summary>
    public required int Steps { get; init; }

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
    /// Asked and right on stories whose answer was <b>also stated in the chain.</b>
    /// </summary>
    /// <remarks>
    /// <b>RECALL, AND IT MUST NOT BE ADDED TO THE OTHER.</b> See
    /// <see cref="Story.Restated"/>: the answer's slot code is already in a moment
    /// the graph just read, so arriving at it composes nothing.
    /// </remarks>
    public required (int Asked, int Right, int Silent) Recalled { get; init; }

    /// <summary>
    /// Asked and right on stories whose answer <b>appears nowhere in the chain.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE ONLY NUMBER ON THIS RECORD THAT CAN SHOW COMPOSITION.</b> The answer
    /// was never in front of the graph, so reaching it means applying something
    /// learned on other stories about relations rather than about people.
    /// </remarks>
    public required (int Asked, int Right, int Silent) Fresh { get; init; }

    /// <summary>
    /// Of the fresh stories that were answered at all, how many were answered with
    /// a relation <b>the story itself stated.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE DIAGNOSTIC THAT SAYS RETRIEVAL FROM COMPOSITION.</b> A fresh story's
    /// answer is by definition not among its stated relations, so every one of
    /// these is wrong BY CONSTRUCTION — and a walk that scores nought while filling
    /// this counter is not failing to afford an answer, it is confidently returning
    /// the wrong KIND of thing. That is a different defect from silence and wants
    /// the opposite fix.
    /// </remarks>
    public required int Echoed { get; init; }

    /// <summary>Of the stories that restated their answer, the share got right.</summary>
    public double Recall => Recalled.Asked == 0 ? 0.0 : Recalled.Right / (double)Recalled.Asked;

    /// <summary>
    /// Of the stories that did not, the share got right — <b>the headline, and the
    /// one an earlier commit reported wrongly.</b>
    /// </summary>
    public double Composed => Fresh.Asked == 0 ? 0.0 : Fresh.Right / (double)Fresh.Asked;

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
        $"fleeting={Carried} slots={Slots} steps={Steps} | stories={Moments} asked={Asked} "
        + $"right={Right} silent={Silent} | accuracy={Accuracy:F4} "
        + $"recall={Recall:F4} ({Recalled.Right}/{Recalled.Asked}) "
        + $"composed={Composed:F4} ({Fresh.Right}/{Fresh.Asked}, quiet {Fresh.Silent}) "
        + $"echoed={Echoed} chance={Chance:F4} | "
        + $"nodes={Nodes} edges={Edges} widest={Widest} | "
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
/// <b>THE SLOTS ARE SAID TWO WAYS AND THAT IS THE ARM</b> — see
/// <see cref="ClutrrSettings.Slots"/>. Grouping a filler with its slot code writes
/// the same PAIR the role channel writes, under <see cref="Kind.With"/> rather than
/// <see cref="Kind.Fills"/>, so it is a working baseline rather than a stand-in.
/// <b>This world is the first caller in the library to reach the role channel at
/// all</b>: <see cref="Coded"/> carried four of the five front-end channels until
/// now, so every number on <c>BindingGapTests</c> came from an occasion a test
/// constructed by hand.
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
    /// <param name="asking">
    /// What the asker knows. <b><see cref="Question.Steps"/> is what this world was
    /// built to measure</b> — see the note there for why it is the asker's call and
    /// not a dial.
    /// </param>
    public async Task<ClutrrResult> RunAsync(
        Question? asking = null, CancellationToken ct = default)
    {
        int asked = 0, right = 0, silent = 0, unbalanced = 0, unsettled = 0;
        int toldAsked = 0, toldRight = 0, toldQuiet = 0;
        int freshAsked = 0, freshRight = 0, freshQuiet = 0, echoed = 0;
        long halted = 0;

        var chains = new Chains();
        var byHops = new Dictionary<int, (int Asked, int Right)>();
        var answers = Answers;
        var question = asking ?? new Question();
        var at = 0L;

        foreach (var story in _world.Stories)
        {
            // THE PREMISES FIRST, AND THAT IS NOT A LEAK. A question here is about
            // THIS story's people, and a person exists in one story -- so asking
            // before the chain is written walks from codes that have no row at all,
            // which scored silent on every question of every length. What must be
            // withheld is the ANSWER, not the story it is asked about.
            var at2 = 0;

            foreach (var edge in story.Edges)
                await ShowAsync(story, at2++, edge.From, edge.To, edge.Relation, at++, ct)
                    .ConfigureAwait(false);

            var answered = await AskAsync(story, answers, question, ct).ConfigureAwait(false);

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

            // RECALL AND COMPOSITION ARE DIFFERENT QUESTIONS. See Story.Restated.
            if (story.Restated)
            {
                toldAsked++;
                if (correct) toldRight++;
                if (answer is null) toldQuiet++;
            }
            else
            {
                freshAsked++;
                if (correct) freshRight++;
                if (answer is null) freshQuiet++;

                // DID IT JUST HAND BACK SOMETHING THE STORY SAID? See Echoed.
                else if (story.Edges.Any(edge => edge.Relation.Role(0) == answer)) echoed++;
            }

            // AND THE ANSWER ONLY AFTER IT WAS GUESSED. The query edge is what
            // turns a chain into a rule: without it the graph sees the hops and
            // never what they add up to. Written here, it can only ever help the
            // NEXT story, which is what makes the score prequential.
            await ShowAsync(
                story, at2, story.Query.From, story.Query.To, story.Answer, at++, ct)
                .ConfigureAwait(false);
        }

        _fabric.Failures();

        return new ClutrrResult
        {
            Carried = _world.Carried,
            Slots = _world.Slots,
            Steps = question.Steps,
            Moments = _world.Stories.Count,
            Asked = asked,
            Right = right,
            Silent = silent,
            Chance = _world.Chance,
            Recalled = (toldAsked, toldRight, toldQuiet),
            Fresh = (freshAsked, freshRight, freshQuiet),
            Echoed = echoed,
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
        Story story,
        int which,
        int from,
        int to,
        Kind relation,
        long at,
        CancellationToken ct)
    {
        var left = story.Who(from);
        var right = story.Who(to);

        // THE ROLE CHANNEL DERIVES THE SLOT CODES, so on that arm the moment is just
        // the two people. Grouped, the slots have to be IN the moment and the
        // grouping keeps each one with its own filler. See Slots.
        var slots = _world.Slots;

        if (slots == Slots.Reified)
        {
            await ReifyAsync(story, which, left, right, relation, at, ct)
                .ConfigureAwait(false);

            await _fabric.QuietAsync(ct).ConfigureAwait(false);
            return;
        }

        var codes = slots == Slots.Roled
            ? ImmutableArray.Create(left, right)
            : ImmutableArray.Create(left, relation.Role(0), right, relation.Role(1));

        var groups = slots == Slots.Grouped
            ? new Dictionary<Code, int>
            {
                [left] = 0,
                [relation.Role(0)] = 0,
                [right] = 1,
                [relation.Role(1)] = 1,
            }
            : null;

        await _reading.ObserveAsync(
            new Coded
            {
                Codes = codes,
                Groups = groups,
                Passing = _world.Carried ? new HashSet<Code> { left, right } : null,
                Relating = slots == Slots.Roled ? relation : null,
                Filling = slots == Slots.Roled
                    ? new Dictionary<Code, int> { [left] = 0, [right] = 1 }
                    : null,
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

    /// <summary>
    /// Writes one stated relation as a NODE joining its people and its type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THREE SMALL MOMENTS AND NOT ONE LARGE ONE, because an occasion pairs
    /// EVERYTHING in it and a star is not a clique.</b> Putting the instance, both
    /// people and the type in one moment writes person-to-type and person-to-person
    /// as well — and a relation's own code co-occurring with every code it ever
    /// related is the superhub its own documentation warns about. Written that way
    /// it timed the walk out entirely, which is the slot hub traded for a worse one.
    /// </para>
    /// <para>
    /// <b>AND THE INSTANCE IS FLEETING WHATEVER THE ARM SAYS.</b> One node per
    /// stated relation that lasted would grow the TYPE's row by an entry per
    /// statement forever, which is precisely the hub this exists to avoid. The
    /// people follow <see cref="ClutrrSettings.Fleeting"/>; the instance does not
    /// get a say.
    /// </para>
    /// <para>
    /// <b>The type-level cell is written too</b>, because an instance is seen once
    /// and nothing accumulates on it — see <see cref="Slots.Reified"/>.
    /// </para>
    /// </remarks>
    private async Task ReifyAsync(
        Story story,
        int which,
        Code left,
        Code right,
        Kind relation,
        long at,
        CancellationToken ct)
    {
        var instance = Clutrr.Stating(story.Index, which);
        var passing = new HashSet<Code> { instance };

        // THE TWO ARMS OF THE STAR, each carrying which slot its person fills.
        foreach (var (person, slot) in new[] { (left, 0), (right, 1) })
            await _reading.ObserveAsync(
                new Coded
                {
                    Codes = [person, instance],
                    Passing = _world.Carried
                        ? new HashSet<Code> { instance, person }
                        : passing,
                    Relating = relation,
                    Filling = new Dictionary<Code, int> { [person] = slot },
                },
                at, ct: ct).ConfigureAwait(false);

        // AND WHAT KIND OF STATEMENT IT IS. One way, because the instance is
        // fleeting: the type records what met it and does not record into it.
        await _reading.ObserveAsync(
            new Coded { Codes = [instance, relation.Code], Passing = passing },
            at, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Asks which relation holds between the pair the question names.</summary>
    /// <remarks>
    /// <b>ONE WALK, OR SEVERAL WITH EACH STARTING FROM WHAT THE LAST CONCLUDED</b> —
    /// see <see cref="Question.Steps"/>. The answer is read from the LAST walk
    /// only, so a second step that adds nothing reads as the baseline getting worse
    /// rather than as a free win.
    /// </remarks>
    private async Task<Answering> AskAsync(
        Story story, ImmutableArray<Code> answers, Question asking, CancellationToken ct)
    {
        var origins = ImmutableArray.Create(
            story.Who(story.Query.From), story.Who(story.Query.To));

        var halted = 0;
        var balanced = true;
        var settled = true;
        IReadOnlyList<Arrival> reached = [];
        Code? answer = null;

        for (var step = 0; step < asking.Steps; step++)
        {
            if (origins.IsEmpty) break;

            var thought = await _reading
                .ThinkAsync(origins, _dials.Stamina, asking, ct).ConfigureAwait(false);

            var quiet = await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);

            halted += thought.Halted;
            balanced &= thought.Balanced();
            settled &= quiet;
            reached = thought.Best(int.MaxValue);

            var best = thought.BestAmong(answers, 1);
            answer = best.Count == 0 ? null : best[0].Endpoint;

            // WHAT THIS WALK CONCLUDED, AS THE NEXT ONE'S ORIGINS. The conclusion
            // is not written back -- fork 21 is what would do that, and writing a
            // guess into the graph before it has been scored would contaminate the
            // very thing this measures.
            origins = step + 1 < asking.Steps
                ? thought.Next(asking.Width, asking.Between)
                : [];

            _reading.Forget(thought.Id);
        }

        return new Answering(answer, halted, balanced, settled, reached);
    }

    public void Dispose() => _fabric.Dispose();
}
