using System.Collections.Immutable;

namespace OpenPlexus.Worlds;

/// <summary>How big the scene is, how cluttered, and how much of it is never drawn.</summary>
public sealed record ArrangedSettings
{
    /// <summary>How many cells across the grid is.</summary>
    /// <remarks>
    /// <b>Three, because the whole space has to be enumerable.</b> Soundness by
    /// enumeration is the instrument <see cref="Cifar"/> had to do without, and it
    /// costs one coding of every scene the world admits — nine cells with one
    /// distractor is ten thousand of them, and four is a quarter of a million.
    /// </remarks>
    public int Side { get; init; } = 3;

    /// <summary>How many pixels across one cell is.</summary>
    /// <remarks>
    /// <b>The resolution dial, and it is the world's.</b> How finely a scene shows
    /// itself is a fact about what is being looked at; the reading is
    /// <c>(Side * Cell)</c> squared numbers, and <see cref="Codes.Winnowing"/> spends
    /// one winner per twenty cells of a sheet that is linear in that.
    /// </remarks>
    public int Cell { get; init; } = 3;

    /// <summary>How many shapes are present that the answer does not depend on.</summary>
    /// <remarks>
    /// <b>The multiplexer's property, in a world made of photons.</b> Several cues
    /// arrive together and only some carry the outcome — here the irrelevant ones are
    /// whole recurring parts rather than bits, and they move around, so what has to be
    /// ignored is different every scene.
    /// </remarks>
    public int Clutter { get; init; } = 1;

    /// <summary>One arrangement in this many is never drawn. Nought withholds nothing.</summary>
    /// <remarks>
    /// <para>
    /// <b>The sharpest instrument this world has, and it is aimed at the thing a
    /// score cannot see.</b> A learner that stores one rule per arrangement is
    /// arbitrarily accurate on what it has been shown and holds no notion of LEFT OF.
    /// Arrangements it was never shown are where the two come apart.
    /// </para>
    /// <para>
    /// <b>Held back in pairs, so the exam is balanced by construction.</b> Every
    /// arrangement has a partner with the two markers swapped and the opposite answer;
    /// withholding one without the other would hand the held-out set a majority class
    /// and let a constant answer beat chance on it.
    /// </para>
    /// </remarks>
    public int Hold { get; init; } = 4;
}

/// <summary>One shape, in one cell.</summary>
public readonly record struct Placed
{
    /// <summary>Which shape, indexing <see cref="Arranged.Shapes"/>.</summary>
    public required int Shape { get; init; }

    /// <summary>Which cell, row-major over the grid.</summary>
    public required int Cell { get; init; }
}

/// <summary>
/// One scene: what is where, and what that arrangement means.
/// </summary>
/// <remarks>
/// <b>The answer key is the scene and not the input</b>, which is CLEVR's shape. What
/// reaches a front end is <see cref="Arranged.Render"/>'s pixels; this is what the
/// world knows about them, and it exists so that soundness can be settled by
/// enumeration rather than sampled.
/// </remarks>
public readonly record struct Layout
{
    /// <summary>Every shape in the scene, in cell order.</summary>
    public required ImmutableArray<Placed> Places { get; init; }

    /// <summary>Nought when the first marker is left of the second, one otherwise.</summary>
    public required int Outcome { get; init; }

    /// <summary>Whether the world ever draws this, or holds its arrangement back.</summary>
    /// <remarks>
    /// <b>On the scene rather than on a second list, because everything that reads the
    /// space needs both halves.</b> Soundness wants every scene and does not care;
    /// a yardstick has to be fitted on one half and scored on the other, or it is not a
    /// held-out number at all. Two methods returning two lists would let those drift.
    /// </remarks>
    public required bool Shown { get; init; }

    /// <summary>Two scenes are the same when they place the same things.</summary>
    /// <remarks>
    /// <b>Written out because the compiler's answer here is wrong and silent.</b> A
    /// synthesised record equality compares <see cref="ImmutableArray{T}"/> by the
    /// identity of the array behind it, so two scenes built from the same placement
    /// compare UNEQUAL — and every determinism check written against it would have
    /// passed by never being able to fail. The same fault, and the same fix, as
    /// <see cref="Assignment"/>.
    /// </remarks>
    /// <param name="other">The scene to compare against.</param>
    public bool Equals(Layout other) =>
        Outcome == other.Outcome
        && Shown == other.Shown
        && Places.AsSpan().SequenceEqual(other.Places.AsSpan());

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(Outcome);
        hash.Add(Shown);
        foreach (var placed in Places) hash.Add(placed);

        return hash.ToHashCode();
    }
}

