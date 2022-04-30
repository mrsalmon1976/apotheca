using Apotheca.Db.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.BLL.Repositories.Tests
{
    internal class DbModelUtils
    {
        internal static UserDbModel GetUserDbModel(string? authId = null, string? userName = null, string? email = null)
        {
            UserDbModel userDbModel = new UserDbModel();
            userDbModel.AuthId = authId ?? Guid.NewGuid().ToString();
            userDbModel.UserName = userName ?? Guid.NewGuid().ToString();
            userDbModel.Email = email ?? Guid.NewGuid().ToString();
            return userDbModel;
        }

        internal static WorkspaceDbModel GetWorkspaceDbModel(string? name = null)
        {
            WorkspaceDbModel workspaceDbModel = new WorkspaceDbModel();
            workspaceDbModel.Name = name ?? Guid.NewGuid().ToString();
            return workspaceDbModel;
        }
    }
}
