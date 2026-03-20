namespace Apotheca.Data;

public interface IDbContextFactory
{
    Task<IDbContext> CreateAsync(CancellationToken cancellationToken = default);
}
