namespace OpenPlexus.Worlds;

/// <summary>
/// A word drawn as pixels — <b>fork 107, and the one crossing that keeps ground truth
/// enumerable.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The same word through two senses is what a camera asks</b>, and this asks it where
/// the answer is still checkable. A word read as a symbol and a word seen as a shape are
/// two attributes of one thing, which is THE ARCHITECTURE's second entry; a camera poses
/// that and takes soundness, overshoot and hard-round coverage away with it, because none
/// of those survives a world that cannot be enumerated. Here the world knows exactly which
/// word it drew.
/// </para>
/// <para>
/// <b>And it gates every sensor rather than being one.</b> If binding works on a crossing
/// this clean, a microphone and a camera are plumbing; if it does not, it will not work on
/// video either, and that is a day rather than a month.
/// </para>
/// <para>
/// <b>The glyphs are a table and never a fitted thing</b>, which the codes rule requires:
/// two machines handed the same word must draw the same pixels, so nothing here is learnt,
/// sampled or refitted. Five by seven is the smallest cell these letters stay legible in.
/// </para>
/// <para>
/// <b>And the drawing MOVES</b>, which is the whole of why it is not a lookup. A word drawn
/// at one place every time renders one fixed raster, so a probe fitted on it scores itself
/// and says nothing at all. The offset is what makes <i>which word is this</i> a question
/// about the shape rather than about the pixels' addresses — and it is the same invariance
/// any real sensor is up against.
/// </para>
/// </remarks>
public static class Lettering
{
    /// <summary>How many pixels across one glyph is.</summary>
    public const int Wide = 5;

    /// <summary>How many pixels down one glyph is. <b>Read through <see cref="Room"/></b>.</summary>
    private const int Tall = 7;

    /// <summary>How many pixels sit between two glyphs.</summary>
    public const int Gap = 1;

    /// <summary>How many pixels across the square canvas is.</summary>
    /// <remarks>
    /// <b>Square because <see cref="Codes.Tiling"/> reads a square</b>, and divisible by
    /// two, three, four, six, eight and twelve so a patch size is a dial rather than a
    /// constraint. A three-letter word is seventeen across, which leaves the offset room to
    /// move in both directions.
    /// </remarks>
    public const int Side = 24;

    /// <summary>Capital A to Z, five across and seven down, row by row.</summary>
    /// <remarks>
    /// <b>Capitals rather than lower case</b>, for legibility at this size alone: an <c>a</c>
    /// and an <c>o</c> differ by two pixels in a five-wide cell, which would make the reading
    /// about the font rather than about the front end.
    /// </remarks>
    private static readonly string[][] Glyphs =
    [
        [".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
        ["####.", "#...#", "#...#", "####.", "#...#", "#...#", "####."],
        [".###.", "#...#", "#....", "#....", "#....", "#...#", ".###."],
        ["####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####."],
        ["#####", "#....", "#....", "###..", "#....", "#....", "#####"],
        ["#####", "#....", "#....", "###..", "#....", "#....", "#...."],
        [".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###."],
        ["#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
        [".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###."],
        ["..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##.."],
        ["#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#"],
        ["#....", "#....", "#....", "#....", "#....", "#....", "#####"],
        ["#...#", "##.##", "#.#.#", "#...#", "#...#", "#...#", "#...#"],
        ["#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#"],
        [".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
        ["####.", "#...#", "#...#", "####.", "#....", "#....", "#...."],
        [".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#"],
        ["####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#"],
        [".####", "#....", "#....", ".###.", "....#", "....#", "####."],
        ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."],
        ["#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
        ["#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.."],
        ["#...#", "#...#", "#...#", "#...#", "#.#.#", "##.##", "#...#"],
        ["#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#"],
        ["#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.."],
        ["#####", "....#", "...#.", "..#..", ".#...", "#....", "#####"],
    ];

    /// <summary>How many pixels across a word is when it is drawn. <b>Read through <see cref="Room"/></b>.</summary>
    /// <param name="letters">How many letters it has.</param>
    private static int Across(int letters) =>
        letters <= 0 ? 0 : (letters * Wide) + ((letters - 1) * Gap);

    /// <summary>How far a word of this length may be moved and still fit.</summary>
    /// <param name="letters">How many letters it has.</param>
    /// <remarks>
    /// <b>Returned rather than assumed</b>, so a caller drawing a longer word cannot silently
    /// push it off the canvas — a glyph half outside the square reads downstream exactly like
    /// a front end that could not see it.
    /// </remarks>
    public static (int Across, int Down) Room(int letters) =>
        (Side - Across(letters), Side - Tall);

    /// <summary>Draws a word onto a square canvas, as pixels at nought or one.</summary>
    /// <param name="word">The word, which must be letters this holds a glyph for.</param>
    /// <param name="across">How far in from the left the word starts.</param>
    /// <param name="down">How far down from the top it starts.</param>
    /// <returns>
    /// <see cref="Side"/> times <see cref="Side"/> pixels, row by row.
    /// </returns>
    /// <exception cref="ArgumentException">A letter has no glyph, or it does not fit.</exception>
    public static IReadOnlyList<double> Draw(string word, int across, int down)
    {
        ArgumentException.ThrowIfNullOrEmpty(word);

        var (room, drop) = Room(word.Length);

        if (room < 0)
            throw new ArgumentException(
                $"'{word}' is {Across(word.Length)} pixels across and the canvas is {Side}",
                nameof(word));

        ArgumentOutOfRangeException.ThrowIfNegative(across);
        ArgumentOutOfRangeException.ThrowIfNegative(down);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(across, room);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(down, drop);

        var canvas = new double[Side * Side];

        for (var at = 0; at < word.Length; at++)
        {
            var letter = char.ToUpperInvariant(word[at]) - 'A';

            if (letter is < 0 or > 25)
                throw new ArgumentException(
                    $"'{word[at]}' is not a letter this can draw", nameof(word));

            var glyph = Glyphs[letter];
            var left = across + (at * (Wide + Gap));

            for (var row = 0; row < Tall; row++)
                for (var column = 0; column < Wide; column++)
                    if (glyph[row][column] == '#')
                        canvas[((down + row) * Side) + left + column] = 1.0;
        }

        return canvas;
    }
}
