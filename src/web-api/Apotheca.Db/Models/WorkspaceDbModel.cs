using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace Apotheca.Db.Models
{
    public class WorkspaceDbModel : DbModel
    {
        public WorkspaceDbModel()
        {
            this.PartitionKey = new PartitionKey(this.Id);
            this.CreateDateTime = DateTime.UtcNow;
        }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }


        [JsonProperty(PropertyName = "createDateTime")]
        public DateTime CreateDateTime { get; internal set; }

        internal override string ContainerName { get => DbSchema.WorkspaceContainer.Name; }

        internal override PartitionKey PartitionKey { get; }

    }
}