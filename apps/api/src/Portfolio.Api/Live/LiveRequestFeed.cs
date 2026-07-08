using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Portfolio.Api.Live;

/// <summary>Anonymized request event: route template only — no IPs, no query strings, no slugs.</summary>
public sealed record LiveRequestEvent(
    string Method,
    string Route,
    int Status,
    double ElapsedMs,
    DateTime TimestampUtc);

/// <summary>
/// Singleton fan-out for the live request feed. The metrics middleware publishes
/// every /api request; each SSE subscriber gets a bounded channel that drops the
/// oldest events if the client can't keep up — a feed viewer wants "now", not backlog.
/// </summary>
public sealed class LiveRequestFeed
{
    private readonly ConcurrentDictionary<Guid, Channel<LiveRequestEvent>> _subscribers = new();

    public int SubscriberCount => _subscribers.Count;

    public void Publish(LiveRequestEvent evt)
    {
        foreach (var channel in _subscribers.Values)
            channel.Writer.TryWrite(evt);
    }

    public async IAsyncEnumerable<LiveRequestEvent> Subscribe(
        [EnumeratorCancellation] CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<LiveRequestEvent>(
            new BoundedChannelOptions(64) { FullMode = BoundedChannelFullMode.DropOldest });
        _subscribers[id] = channel;
        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(ct))
                yield return evt;
        }
        finally
        {
            _subscribers.TryRemove(id, out _);
        }
    }
}
