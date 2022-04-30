using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Apotheca.Db.Models
{
    public class UserDbModel : DbModel
    {
        public UserDbModel()
        {
            this.CreateDateTime = DateTime.UtcNow;
        }

        public string AuthId { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string[] Workspaces { get; set; }

        public DateTime CreateDateTime { get; internal set; }


    }
}