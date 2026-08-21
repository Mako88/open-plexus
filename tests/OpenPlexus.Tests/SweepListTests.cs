using System.Reflection;
using System.Text.RegularExpressions;

namespace OpenPlexus.Tests;

/// <summary>
/// The dispatch list in <c>sweeps.yml</c>, checked against the measurements that actually
/// carry the trait — <b>the budget for the one failure class this repo had named and not
/// paid for.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A workflow is the one artifact with no local check.</b> And it was wrong twice in one
/// day. A sweep carrying <c>kind=sweep</c> and named in no list is skipped by the suite
/// by trait and unreachable by dispatch — a grid that exists and cannot be taken, which
/// reads as a measurement waiting to be run rather than as a defect. And a class listed
/// TWICE is two runners doing one job, invisible from the dispatch notice because the notice
/// prints the list it was given.
/// </para>
/// <para>
/// <b>Neither was caught by anything, and both are mechanical.</b> The trait is on the
/// method and the list is a JSON array of substrings in a file; matching one against the
/// other needs no judgement, which is what makes it a check rather than a review.
/// </para>
/// <para>
/// <b>And it is a substring match because the workflow's is.</b> An entry may name a class
/// or one method of one — <c>dotnet test --filter FullyQualifiedName~</c> is what consumes
/// it — so this asks the same question the runner will: does any entry select this method.
/// Anything stricter would go red on the split entries the list exists to allow.
/// </para>
/// </remarks>
public sealed class SweepListTests
{
    /// <summary>Every entry of the <c>all</c> array in the workflow, in file order.</summary>
    /// <remarks>
    /// <b>Read as text rather than as YAML</b>, and that is a deliberate limit. The array
    /// sits inside a shell script inside a <c>run:</c> block, so a YAML parser reaches a
    /// string and has to be told where to look anyway. What this cannot survive is the list
    /// being rewritten in another shape — which would fail loudly here, since no entry would
    /// be found at all.
    /// </remarks>
    private static List<string> Listed()
    {
        var workflow = File.ReadAllText(
            Path.Combine(Tree.Repo(), ".github", "workflows", "sweeps.yml"));

        var array = Regex.Match(workflow, @"all='\[(?<body>[^]]*)\]'", RegexOptions.Singleline);

        Assert.True(array.Success,
            "the `all=` array is not where this check looks for it in sweeps.yml");

        return Regex.Matches(array.Groups["body"].Value, "\"(?<entry>[^\"]+)\"")
            .Select(one => one.Groups["entry"].Value)
            .ToList();
    }

