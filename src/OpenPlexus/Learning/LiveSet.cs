using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Learning;

/// <summary>What started and what stopped between two frames.</summary>
public readonly record struct Changes
{
    public required ImmutableArray<Code> Started { get; init; }
    public required ImmutableArray<Code> Stopped { get; init; }

    /// <summary>Whether this frame said anything at all. A stable scene is silent.</summary>
    public bool Quiet => Started.IsEmpty && Stopped.IsEmpty;
}

/// <summary>
/// What is currently on, for one input machine.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is what makes a continuous stream discrete without sampling it.</b>
/// Most things persist — a thing here in one moment is usually still here in
/// the next — and sampling on a tick counts a persistent code every tick, so it
/// co-occurs with everything that happens while it is there. That is the
/// ever-present hub the edge weighting spends all its effort refusing,
/// manufactured on purpose.
/// </para>
/// <para>
/// So a machine emits on onset and offset and never per tick. <b>Persistence is
/// the absence of a message.</b>
/// </para>
/// </remarks>
public sealed class LiveSet
{
    /// <summary>Code to when it started. Duration is offset minus this.</summary>
    private readonly Dictionary<Code, long> _live = [];

    /// <summary>
    /// Takes the codes present in this frame and returns what started and what
    /// stopped, by diffing against what was live. <b>Everything that persists
    /// produces nothing at all.</b>
    /// </summary>
    public Changes Update(IReadOnlyCollection<Code> present, long now)
    {
        ArgumentNullException.ThrowIfNull(present);

        var here = present as HashSet<Code> ?? [.. present];
        var started = ImmutableArray.CreateBuilder<Code>();
        var stopped = ImmutableArray.CreateBuilder<Code>();

        foreach (var code in here)
        {
            // A PERSISTING CODE KEEPS ITS ORIGINAL START TIME. Refreshing it
            // here would silently make every duration zero, and duration is the
            // one thing this representation gains over a set of moments.
            if (_live.TryAdd(code, now)) started.Add(code);
        }

        foreach (var code in _live.Keys)
        {
            if (!here.Contains(code)) stopped.Add(code);
        }

        foreach (var code in stopped) _live.Remove(code);

        return new Changes
        {
            Started = started.ToImmutable(),
            Stopped = stopped.ToImmutable(),
        };
    }

    /// <summary>The codes currently on.</summary>
    public IReadOnlyCollection<Code> Live => _live.Keys;

    /// <summary>
    /// When a live code started. Feeds duration, which nothing represents yet.
    /// </summary>
    public long StartedAt(Code code) =>
        _live.TryGetValue(code, out var at)
            ? at
            : throw new KeyNotFoundException($"{code} is not live");
}
