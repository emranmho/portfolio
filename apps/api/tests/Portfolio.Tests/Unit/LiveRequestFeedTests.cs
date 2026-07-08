using FluentAssertions;
using Portfolio.Api.Live;

namespace Portfolio.Tests.Unit;

public class LiveRequestFeedTests
{
    [Fact]
    public async Task Subscriber_receives_events_published_after_it_subscribes()
    {
        var feed = new LiveRequestFeed();
        using var cts = new CancellationTokenSource();
        var subscription = feed.Subscribe(cts.Token).GetAsyncEnumerator(cts.Token);

        // Subscribe() only registers once MoveNextAsync starts pulling; run it as a
        // background task so Publish (below) reaches a live subscriber, not a dropped one.
        var moveNext = subscription.MoveNextAsync().AsTask();
        await WaitUntilAsync(() => feed.SubscriberCount == 1);

        var evt = new LiveRequestEvent("GET", "/api/whoami", 200, 12.3, DateTime.UtcNow);
        feed.Publish(evt);

        (await moveNext).Should().BeTrue();
        subscription.Current.Should().Be(evt);

        cts.Cancel();
    }

    [Fact]
    public async Task Unsubscribing_removes_the_subscriber()
    {
        var feed = new LiveRequestFeed();
        using var cts = new CancellationTokenSource();
        var subscription = feed.Subscribe(cts.Token).GetAsyncEnumerator(cts.Token);
        _ = subscription.MoveNextAsync();

        cts.Cancel();

        await WaitUntilAsync(() => feed.SubscriberCount == 0);
        feed.SubscriberCount.Should().Be(0);
    }

    [Fact]
    public void Publish_with_no_subscribers_does_not_throw()
    {
        var feed = new LiveRequestFeed();

        var act = () => feed.Publish(new LiveRequestEvent("GET", "/api/health", 200, 1, DateTime.UtcNow));

        act.Should().NotThrow();
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
            await Task.Delay(10);
    }
}
