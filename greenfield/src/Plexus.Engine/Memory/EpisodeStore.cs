using Plexus.Core.Knowledge;
using Plexus.Core.Memory;

namespace Plexus.Engine.Memory;

/// <summary>Episodes in memory, append-only.</summary>
public sealed class EpisodeStore : IEpisodeStore
{
    public ValueTask AppendAsync(Observation observation, CancellationToken ct) =>
        throw new NotImplementedException();

    public IAsyncEnumerable<Observation> QueryAsync(EpisodeQuery query, CancellationToken ct) =>
        throw new NotImplementedException();
}
