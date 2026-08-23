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

            public const string WorkspaceAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

            public const int WorkspaceIdLength = 12;
        }

        public static class ProjectRole
        {
            public static string Admin = "ADMIN";

            public static string Contributor = "CONTRIBUTOR";

            public static string Viewer = "VIEWER";
        }

        public static class WorkspaceRole
        {
            public static string Admin = "ADMIN";

            public static string Viewer = "VIEWER";
        }

        public static class WorkspacePlan
        {
            public static string Free = "FREE";

            public static string Paid = "PAID";
        }

        public static class WorkspaceBillingStatus
        {
            public static string Active = "ACTIVE";

            public static string PastDue = "PAST_DUE";
        }

        public static class Billing
        {
            public const int FreeMemberLimit = 3;

            public const long FreeStorageBytesPerMember = 1_073_741_824; // 1 GB

            public const decimal SeatOveragePricePerMonth = 5.00m;

            public const decimal StorageOveragePricePerGb = 0.05m;
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
