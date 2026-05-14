using Apotheca.Api.Configuration;
using Apotheca.Api.Events;
using Apotheca.Api.Events.Documents.DocumentUploaded;
using Apotheca.Api.Events.Documents.DocumentDeleted;
using Apotheca.Api.Events.Documents.DocumentRestored;
using Apotheca.Api.Events.Notes.NoteDeleted;
using Apotheca.Api.Events.Notes.NoteRestored;
using Apotheca.Api.Features.Documents.CreateDocument;
using Apotheca.Api.Features.Documents.CreateDocumentLink;
using Apotheca.Api.Features.Documents.DeleteDocument;
using Apotheca.Api.Features.Documents.DeleteDocumentLink;
using Apotheca.Api.Features.Documents.DownloadDocument;
using Apotheca.Api.Features.Documents.DownloadDocumentByLink;
using Apotheca.Api.Features.Documents.GetDocument;
using Apotheca.Api.Features.Documents.GetDocumentLinks;
using Apotheca.Api.Features.Documents.GetDocuments;
using Apotheca.Api.Features.Documents.RestoreDocument;
using Apotheca.Api.Features.Documents.SaveDocument;
using Apotheca.Api.Features.Documents.SaveDocumentFolder;
using Apotheca.Api.Features.Documents.UploadDocument;
using Google.Cloud.Logging.Console;
using Google.Cloud.Storage.V1;
using Apotheca.Api.Features.Auth.Login;
using Apotheca.Api.Providers;
using Apotheca.Api.Utilities;
using Apotheca.Api.Features.ProjectTasks.CompleteProjectTask;
using Apotheca.Api.Features.ProjectTasks.GetProjectTasks;
using Apotheca.Api.Features.ProjectTasks.SaveProjectTask;
using Apotheca.Api.Features.UserTasks.GetUserTasks;
using Apotheca.Api.Features.Labels.SearchLabels;
using Apotheca.Api.Features.Search;
using Apotheca.Api.Features.Notes.CreateNote;
using Apotheca.Api.Features.Notes.DeleteNote;
using Apotheca.Api.Features.Notes.GetNote;
using Apotheca.Api.Features.Notes.GetNotes;
using Apotheca.Api.Features.Notes.RestoreNote;
using Apotheca.Api.Features.Notes.SaveNote;
using Apotheca.Api.Features.Notes.GetNoteAttachment;
using Apotheca.Api.Features.Notes.SaveNoteAttachment;
using Apotheca.Api.Features.Notes.SaveNoteFolder;
using Apotheca.Api.Features.Projects.GetProjectRecycleBin;
using Apotheca.Api.Features.Projects.GetProjectActivity;
using Apotheca.Api.Features.Projects.GetProjectOverview;
using Apotheca.Api.Features.Projects.GetUserProjects;
using Apotheca.Api.Features.Projects.SaveProject;
using Apotheca.Data;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var appSettings = new AppSettings(builder.Configuration);

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddGoogleCloudConsole();
}

builder.Services.AddSingleton<IAppSettings>(appSettings);
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IDbContextFactory, DbContextFactory>();
builder.Services.AddSingleton<IEventPublisher, PubSubEventPublisher>();
builder.Services.AddTransient<DocumentUploadedEventRepository>();
builder.Services.AddTransient<DocumentDeletedRepository>();
builder.Services.AddTransient<DocumentRestoredRepository>();
builder.Services.AddTransient<NoteDeletedRepository>();
builder.Services.AddTransient<NoteRestoredRepository>();
builder.Services.AddTransient<INetworkProvider, NetworkProvider>();
builder.Services.AddTransient<ISecurityProvider, SecurityProvider>();

builder.Services.AddTransient<FirebaseService>();
builder.Services.AddTransient<CompleteProjectTaskRepository>();
builder.Services.AddTransient<GetProjectTasksRepository>();
builder.Services.AddTransient<GetUserTasksRepository>();
builder.Services.AddTransient<SaveProjectTaskRepository>();
builder.Services.AddTransient<SaveProjectTaskValidator>();
builder.Services.AddTransient<GetProjectActivityRepository>();
builder.Services.AddTransient<GetProjectOverviewRepository>();
builder.Services.AddTransient<GetUserProjectsRepository>();
builder.Services.AddTransient<SaveProjectRepository>();
builder.Services.AddTransient<SaveProjectValidator>();
builder.Services.AddTransient<SearchLabelsRepository>();
builder.Services.AddTransient<SearchRepository>();
builder.Services.AddTransient<CreateNoteRepository>();
builder.Services.AddTransient<DeleteNoteRepository>();
builder.Services.AddTransient<RestoreNoteRepository>();
builder.Services.AddTransient<GetProjectRecycleBinRepository>();
builder.Services.AddTransient<GetNoteRepository>();
builder.Services.AddTransient<GetNotesRepository>();
builder.Services.AddTransient<SaveNoteRepository>();
builder.Services.AddTransient<SaveNoteValidator>();
builder.Services.AddTransient<GetNoteAttachmentRepository>();
builder.Services.AddTransient<SaveNoteAttachmentRepository>();
builder.Services.AddTransient<SaveNoteFolderRepository>();
builder.Services.AddTransient<SaveNoteFolderValidator>();
builder.Services.AddTransient<CreateDocumentRepository>();
builder.Services.AddTransient<CreateDocumentLinkRepository>();
builder.Services.AddTransient<DeleteDocumentRepository>();
builder.Services.AddTransient<DeleteDocumentLinkRepository>();
builder.Services.AddTransient<DownloadDocumentRepository>();
builder.Services.AddTransient<DownloadDocumentByLinkRepository>();
builder.Services.AddTransient<GetDocumentRepository>();
builder.Services.AddTransient<GetDocumentLinksRepository>();
builder.Services.AddTransient<GetDocumentsRepository>();
builder.Services.AddTransient<RestoreDocumentRepository>();
builder.Services.AddTransient<SaveDocumentRepository>();
builder.Services.AddTransient<SaveDocumentValidator>();
builder.Services.AddTransient<SaveDocumentFolderRepository>();
builder.Services.AddTransient<SaveDocumentFolderValidator>();
builder.Services.AddTransient<UploadDocumentRepository>();
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
    })
    .AddJwtBearer("PubSub", options =>
    {
        options.Authority = "https://accounts.google.com";
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = "https://accounts.google.com",
            ValidateAudience = !string.IsNullOrEmpty(appSettings.PubSubAudience),
            ValidAudience = appSettings.PubSubAudience,
        };
    });

builder.Services.AddAuthorization(options =>
{
    if (appSettings.PubSubRequireAuthentication)
    {
        options.AddPolicy("PubSubPush", policy => policy
            .AddAuthenticationSchemes("PubSub")
            .RequireAuthenticatedUser());
    }
    else
    {
        options.AddPolicy("PubSubPush", policy => policy
            .RequireAssertion(_ => true));
    }
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
if (!string.IsNullOrEmpty(appSettings.FirebaseCredentialsPath))
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

StorageClient storageClient;
if (!string.IsNullOrEmpty(appSettings.StorageEmulatorHost))
{
    storageClient = new StorageClientBuilder
    {
        BaseUri = $"{appSettings.StorageEmulatorHost}/storage/v1/",
        UnauthenticatedAccess = true,
    }.Build();
}
else
{
    storageClient = StorageClient.Create(credential);
}
builder.Services.AddSingleton(storageClient);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
