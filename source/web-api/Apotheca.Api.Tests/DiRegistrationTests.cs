using Apotheca.Api.Configuration;
using Apotheca.Api.Features.Auth.Login;
using Apotheca.Api.Features.ProjectTasks.CompleteProjectTask;
using Apotheca.Api.Features.ProjectTasks.GetProjectTasks;
using Apotheca.Api.Features.ProjectTasks.SaveProjectTask;
using Apotheca.Api.Features.Projects.GetUserProjects;
using Apotheca.Api.Utilities;
using Apotheca.Data;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Apotheca.Api.Tests;

[TestFixture]
public class DiRegistrationTests
{
    //[Test]
    //public void AllServices_Resolve_WhenContainerIsBuilt()
    //{
    //    var services = new ServiceCollection();

    //    // Stub infrastructure that requires real external resources
    //    services.AddSingleton(Substitute.For<IDbContextFactory>());
    //    services.AddSingleton(Substitute.For<IAppSettings>());
    //    services.AddSingleton(Substitute.For<INetworkProvider>());

    //    // Exact registrations from Program.cs
    //    services.AddTransient<FirebaseService>();
    //    services.AddTransient<CompleteProjectTaskRepository>();
    //    services.AddTransient<GetProjectTasksRepository>();
    //    services.AddTransient<SaveProjectTaskRepository>();
    //    services.AddTransient<SaveProjectTaskValidator>();
    //    services.AddTransient<GetUserProjectsRepository>();
    //    services.AddTransient<LoginRepository>();

    //    // Register controllers as services so their constructor dependencies are validated
    //    services.AddControllers().AddControllersAsServices();

    //    Assert.DoesNotThrow(() =>
    //        services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true }));
    //}
}
