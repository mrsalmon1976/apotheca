using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Apotheca.Data
{
    public static class DataConstants
    {
        public const string DefaultProjectName = "My Project";

        public static class KeyDefinition
        {
            public const string ProjectAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

            public const int ProjectIdLength = 12;
        }

        public static class ProjectRole
        {
            public static string Owner = "Owner";

            public static string User = "User";

            public static string Viewer = "Viewer";
        } 
    }
}
