namespace Apotheca.Api.Utilities;

public interface INetworkProvider
{
    string? GetClientIpAddress();
}
