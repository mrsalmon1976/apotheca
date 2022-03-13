using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Apotheca.Web.Api.Models;

namespace Apotheca.Web.Api.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class StoreController : ControllerBase
  {

    [HttpGet("userstores")]
    [Authorize]
    public JsonResult GetUserStores()
    {
        var result = new List<Store>();
            result.Add(new Store("Checkers"));
            result.Add(new Store("Woolworths"));
        return new JsonResult(result);
    
      //      return new JsonRe
      //return new ApiResponse("A list of stores");
    }

    public class Store
        {
            public Store(string name)
            {
                this.Name = name;
            }

            public string Name { get; set; }
        }


  }
}
