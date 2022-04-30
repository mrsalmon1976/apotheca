using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.Db
{
    public static class DbSchema
    {
        public static string UserContainer = "User";

        public static string WorkspaceContainer = "Workspace";

        public class UserContainerIndexes
        {
            public static string AuthId = "AuthId_Asc";
        }

    }
}
