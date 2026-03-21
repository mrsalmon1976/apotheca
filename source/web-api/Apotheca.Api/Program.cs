using Apotheca.Api.Configuration;
using Apotheca.Api.Features.Auth.Login;
using Apotheca.Data;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);
var appSettings = new AppSettings(builder.Configuration);

builder.Services.AddSingleton<IAppSettings>(appSettings);
builder.Services.AddControllers();
builder.Services.AddSingleton<IDbContextFactory, DbContextFactory>();

builder.Services.AddTransient<FirebaseService>();
builder.Services.AddTransient<LoginRepository>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(appSettings.CorsAllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

GoogleCredential credential;
if (appSettings.FirebaseCredentialsPath is not null)
{
    var serviceAccount = await CredentialFactory.FromFileAsync<ServiceAccountCredential>(appSettings.FirebaseCredentialsPath, CancellationToken.None);
    credential = serviceAccount.ToGoogleCredential();
}
else
{
    credential = await GoogleCredential.GetApplicationDefaultAsync();
}

FirebaseApp.Create(new AppOptions
{
    Credential = credential,
    ProjectId = appSettings.FirebaseProjectId,
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

app.Run();