/// <summary>
/// The same parts, in two arrangements, with opposite answers.
/// </summary>
/// <remarks>
/// <para>
/// <b>The world step four said it could not be.</b> <see cref="Cifar"/> ships photons
/// and measures a front end, and a ten-way label per picture has no parts and no
/// arrangement — so no score on it can tell a front end that manufactures REUSABLE
/// symbols from one that emits a holistic blob per image. Both separate ten classes;
/// only the first leads anywhere.
/// </para>
/// <para>
/// <b>So the one hard constraint is that a bag of parts must score chance, and here it
/// is a theorem rather than a hope.</b> Swapping which cell holds the first marker and
/// which holds the second leaves the multiset of shapes untouched, flips the answer,
/// and lands on a scene the world draws exactly as often. That map is an involution on
/// the whole space, so what is present carries no information about the outcome AT ALL
/// — see <see cref="Swapped"/>, which the tests enumerate rather than sample.
/// </para>
/// <para>
/// <b>And it is generated, which restores soundness by enumeration on a world made of
/// photons.</b> <see cref="Layouts"/> is the entire space, so a commitment can be asked
/// whether it is TRUE — coded through whatever front end is in use, checked against
/// every scene it fires on — rather than whether it agrees with a basis somebody chose.
/// That is the instrument step four had to do without, and the reason the grid is small.
/// </para>
/// <para>
/// <b>What it still does not test is the other half of the goal.</b> A scene is
/// single-shot and independent: no action, no intervention, no sequence. Settlement is
/// trivial, <c>Abstain</c> cannot fire, and entailment depth is always one. This is the
/// front end and the scope language, measured together and honestly, and nothing more.
/// </para>
/// </remarks>
public sealed class Arranged : IWorld<IReadOnlyList<double>>, IWithholds<IReadOnlyList<double>>
{
    /// <summary>How many shapes there are, markers first.</summary>
    /// <remarks>
    /// <b>Five, and the first two are the ones the answer is about.</b> They are drawn
    /// by the same arithmetic as the clutter and are told apart only by which cells the
    /// world reads — nothing about a marker's pixels says it is special, which is what
    /// stops the problem being solvable by counting ink.
    /// </remarks>
    public const int Shapes = 5;

    /// <summary>How many shapes the answer depends on.</summary>
    /// <remarks>
    /// <b>Private, because nothing outside has any business knowing which shapes the
    /// answer is about.</b> A front end told that would have been handed the problem,
    /// and a test told it would be marking its own paper.
    /// </remarks>
    private const int Markers = 2;

    private readonly ArrangedSettings _settings;
    private readonly Random _rng;

    /// <summary>Arrangements the world draws from, as (first marker, second marker).</summary>
    private readonly ImmutableArray<(int First, int Second)> _drawn;

    /// <summary>Arrangements it never draws.</summary>
    private readonly ImmutableArray<(int First, int Second)> _kept;

    /// <inheritdoc/>
    public int Outcomes => 2;

    /// <inheritdoc/>
    public IReadOnlyList<Turn<IReadOnlyList<double>>> Withheld { get; }

    /// <summary>How many numbers one reading has.</summary>
    public int Width => Pixels * Pixels;

    /// <summary>How many pixels across the whole scene is.</summary>
    public int Pixels => _settings.Side * _settings.Cell;

    /// <summary>How many cells the grid has.</summary>
    public int Cells => _settings.Side * _settings.Side;

    /// <summary>How many arrangements the world draws from.</summary>
    public int Drawn => _drawn.Length;

    /// <summary>What a blind guess scores.</summary>
    /// <remarks>
    /// <b>A half, and it is exact rather than approached.</b> Every arrangement is
    /// drawn as often as its swap, so the two outcomes are equally frequent in the
    /// drawn set and in the withheld one — which is what makes a score above it mean
    /// something without a majority-class arm beside it.
    /// </remarks>
    public static double Chance => 0.5;

