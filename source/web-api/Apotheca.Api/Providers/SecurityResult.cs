namespace Apotheca.Api.Providers;

public record SecurityResult
{
    public bool IsAuthorized { get; init; }
    public string? ErrorMessage { get; init; }
    public string FirebaseUid { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string? Role { get; init; }

    public static SecurityResult Success(string firebaseUid, string userId, string? role = null) => new()
    {
        IsAuthorized = true,
        FirebaseUid  = firebaseUid,
        UserId       = userId,
        Role         = role,
    };

    public static SecurityResult Failure(string errorMessage) => new()
    {
        IsAuthorized = false,
        ErrorMessage = errorMessage,
    };
}
