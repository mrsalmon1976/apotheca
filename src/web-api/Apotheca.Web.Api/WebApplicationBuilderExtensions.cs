using Apotheca.Db;

public static class WebApplicationBuilderExtensions
{

    public static void ExecuteMigrations(this WebApplication app)
    {
        IServiceProvider serviceProvider = app.Services;
        MongoDbContext dbContext = (MongoDbContext)serviceProvider.GetRequiredService<IDbContext>();

        Console.Write("Running database migrations...");
        MongoDbMigrator.RunMigrations(dbContext);
        Console.WriteLine("done.");
    }
}

