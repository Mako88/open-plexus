using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>How the returning world is set up. Every number named and none defaulted.</summary>
public sealed record ReturningSettings
{
    /// <summary>How many individuals the room holds.</summary>
    /// <remarks>
    /// <b>Individuals and not kinds</b>, which is the whole of what this world is for. Every
    /// other world here shows a fresh draw from a distribution; this one shows THE SAME
    /// THINGS AGAIN, so a moment can be about something that was already met.
    /// </remarks>
    public required int Things { get; init; }

    /// <summary>How many visible attributes a thing has.</summary>
    public required int Attributes { get; init; }

    /// <summary>How many codes one attribute of one thing may show.</summary>
    /// <remarks>
    /// <b>More than one, or recognition is a lookup.</b> The same reason
    /// <see cref="BindingSettings.CodesPerAttribute"/> exists: an attribute showing one
    /// code every time makes <i>is this the thing I saw</i> a string comparison, and the
    /// question is whether identity survives the thing looking slightly different.
    /// </remarks>
    public required int CodesPerAttribute { get; init; }

    /// <summary>How many values the hidden attribute may take.</summary>
    public required int Hidden { get; init; }

    /// <summary>
    /// Whether the things come in pairs that look identical and differ in what is hidden.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The cell the whole world exists for, and without it every score here is a kind's
    /// rather than an individual's.</b> A thing that looks unlike everything else can be
    /// re-found by its appearance, and a rule keyed on appearance is a narrow CATEGORY that
    /// this design already builds. Two things that look the same and are not is where a
    /// category runs out and a referent is the only thing left.
    /// </para>
    /// <para>
    /// <b>So the untagged arm must fall to the pair's base rate here, by construction.</b>
    /// That is not a failure to be repaired — it is the measurement. What an individual is
    /// WORTH is the distance from it to the arm that is handed one.
    /// </para>
    /// </remarks>
    public bool Twinned { get; init; }

    /// <summary>
    /// Whether a contentless index for the thing is shown — <b>identity handed over.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The ceiling arm, and it is a thing no camera can do.</b> A front end may say
    /// <i>these codes were one object</i> and this design grants it that; saying <i>and it
    /// is the one you saw on Tuesday</i> is the answer rather than the signal. John's
    /// ordering, 2026-08-12: point a phone at a basket, look away, look back, and nothing
    /// outside the learner may say it is the same basket.
    /// </para>
    /// <para>
    /// <b>It is here anyway because a gap needs two ends.</b> Fork 88's number is what it is
    /// because the selection was handed over and the score compared; this is the same
    /// instrument aimed at identity. An arm nobody may ship is still the arm that says what
    /// shipping one would be worth.
    /// </para>
    /// </remarks>
    public bool Tagged { get; init; }

    /// <summary>
    /// Whether a thing is shown with the landmark it keeps company with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>John's own account of what an individual is, made measurable.</b> A concept here
    /// is not a thing stored somewhere — it is codes and an awareness of how they stand to
    /// each other. If an individual works the same way then what makes two identical
    /// baskets two baskets is not how they look, which is by construction the same, but
    /// WHERE EACH ONE STANDS. Twins get different landmarks, so a relation separates what
    /// appearance cannot.
    /// </para>
    /// <para>
    /// <b>And it is an axis rather than a third arm, because it has to be read against
    /// both.</b> Against the anonymous twinned cell it says whether a relation recovers
    /// what appearance lost; against the tagged one it says how much of a handed index it
    /// recovers. One number without the other two is a score with nothing to mean.
    /// </para>
    /// <para>
    /// <b>The landmark is fixed for the life of the world.</b> Which is the honest limit and is
    /// said here rather than discovered later. A basket that never moves is pinned by a
    /// conjunction, and a conjunction is rung one and already built — so a win in this cell
    /// is a claim about RELATIONS carrying identity and not about anything tracking a thing
    /// through change. A landmark that moved would break the conjunction and is the next
    /// arm rather than this one.
    /// </para>
    /// </remarks>
    public bool Placed { get; init; }

    /// <summary>
    /// How often the next sighting is the SAME thing as the last — <b>fork 106, and the one
    /// thing this world withheld that a category could have used.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>John's: collapse categories over time as well as space.</b> A derivation over a bag
    /// of moments sees no order, which is exactly why twins merge — every statistic over the
    /// moments is the same for both by construction. Sightings that come in RUNS put a
    /// thing's own codes next to each other in time and nobody else's, which is a fact about
    /// the stream rather than about any moment in it.
    /// </para>
    /// <para>
    /// <b>And nought is the uniform draw every reading before this was taken under</b>, so
    /// the world is unchanged where this is not set and the two are one axis rather than two
    /// worlds. What it does NOT do is make a thing easier to answer about: the hidden
    /// attribute, the looks and the landmarks are drawn exactly as they were, and a learner
    /// seeing one moment at a time is handed nothing at all.
    /// </para>
    /// <para>
    /// <b>So it is continuity without motion, which is the honest limit.</b> A thing's
    /// landmark is still fixed for the life of the world, so what runs give is repeated
    /// sightings of one thing rather than a thing moving between places. Whether a MOVING
    /// landmark is what an individual needs is the arm after this one, and it is the arm
    /// this world's own doc already names.
    /// </para>
    /// </remarks>
    public double Wandering { get; init; }

