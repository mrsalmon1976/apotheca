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
    public class WorkspaceRepositoryTests
    {
        #region GetManyByIdAsync Tests

        [Test]
        public void GetManyByIdAsync_OnExecution_InvokesDbContextCall()
        {
            IDbContext dbContext = Substitute.For<IDbContext>();
            string[] ids = new string[2] { "1", "2" };

            WorkspaceRepository workspaceRepository = new WorkspaceRepository();
            workspaceRepository.GetManyByIdAsync(dbContext, ids).Wait();

            dbContext.Received(1).GetManyAsync<WorkspaceDbModel>(DbSchema.WorkspaceContainer, Arg.Any<FilterDefinition<WorkspaceDbModel>>());

        }

        [Test]
        [Category("RequiresMongoDb")]
        [Category("Integration")]
        public void GetManyByIdAsync_ItemsDoNotExist_ReturnsEmptyCollection()
        {
            MongoDbContext dbContext = RepositoryTestUtils.CreateDbContext();
            dbContext.InsertAsync(DbSchema.WorkspaceContainer, DbModelUtils.GetWorkspaceDbModel()).Wait();
            dbContext.InsertAsync(DbSchema.WorkspaceContainer, DbModelUtils.GetWorkspaceDbModel()).Wait();
            dbContext.InsertAsync(DbSchema.WorkspaceContainer, DbModelUtils.GetWorkspaceDbModel()).Wait();
            IEnumerable<string> ids = new string[2] { "1", "2" };

            WorkspaceRepository workspaceRepository = new WorkspaceRepository();
            IEnumerable<WorkspaceDbModel> result = workspaceRepository.GetManyByIdAsync(dbContext, ids).Result;

            Assert.AreEqual(0, result.Count());

        }

        [Test]
        [Category("RequiresMongoDb")]
        [Category("Integration")]
        public void GetManyByIdAsync_ItemsExist_ReturnsCollection()
        {
            MongoDbContext dbContext = RepositoryTestUtils.CreateDbContext();
            WorkspaceDbModel workspaceDbModel1 = DbModelUtils.GetWorkspaceDbModel();
            dbContext.InsertAsync(DbSchema.WorkspaceContainer, workspaceDbModel1).Wait();
            WorkspaceDbModel workspaceDbModel2 = DbModelUtils.GetWorkspaceDbModel();
            dbContext.InsertAsync(DbSchema.WorkspaceContainer, workspaceDbModel2).Wait();

            dbContext.InsertAsync(DbSchema.WorkspaceContainer, DbModelUtils.GetWorkspaceDbModel()).Wait();
            dbContext.InsertAsync(DbSchema.WorkspaceContainer, DbModelUtils.GetWorkspaceDbModel()).Wait();

            IEnumerable<string> ids = new string[2] { workspaceDbModel1.Id, workspaceDbModel2.Id };

            WorkspaceRepository workspaceRepository = new WorkspaceRepository();
            List<WorkspaceDbModel> result = workspaceRepository.GetManyByIdAsync(dbContext, ids).Result.ToList();

            Assert.AreEqual(2, result.Count());
            Assert.IsNotNull(result.SingleOrDefault(x => x.Id == workspaceDbModel1.Id));
            Assert.IsNotNull(result.SingleOrDefault(x => x.Id == workspaceDbModel2.Id));
        }

        #endregion


    }
}