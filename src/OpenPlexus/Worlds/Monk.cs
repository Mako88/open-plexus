using System.Collections.Immutable;

namespace OpenPlexus.Worlds;

/// <summary>Which of the three MONK's problems a world is posing.</summary>
/// <remarks>
/// <b>ALL THREE, BECAUSE ONE OF THEM ALONE ANSWERS NOTHING.</b> The second is the
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
    /// <b>EXACTLY TWO of the six attributes hold their first value</b> — the counting
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

/// <summary>Which MONK's problem, and how much of the bag is never drawn.</summary>
public sealed record MonkSettings
{
    /// <summary>Which of the three.</summary>
    public Puzzle Puzzle { get; init; } = Puzzle.Two;

    /// <summary>
    /// How many of the 432 to load, score and never draw.
    /// </summary>
    /// <remarks>
    /// <b>TAKEN FROM THE END OF A FIXED ENUMERATION, SO THE SPLIT IS A POSITION AND NOT
    /// A SAMPLE</b> — the same reason <see cref="Cifar"/> does it that way. A withheld
    /// set chosen by the world's own generator would move with the seed, and two seeds
    /// would then be scored against two different questions.
    /// </remarks>
    public int Withheld { get; init; } = 132;
}

/// <summary>
/// The MONK's problems — 432 robots, six attributes, and one concept a conjunction
/// cannot say.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE CHEAPEST LANGUAGE-CEILING PROBE THERE IS, AND THE PLAN HAS CALLED IT A DAY'S
/// WORK SINCE THE LADDER WAS WRITTEN.</b> Step eight is <i>the rung the failures
/// demand, and never the rung that sounds next</i> — and no failure has asked yet,
/// because on the one world that can say, twelve one-code rules cover everything held
/// out. This is a world built so a failure can ask.
/// </para>
/// <para>
/// <b>THE BAG IS 432 AND THAT IS THE WHOLE ATTRACTION.</b> Every instrument this project
/// wants is exact here rather than estimated: the concept can be enumerated, so soundness
/// is decidable; the bag is finite, so <see cref="IWithholds{TSeen}"/> gives a real
/// held-out set; and the majority class is countable, so the bar a silent arm drifts
/// toward is a number rather than a guess.
/// </para>
/// <para>
/// <b>AND THE BAR IS NOT A HALF, WHICH IS THE TRAP THIS WORLD SETS FOR ANYBODY READING
/// ITS SCORE.</b> <see cref="Puzzle.Two"/> is 142 positive of 432, so ALWAYS SAYING NO
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

    /// <summary>The modality one attribute-and-value rides on.</summary>
    public const byte Attribute = 150;

    /// <summary>The modality the answer rides on.</summary>
    /// <remarks>
    /// <b>101, WHICH IS THE BRAIN'S OWN, AND THIS FILE MAY NOT SAY SO OUT LOUD.</b> The
    /// outcome alphabet is shared across every world — a brain that learnt a different
    /// one per world would not be one brain — and <c>SeparationTests</c> fails the build
    /// if a world names a brain type, so the number is written here and that same test
    /// asserts the two agree. <see cref="Multiplexer.Said"/> carries the identical
    /// duplication for the identical reason.
    /// </remarks>
    public const byte Answered = 101;

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

        _drawn = [.. all.Take(all.Length - settings.Withheld)
            .Select(one => Turn(settings.Puzzle, one))];

        Withheld = [.. all.Skip(all.Length - settings.Withheld)
            .Select(one => Turn(settings.Puzzle, one))];

        Chance = Bar(settings.Puzzle);
    }

    /// <inheritdoc/>
    public int Outcomes => 2;

    /// <inheritdoc/>
    public IReadOnlyList<Turn<IReadOnlyList<int>>> Withheld { get; }

    /// <summary>What always naming the commoner answer scores on the whole bag.</summary>
    /// <remarks>
    /// <b>THE MAJORITY CLASS AND NOT A HALF.</b> See the note on the type: 0.6713 on
    /// <see cref="Puzzle.Two"/>, and a score read against 0.5 there would call a learner
    /// that has done nothing a success.
    /// </remarks>
    public double Chance { get; }

    /// <summary>One drawn instance, with replacement.</summary>
    public Turn<IReadOnlyList<int>> Next() => _drawn[_rng.Next(_drawn.Count)];

    /// <summary>Every one of the 432 instances, in a fixed order.</summary>
    /// <remarks>
    /// <b>THE ANSWER KEY, AND IT IS WHY THIS WORLD IS WORTH BUILDING.</b> A soundness
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
    public static Codes.Code Of(int attribute, int value) =>
        Codes.Bits.Of(Attribute, attribute, value, Stride);

    /// <summary>The code for what the concept says.</summary>
    /// <param name="holds">Whether the instance is in the concept.</param>
    public static Codes.Code Says(bool holds) => new(Answered, holds ? 1UL : 0UL);

    /// <summary>
    /// Whether this world can decide a scope exactly — <b>always, and that is the point
    /// of choosing it.</b>
    /// </summary>
    /// <param name="scope">The codes that must be present.</param>
    /// <remarks>
    /// <b><see cref="Multiplexer"/> HAS TO REFUSE SCOPES WITH TOO MANY FREE BITS AND
    /// THIS ONE NEVER DOES.</b> There are 432 instances however few attributes a scope
    /// pins, so enumeration is a walk of the whole bag rather than an exponential in
    /// what was left open. Every rule a run holds is checkable here, so
    /// <c>Learned.Unchecked</c> is nought by construction and a soundness count on this
    /// world is over ALL of the population rather than over the part that happened to be
    /// small enough.
    /// </remarks>
    public static bool Checkable(ImmutableArray<Codes.Code> scope) =>
        !scope.IsDefaultOrEmpty && scope.All(code => code.Modality == Attribute);

    /// <summary>Whether a scope really does entail an expectation, over the whole bag.</summary>
    /// <param name="puzzle">Which of the three.</param>
    /// <param name="scope">The codes that must be present.</param>
    /// <param name="expects">What is claimed to follow.</param>
    /// <remarks>
    /// <b>A SCOPE PINNING ONE ATTRIBUTE TO TWO VALUES IS SATISFIED BY NOTHING</b>, so it
    /// entails everything vacuously — and calling that sound would let a learner score by
    /// minting contradictions. It covers no instance, so the empty-coverage test below
    /// refuses it without needing to know why it was empty.
    /// </remarks>
    public static bool Sound(Puzzle puzzle, ImmutableArray<Codes.Code> scope, Codes.Code expects)
    {
        if (!Checkable(scope)) return false;

        var covered = 0;

        foreach (var instance in Everything)
        {
            if (!Covers(scope, instance)) continue;

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
    /// <b>MINIMAL RATHER THAN ALL, BECAUSE SUBSUMPTION IS AIMED AT EXACTLY THIS SET.</b>
    /// A sound rule with a sound sub-scope says nothing the shorter one does not and
    /// covers less, which is the one case the design lets a general commitment replace a
    /// narrow one. So this is what a population that had finished compressing would
    /// hold, and counting how much of it is resident is a fair question. All 1,656 sound
    /// conjunctions on <see cref="Puzzle.One"/> is not.
    /// </para>
    /// <para>
    /// <b>AND THE SIZE OF THIS SET IS THE WHOLE FINDING.</b> 22 rules on
    /// <see cref="Puzzle.One"/> and 12 on <see cref="Puzzle.Three"/>, against 254 on
    /// <see cref="Puzzle.Two"/> — of which 142 are complete six-attribute instances,
    /// because the concept has no sound conjunction saying YES at any shorter depth.
    /// There is no compression available on that side at all.
    /// </para>
    /// </remarks>
    public static ImmutableArray<Truth> Truths(Puzzle puzzle)
    {
        var sound = new Dictionary<string, Truth>(StringComparer.Ordinal);

        foreach (var pinned in Conjunctions())
        {
            var scope = Scope(pinned);

            if (scope.IsEmpty) continue;

            var says = Says(Holds(puzzle, First(pinned)));

            if (Sound(puzzle, scope, says))
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
    private static bool Covers(ImmutableArray<Codes.Code> scope, ImmutableArray<int> instance)
    {
        foreach (var code in scope)
        {
            var at = (int)(code.Value / (ulong)Stride);
            var value = (int)(code.Value % (ulong)Stride);

            if (at >= instance.Length || instance[at] != value) return false;
        }

        return true;
    }

    /// <summary>Every conjunction the scope language can express, as pinned-or-free.</summary>
    /// <remarks>
    /// <b>2,880 OF THEM, WHICH IS WHY THE ANSWER KEY HERE IS COMPLETE RATHER THAN
    /// SAMPLED.</b> An attribute is pinned to one of its values or left free, and it can
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

    private static ImmutableArray<Codes.Code> Scope(int?[] pinned) =>
    [
        .. pinned
            .Select((value, at) => (value, at))
            .Where(one => one.value is not null)
            .Select(one => Of(one.at, one.value!.Value)),
    ];

    /// <summary>Any instance the conjunction covers, for asking what it says.</summary>
    private static ImmutableArray<int> First(int?[] pinned) =>
        Everything.First(one => Covers(Scope(pinned), one));

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
