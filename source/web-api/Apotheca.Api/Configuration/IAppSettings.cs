namespace Apotheca.Api.Configuration;

public interface IAppSettings
{
    string[] CorsAllowedOrigins { get; }
    string? FirebaseCredentialsPath { get; }
    string FirebaseProjectId { get; }
}
