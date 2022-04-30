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

    public class UserRepository
    {

        public virtual async Task<UserDbModel> GetByIdAsync(IDbContext dbContext, string id)
        {
            var filterDef = new FilterDefinitionBuilder<UserDbModel>();
            var filter = filterDef.Eq(x => x.Id, id);
            return await dbContext.GetOneAsync<UserDbModel>(DbSchema.UserContainer, filter);

        }

        public virtual async Task<UserDbModel> GetByAuthIdAsync(IDbContext dbContext, string authId)
        {
            var filterDef = new FilterDefinitionBuilder<UserDbModel>();
            var filter = filterDef.Eq(x => x.AuthId, authId);
            return await dbContext.GetOneAsync<UserDbModel>(DbSchema.UserContainer, filter);

        }


    }
}
