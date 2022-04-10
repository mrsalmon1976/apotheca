using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.Db.Models
{
    public abstract class DbModel
    {
        public DbModel()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonIgnore]
        internal abstract string ContainerName { get; }

        [JsonIgnore]
        internal abstract PartitionKey PartitionKey { get; }
    }
}
