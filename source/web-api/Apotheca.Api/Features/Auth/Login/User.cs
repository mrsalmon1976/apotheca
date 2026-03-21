using Apotheca.Data;
using System.Threading;

namespace Apotheca.Api.Features.Auth.Login
{
    public class User
    {

        public string Email { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string? PhotoUrl { get; set; }

        public string ProviderId { get; set; } = string.Empty;

        public string Uid { get; set; } = string.Empty;
    }

}
