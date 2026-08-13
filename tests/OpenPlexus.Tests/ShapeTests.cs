using System.Reflection;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// A world says what it is LOOKING AT. It does not say how to think about it.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's rule, 2026-08-04, and this is the check that was missing.</b> The
/// point of this project is one brain that several worlds are shown to. A dial
/// that lives on a world's constructor breaks that twice over: the world decides
/// how the walk behaves, and the SAME dial ends up with different defaults in
/// different worlds — <c>Ranking</c> was <c>Sum</c> on bAbI and <c>Agreement</c> on
/// CLEVR, so two measurements that looked comparable were not.
/// </para>
/// <para>
/// <b>Why it went unnoticed for so long, which is the lesson rather than the
/// bug.</b> Every budget in this suite guards the CODE — dead members, clones, doc
/// words, dial count. None of them guarded the SHAPE. So a dial could be added to a
/// world's constructor forever and no check could see it, including
/// <see cref="DialTests"/>, whose entire job is noticing dials arrive: it
/// enumerates the brain's settings record, and these were never in it.
/// </para>
/// <para>
/// <b>The list reached nought on the day it was written.</b> It began as fifteen
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
    /// <para>
    /// <b>Every one of these is about WHAT IS BEING SHOWN or WHERE IT RUNS, never
    /// about how the walk behaves.</b> The world's own settings; the dials as one
    /// object; the seed; the cluster topology; the bus's lateness; and data to
    /// read. A new name here needs an argument, which is the point.
    /// </para>
    /// <para>
    /// <b><c>brain</c> is the handing-in this check exists to enforce, arriving as
    /// something it did not recognise.</b> <i>Brain dials are built once and handed in;
    /// a world turns only its own.</i> A runner taking the whole brain as ONE object is
    /// that rule kept — the dials are assembled outside and the world cannot reach a
    /// single one of them. Taking a settings record instead would be the fault, so the
    /// name is admitted with a TYPE beside it rather than on its own.
    /// </para>
    /// <para>
    /// <b>And the translation is a third thing that belongs at the join.</b> Whether a
    /// picture is read whole or in patches, whether a reading is banded or winnowed, and
    /// which frozen encoder it passes through are none of them facts about the problem
    /// and none of them settings on the brain — so <c>looking</c>, <c>fronting</c> and
    /// <c>through</c> live exactly where <see cref="Machines.Trial{TSeen}"/> says the
    /// choice is made. Putting them on the brain would be a brain that knows worlds
    /// exist; putting them inside a world would be a world deciding what is perceived.
    /// </para>
    /// </remarks>
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "world", "settings", "dials", "seed", "clusters", "replicas", "late", "primer",
        "brain", "looking", "fronting", "through",

        // An instrument switch rather than a dial, and the difference is decidable: a
        // dial changes what the run DOES and this changes only whether a reading is
        // taken. It is on a world rather than on the brain because the census needs the
        // world's own soundness check, which no brain may ever see -- so it could not
        // live anywhere else without handing the learner an answer key.
        //
        // And a name is not an argument, which is what this file's own `brain` CLAUSE
        // SAYS. `CensusTests` asserts that a run with it on and a run with it off learn
        // identically, so the exemption is paid for by a check rather than by this
        // comment -- and the day the census starts changing the run, that goes red here
        // rather than silently making every censused number a different experiment.
        "census",
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

                    // An exemption on a name alone is a hole in the guard, and this is
                    // the one where it would matter: `brain` is admitted because it
                    // hands over the whole brain at once, so a parameter called `brain`
                    // that is a settings record -- or anything else a world could reach
                    // a dial through -- is the exact fault this file was written for.
                    if (name == "brain" && taken.ParameterType != typeof(Brain))
                    {
                        outstanding.Add($"{world.Name}.{name}:{taken.ParameterType.Name}");
                        continue;
                    }

                    if (Allowed.Contains(name)) continue;

                    outstanding.Add($"{world.Name}.{name}");
                }
            }
        }

        output.WriteLine(
            outstanding.Count == 0
                ? "every world takes only what it is showing"
                : string.Join("\n", outstanding.Order(StringComparer.Ordinal)));

        // A dial that arrived on a world since the list was written.
        var fresh = outstanding.Except(NotYetMoved, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToList();

        Assert.True(fresh.Count == 0,
            $"new dial(s) on a world: {string.Join(", ", fresh)}. A world says what "
            + "it is looking at; put this on `CommittingSettings` where the dial census "
            + "can see it, or add it to `Allowed` with an argument for why it is "
            + "about the data rather than the brain.");

        // and the other direction, so the list cannot rot into a record of dials
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
        // The budget, and it sits at the current count rather than above it. There
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
        typeof(CommittingSettings).Assembly
            .GetTypes()
            .Where(one => one.IsClass && one.IsPublic && one.Name.EndsWith("Run", StringComparison.Ordinal))
            .OrderBy(one => one.Name, StringComparer.Ordinal);
}
