using Apotheca.Db;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.BLL.Repositories.Tests.Utils
{
    internal class RepositoryTestUtils
    {

        internal static MongoDbContext CreateDbContext(string databaseName = "ApothecaRepositoryTest")
        {
            return new MongoDbContext(new MongoDB.Driver.MongoClient("mongodb://localhost"), databaseName);
        }

        internal static void DeleteTestDatabase(MongoDbContext dbContext, string dbName = "ApothecaRepositoryTest")
        {
            dbContext.MongoClient.DropDatabase(dbName);
        }


    }
}
