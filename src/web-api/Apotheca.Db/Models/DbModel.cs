using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.Db.Models
{
    [BsonIgnoreExtraElements]
    public abstract class DbModel
    {
        public DbModel()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        [JsonProperty(PropertyName = "id")]
        [BsonId]
        public string Id { get; set; }


    }
}
