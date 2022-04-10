using Apotheca.Db;
using Apotheca.Db.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.Command.Workspace
{
    public class CreateWorkspaceCommand
    {
        public CreateWorkspaceCommand(IDbContext dbContext)
        {
            this.DbContext = dbContext;
        }

        public IDbContext DbContext { get; set; }

        public WorkspaceDbModel Execute(string name)
        {
            WorkspaceDbModel model = new WorkspaceDbModel();
            model.Name = name;
            return model;
        }
    }
}
