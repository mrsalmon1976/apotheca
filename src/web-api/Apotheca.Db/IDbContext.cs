using Apotheca.Db.Models;
using MongoDB.Driver;

namespace Apotheca.Db
{
    public interface IDbContext : IDisposable
    {

        Task<IEnumerable<T>> GetManyAsync<T>(string containerName, FilterDefinition<T> filter) where T : DbModel;

        Task<T> GetOneAsync<T>(string containerName, FilterDefinition<T> filter) where T : DbModel;


        Task<T> InsertAsync<T>(string containerName, T model) where T : DbModel;

    }
}