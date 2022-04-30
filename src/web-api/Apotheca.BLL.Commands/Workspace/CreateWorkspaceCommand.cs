using Apotheca.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apotheca.Db.Models;

namespace Apotheca.BLL.Commands.Workspace
{
    public class CreateWorkspaceCommand
    {
        public virtual async Task<WorkspaceDbModel> ExecuteAsync(IDbContext dbContext, string name)
        {
            WorkspaceDbModel model = new WorkspaceDbModel();
            model.Name = name;

            await dbContext.InsertAsync<WorkspaceDbModel>(DbSchema.WorkspaceContainer, model);

            return model;
        }
    }
}
