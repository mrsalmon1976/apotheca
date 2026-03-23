namespace Apotheca.Api.Utilities;

public class NetworkProvider(IHttpContextAccessor httpContextAccessor) : INetworkProvider
{
    public string? GetClientIpAddress()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null) return null;

        if (request.Headers.TryGetValue("CF-Connecting-IP", out var cfIp) && !string.IsNullOrWhiteSpace(cfIp))
            return cfIp.ToString();

        if (request.Headers.TryGetValue("X-Forwarded-For", out var forwarded) && !string.IsNullOrWhiteSpace(forwarded))
            return forwarded.ToString().Split(',')[0].Trim();

        return httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}
