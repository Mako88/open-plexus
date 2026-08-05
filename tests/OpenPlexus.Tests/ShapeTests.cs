using System.Reflection;
using OpenPlexus.Graph;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// A world says what it is LOOKING AT. It does not say how to think about it.
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S RULE, 2026-08-04, AND THIS IS THE CHECK THAT WAS MISSING.</b> The
/// point of this project is one brain that several worlds are shown to. A dial
/// that lives on a world's constructor breaks that twice over: the world decides
/// how the walk behaves, and the SAME dial ends up with different defaults in
/// different worlds — <c>Ranking</c> was <c>Sum</c> on bAbI and <c>Agreement</c> on
/// CLEVR, so two measurements that looked comparable were not.
/// </para>
/// <para>
/// <b>WHY IT WENT UNNOTICED FOR SO LONG, WHICH IS THE LESSON RATHER THAN THE
/// BUG.</b> Every budget in this suite guards the CODE — dead members, clones, doc
/// words, dial count. None of them guarded the SHAPE. So a dial could be added to a
/// world's constructor forever and no check could see it, including
/// <see cref="DialTests"/>, whose entire job is noticing dials arrive: it
/// enumerates <see cref="WalkSettings"/>, and these were never there.
/// </para>
/// <para>
/// <b>THE LIST REACHED NOUGHT ON THE DAY IT WAS WRITTEN.</b> It began as fifteen
/// dials across seven worlds and was a to-do list that failed the build, which is
/// the only kind that gets done. It is kept at nought rather than deleted, because
/// what it guards is not the migration — it is the next world somebody adds.
/// </para>
/// </remarks>
public sealed class ShapeTests(ITestOutputHelper output)
{
    /// <summary>
    /// What a world's constructor is legitimately allowed to take.
    /// </summary>
    /// <remarks>
    /// <b>Every one of these is about WHAT IS BEING SHOWN or WHERE IT RUNS, never
    /// about how the walk behaves.</b> The world's own settings; the dials as one
    /// object; the seed; the cluster topology; the bus's lateness; and data to
    /// read. A new name here needs an argument, which is the point.
    /// </remarks>
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "world", "settings", "dials", "seed", "clusters", "replicas", "late", "primer",
    };

    /// <summary>
    /// Dials still living on a world. <b>Empty, and it stays empty.</b>
    /// </summary>
    /// <remarks>
    /// <b>Fifteen, across seven worlds, and now none.</b> Kept as an empty set
    /// rather than deleted so the check reads the same way it did while the work
    /// was outstanding — anything that shows up here again is a regression with a
    /// name, not a mystery.
    /// </remarks>
    private static readonly HashSet<string> NotYetMoved = new(StringComparer.Ordinal);

    [Fact]
    public void No_world_takes_a_dial_that_belongs_to_the_brain()
    {
        var outstanding = new List<string>();

        foreach (var world in Worlds())
        {
            foreach (var made in world.GetConstructors())
            {
                foreach (var taken in made.GetParameters())
                {
                    var name = taken.Name!;
                    if (Allowed.Contains(name)) continue;

                    outstanding.Add($"{world.Name}.{name}");
                }
            }
        }

        output.WriteLine(
            outstanding.Count == 0
                ? "every world takes only what it is showing"
                : string.Join("\n", outstanding.Order(StringComparer.Ordinal)));

        // A DIAL THAT ARRIVED ON A WORLD SINCE THE LIST WAS WRITTEN.
        var fresh = outstanding.Except(NotYetMoved, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToList();

        Assert.True(fresh.Count == 0,
            $"new dial(s) on a world: {string.Join(", ", fresh)}. A world says what "
            + "it is looking at; put this on `WalkSettings` where the dial census "
            + "can see it, or add it to `Allowed` with an argument for why it is "
            + "about the data rather than the walk.");

        // AND THE OTHER DIRECTION, so the list cannot rot into a record of dials
        // that have already moved -- the same failure the doc's ticked boxes and
        // the dead-code list are both checked for.
        var done = NotYetMoved.Except(outstanding, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToList();

        Assert.True(done.Count == 0,
            $"listed as outstanding and already moved: {string.Join(", ", done)}. "
            + "Take it off the list so the number left is the truth.");
    }

    [Fact]
    public void The_list_of_worlds_still_holding_dials_only_ever_shrinks()
    {
        // THE BUDGET, AND IT SITS AT THE CURRENT COUNT RATHER THAN ABOVE IT. There
        // is no ordinary edit that should raise this: every entry is a world
        // deciding something the brain should decide, and the whole direction of
        // travel is towards nought.
        Assert.True(NotYetMoved.Count == 0,
            $"{NotYetMoved.Count} dials live on worlds again. This reached NOUGHT on "
            + "2026-08-04 and the only direction left is back up, which is the "
            + "one this must never go.");
    }

    /// <summary>Every world runner, found rather than listed.</summary>
    /// <remarks>
    /// <b>By reflection, because a hand-kept list is the thing that goes stale.</b>
    /// A new world joins this check by existing.
    /// </remarks>
    private static IEnumerable<Type> Worlds() =>
        typeof(WalkSettings).Assembly
            .GetTypes()
            .Where(one => one.IsClass && one.IsPublic && one.Name.EndsWith("Run", StringComparison.Ordinal))
            .OrderBy(one => one.Name, StringComparer.Ordinal);
}
