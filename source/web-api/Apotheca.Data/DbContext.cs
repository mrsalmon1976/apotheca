using System.Data;
using Dapper;
using Npgsql;

namespace Apotheca.Data;

public class DbContext : IDbContext
{
    private readonly NpgsqlConnection _connection;
    private NpgsqlTransaction? _transaction;

    public IDbConnection Connection => _connection;
    public IDbTransaction? Transaction => _transaction;

    public DbContext(string connectionString)
    {
        _connection = new NpgsqlConnection(connectionString);
    }

    internal async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        await _connection.OpenAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _connection.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(sql, param, _transaction, cancellationToken: cancellationToken);
        return _connection.ExecuteAsync(command);
    }

    public Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(sql, param, _transaction, cancellationToken: cancellationToken);
        return _connection.QueryAsync<T>(command);
    }

    public Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(sql, param, _transaction, cancellationToken: cancellationToken);
        return _connection.QueryFirstOrDefaultAsync<T>(command);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to roll back.");

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync();

        await _connection.DisposeAsync();
    }
}
