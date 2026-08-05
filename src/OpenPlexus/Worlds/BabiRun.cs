using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using OpenPlexus.Thinking;

namespace OpenPlexus.Worlds;

/// <summary>
/// What one bAbI task measured. <b>Counts, not claims.</b>
/// </summary>
public sealed record BabiResult : Questioned
{
    /// <summary>Which of the twenty tasks this was.</summary>
    public required int Task { get; init; }

    /// <inheritdoc cref="BabiSettings.Stories"/>
    public required bool Stories { get; init; }

    /// <summary>
    /// How many sentences a departed word was carried for — <see cref="Window"/>.
    /// <b>Reported because a number nobody can see in the output is a number that
    /// looks the same at every setting.</b>
    /// </summary>
    /// <remarks>
    /// <b>THIS IS THE WORLD THE SPAN IS REFUTED ON</b>, and it is on anyway
    /// because a zero here was an off switch. See <see cref="WalkSettings.Span"/>.
    /// </remarks>
    public required int Span { get; init; }

    /// <summary>
    /// How many sentences of plain English were shown before the task —
    /// <see cref="Primer"/>, and nought is off.
    /// </summary>
    /// <remarks>
    /// <b>Reported for the reason <see cref="Span"/> is:</b> an arm nobody can see
    /// in the output is an arm that looks distinct and is not.
    /// </remarks>
    public required int Primed { get; init; }

    /// <inheritdoc cref="Babi.Commonest"/>
    public required double Commonest { get; init; }

    /// <inheritdoc cref="Babi.Compound"/>
    public required int Compound { get; init; }

    /// <summary>How many distinct answers the task ever expects.</summary>
    public required int Alphabet { get; init; }

    /// <summary>
    /// How many of the questions were asked before the answer alphabet held
    /// anything at all.
    /// </summary>
    /// <remarks>
    /// <b>THE PREQUENTIAL COST, AND IT IS NOT A FAULT.</b> The label space is
    /// revealed one answer at a time, so the first question of a run has nothing
    /// to choose between and is necessarily silent. C4 forbids a training phase in
    /// which the alphabet could have been collected up front, so this is what
    /// having no episode boundary actually costs, counted rather than absorbed.
    /// </remarks>
    public required int Blind { get; init; }

    /// <summary>
    /// The share of questions answered right, over the questions this world can
    /// express an answer to at all.
    /// </summary>
    /// <remarks>
    /// <b>Reported beside <see cref="Questioned.Accuracy"/> and never instead of
    /// it.</b> See <see cref="Babi.Compound"/> — a compound answer is scored wrong
    /// because it is wrong, and this says how much of the deficit was that.
    /// </remarks>
    public double Expressible =>
        Asked - Compound <= 0 ? 0.0 : Right / (double)(Asked - Compound);

    /// <inheritdoc/>
    protected override string Shown => "sentences";

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Two, because the twenty tasks do not agree on a depth.</b> <i>Basic
    /// induction</i> needs a chain through an intermediate and <i>single
    /// supporting fact</i> is one hop, so anything higher would complain about
    /// tasks that are working. This is the floor that says the walk walked.
    /// </remarks>
    protected override int Composes => 2;

    /// <inheritdoc/>
    protected override string Stalled => "no route left its origin";

    /// <inheritdoc/>
    protected override void Beyond(List<string> wrong)
    {
        ArgumentNullException.ThrowIfNull(wrong);

        if (Alphabet == 0) wrong.Add("the task expects no answers at all");

        // THE NARROWING HAS TO HAVE SOMETHING TO NARROW TO. Every question after
        // the first should have a non-empty label space, so a run where nearly
        // all of them were blind is a wiring fault and not a hard task.
        if (Asked > 1 && Blind >= Asked - 1)
            wrong.Add($"{Blind} of {Asked} questions had no answer alphabet yet");

        // A TASK WHOSE ANSWERS ARE ALL COMPOUND IS BEING MEASURED FOR NOTHING.
        // Reported rather than hidden, because the score is a foregone zero and
        // the reason is structural.
        if (Asked > 0 && Compound >= Asked)
            wrong.Add("every answer in this task is more than one word");
    }

