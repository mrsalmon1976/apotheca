using Apotheca.Db.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.Db
{
    public class MongoDbContext : IDbContext
    {

        public MongoDbContext(MongoClient mongoClient, string databaseName)
        {
            this.MongoClient = mongoClient;
            this.DatabaseName = databaseName;
            this.Database = this.MongoClient.GetDatabase(this.DatabaseName);
        }

        public MongoClient MongoClient { get; set; }

        public IMongoDatabase Database { get; set; }

        public string DatabaseName { get; set; }

        #region Transaction Implementation - Not Currently Supported for Standalone

        //private IClientSessionHandle? _sessionHandler;

        //public bool IsInTransaction => (this._sessionHandler != null && this._sessionHandler.IsInTransaction);

        //public async Task BeginTransactionAsync()
        //{
        //    _sessionHandler = await this.MongoClient.StartSessionAsync();
        //    _sessionHandler.StartTransaction();
        //}

        //public async Task CommitAsync()
        //{
        //    if (_sessionHandler == null) { throw new NullReferenceException("Cannot call CommitAsync without calling BeginTransaction"); };
        //    await _sessionHandler.CommitTransactionAsync();
        //}

        //public async Task RollbackAsync()
        //{
        //    if (_sessionHandler == null) { throw new NullReferenceException("Cannot call CommitAsync without calling BeginTransaction"); };
        //    await _sessionHandler.AbortTransactionAsync();
        //}

        #endregion

        public async Task<T> GetOneAsync<T>(string containerName, FilterDefinition<T> filter) where T : DbModel
        {
            IMongoCollection<T> coll = Database.GetCollection<T>(containerName);
            var result = await coll.FindAsync<T>(filter);
            var item = await result.SingleOrDefaultAsync<T>();
            return item;
        }

        public async Task<IEnumerable<T>> GetManyAsync<T>(string containerName, FilterDefinition<T> filter) where T : DbModel
        {
            IMongoCollection<T> coll = Database.GetCollection<T>(containerName);
            var result = await coll.FindAsync<T>(filter);
            return result.ToEnumerable<T>();
        }

        public async Task<T> InsertAsync<T>(string containerName, T model) where T : DbModel
        {
            IMongoCollection<T> coll = Database.GetCollection<T>(containerName);
            await coll.InsertOneAsync(model);
            return model;
        }

        public void Dispose()
        {
        }




    }
}
