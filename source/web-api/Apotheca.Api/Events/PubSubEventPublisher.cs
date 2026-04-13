using System.Collections.Concurrent;
using System.Text.Json;
using Apotheca.Api.Configuration;
using Google.Cloud.PubSub.V1;

namespace Apotheca.Api.Events;

public class PubSubEventPublisher(IAppSettings appSettings, ILogger<PubSubEventPublisher> logger) : IEventPublisher
{
    private readonly ConcurrentDictionary<string, Task<PublisherClient>> _clients = new();

    public async Task PublishAsync<T>(string topicId, T eventData, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var clientTask = _clients.GetOrAdd(topicId, id =>
                new PublisherClientBuilder
                {
                    TopicName = TopicName.FromProjectTopic(appSettings.FirebaseProjectId, id),
                    EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOrProduction,
                }.BuildAsync());

            var client = await clientTask;
            var json = JsonSerializer.Serialize(eventData);
            await client.PublishAsync(json);

            logger.LogInformation("Published event to topic {TopicId}", topicId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish event to topic {TopicId}. Event data: {EventType}", topicId, typeof(T).Name);
        }
    }
}