    public override string ToString() =>
        $"task={Task} stories={(Stories ? "on" : "off")} span={Span} " +
        $"primed={Primed} " +
        $"sentences={Moments} asked={Asked} right={Right} silent={Silent} " +
        $"compound={Compound} blind={Blind} | " +
        $"accuracy={Accuracy:F4} expressible={Expressible:F4} " +
        $"commonest={Commonest:F4} chance={Chance:F4} | " +
        $"reflect={(Reflecting ? "on" : "off")} wrote={Reflected} | " +
        $"nodes={Nodes} edges={Edges} widest={Widest} spread=[{string.Join(",", Spread)}] | " +
        $"chains={{{Plumbing.Lengths}}} deepest={Deepest} | " +
        $"msgs={Messages} halted={Halted} unbalanced={Unbalanced} unsettled={Unsettled}{Wrong}";
}

/// <summary>
/// One bAbI task, wired to the graph.
/// </summary>
/// <remarks>
/// <para>
/// <b>Scored prequentially, like every other world here.</b> The corpus ships a
/// train file and a test file and this reads only the train file, straight
/// through: a question is asked at the moment the corpus asks it, against
/// whatever the graph has learnt from the sentences before it. There is no
/// training phase and no testing phase, because C4 forbids one.
/// </para>
/// <para>
/// <b>WHAT THE HARNESS REVEALS, AND IT IS EXACTLY ONE THING.</b> After a question
/// is scored, the answer's words are added to the set the narrowing may choose
/// from — the label space, not the label. Nothing is learnt from it and no
/// occasion is written; a question never joins the graph. That is the same rule
/// the other worlds follow, where reflection is deliberately fed only what was
/// observed and never what was asked.
/// </para>
/// </remarks>
public sealed class BabiRun : IDisposable
{
    private readonly Fabric _fabric;
    private readonly InputMachine<Coded> _reader;
    private readonly Babi _world;
    private readonly WalkSettings _dials;


    /// <summary>
    /// The answers seen so far, which is what the narrowing may choose from.
    /// </summary>
    /// <remarks>
    /// <b>This is the output machine's alphabet and not knowledge of the task.</b>
    /// A walk returns the endpoint it ranked first over the whole graph, and
    /// without this the first answer to nearly every question is a word from the
    /// question itself — <i>where</i> and <i>is</i> are the strongest partners of
    /// everything they occur with. Every published system on this corpus picks
    /// from a fixed answer vocabulary; this one grows it as it goes, which is
    /// strictly harder.
    /// </remarks>
    private readonly HashSet<Code> _alphabet = [];

    /// <param name="world">Which task, from where, and with which arms on.</param>
    /// <param name="dials">The walk.</param>
    /// <param name="seed">The ring's seed, so placement reproduces.</param>
    /// <param name="clusters">How many clusters the codes are spread over.</param>
    /// <param name="replicas">Ring replicas per cluster.</param>
    /// <param name="primer">
    /// <inheritdoc cref="Primer" path="/summary"/> <b>Null is the control</b>, and
    /// it is the arm every number before 2026-08-04 was taken under.
    /// </param>
    /// <remarks>
    /// <b>THIS IS THE FIRST WORLD THE WINDOW COULD POSSIBLY WORK ON.</b> It was
    /// built to give the graph temporal edges, measured null on snake, and never
    /// run anywhere else — and snake is a world where what matters is what is
    /// visible now. A corpus of sentences in the order somebody wrote them is a
    /// stream where <i>before</i> and <i>after</i> are the content, so a null here
    /// would mean something that a null on snake did not.
    /// </remarks>
    public BabiRun(
        BabiSettings world,
        WalkSettings dials,
        int seed,
        int clusters = 8,
        int replicas = 256,
        Primer? primer = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        _world = new Babi(world);
        _dials = dials;
        _primer = primer?.Lines ?? [];
        _fabric = new Fabric(dials, seed, clusters, replicas);

        _reader = new InputMachine<Coded>(
            new MachineAddress("reader"), new Passthrough(),
            new LocalRendezvous(_fabric.Local),
            _fabric.Bus, _fabric.Ring, dials);

        _fabric.Subscribe(_reader);
    }

