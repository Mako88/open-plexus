using Xunit;

// TESTS RUN ONE AT A TIME, AND IT IS A MEASUREMENT DECISION RATHER THAN A
// CONVENIENCE.
//
// Most of this suite does not assert on code paths, it asserts on NUMBERS the
// system produced, and those numbers move with how busy the machine is.
// Measured: the walk's agreement with itself is 0.8833 +-0.0294 run alone and
// 1.0000 inside the full parallel suite -- delivery is concurrent, so under load
// the ordering that produced the disagreement stops happening. Two tests used to
// deliberately assert nothing about it for that reason, which is a hole in the
// suite dressed up as caution.
//
// The consequence was worse than those two tests. A number measured locally was
// not the number the suite measured, so no reading could be compared against any
// other reading unless both happened to run under the same load -- and nothing
// recorded what the load had been.
//
// COST, MEASURED: 2m57s against 1m32s. Ninety seconds to make every number in
// the project comparable to every other number is the cheapest thing on the
// list.
//
// A test that genuinely needs concurrency must create it INSIDE itself, which is
// what `SensesRun.AskAsync(concept, votes)` does -- concurrency as the thing
// under test rather than as ambient weather.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
