using System.Collections;
using System.Reflection;
using FirebaseAdmin.Auth;

namespace Apotheca.Api.Tests.Features.Auth.Login;

internal static class UserRecordBuilder
{
    private static readonly Assembly FirebaseAssembly = typeof(UserRecord).Assembly;
    private static readonly Type UserType = FirebaseAssembly.GetType("FirebaseAdmin.Auth.Users.GetAccountInfoResponse+User")!;
    private static readonly Type ProviderType = FirebaseAssembly.GetType("FirebaseAdmin.Auth.Users.GetAccountInfoResponse+Provider")!;
    private static readonly ConstructorInfo UserRecordCtor = typeof(UserRecord)
        .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { UserType }, null)!;

    public static UserRecord Build(
        string uid,
        string? email = null,
        string? displayName = null,
        string? photoUrl = null,
        params ProviderBuilder[] providers)
    {
        var user = Activator.CreateInstance(UserType)!;
        SetProperty(user, UserType, "UserId", uid);
        SetProperty(user, UserType, "Email", email);
        SetProperty(user, UserType, "DisplayName", displayName);
        SetProperty(user, UserType, "PhotoUrl", photoUrl);

        var providersList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(ProviderType))!;
        foreach (var p in providers)
        {
            var provider = Activator.CreateInstance(ProviderType)!;
            SetProperty(provider, ProviderType, "ProviderID", p.ProviderId);
            SetProperty(provider, ProviderType, "Email", p.Email);
            SetProperty(provider, ProviderType, "DisplayName", p.DisplayName);
            SetProperty(provider, ProviderType, "PhotoUrl", p.PhotoUrl);
            providersList.Add(provider);
        }

        SetProperty(user, UserType, "Providers", providersList);

        return (UserRecord)UserRecordCtor.Invoke(new[] { user });
    }

    private static void SetProperty(object target, Type type, string propertyName, object? value)
    {
        type.GetProperty(propertyName)?.SetValue(target, value);
    }
}

internal record ProviderBuilder(
    string ProviderId,
    string? Email = null,
    string? DisplayName = null,
    string? PhotoUrl = null);
