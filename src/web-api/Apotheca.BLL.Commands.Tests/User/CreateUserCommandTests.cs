using Apotheca.BLL.Commands.User;
using Apotheca.Db;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apotheca.Db.Models;

namespace Apotheca.BLL.Commands.Tests.User
{
    [TestFixture]
    public class CreateUserCommandTests
    {
        [Test]
        public void ExecuteAsync_WithArguments_Creates()
        {
            IDbContext dbContext = Substitute.For<IDbContext>();
            string authId = Guid.NewGuid().ToString();
            string userName = Guid.NewGuid().ToString();
            string email = Guid.NewGuid().ToString();
            UserDbModel userDbModel = new UserDbModel();


            dbContext.When(x => x.InsertAsync(DbSchema.UserContainer, Arg.Any<UserDbModel>())).Do((callInfo) =>
            {
                Assert.AreEqual(DbSchema.UserContainer, callInfo.ArgAt<string>(0));
                userDbModel = callInfo.ArgAt<UserDbModel>(1);
            });

            CreateUserCommand cmd = new CreateUserCommand();
            cmd.ExecuteAsync(dbContext, authId, userName, email).Wait();

            dbContext.Received(1).InsertAsync(DbSchema.UserContainer, Arg.Any<UserDbModel>());
            Assert.AreEqual(authId, userDbModel.AuthId);
            Assert.AreEqual(userName, userDbModel.UserName);
            Assert.AreEqual(email, userDbModel.Email);

        }
    }
}
