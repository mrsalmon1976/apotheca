using Apotheca.Db.Exceptions;
using Apotheca.Db.Models;
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

        public async Task<T> CreateItemAsync<T>(T model) where T: DbModel
        {
            Container container = this.CosmosClient.GetContainer(this.DatabaseName, model.ContainerName);
            var result = await container.CreateItemAsync<T>(model, model.PartitionKey);
            return result.Resource;
        }

        public async Task<IEnumerable<T>> GetManyItemsAsync<T>(string containerName, string query, IEnumerable<DbParameter> dbParameters) where T : DbModel
        {
            Container container = this.CosmosClient.GetContainer(this.DatabaseName, containerName);

            QueryDefinition queryDefinition = new QueryDefinition(query);
            foreach (DbParameter dbParameter in dbParameters)
            {
                queryDefinition.WithParameter(dbParameter.Key, dbParameter.Value);
            }

            List<T> items = new List<T>();

            using (var result = container.GetItemQueryIterator<T>(queryDefinition))
            {
                while (result.HasMoreResults)
                {
                    foreach (var item in await result.ReadNextAsync())
                    {
                        items.Add(item);
                    }
                }
            }
            return items;

        }

        public void Dispose()
        {
            this.CosmosClient?.Dispose();
        }

        public async Task<bool> InitialiseAsync()
        {
            DatabaseResponse response = await this.CosmosClient.CreateDatabaseIfNotExistsAsync(this.DatabaseName);
            if (response == null)
            {
                throw new DbContextInitialisationException($"Unable to initialise database '{DatabaseName}' - CreateDatabaseIfNotExistsAsync returned null");
            }
            int statusCode = (int)response.StatusCode;
            if (statusCode < 200 || statusCode >= 300)
            {
                throw new DbContextInitialisationException($"Unable to initialise database '{DatabaseName}' - CreateDatabaseIfNotExistsAsync returned status code {statusCode}");
            }

            // create the tables
            Database database = response.Database;
            await CreateContainerIfNotExistsAsync(database, DbSchema.UserContainer.Name, DbSchema.UserContainer.PartitionKeyPath);
            await CreateContainerIfNotExistsAsync(database, DbSchema.WorkspaceContainer.Name, DbSchema.WorkspaceContainer.PartitionKeyPath);

            // everything worked!
            return true;
        }

        private async Task<bool> CreateContainerIfNotExistsAsync(Database database, string containerName, string partitionKeyPath)
        {
            ContainerResponse containerResponse = await database.CreateContainerIfNotExistsAsync(new ContainerProperties(containerName, partitionKeyPath));
            int statusCode = (int)containerResponse.StatusCode;
            if (statusCode < 200 || statusCode >= 300)
            {
                throw new DbContextInitialisationException($"Unable to initialise container '{containerName}' with partition key path '{partitionKeyPath}' - CreateContainerIfNotExistsAsync returned status code {statusCode}");
            }
            return true;

        }
    }
}
