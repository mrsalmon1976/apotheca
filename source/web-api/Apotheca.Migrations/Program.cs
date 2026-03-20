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

var upgrader = DeployChanges.To
    .PostgresqlDatabase(settings.ConnectionString)
    .WithScriptsEmbeddedInAssembly(typeof(Program).Assembly)
    .WithTransactionPerScript()
    .JournalTo(new NullJournal())   // no record will be kept of which scripts have been run, so all scripts will run every time
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

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Migrations applied successfully.");
Console.ResetColor();
return 0;
