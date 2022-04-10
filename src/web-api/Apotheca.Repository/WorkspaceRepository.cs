using Apotheca.Db;
using Apotheca.Db.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.Repository
{
    public class WorkspaceRepository
    {
        public WorkspaceRepository()
        {

        }

        public Task<IEnumerable<WorkspaceDbModel>> GetManyByIdAsync(IDbContext dbContext, IEnumerable<string> ids)
        {
            const string query = "SELECT * FROM c WHERE ARRAY_CONTAINS(@ids, c.id)";
            List<DbParameter> parameters = new List<DbParameter>() { new DbParameter("@ids", ids.ToArray()) };
            return dbContext.GetManyItemsAsync<WorkspaceDbModel>(DbSchema.WorkspaceContainer.Name, query, parameters);
        }
    }
}
