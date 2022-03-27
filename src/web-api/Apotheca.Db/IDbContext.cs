namespace Apotheca.Db
{
    public interface IDbContext : IDisposable
    {
        Task<bool> InitialiseAsync();
    }
}