    /// <summary>Every test method carrying the sweep trait, fully qualified.</summary>
    /// <remarks>
    /// <b>Read off the attribute data rather than the attribute.</b> Because xUnit's
    /// <c>TraitAttribute</c> keeps its two strings and exposes neither. What the runner
    /// reads is the constructor arguments, so this reads the same thing.
    /// </remarks>
    private static List<string> Measurements() =>
        typeof(SweepListTests).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttributesData()
                .Where(one => one.AttributeType == typeof(TraitAttribute))
                .Any(one => one.ConstructorArguments.Count == 2
                    && (string?)one.ConstructorArguments[0].Value == Sweeps.Kind
                    && (string?)one.ConstructorArguments[1].Value == Sweeps.Name))
            .Select(method => $"{method.DeclaringType!.FullName}.{method.Name}")
            .Order()
            .ToList();

    [Fact]
    public void Every_measurement_the_suite_skips_can_be_dispatched()
    {
        // THE SUITE EXCLUDES `kind=sweep` and the workflow is the only other way in, so a
        // measurement in neither place runs nowhere at all. That is not a slow grid, it is a
        // grid nobody can take, and it looks exactly like one that has not been asked for.
        var listed = Listed();

        var unreachable = Measurements()
            .Where(one => !listed.Any(one.Contains))
            .ToList();

        Assert.True(
            unreachable.Count == 0,
            "these carry the sweep trait and no entry in sweeps.yml selects them, so the "
            + "suite skips them and a dispatch cannot reach them:\n  "
            + string.Join("\n  ", unreachable));
    }

    [Fact]
    public void No_entry_claims_a_runner_for_work_another_entry_already_has()
    {
        // Two kinds of double-booking and one consequence. A repeated entry is the same
        // string twice, which crept in the first time an entry was removed and re-added; an
        // entry that is a PREFIX of another selects everything the other does, so naming a
        // class beside one of its own methods runs that method on two runners. Both are a
        // runner slot spent on work another slot is already doing, and this account gets
        // twenty at once.
        var listed = Listed();

        var repeated = listed.GroupBy(one => one)
            .Where(one => one.Count() > 1)
            .Select(one => one.Key)
            .ToList();

        Assert.True(repeated.Count == 0,
            "listed more than once in sweeps.yml: " + string.Join(", ", repeated));

        var shadowed = listed
            .Where(one => listed.Any(other => other != one && one.Contains(other)))
            .ToList();

        Assert.True(shadowed.Count == 0,
            "these are also selected by a shorter entry in the same list, so each runs on "
            + "two runners: " + string.Join(", ", shadowed));
    }

    [Fact]
    public void Every_entry_selects_a_measurement_that_exists()
    {
        // The dispatch-time guard brought forward to the build. The workflow already fails a
        // dispatch naming nothing, but only for the ONE substring it was given -- a stale
        // entry in the list is found by whoever happens to ask for it, months later, having
        // waited for a runner first. Renaming a sweep method should go red here instead.
        var measurements = Measurements();

        var stale = Listed()
            .Where(entry => !measurements.Any(one => one.Contains(entry)))
            .ToList();

        Assert.True(stale.Count == 0,
            "listed in sweeps.yml and matching no test carrying the sweep trait: "
            + string.Join(", ", stale));
    }
    /// <summary>
    /// A sweep that trips a bar fails its run — <b>the budget for a failure class</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A grid is dispatched by hand and read by a person, so nothing else looks at whether it
    /// went red. That made one shell detail load-bearing: the measure step pipes
    /// <c>dotnet test</c> into <c>tee</c>, a step runs under <c>bash -e</c> which does not set
    /// <c>pipefail</c>, and the exit code of a pipeline is its LAST command's. <c>tee</c>
    /// always succeeds.
    /// </para>
    /// <para>
    /// So every assertion in every sweep was wired and unable to fire, for as long as the
    /// step has existed. It was found by a grid whose bar tripped, printed its message, and
    /// came back <c>success</c> — the reading was believed only because the log was read by
    /// hand rather than by its colour.
    /// </para>
    /// <para>
    /// <b>A new kind of mistake earns a check rather than a fix</b>, and this is the check.
    /// It reads the workflow rather than the run, so it costs nothing and cannot rot: a
    /// measure step that pipes and does not set <c>pipefail</c> fails the build here.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_sweep_that_trips_a_bar_cannot_report_success()
    {
        var workflow = File.ReadAllText(
            Path.Combine(Tree.Repo(), ".github", "workflows", "sweeps.yml"));

        var measuring = workflow
            .Split("- name:", StringSplitOptions.None)
            .Single(one => one.TrimStart().StartsWith("Measure", StringComparison.Ordinal));

        // Only where it pipes, because a step that does not pipe carries the exit code
        // already and demanding the line anyway would be cargo rather than a check.
        var pipes = measuring.Contains("| tee", StringComparison.Ordinal);

        Assert.True(
            !pipes || measuring.Contains("set -o pipefail", StringComparison.Ordinal),
            "the measure step in `sweeps.yml` pipes `dotnet test` into `tee` and does not "
            + "`set -o pipefail`, so the run takes tee's exit code and a sweep whose "
            + "assertion fires reports success. Every bar in every grid is wired and unable "
            + "to fire while that line is missing");
    }
}

