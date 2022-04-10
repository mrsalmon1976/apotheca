using Apotheca.Web.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Web.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ApothecaBaseController
    {
        public const string UrlGetCurrentUserDocumentStores = "/user/current/documentstores";

        /// <summary>
        /// Gets a list of stores the currently logged in user has access to.
        /// </summary>
        /// <returns></returns>
        [HttpGet(UrlGetCurrentUserDocumentStores)]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<WorkspaceViewModel>), 200)]
        public async Task<IActionResult> GetCurrentUserDocumentStores()
        {
            var result = new List<WorkspaceViewModel>();
            result.Add(new WorkspaceViewModel() {  Id = Guid.NewGuid(), Name = "Checkers" });
            result.Add(new WorkspaceViewModel() { Id = Guid.NewGuid(), Name = "Pick 'n Pay" });
            result.Add(new WorkspaceViewModel() { Id = Guid.NewGuid(), Name = "Spar" });
            result.Add(new WorkspaceViewModel() { Id = Guid.NewGuid(), Name = "Woolworths" });
            return this.Ok(result);
            //var x = this.Ok();
        }
    }
}
