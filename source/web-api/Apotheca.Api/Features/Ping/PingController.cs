using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Diagnostics;

[ApiController]
[Route("[controller]")]
public class PingController(IServiceProvider services) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PingResponse>> Get(CancellationToken cancellationToken)
    {
        // NOTE: ServiceProvider is injected here to ensure that any DI errors do not result in a 
        // crash of the entire endpoint - I'd rather see it in the error message
        string dbStatus;
        string errorMessage = "";
        try
        {
            var dbContextFactory = services.GetRequiredService<IDbContextFactory>();
            await using var db = await dbContextFactory.CreateAsync(cancellationToken);
            await db.QueryFirstOrDefaultAsync<string?>("SELECT id FROM projects LIMIT 1", cancellationToken: cancellationToken);
            dbStatus = "ok";
        }
        catch (Exception ex)
        {
            dbStatus = "error";
            errorMessage = $"[{ex.Message}]";
        }

        return Ok(new PingResponse
        {
            Status = "ok",
            Timestamp = DateTimeOffset.UtcNow,
            DatabaseStatus = dbStatus,
            ErrorMessage = errorMessage
        });
    }
}
