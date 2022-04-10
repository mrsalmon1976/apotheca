using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace Apotheca.Db.Models
{
    public class UserDbModel : DbModel
    {
        public UserDbModel()
        {
            this.PartitionKey = new PartitionKey(this.Auth0Id);
        }

        [JsonProperty(PropertyName = "auth0Id")]
        public string Auth0Id { get; set; }

        [JsonProperty(PropertyName = "workspaces")]
        public IEnumerable<string> Workspaces { get; set; }


        internal override string ContainerName { get => DbSchema.UserContainer.Name; }

        internal override PartitionKey PartitionKey { get; }

    }
}