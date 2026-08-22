using System.Collections.Immutable;

namespace OpenPlexus.Worlds;

/// <summary>Which of the three MONK's problems a world is posing.</summary>
/// <remarks>
/// <b>All three, because one of them alone answers nothing.</b> The second is the
/// language probe and the other two are its controls: a learner that falls short on
/// <see cref="Two"/> has either hit the ceiling of what a conjunction can say or is
/// merely a poor learner, and those look identical from one number. <see cref="One"/>
/// is reachable by the scope language as it stands, so failing there means the learner;
/// failing only on <see cref="Two"/> means the language.
/// </remarks>
public enum Puzzle
{
    /// <summary><c>head = body</c> or <c>jacket is red</c>. Half the bag is positive.</summary>
    One = 1,

    /// <summary>
    /// <b>Exactly two of the six attributes hold their first value</b> — the counting
    /// concept, and the cheapest language ceiling there is.
    /// </summary>
    /// <remarks>
    /// A scope says <i>these codes are all present</i> and nothing else. <i>Exactly two
    /// of six</i> is not that shape at any depth: it is fifteen disjuncts, each of which
    /// must also deny the first value on the other four attributes. The population can
    /// still reach any accuracy it likes by naming instances one at a time — which is
    /// precisely the memorising this project measures rather than assumes, and here the
    /// bag is 432 so the naming can be counted exactly.
    /// </remarks>
    Two = 2,

    /// <summary><c>jacket is green and holding a sword</c>, or <c>jacket is not blue and body is not octagon</c>.</summary>
    Three = 3,
}

/// <summary>How one attribute and its value are said as codes.</summary>
/// <remarks>
/// <b>An arm, and it decides what rung four can reach here.</b> Monk-1's concept is
/// <i>head shape equals body shape</i>, which is a variable standing in two positions —
/// and whether that is sayable at all is a fact about the spelling rather than about the
/// learner. Fork 133.
/// </remarks>
public enum Spelling
{
    /// <summary>
    /// One modality, and the attribute packed into the value — <see cref="Codes.Bits"/>.
    /// </summary>
    /// <remarks>
    /// <b>So head-round and body-round are two values</b> and no variable joins them.
    /// <c>Commitments.Generalising</c> groups a scope's positions by the value they carry,
    /// and these carry different ones, so the hole that REPEATS is unreachable however
    /// many siblings the population holds.
    /// </remarks>
    Fused,

    /// <summary>
    /// A modality an attribute, and the value standing alone — <see cref="Codes.Slotted"/>.
    /// </summary>
    /// <remarks>
    /// <b>So head-round and body-round are one value under two modalities</b>, which is
    /// what <c>Commitments.Unifying</c> matches: two entries carrying one name are filled
    /// by one value, so <i>whichever shape the head has, and the body has that same one</i>
    /// is a scope.
    /// </remarks>
    Split,
}

/// <summary>Which MONK's problem, and how much of the bag is never drawn.</summary>
public sealed record MonkSettings
{
    /// <summary>Which of the three.</summary>
    public Puzzle Puzzle { get; init; } = Puzzle.Two;

    /// <summary>How an attribute and its value are spelt. See <see cref="Worlds.Spelling"/>.</summary>
    public Spelling Spelling { get; init; } = Spelling.Fused;

    /// <summary>
    /// How many of the 432 to load, score and never draw.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Taken at a fixed stride through the enumeration</b>, so the split is a rule rather
    /// than a sample. A withheld set drawn by the world's own generator would move with the
    /// seed and two seeds would be scored against two different questions; a stride is the
    /// same on every machine and for every seed, which is what that objection actually wanted.
    /// </para>
    /// <para>
    /// <b>And it was taken from the END</b>, which held back an attribute value entire. The
    /// enumeration's slowest position is the head shape, so the last 132 of 432 are every
    /// instance whose head is its third value and nothing else — 12 of the 300 drawn carry it.
    /// Every held-out score this world ever reported was therefore about one unseen value, and
    /// on <see cref="Puzzle.One"/> it read 0.7273 on six runs of two arms and three seeds,
    /// which is exactly what <i>the jacket is red, else no</i> scores there. A number that
    /// cannot move reads as a learner that will not generalise.
    /// </para>
    /// </remarks>
    public int Withheld { get; init; } = 132;
}

