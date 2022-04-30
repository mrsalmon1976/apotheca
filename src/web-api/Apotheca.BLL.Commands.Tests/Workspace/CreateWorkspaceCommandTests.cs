using Apotheca.BLL.Commands.Workspace;
using Apotheca.Db;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apotheca.Db.Models;

namespace Apotheca.BLL.Commands.Tests.Workspace
{
    [TestFixture]
    public class CreateWorkspaceCommandTests
    {
        [Test]
        public void ExecuteAsync_WithArguments_Creates()
        {
            IDbContext dbContext = Substitute.For<IDbContext>();
            string name = Guid.NewGuid().ToString();
            WorkspaceDbModel workspaceDbModel = new WorkspaceDbModel();


            dbContext.When(x => x.InsertAsync(DbSchema.WorkspaceContainer, Arg.Any<WorkspaceDbModel>())).Do((callInfo) =>
            {
                Assert.AreEqual(DbSchema.WorkspaceContainer, callInfo.ArgAt<string>(0));
                workspaceDbModel = callInfo.ArgAt<WorkspaceDbModel>(1);
            });

            CreateWorkspaceCommand cmd = new CreateWorkspaceCommand();
            cmd.ExecuteAsync(dbContext, name).Wait();

            dbContext.Received(1).InsertAsync(DbSchema.WorkspaceContainer, Arg.Any<WorkspaceDbModel>());
            Assert.AreEqual(name, workspaceDbModel.Name);

        }
    }
}
