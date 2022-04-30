using Newtonsoft.Json;

namespace Apotheca.Db.Models
{
    public class WorkspaceDbModel : DbModel
    {
        public WorkspaceDbModel()
        {
            this.CreateDateTime = DateTime.UtcNow;
        }

        public string Name { get; set; }


        public DateTime CreateDateTime { get; internal set; }

    }
}