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
    /// <b>The sharp end of the dial, because the two extremes test opposite
    /// claims.</b> A large pool makes each irrelevant code RARE, so the graph
    /// grows wide and every clutter partner is thin — that tests whether cost
    /// stays affordable. A small pool makes them UBIQUITOUS, which manufactures
    /// exactly the ever-present background the forward weighting exists to
    /// refuse: <c>together(here, other) / seen(other)</c> should score a code
    /// present at every moment as a weak partner however often it co-occurs.
    /// <b>That claim has never been tested at a size where it could fail.</b>
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
    /// <b>And <see cref="Pool"/> alone cannot make that shape, because its two
    /// extremes are opposite and both UNIFORM.</b> A small pool makes every
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
public sealed class Senses
{
    /// <summary>What a thing looks like.</summary>
    public const byte Sight = 10;

    /// <summary>What a thing sounds like.</summary>
    public const byte Sound = 11;

    /// <summary>What a thing feels like.</summary>
    public const byte Touch = 12;

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

    public Senses(SensesSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Concepts, 2);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.CodesPerSense);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Noise);

        ArgumentOutOfRangeException.ThrowIfNegative(settings.Clutter);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Pool);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Skew);

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

        _ranks = settings.Skew > 0.0 ? Ranks(settings.Pool, settings.Skew) : null;
    }

    /// <summary>
    /// The cumulative <c>1/k^skew</c> weights over a pool, normalised to end at
    /// one.
    /// </summary>
    /// <remarks>
    /// <b>The table rather than a closed form, because the closed form is only
    /// approximate.</b> Inverse transform on a continuous approximation would give
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

    /// <summary>What a blind guess at one touch code is worth.</summary>
    public double Chance => 1.0 / _settings.Concepts;

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
            Pick(first, concept),
            Pick(second, _settings.Scrambled ? _rng.Next(_settings.Concepts) : concept),
        };

        if (_rng.NextDouble() < _settings.Noise)
            codes.Add(Pick(_rng.Next(2) == 0 ? first : second, _rng.Next(_settings.Concepts)));

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
    /// <b>The uniform branch is the ORIGINAL CALL and not an equivalent of it.</b>
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

    private Code Pick(byte sense, int concept) =>
        Kinds.Pick(sense, concept, _settings.CodesPerSense, _rng);
}
