using Apotheca.Db.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.Db
{
    public static class DbSchema
    {
        public static class WorkspaceContainer
        {
            public const string Name = "Workspace";

            public const string PartitionKeyPath = "/id";

        }

        public static class UserContainer
        {
            public const string Name = "User";

            public const string PartitionKeyPath = "/auth0Id";
        }

    }
}
