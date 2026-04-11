namespace Apotheca.Api.Features.Diagnostics;

public class PingResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string DatabaseStatus { get; set; } = string.Empty;
}
