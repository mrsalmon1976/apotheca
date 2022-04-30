using Apotheca.Db;
using Apotheca.Db.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.BLL.Commands.User
{
    public class CreateUserCommand
    {
        public CreateUserCommand()
        {

        }

        public virtual async Task<UserDbModel> ExecuteAsync(IDbContext dbContext, string authUserId, string userName, string email)
        {
            UserDbModel model = new UserDbModel();
            model.AuthId = authUserId;
            model.UserName = userName;
            model.Email = email;

            await dbContext.InsertAsync<UserDbModel>(DbSchema.UserContainer, model);

            return model;
        }

    }
}
