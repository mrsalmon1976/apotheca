using Apotheca.Db;
using Apotheca.Web.Api.Caching;
using Apotheca.Web.Api.Models;
using AutoMapper;
using Apotheca.BLL.Repositories;
using Apotheca.BLL.Commands.User;
using Apotheca.Db.Models;

namespace Apotheca.Web.Api.Services
{
    public interface IUserViewService : IViewService
    {
        Task<UserViewModel> EnsureUserExistsAsync(UserInfo userInfo);

    }

    public class UserViewService : IUserViewService
    {
        private readonly IDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly UserRepository _userRepository;
        private readonly CreateUserCommand _createUserCommand;

        public UserViewService(IDbContext dbContext, IMapper mapper, UserRepository userRepository, CreateUserCommand createUserCommand)
        {
            this._dbContext = dbContext;
            this._mapper = mapper;
            this._userRepository = userRepository;
            this._createUserCommand = createUserCommand;
        }

        public async Task<UserViewModel> EnsureUserExistsAsync(UserInfo userInfo)
        {
            var userModel = await _userRepository.GetByAuthIdAsync(_dbContext, userInfo.AuthId);

            if (userModel == null)
            {
                userModel = await _createUserCommand.ExecuteAsync(this._dbContext, userInfo.AuthId, userInfo.Name, userInfo.Email);

            }
            var userViewModel = _mapper.Map<UserViewModel>(userModel);
            return userViewModel;
        }


    }
}
