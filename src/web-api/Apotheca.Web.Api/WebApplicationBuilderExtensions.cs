using Apotheca.Db;
using Apotheca.Repository;

public static class WebApplicationBuilderExtensions
{

    public static void ExecuteMigrations(this WebApplication app)
    {
        IServiceProvider serviceProvider = app.Services;
        IDbContext dbContext = serviceProvider.GetRequiredService<IDbContext>();
        bool isInitialised = dbContext.InitialiseAsync().GetAwaiter().GetResult();
        if (!isInitialised)
        {
            throw new Exception("Failed to initialise database - check log files for information");
        }

        // TODO: This is all test code - don't forget to remove!!!!!

        Apotheca.Db.Models.WorkspaceDbModel w = new Apotheca.Db.Models.WorkspaceDbModel();
        w.Name = "This is a test";
        dbContext.CreateItemAsync<Apotheca.Db.Models.WorkspaceDbModel>(w).GetAwaiter().GetResult();

        WorkspaceRepository wr = new WorkspaceRepository();
        List<string> ids = new List<string>() { w.Id };
        var workspaces = wr.GetManyByIdAsync(dbContext, ids).GetAwaiter().GetResult();
    }
}

