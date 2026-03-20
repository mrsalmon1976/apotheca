using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Diagnostics;

[ApiController]
[Route("[controller]")]
public class PingController : ControllerBase
{
    [HttpGet]
    public ActionResult<PingResponse> Get()
    {
        return Ok(new PingResponse
        {
            Status = "ok",
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}
