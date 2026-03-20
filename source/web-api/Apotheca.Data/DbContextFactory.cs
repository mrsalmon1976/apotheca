using Microsoft.Extensions.Configuration;

namespace Apotheca.Data;

public class DbContextFactory : IDbContextFactory
{
    private readonly string _connectionString;

    public DbContextFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
    }

    public async Task<IDbContext> CreateAsync(CancellationToken cancellationToken = default)
    {
        var context = new DbContext(_connectionString);
        await context.OpenAsync(cancellationToken);
        return context;
    }
}