    /// <inheritdoc cref="Primer"/>
    /// <remarks>
    /// <b>Empty when nothing was primed</b>, which is the control. <b>This stays a
    /// world's business rather than the brain's</b> — it is DATA to read, not a
    /// choice about how to think.
    /// </remarks>
    private readonly IReadOnlyList<Sentence> _primer;

    /// <summary>The world this run is reading.</summary>
    public Babi World => _world;

    /// <summary>
    /// One sentence as the brain receives it — <b>the world's own knowledge, and
    /// the only kind a front end is allowed to carry.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE STORY CODE IS MINTED FRESH PER STORY AND NEVER SEEN AGAIN</b>, and
    /// only this world can know that: a code is opaque to the graph, and "this one
    /// will never recur" is a fact about how it was minted. See
    /// <see cref="Codes.Coded.Passing"/>. <b>Null when the arm is off</b>, which is
    /// the control — there is then no story code in the sentence to name.
    /// </remarks>
    private static Coded Told(Sentence line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var telling = Babi.Telling(line.Story);

        return new Coded
        {
            Codes = line.Words,
            Passing = line.Words.Contains(telling) ? new HashSet<Code> { telling } : null,
        };
    }

    /// <summary>
    /// Reads the task, learning from statements and answering questions where
    /// they fall.
    /// </summary>
    /// <param name="sentences">
    /// How many lines of the file to read at most. <b>The whole file when it is
    /// not given</b>, which is a thousand stories.
    /// </param>
    /// <param name="votes">
    /// How many concurrent walks one question gets. See the note on voting in
    /// <see cref="SensesRun"/>.
    /// </param>
    /// <param name="ct">Cancellation.</param>
    public async Task<BabiResult> RunAsync(
        int sentences = int.MaxValue, int votes = 1, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sentences);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(votes);

        int shown = 0, asked = 0, right = 0, silent = 0, blind = 0;
        int unbalanced = 0, unsettled = 0, compound = 0, primed = 0;
        long halted = 0;

        var reflected = 0;
        var chains = new Chains();
        var at = 0L;

        // THE ENGLISH FIRST, IN THE SAME RUN AND ON THE SAME GRAPH. Nothing is
        // reset between this and the task -- there is no train-then-test here and
        // C4 forbids one, so the primer is simply what the system saw earlier.
        // It is never ASKED anything, so it cannot contribute a right answer; all
        // it can do is put words in the graph and join them to each other.
        foreach (var line in _primer)
        {
            primed++;

            var seen = await _reader.ObserveAsync(Told(line), at++, ct: ct).ConfigureAwait(false);
            await _fabric.QuietAsync(ct).ConfigureAwait(false);

            if (seen is not null)
                reflected += await _reader.ReflectAsync(seen, at, ct).ConfigureAwait(false);
        }

