using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features;

[Authorize]
[ApiController]
public abstract class AuthenticatedBaseController : ControllerBase
{
    protected string? GetFirebaseUid() => User.FindFirst("sub")?.Value;
}
