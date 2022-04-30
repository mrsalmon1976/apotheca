using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace Apotheca.Db.Tests
{
    [TestFixture]
    [Category("Integration")]
    [Category("MongoDb")]
    public class MongoDbMigratorTests
    {

        private const string DefaultTestDbName = "ApothecaTest_MongoDbMigrator";

        #region GetOneAsync Tests

        [Test]
        public void RunMigrations_ValidateTables()
        {
            MongoDbContext dbContext = CreateDbContext();

            MongoDbMigrator.RunMigrations(dbContext);

            // validate tables
            List<string> collectionNames = dbContext.MongoClient.GetDatabase(DefaultTestDbName).ListCollectionNames().ToList();
            Assert.AreEqual(2, collectionNames.Count);

            Assert.Contains(DbSchema.UserContainer, collectionNames);
            Assert.Contains(DbSchema.WorkspaceContainer, collectionNames);

            DeleteTestDatabase(dbContext);
        }

        [Test]
        public void RunMigrations_ValidateIndexes()
        {
            MongoDbContext dbContext = CreateDbContext();

            MongoDbMigrator.RunMigrations(dbContext);

            AssertIndexCount(2, dbContext.Database, DbSchema.UserContainer);
            AssertIndexExists(dbContext.Database, DbSchema.UserContainer, DbSchema.UserContainerIndexes.AuthId);
            AssertIndexCount(1, dbContext.Database, DbSchema.WorkspaceContainer);

            DeleteTestDatabase(dbContext);
        }

        #endregion


        private void AssertIndexCount(int expectedIndexCount, IMongoDatabase database, string collectionName)
        {
            var collection = database.GetCollection<BsonDocument>(collectionName);
            List<BsonDocument> indexDocuments = collection.Indexes.List().ToList();
            Assert.AreEqual(expectedIndexCount, indexDocuments.Count);
        }

        private void AssertIndexExists(IMongoDatabase database, string collectionName, string indexName)
        {
            var collection = database.GetCollection<BsonDocument>(collectionName);
            IEnumerable<BsonDocument> indexDocuments = collection.Indexes.List().ToList();
            foreach (BsonDocument index in indexDocuments)
            {
                string currentIndexName = index.GetValue("name").AsString;
                if (indexName == currentIndexName)
                {
                    Assert.Pass();
                    return;
                }
            }
            Assert.Fail($"Index {indexName} does not exist on collection {collectionName}");
        }

        private MongoDbContext CreateDbContext(string databaseName = DefaultTestDbName)
        {
            return new MongoDbContext(new MongoDB.Driver.MongoClient("mongodb://localhost"), databaseName);
        }

        private void DeleteTestDatabase(MongoDbContext dbContext, string dbName = DefaultTestDbName)
        {
            dbContext.MongoClient.DropDatabase(dbName);
        }

    }
}