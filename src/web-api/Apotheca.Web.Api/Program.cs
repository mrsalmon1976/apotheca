using Apotheca.Web.Api.Caching;
using Apotheca.Web.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

// Add custom services to the container.
builder.Services.RegisterAutoMapper();
builder.Services.AddTransient<IMemoryCacheWrapper, MemoryCacheWrapper>();
builder.Services.RegisterDbContext();
builder.Services.RegisterRepositories();
builder.Services.RegisterCommands();
builder.Services.RegisterViewServices();


builder.Services.AddCors(p => p.AddPolicy("ApothecaCorsPolicy", builder =>
{
    builder.WithOrigins("http://localhost:4040").AllowAnyMethod().AllowAnyHeader();
}));

// set up authentication
string domain = $"https://{builder.Configuration["Auth0:Domain"]}/";
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = domain;
        options.Audience = builder.Configuration["Auth0:Audience"];
        // If the access token does not have a `sub` claim, `User.Identity.Name` will be `null`. Map it to a different claim by setting the NameClaimType below.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });
builder.Services.AddAuthorization(options => 
{
    // https://auth0.com/docs/quickstart/backend/aspnet-core-webapi
    //options.AddPolicy("read:messages", policy => policy.Requirements.Add(new HasScopeRequirement("read:messages", domain)));
});
builder.Services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ApothecaCorsPolicy");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.ExecuteMigrations();
app.Run();

// required of .NET 6 integration tests
public partial class Program { }
