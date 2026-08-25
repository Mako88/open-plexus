using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>
/// How the senses world is set up. Every number named and none defaulted.
/// </summary>
public sealed record SensesSettings
{
    /// <summary>How many things there are to know about.</summary>
    public required int Concepts { get; init; }

    /// <summary>
    /// How many codes each sense produces for one concept.
    /// </summary>
    /// <remarks>
    /// <b>More than one, or the task is a lookup table.</b> A concept that
    /// showed the same single code every time would make identity trivial;
    /// several codes per sense is what forces *a concept is what you reach by
    /// walking* to do any work.
    /// </remarks>
    public required int CodesPerSense { get; init; }

    /// <summary>
    /// Pair each sense with a RANDOM concept rather than the right one.
    /// </summary>
    /// <remarks>
    /// <b>The control, and it tests the DATA rather than the code.</b> Every
    /// mechanism runs identically; only the structure the world contains is
    /// destroyed. If accuracy survives this, it was never coming from
    /// composition — it was the measurement finding something spurious, and the
    /// headline number would mean nothing.
    /// </remarks>
    public bool Scrambled { get; init; }

    /// <summary>
    /// The chance that a code shown belongs to some other concept entirely.
    /// </summary>
    /// <remarks>
    /// Real co-occurrence is noisy, and a world without it rewards a mechanism
    /// that cannot tolerate any.
    /// </remarks>
    public required double Noise { get; init; }

    /// <summary>
    /// How many irrelevant codes appear alongside the task, every moment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The size dial, and it is deliberately not a new world.</b> Scale is a
    /// change to a world rather than a task of its own, and the standing rule
    /// here is to run the world that exists instead of building one to flatter a
    /// change. The task, the question and the chance level are all untouched —
    /// clutter carries its own modality, so it can never be an answer. What grows
    /// is the graph.
    /// </para>
    /// <para>
    /// <b>It grows the part that costs.</b> The scaling curve says node count is
    /// nearly free and the WIDEST ROW is what sets the message bill, so a size
    /// dial that only added nodes would measure the cheap axis. Every cluttered
    /// moment joins its codes to the task's, so this widens rows directly.
    /// </para>
    /// </remarks>
    public int Clutter { get; init; }

    /// <summary>
    /// How many distinct irrelevant codes there are to draw from.
    /// </summary>
    /// <remarks>
    /// <b>The sharp end of the dial</b>, because the two extremes test opposite
    /// claims. A large pool makes each irrelevant code RARE, so the graph
    /// grows wide and every clutter partner is thin — that tests whether cost
    /// stays affordable. A small pool makes them UBIQUITOUS, which manufactures
    /// exactly the ever-present background the forward weighting exists to
    /// refuse: <c>together(here, other) / seen(other)</c> should score a code
    /// present at every moment as a weak partner however often it co-occurs.
    /// <b>That claim has never been tested</b> at a size where it could fail.
    /// </remarks>
    public int Pool { get; init; }

    /// <summary>
    /// How unequally the pool is drawn from. <b>Zero is uniform; higher is a
    /// heavier tail.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The shape no world here had, and the scaling measurement is why.</b>
    /// Sixty-four times the alphabet gave sixty-four times the nodes and left the
    /// widest row exactly where it started, so cost per thought never moved. A row
    /// grows without bound only where ONE code accompanies NEARLY EVERYTHING, and
    /// spreading an alphabet thinner gives each code FEWER co-occurrences rather
    /// than more. More nodes was the wrong axis.
    /// </para>
    /// <para>
    /// <b>And <see cref="Pool"/> alone cannot make that shape</b>, because its two
    /// extremes are opposite and both UNIFORM. A small pool makes every
    /// irrelevant code ubiquitous; a large one makes every one rare. Zipf is the
    /// distribution that holds BOTH AT ONCE — a handful of codes at nearly every
    /// moment and a long thin tail behind them — so the ever-present background and
    /// the single-accident coincidence stop being separate arms and become one run.
    /// That is text's shape, and it is the one distribution a row cap could
    /// plausibly be for.
    /// </para>
    /// <para>
    /// <b>Rank <c>k</c> is drawn with probability proportional to
    /// <c>1/k^Skew</c></b>, over the whole pool. At zero this takes the uniform
    /// path unchanged rather than a Zipf draw with a flat exponent — the two agree
    /// in distribution and NOT in how many numbers they take from the generator, so
    /// routing zero through here would move every existing clutter measurement
    /// while looking like it changed nothing.
    /// </para>
    /// </remarks>
    public double Skew { get; init; }

    /// <summary>
    /// How many cross-modal questions this world keeps back and never draws.
    /// </summary>
    /// <remarks>
    /// <b>A COMBINATION rather than a sample</b>, which is what makes the number mean
    /// something here. Every held turn shows a sight and a sound and asks what the thing
    /// feels like, and that combination is drawn nought times however long the run goes on —
    /// so a population that answers it is answering across an occasion type it was never
    /// scored on, rather than one it happened not to meet.
    /// </remarks>
    public int Withheld { get; init; }
}