        foreach (var line in _world.Lines.Take(sentences))
        {
            if (!line.Asking)
            {
                shown++;

                var observed = await _reader
                    .ObserveAsync(Told(line), at++, ct: ct).ConfigureAwait(false);

                await _fabric.QuietAsync(ct).ConfigureAwait(false);

                // FORK 21 REFLECTS ON WHAT WAS OBSERVED AND NEVER ON WHAT WAS
                // ASKED, exactly as the other worlds do. A question's own answer
                // written back would be the measurement leaking into the graph.
                if (observed is not null)
                    reflected += await _reader
                        .ReflectAsync(observed, at, ct).ConfigureAwait(false);

                continue;
            }

            asked++;
            if (line.Answers.Length != 1) compound++;
            if (_alphabet.Count == 0) blind++;

            var (answer, stopped, balanced, settled, everything) =
                await AskingAsync(line, votes, ct).ConfigureAwait(false);

            halted += stopped;
            if (!balanced) unbalanced++;
            if (!settled) unsettled++;

            chains.Fold(everything);

            if (answer is null) silent++;
            else if (line.Answers.Length == 1 && answer.Value == line.Answers[0]) right++;

            // THE LABEL SPACE, AFTER THE SCORING AND NEVER BEFORE IT. See the
            // note on _alphabet: what is revealed is that this word can be an
            // answer, never that it is the answer to this question.
            foreach (var word in line.Answers) _alphabet.Add(word);
        }

        _fabric.Failures();

        return new BabiResult
        {
            Task = _world.Task,
            Stories = _world.Stories,
            Span = _dials.Span,
            Primed = primed,
            Moments = shown,
            Asked = asked,
            Right = right,
            Silent = silent,
            Compound = compound,
            Blind = blind,
            Alphabet = _world.Alphabet.Count,
            Commonest = _world.Commonest,
            Chance = _world.Chance,
            Reflections = Reflections.Of(_dials, reflected),
            Plumbing = _fabric.Facts(chains, unbalanced),
            Halted = halted,
            Unsettled = unsettled,
        };
    }

    /// <summary>One question, with the plumbing left attached.</summary>
    private readonly record struct Asking(
        Code? Answer,
        int Halted,
        bool Balanced,
        bool Settled,
        IReadOnlyList<Arrival> Reached);

    /// <summary>
    /// Broadcasts a question's words and reads back the best answer among the
    /// words that have ever been answers.
    /// </summary>
    /// <remarks>
    /// <b>The question's own words are struck out of the candidates.</b> <i>Where
    /// is Mary?</i> must not be answered <i>Mary</i>, and a route that walks
    /// nowhere ends on the code it started from — so without this the narrowing
    /// would reward exactly the routes that did no work.
    /// </remarks>
    private async Task<Asking> AskingAsync(Sentence line, int votes, CancellationToken ct)
    {
        var origins = line.Words;
        var asked = origins.ToHashSet();

        var candidates = _alphabet.Where(word => !asked.Contains(word)).ToHashSet();

        if (candidates.Count == 0) return new Asking(null, 0, true, true, []);

        var asking = new Task<Asking>[votes];
        for (var i = 0; i < votes; i++) asking[i] = OnceAsync(origins, candidates, ct);

        var answers = await Task.WhenAll(asking).ConfigureAwait(false);

        return answers[0] with
        {
            Answer = Majority.Of(answers.Select(one => one.Answer)).Chosen,
            Halted = answers.Sum(one => one.Halted),
            Balanced = answers.All(one => one.Balanced),
            Settled = answers.All(one => one.Settled),
            Reached = [.. answers.SelectMany(one => one.Reached)],
        };
    }

    /// <summary>One walk.</summary>
    private async Task<Asking> OnceAsync(
        ImmutableArray<Code> origins, IReadOnlyCollection<Code> candidates, CancellationToken ct)
    {
        var thought = await _reader
            .ThinkAsync(origins, _dials.Stamina, new Question { Ranking = _dials.Ranking }, ct)
            .ConfigureAwait(false);

        var settled = await _fabric.SettleAsync(thought, ct).ConfigureAwait(false);

        var reached = thought.BestAmong(candidates, 1);

        var report = new Asking(
            reached.Count == 0 ? null : reached[0].Endpoint,
            thought.Halted,
            thought.Balanced(),
            settled,
            thought.Best(int.MaxValue));

        _reader.Forget(thought.Id);
        return report;
    }

    public void Dispose() => _fabric.Dispose();
}
