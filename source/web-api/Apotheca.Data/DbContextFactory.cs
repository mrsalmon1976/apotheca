using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Apotheca.Data;

public class DbContextFactory : IDbContextFactory, IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    public DbContextFactory(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        _dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
    }

    public async Task<IDbContext> CreateAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return new DbContext(connection);
    }

    public async ValueTask DisposeAsync()
    {
        await _dataSource.DisposeAsync();
    }
}
