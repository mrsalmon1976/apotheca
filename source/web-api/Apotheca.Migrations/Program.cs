using Apotheca.Migrations;
using DbUp;
using DbUp.Helpers;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var settings = AppSettings.GetInstance(config);

if (string.IsNullOrWhiteSpace(settings.ConnectionString))
    throw new InvalidOperationException(
        "ConnectionString is not configured. Set it in appsettings.json or the ConnectionString environment variable.");

EnsureDatabase.For.PostgresqlDatabase(settings.ConnectionString);

// Folders run in the order listed here. Within each folder, scripts run alphabetically by filename.
// To add a new folder, append its name to this array.
var scriptFolders = new[] { "Schemas", "Tables", "Functions" };

var assemblyName = typeof(Program).Assembly.GetName().Name;

foreach (var folder in scriptFolders)
{
    var prefix = $"{assemblyName}.Scripts.{folder}.";

    var upgrader = DeployChanges.To
        .PostgresqlDatabase(settings.ConnectionString)
        .WithScriptsEmbeddedInAssembly(
            typeof(Program).Assembly,
            s => s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        .WithTransactionPerScript()
        .JournalTo(new NullJournal())
        .LogToConsole()
        .Build();

    var result = upgrader.PerformUpgrade();

    if (!result.Successful)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(result.Error);
        Console.ResetColor();
        return 1;
    }
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Migrations applied successfully.");
Console.ResetColor();
return 0;