    /// <param name="settings">The shape of the world.</param>
    /// <param name="seed">The world's own generator.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The grid is too small to hold the markers and the clutter, or too small to have
    /// two columns to compare.
    /// </exception>
    public Arranged(ArrangedSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Side, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Side, 8);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Cell, 3);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Cell, 16);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Clutter);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Hold);

        var cells = settings.Side * settings.Side;

        if (settings.Clutter > cells - Markers)
            throw new ArgumentOutOfRangeException(
                nameof(settings),
                $"a {settings.Side}-by-{settings.Side} grid has {cells - Markers} cells "
                + $"left once the markers are placed, and {settings.Clutter} distractors "
                + "were asked for. Widen the grid or drop the clutter.");

        _settings = settings;
        _rng = new Random(seed);

        // The split is a position in a canonical order and not a draw, so every seed
        // sits the same exam. A held-out set chosen by the world's own generator would
        // move with the seed, and two seeds would then be scored against two different
        // questions.
        var pairs = Pairs().ToList();

        var drawn = new List<(int First, int Second)>();
        var kept = new List<(int First, int Second)>();

        for (var which = 0; which < pairs.Count; which++)
        {
            // Both orderings go the same way, which is what balances the exam. One
            // half of a pair held back would give the withheld set a majority class.
            var (low, high) = pairs[which];

            var held = settings.Hold > 0 && which % settings.Hold == settings.Hold - 1;

            (held ? kept : drawn).Add((low, high));
            (held ? kept : drawn).Add((high, low));
        }

        if (drawn.Count == 0)
            throw new ArgumentOutOfRangeException(
                nameof(settings),
                $"Hold = {settings.Hold} withholds every arrangement this world has, so "
                + "there is nothing left to draw.");

        _drawn = [.. drawn];
        _kept = [.. kept];

        Withheld =
        [
            .. Layouts()
                .Where(scene => !scene.Shown)
                .Select(scene => new Turn<IReadOnlyList<double>>
                {
                    Seen = Render(scene),
                    Outcome = scene.Outcome,
                }),
        ];
    }

    /// <summary>One scene, drawn uniformly from the arrangements the world shows.</summary>
    /// <remarks>
    /// <b>Uniform over the whole drawn space, which is what makes the involution an
    /// argument.</b> An arrangement is picked uniformly and its clutter is picked
    /// uniformly, and every arrangement admits the same number of clutterings — so
    /// every drawn scene is equally likely, and a scene and its swap therefore arrive
    /// equally often.
    /// </remarks>
    public Turn<IReadOnlyList<double>> Next()
    {
        var arrangement = _drawn[_rng.Next(_drawn.Length)];

        // SLOTS RATHER THAN CELLS, because `Compose` is what maps a slot onto whichever
        // cell the markers left free. Drawing real cells here and handing them to it
        // would map them twice, and the draw and `Layouts` would then disagree about
        // what a clutter configuration is -- silently, and in a way that only shows up
        // as a soundness score that cannot see its own world.
        var slots = Enumerable.Range(0, Cells - Markers).ToList();

        // A uniform subset by partial shuffle, then sorted -- so what is drawn is a
        // SET of slots with a shape each, which is exactly what `Clutterings`
        // enumerates. Drawing an ordered tuple instead would over-count.
        for (var taken = 0; taken < _settings.Clutter; taken++)
        {
            var pick = taken + _rng.Next(slots.Count - taken);
            (slots[taken], slots[pick]) = (slots[pick], slots[taken]);
        }

        var clutter = slots
            .Take(_settings.Clutter)
            .Order()
            .Select(slot => new Placed
            {
                Shape = Markers + _rng.Next(Shapes - Markers),
                Cell = slot,
            })
            .ToImmutableArray();

        var scene = Compose(arrangement, clutter, shown: true);

        return new Turn<IReadOnlyList<double>>
        {
            Seen = Render(scene),
            Outcome = scene.Outcome,
        };
    }

    /// <summary>
    /// Every scene this world admits, drawn and withheld alike.
    /// </summary>
    /// <remarks>
    /// <b>The whole space</b>, because a rule is true of the world or it is not.
    /// Soundness asked only over the drawn scenes would call a rule true when it merely
    /// has not been contradicted yet, which is the same fault as scoring a learner on
    /// what it was taught. It is exact rather than sampled, and it is what
    /// <see cref="Cifar"/> can never have.
    /// </remarks>
    public IEnumerable<Layout> Layouts()
    {
        // MATERIALISED ONCE, because the same clutter list serves every arrangement --
        // it is enumerated over SLOTS and not over cells, which is the whole reason
        // every arrangement admits exactly as many scenes as every other.
        var clutterings = Clutterings().ToList();

        foreach (var (arrangements, shown) in new[] { (_drawn, true), (_kept, false) })
        foreach (var arrangement in arrangements)
        foreach (var clutter in clutterings)
            yield return Compose(arrangement, clutter, shown);
    }

    /// <summary>
    /// The same scene with the two markers exchanged.
    /// </summary>
    /// <param name="scene">The scene to swap.</param>
    /// <remarks>
    /// <b>The involution the whole world rests on.</b> It leaves the multiset of shapes
    /// exactly as it was, flips the outcome, and lands inside the space at the same
    /// probability — so knowing every part that is present tells you nothing whatever
    /// about the answer. That is the constraint the plan writes down as unscoreable by a
    /// bag of parts, said as a map rather than as a wish.
    /// </remarks>
    /// <remarks>
    /// <b>And it stays on its own side of the held-out line.</b> Arrangements are
    /// withheld in swapped PAIRS, so a scene the world draws swaps to one it draws and a
    /// withheld one swaps to a withheld one. Which is why the exam is balanced without
    /// anything balancing it, and why <see cref="Layout.Shown"/> is carried across rather
    /// than recomputed.
    /// </remarks>
    public static Layout Swapped(Layout scene)
    {
        var first = scene.Places.Single(one => one.Shape == 0);
        var second = scene.Places.Single(one => one.Shape == 1);

        var places = scene.Places
            .Select(one => one.Shape switch
            {
                0 => one with { Cell = second.Cell },
                1 => one with { Cell = first.Cell },
                _ => one,
            })
            .OrderBy(one => one.Cell)
            .ToImmutableArray();

        return new Layout
        {
            Places = places,
            Outcome = 1 - scene.Outcome,
            Shown = scene.Shown,
        };
    }

    /// <summary>
    /// One scene as photons.
    /// </summary>
    /// <param name="scene">What is where.</param>
    /// <remarks>
    /// <para>
    /// <b>The same glyph in every cell it ever appears in, which is the recurrence the
    /// world exists to offer.</b> A front end that manufactures a reusable symbol per
    /// part will emit something in common between two scenes holding the same shape
    /// somewhere; one that codes the picture as a whole will not. Nothing here makes
    /// either happen — it makes the difference VISIBLE, which is the job of a world.
    /// </para>
    /// <para>
    /// <b>Ink is a scene constant, deliberately.</b> Every shape covers whatever its
    /// own pattern covers wherever it sits, so a scene and its swap carry identical
    /// total intensity — and a front end that centres its reading, as
    /// <see cref="Codes.Winnow"/> does, cannot pick the answer out of the brightness.
    /// </para>
    /// </remarks>
    public ImmutableArray<double> Render(Layout scene)
    {
        var reading = new double[Width];

        foreach (var placed in scene.Places)
        {
            var top = placed.Cell / _settings.Side * _settings.Cell;
            var left = placed.Cell % _settings.Side * _settings.Cell;

            for (var down = 0; down < _settings.Cell; down++)
            for (var across = 0; across < _settings.Cell; across++)
                if (Inked(placed.Shape, down, across, _settings.Cell))
                    reading[((top + down) * Pixels) + left + across] = 1.0;
        }

        return [.. reading];
    }

    /// <summary>
    /// Whether one pixel of one shape's patch is lit.
    /// </summary>
    /// <param name="shape">Which shape.</param>
    /// <param name="down">Which row of the patch.</param>
    /// <param name="across">Which column of the patch.</param>
    /// <param name="cell">How many pixels across the patch is.</param>
    /// <remarks>
    /// <para>
    /// <b>Said as a predicate over the patch rather than as a bitmap, so the shapes
    /// survive the resolution dial.</b> A hand-drawn three-by-three would have to be
    /// redrawn at every <see cref="ArrangedSettings.Cell"/>, and a shape that changed
    /// when the world got sharper would make the resolution arm a comparison between
    /// two different problems.
    /// </para>
    /// <para>
    /// <b>And no shape is a solid block</b>, which is a constraint and not a taste. A
    /// front end worth having asks what SHAPE a reading has and not how loud it was —
    /// <see cref="Codes.Winnow"/> centres before it projects, for the reason a smell
    /// twice as strong is the same smell. A uniformly filled patch and an empty one are
    /// then the same reading, so a solid shape is a part that no contrast-normalising
    /// sense can tell from background. The wedge is the block with that fixed.
    /// </para>
    /// <para>
    /// <b>It cost nothing to find and would have cost a great deal to discover from a
    /// score</b>, because the whole-image arm reads a picture that is never uniform and
    /// separates the world perfectly — the collision only bites a front end that looks
    /// at one part at a time, which is exactly the arm this world exists to try.
    /// </para>
    /// </remarks>
    private static bool Inked(int shape, int down, int across, int cell)
    {
        // The middle third, by integer arithmetic. At three pixels it is the centre
        // one; at six it is the middle two. Nothing here rounds differently on
        // another machine, which a fraction of a pixel would.
        var middling = down * 3 / cell == 1;
        var centring = across * 3 / cell == 1;

        var edging = down == 0 || across == 0 || down == cell - 1 || across == cell - 1;

        return shape switch
        {
            0 => across <= down,              // wedge
            1 => edging,                      // ring
            2 => middling,                    // bar
            3 => centring,                    // post
            4 => middling || centring,        // cross
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
    }

    /// <summary>Unordered cell pairs the markers may occupy, in canonical order.</summary>
    /// <remarks>
    /// <b>Different columns, because the question is about columns.</b> Two markers in
    /// one column have no left and no right, and admitting them would need a third
    /// outcome that is about the world's geometry rather than about arrangement.
    /// </remarks>
    private IEnumerable<(int Low, int High)> Pairs()
    {
        for (var low = 0; low < Cells; low++)
        for (var high = low + 1; high < Cells; high++)
            if (low % _settings.Side != high % _settings.Side)
                yield return (low, high);
    }

    /// <summary>Every way the clutter can be placed, ignoring where the markers went.</summary>
    /// <remarks>
    /// <b>Over slots rather than over cells, so the count is the same for every
    /// arrangement.</b> <see cref="Compose"/> maps a slot onto whichever cell is free,
    /// which is what makes the draw uniform over scenes and the two markers
    /// interchangeable — a clutter enumerated against real cells would admit more
    /// configurations for some arrangements than others.
    /// </remarks>
    private IEnumerable<ImmutableArray<Placed>> Clutterings()
    {
        var slots = Cells - Markers;

        return Subsets(slots, _settings.Clutter)
            .SelectMany(chosen => Shapings(chosen.Length)
                .Select(shapes => Enumerable.Range(0, chosen.Length)
                    .Select(at => new Placed { Shape = Markers + shapes[at], Cell = chosen[at] })
                    .ToImmutableArray()));
    }

    /// <summary>Every sorted choice of <paramref name="take"/> from <paramref name="of"/>.</summary>
    /// <param name="of">How many there are to choose from.</param>
    /// <param name="take">How many to choose.</param>
    private static IEnumerable<ImmutableArray<int>> Subsets(int of, int take)
    {
        if (take == 0)
        {
            yield return [];
            yield break;
        }

        for (var first = 0; first <= of - take; first++)
            foreach (var rest in Subsets(of - first - 1, take - 1))
                yield return [first, .. rest.Select(one => one + first + 1)];
    }

    /// <summary>Every way to give <paramref name="many"/> slots a clutter shape.</summary>
    /// <param name="many">How many slots there are.</param>
    private static IEnumerable<ImmutableArray<int>> Shapings(int many)
    {
        var kinds = Shapes - Markers;

        var total = 1;
        for (var slot = 0; slot < many; slot++) total *= kinds;

        for (var draw = 0; draw < total; draw++)
        {
            var shapes = new int[many];
            var left = draw;

            for (var slot = 0; slot < many; slot++)
            {
                shapes[slot] = left % kinds;
                left /= kinds;
            }

            yield return [.. shapes];
        }
    }

    /// <summary>An arrangement and a clutter, joined into a scene.</summary>
    /// <param name="arrangement">Which cells hold the first and second markers.</param>
    /// <param name="clutter">
    /// The distractors, whose <see cref="Placed.Cell"/> is a SLOT among the cells the
    /// markers left free rather than a cell.
    /// </param>
    /// <param name="shown">Whether the world draws this arrangement.</param>
    private Layout Compose(
        (int First, int Second) arrangement,
        ImmutableArray<Placed> clutter,
        bool shown)
    {
        var free = Enumerable.Range(0, Cells)
            .Where(cell => cell != arrangement.First && cell != arrangement.Second)
            .ToList();

        var places = ImmutableArray.CreateBuilder<Placed>(Markers + clutter.Length);

        places.Add(new Placed { Shape = 0, Cell = arrangement.First });
        places.Add(new Placed { Shape = 1, Cell = arrangement.Second });

        foreach (var placed in clutter)
            places.Add(placed with { Cell = free[placed.Cell] });

        places.Sort((left, right) => left.Cell.CompareTo(right.Cell));

        return new Layout
        {
            Places = places.ToImmutable(),
            Outcome = arrangement.First % _settings.Side < arrangement.Second % _settings.Side
                ? 0
                : 1,
            Shown = shown,
        };
    }
}
