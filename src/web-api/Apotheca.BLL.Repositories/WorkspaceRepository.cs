using Apotheca.Db;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apotheca.Db.Models;

namespace Apotheca.BLL.Repositories
{

    public class WorkspaceRepository
    {

        public virtual async Task<IEnumerable<WorkspaceDbModel>> GetManyByIdAsync(IDbContext dbContext, IEnumerable<string> ids)
        {
            var filterDef = new FilterDefinitionBuilder<WorkspaceDbModel>();
            var filter = filterDef.In(x => x.Id, ids);
            return await dbContext.GetManyAsync<WorkspaceDbModel>(DbSchema.WorkspaceContainer, filter);

        }
    }
}
