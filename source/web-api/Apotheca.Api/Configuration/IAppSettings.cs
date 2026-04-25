namespace Apotheca.Api.Configuration;

public interface IAppSettings
{
    string[] CorsAllowedOrigins { get; }
    string? FirebaseCredentialsPath { get; }
    string FirebaseProjectId { get; }
    bool PubSubRequireAuthentication { get; }
    string? PubSubAudience { get; }
    string StorageBucketName { get; }
    string? StorageEmulatorHost { get; }
}
