using Apotheca.Data;

namespace Apotheca.Api.Providers;

public interface ISecurityProvider
{
    Task<SecurityResult> AuthorizeAccessAsync(IDbContext db, CancellationToken cancellationToken = default);

    Task<SecurityResult> AuthorizeProjectAccessAsync(IDbContext db, string projectId, CancellationToken cancellationToken = default);

    Task<SecurityResult> AuthorizeWorkspaceAccessAsync(IDbContext db, string workspaceId, bool requireAdmin = false, CancellationToken cancellationToken = default);

}
