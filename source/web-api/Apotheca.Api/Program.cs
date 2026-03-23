using Apotheca.Api.Configuration;
using Apotheca.Api.Features.Auth.Login;
using Apotheca.Api.Utilities;
using Apotheca.Api.Features.ProjectTasks.CompleteProjectTask;
using Apotheca.Api.Features.ProjectTasks.GetProjectTasks;
using Apotheca.Api.Features.ProjectTasks.SaveProjectTask;
using Apotheca.Api.Features.Projects.GetUserProjects;
using Apotheca.Data;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var appSettings = new AppSettings(builder.Configuration);

builder.Services.AddSingleton<IAppSettings>(appSettings);
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IDbContextFactory, DbContextFactory>();
builder.Services.AddTransient<INetworkProvider, NetworkProvider>();

builder.Services.AddTransient<FirebaseService>();
builder.Services.AddTransient<CompleteProjectTaskRepository>();
builder.Services.AddTransient<GetProjectTasksRepository>();
builder.Services.AddTransient<SaveProjectTaskRepository>();
builder.Services.AddTransient<SaveProjectTaskValidator>();
builder.Services.AddTransient<GetUserProjectsRepository>();
builder.Services.AddTransient<LoginRepository>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://securetoken.google.com/{appSettings.FirebaseProjectId}";
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = $"https://securetoken.google.com/{appSettings.FirebaseProjectId}",
            ValidAudience = appSettings.FirebaseProjectId,
        };
    });

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
