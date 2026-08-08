namespace OpenPlexus.Tests;

/// <summary>
/// What a test needs when the messages have to cross a real socket.
/// </summary>
/// <remarks>
/// <para>
/// <b>WRITTEN ONCE BECAUSE TWO FILES NOW OPEN PORTS AND THE COPIES WOULD DRIFT.</b>
/// <c>PostedTests</c> put the thinking path on a wire and <c>AskedTests</c> puts the
/// learning path on one; a port picker and a hang detector in both is two places for the
/// bound to differ, and a flake blamed on the bus is the outcome.
/// </para>
/// <para>
/// <b>AND NEITHER OF THESE IS A CLAIM ABOUT SPEED.</b> The bound below is a deadlock
/// detector in exactly the sense <see cref="Fixture.Patience"/> is: what is being asserted
/// is that a message ARRIVES, and a bound generous enough to be uninteresting is what keeps
/// a slow machine from being reported as a broken one. Anything asserting a COST must
/// assert a count.
/// </para>
/// </remarks>
public static class Wired
{
    /// <summary>
    /// A port nothing else is using.
    /// </summary>
    /// <remarks>
    /// <b>TAKEN FROM THE OPERATING SYSTEM RATHER THAN COUNTED UP FROM A CONSTANT.</b> A
    /// fixed port makes a test that passes alone and fails beside anything else holding
    /// it — including a previous run of itself that has not finished releasing it, which
    /// is the flake that would get blamed on the bus.
    /// </remarks>
    public static string Free()
    {
        using var taken = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);

        taken.Start();
        var port = ((System.Net.IPEndPoint)taken.LocalEndpoint).Port;
        taken.Stop();

        return $"http://localhost:{port}";
    }

    /// <summary>
    /// Waits on something's own completion signal rather than looking for it.
    /// </summary>
    /// <param name="done">The signal — <c>Gathering.Everyone</c>, in practice.</param>
    /// <returns>Whether it completed inside the hang detector's bound.</returns>
    /// <remarks>
    /// <b>USE THIS WHERE THE WAIT IS BEING TIMED, AND <see cref="UntilAsync"/> ANYWHERE
    /// ELSE.</b> Polling every ten milliseconds is fine for asserting that a message
    /// arrived and useless for measuring how long it took — the reading would be the poll's
    /// granularity, and a clock that reports how often somebody looked is exactly the trap
    /// this repo keeps a list for.
    /// </remarks>
    public static async Task<bool> ArrivedAsync(Task done)
    {
        ArgumentNullException.ThrowIfNull(done);

        try
        {
            await done.WaitAsync(TimeSpan.FromSeconds(20)).ConfigureAwait(false);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>Waits for a condition without deciding a miss by a deadline.</summary>
    /// <param name="done">What is being waited for.</param>
    /// <returns>Whether it ever became true.</returns>
    /// <remarks>
    /// <b>IT RETURNS RATHER THAN THROWS, so the caller says what the failure MEANS.</b> A
    /// timeout here is <i>the envelope never arrived</i> in one file and <i>the holder that
    /// died answered anyway</i> in another, and a shared exception message could say
    /// neither.
    /// </remarks>
    public static async Task<bool> UntilAsync(Func<bool> done)
    {
        ArgumentNullException.ThrowIfNull(done);

        var gave = DateTime.UtcNow + TimeSpan.FromSeconds(20);

        while (DateTime.UtcNow < gave)
        {
            if (done()) return true;
            await Task.Delay(10).ConfigureAwait(false);
        }

        return done();
    }
}
