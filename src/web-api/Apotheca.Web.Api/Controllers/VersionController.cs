using Microsoft.AspNetCore.Mvc;
using Apotheca.Web.Api.Models;

namespace Apotheca.Web.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VersionController : ControllerBase
    {

        public const string UrlGetVersionInfo = "/version";


        [HttpGet(UrlGetVersionInfo)]
        [ProducesResponseType(typeof(IEnumerable<VersionInfo>), 200)]
        public VersionInfo Index()
        {
            return new VersionInfo();
        }

    }
}
