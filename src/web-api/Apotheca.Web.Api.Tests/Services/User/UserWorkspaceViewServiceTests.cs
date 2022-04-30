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

namespace Apotheca.Web.Api.Tests.Services.User
{
    [TestFixture]
    public class UserWorkspaceViewServiceTests
    {

        #region GetUserWorkspaces Tests

        [Test]
        public void GetUserWorkspaces_NoUser_ReturnsEmptyEnumerable()
        {
            string authUserId = Guid.NewGuid().ToString();
            string cacheKey = CacheKeys.UserWorkspace(authUserId);
            IDbContext dbContext = Substitute.For<IDbContext>();
            IEnumerable<WorkspaceViewModel>? cachedViewModel = null;

            UserRepository userRepository = Substitute.For<UserRepository>();
            userRepository.GetByAuthIdAsync(dbContext, authUserId).Returns<UserDbModel>(x => null);

            WorkspaceRepository workspaceRepository = Substitute.For<WorkspaceRepository>();

            IMemoryCacheWrapper memoryCache = Substitute.For<IMemoryCacheWrapper>();
            memoryCache.Get<IEnumerable<WorkspaceViewModel>>(cacheKey).Returns(cachedViewModel);

            UserWorkspaceViewService userWorkspaceViewService = new UserWorkspaceViewService(dbContext, memoryCache, Substitute.For<IMapper>(), userRepository, workspaceRepository);
            IEnumerable<WorkspaceViewModel> result = userWorkspaceViewService.GetUserWorkspaces(authUserId).Result;

            // assert
            Assert.AreEqual(0, result.Count());
            userRepository.Received(1).GetByAuthIdAsync(dbContext, authUserId);
            workspaceRepository.DidNotReceive().GetManyByIdAsync(Arg.Any<IDbContext>(), Arg.Any<IEnumerable<string>>());
        }

        [Test]
        public void GetUserWorkspaces_NoUser_DoesNotCache()
        {
            string authUserId = Guid.NewGuid().ToString();
            string cacheKey = CacheKeys.UserWorkspace(authUserId);
            IDbContext dbContext = Substitute.For<IDbContext>();
            IEnumerable<WorkspaceViewModel>? cachedViewModel = null;

            UserRepository userRepository = Substitute.For<UserRepository>();
            userRepository.GetByAuthIdAsync(dbContext, authUserId).Returns<UserDbModel>(x => null);

            IMemoryCacheWrapper memoryCache = Substitute.For<IMemoryCacheWrapper>();
            memoryCache.Get<IEnumerable<WorkspaceViewModel>>(cacheKey).Returns(cachedViewModel);

            UserWorkspaceViewService userWorkspaceViewService = new UserWorkspaceViewService(dbContext, memoryCache, Substitute.For<IMapper>(), userRepository, Substitute.For<WorkspaceRepository>());
            IEnumerable<WorkspaceViewModel> result = userWorkspaceViewService.GetUserWorkspaces(authUserId).Result;

            // assert
            Assert.AreEqual(0, result.Count());
            memoryCache.Received(1).Get<IEnumerable<WorkspaceViewModel>>(Arg.Any<string>());
            memoryCache.DidNotReceive().Set<IEnumerable<WorkspaceViewModel>>(Arg.Any<string>(), Arg.Any<IEnumerable<WorkspaceViewModel>>(), Arg.Any<TimeSpan>());
        }

        [Test]
        public void GetUserWorkspaces_UserWorkspacesNull_ReturnsEmptyEnumerable()
        {
            string authUserId = Guid.NewGuid().ToString();
            string cacheKey = CacheKeys.UserWorkspace(authUserId);
            IDbContext dbContext = Substitute.For<IDbContext>();
            IEnumerable<WorkspaceViewModel>? cachedViewModel = null;
            UserDbModel userDbModel = new UserDbModel();

            UserRepository userRepository = Substitute.For<UserRepository>();
            userRepository.GetByAuthIdAsync(dbContext, authUserId).Returns<UserDbModel>(userDbModel);

            IMemoryCacheWrapper memoryCache = Substitute.For<IMemoryCacheWrapper>();
            memoryCache.Get<IEnumerable<WorkspaceViewModel>>(cacheKey).Returns(cachedViewModel);

            UserWorkspaceViewService userWorkspaceViewService = new UserWorkspaceViewService(dbContext, memoryCache, Substitute.For<IMapper>(), userRepository, Substitute.For<WorkspaceRepository>());
            IEnumerable<WorkspaceViewModel> result = userWorkspaceViewService.GetUserWorkspaces(authUserId).Result;

            // assert
            Assert.AreEqual(0, result.Count());
            memoryCache.Received(1).Set<IEnumerable<WorkspaceViewModel>>(cacheKey, Arg.Any<IEnumerable<WorkspaceViewModel>>(), Arg.Any<TimeSpan>());
        }

