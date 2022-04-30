using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apotheca.Db.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Apotheca.Db
{
    public class MongoDbMigrator
    {

        public static void RunMigrations(MongoDbContext dbContext)
        {
            var database = dbContext.Database;
            var collectionNames = database.ListCollectionNames().ToEnumerable<string>().ToList();

            // User
            var userCollection = CreateCollectionIfNotExists<UserDbModel>(database, collectionNames, DbSchema.UserContainer);
            CreateIndexIfNotExists(userCollection, DbSchema.UserContainerIndexes.AuthId, Builders<UserDbModel>.IndexKeys.Ascending(wf => wf.AuthId));

            // Workspace
            CreateCollectionIfNotExists<WorkspaceDbModel>(database, collectionNames, DbSchema.WorkspaceContainer);

        }

        private static IMongoCollection<T> CreateCollectionIfNotExists<T>(IMongoDatabase database, List<string> existingCollectionNames, string collectionName) where T : DbModel
        {
            if (!existingCollectionNames.Contains(collectionName))
            {
                database.CreateCollection(collectionName);
            }
            return database.GetCollection<T>(collectionName);
        }

        /// <summary>
        /// Creates an index on a collection - only if it doesn't exist already.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="indexName"></param>
        /// <param name="indexDefinition"></param>
        /// <returns>True if the index was created, False if the index already exists.</returns>
        private static bool CreateIndexIfNotExists<T>(IMongoCollection<T> collection, string indexName, IndexKeysDefinition<T> indexDefinition) where T : DbModel
        {
            IEnumerable<BsonDocument> indexDocuments = collection.Indexes.List().ToList();
            foreach (BsonDocument index in indexDocuments)
            {
                string currentIndexName = index.GetValue("name").AsString;
                if (indexName == currentIndexName)
                {
                    return false;
                }
            }

            // create indexes
            var indexOptions = new CreateIndexOptions();
            indexOptions.Name = indexName;

            var indexModel = new CreateIndexModel<T>(indexDefinition, indexOptions);
            collection.Indexes.CreateOne(indexModel);
            return true;
        }

    }
}
