namespace Apotheca.Api.Events;

public interface IEventPublisher
{
    Task PublishAsync<T>(string topicId, T eventData, CancellationToken cancellationToken = default) where T : class;
}
