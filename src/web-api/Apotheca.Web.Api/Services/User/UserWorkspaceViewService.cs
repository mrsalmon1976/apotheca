using Apotheca.Db;
using Apotheca.Web.Api.Caching;
using Apotheca.Web.Api.Models;
using AutoMapper;
using Apotheca.BLL.Repositories;
using Apotheca.BLL.Commands.User;
using Apotheca.Db.Models;

namespace Apotheca.Web.Api.Services
{
    public interface IUserWorkspaceViewService : IViewService
    {
        Task<IEnumerable<WorkspaceViewModel>> GetUserWorkspaces(string auth0UserId);
    }

    public class UserWorkspaceViewService : IUserWorkspaceViewService
    {
        private readonly IDbContext _dbContext;
        private readonly IMemoryCacheWrapper _memoryCache;
        private readonly IMapper _mapper;
        private readonly UserRepository _userRepository;
        private readonly WorkspaceRepository _workspaceRepository;

        public UserWorkspaceViewService(IDbContext dbContext, IMemoryCacheWrapper memoryCache, IMapper mapper, UserRepository userRepository, WorkspaceRepository workspaceRepository)
        {
            this._dbContext = dbContext;
            this._memoryCache = memoryCache;
            this._mapper = mapper;
            this._userRepository = userRepository;
            this._workspaceRepository = workspaceRepository;
        }

        public async Task<IEnumerable<WorkspaceViewModel>> GetUserWorkspaces(string authUserId)
        {
            string cacheKey = CacheKeys.UserWorkspace(authUserId);
            IEnumerable<WorkspaceViewModel> workspaceViewModels = _memoryCache.Get<IEnumerable<WorkspaceViewModel>>(cacheKey);

            if (workspaceViewModels == null)
            {
                var userModel = await _userRepository.GetByAuthIdAsync(_dbContext, authUserId);
                // if the user has not added any workspaces yet, they may have no user record....so just return an empty enum
                if (userModel == null)
                {
                    return Enumerable.Empty<WorkspaceViewModel>();
                }
                if (userModel.Workspaces == null || !userModel.Workspaces.Any())
                {
                    workspaceViewModels = Enumerable.Empty<WorkspaceViewModel>();
                }
                else
                {
                    var workspaceDbModels = await _workspaceRepository.GetManyByIdAsync(_dbContext, userModel.Workspaces);
                    workspaceViewModels = _mapper.Map<IEnumerable<WorkspaceDbModel>, IEnumerable<WorkspaceViewModel>>(workspaceDbModels);
                }

                _memoryCache.Set(cacheKey, workspaceViewModels, TimeSpan.FromMinutes(15));
            }

            return workspaceViewModels;
        }

    }
}