/// <summary>
/// The MONK's problems — 432 robots, six attributes, and one concept a conjunction
/// cannot say.
/// </summary>
/// <remarks>
/// <para>
/// <b>The cheapest language-ceiling probe there is.</b> And the plan has called it a day's
/// work since the ladder was written. Step eight is <i>the rung the failures
/// demand, and never the rung that sounds next</i> — and no failure has asked yet,
/// because on the one world that can say, twelve one-code rules cover everything held
/// out. This is a world built so a failure can ask.
/// </para>
/// <para>
/// <b>The bag is 432 and that is the whole attraction.</b> Every instrument this project
/// wants is exact here rather than estimated: the concept can be enumerated, so soundness
/// is decidable; the bag is finite, so <see cref="IWithholds{TSeen}"/> gives a real
/// held-out set; and the majority class is countable, so the bar a silent arm drifts
/// toward is a number rather than a guess.
/// </para>
/// <para>
/// <b>And the bar is not a half.</b> Which is the trap this world sets for anybody reading
/// its score. <see cref="Puzzle.Two"/> is 142 positive of 432, so ALWAYS SAYING NO
/// SCORES 0.6713. A run reporting 0.68 on it has learnt approximately nothing and looks
/// like it has learnt a great deal — the fallback-as-control-arm trap, with the
/// arithmetic already done for it. <see cref="Chance"/> is that number, reported beside
/// every score rather than left to the reader.
/// </para>
/// <para>
/// <b>Values are nought-based here and one-based in the literature.</b> The published
/// attributes run 1..3; <see cref="Turn{TSeen}.Seen"/> carries 0..2 so
/// <see cref="Codes.Bits"/> packs them against a stride of <see cref="Stride"/> with no
/// hole, and <i>first value</i> means nought throughout this file.
/// </para>
/// </remarks>
public sealed class Monk : IWorld<IReadOnlyList<int>>, IWithholds<IReadOnlyList<int>>
{
    /// <summary>How many values each of the six attributes may take.</summary>
    /// <remarks>
    /// Head shape, body shape, smiling, holding, jacket colour, tie — in the published
    /// order, which is the order the concepts are written against.
    /// </remarks>
    public static ImmutableArray<int> Widths => [3, 3, 2, 3, 4, 2];

    /// <summary>One more than the largest value an attribute may hold.</summary>
    /// <remarks>
    /// <b>Four, because the jacket has four colours</b> — and it is stated once here
    /// rather than at each call, since a stride that disagreed with the widths would
    /// silently conflate two attributes. That is the aliasing fault
    /// <see cref="Codes.Bits"/> already carried once.
    /// </remarks>
    public static int Stride => 4;

    /// <summary>The modality an attribute rides on, and under a split spelling the first of six.</summary>
    /// <remarks>
    /// <b><see cref="Spelling.Split"/> claims <see cref="Widths"/>-many numbers from here</b>,
    /// so 150 to 155 belong to this world. Only one is declared because <c>SeparationTests</c>
    /// reads declarations and a world may reuse another world's number freely; what it may not
    /// take is one of the brain's, and <c>MonkTests</c> asserts the whole run stays clear of
    /// them.
    /// </remarks>
    public const byte Attribute = 150;

    /// <summary>The modality the answer rides on.</summary>
    /// <remarks>
    /// <b>101, which is the brain's own</b>, and this file may not say so out loud. The
    /// outcome alphabet is shared across every world — a brain that learnt a different
    /// one per world would not be one brain — and <c>SeparationTests</c> fails the build
    /// if a world names a brain type, so the number is written here and that same test
    /// asserts the two agree. <see cref="Multiplexer.Said"/> carries the identical
    /// duplication for the identical reason.
    /// </remarks>
    public const byte Answered = 101;

