using Microsoft.AspNetCore.Mvc;
using Apotheca.Web.Api.Models;

namespace Apotheca.Web.Api.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class VersionController : ControllerBase
  {
            
    [HttpGet("")]
    public VersionInfo Index()
    {
        return new VersionInfo();
    }

  }
}
