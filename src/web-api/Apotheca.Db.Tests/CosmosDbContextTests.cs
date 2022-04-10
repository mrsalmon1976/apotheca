using Apotheca.Db.Models;
using Microsoft.Azure.Cosmos;
using NUnit.Framework;

namespace Apotheca.Db.Tests
{
    [TestFixture]
    public class CosmosDbContextTests
    {
        [Test]
        public void InitialiseAsync_SingleExecution_CreatesDatabase()
        {
            string dbName = $"ApothecaTest_{Guid.NewGuid().ToString()}";
            CosmosDbContext cosmosDbContext = CreateDbContext(dbName);

            // create the database
            bool result = cosmosDbContext.InitialiseAsync().Result;
            Assert.IsTrue(result);

            DeleteTestDatabase(dbName, cosmosDbContext);
        }

        [Test]
        public void InitialiseAsync_MultipleExecutions_DoesNotThrowException()
        {
            string dbName = $"ApothecaTest_{Guid.NewGuid().ToString()}";
            CosmosDbContext cosmosDbContext = CreateDbContext(dbName);

            // create the database
            bool result1 = cosmosDbContext.InitialiseAsync().Result;
            Assert.IsTrue(result1);

            bool result2 = cosmosDbContext.InitialiseAsync().Result;
            Assert.IsTrue(result2);

            DeleteTestDatabase(dbName, cosmosDbContext);
        }

        [TestCase(DbSchema.UserContainer.Name)]
        [TestCase(DbSchema.WorkspaceContainer.Name)]
        public void InitialiseAsync_SingleExecution_CreatesContainer(string containerName)
        {
            string dbName = $"ApothecaTest_{Guid.NewGuid().ToString()}";
            CosmosDbContext cosmosDbContext = CreateDbContext(dbName);

            // create the database
            cosmosDbContext.InitialiseAsync().GetAwaiter().GetResult();

            // check that the container exists
            List<string> containers = GetContainers(cosmosDbContext.CosmosClient.GetDatabase(dbName));
            Assert.Contains(containerName, containers);

            DeleteTestDatabase(dbName, cosmosDbContext);
        }

        [Test]
        public void InitialiseAsync_SingleExecution_CreatesCorrectContainerCount()
        {
            string dbName = $"ApothecaTest_{Guid.NewGuid().ToString()}";
            CosmosDbContext cosmosDbContext = CreateDbContext(dbName);

            // create the database
            cosmosDbContext.InitialiseAsync().GetAwaiter().GetResult();

            // check that the container exists
            List<string> containers = GetContainers(cosmosDbContext.CosmosClient.GetDatabase(dbName));
            Assert.AreEqual(2, containers.Count);

            DeleteTestDatabase(dbName, cosmosDbContext);
        }

        private CosmosDbContext CreateDbContext(string databaseName)
        {
            return new CosmosDbContext("https://localhost:8081", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", databaseName);
        }

        private List<string> GetContainers(Database database)
        {
            FeedIterator<ContainerProperties> iterator = database.GetContainerQueryIterator<ContainerProperties>();
            FeedResponse<ContainerProperties> containers = iterator.ReadNextAsync().GetAwaiter().GetResult();

            return containers.Select(x => x.Id).ToList();
        }

        private void DeleteTestDatabase(string dbName, CosmosDbContext cosmosDbContext)
        {
            cosmosDbContext.CosmosClient.GetDatabase(dbName).DeleteAsync().Wait();
        }

    }
}