    /// <summary>The modality a scope entry naming a variable rides on.</summary>
    /// <remarks>
    /// <para>
    /// <b>212, on exactly the footing <see cref="Answered"/> is on.</b> An answer key has to
    /// be written in the population's alphabet or it marks the subject wrong, and a key that
    /// refused this modality would call every rule rung four builds unsound.
    /// <see cref="Multiplexer.Whatever"/> is the same copy for the same reason, and
    /// <c>SeparationTests</c> asserts both agree with the learner.
    /// </para>
    /// <para>
    /// <b>And this key BINDS one rather than passing over it</b>, which is where it parts
    /// from the multiplexer's. There a variable appears once, says <i>whichever code of this
    /// kind</i>, and claims nothing — so skipping it is exact. Under
    /// <see cref="Spelling.Split"/> a name repeats across two modalities and says <i>these
    /// two attributes hold one value</i>, which is a real constraint and the whole of what
    /// fork 133 asks about. Skipping it here would call <i>head equals body</i> sound of
    /// every instance.
    /// </para>
    /// </remarks>
    public const byte Whatever = 212;

    /// <summary>Which attribute a variable entry stands over, and which variable it is.</summary>
    /// <param name="entry">A scope entry on <see cref="Whatever"/>.</param>
    /// <remarks>
    /// <b>The modality rides in the high half</b> and the name in the low one, which is the
    /// learner's layout read rather than guessed — <c>SeparationTests</c> asserts this agrees
    /// with what the learner writes, so the two cannot drift apart in silence.
    /// </remarks>
    public static (byte Modality, int Name) Stands(Codes.Code entry) =>
        ((byte)(entry.Value >> 32), (int)(entry.Value & uint.MaxValue));

    private readonly IReadOnlyList<Turn<IReadOnlyList<int>>> _drawn;
    private readonly Random _rng;

    /// <param name="settings">Which problem, and how much is held back.</param>
    /// <param name="seed">The world's own generator, for the draw and nothing else.</param>
    /// <exception cref="ArgumentOutOfRangeException">More is withheld than exists.</exception>
    public Monk(MonkSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Withheld);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(settings.Withheld, Everything.Length);

        _rng = new Random(seed);

        var all = Everything;

        _drawn = [.. all
            .Where((_, at) => !Back(at, settings.Withheld))
            .Select(one => Turn(settings.Puzzle, one))];

        Withheld = [.. all
            .Where((_, at) => Back(at, settings.Withheld))
            .Select(one => Turn(settings.Puzzle, one))];

