using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.Db
{
    public class CosmosDbContext : IDbContext
    {
        private string _databaseName { get; set; }

        internal CosmosClient CosmosClient { get; set; }

        public CosmosDbContext(string endPointUri, string primaryKey, string databaseName)
        {
            this._databaseName = databaseName;
            this.CosmosClient = new CosmosClient(endPointUri, primaryKey);
        }

        public string DatabaseName => this._databaseName;

        public void Dispose()
        {
            this.CosmosClient?.Dispose();
        }

        public async Task<bool> InitialiseAsync()
        {
            DatabaseResponse response = await this.CosmosClient.CreateDatabaseIfNotExistsAsync(this.DatabaseName);
            if (response == null)
            {
                return false;
            }
            int statusCode = (int)response.StatusCode;
            return (statusCode >= 200 && statusCode < 300);
        }
    }
}
