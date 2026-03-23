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
            public static string Owner = "OWNER";

            public static string User = "USER";

            public static string Viewer = "VIEWER";
        }

        public static class TaskPriority
        {
            public static string None = "NONE";

            public static string Low = "LOW";

            public static string Medium = "MEDIUM";
            
            public static string High = "HIGH";
            
            public static string Urgent = "URGENT";
        }

        public static class UserLogEventType
        {
            public const string Login = "LOGIN";
        }

    }
}
