using Microsoft.Extensions.Configuration;

namespace Apotheca.Migrations;

public class AppSettings
{
    private static AppSettings? _instance;

    public static AppSettings GetInstance(IConfiguration configuration)
    {
        _instance = configuration.Get<AppSettings>()
            ?? throw new InvalidOperationException("Failed to bind application settings.");

        return _instance;
    }

    public string ConnectionString { get; init; } = string.Empty;
}