    /// <summary>
    /// How often a thing that is met has MOVED since it was last met — <b>the control that
    /// says whether continuity recovers a thing or a place it happens to sit in.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This world's own doc names the limit and this is it arriving.</b> A landmark fixed
    /// for the life of the world is pinned by a CONJUNCTION, which is rung one and built —
    /// so recovering a thing from a fixed landmark is a claim about relations carrying
    /// identity and not about anything tracking a thing through change.
    /// </para>
    /// <para>
    /// <b>And it is the one arm that could refute what adhesion found.</b> If a thing's
    /// codes only adhere because the thing never moves, then what a derivation over an
    /// ordered stream reaches is a PLACE — and a place is shared by whoever stands in it, so
    /// it predicts a thing's hidden attribute exactly as long as nobody else has stood there.
    /// </para>
    /// <para>
    /// <b>Nought is the fixed landmark every earlier reading was taken under</b>, and the
    /// places are the things' own, so an alphabet's size never moves with this.
    /// </para>
    /// </remarks>
    public double Drifting { get; init; }

    /// <summary>How many sightings are kept back and never drawn.</summary>
    public required int Withheld { get; init; }
}

/// <summary>
/// The same things, met again — <b>the world where a referent is the only thing that could
/// answer.</b> And no front end is allowed to supply one.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's, 2026-08-12, and it is the basket.</b> Point a phone at a white basket, point
/// it somewhere else, point it back. Knowing it is the same basket is not recognising a
/// KIND — the room may hold two white baskets — and nothing in this design reaches it.
/// Rung five names what CO-FIRES, and a basket at two moments never co-occurs with itself.
/// </para>
/// <para>
/// <b>So the problem is posed as a prediction and never as a judgement of identity.</b>
/// Asking the learner <i>is this the same one</i> would need an answer channel about
/// identity, which is the conclusion being handed over in a different envelope. Instead a
/// thing has an attribute that is never shown, and predicting it is possible exactly to the
/// extent that the thing in front of you has been RE-FOUND. Identity is what the score is
/// evidence of, rather than what the question asks about.
/// </para>
/// <para>
/// <b>And the twins are what make that evidence mean anything.</b> Untwinned, appearance
/// decides and a conjunctive scope over the look is a complete answer — the world would be
/// scoring category formation and reading as identity. Twinned, appearance is exhausted by
/// construction and the pair's base rate is the ceiling on anything that has only ever seen
/// a moment at a time.
/// </para>
/// <para>
/// <b>What is not built here, deliberately, is the continuity that would let a learner
/// recover the gap.</b> Company, trajectory, a room that changes slowly — each is a way an
/// individual could be pinned by its relations rather than its looks, and each is an arm to
/// run once the gap is measured. Measuring the gap first is what stops the first mechanism
/// tried from being scored against no baseline at all.
/// </para>
/// </remarks>
public sealed class Returning : IWorld<Coded>, IWithholds<Coded>
{
    /// <summary>The modality a visible attribute rides on.</summary>
    private const byte Look = 23;

    /// <summary>
    /// The modality a landmark rides on — <b>where a thing stands, never what it is.</b>
    /// </summary>
    /// <remarks>
    /// <b>Its own, so a place is not mistakeable for an appearance.</b> Sharing
    /// <see cref="Look"/> would make <i>the white one</i> and <i>the one by the door</i> the
    /// same kind of fact, and the whole question is whether the second does something the
    /// first cannot.
    /// </remarks>
    private const byte Beside = 25;

    /// <summary>
    /// The modality a handed-over index rides on. <b>Contentless on purpose</b> — it says
    /// which thing and nothing whatever about it, so an arm that uses it is using identity
    /// and never appearance.
    /// </summary>
    private const byte Named = 24;

    private readonly ReturningSettings _settings;
    private readonly Random _sightings;

    /// <summary>What each thing's hidden attribute is, by thing.</summary>
    private readonly int[] _hidden;

    /// <summary>Which appearance each thing wears, by thing — <b>twins share one.</b></summary>
    private readonly int[] _wearing;

    /// <summary>Where each thing stands, by thing.</summary>
    /// <inheritdoc cref="ReturningSettings.Drifting"/>
    private readonly int[] _standing;

    private readonly List<Turn<Coded>> _kept = [];

    /// <summary>Which thing was met last, or -1 before anything has been.</summary>
    /// <inheritdoc cref="ReturningSettings.Wandering"/>
    private int _last = -1;

