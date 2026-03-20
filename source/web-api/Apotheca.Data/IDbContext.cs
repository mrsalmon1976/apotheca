using System.Data;

namespace Apotheca.Data;

public interface IDbContext : IDisposable, IAsyncDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
