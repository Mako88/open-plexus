namespace Plexus.Host;

/// <summary>
/// Runs one seeded world through one engine and prints what it learnt.
/// </summary>
/// <remarks>
/// <para>
/// The first useful command is the role-permutation world: print the learnt commitment, the
/// environment it was grounded in, the prediction it issued and how that settled. Four lines
/// is enough to see whether a rule transferred by binding or by coincidence.
/// </para>
/// <para>
/// It composes and does not decide. Every dial arrives from the command line, and none of
/// them has a default here, so a run whose settings were not stated does not start.
/// </para>
/// </remarks>
internal static class Program
{
    private static int Main(string[] arguments)
    {
        Console.Error.WriteLine(
            "Plexus greenfield host: no runnable command yet. "
            + $"({arguments.Length} argument(s) ignored.)");

        return 1;
    }
}
