using Apotheca.Web.Api.Models;
using Apotheca.Web.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Web.Api.Controllers.User
{
    [ApiController]
    [Route("[controller]")]
    public class UserWorkspaceController : ApothecaBaseController
    {
        public const string UrlGetCurrentUserWorkspaces = "/user/current/workspaces";

        private readonly IUserWorkspaceViewService _userWorkspaceViewService;

        public UserWorkspaceController(IUserWorkspaceViewService userWorkspaceViewService)
        {
            this._userWorkspaceViewService = userWorkspaceViewService;
        }

        /// <summary>
        /// Gets a list of stores the currently logged in user has access to.
        /// </summary>
        /// <returns></returns>
        [HttpGet(UrlGetCurrentUserWorkspaces)]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<WorkspaceViewModel>), 200)]
        public async Task<IActionResult> GetCurrentUserWorkspaces()
        {
            var result = await _userWorkspaceViewService.GetUserWorkspaces(this.CurrentUser.AuthId);
            return this.Ok(result);
        }

    }
}
