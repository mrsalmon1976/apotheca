using Microsoft.Extensions.Configuration;

namespace Apotheca.Api.Configuration;

public class AppSettings : IAppSettings
{
    public string[] CorsAllowedOrigins { get; }
    public string? FirebaseCredentialsPath { get; }
    public string FirebaseProjectId { get; }

    public AppSettings(IConfiguration configuration)
    {
        CorsAllowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        FirebaseCredentialsPath = configuration["Firebase:CredentialsPath"];
        FirebaseProjectId = configuration["Firebase:ProjectId"]
            ?? throw new InvalidOperationException("Firebase:ProjectId is not configured.");
    }
}
