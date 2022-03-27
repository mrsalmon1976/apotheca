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

            cosmosDbContext.CosmosClient.GetDatabase(dbName).DeleteAsync().Wait();
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

            cosmosDbContext.CosmosClient.GetDatabase(dbName).DeleteAsync().Wait();
        }

        private CosmosDbContext CreateDbContext(string databaseName)
        {
            return new CosmosDbContext("https://localhost:8081", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", databaseName);
        }

    }
}