/// <summary>
/// Three senses, and a thing that is never shown to two of them at once.
/// </summary>
/// <remarks>
/// <para>
/// <b>The second world, and it shares no code with Snake.</b> No space, no
/// movement, no actions, no energy, nothing to lose, no time pressure. If a
/// finding holds here as well, it was about the architecture; if it does not, it
/// was about snake — and every number in this project so far is one world wide.
/// </para>
/// <para>
/// <b>The task is what the design exists for.</b> An occasion shows either
/// SIGHT with SOUND, or SOUND with TOUCH. <b>Sight and touch are never shown
/// together, not once.</b> Then the question is: given a sight, what does it
/// feel like?
/// </para>
/// <para>
/// **A memoriser scores exactly zero**, because the pair it is asked about has
/// never occurred. Getting it right requires walking sight → sound → touch,
/// which is the two-step composition that decides whether this is a graph
/// database with extra steps or not.
/// </para>
/// </remarks>
public sealed class Senses : IWorld<Coded>, IWithholds<Coded>
{
    /// <summary>What a thing looks like.</summary>
    public const byte Sight = 10;

    /// <summary>What a thing sounds like.</summary>
    public const byte Sound = 11;

    /// <summary>What a thing feels like.</summary>
    public const byte Touch = 12;

    /// <summary>
    /// The modality the question rides on — <b>which sense is being asked about.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Ostension, and the plan calls it the signal rather than a shortcut.</b> Being told
    /// which sense the question is about is information no amount of co-occurrence contains,
    /// and it is the pointing-and-naming shape. Without it the world asks nothing: a moment
    /// of a sight and a sound has three things it could be asked and no way to say which, so
    /// every arm would be scored on a question nobody put.
    /// </para>
    /// <para>
    /// <b>It says what is being looked at and never what to conclude</b>, which is the line a
    /// world's report has to stay on. <i>You are being asked about touch</i> is the same
    /// standing as <see cref="Coded.Statements"/>'s <i>these codes were one object</i>; what
    /// nothing here says is which code answers it.
    /// </para>
    /// </remarks>
    public const byte Asks = 14;

    /// <summary>
    /// Something present and irrelevant. <b>A modality of its own, so it can
    /// never be an answer</b> — see <see cref="SensesSettings.Clutter"/>.
    /// </summary>
    private const byte Aside = 13;

    private readonly SensesSettings _settings;
    private readonly Random _rng;

    /// <summary>Draws the clutter, and <b>nothing else</b>.</summary>
    private readonly Random _aside;

    /// <summary>
    /// The cumulative Zipf weights over the pool. <b>Null when the draw is
    /// uniform</b> — see <see cref="SensesSettings.Skew"/>.
    /// </summary>
    /// <remarks>
    /// <b>Built once and never refitted, which the codes rule requires.</b> It is
    /// a property of the world's settings rather than of anything observed, so two
    /// machines given the same settings build the same table.
    /// </remarks>
    private readonly double[]? _ranks;

    /// <summary>Draws the held-out questions, and <b>nothing else</b>.</summary>
    private readonly Random _quizzing;

    private readonly List<Turn<Coded>> _kept = [];

