using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Where a commitment lives, and what each rule for deciding that costs — <b>fork 61,
/// priced before it is built.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Two rules, and they buy opposite things.</b> Placing by the whole identity spreads
/// evenly and sends a child away from its parent, because a child's scope is its parent's
/// plus one code and hashes nowhere near it. Placing by the parent keeps a lineage together
/// and deduplicates nothing, because <c>{x}</c> repaired with <c>z</c> and <c>{z}</c>
/// repaired with <c>x</c> are one scope reached from two roots.
/// </para>
/// <para>
/// <b>John's is the minimum code of the sorted scope</b>, and it buys both. A scope is
/// already canonicalised as a sorted set, so the minimum is free; identical scopes have
/// identical minima and land together, and a lineage rooted at that minimum stays together.
/// What it cannot buy is spread, and that is the number this file exists for.
/// </para>
/// <para>
/// <b>And the hard limit is distinct roots rather than balance.</b> A rule placing by one
/// code can only reach as many machines as there are distinct minima, so a narrow world
/// caps the fleet no matter how many holders are brought up — and that is a fact about the
/// front end's vocabulary, which is the one thing a placement rule may not depend on.
/// </para>
/// </remarks>
public sealed class PlacingTests(ITestOutputHelper output)
{
    /// <summary>A population trained on the multiplexer.</summary>
    /// <param name="address">Address bits.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    private static List<Commitment> Trained(int address, int seed)
    {
        var brain = new Brain(new CommittingSettings(), seed);

        new MultiplexerRun(
            new MultiplexerSettings { Address = address }, brain, seed).Run(20_000);

        return [.. brain.Held.All];
    }

    /// <summary>How lopsided a placement is, and how much of the fleet it reaches.</summary>
    /// <param name="all">The whole population.</param>
    /// <param name="holders">How many machines.</param>
    /// <param name="where">Which machine a commitment goes to.</param>
    private static (int Empty, int Most, double Ratio) Spread(
        IReadOnlyList<Commitment> all, int holders, Func<Commitment, ulong> where)
    {
        var sizes = new int[holders];

        foreach (var one in all) sizes[(int)(where(one) % (ulong)holders)]++;

        var most = sizes.Max();

        // The ratio of the fullest machine to the average, which is what a deployment
        // feels. A fleet is as slow as its slowest holder and as big as its biggest, so the
        // mean says nothing about whether it fits.
        return (sizes.Count(one => one == 0), most, most / (all.Count / (double)holders));
    }

    /// <summary>
    /// <b>What placing by the minimum code costs in spread</b> — a grid, and no bar.
    /// </summary>
    /// <remarks>
    /// <b>Measured before the change rather than after it, which is the point.</b> Fork 3
    /// has carried <i>prefix placement recovers much of it at unmeasured cost in load</i>
    /// since it was written, and the cost has stayed unmeasured because nothing needed it.
    /// Fork 61 needs it: the rule is chosen for what it deduplicates, and if it cannot fill
    /// a fleet then what it deduplicates is beside the point.
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_placing_by_the_minimum_code_costs_in_spread()
    {
        output.WriteLine(
            "bits | holders | rule     | empty | fullest | fullest over average | roots");

        foreach (var address in new[] { 2, 3 })
        {
            var all = Trained(address, seed: 1);

            var roots = all.Select(one => one.Scope[0]).Distinct().Count();

            foreach (var holders in new[] { 3, 6, 12 })
            {
                var byName = Spread(all, holders, one => one.Identity.Value);
                var byRoot = Spread(all, holders, one => one.Scope[0].Value);

                output.WriteLine(
                    $"{address + (1 << address),4} | {holders,7} | identity | {byName.Empty,5} "
                    + $"| {byName.Most,7} | {byName.Ratio,20:F2} | {roots,5}");

                output.WriteLine(
                    $"{address + (1 << address),4} | {holders,7} | minimum  | {byRoot.Empty,5} "
                    + $"| {byRoot.Most,7} | {byRoot.Ratio,20:F2} | {roots,5}");
            }
        }

        // The one assertion is that the two rules are different rules, which is not
        // guaranteed and would make every row above a tautology. A one-code commitment has
        // its scope's minimum AS its only code, and its identity is a hash of that scope --
        // so if the hash happened to preserve the ordering the grid would compare a rule
        // with itself.
        var narrow = Trained(2, seed: 1);

        Assert.NotEqual(
            Spread(narrow, 6, one => one.Identity.Value).Most,
            Spread(narrow, 6, one => one.Scope[0].Value).Most);

        // NO BAR ON THE RATIO, because what a fleet can tolerate is a fact about the fleet
        // and nothing here knows how big the machines are.
    }
}