        Chance = Bar(settings.Puzzle);
        Spelling = settings.Spelling;
    }

    /// <summary>How this world spells an attribute and its value.</summary>
    public Spelling Spelling { get; }

    /// <inheritdoc/>
    public int Outcomes => 2;

    /// <inheritdoc/>
    public IReadOnlyList<Turn<IReadOnlyList<int>>> Withheld { get; }

    /// <summary>What always naming the commoner answer scores on the whole bag.</summary>
    /// <remarks>
    /// <b>The majority class and not a half.</b> See the note on the type: 0.6713 on
    /// <see cref="Puzzle.Two"/>, and a score read against 0.5 there would call a learner
    /// that has done nothing a success.
    /// </remarks>
    public double Chance { get; }

    /// <summary>One drawn instance, with replacement.</summary>
    public Turn<IReadOnlyList<int>> Next() => _drawn[_rng.Next(_drawn.Count)];

    /// <summary>Whether the instance at one position is held back.</summary>
    /// <param name="at">Which of the 432, in enumeration order.</param>
    /// <param name="withheld">How many to hold back.</param>
    /// <remarks>
    /// <b>A stride rather than a tail</b>, and it picks exactly <paramref name="withheld"/> of
    /// them however many that is. Walking the multiples of <paramref name="withheld"/> modulo
    /// the bag spreads the choice through every position of the enumeration, so no attribute
    /// value can end up on one side of the split — see the note on
    /// <see cref="MonkSettings.Withheld"/> for what taking the tail cost.
    /// </remarks>
    public static bool Back(int at, int withheld) =>
        withheld > 0 && (int)((long)at * withheld % Everything.Length) < withheld;

    /// <summary>Every one of the 432 instances, in a fixed order.</summary>
    /// <remarks>
    /// <b>The answer key, and it is why this world is worth building.</b> A soundness
    /// check needs to enumerate every instance a scope covers and ask whether the
    /// outcome is constant across them. On a world of photographs that is impossible and
    /// on this one it is six nested loops.
    /// </remarks>
    public static ImmutableArray<ImmutableArray<int>> Everything { get; } = Enumerate();

    /// <summary>Whether one instance is in the concept.</summary>
    /// <param name="puzzle">Which of the three.</param>
    /// <param name="instance">Six nought-based attribute values.</param>
    /// <exception cref="ArgumentOutOfRangeException">The instance is not six wide.</exception>
    public static bool Holds(Puzzle puzzle, IReadOnlyList<int> instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (instance.Count != Widths.Length)
            throw new ArgumentOutOfRangeException(
                nameof(instance), $"a MONK instance is {Widths.Length} attributes, not {instance.Count}");

        return puzzle switch
        {
            // head = body, or the jacket is red.
            Puzzle.One => instance[0] == instance[1] || instance[4] == 0,

            // EXACTLY TWO FIRST VALUES. The whole point of the file, in one line, and it
            // is a COUNT rather than a pattern -- which is exactly what a conjunction of
            // present codes has no way to say.
            Puzzle.Two => instance.Count(one => one == 0) == 2,

            // The jacket is green and it holds a sword, or the jacket is not blue and
            // the body is not an octagon.
            Puzzle.Three => (instance[4] == 2 && instance[3] == 0)
                || (instance[4] != 3 && instance[1] != 2),

            _ => throw new ArgumentOutOfRangeException(nameof(puzzle)),
        };
    }

    /// <summary>What always naming the commoner answer scores, counted rather than assumed.</summary>
    /// <param name="puzzle">Which of the three.</param>
    public static double Bar(Puzzle puzzle)
    {
        var holds = Everything.Count(one => Holds(puzzle, one));

        return Math.Max(holds, Everything.Length - holds) / (double)Everything.Length;
    }

    /// <summary>The code for one attribute holding one value.</summary>
    /// <param name="attribute">Which of the six, nought-based.</param>
    /// <param name="value">Which value it holds, nought-based.</param>
    /// <param name="spelling">How the two are said. See <see cref="Worlds.Spelling"/>.</param>
    public static Codes.Code Of(int attribute, int value, Spelling spelling = Spelling.Fused) =>
        spelling == Spelling.Split
            ? Codes.Slotted.Of(Attribute, attribute, value)
            : Codes.Bits.Of(Attribute, attribute, value, Stride);

    /// <summary>The code for what the concept says.</summary>
    /// <param name="holds">Whether the instance is in the concept.</param>
    public static Codes.Code Says(bool holds) => new(Answered, holds ? 1UL : 0UL);

    /// <summary>
    /// Whether this world can decide a scope exactly — <b>always, and that is the point
    /// of choosing it.</b>
    /// </summary>
    /// <param name="scope">The codes that must be present.</param>
    /// <remarks>
    /// <b><see cref="Multiplexer"/> has to refuse scopes with too many free bits</b> and
    /// this one never does. There are 432 instances however few attributes a scope
    /// pins, so enumeration is a walk of the whole bag rather than an exponential in
    /// what was left open. Every rule a run holds is checkable here, so
    /// <c>Learned.Unchecked</c> is nought by construction and a soundness count on this
    /// world is over ALL of the population rather than over the part that happened to be
    /// small enough.
    /// </remarks>
    /// <param name="spelling">How the world it came from spells an attribute.</param>
    public static bool Checkable(
        ImmutableArray<Codes.Code> scope, Spelling spelling = Spelling.Fused) =>
        !scope.IsDefaultOrEmpty && scope.All(code => Mine(code, spelling));

    /// <summary>Whether a code is one this world emits under a spelling.</summary>
    /// <param name="code">The code to ask about.</param>
    /// <param name="spelling">How the world spells an attribute.</param>
    /// <remarks>
    /// <b>A variable entry counts as mine</b>, or a rule rung four built would be reported
    /// as beyond the key rather than graded — and this world's whole claim is that nothing
    /// here is beyond the key.
    /// </remarks>
    private static bool Mine(Codes.Code code, Spelling spelling) =>
        code.Modality == Whatever
        || (spelling == Spelling.Split
            ? code.Modality >= Attribute && code.Modality < Attribute + Widths.Length
            : code.Modality == Attribute);

    /// <summary>Whether a scope really does entail an expectation, over the whole bag.</summary>
    /// <param name="puzzle">Which of the three.</param>
    /// <param name="scope">The codes that must be present.</param>
    /// <param name="expects">What is claimed to follow.</param>
    /// <remarks>
    /// <b>A scope pinning one attribute to two values is satisfied by nothing</b>, so it
    /// entails everything vacuously — and calling that sound would let a learner score by
    /// minting contradictions. It covers no instance, so the empty-coverage test below
    /// refuses it without needing to know why it was empty.
    /// </remarks>
    /// <param name="spelling">How the world it came from spells an attribute.</param>
    public static bool Sound(
        Puzzle puzzle,
        ImmutableArray<Codes.Code> scope,
        Codes.Code expects,
        Spelling spelling = Spelling.Fused)
    {
        if (!Checkable(scope, spelling)) return false;

        var covered = 0;

        foreach (var instance in Everything)
        {
            if (!Covers(scope, instance, spelling)) continue;

            covered++;

            if (Says(Holds(puzzle, instance)) != expects) return false;
        }

        return covered > 0;
    }

    /// <summary>
    /// The MINIMAL sound rules — every true conjunction with no true sub-conjunction.
    /// </summary>
    /// <param name="puzzle">Which of the three.</param>
    /// <remarks>
    /// <para>
    /// <b>Minimal rather than all, because subsumption is aimed at exactly this set.</b>
    /// A sound rule with a sound sub-scope says nothing the shorter one does not and
    /// covers less, which is the one case the design lets a general commitment replace a
    /// narrow one. So this is what a population that had finished compressing would
    /// hold, and counting how much of it is resident is a fair question. All 1,656 sound
    /// conjunctions on <see cref="Puzzle.One"/> is not.
    /// </para>
    /// <para>
    /// <b>And the size of this set is the whole finding.</b> 22 rules on
    /// <see cref="Puzzle.One"/> and 12 on <see cref="Puzzle.Three"/>, against 254 on
    /// <see cref="Puzzle.Two"/> — of which 142 are complete six-attribute instances,
    /// because the concept has no sound conjunction saying YES at any shorter depth.
    /// There is no compression available on that side at all.
    /// </para>
    /// </remarks>
    /// <param name="spelling">How the world it will be compared against spells an attribute.</param>
    public static ImmutableArray<Truth> Truths(
        Puzzle puzzle, Spelling spelling = Spelling.Fused)
    {
        var sound = new Dictionary<string, Truth>(StringComparer.Ordinal);

        foreach (var pinned in Conjunctions())
        {
            var scope = Scope(pinned, spelling);

            if (scope.IsEmpty) continue;

            var says = Says(Holds(puzzle, First(pinned)));

            if (Sound(puzzle, scope, says, spelling))
                sound[Key(pinned)] = new Truth { Scope = scope, Expects = says };
        }

        return
        [
            .. Conjunctions()
                .Where(pinned => sound.ContainsKey(Key(pinned)))
                .Where(pinned => !Shorter(pinned).Any(less => sound.ContainsKey(Key(less))))
                .Select(pinned => sound[Key(pinned)]),
        ];
    }

    /// <summary>Whether every code in a scope is satisfied by an instance.</summary>
    /// <param name="scope">The codes that must be present, and the entries naming variables.</param>
    /// <param name="instance">Six nought-based attribute values.</param>
    /// <param name="spelling">How the world it came from spells an attribute.</param>
    /// <remarks>
    /// <para>
    /// <b>A name is bound rather than skipped.</b> One entry carrying a name constrains only
    /// that the attribute it stands over has some value, which every instance satisfies; two
    /// entries carrying one name say those attributes hold THE SAME value, which is Monk-1's
    /// first disjunct and the reason this world was picked for fork 133.
    /// </para>
    /// <para>
    /// <b>And under <see cref="Spelling.Fused"/> a name reaches no attribute</b>, every code
    /// riding one modality — so <i>whichever attribute-and-value code</i> is satisfied by any
    /// instance whatever, however often it repeats. Passing over it is exact there and would
    /// be a lie under the split spelling.
    /// </para>
    /// </remarks>
    private static bool Covers(
        ImmutableArray<Codes.Code> scope, ImmutableArray<int> instance, Spelling spelling)
    {
        Dictionary<int, int>? bound = null;

        foreach (var code in scope)
        {
            if (code.Modality == Whatever)
            {
                if (spelling != Spelling.Split) continue;

                var (modality, name) = Stands(code);
                var over = modality - Attribute;

                if (over < 0 || over >= instance.Length) return false;

                bound ??= [];

                if (bound.TryGetValue(name, out var already))
                {
                    if (already != instance[over]) return false;
                }
                else
                {
                    bound[name] = instance[over];
                }

                continue;
            }

            var at = spelling == Spelling.Split
                ? Codes.Slotted.Position(Attribute, code)
                : (int)(code.Value / (ulong)Stride);

            var value = spelling == Spelling.Split
                ? Codes.Slotted.Value(code)
                : (int)(code.Value % (ulong)Stride);

            if (at < 0 || at >= instance.Length || instance[at] != value) return false;
        }

        return true;
    }

    /// <summary>Every conjunction the scope language can express, as pinned-or-free.</summary>
    /// <remarks>
    /// <b>2,880 of them</b>, which is why the answer key here is complete rather than
    /// sampled. An attribute is pinned to one of its values or left free, and it can
    /// never be pinned twice — two values of one attribute never co-occur, so such a
    /// scope fires on nothing and is not part of the language in any useful sense.
    /// </remarks>
    private static IEnumerable<int?[]> Conjunctions()
    {
        var pinned = new int?[Widths.Length];

        while (true)
        {
            yield return (int?[])pinned.Clone();

            var which = Widths.Length - 1;

            for (; which >= 0; which--)
            {
                pinned[which] = pinned[which] is null ? 0 : pinned[which] + 1;

                if (pinned[which] < Widths[which]) break;

                pinned[which] = null;
            }

            if (which < 0) yield break;
        }
    }

    private static ImmutableArray<Codes.Code> Scope(int?[] pinned, Spelling spelling) =>
    [
        .. pinned
            .Select((value, at) => (value, at))
            .Where(one => one.value is not null)
            .Select(one => Of(one.at, one.value!.Value, spelling)),
    ];

    /// <summary>Any instance the conjunction covers, for asking what it says.</summary>
    private static ImmutableArray<int> First(int?[] pinned) =>
        Everything.First(one => Covers(Scope(pinned, Spelling.Fused), one, Spelling.Fused));

    private static IEnumerable<int?[]> Shorter(int?[] pinned)
    {
        for (var which = 0; which < pinned.Length; which++)
        {
            if (pinned[which] is null) continue;

            var less = (int?[])pinned.Clone();
            less[which] = null;

            yield return less;
        }
    }

    private static string Key(int?[] pinned) =>
        string.Join(",", pinned.Select(one => one?.ToString() ?? "-"));

    private static Turn<IReadOnlyList<int>> Turn(Puzzle puzzle, ImmutableArray<int> instance) =>
        new() { Seen = instance, Outcome = Holds(puzzle, instance) ? 1 : 0 };

    private static ImmutableArray<ImmutableArray<int>> Enumerate()
    {
        var all = ImmutableArray.CreateBuilder<ImmutableArray<int>>();
        var at = new int[Widths.Length];

        while (true)
        {
            all.Add([.. at]);

            var which = Widths.Length - 1;

            for (; which >= 0; which--)
            {
                at[which]++;
                if (at[which] < Widths[which]) break;
                at[which] = 0;
            }

            if (which < 0) return all.ToImmutable();
        }
    }
}