    public Senses(SensesSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Concepts, 2);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.CodesPerSense);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Noise);

        ArgumentOutOfRangeException.ThrowIfNegative(settings.Clutter);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Pool);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Skew);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Withheld);

        // An arm that looks distinct and is not is how this project has fooled
        // itself before: clutter drawn from an empty pool is no clutter at all.
        if (settings.Clutter > 0 && settings.Pool <= 0)
            throw new ArgumentException(
                "clutter needs a pool to draw from", nameof(settings));

        // And a skew over nothing is the same fault one step along. A pool of one
        // is uniform whatever the exponent says, so a run configured that way
        // would report a heavy tail it does not have.
        if (settings.Skew > 0.0 && settings.Pool < 2)
            throw new ArgumentException(
                "a skew needs a pool of at least two to be unequal over",
                nameof(settings));

        _settings = settings;
        _rng = new Random(seed);

        // ITS OWN STREAM, so adding clutter does not move the task's draws. The
        // arms must see the same concepts in the same order or a change in score
        // has two explanations instead of one -- see Seeds.Apart.
        _aside = new Random(Seeds.Apart(seed, 0xA51D_E001));

        // And the examination gets a third stream, for the same reason. Building the held
        // turns off `_rng` would make how many were held decide which concepts the run then
        // saw, so two arms differing only in `Withheld` would be two different worlds.
        _quizzing = new Random(Seeds.Apart(seed, 0xA51D_E002));

        _ranks = settings.Skew > 0.0 ? Ranks(settings.Pool, settings.Skew) : null;

        for (var back = 0; back < settings.Withheld; back++) _kept.Add(Quiz());
    }

    /// <summary>
    /// The cumulative <c>1/k^skew</c> weights over a pool, normalised to end at
    /// one.
    /// </summary>
    /// <remarks>
    /// <b>The table rather than a closed form</b>, because the closed form is only
    /// approximate. Inverse transform on a continuous approximation would give
    /// a distribution close to Zipf and identical on no machine — fork 12 wants a
    /// seed to reproduce a run exactly, and a table plus a binary search does that
    /// for the cost of one array the size of the pool.
    /// </remarks>
    private static double[] Ranks(int pool, double skew)
    {
        var cumulative = new double[pool];
        var total = 0.0;

        for (var k = 0; k < pool; k++)
        {
            total += 1.0 / Math.Pow(k + 1, skew);
            cumulative[k] = total;
        }

        for (var k = 0; k < pool; k++) cumulative[k] /= total;

        return cumulative;
    }

    /// <summary>How many things there are to know about.</summary>
    public int Concepts => _settings.Concepts;

    /// <summary>Every code any sense can produce, which is the answer alphabet.</summary>
    /// <remarks>
    /// <b>One alphabet over all three senses</b>, so the sense a question asks about is
    /// something the machine says rather than something the scoring assumes. An outcome
    /// space per sense would make an answer in the wrong modality unexpressible, and
    /// answering in the wrong modality is the failure this world is most likely to have.
    /// </remarks>
    public int Outcomes => 3 * _settings.Concepts * _settings.CodesPerSense;

    /// <summary>What a blind guess is worth.</summary>
    public double Chance => 1.0 / Outcomes;

    /// <summary>
    /// What a perfect reader is worth — <b>a ceiling by construction and not a
    /// measurement.</b>
    /// </summary>
    /// <remarks>
    /// <b>The answer is drawn uniformly</b> among the asked sense's codes for that concept,
    /// so knowing the concept and the sense exactly still leaves a draw of one in
    /// <see cref="SensesSettings.CodesPerSense"/>. Nothing in the moment says which of them
    /// is coming, and a score read against one rather than against this would call a perfect
    /// population half wrong.
    /// </remarks>
    public double Ceiling => 1.0 / _settings.CodesPerSense;

    /// <inheritdoc/>
    public IReadOnlyList<Turn<Coded>> Withheld => _kept;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>A pair, a question, and the answer</b> — one of the two senses shown is named and
    /// the answer is in it. The question is what makes the world askable at all — see
    /// <see cref="Asks"/> — and the asked sense is always one that is present, so a drawn
    /// round never rehearses the examination.
    /// </remarks>
    public Turn<Coded> Next()
    {
        var concept = _rng.Next(_settings.Concepts);
        var pairing = _rng.Next(2);
        var (first, second) = pairing == 0 ? (Sight, Sound) : (Sound, Touch);

        var those = _settings.Scrambled ? _rng.Next(_settings.Concepts) : concept;

        var shown = new List<Code> { Pick(first, concept, _rng), Pick(second, those, _rng) };

        // Which of the two is being asked about, and never the third. Asking about the sense
        // that is absent is the examination, and a drawn round that did it would be training
        // on the exam.
        var asked = _rng.Next(2) == 0 ? first : second;

        return Asking(shown, asked, asked == first ? concept : those, _rng);
    }

    /// <summary>
    /// One held-out question: a sight, a sound, and <b>what does it feel like.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The combination the stream never draws</b>, and a memoriser scores nought on it by
    /// construction. Touch is only ever shown beside sound, and touch is only ever asked
    /// about in an occasion holding one — so a moment holding a sight and asking about touch
    /// is a shape the population has been scored on nought times.
    /// </para>
    /// <para>
    /// <b>The sound is the only route and the sight is the distractor</b>, which is what
    /// this measures rather than a limit on it. A subset test cannot walk, so composition
    /// here is a rule keyed on the code the two occasion types share, winning a vote against
    /// everything the sight code has ever advocated. Whether it does is the reading.
    /// </para>
    /// </remarks>
    private Turn<Coded> Quiz()
    {
        var concept = _quizzing.Next(_settings.Concepts);
        var those = _settings.Scrambled ? _quizzing.Next(_settings.Concepts) : concept;

        var shown = new List<Code>
        {
            Pick(Sight, concept, _quizzing),
            Pick(Sound, those, _quizzing),
        };

        // Answered through the SOUND's concept, because the sound is the only code the
        // learner could have met beside a touch. Scoring it against the sight's concept
        // would make the control arm's world unanswerable for a second reason and the two
        // would be inseparable.
        return Asking(shown, Touch, those, _quizzing);
    }

    /// <summary>One moment, with the noise, the clutter and the question on it.</summary>
    /// <param name="shown">The senses that are present.</param>
    /// <param name="asked">Which sense the question is about.</param>
    /// <param name="about">Which concept the answer belongs to.</param>
    /// <param name="rng">Which stream this turn draws from.</param>
    private Turn<Coded> Asking(List<Code> shown, byte asked, int about, Random rng)
    {
        if (rng.NextDouble() < _settings.Noise)
            shown.Add(Pick(
                rng.Next(2) == 0 ? shown[0].Modality : shown[1].Modality,
                rng.Next(_settings.Concepts),
                rng));

        // Present and irrelevant. Distinct within the moment, or a repeat would be one code
        // counted twice rather than two things being here.
        for (var i = 0; i < _settings.Clutter; i++)
        {
            var aside = new Code(Aside, (ulong)Irrelevant());
            if (!shown.Contains(aside)) shown.Add(aside);
        }

        shown.Add(new Code(Asks, asked));

        return new Turn<Coded>
        {
            Seen = Coded.Of(shown),
            Outcome = Answer(Pick(asked, about, rng)),
        };
    }

    /// <summary>Where a code sits in the answer alphabet.</summary>
    /// <param name="code">A code some sense produced.</param>
    /// <remarks>
    /// <b>Derived from the code and never from a counter</b>, so the same code is the same
    /// answer on every machine and in every run. A table numbered by order of first
    /// appearance would make two runs of one seed disagree about what the machine said.
    /// </remarks>
    private int Answer(Code code)
    {
        var sense = code.Modality == Sight ? 0 : code.Modality == Sound ? 1 : 2;
        var concept = Kinds.Of(code);
        var slot = (int)(code.Value % 1000);

        return (((sense * _settings.Concepts) + concept) * _settings.CodesPerSense) + slot;
    }

    /// <summary>Every code one sense produces for one concept.</summary>
    public IReadOnlyList<Code> Of(byte sense, int concept)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(concept, _settings.Concepts);

        return Kinds.All(sense, concept, _settings.CodesPerSense);
    }

    /// <summary>Which concept a code belongs to, whatever sense it came from.</summary>
    public static int Concept(Code code) => Kinds.Of(code);

    /// <summary>
    /// One moment: two senses of one concept, sometimes with a stray code from
    /// another.
    /// </summary>
    /// <remarks>
    /// <b>Never sight with touch.</b> That absence is the whole experiment, and
    /// it is enforced here rather than left to a caller to respect.
    /// </remarks>
    public IReadOnlyCollection<Code> Moment()
    {
        var concept = _rng.Next(_settings.Concepts);
        var pairing = _rng.Next(2);
        var (first, second) = pairing == 0 ? (Sight, Sound) : (Sound, Touch);

        // Under the control the two senses belong to different things, so
        // sight -> sound -> touch leads somewhere unrelated.
        var codes = new List<Code>
        {
            Pick(first, concept, _rng),
            Pick(second, _settings.Scrambled ? _rng.Next(_settings.Concepts) : concept, _rng),
        };

        if (_rng.NextDouble() < _settings.Noise)
            codes.Add(Pick(
                _rng.Next(2) == 0 ? first : second, _rng.Next(_settings.Concepts), _rng));

        // PRESENT AND IRRELEVANT. Distinct within the moment, or a repeat would
        // be one code counted twice rather than two things being here.
        for (var i = 0; i < _settings.Clutter; i++)
        {
            var aside = new Code(Aside, (ulong)Irrelevant());
            if (!codes.Contains(aside)) codes.Add(aside);
        }

        return codes;
    }

    /// <summary>Which irrelevant code turns up, uniform or skewed.</summary>
    /// <remarks>
    /// <b>The uniform branch is the ORIGINAL CALL</b>, not an equivalent of it.
    /// A Zipf table with a zero exponent draws uniformly too, but off
    /// <see cref="Random.NextDouble"/> rather than <see cref="Random.Next(int)"/> —
    /// so every clutter measurement already taken would shift under a change that
    /// reads as a no-op. See <see cref="SensesSettings.Skew"/>.
    /// </remarks>
    private int Irrelevant()
    {
        if (_ranks is null) return _aside.Next(_settings.Pool);

        var drawn = _aside.NextDouble();

        // The first rank whose cumulative weight covers the draw. `BinarySearch`
        // returns the complement of the insertion point when there is no exact
        // hit, which is that rank; an exact hit is that rank too.
        var found = Array.BinarySearch(_ranks, drawn);
        var rank = found >= 0 ? found : ~found;

        return Math.Min(rank, _settings.Pool - 1);
    }

    private Code Pick(byte sense, int concept, Random rng) =>
        Kinds.Pick(sense, concept, _settings.CodesPerSense, rng);
}
