using FirebaseAdmin.Auth;

namespace Apotheca.Api.Features.Auth.Login;

public static class LoginMapper
{
    public static User ToUserModel(this UserRecord userRecord)
    {
        var provider = userRecord.ProviderData.FirstOrDefault();
        var email = userRecord.Email ?? provider?.Email ?? string.Empty;

        return new User
        {
            Uid = userRecord.Uid,
            Email = email,
            DisplayName = userRecord.DisplayName ?? provider?.DisplayName ?? email,
            PhotoUrl = userRecord.PhotoUrl ?? provider?.PhotoUrl,
            ProviderId = provider?.ProviderId ?? "unknown",
        };
    }
}
