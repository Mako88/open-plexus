using System.Text.RegularExpressions;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The duplication budget, in the same spirit as the doc's word budget.
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S ASK, 2026-08-03: MAKE THE HAND PASS UNNECESSARY.</b> Three worlds
/// each grew their own copy of the settle loop, the complaint list, the vote
/// tally and the concept-code scheme, and every one of those was found by
/// somebody reading all three files side by side. That is not a thing anyone
/// does twice.
/// </para>
/// <para>
/// <b>Duplication here is not a style complaint, it is how a measurement goes
/// wrong.</b> The copies drift: one world's tally counts silence and another's
/// does not, one world's stride is 1000 and another's is not, and nothing fails
/// because the copies share no code path. The numbers then stop being comparable
/// and nothing says so.
/// </para>
/// <para>
/// <b>To add a clone, extract it instead.</b> If a run genuinely has to repeat
/// itself, the honest move is to say why in the code rather than to raise
/// <see cref="Window"/> — the same rule the doc budget runs on.
/// </para>
/// </remarks>
public sealed class DuplicationTests
{
    /// <summary>
    /// How many consecutive statements have to match before it is a clone.
    /// </summary>
    /// <remarks>
    /// <b>Counted in real statements, never in lines.</b> Braces, blank lines and
    /// comments are stripped first, so reformatting cannot hide a clone and
    /// cannot invent one. Six is a little under the smallest thing that was worth
    /// extracting by hand.
    /// </remarks>
    private const int Window = 6;

    /// <summary>
    /// Lines that carry no logic and repeat everywhere by nature.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Without this the check measures C# rather than this codebase.</b> A run
    /// of closing braces is identical in every file ever written, and a duplicate
    /// detector that reports those reports nothing anyone will read.
    /// </para>
    /// <para>
    /// <b>Argument guards are in here on purpose, and it is the one judgement
    /// call.</b> <c>ArgumentNullException.ThrowIfNull</c> is what the language
    /// makes you write, not something anyone chose — every well-guarded entry
    /// point in the project opens with the same two or three lines, and counting
    /// those would mean the only way to pass is to guard less.
    /// </para>
    /// </remarks>
    private static readonly Regex Noise = new(
        @"^(\s*[\{\}\(\)\[\];,]+\s*|using .*|namespace .*|\[[A-Za-z]+(\(.*\))?\]"
        + @"|else|try|do|Argument\w*Exception\.Throw\w*\(.*\);)$",
        RegexOptions.Compiled);

    private static readonly Regex Comment = new(@"^\s*(//|///|\*|/\*)", RegexOptions.Compiled);

    private static readonly Regex Spacing = new(@"\s+", RegexOptions.Compiled);

    [Fact]
    public void No_run_of_statements_is_written_twice()
    {
        var clones = Clones([.. Tree.Sources("src"), .. Tree.Sources("tests")]);

        Assert.True(clones.Count == 0,
            $"{clones.Count} block(s) of {Window}+ statements appear more than once. "
            + "Extract rather than raising the window:\n"
            + string.Join("\n\n", clones.Take(5)));
    }

    [Fact]
    public void Only_the_shared_base_decides_what_a_result_complains_about()
    {
        // THE GUARD ON THE THING THAT WAS ACTUALLY DUPLICATED. Every world's
        // result carried its own `Complaints`, which is how a check could be
        // added to one world and silently missed on the other two. A fourth
        // world inherits the list or it does not have one.
        var owners = typeof(Measurement).Assembly
            .GetExportedTypes()
            .Where(type => type.GetProperty("Complaints")?.DeclaringType == type)
            .ToList();

        Assert.Equal([typeof(Measurement)], owners);

        // And every result really does inherit it, rather than the base sitting
        // there unused while the worlds go their own way.
        var results = typeof(Measurement).Assembly
            .GetExportedTypes()
            .Where(type => type.Name.EndsWith("Result", StringComparison.Ordinal)
                || type.Name.EndsWith("Report", StringComparison.Ordinal))
            .Where(type => type != typeof(Thinking.Report) && type != typeof(RunResult))
            .ToList();

        Assert.NotEmpty(results);
        Assert.All(results, type => Assert.True(
            typeof(Measurement).IsAssignableFrom(type),
            $"{type.Name} is a world's result and does not inherit its range checks"));
    }

    /// <summary>
    /// Every run of <see cref="Window"/> statements that appears in more than one
    /// place, reported once each rather than once per overlapping window.
    /// </summary>
    private static IReadOnlyList<string> Clones(IReadOnlyList<string> files)
    {
        var reduced = files.ToDictionary(
            path => Path.GetFileName(path) ?? path,
            path => Statements(File.ReadAllLines(path)),
            StringComparer.Ordinal);

        var seen = new Dictionary<string, List<(string File, int At)>>(StringComparer.Ordinal);

        foreach (var (name, statements) in reduced)
        {
            for (var i = 0; i + Window <= statements.Count; i++)
            {
                var block = Block(statements, i);
                if (!seen.TryGetValue(block, out var where)) seen[block] = where = [];
                where.Add((name, i));
            }
        }

        // OVERLAPPING WINDOWS ARE ONE CLONE. A twelve-statement copy contains
        // seven duplicated windows, and reporting all seven buries every other
        // finding underneath it. So a window whose start is already inside a
        // reported clone is that clone continuing, not a new one.
        var reported = new List<string>();
        var covered = new HashSet<(string, int)>();

        foreach (var statements in reduced
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => entry.Value))
        {
            for (var i = 0; i + Window <= statements.Count; i++)
            {
                var where = seen[Block(statements, i)];

                if (where.Count < 2 || where.Any(one => covered.Contains(one))) continue;

                foreach (var (file, at) in where)
                    for (var span = 0; span < Window; span++) covered.Add((file, at + span));

                var places = where.Select(one => $"{one.File}:{reduced[one.File][one.At].Line}");

                reported.Add(
                    $"{string.Join(" and ", places)}:\n    "
                    + Block(statements, i).Replace("\n", "\n    ", StringComparison.Ordinal));
            }
        }

        return reported;
    }

    private static string Block(IReadOnlyList<(string Text, int Line)> statements, int from) =>
        string.Join("\n", statements.Skip(from).Take(Window).Select(one => one.Text));

    /// <summary>
    /// One file, reduced to the statements it actually makes.
    /// </summary>
    /// <remarks>
    /// <b>Whitespace is collapsed and comments are dropped</b>, so a clone cannot
    /// be hidden by reindenting it or by writing a different comment above it —
    /// which is exactly what a copy-paste with a fresh explanation looks like.
    /// </remarks>
    private static IReadOnlyList<(string Text, int Line)> Statements(IReadOnlyList<string> lines)
    {
        var kept = new List<(string, int)>();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();

            if (line.Length == 0 || Comment.IsMatch(line) || Noise.IsMatch(line)) continue;

            kept.Add((Spacing.Replace(line, " "), i + 1));
        }

        return kept;
    }
}
