using Apotheca.Db;
using Apotheca.Web.Api.Caching;
using Apotheca.Web.Api.Models;
using Apotheca.Web.Api.Services;
using AutoMapper;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apotheca.BLL.Repositories;
using Apotheca.Db.Models;
using Apotheca.BLL.Commands.User;

namespace Apotheca.Web.Api.Tests.Services.User
{
    [TestFixture]
    public class UserViewServiceTests
    {

        #region EnsureUserExistsAsync Tests

        [Test]
        public void EnsureUserExistsAsync_UserDoesNotExist_ReturnedAfterCreation()
        {
            IDbContext dbContext = Substitute.For<IDbContext>();
            UserInfo userInfo = new UserInfo();
            userInfo.AuthId = Guid.NewGuid().ToString();
            userInfo.Name = Guid.NewGuid().ToString();
            userInfo.Email = Guid.NewGuid().ToString();

            UserDbModel userDbModel = new UserDbModel();
            userDbModel.Id = Guid.NewGuid().ToString();
            userDbModel.AuthId = userInfo.AuthId;

            UserRepository userRepository = Substitute.For<UserRepository>();
            userRepository.GetByAuthIdAsync(dbContext, userInfo.AuthId).Returns<UserDbModel>(x => null);

            IMapper mapper = CreateMapper();

            CreateUserCommand createUserCommand = Substitute.For<CreateUserCommand>();
            createUserCommand.ExecuteAsync(dbContext, userInfo.AuthId, userInfo.Name, userInfo.Email).Returns(userDbModel);

            UserViewService userViewService = new UserViewService(dbContext, mapper, userRepository, createUserCommand);
            UserViewModel result = userViewService.EnsureUserExistsAsync(userInfo).Result;

            Assert.AreEqual(userInfo.AuthId, result.AuthId);
            createUserCommand.Received(1).ExecuteAsync(dbContext, userInfo.AuthId, userInfo.Name, userInfo.Email).Wait();


        }

        [Test]
        public void EnsureUserExistsAsync_UserExists_ReturnedWithoutDbCall()
        {
            IDbContext dbContext = Substitute.For<IDbContext>();
            UserInfo userInfo = new UserInfo();
            userInfo.AuthId = Guid.NewGuid().ToString();
            userInfo.Name = Guid.NewGuid().ToString();
            userInfo.Email = Guid.NewGuid().ToString();

            UserDbModel userDbModel = new UserDbModel();
            userDbModel.Id = Guid.NewGuid().ToString();
            userDbModel.AuthId = userInfo.AuthId;

            UserRepository userRepository = Substitute.For<UserRepository>();
            userRepository.GetByAuthIdAsync(dbContext, userInfo.AuthId).Returns(userDbModel);

            IMapper mapper = CreateMapper();

            CreateUserCommand createUserCommand = Substitute.For<CreateUserCommand>();

            UserViewService userViewService = new UserViewService(dbContext, mapper, userRepository, createUserCommand);
            UserViewModel result = userViewService.EnsureUserExistsAsync(userInfo).Result;

            Assert.AreEqual(userInfo.AuthId, result.AuthId);
            createUserCommand.DidNotReceive().ExecuteAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Wait();

        }

        #endregion

        private IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<UserDbModel, UserViewModel>();
            });

            var mapper = new Mapper(config);
            return mapper;

        }
    }
}