    /// <param name="settings">How the world is set up.</param>
    /// <param name="seed">What draws the sightings.</param>
    public Returning(ReturningSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Things, 2);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Attributes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.CodesPerAttribute);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Hidden, 2);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Withheld);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Wandering);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(settings.Wandering, 1.0);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Drifting);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Drifting, 1.0);

        if (settings.Twinned && settings.Things % 2 != 0)
            throw new ArgumentException(
                "twins come in pairs, so an odd room leaves one thing without one and the "
                + "base rate the untwinned half sits at is a different number",
                nameof(settings));

        _settings = settings;
        _sightings = new Random(seed);

        _hidden = new int[settings.Things];
        _wearing = new int[settings.Things];
        _standing = new int[settings.Things];

        for (var thing = 0; thing < settings.Things; thing++)
        {
            // Each thing in its own place to begin with, so a world that never drifts is
            // bit-identical to the one that had no places to stand in at all.
            _standing[thing] = thing;

            // Twins wear one appearance and carry different answers, which is the whole
            // construction. Untwinned, a thing's appearance is its own and the look is a
            // complete key -- so the two arms differ in exactly one fact about the world
            // and in nothing the world shows.
            _wearing[thing] = settings.Twinned ? thing / 2 : thing;

            _hidden[thing] = settings.Twinned
                ? (thing % 2) % settings.Hidden
                : _sightings.Next(settings.Hidden);
        }

        for (var back = 0; back < settings.Withheld; back++) _kept.Add(Draw());
    }

    /// <inheritdoc/>
    public int Outcomes => _settings.Hidden;

    /// <inheritdoc/>
    public IReadOnlyList<Turn<Coded>> Withheld => _kept;

    /// <summary>
    /// What a blind draw would score, and what appearance ALONE can reach.
    /// </summary>
    /// <remarks>
    /// <b>Two bars rather than one, because the interesting arm sits between them.</b>
    /// Untwinned, a perfect reader of appearance answers everything and the bar is one.
    /// Twinned, appearance narrows the answer to a PAIR and no further, so the ceiling on
    /// anything without a referent is the better of the two hidden values within a pair —
    /// which is what this reports and what an individual has to beat to be worth minting.
    /// </remarks>
    public double Appearance => _settings.Twinned ? 0.5 : 1.0;

    /// <inheritdoc/>
    public Turn<Coded> Next() => Draw();

    /// <summary>One sighting of one thing, uniformly or in a run.</summary>
    /// <remarks>
    /// <b>The run is drawn before anything else is.</b> So a sighting's look and landmark are
    /// exactly what they would have been. Continuity decides WHICH thing is met and
    /// changes nothing about how a met thing is shown — otherwise the arm would be handing
    /// over a second channel and reading it as order.
    /// </remarks>
    private Turn<Coded> Draw()
    {
        // The setting is tested before the generator is, which is not a micro-optimisation.
        // A `NextDouble` drawn and thrown away shifts the whole stream, so a world at nought
        // would be uniform and NOT the world every earlier reading was taken on -- and the
        // rule counts here moved by five per cent before this line was written that way.
        var thing = _settings.Wandering > 0.0
            && _last >= 0
            && _sightings.NextDouble() < _settings.Wandering
                ? _last
                : _sightings.Next(_settings.Things);

        _last = thing;

        var look = new HashSet<Code>();

        // One code drawn per attribute per sighting, so the same thing looks slightly
        // different every time it is met. Without that, re-finding a thing is comparing two
        // identical sets and the world would be asking nothing.
        for (var attribute = 0; attribute < _settings.Attributes; attribute++)
            look.Add(Kinds.Pick(
                Look,
                (_wearing[thing] * _settings.Attributes) + attribute,
                _settings.CodesPerAttribute,
                _sightings));

        // One code and only one, drawn from an alphabet of one, so the index is exactly
        // as contentless as it claims to be -- a thing's index never varies and says
        // nothing about the thing, which is what makes this arm a ceiling rather than a
        // second appearance channel.
        // Where it stands, drawn the same way its look is. A landmark that showed one code
        // every time would make the relation sharper than the appearance it is being
        // compared against, and the comparison would be about the noise rather than about
        // what the two channels can carry.
        // And it may have moved since it was last met, which is drawn per SIGHTING rather
        // than per round -- a thing nobody is looking at has no sightings to be inconsistent
        // between, so moving it then would be a change the stream could never show.
        if (_settings.Drifting > 0.0 && _sightings.NextDouble() < _settings.Drifting)
            _standing[thing] = _sightings.Next(_settings.Things);

        if (_settings.Placed)
            look.Add(Kinds.Pick(
                Beside, _standing[thing], _settings.CodesPerAttribute, _sightings));

        if (_settings.Tagged) look.Add(Kinds.Pick(Named, thing, 1, _sightings));

        return new Turn<Coded>
        {
            Seen = Coded.Of(look),
            Outcome = _hidden[thing],
        };
    }
}
