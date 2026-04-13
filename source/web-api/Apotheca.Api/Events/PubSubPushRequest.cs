using System.Text;
using System.Text.Json;

namespace Apotheca.Api.Events;

/// <summary>
/// The envelope Pub/Sub sends to a push endpoint.
/// </summary>
public class PubSubPushRequest
{
    public PubSubMessage Message { get; init; } = new();
    public string Subscription { get; init; } = string.Empty;

    public T? DecodeMessage<T>() where T : class
    {
        if (string.IsNullOrEmpty(Message.Data))
            return null;

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(Message.Data));
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}

public class PubSubMessage
{
    public string Data { get; init; } = string.Empty;
    public string MessageId { get; init; } = string.Empty;
    public string PublishTime { get; init; } = string.Empty;
}
