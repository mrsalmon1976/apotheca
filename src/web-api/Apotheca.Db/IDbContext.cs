using Apotheca.Db.Models;

namespace Apotheca.Db
{
    public interface IDbContext : IDisposable
    {
        Task<T> CreateItemAsync<T>(T model) where T : DbModel;

        Task<IEnumerable<T>> GetManyItemsAsync<T>(string containerName, string query, IEnumerable<DbParameter> dbParameters) where T : DbModel;
        
        Task<bool> InitialiseAsync();
    }
}