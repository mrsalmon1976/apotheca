using Apotheca.Api.Features.Auth.Login;
using Apotheca.Test.Common;
using NUnit.Framework;

namespace Apotheca.Api.Tests.Features.Auth.Login;

[TestFixture]
public class LoginMapperTests
{
    [Test]
    public void ToUserModel_MapsUid()
    {
        string uid = RandomData.GuidString();
        var userRecord = UserRecordBuilder.Build(uid: uid);

        var result = userRecord.ToUserModel();

        Assert.That(result.Uid, Is.EqualTo(uid));
    }

    [Test]
    public void ToUserModel_MapsEmailFromUser()
    {
        string email= RandomData.Email();
        var userRecord = UserRecordBuilder.Build(uid: "uid-123", email: email);

        var result = userRecord.ToUserModel();

        Assert.That(result.Email, Is.EqualTo(email));
    }

    [Test]
    public void ToUserModel_FallsBackToProviderEmail_WhenUserEmailIsNull()
    {
        string email = RandomData.Email();
        var userRecord = UserRecordBuilder.Build(
            uid: "uid-123",
            email: null,
            providers: new ProviderBuilder("google.com", Email: email));

        var result = userRecord.ToUserModel();

        Assert.That(result.Email, Is.EqualTo(email));
    }

    [Test]
    public void ToUserModel_ReturnsEmptyEmail_WhenUserAndProviderEmailAreNull()
    {
        var userRecord = UserRecordBuilder.Build(
            uid: "uid-123",
            email: null,
            providers: new ProviderBuilder("google.com", Email: null));

        var result = userRecord.ToUserModel();

        Assert.That(result.Email, Is.Empty);
    }

    [Test]
    public void ToUserModel_MapsDisplayNameFromUser()
    {
        string displayName = RandomData.StringWord();
        var userRecord = UserRecordBuilder.Build(uid: "uid-123", displayName: displayName);

        var result = userRecord.ToUserModel();

        Assert.That(result.DisplayName, Is.EqualTo(displayName));
    }

    [Test]
    public void ToUserModel_FallsBackToProviderDisplayName_WhenUserDisplayNameIsNull()
    {
        string displayName = RandomData.StringWord();
        var userRecord = UserRecordBuilder.Build(
            uid: "uid-123",
            displayName: null,
            providers: new ProviderBuilder("google.com", DisplayName: displayName));

        var result = userRecord.ToUserModel();

        Assert.That(result.DisplayName, Is.EqualTo(displayName));
    }

    [Test]
    public void ToUserModel_FallsBackToEmail_WhenDisplayNameIsNullOnUserAndProvider()
    {
        string email = RandomData.Email();
        var userRecord = UserRecordBuilder.Build(
            uid: "uid-123",
            email: email,
            displayName: null,
            providers: new ProviderBuilder("google.com", DisplayName: null));

        var result = userRecord.ToUserModel();

        Assert.That(result.DisplayName, Is.EqualTo(email));
    }

    [Test]
    public void ToUserModel_MapsPhotoUrlFromUser()
    {
        string photoUrl = RandomData.Url();
        var userRecord = UserRecordBuilder.Build(uid: "uid-123", photoUrl: photoUrl);

        var result = userRecord.ToUserModel();

        Assert.That(result.PhotoUrl, Is.EqualTo(photoUrl));
    }

    [Test]
    public void ToUserModel_FallsBackToProviderPhotoUrl_WhenUserPhotoUrlIsNull()
    {
        string photoUrl = RandomData.Url();
        var userRecord = UserRecordBuilder.Build(
            uid: "uid-123",
            photoUrl: null,
            providers: new ProviderBuilder("google.com", PhotoUrl: photoUrl));

        var result = userRecord.ToUserModel();

        Assert.That(result.PhotoUrl, Is.EqualTo(photoUrl));
    }

    [Test]
    public void ToUserModel_ReturnsNullPhotoUrl_WhenUserAndProviderPhotoUrlAreNull()
    {
        var userRecord = UserRecordBuilder.Build(
            uid: "uid-123",
            photoUrl: null,
            providers: new ProviderBuilder("google.com", PhotoUrl: null));

        var result = userRecord.ToUserModel();

        Assert.That(result.PhotoUrl, Is.Null);
    }

    [Test]
    public void ToUserModel_MapsProviderIdFromFirstProvider()
    {
        var userRecord = UserRecordBuilder.Build(
            uid: "uid-123",
            providers: new ProviderBuilder("google.com"));

        var result = userRecord.ToUserModel();

        Assert.That(result.ProviderId, Is.EqualTo("google.com"));
    }

    [Test]
    public void ToUserModel_ReturnsUnknownProviderId_WhenNoProviders()
    {
        var userRecord = UserRecordBuilder.Build(uid: "uid-123");

        var result = userRecord.ToUserModel();

        Assert.That(result.ProviderId, Is.EqualTo("unknown"));
    }

    [Test]
    public void ToUserModel_UserEmailTakesPrecedenceOverProviderEmail()
    {
        var userRecord = UserRecordBuilder.Build(
            uid: "uid-123",
            email: "user@example.com",
            providers: new ProviderBuilder("google.com", Email: "provider@example.com"));

        var result = userRecord.ToUserModel();

        Assert.That(result.Email, Is.EqualTo("user@example.com"));
    }

    [Test]
    public void ToUserModel_UserDisplayNameTakesPrecedenceOverProviderDisplayName()
    {
        var userRecord = UserRecordBuilder.Build(
            uid: "uid-123",
            displayName: "User Name",
            providers: new ProviderBuilder("google.com", DisplayName: "Provider Name"));

        var result = userRecord.ToUserModel();

        Assert.That(result.DisplayName, Is.EqualTo("User Name"));
    }

    [Test]
    public void ToUserModel_UserPhotoUrlTakesPrecedenceOverProviderPhotoUrl()
    {
        var userRecord = UserRecordBuilder.Build(
            uid: "uid-123",
            photoUrl: "https://example.com/user-photo.jpg",
            providers: new ProviderBuilder("google.com", PhotoUrl: "https://example.com/provider-photo.jpg"));

        var result = userRecord.ToUserModel();

        Assert.That(result.PhotoUrl, Is.EqualTo("https://example.com/user-photo.jpg"));
    }
}
