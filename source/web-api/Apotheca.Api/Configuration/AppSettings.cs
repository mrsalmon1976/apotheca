using Microsoft.Extensions.Configuration;

namespace Apotheca.Api.Configuration;

public class AppSettings : IAppSettings
{
    public string[] CorsAllowedOrigins { get; }
    public string? FirebaseCredentialsPath { get; }
    public string FirebaseProjectId { get; }
    public bool PubSubRequireAuthentication { get; }
    public string? PubSubAudience { get; }
    public string StorageBucketName { get; }
    public string? StorageEmulatorHost { get; }

    public AppSettings(IConfiguration configuration)
    {
        CorsAllowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        FirebaseCredentialsPath = configuration["Firebase:CredentialsPath"];
        FirebaseProjectId = configuration["Firebase:ProjectId"]
            ?? throw new InvalidOperationException("Firebase:ProjectId is not configured.");
        PubSubRequireAuthentication = configuration.GetValue<bool>("PubSub:RequireAuthentication", defaultValue: true);
        PubSubAudience = configuration["PubSub:Audience"];
        StorageBucketName = configuration["Storage:BucketName"]
            ?? throw new InvalidOperationException("Storage:BucketName is not configured.");
        StorageEmulatorHost = configuration["Storage:EmulatorHost"];
    }
}
