using System.Text.Json;
using Apotheca.Data;
using NanoidDotNet;

namespace Apotheca.Api.Features.Auth.Login
{
    public class LoginRepository
    {
        public virtual async Task CreateProjectActivityLogAsync(IDbContext db, string projectId, string projectName, string userId, string username)
        {
            await db.ExecuteAsync(
                "INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id) VALUES (@ProjectId, @RefId, 'PROJECT', @LogMessage, @UserId)",
                new { ProjectId = projectId, RefId = projectId, LogMessage = $"Project {projectName} created by user {username}", UserId = userId });
        }

        public virtual async Task<string> CreateProjectAsync(IDbContext db, string name, string workspaceId)
        {
            string projectId = Nanoid.Generate(DataConstants.KeyDefinition.ProjectAlphabet, DataConstants.KeyDefinition.ProjectIdLength);
            await db.ExecuteAsync(
                "INSERT INTO projects (id, name, workspace_id) VALUES (@Id, @Name, @WorkspaceId)",
                new { Id = projectId, Name = name, WorkspaceId = workspaceId });

            return projectId;
        }

        public virtual async Task CreateProjectAuditLogAsync(IDbContext db, string projectId, string userId)
        {
            var newData = JsonSerializer.Serialize(new { id = projectId });
            await db.ExecuteAsync(
                "INSERT INTO audit.project_logs (project_id, changed_by, operation, log_message, new_data) VALUES (@ProjectId, @ChangedBy, @Operation, @LogMessage, @NewData::jsonb)",
                new { ProjectId = projectId, ChangedBy = userId, Operation = "INSERT", LogMessage = $"Project '{DataConstants.DefaultProjectName}' created", NewData = newData });
        }

        public virtual async Task<string> CreateUserAsync(IDbContext db, User user)
        {
            string userId = Nanoid.Generate();
            await db.ExecuteAsync(
                "INSERT INTO users (id, email, display_name, photo_url) VALUES (@Id, @Email, @DisplayName, @PhotoUrl)",
                new { Id = userId, Email = user.Email, DisplayName = user.DisplayName, PhotoUrl = user.PhotoUrl });

            return userId;
        }

        public virtual async Task CreateUserIdentityAsync(IDbContext db, User user, string userId)
        {
            await db.ExecuteAsync(
                "INSERT INTO user_firebase_identities (firebase_uid, user_id, provider_id) VALUES (@Uid, @UserId, @ProviderId)",
                new { Uid = user.Uid, UserId = userId, ProviderId = user.ProviderId });
        }

        public virtual async Task CreateUserLoginLogAsync(IDbContext db, string userId, string? ipAddress)
        {
            await db.ExecuteAsync(
                "INSERT INTO audit.user_logs (id, user_id, event_type, log_message, ip_address) VALUES (@Id, @UserId, @EventType, @LogMessage, @IpAddress)",
                new { Id = Nanoid.Generate(), UserId = userId, EventType = DataConstants.UserLogEventType.Login, LogMessage = "User logged in.", IpAddress = ipAddress });
        }

        public virtual async Task CreateUserProjectAsync(IDbContext db, string userId, string projectId, string role)
        {
            await db.ExecuteAsync(
                "INSERT INTO project_users (user_id, project_id, project_role) VALUES (@UserId, @ProjectId, @Role)",
                new { UserId = userId, ProjectId = projectId, Role = role });
        }

        public virtual async Task CreateUserSettingsAsync(IDbContext db, string userId, string currentWorkspaceId)
        {
            await db.ExecuteAsync(
                "INSERT INTO user_settings (user_id, current_workspace_id) VALUES (@UserId, @CurrentWorkspaceId)",
                new { UserId = userId, CurrentWorkspaceId = currentWorkspaceId });
        }

        public virtual async Task<string> CreateWorkspaceAsync(IDbContext db, string name)
        {
            string workspaceId = Nanoid.Generate(DataConstants.KeyDefinition.WorkspaceAlphabet, DataConstants.KeyDefinition.WorkspaceIdLength);
            await db.ExecuteAsync(
                "INSERT INTO workspaces (id, name) VALUES (@Id, @Name)",
                new { Id = workspaceId, Name = name });

            return workspaceId;
        }

        public virtual async Task CreateWorkspaceMemberAsync(IDbContext db, string workspaceId, string userId, string workspaceRole)
        {
            await db.ExecuteAsync(
                "INSERT INTO workspace_users (workspace_id, user_id, workspace_role) VALUES (@WorkspaceId, @UserId, @WorkspaceRole)",
                new { WorkspaceId = workspaceId, UserId = userId, WorkspaceRole = workspaceRole });
        }

        public virtual async Task<string?> GetUserIdByEmailAsync(IDbContext db, string email)
        {
            var userId = await db.QueryFirstOrDefaultAsync<string>(
                "SELECT id FROM users WHERE email = @Email",
                new { Email = email });

            return userId;
        }

        public virtual async Task<string?> GetUserIdByFirebaseUidAsync(IDbContext db, string firebaseUid)
        {
            return await db.QueryFirstOrDefaultAsync<string?>(
                "SELECT user_id FROM user_firebase_identities WHERE firebase_uid = @FirebaseUid",
                new { FirebaseUid = firebaseUid });
        }

        public virtual async Task<bool> UserFirebaseIdentityExistsAsync(IDbContext db, string uid)
        {
            string? userId = await db.QueryFirstOrDefaultAsync<string>(
                "SELECT user_id FROM user_firebase_identities WHERE firebase_uid = @Uid",
                new { Uid = uid });

            return (userId != null);
        }
    }
}
