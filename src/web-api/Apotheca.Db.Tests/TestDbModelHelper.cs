using Apotheca.Db.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.Db.Tests
{
    internal class TestDbModelHelper
    {
        internal static UserDbModel GetUserDbModel(string? authId = null, string? userName = null, string? email = null)
        {
            UserDbModel userDbModel = new UserDbModel();
            userDbModel.AuthId = authId ?? Guid.NewGuid().ToString();
            userDbModel.UserName = userName ?? Guid.NewGuid().ToString();
            userDbModel.Email = email ?? Guid.NewGuid().ToString();
            return userDbModel;
        }
    }
}