        [Test]
        public void GetUserWorkspaces_UserWorkspacesEmpty_ReturnsEmptyEnumerable()
        {
            string authUserId = Guid.NewGuid().ToString();
            string cacheKey = CacheKeys.UserWorkspace(authUserId);
            IDbContext dbContext = Substitute.For<IDbContext>();
            IEnumerable<WorkspaceViewModel>? cachedViewModel = null;
            UserDbModel userDbModel = new UserDbModel();
            userDbModel.Workspaces = new string[0];

            UserRepository userRepository = Substitute.For<UserRepository>();
            userRepository.GetByAuthIdAsync(dbContext, authUserId).Returns<UserDbModel>(userDbModel);

            IMemoryCacheWrapper memoryCache = Substitute.For<IMemoryCacheWrapper>();
            memoryCache.Get<IEnumerable<WorkspaceViewModel>>(cacheKey).Returns(cachedViewModel);

            UserWorkspaceViewService userWorkspaceViewService = new UserWorkspaceViewService(dbContext, memoryCache, Substitute.For<IMapper>(), userRepository, Substitute.For<WorkspaceRepository>());
            IEnumerable<WorkspaceViewModel> result = userWorkspaceViewService.GetUserWorkspaces(authUserId).Result;

            // assert
            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetUserWorkspaces_WorkspacesExist_FetchesViaRepository()
        {
            string authUserId = Guid.NewGuid().ToString();
            string cacheKey = CacheKeys.UserWorkspace(authUserId);
            IDbContext dbContext = Substitute.For<IDbContext>();
            IEnumerable<WorkspaceViewModel>? cachedViewModel = null;
            UserDbModel userDbModel = new UserDbModel();
            userDbModel.Workspaces = new string[1] {  "workspace1" };

            WorkspaceDbModel[] workspaceDbModels = new WorkspaceDbModel[1] { new WorkspaceDbModel() };

            UserRepository userRepository = Substitute.For<UserRepository>();
            userRepository.GetByAuthIdAsync(dbContext, authUserId).Returns<UserDbModel>(userDbModel);

            WorkspaceRepository workspaceRepository = Substitute.For<WorkspaceRepository>();
            workspaceRepository.GetManyByIdAsync(dbContext, userDbModel.Workspaces).Returns(workspaceDbModels);

            IMemoryCacheWrapper memoryCache = Substitute.For<IMemoryCacheWrapper>();
            memoryCache.Get<IEnumerable<WorkspaceViewModel>>(cacheKey).Returns(cachedViewModel);

            UserWorkspaceViewService userWorkspaceViewService = new UserWorkspaceViewService(dbContext, memoryCache, Substitute.For<IMapper>(), userRepository, workspaceRepository);
            IEnumerable<WorkspaceViewModel> result = userWorkspaceViewService.GetUserWorkspaces(authUserId).Result;

            // assert
            workspaceRepository.Received(1).GetManyByIdAsync(dbContext, userDbModel.Workspaces);
        }

        [Test]
        public void GetUserWorkspaces_WorkspacesExist_CachesResult()
        {
            string authUserId = Guid.NewGuid().ToString();
            string cacheKey = CacheKeys.UserWorkspace(authUserId);
            IDbContext dbContext = Substitute.For<IDbContext>();
            IEnumerable<WorkspaceViewModel>? cachedViewModel = null;
            UserDbModel userDbModel = new UserDbModel();
            userDbModel.Workspaces = new string[1] { "workspace1" };

            WorkspaceDbModel[] workspaceDbModels = new WorkspaceDbModel[1] { new WorkspaceDbModel() };

            UserRepository userRepository = Substitute.For<UserRepository>();
            userRepository.GetByAuthIdAsync(dbContext, authUserId).Returns<UserDbModel>(userDbModel);

            WorkspaceRepository workspaceRepository = Substitute.For<WorkspaceRepository>();
            workspaceRepository.GetManyByIdAsync(dbContext, userDbModel.Workspaces).Returns(workspaceDbModels);

            IMemoryCacheWrapper memoryCache = Substitute.For<IMemoryCacheWrapper>();
            memoryCache.Get<IEnumerable<WorkspaceViewModel>>(cacheKey).Returns(cachedViewModel);

            UserWorkspaceViewService userWorkspaceViewService = new UserWorkspaceViewService(dbContext, memoryCache, Substitute.For<IMapper>(), userRepository, workspaceRepository);
            IEnumerable<WorkspaceViewModel> result = userWorkspaceViewService.GetUserWorkspaces(authUserId).Result;

            // assert
            memoryCache.Received(1).Set<IEnumerable<WorkspaceViewModel>>(cacheKey, Arg.Any<IEnumerable<WorkspaceViewModel>>(), Arg.Any<TimeSpan>());
        }

        [Test]
        public void GetUserWorkspaces_WhenUserWorkspacesCached_DoesNotFetchFromRepository()
        {
            string authUserId = Guid.NewGuid().ToString();
            string cacheKey = CacheKeys.UserWorkspace(authUserId);
            IDbContext dbContext = Substitute.For<IDbContext>();
            IEnumerable<WorkspaceViewModel>? cachedViewModel = new WorkspaceViewModel[] { new WorkspaceViewModel() };

            UserRepository userRepository = Substitute.For<UserRepository>();
            WorkspaceRepository workspaceRepository = Substitute.For<WorkspaceRepository>();

            IMemoryCacheWrapper memoryCache = Substitute.For<IMemoryCacheWrapper>();
            memoryCache.Get<IEnumerable<WorkspaceViewModel>>(cacheKey).Returns(cachedViewModel);

            UserWorkspaceViewService userWorkspaceViewService = new UserWorkspaceViewService(dbContext, memoryCache, Substitute.For<IMapper>(), userRepository, workspaceRepository);
            IEnumerable<WorkspaceViewModel> result = userWorkspaceViewService.GetUserWorkspaces(authUserId).Result;

            // assert
            memoryCache.DidNotReceive().Set<IEnumerable<WorkspaceViewModel>>(Arg.Any<string>(), Arg.Any<IEnumerable<WorkspaceViewModel>>(), Arg.Any<TimeSpan>());
            workspaceRepository.DidNotReceive().GetManyByIdAsync(Arg.Any<IDbContext>(), Arg.Any<IEnumerable<string>>());
            userRepository.DidNotReceive().GetByAuthIdAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
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
