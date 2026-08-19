using System.Reflection;
using System.Text.RegularExpressions;

namespace OpenPlexus.Tests;

/// <summary>
/// Every test class lands in exactly one shard of <c>tests.yml</c> — <b>the budget for the
/// failure class a rebalance would create.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The suite is split four ways by name and the fourth is everything else.</b> Which is one
/// typo from a test that runs nowhere. Three shards name their classes and the last
/// excludes exactly those nine names; a class added to one and not excluded from the other
/// runs twice, and a name misspelt in the exclusion list runs nowhere at all. The second is
/// the dangerous one — a test that runs in no shard is green everywhere, because nothing
/// ever asks it anything.
/// </para>
/// <para>
/// <b>And that is why this exists before the rebalance rather than after it.</b> The fourth
/// shard runs past the timeout while the other three finish in ten minutes, so the list is
/// going to be rewritten by somebody who is not looking for this. <c>SweepListTests</c> is
/// the same check one workflow along, written the same day two entries in <c>sweeps.yml</c>
/// turned out to be wrong.
/// </para>
/// <para>
/// <b>It asks the question the runner will ask.</b> Which is a substring test against the fully
/// qualified name. Nothing here interprets what a shard is FOR; it evaluates the filter
/// the workflow hands <c>dotnet test</c> against the classes the assembly actually has, so a
/// shard that selects nothing and a class that no shard selects are the same finding read
/// from two ends.
/// </para>
/// </remarks>
public sealed class ShardTests
{
    /// <summary>Every filter expression in the shard matrix, in file order.</summary>
    /// <remarks>
    /// <b>READ AS TEXT, WHICH IS <c>SweepListTests</c>' limit and its reason.</b> A YAML
    /// parser would reach the same strings and would have to be told where to look; what
    /// neither survives is the matrix being written in another shape, which fails loudly
    /// here because no entry is found at all.
    /// </remarks>
    private static List<string> Shards()
    {
        var workflow = File.ReadAllText(
            Path.Combine(Tree.Repo(), ".github", "workflows", "tests.yml"));

        var matrix = Regex.Match(
            workflow,
            @"^\s*shard:\s*$(?<body>(\r?\n\s*-\s*""[^""]*"")+)",
            RegexOptions.Multiline);

        Assert.True(matrix.Success,
            "the `shard:` matrix is not where this check looks for it in tests.yml");

        return Regex.Matches(matrix.Groups["body"].Value, @"""(?<filter>[^""]+)""")
            .Select(one => one.Groups["filter"].Value)
            .ToList();
    }

    /// <summary>Every class in this assembly holding a test.</summary>
    /// <remarks>
    /// <b>By what the runner would collect rather than by what the file is called.</b> A
    /// class of helpers is not a shard's business and a class of facts is, so the question
    /// asked is whether anything in it carries a fact or a theory — which is the same
    /// question that decides whether leaving it out of every shard costs anything.
    /// </remarks>
    private static List<string> Classes() =>
        [
            .. typeof(ShardTests).Assembly.GetTypes()
                .Where(one => one.IsClass && !one.IsAbstract)
                .Where(one => one
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Any(method => method
                        .GetCustomAttributes()
                        .Any(attribute => attribute is FactAttribute)))
                .Select(one => one.FullName!)
                .OrderBy(one => one, StringComparer.Ordinal),
        ];

    /// <summary>Whether one shard's filter selects one class.</summary>
    /// <param name="filter">The expression the workflow hands the runner.</param>
    /// <param name="name">A class's fully qualified name.</param>
    /// <remarks>
    /// <b>One joiner an expression, and a mixed one is refused rather than guessed at.</b>
    /// <c>dotnet test</c> gives <c>&amp;</c> and <c>|</c> a precedence and nothing in this
    /// matrix has ever relied on it; a filter that mixed them would be read here by whatever
    /// this function happened to do first, which is how a check comes to disagree with the
    /// runner it is checking. Failing is the honest answer and it costs whoever writes one
    /// the ten lines to say what they meant.
    /// </remarks>
    private static bool Selects(string filter, string name)
    {
        var body = filter.Trim().TrimStart('(').TrimEnd(')');

        var any = body.Contains('|', StringComparison.Ordinal);
        var all = body.Contains('&', StringComparison.Ordinal);

        Assert.False(any && all,
            $"the shard filter `{filter}` mixes & and |, which this check will not guess "
            + "at — split it into two shards or teach this function the precedence");

        var terms = body.Split(any ? '|' : '&', StringSplitOptions.RemoveEmptyEntries);

        return any
            ? terms.Any(one => Matches(one, name))
            : terms.All(one => Matches(one, name));
    }

    /// <summary>Whether one term of a filter selects one class.</summary>
    /// <param name="term">A single <c>FullyQualifiedName</c> clause.</param>
    /// <param name="name">A class's fully qualified name.</param>
    private static bool Matches(string term, string name)
    {
        var negated = term.Contains("!~", StringComparison.Ordinal);

        var wanted = term.Split('~', 2)[1].Trim();

        var has = name.Contains(wanted, StringComparison.Ordinal);

        return negated ? !has : has;
    }

    /// <summary>
    /// <b>No test class runs twice on CI, and none runs nowhere.</b>
    /// </summary>
    /// <remarks>
    /// <b>The second half is the one worth the file.</b> A class in two shards costs a
    /// runner and is visible in two logs; a class in none is green forever and shows up as
    /// an empty column nobody reads. Both are one edit of a name away, and neither has ever
    /// had anything checking for it.
    /// </remarks>
    [Fact]
    public void Every_test_class_lands_in_exactly_one_shard()
    {
        var shards = Shards();

        Assert.True(shards.Count > 1, $"only {shards.Count} shard(s) found in tests.yml");

        var wrong = new List<string>();

        foreach (var name in Classes())
        {
            var landing = shards.Count(one => Selects(one, name));

            if (landing != 1) wrong.Add($"{name} lands in {landing} of {shards.Count} shards");
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} test class(es) are not sharded exactly once. A class in none "
            + "never runs on CI and is green because nothing asked it:\n  "
            + string.Join("\n  ", wrong));
    }

    /// <summary>
    /// <b>And no shard is empty, which is the same mistake read from the other end.</b>
    /// </summary>
    /// <remarks>
    /// A shard naming a class that has been renamed or deleted starts a runner, builds the
    /// solution, selects nothing and reports success — the most expensive way there is to
    /// assert nothing. The check above cannot see it, because every class it knows about is
    /// still landing exactly once.
    /// </remarks>
    [Fact]
    public void Every_shard_selects_something()
    {
        var classes = Classes();

        var empty = Shards()
            .Where(one => !classes.Any(name => Selects(one, name)))
            .ToList();

        Assert.True(empty.Count == 0,
            $"{empty.Count} shard(s) select no test class at all, so a runner is being "
            + "spent to assert nothing:\n  " + string.Join("\n  ", empty));
    }
}
