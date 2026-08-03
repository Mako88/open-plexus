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
    /// <b>THE SIZE DIAL, AND IT IS DELIBERATELY NOT A NEW WORLD.</b> Scale is a
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
    /// <b>THE SHARP END OF THE DIAL, BECAUSE THE TWO EXTREMES TEST OPPOSITE
    /// CLAIMS.</b> A large pool makes each irrelevant code RARE, so the graph
    /// grows wide and every clutter partner is thin — that tests whether cost
    /// stays affordable. A small pool makes them UBIQUITOUS, which manufactures
    /// exactly the ever-present background the forward weighting exists to
    /// refuse: <c>together(here, other) / seen(other)</c> should score a code
    /// present at every moment as a weak partner however often it co-occurs.
    /// <b>That claim has never been tested at a size where it could fail.</b>
    /// </remarks>
    public int Pool { get; init; }
}

/// <summary>
/// Three senses, and a thing that is never shown to two of them at once.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE SECOND WORLD, AND IT SHARES NO CODE WITH SNAKE.</b> No space, no
/// movement, no actions, no energy, nothing to lose, no time pressure. If a
/// finding holds here as well, it was about the architecture; if it does not, it
/// was about snake — and every number in this project so far is one world wide.
/// </para>
/// <para>
/// <b>THE TASK IS WHAT THE DESIGN EXISTS FOR.</b> An occasion shows either
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
    public const byte Aside = 13;

    private readonly SensesSettings _settings;
    private readonly Random _rng;

    /// <summary>Draws the clutter, and <b>nothing else</b>.</summary>
    private readonly Random _aside;

    public Senses(SensesSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Concepts, 2);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.CodesPerSense);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Noise);

        ArgumentOutOfRangeException.ThrowIfNegative(settings.Clutter);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Pool);

        // An arm that looks distinct and is not is how this project has fooled
        // itself before: clutter drawn from an empty pool is no clutter at all.
        if (settings.Clutter > 0 && settings.Pool <= 0)
            throw new ArgumentException(
                "clutter needs a pool to draw from", nameof(settings));

        _settings = settings;
        _rng = new Random(seed);

        // ITS OWN STREAM, so adding clutter does not move the task's draws. The
        // arms must see the same concepts in the same order or a change in score
        // has two explanations instead of one -- see Seeds.Apart.
        _aside = new Random(Seeds.Apart(seed, 0xA51D_E001));
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
            var aside = new Code(Aside, (ulong)_aside.Next(_settings.Pool));
            if (!codes.Contains(aside)) codes.Add(aside);
        }

        return codes;
    }

    private Code Pick(byte sense, int concept) =>
        Kinds.Pick(sense, concept, _settings.CodesPerSense, _rng);
}
