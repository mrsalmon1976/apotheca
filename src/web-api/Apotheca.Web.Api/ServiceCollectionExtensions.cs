using Apotheca.Db;
using Apotheca.Web.Api.Models;
using Apotheca.Web.Api.Services;
using AutoMapper;
using MongoDB.Driver;
using Apotheca.BLL.Commands.User;
using Apotheca.BLL.Commands.Workspace;
using Apotheca.Db.Models;
using Apotheca.BLL.Repositories;

public static class ServiceCollectionExtensions
{

    public static void RegisterAutoMapper(this IServiceCollection collection)
    {
        var config = new MapperConfiguration(cfg => {
            cfg.CreateMap<UserDbModel, UserViewModel>();
            cfg.CreateMap<WorkspaceDbModel, WorkspaceViewModel>();
        });

        var mapper = new Mapper(config);
        collection.AddSingleton<IMapper>(mapper);

    }

    public static void RegisterDbContext(this IServiceCollection collection)
    {
        collection.AddSingleton<MongoClient>(new MongoClient("mongodb://localhost:27017/Apotheca"));
        collection.AddTransient<IDbContext>((sp) => {
            IDbContext dbContext = new MongoDbContext(sp.GetService<MongoClient>(), "Apotheca");
            return dbContext;
        });
    }

    public static void RegisterCommands(this IServiceCollection collection)
    {
        collection.AddTransient<CreateUserCommand>();
        collection.AddTransient<CreateWorkspaceCommand>();
    }

    public static void RegisterRepositories(this IServiceCollection collection)
    {
        collection.AddTransient<UserRepository>();
        collection.AddTransient<WorkspaceRepository>();
    }

    public static void RegisterViewServices(this IServiceCollection collection)
    {
        collection.AddTransient<IUserViewService, UserViewService>();
        collection.AddTransient<IUserWorkspaceViewService, UserWorkspaceViewService>();
    }
}

