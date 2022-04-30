using Apotheca.Web.Api.Models;
using Apotheca.Web.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Web.Api.Controllers.User
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ApothecaBaseController
    {
        public const string UrlPostEnsureCurrentUserExists = "/user/current/ensure-exists";

        private readonly IUserViewService _userViewService;

        public UserController(IUserViewService userViewService)
        {
            this._userViewService = userViewService;
        }

        /// <summary>
        /// Post to ensure that a user account is created for the current user.
        /// </summary>
        /// <returns></returns>
        [HttpGet(UrlPostEnsureCurrentUserExists)]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<UserViewModel>), 200)]
        public async Task<IActionResult> EnsureCurrentUserExists()
        {
            UserViewModel userViewModel = await _userViewService.EnsureUserExistsAsync(this.CurrentUser);
            return this.Ok(userViewModel);
        }
    }
}
