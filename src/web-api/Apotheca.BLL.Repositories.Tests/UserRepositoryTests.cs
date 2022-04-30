using Apotheca.BLL.Repositories.Tests.Utils;
using Apotheca.Db;
using Apotheca.Db.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NSubstitute;
using NUnit.Framework;

namespace Apotheca.BLL.Repositories.Tests
{
    [TestFixture]
    public class UserRepositoryTests
    {
        #region GetByIdAsync Tests

        [Test]
        public void GetByIdAsync_OnExecution_InvokesDbContextCall()
        {
            IDbContext dbContext = Substitute.For<IDbContext>();
            string id = Guid.NewGuid().ToString();

            UserRepository userRepository = new UserRepository();
            userRepository.GetByIdAsync(dbContext, id).Wait();

            dbContext.Received(1).GetOneAsync<UserDbModel>(DbSchema.UserContainer, Arg.Any<FilterDefinition<UserDbModel>>());

        }

        [Test]
        [Category("RequiresMongoDb")]
        [Category("Integration")]
        public void GetByIdAsync_ItemDoesNotExist_ReturnsNull()
        {
            MongoDbContext dbContext = RepositoryTestUtils.CreateDbContext();
            dbContext.InsertAsync(DbSchema.UserContainer, DbModelUtils.GetUserDbModel()).Wait();
            dbContext.InsertAsync(DbSchema.UserContainer, DbModelUtils.GetUserDbModel()).Wait();
            dbContext.InsertAsync(DbSchema.UserContainer, DbModelUtils.GetUserDbModel()).Wait();
            string id = Guid.NewGuid().ToString();

            UserRepository userRepository = new UserRepository();
            UserDbModel result = userRepository.GetByIdAsync(dbContext, id).Result;

            Assert.IsNull(result);

        }

        [Test]
        [Category("RequiresMongoDb")]
        [Category("Integration")]
        public void GetByIdAsync_ItemDoesExist_ReturnsSingleItem()
        {
            MongoDbContext dbContext = RepositoryTestUtils.CreateDbContext();
            UserDbModel userDbModel = DbModelUtils.GetUserDbModel();
            dbContext.InsertAsync(DbSchema.UserContainer, userDbModel).Wait();
            dbContext.InsertAsync(DbSchema.UserContainer, DbModelUtils.GetUserDbModel()).Wait();
            dbContext.InsertAsync(DbSchema.UserContainer, DbModelUtils.GetUserDbModel()).Wait();

            UserRepository userRepository = new UserRepository();
            UserDbModel result = userRepository.GetByIdAsync(dbContext, userDbModel.Id).Result;

            Assert.IsNotNull(result);
            Assert.AreEqual(userDbModel.Id, result.Id);
            Assert.AreEqual(userDbModel.UserName, result.UserName);
        }

        #endregion

        #region GetByAuthIdAsync Tests

        [Test]
        public void GetByAuthIdAsync_OnExecution_InvokesDbContextCall()
        {
            IDbContext dbContext = Substitute.For<IDbContext>();
            string id = Guid.NewGuid().ToString();

            UserRepository userRepository = new UserRepository();
            userRepository.GetByAuthIdAsync(dbContext, id).Wait();

            dbContext.Received(1).GetOneAsync<UserDbModel>(DbSchema.UserContainer, Arg.Any<FilterDefinition<UserDbModel>>());

        }

        [Test]
        [Category("RequiresMongoDb")]
        [Category("Integration")]
        public void GetByAuthIdAsync_ItemDoesNotExist_ReturnsNull()
        {
            MongoDbContext dbContext = RepositoryTestUtils.CreateDbContext();
            dbContext.InsertAsync(DbSchema.UserContainer, DbModelUtils.GetUserDbModel()).Wait();
            dbContext.InsertAsync(DbSchema.UserContainer, DbModelUtils.GetUserDbModel()).Wait();
            dbContext.InsertAsync(DbSchema.UserContainer, DbModelUtils.GetUserDbModel()).Wait();
            string authId = Guid.NewGuid().ToString();

            UserRepository userRepository = new UserRepository();
            UserDbModel result = userRepository.GetByAuthIdAsync(dbContext, authId).Result;

            Assert.IsNull(result);

        }

        [Test]
        [Category("RequiresMongoDb")]
        [Category("Integration")]
        public void GetByAuthIdAsync_ItemDoesExist_ReturnsSingleItem()
        {
            MongoDbContext dbContext = RepositoryTestUtils.CreateDbContext();
            UserDbModel userDbModel = DbModelUtils.GetUserDbModel();
            dbContext.InsertAsync(DbSchema.UserContainer, userDbModel).Wait();
            dbContext.InsertAsync(DbSchema.UserContainer, DbModelUtils.GetUserDbModel()).Wait();
            dbContext.InsertAsync(DbSchema.UserContainer, DbModelUtils.GetUserDbModel()).Wait();

            UserRepository userRepository = new UserRepository();
            UserDbModel result = userRepository.GetByAuthIdAsync(dbContext, userDbModel.AuthId).Result;

            Assert.IsNotNull(result);
            Assert.AreEqual(userDbModel.Id, result.Id);
            Assert.AreEqual(userDbModel.AuthId, result.AuthId);
        }

        #endregion
    }
}