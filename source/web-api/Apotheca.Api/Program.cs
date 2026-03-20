using Apotheca.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IDbContextFactory, DbContextFactory>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
