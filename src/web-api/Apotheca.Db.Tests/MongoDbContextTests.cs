using Apotheca.Db.Models;
using MongoDB.Driver;
using NUnit.Framework;

namespace Apotheca.Db.Tests
{
    [TestFixture]
    [Category("Integration")]
    [Category("MongoDb")]
    public class MongoDbContextTests
    {

        #region GetOneAsync Tests

        [Test]
        public void GetOneAsync_EntityExists_ReturnsCorrectly()
        {
            string containerName = $"GetOneAsync_{Guid.NewGuid()}";
            MongoDbContext dbContext = CreateDbContext();

            // create two items
            UserDbModel model1 = CreateTestUserDbModel(dbContext, containerName);
            UserDbModel model2 = CreateTestUserDbModel(dbContext, containerName);

            // execute and assert
            var filterDef = new FilterDefinitionBuilder<UserDbModel>();
            var filter = filterDef.Eq(x => x.Id, model1.Id);

            UserDbModel result = dbContext.GetOneAsync<UserDbModel>(containerName, filter).GetAwaiter().GetResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(model1.Id, result.Id);

            DeleteTestDatabase(dbContext);
        }

        [Test]
        public void GetOneAsync_EntityDoesNotExist_ReturnsNull()
        {
            string containerName = $"GetOneAsync_{Guid.NewGuid()}";
            MongoDbContext dbContext = CreateDbContext();

            // execute and assert
            var filterDef = new FilterDefinitionBuilder<UserDbModel>();
            var filter = filterDef.Eq(x => x.Id, "nothingtoseehere");

            UserDbModel result = dbContext.GetOneAsync<UserDbModel>(containerName, filter).GetAwaiter().GetResult();
            Assert.IsNull(result);
            
            DeleteTestDatabase(dbContext);
        }

        #endregion

        #region GetManyAsync Tests

        [Test]
        public void GetManyAsync_MultipleEntitiesAllExists_ReturnsCorrectly()
        {
            string containerName = $"GetOneAsync_{Guid.NewGuid()}";
            MongoDbContext dbContext = CreateDbContext();

            // create two items
            UserDbModel model1 = CreateTestUserDbModel(dbContext, containerName);
            UserDbModel model2 = CreateTestUserDbModel(dbContext, containerName);
            UserDbModel model3 = CreateTestUserDbModel(dbContext, containerName);

            // execute and assert
            var filterDef = new FilterDefinitionBuilder<UserDbModel>();
            var filter = filterDef.In(x => x.Id, new string[] { model1.Id, model2.Id, model3.Id });

            List<UserDbModel> result = dbContext.GetManyAsync<UserDbModel>(containerName, filter).GetAwaiter().GetResult().ToList();
            Assert.AreEqual(3, result.Count);
            Assert.IsNotNull(result.SingleOrDefault(x => x.Id == model1.Id));
            Assert.IsNotNull(result.SingleOrDefault(x => x.Id == model2.Id));
            Assert.IsNotNull(result.SingleOrDefault(x => x.Id == model3.Id));

            DeleteTestDatabase(dbContext);
        }

        [Test]
        public void GetManyAsync_MultipleEntitiesPartiallyExists_ReturnsCorrectly()
        {
            string containerName = $"GetOneAsync_{Guid.NewGuid()}";
            MongoDbContext dbContext = CreateDbContext();

            // create two items
            UserDbModel model1 = CreateTestUserDbModel(dbContext, containerName);
            UserDbModel model2 = CreateTestUserDbModel(dbContext, containerName);
            UserDbModel model3 = CreateTestUserDbModel(dbContext, containerName);

            // execute and assert
            var filterDef = new FilterDefinitionBuilder<UserDbModel>();
            var filter = filterDef.In(x => x.Id, new string[] { model1.Id, model2.Id, "notfound" });

            List<UserDbModel> result = dbContext.GetManyAsync<UserDbModel>(containerName, filter).GetAwaiter().GetResult().ToList();
            Assert.AreEqual(2, result.Count);
            Assert.IsNotNull(result.SingleOrDefault(x => x.Id == model1.Id));
            Assert.IsNotNull(result.SingleOrDefault(x => x.Id == model2.Id));
            Assert.IsNull(result.SingleOrDefault(x => x.Id == model3.Id));

            DeleteTestDatabase(dbContext);
        }

        [Test]
        public void GetManyAsync_MultipleEntitiesNoneExist_ReturnsEmptyCollection()
        {
            string containerName = $"GetOneAsync_{Guid.NewGuid()}";
            MongoDbContext dbContext = CreateDbContext();

            // create two items
            CreateTestUserDbModel(dbContext, containerName);
            CreateTestUserDbModel(dbContext, containerName);
            CreateTestUserDbModel(dbContext, containerName);

            // execute and assert
            var filterDef = new FilterDefinitionBuilder<UserDbModel>();
            var filter = filterDef.In(x => x.Id, new string[] { "notfound" });

            List<UserDbModel> result = dbContext.GetManyAsync<UserDbModel>(containerName, filter).GetAwaiter().GetResult().ToList();
            Assert.AreEqual(0, result.Count);

            DeleteTestDatabase(dbContext);
        }

        #endregion

        #region InsertAsync Tests

        [Test]
        public void InsertAsync_InsertsEntity()
        {
            string containerName = $"GetOneAsync_{Guid.NewGuid()}";
            MongoDbContext dbContext = CreateDbContext();

            // execute 
            UserDbModel model = TestDbModelHelper.GetUserDbModel();
            UserDbModel insertedModel = dbContext.InsertAsync<UserDbModel>(containerName, model).GetAwaiter().GetResult();


            var filterDef = new FilterDefinitionBuilder<UserDbModel>();
            var filter = filterDef.Eq(x => x.Id, model.Id);
            UserDbModel fetchResult = dbContext.GetOneAsync<UserDbModel>(containerName, filter).GetAwaiter().GetResult();

            Assert.IsNotNull(model.Id, insertedModel.Id);
            Assert.IsNotNull(fetchResult.Id, insertedModel.Id);

            DeleteTestDatabase(dbContext);
        }

        #endregion

        private MongoDbContext CreateDbContext(string databaseName = "ApothecaTest")
        {
            return new MongoDbContext(new MongoDB.Driver.MongoClient("mongodb://localhost"), databaseName);
        }

        private void DeleteTestDatabase(MongoDbContext dbContext, string dbName = "ApothecaTest")
        {
            dbContext.MongoClient.DropDatabase(dbName);
        }

        private UserDbModel CreateTestUserDbModel(MongoDbContext dbContext, string containerName)
        {
            UserDbModel model = TestDbModelHelper.GetUserDbModel();
            dbContext.InsertAsync<UserDbModel>(containerName, model).Wait();
            return model;
        }

    }
}