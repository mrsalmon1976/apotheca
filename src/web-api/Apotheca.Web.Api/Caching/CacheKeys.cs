namespace Apotheca.Web.Api.Caching
{
    public static class CacheKeys
    {
        public static string UserWorkspace(string auth0UserId)
        {
            return $"UserWorkspace_{auth0UserId}";
        }
    }
}
