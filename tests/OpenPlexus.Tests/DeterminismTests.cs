using System.Text.RegularExpressions;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// What a run's IDENTITY is, which is fork 12 from the side nothing else covers.
/// </summary>
/// <remarks>
/// <para>
/// <b>The run half of this file went with the walk</b>, and the property did not. Fork 12
/// asked whether a fixed seed reproduces a run exactly, and it is answered on this side by
/// <c>GradedTests.A_fixed_seed_reproduces_a_graded_run_exactly</c> — which asks MORE than
/// the deleted version did, running its two copies side by side so a learner that agreed
/// with itself through anything ambient could not pass.
/// </para>
/// <para>
/// <b>What is left here is the equality itself</b>, and it is a different question. Every
/// one of those reproducibility tests compares two reports with <c>Assert.Equal</c>, so
/// what a report counts as part of itself decides what they are asserting. A wall clock in
/// there turns every one of them red on a correct machine; a field MISSING from there makes
/// every one of them pass for free. Both faults are invisible from inside the tests that
/// depend on them, which is why they are asserted here instead.
/// </para>
/// </remarks>
public sealed class DeterminismTests
{

    /// <summary>
    /// A short multiplexer run, for the tests below that want a <see cref="Tally"/> and
    /// do not care what is in it.
    /// </summary>
    private static Tally Counted() =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = 2 },
            new Brain(new CommittingSettings(), seed: 1),
            seed: 1).Run(rounds: 200).Tally;

    /// <summary>
    /// <b>A wall clock is not part of a run's identity</b>, and for two days it was.
    /// </summary>
    /// <remarks>
    /// <b>The three <i>a fixed seed reproduces a run exactly</i> tests</b> went red on a
    /// correct machine the moment <see cref="Spent"/> joined <see cref="Tally"/>,
    /// because a record compares every field it has and milliseconds do not repeat.
    /// Every other number in those reports was identical to the digit. See
    /// <see cref="Spent.Equals(Spent)"/> for why the fix is there rather than here.
    /// </remarks>
    [Fact]
    public void Two_runs_differing_only_in_how_long_they_took_are_the_same_run()
    {
        var tally = Counted();

        Assert.Equal(
            tally,
            tally with { Spent = tally.Spent with { Firing = tally.Spent.Firing + 1000.0 } });
    }

    /// <summary>
    /// <b>The companion, and it is the half that was actually dangerous.</b>
    /// </summary>
    /// <remarks>
    /// A clock inside the report did not merely turn three tests red. It made every
    /// <c>Assert.NotEqual</c> over a <see cref="Tally"/> pass for free — the clocks
    /// always differ, so the controls sitting beside those three tests could not fail
    /// whatever the learner did. <b>Excluding the clock is what ARMS them</b>, and a
    /// rule saying two reports are always equal would disarm them again just as
    /// thoroughly, so it is asserted rather than assumed.
    /// </remarks>
    [Fact]
    public void And_a_report_that_differs_anywhere_else_is_still_a_different_run()
    {
        var tally = Counted();

        Assert.NotEqual(tally, tally with { Rounds = tally.Rounds + 1 });
        Assert.NotEqual(tally, tally with { Right = tally.Right + 1 });
        Assert.NotEqual(tally, tally with { Separations = tally.Separations + 1 });
    }

    /// <summary>
    /// <b>Nothing derives a number from a hash the runtime randomises.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="HashCode"/> and <see cref="object.GetHashCode"/> are seeded once per PROCESS,
    /// so anything built on them is reproducible within one run and arbitrary across two. Five
    /// XML comments in <c>src</c> say so and none of them was a check — and a documented
    /// promise is not a check, which is this repo's own line about its own faults.
    /// </para>
    /// <para>
    /// <b>What it cost was a reading nobody could repeat.</b> <c>Alternating.Shuffled</c> drew
    /// its null from <c>HashCode.Combine</c>, so the categories a stream derives were a
    /// function of the process. Two runs of one seed gave 98 admitted proposals and 114, and
    /// the number that mattered went 4 and 0 — which reads as a run being chaotic and was the
    /// codebook moving underneath it.
    /// </para>
    /// <para>
    /// <b>Declaring one is fine and CALLING one is not.</b> An override exists so a type can
    /// go in a dictionary for the length of a process, which is what a hash is for. What is
    /// refused is a value derived from one outliving the process that made it.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_code_in_the_library_derives_a_value_from_a_randomised_hash()
    {
        var called = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var path in Tree.Sources("src"))
        {
            var source = File.ReadAllText(path);
            var relative = Path.GetRelativePath(Tree.Repo(), path);

            var lines = source.Split('\n');

            for (var at = 0; at < lines.Length; at++)
            {
                // Comments say the rule constantly and must not trip it, which is why this
                // reads the code rather than the file. The rule is written down in five
                // remarks that all name the thing they refuse.
                var line = lines[at].Split("//")[0];

                if (line.Contains("HashCode.Combine", StringComparison.Ordinal)
                    || Regex.IsMatch(line, @"\.GetHashCode\s*\("))
                    called.Add($"{relative}:{at + 1}");
            }
        }

        Assert.True(called.Count == 0,
            $"{called.Count} place(s) derive a value from a hash the runtime randomises per "
            + $"process: {string.Join(", ", called)}. Use `Hashing`, whose fold and mix are "
            + "this repo's own and identical on every machine forever. An override of "
            + "`GetHashCode` is not this -- what is refused is CALLING one.");
    }

    /// <summary>
    /// <b>No record compared by an array's identity is ever a key.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// A record's generated equality compares each member with its own, and
    /// <see cref="System.Collections.Immutable.ImmutableArray{T}"/> compares by the identity of
    /// the array it wraps. So two records built separately from identical contents are never
    /// equal — which is invisible until one is used somewhere equality DECIDES something, and
    /// then it is a lookup that always misses.
    /// </para>
    /// <para>
    /// <b>Declaring one is fine and keying on one is not.</b> Most of these are reports, read
    /// once and never compared, and demanding hand-written equality on all of them would be
    /// eleven rewrites for a fault none of them can have. Three types here already define
    /// their own because the fault bit them; this says the rest may not reach the position
    /// where it would.
    /// </para>
    /// <para>
    /// <b>The declared generic positions rather than every comparison.</b> A cast to a
    /// dictionary key is where the original fault lived and it is where a text check can see
    /// it; an <c>Assert.Equal</c> over two of these would need the type inferred, and a check
    /// that half-covers reads exactly like one that covers.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_record_holding_an_immutable_array_is_used_where_equality_decides()
    {
        var bare = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var path in Tree.Sources("src"))
        {
            var code = Bare(File.ReadAllText(path));

            foreach (Match hit in Regex.Matches(
                code,
                @"^(?:internal|public)\s+(?:sealed\s+)?(?:readonly\s+)?record\s+"
                + @"(?:struct\s+|class\s+)?([A-Za-z_][A-Za-z0-9_]*)",
                RegexOptions.Multiline))
            {
                var after = code[hit.Index..];

                var next = Regex.Match(
                    after[hit.Length..],
                    @"^(?:internal|public)\s+(?:sealed\s+|readonly\s+|static\s+|abstract\s+|"
                    + @"partial\s+)*(?:record|class|interface|enum|struct)\s",
                    RegexOptions.Multiline);

                var body = next.Success
                    ? after[hit.Length..(hit.Length + next.Index)]
                    : after[hit.Length..];

                // Its own equality is the opt-in, and the three types that have one wrote it
                // because this bit them. A record that says how it compares is saying it knows.
                if (body.Contains("ImmutableArray<", StringComparison.Ordinal)
                    && !body.Contains("bool Equals(", StringComparison.Ordinal)
                    && !body.Contains("override int GetHashCode", StringComparison.Ordinal))
                    bare.Add(hit.Groups[1].Value);
            }
        }

        Assert.NotEmpty(bare);

        var keyed = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var path in Tree.Sources("src").Concat(Tree.Sources("tests")))
        {
            var code = Bare(File.ReadAllText(path));
            var relative = Path.GetRelativePath(Tree.Repo(), path);

            foreach (var name in bare)
                foreach (var shape in new[]
                {
                    $@"Dictionary<\s*{name}\s*,",
                    $@"HashSet<\s*{name}\s*>",
                    $@"SortedSet<\s*{name}\s*>",
                    $@"ConcurrentDictionary<\s*{name}\s*,",
                    $@"ILookup<\s*{name}\s*,",
                })
                    if (Regex.IsMatch(code, shape))
                        keyed.Add($"{name} in {relative}");
        }

        Assert.True(keyed.Count == 0,
            $"{keyed.Count} use(s) of a record that compares by an array's identity where "
            + $"equality decides: {string.Join(", ", keyed)}. Two of these built separately "
            + "from identical contents are never equal, so the lookup always misses. Give the "
            + "type its own `Equals` and `GetHashCode` over the array's CONTENTS, or key on "
            + "something else.");
    }

    /// <summary>Source with its comments taken out, so a remark cannot trip a check.</summary>
    /// <param name="source">The file.</param>
    private static string Bare(string source) =>
        Regex.Replace(
            Regex.Replace(
                Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline),
                @"^\s*///.*$",
                " ",
                RegexOptions.Multiline),
            @"//.*",
            " ");
}
