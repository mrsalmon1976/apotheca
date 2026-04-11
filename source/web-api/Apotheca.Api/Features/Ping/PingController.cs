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
        string dbStatus;
        try
        {
            var dbContextFactory = services.GetRequiredService<IDbContextFactory>();
            await using var db = await dbContextFactory.CreateAsync(cancellationToken);
            await db.QueryFirstOrDefaultAsync<string?>("SELECT id FROM projects LIMIT 1", cancellationToken: cancellationToken);
            dbStatus = "ok";
        }
        catch (Exception ex)
        {
            dbStatus = $"error: {ex.Message}";
        }

        return Ok(new PingResponse
        {
            Status = "ok",
            Timestamp = DateTimeOffset.UtcNow,
            DatabaseStatus = dbStatus
        });
    }
}
