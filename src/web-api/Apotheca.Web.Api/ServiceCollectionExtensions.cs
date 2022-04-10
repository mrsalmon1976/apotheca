using Apotheca.Db;

public static class ServiceCollectionExtensions
{

    public static void RegisterDbContext(this IServiceCollection collection)
    {
        collection.AddSingleton<IDbContext>((sp) => {
            IDbContext dbContext = new CosmosDbContext("https://localhost:8081", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", "Apotheca");
            return dbContext;
        });

    }

    public static void RegisterRepositories(this IServiceCollection collection)
    {
        //collection.AddTransient<IMoveRepository, MoveRepository>();
    }
}

